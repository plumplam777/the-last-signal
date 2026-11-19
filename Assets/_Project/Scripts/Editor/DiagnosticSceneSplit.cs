using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Temporary diagnostic: splits a scene into one scene per object group, builds a player with them, so a
    /// player-only deserialization error can be pinned to a specific object. Output: Builds/Diag + mapping file.
    /// </summary>
    public static class DiagnosticSceneSplit
    {
        private const string Folder = "Assets/_Diag";

        [MenuItem("Last Signal/Diagnostics/Split Engineering And Build")]
        public static void Run()
        {
            Directory.CreateDirectory(Folder);
            var source = EditorSceneManager.OpenScene(EngineeringBuilder.ScenePath, OpenSceneMode.Single);
            var root = source.GetRootGameObjects().First(g => g.name.StartsWith("[Generated]"));
            var groups = new List<string>();
            foreach (Transform child in root.transform)
            {
                if (child.name == "Props" || child.name == "Environment")
                    foreach (Transform sub in child) groups.Add($"{child.name}/{sub.name}");
                else groups.Add(child.name);
            }

            var scenes = new List<string> { ProjectSetup.BootstrapPath };
            var mapping = new List<string>();
            for (int i = 0; i < groups.Count; i++)
            {
                var scene = EditorSceneManager.OpenScene(EngineeringBuilder.ScenePath, OpenSceneMode.Single);
                var r = scene.GetRootGameObjects().First(g => g.name.StartsWith("[Generated]"));
                var keep = r.transform.Find(groups[i]);
                keep.SetParent(null, true);
                Object.DestroyImmediate(r);
                foreach (var go in scene.GetRootGameObjects().Where(g => g != keep.gameObject)) Object.DestroyImmediate(go);
                var path = $"{Folder}/diag_{i:00}.unity";
                EditorSceneManager.SaveScene(scene, path);
                scenes.Add(path);
                mapping.Add($"diag_{i:00}\t{groups[i]}");
            }
            File.WriteAllLines("Builds/diag_mapping.txt", mapping);
            Directory.CreateDirectory("Builds/Diag");
            var report = BuildPipeline.BuildPlayer(scenes.ToArray(), "Builds/Diag/Diag.app", BuildTarget.StandaloneOSX, BuildOptions.None);
            Debug.Log($"[Diag] {report.summary.result} with {groups.Count} scenes");
            EditorSceneManager.OpenScene(EngineeringBuilder.ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Last Signal/Diagnostics/Delete Diagnostic Scenes")]
        public static void Clean()
        {
            AssetDatabase.DeleteAsset(Folder);
            if (Directory.Exists("Builds/Diag")) Directory.Delete("Builds/Diag", true);
        }
    }
}
