# Executed 04M verification

Branch `feature/living-crystal-world-04m`; [draft PR #8](https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/pull/8) targets supplied 04L `8c6c3804b70898e745479affecdfe5f66d95ad1e`. Tested source `b38a0e649e9e3c8f8df49aa28b8519dcac8e7cb5`, tree `8e3b4d6773bb25a53a031ff7e8543e2d29e79ca2`, C# snapshot `7e894569f4925304477ea4fe17e9403ff8a3aa79f9dab836d1267f9def67e4ef`. Later handoff/evidence commits retain this tested C# snapshot; resolve delivery HEAD from PR #8 or git. **Production sprint PARTIAL. Native Unity, gameplay/save/render/profile/Windows acceptance NOT RUN.**

The supplied 04L checkpoint/report was trusted, without a repeated predecessor run. Its retained report at Docs/CinematicSaga04L/Evidence/resume-external-tests.json reports1389 external passes/46 engine-call failures/58 exclusions. It is the comparison reference, not a fresh 04M baseline execution.

| Executed on current source | Result | Evidence |
|---|---|---|
| Genuine Unity managed API matrix |12/12 configurations pass, player/editor × classic/both/input-only × Built-in/optional URP | managed-api.json |
| Separate assemblies |Runtime764 / Editor10 / EditMode117 source files;3/3 pass | assembly-boundaries.json |
| Full external NUnit |1520 discovered,1462 executed: **1416 pass,46 fail,58 NeedsEngine exclusions**. Raw result Failed/exit46; comparison exit0; no new failure names/changed reasons | external-tests.json; external-results.xml.gz |
| New 04M cases |All27 pass; existing native cases remain excluded/NOT RUN | new-tests.json |
| Roslyn syntax |12 configurations, zero syntax errors | syntax.json |
| Metadata/GUID |1698 sidecars,13 auditor regressions, no invalid/unresolved references | metadata.json |
| Preservation |26 current guards;22 04I /11 04J source guards pass | preservation.json; inherited-04i/j.json |
| Actual new geometry |Ten OBJ exports from compiled MeshBuilders; valid triangle indices, recorded vertex/triangle/height bounds and SHA256 | geometry.json; measure_living_world_04m.py |
| Whitespace/Python |git diff --check; both new tools py_compile pass | tracked tooling |
| Native Unity |NOT RUN on04M; inherited same-host licensing/UPM IPC blocker, no changed host or new attempt | native-status.json |

Source CI was observed queued at source b38a0e6: push39285908439, PR39286044507. No GitHub CI pass is claimed from queued jobs; inspect final delivery runs in PR#8.

All compiler inputs are captured immutably; source-snapshot.json asserts every captured C# file equals published source. Resource JSON hashes are recorded separately. No C# changed after the reported matrix/suite. Prior LFS payloads were not rehashed for this sprint; the preservation audit confirms no original binary asset/GUID changed. Previous LFS results remain predecessor evidence.

Focused source-gate checks: B6 geometry cases; C11 routine/culture/policy cases; D14 focused+shipped-data; E18; F25 including all generated-contract validation. Final full suite includes every applicable external test. Review corrections fixed humanoid calm, double-counted reservations, consent preservation and small-viewport pagination before final delivery. A new mesh-export probe initially assumed a public triangle API; it was corrected to inspect the actual single-submesh topology before successful export. No failed game regression is waived.

Reproduce on this host:

```bash
python tools/check_04g_compile.py --root . --dotnet /tmp/04k-dotnet/dotnet --editor-data /tmp/04k-unity/Editor/Data --packages /tmp/04k-unity/Editor/Data/Resources/PackageManager/ProjectTemplates/libcache/com.unity.template.3d-cross-platform-17.0.14/ScriptAssemblies --nunit /tmp/04k-nunit/framework/lib/netstandard2.0/nunit.framework.dll --input-system /tmp/04k-input-build/Unity.InputSystem.dll --output /tmp/04m-publish-api
python tools/check_04g_assemblies.py --matrix /tmp/04m-publish-api --dotnet /tmp/04k-dotnet/dotnet --output /tmp/04m-publish-assemblies
python tools/offline04f_tests.py --root . --dotnet /tmp/04k-dotnet/dotnet --unity-references /tmp/04k-unity/Editor/Data/PlaybackEngines/LinuxStandaloneSupport/Variations/mono/Managed --nunit /tmp/04k-nunit/framework/lib/netstandard2.0/nunit.framework.dll --nunitlite /tmp/04k-nunit/lite/lib/netstandard2.0/nunitlite.dll --assembly /tmp/04m-publish-api/player-classic-builtin.dll --baseline-report Docs/CinematicSaga04L/Evidence/resume-external-tests.json --output /tmp/04m-publish-tests
python tools/check_living_world_04m.py --report /tmp/04m-publish-preservation.json
python tools/check_unity_metadata_04c.py --report /tmp/04m-metadata.json
python tools/measure_living_world_04m.py --dotnet /tmp/04k-dotnet/dotnet --unity-references /tmp/04k-unity/Editor/Data/PlaybackEngines/LinuxStandaloneSupport/Variations/mono/Managed --assembly /tmp/04m-publish-api/player-classic-builtin.dll --output /tmp/04m-geometry
```

These are managed/source checks, not Unity Test Runner, native scenes or a Windows build. No copied real player save, controller device, rendered frame, measured route duration or Profiler trace has been accepted. Native commands and acceptance route are in START_HERE_04M.md.
