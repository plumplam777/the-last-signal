using System.Collections.Generic;
using System.IO;
using LastSignal.Core;
using LastSignal.Dialogue;
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
    /// Code-built Medical Bay (StoryBible §6.3): small, white, too clean, lit mostly by Okafor's warm desk lamp.
    /// Flow: wipe the frost off Stasis Pod 3's nameplate (RECORD 0417) → PIN on the patient terminal → read Jonah's
    /// record + "Release secured items" → cabinet opens → Rhee's research keycard → hatch to the Research Lab.
    /// Overwrites Sector_Medical.unity.
    /// </summary>
    public static class MedicalBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Station/Sector_Medical.unity";
        private const float HalfW = 6f, HalfL = 7f, H = 4f;

        private static Material _clinical, _clinicalTrim, _coolLamp, _warmLamp, _frost, _screenRed;

        [MenuItem("Last Signal/Build Content/Medical Bay (overwrites scene)")]
        public static void Build()
        {
            StationMaterials.Create();
            _clinical = StationMaterials.Painted(Lit("M_Clinical_Wall", Hex("#9FAAB0"), 0.55f, 0.05f), StationTextures.PaintedMetal(), 0.7f);
            _clinicalTrim = StationMaterials.Painted(Lit("M_Clinical_Trim", Hex("#C3CDD2"), 0.6f, 0.05f), StationTextures.Enamel(), 0.5f);
            _coolLamp = Emissive("M_Emissive_ClinicalCool", Hex("#CFEFF2"), 1.6f, Hex("#CFEFF2"));
            _warmLamp = Emissive("M_Emissive_DeskWarm", Hex("#FFC27A"), 2.2f, Hex("#FFC27A"));
            _frost = Glass("M_Glass_Frost", Hex("#D9F2F5"), 0.97f, 0.08f);
            _screenRed = Emissive("M_Screen_Red", Hex("#B8463E"), 0.8f, Hex("#5A1E1A"));
            DockingBayContent.CreateAll();
            var playerPrefab = PlayerPrefabBuilder.Build();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("[Generated] Sector_Medical");
            root.AddComponent<SectorRoot>().EditorConfigure(SectorIds.Medical, playerPrefab);
            var on = new List<GameObject>();
            var off = new List<GameObject>();

            var env = Group("Environment", root.transform).transform;
            BuildRoom(env, on, off);
            var props = Group("Props", root.transform).transform;
            BuildPods(props);
            BuildOkaforDesk(props);
            BuildPatientTerminal(props);
            BuildCabinet(props);
            BuildHatches(props, root.transform);
            BuildDialogueTriggers(root.transform);

            var power = Group("PowerController", root.transform);
            power.AddComponent<PoweredObject>().EditorConfigure(SectorIds.Medical, on.ToArray(), off.ToArray());
            foreach (var go in on) go.SetActive(false);

            DockingBayBuilder.SetupLightingAndPost(root.transform, dockingProbes: false);
            root.AddComponent<LastSignal.Audio.SectorAmbience>().EditorSet(new List<LastSignal.Audio.AmbienceLoop>
            {
                new() { clip = "amb_station_hum", volume = 0.35f },
                new() { clip = "amb_ventilation", volume = 0.25f, whileFlag = WorldKeys.Power(SectorIds.Medical) },
            });
            RenderSettings.ambientSkyColor = Color.Lerp(Navy, Hex("#CFEFF2"), 0.12f) * 0.6f;
            RenderSettings.ambientEquatorColor = Color.Lerp(Navy, Teal, 0.1f) * 0.5f;
            RenderSettings.fogColor = Color.Lerp(Navy, Hex("#2A3A44"), 0.5f);
            RenderSettings.fogDensity = 0.028f;
            var probe = Group("ReflectionProbe_Room", root.transform, new Vector3(0, 2f, 0)).AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked;
            probe.size = new Vector3(12.4f, 4.2f, 14.4f);
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
            Debug.Log($"[MedicalBuilder] Built {ScenePath}");
        }

        // ------------------------------------------------------------------ room

        private static void BuildRoom(Transform env, List<GameObject> on, List<GameObject> off)
        {
            var r = Group("Room", env).transform;
            Box("Floor", r, new Vector3(0, -0.1f, 0), new Vector3(HalfW * 2, 0.2f, HalfL * 2), M.Floor);
            var clinicalFloor = StationMaterials.Painted(Lit("M_Clinical_Floor", Hex("#B9C6CA"), 0.5f, 0.05f), StationTextures.FloorPlate(), 0.7f);
            Box("FloorInlay", r, new Vector3(0, 0.005f, 0), new Vector3(HalfW * 2 - 1.2f, 0.01f, HalfL * 2 - 1.2f), clinicalFloor, false);
            Box("Ceiling", r, new Vector3(0, H + 0.1f, 0), new Vector3(HalfW * 2, 0.2f, HalfL * 2), M.Ceiling);
            Box("Wall_W", r, new Vector3(-HalfW - 0.1f, H / 2, 0), new Vector3(0.2f, H + 0.2f, HalfL * 2), _clinical);
            Box("Wall_E", r, new Vector3(HalfW + 0.1f, H / 2, 0), new Vector3(0.2f, H + 0.2f, HalfL * 2), _clinical);
            Box("Wall_S", r, new Vector3(0, H / 2, -HalfL - 0.1f), new Vector3(HalfW * 2, H + 0.2f, 0.2f), _clinical);
            Box("Wall_N", r, new Vector3(0, H / 2, HalfL + 0.1f), new Vector3(HalfW * 2, H + 0.2f, 0.2f), _clinical);
            foreach (int side in new[] { -1, 1 })
            {
                float x = side * (HalfW - 0.03f);
                Box($"Band_{side}", r, new Vector3(x, 1.1f, 0), new Vector3(0.05f, 0.18f, HalfL * 2), M.Brass, false);
                Box($"Skirting_{side}", r, new Vector3(x, 0.08f, 0), new Vector3(0.06f, 0.16f, HalfL * 2), M.SlateDark, false);
                for (float z = -HalfL + 1.5f; z < HalfL; z += 3f)
                    Box($"Rib_{side}_{z:0}", r, new Vector3(x - side * 0.06f, H / 2, z), new Vector3(0.12f, H, 0.16f), _clinicalTrim, false);
                // teal enamel panels high between the ribs (above the pods' vital monitor, below the pipes)
                Ornament.PanelRow(r, $"Panels_{side}", new Vector3(side * HalfW, 2.8f, -4f), new Vector3(side * HalfW, 2.8f, 5f),
                    side < 0 ? Vector3.right : Vector3.left, 4, 1.6f, 0.8f);
            }

            int i = 0;
            foreach (var p in new[] { new Vector3(-2.5f, H - 0.05f, -3.5f), new Vector3(2.5f, H - 0.05f, -3.5f), new Vector3(-2.5f, H - 0.05f, 3f), new Vector3(2.5f, H - 0.05f, 3f) })
            {
                Box($"Lamp_{i}_Frame", r, p + Vector3.up * 0.02f, new Vector3(1.3f, 0.06f, 0.5f), M.Brass, false);
                Box($"Lamp_{i}_Off", r, p, new Vector3(1.2f, 0.04f, 0.4f), M.LampOff, false);
                on.Add(Box($"Lamp_{i}_On", r, p + Vector3.down * 0.005f, new Vector3(1.21f, 0.04f, 0.41f), _coolLamp, false, null, false));
                on.Add(PointLight($"Lamp_{i}_Light", r, p + Vector3.down * 0.4f, Hex("#D8F3F5"), 3.2f, 7f, i == 2 ? LightShadows.Soft : LightShadows.None).gameObject);
                i++;
            }
            foreach (var p in new[] { new Vector3(-5.85f, 2.4f, -1.5f), new Vector3(5.85f, 2.4f, 5f) })
            {
                Box($"Emergency_{p.z:0}", r, p, new Vector3(0.1f, 0.15f, 0.45f), M.SlateDark, false);
                off.Add(Box($"EmergencyGlow_{p.z:0}", r, p + new Vector3(-Mathf.Sign(p.x) * 0.06f, 0, 0), new Vector3(0.02f, 0.08f, 0.38f), M.RedLamp, false, null, false));
                off.Add(PointLight($"EmergencyLight_{p.z:0}", r, p + new Vector3(-Mathf.Sign(p.x) * 0.6f, -0.3f, 0), EmergencyRed, 1.4f, 6f).gameObject);
            }
            Atmosphere.Dust(r, new Vector3(0, 2f, 0), new Vector3(11f, 3.4f, 13f), new Color(0.8f, 0.95f, 0.97f, 0.2f), 45);
            PropModels.PipeRun(r, "Pipes_W", new Vector3(-HalfW, H - 0.55f, -HalfL), new Vector3(-HalfW, H - 0.55f, HalfL), -90f);
            PropModels.PipeRun(r, "Pipes_E", new Vector3(HalfW, H - 0.55f, -HalfL), new Vector3(HalfW, H - 0.55f, HalfL), 90f);
            // red cross over the south hatch, "Medical" sign
            WorldText("Sign_Medical", r, new Vector3(0, 3.3f, -HalfL + 0.02f), Quaternion.Euler(0, 180, 0), "sector.medical.name", 2.6f, TealLight);
        }

        // ------------------------------------------------------------------ pods

        private static void BuildPods(Transform props)
        {
            // Pods 1 and 2: open and empty
            for (int n = 1; n <= 2; n++)
            {
                var pod = Group($"StasisPod_{n}", props, new Vector3(-4.6f, 0, n == 1 ? -4.2f : -0.6f)).transform;
                PodShell(pod, open: true);
                RawText("Number", pod, new Vector3(1.0f, 1.15f, 0), Quaternion.Euler(0, -90, 0), n.ToString(), 2.2f, Brass);
            }

            // Pod 3: frosted, Jonah
            var p3 = Group("StasisPod_3", props, new Vector3(-4.6f, 0, 3.0f)).transform;
            PodShell(p3, open: false);
            RawText("Number", p3, new Vector3(1.0f, 1.15f, 0.85f), Quaternion.Euler(0, -90, 0), "3", 2.2f, Brass);
            Box("ChessPaper", p3, new Vector3(0.05f, 1.02f, -0.35f), new Vector3(0.35f, 0.005f, 0.35f), M.Ceramic, false, Quaternion.Euler(0, 12, 0));
            // vital monitor above the pod (flat line)
            var mon = Group("VitalMonitor", p3, new Vector3(-0.85f, 2.0f, 0)).transform;
            mon.localRotation = Quaternion.Euler(0, -90, 0); // faces the aisle (+X)
            Box("Bezel", mon, Vector3.zero, new Vector3(1.0f, 0.6f, 0.06f), M.SlateDark, false);
            Box("Screen", mon, new Vector3(0, 0, -0.035f), new Vector3(0.92f, 0.52f, 0.005f), _screenRed, false);
            Box("FlatLine", mon, new Vector3(0, -0.05f, -0.04f), new Vector3(0.8f, 0.01f, 0.003f), M.RedLamp, false);
            var monText = WorldText("Text", mon, new Vector3(0, 0.12f, -0.045f), Quaternion.identity, "med.pod.monitor", 0.9f, new Color(1f, 0.75f, 0.7f));
            ((RectTransform)monText.transform).sizeDelta = new Vector2(0.9f, 0.3f);
            monText.characterSpacing = 2;

            // nameplate on the aisle side: frosted until wiped
            var plate = Group("Nameplate", p3, new Vector3(0.62f, 0.75f, 0)).transform;
            plate.localRotation = Quaternion.Euler(0, -90, 0); // local -Z faces the aisle (+X)
            Box("Plate", plate, Vector3.zero, new Vector3(0.7f, 0.25f, 0.02f), M.Brass, false);
            var text = WorldText("Text", plate, new Vector3(0, 0, -0.015f), Quaternion.identity, "med.pod.nameplate", 0.75f, Navy);
            ((RectTransform)text.transform).sizeDelta = new Vector2(0.65f, 0.22f);
            text.characterSpacing = 2;
            var frost = Box("Frost", plate, new Vector3(0, 0, -0.03f), new Vector3(0.74f, 0.29f, 0.01f), _frost, false, null, false);
            var wipeGo = plate.gameObject;
            wipeGo.AddComponent<BoxCollider>().size = new Vector3(0.8f, 0.35f, 0.15f);
            var wipe = wipeGo.AddComponent<Interactable>();
            wipe.EditorConfigure("med_pod_wipe", "prompt.wipe", "med.pod.name", true, null, new List<Effect> { Effect.Flag(StationContent.PodWipedFlag) });
            plate.gameObject.AddComponent<ConditionToggle>().EditorSet(
                new ConditionSet { conditions = { Condition.Flag(StationContent.PodWipedFlag) } }, null, new[] { frost });

            DockingBayBuilder.BuildRecorder(props, "Recorder_LogMed02", new Vector3(-3.95f, 0.55f, 4.05f), Quaternion.Euler(0, -90, 0), StationContent.LogMed02);
            Box("SidePanel", props, new Vector3(-3.97f, 0.45f, 4.05f), new Vector3(0.12f, 0.12f, 0.4f), M.SlateDark, false);
        }

        private static void PodShell(Transform pod, bool open)
        {
            if (PropModels.Place("prop_cryo_pod_base", pod, Vector3.zero) != null)
            {
                // Blender model (Tools/blender/build_props.py); canopy hinges on the wall side (-X)
                if (open)
                    PropModels.Place("prop_cryo_pod_canopy", pod, new Vector3(-0.5f, 0.66f, 0), Quaternion.Euler(0, 0, 105),
                        new Dictionary<string, Material> { ["Glass"] = M.GlassTeal });
                else
                {
                    PropModels.Place("prop_cryo_pod_canopy", pod, new Vector3(-0.5f, 0.66f, 0), null,
                        new Dictionary<string, Material> { ["Glass"] = _frost });
                    Box("Shape", pod, new Vector3(0, 0.74f, 0.1f), new Vector3(0.42f, 0.16f, 1.5f), M.SlateDark, false);
                }
                var col = Group("Collider", pod).AddComponent<BoxCollider>();
                col.center = new Vector3(0, 0.6f, 0);
                col.size = new Vector3(1.2f, 1.2f, 2.45f);
                return;
            }
            Box("Plinth", pod, new Vector3(0, 0.25f, 0), new Vector3(1.2f, 0.5f, 2.4f), M.SlateDark);
            Box("Trim", pod, new Vector3(0, 0.51f, 0), new Vector3(1.26f, 0.04f, 2.46f), M.Brass, false);
            Cylinder("Bed", pod, new Vector3(0, 0.7f, 0), new Vector3(1.0f, 1.1f, 0.5f), _clinicalTrim, false, Quaternion.Euler(90, 0, 0));
            if (open)
            {
                Cylinder("Lid_Open", pod, new Vector3(-0.55f, 1.05f, 0), new Vector3(0.9f, 1.05f, 0.18f), M.GlassTeal, false, Quaternion.Euler(90, 0, 60));
            }
            else
            {
                Cylinder("Lid", pod, new Vector3(0, 0.85f, 0), new Vector3(0.98f, 1.08f, 0.42f), _frost, true, Quaternion.Euler(90, 0, 0));
                Box("Shape", pod, new Vector3(0, 0.82f, 0.1f), new Vector3(0.45f, 0.18f, 1.6f), M.SlateDark, false);
            }
            foreach (int z in new[] { -1, 1 })
                Cylinder($"Cap_{z}", pod, new Vector3(0, 0.75f, z * 1.12f), new Vector3(1.05f, 0.08f, 0.6f), M.Brass, false, Quaternion.Euler(90, 0, 0));
        }

        // ------------------------------------------------------------------ Okafor's desk

        private static void BuildOkaforDesk(Transform props)
        {
            var d = Group("OkaforDesk", props, new Vector3(4.9f, 0, -2.8f)).transform;
            if (PropModels.Place("prop_desk", d, Vector3.zero) != null)
            {
                PropModels.Solid(d, new Vector3(0, 0.4f, 0), new Vector3(1.1f, 0.82f, 2.0f));
                PropModels.Place("prop_office_chair", props, new Vector3(4.0f, 0, -2.8f), Quaternion.Euler(0, 90, 0), name: "Chair");
                PropModels.Solid(props.Find("Chair"), new Vector3(0, 0.45f, 0), new Vector3(0.5f, 0.9f, 0.5f));
            }
            else
            {
                Box("Top", d, new Vector3(0, 0.8f, 0), new Vector3(1.1f, 0.06f, 2.0f), M.SlateDark);
                Box("Trim", d, new Vector3(0, 0.77f, 0), new Vector3(1.14f, 0.03f, 2.04f), M.Brass, false);
                Box("Drawers", d, new Vector3(0.1f, 0.38f, 0.6f), new Vector3(0.8f, 0.76f, 0.7f), _clinical);
                Box("Leg", d, new Vector3(0.1f, 0.38f, -0.85f), new Vector3(0.8f, 0.76f, 0.08f), M.SlateDark, false);
                Box("Chair", props, new Vector3(4.0f, 0.45f, -2.8f), new Vector3(0.5f, 0.08f, 0.5f), M.Rubber);
            }
            Box("PrayerBook", d, new Vector3(-0.15f, 0.86f, 0.55f), new Vector3(0.22f, 0.05f, 0.3f), M.Terracotta, false, Quaternion.Euler(0, 8, 0));
            // battery desk lamp: the one warm light in the room (always on)
            var lamp = Group("DeskLamp", d, new Vector3(0.25f, 0.83f, -0.6f)).transform;
            Cylinder("Base", lamp, Vector3.zero, new Vector3(0.18f, 0.02f, 0.18f), M.Brass, false);
            Cylinder("Stem", lamp, new Vector3(0, 0.22f, 0), new Vector3(0.025f, 0.22f, 0.025f), M.Brass, false);
            Sphere("Shade", lamp, new Vector3(-0.08f, 0.45f, 0), new Vector3(0.22f, 0.12f, 0.22f), _warmLamp);
            PointLight("DeskLampLight", lamp, new Vector3(-0.15f, 0.35f, 0), Hex("#FFB36B"), 2.6f, 5.5f, LightShadows.Soft);

            // chess notation pad (readable)
            var pad = Group("ChessPad", d, new Vector3(-0.2f, 0.84f, -0.2f));
            pad.AddComponent<BoxCollider>().size = new Vector3(0.35f, 0.1f, 0.45f);
            Box("Pad", pad.transform, Vector3.zero, new Vector3(0.24f, 0.01f, 0.32f), M.Ceramic, false, Quaternion.Euler(0, -6, 0), false);
            var t = pad.AddComponent<TerminalInteractable>();
            t.EditorConfigure("med_desk_pad", "prompt.terminal.use", null, false);
            t.EditorSetTerminal(StationContent.OkaforNotes, null);

            DockingBayBuilder.BuildRecorder(props, "Recorder_LogMed01", new Vector3(4.7f, 0.84f, -3.4f), Quaternion.Euler(0, -80, 0), StationContent.LogMed01);
        }

        // ------------------------------------------------------------------ patient terminal + cabinet

        private static void BuildPatientTerminal(Transform props)
        {
            var desk = Group("PatientTerminal", props, new Vector3(5.3f, 0, 1.6f));
            desk.transform.localRotation = Quaternion.Euler(0, 90, 0); // faces the room (-X)
            var t = desk.transform;
            var col = desk.AddComponent<BoxCollider>();
            col.size = new Vector3(1.4f, 1.6f, 0.8f);
            col.center = new Vector3(0, 0.8f, 0);
            if (PropModels.Place("prop_terminal_kiosk", t, Vector3.zero) == null)
            {
                Box("Base", t, new Vector3(0, 0.45f, 0), new Vector3(1.3f, 0.9f, 0.55f), M.SlateDark, false);
                Box("Top", t, new Vector3(0, 0.93f, 0.05f), new Vector3(1.4f, 0.06f, 0.7f), M.Brass, false);
                Box("Screen", t, new Vector3(0, 1.38f, -0.15f), new Vector3(0.95f, 0.6f, 0.05f), M.SlateDark, false, Quaternion.Euler(-10, 0, 0));
            }
            var glow = Box("ScreenGlow", t, new Vector3(0, 1.38f, -0.18f), new Vector3(0.88f, 0.53f, 0.005f), M.CyanStrip, false, Quaternion.Euler(-10, 0, 0), false);
            var term = desk.AddComponent<TerminalInteractable>();
            term.EditorConfigure("med_patient", "prompt.terminal.use", null, false,
                new ConditionSet { conditions = { Condition.Powered(SectorIds.Medical, "prompt.door.no_power") } });
            term.EditorSetTerminal(StationContent.PatientTerminal, glow);

            // Okafor's day -9 note appears beside the terminal once the record is open
            var rec = DockingBayBuilder.BuildRecorder(props, "Recorder_LogMed03", new Vector3(5.05f, 0.98f, 2.55f), Quaternion.Euler(0, -90, 0), StationContent.LogMed03);
            var toggle = Group("LogMed03_Reveal", props);
            toggle.AddComponent<ConditionToggle>().EditorSet(
                new ConditionSet { conditions = { new Condition { type = ConditionType.PuzzleSolved, key = StationContent.PatientTerminalPuzzleId } } },
                new[] { rec }, null);
        }

        private static void BuildCabinet(Transform props)
        {
            var c = Group("SecureCabinet", props, new Vector3(3.4f, 0, HalfL - 0.35f)).transform;
            bool caseModel = PropModels.Place("prop_cabinet_case", c, Vector3.zero) != null;
            HideRenderers(HollowCase("Body", c, new Vector3(0, 1.0f, 0), new Vector3(1.2f, 2.0f, 0.5f), M.SlateDark), caseModel); // hollow: the keycard inside must be visible and reachable
            Box("Shelf", c, new Vector3(0, 1.2f, -0.02f), new Vector3(1.1f, 0.04f, 0.42f), M.Brass, false);
            var label = WorldText("Label", c, new Vector3(0, 2.25f, -0.26f), Quaternion.identity, "med.cabinet.label", 1.1f, EmergencyRed);
            ((RectTransform)label.transform).sizeDelta = new Vector2(1.4f, 0.4f);
            label.characterSpacing = 2;
            var closed = Box("Door_Closed", c, new Vector3(0, 1.0f, -0.27f), new Vector3(1.16f, 1.96f, 0.04f), M.Brass, true, null, false);
            var open = Box("Door_Open", c, new Vector3(-0.85f, 1.0f, -0.82f), new Vector3(1.16f, 1.96f, 0.04f), M.Brass, false, Quaternion.Euler(0, 75, 0), false);
            var statusLocked = Sphere("Status_Locked", c, new Vector3(0.45f, 1.6f, -0.3f), Vector3.one * 0.05f, M.RedLamp, false, false);
            var statusOpen = Sphere("Status_Open", c, new Vector3(0.45f, 1.6f, -0.3f), Vector3.one * 0.05f, M.CyanStrip, false, false);
            c.gameObject.AddComponent<ConditionToggle>().EditorSet(
                new ConditionSet { conditions = { Condition.Flag(StationContent.CabinetReleasedFlag) } },
                new[] { open, statusOpen }, new[] { closed, statusLocked });

            // Rhee's keycard on the shelf
            var pickupGo = Group("Pickup_KeycardResearch", c, new Vector3(0, 1.25f, 0));
            pickupGo.AddComponent<BoxCollider>().size = new Vector3(0.4f, 0.15f, 0.35f);
            Keycard(pickupGo.transform, Teal);
            var pickup = pickupGo.AddComponent<PickupInteractable>();
            pickup.EditorConfigure("med_pickup_keycard_research", "prompt.pickup", StationContent.KeycardResearch.NameKey, true,
                new ConditionSet { conditions = { Condition.Flag(StationContent.CabinetReleasedFlag, "med.cabinet.locked") } });
            pickup.EditorSetItem(StationContent.KeycardResearch);

            var inspect = new GameObject("Inspect_KeycardResearch");
            Keycard(inspect.transform, Teal);
            Directory.CreateDirectory("Assets/_Project/Prefabs/Interactables/Inspect");
            DockingBayContent.SetInspectPrefab(StationContent.KeycardResearch,
                PrefabUtility.SaveAsPrefabAsset(inspect, "Assets/_Project/Prefabs/Interactables/Inspect/Inspect_KeycardResearch.prefab"));
            Object.DestroyImmediate(inspect);
        }

        /// <summary>Brass-edged keycard with a coloured stripe (teal = research, amber = core).</summary>
        internal static void Keycard(Transform parent, Color stripe)
        {
            var stripeMat = Emissive(stripe == Teal ? "M_Keycard_Teal" : "M_Keycard_Amber", stripe, 0.6f, stripe);
            Box("Card", parent, Vector3.zero, new Vector3(0.2f, 0.008f, 0.13f), M.Ceramic, false, null, false);
            Box("Edge", parent, new Vector3(0, -0.001f, 0), new Vector3(0.21f, 0.006f, 0.14f), M.Brass, false, null, false);
            Box("Stripe", parent, new Vector3(0, 0.005f, 0.035f), new Vector3(0.2f, 0.002f, 0.03f), stripeMat, false, null, false);
            Box("Chip", parent, new Vector3(-0.05f, 0.005f, -0.02f), new Vector3(0.035f, 0.002f, 0.03f), M.Brass, false, null, false);
        }

        // ------------------------------------------------------------------ hatches, triggers

        private static void BuildHatches(Transform props, Transform root)
        {
            Hatch(props, "Hatch_ToEngineering", new Vector3(0, 1.45f, -HalfL + 0.06f), 180f, SceneIds.Engineering, "from_medical", "sector.engineering.name",
                SectorIds.Engineering, null, M.Terracotta);
            Hatch(props, "Hatch_ToResearch", new Vector3(-1.6f, 1.45f, HalfL - 0.06f), 0f, SceneIds.Research, "from_medical", "sector.research.name",
                SectorIds.Research, new ConditionSet { conditions = { Condition.Item(StationContent.KeycardResearch, "med.door.research_keycard") } }, M.Ceramic);
            WorldText("Sign_Research", props, new Vector3(-1.6f, 3.1f, HalfL - 0.03f), Quaternion.identity, "med.sign.research", 2.2f, SignalCyan);
            // keycard reader beside the research hatch
            var reader = Group("KeycardReader", props, new Vector3(-0.1f, 1.4f, HalfL - 0.05f)).transform;
            Box("Body", reader, Vector3.zero, new Vector3(0.22f, 0.32f, 0.06f), M.SlateDark, false);
            Box("Slot", reader, new Vector3(0, 0.05f, -0.035f), new Vector3(0.14f, 0.02f, 0.01f), M.CyanStrip, false);

            Spawn(root, "from_engineering", new Vector3(0, 0.05f, -5.6f), 0f);
            Spawn(root, "default", new Vector3(0, 0.05f, -5.6f), 0f);
            Spawn(root, "from_research", new Vector3(-1.6f, 0.05f, 5.4f), 180f);
        }

        /// <param name="yaw">Rotation so local +Z points out of the room through the wall (south 180, north 0, east 90, west 270).</param>
        internal static void Hatch(Transform props, string name, Vector3 pos, float yaw, string scene, string spawnId, string nameKey, string sectorId,
            ConditionSet reqs, Material plate)
        {
            var hatchGo = Group(name, props, pos);
            hatchGo.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            Ring("Ring", hatchGo.transform, Vector3.zero, 1.15f, 0.22f, 0.2f, 32, M.Brass);
            Cylinder("Plate", hatchGo.transform, new Vector3(0, 0, -0.02f), new Vector3(2.1f, 0.04f, 2.1f), plate, false, Quaternion.Euler(90, 0, 0));
            Box("Bar", hatchGo.transform, new Vector3(0, 0, -0.06f), new Vector3(2.0f, 0.18f, 0.06f), M.Brass, false);
            HatchOrnament(hatchGo.transform);
            hatchGo.AddComponent<BoxCollider>().size = new Vector3(2.3f, 2.3f, 0.3f);
            var hatch = hatchGo.AddComponent<SectorHatch>();
            hatch.EditorConfigure(name.ToLowerInvariant(), "prompt.hatch.go", null, false, reqs);
            hatch.EditorSetTarget(scene, spawnId, nameKey, sectorId);
        }

        /// <summary>
        /// Ornate surround for the round sector hatches (reference A rings): terracotta surround ring with eight brass
        /// clamp teeth, a rivet circle on the plate and a hand wheel with a teal status hub. Kept within r = 1.41 m so
        /// it never reaches the sector signs above the hatches. Decorative only (the hatch keeps its own collider).
        /// </summary>
        internal static void HatchOrnament(Transform h)
        {
            var o = Group("Ornament", h).transform;
            Ring("Surround", o, new Vector3(0, 0, 0.04f), 1.33f, 0.16f, 0.12f, 32, M.Terracotta);
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f + 22.5f;
                var d = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad), 0);
                Box($"Tooth_{i}", o, d * 1.3f + new Vector3(0, 0, -0.02f), new Vector3(0.2f, 0.12f, 0.2f), M.Brass, false, Quaternion.Euler(0, 0, a));
            }
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f * Mathf.Deg2Rad;
                Sphere($"Rivet_{i}", o, new Vector3(Mathf.Cos(a) * 0.92f, Mathf.Sin(a) * 0.92f, -0.05f), Vector3.one * 0.07f, M.Brass);
            }
            Ring("Wheel", o, new Vector3(0, 0, -0.12f), 0.42f, 0.05f, 0.05f, 20, M.Brass);
            Box("Spoke", o, new Vector3(0, 0, -0.12f), new Vector3(0.05f, 0.84f, 0.04f), M.Brass, false);
            Cylinder("Hub", o, new Vector3(0, 0, -0.13f), new Vector3(0.22f, 0.04f, 0.22f), M.Brass, false, Quaternion.Euler(90, 0, 0));
            Cylinder("HubGlow", o, new Vector3(0, 0, -0.16f), new Vector3(0.1f, 0.01f, 0.1f), M.CyanStrip, false, Quaternion.Euler(90, 0, 0));
        }

        internal static void Spawn(Transform root, string id, Vector3 pos, float yaw)
        {
            var s = Group($"Spawn_{id}", root, pos);
            s.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            s.AddComponent<SpawnPoint>().EditorSetId(id);
        }

        private static void BuildDialogueTriggers(Transform root)
        {
            var t = Group("DialogueTriggers", root).transform;
            var podZone = Group("AI_MED_01", t, new Vector3(-3.2f, 1f, 3.0f));
            var box = podZone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(2.6f, 2f, 3.2f);
            podZone.AddComponent<DialogueTrigger>().EditorSet(DialogueContent.AiMed01, DialogueTriggerMode.OnEnterVolume, null, null, 0.2f);
            Group("AI_MED_02", t).AddComponent<DialogueTrigger>()
                .EditorSet(DialogueContent.AiMed02, DialogueTriggerMode.OnFlag, LastSignal.Audio.AudioLogManager.PlayedKey("log_med_03"), null, 1.0f);
        }
    }
}
