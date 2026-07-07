using FusionMultiplayer.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FusionMultiplayer.Environment
{
    /// <summary>Identifies which playable map layout this game scene uses.</summary>
    public sealed class MapSceneMarker : MonoBehaviour
    {
        [SerializeField] private SessionCatalog.MapKind _mapKind = SessionCatalog.MapKind.Arena;

        public SessionCatalog.MapKind MapKind => _mapKind;

        private void Awake()
        {
            var index = SceneManager.GetActiveScene().buildIndex;
            if (SceneIndices.IsGameScene(index))
                _mapKind = SceneIndices.GetMapKind(index);
        }

        public void ApplyMapKind(SessionCatalog.MapKind kind) => _mapKind = kind;

#if UNITY_EDITOR
        public void SetMapKind(SessionCatalog.MapKind kind) => _mapKind = kind;
#endif
    }
}
