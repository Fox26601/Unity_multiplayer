# Final Project — Fusion Multiplayer (Shared Mode)

## Submission metadata

| Field | Value |
|-------|--------|
| **Author** | Daniil Gorlov |
| **Group** | Solo |
| **Course** | Photon Fusion 2 multiplayer (Unity 6) |
| **Topology** | Shared Mode (`GameMode.Shared`) — create and join; offline `GameMode.Single` |
| **Branch** | `feature/final-project` |

Canonical requirements: [Google Doc](https://docs.google.com/document/d/1w7e_-Kr-uR0px0DKwj6c2cYf_-0i0CTuQdyCFdwp9iA/edit?tab=t.0). This file is an implementation map only.

## Scenes

| Scene | Role |
|-------|------|
| `00_Boot` | Opening bootstrap (`BootLoader`) → loads Main Menu |
| `00_MainMenu` | Profile, create/join, session browser, quick join, reconnect, career stats |
| `01_Lobby` | Player list; Shared master starts match (clients may request start) |
| `02_Game_Arena` / `03_Game_Plaza` / `04_Game_Ruins` | Multiplayer gameplay maps |

## Mandatory requirements (40) — how implemented

| # | Requirement | Implementation |
|---|-------------|----------------|
| 1 | Open / join room, ≥3 players, configurable capacity | `ConnectionManager.StartSessionAsync`, `SessionData.MaxPlayers` (2–10), create UI dropdown |
| 2 | ≥3 RPCs with different variable types | `RPC_RequestCharacter(int)`, `RPC_CharacterApproved(..., Vector3, Quaternion)`, `ChatManager.RPC_Broadcast(NetworkString)` |
| 3 | JSON Serialize + Deserialize (≥3 fields) via RPC | `MatchConfigDto` → `JsonUtility` → `GameManager.RPC_SubmitMatchConfigJson` → deserialize + ack |
| 4 | NetworkTransform | On `PlacedBlock` and **`Projectile` (TRSP)**. Player locomotion uses Fusion `NetworkCharacterController` (TRSP). Physics props use custom `NetworkRigidbody3D` (bonus) |
| 5 | New Input System | `GameplayInput` / `GameplayNetworkInput` + `OnInput` |
| 6 | Client can become master + catch event | Shared Mode: `GameManager.OnBecameSharedMaster` on `IsSharedModeMasterClient` / SA transition; `MasterClientObject` on GameManager/ChatManager |
| 7 | Scene changes via master/Photon | `ConnectionManager.ServerStartMatch` / `Runner.LoadScene` on Shared master |
| 8 | UI split master vs client | Different lobby status strings; create-room settings on create page; clients may request START |
| 9 | Character select, unique slots, master owns occupancy | `GameManager.CharacterOwners` (`RpcInfo.Source`); requester spawns avatar with SA+IA |
| 10 | ≥3 master-only random decisions | (1) spawn jitter (2) `MatchEventId` (3) crit via `CombatRules.RollDamage` on master in `RPC_RequestHit` (4) vote tie-break |
| 11 | End condition → results UI with scores | `MatchTimer` → `MasterSetGameOver` → `GameOverUI` |
| 12 | Synced score system | `PlayerData.Score` / `Deaths` `[Networked]` (survives master change) |
| 13 | Close room, return to menu, valid loop | Match start **hard-close** (`IsOpen=false`); Leave → `Shutdown` → Main Menu; reconnect uses timed window (bonus 8) |
| 14 | NetworkRunner.Spawn | `NetworkSpawnFactory` + fallbacks → `Runner.Spawn` (avatars, projectiles, blocks, props, PlayerData) |
| 15 | NetworkRunner.Despawn | Projectiles, avatars on rematch / seat release, blocks, duplicate PlayerData |

## Bonus — how implemented

| Bonus | Points | Status | Implementation |
|-------|--------|--------|----------------|
| Room settings for creator | 6 | Done | Mode / map / difficulty / max players / hidden |
| NetworkMecanimAnimator | 8 | **Deferred** | Not claimed this submission |
| NetworkRigidbody3D | 5 | Done | Custom SA pose/velocity sync `Networking.NetworkRigidbody3D` on `PhysicsProp` |
| ≥5 `[Networked]` vars | 7 | Done | Health, Score, slots, timers, votes, tokens, etc. |
| Session list + player counts | 5 | Done | `SessionBrowserUI` |
| Join error handling | 3 | Done | `FormatStartGameError`, refuse full/started |
| Local + remote disconnect | 3 | Done | `LocalDisconnectNotice` → `SessionFlowUI` |
| Crash reconnect + full control | 15 | Done | Hard-close + **reconnect window** + known `ConnectionToken`; reclaim `PlayerData`, **despawn bot body**, free slot, character re-pick + SA/IA |
| Master announces start | 3 | Done | Lobby START → master `LoadScene` |
| Join validation vs master | 5 | Done | Token / phase / reconnect-window gate in `OnConnectRequest` + MatchConfig JSON RPC |
| Bot replaces disconnected player | 15 | Done | `BotTakeover` + `BotBrain` (master) |
| Random join by ≥3 settings | 5 | Done | `QuickJoinAsync` |
| Dynamic matchmaking | 5 | Done | Periodic lobby session-list refresh |
| MasterClientObject | 3 | Done | `GameManager` + `ChatManager` scene NetworkObjects (`Flags` MasterClientObject) |
| Database read/write | 8 | Done | Local JSON `MatchStatsDatabase` + `CareerStatsHud` |
| Dedicated Server | 55 | **Removed** | Not claimed — Shared Mode only |
| Host Mode / Host migration | 25/12 | Not claimed | Shared topology |
| Surprise | 10 | Done | Physics props, map vote, Tab scoreboard, whisper |

## Shared Mode + reconnect window

- Create/join → `GameMode.Shared`.
- Match start → `phase=Started`, **`IsOpen=false`** (Google Doc #13).
- Unexpected disconnect → master opens ~90s window (`IsOpen=true` + `reUntil`); `OnConnectRequest` accepts **only known tokens**.
- Reclaim: keep `PlayerData` by token, despawn bot avatar, clear slot → player re-picks character (full control).
- After reclaim / window expiry / no bot seats → hard-close again.
- Leave / end → `Runner.Shutdown` → Main Menu.

## Setup after pull

1. Confirm Build Settings: `00_Boot` → MainMenu → Lobby → maps.
2. Fusion Hub: network prefabs registered (`Projectile` includes NetworkTransform).
3. Run **Tools → Fusion Multiplayer → Create Default Config Assets** if `Resources/Configs/GameConfigs` is missing.
4. Do **not** claim NetworkMecanimAnimator until that pass is done.

## Audit status (code walk vs Google Doc)

Date of last walk: 2026-10-21. Canonical Doc wins over this map.

### Mandatory #1–15

| # | Result | Notes |
|---|--------|--------|
| 1 | Pass | Create/join Shared; capacity clamp 2–10 (supports ≥3) |
| 2 | Pass | Character int, approve Vector3+Quaternion, chat NetworkString (+ more RPCs) |
| 3 | Pass | `MatchConfigDto` ≥3 fields via `RPC_SubmitMatchConfigJson` |
| 4 | Pass | `NetworkTransform` on Projectile + PlacedBlock |
| 5 | Pass | New Input System → `GameplayNetworkInput` / `OnInput` |
| 6 | Pass | `OnBecameSharedMaster`; GameManager/ChatManager `Flags` include MasterClientObject |
| 7 | Pass | Master `LoadScene` on start |
| 8 | Pass | Create settings on creator UI; lobby start gated for non-master |
| 9 | Pass | `CharacterOwners` on master; unique slots |
| 10 | Pass | Spawn jitter, MatchEventId, master crit roll, vote tie-break |
| 11 | Pass | Timer / end → `GameOverUI` with scores |
| 12 | Pass | Networked Score/Deaths on PlayerData |
| 13 | Pass | Hard-close on start; Leave → Main Menu; reconnect window is bonus path |
| 14 | Pass | All spawns still call `Runner.Spawn` (factory + SerializeField fallback) |
| 15 | Pass | Despawn on projectiles, rematch, seat release, blocks |

### Claimed bonuses

Reconnect, bot takeover, session browser, quick-join filters, DB JSON, MasterClientObject, custom NRB, join validation — code paths present. NetworkMecanimAnimator / Dedicated Server / Host Mode — not claimed.

### Docs fixed in this audit

- `PRESENTATION.html`: removed outdated soft-lock / “no NetworkTransform on projectile” Q&A; aligned with hard-close + NT projectile.
- Comment/docs wording scrubbed where it read like filler (“facade over”).

### Manual playtest still required

Follow [`TESTING.md`](TESTING.md) A–F4 (especially reconnect bot despawn → FREE slot → re-pick).

### Git history note

Older commits may contain `Co-authored-by: Cursor` trailers. Do not amend pushed history unless explicitly requested; new commits must use author identity only with no AI trailers.

## Related docs

- Setup: [`SETUP.md`](SETUP.md)
- Testing: [`TESTING.md`](TESTING.md)
- Release: [`RELEASE.md`](RELEASE.md)
- Architecture: [`ARCHITECTURE.md`](ARCHITECTURE.md)
