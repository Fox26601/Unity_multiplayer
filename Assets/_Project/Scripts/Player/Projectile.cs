using Fusion;
using FusionMultiplayer.Core;
using FusionMultiplayer.Environment;
using UnityEngine;

namespace FusionMultiplayer.Player
{
    /// <summary>
    /// Combat projectile: SA advances networked position; Render applies it (no NetworkTransform fight).
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public sealed class Projectile : NetworkBehaviour
    {
        private const float Speed = 45f;
        private const float LifetimeSeconds = 5f;
        private const float MaxHitDistance = 40f;
        private const float Damage = 25f;

        private static readonly RaycastHit[] SweepHits = new RaycastHit[16];

        [Networked] public PlayerRef Shooter { get; private set; }
        [Networked] private Vector3 NetPosition { get; set; }
        [Networked] private TickTimer _lifetime { get; set; }

        private SphereCollider _collider;
        private Vector3 _velocity;
        private bool _resolved;
        private bool _spawned;
        private PlayerRef _shooterRef;

        /// <summary>
        /// Pre-spawn hook: only caches local shooter. Networked Shooter is applied in Spawned.
        /// </summary>
        public void ConfigureShooter(PlayerRef shooter)
        {
            _shooterRef = shooter;
        }

        private void Awake()
        {
            _collider = GetComponent<SphereCollider>();
            _collider.isTrigger = true;
        }

        public override void Spawned()
        {
            _resolved = false;
            if (_shooterRef != PlayerRef.None)
                Shooter = _shooterRef;
            else if (Shooter != PlayerRef.None)
                _shooterRef = Shooter;
            else
            {
                _shooterRef = Object.InputAuthority;
                Shooter = _shooterRef;
            }

            NetPosition = transform.position;
            _velocity = transform.forward * Speed;
            if (HasStateAuthority)
                _lifetime = TickTimer.CreateFromSeconds(Runner, LifetimeSeconds);

            _spawned = true;
            IgnoreShooterCollisions();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _spawned = false;
            _resolved = false;
            _shooterRef = PlayerRef.None;
        }

        private bool CanUseNetworkedState()
        {
            return _spawned && Object != null && Object.IsValid;
        }

        public override void FixedUpdateNetwork()
        {
            if (!CanUseNetworkedState() || !HasStateAuthority)
                return;

            if (_lifetime.Expired(Runner))
            {
                Runner.Despawn(Object);
                return;
            }

            if (_resolved)
            {
                if (Runner != null && Runner.IsRunning && Object.IsValid)
                    Runner.Despawn(Object);
                return;
            }

            var origin = NetPosition;
            var step = _velocity * Runner.DeltaTime;
            if (TrySweepHit(origin, step, out var sweepCollider))
            {
                TryResolveCollision(sweepCollider);
                if (_resolved || !CanUseNetworkedState())
                    return;
            }

            if (!CanUseNetworkedState())
                return;

            NetPosition = origin + step;
            transform.position = NetPosition;
            Physics.SyncTransforms();
        }

        public override void Render()
        {
            if (!CanUseNetworkedState())
                return;

            transform.position = NetPosition;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!CanUseNetworkedState() || !HasStateAuthority || _resolved || other == null)
                return;

            TryResolveCollision(other);
        }

        private bool TrySweepHit(Vector3 origin, Vector3 step, out Collider hitCollider)
        {
            hitCollider = null;
            if (step.sqrMagnitude < 0.000001f)
                return false;

            var radius = _collider != null ? _collider.radius * GetMaxAxis(transform.lossyScale) : 0.18f;
            var count = Physics.SphereCastNonAlloc(origin, radius, step.normalized, SweepHits, step.magnitude,
                Physics.AllLayers, QueryTriggerInteraction.Collide);

            var nearest = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                var col = SweepHits[i].collider;
                if (col == null || col == _collider || !IsRelevantSweepTarget(col))
                    continue;

                if (SweepHits[i].distance < nearest)
                {
                    nearest = SweepHits[i].distance;
                    hitCollider = col;
                }
            }

            return hitCollider != null;
        }

        /// <summary>Sweep must skip the shooter's own colliders and other projectiles.</summary>
        private bool IsRelevantSweepTarget(Collider col)
        {
            if (col.GetComponentInParent<Projectile>() != null)
                return false;

            var avatar = col.GetComponentInParent<PlayerAvatar>();
            if (avatar != null && avatar.Object != null && avatar.Object.IsValid &&
                PlayerOwnership.ResolveLogicalOwner(avatar) == _shooterRef)
                return false;

            return true;
        }

        private static float GetMaxAxis(Vector3 scale)
        {
            return Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        }

        private void TryResolveCollision(Collider other)
        {
            if (!CanUseNetworkedState() || _resolved || other == null)
                return;

            if (other.GetComponentInParent<Projectile>() != null)
                return;

            var victim = other.GetComponentInParent<PlayerAvatar>();
            if (victim != null && victim.Object != null && victim.Object.IsValid)
            {
                if (!victim.IsAlive)
                {
                    DespawnResolved();
                    return;
                }

                var victimRef = PlayerOwnership.ResolveLogicalOwner(victim);
                if (victimRef != PlayerRef.None && victimRef != _shooterRef)
                {
                    RegisterHit(victim, victimRef);
                    return;
                }

                // Contacted a player body but cannot award damage — still end the shot.
                DespawnResolved();
                return;
            }

            if (ProjectileHitSurface.TryGetBlockingSurface(other, out _))
            {
                DespawnResolved();
                return;
            }

            // Solid non-player collider that wasn't tagged — still stop the shot.
            if (!other.isTrigger && other.GetComponentInParent<PlayerAvatar>() == null)
                DespawnResolved();
        }

        private void RegisterHit(PlayerAvatar victim, PlayerRef victimRef)
        {
            if (_resolved)
                return;

            if (ValidateHit(victim, victimRef))
            {
                var hitOrigin = NetPosition;
                victim.RpcRegisterHit(Damage, _shooterRef, hitOrigin);
            }

            // Always despawn on intended target contact, even if damage validation fails.
            DespawnResolved();
        }

        private void DespawnResolved()
        {
            if (_resolved)
                return;

            _resolved = true;
            if (Runner != null && Runner.IsRunning && Object != null && Object.IsValid)
                Runner.Despawn(Object);
        }

        private bool ValidateHit(PlayerAvatar victim, PlayerRef victimRef)
        {
            if (_shooterRef == PlayerRef.None || victimRef == PlayerRef.None || _shooterRef == victimRef)
                return false;

            var hitPos = NetPosition;
            var victimPos = victim.transform.position;
            var planar = hitPos - victimPos;
            planar.y = 0f;
            return planar.sqrMagnitude <= MaxHitDistance * MaxHitDistance;
        }

        private void IgnoreShooterCollisions()
        {
            if (_collider == null || _shooterRef == PlayerRef.None)
                return;

            foreach (var avatar in FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
            {
                if (avatar.Object == null || !avatar.Object.IsValid)
                    continue;

                if (PlayerOwnership.ResolveLogicalOwner(avatar) != _shooterRef)
                    continue;

                foreach (var col in avatar.GetComponentsInChildren<Collider>(true))
                {
                    if (col != null && col != _collider)
                        Physics.IgnoreCollision(_collider, col, true);
                }

                return;
            }
        }
    }
}
