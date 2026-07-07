using System.Text;
using FusionMultiplayer.Core;
using FusionMultiplayer.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>
    /// Master-only end game control, results table, and session exit for all clients.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private GameObject _overlayRoot;
        [SerializeField] private Button _masterEndButton;
        [SerializeField] private Button _leaveButton;
        [SerializeField] private TMP_Text _resultsText;

        private void Awake()
        {
            UiCanvasFix.EnsureReadableCanvas(transform);
            EnsureOverlayLayout();
            if (_overlayRoot != null) _overlayRoot.SetActive(false);
            EnlargeMasterButton();
            if (_masterEndButton != null) _masterEndButton.onClick.AddListener(OnMasterEndClicked);
            if (_leaveButton != null) _leaveButton.onClick.AddListener(OnLeaveClicked);
            UiTypography.ApplyHierarchy(transform);
        }

        private void EnsureOverlayLayout()
        {
            _overlayRoot ??= transform.Find("GameOverOverlay")?.gameObject;
            if (_overlayRoot == null)
                return;

            _resultsText ??= _overlayRoot.transform.Find("ResultsTable")?.GetComponent<TMP_Text>();
            if (_resultsText != null)
                return;

            var title = _overlayRoot.transform.Find("GameOverTitle")?.GetComponent<TMP_Text>();
            if (title != null)
                title.text = "GAME OVER";

            var resultsGo = new GameObject("ResultsTable", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            resultsGo.transform.SetParent(_overlayRoot.transform, false);
            UiRegionLayout.StretchBand(resultsGo.GetComponent<RectTransform>(), 0.28f, 0.60f, 80f);
            _resultsText = resultsGo.GetComponent<TMP_Text>();
            _resultsText.text = UiCopy.GameOverNoScores;
            _resultsText.alignment = TextAlignmentOptions.Top;
            _resultsText.fontSize = UiTypography.Body;
            _resultsText.color = UiTheme.TextPrimary;
            _resultsText.textWrappingMode = TextWrappingModes.Normal;
            UiTypography.ApplyBody(_resultsText, TextAlignmentOptions.Top);

            var resultsTitleGo = new GameObject("ResultsTitle", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            resultsTitleGo.transform.SetParent(_overlayRoot.transform, false);
            UiRegionLayout.StretchBand(resultsTitleGo.GetComponent<RectTransform>(), 0.60f, 0.68f, 80f);
            var resultsTitle = resultsTitleGo.GetComponent<TMP_Text>();
            resultsTitle.text = UiCopy.GameOverResultsTitle;
            resultsTitle.alignment = TextAlignmentOptions.Center;
            resultsTitle.fontSize = UiTypography.Subtitle;
            resultsTitle.fontStyle = FontStyles.Bold;
            resultsTitle.color = UiTheme.TitleAccent;
            UiTypography.ApplySubtitle(resultsTitle);
        }

        private bool _isGameOver;

        private void OnEnable()
        {
            GameOverBridge.StateChanged += OnGameOverStateChanged;
            RefreshGameOverState();
        }

        private void OnDisable()
        {
            GameOverBridge.StateChanged -= OnGameOverStateChanged;
        }

        private void OnGameOverStateChanged(bool isGameOver)
        {
            _isGameOver = isGameOver;
            ApplyGameOverPresentation();
        }

        private void RefreshGameOverState()
        {
            GameSceneReadiness.TryGetGameManager(out var gm);
            _isGameOver = gm != null && gm.GetIsGameOverSafe();
            ApplyGameOverPresentation();
        }

        private void ApplyGameOverPresentation()
        {
            if (_overlayRoot != null)
                _overlayRoot.SetActive(_isGameOver);

            if (_isGameOver)
            {
                UpdateResultsTable();
                GameplayInputMode.SetMenu();
            }
            else if (!GameplayInputMode.ChatBlockingGameplay && HasLocalAvatar())
            {
                GameplayInputMode.SetGameplay();
            }
        }

        private void EnlargeMasterButton()
        {
            if (_masterEndButton == null)
                _masterEndButton = transform.Find("BtnEndGame")?.GetComponent<Button>();
            if (_masterEndButton == null) return;

            var rt = (RectTransform)_masterEndButton.transform;
            rt.anchorMin = new Vector2(0.82f, 0.58f);
            rt.anchorMax = new Vector2(1f, 0.62f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.offsetMin = new Vector2(8f, 0f);
            rt.offsetMax = new Vector2(-12f, 0f);
            UiTypography.StyleButton(_masterEndButton);
        }

        private void Update()
        {
            var runner = ConnectionManager.Instance != null ? ConnectionManager.Instance.Runner : null;
            GameSceneReadiness.TryGetGameManager(out var gm);
            var gmReady = gm != null;

            if (_masterEndButton != null)
            {
                _masterEndButton.gameObject.SetActive(runner != null && runner.IsRunning && runner.IsSceneAuthority &&
                    gmReady && !_isGameOver);
            }
        }

        private void UpdateResultsTable()
        {
            if (_resultsText == null)
                return;

            var rows = new StringBuilder();
            var any = CombatScoreboardTable.AppendRows(rows, UiCopy.GameOverResultsHeader);
            _resultsText.text = any ? rows.ToString() : UiCopy.GameOverNoScores;
        }

        private static bool HasLocalAvatar()
        {
            foreach (var avatar in Object.FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
            {
                if (avatar.Object != null && avatar.Object.IsValid && avatar.HasInputAuthority)
                    return true;
            }

            return false;
        }

        private void OnMasterEndClicked()
        {
            if (!GameSceneReadiness.TryGetGameManager(out var gm)) return;
            gm.MasterSetGameOver();
        }

        private async void OnLeaveClicked()
        {
            if (ConnectionManager.Instance != null)
                await ConnectionManager.Instance.ShutdownToMainMenuAsync();
        }
    }
}
