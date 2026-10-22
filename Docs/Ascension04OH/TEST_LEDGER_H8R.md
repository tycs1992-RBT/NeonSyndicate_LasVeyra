# H8R executed verification

Code integration checkpoint: 21932201822a96f2aa419d9f6dc0eaf8df97fae2. Final art/evidence checkpoint: 4f1dfb67bf1a562e1f5511294bcf0f6c2e2cedab. Exact final C# SHA256: 693cf077b788db5a5ef5a260f0c4856e0686e2e79bb971574e93c3cf7f7b7a9d. Both final API matrices captured these same bytes. The executed external test assembly's Runtime and EditMode test files were byte-compared with the final source: no differences. A final Editor-only skin-normal calibration was separately covered by both final API matrices and assembly checks.

| Executed check | Actual result | Evidence |
|---|---|---|
| Genuine Unity 6000.3.16f1 + external NUnit API | 12/12 PASS | EvidenceH8R/external-api.json and raw compressed row logs |
| Genuine matching Editor + bundled NUnit API | 12/12 PASS | bundled-api.json and raw row logs |
| Managed Runtime / Editor / EditMode boundaries | 3/3 PASS | assemblies.json |
| Roslyn C#9 syntax | 12/12, zero errors | syntax.json |
| Full external NUnit | 1,875 passed, 46 failed, 58 excluded | external-tests.json, external-results.xml.gz, external-nunit.log.gz |
| Exact known failures | No introduced names, changed messages or silently excluded cases | Full report comparison with executed H7 baseline |
| New H8R policy/motion cases | 20/20 passed within full suite | Actual test-case records in XML |
| Final targeted action/rollback run | 56/56 passed, 14,470 assertions; focused-final.json | Explicit filter, separate from full suite |
| Selected source DCC | 41/41 checks; 45 real deformed source poses sampled | dcc-audit.json |
| Actual primary FBXs | Three strict parses; 53 bones, UV/normals, zero unweighted vertices, complete influences | Per-FBX JSON |
| Actual NPC FBXs | Two strict parses; 65 original weighted bones retained | Per-FBX JSON; source studies remain unapproved |
| Static environment FBXs | Six independent static geometry/UV/normal parses | Per-module JSON |
| H0 / H2 / H3 / H5 / H6 / H7 / H8 source guards | 17 / 110 / 78 / 48 / 53 / 59 / 27 checks passed | Preservation JSONs |
| Gameplay parent preservation | 15/15 | gameplay-preservation.json |
| Skin / intake Python cases | 7/7 and 6/6 | Captured tool output |
| Metadata/GUID/reference audit | See metadata.json; zero missing/duplicate/invalid/unresolved records | 13 checker regression cases passed |
| Native Unity, actual saves/devices, screenshots, Profiler, Windows build | NOT RUN | NATIVE_ACCEPTANCE_H8R.md |

Full NUnit raw outcome is Failed and raw exit is 46. The comparison wrapper can exit 0 when all failures exactly match baseline; that does not make the full suite passing. These are managed execution results against genuine modules, not native EditMode, Unity import, assembly-definition execution or a playable build. Fifty-eight original NeedsEngine exclusions were retained.

## Discovered problems and corrections

The first full run had 1,874 passes and 47 failures. The extra failure was an earlier, previously unexecuted H8 recoil fixture indexing the valid original-pose sentinel -1. The fixture now asserts exact outcomes 0, 0 and -1 for blade, unarmed and melee ownership. Runtime recoil rejection was not relaxed, and no failure was reclassified. Development/first-recoil-fixture-* retains the raw first outcome.

The first NPC asset parses found a missing boot UV layer. The source preparation now preserves/generates real UVs before export. Both final NPC parses pass; their visual/material/native approval remains unfinished. The supplied terminal leaf bones carry real weights, so all 65 were retained instead of dropping influences.

Source renders exposed oversized inherited optical surfaces, a stylized donor beard, incomplete boot coverage and coat depth fighting. Eyes were refitted; the donor groom was rejected for Malik; source-surface strand grooming and closed body-derived boots were used; garment surfaces were separated before thickness was restored. Finite skin tests never claimed these visual problems were absent.

A development compile caught a mistyped test enum member; it was corrected to the established Fly value before the final matrices. No final source compile error remains. No native Console result is inferred.

## Published-source recovery

The final art/evidence commit is 4f1dfb67bf1a562e1f5511294bcf0f6c2e2cedab. The existing publication workflow uploaded the actual binary objects and advanced history to c592bd4906ee340cf7eb033502559a65980f9e2e. A separate clean checkout downloaded the selected art through Git LFS: all 108 changed files and all 39 manifest asset hashes match, no C# source changed from the tested integration, and git lfs fsck returned OK.

CI on the temporary raw-art commit failed the expected-pointer checks before forward normalization. The LFS publication job succeeded. The bot commit source run was marked action_required by GitHub; a subsequent ordinary documentation delivery triggers validation against normalized content. These publication states do not imply a native Unity result. DELIVERY_H8R.json pins the recovered source and actual test dispositions.
