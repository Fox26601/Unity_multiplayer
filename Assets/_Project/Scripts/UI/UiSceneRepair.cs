using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>
    /// Forces readable contrast and layout on broken legacy scenes (white-on-white, overlapping).
    /// </summary>
    public static class UiSceneRepair
    {
        public static void RepairCanvas(Transform canvasRoot)
        {
            if (canvasRoot == null) return;

            UiCanvasFix.EnsureReadableCanvas(canvasRoot);

            var panel = canvasRoot.Find("Panel");
            EnsureTitleAndSubtitle(panel);

            foreach (var btn in canvasRoot.GetComponentsInChildren<Button>(true))
                ForceReadableButton(btn);

            foreach (var input in canvasRoot.GetComponentsInChildren<InputField>(true))
                ForceReadableInput(input);

            foreach (var input in canvasRoot.GetComponentsInChildren<TMP_InputField>(true))
                ForceReadableTmpInput(input);

            foreach (var img in canvasRoot.GetComponentsInChildren<Image>(true))
                FixWhitePanelImage(img);

            foreach (var t in canvasRoot.GetComponentsInChildren<Text>(true))
                ForceReadableLegacyText(t);

            UiLayoutRuntimeFix.ApplyIfNeeded(canvasRoot);
            UiTypography.ApplyHierarchy(canvasRoot);
        }

        private static void EnsureTitleAndSubtitle(Transform panel)
        {
            if (panel == null) return;

            if (panel.Find("Title") == null)
                CreateHeaderText(panel, "Title", UiCopy.MainMenuTitle, UiTypography.Title, UiTheme.TitleAccent, true);

            if (panel.Find("Subtitle") == null)
                CreateHeaderText(panel, "Subtitle", UiCopy.MainMenuSubtitle, UiTypography.Subtitle, UiTheme.TextSubtitle, false);
        }

        private static void CreateHeaderText(Transform panel, string name, string content, int fontSize, Color color,
            bool bold)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(panel, false);
            go.transform.SetAsFirstSibling();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = name == "Title" ? new Vector2(0f, -8f) : new Vector2(0f, -96f);
            rt.sizeDelta = name == "Title" ? new Vector2(-48f, 88f) : new Vector2(-64f, 72f);
            var t = go.GetComponent<Text>();
            t.text = content;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static void ForceReadableButton(Button button)
        {
            if (button == null) return;
            var img = button.GetComponent<Image>();
            if (img != null)
                img.color = UiTheme.ButtonBackground;

            var colors = button.colors;
            colors.normalColor = UiTheme.ButtonBackground;
            colors.highlightedColor = UiTheme.ButtonHighlighted;
            colors.pressedColor = UiTheme.ButtonPressed;
            colors.selectedColor = UiTheme.ButtonHighlighted;
            colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.65f);
            button.colors = colors;
        }

        private static void ForceReadableInput(InputField input)
        {
            if (input == null) return;
            var img = input.GetComponent<Image>();
            if (img != null && img.color.grayscale > 0.85f)
                img.color = UiTheme.InputBackground;
        }

        private static void ForceReadableTmpInput(TMP_InputField input)
        {
            if (input == null) return;
            var img = input.GetComponent<Image>();
            if (img != null && img.color.grayscale > 0.85f)
                img.color = UiTheme.InputBackground;
        }

        private static void FixWhitePanelImage(Image img)
        {
            if (img == null) return;
            if (img.GetComponent<Button>() != null || img.GetComponent<InputField>() != null ||
                img.GetComponent<TMP_InputField>() != null)
                return;

            if (img.transform.parent != null && img.transform.parent.name == "Panel" && img.color.a > 0.5f)
                img.color = UiTheme.PanelBackground;
        }

        private static void ForceReadableLegacyText(Text t)
        {
            if (t == null) return;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (t.gameObject.name == "Title")
            {
                t.fontSize = UiTypography.Title;
                t.fontStyle = FontStyle.Bold;
                t.color = UiTheme.TitleAccent;
                t.alignment = TextAnchor.MiddleCenter;
                return;
            }

            if (t.gameObject.name == "Subtitle" || t.gameObject.name == "SessionStatus")
            {
                t.fontSize = UiTypography.Subtitle;
                t.color = UiTheme.TextSubtitle;
                t.alignment = TextAnchor.MiddleCenter;
                return;
            }

            var onButton = t.GetComponentInParent<Button>() != null;
            var isPlaceholder = t.gameObject.name == "Placeholder";

            if (onButton)
            {
                t.fontSize = Mathf.Max(t.fontSize, UiTypography.Button);
                t.fontStyle = FontStyle.Bold;
                t.color = UiTheme.TextPrimary;
                t.alignment = TextAnchor.MiddleCenter;
                return;
            }

            if (isPlaceholder)
            {
                t.fontSize = UiTypography.Input;
                t.fontStyle = FontStyle.Italic;
                t.color = UiTheme.Placeholder;
                return;
            }

            if (t.fontSize < UiTypography.MinimumReadable)
                t.fontSize = UiTypography.Body;

            if (t.color.grayscale > 0.85f && !isPlaceholder)
                t.color = UiTheme.TextPrimary;
        }
    }
}
