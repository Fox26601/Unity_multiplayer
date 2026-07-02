using UnityEngine;

namespace FusionMultiplayer.Environment
{
    /// <summary>
    /// Procedural checkerboard material for the game floor — helps judge movement and distance.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class CheckerboardGround : MonoBehaviour
    {
        private const float PlaneMeshExtent = 10f;

        [SerializeField] private float _tileSizeMeters = 1f;
        [SerializeField] private Color _colorA = new(0.58f, 0.61f, 0.66f);
        [SerializeField] private Color _colorB = new(0.40f, 0.43f, 0.48f);
        [SerializeField] private int _pixelsPerTile = 32;

        private Material _materialInstance;
        private Texture2D _texture;

        private void OnEnable() => Apply();

#if UNITY_EDITOR
        private void OnValidate() => Apply();
#endif

        private void OnDestroy()
        {
            DestroyIfExists(_texture);
            _texture = null;
            DestroyIfExists(_materialInstance);
            _materialInstance = null;
        }

        private static void DestroyIfExists(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }

        private void Apply()
        {
            var renderer = GetComponent<MeshRenderer>();
            if (renderer == null) return;

            if (_tileSizeMeters < 0.25f) _tileSizeMeters = 0.25f;

            if (_materialInstance == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Standard")
                    ?? Shader.Find("Diffuse");
                if (shader == null) return;

                _materialInstance = new Material(shader)
                {
                    name = "CheckerboardGround (Runtime)"
                };
            }

            DestroyIfExists(_texture);
            _texture = BuildCheckerTexture();
            _materialInstance.mainTexture = _texture;
            _materialInstance.color = Color.white;

            if (_materialInstance.HasProperty("_BaseMap"))
                _materialInstance.SetTexture("_BaseMap", _texture);

            if (_materialInstance.HasProperty("_Metallic"))
                _materialInstance.SetFloat("_Metallic", 0f);
            if (_materialInstance.HasProperty("_Glossiness"))
                _materialInstance.SetFloat("_Glossiness", 0.12f);
            if (_materialInstance.HasProperty("_Smoothness"))
                _materialInstance.SetFloat("_Smoothness", 0.12f);

            var scale = transform.lossyScale;
            var tilesX = PlaneMeshExtent * Mathf.Abs(scale.x) / _tileSizeMeters;
            var tilesZ = PlaneMeshExtent * Mathf.Abs(scale.z) / _tileSizeMeters;
            var tiling = new Vector2(tilesX, tilesZ);
            _materialInstance.mainTextureScale = tiling;
            if (_materialInstance.HasProperty("_BaseMap"))
                _materialInstance.SetTextureScale("_BaseMap", tiling);

            renderer.sharedMaterial = _materialInstance;
        }

        private Texture2D BuildCheckerTexture()
        {
            var cell = Mathf.Max(4, _pixelsPerTile);
            var size = cell * 2;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "CheckerboardGround",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat
            };

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dark = (x / cell + y / cell) % 2 == 1;
                    tex.SetPixel(x, y, dark ? _colorB : _colorA);
                }
            }

            tex.Apply(false, true);
            return tex;
        }
    }
}
