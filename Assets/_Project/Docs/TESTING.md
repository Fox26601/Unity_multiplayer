# Testing checklist

**Author:** Daniil Gorlov (solo). Multi-client tests use **Multiplayer Play Mode** (virtual players) — no second machine required.

## Release minimum — single Play in editor

Use this first. It matches the **Definition of Done** in `RELEASE.md`.

### Before Play

1. **Tools → Fusion Multiplayer → Generate Scenes, Prefabs, And Build Settings** (once per clone)
2. **Tools → Fusion Multiplayer → Use Main Menu As Play Mode Start Scene**
3. **Tools → Fusion Multiplayer → Validate Setup** and **Validate UI In Scenes** — fix errors
4. Fusion Hub: register **PlayerData**, **PlayerAvatar**, **PlacedBlock**, **Projectile**; **Tick Rate** Client **32**, Send Index **1**; **Input Data Word Count** = **5** (mouse look); **Apply**
5. Game view **Scale 1x**

### Photon DNS / connection errors

If the console shows `Could not resolve host 'ns.photonengine.io'` or `DnsExceptionOnConnect`:

| Step | Action |
|------|--------|
| 1 | Confirm internet works in a browser |
| 2 | Disable VPN / proxy; allow **Unity** in macOS Firewall (System Settings → Network → Firewall) |
| 3 | Terminal test: `nslookup ns.photonengine.io` — should return an IP |
| 4 | Fusion Hub → **Photon App Settings** — valid **App Id Fusion** (not empty GUID) |
| 5 | **Local workaround:** **Tools → Fusion Multiplayer → Enable Offline Play (No Photon Cloud)** → Play again (single-player only, no lobby sync) |

### Multiplayer Play Mode (MPPM) — read-only Asset Database

If the console shows `Asset Database is set to Read Only` with stack traces mentioning `CloneInternalRuntime` or `TriggerCloneRefreshMessage`:

| Step | Action |
|------|--------|
| 1 | **File → Save Project** before entering Play with virtual players |
| 2 | **Tools → Fusion Multiplayer → Repair MPPM Cache (Delete Library/VP)** → restart Unity |
| 3 | If still failing: close Unity, delete entire **Library** folder, reopen project |
| 4 | For 2-client tests without MPPM: **ParrelSync** or **two standalone builds** (see below) |

This is a [known Unity MPPM issue](https://issuetracker.unity3d.com/issues/multiplayer-play-mode-using-custom-dependencies-and-scriptedimporters-triggers-asset-database-is-set-to-read-only-errors-in-virtual-instances) with **ScriptedImporters** (Fusion `NetworkProjectConfig.fusion`). The project **filters this warning** in all Editor consoles (`MppmReadOnlyLogFilter`). It is **harmless** if Fusion logs `adding player [Player:2]` and gameplay works. If clones fail to start or desync often: **File → Save Project** before Play, then **Repair MPPM Cache**.

### Multiplayer Play Mode — two clients (Player 1 + Player 2)

| Step | Player 1 (main editor) | Player 2 (MPPM clone) |
|------|------------------------|------------------------|
| 1 | **Tools → Use Main Menu As Play Mode Start Scene** (auto-set on first load if empty) | Same — clone redirects to main menu if it started on `02_Game` |
| 2 | Turn **OFF** Offline Play (needs Photon Shared Mode) | Same |
| 3 | Enable **Player 2** in MPPM window, press Play | Clone opens `00_MainMenu` |
| 4 | Nickname + room → **Create / Host** | Waits ~1s, then **auto-joins** the same room as `Player 2` |
| 5 | Both in lobby → master **Start Game** | Both load `02_Game` with active Fusion session |
| 6 | Pick slots, chat, PM dropdown | Same |

If Player 2 shows **NO ACTIVE SESSION** on `02_Game`: stop Play, set Play Mode Start Scene to main menu, host on Player 1 first — do not open `02_Game` alone on the clone.

### Single-client flow

| Step | Action | Pass criteria |
|------|--------|----------------|
| 1 | Press Play | `00_MainMenu` loads; **Landing** shows nickname, color, **Create room** / **Join room** |
| 2 | **Create room** | Create page: mode, map, room name, **Create / Host**; **Back** returns to Landing |
| 3 | Nickname + room → **Create / Host room** | Green status **Connecting to Photon room…**; buttons disabled briefly |
| 4 | Wait | Scene changes to lobby; **LOBBY** title; status **You are in the lobby.** |
| 5 | **Join room** (second client) | Join page: mode/map filters, **Available rooms** dropdown, **Refresh**, **Join room** |
| 6 | Lobby | Player list shows your nickname; **START GAME** visible (you are master in solo Play) |
| 7 | **START GAME** | `02_Game` loads; **SlotGrid** 5×2 at top; click slot → status changes immediately |
| 8 | Pick slot 0 | Status **Requesting slot 0…** then avatar spawns; mouse locks; **Slot N · WASD · Mouse** hint (~3s); panel hides |
| 9 | **Mouse** look | Cursor hidden; move mouse — yaw + pitch change; WASD moves relative to view |
| 10 | Click Game view | If cursor unlocked (alt-tab), **LMB** re-locks mouse look |
| 11 | **Enter** chat | Compose bar appears; cursor visible; type message |
| 12 | **Enter** again | Message sends; compose hides; mouse look returns |
| 13 | **E** / **Q** | **E** places cube on grid; **Q** removes crosshair block (not while chat open) |
| 14 | Master **End game** | Overlay; **Leave to main menu** returns to menu |

### Console checks (single Play)

- No **red** errors
- No **`[Fusion] Invalid TickRate`** or **input word count** mismatch (GameplayNetworkInput needs **5** words — Fusion Hub → Apply after pull)
- Connection failures show **red on-screen status** (test with empty App Id if needed)
- UI panels do **not overlap** (character select y 62–100%, chat x 0–50% y 0–58%, End Game x 82–100% y 58–62%)
- Game view **Scale 1x** — UI uses 1280×720 reference; chat layout is **runtime v27** (PM select + send)

### Chat v27 — PM option clicks

| Check | Pass |
|-------|------|
| PM field | **Single border**; caption **Everyone (global)** + arrow on right |
| First click | List opens upward; compose row **does not shift** |
| PM select | Click **Player2** in list → caption changes → console `PM selected index=1` |
| PM send | `send whisper=true` in console; blue PM for sender + recipient |
| Global | **Everyone (global)** still sends room chat |

### Chat debug console (`[FusionMultiplayer][Chat]`)

Filter the Unity Console by `[FusionMultiplayer][Chat]` when verifying chat.

| Log line | Meaning |
|----------|---------|
| `bind v27: log=ok … path=…/ChatLog` | UI wired; `log=MISSING` means no messages will show |
| `append … rectH=NN preferredH=NN` | After send, **rectH must be > 0** or log area stays blank |
| `PM selected index=N nick="…" target=…` | Dropdown selection registered |
| `PM channel list open/closed` | Channel list lifecycle |
| `dropdown options=["Everyone (global)", "…"]` | All PM targets visible in popup list |
| `PM roster … options=2` | Dropdown should list Everyone + remote player |
| `dropdown caption="Everyone (global)" captionW=… dropdownW=…` | PM caption visible; `captionW=0` means layout bug |
| `manager spawned` / `manager ready` | `ChatManager` network object is live |
| `manager waiting` | Scene still loading; send may fail until `ready` |
| `PM roster active=N … remotes=[…]` | Dropdown player list; warning if remotes empty with 2+ players |
| `send whisper=false sent=true` | Global message queued |
| `send whisper=true target=… sent=true` | PM queued |
| `receive global from=…` | RPC delivered to presenter |
| `append logBound=true totalLen=…` | Line added to log TMP |
| `no chat presenter attached` | `ChatUI` not bound — check `ChatPanel` / Play rebuild |

**Stop Play completely** (main editor + all MPPM clones) after pulling chat UI changes so v27 rebuild runs on every client.

### Character select + chat v8 (disk + Play)

| Check | Pass |
|-------|------|
| **Validate UI In Scenes** before Play | No legacy `CharacterSelectPanel` without `SlotGrid`; no orphan `Char_*` under GameUICanvas |
| After Play | `CharacterSelectPanel/SlotGrid` with 10 slots; labels **FREE** / **YOU** / nickname |
| Slot click | Status updates to **Requesting slot N…** on first click (not stuck on prompt) |
| Occupied slot | Shows player tint; not interactable for others |
| Chat | Compact header **CHAT · You: nick**; log dominates panel; PM row visible when session active |
| Solo chat | PM dropdown shows **Everyone (global)** only; global messages appear in log after Send |
| 2+ clients PM | Pick player in **PM** dropdown → send → sender sees `[PM → nick]:` echo; recipient sees `[PM]` line |

### Block placement (E / Q)

| Check | Pass |
|-------|------|
| After slot spawn | Press **E** 10× in a row (move between cells) → 10 cubes on **2 m grid**, aligned with checkerboard |
| Same cell | Second **E** in occupied cell → no duplicate cube |
| Chat focus | Press **Enter** → compose opens; **E** places only when compose is closed |
| No chat focus | **E** places cube in front on nearest grid cell |
| **Q** | Aim at placed cube, **Q** → cube removed for all clients |
| PM blue text | Select player in **PM** dropdown (not **Everyone**), send → both see **blue** `[PM → nick]:` / `[PM] nick:` |

### Combat / Sandbox — health, damage, Tab scoreboard

Requires **Combat** or **Sandbox** game mode from main menu. Two MPPM clients recommended.

| Check | Pass |
|-------|------|
| HP HUD | Bottom-left shows **HP 100/100** after spawn |
| Hit damage | LMB shoot enemy → red flash; victim HP drops by **25** per hit |
| Kill | **4** hits (100 HP) → victim shows **DEAD — respawning…**; cannot move or shoot while dead |
| Respawn | After **~3 s** victim teleports to their **spawn slot** with full HP |
| Kill credit | Shooter gains **+1 kill**; victim **+1 death** (not per hit, only on kill) |
| Environment hits | Shoot wall/fence/column → projectile disappears (no damage) |
| Placed blocks | Sandbox/Build: shoot player-placed cube → projectile stops |
| Tab scoreboard | **Hold Tab** → centered overlay **SCOREBOARD** with **PLAYER / KILLS / DEATHS** |
| Tab release | Release Tab → overlay hides; cursor stays locked |
| Tab + chat | Tab scoreboard hidden while chat compose is open |
| End game | Master **End Game** → results table shows same **KILLS / DEATHS** columns |

---

## Full assignment — ≥ 3 clients

### Prerequisites

1. Start from **`00_MainMenu`** or Play Mode Start Scene
2. **Validate Setup** passes
3. Network prefabs registered in Fusion

Use **ParrelSync**, **Multiplayer Play Mode** (Unity 6), or **three standalone builds** + one editor instance.

### Assignment checklist

| # | Requirement | How to verify |
|---|-------------|----------------|
| 1 | Create or join a **room** (session name) | Same room name from main menu on two machines; both reach lobby. |
| 2 | **Up to 10** players; **dynamic join** | `PlayerCount = 10`; players can join after **Start Game** (late-join test B below). |
| 3 | **Lobby** vs **game** scenes | After lobby, master **Start Game** → everyone loads `02_Game`. |
| 4 | **10 characters**, taken state synced | Pick slot `0` on client A; client B sees slot **TAKEN** / nickname + tint; button disabled. |
| 5 | **Master** approves spawn | B picks taken slot → rejection message; free slot → spawn at correct **spawn point**. |
| 6 | **Position + rotation** sync | Walk/turn on one client; others see movement. |
| 7 | **Spawn / Despawn** objects | `E` places on 2 m grid (visible to all); same cell blocked; `Q` removes aimed block |
| 8 | **Global chat** | Message with no chip selected → all see line |
| 9 | **Whisper** | Select player in **PM** dropdown, send → recipient sees `[PM]`; sender sees local echo |
| 10 | **Master ends game** | Master clicks **End Game** → all see overlay; **Leave to Main Menu** → `00_MainMenu`. |

### Bonus

| # | Requirement | How to verify |
|---|-------------|----------------|
| B1 | **Nickname** visible | Lobby list + nameplate above avatar. |
| B2 | **Color** in UI + player | Main menu preview; lobby list; avatar tint. |

### Suggested 3-client script (5 minutes)

1. **Player 1** (editor) — create room `test123`, nickname `Alice`, random color.  
2. **Player 2** — join `test123`, nickname `Bob`.  
3. **Player 3** — join `test123`, nickname `Carol`.  
4. **Alice** (master) → **Start Game**.  
5. Each picks **different** character index; verify fourth cannot take occupied slot.  
6. **Bob** sends global chat; **Carol** selects **Bob** in the **PM** dropdown → PM — only Bob sees `[PM]`; Carol sees echo.  
7. **Alice** → **End Game** → all **Leave to Main Menu**.

### Late-join test B (required for dynamic player count — assignment §2)

Use **3 MPPM clients** (or ParrelSync / builds). Player 3 must join **after** the game scene is already running.

| Step | Action | Pass criteria |
|------|--------|----------------|
| 1 | **P1** create room `test123`, **P2** join | Both in lobby |
| 2 | **P1** (master) → **Start Game** | P1 + P2 load `02_Game` |
| 3 | **P1** and **P2** pick **different** slots | Both spawn avatars; occupied slots show nickname/tint |
| 4 | **P3** join `test123` **now** (after step 3) | P3 loads `02_Game`; console `[FusionMultiplayer][Game] spawned` / `ready` within ~8 s |
| 5 | **P3** character select | Status **Connecting game systems…** briefly; slots **disabled** until ready; then P1/P2 slots show **TAKEN** |
| 6 | **P3** picks a **free** slot | Avatar spawns; WASD / E / Q work |
| 7 | **P3** chat | Global message visible to all; PM to P1 or P2 works |
| 8 | **P1** → **End Game** | All see overlay; **Leave to Main Menu** works for P3 |

If P3 stays on **Connecting game systems…** beyond 8 s → status shows timeout message; check console for `[FusionMultiplayer][Game]` lines.

### Game debug console (`[FusionMultiplayer][Game]`)

Filter the Unity Console by `[FusionMultiplayer][Game]` when verifying late-join on Player 3.

| Log line | Meaning |
|----------|---------|
| `scene loaded name=02_Game buildIndex=2 localPlayer=N` | Game scene synced for this client |
| `late-join provision: OnSceneLoadDone` / `OnPlayerJoined` | `PlayerData` spawn + readiness re-check |
| `spawned objectValid=True` | `GameManager` scene NetworkObject spawned |
| `ready: instance=ok objectValid=True` | Safe to pick a character slot |
| `waiting (Xs): instance=null objectValid=False` | UI waiting for `GameManager` |
| `GameManager not ready after 8s timeout` | Late-join failed — rejoin from main menu |

## Known limitations

- Whisper via **PM dropdown** (**Everyone (global)** = room chat).
- **Random color** does not enforce uniqueness across players.
