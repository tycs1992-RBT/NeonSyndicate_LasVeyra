# H5 actual execution ledger

Exact tested source **314483f52ace34751c928b1e827876817f6d7c8e**, C# SHA-256 **a309e09b0804888e787080d452735acb44ae15f6c26ce19614615ad860cf15c9**. Unity 6000.3.16f1 genuine managed references. Final handoff descendant changes documentation/evidence only; source/asset tree equality is recorded in EvidenceH5/delivery-snapshot.json. None of these managed or software checks imports or runs Unity.

| Executed check | Actual result | Evidence |
|---|---|---|
| Genuine API matrix with external NUnit 3.14 | 12/12 PASS | EvidenceH5/managed-api.json |
| Same matrix with actual bundled Custom NUnit 2.0.5 | 12/12 PASS | EvidenceH5/bundled-nunit-api.json |
| Runtime / Editor / EditMode assembly boundaries | 3/3 PASS, 850 / 17 / 138 source files | EvidenceH5/assemblies.json |
| C# syntax configurations | 12 configurations, zero errors | EvidenceH5/syntax.json |
| Full external NUnit regression | 1882 discovered, 1824 executed, 1778 PASS, 46 FAIL, 58 existing exclusions | EvidenceH5/external-tests.json and raw XML/log gzip |
| Exact H3 failure comparison | No introduced/resolved names or changed reasons, exclusions identical | external-tests.json comparison |
| New H5 managed cases | 15/15 PASS, 8 skin cases and 7 civic cases | Full/focused XML |
| Focused H5 plus shipped quest-data validator | 16/16 PASS, 19117 assertions | EvidenceH5/focused-tests.json and raw XML/log gzip |
| Actual binary asset derivation tests | 7/7 PASS | EvidenceH5/python-asset-tests.txt |
| Independent strict actual derived FBX parsing | 5/5 PASS, largest sum error 2.23e-16 | EvidenceH5/normalized-*.json |
| Independent complete-influence deformation | 5/5 PASS, 180968 vertex/pose samples over eight stress poses | EvidenceH5/lossless-*.json |
| Four-weight pruning experiment | Body FAIL .00550511616484m; clothing FAIL .0195033087318m, bound .005m | EvidenceH5/four-*-deformation.json |
| Original raw H3 normalization | Body/clothing still FAIL, originals immutable | Original H3 and H5 guard evidence |
| Five-seed civic connectivity/support/headroom | PASS seeds 61724, 1, 2016, 77231, 92017 | CivicH5Tests XML and world-measurements.json |
| H5/source/intake guards | H5 48/48, H3 78/78, H2 110/110, owner 17/17, inherited 15/15, intake 6/6 | EvidenceH5 guard reports; intake rerun log |
| Unity metadata/reference audit | 1970 records, no missing/orphan/duplicate/invalid/unresolved entries; 13 auditor regressions pass | EvidenceH5/metadata.json and preservation log |
| Git LFS and whitespace | fsck and diff checks PASS | EvidenceH5/final-preservation.txt |
| Source CI on exact tested commit | Push and PR workflows both completed success | EvidenceH5/ci-source.json |

**Raw full NUnit result remains Failed, process exit46.** The comparison wrapper's zero only means no change in those 46 baseline failures; it is not a green suite. Do not convert these into extra exclusions. Full run executed 19306048 assertions over 234.415092 seconds; this is test execution duration, NOT game performance. The exact failure names and messages are in retained external-tests.json and XML. The prior raw H3 sum failures and failed pruning experiment are intentionally retained.

The selected derivatives preserve all original influences, no unsafe four-weight files are shipped. Eight synthetic joint rotations prove the chosen normalization's source mathematical equivalence, not natural motion, clothing fit, native deformation, GPU weight policy, weapon contact, flight, facial quality or owner acceptance. See SKIN_WEIGHT_PROOF_H5.md.

## Reproduction

Reports retain the exact executed command arrays and managed reference hashes. tools/check_04g_compile.py was executed twice, once for external NUnit and once for Unity's actual bundled nunit.framework.dll. tools/check_04g_assemblies.py used the bundled matrix. tools/offline04f_tests.py executed the real compiled player-classic-builtin.dll, comparing against EvidenceH3/external-tests.json. It excludes only the existing NeedsEngine category.

Asset tests: python tools/test_fbx_skin_h5.py. Parser/deriver: tools/fbx_skin_h5.py. Independent parser/pose program: tools/inspect_skin_h5.c using the same pinned ufbx source as original intake; strict parser: tools/validate_candidate_h3.c. Source owner checks: tools/check_real_malik_h5.py plus unchanged predecessor guards. World probe: tools/measure_civic_h5.py, actual compiled release assembly and real kit data. Exact outputs and source-only scope are retained, no mock imported asset or invented screenshots.

Raw managed build logs are compressed under EvidenceH5. Source CI runs https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/actions/runs/39527442023 and https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/actions/runs/39527448390 passed. CI runs source/asset/metadata/intake/syntax guards and independent skin proof; it does not run the full local API matrices/NUnit or native acceptance.

## Native disposition

Native import, native EditMode/PlayMode, current actual Console error clearance, skin rendering/deformation, save/reload, controller/camera/combat, normal-player quest/boss/weapon/Forge acceptance, screenshots/video, visual approval, profiling, Windows build AND launch: each **NOT RUN**. The prior host's pre-import UPM IPC limitation is unchanged and was not repeatedly retried. No professional production Malik/clip set was supplied or created. Owner approval NOT GIVEN. NATIVE_ACCEPTANCE_H5.md contains the exact later Windows steps.
