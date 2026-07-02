using UnityEngine;

namespace FusionMultiplayer.Environment
{
    /// <summary>World grid aligned with <see cref="CheckerboardGround"/> (1 m cells).</summary>
    public static class BuildGrid
    {
        public const float TileSize = 1f;
        public const float HalfTile = TileSize * 0.5f;
        public const float OccupancyInset = 0.02f;

        public static Vector3 SnapHitToCellCenter(Vector3 hitPoint, Vector3 hitNormal)
        {
            var inset = hitPoint + hitNormal.normalized * (HalfTile + OccupancyInset);
            return SnapPointToCellCenter(inset);
        }

        public static Vector3 GetAdjacentCellCenter(Vector3 blockCenter, Vector3 hitNormal)
        {
            return blockCenter + SnapNormalToAxis(hitNormal) * TileSize;
        }

        public static Vector3 SnapNormalToAxis(Vector3 normal)
        {
            var abs = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
            if (abs.x >= abs.y && abs.x >= abs.z)
                return new Vector3(Mathf.Sign(normal.x), 0f, 0f);
            if (abs.y >= abs.z)
                return new Vector3(0f, Mathf.Sign(normal.y), 0f);
            return new Vector3(0f, 0f, Mathf.Sign(normal.z));
        }

        public static Vector3 SnapPointToCellCenter(Vector3 worldPoint)
        {
            return new Vector3(
                FloorToCellCenter(worldPoint.x),
                FloorToCellCenter(worldPoint.y),
                FloorToCellCenter(worldPoint.z));
        }

        public static Vector3 CellIndexToCenter(Vector3Int index)
        {
            return new Vector3(
                index.x * TileSize,
                index.y * TileSize + HalfTile,
                index.z * TileSize);
        }

        public static Vector3Int CenterToCellIndex(Vector3 center)
        {
            return new Vector3Int(
                Mathf.RoundToInt(center.x / TileSize),
                Mathf.RoundToInt((center.y - HalfTile) / TileSize),
                Mathf.RoundToInt(center.z / TileSize));
        }

        private static float FloorToCellCenter(float axis)
        {
            return Mathf.Floor(axis / TileSize) * TileSize + HalfTile;
        }
    }
}
