using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using FusionMultiplayer.Data;
using FusionMultiplayer.Player;
using FusionMultiplayer.Session;
using FusionMultiplayer.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// Hosts the NetworkRunner for Shared Mode sessions (create / join / offline Single).
    /// </summary>
    [RequireComponent(typeof(NetworkSceneManagerDefault))]
    public partial class ConnectionManager : MonoBehaviour, INetworkRunnerCallbacks, ISessionConnection, ISessionBrowser
    {
        public static ConnectionManager Instance { get; private set; }

        public static event Action SessionStarting;
        public static event Action<NetworkRunner> SessionReady;
        public static event Action<string> SessionFailed;
        public static event Action<string> SceneLoaded;
        public static event Action NetworkPlayersChanged;
        public static event Action<IReadOnlyList<SessionInfo>> SessionListUpdated;
        public static event Action<string> LocalDisconnectNotice;
        public static event Action<PlayerRef> RemotePlayerLeft;

        [SerializeField] private NetworkObject _playerDataPrefab;
        [SerializeField] private GameConfigs _gameConfigs;

        private NetworkRunner _runner;
        private readonly List<SessionInfo> _sessionList = new();
        private SessionCatalog.GameModeKind _lobbyGameMode = SessionCatalog.GameModeKind.Sandbox;
        private bool _inSessionLobby;
        private bool _intentionalShutdown;
        private string _activeRoomName = string.Empty;
        private Task<bool> _lobbyJoinTask;
        private SessionCatalog.GameModeKind _lobbyJoinMode;
        private Task<bool> _sessionStartTask;
        private CancellationTokenSource _lifecycleCts;

        public NetworkRunner Runner => _runner;
        public IReadOnlyList<SessionInfo> CachedSessionList => _sessionList;
        public bool IsInSessionLobby => _inSessionLobby;
        public NetworkObject PlayerDataPrefab => _playerDataPrefab;
        public string ActiveRoomName => _activeRoomName;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            GameConfigs.EnsureCurrent(_gameConfigs);
            GameSceneReadiness.EnsureSubscribed();
            SessionData.EnsureReconnectToken();
        }

        public NetworkObject ResolvePlayerDataPrefab() =>
            NetworkSpawnFactory.Catalog?.PlayerData != null
                ? NetworkSpawnFactory.Catalog.PlayerData
                : _playerDataPrefab;

        private void OnEnable()
        {
            // Recover singleton if a prior Instance was destroyed mid-session.
            if (Instance == null)
                Instance = this;
        }

        private void Start()
        {
            // Dedicated Server auto-start is out of scope (Shared Mode only).
        }

        private void Update()
        {
            GameplayInput.AccumulateKeyEdges();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
#if UNITY_EDITOR
                if (MppmUtility.IsMainInstance)
#endif
                    MppmSessionBridge.ClearRoom();
                Instance = null;
            }
        }

        public void ServerStartMatch()
        {
            if (_runner == null || !NetworkAuthority.IsSessionAuthority(_runner))
                return;

            SessionLock.TryLockForGameStart(_runner);

            var map = SessionCatalog.MapKind.Arena;
            if (_runner.SessionInfo.IsValid && SessionCatalog.TryGetMap(_runner.SessionInfo, out var sessionMap))
                map = sessionMap;

            _runner.LoadScene(SceneRef.FromIndex(SessionCatalog.GetSceneBuildIndex(map)));
        }

        private void BeginLifecycleOperation()
        {
            CancelLifecycleOperations();
            _lifecycleCts?.Dispose();
            _lifecycleCts = new CancellationTokenSource();
        }

        private void CancelLifecycleOperations()
        {
            try
            {
                _lifecycleCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void EnsureRunnerComponent(bool provideInput)
        {
            if (_runner != null)
            {
                _runner.ProvideInput = provideInput;
                return;
            }

            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = provideInput;
            _runner.AddCallbacks(this);
        }

        private static string FormatShutdownReason(ShutdownReason reason)
        {
            switch (reason)
            {
                case ShutdownReason.InvalidAuthentication:
                case ShutdownReason.CustomAuthenticationFailed:
                case ShutdownReason.AuthenticationTicketExpired:
                    return UiCopy.PhotonAuthFailed;
                default:
                    return $"Session ended: {reason}";
            }
        }

        private static string FormatConnectFailedReason(NetConnectFailedReason reason)
        {
            var text = reason.ToString();
            if (text.IndexOf("Auth", StringComparison.OrdinalIgnoreCase) >= 0)
                return UiCopy.PhotonAuthFailed;
            return text;
        }

        private static string FormatNetDisconnectReason(NetDisconnectReason reason)
        {
            // Server Disconnect() after nickname reject — if RPC was lost, still avoid raw enum spam.
            if (reason == NetDisconnectReason.Requested)
                return UiCopy.DisconnectedByServer;

            return $"Disconnected: {reason}";
        }

        private static string FormatStartGameError(StartGameResult result)
        {
            var reason = result.ShutdownReason.ToString();
            var detail = result.ErrorMessage?.Trim() ?? string.Empty;
            var combined = string.IsNullOrEmpty(detail) ? reason : $"{reason} {detail}";

            if (result.ShutdownReason == ShutdownReason.InvalidAuthentication
                || result.ShutdownReason == ShutdownReason.CustomAuthenticationFailed
                || result.ShutdownReason == ShutdownReason.AuthenticationTicketExpired
                || combined.IndexOf("OpAuthenticate", StringComparison.OrdinalIgnoreCase) >= 0
                || combined.IndexOf("Authenticate without Token", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return UiCopy.PhotonAuthFailed;
            }

            if (combined.IndexOf("DnsException", StringComparison.OrdinalIgnoreCase) >= 0
                || combined.IndexOf("Could not resolve host", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Cannot reach Photon Cloud (DNS lookup failed for ns.photonengine.io). " +
                       "Check internet, VPN/firewall, and macOS network permissions for Unity. " +
                       "For local testing: Tools → Fusion Multiplayer → Enable Offline Play (No Photon).";
            }

            if (combined.IndexOf("GameClosed", StringComparison.OrdinalIgnoreCase) >= 0)
                return UiCopy.JoinRoomClosed;

            if (combined.IndexOf("GameFull", StringComparison.OrdinalIgnoreCase) >= 0
                || combined.IndexOf("GameDoesNotExist", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return UiCopy.JoinRoomFailed(combined);
            }

            return combined.Trim();
        }
    }
}
