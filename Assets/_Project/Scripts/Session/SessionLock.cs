using System;
using System.Collections.Generic;
using Fusion;
using FusionMultiplayer.Data;
using FusionMultiplayer.Player;
using UnityEngine;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// Match start hard-close (Google Doc #13) plus timed reconnect window for crash reconnect (bonus 15).
    /// Local deadline cache avoids closing the window before Photon property echo arrives.
    /// </summary>
    public static class SessionLock
    {
        public const string PropReconnectUntil = "reUntil";
        public const int ReconnectWindowSeconds = 90;

        public static int ActiveReconnectWindowSeconds =>
            GameConfigs.EnsureCurrent()?.Reconnect != null
                ? GameConfigs.Current.Reconnect.WindowSeconds
                : ReconnectWindowSeconds;

        private static bool _pendingReconnectWindow;
        private static long _localReconnectUntilUnix;

        /// <summary>Marks match started and hard-closes the room (IsOpen=false).</summary>
        public static bool TryLockForGameStart(NetworkRunner runner)
        {
            if (!NetworkAuthority.CanMutateSession(runner))
                return false;

            var info = runner.SessionInfo;
            if (!info.IsValid)
                return false;

            if (SessionCatalog.IsSessionStarted(info))
            {
                // Do not clobber an active reconnect window.
                if (IsReconnectWindowOpen(info))
                    return true;

                if (info.IsOpen)
                {
                    ClearLocalWindow();
                    info.UpdateCustomProperties(new Dictionary<string, SessionProperty>
                    {
                        { PropReconnectUntil, 0 }
                    });
                    info.IsOpen = false;
                }

                return true;
            }

            ClearLocalWindow();
            _pendingReconnectWindow = false;
            info.UpdateCustomProperties(new Dictionary<string, SessionProperty>
            {
                { SessionCatalog.PropPhase, (int)SessionCatalog.SessionPhase.Started },
                { PropReconnectUntil, 0 }
            });
            info.IsOpen = false;
            return true;
        }

        /// <summary>
        /// Request reconnect window now if this peer can mutate session; otherwise mark pending
        /// until Shared master promotion (master-leave race).
        /// </summary>
        public static void RequestReconnectWindow(NetworkRunner runner, int seconds = -1)
        {
            if (seconds < 0)
                seconds = ActiveReconnectWindowSeconds;

            if (runner == null || !runner.IsRunning)
                return;

            if (!runner.SessionInfo.IsValid || !SessionCatalog.IsSessionStarted(runner.SessionInfo))
                return;

            _pendingReconnectWindow = true;

            if (NetworkAuthority.CanMutateSession(runner))
                OpenReconnectWindow(runner, seconds);
            else
                DebugLog("Reconnect window pending — waiting for Shared master authority");
        }

        /// <summary>New Shared master: open window if leave was queued before we had authority.</summary>
        public static void TryFulfillPendingReconnectWindow(NetworkRunner runner)
        {
            if (runner == null || !NetworkAuthority.CanMutateSession(runner))
                return;

            if (!runner.SessionInfo.IsValid || !SessionCatalog.IsSessionStarted(runner.SessionInfo))
                return;

            if (!_pendingReconnectWindow && !HasPendingBotSeats() && !IsLocalWindowActive())
                return;

            OpenReconnectWindow(runner);
        }

        /// <summary>
        /// After unexpected disconnect: briefly reopen so known ConnectionToken can join.
        /// Unknown joiners are still refused in OnConnectRequest.
        /// </summary>
        public static void OpenReconnectWindow(NetworkRunner runner, int seconds = -1)
        {
            if (seconds < 0)
                seconds = ActiveReconnectWindowSeconds;

            if (!NetworkAuthority.CanMutateSession(runner))
            {
                _pendingReconnectWindow = true;
                return;
            }

            var info = runner.SessionInfo;
            if (!info.IsValid || !SessionCatalog.IsSessionStarted(info))
                return;

            var until = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + Math.Max(15, seconds);
            _localReconnectUntilUnix = until;
            _pendingReconnectWindow = false;

            info.UpdateCustomProperties(new Dictionary<string, SessionProperty>
            {
                { SessionCatalog.PropPhase, (int)SessionCatalog.SessionPhase.Started },
                { PropReconnectUntil, (int)until }
            });
            info.IsOpen = true;
            DebugLog($"Reconnect window open until unix {until}");
        }

        public static void CloseReconnectWindow(NetworkRunner runner)
        {
            if (!NetworkAuthority.CanMutateSession(runner))
                return;

            var info = runner.SessionInfo;
            if (!info.IsValid || !SessionCatalog.IsSessionStarted(info))
                return;

            ClearLocalWindow();
            _pendingReconnectWindow = false;
            info.UpdateCustomProperties(new Dictionary<string, SessionProperty>
            {
                { PropReconnectUntil, 0 }
            });
            info.IsOpen = false;
            DebugLog("Reconnect window closed (hard-close)");
        }

        public static void CloseReconnectWindowIfDue(NetworkRunner runner)
        {
            if (!NetworkAuthority.CanMutateSession(runner))
                return;

            var info = runner.SessionInfo;
            if (!info.IsValid || !SessionCatalog.IsSessionStarted(info))
                return;

            // Still waiting to become master after a leave — do not touch IsOpen.
            if (_pendingReconnectWindow)
            {
                if (NetworkAuthority.CanMutateSession(runner))
                    OpenReconnectWindow(runner);
                return;
            }

            if (!info.IsOpen && !IsLocalWindowActive())
                return;

            if (IsReconnectWindowOpen(info) || IsLocalWindowActive())
                return;

            // Deadline passed (or IsOpen without a valid deadline): purge abandoned bot seats
            // and hard-close. Do not re-open the window because bots remain.
            if (info.IsOpen || HasPendingBotSeats())
                ReconnectService.ExpireAbandonedSeats(runner);
        }

        /// <summary>Call after successful reclaim — close early when no bot seats remain.</summary>
        public static void TryCloseReconnectWindowAfterReclaim(NetworkRunner runner)
        {
            if (!NetworkAuthority.CanMutateSession(runner))
                return;

            if (HasPendingBotSeats() || BotTakeover.HasPendingTakeovers)
                return;

            CloseReconnectWindow(runner);
        }

        public static bool IsReconnectWindowOpen(SessionInfo info)
        {
            if (!info.IsValid)
                return false;

            if (IsLocalWindowActive())
                return true;

            if (!info.IsOpen)
                return false;

            if (!TryGetReconnectUntil(info, out var until) || until <= 0)
                return false;

            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() <= until;
        }

        public static bool TryGetReconnectUntil(SessionInfo info, out long unixSeconds)
        {
            unixSeconds = 0;
            if (!info.IsValid || info.Properties == null ||
                !info.Properties.TryGetValue(PropReconnectUntil, out var prop))
            {
                if (IsLocalWindowActive())
                {
                    unixSeconds = _localReconnectUntilUnix;
                    return true;
                }

                return false;
            }

            unixSeconds = prop;
            // Prefer the later of cloud vs local so a fresh open is not treated as expired.
            if (_localReconnectUntilUnix > unixSeconds)
                unixSeconds = _localReconnectUntilUnix;
            return true;
        }

        public static bool HasPendingBotSeats()
        {
            foreach (var pd in PlayerRegistry.EnumerateAllData())
            {
                if (pd == null || pd.Object == null || !pd.Object.IsValid)
                    continue;
                if (pd.IsBotControlled)
                    return true;
            }

            return false;
        }

        private static bool IsLocalWindowActive() =>
            _localReconnectUntilUnix > 0 &&
            DateTimeOffset.UtcNow.ToUnixTimeSeconds() <= _localReconnectUntilUnix;

        private static void ClearLocalWindow() => _localReconnectUntilUnix = 0;

        private static void DebugLog(string msg) =>
            Debug.Log($"[FusionMultiplayer] SessionLock: {msg}");
    }
}
