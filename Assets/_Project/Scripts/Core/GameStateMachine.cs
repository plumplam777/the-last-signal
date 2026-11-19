using System;
using UnityEngine;

namespace LastSignal.Core
{
    public enum GameState
    {
        Boot,
        MainMenu,
        Cutscene,
        Playing,
        /// <summary>Gameplay world visible but a UI (inventory, terminal, dialogue) has focus. Time keeps running.</summary>
        InMenu,
        Paused,
        Ending,
    }

    /// <summary>
    /// High-level game state. Owns cursor lock and time scale so no other script fights over them.
    /// Systems react through <see cref="Changed"/>.
    /// </summary>
    public sealed class GameStateMachine
    {
        public GameState Current { get; private set; } = GameState.Boot;

        /// <summary>(previous, next)</summary>
        public event Action<GameState, GameState> Changed;

        public bool IsGameplayActive => Current == GameState.Playing;

        public void Set(GameState next)
        {
            if (next == Current) return;
            var previous = Current;
            Current = next;
            Apply(next);
            Log.Info(nameof(GameStateMachine), $"{previous} -> {next}");
            Changed?.Invoke(previous, next);
        }

        private static void Apply(GameState state)
        {
            bool lockCursor = state == GameState.Playing;
            Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !lockCursor;
            Time.timeScale = state == GameState.Paused ? 0f : 1f;
        }
    }
}
