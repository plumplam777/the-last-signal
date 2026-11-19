using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastSignal.Audio
{
    [Serializable]
    public sealed class SubtitleLine
    {
        [Tooltip("Seconds from the start of the log.")]
        public float start;
        [Tooltip("Localization key of the line.")]
        public string textKey;
    }

    /// <summary>
    /// A collectible recording. Works without an AudioClip (subtitles play on their own timing) so logs
    /// can be tested before text-to-speech/voice recordings exist.
    /// </summary>
    [CreateAssetMenu(menuName = "Last Signal/Audio Log", fileName = "log_new")]
    public sealed class AudioLogDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string sectorId;
        [SerializeField] private string titleKey;
        [Tooltip("Localization key of the speaker name.")]
        [SerializeField] private string speakerKey;
        [Tooltip("In-world date label, e.g. 'Day -61'. Localization key.")]
        [SerializeField] private string dateKey;
        [SerializeField] private AudioClip englishClip;
        [SerializeField] private AudioClip bulgarianClip;
        [Tooltip("Used when there is no clip.")]
        [SerializeField, Min(1f)] private float fallbackDuration = 20f;
        [SerializeField] private bool critical;
        [Tooltip("Counts towards the hidden ending (all logs).")]
        [SerializeField] private bool countsForHiddenEnding = true;
        [SerializeField] private List<SubtitleLine> subtitles = new();

        public string Id => id;
        public string SectorId => sectorId;
        public string TitleKey => titleKey;
        public string SpeakerKey => speakerKey;
        public string DateKey => dateKey;
        public bool Critical => critical;
        public bool CountsForHiddenEnding => countsForHiddenEnding;
        public IReadOnlyList<SubtitleLine> Subtitles => subtitles;

        public AudioClip ClipFor(Core.Language language)
            => language == Core.Language.Bulgarian && bulgarianClip != null ? bulgarianClip : englishClip;

        public float DurationFor(Core.Language language)
        {
            var clip = ClipFor(language);
            return clip != null ? clip.length : fallbackDuration;
        }

#if UNITY_EDITOR
        public void EditorInit(string newId, string sector, string title, string speaker, string date, bool isCritical,
            float duration, List<SubtitleLine> lines)
        {
            id = newId;
            sectorId = sector;
            titleKey = title;
            speakerKey = speaker;
            dateKey = date;
            critical = isCritical;
            fallbackDuration = duration;
            subtitles = lines;
        }
#endif
    }
}
