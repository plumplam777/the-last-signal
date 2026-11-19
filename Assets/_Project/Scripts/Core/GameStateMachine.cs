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
            var mouse = UnityEngine.InputSystem.Mouse.current;

            // A click re-applies the lock: in the Editor the lock only takes effect once the Game view has focus.
            if (want && NeedsClick && mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                _relockFrame = Time.frameCount + 1;
                _movingFrames = 0;
                _clickFrame = Time.frameCount;
                NeedsClick = false;
            }
            if (want && Time.frameCount >= _relockFrame && (Cursor.lockState != CursorLockMode.Locked || Cursor.visible))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // Detect a "fake" lock: Unity reports Locked but the pointer still moves (macOS Editor, Game view
            // not focused). A really locked pointer stays still.
            if (mouse != null)
            {
                var pos = mouse.position.ReadValue();
                bool moved = (pos - _lastPointer).sqrMagnitude > 9f;
                _lastPointer = pos;
                if (want && Cursor.lockState == CursorLockMode.Locked && moved) _movingFrames++;
                else if (!moved) _movingFrames = Mathf.Max(0, _movingFrames - 1);
                if (_movingFrames >= 4 && !_detectionDisabled)
                {
                    if (_clickFrame > 0 && Time.frameCount - _clickFrame < 40)
                    {
                        // still "moving" right after a fresh lock: this platform reports positions while locked,
                        // so the heuristic cannot be used — fall back to trusting Cursor.lockState.
                        _detectionDisabled = true;
                        Log.Warn(nameof(GameStateMachine), "Pointer moves while locked on this platform; fake-lock detection disabled.");
                    }
                    else NeedsClick = true;
                    _movingFrames = 0;
                }
            }
            if (!want) NeedsClick = false;

            bool captured = want && Cursor.lockState == CursorLockMode.Locked && !NeedsClick;
            if (captured && !IsCursorCaptured) _capturedSinceFrame = Time.frameCount;
            IsCursorCaptured = captured;
        }

        /// <summary>True when the pointer is not really captured and the player must click the game to look around.</summary>
        public bool NeedsClick { get; private set; }

        private Vector2 _lastPointer;
        private int _movingFrames;
        private int _relockFrame;
        private int _clickFrame;
        private bool _detectionDisabled;

        /// <summary>Mouse look should ignore the first frames after (re)capture: they carry the jump of the freed cursor.</summary>
        public bool MouseLookReady => IsCursorCaptured && Time.frameCount > _capturedSinceFrame + 2;

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
