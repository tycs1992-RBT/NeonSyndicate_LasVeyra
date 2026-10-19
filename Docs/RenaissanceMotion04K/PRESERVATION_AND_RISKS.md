# Preservation and remaining risks

Base 04J: `221cefe8fa2fb3e7f9c927f6a2eeb7399ab1eb75`. New branch only: `feature/renaissance-in-motion-04k`. PR #6 targets 04J. No branch merge, force-push, reset, package upgrade or renderer migration. Original main, 04J and earlier drafts remain intact.

Original scenes/prefabs/art, all pre-existing .meta GUIDs, original quest/event JSON, SaveData/LootSave/CreatureSave declaration files, WeaponId enum/order, focus indices 17–22, M00–M11, Underheart story, 04G courier, 04H Quiet Circuit and all five 04I chapters are unchanged. The existing reward ID array is append-only: `uh05_04k_recovered_caster` is added after the original 32 entries. One new optional quest, one saved string flag and two recipe IDs use existing ledger/save structures; no new serialized field or migration. Existing canonical bonds and fusion IDs are unchanged. Source field/ID preservation is not proof of a native legacy-save round trip.

Source tests verify normalized skin streams/budgets, deterministic caster records/entitlement exclusion, no duplicate ledger claim after full-backpack/reload simulation, finite phase timing and new interior clearance. All 1,733 hydrated LFS working payloads were hashed against their OIDs. Old assets are not silently replaced with pointer files.

Main native risks: large drake corridor fit/grounding, safe spawn post, camera clearance, actual attack/pose contact, door/stair travel, prompt competition, old/new save checkpoints, reward pending delivery, native mesh/material lifetime, day/night CityRift success and actual CPU/GPU/GC/draw costs. Dense outfits intentionally omit additions when the original budget has no room. The hero/cast changes are procedural geometry; imported production sculpture and authored clips remain undone. The new faction investigation scope is deferred.

Recover safely by closing Unity, backing up saves outside the checkout, checking git status and switching to the untouched 04J branch if necessary. Do not erase saves, reset branches or merge main to make the draft look green. Fix the first real native error on a licensed supported host before 04L expansion.
