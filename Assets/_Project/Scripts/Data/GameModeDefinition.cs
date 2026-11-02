using FusionMultiplayer.Core;
using UnityEngine;

namespace FusionMultiplayer.Data
{
    [CreateAssetMenu(fileName = "GameModeDefinition", menuName = "Fusion Multiplayer/Game Mode Definition")]
    public sealed class GameModeDefinition : ScriptableObject
    {
        [SerializeField] private SessionCatalog.GameModeKind _kind = SessionCatalog.GameModeKind.Sandbox;
        [SerializeField] private string _label = "Sandbox";
        [SerializeField] private string _description = "";
        [SerializeField] private bool _allowBuild = true;
        [SerializeField] private bool _allowCombat = true;
        [SerializeField] private bool _allowBreakOthers = true;

        public SessionCatalog.GameModeKind Kind => _kind;
        public string Label => _label;
        public string Description => _description;
        public bool AllowBuild => _allowBuild;
        public bool AllowCombat => _allowCombat;
        public bool AllowBreakOthers => _allowBreakOthers;

        public void EditorSet(SessionCatalog.GameModeKind kind, string label, string description,
            bool allowBuild, bool allowCombat, bool allowBreakOthers)
        {
            _kind = kind;
            _label = label;
            _description = description;
            _allowBuild = allowBuild;
            _allowCombat = allowCombat;
            _allowBreakOthers = allowBreakOthers;
        }
    }
}
