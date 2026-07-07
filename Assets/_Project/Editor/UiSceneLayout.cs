#if UNITY_EDITOR
using FusionMultiplayer.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionMultiplayer.EditorTools
{
    /// <summary>
    /// Shared rect layout and canvas setup for generated / patched menu scenes (TextMeshPro).
    /// Uses anchor bands (no overlap) aligned with runtime rebuilds.
    /// </summary>
    internal static class UiSceneLayout
    {
        public static Canvas CreateCanvas(string name, Camera worldCamera = null)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.AddComponent<UiReadabilityBootstrap>();
            var canvas = go.GetComponent<Canvas>();
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = UiTypography.CanvasReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            if (worldCamera != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = worldCamera;
                canvas.planeDistance = 0.5f;
            }

            return canvas;
        }

        public static UIPanelHost CreateUIPanel(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            Stretch(rt, 32f, 32f, 32f, 32f);
            var img = go.GetComponent<Image>();
            img.color = UiTheme.PanelBackground;
            var host = go.AddComponent<UIPanelHost>();
            host.rectTransform = rt;
            host.image = img;
            return host;
        }

        public static TMP_Text CreateTitle(Transform parent, string name, string label)
        {
            var t = CreateText(parent, name, UiTypography.Title, TextAlignmentOptions.Center, FontStyles.Bold);
            t.text = label;
            UiTypography.ApplyTitle(t);
            UiRegionLayout.StretchBand(t.rectTransform, 0.88f, 0.98f, 48f);
            return t;
        }

        public static TMP_Text CreateSubtitle(Transform parent, string name, string label)
        {
            var t = CreateText(parent, name, UiTypography.Subtitle, TextAlignmentOptions.Center);
            t.text = label;
            UiTypography.ApplySubtitle(t);
            UiRegionLayout.StretchBand(t.rectTransform, 0.80f, 0.87f, 48f);
            return t;
        }

        public static TMP_Text CreateStatusLine(Transform parent, string name, string label)
        {
            var t = CreateText(parent, name, UiTypography.Caption, TextAlignmentOptions.Center);
            t.text = label;
            UiTypography.ApplyCaption(t, TextAlignmentOptions.Center);
            UiRegionLayout.StretchBand(t.rectTransform, 0.04f, 0.12f, 48f);
            return t;
        }

        public static Button CreateButton(Transform parent, string name, string label, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(go.transform, false);
            var trt = txtGo.GetComponent<RectTransform>();
            Stretch(trt, 10f, 8f, 10f, 8f);
            var t = txtGo.GetComponent<TMP_Text>();
            t.text = label;
            UiTypography.ApplyButtonLabel(t);
            var img = go.GetComponent<Image>();
            img.color = UiTheme.ButtonBackground;
            var button = go.GetComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = UiTheme.ButtonHighlighted;
            colors.pressedColor = UiTheme.ButtonPressed;
            button.colors = colors;
            return button;
        }

        public static TMP_InputField CreateInputField(Transform parent, string name, string placeholder, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            var field = go.GetComponent<TMP_InputField>();
            var img = go.GetComponent<Image>();
            img.color = UiTheme.InputBackground;

            var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(go.transform, false);
            var textAreaRt = textArea.GetComponent<RectTransform>();
            Stretch(textAreaRt, 8f, 6f, 8f, 6f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(textArea.transform, false);
            var trt = textGo.GetComponent<RectTransform>();
            Stretch(trt, 4f, 2f, 4f, 2f);
            var text = textGo.GetComponent<TMP_Text>();
            UiTypography.ApplyInputText(text, false);

            var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            phGo.transform.SetParent(textArea.transform, false);
            var prt = phGo.GetComponent<RectTransform>();
            Stretch(prt, 4f, 2f, 4f, 2f);
            var ph = phGo.GetComponent<TMP_Text>();
            ph.text = placeholder;
            UiTypography.ApplyInputText(ph, true);

            field.textViewport = textAreaRt;
            field.textComponent = text;
            field.placeholder = ph;
            field.targetGraphic = img;
            return field;
        }

        public static TMP_Text CreateText(Transform parent, string name, int fontSize,
            TextAlignmentOptions align, FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(600f, 400f);
            var t = go.GetComponent<TMP_Text>();
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.alignment = align;
            UiTypography.ApplyBody(t, align);
            return t;
        }

        public static Image CreateImage(Transform parent, string name, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w, h);
            return go.GetComponent<Image>();
        }

        public static void ConfigureMainMenuPanel(Transform panel, TMP_InputField nick, TMP_InputField room,
            Image preview, Button randomColor, Button create, Button join)
        {
            UiRegionLayout.StretchBand(nick.GetComponent<RectTransform>(), 0.66f, 0.79f, 48f);
            UiRegionLayout.StretchBand(room.GetComponent<RectTransform>(), 0.52f, 0.65f, 48f);

            var previewRt = preview.rectTransform;
            previewRt.anchorMin = new Vector2(0.08f, 0.36f);
            previewRt.anchorMax = new Vector2(0.08f, 0.43f);
            previewRt.pivot = new Vector2(0f, 0.5f);
            previewRt.anchoredPosition = Vector2.zero;
            previewRt.sizeDelta = new Vector2(72f, 0f);

            var randomRt = (RectTransform)randomColor.transform;
            randomRt.anchorMin = new Vector2(0.22f, 0.36f);
            randomRt.anchorMax = new Vector2(0.92f, 0.43f);
            randomRt.offsetMin = Vector2.zero;
            randomRt.offsetMax = Vector2.zero;

            UiRegionLayout.CenterInBand((RectTransform)create.transform, 0.24f, 0.33f, new Vector2(520f, 0f));
            UiRegionLayout.CenterInBand((RectTransform)join.transform, 0.13f, 0.22f, new Vector2(520f, 0f));
        }

        public static void ConfigureLobbyPanel(TMP_Text playerList, TMP_Text status, Button start)
        {
            UiRegionLayout.StretchBand(playerList.rectTransform, 0.24f, 0.86f, 40f);
            UiTypography.ApplyBody(playerList, TextAlignmentOptions.TopLeft);
            playerList.text = UiCopy.LobbyPlayersWaiting;

            UiRegionLayout.StretchBand(status.rectTransform, 0.12f, 0.22f, 48f);
            UiTypography.ApplyCaption(status, TextAlignmentOptions.MidlineLeft);

            UiRegionLayout.CenterInBand((RectTransform)start.transform, 0.03f, 0.11f, new Vector2(560f, 0f));
            var startLabel = start.GetComponentInChildren<TMP_Text>();
            if (startLabel != null) UiTypography.ApplyButtonLabel(startLabel);
        }

        public static void ConfigureCharacterSelectPanel(TMP_Text status)
        {
            UiRegionLayout.StretchBand(status.rectTransform, 0.78f, 0.98f, 24f);
            UiTypography.ApplyCaption(status, TextAlignmentOptions.Center);
            status.text = UiCopy.CharacterSelectPrompt;
        }

        public static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }
    }
}
#endif
