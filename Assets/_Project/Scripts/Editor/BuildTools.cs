using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LastSignal.EditorTools
{
    /// <summary>Builds the game. Batchmode: -executeMethod LastSignal.EditorTools.BuildTools.BuildMac</summary>
    public static class BuildTools
    {
        [MenuItem("Last Signal/Build/macOS")]
        public static void BuildMac() => Build(BuildTarget.StandaloneOSX, "Builds/macOS/TheLastSignal.app");

        [MenuItem("Last Signal/Build/Windows")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/TheLastSignal.exe");

        private static void Build(BuildTarget target, string output)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[BuildTools] No scenes in Build Settings. Run Last Signal/Setup first.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = target,
                options = BuildOptions.None,
            });
            var s = report.summary;
            Debug.Log($"[BuildTools] {target}: {s.result}, {s.totalErrors} errors, {s.totalSize / (1024 * 1024)} MB, {s.totalTime}");
            if (Application.isBatchMode && s.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
