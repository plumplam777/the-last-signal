using System.Collections.Generic;
using System.IO;
using LastSignal.Core;
using LastSignal.Interaction;
using LastSignal.Player;
using LastSignal.Puzzles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using static LastSignal.EditorTools.BuilderKit;
using M = LastSignal.EditorTools.StationMaterials;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Code-built greybox of Engineering (StoryBible §6.2) in the Brass &amp; Signal style, warm-orange industrial variant.
    /// Flow: replace the burnt power coupling → start the generator in Kaye's order → route 3 cables (Medical,
    /// Life support, Lighting; Array feed is the temptation) → Medical corridor door powers.
    /// Overwrites Sector_Engineering.unity.
    /// </summary>
    public static class EngineeringBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Station/Sector_Engineering.unity";
        private const float HalfW = 6f, HalfL = 7f, H = 5f;

        private static Material _orangeLamp, _rust, _redTag;

        [MenuItem("Last Signal/Build Content/Engineering (overwrites scene)")]
        public static void Build()
        {
            StationMaterials.Create();
            _orangeLamp = Emissive("M_Emissive_IndustrialOrange", Hex("#FF8F3F"), 1.8f, Hex("#FF8F3F"));
            _rust = Lit("M_Metal_Rust", Color.Lerp(Hex("#6B3A24"), Navy, 0.35f), 0.25f, 0.4f);
            _redTag = Emissive("M_Tag_Red", EmergencyRed, 0.25f, EmergencyRed);
            DockingBayContent.CreateAll();
            var playerPrefab = PlayerPrefabBuilder.Build();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("[Generated] Sector_Engineering");
            root.AddComponent<SectorRoot>().EditorConfigure(SectorIds.Engineering, playerPrefab);
            var on = new List<GameObject>();
            var off = new List<GameObject>();

            var env = Group("Environment", root.transform).transform;
            BuildRoom(env, on, off);
            BuildMedicalCorridor(env, on);
            var props = Group("Props", root.transform).transform;
            BuildGenerator(props, off);
            BuildStartPanel(props);
            BuildRoutingBoard(props);
            BuildWorkbench(props);
            BuildInterlockPanel(props);
            BuildStatusDesk(props);
            BuildHatches(props, root.transform);
            BuildDialogueTriggers(root.transform);

            var power = Group("PowerController", root.transform);
            power.AddComponent<PoweredObject>().EditorConfigure(SectorIds.Engineering, on.ToArray(), off.ToArray());
            foreach (var go in on) go.SetActive(false);

            DockingBayBuilder.SetupLightingAndPost(root.transform, dockingProbes: false);
            root.AddComponent<LastSignal.Audio.SectorAmbience>().EditorSet(new List<LastSignal.Audio.AmbienceLoop>
            {
                new() { clip = "amb_station_hum", volume = 0.45f },
                new() { clip = "amb_generator", volume = 0.6f, whileFlag = EngineeringContent.GeneratorRunningFlag },
                new() { clip = "amb_ventilation", volume = 0.3f, whileFlag = WorldKeys.Power(SectorIds.Engineering) },
            });
            // warmer, more industrial ambience than the bay
            RenderSettings.ambientSkyColor = Color.Lerp(Navy, Hex("#FF8F3F"), 0.08f) * 0.6f;
            RenderSettings.ambientEquatorColor = Color.Lerp(Navy, Terracotta, 0.15f) * 0.5f;
            RenderSettings.fogColor = Color.Lerp(Navy, Hex("#3A2418"), 0.4f);
            RenderSettings.fogDensity = 0.03f;
            var probe = Group("ReflectionProbe_Room", root.transform, new Vector3(0, 2.4f, 0)).AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked;
            probe.size = new Vector3(12.4f, 5.2f, 14.4f);
            probe.boxProjection = true;
            probe.resolution = 256;
            probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
            probe.backgroundColor = Navy * 0.4f;

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)!);
            EditorSceneManager.SaveScene(scene, ScenePath);
            DockingBayBuilder.BakeReflectionProbes(on, ScenePath);
            EditorSceneManager.SaveScene(scene, ScenePath);
            ProjectSetup.RegisterBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log($"[EngineeringBuilder] Built {ScenePath}");
        }

        // ------------------------------------------------------------------ room

        private static void BuildRoom(Transform env, List<GameObject> on, List<GameObject> off)
        {
            var r = Group("Room", env).transform;
            Box("Floor", r, new Vector3(0, -0.1f, 0), new Vector3(HalfW * 2, 0.2f, HalfL * 2), M.Floor);
            Box("Ceiling", r, new Vector3(0, H + 0.1f, 0), new Vector3(HalfW * 2, 0.2f, HalfL * 2), M.Ceiling);
            // grated walkway around the generator
            Box("Grate", r, new Vector3(1f, 0.01f, 1.5f), new Vector3(5.2f, 0.02f, 5.2f), _rust, false);
            Box("Wall_W", r, new Vector3(-HalfW - 0.1f, H / 2, 0), new Vector3(0.2f, H + 0.2f, HalfL * 2), M.Wall);
            Box("Wall_E", r, new Vector3(HalfW + 0.1f, H / 2, 0), new Vector3(0.2f, H + 0.2f, HalfL * 2), M.Wall);
            Box("Wall_S", r, new Vector3(0, H / 2, -HalfL - 0.1f), new Vector3(HalfW * 2, H + 0.2f, 0.2f), M.Wall);
            // north wall with an opening for the Medical corridor door (x -4.7..-2.3, height 3)
            Box("Wall_N_L", r, new Vector3(-5.35f, H / 2, HalfL + 0.1f), new Vector3(1.3f, H + 0.2f, 0.2f), M.Wall);
            Box("Wall_N_R", r, new Vector3(1.85f, H / 2, HalfL + 0.1f), new Vector3(8.3f, H + 0.2f, 0.2f), M.Wall);
            Box("Wall_N_Top", r, new Vector3(-3.5f, 4.0f, HalfL + 0.1f), new Vector3(2.4f, 2.0f, 0.2f), M.Wall);

            foreach (int side in new[] { -1, 1 })
            {
                float x = side * (HalfW - 0.03f);
                Box($"Band_{side}", r, new Vector3(x, 1.2f, 0), new Vector3(0.05f, 0.25f, HalfL * 2), M.Terracotta, false);
                for (float z = -HalfL + 1f; z < HalfL; z += 2.5f)
                    Box($"Rib_{side}_{z:0}", r, new Vector3(x - side * 0.07f, H / 2, z), new Vector3(0.14f, H, 0.2f), M.Brass, false);
                // overhead pipes
                Cylinder($"Pipe_{side}", r, new Vector3(side * 4.2f, H - 0.4f, 0), new Vector3(0.28f, HalfL, 0.28f), M.Brass, false, Quaternion.Euler(90, 0, 0));
                Cylinder($"PipeThin_{side}", r, new Vector3(side * 3.6f, H - 0.3f, 0), new Vector3(0.14f, HalfL, 0.14f), _rust, false, Quaternion.Euler(90, 0, 0));
            }

            int i = 0;
            foreach (var p in new[] { new Vector3(-3f, H - 0.5f, -4f), new Vector3(3f, H - 0.5f, -4f), new Vector3(-3f, H - 0.5f, 3.5f), new Vector3(4f, H - 0.5f, 4f) })
            {
                Box($"Lamp_{i}_Cage", r, p + Vector3.up * 0.15f, new Vector3(0.7f, 0.12f, 0.35f), M.Brass, false);
                Box($"Lamp_{i}_Off", r, p, new Vector3(0.6f, 0.06f, 0.28f), M.LampOff, false);
                on.Add(Box($"Lamp_{i}_On", r, p + Vector3.down * 0.01f, new Vector3(0.61f, 0.06f, 0.29f), _orangeLamp, false, null, false));
                on.Add(PointLight($"Lamp_{i}_Light", r, p + Vector3.down * 0.3f, Hex("#FF9A4D"), 5.5f, 8.5f, i < 2 ? LightShadows.Soft : LightShadows.None).gameObject);
                i++;
            }
            on.Add(PointLight("Fill_Teal", r, new Vector3(3.5f, 2.5f, 5.5f), Teal, 1.4f, 7f).gameObject);
            foreach (var p in new[] { new Vector3(-5.8f, 2.6f, -5f), new Vector3(5.8f, 2.6f, 2f) })
            {
                Box($"Emergency_{p.x:0}", r, p, new Vector3(0.1f, 0.15f, 0.45f), M.SlateDark, false);
                off.Add(Box($"EmergencyGlow_{p.x:0}", r, p + new Vector3(-Mathf.Sign(p.x) * 0.06f, 0, 0), new Vector3(0.02f, 0.08f, 0.38f), M.RedLamp, false, null, false));
                off.Add(PointLight($"EmergencyLight_{p.x:0}", r, p + new Vector3(-Mathf.Sign(p.x) * 0.6f, -0.3f, 0), EmergencyRed, 1.8f, 7f).gameObject);
            }

            // battery monitor display on the north wall
            WorldText("BatteryDisplay", r, new Vector3(-0.3f, 3.9f, HalfL - 0.02f), Quaternion.identity, "eng.battery_display", 3.2f, Hex("#FFB36B"));
        }

        private static void BuildMedicalCorridor(Transform env, List<GameObject> on)
        {
            var c = Group("Corridor_ToMedical", env).transform;
            float z0 = HalfL + 0.2f, z1 = 11.5f, mid = (z0 + z1) / 2, len = z1 - z0, x = -3.5f;
            Box("Floor", c, new Vector3(x, -0.1f, mid), new Vector3(3.2f, 0.2f, len), M.Floor);
            Box("Ceiling", c, new Vector3(x, 3.3f, mid), new Vector3(3.2f, 0.2f, len), M.Ceiling);
            Box("Wall_L", c, new Vector3(x - 1.7f, 1.6f, mid), new Vector3(0.2f, 3.2f, len), M.Wall);
            Box("Wall_R", c, new Vector3(x + 1.7f, 1.6f, mid), new Vector3(0.2f, 3.2f, len), M.Wall);
            Box("Wall_End", c, new Vector3(x, 1.6f, z1 + 0.1f), new Vector3(3.4f, 3.2f, 0.2f), M.Wall);
            Box("Band_L", c, new Vector3(x - 1.58f, 1.2f, mid), new Vector3(0.04f, 0.22f, len), M.Ceramic, false);
            on.Add(PointLight("Light", c, new Vector3(x, 2.8f, mid), TealLight, 2.4f, 6f).gameObject);
            on.Add(WorldText("Sign_Medical", c, new Vector3(x, 2.7f, z1 - 0.05f), Quaternion.identity, "sector.medical.name", 3f, TealLight).gameObject);

            // Medical hatch: the sector is built in a later phase, so it explains that honestly
            var hatchGo = Group("Hatch_ToMedical", c, new Vector3(x, 1.45f, z1 - 0.12f));
            Ring("Ring", hatchGo.transform, Vector3.zero, 1.1f, 0.2f, 0.2f, 28, M.Brass);
            Cylinder("Plate", hatchGo.transform, new Vector3(0, 0, 0.03f), new Vector3(2.0f, 0.04f, 2.0f), M.Ceramic, false, Quaternion.Euler(90, 0, 0));
            hatchGo.AddComponent<BoxCollider>().size = new Vector3(2.2f, 2.2f, 0.3f);
            var hatch = hatchGo.AddComponent<SectorHatch>();
            hatch.EditorConfigure("eng_hatch_medical", "prompt.hatch.go", null, false,
                new ConditionSet { conditions = new List<Condition> { Condition.Flag(WorldKeys.Story("dev.medical_built"), "dev.sector_unavailable") } });
            hatch.EditorSetTarget(SceneIds.Medical, "from_engineering", "sector.medical.name", SectorIds.Medical);

            // the door into the corridor, powered by routing
            var doorRoot = Group("Door_MedicalCorridor", env, new Vector3(-3.5f, 0, HalfL + 0.1f));
            var t = doorRoot.transform;
            Box("Threshold", t, new Vector3(0, -0.1f, 0.05f), new Vector3(2.6f, 0.2f, 0.7f), M.Brass);
            Box("Pillar_L", t, new Vector3(-1.38f, 1.5f, -0.16f), new Vector3(0.3f, 3f, 0.18f), M.Brass);
            Box("Pillar_R", t, new Vector3(1.38f, 1.5f, -0.16f), new Vector3(0.3f, 3f, 0.18f), M.Brass);
            var sL = Box("Status_L", t, new Vector3(-1.38f, 1.6f, -0.26f), new Vector3(0.08f, 1.2f, 0.02f), M.StatusLight, false, null, false);
            var sR = Box("Status_R", t, new Vector3(1.38f, 1.6f, -0.26f), new Vector3(0.08f, 1.2f, 0.02f), M.StatusLight, false, null, false);
            var left = Group("Panel_L", t, new Vector3(-0.6f, 1.5f, 0));
            var right = Group("Panel_R", t, new Vector3(0.6f, 1.5f, 0));
            Box("Slab", left.transform, Vector3.zero, new Vector3(1.2f, 3f, 0.16f), M.Ceramic, true, null, false);
            Box("Slab", right.transform, Vector3.zero, new Vector3(1.2f, 3f, 0.16f), M.Ceramic, true, null, false);
            Box("Cross", left.transform, new Vector3(0.25f, 0.6f, -0.09f), new Vector3(0.5f, 0.12f, 0.02f), M.RedLamp, false, null, false);
            Box("Cross", right.transform, new Vector3(-0.25f, 0.6f, -0.09f), new Vector3(0.5f, 0.12f, 0.02f), M.RedLamp, false, null, false);
            var door = doorRoot.AddComponent<DoorController>();
            door.EditorConfigure("eng_medical_corridor", "prompt.door.open", null, false,
                new ConditionSet { conditions = new List<Condition> { Condition.Powered(SectorIds.Medical, "eng.door.medical_no_power") } });
            door.EditorSetPanels(left.transform, right.transform, new[] { sL.GetComponent<Renderer>(), sR.GetComponent<Renderer>() });
        }

        // ------------------------------------------------------------------ puzzles

        private static void BuildGenerator(Transform props, List<GameObject> off)
        {
            var g = Group("BackupGenerator", props, new Vector3(1f, 0, 2f)).transform;
            Cylinder("Plinth", g, new Vector3(0, 0.15f, 0), new Vector3(3.3f, 0.15f, 3.3f), M.SlateDark);
            Cylinder("Housing", g, new Vector3(0, 1.45f, 0), new Vector3(2.7f, 1.2f, 2.7f), M.Terracotta);
            foreach (var y in new[] { 0.45f, 1.45f, 2.5f })
                Cylinder($"Band_{y:0.0}", g, new Vector3(0, y, 0), new Vector3(2.82f, 0.07f, 2.82f), M.Brass, false);
            Sphere("Dome", g, new Vector3(0, 2.65f, 0), new Vector3(2.4f, 0.9f, 2.4f), M.Brass);
            Cylinder("Exhaust_L", g, new Vector3(-0.8f, 3.8f, 0.4f), new Vector3(0.3f, 1.2f, 0.3f), _rust, false);
            Cylinder("Exhaust_R", g, new Vector3(0.8f, 3.8f, 0.4f), new Vector3(0.3f, 1.2f, 0.3f), _rust, false);
            Box("FrontPanel", g, new Vector3(0, 1.1f, -1.33f), new Vector3(1.0f, 0.9f, 0.1f), M.SlateDark, false);

            // governor (spins once the generator runs)
            var gov = Group("Governor", g, new Vector3(0, 3.15f, 0));
            Cylinder("Shaft", gov.transform, new Vector3(0, 0.35f, 0), new Vector3(0.08f, 0.4f, 0.08f), M.Brass, false, null, false);
            foreach (int s in new[] { -1, 1 })
            {
                Box($"Arm_{s}", gov.transform, new Vector3(s * 0.28f, 0.55f, 0), new Vector3(0.5f, 0.04f, 0.04f), M.Brass, false, Quaternion.Euler(0, 0, s * -25f), false);
                Sphere($"Ball_{s}", gov.transform, new Vector3(s * 0.5f, 0.45f, 0), Vector3.one * 0.2f, M.Brass, false, false);
            }
            gov.AddComponent<FlagSpinner>().EditorSet(EngineeringContent.GeneratorRunningFlag, Vector3.up, 260f);

            // coupling socket on the front panel
            var socketGo = Group("CouplingSocket", g, new Vector3(0, 1.1f, -1.42f));
            socketGo.AddComponent<BoxCollider>().size = new Vector3(0.9f, 0.8f, 0.3f);
            var installed = Group("Installed", socketGo.transform);
            Cylinder("Coupling", installed.transform, Vector3.zero, new Vector3(0.18f, 0.22f, 0.18f), M.Brass, false, Quaternion.Euler(0, 0, 90), false);
            Cylinder("Insulator", installed.transform, Vector3.zero, new Vector3(0.22f, 0.06f, 0.22f), M.Ceramic, false, Quaternion.Euler(0, 0, 90), false);
            var empty = Group("Burnt", socketGo.transform);
            Cylinder("Burnt", empty.transform, Vector3.zero, new Vector3(0.18f, 0.22f, 0.18f), M.Rubber, false, Quaternion.Euler(0, 0, 90), false);
            Sphere("Spark", empty.transform, new Vector3(0.18f, 0.05f, -0.05f), Vector3.one * 0.04f, M.RedLamp, false, false);
            off.Add(PointLight("SparkLight", empty.transform, new Vector3(0, 0, -0.3f), Hex("#FF8F3F"), 0.9f, 2f).gameObject);
            var socket = socketGo.AddComponent<ItemSocketInteractable>();
            socket.EditorConfigure("eng_coupling_socket", "prompt.inspect", "obj.generator.name", true, null,
                new List<Effect> { Effect.Flag(EngineeringContent.CouplingInstalledFlag) });
            socket.EditorSetSocket(EngineeringContent.Coupling, installed, empty, "eng.coupling.missing");
            installed.SetActive(false);
        }

        private static void BuildStartPanel(Transform props)
        {
            var panel = Group("GeneratorStartPanel", props, new Vector3(3.6f, 0, -0.6f));
            // front (local -Z) faces the player coming from the south
            var t = panel.transform;
            Box("Pedestal", t, new Vector3(0, 0.5f, 0), new Vector3(1.6f, 1.0f, 0.5f), M.SlateDark);
            Box("Desk", t, new Vector3(0, 1.05f, -0.05f), new Vector3(1.7f, 0.08f, 0.6f), M.Brass, false, Quaternion.Euler(-15, 0, 0));
            WorldText("Title", t, new Vector3(0, 1.45f, 0.26f), Quaternion.identity, "eng.panel.title", 2.2f, Brass);

            var puzzle = panel.AddComponent<SequencePuzzle>();
            puzzle.EditorConfigurePuzzle(EngineeringContent.GeneratorPuzzleId, SectorIds.Engineering,
                new List<Effect> { Effect.Power(SectorIds.Engineering), Effect.Flag(EngineeringContent.GeneratorRunningFlag) });
            puzzle.EditorSetOrder(new List<string> { "coolant", "fuel", "ignition", "load" });
            puzzle.EditorSetSolvedSfx("sfx_generator_start");

            // physical layout deliberately differs from the correct order: read Kaye's diagram
            var layout = new[] { "ignition", "coolant", "load", "fuel" };
            for (int i = 0; i < layout.Length; i++)
            {
                var step = layout[i];
                float x = -0.6f + i * 0.4f;
                var sw = Group($"Switch_{step}", t, new Vector3(x, 1.12f, -0.08f));
                sw.transform.localRotation = Quaternion.Euler(-15, 0, 0);
                sw.AddComponent<BoxCollider>().size = new Vector3(0.32f, 0.25f, 0.4f);
                Box("Base", sw.transform, Vector3.zero, new Vector3(0.22f, 0.04f, 0.22f), M.SlateDark, false, null, false);
                Box("Toggle", sw.transform, new Vector3(0, 0.08f, 0), new Vector3(0.05f, 0.14f, 0.05f), M.Brass, false, Quaternion.Euler(20, 0, 0), false);
                var lamp = Sphere("Lamp", sw.transform, new Vector3(0, 0.05f, 0.14f), Vector3.one * 0.05f, M.CyanStrip, false, false);
                var label = WorldText("Label", sw.transform, new Vector3(0, 0.03f, -0.16f), Quaternion.Euler(90, 0, 0), $"eng.step.{step}", 0.9f, new Color(0.9f, 0.95f, 1f));
                ((RectTransform)label.transform).sizeDelta = new Vector2(0.4f, 0.12f);
                label.characterSpacing = 0;
                var interact = sw.AddComponent<SequenceStepInteractable>();
                interact.EditorConfigure($"eng_switch_{step}", "prompt.switch", $"eng.step.{step}", false,
                    new ConditionSet { conditions = new List<Condition> { Condition.Flag(EngineeringContent.CouplingInstalledFlag, "eng.panel.no_coupling") } });
                interact.EditorSetStep(puzzle, step, lamp);
                lamp.SetActive(false);
            }
        }

        private static void BuildRoutingBoard(Transform props)
        {
            var b = Group("RoutingBoard", props, new Vector3(3.8f, 0, HalfL - 0.05f));
            var t = b.transform;
            Box("Plate", t, new Vector3(0, 1.7f, 0), new Vector3(2.6f, 1.6f, 0.08f), M.SlateDark, false);
            Box("Frame", t, new Vector3(0, 1.7f, 0.03f), new Vector3(2.74f, 1.74f, 0.04f), M.Brass, false);
            WorldText("Title", t, new Vector3(0, 2.75f, -0.06f), Quaternion.identity, "eng.board.title", 2.2f, Brass);
            Box("Bus", t, new Vector3(0, 2.3f, -0.06f), new Vector3(2.2f, 0.08f, 0.04f), M.Brass, false);

            var board = b.AddComponent<RoutingBoard>();
            board.EditorConfigurePuzzle(EngineeringContent.RoutingPuzzleId, SectorIds.Engineering,
                new List<Effect> { Effect.Power(SectorIds.Medical), Effect.Flag(WorldKeys.Story("eng_routed")) });
            var sockets = new List<RoutingSocket>
            {
                new() { id = "medical", labelKey = "eng.socket.medical", required = true },
                new() { id = "life_support", labelKey = "eng.socket.life_support", required = true },
                new() { id = "lighting", labelKey = "eng.socket.lighting", required = true },
                new() { id = "array_feed", labelKey = "eng.socket.array_feed", required = false },
            };
            board.EditorSetSockets(3, sockets);

            for (int i = 0; i < sockets.Count; i++)
            {
                var s = sockets[i];
                float x = -0.9f + i * 0.6f;
                bool array = s.id == "array_feed";
                var go = Group($"Socket_{s.id}", t, new Vector3(x, 1.6f, -0.08f));
                go.AddComponent<BoxCollider>().size = new Vector3(0.45f, 0.9f, 0.3f);
                Cylinder("Port", go.transform, Vector3.zero, new Vector3(0.18f, 0.03f, 0.18f), array ? M.RedLamp : M.Brass, false, Quaternion.Euler(90, 0, 0), false);
                var label = WorldText("Label", go.transform, new Vector3(0, 0.32f, -0.02f), Quaternion.identity, s.labelKey, 0.9f, array ? new Color(1f, 0.55f, 0.5f) : new Color(0.9f, 0.95f, 1f));
                ((RectTransform)label.transform).sizeDelta = new Vector2(0.55f, 0.2f);
                label.characterSpacing = 0;
                var cable = Group("Cable", go.transform);
                Cylinder("Plug", cable.transform, new Vector3(0, 0, -0.06f), new Vector3(0.14f, 0.06f, 0.14f), M.Brass, false, Quaternion.Euler(90, 0, 0), false);
                Cylinder("Lead", cable.transform, new Vector3(0, -0.45f, -0.1f), new Vector3(0.06f, 0.42f, 0.06f), M.Rubber, false, null, false);
                Cylinder("ToBus", cable.transform, new Vector3(0, 0.35f, -0.1f), new Vector3(0.05f, 0.33f, 0.05f), M.Rubber, false, null, false);
                var live = Sphere("Live", go.transform, new Vector3(0, -0.3f, -0.03f), Vector3.one * 0.06f, M.CyanStrip, false, false);
                var socket = go.AddComponent<RoutingSocketInteractable>();
                socket.EditorConfigure($"eng_socket_{s.id}", "prompt.routing.plug", null, false,
                    new ConditionSet { conditions = new List<Condition> { Condition.Flag(EngineeringContent.GeneratorRunningFlag, "eng.routing.no_power") } });
                socket.EditorSet(board, s.id, cable, live);
                cable.SetActive(false);
                live.SetActive(false);
            }
        }

        // ------------------------------------------------------------------ story props

        private static void BuildWorkbench(Transform props)
        {
            var bench = Group("KayeWorkbench", props, new Vector3(-5.3f, 0, -2.5f)).transform;
            Box("Top", bench, new Vector3(0, 0.9f, 0), new Vector3(1.1f, 0.08f, 2.6f), M.SlateDark);
            Box("Trim", bench, new Vector3(0, 0.86f, 0), new Vector3(1.14f, 0.03f, 2.64f), M.Brass, false);
            Box("Cabinet", bench, new Vector3(0, 0.42f, 0), new Vector3(1.0f, 0.84f, 2.5f), _rust);
            // tools laid out neatly
            for (int i = 0; i < 4; i++)
                Box($"Tool_{i}", bench, new Vector3(0.1f, 0.95f, -0.9f + i * 0.18f), new Vector3(0.4f, 0.02f, 0.05f), i % 2 == 0 ? M.Brass : M.Rubber, false);
            Box("DogPhoto", bench, new Vector3(-0.3f, 1.05f, 0.9f), new Vector3(0.03f, 0.18f, 0.24f), M.Ceramic, false, Quaternion.Euler(0, 0, 10));

            // taped diagram on the wall above the bench (readable note)
            var note = Group("KayeDiagram", props, new Vector3(-5.93f, 1.7f, -2.5f));
            note.transform.localRotation = Quaternion.Euler(0, -90, 0); // local -Z faces into the room (+X)
            note.AddComponent<BoxCollider>().size = new Vector3(0.8f, 0.6f, 0.1f);
            Box("Paper", note.transform, Vector3.zero, new Vector3(0.6f, 0.45f, 0.01f), M.Ceramic, false, Quaternion.Euler(0, 0, 3), false);
            Box("TapeA", note.transform, new Vector3(-0.25f, 0.22f, -0.01f), new Vector3(0.12f, 0.04f, 0.005f), _orangeLamp, false, null, false);
            Box("TapeB", note.transform, new Vector3(0.25f, -0.2f, -0.01f), new Vector3(0.12f, 0.04f, 0.005f), _orangeLamp, false, null, false);
            var terminal = note.AddComponent<TerminalInteractable>();
            terminal.EditorConfigure("eng_diagram", "prompt.terminal.use", null, false);
            terminal.EditorSetTerminal(EngineeringContent.DiagramNote, null);

            // parts drawer with the spare coupling
            var drawer = Group("PartsDrawer", props, new Vector3(-4.75f, 0.62f, -1.6f));
            Box("DrawerFront", drawer.transform, Vector3.zero, new Vector3(0.05f, 0.25f, 0.6f), M.Brass, false, null, false);
            var pickupGo = Group("Pickup_Coupling", props, new Vector3(-5.0f, 0.97f, -1.6f));
            pickupGo.AddComponent<BoxCollider>().size = new Vector3(0.4f, 0.25f, 0.4f);
            Cylinder("Coupling", pickupGo.transform, Vector3.zero, new Vector3(0.14f, 0.12f, 0.14f), M.Brass, false, Quaternion.Euler(0, 0, 90), false);
            Cylinder("Insulator", pickupGo.transform, Vector3.zero, new Vector3(0.17f, 0.04f, 0.17f), M.Ceramic, false, Quaternion.Euler(0, 0, 90), false);
            var pickup = pickupGo.AddComponent<PickupInteractable>();
            pickup.EditorConfigure("eng_pickup_coupling", "prompt.pickup", EngineeringContent.Coupling.NameKey, true);
            pickup.EditorSetItem(EngineeringContent.Coupling);
            PointLight("Bench_Glow", props, new Vector3(-4.9f, 1.6f, -1.8f), Hex("#FFB36B"), 0.8f, 2.2f);

            // inspect prefab for the coupling
            var inspect = new GameObject("Inspect_PowerCoupling");
            Cylinder("Coupling", inspect.transform, Vector3.zero, new Vector3(0.14f, 0.12f, 0.14f), M.Brass, false, Quaternion.Euler(0, 0, 90), false);
            Cylinder("Insulator", inspect.transform, Vector3.zero, new Vector3(0.17f, 0.04f, 0.17f), M.Ceramic, false, Quaternion.Euler(0, 0, 90), false);
            Directory.CreateDirectory("Assets/_Project/Prefabs/Interactables/Inspect");
            DockingBayContent.SetInspectPrefab(EngineeringContent.Coupling,
                PrefabUtility.SaveAsPrefabAsset(inspect, "Assets/_Project/Prefabs/Interactables/Inspect/Inspect_PowerCoupling.prefab"));
            Object.DestroyImmediate(inspect);

            DockingBayBuilder.BuildRecorder(props, "Recorder_LogEng01", new Vector3(-5.15f, 0.97f, -3.3f), Quaternion.Euler(0, 80, 0), EngineeringContent.LogEng01);
        }

        private static void BuildInterlockPanel(Transform props)
        {
            var p = Group("InterlockPanel", props, new Vector3(-5.92f, 1.8f, 3.0f));
            p.transform.localRotation = Quaternion.Euler(0, -90, 0);
            var t = p.transform;
            Box("Plate", t, Vector3.zero, new Vector3(1.8f, 1.1f, 0.08f), M.SlateDark, false);
            Box("Frame", t, new Vector3(0, 0, 0.02f), new Vector3(1.9f, 1.2f, 0.04f), M.Brass, false);
            var title = WorldText("Label", t, new Vector3(0, 0.42f, -0.06f), Quaternion.identity, "eng.interlock_label", 1.3f, Hex("#FFB36B"));
            ((RectTransform)title.transform).sizeDelta = new Vector2(1.8f, 0.2f);
            for (int i = 0; i < 3; i++)
            {
                float x = -0.55f + i * 0.55f;
                Box($"Slot_{i}", t, new Vector3(x, -0.05f, -0.05f), new Vector3(0.3f, 0.4f, 0.04f), M.Rubber, false);
                Box($"Tag_{i}", t, new Vector3(x + 0.08f, -0.35f, -0.07f), new Vector3(0.22f, 0.14f, 0.01f), _redTag, false, Quaternion.Euler(0, 0, -8 + i * 6), false);
                var tag = WorldText($"TagText_{i}", t, new Vector3(x + 0.08f, -0.35f, -0.08f), Quaternion.Euler(0, 0, -8 + i * 6), "eng.interlock_tag", 0.38f, Color.white);
                ((RectTransform)tag.transform).sizeDelta = new Vector2(0.22f, 0.14f);
                tag.characterSpacing = 0;
            }
            Box("Crate", props, new Vector3(-5.3f, 0.3f, 3.0f), new Vector3(0.7f, 0.6f, 0.7f), M.SlateDark);
            DockingBayBuilder.BuildRecorder(props, "Recorder_LogEng03", new Vector3(-5.3f, 0.62f, 3.0f), Quaternion.Euler(0, 60, 0), EngineeringContent.LogEng03);
        }

        private static void BuildStatusDesk(Transform props)
        {
            var desk = Group("StatusDesk", props, new Vector3(5.35f, 0, -3.6f));
            desk.transform.localRotation = Quaternion.Euler(0, 90, 0); // local -Z faces into the room (-X)
            var t = desk.transform;
            desk.AddComponent<BoxCollider>().size = new Vector3(1.4f, 1.5f, 0.8f);
            desk.GetComponent<BoxCollider>().center = new Vector3(0, 0.75f, 0);
            Box("Base", t, new Vector3(0, 0.45f, 0), new Vector3(1.3f, 0.9f, 0.55f), M.SlateDark, false);
            Box("Top", t, new Vector3(0, 0.93f, 0.05f), new Vector3(1.4f, 0.06f, 0.7f), M.Brass, false);
            Box("Screen", t, new Vector3(0, 1.35f, -0.15f), new Vector3(0.9f, 0.55f, 0.05f), M.SlateDark, false, Quaternion.Euler(-10, 0, 0));
            var glow = Box("ScreenGlow", t, new Vector3(0, 1.35f, -0.12f), new Vector3(0.84f, 0.49f, 0.005f), _orangeLamp, false, Quaternion.Euler(-10, 0, 0), false);
            Box("Clipboard", t, new Vector3(0.45f, 0.97f, 0.15f), new Vector3(0.25f, 0.02f, 0.33f), M.Ceramic, false, Quaternion.Euler(0, 20, 0));
            var terminal = desk.AddComponent<TerminalInteractable>();
            terminal.EditorConfigure("eng_status", "prompt.terminal.use", null, false);
            terminal.EditorSetTerminal(EngineeringContent.StatusTerminal, glow);
            DockingBayBuilder.BuildRecorder(props, "Recorder_LogEng02", new Vector3(5.2f, 0.98f, -2.65f), Quaternion.Euler(0, -100, 0), EngineeringContent.LogEng02);
        }

        private static void BuildDialogueTriggers(Transform root)
        {
            var t = Group("DialogueTriggers", root).transform;
            Group("AI_ENG_01", t).AddComponent<LastSignal.Dialogue.DialogueTrigger>()
                .EditorSet(DialogueContent.AiEng01, LastSignal.Dialogue.DialogueTriggerMode.OnSectorStart, null, null, 2.5f);
            Group("AI_ENG_02", t).AddComponent<LastSignal.Dialogue.DialogueTrigger>()
                .EditorSet(DialogueContent.AiEng02, LastSignal.Dialogue.DialogueTriggerMode.OnFlag, LastSignal.Audio.AudioLogManager.PlayedKey("log_eng_03"), null, 1.0f);
            Group("AI_ENG_03", t).AddComponent<LastSignal.Dialogue.DialogueTrigger>()
                .EditorSet(DialogueContent.AiEng03, LastSignal.Dialogue.DialogueTriggerMode.OnFlag, $"routing.{EngineeringContent.RoutingPuzzleId}.array_feed", null, 0.4f);
        }

        private static void BuildHatches(Transform props, Transform root)
        {
            // back to the Docking Bay
            var hatchGo = Group("Hatch_ToDocking", props, new Vector3(0, 1.45f, -HalfL + 0.06f));
            Ring("Ring", hatchGo.transform, Vector3.zero, 1.15f, 0.22f, 0.2f, 32, M.Brass);
            Cylinder("Plate", hatchGo.transform, new Vector3(0, 0, -0.02f), new Vector3(2.1f, 0.04f, 2.1f), M.Terracotta, false, Quaternion.Euler(90, 0, 0));
            hatchGo.AddComponent<BoxCollider>().size = new Vector3(2.3f, 2.3f, 0.3f);
            var hatch = hatchGo.AddComponent<SectorHatch>();
            hatch.EditorConfigure("eng_hatch_docking", "prompt.hatch.go", null, false);
            hatch.EditorSetTarget(SceneIds.DockingBay, "from_engineering", "sector.docking.name", SectorIds.Docking);
            WorldText("Sign_Docking", props, new Vector3(0, 2.9f, -HalfL + 0.05f), Quaternion.Euler(0, 180, 0), "sector.docking.name", 2.6f, SignalCyan);

            var spawn = Group("Spawn_FromDocking", root, new Vector3(0, 0.05f, -5.6f));
            spawn.AddComponent<SpawnPoint>().EditorSetId("from_docking");
            var def = Group("Spawn_Default", root, new Vector3(0, 0.05f, -5.6f));
            def.AddComponent<SpawnPoint>().EditorSetId("default");
        }
    }
}
