# WR02-C test ledger

Fresh baseline and final local checks, using hydrated LFS files and full fetched history:

| Check | Starting source | Route checkpoint |
| --- | --- | --- |
| WR01 preservation | 22 / 22 | 22 / 22 |
| WR02 source and assets | 57 / 57 | 57 / 57 |
| WR02-A foundry preservation | 69 / 69 | 69 / 69 |
| WR02-B source and assets | 140 / 140 | 140 / 140 |
| H5 | 46 / 48 | 46 / 48, same failure details |
| Unity metadata | 2,800 metadata files, no invalid, duplicate or unresolved references | same |
| Fantasy Proof source | Not run at baseline | 33 / 33 |

The two H5 failures are the inherited HumanoidPose authority assertion and the inherited tests/exclusions assertion. They are not reported as passing. The unchanged failure details were compared directly.

The existing WR02-B, WR02 and WR02-A exact-hook mutation tests passed, along with 28 WR01 mutation tests. The new source integrity checks passed 3 / 3. They check route token wiring, original quest references, and exact preservation contracts; they do not execute game code. Git LFS hydrated all 2,382 tracked entries and `git lfs fsck --objects` passed before the code-only change. No art or LFS payload changed.

The new Unity EditMode route test is authored but NOT RUN. Full managed NUnit, the two twelve-mode API matrices and the three assembly boundaries are NOT RUN: the required SDK, matching Unity references and NUnit are unavailable locally. The independent [GitHub source workflow](https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/actions/runs/40929778064) passed all 12 C# 9 syntax configurations and its older-reference diagnostic comparison. Against genuine Unity 2021.3 references, the baseline and changed sources each produced 48 errors, with zero introduced diagnostics. This older API comparison is not a successful Unity 6 compile. The PR test used merge commit `932cf0ca30f7e79fa93cad98547b868b7c062002`, whose tree exactly equals the source commit tree `4f0bf64dfaf130bf0f4662287c4caac835ce4a70`. Syntax parsing and the 2021-reference comparison are source diagnostics only and cannot establish native Unity correctness.

Native Unity import, PlayMode, actual screenshots, route traversal, character appearance, collision, controller, saves, Windows build, performance and owner acceptance are NOT RUN.
