using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace FusionMultiplayer.Core
{
    /// <summary>Closes the Photon session when the match starts (master client only).</summary>
    public static class SessionLock
    {
        public static bool TryLockForGameStart(NetworkRunner runner)
        {
            if (runner == null || !runner.IsRunning || !runner.IsSharedModeMasterClient)
                return false;

            var info = runner.SessionInfo;
            if (!info.IsValid)
                return false;

            if (SessionCatalog.IsSessionStarted(info))
                return true;

            info.UpdateCustomProperties(new Dictionary<string, SessionProperty>
            {
                { SessionCatalog.PropPhase, (int)SessionCatalog.SessionPhase.Started }
            });
            info.IsOpen = false;
            return true;
        }
    }
}
