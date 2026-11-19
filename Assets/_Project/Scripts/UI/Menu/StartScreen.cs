using System.Collections.Generic;
using LastSignal.Core;
using LastSignal.Database;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LastSignal.UI.RuntimeUi;

namespace LastSignal.UI
{
    /// <summary>
    /// The start screen: first-run administrator setup, login, registration, then the main menu
    /// (continue / new game / settings / admin panel / log out / quit). Builds its UI in code.
    /// </summary>
    public sealed class StartScreen : MonoBehaviour
    {
        private enum Page { FirstAdmin, Login, Register, Menu, Settings }

        private readonly Dictionary<Page, GameObject> _pages = new();
        private TMP_Text _feedback;
        private TMP_Text _welcome;
        private TMP_Text _dbBanner;
        private TMP_InputField _loginUser, _loginPass, _regUser, _regPass, _regConfirm, _adminUser, _adminPass, _adminConfirm;
        private Button _continue, _adminPanel;
        private GameObject _newGameConfirm;
        private AdminPanel _admin;
        private Page _current;

        public bool IsAdminPanelOpen => _admin != null && _admin.IsOpen;

        private void Start()
        {
            GameServices.State.Set(GameState.MainMenu);
            GameServices.Audio.SetAmbience(("amb_murmur", 0.35f), ("amb_station_hum", 0.15f));
            Build();
            var auth = GameServices.Auth;
            if (auth.IsLoggedIn && auth.Current.UserId > 0) Show(Page.Menu);
            else Show(auth.NeedsFirstAdmin ? Page.FirstAdmin : Page.Login);
        }

        private void Build()
        {
            var canvas = Canvas("StartScreenUI", transform, 10).transform;
            var title = Text("Title", canvas, new Vector2(0, 330), new Vector2(1400, 120), 92, Brass, "ui.game.title");
            title.characterSpacing = 18;
            Text("Tagline", canvas, new Vector2(0, 250), new Vector2(1400, 40), 24, TealLight, "ui.start.tagline").characterSpacing = 6;
            _dbBanner = Text("DbBanner", canvas, new Vector2(0, -470), new Vector2(1500, 40), 22, Red);
            Text("Version", canvas, new Vector2(150, 30), new Vector2(300, 30), 16, Muted, null, TextAlignmentOptions.Left, Vector2.zero).text = $"v{Application.version}";
            var lang = Button("Language", canvas, new Vector2(-110, -50), new Vector2(170, 48), null, () => GameServices.Localization.ToggleLanguage(), 22);
            ((RectTransform)lang.transform).anchorMin = ((RectTransform)lang.transform).anchorMax = new Vector2(1, 1);
            SetButtonText(lang, "EN  |  БГ");

            var frame = Panel("Frame", canvas, new Vector2(0, -90), new Vector2(640, 520)).transform;
            _feedback = Text("Feedback", frame, new Vector2(0, -230), new Vector2(600, 50), 21, Red);

            // first run: create the administrator account
            var fa = Page_(Page.FirstAdmin, frame);
            Text("Heading", fa, new Vector2(0, 205), new Vector2(600, 40), 28, Cyan, "ui.start.first_admin");
            Text("Hint", fa, new Vector2(0, 160), new Vector2(560, 50), 19, Muted, "ui.start.first_admin_hint");
            _adminUser = Input("Username", fa, new Vector2(0, 95), new Vector2(480, 56), "ui.start.username");
            _adminPass = Input("Password", fa, new Vector2(0, 25), new Vector2(480, 56), "ui.start.password", true);
            _adminConfirm = Input("Confirm", fa, new Vector2(0, -45), new Vector2(480, 56), "ui.start.confirm", true);
            Button("Create", fa, new Vector2(0, -130), new Vector2(480, 62), "ui.start.create_admin", CreateFirstAdmin);

            // login
            var lp = Page_(Page.Login, frame);
            Text("Heading", lp, new Vector2(0, 205), new Vector2(600, 40), 28, Cyan, "ui.start.login");
            _loginUser = Input("Username", lp, new Vector2(0, 120), new Vector2(480, 56), "ui.start.username");
            _loginPass = Input("Password", lp, new Vector2(0, 50), new Vector2(480, 56), "ui.start.password", true);
            Button("Login", lp, new Vector2(0, -35), new Vector2(480, 62), "ui.start.login_button", Login);
            Button("ToRegister", lp, new Vector2(0, -120), new Vector2(480, 52), "ui.start.to_register", () => Show(Page.Register), 22);
            Button("Quit", lp, new Vector2(0, -180), new Vector2(220, 44), "ui.menu.quit", Quit, 20);

            // register
            var rp = Page_(Page.Register, frame);
            Text("Heading", rp, new Vector2(0, 205), new Vector2(600, 40), 28, Cyan, "ui.start.register");
            _regUser = Input("Username", rp, new Vector2(0, 130), new Vector2(480, 56), "ui.start.username");
            _regPass = Input("Password", rp, new Vector2(0, 60), new Vector2(480, 56), "ui.start.password", true);
            _regConfirm = Input("Confirm", rp, new Vector2(0, -10), new Vector2(480, 56), "ui.start.confirm", true);
            Button("Create", rp, new Vector2(0, -95), new Vector2(480, 62), "ui.start.create_account", Register);
            Button("Back", rp, new Vector2(0, -170), new Vector2(220, 46), "ui.dec.back", () => Show(Page.Login), 20);

            // main menu
            var mp = Page_(Page.Menu, frame);
            _welcome = Text("Welcome", mp, new Vector2(0, 205), new Vector2(600, 40), 26, Cyan);
            _continue = Button("Continue", mp, new Vector2(0, 130), new Vector2(480, 60), "ui.start.continue", Continue);
            Button("NewGame", mp, new Vector2(0, 60), new Vector2(480, 60), "ui.start.new_game", NewGame);
            Button("Settings", mp, new Vector2(0, -10), new Vector2(480, 56), "ui.start.settings", () => Show(Page.Settings));
            _adminPanel = Button("Admin", mp, new Vector2(0, -78), new Vector2(480, 56), "ui.start.admin_panel", OpenAdmin);
            Button("Logout", mp, new Vector2(-122, -150), new Vector2(236, 50), "ui.start.logout", Logout, 21);
            Button("Quit", mp, new Vector2(122, -150), new Vector2(236, 50), "ui.menu.quit", Quit, 21);
            _newGameConfirm = Panel("NewGameConfirm", mp, new Vector2(0, 20), new Vector2(560, 260)).gameObject;
            Text("Q", _newGameConfirm.transform, new Vector2(0, 60), new Vector2(520, 90), 23, TextColor, "ui.start.new_game_confirm");
            Button("Yes", _newGameConfirm.transform, new Vector2(-120, -60), new Vector2(220, 56), "ui.dec.confirm", StartNewGame, 22);
            Button("No", _newGameConfirm.transform, new Vector2(120, -60), new Vector2(220, 56), "ui.dec.back", () => _newGameConfirm.SetActive(false), 22);
            _newGameConfirm.SetActive(false);

            // settings
            var sp = Page_(Page.Settings, frame);
            Text("Heading", sp, new Vector2(0, 205), new Vector2(600, 40), 28, Cyan, "ui.start.settings");
            Text("SensLabel", sp, new Vector2(-150, 120), new Vector2(260, 40), 22, TextColor, "ui.pause.sensitivity", TextAlignmentOptions.Left);
            Slider("Sensitivity", sp, new Vector2(120, 120), 240, SettingsService.SensitivityMin, SettingsService.SensitivityMax,
                GameServices.Settings.MouseSensitivity, GameServices.Settings.SetMouseSensitivity);
            Text("VolLabel", sp, new Vector2(-150, 55), new Vector2(260, 40), 22, TextColor, "ui.start.volume", TextAlignmentOptions.Left);
            Slider("Volume", sp, new Vector2(120, 55), 240, 0f, 1f, GameServices.Settings.MasterVolume, GameServices.Settings.SetMasterVolume);
            Toggle("InvertY", sp, new Vector2(0, -10), "ui.pause.invert_y", GameServices.Settings.InvertY, GameServices.Settings.SetInvertY);
            Button("Language", sp, new Vector2(0, -80), new Vector2(480, 56), "ui.menu.language", () => GameServices.Localization.ToggleLanguage());
            Button("Back", sp, new Vector2(0, -160), new Vector2(220, 46), "ui.dec.back", () => Show(Page.Menu), 20);

            _admin = new GameObject("AdminPanel").AddComponent<AdminPanel>();
            _admin.transform.SetParent(transform, false);
            _admin.Closed += () => Show(Page.Menu);
        }

        private Transform Page_(Page page, Transform parent)
        {
            var r = Rect(page.ToString(), parent, Vector2.zero, new Vector2(640, 520));
            _pages[page] = r.gameObject;
            return r;
        }

        private void Show(Page page)
        {
            _current = page;
            foreach (var kv in _pages) kv.Value.SetActive(kv.Key == page);
            _feedback.text = string.Empty;
            _dbBanner.text = GameServices.Database.IsAvailable ? string.Empty : Loc.Get("auth.err.no_database");
            if (page == Page.Menu)
            {
                var s = GameServices.Auth.Current;
                _welcome.text = Loc.Get(s.IsAdmin ? "ui.start.welcome_admin" : "ui.start.welcome", s.Username);
                _continue.interactable = GameServices.Saves.HasSaveForCurrentUser;
                _adminPanel.gameObject.SetActive(s.IsAdmin);
                _newGameConfirm.SetActive(false);
            }
            var first = page switch
            {
                Page.FirstAdmin => _adminUser,
                Page.Login => _loginUser,
                Page.Register => _regUser,
                _ => null,
            };
            if (first != null) first.Select();
        }

        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null || IsAdminPanelOpen) return;
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                if (_current == Page.Login) Login();
                else if (_current == Page.Register) Register();
                else if (_current == Page.FirstAdmin) CreateFirstAdmin();
            }
            if (kb.tabKey.wasPressedThisFrame) FocusNext();
        }

        private void FocusNext()
        {
            var fields = _current switch
            {
                Page.Login => new[] { _loginUser, _loginPass },
                Page.Register => new[] { _regUser, _regPass, _regConfirm },
                Page.FirstAdmin => new[] { _adminUser, _adminPass, _adminConfirm },
                _ => null,
            };
            if (fields == null) return;
            int i = System.Array.FindIndex(fields, f => f.isFocused);
            fields[(i + 1) % fields.Length].Select();
        }

        private void Fail(string key)
        {
            _feedback.text = Loc.Get(key);
            GameServices.Audio.Sfx("sfx_terminal_error", 0.7f);
        }

        private void CreateFirstAdmin()
        {
            var err = GameServices.Auth.Register(_adminUser.text, _adminPass.text, _adminConfirm.text, UserRole.Admin, out _);
            if (err != null) { Fail(err); return; }
            err = GameServices.Auth.Login(_adminUser.text, _adminPass.text);
            if (err != null) { Fail(err); return; }
            Show(Page.Menu);
        }

        private void Login()
        {
            var err = GameServices.Auth.Login(_loginUser.text, _loginPass.text);
            _loginPass.text = string.Empty;
            if (err != null) { Fail(err); return; }
            GameServices.Audio.Sfx("sfx_terminal_ok", 0.7f);
            Show(Page.Menu);
        }

        private void Register()
        {
            var err = GameServices.Auth.Register(_regUser.text, _regPass.text, _regConfirm.text, UserRole.Player, out _);
            if (err != null) { Fail(err); return; }
            err = GameServices.Auth.Login(_regUser.text, _regPass.text);
            _regPass.text = _regConfirm.text = string.Empty;
            if (err != null) { Fail(err); return; }
            Show(Page.Menu);
        }

        private void Continue()
        {
            if (!GameServices.Saves.Continue()) Fail(GameServices.Saves.LastError ?? "save.err.missing");
        }

        private void NewGame()
        {
            if (GameServices.Saves.HasSaveForCurrentUser) _newGameConfirm.SetActive(true);
            else StartNewGame();
        }

        private void StartNewGame()
        {
            GameServices.StartNewRun();
            GameServices.Scenes.Load(SceneIds.CutsceneOpening);
        }

        private void OpenAdmin()
        {
            if (!GameServices.Auth.IsAdmin) return;
            foreach (var p in _pages.Values) p.SetActive(false);
            _admin.Open();
        }

        private void Logout()
        {
            GameServices.Auth.Logout();
            _loginUser.text = _loginPass.text = string.Empty;
            Show(Page.Login);
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
