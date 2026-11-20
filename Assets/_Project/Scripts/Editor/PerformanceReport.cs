using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Deterministic per-scene render cost (independent of editor focus/throttling), used for the performance pass:
    /// realtime lights, shadow-casting lights and the shadow maps they need (point = 6 cube faces, spot = 1),
    /// renderers, static-flagged renderers and triangles. Written to Documentation/PerformanceReport.txt.
    /// </summary>
    public static class PerformanceReport
    {
        private static readonly string[] Scenes =
        {
            "Assets/_Project/Scenes/Station/Sector_DockingBay.unity",
            "Assets/_Project/Scenes/Station/Sector_Engineering.unity",
            "Assets/_Project/Scenes/Station/Sector_Medical.unity",
            "Assets/_Project/Scenes/Station/Sector_Research.unity",
            "Assets/_Project/Scenes/Station/Sector_ReactorCommand.unity",
            "Assets/_Project/Scenes/Cutscenes/Cutscene_Opening.unity",
        };

        [MenuItem("Last Signal/Diagnostics/Report Render Cost")]
        public static void Report()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var previous = EditorSceneManager.GetActiveScene().path;
            var sb = new StringBuilder();
            sb.AppendLine($"Render cost report  {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine("scene | lights (all/active) | shadowed lights (all/active) | shadow maps (active) | renderers | static | triangles");
            foreach (var path in Scenes)
            {
                if (!File.Exists(path)) continue;
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(l => l.lightmapBakeType != LightmapBakeType.Baked).ToList();
                var active = lights.Where(l => l.isActiveAndEnabled).ToList();
                int Maps(Light l) => l.shadows == LightShadows.None ? 0 : l.type == LightType.Point ? 6 : 1;
                var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                long tris = 0;
                foreach (var r in renderers)
                {
                    var mf = r.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null) tris += mf.sharedMesh.triangles.LongLength / 3;
                }
                int statics = renderers.Count(r => GameObjectUtility.GetStaticEditorFlags(r.gameObject).HasFlag(StaticEditorFlags.BatchingStatic));
                sb.AppendLine($"{Path.GetFileNameWithoutExtension(path)} | {lights.Count}/{active.Count} | " +
                              $"{lights.Count(l => l.shadows != LightShadows.None)}/{active.Count(l => l.shadows != LightShadows.None)} | " +
                              $"{active.Sum(Maps)} | {renderers.Length} | {statics} | {tris}");
            }
            Directory.CreateDirectory("Documentation");
            File.WriteAllText("Documentation/PerformanceReport.txt", sb.ToString());
            Debug.Log("[PerformanceReport]\n" + sb);
            if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        }
    }
}
