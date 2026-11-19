using LastSignal.Core;
using UnityEngine;

namespace LastSignal.UI
{
    /// <summary>
    /// Base for full-screen in-game UIs (inventory, terminal). Opening switches the game to InMenu
    /// (cursor free, player input off); only one screen can be open at a time. Esc / UI.Cancel closes.
    /// </summary>
    public abstract class UiScreen : MonoBehaviour
    {
        private static UiScreen _open;

        [SerializeField] protected GameObject panel;

        public static bool AnyOpen => _open != null;
        public bool IsOpen => _open == this;

        protected virtual void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        protected virtual void OnDestroy()
        {
            if (_open == this) _open = null;
        }

        protected virtual void Update()
        {
            if (IsOpen && GameServices.Input.Controls.UI.Cancel.WasPressedThisFrame()) Close();
        }

        protected bool TryOpen()
        {
            if (_open != null || GameServices.State.Current != GameState.Playing) return false;
            _open = this;
            panel.SetActive(true);
            GameServices.State.Set(GameState.InMenu);
            OnOpened();
            return true;
        }

        public void Close()
        {
            if (!IsOpen) return;
            OnClosing();
            panel.SetActive(false);
            _open = null;
            if (GameServices.State.Current == GameState.InMenu) GameServices.State.Set(GameState.Playing);
        }

        protected virtual void OnOpened() { }
        protected virtual void OnClosing() { }
    }
}
