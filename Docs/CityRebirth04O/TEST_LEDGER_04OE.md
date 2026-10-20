# Executed 04O-E verification

Exact tested source: a72ef29e5456a61b470b8bbca4081ebc1a4af04c. C# SHA-256 snapshot: 5b011e3c8b26208b4c91a00444353e2252f7d87792fb5cb1bff735e2c7118d93. 960 source files: 822 runtime, 11 Editor, 127 EditMode test files. A subsequent evidence-only commit must preserve this snapshot. Branch feature/04o-shattered-metropolis-reborn; accepted parent 0765f10f02125dca0ef95b86e9f7263e14963e2c.

| Executed check | Result | Evidence |
|---|---|---|
| Genuine Unity managed API compile | 12/12 PASS: Editor/Player x Classic/Both/Input-only x Built-in/URP | Evidence04OE/api-matrix.json and compressed full reference report |
| Actual asmdef boundaries | 3/3 PASS, runtime/Editor/tests | Evidence04OE/assemblies.json |
| Roslyn C#9 syntax | 12/12, zero syntax errors | Evidence04OE/syntax-matrix.json |
| Full external NUnit | 1722 discovered; 1664 executed; 1618 PASS, 46 FAIL, 58 unchanged NeedsEngine exclusions | Evidence04OE/external-tests.json and nunit-results.xml.gz |
| Failure comparison to executed 04OD | Same 46 failing names AND same messages; zero introduced/resolved/changed reasons | Evidence04OE/failure-reason-comparison.json |
| New 04OE cases | 30/30 PASS, recognition of saved identities/claims/UIDs, original full-pack rules, upgrades, moves, meshes, five-seed routes, optional quests, coats, mastery and import policy | Evidence04OE/new-tests.json; focused-integration-tests.json |
| Current integration/preservation guards | 16/16 PASS; predecessor differences explicitly recorded, unchanged campaign/focus/bond/fusion/spawn/timer owner bytes checked | Evidence04OE/preservation.json |
| Metadata/GUID/reference audit | 1781 sidecars, zero invalid metadata, invalid references or unresolved references; 13 auditor regression cases pass | Evidence04OE/metadata.json |
| Git whitespace / LFS | git diff --check PASS; git lfs fsck PASS | Execution summary; no deleted asset or modified existing meta |
| Topology / geometry probe | Real source revisions 4/5 executed, source meshes exported and physical graph lengths measured | Evidence04OE/topology.json, geometry-exports.json, Geometry |
| GitHub source workflow | Push and PR runs succeeded at a72ef29; workflow runs guards/syntax/metadata/whitespace, not Unity | Evidence04OE/ci-source.json |
| Native Unity | Import, native EditMode/PlayMode, player-save/device acceptance, screenshots, profiling and Windows build NOT RUN | Evidence04OE/native-status.json |

Raw NUnit remains Failed. The comparison wrapper returned 0 only because all 46 engine-call failure identities and reasons match the accepted executed 04OD report. No failing test was moved to NeedsEngine or removed. Source tests are not a running Unity scene or a real player-save test.

During development, five new route tests initially found source admission/height lookup defects. The implementation gained bounded alternative admission and height-aware attachment only along authored revision-five spans. Another check found the new Malik layer was budget-skipped; emission was moved earlier and tests now require nonzero emitted geometry. The initial full run had two additional assertion failures: old tests assumed revision 5 was invalid and all guaranteed uniques were foci. They now verify the explicit new profile and melee exceptions while retaining original focus/unique constraints. The final run has none of those introduced failures. Mesh-count thresholds on smaller access/shin pieces were corrected without changing the actual route/geometry checks.

Reproduction entry points:

- tools/check_04g_compile.py with the exact 6000.3.16f1 installed managed modules, pinned Input System/NUnit/package references and invocation in api-matrix.json.
- tools/check_04g_assemblies.py --matrix <completed API output> --dotnet <dotnet> --output <assemblies>.
- tools/offline04f_tests.py with the completed player-classic-builtin.dll and --baseline-report Docs/CityRebirth04O/Evidence04OD/external-tests.json; full invocation is retained in external-tests.json.
- tools/check_crystal_unbound_04oe.py --report <report.json>.
- tools/measure_crystal_unbound_04oe.py --root . --dotnet <dotnet> --assembly <player-classic-builtin.dll> --unity-references <genuine player managed modules> --output <directory>.
- tools/check_visual_rebirth_04a_syntax.py, tools/check_unity_metadata_04c.py, git diff --check and git lfs fsck.

Known source/art limits: independent ecology and new defender tactics are inherited/incomplete; professional assets and paired clips are absent. The original smooth procedural Malik mesh retains 161 exact zero-area faces in both accepted-before and current C# data, a pre-existing cleanup task. Tiny nonzero blade-tip cap triangles fall below the exporter's near-zero threshold and are recorded separately; they are not hidden as a passed native mesh audit. Source geometry counts and face-normal diagnostics do not prove art quality, camera clearance or performance.
