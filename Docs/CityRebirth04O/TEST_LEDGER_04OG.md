# 04OG exact-source test ledger

Tested source commit: `9fb3ece4fb0faeb4e2fdc7decad81099410d6ea7`. Source digest: `beac8bb15fc6f857a699700c64b0c53ff9f3757b9f2701dc1fb7ae43a0dca39e`. Snapshot digest was recomputed against the current 974 C# files and matched. Final delivery changes documentation/evidence only.

| Check | Actual result | Scope |
|---|---|---|
| Genuine managed API compilation | 12/12 pass | Editor/player x Classic/Both/Input-only x Built-in/URP, C#9, installed genuine Unity references |
| Assembly boundaries | 3/3 pass | Runtime 831 files, Editor 14, tests 129; no cross-assembly compile errors |
| Roslyn syntax | 12/12, zero errors | Syntax only |
| External NUnit | 1,673 pass / 46 fail / 58 filter exclusions | Raw outcome **Failed**, not a Unity Test Runner run |
| New 04OG NUnit cases | 26/26 pass | Ward, finite pressure, roles/cleanup, calm foraging, import scales, bank 6, five-seed physical triangles and quest JSON |
| Baseline comparison | No new failing names; no changed failure reasons; no resolved engine failures | Exact 46-name/message comparison against Evidence04OF/external-tests.json |
| Exclusion policy | Same unchanged runner/filter, 58 exclusions | No new exclusion or expectation for a real engine failure |
| Preservation guard | 12/12 pass | Original campaign/boot/rewards/entitlements/Forge/BONDS bytes; old asset/GUID bytes; only additive old JSON revision eligibility |
| Metadata audit | Pass; no duplicate GUIDs, missing metadata or unresolved serialized refs | 1,796 metadata records; native AssetDatabase NOT RUN |
| Metadata NUnit cases | 6/6 pass inside full suite | Pure GUID validation; an initial Python discovery matched zero and is not counted |
| Emitted geometry | Nine exports, no degenerate triangles or normal errors | Actual source geometry, not rendered images or FPS |
| Git LFS / whitespace | fsck OK / diff check clean | Existing LFS assets retained |
| Native Unity acceptance | NOT RUN | One Editor environment launch exited 1 before import at UPM IPC |

## Exact commands

Run from the repository root. On this host, `dotnet=/tmp/fhb-b-sdk/dotnet`, `editor-data=/tmp/fhb-b-unity/Editor/Data`, `packages=/tmp/fhb-b-unity/Editor/Data/Resources/PackageManager/ProjectTemplates/libcache/com.unity.template.3d-cross-platform-17.0.14/ScriptAssemblies`, `nunit=/tmp/04of-deps/nunit/lib/netstandard2.0/nunit.framework.dll`, `nunitlite=/tmp/04of-deps/lite/lib/netstandard2.0/nunitlite.dll`, `input-system=/tmp/04of-deps/input-build/Unity.InputSystem.dll`, and `unity-references=/tmp/fhb-b-unity/Editor/Data/PlaybackEngines/LinuxStandaloneSupport/Variations/mono/Managed`. Full invocations and exact reference hashes are retained in the reports.

- `python3 tools/check_04g_compile.py --root . --dotnet <dotnet> --editor-data <editor-data> --packages <packages> --nunit <nunit> --input-system <input-system> --output /tmp/04og-release-api`
- `python3 tools/check_04g_assemblies.py --matrix /tmp/04og-release-api --dotnet <dotnet> --output /tmp/04og-release-assemblies`
- `python3 tools/check_visual_rebirth_04a_syntax.py --dotnet <dotnet> --report /tmp/04og-release-syntax.json`
- `python3 tools/offline04f_tests.py --root . --dotnet <dotnet> --unity-references <unity-references> --nunit <nunit> --nunitlite <nunitlite> --assembly /tmp/04og-release-api/player-classic-builtin.dll --output /tmp/04og-release-tests --baseline-report Docs/CityRebirth04O/Evidence04OF/external-tests.json`
- `python3 tools/check_living_frontier_04og.py --report /tmp/04og-release-preservation.json`
- `python3 tools/check_unity_metadata_04c.py --report /tmp/04og-final-metadata.json`
- `python3 tools/measure_living_frontier_04og.py --root . --dotnet <dotnet> --assembly /tmp/04og-release-api/player-classic-builtin.dll --unity-references <unity-references> --output /tmp/04og-release-geometry`
- `git lfs fsck` and `git diff --check a6c86a574cd82e3fee0f4b2f48f922a81127ea24 HEAD`.

The offline wrapper exits zero only for the accepted unchanged engine-failure disposition. The raw NUnit process/XML still reports Failed. The original 46 ECall failures are listed with messages and stack traces in `Evidence04OG/external-tests.json` and the compressed raw XML; they were not suppressed.

## Development failures and legitimate expectation extensions

Early focused route runs failed real triangle clearance; approach routing, gallery-column inset, witness-wall inset and cavern root opening were corrected, without weakening collision admission or excluding seeds. The first comprehensive run had 1,672 passes / 47 failures. Its one new ECall failure was fixed by a non-inlined live-eligibility adapter, preserving the same test. The initial raw report is retained as development-full-tests.json.

Existing tests that said revision 6 was unknown now reject revision 7. Existing quest eligibility assertions append 6 to their original revision lists. Original expected data for revisions 0-5, payouts, IDs and engine failures were not changed. No test was newly excluded.

Five tested seeds: 61724, 1, 2016, 77231, 92017. Clearances inspect the actual new combined collision triangles at three body heights and three lanes, alongside actual floor faces, graph connectivity and old route prefix. These do not run PhysX, controller/companion movement, imported geometry or scene lighting.

## Evidence limits

Unity import/Console acceptance, native EditMode/PlayMode, actual player-save behavior, keyboard/controller gameplay, screenshots, visual acceptance, performance and Windows build/launch all remain NOT RUN. Source compilation and geometry exports prove neither a functioning native scene nor commercial visual quality.
