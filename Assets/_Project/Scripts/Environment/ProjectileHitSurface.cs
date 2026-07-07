using FusionMultiplayer.Player;
using UnityEngine;

namespace FusionMultiplayer.Environment
{
    /// <summary>Resolves colliders that should stop combat projectiles.</summary>
    public static class ProjectileHitSurface
    {
        public static bool TryGetBlockingSurface(Collider col, out Component surface)
        {
            surface = null;
            if (col == null || col.isTrigger)
                return false;

            var env = col.GetComponentInParent<EnvironmentPiece>();
            if (env != null && env.BlocksProjectiles)
            {
                surface = env;
                return true;
            }

            var block = col.GetComponentInParent<PlacedBlock>();
            if (block != null && block.Object != null && block.Object.IsValid)
            {
                surface = block;
                return true;
            }

            return false;
        }
    }
}
