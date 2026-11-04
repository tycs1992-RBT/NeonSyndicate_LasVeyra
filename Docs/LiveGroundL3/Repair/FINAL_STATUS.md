# Final L3 focused repair status

Tested source commit: `a9e6f9e4addcfbd33f25aa108688b94d7b8d7812`.

C# SHA256: `be9367283efda8ae4542ee43af5196e6ee7a81981cd3c1b8b33654007a72974a`.

The final delivery is a forward-only evidence/publication descendant of this source. See FINAL_SEAL.json and PR12 for the publication checkpoint. No L4 was started. PR12 remains draft and unmerged. The separate L3 appearance setting remains OFF by default.

## Fixed

CityDressing.Frost.cs is byte-identical to the pre-L3 baseline 7a28c90. A small CityBuilder instance scope routes only the original frost tile's bare Mats.Sand batch to the existing shared L3 floor proxy. The scope restores its prior value in finally. Snow materials, snow geometry, unrelated sand, chunk ownership, apertures, colliders and no-shadow policy retain their original code. The ground guard now checks the complete Frost file and the exact allowed CityBuilder edits. All four deliberate guard mutations were rejected.

The complete 147-file newest-additions commit was audited. Its 35 new materials use Unity's built-in Standard shader and the same default base color. No other tracked serialized asset refers to their added material/folder GUIDs, and the model importers have no external-object remaps. They are external import copies superseded by the existing embedded-model and Prepared-material lifecycle. Those 35 generated copies, 35 material sidecars and five generated folder sidecars were removed. Their exact bytes remain recoverable in d48ed5b. No model, texture, livecast JSON, identity, rights receipt or legitimate source art was removed.

The 51 protected LiveCast/ProductionH9 metadata files were restored exactly. Required Generic rig, complete weights, embedded materials and Texture2D import configuration remain owned by the existing unchanged HumanoidPrepH9 and LiveCastSetup preparation code; the protected metadata is not a claim of native import acceptance. All 21 newer metadata changes in ProductionH8R, ProductionH8S and WorldFoundationH8R were retained byte-for-byte. Narrow ignore entries cover only the five audited generated Materials directories. Other owner drop-in folders remain trackable. Both unrelated local import temporary files were left intact.

## Executed on final C# source

| Check | Actual result |
|---|---|
| Genuine Unity 6000.3.16f1 API, external NUnit | 12/12 PASS |
| Genuine API, bundled project NUnit | 12/12 PASS |
| Runtime, Editor, EditMode assembly boundaries | 3/3 PASS |
| Roslyn C# 9 syntax | 12 modes, zero errors |
| Focused pure L3 NUnit | 21/21 PASS |
| L1 / L2 preservation | 51/51 and 65/65 PASS |
| Ground assets and exact owner guard | 63/63 PASS |
| Preservation, save-schema, skin and intake regressions | 30/30 PASS |
| Independent strict Redspire FBX parse | 18/18 PASS |
| Metadata | 2535 sidecars; zero invalid GUIDs or unresolved references; 13/13 regressions PASS |
| Full external NUnit, both fresh fetched baseline and repaired source | 2358 passed, 47 failed, 63 excluded; raw FAILED |
| Full-suite comparison | Zero introduced/resolved failure names and zero changed failure reasons |

Raw NUnit XML and logs are retained as compressed archives. A comparison exit of zero means no new failures; it does not turn the raw 47 failures into passes. Both twelve-mode matrices ran serially without host-killed compiler retries.

## Remaining failures and native status

GitHub passes the formerly failing L1 preservation gate, plus L2, ground and independent FBX gates. It then stops at the unchanged H5 assertions `Original actor/gameplay authority / Characters/HumanoidPose.cs` and `All prior tests and exclusions preserved`. The same failures were freshly reproduced at d48ed5b. Later skipped CI steps remain skipped. No workflow or legacy guard was weakened.

Additional locally executed H6, H7, H8 and H8S frozen-owner guards retain their inherited failures. Exact baseline/current names are in legacy-guard-comparison.json. The H8S local metadata guard also sees the two unrelated untracked import temporary files already present before this repair; they are preserved and are absent from clean CI. These findings were not hidden or relabeled as passes.

Unity import, shader compilation, native EditMode/Test Runner, PlayMode, actual saves, controller/device tests, collision/camera, Unity screenshots, Profiler, and Windows build/launch are individually NOT RUN. Managed API compilation and NUnit on .NET are supplemental source evidence. They do not execute the native engine. L1 and L2 native gates remain open; L3 appearance and performance acceptance remain pending.
