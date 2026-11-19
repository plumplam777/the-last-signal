using LastSignal.UI;
using UnityEditor;
using UnityEngine;

namespace LastSignal.EditorTools
{
    /// <summary>Play-mode helpers for checking runtime-built UI by eye (no database writes).</summary>
    public static class UiPreview
    {
        [MenuItem("Last Signal/Diagnostics/Preview Admin Panel (Play Mode)")]
        public static void PreviewAdminPanel()
        {
            var screen = Object.FindFirstObjectByType<StartScreen>();
            if (screen == null) { Debug.LogWarning("[UiPreview] Enter play mode in the MainMenu scene first."); return; }
            screen.PreviewAdminPanel();
            AttachCanvasesToCamera();
        }

        /// <summary>
        /// Overlay canvases are skipped by camera renders, and the unfocused editor renders no frames for
        /// ScreenCapture, so for screenshots move every canvas onto the main camera (play-mode only, not saved).
        /// </summary>
        [MenuItem("Last Signal/Diagnostics/Attach UI To Camera (Play Mode)")]
        public static void AttachCanvasesToCamera()
        {
            var cam = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
            if (cam == null) return;
            Canvas.ForceUpdateCanvases();
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!c.isRootCanvas || c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = cam.nearClipPlane + 0.05f + 0.01f * (100 - Mathf.Clamp(c.sortingOrder, 0, 99));
            }
        }

        [MenuItem("Last Signal/Diagnostics/Attach UI To Camera (Play Mode)", true)]
        [MenuItem("Last Signal/Diagnostics/Preview Admin Panel (Play Mode)", true)]
        private static bool Validate() => Application.isPlaying;
    }
}
