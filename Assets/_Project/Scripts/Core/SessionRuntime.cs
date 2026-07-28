using Fusion;

namespace FusionMultiplayer.Core
{
    /// <summary>Runtime game mode from session properties (or offline SessionData fallback).</summary>
    public static class SessionRuntime
    {
        private static SessionCatalog.GameModeKind _cachedMode = SessionCatalog.GameModeKind.Sandbox;
        private static bool _initialized;

        public static SessionCatalog.GameModeKind CurrentMode
        {
            get
            {
                Refresh();
                return _cachedMode;
            }
        }

        public static bool AllowsBuild =>
            CurrentMode is SessionCatalog.GameModeKind.Build or SessionCatalog.GameModeKind.Sandbox;

        public static bool AllowsShoot =>
            CurrentMode is SessionCatalog.GameModeKind.Combat or SessionCatalog.GameModeKind.Sandbox;

        /// <summary>Sandbox allows breaking any placed block; Build protects host blocks.</summary>
        public static bool AllowsBreakAnyBlock =>
            CurrentMode == SessionCatalog.GameModeKind.Sandbox;

        public static void Refresh(NetworkRunner runner = null)
        {
            runner ??= ConnectionManager.Instance != null ? ConnectionManager.Instance.Runner : null;

            if (runner != null && runner.IsRunning &&
                SessionCatalog.TryGetGameMode(runner.SessionInfo, out var mode))
            {
                _cachedMode = mode;
                _initialized = true;
                return;
            }

            if (!_initialized || runner == null || !runner.IsRunning)
                _cachedMode = SessionData.SelectedGameMode;
        }
    }
}
