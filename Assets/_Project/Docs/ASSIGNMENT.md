# Final Project — Fusion Multiplayer (Dedicated Server / Client-Server)

## Submission metadata

| Field | Value |
|-------|--------|
| **Author** | Daniil Gorlov |
| **Group** | Solo |
| **Course** | Photon Fusion 2 multiplayer (Unity 6) |
| **Topology** | Dedicated Server (`GameMode.Server`) + Host create / Client join for editor play |
| **Branch** | `feature/final-project` |

## Scenes

| Scene | Role |
|-------|------|
| `00_Boot` | Opening bootstrap (`BootLoader`) → loads Main Menu |
| `00_MainMenu` | Profile, create/join, session browser, quick join, reconnect, career stats |
| `01_Lobby` | Player list; host/server starts match (clients may request start) |
| `02_Game_Arena` / `03_Game_Plaza` / `04_Game_Ruins` | Multiplayer gameplay maps |

## Mandatory requirements (40) — how implemented

| # | Requirement | Implementation |
|---|-------------|----------------|
| 1 | Open / join room, ≥3 players, configurable capacity | `ConnectionManager.StartSessionAsync`, `SessionData.MaxPlayers` (2–10), create UI dropdown |
| 2 | ≥3 RPCs with different variable types | e.g. `RPC_RequestCharacter(int, PlayerRef)`, `RPC_CharacterApproved(..., Vector3, Quaternion)`, `RPC_Broadcast(NetworkString)`, `RpcRegisterHit(float, PlayerRef, Vector3)` |
| 3 | JSON Serialize + Deserialize (≥3 fields) via RPC | `MatchConfigDto` → `JsonUtility` → `GameManager.RPC_SubmitMatchConfigJson(NetworkString<_512>)` → deserialize + ack |
| 4 | NetworkTransform | On `PlayerAvatar`, `Projectile`, blocks |
| 5 | New Input System | `GameplayInput` / `GameplayNetworkInput` + `OnInput` |
| 6 | Client can become master (N/A for dedicated) | Waived by assignment for Dedicated Server |
| 7 | Scene changes via master/Photon | `ConnectionManager.ServerStartMatch` / `Runner.LoadScene` on server |
| 8 | UI split host vs client | Lobby start authority messaging; create-room settings only on create page |
| 9 | Character select, unique slots, server owns occupancy | `GameManager.CharacterOwners`, server spawns avatar |
| 10 | ≥3 server-only random decisions | (1) spawn jitter `GetRandomizedSpawn`, (2) `MatchEventId`, (3) crit `RollDamage` + vote tie-break |
| 11 | End condition → results UI with scores | `MatchTimer` → `MasterSetGameOver` → `GameOverUI` |
| 12 | Synced score system | `PlayerData.Score` / `Deaths` `[Networked]` |
| 13 | Close room, return to menu, valid loop | `SessionLock`, `ShutdownToMainMenuAsync`, map vote restart |
| 14 | NetworkRunner.Spawn / Despawn | Avatars, projectiles, blocks, physics props |

## Bonus — how implemented

| Bonus | Points | Status | Implementation |
|-------|--------|--------|----------------|
| Room settings for creator | 6 | Done | Mode / map / difficulty / max players / hidden |
| NetworkMecanimAnimator | 8 | **Deferred** | Not claimed this submission |
| NetworkRigidbody3D | 5 | Done | `Networking.NetworkRigidbody3D` + `PhysicsProp` |
| ≥5 `[Networked]` vars | 7 | Done | Health, Score, slots, timers, votes, tokens, etc. |
| Session list + player counts | 5 | Done | `SessionBrowserUI` |
| Join error handling | 3 | Done | `FormatStartGameError`, refuse full/started |
| Local + remote disconnect | 3 | Done | `LocalDisconnectNotice` → `SessionFlowUI` |
| Crash reconnect + full control | 15 | Done | `ConnectionToken` + `SessionReconnectTokens` + `AssignInputAuthority` |
| Master announces start | 3 | Done | Lobby START → server `LoadScene` |
| Join validation vs master/server | 5 | Done | Token validation + JSON config RPC |
| Bot replaces disconnected player | 15 | Done | `BotTakeover` + `BotBrain` |
| Random join by ≥3 settings | 5 | Done | `QuickJoinAsync` |
| Dynamic matchmaking | 5 | Done | Periodic lobby refresh |
| Database read/write | 8 | Done | `MatchStatsDatabase` write + `CareerStatsHud` read |
| Dedicated Server | 55 | Done | `-dedicated` / `-room=Name` |
| Surprise | 10 | Done | Match event, physics props, map vote, Tab scoreboard, whisper |

## Dedicated server runbook

1. Build a **Server** (or normal) player with Photon AppId configured.
2. Launch: `MyGame.exe -batchmode -nographics -dedicated -room=Ded1` (macOS/Linux: same args after the binary).
3. Clients: open game → Join room name `Ded1` (same mode lobby) or create matching filters then join.
4. Any client can press **START GAME** (RPC to server). Server loads the map.
5. Logs: look for `[FusionMultiplayer] Starting dedicated server room`.

Editor shortcut: Host create (no `-dedicated`) still uses Client-Server authority for local testing.

## Setup after pull

1. Confirm Build Settings: `00_Boot` → MainMenu → Lobby → maps.
2. Fusion Hub: `PhysicsProp` labeled `FusionPrefab` (same as other network prefabs).
3. Optional: **Tools → Fusion Multiplayer → Wire PhysicsProp And Boot Only**.
4. Do **not** claim NetworkMecanimAnimator until that pass is done.

## Related docs

- Testing: [`TESTING.md`](TESTING.md)
- Setup: [`SETUP.md`](SETUP.md)
