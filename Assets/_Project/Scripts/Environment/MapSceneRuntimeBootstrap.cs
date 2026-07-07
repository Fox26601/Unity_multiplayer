using FusionMultiplayer.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FusionMultiplayer.Environment
{
    /// <summary>Ensures map geometry and spawns match the active game scene build index.</summary>
    public static class MapSceneRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterSceneLoad()
        {
            var scene = SceneManager.GetActiveScene();
            if (!SceneIndices.IsGameScene(scene.buildIndex))
                return;

            var map = SceneIndices.GetMapKind(scene.buildIndex);
            RemoveLegacyGround();

            var mapRoot = GameObject.Find("MapEnvironment");
            if (mapRoot == null)
                mapRoot = new GameObject("MapEnvironment");

            var marker = mapRoot.GetComponent<MapSceneMarker>() ?? mapRoot.AddComponent<MapSceneMarker>();
            marker.ApplyMapKind(map);

            var builder = mapRoot.GetComponent<MapEnvironmentBuilder>() ?? mapRoot.AddComponent<MapEnvironmentBuilder>();
            builder.Rebuild();

            var spawnRoot = GameObject.Find("SpawnPoints")?.transform;
            if (spawnRoot != null && spawnRoot.childCount >= 10)
                MapSpawnLayout.PlaceSpawnPoints(spawnRoot, map);
        }

        private static void RemoveLegacyGround()
        {
            var ground = GameObject.Find("Ground");
            if (ground == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(ground);
            else
                Object.DestroyImmediate(ground);
        }
    }
}
