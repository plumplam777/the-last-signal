using LastSignal.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastSignal.Interaction
{
    /// <summary>
    /// Makes an unpowered sector truly dark: scales the scene's ambient light, fog colour, reflections and reflection probes down and
    /// blends in a "dark" post-processing volume (lower exposure, heavier vignette) while "power.&lt;sector&gt;" is off.
    /// When power comes back everything fades to the scene's authored values. Cheap: it only touches RenderSettings
    /// and one volume weight, and only while a fade is running.
    /// </summary>
    public sealed class PowerDarkness : MonoBehaviour
    {
        [SerializeField] private string sectorId = SectorIds.Docking;
        [Tooltip("Blended in (weight 1) while unpowered.")]
        [SerializeField] private Volume darkVolume;
        [Tooltip("Ambient, fog and reflection brightness while unpowered (1 = unchanged).")]
        [SerializeField, Range(0f, 1f)] private float unpoweredLevel = 0.12f;
        [SerializeField] private float fadeSeconds = 1.6f;

        private string _key;
        private Color _sky, _equator, _ground, _fog;
        private float _reflection;
        private ReflectionProbe[] _probes = new ReflectionProbe[0];
        private float[] _probeIntensity = new float[0];
        private float _level = -1f;   // 0 = dark, 1 = lit
        private float _target;

        private void Awake()
        {
            _key = WorldKeys.Power(sectorId);
            _sky = RenderSettings.ambientSkyColor;
            _equator = RenderSettings.ambientEquatorColor;
            _ground = RenderSettings.ambientGroundColor;
            _fog = RenderSettings.fogColor;
            _reflection = RenderSettings.reflectionIntensity;
            // baked probes were captured with the power on: dim them too, or glossy surfaces show a lit room
            _probes = FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None);
            _probeIntensity = new float[_probes.Length];
            for (int i = 0; i < _probes.Length; i++) _probeIntensity[i] = _probes[i].intensity;
        }

        private void OnEnable()
        {
            GameServices.World.Changed += OnWorldChanged;
            GameServices.World.Reset += Snap;
            Snap();
        }

        private void OnDisable()
        {
            // leave the scene as authored (matters in the editor and when the scene unloads)
            Apply(1f);
            if (!GameServices.IsReady) return;
            GameServices.World.Changed -= OnWorldChanged;
            GameServices.World.Reset -= Snap;
        }

        private void OnWorldChanged(string key)
        {
            if (key == _key) _target = GameServices.World.GetBool(_key) ? 1f : 0f;
        }

        private void Snap()
        {
            _target = GameServices.World.GetBool(_key) ? 1f : 0f;
            _level = _target;
            Apply(_level);
        }

        private void Update()
        {
            if (Mathf.Approximately(_level, _target)) return;
            _level = Mathf.MoveTowards(_level, _target, Time.deltaTime / Mathf.Max(0.05f, fadeSeconds));
            Apply(_level);
        }

        private void Apply(float lit)
        {
            float k = Mathf.Lerp(unpoweredLevel, 1f, lit);
            RenderSettings.ambientSkyColor = _sky * k;
            RenderSettings.ambientEquatorColor = _equator * k;
            RenderSettings.ambientGroundColor = _ground * k;
            RenderSettings.fogColor = _fog * k;
            RenderSettings.reflectionIntensity = _reflection * k;
            for (int i = 0; i < _probes.Length; i++)
                if (_probes[i] != null) _probes[i].intensity = _probeIntensity[i] * k;
            if (darkVolume != null) darkVolume.weight = 1f - lit;
        }

#if UNITY_EDITOR
        public void EditorConfigure(string sector, Volume dark, float level)
        {
            sectorId = sector;
            darkVolume = dark;
            unpoweredLevel = level;
        }
#endif
    }
}
