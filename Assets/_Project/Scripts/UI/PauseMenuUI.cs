using FusionMultiplayer.Core;
using FusionMultiplayer.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>
    /// In-match pause overlay (Esc). Blocks local gameplay input without freezing Fusion.
    /// </summary>
    public sealed class PauseMenuUI : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        private GameObject _overlay;
        private bool _built;
        private bool _leaveInProgress;

        private void Awake()
        {
            UiCanvasFix.EnsureReadableCanvas(transform);
            EnsureUi();
            SetOpen(false);
        }

        private void OnDisable()
        {
            if (IsOpen)
                SetOpen(false);
        }

        private void EnsureUi()
        {
            if (_built)
                return;

            var existing = transform.Find("PauseOverlay");
            if (existing != null)
                Destroy(existing.gameObject);

            _overlay = new GameObject("PauseOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _overlay.transform.SetParent(transform, false);
            _overlay.transform.SetAsLastSibling();
            var overlayRt = _overlay.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;
            var dim = _overlay.GetComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            dim.raycastTarget = true;

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(_overlay.transform, false);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(480f, 360f);
            panel.GetComponent<Image>().color = UiTheme.PanelBackground;

            var title = CreateLabel(panel.transform, "Title", UiCopy.PauseTitle, UiTypography.Title,
                UiTheme.TitleAccent, FontStyles.Bold, new Vector2(0f, 120f), new Vector2(400f, 48f));
            title.alignment = TextAlignmentOptions.Center;

            var resume = CreateButton(panel.transform, "BtnResume", UiCopy.PauseResume, new Vector2(0f, 40f));
            resume.onClick.AddListener(Close);

            var leave = CreateButton(panel.transform, "BtnLeave", UiCopy.PauseLeave, new Vector2(0f, -40f));
            leave.onClick.AddListener(() => _ = LeaveAsync());

            var controls = CreateLabel(panel.transform, "Controls", UiCopy.PauseControls, 16,
                UiTheme.TextSubtitle, FontStyles.Normal, new Vector2(0f, -130f), new Vector2(420f, 80f));
            controls.alignment = TextAlignmentOptions.Center;
            controls.textWrappingMode = TextWrappingModes.Normal;

            _built = true;
        }

        private static TMP_Text CreateLabel(Transform parent, string name, string text, int size, Color color,
            FontStyles style, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            var label = go.GetComponent<TMP_Text>();
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.raycastTarget = false;
            UiTypography.ApplyFontTo(label);
            return label;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(320f, 56f);
            go.GetComponent<Image>().color = UiTheme.ButtonBackground;
            var button = go.GetComponent<Button>();
            UiTypography.StyleButton(button);

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var tmp = textGo.GetComponent<TMP_Text>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = UiTypography.Body;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            UiTypography.ApplyFontTo(tmp);
            return button;
        }

        private void Update()
        {
            if (_overlay == null)
                return;

            if (IsGameOverVisible())
            {
                if (IsOpen)
                    SetOpen(false);
                return;
            }

            if (!IsInGameScene())
            {
                if (IsOpen)
                    SetOpen(false);
                return;
            }

            var kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame)
                return;

            if (IsChatComposeOpen())
                return;

            SetOpen(!IsOpen);
        }

        private void Close() => SetOpen(false);

        private void SetOpen(bool open)
        {
            IsOpen = open;
            if (_overlay != null)
                _overlay.SetActive(open);

            if (open)
            {
                GameplayInputMode.SetMenu();
                return;
            }

            if (!GameplayInputMode.ChatBlockingGameplay && IsInGameScene() && !IsGameOverVisible() &&
                HasLocalAvatar())
                GameplayInputMode.SetGameplay();
            else
                GameplayInputMode.SetMenu();
        }

        private static bool HasLocalAvatar()
        {
            foreach (var a in Object.FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
            {
                if (a.Object != null && a.Object.IsValid && a.HasInputAuthority)
                    return true;
            }

            return false;
        }

        private async System.Threading.Tasks.Task LeaveAsync()
        {
            if (_leaveInProgress)
                return;
            _leaveInProgress = true;
            SetOpen(false);
            try
            {
                if (ConnectionManager.Instance != null)
                    await ConnectionManager.Instance.ShutdownToMainMenuAsync();
            }
            finally
            {
                _leaveInProgress = false;
            }
        }

        private static bool IsInGameScene()
        {
            var idx = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
            return SceneIndices.IsGameScene(idx);
        }

        private static bool IsGameOverVisible()
        {
            var go = Object.FindFirstObjectByType<GameOverUI>();
            if (go == null)
                return false;
            var overlay = go.transform.Find("GameOverOverlay");
            return overlay != null && overlay.gameObject.activeSelf;
        }

        private static bool IsChatComposeOpen()
        {
            var chat = Object.FindFirstObjectByType<ChatUI>();
            return chat != null && GameplayInputMode.ChatBlockingGameplay;
        }
    }
}
