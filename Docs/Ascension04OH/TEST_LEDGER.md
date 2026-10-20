# 04OH H0 executed verification

Exact tested source `0892ae385db04e5dd76c3c6d49b7e1ee4a28fb66`. C# SHA-256 `1caf39c56b463d0ce40499852e0a36b367ce296b5575876dd5a14ca0ff1df7cc`, verified against 981 files. Final handoff/evidence changes no game source or fixture data. Full raw XML, failures/messages, reference/invocation hashes and diagnostics are in Evidence.

| Executed check | Actual result | Limit |
|---|---|---|
| Genuine managed API matrix | 12/12 passed | Editor/player x Classic/Both/Input-only x Built-in/optional URP metadata, not Unity import |
| Independent assembly boundaries | 3/3 passed | Runtime 835, Editor 15, tests 131 files |
| C#9 syntax | 12/12, zero errors | Syntax only |
| External NUnit | 1716 passed, 46 failed, 58 excluded | Raw result Failed, exit 46; not native Test Runner |
| New H0 NUnit fixture | 28/28 passed | Inert type admission, rights, hashes, diagnostic refusal, authority and checklist compatibility |
| Focused compatibility run | 29/29 passed on compatibility checkpoint 880f241 | All 28 new cases plus unchanged earlier checklist test; executed again inside final full suite |
| Exact baseline comparison | Zero introduced names, changed reasons or resolved engine failures | Compared against 04OGCompletion actual report, not only totals |
| Exclusions | Same 58 NeedsEngine filter exclusions | No new exclusion or modified original engine-failure expectation |
| Read-only intake regressions | 6/6 passed | Classifications, byte hashes, script review, path traversal and symlink refusal; no execution/extraction |
| Strict independent FBX | Passed | Pinned C parser, skin weights, bind, actual clip deformation and recovery; NOT Unity |
| Preservation | 10 H0 + 15 inherited checks passed | Original gameplay/save/world/asset/render owners remain protected |
| Metadata/GUID | 1807 records, 13 auditor regressions passed | No invalid metadata/references; native AssetDatabase not run |
| LFS/whitespace | fsck OK, diff checks clean | Original data restored/hash-checked after workspace metadata pruning |
| GitHub source CI | Two runs passed on exact tested source | Guards/syntax/intake/metadata/independent FBX scope, not native or full API suite |
| Native/visual/performance/build | NOT RUN | Unchanged before-import IPC host; no repeated launch |

## Commands actually executed

From the repository root. Host paths: `dotnet=/tmp/fhb-b-sdk/dotnet`, `editor-data=/tmp/fhb-b-unity/Editor/Data`, `packages=/tmp/fhb-b-unity/Editor/Data/Resources/PackageManager/ProjectTemplates/libcache/com.unity.template.3d-cross-platform-17.0.14/ScriptAssemblies`, `nunit=/tmp/04of-deps/nunit/lib/netstandard2.0/nunit.framework.dll`, `nunitlite=/tmp/04of-deps/lite/lib/netstandard2.0/nunitlite.dll`, `input-system=/tmp/04of-deps/input-build/Unity.InputSystem.dll`, `unity-references=/tmp/fhb-b-unity/Editor/Data/PlaybackEngines/LinuxStandaloneSupport/Variations/mono/Managed`.

- `python3 tools/check_04g_compile.py --root . --dotnet <dotnet> --editor-data <editor-data> --packages <packages> --nunit <nunit> --input-system <input-system> --output /tmp/04oh-release-api`
- `python3 tools/check_04g_assemblies.py --matrix /tmp/04oh-release-api --dotnet <dotnet> --output /tmp/04oh-release-assemblies`
- `python3 tools/check_visual_rebirth_04a_syntax.py --dotnet <dotnet> --report /tmp/04oh-release-syntax.json`
- `python3 tools/offline04f_tests.py --root . --dotnet <dotnet> --unity-references <unity-references> --nunit <nunit> --nunitlite <nunitlite> --assembly /tmp/04oh-release-api/player-classic-builtin.dll --output /tmp/04oh-release-tests --baseline-report Docs/CityRebirth04O/Evidence04OGCompletion/external-tests.json`
- `python3 tools/check_ascension_04oh.py --report /tmp/04oh-release-preservation.json`
- `python3 tools/check_living_frontier_04og.py --report /tmp/04oh-release-inherited-preservation.json`
- `python3 tools/check_unity_metadata_04c.py --report /tmp/04oh-release-metadata.json`
- `python3 tools/test_art_drop_04oh.py`
- `python3 tools/inspect_art_drop_04oh.py ../upload/Crystal_Manor_Ascension_Complete_Production_Pack.zip --report /tmp/04oh-pack-inventory.json`
- `python3 tools/inspect_art_drop_04oh.py Assets --report /tmp/04oh-project-art-inventory.json`
- `python3 tools/generate_import_fixture_04oh.py --dotnet <dotnet> --assembly /tmp/04og-completion-api/player-classic-builtin.dll --unity-references <unity-references> --output /tmp/04oh-rig --fixture Assets/NeonSyndicate/Art/ImportCalibration04OH/Calibration47.fbx`
- Obtain the independent parser at exact source commit `5955c5c0b042ac2dc6f32955ab206aaec0620cfc` from its public source repository. `cc -O1 -I/tmp/04oh-ufbx-source tools/validate_import_fixture_04oh.c /tmp/04oh-ufbx-source/ufbx.c -lm -o /tmp/04oh-validate-fbx`, then `/tmp/04oh-validate-fbx Assets/NeonSyndicate/Art/ImportCalibration04OH/Calibration47.fbx`.
- `git lfs fsck`, `git diff --check` and the staged diff check.

The offline wrapper's comparison exit is zero because the same 46 engine failures remain. Its raw NUnit process still exits 46 and XML says Failed. The 58 existing exclusions were not changed. Source CI runs 39482831637 and 39482823479 passed on the tested source.

## Failures found and fixed

The first independent FBX run found incorrect bind matrices, max error about 1.679 m. The matrices were corrected, then strict validation passed at about 0.0000001304 m bind/recovery error with about 0.323 m actual clip displacement. The initial failure is retained in fixture-development-failure.json. These are mathematical checks, not precision/performance acceptance in Unity. An initial Python binding attempt exited 139; it is not counted as successful validation. The pinned standalone C parser provides the executed fixture proof.

The first full NUnit run had 1714 passes/47 failures. Its sole new failure was the original checklist API's 21-versus-25 count. The original method and test were preserved; a separate extended panel method and new prefix-preservation regression fixed compatibility. No original test expectation was weakened. The failed run/raw XML are retained as development evidence. Final source also rejects Animator controllers before cloning, preventing state-machine behaviours from bypassing the component check; the complete matrix and suite were rerun after that real change.

## Native and art boundary

No new native attempt, import, render, screenshot, profile, actual player-save/device result or Windows build was obtained. No professional models were imported. All 59 production art entries remain ASSET REQUIRED; the original diagnostic FBX and arm clip do not close those rows. See NATIVE_ACCEPTANCE.md and OWNER_GUIDE.md.
