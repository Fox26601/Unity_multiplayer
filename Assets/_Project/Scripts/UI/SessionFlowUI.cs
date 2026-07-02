using FusionMultiplayer.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>
    /// Shows connection status on Main Menu and Lobby (Connecting, errors, scene transitions).
    /// </summary>
    public sealed class SessionFlowUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _statusText;

        private Text _statusLegacy;
        private bool _isConnecting;

        public bool IsConnecting => _isConnecting;

        private void Awake()
        {
            ResolveRefs();
        }

        private void ResolveRefs()
        {
            if (_statusText != null) return;
            _statusText = GetComponentInChildren<TMP_Text>();
            var panel = transform.Find("Panel");
            if (panel != null)
            {
                var statusTr = panel.Find("SessionStatus");
                if (statusTr != null)
                {
                    _statusText ??= statusTr.GetComponent<TMP_Text>();
                    _statusLegacy ??= statusTr.GetComponent<Text>();
                }
            }
        }

        private void OnEnable()
        {
            ConnectionManager.SessionStarting += OnSessionStarting;
            ConnectionManager.SessionReady += OnSessionReadyHandler;
            ConnectionManager.SessionFailed += OnSessionFailed;
            ConnectionManager.SceneLoaded += OnSceneLoaded;
            SetStatus(string.Empty, false);
        }

        private void OnDisable()
        {
            ConnectionManager.SessionStarting -= OnSessionStarting;
            ConnectionManager.SessionReady -= OnSessionReadyHandler;
            ConnectionManager.SessionFailed -= OnSessionFailed;
            ConnectionManager.SceneLoaded -= OnSceneLoaded;
        }

        public void SetStatus(string message, bool isError)
        {
            var color = isError ? UiTheme.StatusError : UiTheme.StatusOk;
            var show = !string.IsNullOrEmpty(message);

            if (_statusText != null)
            {
                _statusText.text = message ?? string.Empty;
                _statusText.color = color;
                _statusText.gameObject.SetActive(show);
                return;
            }

            if (_statusLegacy != null)
            {
                _statusLegacy.text = message ?? string.Empty;
                _statusLegacy.color = color;
                _statusLegacy.gameObject.SetActive(show);
            }
        }

        private void OnSessionStarting()
        {
            _isConnecting = true;
            SetStatus(SessionData.UseOfflineMode ? UiCopy.StatusConnectingOffline : UiCopy.StatusConnecting, false);
        }

        private void OnSessionReadyHandler(Fusion.NetworkRunner runner)
        {
            _isConnecting = false;
            SetStatus(UiCopy.StatusEnteringLobby, false);
        }

        private void OnSessionFailed(string reason)
        {
            _isConnecting = false;
            SetStatus($"{UiCopy.StatusFailedPrefix}{reason}", true);
        }

        private void OnSceneLoaded(string sceneName)
        {
            _isConnecting = false;
            if (sceneName.Contains("01_Lobby"))
                SetStatus(UiCopy.StatusInLobby, false);
            else if (sceneName.Contains("02_Game"))
                SetStatus(UiCopy.StatusInGame, false);
        }
    }
}
