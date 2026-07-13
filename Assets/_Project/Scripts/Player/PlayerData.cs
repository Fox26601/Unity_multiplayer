using System;
using Fusion;
using FusionMultiplayer.Core;
using FusionMultiplayer.UI;
using UnityEngine;

namespace FusionMultiplayer.Player
{
    /// <summary>
    /// Per-player lobby/session profile replicated from the server.
    /// </summary>
    public class PlayerData : NetworkBehaviour
    {
        public const int NoEndGameVote = -1;

        [Networked] public NetworkString<_32> Nick { get; set; }
        [Networked] public Color Tint { get; set; }
        [Networked] public int CharacterIndex { get; set; }
        [Networked] public int Score { get; set; }
        [Networked] public int Deaths { get; set; }
        [Networked] public int EndGameVote { get; set; }
        [Networked] public NetworkString<_64> ReconnectToken { get; set; }
        [Networked] public NetworkBool IsBotControlled { get; set; }

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

            if (HasInputAuthority)
            {
                var token = SessionData.EnsureReconnectToken();
                RpcSubmitProfile(SessionData.Nickname ?? "Player", SessionData.Tint, token);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasInputAuthority || GameManager.Instance == null || !GameManager.Instance.IsNetworkActive)
                return;

            if (_configSubmitted)
                return;

            _configSubmitted = true;
            SubmitMatchConfig();
        }

        private bool _configSubmitted;

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
            var player = Object.InputAuthority;

            if (IsNicknameTakenByOther(requested, player))
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

            if (GameManager.Instance != null)
                GameManager.Instance.TryRestoreReconnect(player, token.ToString());
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RpcNicknameRejected([RpcTarget] PlayerRef target, NetworkString<_32> nick)
        {
            if (Runner == null || Runner.LocalPlayer != target)
                return;

            ConnectionManager.NotifyNicknameRejected(UiCopy.NicknameTakenDetail(nick.ToString()));
        }

        /// <summary>Nickname must be unique among active human players in the room.</summary>
        private static bool IsNicknameTakenByOther(string requested, PlayerRef self)
        {
            var canonical = CanonicalNickname(requested);
            if (string.IsNullOrEmpty(canonical))
                return false;

            foreach (var other in FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
            {
                if (other == null || other.Object == null || !other.Object.IsValid)
                    continue;
                if (other.IsBotControlled)
                    continue;

                var owner = other.Object.InputAuthority;
                if (owner == PlayerRef.None || owner == self)
                    continue;

                if (string.Equals(CanonicalNickname(other.Nick.ToString()), canonical,
                        StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string CanonicalNickname(string nick)
        {
            if (string.IsNullOrWhiteSpace(nick))
                return string.Empty;

            return nick.Trim();
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcRequestStartMatch()
        {
            if (!HasStateAuthority)
                return;

            ConnectionManager.Instance?.ServerStartMatch();
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RpcAwardScore(int amount, PlayerRef expectedOwner)
        {
            if (amount <= 0 || !OwnsLogicalPlayer(expectedOwner))
                return;

            Score += amount;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RpcRegisterDeath(int amount, PlayerRef expectedOwner)
        {
            if (amount <= 0 || !OwnsLogicalPlayer(expectedOwner))
                return;

            Deaths += amount;
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

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RpcResetMatchStats(PlayerRef expectedOwner)
        {
            if (!HasStateAuthority || !OwnsLogicalPlayer(expectedOwner))
                return;

            Score = 0;
            Deaths = 0;
            CharacterIndex = -1;
            EndGameVote = NoEndGameVote;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RpcClearEndGameVote(PlayerRef expectedOwner)
        {
            if (!HasStateAuthority || !OwnsLogicalPlayer(expectedOwner))
                return;

            EndGameVote = NoEndGameVote;
        }

        private bool OwnsLogicalPlayer(PlayerRef expectedOwner)
        {
            if (expectedOwner == PlayerRef.None || Object == null || !Object.IsValid)
                return false;

            if (Object.InputAuthority == expectedOwner)
                return true;

            // After disconnect InputAuthority is cleared; slot ownership still maps to the left player.
            if (Object.InputAuthority != PlayerRef.None)
                return false;

            var gm = GameManager.Instance;
            return gm != null &&
                   CharacterIndex >= 0 &&
                   CharacterIndex < 10 &&
                   gm.GetCharacterOwner(CharacterIndex) == expectedOwner;
        }
    }
}
