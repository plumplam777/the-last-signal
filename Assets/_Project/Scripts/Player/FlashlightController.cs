using System;
using LastSignal.Core;
using UnityEngine;

namespace LastSignal.Player
{
    /// <summary>
    /// Toggles the player's spot light with F once the flashlight has been picked up
    /// (WorldState flag <see cref="OwnedFlag"/>). Adds a faint organic flicker.
    /// </summary>
    public sealed class FlashlightController : MonoBehaviour
    {
        public static readonly string OwnedFlag = WorldKeys.Story("has_flashlight");

        [SerializeField] private Light spotLight;
        [SerializeField] private float baseIntensity = 6f;
        [SerializeField, Range(0f, 0.3f)] private float flickerAmount = 0.05f;
        [SerializeField] private float flickerSpeed = 7f;

        private float _noiseSeed;

        public bool IsOn { get; private set; }
        public bool IsOwned => GameServices.World.GetBool(OwnedFlag);

        public event Action<bool> Toggled;

        private void Awake()
        {
            _noiseSeed = UnityEngine.Random.value * 100f;
            if (spotLight == null) Log.Error(nameof(FlashlightController), "Spot light is not assigned.", this);
            else spotLight.enabled = false;
        }

        private void Update()
        {
            if (GameServices.State.IsGameplayActive
                && GameServices.Input.Controls.Player.Flashlight.WasPressedThisFrame())
            {
                if (IsOwned) SetOn(!IsOn);
            }

            if (IsOn && spotLight != null)
            {
                float n = Mathf.PerlinNoise(_noiseSeed, Time.time * flickerSpeed);
                spotLight.intensity = baseIntensity * (1f - flickerAmount + n * flickerAmount * 2f);
            }
        }

        public void SetOn(bool on)
        {
            if (spotLight == null || on == IsOn) return;
            IsOn = on;
            spotLight.enabled = on;
            Toggled?.Invoke(on);
        }
    }
}
