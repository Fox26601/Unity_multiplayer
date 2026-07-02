using Fusion;
using FusionMultiplayer.Environment;
using UnityEngine;

namespace FusionMultiplayer.Player
{
    /// <summary>
    /// Minecraft-style block placement: crosshair raycast, 1 m grid snap, stacking (E / Q).
    /// </summary>
    public class BuilderTool : NetworkBehaviour
    {
        private static readonly Vector3 OccupancyHalfExtents =
            Vector3.one * (BuildGrid.HalfTile - BuildGrid.OccupancyInset);

        [SerializeField] private NetworkObject _blockPrefab;
        [SerializeField] private float _rayDistance = 12f;

        private CharacterController _characterController;
        private Transform _cameraTransform;
        private GameObject _previewRoot;
        private Renderer _previewRenderer;
        private NetworkButtons _previousButtons;

        private Vector3 _cachedPlacePosition;
        private bool _cachedPlaceValid;
        private bool _hasCachedPlace;

        private NetworkId _cachedRemoveTarget;
        private Vector3 _cachedRemovePosition;
        private bool _hasCachedRemoveTarget;
        private bool _cachedRemoveValid;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _cameraTransform = transform.Find("PlayerCamera");
        }

        public override void Spawned()
        {
            _previousButtons = default;
            if (HasInputAuthority)
                EnsurePreviewGhost();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (_previewRoot != null)
                Destroy(_previewRoot);

            base.Despawned(runner, hasState);
        }

        public override void FixedUpdateNetwork()
        {
            if (!GetInput(out GameplayNetworkInput input) || !HasInputAuthority)
                return;

            if (GameplayInputMode.IsGameplay)
                RefreshAimCache();

            if (input.Buttons.WasPressed(_previousButtons, GameplayButton.Place)
                && _hasCachedPlace
                && _cachedPlaceValid)
            {
                RpcPlaceBlock(_cachedPlacePosition);
            }

            if (input.Buttons.WasPressed(_previousButtons, GameplayButton.Remove)
                && _hasCachedRemoveTarget
                && _cachedRemoveValid)
            {
                RpcRemoveBlock(_cachedRemoveTarget);
            }

            _previousButtons = input.Buttons;
        }

        private void LateUpdate()
        {
            if (!HasInputAuthority || !GameplayInputMode.IsGameplay)
            {
                ClearAimCache();
                SetPreviewVisible(false);
                return;
            }

            RefreshAimCache();

            if (ShouldShowPlacePreview())
            {
                SetPreviewVisible(true);
                _previewRoot.transform.position = _cachedPlacePosition;
                _previewRoot.transform.rotation = Quaternion.identity;
                _previewRoot.transform.localScale = Vector3.one * BuildGrid.TileSize;

                if (_previewRenderer != null)
                {
                    _previewRenderer.material.color = _cachedPlaceValid
                        ? new Color(0.35f, 0.95f, 0.45f, 0.42f)
                        : new Color(0.95f, 0.35f, 0.35f, 0.42f);
                }

                return;
            }

            if (_hasCachedRemoveTarget && _cachedRemoveValid)
            {
                SetPreviewVisible(true);
                _previewRoot.transform.position = _cachedRemovePosition;
                _previewRoot.transform.rotation = Quaternion.identity;
                _previewRoot.transform.localScale = Vector3.one * BuildGrid.TileSize;
                if (_previewRenderer != null)
                    _previewRenderer.material.color = new Color(0.95f, 0.35f, 0.35f, 0.5f);
                return;
            }

            SetPreviewVisible(false);
        }

        private bool ShouldShowPlacePreview()
        {
            if (!_hasCachedPlace)
                return false;

            if (!_hasCachedRemoveTarget)
                return true;

            return Vector3.SqrMagnitude(_cachedPlacePosition - _cachedRemovePosition)
                   > BuildGrid.HalfTile * BuildGrid.HalfTile * 0.25f;
        }

        private void RefreshAimCache()
        {
            _hasCachedPlace = TryResolvePlacement(out _cachedPlacePosition);
            _cachedPlaceValid = _hasCachedPlace && CanPlaceAt(_cachedPlacePosition);

            _hasCachedRemoveTarget = false;
            _cachedRemoveValid = false;
            if (TryGetAimRay(out var ray) && TryGetAimedBlock(ray, out var block))
            {
                _cachedRemoveTarget = block.Object.Id;
                _cachedRemovePosition = block.transform.position;
                _hasCachedRemoveTarget = true;
                _cachedRemoveValid = CanRemoveBlock(block.Object);
            }
        }

        private void ClearAimCache()
        {
            _hasCachedPlace = false;
            _cachedPlaceValid = false;
            _hasCachedRemoveTarget = false;
            _cachedRemoveValid = false;
            _cachedRemoveTarget = default;
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcPlaceBlock(Vector3 cellCenter)
        {
            if (_blockPrefab == null || Runner == null || !Runner.IsRunning)
                return;

            if (IsCellOccupied(cellCenter))
                return;

            Runner.Spawn(_blockPrefab, cellCenter, Quaternion.identity, Object.InputAuthority);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcRemoveBlock(NetworkId blockId)
        {
            if (Runner == null || !Runner.IsRunning)
                return;

            if (!Runner.TryFindObject(blockId, out var networkObject))
                return;

            if (networkObject.GetComponent<PlacedBlock>() == null)
                return;

            if (!CanRemoveBlock(networkObject))
                return;

            Runner.Despawn(networkObject);
        }

        private bool CanRemoveBlock(NetworkObject blockObject)
        {
            if (blockObject == null || !blockObject.IsValid || Runner == null || !Runner.IsRunning)
                return false;

            var hostPlayer = Runner.GetMasterClient();
            return blockObject.InputAuthority != hostPlayer;
        }

        private bool TryResolvePlacement(out Vector3 cellCenter)
        {
            cellCenter = default;
            if (!TryGetAimRay(out var ray))
                return false;

            if (!TryGetBuildSurfaceHit(ray, out var hit))
                return false;

            var block = hit.collider.GetComponentInParent<PlacedBlock>();
            if (block != null)
            {
                cellCenter = BuildGrid.GetAdjacentCellCenter(block.transform.position, hit.normal);
                return true;
            }

            cellCenter = BuildGrid.SnapHitToCellCenter(hit.point, hit.normal);
            return true;
        }

        private static bool TryGetAimedBlock(Ray ray, float distance, out PlacedBlock block)
        {
            block = null;
            var hits = Physics.RaycastAll(ray, distance, ~0, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0)
                return false;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<PlayerAvatar>() != null)
                    continue;

                var placedBlock = hit.collider.GetComponentInParent<PlacedBlock>();
                if (placedBlock == null || placedBlock.Object == null || !placedBlock.Object.IsValid)
                    continue;

                block = placedBlock;
                return true;
            }

            return false;
        }

        private bool TryGetAimedBlock(Ray ray, out PlacedBlock block)
        {
            return TryGetAimedBlock(ray, _rayDistance, out block);
        }

        private bool TryGetAimRay(out Ray ray)
        {
            ray = default;
            var cam = _cameraTransform != null ? _cameraTransform.GetComponent<Camera>() : null;
            if (cam == null)
                cam = Camera.main;

            if (cam == null)
                return false;

            ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            return true;
        }

        private bool TryGetBuildSurfaceHit(Ray ray, out RaycastHit bestHit)
        {
            bestHit = default;
            var hits = Physics.RaycastAll(ray, _rayDistance, ~0, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0)
                return false;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<PlayerAvatar>() != null)
                    continue;

                if (hit.collider.GetComponentInParent<PlacedBlock>() != null)
                {
                    bestHit = hit;
                    return true;
                }

                if (hit.collider.GetComponentInParent<CheckerboardGround>() != null
                    || hit.collider.gameObject.name == "Ground")
                {
                    bestHit = hit;
                    return true;
                }
            }

            return false;
        }

        private bool CanPlaceAt(Vector3 cellCenter)
        {
            if (IsCellOccupied(cellCenter))
                return false;

            return !IntersectsLocalPlayer(cellCenter);
        }

        private static bool IsCellOccupied(Vector3 center)
        {
            var overlaps = Physics.OverlapBox(center, OccupancyHalfExtents, Quaternion.identity,
                ~0, QueryTriggerInteraction.Ignore);

            foreach (var col in overlaps)
            {
                if (col.GetComponentInParent<PlacedBlock>() != null)
                    return true;
            }

            return false;
        }

        private bool IntersectsLocalPlayer(Vector3 cellCenter)
        {
            if (_characterController == null)
                return false;

            var playerFeetY = transform.position.y;
            var playerTopY = playerFeetY + _characterController.center.y + _characterController.height * 0.5f;
            var cellBottomY = cellCenter.y - BuildGrid.HalfTile;
            var cellTopY = cellCenter.y + BuildGrid.HalfTile;

            if (cellBottomY >= playerTopY - BuildGrid.OccupancyInset)
                return false;

            var dx = Mathf.Abs(cellCenter.x - transform.position.x);
            var dz = Mathf.Abs(cellCenter.z - transform.position.z);
            var horizontalOverlap = dx <= _characterController.radius + BuildGrid.HalfTile
                                    && dz <= _characterController.radius + BuildGrid.HalfTile;
            var verticalOverlap = cellBottomY < playerTopY && cellTopY > playerFeetY;

            return horizontalOverlap && verticalOverlap;
        }

        private void EnsurePreviewGhost()
        {
            if (_previewRoot != null)
                return;

            _previewRoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _previewRoot.name = "BlockPlacementPreview";
            Destroy(_previewRoot.GetComponent<Collider>());

            _previewRenderer = _previewRoot.GetComponent<Renderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Diffuse");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = new Color(0.35f, 0.95f, 0.45f, 0.42f);
                if (mat.HasProperty("_Surface"))
                    mat.SetFloat("_Surface", 1f);
                if (mat.HasProperty("_Blend"))
                    mat.SetFloat("_Blend", 0f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
                _previewRenderer.material = mat;
            }

            SetPreviewVisible(false);
        }

        private void SetPreviewVisible(bool visible)
        {
            if (_previewRoot != null)
                _previewRoot.SetActive(visible);
        }
    }
}
