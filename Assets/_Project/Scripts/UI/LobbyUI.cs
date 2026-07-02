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
        [SerializeField] private TMP_Text _playerListText;
        [SerializeField] private TMP_Text _statusText;

        private Text _playerListLegacy;
        private Text _statusLegacy;

        /// <summary>Called by LobbyRuntimeRebuild after it creates TMP widgets.</summary>
        public void BindRuntimeTmp(TMP_Text playerList, TMP_Text status, Button start)
        {
            _playerListText = playerList;
            _playerListLegacy = null;
            _statusText = status;
            _statusLegacy = null;
            _startButton = start;
            if (_startButton != null)
            {
                _startButton.onClick.RemoveListener(OnStartClicked);
                _startButton.onClick.AddListener(OnStartClicked);
            }
        }

        private void Awake()
        {
            if (GetComponent<UiReadabilityBootstrap>() == null)
                gameObject.AddComponent<UiReadabilityBootstrap>();

            LobbyRuntimeRebuild.EnsureBuilt(transform);
            ResolveRefs();
            if (_startButton != null)
            {
                _startButton.onClick.RemoveListener(OnStartClicked);
                _startButton.onClick.AddListener(OnStartClicked);
            }
            UiTypography.ApplyHierarchy(transform);
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

            if (_startButton == null)
                _startButton = panel.Find("BtnStart")?.GetComponent<Button>();
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
                SetStatusText(UiCopy.LobbyNotConnected);
                return;
            }

            if (_startButton != null)
                _startButton.gameObject.SetActive(runner.IsSceneAuthority);

            SetStatusText(runner.IsSceneAuthority
                ? UiCopy.LobbyMasterStatus
                : UiCopy.LobbyClientStatus);

            var sb = new StringBuilder();
            sb.AppendLine("ROOM PLAYERS");
            sb.AppendLine("----------------");

            var playerCount = 0;
            foreach (var _ in runner.ActivePlayers)
                playerCount++;

            sb.AppendLine($"Count: {playerCount} / 10");
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

        private void OnStartClicked()
        {
            var runner = ConnectionManager.Instance != null ? ConnectionManager.Instance.Runner : null;
            if (runner == null || !runner.IsRunning || !runner.IsSceneAuthority) return;

            runner.LoadScene(SceneRef.FromIndex(SceneIndices.Game));
        }
    }
}
