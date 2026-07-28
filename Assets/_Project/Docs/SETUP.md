# Fusion Multiplayer — Setup (Unity 6)

## Critical: import Fusion first

This repository **does not ship** the Photon Fusion binaries (license). Gameplay code lives in assembly **`FusionMultiplayer.Runtime`**, which references **`Fusion.Unity`**.

- If you see **`CS0246: Fusion could not be found`** (or “missing assembly Fusion.Unity”), you **have not imported** the Fusion SDK yet. See **`Assets/IMPORT_PHOTON_FUSION_FIRST.txt`**.
- After importing Fusion, open **Fusion → Network Project Config**, find **Assemblies To Weave**, add **`FusionMultiplayer.Runtime`**, then click **Apply** (required when using custom asmdefs with Fusion).

## Prerequisites

1. Unity **6000.3.x** (project uses `6000.3.8f1`).
2. **Photon Fusion 2** SDK (import the `.unitypackage` from the Photon dashboard).
3. **Fusion App Id** — paste into `PhotonAppSettings` (created by the Fusion import wizard).

## Topology

- **Editor create:** `GameMode.Host` (Client-Server authority).
- **Editor/client join:** `GameMode.Client`.
- **Dedicated process:** `GameMode.Server` via `-dedicated -room=Name`.
- Opening scene in Build Settings: **`00_Boot`** → Main Menu.

## Where is the menu / UI?

- **Main menu UI** lives only in **`Assets/_Project/Scenes/00_MainMenu.unity`** (`MainMenuCanvas` + `ConnectionManager`).
- **Lobby UI** is in **`01_Lobby.unity`**.
- **Game HUD** (character slots, chat, game over) is built at runtime on the map scenes (`02_Game_Arena` / `03_Game_Plaza` / `04_Game_Ruins`).
- **Do not press Play on a lone game map** unless you are debugging the “no session” overlay: there is **no `ConnectionManager`** in those scenes, so Fusion never starts. Start from **`00_Boot`** / **`00_MainMenu`**, or set the editor **Play Mode Start Scene** (see below).

## Play Mode start scene (recommended)

Once after cloning the repo:

1. Menu: **Tools → Fusion Multiplayer → Use Main Menu As Play Mode Start Scene**  
   Prefer starting from **`00_Boot`** in Build Settings (index 0); or set Play Mode Start Scene to `00_Boot` / `00_MainMenu`.
2. Optional reset: **Tools → Fusion Multiplayer → Clear Play Mode Start Scene (use active scene)**.

## One-time project wiring

1. Import **Photon Fusion 2** and complete the Fusion Hub / App Id step.
2. **Fusion → Network Project Config → Assemblies To Weave**: add **`FusionMultiplayer.Runtime`** (or confirm it was auto-injected), then **Apply**.  
   An editor helper [`Assets/Editor/FusionNetworkConfigWeaverPatcher.cs`](../../Editor/FusionNetworkConfigWeaverPatcher.cs) patches `NetworkProjectConfig.fusion` when Fusion is imported; always verify in the Fusion inspector and press **Apply**.
3. In the editor menu run: **Tools → Fusion Multiplayer → Generate Scenes, Prefabs, And Build Settings**  
   This creates TMP-based UI (readable contrast, layout) in:
   - Scenes: `00_Boot`, `00_MainMenu`, `01_Lobby`, `02_Game_Arena`, `03_Game_Plaza`, `04_Game_Ruins`
   - Prefabs: `PlayerData`, `PlayerAvatar`, `PlacedBlock`, `Projectile` under `Assets/_Project/Prefabs/`
   - **File → Build Settings** entries must keep Boot → MainMenu → Lobby → maps in that order.
   If UI looks broken after an upgrade, run **Fix UI Layout In Scenes** (also runs **Patch UI Scenes On Disk**) or regenerate.
4. Open **Fusion → Network Project Config** (or the Fusion project settings window):
   - **Register network prefabs:** `PlayerData`, `PlayerAvatar`, `PlacedBlock`, `Projectile`, `PhysicsProp`
   - **Peer Mode:** Client-Server (Host / Client / Dedicated Server) — not Shared Mode
   - **Tick Rate:** confirm values shipped in `NetworkProjectConfig.fusion` → **Apply** if changed
   - **Input Data Word Count:** **5** (WASD + strafe + look deltas + buttons) → **Apply**
5. Ensure **Multiplayer Play Mode** (or three standalone builds) can run **three clients** for the assignment test.
6. **Validate wiring:** **Tools → Fusion Multiplayer → Validate Setup (Console Report)** and **Validate UI In Scenes**.

## Controls (game scene)

| Action | Input |
|--------|--------|
| Move | **WASD** (relative to where you look) |
| Look | **Mouse** (locked while playing; cursor hidden) |
| Open chat | **Enter** — shows compose bar and frees the cursor |
| Send + close chat | **Enter** again (empty message just closes) |
| Cancel chat | **Esc** |
| Spawn block | **E** (only while chat is closed) |
| Despawn block (crosshair) | **Q** |

## Session flow

1. **Main menu** — nickname, random color, room name; **Create (Host)** or **Join (Client)** starts a Client-Server session (configurable max players) and loads the **lobby** scene. Dedicated servers use `-dedicated -room=Name`.
2. **Lobby** — each client gets **PlayerData** (nick + color). Host/server (or client request RPC) uses **Start Game** to load the selected map for everyone.
3. **Game** — pick a free character slot (0–9). **Server** approves ownership and spawn point; **PlayerAvatar** syncs with **NetworkTransform**. Projectiles use server-authoritative networked pose (no NetworkTransform). **Chat** (optional whisper by **exact nickname**). Match end → results / vote → **Leave to Main Menu** shuts down Fusion and returns to the menu.

**Dynamic player count:** players may **join or leave at any time** while the session is open (up to **10**). Mid-match join is gated (started rooms refuse unknown tokens); reconnect restores a known `ConnectionToken`.

## Troubleshooting

- **`CS0246` / `Fusion` namespace missing** — import the Fusion `.unitypackage` so the **`Fusion.Unity`** assembly exists. There is no supported way to compile this gameplay code without the official SDK.
- **Fonts / UI unreadable** — UI uses **TextMeshPro** with dark buttons and light text. Run **Generate Scenes** or **Fix UI Layout In Scenes**. Set Game view **Scale 1x**.
- **`[Fusion] Invalid TickRate`** — open Fusion → Network Project Config, confirm Client-Server tick/send rates match the shipped `NetworkProjectConfig.fusion`, then **Apply**.
- **`NetworkString` compile errors** — adjust `PlayerData` / RPC string conversion to your Fusion version (see Fusion docs for `NetworkString<_N>`).
- **Scene / prefab not in build** — re-run the generator or re-add scenes in Build Settings (Boot → MainMenu → Lobby → maps).
- **"The referenced script (Unknown) on this Behaviour is missing!"** on UI panels — older generated scenes used an Editor-only helper script. Current scenes use runtime rebuilds; re-run **Tools → Fusion Multiplayer → Generate…** or **Fix UI Layout In Scenes**.
- **Black Game view / “No cameras rendering” on a game map alone** — expected until a **PlayerAvatar** spawns after lobby; if you started Play without Fusion, **`GameSceneSessionGuard`** shows a blocking message. Fix by starting from **`00_Boot`** / **`00_MainMenu`**.
- **Lobby / menu UI tiny or unreadable** — run **Tools → Fusion Multiplayer → Fix UI Layout In Scenes** (or regenerate scenes). Set Game view **Scale** to **1x** for clearest text.
- **`OpAuthenticate failed` / `Authenticate without Token is only allowed on Name Server`** — Photon rejected your **Fusion App Id**. The repo ships a template Id that only works on the original author's Photon account. Fix:
  1. [Photon Dashboard](https://dashboard.photonengine.com/) → **Create → Fusion** app → copy **Fusion App Id**.
  2. Unity: **Tools → Fusion Multiplayer → Configure Fusion App Id (Photon Hub)** → paste Id → Save.
  3. **Tools → Fusion Multiplayer → Validate Setup** — must not report "repo template" App Id.
  4. Play **00_MainMenu** → Create/Join → lobby loads without auth errors in Console.
  **Workaround (single player, no MPPM):** **Tools → Fusion Multiplayer → Enable Offline Play (No Photon Cloud)**.
- **`Could not resolve host 'ns.photonengine.io'`** — DNS/network issue reaching Photon. Check internet/VPN/firewall, or use **Offline Play** for local tests.

## Comments language

All C# comments in this project are **English** only (per team rule).
