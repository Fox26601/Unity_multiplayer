using System.Threading.Tasks;
using Fusion;
using FusionMultiplayer.Player;
using FusionMultiplayer.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FusionMultiplayer.Core
{
    public partial class ConnectionManager
    {
        public async Task ShutdownToMainMenuAsync(bool clearReconnect = true)
        {
            _intentionalShutdown = true;
            CancelLifecycleOperations();
            GameplayInputMode.ChatBlockingGameplay = false;
            GameplayInputMode.PauseBlockingGameplay = false;
            GameplayInputMode.SetMenu();

            if (clearReconnect)
            {
                SessionReconnectStore.Clear();
                SessionData.ClearReconnectToken();
            }
#if UNITY_EDITOR
            if (MppmUtility.IsMainInstance)
#endif
                MppmSessionBridge.ClearRoom();
            _activeRoomName = string.Empty;
            await DisposeRunnerAsync();
            _sessionList.Clear();
            SceneManager.LoadScene(SceneIndices.MainMenu);
            GameplayInputMode.SetMenu();
        }

        /// <summary>
        /// Leave to Main Menu. Mid-match keeps reconnect store/token (same as force-quit);
        /// lobby leave clears them.
        /// </summary>
        public async Task LeaveToMainMenuAsync()
        {
            var inMatch = SceneIndices.IsGameScene(SceneManager.GetActiveScene().buildIndex);
            await ShutdownToMainMenuAsync(clearReconnect: !inMatch);
        }

        /// <summary>Client: store error, leave session, reload Main Menu.</summary>
        public static void NotifyNicknameRejected(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                reason = UiCopy.NicknameTaken;

            SessionData.SetSessionError(reason);

            if (Instance != null)
            {
                // Prevent OnDisconnectedFromServer from overwriting the nick message with "Disconnected: Requested".
                Instance._intentionalShutdown = true;
                SessionFailed?.Invoke(reason);
                _ = Instance.ShutdownToMainMenuAsync(clearReconnect: true);
            }
            else
            {
                SessionFailed?.Invoke(reason);
            }
        }

        /// <summary>
        /// Master: drop a joining player after nickname reject.
        /// Delays Disconnect so RpcNicknameRejected can flush before the PlayerData object is torn down.
        /// </summary>
        public void RejectJoiningPlayer(PlayerRef player, NetworkObject playerDataObject)
        {
            if (_runner == null || !NetworkAuthority.IsSessionAuthority(_runner) || player == PlayerRef.None)
                return;

            StartCoroutine(RejectJoiningPlayerAfterRpcFlush(player, playerDataObject));
        }

        private System.Collections.IEnumerator RejectJoiningPlayerAfterRpcFlush(PlayerRef player,
            NetworkObject playerDataObject)
        {
            // Fusion needs a few ticks to deliver the targeted reject RPC before Despawn/Disconnect.
            for (var i = 0; i < 12; i++)
                yield return null;

            if (_runner == null)
                yield break;

            if (IsPlayerStillConnected(_runner, player))
                _runner.Disconnect(player);

            if (playerDataObject != null && playerDataObject.IsValid)
                _runner.Despawn(playerDataObject);
        }

        private static bool IsPlayerStillConnected(NetworkRunner runner, PlayerRef player)
        {
            if (runner == null || player == PlayerRef.None)
                return false;

            foreach (var active in runner.ActivePlayers)
            {
                if (active == player)
                    return true;
            }

            return false;
        }

        private async Task DisposeRunnerAsync()
        {
            CancelLifecycleOperations();
            _inSessionLobby = false;
            _activeRoomName = string.Empty;
            PlayerRegistry.Clear();

            if (_runner == null)
                return;

            try
            {
                if (_runner.IsRunning || _runner.IsCloudReady)
                    await _runner.Shutdown(false, ShutdownReason.Ok);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Runner shutdown: {ex.Message}");
            }

            if (_runner != null)
            {
                Destroy(_runner);
                _runner = null;
            }
        }
    }
}
