# Underheart and Bonds 04E: bonded companions and creature-vehicle fusion

This is the single guide the controls page points to. Part 1 is the companions (BONDS), part 2 is the personal
chassis and fusion (FUSION). Status for both: source implemented, compiled in all twelve configurations, pure rules
covered by offline EditMode tests. **Nothing here has been run in a Unity editor.**

## Summary

| Bond | Role | How you get it | Fused form (on your own bike) | Form ability |
|---|---|---|---|---|
| Quartz Drakeling (`quartz_guide`, default Glint) | Resonant Trail route reveal; Glass Lance that marks for a Resonance Burst | Rescue in `uh05_03_bond` | Ventwing | Vent burst; hold for the glide (Updraft or Vent Brake profile) |
| Ashshell Warden (`ash_warden`, default Hob) | Brace and Sense; Ashshell Brace that absorbs hits from one side | Rescue in Act II, Attuned after the Cinder Matron | Kilnback | Brace ram |
| Stormskimmer (`storm_skimmer`, default Skein) | Relay Hop; Static Snare that staggers (never stun locks bosses) | Rescue in Act III, Attuned at the `uh05_12` care moment | Coilrunner | Arc dash discharge |

- **Controls**: N (pad LB + D-pad down) calls, recalls or sends the companion on foot and fuses or separates in your own
  vehicle (hold); U (pad LB + D-pad up) is the companion's assist on foot and the fused ability in a vehicle.
- **One active companion**: a later rescue never benches a Bonded or fused companion (`BondRules.RescueKeepsActive`);
  the newcomer rests at home until you swap at a care station or on the BONDS page.
- **Only your own chassis fuses**: a borrowed or mission vehicle is refused with a reason and nothing is spent.
- **Separation** places the companion once at a verified spot (`BondSystem.PlaceReturned`); the Kiln Regent can force a
  separation. Companion and fused damage reaches bosses and their lures and vents through
  `UnderheartBosses.TryAssistTarget`, with the damage origin set so attribution counts once.
- **Saves**: `SaveData.Bonds` and the garage and fusion blocks (frozen in `SaveDataTests`); a fused save reloads
  separated with both the bike and the companion intact.

# Part 1. Bonded companions


Status: source implemented and compiled in all twelve configurations, pure logic covered by offline EditMode tests.
Nothing here has been run in a Unity editor or on a GPU yet. Everything about looks, feel, timing and frame rate is
unverified until a native session checks it (see "What a native session must check" at the end).

### What the player gets

1. In "A Creature Is Not a Battery" (`uh05_03_bond`) the quest's Rescue step calls `Game.Bonds.BeginRescue`. On the
   Pressure House service level a pocket quartz drakeling is held by three extraction clamps: bolted plates with a
   two-jawed collar, a glowing red core and a glowing tether to one of its legs. It struggles and flinches.
2. Walk up to a clamp and press E (pad: the use button, Y / Triangle) to release it. The core goes dark, the jaws swing
   open, sparks fly, and the drakeling flares: a pink warning ring on the ground (6.5 m), a rearing roar and a bright
   pulse. While it warns, other clamps cannot be worked, and if you stay inside 5 m the warning does not count down.
   Step back and it settles.
3. With all clamps open it is free but wary. Walk in calmly (running or sprinting at it makes it flare again). A small
   "Walk in calmly" bar fills over its head. Once calm and within 2.8 m, the tuning starts: a ring closes over it and you
   press E (pad: use) as the ring closes, three times. A miss only restarts the beat.
4. Attacking it at any point never hurts it. It panics (a long warning) and the approach resets: calm and tuning go back
   to zero, released clamps stay released, nothing fails permanently.
5. A naming card pauses the game: type a name (keyboard, up to 16 letters, Enter to accept) or keep "Glint". On a pad,
   A keeps the default (pads cannot type). Esc or B also keeps the default.
6. The drakeling joins the roster as Rescued and becomes your active companion on the very body you freed. It follows
   at once. A banner shows the controls; the companion panel above the minimap shows its name, kind, rank, health,
   state and when its assists are ready.
7. At the care station (the treatment bench at Bondkeeper's Refuge, or the care corner at Lantern Refuge, or wherever
   the quest's Care step points) the first care seals the bond: Rescued becomes Bonded and its combat assist unlocks.
8. Later, `uh05_04_descent` raises it to Attuned (first safe fusion); quests, care and crystal tuning raise trust toward
   Harmonized (choose a specialization); the final bond evolution makes it Awakened.

The Ashshell Warden and the Stormskimmer use the same production system: a quest calls `BeginRescue` with
`ash_warden` or `storm_skimmer` (or `Grant` for a direct gift), and they follow, assist, grow and fuse the same way.

### Controls (on foot only; in a vehicle these buttons belong to fusion)

| Action | Keyboard and mouse | Gamepad |
|---|---|---|
| Follow or wait (toggle) | Tap N | Hold LB, tap D-pad down |
| Call it to your side (recall) | Hold N 0.6 s | Hold LB, hold D-pad down |
| Assist (exploration, or combat on a target) | U | Hold LB, press D-pad up |
| Release a clamp, tune during a rescue, use a care station | E | Use button (Y / Triangle) |

`Docs/CONTROLS.md` already lists these rows (the integrator added them with the bindings). The hold uses
`HoldGesture` (0.6 s) and the panel shows a progress bar while you hold.

### The three archetypes

All three are built on the shared creature kit (`CreatureLooks` base look, `CreatureBuilder`, `CreatureRig`,
`CreaturePose`) with their own proportions (`CompanionLooks`) and their own real geometry appended to the single
skinned mesh (`CompanionSculpt`, one draw call, rigid to existing bones, placed by probing the body's own surface).

| | Quartz Drakeling (`quartz_guide`, default Glint) | Ashshell Warden (`ash_warden`, default Hob) | Stormskimmer (`storm_skimmer`, default Skein) |
|---|---|---|---|
| Body | The quartz drake look made juvenile: a fifth of the size (about 0.44 m to the head), bigger head, shorter neck and tail, stubby wings, the quartz regalia and its pearl and cyan surface kept | A heavy, low quadruped from the basilisk plan at 0.9 scale (about 0.53 m tall, 2 m with tail), ash-grey hide, smoky quartz, ember glow | Winged, no forelegs, 0.38 scale (about 2 m wingspan), long streamer tail, steel-blue hide, softly glowing cyan membranes |
| Companion geometry | Quartz lantern cluster at the throat, crystal crown on the skull | Shingled ash-shell plates down the back (rows of curved slabs whose rear edges lift over the next row) and tail, glowing ember seams in the gaps, head shield with a brow crest, shoulder pauldrons, a forward brace ridge on the chest | Glowing veins along the wing arms and ribs across the membranes, conductor nodes at the wrists and between the wing roots, a swept crest of crystal fins, dorsal fins, tail stabilizers |
| Harmonized accents | Cheek fins; Prism: shoulder shards; Lantern: a bigger lantern | Kiln vents glowing inside (Kiln Heart: four instead of two); Bulwark: bigger pauldrons | Antennae; Static Net: coil rings on the tail; Updraft: larger stabilizers |
| Awakened form | Crystal vanes on the wing arms, glowing wrist crystals, a ring of quartz on the tail, a taller crown, more glow | A kiln crown of upright plates around the head, molten seams down the legs, gold seams | A halo coil over the shoulders, arc forks at the wing tips, more glow |
| Moves | Walks, hops short heat gaps and ledges, flutters across drops and after a flying player | Walks only; hops short heat gaps and ledges up to 1.6 m; waits at anything bigger and is recalled instead | Hovers at 1.7 m, kept below any ceiling |
| Exploration assist | Resonant Trail: lights a trail of drifting motes and a light pillar to a hidden cool route nearby, or along the tracked lead, then trots a few steps ahead along it | Brace and Sense: braces the quest mechanism in front of you (gates, valves, hoists, actuators within 12 m), or senses dangerous heat within 16 m and rings up to 12 hot spots on the ground | Relay Hop: flies to the relay or quest point within 22 m and bridges it with an arc (then lights the way there), or rises over you and lights the route ahead from above |
| Combat assist | Glass Lance: a crystal lance (20 damage at Bonded) that leaves a Glassbound mark, so your next gun hit sets off a Resonance Burst | Ashshell Brace: plants between you and the target; hits from a 130 degree arc on that side lose 60% to its shell for 6 s, up to 90 damage, while you stay within 5 m | Static Snare: an arc (9 damage) that staggers the target 1.5 s and so stops its shots; elites 0.7 s; bosses only lose 35% of their poise (never a stun lock) |

Assist numbers grow with rank (x1.25 Attuned, x1.5 Harmonized, x1.8 Awakened) and crystal tuning (+8% per level).
Cooldowns: lance 14 s, snare 15 s, brace 18 s, minus 1 s per rank above Bonded (never below 8 s); exploration 8 s
(skimmer 6 s). The exact formulas are in `BondRules` and every number on the BONDS page comes from them.

### Following

- `BondSystem` records the player's breadcrumb trail (`CompanionTrail`): a point every 0.75 m of real movement, 160
  points. A jump of more than 10 m between samples (teleport, lift, fast travel) starts a new segment.
- The companion walks the trail, so it takes the same doors, stairwells and corridors as the player. It steers
  straight to its spot beside the player only when that spot is within 14 m, on the same floor, clear of walls (a
  sphere cast at body height) and, for walkers, over solid floor halfway (no shortcut over a drop). It stops when close
  with a clear line to the player, faces where the player faces and looks at them.
- Walkers probe the floor from one step above their feet (tables and beams overhead are never taken for the floor).
  Higher ledges the player jumped onto are hops. Trail points in heat (`Game.Underheart.HazardAt`) are skipped: ground
  companions hop a short gap or wait.
- Hovering and fluttering bodies keep below the ceiling (a ray up every 0.2 s).
- Stuck (trying to move but covering less than a quarter of the distance for 1.2 s): first it skips ahead to the
  furthest trail point it can reach in a straight line (at most 8 line tests), then it re-finds the trail, then it asks
  for a recall. Losing the trail, falling down a drop, or ending up 40 m behind (or on another floor) also asks for one.
- Automatic recalls happen only off camera (behind the camera, off screen, beyond 70 m or behind walls) and every 6 s
  at most. The target must be verified ground beside the player: a floor hit within 1.3 m of the player's floor and
  level enough, room for the body (capsule check against world, vehicles and props), a clear line from the player (so
  never through a wall, gate or door), the same vertical layer, no heat, headroom for hovering bodies. Twelve candidate
  spots are tried, behind on its side first. When none qualifies, the panel shows why ("no floor beside you", "walls
  are in the way", "the ground beside you is burning", "it waits for you to land").
- Hold N to recall at any time (3 s cooldown, also on camera, with a crystal flash).
- In a vehicle the following companion rides along: its body is hidden (costing nothing) and it reappears beside you on
  verified ground when you step off. A waiting companion stays where it is.

### Health, downed and recovery

- Health: drakeling 70, warden 140, skimmer 55, +15% per rank above Bonded, +10% per tuning level.
- Companions are never targeted by hostile AI (the brief), so what wears them is: heat they stand in, the brace (35% of
  what the shell takes), and assist exposure (4% for the warden, 8% for the others, when an assist puts it within 4 m of
  the target). Out of combat it mends 2% a second after 6 s without harm.
- At zero it is downed, never lost: it collapses (the death pose held with a breathing glow), retreats out of sight
  after 4 s and rests 75 s, then comes back with half health. Care brings it back at once with full health. Downed is
  saved (it keeps resting after a load). Resting or downed, it cannot assist or fuse, and each refusal says why.
- The boss fights never need the companion; every mandatory puzzle has a tool alternative in the quest data.

### Ranks, trust, care and tuning

Stable rank ids: 0 Rescued, 1 Bonded, 2 Attuned, 3 Harmonized, 4 Awakened (`BondIds`).

| Rank | How | Unlocks |
|---|---|---|
| Rescued | End of the rescue | Follow, wait, recall, exploration assist |
| Bonded | First care (any cooldown), or a quest `RaiseRank`, or `Grant` | Combat assist |
| Attuned | 150 trust, or a quest `RaiseRank` (`uh05_04_descent`) | First safe fusion (FUSION also checks its own rules) |
| Harmonized | 450 trust, or `RaiseRank` | Choose one of two specializations (free to change out of combat), Harmonized accents |
| Awakened | Only an awaken entitlement (`RaiseRank(uid, 4)` or `ApplyEvolution(uid, 2, true, source)` from a reward) | Awakened form, x1.8 assist power |

Trust (0 to 1000): care +25 (once per 4 minutes of play per companion; repeats during the cooldown still heal but
give nothing), campaign main quest +40 and side quest +30 (once per quest per companion, remembered in
`CompanionRecord.Milestones`, so replays never pay twice), contracts +8 (once per 10 minutes), crystal tuning +60.
Legacy quests give no bond trust.

Crystal tuning (three levels; previewed on the BONDS page with what you hold; the materials are spent once and given
back if applying fails; an autosave is requested when play resumes; each level gives +60 trust, +10% health, +8% assist
power):

| Level | Quartz Drakeling | Ashshell Warden | Stormskimmer |
|---|---|---|---|
| 1 | 2 Ember Crystal, 4 Mite Dust (value 180) | 2 Ash Resin, 3 Coolant Salt (204) | 3 Glass Chitin, 2 Coolant Salt (110) |
| 2 | 4 Ember Crystal, 3 Shard Chunk, 1 Regulator Cog (445) | 4 Ash Resin, 1 Basilisk Scale, 1 Regulator Cog (405) | 2 Regulator Cog, 1 Neon Hide, 4 Glass Chitin (312) |
| 3 | 1 Pyre Seed-Core, 6 Ember Crystal, 2 Rift Shard (840) | 1 Pyre Seed-Core, 6 Ash Resin, 2 Wyrm Scale (790) | 1 Volt Core, 3 Regulator Cog, 1 Wyvern Membrane (540) |

Specializations (Harmonized): Prism Lance (+25% lance damage, splinters into one more foe for half) or Lantern Sense
(trail reaches 50% further, recharges 2 s sooner); Bulwark (8 s, 170 degree arc, 70% share, +50% capacity, +2 s
cooldown) or Kiln Heart (while braced, half of any hazard heat on you is shaded); Static Net (the snare jumps to a
second foe for half) or Updraft Scout (reveal 50% further, 2 s sooner).

Evolution: the look follows the rank (Harmonized accents at 3, Awakened form at 4) and keeps any cosmetic stage a reward
granted (`CompanionRecord.Evolution`, never lowered). It changes geometry and glow only, never stats. A change rebuilds
the body on a worker thread and swaps it in place with a crystal burst.

### Care stations and the roster

- Care stations are placed when their places exist: the `care_station` anchor of Bondkeeper's Refuge (VENUES) and
  1.2 m in front of `Under:refuge.caretaker` (UNDERWORLD). A missing anchor is skipped and the STATUS row counts what
  was placed ("care stations placed").
- With one companion the station cares at once. With more, a pausing choice offers care or bringing another
  companion (a station is a safe place to swap). The BONDS page can also switch, send home or summon, out of combat.
- One active companion and one live body at any time. Switching sends the previous one home to rest; its record keeps
  everything.
- Home: bonded companions that are not out with you rest at Bondkeeper's Refuge, one in the habitat enclosure and one on
  the perch (the skimmer prefers the perch). They are shown there, idle and watching you, while you are within about
  45 m of the refuge, and removed when you leave or bring one along (never two bodies of one companion). They are display
  bodies only (no assists, never hittable) and count in the companion body budget.

### BONDS pause page (Order 12) and HUD

- The page lists every bonded companion (name, kind, rank, state, health); the right side shows the selected one:
  rename (keyboard), role, rank and trust with the next threshold and what it unlocks, health and state with the reason,
  both assists with their real numbers and cooldowns, fusion readiness (the `CanFuse` reason) and its form name, look
  stage, specialization buttons, the tuning cost against what you hold with a Tune button, care status, and Make active,
  Send home or Summon. With no companion yet it explains where the first one is and shows the three kinds.
- The HUD panel sits above the minimap (HudOverlays key `bonds_04e`): name, kind and rank, health, state or the reason
  it cannot follow, assist readiness, the hold-N bar, the brace capacity bar, the control hint after bonding, and the
  last one or two command lines. During a rescue it draws the keep-distance line, the calm bar and the tuning ring over
  the creature.
- F2 debug page "Bonds" (Order 56): roster, body, trail, budget, stations, and labelled test buttons.

### Attribution and targeting

- Assist damage: `DamageInfo.Make(amount, chest, direction, Game.Player.gameObject, DamageType.Power)` with
  `Origin = DamageOrigin.Companion`. The shared pipeline credits kills, loot and reputation to the player once.
  Power damage does not charge the player's crystal bar, so assists cannot farm energy.
- Companions only act on command and only against `IsHostileTarget`: a living hostile person, or a creature hostile to
  the player that can be targeted. Calm habitat residents, civilians, staff and escorts never qualify. Aiming at a
  non-hostile person refuses with a sound. With no target in a fight, U asks you to aim or lock instead of revealing.
- The companion body sits on the IgnoreRaycast layer with every collider off, is not a `CharacterBase` and never joins
  `Creature.All`, so lock-on, powers, bullets and hostile AI never find it. Only the trapped rescue body is hittable
  (Characters layer, `CompanionHitReceiver`), and a hit only resets the approach (player hits only; it never dies and
  pays nothing).

### Fusion hand-off (for the FUSION stream)

- `CanFuse(uid, out reason)`: active, Attuned or above, not downed, resting, fused or busy, at least 20% health, and
  either riding along (hidden while you drive) or within 25 m. Reasons are plain sentences.
- `SetFused(uid, true)` ends any assist or brace and hides the body through its own lifecycle (never a global
  cleanup). `SetFused(uid, false)` brings it back beside the chassis on verified ground; it stands there for up to 5 s
  and hops on (rides along) if you drive off, or follows if you step off. If no safe spot exists it rides along and
  appears when you step off.
- `Snapshot(uid)` gives health, the combat cooldown and its charge share and remembers them; `RestoreSnapshot(s)` keeps
  the no-refill rule: health never comes back higher than when it fused, the cooldown never shorter than the time
  naturally left. Health restored at zero downs it (never lost).
- A fused companion is folded back to Following in every save, so no load ever creates a fused companion.

### Save block (`SaveData.Bonds`, append only)

`BondSave`: `Version`, `NextUid`, `ActiveUid`, `Companions`, `HintsShown` (appended).
`CompanionRecord`: `Uid`, `Archetype`, `Name`, `Rank`, `Trust`, `Health01`, `State`, `Evolution`, `Aspects`,
`CareCooldown`, `Source` (foundation), then appended `Tuning`, `Specialization`, `RecoverIn`, `ContractCooldown`,
`CareCount`, `Seed`, `Owner`, `Milestones`. Both are frozen by `Bonds04ETests.BondSaveFieldsAreOnlyEverAppended`.

- Capture writes a deep copy with transient states (fused, assisting, waiting, rescue) folded back to Following.
- Restore normalizes (`BondRoster.Normalize`): null lists and records, duplicate archetypes (the highest rank wins),
  missing or duplicate uids, NaN or out-of-range numbers, specializations below Harmonized, stale active ids. Unknown
  archetypes from a newer build are kept untouched but never become active. Old saves load an empty roster: nothing is
  granted.
- Cooldowns of assists, the trail and the live body are runtime only. The body is summoned on verified ground beside
  the player once the session is playing.

### Budget

`CompanionBudget.BodyCost` (1 per visible companion body, 1 for a rescue body, 1 per resting companion shown at the
refuge; hidden bodies cost nothing) is added to
the ambient body count in `CreatureDirector.CountAmbient` (the one listed hook outside this stream), so companions
count against the creature caps on every preset. Bodies are built on a worker thread (`CompanionBodyFactory`); offline
measurements of the pure build: drakeling about 3 300 vertices on Low, 10 700 otherwise (it stays at the standard level
because the quartz regalia is dense); warden 5 800 (standard) or 9 900 (hero); skimmer 5 100 or 8 500. Build time
offline 30 to 130 ms on the worker thread; the main thread only uploads the mesh. Pose updates thin out with camera
distance (every frame within 30 m, every 2nd to 4th further, every 6th off screen) and the renderer is off past 160 m.

### Interfaces

Frozen (stub signatures kept): `Active`, `Roster`, `ActiveBody`, `Owns`, `Find`, `FindArchetype`, `Grant`, `RaiseRank`,
`SetActive`, `BeginRescue` (RescueHandle), `LastExplorationAssistTime`, `LastExplorationAssistPoint`, `CareAt`, `CanFuse`,
`SetFused`, `Snapshot`, `RestoreSnapshot`, `RankOf` (returns -1 for an unknown uid), events `ExplorationAssist(point)`
and `Bonded(record)`. `ExplorationAssist` is raised by every exploration assist of every archetype; the point is where
the companion did it (for the warden's brace and the skimmer's relay, the mechanism or relay point), so a quest Assist
step can check "near the place" whichever companion is active.

Added members other streams may use: `RescueHandle.Archetype`, `.Progress` (0 to 1), `.Resets`; `RescueActive`;
`SendHome`, `Summon`, `TryTune`, `CanAffordTuning`, `SetSpecialization`, `Rename`, `RequestRename`,
`ApplyEvolution(uid, stage, awaken, source)` (for reward deliveries such as `uh_bond_awaken` and the per-archetype
evolve rewards), `CombatCooldownLeft`, `ExploreCooldownLeft`, `BraceCapacity01`, `HurtActive`, `MaxHealthOf`,
`IsHostileTarget`, `Say`. `PlayerDamageHooks` key `bond_warden_brace` at order 40 (before the handheld ward, after
dodge, guard and parry).

### Files

- `Gameplay/Bonds/`: `BondSystem.cs` (core, roster, body lifecycle, saves), `.Commands.cs`, `.Assist.cs`, `.Care.cs`,
  `.Rescue.cs`, `.Fusion.cs`, `.Home.cs`, `.Hud.cs`, `.Debug.cs`, `BondData.cs` (records, save, snapshot, handle), `BondRules.cs`,
  `BondRoster.cs`, `BondAssistRules.cs`, `RescueRules.cs`, `CompanionTrail.cs`, `CompanionFollowRules.cs`,
  `BondNamingCard.cs`.
- `Creatures/`: `CompanionArchetypes.cs`, `CompanionLooks.cs`, `CompanionSculpt.cs`, `CompanionBodyFactory.cs`,
  `CompanionController.cs`, `CompanionController.Motion.cs`, `CompanionRescueSite.cs`, `CompanionArt.cs`,
  `CompanionTrailFx.cs`, `CompanionBudget.cs`.
- `UI/BondsPage.cs`. Tests: `Tests/EditMode/Bonds04ETests.cs`.
- Hook outside ownership: `Creatures/CreatureDirector.Ecology.cs`, `CountAmbient`, one line adding
  `CompanionBudget.BodyCost`.

### What is honestly not done

- Nothing was run in Unity: the bodies' looks, the companion geometry in motion, the follow feel on real stairs and
  doors, recall placement in real rooms, the HUD layout next to the other panels, timing and audio are unverified.
- Companion audio uses the AUDIO stream's `UhSfx` cues; with the foundation stub those calls are silent.
- `HazardAt`, `TryAnchor`, `TryRoute` and the care station anchors depend on the UNDERWORLD and VENUES streams; with the
  stubs, care stations are not placed (the quest Care step still works at its own station point), heat sense finds
  nothing and routes fall back to the GPS route or a straight floor-sampled line.
- Combat assists target `CharacterBase` foes. A boss built by the BOSSES stream that is not a `CharacterBase` (a
  machine with plain hit volumes) is not a valid assist target; U then refuses (no target) or reveals.
- Companions are not attacked by hostile AI (by design), so health only drops from heat, brace strain and assist
  exposure.
- The rename field on the BONDS page and the naming card need a keyboard; a pad keeps the default name.

### What a native session must check

1. Debug route first (F2 > Bonds): "Test rescue ahead (drakeling)". Look at the clamps (plates, jaws, red cores,
   tethers reaching a leg), the warning ring and roar when a clamp opens, the calm bar, the closing tuning ring, the
   naming card (type a name, Enter; also try a pad: A keeps Glint).
2. Real route: `uh05_03_bond` on the service level (needs the UNDERWORLD and CAMPAIGN merges).
3. Follow: walk through a 04C venue door, up and down the Pressure House stairwell, around corners; tap N (it waits),
   walk 30 m away, tap N (it comes along the trail), hold N (recall with a flash). Leave it stuck behind a closed gate and
   turn away (it should recall only off camera; facing it, the panel says why).
4. Each archetype (F2 grant buttons): silhouettes at 3 m and 15 m in daylight and underground; the warden's plates and
   seams, the skimmer's veins and fins; hover under a low ceiling; the drakeling fluttering after a jump.
5. U with no target (trail motes and pillar, warden heat rings, skimmer relay arc), U on a locked hostile (lance burst
   then a gun hit for a Resonance Burst; brace sparks and the capacity bar while being shot from the front versus from
   behind; snare stagger stopping an NPC's shots), U while aiming at a pedestrian (refusal and sound).
6. Downed: F2 "Hurt 30%" four times: collapse, retreat, recovery timer, care station revive.
7. Save and load with the companion waiting, downed and fused (fused must load as following, downed keeps its recovery timer).
8. Rank-ups and evolution: F2 "Raise rank" to Harmonized and Awakened: the body rebuilds in place with new geometry.
9. Frame time with a companion following through a busy street (Perf flythrough before and after), and the creature
   debug page's body count including the companion.
10. Home: own two companions (F2 grants), walk to Bondkeeper's Refuge: the resting one stands at the habitat or perch
    and watches you; make it active at the care station and its home body is replaced by the live one, never both.

# Part 2. Creature-vehicle fusion and the personal garage


This page explains the FUSION work stream: your own bike as a durable personal chassis, the fused forms (Ventwing,
Kilnback, Coilrunner), how fusing and separating work and stay safe, Jax's bay, the handling courses, and what is and is
not proven yet.

Status, plainly: the code compiles in all twelve configurations and the rules, the state machine, the form geometry,
the bay and the courses are tested offline (no Unity editor, no GPU here). Nothing on this page has been run in Unity.
How the forms look, how fusing feels, the handling, the camera and the frame rate must be checked in a native session
(the checklist is at the end).

### 1. What the player does

1. Your bike is yours. After the prologue (or from the first second of Free Roam) the game keeps a personal chassis
   record: the bronze Vanta Stilettra story bike, with its own number (chassis #1). Phone > Bring my bike always brings
   that chassis, with its paint, upgrades and the damage it had. Riding or stealing another bike never makes it yours.
2. In "Two Hearts, One Chassis" (`uh05_05_chassis`) you ride your bike into Jax Calder's garage and stop in the marked
   bay. With an Attuned companion (the quartz drakeling, Glint by default) the HUD panel says
   "Hold N to fuse with Glint". Hold N (or hold LB and the D-pad down on a pad) for 0.6 s.
3. The form grows out of the bike in under a second: wings unfold from the flanks, the drake head cowl and tail
   stabilizer appear, the companion is inside. The banner says VENTWING and tells you the ability.
4. Ride Jax's handling loop: U (LB + D-pad up) is a vent burst hop on the ground; hold U in the air to glide while the
   vents heat up. Brake hard at the turn. Ride back.
5. Hold N again to separate. Glint steps off beside the bike on verified ground. The coupler cools for 6 s.
6. After the quest you own the fusion license (granted by the reward ledger). Later companions (the Ashshell Warden,
   the Stormskimmer) calibrate their forms once in Jax's bay; after that they fuse anywhere with room.

### 2. Controls (on your bike, in the driver seat)

| Action | Keyboard | Pad |
|---|---|---|
| Fuse, or separate (hold 0.6 s) | Hold N | Hold LB, hold D-pad down |
| What would fusing need right now (tap) | Tap N | LB, tap D-pad down |
| Form ability (vent burst, brace ram, arc dash) | U | LB, press D-pad up |
| Ventwing glide (in the air) | Hold U | Hold LB, hold D-pad up |
| Jax's bay services (parked in the bay, not fused) | U | LB, press D-pad up |
| Close the bay services card | Backspace, U or Escape | B (Circle) |

On foot N and U belong to the companion (BONDS stream). These bindings are the ones the integrator already documented
in `Docs/CONTROLS.md`; no new binding was added. The hold is the command, so a tap never fuses by accident.

### 3. The personal chassis (Game.Garage, `Gameplay/Garage/`)

- One record per owned chassis (`ChassisRecord`): a uid that is never reused, the class (sport, cruiser, skiff),
  paint, health share, engine and armour upgrade levels, the applied look, the unlocked looks, the grip tune, the
  source, and whether it was wrecked. The live bike carries the uid in `Vehicle.ChassisUid`; every other vehicle has 0.
- Exactly one live body per record. Binding a new body clears the uid from any stale body, so two bodies of one
  chassis never exist.
- Where records come from: the story bike in the prologue and in Start After the Prologue (`AdoptStoryBike`), the Free
  Roam start or a loaded save past the prologue (`EnsurePersonal` at session start creates the bronze sport chassis),
  a reward (`GrantChassis(class, source)`, idempotent per class), or a paid registration at Jax's bay
  (`RegisterFromVehicle`, 750, refused for cars, the police bike, wrecks, job vehicles, a bike not stopped in the bay,
  a full garage of 6, or too little money).
- Where bodies come from: the bike parked beside you at session start, the safehouse bike when your chassis has no body
  in the world, and Bring my bike (it moves your free body to a curb spot, or builds a new body from the record).
- Health is sampled from the live body into the record twice a second, so a body cleaned up far away leaves an exact
  record behind. A wrecked chassis is marked; the next delivery comes back patched up to 40% (disclosed in the courier's
  line). Fusing and separating never write chassis health.
- Looks (trims) are cosmetic only: real geometry under the bike's lean pivot, rebuilt on apply (never stacked), no
  stats. Unlocking a look gives it to every owned chassis and every future one.
- The grip tune (`tune_grip`) is the Jax side-quest handling sidegrade: grip x1.08, top speed x0.97, switched in the bay.

### 4. The single state machine (`FusionMachine`, pure, `Vehicles/Fusion/`)

States: Separate, FusionRequested, Validating, TransitionIn, Fused, SeparationRequested, TransitionOut, Recovery. One
machine holds the only state. The legal moves are a fixed table (`FusionRules.IsLegal`); the machine throws on any
other move and the tests walk long random-ish sequences to prove it never makes one.

Fusing, in order:
1. Validate (section 5). A refusal changes nothing.
2. Build the new representation hidden (the form geometry on the bike) and run the physical checks against real
   colliders (section 6). On any failure the partial build is removed and nothing else has changed.
3. Snapshot the companion (`Game.Bonds.Snapshot`), then hide the free body through its own lifecycle
   (`Game.Bonds.SetFused(uid, true)`). If that throws, the form is removed, the body is restored and the snapshot put
   back.
4. TransitionIn (0.85 s): the chassis is held still, the form unfolds. Leaving the bike, a wreck, death or a reset during
   this phase rolls everything back with no cooldown spent.
5. Fused: the handling profile is applied, the Fused event fires, the form ability is live.

Separating: the runtime first finds a verified spot for the companion (section 6). With none, the request is refused
and you stay fused. Otherwise TransitionOut (0.6 s, held still), then `SetFused(uid, false)`, the body is placed on
the verified spot, `RestoreSnapshot` returns exactly what it had, the form is removed, the coupler cools 6 s, the
Separated event fires.

Forced recovery (always ends Separate, deterministic): the chassis wrecked or gone, you died, you got off or were
thrown off, a passenger climbed on, the Veil opened, more than 1.5 s in an Underheart space too tight for vehicles, a
scripted `ForceSeparate(reason)` (the Kiln Regent's resonance disruption), or an internal fault. The companion returns
at the best verified spot near the bike (or near you when the bike is gone), placed once through
`BondSystem.PlaceReturned`; with no verified spot its own recall brings it to you. Cooldown 15 s, unless the fusion never finished unfolding (then 0).

Save and load: no fused frame is ever saved. New game, load and return to title drop the live fusion (always separate);
the garage saves the chassis records, the unlocks, the form meters and the fusion cooldown.

### 5. Validation (FusionRules.Validate, in this order, first failure wins)

garage and bonds loaded; you alive and in control; in a vehicle; in the driver seat; a hover bike (cars are not
supported); not the police bike; not a job vehicle; your personal chassis (else the garage's reason, for example
"This bike isn't yours. Phone > Bring my bike delivers your Vanta Stilettra, or register this one at Jax's bay.");
not wrecked; at least 15% health and not burning; upright; at least two hover rays on the ground; not already fused or
transitioning; coupler cooldown over; not in a story mission, a job that forbids vehicles, or another job (races, taxi
work; campaign quests and contracts are allowed); not inside the Veil; a companion with you; its archetype has a form;
rank Attuned or higher; not downed; `Game.Bonds.CanFuse` agrees; the fusion license, or the first-fusion exception
(the `uh05_05_chassis` quest is running, you are in Jax's bay, the companion is the drakeling); the form calibrated,
or this fusion calibrates it (stopped in Jax's bay); no passenger (refused, never deleted); slow enough (3 m/s; 0.8 m/s
for the first fusion or a calibration); underground only in a vehicle-capable cell (`Game.Underheart.VehicleSpaceAt`);
enough headroom when the Underheart reports it.

### 6. Physical checks (runtime, real colliders)

- Overhead: a ray up from the bike must stay clear for the form's height plus 0.45 m (at least 2.2 m).
- Swept transition box: the box the form occupies while it unfolds, sampled from the same pose function the renderer
  uses (every unfold stage from folded to parked), is checked with an overlap against the world, vehicles, vehicle
  shells and debris (the bike's own colliders excluded; the 0.3 m above the ground ignored, the form has no colliders).
- The returning companion: candidate spots beside, behind, ahead and on a ring; ground under it (slope under 45
  degrees), the same street or underground level, a capsule sized to the archetype clear, no Underheart heat hazard,
  and an open line from the bike.
- The fused bike's physics body is unchanged (no colliders added), so a form can never wedge the bike, push through
  walls or pass mission geometry. Abilities are velocity changes the world still stops.

### 7. No refill, and every multiplier written down

- Form meters (heat, cooldown, charges, recharge, timed effect) change only with time and use, fused or not. Fusing
  and separating never touch them, and they are saved, so a reload is not a refill either.
- The companion comes back with exactly the snapshot taken when it fused (health, assist charge, assist cooldown).
- Player health, armour, the ward, ammo, nitro charges and claim entitlements are never touched by fusion.
- Explicit damage multipliers (`FusionRules`):

| Form | Incoming chassis damage | While bracing | Rider damage | Rider, braced, hit from the front |
|---|---|---|---|---|
| Ventwing | x1.0 | n/a | x1.0 | n/a |
| Kilnback | x0.7 | x0.4 | x0.85 | x0.6 |
| Coilrunner | x1.1 | n/a | x1.0 | n/a |

The chassis multiplier is applied in `Vehicle.ApplyDamage` (one listed hook line), the rider multiplier through
`PlayerDamageHooks` (key `fusion_rider_04e`, order 30). No form ever makes the rider invulnerable.

### 8. The forms (real geometry under `Parts.LeanRoot`)

All parts are procedural meshes (`FusionFormGeometry`, pure blueprints; `FusionFormView` builds renderers, no
colliders), parented under the bike's lean pivot, so they bank and bob with the bike and leave the rider's Seat,
HandleL, HandleR, FootL and FootR anchors untouched. Tests prove no vertex of any form, in any pose, enters the rider's
torso, arm or leg space, and nothing comes within 0.15 m of the road.

- Ventwing (drakeling): crystal ribs on the fairing ahead of the knees; a drake head cowl over the nose (skull, snout,
  jaw, brow ridges, glowing eye slits, swept crystal horns ending ahead of the handlebars, a crest) with a pulsing crystal
  emitter at the snout tip; two three-panel articulated wings hinged behind the rider's legs, folded flat against the
  flanks at rest, spreading with speed (about 3 m span) and lifting while gliding; a tail stabilizer (a vertebral boom on
  a strut, crystal spikes, a steering fin, two pitching vanes). Glide trails leave the wing tips while gliding.
- Kilnback (Ashshell Warden): layered mineral shell plates (two caps over the fairing, a stepped hump behind the rider,
  lifted over the courier top box on the skiff) with glowing ember seams; low side skirts that keep the thruster pods
  clear; a forward brace: a blunt prow with an ember chevron on pistons, held by two brace arms. Bracing pushes the prow
  forward and the seams flare.
- Coilrunner (Stormskimmer): a row of dorsal crystal spines on the tail that stand up when the dash is ready; two swept,
  drooping side fins that follow the steering; a coil rail with ring coils on the tail; low conduits with glowing nodes
  along both flanks to a ring around the nose; two forward antennae and an emitter orb. The discharge arc runs along that
  path and out to each struck target.

Each form is recognizable by outline alone (wings and horns; plated hump and prow; spines, fins and antennae), and a
test checks the structures and the widths differ. Low variants (Low graphics preset) drop facets and repeated details
(fewer ribs, plates, spines and coils, no finger struts) but keep every outline: a test requires under 75% of the
vertices and at least 90% of each extent. Budgets: Ventwing 3348 vertices (Low 1628), Kilnback 2192 (1264), Coilrunner
2948 (1700). Meshes are built once per form, class and quality and shared; materials are one cached palette per form.
The form hum loop and all effects are children of the form, so they stop when it ends.

### 9. Handling and abilities (`FusionRules.Handling`, `FusionRules.Ability`, applied in `Vehicle.BikeStep`)

| Form | Drive | Top speed | Grip | Brake | Steer | Other |
|---|---|---|---|---|---|---|
| Ventwing Updraft | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 | glide gravity 0.32, sink at most 3.2 m/s, air yaw x1.6 |
| Ventwing Vent Brake | 1.0 | 1.0 | 1.0 | 1.25 | 1.0 | glide gravity 0.45, sink 4.5 m/s, air brake 6 m/s2 |
| Kilnback | 0.85 | 0.88 | 1.08 | 1.05 | 0.85 | armoured (section 7) |
| Coilrunner | 1.0 | 1.0 | 1.05 | 1.35 | 0.92 | strong brakes, calmer steering |

No form raises top speed (tested). The Ventwing profile comes from the campaign choice `fusion_profile` (updraft or
ventbrake) until you pick one in Jax's bay; it is saved in `GarageSave.Profile`.

| Ability | Cost and limit | Effect |
|---|---|---|
| Vent burst (Ventwing, U on the ground) | heat +0.34, cooldown 2.2 s, needs 4 m headroom | +5.2 m/s up and +3 m/s forward (never past top speed); scorches hostiles behind for 18 fire damage |
| Glide (Ventwing, hold U in the air) | heat +0.16 per second; at full heat the vents overheat and nothing fires until they cool to 35% | carries part of gravity, caps the sink; hot vents (over 75%) cut drive by 15% |
| Brace ram (Kilnback, U) | heat +0.3, cooldown 6 s, lasts 1.2 s | +3.5 m/s forward (never past top speed); harder shell; each hostile in the box ahead is struck once: 55 damage, shoved, creatures staggered 0.7 s (bosses 0.25 s) |
| Arc dash (Coilrunner, U) | 2 charges, one back every 5 s, 0.8 s between uses | +6.5 m/s forward, never past 1.12 x top speed; 0.25 s later a discharge strikes up to 6 hostiles in line of sight within 7 m: 32 damage, stagger 0.5 s (bosses 0.2 s) |

Hits only ever reach hostile NPCs and targetable creatures (never civilians, police that are not after you, companions
or you), with `Source` = the player and `Origin` = `FusedVehicle`: one attribution, one payout.

### 10. The compatibility matrix (honest)

| | Sport (Vanta Stilettra) | Cruiser (Anvil Duskhauler) | Skiff (Kestrel Skiff) |
|---|---|---|---|
| Ventwing (quartz drakeling) | built, offline-tested, primary target | built, offline-tested | built, offline-tested |
| Kilnback (Ashshell Warden) | built, offline-tested | built, offline-tested | built, offline-tested |
| Coilrunner (Stormskimmer) | built, offline-tested | built, offline-tested | built, offline-tested |

"Built, offline-tested" means: one parametric builder per form fitted to each bike's real proportions (checked against
the bike builder's seat point and dimensions), geometry tested for envelope, rider clearance, road clearance and the Low
silhouette, handling and abilities tested as maths. None of the nine has been seen or ridden in Unity yet. Ventwing on
the sport bike is the combination the first route uses and the one to check first.
Not supported, with a reason on screen: every car class ("Cars are not supported: fusion couples with a hover bike
chassis"), the police bike ("never a personal chassis"), and any other bike model. Other named characters' chassis are
not implemented (the data model has an owner-free record per chassis, but no ally pairing exists).

### 11. Jax's bay (`Game.Fusion.TryBay`)

- If the garage venue's layout has the `bay` anchor (the VENUES stream appends it), the bay is that anchor, radius 2.8 m.
- Otherwise the bay is computed from the garage venue's own frame: local (0, 0.012, 6), the middle of the marked
  vehicle bay (stripes at x -2.8 and 2.8, z 0.25 to 13.75), clear of the repair point at z 10.5. In this sandbox the
  anchor is missing, so this computed bay is what runs (the STATUS page says which). A test checks it lies inside the
  layout's `vehicle_bay` keepout and inside the garage building on the default city.
- If the walk-in garage did not load at all, the bay falls back to the lot 5 m outside the Jax Calder Customs door
  (radius 4 m), so the first-fusion quest is never blocked.
- "In the bay" means within the radius (flat) and within 2.5 m height; "parked" adds a speed under 1 m/s.

Bay services (U while parked in the bay, not fused): switch the Ventwing handling profile (free, reversible); cycle the
look of your chassis (if any are unlocked); switch the grip tune (if unlocked); choose which owned chassis is your
personal one (Bring my bike brings it next); register the bike you are on (if it is not yours).

### 12. The three courses (`Game.Fusion.TryCourse`)

| Id | Where | Ability uses | Notes |
|---|---|---|---|
| `jax_handling` | out from the garage door about 200 m by road and back | 1 | ability checkpoint 1, hard brake at the turn |
| `jax_profile_two` | to the door's right about 230 m by road and back | 2 | for the second-profile side quest |
| `canal_crossing` | Underheart anchors canals.portal_bottom, canals.transit, canals.crossing, canals.exit_gardens | 1 | resolves once the Underheart is built (not in this sandbox) |

Road courses follow the real road graph (`CityLayout.FindRoute`), sampled every 45 m (at most 9 checkpoints, radius
7 m), end back at the garage door, and mark `BrakeAt` (the farthest point) and `AbilityAt`. The quest Course step counts
`AbilityUses`, which resets at every fusion. A test resolves both Jax loops on the default city.

### 13. Interfaces for the other streams

- `Game.Garage`: `Personal`, `Owned`, `Find(uid)`, `IsPersonal(v)`, `RecordOf(v)`, `WhyNotPersonal(v)` (for the Park
  step's refusal), `BodyOf(uid)`, `PersonalBody`, `EnsurePersonal()`, `AdoptStoryBike(v)`, `RegisterFromVehicle(v, out
  reason)`, `GrantChassis(class, source)`, `SetPersonal(uid)`, `FusionUnlocked`, `UnlockFusion(source)` (also calibrates
  the Ventwing), `FormUnlocked(form)`, `UnlockForm(form, source)`, `UnlockTrim(id, source)`, `ApplyTrim(id)`,
  `SetTune(id)`, `Profile`, `ActiveProfile`, `SetProfile(id)`, `SpawnBody(record, ground, yaw, trafficParked)`.
- Reward ids for BUILDS: looks `trim_quartz_crown`, `trim_ember_fins`, `trim_frost_plates`, `trim_storm_rails`
  (`ChassisTrims.All`); the handling sidegrade is `UnlockTrim("tune_grip")` (the ids `uh_handling_sidegrade` and
  `handling_sidegrade` are accepted as the same tune); the second vehicle class is `GrantChassis("cruiser", ...)`;
  the license is `UnlockFusion(...)`.
- `Game.Fusion`: `State`, `IsFused`, `ActiveForm`, `AbilityUses`, `CanFuse(out reason)`, `RequestFuse(out reason)`,
  `RequestSeparate(out reason)`, `ForceSeparate(reason)` (BOSSES: the Kiln Regent's opening), `TryCourse(id, out
  course)`, `TryBay(out center, out radius)`, `InBay(v)`, `ParkedInBay(v)`, `BaySource`, `LastReason`, `CooldownLeft`,
  `Chassis`, `Gliding`, `MetersOf(form)`, events `Fused(form)`, `Separated`, `AbilityUsed(form)` (the burst, ram or
  dash; the glide is not counted).
- From BONDS this stream needs: `Active`, `RankOf`, `CanFuse(uid, out reason)`, `SetFused(uid, fused)` (hide or restore
  the body through its own lifecycle; while fused the companion deals no damage), `Snapshot(uid)`,
  `RestoreSnapshot(s)`, `ActiveBody` (the fusion system hands the verified spot to `PlaceReturned` just before
  `SetFused(uid, false)`, and BONDS teleports the body there once), and `CompanionRecord.State == Downed`.
  In this sandbox Bonds is the foundation stub, so fusion correctly refuses with "Bonds are not implemented in this
  build" until the merge.
- From CAMPAIGN: the first-fusion exception keys on `Game.Quests.ActiveId == "uh05_05_chassis"`; the Fuse, Course and
  Park steps read the members above; the profile choice is the `fusion_profile` choice key.

### 14. Save data (append-only)

`SaveData.Garage` (`GarageSave`): Version, NextUid, PersonalUid, Chassis, FusionUnlocked, Forms, Profile (foundation),
plus UnlockedTrims, Meters (`FormMeterSave`: Form, Heat, Cooldown, Charges, Recharge, Overheated) and FusionCooldown.
`ChassisRecord` appended Wrecked, ArmorUpgrade and Tune. Field freeze tests cover GarageSave, ChassisRecord and
FormMeterSave. Loading normalizes the block: a missing block is empty (nothing granted), broken lists are rebuilt,
duplicate or invalid uids fixed, health and paint clamped (a broken health value becomes full, a wreck stays a wreck),
unknown forms and profiles dropped, locked looks and tunes cleared, meters clamped to their limits, the personal uid
pointed at an owned chassis, the uid counter moved past every uid in use.

### 15. HUD, STATUS and debug

- A panel at the lower right (left of the speedometer) while you ride your own bike with a companion, while fused, or in
  Jax's bay: the chassis number and companion, "Hold N to fuse with Glint" when everything passes (or the one reason it
  does not), the transition bar, the form's heat bar or charge pips and cooldown, the ability hint, a gold hold-progress
  bar along its bottom, and short result toasts above it. It stays hidden on bikes that are not yours.
- STATUS rows: "garage" (personal chassis, stored or in the world, forms calibrated, owned count) and "fusion" (state,
  what is missing, where the bay came from).
- F2 > Fusion (order 64): live state, the validation result, the bay source and distance, every form's meters, and
  debug buttons (grant the license, calibrate every form, spawn your chassis here, force a safe separation, make the
  companion Attuned through Bonds, resolve the courses).

### 16. Files

Added: `Gameplay/Garage/GarageRules.cs`, `Gameplay/Garage/PersonalGarage.Body.cs`, `Vehicles/Fusion/FusionRules.cs`,
`FusionMachine.cs`, `FusionCourses.cs`, `FusionFormGeometry.cs` (+ `.Ventwing.cs`, `.Kilnback.cs`, `.Coilrunner.cs`,
`.Trims.cs`), `FusionArt.cs`, `FusionFormView.cs`, `ChassisTrimArt.cs`, `FusionSystem.Requests.cs`,
`FusionSystem.Abilities.cs`, `FusionSystem.Bay.cs`, `FusionSystem.Ui.cs`, `Tests/EditMode/Fusion04ETests.cs`, this page.
Replaced bodies (signatures kept): `Gameplay/Garage/PersonalGarage.cs`, `Vehicles/Fusion/FusionSystem.cs`.
Changed: `Vehicles/Vehicle.Bike.cs` (handling hooks), `Vehicles/TrafficManager.Bikes.cs` (personal chassis delivery),
`UI/Phone.cs` (one comment line).
Hooks outside ownership: `Vehicles/Vehicle.cs` (one line in `ApplyDamage`: the fused damage multiplier) and
`Gameplay/StoryMissions.cs` (one line each in `SkipPrologue` and the prologue mission to adopt the story bike).

### 17. Offline proof (what was actually run, without Unity)

- Twelve compile configurations: `/home/claude/tools/check_urp.sh /home/claude/agents/04e/fusion
  /home/claude/agents/04e/out_fusion/urp` gave 0 errors and 63 warnings, the same 63 as the foundation (all CS0618 in
  `QA/LivingCity04CQa.cs` and `QA/LivingTerritories04BQa.cs`).
- EditMode tests: `cd /home/claude/agents/04e/fusion && /home/claude/tools/check_tests.sh /home/claude/agents/04e/fusion
  /home/claude/agents/04e/out_fusion/tests` gave 787 passed, 25 failed, 47 skipped. The foundation was 748 passed, 25
  failed, 47 skipped; the 25 failures are the identical set (native `Resources.Load` calls offline), and the 39 new
  passes are `Fusion04ETests`.
- `Fusion04ETests` covers: every validation refusal and its message; the first-fusion exception (quest, bay, drakeling,
  parked); calibration only in the bay; separation rules; the transition table; fuse and separate restoring the same
  companion exactly; a failed build, a thrown build and a failed companion hide all rolling back at no cost; aborts and
  recoveries (wreck, death, dismount, loss) always ending separate; refused separations staying fused; every state the
  machine enters being legal; toggling never refilling heat, cooldowns, charges or the companion; meters improving only
  with time; ability impulses never passing their cap; handling never raising top speed; the explicit damage table;
  the 3 x 3 compatibility matrix; garage normalization of empty, old and broken blocks (meters included); uid reuse;
  registration refusals; delivery health; trims never stacking or touching handling; every form on every class at both
  qualities fitting its envelopes and never entering the rider's space; Low keeping each outline; the three structures
  being distinct; the frame matching the real bike models; trims clear of the rider; the computed bay inside the
  garage's marked bay and building on the default city; both Jax road loops resolving on the default city; course
  sampling; and the field freezes for GarageSave, ChassisRecord and FormMeterSave.
- Unity meta files: `python3 /home/claude/cms/tools/make_metas.py /home/claude/agents/04e/fusion` (32-character GUIDs).
- Every changed file scanned: no U+2010 to U+2015 dash characters, no non-ASCII characters.

### 18. What a native Unity session must check

1. Free Roam: the bike beside the Veyra Lofts door is bronze; get on, Phone > Bring my bike says "chassis #1". Steal a
   traffic bike, ride it away, call Bring my bike: the bronze bike comes, not the stolen one.
2. F2 > Fusion > "Debug: Attuned companion" (needs the merged Bonds), "Debug: grant fusion license", sit on your bike in
   open street: the panel shows "Hold N to fuse with Glint". Hold N, then hold LB + D-pad down on a pad.
3. Watch the Ventwing appear: wings folded flat along the flanks, unfolding to the parked spread; head cowl over the nose
   (not over the handlebars or the rider's hands); tail boom and fin behind; nothing through the rider's legs, arms or
   head, also while leaning hard; the emitter pulse; the hum.
4. Ride: wings spread with speed; vent burst hop on flat ground; hold U in the air: wings lift, glide trails, slower
   sink, heat bar rising, overheat stops the glide; landing and braking with Updraft and with Vent Brake (bay card).
5. Separate in the open and next to a wall: Glint appears on the open side, never inside a wall or under the road.
   Try it boxed in by cars: it should refuse with the reason and stay fused.
6. Crash hard while fused, get blown up, fall off at speed, die: every time the form vanishes and Glint reappears near you.
7. Jax's garage: drive into the bay, stop; the panel shows JAX'S BAY; U opens the bay card (mouse, keyboard and pad
   focus, B closes). Fusing there in the quest without the license. Check the computed bay sits in the marked stripes.
8. Kilnback and Coilrunner on all three bikes (F2 calibrate every form), High and Low graphics: silhouettes, the hump on
   the skiff's top box, the prow on the brace, spines flaring, fins steering, the discharge arc to enemies.
9. Save while fused, load: you are separate, the bike and companion are there, the cooldown and meters are as saved.
10. Performance: fuse and separate twenty times; watch for growing object counts (meshes are shared and cached; form
    objects are destroyed on separation).

### 19. Honestly unfinished

- Nothing here has been run in Unity: looks, feel, handling numbers and camera framing are unverified.
- The companion's placement after separation depends on BONDS keeping its body where fusion put it.
- The quest steps (Fuse, Course, Park) and the course markers are CAMPAIGN's; this stream only supplies the data.
- The bay anchor from VENUES is not in this sandbox; the computed bay is used and labelled.
- Combat fusion while moving is allowed only up to 3 m/s by design; faster fusing is future work once collision and
  cancellation have been tested natively.
- No ally or other character chassis, no car or skiff (water) chassis: labelled unsupported.
