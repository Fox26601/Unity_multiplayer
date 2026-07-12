namespace FusionMultiplayer.Core
{
    /// <summary>
    /// Build Settings scene order (must match File &gt; Build Settings).
    /// Resolves by scene file name so Boot can be inserted without breaking references.
    /// </summary>
    public static class SceneIndices
    {
        private static bool _resolved;
        private static int _boot = -1;
        private static int _mainMenu = 0;
        private static int _lobby = 1;
        private static int _arena = 2;
        private static int _plaza = 3;
        private static int _ruins = 4;

        public static int Boot
        {
            get
            {
                EnsureResolved();
                return _boot;
            }
        }

        public static int MainMenu
        {
            get
            {
                EnsureResolved();
                return _mainMenu;
            }
        }

        public static int Lobby
        {
            get
            {
                EnsureResolved();
                return _lobby;
            }
        }

        public static int Arena
        {
            get
            {
                EnsureResolved();
                return _arena;
            }
        }

        public static int Plaza
        {
            get
            {
                EnsureResolved();
                return _plaza;
            }
        }

        public static int Ruins
        {
            get
            {
                EnsureResolved();
                return _ruins;
            }
        }

        /// <summary>Legacy alias — default game map.</summary>
        public static int Game => Arena;

        public static bool IsGameScene(int buildIndex) =>
            buildIndex == Arena || buildIndex == Plaza || buildIndex == Ruins;

        public static int GetBuildIndex(SessionCatalog.MapKind map) => map switch
        {
            SessionCatalog.MapKind.Plaza => Plaza,
            SessionCatalog.MapKind.Ruins => Ruins,
            _ => Arena
        };

        public static SessionCatalog.MapKind GetMapKind(int buildIndex)
        {
            if (buildIndex == Plaza) return SessionCatalog.MapKind.Plaza;
            if (buildIndex == Ruins) return SessionCatalog.MapKind.Ruins;
            return SessionCatalog.MapKind.Arena;
        }

        private static void EnsureResolved()
        {
            if (_resolved)
                return;

            _boot = Find("00_Boot", -1);
            _mainMenu = Find("00_MainMenu", 0);
            _lobby = Find("01_Lobby", 1);
            _arena = Find("02_Game_Arena", 2);
            _plaza = Find("03_Game_Plaza", 3);
            _ruins = Find("04_Game_Ruins", 4);
            _resolved = true;
        }

        private static int Find(string sceneName, int fallback)
        {
            var count = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            for (var i = 0; i < count; i++)
            {
                var path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
                if (string.IsNullOrEmpty(path))
                    continue;
                if (path.EndsWith($"/{sceneName}.unity") || path.EndsWith($"{sceneName}.unity"))
                    return i;
            }

            return fallback;
        }
    }
}
