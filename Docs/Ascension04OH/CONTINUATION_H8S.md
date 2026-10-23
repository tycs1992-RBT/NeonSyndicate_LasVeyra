# H8S Living Cast integration and H9 refinement: delivered, offline verified

Fetched HEAD ae5003fc2b3b4e5754c00b95aaaaa97169b8ff3c (C# SHA256 693cf077...7a9d). Tested code 94984095c0d9b26fc4aa396a9337159387d4035b, C# SHA256 7af46c43b2e7070a109f41eec6a63393a3d92f00fd3e05466a695e7e3d4ff1ba; the final delivery is its documentation-only descendant, pinned in draft PR12 (still targeting feature/04o-shattered-metropolis-reborn). PR11 and 721c1238 untouched. No merge, force push, shared-history rewrite, renderer switch, default-world promotion, save migration, revision 11 or production activation.

Delivered: the H8S curated motion contract (schema 2, 17 clips), landing after real air time, Dante quiet listening, H8S and H9 Malik reviews, the Dante living cast review (H8S and H9 cuts, independently reviewed and fixed), Forge and route VISUAL PREVIEWS, combat readability, companions and named cast, quest guidance, Pause > ACCESS, four new revision-10 side quests (data), revision-aware Forge offers (Hearth Witness first in revision 10) with givers present during their own quests, and progression readability. The Hearth Witness file and reward are byte-identical.

Offline reference harness, not native Unity: 12/12 compile configurations clean; NUnit 2122 passed, the same 27 engine-dependent failures as the baseline, 58 skipped; guards 0 new failures of 44; check_living_cast_h8s 52/52; metadata audit passed. Native Unity import, play, saves, devices, screenshots, profiling and the Windows build: NOT RUN. Owner approval: NOT GIVEN.

Git LFS: the build host's network policy refuses the LFS storage host, so the H9 binaries were pushed as plain blobs and stored in LFS by the repo's own publication workflow (bot commit 11f776d); all 70 objects confirmed on the server. This host's system git config skips LFS smudge: run `git lfs checkout` after any checkout before running guards.

Read Docs/Ascension04OH/FEATURE_STATUS_H8S.md, OWNER_GUIDE_H8S.md, 05_OWNER_PLAYTEST.md, TEST_LEDGER_H8S.md, NATIVE_ACCEPTANCE_H8S.md and DELIVERY_H8S.json. Next: Tyler's native playtest (05_OWNER_PLAYTEST.md), then fix what it finds.

<!-- H8S INTAKE STATUS RETAINED -->
# H8S implementation in progress

Accepted live branch: feature/04oh-ascension-art-foundation. Fetched HEAD b267a7199b94b37cf3a428de6ace7d043fb3758a. Continue the actual H8R source and preserve all earlier assets, default fallback, revisions and save banks 0 through 10. PR12 remains draft against the gameplay parent; PR11 is untouched.

The supplied consolidated H8S directive is authoritative. Separately named 00_START_HERE.md, 03_THREE_ARCHIVE_INTAKE.md and 05_OWNER_PLAYTEST.md were not mounted; its embedded intake and QA requirements are being followed. No separately supplied content was invented.

Three actual archives have been staged read-only and inventoried. Their root licenses explicitly dedicate the included standard contents to CC0-1.0, author Quaternius, https://quaternius.com/. No purchase, bundled code execution or automatic project import occurred. Exact archives, hashes and all member paths are in ASSET_INTAKE_H8S.json. Only selected, recorded files advance.

Native Editor capability checked once: no executable Unity Editor is installed in the current runtime; genuine managed reference DLLs are available. Native import, PlayMode, EditMode, actual saves/devices, Unity screenshots, profiling and Windows build remain NOT RUN. Production approval is unfinished.

Next implementation: retain Malik identity, fix the observed collar seam in a separate derived art version; retarget a supplied landing and quiet NPC pose; use the male court study as the basis of an opt-in Dante Rook candidate, preserving his original male/bearded Forge identity and all original NPC owners; admit a small coherent selection of Forge/gallery props. Existing physical route and collision remain authoritative until native acceptance.
