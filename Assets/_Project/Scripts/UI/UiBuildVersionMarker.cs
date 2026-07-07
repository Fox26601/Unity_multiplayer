using UnityEngine;

namespace FusionMultiplayer.UI
{
    /// <summary>Invisible build version tag so runtime rebuild knows when to refresh UI.</summary>
    public sealed class UiBuildVersionMarker : MonoBehaviour
    {
        [SerializeField] private int _version;

        public int Version
        {
            get => _version;
            set => _version = value;
        }
    }
}
