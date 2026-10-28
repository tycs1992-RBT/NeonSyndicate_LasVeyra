# WR02-A executed verification

Starting branch HEAD: ed483fc720ee1493ab0e2f783c643ea0e9ae6397. Exact managed tested source/art: fafe816fc6dbcd6086941f31af7c2386c8253dda. C# source SHA-256: 64457a34677fbec67ffbc495a3d802a7bb9726d3e0952adac8b3b323291864f6. Source was pinned in a separate checkout. The final receipt commit preserves those C# bytes and model/texture payloads.

| Check | Fresh starting source | Final source |
| --- | --- | --- |
| Genuine Unity 6000.3.16f1 managed API, external NUnit | 12/12 PASS | 12/12 PASS |
| Same twelve cases, project-bundled NUnit | 12/12 PASS | 12/12 PASS |
| Runtime / Editor / EditMode assembly boundaries | 3/3 PASS | 3/3 PASS |
| C# 9 Roslyn syntax configurations | 12/12 PASS | 12/12 PASS |
| Full managed NUnit | 2539 pass / 47 fail / 74 excluded | 2547 pass / 47 fail / 74 excluded |

The full suite remains raw FAILED. The final comparison reports zero new failure identities and zero changed failure messages against the fresh starting run. All 47 names, messages and stack traces are retained in VERIFICATION/inherited-failure-identities.json, alongside both full reports. Exclusions are not passes. The genuine managed references and exact hashes are retained in both matrix receipts. No native Test Runner, scene execution or player build is implied.

Twenty-two existing standalone guards/mutation suites were run against both starting and final source. Their exit outcomes are unchanged. L1/L2/L3, FR01/FR02A/FRV1, WR01, H3, metadata, quest/save preservation and relevant earlier mutation tests pass. Both final WR02 exact-surface and WR02-A foundry guards pass, 57/57 and 69/69 respectively. New foundry lineage mutation cases pass 3/3; historical WR02 cases pass 3/3 without relaxed assertions. All inherited C# tests remain byte-identical.

Known inherited standalone failures remain: H5 46/48, H6 50/52, H7 55/58 and H8 22/26. Their baseline/final reports in VERIFICATION identify each assertion. H5 fails Original actor/gameplay authority / Characters/HumanoidPose.cs and All prior tests and exclusions preserved. No H5-H8 assertion body was weakened.

The optional provider adapter passes 9 offline mocked policy cases; collider policy 2/2, batch gate 1/1, and actual Blender intake counterexamples 6/6. These prove local policy behavior, not a live Tripo connection. No provider request was submitted and no credits spent. The adapter's price estimate is not a provider-enforced hard cap.

Four actual editable Blender source files reopen, six actual FBXs reimport, and six current FBX payloads pass a fresh independent strict ufbx parse. Geometry, finite/unit normals and tangents, nondegenerate UVs, metre bounds, skin/bind contracts and normalized four-influence weights are checked. Fifteen independently decoded clip/LOD combinations cover 25,920 original source bone positions, with maximum error 0.0000009543618995078162 metres. This is a source-pose round trip, not native full-skin deformation, foot contact, attack/damage or telegraph acceptance. Seven images are labelled DCC, not Unity.

GitHub source validation run 40770698913 passed all stages through the new WR02-A stage, including 69 source/asset checks and the real six-FBX/15-motion parse. The following original-owners stage fails at the two inherited H5 assertions. Its later four validation stages are SKIPPED, not passed. Raw source CI log and step receipt are retained. The final published receipt commit is separately checked on GitHub and reported in PR12 and the delivery response.

The initial raw-binary source checkpoint makes Git LFS report representation-only dirty files in the test checkout: its binary files match the published source bytes, while the clean filter expects pointers. The forward LFS commit preserves their exact hashes and sizes. Final delivery also removes trailing whitespace from twelve newly introduced metadata files and prevents the sealer from adding it again. GUIDs and importer semantics do not change. These are checked separately; no C# or model payload changes follow the tested source.

Native Unity import, native Test Runner, PlayMode, Game View, character appearance, collision/camera/flight, creature motion/contact/hitboxes, gameplay, keyboard/controller, saves, profiling and Windows build/launch: NOT RUN. Owner artistic acceptance: NOT RUN.
