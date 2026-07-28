using System.Text;
using FusionMultiplayer.Core;
using FusionMultiplayer.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>
    /// Master end-game control, results table, map vote, and session exit for all clients.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private GameObject _overlayRoot;
        [SerializeField] private Button _masterEndButton;
        [SerializeField] private Button _leaveButton;
        [SerializeField] private TMP_Text _resultsText;

        private TMP_Text _voteCountsText;
        private TMP_Text _voteStatusText;
        private Button[] _voteButtons;
        private bool _isGameOver;
        private bool _localVoteCast;

        private void Awake()
        {
            UiCanvasFix.EnsureReadableCanvas(transform);
            EnsureOverlayLayout();
            EnsureVoteLayout();
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
            _leaveButton ??= _overlayRoot.transform.Find("BtnLeave")?.GetComponent<Button>();

            if (_resultsText != null)
                return;

            var title = _overlayRoot.transform.Find("GameOverTitle")?.GetComponent<TMP_Text>();
            if (title != null)
                title.text = "GAME OVER";

            var resultsGo = new GameObject("ResultsTable", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            resultsGo.transform.SetParent(_overlayRoot.transform, false);
            UiRegionLayout.StretchBand(resultsGo.GetComponent<RectTransform>(), 0.42f, 0.62f, 80f);
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
            UiRegionLayout.StretchBand(resultsTitleGo.GetComponent<RectTransform>(), 0.62f, 0.70f, 80f);
            var resultsTitle = resultsTitleGo.GetComponent<TMP_Text>();
            resultsTitle.text = UiCopy.GameOverResultsTitle;
            resultsTitle.alignment = TextAlignmentOptions.Center;
            resultsTitle.fontSize = UiTypography.Subtitle;
            resultsTitle.fontStyle = FontStyles.Bold;
            resultsTitle.color = UiTheme.TitleAccent;
            UiTypography.ApplySubtitle(resultsTitle);
        }

        private void EnsureVoteLayout()
        {
            if (_overlayRoot == null)
                return;

            var voteRoot = _overlayRoot.transform.Find("VotePanel");
            if (voteRoot == null)
            {
                var voteGo = new GameObject("VotePanel", typeof(RectTransform));
                voteGo.transform.SetParent(_overlayRoot.transform, false);
                voteRoot = voteGo.transform;
                UiRegionLayout.StretchBand((RectTransform)voteRoot, 0.14f, 0.40f, 48f);

                var titleGo = new GameObject("VoteTitle", typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
                titleGo.transform.SetParent(voteRoot, false);
                UiRegionLayout.StretchBand(titleGo.GetComponent<RectTransform>(), 0.78f, 1f, 8f);
                var title = titleGo.GetComponent<TMP_Text>();
                title.text = UiCopy.GameOverVoteTitle;
                title.alignment = TextAlignmentOptions.Center;
                title.fontSize = UiTypography.Subtitle;
                title.fontStyle = FontStyles.Bold;
                title.color = UiTheme.TitleAccent;
                UiTypography.ApplySubtitle(title);

                var countsGo = new GameObject("VoteCounts", typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
                countsGo.transform.SetParent(voteRoot, false);
                UiRegionLayout.StretchBand(countsGo.GetComponent<RectTransform>(), 0.58f, 0.76f, 8f);
                _voteCountsText = countsGo.GetComponent<TMP_Text>();
                _voteCountsText.alignment = TextAlignmentOptions.Center;
                _voteCountsText.fontSize = UiTypography.Body;
                _voteCountsText.color = UiTheme.TextPrimary;
                UiTypography.ApplyBody(_voteCountsText, TextAlignmentOptions.Center);

                var statusGo = new GameObject("VoteStatus", typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
                statusGo.transform.SetParent(voteRoot, false);
                UiRegionLayout.StretchBand(statusGo.GetComponent<RectTransform>(), 0.42f, 0.56f, 8f);
                _voteStatusText = statusGo.GetComponent<TMP_Text>();
                _voteStatusText.alignment = TextAlignmentOptions.Center;
                _voteStatusText.fontSize = UiTypography.Body;
                _voteStatusText.color = UiTheme.TextSubtitle;
                UiTypography.ApplyBody(_voteStatusText, TextAlignmentOptions.Center);

                var rowGo = new GameObject("VoteButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                rowGo.transform.SetParent(voteRoot, false);
                UiRegionLayout.StretchBand(rowGo.GetComponent<RectTransform>(), 0f, 0.40f, 8f);
                var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
                hlg.spacing = 10f;
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = true;
                hlg.childForceExpandHeight = true;

                _voteButtons = new Button[GameManager.VoteOptionCount];
                _voteButtons[GameManager.VoteRestart] = CreateVoteButton(rowGo.transform, "BtnVoteRestart",
                    UiCopy.GameOverVoteRestart, GameManager.VoteRestart);
                _voteButtons[GameManager.VoteArena] = CreateVoteButton(rowGo.transform, "BtnVoteArena",
                    SessionCatalog.GetMapLabel(SessionCatalog.MapKind.Arena), GameManager.VoteArena);
                _voteButtons[GameManager.VotePlaza] = CreateVoteButton(rowGo.transform, "BtnVotePlaza",
                    SessionCatalog.GetMapLabel(SessionCatalog.MapKind.Plaza), GameManager.VotePlaza);
                _voteButtons[GameManager.VoteRuins] = CreateVoteButton(rowGo.transform, "BtnVoteRuins",
                    SessionCatalog.GetMapLabel(SessionCatalog.MapKind.Ruins), GameManager.VoteRuins);
            }
            else
            {
                _voteCountsText ??= voteRoot.Find("VoteCounts")?.GetComponent<TMP_Text>();
                _voteStatusText ??= voteRoot.Find("VoteStatus")?.GetComponent<TMP_Text>();
                if (_voteButtons == null || _voteButtons.Length != GameManager.VoteOptionCount)
                {
                    _voteButtons = new Button[GameManager.VoteOptionCount];
                    _voteButtons[GameManager.VoteRestart] =
                        voteRoot.Find("VoteButtons/BtnVoteRestart")?.GetComponent<Button>();
                    _voteButtons[GameManager.VoteArena] =
                        voteRoot.Find("VoteButtons/BtnVoteArena")?.GetComponent<Button>();
                    _voteButtons[GameManager.VotePlaza] =
                        voteRoot.Find("VoteButtons/BtnVotePlaza")?.GetComponent<Button>();
                    _voteButtons[GameManager.VoteRuins] =
                        voteRoot.Find("VoteButtons/BtnVoteRuins")?.GetComponent<Button>();
                }
            }

            if (_leaveButton != null)
            {
                var leaveRt = (RectTransform)_leaveButton.transform;
                leaveRt.anchorMin = new Vector2(0.5f, 0f);
                leaveRt.anchorMax = new Vector2(0.5f, 0f);
                leaveRt.pivot = new Vector2(0.5f, 0f);
                leaveRt.anchoredPosition = new Vector2(0f, 28f);
                leaveRt.sizeDelta = new Vector2(420f, 64f);
            }

            if (_resultsText != null)
                UiRegionLayout.StretchBand(_resultsText.rectTransform, 0.42f, 0.62f, 80f);

            var resultsTitle = _overlayRoot.transform.Find("ResultsTitle") as RectTransform;
            if (resultsTitle != null)
                UiRegionLayout.StretchBand(resultsTitle, 0.62f, 0.70f, 80f);

            var gameOverTitle = _overlayRoot.transform.Find("GameOverTitle") as RectTransform;
            if (gameOverTitle != null)
                UiRegionLayout.StretchBand(gameOverTitle, 0.72f, 0.88f, 48f);
        }

        private Button CreateVoteButton(Transform parent, string name, string label, int option)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = UiTheme.ButtonBackground;
            var button = go.GetComponent<Button>();
            UiTypography.StyleButton(button);

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<TMP_Text>();
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = UiTypography.Button;
            text.fontStyle = FontStyles.Bold;
            text.color = UiTheme.TextPrimary;
            UiTypography.ApplyButtonLabel(text);

            var captured = option;
            button.onClick.AddListener(() => OnVoteClicked(captured));
            return button;
        }

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
            if (!_isGameOver)
                _localVoteCast = false;
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
                RefreshVoteUi();
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

            if (_isGameOver)
                RefreshVoteUi();
        }

        private void UpdateResultsTable()
        {
            if (_resultsText == null)
                return;

            var rows = new StringBuilder();
            var any = CombatScoreboardTable.AppendRows(rows, UiCopy.GameOverResultsHeader);
            _resultsText.text = any ? rows.ToString() : UiCopy.GameOverNoScores;
        }

        private void RefreshVoteUi()
        {
            var counts = new int[GameManager.VoteOptionCount];
            var localVote = PlayerData.NoEndGameVote;

            foreach (var pd in PlayerRegistry.EnumerateAllData())
            {
                if (pd.Object == null || !pd.Object.IsValid)
                    continue;

                if (pd.HasInputAuthority)
                    localVote = pd.EndGameVote;

                if (GameManager.IsValidVoteOption(pd.EndGameVote))
                    counts[pd.EndGameVote]++;
            }

            _localVoteCast = GameManager.IsValidVoteOption(localVote);

            if (_voteCountsText != null)
            {
                _voteCountsText.text = string.Format(UiCopy.GameOverVoteCountsFormat,
                    counts[GameManager.VoteRestart],
                    counts[GameManager.VoteArena],
                    counts[GameManager.VotePlaza],
                    counts[GameManager.VoteRuins]);
            }

            if (_voteStatusText != null)
            {
                if (_localVoteCast)
                {
                    _voteStatusText.text = string.Format(UiCopy.GameOverVotedFormat, VoteLabel(localVote));
                }
                else if (GameSceneReadiness.TryGetGameManager(out var gm) &&
                         gm.TryGetVoteRemaining(out var remaining))
                {
                    _voteStatusText.text = string.Format(UiCopy.GameOverVoteTimerFormat, Mathf.CeilToInt(remaining));
                }
                else
                {
                    _voteStatusText.text = UiCopy.GameOverVoteTitle;
                }
            }

            if (_voteButtons == null)
                return;

            for (var i = 0; i < _voteButtons.Length; i++)
            {
                if (_voteButtons[i] != null)
                    _voteButtons[i].interactable = !_localVoteCast;
            }
        }

        private static string VoteLabel(int option) => option switch
        {
            GameManager.VoteRestart => UiCopy.GameOverVoteRestart,
            GameManager.VoteArena => SessionCatalog.GetMapLabel(SessionCatalog.MapKind.Arena),
            GameManager.VotePlaza => SessionCatalog.GetMapLabel(SessionCatalog.MapKind.Plaza),
            GameManager.VoteRuins => SessionCatalog.GetMapLabel(SessionCatalog.MapKind.Ruins),
            _ => "?"
        };

        private void OnVoteClicked(int option)
        {
            if (_localVoteCast || !GameManager.IsValidVoteOption(option))
                return;

            if (!TryGetLocalPlayerData(out var pd))
                return;

            pd.RpcCastEndGameVote(option);
            _localVoteCast = true;
            RefreshVoteUi();
        }

        private static bool TryGetLocalPlayerData(out PlayerData data)
        {
            data = null;
            foreach (var pd in PlayerRegistry.EnumerateAllData())
            {
                if (pd.Object == null || !pd.Object.IsValid || !pd.HasInputAuthority)
                    continue;

                data = pd;
                return true;
            }

            return false;
        }

        private static bool HasLocalAvatar()
        {
            foreach (var avatar in PlayerRegistry.EnumerateAllAvatars())
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
