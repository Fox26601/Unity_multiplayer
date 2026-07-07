# Assignment checklist — Fusion Multiplayer

## Submission metadata

| Field | Value |
|-------|--------|
| **Author** | Daniil Gorlov |
| **Group** | **Solo** — Daniil Gorlov only (sole author and implementer). Coursework normally expects groups of 3; this repository is an **individual submission**. |
| **Assignment 1 due** | 16 June 2026 (inclusive) |
| **Assignment 3 due** | 8 July 2026, 00:00 (see matrix below — in progress on branch `feature/shooting-assignment`) |
| **Course** | Photon Fusion 2 multiplayer (Unity 6, Shared Mode) |

## Assignment 1 — requirements matrix (shipped)

| # | Requirement (assignment) | Implementation | Status |
|---|--------------------------|----------------|--------|
| 1 | Create or join a room | `MainMenuUI`, `ConnectionManager.StartSessionAsync` | Done |
| 2 | Unknown player count (up to 10), dynamic join | `PlayerCount = 10`, late-join via `GameSceneReadiness` | Done |
| 3 | Lobby scene separate from game scene | `01_Lobby` → `02_Game`, `LobbyUI` Start Game | Done |
| 4 | 10 characters; request to MasterClient | `GameManager.RPC_RequestCharacter`, `CharacterSelectUI` | Done |
| 5 | Taken character visible to all players | `[Networked] NetworkArray<PlayerRef>`, `CharacterSlotButton` | Done |
| 6 | Master approves spawn at spawn point | `RPC_CharacterApproved`, `runner.Spawn` | Done |
| 7 | Reject if taken; player picks another | `RPC_CharacterRejected` | Done |
| 8 | Sync position and rotation | `NetworkTransform` on `PlayerAvatar` | Done |
| 9 | Spawn / Despawn objects | `BuilderTool`, `PlacedBlock` (E / Q) | Done |
| 10 | Global chat | `ChatManager` broadcast RPC | Done |
| 11 | Private message to one player (UI) | PM dropdown + whisper RPC in `ChatUI` | Done |
| 12 | Master ends game → menu → main menu | `GameOverUI`, `MasterSetGameOver`, `ShutdownToMainMenuAsync` | Done |
| 13 | Test with at least 3 clients | See `TESTING.md` scenarios A + B | Done |

### Bonus

| # | Requirement | Implementation | Status |
|---|-------------|----------------|--------|
| B1 | Nickname visible to all | `PlayerData.Nick`, lobby list, nameplate | Done |
| B2 | Unique color in chat and on player | `PlayerData.Tint`, chat tint, avatar material | Done |

## Assignment 3 — requirements matrix (8.7.26, solo — Daniil Gorlov)

| # | Requirement | Implementation | Status |
|---|-------------|----------------|--------|
| 1 | Hidden session (checkbox) | `SessionBrowserUI`, `SessionData.HiddenSession`, `ConnectionManager` `IsVisible` | Done |
| 2 | Started session visible in lobby but not joinable | `SessionCatalog.IsSessionStarted`, `SessionBrowserUI` disabled STARTED rows | Done |
| 3 | Session browser filtered by Game Mode | `SessionCatalog.LobbyNameForMode`, `ConnectionManager.RefreshSessionLobbyAsync` | Done |
| 4 | Preferred map filter with X-second fallback | `SessionBrowserUI` map timer, `SessionCatalog.MapFilterTimeoutSeconds` | Done |
| 5 | Character control via Input System + `FixedUpdateNetwork` | `GameplayInput`, `PlayerMovement`, `PlayerLook`, `PlayerWeapon` | Done |
| 6 | Spawn trigger objects (projectiles) | `Projectile`, `PlayerWeapon` | Done |
| 7 | Trigger ignores spawner | `Projectile.IgnoreShooterCollisions`, shooter skip | Done |
| 8 | Destroy spawned objects | `Projectile.DespawnResolved`, `BuilderTool` despawn | Done |
| 9 | Lock join when lobby → game | `SessionLock`, `LobbyUI`, `ConnectionManager.OnConnectRequest` | Done |
| 10 | Trigger collision on projectiles | `Projectile.OnTriggerEnter`, sphere sweep | Done |
| 11 | Local VFX on hit | `PlayerAvatar.OnHitCountChanged` → `SpawnLocalHitBurst` | Done |
| 12 | Collider owner fires RPC | `Projectile` → `PlayerAvatar.RpcRegisterHit` | Done |
| 13 | RPC updates `[Networked]` value | `HitCount`, `PlayerData.Score` | Done |
| 14 | `OnChangedRender` on networked value | `PlayerAvatar.HitCount`, `GameManager.IsGameOver` | Done |
| 15 | RPC validation (distance, source) | `PlayerAvatar.RpcRegisterHit`, `Projectile.ValidateHit` | Done |
| 16 | Session locked after game start | `SessionLock` (`IsOpen=false`, `phase=Started`) | Done |
| 17 | Hidden score; master sends results at end; master-migration-safe | `PlayerData.Score`, `RpcBroadcastGameOver`, `GameOverUI`, host migration | Done |
| 18 | Game rules support master client migration | `GameManager` snapshots, `IAfterHostMigration`, `ConnectionManager.OnHostMigration` | Done |
| B1 | Game Mode changes gameplay (Build / Combat / Sandbox) | `SessionRuntime` gates | Done |
| B2 | Networked Animator (≥3 states) | Optional bonus | Not started |

## How to verify before submission (Assignment 1)

1. Run **Tools → Fusion Multiplayer → Validate Setup** and **Validate UI In Scenes**
2. **Scenario A** — all 3 clients in lobby before Start Game (`TESTING.md`)
3. **Scenario B** — Player 3 joins **after** P1+P2 are in game (`TESTING.md` late-join test)
4. Console: no red errors; `[FusionMultiplayer][Game] ready` on late joiner

## How to verify before submission (Assignment 3)

1. MPPM: 3 virtual players — session browser, mode/map filters, hidden session
2. After master **START GAME**, 4th client **cannot** join (rejected or session shows **STARTED**)
3. **Combat** / **Sandbox**: LMB fires projectile; hit updates score (not shown live); master **End Game** shows results table
4. Host blocks cannot be broken by other players (`BuilderTool` rule from Assignment 1)

## Architecture (OOP / patterns)

| Layer | Responsibility | Key types |
|-------|----------------|-----------|
| **Core** | Session, scene authority, readiness | `ConnectionManager`, `GameManager`, `GameSceneReadiness` |
| **UI** | Display and input only | `CharacterSelectUI`, `ChatUI`, `LobbyUI` |
| **Bridge** | Decouple RPC callbacks from UI | `CharacterSelectBridge` |
| **Facade** | Resolve networked services | `GameSceneReadiness.TryGetGameManager` |
| **Observer** | Readiness and slot events | `GameManagerReady`, `CharacterSelectBridge.SlotsChanged` |
| **Player** | Movement and building | `PlayerAvatar`, `BuilderTool` |
| **Chat** | Messaging domain | `ChatManager`, `ChatUI` |

## Related docs

- Setup: [`SETUP.md`](SETUP.md)
- Testing: [`TESTING.md`](TESTING.md)
