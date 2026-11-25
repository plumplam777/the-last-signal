using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LastSignal.UI
{
    /// <summary>
    /// A start-screen menu entry: plain text until it is selected (mouse hover or arrow keys), then it shows its
    /// bevelled cyan frame and the one-line hint underneath. Hovering selects, so mouse and keyboard never disagree.
    /// </summary>
    public sealed class MenuItemSelect : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
    {
        private GameObject _highlight;
        private TMP_Text _label;
        private Color _normal, _selected;
        private bool _pinned, _isSelected;

        /// <summary>Keeps the frame on while this item's side panel is open, even when focus is inside the panel.</summary>
        public bool Pinned
        {
            get => _pinned;
            set { _pinned = value; Set(_isSelected); }
        }

        public bool IsHighlighted => _highlight != null && _highlight.activeSelf;

        public void Setup(GameObject highlight, TMP_Text label, Color normal, Color selected)
        {
            _highlight = highlight;
            _label = label;
            _normal = normal;
            _selected = selected;
            Set(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != gameObject)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public void OnSelect(BaseEventData eventData) => Set(true);
        public void OnDeselect(BaseEventData eventData) => Set(false);
        private void OnDisable() => Set(false);

        private void Set(bool selected)
        {
            _isSelected = selected;
            bool on = selected || _pinned;
            if (_highlight != null) _highlight.SetActive(on);
            if (_label != null) _label.color = on ? _selected : _normal;
        }
    }
}
