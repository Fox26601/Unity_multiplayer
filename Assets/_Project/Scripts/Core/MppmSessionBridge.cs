using System;
using System.IO;
using UnityEngine;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// Shares the active room name between the main MPPM editor and virtual player clones via Temp files.
    /// </summary>
    public static class MppmSessionBridge
    {
        private const string FolderName = "FusionMultiplayerMppm";
        private const string SessionFileName = "session.room";

        public static void PublishRoom(string roomName)
        {
#if UNITY_EDITOR
            if (!MppmUtility.IsMainInstance || string.IsNullOrWhiteSpace(roomName))
                return;

            try
            {
                var path = GetSessionFilePath();
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, roomName.Trim());
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FusionMultiplayer] MPPM session publish failed: {ex.Message}");
            }
#endif
        }

        public static void ClearRoom()
        {
#if UNITY_EDITOR
            // Virtual MPPM clones must never wipe the main editor's published room name.
            if (MppmUtility.IsVirtualInstance)
                return;

            try
            {
                var path = GetSessionFilePath();
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FusionMultiplayer] MPPM session clear failed: {ex.Message}");
            }
#endif
        }

        public static bool TryReadRoom(out string roomName)
        {
            roomName = null;
#if UNITY_EDITOR
            try
            {
                var path = GetSessionFilePath();
                if (!File.Exists(path))
                    return false;

                roomName = File.ReadAllText(path).Trim();
                return !string.IsNullOrWhiteSpace(roomName);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FusionMultiplayer] MPPM session read failed: {ex.Message}");
                return false;
            }
#else
            return false;
#endif
        }

        private static string GetSessionFilePath()
        {
            var root = MppmUtility.GetMainProjectRoot();
            return Path.Combine(root, "Temp", FolderName, SessionFileName);
        }
    }
}
