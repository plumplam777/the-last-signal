using System;
using System.Collections.Generic;
using System.Linq;
using LastSignal.Core;
using UnityEngine;

namespace LastSignal.Audio
{
    /// <summary>
    /// Collects and plays audio logs. Collected state lives in WorldState ("log.&lt;id&gt;.collected"), played
    /// state in "log.&lt;id&gt;.played". Plays the clip for the current language when present, otherwise runs
    /// the subtitle timeline on its own so every log is readable without audio.
    /// </summary>
    public sealed class AudioLogManager : MonoBehaviour
    {
        private AudioSource _source;
        private float _startTime;
        private float _duration;
        private int _lineIndex = -1;
        private readonly List<AudioLogDefinition> _all = new();

        public AudioLogDefinition Current { get; private set; }
        public bool IsPlaying => Current != null;
        public float Elapsed => IsPlaying ? Time.unscaledTime - _startTime : 0f;
        public float Duration => _duration;
        /// <summary>Localization key of the subtitle shown right now (null between lines).</summary>
        public string CurrentLineKey { get; private set; }

        public event Action<AudioLogDefinition> Collected;
        public event Action<AudioLogDefinition> Started;
        public event Action<AudioLogDefinition> Finished;
        public event Action<string> SubtitleChanged;

        public IReadOnlyList<AudioLogDefinition> AllLogs => _all;

        public static string PlayedKey(string logId) => $"log.{logId}.played";

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f; // played through the player's comms, not positional
        }

        public void RegisterCatalog(IEnumerable<AudioLogDefinition> logs)
        {
            foreach (var log in logs)
                if (log != null && !_all.Contains(log)) _all.Add(log);
        }

        public bool IsCollected(AudioLogDefinition log) => GameServices.World.GetBool(WorldKeys.LogCollected(log.Id));

        public int CollectedCount => _all.Count(IsCollected);
        public int TotalCount => _all.Count;
        public int HiddenEndingTotal => _all.Count(l => l.CountsForHiddenEnding);
        public int HiddenEndingCollected => _all.Count(l => l.CountsForHiddenEnding && IsCollected(l));

        public IEnumerable<AudioLogDefinition> CollectedLogs => _all.Where(IsCollected);

        public void Collect(AudioLogDefinition log, bool playNow = true)
        {
            if (log == null) return;
            if (!_all.Contains(log)) _all.Add(log);
            if (!IsCollected(log))
            {
                GameServices.World.SetBool(WorldKeys.LogCollected(log.Id), true);
                Collected?.Invoke(log);
            }
            if (playNow) Play(log);
        }

        public void Play(AudioLogDefinition log)
        {
            if (log == null) return;
            Stop();
            Current = log;
            var language = GameServices.Localization.Current;
            var clip = log.ClipFor(language);
            _duration = log.DurationFor(language);
            _startTime = Time.unscaledTime;
            _lineIndex = -1;
            if (clip != null)
            {
                _source.clip = clip;
                _source.Play();
            }
            Started?.Invoke(log);
        }

        public void Stop()
        {
            if (Current == null) return;
            var finished = Current;
            Current = null;
            _source.Stop();
            SetLine(null);
            GameServices.World.SetBool(PlayedKey(finished.Id), true);
            Finished?.Invoke(finished);
        }

        private void Update()
        {
            if (Current == null) return;
            // Pausing the game pauses the recording too.
            bool paused = GameServices.State.Current == GameState.Paused;
            if (paused)
            {
                if (_source.isPlaying) _source.Pause();
                _startTime += Time.unscaledDeltaTime;
                return;
            }
            if (_source.clip != null && !_source.isPlaying && Elapsed < _duration - 0.05f) _source.UnPause();

            float t = Elapsed;
            var lines = Current.Subtitles;
            int index = -1;
            for (int i = 0; i < lines.Count; i++)
                if (t >= lines[i].start) index = i;
            if (index != _lineIndex)
            {
                _lineIndex = index;
                SetLine(index >= 0 ? lines[index].textKey : null);
            }
            if (t >= _duration) Stop();
        }

        private void SetLine(string key)
        {
            CurrentLineKey = key;
            SubtitleChanged?.Invoke(key);
        }
    }
}
