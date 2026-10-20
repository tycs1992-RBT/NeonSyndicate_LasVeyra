# 04OG exact-source test ledger

Tested source commit: `138451d52c66fc9f3d3d67d9767ff204561e5fb1`. Source digest: `2c74bb39149bed212154e1900be9a89d063365beee4f867739de49657fdb504b`. Snapshot digest was recomputed against the current 978 C# files and matched. Final delivery changes documentation/evidence only.

| Check | Actual result | Scope |
|---|---|---|
| Genuine managed API compilation | 12/12 pass | Editor/player x Classic/Both/Input-only x Built-in/URP, C#9, installed genuine Unity references |
| Assembly boundaries | 3/3 pass | Runtime 834 files, Editor 14, tests 130; no cross-assembly compile errors |
| Roslyn syntax | 12/12, zero errors | Syntax only |
| External NUnit | 1,688 pass / 46 fail / 58 filter exclusions | Raw outcome **Failed**, not a Unity Test Runner run |
| New 04OG NUnit cases | 41/41 pass | Ward, finite pressure, roles/cleanup, calm foraging, import scales, bank 6, five-seed physical triangles and quest JSON |
| Baseline comparison | No new failing names; no changed failure reasons; no resolved engine failures | Exact 46-name/message comparison against Evidence04OG/external-tests.json (the same inherited 04OF failures) |
| Exclusion policy | Same unchanged runner/filter, 58 exclusions | No new exclusion or expectation for a real engine failure |
| Preservation guard | 15/15 pass | Original campaign/boot/rewards/entitlements/Forge/BONDS bytes; old asset/GUID bytes; only additive old JSON revision eligibility |
| Metadata audit | Pass; no duplicate GUIDs, missing metadata or unresolved serialized refs | 1,800 metadata records; native AssetDatabase NOT RUN |
| Metadata NUnit cases | 6/6 pass inside full suite | Pure GUID validation; an initial Python discovery matched zero and is not counted |
| Emitted geometry | Nine exports, no degenerate triangles or normal errors | Actual source geometry, not rendered images or FPS |
| Git LFS / whitespace | fsck OK / diff check clean | Existing LFS assets retained |
| Native Unity acceptance | NOT RUN | One Editor environment launch exited 1 before import at UPM IPC |

## Exact commands

Run from the repository root. On this host, `dotnet=/tmp/fhb-b-sdk/dotnet`, `editor-data=/tmp/fhb-b-unity/Editor/Data`, `packages=/tmp/fhb-b-unity/Editor/Data/Resources/PackageManager/ProjectTemplates/libcache/com.unity.template.3d-cross-platform-17.0.14/ScriptAssemblies`, `nunit=/tmp/04of-deps/nunit/lib/netstandard2.0/nunit.framework.dll`, `nunitlite=/tmp/04of-deps/lite/lib/netstandard2.0/nunitlite.dll`, `input-system=/tmp/04of-deps/input-build/Unity.InputSystem.dll`, and `unity-references=/tmp/fhb-b-unity/Editor/Data/PlaybackEngines/LinuxStandaloneSupport/Variations/mono/Managed`. Full invocations and exact reference hashes are retained in the reports.

- `python3 tools/check_04g_compile.py --root . --dotnet <dotnet> --editor-data <editor-data> --packages <packages> --nunit <nunit> --input-system <input-system> --output /tmp/04og-completion-api`
- `python3 tools/check_04g_assemblies.py --matrix /tmp/04og-completion-api --dotnet <dotnet> --output /tmp/04og-completion-assemblies`
- `python3 tools/check_visual_rebirth_04a_syntax.py --dotnet <dotnet> --report /tmp/04og-completion-syntax.json`
- `python3 tools/offline04f_tests.py --root . --dotnet <dotnet> --unity-references <unity-references> --nunit <nunit> --nunitlite <nunitlite> --assembly /tmp/04og-completion-api/player-classic-builtin.dll --output /tmp/04og-completion-tests --baseline-report Docs/CityRebirth04O/Evidence04OG/external-tests.json`
- `python3 tools/check_living_frontier_04og.py --report /tmp/04og-completion-preservation.json`
- `python3 tools/check_unity_metadata_04c.py --report /tmp/04og-completion-metadata.json`
- `python3 tools/measure_living_frontier_04og.py --root . --dotnet <dotnet> --assembly /tmp/04og-completion-api/player-classic-builtin.dll --unity-references <unity-references> --output /tmp/04og-completion-geometry`
- `git lfs fsck` and `git diff --check a6c86a574cd82e3fee0f4b2f48f922a81127ea24 HEAD`.

The offline wrapper exits zero only for the accepted unchanged engine-failure disposition. The raw NUnit process/XML still reports Failed. The original 46 ECall failures are listed with messages and stack traces in `Evidence04OGCompletion/external-tests.json` and its compressed raw XML; they were not suppressed.

## Development failures and legitimate expectation extensions

Early focused route runs failed real triangle clearance; approach routing, gallery-column inset, witness-wall inset and cavern root opening were corrected, without weakening collision admission or excluding seeds. The first comprehensive run had 1,672 passes / 47 failures. Its one new ECall failure was fixed by a non-inlined live-eligibility adapter, preserving the same test. The initial raw report is retained as development-full-tests.json.

Existing tests that said revision 6 was unknown now reject revision 7. Existing quest eligibility assertions append 6 to their original revision lists. Original expected data for revisions 0-5, payouts, IDs and engine failures were not changed. No test was newly excluded.

Five tested seeds: 61724, 1, 2016, 77231, 92017. Clearances inspect the actual new combined collision triangles at three body heights and three lanes, alongside actual floor faces, graph connectivity and old route prefix. These do not run PhysX, controller/companion movement, imported geometry or scene lighting.

## Evidence limits

Unity import/Console acceptance, native EditMode/PlayMode, actual player-save behavior, keyboard/controller gameplay, screenshots, visual acceptance, performance and Windows build/launch all remain NOT RUN. Source compilation and geometry exports prove neither a functioning native scene nor commercial visual quality.

## Recovered continuation results

Prior accepted delivery `0d2f4e4` was clean and matched the actual remote HEAD once. Continued source checkpoint `138451d52c66fc9f3d3d67d9767ff204561e5fb1` is the exact source tested above. All reports are in `Evidence04OGCompletion`; prior source/evidence remains in `Evidence04OG`. No game source changes follow this source checkpoint in the final delivery.

The new 15-case FrontierContinuation04OGTests fixture passed both focused and full execution. It tests actual guardian/skimmer bones during regional arts and after recovery, bounded travel/return with persistent danger/weather, actual nearby mineral destination clearance, five real/elevated map targets across five seeds, finite/distinct periodic sound PCM and physical audio reach. The original 26 first-checkpoint 04OG cases also passed, total 41. Full suite: 1,792 discovered, 1,734 executed, 1,688 passed, 46 failed, 58 excluded by the unchanged NeedsEngine filter. Raw exit 46 and XML result Failed; the exact-name/message comparison exit is zero. There are zero introduced failing names, zero changed failure reasons and zero resolved engine failures.

All 12 API configurations use the same captured source digest. The independent assembly check compiled 834 runtime, 14 Editor and 130 test files separately. The metadata auditor checked 1,800 sidecars and all 13 auditor regression cases. Fifteen preservation/owner checks passed. Regenerated nine mesh exports and the complete world manifest matched the prior delivery's source world: this continuation changes no collision or coordinates. These are not native geometry/render tests. GitHub source CI runs 39470070679 and 39470060327 passed on the tested source.

Additional focused command: `python3 tools/offline04f_tests.py --root . --dotnet <dotnet> --unity-references <unity-references> --nunit <nunit> --nunitlite <nunitlite> --assembly /tmp/04og-completion-focus-api/player-classic-builtin.dll --output /tmp/04og-continuation-focused --where 'class == NeonSyndicate.Tests.FrontierContinuation04OGTests'`.

No new Unity launch was attempted on the unchanged blocked host. Native execution, visuals, actual save/load/device behavior, audio mix, performance and Windows build remain NOT RUN. Professional models imported: zero. No native success is inferred from CI or managed assertions.
