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

        /// <summary>Editor/local play without Photon Cloud (GameMode.Single).</summary>
        public static bool UseOfflineMode
        {
            get => PlayerPrefs.GetInt(OfflineModePrefKey, 0) == 1;
            set => PlayerPrefs.SetInt(OfflineModePrefKey, value ? 1 : 0);
        }
    }
}
