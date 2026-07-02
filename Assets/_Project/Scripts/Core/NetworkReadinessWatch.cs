using System;
using System.Collections;
using UnityEngine;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// Shared coroutine helper for waiting until a networked scene service becomes active.
    /// </summary>
    public static class NetworkReadinessWatch
    {
        public static IEnumerator WaitUntil(
            Func<bool> isReady,
            Action<string, bool, bool> logState,
            Action onReady,
            Action onTimeout,
            float timeoutSeconds = 8f,
            float pollIntervalSeconds = 1f)
        {
            var elapsed = 0f;
            while (elapsed < timeoutSeconds)
            {
                if (isReady())
                {
                    onReady?.Invoke();
                    yield break;
                }

                logState?.Invoke($"waiting ({elapsed:F1}s)", false, false);
                yield return new WaitForSecondsRealtime(pollIntervalSeconds);
                elapsed += pollIntervalSeconds;
            }

            onTimeout?.Invoke();
        }
    }
}
