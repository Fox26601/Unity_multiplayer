using Fusion;
using FusionMultiplayer.Core;
using FusionMultiplayer.Environment;
using UnityEngine;

namespace FusionMultiplayer.Player
{
    /// <summary>Networked trigger projectile for Combat mode damage.</summary>
    [RequireComponent(typeof(SphereCollider))]
    public sealed class Projectile : NetworkBehaviour
    {
        private const float Speed = 22f;
        private const float LifetimeSeconds = 3f;
        private const float MaxHitDistance = 40f;
        private const float Damage = 25f;

        [Networked] public PlayerRef Shooter { get; private set; }

        private SphereCollider _collider;
        private Vector3 _velocity;
        [Networked] private TickTimer _lifetime { get; set; }
        private bool _resolved;
        private bool _spawned;
        private PlayerRef _shooterRef;

        private void Awake()
        {
            _collider = GetComponent<SphereCollider>();
            _collider.isTrigger = true;
        }

        public override void Spawned()
        {
            _spawned = true;
            _shooterRef = Object.InputAuthority;
            Shooter = _shooterRef;
            _velocity = transform.forward * Speed;
            _lifetime = TickTimer.CreateFromSeconds(Runner, LifetimeSeconds);
            IgnoreShooterCollisions();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _spawned = false;
            _resolved = false;
            _shooterRef = PlayerRef.None;
        }

        public override void FixedUpdateNetwork()
        {
            if (!_spawned || !HasStateAuthority)
                return;

            if (_lifetime.Expired(Runner))
            {
                Runner.Despawn(Object);
                return;
            }

            if (_resolved)
                return;

            var step = _velocity * Runner.DeltaTime;
            if (TrySweepHit(transform.position, step, out var sweepCollider))
            {
                TryResolveCollision(sweepCollider);
                return;
            }

            transform.position += step;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_spawned || !HasStateAuthority || _resolved || other == null)
                return;

            TryResolveCollision(other);
        }

        private bool TrySweepHit(Vector3 origin, Vector3 step, out Collider hitCollider)
        {
            hitCollider = null;
            if (step.sqrMagnitude < 0.000001f)
                return false;

            var radius = _collider != null ? _collider.radius * GetMaxAxis(transform.lossyScale) : 0.18f;
            if (Physics.SphereCast(origin, radius, step.normalized, out var hit, step.magnitude,
                    Physics.AllLayers, QueryTriggerInteraction.Collide))
            {
                hitCollider = hit.collider;
                return true;
            }

            return false;
        }

        private static float GetMaxAxis(Vector3 scale)
        {
            return Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        }

        private void TryResolveCollision(Collider other)
        {
            if (!_spawned || _resolved || other == null)
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

                var victimRef = victim.Object.InputAuthority;
                if (victimRef != PlayerRef.None && victimRef != _shooterRef)
                {
                    RegisterHit(victim, victimRef);
                    return;
                }
            }

            if (ProjectileHitSurface.TryGetBlockingSurface(other, out _))
            {
                DespawnResolved();
                return;
            }
        }

        private void RegisterHit(PlayerAvatar victim, PlayerRef victimRef)
        {
            if (_resolved)
                return;

            if (!ValidateHit(victim, victimRef))
                return;

            var hitOrigin = transform.position;
            victim.RpcRegisterHit(Damage, _shooterRef, hitOrigin);
            DespawnResolved();
        }

        private void DespawnResolved()
        {
            if (_resolved)
                return;

            _resolved = true;
            if (Runner != null && Runner.IsRunning)
                Runner.Despawn(Object);
        }

        private bool ValidateHit(PlayerAvatar victim, PlayerRef victimRef)
        {
            if (_shooterRef == PlayerRef.None || victimRef == PlayerRef.None || _shooterRef == victimRef)
                return false;

            var hitPos = transform.position;
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

                if (avatar.Object.InputAuthority != _shooterRef)
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
