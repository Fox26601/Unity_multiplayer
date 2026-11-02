using Fusion;
using FusionMultiplayer.Networking;
using UnityEngine;

namespace FusionMultiplayer.Player
{
    /// <summary>
    /// Server-spawned physics prop using NetworkRigidbody3D (assignment bonus).
    /// </summary>
    [RequireComponent(typeof(NetworkRigidbody3D))]
    public sealed class PhysicsProp : NetworkBehaviour
    {
        private const float LifetimeSeconds = 25f;

        [Networked] private TickTimer _life { get; set; }

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                _life = TickTimer.CreateFromSeconds(Runner, LifetimeSeconds);
                var nrb = GetComponent<NetworkRigidbody3D>();
                var rb = nrb != null ? nrb.Rigidbody : GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.AddForce(Random.insideUnitSphere * 4f + Vector3.up * 2f, ForceMode.Impulse);
                }
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            if (_life.Expired(Runner))
                Runner.Despawn(Object);
        }
    }
}
