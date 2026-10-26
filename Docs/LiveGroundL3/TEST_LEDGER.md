# L3 executed evidence

Fetched baseline: 7a28c90f00d25a06ff946cd47f6c17f0e6a99b2e. Exact locally tested C# checkpoint: 3438d757306185e7a22b19feec0c097d744029ae. C# SHA256: 172d30f7b6ede6b554f1fe06352c4854a60d025de138c5592853e53fe7d8b54f. Publication through the connected GitHub app preserves the equivalent source tree in focused forward commits; the equivalent published source is 330ab453491d9d8ae4836228c4424bd6c1aeba91. The final delivery SHA is recorded in PR12.

| Actual check | Result |
|---|---|
| Genuine 6000.3.16f1 managed API, external NUnit | 12/12 effective PASS |
| Genuine managed API, bundled NUnit | 12/12 effective PASS |
| Separate Runtime/Editor/EditMode boundaries | 3/3 PASS |
| C# 9 syntax | 12 modes, zero errors |
| New L3 pure tests | 21/21 PASS |
| Fresh fetched-baseline external NUnit | 2337 passed, 47 failed, 61 excluded, raw FAILED |
| Final L3 external NUnit | 2358 passed, 47 failed, 63 excluded, raw FAILED |
| Baseline failure comparison | Zero introduced/resolved names, zero changed failure reasons |
| L1 / L2 preservation | 51/51 and 65/65 PASS |
| Original ground art/channel/seam/import/source guard | 60/60 PASS |
| Actual Blender source reopen | 10 referenced maps resolve; both ground families and three unchanged L2 objects present |
| Native Unity import/shader/Test Runner/PlayMode/saves/devices/screenshots/Profiler/Windows | NOT RUN individually |

The full-tests.json report retains all 47 exact failing names, native-call messages and stacks. The baseline and current raw NUnit XML/logs are compressed alongside it. These inherited failures are not converted to passes; the two added NeedsEngine cases account for the exclusion increase. They cover prepared-map fallback and material identity, plus all four L2/L3 combinations with an actual collider and L1 scoped lease. Those native cases were compiled, not run.

The initial compile found an invalid scalar finite-helper call. The initial ground tests found a pinched noisy border; the implementation now normalizes the boundary field into local ground metres. Development reports preserve both findings. Independent map checks also rejected non-unit normals after image resampling; the authoring pipeline now renormalizes the normals, and all 60 final checks pass. No test threshold or owner guard was weakened.

Three final compiler processes were terminated by the host with exit -9 while parallel checks were running. Their exact captured response files were rerun serially with the same source and references, all exit 0. managed-api-external.json and managed-api-bundled.json retain the original rows and sequential_retry details. Effective PASS means the final same-source attempt passed, not that the original host termination did not happen.

The two H5 assertions, Original actor/gameplay authority / Characters/HumanoidPose.cs and All prior tests and exclusions preserved, fail identically at the exact fetched baseline and current source (46/48). Both reports are included. These assertions remain unchanged. The frontier guard passes 15/15. A GitHub job skipped after H5 is not reported as a CI pass. Current remote run IDs and final publication/LFS state are in PR12.

DCC comparisons are labeled Blender source evidence. Before is a palette approximation, not a Unity procedural shader reproduction. Source images cannot establish Unity shader compilation, contacts, route safety, frame pacing, actual saves or owner visual acceptance. Production default activation is not authorized.

## Focused preservation repair

The fresh final-source repair results, raw artifacts, 147-file audit and inherited failures are in [Repair/FINAL_STATUS.md](Repair/FINAL_STATUS.md). The earlier results above remain historical. Native gates remain NOT RUN.
