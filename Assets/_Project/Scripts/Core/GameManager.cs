using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using FusionMultiplayer.Player;
using UnityEngine;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// Scene authority for character ownership, match timer, game over, and spawn points.
    /// State authority follows Shared Mode master client on this scene object.
    /// </summary>
    public class GameManager : NetworkBehaviour, INetworkRunnerCallbacks, IAfterHostMigration
    {
        public const float MatchDurationSeconds = 300f;
        public const float VoteDurationSeconds = 30f;

        public const int VoteRestart = 0;
        public const int VoteArena = 1;
        public const int VotePlaza = 2;
        public const int VoteRuins = 3;
        public const int VoteOptionCount = 4;

        public static GameManager Instance { get; private set; }

        [SerializeField] private NetworkObject _playerAvatarPrefab;
        [SerializeField] private Transform[] _spawnPoints = new Transform[10];

        /// <summary>Owner per character index (10 slots).</summary>
        [Networked, Capacity(10)]
        private NetworkArray<PlayerRef> CharacterOwners => default;

        [Networked, OnChangedRender(nameof(OnGameOverChanged))]
        public bool IsGameOver { get; set; }

        [Networked] private TickTimer MatchTimer { get; set; }
        [Networked] private TickTimer VoteTimer { get; set; }
        [Networked] private NetworkBool VoteResolved { get; set; }

        private readonly PlayerRef[] _lastOwners = new PlayerRef[10];
        private bool _ownersSnapshotReady;
        private float _nextMigrationSnapshotTime;

        public NetworkObject PlayerAvatarPrefab => _playerAvatarPrefab;

        public Transform GetSpawnPoint(int index)
        {
            if (_spawnPoints == null || index < 0 || index >= _spawnPoints.Length || _spawnPoints[index] == null)
                return transform;
            return _spawnPoints[index];
        }

        public PlayerRef GetCharacterOwner(int index)
        {
            if (index < 0 || index >= 10) return PlayerRef.None;
            if (Object == null || !Object.IsValid) return PlayerRef.None;
            return CharacterOwners.Get(index);
        }

        private bool CanMutateSlots =>
            Runner != null && Runner.IsRunning && Object != null && Object.IsValid;

        /// <summary>True when this NetworkBehaviour is spawned and safe to read [Networked] state.</summary>
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
                for (var i = 0; i < 10; i++)
                    CharacterOwners.Set(i, PlayerRef.None);
                IsGameOver = false;
                VoteResolved = false;
                MatchTimer = TickTimer.CreateFromSeconds(Runner, MatchDurationSeconds);
                VoteTimer = default;
            }

            _ownersSnapshotReady = false;
            Runner.AddCallbacks(this);
            GameSceneReadiness.EnsureSubscribed();
            GameSceneReadiness.NotifyGameManagerReady(this);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || Runner == null || !Runner.IsSharedModeMasterClient)
                return;

            if (!IsGameOver && MatchTimer.Expired(Runner))
                MasterSetGameOver();

            if (IsGameOver && !VoteResolved)
                TryResolveVotes();

            if (Runner.SimulationTime >= _nextMigrationSnapshotTime)
            {
                _nextMigrationSnapshotTime = Runner.SimulationTime + 10f;
                Runner.PushHostMigrationSnapshot();
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
            for (var i = 0; i < 10; i++)
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

            if (Runner != null) Runner.RemoveCallbacks(this);
            base.Despawned(runner, hasState);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (Runner != null) Runner.RemoveCallbacks(this);
        }

        /// <summary>Called from UI to request a character slot (processed on state authority / master).</summary>
        public void RequestCharacter(int index, PlayerRef requester)
        {
            RPC_RequestCharacter(index, requester);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestCharacter(int index, PlayerRef requester)
        {
            if (!CanMutateSlots || !HasStateAuthority) return;
            if (index < 0 || index >= 10) return;

            var owner = CharacterOwners.Get(index);
            if (owner != PlayerRef.None && owner != requester)
            {
                RPC_CharacterRejected(requester, index);
                return;
            }

            CharacterOwners.Set(index, requester);
            var sp = GetSpawnPoint(index);
            RPC_CharacterApproved(requester, index, sp.position, sp.rotation);
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
            for (var i = 0; i < 10; i++)
            {
                if (CharacterOwners.Get(i) == player)
                    CharacterOwners.Set(i, PlayerRef.None);
            }
        }

        public void ReleaseCharacterSlot(int index, PlayerRef player)
        {
            if (!CanMutateSlots || !HasStateAuthority) return;
            if (index < 0 || index >= 10) return;
            if (CharacterOwners.Get(index) == player)
                CharacterOwners.Set(index, PlayerRef.None);
        }

        public void MasterSetGameOver()
        {
            if (!CanMutateSlots || !HasStateAuthority || !Runner.IsSharedModeMasterClient)
                return;

            if (IsGameOver)
                return;

            IsGameOver = true;
            VoteResolved = false;
            VoteTimer = TickTimer.CreateFromSeconds(Runner, VoteDurationSeconds);
            ResetAllEndGameVotes();
            RpcBroadcastGameOver();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RpcBroadcastGameOver()
        {
            GameOverBridge.NotifyStateChanged(true);
        }

        private void TryResolveVotes()
        {
            if (!HasStateAuthority || VoteResolved)
                return;

            CountPlayersAndVotes(out var connected, out var voted, out var counts, out var totalVotes);
            var allVoted = connected > 0 && voted >= connected;
            var timerDone = VoteTimer.ExpiredOrNotRunning(Runner);

            if (!allVoted && !timerDone)
                return;

            if (totalVotes <= 0)
            {
                ApplyVoteResult(GetCurrentMap());
                return;
            }

            var winningOption = PickWinningOption(counts, totalVotes);
            ApplyVoteResult(ResolveMapFromVote(winningOption, GetCurrentMap()));
        }

        private static void CountPlayersAndVotes(out int connected, out int voted, out int[] counts, out int totalVotes)
        {
            connected = 0;
            voted = 0;
            totalVotes = 0;
            counts = new int[VoteOptionCount];

            foreach (var pd in FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
            {
                if (pd.Object == null || !pd.Object.IsValid)
                    continue;

                connected++;
                var vote = pd.EndGameVote;
                if (!IsValidVoteOption(vote))
                    continue;

                voted++;
                totalVotes++;
                counts[vote]++;
            }
        }

        private static int PickWinningOption(int[] counts, int totalVotes)
        {
            var max = -1;
            for (var i = 0; i < counts.Length; i++)
            {
                if (counts[i] > max)
                    max = counts[i];
            }

            var top = new List<int>(VoteOptionCount);
            for (var i = 0; i < counts.Length; i++)
            {
                if (counts[i] == max)
                    top.Add(i);
            }

            if (top.Count == 1 && counts[top[0]] * 2 > totalVotes)
                return top[0];

            return top[Random.Range(0, top.Count)];
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
            for (var i = 0; i < 10; i++)
                CharacterOwners.Set(i, PlayerRef.None);

            foreach (var pd in FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
            {
                if (pd.Object == null || !pd.Object.IsValid)
                    continue;

                if (pd.HasStateAuthority)
                {
                    pd.Score = 0;
                    pd.Deaths = 0;
                    pd.CharacterIndex = -1;
                    pd.EndGameVote = PlayerData.NoEndGameVote;
                }
                else
                {
                    pd.RpcResetMatchStats(pd.Object.InputAuthority);
                }
            }

            foreach (var avatar in FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
            {
                if (avatar.Object == null || !avatar.Object.IsValid || Runner == null)
                    continue;

                Runner.Despawn(avatar.Object);
            }
        }

        private static void ResetAllEndGameVotes()
        {
            foreach (var pd in FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
            {
                if (pd.Object == null || !pd.Object.IsValid)
                    continue;

                if (pd.HasStateAuthority)
                    pd.EndGameVote = PlayerData.NoEndGameVote;
                else
                    pd.RpcClearEndGameVote(pd.Object.InputAuthority);
            }
        }

        #region INetworkRunnerCallbacks

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (HasStateAuthority)
                CharacterSelectBridge.NotifySlotsChanged();
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            ClearSlotsForPlayer(player);
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

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
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
            if (runner != null)
                runner.AddCallbacks(this);
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

        #endregion
    }

    /// <summary>
    /// Lightweight bridge so RPC target code can notify UI without hard Fusion references in UI assembly patterns.
    /// </summary>
    public static class CharacterSelectBridge
    {
        public static System.Action<int, Vector3, Quaternion> Approved;
        public static System.Action<int> Rejected;

        public static System.Action SlotsChanged;

        public static void NotifyApproved(int index, Vector3 pos, Quaternion rot) => Approved?.Invoke(index, pos, rot);
        public static void NotifyRejected(int index) => Rejected?.Invoke(index);
        public static void NotifySlotsChanged() => SlotsChanged?.Invoke();
    }
}
