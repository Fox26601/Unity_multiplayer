using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using FusionMultiplayer.Player;
using FusionMultiplayer.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// Hosts the NetworkRunner for the session and handles lobby PlayerData spawn.
    /// </summary>
    [RequireComponent(typeof(NetworkSceneManagerDefault))]
    public class ConnectionManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        public static ConnectionManager Instance { get; private set; }

        public static event Action SessionStarting;
        public static event Action<NetworkRunner> SessionReady;
        public static event Action<string> SessionFailed;
        public static event Action<string> SceneLoaded;
        public static event Action NetworkPlayersChanged;
        public static event Action<IReadOnlyList<SessionInfo>> SessionListUpdated;

        [SerializeField] private NetworkObject _playerDataPrefab;

        private NetworkRunner _runner;
        private readonly List<SessionInfo> _sessionList = new();
        private SessionCatalog.GameModeKind _lobbyGameMode = SessionCatalog.GameModeKind.Build;
        private bool _inSessionLobby;

        public NetworkRunner Runner => _runner;
        public IReadOnlyList<SessionInfo> CachedSessionList => _sessionList;
        public bool IsInSessionLobby => _inSessionLobby;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            GameSceneReadiness.EnsureSubscribed();
        }

        private void Update()
        {
            GameplayInput.AccumulateKeyEdges();
            GameplayInput.AccumulateLook();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                MppmSessionBridge.ClearRoom();
                Instance = null;
            }
        }

        /// <summary>Joins the Photon session lobby for browsing rooms of the selected game mode.</summary>
        public async Task<bool> EnsureSessionLobbyAsync(SessionCatalog.GameModeKind mode)
        {
            if (SessionData.UseOfflineMode)
            {
                _sessionList.Clear();
                SessionListUpdated?.Invoke(_sessionList);
                return true;
            }

            if (_runner != null && _runner.IsRunning)
                return false;

            EnsureRunnerComponent();
            if (_runner == null)
                return false;

            if (_inSessionLobby && _lobbyGameMode == mode)
                return true;

            _lobbyGameMode = mode;
            var lobbyName = SessionCatalog.LobbyNameForMode(mode);
            try
            {
                await _runner.JoinSessionLobby(SessionLobby.Custom, lobbyName);
            }
            catch (System.Exception ex)
            {
                var msg = $"Session lobby join failed: {ex.Message}";
                Debug.LogWarning(msg);
                SessionFailed?.Invoke(msg);
                _inSessionLobby = false;
                return false;
            }

            _inSessionLobby = true;
            return true;
        }

        /// <summary>Re-enters the session lobby after the selected game mode changes.</summary>
        public async Task<bool> RefreshSessionLobbyAsync(SessionCatalog.GameModeKind mode)
        {
            _inSessionLobby = false;
            return await EnsureSessionLobbyAsync(mode);
        }

        /// <summary>Creates a new session and loads the lobby scene.</summary>
        public Task<bool> CreateSessionAsync(string roomName) =>
            StartSessionAsync(roomName, createIfMissing: true);

        /// <summary>Joins an existing session by name and loads the lobby scene.</summary>
        public Task<bool> JoinSessionAsync(string roomName) =>
            StartSessionAsync(roomName, createIfMissing: false);

        /// <summary>Starts or joins a Shared Mode session and loads the lobby scene.</summary>
        public async Task<bool> StartSessionAsync(string roomName, bool createIfMissing)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                var msg = "Room name is empty.";
                Debug.LogWarning(msg);
                SessionFailed?.Invoke(msg);
                return false;
            }

            SessionStarting?.Invoke();
            EnsureRunnerComponent();

            if (_runner == null)
            {
                SessionFailed?.Invoke("NetworkRunner could not be created.");
                return false;
            }

            var sceneManager = GetComponent<NetworkSceneManagerDefault>();
            if (sceneManager == null)
            {
                var msg = "NetworkSceneManagerDefault is required on ConnectionManager.";
                Debug.LogError(msg);
                SessionFailed?.Invoke(msg);
                return false;
            }

            var sceneInfo = new NetworkSceneInfo();
            sceneInfo.AddSceneRef(SceneRef.FromIndex(SceneIndices.Lobby), LoadSceneMode.Single);

            var offline = SessionData.UseOfflineMode;
            var map = SessionCatalog.ResolveMapForHost(SessionData.SelectedMap);
            var props = SessionCatalog.BuildProperties(SessionData.SelectedGameMode, map,
                SessionCatalog.SessionPhase.Lobby);

            var result = await _runner.StartGame(new StartGameArgs
            {
                GameMode = offline ? GameMode.Single : GameMode.Shared,
                SessionName = roomName.Trim(),
                PlayerCount = offline ? 1 : 10,
                Scene = sceneInfo,
                SceneManager = sceneManager,
                EnableClientSessionCreation = !offline && createIfMissing,
                SessionProperties = offline ? null : props,
                IsVisible = offline || !SessionData.HiddenSession,
                IsOpen = true,
                CustomLobbyName = offline ? null : SessionCatalog.LobbyNameForMode(SessionData.SelectedGameMode)
            });

            _inSessionLobby = false;

            if (!result.Ok)
            {
                var msg = FormatStartGameError(result);
                Debug.LogWarning($"StartGame failed: {msg}");
                SessionFailed?.Invoke(msg);
                if (_runner != null)
                {
                    Destroy(_runner);
                    _runner = null;
                }

                MppmSessionBridge.ClearRoom();
                return false;
            }

            MppmSessionBridge.PublishRoom(roomName.Trim());
            SessionRuntime.Refresh(_runner);
            SessionReady?.Invoke(_runner);
            return true;
        }

        public async Task ShutdownToMainMenuAsync()
        {
            MppmSessionBridge.ClearRoom();

            if (_runner != null && _runner.IsRunning)
                await _runner.Shutdown(false, ShutdownReason.Ok);

            if (_runner != null)
            {
                Destroy(_runner);
                _runner = null;
            }

            _inSessionLobby = false;
            _sessionList.Clear();
            SceneManager.LoadScene(SceneIndices.MainMenu);
        }

        private void EnsureRunnerComponent()
        {
            if (_runner != null)
                return;

            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this);
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            NetworkPlayersChanged?.Invoke();
            ProvisionGameSceneIfNeeded(runner, "OnPlayerJoined");
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            NetworkPlayersChanged?.Invoke();
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            GameplayInput.Sample(out var data);
            input.Set(data);
        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            if (shutdownReason == ShutdownReason.Ok)
                return;

            SessionFailed?.Invoke(FormatShutdownReason(shutdownReason));
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            SessionFailed?.Invoke($"Disconnected: {reason}");
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request,
            byte[] token)
        {
            if (runner == null || !runner.IsRunning)
                return;

            if (runner.SessionInfo.IsValid && SessionCatalog.IsSessionStarted(runner.SessionInfo))
            {
                request.Refuse();
                return;
            }

            if (SceneIndices.IsGameScene(SceneManager.GetActiveScene().buildIndex))
                request.Refuse();
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            SessionFailed?.Invoke(FormatConnectFailedReason(reason));
        }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            _sessionList.Clear();
            if (sessionList != null)
                _sessionList.AddRange(sessionList);
            SessionListUpdated?.Invoke(_sessionList);
        }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
        }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
            _ = ResumeAfterHostMigrationAsync(runner, hostMigrationToken);
        }

        private async Task ResumeAfterHostMigrationAsync(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
            if (runner == null || hostMigrationToken == null)
                return;

            var scene = SceneManager.GetActiveScene();
            var sceneInfo = new NetworkSceneInfo();
            if (scene.buildIndex >= 0)
                sceneInfo.AddSceneRef(SceneRef.FromIndex(scene.buildIndex), LoadSceneMode.Single);

            var result = await runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Shared,
                HostMigrationToken = hostMigrationToken,
                HostMigrationResume = OnHostMigrationResumed,
                Scene = sceneInfo,
                SceneManager = GetComponent<NetworkSceneManagerDefault>(),
            });

            if (!result.Ok)
                SessionFailed?.Invoke($"Host migration resume failed: {result.ShutdownReason}");
        }

        private static void OnHostMigrationResumed(NetworkRunner runner)
        {
            if (runner == null || !runner.IsRunning)
                return;

            SessionRuntime.Refresh(runner);
            GameSceneReadiness.EvaluateNow();
        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key,
            System.ArraySegment<byte> data)
        {
        }

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
        }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            var scene = SceneManager.GetActiveScene();
            var localPlayerId = runner != null && runner.IsRunning ? runner.LocalPlayer.PlayerId : -1;
            GameDiagnostics.LogSceneLoad(scene.name, scene.buildIndex, localPlayerId);
            SceneLoaded?.Invoke(scene.name);

            var idx = scene.buildIndex;
            if (SceneIndices.IsGameScene(idx) || idx == SceneIndices.Lobby)
                TrySpawnPlayerData(runner);

            if (SceneIndices.IsGameScene(idx))
            {
                SessionLock.TryLockForGameStart(runner);
                GameDiagnostics.LogLateJoinProvision("OnSceneLoadDone");
                GameSceneReadiness.EvaluateNow();
            }
        }

        public void OnSceneLoadStart(NetworkRunner runner)
        {
        }

        private void TrySpawnPlayerData(NetworkRunner runner)
        {
            if (_playerDataPrefab == null)
            {
                Debug.LogError("PlayerData prefab is not assigned on ConnectionManager.");
                return;
            }

            foreach (var pd in FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
            {
                if (pd.Object != null && pd.Object.IsValid && pd.Object.InputAuthority == runner.LocalPlayer)
                    return;
            }

            runner.Spawn(_playerDataPrefab, Vector3.zero, Quaternion.identity, runner.LocalPlayer);
        }

        private static void ProvisionGameSceneIfNeeded(NetworkRunner runner, string reason)
        {
            if (runner == null || !runner.IsRunning)
                return;

            if (!SceneIndices.IsGameScene(SceneManager.GetActiveScene().buildIndex))
                return;

            if (Instance == null)
                return;

            GameDiagnostics.LogLateJoinProvision(reason);
            Instance.TrySpawnPlayerData(runner);
            GameSceneReadiness.EvaluateNow();
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

            return combined.Trim();
        }
    }
}
