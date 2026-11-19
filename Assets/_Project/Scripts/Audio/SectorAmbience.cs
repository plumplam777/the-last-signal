using System;
using System.Collections.Generic;
using System.Linq;
using LastSignal.Core;
using UnityEngine;

namespace LastSignal.Audio
{
    [Serializable]
    public sealed class AmbienceLoop
    {
        public string clip;
        [Range(0f, 1f)] public float volume = 0.5f;
        [Tooltip("Plays only while this WorldState flag is true (empty = always).")]
        public string whileFlag;
    }

    /// <summary>The background loops of a scene; re-evaluated when the world changes (power on → ventilation starts).</summary>
    public sealed class SectorAmbience : MonoBehaviour
    {
        [SerializeField] private List<AmbienceLoop> loops = new();

        private void OnEnable()
        {
            GameServices.World.Changed += OnWorldChanged;
            GameServices.World.Reset += Apply;
        }

        private void OnDisable()
        {
            if (!GameServices.IsReady) return;
            GameServices.World.Changed -= OnWorldChanged;
            GameServices.World.Reset -= Apply;
        }

        private void Start() => Apply();

        private void OnWorldChanged(string key)
        {
            if (loops.Any(l => l.whileFlag == key)) Apply();
        }

        private void Apply()
        {
            var active = loops.Where(l => string.IsNullOrEmpty(l.whileFlag) || GameServices.World.GetBool(l.whileFlag))
                              .Select(l => (l.clip, l.volume)).ToArray();
            GameServices.Audio.SetAmbience(active);
        }

#if UNITY_EDITOR
        public void EditorSet(List<AmbienceLoop> newLoops) => loops = newLoops;
#endif
    }
}
