using UnityEngine;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// Local session preferences before networked PlayerData is spawned.
    /// </summary>
    public static class SessionData
    {
        public const string OfflineModePrefKey = "FusionMultiplayer.OfflineMode";

        public static string Nickname { get; set; } = "Player";
        public static Color Tint { get; set; } = Color.white;

        /// <summary>Selected before create/join on the main menu.</summary>
        public static SessionCatalog.GameModeKind SelectedGameMode { get; set; } = SessionCatalog.GameModeKind.Build;

        /// <summary>Preferred map filter in the session browser.</summary>
        public static SessionCatalog.MapKind SelectedMap { get; set; } = SessionCatalog.MapKind.Arena;

        /// <summary>When true, created sessions are not visible in the public browser.</summary>
        public static bool HiddenSession { get; set; }

        /// <summary>Editor/local play without Photon Cloud (GameMode.Single).</summary>
        public static bool UseOfflineMode
        {
            get => PlayerPrefs.GetInt(OfflineModePrefKey, 0) == 1;
            set => PlayerPrefs.SetInt(OfflineModePrefKey, value ? 1 : 0);
        }
    }
}
