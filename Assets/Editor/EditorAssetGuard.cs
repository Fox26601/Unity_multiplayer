#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Guards editor scripts from modifying assets when Unity Asset Database is read-only
/// (Multiplayer Play Mode clones, Accelerator, etc.).
/// </summary>
internal static class EditorAssetGuard
{
    public static bool IsAssetDatabaseReadOnly()
    {
        var prop = typeof(AssetDatabase).GetProperty("isReadOnly", BindingFlags.Public | BindingFlags.Static)
                   ?? typeof(AssetDatabase).GetProperty("IsReadOnly", BindingFlags.Public | BindingFlags.Static);
        return prop?.GetValue(null) is bool readOnly && readOnly;
    }

    public static bool IsMultiplayerPlayModeClone()
    {
        if (Application.dataPath.IndexOf("/Library/VP/mppm", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        var type = System.Type.GetType(
            "Unity.Multiplayer.PlayMode.Editor.CurrentPlayer, Unity.Multiplayer.PlayMode.Editor");
        if (type == null)
            return false;

        var isMain = type.GetProperty("IsMainEditor", BindingFlags.Public | BindingFlags.Static);
        if (isMain?.GetValue(null) is bool mainEditor)
            return !mainEditor;

        return false;
    }

    public static bool CanModifyAssets(bool logIfBlocked = false)
    {
        if (Application.isBatchMode)
            return true;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            return false;

        if (IsMultiplayerPlayModeClone())
        {
            if (logIfBlocked)
                LogBlocked("Multiplayer Play Mode clone editors use a read-only Asset Database.");
            return false;
        }

        if (IsAssetDatabaseReadOnly())
        {
            if (logIfBlocked)
                LogBlocked("Asset Database is read-only.");
            return false;
        }

        return true;
    }

    private static void LogBlocked(string reason)
    {
        Debug.LogWarning(
            $"[FusionMultiplayer] Skipping asset modification — {reason} " +
            "Run this action in the main Editor, or use Tools → Fusion Multiplayer → Repair MPPM Cache.");
    }
}
#endif
