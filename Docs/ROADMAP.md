# Roadmap

This page compares the game as it is today with the master blueprint, the long-term plan for a full-length game.
Every phase and major system of the blueprint gets a status, checked against the code. The blueprint itself is not
kept in the project, because it names commercial games and products as references; this page describes those
generically.

## How to read this page

| Status | Meaning |
|---|---|
| **Done (Phase 3)** | Built in Phase 3. It compiles and passes offline tests, but it has **not been run in the Unity editor yet**. |
| **Done (earlier)** | Already in the game before Phase 3. |
| **Partly done** | Some of it exists. The row says what is there and what is missing. |
| **Not started** | Nothing of it is in the project yet. |

**Phase numbers.** The blueprint numbers its build phases 0 to 10. The project's own deliveries have their own
numbers: Phase 2 brought people, combat, controller support and graphics, and Phase 3 is this delivery. The two lists
are different: the project's Phase 3 covered parts of blueprint Phases 0, 1, 5, 6, 7 and 9. On this page, "blueprint
Phase N" always means the blueprint's numbering.

## 1. Big decisions (blueprint section 1)

| Decision | Status | Notes |
|---|---|---|
| Stay in Unity, do not switch engines | Done (earlier) | The project is on Unity 6.3 (6000.3.16f1). |
| Move to URP | Partly done | An opt-in URP beta exists; Built-in is still the default (see Graphics below). |
| Upscaling and real-time global illumination from Unity 6.7 | Not started | Needs Unity 6.7 (see Graphics below). |
| Humans from a purchased character-creation tool | Not started | People are still built in code (see Art, audio and tools below). |
| Creatures from a purchased animation and riding pack | Partly done | 19 creatures exist, built and animated in code; nothing purchased, no riding. |
| Animation clips, motion matching, the pose solver as a final layer | Not started | All motion is procedural. |
| Audio middleware and licensed music | Not started | All sound is synthesized in code. |
| Co-op networking (architect now, ship single player first) | Not started | See Multiplayer readiness below. |
| AI tooling: Unity's plugin for Claude Code, the command line tools, batchmode tests | Done (Phase 3) | `CLAUDE.md`, the build and test scripts, EditMode tests. Installing the optional plugin is Tyler's step in Claude Code (`CLAUDE.md` shows the two commands). |

## 2. The blueprint's build phases (blueprint section 7)

| Blueprint phase | Status | What exists, what is missing |
|---|---|---|
| 0. Tooling and safety net | Done (Phase 3) | `CLAUDE.md`, `BuildScripts`, `tools/build.ps1` and `tools/test.ps1` (plus `build.sh` and `test.sh` for the Mac), EditMode tests, `.gitignore` and `.gitattributes` with Git LFS rules, and the golden shots and perf flythrough ahead of plan. Not yet confirmed: the scripts' first real run on Tyler's PC. Reading the Unity Console live from Claude Code needs Unity's optional plugin. |
| 1. Render pipeline migration | Partly done | The opt-in URP beta: switch menus, pipeline assets, URP shaders, the post effects and the graphics presets. Missing: a first run in Unity, the done line (no magenta materials, 60 fps, golden shot differences approved) and making URP the default. |
| 2. Engine upgrade and architecture refactor | Partly done | Done: an append-only save format with a version field, one save block per system and freeze tests. Not started: the Unity 6.7 upgrade, splitting the code into several assemblies, entity ids and request methods, district scenes loaded on demand, and a PlayMode smoke test of the 12 missions. |
| 3. Character pipeline | Not started | People are still built by `BodyBuilder` and moved by the game's own pose solver (`HumanoidPose`). Missing: characters from a purchased character-creation tool, motion capture clips, Unity's Animator, motion matching, lip sync. (The blueprint's Phase 3 is not the project's Phase 3.) |
| 4. Vertical slice: one district at full fidelity | Not started | It needs purchased art. Pieces already in code: creatures and bosses, races, contracts and creature hunts, weapons and vehicles. Missing: the full-fidelity district, audio middleware, the new UI framework and the three-person playtest. |
| 5. Creatures, mounts, taming | Partly done | Done (Phase 3): the creature framework, the ecology spawner, loot tables and the bestiary, with 19 species in game (the done line asks for 12). Not started: taming, the two flying mounts (Rime Griffin, Quartz Drake) and any purchased animal controller. |
| 6. Content engine | Partly done | Done (Phase 3): quests, contracts and world events as data with a validator, the event director, faction reputation, crafting. Partly: dialogue lines and choices live inside quest data; cinematics are scripted camera shots. Not started: story missions as data, a dialogue language, Timeline cinematics, the economy ledger, an automated playthrough bot. |
| 7. Scale out the world | Partly done | Done: the nine city districts and the Quartz Badlands (earlier), the Inner World and the Frostline (Phase 3), and the wilds (2026-10-24: five lands, rolling ground, landforms and 17 places to find around the city, Docs/WILDS.md). Not started: full-fidelity art, enterable interiors, air traffic lanes, the 40 story missions. |
| 8. Breadth content | Partly done | 19 of 60 side quests, 90 of 300 collectibles, 46 world events; more under Content targets below. |
| 9. Performance and QA gates | Partly done | Done (Phase 3): golden screenshots with a compare report, a perf flythrough that records frame times, EditMode tests including save round trips. Not started: nightly automatic runs, PlayMode tests, a flythrough per district, and pass or fail gates (frame time, garbage collection spikes, memory). |
| 10. Co-op | Not started | Nothing is networked. See Multiplayer readiness below. |

The blueprint's "this week" list: committing to Git with LFS is Tyler's step (the project ships the Git files;
`Docs/TOOLING.md` section 6); `CLAUDE.md` and the build and test scripts are done (Phase 3); starting the URP work on
Unity 6.3 is done as the opt-in beta (Phase 3); buying tools only when the phase that needs them starts is advice to
keep.

## 3. Systems (blueprint section 4)

| System | Status | What exists, what is missing |
|---|---|---|
| Rendering: URP, Volume overrides, custom passes as Render Graph features | Partly done | The URP beta does exactly this (a runtime Volume plus a Render Graph feature for eye adaptation and the vignette). It is opt-in and untested in Unity. |
| City generation: keep the layout brain, place modular art kits with it | Not started | `CityLayout` stays the layout brain, as planned. Every building, road and prop is still a procedural mesh. |
| Characters: purchased characters, code-built bodies only for far crowds | Not started | Every person is built by `BodyBuilder` (one skinned mesh per person). |
| Animation: clips, Animator, motion matching, the pose solver on top | Partly done (Phase 4) | Still no clips: `HumanoidPose` now drives a gait built from published human kinematics, two-handed weapon holds by inverse kinematics, and travel and riding poses. Clips and motion matching would need purchased motion capture. |
| Combat: hit-stop, camera shake, root-motion attacks, creature hitboxes per bone | Done (earlier and Phase 3) | Hit-stop (45 to 110 ms) and camera shake came in Phase 2. Attacks lunge with procedural motion, not clips. Creatures have a collider per hit volume, with glowing weak points (Phase 3). |
| Guns: a modular weapon system | Partly done (Phase 4) | Procedural loot guns: 8 types, 6 makers, 7 part slots with 4 variants each, six rarities, four elements, 15 traits, 17 named uniques, weapon cores at the Forge. Still missing: part swapping by the player and the blueprint's crystal archetypes. The 20 fixed weapons stay. |
| Crystal powers: into the Shard tree, effects in VFX Graph | Partly done | The 11 powers still unlock through spell stones and the story; the Shard tree boosts them (damage, cost, energy). Effects are code-made particles. |
| Loot in the world: chests, caches, keys | Done (Phase 5, untested in Unity) | 140 chests (common, sealed with level-scaled guard crews and a daily bounty board, Veil Vaults inside the Inner World), 40 hidden satchels for the travel powers, Chest Keys, the Collector, the Third Eye loot sense and an 8-quest Gunsmith arc with the new GiveGun step. `Docs/LOOTWORLD.md`. |
| Gear: keep it, feed it from creature loot | Done (Phase 3) | Creatures drop materials that the Forge turns into gear; bosses drop an Epic or better piece. |
| Progression: Body, Mind and Shard trees, 30 nodes each | Done (Phase 3) | 90 skills with signature skills; points from levels and quests. |
| Vehicles: hover physics rewrite, flyers, mounts as vehicles | Partly done (Phase 4) | 30 hover cars plus 4 hover bikes (riders in view, leaning into turns) on the original raycast suspension. Players can fly with the Flight travel power. Still missing: the hover physics rewrite, flying vehicles, mounts. |
| Traffic and pedestrians: air lanes, daily schedules | Not started | Ground traffic with signals. Pedestrians flee, cower, raise their hands, phone the police and fight back, but have no schedules. |
| Wanted level becomes a Threat system | Not started | The five-star police wanted level is unchanged. Creature fights simply do not count as crimes (Phase 3). |
| Gangs become factions with reputation | Done (Phase 3) | 14 factions (see Story and world below). |
| Missions: one data-driven runtime | Partly done | Guild quests and contracts run as JSON on one step runtime with a validator: 24 step types and 7 fail rules. The 12 story missions are still C#. Missing from the blueprint's list: chase, stealth and race objectives and a music-state action. A failed quest restarts from its beginning (no checkpoints). |
| Dialogue: a text-based dialogue tool | Partly done | Quest data carries dialogue lines and choices of 2 to 4 options. No dialogue language, no dialogue files. |
| Cinematics: Timeline and Cinemachine | Partly done | Scripted camera shots (the Cinematic quest step, `PlayerCamera.SetCinematic`). No Timeline or Cinemachine. |
| Creature AI | Done (Phase 3) | Written in the game's own C#: temperaments, packs, swarms, flight, burrowing, attacks that warn first, boss phases. |
| Taming and companions | Not started | Story companions (Luma, Ion) follow you in missions, and quests can add a following ally, but there is no taming and no companion system. |
| Crafting: a recipe database | Done (Phase 3) | The Forge's recipes are a C# table (`ForgeDb`) with unlock rules as data, not ScriptableObjects. |
| Economy: two currencies and a source and sink ledger | Partly done | Cash and Lumen, properties, robberies, shop discounts, Forge fees and salvage. Missing: Shards as a second currency, and the ledger that logs every transaction (`Economy.Earn` and `Spend` already take a reason text a ledger could record). |
| Living world: NPC schedules and an ambient event director | Partly done | The world event director (46 events, Phase 3). Weather and time of day drive lighting, wet roads and creature spawns. No NPC schedules. |
| Audio: middleware events | Not started | Effects, six radio stations and creature voices are all synthesized in code. |
| UI: Unity's UI Toolkit | Not started | Everything, including the new tabs, is drawn with IMGUI through `UIKit`. |
| Save system: a versioned schema, network-friendly ids | Partly done | JSON with a version field, append-only fields, one block per system, freeze tests. Quests, species, materials and factions use stable text ids. Missing: ids for live objects. |
| Streaming: district scenes and Addressables | Not started | The whole city is built at boot; props, lights, people, traffic and creatures appear near the player. |
| Multiplayer | Not started | See below. |
| Testing: EditMode and PlayMode tests, golden screenshots | Partly done | EditMode tests, golden screenshots and the compare report (Phase 3). No PlayMode tests. |

**Weapon feel** (blueprint section 4.3): Partly done. In: hit-stop, camera shake on hits, recoil that kicks the
camera and the arms, muzzle flashes, impact effects and bullet holes by surface, creature hit sparks, damage numbers.
Missing: shell ejection, and separate close, mid and far gunshot sounds (gunshots are synthesized with an echo tail).

**Creature-part crafting** (blueprint section 4.3): Done (Phase 3). Each species drops its own materials (30 in
all), and Mythic gear needs a boss heart. Recipes make gear, infusions, brews and upgrades. Phase 4 added weapon cores: the Forge's ARMORY
tab infuses elements into loot guns, ascends their rarity, raises their level and scraps them.

**Vehicles** (blueprint section 4.4):

| Item | Status | Notes |
|---|---|---|
| Hover-car physics rewrite (thruster springs, hover height, banking) | Not started | |
| Flyers and air lanes in three height bands | Not started | |
| Mounts that drive like vehicles | Not started | |
| Customization | Partly done | Jax Calder Customs: repair, paint colours, engine and armor levels 1 to 3, bulletproof tires, nitro. Missing: wraps, body kits, free colour and metallic paint, five levels, a garage with a turntable camera. |
| Damage | Done (earlier) | A health pool, dents (body deformation), smoke, fire and explosions, popped tires. Panels do not come off. |

**Multiplayer readiness** (blueprint section 4.6):

| Rule | Status | Notes |
|---|---|---|
| A networking stack for 4-player co-op | Not started | |
| Every gameplay object has a stable entity id | Not started | |
| State changes go through request methods, never direct writes from input | Not started | |
| Mission, economy and world state are serializable structures | Partly done | The save blocks are serializable; live mission and quest state is not. |
| No static mutable gameplay state | Not started | The game keeps its live systems in static fields (`Game.X`) and clears them with `ResetStatics`; meeting this rule needs a refactor. |
| A fixed random seed per world event | Partly done | The event director and creature bodies use seeded random numbers. |

**Why the code is still one assembly** (blueprint section 4.7). The blueprint splits the code into about 16 assemblies
(asmdefs), one per area. The project keeps one runtime assembly (`NeonSyndicate.Runtime`), one editor assembly and
one EditMode test assembly. This was left on purpose:

- One assembly keeps the compile checks simple. This project is built by AI and checked without Unity in twelve
  configurations; one runtime assembly means one compile per configuration and no web of references to keep in order.
- Systems call each other directly (`Game.Creatures`, `Game.Factions`, `SkillMods`), so a feature that crosses systems
  (a creature kill that moves reputation, pays Lumen and counts for a quest) stays simple to write and to review.
- Splitting pays off mostly in compile time once the code is much bigger, and it is easiest once the systems stop
  changing. The folders already follow the blueprint's areas, so the split is a later refactor: one asmdef per folder,
  then fix the references that cross.

The blueprint also caps files at about 400 lines. The project's rule is to keep files focused and split big classes
into partial files; some older files are still over 1,000 lines. A PlayMode test assembly and the blueprint's content
folders do not exist yet, except `Resources/Quests` and `Resources/Events` for quest and event data.

## 4. Content targets (blueprint section 2)

| 1.0 target | Today | Status |
|---|---|---|
| 40 story missions in 4 acts | 12 story missions with the Mercy or Power ending | Partly done |
| 60 side quests | 19 guild quests in 4 arcs | Partly done |
| 18 repeatable activity types | The older activities plus contracts, world events and boss arenas (see Activities below) | Partly done |
| 250 activity instances | Contract boards post jobs from 19 templates (three per board, new ones every in-game day), 46 world event templates, plus races, rampages and payphone contracts | Partly done |
| 12 districts and zones | 9 city districts, the Quartz Badlands, the Frostline and the Inner World | Done (Phase 3 added the last two) |
| 40 enterable interiors | 12 inhabited city venues plus 28 generated interiors (noodle bars, tea houses, gyms, boutiques, pawnshops, bookshops, chapels, arcades; `Docs/INTERIORS.md`) | Done (not yet run in Unity) |
| 24 creature species (16 regular, 6 elite, 2 apex) | 19: 15 regular plus 4 bosses; any species can spawn as an elite | Partly done |
| 12 bosses | 4 creature bosses with three phases, plus the Shard Warden who ends the story | Partly done |
| 14 weapon archetypes | 20 weapons plus fists in the game's own styles; the blueprint's crystal archetypes (a rune bow, a summoning totem, a drone swarm and the rest) do not exist | Partly done |
| 120 modular weapon parts | 28 procedural part variants (7 slots by 4) across 6 makers and 8 gun types | Partly done (Phase 4) |
| 25 named weapons | 3 mythic weapons (Volt Katana, Crystal Maul, Arc Gauntlets) | Partly done |
| 51 vehicles (30 cars, 8 flyers, 4 bikes, 3 boats, 6 mounts) | 30 cars and 4 hover bikes | Partly done |
| 300 collectibles across 6 types | 90 across 4 types (50 data chips, 20 crystal shards, 10 spell stones, 10 Echo Relics), plus stunt jumps | Partly done |
| 22 to 28 hours of main story, 70 to 90 hours to finish everything | Not measured: nobody has played through in Unity yet | Cannot tell yet |

### Activities (blueprint section 2.3)

- **Done (earlier):** Resonance Shrines (rest, level up, fast travel), payphone contracts, street races on the ground,
  rampages, store robberies, taxi, vigilante and paramedic jobs (still under those names, not rethemed), 10
  properties, Jax Calder Customs, gun shops, stunt jumps, safehouses.
- **Done (Phase 3):** creature hunts (Hunt contracts, and beast sightings with a bounty), Rift Incursions (rift
  contracts and the Rift Incursion event in the city), crafting at the Forge, and random world events (46 templates;
  the blueprint asks for 40).
- **Partly done:** the phone (contacts, side jobs and a camera; no map, bank, companion chat, creature list or radio
  apps), photography (the phone camera, and Third Eye scans fill the bestiary; no photo mode), fencing (Dante Rook buys
  materials; there are no stolen goods to fence), Echo Relics (ten hidden relics, each holding Epic or better gear; no
  digging or puzzle activity), the safehouse wardrobe (its button only puts the default clothes back; outfits come from
  clothing stores), the casino (slots only), properties (no guild halls), car customization (see Vehicles above).
- **Not started:** air races, card games and creature-fight betting, augmentation clinics, clubs and bars with
  minigames, a gym, and the rest of the stretch list (taming and a stable, fishing, arena tournaments, guild wars over
  territory, apartment decoration, racing league seasons).

## 5. Story and world (blueprint section 3)

| Blueprint item | Status | What exists, what is missing |
|---|---|---|
| Premise: Las Veyra, the Crystal Manor, the Third Eye, Hollows leaking out of the Inner World | Done (earlier and Phase 3) | The story and its ending came first; Phase 3 added the Inner World you can enter and the creatures. |
| The cast | Done (earlier) | Malik, Luma, Mara and Ion Solane, Jax Calder, Nico Ash, Dante Rook, June Pyre, Iris Moon, Officer Valez, Sable Knox and the Shard Warden all appear in the 12 story missions. Not started: Luma as a second playable character, June Pyre's loyalty quest. |
| Factions with reputation from -100 to +100 | Partly done | Done (Phase 3), under the game's own names: 14 factions (the 8 gangs as syndicates, the Frequency Order, Menders, Forgewrights and Frost Wardens guilds, Solane Holdings, the Hollow Cult) with ranks, shop discounts, gang behavior and guild perks. Missing: safehouses unlocked by reputation, and quest arcs for the syndicates, Solane Holdings and the Hollow Cult (only the four guilds have arcs, of 4 or 5 quests each). |
| Creature mythology in four tiers | Done (Phase 3) | All 17 species the blueprint names, in its four tiers, plus the Shard Golem Foreman and the Mirror Wraith Queen as bosses. |
| Ecology rules | Partly done | Each species has habitats, a time of day, weather it likes, a home territory, a temperament and a loot table. Badly hurt prey-like species flee; apex beasts patrol. Missing: diet, and creatures hunting each other. |
| Taming: weaken, attune with the Third Eye, then a minigame | Not started | |
| 40 missions in 4 acts | Partly done | The 12-mission story stands. Three of the blueprint's boss beats exist as guild quest finales (Shard Golem Foreman, Mirror Wraith Queen, Glacier Titan), and the Mirage Djinn is an open-world boss at night. The 40-mission structure is not written. |
| The mission variety mix | Not started | A writing guide for when the 40 missions are written. |

## 6. Graphics (blueprint section 5)

| Blueprint item | Status | What exists, what is missing |
|---|---|---|
| URP with Forward+, depth and opaque textures, HDR with a 64-bit colour buffer | Partly done | The URP beta sets all of this up. Untested in Unity. |
| Porting the post effects and the shaders | Partly done | On the URP beta: bloom, ACES tonemapping, colour adjustments, ambient occlusion (SSAO), motion blur, SMAA or TAA, plus eye adaptation and the exact vignette as a Render Graph feature; the character shader rewritten for URP; the code-made materials mapped to URP shaders. Untested in Unity. |
| GPU Resident Drawer and GPU occlusion culling | Not started | |
| Real-time global illumination (Surface Cache, Unity 6.7), Adaptive Probe Volumes on lower presets | Not started | How it would plug in: `Docs/URP_MIGRATION.md`. |
| Screen space reflections, ground truth ambient occlusion, AgX tonemapping (Unity 6.7) | Not started | SSAO and ACES today; a live reflection probe covers reflections. |
| AI upscaling on supported graphics cards (Unity 6.7), TAA on the rest | Partly done | TAA is the Ultra anti-aliasing on the URP beta. No upscaler. |
| Volumetric fog | Not started | |
| Post choices: a grade per district, chromatic aberration only in the Inner World, film grain off by default, per-object motion blur, depth of field only in cinematics | Partly done | The grade follows time of day, the Inner World, the Frostline and rifts, not districts. Chromatic aberration is on at High and Ultra and stronger in the Veil. Film grain is on by default. Motion blur is camera only. No depth of field. |
| Material standards: texel density, trim sheets, skin, eye and hair shaders | Not started | Rules for purchased art. Today every material is made in code (the character shader has fabric detail normals, rim light and wetness). |
| Quality presets per graphics card tier | Partly done | Low, Medium, High and Ultra on both pipelines. No Cinematic preset, no upscaler or GI settings, nothing benchmarked. |
| Performance budgets for a 16.6 ms frame | Partly done | The perf flythrough records frame times and memory allocations, but no budget is checked automatically and nothing has been measured on a real PC yet. |

## 7. Art, audio and tools to buy (blueprint section 6)

| What the blueprint plans to buy or use | Status |
|---|---|
| A purchased character-creation tool with its free Unity importer, and an optional free source of hero faces | Not started |
| A creature animation and riding pack, and a mountable dragon model | Not started |
| Purchased creature models, and AI 3D generation services for variants | Not started |
| A photoscanned material library and environment kits from an asset marketplace | Not started |
| Motion capture clips and a motion matching package | Not started |
| Audio middleware, a sound effects library, licensed or commissioned music | Not started |
| A VFX pack for crystal and magic effects, and a third-party volumetric fog asset | Not started |
| A third-party frame generation plugin (optional) | Not started |
| A third-party networking service (one of the co-op options) | Not started |
| Licensing habits: purchased art kept out of public version control, records of how AI assets were made | Not started: nothing has been bought yet. Add the third-party art folder to `.gitignore` with the first purchase. |
| Clearing and filing the game, city and character names | Outside the code: for Tyler and his attorney, not tracked here. |

## Next

1. **Tyler tests Phase 4 in Unity** with the Phase 4 checklist in `Docs/STATUS_AND_TESTING.md` (the opening and
   bike, the walk, guns in hand, loot, travel powers) and sends back screenshots and any red Console errors.
2. **The Fable pass on looks**: character visuals (faces, hair, finger bones so hands wrap the grip, skin and cloth
   materials) and city graphics (window interiors, wet streets, fog and light shafts, sky, sharper image), each judged
   with before and after previews and the golden screenshot loop.
3. **An independent code review of Phase 4** and fixes for what it finds (Phase 3's review found real bugs).
   Phase 5's loot world (chests, satchels, keys, the Gunsmith arc) goes through the same test and review; its
   checklist is at the end of `Docs/LOOTWORLD.md`.
4. **Keep a safe point**: if the project is not in Git yet, commit it with Git LFS and tag the tested state
   (`Docs/TOOLING.md`, section 6).
5. **Try the URP beta** with its checklist in `Docs/URP_MIGRATION.md`; if it holds up, make URP the default
   (blueprint Phase 1).
6. **Story missions as data** and more quest arcs, contracts and events (blueprint Phase 6).
7. **Taming and the two flying mounts**, then the hover physics rewrite and flying vehicles (blueprint Phase 5 and the
   vehicle plan).
8. **Later**: Unity 6.7 and its URP features, splitting the assembly once the systems settle, the multiplayer rules,
   then co-op.
