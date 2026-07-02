using UnityEngine;
using UnityEngine.InputSystem;

namespace FusionMultiplayer.Player
{
    /// <summary>Keyboard and mouse sampling for Fusion OnInput (WASD, look, E / Q).</summary>
    internal static class GameplayInput
    {
        private static bool _placeEdgeQueued;
        private static bool _removeEdgeQueued;

        /// <summary>Call every Unity frame before Fusion polls <see cref="Sample"/>.</summary>
        public static void AccumulateKeyEdges()
        {
            if (!GameplayInputMode.IsGameplay)
                return;

            var kb = Keyboard.current;
            if (kb == null)
                return;

            if (kb.eKey.wasPressedThisFrame)
                _placeEdgeQueued = true;
            if (kb.qKey.wasPressedThisFrame)
                _removeEdgeQueued = true;
        }

        public static void Sample(out GameplayNetworkInput data)
        {
            AccumulateKeyEdges();
            data = default;
            if (!GameplayInputMode.IsGameplay)
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
            }

            var mouse = Mouse.current;
            if (mouse == null)
                return;

            var delta = mouse.delta.ReadValue();
            data.LookDeltaX = delta.x;
            data.LookDeltaY = delta.y;
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
