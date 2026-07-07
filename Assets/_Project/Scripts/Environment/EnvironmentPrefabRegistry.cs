using UnityEngine;

namespace FusionMultiplayer.Environment
{
    /// <summary>Loads environment piece prefabs from Resources/Environment.</summary>
    public static class EnvironmentPrefabRegistry
    {
        private const string CubeResource = "Environment/EnvPiece_Cube";
        private const string CylinderResource = "Environment/EnvPiece_Cylinder";
        private const string GroundResource = "Environment/EnvPiece_Ground";

        private static GameObject _cube;
        private static GameObject _cylinder;
        private static GameObject _ground;

        public static GameObject Cube => _cube ??= Load(CubeResource);
        public static GameObject Cylinder => _cylinder ??= Load(CylinderResource);
        public static GameObject Ground => _ground ??= Load(GroundResource);

        public static bool HasAllPrefabs =>
            Cube != null && Cylinder != null && Ground != null;

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
