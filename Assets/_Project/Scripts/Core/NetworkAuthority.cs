using Fusion;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// Client-Server authority helpers. Dedicated Server / Host own StateAuthority;
    /// Shared Mode master checks are intentionally not used.
    /// </summary>
    public static class NetworkAuthority
    {
        public static bool IsServerOrHost(NetworkRunner runner)
        {
            if (runner == null || !runner.IsRunning)
                return false;

            return runner.IsServer || runner.IsSceneAuthority;
        }

        public static bool CanMutateSession(NetworkRunner runner) => IsServerOrHost(runner);

        public static bool IsClientOnly(NetworkRunner runner)
        {
            if (runner == null || !runner.IsRunning)
                return false;

            return runner.IsClient && !runner.IsServer;
        }

        public static GameMode ResolveStartMode(bool createIfMissing, bool offline, bool dedicated)
        {
            if (offline)
                return GameMode.Single;

            if (dedicated)
                return GameMode.Server;

            if (createIfMissing)
                return GameMode.Host;

            return GameMode.Client;
        }
    }
}
