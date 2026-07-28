using UnityEngine;

namespace FusionMultiplayer.Core
{
    /// <summary>Combat math independent of match flow (difficulty multiplier + crit).</summary>
    public static class CombatRules
    {
        public const float CritChance = 0.18f;
        public const float CritMultiplier = 1.75f;

        public static float RollDamage(float baseDamage)
        {
            var mult = 1f;
            if (ConnectionManager.Instance != null &&
                ConnectionManager.Instance.Runner != null &&
                ConnectionManager.Instance.Runner.SessionInfo.IsValid &&
                SessionCatalog.TryGetDifficulty(ConnectionManager.Instance.Runner.SessionInfo, out var diff))
            {
                mult = SessionCatalog.GetDamageMultiplier(diff);
            }
            else
            {
                mult = SessionCatalog.GetDamageMultiplier(SessionData.SelectedDifficulty);
            }

            var dmg = baseDamage * mult;
            if (Random.value < CritChance)
                dmg *= CritMultiplier;
            return dmg;
        }
    }
}
