using UnityEngine;

namespace FusionMultiplayer.Data
{
    /// <summary>
    /// Hub asset referencing all gameplay/UI config ScriptableObjects.
    /// Set once at boot via <see cref="SetCurrent"/> or Resources path Configs/GameConfigs.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfigs", menuName = "Fusion Multiplayer/Game Configs Hub")]
    public sealed class GameConfigs : ScriptableObject
    {
        public const string ResourcesPath = "Configs/GameConfigs";

        [SerializeField] private MatchConfig _match;
        [SerializeField] private CombatConfig _combat;
        [SerializeField] private ReconnectConfig _reconnect;
        [SerializeField] private PlayerStatsConfig _playerStats;
        [SerializeField] private BotConfig _bot;
        [SerializeField] private DifficultyCatalog _difficulty;
        [SerializeField] private MapCatalog _maps;
        [SerializeField] private GameModeCatalog _modes;
        [SerializeField] private UiThemeConfig _uiTheme;
        [SerializeField] private NetworkPrefabCatalog _prefabs;

        public static GameConfigs Current { get; private set; }

        public MatchConfig Match => _match;
        public CombatConfig Combat => _combat;
        public ReconnectConfig Reconnect => _reconnect;
        public PlayerStatsConfig PlayerStats => _playerStats;
        public BotConfig Bot => _bot;
        public DifficultyCatalog Difficulty => _difficulty;
        public MapCatalog Maps => _maps;
        public GameModeCatalog Modes => _modes;
        public UiThemeConfig UiTheme => _uiTheme;
        public NetworkPrefabCatalog Prefabs => _prefabs;

        public static void SetCurrent(GameConfigs configs)
        {
            if (configs != null)
                Current = configs;
        }

        /// <summary>Ensures Current is set from explicit ref or Resources.</summary>
        public static GameConfigs EnsureCurrent(GameConfigs preferred = null)
        {
            if (preferred != null)
            {
                Current = preferred;
                return Current;
            }

            if (Current != null)
                return Current;

            Current = Resources.Load<GameConfigs>(ResourcesPath);
            return Current;
        }

        public bool HasRequiredRefs =>
            _match != null &&
            _combat != null &&
            _reconnect != null &&
            _playerStats != null &&
            _bot != null &&
            _difficulty != null &&
            _maps != null &&
            _modes != null &&
            _uiTheme != null &&
            _prefabs != null;
    }
}
