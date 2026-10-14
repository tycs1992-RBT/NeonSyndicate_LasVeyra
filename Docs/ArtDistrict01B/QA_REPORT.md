# Visual Renaissance 01B QA report

## Release status

Implemented source and asset refinement, ready for import and art review in Unity 6000.3.16f1. **Not Unity-compiled, not playtested, not visually approved and not performance-certified.** No Unity editor, C# semantic compiler or shader compiler was available in this environment. No rendered before/after district screenshot was supplied or produced.

The supplied reference images were all opened and inspected. Comparison conclusions concern their visible art direction and the delivered source changes. The offline module sheet is generated from the actual old/new OBJ files, not a substitute for an in-engine screenshot.

## Checks performed

| Check | Result and evidence |
| --- | --- |
| Baseline preservation | Every Master01 source file compared against the preserved delivery. No deletion or unrelated gameplay change. See `STRICT_PRESERVATION_01B.json` and `MASTER01_CHANGES.diff` for exact paths and counts. |
| Source mesh integrity | All 23 OBJ files checked for finite positions, valid indices, normals, UVs, nondegenerate triangles and winding consistency. Eight modules revised; all model names and GUIDs retained. See `ARCHITECTURE_SOURCE_QA.json`. |
| Pane placement | Source transform calculation places the pane 0.135 m ahead of the recessed wall backing. This does not prove Unity import orientation, camera visibility or lack of all possible intersections. |
| Texture integrity | All 32 PNG files decode. Manifest hashes match. Two generator runs were byte-identical. Masks, normal-vector tolerances and import settings were checked; existing texture metadata remains unchanged. See `MATERIAL_SOURCE_QA.json`. |
| C# grammar | All 328 C# files parsed, plus preprocessor evaluation for 12 editor/player, input and rendering configurations. No syntax errors. This is grammar checking only: no type resolution, Unity API binding, assembly compilation or test execution. See `CSHARP_SYNTAX_12_CONFIGS_01B.json`. |
| Shader source review | New drake variants retain the original fog/shadow/depth/motion pass structure and hit-flash/glow interface. URP code keeps package guards. Source review is not shader compilation, variant-retention proof or rendering validation. |
| Lifecycle and scope review | New cached drake material/textures have a reset registered in `Game.ResetStatics`. Surface selection is bounded to existing demonstration road strips. No added input binding, UI canvas, gameplay collider, quest or save field. |
| Offline visual inspection | Actual source mesh comparisons and selected texture maps were viewed. A mirrored pointed-frame winding problem found during this inspection was corrected before final validation. |

Detailed automated integrity results are in `OFFLINE_VALIDATION_01B.json`. Delivery packaging checks and payload hashes are recorded in `DELIVERY_MANIFEST.json`.

## Performance design, not measured performance

- Keep 23 reusable source modules; reduce unique-kit triangles from 10,270 to 9,150. Final scene counts depend on generated buildings and camera position.
- Retain per-building material batches and the existing two-level LOD system. LOD transitions and draw calls have not been measured.
- Retain 32 disk texture maps at their existing resolution. Three new material variants reuse them. The private drake adds two 256-square detail maps and three 16-square palette maps, shared by all matching drakes.
- Crystal shaders add internal pattern sampling; the drake adds a scale-surface sample. GPU cost remains unmeasured.
- Retain a maximum of 12 active ambient emitters with 24 particles each. Trails apply to 25 percent of particles and are disabled on Low. Trail vertex counts and overdraw still need profiling.
- Retain maximum 0/2/4 non-shadow facade lights on Low/Medium/High-or-Ultra. Lower night intensity does not itself reduce their draw cost.
- Reuse the original camera-following reflection probe. High captures at 128 pixels per face and Ultra at 256, time-sliced, on the original 2.5/1.2-second interval. No duplicate probe was added.

## Required Unity acceptance tests: all NOT RUN

1. **Import and compile.** Open an extracted copy in the recorded Unity version. Allow package resolution/import and automatic setup. Confirm no script errors, missing/pink materials or asset-import errors. Compile the 12 configurations: editor and Windows player, each with classic input, both inputs and Input System only, on Built-in and the optional URP setup. Use the project's existing renderer-switch workflow; do not change renderers merely to inspect the default build.
2. **Existing test suite.** Run EditMode tests, including `ArtDistrictTests`, `QuartzRegaliaTests`, save-schema, quest-data, city-layout, creature and camera/pose tests. These tests were preserved; none was executed here. Then produce and launch a Windows development build to check shader retention outside the editor.
3. **Paired visual capture.** Open Master01 and 01B separately. Use the same seed, 1920 x 1080 Game view, graphics preset, camera transform/FOV and weather. Visit the promenade while in Free Roam. Record matched views at 12:00, dusk and 22:00. Inspect near facade, long street perspective, entrance crystals, road reflection and the rooftop drake. Use the existing QA golden-shot tools for broader regression views as well. Record the exact camera transform for any new custom comparison; the existing golden list is not claimed to frame these refinements.
4. **Readability and surface approval.** Check pale stone against dark backing, glazing visibility, arches/import normals, crystal facet retention under bloom and no distracting full-surface pulsing. Confirm the road's roughness reads as mineral paving rather than flat plastic, and test reflection latency at High/Ultra. Check daylight as carefully as night. Crystals and glass are opaque; do not accept them as physical refractive assets.
5. **Cameras, controls and UI.** Traverse, sprint, jump, aim and interact in first and third person. Inspect the 1.5 m particle suppression boundary, near-camera clipping, roof access, arcade appearance against original solid colliders, existing signs and pickups. Verify HUD, reticle, mission prompts, menus and dialogue remain readable with the HUD on. New ornamental geometry has no gameplay collision of its own.
6. **Creature regression.** Inspect hero, standard and crowd LODs from all sides. Test resting, walking, turning, flight and landing poses. Spawn a live Quartz Drake to verify combat wind-up glow, hit flash, visibility, breakable parts and death presentation. Check other species/humanoids remain visually unchanged. The rooftop display is cosmetic, not a newly tameable companion.
7. **Atmosphere transitions.** Move into and out of the district at ground and flight heights. Test Clear to Cloudy, Rain, Fog, Storm and Sandstorm; shift into and out of the Inner World and travel to the Frostline. Verify base lighting returns without stale tint. Disable/re-enable `DistrictAtmosphere` in Play Mode to confirm immediate restoration. No new sky asset or volumetric shafts are claimed.
8. **Resource lifecycle.** Enter/exit Play Mode twice with domain reload disabled. Verify no retained drake materials/textures, stale emitter slots, duplicate lights or errors. Test missing/unreadable kit asset fallback on a disposable project copy, then restore the assets.
9. **Performance.** Run the existing perf flythrough in Master01 and 01B, plus a district walk/flight and several visible drakes, on the intended desktop GPU at 1080p. Record CPU main/render thread, GPU ms, p95/p99 frame time, GC allocations, batches, triangles, particles and memory. Exercise Low through Ultra. Check cost during probe updates and near crystal trails. A 60 fps target means a 16.67 ms total frame budget; this delivery has no measured result against it.

## Known production gaps

The references contain substantially more authored geometry, anatomical sculpting, regional texture detail, environmental composition and lighting than the procedural foundation. This pass refines the actual delivered district but does not close those gaps. Full Manor interiors/evolving rooms, mature forest biomes, large realms, production NPCs, hero dragon sculpt/rig/clips, atmospheric sky assets and combat-effect suites remain proposed. See `REMAINING_PRODUCTION_ASSETS.md` for concrete asset briefs.
