using System.Collections.Generic;
using System.IO;
using LastSignal.Core;
using LastSignal.Cutscene;
using LastSignal.Dialogue;
using LastSignal.Endings;
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
    /// Code-built finale (StoryBible §6.5, §9): the Reactor Core shaft (catwalk, three coolant valves, output
    /// gauges, cold-blue core) and, above it, the Command Center (captain's chair and console, comms console,
    /// CAIRN's private archive, window onto Nereth), joined by a lift that powers up once the reactor is stable.
    /// Ending A escape route: lift down → emergency service tunnel (sealed by CAIRN at low trust: manual crank) →
    /// Docking Bay. Overwrites Sector_ReactorCommand.unity.
    /// </summary>
    public static class ReactorCommandBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Station/Sector_ReactorCommand.unity";
        private const float R = 7f, ShaftH = 9f;
        private static readonly Vector3 CommandOrigin = new(0f, 20f, 0f);
        private const float CmdHalfW = 7f, CmdHalfL = 6f, CmdH = 4f;

        private static Material _coreGlow, _frostRail, _amberLamp, _console, _consoleGlow;

        [MenuItem("Last Signal/Build Content/Reactor + Command (overwrites scene)")]
        public static void Build()
        {
            StationMaterials.Create();
            _coreGlow = Emissive("M_Emissive_CoreBlue", Hex("#3C7FC4"), 0.5f, Hex("#14263D"));
            _frostRail = Lit("M_Rail_Frost", Color.Lerp(MetalGrey, Color.white, 0.35f), 0.7f, 0.6f);
            _amberLamp = Emissive("M_Emissive_CommandAmber", Amber, 1.6f, Amber);
            _console = Lit("M_Console_Dark", Color.Lerp(Navy, Slate, 0.6f), 0.4f, 0.2f);
            _consoleGlow = Emissive("M_Console_Glow", Color.Lerp(Teal, Navy, 0.35f), 0.45f, Color.Lerp(Teal, Navy, 0.6f));
            DockingBayContent.CreateAll();
            var playerPrefab = PlayerPrefabBuilder.Build();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("[Generated] Sector_ReactorCommand");
            root.AddComponent<SectorRoot>().EditorConfigure(SectorIds.Reactor, playerPrefab);
            var on = new List<GameObject>();
            var off = new List<GameObject>();

            var reactor = Group("ReactorCore", root.transform).transform;
            var coreLight = BuildShaft(reactor, on, off);
            BuildBalanceConsole(reactor, coreLight);
            BuildLift(reactor, "Lift_Up", new Vector3(4.6f, 0, 4.6f), 45f, "command", "sector.command.name", true);
            BuildServiceTunnel(reactor);

            var command = Group("CommandCenter", root.transform, CommandOrigin).transform;
            BuildCommandRoom(command, on, off);
            BuildCommandProps(command);
            BuildLift(command, "Lift_Down", new Vector3(-5.6f, 0, -4.4f), 232f, "reactor_lift", "sector.reactor.name", false);

            MedicalBuilder.Hatch(reactor, "Hatch_ToResearch", new Vector3(0, 1.45f, -R + 0.15f), 180f, SceneIds.Research, "from_reactor",
                "sector.research.name", SectorIds.Research, null, M.Terracotta);
            MedicalBuilder.Spawn(root.transform, "from_research", new Vector3(0, 0.05f, -5.3f), 0f);
            MedicalBuilder.Spawn(root.transform, "default", new Vector3(0, 0.05f, -5.3f), 0f);
            MedicalBuilder.Spawn(root.transform, "reactor_lift", new Vector3(3.4f, 0.05f, 3.4f), -135f);
            MedicalBuilder.Spawn(root.transform, "command", CommandOrigin + new Vector3(-4.4f, 0.05f, -3.2f), 45f);
            BuildDialogueTriggers(root.transform);

            var power = Group("PowerController", root.transform);
            power.AddComponent<PoweredObject>().EditorConfigure(SectorIds.Reactor, on.ToArray(), off.ToArray());
            foreach (var go in on) go.SetActive(false);

            DockingBayBuilder.SetupLightingAndPost(root.transform, dockingProbes: false);
            root.AddComponent<LastSignal.Audio.SectorAmbience>().EditorSet(new List<LastSignal.Audio.AmbienceLoop>
            {
                new() { clip = "amb_station_hum", volume = 0.4f },
                new() { clip = "amb_generator", volume = 0.35f, whileFlag = WorldKeys.Power(SectorIds.Reactor) },
                new() { clip = "amb_murmur", volume = 0.2f },
            });
            RenderSettings.ambientSkyColor = Color.Lerp(Navy, Hex("#4FA8FF"), 0.12f) * 0.6f;
            RenderSettings.ambientEquatorColor = Color.Lerp(Navy, Teal, 0.1f) * 0.5f;
            RenderSettings.fogColor = Color.Lerp(Navy, Hex("#10243A"), 0.5f);
            RenderSettings.fogDensity = 0.022f;
            foreach (var (name, pos, size) in new[]
                     {
                         ("ReflectionProbe_Reactor", new Vector3(0, 3f, 0), new Vector3(14.4f, 9f, 14.4f)),
                         ("ReflectionProbe_Command", CommandOrigin + new Vector3(0, 2f, 0), new Vector3(14.4f, 4.2f, 12.4f)),
                     })
            {
                var probe = Group(name, root.transform, pos).AddComponent<ReflectionProbe>();
                probe.mode = ReflectionProbeMode.Baked;
                probe.size = size;
                probe.boxProjection = true;
                probe.resolution = 256;
                probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
                probe.backgroundColor = Navy * 0.4f;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)!);
            EditorSceneManager.SaveScene(scene, ScenePath);
            DockingBayBuilder.BakeReflectionProbes(on, ScenePath);
            EditorSceneManager.SaveScene(scene, ScenePath);
            ProjectSetup.RegisterBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log($"[ReactorCommandBuilder] Built {ScenePath}");
        }

        // ------------------------------------------------------------------ reactor shaft

        private static Light BuildShaft(Transform t, List<GameObject> on, List<GameObject> off)
        {
            var s = Group("Shaft", t).transform;
            Box("Catwalk", s, new Vector3(0, -0.1f, 0), new Vector3(R * 2, 0.2f, R * 2), M.Floor);
            Box("Ceiling", s, new Vector3(0, ShaftH + 0.1f, 0), new Vector3(R * 2, 0.2f, R * 2), M.Ceiling);
            // 12-sided shaft wall
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f;
                var dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                Box($"Wall_{i:00}", s, dir * (R + 0.1f) + Vector3.up * (ShaftH / 2), new Vector3(3.9f, ShaftH, 0.2f), M.Wall, true, Quaternion.Euler(0, a, 0));
                Box($"Rib_{i:00}", s, dir * (R - 0.05f) + Vector3.up * (ShaftH / 2) + Quaternion.Euler(0, a, 0) * Vector3.right * 1.9f,
                    new Vector3(0.18f, ShaftH, 0.16f), M.Brass, false, Quaternion.Euler(0, a, 0));
            }
            // the core: a tall column through the catwalk, cold blue
            Cylinder("CoreHousing", s, new Vector3(0, ShaftH / 2, 0), new Vector3(3.6f, ShaftH / 2, 3.6f), M.SlateDark);
            var glow = Cylinder("CoreGlow", s, new Vector3(0, ShaftH / 2, 0), new Vector3(3.7f, ShaftH / 2 - 0.6f, 3.7f), _coreGlow, false, null, false);
            for (float y = 1.2f; y < ShaftH; y += 1.6f)
                Cylinder($"CoreBand_{y:0.0}", s, new Vector3(0, y, 0), new Vector3(3.85f, 0.08f, 3.85f), M.Brass, false);
            var coreLight = PointLight("CoreLight", s, new Vector3(0, 2.2f, 0), Hex("#6FB8FF"), 1.5f, 14f, LightShadows.Soft);
            glow.AddComponent<EmissionPulse>().EditorSet(Hex("#3C7FC4"), 0.15f, 0.65f, 6f, 0f); // safe shutdown: dim, slow
            // safety railing around the core (frosted)
            for (int i = 0; i < 16; i++)
            {
                float a = i * 22.5f * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * 2.6f;
                Cylinder($"Post_{i:00}", s, p + Vector3.up * 0.55f, new Vector3(0.06f, 0.55f, 0.06f), _frostRail);
            }
            Ring("RailTop", s, new Vector3(0, 1.1f, 0), 2.6f, 0.06f, 0.06f, 24, _frostRail, 0, 360, false).transform.localRotation = Quaternion.Euler(90, 0, 0);
            Ring("RailMid", s, new Vector3(0, 0.6f, 0), 2.6f, 0.04f, 0.04f, 24, _frostRail, 0, 360, false).transform.localRotation = Quaternion.Euler(90, 0, 0);
            // invisible barrier so nobody climbs over the rail
            var barrier = Cylinder("RailBarrier", s, new Vector3(0, 1.5f, 0), new Vector3(5.3f, 1.5f, 5.3f), M.SlateDark);
            Object.DestroyImmediate(barrier.GetComponent<Renderer>());

            WorldText("Warning_N", s, new Vector3(0, 2.6f, R - 0.05f), Quaternion.identity, "reactor.warning", 1.6f, Amber);
            WorldText("Warning_S", s, new Vector3(0, 3.2f, -R + 0.05f), Quaternion.Euler(0, 180, 0), "reactor.warning", 1.6f, Amber);

            // lamps: dim emergency until stable, then working lights
            for (int i = 0; i < 4; i++)
            {
                float a = (45f + i * 90f) * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * (R - 0.4f) + Vector3.up * 4.2f;
                Box($"Lamp_{i}", s, p, new Vector3(0.5f, 0.15f, 0.5f), M.LampOff, false);
                on.Add(PointLight($"WorkLight_{i}", s, p + Vector3.down * 0.4f, Hex("#D8F3F5"), 3.5f, 9f).gameObject);
                off.Add(PointLight($"EmergencyLight_{i}", s, p + Vector3.down * 0.4f, EmergencyRed, 1.2f, 7f).gameObject);
            }
            return coreLight;
        }

        private static void BuildBalanceConsole(Transform t, Light coreLight)
        {
            var c = Group("BalanceConsole", t, new Vector3(-3.8f, 0, -4.0f));
            var ct = c.transform;
            Box("Pedestal", ct, new Vector3(0, 0.55f, 0), new Vector3(2.0f, 1.1f, 0.5f), M.SlateDark);
            Box("Panel", ct, new Vector3(0, 1.6f, 0.1f), new Vector3(2.1f, 1.1f, 0.08f), M.Brass, false);
            Box("PanelFace", ct, new Vector3(0, 1.6f, 0.05f), new Vector3(2.0f, 1.0f, 0.02f), _console, false);
            var title = WorldText("Title", ct, new Vector3(0, 2.32f, 0.03f), Quaternion.identity, "reactor.panel.title", 1.1f, Brass);
            ((RectTransform)title.transform).sizeDelta = new Vector2(2.4f, 0.25f);
            title.characterSpacing = 2;
            Transform Needle(string name, float x)
            {
                var dial = Group($"{name}_Dial", ct, new Vector3(x, 1.7f, 0.03f)).transform;
                Cylinder("Face", dial, Vector3.zero, new Vector3(0.6f, 0.01f, 0.6f), M.Ceramic, false, Quaternion.Euler(90, 0, 0));
                Box("Green", dial, new Vector3(0, 0.22f, -0.012f), new Vector3(0.22f, 0.05f, 0.005f), M.CyanStrip, false);
                var pivot = Group("Needle", dial, new Vector3(0, 0, -0.02f)).transform;
                Box("Hand", pivot, new Vector3(0, 0.13f, 0), new Vector3(0.02f, 0.26f, 0.005f), M.RedLamp, false, null, false);
                return pivot;
            }
            var outNeedle = Needle("Output", -0.5f);
            var tempNeedle = Needle("Temperature", 0.5f);
            var outText = WorldText("OutputText", ct, new Vector3(-0.5f, 1.25f, 0.03f), Quaternion.identity, "reactor.gauge.output", 1.0f, Color.white);
            ((RectTransform)outText.transform).sizeDelta = new Vector2(0.95f, 0.2f);
            outText.characterSpacing = 1;
            var tempText = WorldText("TemperatureText", ct, new Vector3(0.5f, 1.25f, 0.03f), Quaternion.identity, "reactor.gauge.temperature", 1.0f, Color.white);
            ((RectTransform)tempText.transform).sizeDelta = new Vector2(0.95f, 0.2f);
            tempText.characterSpacing = 1;
            // the gauge texts are driven by the puzzle; remove the static localization so it does not overwrite them
            Object.DestroyImmediate(outText.GetComponent<LocalizedText>());
            Object.DestroyImmediate(tempText.GetComponent<LocalizedText>());

            // the face (local -Z) points at the core / the player coming in from the south hatch
            c.transform.localRotation = Quaternion.Euler(0, 224f, 0);

            var puzzle = c.AddComponent<ReactorBalancePuzzle>();
            puzzle.EditorConfigurePuzzle(StationContent.ReactorPuzzleId, SectorIds.Reactor, new List<Effect> { Effect.Power(SectorIds.Reactor) });
            puzzle.EditorSetSolvedSfx("sfx_power_up");
            puzzle.EditorBindGauges(outText, tempText, outNeedle, tempNeedle, coreLight);

            // three coolant loop valves on the shaft wall: west, north, east
            var spots = new[] { (-90f, "reactor.valve.1"), (0f, "reactor.valve.2"), (90f, "reactor.valve.3") };
            for (int i = 0; i < spots.Length; i++)
            {
                var (yaw, label) = spots[i];
                var dir = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
                var v = Group($"Valve_{i + 1}", t, dir * (R - 0.35f) + Vector3.up * 1.2f);
                v.transform.localRotation = Quaternion.Euler(0, yaw, 0); // local +Z into the wall, wheel (-Z) faces the catwalk
                bool modelBody = PropModels.Place("prop_valve_body", v.transform, new Vector3(0, 0, -0.05f)) != null;
                if (!modelBody) Cylinder("Pipe", v.transform, new Vector3(0, 0, 0.25f), new Vector3(0.3f, 0.25f, 0.3f), M.Brass, false, Quaternion.Euler(90, 0, 0));
                Cylinder("PipeUp", v.transform, new Vector3(0, 1.8f, 0.3f), new Vector3(0.22f, 1.8f, 0.22f), M.Brass, false);
                Cylinder("PipeDown", v.transform, new Vector3(0, -0.6f, 0.3f), new Vector3(0.22f, 0.6f, 0.22f), M.Brass, false);
                var wheel = Group("Wheel", v.transform, new Vector3(0, 0, -0.05f)).transform;
                if (PropModels.Place("prop_valve_wheel", wheel, Vector3.zero) != null)
                    Box("Marker", wheel, new Vector3(0, 0.3f, -0.05f), new Vector3(0.05f, 0.09f, 0.02f), M.CyanStrip, false, null, false);
                else
                {
                    Ring("Rim", wheel, Vector3.zero, 0.32f, 0.05f, 0.06f, 16, M.Terracotta);
                    Box("Spoke_A", wheel, Vector3.zero, new Vector3(0.62f, 0.04f, 0.04f), M.Terracotta, false, null, false);
                    Box("Spoke_B", wheel, Vector3.zero, new Vector3(0.04f, 0.62f, 0.04f), M.Terracotta, false, null, false);
                    Box("Marker", wheel, new Vector3(0, 0.3f, -0.03f), new Vector3(0.06f, 0.1f, 0.02f), M.CyanStrip, false, null, false);
                }
                var tag = WorldText("Label", v.transform, new Vector3(0, 0.6f, -0.05f), Quaternion.identity, label, 1.0f, Brass);
                ((RectTransform)tag.transform).sizeDelta = new Vector2(1.6f, 0.2f);
                tag.characterSpacing = 1;
                v.AddComponent<BoxCollider>().size = new Vector3(0.8f, 0.8f, 0.5f);
                var valve = v.AddComponent<ValveInteractable>();
                valve.EditorConfigure($"core_valve_{i + 1}", "prompt.valve", label, false);
                valve.EditorSetValve(puzzle, i, wheel, label);
            }
        }

        private static void BuildLift(Transform t, string name, Vector3 pos, float yaw, string spawnId, string destinationKey, bool needsPower)
        {
            var l = Group(name, t, pos);
            l.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var lt = l.transform;
            Box("Platform", lt, new Vector3(0, 0.05f, 0), new Vector3(1.8f, 0.1f, 1.8f), M.Brass, false);
            foreach (var (x, z) in new[] { (-0.85f, -0.85f), (0.85f, -0.85f), (-0.85f, 0.85f), (0.85f, 0.85f) })
                Box($"Post_{x}_{z}", lt, new Vector3(x, 1.5f, z), new Vector3(0.1f, 3f, 0.1f), M.Brass, false);
            Box("Roof", lt, new Vector3(0, 3.02f, 0), new Vector3(1.9f, 0.08f, 1.9f), M.SlateDark, false);
            var panel = Group("CallPanel", lt, new Vector3(0.85f, 1.3f, -0.95f));
            Box("Body", panel.transform, Vector3.zero, new Vector3(0.3f, 0.45f, 0.08f), M.SlateDark, false, null, false);
            var lamp = Sphere("Lamp", panel.transform, new Vector3(0, 0.1f, -0.05f), Vector3.one * 0.07f, M.CyanStrip, false, false);
            panel.AddComponent<BoxCollider>().size = new Vector3(0.5f, 0.6f, 0.3f);
            var lift = panel.AddComponent<LiftInteractable>();
            var reqs = needsPower ? new ConditionSet { conditions = { Condition.Powered(SectorIds.Reactor, "reactor.lift.no_power") } } : null;
            var fx = needsPower ? new List<Effect> { Effect.Flag(StationContent.CommandReachedFlag) } : null;
            lift.EditorConfigure($"core_{name.ToLowerInvariant()}", "prompt.lift", destinationKey, false, reqs, fx);
            lift.EditorSetLift(spawnId, destinationKey);
            if (needsPower)
                panel.AddComponent<ConditionToggle>().EditorSet(new ConditionSet { conditions = { Condition.Powered(SectorIds.Reactor) } }, new[] { lamp }, null);
        }

        private static void BuildServiceTunnel(Transform t)
        {
            float yaw = 225f; // south-west wall
            var dir = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            var choiceDestroy = new Condition { type = ConditionType.StringEquals, key = EndingScript.ChoiceKey, text = EndingScript.Id(EndingType.Destroy), failReasonKey = "reactor.tunnel.sealed" };
            MedicalBuilder.Hatch(t, "Hatch_ServiceTunnel", dir * (R - 0.12f) + Vector3.up * 1.45f, yaw, SceneIds.DockingBay, "from_service_tunnel",
                "sector.docking.name", SectorIds.Docking,
                new ConditionSet { conditions = { choiceDestroy, Condition.Flag(EscapeSequence.TunnelOpenFlag, "reactor.tunnel.cairn_sealed") } }, M.SlateDark);
            var label = WorldText("TunnelLabel", t, dir * (R - 0.08f) + Vector3.up * 3.0f, Quaternion.Euler(0, yaw, 0), "reactor.tunnel.label", 1.3f, EmergencyRed);
            ((RectTransform)label.transform).sizeDelta = new Vector2(3.6f, 0.3f);
            label.characterSpacing = 2;

            // manual crank beside the tunnel (only while escaping and CAIRN keeps it sealed)
            var side = Quaternion.Euler(0, yaw, 0) * Vector3.right;
            var crankGo = Group("ManualCrank", t, dir * (R - 0.3f) + side * 1.8f + Vector3.up * 1.2f);
            crankGo.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var c = crankGo.transform;
            Box("Housing", c, new Vector3(0, 0, 0.1f), new Vector3(0.4f, 0.4f, 0.2f), M.SlateDark, false, null, false);
            var handle = Group("Handle", c, new Vector3(0, 0, -0.05f)).transform;
            Box("Arm", handle, new Vector3(0, 0.15f, 0), new Vector3(0.06f, 0.3f, 0.04f), M.Brass, false, null, false);
            Cylinder("Grip", handle, new Vector3(0, 0.3f, -0.08f), new Vector3(0.06f, 0.08f, 0.06f), M.Rubber, false, Quaternion.Euler(90, 0, 0), false);
            crankGo.AddComponent<BoxCollider>().size = new Vector3(0.6f, 0.8f, 0.4f);
            var crank = crankGo.AddComponent<CrankInteractable>();
            crank.EditorConfigure("core_tunnel_crank", "prompt.crank", null, false);
            crank.EditorSetCrank(6, handle, new List<Effect> { Effect.Flag(EscapeSequence.TunnelOpenFlag) });
            Group("ManualCrank_Toggle", t).AddComponent<ConditionToggle>().EditorSet(
                new ConditionSet { conditions = { Condition.Flag(EscapeSequence.ActiveFlag), new Condition { type = ConditionType.FlagFalse, key = EscapeSequence.TunnelOpenFlag } } },
                new[] { crankGo }, null);
        }

        // ------------------------------------------------------------------ Command Center

        private static void BuildCommandRoom(Transform c, List<GameObject> on, List<GameObject> off)
        {
            var r = Group("Room", c).transform;
            Box("Floor", r, new Vector3(0, -0.1f, 0), new Vector3(CmdHalfW * 2, 0.2f, CmdHalfL * 2), M.Floor);
            Box("Ceiling", r, new Vector3(0, CmdH + 0.1f, 0), new Vector3(CmdHalfW * 2, 0.2f, CmdHalfL * 2), M.Ceiling);
            Box("Wall_W", r, new Vector3(-CmdHalfW - 0.1f, CmdH / 2, 0), new Vector3(0.2f, CmdH + 0.2f, CmdHalfL * 2), M.Wall);
            Box("Wall_E", r, new Vector3(CmdHalfW + 0.1f, CmdH / 2, 0), new Vector3(0.2f, CmdH + 0.2f, CmdHalfL * 2), M.Wall);
            Box("Wall_S", r, new Vector3(0, CmdH / 2, -CmdHalfL - 0.1f), new Vector3(CmdHalfW * 2, CmdH + 0.2f, 0.2f), M.Wall);
            // big window onto Nereth (north)
            Box("Wall_N_Low", r, new Vector3(0, 0.45f, CmdHalfL + 0.1f), new Vector3(CmdHalfW * 2, 0.9f, 0.2f), M.Wall);
            Box("Wall_N_High", r, new Vector3(0, 3.75f, CmdHalfL + 0.1f), new Vector3(CmdHalfW * 2, 0.7f, 0.2f), M.Wall);
            Box("Glass", r, new Vector3(0, 2.15f, CmdHalfL + 0.1f), new Vector3(CmdHalfW * 2, 2.5f, 0.06f), M.GlassTeal);
            for (float x = -5.25f; x <= 5.3f; x += 3.5f)
                Box($"Mullion_{x:0.0}", r, new Vector3(x, 2.15f, CmdHalfL + 0.02f), new Vector3(0.12f, 2.5f, 0.14f), M.Brass, false);
            Box("Space", r, new Vector3(0, 2f, CmdHalfL + 80f), new Vector3(300f, 180f, 0.2f), M.SlateDark, false);
            var planet = Sphere("Nereth", r, new Vector3(0, -14f, CmdHalfL + 26f), new Vector3(48f, 48f, 48f),
                Emissive("M_Nereth_Night", Hex("#1E4E7A"), 0.5f, Hex("#0B1A2A")));
            planet.AddComponent<EmissionPulse>().EditorSet(Hex("#2F8FD8"), 0.15f, 1.2f, 40f / 6f, 0.3f);
            Box("Band", r, new Vector3(0, 1.0f, -CmdHalfL + 0.03f), new Vector3(CmdHalfW * 2, 0.18f, 0.05f), M.Terracotta, false);
            PropModels.PipeRun(r, "Pipes_W", new Vector3(-CmdHalfW, CmdH - 0.55f, -CmdHalfL), new Vector3(-CmdHalfW, CmdH - 0.55f, CmdHalfL), -90f);
            PropModels.PipeRun(r, "Pipes_E", new Vector3(CmdHalfW, CmdH - 0.55f, -CmdHalfL), new Vector3(CmdHalfW, CmdH - 0.55f, CmdHalfL), 90f);

            int i = 0;
            foreach (var p in new[] { new Vector3(-3.5f, CmdH - 0.05f, -2f), new Vector3(3.5f, CmdH - 0.05f, -2f), new Vector3(0, CmdH - 0.05f, 2f) })
            {
                Box($"Lamp_{i}", r, p, new Vector3(1.0f, 0.05f, 1.0f), M.LampOff, false);
                on.Add(Box($"Lamp_{i}_On", r, p + Vector3.down * 0.005f, new Vector3(1.01f, 0.05f, 1.01f), _amberLamp, false, null, false));
                on.Add(PointLight($"Lamp_{i}_Light", r, p + Vector3.down * 0.5f, Hex("#FFC891"), 3.0f, 8f, i == 2 ? LightShadows.Soft : LightShadows.None).gameObject);
                i++;
            }
            off.Add(PointLight("Emergency", r, new Vector3(0, 3.2f, -4f), EmergencyRed, 1.2f, 9f).gameObject);
            PointLight("PlanetFill", r, new Vector3(0, 1.8f, CmdHalfL - 0.8f), Hex("#5A8FD0"), 1.3f, 8f);
        }

        /// <summary>Bridge workstation from the Blender model; false when the model is missing (greybox fallback).</summary>
        private static bool StationConsole(Transform con)
        {
            if (PropModels.Place("prop_console_station", con, Vector3.zero) == null) return false;
            Box("Glow", con, new Vector3(0, 1.3f, 0.175f), new Vector3(1.12f, 0.48f, 0.005f), _consoleGlow, false, Quaternion.Euler(-8, 0, 0), false);
            var col = Group("Collider", con).AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.8f, 0.05f);
            col.size = new Vector3(1.5f, 1.6f, 0.7f);
            return true;
        }

        private static void BuildCommandProps(Transform c)
        {
            var p = Group("Props", c).transform;
            // ring of consoles facing the window
            for (int i = 0; i < 5; i++)
            {
                float a = (-50f + i * 25f) * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Sin(a) * 4.6f, 0, Mathf.Cos(a) * 4.6f - 0.6f);
                var con = Group($"Console_{i}", p, pos).transform;
                con.localRotation = Quaternion.Euler(0, a * Mathf.Rad2Deg, 0);
                if (StationConsole(con)) continue;
                Box("Body", con, new Vector3(0, 0.45f, 0), new Vector3(1.4f, 0.9f, 0.6f), _console);
                Box("Top", con, new Vector3(0, 0.95f, -0.05f), new Vector3(1.5f, 0.06f, 0.7f), M.Brass, false, Quaternion.Euler(-12, 0, 0));
                Box("Screen", con, new Vector3(0, 1.3f, 0.2f), new Vector3(1.2f, 0.55f, 0.04f), M.SlateDark, false, Quaternion.Euler(-8, 0, 0));
                Box("Glow", con, new Vector3(0, 1.3f, 0.175f), new Vector3(1.12f, 0.48f, 0.005f), _consoleGlow, false, Quaternion.Euler(-8, 0, 0));
            }

            // captain's chair with the notebook recorder (LOG_CORE_01)
            var chair = Group("CaptainChair", p, new Vector3(0, 0, -1.2f)).transform;
            if (PropModels.Place("prop_captain_chair", chair, Vector3.zero) != null)
            {
                // seat and back separately: the notebook recorder (LOG_CORE_01) rests on the seat and must stay reachable
                var seatCol = Group("Collider_Seat", chair).AddComponent<BoxCollider>();
                seatCol.center = new Vector3(0, 0.3f, 0);
                seatCol.size = new Vector3(0.75f, 0.6f, 0.7f);
                var backCol = Group("Collider_Back", chair).AddComponent<BoxCollider>();
                backCol.center = new Vector3(0, 1.1f, -0.34f);
                backCol.size = new Vector3(0.75f, 1.0f, 0.2f);
            }
            else
            {
            Cylinder("Base", chair, new Vector3(0, 0.25f, 0), new Vector3(0.5f, 0.25f, 0.5f), M.Brass);
            Box("Seat", chair, new Vector3(0, 0.55f, 0), new Vector3(0.75f, 0.12f, 0.7f), M.Terracotta);
            Box("Back", chair, new Vector3(0, 1.1f, -0.33f), new Vector3(0.75f, 1.0f, 0.1f), M.Terracotta);
            Box("Arm_L", chair, new Vector3(-0.42f, 0.8f, 0), new Vector3(0.08f, 0.08f, 0.6f), M.Brass, false);
            Box("Arm_R", chair, new Vector3(0.42f, 0.8f, 0), new Vector3(0.08f, 0.08f, 0.6f), M.Brass, false);
            }
            Box("Notebook", chair, new Vector3(0.12f, 0.63f, 0.05f), new Vector3(0.22f, 0.03f, 0.3f), M.Ceramic, false, Quaternion.Euler(0, 15, 0));
            DockingBayBuilder.BuildRecorder(p, "Recorder_LogCore01", new Vector3(-0.15f, 0.62f, -1.15f), Quaternion.Euler(0, 20, 0), StationContent.LogCore01);

            // captain's console in front of the chair: CAIRN's confrontation, then the decision
            var cap = Group("CaptainConsole", p, new Vector3(0, 0, 0.3f));
            var ct = cap.transform;
            if (PropModels.Place("prop_captain_console", ct, Vector3.zero) == null)
            {
                Box("Body", ct, new Vector3(0, 0.45f, 0), new Vector3(1.6f, 0.9f, 0.5f), _console);
                Box("Top", ct, new Vector3(0, 0.95f, 0), new Vector3(1.7f, 0.06f, 0.6f), M.Brass, false, Quaternion.Euler(12, 0, 0));
            }
            var glow = Box("KeyGlow", ct, new Vector3(0, 0.99f, 0), new Vector3(1.4f, 0.01f, 0.4f), _consoleGlow, false, Quaternion.Euler(12, 0, 0), false);
            Box("LockoutPlate", ct, new Vector3(0.55f, 1.0f, 0.05f), new Vector3(0.3f, 0.02f, 0.2f), M.RedLamp, false, Quaternion.Euler(12, 0, 0));
            var col = cap.AddComponent<BoxCollider>();
            col.size = new Vector3(1.8f, 1.2f, 0.8f);
            col.center = new Vector3(0, 0.6f, 0);
            var console = cap.AddComponent<ConsoleDialogueInteractable>();
            console.EditorConfigure("cmd_captain_console", "prompt.sit", "command.console.name", false,
                new ConditionSet { conditions = { new Condition { type = ConditionType.LogCollected, key = "log_core_02", failReasonKey = "command.console.first_comms" } } });
            console.EditorSetDialogue(DialogueContent.AiCore03, true);
            _ = glow;

            // comms console (west): the beacon loop + the original warning (LOG_CORE_02, critical)
            var comms = Group("CommsConsole", p, new Vector3(-6.3f, 0, 1.2f));
            comms.transform.localRotation = Quaternion.Euler(0, -90, 0); // faces the room (+X)
            var cm = comms.transform;
            GameObject cglow;
            TMPro.TextMeshPro beacon;
            Vector3 blinkPos;
            if (PropModels.Place("prop_console_station", cm, Vector3.zero) != null)
            {
                var tilt = Quaternion.Euler(-8, 0, 0);
                cglow = Box("ScreenGlow", cm, new Vector3(0, 1.3f, 0.175f), new Vector3(1.12f, 0.48f, 0.005f), _consoleGlow, false, tilt, false);
                beacon = WorldText("Beacon", cm, new Vector3(0, 1.3f, 0.165f), tilt, "command.comms", 0.8f, Amber);
                ((RectTransform)beacon.transform).sizeDelta = new Vector2(1.05f, 0.42f);
                blinkPos = new Vector3(0.58f, 1.05f, -0.3f);
            }
            else
            {
                Box("Body", cm, new Vector3(0, 0.45f, 0), new Vector3(1.6f, 0.9f, 0.6f), _console);
                Box("Top", cm, new Vector3(0, 0.95f, -0.05f), new Vector3(1.7f, 0.06f, 0.7f), M.Brass, false, Quaternion.Euler(-12, 0, 0));
                Box("Screen", cm, new Vector3(0, 1.45f, 0.2f), new Vector3(1.4f, 0.7f, 0.04f), M.SlateDark, false);
                cglow = Box("ScreenGlow", cm, new Vector3(0, 1.45f, 0.175f), new Vector3(1.32f, 0.62f, 0.005f), _consoleGlow, false, null, false);
                beacon = WorldText("Beacon", cm, new Vector3(0, 1.45f, 0.16f), Quaternion.identity, "command.comms", 0.9f, Amber);
                ((RectTransform)beacon.transform).sizeDelta = new Vector2(1.3f, 0.5f);
                blinkPos = new Vector3(0.6f, 1.12f, -0.1f);
            }
            beacon.characterSpacing = 1;
            var blink = Sphere("BeaconBlink", cm, blinkPos, Vector3.one * 0.06f, M.RedLamp);
            blink.AddComponent<EmissionPulse>().EditorSet(EmergencyRed, 0f, 3f, 1.4f, 0f);
            var commsCol = comms.AddComponent<BoxCollider>();
            commsCol.size = new Vector3(1.6f, 1.6f, 0.8f);
            commsCol.center = new Vector3(0, 0.8f, 0);
            var term = comms.AddComponent<TerminalInteractable>();
            term.EditorConfigure("cmd_comms", "prompt.terminal.use", null, false);
            term.EditorSetTerminal(StationContent.CommsTerminal, cglow);
            DockingBayBuilder.BuildRecorder(p, "Recorder_LogCore02", new Vector3(-5.95f, 1.0f, 0.3f), Quaternion.Euler(0, 90, 0), StationContent.LogCore02);

            // CAIRN's private archive (east): sealed until LOG_RES_03 was played
            var arch = Group("PrivateArchive", p, new Vector3(CmdHalfW - 0.08f, 0, 1.2f));
            arch.transform.localRotation = Quaternion.Euler(0, 90, 0); // faces the room (-X)
            var at = arch.transform;
            Box("Frame", at, new Vector3(0, 1.4f, 0.02f), new Vector3(1.5f, 1.6f, 0.06f), M.Brass, false);
            var sealedPlate = Box("Sealed", at, new Vector3(0, 1.4f, -0.02f), new Vector3(1.4f, 1.5f, 0.04f), M.SlateDark, true, null, false);
            var openNiche = Box("Niche", at, new Vector3(0, 1.4f, 0.1f), new Vector3(1.3f, 1.4f, 0.04f), _console, false, null, false);
            var archLabel = WorldText("Label", at, new Vector3(0, 2.4f, -0.05f), Quaternion.identity, "command.archive.label", 1.1f, SignalCyan);
            ((RectTransform)archLabel.transform).sizeDelta = new Vector2(2.0f, 0.25f);
            archLabel.characterSpacing = 2;
            var sealedInfo = sealedPlate.AddComponent<Interactable>();
            sealedInfo.EditorConfigure("cmd_archive_sealed", "prompt.inspect", "command.archive.label", false,
                new ConditionSet { conditions = { Condition.Flag(LastSignal.Audio.AudioLogManager.PlayedKey("log_res_03"), "command.archive.sealed") } });
            var rec = DockingBayBuilder.BuildRecorder(p, "Recorder_LogCore03", new Vector3(CmdHalfW - 0.3f, 1.2f, 1.2f), Quaternion.Euler(0, -90, 0), StationContent.LogCore03);
            arch.AddComponent<ConditionToggle>().EditorSet(
                new ConditionSet { conditions = { Condition.Flag(LastSignal.Audio.AudioLogManager.PlayedKey("log_res_03")) } },
                new[] { rec, openNiche }, new[] { sealedPlate });

            // paper map and mugs
            var table = Group("MapTable", p, new Vector3(3.6f, 0, -3.6f)).transform;
            Box("Top", table, new Vector3(0, 0.85f, 0), new Vector3(1.6f, 0.06f, 1.0f), M.SlateDark);
            Box("Leg", table, new Vector3(0, 0.42f, 0), new Vector3(0.2f, 0.84f, 0.2f), M.Brass, false);
            Box("Map", table, new Vector3(0, 0.885f, 0), new Vector3(1.4f, 0.005f, 0.85f), M.Ceramic, false, Quaternion.Euler(0, 4, 0));
            var mapText = WorldText("MapLabel", table, new Vector3(0, 0.9f, 0.25f), Quaternion.Euler(90, 4, 0), "command.map", 0.7f, Navy);
            ((RectTransform)mapText.transform).sizeDelta = new Vector2(1.3f, 0.2f);
            mapText.characterSpacing = 1;
            Cylinder("Mug_A", table, new Vector3(0.55f, 0.94f, -0.3f), new Vector3(0.09f, 0.06f, 0.09f), M.Terracotta, false);
            Cylinder("Mug_B", table, new Vector3(-0.6f, 0.94f, 0.25f), new Vector3(0.09f, 0.06f, 0.09f), M.Ceramic, false);
        }

        private static void BuildDialogueTriggers(Transform root)
        {
            var t = Group("DialogueTriggers", root).transform;
            Group("AI_CORE_01", t).AddComponent<DialogueTrigger>()
                .EditorSet(DialogueContent.AiCore01, DialogueTriggerMode.OnSectorStart, null, null, 2.5f);
            Group("AI_CORE_02", t).AddComponent<DialogueTrigger>()
                .EditorSet(DialogueContent.AiCore02, DialogueTriggerMode.OnFlag, LastSignal.Audio.AudioLogManager.PlayedKey("log_core_02"), null, 1.0f);
        }
    }
}
