using FusionMultiplayer.Data;
using UnityEngine;

namespace FusionMultiplayer.Environment
{
    /// <summary>Loads environment piece prefabs from catalog or Resources/Environment.</summary>
    public static class EnvironmentPrefabRegistry
    {
        private const string CubeResource = "Environment/EnvPiece_Cube";
        private const string CylinderResource = "Environment/EnvPiece_Cylinder";
        private const string GroundResource = "Environment/EnvPiece_Ground";

        private static GameObject _cube;
        private static GameObject _cylinder;
        private static GameObject _ground;

        public static GameObject Cube =>
            Resolve(ref _cube, GameConfigs.EnsureCurrent()?.Prefabs?.EnvCube, CubeResource);

        public static GameObject Cylinder =>
            Resolve(ref _cylinder, GameConfigs.EnsureCurrent()?.Prefabs?.EnvCylinder, CylinderResource);

        public static GameObject Ground =>
            Resolve(ref _ground, GameConfigs.EnsureCurrent()?.Prefabs?.EnvGround, GroundResource);

        public static bool HasAllPrefabs =>
            Cube != null && Cylinder != null && Ground != null;

        private static GameObject Resolve(ref GameObject cache, GameObject catalogPrefab, string resourcePath)
        {
            if (catalogPrefab != null)
                return catalogPrefab;
            return cache ??= Load(resourcePath);
        }

        private static GameObject Load(string path)
        {
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
                Debug.LogWarning(
                    $"[FusionMultiplayer] Missing environment prefab at Resources/{path}. " +
                    "Run Tools → Fusion Multiplayer → Generate Environment Prefabs.");
            return prefab;
        }
    }
}
