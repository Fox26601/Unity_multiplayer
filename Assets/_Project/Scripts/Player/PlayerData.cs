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
            PlayerRegistry.RegisterPlayerData(this);
            PlayerRegistry.NotifyReconnectTokenChanged(this);

            if (GameManager.Instance != null)
                ReconnectService.RestorePlayer(Object.Runner, player, token.ToString());
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RpcNicknameRejected([RpcTarget] PlayerRef target, NetworkString<_32> nick)
        {
            if (Runner == null || Runner.LocalPlayer != target)
                return;

            ConnectionManager.NotifyNicknameRejected(UiCopy.NicknameTakenDetail(nick.ToString()));
        }

        /// <summary>Nickname must be unique among active human players in the room.</summary>
        private static bool IsNicknameTakenByOther(string requested, PlayerRef self) =>
            PlayerRegistry.IsNicknameTakenByOther(requested, self);

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
