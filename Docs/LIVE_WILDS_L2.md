# L2: Real Redspire Wilderness, source integration

Fetched starting HEAD: `e2eb89aaa6a3c63868beee219be2e42a55273acf` on `feature/04oh-ascension-art-foundation`. Newer concurrent commits `ee69b63` and `a65312037e72a7e7d107be73f7ec3f0bdd5ab82e` were fetched and reconciled before publication. Their variant-refusal counts, strict independent FBX inspector, broader 65-check preservation guard and CI steps are retained. PR12 stays draft on its original base. This continues the existing L2 art and unfinished runtime implementation, without rebuilding its models.

V0 is owner-reported seen. L1 pavilion source and art are delivered, but native import, stairs, pillars, camera and save/reload acceptance remain open. No concrete L1 failure was supplied in this continuation. L2 is source implemented, not native accepted.

## What changes in ordinary play

Pause > EXPLORE has a persistent **Redspire kit: OFF / ON** button and **Route: Redspire art walk**. Turn the kit on to replace eligible open-scatter boulders, sandstone spires and dead trees in western Redspire. The route finds a mixed cluster beside the existing Hollowwind Maze or Redwatch Tower corridor and sets the ordinary GPS waypoint. It does not teleport or award anything.

The original six delivered silhouettes, 18 distance meshes, two shared material sets and six 1024-square maps are reused. More rounded weathered rocks, layered sandstone caps and curved dead branches replace their procedural representations. The four other lands, all named-site scenery, buttes, arches, roads, terrain and discovery identities are untouched.

The candidate setting starts OFF and is a graphics preference, separate from game saves. It is restricted to revision 10. The existing 20-second legacy comparison temporarily restores procedural Redspire alongside the earlier character/world comparison, then restores automatically. Saving, loading, returning to title, disabling EXPLORE or starting a QA capture ends the comparison. Physics stays active throughout.

## Implementation and ownership

- `LiveWildsSetupL2` runs through the existing LiveWorld pre-Play/build lifecycle. It verifies the original receipt, source bytes and credits, imports passive readable meshes, includes FBX root axis/unit transforms, prepares normalized meshes and shared materials, and includes the catalog through Resources. Missing source invalidates stale preparation. The fingerprint includes pipeline and preparation version. Generated `LiveWildsPrepared` is ignored by Git.
- `LiveWildsRulesL2` admits only the three Redspire open-scatter kinds and chooses variants from the existing seed. It fits them to the original visual envelope without changing the plan. Invalid data keeps procedural visuals.
- `WildsBuilder` uses the original 160 m chunks. Imported geometry is combined by kind and LOD, with at most nine imported mesh renderers per affected chunk. No per-piece object, runtime filesystem scan, second generator or new Update loop is introduced. There are three actual mesh detail levels and a final cull threshold. Counts are source arithmetic, not draw-call or FPS measurements.
- Every collision triangle still comes from the original `WildsShapes`. Appearance switching builds a complete replacement before publication and transfers the same collider object and mesh without recooking. Batch failure builds procedural fallback. If even fallback creation fails, the previous geometry stays active.
- L1 scoped clearing remains authoritative for both representations. Overlapping leases cannot restore each other's pieces, and failed rebuilds roll back their lease state. Fully cleared chunks remember the selected art setting for later restoration.
- F2 > Wilds reports loaded/refused variants for each kind, replacements, procedural counts, L1 exclusions and rendering/collision ownership. `LIVE_WILDS` retains fallback reasons. A refused family does not suppress the others.

Unchanged authorities include Malik, Dante, live people, Glint/Bonds, creature AI and spawning, combat damage, traversal, story/quests, progression, Forge, inventories, original save schema and banks, seed 61724, revision 10, all 17 wild sites and discovery rewards. Built-in remains default; optional URP uses the same prepared source with its matching material shader. No assets were purchased.

## What to check in Unity, 5 to 15 minutes

Preparation, before the timer: GitHub Desktop > select the existing feature branch > Fetch origin > Pull origin. Open the existing project in Unity 6000.3.16f1. Let import finish. Use the normal scene, current renderer, Medium or higher graphics and Free Roam. If needed, stop Play and choose Neon Syndicate > Live World > Prepare Redspire wild art. This menu prepares assets; it is not a preview scene.

1. First check L1: Pause > EXPLORE > Route: Mirrorwell pavilion. Follow normal GPS, walk up the broad steps without jumping, bump a pillar, exit through a side gap and orbit the normal camera. If any concrete failure occurs, stop the acceptance route and send the first Console error and F2 > Live world placement record. L1 is not accepted by this source delivery.
2. Pause > EXPLORE. Click **Redspire kit: OFF** so it reads **ON**. Click **Route: Redspire art walk**, then resume. Keep the HUD on. Follow GPS west using ordinary movement. In Free Roam, jump and double-tap Space in the air for flight; gamepad uses A. Land before inspecting.
3. Look around the mixed cluster near Hollowwind Maze or Redwatch Tower. Compare rounded textured boulders, layered spires and curved dead trees with the old faceted shapes. Named-site structures and ground stay familiar. Take a wide gameplay screenshot and one close view of each kind.
4. Keep the camera still. Pause > EXPLORE > Compare legacy look for 20 seconds, then close Pause. Take a screenshot of the same cluster before the timer restores it. The camera, time and weather must stay comparable. Restore immediately from EXPLORE if needed.
5. Walk around a boulder and spire, approach a tree, rotate the camera close to each and drive past them slowly. Collision should remain where the original rocks stood, without falling through or creating a second obstacle. Actual collider/silhouette fit needs your judgment.
6. Approach an existing creature and use the original combat/traversal controls. Confirm Glint and the existing encounter behave normally. The kit adds no creature, damage or reward authority.
7. F2 > Wilds: capture the three family rows, replacements/fallback and exclusion counts. Send the first red Console error with its full stack, plus any `LIVE_WILDS` refusal. If the route is refused, this record is more useful than searching the whole map.
8. Save in an existing slot, return to title and reload. Confirm one world representation, the same selected preference and no extra discovery payment. Revisit Mirrorwell to check its clearing and collision. Check evening and compare the same view with the kit OFF and ON.

After the short route, run the existing Golden Shots and Perf flythrough comparison and a Windows build on the real machine. Also run the two new NeedsEngine tests in Unity Test Runner: actual collider identity across switches and overlapping L1 leases. They were compiled here but not executed here.

## Current limits, honestly

This environment has genuine matching Unity managed assemblies, but no runnable Unity Editor. Native import, Test Runner, PlayMode, screenshots, actual saves, physics/camera feel, FPS/Profiler and Windows build/launch are NOT RUN. The source comparison is a labeled Blender render, not a Unity screenshot. Source plan counts omit runtime reserved locations and do not promise live replacement totals.

Original procedural collision is intentionally retained, so the imported silhouette is fitted to its envelope rather than using a new mesh collider. Close-range collision feel, LOD transitions, shadow cost and material appearance need native review. Combined LOD meshes increase geometry memory; no performance acceptance is claimed. No ground-texture or wider-land sprint is included.

Raw validation and publication identities are in `LiveWildL2/VERIFICATION_L2.json` and `LiveWildL2/DELIVERY_L2.json`. If native evidence fails, the next task is its focused repair. Further art passes remain separate acceptance gates.


## Executed source verification and publication

Tested source commit: `9e86bc0a2a871f00d99133613bf4b2fbb3b4f2ba`. C# digest: `6ad689084f3c8d435774b07049752e499cc550953a284b9f440895362752b2cf`. All 12 genuine managed API modes pass with zero added warnings, all 3 assembly boundaries pass, and all 12 syntax modes pass. Focused regressions: 107/107. Full NUnit: 2,337 passed, 47 failures, 61 exclusions, raw result Failed and raw exit 47. The 47 names and reasons match the executed L1 baseline exactly. Two of the exclusions are new native-only collider/lease tests. The final Editor-only DTO initialization has byte-identical player/test sources to the full run; the proof is retained.

Original-asset Blender checks: 91/91; independent strict FBX checks: 18/18; L2 preservation: 65/65; L1 preservation: 51/51; frontier: 15/15; metadata: 2,515 valid sidecars and 13 validator regressions. Source comparisons were reframed to keep every silhouette below the label band; they remain Blender images.

GitHub source run 40217096592 passes L1, L2, schema regression and independent L2 FBX steps. It remains Failed at two inherited H5 assertions, HumanoidPose ownership and the prior-test freeze. Both failures were independently reproduced unchanged in a fresh worktree at the exact fetched starting commit e2eb89a. Those guards were not weakened. All runtime/source/art acceptance claims remain separate from native Unity acceptance.


Later source preserved: `9412c3957f71e0d9e0c8be32fe59c5e0e0df97ea` adds partial-kit GPS and optional URP preparation fixes after the sealed test run. It is retained unchanged. The recorded compile/full-suite results above apply to the exact pinned `9e86bc0a2a871f00d99133613bf4b2fbb3b4f2ba` source; those later source changes were not independently rerun by this takeover session.
