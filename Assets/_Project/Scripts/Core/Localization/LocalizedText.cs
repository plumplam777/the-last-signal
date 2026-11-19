using TMPro;
using UnityEngine;

namespace LastSignal.Core
{
    /// <summary>
    /// Put on any TextMeshPro text to display a localization key; refreshes when the language changes.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key;

        private TMP_Text _text;
        private object[] _args;

        public string Key => key;

        private void Awake() => _text = GetComponent<TMP_Text>();

        private void OnEnable()
        {
            GameServices.Localization.LanguageChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (GameServices.IsReady) GameServices.Localization.LanguageChanged -= Refresh;
        }

        public void SetKey(string newKey, params object[] args)
        {
            key = newKey;
            _args = args;
            Refresh();
        }

        public void Refresh()
        {
            if (_text == null) _text = GetComponent<TMP_Text>();
            _text.text = string.IsNullOrEmpty(key) ? string.Empty : GameServices.Localization.Get(key, _args);
        }
    }
}
