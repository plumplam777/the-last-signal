using LastSignal.Core;
using LastSignal.Quests;
using TMPro;
using UnityEngine;

namespace LastSignal.UI
{
    /// <summary>Top-left objective panel. Shows the current main-quest objective; flashes on change.</summary>
    public sealed class ObjectiveHud : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text header;
        [SerializeField] private TMP_Text text;

        private ObjectiveDefinition _shown;
        private float _flash;

        private void OnEnable()
        {
            GameServices.Quests.CurrentObjectiveChanged += Show;
            GameServices.Quests.ObjectiveCompleted += OnCompleted;
            GameServices.Localization.LanguageChanged += Refresh;
            Show(GameServices.Quests.CurrentObjective);
        }

        private void OnDisable()
        {
            if (!GameServices.IsReady) return;
            GameServices.Quests.CurrentObjectiveChanged -= Show;
            GameServices.Quests.ObjectiveCompleted -= OnCompleted;
            GameServices.Localization.LanguageChanged -= Refresh;
        }

        private void OnCompleted(QuestDefinition quest, ObjectiveDefinition objective)
            => GetComponentInParent<HudController>()?.ShowToast(Loc.Get("toast.objective_complete"));

        private void Show(ObjectiveDefinition objective)
        {
            _shown = objective;
            _flash = objective != null ? 1.2f : 0f;
            Refresh();
        }

        private void Refresh()
        {
            group.alpha = _shown != null ? 1f : 0f;
            if (_shown == null) return;
            header.text = Loc.Get("ui.hud.objective");
            text.text = Loc.Get(_shown.textKey);
        }

        private void Update()
        {
            if (_flash <= 0f) return;
            _flash -= Time.unscaledDeltaTime;
            float pulse = Mathf.Clamp01(_flash) * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 14f));
            text.color = Color.Lerp(new Color(0.9f, 0.96f, 1f), new Color(0.37f, 0.9f, 1f), pulse);
        }

#if UNITY_EDITOR
        public void EditorBind(CanvasGroup g, TMP_Text h, TMP_Text t) { group = g; header = h; text = t; }
#endif
    }
}
