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

        private const int PauseBuildVersion = 2;

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
            var existing = transform.Find("PauseOverlay");
            if (existing != null)
            {
                var marker = existing.GetComponent<PauseOverlayVersion>();
                if (_built && marker != null && marker.Version == PauseBuildVersion)
                {
                    _overlay = existing.gameObject;
                    return;
                }

                Destroy(existing.gameObject);
                _built = false;
            }

            _overlay = UiRuntimeBuildKit.CreateDimOverlay(transform, "PauseOverlay");
            _overlay.transform.SetAsLastSibling();
            var version = _overlay.AddComponent<PauseOverlayVersion>();
            version.Version = PauseBuildVersion;

            var panel = UiRuntimeBuildKit.CreateCenteredPanel(_overlay.transform, "Panel", new Vector2(520f, 420f));
            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(28, 28, 24, 24);
            vlg.spacing = 14f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var title = CreateLayoutLabel(panel.transform, "PauseTitle", UiCopy.PauseTitle, 42,
                UiTheme.TitleAccent, FontStyles.Bold, 52f);
            title.alignment = TextAlignmentOptions.Center;

            var resume = CreateLayoutButton(panel.transform, "BtnResume", UiCopy.PauseResume);
            resume.onClick.AddListener(Close);

            var leave = CreateLayoutButton(panel.transform, "BtnLeave", UiCopy.PauseLeave);
            leave.onClick.AddListener(() => _ = LeaveAsync());

            var controls = CreateLayoutLabel(panel.transform, "PauseControls", UiCopy.PauseControls, 18,
                UiTheme.TextSubtitle, FontStyles.Normal, 88f);
            controls.alignment = TextAlignmentOptions.Center;
            controls.textWrappingMode = TextWrappingModes.Normal;
            controls.overflowMode = TextOverflowModes.Ellipsis;
            controls.enableAutoSizing = true;
            controls.fontSizeMin = 14f;
            controls.fontSizeMax = 18f;

            _built = true;
        }

        private static TMP_Text CreateLayoutLabel(Transform parent, string name, string text, int size, Color color,
            FontStyles style, float preferredHeight)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI),
                typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;
            le.minHeight = preferredHeight * 0.6f;
            le.flexibleWidth = 1f;

            var label = go.GetComponent<TMP_Text>();
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.raycastTarget = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            UiTypography.ApplyFontTo(label);
            return label;
        }

        private static Button CreateLayoutButton(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = 56f;
            le.minHeight = 56f;
            le.flexibleWidth = 1f;

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
            tmp.fontSize = 28f;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
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
                LocalPlayerHudCache.TryGetLocalAvatar(out _))
                GameplayInputMode.SetGameplay();
            else
                GameplayInputMode.SetMenu();
        }

        private async System.Threading.Tasks.Task LeaveAsync()
        {
            if (_leaveInProgress)
                return;
            _leaveInProgress = true;
            try
            {
                IsOpen = false;
                if (_overlay != null)
                    _overlay.SetActive(false);
                GameplayInputMode.ChatBlockingGameplay = false;
                GameplayInputMode.SetMenu();

                if (ConnectionManager.Instance != null)
                    await ConnectionManager.Instance.LeaveToMainMenuAsync();
                else
                    GameplayInputMode.SetMenu();
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

        private sealed class PauseOverlayVersion : MonoBehaviour
        {
            public int Version;
        }
    }
}
