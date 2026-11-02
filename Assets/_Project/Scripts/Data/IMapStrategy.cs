using FusionMultiplayer.Core;
using UnityEngine;

namespace FusionMultiplayer.Data
{
    /// <summary>Strategy for per-map spawn layout and metadata.</summary>
    public interface IMapStrategy
    {
        SessionCatalog.MapKind Kind { get; }
        string Label { get; }
        string Description { get; }
        Vector3 GetSpawnPosition(int index);
        float GetSpawnYaw(int index);
    }
}
