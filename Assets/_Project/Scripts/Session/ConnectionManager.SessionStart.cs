using System.Threading;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FusionMultiplayer.Core
{
    public partial class ConnectionManager
    {
        public Task<bool> CreateSessionAsync(string roomName) =>
            StartSessionAsync(roomName, createIfMissing: true);

        public Task<bool> JoinSessionAsync(string roomName) =>
            StartSessionAsync(roomName, createIfMissing: false);

        public Task<bool> ReconnectSessionAsync(string roomName) =>
            StartSessionAsync(roomName, createIfMissing: false, isReconnect: true);

        /// <summary>Starts a Shared Mode session (create or join). Offline uses Single.</summary>
        public Task<bool> StartSessionAsync(string roomName, bool createIfMissing,
            bool dedicated = false, bool isReconnect = false)
        {
            if (_sessionStartTask != null && !_sessionStartTask.IsCompleted)
                return _sessionStartTask;

            // Dedicated Server path is out of scope for Shared Mode coursework.
            _sessionStartTask = StartSessionInternalAsync(roomName, createIfMissing, dedicated: false, isReconnect);
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

            // Finish any in-flight lobby join before StartGame (same runner).
            if (_lobbyJoinTask != null && !_lobbyJoinTask.IsCompleted)
            {
                try { await _lobbyJoinTask; }
                catch { /* StartGame path continues */ }
            }

            if (ct.IsCancellationRequested)
                return false;

            EnsureRunnerComponent(provideInput: true);

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
                SessionCatalog.SessionPhase.Lobby, SessionData.SelectedDifficulty);
            var maxPlayers = SessionData.ClampMaxPlayers(SessionData.MaxPlayers);
            SessionData.MaxPlayers = maxPlayers;
            SessionData.EnsureReconnectToken();

            var mode = NetworkAuthority.ResolveStartMode(createIfMissing, offline, dedicated: false);
            var connectionToken = SessionReconnectTokens.ToBytes(SessionData.EnsureReconnectToken());

            var result = await _runner.StartGame(new StartGameArgs
            {
                GameMode = mode,
                SessionName = roomName.Trim(),
                PlayerCount = offline ? 1 : maxPlayers,
                Scene = sceneInfo,
                SceneManager = sceneManager,
                EnableClientSessionCreation = !offline && createIfMissing,
                SessionProperties = offline ? null : props,
                IsVisible = offline || !SessionData.HiddenSession,
                IsOpen = true,
                CustomLobbyName = offline ? null : SessionCatalog.LobbyNameForMode(SessionData.SelectedGameMode),
                ConnectionToken = offline ? null : connectionToken
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
                // Only the create/main editor owns the MPPM room file — clones must not clear it on join fail.
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
            if (!offline)
            {
                SessionReconnectStore.Save(
                    _activeRoomName,
                    SessionData.EnsureReconnectToken(),
                    SessionData.SelectedGameMode,
                    SessionData.HiddenSession);
            }

            MppmSessionBridge.PublishRoom(_activeRoomName);
            SessionRuntime.Refresh(_runner);
            SessionReady?.Invoke(_runner);

            if (isReconnect)
                Debug.Log($"[FusionMultiplayer] Reconnected to room '{_activeRoomName}'");

            return true;
        }
    }
}
