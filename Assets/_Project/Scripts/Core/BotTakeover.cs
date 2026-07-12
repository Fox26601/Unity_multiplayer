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

            PlayerAvatar avatar = null;
            foreach (var candidate in Object.FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
            {
                if (candidate.Object != null && candidate.Object.IsValid &&
                    candidate.Object.InputAuthority == player)
                {
                    avatar = candidate;
                    break;
                }
            }

            if (avatar == null)
                return;

            var brain = avatar.GetComponent<BotBrain>();
            if (brain == null)
                brain = avatar.gameObject.AddComponent<BotBrain>();

            brain.Activate(player);

            foreach (var pd in Object.FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
            {
                if (pd.Object == null || !pd.Object.IsValid || pd.Object.InputAuthority != player)
                    continue;

                if (pd.HasStateAuthority)
                {
                    pd.IsBotControlled = true;
                    var nick = pd.Nick.ToString();
                    if (!nick.StartsWith("BOT "))
                        pd.Nick = "BOT " + nick;
                }

                break;
            }

            Debug.Log($"[FusionMultiplayer] Bot takeover for disconnected player {player.PlayerId}");
        }
    }
}
