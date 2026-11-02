using System.Collections.Generic;
using Fusion;
using FusionMultiplayer.Player;
using UnityEngine;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// When a human disconnects, the Shared master keeps their avatar and drives it with a basic bot brain.
    /// Decision logic lives here; the avatar only executes movement/fire.
    /// </summary>
    public static class BotTakeover
    {
        private static readonly List<PlayerRef> PendingLeavers = new(8);
        private static readonly List<PlayerRef> PendingBotFlags = new(8);

        public static bool HasPendingTakeovers => PendingLeavers.Count > 0;

        public static void TryReplaceDisconnectedPlayer(NetworkRunner runner, PlayerRef player)
        {
            if (runner == null || player == PlayerRef.None)
                return;

            if (!NetworkAuthority.IsSessionAuthority(runner))
            {
                EnqueuePending(player);
                Debug.Log(
                    $"[FusionMultiplayer] Bot takeover deferred for player {player.PlayerId} (waiting for Shared master)");
                return;
            }

            ApplyTakeover(runner, player);
        }

        /// <summary>New Shared master: apply bot brains for leavers queued before authority arrived.</summary>
        public static void FlushPending(NetworkRunner runner)
        {
            if (runner == null || !NetworkAuthority.IsSessionAuthority(runner) || PendingLeavers.Count == 0)
                return;

            var copy = PendingLeavers.ToArray();
            PendingLeavers.Clear();
            for (var i = 0; i < copy.Length; i++)
                ApplyTakeover(runner, copy[i]);
        }

        /// <summary>
        /// Retry IsBotControlled / BOT nick when PlayerData SA arrives after RequestStateAuthority.
        /// </summary>
        public static void TickPendingFlags(NetworkRunner runner)
        {
            if (runner == null || !runner.IsRunning || !NetworkAuthority.IsSessionAuthority(runner) ||
                PendingBotFlags.Count == 0)
                return;

            for (var i = PendingBotFlags.Count - 1; i >= 0; i--)
            {
                var player = PendingBotFlags[i];
                if (player == PlayerRef.None || IsPlayerStillConnected(runner, player))
                {
                    PendingBotFlags.RemoveAt(i);
                    continue;
                }

                var avatar = PlayerOwnership.FindAvatar(player);
                var pd = avatar != null
                    ? PlayerOwnership.FindPlayerData(player, avatar.CharacterSlot)
                    : PlayerOwnership.FindPlayerData(player);

                if (pd == null || pd.Object == null || !pd.Object.IsValid)
                    continue;

                if (!pd.HasStateAuthority)
                {
                    pd.Object.RequestStateAuthority();
                    continue;
                }

                ApplyBotFlag(pd, avatar, player);
                PendingBotFlags.RemoveAt(i);
            }
        }

        private static void EnqueuePending(PlayerRef player)
        {
            for (var i = 0; i < PendingLeavers.Count; i++)
            {
                if (PendingLeavers[i] == player)
                    return;
            }

            PendingLeavers.Add(player);
        }

        private static void EnqueueBotFlag(PlayerRef player)
        {
            for (var i = 0; i < PendingBotFlags.Count; i++)
            {
                if (PendingBotFlags[i] == player)
                    return;
            }

            PendingBotFlags.Add(player);
        }

        private static void ApplyTakeover(NetworkRunner runner, PlayerRef player)
        {
            if (player == PlayerRef.None)
                return;

            // Fusion clears InputAuthority before/during OnPlayerLeft — resolve via character slot.
            var avatar = PlayerOwnership.FindAvatar(player);
            if (avatar == null || avatar.Object == null || !avatar.Object.IsValid)
            {
                Debug.LogWarning(
                    $"[FusionMultiplayer] Bot takeover skipped — no avatar for disconnected player {player.PlayerId}");
                return;
            }

            // Shared Mode: after leave, SA typically migrates to master; request if still needed.
            if (!avatar.HasStateAuthority)
                avatar.Object.RequestStateAuthority();

            var brain = avatar.GetComponent<BotBrain>();
            if (brain == null)
                brain = avatar.gameObject.AddComponent<BotBrain>();

            brain.Activate(player);

            var pd = PlayerOwnership.FindPlayerData(player, avatar.CharacterSlot);
            if (pd != null)
            {
                if (!pd.HasStateAuthority)
                {
                    pd.Object.RequestStateAuthority();
                    EnqueueBotFlag(player);
                }
                else
                {
                    ApplyBotFlag(pd, avatar, player);
                }
            }
            else
            {
                EnqueueBotFlag(player);
            }

            if (avatar.HasStateAuthority)
            {
                var label = pd != null ? pd.Nick.ToString() : avatar.DisplayName.ToString();
                if (string.IsNullOrWhiteSpace(label))
                    label = $"Player {player.PlayerId}";
                if (!label.StartsWith("BOT "))
                    label = "BOT " + label;
                avatar.DisplayName = label;
            }

            Debug.Log($"[FusionMultiplayer] Bot takeover for disconnected player {player.PlayerId}");
        }

        private static void ApplyBotFlag(PlayerData pd, PlayerAvatar avatar, PlayerRef player)
        {
            if (pd == null || !pd.HasStateAuthority)
                return;

            pd.IsBotControlled = true;
            var nick = pd.Nick.ToString();
            if (!nick.StartsWith("BOT "))
                pd.Nick = "BOT " + nick;
            PlayerRegistry.NotifyReconnectTokenChanged(pd);

            if (avatar != null && avatar.Object != null && avatar.Object.IsValid && avatar.HasStateAuthority)
            {
                var label = pd.Nick.ToString();
                if (string.IsNullOrWhiteSpace(label))
                    label = $"Player {player.PlayerId}";
                if (!label.StartsWith("BOT "))
                    label = "BOT " + label;
                avatar.DisplayName = label;
            }
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
