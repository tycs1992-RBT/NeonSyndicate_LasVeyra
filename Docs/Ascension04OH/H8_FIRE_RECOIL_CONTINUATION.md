# H8 firearm recoil continuation

This is an incremental implementation on the existing H8 Living Hero branch, not a replacement for the H8 delivery or an owner-accepted character.

## Exact source ancestry

- H7 delivery: `556ac54f8b15993fc208f2932166f5059315310d`
- H8 handoff at start of continuation: `5a2b6660e7e71900e0cf09a6cd7af25708e7d658`
- Authored recoil selection: `3e0f0f230a2f25d22f1c61f96212b332cf165a88`
- Motion review integration: `26a873f0575511687defdb753906a5cb2e0ae907`
- Regression test implementation: `60311f723eb46c53859aefed9fdf5ae7c0c96054`

## Source integration delivered

The existing `H8_FireRecoil` authored animation from the real H8 character action set now participates in the same reversible Editor full-body review as H7 idle, walk, run, ready and OathCut and H8 dodge, cast, gun aim and earned guardian cooperation.

The `MalikMotionH8.Select` function prioritizes existing dodge, earned companion state and interruption requirements before an actual weapon recoil pulse. A genuine held firearm and an existing positive recoil state are required, so unarmed and blade states cannot select this clip. Reload, guard, stagger, travel and other protected states still preserve the original pose owner.

The original `HumanoidPose` samples the authored recoil motion using per-review clip timing. A new recoil rise restarts the visual clip for follow-up gunshots without generating a new gameplay fire event. The new sampled visual is limited to the upper torso with original right and support hands, feet, pelvis, weapon IK and muzzle authority preserved. This does not modify ammo, hit registration, resources, aim, inputs, camera, missions, inventories or saves.

Two new NUnit tests exercise real-clip selection, actual pose review, recoil-to-aim recovery, repeat recoil, reload rejection, dodge priority, rollback, and rejection of unarmed/bladed recoil. They are written source tests and are not a claim of successful test execution until a runner or GitHub CI actually reports results.

## Scope and acceptance

- New anatomical sculpt, new texture work and costume reauthoring: NOT DONE in this incremental checkpoint.
- Source changes: COMMITTED to draft PR12's existing branch.
- Managed NUnit and complete twelve-configuration matrix on this incremental source: NOT RUN in this workspace.
- GitHub art-proof source-validation run: check its separate CI result for this commit.
- Actual Unity 6000.3.16f1 native import, EditMode, PlayMode, quality review, controller and keyboard, save and reload, real screenshots, profile and Windows build: NOT RUN.
- Owner appearance approval: NOT GIVEN.
- Normal player Malik remains the original protected fallback.

## Owner native check, no C# editing required

In GitHub Desktop, fetch then pull the H8 art branch with working Git LFS. Open the project using Unity 6000.3.16f1. Use the existing menu `Neon Syndicate > 04OH > H8 Living Hero gameplay review`, prepare the actual H7 candidate in EditMode, and select `Title > Gothic Civic H5` revision 10 for normal PlayMode. Begin the reversible review.

Aim and fire a legitimately owned gun. Confirm a short torso recoil, stable muzzle and hand attachment, preserved ammo and hit timing, and restoration of the aim pose. Repeat shots quickly, fire while moving, reload, dodge during recoil and interrupt with stagger, then verify the fallback restores on Revert/close. Capture actual Unity screenshots/video and Console logs, not Blender source renders. Test with both keyboard/mouse and controller. If actual import or PlayMode fails, retain the fallback, record the first full Console error and do not declare native acceptance.

The larger H8 production gates remain open: final facial/groom/costume quality, deep-pose cloth/weapon/foot contact, remaining authored traversal clips, native performance/build and owner approval. PR11 and world/save banks 0 to 10 are unchanged.
