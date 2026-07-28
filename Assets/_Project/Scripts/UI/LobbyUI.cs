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
        private readonly StringBuilder _playerListSb = new(256);
        private readonly StringBuilder _signatureSb = new(128);
        private float _nextListRefreshTime;
        private string _lastPlayerListSignature = string.Empty;
        private string _lastStatus;
        private string _lastSessionInfo;

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
                _startButton.gameObject.SetActive(true);
                _startButton.interactable = true;
            }

            if (_leaveButton != null)
            {
                _leaveButton.gameObject.SetActive(true);
                _leaveButton.interactable = !_leaveInProgress;
            }

            if (Time.unscaledTime < _nextListRefreshTime)
                return;

            _nextListRefreshTime = Time.unscaledTime + 0.25f;

            UpdateSessionInfo(runner);

            var started = runner.SessionInfo.IsValid && SessionCatalog.IsSessionStarted(runner.SessionInfo);
            var isAuthority = NetworkAuthority.IsServerOrHost(runner);
            SetStatusText(started
                ? UiCopy.LobbySessionStarted
                : isAuthority
                    ? UiCopy.LobbyMasterStatus
                    : UiCopy.LobbyClientStatus);

            RebuildPlayerListIfChanged(runner);
        }

        private void RebuildPlayerListIfChanged(NetworkRunner runner)
        {
            var playerCount = 0;
            foreach (var _ in runner.ActivePlayers)
                playerCount++;

            var maxPlayers = runner.SessionInfo.IsValid ? runner.SessionInfo.MaxPlayers : SessionData.MaxPlayers;

            _signatureSb.Clear();
            _signatureSb.Append(playerCount).Append('/').Append(maxPlayers);

            _playerListSb.Clear();
            _playerListSb.AppendLine("ROOM PLAYERS");
            _playerListSb.AppendLine("----------------");
            _playerListSb.Append("Count: ").Append(playerCount).Append(" / ").Append(maxPlayers)
                .AppendLine().AppendLine();

            var anyListed = false;
            var list = PlayerRegistry.CopyAllData();
            for (var i = 0; i < list.Count; i++)
            {
                var pd = list[i];
                if (pd.Object == null || !pd.Object.IsValid) continue;
                anyListed = true;
                var nick = pd.Nick.ToString();
                var id = pd.Object.InputAuthority;
                var youTag = id == runner.LocalPlayer ? " (you)" : string.Empty;
                var tintHex = ColorUtility.ToHtmlStringRGB(pd.Tint);
                _playerListSb.Append("• <color=#").Append(tintHex).Append('>').Append(nick)
                    .Append("</color>").Append(youTag).AppendLine();
                _signatureSb.Append('|').Append(id.PlayerId).Append(':').Append(nick).Append(':')
                    .Append(tintHex);
            }

            if (!anyListed)
                _playerListSb.AppendLine("(Player profiles still loading...)");

            var signature = _signatureSb.ToString();
            if (signature == _lastPlayerListSignature)
                return;

            _lastPlayerListSignature = signature;
            SetPlayerListText(_playerListSb.ToString());
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
            if (value == _lastStatus)
                return;
            _lastStatus = value;
            if (_statusText != null) _statusText.text = value;
            else if (_statusLegacy != null) _statusLegacy.text = value;
        }

        private void SetSessionInfoText(string value)
        {
            value ??= string.Empty;
            if (value == _lastSessionInfo)
                return;
            _lastSessionInfo = value;
            if (_sessionInfoText != null)
                _sessionInfoText.text = value;
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

            foreach (var pd in PlayerRegistry.CopyAllData())
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
                    await ConnectionManager.Instance.LeaveToMainMenuAsync();
            }
            finally
            {
                _leaveInProgress = false;
            }
        }
    }
}
