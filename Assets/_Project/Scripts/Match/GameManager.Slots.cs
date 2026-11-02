using System.Collections.Generic;
using Fusion;
using FusionMultiplayer.Data;
using FusionMultiplayer.Player;
using UnityEngine;

namespace FusionMultiplayer.Core
{
    public partial class GameManager
    {
        /// <summary>Master random decision #1: pick a spawn with a small planar jitter.</summary>
        public void GetRandomizedSpawn(int index, out Vector3 position, out Quaternion rotation)
        {
            var sp = GetSpawnPoint(index);
            var jitter = Random.insideUnitCircle * 1.25f;
            position = sp.position + new Vector3(jitter.x, 0f, jitter.y);
            rotation = sp.rotation;
        }

        public void RequestCharacter(int index)
        {
            RPC_RequestCharacter(index);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestCharacter(int index, RpcInfo info = default)
        {
            if (!CanMutateSlots || !HasStateAuthority) return;
            if (index < 0 || index >= PlayerRegistry.MaxCharacterSlots) return;

            var requester = info.Source;
            if (requester == PlayerRef.None)
                requester = Runner.LocalPlayer;
            if (requester == PlayerRef.None)
                return;

            var owner = CharacterOwners.Get(index);
            if (owner != PlayerRef.None && owner != requester)
            {
                // Reconnect race: stale pre-leave PlayerRef / bot seat for this token — free and continue.
                if (!TryClaimStaleReconnectSeat(index, requester, owner))
                {
                    RPC_CharacterRejected(requester, index);
                    return;
                }
            }

            // Reconnect / re-pick: release every prior seat+body for this player, then assign.
            ReleasePreviousSeats(requester, index);

            CharacterOwners.Set(index, requester);
            GetRandomizedSpawn(index, out var position, out var rotation);

            // Same-seat reclaim only: never teleport a different-slot / bot body onto the new seat.
            var existing = FindAvatarFor(requester) ?? PlayerOwnership.FindAvatarBySlot(index);
            if (existing != null && existing.CharacterSlot == index)
            {
                StopBotOnAvatar(existing);
                if (existing.HasStateAuthority)
                {
                    var ncc = existing.GetComponent<NetworkCharacterController>();
                    if (ncc != null)
                        ncc.Teleport(position, rotation);
                    else
                        existing.transform.SetPositionAndRotation(position, rotation);
                }
            }
            // Shared Mode: requester spawns their own avatar (SA+IA) on CharacterApproved.
            // Master never spawns remote player avatars.

            var pd = PlayerRegistry.FindPlayerData(requester) ??
                     PlayerOwnership.FindPlayerData(requester);
            if (pd == null &&
                SessionReconnectTokens.TryReadPlayerToken(Runner, requester, out var token) &&
                !string.IsNullOrWhiteSpace(token))
            {
                pd = ReconnectService.FindSeatByToken(token) ??
                     PlayerRegistry.FindPlayerDataByToken(token);
            }

            if (pd != null && pd.HasStateAuthority)
            {
                pd.CharacterIndex = index;
                pd.IsBotControlled = false;
                PlayerRegistry.NotifyCharacterIndexChanged(pd);
                PlayerRegistry.RegisterPlayerData(pd);
            }

            RPC_CharacterApproved(requester, index, position, rotation);
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RPC_CharacterApproved([RpcTarget] PlayerRef target, int index, Vector3 position,
            Quaternion rotation)
        {
            if (Runner.LocalPlayer != target) return;
            CharacterSelectBridge.NotifyApproved(index, position, rotation);
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RPC_CharacterRejected([RpcTarget] PlayerRef target, int index)
        {
            if (Runner.LocalPlayer != target) return;
            CharacterSelectBridge.NotifyRejected(index);
        }

        public void ClearSlotsForPlayer(PlayerRef player)
        {
            if (!CanMutateSlots || !HasStateAuthority) return;
            for (var i = 0; i < PlayerRegistry.MaxCharacterSlots; i++)
            {
                if (CharacterOwners.Get(i) == player)
                    CharacterOwners.Set(i, PlayerRef.None);
            }
        }

        public void ReleaseCharacterSlot(int index, PlayerRef player)
        {
            if (!CanMutateSlots || !HasStateAuthority) return;
            if (index < 0 || index >= PlayerRegistry.MaxCharacterSlots) return;
            if (CharacterOwners.Get(index) == player)
                CharacterOwners.Set(index, PlayerRef.None);
        }

        /// <summary>Local player spawns avatar after master slot approval (Shared Mode SA+IA).</summary>
        public NetworkObject TrySpawnLocalAvatar(int index, Vector3 position, Quaternion rotation)
        {
            if (Runner == null || !Runner.IsRunning)
                return null;

            var local = Runner.LocalPlayer;
            if (local == PlayerRef.None)
                return null;

            var existing = FindAvatarFor(local);
            if (existing != null && existing.Object != null && existing.Object.IsValid)
            {
                // Same body reused for the approved seat (teleport).
                if (existing.CharacterSlot == index || existing.CharacterSlot < 0)
                {
                    existing.CharacterSlot = index;
                    if (existing.HasStateAuthority)
                    {
                        var ncc = existing.GetComponent<NetworkCharacterController>();
                        if (ncc != null)
                            ncc.Teleport(position, rotation);
                        else
                            existing.transform.SetPositionAndRotation(position, rotation);
                    }

                    ApplyLocalCharacterIndex(local, index);
                    return existing.Object;
                }

                // Stale body at another slot after seat change — release before spawn.
                existing.RequestReleaseBody();
            }

            // Clear any other local ghost at a different slot (registry race).
            foreach (var avatar in PlayerRegistry.EnumerateAllAvatars())
            {
                if (avatar == null || avatar.Object == null || !avatar.Object.IsValid)
                    continue;
                if (avatar.Object.InputAuthority != local &&
                    PlayerOwnership.ResolveLogicalOwner(avatar) != local)
                    continue;
                if (avatar.CharacterSlot == index)
                    continue;
                avatar.RequestReleaseBody();
            }

            var spawned = NetworkSpawnFactory.SpawnAvatar(Runner, position, rotation, local);
            if (spawned == null && _playerAvatarPrefab != null)
                spawned = Runner.Spawn(_playerAvatarPrefab, position, rotation, local);

            if (spawned != null)
            {
                var pa = spawned.GetComponent<PlayerAvatar>();
                if (pa != null)
                    pa.CharacterSlot = index;
            }

            ApplyLocalCharacterIndex(local, index);
            return spawned;
        }

        /// <summary>
        /// After reclaim, PlayerData SA is on the client — master cannot write CharacterIndex.
        /// </summary>
        private static void ApplyLocalCharacterIndex(PlayerRef local, int index)
        {
            var pd = PlayerRegistry.FindPlayerData(local) ?? PlayerOwnership.FindPlayerData(local);
            if (pd == null || !pd.HasStateAuthority)
                return;

            pd.CharacterIndex = index;
            pd.IsBotControlled = false;
            PlayerRegistry.NotifyCharacterIndexChanged(pd);
            PlayerRegistry.RegisterPlayerData(pd);
        }

        private static PlayerAvatar FindAvatarFor(PlayerRef player) =>
            PlayerOwnership.FindAvatar(player);

        /// <summary>
        /// Vacate every seat/body belonging to <paramref name="requester"/> except
        /// <paramref name="keepIndex"/>. Stops bot AI and despawns via SA (local or RPC).
        /// </summary>
        private void ReleasePreviousSeats(PlayerRef requester, int keepIndex)
        {
            if (requester == PlayerRef.None)
                return;

            var vacated = new HashSet<int>();
            var pd = PlayerRegistry.FindPlayerData(requester) ??
                     PlayerOwnership.FindPlayerData(requester);

            // Reconnect: CharacterOwners may still hold the pre-leave PlayerRef and IA may not
            // be remapped yet — resolve the seat via connection token.
            if (pd == null &&
                SessionReconnectTokens.TryReadPlayerToken(Runner, requester, out var token) &&
                !string.IsNullOrWhiteSpace(token))
            {
                pd = ReconnectService.FindSeatByToken(token) ??
                     PlayerRegistry.FindPlayerDataByToken(token);
            }

            for (var i = 0; i < PlayerRegistry.MaxCharacterSlots; i++)
            {
                if (i == keepIndex)
                    continue;
                if (CharacterOwners.Get(i) == requester)
                    vacated.Add(i);
            }

            if (pd != null)
            {
                var prev = pd.CharacterIndex;
                if (prev >= 0 && prev < PlayerRegistry.MaxCharacterSlots && prev != keepIndex)
                    vacated.Add(prev);

                // After disconnect, CharacterOwners may still hold the pre-leave PlayerRef.
                // Vacate any seat bound to the same PlayerData object.
                for (var i = 0; i < PlayerRegistry.MaxCharacterSlots; i++)
                {
                    if (i == keepIndex)
                        continue;
                    var slotOwner = CharacterOwners.Get(i);
                    if (slotOwner == PlayerRef.None || slotOwner == requester)
                        continue;

                    var otherPd = PlayerOwnership.FindPlayerData(slotOwner, i);
                    if (otherPd != null && otherPd.Object != null && pd.Object != null &&
                        otherPd.Object == pd.Object)
                        vacated.Add(i);
                }
            }

            var bodies = new HashSet<PlayerAvatar>();

            var byRequester = FindAvatarFor(requester);
            if (byRequester != null)
                bodies.Add(byRequester);

            foreach (var slot in vacated)
            {
                var bySlot = PlayerOwnership.FindAvatarBySlot(slot);
                if (bySlot != null)
                    bodies.Add(bySlot);
            }

            foreach (var avatar in PlayerRegistry.EnumerateAllAvatars())
            {
                if (avatar == null || avatar.Object == null || !avatar.Object.IsValid)
                    continue;

                var slot = avatar.CharacterSlot;
                var ia = avatar.Object.InputAuthority;
                var logical = PlayerOwnership.ResolveLogicalOwner(avatar);
                var brain = avatar.GetComponent<BotBrain>();
                var botForSeat = brain != null && brain.IsActive &&
                                 pd != null && slot == pd.CharacterIndex && slot != keepIndex;

                if (ia == requester || logical == requester ||
                    (slot >= 0 && vacated.Contains(slot)) || botForSeat)
                {
                    if (slot == keepIndex && (ia == requester || logical == requester))
                        continue;
                    bodies.Add(avatar);
                }
            }

            var releaseCount = 0;
            foreach (var avatar in bodies)
            {
                if (avatar == null || avatar.Object == null || !avatar.Object.IsValid)
                    continue;
                if (avatar.CharacterSlot == keepIndex &&
                    (avatar.Object.InputAuthority == requester ||
                     PlayerOwnership.ResolveLogicalOwner(avatar) == requester))
                    continue;

                avatar.RequestReleaseBody();
                releaseCount++;
            }

            foreach (var slot in vacated)
            {
                if (CharacterOwners.Get(slot) != PlayerRef.None)
                    CharacterOwners.Set(slot, PlayerRef.None);
            }

            if (vacated.Count > 0 || releaseCount > 0)
            {
                Debug.Log(
                    $"[FusionMultiplayer] ReleasePreviousSeats player={requester.PlayerId} keep={keepIndex} vacated={vacated.Count} releasedBodies={releaseCount}");
            }
        }

        private static void StopBotOnAvatar(PlayerAvatar avatar)
        {
            if (avatar == null)
                return;

            var brain = avatar.GetComponent<BotBrain>();
            if (brain != null)
                brain.Deactivate();
        }

        /// <summary>
        /// True when <paramref name="slot"/> is still marked for a disconnected owner whose
        /// PlayerData token matches the reconnecting requester — clear owners and release body.
        /// </summary>
        private bool TryClaimStaleReconnectSeat(int slot, PlayerRef requester, PlayerRef staleOwner)
        {
            if (!SessionReconnectTokens.TryReadPlayerToken(Runner, requester, out var token) ||
                string.IsNullOrWhiteSpace(token))
                return false;

            var seatPd = ReconnectService.FindSeatByToken(token) ??
                         PlayerRegistry.FindPlayerDataByToken(token) ??
                         PlayerOwnership.FindPlayerData(staleOwner, slot) ??
                         PlayerRegistry.FindPlayerDataBySlot(slot);

            if (seatPd == null || seatPd.Object == null || !seatPd.Object.IsValid)
                return false;

            if (!string.Equals(seatPd.ReconnectToken.ToString().Trim(), token.Trim(),
                    System.StringComparison.Ordinal))
                return false;

            var avatar = PlayerOwnership.FindAvatarBySlot(slot) ??
                         PlayerOwnership.FindAvatar(staleOwner);
            if (avatar != null && avatar.Object != null && avatar.Object.IsValid)
                avatar.RequestReleaseBody();

            CharacterOwners.Set(slot, PlayerRef.None);
            if (seatPd.HasStateAuthority)
            {
                seatPd.IsBotControlled = false;
                if (seatPd.CharacterIndex == slot)
                    seatPd.CharacterIndex = -1;
                PlayerRegistry.NotifyCharacterIndexChanged(seatPd);
            }

            Debug.Log(
                $"[FusionMultiplayer] Cleared stale reconnect seat {slot} for player {requester.PlayerId}");
            return true;
        }
    }
}
