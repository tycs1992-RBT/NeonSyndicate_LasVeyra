# Crystal Manor Saga / Neon Syndicate: Las Veyra
# Master 04O-C Continuation: The Fractured Wilds and the Vanishing Metropolis

Status: NEW IMPLEMENTATION ORDER, NOT COMPLETED GAMEPLAY.
Date: 2026-10-19.
Gameplay branch: feature/04o-shattered-metropolis-reborn.
Source handoff at this order: c84ac3f005b966d6fc4fcbbab78065b107ce8c3e, draft PR #11 against feature/crystal-kingdom-awakens-04n.
Unity: 6000.3.16f1. Built-in default, optional URP. Existing original scene/project. New World revision must remain opt-in until native approval.

## 1. Mission

Continue the actual Unity implementation. Do not deliver only a proposal, an art concept gallery, more documents, a synthetic image, or another building-count report. Rebuild remaining low-value conventional urban sprawl as an inhabited, playable, original dark gothic crystal realm with meaningful natural geography, visible creature habitats, short connected routes and real exploration decisions. Make it feel like a compact dark-mythic settlement surrounded by supernatural wilds, not a giant modern city whose buildings have purple windows.

**The core complaint is what the player sees and does between key destinations.** Compress and replace space, not just increase architectural detail. Keep important city functions, vehicles and story routes, but ordinary asphalt avenues and box-tower skylines must stop dominating representative views. A finished fantasy route should feel alive on the ground, during traversal and in aerial views.

Continue the latest actual HEAD on the existing 04O branch. Do not reset it to the reference commit. Do not merge or rewrite PR #11, previous PRs or main. Do not change default profiles or silently migrate saves.

## 2. What is already in the game source, and what is not yet proven

Real existing 04O-B source:
- World revision 2, selectable at Title > Gothic realm preview (04O-B), with isolated realm save bank. Older revisions 0 and 1 retain their compatibility paths.
- All 12 original service hosts bound and relocated into the compact neighborhood. The Forge, Archive, clinic, Dispatch, Pressure, garage and BONDS services retain original owners.
- Cloister structure with genuine open arches and hollow rooms, Archive gallery/stair/bridge, surface two-mouth mineral grotto and return foot path, stone/crystal surface maps.
- Finite Forge survey Stone, Not Silence, original dialogue/quest receipt and payment.
- Gothic structural families, original-height receipt rooftop protections, original roads/vehicles and crystal companions, Crystal Resonance and story/laws.
- Source QA at the previous 04O-B delivery: 12 managed configurations and 3 assemblies; 1549 external NUnit passes, 46 unchanged engine-call failures, 58 existing exclusions. 43 Gothic cases, 3 quest-placement cases. Native game/render/player-save/device/profile/Windows acceptance was NOT RUN on that execution host.

Real compiled 04O-B route lengths at seed 61724:
- Home -> Dispatch: 62.899 m (legacy 69.582 m).
- Home -> clinic: 119.485 m (legacy 670.007 m).
- Home -> Forge: 182.984 m (legacy 990.504 m).
- Home -> Bondkeeper: 49.527 m (legacy 103.033 m).
- Home -> garage: 260.689 m (legacy 1229.499 m).
- Home -> Archive: 158.987 m.
- Home -> threshold: 334.584 m.
- Home -> grotto survey: 362.584 m.
- Dispatch -> Pressure: 76.538 m (legacy 75.789 m, slightly longer).

These distances are compiled graph lengths, not native walk-time measurements. Whole-core area, actual 3D skyline exposure, real creature visibility, visual fidelity and performance remain unverified. Retained roof receipts and external infrastructure must be audited rather than silently deleted.

## 3. New world identity and density target

The desired composition is:
- **Gothic Heart:** A compact lived-in settlement of short stepped streets, shaded courts, workshops, cloisters and rare monumental structures. Human scale first, strong silhouettes second.
- **Amethyst Sanctuary:** A genuine deep multi-room vaulted destination, with gallery space, ancient mineral supports, readable light contrast and lore identity, built from the current Archive rather than a disconnected showcase.
- **Teal Root Grotto:** A short surface cave route with tall mineral clusters, old roots and foreboding ecology; existing two-mouth grotto remains an authentic physical traversal route.
- **Shardwood Fringe:** Low ruined homes, dark trees, fractured paving, boulders, thorn/mineral growth, scattered work camps and small creature territories in space previously taken by repetitive tower blocks.
- **Ember Forge Scar:** Broken industrial cathedrals, crucible machinery, basalt cliff framing, shorter physical access to the existing Forge and real material gathering.
- **Veil Scar and Underheart:** A dangerous transition toward existing Grave Frequency/Veil and deep pressure caverns, with original gates, layer checks and story restrictions.

Do not keep a 1.6 km-wide continuous urban impression just because the world has large MapHalf or named districts. The extensive map may still contain legitimate remote Badlands, Canyon, Frostline, Manor and other wilderness destinations. The target is to greatly reduce continuous generic development, replace it with purposeful terrain and stop normal city towers appearing endlessly in sightlines.

The neon-magical plaza visual language belongs in a small bounded commerce and advanced-tech pocket only. The broader world uses black mineral stone, amethyst, teal, forged bronze, wet slate, aged cloth, gnarly foliage and luminous crystal veins. No copy-pasted endless facades, no neon on every building and no excessive bloom masking geometry.

## 4. Hard success metrics, measured before/after

Target metrics are acceptance goals, not achieved results:
1. In the core hero route, by matched normal-game camera screenshots, modern rectangular towers dominate no more than about 20% of the visual composition across the designated sample set. Audit actual silhouette and not simply material color.
2. Preserve existing tested improved service distances; any route >10% longer requires documented functional reason and owner approval.
3. Provide a naturally discoverable no-combat crystal phenomenon visible or encountered immediately from the home approach; one legitimate optional activity reachable within 60-90 seconds on foot in native testing.
4. Provide a legitimate reachable surface crystal habitat/cave route closer than current 362.584 m where relocation is safe; *design target* for core-to-first-natural-crystal-threshold is <=180 m graph and first accessible small cavern/objective <=250 m graph. If constraints block this, report actual measured path and why; do not fake it by moving a GPS marker or adding an instant teleport.
5. At least three wholly different adjacent routes (sanctuary, natural grotto/creature border and Forge scar) that have distinct terrain, geometry, interactions, and ambient behaviors.
6. At ground level, see a meaningful change of silhouette/space every 20-40 seconds of real normal movement on one validated route; not a guaranteed encounter every 20 seconds.
7. The built settlement should appear small and distinctive from flight. Original remote wilderness may stay large, but it must read as wilderness rather than inactive infinite towers.
8. Validate 10 to 15 minute ordinary gameplay route with at least one optional encounter, one narrative discovery, one earned resource and one real service/quest consequence, plus safe retreat.
9. No duplicate quest payouts, entitlement grants, hostile spawn runaway, save bank contamination or broken original story access.
10. Real performance goals are provisional: target 60 FPS on specified Windows hardware after native profiling. Do not make invented CPU/GPU claims.

## 5. Source ownership constraints

Read CLAUDE.md and exact 04O-B source before editing. Do not redesign all architecture or add a second runtime world.

Existing source owner map:
- World: CityLayout.Gothic04OB.cs, CityLayout.Rebirth04O.cs, CityLayout.Routes04O.cs, CityLayout.Graph.cs, CityBuilder.Gothic04OB.cs, CityBuilder.Rebirth04O.cs, GothicRealmGeometry04OB.*, GothicRealmMaterials04OB.*, MapTexture, WorldBounds, WorldApertures and original CityLayout.
- Scene/application: GameRoot.Rebirth04O.cs, CityRebirthBoot04O.cs, WorldIdentity04OB.cs, main title and SaveSystem world-revision/bank owners.
- Places: 12 VenueBinding04C originals; MissionManager, QuestBook/QuestRunner, Guild boards, Forge, MaterialStore, NPC and resident owners, CreatureDirector/WorldEvents.
- Player/companions: PlayerCamera, PlayerCombat, BondSystem, existing companion AI, vehicle and flight owners.
- QA: Docs/CityRebirth04O/FEATURE_STATUS.md, TRAVERSAL_MATRIX.md, BUILDING_COVERAGE_04OB.json, WORLD_TOPOLOGY.md, PROFILE_COMPATIBILITY_04OB.md, TEST_LEDGER.md and NATIVE_ACCEPTANCE.md.

Use existing BuildRoot/combined chunks and official project material owners. Retain old content IDs, save fields, GUIDs, LFS, graphics/input 12 matrix, ResetStatics and supported engine-native APIs. No third-party commercial names in committed project files. No em-dash or en-dash characters in code/comments/docs as CLAUDE.md requires.

## 6. Revision design and save protections

The current versioned layouts are 0 Legacy, 1 Crystalline preview and 2 Gothic Realm. If large-scale relocation/re-tessellation alters native ground, spatial receipts, exterior quest endpoints or saved actor positions, create an explicit version 3 profile (proposed code name FracturedWilds04OC) and its own bank or a proven safe version-aware routing scheme. Do not silently modify the topology of saved revision-2 games.

Do not add a new duplicate world manager. Derive the revised profile after original deterministic layout + actual venue binding but before Underheart, map and world scene construction. Author semantic anchor migration for the new profile only; exact original content IDs stay the same.

Revision 2 may receive low-risk presentation fixes only if old saves and receipts remain valid and documented. Unknown saved revision must still refuse unsafe restore. No automatic migration, overwriting save banks, default/new-story switch or merging without approval. Player may explicitly choose the new profile from Title after source readiness; legacy and old Gothic previews remain selectable.

## 7. Implementation gates

### Gate A. City sprawl and skyline evidence

Inspect current 04O-C baseline and list the remaining large conventional block mass and distant towers by district, location, role and owner. Do not consider a protected collectible rooftop an excuse to keep the original rectangular building's visible external appearance. Separately inventory the 109 supported roof receipts, 24 external building records, original special sites, service hosts, forts and infrastructure.

Make an automated source coverage report with classification: preserved real gameplay owner, fantasy reskin requiring silhouette rebuild, intentionally inhabited settlement, deliberate scenic ruin, natural terrain, or protected pathway. Source tests should detect regressions in known locations and unclassified exposed building records.

Produce native screenshot capture waypoints for home, Archive, Forge, threshold, grotto, distant skyline, flight and night. If native unavailable on the host, provide exact runnable capture steps and do not invent images.

### Gate B. Shrink remaining continuous urban territory

Implement a finite, deterministic settlement envelope that keeps real functions in compact districts and stops placing large generic blocks throughout the outer ring. Outside compact inhabited nodes, replace filler parcels with physically traversable crystal-contaminated land, small ruins, farms/huts/work camps, cliffs, brambles, dead trees, stone ledges, ravines and safe visible gaps.

Do not merely delete all buildings leaving flat sand. Build a varied landscape by elevation layers, landmark silhouettes and functional routes.

Preserve legitimate vehicle arteries, story race paths, parking, police/emergency access, original wilderness and remote boss geography. Prevent excessive mesh/collider counts through near/far and chunk-aware generation. Ensure cliffs and pits do not trap game characters.

### Gate C. Real terrain and outer crystal biome

Add one cohesive surface biome adjacent to the Gothic Heart, suggested Shardwood Fringe and Veil Scar. It should include geological shapes, mineral veins rooted in terrain, dark forest coverage or alternative sparse alien foliage, varied ground materials, meaningful open glades, territorial boundary signs and real navigable paths. Make the wildland noticeable within 1-2 minutes of ordinary exploration.

Distinguish safe plant/stone ecology from dangerous enemy ecologies. Make creature presence legible through footprints, dens, roosts, ambient sound, mineral scars and warning poses. Use the original CreatureDirector for admitted attack events; do not spawn unlimited agents for cosmetic atmosphere.

### Gate D. Pull first exploration content closer

Create a shorter physical route between home, Archive/sanctuary, outer Shardwood threshold and the current mineral grotto while preserving all actual service access. If existing threshold and grotto have to move, migrate their semantic quest anchors, entrance/collision geometry, route graph and receipt-bearing content together. Update the finite Stone, Not Silence quest, GPS compass, terrain clearance and WorldRevision requirements; verify original revision 2 quest remains valid.

Require a first playable low-risk crystal observation, a legitimate exploration resource, a sanctuary story interaction and an optional finite creature challenge along these routes. No forced attacks near Lofts. A route with only scenic crystals and no payoff does not pass.

Retain distant wilderness for advanced optional expeditions; do not trivialize access to endgame bosses or story-unlocked zones.

### Gate E. Fantasy settlement and silhouette standardization

For 100% of visible buildings inside the chosen hero-capture area, use distinct gothic original silhouettes and plausible occupied depth. Replace modern tall box shells, bland vertical repeats and useless giant blank walls.

Demand structure: arched passage with depth, floors, roofs with different pitches, buttresses, stepped massing, gabled cladding, balconies, cloister openings, optional reachable gallery/landing, window recesses, supported timber/metal structures, genuine interior silhouettes and protected service entrances.

Continue using existing GothicRoomComposition04OB and GothicRealmGeometry04OB. If a hero mesh/room reaches current per-mesh limits, split geometry cleanly rather than dropping details or claiming sampled exports are in-game visual approval. Retain any collectible landing height and coordinates when its associated old save depends on them.

### Gate F. Living habitats and fantasy content loops

Implement 3 connected micro-activities through original owners:
1. Observation/discovery: nonviolent mysterious crystal growth or shrine, with a persistent legitimate discovery receipt.
2. World encounter: optional finite wild creature patrol/defense near fracture edge, with clear tell, retreat, contribution, bounded body count, proper ecology and no repeat payout.
3. Social/crafting: an existing guild, vendor or Forge quest reacts to earned materials, creature event, or environmental clue and opens a real paid craft/service option.

One micro-activity must lead naturally to the next. Do not create a second materials, currency, companion, quest or save system. Preserve original Stone, Not Silence reward and existing settlement/territory reputation.

### Gate G. Cinematic presentation and player navigation

Update both world and UI to emphasize nearby named landmarks, signal trails, silhouettes, actual thresholds and guided discovery. Do not simply hide the full map. The original player must still navigate by keyboard, mouse and gamepad, and the original full map/quest priority remains usable.

Improve terrain/sky composition in actual scene: varied relief, darker rock and warm interior accents, restrained teal/amethyst illumination, readable daylight and night, thick but controlled depth, visible creature/companion silhouettes. Avoid a neon city horizon everywhere and preserve accessible reduced effects.

Capture normal walking and flight at controlled positions. Keep vehicle roads and flight viable, with no invisible mass/collider under roofs or on natural hills. Human companions and drakes require realistic path width/clearance and safe recall.

### Gate H. Saved-game, gameplay and native production acceptance

Preserve and test original story M00-M11, both First Laws, side quests, BONDS, fusion, earned crystal focuses, Crystal Resonance, paid Forge, world events, old revision 0/1/2 save banks, current revision 3 if added, death/travel and Underheart. Never let a legacy save silently restore into incompatible reshaped geography.

Run the real source test matrix: all 12 managed configurations, 3 assemblies, current external NUnit and exact failing name/reason comparison, new pure spatial tests, GUID/metadata, LFS fsck and reliable geometry/graph exports. Baseline from previous 04O-B: 1549 external pass, 46 unchanged native ECall failures and 58 existing exclusions. Never present the raw 46-failure suite as an entirely green native test run.

On a licensed Unity host, import, walk the complete route without debug teleport, exercise real quest/interactions, drive a vehicle, fly, follow with a bonded drake, save/reload, take matched screenshots and run profiler before promoting. If unavailable, label the native stages NOT RUN and continue independent source implementation.

## 8. Exact feature implementation list

The priority modules to complete, in order:
1. Measure urban skyline exposure and content density from original representative samples.
2. Enumerate and transform remaining large conventional structures, with exceptions and safe ownership.
3. Generate one coherent compact settlement envelope and bounded outer wildland.
4. Create natural elevation, cliffs, ravines and path-safe rock/canopy variation.
5. Add strong visible fracture and sanctuary landmarks, with in-world crystal source connection.
6. Relocate/shorten threshold and grotto route safely, including actual quest and map owners.
7. Build a natural habitat and at least one legitimate nonviolent exploration discovery.
8. Add one optional finite creature encounter and one earned material consequence.
9. Connect an original guild/Forge outcome, preserving pay-once rules.
10. Rework skyline and settlement material/light/prefab silhouette at ground and air camera views.
11. Validate foot, vehicle, flight, drake, companion, story and camera traversal.
12. Expand content across other needed districts only after the hero route plays well and looks distinct.
13. Run deterministic spatial and full source regression before native acceptance.
14. Deliver feature/coverage/test/evidence docs, current exact HEAD and recoverable checkpoints.

Do not submit an unconnected art kit as gameplay. Every new passage, grotto, bridge or settlement route must be incorporated in the existing game and its navigation graph. Prefer fewer truly playable spaces over a billion decorative vertices.

## 9. Concrete acceptance route

Without debug teleport, start a newly selected compatible preview save:
- First 10-20 seconds: player sees a distinct fractured-crystal settlement silhouette.
- In 60-90 seconds: discover a real optional crystal interaction, no forced starter ambush.
- Walk to Archive and see its real hollow arches, stair and galleries; navigate normally.
- Walk to an admitted natural border and grotto through a shortened route. Return safely without loading a disconnected demo scene.
- Observe an eligible creature habitat and, optionally, accept/fight/retreat from one bounded encounter with a proper hostile/quiet policy.
- Return to Dante, complete Stone, Not Silence or linked legitimate guild work, then use an existing paid Forge option with retained inventory/material state.
- With an owned eligible companion, perform a legitimate Crystal Resonance chain and return to world exploration.
- Follow with companion/drake through at least one narrower bridge, natural passage, vehicle parking area and legitimate Underheart entrance.
- Save, quit, reload and test untouched revision-0/1/2 bank samples separately, including receipt exhaustion.
- Compare ground, night, aerial and low-effects screenshots to the owner's original large-city complaint, not merely other procedurally generated preview stills.
- Measure walkable route lengths to critical destinations from actual compiled graph before/after and, if native available, record real walk time, controller response and profiler.

## 10. Delivery

Keep PR #11 DRAFT targeting 04N. Continue feature/04o-shattered-metropolis-reborn using recoverable incremental commits and source-only evidence until native approval. Never merge without owner permission.

Update:
- START_HERE_04O.md and RESUME_NEXT.md with exact current truth.
- Docs/CityRebirth04O/FEATURE_STATUS.md.
- Docs/CityRebirth04O/WORLD_TOPOLOGY.md and TRAVERSAL_MATRIX.md.
- Docs/CityRebirth04O/BUILDING_COVERAGE_04OB.json or a new current revision manifest.
- Docs/CityRebirth04O/INTEGRATION_GUIDE.md.
- Docs/CityRebirth04O/TEST_LEDGER.md and NATIVE_ACCEPTANCE.md.
- Docs/CityRebirth04O/VISUAL_EVIDENCE.md, distinguishing source geometry exports from real Unity images.

Required release response includes: changes directly in source; newly reachable normal-player routes; exact distances and exceptions; original/proposed profile states; source tests and any new failure; whether native play and screenshots ran; which new visual family is actually present; unresolved gaps; current commit and PR link. No invented game acceptance or automatic default promotion.

## 11. Finish condition

Do not call 04O-C complete for changing tower color, adding decorative shard meshes, enlarging a cathedral interior, moving a waypoint without relocating the destination, increasing the number of generated buildings, or showing an offline-only render.

Success requires an actual smaller, denser and more mythic playable experience with real adjacent wildlands, fewer generic towers, natural terrain, meaningful crystal encounters, working quests and saved progression, accompanied by the native evidence stage it has truly reached. The world's structure must have changed, not only its texture.

**BEGIN THE ACTUAL 04O-C GAMEPLAY AND WORLD IMPLEMENTATION FROM LATEST 04O HEAD.**
