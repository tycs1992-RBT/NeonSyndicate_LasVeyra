# 32-item acceptance: no native blanket pass

PASS entries below are limited to their source/repository test. Every gameplay requirement remains BLOCKED until its stated native test runs. This is not 32/32 accepted.

| Item | State | Evidence / next reproducible action | Owner |
|---|---|---|---|
| 01 | PASS | 04K descendant / source refs; final readback in handoff | Release |
| 02 | BLOCKED | Exact Editor licensing and UPM IPC; no import result | Unity |
| 03 | PASS | LFS 1733 hashes/fsck, 1665 metadata; native import coherence unverified | Build/QA |
| 04 | BLOCKED | New spawn-path source integrated; zero native frames | Art/QA |
| 05 | BLOCKED | Skin/weight/budget external passes; clipping/deformation not rendered | Art |
| 06 | BLOCKED | Corin/Tamsin geometry source; native original interactions untested | Cast/QA |
| 07 | BLOCKED | Role skins and champion timing source; native combat untested | Combat |
| 08 | BLOCKED | Mite/Jackal geometry tests pass; physical bodies/AI not playtested | Creatures |
| 09 | BLOCKED | All three earned-stage skins tested externally; live ownership/motion untested | Bonds |
| 10 | BLOCKED | 04J placement/F2 preserved; no day/night accepted-placement evidence | Ecology |
| 11 | BLOCKED | Original habitat tables unchanged; no native observations | Ecology |
| 12 | BLOCKED | Original caps/token owners preserved; runtime stress untested | Combat/QA |
| 13 | BLOCKED | M00/Luma source untouched; native mission untested | Story |
| 14 | BLOCKED | M03 source untouched; native fight/retreat untested | Story |
| 15 | BLOCKED | M08 source untouched; native transmitters/roles untested | Story |
| 16 | BLOCKED | Finite champion source/timing tests pass; actual victory/reward untested | Combat/Quest |
| 17 | BLOCKED | Live cosmetic motion integrated; original hit/defense tests external only | Combat |
| 18 | BLOCKED | Six cast poses, original signatures/IDs; native activation/interception untested | Weapons |
| 19 | BLOCKED | Caster seeded/save and receipt tests pass; physical pickup/equip/reload not played | Loot |
| 20 | BLOCKED | Two paid gated recipes/old recipes preserved; real crafting transaction untested | RPG |
| 21 | BLOCKED | Existing bond/fusion systems retained; native travel/doors/reload untested | Bonds |
| 22 | BLOCKED | Existing shelter/de-escalation preserved; no live new routine pass | City AI |
| 23 | BLOCKED | Two original enterable venues decorated; exact native routes untested | World |
| 24 | BLOCKED | 23-step checkpointed route validates; no complete native run/retry/save | Quest |
| 25 | BLOCKED | Original source/IDs/data preserved; no native old-save regression | Save/QA |
| 26 | BLOCKED | Existing E/Y, menu/tab bindings reused; no hardware controller run | Input |
| 27 | BLOCKED | Reduced fragments/camera controls preserved; perceptual effects not rendered | Accessibility |
| 28 | BLOCKED | No native matched screenshots or clips | Art/QA |
| 29 | BLOCKED | No native Profiler; geometry counts are not performance | Performance |
| 30 | BLOCKED | No Windows build or launch; native compile gate not reached | Build |
| 31 | PASS | 7 new executable NUnit cases pass externally; full native behavior remains open | QA |
| 32 | PASS | Published checkpoints and draft PR #6 target 04J; final refs readback in handoff | Release |

For all native-blocked rows: run START_HERE_04K on a licensed Unity 6000.3.16f1 host, resolve the first actual import/Console error, execute EditMode/PlayMode then the normal physical route on new and copied legacy saves. Log results per row before changing it to PASS. Native screenshots/performance/Windows launch cannot be inferred from source passes.
