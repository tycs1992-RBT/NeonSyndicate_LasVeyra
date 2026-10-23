# Creatures, bosses and the bestiary

Everything here is generated in code like the rest of the game: bodies (CreatureBuilder, BodyBuilder), animation
(CreaturePose, the people rig), voices (CreatureSounds), effects and UI. Nothing in this file has been run inside
Unity yet: it compiles in all twelve configurations and the pure maths is tested offline (see the end).

## The species

| Species | Body | Where and when | How it fights |
|---|---|---|---|
| Glass Mite | Swarm of tiny crystal insects | City construction ground and the quarry, at night | 8 to 16 fly as one; bites come in waves, never more than 4 at once |
| Neon Jackal | Quadruped pack | City parks at night; the Badlands at night | Packs of 3 to 5 take flanking slots, one bites at a time; howls call the pack |
| Shard Golem | Humanoid (golem) | Construction ground and the quarry at night | Territorial: warns, then slams (ground ring) and throws shards |
| Hollow Shade | Floating wisp | Grave Frequency streets and the cemetery at night; Inner World | Nearly invisible until close (the Third Eye shows it), drains crystal energy; hangs behind you for 2 to 3 s, then strikes |
| Hollow | Humanoid (shadow) | Inner World | Packs; a shimmer, then a blink to your blind side |
| Mirror Wraith | Humanoid (your shape in glass) | Inner World | Frontal glass guard: flank it or break it with a charged heavy |
| Crystal Basilisk | Quadruped lizard | Inner World; the quarry | Territorial: bite, a wide tail sweep, a charge, and a crown gaze that only hurts you while you hold still |
| Violet Wyvern | Flyer | Inner World | Circles, dives, breathes violet fire, climbs away |
| Frost Stalker | Quadruped | The Frostline, day and night | Stalks to your blind side and pounces |
| Rime Griffin | Flyer | The Frostline, most near Rimeglass Lake | Territorial: screams a warning, then dives talons first |
| Snow Hollow | Humanoid (frozen Hollow) | The Frostline at night | Its touch chills your stamina |
| Sun Griffin | Flyer | The Badlands by day | Territorial, dives out of the glare |
| Sand Golem | Humanoid (golem) | The Badlands and the quarry | Slams and packed-sand throws |
| Dune Wyrm | Burrowing serpent (great beast) | The Badlands, roaming | Hunts by vibration: engines and gunfire draw it; stand still on foot and it loses you. A dust trail, then an eruption under you |
| Quartz Drake | Flyer (great beast) | Roosts on the Glass Row Canyon walls (it takes wing rather than step off) | Ember breath strafes, lands to fight up close |

Bosses: Glacier Titan (Titan Hollow, Frostline), Shard Golem Foreman (the quarry floor), Mirage Djinn (the Mirage
Flats at about (1400, -1400), deep in the south-east Badlands, at night only), Mirror Wraith Queen (Crystal Manor,
inside the Inner World only).

## The ambient ecology

Every 7 to 30 seconds (more often at night in the wilderness, most often in the Inner World) the spawner reads where
the player is and picks a species from the table in `CreatureEcology` (Creatures/CreatureLogic.cs). The Outer City
only has creatures at night. Weather shifts the mix (flyers stay down in storms and sandstorms, shades and stalkers
love fog, golems and the wyrm rise in sandstorms).

A group's bodies are built on worker threads first; the group then appears 60 to 140 m away, out of the camera's view
(flyers start circling at height), never in a building, on a road, in the canal or inside a ridge, and never within
70 m of a mission objective blip or inside an arena whose boss is still alive. Caps by graphics preset: Low 3 groups
and 8 bodies, Medium 4 and 12, High 6 and 18, Ultra 7 and 22 (a mite counts a quarter, great beasts and bosses two).
Creatures that are not Persistent are removed when more than 230 m away. Inner World creatures fade when the Veil
closes; calm city wildlife stays behind when you enter it, and groups still being built for the world you left are
dropped. Flyers placed in the air fly from the start (their ground probe reaches 400 m down), a walker that steps off a
ledge falls to the real ground, and a flyer killed in the air drops to the ground below it.

## Fighting creatures

- Every attack has a readable wind-up (never under 0.32 s, 0.55 s for knockdowns and unblockable hits; longer on
  Story, a little shorter on Veteran), an active window and a recovery you can punish. A red "!" shows over a
  creature about to strike you; an orange "!!" means it cannot be blocked (dodge it); an arrow at the screen edge
  warns of a blow from out of view. Area attacks fill a ground ring before they burst.
- H8S tell language (Combat/CombatTellsH8S.cs, Docs/Ascension04OH/COMBAT_READABILITY_H8S.md): the ground marks use
  the glyph colours too (red edge: you may guard it, orange edge: dodge or leave), and every ring's edge is drawn
  where the blow really hits your feet. All-round tail sweeps fill a ring around the body; pounces and ground charges
  draw a lane along their path that stops following you at 70 percent of the wind-up, and the blow then goes where
  the lane points, so a sidestep out of the lane works. Wind-ups have class minimums per difficulty (Veteran keeps
  the floors above) and an area blow always leaves time to react and complete one dodge roll. People and
  humanoid creatures winding up a melee blow on you show the same "!" or "!!". Blows on you land at least a moment
  apart (0.55 s Story, 0.35 s Balanced, 0.22 s Veteran): an attacker whose blow would land on top of another waits
  a beat and keeps circling.
- No instant deaths: one creature hit takes at most 25 percent of your health on Story, 35 on Balanced, 50 on
  Veteran.
- Creatures share the NPC melee attack limit (a whole swarm counts as one attacker). A Glass Mite's nibble stings but
  never makes you flinch, so a swarm cannot stun-lock your dodge.
- Packs and swarms: creatures of a pack species spawned one at a time (quests, events) join a live group of their kind
  and tag within 25 m, so they flank and bite in waves like an ambient pack. A pack that calmed down answers the next
  alarm again (a member running away or heading home to heal keeps going).
- Poise: hits wear it down (melee most), a broken poise staggers the creature (staggered creatures take 25 percent
  more). Parrying a creature staggers it. Small creatures flinch out of a wind-up when shot. A stagger breaks a
  humanoid creature's slam, throw or blink (its ground ring fades); the Foreman's stunned crown and the Titan's kneel
  do the same, and a broken Foreman slam takes its widening rings with it. Golem slams and throws show the "!!" or "!"
  warning like any other wind-up.
- Weak points (glowing crystal cores) are criticals, like headshots. On golems, the Glacier Titan and the Foreman that
  is the glowing chest core (shots and powers within about 0.4 m times the body's scale); their stone heads are not
  weak points. A Third Eye scan marks them for 20 s.
- Elements: each species has a weakness (1.5x) and a resistance (0.6x); fire is Ember, crystal powers are Volt, your
  melee carries your weapon's element. The skill tree's creature damage bonus (SkillMods.CreatureDamage) applies.
  Burning counts as Ember too (and goes through the Foreman's plates); a wyrm that dives under the sand puts it out.
- Big bodies are heavy: they turn slowly (flank them), barely move when hit, and the camera pulls back to frame them.
  Shoves (a car, a blast, the Gravity Well) stop at walls instead of carrying a body through them.
- Breath opens aimed straight at you (a flyer overhead breathes down) and then sweeps after you a little slower than a
  sprint carries you round it. Thrown and spat shots burst on walls, cars and people as well as on you.
- Calm humanoid creatures (a golem minding its ground) are not enemies until provoked or hunting you: no minimap dot or
  elite bar, and companions leave them alone. A gunshot within earshot makes a territorial one turn to look (then it
  warns as usual); Hollows come for you. Companions do fight creatures that hunt you. Lock-on hops to the next enemy
  when a locked creature dies.
- Stalkers follow what they last saw: break the line of sight and one heads for where you were, and gives up after 10 s.
- Fighting creatures is never a crime, and gunfire or blasts near a creature fight do not count as crimes either. A
  shot that hits a creature, lands within 15 m of one, or passes close by one within 45 m of the muzzle (calm ones
  too) is a creature fight.
- Deaths: a short death pose, then the body dissolves into crystal sparks or shadow smoke (golems crumble, wraiths
  shatter, bosses flare and burst). No ragdolls. Loot (materials) and Lumen go straight to you.

## Bosses

H8S: a phase opening shows a "PHASE 2 / 3" banner with a deep bell for every boss; the Foreman, the Djinn and the
Queen also hold their own swings for a moment while they roar (like the regional guardians) and drop rings that were
scheduled but never shown, and support golems and mirror copies wait a moment before their first swing. Boss rings
keep their elemental fill with an orange edge at the burst's real reach. The Glacier Titan's script is unchanged.

Coming near an arena finds it (a "B" map icon; during a mission it is found without the banner). Walking in once found
starts the fight, but never during a mission or a world event at the arena, nor for 10 s after one ends or after a
load; if you were standing in the arena then (or a fight there just ended), step out and walk back in. Each boss has three phases (at two
thirds and one third of its health); each phase opens with a roar and a summon. Summons and copies appear only on
open ground with a clear line to the boss (never inside a pillar or behind a wall). After a ring hits you, the rest of
its volley passes harmlessly for 0.6 s, and ring hits only flinch lightly, so a volley never chains into a stagger lock.

- Glacier Titan: stomp shockwave, ice spikes under and ahead of you, a frost-breath line of rings. Kneels at each phase
  change: its core takes 60 percent extra damage while it kneels. Summons Snow Hollows.
- Mirage Djinn: sandstorm rings and sand volleys. Splits into mirages that cast no shadow and pop at a touch (only as
  many as there are bodies built ahead; more are ordered as they are used). Summons Sand Golems.
- Shard Golem Foreman: hazard plates halve the damage it takes until its poise breaks once. Crystal powers (Volt)
  stun its crown. Slams in widening rings, crystal rain, calls Glass Mite swarms.
- Mirror Wraith Queen: glass shatters across the floor in rings; mirror copies of you share her guard (break one guard
  and every mirror cracks).

Leaving the arena (75 m past its edge), dying, or (Queen) leaving the Veil ends the fight: the boss withdraws and
waits. A defeated boss stays defeated (saved) and drops an Epic or better gear item plus its heart material.

## Bestiary and loot

- Seeing a creature on screen within 40 m opens its entry (a toast on the right).
- Scanning: with the Third Eye on, keep a creature near the middle of the screen within 35 m for 1.2 s. The full entry
  (weak point, element, health, poise, drops) opens, the first scan of each species pays Lumen, and its weak point is
  marked for 20 s.
- Pause menu tab BESTIARY: every species (undiscovered ones as question marks), kills, and a MATERIAL SATCHEL view.
- Loot comes from each species' table; SkillMods.MaterialFind adds to every chance; elites roll twice.
- **Breakable parts (Phase 6):** every species has at least one (bosses two or three): claws, fangs, wings, crowns,
  core plates. Each has its own health and glowing crystal shards; breaking it drops its material at once, staggers the
  creature and changes the fight (halved melee, grounded flyers, bared cores, no breath or gaze, slower bodies). Each
  broken part raises the loot-gun chance at death. The Third Eye marks intact parts; the BESTIARY lists parts once you
  have broken them; a Resonance Burst on a part deals double part damage. Full table in [COMBAT_LOOP.md](COMBAT_LOOP.md).

## Debug page (F2, tab "Creatures")

Spawn any species near you (packs as packs; toggle whether they spawn hostile), fight any boss in its arena (moves
you there, sets night for the Djinn, opens the Veil for the Queen), end a boss fight, forget defeated bosses, toggle
the ambient ecology, kill or remove every creature, fill the bestiary. The top line shows live counts, the caps and
the habitat the spawner sees.

## For other systems (quests, events, missions)

`Game.Creatures` (CreatureDirector):
- `Spawn(speciesId, position, yaw, options)` and `SpawnGroup(speciesId, center, count, options)`. Options: Tag,
  Hostile (default true), Persistent (mission creatures: never removed for distance), Elite, HealthScale,
  DamageScale, DisplayName, Seed. Non-humanoid bodies arrive a few frames later (they are built on worker threads);
  they count for AliveWithTag at once. `Spawn` of a boss id goes to `SpawnBoss`; `Spawn` of a non-humanoid species
  joins a live group of the same species and tag within 25 m, or starts one (mites share the swarm's body). Prefer
  `SpawnGroup` for a whole pack.
- `AliveWithTag(tag)`, `WithTag(tag, list)`, `DespawnTag(tag)`, `DespawnAll(puff)`, `KillAll()`. `DespawnTag` of the
  tag the boss being fought carries ends that fight first (its summons and copies, tagged `BossAddsTag(bossId)`, go
  too).
- `Killed` event (every death, with species, position, tag and whether the player did it).
- `AmbientEnabled`: switch the ecology off during a mission (calm wildlife further than 40 m fades out).
- `HostileNear(position, radius)`: true during creature fights (the wanted system uses it).
  `CreatureInLineOfFire(muzzle, aim, range)`: a shot passes close by a living creature (calm ones too).
- Bosses: `SpawnBoss(bossId, position, yaw, options)` (a quest boss anywhere, full fight script, it becomes
  `ActiveBoss`; the options' Tag, Hostile, Persistent, HealthScale, DamageScale and DisplayName are kept, and Seed 0
  gives the boss its usual face and the body built ahead near its arena), `StartBossFight(bossId, force)`,
  `EndBossFight()`, `RevealArena(bossId)`, `IsBossDefeated(id)`, `IsArenaFound(id)`, `ActiveBoss`, `ActiveBossBrain`,
  `Arenas`. The boss being fought is never removed for distance.
- `Game.Compendium`: `IsDiscovered`, `IsScanned`, `Kills`, `MarkSeen`, `MarkScanned`, `RecordKill`, the
  `Discovered` event.

Saves: `SaveData.Creatures` (CreatureSave: Seen, Scanned, KillIds, KillCounts, BossesDefeated, and the appended
ArenasFound). CreatureLogicTests freezes its field list.

## Code map

| File | What |
|---|---|
| Creatures/CreatureSpecies.cs | The bestiary data (stats, habitats, loot, lore) and combat data per species |
| Creatures/CreatureAttacks.cs | Attack tables, timing (difficulty and body size), attack choice, humanoid creature moves |
| Creatures/CreatureLogic.cs | Pure rules: ecology table, caps, loot, perception, temperament, fairness caps, swarm and flight maths |
| Creatures/CreatureFightRules.cs | Pure rules: nibbles, the golems' chest core, shots at wildlife, shoves and walls, breath aim, the shade's strike, ring respite, the arena start rule |
| Creatures/Creature*.cs | The non-humanoid creature: body, damage, brain, attacks, motion (flight, burrowing, swarming) |
| Creatures/CreatureRig.cs, CreatureGroup.cs | The skinned body with hit volumes; packs and swarms |
| Creatures/CreatureTelegraphs.cs | Ground rings and creature projectiles |
| Creatures/CreatureAbilities.cs | Humanoid creature abilities and their deaths |
| Creatures/CreatureBoss.cs | Boss phases, rings, summons, mirages and mirror copies |
| Creatures/CreatureDirector*.cs | Spawning, worker-thread bodies, ecology, arenas and bosses, the debug page |
| AI/NpcController.Creature.cs | Humanoid creatures as NPCs in creature mode |
| Gameplay/Compendium.cs, UI/BestiaryPage.cs | Sightings, scans, kills, the BESTIARY page |
| UI/HudCreatures.cs | Creature bars, wind-up warnings, the boss bar's data |
| Audio/CreatureSounds.cs, Combat/Fx.Creatures.cs | Voices and creature hit effects |

## What to check in Unity first

1. F2 > Creatures: spawn each species near you in daylight and watch it walk, turn, attack and die (feet planted,
   wings, the wyrm's dust trail and eruption, the swarm, the shade's ghost look).
2. Fight a Neon Jackal pack and a Shard Golem: wind-up warnings, parries, staggers, damage numbers, crystal hit
   sparks (no blood), loot toasts, Lumen.
3. Each boss from the debug page: rings readable and fair, phase changes, summons, the boss bar, the death burst.
4. Night in a park, on construction ground and in the Grave Frequency; the Frostline and the Badlands by day and
   night; the Inner World: creatures appear out of view and despawn when far.
5. Third Eye scanning, the BESTIARY tab with mouse and with a pad, saving and loading.
6. Performance: Perf flythrough before and after; with 18 creatures nearby the creature scripts should stay under
   0.6 ms per frame on High (the profiler's Creature and CreatureDirector entries).
7. The review fixes (code-level only so far): griffins and wyverns spawned in the air fly at once, the drake takes wing
   off its roost, flyers killed in the air drop; a swarm never stun-locks you; shooting a calm golem from 40 m brings
   no police; an ambient Hollow Shade strikes from behind after a few seconds; boss ring volleys hit once and never
   lock the dodge; F2 "fight a boss" spawns one boss; staggering a golem breaks its slam; chest-core shots on golems
   show critical numbers; finish a mission inside the quarry or Titan Hollow and the boss waits until you step out
   and back in; a quest's boss has its phases, rings, summons and boss bar.
