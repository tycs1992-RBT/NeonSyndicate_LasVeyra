# Final habitat integration review, Visual Renaissance 01C

Review scope: current habitat, creature damage and attack call paths, lifecycle cleanup, discovery interaction,
and the shared health pipeline. This review was performed from source. No Unity execution is represented here.

## Findings and disposition

| Issue | Final source disposition | Remaining live check |
| --- | --- | --- |
| Burn tick bypassed final lethal callback and scaled damage after the practice filter | `LimitPracticeDamage` checks filtered damage times `Health.DamageScale`, then caps using the same scale. Final direct hits retain `OnLethal` after resonance and other multipliers. `CharacterHealth.cs` is unchanged. | Mark trainer with an amplifying gadget and burn it at low health; confirm no death/reward event. |
| Trainer could kill player | `DealPracticeHit` uses a temporary final lethal callback only around the practice jackal's synchronous bite/pounce damage call and restores the original callback in `finally`. Low-health defeat cancels its attack and the habitat ends practice with feedback. | Lose deliberately with and without armor, dodge and last-stand skills; verify at least one health remains and later ordinary combat remains lethal. |
| Deferred destruction could leave an extra attack or telegraph | `StopHabitatPractice` clears target, calls `CancelAttack`, releases tokens, changes to idle and deactivates before `DespawnTag`. | Cancel during bite and pounce wind-up, cancel at impact, enter a vehicle and leave the marker radius. |
| Winning area hit could aggro spectators after `TrainingActive` became false | Calm residents check `SuppressPracticeNoise`, which spans allocated trainer lifetime and a two-second, 45-metre local cleanup grace. | Win with explosion/resonance/burning; confirm residents remain calm. Direct crimes against ordinary people must still count. |
| Highlight expiry could use a different clock | `Observe` uses `Time.time`, as do `Compendium.CompleteScan` and highlight-expiry readers. Only pulse animation uses unscaled time. | Observe during slow motion and pause; compare with ordinary scan behavior. |

## Exact final patch from this review

Existing pending fixes for the burn multiplier, practice-only player protection, deactivation and finish grace were
already present when this review began. This final pass added `Creature.StopHabitatPractice()` and called it from
`StarterHabitat.StopPractice()`, ensuring an active attack's warning ring is explicitly released before deactivation.
It did not alter shared `CharacterHealth`, species attack tables, normal damage rules, saves, controls or rewards.

The only sparring species is currently Neon Jackal. Its two damaging attacks route through `Deal`, which invokes
`DealPracticeHit`; its howl does not deal damage. Extending the trial to species with projectiles, cones, fire or
other attack paths requires an additional nonlethal review. This implementation is not general player invulnerability.

Calm resident collision and retaliation outside practice remain part of the ordinary creature system. Successful
site placement for all three residents is conditional on actual terrain, obstruction and clearance checks. The
sanctuary may log fewer than three safe sites; the requested discovery time has not yet been measured.

## Checks actually performed

- Read direct-hit and burn damage order, final lethal interception, attack table and all practice cleanup paths.
- Parsed seven habitat-related C# files with tree-sitter: no grammar errors. This does not type-check Unity APIs.
- Ran 20 IEEE float32 arithmetic cases for the burn cap using health 1, 1.2, 3 and 45.5 with damage scales 0.65, 1,
  1.2, 2.5 and 10. Every case retained positive health. This transcribes the expression; it is not a C# runtime test.
- Confirmed `CharacterHealth.cs` is byte-identical to the completed 01B source.
- Wrote source hashes and numerical inputs/results to `HABITAT_SOURCE_CHECKS.json`.

No remaining blocker was identified in this scoped source review. Unity compilation, physics, visuals, input,
callback order, body-job timing and all gameplay acceptance criteria still require the runs in `HABITAT_AUDIT.md`.
