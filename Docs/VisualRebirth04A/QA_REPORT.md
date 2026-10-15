# Visual Rebirth 04A verification

The cloud task produced source and asset evidence, plus offline Blender previews. Unity 6000.3.16f1 is not installed in this machine. Unity import, pinned Editor compilation, native EditMode execution, real gameplay screenshots and frame profiling are **NOT RUN**. A successful offline check does not establish live playability.

The comparison base is `8af8559fa646672d94f2c27ea04e3e2e0a1728cf`, the checked live art-branch tip used for this task. Original save data was never read or written by the audit. All 134 existing Git LFS asset pointers were hydrated using the repository's existing authentication: 78 OBJ models, 52 images and four sound files. Their tracked identities are unchanged.

## Executed checks

| Check | Result | What this establishes |
|---|---|---|
| `python3 tools/check_visual_rebirth_04a.py` | 1,889 / 1,889 passed | Actual CMH1/OBJ geometry, inventories, surfaces, GUIDs, exact baseline preservation and authored layout geometry |
| Supplementary Roslyn C# 9 diagnostic comparison | 391 current runtime/test sources, zero introduced compiler errors against 35 baseline version-mismatch errors | Runtime and NUnit source type-checking against genuine older Unity 2021.3 module references, classic input and Built-in only |
| Roslyn syntax parsing across twelve configurations | 12 configurations, zero syntax errors | Active C# 9 syntax for editor/player, three input configurations and Built-in/URP. No API type resolution or compilation |
| Actual city-generator/composition CPU harness | 1,319 eligible structures audited, zero geometry invariant failures | Unchanged city and architecture formulas executed with explicit managed math/resource adapters; scene placement and rendering remain unverified |
| Existing 02E source/arithmetic audit | 60 / 60 passed | Retained source contracts and reference placement arithmetic |
| Existing 02F asset/source audit | 665 / 665 passed | Existing OBJ/PNG/JSON and source contracts |
| Existing 02G asset/source audit | 139 / 139 passed | Existing discovery source/data contracts |
| Existing 02I asset/source audit | 409 / 409 passed | Existing fort asset data and source contracts |
| Existing stronghold Python model suite | 24 passed | Independent Python model of the fort ledger, not execution of game C# |
| Existing discovery Python model suite | 27 passed | Independent Python reward/state model, not execution of game C# |
| Existing 03A2 source audit | 73 / 74 passed, one superseded expectation | Its exact reverse-patch hash expects the pre-04A fort host. The new audit removes the four reviewed 04A edits and recovers the exact actual base file |
| `bash tools/test.sh` | Exit 3, tests not run | The pinned Unity Editor is unavailable |
| Scoped Git LFS integrity check | 296 Unity/delivery objects passed | All current `Assets/` and new 04A evidence objects are present and match their hashes |

The repository-wide LFS cache check was incomplete because 87 unrelated older archive, browser and preview objects were not downloaded into this cloud checkout. The explicit Unity/delivery scope above passes; original pointers remain unchanged. This is recorded in `Validation/retained_audit_summary.json`. Final packaging also normalized whitespace in newly authored model metadata and made the generator retain existing importer settings and GUIDs.

Machine-readable output is in [Validation/asset_preservation_audit.json](Validation/asset_preservation_audit.json). Supplementary compiler evidence is in [Validation/diagnostic_comparison.json](Validation/diagnostic_comparison.json), with the baseline and working compiler output beside it. The older reference libraries cannot resolve Unity 6 physics property names and newer frame-timing fields; these 35 pre-existing errors remain visible in both logs. The scratch-only alias from `PhysicsMaterial` to the older `PhysicMaterial` permits the compiler to continue reporting member errors. No Unity API stubs are used for this diagnostic check, no source adaptation is committed, and no assembly or test executable is represented as a successful Unity build.

The optional Editor toggle/menu classes were source-reviewed and included in [twelve-configuration syntax parsing](Validation/syntax_configurations.json). Each editor configuration parses 398 source files and each player configuration parses 342. This verifies preprocessor-active syntax only. Editor classes remain outside the supplementary type-checking comparison because genuine matching UnityEditor assemblies are unavailable. The Input System-only, combined input, URP and Windows player configurations remain unverified in Unity.

## Actual asset and spatial evidence

The audit reads 47 distinct model masters, 94 high/low CMH1 prototypes and their matching editable OBJ exports. Every shipped triangle has finite vertex data, unit normals, nonzero area and matching winding. Expanded OBJ faces agree with the float32 runtime payload. All inventory hashes, counts and bounds agree with the files. Every low prototype costs no more triangles than its high counterpart. The surface library has 12 texture families, 48 PNG maps and 56 palette/material variants. Normal maps and packed masks have linear import settings; normal maps use Unity's normal-map importer.

All original metadata is retained byte-for-byte and all asset GUIDs are unique. Original models, textures, audio, shaders, scenes and prefabs remain present. Baseline gameplay, saves, input, quests/events, packages and Editor version are unchanged. Existing runtime edits are limited to nine reviewed art/QA integration hosts. Removing the new fort registration call, optional art hook, version-label branch and summary comment recovers the exact baseline `StrongholdView.cs`, including its original collisions and interactions.

The three exact fort layouts contain 177 Hearthspire parts, 170 Galehook parts and 109 Sunscar parts before saved-module ornamentation. Both high and low transformed meshes fit the existing 48-metre placement plot. Triangle clipping confirms the original front approach has no intersecting low art surfaces. The 46 added Hearthspire colliders preserve the gate, core corridor and all six socket envelopes. Rival forts add no colliders.

Hearthspire's western route has continuous analytic support sampled every 0.1 metres, stair rises no greater than 0.25 metres and a final overlook at local height 6.07 metres. For every tread and deck collider, its center and four inset corners match actual visible horizontal triangles on both detail levels. This caught and corrected an early mismatch where deck collision extended beyond clipped octagonal corners. The refined vault soffit has no hanging central posts below source height two metres and reaches all four column-top endpoints in both LODs. This does not replace a Unity CharacterController walking test.

The three surrounding court layouts contain 127 sanctuary, 62 workshop and 56 resource nodes. The final sanctuary render review prompted four shallow soil beds, corner willows, outer garden rails, layered fern/flower borders and additional seating. Triangle-level checks confirm their nine-metre cross paths are clear at pedestrian height on both LODs, and conservative bounds match the live placement validator. Early bench and root intrusions were moved away from these paths; the workshop's overhead vaults now have eight visible support columns. Solid props remain included on Low; their static mesh colliders use the same readable low source mesh. Court placement remains conditional on the existing physical ground/road/POI survey, so these counts describe authored content rather than a measured number of successfully placed courts.

For seed 61724, the [actual city-generator coverage report](Reports/planned_city_coverage.json) records 1,323 generated buildings, 1,319 eligible buildings, four explicitly excluded custom lots and 24 protected helipad roofs. The [source geometry audit](Reports/city_geometry_counts.json) executes the unchanged Tier/Crown and MeshBuilder formulas for all 1,319 eligible buildings: 4,331,028 ordinary-equivalent triangles, a maximum of 14,646 per building, and zero invalid vertices, degenerate triangles, opposed normals, footprint-envelope violations or central landing-zone overlaps. Planned replacement foliage for 558 ordinary trees adds 757,672 triangles including retained trunks/branches. These are CPU source-output counts from disclosed recording adapters, not Unity draw calls, instantiated coverage or GPU performance. The reports record the exact source and asset hashes; those source hashes match the final reviewed files.

The audit's triangle totals are sums of every authored part at each source detail level. Runtime fine-part omission, animated ornaments, batching and LOD culling change the actual drawn totals. These are asset complexity figures, not measured draw calls or frame rates.

## Native tests supplied, not executed

`VisualRebirth04ATests` verifies resource import, valid triangles/normals/UVs/tangents, LOD cost, shared materials and maps, cache destruction/recreation, missing-resource rejection, actual fort capture anchors, neighborhood bounds and invalid transforms. `VisualRebirth04ACityTests` covers deterministic composition, region/variant rules, eligibility and roof landing envelopes. `StrongholdRebirth04ATests` covers saved-slot immutability across module rotations, exact source-mesh bounds, collision exclusions and supported elevated routes. Tests that require Unity object creation are marked as engine-dependent where appropriate.

## Windows acceptance procedure

1. Open this branch in the existing Unity 6000.3.16f1 project after Git LFS finishes downloading. Let import complete. Stop at any new Console compilation error and retain the Editor log.
2. Close the Editor and run `powershell -ExecutionPolicy Bypass -File tools\test.ps1`. Retain `Logs/` results and rerun failed tests after fixes. Reopen the existing startup scene and check the `Visual Rebirth 04A` Console marker.
3. Test a new Free Roam game and a copy of an existing save. Verify return to title, repeated Play sessions with domain reload disabled, pause/resume, keyboard/mouse and gamepad, Third Eye/Veil, driving, flight/glide/ascend/descend, original landing decks and camera collision.
4. Track Hearthspire with the existing STRONGHOLDS page. Enter the gate, claim the unchanged core, walk the western stairs to the northern overlook, visit the garden and surrounding courts, then change all six sockets through each module/rotation and theme. Save/reload and verify ownership, layouts, crystal echoes and flight bonuses. Check 02G shrine rewards and optional rival raids without unintended police escalation.
5. Repeat street, flight, fort, garden and night checks on Low, default and High. Select `Neon Syndicate > QA > Capture Visual Rebirth 04A Looks` for fixed actual in-engine day, golden-hour, dusk, night and rain captures. The runner records real positions, missing forts, renderer counters, memory and sampled frame times. Capture before/after at matching resolution and graphics settings, then run the existing performance flythrough.
6. Use `Neon Syndicate > Graphics > Visual Rebirth 04A` to disable the whole visual layer or one feature, restart Play Mode and verify the previous working art. Re-enable and restart. Confirm no duplicate meshes, particles, colliders or markers after repeated sessions.

Live screenshots, measured startup duration, memory/draw-call/frame-time comparisons, native twelve-configuration coverage and gameplay acceptance remain required before declaring the visual sprint production-ready. Offline previews are labeled source-asset Blender renders and are never substituted for these checks.
