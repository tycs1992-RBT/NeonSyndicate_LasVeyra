# WR01 fresh final verification

Exact tested published source:7b7716f16417607391b3b3c097b8c6c95fb2cc92. C# fingerprint:ed2e2cf4d8922174218d04a0828b394e1524a2454c216d0ec3eccc88d6a687a2,1,295 files. Independent git-archive verification binds this commit to the exact compiled working bytes; the older editing checkout HEAD is not used as a substitute source identity.

Fresh post-maintenance executions, with actual new raw reports/XML retained in VERIFICATION:

| Check | Actual result |
|---|---|
| Genuine Unity 6000.3.16f1 API, external NUnit |12/12 PASS|
| Genuine API, Unity-bundled NUnit |12/12 PASS|
| Runtime / Editor / EditMode boundaries |3/3 PASS|
| C#9syntax |12/12 PASS, zero errors|
| Warning counts and diagnostic identities |Zero delta across24 cases|
| Focused managed |142/142 PASS|
| Fresh baseline full, b1ecc142 |2420passed /47failed /65excluded, rawFAILED|
| Fresh final full, exact ed2e source |2490passed /47failed /71excluded, rawFAILED, NUnit exit47|
| Failure comparison |No introduced/resolved failure names or changed messages|
| New pure managed cases |70 PASS (45replacement,19upperregion,6proof)|
| New native transaction cases |6 NOT RUN, NeedsEngine|
| L1 / L2 / L3 preservation |51/51,65/65,63/63 PASS|
| FR01 / FR02A |144/144,185/185 PASS|
| FRV1 / WR01 exact preservation |33/33,22/22 PASS|
| Ascension / H3 |27/27,78/78 PASS|
| H5 |46/48, same two inherited failures|
| New Python guard regressions |28/28 PASS|
| GUID/reference audit |2,685 metadata, zero invalid/duplicate/unresolved;13 regressions PASS|
| Actual Blender compressed source + FBX reimport |15/15 PASS|
| Pinned independent FBX parser |15/15 PASS|
| Fresh normalized remote LFS |2,273 paths,1549objects, every SHA256/size and fsck PASS|

Final full XML ran2026-10-27T20:22:21.8153134Z to20:28:30.6025015Z (368.787150s). Actual XML SHA256:c7a90703eddf79cc10ccc81b940e664400f389f5da3129728e431f5bc34953a9. This is managed execution with genuine metadata, not Unity Test Runner or PlayMode.

Inherited H5 failure identities remain unchanged and the assertions are untouched:
- Original actor/gameplay authority / Characters/HumanoidPose.cs
- All prior tests and exclusions preserved

Two historical sprint-scope assumptions needed explicit owner-authorized compatibility updates, without allowing inherited payload changes. The old FRV1 whole-Art/no-addition freeze gave31/33 after the new art; it now accepts only new HeartWR01 prefixes and verifies every inherited payload/GUID/deletion. The old ascension rule gave25/27 solely for UnderheartBuilder and its aggregate. Its narrow proof accepts only unchanged immutable b1 source or the complete exact literal render-hook transformation, with all other bytes frozen.28 Python regressions reject modified/deleted old assets/tests, unrelated additions, changed LFS identities and collider/unrelated source changes. Historical raw failures and repaired reports are retained. No unrestricted owner exemption, H5 change or NeedsEngine reclassification was used.

Source CI push40576896586 and PR40576906191 complete with all new WR01 stages passing, then only the same two H5 assertions. Later stages are SKIPPED, not passes. LFS publication40576896522 succeeds. Exact job logs122445622570/122445656850 were read. Final documentation-head CI is recorded in PR12 and the final delivery response.

Workspace maintenance removed the first unpublished attempt and its raw receipts. Source was reconstructed exactly to the same fingerprint, art genuinely regenerated in a smaller compressed container, and all results above were freshly rerun. Historical maintenance-loss metadata remains separate; no XML was fabricated. A first empty-cache fetch accidentally resolved the older local branch and was corrected to the exact normalized SHA before the final LFS pass.

Native import/shader compilation, Unity Test Runner, PlayMode, Malik appearance, actual counts/refusals, collision, controller, saves, gameplay, screenshots, profiling and Windows build/launch remain NOT RUN. No native acceptance or artistic score follows from these tests.
