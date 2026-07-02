using Fusion;
using UnityEngine;

namespace FusionMultiplayer.Player
{
    /// <summary>
    /// Simple networked block spawned/despawned by players.
    /// </summary>
    [RequireComponent(typeof(NetworkTransform))]
    public class PlacedBlock : NetworkBehaviour
    {
    }
}
