using LastSignal.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.UI
{
    /// <summary>
    /// Esc pause menu: resume, language, back to main menu. Opens from the Interaction.Pause action while playing
    /// and closes with UI.Cancel. (Settings and save/load buttons arrive with those systems.)
    /// </summary>
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button languageButton;
        [SerializeField] private Button mainMenuButton;

        private bool _openedThisFrame;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void OnEnable()
        {
            resumeButton.onClick.AddListener(Close);
            languageButton.onClick.AddListener(ToggleLanguage);
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }

        private void OnDisable()
        {
            resumeButton.onClick.RemoveListener(Close);
            languageButton.onClick.RemoveListener(ToggleLanguage);
            mainMenuButton.onClick.RemoveListener(GoToMainMenu);
        }

        private void Update()
        {
            _openedThisFrame = false;
            var controls = GameServices.Input.Controls;
            if (!IsOpen && GameServices.State.IsGameplayActive && controls.Interaction.Pause.WasPressedThisFrame())
            {
                Open();
                return;
            }
            if (IsOpen && !_openedThisFrame && controls.UI.Cancel.WasPressedThisFrame()) Close();
        }

        public void Open()
        {
            panel.SetActive(true);
            _openedThisFrame = true;
            GameServices.State.Set(GameState.Paused);
            resumeButton.Select();
        }

        public void Close()
        {
            panel.SetActive(false);
            if (GameServices.State.Current == GameState.Paused) GameServices.State.Set(GameState.Playing);
        }

        private static void ToggleLanguage() => GameServices.Localization.ToggleLanguage();

        private void GoToMainMenu()
        {
            panel.SetActive(false);
            GameServices.State.Set(GameState.MainMenu);
            GameServices.Scenes.Load(SceneIds.MainMenu);
        }

#if UNITY_EDITOR
        public void EditorBind(GameObject p, Button resume, Button language, Button menu)
        {
            panel = p; resumeButton = resume; languageButton = language; mainMenuButton = menu;
        }
#endif
    }
}
