#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Ensures TMP Essential Resources are present and TMP Settings version matches the package.
/// Does not auto-modify assets on editor load (avoids read-only Asset Database errors).
/// </summary>
internal static class TmpEssentialsSetup
{
    private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
    private const string TmpFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string CurrentTmpAssetVersion = "2";

    [MenuItem("Tools/Fusion Multiplayer/Import TMP Essential Resources")]
    public static void ImportTmpEssentialsMenu()
    {
        ImportTmpEssentials();
    }

    /// <summary>Unity batchmode entry point: -executeMethod TmpEssentialsSetup.RunImportAndFix</summary>
    public static void RunImportAndFix()
    {
        ImportTmpEssentials();
        TryFixTmpSettingsVersion(force: true);
        AssetDatabase.Refresh();
    }

    private static void ImportTmpEssentials()
    {
        if (!CanModifyAssets())
        {
            Debug.LogWarning(
                "[FusionMultiplayer] Asset Database is read-only — cannot import TMP Essential Resources. " +
                "Run this menu item from the Unity Editor with a writable project.");
            return;
        }

        TMP_PackageResourceImporter.ImportResources(importEssentials: true, importExamples: false, interactive: false);
        Debug.Log("[FusionMultiplayer] Importing TMP Essential Resources (silent). Watch Console for completion.");
    }

    [MenuItem("Tools/Fusion Multiplayer/Fix TMP Settings Version")]
    private static void FixTmpSettingsVersionMenu()
    {
        if (TryFixTmpSettingsVersion(force: true))
            Debug.Log("[FusionMultiplayer] TMP Settings version updated.");
        else
            Debug.LogWarning("[FusionMultiplayer] Could not fix TMP Settings. Run Import TMP Essential Resources.");
    }

    internal static bool TmpResourcesPresentOnDisk()
    {
        return File.Exists(TmpSettingsPath) && File.Exists(TmpFontPath);
    }

    internal static bool TmpSettingsVersionNeedsFix()
    {
        if (!TmpResourcesPresentOnDisk())
            return false;

        var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
        if (settings == null)
            return true;

        var serialized = new SerializedObject(settings);
        var versionProp = serialized.FindProperty("assetVersion");
        return versionProp == null || versionProp.stringValue != CurrentTmpAssetVersion;
    }

    internal static bool TryFixTmpSettingsVersion(bool force = false)
    {
        if (!TmpResourcesPresentOnDisk())
            return false;

        if (!force && !TmpSettingsVersionNeedsFix())
            return true;

        if (!CanModifyAssets())
        {
            Debug.LogWarning(
                "[FusionMultiplayer] Asset Database is read-only — skipping TMP Settings fix. " +
                "Use Tools → Fusion Multiplayer → Fix TMP Settings Version when the project is writable.");
            return false;
        }

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            return false;

        var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
        if (settings == null)
            return false;

        var serialized = new SerializedObject(settings);
        var versionProp = serialized.FindProperty("assetVersion");
        if (versionProp == null)
            return false;

        if (versionProp.stringValue == CurrentTmpAssetVersion)
            return true;

        versionProp.stringValue = CurrentTmpAssetVersion;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        return true;
    }

    private static bool CanModifyAssets()
    {
        return EditorAssetGuard.CanModifyAssets();
    }
}
#endif
