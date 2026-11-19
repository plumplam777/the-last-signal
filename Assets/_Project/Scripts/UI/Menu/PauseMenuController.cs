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

        private GameObject _adminTools;
        private Button _adminButton;
        private TMPro.TMP_Text _status;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            BuildRuntimeButtons();
        }

        /// <summary>Save + admin tools are added at runtime (they depend on the session, not on the prefab).</summary>
        private void BuildRuntimeButtons()
        {
            if (panel == null) return;
            var t = panel.transform;
            RuntimeUi.Button("SaveButton", t, new Vector2(430, 40), new Vector2(340, 60), "ui.pause.save", SaveNow, 24);
            _adminButton = RuntimeUi.Button("AdminToolsButton", t, new Vector2(430, -40), new Vector2(340, 60), "ui.pause.admin_tools", ToggleAdminTools, 24);
            _status = RuntimeUi.Text("Status", t, new Vector2(430, -110), new Vector2(400, 40), 21, RuntimeUi.Cyan);

            var tools = RuntimeUi.Panel("AdminTools", t, new Vector2(-470, -40), new Vector2(420, 470));
            _adminTools = tools.gameObject;
            var tt = tools.transform;
            RuntimeUi.Text("Title", tt, new Vector2(0, 200), new Vector2(380, 40), 24, RuntimeUi.Brass, "ui.pause.admin_tools");
            RuntimeUi.Toggle("UnlockAll", tt, new Vector2(10, 145), "ui.admin.tool_unlock_all", GameServices.Admin.UnlockAllDoors,
                on => { GameServices.Admin.SetUnlockAllDoors(on); });
            RuntimeUi.Button("Power", tt, new Vector2(0, 85), new Vector2(360, 50), "ui.admin.tool_power", () =>
            {
                var sector = FindAnyObjectByType<Player.SectorRoot>();
                if (sector != null) GameServices.Admin.RestorePower(sector.SectorId);
            }, 21);
            RuntimeUi.Button("Items", tt, new Vector2(0, 25), new Vector2(360, 50), "ui.admin.tool_items", () => GameServices.Admin.GiveAllItems(), 21);
            RuntimeUi.Button("ToDocking", tt, new Vector2(0, -35), new Vector2(360, 50), "ui.admin.tool_tp_docking", () => Teleport(SectorIds.Docking), 21);
            RuntimeUi.Button("ToEngineering", tt, new Vector2(0, -95), new Vector2(360, 50), "ui.admin.tool_tp_engineering", () => Teleport(SectorIds.Engineering), 21);
            RuntimeUi.Button("Decision", tt, new Vector2(0, -155), new Vector2(360, 50), "ui.admin.tool_decision", () =>
            {
                Close();
                DecisionScreen.RequestOpen();
            }, 21);
            _adminTools.SetActive(false);
        }

        private void SaveNow()
        {
            var auth = GameServices.Auth;
            if (!auth.IsLoggedIn || auth.Current.UserId <= 0)
            {
                _status.text = Loc.Get("save.err.no_session");
                return;
            }
            GameServices.Checkpoint();
            _status.text = Loc.Get("toast.saved");
            GameServices.Audio.Sfx("sfx_terminal_ok", 0.7f);
        }

        private void ToggleAdminTools()
        {
            if (!GameServices.Auth.IsAdmin) return;
            _adminTools.SetActive(!_adminTools.activeSelf);
        }

        private void Teleport(string sectorId)
        {
            Close();
            GameServices.Admin.Teleport(sectorId);
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
            if (_adminButton != null) _adminButton.gameObject.SetActive(GameServices.Auth.IsAdmin);
            if (_adminTools != null) _adminTools.SetActive(false);
            if (_status != null) _status.text = string.Empty;
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
