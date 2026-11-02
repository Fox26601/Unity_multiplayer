using UnityEngine;

namespace FusionMultiplayer.Data
{
    [CreateAssetMenu(fileName = "MatchConfig", menuName = "Fusion Multiplayer/Match Config")]
    public sealed class MatchConfig : ScriptableObject
    {
        [SerializeField] private float _matchDurationSeconds = 300f;
        [SerializeField] private float _voteDurationSeconds = 30f;
        [SerializeField] private float _physicsPropFirstDelayMin = 20f;
        [SerializeField] private float _physicsPropFirstDelayMax = 35f;
        [SerializeField] private float _physicsPropRepeatMin = 18f;
        [SerializeField] private float _physicsPropRepeatMax = 30f;

        public float MatchDurationSeconds => _matchDurationSeconds;
        public float VoteDurationSeconds => _voteDurationSeconds;
        public float PhysicsPropFirstDelayMin => _physicsPropFirstDelayMin;
        public float PhysicsPropFirstDelayMax => _physicsPropFirstDelayMax;
        public float PhysicsPropRepeatMin => _physicsPropRepeatMin;
        public float PhysicsPropRepeatMax => _physicsPropRepeatMax;
    }
}
