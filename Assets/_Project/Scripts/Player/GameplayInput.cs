using UnityEngine;
using UnityEngine.InputSystem;

namespace FusionMultiplayer.Player
{
    /// <summary>Keyboard and mouse sampling for Fusion OnInput (WASD, look, E / Q / Space).</summary>
    internal static class GameplayInput
    {
        private static bool _placeEdgeQueued;
        private static bool _removeEdgeQueued;
        private static bool _fireEdgeQueued;
        private static bool _jumpEdgeQueued;

        /// <summary>Call every Unity frame before Fusion polls <see cref="Sample"/>.</summary>
        public static void AccumulateKeyEdges()
        {
            if (!GameplayInputMode.IsGameplay || FusionMultiplayer.UI.PauseMenuUI.IsOpen)
                return;

            var kb = Keyboard.current;
            if (kb == null)
                return;

            if (kb.eKey.wasPressedThisFrame)
                _placeEdgeQueued = true;
            if (kb.qKey.wasPressedThisFrame)
                _removeEdgeQueued = true;
            if (kb.spaceKey.wasPressedThisFrame)
                _jumpEdgeQueued = true;

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState == CursorLockMode.Locked)
                _fireEdgeQueued = true;
        }

        /// <summary>Per-frame mouse delta for local Render (not drained into network input).</summary>
        public static bool TryReadLocalLookDelta(out Vector2 delta)
        {
            delta = default;
            if (!GameplayInputMode.IsGameplay || FusionMultiplayer.UI.PauseMenuUI.IsOpen ||
                Cursor.lockState != CursorLockMode.Locked)
                return false;

            var mouse = Mouse.current;
            if (mouse == null)
                return false;

            delta = mouse.delta.ReadValue();
            return delta.sqrMagnitude > 0f;
        }

        public static void Sample(out GameplayNetworkInput data)
        {
            AccumulateKeyEdges();
            data = default;

            // Always publish absolute look so pause/menus do not snap SA facing to 0.
            if (PlayerLook.TryGetLocalLook(out var yaw, out var pitch))
            {
                data.LookYaw = yaw;
                data.LookPitch = pitch;
            }

            if (!GameplayInputMode.IsGameplay || FusionMultiplayer.UI.PauseMenuUI.IsOpen)
                return;

            var kb = Keyboard.current;
            if (kb != null)
            {
                data.MoveForward = ReadVertical(kb);
                data.Strafe = ReadHorizontal(kb);

                if (kb.eKey.isPressed || _placeEdgeQueued)
                {
                    data.Buttons.Set(GameplayButton.Place, true);
                    _placeEdgeQueued = false;
                }

                if (kb.qKey.isPressed || _removeEdgeQueued)
                {
                    data.Buttons.Set(GameplayButton.Remove, true);
                    _removeEdgeQueued = false;
                }

                if (kb.spaceKey.isPressed || _jumpEdgeQueued)
                {
                    data.Buttons.Set(GameplayButton.Jump, true);
                    _jumpEdgeQueued = false;
                }
            }

            var mouse = Mouse.current;
            if (mouse != null && Cursor.lockState == CursorLockMode.Locked &&
                (mouse.leftButton.isPressed || _fireEdgeQueued))
            {
                data.Buttons.Set(GameplayButton.Fire, true);
                _fireEdgeQueued = false;
            }
        }

        private static float ReadHorizontal(Keyboard kb)
        {
            var value = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) value -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) value += 1f;
            return Mathf.Clamp(value, -1f, 1f);
        }

        private static float ReadVertical(Keyboard kb)
        {
            var value = 0f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) value += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) value -= 1f;
            return Mathf.Clamp(value, -1f, 1f);
        }
    }
}
