# H1 executed verification

Exact tested source: **74e1527c1eab463a684fd10881abc45b4e9203c8**. C# SHA-256 **e922e02eb56963cff0242cef580ae66098cbe1535a919e9141268f436dc061c6**, 987 files. The final handoff/evidence commit changes no C# source. Raw reports, failure messages, command/reference hashes and raw compressed XML/logs are in EvidenceH1. H0 evidence remains unchanged in Evidence.

| Executed check | Actual result | Scope |
|---|---|---|
| Genuine managed API compilation | 12/12 passed | Editor/player x Classic/Both/Input-only x Built-in/optional URP metadata. Not Unity import. |
| Independent assembly boundaries | 3/3 passed | Runtime 840, Editor 15, tests 132 files. |
| C#9 syntax configurations | 12/12 passed, zero errors | Real syntax parser, separate from API compilation. |
| Final full external NUnit | 1734 passed, 46 failed, 58 excluded | Raw result Failed, raw process exit 46. 1780 executed, 1838 discovered. |
| Exact H0 comparison | Zero introduced failures, changed reasons or resolved engine failures | Same 46 engine-dependent failures and same 58 exclusions. Comparison wrapper exit 0 does not turn raw NUnit green. |
| New H1 cases | 18/18 passed in final suite | Skin/budget/bones/expressions, three detail levels, legitimate companion visual tag, original combat volumes, five-seed geometry/paths, bank isolation, quest eligibility, phases and checklist preservation. |
| Focused H1 + unchanged Quartz Regalia | 30/30 passed | 18 H1 and all 12 original regalia tests, including unchanged decoration budgets. |
| Source preservation | 15 H0/H1 + 15 inherited checks passed | Original campaign, gameplay owners, assets/GUIDs, original bank mappings, renderer/packages. Two authorized visual calls are the only BONDS transaction-owner edits. |
| Actual source geometry export | Passed | Real emitters, finite/wound triangles, zero degenerate/normal errors for new route kit; all original authored anchors retained. Not screenshots or native Physics. |
| Route/collision source probes | Five seeds passed | 61724, 1, 2016, 77231, 92017. Real floors, sampled body/companion-height lanes, adjacent bay floors/headroom, retained inherited routes and graph reachability. No native capsule/vehicle/camera claim. |
| Metadata/GUID/reference audit | 1813 records, 13 auditor regressions passed | Zero invalid metadata/references or unresolved references. Not native AssetDatabase. |
| Read-only intake | 6/6 passed | Existing classification/path/hash rules, no extraction/execution. |
| Unchanged diagnostic FBX | Strict independent parser passed | Real 47-bone calibration skin/bind/clip proof, not production Malik or Unity import. |
| Git LFS / whitespace | fsck OK, diff checks clean | Narrow raw-evidence gzip exception; original asset LFS policy retained. |
| Source CI on tested source | Two successful runs | 39490311307 and 39490304489. Guards, syntax, intake, metadata and independent diagnostic FBX; not native/full API/NUnit. |
| Native / visuals / performance / Windows | NOT RUN | Unchanged before-import IPC-blocked host; no repeated Editor launch. |

## Actual invocation paths

From the repository root. `dotnet=/tmp/fhb-b-sdk/dotnet`, `editor-data=/tmp/fhb-b-unity/Editor/Data`, `packages=/tmp/fhb-b-unity/Editor/Data/Resources/PackageManager/ProjectTemplates/libcache/com.unity.template.3d-cross-platform-17.0.14/ScriptAssemblies`, `nunit=/tmp/04of-deps/nunit/lib/netstandard2.0/nunit.framework.dll`, `nunitlite=/tmp/04of-deps/lite/lib/netstandard2.0/nunitlite.dll`, `input-system=/tmp/04of-deps/input-build/Unity.InputSystem.dll`, `unity-references=/tmp/fhb-b-unity/Editor/Data/PlaybackEngines/LinuxStandaloneSupport/Variations/mono/Managed`.

- `python3 tools/check_04g_compile.py --root . --dotnet <dotnet> --editor-data <editor-data> --packages <packages> --nunit <nunit> --input-system <input-system> --output /tmp/04oh-h1-verified-api`
- `python3 tools/check_04g_assemblies.py --matrix /tmp/04oh-h1-verified-api --dotnet <dotnet> --output /tmp/04oh-h1-verified-assemblies`
- `python3 tools/check_visual_rebirth_04a_syntax.py --dotnet <dotnet> --report /tmp/04oh-h1-verified-syntax.json`
- `python3 tools/offline04f_tests.py --root . --dotnet <dotnet> --unity-references <unity-references> --nunit <nunit> --nunitlite <nunitlite> --assembly /tmp/04oh-h1-verified-api/player-classic-builtin.dll --output /tmp/04oh-h1-verified-tests --baseline-report Docs/Ascension04OH/Evidence/external-tests.json`
- Focused source run used `/tmp/04oh-h1-dev12-api/player-classic-builtin.dll`, output `/tmp/04oh-h1-focused-navigation`, filter `class == NeonSyndicate.Tests.VisualSlice04OHTests || class == NeonSyndicate.Tests.QuartzRegaliaTests`. The final frozen full suite executes those same 30 cases again.
- `python3 tools/measure_visual_slice_04oh.py --root . --dotnet <dotnet> --assembly /tmp/04oh-h1-verified-api/player-classic-builtin.dll --unity-references <unity-references> --output /tmp/04oh-h1-verified-world`
- `python3 tools/check_ascension_04oh.py --report /tmp/04oh-h1-verified-preservation.json`
- `python3 tools/check_living_frontier_04og.py --report /tmp/04oh-h1-verified-inherited.json`
- `python3 tools/check_unity_metadata_04c.py --report /tmp/04oh-h1-verified-metadata.json`
- `python3 tools/test_art_drop_04oh.py`
- `/tmp/04oh-validate-fbx Assets/NeonSyndicate/Art/ImportCalibration04OH/Calibration47.fbx` using the H0 pinned independent parser.
- `git lfs fsck`, `git diff --check` and staged whitespace checks.

## Defects found and fixed, retained in raw development evidence

The first full run had 1729 passes and 48 failures. Two new failures were unchanged Quartz Regalia budget tests: low detail 252 vertices versus less than 200, medium 5080 versus less than 4500. H1's sculpt is now selected only by the existing Quartz Drakeling appearance factory's visual-only tag. Wild drakes keep their old budget. All 12 original regalia cases pass without expectation changes; three new cases check separation and combat volumes.

Focused real-triangle tests found a lateral arch foot at a ramp mouth, a railing across a new floor bay and widened natural floor shoulders clipping a ramp approach. The actual structures, railing and tapered ground were corrected. Coat production was initially skipped by an overly conservative admission estimate; the reduced added mesh now really emits within both old budgets. The weighted-coat check was corrected to account for the existing W2 solver sorting the dominant influence first.

Broader retained-route checks demonstrated two generic neighborhood shortcuts through structural collision. Revision 7 now rejects generated shortcuts within the actual kit's structural envelope and removes their walking-graph links. Authored services/exploration routes and their anchor identities remain; bank revisions 0-6 are not modified. Default seed rejects seven incidental shortcuts. Source graph lengths are measured, with no compression claim.

The new geometry test helper needed bounding-box rejection and double-precision intersection arithmetic for nearly parallel faces at large world coordinates, retaining the original edge tolerance. These are fixes to newly authored tests; no pre-existing engine failure was reclassified. Earlier focused failures/messages remain in development-focused-history.json.

Three older source tests used 7 as an unknown-revision sentinel. Revision 7 is now intentionally valid. Only that sentinel changed to 99; old filename, protection and failure assertions remain. The source guard verifies those exact substitutions. All earlier checklist APIs/tests stay unchanged: 21 original rows, 25 H0 rows, 32 H1 panel rows.

No new exclusions, weakened Quartz budget, hidden native test failure, fabricated screenshot or inferred game performance result is used.
