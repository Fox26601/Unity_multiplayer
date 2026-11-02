using Fusion;
using FusionMultiplayer.Player;
using UnityEngine;

namespace FusionMultiplayer.Core
{
    public partial class GameManager
    {
        private const float MaxHitDistance = 40f;

        /// <summary>
        /// Shooter asks Shared master to validate hit and roll damage (Google Doc #10 master-only RNG).
        /// </summary>
        public void RequestMasterHit(PlayerRef victimRef, float baseDamage, PlayerRef attacker, Vector3 hitOrigin)
        {
            if (victimRef == PlayerRef.None || attacker == PlayerRef.None || baseDamage <= 0f)
                return;

            if (HasStateAuthority)
                ProcessMasterHit(victimRef, baseDamage, attacker, hitOrigin);
            else
                RPC_RequestHit(victimRef, baseDamage, attacker, hitOrigin);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestHit(PlayerRef victimRef, float baseDamage, PlayerRef attacker, Vector3 hitOrigin,
            RpcInfo info = default)
        {
            if (!HasStateAuthority)
                return;

            SessionRuntime.Refresh(Runner);
            if (!SessionRuntime.AllowsShoot)
                return;

            var source = info.Source;
            if (source == PlayerRef.None)
                source = Runner.LocalPlayer;

            // Only the attacker (or local master simulating bots) may request a hit.
            if (source != PlayerRef.None && source != attacker && source != Runner.LocalPlayer)
                return;

            ProcessMasterHit(victimRef, baseDamage, attacker, hitOrigin);
        }

        private void ProcessMasterHit(PlayerRef victimRef, float baseDamage, PlayerRef attacker, Vector3 hitOrigin)
        {
            if (!HasStateAuthority)
                return;

            SessionRuntime.Refresh(Runner);
            if (!SessionRuntime.AllowsShoot)
                return;

            if (attacker == PlayerRef.None || victimRef == PlayerRef.None || attacker == victimRef)
                return;

            var victim = PlayerOwnership.FindAvatar(victimRef) ?? PlayerOwnership.FindAvatarBySlot(
                FindSlotForPlayer(victimRef));
            if (victim == null || victim.Object == null || !victim.Object.IsValid || !victim.IsAlive)
                return;

            var planar = hitOrigin - victim.transform.position;
            planar.y = 0f;
            if (planar.sqrMagnitude > MaxHitDistance * MaxHitDistance)
                return;

            // Master random decision #3: critical hit / difficulty multiplier.
            var rolled = CombatRules.RollDamage(baseDamage);
            DeliverMasterHit(victim, victimRef, rolled, attacker, hitOrigin);
        }

        private void DeliverMasterHit(PlayerAvatar victim, PlayerRef victimRef, float rolledDamage,
            PlayerRef attacker, Vector3 hitOrigin)
        {
            if (victim.HasStateAuthority)
            {
                victim.ApplyMasterHitLocal(rolledDamage, attacker, hitOrigin);
                return;
            }

            RPC_ApplyHitToVictim(victimRef, rolledDamage, attacker, hitOrigin);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_ApplyHitToVictim([RpcTarget] PlayerRef victimPlayer, float rolledDamage,
            PlayerRef attacker, Vector3 hitOrigin)
        {
            if (Runner == null || Runner.LocalPlayer != victimPlayer)
                return;

            var victim = PlayerOwnership.FindAvatar(victimPlayer);
            if (victim == null || !victim.HasStateAuthority)
                return;

            victim.ApplyMasterHitLocal(rolledDamage, attacker, hitOrigin);
        }

        private int FindSlotForPlayer(PlayerRef player)
        {
            if (player == PlayerRef.None || Object == null || !Object.IsValid)
                return -1;

            for (var i = 0; i < PlayerRegistry.MaxCharacterSlots; i++)
            {
                if (CharacterOwners.Get(i) == player)
                    return i;
            }

            return -1;
        }
    }
}
