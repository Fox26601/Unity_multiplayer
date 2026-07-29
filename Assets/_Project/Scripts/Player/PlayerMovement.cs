using Fusion;
using FusionMultiplayer.Environment;
using UnityEngine;

namespace FusionMultiplayer.Player
{
    /// <summary>
    /// Minecraft-style locomotion: WASD + Space jump (apex ~1.5 tiles). Bots drive the same motor via SetBotMove.
    /// StateAuthority is authoritative; InputAuthority without SA runs the same motor as local prediction.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DefaultExecutionOrder(0)]
    public class PlayerMovement : NetworkBehaviour
    {
        /// <summary>Jump apex height in world units (1.5 × <see cref="BuildGrid.TileSize"/>).</summary>
        public const float JumpApexHeight = 1.5f;

        [SerializeField] private float _moveSpeed = 4f;

        private CharacterController _cc;
        private BotBrain _botBrain;
        private PlayerAvatar _avatar;
        private float _verticalVelocity;
        private NetworkButtons _previousButtons;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _botBrain = GetComponent<BotBrain>();
            _avatar = GetComponent<PlayerAvatar>();
        }

        public override void FixedUpdateNetwork()
        {
            if (_avatar != null && !_avatar.IsAlive)
            {
                if (HasStateAuthority || HasInputAuthority)
                    _verticalVelocity = 0f;
                return;
            }

            if (HasStateAuthority)
            {
                if (_botBrain == null)
                    _botBrain = GetComponent<BotBrain>();

                if (_botBrain != null && _botBrain.IsActive)
                {
                    _botBrain.SimulationTick(this);
                    return;
                }

                ApplyInputMotor();
                return;
            }

            // Client prediction: same motor from local input (NT off while predicting).
            if (HasInputAuthority)
                ApplyInputMotor();
        }

        private void ApplyInputMotor()
        {
            if (!GetInput(out GameplayNetworkInput input))
            {
                ApplyMotor(Vector3.zero, false);
                return;
            }

            var yaw = input.LookYaw;
            if (HasInputAuthority && PlayerLook.TryGetLocalLook(out var liveYaw, out _))
                yaw = liveYaw;

            var basis = Quaternion.Euler(0f, yaw, 0f);
            var horizontal = basis * Vector3.forward * (input.MoveForward * _moveSpeed)
                             + basis * Vector3.right * (input.Strafe * _moveSpeed);
            var wantJump = input.Buttons.WasPressed(_previousButtons, GameplayButton.Jump);
            _previousButtons = input.Buttons;
            ApplyMotor(horizontal, wantJump);
        }

        /// <summary>Bot locomotion entry: world-space horizontal velocity (m/s) + optional jump.</summary>
        public void SetBotMove(Vector3 horizontalWorldVelocity, bool wantJump)
        {
            if (!HasStateAuthority)
                return;

            ApplyMotor(horizontalWorldVelocity, wantJump);
        }

        public bool IsGrounded => _cc != null && _cc.enabled && _cc.isGrounded;

        private void ApplyMotor(Vector3 horizontalWorldVelocity, bool wantJump)
        {
            if (_cc == null || !_cc.enabled)
                return;

            var dt = Runner != null ? Runner.DeltaTime : Time.fixedDeltaTime;
            var g = Mathf.Abs(Physics.gravity.y);
            if (g < 0.01f)
                g = 9.81f;

            if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;

            if (wantJump && _cc.isGrounded)
                _verticalVelocity = Mathf.Sqrt(2f * g * JumpApexHeight);

            _verticalVelocity += Physics.gravity.y * dt;

            var motion = horizontalWorldVelocity * dt;
            motion.y = _verticalVelocity * dt;
            _cc.Move(motion);

            if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
        }
    }
}
