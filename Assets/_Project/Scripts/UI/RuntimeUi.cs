using System;
using LastSignal.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LastSignal.UI
{
    /// <summary>
    /// Small runtime UI kit in the "Brass &amp; Signal" style for screens that build themselves in code
    /// (start screen, admin panel). Texts created with a key get a <see cref="LocalizedText"/>.
    /// </summary>
    public static class RuntimeUi
    {
        public static readonly Color Navy = new(0.078f, 0.106f, 0.149f);
        public static readonly Color PanelColor = new(0.05f, 0.07f, 0.11f, 1f);
        public static readonly Color FieldColor = new(0.09f, 0.12f, 0.17f, 1f);
        public static readonly Color ButtonColor = new(0.08f, 0.13f, 0.19f, 1f);
        public static readonly Color Brass = new(0.79f, 0.63f, 0.29f);
        public static readonly Color Cyan = new(0.37f, 0.9f, 1f);
        public static readonly Color TealLight = new(0.5f, 0.89f, 0.85f);
        public static readonly Color TextColor = new(0.88f, 0.94f, 1f);
        public static readonly Color Muted = new(1f, 1f, 1f, 0.45f);
        public static readonly Color Red = new(0.88f, 0.27f, 0.23f);

        public static Canvas Canvas(string name, Transform parent, int order)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var s = go.GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            s.matchWidthOrHeight = 0.5f;
            return c;
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size, Vector2? anchor = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = anchor ?? new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            return r;
        }

        public static RectTransform Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            return r;
        }

        public static Image Image(string name, Transform parent, Vector2 pos, Vector2 size, Color color, Vector2? anchor = null)
        {
            var img = Rect(name, parent, pos, size, anchor).gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        /// <summary>Opaque panel with a thin brass outline (opaque so the outline never tints it).</summary>
        public static Image Panel(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var img = Image(name, parent, pos, size, PanelColor);
            var o = img.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(Brass.r, Brass.g, Brass.b, 0.8f);
            o.effectDistance = new Vector2(2, -2);
            return img;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, Vector2 pos, Vector2 size, float fontSize, Color color,
            string key = null, TextAlignmentOptions align = TextAlignmentOptions.Center, Vector2? anchor = null)
        {
            var r = Rect(name, parent, pos, size, anchor);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            if (key != null) r.gameObject.AddComponent<LocalizedText>().SetKey(key);
            return t;
        }

        public static Button Button(string name, Transform parent, Vector2 pos, Vector2 size, string key, UnityAction onClick, float fontSize = 26)
        {
            var img = Image(name, parent, pos, size, ButtonColor);
            var o = img.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(Brass.r, Brass.g, Brass.b, 0.7f);
            o.effectDistance = new Vector2(1.5f, -1.5f);
            var b = img.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.highlightedColor = new Color(0.65f, 0.95f, 1f);
            colors.pressedColor = new Color(0.45f, 0.75f, 0.85f);
            colors.selectedColor = new Color(0.75f, 0.92f, 1f);
            colors.disabledColor = new Color(0.4f, 0.4f, 0.45f, 0.5f);
            b.colors = colors;
            Text("Label", img.transform, Vector2.zero, size - new Vector2(16, 4), fontSize, TextColor, key);
            Stretch((RectTransform)img.transform.Find("Label"));
            if (onClick != null) b.onClick.AddListener(onClick);
            b.onClick.AddListener(() => { if (GameServices.IsReady) GameServices.Audio.Sfx("sfx_ui", 0.8f); });
            return b;
        }

        public static void SetButtonText(Button b, string text)
        {
            var t = b.GetComponentInChildren<TMP_Text>();
            var lt = t.GetComponent<LocalizedText>();
            if (lt != null) UnityEngine.Object.Destroy(lt);
            t.text = text;
        }

        public static TMP_InputField Input(string name, Transform parent, Vector2 pos, Vector2 size, string placeholderKey, bool password = false)
        {
            var go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            go.name = name;
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            go.GetComponent<Image>().color = FieldColor;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f);
            var field = go.GetComponent<TMP_InputField>();
            field.textComponent.color = TextColor;
            field.textComponent.fontSize = 24;
            var ph = (TMP_Text)field.placeholder;
            ph.fontSize = 22;
            ph.color = Muted;
            ph.gameObject.AddComponent<LocalizedText>().SetKey(placeholderKey);
            field.contentType = password ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            field.characterLimit = 64;
            field.caretColor = Cyan;
            field.selectionColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f);
            return field;
        }

        public static Toggle Toggle(string name, Transform parent, Vector2 pos, string key, bool value, UnityAction<bool> onChange)
        {
            var r = Rect(name, parent, pos, new Vector2(360, 40));
            var box = Image("Box", r, new Vector2(-160, 0), new Vector2(30, 30), FieldColor);
            var check = Image("Check", box.transform, Vector2.zero, new Vector2(18, 18), Cyan);
            var t = r.gameObject.AddComponent<Toggle>();
            t.targetGraphic = box;
            t.graphic = check;
            t.isOn = value;
            Text("Label", r, new Vector2(30, 0), new Vector2(330, 40), 22, TextColor, key, TextAlignmentOptions.Left);
            if (onChange != null) t.onValueChanged.AddListener(onChange);
            return t;
        }

        public static Slider Slider(string name, Transform parent, Vector2 pos, float width, float min, float max, float value, UnityAction<float> onChange)
        {
            var r = Rect(name, parent, pos, new Vector2(width, 22));
            Image("Background", r, Vector2.zero, new Vector2(width, 8), FieldColor);
            var fillArea = Rect("Fill Area", r, Vector2.zero, new Vector2(width, 8));
            var fill = Image("Fill", fillArea, Vector2.zero, Vector2.zero, Cyan);
            Stretch((RectTransform)fill.transform);
            var handleArea = Rect("Handle Area", r, Vector2.zero, new Vector2(width, 22));
            var handle = Image("Handle", handleArea, Vector2.zero, new Vector2(18, 22), Brass);
            var s = r.gameObject.AddComponent<Slider>();
            s.fillRect = (RectTransform)fill.transform;
            s.handleRect = (RectTransform)handle.transform;
            s.targetGraphic = handle;
            s.minValue = min;
            s.maxValue = max;
            s.value = value;
            if (onChange != null) s.onValueChanged.AddListener(onChange);
            return s;
        }

        /// <summary>Scrollable vertical list; returns the content transform rows go into.</summary>
        public static RectTransform ScrollList(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var view = Image(name, parent, pos, size, new Color(0.03f, 0.05f, 0.08f, 1f));
            view.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", view.transform, Vector2.zero, new Vector2(size.x, 0));
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = new Vector2(6, content.offsetMin.y);
            content.offsetMax = new Vector2(-6, 0);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6;
            layout.padding = new RectOffset(0, 0, 6, 6);
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 30f;
            return content;
        }

        public static Button ListRow(Transform list, string text, UnityAction onClick, float height = 52)
        {
            var b = Button("Row", list, Vector2.zero, new Vector2(100, height), null, onClick, 21);
            var t = b.GetComponentInChildren<TMP_Text>();
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.text = text;
            ((RectTransform)t.transform).offsetMin = new Vector2(14, 0);
            return b;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }
    }
}
