using LastSignal.Core;
using UnityEngine;

namespace LastSignal.Interaction
{
    /// <summary>Spins (with spin-up/down) while a WorldState flag is true (or always, with no flag) — generator governors, fans, reactor rings, the lab armillary.</summary>
    public sealed class FlagSpinner : MonoBehaviour
    {
        [SerializeField] private string flag;
        [SerializeField] private Vector3 axis = Vector3.up;
        [SerializeField] private float degreesPerSecond = 220f;
        [SerializeField] private float spinUpSeconds = 2.5f;

        private float _speed;

        private void Update()
        {
            bool on = string.IsNullOrEmpty(flag) || GameServices.World.GetBool(flag); // no flag = always turning
            float target = on ? degreesPerSecond : 0f;
            _speed = Mathf.MoveTowards(_speed, target, degreesPerSecond / Mathf.Max(0.1f, spinUpSeconds) * Time.deltaTime);
            if (_speed > 0.01f) transform.Rotate(axis, _speed * Time.deltaTime, Space.Self);
        }

#if UNITY_EDITOR
        public void EditorSet(string f, Vector3 a, float speed)
        {
            flag = f; axis = a; degreesPerSecond = speed;
        }
#endif
    }
}
