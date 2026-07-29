using UnityEngine;

namespace FusionMultiplayer.Core
{
    /// <summary>Persists session identity so a crashed client can rejoin with full control.</summary>
    public static class SessionReconnectStore
    {
        private const string PrefRoom = "FusionMultiplayer.Reconnect.Room";
        private const string PrefToken = "FusionMultiplayer.Reconnect.Token";
        private const string PrefMode = "FusionMultiplayer.Reconnect.Mode";
        private const string PrefActive = "FusionMultiplayer.Reconnect.Active";
        private const string PrefHidden = "FusionMultiplayer.Reconnect.Hidden";

        public static void Save(string roomName, string token, SessionCatalog.GameModeKind mode, bool hidden = false)
        {
            if (string.IsNullOrWhiteSpace(roomName) || string.IsNullOrWhiteSpace(token))
                return;

            PlayerPrefs.SetString(PrefRoom, roomName.Trim());
            PlayerPrefs.SetString(PrefToken, token);
            PlayerPrefs.SetInt(PrefMode, (int)mode);
            PlayerPrefs.SetInt(PrefActive, 1);
            PlayerPrefs.SetInt(PrefHidden, hidden ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(PrefRoom);
            PlayerPrefs.DeleteKey(PrefToken);
            PlayerPrefs.DeleteKey(PrefMode);
            PlayerPrefs.DeleteKey(PrefActive);
            PlayerPrefs.DeleteKey(PrefHidden);
            PlayerPrefs.Save();
        }

        public static bool TryLoad(
            out string roomName,
            out string token,
            out SessionCatalog.GameModeKind mode,
            out bool hidden)
        {
            roomName = PlayerPrefs.GetString(PrefRoom, string.Empty);
            token = PlayerPrefs.GetString(PrefToken, string.Empty);
            mode = (SessionCatalog.GameModeKind)PlayerPrefs.GetInt(PrefMode, 0);
            hidden = PlayerPrefs.GetInt(PrefHidden, 0) == 1;
            var active = PlayerPrefs.GetInt(PrefActive, 0) == 1;
            return active && !string.IsNullOrWhiteSpace(roomName) && !string.IsNullOrWhiteSpace(token);
        }

        public static bool TryLoad(out string roomName, out string token, out SessionCatalog.GameModeKind mode)
        {
            return TryLoad(out roomName, out token, out mode, out _);
        }
    }
}
