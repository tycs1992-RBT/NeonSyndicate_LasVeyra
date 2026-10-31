# H2 executed source verification

Exact tested source **4d8b399358ae0571e95c9f0bd17781e0c1f9e90a**. C# SHA-256 **27c7e26cb5457c4a91d441581ab496bfdea9936101661dc68c504abbcbbcc8ae**, 990 files: Runtime 842, Editor 15, tests 133. Final handoff/evidence changes no C# or runtime art/content source. Raw reports, hashes, exact failures and compressed raw XML/logs are in EvidenceH2. H0 Evidence and H1 EvidenceH1 remain untouched.

| Actually executed | Actual result | Limits |
|---|---|---|
| Genuine API matrix using actual bundled Custom NUnit 2.0.5 | 12/12 PASS | Actual project test API compatibility, Editor/player x three input modes x Built-in/optional URP. Not Unity import. |
| Separate external-runner NUnit 3.14 API matrix | 12/12 PASS | Exact same C# digest. Separate runner compatibility, not native acceptance. |
| Assembly boundaries against bundled NUnit matrix | 3/3 PASS | Runtime, Editor, test assemblies with genuine Editor references. |
| C#9 syntax | 12/12 PASS, zero errors | Syntax only, separate from API checks. |
| Final full external NUnit | 1746 PASS, 46 FAIL, 58 exclusions | 1792 executed, 1850 discovered. Raw outcome Failed, process exit 46. |
| Exact H1 failure comparison | Zero introduced failure names, changed reasons or resolved failures | Same 46 engine-dependent failures and 58 exclusions. Wrapper exit 0 does not make raw NUnit green. |
| New H2 cases | 12/12 PASS in final full run | Actual asset streams/UVs/normals/winding/budgets, corrupt-data rejection, five-seed floor/collision/path/anchor probes, bank/quest eligibility, finite receipts, mirrored boss/core, physical quest data and checklist prefix. |
| Focused final H2 + shipped quest validator | 13/13 PASS | All 12 new cases and real shipped quest validator, unchanged requirements. |
| H0/H1/native-error ownership guards | 17/17 PASS | Original owners, renderer/packages/default boot, exact assertion equivalence and only obsolete unknown-8 sentinel correction. |
| Inherited preservation | 15/15 PASS | Original campaign/reward/Forge/BONDS/damage and old assets/GUIDs; two original H1 FX calls retained. |
| H2 actual assets and protected owners | 108/108 PASS | 19 high/low fixed meshes and matching OBJ positions, PNG channels/import metadata, original source rights, old geometry/skin/transaction owners and finite quest rules. |
| Actual compiled world/composition | PASS | Seed 61724 profile 7/8 graph/anchors/points unchanged; 165 fixed placements. Graph metres, not walking time or FPS. |
| Metadata/GUID/reference audit | 1904 records, 13 auditor regressions PASS | Zero invalid/unresolved metadata or GUID references. Not native AssetDatabase. |
| Read-only intake | 6/6 PASS | Classification/path/hash checks, unchanged proof workflow. |
| Git LFS / whitespace | fsck OK, staged/working diff checks clean | New small original H2 source art has narrowly scoped ordinary-Git paths. Every earlier LFS asset/policy stays intact. |
| Source CI | Two successful source runs, 39501957693 and 39501949288 | Guards, asset integrity, intake, metadata, syntax, LFS and independent calibration parser. Not full API/NUnit or native Unity. |
| Native import, EditMode/PlayMode, actual save/device, screenshots, visual approval, profiling, Windows player | NOT RUN | Unchanged previously IPC-blocked host; no repeated native launch. |

## Owner-reported compile failure

Actual screenshot: Unity 6000.3.16f1 Safe Mode, CS0117 at CinematicRebirth04OFTests.cs:122, Is.AnyOf unavailable. Installed com.unity.ext.nunit is 2.0.5, based on NUnit 3.5. Prior external 3.14 verification accepted that API and missed the defect. Equivalent EqualTo / Or constraints preserve all three allowed IDs. The shown deprecated object-count calls use the supported unsorted FindObjectsByType API and UIKit's unused field is removed. Full exact bundled-API matrix now passes. Native owner reimport is still needed to prove Safe Mode clears.

## Development failures retained honestly

Initial focused compile found a new test using the old RewardToken argument order, corrected in the new test only. First geometry probe failed five seeds because the new vault asset had unwanted ground-reaching legs across the upper ramp approach. Removing those legs from the vault module retains separate side columns. Next probes exposed Azure canopy roots crossing the descending ramp at 2.6 m clearance; they are now above the traversal envelope. No lane tolerance or old test was weakened.

The first full H2 run returned **1741 pass / 51 fail / 58 exclusions**. Its five introduced failures were existing quest-validator tests all reporting the same genuine new-content defect: Dante's 285-character opening exceeded the unchanged 260-character limit. The new dialogue was shortened; validator/tests remained unchanged. The final full run returns 1746 / 46 / 58, exact same old failure reasons. Raw development report and focused failures remain in EvidenceH2/development-history.json and development-first-full.json.

H1 had used 8 as an unknown-revision sentinel. Revision 8 is intentionally valid now; only that sentinel became 99, with the exact source diff guarded. Native NUnit assertion syntax changes preserve identical expected IDs. No genuine failure was excluded, recategorized or given a weaker expectation.

## Reproduction paths

Dotnet /tmp/fhb-b-sdk/dotnet, editor-data /tmp/fhb-b-unity/Editor/Data, package references /tmp/fhb-b-unity/Editor/Data/Resources/PackageManager/ProjectTemplates/libcache/com.unity.template.3d-cross-platform-17.0.14/ScriptAssemblies, project NUnit /tmp/fhb-b-unity/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ext.nunit/net40/unity-custom/nunit.framework.dll, external NUnit /tmp/04of-deps/nunit/lib/netstandard2.0/nunit.framework.dll, NUnitLite /tmp/04of-deps/lite/lib/netstandard2.0/nunitlite.dll, Input System /tmp/04of-deps/input-build/Unity.InputSystem.dll, player references /tmp/fhb-b-unity/Editor/Data/PlaybackEngines/LinuxStandaloneSupport/Variations/mono/Managed. Each copied JSON records its actual invocation/reference hashes where provided.

- tools/check_04g_compile.py, final-api and final-unity-nunit-api output folders, 12 configurations each.
- tools/check_04g_assemblies.py using final-unity-nunit-api.
- tools/check_visual_rebirth_04a_syntax.py and tools/check_unity_metadata_04c.py.
- tools/offline04f_tests.py using final-api/player-classic-builtin.dll, baseline Docs/Ascension04OH/EvidenceH1/external-tests.json. Focused filter selects ArtRevolutionH2Tests plus QuestDataTests.ShippedQuestAndEventDataPassesTheValidator.
- tools/check_ascension_04oh.py, tools/check_living_frontier_04og.py, tools/check_art_revolution_h2.py, tools/test_art_drop_04oh.py.
- tools/measure_art_revolution_h2.py using the final compiled assembly. tools/review_ascension_h2.py renders real asset files offline, not Unity.
- git lfs fsck; git diff --check; git diff --cached --check.

Source and environment access are separate. These results do not prove a working Unity scene, rendered quality, camera/controls, actual player saves, performance or a Windows build.
