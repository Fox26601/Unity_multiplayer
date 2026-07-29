using Fusion;
using UnityEngine;

namespace FusionMultiplayer.Player
{
    /// <summary>First-person mouse look: yaw on body (networked), pitch on PlayerCamera (local).</summary>
    [DefaultExecutionOrder(-20)]
    public sealed class PlayerLook : NetworkBehaviour
    {
        [SerializeField] private float _sensitivity = 0.15f;
        [SerializeField] private float _minPitch = -89f;
        [SerializeField] private float _maxPitch = 89f;

        private static PlayerLook _localInputAuthority;

        private Transform _cameraPivot;
        private PlayerAvatar _avatar;
        private float _pitch;
        private float _yaw;
        private BotBrain _botBrain;

        /// <summary>Absolute look used by <see cref="GameplayInput.Sample"/> for the local IA player.</summary>
        public static bool TryGetLocalLook(out float yaw, out float pitch)
        {
            if (_localInputAuthority == null)
            {
                yaw = 0f;
                pitch = 0f;
                return false;
            }

            yaw = _localInputAuthority._yaw;
            pitch = _localInputAuthority._pitch;
            return true;
        }

        public float Yaw => _yaw;
        public float Pitch => _pitch;

        private void Awake()
        {
            _cameraPivot = transform.Find("PlayerCamera");
            _avatar = GetComponent<PlayerAvatar>();
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

            enabled = true;
            if (_avatar == null)
                _avatar = GetComponent<PlayerAvatar>();

            _yaw = transform.eulerAngles.y;
            _pitch = _cameraPivot != null ? _cameraPivot.localEulerAngles.x : 0f;
            if (_pitch > 180f) _pitch -= 360f;

            if (HasInputAuthority)
            {
                _localInputAuthority = this;
                GameplayInputMode.Changed += OnInputModeChanged;
            }

            ApplyCameraPitchOnly();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (HasInputAuthority)
                GameplayInputMode.Changed -= OnInputModeChanged;

            if (_localInputAuthority == this)
                _localInputAuthority = null;

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
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            ApplyCameraPitchOnly();
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
            if (_avatar != null && !_avatar.IsAlive)
                return;

            if (IsBotDriven)
            {
                if (HasStateAuthority)
                    transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
                return;
            }

            // Local IA (host or client prediction): body yaw from live look.
            if (HasInputAuthority)
            {
                transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
                return;
            }

            if (!HasStateAuthority)
                return;

            if (!GetInput(out GameplayNetworkInput input))
                return;

            _yaw = input.LookYaw;
            _pitch = Mathf.Clamp(input.LookPitch, _minPitch, _maxPitch);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        public override void Render()
        {
            if (IsBotDriven)
            {
                if (HasStateAuthority)
                    ApplyCameraPitchOnly();
                return;
            }

            if (HasInputAuthority)
            {
                if (_avatar == null || _avatar.IsAlive)
                {
                    if (GameplayInputMode.IsGameplay && GameplayInput.TryReadLocalLookDelta(out var delta))
                        ApplyLookDelta(delta.x, delta.y);
                }

                transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
                ApplyCameraPitchOnly();
                return;
            }

            if (HasStateAuthority)
                ApplyCameraPitchOnly();
        }

        private void ApplyLookDelta(float deltaX, float deltaY)
        {
            _yaw += deltaX * _sensitivity;
            _pitch -= deltaY * _sensitivity;
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
        }

        private void ApplyCameraPitchOnly()
        {
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
