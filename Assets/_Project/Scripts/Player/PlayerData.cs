using Fusion;
using FusionMultiplayer.Core;
using UnityEngine;

namespace FusionMultiplayer.Player
{
    /// <summary>
    /// Per-player lobby/session profile replicated to all peers.
    /// </summary>
    public class PlayerData : NetworkBehaviour
    {
        public const int NoEndGameVote = -1;

        [Networked] public NetworkString<_32> Nick { get; set; }
        [Networked] public Color Tint { get; set; }
        /// <summary>Selected character slot in game scene, or -1 if none.</summary>
        [Networked] public int CharacterIndex { get; set; }
        /// <summary>Combat kills — hidden from HUD, shown on Tab leaderboard and game-over.</summary>
        [Networked] public int Score { get; set; }
        [Networked] public int Deaths { get; set; }
        /// <summary>End-game map vote option, or <see cref="NoEndGameVote"/>.</summary>
        [Networked] public int EndGameVote { get; set; }

        public override void Spawned()
        {
            CharacterIndex = -1;
            if (HasStateAuthority)
            {
                Nick = SessionData.Nickname;
                Tint = SessionData.Tint;
                EndGameVote = NoEndGameVote;
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RpcAwardScore(int amount, PlayerRef expectedOwner)
        {
            if (amount <= 0 || expectedOwner != Object.InputAuthority)
                return;

            Score += amount;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RpcRegisterDeath(int amount, PlayerRef expectedOwner)
        {
            if (amount <= 0 || expectedOwner != Object.InputAuthority)
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
            if (!HasStateAuthority || expectedOwner != Object.InputAuthority)
                return;

            Score = 0;
            Deaths = 0;
            CharacterIndex = -1;
            EndGameVote = NoEndGameVote;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RpcClearEndGameVote(PlayerRef expectedOwner)
        {
            if (!HasStateAuthority || expectedOwner != Object.InputAuthority)
                return;

            EndGameVote = NoEndGameVote;
        }
    }
}
