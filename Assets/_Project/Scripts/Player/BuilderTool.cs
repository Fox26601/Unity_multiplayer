using Fusion;
using FusionMultiplayer.Core;
using FusionMultiplayer.Environment;
using UnityEngine;

namespace FusionMultiplayer.Player
{
    /// <summary>
    /// Minecraft-style block placement: crosshair raycast, 1 m grid snap, stacking (E / Q).
    /// No placement ghost — aim is cache-only for input.
    /// </summary>
    public class BuilderTool : NetworkBehaviour
    {
        private static readonly Vector3 OccupancyHalfExtents =
            Vector3.one * (BuildGrid.HalfTile - BuildGrid.OccupancyInset);

        [SerializeField] private NetworkObject _blockPrefab;
        [SerializeField] private float _rayDistance = 12f;

        private CharacterController _characterController;
        private Transform _cameraTransform;
        private NetworkButtons _previousButtons;

        private Vector3 _cachedPlacePosition;
        private bool _cachedPlaceValid;
        private bool _hasCachedPlace;

        private NetworkId _cachedRemoveTarget;
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
        }

        public override void FixedUpdateNetwork()
        {
            if (!GetInput(out GameplayNetworkInput input) || !HasInputAuthority)
                return;

            SessionRuntime.Refresh(Runner);

            if (!SessionRuntime.AllowsBuild)
            {
                _previousButtons = input.Buttons;
                return;
            }

            if (GameplayInputMode.IsGameplay)
                RefreshAimCache();
            else
                ClearAimCache();

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

        private void RefreshAimCache()
        {
            _hasCachedPlace = TryResolvePlacement(out _cachedPlacePosition);
            _cachedPlaceValid = _hasCachedPlace && CanPlaceAt(_cachedPlacePosition);

            _hasCachedRemoveTarget = false;
            _cachedRemoveValid = false;
            if (TryGetAimRay(out var ray) && TryGetAimedBlock(ray, out var block))
            {
                _cachedRemoveTarget = block.Object.Id;
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
            SessionRuntime.Refresh(Runner);
            if (!SessionRuntime.AllowsBuild)
                return;

            if (_blockPrefab == null || Runner == null || !Runner.IsRunning)
                return;

            if (IsCellOccupied(cellCenter))
                return;

            Runner.Spawn(_blockPrefab, cellCenter, Quaternion.identity, Object.InputAuthority);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcRemoveBlock(NetworkId blockId)
        {
            SessionRuntime.Refresh(Runner);
            if (!SessionRuntime.AllowsBuild)
                return;

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

            SessionRuntime.Refresh(Runner);

            if (SessionRuntime.AllowsBreakAnyBlock)
                return true;

            // Build: only the placer may remove their own blocks (Dedicated-safe).
            return blockObject.InputAuthority != PlayerRef.None &&
                   Object.InputAuthority == blockObject.InputAuthority;
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
    }
}
