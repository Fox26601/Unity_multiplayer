using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FusionMultiplayer.Environment
{
    /// <summary>Instantiates configured environment prefabs (with fallback primitives).</summary>
    internal static class EnvironmentPieceSpawner
    {
        public static GameObject SpawnCube(Transform parent, string name, Vector3 position, Vector3 scale,
            Color color)
        {
            var prefab = EnvironmentPrefabRegistry.Cube;
            if (prefab != null)
                return Configure(Spawn(prefab, parent, name, position, scale, Quaternion.identity), color);

            return Configure(CreateFallbackPrimitive(PrimitiveType.Cube, parent, name, position, scale,
                Quaternion.identity), color);
        }

        public static GameObject SpawnCylinder(Transform parent, string name, Vector3 position, Vector3 scale,
            Color color)
        {
            var prefab = EnvironmentPrefabRegistry.Cylinder;
            if (prefab != null)
                return Configure(Spawn(prefab, parent, name, position, scale, Quaternion.identity), color);

            return Configure(CreateFallbackPrimitive(PrimitiveType.Cylinder, parent, name, position, scale,
                Quaternion.identity), color);
        }

        public static GameObject SpawnGround(Transform parent, string name, Vector3 scale, Color colorA, Color colorB)
        {
            GameObject go;
            var prefab = EnvironmentPrefabRegistry.Ground;
            if (prefab != null)
            {
                go = Spawn(prefab, parent, name, Vector3.zero, scale, Quaternion.identity);
            }
            else
            {
                go = CreateFallbackPrimitive(PrimitiveType.Plane, parent, name, Vector3.zero, scale,
                    Quaternion.identity);
            }

            var checker = go.GetComponent<CheckerboardGround>() ?? go.AddComponent<CheckerboardGround>();
            checker.SetPalette(colorA, colorB);
            return go;
        }

        private static GameObject Spawn(GameObject prefab, Transform parent, string name, Vector3 position,
            Vector3 scale, Quaternion rotation)
        {
            GameObject go;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            else
#endif
                go = Object.Instantiate(prefab, parent);

            go.name = name;
            var tr = go.transform;
            tr.localPosition = position;
            tr.localRotation = rotation;
            tr.localScale = scale;
            go.isStatic = true;
            return go;
        }

        private static GameObject CreateFallbackPrimitive(PrimitiveType type, Transform parent, string name,
            Vector3 position, Vector3 scale, Quaternion rotation)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var tr = go.transform;
            tr.SetParent(parent, false);
            tr.localPosition = position;
            tr.localRotation = rotation;
            tr.localScale = scale;
            go.isStatic = true;
            if (go.GetComponent<EnvironmentPiece>() == null)
                go.AddComponent<EnvironmentPiece>();
            return go;
        }

        private static GameObject Configure(GameObject go, Color color)
        {
            ApplyColor(go, color);
            if (go.GetComponent<EnvironmentPiece>() == null)
                go.AddComponent<EnvironmentPiece>();
            go.isStatic = true;
            return go;
        }

        private static void ApplyColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return;

            var shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Diffuse");
            if (shader == null)
                return;

            renderer.sharedMaterial = new Material(shader) { color = color };
        }
    }
}
