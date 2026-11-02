using Fusion;
using FusionMultiplayer.Core;
using FusionMultiplayer.Data;
using UnityEngine;

namespace FusionMultiplayer.Player
{
    /// <summary>LMB projectile weapon for Combat / Sandbox modes.</summary>
    public sealed class PlayerWeapon : NetworkBehaviour
    {
        private const float FireCooldownSeconds = 0.25f;
        private const float MinPitch = -89f;
        private const float MaxPitch = 89f;

        [SerializeField] private NetworkObject _projectilePrefab;
        [SerializeField] private float _muzzleForwardOffset = 0.65f;

        private Transform _cameraTransform;
        private NetworkButtons _previousButtons;
        [Networked] private TickTimer _fireCooldown { get; set; }

        /// <summary>Same composition as PlayerCamera: body yaw * local pitch.</summary>
        public static Quaternion AimRotation(float yaw, float pitch) =>
            Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(pitch, 0f, 0f);

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

            if (input.Buttons.WasPressed(_previousButtons, GameplayButton.Fire) &&
                _fireCooldown.ExpiredOrNotRunning(Runner))
            {
                var yaw = input.LookYaw;
                var pitch = input.LookPitch;
                if (PlayerLook.TryGetLocalLook(out var liveYaw, out var livePitch))
                {
                    yaw = liveYaw;
                    pitch = livePitch;
                }

                RpcFire(yaw, pitch);
            }

            _previousButtons = input.Buttons;
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcFire(float yaw, float pitch)
        {
            pitch = Mathf.Clamp(pitch, MinPitch, MaxPitch);
            ServerFire(yaw, pitch);
        }

        /// <summary>Server / bot fire path (StateAuthority only).</summary>
        public void ServerBotFire()
        {
            if (!HasStateAuthority)
                return;

            var look = GetComponent<PlayerLook>();
            var yaw = transform.eulerAngles.y;
            var pitch = 0f;
            if (look != null)
            {
                yaw = look.Yaw;
                pitch = look.Pitch;
            }

            ServerFire(yaw, pitch);
        }

        private void ServerFire(float yaw, float pitch)
        {
            var hasProjectile = NetworkSpawnFactory.Catalog?.Projectile != null || _projectilePrefab != null;
            if (!SessionRuntime.AllowsShoot || !hasProjectile || Runner == null || !Runner.IsRunning)
                return;

            var avatar = GetComponent<PlayerAvatar>();
            if (avatar != null && !avatar.IsAlive)
                return;

            if (!_fireCooldown.ExpiredOrNotRunning(Runner))
                return;

            pitch = Mathf.Clamp(pitch, MinPitch, MaxPitch);
            if (!TryGetMuzzlePose(yaw, pitch, out var position, out var rotation))
                return;

            var shooter = Object.InputAuthority;
            if (shooter == PlayerRef.None && avatar != null)
                shooter = PlayerOwnership.ResolveLogicalOwner(avatar);
            if (shooter == PlayerRef.None)
                shooter = Runner.LocalPlayer;

            // Shared Mode: spawn with shooter as IA so SA stays with the firing player (TRSP owner).
            NetworkRunner.OnBeforeSpawned before = (_, obj) =>
            {
                if (shooter == PlayerRef.None)
                    return;
                var projectile = obj.GetComponent<Projectile>();
                if (projectile != null)
                    projectile.ConfigureShooter(shooter);
            };

            var spawned = NetworkSpawnFactory.SpawnProjectile(Runner, position, rotation, shooter, before);
            if (spawned == null && _projectilePrefab != null)
                Runner.Spawn(_projectilePrefab, position, rotation, shooter, before);

            _fireCooldown = TickTimer.CreateFromSeconds(Runner, FireCooldownSeconds);
            GetComponent<PlayerAnimationSync>()?.PulseShoot();
        }

        private bool TryGetMuzzlePose(float yaw, float pitch, out Vector3 position, out Quaternion rotation)
        {
            rotation = AimRotation(yaw, pitch);
            var eye = transform.position + Vector3.up * 1.6f;
            if (_cameraTransform == null)
                _cameraTransform = transform.Find("PlayerCamera");
            if (_cameraTransform != null)
                eye = transform.position + Vector3.up * _cameraTransform.localPosition.y;

            position = eye + rotation * Vector3.forward * _muzzleForwardOffset;
            return true;
        }
    }
}
