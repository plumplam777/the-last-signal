using System.Collections.Generic;
using LastSignal.Core;
using UnityEngine;

namespace LastSignal.Audio
{
    /// <summary>
    /// Central playback for sound effects, voices and ambience. Clips are loaded from Resources by name:
    /// Sfx/&lt;name&gt;, Ambience/&lt;name&gt;, Voice/&lt;lang&gt;/&lt;localization key with '.' → '_'&gt;.
    /// Missing clips are skipped silently (logged once), so the game always works without audio.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        private const int PoolSize = 8;

        private readonly Dictionary<string, AudioClip> _cache = new();
        private readonly HashSet<string> _missing = new();
        private readonly List<AudioSource> _pool = new();
        private AudioSource _ui;
        private AudioSource _voice;
        private readonly List<AmbienceChannel> _ambience = new();
        private int _poolIndex;

        public float SfxVolume { get; set; } = 0.9f;
        public float VoiceVolume { get; set; } = 1f;
        public float AmbienceVolume { get; set; } = 0.8f;

        public bool IsVoicePlaying => _voice != null && _voice.isPlaying;

        private sealed class AmbienceChannel
        {
            public AudioSource Source;
            public float Target;
        }

        private void Awake()
        {
            _ui = gameObject.AddComponent<AudioSource>();
            _ui.playOnAwake = false;
            _voice = gameObject.AddComponent<AudioSource>();
            _voice.playOnAwake = false;
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject($"Sfx3D_{i}");
                go.transform.SetParent(transform, false);
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 1f;
                s.rolloffMode = AudioRolloffMode.Linear;
                s.minDistance = 1.5f;
                s.maxDistance = 22f;
                _pool.Add(s);
            }
        }

        private void Start()
        {
            // global hooks: pickups and objectives are heard wherever they happen
            GameServices.Inventory.Added += (_, _) => Sfx("sfx_pickup");
            GameServices.Quests.ObjectiveCompleted += (_, _) => Sfx("sfx_objective", 0.8f);
            GameServices.Puzzles.PuzzleFailed += (_, _) => Sfx("sfx_error", 0.8f);
            GameServices.AudioLogs.Started += _ => Sfx("sfx_log_start", 0.7f);
            GameServices.Dialogue.Started += _ => Sfx("sfx_cairn", 0.6f);
            GameServices.World.Changed += key =>
            {
                if (key.StartsWith("power.") && GameServices.World.GetBool(key)) Sfx("sfx_power_up", 0.8f);
            };
            GameServices.Scenes.LoadStarted += _ =>
            {
                StopVoice();
                SetAmbience(); // sectors start their own ambience
            };
        }

        // ------------------------------------------------------------------ sfx

        public void Sfx(string name, float volume = 1f)
        {
            var clip = Load($"Sfx/{name}");
            if (clip != null) _ui.PlayOneShot(clip, volume * SfxVolume);
        }

        public void SfxAt(string name, Vector3 position, float volume = 1f, float pitchJitter = 0.06f)
        {
            var clip = Load($"Sfx/{name}");
            if (clip == null) return;
            var s = _pool[_poolIndex++ % _pool.Count];
            s.transform.position = position;
            s.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            s.PlayOneShot(clip, volume * SfxVolume);
        }

        // ------------------------------------------------------------------ voice

        /// <summary>Plays the voice clip for a localization key in the current language; returns its length (0 if none).</summary>
        public float PlayVoice(string key)
        {
            if (string.IsNullOrEmpty(key)) return 0f;
            var lang = LocalizationService.Code(GameServices.Localization.Current);
            var clip = Load($"Voice/{lang}/{key.Replace('.', '_')}");
            if (clip == null) return 0f;
            _voice.Stop();
            _voice.clip = clip;
            _voice.volume = VoiceVolume;
            _voice.Play();
            return clip.length;
        }

        public void StopVoice()
        {
            if (_voice != null) _voice.Stop();
        }

        public void PauseVoice(bool paused)
        {
            if (_voice == null) return;
            if (paused) _voice.Pause();
            else _voice.UnPause();
        }

        // ------------------------------------------------------------------ ambience

        /// <summary>Cross-fades to the given loops (name, volume). Call with nothing to fade everything out.</summary>
        public void SetAmbience(params (string name, float volume)[] loops)
        {
            foreach (var ch in _ambience) ch.Target = 0f;
            foreach (var (name, volume) in loops)
            {
                var clip = Load($"Ambience/{name}");
                if (clip == null) continue;
                var ch = _ambience.Find(c => c.Source.clip == clip);
                if (ch == null)
                {
                    var src = gameObject.AddComponent<AudioSource>();
                    src.clip = clip;
                    src.loop = true;
                    src.volume = 0f;
                    src.playOnAwake = false;
                    src.Play();
                    ch = new AmbienceChannel { Source = src };
                    _ambience.Add(ch);
                }
                ch.Target = volume;
            }
        }

        private bool _wasPaused;

        private void Update()
        {
            bool paused = GameServices.State.Current == GameState.Paused;
            if (paused != _wasPaused)
            {
                _wasPaused = paused;
                PauseVoice(paused);
            }
            float dt = Time.unscaledDeltaTime;
            for (int i = _ambience.Count - 1; i >= 0; i--)
            {
                var ch = _ambience[i];
                float target = ch.Target * AmbienceVolume * (paused ? 0.4f : 1f);
                ch.Source.volume = Mathf.MoveTowards(ch.Source.volume, target, dt * 0.6f);
                if (ch.Target <= 0f && ch.Source.volume <= 0.001f)
                {
                    Destroy(ch.Source);
                    _ambience.RemoveAt(i);
                }
            }
        }

        private AudioClip Load(string path)
        {
            if (_cache.TryGetValue(path, out var clip)) return clip;
            clip = Resources.Load<AudioClip>(path);
            if (clip == null && _missing.Add(path)) Log.Info(nameof(AudioManager), $"No clip at Resources/{path} (silent).");
            _cache[path] = clip;
            return clip;
        }
    }
}
