using Fusion;
using UnityEngine;

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
            if (!HasStateAuthority)
                return;

            var avatar = GetComponent<PlayerAvatar>();
            if (avatar != null && !avatar.IsAlive)
                return;

            if (HasInputAuthority)
            {
                transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
                return;
            }

            if (!GameplayInputMode.IsGameplay)
                return;

            if (!GetInput(out GameplayNetworkInput input))
                return;

            if (!Mathf.Approximately(input.LookDeltaX, 0f) || !Mathf.Approximately(input.LookDeltaY, 0f))
                ApplyLookDelta(input.LookDeltaX, input.LookDeltaY);

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        public override void Render()
        {
            if (HasInputAuthority)
            {
                var avatar = GetComponent<PlayerAvatar>();
                if (avatar == null || avatar.IsAlive)
                {
                    if (GameplayInputMode.IsGameplay && GameplayInput.TryReadLocalLookDelta(out var delta))
                        ApplyLookDelta(delta.x, delta.y);
                }

                ApplyLookRotation();
                return;
            }

            if (HasStateAuthority)
                ApplyLookRotation();
        }

        private void ApplyLookDelta(float deltaX, float deltaY)
        {
            _yaw += deltaX * _sensitivity;
            _pitch -= deltaY * _sensitivity;
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
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
