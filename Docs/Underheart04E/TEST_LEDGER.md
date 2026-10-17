# Underheart and Bonds 04E: test ledger

Everything here ran in a cloud Linux session with no Unity editor and no GPU. The tools live in that session, not in the
repository; on the owner's machine use `tools/test.ps1` with Unity 6000.3.16f1.

## Executed checks (revision `9315fe4`)

| Check | Command | Result |
|---|---|---|
| Compile, 12 configurations (editor and player; classic, both, Input System only; Built-in and URP) | `/home/claude/tools/check_urp.sh <root> <out>` | 12/12, 0 errors, the 63 pre-existing warnings (9 distinct). Assemblies built from the Unity 6000.3.16f1 C# reference source, not the native editor |
| EditMode suite under .NET 8 | `cd <root> && /home/claude/tools/check_tests.sh <root> <out>` | **1102 passed, 25 failed, 52 skipped** |
| Failures | same run, verbose (`TestRunner.dll ... -v`) | All 25 call `Resources.Load` or `Resources.LoadAll`, which needs the engine (ArtDistrict, CitadelArt03A2, CrystalCity02D, StrongholdRebirth04A and similar). The identical 25 fail at the 04D base `0dbfc0b` |
| Skipped | same run | 52 tests marked `NeedsEngine` (JsonUtility and friends) |
| Save recovery | same run | `SaveRecovery04DTests` 18 of 18 passed (offline .NET 8, the first time they have been executed) |
| 04E suites | same run | `Integration04ETests` 6/6, `QuestRuntime04ETests` 13/13, `HudLayout04ETests` 12/12, `QuestDataTests` 20/20 (all quest JSON passes `QuestValidator`), plus the Bonds, Fusion, Builds, Underheart, Campaign, Guidance, Ecology and Boss suites; `SaveDataTests` freeze list passed |
| Metadata | `python3 tools/check_unity_metadata_04c.py --report <file>` | 1513 sidecars, 0 invalid, 0 duplicate GUIDs, 0 missing, 0 orphans, 1 TerritoryPlacement source, 13 regressions passed |
| Dash scan | grep for U+2010 to U+2015 in every changed and new file | 0 |
| Ancestry | `git merge-base --is-ancestor 0dbfc0b HEAD` | passes; `main` (9acbfef) and `feature/crystal-arsenal-04d` (0dbfc0b) untouched |
| LFS | `git lfs ls-files` | 1733 tracked files; 366 Assets objects hydrated with matching hashes at the start of the run. 04E adds no binary assets (all procedural) |

Test count history this run: 1074 after the stream merge (`11bc7ef`, `e06643a`), 1102 after the review fixes (`9315fe4`).

## Not run

- Native Unity import, compile, EditMode run, play mode, Windows build.
- Golden screenshots, the perf flythrough, any profiling, any footage.
- Any network test (there is no networking).

## Acceptance matrix (spec section 22)

**PASS-offline** = passed in the offline environment above (not native). **SOURCE** = implemented and wired, pure parts
covered by passing offline tests, behaviour NOT RUN natively. **PARTIAL** = see `FEATURE_STATUS.md`. **NOT RUN** = needs
Unity or the owner.

| Id | Status | Evidence or gap |
|---|---|---|
| A01 | PASS-offline | `0dbfc0b` is an ancestor of `9315fe4`; no local edits were discarded |
| A02 | PASS-offline | 366 Assets LFS objects hydrated, hashes match |
| A03 | PASS-offline | one TerritoryPlacement source; metadata check clean |
| A04 | PASS-offline | `SaveRecovery04DTests` 18/18 and the save transaction regressions under .NET 8; native NOT RUN |
| A05 | PASS-offline | this ledger separates passed, engine-only failures and skips |
| A06 | NOT RUN | needs the pinned editor |
| A07 | SOURCE | `BuildInfo04E`, `ModuleStatus`, pause > STATUS |
| A08 | SOURCE | settings are read, never reset by 04E; not exercised |
| A09 | PASS-offline | every checkpoint pushed; remote head verified after each push |
| A10 | PASS-offline | main and 04D branches untouched; same project |
| B01 to B10 | SOURCE | guidance, venues, map routing and offer card; B07 clearance checked offline for the 12 venues on seed 61724; B09 pad chords documented in `CONTROLS.md` |
| C01 to C10 | SOURCE | Underheart layout validated offline (`UnderheartRules.Validate`, `ValidateWorld`, integration tests); C08 layer checks through `WorldBounds.SameLayer`; C09 and C10 NOT RUN natively |
| D01 to D10 | SOURCE | starter ward via `uh05_01`; ledger once per token (tests); D06 beam and ward frame-rate parity inherited from 04D, not re-measured |
| E01 to E10 | SOURCE | rescue quest, one active companion (`BondRules`, tests), hostile-only targeting, attribution by `DamageOrigin` |
| F01 to F10 | SOURCE | owned chassis only, state machine and clearance rules tested offline; F05 rollback and F08 feel NOT RUN natively |
| G01 | PASS-offline | 16 main quests pass `QuestValidator` (stable ids, reachable phases) |
| G02 to G10 | SOURCE | G03 (normal-play completion) NOT RUN; arena reset on leaving (fixed) |
| H01 | PASS-offline | save normalization tests for the 04E blocks |
| H02 to H10 | SOURCE | H03 fallback and safehouse fix in `GameRoot`; H07 twelve side quests and eight contracts validated offline |
| I01, I02, I05, I06, I07 | NOT RUN | need Unity, footage and profiling on the owner's machine |
| I03 | PASS-offline | no preview images are offered as Unity proof |
| I04, I08 | SOURCE | restrained effects and budgets in source; unmeasured |
| I09 | PASS-offline | `NETWORK_BOUNDARY.md` labels everything local PvE |
| I10 | NOT RUN | no network claim is made |
