using FusionMultiplayer.Core;
using UnityEngine;

namespace FusionMultiplayer.Environment
{
    /// <summary>Procedurally builds distinct floor geometry and cover per map kind.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MapSceneMarker))]
    public sealed class MapEnvironmentBuilder : MonoBehaviour
    {
        private const string GeometryRootName = "MapGeometry";

        [SerializeField] private bool _rebuildInEditMode = true;

        private MapSceneMarker _marker;

        private void OnEnable()
        {
            _marker = GetComponent<MapSceneMarker>();
#if UNITY_EDITOR
            if (!Application.isPlaying && _rebuildInEditMode)
                Rebuild();
#endif
        }

        private void Awake()
        {
            if (_marker == null)
                _marker = GetComponent<MapSceneMarker>();
            Rebuild();
        }

        public void Rebuild()
        {
            if (_marker == null)
                return;

            ClearGeometry();
            switch (_marker.MapKind)
            {
                case SessionCatalog.MapKind.Plaza:
                    BuildPlaza();
                    break;
                case SessionCatalog.MapKind.Ruins:
                    BuildRuins();
                    break;
                default:
                    BuildArena();
                    break;
            }
        }

        private void ClearGeometry()
        {
            var existing = transform.Find(GeometryRootName);
            if (existing == null)
                return;

            if (Application.isPlaying)
                Destroy(existing.gameObject);
            else
                DestroyImmediate(existing.gameObject);
        }

        private Transform EnsureGeometryRoot()
        {
            var root = new GameObject(GeometryRootName).transform;
            root.SetParent(transform, false);
            return root;
        }

        private void BuildArena()
        {
            var root = EnsureGeometryRoot();
            const float halfExtent = 22f;
            const float fenceHeight = 3.5f;
            const float fenceThickness = 1.2f;
            var fenceColor = new Color(0.45f, 0.42f, 0.40f);
            var postColor = new Color(0.38f, 0.35f, 0.33f);

            // Plane default size is 10×10; scale 4.5 → 45×45 m play area.
            CreateGround(root, new Vector3(4.5f, 1f, 4.5f),
                new Color(0.55f, 0.52f, 0.50f), new Color(0.38f, 0.36f, 0.34f), "ArenaFloor");

            BuildArenaPerimeterFence(root, halfExtent, fenceHeight, fenceThickness, fenceColor, postColor);
            CreateMapLabel(root, "ARENA", new Vector3(0f, 0.05f, -halfExtent + 4f));
        }

        /// <summary>Full perimeter fence aligned with the arena floor edge.</summary>
        private static void BuildArenaPerimeterFence(Transform root, float halfExtent, float height, float thickness,
            Color wallColor, Color postColor)
        {
            var y = height * 0.5f;
            var edge = halfExtent - thickness * 0.5f;
            var span = halfExtent * 2f;

            CreateCoverWall(root, "Fence_N", new Vector3(0f, y, edge),
                new Vector3(span, height, thickness), wallColor);
            CreateCoverWall(root, "Fence_S", new Vector3(0f, y, -edge),
                new Vector3(span, height, thickness), wallColor);
            CreateCoverWall(root, "Fence_E", new Vector3(edge, y, 0f),
                new Vector3(thickness, height, span), wallColor);
            CreateCoverWall(root, "Fence_W", new Vector3(-edge, y, 0f),
                new Vector3(thickness, height, span), wallColor);

            var postSize = new Vector3(thickness * 1.35f, height * 1.05f, thickness * 1.35f);
            var postY = height * 0.525f;
            var corners = new[]
            {
                new Vector3(edge, postY, edge),
                new Vector3(-edge, postY, edge),
                new Vector3(edge, postY, -edge),
                new Vector3(-edge, postY, -edge)
            };
            for (var i = 0; i < corners.Length; i++)
                CreateCoverWall(root, $"FencePost_{i}", corners[i], postSize, postColor);

            // Low rim at floor level — blocks sliding under gaps at wall bases.
            const float rimHeight = 0.6f;
            var rimY = rimHeight * 0.5f;
            var rimSpan = span + thickness;
            CreateCoverWall(root, "FenceRim_N", new Vector3(0f, rimY, edge),
                new Vector3(rimSpan, rimHeight, thickness * 1.4f), wallColor);
            CreateCoverWall(root, "FenceRim_S", new Vector3(0f, rimY, -edge),
                new Vector3(rimSpan, rimHeight, thickness * 1.4f), wallColor);
            CreateCoverWall(root, "FenceRim_E", new Vector3(edge, rimY, 0f),
                new Vector3(thickness * 1.4f, rimHeight, rimSpan), wallColor);
            CreateCoverWall(root, "FenceRim_W", new Vector3(-edge, rimY, 0f),
                new Vector3(thickness * 1.4f, rimHeight, rimSpan), wallColor);
        }

        private void BuildPlaza()
        {
            var root = EnsureGeometryRoot();
            CreateGround(root, new Vector3(5.5f, 1f, 5.5f),
                new Color(0.72f, 0.70f, 0.66f), new Color(0.58f, 0.56f, 0.52f), "PlazaFloor");

            CreateCube(root, "CentralPlatform", new Vector3(0f, 0.35f, 0f),
                new Vector3(8f, 0.7f, 8f), new Color(0.62f, 0.60f, 0.56f)).isStatic = true;

            var columnColor = new Color(0.78f, 0.76f, 0.72f);
            var offsets = new[]
            {
                new Vector3(-14f, 0f, -14f), new Vector3(14f, 0f, -14f),
                new Vector3(-14f, 0f, 14f), new Vector3(14f, 0f, 14f),
                new Vector3(-14f, 0f, 0f), new Vector3(14f, 0f, 0f),
                new Vector3(0f, 0f, -14f), new Vector3(0f, 0f, 14f)
            };

            for (var i = 0; i < offsets.Length; i++)
            {
                CreateCylinder(root, $"Column_{i}", offsets[i] + new Vector3(0f, 2f, 0f),
                    1.1f, 4f, columnColor).isStatic = true;
            }

            CreateMapLabel(root, "PLAZA", new Vector3(0f, 0.05f, -20f));
        }

        private void BuildRuins()
        {
            var root = EnsureGeometryRoot();
            const float halfExtent = 20f;

            CreateGround(root, new Vector3(5f, 1f, 5f),
                new Color(0.42f, 0.38f, 0.32f), new Color(0.30f, 0.27f, 0.22f), "RuinsFloor");

            var wallColor = new Color(0.48f, 0.44f, 0.38f);
            var darkWall = new Color(0.38f, 0.34f, 0.30f);
            var rubbleColor = new Color(0.35f, 0.32f, 0.28f);
            var pillarColor = new Color(0.52f, 0.48f, 0.42f);

            BuildRuinsPerimeter(root, halfExtent, wallColor, darkWall);
            BuildRuinsCenter(root, pillarColor, wallColor);
            BuildRuinsQuadrantNw(root, wallColor, darkWall, rubbleColor);
            BuildRuinsQuadrantNe(root, wallColor, pillarColor);
            BuildRuinsQuadrantSw(root, wallColor, rubbleColor);
            BuildRuinsQuadrantSe(root, wallColor, darkWall, pillarColor);
            BuildRuinsMidLanes(root, wallColor, rubbleColor);

            CreateMapLabel(root, "RUINS", new Vector3(0f, 0.05f, -halfExtent - 2f));
        }

        private static void BuildRuinsPerimeter(Transform root, float halfExtent, Color wallColor, Color darkWall)
        {
            const float gap = 6f;
            float segment = (halfExtent * 2f - gap) * 0.5f;
            const float wallH = 2.6f;
            const float thick = 1.1f;
            var y = wallH * 0.5f;
            var zEdge = halfExtent - thick * 0.5f;
            var xEdge = halfExtent - thick * 0.5f;

            CreateCoverWall(root, "Perim_N_L", new Vector3(-segment * 0.5f - gap * 0.25f, y, zEdge),
                new Vector3(segment, wallH, thick), wallColor);
            CreateCoverWall(root, "Perim_N_R", new Vector3(segment * 0.5f + gap * 0.25f, y, zEdge),
                new Vector3(segment, wallH, thick), darkWall);
            CreateCoverWall(root, "Perim_S_L", new Vector3(-segment * 0.5f - gap * 0.25f, y * 0.75f, -zEdge),
                new Vector3(segment * 0.85f, wallH * 0.65f, thick), darkWall);
            CreateCoverWall(root, "Perim_S_R", new Vector3(segment * 0.5f + gap * 0.25f, y, -zEdge),
                new Vector3(segment, wallH, thick), wallColor);

            CreateCoverWall(root, "Perim_E_T", new Vector3(xEdge, y * 0.9f, segment * 0.5f + gap * 0.25f),
                new Vector3(thick, wallH * 0.85f, segment), wallColor);
            CreateCoverWall(root, "Perim_E_B", new Vector3(xEdge, y, -segment * 0.5f - gap * 0.25f),
                new Vector3(thick, wallH, segment), darkWall);
            CreateCoverWall(root, "Perim_W_T", new Vector3(-xEdge, y, segment * 0.5f + gap * 0.25f),
                new Vector3(thick, wallH, segment * 0.9f), darkWall);
            CreateCoverWall(root, "Perim_W_B", new Vector3(-xEdge, y * 0.8f, -segment * 0.5f - gap * 0.25f),
                new Vector3(thick, wallH * 0.7f, segment), wallColor);
        }

        private static void BuildRuinsCenter(Transform root, Color pillarColor, Color wallColor)
        {
            var pillarOffsets = new[]
            {
                new Vector3(-5f, 0f, 0f), new Vector3(5f, 0f, 0f),
                new Vector3(0f, 0f, -5f), new Vector3(0f, 0f, 5f)
            };
            for (var i = 0; i < pillarOffsets.Length; i++)
            {
                CreatePillar(root, $"CenterPillar_{i}", pillarOffsets[i] + new Vector3(0f, 1.6f, 0f),
                    0.75f, 3.2f, pillarColor);
            }

            CreateCoverWall(root, "CenterCross_N", new Vector3(0f, 0.65f, 2.5f),
                new Vector3(6f, 1.3f, 1f), wallColor);
            CreateCoverWall(root, "CenterCross_S", new Vector3(0f, 0.65f, -2.5f),
                new Vector3(6f, 1.3f, 1f), wallColor);
            CreateCoverWall(root, "CenterCross_E", new Vector3(2.5f, 0.65f, 0f),
                new Vector3(1f, 1.3f, 6f), wallColor);
            CreateCoverWall(root, "CenterCross_W", new Vector3(-2.5f, 0.65f, 0f),
                new Vector3(1f, 1.3f, 6f), wallColor);

            CreateCoverWall(root, "CenterAltar", new Vector3(0f, 0.45f, 0f),
                new Vector3(2.4f, 0.9f, 2.4f), new Color(0.44f, 0.40f, 0.36f));
        }

        private static void BuildRuinsQuadrantNw(Transform root, Color wallColor, Color darkWall, Color rubbleColor)
        {
            CreateElevatedPlatform(root, "NW_Platform", new Vector3(-11f, 1.25f, 11f),
                new Vector3(7f, 2.5f, 7f), wallColor);
            CreateRamp(root, "NW_Ramp", new Vector3(-7f, 0.55f, 7f),
                new Vector3(4f, 0.35f, 5f), 22f, darkWall);

            CreateLCover(root, "NW_LCover", new Vector3(-14f, 0f, 7f), wallColor, flipX: false);
            CreateCoverWall(root, "NW_Wall_A", new Vector3(-9f, 0.9f, 14f),
                new Vector3(8f, 1.8f, 1.2f), darkWall);
            CreateCoverWall(root, "NW_Wall_B", new Vector3(-15f, 0.7f, 3f),
                new Vector3(1.2f, 1.4f, 6f), wallColor);

            CreateRubbleCluster(root, "NW_Rubble", new Vector3(-5f, 0f, 12f), rubbleColor, 4);
        }

        private static void BuildRuinsQuadrantNe(Transform root, Color wallColor, Color pillarColor)
        {
            CreateCoverWall(root, "NE_Corridor_L", new Vector3(10f, 0.85f, 10f),
                new Vector3(1.2f, 1.7f, 10f), wallColor);
            CreateCoverWall(root, "NE_Corridor_R", new Vector3(14f, 0.85f, 10f),
                new Vector3(1.2f, 1.7f, 10f), wallColor);

            CreatePillar(root, "NE_Pillar_A", new Vector3(12f, 1.4f, 6f), 0.65f, 2.8f, pillarColor);
            CreatePillar(root, "NE_Pillar_B", new Vector3(8f, 1.4f, 14f), 0.65f, 2.8f, pillarColor);

            CreateElevatedPlatform(root, "NE_Sniper", new Vector3(15f, 1.75f, 14f),
                new Vector3(4f, 3.5f, 4f), new Color(0.46f, 0.42f, 0.37f));
            CreateStackedCrates(root, "NE_Crates", new Vector3(6f, 0f, 8f), wallColor);
        }

        private static void BuildRuinsQuadrantSw(Transform root, Color wallColor, Color rubbleColor)
        {
            CreateBrokenWall(root, "SW_Zig_A", new Vector3(-12f, 0.9f, -8f),
                new Vector3(7f, 1.8f, 1.1f), wallColor);
            CreateBrokenWall(root, "SW_Zig_B", new Vector3(-8f, 0.75f, -12f),
                new Vector3(1.1f, 1.5f, 7f), wallColor);
            CreateBrokenWall(root, "SW_Zig_C", new Vector3(-15f, 0.6f, -14f),
                new Vector3(5f, 1.2f, 1f), wallColor);

            CreateCoverWall(root, "SW_BarrierRow_0", new Vector3(-6f, 0.55f, -6f),
                new Vector3(2.5f, 1.1f, 1f), wallColor);
            CreateCoverWall(root, "SW_BarrierRow_1", new Vector3(-3f, 0.55f, -8f),
                new Vector3(2.5f, 1.1f, 1f), wallColor);
            CreateCoverWall(root, "SW_BarrierRow_2", new Vector3(-8f, 0.55f, -4f),
                new Vector3(1f, 1.1f, 2.5f), wallColor);

            CreateRubbleCluster(root, "SW_Rubble", new Vector3(-10f, 0f, -6f), rubbleColor, 5);
            CreatePillar(root, "SW_BrokenPillar", new Vector3(-14f, 0.9f, -4f), 0.55f, 1.8f,
                new Color(0.40f, 0.36f, 0.32f));
        }

        private static void BuildRuinsQuadrantSe(Transform root, Color wallColor, Color darkWall, Color pillarColor)
        {
            CreateElevatedPlatform(root, "SE_TowerBase", new Vector3(11f, 1.5f, -11f),
                new Vector3(6f, 3f, 6f), darkWall);
            CreatePillar(root, "SE_TowerPillar", new Vector3(11f, 3.4f, -11f), 0.9f, 2.2f, pillarColor);

            CreateSemicircleCover(root, "SE_Arc", new Vector3(7f, 0f, -14f), wallColor, faceNorth: true);
            CreateLCover(root, "SE_LCover", new Vector3(14f, 0f, -6f), wallColor, flipX: true);
            CreateStackedCrates(root, "SE_Crates", new Vector3(5f, 0f, -10f), wallColor);
        }

        private static void BuildRuinsMidLanes(Transform root, Color wallColor, Color rubbleColor)
        {
            var laneOffsets = new[]
            {
                new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, -10f),
                new Vector3(10f, 0f, 0f), new Vector3(-10f, 0f, 0f)
            };
            for (var i = 0; i < laneOffsets.Length; i++)
            {
                var pos = laneOffsets[i];
                var alongZ = Mathf.Abs(pos.z) > Mathf.Abs(pos.x);
                var size = alongZ ? new Vector3(3.5f, 1.2f, 1.1f) : new Vector3(1.1f, 1.2f, 3.5f);
                CreateCoverWall(root, $"MidLane_{i}", pos + new Vector3(0f, 0.6f, 0f), size, wallColor);
            }

            CreateRubbleCluster(root, "Mid_Rubble_A", new Vector3(4f, 0f, 4f), rubbleColor, 3);
            CreateRubbleCluster(root, "Mid_Rubble_B", new Vector3(-4f, 0f, -4f), rubbleColor, 3);
        }

        private static GameObject CreateCoverWall(Transform parent, string name, Vector3 position, Vector3 scale,
            Color color)
        {
            var wall = CreateCube(parent, name, position, scale, color);
            wall.isStatic = true;
            return wall;
        }

        private static GameObject CreatePillar(Transform parent, string name, Vector3 position, float radius,
            float height, Color color)
        {
            var pillar = CreateCylinder(parent, name, position, radius, height, color);
            pillar.isStatic = true;
            return pillar;
        }

        private static void CreateElevatedPlatform(Transform parent, string name, Vector3 center, Vector3 size,
            Color color)
        {
            CreateCoverWall(parent, name, center, size, color);
        }

        private static void CreateRamp(Transform parent, string name, Vector3 position, Vector3 scale, float pitchDeg,
            Color color)
        {
            var go = CreateCube(parent, name, position, scale, color);
            go.transform.localRotation = Quaternion.Euler(pitchDeg, 0f, 0f);
            go.isStatic = true;
        }

        private static void CreateLCover(Transform parent, string name, Vector3 corner, Color color, bool flipX)
        {
            var sx = flipX ? -1f : 1f;
            CreateCoverWall(parent, $"{name}_H", corner + new Vector3(2f * sx, 0.65f, 0f),
                new Vector3(4f, 1.3f, 1f), color);
            CreateCoverWall(parent, $"{name}_V", corner + new Vector3(0f, 0.65f, 2f),
                new Vector3(1f, 1.3f, 4f), color);
        }

        private static void CreateStackedCrates(Transform parent, string name, Vector3 basePos, Color color)
        {
            CreateCoverWall(parent, $"{name}_0", basePos + new Vector3(0f, 0.55f, 0f),
                new Vector3(1.4f, 1.1f, 1.4f), color);
            CreateCoverWall(parent, $"{name}_1", basePos + new Vector3(1.5f, 0.4f, 0f),
                new Vector3(1.2f, 0.8f, 1.2f), color);
            CreateCoverWall(parent, $"{name}_2", basePos + new Vector3(0.75f, 1.15f, 0f),
                new Vector3(1f, 0.7f, 1f), color);
        }

        private static void CreateRubbleCluster(Transform parent, string name, Vector3 center, Color color, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var angle = i * 72f * Mathf.Deg2Rad;
                var radius = 0.8f + (i % 3) * 0.35f;
                var offset = new Vector3(Mathf.Cos(angle) * radius, 0.25f + (i % 2) * 0.15f,
                    Mathf.Sin(angle) * radius);
                var scale = 0.7f + (i % 4) * 0.2f;
                var rubble = CreateCube(parent, $"{name}_{i}", center + offset, Vector3.one * scale, color);
                rubble.transform.localRotation = Quaternion.Euler(0f, i * 23f, i % 2 == 0 ? 8f : -6f);
                rubble.isStatic = true;
            }
        }

        private static void CreateSemicircleCover(Transform parent, string name, Vector3 center, Color color,
            bool faceNorth)
        {
            const int segments = 5;
            const float radius = 4f;
            const float arcDeg = 100f;
            var startAngle = faceNorth ? 40f : -140f;
            for (var i = 0; i < segments; i++)
            {
                var t = i / (float)(segments - 1);
                var angle = (startAngle + arcDeg * t) * Mathf.Deg2Rad;
                var pos = center + new Vector3(Mathf.Sin(angle) * radius, 0.65f, Mathf.Cos(angle) * radius);
                var yaw = (startAngle + arcDeg * t);
                var wall = CreateCube(parent, $"{name}_{i}", pos, new Vector3(1.4f, 1.3f, 1f), color);
                wall.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                wall.isStatic = true;
            }
        }

        private static void CreateGround(Transform parent, Vector3 scale, Color colorA, Color colorB, string name)
        {
            EnvironmentPieceSpawner.SpawnGround(parent, name, scale, colorA, colorB);
        }

        private static GameObject CreateBrokenWall(Transform parent, string name, Vector3 position, Vector3 scale,
            Color color)
        {
            var wall = CreateCube(parent, name, position, scale, color);
            wall.isStatic = true;
            return wall;
        }

        private static GameObject CreateCube(Transform parent, string name, Vector3 position, Vector3 scale,
            Color color)
        {
            return EnvironmentPieceSpawner.SpawnCube(parent, name, position, scale, color);
        }

        private static GameObject CreateCylinder(Transform parent, string name, Vector3 position, float radius,
            float height, Color color)
        {
            return EnvironmentPieceSpawner.SpawnCylinder(parent, name, position,
                new Vector3(radius * 2f, height * 0.5f, radius * 2f), color);
        }

        private static void CreateMapLabel(Transform parent, string label, Vector3 position)
        {
            var go = new GameObject($"MapLabel_{label}");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var text = go.AddComponent<TextMesh>();
            text.text = label;
            text.fontSize = 64;
            text.characterSize = 0.12f;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 1f, 1f, 0.35f);
        }
    }
}
