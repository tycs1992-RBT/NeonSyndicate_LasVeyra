# CRYSTAL MANOR SAGA / NEON SYNDICATE: LAS VEYRA
# MASTER SPRINT FR-01: GOTHIC HEART FIRST IMPRESSION
## Real fantasy architecture, altered skyline, and a playable home-court approach

EXECUTION MODE: ACTUAL UNITY AND BLENDER GAME IMPLEMENTATION. NOT A RESEARCH-ONLY OR PLANNING TASK.

## Sprint card

- **Goal:** In ordinary Gothic Civic H5 revision-10 Free Roam, the Veyra Lofts home-court approach looks unmistakably like an original inhabited gothic crystal settlement rather than a modern city.
- **Player sees:** a monumental stone gate, coherent steep-roof and tower skyline, real gothic street architecture, softened stone/vegetation court edges, the existing sanctuary/Archive, realistic people and the original Forge service along one accessible GPS route.
- **What changes:** actual imported/editable 3D geometry and existing city visual emission where eligible, not just a tint, UI screen or isolated prop.
- **What remains protected:** all original story, quests, travel powers, NPCs, physics, damage, vehicle and save owners; world revision and renderer defaults unchanged.
- **Primary risk:** modern buildings and roads may be combined with structural collision. Avoid hiding a shared renderer or deleting critical collider; use scoped visual replacement with exact fallback.
- **Time to see result:** after a normal Free Roam load, the home-court approach or a single EXPLORE route should show a major difference within the first five minutes of player control. No exact travel duration is asserted.
- **Start checkpoint:** research-time observed PR12 HEAD `102a2f23fe96e480dc2f866cd2370f7ba5a3aa57`. Fetch latest; DO NOT reset or start from this SHA if newer commits exist.
- **Definition:** SOURCE DELIVERED is not NATIVE PLAYTESTED and is not OWNER ACCEPTED.

## 1. Exact repository and safeguards

Repository: `https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra`
Branch: `feature/04oh-ascension-art-foundation`
Draft PR: `https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/pull/12`
Parent: `feature/04o-shattered-metropolis-reborn`
Unity: `6000.3.16f1`, C# 9, twelve compilation configurations, Built-in default with optional URP.

Read current `CLAUDE.md`, applicable `AGENTS.md`, `START_HERE_04OH.md`, `RESUME_NEXT.md`, `Docs/ARCHITECT_MASTER_PROMPT.md`, `Docs/FantasyRenaissance/ENGINEERING_BLUEPRINT.md`, current `Docs/ROADMAP.md`, H3/H5 art coverage and L1/L2/L3/Repair handoffs. Read actual source only for the relevant owners before coding. Archive older reports as historical, not as current HEAD.

Current authoritative source candidates:

- `Assets/NeonSyndicate/Scripts/Runtime/World/CityLayout.cs`
- `.../World/CityLayout.Gothic04OB.cs`
- `.../World/CityBuilder.cs`, `CityBuilder.Gothic04OB.cs`, `CityBuilder.Rebirth04O.cs`
- `.../World/GothicHeartH3.cs`, `GothicCivicH5.cs`
- `.../World/LiveWorld/LiveWorldArtH8S.cs`, `LiveWorldPlanH8S.cs`, its current placement/safety partials
- `.../Runtime/UI/VisibleRealityV0.cs`
- `.../World/CityBuilder.GroundL3.cs` and `.../World/CityDressing.Frost.cs` (frozen source)
- `Assets/NeonSyndicate/Art/WorldFoundationH8R/`, actual H2/H3/H5 kit and manifests.

Determine real route/anchor ownership and real paths at fetched HEAD; do not blindly edit all candidates. The home-court relation to Veyra Lofts, Forge, Archive and court is source-supported but the exact transforms are to be resolved at runtime from current original bindings.

## 2. FIRST ACTION: Recover latest reality and preserve other work

1. Fetch remote branch and PR12 metadata. Verify exact current HEAD, base, draft state and any newer commits. Inspect local status; treat any nonclean worktree as another owner's work and avoid changing it. Work in an isolated clean worktree if necessary without moving or resetting the owner's local branch. No destructive stash/pop/reset/clean.
2. Read latest L3 `Docs/LiveGroundL3/Repair/FINAL_STATUS.md` and `FINAL_SEAL.json`. Its repair restored the Frost file, moved ground hook into scoped CityBuilder and classified generated Unity materials. Confirm these conditions still hold. Avoid repeating their prior failures.
3. L1 pavilion, L2 rocks/spires/trees and L3 ground retain outstanding native acceptance. If Unity Editor is available, perform their minimal smoke and capture failures before modifying world. If unavailable, mark NOT RUN and make the new art fully opt-in/reversible.
4. Preserve `LiveCast`, `ProductionH9`, rig metadata, and any uncommitted files. DO NOT add generated Unity Materials directories.
5. Read latest red CI checks and list exact failing names and baseline; inherited H5 tests must not be disabled. If a new failure is introduced by this sprint, repair it before publication.
6. Record exact existing asset inventory with hashes. Prioritize reuse rather than drawing duplicate portal, vault, root arch or SanctuaryStair files.
7. Emit one concise intake summary, then IMPLEMENT. Do not stop after producing intake docs.

## 3. Gate A: Select one compact, high-impact real route

Read actual world `GothicCivicH5` revision-10 output, H3 home porch, Archive/Office H5, venue bindings, realm route plan, and current GPS service anchors.

Choose one bounded transformation envelope whose default walking approach begins near the Veyra Lofts / inhabited court, follows an existing public route and naturally arrives at the Archive/sanctuary and Forge without crossing protected quest triggers.

Prefer: home court -> newly framed gate -> 1 short street segment -> lively market/court -> existing sanctuary/Archive entrance -> optional safe overlook -> Forge interaction. Do not fabricate a new ruin across a mission route if no safe geometry exists.

Create `Docs/FantasyRenaissance/FR01_AUTHORITY_MAP.json` recording:

- seed/revision and active branch source SHA;
- existing world/service/mission anchors in the slice and their true IDs/transforms;
- all protected colliders, interaction volumes, pedestrian routes, vehicle clearance, camera lines, story triggers, spawn locations, and GPS nodes;
- every original modern visual eligible for overlay or replacement, exact emitter/file identity and fallback;
- original camera viewpoints and expected A/B composition;
- old save/reload tests without editing any save schema.

Avoid first changing world geometry, quest placements or layout; this is a visual-first slice. Explicitly blacklist unresolved/unsafe objects from substitution.

Gate A fail criteria: invented coordinates; no real playable route; inability to classify source collision/renderer owners; any proposal to remove the only quest/service entrance.

## 4. Gate B: Original Blender gothic mesh production

**Not a concept sheet. Build actual original authored models with editable Blender sources and production exports.**

Required priority-1 set:

1. `HeartGate` hero entrance. A clear walkable pointed portal between asymmetrical masonry bases and a recognizable broken/steep pinnacle silhouette. Real foundations, shafts, capitals, voussoirs and buttressing, not a stretched cube with etched windows.
2. `HeartRoofline` kit. At least two differentiated tower/roof silhouettes and gable/chimney elements capable of replacing or visually hiding modern rectangular upper masses from the chosen approach.
3. `HeartFacade` kit. Two to four wall/arcade bay types with genuine carved deep openings, piers, buttresses and consistent scale. Match original service-facing facade envelope rather than burying functional doors.
4. `HeartStreet` kit. Shared stone paving/steps/border and retaining-wall segments that make the route visually read as ancient stone while preserving original walking and vehicle corridor.
5. `HeartWildGrowth` kit. At least two rooted tree/ivy/ruin/rock clusters that occlude unwanted box skylines without blocking paths.
6. `HeartFocus` kit. One focal restrained amethyst/bronze feature near the sanctuary approach (use existing sanctuary art and canonical Third Eye/crystal tone), readable at day and night.

Reuse verified H8R ProcessionalVault, SanctuaryPortal, SanctuaryStair, RootSpringArch, ShardwoodElder and current H3/H5 art where they fit; no duplicate bytes or IDs. Use the existing Archive exterior as a major focal anchor before authoring a replacement cathedral; this sprint should not waste effort rebuilding a production candidate already present.

Assets must have real UVs and normals, intentional structural depth and sufficient variation. Establish an initial 1-meter grid and 4-meter facade bay family as a proposal, adjust to measured source collider/camera/venue envelopes. Maintain proper Y-up Unity export conversion and actual project FBX setup policy.

Ship source `.blend`, asset manifest, FBX LOD0/LOD1/LOD2 for major modules, shared 1K or 2K PBR maps where useful, accurate source/license receipt, source-to-export SHA hashes, and authored collision proxies only for legitimately new walkable volumes.

Materials: weathered structural stone; carved trim stone; aged bronze/dark metal; slate roof; aged timber; worn street paving; moss/root; rare amethyst. Prefer shared families. No blanket neon, glass office facade, repetitive rectangular architecture or a separate unique 4K material for every module.

Generate reproducible Blender previews at approach/gate/market/sanctuary/day/night. Label all as DCC. Never call them Unity screenshots.

If Blender cannot run, integrate already delivered suitable licensed model art and deliver actual importer/placement work; mark fresh Blender production blocked, no false claims. If no real usable source art can be produced, do not substitute primitive proxies as accepted art. Deliver the highest-impact valid partial and exact asset blockers.

Gate B fail criteria: only 2D concepts or procedural cuboids, missing source mesh, no rights provenance, unverified FBX/model/texture pairing, untested normals/scale.

## 5. Gate C: Change the actual visual skyline and street, NOT the save/AI brains

Build a revision-10-only reversible presentation policy with explicit owner eligibility and safe rollback. Prefer extending `CityBuilder` visual emission and the current LiveWorld preparation/placement lifecycle rather than inventing a separate game-world generator.

**Important renderer architecture:** Existing modern buildings may be combined into shared mesh batches. Do not disable an entire shared renderer to remove a single facade. Choose one of these grounded strategies per emitter:

- If safe to alter source visual emission, conditionally omit only the eligible old visible facade geometry and substitute a prepared imported mesh, while keeping original collider, service and nav owners untouched.
- Otherwise, build a correctly bounded original overlay/silhouette that hides the modern form from the approved route without burying the original entrance or causing doubled visible faces.
- Mark unsafe sources skipped with reason; never attempt an automatic broad sweep over all city building renderers.

Ensure core skyline transformation (gate silhouette, roofline, street perspective, sanctuary approach) visible in normal Free Roam. Decorative crystals without skyline change do not count.

Avoid per-piece runtime scripts or per-frame IO. Use deterministic plans seeded from the existing world owner and static batches/instances. Reuse existing import-preparation lifecycle and stable Unity GUIDs. If new meshes fail preparation, previous visuals and colliders must remain visible/functional.

Implement current-owner tooling to display how many eligible modern visual masses were substituted, how many were refused for collision/quest reasons, which model family is used, and where the source is missing.

Preserve initial opt-in state unless the owner explicitly approves a default. The owner's normal Pause > EXPLORE page should offer an obvious `Gothic Heart Renaissance: OFF / ON` action and `Route: Gothic Heart` using actual current GPS and a real accepted approach marker. This is a normal game path, not a separate debug preview scene. Turning ON should visually change the home-court route during this play session. A consistent camera A/B comparison must restore on time, title, save/load, reset and QA capture.

Gate C fail criteria: hidden only in Editor; same skyline before and after; old and new facades visibly overlap; triggers disappear; hard-coded debug coordinates; no fallback.

## 6. Gate D: Route composition, physically credible play and inhabited feel

Create a cohesive progression in the selected bounded area:

1. **Arrival:** one strongly legible Gothic Gate with genuine depth and a traversable portal.
2. **Perspective:** stone processional way reframes the former road using original collision widths and shared paving.
3. **Street:** mixed structural bays and rooflines frame a narrow-looking pedestrian perspective. Only the appearance may narrow if original roads are gameplay-critical.
4. **Court:** retain realistic crowd actors, original markets and service owners; augment with selective trees, ruined walls, small vendor and sign props.
5. **Sanctuary:** preserve existing Archive/portal/dais contacts, but frame its exterior with the new facade/roofline kit and a focal crystal landmark.
6. **Return/overlook:** allow existing story/Forge use, show gothic skyline from accessible elevated camera position only if the original route supports safe vertical traversal.

Camera, collisions and physics:

- Old physical road and door envelopes stay open. Original vehicle and pedestrian collision is not deleted or duplicated.
- Truly new stairs/gate structures get simple World-layer collision proxies only if needed, not high-poly carvings; test grounded steps, capsule clearance and camera near obstacles.
- Check walking, dodge, melee, interaction, earned Surge/Super Leap/Glide/Flight, a sample NPC route and a vehicle approach if current route intersects one.
- Verify Glint and other companions remain eligible and can traverse the court; do not grant new BONDS or attack authority.
- Avoid putting fantasy props in original quest marker radii or where weapons/movement target collisions rely on original geometry.

The route must retain a meaningful original gameplay interaction such as Forge/Dante or Archive inspection. Do not invent a duplicate monetary or skill reward or new save authority.

Gate D fail criteria: visual scene only; unreachable gate; blocked Forge or Archive; camera traps; NPCs trapped; imported art that damages combat or adds duplicate rewards.

## 7. Gate E: Technical art and performance

- Keep Unity 6000.3.16f1 and Built-in default, with optional URP compatibility. No HDRP conversion or new global material pipeline.
- Use shared PBR families, correct sRGB/normal/metal/smoothness packing, and restrained emission.
- Use LODs for skyline and repeated modules; preserve silhouette and avoid aggressive near-distance popping.
- Batch/instance compatible repeating modules without compromising culling. Reuse existing texture/material caches and scoped render lifetimes.
- Ground/root meshes require bounded count and should not add dozens of material sets or hidden per-frame work.
- Avoid a second post-grading controller, duplicate sun/weather owner or unmeasured volumetric effect budget.
- Compare real Profiler CPU/GPU/frame-time, memory, batches and GC on identical accepted route if Unity available. If unavailable, provide source counts and mark actual performance NOT RUN.

## 8. Gate F: Tests and preservation

The sprint must not silently mutate protected owners. Run:

- 12 official managed API compilation configurations on the EXACT final source, or report any unavailable mode honestly.
- 3 assembly boundaries, 12 C#9 syntax modes, relevant new pure EditMode logic tests.
- L1/L2/L3 preservation, current H3/H5 guardians and all applicable source owner tests. Investigate actual new failures; do not weaken rules for green CI.
- New tests for revision-10 scope, presentation ON/OFF, identity/seed stability, safe renderer selection, fallback, transactional rollback, overlapping L1 clear leases, camera/quest/service exclusion, retained navigation and no save migration.
- Asset checks for LOD, materials, transforms, UV, normals, source manifests, embedded/external references, duplicate GUIDs and hydrated LFS payloads.
- Comparison against fetched baseline full NUnit with raw failing/exclusion identities. Latest historical baseline: 2,358 passed, 47 native-call failures, 63 excluded, but recalculate fresh at current HEAD.
- GitHub CI results and any inherited H5 failures by exact name. Never call raw full suite green if it is failed.

**Native if available:** Unity Editor import and shader errors, Test Runner, PlayMode, authored camera from ordinary Free Roam, controller parity, walk gate stairs, companion movement, Forge / Archive, original save/reload, default and comparison, Windows build and Profiler. Otherwise mark each individually NOT RUN.

Require completed native review before offering default promotion. Owner approval remains separate.

## 9. Recoverable implementation checkpoints

Make focused forward-only commits in this order if work warrants them:

1. FR01-A: grounded original authority map and minimal owner-preservation tests.
2. FR01-B: original Blender source, LOD exports, materials, import manifest and license proof.
3. FR01-C: reversible scoped visual substitution, route and normal-game EXPLORE UI.
4. FR01-D: regression tests, evidence, handoff, LFS publication verification.

Use latest branch HEAD and clean worktree. Fetch immediately before each push. On remote divergence, preserve the newer work and reconcile non-destructively; do not rebase shared history, force-push, reset or discard. Keep PR #12 draft on its original base, PR #11 untouched.

Commit only intended production art and code; never Unity-generated material folders, Prepared, Library, Temp, Logs or local backup files. No unlicensed external asset. Avoid banned glyphs and other commercial game names in project source, filenames and docs. Use original fantasy identities.

## 10. Final deliverables and status definitions

Required final report:

1. Fetched HEAD and newer remote commits preserved.
2. Exact tested source SHA, C# fingerprint and final published HEAD.
3. Final PR state with no merge.
4. Exact world envelope and anchors, no arbitrary coordinates.
5. Protected gameplay/collider/service/quest owners and source hashes.
6. Actual imported Blender source and named exported mesh assets, texture maps, LOD and licensed inputs.
7. Which modern visual emitters were substituted vs preserved vs refused.
8. Screenshots from real Unity if executed; otherwise labeled Blender renders only.
9. Performance measurements if actually profiled, with hardware and settings.
10. Raw compile, test, exclusion, failed and CI outcomes, and any native Test Runner details.
11. How fallback/reversible toggle and existing save banks behave.
12. Exact incomplete work and blockers.
13. Five-to-fifteen-minute owner Unity acceptance instructions.
14. Recommendation to repair or proceed, based on evidence, not automated test count alone.

Status terms:

- **BUILT SOURCE:** implementation genuinely pushed, source and offline tests reported.
- **ART READY:** real mesh/source/material asset validated outside engine.
- **UNITY IMPORTED:** Unity importer succeeded with actual mesh/shader/asset references.
- **NATIVE PLAY:** real free-roam route tested with physics, NPC/service and save/reload.
- **OWNER ACCEPTED:** owner confirms it now looks like a fantasy RPG and controls still work.
- **PRODUCTION PROMOTED:** only after explicit owner approval and native success.

A partial source implementation with native gaps is PARTIAL, never complete playable or visually accepted.

## 11. Exact owner acceptance script (no coding)

1. GitHub Desktop: select `feature/04oh-ascension-art-foundation`, Fetch origin, Pull origin once worktree is verified clean. Do not discard stashes/owner backups to force a pull.
2. Unity Hub: open original project with 6000.3.16f1, await import; do not switch renderer.
3. Press Play, Free Roam. Rotate ordinary third-person camera around Malik at the home court. Capture default baseline with HUD visible.
4. Pause > EXPLORE > turn `Gothic Heart Renaissance` ON and choose `Route: Gothic Heart`. If the action is missing, F2 must show the precise refusal. Follow the actual GPS without teleporting.
5. At the gate, capture gateway and upper skyline from the same view. Walk through without jumping, approach a pier and check collision/camera.
6. Follow stone lane to original inhabited court. Inspect NPCs and Glint; interact with one existing original actor/service. Capture ordinary HUD-on screenshot.
7. Approach the Archive sanctuary facade. Check entrance and stairs, orbit camera and inspect roofline from ordinary permitted elevated view.
8. Return to Forge/Dante or another verified original service. Press the original interact key E or Y and verify it still works.
9. Turn development comparison on for its bounded interval. Keep camera and time still; screenshot old vs new and confirm it automatically restores. Return to main route.
10. Save existing slot, return to Title, load, revisit and confirm no duplicate rewards, lost equipment, misplaced companion or changed mission stage. Do not replace any older save bank.
11. Test dusk/night and Medium/Low. Run golden captures and profiling if available. Send first full red Console error, F2 Gothic Heart, matched screenshots, and whether the first-screen impression finally feels different.

**Acceptance**: visual score at least 80/100 from the permanent blueprint, with fantasy identity, architecture, traversal and native save safety each >=4/5. Actual game screenshots and no broken contact are mandatory; Blender renders alone are not acceptance.

## 12. Final execution order

BEGIN MASTER SPRINT FR-01: GOTHIC HEART FIRST IMPRESSION.

Fetch current truth, preserve the existing game, author an original gothic skyline/gateway/facade kit, integrate a bounded reversible actual world-art substitution through the real existing owners, make the change unmistakable from the home-court approach in normal Free Roam, keep one playable route and original contacts intact, test honestly and publish forward-only recoverable work to draft PR 12.

Do not start the next sprint, rework every city block, or claim an untested imported shell is playable. A convincing, safely preserved first fantasy district slice is the success criterion.