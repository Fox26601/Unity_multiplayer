using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FusionMultiplayer.Player
{
    /// <summary>First-person mouse look: yaw on body (networked), pitch on PlayerCamera (local).</summary>
    public sealed class PlayerLook : NetworkBehaviour
    {
        [SerializeField] private float _sensitivity = 0.15f;
        [SerializeField] private float _minPitch = -89f;
        [SerializeField] private float _maxPitch = 89f;

        private Transform _cameraPivot;
        private float _pitch;
        private float _yaw;

        private void Awake()
        {
            _cameraPivot = transform.Find("PlayerCamera");
            _yaw = transform.eulerAngles.y;
        }

        public override void Spawned()
        {
            if (!HasInputAuthority && !HasStateAuthority)
            {
                enabled = false;
                return;
            }

            _yaw = transform.eulerAngles.y;
            _pitch = _cameraPivot != null ? _cameraPivot.localEulerAngles.x : 0f;
            if (_pitch > 180f) _pitch -= 360f;

            if (HasInputAuthority)
                GameplayInputMode.Changed += OnInputModeChanged;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (HasInputAuthority)
                GameplayInputMode.Changed -= OnInputModeChanged;

            base.Despawned(runner, hasState);
        }

        public override void FixedUpdateNetwork()
        {
            // Local player applies look in LateUpdate; authority simulates remote peers only.
            if (!HasStateAuthority || HasInputAuthority)
                return;

            if (!GetInput(out GameplayNetworkInput input))
                return;

            if (Mathf.Approximately(input.LookDeltaX, 0f))
                return;

            _yaw += input.LookDeltaX * _sensitivity;
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        private void LateUpdate()
        {
            if (!HasInputAuthority || !GameplayInputMode.IsGameplay)
                return;

            var mouse = Mouse.current;
            if (mouse == null)
                return;

            var delta = mouse.delta.ReadValue();
            if (delta.sqrMagnitude < 0.0001f)
                return;

            _yaw += delta.x * _sensitivity;
            _pitch -= delta.y * _sensitivity;
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

            ApplyLookRotation();
        }

        public override void Render()
        {
            if (!HasInputAuthority)
                return;

            ApplyLookRotation();
        }

        private void ApplyLookRotation()
        {
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (_cameraPivot != null)
                _cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!HasInputAuthority || !hasFocus)
                return;

            GameplayInputMode.RefreshCursor();
        }

        private void OnInputModeChanged(GameplayInputModeKind mode)
        {
            if (mode == GameplayInputModeKind.Gameplay)
                GameplayInputMode.RefreshCursor();
        }
    }
}
