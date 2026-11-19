using System.IO;
using LastSignal.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;

namespace LastSignal.EditorTools
{
    /// <summary>Builds MainMenu.unity: Lantern Station over Nereth as a slowly orbiting backdrop + the StartScreen.</summary>
    public static class MainMenuBuilder
    {
        [MenuItem("Last Signal/Build Content/Main Menu (overwrites scene)")]
        public static void Build()
        {
            CutsceneBuilder.InitMaterials();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("[Generated] MainMenu").transform;
            CutsceneBuilder.BuildExterior(root);
            CutsceneBuilder.BuildLighting(root);
            var station = root.Find("Set_Exterior/LanternStation");
            root.Find("Set_Exterior/Tern").gameObject.SetActive(false);

            var camGo = new GameObject("MenuCamera", typeof(Camera), typeof(AudioListener));
            camGo.transform.SetParent(root, false);
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.005f, 0.008f, 0.015f);
            cam.farClipPlane = 3000f;
            cam.fieldOfView = 40f;
            camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            camGo.AddComponent<SlowOrbitCamera>().EditorSet(station, CutsceneBuilder.Station + new Vector3(0, -10f, 20f));

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)).transform.SetParent(root, false);
            new GameObject("StartScreen").AddComponent<StartScreen>().transform.SetParent(root, false);

            Directory.CreateDirectory(Path.GetDirectoryName(ProjectSetup.MainMenuPath)!);
            EditorSceneManager.SaveScene(scene, ProjectSetup.MainMenuPath);
            ProjectSetup.RegisterBuildScenes();
            Debug.Log($"[MainMenuBuilder] Built {ProjectSetup.MainMenuPath}");
        }
    }
}
