using Fusion;
using UnityEngine;

namespace FusionMultiplayer.Player
{
    /// <summary>
    /// Minecraft-style movement: WASD relative to look direction (local mouse yaw).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : NetworkBehaviour
    {
        [SerializeField] private float _moveSpeed = 4f;

        private CharacterController _cc;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;
            if (!GetInput(out GameplayNetworkInput input)) return;

            var avatar = GetComponent<PlayerAvatar>();
            if (avatar != null && !avatar.IsAlive)
                return;

            var move = transform.forward * (input.MoveForward * _moveSpeed)
                       + transform.right * (input.Strafe * _moveSpeed);
            move *= Runner.DeltaTime;
            _cc.Move(move + Physics.gravity * Runner.DeltaTime);
        }
    }
}
