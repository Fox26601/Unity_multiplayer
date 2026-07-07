using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace FusionMultiplayer.Player
{
    /// <summary>Re-locks the cursor on left-click in the Game view (Minecraft-style after alt-tab).</summary>
    public sealed class GameplayCursorController : MonoBehaviour
    {
        private void Update()
        {
            if (!GameplayInputMode.IsGameplay || GameplayInputMode.ChatBlockingGameplay)
                return;

            if (Cursor.lockState == CursorLockMode.Locked)
                return;

            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            GameplayInputMode.RefreshCursor();
        }
    }
}
