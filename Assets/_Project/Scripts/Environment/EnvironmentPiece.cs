using UnityEngine;

namespace FusionMultiplayer.Environment
{
    /// <summary>
    /// Static map geometry prefab marker — collider on this object (or child) blocks projectiles.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnvironmentPiece : MonoBehaviour
    {
        [SerializeField] private bool _blocksProjectiles = true;

        public bool BlocksProjectiles => _blocksProjectiles;

        private void Awake() => EnsureSolidCollider();

#if UNITY_EDITOR
        private void OnValidate() => EnsureSolidCollider();
#endif

        private void EnsureSolidCollider()
        {
            var col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = false;
        }
    }
}
