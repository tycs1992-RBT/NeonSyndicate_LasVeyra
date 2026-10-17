# Underheart and Bonds 04E: bonded companions (BONDS stream)

Status: source implemented and compiled in all twelve configurations, pure logic covered by offline EditMode tests.
Nothing here has been run in a Unity editor or on a GPU yet. Everything about looks, feel, timing and frame rate is
unverified until a native session checks it (see "What a native session must check" at the end).

## What the player gets

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

## Controls (on foot only; in a vehicle these buttons belong to fusion)

| Action | Keyboard and mouse | Gamepad |
|---|---|---|
| Follow or wait (toggle) | Tap N | Hold LB, tap D-pad down |
| Call it to your side (recall) | Hold N 0.6 s | Hold LB, hold D-pad down |
| Assist (exploration, or combat on a target) | U | Hold LB, press D-pad up |
| Release a clamp, tune during a rescue, use a care station | E | Use button (Y / Triangle) |

`Docs/CONTROLS.md` already lists these rows (the integrator added them with the bindings). The hold uses
`HoldGesture` (0.6 s) and the panel shows a progress bar while you hold.

## The three archetypes

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

## Following

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

## Health, downed and recovery

- Health: drakeling 70, warden 140, skimmer 55, +15% per rank above Bonded, +10% per tuning level.
- Companions are never targeted by hostile AI (the brief), so what wears them is: heat they stand in, the brace (35% of
  what the shell takes), and assist exposure (4% for the warden, 8% for the others, when an assist puts it within 4 m of
  the target). Out of combat it mends 2% a second after 6 s without harm.
- At zero it is downed, never lost: it collapses (the death pose held with a breathing glow), retreats out of sight
  after 4 s and rests 75 s, then comes back with half health. Care brings it back at once with full health. Downed is
  saved (it keeps resting after a load). Resting or downed, it cannot assist or fuse, and each refusal says why.
- The boss fights never need the companion; every mandatory puzzle has a tool alternative in the quest data.

## Ranks, trust, care and tuning

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

## Care stations and the roster

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

## BONDS pause page (Order 12) and HUD

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

## Attribution and targeting

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

## Fusion hand-off (for the FUSION stream)

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

## Save block (`SaveData.Bonds`, append only)

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

## Budget

`CompanionBudget.BodyCost` (1 per visible companion body, 1 for a rescue body, 1 per resting companion shown at the
refuge; hidden bodies cost nothing) is added to
the ambient body count in `CreatureDirector.CountAmbient` (the one listed hook outside this stream), so companions
count against the creature caps on every preset. Bodies are built on a worker thread (`CompanionBodyFactory`); offline
measurements of the pure build: drakeling about 3 300 vertices on Low, 10 700 otherwise (it stays at the standard level
because the quartz regalia is dense); warden 5 800 (standard) or 9 900 (hero); skimmer 5 100 or 8 500. Build time
offline 30 to 130 ms on the worker thread; the main thread only uploads the mesh. Pose updates thin out with camera
distance (every frame within 30 m, every 2nd to 4th further, every 6th off screen) and the renderer is off past 160 m.

## Interfaces

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

## Files

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

## What is honestly not done

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

## What a native session must check

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
