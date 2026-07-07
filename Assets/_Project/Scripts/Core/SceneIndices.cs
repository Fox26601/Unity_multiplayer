namespace FusionMultiplayer.Core
{
    /// <summary>
    /// Build Settings scene order (must match File &gt; Build Settings).
    /// </summary>
    public static class SceneIndices
    {
        public const int MainMenu = 0;
        public const int Lobby = 1;
        public const int Arena = 2;
        public const int Plaza = 3;
        public const int Ruins = 4;

        /// <summary>Legacy alias — default game map.</summary>
        public const int Game = Arena;

        public static bool IsGameScene(int buildIndex) =>
            buildIndex == Arena || buildIndex == Plaza || buildIndex == Ruins;

        public static int GetBuildIndex(SessionCatalog.MapKind map) => map switch
        {
            SessionCatalog.MapKind.Plaza => Plaza,
            SessionCatalog.MapKind.Ruins => Ruins,
            _ => Arena
        };

        public static SessionCatalog.MapKind GetMapKind(int buildIndex) => buildIndex switch
        {
            Plaza => SessionCatalog.MapKind.Plaza,
            Ruins => SessionCatalog.MapKind.Ruins,
            _ => SessionCatalog.MapKind.Arena
        };
    }
}
