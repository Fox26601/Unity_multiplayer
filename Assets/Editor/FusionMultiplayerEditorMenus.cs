#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FusionMultiplayer.Core;
using FusionMultiplayer.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// Editor utilities for Fusion Multiplayer template: play mode start scene, setup validation, scene patches.
/// </summary>
public static class FusionMultiplayerEditorMenus
{
    private const string MainMenuScenePath = "Assets/_Project/Scenes/00_MainMenu.unity";
    private const string LobbyScenePath = "Assets/_Project/Scenes/01_Lobby.unity";
    private const string ArenaScenePath = "Assets/_Project/Scenes/02_Game_Arena.unity";
    private const string PlazaScenePath = "Assets/_Project/Scenes/03_Game_Plaza.unity";
    private const string RuinsScenePath = "Assets/_Project/Scenes/04_Game_Ruins.unity";

    /// <summary>App Id shipped in repo template — belongs to another Photon account; replace with yours.</summary>
    private const string RepoTemplateAppId = "5af1bf0d-8cab-4bf6-a56a-857bd53c861b";

    private const string PhotonAppSettingsRelativePath = "Photon/Fusion/Resources/PhotonAppSettings.asset";

    [MenuItem("Tools/Fusion Multiplayer/Configure Fusion App Id (Photon Hub)")]
    private static void ConfigureFusionAppId()
    {
        if (!EditorApplication.ExecuteMenuItem("Tools/Fusion/Fusion Hub &f"))
            Debug.LogWarning("[FusionMultiplayer] Fusion Hub menu not found — import Photon Fusion 2 SDK first.");

        var settingsPath = $"Assets/{PhotonAppSettingsRelativePath}";
        var settings = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(settingsPath);
        if (settings != null)
        {
            EditorGUIUtility.PingObject(settings);
            Selection.activeObject = settings;
        }

        Debug.Log(
            "[FusionMultiplayer] Paste YOUR Fusion App Id from https://dashboard.photonengine.com/ " +
            "(Create → Fusion app). The template App Id in this repo will not authenticate on your account. " +
            "For local single-player tests without cloud: Tools → Fusion Multiplayer → Enable Offline Play.");
    }

    [MenuItem("Tools/Fusion Multiplayer/Use Main Menu As Play Mode Start Scene")]
    private static void SetPlayModeStartToMainMenu()
    {
        var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
        if (scene == null)
        {
            Debug.LogError($"[FusionMultiplayer] Scene asset not found: {MainMenuScenePath}");
            return;
        }

        EditorSceneManager.playModeStartScene = scene;
        Debug.Log("[FusionMultiplayer] Play Mode Start Scene set to 00_MainMenu. (Project Settings → Editor shows this.)");
    }

    [MenuItem("Tools/Fusion Multiplayer/Clear Play Mode Start Scene (use active scene)")]
    private static void ClearPlayModeStartScene()
    {
        EditorSceneManager.playModeStartScene = null;
        Debug.Log("[FusionMultiplayer] Play Mode Start Scene cleared.");
    }

    private const string OfflinePlayMenuPath = "Tools/Fusion Multiplayer/Enable Offline Play (No Photon Cloud)";

    [MenuItem(OfflinePlayMenuPath)]
    private static void ToggleOfflinePlay()
    {
        SessionData.UseOfflineMode = !SessionData.UseOfflineMode;
        Debug.Log(SessionData.UseOfflineMode
            ? "[FusionMultiplayer] Offline play ON — Create/Join uses GameMode.Single (no Photon DNS). Multiplayer requires turning this OFF."
            : "[FusionMultiplayer] Offline play OFF — Create/Join uses Photon Client-Server (Host/Client).");
    }

    [MenuItem(OfflinePlayMenuPath, true)]
    private static bool ToggleOfflinePlayValidate()
    {
        Menu.SetChecked(OfflinePlayMenuPath, SessionData.UseOfflineMode);
        return true;
    }

    [MenuItem("Tools/Fusion Multiplayer/Repair MPPM Cache (Delete Library/VP)")]
    private static void RepairMppmCache()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[FusionMultiplayer] Stop Play Mode before repairing MPPM cache.");
            return;
        }

        var vpPath = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Library", "VP");
        if (!Directory.Exists(vpPath))
        {
            Debug.Log("[FusionMultiplayer] Library/VP not found — nothing to delete.");
            return;
        }

        try
        {
            Directory.Delete(vpPath, recursive: true);
            Debug.Log(
                "[FusionMultiplayer] Deleted Library/VP. Re-open the project or restart Unity, then start Multiplayer Play Mode again.");
        }
        catch (IOException ex)
        {
            Debug.LogError($"[FusionMultiplayer] Could not delete Library/VP (close Unity and delete manually): {ex.Message}");
        }
    }

    [MenuItem("Tools/Fusion Multiplayer/Ensure Game Scene Session Guard (All Maps)")]
    private static void EnsureGameSceneGuard()
    {
        foreach (var path in new[] { ArenaScenePath, PlazaScenePath, RuinsScenePath })
        {
            if (!File.Exists(path))
                continue;

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (Object.FindFirstObjectByType<GameSceneSessionGuard>() != null)
                continue;

            var go = new GameObject("GameSceneSessionGuard");
            go.AddComponent<GameSceneSessionGuard>();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[FusionMultiplayer] Added GameSceneSessionGuard to {path}.");
        }
    }

    [MenuItem("Tools/Fusion Multiplayer/Validate Setup (Console Report)")]
    public static void ValidateSetup()
    {
        var ok = true;

        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();
        if (scenes.Length < 6)
        {
            Debug.LogError(
                $"[FusionMultiplayer] Build Settings: expected 6 enabled scenes (Boot, MainMenu, Lobby, 3 maps), found {scenes.Length}.");
            ok = false;
        }
        else
        {
            var expected = new[]
            {
                "00_Boot.unity", "00_MainMenu.unity", "01_Lobby.unity",
                "02_Game_Arena.unity", "03_Game_Plaza.unity", "04_Game_Ruins.unity"
            };
            for (var i = 0; i < expected.Length; i++)
            {
                if (!scenes[i].path.EndsWith(expected[i], System.StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogError($"[FusionMultiplayer] Build index {i} should be {expected[i]}. Found: {scenes[i].path}");
                    ok = false;
                }
            }
        }

        var fusionCfg = Path.Combine(Application.dataPath, "Photon/Fusion/Resources/NetworkProjectConfig.fusion");
        if (!File.Exists(fusionCfg))
        {
            Debug.LogWarning(
                "[FusionMultiplayer] Assets/Photon/Fusion/Resources/NetworkProjectConfig.fusion not found — import Photon Fusion 2.");
            ok = false;
        }
        else
        {
            var json = File.ReadAllText(fusionCfg);
            if (!json.Contains("\"FusionMultiplayer.Runtime\""))
            {
                Debug.LogError(
                    "[FusionMultiplayer] NetworkProjectConfig.fusion: add \"FusionMultiplayer.Runtime\" to AssembliesToWeave, then Apply in Fusion → Network Project Config.");
                ok = false;
            }

            if (json.Contains("\"Client\": 64"))
            {
                Debug.LogError(
                    "[FusionMultiplayer] NetworkProjectConfig.fusion: Client tick rate 64 is unsupported for this project. Open Fusion → Network Project Config → Tick Rate and Apply the shipped rates.");
                ok = false;
            }

            if (json.Contains("\"Client\": 32") && json.Contains("\"ClientSendIndex\": 0"))
            {
                Debug.LogError(
                    "[FusionMultiplayer] NetworkProjectConfig.fusion: Client Send Index 0 with 32 Hz tick under-sends. Set Client Send Index = 1 (or match shipped NetworkProjectConfig.fusion) and Apply.");
                ok = false;
            }

            if (json.Contains("\"InputDataWordCount\": 0"))
            {
                Debug.LogError(
                    "[FusionMultiplayer] NetworkProjectConfig.fusion: InputDataWordCount is 0 — GameplayNetworkInput will not work. Recompile scripts or Fusion Hub → Network Project Config → Apply.");
                ok = false;
            }
        }

        foreach (var rel in new[]
                 {
                     "_Project/Prefabs/PlayerData.prefab",
                     "_Project/Prefabs/PlayerAvatar.prefab",
                     "_Project/Prefabs/PlacedBlock.prefab",
                     "_Project/Prefabs/Projectile.prefab"
                 })
        {
            var check = Path.Combine(Application.dataPath, rel);
            if (!File.Exists(check))
            {
                Debug.LogError(
                    $"[FusionMultiplayer] Missing prefab: Assets/{rel}. Run Tools → Fusion Multiplayer → Generate Scenes, Prefabs, And Build Settings.");
                ok = false;
            }
        }

        if (EditorSceneManager.playModeStartScene == null)
        {
            Debug.LogWarning(
                "[FusionMultiplayer] Play Mode Start Scene is not set. Use Tools → Fusion Multiplayer → Use Main Menu As Play Mode Start Scene to avoid accidentally playing 02_Game alone.");
        }
        else
        {
            var p = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene);
            if (!p.EndsWith("00_Boot.unity", System.StringComparison.OrdinalIgnoreCase) &&
                !p.EndsWith("00_MainMenu.unity", System.StringComparison.OrdinalIgnoreCase))
                Debug.LogWarning(
                    $"[FusionMultiplayer] Play Mode Start Scene is {p} (expected 00_Boot or 00_MainMenu).");
        }

        ok &= ValidateUiScenes();
        ok &= ValidateTmpResources();
        ok &= ValidateInputSystem();
        ok &= ValidatePhotonAppId();

        if (ok)
            Debug.Log(
                "[FusionMultiplayer] Setup validation passed (build order, weave asm, prefabs, UI, tick rate, Photon App Id). " +
                "Register network prefabs in Fusion Hub if not done yet — see RELEASE.md.");
        else
            Debug.LogError("[FusionMultiplayer] Setup validation reported errors above. Fix them before release tests.");
    }

    [MenuItem("Tools/Fusion Multiplayer/Validate UI In Scenes")]
    private static void ValidateUiOnly()
    {
        if (ValidateUiScenes())
            Debug.Log("[FusionMultiplayer] UI validation passed for Main Menu, Lobby, and Game scenes.");
        else
            Debug.LogError("[FusionMultiplayer] UI validation failed. Run Generate Scenes or Fix UI Layout In Scenes.");
    }

    private static bool ValidateUiScenes()
    {
        var ok = true;
        var previous = EditorSceneManager.GetActiveScene().path;

        ok &= ValidateSceneUi(MainMenuScenePath, "Main Menu", requireTitle: true, requireSessionFlow: true);
        ok &= ValidateSceneUi(LobbyScenePath, "Lobby", requireTitle: true, requireSessionFlow: false);
        ok &= ValidateSceneUi(ArenaScenePath, "Game Arena", requireTitle: false, requireSessionFlow: false);
        ok &= ValidateSceneUi(PlazaScenePath, "Game Plaza", requireTitle: false, requireSessionFlow: false);
        ok &= ValidateSceneUi(RuinsScenePath, "Game Ruins", requireTitle: false, requireSessionFlow: false);

        if (!string.IsNullOrEmpty(previous) && previous != EditorSceneManager.GetActiveScene().path)
            EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);

        return ok;
    }

    private static bool ValidateSceneUi(string path, string label, bool requireTitle, bool requireSessionFlow)
    {
        if (!File.Exists(path))
        {
            Debug.LogError($"[FusionMultiplayer] {label}: scene missing at {path}");
            return false;
        }

        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        var ok = true;

        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            var usesRuntimeRebuild = canvas.GetComponent<UiReadabilityBootstrap>() != null &&
                (canvas.GetComponent<MainMenuUI>() != null ||
                 canvas.GetComponent<LobbyUI>() != null ||
                 canvas.GetComponent<CharacterSelectUI>() != null);

            if (canvas.GetComponent<UiReadabilityBootstrap>() == null)
            {
                Debug.LogError($"[FusionMultiplayer] {label}: Canvas '{canvas.name}' is missing UiReadabilityBootstrap.");
                ok = false;
            }

            var channels = AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal
                | AdditionalCanvasShaderChannels.Tangent;
            if ((canvas.additionalShaderChannels & channels) != channels && !usesRuntimeRebuild)
            {
                Debug.LogError(
                    $"[FusionMultiplayer] {label}: Canvas '{canvas.name}' missing TMP shader channels (TexCoord1/Normal/Tangent). " +
                    "UiReadabilityBootstrap should fix this at runtime; re-save scene or enter Play once.");
                ok = false;
            }
        }

        if (requireTitle)
        {
            var bootstrapMenu = Object.FindFirstObjectByType<MainMenuUI>();
            var bootstrapLobby = Object.FindFirstObjectByType<LobbyUI>();
            var titleBuiltAtRuntime =
                (bootstrapMenu != null && bootstrapMenu.GetComponent<UiReadabilityBootstrap>() != null) ||
                (bootstrapLobby != null && bootstrapLobby.GetComponent<UiReadabilityBootstrap>() != null);
            if (titleBuiltAtRuntime)
            {
                // Title is created at runtime by MainMenuRuntimeRebuild / LobbyRuntimeRebuild.
            }
            else
            {
                var title = Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)
                    .FirstOrDefault(t => t.gameObject.name == "Title");
                if (title == null)
                {
                    Debug.LogError($"[FusionMultiplayer] {label}: missing Title (TMP_Text). Regenerate scenes.");
                    ok = false;
                }
                else if (title.fontSize < UiTypography.MinimumReadable)
                {
                    Debug.LogError($"[FusionMultiplayer] {label}: Title fontSize {title.fontSize} < {UiTypography.MinimumReadable}.");
                    ok = false;
                }
            }
        }

        foreach (var button in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            var canvas = button.GetComponentInParent<Canvas>();
            var usesRuntimeRebuild = canvas != null && canvas.GetComponent<UiReadabilityBootstrap>() != null &&
                (canvas.GetComponent<MainMenuUI>() != null ||
                 canvas.GetComponent<LobbyUI>() != null ||
                 canvas.GetComponent<CharacterSelectUI>() != null);
            if (usesRuntimeRebuild)
                continue;

            var img = button.targetGraphic as Image;
            var labelText = button.GetComponentInChildren<TMP_Text>();
            if (img != null && labelText != null)
            {
                var imgBright = img.color.grayscale > 0.85f;
                var textBright = labelText.color.grayscale > 0.85f;
                if (imgBright && textBright)
                {
                    Debug.LogError(
                        $"[FusionMultiplayer] {label}: Button '{button.name}' has white/light image and light text (unreadable).");
                    ok = false;
                }
            }
        }

        if (requireSessionFlow)
        {
            var flow = Object.FindFirstObjectByType<SessionFlowUI>();
            var menu = Object.FindFirstObjectByType<MainMenuUI>();
            if (flow == null && menu == null)
            {
                Debug.LogError($"[FusionMultiplayer] {label}: missing SessionFlowUI or MainMenuUI on canvas.");
                ok = false;
            }
        }

        if (label == "Game")
            ok &= ValidateGameSceneUi();

        foreach (var legacy in Object.FindObjectsByType<StandaloneInputModule>(FindObjectsSortMode.None))
        {
            if (legacy == null) continue;
            Debug.LogError(
                $"[FusionMultiplayer] {label}: EventSystem uses deprecated StandaloneInputModule on '{legacy.gameObject.name}'. " +
                "Run Tools → Fusion Multiplayer → Migrate EventSystem To Input System.");
            ok = false;
        }

        EditorSceneManager.CloseScene(scene, true);
        return ok;
    }

    private static bool ValidateGameSceneUi()
    {
        var ok = true;
        var charUi = Object.FindFirstObjectByType<CharacterSelectUI>();
        if (charUi == null)
        {
            Debug.LogError("[FusionMultiplayer] Game: missing CharacterSelectUI on GameUICanvas.");
            return false;
        }

        var canvas = charUi.GetComponent<Canvas>();
        if (canvas == null)
            return ok;

        for (var i = 0; i < canvas.transform.childCount; i++)
        {
            var child = canvas.transform.GetChild(i);
            if (child.name.StartsWith("Char_"))
            {
                Debug.LogWarning(
                    $"[FusionMultiplayer] Game: orphan '{child.name}' under GameUICanvas — run Fix UI Layout In Scenes.");
                ok = false;
            }
        }

        var panel = canvas.transform.Find("CharacterSelectPanel");
        if (panel != null && panel.Find("SlotGrid") == null)
        {
            Debug.LogError(
                "[FusionMultiplayer] Game: legacy CharacterSelectPanel without SlotGrid on disk. " +
                "Run Tools → Fusion Multiplayer → Fix UI Layout In Scenes (strips legacy HUD; runtime rebuild at Play).");
            ok = false;
        }

        foreach (var tmp in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
        {
            if (tmp.gameObject.name is "SlotNumber" or "SlotOwner")
                continue;

            if (tmp.fontSize > 0 && tmp.fontSize < UiTypography.MinimumReadable &&
                tmp.GetComponentInParent<Canvas>() == canvas)
            {
                Debug.LogWarning(
                    $"[FusionMultiplayer] Game: TMP '{tmp.gameObject.name}' fontSize {tmp.fontSize} may be unreadable.");
            }
        }

        return ok;
    }

    private static bool ValidateInputSystem()
    {
        var settingsPath = Path.Combine(Application.dataPath, "../ProjectSettings/ProjectSettings.asset");
        if (!File.Exists(settingsPath))
            return true;

        foreach (var line in File.ReadAllLines(settingsPath))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("activeInputHandler:", System.StringComparison.Ordinal))
                continue;

            var value = trimmed.Substring("activeInputHandler:".Length).Trim();
            if (value == "0")
            {
                Debug.LogError(
                    "[FusionMultiplayer] Project Settings → Player → Active Input Handling must be " +
                    "'Input System Package (New)' (activeInputHandler: 1). " +
                    "Edit → Project Settings → Player → Other Settings → Configuration.");
                return false;
            }

            return true;
        }

        return true;
    }

    private static bool ValidateTmpResources()
    {
        if (!TmpEssentialsSetup.TmpResourcesPresentOnDisk())
        {
            Debug.LogError(
                "[FusionMultiplayer] TMP Essential Resources missing on disk. " +
                "Run Tools → Fusion Multiplayer → Import TMP Essential Resources.");
            return false;
        }

        if (TMP_Settings.defaultFontAsset == null && UiTypography.DefaultFont == null)
        {
            Debug.LogWarning(
                "[FusionMultiplayer] TMP_Settings not loaded or default font missing. " +
                "Run Tools → Fusion Multiplayer → Fix TMP Settings Version (writable project).");
            if (TmpEssentialsSetup.TmpSettingsVersionNeedsFix())
                TmpEssentialsSetup.TryFixTmpSettingsVersion();
        }

        if (UiTypography.DefaultFont == null)
        {
            Debug.LogError(
                "[FusionMultiplayer] LiberationSans SDF font asset not found. Re-import TMP Essential Resources.");
            return false;
        }

        Debug.Log(
            "[FusionMultiplayer] TMP resources OK. " +
            "If you see a spurious error when closing Unity's TMP Importer window, it is a known Unity 6 bug (UUM-136226) and can be ignored when resources exist.");
        return true;
    }

    private static bool ValidatePhotonAppId()
    {
        var path = Path.Combine(Application.dataPath, PhotonAppSettingsRelativePath);
        if (!File.Exists(path))
        {
            Debug.LogWarning(
                "[FusionMultiplayer] PhotonAppSettings.asset not found — import Photon Fusion 2 SDK, then paste your Fusion App Id.");
            return false;
        }

        var appId = TryReadAppIdFusion(path);
        if (string.IsNullOrWhiteSpace(appId))
        {
            Debug.LogError(
                "[FusionMultiplayer] AppIdFusion is empty in PhotonAppSettings. " +
                "Tools → Fusion Multiplayer → Configure Fusion App Id (Photon Hub).");
            return false;
        }

        if (appId.Equals(RepoTemplateAppId, StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError(
                "[FusionMultiplayer] Photon App Id is still the repo template (" + RepoTemplateAppId + "). " +
                "Create a Fusion app at https://dashboard.photonengine.com/ under YOUR account and paste the App Id. " +
                "OpAuthenticate failed until you replace it. Offline: Tools → Fusion Multiplayer → Enable Offline Play.");
            return false;
        }

        if (!Guid.TryParse(appId, out _))
        {
            Debug.LogWarning("[FusionMultiplayer] AppIdFusion does not look like a GUID: " + appId);
            return false;
        }

        Debug.Log("[FusionMultiplayer] Photon App Id is set (not the repo template).");
        return true;
    }

    private static string TryReadAppIdFusion(string assetPath)
    {
        var match = Regex.Match(File.ReadAllText(assetPath), @"AppIdFusion:\s*(\S+)");
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }
}
#endif
