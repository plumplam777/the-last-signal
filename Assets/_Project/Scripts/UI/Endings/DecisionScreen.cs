using System;
using LastSignal.Core;
using LastSignal.Endings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.UI
{
    /// <summary>
    /// The final decision (StoryBible §9.2): A destroy / B restore / C silence (only with all logs), then a
    /// "cannot be undone" confirmation. Confirming stores choice.final and loads the Ending scene.
    /// Opened by the captain's console in the finale (or the debug menu) through <see cref="OpenRequested"/>.
    /// </summary>
    public sealed class DecisionScreen : UiScreen
    {
        public static event Action OpenRequested;
        public static void RequestOpen() => OpenRequested?.Invoke();

        [SerializeField] private GameObject choicePage;
        [SerializeField] private Button optionA;
        [SerializeField] private Button optionB;
        [SerializeField] private Button optionC;
        [SerializeField] private GameObject confirmPage;
        [SerializeField] private TMP_Text confirmChoice;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button backButton;

        private EndingType _pending;

        protected override void Awake()
        {
            base.Awake();
            optionA.onClick.AddListener(() => Pick(EndingType.Destroy));
            optionB.onClick.AddListener(() => Pick(EndingType.Restore));
            optionC.onClick.AddListener(() => Pick(EndingType.Silence));
            confirmButton.onClick.AddListener(Confirm);
            backButton.onClick.AddListener(Back);
            OpenRequested += Open;
        }

        protected override void OnDestroy()
        {
            OpenRequested -= Open;
            base.OnDestroy();
        }

        public void Open() => TryOpen();

        protected override void OnOpened()
        {
            _pending = EndingType.None;
            choicePage.SetActive(true);
            confirmPage.SetActive(false);
            bool hidden = EndingScript.HiddenEndingAvailable(GameServices.AudioLogs.HiddenEndingCollected);
            optionC.gameObject.SetActive(hidden);
        }

        protected override void Update()
        {
            // Esc goes back from the confirmation instead of closing everything
            if (IsOpen && confirmPage.activeSelf && GameServices.Input.Controls.UI.Cancel.WasPressedThisFrame())
            {
                Back();
                return;
            }
            base.Update();
        }

        public void Pick(EndingType type)
        {
            if (type == EndingType.Silence && !EndingScript.HiddenEndingAvailable(GameServices.AudioLogs.HiddenEndingCollected)) return;
            _pending = type;
            choicePage.SetActive(false);
            confirmPage.SetActive(true);
            confirmChoice.text = Loc.Get(type switch
            {
                EndingType.Destroy => "ui.dec.option_a",
                EndingType.Restore => "ui.dec.option_b",
                _ => "ui.dec.option_c",
            });
        }

        public void Confirm()
        {
            if (_pending == EndingType.None) return;
            EndingScript.Commit(GameServices.World, _pending);
            var auth = GameServices.Auth;
            if (auth.IsLoggedIn && auth.Current.UserId > 0 && GameServices.Database.IsAvailable)
            {
                GameServices.Database.Progress.RecordChoice(auth.Current.UserId, "final_decision", EndingScript.Id(_pending));
                GameServices.Database.Progress.RecordChoice(auth.Current.UserId, "ai_trust_at_end", GameServices.World.GetInt(WorldKeys.AiTrust).ToString());
                GameServices.Checkpoint();
            }
            Log.Info(nameof(DecisionScreen), $"Final decision: {_pending}");
            Close();
            if (_pending == EndingType.Destroy && EscapeSequence.Instance != null)
            {
                // Ending A is played: get back to the Tern before the reactor goes (StoryBible §9.3)
                EscapeSequence.Instance.Begin();
                return;
            }
            GameServices.State.Set(GameState.Ending);
            GameServices.Scenes.Load(SceneIds.Ending);
        }

        private void Back()
        {
            _pending = EndingType.None;
            confirmPage.SetActive(false);
            choicePage.SetActive(true);
        }

#if UNITY_EDITOR
        public void EditorBind(GameObject p, GameObject choices, Button a, Button b, Button c, GameObject confirm, TMP_Text confirmText, Button ok, Button back)
        {
            panel = p; choicePage = choices; optionA = a; optionB = b; optionC = c; confirmPage = confirm; confirmChoice = confirmText; confirmButton = ok; backButton = back;
        }
#endif
    }
}
