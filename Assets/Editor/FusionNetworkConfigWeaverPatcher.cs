#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// After Photon Fusion is imported, ensures FusionMultiplayer.Runtime is listed in
/// NetworkProjectConfig AssembliesToWeave (required for custom asmdefs). Pure file edit — no Fusion editor API.
/// Does not force AssetDatabase.ImportAsset (avoids MPPM clone refresh errors).
/// </summary>
internal static class FusionNetworkConfigWeaverPatcher
{
    private const string ConfigRelative = "Photon/Fusion/Resources/NetworkProjectConfig.fusion";
    private const string AssemblyName = "FusionMultiplayer.Runtime";

    [InitializeOnLoadMethod]
    private static void OnEditorLoaded()
    {
        EditorApplication.delayCall += TryPatch;
    }

    internal static void TryPatch()
    {
        if (!EditorAssetGuard.CanModifyAssets())
            return;

        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying)
            return;

        var fullPath = Path.Combine(Application.dataPath, ConfigRelative);
        if (!File.Exists(fullPath))
            return;

        var text = File.ReadAllText(fullPath);
        if (text.IndexOf("\"" + AssemblyName + "\"", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return;

        var marker = "\"AssembliesToWeave\"";
        var idx = text.IndexOf(marker, System.StringComparison.Ordinal);
        if (idx < 0)
        {
            Debug.LogWarning(
                $"[FusionMultiplayer] Found NetworkProjectConfig.fusion but no {marker} key. Add '{AssemblyName}' manually under Assemblies To Weave, then Apply.");
            return;
        }

        var openBracket = text.IndexOf('[', idx);
        if (openBracket < 0) return;

        var insert = $"\n    \"{AssemblyName}\",";
        var patched = text.Insert(openBracket + 1, insert);
        File.WriteAllText(fullPath, patched);
        Debug.Log(
            $"[FusionMultiplayer] Injected '{AssemblyName}' into NetworkProjectConfig AssembliesToWeave. " +
            "Unity will reimport NetworkProjectConfig.fusion. Open Fusion → Network Project Config and click Apply if needed.");
    }
}
#endif
