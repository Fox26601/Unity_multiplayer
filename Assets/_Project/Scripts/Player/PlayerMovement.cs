using Fusion;
using FusionMultiplayer.Environment;
using UnityEngine;

namespace FusionMultiplayer.Player
{
    /// <summary>
    /// Minecraft-style locomotion via Fusion <see cref="NetworkCharacterController"/>.
    /// Bots drive the same path through <see cref="SetBotMove"/>.
    /// </summary>
    [RequireComponent(typeof(NetworkCharacterController))]
    [DefaultExecutionOrder(0)]
    public class PlayerMovement : NetworkBehaviour
    {
        /// <summary>Jump apex height in world units (1.5 × <see cref="BuildGrid.TileSize"/>).</summary>
        public const float JumpApexHeight = 1.5f;

        [SerializeField] private float _moveSpeed = 4f;

        private NetworkCharacterController _ncc;
        private BotBrain _botBrain;
        private PlayerAvatar _avatar;
        private NetworkButtons _previousButtons;

        private void Awake()
        {
            _ncc = GetComponent<NetworkCharacterController>();
            _botBrain = GetComponent<BotBrain>();
            _avatar = GetComponent<PlayerAvatar>();
        }

        public override void Spawned()
        {
            if (_ncc == null)
                _ncc = GetComponent<NetworkCharacterController>();
            ApplyNccTuning();
        }

        public override void FixedUpdateNetwork()
        {
            if (_ncc == null)
                return;

            if (_avatar != null && !_avatar.IsAlive)
                return;

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

            if (HasInputAuthority)
                ApplyInputMotor();
        }

        private void ApplyInputMotor()
        {
            if (!GetInput(out GameplayNetworkInput input))
            {
                ApplyMove(Vector3.zero, false);
                return;
            }

            var yaw = input.LookYaw;
            if (HasInputAuthority && PlayerLook.TryGetLocalLook(out var liveYaw, out _))
                yaw = liveYaw;

            var basis = Quaternion.Euler(0f, yaw, 0f);
            var direction = basis * Vector3.forward * input.MoveForward
                            + basis * Vector3.right * input.Strafe;
            var wantJump = input.Buttons.WasPressed(_previousButtons, GameplayButton.Jump);
            _previousButtons = input.Buttons;
            ApplyMove(direction, wantJump);
        }

        /// <summary>Bot locomotion: world-space horizontal intent + optional jump.</summary>
        public void SetBotMove(Vector3 horizontalWorldVelocity, bool wantJump)
        {
            if (!HasStateAuthority)
                return;

            ApplyMove(horizontalWorldVelocity, wantJump);
        }

        public bool IsGrounded => _ncc != null && _ncc.Grounded;

        private void ApplyMove(Vector3 horizontalIntent, bool wantJump)
        {
            if (_ncc == null)
                return;

            ApplyNccTuning();

            var flat = horizontalIntent;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.0001f)
                _ncc.Move(flat.normalized);
            else
                _ncc.Move(Vector3.zero);

            if (wantJump)
            {
                var impulse = Mathf.Sqrt(2f * Mathf.Abs(_ncc.gravity) * JumpApexHeight);
                _ncc.Jump(overrideImpulse: impulse);
            }
        }

        private void ApplyNccTuning()
        {
            if (_ncc == null)
                return;

            // Look owns body yaw (FPS); NCC must not rotate toward move direction.
            _ncc.rotationSpeed = 0f;
            _ncc.maxSpeed = _moveSpeed;
            _ncc.acceleration = Mathf.Max(_ncc.acceleration, _moveSpeed * 8f);
            _ncc.braking = Mathf.Max(_ncc.braking, _moveSpeed * 8f);
        }
    }
}
