using System.Collections.Generic;
using LastSignal.Core;
using LastSignal.Puzzles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.UI
{
    /// <summary>
    /// Shows a <see cref="TerminalDefinition"/>: a PIN keypad first if the terminal is locked, then the list of
    /// visible entries and the selected entry's text. Number keys also work on the keypad.
    /// </summary>
    public sealed class TerminalScreen : UiScreen
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text subtitle;
        [Header("Lock")]
        [SerializeField] private GameObject lockPage;
        [SerializeField] private TMP_Text pinDisplay;
        [SerializeField] private TMP_Text lockHint;
        [SerializeField] private TMP_Text lockFeedback;
        [SerializeField] private Transform keypad;
        [Header("Entries")]
        [SerializeField] private GameObject entriesPage;
        [SerializeField] private Transform entryList;
        [SerializeField] private Button entryTemplate;
        [SerializeField] private TMP_Text entryTitle;
        [SerializeField] private TMP_Text entryBody;

        private TerminalDefinition _terminal;
        private CodeLock _lock;
        private readonly List<GameObject> _rows = new();
        private readonly Dictionary<int, TMP_Text> _rowLabels = new();
        private float _feedbackTimer;

        public TerminalDefinition Current => IsOpen ? _terminal : null;

        protected override void Awake()
        {
            base.Awake();
            entryTemplate.gameObject.SetActive(false);
            foreach (var button in keypad.GetComponentsInChildren<Button>(true))
            {
                var key = button.name.Replace("Key_", string.Empty);
                button.onClick.AddListener(() => PressKey(key));
            }
            TerminalInteractable.OpenRequested += Open;
        }

        protected override void OnDestroy()
        {
            TerminalInteractable.OpenRequested -= Open;
            base.OnDestroy();
        }

        public void Open(TerminalDefinition terminal)
        {
            if (terminal == null) return;
            _terminal = terminal;
            if (!TryOpen()) return;
        }

        protected override void OnOpened()
        {
            GameServices.Audio.Sfx("sfx_terminal_open", 0.8f);
            title.text = Loc.Get(_terminal.TitleKey);
            subtitle.text = string.IsNullOrEmpty(_terminal.SubtitleKey) ? string.Empty : Loc.Get(_terminal.SubtitleKey);
            bool locked = !_terminal.IsUnlocked(GameServices.World);
            _lock = locked ? new CodeLock(_terminal.PinCode) : null;
            lockPage.SetActive(locked);
            entriesPage.SetActive(!locked);
            lockFeedback.text = string.Empty;
            lockHint.text = string.IsNullOrEmpty(_terminal.LockHintKey) ? string.Empty : Loc.Get(_terminal.LockHintKey);
            RefreshPin();
            if (!locked) BuildEntries();
        }

        protected override void Update()
        {
            base.Update();
            if (!IsOpen) return;
            if (_feedbackTimer > 0f && (_feedbackTimer -= Time.unscaledDeltaTime) <= 0f) lockFeedback.text = string.Empty;
            if (_lock == null) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            for (int d = 0; d <= 9; d++)
            {
                // Key enum order is Digit1..Digit9, Digit0; numpad is Numpad0..Numpad9.
                var digitKey = d == 0 ? UnityEngine.InputSystem.Key.Digit0 : (UnityEngine.InputSystem.Key)((int)UnityEngine.InputSystem.Key.Digit1 + d - 1);
                if (kb[digitKey].wasPressedThisFrame
                    || kb[(UnityEngine.InputSystem.Key)((int)UnityEngine.InputSystem.Key.Numpad0 + d)].wasPressedThisFrame)
                    PressKey(d.ToString());
            }
            if (kb.backspaceKey.wasPressedThisFrame) PressKey("Back");
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) PressKey("Enter");
        }

        /// <summary>Keypad input: "0".."9", "Back", "Clear", "Enter". Public for tests and gamepad UI.</summary>
        public void PressKey(string key)
        {
            if (_lock == null) return;
            GameServices.Audio.Sfx("sfx_terminal_key", 0.7f);
            switch (key)
            {
                case "Back": _lock.Backspace(); break;
                case "Clear": _lock.Clear(); break;
                case "Enter":
                    if (_lock.Submit())
                    {
                        GameServices.Audio.Sfx("sfx_terminal_ok");
                        Unlock();
                    }
                    else
                    {
                        GameServices.Audio.Sfx("sfx_terminal_error");
                        ShowFeedback("terminal.pin.wrong");
                    }
                    break;
                default:
                    if (key.Length == 1) _lock.Press(key[0]);
                    break;
            }
            RefreshPin();
        }

        private void Unlock()
        {
            GameServices.World.SetBool(WorldKeys.PuzzleSolved(_terminal.LockPuzzleId), true);
            Effects.ApplyAll(_terminal.OnUnlock);
            _lock = null;
            lockPage.SetActive(false);
            entriesPage.SetActive(true);
            BuildEntries();
        }

        private void ShowFeedback(string key)
        {
            lockFeedback.text = Loc.Get(key);
            _feedbackTimer = 2f;
        }

        private void RefreshPin()
        {
            if (_lock == null)
            {
                pinDisplay.text = string.Empty;
                return;
            }
            var shown = _lock.Entry.PadRight(_lock.MaxLength, '_');
            pinDisplay.text = string.Join(" ", shown.ToCharArray());
        }

        private void BuildEntries()
        {
            foreach (var row in _rows) Destroy(row);
            _rows.Clear();
            _rowLabels.Clear();
            int first = -1;
            for (int i = 0; i < _terminal.Entries.Count; i++)
            {
                var entry = _terminal.Entries[i];
                if (!entry.visibleWhen.Evaluate()) continue;
                if (first < 0) first = i;
                var row = Instantiate(entryTemplate, entryList);
                row.gameObject.SetActive(true);
                bool read = GameServices.World.GetBool(TerminalDefinition.ReadKey(_terminal.Id, i));
                var label = row.GetComponentInChildren<TMP_Text>();
                label.text = (read ? "  " : "<color=#5FE6FF>●</color> ") + Loc.Get(entry.titleKey);
                _rowLabels[i] = label;
                int index = i;
                row.onClick.AddListener(() => ShowEntry(index));
                _rows.Add(row.gameObject);
            }
            if (first >= 0) ShowEntry(first);
            else
            {
                entryTitle.text = string.Empty;
                entryBody.text = Loc.Get("terminal.no_entries");
            }
        }

        private void ShowEntry(int index)
        {
            var entry = _terminal.Entries[index];
            entryTitle.text = Loc.Get(entry.titleKey);
            entryBody.text = Loc.Get(entry.bodyKey);
            GameServices.Audio.Sfx("sfx_terminal_key", 0.5f);
            var readKey = TerminalDefinition.ReadKey(_terminal.Id, index);
            if (!GameServices.World.GetBool(readKey))
            {
                GameServices.World.SetBool(readKey, true);
                Effects.ApplyAll(entry.onRead);
                if (_rowLabels.TryGetValue(index, out var label)) label.text = "  " + Loc.Get(entry.titleKey);
            }
        }

#if UNITY_EDITOR
        public void EditorBind(GameObject p, TMP_Text t, TMP_Text st, GameObject lp, TMP_Text pin, TMP_Text hint, TMP_Text fb, Transform pad,
            GameObject ep, Transform list, Button template, TMP_Text et, TMP_Text eb)
        {
            panel = p; title = t; subtitle = st; lockPage = lp; pinDisplay = pin; lockHint = hint; lockFeedback = fb; keypad = pad;
            entriesPage = ep; entryList = list; entryTemplate = template; entryTitle = et; entryBody = eb;
        }
#endif
    }
}
