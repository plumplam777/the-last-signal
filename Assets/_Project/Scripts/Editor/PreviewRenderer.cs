using System.IO;
using LastSignal.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Renders still previews of a scene from fixed viewpoints into Documentation/Screenshots
    /// (used for art-direction reviews; works in batchmode).
    /// </summary>
    public static class PreviewRenderer
    {
        private const string OutputFolder = "Documentation/Screenshots";

        [MenuItem("Last Signal/Render Previews/Docking Bay")]
        public static void RenderDockingBay()
        {
            EditorSceneManager.OpenScene(ProjectSetup.DockingBayPath, OpenSceneMode.Single);
            var power = Object.FindAnyObjectByType<PoweredObject>();

            SetPower(power, false);
            Shot("phase2_dock_01_unpowered_flashlight", new Vector3(0.4f, 1.62f, -7.2f), new Vector3(8, 18, 0), flashlight: true);
            Shot("phase2_dock_02_unpowered_crates", new Vector3(1.2f, 1.62f, -2.6f), new Vector3(14, 42, 0), flashlight: true);
            SetPower(power, true);
            Shot("phase2_dock_03_powered_hall", new Vector3(0.4f, 1.62f, -7.2f), new Vector3(6, 10, 0));
            Shot("phase2_dock_04_powered_airlock", new Vector3(-0.6f, 1.62f, 3.2f), new Vector3(2, 186, 0));
            Shot("phase2_dock_05_powered_fusebox_door", new Vector3(1.4f, 1.62f, 0.8f), new Vector3(6, 42, 0));
            SetPower(power, false);
            Debug.Log("[PreviewRenderer] Docking Bay previews written to " + OutputFolder);
        }

        private static void SetPower(PoweredObject power, bool on)
        {
            if (power == null) return;
            var so = new SerializedObject(power);
            Toggle(so.FindProperty("poweredOn"), on);
            Toggle(so.FindProperty("poweredOff"), !on);
        }

        private static void Toggle(SerializedProperty array, bool active)
        {
            for (int i = 0; i < array.arraySize; i++)
                if (array.GetArrayElementAtIndex(i).objectReferenceValue is GameObject go) go.SetActive(active);
        }

        private static void Shot(string fileName, Vector3 pos, Vector3 euler, bool flashlight = false, int width = 1600, int height = 900)
        {
            var go = new GameObject("PreviewCamera");
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(euler));
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 70f;
            cam.nearClipPlane = 0.05f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = BuilderKit.Navy * 0.5f;
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            if (flashlight)
            {
                var l = BuilderKit.SpotLight("PreviewFlashlight", go.transform, new Vector3(0.18f, -0.12f, 0.1f), Quaternion.identity,
                    new Color(1f, 0.93f, 0.82f), 6f, 20f, 58f);
                l.enabled = true;
            }

            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 1 };
            cam.targetTexture = rt;
            // Camera.Render runs the full URP camera stack including post-processing (the single-camera
            // render request path skips the final post pass).
            cam.Render();

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            Directory.CreateDirectory(OutputFolder);
            File.WriteAllBytes(Path.Combine(OutputFolder, fileName + ".png"), tex.EncodeToPNG());
            cam.targetTexture = null;
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
        }
    }
}
