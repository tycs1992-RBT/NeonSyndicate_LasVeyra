# Build 02D: Verification record

## Scope

Baseline: art/crystal-fantasy-block-proof at 242f7be8babd4f703a2627c0bdcb3b09d452a8f9.
Target Editor read from the repository: Unity 6000.3.16f1.
No Editor, package, assembly-definition, input, save, gameplay collision, vehicle or mission changes.

## Executed outside Unity

`python tools/check_crystal_city_02d.py`

Result: 55 source-contract and independent arithmetic checks passed, zero reported failures. Checks include delimiter balance, required integration calls, static reset ownership, forbidden gameplay mutations in the added files, presence of .meta files, palette boundary arithmetic and garden-pocket arithmetic over 175 dimensions. See BUILD_02D_OFFLINE_REPORT.json for exact file hashes.

This is NOT a C# compiler or a syntax parser, and these checks do not execute the C# mesh generator. They cannot establish native API compatibility, correct live scene placement, rendering, gameplay, memory or frame rate. No quality percentage is assigned.

## Tests included but NOT run

CrystalCity02DTests adds eight EditMode test cases:
- Five geographical palette boundary cases.
- Finite vertices, normals, UVs, triangle indices and nonzero triangle areas for all near/far meshes.
- Each distant mesh has fewer vertices, and repeated requests share the source mesh.
- Garden reset clears the counter.

The test fixture intentionally does not use the previously unsupported NonParallelizable attribute. These tests require the actual Editor. Run them from the existing Unity Test Runner; do not install a different test package just for this sprint.

## Local acceptance checklist

- [ ] Import/compile in the pinned Editor, with zero new red Console entries.
- [ ] Run Free Story. The Console includes `Crystal City 02D architecture is active.`
- [ ] Photograph a central building before/after from the same camera position and time of day.
- [ ] Inspect the east and west palettes, including roofline silhouettes and close facade glass.
- [ ] Find an eligible park. Central paths, nearby objectives and parking remain visually readable.
- [ ] Walk, sprint, camera-toggle, drive, exit a vehicle, fight and interact without regression.
- [ ] Check a save load, title return and second Play session for duplicate art and missing resources.
- [ ] Test domain reload enabled and disabled. Confirm source caches and garden count reset.
- [ ] Verify Low and High presets after a full world rebuild, then compare startup time and frame times.
- [ ] Run the existing art tests plus CrystalCity02DTests.
- [ ] Review Built-in and optional URP paths, without migrating the project to run this build.

## Performance status

No CPU/GPU frame times, draw-call counts, heap measurements or loading benchmarks were collected. Caps are design guardrails, not validated hardware budgets. Extra vegetation, two LOD mesh copies per material group and extra material palettes add cost. The source kit is cached; the build creates no physics bodies and no new repeating component loops. If performance regresses, record hardware, graphics preset, location, load time and Profiler data before increasing the budgets.

## Known visual risks

- Palette tint and glass emission are not calibrated against live screenshots.
- Existing ordinary city geometry remains beside upgraded art; transitions require review.
- Rooftop relics are scenery only. Their visual height may affect long-distance LOD selection.
- Garden placement checks known points and walk nodes, not every future event or navigable path segment.
- Architectural ornament is collision-free. Walkability is preserved in code, but visual intersection or camera clipping still needs inspection.

## Recovery

Do not delete the test suite or change engine versions in response to an error. Capture the first new Console error and the branch/commit. Preserve local edits before reverting the one 02D commit. User metadata and recovery scenes are not deleted by this sprint. Recovery folders are not assumed to be automatically ignored by Git.
