# CRYSTAL MANOR SAGA / NEON SYNDICATE: LAS VEYRA
# MASTER SPRINT WR-R1: GOTHIC KINGDOM STRUCTURAL REBUILD
Status: EXECUTABLE BUILD ORDER, NOT IMPLEMENTED. Unity 6000.3.16f1, C# 9.

You are my complete senior Unity engineering, Blender environment art, fantasy level design, gameplay integration and QA studio. I am the non-programmer creative director. This is ACTUAL IMPLEMENTATION, not research or a documentation-only assignment.

## 0. Exact starting rules

Repository: https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra
Working branch: feature/04oh-ascension-art-foundation
Draft PR: #12, targeting feature/04o-shattered-metropolis-reborn
Connected GitHub HEAD at planning: b1ecc142b9587093cd57cbdfe1414ba468b5c5ed
Unity: 6000.3.16f1

The October 27 research report cited commit 81b6635e0c8662c0ee747497ea56452607dea1b8, but a direct connected GitHub lookup did not find that commit or its alleged WR01 handoff. Treat that claim as UNVERIFIED. FETCH LATEST REMOTE HEAD AGAIN. Preserve every real newer commit and local owner change. If actual WR01 work exists, read it and continue its next unfinished gate, never duplicate it.

Read CLAUDE.md, AGENTS.md where present, START_HERE_04OH.md, RESUME_NEXT.md, Docs/ARCHITECT_MASTER_PROMPT.md, Docs/FantasyRenaissance/ENGINEERING_BLUEPRINT.md, Docs/WorldRewrite/WRR1_BLUEPRINT.md, FR01/FR01E/FR02A/FRV1 handoffs, current roadmap and current code. Preserve original save banks, PR history, asset GUIDs and Git LFS. Never merge PRs, force-push, rewrite shared history, change default renderer/world, or migrate saves without owner approval.

## 1. Non-negotiable creative outcome

I want a grounded, visually rich original dark-fantasy crystal action RPG, not the existing modern-city presentation with fantasy props placed around it.

The first normal Free Roam view must read as a gothic crystal kingdom: monumental cathedral-like skyline, varied slate roofs, buttressed masonry, lived-in courts, stone causeways, real cliffs, rooted trees, ruins and restrained crystal-powered technology. Futuristic identity comes from Resonance, mineral machinery, arcane interfaces and crystal weapons, not office towers or asphalt.

Use believable adult human scale, detailed PBR stone, realistic Malik and NPCs, and coherent near/mid/far visual composition. Do not imitate protected content from other games. No 2D concept art may be called a usable 3D asset.

## 2. Preserve existing gameplay intelligence

Preserve Malik, Luma, Dante, Glint, established cast, M00-M11, both First Laws, Crystal Manor, Veil, Underheart, six focuses, Resonance, BONDS, companions, Forge, equipment, quests, factions, creatures, combat, AI, camera, input, original quest IDs, all 17 discoveries, world revision 10 and historical saves.

Do not create a second player controller, camera, world builder, quest runner, reward owner, combat/damage system, save manager or companion eligibility system.

A service can receive a different exterior without losing its actual NPC, entrance, trigger, transactions or saved identity. Do not hide its functioning doorway behind a new facade.

## 3. Gate A: Focused live-source audit and coverage map

Fetch origin, inspect current HEAD and status, verify LFS and record baseline test failures. Do not repeatedly audit unchanged assets.

Inspect actual CityBuilder.cs, CityBuilder.Gothic04OB.cs, CityBuilder.HeartFR01.cs, HeartPlanFR01 and LivingStreets, HeartPresentationFR01 and LivingStreets, GothicHeartH3, GothicCivicH5, VisibleRealityV0, LiveWorldArtH8S, LiveCast, CityDressing, road and ground partials, UnderheartBuilder/World, and gameplay/service owners.

Known current facts: CityBuilder merges visuals by 160 m chunk/material using Get(), _chunks and EmitAll(). BuildBuilding() has separate early returns for inhabited venues and Gothic/Rebirth paths. Gothic04OB calls EmitHeartFR01 and constructs its collider separately. HeartPlanFR01 eligibility is narrow; Living Streets is mostly additive.

Generate a deterministic coverage map for HOME, NORTH CIVIC, WEST STREET, gateway and Archive. For each dominant original mass record stable lot/seed, original renderer group and batch, collision/service/quest/NPC/portal owners, bounds, visual importance, replacement asset and reason.

Classify exactly REPLACE_VISUAL, AUGMENT_ONLY, NATIVE_COLLISION_REVIEW, PROTECTED or UNKNOWN. Protect UNKNOWN and PROTECTED. Revalidate historical FR01 seeds 961727 and 961728 from current source.

## 4. Gate B: Actually replace original city visuals

Modify existing CityBuilder and Heart planning/presentation owners, not a new generator.

For each eligible original building, separate its visual output BEFORE it enters shared chunks. Keep all original physics, services, quests and saves. Do not hide a whole shared renderer. Preserve normal batching for all unaffected content.

Extend HeartPlanFR01 with stable original visual identity, protected doors/portals, candidate family, fit/collision policy and rollback plan.

Extend HeartPresentationFR01 transactionally:
1. Prepare replacement inactive.
2. Validate actual FBX/material/catalog.
3. Validate scale, footprint, grounding and required openings.
4. Check actor, route and collision policy.
5. Admit and activate new visual.
6. Suppress ONLY matching original visual group.
7. Register restoration on OFF, scene unload or failure.

If any step fails, keep the original visible and report stable ID and exact refusal reason. No double-rendering, invisible original walls, lost colliders or changed saved actors.

Write pure tests for eligible isolation, protected batching, deterministic identities, portal preservation, rollback and repeated OFF/ON.

## 5. Gate C: Monumental gothic opening area

Reuse existing FR01, FR02A, H3/H5 and H8 art where suitable. Produce real editable Blender assets for actual missing dominant silhouettes.

Prioritize one bespoke large sanctuary/cathedral mass, one primary tower, two visibly distinct secondary tower/roof families, large fitted civic facades, structural arches and buttresses, retaining walls and believable stone terraces. The final number depends on the real coverage map, not a quota.

Create actual structural massing: foundation, walls, bays, openings, roofs, towers. Avoid stretched boxes, decorative windows on blank towers, repeated identical buildings, and fake inaccessible doors.

The opening HOME and NORTH views must no longer be dominated by old modern towers. New structures must replace the original visual emitters, not merely stand next to them. Show a coherent skyline from multiple normal camera angles.

## 6. Gate D: Stone streets and natural world framing

Inspect BuildRoadEdge, BuildIntersection, BuildGround, BuildBlock and CityDressing. Preserve original physical roads, GPS, quests and legitimate vehicle connectivity.

Where source owners permit, replace visual asphalt, lane paint, curbs and metropolitan furniture with weathered stone causeways, irregular paving, drainage, wall edges, rooted vegetation and ancient retaining systems. Do not just overlay gray planes on modern roads.

Add a deliberate forest/cliff/ruin transition: large living trees, natural rock outcrops, roots, ruined archways, distant geology and atmospheric depth. Preserve collision; do not create false walkable cliffs or hide existing ground support.

Use restrained amethyst/turquoise crystal conduits, seals and machinery as functional highlights. Do not turn every surface neon.

## 7. Gate E: Inhabited RPG interaction and Underheart continuity

Keep Lofts, Forge/Dante, Archive, Office, existing residents and their original interactions functional. Connect one existing-compatible objective to the rebuilt district through the original quest or event system, with readable goal, real travel, interaction and legitimate completion/return. Use an appropriate existing-system encounter only if legal for the region and progression.

No duplicate NPCs, damage, kill credit, rewards or story unlocks.

Preserve the Pressure House entrance and physical descent. Inspect actual aperture, stair and Underheart collision/render ownership. The complete cellar/crypt/cavern transformation is WR-R3, not a false claim in this sprint. If a safe entrance threshold improvement is possible, integrate it without moving original apertures, cell IDs, save state or stair geometry.

## 8. Gate F: Real asset quality and Malik

Every new art family needs editable .blend source, valid FBX meshes, human scale, pivots, UVs, normals, stable material slots, shared PBR textures, LODs when justified, GUID/hash/LFS/license records and independent reimport/inspection. No unauthorized paid assets, ripped game models or generated junk .meta replacements.

Use existing LiveCast to diagnose white Malik skin/clothing/hair/eye materials. If Unity runs, inspect the actual importer, remaps, textures and shader and fix observed causes. If Unity is unavailable, keep native appearance UNVERIFIED, not FIXED.

## 9. Gate G: Native proof that the world changed

Reuse FR-V1 Neon Syndicate > Acceptance > Verify Fantasy World and PLAY FANTASY WORLD. Do not spend the sprint building another QA framework.

On a native Unity host, verify exact checkout SHA, world revision, catalogs, selected/prepared/admitted/active counts and refusals. Capture matched actual Game View OFF/ON HOME, NORTH, WEST and elevated views, plus one real interaction and the Underheart entrance.

Check camera, collision, stairs, Malik, NPC services, controller, save/load, repeated toggle, Console, and performance where available. Do not count Blender images or camera-frustum candidates as gameplay proof.

FAIL visual acceptance if old city towers and asphalt still dominate, if the new art is only additive, if OFF/ON images barely change, if Malik is white/untextured, if an original collider blocks a new opening, or if the route needs debug teleporting.

If native Unity is unavailable, complete safe reversible source and Blender work, tests and commits, but mark NATIVE PLAYTESTED, VISIBLE DIFFERENCE CONFIRMED, GAMEPLAY VERIFIED and OWNER ACCEPTED as NOT RUN. Give me a 5 to 15 minute no-code Windows walkthrough.

## 10. Gate H: Regression, performance and delivery

Run both twelve-configuration managed compile matrices, 3 assembly boundaries, C# 9 syntax, current FR/H3/H5 preservation, new replacement tests, quest/save/content guards, GUID checks, Blender/FBX/material checks, full NUnit, Git LFS and GitHub CI.

Historical FR-V1 baseline was 2420 pass, 47 fail, 65 excluded, raw FAILED, with two inherited H5 CI assertions. Establish current baseline from fetched source. Repair any NEW failures. Never weaken tests or count skipped checks as passing.

No default HDRP migration or world revision promotion. Performance figures require actual native profiling, not estimates.

Make forward-only checkpoints: coverage; CityBuilder separation; transactional replacement; real art; integrated street/interaction; final verification. Re-fetch before pushes. Keep PR #12 draft/unmerged and PR #11 untouched.

Create Docs/WorldRewrite/WRR1/ with FINAL_STATUS.md, COVERAGE_MAP.json, REPLACEMENT_MANIFEST.json, ASSET_MANIFEST.json, GUID_MANIFEST.json, TEST_LEDGER.md, COLLIDER_REVIEW.md, NATIVE_ACCEPTANCE.json, VISUAL_ACCEPTANCE.md, LFS_HYDRATION.md, OWNER_WALKTHROUGH.md and REMAINING_WORK.md.

Update master bible and roadmap with what was ACTUALLY implemented, preserving all historical records.

Final report: fetched SHA, tested SHA, pushed SHA, exact original modern visual masses replaced, stable IDs, source-admitted/native-active/refused counts, real assets, raw tests, CI, LFS, actual screenshots or NOT RUN, gameplay/collision/save results, remaining modern structures, owner approval and next unfinished gate.

## 11. Priority and stopping rules

P0: Preserve newest source, map owners and baseline.
P1: Partition and replace dominant original modern visual emitters.
P2: Monumental skyline, causeways and natural framing.
P3: One real RPG interaction and existing Underheart route continuity.
P4: Native evidence, source regressions and honest delivery.

Do not spend most of the session auditing or authoring minor props. If interrupted, push a recoverable checkpoint with an exact remaining-work list. If Unity is unavailable, continue substantial safe source/art work but never claim the game is visually approved.

SUCCESS IS NOT A COUNT OF FBX FILES. SUCCESS IS A SUBSTANTIALLY DIFFERENT FANTASY WORLD IN ORDINARY GAMEPLAY WITH THE ORIGINAL RPG SYSTEMS PRESERVED.

BEGIN ACTUAL WR-R1 IMPLEMENTATION.
