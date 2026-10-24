# H8 test and recovery ledger

Tested source commit: 36d843a5273d0decbec98c5c46ca1470d54ca3b5. Exact C# SHA256: 5b7b32da37b6d924d7f275ea9513e04b540833799f04347dcf5c4631063534ac. This digest was recomputed after recovery and exactly matches both completed genuine compilation matrices. Actual artist source/FBX were recovered from uploaded Git objects; sampled data was regenerated from that exact source with the same Blender 4.2.23 exporter.

| Actually executed before maintenance | Result |
|---|---|
| Genuine Unity 6000.3.16f1 + external NUnit API | 12/12 passed |
| Genuine Unity 6000.3.16f1 + bundled NUnit API | 12/12 passed |
| Runtime / Editor / EditMode managed boundaries | 3/3 passed |
| Roslyn syntax | 12/12, zero errors |
| New H8 focused tests | 34/34 passed, 14,401 assertions |
| External full NUnit | 1,853 passed, 46 failed, 58 excluded; raw Failed, exit46 |
| Exact H7 failed names and messages | No new failures, no changed reasons, none silently excluded |
| Actual animation FBX | 14 clips, 53 bones, 29,044 bone samples, zero root translation |
| Editable DCC lineage | 4/4, H7 geometry/skin/skeleton and five original actions retained; nine new actions |
| Metadata | 2,028 valid sidecars, 13 regression cases |
| H8 / H7 / H6 / H5 guards | 27/27, 59/59, 53/53, 48/48 |
| H0 / H2 / H3 / gameplay preservation | 17/17, 110/110, 78/78, 15/15 |
| Intake / skin tool | 6/6, 7/7 |
| Native Unity / gameplay / saves / screenshots / profiler / build | NOT RUN |

Workspace maintenance removed the unpublished final raw XML/logs and some DCC renders during publication. EvidenceH8/execution-receipt.json preserves the actual completed results, not reconstructed raw output. Earlier failed fixture and first 29-case receipts already published in the initial checkpoint remain intact. The fixture array loader was repaired using strict list DTOs; no existing tests, expected engine failures or exclusions were weakened.

The recovered full C# bytes exactly match the tested snapshot. EvidenceH8/recovered-data.json is a fresh post-recovery data inspection, separately labeled. Remote CI metadata/source/FBX/syntax checks are separate from the earlier managed execution and from native Unity. No successful native acceptance or commercial character quality is inferred.
