using Fusion;
using UnityEngine;

namespace FusionMultiplayer.Networking
{
    /// <summary>
    /// Server-authoritative 3D rigidbody sync (Fusion Physics addon stand-in for this project).
    /// Uses a real Unity Rigidbody and replicates pose/velocity to proxies.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public sealed class NetworkRigidbody3D : NetworkBehaviour
    {
        [Networked] private Vector3 NetPosition { get; set; }
        [Networked] private Quaternion NetRotation { get; set; }
        [Networked] private Vector3 NetVelocity { get; set; }
        [Networked] private Vector3 NetAngularVelocity { get; set; }

        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        public override void Spawned()
        {
            if (_rb == null)
                _rb = GetComponent<Rigidbody>();

            if (_rb == null)
                return;

            if (HasStateAuthority)
            {
                _rb.isKinematic = false;
                NetPosition = _rb.position;
                NetRotation = _rb.rotation;
            }
            else
            {
                _rb.isKinematic = true;
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (_rb == null)
                return;

            if (HasStateAuthority)
            {
                NetPosition = _rb.position;
                NetRotation = _rb.rotation;
                NetVelocity = _rb.linearVelocity;
                NetAngularVelocity = _rb.angularVelocity;
            }
            else
            {
                _rb.position = NetPosition;
                _rb.rotation = NetRotation;
                transform.SetPositionAndRotation(NetPosition, NetRotation);
            }
        }

        public Rigidbody Rigidbody => _rb;
    }
}
