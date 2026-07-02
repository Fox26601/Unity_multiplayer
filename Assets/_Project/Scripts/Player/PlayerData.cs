using Fusion;
using FusionMultiplayer.Core;
using UnityEngine;

namespace FusionMultiplayer.Player
{
    /// <summary>
    /// Per-player lobby/session profile replicated to all peers.
    /// </summary>
    public class PlayerData : NetworkBehaviour
    {
        [Networked] public NetworkString<_32> Nick { get; set; }
        [Networked] public Color Tint { get; set; }
        /// <summary>Selected character slot in game scene, or -1 if none.</summary>
        [Networked] public int CharacterIndex { get; set; }

        public override void Spawned()
        {
            CharacterIndex = -1;
            if (HasStateAuthority)
            {
                Nick = SessionData.Nickname;
                Tint = SessionData.Tint;
            }
        }
    }
}
