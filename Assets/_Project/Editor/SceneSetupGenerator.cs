#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fusion;
using FusionMultiplayer.Chat;
using FusionMultiplayer.Core;
using FusionMultiplayer.Environment;
using FusionMultiplayer.Networking;
using FusionMultiplayer.Player;
using FusionMultiplayer.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace FusionMultiplayer.EditorTools
{
    /// <summary>
    /// One-click scene + prefab setup (requires Photon Fusion SDK imported first).
    /// </summary>
    public static class SceneSetupGenerator
    {
        private const string Root = "Assets/_Project";
        private const string ScenesPath = Root + "/Scenes";
        private const string PrefabsPath = Root + "/Prefabs";

        [MenuItem("Tools/Fusion Multiplayer/Generate Map Scenes Only")]
        public static void GenerateMapScenesOnly()
        {
            Directory.CreateDirectory(ScenesPath);
            CreateEnvironmentPrefabs();
            CreateGameScene(SessionCatalog.MapKind.Arena, "02_Game_Arena");
            CreateGameScene(SessionCatalog.MapKind.Plaza, "03_Game_Plaza");
            CreateGameScene(SessionCatalog.MapKind.Ruins, "04_Game_Ruins");

            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene($"{ScenesPath}/00_MainMenu.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/01_Lobby.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/02_Game_Arena.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/03_Game_Plaza.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/04_Game_Ruins.unity", true)
            };

            var existing = EditorBuildSettings.scenes;
            if (existing != null && existing.Length > 0)
            {
                if (!existing[0].path.EndsWith("00_MainMenu.unity"))
                    scenes[0] = existing[0];
                if (existing.Length > 1 && !existing[1].path.EndsWith("01_Lobby.unity"))
                    scenes[1] = existing[1];
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Fusion Multiplayer: Arena, Plaza, and Ruins map scenes generated.");
        }

        [MenuItem("Tools/Fusion Multiplayer/Generate Scenes, Prefabs, And Build Settings")]
        public static void GenerateAll()
        {
            Directory.CreateDirectory(ScenesPath);
            Directory.CreateDirectory(PrefabsPath);

            CreateEnvironmentPrefabs();
            CreatePlayerLocomotionController();
            CreatePlayerDataPrefab();
            CreatePlacedBlockPrefab();
            CreateProjectilePrefab();
            CreatePhysicsPropPrefab();
            CreatePlayerAvatarPrefab();
            CreateBootScene();

            CreateMainMenuScene();
            CreateLobbyScene();
            CreateGameScene(SessionCatalog.MapKind.Arena, "02_Game_Arena");
            CreateGameScene(SessionCatalog.MapKind.Plaza, "03_Game_Plaza");
            CreateGameScene(SessionCatalog.MapKind.Ruins, "04_Game_Ruins");

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene($"{ScenesPath}/00_Boot.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/00_MainMenu.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/01_Lobby.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/02_Game_Arena.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/03_Game_Plaza.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/04_Game_Ruins.unity", true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Fusion Multiplayer: scenes and prefabs generated. Register prefabs in Fusion NetworkProjectConfig.");
        }

        [MenuItem("Tools/Fusion Multiplayer/Generate Environment Prefabs")]
        public static void GenerateEnvironmentPrefabsMenu()
        {
            CreateEnvironmentPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Fusion Multiplayer: Environment prefabs generated under Assets/_Project/Resources/Environment/");
        }

        /// <summary>Unity batchmode entry point.</summary>
        public static void GenerateEnvironmentPrefabsBatch()
        {
            GenerateEnvironmentPrefabsMenu();
        }

        [MenuItem("Tools/Fusion Multiplayer/Wire PhysicsProp And Boot Only")]
        public static void WirePhysicsPropAndBootOnly()
        {
            Directory.CreateDirectory(PrefabsPath);
            CreatePhysicsPropPrefab();
            CreateBootScene();

            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene($"{ScenesPath}/00_Boot.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/00_MainMenu.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/01_Lobby.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/02_Game_Arena.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/03_Game_Plaza.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/04_Game_Ruins.unity", true)
            };
            EditorBuildSettings.scenes = scenes.ToArray();

            WirePhysicsPropIntoOpenGameScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Fusion Multiplayer: PhysicsProp + 00_Boot wired (animator not touched).");
        }

        private static void WirePhysicsPropIntoOpenGameScenes()
        {
            var physicsProp = AssetDatabase.LoadAssetAtPath<NetworkObject>($"{PrefabsPath}/PhysicsProp.prefab");
            if (physicsProp == null)
                return;

            var mapScenes = new[]
            {
                $"{ScenesPath}/02_Game_Arena.unity",
                $"{ScenesPath}/03_Game_Plaza.unity",
                $"{ScenesPath}/04_Game_Ruins.unity",
                $"{ScenesPath}/02_Game.unity"
            };

            foreach (var path in mapScenes)
            {
                if (!System.IO.File.Exists(path))
                    continue;

                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var gm = Object.FindFirstObjectByType<GameManager>();
                if (gm != null)
                {
                    var so = new SerializedObject(gm);
                    var prop = so.FindProperty("_physicsPropPrefab");
                    if (prop != null)
                    {
                        prop.objectReferenceValue = physicsProp;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        EditorSceneManager.MarkSceneDirty(scene);
                        EditorSceneManager.SaveScene(scene);
                    }
                }
            }
        }

        [MenuItem("Tools/Fusion Multiplayer/Regenerate Combat Prefabs")]
        public static void RegenerateCombatPrefabs()
        {
            Directory.CreateDirectory(PrefabsPath);
            // Animator / locomotion intentionally skipped in remaining-work pass.
            CreateProjectilePrefab();
            CreatePhysicsPropPrefab();
            CreatePlayerAvatarPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Fusion Multiplayer: Combat prefabs regenerated (Projectile, PhysicsProp, PlayerAvatar).");
        }

        /// <summary>Unity batchmode entry point.</summary>
        public static void RegenerateCombatPrefabsBatch()
        {
            RegenerateCombatPrefabs();
        }

        private static void CreateProjectilePrefab()
        {
            var go = new GameObject("Projectile");
            go.AddComponent<NetworkObject>();
            go.AddComponent<NetworkTransform>();
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.18f;
            go.AddComponent<Projectile>();

            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = Vector3.one * 0.28f;
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(1f, 0.85f, 0.2f, 1f);

            PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabsPath}/Projectile.prefab");
            Object.DestroyImmediate(go);
        }

        private static void CreatePlayerDataPrefab()
        {
            var go = new GameObject("PlayerData");
            go.AddComponent<NetworkObject>();
            go.AddComponent<PlayerData>();
            PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabsPath}/PlayerData.prefab");
            Object.DestroyImmediate(go);
        }

        private static void CreatePlacedBlockPrefab()
        {
            var go = new GameObject("PlacedBlock");
            go.AddComponent<NetworkObject>();
            go.AddComponent<NetworkTransform>();
            go.AddComponent<PlacedBlock>();
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(go.transform, false);
            cube.transform.localScale = Vector3.one * BuildGrid.TileSize;
            cube.name = "Visual";
            PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabsPath}/PlacedBlock.prefab");
            Object.DestroyImmediate(go);
        }

        private static void CreateEnvironmentPrefabs()
        {
            var envPath = Root + "/Resources/Environment";
            Directory.CreateDirectory(envPath);

            CreateEnvPiecePrefab(PrimitiveType.Cube, $"{envPath}/EnvPiece_Cube.prefab", addCheckerboard: false);
            CreateEnvPiecePrefab(PrimitiveType.Cylinder, $"{envPath}/EnvPiece_Cylinder.prefab",
                addCheckerboard: false);
            CreateEnvPiecePrefab(PrimitiveType.Plane, $"{envPath}/EnvPiece_Ground.prefab", addCheckerboard: true);
        }

        private static void CreateEnvPiecePrefab(PrimitiveType primitive, string assetPath, bool addCheckerboard)
        {
            var go = GameObject.CreatePrimitive(primitive);
            go.name = Path.GetFileNameWithoutExtension(assetPath);
            go.isStatic = true;

            var col = go.GetComponent<Collider>();
            if (col != null)
                col.isTrigger = false;

            go.AddComponent<EnvironmentPiece>();
            if (addCheckerboard)
                go.AddComponent<CheckerboardGround>();

            PrefabUtility.SaveAsPrefabAsset(go, assetPath);
            Object.DestroyImmediate(go);
        }

        private static void CreatePlayerAvatarPrefab()
        {
            var go = new GameObject("PlayerAvatar");
            go.AddComponent<NetworkObject>();
            go.AddComponent<NetworkTransform>();
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            go.AddComponent<PlayerAvatar>();
            go.AddComponent<PlayerMovement>();
            go.AddComponent<PlayerLook>();
            go.AddComponent<BuilderTool>();
            go.AddComponent<PlayerWeapon>();
            go.AddComponent<PlayerAnimationSync>();
            go.AddComponent<NetworkMecanimAnimator>();

            var hitboxGo = new GameObject("Hitbox");
            hitboxGo.transform.SetParent(go.transform, false);
            hitboxGo.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            var hitbox = hitboxGo.AddComponent<CapsuleCollider>();
            hitbox.isTrigger = true;
            hitbox.radius = 0.4f;
            hitbox.height = 1.8f;
            hitbox.direction = 1;

            var vis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            vis.name = "Body";
            vis.transform.SetParent(go.transform, false);
            vis.transform.localPosition = new Vector3(0f, 1f, 0f);
            Object.DestroyImmediate(vis.GetComponent<CapsuleCollider>());
            var anim = vis.AddComponent<Animator>();
            var controller =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    $"{Root}/Resources/PlayerLocomotion.controller");
            if (controller != null)
                anim.runtimeAnimatorController = controller;

            var netAnim = go.GetComponent<NetworkMecanimAnimator>();
            var naso = new SerializedObject(netAnim);
            var animProp = naso.FindProperty("Animator");
            if (animProp != null)
                animProp.objectReferenceValue = anim;
            naso.ApplyModifiedPropertiesWithoutUndo();

            var tagGo = new GameObject("NameTag");
            tagGo.transform.SetParent(go.transform, false);
            tagGo.transform.localPosition = new Vector3(0f, 2.1f, 0f);
            tagGo.AddComponent<TextMesh>();
            tagGo.AddComponent<NameTag>();

            var cam = new GameObject("PlayerCamera");
            cam.transform.SetParent(go.transform, false);
            cam.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var camComp = cam.AddComponent<Camera>();
            cam.AddComponent<AudioListener>();

            var pa = go.GetComponent<PlayerAvatar>();
            var so = new SerializedObject(pa);
            so.FindProperty("_bodyRenderer").objectReferenceValue = vis.GetComponent<Renderer>();
            so.FindProperty("_nameTag").objectReferenceValue = tagGo.GetComponent<NameTag>();
            so.ApplyModifiedPropertiesWithoutUndo();

            var builder = go.GetComponent<BuilderTool>();
            var blockPrefab = AssetDatabase.LoadAssetAtPath<NetworkObject>($"{PrefabsPath}/PlacedBlock.prefab");
            var bso = new SerializedObject(builder);
            bso.FindProperty("_blockPrefab").objectReferenceValue = blockPrefab;
            bso.ApplyModifiedPropertiesWithoutUndo();

            var weapon = go.GetComponent<PlayerWeapon>();
            var projectilePrefab = AssetDatabase.LoadAssetAtPath<NetworkObject>($"{PrefabsPath}/Projectile.prefab");
            var wso = new SerializedObject(weapon);
            wso.FindProperty("_projectilePrefab").objectReferenceValue = projectilePrefab;
            wso.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabsPath}/PlayerAvatar.prefab");
            Object.DestroyImmediate(go);
        }

        private static void CreatePhysicsPropPrefab()
        {
            var go = new GameObject("PhysicsProp");
            go.AddComponent<NetworkObject>();
            go.AddComponent<NetworkTransform>();
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 2f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            go.AddComponent<NetworkRigidbody3D>();
            go.AddComponent<PhysicsProp>();
            var col = go.AddComponent<SphereCollider>();
            col.radius = 0.45f;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = Vector3.one * 0.9f;
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.2f, 0.75f, 1f, 1f);

            PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabsPath}/PhysicsProp.prefab");
            Object.DestroyImmediate(go);
        }

        private static void CreatePlayerLocomotionController()
        {
            Directory.CreateDirectory($"{Root}/Resources");
            var path = $"{Root}/Resources/PlayerLocomotion.controller";
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
                return;

            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Shoot", AnimatorControllerParameterType.Bool);

            var root = controller.layers[0].stateMachine;
            var idle = root.AddState("Idle");
            var run = root.AddState("Run");
            var air = root.AddState("Air");
            root.defaultState = idle;

            var idleToRun = idle.AddTransition(run);
            idleToRun.AddCondition(AnimatorConditionMode.Greater, 0.15f, "Speed");
            idleToRun.hasExitTime = false;

            var runToIdle = run.AddTransition(idle);
            runToIdle.AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed");
            runToIdle.hasExitTime = false;

            var anyToAir = root.AddAnyStateTransition(air);
            anyToAir.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
            anyToAir.hasExitTime = false;

            var airToIdle = air.AddTransition(idle);
            airToIdle.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
            airToIdle.AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed");
            airToIdle.hasExitTime = false;

            var airToRun = air.AddTransition(run);
            airToRun.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
            airToRun.AddCondition(AnimatorConditionMode.Greater, 0.15f, "Speed");
            airToRun.hasExitTime = false;

            AssetDatabase.SaveAssets();
        }

        private static void CreateMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Directional Light", typeof(Light));
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var menuCam = new GameObject("MenuCamera", typeof(Camera));
            menuCam.tag = "MainCamera";

            var connGo = new GameObject("ConnectionManager");
            connGo.AddComponent<NetworkSceneManagerDefault>();
            connGo.AddComponent<ConnectionManager>();
            var pdPrefab = AssetDatabase.LoadAssetAtPath<NetworkObject>($"{PrefabsPath}/PlayerData.prefab");
            var cso = new SerializedObject(connGo.GetComponent<ConnectionManager>());
            cso.FindProperty("_playerDataPrefab").objectReferenceValue = pdPrefab;
            cso.ApplyModifiedPropertiesWithoutUndo();

            var canvas = UiSceneLayout.CreateCanvas("MainMenuCanvas");
            var menu = canvas.gameObject.AddComponent<MainMenuUI>();
            var sessionFlow = canvas.gameObject.AddComponent<SessionFlowUI>();
            var panel = UiSceneLayout.CreateUIPanel(canvas.transform, "Panel");
            UiSceneLayout.CreateTitle(panel.transform, "Title", UiCopy.MainMenuTitle);
            UiSceneLayout.CreateSubtitle(panel.transform, "Subtitle", UiCopy.MainMenuSubtitle);

            var nick = UiSceneLayout.CreateInputField(panel.transform, "NicknameField", "Your nickname", new Vector2(560f, 64f));
            var room = UiSceneLayout.CreateInputField(panel.transform, "RoomField", "Room name (same for all players)", new Vector2(560f, 64f));
            var preview = UiSceneLayout.CreateImage(panel.transform, "ColorPreview", 80f, 80f);
            var randomColor = UiSceneLayout.CreateButton(panel.transform, "BtnRandomColor", "Random player color", new Vector2(320f, 64f));
            var create = UiSceneLayout.CreateButton(panel.transform, "BtnCreate", "Create / Host room", new Vector2(400f, 72f));
            var join = UiSceneLayout.CreateButton(panel.transform, "BtnJoin", "Join room", new Vector2(400f, 72f));
            var status = UiSceneLayout.CreateStatusLine(panel.transform, "SessionStatus", string.Empty);
            UiSceneLayout.ConfigureMainMenuPanel(panel.transform, nick, room, preview, randomColor, create, join);

            var sfso = new SerializedObject(sessionFlow);
            sfso.FindProperty("_statusText").objectReferenceValue = status;
            sfso.ApplyModifiedPropertiesWithoutUndo();

            var mso = new SerializedObject(menu);
            mso.FindProperty("_nicknameField").objectReferenceValue = nick;
            mso.FindProperty("_roomField").objectReferenceValue = room;
            mso.FindProperty("_createButton").objectReferenceValue = create;
            mso.FindProperty("_joinButton").objectReferenceValue = join;
            mso.FindProperty("_colorPreview").objectReferenceValue = preview;
            mso.FindProperty("_randomColorButton").objectReferenceValue = randomColor;
            mso.FindProperty("_sessionFlow").objectReferenceValue = sessionFlow;
            mso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, $"{ScenesPath}/00_MainMenu.unity");
        }

        private static void CreateLobbyScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Directional Light", typeof(Light));
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var cam = new GameObject("LobbyCamera", typeof(Camera));
            cam.tag = "MainCamera";
            cam.transform.position = new Vector3(0f, 6f, -8f);
            cam.transform.rotation = Quaternion.Euler(25f, 0f, 0f);

            var canvas = UiSceneLayout.CreateCanvas("LobbyCanvas");
            canvas.gameObject.AddComponent<SessionFlowUI>();
            var lobby = canvas.gameObject.AddComponent<LobbyUI>();
            var panel = UiSceneLayout.CreateUIPanel(canvas.transform, "Panel");
            UiSceneLayout.CreateTitle(panel.transform, "Title", UiCopy.LobbyTitle);
            var listText = UiSceneLayout.CreateText(panel.transform, "PlayerList", UiTypography.Body, TextAlignmentOptions.TopLeft);
            var status = UiSceneLayout.CreateText(panel.transform, "Status", UiTypography.Caption, TextAlignmentOptions.MidlineLeft);
            var start = UiSceneLayout.CreateButton(panel.transform, "BtnStart", "START GAME (master only)", new Vector2(560f, 80f));
            UiSceneLayout.ConfigureLobbyPanel(listText, status, start);

            var lso = new SerializedObject(lobby);
            lso.FindProperty("_startButton").objectReferenceValue = start;
            lso.FindProperty("_playerListText").objectReferenceValue = listText;
            lso.FindProperty("_statusText").objectReferenceValue = status;
            lso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, $"{ScenesPath}/01_Lobby.unity");
        }

        private static void CreateGameScene(SessionCatalog.MapKind mapKind, string sceneFileName)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Directional Light", typeof(Light));
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            new GameObject("GameSceneSessionGuard", typeof(GameSceneSessionGuard));

            var mapRoot = new GameObject("MapEnvironment");
            var marker = mapRoot.AddComponent<MapSceneMarker>();
            var markerSo = new SerializedObject(marker);
            markerSo.FindProperty("_mapKind").enumValueIndex = (int)mapKind;
            markerSo.ApplyModifiedPropertiesWithoutUndo();
            var builder = mapRoot.AddComponent<MapEnvironmentBuilder>();
            builder.Rebuild();

            var spawnRoot = new GameObject("SpawnPoints");
            for (var i = 0; i < 10; i++)
            {
                var sp = new GameObject($"Spawn_{i}");
                sp.transform.SetParent(spawnRoot.transform, false);
            }

            MapSpawnLayout.PlaceSpawnPoints(spawnRoot.transform, mapKind);

            var gmGo = new GameObject("GameManager");
            gmGo.AddComponent<NetworkObject>();
            var gm = gmGo.AddComponent<GameManager>();
            var avatarPrefab = AssetDatabase.LoadAssetAtPath<NetworkObject>($"{PrefabsPath}/PlayerAvatar.prefab");
            var physicsProp = AssetDatabase.LoadAssetAtPath<NetworkObject>($"{PrefabsPath}/PhysicsProp.prefab");
            var gmSo = new SerializedObject(gm);
            gmSo.FindProperty("_playerAvatarPrefab").objectReferenceValue = avatarPrefab;
            gmSo.FindProperty("_physicsPropPrefab").objectReferenceValue = physicsProp;
            var spProp = gmSo.FindProperty("_spawnPoints");
            spProp.arraySize = 10;
            for (var i = 0; i < 10; i++)
                spProp.GetArrayElementAtIndex(i).objectReferenceValue = spawnRoot.transform.GetChild(i);
            gmSo.ApplyModifiedPropertiesWithoutUndo();

            var chatGo = new GameObject("ChatManager");
            chatGo.AddComponent<NetworkObject>();
            chatGo.AddComponent<ChatManager>();

            var uiCanvas = UiSceneLayout.CreateCanvas("GameUICanvas");
            uiCanvas.gameObject.AddComponent<UiReadabilityBootstrap>();
            uiCanvas.gameObject.AddComponent<CharacterSelectUI>();
            uiCanvas.gameObject.AddComponent<ChatUI>();

            var goCanvas = UiSceneLayout.CreateCanvas("GameOverCanvas");
            goCanvas.gameObject.AddComponent<UiReadabilityBootstrap>();
            goCanvas.sortingOrder = 100;
            var goUi = goCanvas.gameObject.AddComponent<GameOverUI>();
            goCanvas.gameObject.AddComponent<CombatHealthHud>();
            goCanvas.gameObject.AddComponent<CrosshairHud>();
            goCanvas.gameObject.AddComponent<LeaderboardUI>();
            goCanvas.gameObject.AddComponent<MatchTimerHud>();
            goCanvas.gameObject.AddComponent<MatchInfoHud>();
            goCanvas.gameObject.AddComponent<PauseMenuUI>();
            var masterBtn = UiSceneLayout.CreateButton(goCanvas.transform, "BtnEndGame", "END GAME", new Vector2(320f, 64f));
            var masterRt = masterBtn.GetComponent<RectTransform>();
            masterRt.anchorMin = new Vector2(0.82f, 0.58f);
            masterRt.anchorMax = new Vector2(1f, 0.62f);
            masterRt.offsetMin = new Vector2(8f, 0f);
            masterRt.offsetMax = new Vector2(-12f, 0f);
            UiTypography.StyleButton(masterBtn);

            var overlay = UiSceneLayout.CreateUIPanel(goCanvas.transform, "GameOverOverlay");
            overlay.gameObject.SetActive(false);
            overlay.image.color = new Color(0f, 0f, 0f, 0.75f);
            overlay.rectTransform.anchorMin = Vector2.zero;
            overlay.rectTransform.anchorMax = Vector2.one;
            overlay.rectTransform.offsetMin = Vector2.zero;
            overlay.rectTransform.offsetMax = Vector2.zero;
            var goTitle = UiSceneLayout.CreateText(overlay.transform, "GameOverTitle", UiTypography.Title,
                TextAlignmentOptions.Center, FontStyles.Bold);
            goTitle.text = "GAME OVER";
            UiTypography.ApplyTitle(goTitle);
            UiRegionLayout.StretchBand(goTitle.rectTransform, 0.72f, 0.88f, 48f);

            var resultsTitle = UiSceneLayout.CreateText(overlay.transform, "ResultsTitle", UiTypography.Subtitle,
                TextAlignmentOptions.Center, FontStyles.Bold);
            resultsTitle.text = UiCopy.GameOverResultsTitle;
            UiTypography.ApplySubtitle(resultsTitle);
            UiRegionLayout.StretchBand(resultsTitle.rectTransform, 0.62f, 0.70f, 80f);

            var resultsTable = UiSceneLayout.CreateText(overlay.transform, "ResultsTable", UiTypography.Body,
                TextAlignmentOptions.Top, FontStyles.Normal);
            resultsTable.text = UiCopy.GameOverNoScores;
            UiTypography.ApplyBody(resultsTable, TextAlignmentOptions.Top);
            UiRegionLayout.StretchBand(resultsTable.rectTransform, 0.42f, 0.62f, 80f);

            var leaveBtn = UiSceneLayout.CreateButton(overlay.transform, "BtnLeave", UiCopy.GameOverLeave, new Vector2(360f, 72f));
            var leaveRt = leaveBtn.GetComponent<RectTransform>();
            leaveRt.anchorMin = new Vector2(0.5f, 0f);
            leaveRt.anchorMax = new Vector2(0.5f, 0f);
            leaveRt.pivot = new Vector2(0.5f, 0f);
            leaveRt.anchoredPosition = new Vector2(0f, 28f);
            leaveRt.sizeDelta = new Vector2(420f, 64f);
            UiTypography.StyleButton(leaveBtn);

            var goSo = new SerializedObject(goUi);
            goSo.FindProperty("_overlayRoot").objectReferenceValue = overlay.gameObject;
            goSo.FindProperty("_masterEndButton").objectReferenceValue = masterBtn;
            goSo.FindProperty("_leaveButton").objectReferenceValue = leaveBtn;
            goSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, $"{ScenesPath}/{sceneFileName}.unity");
        }

        private static void CreateBootScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var boot = new GameObject("BootLoader");
            boot.AddComponent<BootLoader>();
            EditorSceneManager.SaveScene(scene, $"{ScenesPath}/00_Boot.unity");
        }
    }
}
#endif
