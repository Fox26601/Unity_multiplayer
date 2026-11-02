using Fusion;
using FusionMultiplayer.Core;
using FusionMultiplayer.UI;
using UnityEngine;

namespace FusionMultiplayer.Player
{
    /// <summary>
    /// Per-player lobby/session profile (Shared Mode: owning player SA+IA).
    /// </summary>
    public class PlayerData : NetworkBehaviour
    {
        public const int NoEndGameVote = -1;

        [Networked] public NetworkString<_32> Nick { get; set; }
        [Networked] public Color Tint { get; set; }
        [Networked, OnChangedRender(nameof(OnCharacterIndexChanged))]
        public int CharacterIndex { get; set; }
        [Networked] public int Score { get; set; }
        [Networked] public int Deaths { get; set; }
        [Networked] public int EndGameVote { get; set; }
        [Networked] public NetworkString<_64> ReconnectToken { get; set; }
        [Networked] public NetworkBool IsBotControlled { get; set; }

        private void OnCharacterIndexChanged()
        {
            PlayerRegistry.NotifyCharacterIndexChanged(this);
        }

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                CharacterIndex = -1;
                EndGameVote = NoEndGameVote;
                IsBotControlled = false;
                if (string.IsNullOrWhiteSpace(Nick.ToString()))
                {
                    Nick = "Player";
                    Tint = Color.white;
                }
            }
            else if (Object.InputAuthority == Runner.LocalPlayer)
            {
                // Shared reconnect: reclaim SA after master AssignInputAuthority.
                Object.RequestStateAuthority();
            }

            PlayerRegistry.RegisterPlayerData(this);

            if (HasInputAuthority)
            {
                var token = SessionData.EnsureReconnectToken();
                RpcSubmitProfile(SessionData.Nickname ?? "Player", SessionData.Tint, token);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            PlayerRegistry.UnregisterPlayerData(this);
            base.Despawned(runner, hasState);
        }

        private bool _configSubmitted;
        private bool _needsProfileRefresh;

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority && Object != null && Object.IsValid &&
                Object.InputAuthority == Runner.LocalPlayer)
            {
                Object.RequestStateAuthority();
            }

            if (_needsProfileRefresh && Object != null && Object.IsValid &&
                Object.InputAuthority == Runner.LocalPlayer)
            {
                _needsProfileRefresh = false;
                var token = SessionData.EnsureReconnectToken();
                RpcSubmitProfile(SessionData.Nickname ?? "Player", SessionData.Tint, token);
            }

            if (!HasInputAuthority || GameManager.Instance == null || !GameManager.Instance.IsNetworkActive)
                return;

            if (_configSubmitted)
                return;

            _configSubmitted = true;
            SubmitMatchConfig();
        }

        private void SubmitMatchConfig()
        {
            var room = ConnectionManager.Instance != null
                ? ConnectionManager.Instance.ActiveRoomName
                : "unknown";
            var dto = MatchConfigDto.FromSessionData(room);
            var json = dto.ToJson();
            if (GameManager.Instance != null)
                GameManager.Instance.SubmitMatchConfigJson(json);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcSubmitProfile(NetworkString<_32> nick, Color tint, NetworkString<_64> token)
        {
            if (!HasStateAuthority)
                return;

            var requested = string.IsNullOrWhiteSpace(nick.ToString()) ? "Player" : nick.ToString().Trim();
            if (requested.StartsWith("BOT "))
                requested = requested.Substring(4).Trim();
            if (string.IsNullOrWhiteSpace(requested))
                requested = "Player";

            var player = Object.InputAuthority;
            var submittedToken = token.ToString().Trim();
            // Same-token seat reclaim: never treat this seat's nick as a conflict.
            var excludeToken = string.IsNullOrWhiteSpace(submittedToken) ? null : submittedToken;

            if (IsNicknameTakenByOther(requested, player, excludeToken))
            {
                Debug.LogWarning(
                    $"[FusionMultiplayer] Rejected nickname \"{requested}\" for player {player.PlayerId} — already taken.");
                RpcNicknameRejected(player, requested);
                ConnectionManager.Instance?.RejectJoiningPlayer(player, Object);
                return;
            }

            Nick = requested;
            Tint = tint;
            ReconnectToken = token;
            IsBotControlled = false;
            PlayerRegistry.RegisterPlayerData(this);
            PlayerRegistry.NotifyReconnectTokenChanged(this);
            // Reclaim is master provision / ReconnectService.FlushPending — not from a fresh spawn.
        }

        /// <summary>
        /// Master finished AssignInputAuthority; tell the reclaiming client to take SA and refresh profile.
        /// </summary>
        public void NotifyClientReclaimAuthority()
        {
            if (Object == null || !Object.IsValid)
                return;

            var player = Object.InputAuthority;
            if (player == PlayerRef.None)
                return;

            if (Runner != null && Runner.LocalPlayer == player)
            {
                ApplyLocalReclaimAuthority();
                return;
            }

            if (HasStateAuthority)
                RPC_RequestReclaimAuthority(player);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_RequestReclaimAuthority([RpcTarget] PlayerRef target)
        {
            if (Runner == null || Runner.LocalPlayer != target)
                return;

            ApplyLocalReclaimAuthority();
        }

        private void ApplyLocalReclaimAuthority()
        {
            if (Object == null || !Object.IsValid)
                return;

            if (!HasStateAuthority)
                Object.RequestStateAuthority();

            var avatar = PlayerOwnership.FindAvatar(Object.InputAuthority) ??
                         PlayerOwnership.FindAvatarBySlot(CharacterIndex);
            if (avatar != null && avatar.Object != null && avatar.Object.IsValid && !avatar.HasStateAuthority)
                avatar.Object.RequestStateAuthority();

            _needsProfileRefresh = true;
            if (Object.InputAuthority == Runner.LocalPlayer)
            {
                _needsProfileRefresh = false;
                var token = SessionData.EnsureReconnectToken();
                RpcSubmitProfile(SessionData.Nickname ?? "Player", SessionData.Tint, token);
            }
        }

        /// <summary>SA owner despawns a duplicate empty PlayerData left by a bad reconnect spawn.</summary>
        public void RequestDespawnAsDuplicate()
        {
            if (Object == null || !Object.IsValid || Runner == null)
                return;

            if (HasStateAuthority)
            {
                Runner.Despawn(Object);
                return;
            }

            RPC_DespawnAsDuplicate();
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_DespawnAsDuplicate()
        {
            if (!HasStateAuthority || Object == null || !Object.IsValid || Runner == null)
                return;

            Runner.Despawn(Object);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RpcNicknameRejected([RpcTarget] PlayerRef target, NetworkString<_32> nick)
        {
            if (Runner == null || Runner.LocalPlayer != target)
                return;

            ConnectionManager.NotifyNicknameRejected(UiCopy.NicknameTakenDetail(nick.ToString()));
        }

        /// <summary>Nickname must be unique among active human players in the room.</summary>
        private static bool IsNicknameTakenByOther(string requested, PlayerRef self, string excludeToken = null) =>
            PlayerRegistry.IsNicknameTakenByOther(requested, self, excludeToken);

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcRequestStartMatch()
        {
            if (!HasStateAuthority)
                return;

            ConnectionManager.Instance?.ServerStartMatch();
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcCastEndGameVote(int option)
        {
            if (!HasStateAuthority || !GameManager.IsValidVoteOption(option))
                return;

            var gm = GameManager.Instance;
            if (gm == null || !gm.GetIsGameOverSafe())
                return;

            if (EndGameVote != NoEndGameVote)
                return;

            EndGameVote = option;
        }

        /// <summary>StateAuthority-only score mutation (combat server path).</summary>
        public void ServerAwardScore(int amount)
        {
            if (!HasStateAuthority || amount <= 0)
                return;

            Score += amount;
        }

        /// <summary>StateAuthority-only death counter mutation.</summary>
        public void ServerRegisterDeath(int amount)
        {
            if (!HasStateAuthority || amount <= 0)
                return;

            Deaths += amount;
        }

        /// <summary>StateAuthority-only match reset (map vote restart).</summary>
        public void ServerResetMatchStats()
        {
            if (!HasStateAuthority)
                return;

            Score = 0;
            Deaths = 0;
            CharacterIndex = -1;
            EndGameVote = NoEndGameVote;
            IsBotControlled = false;
            var nick = Nick.ToString();
            if (nick.StartsWith("BOT "))
                Nick = nick.Substring(4);
            PlayerRegistry.NotifyCharacterIndexChanged(this);
            PlayerRegistry.NotifyReconnectTokenChanged(this);
        }

        /// <summary>StateAuthority-only clear of end-game vote.</summary>
        public void ServerClearEndGameVote()
        {
            if (!HasStateAuthority)
                return;

            EndGameVote = NoEndGameVote;
        }
    }
}
