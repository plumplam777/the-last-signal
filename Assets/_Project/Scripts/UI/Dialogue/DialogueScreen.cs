using System.Collections.Generic;
using LastSignal.Core;
using LastSignal.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.UI
{
    /// <summary>
    /// CAIRN comms panel: speaker, typewriter line, up to three numbered choices (keys 1–3 or click).
    /// CAIRN's "eye" pulses while it speaks and flashes teal/red when trust rises/falls (no numbers shown).
    /// Rendering only — all logic is in <see cref="DialogueManager"/>.
    /// </summary>
    public sealed class DialogueScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image eye;
        [SerializeField] private Image eyeRing;
        [SerializeField] private TMP_Text speaker;
        [SerializeField] private TMP_Text line;
        [SerializeField] private Transform choiceList;
        [SerializeField] private Button choiceTemplate;
        [SerializeField] private float charsPerSecond = 55f;

        private static readonly Color EyeIdle = new(0.37f, 0.9f, 1f);
        private static readonly Color TrustUp = new(0.5f, 1f, 0.85f);
        private static readonly Color TrustDown = new(0.88f, 0.27f, 0.23f);

        private readonly List<GameObject> _choiceRows = new();
        private float _reveal;
        private float _flash;
        private Color _flashColor;
        private float _targetAlpha;

        private void Awake()
        {
            choiceTemplate.gameObject.SetActive(false);
            group.alpha = 0f;
        }

        private void OnEnable()
        {
            var d = GameServices.Dialogue;
            d.LineShown += OnLine;
            d.ChoicesShown += OnChoices;
            d.ChoiceMade += OnChoiceMade;
            d.Ended += OnEnded;
            GameServices.Localization.LanguageChanged += Refresh;
            if (d.IsRunning && d.CurrentNode != null) OnLine(d.CurrentNode);
        }

        private void OnDisable()
        {
            if (!GameServices.IsReady) return;
            var d = GameServices.Dialogue;
            d.LineShown -= OnLine;
            d.ChoicesShown -= OnChoices;
            d.ChoiceMade -= OnChoiceMade;
            d.Ended -= OnEnded;
            GameServices.Localization.LanguageChanged -= Refresh;
        }

        private void OnLine(DialogueNode node)
        {
            ClearChoices();
            _targetAlpha = 1f;
            _reveal = 0f;
            Refresh();
        }

        private void Refresh()
        {
            var node = GameServices.Dialogue.CurrentNode;
            if (node == null || string.IsNullOrEmpty(node.lineKey)) return;
            speaker.text = Loc.Get(node.speakerKey);
            line.text = Loc.Get(node.lineKey);
            line.maxVisibleCharacters = Mathf.FloorToInt(_reveal);
        }

        private void OnChoices(DialogueNode node, IReadOnlyList<DialogueChoice> choices)
        {
            ClearChoices();
            _reveal = 9999f;
            for (int i = 0; i < choices.Count; i++)
            {
                int index = i;
                var row = Instantiate(choiceTemplate, choiceList);
                row.gameObject.SetActive(true);
                row.GetComponentInChildren<TMP_Text>().text = $"<color=#5FE6FF>{i + 1}.</color>  {Loc.Get(choices[i].textKey)}";
                row.onClick.AddListener(() => GameServices.Dialogue.Choose(index));
                _choiceRows.Add(row.gameObject);
            }
        }

        private void OnChoiceMade(DialogueChoice choice, int trustDelta)
        {
            ClearChoices();
            if (trustDelta == 0) return;
            _flash = 1.4f;
            _flashColor = trustDelta > 0 ? TrustUp : TrustDown;
        }

        private void OnEnded(DialogueGraph graph)
        {
            ClearChoices();
            _targetAlpha = 0f;
        }

        private void ClearChoices()
        {
            foreach (var row in _choiceRows) Destroy(row);
            _choiceRows.Clear();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            group.alpha = Mathf.MoveTowards(group.alpha, _targetAlpha, dt * 4f);
            group.blocksRaycasts = _choiceRows.Count > 0;
            if (_targetAlpha <= 0f) return;

            if (_reveal < 9999f)
            {
                _reveal += charsPerSecond * dt;
                line.maxVisibleCharacters = Mathf.FloorToInt(_reveal);
            }

            // eye: gentle pulse while speaking, colour flash on trust change
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 3.1f);
            var c = EyeIdle;
            if (_flash > 0f)
            {
                _flash -= dt;
                c = Color.Lerp(EyeIdle, _flashColor, Mathf.Clamp01(_flash));
            }
            eye.color = new Color(c.r, c.g, c.b, pulse);
            eyeRing.color = new Color(c.r, c.g, c.b, 0.55f + 0.2f * pulse);
            eyeRing.rectTransform.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(Time.unscaledTime * 1.7f));
        }

#if UNITY_EDITOR
        public void EditorBind(CanvasGroup g, Image e, Image ring, TMP_Text s, TMP_Text l, Transform list, Button template)
        {
            group = g; eye = e; eyeRing = ring; speaker = s; line = l; choiceList = list; choiceTemplate = template;
        }
#endif
    }
}
