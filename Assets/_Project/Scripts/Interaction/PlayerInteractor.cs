using System;
using LastSignal.Core;
using UnityEngine;

namespace LastSignal.Interaction
{
    /// <summary>
    /// Ray-casts from the camera to find the <see cref="IInteractable"/> in front of the player,
    /// raises focus events for the prompt UI and calls Interact on E. Contains no object-specific logic.
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private float range = 2.6f;
        [SerializeField] private float probeRadius = 0.06f;
        [SerializeField] private LayerMask mask = Physics.DefaultRaycastLayers; // excludes "Ignore Raycast" (the player)

        private IInteractable _current;
        private bool _currentAvailable;
        private string _currentReason;

        public IInteractable Current => _current;

        /// <summary>(target or null, available, localized prompt, localized reason or null)</summary>
        public event Action<IInteractable, bool, string, string> FocusChanged;
        /// <summary>Interaction refused; argument is the localized reason (may be null).</summary>
        public event Action<string> Denied;
        public event Action<IInteractable> Interacted;

        private void Awake()
        {
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
        }

        private void Update()
        {
            if (!GameServices.State.IsGameplayActive)
            {
                SetFocus(null, false, null);
                return;
            }

            var target = Probe();
            bool available = false;
            string reason = null;
            if (target != null) available = target.CanInteract(this, out reason);
            if (target != _current || available != _currentAvailable || reason != _currentReason)
                SetFocus(target, available, reason);

            if (_current != null && GameServices.Input.Controls.Interaction.Interact.WasPressedThisFrame())
            {
                if (_currentAvailable)
                {
                    _current.Interact(this);
                    Interacted?.Invoke(_current);
                    _current = null; // force a refresh next frame (state probably changed)
                }
                else
                {
                    Denied?.Invoke(string.IsNullOrEmpty(_currentReason) ? null : Loc.Get(_currentReason));
                }
            }
        }

        /// <summary>Re-raises the current focus (e.g. after a language change).</summary>
        public void RefreshFocus() => SetFocus(_current, _currentAvailable, _currentReason, force: true);

        private IInteractable Probe()
        {
            if (viewCamera == null) return null;
            var ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
            if (!Physics.SphereCast(ray, probeRadius, out var hit, range, mask, QueryTriggerInteraction.Ignore))
                return null;
            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            return interactable != null && interactable.IsInteractionEnabled ? interactable : null;
        }

        private void SetFocus(IInteractable target, bool available, string reasonKey, bool force = false)
        {
            bool changed = target != _current || available != _currentAvailable || reasonKey != _currentReason;
            _current = target;
            _currentAvailable = available;
            _currentReason = reasonKey;
            if (!changed && !force) return;
            string prompt = target?.GetPromptText();
            string reason = string.IsNullOrEmpty(reasonKey) ? null : Loc.Get(reasonKey);
            FocusChanged?.Invoke(target, available, prompt, reason);
        }
    }
}
