# Preservation, limitations and recovery

Branch `feature/the-veil-remembers-04l`; draft PR [#7](https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/pull/7) targets preserved 04K `df9072930596202b2a8f0fc23c9499f76faada6a`. Latest tested gameplay source `c6aca60ca18634c120ebf0798643856a1c761cbc`, tree `d4be4008813635c5e47ce9beb71a29ceff4634dd`, C# snapshot `21fd7724c9e58228d6cf6d351dda08da8a20f05cb784aaa6d40f40880b0a645d`. Later evidence/handoff commits retain this source snapshot; resolve final HEAD from PR #7 or git. **Production sprint PARTIAL; native-unverified.**

The source audit pins exact 04K. All predecessor scene/prefab/binary-art/metadata files, quest/event JSON, packages/project settings, save declaration files, original controls, rig layout and item enum/catalog remain unchanged. Existing map geometry is retained; new source geometry is additive, collider-free and owned by original batch/mission lifetimes. All 1,733 LFS payloads were freshly hashed; no pointer was accepted as an asset.

Remote readback preserved main 9acbfefcea753058766d9b0ab65cae4c16f0b91c, 04J 221cefe8fa2fb3e7f9c927f6a2eeb7399ab1eb75 and 04K df9072930596202b2a8f0fc23c9499f76faada6a. PR #6 remains draft/open targeting04J; PR #7 is draft/open targeting04K. No merge, force push, reset or rewrite was performed.

Additive persisted identifiers: quest `uh05_04l_01_veil_memory` and existing-block flag `uh05_04l_witness_defeated`; optional PlayerPrefs setting `subtitle_size_04l` defaults1.0. No new SaveData/EventSave/GuidanceSave/LootSave fields or migration. Original completed quests, gear UIDs, owned bonds, focus IDs17–22, cells/charge/heat, fusions and both First Law outcomes retain their original authority. The new quest pays only via the existing completion ledger and denies retry loot for its exact encounter owner. Native persistence still requires a copied-old-save test.

Fresh continuation: existing Pause/menu/runner/input/pose owners were corrected without new records or bindings. AF_UNIX bind is denied on this host; licensed native startup remains blocked. See CONTINUATION_2026_10_19.md and Evidence/resume-* for current tests; earlier asset export evidence is retained at its original source checkpoint.

Material open risks:

- Facial lids/brows/lips and held lantern have no native deformation/clipping review. CPU blendshape arrays and GPU cost are unprofiled.
- Conditional contact placement checks ground/capsule/LOS but needs real building/vehicle/door/camera review. If placement is refused, source intentionally retains ordinary dialogue rather than fabricating an inaccessible actor.
- Cinematic health/threat/movement guards and cleanup have managed checks plus five authored/API-compiled native camera/lifecycle cases, but real pause/death/load/device transitions remain unplayed.
- Arrival tracery and set dressing add render work. Low detail is cheaper; no FPS/GC claim is made.
- The optional investigation's routes, companion cooldowns, stair/courier behavior, Shade balance, duration and calm aftermath need a complete native run.
- 04K's pre-existing unplayed Fractured Bell, natural city mobs, physical caster pickup/full backpack, original main missions and old saves remain acceptance blockers. No source test closes them.

Recovery: keep the existing project and back up real saves outside the checkout. Preserve local changes before git switch/pull; use only fast-forward updates. Work on the04L branch, leave04K/main intact. Import/test on licensed Unity 6000.3.16f1, retain the first Console error and fix it before a build. If a real regression appears, make a new corrective commit; do not force-reset the user's work. Do not advance04M on an assumed native pass.
