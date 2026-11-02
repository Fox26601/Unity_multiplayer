using FusionMultiplayer.Core;
using UnityEngine;

namespace FusionMultiplayer.Data
{
    [CreateAssetMenu(fileName = "MapCatalog", menuName = "Fusion Multiplayer/Map Catalog")]
    public sealed class MapCatalog : ScriptableObject
    {
        [SerializeField] private MapDefinition _arena;
        [SerializeField] private MapDefinition _plaza;
        [SerializeField] private MapDefinition _ruins;

        public MapDefinition Arena => _arena;
        public MapDefinition Plaza => _plaza;
        public MapDefinition Ruins => _ruins;

        public IMapStrategy GetStrategy(SessionCatalog.MapKind map) => map switch
        {
            SessionCatalog.MapKind.Plaza => _plaza,
            SessionCatalog.MapKind.Ruins => _ruins,
            _ => _arena
        };

        public MapDefinition GetDefinition(SessionCatalog.MapKind map) =>
            GetStrategy(map) as MapDefinition;

        public string GetLabel(SessionCatalog.MapKind map)
        {
            if (map == SessionCatalog.MapKind.Any)
                return "Any map";
            var def = GetDefinition(map);
            return def != null ? def.Label : map.ToString();
        }

        public string GetDescription(SessionCatalog.MapKind map)
        {
            var def = GetDefinition(map);
            return def != null ? def.Description : string.Empty;
        }

        public bool HasAllMaps => _arena != null && _plaza != null && _ruins != null;
    }
}
