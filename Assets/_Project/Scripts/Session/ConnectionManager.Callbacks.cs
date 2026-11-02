using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using FusionMultiplayer.Data;
using FusionMultiplayer.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FusionMultiplayer.Core
{
    public partial class ConnectionManager
    {
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            NetworkPlayersChanged?.Invoke();

            if (NetworkAuthority.IsSessionAuthority(runner))
                ValidateJoiningPlayer(runner, player);

            ProvisionAllPlayersForCurrentScene(runner, "OnPlayerJoined");
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            NetworkPlayersChanged?.Invoke();
            RemotePlayerLeft?.Invoke(player);

            var pd = PlayerOwnership.FindPlayerData(player);
            if (pd != null)
                PlayerRegistry.NotifyReconnectTokenChanged(pd);

            // Bot takeover may defer until Shared master promotion.
            BotTakeover.TryReplaceDisconnectedPlayer(runner, player);

            // Always request reconnect window when match started (authority may arrive a tick later).
            if (runner != null && runner.SessionInfo.IsValid && SessionCatalog.IsSessionStarted(runner.SessionInfo))
                SessionLock.RequestReconnectWindow(runner);
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

            if (!NetworkAuthority.IsSessionAuthority(runner))
                return;

            var hasToken = SessionReconnectTokens.TryRead(token, out var tokenText);
            var known = hasToken && SessionReconnectTokens.IsKnownToken(tokenText);
            var started = runner.SessionInfo.IsValid && SessionCatalog.IsSessionStarted(runner.SessionInfo);

            if (started)
            {
                // Mid-match: only known reconnect tokens during an open reconnect window.
                if (!known || !SessionLock.IsReconnectWindowOpen(runner.SessionInfo))
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

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
        }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
            // Shared Mode uses GameManager.OnBecameSharedMaster; Host Migration is unused.
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
            Debug.Log($"[FusionMultiplayer] Master validated join for player {player.PlayerId}");
        }

        private void ProvisionPlayer(NetworkRunner runner, PlayerRef player)
        {
            if (runner == null || player == PlayerRef.None)
                return;

            // Shared Mode: each peer provisions only their LocalPlayer (SA+IA).
            // Master also restores reconnect seats for joining players.
            var isLocal = runner.LocalPlayer == player;
            var isMaster = NetworkAuthority.IsSessionAuthority(runner);

            if (!isLocal && !isMaster)
                return;

            if (isMaster &&
                SessionReconnectTokens.TryReadPlayerToken(runner, player, out var token) &&
                !string.IsNullOrWhiteSpace(token) &&
                ReconnectService.RestorePlayer(runner, player, token))
            {
                return;
            }

            if (!isLocal)
                return;

            TrySpawnPlayerDataFor(runner, player);
        }

        private void TrySpawnPlayerDataFor(NetworkRunner runner, PlayerRef player)
        {
            if (runner.LocalPlayer != player)
                return;

            if (PlayerRegistry.FindPlayerData(player) != null)
                return;

            foreach (var pd in PlayerRegistry.EnumerateAllData())
            {
                if (pd.Object != null && pd.Object.IsValid && pd.Object.InputAuthority == player)
                    return;
            }

            // Mid-match reclaim: never spawn a parallel PlayerData for a known reconnect token.
            var localToken = SessionData.ReconnectToken;
            if (string.IsNullOrWhiteSpace(localToken))
                SessionReconnectTokens.TryReadPlayerToken(runner, player, out localToken);

            if (!string.IsNullOrWhiteSpace(localToken))
            {
                if (ReconnectService.FindSeatByToken(localToken) != null ||
                    PlayerRegistry.FindPlayerDataByToken(localToken) != null)
                    return;

                if (runner.SessionInfo.IsValid && SessionCatalog.IsSessionStarted(runner.SessionInfo))
                    return;
            }

            if (NetworkSpawnFactory.SpawnPlayerData(runner, player) != null)
                return;

            var prefab = ResolvePlayerDataPrefab();
            if (prefab == null)
            {
                Debug.LogError("PlayerData prefab is not assigned on ConnectionManager or NetworkPrefabCatalog.");
                return;
            }

            runner.Spawn(prefab, Vector3.zero, Quaternion.identity, player);
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

            // Shared: each peer spawns own PlayerData; master also walks ActivePlayers for reconnect restore.
            if (NetworkAuthority.IsSessionAuthority(runner))
            {
                foreach (var player in runner.ActivePlayers)
                    ProvisionPlayer(runner, player);

                ReconnectService.FlushPending(runner);
            }
            else if (runner.LocalPlayer != PlayerRef.None)
            {
                ProvisionPlayer(runner, runner.LocalPlayer);
            }

            if (SceneIndices.IsGameScene(idx))
                GameSceneReadiness.EvaluateNow();
        }
    }
}
