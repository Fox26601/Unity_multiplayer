using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace FusionMultiplayer.Core
{
    /// <summary>Helpers for Unity Multiplayer Play Mode (MPPM) virtual player instances.</summary>
    public static class MppmUtility
    {
        public static bool IsAvailable =>
#if UNITY_EDITOR
            Application.dataPath.IndexOf("/Library/VP/mppm", StringComparison.OrdinalIgnoreCase) >= 0
            || GetMppmType() != null;
#else
            false;
#endif

        public static bool IsVirtualInstance
        {
            get
            {
#if UNITY_EDITOR
                if (Application.dataPath.IndexOf("/Library/VP/mppm", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                var type = GetMppmType();
                if (type == null)
                    return false;

                var isMain = type.GetProperty("IsMainEditor", BindingFlags.Public | BindingFlags.Static);
                if (isMain?.GetValue(null) is bool mainEditor)
                    return !mainEditor;
#endif
                return false;
            }
        }

        public static bool IsMainInstance => IsAvailable && !IsVirtualInstance;

        /// <summary>Player tag from MPPM (e.g. "Player 2") or a generated fallback.</summary>
        public static string GetVirtualPlayerLabel()
        {
#if UNITY_EDITOR
            var type = GetMppmType();
            if (type != null)
            {
                foreach (var propName in new[] { "Tag", "PlayerTag", "Name" })
                {
                    var prop = type.GetProperty(propName, BindingFlags.Public | BindingFlags.Static);
                    if (prop?.GetValue(null) is string tag && !string.IsNullOrWhiteSpace(tag))
                        return tag.Trim();
                }

                foreach (var propName in new[] { "PlayerIndex", "Index" })
                {
                    var prop = type.GetProperty(propName, BindingFlags.Public | BindingFlags.Static);
                    if (prop?.GetValue(null) is int index && index > 0)
                        return $"Player {index + 1}";
                }
            }

            if (IsVirtualInstance)
                return "Player 2";
#endif
            return "Player";
        }

        public static string GetMainProjectRoot()
        {
            var dataPath = Application.dataPath;
            var mppmIdx = dataPath.IndexOf("/Library/VP/mppm", StringComparison.OrdinalIgnoreCase);
            if (mppmIdx >= 0)
                return dataPath.Substring(0, mppmIdx);

            return Directory.GetParent(dataPath)?.FullName ?? dataPath;
        }

#if UNITY_EDITOR
        private static Type GetMppmType() =>
            Type.GetType("Unity.Multiplayer.PlayMode.Editor.CurrentPlayer, Unity.Multiplayer.PlayMode.Editor");
#endif
    }
}
