using LastSignal.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.UI
{
    /// <summary>
    /// PHASE 1 PLACEHOLDER main menu: start, language switch, quit.
    /// Replaced in Phase 5 by the polished start screen with login / register / admin.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button languageButton;
        [SerializeField] private Button quitButton;

        private void OnEnable()
        {
            GameServices.State.Set(GameState.MainMenu);
            startButton.onClick.AddListener(OnStart);
            languageButton.onClick.AddListener(GameServices.Localization.ToggleLanguage);
            quitButton.onClick.AddListener(OnQuit);
        }

        private void OnDisable()
        {
            startButton.onClick.RemoveListener(OnStart);
            quitButton.onClick.RemoveListener(OnQuit);
            if (!GameServices.IsReady) return;
            languageButton.onClick.RemoveListener(GameServices.Localization.ToggleLanguage);
        }

        private void OnStart()
        {
            GameServices.StartNewRun();
            // new game: opening cutscene "The Call", which then loads the Docking Bay
            GameServices.Scenes.Load(SceneIds.CutsceneOpening);
        }

        private static void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
