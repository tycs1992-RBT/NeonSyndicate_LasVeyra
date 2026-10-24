# H6 actual executed verification

Exact tested source **d25981cab6d908e58e6a1d4f348dae61e9454943**, C# SHA-256 **8814bcaff5609e0ab8ef7fc8defe90a47f33509326872512cee91d82a5b6d284**. Final handoff is documentation/evidence only. Every native Unity status is NOT RUN. Genuine Unity managed assemblies are from 6000.3.16f1; these checks do not launch its Editor or player.

| Executed check | Actual result | Evidence |
|---|---|---|
| External NUnit genuine API matrix | 12/12 PASS | EvidenceH6/managed-api.json |
| Actual bundled Custom NUnit genuine API matrix | 12/12 PASS after two identical-source sequential retries | bundled-nunit-api.json; original killed attempts retained under Development |
| Runtime / Editor / EditMode assembly boundaries | 3/3 PASS, 852 / 18 / 139 source files | assemblies.json |
| C# syntax configurations | 12, zero errors | syntax.json |
| Final full external NUnit | 1894 discovered, 1836 executed, 1790 PASS, 46 FAIL, 58 unchanged exclusions | external-tests.json; actual XML/log gzip |
| Exact H5 failure comparison | No introduced/resolved names, no changed failure reasons; exclusions58 unchanged | external-tests.json comparison, baseline EvidenceH5/external-tests.json |
| New H6 policy cases | 12/12 PASS | Full/focused actual results |
| Focused H6 plus shipped quest-data validator | 13/13 PASS, 34 assertions | focused-tests.json; actual XML/log gzip |
| Existing binary FBX derivation tests | 7/7 PASS | python-asset-tests.txt |
| Actual selected FBX strict parsing | 3/3 PASS, 53 bones each, zero clips/unweighted, maximum7 influences | skin-proof.json and per-file strict reports |
| Complete source skin equivalence | 3/3 PASS, eight stress poses, 2439640 samples, max8.9001965491e-16m | Per-file complete reports |
| Four-weight experiment | FAIL on all3 LOD bodies, global0.00632845259882m exceeds unchanged0.005m | Per-file four-weight reports; not selected/shipped |
| H6 source/asset guard | 53/53 PASS | h6-guard.json |
| Predecessor source/asset guards | H5 48/48, H3 78/78, H2 110/110, owner17/17, inherited15/15 | Named preservation reports |
| Protected source/assets/save declarations | 6/6 PASS against starting0f18b4fb | original-preservation.json |
| Original intake regressions | 6/6 PASS | intake-tests.txt |
| Metadata/GUID/reference audit | 1995 records valid, zero missing/orphan/duplicates/invalid/unresolved; 13 auditor regressions pass | metadata.json |
| LFS and whitespace | PASS | final-preservation.txt |
| Source CI on exact tested source | Push and PR workflows completed success | ci-source.json |

**Raw full suite remains Failed, process exit46.** The comparison wrapper's zero means the failure set did not change; it is not a green NUnit suite. Actual final run executed19306076 assertions in238.766608 seconds. That duration is test execution time, not game performance. The 46 exact names and reasons are preserved in JSON/XML. No new exclusions or test expectation changes were made to conceal an engine or gameplay failure.

## Observed failed attempts and corrections

Two bundled compiler processes were killed with exit-9 and no C# diagnostic during overlapping authoring/compilation work. Same response files and source were executed sequentially and both returned0. Original report/logs and actual retry commands are retained. No claim is made that those initial processes passed.

The original H2 frozen-runtime textual guard initially reported109/110 because intentional H6 review/material hooks changed the importer file. Its normalizer now strips only those exact literal extensions, then performs the original unchanged strict validation-body comparison; final110/110. H6's separate rights, disablement, owner, sockets and original-authority checks plus12 new managed cases cover the new policy. Original before-scope report is retained under Development. No runtime validation or skin tolerance was removed.

A mid-production H6 guard rejected stale source/render hashes after the source was re-exported. All eight views were rendered again from the current editable source, their hashes/manifest refreshed, and the actual final53/53 check passed. The stale report remains in Development.

An initial DCC fit saved but exited139 during extension finalization. The final fit uses an exec handoff after successful save to a separate actual-scene reopen validator; fit/validate, author/export and rendering returned0. Earlier failure disposition is retained. The combined FBX transfer failed HTTP400 due to the connector16MiB request cap. Three separate real LOD FBXs solved transfer without reducing geometry. Neither event is presented as a Unity test.

Four-weight pruning is still FAILED and excluded from selection, not silently reclassified as a passing skin. The selected full-influence source proof passes. H3/H5 raw/rejected proof stays unchanged.

## Reproduce

Actual command arrays/reference hashes are in the JSON reports. tools/check_04g_compile.py used external and actual bundled NUnit separately; tools/check_04g_assemblies.py used the latter. tools/offline04f_tests.py executed the actual release player-classic-builtin.dll, excluding only the existing NeedsEngine category and comparing exact H5 names/reasons. tools/check_visual_rebirth_04a_syntax.py performed all12 real Roslyn parses. Asset proof: tools/prove_malik_h6.py with the independent C parser/inspector and pinned ufbx. Authoring uses tools/fit_malik_h6.py, materials_malik_h6.py, author_malik_h6.py and render_malik_h6.py with the actual source assets/pins recorded in RIGHTS.md and tooling evidence.

## Native disposition

Unity import, current actual Console clearance, EditMode/PlayMode, actual deformation/actions/weapon contact, saves, input devices, camera, screenshots/video, visual approval, profiling, Windows build AND launch: **NOT RUN**. The unchanged pre-import UPM IPC blocker was not repeatedly retried. Original procedural timing remains; authored natural clips and facial performance are ASSET REQUIRED. Owner appearance NOT GIVEN. See NATIVE_ACCEPTANCE_H6.md.
