L1 status, 2026-10-25: Tyler reports V0 passed ordinary Unity play, with further visual improvement requested. The L1 solid Mirrorwell pavilion and Wild placement lane are source implemented in the existing owners. Native L1 appearance/physics acceptance remains pending; read `Docs/LIVE_WILD_L1.md` before treating the historical planned lanes below as current.

# Architect master prompt: the game bible, the blueprint and the build orders

Written 2026-10-24 for Tyler. This one file is everything an architect AI chat needs to plan the rest of Neon Syndicate:
Las Veyra with you, sprint by sprint, and to write the exact prompts you paste into the building chat (the AI coding
session that has the repository and pushes the code). Keep it in the repository so the architect and the builder read
the same facts.

## 0. How to use this file (for Tyler)

1. In the architect chat, make a project (or a long conversation) and paste **section 1** as its instructions.
2. Upload this whole file, plus `CLAUDE.md`, `Docs/ROADMAP.md`, `Docs/WILDS.md` and `Docs/LIVE_WORLD_ART.md`. These five
   files are the architect's memory. Re-upload them whenever the builder changes them.
3. Say: **"Start Sprint L1."** The architect answers with a sprint card and one builder prompt.
4. Paste the builder prompt into a new building chat on this repository. It builds, tests offline, commits and pushes.
5. Open Unity, pull the branch, press Play and do the sprint's checks. Send the architect what it asks for: the Console
   lines it names, screenshots, the F2 page text, and how it felt.
6. The architect decides: fix round (a small follow-up prompt) or next sprint. Repeat.

The loop in one line: **architect plans, builder builds, Tyler plays, architect judges.** Never skip the Unity step: the
building chat cannot run Unity, so nothing counts as done until you have seen it.

---

## 1. The master prompt (paste this as the architect's instructions)

```
You are the lead architect and producer of Neon Syndicate: Las Veyra, a single-player open-world action game in
Unity 6000.3.16f1. The owner, Tyler, is not a programmer. He directs AI. Your job is to plan the work and write the
prompts he pastes into a separate AI building chat that has the Git repository and writes, tests, commits and pushes
the code. You do not write the game code yourself; you write build orders precise enough that the builder cannot
misread them.

Your knowledge: the uploaded file Docs/ARCHITECT_MASTER_PROMPT.md is the game bible, the blueprint status and the
engineering rules. CLAUDE.md is the builder's rulebook (it wins on any engineering conflict). Docs/ROADMAP.md is the
status of every blueprint item. Read them before answering. When you are unsure whether something exists, tell the
builder to check the code first and report, instead of guessing.

How you work:
1. Work in sprints. One sprint = one builder session = one clear, visible improvement Tyler can see in Unity within
   15 minutes of pressing Play. Never bundle two big features in one sprint. Prefer five small sprints that each show
   something over one big one that shows nothing until the end.
2. Every answer that starts a sprint has exactly these parts:
   a. SPRINT CARD: name, goal in one sentence, why it matters to the player, what Tyler will see, risks.
   b. MASTER BUILD ORDER: the numbered steps the builder follows, in order, each with the files or systems it touches.
   c. BUILDER PROMPT: one fenced block Tyler pastes as is, written with the template in section 9 of the bible.
   d. TYLER'S PREP: anything Tyler must do before or during the sprint (download assets, run Blender, take golden
      shots), as numbered clicks in plain words.
   e. UNITY CHECKS: numbered steps for Tyler after the push, each with what he should see and what to send back.
3. Put the player first. Every sprint must make the game look, sound or play visibly better in ordinary play (Free Roam
   and the story), not only in a debug menu. A mechanic that exists but is not noticeable is unfinished. Tie each
   system to something on screen: a marker, a banner, a model, a sound, a map icon.
4. Art comes from real assets now. Code-built shapes are the fallback, not the goal. Use the drop-in pipelines in
   section 5 of the bible. Only free assets with a license that allows commercial use and changes (CC0 first, CC-BY with
   a credit file second). Never anything ripped from another game, never "non-commercial", "editorial only" or unclear
   licenses. Tell Tyler exactly what to download, what to search for, and how to prepare it.
5. Respect the house rules in section 4 of the bible in every prompt, especially: no em or en dash characters anywhere,
   no names of other commercial games, studios, their characters or places, or commercial asset products in any
   project file (all names are invented), save data append-only, every new player input on keyboard and gamepad,
   twelve compile configurations, every new static reset, content as JSON data, pure logic tested offline.
6. Be honest. The builder cannot run Unity, so its "done" means "compiles and passes offline tests". Only Tyler's
   Unity run proves it works. Track each sprint as Built (pushed), Seen (Tyler checked it in Unity) or Fixed (after a
   fix round). Keep a running sprint ledger at the top of your answers when Tyler asks "where are we".
7. Talk to Tyler in plain words. Short sentences, no jargon without a one-line explanation, exact menu paths and keys.
8. When Tyler reports a problem, ask for the specific evidence (the Console line with its prefix, the F2 page, a
   screenshot) and write a small fix-round prompt. Do not restart a sprint from scratch.
9. Keep scope honest: a full-fidelity open world is built from many small passes. Say when something needs purchased
   art or an engine upgrade and offer the free path first.

Start every new conversation by reading the bible's section 3 (where we left off) and asking Tyler what he saw in
Unity since the last push, unless he gives an order directly.
```

---

## 2. The game bible

### 2.1 Pitch

An open-world action game in Las Veyra, a neon desert city of enhanced humans, hover traffic and crystal power. You
play Malik, who carries the Third Eye: a sense that sees through the Veil (the Inner World, a red-violet mirror of the
city) where Hollows and crystal creatures leak out. Fight gangs, hunters, police and creatures with blades, fists, guns,
crystal foci and powers; run, glide, leap and fly across the city and the wild lands around it; loot guns, forge gear,
bond with companions and choose an ending (Mercy or Power).

**Tone:** gothic crystal grandeur meets neon street grit. Cathedral stone and amethyst against hover-car underglow.
Warm, slightly mythic, never grimdark for its own sake. Children are never harmed.

**Look canon (the palette):** Gothic Heart stone `#665b4e` with trims `#c9b98d`; Amethyst charcoal `#31373f` with
violet `#9d67c1`; Azure turquoise `#46c4c8` with moss `#135058`; neon accents cyan, magenta and gold at night. Crystals
glow softly; stone is weathered; night is the showcase.

### 2.2 The map

The map is a square about 3.2 km across (`CityLayout.MapHalf` = 1600 m) with a mountain ring at the edge. The city
sits inside the Beltway (a ring road at 1000 m from the centre). Everything is generated from the seed 61724 at boot.

| Area | What it is |
|---|---|
| City districts (9) | Las Veyra Core, Glass Heights, Mirage Strip, Old Silver, Brass Projects, Ash Underpass, Sunken Row, Grave Frequency, Skyrail Orbit |
| Current world | Gothic Civic H5 (world revision 10): a gothic court at the centre with no streets, outer avenues, the Beltway, a ring road and four approach roads. Every title-screen button plays it. Mara's clinic and Jax's garage are in the court. |
| Quartz Badlands | The desert outside the Beltway (story sites, desert lots, mesas, stunt jumps, the airstrip) |
| The Frostline | Snow zone beyond the north-west Beltway: Rimeglass Lake, the Frost Wardens Outpost, Titan Hollow |
| The wilds (2026-10-24) | Five lands filling the desert band with rolling ground, landforms, scatter and 17 named places to discover (below) |
| The Veil (Inner World) | The red-violet mirror city, entered by holding the Third Eye 0.7 s; drains crystal energy |
| Underheart | An underworld under the Pressure House venue, six variants |
| Interiors | 12 inhabited venues (Forge, Lofts, Archive, Clinic, Inn, Office and others) plus 28 generated rooms behind lit street doors |

**The five wild lands and their places**

| Land | Where | Places |
|---|---|---|
| Redspire Badlands | west | Hollowwind Maze, Sunken Temple of Vey, Circle of Nine Suns, Redwatch Tower |
| Whispering Dune Sea | south | Ribs of the Old Titan, Wreck of the Dawnstrider, Mirrorwell Oasis, Caravan Rest |
| Glasslight Barrens | east | The Singing Grove, Breathing Flats, Shardwatch Obelisks |
| Saltbrush Steppe | north and the band by the Beltway | Emberfall Village, Old Wind Relay, Saltroad Camp |
| Rimewood | the Frostline snow | Frostmaw Cave, The Icebound Barge, Steamveil Springs |

Each place shows as a grey "?" until found, then pays $300 and 120 Lumen; all 17 pay $6,000 and a skill point.

### 2.3 Cast (all invented)

Malik (the player), Luma, Mara Solane (the clinic) and Ion Solane, Jax Calder (garage, Jax Calder Customs), Nico Ash
(racer), Dante Rook (the Forge in Old Silver, buys materials), June Pyre, Iris Moon, Officer Valez, Sable Knox, the Shard
Warden (ends the story). Companions: Glint the Quartz Drakeling (starter bond), more through Bonds. The Collector (loot
phone contact). Guild givers per guild.

### 2.4 Factions (14, reputation -100 to 100)

Eight gangs as syndicates, four guilds (the Frequency Order, the Menders, the Forgewrights, the Frost Wardens),
Solane Holdings (a company) and the Hollow Cult. Reputation changes prices, gang behaviour and guild perks. Gang
territories and strongholds sit in the city and the desert.

### 2.5 Creatures and bosses

19 species built and animated in code, in four tiers: Glass Mite swarms, Neon Jackal packs, golems, Hollows, Mirror
Wraiths, the Crystal Basilisk, wyverns, griffins, the burrowing Dune Wyrm, the Quartz Drake and more. Bosses: Glacier
Titan, Shard Golem Foreman, Mirage Djinn, Mirror Wraith Queen (three phases each); the Shard Warden ends the story. They
spawn by habitat, time of day and weather, warn before attacks, drop crafting materials and have breakable parts.

### 2.6 Player kit

- Melee (stamina, dodge, guard, parry, combos), 20 fixed weapons, procedural loot guns (8 types, 6 invented makers,
  Common to Mythic, elements Volt, Ember, Frost, Void), six crystal foci (Shardwake, Prism Thread, Gyre Crescent,
  Sunderheart, Mirage Choir, Mirrorwake).
- Crystal powers, the Third Eye (tap: bullet time; hold: shift into the Veil), the Resonance loop (marks and bursts),
  gadgets, armour plates on enemies, cover.
- Travel powers: Surge (super speed), Super Leap, Glide, Flight. Hover cars (30) and hover bikes (4).
- Progression: levels and Lumen, Body, Mind and Shard skill trees (90 nodes), the Forge (crafting from 30 materials),
  gear sets and gear score, builds.

### 2.7 Story

12 story missions to the Mercy or Power ending, 19 guild quests in four arcs, contract boards, 46 world events, the
Underheart campaign (04E), the Gunsmith loot arc. Quests and events are JSON in `Resources/Quests` and
`Resources/Events`.

### 2.8 Names rule for new content

Every new place, creature, gun, gang or song gets an invented name that fits the tone (stone, glass, frost, neon,
resonance words). Never borrow a name from another game, film or brand.

---

## 3. Where we left off (2026-10-24)

**Branch:** `feature/04oh-ascension-art-foundation` (draft pull request 12) on tycs1992-RBT/NeonSyndicate_LasVeyra.
The builder always pushes to the branch it is told to and never force pushes.

**Latest commits on that branch, newest first**

| Commit | What it did |
|---|---|
| b47f7b3 | Rugged mountain ring: faceted ridges, shoulders and snow caps instead of smooth half spheres |
| 8fd800a | Mechanics made visible: Glint the Quartz Drakeling bonds at once in Free Roam and after the prologue; swords held through the fist (the "weird sword guy" fix) |
| 89644e8 | The wilds: five lands, rolling ground, landforms, scatter and 17 places to discover |
| dd05470 | Children, elders and a syndicate heavy from Tyler's own MPFB blends in the crowd (families walk together, children never harmed) |

**Not yet seen in Unity:** all four commits above, and much of Phases 3 to 6 and 04D to 04E (see `Docs/ROADMAP.md`,
`Docs/STATUS_AND_TESTING.md`). Tyler's first Unity session after these commits should be a check of the wilds
(`Docs/WILDS.md` section 4), Glint following him, and a sword fight.

**Owner's stated priorities, in his words:** "fill the map ... with better areas to explore"; "all the brains of the
game to be visible in the real world"; "add more lands and structures since they suck through just code and through
free downloads". In short: **real models and textures for the land and buildings, and every mechanic visible in play.**

**Known open items**

- Taming and two flying mounts (Rime Griffin, Quartz Drake) were agreed ("option 2") and not started.
- The syndicate heavy's skin creator credit is blank in `Art/LiveCast/SyndicateHeavy/CREDITS.txt`; Tyler fills it in.
- Earlier art deliveries live in `Assets/NeonSyndicate/Art/` (`CandidatesH3`, `CandidatesH5`, `ProductionH6` to
  `ProductionH9`, `WorldFoundationH8R`, `CrystalGothicProof`, `Imported`). A builder must audit what is already there
  and wired before adding new art, so nothing is imported twice.
- Some mission text still names the original districts; markers are right.

---

## 4. Engineering directions (the builder's rules, short form)

`CLAUDE.md` is the full rulebook and wins on conflict. Every builder prompt must repeat the parts that apply.

**House rules**
- No em dash or en dash characters anywhere (code, comments, strings, docs, commit messages).
- No names of other commercial games, studios, their characters, places, brands, songs or commercial asset products in
  project files. A license-required credit (CC-BY) names the creator exactly as the license asks, in `CREDITS.txt`.
- Never break the twelve compile configurations (editor and Windows player; classic Input Manager, both, Input System
  only; Built-in and URP). Input System code inside `#if ENABLE_INPUT_SYSTEM && NS_INPUT_SYSTEM`, URP code inside
  `#if NS_URP`.
- Every new static field gets a `ResetStatics()` called from `Game.ResetStatics()`.
- Every new player input works on keyboard and mouse and gamepad, through `Act`, and goes in `Docs/CONTROLS.md`.
- `SaveData` is append-only: add fields with initializers, add them to the `SaveDataTests` freeze list, never rename or
  remove. A system with per-playthrough state implements `IWorldSystem` and registers itself.
- Content (quests, contracts, events) is JSON data that passes `QuestValidator`; shipped ids never change.
- Keep files focused (partial classes), XML doc summaries in plain English, explicit types (no `var`), C# 9, Unity 6
  APIs, `Log.Info/Warn/Error`.
- Pure logic lives in engine-free classes with EditMode tests. Boot steps go through `Step(...)` so one failure cannot
  stop the game; callers null-check `Game.X`.

**How the game is put together**
- `GameRoot` boots every system in order after the scene loads; `Game` is the registry of live systems.
- The city comes from `CityLayout` (pure planner) and `CityBuilder` / `CityDressing` (meshes). The wilds come from
  `WildsLayout` (pure plan), `WildsSites`, `WildsShapes`, `WildsBuilder` (160 m chunks), `WildsSystem`.
- UI is IMGUI through `UIKit`; pause tabs through `MenuPages`, F2 debug tabs through `DebugPages`, HUD layers through
  `HudOverlays`.
- Built-in render pipeline is the default; the URP beta is opt-in. Colour grade lives in `Core/PostGrade.cs` only.

**Working rules for every sprint**
- The builder cannot run Unity. It must compile in the offline harness, run the EditMode tests it touched, render what
  it can offline, and say plainly what only Tyler can check.
- Graphics changes follow the golden screenshot loop: Tyler captures Neon Syndicate > QA > Capture Golden Shots before
  and after, then Compare Golden Shots. Performance changes run the Perf flythrough before and after.
- Never commit `Library`, `Temp`, `Logs`, generated `Prepared` folders or `Assets/_Recovery`.
- Binary art (FBX, PNG, blend) is committed as plain files with Git LFS filters bypassed; the repository's LFS workflow
  converts it afterwards. Pull with `--no-rebase` if the workflow committed in between. Never force push.
- Every feature gets an F2 debug tab or line, a Console prefix (for example `LIVE_WORLD`, `UNDERHEART_04E`), and a doc
  page under `Docs/` with "What to check in Unity" and "Current limits, honestly" sections.

---

## 5. The art pipelines (how real assets get into the game)

The game has no art in the scene: it builds the world in code, and drop-in folders let real models replace or join the
code-built pieces. Three lanes exist today; Sprint L1 and L2 add the lanes the lands and structures need.

### 5.1 Live cast, for people (exists)

- Folder per character in `Assets/NeonSyndicate/Art/LiveCast/<Name>`: one FBX on the MalikRig 53-bone skeleton,
  textures `<Material>_Albedo.png` and `_Normal.png`, `livecast.json` (identity, roles, age adult, child or elder,
  hair) and `CREDITS.txt`. `Malik` is the player; `crowd:...` identities join the crowd.
- `tools/convert_mpfb_crowd_dropin.py` with a recipe in `tools/mpfb_recipes/` turns a Blender MPFB human into a drop-in
  folder, garments and painted textures included. Guide: `Docs/LIVE_CROWD.md` section 9, `Docs/LIVE_CHARACTERS.md`.

### 5.2 Live world art, for props and set pieces near named places (exists)

- Folder per piece in `Assets/NeonSyndicate/Art/LiveWorld/<Piece>`: an FBX, textures
  `<Material>_BaseColor/_Normal/_MetallicSmoothness/_Emission.png`, and `placement.txt` (`near PLACE offset x z turn
  deg scale s`, `room forge`, `replace forge-anvil`, `route`, `skip`).
- Places it understands today: `Realm:` anchors, `Venue:`, `Under:` and `Cast:` tokens, site keys and shop or person
  names. It does **not** yet understand the wild places.
- Pieces are visual only (no collision) and appear only in the Gothic Civic H5 world. A safety pass keeps them off
  roads, walks, doors, prompts and footprints. Console prefix `LIVE_WORLD`, F2 > Live world.
- Material names without textures map to palette textures: stone, masonry, rock, mineral; trim, edge, weathered,
  carved; bark, wood, root; leaf, moss, fern; crystal, amethyst, azure, gem; iron, metal, bronze.
- Guide: `Docs/LIVE_WORLD_ART.md`.

### 5.3 Earlier art deliveries (exist, audit first)

`Art/CandidatesH3`, `CandidatesH5`, `ProductionH6` to `ProductionH9`, `WorldFoundationH8R`, `CrystalGothicProof`,
`Imported` came from earlier sprints (with their own docs under `Docs/Ascension04OH` and the `BUILD_*` pages). Before any
new import the builder lists what each holds, what is wired into the game and what is unused.

### 5.4 Blender export rules (every model)

- Metres, Z up, the model's front facing Blender's Front view, origin at the base centre, all transforms applied.
- FBX: Mesh only, Scale 1, Apply Scalings "FBX All", Forward -Z, Up Y, Apply Unit and Apply Transform on, Apply
  Modifiers on, no animation bake.
- Budgets (open-world pieces seen many times): rocks and plants 300 to 3,000 triangles; a landmark 10,000 to 40,000;
  textures 1024 or 2048 square; LOD meshes named `_LOD0`, `_LOD1`, `_LOD2` when the piece is big.
- Material names follow the palette words above when the piece has no textures of its own.

### 5.5 Where free assets come from (licensing)

- **Preferred:** CC0 (public domain) libraries: photo-scanned rocks, cliffs, trees and ground textures; CC0 low-poly
  nature, ruin and building kits by independent artists; CC0 PBR texture libraries (ground, sand, rock, snow, stone,
  plaster, metal). These need no credit, but record the source in `CREDITS.txt` anyway.
- **Allowed with a credit:** CC-BY. `CREDITS.txt` names the creator, the title, the source page and the license exactly
  as the license asks.
- **Never:** non-commercial (NC), no-derivatives (ND), "editorial use", unclear or missing licenses, anything ripped or
  converted from another game, anything showing a real brand or logo.
- File names, material names and in-game names are invented and neutral (`RedspireArch_A`, not the store's product
  name). A store or kit is described generically in docs ("a CC0 low-poly ruins kit").

### 5.6 Planned lanes for lands and structures (built in Sprints L1 to L3)

| Lane | Folder | What it does |
|---|---|---|
| Wild landmarks | `Art/LiveWorld/<Piece>` with `near Wild:<place>` | Places a modelled structure at one of the 17 wild places, with optional collision |
| Wild kit swap | `Art/LiveWilds/<PieceKind>/` | Replaces a code-built piece kind (Boulder, Palm, RuinWall ...) everywhere with imported variants, keeping the plan, placement and collision |
| Ground surfaces | `Art/LiveGround/<Land>/` | Real tiling ground textures per land, blended over the rises |

---

## 6. Sprint plan, starting with lands and structures

Each sprint is one builder session. The architect writes the full prompt from the template in section 9; the notes
below are the starting brief.

### Sprint L1: wild places accept real landmarks (with collision)
- **Goal:** Tyler can drop a downloaded structure (a ruin, a tower, a wreck) into a folder and see it standing at any
  of the 17 wild places, solid to walk on and drive into.
- **Builder brief:** extend live world art with `Wild:<place>` place tokens (by place name such as `Wild:Mirrorwell Oasis`, or by the saved site id,
  resolved through `Game.Wilds` and `WildsLayout` site data; F2 > Wilds rows show each id); a
  `collide box|mesh|none` placement line (simple colliders on the World layer, never on the shield layer 15); a
  `clear 12` line that hides code-built wild pieces within 12 m of the landmark so it does not clip rocks; safety rules
  that still refuse roads and story sites. Pure resolver and clearing logic get EditMode tests. F2 > Live world lists
  wild placements. Ship one example folder with a CC0 piece or a builder-made Blender placeholder, with `skip` off.
- **Tyler sees:** a real model at Mirrorwell Oasis; he can stand on it.

### Sprint L2: wild kit swap (real rocks, trees and ruins everywhere)
- **Goal:** the 18,000 code-built wild pieces can become real models one kind at a time.
- **Builder brief:** `Art/LiveWilds/<PieceKind>/` with one or more variant FBX files and textures, an optional
  `kit.txt` (scale range, sink depth, collider type, which lands). `WildsBuilder` keeps the plan and collision from
  `WildsLayout`, but draws the imported variant (picked by the piece seed) instead of the code shape, with GPU instancing
  and LOD culling. Missing or broken kits fall back to the code shape and say why under a `LIVE_WILDS` Console prefix.
  F2 > Wilds shows which kinds are swapped. Editor setup prepares import settings like live world art does.
- **Tyler's prep:** download a CC0 rock pack and a CC0 desert or nature pack; the architect lists which piece kinds to
  map (Boulder, RockSpire, Butte, DeadTree, Juniper, Palm, Shrub, RuinWall, RuinColumn, Tent, Crystal first).
- **Tyler sees:** the Redspire Badlands full of scanned red rock instead of faceted shapes.

### Sprint L3: real ground
- **Goal:** the desert floor looks like sand, cracked earth, salt and snow, not flat colour.
- **Builder brief:** `Art/LiveGround/<Land>/` tiling PBR textures; a ground material per land blended at land edges and
  on rises; detail grass or pebble cards near the camera on the higher presets; both render pipelines. Golden shots
  before and after.

### Sprint L4: landmark pass for all 17 places
- Each place gets one hero structure or a set (through L1) plus its kit pieces (through L2). Tyler and the architect
  pick the assets place by place; the builder wires and tunes placement, clearing and collision. Two to four places per
  session.

### Sprint L5: city structures
- **Goal:** the city's buildings stop looking like boxes.
- **Builder brief, first session:** audit `CityBuilder` and `CityDressing` and the earlier art folders, then propose a
  facade kit lane (`Art/LiveCity/<Style>/`: wall, window, door, corner, roof and trim modules on a fixed grid) that
  dresses the existing building footprints by district style, with the code-built building kept as collision and
  fallback. Build it in the next session.

### Sprint L6: new lands to explore
- Options to choose from with Tyler: a canyon land carved into the Redspire Badlands (real cliffs from L2), a crystal
  forest in the Glasslight Barrens, a sunken city district under the canals, or larger Veil regions. The map size stays
  3.2 km unless the architect plans a dedicated sprint to grow it (it touches the city layout, the map, the mountains,
  streaming and every test).

### Then: making the lands alive
- **Taming and two flying mounts** (Rime Griffin, Quartz Drake): weaken, attune with the Third Eye, a short minigame,
  a stable at a safehouse, mounts that ride like the hover bike and fly like the Flight power.
- **Creature dens and camps at the wild places**, bounty crews and chests placed by place.
- **Quests at the wild places** (JSON in `Resources/Quests`, places resolved by the `Wild:` tokens from L1).
- **Visibility pass**: every system gets a world-space sign (map icons, markers, banners, ambient NPC reactions).

---

## 7. The definition of done (every sprint)

1. Compiles in the offline harness for the configurations the change touches; no new warnings.
2. EditMode tests added for new pure logic; touched tests pass offline.
3. Offline renders or printed numbers for anything visual the builder could check.
4. Docs updated: the feature's page, `CLAUDE.md` if a new system or rule, `Docs/ROADMAP.md` status, `Docs/CONTROLS.md`
   for inputs, `CREDITS.txt` for every asset.
5. Committed and pushed to the named branch, with a plain-words summary listing what only Tyler can check.
6. Tyler checks in Unity and reports; the architect marks it Seen or writes a fix round.

---

## 8. What the architect asks Tyler for after each sprint

- The Console lines with the sprint's prefix (copy them from the Console, right click, Copy).
- The F2 page of the feature (screenshot).
- Two or three screenshots from the normal camera (not the debug camera), day and night.
- The Perf flythrough `.txt` summary when the sprint added a lot of geometry.
- Golden shot compare report for graphics changes.
- One sentence on how it felt.

---

## 9. Builder prompt template (the architect fills this in)

```
You are the building session for Neon Syndicate: Las Veyra (Unity 6000.3.16f1), repository
tycs1992-RBT/NeonSyndicate_LasVeyra. Work on branch <BRANCH>, push to the same branch, never force push.
Read CLAUDE.md first and follow it exactly. The owner, Tyler, is not a programmer: explain in plain words, and never
claim something works in Unity, because you cannot run Unity.

SPRINT <ID>: <NAME>
Goal: <one sentence the player would notice>
Why: <the player-facing reason>

Before you build:
1. Read: <docs and files to read>.
2. Check and report in one paragraph: <what to verify exists, so nothing is built twice>.

Build, in this order:
1. <step, with files and systems>
2. <step>
...

Rules that matter for this sprint:
- No em or en dash characters anywhere; no names of other commercial games, studios, characters, places, brands or
  commercial asset products in project files (credits required by a license are the exception).
- Twelve compile configurations; Input System code inside #if ENABLE_INPUT_SYSTEM && NS_INPUT_SYSTEM, URP inside #if NS_URP.
- New statics reset from Game.ResetStatics(); SaveData append-only with the freeze list; inputs on keyboard and gamepad
  in Act and Docs/CONTROLS.md; content as JSON; pure logic in engine-free classes with EditMode tests.
- Every imported asset gets CREDITS.txt with source, creator and license. Commit binaries as plain blobs with the
  LFS filters bypassed; never commit Library, Temp, Logs, Prepared folders or Assets/_Recovery.
- <anything sprint specific: budgets, layers, which world revision, fallbacks>

Done means:
- Compiles offline, new and touched tests pass, offline renders or numbers for anything visual.
- An F2 page or line and a Console prefix <PREFIX> for the feature.
- Docs: <page> with "What to check in Unity" and "Current limits, honestly"; CLAUDE.md and ROADMAP.md updated.
- Committed and pushed. Your final message: what changed in plain words, what you verified, and numbered Unity checks
  for Tyler with what he should see and what to send back.
```

---

## 10. The first builder prompt (Sprint L1, ready to paste)

```
You are the building session for Neon Syndicate: Las Veyra (Unity 6000.3.16f1), repository
tycs1992-RBT/NeonSyndicate_LasVeyra. Work on branch feature/04oh-ascension-art-foundation, push to the same branch,
never force push. Read CLAUDE.md first and follow it exactly. The owner, Tyler, is not a programmer: explain in plain
words, and never claim something works in Unity, because you cannot run Unity.

SPRINT L1: wild places accept real landmarks
Goal: a downloaded or Blender-made structure dropped in a folder stands at any of the 17 wild places, solid to walk on.
Why: the wild places are code-built low-poly shapes; real models make them worth exploring.

Before you build:
1. Read Docs/LIVE_WORLD_ART.md, Docs/WILDS.md, Docs/ARCHITECT_MASTER_PROMPT.md sections 4 and 5, and the code in
   World/LiveWorld (LiveWorldArtH8S and its partials, LiveWorldPlanH8S, LiveWorldSafetyH8S, LiveWorldPlannerH8S),
   Editor/LiveWorldArtSetupH8S.cs, and World/Wilds (WildsLayout, WildsSystem, WildsBuilder).
2. Report in one paragraph: how placement lines are parsed and resolved, when live world art runs relative to the
   "Wilds" boot step, and whether the safety pass already sees the wild pieces' colliders.

Build, in this order:
1. Place tokens Wild:<place name> (case insensitive, for example Wild:Mirrorwell Oasis) and Wild:<saved site id>,
   resolved from Game.Wilds' plan; add each site's id to its F2 > Wilds row so Tyler can read it. Put the parsing and matching in a pure class with tests.
   Make sure live world art runs after the wilds are built (or waits for them) without slowing the boot.
2. A placement line "collide box", "collide mesh" or "collide none" (default none, as today). Colliders go on the World
   layer so people, cars and the camera treat the piece as ground; never layer 15. Mesh colliders use the LOD0 mesh or
   a mesh named <Piece>_Collision when present.
3. A placement line "clear R" (metres, up to 40) that hides code-built wild pieces whose centres fall within R of the
   placed piece (scatter and landforms of that chunk), including their collision, so a landmark does not clip rocks.
   Do it without rebuilding the whole wilds (for example, by rebuilding only the touched chunks with those pieces
   left out). Pure selection logic gets a test.
4. The safety pass for Wild: places: still refuse roads, story sites, desert lots, prompts and walks; allow the spot to
   overlap the wild pieces that "clear" removes; keep the existing behaviour for every other place.
5. F2 > Live world lists each wild placement with its place, cleared piece count and collider type. Console lines keep
   the LIVE_WORLD prefix.
6. Ship one example folder, Art/LiveWorld/OasisPavilion, with a modest stone pavilion you build in Blender (CC0 by
   nature, under 8,000 triangles, palette material names stone, trim, crystal), placed "near Wild:Mirrorwell Oasis
   offset 0 18 collide mesh clear 10".
7. Update Docs/LIVE_WORLD_ART.md (new place tokens, collide and clear lines, a worked example for each of the five
   lands), Docs/WILDS.md, CLAUDE.md (one line under The wilds) and Docs/ROADMAP.md.

Rules that matter for this sprint:
- No em or en dash characters anywhere; no names of other commercial games, studios, characters, places, brands or
  commercial asset products in project files.
- Twelve compile configurations. New statics reset from Game.ResetStatics(). No SaveData change is expected; if one is
  needed, append only and add it to the freeze list.
- Live world art stays visual-only for every line without "collide", and stays limited to Gothic Civic H5.
- Commit binaries as plain blobs with the LFS filters bypassed; CREDITS.txt in the example folder. Never commit
  Library, Temp, Logs, Prepared folders or Assets/_Recovery.

Done means:
- Compiles offline, new and touched tests pass (including WildsLayoutTests), an offline render of the pavilion at the
  oasis from the plan's real positions.
- Committed and pushed. Final message in plain words, with numbered Unity checks for Tyler: the LIVE_WORLD lines to
  copy, F2 > Wilds > Go there > Mirrorwell Oasis, walk onto the pavilion, drive into it, check no rock pokes through,
  and how to drop his own model at another place.
```

---

## 11. Tyler's asset shopping list for the lands sprints (all free)

The architect expands this per sprint; this is the starting point. Search the CC0 libraries for these words.

| For | Search for | Notes |
|---|---|---|
| Redspire Badlands | "red sandstone cliff scan", "desert rock formation", "hoodoo", "rock arch" | 3 to 6 rock variants, 1 arch |
| Whispering Dune Sea | "desert ruin", "bleached bones", "palm tree low poly", "tent" | the oasis and caravan |
| Glasslight Barrens | "crystal cluster", "basalt column", "geyser" | crystals can use the palette's crystal material |
| Saltbrush Steppe | "juniper", "dry shrub", "adobe house", "wind turbine" | Emberfall Village and the relay |
| Rimewood | "ice rock", "snow boulder", "frozen dead tree", "wooden boat" | the barge and the cave |
| Ground | "sand", "cracked earth", "salt flat", "snow", "dry grass" PBR textures | 2K, tiling |
| Landmarks | "stone pavilion", "ruined temple", "watchtower", "crashed airship" | one hero piece per place |

Keep each download in its own folder with its license page saved as `LICENSE.txt` next to it, so the builder can write
the credit.
