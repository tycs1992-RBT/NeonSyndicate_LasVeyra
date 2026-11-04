# L2 executed validation

Final tested code: 9412c3957f71e0d9e0c8be32fe59c5e0e0df97ea. C# SHA256: 8cd95881c55c5f9def3998626b26df9541e9b448bd5c690e945a54028f92646e. The latest fetched evidence-only descendant has identical C# bytes. Earlier reports remain preserved and apply to their own pinned source versions.

| Executed check | Actual latest result |
|---|---|
| Genuine Unity 6000.3.16f1 API matrix, external NUnit | 12/12 PASS |
| Same matrix, genuine bundled NUnit | 12/12 PASS |
| Runtime / Editor / EditMode boundaries | 3/3 PASS |
| C# 9 syntax | 12 modes, zero syntax errors |
| L2 pure fixture | 22/22 PASS |
| Full external NUnit | 2337 pass, 47 fail, 61 exclusions; raw FAILED |
| Failure identity comparison | All 47 names identical to L1 |
| L2 original owner/content/test/asset guard | 65/65 PASS, local and GitHub |
| L1 preservation | 51/51 PASS |
| Exact historical save-schema guard | 15/15 PASS, seven mutation tests pass |
| Blender reimport, UV, hash, LOD and map checks | 91/91 PASS |
| Independent strict FBX parser | 18/18 PASS |
| Metadata audit before workspace maintenance | 2515 metadata, 0 invalid/unresolved, 13 regressions pass |
| Native Unity import/Test Runner/PlayMode/saves/devices/screenshots/perf/Windows | NOT RUN separately |

Latest captured outputs are delivery-*-console.txt and DELIVERY_VALIDATION_L2.json. The normal established filter is cat != NeedsEngine. L1 had 59 exclusions; two explicit new collider/lease tests add two, with no prior test removed. A separate all-fixture managed probe reached those two missing-native failures; delivery-focused-console.txt retains that failed probe. Native fixtures still need the Unity Test Runner.

Earlier VERIFICATION_L2.json, full-tests-release.json and compressed raw NUnit XML/logs seal 2337/47/61 at source 9e86bc0, with zero introduced failure names, changed reasons or resolved names against L1. Latest final code also ran the full suite and retained identical names/counts in its console. Automated workspace maintenance removed the temporary latest report/XML after execution. They are not recreated or passed off as retained latest raw messages. The earlier full raw evidence remains in Git.

GitHub run 40217423105 at 9412c395 passed L1, L2, strict FBX, ownership 26/26, frontier 15/15, H2 110/110 and H3 78/78. Overall CI is still FAILED at H5 46/48: Original actor/gameplay authority / Characters/HumanoidPose.cs, and All prior tests and exclusions preserved. The same two are reproduced in h5-baseline.json/ci-baseline-comparison.json. Those guards are not weakened. Later workflow steps skipped after that failure are not reported as CI passes.

The inherited frontier failure was a stale comparison against a pre-Interiors/Wilds historical schema, not an L2 save edit. saved_schema_lineage_l2.py proves the exact reviewed historical prefix and fixed ancestor/hash, rejects future changes, and passes seven mutation regressions. SaveSystem remains byte-identical to L1. Before and after guard reports are retained.

Earlier exploratory compile failures and the regenerated truncated sandstone map are not final passes. The final map bytes were fully decoded, resealed and revalidated before publication. No Unity screenshot, FPS, actual save result or Windows executable is supplied.
