# 04A baseline gate for Living Territories 04B

The source branch was inspected at `1afbaa9b7e311f977219412a1fb345bf6857c5a6`, matching the requested 04A tip. Existing untracked browser build output and dependencies were preserved. These checks ran before 04B runtime changes.

Unity 6000.3.16f1 is unavailable in this cloud environment. `bash tools/test.sh` exited 3 with the explicit `TESTS NOT RUN` message. Native import, compilation, EditMode execution, live screenshots, gameplay and frame profiling are **NOT RUN**. The first required Windows task is to compile and test the existing 04A branch, then repeat on 04B.

| Baseline check | Executed result | Scope |
|---|---|---|
| Existing 04A geometry, spatial and preservation audit | 1,889 of 1,889 passed | Actual source meshes, OBJ payload agreement, surfaces/import metadata, collision support and pre-04A preservation |
| Scoped Git LFS payload integrity | 296 of 296 payload hashes match | All tracked `Assets/` payloads and `Docs/VisualRebirth04A/` evidence exist as real bytes |
| Fresh Roslyn C# 9 comparison | 391 runtime/test source files, 35 pre-existing reference-version errors in each side, zero introduced | Genuine Unity 2021.3 reference modules, classic input and Built-in only. This is not a successful pinned Unity build |
| Runtime review | No demonstrated baseline blocker found | 04A material/mesh ownership, static resets, fort fallback transaction, garden placement/colliders and capture restoration inspected |

The 04A switches (`Enabled`, `City`, `Gardens`, `Strongholds`, `Atmosphere`) remain independent. Fort art resolves source geometry and material dependencies before hiding the prior optional shell and restores that shell on failure. The city preflights its architecture assets before selecting the new layer. The original gameplay geometry and previous art code remain available. Source review does not establish live behavior.

Evidence is recorded in `Baseline04A/asset_spatial_audit.json`, `Baseline04A/lfs_payloads.json`, `Baseline04A/diagnostic_comparison.json`, the corresponding compiler logs, and `Baseline04A/unity_test_attempt.txt`. Older, unrelated documentation screenshots remain unresolved LFS pointers in the checkout; none are Unity assets or the 04A delivery evidence scope. No player save was opened or rewritten by this audit.

## Verified baseline repair

The follow-up ecology review found a concrete shared-runtime defect in `CreatureDirector.ReportDeath`: `NoRewards` was honored only for the non-humanoid `Creature` rig. Humanoid creatures keep the same options in `NpcController.CreatureOptions`, which was ignored. A humanoid exhibition/controlled encounter could therefore pay loot, progression and quest-counting kill notifications despite explicitly disabling rewards.

The repair resolves the spawn options from either existing rig before entering the unchanged reward path. Ordinary creatures still receive their original rewards and death notifications. The existing `NoRewards` behavior also suppresses the director's quest-counting `Killed` event; the humanoid's separate `OnKilled` callback still runs in its caller. Controlled encounters must observe their own tracked bodies rather than use the general quest kill stream to detect these deaths.

`tools/check_creature_no_rewards.py` compiles and executes the exact `ReportDeath` source method with explicitly recorded host/reward doubles. Across two body rigs, missing/false/true options and player/non-player kills, the unrepaired 04A method fails the two humanoid `NoRewards` cases; the repaired method passes all 12. This establishes the routing regression and the preserved ordinary path, not native Unity behavior. Four actual Unity component regression cases are supplied in `CreatureNoRewardsTests.cs`; they are **NOT RUN** here. Detailed cases and both exact method hashes are in `no_rewards_regression.json`.
