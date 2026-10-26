# CRYSTAL MANOR SAGA / NEON SYNDICATE: LAS VEYRA
# FANTASY WORLD RENAISSANCE: PERMANENT ENGINEERING BLUEPRINT

Edition: 2026-10-26. Status: DESIGN DIRECTIVE, NOT IMPLEMENTED GAMEPLAY.
Unity 6000.3.16f1. C# 9. Current work is on `feature/04oh-ascension-art-foundation`, draft PR 12, still unmerged when verified.
Last inspected SHA at creation: `102a2f23fe96e480dc2f866cd2370f7ba5a3aa57`. ALWAYS FETCH THE ACTUAL HEAD AGAIN.

## 0. Authority, scope and source hierarchy

This plan interprets the owner's Fantasy World Renaissance research into engineering instructions. It does not override existing source or authorize destructive world migration. Confirm exact implementation at current HEAD before any edits.

Precedence: current actual source and durable ownership contracts > `CLAUDE.md` and applicable `AGENTS.md` > newest preservation and sprint handoffs > tests and manifests > this blueprint > archived planning.

Every implementation worker must use the original gameplay authorities. This project already has a live game with player controls, NPCs, companions, creatures, quests, saved data and travel powers. The purpose of the renaissance is to alter the visible world, not rebuild the game logic.

**Preserve:** Malik, Luma, original missions M00-M11, both First Laws, Crystal Manor, the Veil, Underheart, BONDS, Forge, quest IDs, all 17 discovered-site IDs and rewards, current world revision 10, all older world save banks, vehicle routes and original player/camera/combat/AI/animation owners.

**Forbidden:** unapproved PR merges, force push, rebase, hard reset, saved-game migration, default world revision change, renderer migration, third-party ripped assets, new duplicate damage or reward controllers, deleting legitimate owner art, changing preexisting Unity GUIDs, and calling offline compile a playable test.

## 1. Core creative identity and world model

### 1.1 Gothic Mineral Romanticism

Build a grounded but fantastical crystal civilization. Realistic people and believable material scale live among visually spectacular ancient geology, massive gothic masonry, overgrown courts, secret caverns and selectively glowing minerals.

Visual priority: SILHOUETTE > SPATIAL COMPOSITION > ARCHITECTURE / GEOLOGY > MATERIALS > ORGANIC DETAIL > LIGHTING / VFX.

Never call a rectangle with a pointed decal a finished cathedral. Carved-looking walls need structural base courses, openings, piers, buttresses, floor/roof structure and deliberately shaped skylines.

Region identities:

| Region | Dominant shape | Stone / ground | Nature | Crystal / glow | Architecture |
|---|---|---|---|---|---|
| Gothic Heart | Steep roof, arches, spires | weathered limestone, slate | ivy, courtyards, ancient trees | restrained amethyst | dense lived-in gothic |
| Amethyst | vast escarpment and monumental vault | basalt, pale stone | ceremonial sparse growth | deep violet at sacred focal points | sanctuary and cloister |
| Azure Shardwood | canopy, roots, cliffs | wet rock, dark soil | lush multi-layer forest | turquoise near springs and ruins | root-consumed stone |
| Redspire | eroded silhouette and ravines | iron red sandstone | scrub, deadwood, oases | rare warm mineral seam | wind-worn ruins |
| Rimewood | ice cliffs and snow forms | granite, ice, snow | bare conifer grove | blue-white at special sites | fortified highland gothic |
| Underheart | overhead cave shell, deep shafts | basalt, cave limestone | fungus, hanging roots | localized mineral cores | buried ancient works |

Advanced crystal technology exists, but must appear in deliberately restricted areas. Do not blanket every district with neon billboards and road lanes. Do not eliminate canonically required technology, vehicles or plot locations.

### 1.2 Structural game-world transformation

Recommended production model: Layered Crystal Civilization.

- Reconstruct inhabited cores as interconnected gothic centers, marketplaces and sanctuaries.
- Let forest, cliffs, ravines, broken walls, ancient bridges and gardens divide the currently uninterrupted urban sprawl.
- Interpret required vehicle corridors as stone causeways, old processional roads, canyon routes or crystal transit routes. Preserve underlying navigability and quest coordinates until an explicitly approved migration.
- Treat one settlement slice as the art pipeline proof before any map-wide substitution.
- Favor short sightlines with monumental reveals and vertical routes over thousands of low-value props.

## 2. Verified existing ownership map (research snapshot, not a guarantee of latest state)

Repository paths below were inspected at SHA `102a2f23...`. Confirm all names again if HEAD advances.

| Responsibility | Existing source / asset | Rule for future features |
|---|---|---|
| World seed, original lots/buildings/roads | `Runtime/World/CityLayout.cs`, `CityLayout.Gothic04OB.cs`, `CityLayout.Rebirth04O.cs` | Do not reshuffle saved IDs or global seed for artwork |
| Revision 10 layout selection | `Runtime/World/GothicCivicH5.cs`, `Runtime/World/GothicHeartH3.cs` | Keep Gothic Civic H5 and existing story contacts |
| City visual building output | `Runtime/World/CityBuilder.cs`, `CityBuilder.Gothic04OB.cs`, `CityBuilder.Rebirth04O.cs` | Substitute visual output at a narrow existing owner; do not duplicate geometry/collision |
| Gothic archive / office | `GothicCivicH5.Plan/Structure`, `GothicHeartH3.ServicePlan/Structure` | Reuse original service locations and existing fixed kit where useful |
| Existing modeled world art | `Assets/NeonSyndicate/Art/WorldFoundationH8R`, `ProductionH8S/Props`, `Runtime/World/LiveWorld/*` | Audit/reuse; do not reimport duplicates |
| World-art preparation | `Editor/LiveWorldArtSetupH8S.cs` | Extend the preparation lifecycle, never per-frame disk scans |
| Existing owner navigation | `Runtime/UI/VisibleRealityV0.cs`, `Game.Gps`, `WorldRouteH8S` and realm routes | Provide one normal Free Roam route; no debug teleport as evidence |
| Redspire visuals | `Art/LiveWilds`, `Runtime/World/LiveWilds`, `WildsBuilder` | Separate and preserve L2 reversible visuals and original colliders |
| Redspire ground | `Art/LiveGround`, `Runtime/World/LiveGround`, `CityBuilder.GroundL3.cs` | Keep L3 scoped material flow; DO NOT modify frozen Frost file |
| Discovery and wild sites | `WildsSystem`, `WildsLayout`, `WildsSites`, `WildsChunkL1` | No new discovery rewards or edits to existing site IDs |
| Gameplay/quests and saves | `GameRoot`, `Game`, `SaveData`, `QuestRunner`, `WorldSystems` | Authorities untouched by appearance settings |
| Real characters | `LiveCastSetup`, `HumanoidPrepH9`, current `LiveCast` assets | Avoid adding generated importer materials or changing rig metadata |

**Critical case:** `CityDressing.Frost.cs` was restored byte-for-byte in L3 repair. `CityBuilder.GroundL3.cs` now owns the scoped frost proxy hook. This is the tested path. Do not reintroduce the old Frost edit.

**Protected character asset case:** L3 repair classified and removed unreferenced generated character import materials from the `newest additions` commit, restored 51 protected metadata files, and kept 21 other legitimate newer metadata changes. Never re-add generated import folders to source control.

## 3. Golden Heart development route and baseline

The first transformation should use an existing, short playable route near the revision-10 home court. Candidate route for worker verification: Veyra Lofts approach -> court-side portal/vault -> inhabited court / current market -> Resonance Archive / sanctuary exterior -> Forge / Dante service -> safe overlook (only if existing anchors permit). This is a DESIGN CANDIDATE; the worker must ground exact locations, distances, directions and quest contacts in the live source plan.

The route is not a replacement scene. Use Free Roam with normal inventory, quests, camera, NPCs, Glint and currently earned travel powers. No teleport or forced unlock for visual acceptance.

Prove first-frame difference on the home-court approach: at least one memorable foreground gateway, nonrectangular skyline mass, roof silhouettes and fantasy ground at the ordinary player camera height. A distant optional ruin unreachable in normal play does not meet the outcome.

### Mandatory visible result for every art sprint

The owner explicitly requires a noticeable visual difference each sprint. Before implementation, name one ordinary Free Roam viewpoint or short existing route and the exact architectural/environmental difference expected there. The delivery must state how to enable it and what the player should see within five minutes after reaching the route.

Acceptance requires matched actual Unity OFF/ON views or ordinary player-controlled footage, followed by the owner's confirmation. Keep the same camera, time, weather and quality. Test counts, a new button, an isolated prop or a DCC render do not satisfy visual acceptance. Without usable Unity, deliver useful reversible source/art work and label it PARTIAL, with visible acceptance NOT RUN. Do not advance the next dependent art sprint on that basis. Preserve the existing OFF default until explicit production approval.

Current correction pass: FR01E, see FR01E/FINAL_STATUS.md. FR02 remains unauthorized while native first-impression, contact and owner gates are open.

## 4. Engineering module FR-A: Snapshot and preservation inventory

Inputs: live source tree, original building/court/venue plans, story route samples, collision meshes, art manifests and testing ledgers.

Implementation:

1. Fetch live branch and record HEAD and local status without discarding local changes.
2. Pin revision 10, default seed and actual court service anchors from source. Never invent world coordinates.
3. Map all original world-building objects within a narrow target envelope. For each object record source owner, placement key or deterministic identity, transform, collision owner, quest/interaction dependency, NPC spawn/route dependency, player/vehicle path and candidate art disposition.
4. Classify as renderer-only, decorative+collision, actor/service, story/quest/save, route/physics or unsafe/unknown.
5. Create a machine-readable preservation manifest with scope/allowed edits, source SHA and expected untouched behavior.
6. Record existing L1-L3 owner acceptance status, latest inherited CI failures and specific legacy assertions. Do not make those a green status without actual correction and baseline comparison.

Suggested deliverables: `Docs/FantasyRenaissance/FR01_AUTHORITY_MAP.json`, `FR01_REUSE_MANIFEST.json`, `FR01_BASELINE_STATUS.md`. These are proposed paths until committed.

## 5. Engineering module FR-B: One reversible visual substitution owner

Design one narrowly scoped revision-10 art layer integrated with actual `CityBuilder`/Gothic Heart/LiveWorld owners.

- Existing model/collider/service ownership is preserved. A visual replacement must not suppress the only collider, quest trigger, GPS node, nav link or building anchor.
- If source geometry is combined in existing renderer batches, do not disable an entire shared renderer to hide one modern facade. Change only the specific visual source append path, or conceal selectively without blocking established routes.
- Expose explicit classification for each original building: overlay permitted, independent renderer replace permitted, collision change requires native approval, or protected/no replacement.
- Keep a safe fallback to the legacy renderer per building or module, with clear reason on F2 and Console.
- Runtime appearance toggles should update only visual state, not seed, save data, collider cooking or action progression. Avoid `Update()` handlers per piece.
- Keep restoration transactional. Build replacement renderers before disabling originals. If preparation/import/placement fails, keep or restore previous visuals and collision.
- A temporary old/new comparison must restore on timeout, load, title transition, object teardown, reset and QA captures. Do not write it into story saves.

Suggested architecture: focused `CityBuilder` partial for plan and emission, existing LiveWorld/Editor preparation extensions for imported kit, `VisibleRealityV0` EXPLORE hook and a pure static eligibility resolver. Final names are chosen based on current source, not invented as already existing.

## 6. Engineering module FR-C: Gothic production kit and asset contracts

### 6.1 Geometry families

Target for FIRST SLICE, not the whole kingdom:

- 1 distinctive authored main gate with unequal towers or broken flanking pinnacles, true structural pointed opening and worn stone base.
- 1 sanctuary/Archive silhouette enhancement with rib-vault and roof massing, using existing structure authority.
- 2-4 modular wall/house facade bays with arch, pier and roof variants; retain actual entrance and window alignments.
- 1-2 skyline tower accents, 1 roof/gable set, 2 buttress/column sets.
- 2 walkable stone paving or elevated terrace modules only where current collision safely admits them.
- 2 environmental layers: stone retaining/ruin clusters plus foliage/root/planter clusters.
- Shared controlled crystal sockets, lamps or braziers to guide gaze after dusk.

This is the minimum coherent visual set. The general library roadmap expands to 6-8 wall bays, 4 arch bays, 3 buttress types, 2 tower families, 3-4 townhouse shells, a bespoke cathedral shell, 5-6 stair/terrace modules and further ruins once the proof is accepted.

Do not generate 200 unrelated props or merely reskin modern boxes.

### 6.2 Art and export contract

- Blender source and reproducible export script where possible. Build real editable meshes, not concept images pretending to be geometry.
- 1 m common grid, typically 4 m facade bays, 6-8 m tall civic bays; measure Malik's actual character capsule, camera and vehicular clearance before locking dimensions.
- Real UVs, consistent texel-density policy, clean normals and pivots, no duplicate coplanar surfaces or inverted faces.
- Shared tileable material groups: limestone/dark masonry, carved stone trim, slate/timber, aged metal, ground paving, moss/root, restrained amethyst crystal. Reuse verified existing material assets where appropriate.
- Near/mid/far meshes must preserve silhouette. LOD0/1/2 source evidence; optional more only when justified.
- Separate simple collider proxy for any NEW physically walkable span; never use decorative carvings as automatic collision. Existing collision stays authoritative by default.
- Verify `CREDITS.txt` or manifest per external asset: exact source, author/provider, license, retrieval date, derivative, hashes and redistribution rights. Prefer project-authored or verified CC0/CC-BY with required attribution. No purchase without approval.
- Preserve all existing `.meta` GUIDs. New GUIDs must be unique and stable. Store `.blend` source separately from runtime FBX and texture payload.
- Respect current Git LFS publication and hydration workflow. Verify remote pointers against actual objects. Never commit Unity-generated Materials/Prepared folders.

### 6.3 Native art acceptance views

Seven camera positions: default spawn first impression, gateway approach, mid-street, inhabited court, sanctuary/Archive, short overlook, evening lantern/crystal reveal.

Each view: matched seed, view, quality preset, time/weather and original/renaissance look, with actual in-game HUD-on overview and a clean golden comparison if the QA capture system supports it.

## 7. Engineering module FR-D: City route, service and gameplay preservation

- Reuse `CityLayout`/`GothicHeartH3`/`GothicCivicH5` source-specific coordinates and `RealmRoutes04OB`; no independent quest or NPC placements.
- Retain Forge/Dante, Archive, home, clinic, garage and any overlapping story/interaction geometry.
- Design a visible walkable route: arrival threshold -> arch/gate -> narrower visually framed path -> original inhabited court -> existing Archive/sanctuary structure -> return/service -> overlook if safe.
- Reinterpret required road-space as stone causeway; do not narrow underlying collision or delete it until separate native regression proves safe. Parked vehicles and mission routes must remain functional.
- Surround routes with architectural and vegetation sightline blockers, but first check protected spawn volumes, mission use, vehicle turning, GPS, camera collision, flight approaches and interaction radius.
- Actual story/contact ownership does not change. If a new inspect prompt is added, use a nonreward visual/lore affordance routed through current interaction owner, not a second progression system.
- Keep original faction, NPC, companion and hostile creature logic untouched. New architecture must not suppress expected movement/spawns.

## 8. Engineering module FR-E: Rendering and performance

Keep current Built-in default and opt-in URP. No renderer or Unity version migration.

- Shared PBR material variants with correct tangent normals, texture import types and physically plausible roughness.
- Repeated architectural modules use shared meshes/materials with static batching or instancing where correct, not a per-piece Update loop.
- LOD and shadow settings consistent with skyline readability. Small ornament should not cast expensive long-distance shadows.
- Use existing lighting, time and weather owners. Avoid a new global color-grade implementation.
- Local lighting and probes may be appropriate after reviewing source owners; no budget/FPS claim without native measurement.
- Profile actual CPU main, render thread, GPU when available, GC and memory before and after on the same route/hardware.
- Maintain source run cost bounded to chosen district, not a world-wide runtime mesh rebuild for every appearance toggle.

## 9. Engineering module FR-F: Evidence ladder and release gates

Track statuses independently:

1. SOURCE IMPLEMENTED: code and source checks executed.
2. ART DELIVERED: Blender/FBX/UV/normal/maps/rights validated.
3. UNITY IMPORTED: Unity importer/shaders/guid references verified in native Editor.
4. NATIVE PLAYTESTED: movement, camera, physical path, interaction, keyboard/controller and saved world tested in Editor.
5. OWNER SEEN: owner confirms visual change from ordinary game.
6. PERFORMANCE ACCEPTED: actual profiler and build measurements reviewed.
7. PRODUCTION APPROVED: owner explicitly signs off on default activation, if a default change is proposed.

Offline managed tests can only establish source facts. Previous L3 reference harness still reports 47 raw engine-dependent failures and 63 exclusions. Any future comparison must be against a fresh baseline from the same toolchain, without downgrading legacy assertions.

Required standard test scope: twelve managed compilation configurations, C# 9 syntax, separate assembly checks, current EditMode pure logic, asset manifest and metadata/GUID checks, LFS integrity, preserved quests/save IDs, old-world revision isolation, source preservation and original H5 invariants. Native Unity Test Runner plus real PlayMode and owner checks where available.

CI may remain red at documented inherited H5 tests. Do not call it green or disable it. Show each raw failing identity and whether baseline/current changed.

## 10. Quantitative visual acceptance scorecard

Each criterion is scored 0-5 in actual ordinary-gameplay observation; weighted points = (score / 5) * weight.

| Criterion | Weight |
|---|---:|
| Fantasy identity | 15 |
| Skyline/landmark silhouette | 12 |
| Architectural quality | 12 |
| Environment composition/density | 10 |
| Material cohesion | 8 |
| Organic geology/vegetation integration | 8 |
| Traversal/readability | 8 |
| Character/creature cohesion | 5 |
| Lighting/atmosphere | 5 |
| Meaningful interaction | 5 |
| Performance/frame consistency | 6 |
| Native functionality/save safety | 6 |
| **Total** | **100** |

Suggested target: at least 80/100 AND fantasy identity, architecture, traversal/readability, native functionality/save safety >=4/5. An art score never excuses broken gameplay, collision or saves. If native acceptance was not run, no complete score can be certified.

## 11. Multi-sprint fantasy production roadmap

| Sprint | Visible outcome | Key dependencies | Completion gate |
|---|---|---|---|
| FR-00 Preservation and visual baseline | Clear L1/L2/L3 native outcomes and red CI categories | current HEAD, Unity access | branch safe; baseline evidence recorded |
| FR-01 Gothic Heart First Impression | Gateway, gothic skyline, inhabitable court route in ordinary Free Roam | FR-00 source reconciliation; reversible art lane | visible normal-play before/after |
| FR-02 Gothic Heart Living Streets | Distinctive inhabited buildings, market and service envelopes | accepted FR-01 art | all affected NPC/service paths intact |
| FR-03 Gothic Heart Sanctuary and Roofwalk | Bespoke sanctuary exterior, traversable terraces, garden and reveal | FR-02 architecture and camera | physical route + existing quest use |
| FR-04 Fantasy Causeways and City Segmentation | Urban grid visually broken by walls, cliffs, roots and stone roads | accepted facade kit, road ownership audit | vehicle/story/navigation gates |
| FR-05 Amethyst Monumental Approaches | Terraces, cloister and high escarpment | reuse FR-01/03 kit | story/cavern entrance traversable |
| FR-06 Azure Shardwood Canopy | Dense layered canopy/root/ruin region | vegetation batch pipeline | ecosystem, traversal and camera |
| FR-07 Underheart Cavern Volumes | Actual overhead tunnels, shafts and mineral halls | cave shell and current owner audits | no clipping, puzzles/quests retained |
| FR-08 Redspire Compositional Pass | Dramatic ravines and distant mesas using accepted L2/L3 | L2/L3 owner acceptance | saved discoveries/colliders preserved |
| FR-09 Creatures with Real Silhouettes | distinct skinned regional rigs, authored telegraphs | environment baseline | original creature combat authority |
| FR-10 Living Cast Cohesion | differentiated major characters and NPCs | character art pipeline | actual animation/camera/quest use |
| FR-11 Story and Exploration Renaissance | meaningful hubs, routes, secrets, companions and commissions | accepted physical scenes | progression and reward regression |
| FR-12 Full Fantasy Vertical Slice | connected 15-30 min experience | FR-01 to FR-11 integration | native gameplay and save/build |
| FR-13 Optimization and Release Foundation | predictable performance, quality presets, accessibility | integrated slice | measured Windows build acceptance |

Only one major player-visible outcome per Work sprint. Reorder by real blockers and owner screenshots. All entries are plans, not implementation claims.

## 12. Builder session template

Use for every FR sprint:

1. Fetch branch, report exact HEAD, preserve newer and local work.
2. Read `CLAUDE.md`, latest handoffs, existing geometry/quest/collision owners, and the sprint-specific master order.
3. State one visual outcome and exact game route before coding.
4. Produce preservation map and evidence baseline, avoiding redundant audit of unchanged hashes.
5. Make meaningful source and real 3D asset changes; do not stop at plan or editor-only proof.
6. Test and label results: source, asset, Unity import, native play, owner visual, performance.
7. Push recoverable forward-only commits, update existing draft PR only, never merge.
8. Provide no-code owner route, expected differences, actual evidence and blocker list.

Never promise work will finish later. The session must execute as much permitted work as the environment allows and make a recoverable checkpoint.