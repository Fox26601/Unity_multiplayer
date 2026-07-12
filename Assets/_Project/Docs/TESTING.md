# Final Project — Testing

## Topology under test

- **Editor / local:** Create room uses `GameMode.Host`; join uses `GameMode.Client`.
- **Opening scene:** Play from `00_Boot` (Build Settings index 0) → Main Menu.
- **Dedicated:** Launch with `-dedicated -room=TestRoom` (`GameMode.Server`). Clients join the same room name.

## A — Create / join / lobby start (mandatory)

1. Client A: Main Menu → Create room (set max players ≥ 3, pick mode/map/difficulty).
2. Clients B, C: Join the same room (or Quick Join with matching filters).
3. Lobby shows all players; host/server presses **START GAME** (clients may request start).
4. All load the selected map; character select appears.

## B — Character slots + server spawn

1. Each player picks a different slot — avatar appears for everyone.
2. Attempt to pick a taken slot — reject message, no duplicate.
3. Confirm only one body per player (server spawn, not client spawn).

## C — JSON MatchConfig RPC

1. Enter game scene; console should log deserialized `MatchConfigDto` on server and ack on client.
2. Payload includes room, max players, mode, map, difficulty, token, nickname (≥3 fields).

## D — Combat / scores / timer / vote

1. Combat or Sandbox: shoot, confirm damage/kills.
2. Tab scoreboard while alive.
3. Wait for 5:00 timer (or End Game) → results + map vote → leave or new map.

## E — Disconnect + bot takeover

1. Mid-match: quit Client B abruptly.
2. Server keeps B’s avatar via character-slot ownership (Fusion clears `InputAuthority` on leave — lookup must not rely on it alone).
3. Nick becomes `BOT …`; `IsBotControlled` badge.
4. Bot AI runs in Fusion `FixedUpdateNetwork` via `PlayerMovement` (no NavMesh): **Patrol** → LOS → **Chase** / **Combat**; lose LOS → **Search** → **Patrol**. Whisker steering; auto-jump onto ~1-block ledges (apex 1.5 m); stuck recovery.
5. Newly placed build blocks are normal Physics colliders — bot/player can walk/jump on them immediately.
6. Local disconnect shows status via `SessionFlowUI` (`Disconnected: …`).

Manual checks: bot patrols across the map after disconnect; stand behind a corner (stops firing); step into LOS (chase + shots); place a block in front (auto-jump or steer around, then walk on top).

## E2 — Jump

1. In gameplay, press **Space** — jump apex ~1.5 m (clear a single 1.0 tile block).
2. Bot facing a 1-block ledge auto-jumps; taller walls are steered around.

## F — Crash reconnect

1. During a match, note room name; force-quit the client (do not use Leave — that clears the reconnect store).
2. Relaunch → Main Menu shows **Reconnect**.
3. Reconnect sends the same `ConnectionToken`; server restores `InputAuthority` on the existing avatar and disables the bot.

## G — Physics prop (animator deferred)

1. During match, cyan physics spheres spawn periodically and bounce (`NetworkRigidbody3D` / `PhysicsProp`).
2. NetworkMecanimAnimator — **not in this pass** (deferred).

## H — Database

1. Finish a match (timer or End Game).
2. Check `Application.persistentDataPath/fusion_match_stats.json`.
3. Return to Main Menu — **CAREER STATS** panel shows top rows (read path).

## I — Dedicated server smoke

1. Start server: `… -batchmode -nographics -dedicated -room=Ded1`
2. Two clients join `Ded1`, request start, play.
3. Confirm server is StateAuthority (clients cannot spawn networked objects themselves).
4. Kill one client → bot takeover; reconnect with Reconnect button → control restored.

## Regression checklist

- [ ] Boot → Main Menu loads
- [ ] Join room page: session list loads without `JoinLobby … JoiningLobby` console errors
- [ ] Mouse look smooth
- [ ] Projectiles despawn on environment / corpses
- [ ] Chat + whisper
- [ ] Build mode place/remove
- [ ] Session browser filters + started rooms not joinable (except known reconnect token)
- [ ] Career stats visible on main menu after at least one finished match
