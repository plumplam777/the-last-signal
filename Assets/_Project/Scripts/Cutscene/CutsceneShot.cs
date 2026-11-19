using System;
using UnityEngine;

namespace LastSignal.Cutscene
{
    /// <summary>One shot of a scripted cutscene (StoryBible §10 shot list): a camera move plus text overlays.</summary>
    [Serializable]
    public sealed class CutsceneShot
    {
        public string name;
        public float duration = 5f;
        [Header("Camera (world space): eased from → to")]
        public Vector3 fromPosition;
        public Vector3 fromEuler;
        public Vector3 toPosition;
        public Vector3 toEuler;
        public float fieldOfView = 55f;
        [Header("Overlays (localization keys, empty = none)")]
        public string subtitleKey;
        [Tooltip("Large centred card text, e.g. 'Three days later'.")]
        public string cardKey;
        [Tooltip("Text shown on the cockpit display during this shot.")]
        public string displayKey;
        [Header("Fades (seconds)")]
        public float fadeIn;
        public float fadeOut;
        [Tooltip("Keep the screen black for the whole shot (cards on black).")]
        public bool black;
        [Tooltip("Sound effect played when the shot starts (Resources/Sfx name).")]
        public string sfx;
        [Tooltip("Objects switched on when the shot starts (e.g. docking lights).")]
        public GameObject[] activate = Array.Empty<GameObject>();
    }
}
