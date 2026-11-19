using UnityEngine;

namespace LastSignal.UI
{
    /// <summary>Menu backdrop: the camera drifts slowly around a target (Lantern Station over Nereth).</summary>
    public sealed class SlowOrbitCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 center;
        [SerializeField] private float radius = 110f;
        [SerializeField] private float height = 18f;
        [SerializeField] private float degreesPerSecond = 0.8f;
        [SerializeField] private float angle = 235f;

        private void Update()
        {
            angle += degreesPerSecond * Time.unscaledDeltaTime;
            float r = angle * Mathf.Deg2Rad;
            transform.position = center + new Vector3(Mathf.Cos(r) * radius, height, Mathf.Sin(r) * radius);
            if (target != null) transform.LookAt(target.position + Vector3.down * 12f);
        }

#if UNITY_EDITOR
        public void EditorSet(Transform t, Vector3 c) { target = t; center = c; }
#endif
    }
}
