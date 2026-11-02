using UnityEngine;

namespace FusionMultiplayer.Data
{
    [CreateAssetMenu(fileName = "CombatConfig", menuName = "Fusion Multiplayer/Combat Config")]
    public sealed class CombatConfig : ScriptableObject
    {
        [SerializeField] private float _critChance = 0.18f;
        [SerializeField] private float _critMultiplier = 1.75f;

        public float CritChance => _critChance;
        public float CritMultiplier => _critMultiplier;
    }
}
