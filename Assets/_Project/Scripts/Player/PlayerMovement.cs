using Fusion;
using FusionMultiplayer.Environment;
using UnityEngine;

namespace FusionMultiplayer.Player
{
    /// <summary>
    /// Minecraft-style locomotion: WASD + Space jump (apex ~1.5 tiles). Bots drive the same motor via SetBotMove.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : NetworkBehaviour
    {
        /// <summary>Jump apex height in world units (1.5 × <see cref="BuildGrid.TileSize"/>).</summary>
        public const float JumpApexHeight = 1.5f;

        [SerializeField] private float _moveSpeed = 4f;

        private CharacterController _cc;
        private BotBrain _botBrain;
        private float _verticalVelocity;
        private NetworkButtons _previousButtons;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _botBrain = GetComponent<BotBrain>();
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            var avatar = GetComponent<PlayerAvatar>();
            if (avatar != null && !avatar.IsAlive)
            {
                _verticalVelocity = 0f;
                return;
            }

            if (_botBrain == null)
                _botBrain = GetComponent<BotBrain>();

            if (_botBrain != null && _botBrain.IsActive)
            {
                _botBrain.SimulationTick(this);
                return;
            }

            if (!GetInput(out GameplayNetworkInput input))
            {
                ApplyMotor(Vector3.zero, false);
                return;
            }

            var horizontal = transform.forward * (input.MoveForward * _moveSpeed)
                             + transform.right * (input.Strafe * _moveSpeed);
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
