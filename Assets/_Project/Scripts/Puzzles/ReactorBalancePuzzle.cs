using System.Collections;
using LastSignal.Core;
using TMPro;
using UnityEngine;

namespace LastSignal.Puzzles
{
    /// <summary>
    /// The reactor catwalk console: valves (<see cref="ValveInteractable"/>) change a <see cref="ReactorBalance"/>;
    /// gauges show output and temperature. Stable → solved (powers the Command lift). Overshoot → alarm and a slow
    /// automatic reset to Mid, during which the valves do not respond.
    /// </summary>
    public sealed class ReactorBalancePuzzle : Puzzle
    {
        [SerializeField] private TMP_Text outputText;
        [SerializeField] private TMP_Text temperatureText;
        [SerializeField] private Transform outputNeedle;
        [SerializeField] private Transform temperatureNeedle;
        [SerializeField] private Light coreLight;
        [SerializeField] private PressureGauge[] loopGauges = new PressureGauge[ReactorBalance.ValveCount];
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private float resetSeconds = 4f;

        private readonly ReactorBalance _balance = new();
        private bool _resetting;

        public ReactorBalance Balance => _balance;
        public bool IsResetting => _resetting;

        public event System.Action Changed;

        public PressureGauge LoopGauge(int valve) => loopGauges != null && valve < loopGauges.Length ? loopGauges[valve] : null;
        public string StatusLine => statusText != null ? statusText.text : null;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (GameServices.IsReady) GameServices.Localization.LanguageChanged += Refresh;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (GameServices.IsReady) GameServices.Localization.LanguageChanged -= Refresh;
        }

        protected override void Start()
        {
            base.Start();
            Refresh();
        }

        public void CycleValve(int valve)
        {
            if (IsSolved || _resetting) return;
            _balance.Cycle(valve);
            Refresh();
            Changed?.Invoke();
            if (_balance.IsStable)
            {
                MarkSolved();
                return;
            }
            if (_balance.IsOvershoot)
            {
                GameServices.Audio.SfxAt("sfx_alarm", transform.position, 1f, 0.3f);
                ReportFailure("reactor.overshoot");
                StartCoroutine(AutoReset());
            }
        }

        private IEnumerator AutoReset()
        {
            _resetting = true;
            Refresh();
            yield return new WaitForSeconds(resetSeconds);
            _balance.Reset();
            _resetting = false;
            Refresh();
            Changed?.Invoke();
        }

        protected override void ApplySolvedState()
        {
            // a stable configuration: one valve on each setting
            _balance.Set(0, ReactorBalance.Low);
            _balance.Set(1, ReactorBalance.Mid);
            _balance.Set(2, ReactorBalance.High);
            Refresh();
            Changed?.Invoke();
        }

        private void Refresh()
        {
            int o = _balance.Output, t = _balance.Temperature;
            if (outputText != null)
            {
                outputText.text = Loc.Get("reactor.gauge.output", o);
                outputText.color = Colour(o, true);
            }
            if (temperatureText != null)
            {
                temperatureText.text = Loc.Get("reactor.gauge.temperature", t);
                temperatureText.color = Colour(t, false);
            }
            if (outputNeedle != null) outputNeedle.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(90f, -90f, o / 100f));
            if (temperatureNeedle != null) temperatureNeedle.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(90f, -90f, t / 100f));
            if (coreLight != null) coreLight.intensity = Mathf.Lerp(1.5f, 9f, o / 100f);

            if (loopGauges != null)
                for (int i = 0; i < loopGauges.Length && i < ReactorBalance.ValveCount; i++)
                {
                    if (loopGauges[i] == null) continue;
                    int s = _balance.Get(i);
                    loopGauges[i].Show(s, _balance.IsResonating(i),
                        Loc.Get("reactor.gauge.loop", ReactorBalance.PressureBar(s).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), Loc.Get(ValveInteractable.SettingKey(s))));
                }
            if (statusText != null)
            {
                var loops = _balance.ResonatingLoops();
                if (IsSolved || _balance.IsStable)
                {
                    statusText.text = Loc.Get("reactor.status.stable");
                    statusText.color = new Color(0.45f, 1f, 0.6f);
                }
                else if (_resetting)
                {
                    statusText.text = Loc.Get("reactor.status.scram");
                    statusText.color = new Color(1f, 0.35f, 0.3f);
                }
                else if (loops.Length > 0)
                {
                    var names = new System.Text.StringBuilder();
                    foreach (int i in loops)
                    {
                        if (names.Length > 0) names.Append(" = ");
                        names.Append((char)('A' + i));
                    }
                    statusText.text = Loc.Get("reactor.status.resonance", names.ToString());
                    statusText.color = PressureGauge.Resonant;
                }
                else
                {
                    statusText.text = Loc.Get("reactor.status.clear");
                    statusText.color = PressureGauge.Calm;
                }
            }
        }

        private static Color Colour(int v, bool output)
        {
            if (ReactorBalance.InBand(v)) return new Color(0.45f, 1f, 0.6f);
            if (output && v > ReactorBalance.OvershootAbove) return new Color(1f, 0.35f, 0.3f);
            return new Color(1f, 0.75f, 0.4f);
        }

#if UNITY_EDITOR
        public void EditorBindGauges(TMP_Text output, TMP_Text temperature, Transform oNeedle, Transform tNeedle, Light core)
        {
            outputText = output; temperatureText = temperature; outputNeedle = oNeedle; temperatureNeedle = tNeedle; coreLight = core;
        }

        public void EditorBindLoopGauges(PressureGauge[] gauges, TMP_Text status)
        {
            loopGauges = gauges;
            statusText = status;
        }
#endif
    }
}
