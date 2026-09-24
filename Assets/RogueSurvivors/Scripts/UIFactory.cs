using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
namespace RogueSurvivors
{
    public static class UIFactory
    {
        static Font japaneseFont;
        public static Font JapaneseFont
        {
            get
            {
                if (!japaneseFont) japaneseFont = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic UI", "Yu Gothic", "Meiryo", "Noto Sans CJK JP", "Hiragino Sans" }, 24);
                if (!japaneseFont) japaneseFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return japaneseFont;
            }
        }
        public static readonly Color Ink = new Color(.035f, .05f, .10f, .96f);
        public static readonly Color Cyan = new Color(.28f, .94f, .92f);
        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor;
            rt.anchoredPosition = position; rt.sizeDelta = size; return rt;
        }
        public static Image Panel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var rt = Rect(name, parent, anchor, position, size);
            var image = rt.gameObject.AddComponent<Image>(); image.color = color; return image;
        }
        public static Text Label(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size,
            string text, int fontSize = 22, Color? color = null, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var rt = Rect(name, parent, anchor, position, size);
            var label = rt.gameObject.AddComponent<Text>(); label.font = JapaneseFont;
            label.text = text; label.fontSize = fontSize; label.color = color ?? Color.white;
            label.alignment = alignment; label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; return label;
        }
        public static Button Button(string name, Transform parent, Vector2 anchor, Vector2 position,
            Vector2 size, string text, UnityAction onClick)
        {
            var image = Panel(name, parent, anchor, position, size, new Color(.12f, .25f, .32f));
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(.5f, 1, .95f); colors.pressedColor = Cyan;
            button.colors = colors; if (onClick != null) button.onClick.AddListener(onClick);
            Label("Label", image.transform, new Vector2(.5f, .5f), Vector2.zero, size - new Vector2(20, 10), text, 21, null, TextAnchor.MiddleCenter);
            return button;
        }
        public static Image Bar(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var back = Panel(name, parent, Vector2.up, position, size, new Color(.10f, .13f, .20f));
            var fill = Panel("Fill", back.transform, Vector2.zero, Vector2.zero, size, color);
            return fill;
        }
        public static void SetBar(Image fill, float fraction, float width)
        {
            fill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width * Mathf.Clamp01(fraction));
        }
    }
}
