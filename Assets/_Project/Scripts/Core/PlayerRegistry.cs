using System.Collections.Generic;
using Fusion;
using FusionMultiplayer.Player;
using UnityEngine;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// In-memory lookup for PlayerData / PlayerAvatar. Updated from Spawned/Despawned.
    /// Prefer this over FindObjectsByType on hot paths.
    /// </summary>
    public static class PlayerRegistry
    {
        public const int MaxCharacterSlots = SessionData.MaxPlayersCap;

        private static readonly Dictionary<PlayerRef, PlayerData> DataByPlayer = new();
        private static readonly Dictionary<int, PlayerAvatar> AvatarBySlot = new();
        private static readonly Dictionary<PlayerRef, PlayerAvatar> AvatarByPlayer = new();
        private static readonly Dictionary<string, PlayerData> DataByToken = new();

        private static readonly HashSet<PlayerData> DataEnumSeen = new();
        private static readonly HashSet<PlayerAvatar> AvatarEnumSeen = new();
        private static readonly List<PlayerRef> PlayerRefScratch = new(8);
        private static readonly List<string> StringScratch = new(8);
        private static int _dataEnumDepth;
        private static int _avatarEnumDepth;

        public static IEnumerable<PlayerData> AllPlayerData => EnumerateAllData();
        public static IEnumerable<PlayerAvatar> AllAvatars => EnumerateAllAvatars();

        public static void Clear()
        {
            DataByPlayer.Clear();
            AvatarBySlot.Clear();
            AvatarByPlayer.Clear();
            DataByToken.Clear();
        }

        public static void RegisterPlayerData(PlayerData data)
        {
            if (data == null || data.Object == null || !data.Object.IsValid)
                return;

            var owner = data.Object.InputAuthority;
            if (owner != PlayerRef.None)
                DataByPlayer[owner] = data;

            // Slot-based lookup for bot seats (InputAuthority cleared).
            if (data.CharacterIndex >= 0 && data.CharacterIndex < MaxCharacterSlots)
            {
                // Keep by-token index for reconnect.
            }

            RefreshTokenIndex(data);
        }

        public static void UnregisterPlayerData(PlayerData data)
        {
            if (data == null)
                return;

            RemoveDataEntries(data);
        }

        public static void NotifyPlayerDataAuthorityChanged(PlayerData data, PlayerRef previousOwner)
        {
            if (previousOwner != PlayerRef.None &&
                DataByPlayer.TryGetValue(previousOwner, out var existing) &&
                existing == data)
            {
                DataByPlayer.Remove(previousOwner);
            }

            RegisterPlayerData(data);
        }

        public static void NotifyReconnectTokenChanged(PlayerData data)
        {
            RefreshTokenIndex(data);
        }

        public static void RegisterAvatar(PlayerAvatar avatar)
        {
            if (avatar == null || avatar.Object == null || !avatar.Object.IsValid)
                return;

            var slot = avatar.CharacterSlot;
            if (slot >= 0 && slot < MaxCharacterSlots)
                AvatarBySlot[slot] = avatar;

            var owner = avatar.Object.InputAuthority;
            if (owner != PlayerRef.None)
                AvatarByPlayer[owner] = avatar;
        }

        public static void UnregisterAvatar(PlayerAvatar avatar)
        {
            if (avatar == null)
                return;

            if (avatar.CharacterSlot >= 0 &&
                AvatarBySlot.TryGetValue(avatar.CharacterSlot, out var bySlot) &&
                bySlot == avatar)
            {
                AvatarBySlot.Remove(avatar.CharacterSlot);
            }

            PlayerRefScratch.Clear();
            foreach (var kv in AvatarByPlayer)
            {
                if (kv.Value == avatar)
                    PlayerRefScratch.Add(kv.Key);
            }

            for (var i = 0; i < PlayerRefScratch.Count; i++)
                AvatarByPlayer.Remove(PlayerRefScratch[i]);
            PlayerRefScratch.Clear();
        }

        public static void NotifyAvatarAuthorityOrSlotChanged(PlayerAvatar avatar, PlayerRef previousOwner)
        {
            if (previousOwner != PlayerRef.None &&
                AvatarByPlayer.TryGetValue(previousOwner, out var existing) &&
                existing == avatar)
            {
                AvatarByPlayer.Remove(previousOwner);
            }

            // Clear stale slot entries pointing at this avatar.
            var staleSlots = new List<int>();
            foreach (var kv in AvatarBySlot)
            {
                if (kv.Value == avatar && kv.Key != avatar.CharacterSlot)
                    staleSlots.Add(kv.Key);
            }

            for (var i = 0; i < staleSlots.Count; i++)
                AvatarBySlot.Remove(staleSlots[i]);

            RegisterAvatar(avatar);
        }

        public static PlayerData FindPlayerData(PlayerRef player)
        {
            if (player == PlayerRef.None)
                return null;

            if (DataByPlayer.TryGetValue(player, out var data) && IsValidData(data))
                return data;

            // Fallback: rebuild from scene once (e.g. after domain quirks).
            RebuildFromScene();
            return DataByPlayer.TryGetValue(player, out data) && IsValidData(data) ? data : null;
        }

        public static PlayerData FindPlayerDataBySlot(int slot)
        {
            if (slot < 0 || slot >= MaxCharacterSlots)
                return null;

            foreach (var pd in EnumerateAllData())
            {
                if (pd.CharacterIndex == slot)
                    return pd;
            }

            RebuildFromScene();
            foreach (var pd in EnumerateAllData())
            {
                if (pd.CharacterIndex == slot)
                    return pd;
            }

            return null;
        }

        public static PlayerData FindPlayerDataByToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            if (DataByToken.TryGetValue(token, out var data) && IsValidData(data) &&
                data.ReconnectToken.ToString() == token)
                return data;

            RebuildFromScene();
            return DataByToken.TryGetValue(token, out data) && IsValidData(data) ? data : null;
        }

        public static bool IsKnownToken(string token) => FindPlayerDataByToken(token) != null;

        public static PlayerAvatar FindAvatar(PlayerRef player)
        {
            if (player == PlayerRef.None)
                return null;

            if (AvatarByPlayer.TryGetValue(player, out var avatar) && IsValidAvatar(avatar))
                return avatar;

            RebuildFromScene();
            return AvatarByPlayer.TryGetValue(player, out avatar) && IsValidAvatar(avatar) ? avatar : null;
        }

        public static PlayerAvatar FindAvatarBySlot(int slot)
        {
            if (slot < 0 || slot >= MaxCharacterSlots)
                return null;

            if (AvatarBySlot.TryGetValue(slot, out var avatar) && IsValidAvatar(avatar) &&
                avatar.CharacterSlot == slot)
                return avatar;

            RebuildFromScene();
            return AvatarBySlot.TryGetValue(slot, out avatar) && IsValidAvatar(avatar) ? avatar : null;
        }

        public static IEnumerable<PlayerData> EnumerateAllData()
        {
            if (_dataEnumDepth > 0)
                return BuildDataSnapshot(new List<PlayerData>(DataByPlayer.Count + DataByToken.Count),
                    new HashSet<PlayerData>());

            _dataEnumDepth++;
            try
            {
                return BuildDataSnapshot(new List<PlayerData>(DataByPlayer.Count + DataByToken.Count),
                    DataEnumSeen);
            }
            finally
            {
                _dataEnumDepth--;
            }
        }

        public static IEnumerable<PlayerAvatar> EnumerateAllAvatars()
        {
            if (_avatarEnumDepth > 0)
                return BuildAvatarSnapshot(new List<PlayerAvatar>(AvatarBySlot.Count + AvatarByPlayer.Count),
                    new HashSet<PlayerAvatar>());

            _avatarEnumDepth++;
            try
            {
                return BuildAvatarSnapshot(new List<PlayerAvatar>(AvatarBySlot.Count + AvatarByPlayer.Count),
                    AvatarEnumSeen);
            }
            finally
            {
                _avatarEnumDepth--;
            }
        }

        private static List<PlayerData> BuildDataSnapshot(List<PlayerData> snapshot, HashSet<PlayerData> seen)
        {
            snapshot.Clear();
            seen.Clear();

            foreach (var pd in DataByPlayer.Values)
            {
                if (!IsValidData(pd) || !seen.Add(pd))
                    continue;
                snapshot.Add(pd);
            }

            foreach (var pd in DataByToken.Values)
            {
                if (!IsValidData(pd) || !seen.Add(pd))
                    continue;
                snapshot.Add(pd);
            }

            return snapshot;
        }

        private static List<PlayerAvatar> BuildAvatarSnapshot(List<PlayerAvatar> snapshot, HashSet<PlayerAvatar> seen)
        {
            snapshot.Clear();
            seen.Clear();

            foreach (var avatar in AvatarBySlot.Values)
            {
                if (!IsValidAvatar(avatar) || !seen.Add(avatar))
                    continue;
                snapshot.Add(avatar);
            }

            foreach (var avatar in AvatarByPlayer.Values)
            {
                if (!IsValidAvatar(avatar) || !seen.Add(avatar))
                    continue;
                snapshot.Add(avatar);
            }

            return snapshot;
        }

        public static bool IsNicknameTakenByOther(string requested, PlayerRef self)
        {
            if (string.IsNullOrWhiteSpace(requested))
                return false;

            var canonical = requested.Trim();
            EnsurePopulated();

            foreach (var other in EnumerateAllData())
            {
                if (other.IsBotControlled)
                    continue;

                var owner = other.Object.InputAuthority;
                if (owner == PlayerRef.None || owner == self)
                    continue;

                if (string.Equals(other.Nick.ToString().Trim(), canonical,
                        System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static void RefreshTokenIndex(PlayerData data)
        {
            if (data == null)
                return;

            // Remove stale token entries pointing at this instance.
            StringScratch.Clear();
            foreach (var kv in DataByToken)
            {
                if (kv.Value == data)
                    StringScratch.Add(kv.Key);
            }

            for (var i = 0; i < StringScratch.Count; i++)
                DataByToken.Remove(StringScratch[i]);
            StringScratch.Clear();

            if (!IsValidData(data))
                return;

            var token = data.ReconnectToken.ToString();
            if (!string.IsNullOrWhiteSpace(token))
                DataByToken[token] = data;
        }

        private static void RemoveDataEntries(PlayerData data)
        {
            PlayerRefScratch.Clear();
            foreach (var kv in DataByPlayer)
            {
                if (kv.Value == data)
                    PlayerRefScratch.Add(kv.Key);
            }

            for (var i = 0; i < PlayerRefScratch.Count; i++)
                DataByPlayer.Remove(PlayerRefScratch[i]);
            PlayerRefScratch.Clear();

            StringScratch.Clear();
            foreach (var kv in DataByToken)
            {
                if (kv.Value == data)
                    StringScratch.Add(kv.Key);
            }

            for (var i = 0; i < StringScratch.Count; i++)
                DataByToken.Remove(StringScratch[i]);
            StringScratch.Clear();
        }

        private static void EnsurePopulated()
        {
            if (DataByPlayer.Count > 0 || DataByToken.Count > 0 || AvatarBySlot.Count > 0)
                return;
            RebuildFromScene();
        }

        private static void RebuildFromScene()
        {
            DataByPlayer.Clear();
            AvatarBySlot.Clear();
            AvatarByPlayer.Clear();
            DataByToken.Clear();

            foreach (var pd in Object.FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
            {
                if (!IsValidData(pd))
                    continue;
                RegisterPlayerData(pd);
            }

            foreach (var avatar in Object.FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
            {
                if (!IsValidAvatar(avatar))
                    continue;
                RegisterAvatar(avatar);
            }
        }

        private static bool IsValidData(PlayerData data) =>
            data != null && data.Object != null && data.Object.IsValid;

        private static bool IsValidAvatar(PlayerAvatar avatar) =>
            avatar != null && avatar.Object != null && avatar.Object.IsValid;
    }
}
