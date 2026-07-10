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
            if (col == null || !col.enabled || col.isTrigger)
                return false;

            // Players and projectiles are handled by Projectile hit logic, not as world blocks.
            if (col.GetComponentInParent<PlayerAvatar>() != null)
                return false;
            if (col.GetComponentInParent<Projectile>() != null)
                return false;

            var env = col.GetComponentInParent<EnvironmentPiece>();
            if (env != null)
            {
                if (!env.BlocksProjectiles)
                    return false;
                surface = env;
                return true;
            }

            var block = col.GetComponentInParent<PlacedBlock>();
            if (block != null && block.Object != null && block.Object.IsValid)
            {
                surface = block;
                return true;
            }

            var ground = col.GetComponentInParent<CheckerboardGround>();
            if (ground != null)
            {
                surface = ground;
                return true;
            }

            // Any other solid world collider (floor, walls, props).
            surface = col;
            return true;
        }
    }
}
