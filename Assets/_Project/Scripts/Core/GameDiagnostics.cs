using UnityEngine;

namespace FusionMultiplayer.Core
{
    /// <summary>Structured console logging for GameManager readiness and late-join diagnostics.</summary>
    public static class GameDiagnostics
    {
        private const string Prefix = "[FusionMultiplayer][Game]";

        public static void Log(string message)
        {
            Debug.Log($"{Prefix} {message}");
        }

        public static void LogWarning(string message)
        {
            Debug.LogWarning($"{Prefix} {message}");
        }

        public static void LogSpawned(bool objectValid)
        {
            Log($"spawned objectValid={objectValid}");
        }

        public static void LogDespawned()
        {
            Log("despawned");
        }

        public static void LogReady(string phase, bool instanceOk, bool objectValid)
        {
            Log($"{phase}: instance={(instanceOk ? "ok" : "null")} objectValid={objectValid}");
        }

        public static void LogSceneLoad(string sceneName, int buildIndex, int localPlayerId)
        {
            Log($"scene loaded name={sceneName} buildIndex={buildIndex} localPlayer={localPlayerId}");
        }

        public static void LogLateJoinProvision(string reason)
        {
            Log($"late-join provision: {reason}");
        }

        public static void LogTimeout(float seconds)
        {
            LogWarning($"GameManager not ready after {seconds:F0}s timeout.");
        }
    }
}
