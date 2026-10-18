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

## Crystal creature restoration: source follow-up after `d0622cf` (2026-10-18)

The owner reported that crystal enemies had virtually vanished from both daylight Free Roam and the start of the main campaign. Source audit found two concrete absence causes: `CreatureEcology.Candidates` returned **zero** choices for every city district during daylight, and it returned **zero** choices for generic city streets even at night; `StoryMissions.M00` only created ordinary humanoid hunters after the Third Eye awakening. Existing creature/weapon rewards were functioning in source but were rarely accessible because there were no creatures in those circumstances.

### Actually changed in the published 04I branch

- `CreatureLogic.cs`: daylight/night crystal packs across generic city rifts, Jackals in parks, Mites/Golems at worksites, shades in Grave Frequency. Append-only `SpawnSpot.CityRift` (old enum values unchanged); downtown threat interval is now 20 seconds by day and 12 by night before bounded jitter. All existing weather, body/group graphics caps, species lifetime and combat temperament rules remain.
- `CreatureDirector.Ecology.cs`: collect eligible existing surface-city walk nodes; place packs off-camera, 60-140 metres away, using the old ground/road/roof/arena/mission-objective/physics exclusion checks. Only the first admissible member can be a marked elite; elite chance is bounded by location/time/species; no extra manager or permanent spawn. The original 3/4/6/7 group and 8/12/18/22 body-weight graphics caps remain unchanged.
- `StoryMissions.cs`: the original Third Eye prologue now uses real blade/caster crystal enemy roles among the mission hunters and adds a tagged 6-mite/2-Jackal pack after the hunters. The story blockade includes Sentinel/Duelist roles. `m03_false_kings` adds a tagged 9-mite/3-Jackal group and one 2.5-health-multiplier elite Alpha after the king, with a **maximum 20-second fight-or-retreat** window. Neither optional creature wave softlocks a story mission or awards completion without its original objectives. Existing MissionManager cleanup calls `OnCleanup` and only the precise chapter tag is despawned on pass/fail/abort/retry. Old saved mission IDs stay intact.
- `LootArmory.Drops.cs`: actual player-killed non-mite Outer City threats have a minimum 12% focus-drop chance; elites 60%; great beasts 72%; ordinary mites retain their original tiny drop chance. Crystal-armed nonelite NPCs have a 30% focus chance; original elite/champion sources remain authoritative. This is one `DropRolled` call per eligible kill through **existing** rarity, pity, live-drop caps, weapon UID, backpack, pickup and upgrade systems. Materials, Lumen and gear rolls are otherwise unchanged. Practice/exhibition targets and ambient friendly residents still use existing `NoRewards` gates.
- `CreatureLogicTests.cs`: updates the historical contradictory night-only assertion; authors day/night/generic-city and elite/focus-policy assertions. New tests are **authored, not executed** in this environment.

### Non-negotiable retained safety

- The Lofts, clinic, Manor and promenade welcome-area policy suppresses unprovoked ambient aggression. Explicit mission creatures and direct provocation retain their existing rules; these areas are not rendered universally invulnerable.
- Passive observers and ordinary civilians retain faction protection; body/attack tokens limit concurrent strikes. No infinite local police waves or new faction hostility inheritance.
- City packs are transient and capped, off-camera, on walkable surfaces, and independent of mission-owned chapter creatures. Mission creatures are tagged and cleaned up with their own mission only.
- A tiny Mite cannot become a high-yield focus farm; elite chance is zero for Mites, bosses and Great Beasts. Boss rewards and existing unique indices are untouched.
- No change to Unity packages, pipeline, scenes, existing prefabs, binary assets, `.meta`, save declarations, weapon IDs, quest IDs, bond IDs, or player ownership.

### Actual verification boundary

GitHub API source writes/readback and the GitHub compare endpoint confirm published edits on 04I. All edited C# files passed a local static delimiter-count audit of braces/parentheses/brackets. **No native Unity import, EditMode test execution, gamepad, material render, PlayMode, save/load, LFS hydration or FPS profiling occurred for this follow-up.** The historical 1,342-pass external NUnit result belongs to the earlier source checkpoint, not this change. Current test pass/fail count is unknown.

Next: Use the exact Windows route in `START_HERE_04I.md`. Test daytime generic city spawn **outside** the welcome circle, night creature packs, Chapter M00 wake after the hunters, M03 timed pack, elite item pickups and original old saves. Inspect Console first, then tune ecology thresholds only with reproducible native play evidence.
