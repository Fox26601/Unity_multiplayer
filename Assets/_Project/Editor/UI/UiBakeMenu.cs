#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FusionMultiplayer.EditorTools.UI
{
    /// <summary>Regenerates scene UI via the existing generator menu.</summary>
    public static class UiBakeMenu
    {
        private const string MenuPath = "Tools/Fusion Multiplayer/Bake UI To Scenes (Use Generator)";

        [MenuItem(MenuPath)]
        public static void BakeUiViaGenerator()
        {
            if (!EditorUtility.DisplayDialog(
                    "Bake UI",
                    "Run the scene generator and UI layout fix for Main Menu, Lobby, and map HUD?",
                    "Run",
                    "Cancel"))
                return;

            EditorApplication.ExecuteMenuItem("Tools/Fusion Multiplayer/Generate Scenes, Prefabs, And Build Settings");
            EditorApplication.ExecuteMenuItem("Tools/Fusion Multiplayer/Fix UI Layout In Scenes");
            Debug.Log("[FusionMultiplayer] UI bake: generator + layout fix.");
        }
    }
}
#endif
