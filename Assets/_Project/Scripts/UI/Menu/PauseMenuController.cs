using LastSignal.Core;
using TMPro;
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
        [Header("Settings")]
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private TMP_Text sensitivityValue;
        [SerializeField] private Toggle invertYToggle;

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
            if (sensitivitySlider != null)
            {
                sensitivitySlider.minValue = SettingsService.SensitivityMin;
                sensitivitySlider.maxValue = SettingsService.SensitivityMax;
                sensitivitySlider.SetValueWithoutNotify(GameServices.Settings.MouseSensitivity);
                sensitivitySlider.onValueChanged.AddListener(OnSensitivity);
                UpdateSensitivityLabel();
            }
            if (invertYToggle != null)
            {
                invertYToggle.SetIsOnWithoutNotify(GameServices.Settings.InvertY);
                invertYToggle.onValueChanged.AddListener(GameServices.Settings.SetInvertY);
            }
        }

        private void OnDisable()
        {
            resumeButton.onClick.RemoveListener(Close);
            languageButton.onClick.RemoveListener(ToggleLanguage);
            mainMenuButton.onClick.RemoveListener(GoToMainMenu);
            if (sensitivitySlider != null) sensitivitySlider.onValueChanged.RemoveListener(OnSensitivity);
            if (invertYToggle != null && GameServices.IsReady) invertYToggle.onValueChanged.RemoveListener(GameServices.Settings.SetInvertY);
        }

        private void OnSensitivity(float value)
        {
            GameServices.Settings.SetMouseSensitivity(value);
            UpdateSensitivityLabel();
        }

        private void UpdateSensitivityLabel()
        {
            // Shown as a percentage of the default so "100%" means the recommended speed.
            if (sensitivityValue != null)
                sensitivityValue.text = $"{Mathf.RoundToInt(GameServices.Settings.MouseSensitivity / SettingsService.SensitivityDefault * 100f)}%";
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
        public void EditorBind(GameObject p, Button resume, Button language, Button menu, Slider sensitivity = null, TMP_Text sensitivityLabel = null, Toggle invert = null)
        {
            panel = p; resumeButton = resume; languageButton = language; mainMenuButton = menu;
            sensitivitySlider = sensitivity; sensitivityValue = sensitivityLabel; invertYToggle = invert;
        }
#endif
    }
}
