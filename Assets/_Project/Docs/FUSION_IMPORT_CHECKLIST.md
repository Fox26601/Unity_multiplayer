# Fusion import checklist (fixes CS0246)

Complete these steps **in order** inside the Unity Editor on this machine.

## 1. Import Photon Fusion 2 SDK

1. Log in at [Photon dashboard](https://dashboard.photonengine.com/) and download **Fusion SDK 2.x Stable** (`.unitypackage`).
2. In Unity: **Assets → Import Package → Custom Package…** and import the file.
3. Wait for import to finish (can take several minutes).

**Success:** Project window shows `Assets/Photon/Fusion/...` and compile errors about missing `Fusion` namespace disappear.

## 2. Fusion App Id

1. In Photon dashboard create an app with SDK type **Fusion** / **Fusion 2** if you do not have one.
2. Open **Fusion Hub** (Unity menu **Fusion** or auto window) and paste **Fusion App Id** on the Welcome screen.

## 3. Assembly definitions (already in repo)

- [`Assets/_Project/Scripts/FusionMultiplayer.Runtime.asmdef`](../Scripts/FusionMultiplayer.Runtime.asmdef) references **`Fusion.Unity`** and the **`Fusion.Sockets`** plugin assembly (GUID) so types like `NetDisconnectReason`, `NetAddress`, `ReliableKey`, and `INetworkRunnerCallbacks` match Fusion 2.
- After step 1, select this asmdef in the Project window: **Fusion.Unity** must show as a resolved reference (no yellow warning).

## 4. Assemblies to weave (automated + manual confirm)

This repo includes an editor utility [`Assets/Editor/FusionNetworkConfigWeaverPatcher.cs`](../../Editor/FusionNetworkConfigWeaverPatcher.cs) that injects **`FusionMultiplayer.Runtime`** into `NetworkProjectConfig.fusion` when that file appears.

1. After Fusion import, check the Console for:  
   `Injected 'FusionMultiplayer.Runtime' into NetworkProjectConfig AssembliesToWeave`
2. Open **Fusion → Network Project Config** and click **Apply** if the inspector shows unsaved changes.
3. If no log appeared, add **`FusionMultiplayer.Runtime`** manually to **Assemblies To Weave**, then **Apply**.

## 5. Rebuild verification

1. Wait for script compilation to finish (**bottom-right** progress in Unity).
2. Open **Console** — there should be **zero** red compile errors.
3. If you still see errors **other than** missing `Fusion`:
   - **`NetworkString` / string assignment** — adjust to your Fusion version (see [SETUP.md](SETUP.md) troubleshooting).
   - **`Despawned()` override** — Fusion 2 uses `Despawned(NetworkRunner runner, bool hasState)` on `NetworkBehaviour`; this repo already matches that API.
   - **Weaver / Cecil** — try adding package `com.unity.nuget.mono-cecil` via Package Manager (Photon forum tip).

## 6. Project content (after Fusion compiles)

Run **Tools → Fusion Multiplayer → Generate Scenes, Prefabs, And Build Settings**, then register prefabs in Network Project Config per [SETUP.md](SETUP.md).

## 7. Play Mode (avoid “empty” game scene)

- Use **Tools → Fusion Multiplayer → Use Main Menu As Play Mode Start Scene** so **Play** always boots **`00_MainMenu`** (menu + `ConnectionManager`), not whatever scene tab was left open.
- Run **Tools → Fusion Multiplayer → Validate Setup (Console Report)** to confirm build order, weave entry, and prefab files on disk.
