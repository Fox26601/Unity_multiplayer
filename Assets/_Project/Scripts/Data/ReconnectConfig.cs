using UnityEngine;

namespace FusionMultiplayer.Data
{
    [CreateAssetMenu(fileName = "ReconnectConfig", menuName = "Fusion Multiplayer/Reconnect Config")]
    public sealed class ReconnectConfig : ScriptableObject
    {
        [SerializeField] private int _windowSeconds = 90;

        public int WindowSeconds => Mathf.Max(1, _windowSeconds);
    }
}
