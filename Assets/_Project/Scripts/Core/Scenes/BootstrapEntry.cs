using System;
using UnityEngine;

namespace LastSignal.Core
{
    /// <summary>
    /// Lives in the Bootstrap scene (build index 0): services are already up, go to the main menu.
    /// Development builds/Editor accept "-startScene &lt;SceneName&gt;" to jump straight into a scene for testing.
    /// </summary>
    public sealed class BootstrapEntry : MonoBehaviour
    {
        [SerializeField] private string firstScene = SceneIds.MainMenu;

        private void Start()
        {
            GameServices.State.Set(GameState.Boot);
            var target = firstScene;
            var overrideScene = ReadArg("-startScene");
            if (!string.IsNullOrEmpty(overrideScene))
            {
                Log.Info(nameof(BootstrapEntry), $"Command line start scene: {overrideScene}");
                GameServices.StartNewRun();
                target = overrideScene;
            }
            GameServices.Scenes.Load(target);

            // automated smoke tests: quit cleanly instead of being killed (avoids macOS crash/"Reopen" dialogs)
            if (float.TryParse(ReadArg("-quitAfterSeconds"), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var seconds) && seconds > 0f)
            {
                DontDestroyOnLoad(gameObject);
                Invoke(nameof(QuitNow), seconds);
            }
        }

        private void QuitNow()
        {
            Log.Info(nameof(BootstrapEntry), "Quitting (-quitAfterSeconds).");
            Application.Quit(0);
        }

        private static string ReadArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }
    }
}
