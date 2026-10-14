# Police and wanted response audit, Renaissance 01C

Scope: `AI/PoliceManager.cs`, `AI/WantedSystem.cs`, one automatic-bike call in `Vehicles/TrafficManager.Bikes.cs`, new engine-free `AI/PoliceResponseRules.cs`, and `Tests/EditMode/PoliceResponseTests.cs`. Foundation: the preserved 01B project. No quest data, mission scripts, crime-emitting combat code, inputs, UI, saves, render assets or character health were changed by this work.

## Starting locations

The story opening is not on the art demonstration street. `StoryMissions.Build` resolves the CrystalManor site at `(470, 0, 1470)` and `PrologueLayout.PlayerStart` adds `(0, 0, -8)`, giving `(470, 0, 1462)` before the ground placement step. Free Roam uses `GameRoot.HomeSpawn`, the generated Veyra Lofts safehouse entrance and its facing. Start after the prologue uses `PrologueLayout.ClinicStand`, 3.5 metres out from the clinic doorway. Loaded saves use their saved position, with the existing invalid-position fallback. Lofts and clinic coordinates must come from the generated layout, not a hard-coded art-district centre. Their exact world positions were not executed in Unity here.

## Problems found

- Every automatic dispatch tick, originally every 2 seconds, could recruit every nearby patrol, spawn another cruiser, and spawn a two-person foot squad.
- Recruitment had no car or officer budget. The foot limit counted only officers already outside cars, so car occupants became additional pressure after deploying. A two-person squad could also overshoot the remaining foot allowance.
- Roadblocks created four more officers through a separate path. Automatic traffic bikes also used the unrestricted explicit-spawn entry point on their own timer.
- Delayed witness calls could finish after a mission enabled crime suppression, or while gameplay was not in the Playing state. Clearing wanted status discarded pending-call records without releasing the associated civilian phone state.
- Police dispatch itself was already dormant at zero wanted stars. Passive walking does not create wanted heat. The broader ambient-aggression problem belongs to the starter-safety and NPC changes, not a blanket immunity inside wanted logic.

## Changes

Automatic patrol recruitment, newly dispatched car crews, automatic traffic bikes, foot squads and roadblock officers now share a nearby ground-officer allowance. Seated managed officers count before they deploy. Recruitment must fit both the car allowance and the entire recruited crew. A foot squad is limited to the available slots. Roadblocks wait until all four officer slots are free. This constrains new automatic response; it does not delete visible officers or cancel explicitly authored mission spawns when the difficulty changes.

| Setting | Story | Balanced | Veteran |
|---|---:|---:|---:|
| Automatic car allowance, 1 to 5 stars | 1 / 1 / 2 / 2 / 3 | 1 / 2 / 3 / 4 / 5 | 2 / 3 / 4 / 5 / 6 |
| Nearby ground-officer allowance, 1 to 5 stars | 2 / 3 / 4 / 6 / 8 | 3 / 5 / 7 / 9 / 11 | 5 / 8 / 11 / 14 / 17 |
| Initial automatic dispatch delay | 5 seconds | 3 seconds | 1.5 seconds |
| Automatic dispatch interval | 5 seconds | 3.5 seconds | 2 seconds |
| Crime heat accumulation | 0.65x | 0.85x | 1x |
| Unseen escape progress | 1.35x | 1.15x | 1x |

Officer accounting covers all living managed ground officers, including distant crews that could return to the chase, plus ambient foot police within 240 metres. Existing ambient foot police consume room, while an unrecruited traffic crew is counted when it joins. Automatic traffic bikes use a separate guarded entry point and cannot bypass the initial dispatch delay. Air support thresholds and mission-requested units retain their previous behavior. Serious crimes retain their existing minimum wanted stars, and explicitly set wanted stars are unchanged. At one star on Story, the normal automatic response fits one two-person cruiser or the same two slots on foot.

Pending calls now pause outside active gameplay and cancel when ordinary crime reporting is suppressed. Civilian phone state is released when calls cancel. Completing calls recheck the existing creature-noise exemption. `ClearAll` also clears stale sighting and gunfire timing.

Ordinary reported crimes notify the shared `StarterSafety` system after existing creature and mission exemptions. Aiming alone does not invoke this notification. This hook preserves consequences for harming a civilian or police officer during practice. No global training immunity was introduced. Existing creature victim tags, direct creature-hit detection, and nearby creature-fight noise rules remain in force. The opt-in trainer is an existing nonhumanoid creature, so no new humanoid exemption is needed.

## Preserved intent

`WantedSystem.SetStars` and the public four-argument `PoliceManager.SpawnPursuitCar` retain their requested stars and full authored crew. The latter delegates to a private crew-limited overload only for automatic response. Frozen wanted state still prevents evasion. Crime types, star thresholds, minimum stars, arrest rules, weapon selection, line-of-sight witnesses, story requests, mission flags and save schemas are unchanged. No default-setting or save migration is implemented in these files.

## Verification

Five changed or new C# files passed tree-sitter grammar parsing with no syntax errors. They contain no prohibited dash characters. Source inspection checked every changed dispatch path and the public mission entry points. Evidence is in `POLICE_STATIC_CHECKS.json`.

Eight engine-free NUnit cases were added: five minutes without crime, repeated mixed dispatch attempts, partial and over-budget crews, star and difficulty ordering, slower minor-crime escalation with serious consequences retained, explicit frozen mission wanted, unseen-only faster evasion, and delayed-call gating. These tests were written but not executed: this environment has no Unity editor or C# compiler. Syntax parsing does not prove API resolution, scene behavior or gameplay quality.

Required Unity checks: walk each actual starting area for five minutes without combat; complete the opt-in creature practice without wanted stars; harm a civilian or police officer and confirm consequences; hold a one-star Story pursuit and count recruited and deployed ground officers; lose line of sight and confirm escape; enter a crime-ignored mission with a pending witness call; exercise story missions that request wanted levels or police cars; load an existing save and repeat with all three difficulties. Confirm pause, death, new-game and domain-reload-disabled resets do not retain old response timers.
