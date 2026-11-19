using System;
using LastSignal.Core;
using UnityEngine;

namespace LastSignal.Player
{
    /// <summary>
    /// Toggles the player's spot light with F once the flashlight has been picked up (WorldState flag
    /// <see cref="OwnedFlag"/>). Has a battery: drains while on, slowly recharges while off (the cell
    /// recharges from the suit). Low battery dims and flickers the beam; empty switches it off until it
    /// has recovered enough charge. Charge is saved in WorldState as a percentage.
    /// </summary>
    public sealed class FlashlightController : MonoBehaviour
    {
        public static readonly string OwnedFlag = WorldKeys.Story("has_flashlight");
        public const string BatteryKey = "flashlight.battery";

        [SerializeField] private Light spotLight;
        [SerializeField] private float baseIntensity = 6f;
        [SerializeField, Range(0f, 0.3f)] private float flickerAmount = 0.05f;
        [SerializeField] private float flickerSpeed = 7f;

        [Header("Battery")]
        [Tooltip("Seconds a full battery lasts while on.")]
        [SerializeField] private float fullDrainSeconds = 240f;
        [Tooltip("Seconds to recharge from empty to full while off.")]
        [SerializeField] private float fullRechargeSeconds = 120f;
        [SerializeField, Range(0f, 1f)] private float lowThreshold = 0.2f;
        [Tooltip("After running empty the light can only be switched on again above this charge.")]
        [SerializeField, Range(0f, 1f)] private float restartThreshold = 0.1f;

        private float _noiseSeed;
        private float _charge = 1f;

        public bool IsOn { get; private set; }
        public bool IsOwned => GameServices.World.GetBool(OwnedFlag);
        /// <summary>0..1</summary>
        public float Charge => _charge;
        public bool IsLow => _charge <= lowThreshold;
        public bool IsDepleted { get; private set; }

        public event Action<bool> Toggled;
        /// <summary>Raised when F is pressed but the battery is too low.</summary>
        public event Action Refused;

        private void Awake()
        {
            _noiseSeed = UnityEngine.Random.value * 100f;
            if (spotLight == null) Log.Error(nameof(FlashlightController), "Spot light is not assigned.", this);
            else spotLight.enabled = false;
        }

        private void Start()
        {
            var world = GameServices.World;
            _charge = world.HasKey(BatteryKey) ? Mathf.Clamp01(world.GetInt(BatteryKey) / 100f) : 1f;
        }

        private void Update()
        {
            if (GameServices.State.IsGameplayActive
                && GameServices.Input.Controls.Player.Flashlight.WasPressedThisFrame()
                && IsOwned)
            {
                if (IsOn) SetOn(false);
                else if (!IsDepleted || _charge >= restartThreshold) SetOn(true);
                else Refused?.Invoke();
            }

            UpdateBattery();

            if (IsOn && spotLight != null)
            {
                float flicker = IsLow ? Mathf.Lerp(0.45f, flickerAmount, _charge / lowThreshold) : flickerAmount;
                float n = Mathf.PerlinNoise(_noiseSeed, Time.time * (IsLow ? flickerSpeed * 2.5f : flickerSpeed));
                float dim = IsLow ? Mathf.Lerp(0.45f, 1f, _charge / lowThreshold) : 1f;
                spotLight.intensity = baseIntensity * dim * (1f - flicker + n * flicker * 2f);
            }
        }

        private void UpdateBattery()
        {
            if (!IsOwned || GameServices.State.Current == GameState.Paused) return;
            float before = _charge;
            if (IsOn) _charge = Mathf.Max(0f, _charge - Time.deltaTime / Mathf.Max(1f, fullDrainSeconds));
            else _charge = Mathf.Min(1f, _charge + Time.deltaTime / Mathf.Max(1f, fullRechargeSeconds));

            if (IsOn && _charge <= 0f)
            {
                IsDepleted = true;
                SetOn(false);
            }
            if (IsDepleted && _charge >= restartThreshold) IsDepleted = false;

            int pct = Mathf.RoundToInt(_charge * 100f);
            if (pct != Mathf.RoundToInt(before * 100f)) GameServices.World.SetInt(BatteryKey, pct);
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
