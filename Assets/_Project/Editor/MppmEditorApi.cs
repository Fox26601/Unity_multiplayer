#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Reflection helpers for Unity Multiplayer Play Mode editor APIs.</summary>
internal static class MppmEditorApi
{
    public static bool IsCloneEditor => EditorAssetGuard.IsMultiplayerPlayModeClone();

    public static bool IsMainEditor => !IsCloneEditor;

    /// <summary>True when Library/VP exists (MPPM has been used in this project).</summary>
    public static bool HasVirtualProjectCache()
    {
        var vpPath = Path.Combine(GetProjectRoot(), "Library", "VP");
        return Directory.Exists(vpPath);
    }

    public static string GetProjectRoot() =>
        Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;

    /// <summary>Drain imports on the main editor before virtual players boot.</summary>
    public static void FlushAssetPipelineBeforePlay()
    {
        if (IsCloneEditor || EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }
}
#endif
