using Fusion;
using UnityEngine;

namespace FusionMultiplayer.Data
{
    [CreateAssetMenu(fileName = "NetworkPrefabCatalog", menuName = "Fusion Multiplayer/Network Prefab Catalog")]
    public sealed class NetworkPrefabCatalog : ScriptableObject
    {
        [SerializeField] private NetworkObject _playerAvatar;
        [SerializeField] private NetworkObject _playerData;
        [SerializeField] private NetworkObject _projectile;
        [SerializeField] private NetworkObject _physicsProp;
        [SerializeField] private NetworkObject _placedBlock;
        [SerializeField] private GameObject _envCube;
        [SerializeField] private GameObject _envCylinder;
        [SerializeField] private GameObject _envGround;

        public NetworkObject PlayerAvatar => _playerAvatar;
        public NetworkObject PlayerData => _playerData;
        public NetworkObject Projectile => _projectile;
        public NetworkObject PhysicsProp => _physicsProp;
        public NetworkObject PlacedBlock => _placedBlock;
        public GameObject EnvCube => _envCube;
        public GameObject EnvCylinder => _envCylinder;
        public GameObject EnvGround => _envGround;

        public bool HasCoreNetworkPrefabs =>
            _playerAvatar != null && _playerData != null && _projectile != null && _physicsProp != null;
    }
}
