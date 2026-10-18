# 04G verification ledger

## Source and access

Starting head: `2f8491681d7b3e45e72ab8e3a250c2c41bc568d3`. Preserved 04F: `747624335141a18031091f62737c73fe7f8969b7`. Art source is `f2eabb2c0fbc0500dea4cbf26ba3b701bc3b0979`; final gameplay source is `8bfdc355b92f96fd3f2a293b6e8a4f83ef358b8d` (companion cue ownership and event gaze fixes). C# snapshot: `2d34a59bcd343e9b84213196e4dae477ef4afe7db50634b9422bf38c79ab7a2f`. The final handoff commit adds docs, developer tools and evidence only.

Public clone of the existing 04G branch succeeded. Explicit LFS fetch/pull and fsck passed. All 437 required binary payload SHA-256 hashes matched. Authenticated GitHub tree/commit/ref publication succeeded; each remote tree matched the local Git index before its ref moved. Main and 04F remain at their original refs. No new LFS object is required.

## Executed results

| Check | Result | What it establishes |
|---|---|---|
| Managed source/API matrix | 12/12 PASS on the final source, and 12/12 on the starting checkpoint | Classic, both and input-only; Built-in and optional URP; player and Editor source. Windows symbols, genuine pinned Unity modules and .NET 8 BCL. This is not Unity import or a Windows build. |
| Diagnostic comparison | No introduced or removed warning messages | Same 28/26/28 warnings by input mode in each pipeline and source group. |
| External NUnit | 1,407 discovered; 1,354 executed; 1,308 PASS; 46 FAIL; 53 NeedsEngine excluded | The 46 failures have the exact starting-checkpoint identities and messages, all native ECall limitations. Raw NUnit exit 46; comparison wrapper exit 0. |
| Added 04G cases | 21/21 PASS | Defensive timing, maximum-charge buffering, finisher recovery, weapon-specific transitions, approach/turn/progression bounds, finite drake health, parcel drop/schema and actual skinning budgets. |
| Starting checkpoint NUnit | 1,287 PASS, 46 FAIL, 53 excluded | Executed with the same engine modules, matching NUnit framework and BCL. No new failure is hidden by relabeling it. |
| Roslyn syntax | 12/12 PASS, zero syntax errors | Parsing only, separate from API compilation. |
| Metadata and GUIDs | PASS: 1,631 sidecars; 13/13 audit regression cases; no missing/orphan/duplicate/malformed metadata or unresolved references | Existing GUID relationships retained; one TerritoryPlacement source. Native AssetDatabase import remains unvalidated. |
| Preservation | 6/6 PASS; 2,358 protected files exact | Existing assets, original quest/event JSON, metadata, packages and settings; unchanged SaveData 73, EventSave 13 and GuidanceSave 6 declarations; stable WeaponId order and unique catalog. |
| Actual asset geometry audits | Territory 290/290; venue 292/292 PASS | Numerical payload/room geometry, not native colliders or player traversal. |
| Actual character mesh generation | Seven deterministic baseline/current samples executed | Vertex/triangle/weight counts in VISUAL_EVIDENCE.md. No rendering or FPS result. |
| Independent Python models | Discovery 27/27 and fort 24/24 PASS | Historical Python reference models only, not the C# implementation. |
| Historical installer test discovery | Same 147 cases, 51 passes and 96 setup errors at baseline/current | CLI installer suites require separate release `--package`/`--baseline` fixtures. Unsupported unittest discovery gives `NameError: name 'PACKAGE' is not defined`. Release fixtures are not shipped in this repo; these are not passed installer or game tests. |
| Whitespace and tool syntax | `git diff --check` and developer-tool Python compilation PASS | Source/package hygiene. |

The separate Runtime/Editor/EditMode boundary check passes 3/3 on current and baseline source in the captured Editor/both/Built-in case. Its actual result is recorded in `Evidence/assembly-boundaries.json`; it remains an external metadata check, not the native asmdef importer.

## Exact dependency provenance

Unity metadata and Editor executable: official Linux Editor 6000.3.16f1 (a56f230f6470) archive. URP/Core checks use the compiled libraries bundled in that exact Editor's template cache. They do not prove that an optional URP package imports on the user's machine. The project package manifest and pipeline settings are unchanged.

Input System 1.17.0 was downloaded from the official Unity registry and built from 401 authentic source files against the genuine engine modules. InputForUI and optional UGUI support are omitted as in the previous verification tool, because this project has no UGUI package. The input package report records version, source hash, symbols and exit 0. No engine API stubs or source substitutions are used.

NUnit/NUnitLite 3.14.0 and Roslyn from .NET SDK 8.0.408 were used externally. Final baseline/current failures were compared with matching framework binaries. .NET BCL/API checking is not identical to Unity's compilation/import environment.

## Native attempt and exact failure

The exact Editor was launched:

```text
Unity -batchmode -nographics -projectPath <existing-repo> -runTests -testPlatform EditMode -testResults Logs/04G/baseline-native.xml -logFile Logs/04G/baseline-native.log
```

Observed errors before any test result:

```text
Failed to connect to LicenseClient-root channel (error code: 0x80000000)
Licensing initialization failed after 0.00s
```

No result XML appeared. The process was stopped after repeated licensing failures, exit 130. Native import/compile, Play Mode, screenshots, Windows build, controller input, physics, save/reload and Profiler work are **BLOCKED/UNVALIDATED**. The version query succeeding is not an import or playtest.

## Corrections during verification

- The first external runner mixed Unity's custom NUnit framework with a different runner binary and produced 216 failures, including MissingMethodException. Recompiling both baseline/current with matching NUnit 3.14.0 restored the 46 consistent native-only failures. That bad harness result is not a game regression or a passing test.
- The first Editor API harness accidentally combined monolithic compatibility DLLs with modular DLLs, causing duplicate-type errors. The harness now selects one consistent modular reference set. Game source was not changed to hide those errors.
- An assembly version check found the Editor template's Input System was 1.19. Those intermediate input-matrix results were superseded by the final official 1.17.0 build and matrix. Project packages were never upgraded.
- The first courier validation failed its 110-character objective limit and two exact content-count expectations. Text was shortened, and assertions now explicitly enumerate the three original 04F jobs plus the new courier, while retaining all original campaign IDs/counts.
- Review fixed emitter self-interception for a damageable event drake, mission-release cleanup for the parcel pose, and reset-only combat reference cleanup without clearing a live swing's hit ledger during control loss. A final cue review also moved and retained idle glances after steering and protected handler-owned UID 0 alert state. The final snapshot includes these fixes.

## Reproduce available checks

On Windows, use the exact native route in START_HERE_04G.md and `tools/test.ps1`. These developer-host commands reproduce the external checks when the genuine dependencies are available:

```bash
python3 tools/offline04f_build_input.py --dotnet "$DOTNET" --unity-references "$PLAYER_REFS" --package "$INPUT117_SOURCE" --output "$INPUT117_BUILD"
python3 tools/check_04g_compile.py --dotnet "$DOTNET" --editor-data "$EDITOR_DATA" --packages "$URP_TEMPLATE_ASSEMBLIES" --input-system "$INPUT117_BUILD/Unity.InputSystem.dll" --nunit "$NUNIT" --output Logs/04G/pinned-matrix
python3 tools/check_04g_assemblies.py --matrix Logs/04G/pinned-matrix --dotnet "$DOTNET" --output Logs/04G/separate-assemblies
python3 tools/offline04f_tests.py --root . --dotnet "$DOTNET" --unity-references "$PLAYER_REFS" --nunit "$NUNIT" --nunitlite "$NUNITLITE" --assembly Logs/04G/pinned-matrix/player-classic-builtin.dll --baseline-report "$BASELINE_REPORT" --output Logs/04G/tests
python3 tools/measure_04g_geometry.py --dotnet "$DOTNET" --assembly Logs/04G/pinned-matrix/player-classic-builtin.dll --unity-references "$PLAYER_REFS" --output Logs/04G/geometry
python3 tools/offline04f_preservation.py --baseline 2f8491681d7b3e45e72ab8e3a250c2c41bc568d3 --report Logs/04G/preservation.json
python3 tools/check_unity_metadata_04c.py --report Logs/04G/metadata.json
python3 tools/check_living_territories_04b_lfs.py --report Logs/04G/lfs.json
git diff --check
```

Expanded executed invocations, hashes and actual results are in Evidence/. Large DLLs and raw logs are intentionally not committed. The helper tools do not download runtime dependencies into the game or change packages/settings.

## Required acceptance, kept separate from executed pure checks

| # | Acceptance | Current evidence and native status |
|---:|---|---|
| 1 | Project preserved and imports | Preservation/metadata PASS; native import BLOCKED |
| 2 | Free Roam starts | Source integrated; NEEDS UNITY |
| 3 | Three distinct melee weapons | Timing/move data PASS; actual fight NEEDS UNITY |
| 4 | Dodge/parry/guard/buffering | Pure boundaries PASS; inputs and hit timing NEEDS UNITY |
| 5 | Crystal charge/impact/protection | Existing logic suite PASS; six live behaviors and physics NEEDS UNITY |
| 6 | Bounded observer-safe faction combat | Existing budget/ownership tests PASS; live placement and resolution NEEDS UNITY |
| 7 | Event drake motion/ownership | Health rules PASS; aiming, injury and reunion NEEDS UNITY |
| 8 | Bonded functions and progression | Existing pure bond/fusion suite retained; follow/wait/assist/rest/rescue/evolve/reload NEEDS UNITY |
| 9 | Meaningful normal city activity | Courier data/schema/drop tests PASS; complete normal route NEEDS UNITY |
| 10 | Quests and interactions | Full strict data validator PASS; actual prompts/controller parity NEEDS UNITY |
| 11 | Character/animation improvements | Actual mesh generation, weights and source integration PASS; comparable screenshots NEEDS UNITY |
| 12 | Old saves | Frozen fields/IDs/preservation PASS; native JsonUtility loading and gameplay state NEEDS UNITY |
| 13 | Temporary cleanup | Source hooks and existing lifecycle checks retained; abandon/session/reload behavior NEEDS UNITY |
| 14 | Honest performance evidence | Bounded source and actual mesh counts recorded; frame time/GC/FPS NOT MEASURED |
| 15 | 04G only, no main merge | GitHub publication and local/remote tree comparison PASS; PR remains draft targeting 04F |
