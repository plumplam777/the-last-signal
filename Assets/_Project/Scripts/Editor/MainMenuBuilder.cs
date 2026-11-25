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
    /// <summary>Builds MainMenu.unity: a still space shot of Nereth (MenuBackdrop) with a slow camera drift + the StartScreen.</summary>
    public static class MainMenuBuilder
    {
        [MenuItem("Last Signal/Build Content/Main Menu (overwrites scene)")]
        public static void Build()
        {
            CutsceneBuilder.InitMaterials();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("[Generated] MainMenu").transform;
            MenuBackdrop.Build(root);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)).transform.SetParent(root, false);
            new GameObject("StartScreen").AddComponent<StartScreen>().transform.SetParent(root, false);

            Directory.CreateDirectory(Path.GetDirectoryName(ProjectSetup.MainMenuPath)!);
            EditorSceneManager.SaveScene(scene, ProjectSetup.MainMenuPath);
            ProjectSetup.RegisterBuildScenes();
            Debug.Log($"[MainMenuBuilder] Built {ProjectSetup.MainMenuPath}");
        }
    }
}
