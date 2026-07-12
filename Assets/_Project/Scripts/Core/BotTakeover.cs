using Fusion;
using FusionMultiplayer.Player;
using UnityEngine;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// When a human disconnects, the server keeps their avatar and drives it with a basic bot brain.
    /// Decision logic lives here; the avatar only executes movement/fire.
    /// </summary>
    public static class BotTakeover
    {
        public static void TryReplaceDisconnectedPlayer(NetworkRunner runner, PlayerRef player)
        {
            if (runner == null || !NetworkAuthority.IsServerOrHost(runner) || player == PlayerRef.None)
                return;

            // Fusion clears InputAuthority before/during OnPlayerLeft — resolve via character slot.
            var avatar = PlayerOwnership.FindAvatar(player);
            if (avatar == null || avatar.Object == null || !avatar.Object.IsValid)
            {
                Debug.LogWarning(
                    $"[FusionMultiplayer] Bot takeover skipped — no avatar for disconnected player {player.PlayerId}");
                return;
            }

            var brain = avatar.GetComponent<BotBrain>();
            if (brain == null)
                brain = avatar.gameObject.AddComponent<BotBrain>();

            brain.Activate(player);

            var pd = PlayerOwnership.FindPlayerData(player, avatar.CharacterSlot);
            if (pd != null && pd.HasStateAuthority)
            {
                pd.IsBotControlled = true;
                var nick = pd.Nick.ToString();
                if (!nick.StartsWith("BOT "))
                    pd.Nick = "BOT " + nick;
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
    }
}
