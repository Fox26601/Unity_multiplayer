using Fusion;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// Marks the session as started when the match begins.
    /// Keeps <see cref="SessionInfo.IsOpen"/> true so crash-reconnect can still reach
    /// <see cref="ConnectionManager.OnConnectRequest"/>; new players are refused there via phase + token.
    /// </summary>
    public static class SessionLock
    {
        public static bool TryLockForGameStart(NetworkRunner runner)
        {
            if (!NetworkAuthority.CanMutateSession(runner))
                return false;

            var info = runner.SessionInfo;
            if (!info.IsValid)
                return false;

            if (SessionCatalog.IsSessionStarted(info))
                return true;

            info.UpdateCustomProperties(new System.Collections.Generic.Dictionary<string, SessionProperty>
            {
                { SessionCatalog.PropPhase, (int)SessionCatalog.SessionPhase.Started }
            });
            // Must stay open: Photon GameClosed (32764) rejects reconnects before host OnConnectRequest.
            info.IsOpen = true;
            return true;
        }
    }
}
