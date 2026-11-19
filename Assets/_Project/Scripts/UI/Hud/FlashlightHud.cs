using LastSignal.Core;
using LastSignal.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.UI
{
    /// <summary>
    /// Bottom-left flashlight panel, shown once the flashlight is owned: "[F] Flashlight · ON/OFF" and a battery
    /// bar with percentage (cyan → amber → red as it drains). Pulses when the battery is too low to switch on.
    /// </summary>
    public sealed class FlashlightHud : MonoBehaviour
    {
        [SerializeField] private FlashlightController flashlight;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image batteryFill;
        [SerializeField] private TMP_Text percent;

        private static readonly Color Full = new(0.37f, 0.9f, 1f);
        private static readonly Color Mid = new(1f, 0.7f, 0.42f);
        private static readonly Color Low = new(0.88f, 0.27f, 0.23f);

        private float _refusedPulse;

        private void OnEnable()
        {
            if (flashlight != null) flashlight.Refused += OnRefused;
            GameServices.Localization.LanguageChanged += Refresh;
        }

        private void OnDisable()
        {
            if (flashlight != null) flashlight.Refused -= OnRefused;
            if (GameServices.IsReady) GameServices.Localization.LanguageChanged -= Refresh;
        }

        private void OnRefused() => _refusedPulse = 1.2f;

        private void Update() => Refresh();

        private void Refresh()
        {
            if (flashlight == null) return;
            bool owned = flashlight.IsOwned;
            group.alpha = Mathf.MoveTowards(group.alpha, owned ? 1f : 0f, Time.unscaledDeltaTime * 4f);
            if (!owned) return;

            float c = flashlight.Charge;
            var state = flashlight.IsOn ? Loc.Get("hud.flashlight.on") : flashlight.IsDepleted ? Loc.Get("hud.flashlight.empty") : Loc.Get("hud.flashlight.off");
            label.text = $"<color=#5FE6FF>[F]</color>  {Loc.Get("item.flashlight.name")}  <color=#8A8F93>·</color>  {state}";
            batteryFill.fillAmount = c;
            var color = c > 0.5f ? Color.Lerp(Mid, Full, (c - 0.5f) * 2f) : Color.Lerp(Low, Mid, c * 2f);
            if (_refusedPulse > 0f)
            {
                _refusedPulse -= Time.unscaledDeltaTime;
                color = Color.Lerp(color, Low, Mathf.Abs(Mathf.Sin(Time.unscaledTime * 12f)));
            }
            batteryFill.color = color;
            percent.text = $"{Mathf.RoundToInt(c * 100f)}%";
        }

#if UNITY_EDITOR
        public void EditorBind(FlashlightController f, CanvasGroup g, TMP_Text l, Image fill, TMP_Text p)
        {
            flashlight = f; group = g; label = l; batteryFill = fill; percent = p;
        }
#endif
    }
}
