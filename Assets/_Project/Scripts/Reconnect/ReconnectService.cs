using System.Collections.Generic;
using Fusion;
using FusionMultiplayer.Player;
using UnityEngine;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// Shared master crash reconnect: reclaim PlayerData by token, despawn bot body, free the slot
    /// for character re-pick (no same-body handoff).
    /// </summary>
    public static class ReconnectService
    {
        private struct PendingReclaim
        {
            public PlayerRef Player;
            public string Token;
        }

        private static readonly List<PendingReclaim> Pending = new(8);

        public static bool HasPending => Pending.Count > 0;

        /// <summary>
        /// Restores a reconnecting player. Returns true when the seat was reclaimed or queued
        /// (caller must not spawn a fresh PlayerData).
        /// </summary>
        public static bool RestorePlayer(NetworkRunner runner, PlayerRef player, string token)
        {
            if (runner == null || player == PlayerRef.None || string.IsNullOrWhiteSpace(token))
                return false;

            if (!NetworkAuthority.IsSessionAuthority(runner))
                return false;

            token = token.Trim();
            var pd = FindSeatByToken(token);
            if (pd == null || pd.Object == null || !pd.Object.IsValid)
            {
                Enqueue(player, token);
                Debug.Log(
                    $"[FusionMultiplayer] Reconnect queued for player {player.PlayerId} (seat not ready yet)");
                return true;
            }

            DespawnDuplicatePlayerData(runner, pd, token);

            var oldDataOwner = pd.Object.InputAuthority;
            if (oldDataOwner != player)
            {
                if (!CanReclaimSeat(runner, pd, oldDataOwner))
                    return false;

                pd.Object.AssignInputAuthority(player);
                PlayerRegistry.NotifyPlayerDataAuthorityChanged(pd, oldDataOwner);
            }

            if (!TryReleaseBotSeatAndClearSlot(runner, player, pd, oldDataOwner))
            {
                Enqueue(player, token);
                return true;
            }

            Debug.Log($"[FusionMultiplayer] Reconnect restored player {player.PlayerId} (bot seat freed)");
            SessionLock.TryCloseReconnectWindowAfterReclaim(runner);
            RemovePendingFor(player, token);
            return true;
        }

        /// <summary>Retry queued reclaim when GameManager / Shared master is ready.</summary>
        public static void FlushPending(NetworkRunner runner)
        {
            if (runner == null || !runner.IsRunning || !NetworkAuthority.IsSessionAuthority(runner))
                return;

            if (Pending.Count == 0)
                return;

            var copy = Pending.ToArray();
            Pending.Clear();
            for (var i = 0; i < copy.Length; i++)
            {
                var entry = copy[i];
                if (entry.Player == PlayerRef.None || string.IsNullOrWhiteSpace(entry.Token))
                    continue;

                if (!IsPlayerStillConnected(runner, entry.Player))
                    continue;

                RestorePlayer(runner, entry.Player, entry.Token);
            }
        }

        /// <summary>
        /// Window expiry: despawn bot / disconnected seats, then hard-close the room.
        /// </summary>
        public static void ExpireAbandonedSeats(NetworkRunner runner)
        {
            PurgeDisconnectedSeats(runner);
            SessionLock.CloseReconnectWindow(runner);
        }

        /// <summary>
        /// Master-only: remove PlayerData + avatar for bot seats and seats whose IA is gone.
        /// Used by reconnect-window expiry and rematch reset.
        /// </summary>
        public static void PurgeDisconnectedSeats(NetworkRunner runner)
        {
            if (runner == null || !runner.IsRunning || !NetworkAuthority.IsSessionAuthority(runner))
                return;

            var gm = GameManager.Instance;
            var toPurge = new List<PlayerData>(8);

            foreach (var pd in PlayerRegistry.EnumerateAllData())
            {
                if (pd == null || pd.Object == null || !pd.Object.IsValid)
                    continue;

                var ia = pd.Object.InputAuthority;
                var disconnected = ia == PlayerRef.None || !IsPlayerStillConnected(runner, ia);
                var botSeat = (bool)pd.IsBotControlled;
                var hasSeat = pd.CharacterIndex >= 0 || botSeat;
                if (!hasSeat)
                    continue;

                // Keep seats still held by a connected human.
                if (!botSeat && !disconnected)
                    continue;

                toPurge.Add(pd);
            }

            for (var i = 0; i < toPurge.Count; i++)
                PurgeSeat(runner, gm, toPurge[i]);
        }

        private static void PurgeSeat(NetworkRunner runner, GameManager gm, PlayerData pd)
        {
            if (pd == null || pd.Object == null || !pd.Object.IsValid)
                return;

            var slot = pd.CharacterIndex;
            var ia = pd.Object.InputAuthority;

            var avatar = PlayerOwnership.FindAvatarBySlot(slot) ??
                         PlayerOwnership.FindAvatar(ia) ??
                         (slot >= 0 ? PlayerRegistry.FindAvatarBySlot(slot) : null);

            if (avatar != null && avatar.Object != null && avatar.Object.IsValid)
                avatar.RequestReleaseBody();

            if (gm != null && slot >= 0 && slot < PlayerRegistry.MaxCharacterSlots)
                gm.SetCharacterOwner(slot, PlayerRef.None);

            Debug.Log(
                $"[FusionMultiplayer] Purging disconnected seat (slot={slot}, bot={pd.IsBotControlled})");
            pd.RequestDespawnAsDuplicate();
        }

        /// <summary>
        /// Prefer the original bot / seated PlayerData over a fresh duplicate (CharacterIndex -1).
        /// </summary>
        public static PlayerData FindSeatByToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            token = token.Trim();
            PlayerData best = null;
            var bestScore = -1;

            foreach (var pd in PlayerRegistry.EnumerateAllData())
            {
                if (pd == null || pd.Object == null || !pd.Object.IsValid)
                    continue;
                if (pd.ReconnectToken.ToString() != token)
                    continue;

                var score = SeatScore(pd);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = pd;
                }
            }

            if (best != null)
                return best;

            return PlayerRegistry.FindPlayerDataByToken(token);
        }

        private static int SeatScore(PlayerData pd)
        {
            var score = 0;
            if (pd.IsBotControlled)
                score += 4;
            if (pd.CharacterIndex >= 0)
                score += 2;
            return score;
        }

        private static void DespawnDuplicatePlayerData(NetworkRunner runner, PlayerData keep, string token)
        {
            if (runner == null || keep == null || string.IsNullOrWhiteSpace(token))
                return;

            foreach (var pd in PlayerRegistry.EnumerateAllData())
            {
                if (pd == null || pd == keep || pd.Object == null || !pd.Object.IsValid)
                    continue;
                if (pd.ReconnectToken.ToString() != token)
                    continue;

                // Fresh duplicate: no seat, not the bot body.
                if (pd.CharacterIndex >= 0 || pd.IsBotControlled)
                    continue;

                Debug.Log(
                    $"[FusionMultiplayer] Despawning duplicate PlayerData for token reclaim (player {pd.Object.InputAuthority.PlayerId})");
                pd.RequestDespawnAsDuplicate();
            }
        }

        /// <summary>
        /// Despawn bot/old body, clear CharacterOwners, reset CharacterIndex so the player re-picks.
        /// </summary>
        private static bool TryReleaseBotSeatAndClearSlot(NetworkRunner runner, PlayerRef player, PlayerData pd,
            PlayerRef oldDataOwner)
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.HasStateAuthority)
            {
                // Need GameManager SA to clear CharacterOwners — retry on flush.
                ClearPlayerDataSeatFlags(pd);
                pd.NotifyClientReclaimAuthority();
                return false;
            }

            var slot = pd.CharacterIndex;
            var avatar = PlayerOwnership.FindAvatar(oldDataOwner) ??
                         PlayerOwnership.FindAvatar(player) ??
                         PlayerOwnership.FindAvatarBySlot(slot);

            // Also scan for an active bot body still bound to this seat index.
            if (avatar == null && slot >= 0)
                avatar = PlayerRegistry.FindAvatarBySlot(slot);

            if (avatar != null && avatar.Object != null && avatar.Object.IsValid)
            {
                var bodySlot = avatar.CharacterSlot;
                if (bodySlot >= 0 && bodySlot < PlayerRegistry.MaxCharacterSlots)
                    slot = bodySlot;

                Debug.Log(
                    $"[FusionMultiplayer] Reconnect releasing bot body slot={slot} for player {player.PlayerId}");
                avatar.RequestReleaseBody();
            }

            if (slot >= 0 && slot < PlayerRegistry.MaxCharacterSlots)
                gm.SetCharacterOwner(slot, PlayerRef.None);

            // Stale CharacterOwners may still hold the pre-leave PlayerRef on another index.
            for (var i = 0; i < PlayerRegistry.MaxCharacterSlots; i++)
            {
                if (gm.GetCharacterOwner(i) == oldDataOwner || gm.GetCharacterOwner(i) == player)
                    gm.SetCharacterOwner(i, PlayerRef.None);
            }

            ClearPlayerDataSeatFlags(pd);
            pd.NotifyClientReclaimAuthority();
            CharacterSelectBridge.NotifySlotsChanged();
            return true;
        }

        private static void ClearPlayerDataSeatFlags(PlayerData pd)
        {
            if (pd == null || !pd.HasStateAuthority)
                return;

            pd.IsBotControlled = false;
            var nick = pd.Nick.ToString();
            if (nick.StartsWith("BOT "))
                pd.Nick = nick.Substring(4);

            if (pd.CharacterIndex >= 0)
            {
                pd.CharacterIndex = -1;
                PlayerRegistry.NotifyCharacterIndexChanged(pd);
            }

            PlayerRegistry.NotifyReconnectTokenChanged(pd);
            PlayerRegistry.RegisterPlayerData(pd);
        }

        private static void Enqueue(PlayerRef player, string token)
        {
            for (var i = 0; i < Pending.Count; i++)
            {
                if (Pending[i].Player == player && Pending[i].Token == token)
                    return;
            }

            Pending.Add(new PendingReclaim { Player = player, Token = token });
        }

        private static void RemovePendingFor(PlayerRef player, string token)
        {
            for (var i = Pending.Count - 1; i >= 0; i--)
            {
                if (Pending[i].Player == player && Pending[i].Token == token)
                    Pending.RemoveAt(i);
            }
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
