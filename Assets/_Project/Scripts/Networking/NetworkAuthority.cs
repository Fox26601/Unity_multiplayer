using Fusion;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// Shared Mode session authority helpers. Match services live on the Shared master client.
    /// </summary>
    public static class NetworkAuthority
    {
        /// <summary>
        /// True for the peer that may mutate session-wide state (Shared master, or offline Single).
        /// </summary>
        public static bool IsServerOrHost(NetworkRunner runner) => IsSessionAuthority(runner);

        public static bool IsSessionAuthority(NetworkRunner runner)
        {
            if (runner == null || !runner.IsRunning)
                return false;

            if (runner.GameMode == GameMode.Single)
                return true;

            if (runner.GameMode == GameMode.Shared)
                return runner.IsSharedModeMasterClient;

            // Fallback if a legacy Host/Server runner is somehow active.
            return runner.IsServer || runner.IsSceneAuthority || runner.IsSharedModeMasterClient;
        }

        public static bool CanMutateSession(NetworkRunner runner) => IsSessionAuthority(runner);

        public static bool IsClientOnly(NetworkRunner runner)
        {
            if (runner == null || !runner.IsRunning)
                return false;

            if (runner.GameMode == GameMode.Shared)
                return !runner.IsSharedModeMasterClient;

            return runner.IsClient && !runner.IsServer;
        }

        /// <summary>Create and join both use Shared; offline uses Single. Dedicated path is disabled.</summary>
        public static GameMode ResolveStartMode(bool createIfMissing, bool offline, bool dedicated)
        {
            if (offline)
                return GameMode.Single;

            // Dedicated Server is out of scope; always Shared for online play.
            _ = createIfMissing;
            _ = dedicated;
            return GameMode.Shared;
        }
    }
}
