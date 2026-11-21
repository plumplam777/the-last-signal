using System.Collections.Generic;
using System.IO;
using LastSignal.Core;
using LastSignal.Cutscene;
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
    /// Code-built Research Laboratory (StoryBible §6.4): dark lab lit by a wall screen with a slow waveform and the
    /// night side of Nereth pulsing blue through a long window. Flow: read the Response Trials board → enter the
    /// Trial 7 answer (the sent row reversed) on the decode console → Rhee's protected files + specimen safe open →
    /// core access card → hatch to the Reactor Core. Overwrites Sector_Research.unity.
    /// </summary>
    public static class ResearchBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Station/Sector_Research.unity";
        private const float HalfW = 7f, HalfL = 8f, H = 4.5f;

        private static readonly string[][] Trials =
        {
            new[] { "tri", "cir", "sqr" },
            new[] { "cir", "cir", "dia", "tri" },
            new[] { "sqr", "tri", "dia", "cir", "tri" },
        };
        private static readonly int[] TrialNumbers = { 2, 4, 6 };
        public static readonly string[] Trial7Sent = { "dia", "sqr", "cir", "tri", "cir", "sqr" };

        private static Material _screen, _screenWave, _note, _planetGlow, _wallDark;

        [MenuItem("Last Signal/Build Content/Research Lab (overwrites scene)")]
        public static void Build()
        {
            StationMaterials.Create();
            _screen = Emissive("M_Screen_DeepBlue", Hex("#0E2A3A"), 0.6f, Hex("#0A1A24"));
            _screenWave = Emissive("M_Screen_Wave", SignalCyan, 1.6f, SignalCyan);
            _note = Emissive("M_StickyNote", Hex("#E8C76A"), 0.25f, Hex("#E8C76A"));
            _planetGlow = Emissive("M_Nereth_Night", Hex("#1E4E7A"), 0.5f, Hex("#0B1A2A"));
            _wallDark = StationMaterials.Painted(Lit("M_Wall_LabDark", Color.Lerp(Navy, Slate, 0.55f), 0.35f, 0.1f), StationTextures.PaintedMetal());
            DockingBayContent.CreateAll();
            var playerPrefab = PlayerPrefabBuilder.Build();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("[Generated] Sector_Research");
            root.AddComponent<SectorRoot>().EditorConfigure(SectorIds.Research, playerPrefab);

            var env = Group("Environment", root.transform).transform;
            BuildRoom(env);
            BuildWindow(env);
            var props = Group("Props", root.transform).transform;
            BuildContainmentRig(props);
            BuildTrialWall(props);
            BuildDecodeConsole(props);
            BuildRheeDesk(props);
            BuildListeningBooth(props);
            BuildSafe(props);
            BuildHatches(props, root.transform);
            BuildDialogueTriggers(root.transform);

            DockingBayBuilder.SetupLightingAndPost(root.transform, dockingProbes: false);
            root.AddComponent<LastSignal.Audio.SectorAmbience>().EditorSet(new List<LastSignal.Audio.AmbienceLoop>
            {
                new() { clip = "amb_station_hum", volume = 0.3f },
                new() { clip = "amb_murmur", volume = 0.3f },
                new() { clip = "amb_murmur", volume = 0.35f, whileFlag = StationContent.ListenedMurmur }, // AI_RES_01 "listen with me"
            });
            RenderSettings.ambientSkyColor = Color.Lerp(Navy, SignalCyan, 0.1f) * 0.55f;
            RenderSettings.ambientEquatorColor = Color.Lerp(Navy, Teal, 0.12f) * 0.45f;
            RenderSettings.fogColor = Color.Lerp(Navy, Hex("#0B2233"), 0.6f);
            RenderSettings.fogDensity = 0.03f;
            var probe = Group("ReflectionProbe_Lab", root.transform, new Vector3(0, 2.2f, 0)).AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked;
            probe.size = new Vector3(14.4f, 4.7f, 16.4f);
            probe.boxProjection = true;
            probe.resolution = 256;
            probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
            probe.backgroundColor = Navy * 0.4f;

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)!);
            EditorSceneManager.SaveScene(scene, ScenePath);
            DockingBayBuilder.BakeReflectionProbes(new List<GameObject>(), ScenePath);
            EditorSceneManager.SaveScene(scene, ScenePath);
            ProjectSetup.RegisterBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log($"[ResearchBuilder] Built {ScenePath}");
        }

        // ------------------------------------------------------------------ room

        private static void BuildRoom(Transform env)
        {
            var r = Group("Room", env).transform;
            Box("Floor", r, new Vector3(0, -0.1f, 0), new Vector3(HalfW * 2, 0.2f, HalfL * 2), M.Floor);
            PropModels.PipeRun(r, "Pipes_W", new Vector3(-HalfW, H - 0.55f, -HalfL), new Vector3(-HalfW, H - 0.55f, HalfL), -90f);
            PropModels.PipeRun(r, "Pipes_S", new Vector3(-HalfW, H - 0.55f, -HalfL), new Vector3(HalfW, H - 0.55f, -HalfL), 180f);
            Box("Ceiling", r, new Vector3(0, H + 0.1f, 0), new Vector3(HalfW * 2, 0.2f, HalfL * 2), M.Ceiling);
            Box("Wall_W", r, new Vector3(-HalfW - 0.1f, H / 2, 0), new Vector3(0.2f, H + 0.2f, HalfL * 2), _wallDark);
            Box("Wall_E", r, new Vector3(HalfW + 0.1f, H / 2, 0), new Vector3(0.2f, H + 0.2f, HalfL * 2), _wallDark);
            Box("Wall_S", r, new Vector3(0, H / 2, -HalfL - 0.1f), new Vector3(HalfW * 2, H + 0.2f, 0.2f), _wallDark);
            // north wall: long window between 1.0 and 3.4 m
            Box("Wall_N_Low", r, new Vector3(0, 0.5f, HalfL + 0.1f), new Vector3(HalfW * 2, 1.0f, 0.2f), _wallDark);
            Box("Wall_N_High", r, new Vector3(0, 3.95f, HalfL + 0.1f), new Vector3(HalfW * 2, 1.1f, 0.2f), _wallDark);
            Box("Wall_N_L", r, new Vector3(-6.25f, 2.2f, HalfL + 0.1f), new Vector3(1.5f, 2.4f, 0.2f), _wallDark);
            Box("Wall_N_R", r, new Vector3(6.25f, 2.2f, HalfL + 0.1f), new Vector3(1.5f, 2.4f, 0.2f), _wallDark);
            Box("Sill", r, new Vector3(0, 1.0f, HalfL - 0.05f), new Vector3(10.4f, 0.08f, 0.35f), M.Brass, false);
            for (float x = -4.5f; x <= 4.6f; x += 1.8f)
                Box($"Mullion_{x:0.0}", r, new Vector3(x, 2.2f, HalfL + 0.02f), new Vector3(0.1f, 2.4f, 0.12f), M.Brass, false);
            foreach (int side in new[] { -1, 1 })
                Box($"Band_{side}", r, new Vector3(side * (HalfW - 0.03f), 1.2f, 0), new Vector3(0.05f, StationTextures.TrimHeight, HalfL * 2), M.TrimRondel, false);
            // ornament: framed teal panels on the free wall stretches (south wall beside the hatch, west wall middle)
            Ornament.PanelRow(r, "Panels_S_W", new Vector3(-5.2f, 2.45f, -HalfL), new Vector3(-2.8f, 2.45f, -HalfL), Vector3.forward, 2);
            Ornament.PanelRow(r, "Panels_S_E", new Vector3(2.8f, 2.45f, -HalfL), new Vector3(5.2f, 2.45f, -HalfL), Vector3.forward, 2);
            Ornament.PanelRow(r, "Panels_W", new Vector3(-HalfW, 2.45f, -1.0f), new Vector3(-HalfW, 2.45f, 1.9f), Vector3.right, 2);

            Atmosphere.Dust(r, new Vector3(0, 2.2f, 0), new Vector3(13f, 3.8f, 15f), new Color(0.55f, 0.95f, 0.92f, 0.28f));

            // dim ceiling strips (the lab was kept dark for the screens)
            foreach (float x in new[] { -3.5f, 3.5f })
            {
                Box($"Strip_{x}", r, new Vector3(x, H - 0.03f, 0), new Vector3(0.12f, 0.04f, 12f), M.CyanStrip, false);
                PointLight($"StripLight_{x}_S", r, new Vector3(x, H - 0.4f, -4f), TealLight, 0.9f, 6f);
                PointLight($"StripLight_{x}_N", r, new Vector3(x, H - 0.4f, 3f), TealLight, 0.7f, 6f);
            }
        }

        private static void BuildWindow(Transform env)
        {
            var w = Group("Window_Nereth", env).transform;
            Box("Glass", w, new Vector3(0, 2.2f, HalfL + 0.12f), new Vector3(10.4f, 2.4f, 0.04f), M.GlassTeal, false);
            Box("Space", w, new Vector3(0, 2.2f, HalfL + 70f), new Vector3(260f, 160f, 0.2f), SkyArt.Starfield(), false);
            // the night side of Nereth: dark ice whose fracture lines pulse blue (procedural textures, SkyArt)
            var planet = Sphere("Nereth", w, new Vector3(0, -9f, HalfL + 18f), new Vector3(34f, 34f, 34f), SkyArt.Nereth());
            planet.transform.localRotation = Quaternion.Euler(18f, 40f, 0f);
            var pulse = PointLight("MurmurPulse", w, new Vector3(0, 1.2f, HalfL + 1.2f), SignalCyan, 2f, 9f);
            planet.AddComponent<EmissionPulse>().EditorSet(Hex("#2F8FD8"), 0.15f, 1.4f, 40f / 6f, 0f, pulse);
            for (int i = 0; i < 5; i++)
            {
                var spot = Sphere($"Pulse_{i}", w, new Vector3(-6f + i * 3f, -1.0f + (i % 2) * 0.6f, HalfL + 2.2f + i * 0.1f), new Vector3(0.9f, 0.25f, 0.9f), _screenWave);
                spot.AddComponent<EmissionPulse>().EditorSet(SignalCyan, 0f, 2.2f, 40f / 6f, i * 0.13f);
            }
        }

        // ------------------------------------------------------------------ trials + decode

        /// <summary>
        /// Hero machine (ArtDirection.md): an armillary-like containment rig around a teal crystal shard of the
        /// Murmur — three nested brass rings turning slowly on different axes, hung from the ceiling on a brass rod,
        /// standing on a stepped terracotta pedestal. Centre of the lab, framed by the Nereth window from the entrance.
        /// </summary>
        private static void BuildContainmentRig(Transform props)
        {
            var rig = Group("ContainmentRig", props, new Vector3(0f, 0f, 1.2f)).transform;
            var crystal = Emissive("M_Crystal_Teal", Teal, 0.75f, Color.Lerp(Teal, Navy, 0.35f)); // stays teal under bloom
            crystal.SetFloat("_Smoothness", 0.9f);

            // pedestal (the only collider)
            Cylinder("Base", rig, new Vector3(0, 0.1f, 0), new Vector3(1.9f, 0.1f, 1.9f), M.SlateDark);
            Cylinder("BaseTrim", rig, new Vector3(0, 0.21f, 0), new Vector3(1.96f, 0.02f, 1.96f), M.Brass, false);
            Cylinder("Step", rig, new Vector3(0, 0.32f, 0), new Vector3(1.4f, 0.1f, 1.4f), M.Terracotta, false);
            Cylinder("StepTrim", rig, new Vector3(0, 0.43f, 0), new Vector3(1.44f, 0.02f, 1.44f), M.Brass, false);
            Cylinder("Column", rig, new Vector3(0, 0.66f, 0), new Vector3(0.7f, 0.22f, 0.7f), M.Terracotta, false);
            Cylinder("ColumnCap", rig, new Vector3(0, 0.9f, 0), new Vector3(0.8f, 0.03f, 0.8f), M.Brass, false);
            for (int i = 0; i < 4; i++)                                           // glowing vents in the step
            {
                float a = i * 90f + 45f;
                var dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                Box($"Vent_{i}", rig, dir * 0.66f + Vector3.up * 0.32f, new Vector3(0.22f, 0.05f, 0.04f), crystal, false, Quaternion.Euler(0, a, 0));
            }

            const float cy = 1.95f;
            Cylinder("PivotLow", rig, new Vector3(0, 0.95f, 0), new Vector3(0.06f, 0.06f, 0.06f), M.Brass, false);
            Cylinder("HangerRod", rig, new Vector3(0, (cy + 0.95f + H) * 0.5f, 0), new Vector3(0.06f, (H - cy - 0.95f) * 0.5f, 0.06f), M.Brass, false);
            Cylinder("HangerPlate", rig, new Vector3(0, H - 0.03f, 0), new Vector3(0.6f, 0.03f, 0.6f), M.Brass, false);

            // gimbal: outer (vertical axis) > middle (local X) > inner (local Z, tilted)
            var outer = Group("Ring_Outer", rig, new Vector3(0, cy, 0));
            Ring("Band", outer.transform, Vector3.zero, 0.95f, 0.07f, 0.09f, 32, M.Brass);
            foreach (float y in new[] { -0.95f, 0.95f })
                Sphere($"Pin_{y:0}", outer.transform, new Vector3(0, y, 0), Vector3.one * 0.11f, M.Brass, false, false);
            outer.AddComponent<FlagSpinner>().EditorSet(null, Vector3.up, 9f);
            MakeDynamic(outer);

            var middle = Group("Ring_Middle", outer.transform);
            middle.transform.localRotation = Quaternion.Euler(0, 90f, 0);
            Ring("Band", middle.transform, Vector3.zero, 0.78f, 0.05f, 0.07f, 28, M.Brass);
            foreach (int s in new[] { -1, 1 })
                Sphere($"Orb_{s}", middle.transform, new Vector3(s * 0.78f, 0, 0), Vector3.one * 0.16f, M.GlassTeal, false, false);
            middle.AddComponent<FlagSpinner>().EditorSet(null, Vector3.right, 14f);

            var inner = Group("Ring_Inner", middle.transform);
            inner.transform.localRotation = Quaternion.Euler(60f, 0, 0);
            Ring("Band", inner.transform, Vector3.zero, 0.6f, 0.045f, 0.06f, 24, M.Brass);
            Ring("Teeth", inner.transform, new Vector3(0, 0, 0.035f), 0.56f, 0.03f, 0.02f, 24, M.TrimRondel);
            inner.AddComponent<FlagSpinner>().EditorSet(null, Vector3.forward, 22f);

            // the shard: two crossed, tilted prisms glowing teal, slowly turning
            var shard = Group("Shard", rig, new Vector3(0, cy, 0));
            Box("Prism_A", shard.transform, Vector3.zero, new Vector3(0.2f, 0.62f, 0.2f), crystal, false, Quaternion.Euler(8f, 45f, 6f), false);
            Box("Prism_B", shard.transform, new Vector3(0.05f, -0.08f, 0), new Vector3(0.13f, 0.4f, 0.13f), crystal, false, Quaternion.Euler(-14f, 20f, 22f), false);
            shard.AddComponent<FlagSpinner>().EditorSet(null, Vector3.up, -16f);
            MakeDynamic(outer);
            MakeDynamic(shard);
            PointLight("ShardGlow", rig, new Vector3(0, cy, 0), TealLight, 2.6f, 5f);
        }

        private static void BuildTrialWall(Transform props)
        {
            var wall = Group("TrialWall", props, new Vector3(HalfW - 0.06f, 0, 1.0f));
            wall.transform.localRotation = Quaternion.Euler(0, 90, 0); // local -Z faces into the room (-X)
            var t = wall.transform;
            Box("Frame", t, new Vector3(0, 2.90f, 0.02f), new Vector3(5.4f, 2.9f, 0.06f), M.Brass, false);
            Box("Screen", t, new Vector3(0, 2.90f, -0.02f), new Vector3(5.2f, 2.7f, 0.02f), _screen, false);
            // slow waveform along the top
            for (int i = 0; i < 40; i++)
            {
                float x = -2.4f + i * 0.12f;
                float h = 0.08f + 0.22f * Mathf.Abs(Mathf.Sin(i * 0.37f) * Mathf.Cos(i * 0.11f));
                var bar = Box($"Wave_{i:00}", t, new Vector3(x, 3.95f, -0.04f), new Vector3(0.06f, h, 0.01f), _screenWave, false, null, false);
                bar.AddComponent<EmissionPulse>().EditorSet(SignalCyan, 0.4f, 2.0f, 40f / 6f, i / 40f);
            }
            WorldText("Title", t, new Vector3(0, 3.55f, -0.05f), Quaternion.identity, "res.board.title", 2.4f, Brass);
            var sent = WorldText("SentHeader", t, new Vector3(-0.6f, 3.2f, -0.05f), Quaternion.identity, "res.board.sent", 1.5f, TealLight);
            ((RectTransform)sent.transform).sizeDelta = new Vector2(2.2f, 0.3f);
            var ans = WorldText("AnsweredHeader", t, new Vector3(1.6f, 3.2f, -0.05f), Quaternion.identity, "res.board.answered", 1.5f, TealLight);
            ((RectTransform)ans.transform).sizeDelta = new Vector2(2.2f, 0.3f);
            for (int i = 0; i <= Trials.Length; i++)
            {
                float y = 2.85f - i * 0.38f;
                bool seven = i == Trials.Length;
                var label = RawText($"Trial_{i}", t, new Vector3(-2.15f, y, -0.05f), Quaternion.identity, $"#{(seven ? 7 : TrialNumbers[i])}", 1.6f, seven ? Amber : Brass);
                ((RectTransform)label.transform).sizeDelta = new Vector2(0.6f, 0.3f);
                var row = seven ? Trial7Sent : Trials[i];
                var s = RawText($"Sent_{i}", t, new Vector3(-0.6f, y, -0.05f), Quaternion.identity, SymbolDecodePuzzle.Row(row), 1.8f, Color.white);
                ((RectTransform)s.transform).sizeDelta = new Vector2(2.4f, 0.32f);
                s.characterSpacing = 0;
                var a = RawText($"Answer_{i}", t, new Vector3(1.6f, y, -0.05f), Quaternion.identity,
                    seven ? "?" : SymbolDecodePuzzle.Row(SymbolSequenceLock.Mirror(row)), 1.8f, seven ? Amber : SignalCyan);
                ((RectTransform)a.transform).sizeDelta = new Vector2(2.4f, 0.32f);
                a.characterSpacing = 0;
            }
            PointLight("ScreenLight", t, new Vector3(0, 2.90f, -1.2f), SignalCyan, 1.6f, 6f);
        }

        private static void BuildDecodeConsole(Transform props)
        {
            var c = Group("DecodeConsole", props, new Vector3(5.1f, 0, 1.0f));
            c.transform.localRotation = Quaternion.Euler(0, 90, 0); // player stands west of it, facing the wall
            var t = c.transform;
            if (PropModels.Place("prop_decode_console", t, Vector3.zero) != null)
                PropModels.Solid(t, new Vector3(0, 0.5f, 0), new Vector3(1.8f, 1.0f, 0.5f));
            else
            {
                Box("Pedestal", t, new Vector3(0, 0.5f, 0), new Vector3(1.8f, 1.0f, 0.5f), M.SlateDark);
                Box("Desk", t, new Vector3(0, 1.05f, -0.05f), new Vector3(1.9f, 0.08f, 0.6f), M.Brass, false, Quaternion.Euler(-15, 0, 0));
            }
            var display = RawText("Display", t, new Vector3(0, 1.36f, 0.2f), Quaternion.identity, "_  _  _  _  _  _", 2.0f, Color.white);
            ((RectTransform)display.transform).sizeDelta = new Vector2(1.8f, 0.3f);
            display.characterSpacing = 0;

            var puzzle = c.AddComponent<SymbolDecodePuzzle>();
            puzzle.EditorConfigurePuzzle(StationContent.DecodePuzzleId, SectorIds.Research, new List<Effect>());
            puzzle.EditorSetDecode(new List<string>(SymbolSequenceLock.Mirror(Trial7Sent)), display, StationContent.NoticedMirror, StationContent.DecodeHintFlag);
            puzzle.EditorSetSolvedSfx("sfx_terminal_ok");

            var keys = new[] { "tri", "cir", "sqr", "dia", SymbolKeyInteractable.ClearKey, SymbolKeyInteractable.SubmitKey };
            for (int i = 0; i < keys.Length; i++)
            {
                float x = -0.75f + i * 0.3f;
                var k = Group($"Key_{keys[i]}", t, new Vector3(x, 1.12f, -0.08f));
                k.transform.localRotation = Quaternion.Euler(-15, 0, 0);
                k.AddComponent<BoxCollider>().size = new Vector3(0.26f, 0.2f, 0.3f);
                bool symbol = i < 4;
                Box("Cap", k.transform, Vector3.zero, new Vector3(0.22f, 0.05f, 0.22f), symbol ? M.Ceramic : (keys[i] == SymbolKeyInteractable.SubmitKey ? M.CyanStrip : M.Terracotta), false, null, false);
                var label = RawText("Glyph", k.transform, new Vector3(0, 0.03f, 0), Quaternion.Euler(90, 0, 0),
                    symbol ? SymbolDecodePuzzle.Glyph(keys[i]) : (keys[i] == SymbolKeyInteractable.SubmitKey ? "OK" : "C"), symbol ? 1.4f : 0.9f, Navy);
                ((RectTransform)label.transform).sizeDelta = new Vector2(0.2f, 0.2f);
                label.characterSpacing = 0;
                var key = k.AddComponent<SymbolKeyInteractable>();
                key.EditorConfigure($"res_key_{keys[i]}", "prompt.use", null, false);
                key.EditorSetKey(puzzle, keys[i]);
            }

            // Rhee's protected files (LOG_RES_02) appear on the console once the decode is solved
            var rec = DockingBayBuilder.BuildRecorder(props, "Recorder_LogRes02", new Vector3(5.3f, 1.12f, 2.25f), Quaternion.Euler(0, 90, 0), StationContent.LogRes02);
            var locked = Group("ProtectedFiles_Locked", props, new Vector3(5.3f, 1.12f, 2.25f));
            Box("Seal", locked.transform, new Vector3(0, 0.03f, 0), new Vector3(0.24f, 0.06f, 0.16f), M.RedLamp, false, null, false);
            Group("ProtectedFiles_Toggle", props).AddComponent<ConditionToggle>().EditorSet(Solved(), new[] { rec }, new[] { locked });
        }

        private static ConditionSet Solved() => new() { conditions = { new Condition { type = ConditionType.PuzzleSolved, key = StationContent.DecodePuzzleId } } };

        // ------------------------------------------------------------------ Rhee's desk, listening booth, safe

        private static void BuildRheeDesk(Transform props)
        {
            var d = Group("RheeDesk", props, new Vector3(-5.9f, 0, -3.5f)).transform;
            if (PropModels.Place("prop_desk", d, Vector3.zero, Quaternion.Euler(0, 180, 0), scale: new Vector3(1f, 1f, 1.3f)) != null)
            {
                PropModels.Solid(d, new Vector3(0, 0.4f, 0), new Vector3(1.1f, 0.82f, 2.6f));
                PropModels.Place("prop_office_chair", props, new Vector3(-4.9f, 0, -3.5f), Quaternion.Euler(0, -90, 0), name: "Chair");
                PropModels.Solid(props.Find("Chair"), new Vector3(0, 0.45f, 0), new Vector3(0.5f, 0.9f, 0.5f));
            }
            else
            {
                Box("Top", d, new Vector3(0, 0.8f, 0), new Vector3(1.1f, 0.06f, 2.6f), M.SlateDark);
                Box("Trim", d, new Vector3(0, 0.77f, 0), new Vector3(1.14f, 0.03f, 2.64f), M.Brass, false);
                Box("Body", d, new Vector3(0.1f, 0.38f, 0), new Vector3(0.9f, 0.76f, 2.4f), _wallDark);
                Box("Chair", props, new Vector3(-4.9f, 0.45f, -3.5f), new Vector3(0.5f, 0.08f, 0.5f), M.Rubber);
            }
            Box("Monitor", d, new Vector3(-0.3f, 1.2f, 0.3f), new Vector3(0.05f, 0.5f, 0.8f), M.SlateDark, false);
            Box("MonitorGlow", d, new Vector3(-0.27f, 1.2f, 0.3f), new Vector3(0.01f, 0.44f, 0.74f), _screen, false);
            PointLight("DeskGlow", d, new Vector3(0.4f, 1.4f, 0), Amber, 0.9f, 3.5f);

            // sticky notes on the wall above the desk: many small ones, one large, one half hidden
            var wall = Group("Notes", props, new Vector3(-HalfW + 0.03f, 1.8f, -3.5f));
            wall.transform.localRotation = Quaternion.Euler(0, -90, 0); // local -Z faces the room (+X)
            for (int i = 0; i < 9; i++)
            {
                var p = new Vector3(-1.1f + (i % 5) * 0.5f + (i / 5) * 0.2f, 0.45f - (i / 5) * 0.55f, 0);
                Box($"Note_{i}", wall.transform, p, new Vector3(0.18f, 0.18f, 0.005f), _note, false, Quaternion.Euler(0, 0, (i * 7 % 11) - 5f));
            }
            Box("BigNote", wall.transform, new Vector3(0.1f, -0.05f, -0.005f), new Vector3(0.78f, 0.46f, 0.005f), _note, false, Quaternion.Euler(0, 0, -2));
            var big = WorldText("BigNoteText", wall.transform, new Vector3(0.1f, -0.05f, -0.012f), Quaternion.Euler(0, 0, -2), "res.note.answers", 1.5f, Navy);
            ((RectTransform)big.transform).sizeDelta = new Vector2(0.68f, 0.36f);
            big.characterSpacing = 2;
            Box("SmallNote", wall.transform, new Vector3(0.74f, -0.4f, 0.002f), new Vector3(0.34f, 0.24f, 0.005f), _note, false, Quaternion.Euler(0, 0, 9));
            var small = WorldText("SmallNoteText", wall.transform, new Vector3(0.75f, -0.4f, -0.004f), Quaternion.Euler(0, 0, 9), "res.note.or_does_it", 0.9f, Navy);
            ((RectTransform)small.transform).sizeDelta = new Vector2(0.3f, 0.2f);
            small.characterSpacing = 0;

            DockingBayBuilder.BuildRecorder(props, "Recorder_LogRes01", new Vector3(-5.75f, 0.84f, -2.6f), Quaternion.Euler(0, 80, 0), StationContent.LogRes01);
        }

        private static void BuildListeningBooth(Transform props)
        {
            var b = Group("ListeningBooth", props, new Vector3(-5.2f, 0, 4.4f)).transform;
            // three-sided booth open to the east (Blender model; the boxes stay as colliders / greybox fallback)
            bool boothModel = PropModels.Place("prop_listening_booth", b, Vector3.zero) != null;
            HideRenderers(Box("Back", b, new Vector3(-1.6f, 1.3f, 0), new Vector3(0.1f, 2.6f, 2.6f), M.Terracotta), boothModel);
            HideRenderers(Box("Side_S", b, new Vector3(-0.4f, 1.3f, -1.3f), new Vector3(2.4f, 2.6f, 0.1f), M.Terracotta), boothModel);
            HideRenderers(Box("Side_N", b, new Vector3(-0.4f, 1.3f, 1.3f), new Vector3(2.4f, 2.6f, 0.1f), M.Terracotta), boothModel);
            if (!boothModel) Box("Roof", b, new Vector3(-0.4f, 2.62f, 0), new Vector3(2.4f, 0.06f, 2.7f), M.Brass, false);
            Box("Seat", b, new Vector3(-0.8f, 0.45f, 0), new Vector3(0.55f, 0.08f, 0.55f), M.Rubber);
            Box("SeatBack", b, new Vector3(-1.05f, 0.8f, 0), new Vector3(0.06f, 0.7f, 0.5f), M.Rubber, false);
            Box("Shelf", b, new Vector3(-1.4f, 1.0f, 0), new Vector3(0.35f, 0.05f, 1.2f), M.SlateDark, false);
            // headphones hanging from a hook
            Cylinder("Hook", b, new Vector3(-1.5f, 1.75f, 0.6f), new Vector3(0.02f, 0.08f, 0.02f), M.Brass, false, Quaternion.Euler(0, 0, 90));
            var hp = Group("Headphones", b, new Vector3(-1.45f, 1.55f, 0.6f)).transform;
            Ring("Band", hp, Vector3.zero, 0.11f, 0.02f, 0.03f, 10, M.Rubber, 0f, 180f);
            Sphere("Cup_L", hp, new Vector3(-0.11f, -0.02f, 0), new Vector3(0.08f, 0.09f, 0.05f), M.Rubber);
            Sphere("Cup_R", hp, new Vector3(0.11f, -0.02f, 0), new Vector3(0.08f, 0.09f, 0.05f), M.Rubber);
            // tally marks on the inside of the south wall: 31 nights
            var tally = WorldText("Tally", b, new Vector3(-0.5f, 1.55f, -1.24f), Quaternion.Euler(0, 180, 0), "res.tally", 1.2f, Hex("#C9D3D8"));
            ((RectTransform)tally.transform).sizeDelta = new Vector2(1.6f, 0.6f);
            tally.characterSpacing = 2;
            PointLight("BoothLight", b, new Vector3(-0.6f, 2.3f, 0), Hex("#7FB7D8"), 0.9f, 3f);

            var termGo = Group("BoothTerminal", b, new Vector3(-1.4f, 1.2f, -0.4f));
            termGo.transform.localRotation = Quaternion.Euler(0, -90, 0);
            termGo.AddComponent<BoxCollider>().size = new Vector3(0.6f, 0.45f, 0.3f);
            Box("Screen", termGo.transform, Vector3.zero, new Vector3(0.5f, 0.35f, 0.04f), M.SlateDark, false, null, false);
            var glow = Box("Glow", termGo.transform, new Vector3(0, 0, -0.025f), new Vector3(0.46f, 0.31f, 0.005f), _screen, false, null, false);
            var term = termGo.AddComponent<TerminalInteractable>();
            term.EditorConfigure("res_booth", "prompt.terminal.use", null, false);
            term.EditorSetTerminal(StationContent.BoothTerminal, glow);

            DockingBayBuilder.BuildRecorder(props, "Recorder_LogRes03", new Vector3(-6.55f, 1.04f, 4.65f), Quaternion.Euler(0, 90, 0), StationContent.LogRes03);
        }

        private static void BuildSafe(Transform props)
        {
            var s = Group("SpecimenSafe", props, new Vector3(4.6f, 0, HalfL - 0.5f)).transform;
            bool caseModel = PropModels.Place("prop_safe_case", s, Vector3.zero) != null;
            HideRenderers(HollowCase("Body", s, new Vector3(0, 0.75f, 0), new Vector3(1.1f, 1.5f, 0.7f), M.SlateDark, 0.08f), caseModel); // hollow: keycard reachable once open
            Box("Trim", s, new Vector3(0, 1.52f, 0), new Vector3(1.16f, 0.05f, 0.76f), M.Brass, false);
            var label = WorldText("Label", s, new Vector3(0, 1.75f, -0.36f), Quaternion.identity, "res.safe.label", 1.0f, Brass);
            ((RectTransform)label.transform).sizeDelta = new Vector2(1.6f, 0.25f);
            label.characterSpacing = 2;
            var closed = Box("Door_Closed", s, new Vector3(0, 0.75f, -0.37f), new Vector3(1.04f, 1.4f, 0.05f), M.Brass, true, null, false);
            var keypad = RawText("SymbolPad", closed.transform, new Vector3(0, 0.2f, -0.6f), Quaternion.identity,
                SymbolDecodePuzzle.Row(new[] { "tri", "cir", "sqr", "dia" }), 0.9f, Navy);
            keypad.transform.localScale = new Vector3(1f / 1.04f, 1f / 1.4f, 1f / 0.05f);
            var open = Box("Door_Open", s, new Vector3(-0.85f, 0.75f, -0.75f), new Vector3(1.04f, 1.4f, 0.05f), M.Brass, false, Quaternion.Euler(0, 80, 0), false);
            Box("Shelf", s, new Vector3(0, 0.9f, -0.05f), new Vector3(0.95f, 0.04f, 0.55f), M.Ceramic, false);
            s.gameObject.AddComponent<ConditionToggle>().EditorSet(Solved(), new[] { open }, new[] { closed });

            var pickupGo = Group("Pickup_KeycardCore", s, new Vector3(0, 0.95f, -0.1f));
            pickupGo.AddComponent<BoxCollider>().size = new Vector3(0.4f, 0.15f, 0.35f);
            MedicalBuilder.Keycard(pickupGo.transform, Amber);
            var pickup = pickupGo.AddComponent<PickupInteractable>();
            pickup.EditorConfigure("res_pickup_keycard_core", "prompt.pickup", StationContent.KeycardCore.NameKey, true,
                new ConditionSet { conditions = { new Condition { type = ConditionType.PuzzleSolved, key = StationContent.DecodePuzzleId, failReasonKey = "res.safe.locked" } } });
            pickup.EditorSetItem(StationContent.KeycardCore);
            PointLight("SafeGlow", s, new Vector3(0, 1.3f, -0.8f), Amber, 0.6f, 2.5f);

            var inspect = new GameObject("Inspect_KeycardCore");
            MedicalBuilder.Keycard(inspect.transform, Amber);
            DockingBayContent.SetInspectPrefab(StationContent.KeycardCore,
                PrefabUtility.SaveAsPrefabAsset(inspect, "Assets/_Project/Prefabs/Interactables/Inspect/Inspect_KeycardCore.prefab"));
            Object.DestroyImmediate(inspect);
        }

        // ------------------------------------------------------------------ hatches, triggers

        private static void BuildHatches(Transform props, Transform root)
        {
            MedicalBuilder.Hatch(props, "Hatch_ToMedical", new Vector3(0, 1.45f, -HalfL + 0.06f), 180f, SceneIds.Medical, "from_research",
                "sector.medical.name", SectorIds.Medical, null, M.Ceramic);
            WorldText("Sign_Medical", props, new Vector3(0, 3.1f, -HalfL + 0.04f), Quaternion.Euler(0, 180, 0), "sector.medical.name", 2.2f, TealLight);
            MedicalBuilder.Hatch(props, "Hatch_ToReactor", new Vector3(HalfW - 0.06f, 1.45f, -4.6f), 90f, SceneIds.ReactorCommand, "from_research",
                "sector.reactor.name", SectorIds.Reactor,
                new ConditionSet { conditions = { Condition.Item(StationContent.KeycardCore, "res.door.core_keycard") } }, M.Terracotta);
            WorldText("Sign_Reactor", props, new Vector3(HalfW - 0.04f, 3.1f, -4.6f), Quaternion.Euler(0, 90, 0), "sector.reactor.name", 2.2f, Amber);

            MedicalBuilder.Spawn(root, "from_medical", new Vector3(0, 0.05f, -6.6f), 0f);
            MedicalBuilder.Spawn(root, "default", new Vector3(0, 0.05f, -6.6f), 0f);
            MedicalBuilder.Spawn(root, "from_reactor", new Vector3(5.4f, 0.05f, -4.6f), -90f);
        }

        private static void BuildDialogueTriggers(Transform root)
        {
            var t = Group("DialogueTriggers", root).transform;
            Group("AI_RES_01", t).AddComponent<DialogueTrigger>()
                .EditorSet(DialogueContent.AiRes01, DialogueTriggerMode.OnSectorStart, null, null, 3.0f);
            Group("AI_RES_HINT", t).AddComponent<DialogueTrigger>()
                .EditorSet(DialogueContent.AiResHint, DialogueTriggerMode.OnFlag, StationContent.DecodeHintFlag, null, 0.6f);
            Group("AI_RES_02", t).AddComponent<DialogueTrigger>()
                .EditorSet(DialogueContent.AiRes02, DialogueTriggerMode.OnFlag, WorldKeys.PuzzleSolved(StationContent.DecodePuzzleId), null, 1.2f);
        }
    }
}
