#if UNITY_EDITOR
using System.Linq;
using FusionMultiplayer.Environment;
using FusionMultiplayer.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace FusionMultiplayer.EditorTools
{
    /// <summary>
    /// Patches existing generated scenes with readable full-screen UI layout and typography (TMP).
    /// </summary>
    internal static class UILayoutPatcher
    {
        private const string MainMenuPath = "Assets/_Project/Scenes/00_MainMenu.unity";
        private const string LobbyPath = "Assets/_Project/Scenes/01_Lobby.unity";
        private const string ArenaScenePath = "Assets/_Project/Scenes/02_Game_Arena.unity";
    private const string PlazaScenePath = "Assets/_Project/Scenes/03_Game_Plaza.unity";
    private const string RuinsScenePath = "Assets/_Project/Scenes/04_Game_Ruins.unity";

        [MenuItem("Tools/Fusion Multiplayer/Patch UI Scenes On Disk (Python)")]
        public static void PatchScenesOnDisk()
        {
            var script = $"{Application.dataPath}/_Project/Editor/patch_ui_scenes.py";
            if (!System.IO.File.Exists(script))
            {
                Debug.LogError("[FusionMultiplayer] patch_ui_scenes.py not found.");
                return;
            }

            System.Diagnostics.Process.Start("python3", $"\"{script}\"")?.WaitForExit();
            AssetDatabase.Refresh();
            Debug.Log("[FusionMultiplayer] UI scenes patched on disk (1280x720, bootstrap, button colors).");
        }

        [MenuItem("Tools/Fusion Multiplayer/Fix UI Layout In Scenes")]
        public static void FixAll()
        {
            PatchScenesOnDisk();
            FixMainMenu();
            FixLobby();
            FixGame();
            MigrateEventSystemsInAllScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[FusionMultiplayer] UI layout, EventSystem (Input System), and typography patched in all scenes.");
        }

        [MenuItem("Tools/Fusion Multiplayer/Migrate EventSystem To Input System")]
        public static void MigrateEventSystemsMenu()
        {
            MigrateEventSystemsInAllScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[FusionMultiplayer] EventSystem migrated to InputSystemUIInputModule in all scenes.");
        }

        private static readonly string[] MapScenePaths =
        {
            ArenaScenePath, PlazaScenePath, RuinsScenePath
        };

        private static void MigrateEventSystemsInAllScenes()
        {
            foreach (var path in new[] { MainMenuPath, LobbyPath }.Concat(MapScenePaths))
            {
                if (!System.IO.File.Exists(path)) continue;
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                MigrateEventSystemsInScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void MigrateEventSystemsInScene()
        {
            foreach (var es in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            {
                if (es == null) continue;

                var legacy = es.GetComponent<StandaloneInputModule>();
                if (legacy != null)
                    Object.DestroyImmediate(legacy);

                if (es.GetComponent<InputSystemUIInputModule>() == null)
                {
                    var uiModule = es.gameObject.AddComponent<InputSystemUIInputModule>();
                    uiModule.actionsAsset = null;
                }
            }
        }

        [MenuItem("Tools/Fusion Multiplayer/Emergency Patch Main Menu Scene (Disk)")]
        public static void EmergencyPatchMainMenuOnDisk()
        {
            var script = $"{Application.dataPath}/_Project/Editor/patch_main_menu_scene.py";
            if (!System.IO.File.Exists(script))
            {
                Debug.LogError("[FusionMultiplayer] patch_main_menu_scene.py not found.");
                return;
            }

            System.Diagnostics.Process.Start("python3", $"\"{script}\"")?.WaitForExit();
            AssetDatabase.Refresh();
            FixMainMenu();
            Debug.Log("[FusionMultiplayer] Main menu scene patched on disk. Re-open 00_MainMenu if it was open.");
        }

        private static void FixMainMenu()
        {
            var scene = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
            var menu = Object.FindFirstObjectByType<MainMenuUI>();
            if (menu == null)
            {
                Debug.LogWarning("[FusionMultiplayer] MainMenuUI not found — run Generate Scenes first.");
                return;
            }

            ConfigureCanvas(menu.GetComponent<Canvas>());
            EnsureTitle(menu.transform, "Title", UiCopy.MainMenuTitle);
            EnsureSubtitle(menu.transform, "Subtitle", UiCopy.MainMenuSubtitle);
            EnsureSessionFlow(menu.gameObject);

            var panel = menu.transform.Find("Panel");
            if (panel == null) return;

            var so = new SerializedObject(menu);
            var nick = so.FindProperty("_nicknameField").objectReferenceValue as TMP_InputField;
            var room = so.FindProperty("_roomField").objectReferenceValue as TMP_InputField;
            var preview = so.FindProperty("_colorPreview").objectReferenceValue as Image;
            var randomColor = so.FindProperty("_randomColorButton").objectReferenceValue as Button;
            var create = so.FindProperty("_createButton").objectReferenceValue as Button;
            var join = so.FindProperty("_joinButton").objectReferenceValue as Button;
            if (nick != null && room != null && preview != null && randomColor != null && create != null && join != null)
                UiSceneLayout.ConfigureMainMenuPanel(panel, nick, room, preview, randomColor, create, join);

            ApplyTypography(menu.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void FixLobby()
        {
            var scene = EditorSceneManager.OpenScene(LobbyPath, OpenSceneMode.Single);
            var lobby = Object.FindFirstObjectByType<LobbyUI>();
            if (lobby == null)
            {
                Debug.LogWarning("[FusionMultiplayer] LobbyUI not found — run Generate Scenes first.");
                return;
            }

            ConfigureCanvas(lobby.GetComponent<Canvas>());
            EnsureSessionFlow(lobby.gameObject);
            var panel = lobby.transform.Find("Panel");
            if (panel != null)
                EnsureTitle(panel, "Title", UiCopy.LobbyTitle);

            var lso = new SerializedObject(lobby);
            var list = lso.FindProperty("_playerListText").objectReferenceValue as TMP_Text;
            var status = lso.FindProperty("_statusText").objectReferenceValue as TMP_Text;
            var start = lso.FindProperty("_startButton").objectReferenceValue as Button;
            if (list != null && status != null && start != null)
                UiSceneLayout.ConfigureLobbyPanel(list, status, start);

            ApplyTypography(lobby.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        [MenuItem("Tools/Fusion Multiplayer/Strip Legacy Game HUD (All Maps)")]
        public static void StripLegacyGameHudMenu()
        {
            foreach (var path in MapScenePaths)
            {
                if (!System.IO.File.Exists(path)) continue;
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                StripLegacyGameHud();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log("[FusionMultiplayer] Legacy HUD stripped from all map scenes.");
        }

        private static void FixGame()
        {
            foreach (var path in MapScenePaths)
            {
                if (!System.IO.File.Exists(path)) continue;
                FixGameScene(path);
            }
        }

        private static void FixGameScene(string path)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            StripLegacyGameHud();

            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas == null) continue;
                EnsureBootstrap(canvas);
                ConfigureCanvas(canvas);

                if (canvas.GetComponent<CharacterSelectUI>() != null || canvas.GetComponent<ChatUI>() != null)
                    continue;

                ApplyTypography(canvas.transform);
            }

            var goCanvas = Object.FindObjectsByType<GameOverUI>(FindObjectsSortMode.None);
            foreach (var goUi in goCanvas)
            {
                var btn = goUi.transform.Find("BtnEndGame")?.GetComponent<Button>();
                if (btn == null) continue;
                var rt = (RectTransform)btn.transform;
                rt.anchorMin = new Vector2(0.82f, 0.58f);
                rt.anchorMax = new Vector2(1f, 0.62f);
                rt.offsetMin = new Vector2(8f, 0f);
                rt.offsetMax = new Vector2(-12f, 0f);
                UiTypography.StyleButton(btn);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void StripLegacyGameHud()
        {
            foreach (var charUi in Object.FindObjectsByType<CharacterSelectUI>(FindObjectsSortMode.None))
            {
                var canvas = charUi.GetComponent<Canvas>();
                if (canvas == null) continue;

                for (var i = canvas.transform.childCount - 1; i >= 0; i--)
                {
                    var child = canvas.transform.GetChild(i);
                    if (child.name is "CharacterSelectPanel" or "ChatPanel")
                        Object.DestroyImmediate(child.gameObject);
                }

                var cso = new SerializedObject(charUi);
                cso.FindProperty("_panelRoot").objectReferenceValue = null;
                cso.FindProperty("_statusText").objectReferenceValue = null;
                var arr = cso.FindProperty("_characterButtons");
                arr.arraySize = 0;
                cso.ApplyModifiedPropertiesWithoutUndo();
            }

            foreach (var chatUi in Object.FindObjectsByType<ChatUI>(FindObjectsSortMode.None))
            {
                var cso = new SerializedObject(chatUi);
                cso.FindProperty("_input").objectReferenceValue = null;
                cso.FindProperty("_sendButton").objectReferenceValue = null;
                cso.FindProperty("_log").objectReferenceValue = null;
                cso.FindProperty("_whisperTargetNick").objectReferenceValue = null;
                cso.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ConfigureCanvas(Canvas canvas)
        {
            if (canvas == null) return;
            EnsureBootstrap(canvas);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = UiTypography.CanvasReferenceResolution;
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        private static void EnsureBootstrap(Canvas canvas)
        {
            if (canvas.GetComponent<UiReadabilityBootstrap>() == null)
                canvas.gameObject.AddComponent<UiReadabilityBootstrap>();
        }

        private static void EnsureSessionFlow(GameObject canvasGo)
        {
            var flow = canvasGo.GetComponent<SessionFlowUI>();
            if (flow == null)
                flow = canvasGo.AddComponent<SessionFlowUI>();

            var panel = canvasGo.transform.Find("Panel");
            if (panel == null) return;

            var statusTr = panel.Find("SessionStatus");
            TMP_Text status;
            if (statusTr != null)
                status = statusTr.GetComponent<TMP_Text>();
            else
                status = UiSceneLayout.CreateStatusLine(panel, "SessionStatus", string.Empty);

            var sfso = new SerializedObject(flow);
            sfso.FindProperty("_statusText").objectReferenceValue = status;
            sfso.ApplyModifiedPropertiesWithoutUndo();

            var menu = canvasGo.GetComponent<MainMenuUI>();
            if (menu != null)
            {
                var mso = new SerializedObject(menu);
                mso.FindProperty("_sessionFlow").objectReferenceValue = flow;
                mso.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ApplyTypography(Transform root)
        {
            UiTypography.ApplyHierarchy(root);
            EditorUtility.SetDirty(root);
        }

        private static void EnsureTitle(Transform parent, string name, string label)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                var t = existing.GetComponent<TMP_Text>();
                if (t != null)
                {
                    t.text = label;
                    UiTypography.ApplyTitle(t);
                    var rt = t.rectTransform;
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = new Vector2(0f, -8f);
                    rt.sizeDelta = new Vector2(-48f, 88f);
                }

                return;
            }

            UiSceneLayout.CreateTitle(parent, name, label);
        }

        private static void EnsureSubtitle(Transform parent, string name, string label)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                var t = existing.GetComponent<TMP_Text>();
                if (t != null)
                {
                    t.text = label;
                    UiTypography.ApplySubtitle(t);
                    var rt = t.rectTransform;
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = new Vector2(0f, -96f);
                    rt.sizeDelta = new Vector2(-64f, 72f);
                }

                return;
            }

            UiSceneLayout.CreateSubtitle(parent, name, label);
        }
    }
}
#endif
