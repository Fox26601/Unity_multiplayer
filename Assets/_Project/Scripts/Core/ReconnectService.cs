using Fusion;
using FusionMultiplayer.Player;
using UnityEngine;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// Single orchestrator for crash-reconnect: reclaim PlayerData, restore avatar InputAuthority,
    /// reassign character slot, and deactivate bot takeover.
    /// </summary>
    public static class ReconnectService
    {
        /// <summary>
        /// Restores a reconnecting player. Returns true when PlayerData was reclaimed
        /// (caller should not spawn a fresh PlayerData).
        /// </summary>
        public static bool RestorePlayer(NetworkRunner runner, PlayerRef player, string token)
        {
            if (runner == null || player == PlayerRef.None || string.IsNullOrWhiteSpace(token))
                return false;

            if (!NetworkAuthority.IsServerOrHost(runner))
                return false;

            var pd = PlayerRegistry.FindPlayerDataByToken(token);
            if (pd == null || pd.Object == null || !pd.Object.IsValid)
                return false;

            var oldDataOwner = pd.Object.InputAuthority;
            if (oldDataOwner == player)
            {
                RestoreAvatarControl(player, pd, oldDataOwner);
                return true;
            }

            if (!CanReclaimSeat(runner, pd, oldDataOwner))
                return false;

            pd.Object.AssignInputAuthority(player);
            PlayerRegistry.NotifyPlayerDataAuthorityChanged(pd, oldDataOwner);

            if (pd.HasStateAuthority)
            {
                pd.IsBotControlled = false;
                var nick = pd.Nick.ToString();
                if (nick.StartsWith("BOT "))
                    pd.Nick = nick.Substring(4);
            }

            RestoreAvatarControl(player, pd, oldDataOwner);
            Debug.Log($"[FusionMultiplayer] Reconnect restored player {player.PlayerId}");
            return true;
        }

        private static void RestoreAvatarControl(PlayerRef player, PlayerData pd, PlayerRef oldDataOwner)
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.HasStateAuthority)
                return;

            var avatar = PlayerOwnership.FindAvatar(oldDataOwner) ??
                         PlayerOwnership.FindAvatar(player) ??
                         PlayerOwnership.FindAvatarBySlot(pd.CharacterIndex);

            if (avatar == null || avatar.Object == null || !avatar.Object.IsValid)
                return;

            var previousAvatarOwner = avatar.Object.InputAuthority;
            if (previousAvatarOwner != player)
            {
                avatar.Object.AssignInputAuthority(player);
                PlayerRegistry.NotifyAvatarAuthorityOrSlotChanged(avatar, previousAvatarOwner);
            }

            if (avatar.CharacterSlot >= 0 && avatar.CharacterSlot < PlayerRegistry.MaxCharacterSlots)
                gm.SetCharacterOwner(avatar.CharacterSlot, player);

            var brain = avatar.GetComponent<BotBrain>();
            if (brain != null)
                brain.Deactivate();

            if (pd.HasStateAuthority)
                pd.IsBotControlled = false;
        }

        private static bool CanReclaimSeat(NetworkRunner runner, PlayerData pd, PlayerRef oldOwner)
        {
            if (pd.IsBotControlled || oldOwner == PlayerRef.None)
                return true;

            return !IsPlayerStillConnected(runner, oldOwner);
        }

        private static bool IsPlayerStillConnected(NetworkRunner runner, PlayerRef player)
        {
            if (runner == null || player == PlayerRef.None)
                return false;

            foreach (var active in runner.ActivePlayers)
            {
                if (active == player)
                    return true;
            }

            return false;
        }
    }
}
