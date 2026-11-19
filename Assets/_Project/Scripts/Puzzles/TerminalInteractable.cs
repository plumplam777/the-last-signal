using System;
using LastSignal.Core;
using LastSignal.Interaction;
using UnityEngine;

namespace LastSignal.Puzzles
{
    /// <summary>
    /// A console in the world that opens the terminal screen with its <see cref="TerminalDefinition"/>.
    /// Powered terminals need their sector powered (requirements in the base class).
    /// </summary>
    public sealed class TerminalInteractable : Interactable
    {
        /// <summary>Raised to ask the UI to open a terminal. The terminal screen on the player listens.</summary>
        public static event Action<TerminalDefinition> OpenRequested;

        [SerializeField] private TerminalDefinition terminal;
        [Tooltip("Screen renderer that glows while the terminal is usable.")]
        [SerializeField] private GameObject screenGlow;

        public TerminalDefinition Terminal => terminal;

        protected override void Start()
        {
            oneShot = false;
            if (terminal == null) Log.Error(nameof(TerminalInteractable), $"'{name}' has no terminal definition.", this);
            base.Start();
        }

        public override string GetPromptText()
            => Loc.Get("prompt.terminal.use", terminal != null ? Loc.Get(terminal.TitleKey) : string.Empty);

        protected override void OnInteract(PlayerInteractor interactor)
        {
            if (OpenRequested == null)
            {
                Log.Warn(nameof(TerminalInteractable), "No terminal screen is listening.", this);
                return;
            }
            OpenRequested.Invoke(terminal);
        }

        private void Update()
        {
            if (screenGlow == null) return;
            bool usable = requirements.Evaluate();
            if (screenGlow.activeSelf != usable) screenGlow.SetActive(usable);
        }

#if UNITY_EDITOR
        public void EditorSetTerminal(TerminalDefinition def, GameObject glow)
        {
            terminal = def;
            screenGlow = glow;
        }
#endif
    }
}
