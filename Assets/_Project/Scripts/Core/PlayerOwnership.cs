using Fusion;
using FusionMultiplayer.Player;
using UnityEngine;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// Resolves avatars / PlayerData after Fusion clears InputAuthority on disconnect.
    /// Character slot ownership is kept so bots and reconnect can still find the body.
    /// </summary>
    public static class PlayerOwnership
    {
        public static PlayerRef ResolveLogicalOwner(PlayerAvatar avatar)
        {
            if (avatar == null || avatar.Object == null || !avatar.Object.IsValid)
                return PlayerRef.None;

            var input = avatar.Object.InputAuthority;
            if (input != PlayerRef.None)
                return input;

            var gm = GameManager.Instance;
            if (gm == null || avatar.CharacterSlot < 0 || avatar.CharacterSlot >= 10)
                return PlayerRef.None;

            return gm.GetCharacterOwner(avatar.CharacterSlot);
        }

        public static PlayerAvatar FindAvatar(PlayerRef player)
        {
            if (player == PlayerRef.None)
                return null;

            foreach (var avatar in Object.FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
            {
                if (avatar.Object == null || !avatar.Object.IsValid)
                    continue;
                if (avatar.Object.InputAuthority == player)
                    return avatar;
            }

            var gm = GameManager.Instance;
            if (gm == null)
                return null;

            for (var slot = 0; slot < 10; slot++)
            {
                if (gm.GetCharacterOwner(slot) != player)
                    continue;

                foreach (var avatar in Object.FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
                {
                    if (avatar.Object == null || !avatar.Object.IsValid)
                        continue;
                    if (avatar.CharacterSlot == slot)
                        return avatar;
                }
            }

            return null;
        }

        public static PlayerData FindPlayerData(PlayerRef player, int preferredSlot = -1)
        {
            if (player == PlayerRef.None)
                return null;

            foreach (var pd in Object.FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
            {
                if (pd.Object == null || !pd.Object.IsValid)
                    continue;
                if (pd.Object.InputAuthority == player)
                    return pd;
            }

            var slot = preferredSlot;
            if (slot < 0 || slot >= 10)
            {
                var gm = GameManager.Instance;
                if (gm != null)
                {
                    for (var i = 0; i < 10; i++)
                    {
                        if (gm.GetCharacterOwner(i) != player)
                            continue;
                        slot = i;
                        break;
                    }
                }
            }

            if (slot < 0 || slot >= 10)
                return null;

            foreach (var pd in Object.FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
            {
                if (pd.Object == null || !pd.Object.IsValid)
                    continue;
                if (pd.CharacterIndex == slot)
                    return pd;
            }

            return null;
        }
    }
}
