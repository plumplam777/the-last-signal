using TMPro;
using UnityEngine;

namespace LastSignal.Player
{
    /// <summary>
    /// A message painted by the crew in fluorescent paint: invisible in the dark, it glows only while the player's
    /// flashlight beam is on it (strongest in the centre of the beam, fading at the edge and with distance), and
    /// fades out again a moment after the beam moves away. No shader: it only changes the text's alpha.
    /// </summary>
    [RequireComponent(typeof(TextMeshPro))]
    public sealed class BeamRevealText : MonoBehaviour
    {
        [Tooltip("Fraction of the flashlight range at which the paint still reacts.")]
        [SerializeField, Range(0.2f, 1f)] private float reach = 0.75f;
        [SerializeField] private float fadeInSpeed = 2.5f;
        [SerializeField] private float fadeOutSpeed = 0.8f;

        private TextMeshPro _text;
        private Color _color;
        private float _alpha;

        /// <summary>0..1, how visible the message is right now.</summary>
        public float Visibility => _alpha;

        private void Awake()
        {
            _text = GetComponent<TextMeshPro>();
            _color = _text.color;
            Set(0f);
        }

        private void Update()
        {
            float target = Exposure();
            float speed = target > _alpha ? fadeInSpeed : fadeOutSpeed;
            float next = Mathf.MoveTowards(_alpha, target, Time.deltaTime * speed);
            if (!Mathf.Approximately(next, _alpha)) Set(next);
        }

        private float Exposure()
        {
            var flashlight = FlashlightController.Current;
            if (flashlight == null || !flashlight.IsOn || flashlight.Beam == null) return 0f;
            var beam = flashlight.Beam;
            var toText = transform.position - beam.transform.position;
            float range = beam.range * reach;
            float distance = toText.magnitude;
            if (distance > range) return 0f;
            float half = beam.spotAngle * 0.5f;
            float angle = Vector3.Angle(beam.transform.forward, toText);
            if (angle > half) return 0f;
            float centre = 1f - Mathf.Clamp01((angle - beam.innerSpotAngle * 0.5f) / Mathf.Max(1f, half - beam.innerSpotAngle * 0.5f));
            float falloff = 1f - Mathf.Clamp01((distance - range * 0.5f) / (range * 0.5f));
            return Mathf.Clamp01(centre * falloff * Mathf.Clamp01(beam.intensity / 6f));
        }

        private void Set(float alpha)
        {
            _alpha = alpha;
            _text.color = new Color(_color.r, _color.g, _color.b, _color.a * alpha);
        }
    }
}
