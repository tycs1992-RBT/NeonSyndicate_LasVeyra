# Crystal Combat and Living City 04F: test ledger

## Scope and baseline

Baseline is `4c690c6248f0ff7f00098598e6e42e409d70f84f`. A detached worktree at
`../baseline04f` preserves this revision for repeatable comparison. No baseline
or main branch was changed by verification.

Unity Editor, GPU rendering, Unity Test Runner, Play Mode, Windows build,
controller operation, collision behavior, screenshots and measured performance
are **NOT RUN**. `bash tools/test.sh` exits 3 with:

```text
TESTS NOT RUN: Unity 6000.3.16f1 was not found in /Applications/Unity/Hub/Editor.
```

Managed source checking uses genuine Unity `6000.3.16f1`, revision
`a56f230f6470`, modules from the official Linux IL2CPP support archive. It uses
Roslyn C# 9 and .NET 8 BCL references. This verifies source/API compatibility;
it is not Unity import or native compilation. No engine API stubs, source
substitutions, version changes or PhysicsMaterial aliases are used.

## Reusable commands

In this session `DOTNET` is `/tmp/fhb-dotnet/dotnet`; `REFS` is
`/tmp/crystal04f-toolchain/pinned/Editor/Data/PlaybackEngines/LinuxStandaloneSupport/Variations/linux64_player_development_il2cpp/Data/Managed`;
`NUNIT` and `NUNITLITE` are the corresponding DLLs under
`/tmp/crystal04f-toolchain/lib/netstandard2.0/`.

```bash
python tools/offline04f_build_input.py --dotnet "$DOTNET" --unity-references "$REFS" --package /tmp/crystal04f-toolchain/inputsystem/package --output /tmp/crystal04f-toolchain/input-build
python tools/offline04f_compile.py --root ../baseline04f --dotnet "$DOTNET" --unity-references "$REFS" --nunit "$NUNIT" --input-system /tmp/crystal04f-toolchain/input-build/Unity.InputSystem.dll --output Logs/04F/baseline-compile
python tools/offline04f_tests.py --root ../baseline04f --dotnet "$DOTNET" --unity-references "$REFS" --nunit "$NUNIT" --nunitlite "$NUNITLITE" --assembly Logs/04F/baseline-compile/player-classic-builtin.dll --output Logs/04F/baseline-tests
python tools/check_visual_rebirth_04a_syntax.py --dotnet "$DOTNET" --report Logs/04F/baseline-syntax.json
python tools/check_unity_metadata_04c.py --report Logs/04F/baseline-metadata.json
python tools/check_living_territories_04b_lfs.py --report Logs/04F/baseline-lfs.json
```

The Input System assembly is built from the unmodified official `1.17.0`
package, 401 source files. Optional UGUI support and its separate InputForUI
assembly are excluded according to package boundaries. The game does not
install UGUI. Source hashes and reference DLL hashes are recorded in reports.

## Executed baseline results

| Check | Result | Evidence |
|---|---|---|
| Genuine pinned managed API compile, player Built-in, classic/both/Input System only | 3 passed, 0 failed, 9 unavailable matrix rows skipped | `Logs/04F/baseline-compile/compile-report.json`; all 784 runtime/test source files included |
| Other compile matrix rows | 9 skipped | Matching UnityEditor and optional compiled URP/Core dependencies unavailable |
| Roslyn syntax across all twelve preprocessor configurations | 12 passed, 0 syntax errors | `Logs/04F/baseline-syntax.json`; syntax only |
| NUnit suite on .NET with genuine pinned Unity managed modules | 1,339 discovered; 1,286 executed; 1,240 passed, 46 failed; 53 NeedsEngine cases excluded | `Logs/04F/baseline-tests/nunit-results.xml`; all 46 failures reach native Unity ECall APIs |
| Metadata and serialized GUID audit | 1,590 sidecars; 13 audit regressions passed; no invalid, missing, orphan or duplicate metadata; one TerritoryPlacement | `Logs/04F/baseline-metadata.json` |
| Delivery LFS payload audit | 437 of 437 matched; 151,868,856 bytes | `Logs/04F/baseline-lfs.json`; actual payload SHA-256 values |
| 04A art audit with exact 04E comparison baseline | 1,888 of 1,889 checks passed | `../baseline04f/Logs/04F/baseline-art04a.json`; old reverse-patch fort-host expectation fails already at baseline |
| 04B authored assets | 290 of 290 passed | `../baseline04f/Logs/04F/source-batch/check_living_territories_04b_assets.py.json` |
| 04B ecology rules and conservative geometry | 100 rules plus 26 geometry checks passed | `../baseline04f/Logs/04F/baseline-ecology04b.json` |
| 04B ecology lifecycle source harness | 16 passed | `../baseline04f/Logs/04F/source-batch/check_ecology_lifecycle_04b.py.log` |
| 04C authored mesh geometry | 292 of 292 passed | `../baseline04f/Logs/04F/baseline-geometry04c.json`; native collision unverified |
| Creature reward exclusions | 12 current cases passed | `../baseline04f/Logs/04F/source-batch/check_creature_no_rewards.py.log` |
| Discovery reward Python model | 27 passed | `../baseline04f/Logs/04F/source-batch/test_crystal_discovery_02g_model.py.log`; model, not production C# |
| Stronghold Python model | 24 passed | `../baseline04f/Logs/04F/source-batch/test_strongholds_02i_model.py.log`; model, not production C# |

The first baseline NUnit failure is
`ArtDistrictStarterSelectionTests.AnchorsMatchExistingSpawnMathAndAreCached`:
`System.Security.SecurityException: ECall methods must be packaged into a system module.`
These 46 failures are retained, not represented as passing tests. The earlier
04E private source harness reported fewer native failures because its reference
assembly implementation differed. This sprint compares against the same
genuine-module baseline above.

Older 04B and 04C preservation scripts also fail at baseline because they freeze
an earlier sprint's host allowlist and require that sprint's single appended
save block. Their full logs are retained. The 04C resident harness fails at
baseline to compile because `ResidentReservations04E` is absent from its custom
host. The old resident-route export has 132 passes, one stale garage-anchor
failure, and four missing venue exports at baseline. These tools were not
changed to hide their stale assumptions.

Historical installer tests were not executed because their required package
and baseline ZIP fixtures are unavailable. Reproducibility regeneration was
not run because it rewrites artist-authored source assets.

## Reference provenance

- Official release metadata: `https://services.api.unity.com/unity/editor/release/v1/releases?version=6000.3.16f1&limit=1`
- Exact reference archive: `https://download.unity3d.com/download_unity/a56f230f6470/LinuxEditorTargetInstaller/UnitySetup-Linux-IL2CPP-Support-for-Editor-6000.3.16f1.tar.xz`
- Archive size 59,129,484 bytes, SHA-256 `57f31938aaddbd8778f3cacc0c3be827f899cf520753d78c87680e1598fe5436`.
- Official Input System package: `https://packages.unity.com/com.unity.inputsystem/-/com.unity.inputsystem-1.17.0.tgz`
- Package SHA-256 `493b4891a4617d092865b0cb1d3aee1ce60c71f9a89a816e7866ed8136b5e91e`.
- NUnit and NUnitLite `3.14.0` obtained from the NuGet flat container. No packages added to the Unity project.

## Final working-source results

Final C# source snapshot SHA-256:
`fe56fd93575b420a7dbf7a4efde95eb99befa7843643886307e847f4fb3ab6a7`.
The compiler captured immutable copies before checking the matrix, so every row
uses the same bytes. This snapshot includes the final NPC bolt saturation,
start-overlap and session cleanup fixes, plus the policy-aware legacy tests.

| Check | Final result | Delivered evidence |
|---|---|---|
| Genuine pinned managed compile | 3 passed, 0 failed, 9 unavailable rows skipped; 811 runtime/test source files | `Evidence/managed-compile.json` |
| Compiler diagnostics compared with baseline | No introduced or removed warning/error messages in any executed row; warning counts remain 28, 26 and 28 | `Evidence/diagnostic-delta.json` |
| Full NUnit on .NET | 1,384 discovered; 1,331 executed; **1,285 passed, 46 failed**, 53 NeedsEngine cases excluded | `Evidence/nunit-delta.json` |
| NUnit regression comparison | **0 new failure identities, 0 changed failure reasons**; all 46 native ECall failures match baseline | `Evidence/nunit-delta.json` |
| New 04F suites | **45 of 45 passed**: combat 16, city 11, character art 11, guidance 7 | `Evidence/nunit-delta.json` |
| Twelve-way syntax parsing | 12 passed, 0 errors | `Evidence/syntax-matrix.json` |
| Unity metadata | 1,621 sidecars; 13 regressions passed; no malformed, missing, duplicate or orphan metadata; one TerritoryPlacement | `Evidence/metadata.json` |
| Required delivery LFS payloads | 437 of 437 SHA-256 matched; 151,868,856 bytes; no new binary objects introduced | `Evidence/lfs.json` |
| 04E preservation | 6 of 6 checks passed; 2,323 pre-existing protected files exact; saved WeaponId and unique catalog exact | `Evidence/preservation.json` |
| Save declarations | SaveData 72 to 73, EventSave 10 to 13, GuidanceSave 5 to 6; all existing declarations retained | `Evidence/preservation.json` |
| Whitespace and Python tooling | `git diff --check` and `python -m py_compile tools/offline04f_*.py` passed | Command evidence and source tools |
| Native projectile wall/overlap/session behavior | **NOT RUN**; managed API compile passed | Requires Unity physics and Play Mode |

The final NUnit run executed from `2026-10-18T03:07:03Z` to
`2026-10-18T03:07:20Z`, using the same genuine Unity module set as baseline.
The raw NUnit process returns 46 because the engine-dependent failures remain.
The baseline-comparison wrapper returns 0 because there are no new or changed
failures. This is not an all-green native test result.

Early integration runs identified outdated assumptions that every gun uses
ammunition, that every ordinary loot record remains an ammo weapon, and that
EventSave has only ten fields. The updated assertions preserve inert owned
ammo, validate the shared charge route and append-only save fields, and retain
the saved generator's original numeric reference. The final arsenal-card
assertion checks converted live statistics while preserving the original
record's unique index. No baseline native failures were hidden or relabeled.

Executed final commands use the same DOTNET, REFS, NUNIT and NUNITLITE paths
defined above:

```bash
python tools/offline04f_compile.py --dotnet "$DOTNET" --unity-references "$REFS" --nunit "$NUNIT" --input-system /tmp/crystal04f-toolchain/input-build/Unity.InputSystem.dll --output Logs/04F/final-compile
python tools/offline04f_tests.py --root /workspace/scratch/a318934c25ce/game --dotnet "$DOTNET" --unity-references "$REFS" --nunit "$NUNIT" --nunitlite "$NUNITLITE" --assembly Logs/04F/final-compile/player-classic-builtin.dll --baseline-report Logs/04F/baseline-tests/test-report.json --output Logs/04F/final-tests
python tools/offline04f_preservation.py --report Logs/04F/final-preservation.json
python tools/check_unity_metadata_04c.py --report Logs/04F/final-metadata.json
python tools/check_living_territories_04b_lfs.py --report Logs/04F/final-lfs.json
python tools/check_visual_rebirth_04a_syntax.py --dotnet "$DOTNET" --report Logs/04F/final-syntax.json
python tools/offline04f_evidence.py
git diff --check
```

`Evidence/executed-commands.json` records expanded invocations and exit codes.
The eight evidence JSON files total about 33 KB. Raw compiler/test logs and
the NUnit XML remain under ignored `Logs/04F/`; no DLLs are shipped as evidence.

## Required native acceptance

Use Unity `6000.3.16f1` and `tools/test.ps1`, then run Free Roam and the matching
04E baseline at the same resolution and graphics preset.

1. Identify the 04F revision label, starter blade/focus and nearby quest route.
2. Test light chain, heavy, guard/parry, counters and every supported controller action.
3. Drain charge, fight with melee, refill from legitimate enemies and a station; verify no farming civilians/dummies.
4. Observe a finite autonomous faction fight and NPC drake without provoking unwanted player pursuit.
5. Enter each added quest venue, complete the indoor objective, leave, abandon and reload safely.
6. Complete the original drakeling rescue or verify existing bond and fusion ownership survive reload.
7. Test old-save conversion, once-only encounter rewards, save/exit/load and remaining original campaign routes.
8. Capture actual Unity player, warden, drake, weapon and room screenshots. Compare against the supplied references.
9. Measure frame time, memory and peak actors on Low/Medium/High/Ultra. No frame rate result is claimed here.
