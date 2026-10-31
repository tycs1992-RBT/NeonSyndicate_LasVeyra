# FR-V1 source and native ledger

SOURCE IMPLEMENTED. ART IMPORTED / NATIVE PLAYTESTED / VISIBLE DIFFERENCE CONFIRMED / GAMEPLAY VERIFIED / OWNER ACCEPTED: NOT RUN here. Overall BLOCKED pending the local native workflow.

Current frozen C# source fingerprint is in SOURCE_CHECKPOINT.json. Both exact-Unity genuine managed API matrices pass12/12, separate Runtime/Editor/EditMode assemblies pass3/3, C#9 syntax passes12/12, and72focused cases pass (including21new native-free proof verdict/selection/privacy cases). One new NeedsEngine preference-restoration case remains NOT RUN. Warning deltas versus the fresh baseline are zero.

Fresh hydrated baseline at57975b3 completes2399pass/47fail/64excluded, rawFAILED. Final-source full regression completes2420pass/47fail/65excluded, rawFAILED, with zero introduced/resolved failure names and zero changed failure reasons. All21new pure cases pass; the one new NeedsEngine case is NOT RUN. Exact tested source is1cac4b8f6d88240366dd4408d6393cc45fa4d78e. The prior 05f82 source run is superseded by a public-accessor assembly-boundary repair. Its results are not substituted for the final source.

Actual source/asset checks: L1 51/51, L2 65/65, L3 63/63, FR01 144/144, FR02A 185/185, H3 78/78, new FR-V1 owner/privacy/source contracts33/33, metadata/GUID/reference checks and three new test-output-isolation cases pass. H5 retains its two inherited failures (46/48): Characters/HumanoidPose.cs and All prior tests and exclusions preserved. No assertion, category or old test was weakened.

Actual Blender loads both original editable sources and reimports72/72 originalFBXs with finite bounds and UVs. No artwork changed and no DCC picture is used as native proof.

Verification infrastructure corrections: an unhydrated baseline first had16extra data failures; it is retained as invalid delivery baseline evidence and was corrected by LFS hydration, not test edits. Resource-killed and interrupted compile/suite attempts are not final passes. A real Runtime/Editor boundary error was fixed with a narrow read-only runtime accessor. The offline harness now archives old XML/report/log files before running, so an interrupted runner cannot inherit stale results. Its three new isolation regressions pass.

Native Test Runner, imported appearance, white-character recovery, actual staging/refusal/frustum counts, PlayMode route/interaction/controller/saves/physics, screenshot pairs, Profiler and Windows launcher/build are NOT RUN. Native evidence will exist only after the local Editor tool executes.

Source CI40414149823 and PR40414157597 complete with the new FR-V1 stage passing and only the same two inherited H5 assertions. LFS pointer set is unchanged:2250paths,1526uniqueobjects,644025337bytes; all object SHA256 values and local fsck pass. No LFS publication workflow was needed because no new binary object was introduced. Final evidence-only descendant and its actual CI outcomes are recorded in PR12.
