# CRYSTAL MANOR SAGA / NEON SYNDICATE — LAS VEYRA
# WORKING MASTER BRAIN BIBLE + DEVELOPMENT BLUEPRINT, 04K → 04L → 04M → 04N

**Version:** 2026-10-19 source-audited planning brief. **Status:** creative/engineering continuity document, **not** a completed implementation or a substitute for the original archived art reference pack.

## Mission

Continue the existing original-IP open-world urban-fantasy RPG as a unified, genuinely playable game. This bible retains established canon and architecture, then sequences three separately reviewable major Work development sprints after the currently active 04K. The user wants significantly more depth in real graphics, creatures, storytelling, physical world interactions, weapon combat, companions, environments, missions, meaningful RPG progression, immersion and game quality. Avoid plans that generate classes/data with no actual player-facing effects.

## Verified current handoff

The repository is `https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra`, Unity 6000.3.16f1 / Built-in default / optional URP / Input 1.17.0. 04J branch `feature/shard-war-horde-renaissance-04j` was published at SHA `221cefe8fa2fb3e7f9c927f6a2eeb7399ab1eb75`; draft PR #5 targets 04I, and 04J remained **partial**, with 04J gates C–G not finished and native playtests unverified. **04K is actively being implemented:** `feature/renaissance-in-motion-04k` had advanced to `ebd1eee9b9758bc42fa1bbf2e9e8ab1401f89cc2` at the latest inspection, with at least three source checkpoints: 04K-B weighted character/creature anatomy and poses (`15e42467`), 04K-C finite crystal champion/caster combat (`19be52fa`), and 04K-D arrival district/venue geometry (`ebd1eee9`). This is **progress, not a finished or native-verified 04K handoff**; the Work chat may publish more commits after this document is generated. **Resolve the latest 04K HEAD before executing 04L; do not start 04L from an old SHA.** All 04L/M/N files in this package are directives, not changes applied to the GitHub repository.

## Canon story: preserve the original stakes

A single father, **Malik Veyr**, protects his young daughter **Luma Veyr**. The ancient Crystal Manor guarded the seam between Las Veyra and the red-violet mirror world called **the Veil**. Its Heart broke and its shards opened rifts; Luma hears the stones and the Manor calls her to be a keeper. Malik's **Third Eye** is activated through his bond with Luma. The **Shard Warden**, former Manor steward and now partially crystalline, wants to use Luma's signal and the Third Eye to restore the Heart as a throne. The finale is a single player's choice between **Mercy** (shelter, open door, restorative powers) and **Power** (throne, sealed rule, combat and tribute). Neither can be replaced, retconned or force-chosen; Free Roam continues after either ending.

## Canonical 12-mission storyline

| Saved mission ID | Chapter | Source-derived identity |
|---|---|---|
| `m00_broken_map` | The Broken Map | Third Eye awakening, Luma and Malik, Manor ruins, opening hunters, bike escape |
| `m01_heat_mirrors` | Heat Mirrors | Jax job, city vehicle theft, Flight earned after chapter |
| `m02_ghost_lanes` | Ghost Lanes | Nico race, Veil lights beneath roads |
| `m03_false_kings` | False Kings | Dante and a hunter posing as Brass Tower king |
| `m04_flooded_grid` | The Flooded Grid | Mara, canal tear and trapped couriers |
| `m05_ashline_treaty` | Ashline Treaty | June peace attempt, Ember Court betrayal |
| `m06_nocturne_market` | Nocturne Market | Iris, Luma signal drive, four beast clues |
| `m07_glass_saints` | Glass Saints | Officer Valez and Glass Jury lawyers |
| `m08_signal_war` | The Signal War | Ion, three Grave Frequency transmitters and Veil damage |
| `m09_crownless_mile` | Crownless Mile | Warden payroll truck, Sable betrayal |
| `m10_long_switch` | The Long Switch | Luma abduction and Shard Wolves chase |
| `m11_sunrise_debt` | Sunrise Debt | three Warden anchors, Heart recovered, Mercy or Power |

## People and guild contacts

Protagonist Malik; young noncombatant Luma; Third Eye (speaks as the Manor key); Mara Solane (Menders and clinic); Ion Solane (signal/tech); Jax Calder (mechanic); Nico Ash (racer); Dante Rook (Forgewrights); June Pyre (peace broker); Iris Moon (Frequency Order); Officer Valez (investigator); Sable Knox (fixer); Shard Warden (antagonist). Existing later additions Corin Pell and Tamsin Reyes remain real NPC/quest contacts. Captain Rhosyn Hale is documented as a Frost Warden giver. Never replace a role or create a false relationship from concept boards.

## City factions and role grammar

Eight source-defined syndicates: Ember Court (`ember_court`, Ash Underpass), Quartz Kings (`quartz_kings`, Quartz Badlands), Mirage Lotus (`mirage_lotus`, Mirage Strip), Brass Tower Union (`brass_union`, Brass Projects), Static Saints (`static_saints`, Grave Frequency), Canal Choir (`canal_choir`, Sunken Row), Glass Jury (`glass_jury`, Glass Heights), Roof Oni (`roof_oni`, Skyrail Orbit). Four source-defined principal guilds: Frequency Order (`frequency_order`), Menders (`menders`), Forgewrights (`forgewrights`), Frost Wardens (`frost_wardens`). Additional established corporate/cult factions include Solane Holdings (`solane_holdings`) and Hollow Cult (`hollow_cult`). Their source `FactionDb`, gang IDs, rivalry, reputation, story hostility floors and quest ownership remain stable. New district behavior must build upon those owners.

## Powers, creatures, bonds and major adversaries

The Third Eye allows bullet-time perception and crossing the Veil; Surge works early, Super Leap and Glide follow the first story mission and Flight follows the second. Other gained powers include Ember Chain, Static Lance, Quartz Ward, Mirage Step, Gravity Well, Canal Mend, Roc Call, Ghost Frequency, Glass Sight and Drive Sigil, with earned unlock conditions. Crystal melee has Asterblade, Riftfang Pair and Basalt Crown. Six saved focus signatures are Shardwake, Prism Thread, Gyre Crescent, Sunderheart, Mirage Choir and Mirrorwake; existing high-rarity 17–22 unique entitlements are **not** general drops. Focus charge, heat, stamina and travel systems have separate authorities.

Source-defined major creatures include Glass Mites, Neon Jackals, Shard Golems, Hollow Shades/Hollows, Mirror Wraiths, Crystal Basilisks, Violet Wyverns, Frost Stalkers, Rime Griffins, Snow Hollows, Sun Griffins, Sand Golems, Dune Wyrms and Quartz Drakes, plus 04E Underheart creatures. Four established major original-world optional bosses: Glacier Titan, Shard Golem Foreman, Mirage Djinn and Mirror Wraith Queen. The original Shard Warden is the story finale, not a roaming early enemy. Note that historic `Docs/CREATURES.md` says city creatures appear only at night, but 04I source has **restored daytime CityRift packs**; resolve discrepancies in favor of current code, then actual Unity observation.

**Saved bonded species:** Quartz Drakeling `quartz_guide` (Resonant Trail/Glass Lance), Ashshell Warden `ash_warden` (Brace and Sense/Ashshell Brace), Stormskimmer `storm_skimmer` (Relay Hop/Static Snare). **Saved fusions:** Ventwing `ventwing`, Kilnback `kilnback`, Coilrunner `coilrunner`. Frost/Ember/Shadow/Volt/Terra/Aether drakes on historical boards are speculative **design ideas**, not existing granted player species.

## Existing real game architecture

- `GameRoot`, `MissionManager`, `StoryMissions` own gameplay boot and original story; do not create second story manager.
- `QuestBook`, `CampaignDirector`, `WorldEvents` own guild/Underheart quest data, physical steps, contract grammar, event instances and once-only reward receipts. Underheart 04E adds 16 main quests, 12 side quests and eight contract grammars. 04I adds five connected Ascension chapters; 04G courier and 04H Quiet Circuit remain.
- `CreatureDirector`/`CreatureEcology`, original `Creature`/`NpcController`, `PedManager` and `WorldEvents` own ambient populations, eligibility, attack budgets and per-event cleanup. 04I/04J restored and instrumented CityRift creatures; real native visibility remains unproven.
- `BodyBuilder`, `HumanoidPose`, `CreatureBuilder`, `CompanionSculpt`, `CityBuilder`, `DistrictBuilding`, `VisualRebirth` own animation/mesh/world render consumption; 04K adds motion geometry and tests. Original concept boards are not game-ready assets.
- `BondSystem` and `Fusion` owners preserve earned companions and fused vehicles. `ForgeDb`/`CraftingSystem`, `MaterialStore`, `GearSystem`, `LootArmory`/`LootGenerator`, `BuildSystem`, `FactionSystem`, `TerritorySystem`, `StrongholdSystem` own item and progression data; don't build duplicate inventories or parallel currency managers.
- `Docs/CONTROLS.md` and the game HUD own existing keyboard/controller bindings and menu behavior. Do not add unexplained gameplay keys.

## Art and player-experience target

Midnight navy/indigo/amethyst crystal-gothic urban fantasy, deep stone and metal, premium gold/copper hardware, tactile cyan crystal and a restrained magenta/ember warning palette, believable bodies and creature anatomy, actual rigged/skinned meshes, readable faction role silhouettes, practical daytime readability, accessible combat tells and enterable story-relevant architecture. Make visual changes detectable at the real normal third-person camera distance in actual Free Roam, not only in concept panels or mesh-vertex metadata. Keep original RPG distinct from a conventional open-world crime reskin.

## Verified engineering constraints

No main merge. No Unity HDRP swap. No generic 3D template replacement. No broken saved IDs, quests, catalogue or inventory UIDs. No duplicate boss loot; no invalid Mythic CrystalCaster random entitlement. World/quest events must end, be bounded and avoid passive civilian aggro. Ground/LOS/camera/road/physics checks govern creature spawning; do not infer successful runtime placement solely from species candidate lists or source CI. No claims of native Unity screenshots or FPS from static geometry. Old saves, both endings, M00/M03/M08 crystal waves, Original Underheart, earned bond/fusion, Forge, existing vehicles and city doors require regression.

## Development order and handoff gates

1. **04K currently active: Renaissance in Motion.** Finish its actual original art/movement/creature/Shard War integration work and native acceptance if available. Read the user's existing full 04K directive; do not silently rename or restart its feature branch.
2. **04L — The Cinematic Saga: The Veil Remembers** (`feature/cinematic-saga-04l`). 24 modules: character performances, cinematic mission presentation, original 12 story, camera/subtitles, real animations, creature intros and an optional Veil memory quest. Start from **verified final 04K HEAD**, PR onto 04K.
3. **04M — The Living Crystal World** (`feature/living-crystal-world-04m`). 24 modules: actual districts, population, eight faction cultures, four guild services, gameplay-integrated interiors, creature/ecology, usable forging/economy, territory and optional physical contracts. Start from **verified final 04L HEAD**, PR onto 04L.
4. **04N — The Shardbound Endgame** (`feature/shardbound-endgame-04n`). 24 modules: both canonical First Laws, optional boss, owned drakes/fusions, six crystal focus sidegrades, fairly tuned combat, post-story faction events and release-quality regression. Start from **verified final 04M HEAD**, PR onto 04M.

**Do not open 04L, M and N in parallel on stale heads.** The user may prepare all directives now, but execution must occur sequentially, incorporating actual previous gate results. Every next agent should reconcile the latest remote state rather than assuming the first sprint has finished.

## Acceptance rubric reused in each sprint

1. **Player-facing:** something the player can immediately see/do without F2, within five minutes of normal Free Roam.
2. **Coherent loop:** a complete gameplay interaction involving world presence, an NPC/creature, a physical objective, real resource/reward, legal interruption and return to a calm world.
3. **Correct owners:** all changes through existing game services and saved data; no new unsaved duplicate authoritative singleton.
4. **Safety:** civilian/safe area targeting, spawn bounds, LOS/telegraphs, event lifecycle, boss claim, no exploitation or repeated reward.
5. **Native when possible:** actual Unity import/PlayMode, screenshots, controller, save/reload, Profiler and Windows build, reported separately.
6. **Traceable handoff:** actual HEAD, source/diff paths, test counts and filenames, artifact provenance, module matrix and next three executable steps. Report incomplete work rather than claiming every gate finished.

## Files in this release package

- `Crystal_Manor_Saga_MASTER_BLUEPRINT_04K_to_04N.md` (this shared canon and cross-sprint architecture).
- `Crystal_Manor_Saga_MASTER_SPRINT_04L.md` (independent execution directive after 04K).
- `Crystal_Manor_Saga_MASTER_SPRINT_04M.md` (independent execution directive after 04L).
- `Crystal_Manor_Saga_MASTER_SPRINT_04N.md` (independent execution directive after 04M).

Prepare all now; execute each in a separate Work sprint or continuation from the verified prior branch. If the user's visual master-bible ZIP is available to the Work chat, extract the nested art briefs and use them as creative references. Do not request another Unity project ZIP when repository access works.
