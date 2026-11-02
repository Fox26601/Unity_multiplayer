using FusionMultiplayer.Core;
using UnityEngine;

namespace FusionMultiplayer.Data
{
    [CreateAssetMenu(fileName = "DifficultyCatalog", menuName = "Fusion Multiplayer/Difficulty Catalog")]
    public sealed class DifficultyCatalog : ScriptableObject
    {
        [SerializeField] private float _easyDamageMultiplier = 0.75f;
        [SerializeField] private float _normalDamageMultiplier = 1f;
        [SerializeField] private float _hardDamageMultiplier = 1.35f;
        [SerializeField] private string _easyLabel = "Easy";
        [SerializeField] private string _normalLabel = "Normal";
        [SerializeField] private string _hardLabel = "Hard";

        public float GetDamageMultiplier(SessionCatalog.DifficultyKind difficulty) => difficulty switch
        {
            SessionCatalog.DifficultyKind.Easy => _easyDamageMultiplier,
            SessionCatalog.DifficultyKind.Hard => _hardDamageMultiplier,
            _ => _normalDamageMultiplier
        };

        public string GetLabel(SessionCatalog.DifficultyKind difficulty) => difficulty switch
        {
            SessionCatalog.DifficultyKind.Easy => _easyLabel,
            SessionCatalog.DifficultyKind.Hard => _hardLabel,
            _ => _normalLabel
        };
    }
}
