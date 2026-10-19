# Executed 04N verification

Exact tested gameplay source: `75ecf99cb765c80a19a7a8643b9c4c1566c3a7f6`, tree `5b45bf05b3527c197ab158be016aabc7454f47fd`, branch `feature/crystal-kingdom-awakens-04n`. C# input snapshot: `ad5d5ae7d0e026551239464a9f880ebe98e01f820f36988ff84a67c66779ab91`. Later evidence/documentation commits retain this source snapshot; the draft PR and final delivery response identify delivery HEAD.

The supplied 04M checkpoint was accepted once: `f9655c2c4373b6ef6efffee21f49f364e70ea765`. Its retained external result (1416 passes,46 failures,58 exclusions) is the comparison reference, not a fresh predecessor execution. No previous sprint was rerun as a new baseline.

| Executed check | Actual final result | Tracked evidence |
|---|---|---|
| Genuine Unity6000.3.16f1 managed API matrix |12/12 pass: player/editor × classic/both/input-only × Built-in/optional URP | Evidence/managed-api.json |
| Separate assembly compilation |3/3 pass: Runtime782, Editor10, EditMode122 source files | Evidence/assembly-boundaries.json |
| Full external NUnit |1582 discovered;1524 executed: **1478 pass,46 fail**;58 NeedsEngine exclusions | Evidence/external-tests.json and external-results.xml.gz |
| Failure comparison against accepted04M report |No introduced failure names, no changed reasons, no resolved native failures | external-tests.json comparison |
| New04N cases, extracted from final full XML |**62/62 pass**, no new exclusions | Evidence/new-tests.json |
| Roslyn C#9 syntax |12 configurations,0 syntax errors | Evidence/syntax.json |
| Unity metadata/GUID auditor |1722 sidecars;13 auditor regressions pass; no invalid/duplicate/missing/orphan/unresolved delivery references | Evidence/metadata.json |
| Current preservation/integration guard |48/48 pass | Evidence/preservation.json |
| Actual compiled geometry probe |24 OBJ exports; all topology indices/nonfinite checks pass; counts/bounds/SHA recorded | Evidence/geometry.json; four sample OBJs in Geometry/ |
| Exact inputs |All914 captured C# inputs equal source bytes; canonical digest reproduced; resource JSON hashes retained | Evidence/source-snapshot.json |
| Python/whitespace |Both04N tools py_compile and git diff --check pass | tools/check_crystal_kingdom_04n.py; tools/measure_crystal_kingdom_04n.py |
| GitHub source CI at exact tested gameplay source |Push39300328054 and PR39300333710 succeeded | Evidence/source-ci-observation.json |
| Native Unity / actual player / Windows build |**NOT RUN** | Evidence/native-status.json; NATIVE_ACCEPTANCE.md |

The raw NUnit outcome remains **Failed**, raw exit46. The comparison wrapper exits0 because all46 remaining failure names/reasons match the retained04M engine-dependent failures. They were not relabeled as exclusions. External NUnit is not Unity Test Runner, a PlayMode run, a rendered scene or proof of real player-save behavior.

## New executed cases

CrystalKingdom04NTests:14 architecture/hero/caster/ground/admission cases. ResonanceChains04NTests:18 recognition/resource/eligibility/mastery/prompt/lifecycle cases. KingdomEconomy04NTests:6 stock/equipment cases. KingdomWorld04NTests:12 route/puzzle/environment/boss/contract cases. KingdomContact04NTests:12 physical sweep/withdrawal cases. The final XML is authoritative if counts are reviewed later.

The tests exercise meaningful rules, geometry and generated content. Native callbacks, collision timing, rendering, device input and actual save files still require the acceptance checklist. A source integration guard is a wiring/preservation receipt, not proof of playing that feature.

## Corrections retained in the ledger

The first full consolidated run on `cc77ac587cee42e7e5324d0db6230278d3add354` produced1473 passes/47 failures. It introduced `QuestDataTests.ContractTemplatesGenerateValidRepeatableJobs`: a generated Warden title was54 characters, exceeding42/48-character UI limits. The actual failure/report is retained as Evidence/initial-title-failure.json. The four new title templates were shortened; the existing validator then passed. This failure was fixed, not excluded.

Review also tightened the hero admission guard to its actual480 added vertices, added zero-delta scene/death cancellation and positive partial-puzzle feedback. Final source was recaptured and compiled/tested after these changes. Early local probes caught a Cylinder API argument mismatch, missing UIKit anchors and unassigned sweep locals before successful checkpoints; these were corrected, not waived. A geometry distinctness assertion was corrected to compare actual vertex positions rather than assuming different shapes must have different vertex counts. The source-snapshot packaging probe was corrected to include the matrix's Editor inputs in its digest. None of those failed probe attempts is presented as a successful game test.

## Reproduce source checks

On this host (genuine installed managed references, no native launch):

```bash
python tools/check_04g_compile.py --root . --dotnet /tmp/04k-dotnet/dotnet --editor-data /tmp/04k-unity/Editor/Data --packages /tmp/04k-unity/Editor/Data/Resources/PackageManager/ProjectTemplates/libcache/com.unity.template.3d-cross-platform-17.0.14/ScriptAssemblies --nunit /tmp/04k-nunit/framework/lib/netstandard2.0/nunit.framework.dll --input-system /tmp/04k-input-build/Unity.InputSystem.dll --output /tmp/04n-api
python tools/check_04g_assemblies.py --matrix /tmp/04n-api --dotnet /tmp/04k-dotnet/dotnet --output /tmp/04n-assemblies
python tools/offline04f_tests.py --root . --dotnet /tmp/04k-dotnet/dotnet --unity-references /tmp/04k-unity/Editor/Data/PlaybackEngines/LinuxStandaloneSupport/Variations/mono/Managed --nunit /tmp/04k-nunit/framework/lib/netstandard2.0/nunit.framework.dll --nunitlite /tmp/04k-nunit/lite/lib/netstandard2.0/nunitlite.dll --assembly /tmp/04n-api/player-classic-builtin.dll --baseline-report Docs/LivingWorld04M/Evidence/external-tests.json --output /tmp/04n-tests
python tools/check_crystal_kingdom_04n.py --report /tmp/04n-preservation.json
python tools/check_unity_metadata_04c.py --report /tmp/04n-metadata.json
python tools/measure_crystal_kingdom_04n.py --dotnet /tmp/04k-dotnet/dotnet --unity-references /tmp/04k-unity/Editor/Data/PlaybackEngines/LinuxStandaloneSupport/Variations/mono/Managed --assembly /tmp/04n-api/player-classic-builtin.dll --output /tmp/04n-geometry
```

CI runs current source preservation, syntax, metadata and whitespace. It does not download/execute licensed Unity or replace the managed/native dispositions above. Existing binary LFS payloads were not rehashed for04N; the git preservation guard verifies that no original scene/art/prefab/GUID payload changed. Earlier LFS evidence remains predecessor evidence.
