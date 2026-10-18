# Neon Syndicate — 04G Combat Responsiveness Checkpoint

Branch: `feature/crystal-combat-living-city-04g`  
Starting 04F parent: `747624335141a18031091f62737c73fe7f8969b7`  
Engine to use: **Unity 6000.3.16f1**, same existing project and existing render-pipeline configuration.

## Pull and verify

Close Unity and back up your latest game saves and local changes before switching branches.

```powershell
cd C:\games\NeonSyndicate_LasVeyra
git status --short
git fetch origin
git switch feature/crystal-combat-living-city-04g
git pull --ff-only origin feature/crystal-combat-living-city-04g
git lfs pull
git lfs fsck
git rev-parse HEAD
```

If `git status --short` reports local work, preserve it first. Do not discard or overwrite Unity scene/settings files. This branch is based on 04F; do not merge it into main.

## Native verification order

1. Open **the existing project folder**, not a blank Unity project, in Unity 6000.3.16f1. Record all Console compile/import errors before Play Mode.
2. Run `powershell -ExecutionPolicy Bypass -File tools\test.ps1` from the repository root. Keep the actual EditMode output; authored tests are not evidence they passed.
3. From a **new Free Roam** start, equip the Asterblade and Riftfang Pair. Tap light attacks during early and late portions of another swing. Confirm a valid follow-up occurs only after the current move's permitted cancellation point.
4. Equip Basalt Crown. During the maul's long windup, buffer the next attack and confirm it no longer disappears because the old 0.5-second queue timed out. Confirm stamina cost, six-second shatter recovery and existing guard/poise rules have not changed.
5. During a forward dodge, press attack after approximately 40% of the roll. Confirm a roll strike begins only near the end of the animation (after 78%), never during invulnerable travel. Confirm backstep behavior is unchanged.
6. Find a `CrystalDispute` or `DrakeRescue` event. Observe duelist targeting through close crossings and deaths. Actors should not oscillate rapidly between near-equal targets, retarget dead rivals, switch sides, attack passive bystanders or attack the player for watching.
7. Observe an event drakeling. It should seek a lateral position beside its handler during a fight without walking through walls. It must not attack a neutral player or register a player bond.
8. Observe repeated NPC crystal volleys for several minutes. Inspect projectile counts and performance with the Profiler; check that disabled pooled objects are reused, impacts still stop at walls, saturated casts fail closed, and active bolts disappear at a session reset.
9. Reload an **existing pre-04F save**, recheck bonds, missions, gears, charge, focus weapons and all original 04E–04F routes. End play, reload again, and compare.
10. Capture same-seed 04F-versus-04G before/after footage. Record wall collisions, CPU allocations, frame time and any missing visual/character quality improvements before claiming a visual or performance win.

## Changed source

- `Combat/MeleeMoves.cs`, `Characters/PlayerCombat.cs`: duration-aware buffered combos and dodge-attack responsiveness
- `AI/CityCombatRules04F.cs`, `AI/NpcCityActor04F.cs`: local faction-opponent focus hysteresis
- `AI/NpcBondedDrake04F.cs`: small combat flank with a wall check and handler-only damage authority
- `Combat/NpcCrystal04FProjectile.cs`: session-cleared, bounded, reusable transient bolt GameObjects
- Existing EditMode test files: `CrystalCombat04FTests.cs`, `CityCombat04FTests.cs`

## Limitations — not yet validated

No native Unity import, compiler, Play Mode, EditMode execution, Windows build, screenshots or measured frame-time comparison has been performed in the implementation environment. No new skinned character models, animation clips, authored quests, independent damageable event drake, additional indoor venue or new story content is included in this checkpoint. Existing visual quality, save compatibility and in-engine usability must still be verified, not assumed.

This is an actual **04G source checkpoint**, not a declaration that every proposed 04G feature or a shipping game is complete.
