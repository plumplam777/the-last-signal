using System.Collections.Generic;
using System.IO;
using System.Linq;
using LastSignal.Core;
using LastSignal.Interaction;
using LastSignal.Inventory;
using LastSignal.Player;
using LastSignal.Puzzles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static LastSignal.EditorTools.BuilderKit;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Code-built greybox of the Docking Bay (Phase 2 vertical slice) in the "Brass &amp; Signal" art direction.
    /// Overwrites Sector_DockingBay.unity — once the scene is hand-edited, stop using this builder.
    /// Flow: spawn at the ring airlock → flashlight on the bench → fuse on the crates → fuse box → powered door → corridor.
    /// </summary>
    public static class DockingBayBuilder
    {
        public const string VolumePath = "Assets/_Project/Settings/Volume_DockingBay.asset";


        /// <summary>Batchmode entry: build content, register scenes, render previews.</summary>
        public static void BuildAndPreview()
        {
            Build();
            ProjectSetup.RegisterBuildScenes();
            PreviewRenderer.RenderDockingBay();
        }

        [MenuItem("Last Signal/Build Content/Docking Bay (overwrites scene)")]
        public static void Build()
        {
            StationMaterials.Create();
            DockingBayContent.CreateAll();
            var flashlightItem = DockingBayContent.Flashlight;
            var fuseItem = DockingBayContent.Fuse;
            CreateInspectPrefabs(flashlightItem, fuseItem);
            var playerPrefab = PlayerPrefabBuilder.Build();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("[Generated] Sector_DockingBay");
            var sector = root.AddComponent<SectorRoot>();
            sector.EditorConfigure(SectorIds.Docking, playerPrefab);

            var env = Group("Environment", root.transform).transform;
            var poweredOn = new List<GameObject>();
            var poweredOff = new List<GameObject>();

            BuildHall(env, poweredOn, poweredOff);
            BuildAirlock(env);
            BuildCorridor(env, poweredOn);
            var props = Group("Props", root.transform).transform;
            BuildBenchWithFlashlight(props, flashlightItem);
            BuildCratesWithFuse(props, fuseItem);
            BuildFuseBox(props, fuseItem, poweredOff);
            BuildBreaker(props);
            BuildLanderConsole(props, poweredOn);
            BuildInnerDoor(props);
            BuildEscapePodCradle(props);
            BuildDialogueTriggers(root.transform);
            BuildEscapeExit(root.transform);

            var power = Group("PowerController", root.transform);
            power.AddComponent<PoweredObject>().EditorConfigure(SectorIds.Docking, poweredOn.ToArray(), poweredOff.ToArray());
            foreach (var go in poweredOn) go.SetActive(false); // scene starts unpowered

            var spawn = Group("Spawn_Default", root.transform, new Vector3(0f, 0.05f, -7.4f));
            spawn.AddComponent<SpawnPoint>().EditorSetId("default");

            SetupLightingAndPost(root.transform);
            root.AddComponent<LastSignal.Audio.SectorAmbience>().EditorSet(new List<LastSignal.Audio.AmbienceLoop>
            {
                new() { clip = "amb_station_hum", volume = 0.5f },
                new() { clip = "amb_ventilation", volume = 0.35f, whileFlag = WorldKeys.Power(SectorIds.Docking) },
            });

            Directory.CreateDirectory(Path.GetDirectoryName(ProjectSetup.DockingBayPath)!);
            EditorSceneManager.SaveScene(scene, ProjectSetup.DockingBayPath);
            BakeReflectionProbes(poweredOn);
            EditorSceneManager.SaveScene(scene, ProjectSetup.DockingBayPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[DockingBayBuilder] Built {ProjectSetup.DockingBayPath}");
        }

        // ---------------------------------------------------------------- materials & items


        // ---------------------------------------------------------------- architecture

        private const float HallHalfWidth = 5f, HallHeight = 4.6f, HallBack = -10f, HallFront = 6f;

        private static void BuildHall(Transform env, List<GameObject> on, List<GameObject> off)
        {
            var hall = Group("Hall", env).transform;
            float len = HallFront - HallBack, midZ = (HallFront + HallBack) * 0.5f;

            Box("Floor", hall, new Vector3(0, -0.1f, midZ), new Vector3(HallHalfWidth * 2, 0.2f, len), StationMaterials.Floor);
            Box("Ceiling", hall, new Vector3(0, HallHeight + 0.1f, midZ), new Vector3(HallHalfWidth * 2, 0.2f, len), StationMaterials.Ceiling);
            // floor inlay: terracotta runner with brass edges
            for (float z = HallBack + 0.6f; z < HallFront - 0.6f; z += 2f)
            {
                Box($"Floor_RunnerTile_{z:0}", hall, new Vector3(0, 0.006f, z + 0.95f), new Vector3(1.9f, 0.012f, 1.7f), StationMaterials.Terracotta, false);
                Box($"Floor_RunnerInlay_{z:0}", hall, new Vector3(0, 0.008f, z + 0.95f), new Vector3(0.5f, 0.014f, 0.5f), StationMaterials.SlateDark, false, Quaternion.Euler(0, 45, 0));
                Box($"Floor_RunnerBrass_{z:0}", hall, new Vector3(0, 0.007f, z), new Vector3(2.2f, 0.014f, 0.06f), StationMaterials.Brass, false);
            }
            Box("Floor_RunnerEdgeL", hall, new Vector3(-1.14f, 0.01f, midZ), new Vector3(0.07f, 0.02f, len - 0.6f), StationMaterials.Brass, false);
            Box("Floor_RunnerEdgeR", hall, new Vector3(1.14f, 0.01f, midZ), new Vector3(0.07f, 0.02f, len - 0.6f), StationMaterials.Brass, false);
            for (float z = HallBack + 1.5f; z < HallFront - 1f; z += 2f)
                Box($"Floor_Seam_{z:0}", hall, new Vector3(0, 0.004f, z), new Vector3(HallHalfWidth * 2 - 0.4f, 0.008f, 0.03f), StationMaterials.SlateDark, false);

            foreach (int side in new[] { -1, 1 })
            {
                string s = side < 0 ? "L" : "R";
                float x = side * (HallHalfWidth + 0.1f);
                Box($"Wall_{s}", hall, new Vector3(x, HallHeight * 0.5f, midZ), new Vector3(0.2f, HallHeight + 0.2f, len), StationMaterials.Wall);
                float inner = side * (HallHalfWidth - 0.02f);
                Box($"Wall_{s}_Dado", hall, new Vector3(inner, 0.55f, midZ), new Vector3(0.06f, 1.1f, len), StationMaterials.SlateDark, false);
                Box($"Wall_{s}_Band", hall, new Vector3(inner - side * 0.02f, 1.2f, midZ), new Vector3(0.06f, 0.22f, len), StationMaterials.Terracotta, false);
                Box($"Wall_{s}_BandTrim", hall, new Vector3(inner - side * 0.03f, 1.33f, midZ), new Vector3(0.05f, 0.04f, len), StationMaterials.Brass, false);
                Box($"Wall_{s}_Window", hall, new Vector3(inner, 3.35f, midZ), new Vector3(0.04f, 0.7f, len - 1f), StationMaterials.WindowGlow, false);
                Box($"Wall_{s}_WindowSillTrim", hall, new Vector3(inner - side * 0.03f, 2.97f, midZ), new Vector3(0.07f, 0.06f, len), StationMaterials.Brass, false);

                // brass ribs with rivets
                for (float z = HallBack + 1f; z <= HallFront - 1f; z += 2f)
                {
                    var rib = Box($"Wall_{s}_Rib_{z:0}", hall, new Vector3(inner - side * 0.08f, HallHeight * 0.5f, z), new Vector3(0.14f, HallHeight, 0.18f), StationMaterials.Brass, false);
                    for (float y = 0.6f; y < HallHeight; y += 0.9f)
                        Sphere($"Rivet_{y:0.0}", hall, new Vector3(inner - side * 0.16f, y, z), Vector3.one * 0.06f, StationMaterials.Brass);
                    _ = rib;
                }

                // floor-edge cyan guide strips (powered)
                var strip = Box($"Guide_{s}", hall, new Vector3(side * (HallHalfWidth - 0.18f), 0.025f, midZ), new Vector3(0.05f, 0.03f, len - 0.4f), StationMaterials.CyanStrip, false);
                on.Add(strip);
            }

            // ceiling beams
            for (float z = HallBack + 2f; z < HallFront; z += 4f)
                Box($"Ceiling_Beam_{z:0}", hall, new Vector3(0, HallHeight - 0.12f, z), new Vector3(HallHalfWidth * 2, 0.22f, 0.28f), StationMaterials.Brass, false);

            // front wall with door opening (2.4 wide x 3 high)
            float fz = HallFront + 0.1f;
            Box("Wall_Front_L", hall, new Vector3(-3.1f, HallHeight * 0.5f, fz), new Vector3(3.8f, HallHeight + 0.2f, 0.2f), StationMaterials.Wall);
            Box("Wall_Front_R", hall, new Vector3(3.1f, HallHeight * 0.5f, fz), new Vector3(3.8f, HallHeight + 0.2f, 0.2f), StationMaterials.Wall);
            Box("Wall_Front_Top", hall, new Vector3(0, 3.85f, fz), new Vector3(2.4f, 1.7f, 0.2f), StationMaterials.Wall);
            // the band stops at the door pillars (outer edge x = ±1.53) — it must never cross the opening
            const float bandGap = 1.53f, bandW = HallHalfWidth - bandGap;
            Box("Wall_Front_Band_L", hall, new Vector3(-(bandGap + bandW * 0.5f), 1.2f, fz - 0.12f), new Vector3(bandW, 0.22f, 0.04f), StationMaterials.Terracotta, false);
            Box("Wall_Front_Band_R", hall, new Vector3(bandGap + bandW * 0.5f, 1.2f, fz - 0.12f), new Vector3(bandW, 0.22f, 0.04f), StationMaterials.Terracotta, false);
            // back wall
            Box("Wall_Back", hall, new Vector3(0, HallHeight * 0.5f, HallBack - 0.1f), new Vector3(HallHalfWidth * 2, HallHeight + 0.2f, 0.2f), StationMaterials.Wall);

            // ceiling lamps: powered amber vs. dead fixtures; emergency red when unpowered
            int i = 0;
            foreach (float z in new[] { -7f, -3f, 1f, 4.5f })
            {
                var housing = Cylinder($"Lamp_{i}_Housing", hall, new Vector3(0, HallHeight - 0.3f, z), new Vector3(0.9f, 0.08f, 0.9f), StationMaterials.Brass, false);
                _ = housing;
                Cylinder($"Lamp_{i}_DiscOff", hall, new Vector3(0, HallHeight - 0.35f, z), new Vector3(0.75f, 0.02f, 0.75f), StationMaterials.LampOff, false);
                var lit = Cylinder($"Lamp_{i}_DiscOn", hall, new Vector3(0, HallHeight - 0.36f, z), new Vector3(0.76f, 0.02f, 0.76f), StationMaterials.AmberLamp, false, null, false);
                var light = PointLight($"Lamp_{i}_Light", hall, new Vector3(0, HallHeight - 0.6f, z), Amber, 5.2f, 7.5f,
                    i == 1 ? LightShadows.Soft : LightShadows.None); // performance: one shadowed point light per room
                on.Add(lit);
                on.Add(light.gameObject);
                i++;
            }
            // cool fill from the windows when powered (station systems awake)
            on.Add(PointLight("Fill_Teal_A", hall, new Vector3(-2.5f, 2.2f, -5f), Teal, 1.1f, 7f).gameObject);
            on.Add(PointLight("Fill_Teal_B", hall, new Vector3(2.5f, 2.2f, 3f), Teal, 1.1f, 7f).gameObject);

            int e = 0;
            foreach (var p in new[] { new Vector3(-4.85f, 2.45f, -6.5f), new Vector3(4.85f, 2.45f, -1.5f), new Vector3(-4.85f, 2.45f, 3.5f) })
            {
                var fixture = Box($"Emergency_{e}_Fixture", hall, p, new Vector3(0.12f, 0.18f, 0.5f), StationMaterials.SlateDark, false);
                _ = fixture;
                var glow = Box($"Emergency_{e}_Glow", hall, p + new Vector3(-Mathf.Sign(p.x) * 0.07f, 0, 0), new Vector3(0.03f, 0.1f, 0.4f), StationMaterials.RedLamp, false, null, false);
                var red = PointLight($"Emergency_{e}_Light", hall, p + new Vector3(-Mathf.Sign(p.x) * 0.5f, -0.3f, 0), EmergencyRed, 1.7f, 6f);
                off.Add(glow);
                off.Add(red.gameObject);
                e++;
            }
        }

        private static void BuildAirlock(Transform env)
        {
            // Hero piece: ring-shaped docking clamp around the circular airlock to the player's ship.
            var airlock = Group("Airlock_RingClamp", env, new Vector3(0, 2.25f, HallBack + 0.05f)).transform;
            Ring("OuterRing", airlock, Vector3.zero, 2.15f, 0.32f, 0.45f, 40, StationMaterials.Brass);
            Ring("InnerRing", airlock, new Vector3(0, 0, 0.08f), 1.7f, 0.2f, 0.35f, 36, StationMaterials.Terracotta);
            Ring("LipRing", airlock, new Vector3(0, 0, 0.15f), 1.5f, 0.08f, 0.2f, 32, StationMaterials.Brass);
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Cos(a) * 2.5f, Mathf.Sin(a) * 2.5f, 0.05f);
                Box($"ClampTooth_{i}", airlock, pos, new Vector3(0.5f, 0.28f, 0.6f), StationMaterials.Brass, false, Quaternion.Euler(0, 0, i * 45f));
                Cylinder($"ClampPiston_{i}", airlock, pos * 0.86f + new Vector3(0, 0, 0.25f), new Vector3(0.12f, 0.2f, 0.12f), StationMaterials.SlateDark, false, Quaternion.Euler(90, 0, 0));
            }
            Cylinder("Viewport_Glass", airlock, new Vector3(0, 0, 0.02f), new Vector3(3.0f, 0.03f, 3.0f), StationMaterials.GlassTeal, false, Quaternion.Euler(90, 0, 0));
            Cylinder("Viewport_Back", airlock, new Vector3(0, 0, -0.04f), new Vector3(3.1f, 0.02f, 3.1f), StationMaterials.WindowGlow, false, Quaternion.Euler(90, 0, 0));
            // floor step ring in front of the airlock
            Cylinder("Airlock_Step", env, new Vector3(0, 0.02f, HallBack + 1.1f), new Vector3(4.2f, 0.04f, 1.6f), StationMaterials.Brass, false);
        }

        private static void BuildCorridor(Transform env, List<GameObject> on)
        {
            var c = Group("Corridor_ToEngineering", env).transform;
            float z0 = HallFront + 0.2f, z1 = 14f, midZ = (z0 + z1) * 0.5f, len = z1 - z0, h = 3.2f;
            Box("Floor", c, new Vector3(0, -0.1f, midZ), new Vector3(3.2f, 0.2f, len), StationMaterials.Floor);
            Box("Ceiling", c, new Vector3(0, h + 0.1f, midZ), new Vector3(3.2f, 0.2f, len), StationMaterials.Ceiling);
            Box("Wall_L", c, new Vector3(-1.7f, h * 0.5f, midZ), new Vector3(0.2f, h, len), StationMaterials.Wall);
            Box("Wall_R", c, new Vector3(1.7f, h * 0.5f, midZ), new Vector3(0.2f, h, len), StationMaterials.Wall);
            Box("Wall_End", c, new Vector3(0, h * 0.5f, z1 + 0.1f), new Vector3(3.4f, h, 0.2f), StationMaterials.Wall);
            Box("Band_L", c, new Vector3(-1.58f, 1.2f, midZ), new Vector3(0.04f, 0.22f, len), StationMaterials.Terracotta, false);
            Box("Band_R", c, new Vector3(1.58f, 1.2f, midZ), new Vector3(0.04f, 0.22f, len), StationMaterials.Terracotta, false);
            for (float z = z0 + 1f; z < z1; z += 2.4f)
            {
                Ring($"Arch_{z:0}", c, new Vector3(0, 1.6f, z), 1.55f, 0.16f, 0.22f, 20, StationMaterials.Brass, 0f, 180f);
                Box($"ArchPostL_{z:0}", c, new Vector3(-1.55f, 0.8f, z), new Vector3(0.16f, 1.6f, 0.22f), StationMaterials.Brass, false);
                Box($"ArchPostR_{z:0}", c, new Vector3(1.55f, 0.8f, z), new Vector3(0.16f, 1.6f, 0.22f), StationMaterials.Brass, false);
            }
            // sealed engineering hatch (opens in Phase 3)
            Ring("Hatch_Ring", c, new Vector3(0, 1.45f, z1 - 0.02f), 1.15f, 0.22f, 0.2f, 32, StationMaterials.Brass);
            Cylinder("Hatch_Plate", c, new Vector3(0, 1.45f, z1 - 0.01f), new Vector3(2.1f, 0.04f, 2.1f), StationMaterials.Terracotta, false, Quaternion.Euler(90, 0, 0));
            Box("Hatch_Bar", c, new Vector3(0, 1.45f, z1 - 0.06f), new Vector3(2.0f, 0.18f, 0.06f), StationMaterials.Brass, false);
            var hatchGo = Group("Hatch_ToEngineering", c, new Vector3(0, 1.45f, z1 - 0.12f));
            var hatchCol = hatchGo.AddComponent<BoxCollider>();
            hatchCol.size = new Vector3(2.3f, 2.3f, 0.3f);
            var hatch = hatchGo.AddComponent<SectorHatch>();
            hatch.EditorConfigure("dock_hatch_engineering", "prompt.hatch.go", null, false,
                new ConditionSet { conditions = new List<Condition> { Condition.Powered(SectorIds.Docking, "prompt.door.no_power") } });
            hatch.EditorSetTarget(SceneIds.Engineering, "from_docking", "sector.engineering.name", SectorIds.Engineering);
            var back = Group("Spawn_FromEngineering", c, new Vector3(0, 0.05f, z1 - 1.6f));
            back.transform.localRotation = Quaternion.Euler(0, 180, 0);
            back.AddComponent<SpawnPoint>().EditorSetId("from_engineering");

            var sign = WorldText("Sign_Engineering", c, new Vector3(0, 2.92f, z1 - 0.05f), Quaternion.identity, "sector.engineering.name", 3.2f, SignalCyan);
            ((RectTransform)sign.transform).sizeDelta = new Vector2(2.7f, 0.42f); // corridor is 3.2 m wide, ceiling at 3.2 m
            on.Add(sign.gameObject);
            on.Add(PointLight("Corridor_Light_A", c, new Vector3(0, h - 0.4f, z0 + 2f), Amber, 3f, 6f).gameObject);
            on.Add(PointLight("Corridor_Light_B", c, new Vector3(0, h - 0.4f, z1 - 1.5f), Teal, 2.5f, 5f).gameObject);
            on.Add(Box("Corridor_Strip_L", c, new Vector3(-1.5f, 0.03f, midZ), new Vector3(0.04f, 0.03f, len), StationMaterials.CyanStrip, false));
            on.Add(Box("Corridor_Strip_R", c, new Vector3(1.5f, 0.03f, midZ), new Vector3(0.04f, 0.03f, len), StationMaterials.CyanStrip, false));
        }

        // ---------------------------------------------------------------- interactables

        private static void BuildBenchWithFlashlight(Transform props, ItemDefinition flashlightItem)
        {
            var bench = Group("Workbench", props, new Vector3(-4.2f, 0, -5.6f)).transform;
            if (PropModels.Place("prop_workbench", bench, Vector3.zero) != null)
                PropModels.Solid(bench, new Vector3(0, 0.46f, 0), new Vector3(1.1f, 0.92f, 2.4f));
            else
            {
                Box("Top", bench, new Vector3(0, 0.88f, 0), new Vector3(1.1f, 0.08f, 2.4f), StationMaterials.SlateDark);
                Box("TopTrim", bench, new Vector3(0, 0.84f, 0), new Vector3(1.14f, 0.03f, 2.44f), StationMaterials.Brass, false);
                foreach (var p in new[] { new Vector3(-0.45f, 0.42f, -1.1f), new Vector3(0.45f, 0.42f, -1.1f), new Vector3(-0.45f, 0.42f, 1.1f), new Vector3(0.45f, 0.42f, 1.1f) })
                    Box("Leg", bench, p, new Vector3(0.08f, 0.84f, 0.08f), StationMaterials.Brass);
            }
            Box("Toolbox", bench, new Vector3(0.1f, 1.04f, 0.75f), new Vector3(0.5f, 0.24f, 0.32f), StationMaterials.Terracotta);
            Box("Datapad", bench, new Vector3(0.15f, 0.93f, -0.7f), new Vector3(0.28f, 0.02f, 0.2f), StationMaterials.Rubber, false);
            Box("Datapad_Screen", bench, new Vector3(0.15f, 0.945f, -0.7f), new Vector3(0.24f, 0.005f, 0.16f), StationMaterials.CyanStrip, false);

            var pickupGo = Group("Pickup_Flashlight", props, new Vector3(-4.05f, 0.97f, -5.4f));
            var col = pickupGo.AddComponent<BoxCollider>();
            col.size = new Vector3(0.45f, 0.25f, 0.45f);
            Cylinder("Body", pickupGo.transform, Vector3.zero, new Vector3(0.08f, 0.14f, 0.08f), StationMaterials.Brass, false, Quaternion.Euler(0, 0, 90), false);
            Cylinder("Head", pickupGo.transform, new Vector3(0.16f, 0, 0), new Vector3(0.12f, 0.04f, 0.12f), StationMaterials.Brass, false, Quaternion.Euler(0, 0, 90), false);
            Cylinder("Lens", pickupGo.transform, new Vector3(0.2f, 0, 0), new Vector3(0.1f, 0.005f, 0.1f), StationMaterials.CyanStrip, false, Quaternion.Euler(0, 0, 90), false);
            Cylinder("Grip", pickupGo.transform, new Vector3(-0.06f, 0, 0), new Vector3(0.085f, 0.06f, 0.085f), StationMaterials.Rubber, false, Quaternion.Euler(0, 0, 90), false);
            // Jonah's jacket (name tape "LIND") and his recorder: LOG_DOCK_01 [critical]
            Box("Jacket", bench, new Vector3(-0.05f, 0.95f, 0.05f), new Vector3(0.7f, 0.06f, 0.55f), StationMaterials.Terracotta, false, Quaternion.Euler(0, 12, 3));
            Box("Jacket_NameTape", bench, new Vector3(0.1f, 0.985f, 0.05f), new Vector3(0.2f, 0.01f, 0.06f), StationMaterials.Ceramic, false, Quaternion.Euler(0, 12, 3));
            BuildRecorder(props, "Recorder_LogDock01", new Vector3(-4.1f, 0.95f, -5.05f), Quaternion.Euler(0, 70, 0), DockingBayContent.LogDock01);
            // little pool of light so the first pickup reads in the dark
            PointLight("Bench_Glow", props, new Vector3(-3.9f, 1.5f, -5.4f), TealLight, 0.9f, 2.4f);

            var pickup = pickupGo.AddComponent<PickupInteractable>();
            pickup.EditorConfigure("dock_flashlight", "prompt.pickup", flashlightItem.NameKey, true, null,
                new List<Effect> { Effect.Flag(FlashlightController.OwnedFlag) });
            pickup.EditorSetItem(flashlightItem);
        }

        private static void BuildCratesWithFuse(Transform props, ItemDefinition fuseItem)
        {
            var crates = Group("CrateStack", props, new Vector3(3.7f, 0, 0.3f)).transform;
            Crate(crates, new Vector3(0, 0.45f, 0), 0.9f, 0f);
            Crate(crates, new Vector3(-0.15f, 1.2f, 0.1f), 0.6f, 12f);
            Crate(crates, new Vector3(0.1f, 0.35f, 1.0f), 0.7f, -8f);
            // scattered debris telling something went wrong
            Box("FallenPanel", props, new Vector3(2.4f, 0.05f, 1.8f), new Vector3(1.2f, 0.05f, 0.7f), StationMaterials.Wall, true, Quaternion.Euler(0, 28, 4));
            Box("Cable_A", props, new Vector3(2.9f, 0.03f, -0.9f), new Vector3(0.05f, 0.05f, 2.2f), StationMaterials.Rubber, false, Quaternion.Euler(0, 35, 0));
            Box("Cable_B", props, new Vector3(3.4f, 0.03f, -1.6f), new Vector3(0.05f, 0.05f, 1.6f), StationMaterials.Rubber, false, Quaternion.Euler(0, -20, 0));

            var fuseGo = Group("Pickup_Fuse", props, new Vector3(3.6f, 1.58f, 0.42f));
            var col = fuseGo.AddComponent<BoxCollider>();
            col.size = new Vector3(0.4f, 0.22f, 0.4f);
            Cylinder("Ceramic", fuseGo.transform, Vector3.zero, new Vector3(0.09f, 0.11f, 0.09f), StationMaterials.Ceramic, false, Quaternion.Euler(0, 0, 90), false);
            Cylinder("CapA", fuseGo.transform, new Vector3(0.12f, 0, 0), new Vector3(0.1f, 0.025f, 0.1f), StationMaterials.Brass, false, Quaternion.Euler(0, 0, 90), false);
            Cylinder("CapB", fuseGo.transform, new Vector3(-0.12f, 0, 0), new Vector3(0.1f, 0.025f, 0.1f), StationMaterials.Brass, false, Quaternion.Euler(0, 0, 90), false);
            Box("Band", fuseGo.transform, Vector3.zero, new Vector3(0.05f, 0.1f, 0.1f), StationMaterials.Terracotta, false, null, false);

            var pickup = fuseGo.AddComponent<PickupInteractable>();
            pickup.EditorConfigure("dock_fuse_30a", "prompt.pickup", fuseItem.NameKey, true);
            pickup.EditorSetItem(fuseItem);
        }

        private static void Crate(Transform parent, Vector3 pos, float size, float yaw)
        {
            if (PropModels.Crate(parent, "Crate", pos, Vector3.one * size, yaw) != null) return;
            var crate = Box("Crate", parent, pos, Vector3.one * size, StationMaterials.SlateDark, true, Quaternion.Euler(0, yaw, 0));
            var t = crate.transform;
            // brass corner bands and a terracotta label (children inherit the crate's scale, so use unit-space sizes)
            Box("BandTop", t, new Vector3(0, 0.45f, 0), new Vector3(1.02f, 0.08f, 1.02f), StationMaterials.Brass, false);
            Box("BandBottom", t, new Vector3(0, -0.45f, 0), new Vector3(1.02f, 0.08f, 1.02f), StationMaterials.Brass, false);
            Box("Label", t, new Vector3(0, 0.05f, 0.505f), new Vector3(0.5f, 0.3f, 0.01f), StationMaterials.Terracotta, false);
        }

        private static void BuildFuseBox(Transform props, ItemDefinition fuseItem, List<GameObject> off)
        {
            var box = Group("FuseBox", props, new Vector3(4.82f, 1.45f, 3.6f));
            box.transform.localRotation = Quaternion.Euler(0, -90, 0); // faces into the hall (-X)
            var col = box.AddComponent<BoxCollider>();
            col.size = new Vector3(0.7f, 1.0f, 0.35f);
            var t = box.transform;
            if (PropModels.Place("prop_fuse_box", t, Vector3.zero) == null)
            {
                Box("Housing", t, Vector3.zero, new Vector3(0.62f, 0.92f, 0.22f), StationMaterials.SlateDark, false);
                Box("Frame_T", t, new Vector3(0, 0.47f, 0.06f), new Vector3(0.7f, 0.06f, 0.1f), StationMaterials.Brass, false);
                Box("Frame_B", t, new Vector3(0, -0.47f, 0.06f), new Vector3(0.7f, 0.06f, 0.1f), StationMaterials.Brass, false);
                Box("Frame_L", t, new Vector3(-0.33f, 0, 0.06f), new Vector3(0.06f, 0.98f, 0.1f), StationMaterials.Brass, false);
                Box("Frame_R", t, new Vector3(0.33f, 0, 0.06f), new Vector3(0.06f, 0.98f, 0.1f), StationMaterials.Brass, false);
                Box("Plaque", t, new Vector3(0, 0.33f, 0.115f), new Vector3(0.4f, 0.1f, 0.01f), StationMaterials.Terracotta, false);
                Box("Socket", t, new Vector3(0, 0, 0.115f), new Vector3(0.34f, 0.16f, 0.02f), StationMaterials.Rubber, false);
                Cylinder("Gauge", t, new Vector3(0.18f, -0.28f, 0.12f), new Vector3(0.12f, 0.01f, 0.12f), StationMaterials.Brass, false, Quaternion.Euler(90, 0, 0));
            }
            Box("Conduit", props, new Vector3(4.9f, 3.1f, 3.6f), new Vector3(0.12f, 2.4f, 0.12f), StationMaterials.Brass, false);

            var installed = Group("Installed", t, new Vector3(0, 0, 0.14f));
            Cylinder("Fuse", installed.transform, Vector3.zero, new Vector3(0.09f, 0.12f, 0.09f), StationMaterials.Ceramic, false, Quaternion.Euler(0, 0, 90), false);
            Box("Indicator", installed.transform, new Vector3(0, -0.15f, 0), new Vector3(0.3f, 0.03f, 0.01f), StationMaterials.CyanStrip, false, null, false);
            var empty = Group("Empty", t, new Vector3(0, 0, 0.14f));
            Box("Spark", empty.transform, new Vector3(0, -0.15f, 0), new Vector3(0.3f, 0.03f, 0.01f), StationMaterials.RedLamp, false, null, false);
            var sparkLight = PointLight("SparkLight", empty.transform, new Vector3(0, 0, 0.25f), EmergencyRed, 0.8f, 1.6f);
            off.Add(sparkLight.gameObject);

            var socket = box.AddComponent<ItemSocketInteractable>();
            socket.EditorConfigure("dock_fusebox", "prompt.inspect", "obj.fusebox.name", true, null,
                new List<Effect> { Effect.Flag(DockingBayContent.FuseInstalledFlag) });
            socket.EditorSetSocket(fuseItem, installed, empty);
            installed.SetActive(false);
        }

        private static void BuildInnerDoor(Transform props)
        {
            var doorRoot = Group("Door_DockInner", props, new Vector3(0, 0, HallFront + 0.1f));
            var t = doorRoot.transform;
            Box("Threshold", t, new Vector3(0, -0.1f, 0.05f), new Vector3(2.6f, 0.2f, 0.7f), StationMaterials.Brass);
            // brass pillars + arch on the hall face
            Box("Pillar_L", t, new Vector3(-1.38f, 1.5f, -0.16f), new Vector3(0.3f, 3.0f, 0.18f), StationMaterials.Brass);
            Box("Pillar_R", t, new Vector3(1.38f, 1.5f, -0.16f), new Vector3(0.3f, 3.0f, 0.18f), StationMaterials.Brass);
            Ring("Arch", t, new Vector3(0, 3.0f, -0.16f), 1.38f, 0.3f, 0.18f, 18, StationMaterials.Brass, 0f, 180f);
            Box("Lintel", t, new Vector3(0, 3.06f, -0.16f), new Vector3(2.5f, 0.12f, 0.18f), StationMaterials.Terracotta, false);
            var statusL = Box("Status_L", t, new Vector3(-1.38f, 1.6f, -0.26f), new Vector3(0.08f, 1.2f, 0.02f), StationMaterials.StatusLight, false, null, false);
            var statusR = Box("Status_R", t, new Vector3(1.38f, 1.6f, -0.26f), new Vector3(0.08f, 1.2f, 0.02f), StationMaterials.StatusLight, false, null, false);

            var left = Group("Panel_L", t, new Vector3(-0.6f, 1.5f, 0f));
            var right = Group("Panel_R", t, new Vector3(0.6f, 1.5f, 0f));
            foreach (var (panel, side) in new[] { (left, -1f), (right, 1f) })
            {
                var p = panel.transform;
                var slab = Box("Slab", p, Vector3.zero, new Vector3(1.2f, 3.0f, 0.16f), StationMaterials.Terracotta, true, null, false);
                _ = slab;
                Box("Inset", p, new Vector3(0, 0.2f, -0.085f), new Vector3(0.8f, 1.9f, 0.02f), StationMaterials.SlateDark, false, null, false);
                Box("Rail", p, new Vector3(-side * 0.57f, 0, -0.09f), new Vector3(0.06f, 3.0f, 0.03f), StationMaterials.Brass, false, null, false);
                Box("HandleBar", p, new Vector3(-side * 0.42f, 0, -0.11f), new Vector3(0.05f, 0.6f, 0.05f), StationMaterials.Brass, false, null, false);
            }

            var door = doorRoot.AddComponent<DoorController>();
            var reqs = new ConditionSet { conditions = new List<Condition> { Condition.Powered(SectorIds.Docking, "prompt.door.no_power") } };
            door.EditorConfigure("dock_inner", "prompt.door.open", null, false, reqs);
            door.EditorSetPanels(left.transform, right.transform, new[] { statusL.GetComponent<Renderer>(), statusR.GetComponent<Renderer>() });
        }

        /// <summary>
        /// Ending A: the service tunnel from the Reactor Core comes out next to the airlock, and the Tern can be
        /// boarded while the overload countdown runs (both only matter during the escape).
        /// </summary>
        private static void BuildEscapeExit(Transform root)
        {
            var spawn = Group("Spawn_FromServiceTunnel", root, new Vector3(2.6f, 0.05f, HallBack + 3.2f));
            spawn.transform.localRotation = Quaternion.Euler(0, 200f, 0);
            spawn.AddComponent<SpawnPoint>().EditorSetId("from_service_tunnel");
            var tunnel = Group("ServiceTunnelHatch", root, new Vector3(HallHalfWidth - 0.06f, 1.2f, HallBack + 3.2f));
            tunnel.transform.localRotation = Quaternion.Euler(0, 90, 0);
            Ring("Ring", tunnel.transform, Vector3.zero, 0.8f, 0.14f, 0.15f, 24, StationMaterials.Brass);
            Cylinder("Plate", tunnel.transform, new Vector3(0, 0, -0.02f), new Vector3(1.45f, 0.03f, 1.45f), StationMaterials.SlateDark, false, Quaternion.Euler(90, 0, 0));

            var board = Group("BoardTheTern", root, new Vector3(0, 1.6f, HallBack + 0.5f));
            var col = board.AddComponent<BoxCollider>();
            col.size = new Vector3(3.4f, 3.2f, 0.8f);
            var boarding = board.AddComponent<LastSignal.Endings.BoardShipInteractable>();
            boarding.EditorConfigure("dock_board_tern", "prompt.board", null, false);
            var marker = PointLight("BoardingLight", board.transform, new Vector3(0, 0.6f, 0.8f), SignalCyan, 2.4f, 4f);
            Group("BoardTheTern_Toggle", root).AddComponent<ConditionToggle>().EditorSet(
                new ConditionSet { conditions = { Condition.Flag(LastSignal.Endings.EscapeSequence.ActiveFlag) } }, new[] { board, marker.gameObject }, null);
        }

        internal static GameObject BuildRecorder(Transform parent, string name, Vector3 pos, Quaternion rot, LastSignal.Audio.AudioLogDefinition log,
            ConditionSet requirements = null)
        {
            var go = Group(name, parent, pos);
            go.transform.localRotation = rot;
            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(0.32f, 0.2f, 0.32f);
            col.center = new Vector3(0, 0.04f, 0);
            Box("Body", go.transform, new Vector3(0, 0.03f, 0), new Vector3(0.2f, 0.06f, 0.13f), StationMaterials.Brass, false, null, false);
            Box("Grille", go.transform, new Vector3(-0.03f, 0.062f, 0), new Vector3(0.1f, 0.005f, 0.1f), StationMaterials.Rubber, false, null, false);
            Cylinder("Reel", go.transform, new Vector3(0.06f, 0.07f, 0), new Vector3(0.05f, 0.01f, 0.05f), StationMaterials.Terracotta, false, null, false);
            var blink = Sphere("UncollectedLight", go.transform, new Vector3(0.08f, 0.065f, 0.05f), Vector3.one * 0.025f, StationMaterials.CyanStrip, false, false);
            var glow = PointLight("Glow", blink.transform, Vector3.zero, SignalCyan, 0.35f, 0.8f);
            _ = glow;
            var pickup = go.AddComponent<AudioLogPickup>();
            pickup.EditorConfigure(log.Id, "prompt.log.play", null, false, requirements);
            pickup.EditorSetLog(log, blink);
            return go;
        }

        private static void BuildBreaker(Transform props)
        {
            // Breaker B-2 next to the fuse box: works only once the fuse is installed.
            var root = Group("Breaker_B2", props, new Vector3(4.85f, 1.45f, 2.75f));
            root.transform.localRotation = Quaternion.Euler(0, -90, 0);
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(0.45f, 0.7f, 0.4f);
            col.center = new Vector3(0, 0, 0.12f);
            var t = root.transform;
            Box("Plate", t, Vector3.zero, new Vector3(0.36f, 0.6f, 0.06f), StationMaterials.SlateDark, false);
            Box("Frame", t, new Vector3(0, 0, 0.035f), new Vector3(0.4f, 0.64f, 0.02f), StationMaterials.Brass, false);
            Box("Label", t, new Vector3(0, 0.22f, 0.05f), new Vector3(0.2f, 0.07f, 0.01f), StationMaterials.Terracotta, false);
            var pivot = Group("HandlePivot", t, new Vector3(0, -0.05f, 0.06f));
            Box("Handle", pivot.transform, new Vector3(0, 0.12f, 0.03f), new Vector3(0.06f, 0.24f, 0.05f), StationMaterials.Brass, false, null, false);
            Box("Grip", pivot.transform, new Vector3(0, 0.25f, 0.05f), new Vector3(0.14f, 0.05f, 0.07f), StationMaterials.Rubber, false, null, false);
            var lever = root.AddComponent<LeverInteractable>();
            var reqs = new ConditionSet { conditions = new List<Condition> { Condition.Flag(DockingBayContent.FuseInstalledFlag, "prompt.breaker.no_fuse") } };
            lever.EditorConfigure("dock_breaker_b2", "prompt.breaker.close", null, true, reqs, new List<Effect> { Effect.Power(SectorIds.Docking) });
            lever.EditorSetHandle(pivot.transform);
            pivot.transform.localRotation = Quaternion.Euler(-40, 0, 0);
        }

        private static void BuildLanderConsole(Transform props, List<GameObject> on)
        {
            // Bay control console beside the empty Survey Lander cradle (front-left), plus Rhee's log LOG_DOCK_02.
            var cradle = Group("SurveyLanderCradle_Empty", props, new Vector3(-2.6f, 0, 3.4f)).transform;
            Box("RailL", cradle, new Vector3(-0.9f, 0.08f, 0), new Vector3(0.12f, 0.16f, 3.0f), StationMaterials.Brass);
            Box("RailR", cradle, new Vector3(0.9f, 0.08f, 0), new Vector3(0.12f, 0.16f, 3.0f), StationMaterials.Brass);
            Box("FrostMark", cradle, new Vector3(0, 0.005f, 0), new Vector3(1.6f, 0.01f, 2.6f), StationMaterials.GlassTeal, false);
            Cylinder("CutUmbilical", cradle, new Vector3(0.4f, 1.4f, 1.1f), new Vector3(0.06f, 1.4f, 0.06f), StationMaterials.Rubber, false, Quaternion.Euler(0, 0, 8));

            var console = Group("Console_BayControl", props, new Vector3(-4.45f, 0, 1.2f));
            console.transform.localRotation = Quaternion.Euler(0, -90, 0); // screen (local -Z) faces into the hall
            var t = console.transform;
            var col = console.AddComponent<BoxCollider>();
            col.size = new Vector3(1.3f, 1.4f, 0.8f);
            col.center = new Vector3(0, 0.7f, 0);
            GameObject glow;
            if (PropModels.Place("prop_terminal_kiosk", t, Vector3.zero) != null)
                glow = Box("ScreenGlow", t, new Vector3(0, 1.38f, -0.18f), new Vector3(0.88f, 0.53f, 0.005f), StationMaterials.CyanStrip, false, Quaternion.Euler(-10, 0, 0), false);
            else
            {
                Box("Base", t, new Vector3(0, 0.45f, 0), new Vector3(1.2f, 0.9f, 0.55f), StationMaterials.SlateDark, false);
                Box("Desk", t, new Vector3(0, 0.93f, 0.08f), new Vector3(1.3f, 0.06f, 0.7f), StationMaterials.Brass, false, Quaternion.Euler(-12, 0, 0));
                Box("Screen", t, new Vector3(0, 1.32f, -0.12f), new Vector3(0.9f, 0.55f, 0.05f), StationMaterials.SlateDark, false, Quaternion.Euler(-12, 0, 0));
                Box("ScreenFrame", t, new Vector3(0, 1.32f, -0.15f), new Vector3(0.98f, 0.63f, 0.03f), StationMaterials.Brass, false, Quaternion.Euler(-12, 0, 0));
                glow = Box("ScreenGlow", t, new Vector3(0, 1.32f, -0.09f), new Vector3(0.84f, 0.49f, 0.005f), StationMaterials.CyanStrip, false, Quaternion.Euler(-12, 0, 0), false);
            }
            var light = PointLight("ScreenLight", glow.transform, new Vector3(0, 0, 1.5f), SignalCyan, 0.6f, 2.2f);
            _ = light;
            var terminal = console.AddComponent<TerminalInteractable>();
            terminal.EditorConfigure("dock_console", "prompt.terminal.use", null, false);
            terminal.EditorSetTerminal(DockingBayContent.BayTerminal, glow);

            BuildRecorder(props, "Recorder_LogDock02", new Vector3(-4.2f, 0.98f, 2.05f), Quaternion.Euler(0, 100, 0), DockingBayContent.LogDock02);
        }

        private static void BuildEscapePodCradle(Transform props)
        {
            // Escape Pod 1 cradle (empty, umbilicals cut) — looking here makes CAIRN explain the evacuation (AI_DOCK_02)
            var c = Group("EscapePod1Cradle_Empty", props, new Vector3(2.3f, 0, 3.9f)).transform;
            Ring("CradleRing", c, new Vector3(0, 1.3f, 0), 1.2f, 0.2f, 0.25f, 28, StationMaterials.Brass);
            Box("Clamp_L", c, new Vector3(-1.25f, 0.6f, 0), new Vector3(0.2f, 1.2f, 0.4f), StationMaterials.SlateDark);
            Box("Clamp_R", c, new Vector3(1.25f, 0.6f, 0), new Vector3(0.2f, 1.2f, 0.4f), StationMaterials.SlateDark);
            Cylinder("CutUmbilical_A", c, new Vector3(-0.3f, 3.2f, 0.1f), new Vector3(0.07f, 1.1f, 0.07f), StationMaterials.Rubber, false, Quaternion.Euler(0, 0, -6));
            Cylinder("CutUmbilical_B", c, new Vector3(0.35f, 3.3f, -0.1f), new Vector3(0.07f, 0.9f, 0.07f), StationMaterials.Rubber, false, Quaternion.Euler(0, 0, 9));
            var label = WorldText("Label", c, new Vector3(0, 2.75f, -0.15f), Quaternion.identity, "dock.pod_label", 2.0f, EmergencyRed);
            label.characterSpacing = 6;

            var volume = Group("Trigger_AI_DOCK_02", c, new Vector3(0, 1f, -1.6f));
            var box = volume.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(3.6f, 2.2f, 2.6f);
            volume.AddComponent<LastSignal.Dialogue.DialogueTrigger>().EditorSet(DialogueContent.AiDock02, LastSignal.Dialogue.DialogueTriggerMode.OnEnterVolume, null,
                new ConditionSet { conditions = new List<Condition> { Condition.Flag(DialogueContent.AiDock01.PlayedKey) } }, 0.3f);
        }

        private static void BuildDialogueTriggers(Transform root)
        {
            var t = Group("DialogueTriggers", root).transform;
            Group("AI_DOCK_01", t).AddComponent<LastSignal.Dialogue.DialogueTrigger>()
                .EditorSet(DialogueContent.AiDock01, LastSignal.Dialogue.DialogueTriggerMode.OnFlag, WorldKeys.Power(SectorIds.Docking), null, 1.4f);
        }

        private const string InspectFolder = "Assets/_Project/Prefabs/Interactables/Inspect";

        private static void CreateInspectPrefabs(ItemDefinition flashlight, ItemDefinition fuse)
        {
            Directory.CreateDirectory(InspectFolder);
            var f = new GameObject("Inspect_Flashlight");
            Cylinder("Body", f.transform, Vector3.zero, new Vector3(0.08f, 0.14f, 0.08f), StationMaterials.Brass, false, Quaternion.Euler(0, 0, 90), false);
            Cylinder("Head", f.transform, new Vector3(0.16f, 0, 0), new Vector3(0.12f, 0.04f, 0.12f), StationMaterials.Brass, false, Quaternion.Euler(0, 0, 90), false);
            Cylinder("Lens", f.transform, new Vector3(0.2f, 0, 0), new Vector3(0.1f, 0.005f, 0.1f), StationMaterials.CyanStrip, false, Quaternion.Euler(0, 0, 90), false);
            Cylinder("Grip", f.transform, new Vector3(-0.06f, 0, 0), new Vector3(0.085f, 0.06f, 0.085f), StationMaterials.Rubber, false, Quaternion.Euler(0, 0, 90), false);
            DockingBayContent.SetInspectPrefab(flashlight, PrefabUtility.SaveAsPrefabAsset(f, $"{InspectFolder}/Inspect_Flashlight.prefab"));
            Object.DestroyImmediate(f);

            var u = new GameObject("Inspect_Fuse30A");
            Cylinder("Ceramic", u.transform, Vector3.zero, new Vector3(0.09f, 0.11f, 0.09f), StationMaterials.Ceramic, false, Quaternion.Euler(0, 0, 90), false);
            Cylinder("CapA", u.transform, new Vector3(0.12f, 0, 0), new Vector3(0.1f, 0.025f, 0.1f), StationMaterials.Brass, false, Quaternion.Euler(0, 0, 90), false);
            Cylinder("CapB", u.transform, new Vector3(-0.12f, 0, 0), new Vector3(0.1f, 0.025f, 0.1f), StationMaterials.Brass, false, Quaternion.Euler(0, 0, 90), false);
            Box("Band", u.transform, Vector3.zero, new Vector3(0.05f, 0.1f, 0.1f), StationMaterials.Terracotta, false, null, false);
            DockingBayContent.SetInspectPrefab(fuse, PrefabUtility.SaveAsPrefabAsset(u, $"{InspectFolder}/Inspect_Fuse30A.prefab"));
            Object.DestroyImmediate(u);
        }

        // ---------------------------------------------------------------- lighting & post

        internal static void SetupLightingAndPost(Transform root, bool dockingProbes = true)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(Navy, Teal, 0.15f) * 0.7f;
            RenderSettings.ambientEquatorColor = Color.Lerp(Navy, Terracotta, 0.08f) * 0.5f;
            RenderSettings.ambientGroundColor = Navy * 0.15f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Navy * 0.9f;
            RenderSettings.fogDensity = 0.035f;
            RenderSettings.skybox = null;

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumePath);
            }
            foreach (var c in profile.components.ToArray())
            {
                profile.Remove(c.GetType());
                Object.DestroyImmediate(c, true);
            }
            var bloom = AddComponent<Bloom>(profile);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.9f);
            bloom.scatter.Override(0.72f);
            bloom.tint.Override(new Color(1f, 0.92f, 0.85f));
            var tone = AddComponent<Tonemapping>(profile);
            tone.mode.Override(TonemappingMode.ACES);
            var grade = AddComponent<ColorAdjustments>(profile);
            grade.postExposure.Override(0.55f);
            grade.contrast.Override(12f);
            grade.saturation.Override(6f);
            var split = AddComponent<SplitToning>(profile);
            split.shadows.Override(new Color(0.42f, 0.5f, 0.55f));
            split.highlights.Override(new Color(0.8f, 0.66f, 0.5f));
            split.balance.Override(10f);
            var vignette = AddComponent<Vignette>(profile);
            vignette.intensity.Override(0.32f);
            vignette.smoothness.Override(0.45f);
            vignette.color.Override(Navy * 0.4f);
            var grain = AddComponent<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Thin1);
            grain.intensity.Override(0.18f);
            EditorUtility.SetDirty(profile);

            if (dockingProbes) AddDockingProbes(root);

            var volumeGo = Group("GlobalVolume", root);
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        private static void AddDockingProbes(Transform root)
        {
            var probeGo = Group("ReflectionProbe_Hall", root, new Vector3(0, 2.2f, -2f));
            var probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked;
            probe.size = new Vector3(10.4f, 4.8f, 16.4f);
            probe.boxProjection = true;
            probe.resolution = 256;
            probe.intensity = 0.9f;
            probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
            probe.backgroundColor = Navy * 0.4f;
            var probe2 = Group("ReflectionProbe_Corridor", root, new Vector3(0, 1.6f, 10f)).AddComponent<ReflectionProbe>();
            probe2.mode = ReflectionProbeMode.Baked;
            probe2.size = new Vector3(3.6f, 3.4f, 8f);
            probe2.boxProjection = true;
            probe2.resolution = 128;
            probe2.clearFlags = ReflectionProbeClearFlags.SolidColor;
            probe2.backgroundColor = Navy * 0.4f;
        }

        /// <summary>Bakes probes with the station powered (the state players will look at most) and stores them next to the scene.</summary>
        internal static void BakeReflectionProbes(List<GameObject> poweredOn, string scenePath = null)
        {
            scenePath ??= ProjectSetup.DockingBayPath;
            foreach (var go in poweredOn) go.SetActive(true);
            var folder = Path.Combine(Path.GetDirectoryName(scenePath)!, Path.GetFileNameWithoutExtension(scenePath) + "_Probes");
            Directory.CreateDirectory(folder);
            foreach (var probe in Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None))
            {
                var path = $"{folder}/{probe.name}.exr";
                if (!Lightmapping.BakeReflectionProbe(probe, path))
                    Debug.LogWarning($"[DockingBayBuilder] Could not bake {probe.name}");
                probe.bakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(path);
            }
            foreach (var go in poweredOn) go.SetActive(false);
        }

        private static T AddComponent<T>(VolumeProfile profile) where T : VolumeComponent
        {
            var c = profile.Add<T>(true);
            c.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(c, profile);
            return c;
        }
    }
}
