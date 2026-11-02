using UnityEngine;

namespace FusionMultiplayer.Data
{
    [CreateAssetMenu(fileName = "PlayerStatsConfig", menuName = "Fusion Multiplayer/Player Stats Config")]
    public sealed class PlayerStatsConfig : ScriptableObject
    {
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _respawnDelaySeconds = 3f;

        public float MaxHealth => _maxHealth;
        public float RespawnDelaySeconds => _respawnDelaySeconds;
    }
}
