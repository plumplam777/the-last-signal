using LastSignal.Interaction;
using LastSignal.Player;
using LastSignal.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static LastSignal.EditorTools.BuilderKit;

namespace LastSignal.EditorTools
{
    /// <summary>Builds Prefabs/Player/Player.prefab: controller, camera, flashlight, HUD, pause menu, event system.</summary>
    public static class PlayerPrefabBuilder
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";

        [MenuItem("Last Signal/Build Content/Player Prefab")]
        public static GameObject Build()
        {
            var root = new GameObject("Player");
            root.layer = LayerMask.NameToLayer("Ignore Raycast");
            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 50f;

            var pivot = Group("CameraPivot", root.transform, new Vector3(0f, 1.62f, 0f));
            var camGo = new GameObject("PlayerCamera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(pivot.transform, false);
            var cam = camGo.GetComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            cam.fieldOfView = 70f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Navy * 0.5f;
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            var flash = SpotLight("Flashlight", camGo.transform, new Vector3(0.18f, -0.12f, 0.1f), Quaternion.identity,
                new Color(1f, 0.93f, 0.82f), 6f, 20f, 58f, LightShadows.Soft);
            flash.enabled = false;

            var controller = root.AddComponent<PlayerController>();
            var interactor = root.AddComponent<PlayerInteractor>();
            var flashlight = root.AddComponent<FlashlightController>();
            SetRef(controller, "cameraPivot", pivot.transform);
            SetRef(interactor, "viewCamera", cam);
            SetRef(flashlight, "spotLight", flash);

            BuildHud(root.transform, interactor);
            BuildPause(root.transform);

            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.transform.SetParent(root.transform, false);

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PrefabPath)!);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[PlayerPrefabBuilder] Saved {PrefabPath}");
            return prefab;
        }

        private static void BuildHud(Transform parent, PlayerInteractor interactor)
        {
            var canvas = UiCanvas("HUD", parent, 10);
            var rootGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            rootGroup.blocksRaycasts = false;
            var center = new Vector2(0.5f, 0.5f);

            var crosshair = UiImage("Crosshair", canvas.transform, center, Vector2.zero, new Vector2(6, 6), new Color(1, 1, 1, 0.45f));

            var promptRect = UiRect("Prompt", canvas.transform, center, new Vector2(0, -90), new Vector2(900, 90));
            var promptGroup = promptRect.gameObject.AddComponent<CanvasGroup>();
            promptGroup.alpha = 0f;
            var plate = UiImage("Plate", promptRect, center, new Vector2(0, 8), new Vector2(520, 46), new Color(0.05f, 0.07f, 0.11f, 0.72f));
            var plateOutline = plate.gameObject.AddComponent<Outline>();
            plateOutline.effectColor = new Color(Brass.r, Brass.g, Brass.b, 0.55f);
            var promptText = UiText("PromptText", promptRect, center, new Vector2(0, 8), new Vector2(900, 46), 26, new Color(0.9f, 0.96f, 1f));
            var reasonText = UiText("ReasonText", promptRect, center, new Vector2(0, -32), new Vector2(900, 36), 21, new Color(1f, 0.62f, 0.55f));
            reasonText.fontStyle = FontStyles.Italic;

            var toastRect = UiRect("Toast", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(900, 60));
            var toastGroup = toastRect.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            var toastText = UiText("ToastText", toastRect, center, Vector2.zero, new Vector2(900, 60), 28, TealLight);

            var hud = canvas.gameObject.AddComponent<HudController>();
            hud.EditorBind(interactor, rootGroup, crosshair, promptGroup, promptText, reasonText, toastText, toastGroup);
        }

        private static void BuildPause(Transform parent)
        {
            var canvas = UiCanvas("PauseMenu", parent, 50);
            var panel = UiImage("Panel", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000, 3000), new Color(0.03f, 0.05f, 0.08f, 0.82f));
            var title = UiText("Title", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(800, 90), 64, Brass, "ui.pause.title");
            title.characterSpacing = 14;
            var resume = UiButton("ResumeButton", panel.transform, new Vector2(0, 40), "ui.pause.resume");
            var language = UiButton("LanguageButton", panel.transform, new Vector2(0, -40), "ui.menu.language");
            var menu = UiButton("MainMenuButton", panel.transform, new Vector2(0, -120), "ui.pause.main_menu");
            var pause = canvas.gameObject.AddComponent<PauseMenuController>();
            pause.EditorBind(panel.gameObject, resume, language, menu);
        }

        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
