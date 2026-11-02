using FusionMultiplayer.Core;
using UnityEngine;

namespace FusionMultiplayer.Data
{
    [CreateAssetMenu(fileName = "MapDefinition", menuName = "Fusion Multiplayer/Map Definition")]
    public sealed class MapDefinition : ScriptableObject, IMapStrategy
    {
        public const int SpawnSlotCount = 10;

        [SerializeField] private SessionCatalog.MapKind _kind = SessionCatalog.MapKind.Arena;
        [SerializeField] private string _label = "Arena";
        [SerializeField] private string _description = "";
        [SerializeField] private Vector3[] _spawnPositions = new Vector3[SpawnSlotCount];
        [SerializeField] private float[] _spawnYaws = new float[SpawnSlotCount];

        public SessionCatalog.MapKind Kind => _kind;
        public string Label => _label;
        public string Description => _description;

        public Vector3 GetSpawnPosition(int index)
        {
            if (_spawnPositions == null || index < 0 || index >= _spawnPositions.Length)
                return Vector3.zero;
            return _spawnPositions[index];
        }

        public float GetSpawnYaw(int index)
        {
            if (_spawnYaws == null || index < 0 || index >= _spawnYaws.Length)
                return 0f;
            return _spawnYaws[index];
        }

        public void EditorSetSpawns(SessionCatalog.MapKind kind, string label, string description,
            Vector3[] positions, float[] yaws)
        {
            _kind = kind;
            _label = label;
            _description = description;
            _spawnPositions = positions;
            _spawnYaws = yaws;
        }
    }
}
