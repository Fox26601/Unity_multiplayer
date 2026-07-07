using Fusion;
using FusionMultiplayer.Core;
using UnityEngine;

namespace FusionMultiplayer.Player
{
    /// <summary>LMB projectile weapon for Combat / Sandbox modes.</summary>
    public sealed class PlayerWeapon : NetworkBehaviour
    {
        private const float FireCooldownSeconds = 0.25f;

        [SerializeField] private NetworkObject _projectilePrefab;
        [SerializeField] private float _muzzleForwardOffset = 0.65f;

        private Transform _cameraTransform;
        private NetworkButtons _previousButtons;
        [Networked] private TickTimer _fireCooldown { get; set; }

        private void Awake()
        {
            _cameraTransform = transform.Find("PlayerCamera");
        }

        public override void Spawned()
        {
            _previousButtons = default;
        }

        public override void FixedUpdateNetwork()
        {
            SessionRuntime.Refresh(Runner);

            if (!GetInput(out GameplayNetworkInput input) || !HasInputAuthority)
                return;

            var avatar = GetComponent<PlayerAvatar>();
            if (avatar != null && !avatar.IsAlive)
            {
                _previousButtons = input.Buttons;
                return;
            }

            if (!SessionRuntime.AllowsShoot)
            {
                _previousButtons = input.Buttons;
                return;
            }

            if (input.Buttons.WasPressed(_previousButtons, GameplayButton.Fire) && _fireCooldown.ExpiredOrNotRunning(Runner))
                RpcFire();

            _previousButtons = input.Buttons;
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcFire()
        {
            if (!SessionRuntime.AllowsShoot || _projectilePrefab == null || Runner == null || !Runner.IsRunning)
                return;

            var avatar = GetComponent<PlayerAvatar>();
            if (avatar != null && !avatar.IsAlive)
                return;

            if (!_fireCooldown.ExpiredOrNotRunning(Runner))
                return;

            if (!TryGetMuzzlePose(out var position, out var rotation))
                return;

            Runner.Spawn(_projectilePrefab, position, rotation, Object.InputAuthority);
            _fireCooldown = TickTimer.CreateFromSeconds(Runner, FireCooldownSeconds);
        }

        private bool TryGetMuzzlePose(out Vector3 position, out Quaternion rotation)
        {
            position = default;
            rotation = transform.rotation;

            var camTr = _cameraTransform;
            if (camTr == null)
                camTr = transform.Find("PlayerCamera");

            if (camTr != null)
            {
                rotation = camTr.rotation;
                position = camTr.position + camTr.forward * _muzzleForwardOffset;
                return true;
            }

            position = transform.position + Vector3.up * 1.6f + transform.forward * _muzzleForwardOffset;
            return true;
        }
    }
}
