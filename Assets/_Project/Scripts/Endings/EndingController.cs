using System.Collections;
using System.Collections.Generic;
using LastSignal.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.Endings
{
    /// <summary>
    /// Ending scene director: reads choice.final and AI trust, plays the ending's lines over the exterior with its
    /// visual event, then the epilogue and credits, then returns to the main menu.
    /// </summary>
    public sealed class EndingController : MonoBehaviour
    {
        [SerializeField] private Camera shotCamera;
        [SerializeField] private Transform lookTarget;
        [SerializeField] private Vector3 orbitCenter;
        [SerializeField] private float orbitRadius = 105f;
        [SerializeField] private float orbitHeight = 28f;
        [SerializeField] private float orbitDegreesPerSecond = 1.2f;

        [Header("Visual events")]
        [SerializeField] private GameObject stationRoot;
        [SerializeField] private Transform explosion;
        [SerializeField] private GameObject stationLightsOn;
        [SerializeField] private Transform arrayDish;
        [SerializeField] private List<Renderer> murmurPulses = new();

        [Header("UI")]
        [SerializeField] private Image fade;
        [SerializeField] private TMP_Text speaker;
        [SerializeField] private TMP_Text line;
        [SerializeField] private TMP_Text caption;
        [SerializeField] private TMP_Text epilogue;
        [SerializeField] private TMP_Text credits;

        private EndingType _type;
        private float _angle = 245f;
        private bool _turnDish;
        private float _murmurFade = 1f;
        private bool _fadingMurmur;

        public EndingType Type => _type;
        public bool Done { get; private set; }

        private void Start()
        {
            GameServices.State.Set(GameState.Ending);
            _type = EndingScript.FromId(GameServices.World.GetString(EndingScript.ChoiceKey));
            if (_type == EndingType.None)
            {
                Log.Warn(nameof(EndingController), "No ending chosen; showing Ending B as a fallback.");
                _type = EndingType.Restore;
            }
            explosion.gameObject.SetActive(false);
            stationLightsOn.SetActive(false);
            speaker.text = line.text = caption.text = epilogue.text = credits.text = string.Empty;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            int trust = GameServices.World.GetInt(WorldKeys.AiTrust, WorldKeys.AiTrustDefault);
            Log.Info(nameof(EndingController), $"Ending {_type}, trust {trust}.");
            yield return Fade(1f, 0f, 2f);
            foreach (var beat in EndingScript.Lines(_type, trust))
            {
                switch (beat.Beat)
                {
                    case EndingBeat.Cairn:
                        speaker.text = Loc.Get("speaker.cairn");
                        line.text = Loc.Get(beat.Key);
                        yield return Hold(Dialogue.DialogueManager.ReadingTime(line.text) + 0.6f);
                        speaker.text = line.text = string.Empty;
                        break;
                    case EndingBeat.Caption:
                        caption.text = Loc.Get(beat.Key);
                        yield return Hold(Dialogue.DialogueManager.ReadingTime(caption.text) + 1.5f);
                        caption.text = string.Empty;
                        break;
                    case EndingBeat.Event:
                        yield return PlayEvent();
                        break;
                    case EndingBeat.Epilogue:
                        yield return Fade(0f, 0.85f, 2.5f);
                        epilogue.text = Loc.Get(beat.Key);
                        yield return Hold(Dialogue.DialogueManager.ReadingTime(epilogue.text) * 1.2f + 3f);
                        epilogue.text = string.Empty;
                        break;
                }
            }
            yield return Fade(0.85f, 1f, 1.5f);
            credits.text = Loc.Get("ui.credits");
            yield return Hold(7f);
            Done = true;
            GameServices.Scenes.Load(SceneIds.MainMenu);
        }

        private IEnumerator PlayEvent()
        {
            switch (_type)
            {
                case EndingType.Destroy:
                    // silent bloom (no sound in space)
                    explosion.gameObject.SetActive(true);
                    for (float t = 0; t < 3.5f; t += Time.deltaTime)
                    {
                        float k = t / 3.5f;
                        explosion.localScale = Vector3.one * Mathf.Lerp(2f, 90f, Mathf.Sqrt(k));
                        if (k > 0.25f && stationRoot.activeSelf) stationRoot.SetActive(false);
                        yield return null;
                    }
                    for (float t = 0; t < 2.5f; t += Time.deltaTime)
                    {
                        explosion.localScale = Vector3.one * Mathf.Lerp(90f, 0.1f, t / 2.5f);
                        yield return null;
                    }
                    explosion.gameObject.SetActive(false);
                    break;
                case EndingType.Restore:
                    stationLightsOn.SetActive(true);
                    _turnDish = true;
                    yield return Hold(2f);
                    break;
                case EndingType.Silence:
                    _fadingMurmur = true;
                    yield return Hold(6f);
                    break;
            }
        }

        private void Update()
        {
            _angle += orbitDegreesPerSecond * Time.deltaTime;
            float rad = _angle * Mathf.Deg2Rad;
            shotCamera.transform.position = orbitCenter + new Vector3(Mathf.Cos(rad) * orbitRadius, orbitHeight, Mathf.Sin(rad) * orbitRadius);
            shotCamera.transform.LookAt(lookTarget);
            if (_turnDish && arrayDish != null) arrayDish.Rotate(Vector3.up, 6f * Time.deltaTime, Space.World);
            if (_fadingMurmur)
            {
                // pulses go dark one by one
                _murmurFade -= Time.deltaTime / 6f;
                for (int i = 0; i < murmurPulses.Count; i++)
                {
                    float threshold = (i + 1f) / (murmurPulses.Count + 1f);
                    if (_murmurFade < threshold && murmurPulses[i] != null) murmurPulses[i].gameObject.SetActive(false);
                }
            }
        }

        private static IEnumerator Hold(float seconds)
        {
            for (float t = 0; t < seconds; t += Time.deltaTime) yield return null;
        }

        private IEnumerator Fade(float from, float to, float seconds)
        {
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                SetFade(Mathf.Lerp(from, to, t / seconds));
                yield return null;
            }
            SetFade(to);
        }

        private void SetFade(float a)
        {
            var c = fade.color;
            c.a = a;
            fade.color = c;
        }

#if UNITY_EDITOR
        public void EditorBind(Camera cam, Transform target, Vector3 center, GameObject station, Transform boom, GameObject lightsOn, Transform dish,
            List<Renderer> pulses, Image f, TMP_Text spk, TMP_Text ln, TMP_Text cap, TMP_Text epi, TMP_Text cred)
        {
            shotCamera = cam; lookTarget = target; orbitCenter = center; stationRoot = station; explosion = boom; stationLightsOn = lightsOn;
            arrayDish = dish; murmurPulses = pulses; fade = f; speaker = spk; line = ln; caption = cap; epilogue = epi; credits = cred;
        }
#endif
    }
}
