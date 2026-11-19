using LastSignal.Audio;
using LastSignal.Core;
using UnityEngine;

namespace LastSignal.Interaction
{
    /// <summary>
    /// A recorder in the world. Interacting collects the log and plays it; afterwards it can be replayed here
    /// or from the log list. The recorder's light dims once collected.
    /// </summary>
    public sealed class AudioLogPickup : Interactable
    {
        [SerializeField] private AudioLogDefinition log;
        [Tooltip("Shown while not yet collected (e.g. a blinking indicator).")]
        [SerializeField] private GameObject uncollectedIndicator;

        public AudioLogDefinition Log => log;

        protected override void Start()
        {
            oneShot = false;
            if (log == null) Core.Log.Error(nameof(AudioLogPickup), $"'{name}' has no audio log assigned.", this);
            base.Start();
            RefreshIndicator();
        }

        public override string GetPromptText()
        {
            if (log == null) return string.Empty;
            bool collected = GameServices.AudioLogs.IsCollected(log);
            var title = Loc.Get(log.TitleKey);
            return Loc.Get(collected ? "prompt.log.replay" : "prompt.log.play", title);
        }

        protected override void OnInteract(PlayerInteractor interactor)
        {
            var logs = GameServices.AudioLogs;
            if (logs.Current == log) logs.Stop();
            else logs.Collect(log);
            RefreshIndicator();
        }

        private void RefreshIndicator()
        {
            if (uncollectedIndicator != null && log != null)
                uncollectedIndicator.SetActive(!GameServices.AudioLogs.IsCollected(log));
        }

#if UNITY_EDITOR
        public void EditorSetLog(AudioLogDefinition newLog, GameObject indicator)
        {
            log = newLog;
            uncollectedIndicator = indicator;
            promptKey = "prompt.log.play";
        }
#endif
    }
}
