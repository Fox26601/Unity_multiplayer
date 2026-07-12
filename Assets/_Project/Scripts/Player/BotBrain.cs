using Fusion;
using FusionMultiplayer.Core;
using UnityEngine;

namespace FusionMultiplayer.Player
{
    /// <summary>
    /// Server-side bot: Patrol → Chase → Combat → Search with LOS, memory, and whisker steering.
    /// </summary>
    public sealed class BotBrain : MonoBehaviour
    {
        private enum BotState
        {
            Patrol,
            Chase,
            Combat,
            Search
        }

        [Header("Perception")]
        [SerializeField] private float _detectRadius = 35f;
        [SerializeField] private float _combatRange = 14f;
        [SerializeField] private float _eyeHeight = 1.5f;
        [SerializeField] private float _chestHeight = 1.2f;
        [SerializeField] private float _thinkInterval = 0.25f;
        [SerializeField] private float _searchDuration = 4.5f;
        [SerializeField] private float _aimAngleDegrees = 12f;
        [SerializeField] private float _fireCooldown = 0.55f;

        [Header("Movement")]
        [SerializeField] private float _patrolSpeed = 3.5f;
        [SerializeField] private float _chaseSpeed = 5.5f;
        [SerializeField] private float _combatStrafeSpeed = 2.2f;
        [SerializeField] private float _steerProbeDistance = 1.75f;
        [SerializeField] private float _steerProbeRadius = 0.28f;
        [SerializeField] private float _stuckSeconds = 1.1f;
        [SerializeField] private float _stuckMoveEpsilon = 0.08f;

        private static readonly float[] WhiskerAngles = { 0f, -28f, 28f, -55f, 55f, -85f, 85f };

        private PlayerAvatar _avatar;
        private CharacterController _cc;
        private PlayerLook _look;
        private PlayerWeapon _weapon;
        private Collider[] _ownColliders;
        private readonly RaycastHit[] _hits = new RaycastHit[8];

        private bool _active;
        private BotState _state = BotState.Patrol;
        private PlayerRef _originalOwner;
        private PlayerAvatar _target;
        private Vector3 _lastSeenPos;
        private float _lostSightUntil;
        private Vector3 _patrolTarget;
        private float _nextThinkTime;
        private float _nextFireTime;
        private float _strafeSign = 1f;
        private float _nextStrafeFlip;
        private Vector3 _lastPosSample;
        private float _stuckSince = -1f;
        private bool _wasDead;

        public bool IsActive => _active;

        public void Activate(PlayerRef originalOwner)
        {
            _originalOwner = originalOwner;
            _avatar = GetComponent<PlayerAvatar>();
            _cc = GetComponent<CharacterController>();
            _look = GetComponent<PlayerLook>();
            _weapon = GetComponent<PlayerWeapon>();
            _ownColliders = GetComponentsInChildren<Collider>(true);
            _active = true;
            _wasDead = _avatar != null && _avatar.IsDead;
            ResetMemory();
            PickPatrolTarget();
            _lastPosSample = transform.position;
            _stuckSince = -1f;
        }

        public void Deactivate()
        {
            _active = false;
            _target = null;
        }

        private void ResetMemory()
        {
            _state = BotState.Patrol;
            _target = null;
            _lostSightUntil = 0f;
            _nextThinkTime = 0f;
            _nextFireTime = 0f;
        }

        private void Update()
        {
            if (!_active || _avatar == null || !_avatar.HasStateAuthority)
                return;

            if (_avatar.IsDead)
            {
                _wasDead = true;
                return;
            }

            if (_wasDead)
            {
                _wasDead = false;
                ResetMemory();
                PickPatrolTarget();
            }

            if (Time.time >= _nextThinkTime)
            {
                _nextThinkTime = Time.time + _thinkInterval;
                Think();
            }

            TickMove();
            TickAimAndFire();
            TrackStuck();
        }

        private void Think()
        {
            if (TryAcquireVisibleTarget(out var visible, out var dist))
            {
                _target = visible;
                _lastSeenPos = ChestPoint(visible);
                _lostSightUntil = 0f;

                _state = dist <= _combatRange ? BotState.Combat : BotState.Chase;

                if ((transform.position - _patrolTarget).sqrMagnitude < 2f)
                    PickPatrolTarget();
                return;
            }

            if (_target != null || _lostSightUntil > Time.time)
            {
                if (_lostSightUntil <= 0f)
                    _lostSightUntil = Time.time + _searchDuration;

                if (Time.time < _lostSightUntil)
                {
                    _state = BotState.Search;
                    if (_target != null && _target.IsAlive)
                        _lastSeenPos = ChestPoint(_target);
                    return;
                }
            }

            _target = null;
            _lostSightUntil = 0f;
            _state = BotState.Patrol;
            if ((transform.position - _patrolTarget).sqrMagnitude < 2.5f)
                PickPatrolTarget();
        }

        private void TickMove()
        {
            if (_cc == null || !_cc.enabled)
                return;

            Vector3 goal;
            float speed;
            var strafe = Vector3.zero;

            switch (_state)
            {
                case BotState.Chase:
                    goal = _target != null ? FlatPos(_target.transform.position) : FlatPos(_lastSeenPos);
                    speed = _chaseSpeed;
                    break;
                case BotState.Combat:
                    goal = transform.position;
                    speed = _combatStrafeSpeed;
                    if (Time.time >= _nextStrafeFlip)
                    {
                        _nextStrafeFlip = Time.time + Random.Range(0.7f, 1.4f);
                        _strafeSign = -_strafeSign;
                    }

                    if (_target != null)
                    {
                        var toEnemy = FlatPos(_target.transform.position) - FlatPos(transform.position);
                        if (toEnemy.sqrMagnitude > 0.01f)
                        {
                            var side = Vector3.Cross(Vector3.up, toEnemy.normalized);
                            strafe = side * _strafeSign;
                            // Hold distance: ease away if too close, nudge in if a bit far.
                            var d = toEnemy.magnitude;
                            if (d < _combatRange * 0.45f)
                                goal = FlatPos(transform.position) - toEnemy.normalized * 2f;
                            else if (d > _combatRange * 0.85f)
                                goal = FlatPos(_target.transform.position);
                        }
                    }

                    break;
                case BotState.Search:
                    goal = FlatPos(_lastSeenPos);
                    speed = _patrolSpeed;
                    break;
                default:
                    goal = FlatPos(_patrolTarget);
                    speed = _patrolSpeed;
                    break;
            }

            var desired = goal - FlatPos(transform.position);
            desired.y = 0f;
            if (strafe.sqrMagnitude > 0.01f)
            {
                if (desired.sqrMagnitude > 0.01f)
                    desired = (desired.normalized * 0.35f + strafe.normalized).normalized;
                else
                    desired = strafe.normalized;
            }

            Vector3 moveDir;
            if (desired.sqrMagnitude < 0.04f)
                moveDir = Vector3.zero;
            else
                moveDir = ComputeSteerDir(desired.normalized);

            if (moveDir.sqrMagnitude > 0.01f)
                _cc.Move(moveDir * speed * Time.deltaTime + Physics.gravity * Time.deltaTime);
            else
                _cc.Move(Physics.gravity * Time.deltaTime);
        }

        private void TickAimAndFire()
        {
            if (_look == null)
                return;

            switch (_state)
            {
                case BotState.Combat:
                case BotState.Chase:
                    if (_target != null && _target.IsAlive)
                        _look.ApplyBotAim(ChestPoint(_target));
                    break;
                case BotState.Search:
                    _look.ApplyBotAim(_lastSeenPos + Vector3.up * 0.4f *
                        Mathf.Sin(Time.time * 2.2f));
                    break;
                default:
                    if ((FlatPos(_patrolTarget) - FlatPos(transform.position)).sqrMagnitude > 0.25f)
                        _look.ApplyBotAim(_patrolTarget + Vector3.up * _chestHeight);
                    break;
            }

            if (_state != BotState.Combat || Time.time < _nextFireTime)
                return;
            if (!SessionRuntime.AllowsShoot || _weapon == null)
                return;
            if (_target == null || !_target.IsAlive)
                return;
            if (!HasLineOfSight(_target))
                return;
            if (!IsAimedAt(ChestPoint(_target)))
                return;

            _nextFireTime = Time.time + _fireCooldown;
            _weapon.ServerBotFire();
        }

        private bool TryAcquireVisibleTarget(out PlayerAvatar best, out float bestDist)
        {
            best = null;
            bestDist = float.MaxValue;
            var maxSq = _detectRadius * _detectRadius;

            foreach (var other in FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
            {
                if (other == _avatar || other.Object == null || !other.Object.IsValid || !other.IsAlive)
                    continue;
                if (PlayerOwnership.ResolveLogicalOwner(other) == _originalOwner)
                    continue;

                var delta = other.transform.position - transform.position;
                delta.y = 0f;
                var sq = delta.sqrMagnitude;
                if (sq > maxSq)
                    continue;
                if (!HasLineOfSight(other))
                    continue;

                var dist = Mathf.Sqrt(sq);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = other;
                }
            }

            return best != null;
        }

        private bool HasLineOfSight(PlayerAvatar target)
        {
            if (target == null)
                return false;

            var origin = transform.position + Vector3.up * _eyeHeight;
            var dest = ChestPoint(target);
            var to = dest - origin;
            var dist = to.magnitude;
            if (dist < 0.05f)
                return true;

            var dir = to / dist;
            var count = Physics.SphereCastNonAlloc(
                origin,
                0.12f,
                dir,
                _hits,
                dist,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            var nearest = float.MaxValue;
            Collider nearestCol = null;
            for (var i = 0; i < count; i++)
            {
                var col = _hits[i].collider;
                if (col == null || IsOwnCollider(col))
                    continue;
                if (_hits[i].distance < nearest)
                {
                    nearest = _hits[i].distance;
                    nearestCol = col;
                }
            }

            if (nearestCol == null)
                return true;

            var hitAvatar = nearestCol.GetComponentInParent<PlayerAvatar>();
            return hitAvatar == target;
        }

        private bool IsAimedAt(Vector3 worldPoint)
        {
            var eye = transform.position + Vector3.up * _eyeHeight;
            var cam = transform.Find("PlayerCamera");
            var forward = cam != null ? cam.forward : transform.forward;
            var to = worldPoint - eye;
            if (to.sqrMagnitude < 0.01f)
                return true;
            return Vector3.Angle(forward, to) <= _aimAngleDegrees;
        }

        private Vector3 ComputeSteerDir(Vector3 desiredFlat)
        {
            desiredFlat.y = 0f;
            if (desiredFlat.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            desiredFlat.Normalize();
            var origin = transform.position + Vector3.up * (_cc != null ? _cc.height * 0.45f : 0.9f);
            var bestDir = desiredFlat;
            var bestScore = float.MinValue;

            for (var i = 0; i < WhiskerAngles.Length; i++)
            {
                var dir = Quaternion.Euler(0f, WhiskerAngles[i], 0f) * desiredFlat;
                var blocked = Physics.SphereCast(
                    origin,
                    _steerProbeRadius,
                    dir,
                    out var hit,
                    _steerProbeDistance,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore);

                float clearance;
                if (!blocked || IsOwnCollider(hit.collider))
                {
                    clearance = _steerProbeDistance;
                }
                else
                {
                    var hitAvatar = hit.collider.GetComponentInParent<PlayerAvatar>();
                    if (hitAvatar != null)
                        clearance = _steerProbeDistance;
                    else
                        clearance = hit.distance;
                }

                // Prefer forward alignment and open space.
                var score = clearance + Vector3.Dot(dir, desiredFlat) * 0.65f;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDir = dir;
                }
            }

            // If forward whisker blocked, bias along wall normal of center cast.
            if (Physics.SphereCast(
                    origin,
                    _steerProbeRadius,
                    desiredFlat,
                    out var centerHit,
                    _steerProbeDistance * 0.85f,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore) &&
                !IsOwnCollider(centerHit.collider) &&
                centerHit.collider.GetComponentInParent<PlayerAvatar>() == null)
            {
                var slide = Vector3.ProjectOnPlane(desiredFlat, centerHit.normal);
                slide.y = 0f;
                if (slide.sqrMagnitude > 0.01f)
                    bestDir = (bestDir + slide.normalized).normalized;
            }

            return bestDir;
        }

        private void TrackStuck()
        {
            var moved = Vector3.Distance(FlatPos(transform.position), FlatPos(_lastPosSample));
            var wantsMove = _state is BotState.Patrol or BotState.Chase or BotState.Search;
            if (wantsMove && moved < _stuckMoveEpsilon)
            {
                if (_stuckSince < 0f)
                    _stuckSince = Time.time;
                else if (Time.time - _stuckSince >= _stuckSeconds)
                {
                    _stuckSince = -1f;
                    if (_state == BotState.Search)
                    {
                        _lostSightUntil = 0f;
                        _target = null;
                        _state = BotState.Patrol;
                    }

                    PickPatrolTarget();
                    // Nudge sideways to escape corner.
                    var nudge = Quaternion.Euler(0f, Random.Range(60f, 120f) * (_strafeSign >= 0 ? 1f : -1f), 0f) *
                                transform.forward;
                    if (_cc != null && _cc.enabled)
                        _cc.Move(nudge.normalized * 0.6f);
                }
            }
            else
            {
                _stuckSince = -1f;
            }

            _lastPosSample = transform.position;
        }

        private bool IsOwnCollider(Collider col)
        {
            if (col == null || _ownColliders == null)
                return false;
            for (var i = 0; i < _ownColliders.Length; i++)
            {
                if (_ownColliders[i] == col)
                    return true;
            }

            return col.transform.IsChildOf(transform);
        }

        private static Vector3 FlatPos(Vector3 p) => new Vector3(p.x, 0f, p.z);

        private Vector3 ChestPoint(PlayerAvatar target) =>
            target.transform.position + Vector3.up * _chestHeight;

        private void PickPatrolTarget()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.GetRandomizedSpawn(Random.Range(0, 10), out _patrolTarget, out _);
                _patrolTarget += new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
                return;
            }

            _patrolTarget = transform.position + new Vector3(Random.Range(-8f, 8f), 0f, Random.Range(-8f, 8f));
        }
    }
}
