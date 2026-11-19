using LastSignal.Core;
using UnityEngine;

namespace LastSignal.Interaction
{
    /// <summary>
    /// One-shot lever / breaker switch. Swings its handle and applies the base effects (e.g. sector power).
    /// Requirements (e.g. "fuse installed") come from the base class.
    /// </summary>
    public sealed class LeverInteractable : Interactable
    {
        [SerializeField] private Transform handle;
        [SerializeField] private Vector3 offEuler = new(-40f, 0f, 0f);
        [SerializeField] private Vector3 onEuler = new(40f, 0f, 0f);
        [SerializeField] private float swingSpeed = 6f;

        private float _t; // 0 off .. 1 on
        private bool _on;

        protected override void Start()
        {
            oneShot = true;
            base.Start();
            Apply();
        }

        protected override void OnInteract(PlayerInteractor interactor)
        {
            _on = true;
            GameServices.Audio.SfxAt("sfx_breaker", transform.position);
        }

        protected override void RestoreUsedState()
        {
            _on = true;
            _t = 1f;
            Apply();
        }

        private void Update()
        {
            float target = _on ? 1f : 0f;
            if (Mathf.Approximately(_t, target)) return;
            _t = Mathf.MoveTowards(_t, target, swingSpeed * Time.deltaTime);
            Apply();
        }

        private void Apply()
        {
            if (handle != null) handle.localRotation = Quaternion.Slerp(Quaternion.Euler(offEuler), Quaternion.Euler(onEuler), _t);
        }

#if UNITY_EDITOR
        public void EditorSetHandle(Transform h) => handle = h;
#endif
    }
}
