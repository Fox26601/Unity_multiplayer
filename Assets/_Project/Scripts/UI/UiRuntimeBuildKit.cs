using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>Shared TMP UI builders for runtime scene rebuilds.</summary>
    internal static class UiRuntimeBuildKit
    {
        internal static TMP_FontAsset TmpFont => UiTypography.DefaultFont;

        internal static void EnsureCanvasScaler(Transform canvas)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = UiTypography.CanvasReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
        }

        internal static Transform CreatePanel(Transform canvas)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvas, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(24f, 24f);
            rt.offsetMax = new Vector2(-24f, -24f);
            go.GetComponent<Image>().color = UiTheme.PanelBackground;
            if (go.GetComponent<UIPanelHost>() == null)
                go.AddComponent<UIPanelHost>();
            return go.transform;
        }

        internal static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Object.Destroy(child);
                else
                    Object.DestroyImmediate(child);
            }
        }

        internal static TMP_Text CreateLabel(Transform parent, string name, string text, int fontSize, Color color,
            FontStyles style, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition,
            Vector2 sizeDelta, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;

            var label = go.GetComponent<TMP_Text>();
            var font = TmpFont;
            if (font != null) label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            UiTypography.ApplyFontTo(label);
            return label;
        }

        internal static TMP_InputField CreateInput(Transform parent, string name, string placeholder,
            Vector2 anchoredPosition, Vector2 size)
        {
            var field = CreateInputCore(parent, name, placeholder);
            PlaceCenter(field.GetComponent<RectTransform>(), anchoredPosition, size);
            return field;
        }

        /// <summary>Input field for Horizontal/Vertical layout groups (stretch anchors, no fixed position).</summary>
        internal static TMP_InputField CreateLayoutInput(Transform parent, string name, string placeholder,
            float preferredHeight = 32f)
        {
            var field = CreateInputCore(parent, name, placeholder);
            var rt = field.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var le = field.gameObject.GetComponent<LayoutElement>() ?? field.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.flexibleHeight = 0f;
            le.minHeight = preferredHeight - 2f;
            le.preferredHeight = preferredHeight;
            return field;
        }

        private static TMP_InputField CreateInputCore(Transform parent, string name, string placeholder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(TMP_InputField));
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.color = UiTheme.InputBackground;

            var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(go.transform, false);
            var textAreaRt = textArea.GetComponent<RectTransform>();
            Stretch(textAreaRt, 8f, 6f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(textArea.transform, false);
            Stretch(textGo.GetComponent<RectTransform>(), 4f, 2f);
            var text = textGo.GetComponent<TMP_Text>();
            if (TmpFont != null) text.font = TmpFont;
            UiTypography.ApplyInputText(text, false);

            var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            phGo.transform.SetParent(textArea.transform, false);
            Stretch(phGo.GetComponent<RectTransform>(), 4f, 2f);
            var ph = phGo.GetComponent<TMP_Text>();
            if (TmpFont != null) ph.font = TmpFont;
            ph.text = placeholder;
            UiTypography.ApplyInputText(ph, true);
            ph.raycastTarget = false;

            var field = go.GetComponent<TMP_InputField>();
            field.textViewport = textAreaRt;
            field.textComponent = text;
            field.placeholder = ph;
            field.targetGraphic = img;
            field.fontAsset = TmpFont;
            field.pointSize = UiTypography.Input;
            return field;
        }

        internal static Button CreateButton(Transform parent, string name, string label,
            Vector2 anchoredPosition, Vector2 size)
        {
            var button = CreateButtonCore(parent, name, label);
            PlaceCenter(button.GetComponent<RectTransform>(), anchoredPosition, size);
            return button;
        }

        internal const string PmChannelListName = "PmChannelList";
        internal const string PmChannelBackdropName = "PmChannelBackdrop";
        internal const string PmChannelFieldHitName = "FieldHit";
        internal const string WhisperDropdownTemplateName = "WhisperDropdownTemplate";
        private const float DropdownListPadding = 4f;
        private const float DropdownOptionHeight = 28f;
        internal const int PmChannelListSortOrder = 30100;

        internal static void ConfigureDropdownFieldLayout(RectTransform rt, float preferredHeight = 28f)
        {
            StretchForLayoutGroup(rt);
            var le = rt.gameObject.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;
            le.minWidth = 160f;
            le.preferredWidth = 220f;
            le.minHeight = preferredHeight;
            le.preferredHeight = preferredHeight;
        }

        internal static void ConfigureDropdownOverlay(RectTransform rt)
        {
            var le = rt.gameObject.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
        }

        internal static TMP_Text CreateDropdownCaption(Transform root, string initialText = null)
        {
            var captionAreaGo = new GameObject("CaptionArea", typeof(RectTransform), typeof(RectMask2D));
            captionAreaGo.transform.SetParent(root, false);
            var captionAreaRt = captionAreaGo.GetComponent<RectTransform>();
            captionAreaRt.anchorMin = Vector2.zero;
            captionAreaRt.anchorMax = Vector2.one;
            captionAreaRt.offsetMin = new Vector2(8f, 2f);
            captionAreaRt.offsetMax = new Vector2(-24f, -2f);
            ConfigureDropdownOverlay(captionAreaRt);

            var labelGo = new GameObject("DropdownCaption", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(captionAreaGo.transform, false);
            Stretch(labelGo.GetComponent<RectTransform>(), 0f, 0f);
            var caption = labelGo.GetComponent<TMP_Text>();
            caption.text = initialText ?? UiCopy.ChatPmGlobalOption;
            caption.raycastTarget = false;
            UiTypography.ApplyChatDropdownLabel(caption);
            return caption;
        }

        internal static void CreateDropdownArrow(Transform root)
        {
            var arrowGo = new GameObject("Arrow", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            arrowGo.transform.SetParent(root, false);
            var arrowRt = arrowGo.GetComponent<RectTransform>();
            arrowRt.anchorMin = new Vector2(1f, 0.5f);
            arrowRt.anchorMax = new Vector2(1f, 0.5f);
            arrowRt.pivot = new Vector2(1f, 0.5f);
            arrowRt.sizeDelta = new Vector2(20f, 20f);
            arrowRt.anchoredPosition = new Vector2(-6f, 0f);
            var arrow = arrowGo.GetComponent<TMP_Text>();
            arrow.text = "v";
            arrow.fontSize = 14f;
            arrow.alignment = TextAlignmentOptions.Center;
            arrow.color = UiTheme.TextSubtitle;
            arrow.raycastTarget = false;
            UiTypography.ApplyFontTo(arrow);
            ConfigureDropdownOverlay(arrowRt);
        }

        internal static (RectTransform list, RectTransform content) CreateDropdownPopupList(Transform root,
            string listName, float defaultHeight = 64f)
        {
            var listGo = new GameObject(listName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(RectMask2D));
            listGo.transform.SetParent(root, false);
            listGo.SetActive(false);
            var listRt = listGo.GetComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0f, 1f);
            listRt.anchorMax = new Vector2(1f, 1f);
            listRt.pivot = new Vector2(0.5f, 0f);
            listRt.anchoredPosition = new Vector2(0f, -2f);
            listRt.sizeDelta = new Vector2(0f, defaultHeight);
            ConfigureDropdownOverlay(listRt);
            var listBg = listGo.GetComponent<Image>();
            listBg.color = UiTheme.InputBackground;
            listBg.raycastTarget = false;

            var listCanvas = listGo.AddComponent<Canvas>();
            listCanvas.overrideSorting = true;
            listCanvas.sortingOrder = PmChannelListSortOrder;
            UiCanvasFix.ApplyCanvasSettings(listCanvas);
            listGo.AddComponent<GraphicRaycaster>();

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentGo.transform.SetParent(listGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            Stretch(contentRt, DropdownListPadding, DropdownListPadding);
            var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 0f;
            vlg.padding = new RectOffset(0, 0, 0, 0);

            return (listRt, contentRt);
        }

        /// <summary>Button-based PM channel selector for ChatPmRow.</summary>
        internal static PmChannelSelector CreatePmChannelSelector(Transform pmRow, Transform chatPanel,
            float preferredHeight = 28f)
        {
            var root = new GameObject("PmChannelSelector", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(PmChannelSelector));
            root.transform.SetParent(pmRow, false);
            var rt = root.GetComponent<RectTransform>();
            ConfigureDropdownFieldLayout(rt, preferredHeight);

            var bg = root.GetComponent<Image>();
            bg.color = UiTheme.InputBackground;
            bg.raycastTarget = false;

            var hitGo = new GameObject(PmChannelFieldHitName, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            hitGo.transform.SetParent(root.transform, false);
            Stretch(hitGo.GetComponent<RectTransform>(), 0f, 0f);
            var hitImg = hitGo.GetComponent<Image>();
            hitImg.color = UiTheme.InputBackground;
            var fieldButton = hitGo.GetComponent<Button>();
            fieldButton.targetGraphic = hitImg;

            var caption = CreateDropdownCaption(root.transform);
            CreateDropdownArrow(root.transform);
            var (listRt, contentRt) = CreateDropdownPopupList(root.transform, PmChannelListName);

            var selector = root.GetComponent<PmChannelSelector>();
            selector.BindRuntime(fieldButton, caption, listRt, contentRt, chatPanel);
            return selector;
        }

        internal static Button CreatePmChannelOptionButton(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, DropdownOptionHeight);

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = DropdownOptionHeight;
            le.preferredHeight = DropdownOptionHeight;
            le.flexibleWidth = 1f;

            var img = go.GetComponent<Image>();
            img.color = UiTheme.InputBackground;
            img.raycastTarget = true;

            var labelGo = new GameObject("DropdownItemText", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            Stretch(labelGo.GetComponent<RectTransform>(), 10f, 2f);
            var text = labelGo.GetComponent<TMP_Text>();
            text.text = label;
            text.raycastTarget = false;
            UiTypography.ApplyChatDropdownItem(text);

            var button = go.GetComponent<Button>();
            button.targetGraphic = img;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);
            button.colors = colors;
            return button;
        }

        internal static void StretchForLayoutGroup(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        internal static void ConfigureVerticalLayoutChild(GameObject go, float preferredHeight, float flexibleHeight = 0f)
        {
            var rt = go.GetComponent<RectTransform>();
            StretchForLayoutGroup(rt);
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            if (preferredHeight > 0f)
            {
                le.preferredHeight = preferredHeight;
                le.minHeight = preferredHeight;
            }

            le.flexibleHeight = flexibleHeight;
            le.flexibleWidth = 0f;
        }

        internal static Button CreateLayoutButton(Transform parent, string name, string label, float width = 72f,
            float height = 34f)
        {
            var button = CreateButtonCore(parent, name, label);
            var rt = button.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var le = button.gameObject.GetComponent<LayoutElement>() ?? button.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 0f;
            le.minWidth = width - 8f;
            le.preferredWidth = width;
            le.minHeight = height - 2f;
            le.preferredHeight = height;
            return button;
        }

        private static Button CreateButtonCore(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.color = UiTheme.ButtonBackground;

            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(go.transform, false);
            Stretch(txtGo.GetComponent<RectTransform>(), 10f, 6f);
            var txt = txtGo.GetComponent<TMP_Text>();
            txt.text = label;
            if (TmpFont != null) txt.font = TmpFont;
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

        internal static Button CreateCharacterSlotButton(Transform parent, int index, CharacterSelectUI ui)
        {
            var go = new GameObject($"Char_{index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button));
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.color = UiTheme.ButtonBackground;

            var numGo = new GameObject("SlotNumber", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            numGo.transform.SetParent(go.transform, false);
            var numRt = numGo.GetComponent<RectTransform>();
            numRt.anchorMin = new Vector2(0f, 0.42f);
            numRt.anchorMax = new Vector2(1f, 1f);
            numRt.offsetMin = new Vector2(4f, 0f);
            numRt.offsetMax = new Vector2(-4f, -2f);
            var num = numGo.GetComponent<TMP_Text>();
            num.text = index.ToString();
            if (TmpFont != null) num.font = TmpFont;
            num.fontSize = UiTypography.Title;
            num.fontStyle = FontStyles.Bold;
            num.alignment = TextAlignmentOptions.Center;
            num.color = UiTheme.TitleAccent;
            num.raycastTarget = false;

            var ownerGo = new GameObject("SlotOwner", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            ownerGo.transform.SetParent(go.transform, false);
            var ownerRt = ownerGo.GetComponent<RectTransform>();
            ownerRt.anchorMin = new Vector2(0f, 0f);
            ownerRt.anchorMax = new Vector2(1f, 0.42f);
            ownerRt.offsetMin = new Vector2(4f, 2f);
            ownerRt.offsetMax = new Vector2(-4f, 0f);
            var owner = ownerGo.GetComponent<TMP_Text>();
            owner.text = "FREE";
            if (TmpFont != null) owner.font = TmpFont;
            owner.fontSize = UiTypography.Caption;
            owner.alignment = TextAlignmentOptions.Center;
            owner.color = UiTheme.TextSubtitle;
            owner.textWrappingMode = TextWrappingModes.NoWrap;
            owner.overflowMode = TextOverflowModes.Ellipsis;
            owner.raycastTarget = false;

            var button = go.GetComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);
            button.colors = colors;

            var slot = go.AddComponent<CharacterSlotButton>();
            slot.Configure(ui, index);
            slot.BindLabels(num, owner, img);
            button.onClick.AddListener(slot.OnClick);
            return button;
        }

        internal static Button CreatePlayerChip(Transform parent, string label)
        {
            var go = new GameObject("PlayerChip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var layout = go.GetComponent<LayoutElement>();
            layout.minWidth = 56f;
            layout.preferredHeight = 26f;
            layout.flexibleWidth = 0f;

            var img = go.GetComponent<Image>();
            img.color = UiTheme.ButtonBackground;

            var txtGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(go.transform, false);
            Stretch(txtGo.GetComponent<RectTransform>(), 8f, 4f);
            var txt = txtGo.GetComponent<TMP_Text>();
            txt.text = label;
            txt.raycastTarget = false;
            if (TmpFont != null) txt.font = TmpFont;
            txt.fontSize = UiTypography.ChatChipCompact;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.textWrappingMode = TextWrappingModes.NoWrap;
            txt.overflowMode = TextOverflowModes.Ellipsis;

            var button = go.GetComponent<Button>();
            button.targetGraphic = img;
            return button;
        }

        internal static void EnsureMinHeight(RectTransform rt, float minHeight)
        {
            if (rt == null) return;
            var layout = rt.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = minHeight;
        }

        internal static void PlaceCenter(RectTransform rt, Vector2 anchoredPosition, Vector2 size)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
        }

        internal static void Stretch(RectTransform rt, float horizontalPadding, float verticalPadding)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(horizontalPadding, verticalPadding);
            rt.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
        }

        internal static void StretchAnchored(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        internal static void HideVersionMarker(Transform panel, int version)
        {
            var existing = panel.Find("UiBuildVersion");
            if (existing != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(existing.gameObject);
                else
                    Object.DestroyImmediate(existing.gameObject);
            }

            var versionGo = new GameObject("UiBuildVersion");
            versionGo.transform.SetParent(panel, false);
            versionGo.hideFlags = HideFlags.HideInHierarchy;
            var marker = versionGo.AddComponent<UiBuildVersionMarker>();
            marker.Version = version;
        }

        internal static bool VersionMatches(Transform panel, int version)
        {
            var marker = panel.Find("UiBuildVersion")?.GetComponent<UiBuildVersionMarker>();
            if (marker != null)
                return marker.Version == version;

            var legacy = panel.Find("UiBuildVersion")?.GetComponent<TMP_Text>();
            return legacy != null && legacy.text == version.ToString();
        }
    }
}
