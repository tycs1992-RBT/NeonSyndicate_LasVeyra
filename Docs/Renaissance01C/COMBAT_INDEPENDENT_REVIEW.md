# Combat and starter safety independent source review

Status: completed source review. Unity compilation and live playtests were not run.

## Concrete blocker corrected

The new quiet-ambient branch in `AI/NpcController.Creature.cs` returned an unprovoked humanoid creature home and cleared its target without cancelling its separate `CreatureAbilityKit`. A queued Blink could then dereference the cleared target, and a queued Slam could still damage the player after disengagement. The branch now calls `CreatureKit.Cancel()` before `ReturnHome(false)`.

The existing cancellation method clears the pending ability, releases its warning ring, ends its animation, and prevents its damage event. The edit is restricted to the new quiet-ambient branch. Explicit mission actors, persistent creatures, planned attackers and directly provoked creatures remain outside that policy.

## Reviewed boundaries

- `StarterSafety` caches the established home, clinic and Manor positions and resets its local provocation timers on a new session. Its four protected areas do not clear wanted stars or saved gang heat.
- Ambient gang heat alone cannot acquire the protected player. Direct hostility remains active, and the existing damage response retains retaliation.
- Ordinary actor damage explicitly provokes the area even when sanctuary practice noise is being suppressed.
- Humanoid and non-humanoid ambient creature callers distinguish unprovoked ecology from authored combat. The newly added cancellation prevents an already-pending humanoid ability from surviving the safety transition.
- NPC firearms and creature ranged attacks share the existing difficulty-dependent ranged allowance. Story difficulty disables denied-token suppression shots; sniper preparation and final firing both request a token. Existing melee limits remain separate.
- New difficulty and safety rules are pure policy helpers with authored EditMode contracts. Those test files were inspected but not executed.

No additional concrete blocker was found in these changed combat and NPC safety paths. This is a bounded source review, not proof of all live encounter behavior. Police dispatch, vehicles, habitat lifecycle, and architecture have separate audit reports.

## Required Unity checks

1. With ambient humanoid wildlife preparing a Blink or Slam, enter a protected starter area without provoking it. Confirm the warning cancels, no pending damage occurs, and no null-reference exception appears.
2. Repeat with a directly provoked or mission-owned creature. Confirm the authored encounter is not cancelled by the ambient policy.
3. At Story difficulty, verify a mixed group of firearm NPCs and ranged creatures obeys the shared ranged allowance while normal melee behavior still works.
4. Run the five-minute peaceful-start acceptance test on both Free Roam and post-prologue starts, then deliberately attack an ordinary actor and confirm the expected consequences still occur.

All four Unity checks remain **NOT RUN**.
