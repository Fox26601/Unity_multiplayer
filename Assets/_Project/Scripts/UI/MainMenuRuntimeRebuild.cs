using FusionMultiplayer.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>
    /// Builds a readable main-menu panel at runtime (TMP text, solid-color widgets, no legacy sprites).
    /// Layout uses non-overlapping vertical bands (anchor fractions).
    /// </summary>
    public static class MainMenuRuntimeRebuild
    {
        private const int MenuBuildVersion = 6;

        private static TMP_FontAsset _tmpFont;

        public static void EnsureBuilt(Transform canvas)
        {
            if (canvas == null || canvas.GetComponent<MainMenuUI>() == null)
                return;

            EnsureCanvasScaler(canvas);
            UiCanvasFix.EnsureReadableCanvas(canvas);

            var panel = canvas.Find("Panel");
            if (panel != null && !NeedsRebuild(panel))
                return;

            _tmpFont = UiTypography.DefaultFont;
            if (_tmpFont == null)
                Debug.LogWarning("[FusionMultiplayer] TMP font missing — run Tools → Fusion Multiplayer → Import TMP Essential Resources.");

            if (panel == null)
                panel = CreatePanel(canvas);
            else
                ClearChildren(panel);

            BuildContents(panel, canvas.GetComponent<MainMenuUI>());
        }

        private static void EnsureCanvasScaler(Transform canvas)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = UiTypography.CanvasReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private static bool NeedsRebuild(Transform panel)
        {
            var version = panel.Find("MenuUiVersion")?.GetComponent<UiBuildVersionMarker>();
            if (version == null || version.Version != MenuBuildVersion)
                return true;

            var legacyVersion = panel.Find("MenuUiVersion")?.GetComponent<TMP_Text>();
            if (legacyVersion != null)
                return true;

            var title = panel.Find("Title")?.GetComponent<TMP_Text>();
            if (title == null || title.fontSize < 60)
                return true;

            if (panel.Find("NicknameLabel") == null || panel.Find("RoomLabel") == null)
                return true;

            var nickRt = panel.Find("NicknameField") as RectTransform;
            if (nickRt == null || nickRt.anchorMin.y < 0.5f)
                return true;

            var btnImage = panel.Find("BtnCreate")?.GetComponent<Image>();
            return btnImage == null || btnImage.color.grayscale > 0.7f;
        }

        private static Transform CreatePanel(Transform canvas)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvas, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(32f, 32f);
            rt.offsetMax = new Vector2(-32f, -32f);
            go.GetComponent<Image>().color = UiTheme.PanelBackground;

            if (go.GetComponent<UIPanelHost>() == null)
                go.AddComponent<UIPanelHost>();
            return go.transform;
        }

        private static void ClearChildren(Transform panel)
        {
            for (var i = panel.childCount - 1; i >= 0; i--)
            {
                var child = panel.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Object.Destroy(child);
                else
                    Object.DestroyImmediate(child);
            }
        }

        private static void BuildContents(Transform panel, MainMenuUI menu)
        {
            // Vertical bands (yMin, yMax) — no overlap.
            const float bandTitleTop = 0.98f;
            const float bandTitleBottom = 0.88f;
            const float bandSubtitleTop = 0.87f;
            const float bandSubtitleBottom = 0.80f;
            const float bandNickTop = 0.79f;
            const float bandNickBottom = 0.66f;
            const float bandRoomTop = 0.65f;
            const float bandRoomBottom = 0.52f;
            const float bandColorTop = 0.51f;
            const float bandColorBottom = 0.36f;
            const float bandJoinTop = 0.22f;
            const float bandJoinBottom = 0.13f;
            const float bandCreateTop = 0.33f;
            const float bandCreateBottom = 0.24f;
            const float bandStatusTop = 0.12f;
            const float bandStatusBottom = 0.04f;

            CreateBandLabel(panel, "Title", UiCopy.MainMenuTitle, UiTypography.Title, UiTheme.TitleAccent,
                FontStyles.Bold, bandTitleBottom, bandTitleTop, TextAlignmentOptions.Center);
            UiTypography.ApplyTitle(panel.Find("Title")?.GetComponent<TMP_Text>());

            CreateBandLabel(panel, "Subtitle", UiCopy.MainMenuSubtitle, UiTypography.Subtitle, UiTheme.TextSubtitle,
                FontStyles.Normal, bandSubtitleBottom, bandSubtitleTop, TextAlignmentOptions.Center);
            UiTypography.ApplySubtitle(panel.Find("Subtitle")?.GetComponent<TMP_Text>());

            var nick = CreateBandField(panel, "NicknameField", UiCopy.NicknameLabel, UiCopy.NicknamePlaceholder,
                bandNickBottom, bandNickTop);
            var room = CreateBandField(panel, "RoomField", UiCopy.RoomLabel, UiCopy.RoomPlaceholder,
                bandRoomBottom, bandRoomTop);

            CreateBandLabel(panel, "ColorLabel", "Player color", UiTypography.FieldLabel, UiTheme.TextPrimary,
                FontStyles.Bold, bandColorBottom + 0.08f, bandColorTop, TextAlignmentOptions.MidlineLeft);

            var preview = CreateColorPreview(panel, bandColorBottom, bandColorBottom + 0.07f);
            var randomColor = CreateBandButton(panel, "BtnRandomColor", "Random player color",
                bandColorBottom, bandColorBottom + 0.07f, new Vector2(420f, 0f));

            var create = CreateBandButton(panel, "BtnCreate", "Create / Host room",
                bandCreateBottom, bandCreateTop, new Vector2(520f, 0f));
            var join = CreateBandButton(panel, "BtnJoin", "Join room",
                bandJoinBottom, bandJoinTop, new Vector2(520f, 0f));

            CreateBandLabel(panel, "SessionStatus", string.Empty, UiTypography.Body, UiTheme.StatusOk,
                FontStyles.Normal, bandStatusBottom, bandStatusTop, TextAlignmentOptions.Center);

            menu.BindRuntimeTmp(nick, room, create, join, preview, randomColor);

            var versionGo = new GameObject("MenuUiVersion");
            versionGo.transform.SetParent(panel, false);
            versionGo.hideFlags = HideFlags.HideInHierarchy;
            var marker = versionGo.AddComponent<UiBuildVersionMarker>();
            marker.Version = MenuBuildVersion;
        }

        private static void CreateBandLabel(Transform parent, string name, string text, int fontSize, Color color,
            FontStyles style, float yMin, float yMax, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            UiRegionLayout.StretchBand(go.GetComponent<RectTransform>(), yMin, yMax, 48f);

            var label = go.GetComponent<TMP_Text>();
            if (_tmpFont != null) label.font = _tmpFont;
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Truncate;

            if (string.IsNullOrEmpty(text))
                go.SetActive(false);
        }

        private static TMP_InputField CreateBandField(Transform parent, string fieldName, string labelText,
            string placeholder, float yMin, float yMax)
        {
            var labelGo = new GameObject(fieldName + "Label", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(parent, false);
            UiRegionLayout.StretchBand(labelGo.GetComponent<RectTransform>(), yMax - 0.05f, yMax, 48f);
            var label = labelGo.GetComponent<TMP_Text>();
            if (_tmpFont != null) label.font = _tmpFont;
            label.text = labelText;
            label.fontSize = UiTypography.FieldLabel;
            label.fontStyle = FontStyles.Bold;
            label.color = UiTheme.TextPrimary;
            label.alignment = TextAlignmentOptions.BottomLeft;

            var fieldGo = new GameObject(fieldName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(TMP_InputField));
            fieldGo.transform.SetParent(parent, false);
            UiRegionLayout.StretchBand(fieldGo.GetComponent<RectTransform>(), yMin, yMax - 0.06f, 48f);

            var img = fieldGo.GetComponent<Image>();
            img.color = UiTheme.InputBackground;

            var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(fieldGo.transform, false);
            Stretch(textArea.GetComponent<RectTransform>(), 8f, 6f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(textArea.transform, false);
            Stretch(textGo.GetComponent<RectTransform>(), 4f, 2f);
            var text = textGo.GetComponent<TMP_Text>();
            if (_tmpFont != null) text.font = _tmpFont;
            UiTypography.ApplyInputText(text, false);

            var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            phGo.transform.SetParent(textArea.transform, false);
            Stretch(phGo.GetComponent<RectTransform>(), 4f, 2f);
            var ph = phGo.GetComponent<TMP_Text>();
            if (_tmpFont != null) ph.font = _tmpFont;
            ph.text = placeholder;
            UiTypography.ApplyInputText(ph, true);
            ph.raycastTarget = false;

            var field = fieldGo.GetComponent<TMP_InputField>();
            field.textViewport = textArea.GetComponent<RectTransform>();
            field.textComponent = text;
            field.placeholder = ph;
            field.targetGraphic = img;
            field.fontAsset = _tmpFont;
            field.pointSize = UiTypography.Input;
            return field;
        }

        private static Image CreateColorPreview(Transform parent, float yMin, float yMax)
        {
            var go = new GameObject("ColorPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.08f, yMin);
            rt.anchorMax = new Vector2(0.08f, yMax);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(72f, 0f);
            var img = go.GetComponent<Image>();
            img.color = SessionData.Tint;
            return img;
        }

        private static Button CreateBandButton(Transform parent, string name, string label,
            float yMin, float yMax, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            if (name == "BtnRandomColor")
            {
                rt.anchorMin = new Vector2(0.22f, yMin);
                rt.anchorMax = new Vector2(0.92f, yMax);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            else
            {
                UiRegionLayout.CenterInBand(rt, yMin, yMax, new Vector2(size.x, 0f));
            }

            var img = go.GetComponent<Image>();
            img.color = UiTheme.ButtonBackground;

            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(go.transform, false);
            Stretch(txtGo.GetComponent<RectTransform>(), 10f, 8f);
            var txt = txtGo.GetComponent<TMP_Text>();
            txt.text = label;
            if (_tmpFont != null) txt.font = _tmpFont;
            UiTypography.ApplyButtonLabel(txt);

            var button = go.GetComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);
            button.colors = colors;
            return button;
        }

        private static void Stretch(RectTransform rt, float horizontalPadding, float verticalPadding)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(horizontalPadding, verticalPadding);
            rt.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
        }
    }
}
