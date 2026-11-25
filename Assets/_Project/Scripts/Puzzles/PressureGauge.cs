using TMPro;
using UnityEngine;

namespace LastSignal.Puzzles
{
    /// <summary>
    /// A loop pressure gauge above one reactor valve. The needle swings to the new pressure with a short overshoot
    /// wobble (so turning the wheel visibly does something), and the readout turns amber while the loop resonates
    /// with another one. Update only runs while the needle is still moving.
    /// </summary>
    public sealed class PressureGauge : MonoBehaviour
    {
        [SerializeField] private Transform needle;
        [SerializeField] private TMP_Text readout;
        [SerializeField] private float minAngle = 70f, maxAngle = -70f; // LOW (left) → HIGH (right)

        public static readonly Color Calm = new(0.55f, 0.95f, 0.85f);
        public static readonly Color Resonant = new(1f, 0.68f, 0.3f);

        private float _angle, _velocity, _target;
        private bool _initialised;

        /// <summary>The angle the needle is heading to (degrees around local Z).</summary>
        public float TargetAngle => _target;
        public string Readout => readout != null ? readout.text : null;
        public Color ReadoutColour => readout != null ? readout.color : Color.clear;

        public void Show(int setting, bool resonating, string text)
        {
            _target = Mathf.Lerp(minAngle, maxAngle, setting / 2f);
            if (!_initialised)
            {
                _initialised = true;
                _angle = _target;
                Apply();
            }
            if (readout != null)
            {
                readout.text = text;
                readout.color = resonating ? Resonant : Calm;
            }
            enabled = true;
        }

        private void Update()
        {
            // under-damped spring: a quick swing past the mark and back
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            _velocity += ((_target - _angle) * 160f - _velocity * 14f) * dt;
            _angle += _velocity * dt;
            Apply();
            if (Mathf.Abs(_target - _angle) < 0.3f && Mathf.Abs(_velocity) < 1f)
            {
                _angle = _target;
                Apply();
                enabled = false;
            }
        }

        private void Apply()
        {
            if (needle != null) needle.localRotation = Quaternion.Euler(0, 0, _angle);
        }

#if UNITY_EDITOR
        public void EditorSet(Transform n, TMP_Text r)
        {
            needle = n;
            readout = r;
        }
#endif
    }
}
