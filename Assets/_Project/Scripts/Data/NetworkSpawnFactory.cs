using Fusion;
using UnityEngine;

namespace FusionMultiplayer.Data
{
    /// <summary>Factory for network prefab spawns via <see cref="NetworkPrefabCatalog"/>.</summary>
    public static class NetworkSpawnFactory
    {
        public static NetworkPrefabCatalog Catalog =>
            GameConfigs.EnsureCurrent()?.Prefabs;

        public static NetworkObject SpawnAvatar(NetworkRunner runner, Vector3 position, Quaternion rotation,
            PlayerRef inputAuthority)
        {
            var prefab = Catalog?.PlayerAvatar;
            if (runner == null || prefab == null)
                return null;
            return runner.Spawn(prefab, position, rotation, inputAuthority);
        }

        public static NetworkObject SpawnPlayerData(NetworkRunner runner, PlayerRef inputAuthority)
        {
            var prefab = Catalog?.PlayerData;
            if (runner == null || prefab == null)
                return null;
            return runner.Spawn(prefab, Vector3.zero, Quaternion.identity, inputAuthority);
        }

        public static NetworkObject SpawnProjectile(NetworkRunner runner, Vector3 position, Quaternion rotation,
            PlayerRef inputAuthority, NetworkRunner.OnBeforeSpawned onBeforeSpawned = null)
        {
            var prefab = Catalog?.Projectile;
            if (runner == null || prefab == null)
                return null;
            return runner.Spawn(prefab, position, rotation, inputAuthority, onBeforeSpawned);
        }

        public static NetworkObject SpawnPhysicsProp(NetworkRunner runner, Vector3 position, Quaternion rotation)
        {
            var prefab = Catalog?.PhysicsProp;
            if (runner == null || prefab == null)
                return null;
            return runner.Spawn(prefab, position, rotation, null);
        }

        public static NetworkObject SpawnPlacedBlock(NetworkRunner runner, Vector3 position, Quaternion rotation,
            PlayerRef inputAuthority)
        {
            var prefab = Catalog?.PlacedBlock;
            if (runner == null || prefab == null)
                return null;
            return runner.Spawn(prefab, position, rotation, inputAuthority);
        }
    }
}
