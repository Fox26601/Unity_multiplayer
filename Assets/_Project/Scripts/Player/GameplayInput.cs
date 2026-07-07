using UnityEngine;
using UnityEngine.InputSystem;

namespace FusionMultiplayer.Player
{
    /// <summary>Keyboard and mouse sampling for Fusion OnInput (WASD, look, E / Q).</summary>
    internal static class GameplayInput
    {
        private static bool _placeEdgeQueued;
        private static bool _removeEdgeQueued;
        private static bool _fireEdgeQueued;
        private static Vector2 _lookAccumulator;

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

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState == CursorLockMode.Locked)
                _fireEdgeQueued = true;
        }

        /// <summary>Accumulate mouse look delta between Fusion ticks (~32 Hz).</summary>
        public static void AccumulateLook()
        {
            if (!GameplayInputMode.IsGameplay)
                return;

            if (Cursor.lockState != CursorLockMode.Locked)
                return;

            var mouse = Mouse.current;
            if (mouse == null)
                return;

            _lookAccumulator += mouse.delta.ReadValue();
        }

        /// <summary>Per-frame mouse delta for local Render (not drained into network input).</summary>
        public static bool TryReadLocalLookDelta(out Vector2 delta)
        {
            delta = default;
            if (!GameplayInputMode.IsGameplay || Cursor.lockState != CursorLockMode.Locked)
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
            if (mouse != null && Cursor.lockState == CursorLockMode.Locked &&
                (mouse.leftButton.isPressed || _fireEdgeQueued))
            {
                data.Buttons.Set(GameplayButton.Fire, true);
                _fireEdgeQueued = false;
            }

            data.LookDeltaX = _lookAccumulator.x;
            data.LookDeltaY = _lookAccumulator.y;
            _lookAccumulator = Vector2.zero;
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
