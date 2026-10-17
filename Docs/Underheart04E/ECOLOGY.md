# Underheart ecology (04E): roles, residents, finite encounters

This page covers the ECOLOGY work of the 04E sprint: the six underground enemy roles, the three harmless or territorial
residents, the finite skirmish runtime that quest Skirmish steps use, how it shares space with every other spawner, and
the underground-safe spawn placement. Everything here was compiled in all twelve configurations and the pure rules are
covered by `Ecology04ETests`. **None of it has been run inside Unity yet.** The last section lists exactly what to look
at in the first native session.

## 1. The six roles

Each role has its own body (a new `CreatureLooks` entry, not a recolour), its own attack set with readable wind-ups and
recovery, its own breakable parts, its own voice mix and loot that uses only the four 04E materials. On top of the
creature brain a role driver adds one signature move with a tell, an active window and a punish window, a retreat rule
(never the leash heal), and the counters below. Numbers live in `Creatures/Underheart/UnderheartRoles04E.cs`.

| Species | Role | Body | Signature (tell / punish) | Counters | Retreat |
|---|---|---|---|---|---|
| Ember Scavenger | Pack harasser | Low, lean spined runner with a split tail and ember spines | Hit and run: crouch and chitter, spines flare, a low dart (0.7 s / 0.9 s) | Face the pack (they hesitate while you look at them), break a dart in its wind-up, keep a wall behind you, frost slows | Runs off after every hit to circle again; once below 30% it breaks and runs for 4 s, then returns |
| Ash Burrower | Armoured digger | Long, very bulky, flat plated body with a beaked head and short tail | Burrow ambush: plate rattle and an ash plume, a dust trail toward you, then an ash ring fills under you (1.1 s / 1.6 s rearing open) | Step out of the ring, punish the open belly after it surfaces, flank or break the frontal plates, lightning | Below 25% it dives once and surfaces about 10 m away (no damage, no heal) |
| Glasswing Ambusher | Ceiling ambusher | Glass winged flyer with clear quartz membranes, no forelegs | Ceiling drop: hangs on the ceiling (lit by the Third Eye), glass dust, white ring, screech, drop (0.9 s / 0.8 s stunned) | Look up, shoot it down before it drops (it lands stunned), leave the ring and punish the landing | Hurt, it climbs back to the ceiling slowly (a hit during the climb drops it); with cracked wings it can only run |
| Furnace Caster | Ranged caster | Tall kiln backed quadruped with a glowing crucible and two chimneys | Slag rain: feet planted, the crucible brightens, a red ring fills at your feet, three slag drops fall into it (1.4 s / 1.2 s cooling) | Leave the ring, close in (frost or any melee hit during the charge douses it: stagger, no bolts for 4 s), break the crucible | Keeps 12 m away while hunting, backs off when you close within 5 m, runs once below 35% |
| Extraction Sentinel | Sentinel (humanoid machine) | The golem body with a scanner hood and lens, clamp arms and a harvest tank | Scan and clamp: an amber cone sweeps, turns red to lock, then the clamp fires along the line (1.65 s / 1 s) | Break line of sight or leave the cone, raise the ward toward it (the clamp turns aside), shoot the lens to blind it, flank its braced plate | Below 30% it braces its front plate at its post (frontal hits halved) |
| Repair Construct | Repair support | Small, tall legged machine with a weld arm and a dome | Weld beam: the arm unfolds and a cyan beam mends a wounded ally (0.6 s tell, up to 4 s of mending) | Kill it first (it cannot mend itself), any hit breaks the beam for 1.5 s, lightning | Stays 8 m back behind allies and scuttles away whenever you come within 6 m |

Only one signature plays at a time in an encounter (`SkirmishArena04E.TryClaimSignature`, with a 1.2 s gap), so a room
never stacks a burrow ring, a slag ring and a clamp line on the player at once.

**Parts** (break them for a changed fight and a material): scavenger Ember Fangs (half melee) and Ash Shoulder Guards
(takes more damage); burrower Ash Plates (frontal armour lost) and Digging Beak (no burrow); glasswing Glass Wings (no
ceiling, slowed) and Shard Gland (no spit); caster Crucible (no ranged, core exposed); sentinel Scanner (no scan) and
Core Plate (core exposed); construct Weld Arm (no mending); moth Dust Wings; crab Kiln Shell and Pincers; hatchling
Ember Fluff.

**Loot** (only 04E materials): scavenger ember crystal (55%) and ash resin; burrower ash resin (85%, 1 to 2) and ember
crystal; glasswing ember crystal (60%, 1 to 2) and coolant salt; caster coolant salt (60%, 1 to 2) and ember crystal;
sentinel regulator cog (75%, 1 to 2) and ash resin; construct regulator cog and coolant salt. Residents never give
anything.

**Voices** (`Audio/CreatureSounds.cs`): scavengers chitter and yelp, the burrower grinds and hisses, the glasswing
screeches, the caster roars like a furnace, the machines grind, whir and slam, the crab chitters and hisses, the moths
flutter and fade with a whisper, the hatchlings chitter and yelp.

The behaviour attaches to every 04E body whoever spawns it (an encounter, a quest step, a boss's adds, the debug menu):
`CreatureDirector.SpawnBody` calls `UnderheartRoleAttach04E.OnSpawned`. A hostile spawn outside an encounter treats its
spawn point as its post and a 25 m circle around it as its room.

## 2. The residents

| Species | Behaviour | Where |
|---|---|---|
| Cinder moth | Drifts in slow loops 0.8 to 2.2 m above its warm crystal, scatters when someone comes within 2.6 m or anything hits it, never a lock-on threat (it has no damaging move at all) | Service vent, refuge residents and map board, canals cool pocket, gardens study A, workshops craft point, reliquary columns and archive hall, crown staging |
| Kiln crab | Territorial nest defender: an amber ring at its strike radius fills over its 5 s patience; step out and it settles ("The kiln crab settles"); it defends only once the ring is full or it was hurt, and walks back to its nest when drawn past its 14 m leash | Canals branch cache, gardens habitat, workshops store room (territorial); refuge sample drop and gardens study B (calm) |
| Ember hatchling | Follows warmth: trails the player within 6 m (never more than 9 m from its nest), otherwise sits at the nest; hurt, it scatters instead of fighting | Canals rest, gardens study C, the Pressure House nest, the guided clutch at the service level's juvenile pocket |

Rules (`ResidentRules04E`, `UnderheartEcology.Residents.cs`):

- Placed at the frozen Underheart anchors, only in the player's current region and the regions next to it (on the street,
  the service level is prepared only within 50 m of the stair entrance). The current region fills first, at most two
  bodies a second, at most 6 per region.
- Cap by graphics preset (6, 9, 12, 16), halved while an encounter runs; over the cap the farthest leave first.
- A resident is only removed off camera, or beyond 70 m; nothing pops away in view.
- **Never hostile in Lantern Refuge** (or at the crown staging rest): calm and unable to be hurt there. At the study
  points the crab is calm too, so "study the non-hostile creatures" can never turn into a fight. Residents never give
  rewards anywhere.
- Spots are a short floor probe near the anchor, within 1.5 m of its height, with room to stand, in sight of the anchor,
  off damaging heat (`Game.Underheart.HazardAt`) and never inside a claimed encounter.
- **The Pressure House nest**: while the player is on the street within 45 m of the venue's `nest` anchor, three
  hatchlings live in the nest corner (calm, cannot be hurt, give nothing) and follow warmth inside the room only. The
  quest choice `juveniles` changes them: `shelter` settles them in the nest, `guide` moves them to the service level's
  `service.juveniles` pocket, where they follow the player within the service level.
- **A resident that dies leaves its slot empty for 3 minutes** (`ResidentRules04E.RespawnSeconds`): a defending crab
  you put down is not replaced a second later. A despawn (off camera, a claimed room) is not a death and waits for
  nothing.

### 2b. The campaign decisions change the residents from then on

Two campaign decisions are read every second by the residents code from the stored choice (`Game.Quests.Choice`), so a
loaded save shows the same world; there is no save field of its own. The table is pure (`ChoiceEcology04E`): it takes the
authored slots, applies the decisions (choice slots first) and trims each region back to 6 by taking authored moths
away, never a choice slot. A resident whose slot or temperament changed leaves off camera; a choice's new residents
settle while the anchor is out of view (nothing pops in right after the decision). Each slot's key now carries its
anchor, species, temperament and shelter (`ResidentRules04E.SlotKey`), so a new temperament always means a new body.

| Decision (asked in) | Answer | What changes in the Underheart |
|---|---|---|
| `matron_fate` (08 The Cinder Matron) | contain | She lives in the old cooling pen in her lair. The two hatchlings that wandered the gardens' third study point go back to her nest: a sheltered clutch at `gardens.matron_arena` (they stay at the nest). The gardens habitat crab settles: calm, it no longer warns you off |
| | relocate | She goes wild in the deep. Her nest stays empty and the habitat crab keeps guarding its nest (territorial, as before); the wandering hatchlings follow her down and sleep by the warm stones of the Ash Reliquary rest room (`reliquary.rest`, a sheltered clutch in a room no encounter ever claims). One archive-hall moth keeps clear of her range |
| `regent_end` (15 The Kiln Regent) | contain | The custodian keeps the Crown warm and steady: two cinder moths come back to the Heart Furnace balcony (`crown.vista`) and two young hatchlings settle in the Crown's staging refuge (calm, unhurtable, they follow you inside it) |
| | shutdown | The Crown runs cold and quiet: the staging refuge's moths leave (nothing lives in the Crown) and each Ash Reliquary hall (colonnade, archive hall) keeps a single moth |

Regions above the deep never change with `regent_end`. The board and the refuge change with the same decisions (see
CAMPAIGN.md, "Choices and their effects"). F2 > Ecology 04E shows a "Campaign choices" row with what is in force; F2 >
Campaign can start 08 or 15 directly (Start) to make the decision, and clears choices to see the world before it.

## 3. Finite encounters (the skirmish runtime)

Quest Skirmish steps call `Game.Ecology04E.BeginSkirmish(spec, out reason)`:

```
SkirmishSpec: Owner, RoomId, Center, Radius (clamped 6 to 40), Roster (species, count, elite), Reinforcements,
              HealthScale, DamageScale, and (appended, optional) Points (authored posts), Title, AllowCleared.
SkirmishHandle: Done, Failed, Status, Alive, Total, Abort(), and (appended) Defeated.
```

How it plays:

1. **Declared roster at arena points.** The roster is validated (unknown or boss species refused) and clamped (6 per
   line, 8 bodies, 4 reinforcements). Every body is reserved up front, then spawned two per frame at its post (authored
   points, or a golden-angle spread between 35% and 75% of the radius), each placed by a short floor probe at the
   room's height. The foes hold their posts until the player steps within the radius plus 10 m (or kills one from
   outside), then fight; drawn too far, they walk back to their posts.
2. **Announced reinforcements, from the declared budget only.** Once the whole roster is in the room and half of it is
   confirmed dead (or none is left standing), one reinforcement is reserved, a glowing ring and a banner announce it,
   and it arrives 4 s later at the free post farthest from the player (never within 6 m). One at a time, at least 6 s
   apart, never past the alive cap (4, 5 or 6 by difficulty). A reinforcement with no safe arrival point, or whose body
   cannot be made, is withdrawn: the total shrinks and the player is told. Its kind follows the roster's lines in turn.
3. **Done only after confirmed deaths.** The handle reports Done only when every spawned foe, the humanoid sentinel
   included, has died (health reached zero), the whole roster spawned, nothing was lost and no reinforcement is still
   reserved or outstanding. A body that vanishes without dying (removed, never built within 15 s) makes the encounter
   unwinnable and it ends without a victory.
4. **Victory**: reservations are released, every owned threat stops, the admission lease becomes a quiet aftermath
   lease (`ecology04e:aftermath:<room>`, the 04D aftermath rules: active play time plus the player leaving), the room is
   marked cleared (`Game.Underheart.MarkCleared(RoomId)`), the banner says ROOM CLEARED.
5. **Abort never counts as victory.** The player being downed, crossing the Veil, a lost or failed body, leaving the
   area for 20 s after engaging (beyond the radius plus 45 m, or another layer), the owner calling `Abort()`, a load or
   new game: reservations are released, owned threats stop, the foes walk back to their posts and are removed off camera
   (at most 3.5 s later with a puff if seen). Removed foes are never kills. The handle reports Failed with the reason
   and "Nothing was counted as a victory." The admission lease is released once the last foe is gone.
6. **Cleared rooms.** Starting a room already cleared this run reports Done at once and spawns nothing (unless
   `AllowCleared`). The same owner asking again for its running room gets the running handle (a step restart never
   doubles an encounter).

The HUD shows a small encounter line near the top while the player is in or near the room: the title, foes down of the
declared total, and the reinforcement countdown or how many may still come.

## 4. Arbitration (nothing refills a claimed arena)

- Every running encounter holds a LocalQuiet **admission lease** (`ecology04e:skirmish:<n>`, the radius plus 10 m,
  layer aware), so the automatic aggressive producers that check `LocalQuiet.Blocks` (the creature ambient spawner,
  world events, gang spots, loot world guards) stay out, and residents never place a body inside it (crabs and
  hatchlings in the room leave when it starts).
- `CreatureDirector.SpawnBody` refuses an ambient spawn whose point is inside any quiet lease.
- A new encounter is refused with a reason when two already run underground, when another encounter's arena overlaps
  it on the same layer, while a boss fight runs (`ActiveBoss`, or `Game.UnderBosses.FightActive` unless the owner is
  boss related), or when another system's admission lease covers its centre.
- The 04D encounters' orphan sweep only touches `encounter:` leases, so it never releases ours; if `Game.Encounters` is
  missing, the ecology ticks LocalQuiet itself so aftermaths still end.
- Quiet is layer aware (04E `QuietLease.Covers` uses `WorldBounds.SameLayer`): a street lease never silences the room
  below it, and the reverse.

## 5. Underground-safe spawn placement

The old `CreatureDirector.GroundAt` probed from 3 m above a point down 63 m, and spawns could fall back to an 80 m probe:
under the city that found the Underheart ceiling or the street above. Now (`UnderheartSpawnRules04E`):

- A point counts as underground by height (below `WorldBounds.LayerSplitY`) or by the Underheart probe.
- Underground probes start at most 1.2 m above the point (below any ceiling at least 2.6 m up) and reach 3.5 m below it,
  so they land on the anchor's own floor and never the street, the ceiling or a deeper level. No long fallback
  underground: a spawn with no floor nearby keeps its authored height instead of being moved to the street. The
  ecology's own placement is stricter: a resident spot without a floor is skipped, and an encounter post without a
  floor at the room's height uses the room centre.
- Surface probes are unchanged (same reach and fallback).
- The same short probe serves wander goals and habitat placement (`Creature.PickWander`, `ThinkHabitat`).
- Boss arenas only start an arena exit check on the player's own layer (`CreatureDirector.Bosses`).
- **Starter safety is layer aware**: `StarterSafety.Contains` is false for any underground point
  (`StarterSafetyRules.OnSurfaceLayer`), so the starter district's calm rule never leaks into the Underheart below it.

## 6. Debug page (F2 > Ecology 04E, Order 61)

- **Spawn role lineup**: every 04E body (six roles and three residents) in a row 9 m ahead, calm and without rewards,
  to look at bodies, parts and accessories. **Clear lineup** removes them.
- **Test encounter here**: three scavengers, a sentinel and a construct with one announced reinforcement, 12 m ahead.
- **Abort all encounters**, **Refresh residents**.
- Live lines: roles, residents placed and the cap, anchors resolved this pass, quiet leases (ours and total), every
  running encounter with its ledger, and the recent log (also written to the player log with the prefix `ECO04E`).

Module status `ecology_04e` reports Loaded, or Partial with the reason (the Underheart world not ready after 20 s of
play, or resident anchors that did not resolve).

## 6b. Bestiary labels (BESTIARY pause page)

The nine species keep `CreatureTier.Badlands` in `CreatureSpeciesDb`, so the page used to call them "Quartz Badlands"
and "At night". `UI/BestiaryPage.cs` now asks the pure `UnderheartBestiary04E` (in this folder) for them:

| Field | Shows |
|---|---|
| Region (also on an undiscovered entry) | "The Underheart" |
| Kind | the hostile roles keep their temperament (Aggressive, Territorial, Stalker); the cinder moth and the ember hatchling read "Resident, harmless", the kiln crab "Resident, guards its nest" |
| When | "Underground, day and night" |
| Where found (one line under the counts, above Behaviour) | the subregions where the player meets it, in plain words |

| Species | Where found |
|---|---|
| Ember Scavenger | Ember Canals, Pyre Gardens, Crucible Workshops, Ash Reliquary and Foundry Crown |
| Ash Burrower | Pyre Gardens and Crucible Workshops |
| Glasswing Ambusher | Crucible Workshops, Ash Reliquary and Foundry Crown |
| Furnace Caster | Crucible Workshops, Ash Reliquary and Foundry Crown |
| Extraction Sentinel | the service level, Ember Canals, Pyre Gardens, Crucible Workshops, Ash Reliquary and Foundry Crown |
| Repair Construct | the service level, Ember Canals, Crucible Workshops, Ash Reliquary and Foundry Crown |
| Cinder Moth | every region, from the service level to the Foundry Crown |
| Kiln Crab | Lantern Refuge, Ember Canals, Pyre Gardens and Crucible Workshops |
| Ember Hatchling | the Pressure House nest, then the service level, Ember Canals and Pyre Gardens |

`Bestiary04ETests` cross-checks the table against the shipped data (every quest Skirmish roster at an `Under:` anchor,
every contract grammar's roster and regions, the Choir mechanism waves, the boss reinforcement batches by room, and the
resident slots), so a new encounter in an unlisted region fails the test instead of mislabelling the page.

## 7. Files

Owned and added (`Assets/NeonSyndicate/Scripts/Runtime/Creatures/Underheart/`): `UnderheartEcology.cs` (replaces the
stub), `UnderheartEcology.Skirmish.cs`, `.Residents.cs`, `.Ui.cs`, `UnderheartRoles04E.cs`, `SkirmishRules04E.cs`,
`ResidentRules04E.cs`, `UnderheartSpawnRules04E.cs` (pure), `UnderheartRoleAttach04E.cs`, `CreatureRoleDriver04E.cs`
plus `.Scavenger.cs`, `.Burrower.cs`, `.Glasswing.cs`, `.Caster.cs`, `.Construct.cs`, `SentinelDriver04E.cs`,
`ResidentDriver04E.cs`, `Creature.Role04E.cs` (a partial of `Creature`: the role seam), `UnderheartAccessories04E.cs`
(crucible, chimneys, weld arm, dome, pincers, sentinel hood, lens, clamps and tank), `OwnedRing04E.cs`,
`ChoiceEcology04E.cs` (pure: the residents for `matron_fate` and `regent_end`). Tests:
`Tests/EditMode/Ecology04ETests.cs`, `Tests/EditMode/ChoiceOutcomes04ETests.cs` (the choice ecology, with the board and
refuge outcomes), `Tests/EditMode/ChoiceConsequences04ETests.cs` (every answer changes play).

Owned and changed: `CreatureLooks.cs` (eight appended looks), `CreatureSpecies.cs` (the nine 04E entries and their
combat data), `CreatureAttacks.cs` (the 04E sets replace the aliases), `CreatureParts.cs` (the 04E parts),
`Audio/CreatureSounds.cs` (the 04E cries), `CreatureDirector.cs` (short probes, no long fallback underground, ambient
refusal inside quiet leases, the role attach call), `CreatureDirector.Bosses.cs` (same-layer arena check),
`AI/StarterSafety.cs` and `AI/StarterSafetyRules.cs` (layer aware).

Hooks outside the ownership list: `Creatures/Creature.Motion.cs`, three lines in `HuntSteer` marked "04E role hook" so a
ranged role keeps its distance (`_roleKeepRange`, 0 for every other creature, so nothing else changes).

## 8. Tests

`Ecology04ETests` (13 tests): the six roles are distinct and readable (unique roles, tells and punish windows at least
0.6 s, three counters each); every new species has its own body (pairwise different silhouettes and different from
the template it replaced, by plan or 12% in size, plus at least two shape features); attack sets are per role (no
template move names, readable wind-ups, two damaging moves per hostile, none for moths and hatchlings, the crab's clack
first); parts, cries and loot are their own (04E materials only, no surface habitats); rosters are clamped and
validated; reinforcements only come from the declared budget and respect the alive cap (including 400 random runs);
victory requires confirmed deaths (lost, failed, cancelled and aborted never win; an outstanding reinforcement blocks
victory); the watch rules and layer aware arenas; residents are never hostile in the refuge (plans, crab mood, hatchling
goals, moth drift); residents live in the current and adjacent regions only (and the caps); underground probes stay on
the anchor's floor (a street, ceiling, floor and lower level column); starter safety is layer aware.

`ChoiceOutcomes04ETests` (ecology part): without a decision the authored slots stand unchanged; every combination of
the two decisions stays at or under 6 per region on real anchors in their own region, residents only, listed by the
bestiary, never hostile in a refuge, unique keys, choice slots never trimmed; contain and relocate differ as the table
says (nest, habitat crab temperament, the guarded brood, fewer reliquary moths); regent contain and shutdown differ in
the Crown and the reliquary and leave the upper regions alone; temperament overrides are bounded (never hostile in a
refuge) and keyed.

## 9. Honest limits

- Nothing here has run in Unity. Bodies, accessories, poses, rings, beams and timing are verified only as compiled code,
  offline maths and a proportion check of the generated meshes (not a render).
- Resident anchors and the Pressure House `nest` anchor come from the UNDERWORLD and VENUES streams. In this sandbox the
  Underheart world is a stub (`TryAnchor` returns false), so no resident is placed here; the module reports Partial with
  that reason until the real world is merged.
- The guided clutch follows the player within the service level only; there is no escort across the stairwell (the
  hatchlings appear in the juvenile pocket once `guide` is chosen).
- The Matron herself is not shown after her fight (her body is a boss controller, not a creature); her pen or her range
  reads through her brood, the habitat crab and the moths. The decisions change residents only: the deep's heat,
  hazards and lights are the UNDERWORLD stream's and stay as they are.
- The bestiary labels are fixed (section 6b): the 04E species no longer read "Quartz Badlands". Their stored tier is still
  `CreatureTier.Badlands` (quest pay scaling and the creature reputation table read it); nothing else changed.
- Foes return to their posts when the player steps out of the room's band (the radius plus 10 m); a player can reset a
  fight that way, by design of a room encounter, but they do not heal while walking back.

## 10. What a native Unity session must check

1. F2 > Ecology 04E > Spawn role lineup on a flat street: each of the nine bodies reads as its own creature (scavenger
   low and spiny, burrower long plated with a beak, glasswing glassy winged, caster tall with a glowing crucible and
   chimneys, construct small and tall legged with a weld arm, sentinel with hood, lens, clamps and tank, moth small
   winged, crab wide with pincers, hatchling round headed). Check accessories sit on the right bones and move with them.
2. F2 > Ecology 04E > Test encounter here: the foes wait at their posts; step in and they engage; the HUD line counts
   down; after half fall the reinforcement ring and banner appear and one foe arrives 4 s later; killing everything
   shows ROOM CLEARED. Try Abort: they walk off and vanish off camera, the line reads "Nothing was counted".
3. Per role: the scavenger hesitates when faced and darts in otherwise; the burrower's dust trail, ash ring and rear
   are readable and stepping out avoids the hit; the glasswing hangs on a real ceiling (needs one 3.2 to 14 m up),
   drops after its tell, and lands stunned when shot down; the caster's crucible brightens, its red ring fills and slag
   falls into it, and a melee hit or frost during the charge douses it; the sentinel's amber cone turns red and the
   clamp misses if you step aside or ward it, and shooting the lens blinds it; the construct's cyan beam heals a hurt
   ally and breaks when hit.
4. Underground (with the real Underheart merged): residents appear only near the player's region, moths loop near warm
   crystals and scatter, a canals crab shows its amber ring and settles when you step out, hatchlings follow and stay
   near their nest, nothing in Lantern Refuge can be hurt or turns hostile.
4b. The campaign decisions (play 08 and 15, or F2 > Campaign > Start on them): after `matron_fate` contain, two hatchlings
   sleep at the Matron's nest in her lair and the gardens habitat crab no longer shows its amber ring; after relocate,
   the nest is empty, the habitat crab still warns, and two hatchlings sleep beside the reliquary rest point (they stay
   there, they do not follow). A resident you kill is not replaced for 3 minutes. After `regent_end` contain, moths drift on the Crown balcony and two
   hatchlings follow you in the staging refuge; after shutdown, the Crown has no residents and each reliquary hall one
   moth. Nothing should pop in or out while you watch.
5. Spawns under the city land on the Underheart floor (no creature on the street above, none stuck in the ceiling), and
   the starter district's calm rule does not apply below it.
6. Frame rate in a full encounter with residents near (eight bodies, a reinforcement, rings and the weld beam).
7. Pause > BESTIARY after meeting each species (F2 > Ecology 04E > Spawn role lineup discovers them fast): the entry
   says "The Underheart" and "Underground, day and night", the residents read as residents, and the "Where found" line
   sits under the counts, clear of the emblem, without pushing the drops line off the page.
