using Fusion;
using FusionMultiplayer.Data;
using FusionMultiplayer.Player;
using UnityEngine;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// Shared-master scene authority: character slots, match timer, votes, spawn, JSON config RPC.
    /// </summary>
    public partial class GameManager : NetworkBehaviour, IAfterHostMigration
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

        public static float ActiveMatchDurationSeconds =>
            GameConfigs.EnsureCurrent()?.Match != null
                ? GameConfigs.Current.Match.MatchDurationSeconds
                : MatchDurationSeconds;

        public static float ActiveVoteDurationSeconds =>
            GameConfigs.EnsureCurrent()?.Match != null
                ? GameConfigs.Current.Match.VoteDurationSeconds
                : VoteDurationSeconds;

        [Networked, Capacity(10)]
        private NetworkArray<PlayerRef> CharacterOwners => default;

        [Networked, OnChangedRender(nameof(OnGameOverChanged))]
        public bool IsGameOver { get; set; }

        [Networked] private TickTimer MatchTimer { get; set; }
        [Networked] private TickTimer VoteTimer { get; set; }
        [Networked] private NetworkBool VoteResolved { get; set; }

        /// <summary>Master-rolled weather/event id (0 calm, 1 storm, 2 low gravity).</summary>
        [Networked] public int MatchEventId { get; set; }

        /// <summary>Last JSON match-config ack payload length (debug).</summary>
        [Networked] public int LastConfigJsonLength { get; set; }

        private readonly PlayerRef[] _lastOwners = new PlayerRef[PlayerRegistry.MaxCharacterSlots];
        private bool _ownersSnapshotReady;
        private TickTimer _physicsPropTimer;
        private bool _physicsPropsArmed;
        private GameManagerRunnerCallbacks _runnerCallbacks;
        private bool _wasSharedMaster;
        private bool _sharedMasterTrackingReady;

        public NetworkObject PlayerAvatarPrefab => _playerAvatarPrefab;

        public Transform GetSpawnPoint(int index)
        {
            if (_spawnPoints == null || index < 0 || index >= _spawnPoints.Length || _spawnPoints[index] == null)
                return transform;
            return _spawnPoints[index];
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
                seconds = ActiveMatchDurationSeconds;
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
                seconds = ActiveVoteDurationSeconds;
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
                MatchTimer = TickTimer.CreateFromSeconds(Runner, ActiveMatchDurationSeconds);
                VoteTimer = default;
                // Master random decision #2: match event / weather.
                MatchEventId = Random.Range(0, 3);
                _physicsPropsArmed = false;
                _physicsPropTimer = default;
                EnsurePhysicsPropPrefab();
            }

            _ownersSnapshotReady = false;
            _sharedMasterTrackingReady = false;
            _wasSharedMaster = false;
            _runnerCallbacks ??= new GameManagerRunnerCallbacks(this);
            Runner.AddCallbacks(_runnerCallbacks);
            GameSceneReadiness.EnsureSubscribed();
            GameSceneReadiness.NotifyGameManagerReady(this);
            // Initial master (session creator) also runs the become-master path once tracking is primed.
            TrackSharedMasterTransition(forceInitial: true);
        }

        public override void FixedUpdateNetwork()
        {
            TrackSharedMasterTransition(forceInitial: false);

            if (Runner != null && NetworkAuthority.CanMutateSession(Runner))
            {
                SessionLock.TryFulfillPendingReconnectWindow(Runner);
                SessionLock.CloseReconnectWindowIfDue(Runner);
            }

            if (!HasStateAuthority || Runner == null)
                return;

            if (ReconnectService.HasPending)
                ReconnectService.FlushPending(Runner);

            BotTakeover.TickPendingFlags(Runner);

            if (!IsGameOver && MatchTimer.Expired(Runner))
                MasterSetGameOver();

            if (IsGameOver && !VoteResolved)
                TryResolveVotes();

            if (!IsGameOver)
                TickPhysicsPropSpawns();
        }

        /// <summary>
        /// Google Doc #6: Shared Mode master change — detect SA/master transition and refresh local UI/state.
        /// Primary path for Shared; Host Migration callbacks are secondary.
        /// </summary>
        private void TrackSharedMasterTransition(bool forceInitial)
        {
            if (Runner == null || !Runner.IsRunning || Object == null || !Object.IsValid)
                return;

            var isMaster = NetworkAuthority.IsSessionAuthority(Runner) && HasStateAuthority;
            if (!_sharedMasterTrackingReady)
            {
                _sharedMasterTrackingReady = true;
                _wasSharedMaster = isMaster;
                if (forceInitial && isMaster)
                    OnBecameSharedMaster(initial: true);
                return;
            }

            if (isMaster && !_wasSharedMaster)
                OnBecameSharedMaster(initial: false);

            _wasSharedMaster = isMaster;
        }

        private void OnBecameSharedMaster(bool initial)
        {
            CharacterSelectBridge.NotifySlotsChanged();
            if (IsNetworkActive)
                GameOverBridge.NotifyStateChanged(IsGameOver);

            // Reclaim seats that waited for GameManager SA; flush deferred bots / window after promote.
            ReconnectService.FlushPending(Runner);
            if (!initial || BotTakeover.HasPendingTakeovers)
                BotTakeover.FlushPending(Runner);
            if (!initial)
                SessionLock.TryFulfillPendingReconnectWindow(Runner);

            Debug.Log(initial
                ? "[FusionMultiplayer] Shared Mode master ready (GameManager StateAuthority)"
                : "[FusionMultiplayer] Became Shared Mode master — match state retained from networked fields");
        }

        public void AfterHostMigration()
        {
            // Host Migration API (not primary Shared path); still refresh UI if it fires.
            OnBecameSharedMaster(initial: false);
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
    }
}
