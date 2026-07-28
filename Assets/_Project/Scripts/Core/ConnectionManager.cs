using System;
using System.Collections.Generic;
using System.Threading;
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
    /// Hosts the NetworkRunner for Client-Server / Dedicated Server sessions.
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
        public static event Action<string> LocalDisconnectNotice;
        public static event Action<PlayerRef> RemotePlayerLeft;

        [SerializeField] private NetworkObject _playerDataPrefab;

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
            GameSceneReadiness.EnsureSubscribed();
            SessionData.EnsureReconnectToken();
        }

        private void OnEnable()
        {
            // Recover singleton if a prior Instance was destroyed mid-session.
            if (Instance == null)
                Instance = this;
        }

        private void Start()
        {
            if (SessionData.IsDedicatedLaunch)
                _ = StartDedicatedServerAsync();
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
#if UNITY_EDITOR
                if (MppmUtility.IsMainInstance)
#endif
                    MppmSessionBridge.ClearRoom();
                Instance = null;
            }
        }

        private async Task StartDedicatedServerAsync()
        {
            var room = "Dedicated_" + System.Environment.MachineName;
            foreach (var arg in System.Environment.GetCommandLineArgs())
            {
                if (arg.StartsWith("-room=", StringComparison.OrdinalIgnoreCase))
                    room = arg.Substring("-room=".Length);
            }

            SessionData.MaxPlayers = SessionData.ClampMaxPlayers(SessionData.MaxPlayers);
            Debug.Log($"[FusionMultiplayer] Starting dedicated server room '{room}'");
            await StartSessionAsync(room, createIfMissing: true, dedicated: true);
        }

        /// <summary>Joins the Photon session lobby for browsing rooms of the selected game mode.</summary>
        public Task<bool> EnsureSessionLobbyAsync(SessionCatalog.GameModeKind mode)
        {
            if (SessionData.UseOfflineMode)
            {
                _sessionList.Clear();
                SessionListUpdated?.Invoke(_sessionList);
                return Task.FromResult(true);
            }

            // Already in the requested lobby — session list keeps updating via callbacks.
            if (_inSessionLobby && _lobbyGameMode == mode)
                return Task.FromResult(true);

            // Single-flight: concurrent Refresh/Update must not call JoinSessionLobby twice.
            if (_lobbyJoinTask != null && !_lobbyJoinTask.IsCompleted)
            {
                if (_lobbyJoinMode == mode)
                    return _lobbyJoinTask;

                // Different mode requested while joining — wait, then re-enter for the new mode.
                return AwaitThenEnsureLobbyAsync(mode);
            }

            _lobbyJoinMode = mode;
            _lobbyJoinTask = JoinSessionLobbyInternalAsync(mode);
            return _lobbyJoinTask;
        }

        /// <summary>
        /// Ensures lobby membership. Photon pushes list updates; re-joining while JoiningLobby causes errors.
        /// </summary>
        public Task<bool> RefreshSessionLobbyAsync(SessionCatalog.GameModeKind mode) =>
            EnsureSessionLobbyAsync(mode);

        private async Task<bool> AwaitThenEnsureLobbyAsync(SessionCatalog.GameModeKind mode)
        {
            try
            {
                if (_lobbyJoinTask != null)
                    await _lobbyJoinTask;
            }
            catch
            {
                // ignored — retry below
            }

            return await EnsureSessionLobbyAsync(mode);
        }

        private async Task<bool> JoinSessionLobbyInternalAsync(SessionCatalog.GameModeKind mode)
        {
            // Active match session — cannot browse lobbies on the same runner.
            if (_runner != null && _runner.IsRunning && !_inSessionLobby && !string.IsNullOrEmpty(_activeRoomName))
                return false;

            EnsureRunnerComponent(provideInput: true);
            if (_runner == null)
                return false;

            if (_inSessionLobby && _lobbyGameMode == mode)
                return true;

            _lobbyGameMode = mode;
            var lobbyName = SessionCatalog.LobbyNameForMode(mode);
            _sessionList.Clear();
            try
            {
                var result = await _runner.JoinSessionLobby(SessionLobby.Custom, lobbyName);
                if (!result.Ok)
                {
                    var msg = FormatStartGameError(result);
                    Debug.LogWarning($"Session lobby join failed: {msg}");
                    SessionFailed?.Invoke(msg);
                    _inSessionLobby = false;
                    await DisposeRunnerAsync();
                    return false;
                }
            }
            catch (Exception ex)
            {
                var msg = $"Session lobby join failed: {ex.Message}";
                Debug.LogWarning(msg);
                SessionFailed?.Invoke(msg);
                _inSessionLobby = false;
                await DisposeRunnerAsync();
                return false;
            }

            _inSessionLobby = true;
            return true;
        }

        public Task<bool> CreateSessionAsync(string roomName) =>
            StartSessionAsync(roomName, createIfMissing: true);

        public Task<bool> JoinSessionAsync(string roomName) =>
            StartSessionAsync(roomName, createIfMissing: false);

        public Task<bool> ReconnectSessionAsync(string roomName) =>
            StartSessionAsync(roomName, createIfMissing: false, isReconnect: true);

        /// <summary>Starts Host (create), Client (join), or Dedicated Server session.</summary>
        public Task<bool> StartSessionAsync(string roomName, bool createIfMissing,
            bool dedicated = false, bool isReconnect = false)
        {
            if (_sessionStartTask != null && !_sessionStartTask.IsCompleted)
                return _sessionStartTask;

            _sessionStartTask = StartSessionInternalAsync(roomName, createIfMissing, dedicated, isReconnect);
            return _sessionStartTask;
        }

        private async Task<bool> StartSessionInternalAsync(string roomName, bool createIfMissing,
            bool dedicated, bool isReconnect)
        {
            BeginLifecycleOperation();
            var ct = _lifecycleCts.Token;

            if (string.IsNullOrWhiteSpace(roomName))
            {
                var msg = "Room name is empty.";
                Debug.LogWarning(msg);
                SessionFailed?.Invoke(msg);
                return false;
            }

            SessionData.ClearSessionError();
            SessionStarting?.Invoke();
            dedicated = dedicated || SessionData.IsDedicatedLaunch;

            // Finish any in-flight lobby join before StartGame (same runner).
            if (_lobbyJoinTask != null && !_lobbyJoinTask.IsCompleted)
            {
                try { await _lobbyJoinTask; }
                catch { /* StartGame path continues */ }
            }

            if (ct.IsCancellationRequested)
                return false;

            EnsureRunnerComponent(provideInput: !dedicated);

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

            var offline = SessionData.UseOfflineMode && !dedicated;
            var map = SessionCatalog.ResolveMapForHost(SessionData.SelectedMap);
            var props = SessionCatalog.BuildProperties(SessionData.SelectedGameMode, map,
                SessionCatalog.SessionPhase.Lobby, SessionData.SelectedDifficulty);
            var maxPlayers = SessionData.ClampMaxPlayers(SessionData.MaxPlayers);
            SessionData.MaxPlayers = maxPlayers;
            SessionData.EnsureReconnectToken();

            var mode = NetworkAuthority.ResolveStartMode(createIfMissing, offline, dedicated);
            var connectionToken = SessionReconnectTokens.ToBytes(SessionData.EnsureReconnectToken());

            var result = await _runner.StartGame(new StartGameArgs
            {
                GameMode = mode,
                SessionName = roomName.Trim(),
                PlayerCount = offline ? 1 : maxPlayers,
                Scene = sceneInfo,
                SceneManager = sceneManager,
                EnableClientSessionCreation = !offline && !dedicated && createIfMissing,
                SessionProperties = offline ? null : props,
                IsVisible = offline || !SessionData.HiddenSession,
                IsOpen = true,
                CustomLobbyName = offline ? null : SessionCatalog.LobbyNameForMode(SessionData.SelectedGameMode),
                ConnectionToken = dedicated || offline ? null : connectionToken
            });

            if (ct.IsCancellationRequested)
            {
                await DisposeRunnerAsync();
                return false;
            }

            _inSessionLobby = false;

            if (!result.Ok)
            {
                var msg = FormatStartGameError(result);
                Debug.LogWarning($"StartGame failed: {msg}");
                SessionFailed?.Invoke(msg);
                await DisposeRunnerAsync();
                // Only the host/main editor owns the MPPM room file — clones must not clear it on join fail.
                if (createIfMissing
#if UNITY_EDITOR
                    || MppmUtility.IsMainInstance
#endif
                   )
                    MppmSessionBridge.ClearRoom();
                return false;
            }

            _intentionalShutdown = false;
            _activeRoomName = roomName.Trim();
            if (!dedicated && !offline)
            {
                SessionReconnectStore.Save(_activeRoomName, SessionData.EnsureReconnectToken(),
                    SessionData.SelectedGameMode);
            }

            MppmSessionBridge.PublishRoom(_activeRoomName);
            SessionRuntime.Refresh(_runner);
            SessionReady?.Invoke(_runner);

            if (isReconnect)
                Debug.Log($"[FusionMultiplayer] Reconnected to room '{_activeRoomName}'");

            return true;
        }

        /// <summary>Quick-join first open session matching mode / map / difficulty.</summary>
        public async Task<bool> QuickJoinAsync()
        {
            var preferredMode = SessionData.SelectedGameMode;
            var map = SessionData.SelectedMap;
            var difficulty = SessionData.SelectedDifficulty;

#if UNITY_EDITOR
            // MPPM virtual players: join the room the main editor just hosted.
            if (MppmUtility.IsVirtualInstance && MppmSessionBridge.TryReadRoom(out var mppmRoom))
            {
                Debug.Log($"[FusionMultiplayer] Quick Join via MPPM bridge → '{mppmRoom}'");
                return await JoinSessionAsync(mppmRoom);
            }
#endif

            // Prefer the selected mode lobby; if empty, scan other mode lobbies (map + difficulty still apply).
            var modesToTry = new List<SessionCatalog.GameModeKind> { preferredMode };
            foreach (var mode in SessionCatalog.AllGameModes)
            {
                if (mode != preferredMode)
                    modesToTry.Add(mode);
            }

            SessionInfo best = default;
            var found = false;

            foreach (var mode in modesToTry)
            {
                if (!await EnsureSessionLobbyAsync(mode))
                    continue;

                await WaitForSessionListAsync(2.5f);

                SessionInfo bestInLobby = default;
                var foundInLobby = false;
                foreach (var info in _sessionList)
                {
                    if (!SessionCatalog.MatchesFilters(info, mode, map, difficulty, requireOpen: true))
                        continue;

                    if (!foundInLobby || info.PlayerCount > bestInLobby.PlayerCount)
                    {
                        bestInLobby = info;
                        foundInLobby = true;
                    }
                }

                if (!foundInLobby)
                    continue;

                best = bestInLobby;
                found = true;
                // modesToTry is preferred-first — take the first lobby that has a match.
                break;
            }

            if (!found)
            {
                var filters =
                    $"{SessionCatalog.GetModeLabel(preferredMode)} / {SessionCatalog.GetMapLabel(map)} / {SessionCatalog.GetDifficultyLabel(difficulty)}";
                SessionFailed?.Invoke(UiCopy.QuickJoinNoMatchDetail(filters));
                return false;
            }

            if (SessionCatalog.TryGetGameMode(best, out var joinedMode) && joinedMode != preferredMode)
            {
                Debug.Log(
                    $"[FusionMultiplayer] Quick Join: no '{preferredMode}' rooms; joining '{joinedMode}' room '{best.Name}'.");
                SessionData.SelectedGameMode = joinedMode;
            }

            return await JoinSessionAsync(best.Name);
        }

        /// <summary>Waits for Photon to push a session list after joining a lobby.</summary>
        private async Task WaitForSessionListAsync(float timeoutSeconds)
        {
            if (_sessionList.Count > 0)
            {
                await Task.Delay(150);
                return;
            }

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            void OnList(IReadOnlyList<SessionInfo> _)
            {
                tcs.TrySetResult(true);
            }

            SessionListUpdated += OnList;
            try
            {
                var timeout = Task.Delay(TimeSpan.FromSeconds(Mathf.Max(0.5f, timeoutSeconds)));
                await Task.WhenAny(tcs.Task, timeout);
                await Task.Delay(200);
            }
            finally
            {
                SessionListUpdated -= OnList;
            }
        }

        public void ServerStartMatch()
        {
            if (_runner == null || !NetworkAuthority.IsServerOrHost(_runner))
                return;

            SessionLock.TryLockForGameStart(_runner);

            var map = SessionCatalog.MapKind.Arena;
            if (_runner.SessionInfo.IsValid && SessionCatalog.TryGetMap(_runner.SessionInfo, out var sessionMap))
                map = sessionMap;

            _runner.LoadScene(SceneRef.FromIndex(SessionCatalog.GetSceneBuildIndex(map)));
        }

        public async Task ShutdownToMainMenuAsync(bool clearReconnect = true)
        {
            _intentionalShutdown = true;
            CancelLifecycleOperations();
            GameplayInputMode.ChatBlockingGameplay = false;
            GameplayInputMode.SetMenu();

            if (clearReconnect)
                SessionReconnectStore.Clear();
#if UNITY_EDITOR
            if (MppmUtility.IsMainInstance)
#endif
                MppmSessionBridge.ClearRoom();
            _activeRoomName = string.Empty;
            await DisposeRunnerAsync();
            _sessionList.Clear();
            SceneManager.LoadScene(SceneIndices.MainMenu);
            GameplayInputMode.SetMenu();
        }

        /// <summary>Client: store error, leave session, reload Main Menu.</summary>
        public static void NotifyNicknameRejected(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                reason = UiCopy.NicknameTaken;

            SessionData.SetSessionError(reason);

            if (Instance != null)
            {
                // Prevent OnDisconnectedFromServer from overwriting the nick message with "Disconnected: Requested".
                Instance._intentionalShutdown = true;
                SessionFailed?.Invoke(reason);
                _ = Instance.ShutdownToMainMenuAsync(clearReconnect: true);
            }
            else
            {
                SessionFailed?.Invoke(reason);
            }
        }

        /// <summary>
        /// Server: drop a joining player after nickname reject.
        /// Delays Disconnect so RpcNicknameRejected can flush before the PlayerData object is torn down.
        /// </summary>
        public void RejectJoiningPlayer(PlayerRef player, NetworkObject playerDataObject)
        {
            if (_runner == null || !NetworkAuthority.IsServerOrHost(_runner) || player == PlayerRef.None)
                return;

            StartCoroutine(RejectJoiningPlayerAfterRpcFlush(player, playerDataObject));
        }

        private System.Collections.IEnumerator RejectJoiningPlayerAfterRpcFlush(PlayerRef player,
            NetworkObject playerDataObject)
        {
            // Fusion needs a few ticks to deliver the targeted reject RPC before Despawn/Disconnect.
            for (var i = 0; i < 12; i++)
                yield return null;

            if (_runner == null)
                yield break;

            if (IsPlayerStillConnected(_runner, player))
                _runner.Disconnect(player);

            if (playerDataObject != null && playerDataObject.IsValid)
                _runner.Despawn(playerDataObject);
        }

        private static bool IsPlayerStillConnected(NetworkRunner runner, PlayerRef player)
        {
            if (runner == null || player == PlayerRef.None)
                return false;

            foreach (var active in runner.ActivePlayers)
            {
                if (active == player)
                    return true;
            }

            return false;
        }

        private async Task DisposeRunnerAsync()
        {
            CancelLifecycleOperations();
            _inSessionLobby = false;
            _activeRoomName = string.Empty;
            PlayerRegistry.Clear();

            if (_runner == null)
                return;

            try
            {
                if (_runner.IsRunning || _runner.IsCloudReady)
                    await _runner.Shutdown(false, ShutdownReason.Ok);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Runner shutdown: {ex.Message}");
            }

            if (_runner != null)
            {
                Destroy(_runner);
                _runner = null;
            }
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

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            NetworkPlayersChanged?.Invoke();

            if (NetworkAuthority.IsServerOrHost(runner))
            {
                ProvisionPlayer(runner, player);
                ValidateJoiningPlayer(runner, player);
            }

            ProvisionAllPlayersForCurrentScene(runner, "OnPlayerJoined");
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            NetworkPlayersChanged?.Invoke();
            RemotePlayerLeft?.Invoke(player);

            if (NetworkAuthority.IsServerOrHost(runner))
            {
                // Keep reconnect token indexed after Fusion clears InputAuthority.
                var pd = PlayerOwnership.FindPlayerData(player);
                if (pd != null)
                    PlayerRegistry.NotifyReconnectTokenChanged(pd);

                BotTakeover.TryReplaceDisconnectedPlayer(runner, player);
            }
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            if (!runner.ProvideInput)
                return;

            GameplayInput.Sample(out var data);
            input.Set(data);
        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            _inSessionLobby = false;
            if (runner == _runner)
                _activeRoomName = string.Empty;

            if (_intentionalShutdown || shutdownReason == ShutdownReason.Ok)
                return;

            var msg = FormatShutdownReason(shutdownReason);
            LocalDisconnectNotice?.Invoke(msg);
            SessionFailed?.Invoke(msg);
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            if (_intentionalShutdown)
                return;

            // Nickname reject already stored SessionData.SessionError — keep that text, still leave the lobby.
            if (string.IsNullOrWhiteSpace(SessionData.SessionError))
            {
                var msg = FormatNetDisconnectReason(reason);
                SessionData.SetSessionError(msg);
                LocalDisconnectNotice?.Invoke(msg);
                SessionFailed?.Invoke(msg);
            }

            // Always leave a broken lobby/session. Keep reconnect token if we were in a match.
            var inMatch = SceneIndices.IsGameScene(SceneManager.GetActiveScene().buildIndex);
            _ = ShutdownToMainMenuAsync(clearReconnect: !inMatch);
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request,
            byte[] token)
        {
            if (runner == null || !runner.IsRunning)
                return;

            if (!NetworkAuthority.IsServerOrHost(runner))
                return;

            var hasToken = SessionReconnectTokens.TryRead(token, out var tokenText);
            var known = hasToken && SessionReconnectTokens.IsKnownToken(tokenText);

            if (runner.SessionInfo.IsValid && SessionCatalog.IsSessionStarted(runner.SessionInfo))
            {
                // Mid-match joins only allowed for crash reconnect with a known token.
                if (!known)
                {
                    request.Refuse();
                    return;
                }
            }

            if (runner.SessionInfo.IsValid && runner.SessionInfo.PlayerCount >= runner.SessionInfo.MaxPlayers)
            {
                if (!known)
                {
                    request.Refuse();
                    return;
                }
            }
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
            // Dedicated Server / Host Mode: host migration is not the primary path.
        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key,
            ArraySegment<byte> data)
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
                ProvisionAllPlayersForCurrentScene(runner, "OnSceneLoadDone");

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

        private void ValidateJoiningPlayer(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"[FusionMultiplayer] Server validated join for player {player.PlayerId}");
        }

        private void ProvisionPlayer(NetworkRunner runner, PlayerRef player)
        {
            if (runner == null || player == PlayerRef.None || !NetworkAuthority.IsServerOrHost(runner))
                return;

            if (SessionReconnectTokens.TryReadPlayerToken(runner, player, out var token) &&
                !string.IsNullOrWhiteSpace(token) &&
                ReconnectService.RestorePlayer(runner, player, token))
            {
                return;
            }

            TrySpawnPlayerDataFor(runner, player);
        }

        private void TrySpawnPlayerDataFor(NetworkRunner runner, PlayerRef player)
        {
            if (_playerDataPrefab == null)
            {
                Debug.LogError("PlayerData prefab is not assigned on ConnectionManager.");
                return;
            }

            if (!NetworkAuthority.IsServerOrHost(runner))
                return;

            if (PlayerRegistry.FindPlayerData(player) != null)
                return;

            foreach (var pd in PlayerRegistry.EnumerateAllData())
            {
                if (pd.Object != null && pd.Object.IsValid && pd.Object.InputAuthority == player)
                    return;
            }

            runner.Spawn(_playerDataPrefab, Vector3.zero, Quaternion.identity, player);
        }

        /// <summary>Ensures every active player has PlayerData (and reconnect restore) for the current scene.</summary>
        private void ProvisionAllPlayersForCurrentScene(NetworkRunner runner, string reason)
        {
            if (runner == null || !runner.IsRunning)
                return;

            var idx = SceneManager.GetActiveScene().buildIndex;
            if (!SceneIndices.IsGameScene(idx) && idx != SceneIndices.Lobby)
                return;

            if (Instance == null)
                return;

            if (SceneIndices.IsGameScene(idx))
                GameDiagnostics.LogLateJoinProvision(reason);

            if (NetworkAuthority.IsServerOrHost(runner))
            {
                foreach (var player in runner.ActivePlayers)
                    ProvisionPlayer(runner, player);
            }

            if (SceneIndices.IsGameScene(idx))
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
