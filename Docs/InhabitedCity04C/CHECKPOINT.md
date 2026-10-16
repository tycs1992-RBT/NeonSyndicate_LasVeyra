# UNFINISHED 04C CHECKPOINT

This commit preserves interrupted implementation work at the user's explicit request. It is not a finished 04C release and is not accepted for gameplay. No further feature implementation was performed while securing this checkpoint. No merge into main is intended.

Branch: `feature/inhabited-city-04c`.
Verified original base: `c49144150cfeb2f105f628f7f9d6ed85c1ccb218` (`feature/living-territories-04b`).
The preceding isolated repair commit is `91c790b`: malformed placement/test metadata and serialized 04A shader references, preserving previously valid GUIDs. The user's separate Windows rename/GUID remains unavailable; follow `BASELINE_AND_METADATA.md` before reconciling that local repair.

## Work preserved

- In-place venue selection, transactional shell creation, original-service adapters, append-only save block, physical service guards and Places UI.
- Original source models, high/low runtime mesh payloads, texture, sidecars and authored layouts for **Veyra Lofts, Mara's clinic and Dante's forge**.
- Five Quiet Signal quest definitions, generic interior action bridge, reading state, distributed contract offers, optional finite depot encounter source and native QA tools.
- Resident routines, admission rules and scoped shelter/camera/audio integration sources.
- Source harnesses, tests, partial geometry exports and documentation. These are preserved as work in progress, not recertified as current acceptance evidence.

## Unfinished work and known limitations

1. Only three room layout JSON files exist. **Cinder Diner/inn, archive, observatory, vehicle workshop and freight depot have no authored room layouts yet.** Their catalog/quest/runtime integration is partial. Missing layouts are expected to retain the original building path, and dependent chapters should remain unavailable. These failure paths still need native verification.
2. First-route geometry and facade refinements were interrupted. Source audit reports and exported geometry may predate final edits. Some reports contain failed or pending checks; they must not be summarized as a final all-pass result. Final geometry/export synchronization and visual inspection remain undone.
3. No completed offline image renders are included at this checkpoint. The preserved source geometry and render scripts are not actual gameplay screenshots.
4. Resident admission currently blocks new actors during any active mission, including the new chapter. The proposed narrow exception was not completed. Native staffing, movement, crime and encounter behavior need review.
5. Default source seed selection finds eight candidate parcels, but selection is not room availability. Seed 77231 skips the diner because its canonical parcel is protected/tiered. The requested compact home-clinic-forge route is not achieved: canonical clinic and forge remain distant and unchanged.
6. Integration, preservation and performance review is incomplete. Documentation describing intended eight-destination behavior must be read subject to this checkpoint notice.

## Evidence and unrun testing

Earlier offline runs are recorded in `Validation/` with their own source hashes: ledger/binding/service tests, strict chapter validation, metadata checks and older-Unity-reference compiler comparisons. Those results cover their recorded inputs, not native Unity acceptance or every later edit. The baseline metadata repair has its own isolated evidence.

Unity `6000.3.16f1` is unavailable in this environment. Native import/compilation, EditMode and PlayMode tests, actual doorway/stair traversal, services, save migration, quest completion, encounter cleanup, render review, Windows player builds, controller coverage and performance tests are **NOT RUN**. No actual game screenshot or measured runtime quality/performance claim is made.

Caches, credentials, Unity build outputs, browser `dist`/`node_modules` and Python bytecode are excluded from the checkpoint. Existing untracked local outputs are left untouched in the workspace. Relevant editable assets and required binary payloads use Git LFS. Stop after the checkpoint is published; resume development only under a subsequent instruction.
