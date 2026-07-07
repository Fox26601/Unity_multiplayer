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

            // Edit-mode builds leave MenuUiVersion on Panel; main editor then skips play-mode rebuild.
            if (!Application.isPlaying)
                return;

            if (GetComponent<MainMenuUI>() != null)
                MainMenuRuntimeRebuild.EnsureBuilt(transform);
            else if (GetComponent<LobbyUI>() != null)
                LobbyRuntimeRebuild.EnsureBuilt(transform);
            else if (GetComponent<CharacterSelectUI>() != null || GetComponent<ChatUI>() != null)
                GameUiRuntimeRebuild.EnsureBuilt(transform);
            else if (GetComponent<GameOverUI>() != null)
            {
                EnsureCombatHudComponents();
                UiSceneRepair.RepairCanvas(transform);
            }
            else
                UiSceneRepair.RepairCanvas(transform);
        }

        private void EnsureCombatHudComponents()
        {
            if (GetComponent<CombatHealthHud>() == null)
                gameObject.AddComponent<CombatHealthHud>();
            if (GetComponent<CrosshairHud>() == null)
                gameObject.AddComponent<CrosshairHud>();
        }
    }
}
