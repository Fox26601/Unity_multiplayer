#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Runs in the default Editor assembly (no Fusion reference) so this file compiles
/// even before Photon Fusion is imported.
/// </summary>
[InitializeOnLoad]
internal static class FusionImportChecker
{
    /// <summary>Core types such as NetworkRunner live in Fusion.Runtime, not Fusion.Unity.</summary>
    private const string FusionRunnerInRuntime = "Fusion.NetworkRunner, Fusion.Runtime";

    private static string FusionRuntimeDllPath =>
        Path.Combine(Application.dataPath, "Photon", "Fusion", "Assemblies", "Fusion.Runtime.dll");

    private static string FusionUnityAsmdefPath =>
        Path.Combine(Application.dataPath, "Photon", "Fusion", "Runtime", "Fusion.Unity.asmdef");

    static FusionImportChecker()
    {
        if (IsFusionRuntimeLoaded())
            return;

        // Second pass: Fusion.Runtime may not be in the AppDomain yet during static ctor order.
        EditorApplication.delayCall += DeferredCheck;
    }

    private static void DeferredCheck()
    {
        EditorApplication.delayCall -= DeferredCheck;

        if (IsFusionRuntimeLoaded())
            return;

        var sdkOnDisk = File.Exists(FusionUnityAsmdefPath) || File.Exists(FusionRuntimeDllPath);
        if (sdkOnDisk)
        {
            Debug.LogWarning(
                "[FusionMultiplayer] Photon Fusion files are under Assets/Photon/Fusion, but Fusion.Runtime did not load. " +
                "Look for red compile errors in the Console (often under Assets/Photon). After fixing them, recompile; " +
                "or re-import the Fusion 2 .unitypackage from the Photon dashboard.");
            return;
        }

        var marker = Path.Combine(Application.dataPath, "IMPORT_PHOTON_FUSION_FIRST.txt");
        var hint = File.Exists(marker)
            ? "Read Assets/IMPORT_PHOTON_FUSION_FIRST.txt"
            : "Import the Photon Fusion 2 .unitypackage from the Photon dashboard.";

        Debug.LogError(
            "[FusionMultiplayer] Photon Fusion 2 is not imported — Fusion.Runtime is missing from the project. " +
            hint +
            " Until Fusion is imported, assemblies FusionMultiplayer.Runtime / Editor cannot compile.");
    }

    private static bool IsFusionRuntimeLoaded()
    {
        if (System.Type.GetType(FusionRunnerInRuntime) != null)
            return true;

        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm.GetName().Name != "Fusion.Runtime")
                continue;
            if (asm.GetType("Fusion.NetworkRunner") != null)
                return true;
        }

        return false;
    }
}
#endif
