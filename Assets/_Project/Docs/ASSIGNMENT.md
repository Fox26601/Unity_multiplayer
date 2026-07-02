# Assignment checklist — Fusion Multiplayer

## Submission metadata

| Field | Value |
|-------|--------|
| **Author** | Daniil Gorlov |
| **Due date** | 16 June 2026 (inclusive) |
| **Group** | Submit in groups of 3 unless otherwise approved by instructor — **note approval in submission if solo / different group size** |
| **Course** | Photon Fusion 2 multiplayer (Unity 6, Shared Mode) |

## Requirements matrix

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

## How to verify before submission

1. Run **Tools → Fusion Multiplayer → Validate Setup** and **Validate UI In Scenes**
2. **Scenario A** — all 3 clients in lobby before Start Game (`TESTING.md`)
3. **Scenario B** — Player 3 joins **after** P1+P2 are in game (`TESTING.md` late-join test)
4. Console: no red errors; `[FusionMultiplayer][Game] ready` on late joiner

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
