using FusionMultiplayer.Core;
using UnityEngine;

namespace FusionMultiplayer.Data
{
    [CreateAssetMenu(fileName = "GameModeCatalog", menuName = "Fusion Multiplayer/Game Mode Catalog")]
    public sealed class GameModeCatalog : ScriptableObject
    {
        [SerializeField] private GameModeDefinition _build;
        [SerializeField] private GameModeDefinition _combat;
        [SerializeField] private GameModeDefinition _sandbox;

        public GameModeDefinition Get(SessionCatalog.GameModeKind mode) => mode switch
        {
            SessionCatalog.GameModeKind.Build => _build,
            SessionCatalog.GameModeKind.Combat => _combat,
            _ => _sandbox
        };

        public string GetLabel(SessionCatalog.GameModeKind mode)
        {
            var def = Get(mode);
            return def != null ? def.Label : mode.ToString();
        }

        public string GetDescription(SessionCatalog.GameModeKind mode)
        {
            var def = Get(mode);
            return def != null ? def.Description : string.Empty;
        }

        public bool AllowsShoot(SessionCatalog.GameModeKind mode)
        {
            var def = Get(mode);
            return def == null || def.AllowCombat;
        }

        public bool AllowsBuild(SessionCatalog.GameModeKind mode)
        {
            var def = Get(mode);
            return def == null || def.AllowBuild;
        }

        public bool AllowsBreakOthers(SessionCatalog.GameModeKind mode)
        {
            var def = Get(mode);
            return def == null || def.AllowBreakOthers;
        }

        public bool HasAllModes => _build != null && _combat != null && _sandbox != null;
    }
}
