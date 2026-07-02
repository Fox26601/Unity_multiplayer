# Release checklist — Fusion Multiplayer

Quick path from clone to a playable **single-editor Play** session.

## 1. Import Fusion and App Id

1. Import **Photon Fusion 2** from the Photon dashboard (`.unitypackage`).
2. Complete Fusion Hub / paste **Fusion App Id** into `PhotonAppSettings`.
3. Unity imports **Input System** from `Packages/manifest.json`. If prompted, confirm switching **Active Input Handling** to **Input System Package** (already set in `ProjectSettings`).

## 2. Generate project content

1. **Tools → Fusion Multiplayer → Generate Scenes, Prefabs, And Build Settings**
2. If UI was generated before TMP migration: **Tools → Fusion Multiplayer → Fix UI Layout In Scenes** (1280×720, bootstrap, strips legacy game HUD, End Game band, **EventSystem → Input System**)
3. After **git pull** or scene conflicts: run **Fix UI Layout In Scenes** again, then **Validate UI In Scenes**
4. First TMP import: **Tools → Fusion Multiplayer → Import TMP Essential Resources** (or Window → TextMeshPro → Import TMP Essential Resources).
5. If the importer window logs an error when you **close** it even after import, that is a [known Unity 6 false positive (UUM-136226)](https://issuetracker.unity3d.com/issues/textmesh-pro-essential-resources-are-missing-error-is-logged-after-importing-essentials-and-closing-tmp-importer-opened-by-creating-a-tmp-asset). Run **Tools → Fusion Multiplayer → Fix TMP Settings Version** and **Validate Setup** — ignore the spurious error if validation passes.

## 3. Fusion Hub / Network Project Config

1. **Fusion → Network Project Config**
2. **Assemblies To Weave:** include `FusionMultiplayer.Runtime` → **Apply**
3. **Network prefabs:** register `PlayerData`, `PlayerAvatar`, `PlacedBlock` from `Assets/_Project/Prefabs/`
4. **Tick Rate (Shared Mode):** **32 Hz** client/server tick, **16 Hz** send (Client Send Index = 1, Server Send Index = 1). The repo ships this in `NetworkProjectConfig.fusion`; confirm in Fusion Hub and **Apply** if you changed it.

## 4. Editor play settings

1. **Tools → Fusion Multiplayer → Use Main Menu As Play Mode Start Scene**
2. Game view **Scale: 1x** (not 3x+) for readable UI

## 5. Validate

**Tools → Fusion Multiplayer → Validate Setup (Console Report)**  
**Tools → Fusion Multiplayer → Validate UI In Scenes**

Fix any errors before testing.

## 6. Single Play test (release minimum)

1. Press **Play** (starts `00_MainMenu`)
2. Enter nickname + room name → **Create / Host room**
3. Status shows **Connecting…** → lobby with **LOBBY** title and player list
4. **START GAME (master only)** → game scene
5. Pick character slot → WASD move, E/Q blocks, chat
6. Master **End game** → **Leave to main menu**

Console should have **no red errors** and **no `[Fusion] Invalid TickRate`**.

## Optional: multi-client

Use **Multiplayer Play Mode** or ParrelSync for 2–3 clients — see `TESTING.md`.

## Definition of Done

| Check | Expected |
|-------|----------|
| Main menu | Readable title, fields, dark buttons, white text |
| Connect | On-screen status; auto lobby load |
| Lobby | Player list, master Start Game |
| Game | SlotGrid 5×2, avatar, camera, chat scroll, End Game band |
| Errors | Shown on screen (red status), not only Console |
| Fusion | Valid tick rate, prefabs registered, runtime woven |
