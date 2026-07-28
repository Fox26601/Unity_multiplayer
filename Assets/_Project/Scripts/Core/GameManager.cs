using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using FusionMultiplayer.Player;
using UnityEngine;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// Server-owned scene authority: character slots, match timer, votes, spawn, JSON config RPC.
    /// </summary>
    public class GameManager : NetworkBehaviour, IAfterHostMigration
    {
        public const float MatchDurationSeconds = 300f;
        public const float VoteDurationSeconds = 30f;
        public const float CritChance = CombatRules.CritChance;
        public const float CritMultiplier = CombatRules.CritMultiplier;

        public const int VoteRestart = 0;
        public const int VoteArena = 1;
        public const int VotePlaza = 2;
        public const int VoteRuins = 3;
        public const int VoteOptionCount = 4;

        public static GameManager Instance { get; private set; }

        [SerializeField] private NetworkObject _playerAvatarPrefab;
        [SerializeField] private NetworkObject _physicsPropPrefab;
        [SerializeField] private Transform[] _spawnPoints = new Transform[PlayerRegistry.MaxCharacterSlots];

        [Networked, Capacity(10)]
        private NetworkArray<PlayerRef> CharacterOwners => default;

        [Networked, OnChangedRender(nameof(OnGameOverChanged))]
        public bool IsGameOver { get; set; }

        [Networked] private TickTimer MatchTimer { get; set; }
        [Networked] private TickTimer VoteTimer { get; set; }
        [Networked] private NetworkBool VoteResolved { get; set; }

        /// <summary>Server-rolled weather/event id (0 calm, 1 storm, 2 low gravity).</summary>
        [Networked] public int MatchEventId { get; set; }

        /// <summary>Last JSON match-config ack payload length (debug / assignment proof).</summary>
        [Networked] public int LastConfigJsonLength { get; set; }

        private readonly PlayerRef[] _lastOwners = new PlayerRef[PlayerRegistry.MaxCharacterSlots];
        private bool _ownersSnapshotReady;
        private TickTimer _physicsPropTimer;
        private GameManagerRunnerCallbacks _runnerCallbacks;

        public NetworkObject PlayerAvatarPrefab => _playerAvatarPrefab;

        public Transform GetSpawnPoint(int index)
        {
            if (_spawnPoints == null || index < 0 || index >= _spawnPoints.Length || _spawnPoints[index] == null)
                return transform;
            return _spawnPoints[index];
        }

        /// <summary>Server random decision #1: pick a spawn with a small planar jitter.</summary>
        public void GetRandomizedSpawn(int index, out Vector3 position, out Quaternion rotation)
        {
            var sp = GetSpawnPoint(index);
            var jitter = Random.insideUnitCircle * 1.25f;
            position = sp.position + new Vector3(jitter.x, 0f, jitter.y);
            rotation = sp.rotation;
        }

        public PlayerRef GetCharacterOwner(int index)
        {
            if (index < 0 || index >= PlayerRegistry.MaxCharacterSlots) return PlayerRef.None;
            if (Object == null || !Object.IsValid) return PlayerRef.None;
            return CharacterOwners.Get(index);
        }

        public void SetCharacterOwner(int index, PlayerRef player)
        {
            if (!CanMutateSlots || !HasStateAuthority) return;
            if (index < 0 || index >= PlayerRegistry.MaxCharacterSlots) return;
            CharacterOwners.Set(index, player);
        }

        private bool CanMutateSlots =>
            Runner != null && Runner.IsRunning && Object != null && Object.IsValid;

        public bool IsNetworkActive => Object != null && Object.IsValid;

        public bool GetIsGameOverSafe()
        {
            if (!IsNetworkActive) return false;
            return IsGameOver;
        }

        public bool TryGetMatchRemaining(out float seconds)
        {
            seconds = 0f;
            if (!IsNetworkActive || IsGameOver)
                return false;

            if (!MatchTimer.IsRunning)
            {
                seconds = MatchDurationSeconds;
                return true;
            }

            var remaining = MatchTimer.RemainingTime(Runner);
            if (!remaining.HasValue)
                return false;

            seconds = Mathf.Max(0f, remaining.Value);
            return true;
        }

        public bool TryGetVoteRemaining(out float seconds)
        {
            seconds = 0f;
            if (!IsNetworkActive || !IsGameOver || VoteResolved)
                return false;

            if (!VoteTimer.IsRunning)
            {
                seconds = VoteDurationSeconds;
                return true;
            }

            var remaining = VoteTimer.RemainingTime(Runner);
            if (!remaining.HasValue)
                return false;

            seconds = Mathf.Max(0f, remaining.Value);
            return true;
        }

        public static bool IsValidVoteOption(int option) =>
            option >= VoteRestart && option < VoteOptionCount;

        public static SessionCatalog.MapKind ResolveMapFromVote(int option, SessionCatalog.MapKind current)
        {
            return option switch
            {
                VoteArena => SessionCatalog.MapKind.Arena,
                VotePlaza => SessionCatalog.MapKind.Plaza,
                VoteRuins => SessionCatalog.MapKind.Ruins,
                _ => current
            };
        }

        public override void Spawned()
        {
            Instance = this;
            GameDiagnostics.LogSpawned(Object != null && Object.IsValid);
            if (HasStateAuthority)
            {
                for (var i = 0; i < PlayerRegistry.MaxCharacterSlots; i++)
                    CharacterOwners.Set(i, PlayerRef.None);
                IsGameOver = false;
                VoteResolved = false;
                MatchTimer = TickTimer.CreateFromSeconds(Runner, MatchDurationSeconds);
                VoteTimer = default;
                // Server random decision #2: match event / weather.
                MatchEventId = Random.Range(0, 3);
                _physicsPropTimer = TickTimer.CreateFromSeconds(Runner, Random.Range(12f, 22f));
                EnsurePhysicsPropPrefab();
            }

            _ownersSnapshotReady = false;
            _runnerCallbacks ??= new GameManagerRunnerCallbacks(this);
            Runner.AddCallbacks(_runnerCallbacks);
            GameSceneReadiness.EnsureSubscribed();
            GameSceneReadiness.NotifyGameManagerReady(this);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || Runner == null)
                return;

            if (!IsGameOver && MatchTimer.Expired(Runner))
                MasterSetGameOver();

            if (IsGameOver && !VoteResolved)
                TryResolveVotes();

            if (!IsGameOver && _physicsPropTimer.ExpiredOrNotRunning(Runner))
            {
                TrySpawnPhysicsProp();
                _physicsPropTimer = TickTimer.CreateFromSeconds(Runner, Random.Range(18f, 30f));
            }
        }

        public void AfterHostMigration()
        {
            CharacterSelectBridge.NotifySlotsChanged();
            if (IsNetworkActive)
                GameOverBridge.NotifyStateChanged(IsGameOver);
        }

        private void OnGameOverChanged()
        {
            GameOverBridge.NotifyStateChanged(IsGameOver);
        }

        public override void Render()
        {
            if (Object == null || !Object.IsValid)
                return;

            var changed = !_ownersSnapshotReady;
            for (var i = 0; i < PlayerRegistry.MaxCharacterSlots; i++)
            {
                var owner = CharacterOwners.Get(i);
                if (!_ownersSnapshotReady || _lastOwners[i] != owner)
                {
                    _lastOwners[i] = owner;
                    changed = true;
                }
            }

            _ownersSnapshotReady = true;
            if (changed)
                CharacterSelectBridge.NotifySlotsChanged();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Instance == this)
            {
                Instance = null;
                GameSceneReadiness.NotifyGameManagerLost();
            }

            if (Runner != null && _runnerCallbacks != null)
                Runner.RemoveCallbacks(_runnerCallbacks);
            base.Despawned(runner, hasState);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (Runner != null && _runnerCallbacks != null)
                Runner.RemoveCallbacks(_runnerCallbacks);
        }

        public void RequestCharacter(int index)
        {
            RPC_RequestCharacter(index);
        }

        /// <summary>Client sends serialized MatchConfigDto JSON (≥3 fields) for server deserialize + ack.</summary>
        public void SubmitMatchConfigJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return;

            var clipped = json.Length > 500 ? json.Substring(0, 500) : json;
            RPC_SubmitMatchConfigJson(clipped);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_SubmitMatchConfigJson(NetworkString<_512> jsonPayload, RpcInfo info = default)
        {
            if (!HasStateAuthority)
                return;

            var json = jsonPayload.ToString();
            LastConfigJsonLength = json.Length;

            if (!MatchConfigDto.TryFromJson(json, out var dto))
            {
                Debug.LogWarning("[FusionMultiplayer] Rejected invalid MatchConfigDto JSON");
                return;
            }

            // Reclaim only when JSON token matches this peer's ConnectionToken.
            if (!string.IsNullOrWhiteSpace(dto.PlayerToken) &&
                SessionReconnectTokens.TryReadPlayerToken(Runner, info.Source, out var joinToken) &&
                string.Equals(joinToken, dto.PlayerToken.Trim(), System.StringComparison.Ordinal))
            {
                TryRestoreReconnect(info.Source, joinToken);
            }

            Debug.Log(
                $"[FusionMultiplayer] Deserialized MatchConfigDto from {info.Source}: room={dto.RoomName}, max={dto.MaxPlayers}, mode={dto.GameMode}, map={dto.Map}, diff={dto.Difficulty}, nick={dto.Nickname}");

            RPC_AckMatchConfigJson(info.Source, json.Length, dto.MaxPlayers, dto.Difficulty);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_AckMatchConfigJson([RpcTarget] PlayerRef target, int jsonLength, int maxPlayers,
            int difficulty)
        {
            if (Runner == null || Runner.LocalPlayer != target)
                return;

            Debug.Log(
                $"[FusionMultiplayer] MatchConfig JSON ack: len={jsonLength}, maxPlayers={maxPlayers}, difficulty={difficulty}");
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestCharacter(int index, RpcInfo info = default)
        {
            if (!CanMutateSlots || !HasStateAuthority) return;
            if (index < 0 || index >= PlayerRegistry.MaxCharacterSlots) return;

            var requester = info.Source;
            if (requester == PlayerRef.None)
                return;

            var owner = CharacterOwners.Get(index);
            if (owner != PlayerRef.None && owner != requester)
            {
                RPC_CharacterRejected(requester, index);
                return;
            }

            // Reconnect: if requester already owns another slot, clear duplicate body.
            for (var i = 0; i < PlayerRegistry.MaxCharacterSlots; i++)
            {
                if (i == index) continue;
                if (CharacterOwners.Get(i) == requester)
                {
                    CharacterOwners.Set(i, PlayerRef.None);
                    DespawnAvatarsFor(requester);
                }
            }

            CharacterOwners.Set(index, requester);
            GetRandomizedSpawn(index, out var position, out var rotation);

            var existing = FindAvatarFor(requester);
            if (existing != null)
            {
                existing.CharacterSlot = index;
                existing.transform.SetPositionAndRotation(position, rotation);
            }
            else if (_playerAvatarPrefab != null)
            {
                var spawned = Runner.Spawn(_playerAvatarPrefab, position, rotation, requester);
                if (spawned != null)
                {
                    var pa = spawned.GetComponent<PlayerAvatar>();
                    if (pa != null) pa.CharacterSlot = index;
                }
            }

            var pd = PlayerRegistry.FindPlayerData(requester);
            if (pd != null && pd.HasStateAuthority)
            {
                pd.CharacterIndex = index;
                PlayerRegistry.RegisterPlayerData(pd);
            }

            RPC_CharacterApproved(requester, index, position, rotation);
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RPC_CharacterApproved([RpcTarget] PlayerRef target, int index, Vector3 position,
            Quaternion rotation)
        {
            if (Runner.LocalPlayer != target) return;
            CharacterSelectBridge.NotifyApproved(index, position, rotation);
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RPC_CharacterRejected([RpcTarget] PlayerRef target, int index)
        {
            if (Runner.LocalPlayer != target) return;
            CharacterSelectBridge.NotifyRejected(index);
        }

        public void ClearSlotsForPlayer(PlayerRef player)
        {
            if (!CanMutateSlots || !HasStateAuthority) return;
            for (var i = 0; i < PlayerRegistry.MaxCharacterSlots; i++)
            {
                if (CharacterOwners.Get(i) == player)
                    CharacterOwners.Set(i, PlayerRef.None);
            }
        }

        public void ReleaseCharacterSlot(int index, PlayerRef player)
        {
            if (!CanMutateSlots || !HasStateAuthority) return;
            if (index < 0 || index >= PlayerRegistry.MaxCharacterSlots) return;
            if (CharacterOwners.Get(index) == player)
                CharacterOwners.Set(index, PlayerRef.None);
        }

        public void MasterSetGameOver()
        {
            if (!CanMutateSlots || !HasStateAuthority)
                return;

            if (IsGameOver)
                return;

            IsGameOver = true;
            VoteResolved = false;
            VoteTimer = TickTimer.CreateFromSeconds(Runner, VoteDurationSeconds);
            ResetAllEndGameVotes();
            MatchStatsDatabase.WriteMatchResults();
            RpcBroadcastGameOver();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RpcBroadcastGameOver()
        {
            GameOverBridge.NotifyStateChanged(true);
        }

        /// <summary>Server random decision #3: critical hit roll.</summary>
        public static float RollDamage(float baseDamage) => CombatRules.RollDamage(baseDamage);

        private void TrySpawnPhysicsProp()
        {
            if (_physicsPropPrefab == null || Runner == null)
                return;

            GetRandomizedSpawn(Random.Range(0, PlayerRegistry.MaxCharacterSlots), out var pos, out _);
            pos.y += 3f;
            Runner.Spawn(_physicsPropPrefab, pos, Quaternion.identity, null);
        }

        private void EnsurePhysicsPropPrefab()
        {
            // Prefab must be assigned in the scene / via Tools → Wire PhysicsProp And Boot Only.
            if (_physicsPropPrefab == null)
                Debug.LogWarning("[FusionMultiplayer] PhysicsProp prefab is not assigned on GameManager.");
        }

        public void TryRestoreReconnect(PlayerRef player, string token)
        {
            if (!HasStateAuthority || Runner == null)
                return;

            ReconnectService.RestorePlayer(Runner, player, token);
        }

        private static PlayerAvatar FindAvatarFor(PlayerRef player) =>
            PlayerOwnership.FindAvatar(player);

        private void DespawnAvatarsFor(PlayerRef player)
        {
            foreach (var avatar in PlayerRegistry.EnumerateAllAvatars())
            {
                if (avatar.Object == null || !avatar.Object.IsValid)
                    continue;
                if (avatar.Object.InputAuthority != player)
                    continue;
                Runner.Despawn(avatar.Object);
            }
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

            foreach (var pd in PlayerRegistry.EnumerateAllData())
            {
                if (pd.Object == null || !pd.Object.IsValid)
                    continue;

                pd.ServerResetMatchStats();
            }

            foreach (var avatar in PlayerRegistry.EnumerateAllAvatars())
            {
                if (avatar.Object == null || !avatar.Object.IsValid || Runner == null)
                    continue;

                Runner.Despawn(avatar.Object);
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
