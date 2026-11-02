using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using FusionMultiplayer.Data;
using FusionMultiplayer.Player;
using UnityEngine;

namespace FusionMultiplayer.Core
{
    public partial class GameManager
    {
        public void MasterSetGameOver()
        {
            if (!CanMutateSlots || !HasStateAuthority)
                return;

            if (IsGameOver)
                return;

            IsGameOver = true;
            VoteResolved = false;
            VoteTimer = TickTimer.CreateFromSeconds(Runner, ActiveVoteDurationSeconds);
            ResetAllEndGameVotes();
            RpcBroadcastGameOver();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RpcBroadcastGameOver()
        {
            GameOverBridge.NotifyStateChanged(true);
            MatchStatsDatabase.WriteMatchResults();
        }

        /// <summary>Master random decision: critical hit / difficulty multiplier.</summary>
        public static float RollDamage(float baseDamage) => CombatRules.RollDamage(baseDamage);

        private void TickPhysicsPropSpawns()
        {
            if (!_physicsPropsArmed)
            {
                if (!HasAnyCharacterOccupied())
                    return;

                _physicsPropsArmed = true;
                var match = GameConfigs.EnsureCurrent()?.Match;
                var firstMin = match != null ? match.PhysicsPropFirstDelayMin : 20f;
                var firstMax = match != null ? match.PhysicsPropFirstDelayMax : 35f;
                _physicsPropTimer = TickTimer.CreateFromSeconds(Runner, Random.Range(firstMin, firstMax));
                return;
            }

            if (!_physicsPropTimer.Expired(Runner))
                return;

            TrySpawnPhysicsProp();
            var matchCfg = GameConfigs.EnsureCurrent()?.Match;
            var repeatMin = matchCfg != null ? matchCfg.PhysicsPropRepeatMin : 18f;
            var repeatMax = matchCfg != null ? matchCfg.PhysicsPropRepeatMax : 30f;
            _physicsPropTimer = TickTimer.CreateFromSeconds(Runner, Random.Range(repeatMin, repeatMax));
        }

        private bool HasAnyCharacterOccupied()
        {
            for (var i = 0; i < PlayerRegistry.MaxCharacterSlots; i++)
            {
                if (CharacterOwners.Get(i) != PlayerRef.None)
                    return true;
            }

            return false;
        }

        private void TrySpawnPhysicsProp()
        {
            if (Runner == null)
                return;

            GetRandomizedSpawn(Random.Range(0, PlayerRegistry.MaxCharacterSlots), out var pos, out _);
            pos.y += 3f;
            if (NetworkSpawnFactory.SpawnPhysicsProp(Runner, pos, Quaternion.identity) != null)
                return;

            if (_physicsPropPrefab != null)
                Runner.Spawn(_physicsPropPrefab, pos, Quaternion.identity, null);
            else
                Debug.LogWarning("[FusionMultiplayer] PhysicsProp prefab missing (catalog and GameManager).");
        }

        private void EnsurePhysicsPropPrefab()
        {
            if (_physicsPropPrefab == null && NetworkSpawnFactory.Catalog?.PhysicsProp == null)
                Debug.LogWarning("[FusionMultiplayer] PhysicsProp prefab is not assigned on GameManager or catalog.");
        }

        private void TryResolveVotes()
        {
            if (!HasStateAuthority || VoteResolved)
                return;

            EndGameVoteResolver.CountPlayersAndVotes(out var connected, out var voted, out var counts,
                out var totalVotes);
            var allVoted = connected > 0 && voted >= connected;
            var timerDone = VoteTimer.ExpiredOrNotRunning(Runner);

            if (!allVoted && !timerDone)
                return;

            if (totalVotes <= 0)
            {
                ApplyVoteResult(GetCurrentMap());
                return;
            }

            var winningOption = EndGameVoteResolver.PickWinningOption(counts, totalVotes);
            ApplyVoteResult(ResolveMapFromVote(winningOption, GetCurrentMap()));
        }

        private SessionCatalog.MapKind GetCurrentMap()
        {
            if (Runner != null && Runner.SessionInfo.IsValid &&
                SessionCatalog.TryGetMap(Runner.SessionInfo, out var map))
                return map;

            return SceneIndices.GetMapKind(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        private void ApplyVoteResult(SessionCatalog.MapKind map)
        {
            if (!HasStateAuthority || VoteResolved)
                return;

            VoteResolved = true;
            ResetMatchScoresAndSlots();
            IsGameOver = false;
            GameOverBridge.NotifyStateChanged(false);

            if (Runner != null && Runner.SessionInfo.IsValid)
            {
                Runner.SessionInfo.UpdateCustomProperties(new Dictionary<string, SessionProperty>
                {
                    { SessionCatalog.PropMap, (int)map }
                });
            }

            SessionData.SelectedMap = map;
            Runner.LoadScene(SceneRef.FromIndex(SessionCatalog.GetSceneBuildIndex(map)));
        }

        private void ResetMatchScoresAndSlots()
        {
            for (var i = 0; i < PlayerRegistry.MaxCharacterSlots; i++)
                CharacterOwners.Set(i, PlayerRef.None);

            _physicsPropsArmed = false;
            _physicsPropTimer = default;

            // Drop bot / disconnected seats (PlayerData + avatar) before resetting humans.
            ReconnectService.PurgeDisconnectedSeats(Runner);

            foreach (var pd in PlayerRegistry.EnumerateAllData())
            {
                if (pd.Object == null || !pd.Object.IsValid)
                    continue;

                pd.ServerResetMatchStats();
            }

            foreach (var avatar in PlayerRegistry.EnumerateAllAvatars())
            {
                if (avatar == null || avatar.Object == null || !avatar.Object.IsValid)
                    continue;

                avatar.RequestReleaseBody();
            }
        }

        private static void ResetAllEndGameVotes()
        {
            foreach (var pd in PlayerRegistry.EnumerateAllData())
            {
                if (pd.Object == null || !pd.Object.IsValid)
                    continue;

                pd.ServerClearEndGameVote();
            }
        }

        #region Runner callbacks

        private sealed class GameManagerRunnerCallbacks : INetworkRunnerCallbacks
        {
            private readonly GameManager _owner;

            public GameManagerRunnerCallbacks(GameManager owner) => _owner = owner;

            public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
            {
                if (_owner != null && _owner.HasStateAuthority)
                    CharacterSelectBridge.NotifySlotsChanged();
            }

            public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
            {
                if (_owner != null && _owner.HasStateAuthority)
                    CharacterSelectBridge.NotifySlotsChanged();
            }

            public void OnInput(NetworkRunner runner, NetworkInput input)
            {
            }

            public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
            {
            }

            public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
            {
            }

            public void OnConnectedToServer(NetworkRunner runner)
            {
            }

            public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
            {
            }

            public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request,
                byte[] token)
            {
            }

            public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress,
                NetConnectFailedReason reason)
            {
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
                // Not used for Dedicated/Host coursework; avoid duplicate callback registration.
                if (runner == null || _owner == null || _owner._runnerCallbacks == null)
                    return;

                runner.RemoveCallbacks(_owner._runnerCallbacks);
                runner.AddCallbacks(_owner._runnerCallbacks);
                _owner.AfterHostMigration();
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
            }

            public void OnSceneLoadStart(NetworkRunner runner)
            {
            }
        }

        #endregion
    }
}
