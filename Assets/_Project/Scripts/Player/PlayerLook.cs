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
        private BotBrain _botBrain;

        private void Awake()
        {
            _cameraPivot = transform.Find("PlayerCamera");
            _yaw = transform.eulerAngles.y;
            _botBrain = GetComponent<BotBrain>();
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

        /// <summary>Server bot aim: updates yaw/pitch toward a world point and applies rotation.</summary>
        public void ApplyBotAim(Vector3 worldPoint)
        {
            if (!HasStateAuthority)
                return;

            var eye = _cameraPivot != null ? _cameraPivot.position : transform.position + Vector3.up * 1.5f;
            var to = worldPoint - eye;
            if (to.sqrMagnitude < 0.0001f)
                return;

            var flat = new Vector3(to.x, 0f, to.z);
            if (flat.sqrMagnitude > 0.0001f)
                _yaw = Quaternion.LookRotation(flat.normalized, Vector3.up).eulerAngles.y;

            var pitch = -Mathf.Atan2(to.y, flat.magnitude) * Mathf.Rad2Deg;
            _pitch = Mathf.Clamp(pitch, _minPitch, _maxPitch);
            ApplyLookRotation();
        }

        public bool IsBotDriven
        {
            get
            {
                if (_botBrain == null)
                    _botBrain = GetComponent<BotBrain>();
                return _botBrain != null && _botBrain.IsActive;
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            var avatar = GetComponent<PlayerAvatar>();
            if (avatar != null && !avatar.IsAlive)
                return;

            if (IsBotDriven)
            {
                transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
                return;
            }

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
            if (IsBotDriven)
            {
                if (HasStateAuthority)
                    ApplyLookRotation();
                return;
            }

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
