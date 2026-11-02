using System.IO;
using Fusion;
using FusionMultiplayer.Core;
using FusionMultiplayer.Data;
using UnityEditor;
using UnityEngine;

namespace FusionMultiplayer.EditorTools
{
    /// <summary>Creates default ScriptableObject config assets under Assets/_Project.</summary>
    public static class GameConfigsGenerator
    {
        private const string ConfigsRoot = "Assets/_Project/Configs";
        private const string ResourcesConfigs = "Assets/_Project/Resources/Configs";

        [MenuItem("Tools/Fusion Multiplayer/Create Default Config Assets")]
        public static void CreateDefaultConfigAssets()
        {
            EnsureFolder("Assets/_Project");
            EnsureFolder(ConfigsRoot);
            EnsureFolder("Assets/_Project/Resources");
            EnsureFolder(ResourcesConfigs);

            var match = CreateOrLoad<MatchConfig>($"{ConfigsRoot}/MatchConfig.asset");
            var combat = CreateOrLoad<CombatConfig>($"{ConfigsRoot}/CombatConfig.asset");
            var reconnect = CreateOrLoad<ReconnectConfig>($"{ConfigsRoot}/ReconnectConfig.asset");
            var playerStats = CreateOrLoad<PlayerStatsConfig>($"{ConfigsRoot}/PlayerStatsConfig.asset");
            var bot = CreateOrLoad<BotConfig>($"{ConfigsRoot}/BotConfig.asset");
            var difficulty = CreateOrLoad<DifficultyCatalog>($"{ConfigsRoot}/DifficultyCatalog.asset");
            var uiTheme = CreateOrLoad<UiThemeConfig>($"{ConfigsRoot}/UiThemeConfig.asset");

            var mapArena = CreateMap(SessionCatalog.MapKind.Arena, "Arena",
                "Walled combat arena with circular spawns.");
            var mapPlaza = CreateMap(SessionCatalog.MapKind.Plaza, "Plaza",
                "Open plaza with columns and a central platform.");
            var mapRuins = CreateMap(SessionCatalog.MapKind.Ruins, "Ruins",
                "Broken walls, rubble, and elevated platforms.");

            var maps = CreateOrLoad<MapCatalog>($"{ConfigsRoot}/MapCatalog.asset");
            SetPrivate(maps, "_arena", mapArena);
            SetPrivate(maps, "_plaza", mapPlaza);
            SetPrivate(maps, "_ruins", mapRuins);
            EditorUtility.SetDirty(maps);

            var modeBuild = CreateMode(SessionCatalog.GameModeKind.Build, "Build",
                "Place and remove blocks (E / Q). No weapons.", true, false, false);
            var modeCombat = CreateMode(SessionCatalog.GameModeKind.Combat, "Combat",
                "Shoot projectiles (LMB). Score hidden until game ends.", false, true, false);
            var modeSandbox = CreateMode(SessionCatalog.GameModeKind.Sandbox, "Sandbox",
                "Build, break other players' blocks (Q), and shoot (LMB).", true, true, true);

            var modes = CreateOrLoad<GameModeCatalog>($"{ConfigsRoot}/GameModeCatalog.asset");
            SetPrivate(modes, "_build", modeBuild);
            SetPrivate(modes, "_combat", modeCombat);
            SetPrivate(modes, "_sandbox", modeSandbox);
            EditorUtility.SetDirty(modes);

            var prefabs = CreateOrLoad<NetworkPrefabCatalog>($"{ConfigsRoot}/NetworkPrefabCatalog.asset");
            WirePrefabs(prefabs);
            EditorUtility.SetDirty(prefabs);

            var hub = CreateOrLoad<GameConfigs>($"{ResourcesConfigs}/GameConfigs.asset");
            SetPrivate(hub, "_match", match);
            SetPrivate(hub, "_combat", combat);
            SetPrivate(hub, "_reconnect", reconnect);
            SetPrivate(hub, "_playerStats", playerStats);
            SetPrivate(hub, "_bot", bot);
            SetPrivate(hub, "_difficulty", difficulty);
            SetPrivate(hub, "_maps", maps);
            SetPrivate(hub, "_modes", modes);
            SetPrivate(hub, "_uiTheme", uiTheme);
            SetPrivate(hub, "_prefabs", prefabs);
            EditorUtility.SetDirty(hub);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[FusionMultiplayer] Default config assets created under Assets/_Project/Configs and Resources/Configs/GameConfigs.");
        }

        private static MapDefinition CreateMap(SessionCatalog.MapKind kind, string label, string description)
        {
            var path = $"{ConfigsRoot}/Map_{kind}.asset";
            var map = CreateOrLoad<MapDefinition>(path);
            var positions = new Vector3[MapDefinition.SpawnSlotCount];
            var yaws = new float[MapDefinition.SpawnSlotCount];
            for (var i = 0; i < MapDefinition.SpawnSlotCount; i++)
            {
                positions[i] = ComputeSpawnPosition(kind, i);
                yaws[i] = Mathf.Atan2(-positions[i].x, -positions[i].z) * Mathf.Rad2Deg;
            }

            map.EditorSetSpawns(kind, label, description, positions, yaws);
            EditorUtility.SetDirty(map);
            return map;
        }

        private static GameModeDefinition CreateMode(SessionCatalog.GameModeKind kind, string label,
            string description, bool build, bool combat, bool breakOthers)
        {
            var path = $"{ConfigsRoot}/Mode_{kind}.asset";
            var mode = CreateOrLoad<GameModeDefinition>(path);
            mode.EditorSet(kind, label, description, build, combat, breakOthers);
            EditorUtility.SetDirty(mode);
            return mode;
        }

        private static Vector3 ComputeSpawnPosition(SessionCatalog.MapKind map, int index)
        {
            switch (map)
            {
                case SessionCatalog.MapKind.Plaza:
                    if (index < 4)
                        return new Vector3(-16f + index * 10f, 0f, -18f);
                    if (index < 8)
                        return new Vector3(-16f + (index - 4) * 10f, 0f, 18f);
                    return new Vector3(index == 8 ? -18f : 18f, 0f, 0f);
                case SessionCatalog.MapKind.Ruins:
                {
                    var angle = (index * 37f + 10f) * Mathf.Deg2Rad;
                    var radius = 13f + (index % 3) * 1.5f;
                    return new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
                }
                default:
                {
                    var angle = index * 36f * Mathf.Deg2Rad;
                    const float radius = 12f;
                    return new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
                }
            }
        }

        private static void WirePrefabs(NetworkPrefabCatalog catalog)
        {
            SetPrivate(catalog, "_playerAvatar",
                AssetDatabase.LoadAssetAtPath<NetworkObject>("Assets/_Project/Prefabs/PlayerAvatar.prefab"));
            SetPrivate(catalog, "_playerData",
                AssetDatabase.LoadAssetAtPath<NetworkObject>("Assets/_Project/Prefabs/PlayerData.prefab"));
            SetPrivate(catalog, "_projectile",
                AssetDatabase.LoadAssetAtPath<NetworkObject>("Assets/_Project/Prefabs/Projectile.prefab"));
            SetPrivate(catalog, "_physicsProp",
                AssetDatabase.LoadAssetAtPath<NetworkObject>("Assets/_Project/Prefabs/PhysicsProp.prefab"));
            SetPrivate(catalog, "_placedBlock",
                AssetDatabase.LoadAssetAtPath<NetworkObject>("Assets/_Project/Prefabs/PlacedBlock.prefab"));
            SetPrivate(catalog, "_envCube",
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Resources/Environment/EnvPiece_Cube.prefab"));
            SetPrivate(catalog, "_envCylinder",
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_Project/Resources/Environment/EnvPiece_Cylinder.prefab"));
            SetPrivate(catalog, "_envGround",
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_Project/Resources/Environment/EnvPiece_Ground.prefab"));
        }

        private static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void SetPrivate(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[FusionMultiplayer] Missing field {fieldName} on {target.name}");
                return;
            }

            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
                return;

            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
