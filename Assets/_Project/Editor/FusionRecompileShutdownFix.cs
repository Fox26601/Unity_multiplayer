#if UNITY_EDITOR
using Fusion;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

/// <summary>
/// Exits Play Mode before script recompile so Fusion shuts down cleanly.
/// </summary>
[InitializeOnLoad]
internal static class FusionRecompileShutdownFix
{
    static FusionRecompileShutdownFix()
    {
        AssemblyReloadEvents.beforeAssemblyReload += ExitPlayIfNeeded;
        CompilationPipeline.compilationStarted += _ => ExitPlayIfNeeded();
    }

    private static void ExitPlayIfNeeded()
    {
        if (!EditorApplication.isPlaying)
            return;

        EditorApplication.ExitPlaymode();
    }
}
#endif
