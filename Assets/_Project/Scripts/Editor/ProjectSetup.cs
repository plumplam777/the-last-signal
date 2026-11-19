using System.IO;
using System.Linq;
using LastSignal.Core;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Re-runnable project setup: creates the base scenes (only if missing), registers them in Build Settings
    /// and applies player settings. Menu: Last Signal/Setup. Batchmode: -executeMethod LastSignal.EditorTools.ProjectSetup.RunAll
    /// </summary>
    public static class ProjectSetup
    {
        private const string ScenesRoot = "Assets/_Project/Scenes";
        private const string TmpEssentialsPackage = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

        public static readonly string BootstrapPath = $"{ScenesRoot}/Bootstrap/{SceneIds.Bootstrap}.unity";
        public static readonly string MainMenuPath = $"{ScenesRoot}/MainMenu/{SceneIds.MainMenu}.unity";
        public static readonly string DockingBayPath = $"{ScenesRoot}/Station/{SceneIds.DockingBay}.unity";

        [MenuItem("Last Signal/Setup/Run Full Project Setup")]
        public static void RunAll()
        {
            ImportTmpEssentialsIfMissing();
            ApplyPlayerSettings();
            CreateBootstrapScene();
            if (!File.Exists(MainMenuPath)) MainMenuBuilder.Build();
            CreateSectorPlaceholder(DockingBayPath, "Docking Bay");
            RegisterBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectSetup] Done.");
        }

        [MenuItem("Last Signal/Setup/Register Build Scenes")]
        public static void RegisterBuildScenes()
        {
            // Bootstrap first; then every scene under _Project/Scenes in a stable order.
            var ordered = new[] { BootstrapPath, MainMenuPath }
                .Concat(AssetDatabase.FindAssets("t:Scene", new[] { ScenesRoot })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(p => p != BootstrapPath && p != MainMenuPath)
                    .OrderBy(p => p))
                .Where(File.Exists)
                .Select(p => new EditorBuildSettingsScene(p, true))
                .ToArray();
            EditorBuildSettings.scenes = ordered;
            Debug.Log($"[ProjectSetup] Build scenes: {string.Join(", ", ordered.Select(s => Path.GetFileNameWithoutExtension(s.path)))}");
        }

        private static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "LastSignal";
            PlayerSettings.productName = "The Last Signal";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "com.lastsignal.thelastsignal");
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.runInBackground = false;
        }

        [MenuItem("Last Signal/Setup/Import TMP Essentials")]
        public static void ImportTmpEssentialsIfMissing()
        {
            if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro")) return;
            if (!File.Exists(Path.GetFullPath(TmpEssentialsPackage)))
            {
                Debug.LogError($"[ProjectSetup] TMP essentials package not found at {TmpEssentialsPackage}");
                return;
            }
            AssetDatabase.ImportPackage(TmpEssentialsPackage, false);
            AssetDatabase.Refresh();
        }

        private static void CreateBootstrapScene()
        {
            if (File.Exists(BootstrapPath)) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("Bootstrap");
            go.AddComponent<BootstrapEntry>();
            var cam = new GameObject("Camera").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            Save(scene, BootstrapPath);
        }

        private static void CreateSectorPlaceholder(string path, string label)
        {
            if (File.Exists(path)) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor_Placeholder";
            floor.transform.localScale = new Vector3(2, 1, 2);
            var cam = Camera.main;
            if (cam != null) cam.transform.SetPositionAndRotation(new Vector3(0, 1.7f, -6), Quaternion.Euler(10, 0, 0));
            var sectorRoot = new GameObject($"SectorRoot ({label}) — greybox comes in Phase 2");
            sectorRoot.transform.position = Vector3.zero;
            Save(scene, path);
        }

        private static void CreateEventSystem()
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.transform.SetAsFirstSibling();
        }

        private static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string key, float size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(1600, size * 1.6f);
            rect.anchoredPosition = pos;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = key;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            go.AddComponent<LocalizedText>();
            var so = new SerializedObject(go.GetComponent<LocalizedText>());
            so.FindProperty("key").stringValue = key;
            so.ApplyModifiedPropertiesWithoutUndo();
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string key, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(420, 70);
            rect.anchoredPosition = pos;
            go.GetComponent<Image>().color = new Color(0.08f, 0.16f, 0.22f, 0.9f);
            var label = CreateText(go.transform, "Label", key, 30, Vector2.zero);
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            label.color = new Color(0.8f, 0.95f, 1f);
            return go.GetComponent<Button>();
        }

        private static void Save(Scene scene, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[ProjectSetup] Created {path}");
        }
    }
}
