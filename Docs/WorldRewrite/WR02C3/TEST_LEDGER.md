# WR02-C3 test ledger

Baseline is published `5c608324bbc2ef330bcbb304da3f8cd31eaafc2a`; current source includes the uncommitted WR02-C3 changes when these local checks were run. Results are source-level unless explicitly stated. `--report` paths were under the worker scratch directory.

| Command or check | Result |
| --- | --- |
| `python3 tools/test_forge_wr02c3.py -v` | 5/5 source contracts PASS. The executable C# geometry harness requires dotnet and is NOT RUN locally. |
| `python3 tools/test_composition_lineage_wr02b.py` | 4/4 PASS. |
| `python3 tools/test_wr02c2_preparation.py` | 8/8 PASS. |
| `python3 tools/test_wr02c_route_integrity.py` | 3/3 PASS. |
| `python3 tools/test_check_world_rewrite_wr01.py` | 28/28 PASS. |
| `python3 tools/check_world_rewrite_wr01.py --report ...` | 29/29 PASS. |
| `python3 tools/check_surface_wr02.py --report ...` | 57/57 PASS. |
| `python3 tools/check_foundry_wr02a.py --report ...` | 69/69 PASS. |
| `python3 tools/check_kingdom_wr02b.py --report ...` | 142/142 PASS. |
| `python3 tools/validate_heart_fr01.py --report ...` | 144/144 PASS. |
| `python3 tools/check_heart_fr02a.py --report ...` | 185/185 PASS; native Unity NOT RUN. |
| `python3 tools/check_fantasy_proof_v1.py --report ...` | 33/33 PASS. |
| `python3 tools/check_real_malik_h5.py --report ...` | 46/48, same two inherited failures as baseline: `Original actor/gameplay authority / Characters/HumanoidPose.cs` and `All prior tests and exclusions preserved`. Its frozen earlier baseline predates established SwordHand/H8 source and later append-only SaveData tests. The assertions were not removed or relaxed. |
| `python3 tools/check_unity_metadata_04c.py ...` | 2,802 metadata files; zero invalid, duplicate or unresolved references; 13 regression mutations PASS. Not a Unity import. |
| `git lfs fsck --objects` and hydration | 2,382 LFS files hydrated; fsck PASS in this checkout. This cannot establish the state of a different checkout. |

The route workflow now runs `python3 tools/test_forge_wr02c3.py --dotnet "$(command -v dotnet)"` on its .NET 8 runner, executing the actual production plan against minimal non-Unity vector stubs. It tests nonoverlapping partitions, exact volume, rejection of wrong source dimensions and the existing service anchor. Read the exact published head's Actions result before claiming this ran successfully. The same workflow's C# syntax configurations and older Unity-reference comparison are supplementary checks, not a Unity 6 managed compile.

Two new Unity `NeedsEngine` tests exercise a stale broken prefab and fallback after a candidate loses material. Additional EditMode tests exercise the geometry plan. All are authored but NOT RUN here. Current Unity 6 compile, Unity import, managed NUnit, Game View, before/after visuals, route, interactions, camera, flight, collision, warnings, save/load, controller, performance, Windows build and owner acceptance are NOT RUN. Prior art CI stopped at the inherited H5 failures and skipped later stages. Check the new published head independently for its exact result.
