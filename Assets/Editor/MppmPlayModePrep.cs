#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Prepares the main editor before MPPM Play so virtual-player clones are less likely to desync.
/// Ensures MPPM virtual players start from the main menu scene.
/// </summary>
[InitializeOnLoad]
internal static class MppmPlayModePrep
{
    private const string MainMenuScenePath = "Assets/_Project/Scenes/00_MainMenu.unity";

    static MppmPlayModePrep()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EnsurePlayModeStartScene();
    }

    private static void EnsurePlayModeStartScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
        if (scene == null)
            return;

        if (EditorSceneManager.playModeStartScene == null)
        {
            EditorSceneManager.playModeStartScene = scene;
            Debug.Log("[FusionMultiplayer] Play Mode Start Scene auto-set to 00_MainMenu (MPPM-safe).");
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            EnsurePlayModeStartScene();

            if (MppmEditorApi.IsMainEditor && MppmEditorApi.HasVirtualProjectCache())
                MppmEditorApi.FlushAssetPipelineBeforePlay();
        }
    }
}
#endif
