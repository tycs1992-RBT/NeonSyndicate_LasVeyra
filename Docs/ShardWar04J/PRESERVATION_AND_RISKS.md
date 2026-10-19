# Preservation ledger, risks and rollback

## Preserved source contracts
Branch parent exact 04I 1b0f8275ad3952784939168a80e704f22863cf69; new branch feature/shard-war-horde-renaissance-04j. main, 04H, earlier PR #4, original mission identifiers, existing WeaponId enum/order, loot-unique indices 17..22, six original crystal focus signatures, all previously earned unique entitlement paths, Saved UID/items, SaveData/LootSave/CreatureSave schema, quest uh05_* IDs, bonded quartz_guide/ash_warden/storm_skimmer, three earned fusion IDs, Luma/Underheart, graphics presets, vehicle and city coordinates were NOT modified.

04J touched four runtime .cs files, two pre-existing test .cs files, one legacy source-check script, and added a new source-check script, CI workflow and handoff documents. No .meta, prefab, scene, package manifest, ProjectVersion, LFS-tracked art or binary was modified by these commits. New runtime diagnostic fields are not serialized.

## Main risks
- Unity never opened: source CI syntax is not Unity API typechecking. Test on exact pinned Editor and record first issue.
- CityRift 28 attempts can increase failed-spawn raycast work over the 04I 16 attempts; inspect live counters and Profiler before tuning.
- Ground and roof, road/building and mesh readiness can still cause zero accepted spawns; current F2 ZoneOrRoad is an aggregate, not per-layer physics trace.
- Random unentitled caster Mythic drops are bounded down to Legendary because no legitimate general Mythic caster exists. This deliberately changes random loot rarity for eligible crystal enemies.
- On defeat, physical pickup collision, short time-to-live, full inventory and same-UID persistence are still native-unverified.
- New F2 text may clip at low display scale; test layout and pad navigation.
- Broader 04J visual overhaul, story/quest episode and all 20 modules remain unfinished; do not merge merely because CI is green.

## Safe rollback
In Windows Git fetch origin, commit/stash local edits if any, and switch back to the untouched origin/feature/crystal-city-ascension-04i checkout. Never rewrite the saved-game files or forcibly move the 04I/04J remote head. The new 04J branch can remain open as a draft PR while fixing native issues. Save backup should live OUTSIDE the repository/project.
