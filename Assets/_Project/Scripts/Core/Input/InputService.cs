using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastSignal.Core
{
    /// <summary>
    /// Owns the single <see cref="LastSignalControls"/> instance (generated from LastSignalControls.inputactions).
    /// Gameplay reads <see cref="Controls"/>.Player / .Interaction; menus use .UI.
    /// Map switching follows the game state so scripts never enable/disable maps themselves.
    /// </summary>
    public sealed class InputService : IDisposable
    {
        private const string BindingsPrefKey = "ls.input.bindings";

        public LastSignalControls Controls { get; }

        public InputService()
        {
            Controls = new LastSignalControls();
            LoadBindingOverrides();
            Controls.UI.Enable();
        }

        public void ApplyState(GameState state)
        {
            bool gameplay = state == GameState.Playing;
            // Interaction map stays on while playing so E / Q / Esc work; Esc must also work in InMenu/Paused,
            // which is handled by UI.Cancel.
            if (gameplay)
            {
                Controls.Player.Enable();
                Controls.Interaction.Enable();
            }
            else
            {
                Controls.Player.Disable();
                Controls.Interaction.Disable();
            }
            Controls.UI.Enable();
        }

        public void SaveBindingOverrides()
        {
            PlayerPrefs.SetString(BindingsPrefKey, Controls.asset.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
        }

        private void LoadBindingOverrides()
        {
            var json = PlayerPrefs.GetString(BindingsPrefKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                Controls.asset.LoadBindingOverridesFromJson(json);
            }
            catch (Exception e)
            {
                Log.Warn(nameof(InputService), $"Ignoring corrupt binding overrides: {e.Message}");
                PlayerPrefs.DeleteKey(BindingsPrefKey);
            }
        }

        public void Dispose()
        {
            Controls.Disable();
            Controls.Dispose();
        }
    }
}
