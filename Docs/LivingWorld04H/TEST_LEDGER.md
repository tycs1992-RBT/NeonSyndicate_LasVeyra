# 04H test ledger

## Exact source and genuine baseline

Baseline 04G: `86a6c05d62a7f59b0c97e72cd3c2462d16202a04`, freshly checked out and executed in a detached comparison worktree. Final implemented gameplay source: `afd755e5c802dcc7c0ad4ad80d3854767bba11bb`, tree `074c036b1f14ecfac38f08a25be0eba6d77c6d58`. C# snapshot SHA-256: `e6a24f3d76de1e0cdc256eee545ed3ac7ef30461f5798a1431d5ccdc17c774ab`. All final managed configurations use one captured immutable snapshot: 723 runtime, 10 Editor and 105 EditMode C# files. The published source was checked against that digest before publication. The following handoff commit adds documentation/evidence/tools only.

The baseline numbers below were actually re-executed. They were not copied from 04G's report.

| Executed check | Baseline/current result | Scope |
|---|---|---|
| Managed API configurations | 12/12 baseline, 12/12 final PASS | Player/Editor x classic/both/input-only x Built-in/URP, using genuine engine metadata and pinned Input System 1.17.0; not a Unity import/build |
| Diagnostic comparison | Zero introduced/removed warning messages in all 12 | Same 28 classic, 26 both, 28 input-only warnings in each source/pipeline group |
| Baseline NUnit | 1,407 discovered; 1,354 executed; 1,308 passed; 46 failed; 53 NeedsEngine excluded | Matching external NUnit/NUnitLite and engine modules |
| Final NUnit | 1,420 discovered; 1,367 executed; 1,321 passed; 46 failed; 53 NeedsEngine excluded | Same 46 failure identities and messages; raw NUnit exit 46; comparison-wrapper exit 0 means no new failure, not an all-green native suite |
| New 04H cases | 13/13 executed and passed | Three weapon interval-partition cases, contact reach/distance, drake retreat/recovery, rhythm bounds, recipe costs/unlock, four preset facade bounds, hero normals/weights/budget |
| Assembly boundaries | Runtime, Editor, EditMode: 3/3 PASS | Separate assemblies in Editor/both/Built-in; external managed check, not native asmdef import |
| Roslyn syntax | 12/12 PASS, zero syntax errors | Parsing only; separate from API compilation |
| Metadata/GUIDs | 1,640 sidecars valid; 13/13 auditor regressions PASS | No malformed, duplicate, missing, orphan or unresolved metadata/reference; one TerritoryPlacement retained |
| Preservation | 6/6 PASS; 2,369 old protected files exact | Existing assets/meta/original quest/event JSON/settings/packages, SaveData 73, EventSave 13 and GuidanceSave 6 declarations, WeaponId order and unique catalog |
| Git LFS | 437/437 required actual binary SHA-256 payloads matched; 151,868,856 bytes checked | Explicit fetch/pull and fsck succeeded. No new binary/LFS object introduced |
| Actual generated geometry | Seven actor samples and eight preset/LOD facade samples executed | See VISUAL_EVIDENCE and geometry.json; no rendering or FPS inference |
| Existing asset geometry | Territory 290/290; venue 292/292 PASS | Numerical asset/geometry audits; not actual player traversal/collision acceptance |
| Independent Python models | Discovery 27/27; forts 24/24 PASS | Historical reference models, not execution of the C# runtime |
| Hygiene | git diff --check and Python tool compilation PASS | No source/package/pipeline migration |

Full commands, source hashes, dependency hashes, test counts and unchanged native failures are retained in Evidence/. Large DLLs and raw full-run logs remain in the ignored Logs/04H workspace; no claim depends on a screenshot that was not captured.

## Native attempt

The installed official Editor is 6000.3.16f1, revision a56f230f6470. One 04H attempt launched:

```text
Unity -batchmode -nographics -projectPath <existing-project> -runTests -testPlatform EditMode -testResults Logs/04H/native.xml -logFile Logs/04H/native.log
```

It failed before import/tests:

```text
Failed to connect to LicenseClient-root channel (error code: 0x80000000)
Licensing initialization failed after 0.00s
```

Observed process exit was 1; no result XML was created. The attempt was stopped after the repeated licensing diagnostics. This is a licensing/IPC failure, not a demonstrated game C# error. No licensing bypass was attempted.

Native Unity import, scene startup, Play Mode, physics, actual save/reload, input devices, screenshots, Windows build and Profiler measurements are **BLOCKED/NOT EXECUTED**. The Editor version existing and the managed matrix passing do not establish a playable build.

## Demonstrated failures and corrections

1. Initial art regression: one dense standard outfit produced 8,282 vertices against its unchanged 8,200 ceiling. Optional accessory admission was bounded; final original body tests pass. The initial failed run remains in intermediate-findings.json.
2. An intermediate combat test assembly ran while the working-tree content was being expanded. Its compiled schema preceded the new Attune JSON, causing three content-count/schema failures plus the known outfit failure. This mixed snapshot was superseded; the final suite and final JSON agree, and all data validation passes. It is not counted as a clean release run.
3. Integrated review: The Quiet Circuit initially paid 220 cash against the established 200 cap. Payment was reduced to 180. Existing exact job-count and sprint-label expectations were updated for the one additive job and 04H; the economy limit itself was retained. Final rerun passes those cases.
4. Source review: a hidden Weaver could retain an unfinished windup. Explicit cancellation now starts a fresh exposed tell without resetting volley/cooldown budgets. General NPC CancelActions also cancels the caster. Native timing still needs observation.
5. The new geometry probe initially had an ambiguous Random reference after adding UnityEngine. It was corrected to System.Random. Actual mesh probes then executed successfully; no engine stub or source substitution was used.

No failing game test was moved into NeedsEngine or skipped to improve the reported total.

## Dependency provenance and reproduction

External checks use .NET SDK 8.0.408/Roslyn, genuine modules from the official Unity 6000.3.16f1 Linux Editor, the genuine URP/Core assemblies bundled with that Editor's template and matching NUnit/NUnitLite 3.14.0. The project still pins Input System 1.17.0; checks use the official 401-source-file package build from 04G, not the template's different Input System assembly. The 04G ledger/evidence preserves its source/build provenance. No engine API stubs or imported replacement game project are used.

On Windows use START_HERE_04H.md and tools/test.ps1 for actual native tests. Developer-host commands, with genuine local dependency paths supplied:

```bash
python3 tools/check_04g_compile.py --root <checkout> --dotnet "$DOTNET" --editor-data "$EDITOR_DATA" --packages "$URP_TEMPLATE_ASSEMBLIES" --input-system "$INPUT117_DLL" --nunit "$NUNIT" --output Logs/04H/verified-matrix
python3 tools/check_04g_assemblies.py --matrix Logs/04H/verified-matrix --dotnet "$DOTNET" --output Logs/04H/verified-assemblies
python3 tools/offline04f_tests.py --root . --dotnet "$DOTNET" --unity-references "$PLAYER_REFS" --nunit "$NUNIT" --nunitlite "$NUNITLITE" --assembly Logs/04H/verified-matrix/player-classic-builtin.dll --baseline-report <executed-baseline-report> --output Logs/04H/verified-tests
python3 tools/measure_04h_geometry.py --dotnet "$DOTNET" --assembly Logs/04H/verified-matrix/player-classic-builtin.dll --unity-references "$PLAYER_REFS" --output Logs/04H/verified-geometry
python3 tools/check_visual_rebirth_04a_syntax.py --dotnet "$DOTNET" --report Logs/04H/verified-syntax.json
python3 tools/check_unity_metadata_04c.py --report Logs/04H/verified-metadata.json
python3 tools/offline04f_preservation.py --baseline 86a6c05d62a7f59b0c97e72cd3c2462d16202a04 --report Logs/04H/verified-preservation.json
python3 tools/check_living_territories_04b_lfs.py --report Logs/04H/lfs.json
python3 tools/check_living_territories_04b_assets.py --report Logs/04H/territory-assets.json
python3 tools/check_inhabited_city_04c_geometry.py --report Logs/04H/venue-geometry.json
python3 tools/test_crystal_discovery_02g_model.py --report Logs/04H/discovery-model.json
python3 tools/test_strongholds_02i_model.py --report Logs/04H/fort-model.json
git lfs fetch origin feature/living-world-renaissance-04h
git lfs pull
git lfs fsck
git diff --check
```

The historical installer tests require separate release package/baseline fixtures absent from this repository. They are not a configured game test target in this environment and were not claimed passed. The previous 04G unconfigured-discovery errors remain documented there.

## Native acceptance matrix

| Area | Executed external evidence | Required native acceptance, not executed |
|---|---|---|
| Combat | Existing timing/resource suite, new swept contact/partition math, API matrix | Three weapons, buffering, dodge/parry/guard, stamina, wall contact, pause/swap/hitches, prop single hit |
| Focuses | Existing six-signature pure rules retained; query/API checks | Six live behaviors, initial overlap, busy-door saturation, protected actor, disc return, charge/heat/cancel |
| Companions | Existing bond/fusion/progression suite, bounded drake health rules | Follow/wait/recall/rest/rescue/evolve/assist, tangents, stairs, ceilings, fusion and vehicle transitions |
| NPCs/events | Existing ownership/budget rules; source-integrated cover/drake decisions | Observer safety, finite endings, cover/peek telegraphs, injuries, rescue/reunion and cleanup |
| RPG/content | All shipped strict data, recipe/economy checks, stable IDs/declarations | Finish Quiet Circuit and 04G courier; consumption/unlock/reward exactly once; old/new saves |
| World/visual | Metadata, payload hashes, actual generated mesh bounds/weights and asset audits | Import/materials/skinning, daytime/night screenshots, all presets, doors/interiors/surface/underground |
| Presentation/traversal | API and source checks; original vehicles preserved | Camera wall/vehicle transitions, controller labels, reduced effects/shake, audio synchronization |
| Recovery/performance | Explicit cleanup owners, bounded entities/queries and geometry counts | Death/abandon/menu/load during every new action; Profiler frame time, GC, physics, rendering and actor counts |

There is no measured FPS, allocation reduction, rendered before/after image or shipping-readiness claim. New initial-overlap checks add physics work; contact and cover also need real profiling on the same scene/settings.
