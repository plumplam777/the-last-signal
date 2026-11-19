using System.Collections.Generic;
using System.IO;
using System.Linq;
using LastSignal.Core;
using LastSignal.Endings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static LastSignal.EditorTools.BuilderKit;

namespace LastSignal.EditorTools
{
    /// <summary>Builds the Ending scene: the cutscene's exterior set, an orbiting camera and the ending/epilogue UI.</summary>
    public static class EndingBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Endings/Ending.unity";

        [MenuItem("Last Signal/Build Content/Ending Scene (overwrites scene)")]
        public static void Build()
        {
            CutsceneBuilder.InitMaterials();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("[Generated] Ending").transform;

            CutsceneBuilder.BuildExterior(root);
            CutsceneBuilder.BuildLighting(root);
            var exterior = root.Find("Set_Exterior");
            var station = exterior.Find("LanternStation").gameObject;
            var dish = station.transform.Find("ArrayArm");
            var pulses = exterior.GetComponentsInChildren<Renderer>(true).Where(r => r.name.StartsWith("Murmur_")).ToList();

            // Ending A: silent white-gold bloom where the station was
            var boom = Sphere("Explosion", exterior, station.transform.localPosition, Vector3.one * 2f,
                Emissive("M_Explosion", Hex("#FFE2B0"), 6f, Hex("#FFE2B0")), false, false);
            // Ending B: the whole station lights up
            var lightsOn = Group("StationLightsOn", station.transform);
            for (int i = 0; i < 24; i++)
            {
                float a = i * 15f * Mathf.Deg2Rad;
                Sphere($"Window_{i}", lightsOn.transform, new Vector3(Mathf.Cos(a) * 22f, Mathf.Sin(a) * 22f, -2.2f), Vector3.one * 1.1f, StationMaterials.AmberLamp, false, false);
            }
            PointLight("HubLight", lightsOn.transform, new Vector3(0, 0, -8f), Amber, 6f, 40f);

            var camGo = new GameObject("EndingCamera", typeof(Camera), typeof(AudioListener));
            camGo.transform.SetParent(root, false);
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.005f, 0.008f, 0.015f);
            cam.farClipPlane = 3000f;
            cam.fieldOfView = 45f;
            camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;

            var canvas = UiCanvas("EndingUI", root, 100);
            var fade = UiImage("Fade", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000, 3000), Color.black);
            fade.raycastTarget = false;
            var speaker = UiText("Speaker", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0, 230), new Vector2(1400, 36), 22, SignalCyan);
            speaker.characterSpacing = 8;
            var line = UiText("Line", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0, 160), new Vector2(1400, 100), 30, new Color(0.93f, 0.97f, 1f));
            var caption = UiText("Caption", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -200), new Vector2(1500, 80), 34, Brass);
            caption.characterSpacing = 6;
            var epilogue = UiText("Epilogue", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 500), 28, new Color(0.88f, 0.93f, 0.97f));
            epilogue.lineSpacing = 18;
            var credits = UiText("Credits", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400, 400), 64, Brass);
            credits.characterSpacing = 14;

            var controller = new GameObject("EndingController").AddComponent<EndingController>();
            controller.transform.SetParent(root, false);
            controller.gameObject.AddComponent<LastSignal.Audio.SectorAmbience>().EditorSet(new List<LastSignal.Audio.AmbienceLoop>
            {
                new() { clip = "amb_murmur", volume = 0.4f },
                new() { clip = "amb_station_hum", volume = 0.15f },
            });
            controller.EditorBind(cam, station.transform, CutsceneBuilder.Station + new Vector3(0, -20f, 30f), station, boom.transform, lightsOn, dish,
                pulses, fade, speaker, line, caption, epilogue, credits);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)!);
            EditorSceneManager.SaveScene(scene, ScenePath);
            ProjectSetup.RegisterBuildScenes();
            Debug.Log($"[EndingBuilder] Built {ScenePath}");
        }
    }
}
