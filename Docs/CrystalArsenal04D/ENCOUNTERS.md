# 04D Finite encounters: the Resonance Breach

Status: source-validated, native Unity testing pending. Nothing in this page has been run inside Unity. Compile checks
(twelve configurations) and the offline EditMode tests passed; how it looks, feels and performs is NOT RUN.

## What the player gets

The Resonance Breach is a short, finite skirmish that the player starts on purpose from the crystal case board near
Veyra Lofts (the arsenal pass draws the board and calls this system). It opens on open ground 160 to 320 m from the
Lofts arrival, outside the 140 m starter safety area. A GPS waypoint and a map blip point the way.

Four authored opponents fight there in total. Three wait at the site; one advertised reinforcement arrives later:

| Role | Species (existing) | What it does | How to answer it |
|---|---|---|---|
| Crystal Warden | Shard Golem | Advances behind a frontal guard (Balanced and Veteran), commits to one ringed ground slam, then stands still with its chest core marked WEAK POINT for 1.25 to 2 s | Dodge the ring, then hit the core; or flank, or use explosions |
| Refractor | Mirror Wraith (its glass guard removed) | Holds a firing lane 16 to 30 m away, glows and draws an aim line while charging a shard; the aim stops following you at 75 percent of the charge, then it fires along that fixed line; moves to a new lane when you pressure it | Sidestep after the line locks, break line of sight, push it |
| Skirmisher | Hollow (its blink turned off) | Runs to a flank point on legal open ground beside you, pulses visibly for 0.55 to 0.9 s, then closes in to strike; gives up a route that turns out blocked | Watch your sides; punish the pause |
| Resonator (the reinforcement) | Hollow, glowing | Arrives on a filling ground ring after a warning banner; keeps behind its ally and holds one visible beam link that makes that ally take about half damage | Break the line between them, hit it hard to interrupt, or kill it (that ends the link for good) |

Story keeps at most three alive at once, Balanced and Veteran four. The reinforcement comes after the first opponent
falls or 25 s after the player arrives, and only when the alive cap has room. There is never a second batch.

A victory needs every one of the four authored opponents to die (health reaching zero). The first victory grants the
Gyre Crescent through the arsenal (token `contract:resonance_breach`; a full backpack makes it a pending claim there).
Replays are allowed and give no new reward.

After the victory the area stays quiet: 90 m around the breach, for 120 s of active play (paused time does not count),
and then until the player has gone 120 m away. Inside that area no new ambient creature group, gang crew, champion,
world event or loot-world crew appears. Nothing already there is removed, a wanted level is never cleared and nobody
becomes invulnerable. Every boss victory gives its arena the same quiet aftermath.

## When it refuses to start (one line each)

During a mission, while wanted, during a boss fight, during a fort raid, a relay capture or territory activity, a depot
operation, with a world event within 220 m, inside the Veil, while a QA capture runs, while the player is down, while a
breach is open or withdrawing, during the 30 s retry wait, or when no site passes the ground checks.

## How it ends without a reward

Leaving 140 m from the breach after arriving, being downed or arrested, crossing the Veil, a mission or a heavier combat
owner starting (boss fight, raid, capture, territory activity, depot), an opponent vanishing without dying (for example
removed by the debug menu), a body that cannot be created, not arriving within 8 minutes of active play, a new game,
load or title, or the system shutting down. The opponents then stop every attack, walk away and are removed as soon as
the camera cannot see them (20 s at most). Removals never count as kills. Reservations and the area lease are
released, and the contract can be tried again after 30 s. An open breach is never saved: after a load it is simply gone.

## How the opponents think (bounded, readable)

The encounter overlay (`AI/EncounterRole04D.cs`) makes each opponent's decisions while its body, movement, melee
swings, abilities, damage and death stay the species' own. Attack tokens still come from `CombatDirector`.

- Sight: range from the species' sense and the player's stealth (crouching, standing still, fog, night), a 240 degree
  view until engaged, and a clear line of sight. Hearing a shot or a blast only moves the last known position.
- Reaction delay before committing: 0.9 s Story, 0.6 s Balanced, 0.45 s Veteran.
- Without sight for 8 s it stops pursuing and searches the last known position (3 s there, 12 s at most), then walks
  back to its post. Hard leash: never more than 120 m from the breach, and 25 s of pursuit without a reachable
  sighting sends it home. A returning opponent re-engages only when both it and the player are within 100 m.
- A player hovering or on a roof (more than 2.6 m above its feet) is out of reach: melee bodies hold a ring below
  instead of walking up invisible stairs; the refractor and the warden throw visible, world-colliding shards.
- Shared alerts give nearby guards the last known position, never live aim.

## Monster-combat noise

Gunfire, blasts and fire noise with no victim do not call the police when they belong to this contract's fight: the
breach is live, the noise is inside its 140 m area, and the shot landed within 12 m of an owned opponent, passed within
3 m of one, or the player hurt one in the last 2 s. Crimes with a victim (a civilian, a car, the police) are untouched.

## Files

| File | Purpose |
|---|---|
| `Gameplay/CrystalArsenal/CrystalEncounters.cs` | Game.Encounters: the board interface, state, admission, frame loop, saves |
| `Gameplay/CrystalArsenal/CrystalEncounters.Run.cs` | One open breach: spawning, budget, reinforcement, victory, abort, withdrawal |
| `Gameplay/CrystalArsenal/CrystalEncounters.Site.cs` | The site survey on the real ground and colliders |
| `Gameplay/CrystalArsenal/CrystalEncounters.Noise.cs` | The scoped monster-combat noise query |
| `Gameplay/CrystalArsenal/CrystalEncounters.Ui.cs` | The HUD line and the F2 "Encounters" page |
| `Gameplay/CrystalArsenal/EncounterRules04D.cs` | Pure rules: states, budget, victory, abort, admission, intents, noise, site candidates |
| `Gameplay/CrystalArsenal/LocalQuiet.cs` | Pure owner-keyed quiet leases and the LocalQuiet static |
| `AI/EncounterRole04D.cs`, `AI/EncounterRole04D.Patterns.cs` | The opponent overlay and the four role patterns |
| `AI/EncounterShard04D.cs` | The pooled, owned, cancellable refracted shard |
| `AI/NpcController.Encounter04D.cs` | The seam that lets an overlay make a creature's think decision |
| `Tests/EditMode/CrystalEncounters04DTests.cs` | 44 engine-free tests |

## Automatic producers audited

Hooked (one line each, `LocalQuiet.Blocks` before an automatic aggressive spawn): the creature ambient ecology (spawn
point choice and the re-check before a planned group steps in), gang crews (`PedManager`), champion promotion
(`LootArmory.Drops`), random world event placement (`WorldEvents`, forced debug events excepted) and loot-world chest
and bounty crews (`LootWorld.Guards`).

Left alone on purpose: civilians, traffic and station patrols (not aggressive), police dispatch (a legitimate wanted
level must keep working), territory habitats and the starter habitat (calm until provoked), and everything the player
starts explicitly (missions, quests, fort raids, relay captures and territory activities, depot operations, Veil vault
packs, boss arenas). Boss arenas already keep wildlife out until their boss falls; afterwards the boss quiet applies.

## How Tyler tests it in Unity (NOT RUN)

1. Start Free Story. Press F2 and open the "Encounters" tab (pad: open the debug menu and move with the D-pad).
2. Check the "Site" line: it should name a spot about 160 to 320 m from Veyra Lofts (on the default city it should
   be the parking lots about 220 m west of the Lofts). "Teleport to the breach" puts you 30 m from it.
3. Press "Start (normal rules)" (or use the crystal case board once the arsenal pass is merged). Follow the GPS.
4. Watch for: the warden's ground ring and the gold WEAK POINT marker after its slam, the refractor's aim line turning
   orange and fixed just before it fires, the skirmisher's pulse before it lunges, the reinforcement banner and ring,
   and the teal link beam (break it by putting a wall between them).
5. Win, then stay nearby for two minutes: no new hostile group should appear inside 90 m. The HUD line counts down the
   quiet; pausing must stop the countdown.
6. Failure checks: start again and walk 140 m away (they withdraw and vanish off camera, no reward, retry in 30 s);
   fly or climb onto a roof mid-fight (melee opponents wait below, shards still fly); fire at them with no wanted
   level (no police); hit a civilian (police as usual).
7. "Force victory", "Force abort", "Release quiet zones" and "Quiet zone here (test)" exist for quick checks. The page
   lists every owned opponent (intent, what it is doing, health, distance, pursuit seconds, link) and every quiet lease.

Every state change, spawn, death, removal, reservation and lease writes one `ENC04D` line to the Unity console.

## Known limits

- Never run in Unity: navigation around real props, attack timing, the readability of the telegraphs, the frame cost
  of the spawns (humanoid bodies build on the main thread, one per frame) and the site survey (about 70 ms once per
  city offline) all need a native look.
- The warden's frontal guard reuses the creature kit's glass guard, so its deflect message says "Glass guard".
- Opponents move without a navigation mesh (the creature movement's feelers); a skirmisher route is judged by straight
  lines and abandoned when it stops making progress.
- The quiet aftermath is not saved: a load or a new game lifts it (as the brief allows when not designed for saves).
