using System.Collections.Generic;
using System.IO;
using LastSignal.Core;
using LastSignal.Cutscene;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static LastSignal.EditorTools.BuilderKit;
using M = LastSignal.EditorTools.StationMaterials;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Builds Cutscene_Opening ("The Call", StoryBible §10): Tern cockpit set at the origin, exterior set (Nereth,
    /// the Murmur pulses, Lantern Station, the Tern, docking lights) far away, and the 14-shot list.
    /// </summary>
    public static class CutsceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Cutscenes/Cutscene_Opening.unity";
        internal static readonly Vector3 Ext = new(1000f, 0f, 0f); // exterior set origin
        private static readonly Vector3 Tern = Ext + new Vector3(0f, 22f, 205f);
        internal static readonly Vector3 Station = Ext + new Vector3(0f, 25f, 262f);

        private static Material _ice, _murmur, _hull, _starMat, _display;

        /// <summary>Creates/loads the cutscene materials (shared with the Ending scene).</summary>
        internal static void InitMaterials()
        {
            StationMaterials.Create();
            _ice = Lit("M_Planet_Ice", Hex("#9FB3C2"), 0.35f, 0f);
            _murmur = Emissive("M_Murmur_Pulse", Hex("#3FB8FF"), 2f, Hex("#2A6A90"));
            _hull = Lit("M_Hull_Dark", Color.Lerp(Navy, MetalGrey, 0.3f), 0.45f, 0.6f);
            _display = Emissive("M_Display_Back", Hex("#061A20"), 0.25f, Hex("#03090C"));
            _starMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/M_Stars.mat");
            if (_starMat == null)
            {
                _starMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(_starMat, $"{MaterialsFolder}/M_Stars.mat");
            }
            _starMat.SetColor("_BaseColor", Color.white);
        }

        [MenuItem("Last Signal/Build Content/Opening Cutscene (overwrites scene)")]
        public static void Build()
        {
            InitMaterials();
            var dot = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
            if (dot != null) _starMat.SetTexture("_BaseMap", dot);
            _starMat.SetFloat("_Surface", 1f);
            _starMat.SetFloat("_Blend", 2f); // additive
            _starMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _starMat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(_starMat);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("[Generated] Cutscene_Opening").transform;

            var camGo = new GameObject("ShotCamera", typeof(Camera), typeof(AudioListener));
            camGo.transform.SetParent(root, false);
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.005f, 0.008f, 0.015f);
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 3000f;
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            var display = BuildCockpit(root);
            var dockLights = BuildExterior(root);
            BuildLighting(root);

            var canvas = UiCanvas("CutsceneUI", root, 100);
            var fade = UiImage("Fade", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000, 3000), Color.black);
            fade.raycastTarget = false;
            var subtitle = UiText("Subtitle", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0, 140), new Vector2(1500, 110), 30, new Color(0.93f, 0.96f, 1f));
            var card = UiText("Card", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600, 120), 46, TealLight);
            card.characterSpacing = 12;
            var skip = UiText("SkipHint", canvas.transform, new Vector2(1f, 0f), new Vector2(-200, 50), new Vector2(360, 30), 18, new Color(1, 1, 1, 0.45f));
            var skipBack = UiImage("SkipBack", canvas.transform, new Vector2(1f, 0f), new Vector2(-200, 30), new Vector2(300, 4), new Color(1, 1, 1, 0.1f));
            var skipFill = UiImage("SkipFill", skipBack.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 4), SignalCyan);
            skipFill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            skipFill.type = Image.Type.Filled;
            skipFill.fillMethod = Image.FillMethod.Horizontal;
            skipFill.fillAmount = 0f;

            var director = new GameObject("CutsceneDirector").AddComponent<CutsceneDirector>();
            director.transform.SetParent(root, false);
            director.EditorBind(cam, Shots(dockLights), fade, subtitle, card, skip, skipFill, display);
            director.gameObject.AddComponent<LastSignal.Audio.SectorAmbience>().EditorSet(new List<LastSignal.Audio.AmbienceLoop>
            {
                new() { clip = "amb_ventilation", volume = 0.3f },
                new() { clip = "amb_murmur", volume = 0.35f },
            });

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)!);
            EditorSceneManager.SaveScene(scene, ScenePath);
            ProjectSetup.RegisterBuildScenes();
            Debug.Log($"[CutsceneBuilder] Built {ScenePath}");
        }

        // ------------------------------------------------------------------ sets

        private static TMP_Text BuildCockpit(Transform root)
        {
            var c = Group("Set_TernCockpit", root).transform;
            Box("Floor", c, new Vector3(0, -0.05f, 0.2f), new Vector3(2.6f, 0.1f, 3.4f), M.Floor);
            Box("Ceiling", c, new Vector3(0, 2.3f, 0.2f), new Vector3(2.6f, 0.1f, 3.4f), M.Ceiling);
            Box("Wall_L", c, new Vector3(-1.3f, 1.1f, 0.2f), new Vector3(0.1f, 2.4f, 3.4f), M.Wall);
            Box("Wall_R", c, new Vector3(1.3f, 1.1f, 0.2f), new Vector3(0.1f, 2.4f, 3.4f), M.Wall);
            Box("Wall_Back", c, new Vector3(0, 1.1f, -1.6f), new Vector3(2.6f, 2.4f, 0.1f), M.Wall);
            // canopy frame with brass struts; open window to space
            Box("Canopy_Sill", c, new Vector3(0, 0.95f, 1.75f), new Vector3(2.6f, 0.1f, 0.15f), M.Brass);
            Box("Canopy_Top", c, new Vector3(0, 2.2f, 1.6f), new Vector3(2.6f, 0.12f, 0.3f), M.Brass);
            Box("Canopy_StrutL", c, new Vector3(-0.7f, 1.6f, 1.7f), new Vector3(0.08f, 1.3f, 0.08f), M.Brass, false, Quaternion.Euler(-15, 0, 12));
            Box("Canopy_StrutR", c, new Vector3(0.7f, 1.6f, 1.7f), new Vector3(0.08f, 1.3f, 0.08f), M.Brass, false, Quaternion.Euler(-15, 0, -12));
            Box("Canopy_Below", c, new Vector3(0, 0.45f, 1.8f), new Vector3(2.6f, 0.9f, 0.1f), M.Wall);

            // console with the display
            Box("Console", c, new Vector3(0, 0.8f, 1.15f), new Vector3(1.8f, 0.12f, 0.6f), M.SlateDark, false, Quaternion.Euler(-18, 0, 0));
            Box("ConsoleTrim", c, new Vector3(0, 0.75f, 0.85f), new Vector3(1.85f, 0.04f, 0.05f), M.Brass, false);
            Box("DisplayBack", c, new Vector3(0, 1.12f, 1.32f), new Vector3(0.62f, 0.38f, 0.03f), _display, false, Quaternion.Euler(-12, 0, 0));
            Box("DisplayFrame", c, new Vector3(0, 1.12f, 1.335f), new Vector3(0.68f, 0.44f, 0.02f), M.Brass, false, Quaternion.Euler(-12, 0, 0));
            var text = WorldText("DisplayText", c, new Vector3(0, 1.16f, 1.3f), Quaternion.Euler(-12, 0, 0), "cut.display.signal", 0.32f, SignalCyan);
            ((RectTransform)text.transform).sizeDelta = new Vector2(0.56f, 0.24f);
            text.characterSpacing = 2;
            text.textWrappingMode = TextWrappingModes.Normal;
            // waveform bars that pulse like the Murmur
            for (int i = 0; i < 18; i++)
            {
                var bar = Box($"Wave_{i}", c, new Vector3(-0.25f + i * 0.03f, 1.0f, 1.29f), new Vector3(0.012f, 0.03f + (i % 5) * 0.012f, 0.005f), M.CyanStrip, false, Quaternion.Euler(-12, 0, 0), false);
                bar.AddComponent<EmissionPulse>().EditorSet(SignalCyan, 0.4f, 2.4f, 1.6f, i * 0.07f);
            }
            PointLight("DisplayGlow", c, new Vector3(0, 1.35f, 0.4f), SignalCyan, 0.35f, 1.8f);
            PointLight("FaceRim", c, new Vector3(-0.35f, 1.5f, 0.45f), SignalCyan, 0.6f, 1.4f);
            PointLight("InstrumentGlow", c, new Vector3(0.6f, 0.95f, 0.9f), Amber, 0.5f, 1.6f);

            // seat + Noa silhouette (no character model yet)
            Box("SeatBase", c, new Vector3(0, 0.25f, -0.2f), new Vector3(0.6f, 0.5f, 0.6f), M.SlateDark);
            Box("SeatBack", c, new Vector3(0, 0.95f, -0.5f), new Vector3(0.6f, 0.9f, 0.12f), M.Terracotta, false, Quaternion.Euler(-8, 0, 0));
            var noa = Group("Noa_Silhouette", c, new Vector3(0, 0, -0.15f)).transform;
            Cylinder("Torso", noa, new Vector3(0, 0.98f, 0), new Vector3(0.42f, 0.32f, 0.3f), M.Rubber, false);
            Sphere("Head", noa, new Vector3(0, 1.45f, 0.02f), new Vector3(0.24f, 0.28f, 0.26f), M.Rubber, false);
            Box("Shoulders", noa, new Vector3(0, 1.22f, 0), new Vector3(0.52f, 0.1f, 0.24f), M.Rubber, false);

            // pale planet edge beyond the canopy (shot 4)
            Sphere("PlanetEdge", c, new Vector3(30, -190, 260), Vector3.one * 380f, _ice, false);
            Stars(c, Vector3.zero, 400f, 900);
            return text;
        }

        internal static GameObject BuildExterior(Transform root)
        {
            var e = Group("Set_Exterior", root, Ext).transform;
            // Nereth
            var planetCenter = new Vector3(0, -120, 420);
            Sphere("Nereth", e, planetCenter, Vector3.one * 300f, _ice, false);
            // the Murmur: slow blue pulses on the night side, just under the visible surface
            var pulses = new[] { new Vector3(-40, 10, -125), new Vector3(25, 25, -120), new Vector3(-5, -20, -140), new Vector3(60, -5, -118), new Vector3(-75, 35, -102), new Vector3(10, 55, -100) };
            for (int i = 0; i < pulses.Length; i++)
            {
                var dir = (pulses[i]).normalized;
                var p = Sphere($"Murmur_{i}", e, planetCenter + dir * 149.6f, new Vector3(9f, 9f, 2f), _murmur, false, false);
                p.transform.rotation = Quaternion.LookRotation(dir);
                p.AddComponent<EmissionPulse>().EditorSet(Hex("#3FB8FF"), 0f, 3.5f, 4.0f, i * 0.17f);
            }
            Stars(e, Vector3.zero, 900f, 2500);

            // Lantern Station: hub, ring, spokes, the Array dish pointed at the planet, dark with red lights
            var s = Group("LanternStation", e, Station - Ext).transform;
            Cylinder("Hub", s, Vector3.zero, new Vector3(10f, 7f, 10f), _hull, false);
            Ring("HabitatRing", s, Vector3.zero, 22f, 3.5f, 4f, 48, _hull);
            for (int i = 0; i < 4; i++)
                Box($"Spoke_{i}", s, Quaternion.Euler(0, 0, i * 90f) * new Vector3(13f, 0, 0), new Vector3(16f, 1.2f, 1.2f), M.Brass, false, Quaternion.Euler(0, 0, i * 90f));
            var arm = Group("ArrayArm", s, new Vector3(0, -9f, 4f)).transform;
            Cylinder("Mast", arm, new Vector3(0, -4f, 0), new Vector3(1.2f, 5f, 1.2f), M.Brass, false);
            Cylinder("Dish", arm, new Vector3(0, -10f, 2f), new Vector3(16f, 0.6f, 16f), _hull, false, Quaternion.Euler(35, 0, 0));
            Cylinder("DishRim", arm, new Vector3(0, -10f, 2f), new Vector3(16.6f, 0.3f, 16.6f), M.Brass, false, Quaternion.Euler(35, 0, 0));
            foreach (var p in new[] { new Vector3(22, 0, -2), new Vector3(-22, 0, -2), new Vector3(0, 22, -2), new Vector3(0, -22, -2), new Vector3(5, 3, -5) })
                Sphere($"RedLight_{p.x:0}_{p.y:0}", s, p, Vector3.one * 0.8f, M.RedLamp, false, false).AddComponent<EmissionPulse>().EditorSet(EmergencyRed, 0.2f, 3f, 2.2f, p.x * 0.01f);
            // Bay 2 mouth and its docking lights (switch on one by one in shot 11)
            Box("Bay2_Mouth", s, new Vector3(0, 0, -5.2f), new Vector3(6f, 4f, 0.6f), M.SlateDark, false);
            var lights = Group("Bay2_DockingLights", s, new Vector3(0, 0, -5.6f));
            for (int i = 0; i < 8; i++)
            {
                var side = i % 2 == 0 ? -1f : 1f;
                var lamp = Sphere($"DockLight_{i}", lights.transform, new Vector3(side * 3.2f, -1.8f + (i / 2) * 1.2f, -0.2f), Vector3.one * 0.4f, M.AmberLamp, false, false);
                PointLight("Glow", lamp.transform, Vector3.zero, Amber, 1.2f, 5f);
            }
            lights.AddComponent<SequentialActivator>();
            lights.SetActive(false);

            // the Tern: a small relay tug
            var t = Group("Tern", e, Tern - Ext).transform;
            Box("Body", t, Vector3.zero, new Vector3(2.4f, 1.6f, 5.5f), _hull, false);
            Box("Cockpit", t, new Vector3(0, 0.6f, 2.3f), new Vector3(1.6f, 0.9f, 1.4f), M.Brass, false);
            Box("CanopyGlow", t, new Vector3(0, 0.75f, 3.0f), new Vector3(1.3f, 0.5f, 0.05f), M.CyanStrip, false, null, false);
            Box("EngineBlock", t, new Vector3(0, 0, -3.1f), new Vector3(3.0f, 1.8f, 1.0f), M.SlateDark, false);
            foreach (int x in new[] { -1, 1 })
                Cylinder($"Nozzle_{x}", t, new Vector3(x * 0.8f, 0, -3.8f), new Vector3(0.8f, 0.4f, 0.8f), M.AmberLamp, false, Quaternion.Euler(90, 0, 0), false);
            Box("Stripe", t, new Vector3(0, 0.81f, 0), new Vector3(2.42f, 0.05f, 5.0f), M.Terracotta, false);
            t.localRotation = Quaternion.LookRotation((Station - Tern).normalized);
            return lights;
        }

        private static void Stars(Transform parent, Vector3 localPos, float radius, int count)
        {
            var go = Group("Stars", parent, localPos);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = 100000f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(radius * 0.002f, radius * 0.0055f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.85f, 1f), new Color(1f, 0.92f, 0.8f));
            main.maxParticles = count;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            shape.radiusThickness = 0f;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = _starMat;
        }

        internal static void BuildLighting(Transform root)
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(root, false);
            sun.type = LightType.Directional;
            sun.color = new Color(0.85f, 0.9f, 1f);
            sun.intensity = 0.9f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(8f, -70f, 0f); // grazing light: most of Nereth stays on the night side
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.03f, 0.04f, 0.06f);
            RenderSettings.fog = false;
            RenderSettings.skybox = null;
            var volume = Group("GlobalVolume", root).AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(DockingBayBuilder.VolumePath);
        }

        // ------------------------------------------------------------------ shot list (StoryBible §10)

        private static List<CutsceneShot> Shots(GameObject dockLights)
        {
            var eye = new Vector3(0, 1.15f, 0.55f);
            return new List<CutsceneShot>
            {
                new() { name = "01 Signal", duration = 5f, black = true, cardKey = "cut.open.01", fromPosition = eye, toPosition = eye },
                new() { name = "02 Display", duration = 4f, fadeIn = 1.2f, displayKey = "cut.display.signal",
                    fromPosition = new Vector3(0, 1.15f, 0.45f), fromEuler = new Vector3(8, 0, 0), toPosition = new Vector3(0, 1.15f, 0.52f), toEuler = new Vector3(8, 0, 0), fieldOfView = 45f },
                new() { name = "03 Push-in", duration = 7f, displayKey = "cut.display.loop", subtitleKey = "cut.open.02",
                    fromPosition = new Vector3(0, 1.15f, 0.52f), fromEuler = new Vector3(8, 0, 0), toPosition = new Vector3(0, 1.14f, 0.85f), toEuler = new Vector3(10, 0, 0), fieldOfView = 45f },
                new() { name = "04 Behind seat", duration = 8f, subtitleKey = "cut.open.03",
                    fromPosition = new Vector3(0.55f, 1.65f, -1.35f), fromEuler = new Vector3(8, -10, 0), toPosition = new Vector3(0.35f, 1.62f, -1.2f), toEuler = new Vector3(6, -5, 0), fieldOfView = 60f },
                new() { name = "05 Noa reply", duration = 6f, subtitleKey = "cut.open.04", fadeOut = 1.2f,
                    fromPosition = new Vector3(-0.95f, 1.42f, 0.35f), fromEuler = new Vector3(4, 112, 0), toPosition = new Vector3(-0.88f, 1.43f, 0.28f), toEuler = new Vector3(4, 114, 0), fieldOfView = 40f },
                new() { name = "06 Three days later", duration = 3f, black = true, cardKey = "cut.open.05", fromPosition = eye, toPosition = eye },
                new() { name = "07 Past the Tern", duration = 8f, fadeIn = 1.5f,
                    fromPosition = Tern + new Vector3(-12, 5, -28), fromEuler = new Vector3(10, 23, 0), toPosition = Tern + new Vector3(-7, 3, -8), toEuler = new Vector3(42, -15, 0), fieldOfView = 55f },
                new() { name = "08 Station reveal", duration = 7f,
                    fromPosition = Ext + new Vector3(-40, 8, 170), fromEuler = new Vector3(-8, 22, 0), toPosition = Ext + new Vector3(-36, 6, 180), toEuler = new Vector3(-10, 28, 0), fieldOfView = 50f },
                new() { name = "09a Status", duration = 3f, displayKey = "cut.display.status",
                    fromPosition = new Vector3(0.35f, 1.5f, -0.55f), fromEuler = new Vector3(12, -4, 0), toPosition = new Vector3(0.32f, 1.48f, -0.48f), toEuler = new Vector3(12, -3, 0), fieldOfView = 50f },
                new() { name = "09b Guidance", duration = 3.5f, displayKey = "cut.display.guidance", subtitleKey = "cut.open.06", sfx = "sfx_terminal_ok",
                    fromPosition = new Vector3(0.32f, 1.48f, -0.48f), fromEuler = new Vector3(12, -3, 0), toPosition = new Vector3(0.3f, 1.46f, -0.4f), toEuler = new Vector3(13, -2, 0), fieldOfView = 50f },
                new() { name = "10 I didn't request guidance", duration = 4f, subtitleKey = "cut.open.07",
                    fromPosition = new Vector3(-0.85f, 1.42f, 0.28f), fromEuler = new Vector3(4, 114, 0), toPosition = new Vector3(-0.8f, 1.42f, 0.24f), toEuler = new Vector3(5, 116, 0), fieldOfView = 38f },
                new() { name = "11 Docking", duration = 9f, activate = new[] { dockLights }, sfx = "sfx_docking_clamp",
                    fromPosition = Tern + new Vector3(14, 4, 4), fromEuler = new Vector3(5, -41, 0), toPosition = Tern + new Vector3(16, 5, 30), toEuler = new Vector3(4, -36, 0), fieldOfView = 50f },
                new() { name = "12 Standby", duration = 6f, displayKey = "cut.display.standby", subtitleKey = "cut.open.08",
                    fromPosition = new Vector3(0, 1.62f, -1.25f), fromEuler = new Vector3(6, 0, 0), toPosition = new Vector3(0, 1.6f, -1.05f), toEuler = new Vector3(5, 0, 0), fieldOfView = 60f },
                new() { name = "13 Let's see who's home", duration = 4f, subtitleKey = "cut.open.09", fadeOut = 1.5f,
                    fromPosition = new Vector3(-0.7f, 1.55f, 0.45f), fromEuler = new Vector3(2, 125, 0), toPosition = new Vector3(-0.65f, 1.62f, 0.4f), toEuler = new Vector3(-2, 125, 0), fieldOfView = 42f },
                new() { name = "14 To the airlock", duration = 1.5f, black = true, fromPosition = eye, toPosition = eye },
            };
        }
    }
}
