# 04I source follow-up: faction stand-down and arrival detail

This file describes implementation after the published 04I source checkpoint. It is not a native Unity acceptance report.

## Changes integrated into current source

1. **Event combat de-escalation:** NPC event fighters now use a single pure eligibility rule when selecting or retaining rival targets. A rival from a different event, a same-side actor, a dead combatant, a finished or paused event, and any actor that has stood down are excluded from acquisition and retained focus. Existing explicit player-retaliation semantics remain unchanged. The same rule controls the actual MayDamage gate and the thinker's opponent choices, avoiding an NPC continuing to swing at a surrendered target after damage was already blocked.
2. **Four arrival venue silhouettes:** Veyra Lofts, Cinder Dispatch, Pressure House and Bondkeeper's Refuge get additional original, noncolliding faceted ceiling keystones and wall-mounted civic service relief above the established usable floor and entrance clearances. The existing near/far LOD selection and graphics-preset costs remain. Low retains its prior visual mesh. These are source geometry, NOT new imported models, rendered screenshots or proof of target visual quality.
3. **Malik hero geometry:** The detailed jacket path now adds a fitted, Chest/Spine-weighted faceted quartz harness and central accent. No bone, character ID, animation clip, collider or save field changes. It is procedural source geometry, not a sculpted hero asset.

## Regression tests added, verification status

- `CityCombat04FTests.StoodDownOrHeldEventActorsCannotRemainTargets` verifies seven target eligibility cases.
- `AscensionArt04ITests.DetailedCivicSigilsStayWithinExistingNearMeshBudget` checks all four venues under Ultra against original geometry budget.
- All pre-existing 04I tests remain present.

**Test execution in this follow-up:** NOT RUN. The present environment has no Unity Editor, C# compiler, Git LFS executable or terminal access to GitHub. GitHub API read/write is available and was used without rehydrating binary files. No new binary LFS objects, .meta GUIDs, packages, scenes, save fields, quest IDs or weapon catalog IDs were introduced. The earlier 1,342 passed / 46 unchanged native-ECall failures / 53 skipped are results for the prior immutable source revision, not for these changes.

## Required next checks on Windows Unity 6000.3.16f1

1. Close Unity before updating the existing checkout; preserve local edits and backup saves. Fetch/fast-forward `feature/crystal-city-ascension-04i`, run `git lfs pull` / `git lfs fsck` using the local installed Git LFS. Do not merge into main.
2. Import and compile the actual project; run `tools/test.ps1` with the pinned Editor. Inspect failures by identity rather than assuming the previous baseline. The immediate new tests are the two methods above.
3. In New Free Roam compare the four venue high/preset interiors and Malik's jacket, especially clipping and silhouette. Trigger an explicit city dispute or handler incident, watch both sides, and verify a stood-down actor is no longer attacked; a passive observer should stay safe. Then run the five 04I chapters and existing 04G/04H content.
4. Profile close-up geometry/render work; compare actual native screenshots and frame times on equal seed/preset. The earlier 04I visual evidence remains baseline-only for this follow-up.

## Deferred

Original 04I incomplete production ambitions remain: externally sculpted textured/rigged hero models, facial blendshapes, cinematic authored animations, broader districts, richer law investigate/search and widespread NPC ecology. This follow-up does not pretend to finish them. Native import, physics, playable 30-60 minute duration, FPS and Windows player builds are not verified.
