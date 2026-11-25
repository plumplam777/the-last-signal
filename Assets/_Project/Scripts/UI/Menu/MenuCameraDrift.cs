using UnityEngine;

namespace LastSignal.UI
{
    /// <summary>Start-screen camera: a slow, barely noticeable float (position sway + look drift), like a ship in orbit.</summary>
    public sealed class MenuCameraDrift : MonoBehaviour
    {
        [SerializeField] private Vector3 sway = new(6f, 3f, 0f);
        [SerializeField] private Vector3 lookDrift = new(0.6f, 1.2f, 0.4f);
        [SerializeField] private float period = 60f;

        private Vector3 _basePos;
        private Quaternion _baseRot;

        private void Awake()
        {
            _basePos = transform.localPosition;
            _baseRot = transform.localRotation;
        }

        private void Update()
        {
            float t = Time.unscaledTime * Mathf.PI * 2f / period;
            transform.localPosition = _basePos + new Vector3(Mathf.Sin(t) * sway.x, Mathf.Sin(t * 0.7f + 1f) * sway.y, 0f);
            transform.localRotation = _baseRot * Quaternion.Euler(
                Mathf.Sin(t * 0.8f + 2f) * lookDrift.x, Mathf.Sin(t * 0.6f) * lookDrift.y, Mathf.Sin(t * 0.5f + 0.5f) * lookDrift.z);
        }
    }
}
