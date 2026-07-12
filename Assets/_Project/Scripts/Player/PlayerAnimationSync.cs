using Fusion;
using UnityEngine;

namespace FusionMultiplayer.Player
{
    /// <summary>
    /// Syncs Idle / Run / Air animation parameters via NetworkMecanimAnimator (≥3 states).
    /// </summary>
    public sealed class PlayerAnimationSync : NetworkBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");
        private static readonly int ShootHash = Animator.StringToHash("Shoot");

        [Networked] private float NetSpeed { get; set; }
        [Networked] private NetworkBool NetGrounded { get; set; }
        [Networked] private NetworkBool NetShoot { get; set; }

        private Animator _animator;
        private NetworkMecanimAnimator _netAnimator;
        private CharacterController _cc;
        private Vector3 _lastPos;
        private double _shootPulseUntil;

        public override void Spawned()
        {
            _cc = GetComponent<CharacterController>();
            _lastPos = transform.position;
            EnsureAnimatorStack();
        }

        public void PulseShoot()
        {
            if (HasStateAuthority && Runner != null)
                _shootPulseUntil = Runner.SimulationTime + 0.2;
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            var delta = transform.position - _lastPos;
            _lastPos = transform.position;
            delta.y = 0f;
            var dt = Runner != null && Runner.DeltaTime > 0f ? Runner.DeltaTime : 0.016f;
            NetSpeed = delta.magnitude / Mathf.Max(0.0001f, dt);
            NetGrounded = _cc == null || _cc.isGrounded;
            NetShoot = Runner != null && Runner.SimulationTime < _shootPulseUntil;
            ApplyAnimator();
        }

        public override void Render()
        {
            if (HasStateAuthority)
                return;

            ApplyAnimator();
        }

        private void ApplyAnimator()
        {
            if (_animator == null)
                EnsureAnimatorStack();
            if (_animator == null)
                return;

            _animator.SetFloat(SpeedHash, NetSpeed);
            _animator.SetBool(GroundedHash, NetGrounded);
            _animator.SetBool(ShootHash, NetShoot);

            if (_netAnimator != null && HasStateAuthority && NetShoot)
                _netAnimator.SetTrigger("Shoot", false);
        }

        private void EnsureAnimatorStack()
        {
            _animator = GetComponentInChildren<Animator>();
            if (_animator == null)
            {
                var body = transform.Find("Body");
                var host = body != null ? body.gameObject : gameObject;
                _animator = host.GetComponent<Animator>() ?? host.AddComponent<Animator>();
            }

            var shipped = Resources.Load<RuntimeAnimatorController>("PlayerLocomotion");
            if (shipped != null && _animator.runtimeAnimatorController == null)
                _animator.runtimeAnimatorController = shipped;

            _netAnimator = GetComponent<NetworkMecanimAnimator>();
            if (_netAnimator != null && _netAnimator.Animator == null)
                _netAnimator.Animator = _animator;
        }
    }
}
