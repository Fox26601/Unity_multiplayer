using System;
using UnityEngine;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// Local session preferences before networked PlayerData is spawned.
    /// </summary>
    public static class SessionData
    {
        public const string OfflineModePrefKey = "FusionMultiplayer.OfflineMode";
        public const int MinPlayers = 2;
        public const int MaxPlayersCap = 10;
        public const int DefaultMaxPlayers = 10;

        public static string Nickname { get; set; } = "Player";
        public static Color Tint { get; set; } = Color.white;

        /// <summary>Selected before create/join on the main menu.</summary>
        public static SessionCatalog.GameModeKind SelectedGameMode { get; set; } = SessionCatalog.GameModeKind.Build;

        /// <summary>Preferred map filter in the session browser (Any = no map filter).</summary>
        public static SessionCatalog.MapKind SelectedMap { get; set; } = SessionCatalog.MapKind.Any;

        /// <summary>Match difficulty stored in session properties.</summary>
        public static SessionCatalog.DifficultyKind SelectedDifficulty { get; set; } =
            SessionCatalog.DifficultyKind.Normal;

        /// <summary>Room capacity chosen by the creator (2–10).</summary>
        public static int MaxPlayers { get; set; } = DefaultMaxPlayers;

        /// <summary>When true, created sessions are not visible in the public browser.</summary>
        public static bool HiddenSession { get; set; }

        /// <summary>Stable token used for crash reconnect.</summary>
        public static string ReconnectToken { get; private set; }

        /// <summary>Editor/local play without Photon Cloud (GameMode.Single).</summary>
        public static bool UseOfflineMode
        {
            get => PlayerPrefs.GetInt(OfflineModePrefKey, 0) == 1;
            set => PlayerPrefs.SetInt(OfflineModePrefKey, value ? 1 : 0);
        }

        /// <summary>True when launched with -dedicated (headless server process).</summary>
        public static bool IsDedicatedLaunch
        {
            get
            {
                foreach (var arg in System.Environment.GetCommandLineArgs())
                {
                    if (string.Equals(arg, "-dedicated", StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                return false;
            }
        }

        public static string EnsureReconnectToken()
        {
            if (!string.IsNullOrWhiteSpace(ReconnectToken))
                return ReconnectToken;

            if (SessionReconnectStore.TryLoad(out _, out var stored, out _) &&
                !string.IsNullOrWhiteSpace(stored))
            {
                ReconnectToken = stored;
                return ReconnectToken;
            }

            ReconnectToken = Guid.NewGuid().ToString("N");
            return ReconnectToken;
        }

        public static void SetReconnectToken(string token)
        {
            if (!string.IsNullOrWhiteSpace(token))
                ReconnectToken = token.Trim();
        }

        public static int ClampMaxPlayers(int value) =>
            Mathf.Clamp(value, MinPlayers, MaxPlayersCap);
    }
}
