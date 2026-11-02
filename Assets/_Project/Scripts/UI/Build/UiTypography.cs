using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>Readable TMP font sizes and styling (1280x720 reference).</summary>
    public static class UiTypography
    {
        public const int Title = 72;
        public const int Subtitle = 38;
        public const int Body = 38;
        public const int Button = 36;
        public const int Input = 34;
        public const int FieldLabel = 34;
        public const int Caption = 32;
        public const int ChatLog = 36;
        public const int MinimumReadable = 30;

        /// <summary>Compact sizes for the in-game chat corner panel (~30% screen width).</summary>
        public const int ChatTitleCompact = 18;
        public const int ChatMetaCompact = 16;
        public const int ChatLogCompact = 17;
        public const int ChatInputCompact = 17;
        public const int ChatChipCompact = 16;
        public const int ChatButtonCompact = 18;

        /// <summary>Canvas reference resolution — lower values yield larger on-screen UI.</summary>
        public static readonly Vector2 CanvasReferenceResolution = new Vector2(1280f, 720f);

        private static TMP_FontAsset _cachedDefaultFont;

        public static TMP_FontAsset DefaultFont
        {
            get
            {
                if (_cachedDefaultFont != null)
                    return _cachedDefaultFont;

                if (TMP_Settings.defaultFontAsset != null)
                    _cachedDefaultFont = TMP_Settings.defaultFontAsset;

                if (_cachedDefaultFont == null)
                    _cachedDefaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

#if UNITY_EDITOR
                if (_cachedDefaultFont == null)
                {
                    _cachedDefaultFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
                }
#endif

                return _cachedDefaultFont;
            }
        }

        public static void ApplyFontTo(TMP_Text text)
        {
            if (text == null) return;
            var font = DefaultFont;
            if (font == null) return;
            text.font = font;
            text.fontSharedMaterial = font.material;
        }

        private static void EnableWordWrapping(TMP_Text text)
        {
            text.textWrappingMode = TextWrappingModes.Normal;
        }

        public static void ApplyTitle(TMP_Text text)
        {
            if (text == null) return;
            text.font = DefaultFont;
            text.fontSize = Title;
            text.fontStyle = FontStyles.Bold;
            text.color = UiTheme.TitleAccent;
            text.alignment = TextAlignmentOptions.Center;
            EnableWordWrapping(text);
            text.overflowMode = TextOverflowModes.Overflow;
        }

        public static void ApplySubtitle(TMP_Text text)
        {
            if (text == null) return;
            text.font = DefaultFont;
            text.fontSize = Subtitle;
            text.fontStyle = FontStyles.Normal;
            text.color = UiTheme.TextSubtitle;
            text.alignment = TextAlignmentOptions.Center;
            EnableWordWrapping(text);
            text.overflowMode = TextOverflowModes.Overflow;
        }

        public static void ApplyBody(TMP_Text text, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            if (text == null) return;
            text.font = DefaultFont;
            text.fontSize = Body;
            text.fontStyle = FontStyles.Normal;
            text.color = UiTheme.TextPrimary;
            text.alignment = align;
            EnableWordWrapping(text);
            text.overflowMode = TextOverflowModes.Overflow;
            text.lineSpacing = 4f;
        }

        public static void ApplyCaption(TMP_Text text, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            if (text == null) return;
            text.font = DefaultFont;
            text.fontSize = Caption;
            text.fontStyle = FontStyles.Normal;
            text.color = UiTheme.TextSubtitle;
            text.alignment = align;
            EnableWordWrapping(text);
            text.overflowMode = TextOverflowModes.Overflow;
        }

        public static void ApplyFieldLabel(TMP_Text text)
        {
            if (text == null) return;
            text.font = DefaultFont;
            text.fontSize = FieldLabel;
            text.fontStyle = FontStyles.Bold;
            text.color = UiTheme.TextPrimary;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            EnableWordWrapping(text);
            text.overflowMode = TextOverflowModes.Overflow;
        }

        public static void ApplyButtonLabel(TMP_Text text)
        {
            if (text == null) return;
            text.font = DefaultFont;
            text.fontSize = Button;
            text.fontStyle = FontStyles.Bold;
            text.color = UiTheme.TextPrimary;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 22;
            text.fontSizeMax = Button;
            EnableWordWrapping(text);
            text.overflowMode = TextOverflowModes.Overflow;
        }

        public static void ApplyInputText(TMP_Text text, bool placeholder)
        {
            if (text == null) return;
            text.font = DefaultFont;
            text.fontSize = Input;
            text.fontStyle = FontStyles.Normal;
            text.color = placeholder ? UiTheme.Placeholder : UiTheme.TextPrimary;
            text.alignment = TextAlignmentOptions.MidlineLeft;
        }

        public static void ApplyChatTitle(TMP_Text text)
        {
            if (text == null) return;
            text.font = DefaultFont;
            text.fontSize = ChatTitleCompact;
            text.fontStyle = FontStyles.Bold;
            text.color = UiTheme.TitleAccent;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        public static void ApplyChatMeta(TMP_Text text, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            if (text == null) return;
            text.font = DefaultFont;
            text.fontSize = ChatMetaCompact;
            text.fontStyle = FontStyles.Normal;
            text.color = UiTheme.TextSubtitle;
            text.alignment = align;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        public static void ApplyChatSelfNick(TMP_Text text)
        {
            if (text == null) return;
            text.font = DefaultFont;
            text.fontSize = ChatMetaCompact;
            text.fontStyle = FontStyles.Bold;
            text.color = UiTheme.TextPrimary;
            text.alignment = TextAlignmentOptions.MidlineRight;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        public static void ApplyChatLog(TMP_Text text)
        {
            if (text == null) return;
            text.font = DefaultFont;
            text.fontSize = ChatLogCompact;
            text.fontStyle = FontStyles.Normal;
            text.color = UiTheme.TextPrimary;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.lineSpacing = 2f;
            text.richText = true;
            text.raycastTarget = false;
        }

        public static void ApplyChatDropdownLabel(TMP_Text text)
        {
            if (text == null) return;
            text.font = DefaultFont;
            text.fontSize = ChatInputCompact;
            text.fontStyle = FontStyles.Normal;
            text.color = UiTheme.TextPrimary;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        public static void ApplyChatDropdownItem(TMP_Text text)
        {
            ApplyChatDropdownLabel(text);
        }

        public static void ApplyPmChannelCaption(TMP_Text text)
        {
            ApplyChatDropdownLabel(text);
        }

        public static void ApplyPmChannelOption(TMP_Text text)
        {
            ApplyChatDropdownItem(text);
        }

        /// <summary>Re-apply PM channel button styles after roster refresh.</summary>
        public static void ReapplyPmChannelStyles(Transform chatPanel)
        {
            if (chatPanel == null) return;

            var selector = chatPanel.Find("ChatPmRow/PmChannelSelector");
            if (selector != null)
                ApplyPmChannelTextStyles(selector);
        }

        private static void ApplyPmChannelTextStyles(Transform root)
        {
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.gameObject.name == "Arrow")
                    continue;
                if (text.gameObject.name == "DropdownCaption" || text.gameObject.name == "PmChannelCaption")
                    ApplyPmChannelCaption(text);
                else if (text.gameObject.name == "DropdownItemText"
                         || text.gameObject.name.StartsWith("PmOption_")
                         || text.transform.parent?.name.StartsWith("PmOption_") == true)
                    ApplyPmChannelOption(text);
            }
        }

        private static void ApplyDropdownTextStyles(Transform root)
        {
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.gameObject.name == "Arrow")
                    continue;
                if (text.gameObject.name == "DropdownCaption")
                    ApplyChatDropdownLabel(text);
                else if (text.gameObject.name == "DropdownItemText")
                    ApplyChatDropdownItem(text);
            }
        }

        private static bool IsUnderWhisperDropdown(Transform t)
        {
            while (t != null)
            {
                if (t.name == "WhisperDropdown")
                    return true;
                t = t.parent;
            }

            return false;
        }

        public static void ApplyChatInput(TMP_Text text, bool placeholder)
        {
            if (text == null) return;
            text.font = DefaultFont;
            text.fontSize = ChatInputCompact;
            text.fontStyle = FontStyles.Normal;
            text.color = placeholder ? UiTheme.Placeholder : UiTheme.TextPrimary;
            text.alignment = TextAlignmentOptions.MidlineLeft;
        }

        public static void ApplyChatChipLabel(TMP_Text text)
        {
            if (text == null) return;
            text.font = DefaultFont;
            text.fontSize = ChatChipCompact;
            text.fontStyle = FontStyles.Bold;
            text.color = UiTheme.TextPrimary;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        public static void StyleChatSendButton(Button button)
        {
            if (button == null) return;
            var img = button.targetGraphic as Image;
            if (img != null) img.color = UiTheme.ButtonBackground;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = UiTheme.ButtonHighlighted;
            colors.pressedColor = UiTheme.ButtonPressed;
            button.colors = colors;
            var label = button.GetComponentInChildren<TMP_Text>();
            if (label == null) return;
            label.font = DefaultFont;
            label.fontSize = ChatButtonCompact;
            label.fontStyle = FontStyles.Bold;
            label.color = UiTheme.TextPrimary;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14;
            label.fontSizeMax = ChatButtonCompact;
        }

        public static void StyleButtonCompactChip(Button button)
        {
            if (button == null) return;
            var img = button.targetGraphic as Image;
            if (img != null) img.color = UiTheme.ButtonBackground;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = UiTheme.ButtonHighlighted;
            colors.pressedColor = UiTheme.ButtonPressed;
            button.colors = colors;
            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) ApplyChatChipLabel(label);
        }

        public static void StyleButton(Button button)
        {
            if (button == null) return;
            var img = button.targetGraphic as Image;
            if (img != null) img.color = UiTheme.ButtonBackground;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = UiTheme.ButtonHighlighted;
            colors.pressedColor = UiTheme.ButtonPressed;
            colors.selectedColor = UiTheme.ButtonHighlighted;
            colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.65f);
            button.colors = colors;
            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) ApplyButtonLabel(label);
            var legacyLabel = button.GetComponentInChildren<Text>();
            if (legacyLabel != null) ApplyLegacyButtonLabel(legacyLabel);
        }

        public static void ApplyHierarchy(Transform root)
        {
            if (root == null) return;

            UiLayoutRuntimeFix.ApplyIfNeeded(root);
            UiCanvasFix.EnsureReadableCanvas(root);

            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                var go = text.gameObject;
                if (go.name is "UiBuildVersion" or "MenuUiVersion")
                    continue;
                if ((go.hideFlags & HideFlags.HideInHierarchy) != 0)
                    continue;

                var parent = go.transform.parent;

                if (go.name == "Title")
                {
                    ApplyTitle(text);
                    continue;
                }

                if (go.name == "Subtitle")
                {
                    ApplySubtitle(text);
                    continue;
                }

                if (go.name == "SessionStatus")
                {
                    text.fontSize = Caption;
                    text.enableAutoSizing = true;
                    text.fontSizeMin = 16f;
                    text.fontSizeMax = 22f;
                    text.fontStyle = FontStyles.Normal;
                    text.alignment = TextAlignmentOptions.Center;
                    text.textWrappingMode = TextWrappingModes.Normal;
                    text.overflowMode = TextOverflowModes.Ellipsis;
                    text.raycastTarget = false;
                    continue;
                }

                if (go.name == "EmptyHint")
                {
                    text.fontSize = 18f;
                    text.enableAutoSizing = true;
                    text.fontSizeMin = 14f;
                    text.fontSizeMax = 18f;
                    text.color = UiTheme.TextSubtitle;
                    text.alignment = TextAlignmentOptions.TopLeft;
                    text.textWrappingMode = TextWrappingModes.Normal;
                    text.overflowMode = TextOverflowModes.Ellipsis;
                    text.raycastTarget = false;
                    continue;
                }

                if (go.name == "PauseTitle")
                {
                    text.fontSize = 42f;
                    text.fontStyle = FontStyles.Bold;
                    text.color = UiTheme.TitleAccent;
                    text.alignment = TextAlignmentOptions.Center;
                    text.textWrappingMode = TextWrappingModes.NoWrap;
                    text.overflowMode = TextOverflowModes.Ellipsis;
                    text.raycastTarget = false;
                    continue;
                }

                if (go.name == "PauseControls" || go.name == "Controls")
                {
                    text.fontSize = 18f;
                    text.enableAutoSizing = true;
                    text.fontSizeMin = 14f;
                    text.fontSizeMax = 18f;
                    text.color = UiTheme.TextSubtitle;
                    text.alignment = TextAlignmentOptions.Center;
                    text.textWrappingMode = TextWrappingModes.Normal;
                    text.overflowMode = TextOverflowModes.Ellipsis;
                    text.raycastTarget = false;
                    continue;
                }

                if (parent != null && parent.GetComponent<Button>() != null)
                {
                    if (go.name is "SlotNumber" or "SlotOwner")
                    {
                        text.raycastTarget = false;
                        continue;
                    }

                    if (parent.name == "PlayerChip")
                    {
                        ApplyChatChipLabel(text);
                        continue;
                    }

                    if (parent.name == "ChatSend")
                    {
                        ApplyChatChipLabel(text);
                        continue;
                    }

                    // Pause menu buttons use compact labels inside a fixed 56px row.
                    if ((parent.name is "BtnResume" or "BtnLeave") &&
                        parent.parent != null && parent.parent.name == "Panel" &&
                        parent.parent.parent != null && parent.parent.parent.name == "PauseOverlay")
                    {
                        text.fontSize = 28f;
                        text.alignment = TextAlignmentOptions.Center;
                        text.overflowMode = TextOverflowModes.Ellipsis;
                        text.raycastTarget = false;
                        continue;
                    }

                    ApplyButtonLabel(text);
                    continue;
                }

                if (parent != null && parent.GetComponent<TMP_InputField>() != null)
                {
                    var chatInput = parent.name is "ChatInput" or "WhisperNick";
                    if (chatInput)
                        ApplyChatInput(text, go.name == "Placeholder");
                    else
                        ApplyInputText(text, go.name == "Placeholder");
                    continue;
                }

                switch (go.name)
                {
                    case "PlayerList":
                        ApplyBody(text, TextAlignmentOptions.TopLeft);
                        break;
                    case "Meta":
                    case "Event":
                    case "KD":
                    case "Bot":
                        text.fontSize = ChatMetaCompact;
                        text.enableAutoSizing = true;
                        text.fontSizeMin = 14f;
                        text.fontSizeMax = ChatMetaCompact;
                        text.textWrappingMode = TextWrappingModes.NoWrap;
                        text.overflowMode = TextOverflowModes.Ellipsis;
                        text.raycastTarget = false;
                        break;
                    case "ChatLog":
                        ApplyChatLog(text);
                        break;
                    case "ChatTitle":
                        ApplyChatTitle(text);
                        break;
                    case "ChatSelfNick":
                        ApplyChatSelfNick(text);
                        break;
                    case "ChatRosterTitle":
                        ApplyChatMeta(text);
                        break;
                    case "Status":
                    case "CharStatus":
                        ApplyCaption(text, TextAlignmentOptions.Center);
                        break;
                    case "WhisperLabel":
                        ApplyChatMeta(text);
                        break;
                    case "DropdownCaption":
                        ApplyChatDropdownLabel(text);
                        break;
                    case "DropdownItemText":
                        ApplyChatDropdownItem(text);
                        break;
                    default:
                        if (IsUnderWhisperDropdown(go.transform))
                        {
                            if (go.name == "DropdownItemText")
                                ApplyChatDropdownItem(text);
                            else
                                ApplyChatDropdownLabel(text);
                        }
                        else if (go.name.EndsWith("Label"))
                            ApplyFieldLabel(text);
                        else if (text.fontSize < MinimumReadable)
                            ApplyBody(text, text.alignment);
                        break;
                }
            }

            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.name == "ChatSend")
                    StyleChatSendButton(button);
                else if (button.name == "PlayerChip")
                    StyleButtonCompactChip(button);
                else
                    StyleButton(button);
            }

            ApplyLegacyHierarchy(root);

            foreach (var scaler in root.GetComponentsInChildren<CanvasScaler>(true))
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = CanvasReferenceResolution;
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        private static void ApplyLegacyHierarchy(Transform root)
        {
            foreach (var text in root.GetComponentsInChildren<Text>(true))
            {
                var go = text.gameObject;
                var parent = go.transform.parent;

                if (go.name == "Title")
                {
                    ApplyLegacyTitle(text);
                    continue;
                }

                if (go.name == "Subtitle")
                {
                    ApplyLegacySubtitle(text);
                    continue;
                }

                if (go.name == "SessionStatus")
                {
                    text.fontSize = Caption;
                    text.fontStyle = FontStyle.Normal;
                    text.alignment = TextAnchor.MiddleCenter;
                    text.horizontalOverflow = HorizontalWrapMode.Wrap;
                    text.verticalOverflow = VerticalWrapMode.Truncate;
                    text.raycastTarget = false;
                    continue;
                }

                if (parent != null && parent.GetComponent<Button>() != null)
                {
                    ApplyLegacyButtonLabel(text);
                    continue;
                }

                if (parent != null && parent.GetComponent<InputField>() != null)
                {
                    ApplyLegacyInputText(text, go.name == "Placeholder");
                    continue;
                }

                switch (go.name)
                {
                    case "PlayerList":
                    case "ChatLog":
                        ApplyLegacyBody(text, TextAnchor.UpperLeft);
                        break;
                    case "Status":
                    case "CharStatus":
                        ApplyLegacyCaption(text, TextAnchor.MiddleCenter);
                        break;
                    default:
                        if (text.fontSize < MinimumReadable)
                            ApplyLegacyBody(text, text.alignment);
                        break;
                }
            }
        }

        private static Font LegacyFont =>
            Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        private static void ApplyLegacyTitle(Text text)
        {
            text.font = LegacyFont;
            text.fontSize = Title;
            text.fontStyle = FontStyle.Bold;
            text.color = UiTheme.TitleAccent;
            text.alignment = TextAnchor.MiddleCenter;
        }

        private static void ApplyLegacySubtitle(Text text)
        {
            text.font = LegacyFont;
            text.fontSize = Subtitle;
            text.color = UiTheme.TextSubtitle;
            text.alignment = TextAnchor.MiddleCenter;
        }

        private static void ApplyLegacyBody(Text text, TextAnchor anchor)
        {
            text.font = LegacyFont;
            text.fontSize = Body;
            text.color = UiTheme.TextPrimary;
            text.alignment = anchor;
            text.lineSpacing = 1.1f;
        }

        private static void ApplyLegacyCaption(Text text, TextAnchor anchor)
        {
            text.font = LegacyFont;
            text.fontSize = Caption;
            text.color = UiTheme.TextSubtitle;
            text.alignment = anchor;
        }

        private static void ApplyLegacyButtonLabel(Text text)
        {
            text.font = LegacyFont;
            text.fontSize = Button;
            text.fontStyle = FontStyle.Bold;
            text.color = UiTheme.TextPrimary;
            text.alignment = TextAnchor.MiddleCenter;
        }

        private static void ApplyLegacyInputText(Text text, bool placeholder)
        {
            text.font = LegacyFont;
            text.fontSize = Input;
            text.fontStyle = placeholder ? FontStyle.Italic : FontStyle.Normal;
            text.color = placeholder ? UiTheme.Placeholder : UiTheme.TextPrimary;
            text.alignment = TextAnchor.MiddleLeft;
        }
    }
}
