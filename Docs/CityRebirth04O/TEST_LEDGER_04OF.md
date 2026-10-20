# Executed exact-source verification

Source commit: a580483ef5467282f3724c8357ab6226ca429c32. C# SHA-256: 219945986a89b01c5074a7783dabe49b651f697dc4f4420b9ff9fca088271ce8. 970 C# files: 828 runtime, 14 Editor, 128 EditMode tests. Accepted parent 9c8f32119a6ee9b7b94b5e3e879429981179507b; accepted 04OE comparison source a72ef29e5456a61b470b8bbca4081ebc1a4af04c. Evidence-only delivery must retain this snapshot.

| Executed check | Result | Raw evidence |
|---|---|---|
| Genuine managed API compile | 12/12 PASS: Editor/Player x Classic/Both/Input-only x Built-in/URP | api-matrix.json, api-matrix-full.json.gz, api-logs.txt.gz |
| Real source assembly boundaries | Runtime, Editor and tests 3/3 PASS | assemblies.json |
| C#9 syntax | 12/12, zero syntax errors | syntax-matrix.json |
| Full external NUnit | 1751 discovered, 1693 executed, 1647 PASS, 46 FAIL, 58 unchanged exclusions | external-tests.json, nunit-results.xml.gz, nunit.log.gz |
| Failure names AND messages | Zero introduced, changed or resolved compared to accepted executed 04OE | failure-comparison.json |
| New tests | 29/29 PASS, included in full run | new-tests.json |
| Current source integration guards | 14/14 PASS, earlier raw guard retained with one explicit asmdef exception | report.json, inherited-04oe-raw.json and predecessor raw files |
| Metadata/GUID/reference audit | 1791 sidecars, zero invalid/unresolved; 13 auditor regressions PASS | metadata.json.gz |
| LFS / whitespace | git lfs fsck PASS; git diff --check PASS | preservation-execution.json |
| Topology/geometry | Real source revisions 4/5 executed; five-seed route tests pass; new diagnostic meshes finite | topology.json, source-world-manifest.json.gz, geometry-exports.json, Geometry |
| GitHub source CI | Push 39444622227 and PR 39444633002 success at tested source | ci-source.json |
| Native Unity acceptance | Import, native EditMode/PlayMode, device/save behavior, images, profiling, Windows build NOT RUN | native-status.json |

The raw external suite remains Failed with exit 46. The comparison wrapper returns zero only because every failing name and message matches accepted 04OE. No source test is reclassified or newly excluded. Existing test source is unchanged; only CinematicRebirth04OFTests is added. A NeedsEngine exclusion is not a passing native test. The 29 new cases cover actual body/apron emission, vertex/weight/expression budgets, exact collapsed faces, bind math/target order, safe paths/evidence filenames, phase/part crests, one-shot/cancel/Pause window and finite regional receipt compatibility, prowler clone/timing, and five-seed authored routes/views.

Development failures were inspected and fixed: an ambiguous CompressionLevel name, List<Point>.Length misuse, and five new route tests incorrectly supplying no original venue bindings. Corrected tests select real catalog bindings and assert profile admission before geometry. They were not excluded. A prior source guard refused the new Editor asmdef references; the current guard narrowly allows exactly UnityEditor.TestRunner and UnityEngine.TestRunner and independently retains all other asset/owner invariants. Final full source has none of these introduced failures.

Reproduction: use tools/check_04g_compile.py with exact Editor modules/package assemblies, official Input System 1.17.0 source-built reference and NUnit. Full invocation/reference hashes are retained in API evidence. Run tools/check_04g_assemblies.py only after that matrix completes; tools/offline04f_tests.py with the completed player-classic-builtin.dll and --baseline-report Docs/CityRebirth04O/Evidence04OE/external-tests.json; then current guard, syntax/metadata tools and tools/measure_cinematic_rebirth_04of.py. No Unity executable/license was used for these managed checks. Do not install managed validation dependencies inside Assets.
