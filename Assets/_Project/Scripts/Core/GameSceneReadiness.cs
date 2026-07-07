using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// Facade for GameManager scene-object readiness (late-join and UI gating).
    /// </summary>
    public static class GameSceneReadiness
    {
        public static event Action<GameManager> GameManagerReady;
        public static event Action GameManagerLost;

        private static bool _subscribed;
        private static GameManager _lastNotifiedGm;

        public static bool IsGameManagerReady => TryGetGameManager(out _);

        public static void EnsureSubscribed()
        {
            if (_subscribed)
                return;

            ConnectionManager.SceneLoaded += OnSceneLoaded;
            ConnectionManager.NetworkPlayersChanged += OnNetworkPlayersChanged;
            _subscribed = true;
        }

        public static bool TryGetGameManager(out GameManager gm)
        {
            gm = GameManager.Instance;
            if (gm != null && gm.IsNetworkActive)
                return true;

            foreach (var candidate in UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None))
            {
                if (candidate == null || !candidate.IsNetworkActive)
                    continue;

                gm = candidate;
                return true;
            }

            gm = null;
            return false;
        }

        public static void NotifyGameManagerReady(GameManager gm)
        {
            if (gm == null || !gm.IsNetworkActive)
                return;

            if (_lastNotifiedGm == gm)
                return;

            _lastNotifiedGm = gm;
            GameDiagnostics.LogReady("ready", true, true);
            GameManagerReady?.Invoke(gm);
        }

        public static void NotifyGameManagerLost()
        {
            _lastNotifiedGm = null;
            GameDiagnostics.LogDespawned();
            GameManagerLost?.Invoke();
        }

        public static void EvaluateNow()
        {
            if (TryGetGameManager(out var gm))
                NotifyGameManagerReady(gm);
        }

        public static IEnumerator WaitForGameManager(
            Action<bool> onComplete,
            float timeoutSeconds = 8f,
            float pollIntervalSeconds = 1f)
        {
            var completed = false;
            yield return NetworkReadinessWatch.WaitUntil(
                () => TryGetGameManager(out _),
                (phase, instanceOk, objectValid) =>
                {
                    TryGetGameManager(out var gm);
                    var hasInstance = gm != null;
                    var valid = gm != null && gm.IsNetworkActive;
                    GameDiagnostics.LogReady(phase, hasInstance, valid);
                },
                () =>
                {
                    if (TryGetGameManager(out var gm))
                        NotifyGameManagerReady(gm);
                    completed = true;
                    onComplete?.Invoke(true);
                },
                () =>
                {
                    GameDiagnostics.LogTimeout(timeoutSeconds);
                    completed = true;
                    onComplete?.Invoke(false);
                },
                timeoutSeconds,
                pollIntervalSeconds);

            if (!completed)
                onComplete?.Invoke(TryGetGameManager(out _));
        }

        private static void OnSceneLoaded(string sceneName)
        {
            if (!SceneIndices.IsGameScene(SceneManager.GetActiveScene().buildIndex))
                return;

            EvaluateNow();
        }

        private static void OnNetworkPlayersChanged()
        {
            if (!SceneIndices.IsGameScene(SceneManager.GetActiveScene().buildIndex))
                return;

            EvaluateNow();
        }
    }
}
