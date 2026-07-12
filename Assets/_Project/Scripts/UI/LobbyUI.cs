using System.Text;
using Fusion;
using FusionMultiplayer.Core;
using FusionMultiplayer.Player;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>
    /// Lobby list and master-only start into the game scene.
    /// </summary>
    public class LobbyUI : MonoBehaviour
    {
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _leaveButton;
        [SerializeField] private TMP_Text _playerListText;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _sessionInfoText;

        private Text _playerListLegacy;
        private Text _statusLegacy;
        private bool _leaveInProgress;

        /// <summary>Called by LobbyRuntimeRebuild after it creates TMP widgets.</summary>
        public void BindRuntimeTmp(TMP_Text playerList, TMP_Text status, Button start, TMP_Text sessionInfo,
            Button leave = null)
        {
            _playerListText = playerList;
            _playerListLegacy = null;
            _statusText = status;
            _statusLegacy = null;
            _startButton = start;
            _sessionInfoText = sessionInfo;
            _leaveButton = leave;
            WireButtons();
        }

        private void Awake()
        {
            if (GetComponent<UiReadabilityBootstrap>() == null)
                gameObject.AddComponent<UiReadabilityBootstrap>();

            LobbyRuntimeRebuild.EnsureBuilt(transform);
            ResolveRefs();
            WireButtons();
            UiTypography.ApplyHierarchy(transform);
        }

        private void WireButtons()
        {
            if (_startButton != null)
            {
                _startButton.onClick.RemoveListener(OnStartClicked);
                _startButton.onClick.AddListener(OnStartClicked);
            }

            if (_leaveButton != null)
            {
                _leaveButton.onClick.RemoveListener(OnLeaveClicked);
                _leaveButton.onClick.AddListener(OnLeaveClicked);
            }
        }

        private void ResolveRefs()
        {
            var panel = transform.Find("Panel");
            if (panel == null) return;

            if (_playerListText == null)
                _playerListText = panel.Find("PlayerList")?.GetComponent<TMP_Text>();
            if (_playerListText == null)
                _playerListLegacy = panel.Find("PlayerList")?.GetComponent<Text>();

            if (_statusText == null)
                _statusText = panel.Find("Status")?.GetComponent<TMP_Text>();
            if (_statusText == null)
                _statusLegacy = panel.Find("Status")?.GetComponent<Text>();

            if (_sessionInfoText == null)
                _sessionInfoText = panel.Find("SessionInfo")?.GetComponent<TMP_Text>();

            if (_startButton == null)
                _startButton = panel.Find("BtnStart")?.GetComponent<Button>();
            if (_leaveButton == null)
                _leaveButton = panel.Find("BtnLeave")?.GetComponent<Button>();
        }

        private void Start()
        {
            var current = GetPlayerListText();
            if (string.IsNullOrWhiteSpace(current))
                SetPlayerListText(UiCopy.LobbyPlayersWaiting);
        }

        private void Update()
        {
            if (SceneManager.GetActiveScene().buildIndex != SceneIndices.Lobby)
            {
                if (gameObject.activeSelf)
                    gameObject.SetActive(false);
                return;
            }

            var runner = ConnectionManager.Instance != null ? ConnectionManager.Instance.Runner : null;
            if (runner == null || !runner.IsRunning)
            {
                if (_startButton != null) _startButton.gameObject.SetActive(false);
                if (_leaveButton != null)
                {
                    _leaveButton.gameObject.SetActive(true);
                    _leaveButton.interactable = !_leaveInProgress;
                }

                SetStatusText(UiCopy.LobbyNotConnected);
                SetSessionInfoText(string.Empty);
                return;
            }

            if (_startButton != null)
            {
                // Host/server can start directly; clients request start via RPC (Dedicated Server).
                _startButton.gameObject.SetActive(true);
                _startButton.interactable = true;
            }

            if (_leaveButton != null)
            {
                _leaveButton.gameObject.SetActive(true);
                _leaveButton.interactable = !_leaveInProgress;
            }

            UpdateSessionInfo(runner);

            var started = runner.SessionInfo.IsValid && SessionCatalog.IsSessionStarted(runner.SessionInfo);
            var isAuthority = NetworkAuthority.IsServerOrHost(runner);
            SetStatusText(started
                ? UiCopy.LobbySessionStarted
                : isAuthority
                    ? UiCopy.LobbyMasterStatus
                    : UiCopy.LobbyClientStatus);

            var sb = new StringBuilder();
            sb.AppendLine("ROOM PLAYERS");
            sb.AppendLine("----------------");

            var playerCount = 0;
            foreach (var _ in runner.ActivePlayers)
                playerCount++;

            var maxPlayers = runner.SessionInfo.IsValid ? runner.SessionInfo.MaxPlayers : SessionData.MaxPlayers;
            sb.AppendLine($"Count: {playerCount} / {maxPlayers}");
            sb.AppendLine();

            var anyListed = false;
            foreach (var pd in FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
            {
                if (pd.Object == null || !pd.Object.IsValid) continue;
                anyListed = true;
                var nick = pd.Nick.ToString();
                var id = pd.Object.InputAuthority;
                var youTag = id == runner.LocalPlayer ? " (you)" : string.Empty;
                var tintHex = ColorUtility.ToHtmlStringRGB(pd.Tint);
                sb.AppendLine($"• <color=#{tintHex}>{nick}</color>{youTag}");
            }

            if (!anyListed)
                sb.AppendLine("(Player profiles still loading...)");

            SetPlayerListText(sb.ToString());
        }

        private void UpdateSessionInfo(NetworkRunner runner)
        {
            if (_sessionInfoText == null)
                return;

            var info = runner.SessionInfo;
            if (!info.IsValid)
            {
                SetSessionInfoText(string.Empty);
                return;
            }

            var mode = SessionCatalog.TryGetGameMode(info, out var gm)
                ? SessionCatalog.GetModeLabel(gm)
                : SessionCatalog.GetModeLabel(SessionData.SelectedGameMode);
            var map = SessionCatalog.TryGetMap(info, out var mp)
                ? SessionCatalog.GetMapLabel(mp)
                : SessionCatalog.GetMapLabel(SessionData.SelectedMap);
            var startedTag = SessionCatalog.IsSessionStarted(info) ? "  ·  STARTED" : string.Empty;
            SetSessionInfoText(string.Format(UiCopy.LobbySessionInfoFormat, mode, map) + startedTag);
        }

        private string GetPlayerListText()
        {
            if (_playerListText != null) return _playerListText.text;
            return _playerListLegacy != null ? _playerListLegacy.text : string.Empty;
        }

        private void SetPlayerListText(string value)
        {
            if (_playerListText != null) _playerListText.text = value;
            else if (_playerListLegacy != null) _playerListLegacy.text = value;
        }

        private void SetStatusText(string value)
        {
            if (_statusText != null) _statusText.text = value;
            else if (_statusLegacy != null) _statusLegacy.text = value;
        }

        private void SetSessionInfoText(string value)
        {
            if (_sessionInfoText != null)
                _sessionInfoText.text = value ?? string.Empty;
        }

        private void OnStartClicked()
        {
            var runner = ConnectionManager.Instance != null ? ConnectionManager.Instance.Runner : null;
            if (runner == null || !runner.IsRunning)
                return;

            if (NetworkAuthority.IsServerOrHost(runner))
            {
                ConnectionManager.Instance.ServerStartMatch();
                return;
            }

            foreach (var pd in FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
            {
                if (pd.Object != null && pd.Object.IsValid && pd.Object.InputAuthority == runner.LocalPlayer)
                {
                    pd.RpcRequestStartMatch();
                    SetStatusText(UiCopy.LobbyStartRequested);
                    return;
                }
            }
        }

        private async void OnLeaveClicked()
        {
            if (_leaveInProgress)
                return;

            _leaveInProgress = true;
            if (_leaveButton != null)
                _leaveButton.interactable = false;
            if (_startButton != null)
                _startButton.interactable = false;

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
    }
}
