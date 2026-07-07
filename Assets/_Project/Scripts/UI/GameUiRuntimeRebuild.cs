using FusionMultiplayer.Chat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>Rebuilds game HUD (character select + chat) with separated screen regions.</summary>
    public static class GameUiRuntimeRebuild
    {
        private const int BuildVersion = 29;

        /// <summary>Bottom-left chat panel height (screen fraction). Was 0.24; +30%.</summary>
        private const float ChatPanelAnchorYMax = 0.312f;

        public static int CurrentBuildVersion => BuildVersion;

        public static void EnsureBuilt(Transform canvas)
        {
            if (canvas == null)
                return;

            var charUi = canvas.GetComponent<CharacterSelectUI>();
            var chatUi = canvas.GetComponent<ChatUI>();
            if (charUi == null && chatUi == null)
                return;

            UiCanvasFix.EnsureReadableCanvas(canvas);
            UiRuntimeBuildKit.EnsureCanvasScaler(canvas);

            var canvasComp = canvas.GetComponent<Canvas>();
            if (canvasComp != null)
                canvasComp.sortingOrder = 50;

            var charPanel = canvas.Find("CharacterSelectPanel");
            if (charPanel != null && !NeedsCharacterRebuild(charPanel))
            {
                RebindExisting(canvas, charPanel, charUi, chatUi);
                return;
            }

            PurgeLegacyGameHud(canvas);

            charPanel = BuildCharacterSelect(canvas, charUi);
            var chatPanel = BuildChatPanel(canvas);

            var buttons = BindCharacterButtons(charPanel, charUi);
            var status = charPanel.Find("CharStatus")?.GetComponent<TMP_Text>();
            charUi?.BindRuntimeTmp(buttons, status, charPanel.gameObject);

            BindChatPanel(chatPanel, chatUi);
            UiRuntimeBuildKit.HideVersionMarker(charPanel, BuildVersion);
            UiRuntimeBuildKit.HideVersionMarker(chatPanel, BuildVersion);
            UiTypography.ApplyHierarchy(canvas);
            UiTypography.ReapplyPmChannelStyles(chatPanel);
        }

        private static void RebindExisting(Transform canvas, Transform charPanel, CharacterSelectUI charUi,
            ChatUI chatUi)
        {
            var buttons = BindCharacterButtons(charPanel, charUi);
            var status = charPanel.Find("CharStatus")?.GetComponent<TMP_Text>();
            charUi?.BindRuntimeTmp(buttons, status, charPanel.gameObject);

            var chatPanel = canvas.Find("ChatPanel");
            if (NeedsChatRebuild(chatPanel))
            {
                DestroyWhisperDropdownTemplate(canvas);
                if (chatPanel != null)
                    DestroyPanel(chatPanel);
                chatPanel = BuildChatPanel(canvas);
                UiRuntimeBuildKit.HideVersionMarker(chatPanel, BuildVersion);
            }

            if (chatPanel != null)
            {
                BindChatPanel(chatPanel, chatUi);
                UiTypography.ReapplyPmChannelStyles(chatPanel);
            }
        }

        private static Button[] BindCharacterButtons(Transform charPanel, CharacterSelectUI charUi)
        {
            var buttons = new Button[10];
            for (var i = 0; i < 10; i++)
            {
                var b = charPanel.Find($"Char_{i}")?.GetComponent<Button>();
                if (b == null) continue;
                buttons[i] = b;
                var slot = b.GetComponent<CharacterSlotButton>() ?? b.gameObject.AddComponent<CharacterSlotButton>();
                slot.Configure(charUi, i);
                TMP_Text num = null;
                TMP_Text owner = null;
                foreach (var t in b.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (t.gameObject.name == "SlotNumber") num = t;
                    if (t.gameObject.name == "SlotOwner") owner = t;
                }

                if (num != null) num.raycastTarget = false;
                if (owner != null) owner.raycastTarget = false;

                slot.BindLabels(num, owner, b.targetGraphic as Image);
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(slot.OnClick);
            }

            return buttons;
        }

        private static void BindChatPanel(Transform chatPanel, ChatUI chatUi)
        {
            if (chatUi == null || chatPanel == null) return;
            var chatInput = chatPanel.Find("ChatComposeRow/ChatInput")?.GetComponent<TMP_InputField>()
                ?? chatPanel.Find("ChatInput")?.GetComponent<TMP_InputField>();
            var chatSend = chatPanel.Find("ChatComposeRow/ChatSend")?.GetComponent<Button>()
                ?? chatPanel.Find("ChatSend")?.GetComponent<Button>();
            var chatLog = chatPanel.Find("ChatLogScroll/Viewport/Content/ChatLog")?.GetComponent<TMP_Text>()
                ?? chatPanel.Find("ChatLogScroll/Viewport/ChatLog")?.GetComponent<TMP_Text>()
                ?? chatPanel.Find("ChatLog")?.GetComponent<TMP_Text>();
            var pmChannel = chatPanel.Find("ChatPmRow/PmChannelSelector")?.GetComponent<PmChannelSelector>();
            var selfNick = chatPanel.Find("ChatHeaderBar/ChatSelfNick")?.GetComponent<TMP_Text>()
                ?? chatPanel.Find("ChatSelfNick")?.GetComponent<TMP_Text>();
            var logScroll = chatPanel.Find("ChatLogScroll")?.GetComponent<ScrollRect>();
            var whisperLabel = chatPanel.Find("ChatPmRow/WhisperLabel")?.GetComponent<TMP_Text>()
                ?? chatPanel.Find("WhisperLabel")?.GetComponent<TMP_Text>();
            var pmRow = chatPanel.Find("ChatPmRow");
            chatUi.BindRuntimeTmp(chatInput, chatSend, chatLog, pmChannel, selfNick, logScroll, whisperLabel, pmRow);
            ChatDiagnostics.LogBind(
                BuildVersion,
                chatLog != null,
                chatInput != null,
                chatSend != null,
                pmChannel != null,
                chatLog != null ? GetTransformPath(chatLog.transform) : "null");
        }

        private static string GetTransformPath(Transform t)
        {
            if (t == null) return "null";
            var path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }

            return path;
        }

        private static bool NeedsCharacterRebuild(Transform charPanel)
        {
            if (!UiRuntimeBuildKit.VersionMatches(charPanel, BuildVersion))
                return true;

            if (charPanel.Find("SlotGrid") == null)
                return true;

            var panelRt = charPanel as RectTransform;
            return panelRt == null || panelRt.anchorMin.y < 0.55f;
        }

        private static bool NeedsChatRebuild(Transform chatPanel)
        {
            if (chatPanel == null || chatPanel.Find("ChatHeaderBar") == null)
                return true;

            if (!UiRuntimeBuildKit.VersionMatches(chatPanel, BuildVersion))
                return true;

            if (chatPanel.Find("ChatLogScroll") == null || chatPanel.Find("ChatPmRow") == null)
                return true;

            if (chatPanel.Find("ChatLogScroll/Viewport/Content") == null)
                return true;

            if (chatPanel.Find("ChatPmRow/PmChannelSelector") == null)
                return true;

            if (chatPanel.Find("ChatPmRow/PmChannelSelector/PmChannelTrigger") != null)
                return true;

            if (chatPanel.Find("ChatPmRow/PmChannelSelector/DropdownCaption") == null)
                return true;

            var selector = chatPanel.Find("ChatPmRow/PmChannelSelector");
            if (selector?.GetComponent<Button>() != null)
                return true;

            if (chatPanel.Find($"ChatPmRow/PmChannelSelector/{UiRuntimeBuildKit.PmChannelFieldHitName}") == null)
                return true;

            if (chatPanel.Find(UiRuntimeBuildKit.PmChannelBackdropName) != null)
                return true;

            if (chatPanel.Find("ChatPmRow/WhisperDropdown") != null)
                return true;

            var pmListPath = $"ChatPmRow/PmChannelSelector/{UiRuntimeBuildKit.PmChannelListName}";
            if (chatPanel.Find(pmListPath) == null)
                return true;

            if (chatPanel.Find(UiRuntimeBuildKit.PmChannelListName) != null)
                return true;

            var pmList = chatPanel.Find(pmListPath);
            if (pmList?.GetComponent<RectMask2D>() == null)
                return true;

            var listCanvas = pmList?.GetComponent<Canvas>();
            if (listCanvas == null || listCanvas.sortingOrder != UiRuntimeBuildKit.PmChannelListSortOrder)
                return true;

            var canvasRoot = chatPanel.parent;
            if (canvasRoot?.Find(UiRuntimeBuildKit.WhisperDropdownTemplateName) != null)
                return true;

            if (chatPanel.Find("ChatLogScroll/Viewport/Content/ChatLog")?.GetComponent<ContentSizeFitter>() != null)
                return true;

            if (chatPanel.Find("ChatLogScroll/Viewport/Content/ChatLog")?.GetComponent<LayoutElement>() == null)
                return true;

            if (chatPanel.Find("ChatLogScroll/Viewport")?.GetComponent<RectMask2D>() == null)
                return true;

            var logViewportImage = chatPanel.Find("ChatLogScroll/Viewport")?.GetComponent<Image>();
            if (logViewportImage != null && logViewportImage.raycastTarget)
                return true;

            var chatLogText = chatPanel.Find("ChatLogScroll/Viewport/Content/ChatLog")?.GetComponent<TMP_Text>();
            if (chatLogText != null && chatLogText.raycastTarget)
                return true;

            if (chatPanel.Find("ChatRosterScroll") != null || chatPanel.Find("ChatRosterTitle") != null)
                return true;

            return chatPanel.GetComponent<VerticalLayoutGroup>() == null;
        }

        private static bool NeedsRebuild(Transform charPanel)
        {
            if (NeedsCharacterRebuild(charPanel))
                return true;

            var chatPanel = charPanel.parent?.Find("ChatPanel");
            return NeedsChatRebuild(chatPanel);
        }

        private static void PurgeLegacyGameHud(Transform canvas)
        {
            DestroyWhisperDropdownTemplate(canvas);
            for (var i = canvas.childCount - 1; i >= 0; i--)
            {
                var child = canvas.GetChild(i);
                if (child.name == "CharacterSelectPanel" || child.name == "ChatPanel")
                    DestroyPanel(child);
            }
        }

        private static void DestroyWhisperDropdownTemplate(Transform canvas)
        {
            if (canvas == null) return;
            var template = canvas.Find(UiRuntimeBuildKit.WhisperDropdownTemplateName);
            if (template != null)
                DestroyPanel(template);

            var chatPanel = canvas.Find("ChatPanel");
            if (chatPanel == null) return;
            var list = chatPanel.Find(UiRuntimeBuildKit.PmChannelListName);
            if (list != null)
                DestroyPanel(list);
            var backdrop = chatPanel.Find(UiRuntimeBuildKit.PmChannelBackdropName);
            if (backdrop != null)
                DestroyPanel(backdrop);
        }

        private static void DestroyPanel(Transform panel)
        {
            if (panel == null) return;
            if (Application.isPlaying)
                Object.Destroy(panel.gameObject);
            else
                Object.DestroyImmediate(panel.gameObject);
        }

        private static Transform BuildCharacterSelect(Transform canvas, CharacterSelectUI charUi)
        {
            var panelGo = new GameObject("CharacterSelectPanel", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            panelGo.transform.SetParent(canvas, false);
            var panelRt = panelGo.GetComponent<RectTransform>();
            UiRegionLayout.StretchRegion(panelRt, 0f, 0.62f, 1f, 1f, 12f, 8f, 12f, 8f);
            panelGo.GetComponent<Image>().color = UiTheme.PanelBackground;

            var status = UiRuntimeBuildKit.CreateLabel(panelGo.transform, "CharStatus", UiCopy.CharacterSelectPrompt,
                UiTypography.Body, UiTheme.TextPrimary, FontStyles.Bold,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center);
            UiRegionLayout.StretchBand(status.rectTransform, 0.88f, 0.98f, 20f);
            UiTypography.ApplyBody(status, TextAlignmentOptions.Center);

            var gridGo = new GameObject("SlotGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            gridGo.transform.SetParent(panelGo.transform, false);
            var gridRt = gridGo.GetComponent<RectTransform>();
            UiRegionLayout.StretchBand(gridRt, 0.06f, 0.86f, 20f);

            var grid = gridGo.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.cellSize = new Vector2(108f, 72f);
            grid.spacing = new Vector2(10f, 8f);
            grid.padding = new RectOffset(8, 8, 4, 4);
            grid.childAlignment = TextAnchor.MiddleCenter;

            for (var i = 0; i < 10; i++)
            {
                var btn = UiRuntimeBuildKit.CreateCharacterSlotButton(gridGo.transform, i, charUi);
                var btnRt = (RectTransform)btn.transform;
                btnRt.sizeDelta = grid.cellSize;
            }

            return panelGo.transform;
        }

        private static Transform BuildChatPanel(Transform canvas)
        {
            var panelGo = new GameObject("ChatPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(VerticalLayoutGroup));
            panelGo.transform.SetParent(canvas, false);
            var panelRt = panelGo.GetComponent<RectTransform>();
            UiRegionLayout.StretchRegion(panelRt, 0f, 0f, 0.30f, ChatPanelAnchorYMax, 8f, 8f, 8f, 8f);
            panelGo.GetComponent<Image>().color = UiTheme.PanelBackground;

            var vlg = panelGo.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.spacing = 3f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            BuildChatHeaderBar(panelGo.transform);
            BuildChatLogScroll(panelGo.transform);
            BuildChatPmRow(panelGo.transform);
            BuildChatComposeRow(panelGo.transform);

            return panelGo.transform;
        }

        private static void BuildChatHeaderBar(Transform panel)
        {
            var barGo = new GameObject("ChatHeaderBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(HorizontalLayoutGroup));
            barGo.transform.SetParent(panel, false);
            UiRuntimeBuildKit.ConfigureVerticalLayoutChild(barGo, 22f);
            barGo.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.08f, 0.55f);

            var hlg = barGo.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(10, 10, 2, 2);
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            var titleGo = new GameObject("ChatTitle", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI), typeof(LayoutElement));
            titleGo.transform.SetParent(barGo.transform, false);
            titleGo.GetComponent<LayoutElement>().flexibleWidth = 0.35f;
            var title = titleGo.GetComponent<TMP_Text>();
            title.text = "CHAT";
            UiTypography.ApplyChatTitle(title);

            var selfGo = new GameObject("ChatSelfNick", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI), typeof(LayoutElement));
            selfGo.transform.SetParent(barGo.transform, false);
            selfGo.GetComponent<LayoutElement>().flexibleWidth = 0.65f;
            var self = selfGo.GetComponent<TMP_Text>();
            self.text = string.Format(UiCopy.ChatSelfNickFormat, "…");
            UiTypography.ApplyChatSelfNick(self);
        }

        private static void BuildChatPmRow(Transform panel)
        {
            var rowGo = new GameObject("ChatPmRow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(HorizontalLayoutGroup));
            rowGo.transform.SetParent(panel, false);
            UiRuntimeBuildKit.ConfigureVerticalLayoutChild(rowGo, 30f);

            rowGo.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.09f, 0.45f);

            var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(10, 10, 4, 4);
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var labelGo = new GameObject("WhisperLabel", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelGo.transform.SetParent(rowGo.transform, false);
            var labelLe = labelGo.GetComponent<LayoutElement>();
            labelLe.minWidth = 36f;
            labelLe.preferredWidth = 40f;
            labelLe.flexibleWidth = 0f;
            labelLe.minHeight = 26f;
            labelLe.preferredHeight = 28f;
            var label = labelGo.GetComponent<TMP_Text>();
            label.text = UiCopy.ChatWhisperToLabel;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            UiTypography.ApplyChatMeta(label);

            UiRuntimeBuildKit.CreatePmChannelSelector(rowGo.transform, panel, 24f);

            var spacerGo = new GameObject("PmRowSpacer", typeof(RectTransform), typeof(LayoutElement));
            spacerGo.transform.SetParent(rowGo.transform, false);
            var spacerLe = spacerGo.GetComponent<LayoutElement>();
            spacerLe.flexibleWidth = 1f;
            spacerLe.minWidth = 0f;

            LayoutRebuilder.ForceRebuildLayoutImmediate(rowGo.GetComponent<RectTransform>());
        }

        private static void BuildChatComposeRow(Transform panel)
        {
            var rowGo = new GameObject("ChatComposeRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowGo.transform.SetParent(panel, false);
            UiRuntimeBuildKit.ConfigureVerticalLayoutChild(rowGo, 30f);

            var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(0, 0, 0, 0);
            hlg.spacing = 6f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            var input = UiRuntimeBuildKit.CreateLayoutInput(rowGo.transform, "ChatInput", "Type a message…", 28f);
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.pointSize = UiTypography.ChatInputCompact;

            var send = UiRuntimeBuildKit.CreateLayoutButton(rowGo.transform, "ChatSend", "Send", 60f, 28f);
            UiTypography.StyleChatSendButton(send);
        }

        private static void BuildChatLogScroll(Transform panel)
        {
            var scrollGo = new GameObject("ChatLogScroll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(ScrollRect));
            scrollGo.transform.SetParent(panel, false);
            UiRuntimeBuildKit.ConfigureVerticalLayoutChild(scrollGo, 0f, 1f);
            var scrollBg = scrollGo.GetComponent<Image>();
            scrollBg.color = new Color(0.08f, 0.08f, 0.1f, 0.35f);
            scrollBg.raycastTarget = false;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = new Vector2(6f, 4f);
            viewportRt.offsetMax = new Vector2(-6f, -4f);
            var viewportImage = viewportGo.GetComponent<Image>();
            viewportImage.color = Color.clear;
            viewportImage.raycastTarget = false;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;
            var contentFitter = contentGo.GetComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var log = UiRuntimeBuildKit.CreateLabel(contentGo.transform, "ChatLog", string.Empty,
                UiTypography.ChatLogCompact, UiTheme.TextPrimary, FontStyles.Normal,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.TopLeft);
            log.rectTransform.anchorMin = new Vector2(0f, 1f);
            log.rectTransform.anchorMax = new Vector2(1f, 1f);
            log.rectTransform.pivot = new Vector2(0.5f, 1f);
            log.rectTransform.offsetMin = new Vector2(2f, 0f);
            log.rectTransform.offsetMax = new Vector2(-2f, 0f);
            var logLe = log.gameObject.AddComponent<LayoutElement>();
            logLe.minHeight = 24f;
            UiTypography.ApplyChatLog(log);
            log.raycastTarget = false;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.viewport = viewportRt;
            scroll.content = contentRt;
        }
    }
}
