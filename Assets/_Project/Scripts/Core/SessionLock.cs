using Fusion;

namespace FusionMultiplayer.Core
{
    /// <summary>Closes the Photon session when the match starts (server / host only).</summary>
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
            info.IsOpen = false;
            return true;
        }
    }
}
