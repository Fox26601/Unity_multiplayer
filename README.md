# Unity Multiplayer (Photon Fusion 2)

**Author:** Daniil Gorlov (solo submission)

3D coursework prototype: **Shared Mode** lobby + game, master-approved character slots, chat (incl. whisper by nickname), spawn/despawn blocks, master game-over flow. Assignment 3 adds session browser, shooting, and scoring (see [`ASSIGNMENT.md`](Assets/_Project/Docs/ASSIGNMENT.md)).

## Quick links

- [Release checklist (single Play)](Assets/_Project/Docs/RELEASE.md)
- [Setup guide](Assets/_Project/Docs/SETUP.md)
- [Fusion import checklist (CS0246)](Assets/_Project/Docs/FUSION_IMPORT_CHECKLIST.md)
- [Testing checklist](Assets/_Project/Docs/TESTING.md)

## Requirements

- Unity **6000.3.x**
- **Photon Fusion 2** SDK + App Id (see SETUP)

After importing Fusion, run **Tools → Fusion Multiplayer → Generate Scenes, Prefabs, And Build Settings** (TMP UI), then register prefabs in the Fusion **Network Project Config**. If UI looks broken on old scenes, run **Fix UI Layout In Scenes** or regenerate.

Editor helpers (same **Tools → Fusion Multiplayer** menu): **Use Main Menu As Play Mode Start Scene**, **Validate Setup**, **Validate UI In Scenes**, **Fix UI Layout In Scenes**, **Ensure Game Scene Session Guard**.
