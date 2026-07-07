using System.Collections;
using FusionMultiplayer.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// MPPM virtual players: wait for the main editor to host, then auto-join the same room.
    /// </summary>
    public sealed class MppmAutoJoin : MonoBehaviour
    {
        private const float PollInterval = 0.35f;
        private const float TimeoutSeconds = 90f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
#if UNITY_EDITOR
            if (!MppmUtility.IsVirtualInstance)
                return;

            if (SceneIndices.IsGameScene(SceneManager.GetActiveScene().buildIndex))
            {
                Debug.Log(
                    "[FusionMultiplayer] MPPM clone started on a game map without a session — loading 00_MainMenu for auto-join.");
                SceneManager.LoadScene(SceneIndices.MainMenu);
            }

            var go = new GameObject(nameof(MppmAutoJoin));
            DontDestroyOnLoad(go);
            go.AddComponent<MppmAutoJoin>();
#endif
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (!MppmUtility.IsVirtualInstance)
            {
                Destroy(gameObject);
                return;
            }

            SessionData.Nickname = MppmUtility.GetVirtualPlayerLabel();
            StartCoroutine(AutoJoinRoutine());
#endif
        }

#if UNITY_EDITOR
        private IEnumerator AutoJoinRoutine()
        {
            if (SessionData.UseOfflineMode)
            {
                Debug.LogWarning(
                    "[FusionMultiplayer] MPPM clone: Offline Play is ON (single-player only). " +
                    "Disable Tools → Fusion Multiplayer → Enable Offline Play for multi-client MPPM.");
            }

            var elapsed = 0f;
            while (elapsed < TimeoutSeconds)
            {
                if (SceneManager.GetActiveScene().buildIndex != SceneIndices.MainMenu)
                {
                    yield return null;
                    elapsed += Time.unscaledDeltaTime;
                    continue;
                }

                var cm = ConnectionManager.Instance;
                if (cm == null)
                {
                    cm = Object.FindFirstObjectByType<ConnectionManager>();
                    if (cm == null)
                    {
                        yield return new WaitForSecondsRealtime(PollInterval);
                        elapsed += PollInterval;
                        continue;
                    }
                }

                var runner = cm.Runner;
                if (runner != null && runner.IsRunning)
                {
                    Destroy(gameObject);
                    yield break;
                }

                if (MppmSessionBridge.TryReadRoom(out var room))
                {
                    var flow = Object.FindFirstObjectByType<SessionFlowUI>();
                    flow?.SetStatus($"MPPM auto-joining room \"{room}\"...", false);

                    var task = cm.JoinSessionAsync(room);
                    while (!task.IsCompleted)
                        yield return null;

                    if (task.Result)
                    {
                        Debug.Log($"[FusionMultiplayer] MPPM clone joined room \"{room}\" as {SessionData.Nickname}.");
                        Destroy(gameObject);
                        yield break;
                    }

                    flow?.SetStatus("MPPM auto-join failed — check console.", true);
                    yield break;
                }

                yield return new WaitForSecondsRealtime(PollInterval);
                elapsed += PollInterval;
            }

            Debug.LogWarning(
                "[FusionMultiplayer] MPPM clone timed out waiting for the main editor to host a room. " +
                "On Player 1: open 00_MainMenu, enter room name, click Create / Host.");
        }
#endif
    }
}
