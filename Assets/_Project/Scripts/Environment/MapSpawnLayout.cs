using FusionMultiplayer.Core;
using UnityEngine;

namespace FusionMultiplayer.Environment
{
    /// <summary>Spawn point layouts per map (10 slots).</summary>
    public static class MapSpawnLayout
    {
        public static void PlaceSpawnPoints(Transform root, SessionCatalog.MapKind map)
        {
            for (var i = 0; i < 10; i++)
            {
                var child = root.GetChild(i);
                child.localPosition = GetSpawnPosition(map, i);
                child.localRotation = Quaternion.Euler(0f, GetSpawnYaw(map, i), 0f);
            }
        }

        private static Vector3 GetSpawnPosition(SessionCatalog.MapKind map, int index)
        {
            return map switch
            {
                SessionCatalog.MapKind.Plaza => GetPlazaSpawn(index),
                SessionCatalog.MapKind.Ruins => GetRuinsSpawn(index),
                _ => GetArenaSpawn(index)
            };
        }

        private static float GetSpawnYaw(SessionCatalog.MapKind map, int index)
        {
            var pos = GetSpawnPosition(map, index);
            return Mathf.Atan2(-pos.x, -pos.z) * Mathf.Rad2Deg;
        }

        private static Vector3 GetArenaSpawn(int index)
        {
            var angle = index * 36f * Mathf.Deg2Rad;
            var radius = 12f;
            return new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
        }

        private static Vector3 GetPlazaSpawn(int index)
        {
            if (index < 4)
                return new Vector3(-16f + index * 10f, 0f, -18f);
            if (index < 8)
                return new Vector3(-16f + (index - 4) * 10f, 0f, 18f);
            return new Vector3(index == 8 ? -18f : 18f, 0f, 0f);
        }

        private static Vector3 GetRuinsSpawn(int index)
        {
            var angle = (index * 37f + 10f) * Mathf.Deg2Rad;
            var radius = 13f + (index % 3) * 1.5f;
            return new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
        }
    }
}
