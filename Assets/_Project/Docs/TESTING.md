# Final Project — Testing

## Topology under test

- **Editor / local:** Create and join both use `GameMode.Shared`.
- **Opening scene:** Play from `00_Boot` (Build Settings index 0) → Main Menu.
- **Offline:** Tools → Enable Offline Play → `GameMode.Single`.
- All MPPM peers are equal Shared clients; one becomes Shared Mode master.

## A — Create / join / lobby start (mandatory)

1. Client A: Main Menu → Create room (set max players ≥ 3, pick mode/map/difficulty).
2. Clients B, C: Join the same room (or Quick Join with matching filters).
3. Lobby shows all players; Shared master presses **START GAME** (clients may request start).
4. All load the selected map; character select appears.

## B — Character slots + Shared spawn

1. Each player picks a different slot — avatar appears for everyone (local SA+IA spawn after master approval).
2. Attempt to pick a taken slot — reject message, no duplicate.
3. Confirm only one body per player.

## C — JSON MatchConfig RPC

1. Enter game scene; console should log deserialized `MatchConfigDto` on master and ack on client.
2. Payload includes room, max players, mode, map, difficulty, token, nickname (≥3 fields).

## D — Combat / scores / timer / vote (master RNG)

1. Combat or Sandbox: shoot — projectiles smooth on all clients (NetworkTransform TRSP).
2. No mid-air hang; no `InvalidOperationException` on `Projectile.Spent`.
3. Damage/kills work — crit roll happens on **Shared master** (`GameManager.RPC_RequestHit` → `CombatRules.RollDamage`).
4. Tab scoreboard while alive.
5. Wait for 5:00 timer (or End Game) → results + map vote → leave or new map.

## E — Disconnect + bot takeover

1. Mid-match: quit Client B abruptly.
2. Shared master keeps B’s avatar; nick becomes `BOT …`.
3. Console: reconnect window open (`SessionLock: Reconnect window open…`).
4. Bot AI runs (patrol / chase / combat).

## E2 — Jump

1. Press **Space** — jump apex ~1.5 m.
2. Bot auto-jumps 1-block ledges.

## E3 — Master migration (#6)

1. Note who is Shared master.
2. Quit the master client.
3. Remaining peer logs `Became Shared Mode master`; timer / slots / votes continue.
4. New master can still resolve game-over / votes.

## E4 — Hard-close (#13)

1. After START, from a third client try Join same room name **without** reconnect token → refuse / GameClosed.
2. Session browser / Quick Join must not treat the started room as an open lobby.

## F — Crash reconnect (window)

1. Mid-match force-quit (or Leave mid-match — token kept).
2. Remaining peer opens reconnect window (~90s); console: `SessionLock: Reconnect window open…` and it must **not** close on the next tick.
3. Main Menu → **Reconnect** within the window → bot body **despawns immediately**; former slot is **FREE**; **one** PlayerData kept (no duplicate).
4. Character select → pick any free slot (including the old one) → one new avatar, full control.
5. After reclaim (no bot seats left), room hard-closes again (`IsOpen=false`).
6. **Window expiry (no reclaim):** after ~90s the room hard-closes (`IsOpen=false`); abandoned bot body + `PlayerData` are despawned; that token is no longer known; late join without a valid token is refused.
7. Peer without known token cannot join during the window.

### F2 — Master leave then reconnect

1. Player 1 (creator/master) force-quit mid-match.
2. Player 2: `Became Shared Mode master` then `Reconnect window open` (may be deferred one tick).
3. Player 1 → Reconnect → bot despawned, slot FREE → pick character → full control.

### F3 — Non-master leave then reconnect

1. Player 2 force-quit mid-match while Player 1 remains master.
2. Player 1 console: `Reconnect window open` (no immediate hard-close).
3. Player 2 → Reconnect → bot despawned, slot FREE → pick character; no second PlayerData.

### F4 — Mid-match change character

1. In match with a spawned avatar: Esc → Pause → **Change character** → panel **stays open** until you click a slot.
2. Pick a **different** free slot → old body despawns; only one avatar remains.
3. Same-slot (YOU) or Resume from pause cancels reselect without a double body.

## G — Physics prop (animator deferred)

1. Cyan physics spheres spawn periodically (`Networking.NetworkRigidbody3D`).
2. NetworkMecanimAnimator — **not claimed**.

## H — Database

1. Finish a match — `fusion_match_stats.json` written.
2. Main Menu → Leaderboard shows rows.
3. Duplicate nickname rejected.

## Regression checklist

- [ ] Boot → Main Menu loads
- [ ] Shared Mode create/join (not Host)
- [ ] Projectiles smooth; no Spent hang
- [ ] Master-only crit (hits go through GameManager)
- [ ] Hard-close after START; unknown join refused
- [ ] P2 force-quit → window open stays → Reconnect works
- [ ] P1 (master) force-quit → P2 become-master + window → P1 Reconnect works
- [ ] Disconnect → bot + reconnect window
- [ ] Reconnect within window: bot despawn + FREE slot; window closes after reclaim
- [ ] Reconnect = one PlayerData; pick slot → one avatar (no duplicate PD)
- [ ] Mid-match Change character → other slot → old body despawned
- [ ] Master leave → `OnBecameSharedMaster`
- [ ] Leave → Main Menu loop
- [ ] Career Leaderboard after match
- [ ] Tools → Validate Setup / Validate UI
