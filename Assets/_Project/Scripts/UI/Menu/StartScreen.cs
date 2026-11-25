using System.Collections.Generic;
using LastSignal.Core;
using LastSignal.Database;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static LastSignal.UI.RuntimeUi;

namespace LastSignal.UI
{
    /// <summary>
    /// The start screen. Main page: New game / Load game / Settings / Quit.
    /// New game → create an account (or sign in) → fresh run. Load game → sign in → continue or restart.
    /// Administrator setup/login lives inside Settings so normal players don't run into it. Builds its UI in code.
    /// </summary>
    public sealed class StartScreen : MonoBehaviour
    {
        private enum Page { Main, Register, Login, Load, Settings, Admin }
        /// <summary>What the player wanted when they were sent to sign in / register.</summary>
        private enum Intent { NewGame, LoadGame }

        private readonly Dictionary<Page, GameObject> _pages = new();
        private TMP_Text _feedback, _signedIn, _loadWelcome, _loadHint, _loginHeading, _adminHeading, _adminHint;
        private TMP_Text _dbBanner;
        private TMP_InputField _loginUser, _loginPass, _regUser, _regPass, _regConfirm, _adminUser, _adminPass, _adminConfirm;
        private Button _quality;
        private Button _continue, _mainLogout, _adminSubmit, _adminOpenPanel, _adminLogout;
        private GameObject _restartConfirm, _adminForm;
        private Image _frame;
        private Button _newGame;

        private static readonly Color ItemColor = new(0.93f, 0.96f, 1f);
        private static readonly Color QuitColor = new(0.96f, 0.52f, 0.44f);
        private AdminPanel _admin;
        private Page _current;
        private Intent _intent;

        public bool IsAdminPanelOpen => _admin != null && _admin.IsOpen;

        private static bool SignedIn => GameServices.Auth.IsLoggedIn && GameServices.Auth.Current.UserId > 0;

        private void Start()
        {
            GameServices.State.Set(GameState.MainMenu);
            GameServices.Audio.SetAmbience(("amb_murmur", 0.35f), ("amb_station_hum", 0.15f));
            Build();
            Show(Page.Main);
            GameServices.Localization.LanguageChanged += RefreshTexts;
        }

        private void OnDestroy()
        {
            if (GameServices.Localization != null) GameServices.Localization.LanguageChanged -= RefreshTexts;
        }

        /// <summary>Texts set from code (welcome lines, admin page state) re-render in the new language.</summary>
        private void RefreshTexts()
        {
            if (IsAdminPanelOpen) return;
            UpdateQualityLabel();
            var keep = _feedback.text;
            bool confirmOpen = _restartConfirm.activeSelf;
            Show(_current);
            _feedback.text = keep;
            _restartConfirm.SetActive(confirmOpen && _current == Page.Load);
        }

        private void Build()
        {
            var canvas = Canvas("StartScreenUI", transform, 10).transform;
            Logo(canvas);
            var hints = Text("Hints", canvas, new Vector2(-330, 46), new Vector2(600, 32), 20, new Color(0.85f, 0.9f, 0.95f, 0.75f), "ui.start.hints", TextAlignmentOptions.Right, new Vector2(1, 0));
            hints.richText = true;
            _dbBanner = Text("DbBanner", canvas, new Vector2(0, -470), new Vector2(1500, 40), 22, Red);
            Text("Version", canvas, new Vector2(150, 30), new Vector2(300, 30), 16, Muted, null, TextAlignmentOptions.Left, Vector2.zero).text = $"v{Application.version}";
            var lang = Button("Language", canvas, new Vector2(-110, -50), new Vector2(170, 48), null, () => GameServices.Localization.ToggleLanguage(), 22);
            ((RectTransform)lang.transform).anchorMin = ((RectTransform)lang.transform).anchorMax = new Vector2(1, 1);
            SetButtonText(lang, "EN  |  БГ");

            // left column (as in a console game menu): the main page is plain text items, the other pages sit on a panel
            _frame = Panel("Frame", canvas, new Vector2(110 + 320, -70), new Vector2(640, 520));
            var anchor = new Vector2(0f, 0.5f);
            ((RectTransform)_frame.transform).anchorMin = ((RectTransform)_frame.transform).anchorMax = anchor;
            _frame.color = new Color(0.02f, 0.035f, 0.06f, 0.82f);
            _frame.GetComponent<Outline>().effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.45f);
            var frame = _frame.transform;
            _feedback = Text("Feedback", frame, new Vector2(0, -238), new Vector2(600, 36), 21, Red);

            // main: new game / load game / settings / quit
            var mp = Page_(Page.Main, frame);
            _signedIn = Text("SignedIn", mp, new Vector2(0, 225), new Vector2(600, 36), 21, TealLight, null, TextAlignmentOptions.Left);
            _newGame = MenuItem("NewGame", mp, 150, "ui.start.new_game", "ui.start.sub.new_game", NewGame, ItemColor);
            MenuItem("LoadGame", mp, 66, "ui.start.load_game", "ui.start.sub.load_game", LoadGame, ItemColor);
            MenuItem("Settings", mp, -18, "ui.start.settings", "ui.start.sub.settings", () => Show(Page.Settings), ItemColor);
            MenuItem("Quit", mp, -102, "ui.menu.quit", "ui.start.sub.quit", Quit, QuitColor);
            _mainLogout = MenuItem("Logout", mp, -186, "ui.start.logout", "ui.start.sub.logout", Logout, Muted, 24);

            // register (new game, no account yet)
            var rp = Page_(Page.Register, frame);
            Text("Heading", rp, new Vector2(0, 205), new Vector2(600, 40), 28, Cyan, "ui.start.register");
            Text("Hint", rp, new Vector2(0, 165), new Vector2(560, 36), 19, Muted, "ui.start.register_hint");
            _regUser = Input("Username", rp, new Vector2(0, 105), new Vector2(480, 56), "ui.start.username");
            _regPass = Input("Password", rp, new Vector2(0, 40), new Vector2(480, 56), "ui.start.password", true);
            _regConfirm = Input("Confirm", rp, new Vector2(0, -25), new Vector2(480, 56), "ui.start.confirm", true);
            Button("Create", rp, new Vector2(0, -100), new Vector2(480, 62), "ui.start.create_and_start", Register);
            Button("ToLogin", rp, new Vector2(-122, -175), new Vector2(236, 46), "ui.start.to_login", () => ToLogin(Intent.NewGame), 19);
            Button("Back", rp, new Vector2(122, -175), new Vector2(236, 46), "ui.dec.back", () => Show(Page.Main), 19);

            // login (load game, or new game with an existing account)
            var lp = Page_(Page.Login, frame);
            _loginHeading = Text("Heading", lp, new Vector2(0, 205), new Vector2(600, 40), 28, Cyan);
            _loginUser = Input("Username", lp, new Vector2(0, 120), new Vector2(480, 56), "ui.start.username");
            _loginPass = Input("Password", lp, new Vector2(0, 50), new Vector2(480, 56), "ui.start.password", true);
            Button("Login", lp, new Vector2(0, -35), new Vector2(480, 62), "ui.start.login_button", Login);
            Button("ToRegister", lp, new Vector2(-122, -120), new Vector2(236, 46), "ui.start.to_register", () => Show(Page.Register), 19);
            Button("Back", lp, new Vector2(122, -120), new Vector2(236, 46), "ui.dec.back", () => Show(Page.Main), 19);

            // load: continue the saved run or restart from the beginning
            var ld = Page_(Page.Load, frame);
            _loadWelcome = Text("Welcome", ld, new Vector2(0, 205), new Vector2(600, 40), 26, Cyan);
            _loadHint = Text("Hint", ld, new Vector2(0, 160), new Vector2(560, 40), 19, Muted);
            _continue = Button("Continue", ld, new Vector2(0, 85), new Vector2(480, 64), "ui.start.continue", Continue);
            Button("Restart", ld, new Vector2(0, 5), new Vector2(480, 64), "ui.start.restart", Restart);
            Button("Logout", ld, new Vector2(-122, -80), new Vector2(236, 46), "ui.start.logout", Logout, 19);
            Button("Back", ld, new Vector2(122, -80), new Vector2(236, 46), "ui.dec.back", () => Show(Page.Main), 19);
            _restartConfirm = Panel("RestartConfirm", ld, new Vector2(0, 20), new Vector2(560, 260)).gameObject;
            Text("Q", _restartConfirm.transform, new Vector2(0, 60), new Vector2(520, 90), 23, TextColor, "ui.start.new_game_confirm");
            Button("Yes", _restartConfirm.transform, new Vector2(-120, -60), new Vector2(220, 56), "ui.dec.confirm", StartNewGame, 22);
            Button("No", _restartConfirm.transform, new Vector2(120, -60), new Vector2(220, 56), "ui.dec.back", () => _restartConfirm.SetActive(false), 22);
            _restartConfirm.SetActive(false);

            // settings (+ a low-key way into administration)
            var sp = Page_(Page.Settings, frame);
            Text("Heading", sp, new Vector2(0, 205), new Vector2(600, 40), 28, Cyan, "ui.start.settings");
            Text("SensLabel", sp, new Vector2(-150, 140), new Vector2(260, 40), 22, TextColor, "ui.pause.sensitivity", TextAlignmentOptions.Left);
            Slider("Sensitivity", sp, new Vector2(120, 140), 240, SettingsService.SensitivityMin, SettingsService.SensitivityMax,
                GameServices.Settings.MouseSensitivity, GameServices.Settings.SetMouseSensitivity);
            Text("VolLabel", sp, new Vector2(-150, 80), new Vector2(260, 40), 22, TextColor, "ui.start.volume", TextAlignmentOptions.Left);
            Slider("Volume", sp, new Vector2(120, 80), 240, 0f, 1f, GameServices.Settings.MasterVolume, GameServices.Settings.SetMasterVolume);
            Toggle("InvertY", sp, new Vector2(0, 25), "ui.pause.invert_y", GameServices.Settings.InvertY, GameServices.Settings.SetInvertY);
            _quality = Button("Graphics", sp, new Vector2(0, -40), new Vector2(480, 56), null, CycleQuality);
            UpdateQualityLabel();
            Button("Language", sp, new Vector2(0, -105), new Vector2(480, 56), "ui.menu.language", () => GameServices.Localization.ToggleLanguage());
            Button("Back", sp, new Vector2(0, -170), new Vector2(220, 46), "ui.dec.back", () => Show(Page.Main), 20);
            var adminLink = Button("Administrator", sp, new Vector2(0, -222), new Vector2(260, 34), "ui.start.admin_link", () => Show(Page.Admin), 16);
            adminLink.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

            // administrator: create the first admin, or log in as one, then open the admin panel
            var ap = Page_(Page.Admin, frame);
            _adminHeading = Text("Heading", ap, new Vector2(0, 205), new Vector2(600, 40), 28, Cyan);
            _adminHint = Text("Hint", ap, new Vector2(0, 160), new Vector2(560, 50), 19, Muted);
            _adminForm = Rect("Form", ap, Vector2.zero, new Vector2(640, 520)).gameObject;
            _adminUser = Input("Username", _adminForm.transform, new Vector2(0, 95), new Vector2(480, 56), "ui.start.username");
            _adminPass = Input("Password", _adminForm.transform, new Vector2(0, 25), new Vector2(480, 56), "ui.start.password", true);
            _adminConfirm = Input("Confirm", _adminForm.transform, new Vector2(0, -45), new Vector2(480, 56), "ui.start.confirm", true);
            _adminSubmit = Button("Submit", _adminForm.transform, new Vector2(0, -115), new Vector2(480, 62), null, SubmitAdmin);
            _adminOpenPanel = Button("OpenPanel", ap, new Vector2(0, 60), new Vector2(480, 64), "ui.start.admin_panel", OpenAdmin);
            _adminLogout = Button("Logout", ap, new Vector2(0, -20), new Vector2(480, 56), "ui.start.logout", () => { Logout(); Show(Page.Admin); });
            Button("Back", ap, new Vector2(0, -192), new Vector2(220, 46), "ui.dec.back", () => Show(Page.Settings), 20);

            _admin = new GameObject("AdminPanel").AddComponent<AdminPanel>();
            _admin.transform.SetParent(transform, false);
            _admin.Closed += () => Show(Page.Admin);
        }

        /// <summary>Graphics quality: Low → Medium → High → Low (Medium is the default; Low for hot laptops).</summary>
        private void CycleQuality()
        {
            int count = Mathf.Max(1, QualitySettings.names.Length);
            GameServices.Settings.SetQualityLevel((GameServices.Settings.QualityLevel + 1) % count);
            UpdateQualityLabel();
        }

        private void UpdateQualityLabel()
        {
            if (_quality == null) return;
            string level = Loc.Get($"ui.quality.{GameServices.Settings.QualityLevel}");
            SetButtonText(_quality, Loc.Get("ui.start.graphics", level));
        }

        // ------------------------------------------------------------------ start-screen look

        /// <summary>Top-left logo: a glowing arc, the title in brushed silver, the tagline underneath.</summary>
        private static void Logo(Transform canvas)
        {
            var topLeft = new Vector2(0f, 1f);
            var arc = Image("LogoArc", canvas, new Vector2(110 + 300, -78), new Vector2(600, 96), new Color(Cyan.r, Cyan.g, Cyan.b, 0.85f), topLeft);
            arc.sprite = MenuArt.Arc;
            arc.raycastTarget = false;
            var shadow = Text("TitleShadow", canvas, new Vector2(110 + 450 + 3, -150 - 4), new Vector2(900, 96), 78, new Color(0f, 0f, 0f, 0.55f),
                "ui.game.title", TextAlignmentOptions.Left, topLeft);
            var title = Text("Title", canvas, new Vector2(110 + 450, -150), new Vector2(900, 96), 78, Color.white, "ui.game.title", TextAlignmentOptions.Left, topLeft);
            foreach (var t in new[] { shadow, title })
            {
                t.fontStyle = FontStyles.Bold;
                t.characterSpacing = 4;
                t.enableAutoSizing = true;
                t.fontSizeMin = 40;
                t.fontSizeMax = 78;
                t.textWrappingMode = TextWrappingModes.NoWrap;
            }
            title.enableVertexGradient = true;
            title.colorGradient = new VertexGradient(Color.white, Color.white, new Color(0.62f, 0.68f, 0.76f), new Color(0.62f, 0.68f, 0.76f));
            Text("Tagline", canvas, new Vector2(114 + 450, -212), new Vector2(900, 32), 21, TealLight, "ui.start.tagline", TextAlignmentOptions.Left, topLeft)
                .characterSpacing = 6;
        }

        /// <summary>Plain text item; when selected it gets the bevelled cyan frame (hex pattern inside) and its hint line.</summary>
        private static Button MenuItem(string name, Transform parent, float y, string key, string hintKey, UnityEngine.Events.UnityAction onClick,
            Color color, float fontSize = 34)
        {
            var hit = Image(name, parent, new Vector2(-10, y), new Vector2(600, 78), new Color(0, 0, 0, 0));
            var item = hit.transform;
            var highlight = Rect("Highlight", item, Vector2.zero, Vector2.zero);
            Stretch(highlight);
            var fill = Image("Fill", highlight, Vector2.zero, Vector2.zero, new Color(0.03f, 0.09f, 0.14f, 0.82f));
            fill.sprite = MenuArt.BevelFill;
            fill.type = UnityEngine.UI.Image.Type.Sliced;
            Stretch(fill.rectTransform);
            var hex = Image("Hex", highlight, Vector2.zero, Vector2.zero, new Color(Cyan.r, Cyan.g, Cyan.b, 0.07f));
            hex.sprite = MenuArt.Hex;
            hex.type = UnityEngine.UI.Image.Type.Tiled;
            Stretch(hex.rectTransform).offsetMin = new Vector2(10, 6);
            hex.rectTransform.offsetMax = new Vector2(-10, -6);
            var line = Image("Line", highlight, Vector2.zero, Vector2.zero, new Color(Cyan.r, Cyan.g, Cyan.b, 0.9f));
            line.sprite = MenuArt.BevelLine;
            line.type = UnityEngine.UI.Image.Type.Sliced;
            Stretch(line.rectTransform);
            foreach (var g in new Graphic[] { fill, hex, line }) g.raycastTarget = false;
            var hint = Text("Hint", highlight, new Vector2(0, -20), new Vector2(560, 26), 18, new Color(0.78f, 0.86f, 0.92f), hintKey, TextAlignmentOptions.Left);
            hint.textWrappingMode = TextWrappingModes.NoWrap;
            hint.enableAutoSizing = true;
            hint.fontSizeMin = 13;
            hint.fontSizeMax = 18;

            var label = Text("Label", item, new Vector2(0, 9), new Vector2(560, 46), fontSize, color, key, TextAlignmentOptions.Left);
            label.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            label.characterSpacing = 3;

            var b = hit.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(onClick);
            b.onClick.AddListener(() => { if (GameServices.IsReady) GameServices.Audio.Sfx("sfx_ui", 0.8f); });
            hit.gameObject.AddComponent<MenuItemSelect>().Setup(highlight.gameObject, label, color, color == QuitColor ? QuitColor : Cyan);
            return b;
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
            _frame.enabled = page != Page.Main; // the main page floats over the backdrop, the forms keep their panel
            _frame.GetComponent<Outline>().enabled = page != Page.Main;
            _feedback.text = string.Empty;
            _dbBanner.text = GameServices.Database.IsAvailable ? string.Empty : Loc.Get("auth.err.no_database");
            var s = GameServices.Auth.Current;
            switch (page)
            {
                case Page.Main:
                    _signedIn.text = SignedIn ? Loc.Get(s.IsAdmin ? "ui.start.welcome_admin" : "ui.start.welcome", s.Username) : string.Empty;
                    _mainLogout.gameObject.SetActive(SignedIn);
                    if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_newGame.gameObject);
                    break;
                case Page.Login:
                    _loginHeading.text = Loc.Get(_intent == Intent.LoadGame ? "ui.start.login_load" : "ui.start.login");
                    break;
                case Page.Load:
                    bool hasSave = GameServices.Saves.HasSaveForCurrentUser;
                    _loadWelcome.text = Loc.Get(s.IsAdmin ? "ui.start.welcome_admin" : "ui.start.welcome", s.Username);
                    _loadHint.text = Loc.Get(hasSave ? "ui.start.load_hint" : "ui.start.no_save");
                    _continue.interactable = hasSave;
                    _restartConfirm.SetActive(false);
                    break;
                case Page.Admin:
                    ShowAdminState();
                    break;
            }
            var first = page switch
            {
                Page.Login => _loginUser,
                Page.Register => _regUser,
                Page.Admin when _adminForm.activeSelf => _adminUser,
                _ => null,
            };
            if (first != null) first.Select();
        }

        /// <summary>Admin page has three states: no admin yet (create), not signed in as admin (log in), admin (open panel).</summary>
        private void ShowAdminState()
        {
            var auth = GameServices.Auth;
            bool create = auth.NeedsFirstAdmin;
            bool isAdmin = auth.IsAdmin;
            _adminForm.SetActive(!isAdmin);
            _adminConfirm.gameObject.SetActive(create);
            _adminOpenPanel.gameObject.SetActive(isAdmin);
            _adminLogout.gameObject.SetActive(isAdmin);
            _adminUser.text = _adminPass.text = _adminConfirm.text = string.Empty;
            _adminHeading.text = Loc.Get(create ? "ui.start.first_admin" : isAdmin ? "ui.start.admin_signed_in" : "ui.start.admin_login");
            _adminHint.text = isAdmin ? auth.Current.Username : Loc.Get(create ? "ui.start.first_admin_hint" : "ui.start.admin_login_hint");
            SetButtonText(_adminSubmit, Loc.Get(create ? "ui.start.create_admin" : "ui.start.login_button"));
            ((RectTransform)_adminSubmit.transform).anchoredPosition = new Vector2(0, create ? -115 : -50);
        }

        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null || IsAdminPanelOpen) return;
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                if (_current == Page.Login) Login();
                else if (_current == Page.Register) Register();
                else if (_current == Page.Admin && _adminForm.activeSelf) SubmitAdmin();
            }
            if (kb.escapeKey.wasPressedThisFrame && _current != Page.Main)
                Show(_current == Page.Admin ? Page.Settings : Page.Main);
            if (kb.tabKey.wasPressedThisFrame) FocusNext();
        }

        private void FocusNext()
        {
            var fields = _current switch
            {
                Page.Login => new[] { _loginUser, _loginPass },
                Page.Register => new[] { _regUser, _regPass, _regConfirm },
                Page.Admin when GameServices.Auth.NeedsFirstAdmin => new[] { _adminUser, _adminPass, _adminConfirm },
                Page.Admin => new[] { _adminUser, _adminPass },
                _ => null,
            };
            if (fields == null || !fields[0].gameObject.activeInHierarchy) return;
            int i = System.Array.FindIndex(fields, f => f.isFocused);
            fields[(i + 1) % fields.Length].Select();
        }

        private void Fail(string key)
        {
            _feedback.text = Loc.Get(key);
            GameServices.Audio.Sfx("sfx_terminal_error", 0.7f);
        }

        // ------------------------------------------------------------------ player flow

        private void NewGame()
        {
            if (!SignedIn) { _intent = Intent.NewGame; Show(Page.Register); return; }
            if (GameServices.Saves.HasSaveForCurrentUser) { Show(Page.Load); _restartConfirm.SetActive(true); return; }
            StartNewGame();
        }

        private void LoadGame()
        {
            if (!SignedIn) { ToLogin(Intent.LoadGame); return; }
            Show(Page.Load);
        }

        private void ToLogin(Intent intent)
        {
            _intent = intent;
            _loginPass.text = string.Empty;
            Show(Page.Login);
        }

        private void Login()
        {
            var err = GameServices.Auth.Login(_loginUser.text, _loginPass.text);
            _loginPass.text = string.Empty;
            if (err != null) { Fail(err); return; }
            GameServices.Audio.Sfx("sfx_terminal_ok", 0.7f);
            if (_intent == Intent.NewGame) NewGame();
            else Show(Page.Load);
        }

        private void Register()
        {
            var err = GameServices.Auth.Register(_regUser.text, _regPass.text, _regConfirm.text, UserRole.Player, out _);
            if (err != null) { Fail(err); return; }
            err = GameServices.Auth.Login(_regUser.text, _regPass.text);
            _regPass.text = _regConfirm.text = string.Empty;
            if (err != null) { Fail(err); return; }
            GameServices.Audio.Sfx("sfx_terminal_ok", 0.7f);
            StartNewGame();
        }

        private void Continue()
        {
            if (!GameServices.Saves.Continue()) Fail(GameServices.Saves.LastError ?? "save.err.missing");
        }

        private void Restart()
        {
            if (GameServices.Saves.HasSaveForCurrentUser) _restartConfirm.SetActive(true);
            else StartNewGame();
        }

        private void StartNewGame()
        {
            GameServices.StartNewRun();
            GameServices.Scenes.Load(SceneIds.CutsceneOpening);
        }

        // ------------------------------------------------------------------ administration

        private void SubmitAdmin()
        {
            var auth = GameServices.Auth;
            if (auth.NeedsFirstAdmin)
            {
                var err = auth.Register(_adminUser.text, _adminPass.text, _adminConfirm.text, UserRole.Admin, out _);
                if (err != null) { Fail(err); return; }
            }
            var loginErr = auth.Login(_adminUser.text, _adminPass.text);
            _adminPass.text = _adminConfirm.text = string.Empty;
            if (loginErr != null) { Fail(loginErr); return; }
            if (!auth.IsAdmin) { auth.Logout(); Fail("auth.err.not_admin"); return; }
            GameServices.Audio.Sfx("sfx_terminal_ok", 0.7f);
            Show(Page.Admin);
        }

#if UNITY_EDITOR
        /// <summary>Editor preview: opens the admin panel with an in-memory admin session.</summary>
        public void PreviewAdminPanel()
        {
            GameServices.Auth.UseDevAdminSession();
            OpenAdmin();
        }
#endif

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
            Show(Page.Main);
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
