using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using FusionMultiplayer.Player;
using UnityEngine;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// Scene authority for character ownership, game over state, and spawn points.
    /// State authority follows Shared Mode master client on this scene object.
    /// </summary>
    public class GameManager : NetworkBehaviour, INetworkRunnerCallbacks
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private NetworkObject _playerAvatarPrefab;
        [SerializeField] private Transform[] _spawnPoints = new Transform[10];

        /// <summary>Owner per character index (10 slots).</summary>
        [Networked, Capacity(10)]
        private NetworkArray<PlayerRef> CharacterOwners => default;

        [Networked] public bool IsGameOver { get; set; }

        private readonly PlayerRef[] _lastOwners = new PlayerRef[10];
        private bool _ownersSnapshotReady;

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

        public override void Spawned()
        {
            Instance = this;
            GameDiagnostics.LogSpawned(Object != null && Object.IsValid);
            if (HasStateAuthority)
            {
                for (var i = 0; i < 10; i++)
                    CharacterOwners.Set(i, PlayerRef.None);
                IsGameOver = false;
            }

            _ownersSnapshotReady = false;
            Runner.AddCallbacks(this);
            GameSceneReadiness.EnsureSubscribed();
            GameSceneReadiness.NotifyGameManagerReady(this);
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
            if (!CanMutateSlots || !HasStateAuthority) return;
            IsGameOver = true;
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
