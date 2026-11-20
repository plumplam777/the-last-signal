using UnityEngine;

namespace LastSignal.Core
{
    /// <summary>
    /// Keeps the laptop cool: vSync off (on a 120 Hz ProMotion screen vSync would mean 120 fps) and an explicit cap —
    /// 60 fps while playing, 30 in menus / pause / dialogue screens, 10 when the window is not focused.
    /// </summary>
    public sealed class FrameRateGovernor : MonoBehaviour
    {
        public const int PlayingFps = 60;
        public const int MenuFps = 30;
        public const int BackgroundFps = 10;

        private bool _focused = true;

        private void Start()
        {
            QualitySettings.vSyncCount = 0;
            GameServices.State.Changed += (_, _) => Apply();
            Apply();
        }

        private void OnApplicationFocus(bool focus)
        {
            _focused = focus;
            Apply();
        }

        /// <summary>The cap for a given state and focus (pure; used by tests).</summary>
        public static int TargetFor(GameState state, bool focused)
        {
            if (!focused && !Application.isEditor) return BackgroundFps; // the editor is often unfocused during tests
            return state is GameState.Playing or GameState.Cutscene or GameState.Ending ? PlayingFps : MenuFps;
        }

        private void Apply()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFor(GameServices.State.Current, _focused);
        }
    }
}
