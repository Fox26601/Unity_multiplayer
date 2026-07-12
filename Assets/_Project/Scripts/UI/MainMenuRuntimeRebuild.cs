using FusionMultiplayer.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>
    /// Builds a readable main-menu panel at runtime (TMP text, solid-color widgets, no legacy sprites).
    /// Assignment 3: landing + create/join pages with session dropdown.
    /// </summary>
    public static class MainMenuRuntimeRebuild
    {
        private const int MenuBuildVersion = 22;
        private const int MenuCompactText = 26;
        private const float FormLabelWidth = 168f;
        private const float LabelShareOfBlock = 0.32f;
        private const float GapShareOfBlock = 0.05f;

        private static TMP_FontAsset _tmpFont;

        public static void EnsureBuilt(Transform canvas)
        {
            if (canvas == null || canvas.GetComponent<MainMenuUI>() == null)
                return;

            EnsureCanvasScaler(canvas);
            UiCanvasFix.EnsureReadableCanvas(canvas);

            _tmpFont = UiTypography.DefaultFont;

            var panel = canvas.Find("Panel");
            ApplyPanelHygiene(panel);

            if (panel != null && !NeedsRebuild(panel))
                return;

            if (_tmpFont == null)
                Debug.LogWarning("[FusionMultiplayer] TMP font missing — run Tools → Fusion Multiplayer → Import TMP Essential Resources.");

            if (panel == null)
                panel = CreatePanel(canvas);
            else
                ClearChildren(panel);

            BuildContents(panel, canvas.GetComponent<MainMenuUI>());
        }

        /// <summary>Always run — even when NeedsRebuild skips full rebuild (main editor / edit-mode leftovers).</summary>
        private static void ApplyPanelHygiene(Transform panel)
        {
            if (panel == null)
                return;

            DisablePanelBackgroundRaycast(panel);
            DropdownOverlayRegistry.CloseAll();
            DestroyLegacyOverlayLayer(panel);
        }

        private static void DestroyLegacyOverlayLayer(Transform panel)
        {
            var overlay = panel.Find("DropdownOverlayLayer");
            if (overlay == null)
                return;

            DropdownOverlayRegistry.CleanupOverlayLayer(overlay);
            if (Application.isPlaying)
                Object.Destroy(overlay.gameObject);
            else
                Object.DestroyImmediate(overlay.gameObject);
        }

        private static void DisablePanelBackgroundRaycast(Transform panel)
        {
            var image = panel.GetComponent<Image>();
            if (image != null)
                image.raycastTarget = false;
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

            if (panel.Find("LandingPage") == null || panel.Find("CreatePage") == null || panel.Find("JoinPage") == null)
                return true;

            if (panel.Find("DropdownOverlayLayer") != null)
                return true;

            var panelImage = panel.GetComponent<Image>();
            if (panelImage != null && panelImage.raycastTarget)
                return true;

            if (panel.Find("CreatePage/FormRoot")?.GetComponent<VerticalLayoutGroup>() == null)
                return true;

            return panel.Find("CreatePage/FormRoot/CreateGameModeSelectorRow/StringDropdownSelector") == null;
        }

        private static Transform CreatePanel(Transform canvas)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvas, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(24f, 24f);
            rt.offsetMax = new Vector2(-24f, -24f);
            go.GetComponent<Image>().color = UiTheme.PanelBackground;
            go.GetComponent<Image>().raycastTarget = false;

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
            CreateBandLabel(panel, "Title", UiCopy.MainMenuTitle, UiTypography.Title, UiTheme.TitleAccent,
                FontStyles.Bold, 0.93f, 0.99f, TextAlignmentOptions.Center);
            UiTypography.ApplyTitle(panel.Find("Title")?.GetComponent<TMP_Text>());

            CreateBandLabel(panel, "Subtitle", UiCopy.MainMenuLandingSubtitle, UiTypography.Subtitle,
                UiTheme.TextSubtitle, FontStyles.Normal, 0.87f, 0.92f, TextAlignmentOptions.Center);
            var subtitle = panel.Find("Subtitle")?.GetComponent<TMP_Text>();
            if (subtitle != null)
            {
                subtitle.textWrappingMode = TextWrappingModes.Normal;
                subtitle.overflowMode = TextOverflowModes.Ellipsis;
            }

            var landingPage = CreatePageRoot(panel, "LandingPage");
            var createPage = CreatePageRoot(panel, "CreatePage");
            var joinPage = CreatePageRoot(panel, "JoinPage");

            // --- Landing ---
            var nick = CreateColumnField(landingPage, "NicknameField", UiCopy.NicknameLabel,
                UiCopy.NicknamePlaceholder, 0.58f, 0.74f);
            CreateColumnLabel(landingPage, "ColorLabel", UiCopy.PlayerColorLabel, 0.50f, 0.56f);
            var preview = CreateColorPreview(landingPage, 0.40f, 0.48f);
            var randomColor = CreateColumnButton(landingPage, "BtnRandomColor", UiCopy.RandomColorButton,
                0.40f, 0.48f, new Vector2(0.58f, 0.5f));
            var leaderboard = CreateColumnButton(landingPage, "BtnLeaderboard", UiCopy.MainMenuLeaderboard,
                0.32f, 0.38f, new Vector2(0.5f, 1f), fullWidth: true);
            var goCreate = CreateColumnButton(landingPage, "BtnGoCreate", UiCopy.MainMenuGoCreate,
                0.24f, 0.30f, new Vector2(0.5f, 1f), fullWidth: true);
            var goJoin = CreateColumnButton(landingPage, "BtnGoJoin", UiCopy.MainMenuGoJoin,
                0.16f, 0.22f, new Vector2(0.5f, 1f), fullWidth: true);
            var quickJoin = CreateColumnButton(landingPage, "BtnQuickJoin", UiCopy.MainMenuQuickJoin,
                0.08f, 0.14f, new Vector2(0.5f, 1f), fullWidth: true);
            var reconnect = CreateColumnButton(landingPage, "BtnReconnect", UiCopy.MainMenuReconnect,
                0.01f, 0.07f, new Vector2(0.5f, 1f), fullWidth: true);

            // --- Create ---
            var createForm = CreatePageContentRoot(createPage);
            var backCreate = CreateBackButtonRow(createForm, "BtnBack", UiCopy.MainMenuBack);
            CreatePageTitle(createForm, "CreateTitle", UiCopy.CreatePageTitle);
            var createMode = BuildFormDropdownRow(createForm, "CreateGameModeSelector", UiCopy.GameModeLabel);
            var createMap = BuildFormDropdownRow(createForm, "CreateMapSelector", UiCopy.MapFilterLabel);
            var createDifficulty = BuildFormDropdownRow(createForm, "CreateDifficultySelector", UiCopy.DifficultyLabel);
            var createMaxPlayers = BuildFormDropdownRow(createForm, "CreateMaxPlayersSelector", UiCopy.MaxPlayersLabel);
            var hiddenToggle = CreateFormToggleRow(createForm, "HiddenSessionToggle", UiCopy.HiddenSessionLabel);
            var modeHint = CreateFormHint(createForm, "ModeHint", string.Empty);
            var room = CreateFormInputRow(createForm, "RoomField", UiCopy.RoomLabel, UiCopy.RoomPlaceholder);
            var create = CreateFormPrimaryButton(createForm, "BtnCreate", UiCopy.CreateHostButton);

            // --- Join ---
            var joinForm = CreatePageContentRoot(joinPage);
            var backJoin = CreateBackButtonRow(joinForm, "BtnBack", UiCopy.MainMenuBack);
            CreatePageTitle(joinForm, "JoinTitle", UiCopy.JoinPageTitle);
            var joinMode = BuildFormDropdownRow(joinForm, "JoinGameModeSelector", UiCopy.GameModeLabel);
            var joinMap = BuildFormDropdownRow(joinForm, "JoinMapSelector", UiCopy.MapFilterLabel);
            var joinDifficulty = BuildFormDropdownRow(joinForm, "JoinDifficultySelector", UiCopy.DifficultyLabel);
            var (listContent, emptyHint) = CreateSessionListScrollLayout(joinForm);
            var (refresh, joinSession) = CreateJoinActionsRow(joinForm);

            LayoutRebuilder.ForceRebuildLayoutImmediate(createForm);
            LayoutRebuilder.ForceRebuildLayoutImmediate(joinForm);

            CreateBandLabel(panel, "SessionStatus", string.Empty, UiTypography.Body, UiTheme.StatusOk,
                FontStyles.Normal, 0.01f, 0.07f, TextAlignmentOptions.Center);

            var browser = menu.GetComponent<SessionBrowserUI>();
            if (browser == null)
                browser = menu.gameObject.AddComponent<SessionBrowserUI>();

            menu.BindRuntimeTmp(
                landingPage.gameObject,
                createPage.gameObject,
                joinPage.gameObject,
                nick,
                room,
                goCreate,
                goJoin,
                backCreate,
                backJoin,
                create,
                joinSession,
                preview,
                randomColor,
                browser,
                createMode,
                createMap,
                createDifficulty,
                createMaxPlayers,
                hiddenToggle,
                joinMode,
                joinMap,
                joinDifficulty,
                listContent,
                refresh,
                emptyHint,
                modeHint,
                quickJoin,
                reconnect,
                leaderboard);

            createPage.gameObject.SetActive(false);
            joinPage.gameObject.SetActive(false);
            landingPage.gameObject.SetActive(true);

            var versionGo = new GameObject("MenuUiVersion");
            versionGo.transform.SetParent(panel, false);
            versionGo.hideFlags = HideFlags.HideInHierarchy;
            var marker = versionGo.AddComponent<UiBuildVersionMarker>();
            marker.Version = MenuBuildVersion;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel as RectTransform);
        }

        private static RectTransform CreatePageRoot(Transform panel, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(panel, false);
            UiRegionLayout.StretchBand(go.GetComponent<RectTransform>(), 0.08f, 0.86f, 32f);
            return go.GetComponent<RectTransform>();
        }

        private static RectTransform CreateColumn(Transform parent, string name, float xMin, float xMax, float yMin,
            float yMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            UiRegionLayout.StretchRegion(rt, xMin, yMin, xMax, yMax, 8f, 4f, 8f, 4f);
            return rt;
        }

        private static void CreateBandLabel(Transform parent, string name, string text, int fontSize, Color color,
            FontStyles style, float yMin, float yMax, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            UiRegionLayout.StretchBand(go.GetComponent<RectTransform>(), yMin, yMax, 32f);

            var label = go.GetComponent<TMP_Text>();
            if (_tmpFont != null) label.font = _tmpFont;
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Truncate;
            label.raycastTarget = false;

            if (string.IsNullOrEmpty(text))
                go.SetActive(false);
        }

        private static TMP_Text CreateColumnLabel(Transform column, string name, string text, float yMin, float yMax,
            int fontSize = 0, FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RectMask2D),
                typeof(TextMeshProUGUI));
            go.transform.SetParent(column, false);
            UiRegionLayout.StretchBand(go.GetComponent<RectTransform>(), yMin, yMax, 4f);

            var label = go.GetComponent<TMP_Text>();
            if (_tmpFont != null) label.font = _tmpFont;
            label.text = text;
            label.fontSize = fontSize > 0 ? fontSize : MenuCompactText;
            label.fontStyle = style;
            label.color = UiTheme.TextPrimary;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }

        private static TMP_Text CreateWrappedHint(Transform column, string name, string text, float yMin, float yMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RectMask2D),
                typeof(TextMeshProUGUI));
            go.transform.SetParent(column, false);
            UiRegionLayout.StretchBand(go.GetComponent<RectTransform>(), yMin, yMax, 4f);

            var label = go.GetComponent<TMP_Text>();
            if (_tmpFont != null) label.font = _tmpFont;
            label.text = text;
            label.fontSize = MenuCompactText;
            label.fontStyle = FontStyles.Italic;
            label.color = UiTheme.TextSubtitle;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }

        private static void SplitBlock(float yMin, float yMax, out float labelMin, out float fieldMax)
        {
            var height = Mathf.Max(yMax - yMin, 0.001f);
            labelMin = yMax - height * LabelShareOfBlock;
            fieldMax = labelMin - height * GapShareOfBlock;
        }

        private static TMP_InputField CreateColumnField(Transform column, string fieldName, string labelText,
            string placeholder, float yMin, float yMax)
        {
            SplitBlock(yMin, yMax, out var labelMin, out var fieldMax);
            CreateColumnLabel(column, fieldName + "Label", labelText, labelMin, yMax);

            var fieldGo = new GameObject(fieldName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(TMP_InputField));
            fieldGo.transform.SetParent(column, false);
            UiRegionLayout.StretchBand(fieldGo.GetComponent<RectTransform>(), yMin, fieldMax, 4f);

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

        private static Image CreateColorPreview(Transform column, float yMin, float yMax)
        {
            var go = new GameObject("ColorPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(column, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, yMin);
            rt.anchorMax = new Vector2(0.12f, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = SessionData.Tint;
            return img;
        }

        private static Button CreateColumnButton(Transform column, string name, string label, float yMin, float yMax,
            Vector2 anchorX, bool fullWidth = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(column, false);
            var rt = go.GetComponent<RectTransform>();

            if (fullWidth)
            {
                UiRegionLayout.StretchBand(rt, yMin, yMax, 4f);
            }
            else
            {
                rt.anchorMin = new Vector2(anchorX.x - 0.22f, yMin);
                rt.anchorMax = new Vector2(anchorX.x + 0.22f, yMax);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            var img = go.GetComponent<Image>();
            img.color = UiTheme.ButtonBackground;

            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(go.transform, false);
            Stretch(txtGo.GetComponent<RectTransform>(), 8f, 6f);
            var txt = txtGo.GetComponent<TMP_Text>();
            txt.text = label;
            if (_tmpFont != null) txt.font = _tmpFont;
            txt.fontSize = fullWidth ? UiTypography.Button : MenuCompactText;
            txt.textWrappingMode = TextWrappingModes.NoWrap;
            txt.overflowMode = TextOverflowModes.Ellipsis;
            UiTypography.ApplyButtonLabel(txt);
            if (!fullWidth)
                txt.fontSize = MenuCompactText;

            var button = go.GetComponent<Button>();
            button.targetGraphic = img;
            StyleButton(button);
            return button;
        }

        private static RectTransform CreatePageContentRoot(RectTransform page)
        {
            var go = new GameObject("FormRoot", typeof(RectTransform), typeof(VerticalLayoutGroup));
            go.transform.SetParent(page, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 8, 8);
            vlg.spacing = 8f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            return rt;
        }

        private static RectTransform BuildFormRow(Transform parent, string name, float height)
        {
            var rowGo = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowGo.transform.SetParent(parent, false);
            UiRuntimeBuildKit.ConfigureVerticalLayoutChild(rowGo, height);

            var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            return rowGo.GetComponent<RectTransform>();
        }

        private static void AddFormRowLabel(RectTransform row, string text, float rowHeight)
        {
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelGo.transform.SetParent(row, false);
            var le = labelGo.GetComponent<LayoutElement>();
            le.minWidth = FormLabelWidth;
            le.preferredWidth = FormLabelWidth;
            le.flexibleWidth = 0f;
            le.minHeight = rowHeight - 4f;
            le.preferredHeight = rowHeight - 4f;

            var label = labelGo.GetComponent<TMP_Text>();
            if (_tmpFont != null) label.font = _tmpFont;
            label.text = text;
            label.fontSize = MenuCompactText;
            label.color = UiTheme.TextPrimary;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
        }

        private static StringDropdownSelector BuildFormDropdownRow(Transform formRoot, string rowName, string labelText,
            float height = 44f)
        {
            var row = BuildFormRow(formRoot, rowName + "Row", height);
            AddFormRowLabel(row, labelText, height);
            var selector = UiRuntimeBuildKit.CreateStringDropdownSelector(row, height - 4f);
            LayoutRebuilder.ForceRebuildLayoutImmediate(row);
            return selector;
        }

        private static Button CreateBackButtonRow(Transform formRoot, string name, string label)
        {
            var row = BuildFormRow(formRoot, name + "Row", 36f);
            var button = UiRuntimeBuildKit.CreateLayoutButton(row, name, label, 120f, 32f);
            StyleButton(button);
            return button;
        }

        private static void CreatePageTitle(Transform formRoot, string name, string text)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI),
                typeof(LayoutElement));
            go.transform.SetParent(formRoot, false);
            UiRuntimeBuildKit.ConfigureVerticalLayoutChild(go, 40f);

            var label = go.GetComponent<TMP_Text>();
            if (_tmpFont != null) label.font = _tmpFont;
            label.text = text;
            label.fontSize = UiTypography.Subtitle;
            label.fontStyle = FontStyles.Bold;
            label.color = UiTheme.TextPrimary;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
        }

        private static TMP_Text CreateFormHint(Transform formRoot, string name, string text)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI),
                typeof(LayoutElement));
            go.transform.SetParent(formRoot, false);
            UiRuntimeBuildKit.ConfigureVerticalLayoutChild(go, 48f, flexibleHeight: 1f);

            var label = go.GetComponent<TMP_Text>();
            if (_tmpFont != null) label.font = _tmpFont;
            label.text = text;
            label.fontSize = MenuCompactText;
            label.fontStyle = FontStyles.Italic;
            label.color = UiTheme.TextSubtitle;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }

        private static TMP_InputField CreateFormInputRow(Transform formRoot, string fieldName, string labelText,
            string placeholder, float height = 44f)
        {
            var row = BuildFormRow(formRoot, fieldName + "Row", height);
            AddFormRowLabel(row, labelText, height);

            var fieldGo = new GameObject(fieldName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(TMP_InputField), typeof(LayoutElement));
            fieldGo.transform.SetParent(row, false);
            UiRuntimeBuildKit.StretchForLayoutGroup(fieldGo.GetComponent<RectTransform>());
            var fieldLe = fieldGo.GetComponent<LayoutElement>();
            fieldLe.flexibleWidth = 1f;
            fieldLe.minWidth = 120f;
            fieldLe.minHeight = height - 4f;
            fieldLe.preferredHeight = height - 4f;

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
            LayoutRebuilder.ForceRebuildLayoutImmediate(row);
            return field;
        }

        private static Button CreateFormPrimaryButton(Transform formRoot, string name, string label)
        {
            var button = UiRuntimeBuildKit.CreateLayoutButton(formRoot, name, label, 240f, 52f);
            var le = button.GetComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.minWidth = 0f;
            StyleButton(button);
            return button;
        }

        private static Toggle CreateFormToggleRow(Transform formRoot, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Toggle), typeof(LayoutElement));
            go.transform.SetParent(formRoot, false);
            UiRuntimeBuildKit.ConfigureVerticalLayoutChild(go, 36f);

            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGo.transform.SetParent(go.transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0.5f);
            bgRt.anchorMax = new Vector2(0f, 0.5f);
            bgRt.pivot = new Vector2(0f, 0.5f);
            bgRt.sizeDelta = new Vector2(22f, 22f);
            bgGo.GetComponent<Image>().color = UiTheme.InputBackground;

            var checkGo = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            checkGo.transform.SetParent(bgGo.transform, false);
            Stretch(checkGo.GetComponent<RectTransform>(), 4f, 4f);
            checkGo.GetComponent<Image>().color = UiTheme.TitleAccent;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(RectMask2D),
                typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.offsetMin = new Vector2(30f, 2f);
            labelRt.offsetMax = new Vector2(-2f, -2f);
            var labelText = labelGo.GetComponent<TMP_Text>();
            if (_tmpFont != null) labelText.font = _tmpFont;
            labelText.text = label;
            labelText.fontSize = MenuCompactText;
            labelText.color = UiTheme.TextPrimary;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
            labelText.overflowMode = TextOverflowModes.Ellipsis;

            var toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = bgGo.GetComponent<Image>();
            toggle.graphic = checkGo.GetComponent<Image>();
            toggle.isOn = false;
            return toggle;
        }

        private static (Transform listContent, TMP_Text emptyHint) CreateSessionListScrollLayout(Transform formRoot)
        {
            var scrollGo = new GameObject("SessionListScroll", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            scrollGo.transform.SetParent(formRoot, false);
            UiRuntimeBuildKit.ConfigureVerticalLayoutChild(scrollGo, 120f, flexibleHeight: 1f);
            scrollGo.GetComponent<Image>().color = UiTheme.InputBackground;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            Stretch(viewportGo.GetComponent<RectTransform>(), 4f, 4f);
            viewportGo.GetComponent<Image>().color = Color.clear;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 2f;
            vlg.padding = new RectOffset(4, 4, 4, 4);

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewportGo.GetComponent<RectTransform>();
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var emptyGo = new GameObject("EmptyHint", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            emptyGo.transform.SetParent(scrollGo.transform, false);
            Stretch(emptyGo.GetComponent<RectTransform>(), 12f, 12f);
            var emptyHint = emptyGo.GetComponent<TMP_Text>();
            if (_tmpFont != null) emptyHint.font = _tmpFont;
            emptyHint.text = UiCopy.SessionBrowserLoading;
            emptyHint.fontSize = MenuCompactText;
            emptyHint.color = UiTheme.TextSubtitle;
            emptyHint.alignment = TextAlignmentOptions.TopLeft;
            emptyHint.textWrappingMode = TextWrappingModes.Normal;
            emptyHint.overflowMode = TextOverflowModes.Ellipsis;

            return (contentGo.transform, emptyHint);
        }

        private static (Button refresh, Button join) CreateJoinActionsRow(Transform formRoot)
        {
            var row = BuildFormRow(formRoot, "JoinActionsRow", 48f);
            var refresh = UiRuntimeBuildKit.CreateLayoutButton(row, "BtnRefresh", UiCopy.SessionBrowserRefresh, 140f, 40f);
            StyleButton(refresh);

            var join = UiRuntimeBuildKit.CreateLayoutButton(row, "BtnJoin", UiCopy.JoinRoomButton, 140f, 40f);
            var joinLe = join.GetComponent<LayoutElement>();
            joinLe.flexibleWidth = 1f;
            joinLe.minWidth = 140f;
            StyleButton(join);
            return (refresh, join);
        }

        private static void StyleButton(Button button)
        {
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);
            button.colors = colors;
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
