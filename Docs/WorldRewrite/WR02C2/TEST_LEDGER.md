# WR02-C2 test ledger

Published starting HEAD: `e5378e8b2e59347c88a903b9e07c2440f8c2cef1`. This checkout was based on that exact remote branch head; the user's earlier local branch was preserved. The final commit/tree and later GitHub Actions results must be read from the actual published branch, not inferred from this document.

| Check | Result and scope |
| --- | --- |
| New preparation, proof and exact-replay Python checks | 8 tests PASS locally; source and hydrated payloads only. |
| WR02-B owner replay mutation checks | 4 tests PASS; arbitrary or partial edits remain visible. |
| WR02-C route and prior contract tests | 3 tests PASS. |
| Prior world preservation guards | WR01 22/22, WR02 57/57, WR02-A 69/69, WR02-B 140/140. |
| H5 guard | 46/48 before and after. Same two failures: `Original actor/gameplay authority / Characters/HumanoidPose.cs` and `All prior tests and exclusions preserved`. No assertion was deleted or weakened. The frozen older baseline predates legitimate SwordHand/H8 additions and later append-only `SaveDataTests.cs` cases. |
| Source receipts | 33 FR01 FBX rows, 39 FR02A FBX rows, shared texture rows and foundry payload rows matched their manifest byte lengths and SHA-256 hashes locally. Independent slot-name parsing found the expected shared materials in all FR01/FR02A files. This does not inspect the owner's checkout. |
| Independent FBX parser | Existing pinned ufbx parser passed Archive LOD0/1/2; 65,100 / 54,730 / 45,011 triangles, no degenerate UV triangles. Not a Unity import. |
| Unity metadata | 2,800 metadata files; zero invalid metadata/references/unresolved references; 13 mutation regressions passed. NOT a native import. |
| Git LFS | All 2,382 listed files hydrated locally; `git lfs fsck --objects` passed. |
| Authored Unity tests | Two NeedsEngine tests cover preparation, foundry selection/fallback and missing catalog/model diagnostics; NOT RUN. |
| Current Unity 6 compile, full managed NUnit, native import and play | NOT RUN here: no Unity Editor, dotnet or mono installed. C# syntax and older-reference comparison are separately run by the existing route CI; those are not Unity 6 compilation. |
| Screenshots, wall identity, warning reproduction, physics, flight, landings, visual acceptance, saves, controller, performance and Windows build | NOT RUN. Owner supplied one pre-change screenshot; it is not a worker-generated before/after pair. |

The baseline art CI for `e5378e8` passed WR02-B then stopped at the same two inherited H5 failures; later stages were SKIPPED. The baseline route workflow passed its twelve syntax configurations and older-reference comparison. No claim of current green CI is made until the exact published head has run. ART PRODUCED: no new art. ART IMPORTED IN UNITY, VISIBLE DIFFERENCE CONFIRMED, GAMEPLAY VERIFIED and OWNER ACCEPTED: NOT RUN.
