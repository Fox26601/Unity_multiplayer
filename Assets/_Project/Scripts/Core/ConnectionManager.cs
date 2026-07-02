using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using FusionMultiplayer.Player;
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

        [SerializeField] private NetworkObject _playerDataPrefab;

        private NetworkRunner _runner;

        public NetworkRunner Runner => _runner;

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
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                MppmSessionBridge.ClearRoom();
                Instance = null;
            }
        }

        /// <summary>Starts or joins a Shared Mode session and loads the lobby scene.</summary>
        public async Task<bool> StartSessionAsync(string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                var msg = "Room name is empty.";
                Debug.LogWarning(msg);
                SessionFailed?.Invoke(msg);
                return false;
            }

            SessionStarting?.Invoke();

            if (_runner == null)
            {
                _runner = gameObject.AddComponent<NetworkRunner>();
                _runner.ProvideInput = true;
                _runner.AddCallbacks(this);
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
            var result = await _runner.StartGame(new StartGameArgs
            {
                GameMode = offline ? GameMode.Single : GameMode.Shared,
                SessionName = roomName.Trim(),
                PlayerCount = offline ? 1 : 10,
                Scene = sceneInfo,
                SceneManager = sceneManager,
                EnableClientSessionCreation = !offline
            });

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

            SceneManager.LoadScene(SceneIndices.MainMenu);
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

            SessionFailed?.Invoke($"Session ended: {shutdownReason}");
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
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            SessionFailed?.Invoke(reason.ToString());
        }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
        }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
        }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
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
            if (idx == SceneIndices.Lobby || idx == SceneIndices.Game)
                TrySpawnPlayerData(runner);

            if (idx == SceneIndices.Game)
            {
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

            if (SceneManager.GetActiveScene().buildIndex != SceneIndices.Game)
                return;

            if (Instance == null)
                return;

            GameDiagnostics.LogLateJoinProvision(reason);
            Instance.TrySpawnPlayerData(runner);
            GameSceneReadiness.EvaluateNow();
        }

        private static string FormatStartGameError(StartGameResult result)
        {
            var reason = result.ShutdownReason.ToString();
            var detail = result.ErrorMessage?.Trim() ?? string.Empty;
            var combined = string.IsNullOrEmpty(detail) ? reason : $"{reason} {detail}";

            if (combined.IndexOf("DnsException", System.StringComparison.OrdinalIgnoreCase) >= 0
                || combined.IndexOf("Could not resolve host", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Cannot reach Photon Cloud (DNS lookup failed for ns.photonengine.io). " +
                       "Check internet, VPN/firewall, and macOS network permissions for Unity. " +
                       "For local testing: Tools → Fusion Multiplayer → Enable Offline Play (No Photon).";
            }

            return combined.Trim();
        }
    }
}
