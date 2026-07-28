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
        private static readonly Dictionary<int, PlayerData> DataBySlot = new();
        private static readonly Dictionary<int, PlayerAvatar> AvatarBySlot = new();
        private static readonly Dictionary<PlayerRef, PlayerAvatar> AvatarByPlayer = new();
        private static readonly Dictionary<string, PlayerData> DataByToken = new();

        private static readonly HashSet<PlayerData> DataEnumSeen = new();
        private static readonly HashSet<PlayerAvatar> AvatarEnumSeen = new();
        private static readonly List<PlayerData> DataScratch = new(16);
        private static readonly List<PlayerAvatar> AvatarScratch = new(16);
        private static readonly List<PlayerRef> PlayerRefScratch = new(8);
        private static readonly List<string> StringScratch = new(8);
        private static readonly List<int> IntScratch = new(8);

        private static float _nextRebuildAllowedTime;

        public static IEnumerable<PlayerData> AllPlayerData => EnumerateAllData();
        public static IEnumerable<PlayerAvatar> AllAvatars => EnumerateAllAvatars();

        public static void Clear()
        {
            DataByPlayer.Clear();
            DataBySlot.Clear();
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

            IndexDataSlot(data);
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

        public static void NotifyCharacterIndexChanged(PlayerData data)
        {
            if (data == null)
                return;

            RemoveDataSlotEntries(data);
            if (IsValidData(data))
                IndexDataSlot(data);
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

            IntScratch.Clear();
            foreach (var kv in AvatarBySlot)
            {
                if (kv.Value == avatar && kv.Key != avatar.CharacterSlot)
                    IntScratch.Add(kv.Key);
            }

            for (var i = 0; i < IntScratch.Count; i++)
                AvatarBySlot.Remove(IntScratch[i]);
            IntScratch.Clear();

            RegisterAvatar(avatar);
        }

        public static PlayerData FindPlayerData(PlayerRef player)
        {
            if (player == PlayerRef.None)
                return null;

            if (DataByPlayer.TryGetValue(player, out var data) && IsValidData(data))
                return data;

            TryRebuildFromScene();
            return DataByPlayer.TryGetValue(player, out data) && IsValidData(data) ? data : null;
        }

        public static PlayerData FindPlayerDataBySlot(int slot)
        {
            if (slot < 0 || slot >= MaxCharacterSlots)
                return null;

            if (DataBySlot.TryGetValue(slot, out var data) && IsValidData(data) && data.CharacterIndex == slot)
                return data;

            TryRebuildFromScene();
            return DataBySlot.TryGetValue(slot, out data) && IsValidData(data) && data.CharacterIndex == slot
                ? data
                : null;
        }

        public static PlayerData FindPlayerDataByToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            if (DataByToken.TryGetValue(token, out var data) && IsValidData(data) &&
                data.ReconnectToken.ToString() == token)
                return data;

            TryRebuildFromScene();
            return DataByToken.TryGetValue(token, out data) && IsValidData(data) ? data : null;
        }

        public static bool IsKnownToken(string token) => FindPlayerDataByToken(token) != null;

        public static PlayerAvatar FindAvatar(PlayerRef player)
        {
            if (player == PlayerRef.None)
                return null;

            if (AvatarByPlayer.TryGetValue(player, out var avatar) && IsValidAvatar(avatar))
                return avatar;

            TryRebuildFromScene();
            return AvatarByPlayer.TryGetValue(player, out avatar) && IsValidAvatar(avatar) ? avatar : null;
        }

        public static PlayerAvatar FindAvatarBySlot(int slot)
        {
            if (slot < 0 || slot >= MaxCharacterSlots)
                return null;

            if (AvatarBySlot.TryGetValue(slot, out var avatar) && IsValidAvatar(avatar) &&
                avatar.CharacterSlot == slot)
                return avatar;

            TryRebuildFromScene();
            return AvatarBySlot.TryGetValue(slot, out avatar) && IsValidAvatar(avatar) ? avatar : null;
        }

        /// <summary>Fills recycled buffer; nested callers get a fresh list.</summary>
        public static List<PlayerData> CopyAllData()
        {
            if (_dataCopyDepth > 0)
                return FillDataSnapshot(new List<PlayerData>(DataByPlayer.Count + DataByToken.Count + DataBySlot.Count),
                    new HashSet<PlayerData>());

            _dataCopyDepth++;
            try
            {
                return FillDataSnapshot(DataScratch, DataEnumSeen);
            }
            finally
            {
                _dataCopyDepth--;
            }
        }

        /// <summary>Fills recycled buffer; nested callers get a fresh list.</summary>
        public static List<PlayerAvatar> CopyAllAvatars()
        {
            if (_avatarCopyDepth > 0)
                return FillAvatarSnapshot(new List<PlayerAvatar>(AvatarBySlot.Count + AvatarByPlayer.Count),
                    new HashSet<PlayerAvatar>());

            _avatarCopyDepth++;
            try
            {
                return FillAvatarSnapshot(AvatarScratch, AvatarEnumSeen);
            }
            finally
            {
                _avatarCopyDepth--;
            }
        }

        public static IEnumerable<PlayerData> EnumerateAllData() => CopyAllData();

        public static IEnumerable<PlayerAvatar> EnumerateAllAvatars() => CopyAllAvatars();

        private static int _dataCopyDepth;
        private static int _avatarCopyDepth;

        private static List<PlayerData> FillDataSnapshot(List<PlayerData> snapshot, HashSet<PlayerData> seen)
        {
            snapshot.Clear();
            seen.Clear();

            foreach (var pd in DataByPlayer.Values)
            {
                if (!IsValidData(pd) || !seen.Add(pd))
                    continue;
                snapshot.Add(pd);
            }

            foreach (var pd in DataBySlot.Values)
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

        private static List<PlayerAvatar> FillAvatarSnapshot(List<PlayerAvatar> snapshot, HashSet<PlayerAvatar> seen)
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

            var list = CopyAllData();
            for (var i = 0; i < list.Count; i++)
            {
                var other = list[i];
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

        private static void IndexDataSlot(PlayerData data)
        {
            if (data.CharacterIndex >= 0 && data.CharacterIndex < MaxCharacterSlots)
                DataBySlot[data.CharacterIndex] = data;
        }

        private static void RemoveDataSlotEntries(PlayerData data)
        {
            IntScratch.Clear();
            foreach (var kv in DataBySlot)
            {
                if (kv.Value == data)
                    IntScratch.Add(kv.Key);
            }

            for (var i = 0; i < IntScratch.Count; i++)
                DataBySlot.Remove(IntScratch[i]);
            IntScratch.Clear();
        }

        private static void RefreshTokenIndex(PlayerData data)
        {
            if (data == null)
                return;

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

            RemoveDataSlotEntries(data);

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
            if (DataByPlayer.Count > 0 || DataByToken.Count > 0 || DataBySlot.Count > 0 || AvatarBySlot.Count > 0)
                return;
            TryRebuildFromScene(force: true);
        }

        private static void TryRebuildFromScene(bool force = false)
        {
            if (!force && Time.unscaledTime < _nextRebuildAllowedTime)
                return;

            _nextRebuildAllowedTime = Time.unscaledTime + 1f;
            RebuildFromScene();
        }

        private static void RebuildFromScene()
        {
            DataByPlayer.Clear();
            DataBySlot.Clear();
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
