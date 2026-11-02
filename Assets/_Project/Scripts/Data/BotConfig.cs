using UnityEngine;

namespace FusionMultiplayer.Data
{
    [CreateAssetMenu(fileName = "BotConfig", menuName = "Fusion Multiplayer/Bot Config")]
    public sealed class BotConfig : ScriptableObject
    {
        [Header("Perception")]
        [SerializeField] private float _detectRadius = 35f;
        [SerializeField] private float _combatRange = 14f;
        [SerializeField] private float _eyeHeight = 1.5f;
        [SerializeField] private float _chestHeight = 1.2f;
        [SerializeField] private float _thinkInterval = 0.35f;
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
        [SerializeField] private float _climbProbeDistance = 0.55f;
        [SerializeField] private float _maxAutoJumpLedge = 1.15f;

        public float DetectRadius => _detectRadius;
        public float CombatRange => _combatRange;
        public float EyeHeight => _eyeHeight;
        public float ChestHeight => _chestHeight;
        public float ThinkInterval => _thinkInterval;
        public float SearchDuration => _searchDuration;
        public float AimAngleDegrees => _aimAngleDegrees;
        public float FireCooldown => _fireCooldown;
        public float PatrolSpeed => _patrolSpeed;
        public float ChaseSpeed => _chaseSpeed;
        public float CombatStrafeSpeed => _combatStrafeSpeed;
        public float SteerProbeDistance => _steerProbeDistance;
        public float SteerProbeRadius => _steerProbeRadius;
        public float StuckSeconds => _stuckSeconds;
        public float StuckMoveEpsilon => _stuckMoveEpsilon;
        public float ClimbProbeDistance => _climbProbeDistance;
        public float MaxAutoJumpLedge => _maxAutoJumpLedge;
    }
}
