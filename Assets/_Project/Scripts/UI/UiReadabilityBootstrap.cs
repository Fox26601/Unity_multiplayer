using UnityEngine;

namespace FusionMultiplayer.UI
{
    /// <summary>
    /// Rebuilds broken main-menu UI before other scripts run.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class UiReadabilityBootstrap : MonoBehaviour
    {
        private void OnEnable()
        {
            UiCanvasFix.EnsureReadableCanvas(transform);

            if (GetComponent<MainMenuUI>() != null)
                MainMenuRuntimeRebuild.EnsureBuilt(transform);
            else if (GetComponent<LobbyUI>() != null)
                LobbyRuntimeRebuild.EnsureBuilt(transform);
            else if (GetComponent<CharacterSelectUI>() != null || GetComponent<ChatUI>() != null)
                GameUiRuntimeRebuild.EnsureBuilt(transform);
            else
                UiSceneRepair.RepairCanvas(transform);
        }
    }
}
