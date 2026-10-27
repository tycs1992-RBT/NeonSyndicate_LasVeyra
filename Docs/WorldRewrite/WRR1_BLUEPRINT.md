# Crystal Manor Saga: Structural World Renaissance Blueprint
Edition: 2026-10-27. Status: design and next-sprint authority, NOT implemented gameplay.

## 0. Current-source truth and research discrepancy

This blueprint incorporates the owner-supplied October 27 Deep Research findings. A direct GitHub check at preparation resolved feature/04oh-ascension-art-foundation and PR #12 to b1ecc142b9587093cd57cbdfe1414ba468b5c5ed. The research report cited a different SHA, 81b6635e0c8662c0ee747497ea56452607dea1b8, and claimed a Docs/WorldRewrite/WR01 handoff. Direct GitHub commit lookup returned no such commit, and the claimed WR01 documents were not found on the connected branch. Those claims remain UNVERIFIED. Always fetch actual HEAD before editing. If newer work exists, preserve it and resume its next unfinished gate.

Current FR-V1 proof tooling is source delivered; Unity import, actual screenshots, Malik material appearance, save/load, gameplay and Windows launch were not natively verified. Historical FR-V1 managed results were 2420 pass, 47 fail, 65 excluded; two inherited H5 CI assertions. Do not reuse historical counts as a fresh test result.

This document augments, never erases, CLAUDE.md, Docs/ARCHITECT_MASTER_PROMPT.md, Docs/FantasyRenaissance/ENGINEERING_BLUEPRINT.md, and historical sprint receipts. All CLAUDE.md house rules prevail.

## 1. Permanent fantasy identity

Original dark-fantasy crystal action RPG, realistic characters, believable creatures, monumental gothic architecture, deeply connected exploration, intelligent combat, meaningful equipment and companion progression, and restrained advanced crystal technology.

Visual hierarchy:
1. Natural geography: cliffs, forests, mountains, caverns, vertical routes.
2. Monumental ancient architecture: structural vaults, towers, buttresses, roofs, bridges, terraces.
3. Inhabited stone settlements: shops, courtyards, residents, practical entrances.
4. Ruins, ecology, underground routes, creature habitats and encounters.
5. Futuristic crystal infrastructure: Resonance conduits, mineral machinery, relics and sealed mechanisms.

Conventional glass towers, flat-roofed city blocks, asphalt, neon storefronts and traffic infrastructure must not dominate the opening view. Do not disguise these forms with crystal props. Do not commit names of other commercial games or their brands to project files.

## 2. Actual current code owners

Confirmed in connected branch:
- CityBuilder.cs Build() constructs ground, roads, intersections, blocks, buildings, props and mountains, then EmitAll(). Visual geometry is batched by 160-meter chunk and material through Get() and _chunks. Colliders are independent.
- CityBuilder.cs BuildBuilding() returns early for Inhabited venue owners, BuildGothicBuilding04OB and BuildRebirth04O. The generic tier path uses Get() and adds tier colliders. Treat each visual path separately.
- CityBuilder.Gothic04OB.cs emits stone, trim and crystal through EmitHeartFR01 and builds exact compound collision separately.
- CityBuilder.HeartFR01.cs already isolates some eligible original gothic visual output through EmitHeartFR01 before shared batching.
- HeartFR01/HeartPlanFR01.cs Eligible() is narrowly scoped to selected gothic families and the existing court slice.
- HeartFR01/HeartPlanFR01.LivingStreets.cs LivingClassification() protects service owners, while LivingClear() selects mostly additive locations around existing lots.
- HeartFR01/HeartPresentationFR01.cs Capture(), BuildRoute(), Apply() and original/art Pair implement reversible visual substitution.
- HeartFR01/HeartPresentationFR01.LivingStreets.cs provides existing prepared-catalog and refusal handling.
- GothicHeartH3 and GothicCivicH5 own original civic geography and protected service relationships.
- VisibleRealityV0, LiveWorldArtH8S and FR-V1 Fantasy Proof own existing player-facing art activation, catalog conventions and local evidence.
- LiveCast owns Malik and NPC mesh/material preparation. Do not replace gameplay or rig ownership.
- UnderheartBuilder and UnderheartWorld own existing dungeon geometry, streamed render groups, always-on collider root, routes, shortcuts, hazards and saves.

Workers must re-resolve current exact file paths and code line numbers after fetching.

## 3. The core engineering change

Stop adding scenery around unchanged original building masses. Make eligible original visual emitters individually replaceable BEFORE they are irreversibly fused into shared chunk renderers.

Build a deterministic coverage manifest for the opening court, north skyline, western street and protected services. For each original mass record stable seed/lot or route ID, world revision, visual owner, batch membership, collider owner, NPC/service/quest owners, doors and portals, world bounds, eligibility, proposed replacement and refusal reason.

Classifications: REPLACE_VISUAL, AUGMENT_ONLY, NATIVE_COLLISION_REVIEW, PROTECTED, UNKNOWN. UNKNOWN and PROTECTED remain unchanged.

Implement visual-group partitioning only where needed, within the original CityBuilder and Heart pipeline. Keep original gameplay, collision, services and saves under original owners. Prepare a replacement inactive, verify mesh, materials, fit, ground, doors, actor clearance and collision policy, activate it, then hide ONLY its matching old visual group. On failure restore the old group. Never suppress an entire shared chunk renderer. Never remove ground or walls under saved actors.

Track selected, prepared, planned, admitted, active and camera-frustum candidates separately. None proves artistic visibility without a rendered Unity frame.

## 4. Real 3D art and environmental production

Reuse FR01, FR02A, H3/H5 and H8 environment assets before creating duplicates. New bespoke art should address large missing forms, not minor decoration:
- Primary sanctuary/cathedral silhouette with believable roof, vault and structural buttresses.
- Distinctive towers, roof families, civic facades fitted to actual protected doors.
- Stone causeways and authentic edges instead of modern asphalt/curbs.
- Cliff and forest-edge massing, large trees, roots, ruined stone bridges.
- Basement cellar, crypt and cavern geometry, mineral machines and atmospheric depth.
- Shared PBR stone, slate, timber, bronze, soil, moss and mineral materials.

No giant stretched boxes with pointed windows. Use structural foundations, piers, openings, real roof geometry and intentional asymmetry. Preserve believable human scale. Source Blender files remain editable, FBX exports must have stable pivots, UVs, normals, LODs where justified, GUIDs, license and LFS manifests. No paid assets without approval. Blender screenshots are DCC evidence, not Unity gameplay.

## 5. A connected fantasy RPG route

Target a continuous first region:
Home court -> replaced gothic skyline -> inhabited stone street -> fantasy causeway / forest edge -> existing Pressure House -> actual descent -> cellar -> upper Underheart -> existing-system interaction or encounter -> return.

Preserve M00-M11, both First Laws, Malik, Luma, Dante, companions, BONDS, Forge, six focuses, Resonance, all 17 discoveries, revision 10 and historical save banks. Do not add a second quest runner, reward ledger, damage system, camera, player controller or world generator.

Underheart has always-on colliders and streamed render groups. Preserve existing entrances, cell IDs, hazards, shortcuts and return. Do not mislabel an instanced room as a physically connected basement.

## 6. Native proof and release truth

Reuse Neon Syndicate > Acceptance > Verify Fantasy World. Do not build a parallel QA framework. Require local project SHA, Unity version, world revision, material status, prepared/active/refused art counts, exact old visual groups suppressed, matched legacy/new Game View frames, player route, save/load, controller and collision checks. If Unity is unavailable, native results remain NOT RUN, regardless of Blender and source tests.

Fail visual acceptance if modern skyline and asphalt still dominate, if new geometry is merely additive, if legacy and new frames barely differ, if Malik is white/untextured, if the route requires teleporting, or if visible architecture contradicts collision. Only the owner can grant OWNER ACCEPTED.

## 7. Dependency-ordered future sprint sequence

WR-R1 Structural First Impression: reconcile current source, partition eligible emitters, replace dominant home/north/west modern masses, rebuild visible causeway and local natural framing, integrate one actual route. First priority.
WR-R2 Living Gothic Heart: complete coherent residences, Forge/Archive exteriors, market and services with valid entrances and inhabitants. Requires R1 native visual feedback.
WR-R3 Surface to Underheart: authentic cellar, crypt, cavern, legal interaction and safe return. Requires collision acceptance for new traversable architecture.
WR-R4 Amethyst Sanctuaries: real mineral cathedral complexes, warded enemies, exploration and permanent rewards.
WR-R5 Azure Shardwood: dense rooted forest, storm wildlife, turquoise caverns and vertical exploration.
WR-R6 Regional Wilderness: Redspire, desert, steppe, Rimewood, geology, habitats and named discoveries.
WR-R7 Characters and Creatures: anatomically believable humans, creature rigs, animations and existing AI integration.
WR-R8 Combat and Resonance: contact, readable transitions, companion coordination, meaningful boss encounters.
WR-R9 Traversal and Mounts: earned magical movement, safe mount systems and later Shardstep if approved.
WR-R10 Living Quests and Factions: meaningful exploration, puzzles, guild consequences, progression and return loops.
WR-R11 Rendering and Performance: evidence-driven atmosphere, materials, audio, culling, LOD and native profiling.
WR-R12 Integrated RPG Slice: 15 to 30 minute cohesive campaign route with real native acceptance.
WR-R13 Stability and Production Foundation: save compatibility, controllers, accessibility, Windows builds, profiling and release debt.

These are proposed future work packages, not implemented records or fixed promises. Reorder only based on tested blockers and owner review. Each sprint must visibly change ordinary gameplay or deliver a complete gameplay loop, not just files.

## 8. Delivery standards

Before each sprint, fetch latest HEAD, inspect actual current source and current handoffs, protect local changes, run baseline, implement source and real assets, commit recoverably, run fresh final checks, verify LFS and GitHub CI, and report tested and final SHAs. Keep PR #12 draft/unmerged and PR #11 untouched.

A sprint is SOURCE IMPLEMENTED only when code/art exist. It is NATIVE PLAYTESTED only after real Unity execution. VISIBLE DIFFERENCE CONFIRMED requires matched actual Game View evidence. GAMEPLAY VERIFIED requires a complete player route. OWNER ACCEPTED requires explicit owner approval. Never infer one state from another.
