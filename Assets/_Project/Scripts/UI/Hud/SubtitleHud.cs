using LastSignal.Audio;
using LastSignal.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.UI
{
    /// <summary>Bottom subtitle bar for audio logs (later also CAIRN): speaker, line, progress bar.</summary>
    public sealed class SubtitleHud : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text speaker;
        [SerializeField] private TMP_Text line;
        [SerializeField] private Image progress;

        private void OnEnable()
        {
            var logs = GameServices.AudioLogs;
            logs.Started += OnStarted;
            logs.Finished += OnFinished;
            logs.SubtitleChanged += OnLine;
            GameServices.Localization.LanguageChanged += Refresh;
            group.alpha = logs.IsPlaying ? 1f : 0f;
            Refresh();
        }

        private void OnDisable()
        {
            if (!GameServices.IsReady) return;
            var logs = GameServices.AudioLogs;
            logs.Started -= OnStarted;
            logs.Finished -= OnFinished;
            logs.SubtitleChanged -= OnLine;
            GameServices.Localization.LanguageChanged -= Refresh;
        }

        private void OnStarted(AudioLogDefinition log)
        {
            group.alpha = 1f;
            Refresh();
        }

        private void OnFinished(AudioLogDefinition log) => group.alpha = 0f;

        private void OnLine(string key) => Refresh();

        private void Refresh()
        {
            var logs = GameServices.AudioLogs;
            if (!logs.IsPlaying) return;
            speaker.text = $"{Loc.Get(logs.Current.SpeakerKey)}  <color=#8A8F93>·  {Loc.Get(logs.Current.TitleKey)}</color>";
            line.text = string.IsNullOrEmpty(logs.CurrentLineKey) ? string.Empty : Loc.Get(logs.CurrentLineKey);
        }

        private void Update()
        {
            var logs = GameServices.AudioLogs;
            if (logs.IsPlaying && progress != null) progress.fillAmount = Mathf.Clamp01(logs.Elapsed / Mathf.Max(0.01f, logs.Duration));
        }

#if UNITY_EDITOR
        public void EditorBind(CanvasGroup g, TMP_Text s, TMP_Text l, Image p) { group = g; speaker = s; line = l; progress = p; }
#endif
    }
}
