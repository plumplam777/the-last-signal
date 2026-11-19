using UnityEditor;

namespace LastSignal.EditorTools
{
    /// <summary>Rebuilds every generated sector in order and registers the scenes in Build Settings.</summary>
    public static class ContentPipeline
    {
        [MenuItem("Last Signal/Build Content/Build All Sectors", priority = 0)]
        public static void BuildAll()
        {
            DockingBayBuilder.Build();
            EngineeringBuilder.Build();
            CutsceneBuilder.Build();
            EndingBuilder.Build();
            MainMenuBuilder.Build();
            ProjectSetup.RegisterBuildScenes();
            EditorSceneManagerOpen(ProjectSetup.DockingBayPath);
        }

        private static void EditorSceneManagerOpen(string path)
            => UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
    }
}
