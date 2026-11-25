using UnityEngine;

namespace LastSignal.UI
{
    /// <summary>
    /// Start-screen side panel: slides out from the menu column and fades in (0.25 s, unscaled time) each time it is
    /// shown. Update only runs while the animation plays.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PopOutPanel : MonoBehaviour
    {
        [SerializeField] private float duration = 0.25f;
        [SerializeField] private float slide = 70f;

        private RectTransform _rect;
        private CanvasGroup _group;
        private Vector2 _home;
        private float _t = 1f;

        public bool IsAnimating => _t < 1f;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _group = GetComponent<CanvasGroup>();
            _home = _rect.anchoredPosition;
        }

        /// <summary>Shows the panel and replays the slide-out (also when switching between pages).</summary>
        public void Pop()
        {
            if (_rect == null) Awake();
            gameObject.SetActive(true);
            _t = 0f;
            Apply();
            enabled = true;
        }

        public void Hide()
        {
            _t = 1f;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            _t = Mathf.Min(1f, _t + Time.unscaledDeltaTime / duration);
            Apply();
            if (_t >= 1f) enabled = false;
        }

        private void Apply()
        {
            float e = 1f - Mathf.Pow(1f - _t, 3f); // ease-out cubic
            _rect.anchoredPosition = _home + new Vector2((e - 1f) * slide, 0f);
            _group.alpha = e;
            _group.interactable = _t >= 0.6f;
        }
    }
}
