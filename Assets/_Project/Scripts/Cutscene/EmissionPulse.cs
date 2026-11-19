using UnityEngine;

namespace LastSignal.Cutscene
{
    /// <summary>
    /// Slow pulsing glow (the Murmur under Nereth's ice, waveform bars, warning lights): scales emission and an
    /// optional light with a smooth pulse. Uses a MaterialPropertyBlock, so the shared material is untouched.
    /// </summary>
    public sealed class EmissionPulse : MonoBehaviour
    {
        [SerializeField] private Color color = new(0.3f, 0.75f, 1f);
        [SerializeField] private float minIntensity;
        [SerializeField] private float maxIntensity = 4f;
        [Tooltip("Seconds per pulse (the Murmur: one slow cycle).")]
        [SerializeField] private float period = 4f;
        [SerializeField] private float phase;
        [SerializeField] private Light pulseLight;

        private Renderer _renderer;
        private MaterialPropertyBlock _block;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _block = new MaterialPropertyBlock();
        }

        private void Update()
        {
            float x = (Time.time / Mathf.Max(0.1f, period) + phase) % 1f;
            // quick swell, long fade — reads as something "breathing" rather than blinking
            float k = x < 0.25f ? Mathf.SmoothStep(0f, 1f, x / 0.25f) : Mathf.Pow(1f - (x - 0.25f) / 0.75f, 2f);
            float intensity = Mathf.Lerp(minIntensity, maxIntensity, k);
            if (_renderer == null) _renderer = GetComponent<Renderer>();
            _block ??= new MaterialPropertyBlock();
            if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_block);
                _block.SetColor("_EmissionColor", color * intensity);
                _renderer.SetPropertyBlock(_block);
            }
            if (pulseLight != null) pulseLight.intensity = intensity * 0.5f;
        }

#if UNITY_EDITOR
        public void EditorSet(Color c, float min, float max, float p, float ph, Light l = null)
        {
            color = c; minIntensity = min; maxIntensity = max; period = p; phase = ph; pulseLight = l;
        }
#endif
    }
}
