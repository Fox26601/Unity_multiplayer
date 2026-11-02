using Fusion;
using FusionMultiplayer.Core;
using FusionMultiplayer.Environment;
using UnityEngine;

namespace FusionMultiplayer.Player
{
    /// <summary>
    /// Combat projectile: shooter StateAuthority simulates locally; NetworkTransform (TRSP) replicates pose.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(NetworkTransform))]
    public sealed class Projectile : NetworkBehaviour
    {
        private const float Speed = 45f;
        private const float LifetimeSeconds = 5f;
        private const float MaxTravelDistance = 80f;
        private const float MaxHitDistance = 40f;
        private const float Damage = 25f;

        private static readonly RaycastHit[] SweepHits = new RaycastHit[16];
        private static readonly Collider[] OverlapHits = new Collider[16];

        [Networked] public PlayerRef Shooter { get; private set; }
        [Networked] private TickTimer _lifetime { get; set; }
        [Networked] private NetworkBool Spent { get; set; }

        private SphereCollider _collider;
        private bool _spawned;
        private bool _spentLocal;
        private PlayerRef _shooterRef;
        private Vector3 _velocity;
        private Vector3 _spawnOrigin;

        public void ConfigureShooter(PlayerRef shooter)
        {
            _shooterRef = shooter;
        }

        private void Awake()
        {
            _collider = GetComponent<SphereCollider>();
            _collider.isTrigger = true;

            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        public override void Spawned()
        {
            _spentLocal = false;

            if (HasStateAuthority)
            {
                if (_shooterRef != PlayerRef.None)
                    Shooter = _shooterRef;

                var forward = transform.forward;
                if (forward.sqrMagnitude < 0.0001f)
                    forward = Vector3.forward;

                _spawnOrigin = transform.position;
                _velocity = forward.normalized * Speed;
                Spent = false;
                _lifetime = TickTimer.CreateFromSeconds(Runner, LifetimeSeconds);
            }
            else
            {
                _shooterRef = Shooter;
            }

            _spawned = true;
            IgnoreShooterCollisions();

            if (HasStateAuthority)
                ResolveOverlapsAt(transform.position);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _spawned = false;
            _spentLocal = true;
            _shooterRef = PlayerRef.None;
            _velocity = Vector3.zero;
        }

        private bool CanUseNetworkedState()
        {
            return _spawned && Object != null && Object.IsValid && Runner != null && Runner.IsRunning;
        }

        private bool IsSpentSafe()
        {
            if (_spentLocal)
                return true;
            if (!CanUseNetworkedState())
                return true;
            return Spent;
        }

        public override void FixedUpdateNetwork()
        {
            if (!CanUseNetworkedState() || !HasStateAuthority)
                return;

            if (IsSpentSafe())
            {
                TryDespawn();
                return;
            }

            if (ShouldExpire())
            {
                EndShot();
                return;
            }

            var origin = transform.position;

            if (ResolveOverlapsAt(origin))
                return;

            var step = _velocity * Runner.DeltaTime;
            if (TrySweepHit(origin, step, out var sweepCollider))
            {
                TryResolveCollision(sweepCollider);
                if (!CanUseNetworkedState() || IsSpentSafe())
                    return;

                if (sweepCollider != null && !sweepCollider.isTrigger)
                {
                    EndShot();
                    return;
                }
            }

            if (!CanUseNetworkedState() || IsSpentSafe())
                return;

            transform.position = origin + step;

            if ((transform.position - _spawnOrigin).sqrMagnitude >= MaxTravelDistance * MaxTravelDistance)
                EndShot();
        }

        public override void Render()
        {
            // NetworkTransform drives proxy visuals; no custom networked pose reads here.
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!CanUseNetworkedState() || !HasStateAuthority || IsSpentSafe() || other == null)
                return;

            TryResolveCollision(other);
        }

        private bool ShouldExpire()
        {
            if (!CanUseNetworkedState())
                return true;
            return _lifetime.ExpiredOrNotRunning(Runner);
        }

        private bool ResolveOverlapsAt(Vector3 origin)
        {
            var radius = _collider != null ? _collider.radius * GetMaxAxis(transform.lossyScale) : 0.18f;
            var count = Physics.OverlapSphereNonAlloc(origin, radius, OverlapHits, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            for (var i = 0; i < count; i++)
            {
                var col = OverlapHits[i];
                if (col == null || col == _collider || !IsRelevantSweepTarget(col))
                    continue;

                TryResolveCollision(col);
                if (IsSpentSafe())
                    return true;

                if (!col.isTrigger)
                {
                    EndShot();
                    return true;
                }
            }

            return false;
        }

        private bool TrySweepHit(Vector3 origin, Vector3 step, out Collider hitCollider)
        {
            hitCollider = null;
            if (step.sqrMagnitude < 0.000001f)
                return false;

            var radius = _collider != null ? _collider.radius * GetMaxAxis(transform.lossyScale) : 0.18f;
            var count = Physics.SphereCastNonAlloc(origin, radius, step.normalized, SweepHits, step.magnitude,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);

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
            if (!CanUseNetworkedState() || IsSpentSafe() || other == null)
                return;

            if (other.GetComponentInParent<Projectile>() != null)
                return;

            var victim = other.GetComponentInParent<PlayerAvatar>();
            if (victim != null && victim.Object != null && victim.Object.IsValid)
            {
                if (!victim.IsAlive)
                {
                    EndShot();
                    return;
                }

                var victimRef = PlayerOwnership.ResolveLogicalOwner(victim);
                if (victimRef != PlayerRef.None && victimRef != _shooterRef)
                {
                    RegisterHit(victim, victimRef);
                    return;
                }

                EndShot();
                return;
            }

            if (ProjectileHitSurface.TryGetBlockingSurface(other, out _))
            {
                EndShot();
                return;
            }

            if (!other.isTrigger && other.GetComponentInParent<PlayerAvatar>() == null)
                EndShot();
        }

        private void RegisterHit(PlayerAvatar victim, PlayerRef victimRef)
        {
            if (IsSpentSafe())
                return;

            if (ValidateHit(victim, victimRef))
            {
                var gm = GameManager.Instance;
                if (gm != null && gm.IsNetworkActive)
                    gm.RequestMasterHit(victimRef, Damage, _shooterRef, transform.position);
                else
                    victim.RequestHit(Damage, _shooterRef, transform.position);
            }

            EndShot();
        }

        private void EndShot()
        {
            if (_spentLocal)
            {
                TryDespawn();
                return;
            }

            if (!HasStateAuthority || !CanUseNetworkedState())
            {
                _spentLocal = true;
                return;
            }

            _spentLocal = true;
            _velocity = Vector3.zero;
            Spent = true;
            TryDespawn();
        }

        private void TryDespawn()
        {
            if (Runner == null || !Runner.IsRunning || Object == null || !Object.IsValid)
                return;

            Runner.Despawn(Object);
        }

        private bool ValidateHit(PlayerAvatar victim, PlayerRef victimRef)
        {
            if (_shooterRef == PlayerRef.None || victimRef == PlayerRef.None || _shooterRef == victimRef)
                return false;

            var planar = transform.position - victim.transform.position;
            planar.y = 0f;
            return planar.sqrMagnitude <= MaxHitDistance * MaxHitDistance;
        }

        private void IgnoreShooterCollisions()
        {
            if (_collider == null || _shooterRef == PlayerRef.None)
                return;

            var avatar = PlayerRegistry.FindAvatar(_shooterRef);
            if (avatar == null || avatar.Object == null || !avatar.Object.IsValid)
            {
                var buffer = PlayerRegistry.CopyAllAvatars();
                for (var i = 0; i < buffer.Count; i++)
                {
                    var candidate = buffer[i];
                    if (candidate == null || candidate.Object == null || !candidate.Object.IsValid)
                        continue;
                    if (PlayerOwnership.ResolveLogicalOwner(candidate) == _shooterRef)
                    {
                        avatar = candidate;
                        break;
                    }
                }
            }

            if (avatar == null)
                return;

            var cols = avatar.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < cols.Length; i++)
            {
                var col = cols[i];
                if (col != null && col != _collider)
                    Physics.IgnoreCollision(_collider, col, true);
            }
        }
    }
}
