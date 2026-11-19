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

        /// <summary>True while the cursor is really captured for mouse look (frame-accurate).</summary>
        public bool IsCursorCaptured { get; private set; }

        private int _capturedSinceFrame = -1;

        /// <summary>
        /// Called every frame by GameServices. Re-captures the cursor while playing: the Unity Editor (and macOS on
        /// focus changes) can release the lock on its own, e.g. when Esc closes a menu, which otherwise leaves a
        /// visible system cursor moving independently of the camera.
        /// </summary>
        public void EnforceCursor()
        {
            bool want = Current == GameState.Playing && Application.isFocused;
            if (want && (Cursor.lockState != CursorLockMode.Locked || Cursor.visible))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            bool captured = want && Cursor.lockState == CursorLockMode.Locked;
            if (captured && !IsCursorCaptured) _capturedSinceFrame = Time.frameCount;
            IsCursorCaptured = captured;
        }

        /// <summary>Mouse look should ignore the first frames after (re)capture: they carry the jump of the freed cursor.</summary>
        public bool MouseLookReady => IsCursorCaptured && Time.frameCount > _capturedSinceFrame + 1;

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
