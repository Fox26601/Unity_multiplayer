using Fusion;
using FusionMultiplayer.Player;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// Resolves avatars / PlayerData after Fusion clears InputAuthority on disconnect.
    /// Character slot ownership is kept so bots and reconnect can still find the body.
    /// Backed by <see cref="PlayerRegistry"/>.
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
            if (gm == null || avatar.CharacterSlot < 0 ||
                avatar.CharacterSlot >= PlayerRegistry.MaxCharacterSlots)
                return PlayerRef.None;

            return gm.GetCharacterOwner(avatar.CharacterSlot);
        }

        public static PlayerAvatar FindAvatar(PlayerRef player)
        {
            if (player == PlayerRef.None)
                return null;

            var byInput = PlayerRegistry.FindAvatar(player);
            if (byInput != null)
                return byInput;

            var gm = GameManager.Instance;
            if (gm == null)
                return null;

            for (var slot = 0; slot < PlayerRegistry.MaxCharacterSlots; slot++)
            {
                if (gm.GetCharacterOwner(slot) != player)
                    continue;

                var bySlot = PlayerRegistry.FindAvatarBySlot(slot);
                if (bySlot != null)
                    return bySlot;
            }

            return null;
        }

        public static PlayerAvatar FindAvatarBySlot(int slot) =>
            PlayerRegistry.FindAvatarBySlot(slot);

        public static PlayerData FindPlayerData(PlayerRef player, int preferredSlot = -1)
        {
            if (player == PlayerRef.None)
                return null;

            var byInput = PlayerRegistry.FindPlayerData(player);
            if (byInput != null)
                return byInput;

            var slot = preferredSlot;
            if (slot < 0 || slot >= PlayerRegistry.MaxCharacterSlots)
            {
                var gm = GameManager.Instance;
                if (gm != null)
                {
                    for (var i = 0; i < PlayerRegistry.MaxCharacterSlots; i++)
                    {
                        if (gm.GetCharacterOwner(i) != player)
                            continue;
                        slot = i;
                        break;
                    }
                }
            }

            if (slot < 0 || slot >= PlayerRegistry.MaxCharacterSlots)
                return null;

            return PlayerRegistry.FindPlayerDataBySlot(slot);
        }
    }
}
