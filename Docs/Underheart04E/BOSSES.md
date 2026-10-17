# Underheart bosses and the Extraction Choir (04E, BOSSES stream)

Status: source implemented and compiled in all twelve configurations, pure rules covered by offline EditMode tests.
**Nothing here has been run in Unity.** How the bodies look, how the fights feel, frame rate and difficulty are not
verified; the native checklist at the end says exactly what to look at.

Source: `Assets/NeonSyndicate/Scripts/Runtime/Creatures/UnderBosses/`. Tests:
`Assets/NeonSyndicate/Tests/EditMode/UnderBosses04ETests.cs` (52 tests).

## What the player meets

| Encounter | Where (Underheart anchor) | Phases | Lesson (what the reward expresses) | First-win reward (token) |
|---|---|---|---|---|
| Valve Guardian (miniboss) | `canals.valve_room` | 2 | Ward direction: a frontal steam jet, then the valve wheel on its back vents | `uh_descent_piece` (`boss04e:valve_guardian`) |
| The Cinder Matron | `gardens.matron_arena`, seal at `gardens.matron_gate`, lures at `gardens.lure_a/b/c` | 3 | Shielding and cooling: lures, cooled plates, a wardable breath | `uh_matron_aspect` (`boss04e:cinder_matron`), `uh_matron_gear_choice` (`boss04e:cinder_matron:gear`) |
| The Vaultbreaker | `workshops.vault_arena`, seal at `workshops.vault_gate` | 3 | Positioning and fracture: flank the seams, break one, the core opens | `uh_vault_module` (`boss04e:vaultbreaker`) |
| The Kiln Regent | `crown.regent_arena`, seal at `crown.regent_gate` | 3 | Bond, movement and regulator: safe separation, furnaces, beam lanes, the exposed regulator | `uh_regent_pinnacle` (`boss04e:kiln_regent`), `uh_bond_awaken` (`boss04e:kiln_regent:bond`) |
| Choir Valve | `crown.valve_mech` | | Defend a valve operation against three announced waves | (the quest's Claim) |
| Choir Reflectors | `crown.reflector_mech` | | Turn visible reflectors to bend a beam into a receptor | (the quest's Claim) |
| Choir Escort | `crown.escort_start` to `crown.escort_end` | | Keep a maintenance construct alive on a short safe path | (the quest's Claim) |

## Interface (Game.UnderBosses, frozen signatures kept)

- `Begin(bossId, arena, yaw, radius, out reason)` returns a `BossFightHandle`. The radius is clamped to 12 to 60 m.
  The boss waits **dormant** behind a glowing boundary until the player walks in (or strikes it from close by), then
  plays a 2.6 s intro and fights. Asking for the same boss again while it runs returns the same live handle (a
  repeated quest callback never builds a second fight). Another boss or a running Choir mechanism refuses with a
  reason. An unknown id, a bad position or a failed build returns a handle with `Failed = true` and the reason.
- Handle: `Done` on a confirmed death only; `Failed` with a plain `Status` when the player dies, leaves the arena,
  withdraws through the seal, the quest aborts (`Abort()`), a save loads, the game returns to the title, or the build
  failed. `Health01` and `Phase` follow the fight. `Abort()` after a victory does nothing (the aftermath tidies itself).
- `EverDefeated(bossId)`: true once the boss's first-win token is in the reward ledger (or it was won this session
  while the ledger was unavailable).
- `FightActive`: true while a fight is dormant, in its intro or fighting.
- `BeginMechanism(id, anchor, yaw, out reason)` returns a `MechanismHandle` (`Objective` is the live instruction).
  Same rules: the same id returns the live handle, anything else running refuses.
- `Defeated(bossId)`: raised once per confirmed victory (each listener wrapped).
- Added (read only or optional): `ActiveFight`, `ActiveMechanism`, and
  `TryAssistTarget(from, aimPoint, maxRange, out point, out IDamageable)` for the BONDS stream's combat assist (the
  bodies are not CharacterBase, so an aim check by CharacterBase will not find them; this hook returns the boss's
  damage receiver when the aim point is on the boss with a clear line).
- `UnderheartBosses` is an `IWorldSystem` with no save block: `ResetAll` (new game, load, title) ends whatever runs
  without a reward. Nothing about a fight is serialized; a reloaded checkpoint simply begins the encounter again.

## How a fight runs (UnderBossFight)

1. **Dormant**: body built and idle, boundary posts cyan, an admission lease `uboss04e:<id>` holds the arena
   (radius + 20 m) so ambient producers that ask `LocalQuiet.Blocks` stay out. HUD: "Cross the glowing boundary to
   face ..." and the counter line.
2. **Intro** (2.6 s): banner with the lesson, posts turn amber.
3. **Fighting**: one beat at a time.
   - Telegraph: the HUD names the move and fills a bar, a "!!" pulses over the boss, the body glows, and the floor
     (rings, lanes) or the air (aim lines) shows where it lands. Lanes lock when the wind-up starts; aimed shots follow
     the player at a limited turn rate during the wind-up and lock at the commit. Nothing predicts the player's motion
     and nothing reads input (`UBossView` holds only observable facts; a test guards its field list).
   - Commit, then the active part (bursts, a dash, a breath, beams).
   - Punish window: a shrinking bar, the window's line on the boss bar, rings on the parts to strike. Windows only ever
     extend, never shorten (PunishWindow04E).
   - Phases open once each through `BossPhaseLatch04D` (a burst past both thresholds enters the last and skips the
     middle phase's event). A phase change breaks a move still winding up.
   - Reinforcements come only from the profile's declared batches (each batch fires once, `AddLedger04E` caps the
     total and the bodies alive, a death never refunds). Each batch is announced with a banner.
4. **Victory** (a confirmed death while fighting, once through `VictoryLatch04E`): every owned hazard is released,
   every loop it started stops (combat audio), living reinforcements leave with a puff, the seal opens, a green
   marker stands at the exit (the gate, or where the player came in) with an "EXIT" ring on the HUD, the admission
   lease is released and a quiet aftermath lease `uboss04e:<id>:aftermath` starts (`LocalQuiet.AcquireAftermath`,
   radius + 10 m), `Game.Underheart.MarkCleared(roomId)` is called, the first-win entitlements are granted and
   `Defeated` fires. The husk stays as the quiet aftermath; the marker and boundary go when the player leaves (or after
   four minutes), the husk when the player is 150 m away (or after eight minutes) or another fight starts.
5. **Any other ending**: hazards, loops, reinforcements, leases and every object go at once; the handle reports why.

### Arena boundary and seal

- 10 to 28 short pylons on the boundary circle (one mesh, one renderer), probed onto the floor (skipped where there is
  no floor or a wall), joined by a low translucent band. Cyan while dormant, amber in the fight (pulsing while the
  player is outside), green after victory.
- Leaving: beyond radius + 3 m the HUD counts down 6 s ("LEAVING THE ARENA"); out longer, or 40 m out, or 25 m above
  or below, resets the fight (`ArenaWatch04E`).
- Seal: only when the profile's gate anchor resolves within 1.6 radii. It closes once the player is 4 m inside and in
  front of it; it is a 7 x 5 m Debris-layer box (blocks walking bodies, not vehicles or the camera). Holding against it
  from inside for 2.5 s withdraws on purpose (the fight resets). Every ending removes it, so it can never trap anyone.

### Damage

- The body carries a `CharacterHealth` (hit markers, explosions, crystal shots and guns all find it through
  `IDamageable`) whose `Filter` is the fight. Only the player's side counts (`DamageInfo.FromPlayer`: the player, a
  companion or a fused vehicle, credited once to the player). The struck part is the nearest tagged collider
  (`UBossPartTag`), scaled by `UnderBossProfiles.PartMultiplier` and the element (weakness 1.35, resistance 0.65).
  Every multiplier is at least 0.15: nothing is an invulnerable sponge. Bosses never burn (fire immunity, no free
  burning ticks).
- Hits on the player use the profile damage, capped per hit by the difficulty exactly like creature hits
  (`CreatureBrainLogic.CapHit`), with the ring respite so a volley cannot chain a stagger lock. The direction always
  runs from the source to the player, so a raised ward catches breath, steam and lane hits by angle (ground rings are
  Unblockable). A player in a vehicle takes it on the vehicle.
- Rings and lanes reach 3 m above their floor; a flyer above that is safe from them, and every boss switches to a move
  that reaches the air (aimed volleys, the steam lance) when the player flies.

## The bosses

### Valve Guardian (miniboss, `ValveGuardianFight`)

Body: a 3.7 m boiler drum on four stub legs, brass bands and rivets, a pressure gauge face, a steam nozzle, two pipe
arms with clamps, twin stacks, and the **valve wheel on its back** (weak point: it spins and glows while venting). One
shadowless light at the valve.

| Move | Telegraph (Balanced) | Window | Notes |
|---|---|---|---|
| Steam jet (signature) | 1.1 s lane, 14 m x 2.8 m | 2.2 s, valve x2.4 | Fire type, wardable; locked when the wind-up starts |
| Pressure stomp | 0.95 s ring 5.2 m | none | when crowded |
| Scalding spray | 1.0 s, 3 rings (4 in phase 2) | none | |
| Steam lance | 1.1 s aim line then a 0.6 s beam | 1.4 s | the anti-air move |

Phase 2 (at half health), OVERPRESSURE: two marked vent rings burst every 8 s (10 s Story); 2 ember scavengers
(the whole budget), announced.

### The Cinder Matron (`CinderMatronFight`)

Body: a creature-kit quadruped (CreatureBuilder with her own CreatureLook: heavy, sprawling, obsidian scales, amber
plates, a crowned flat head, a crystal-blade tail, about 2.2 m at the shoulder and 5.4 m long), driven by CreatureRig
and CreaturePose from this controller (never a Creature, never in `Creature.All`). Her **ember heart** is an extra
burning crystal on her back (weak point) with one light. Hit volumes: crystal cores and the heart Weak, trunk Shell,
head and neck Body, legs and tail Limb.

Arena props: 3 lures (at the lure anchors when the Underheart has them within 1.25 radii, else spread on the arena),
3 cooled crystal plates (2.6 m radius, at half radius).

| Move | Telegraph | Window | Notes |
|---|---|---|---|
| Ember charge (signature) | 1.2 s lane to the arena edge or wall | 3.2 s quench (shell cracked, heart x2.4) if it crossed a ready plate, else 1.2 s | the plate then recharges 22 s (16 Story) |
| Fire breath | 1.2 s three-lane cone | 1.6 s, heart x2.2 | ticks every 0.25 s in a 30 degree cone, wardable |
| Tail sweep | 0.9 s ring 6.5 m | none | |
| Ember rain (phase 2+) | 1.0 s, 4 rings | none | leaves burning ground 4 s |
| Ember volley | 1.0 s aim lines (3) | none | anti-air, 3 aimed shots |
| Cinder nova (phase 3 signature) | 1.8 s (2.3 Story), ring 0.92 radius | 3.5 s exhausted, heart x2.4 | safe only on a cooled plate |

Lures: while any burns she is LURED (0.6 times damage, shorter gaps between moves). Shoot a lure's crystal (160 HP
Balanced) or shut it down with the interact prompt. Phase 2 relights one shut lure once (60 percent health),
announced. Phase 2 also brings 3 ember scavengers (the whole budget).

### The Vaultbreaker (`VaultbreakerFight`)

Body: a furnace hull on four jointed legs (thigh, shin, foot) with two hammer arms in front, a sensor head, sloped
**front plates** (x0.3), a **core** behind sliding hatch doors, glowing **seams** on both flanks (x1.5, building
fracture) and one across the back (wakes in phase 2), exhaust stacks and a rear grill. One light at the core. It turns
at 38 degrees per second (30 Story, 44 Veteran), so flanking works.

| Move | Telegraph | Window | Heat |
|---|---|---|---|
| Hammer | 1.05 s lane 9 m | 1.4 s, seams x1.9 | +22 |
| Arm sweep | 0.95 s ring 6 m | none | +18 |
| Furnace shot | 1.0 s ring at the player (a mortar, burning ground 3 s), or aim lines and 2 aimed shots for a flyer or a vehicle | none | +26 |
| Shard fan (phase 2+) | 1.1 s, 5 lanes, 16 degrees apart | 1.5 s, seams x1.9 | +30 |
| Overheat (signature, at 100 heat) | 1.4 s ring 7.5 m ("get clear") | 3 s, core x2.4 (hatch open) | resets to 0 |

A seam breaks after 260 fracture (200 Story, 330 Veteran): the hatch slides open for a 2.6 s core window; a broken
seam stays at x1.8. Phase 2: 2 repair constructs (announced) mend unbroken seams at 20 fracture per second each while
alive within 14 m (the bar says so). Phase 3: heat x1.35 per move (it overheats sooner) and 2 ash burrowers
(announced). Declared total: 4.

### The Kiln Regent (`KilnRegentFight`)

Body: an 8 m colossus on an octagonal pedestal (the pedestal stays still while the torso turns at 30 degrees per
second), a caged **regulator** in the chest, pauldrons, twin chimneys, a masked head and an emitter **crown** of six
prongs, two long arms ending in kiln hammers. Arena props: 4 furnaces at 0.62 radius, each with a glowing vent valve.
One realtime light (the regulator's key light, brighter once the cage lifts); the furnaces burn on emissive grills and
vents with fire puffs only (they used to add two more lights while burning, see Light budget below).

| Move | Telegraph | Window | Notes |
|---|---|---|---|
| Resonance disruption (opening, signature) | 1.6 s, two expanding harmless rings | 2.4 s, crown x2.2 | fused: asks `Game.Fusion.RequestSeparate`; refused: says why and retries every 3 s; not fused: "your bond holds". Repeats only while the player is fused (at most every 10 s) |
| Kiln hammer | 1.1 s lane | none | |
| Pedestal sweep | 1.0 s ring 7.5 m | none | |
| Ember volley | 1.0 s aim lines (3) | none | anti-air |
| Furnaces (phase 2+) | 1.4 s, rings at the pair | starved 3 s (x1.8, regulator x2.4 once exposed) when both are quenched | two opposite furnaces burn 25 s at most, ring the floor every 2.6 s, shield the Regent (x0.45) |
| Beam lanes (phase 2+) | 1.4 s floor lanes, then 1.6 s beams at waist height | 2 s | 4 lanes (5 in phase 3), the gaps are safe |
| Regulator beams (phase 3) | 1.2 s, 3 lanes fanned at the player | 1.6 s, regulator x2.4 | |

Furnaces and beam lanes alternate. A furnace vent has 140 HP (110 Story, 170 Veteran); shooting it out or using the
interact prompt quenches it. Phase 3 lifts the cage: the regulator takes x2.4 for the rest of the fight. Adds: 2
extraction sentinels (phase 2), 2 furnace casters (phase 3), announced. The contain or shutdown choice is the quest's
Decide step after victory.

## The Extraction Choir (ChoirMechanism)

All three hold the area with `uboss04e:mech:<id>` while running and leave a quiet aftermath on success; props keep
their solved look for up to three minutes or until the player is 120 m away. Player death, abort, load or title fail
the handle and remove everything. A loss **inside** a mechanism resets the mechanism, never the quest; each new attempt
declares a fresh finite wave budget.

- **Choir Valve** (`ChoirValve`, `ValveOperation04E`): start at the wheel (interact prompt). It opens over 40 s (35
  Story, 45 Veteran) only while the player is within 14 m. Waves at 0, 35 and 70 percent: 3 ember scavengers; 2 ember
  scavengers and a furnace caster; 2 extraction sentinels (8 declared). Each attacker within 4 m of the valve wears 6
  integrity per second; at 0 the valve seizes, the attempt's attackers leave and the operation waits to be started
  again. Done when fully open and every attacker that came is confirmed dead.
- **Choir Reflectors** (`ChoirReflectors`, `ReflectorPuzzle04E`): local frame of the anchor (x right, z forward, m):
  emitter (-9, -7.5) firing +z, reflectors A (-9, 6), B (0, 6), C (0, 0), D (9, 0), a pillar (0, -7.5), receptor
  (9, -7.5). Each reflector turns 45 degrees per use. The only solution is A 45, B 135, C 135, D 135 degrees (a test
  counts all 256 states); it starts at 0, 45, 90, 0 (unsolved). The beam is traced with the same maths, drawn segment
  by segment and cut at the room's first wall. If the solved path would cross a wall, the layout shrinks to 0.8 or
  0.65 scale; if it is still blocked, the HUD says the room's wall blocks the beam. Touching the beam stings (small,
  capped, once a second). The receptor must stay lit 2 s.
- **Choir Escort** (`ChoirEscort`, `EscortRun04E`): the path is the Underheart's own route when offered and clear,
  else the straight line, else a dog-leg clearing the walls. The construct walks 2.2 m/s only while the player is
  within 9 m (it waits and blinks otherwise). Waves at 25 and 60 percent spawn 12 m ahead (2 ember scavengers; a
  glasswing ambusher and 2 ember scavengers; 5 declared). Each attacker within 3 m costs it 8 health per second; at 0
  it breaks, the attempt's attackers leave and it reboots at the start after 3 s.

## HUD (`UnderBossHud`, HudOverlays key `uboss04e`)

Boss bar in the shared encounter slot near the top (`HudLayout04E`; the bottom strip belongs to the quest objective) (name, phase or window line, health with the phase marks, the live mechanic line under it),
the move label and wind-up bar above it with a "!!" over the boss, the window line with a shrinking bar, rings with
words on lures, vents, seams, the core, the crown, the regulator and the plates when they matter, the leaving
countdown, the withdrawal bar, the exit ring after victory, and the Choir panel (title, objective, progress, a status
line, rings on reflectors, the receptor, the construct and the destination). Words and shapes, never colour alone.

## Debug page (F2 > "Underheart bosses", Order 62)

Start any boss a radius plus 4 m ahead of the player (walk in to begin), or at its own Underheart arena anchor (travel
there with the Underheart page); push past the next phase threshold; open the punish window; take a debug victory (it
still grants each first-win token only once); abort. Start any Choir mechanism ahead; solve the reflectors; abort.
The page also lists each boss's first-win state and any grant still pending.

## Lock-on, aim assist and damage numbers (04E integration)

- The bosses are not Creatures, so the lock-on (`PlayerCombat.FindLockTarget`) and the pad aim assist
  (`PlayerCamera.UpdateAimAssist`) never found them. Each fight now builds a lock-on stand-in, `UBossLockProxy04E` (a
  `CharacterBase` on a child of the body with no collider, registered nowhere, its `Health` the body's
  `CharacterHealth`): the lock-on scores it like an engaged creature (angle, distance to the body's edge, line of
  sight), the aim assist offers it while the fight is on, the reticle shows the boss's health, the melee lunge stops at
  the body's edge and the camera frames it. Hits still land on the tagged colliders, and `CharacterHealth.Owner` stays
  empty, so the damage path (filter, part multipliers, rewards) is unchanged.
- The stand-in's aim point follows the best part to strike (`UBossLockRules04E`, re-chosen 5 times a second): the part
  kind with the highest multiplier right now by the boss's own table, the open punish window included, the body on ties.
  In practice: the Guardian's valve wheel, the Matron's ember heart, the Vaultbreaker's nearest seam (its core once the
  hatch opens), the Regent's crown while it vents and its regulator once the cage lifts. Among several colliders of that
  kind the one nearest the camera's aim wins, and the current one is kept unless another is more than 10 degrees nearer.
- Companion and fused assists: `TryAssistTarget` falls back to the lock point when the player is locked on and the aim
  finds nothing, and a strike on the body goes to the lock point (the weak point) when the player is locked on or aims
  within 2.5 m of it.
- Damage numbers: the body's `Damaged` event reports every hit the filter let through (`DamageNumbers.ReportAt`, the
  applied amount, at the struck point, one merging number per part, yellow on a weak point or an open core); lures and
  furnace vents (`UBossTarget`) report theirs too. Companion and fused-vehicle hits are credited to the player, so they
  show the same way.
- Files: `UBossLockProxy04E.cs`, `UBossLockRules04E.cs`, `UnderBossFight.Lock.cs`; hooks: four lines in
  `PlayerCombat.FindLockTarget`, one line in `PlayerCamera.UpdateAimAssist`; `DamageNumbers.ReportAt`. Tests:
  `BossFeedback04ETests`.

## Light budget (BossLightBudget04E)

| Fight | Realtime lights created | On at once | Shadows | Roles |
|---|---|---|---|---|
| Valve Guardian | 1 | 1 | none | the valve's key light (brighter while it vents) |
| Cinder Matron | 1 | 1 | none | the ember heart (brighter in a window) |
| Vaultbreaker | 1 | 1 | none | the core (brighter with the hatch open) |
| Kiln Regent, before | 5 | 3 | none | the regulator, and one per furnace while burning |
| Kiln Regent, now | 1 | 1 | none | the regulator; the furnaces are emissive only |
| Choir mechanisms | 0 | 0 | none | |

Every fight light goes through `UnderBossFight.KeyLight` (one per fight, shadowless, Auto render mode, refused with a
log line beyond the budget); `BossFeedback04ETests` also scans the boss sources so no fight can add a light another way.
No attack telegraph depends on a light: telegraphs are rings, lanes, beams and emissive ramps, and the short impact and
commit flashes come from the shared, fixed pool of eight in `Fx` (not counted per fight). Near the Regent the total is
the region's 8 practical lights (5 on Low, `UnderheartWorld.UpdateLights`) plus the key light: 9 (6 on Low), down from
11 (8 on Low). Details and the comparison with the rest of the game: `ART_AND_PERFORMANCE.md`.

## Statics, budgets, logging

- One reset hub: `UnderBossStatics.ResetStatics()` (profiles, timing, telegraph pools and materials, HUD scratch, the
  Matron's cached creature body), already called from `Game.ResetStatics`.
- Budgets: one boss at a time; machine parts are single meshes per articulated part with the six shared machine
  materials; telegraph lanes, beams and discs are pooled with shared materials tinted per renderer; at most 6 burning
  ground discs; one shadowless realtime light per boss (BossLightBudget04E, see Light budget).
- Log lines start with `UBOSS04E` (armed, intro, phases, reinforcements with the ledger counts, victory with every
  cleanup count, endings with the reason, first-win grants).

## For the other streams

- CAMPAIGN (UBoss and Mechanism steps): call `Begin` or `BeginMechanism` with the `Under:` anchor position and yaw
  (the radius is yours: about 20 for the Guardian, 24 for the Matron and the Vaultbreaker, 28 for the Regent), poll
  `Done` and `Failed`, show `Status` or `Objective`, call `Abort()` on cleanup. Claim steps for the boss rewards must
  use exactly the tokens above; the boss victory grants them first, so a Claim only claims.
- BUILDS: the victory calls `Game.Builds.Ledger.Grant(rewardId, token, title)` for each token it does not already
  hold. If the ledger refuses (unavailable or not implemented), the grant is retried every 5 s, at most 12 times, and
  the banner says so; nothing is claimed as earned while refused.
- ECOLOGY: boss and Choir reinforcements spawn through `CreatureDirector.Spawn` **inside the fight's own admission
  lease**. Keep LocalQuiet admission checks in the ambient producers; a check inside `Spawn` itself would refuse them.
- FUSION: the Regent only calls `IsFused` and `RequestSeparate(out reason)`; it never changes vehicles or companions
  itself.
- BONDS: see `TryAssistTarget`. Companion hits are credited through `DamageInfo.FromPlayer` like the player's own.
- AUDIO: cues used: BossTelegraph, BossPhase, BossVulnerable, BossDefeat, FurnaceRoar, BeamCharge, SteamBurst,
  ValveTurn, PlateCrack, GateOpen, LureHum, RepairPulse; loops: Steam, Furnace, BeamLane (all stopped at the end).

## What is honestly unfinished or limited

- Never run in Unity: looks, animation feel, difficulty, pacing and performance are unverified.
- Lock-on, aim assist and damage numbers now reach these bodies (see above); never seen in Unity. Resonance marks and
  bursts still do not apply to them (powers mark characters only), as before.
- Machine animation is procedural transform motion (no skinning); the Matron uses the creature pose solver with flat
  ground under her feet (no per-foot ground sampling on uneven cave floors).
- The seal and the mechanism layouts adapt to the room only through probes (floors, walls, the gate anchor's distance);
  a room much smaller than the layouts assume will be reported (reflectors) or crowded, not reshaped.

## Native checklist (what to look at in Unity)

1. F2 > Underheart bosses > "Valve Guardian here" on the street: the boundary posts appear (cyan), walking in starts
   the intro; check the steam jet lane is readable from player height, the valve wheel visibly spins and glows after it,
   and hits on the wheel feel stronger. Repeat on each difficulty.
2. "Cinder Matron here": she reads as a big beast (not a recolor), the lure tethers and crystals are visible, shooting
   a lure crystal or using its prompt shuts it, the charge lane is visible, a charge across a frosted plate staggers her,
   the nova's safe plates glow during its wind-up, the breath is blocked by a raised ward.
3. "Vaultbreaker here": walk around it; the seams glow brighter as they crack, a break slides the hatch open, the
   overheat ring is clear, repair constructs arrive in phase 2 and the bar says they are mending.
4. "Kiln Regent here": the opening pulse; then fused on the bike (if the FUSION stream allows it there) confirm the
   safe separation message and that companion and bike remain; furnaces ignite in pairs, vents can be shot or used, beam
   lanes leave visible gaps, the cage lifts at one third health.
5. After each victory: rings and lanes vanish, adds puff away, the seal opens, the green exit marker shows, the boss bar
   leaves, loops stop, nothing ambient spawns in the arena for two minutes, and the ledger holds the rewards once
   (beat the boss again with the debug page: no second grant).
6. Die, leave the boundary, hold against the seal, and load a save mid-fight: each ends cleanly with its message and
   leaves nothing behind (Console: the `UBOSS04E ... ended without victory` line and the cleanup counts).
7. Choir: each mechanism ahead of you on open ground; the reflector beam re-draws on every turn and lights the receptor
   with the solution; the valve seizes if attackers stand on it; the escort waits when you walk away and reboots when
   broken.
8. Fly above each boss: the aim lines appear and the boss switches to its aimed move; ground rings do not reach you.
9. Profile a Regent fight in phase 3 (two furnaces burning, beams firing, adds alive) for frame time and draw calls;
   the furnaces now glow without lights: check they still read as burning (grill, vent, fire puffs) in the dark.
10. Lock-on (middle mouse or Z, pad R3) on each boss: the white diamond sits on the boss, moves to the weak
    point when a window opens (crown, regulator, core, heart) and back afterwards without flickering between seams;
    the stick or mouse flick still switches to an add; the camera keeps the boss framed. With a pad, aim down sights near
    a boss: the assist pulls to the same point.
11. Damage numbers on boss hits (yellow on the weak point), on lure and vent hits, and on a companion's or a fused
    form's hits; Settings > damage numbers off hides them.
