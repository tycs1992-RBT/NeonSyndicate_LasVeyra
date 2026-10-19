# Exact verification and limits

Source checkpoint `7f4e0326f13858acbb34306df2be1a60ce4c527e`, C# snapshot `a99cd4d640f73d0b257e5b59be8b96522776cff8dd46b7320aa631e2813c48b5`; 740 runtime, 10 Editor and 111 EditMode source files. Subsequent evidence/docs/CI changes do not change this C# snapshot.

| Executed check | Actual result | Evidence |
|---|---|---|
| Fresh 04J baseline genuine-metadata compile | One player/classic/Built-in pass; other 11 not requested in this baseline command | baseline-api.json |
| Exact 04K genuine-metadata compile | 12/12 pass, zero errors, exact Editor modules; official Input System 1.17.0 sources independently compiled | managed-api.json, input117-build.json |
| Separate Runtime/Editor/Test assemblies | 3/3 pass | assembly-boundaries.json |
| Roslyn C# 9 syntax | 12/12 pass | syntax.json |
| 04J external NUnit, hydrated assets | 1,350 pass, 47 failures, 53 NeedsEngine exclusions | baseline-tests.json, baseline-results.xml |
| 04K external NUnit | 1,358 pass, 46 unchanged native-engine failures, 53 exclusions; all seven new cases pass; no introduced failures or changed failure reasons | external-tests.json, external-results.xml |
| Inherited failing source case | Corrected the false empty-Mythic-pool assumption; actual cap/type/save protection test passes | LootDropTests + comparison |
| Metadata/GUID | 1,665 sidecars coherent; 13 auditor regression tests pass | metadata.json |
| Original 04I/04J source guards | 22 / 11 pass | inherited-04i.json, inherited-04j.json |
| Current 04K source/preservation guard | 11 pass | preservation.json |
| LFS working payloads and fsck | 1,733/1,733 actual SHA256 matches, zero pointers/mismatches; fsck OK | lfs.json |
| Actual compiled geometry / skin exports | Actor/species/venue/fusion probe executed; 10 skin exports structurally checked | geometry.json, art-export.json |
| Python exporter / Blender importer syntax | Both py_compile pass; exporter compiled/executed; Blender absent | tools + art-export.json |
| Whitespace | git diff --check passes | final branch/source guard |
| Native Editor launch | Exact 6000.3.16f1/a56f230f6470, one bounded attempt, exit 1, no XML | native-environment.json, native-editor.log |

All Evidence paths above are relative to `Evidence/`. External NUnit ran on .NET 8 with genuine Unity managed assemblies and NUnit/NUnitLite 3.13.3. It is **not Unity Test Runner**. The raw final NUnit result is Failed/exit 46; the no-new-regression comparison exits 0. Do not describe it as all native tests passing. The initial unhydrated baseline run was discarded as the comparator, hydrated and rerun; the final comparison above uses only the hydrated baseline.

First native blocker: `Failed to connect to LicenseClient-root channel (0x80000000)` and licensing initialization failure. Package Manager also failed IPC after 30 seconds. Archive extraction completed with 14 GB free; no current disk exhaustion. No licensing bypass, repeated install, native import pass, PlayMode, Windows build/launch, copied old-save gameplay, screenshot, Profiler or timed adventure. The Editor attempt preceded final source corrections and failed before import; final source remains native-unverified.

## Reproduction commands actually used

`tools/check_04g_compile.py --root . --dotnet /tmp/04k-dotnet/dotnet --editor-data /tmp/04k-unity/Editor/Data --packages /tmp/04k-unity/Editor/Data/Resources/PackageManager/ProjectTemplates/libcache/com.unity.template.3d-cross-platform-17.0.14/ScriptAssemblies --nunit /tmp/04k-nunit/framework/lib/netstandard2.0/nunit.framework.dll --input-system /tmp/04k-input-build/Unity.InputSystem.dll --output /tmp/04k-release-api`

`tools/offline04f_tests.py --root . --dotnet /tmp/04k-dotnet/dotnet --unity-references /tmp/04k-unity/Editor/Data/PlaybackEngines/LinuxStandaloneSupport/Variations/mono/Managed --nunit /tmp/04k-nunit/framework/lib/netstandard2.0/nunit.framework.dll --nunitlite /tmp/04k-nunit/lite/lib/netstandard2.0/nunitlite.dll --assembly /tmp/04k-release-api/player-classic-builtin.dll --baseline-report /tmp/04k-baseline-tests-hydrated/test-report.json --output /tmp/04k-release-tests`

The JSON evidence retains full invocations, source hashes, reference hashes and diagnostics. Exact Editor archive URL/SHA, native invocation and return state are in native-environment.json. The official Input 1.17.0 package URL is `https://packages.unity.com/com.unity.inputsystem/-/com.unity.inputsystem-1.17.0.tgz`. No SDK/Editor binaries are committed.

GitHub workflow `renaissance-motion-04k.yml` is source-only. Its run URL and final commit are recorded in the final handoff, not substituted for the local managed/NUnit results.

Published source CI success at the exact gameplay checkpoint: https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/actions/runs/39207615013. Final docs/evidence HEAD is independently reported in the delivery/PR.
