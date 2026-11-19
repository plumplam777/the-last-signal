using LastSignal.Core;
using LastSignal.Interaction;
using UnityEngine;

namespace LastSignal.Player
{
    /// <summary>Player-side sounds: footsteps, flashlight clicks, denied-interaction buzz.</summary>
    public sealed class PlayerAudio : MonoBehaviour
    {
        [SerializeField] private PlayerController controller;
        [SerializeField] private FlashlightController flashlight;
        [SerializeField] private PlayerInteractor interactor;

        private int _step;

        private void OnEnable()
        {
            if (controller != null) controller.Footstep += OnFootstep;
            if (flashlight != null)
            {
                flashlight.Toggled += OnFlashlight;
                flashlight.Refused += OnRefused;
            }
            if (interactor != null) interactor.Denied += OnDenied;
        }

        private void OnDisable()
        {
            if (controller != null) controller.Footstep -= OnFootstep;
            if (flashlight != null)
            {
                flashlight.Toggled -= OnFlashlight;
                flashlight.Refused -= OnRefused;
            }
            if (interactor != null) interactor.Denied -= OnDenied;
        }

        private void OnFootstep(bool sprinting)
        {
            _step = (_step + 1 + Random.Range(0, 3)) % 4;
            GameServices.Audio.SfxAt($"sfx_footstep_{_step + 1}", transform.position, sprinting ? 0.75f : 0.55f, 0.1f);
        }

        private void OnFlashlight(bool on) => GameServices.Audio.Sfx("sfx_flashlight", 0.8f);
        private void OnRefused() => GameServices.Audio.Sfx("sfx_error", 0.5f);
        private void OnDenied(string reason) => GameServices.Audio.Sfx("sfx_door_locked", 0.5f);

#if UNITY_EDITOR
        public void EditorBind(PlayerController c, FlashlightController f, PlayerInteractor i)
        {
            controller = c; flashlight = f; interactor = i;
        }
#endif
    }
}
