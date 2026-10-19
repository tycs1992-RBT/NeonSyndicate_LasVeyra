# Executed verification and blocked acceptance

Branch `feature/the-veil-remembers-04l`; draft PR [#7](https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/pull/7) targets preserved 04K `df9072930596202b2a8f0fc23c9499f76faada6a`. Verified source `602033249ea2762606db9477799a5a336c5a706f`, tree `e9fc0a10122f7e31702d76bbd2cbf924236d225d`, C# snapshot `32a65f004999e1b4fc9f42eafb79b2770a954b924009dcebf5136e15ddb7e7ee`. The later docs/evidence delivery commit retains this C# snapshot; resolve final HEAD from PR #7 or git. **Partial production sprint; native-unverified.**

The actual source manifest contains 753 Runtime, 10 Editor and 115 EditMode C# files. All twelve API matrix rows compiled one immutable captured snapshot. No C# changed after that snapshot. Raw reports retain commands, diagnostic scope and hashes.

| Executed check | Actual result | Evidence |
|---|---|---|
| Fresh exact 04K API baseline | 12/12 genuine-metadata configurations pass; baseline hash a99cd4d640f73d0b257e5b59be8b96522776cff8dd46b7320aa631e2813c48b5 | baseline-api.json |
| Fresh 04K external NUnit | 1,358 pass, 46 engine-call failures, 53 NeedsEngine exclusions | baseline-tests.json; baseline-results.xml.gz |
| Final 04L managed API matrix | 12/12 pass, zero errors: player/editor × classic/both/input-only × Built-in/optional URP | managed-api.json |
| Runtime/Editor/Tests boundaries | 3/3 separately compiled assemblies pass | assembly-boundaries.json |
| Final external NUnit | 1,485 discovered, 1,431 executed: **1,385 pass, 46 fail**, 54 NeedsEngine exclusions; raw NUnit result Failed/exit46. Comparison wrapper exits0: no introduced failure names or changed reasons | external-tests.json; external-results.xml.gz |
| New tests | All 27 engine-independent cases pass. One native camera return test authored/compiled, excluded and NOT RUN | SagaDirection04LTests, CreatureTactics04LTests, VeilMemory04LTests, CinematicNative04LTests |
| Exact quest/event validator | All shipped references/schema pass, including new caretaker token and one explicitly listed optional ID | Included in external NUnit |
| Roslyn C#9 syntax | 12 configurations, zero syntax errors | syntax.json |
| Metadata/GUID | 1,683 sidecars coherent; 13 auditor regressions pass; no unresolved/invalid local references | metadata.json |
| Preservation / inherited guards | 16 current source checks; 22 04I / 11 04J / 11 04K checks pass | preservation.json; inherited-04i/j/k.json |
| LFS payloads | 1,733 SHA256 matches, 258,407,972 bytes, zero pointers/mismatches; git lfs fsck OK | lfs.json |
| Actual geometry/export | Existing actor/species/venue/fusion probe executed; eleven real weighted skins and three facial channels for Malik/Luma validated | geometry.json; art-export.json |
| Source CI | Successful at source checkpoint 6020332 | https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/actions/runs/39246126257 |
| Native baseline launch | Exact Unity 6000.3.16f1/a56f230f6470; preferences-directory warning repaired; launch still exit1, no XML, licensing IPC then UPM IPC | native-environment.json; native-editor.log |
| Whitespace / Python tools | git diff --check and Python py_compile pass | tracked source tools |

The 46 failures are the same native-engine-call cohort as the separately executed exact 04K baseline. They have not been hidden, marked passing or relabeled as Unity tests. One additional NeedsEngine exclusion is the new genuine camera regression. Old-save field/ID logic tests pass externally; no real player's copied save was loaded in Unity.

Two intermediate test runs exposed issues and were corrected before publication: the initial facial bound assumed fewer than100 emitted vertices but the actual feature contains144, so the bound was corrected to300 while retaining per-head-weight and displacement checks; the first optional quest used `Cast:bondkeeper` instead of canonical `Cast:caretaker`, and three exact catalog-count/list tests needed the one explicitly authorized additive ID. Their raw reports are retained as iteration-face-tests.json and iteration-quest-tests.json. No final failure was waived. An earlier facial edit also introduced an out-of-scope brow variable in the beard method; managed compilation caught it and it was removed before any checkpoint publication.

## Reproduce the executed external checks

These are paths on this host, not assumed paths on the user's PC. Genuine dependencies persist here from the verified 04K tooling: .NET8.0.408, exact Editor6000.3.16f1 metadata, officially pinned Input1.17.0 build, NUnit/NUnitLite3.13.3. Their provenance is retained in 04K and current JSON reports.

```bash
python tools/check_04g_compile.py --root . --dotnet /tmp/04k-dotnet/dotnet --editor-data /tmp/04k-unity/Editor/Data --packages /tmp/04k-unity/Editor/Data/Resources/PackageManager/ProjectTemplates/libcache/com.unity.template.3d-cross-platform-17.0.14/ScriptAssemblies --nunit /tmp/04k-nunit/framework/lib/netstandard2.0/nunit.framework.dll --input-system /tmp/04k-input-build/Unity.InputSystem.dll --output /tmp/04l-final-api
python tools/check_04g_assemblies.py --matrix /tmp/04l-final-api --dotnet /tmp/04k-dotnet/dotnet --output /tmp/04l-final-assemblies
python tools/offline04f_tests.py --root . --dotnet /tmp/04k-dotnet/dotnet --unity-references /tmp/04k-unity/Editor/Data/PlaybackEngines/LinuxStandaloneSupport/Variations/mono/Managed --nunit /tmp/04k-nunit/framework/lib/netstandard2.0/nunit.framework.dll --nunitlite /tmp/04k-nunit/lite/lib/netstandard2.0/nunitlite.dll --assembly /tmp/04l-final-api/player-classic-builtin.dll --baseline-report /tmp/04l-parent-tests-fresh/test-report.json --output /tmp/04l-final-tests
python tools/check_cinematic_04l.py --report /tmp/04l-preservation.json
python tools/check_unity_metadata_04c.py --report /tmp/04l-final-metadata.json
python tools/check_visual_rebirth_04a_syntax.py --dotnet /tmp/04k-dotnet/dotnet --report /tmp/04l-final-syntax.json
```

Raw NUnit XML is losslessly gzip-compressed in the evidence directory. A scoped documentation attribute rule stores these two reproducible reports as ordinary Git blobs while preserving all original asset/LFS rules. Decompress before opening with an NUnit viewer. Full meshes are reproducible with the tracked exporter; the evidence summary hashes the actual exported data and does not represent a render.

## Native truth

The Gate A attempt ran before 04L source edits, against the exact 04K baseline. Creating `/root/.local/share/unity3d` removed the old preferences warning. The first remaining observed error is `Failed to connect to LicenseClient-root channel (0x80000000)`, then licensing initialization failed. UPM could not connect to `Upm-7` after 30 seconds. The generic antivirus suggestion in Unity's log is not evidence that antivirus caused this host failure. There was no license bypass or new executable.

Native import/Console compilation, EditMode, PlayMode, actual material/skin render, physical loot pickup, all original mission routes, controller tests, copied old-save gameplay, measured adventure duration, frame-time/GC/draw/physics, Windows build and player launch are **NOT RUN/BLOCKED**. The current source has no native pass. Run the licensed path in START_HERE_04L before accepting gameplay, then fix the first real native failure.
