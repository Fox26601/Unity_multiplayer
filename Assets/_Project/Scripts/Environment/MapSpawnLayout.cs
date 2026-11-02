using FusionMultiplayer.Core;
using FusionMultiplayer.Data;
using UnityEngine;

namespace FusionMultiplayer.Environment
{
    /// <summary>Spawn point layouts per map (10 slots) via <see cref="MapDefinition"/> strategy assets.</summary>
    public static class MapSpawnLayout
    {
        public static void PlaceSpawnPoints(Transform root, SessionCatalog.MapKind map)
        {
            for (var i = 0; i < MapDefinition.SpawnSlotCount; i++)
            {
                var child = root.GetChild(i);
                child.localPosition = GetSpawnPosition(map, i);
                child.localRotation = Quaternion.Euler(0f, GetSpawnYaw(map, i), 0f);
            }
        }

        public static Vector3 GetSpawnPosition(SessionCatalog.MapKind map, int index)
        {
            var strategy = GameConfigs.EnsureCurrent()?.Maps?.GetStrategy(map);
            if (strategy != null)
                return strategy.GetSpawnPosition(index);

            return FallbackSpawnPosition(map, index);
        }

        public static float GetSpawnYaw(SessionCatalog.MapKind map, int index)
        {
            var strategy = GameConfigs.EnsureCurrent()?.Maps?.GetStrategy(map);
            if (strategy != null)
                return strategy.GetSpawnYaw(index);

            var pos = FallbackSpawnPosition(map, index);
            return Mathf.Atan2(-pos.x, -pos.z) * Mathf.Rad2Deg;
        }

        private static Vector3 FallbackSpawnPosition(SessionCatalog.MapKind map, int index)
        {
            return map switch
            {
                SessionCatalog.MapKind.Plaza => FallbackPlaza(index),
                SessionCatalog.MapKind.Ruins => FallbackRuins(index),
                _ => FallbackArena(index)
            };
        }

        private static Vector3 FallbackArena(int index)
        {
            var angle = index * 36f * Mathf.Deg2Rad;
            const float radius = 12f;
            return new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
        }

        private static Vector3 FallbackPlaza(int index)
        {
            if (index < 4)
                return new Vector3(-16f + index * 10f, 0f, -18f);
            if (index < 8)
                return new Vector3(-16f + (index - 4) * 10f, 0f, 18f);
            return new Vector3(index == 8 ? -18f : 18f, 0f, 0f);
        }

        private static Vector3 FallbackRuins(int index)
        {
            var angle = (index * 37f + 10f) * Mathf.Deg2Rad;
            var radius = 13f + (index % 3) * 1.5f;
            return new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
        }
    }
}
