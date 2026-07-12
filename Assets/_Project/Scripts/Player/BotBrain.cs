using Fusion;
using FusionMultiplayer.Core;
using UnityEngine;

namespace FusionMultiplayer.Player
{
    /// <summary>
    /// Server-side bot controller: patrol → seek → shoot. Avatar executes; brain decides.
    /// </summary>
    public sealed class BotBrain : MonoBehaviour
    {
        private enum BotState
        {
            Patrol,
            Seek,
            Shoot
        }

        private PlayerAvatar _avatar;
        private CharacterController _cc;
        private PlayerLook _look;
        private bool _active;
        private BotState _state = BotState.Patrol;
        private Vector3 _patrolTarget;
        private float _nextThinkTime;
        private float _nextFireTime;
        private PlayerRef _originalOwner;

        public void Activate(PlayerRef originalOwner)
        {
            _originalOwner = originalOwner;
            _avatar = GetComponent<PlayerAvatar>();
            _cc = GetComponent<CharacterController>();
            _look = GetComponent<PlayerLook>();
            _active = true;
            PickPatrolTarget();
        }

        public void Deactivate()
        {
            _active = false;
        }

        private void Update()
        {
            if (!_active || _avatar == null || !_avatar.HasStateAuthority)
                return;

            if (_avatar.IsDead)
                return;

            if (Time.time < _nextThinkTime)
            {
                TickMove();
                return;
            }

            _nextThinkTime = Time.time + 0.35f;
            Think();
            TickMove();
            TryShoot();
        }

        private void Think()
        {
            var target = FindNearestEnemy();
            if (target == null)
            {
                _state = BotState.Patrol;
                if ((transform.position - _patrolTarget).sqrMagnitude < 2f)
                    PickPatrolTarget();
                return;
            }

            var toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            var dist = toTarget.magnitude;
            if (dist < 14f)
                _state = BotState.Shoot;
            else
                _state = BotState.Seek;
        }

        private void TickMove()
        {
            if (_cc == null || !_cc.enabled)
                return;

            Vector3 goal;
            if (_state == BotState.Patrol)
                goal = _patrolTarget;
            else
            {
                var enemy = FindNearestEnemy();
                goal = enemy != null ? enemy.transform.position : _patrolTarget;
            }

            var flat = goal - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.05f)
                return;

            var dir = flat.normalized;
            var speed = _state == BotState.Seek ? 5.5f : 3.5f;
            _cc.Move(dir * speed * Time.deltaTime + Physics.gravity * Time.deltaTime);

            var lookRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 6f);
            if (_look != null)
            {
                // Keep yaw roughly aligned for muzzle aim.
            }
        }

        private void TryShoot()
        {
            if (_state != BotState.Shoot || Time.time < _nextFireTime)
                return;

            if (!SessionRuntime.AllowsShoot)
                return;

            var weapon = GetComponent<PlayerWeapon>();
            if (weapon == null)
                return;

            _nextFireTime = Time.time + 0.55f;
            weapon.ServerBotFire();
        }

        private PlayerAvatar FindNearestEnemy()
        {
            PlayerAvatar best = null;
            var bestSq = float.MaxValue;
            foreach (var other in FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
            {
                if (other == _avatar || other.Object == null || !other.Object.IsValid || !other.IsAlive)
                    continue;
                if (PlayerOwnership.ResolveLogicalOwner(other) == _originalOwner)
                    continue;

                var sq = (other.transform.position - transform.position).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = other;
                }
            }

            return best;
        }

        private void PickPatrolTarget()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.GetRandomizedSpawn(Random.Range(0, 10), out _patrolTarget, out _);
                return;
            }

            _patrolTarget = transform.position + new Vector3(Random.Range(-8f, 8f), 0f, Random.Range(-8f, 8f));
        }
    }
}
