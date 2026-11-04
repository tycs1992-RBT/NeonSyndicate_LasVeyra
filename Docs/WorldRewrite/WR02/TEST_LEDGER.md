# WR02 exact tested-source ledger

Starting HEAD: 99072f180f11da7edf168602b6fc603d5eb71daf. Exact tested source: 251a3d8f10fd84bae27b338391574cfdaa3ac8af. Complete Runtime/Editor/EditMode C# fingerprint: 81120dbf0bb7910d11322d696b94357094abba4625b8523960c8c69c6a24778a. Tests ran against a separate worktree pinned to that commit, not the changing delivery-document worktree. All 125 source/art checkpoint payloads also matched their uploaded hashes.

| Executed check | Result |
| --- | --- |
| Fresh baseline genuine external API matrix | 12/12 |
| Fresh baseline bundled NUnit API matrix | 12/12 |
| Fresh baseline assembly boundaries / C# 9 | 3/3 and 12/12 |
| Fresh baseline full managed NUnit | 2490 passed, 47 failed, 71 excluded |
| Final genuine external API matrix | 12/12 |
| Final bundled NUnit API matrix | 12/12 |
| Final Runtime, Editor, EditMode boundaries | 3/3 |
| Final Roslyn C# 9 syntax configurations | 12/12 |
| Final full managed NUnit | 2539 passed, 47 failed, 74 excluded |
| New or changed failure identities/reasons | 0 / 0 |
| WR02 exact inherited-source/assets/GUID guard | 57/57 |
| WR02 hook mutation regressions | 3 tests, including every hook path and extra/partial mutations |
| Existing WR01 guard regressions | 28/28 |
| Blender 4.5.3 editable sources / FBX reimport | 2 source loads, 18/18 FBXs |
| Independent pinned ufbx parse | 18/18 |
| PBR PNG dimensions/channels/normal vectors | 15/15 |
| Metadata GUID audit | PASS, no established metadata changed |
| Empty-cache LFS fetch/hash verification/fsck | PASS, 2311 entries, 1587 unique objects |
| Native Unity import/Test Runner/PlayMode/screenshots | NOT RUN |

The raw final NUnit result remains FAILED, native execution NOT RUN. Raw NUnit exit is 47; the baseline-comparison wrapper returns 0 because all 47 names and exact failure messages match the freshly executed baseline. Forty-nine new managed cases pass; three newly added NeedsEngine cases remain excluded and NOT RUN. There were no existing test/category edits or exclusions added to hide failures. All 47 exact inherited names, messages and stack traces are retained in TEST_SUMMARY.json and VERIFICATION/full-managed.json; actual XML is VERIFICATION/nunit-results.xml.

All local L1/L2/L3, FR01/FR02A/FRV1/WR01, H3, prior-owner, source-schema and metadata checks retain their executed baseline outcomes. Exact raw receipts are in VERIFICATION/baseline-guards and final-guards. The old frozen-source guards gained complete SHA-pinned literal replay for authorized WR02 integration hooks; arbitrary changes still fail. Mutation tests prove the distinction. Raw failures of the original unadapted guards are preserved in VERIFICATION/raw-before-scope.

Inherited local guard failures remain unchanged: H5 46/48, H6 50/52, H7 55/58, H8 22/26. These are frozen-owner assertions on previously changed HumanoidPose, SaveSystem, Game and PlayerCamera, plus H5's inherited prior-test assertion. Their assertion bodies were not modified. Exact names and per-check results are preserved in raw baseline and final JSON.

GitHub source-commit CI: both push and PR validations fail at the same two inherited H5 assertions, Original actor/gameplay authority / Characters/HumanoidPose.cs and All prior tests and exclusions preserved. The new WR02 stage and every preceding stage pass. Subsequent independent-character parser stages are SKIPPED, not passes. Actual run IDs are 40741541700 and 40741554929. See ci-source.json and ci-source.log. Final publication CI is checked again after the text evidence commit; the PR receipt records the final run without rewriting this historical source receipt.

The dependency receipt records official Unity 6000.3.16f1 managed module hashes, genuine package references, the Input System source-built assembly, both NUnit references and tool versions. This is managed API validation, not an installed/licensed native Unity Editor, Unity import, renderer execution, Windows build or hardware profiling.
