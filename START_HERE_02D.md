# Build 02D: The Living Mythical City

This is an automatic visual update to the existing game, not a new project or a manual prefab demo.

## Play the update

1. Close Unity before updating files. Keep GitHub Desktop on `art/crystal-fantasy-block-proof`.
2. Fetch origin, then Pull origin. Reopen the same local project: `C:\games\NeonSyndicate_LasVeyra`.
3. Let Unity 6000.3.16f1 finish compiling, press Play, and choose Free Story Mode.

There is no Art menu step, no required new scene and no required new game save. Existing saves use the rebuilt environment when the city is generated. No package or Editor version changes are included.

## What to look for

The original selected Gothic buildings now use three geographical art styles:

| City area | Art style | Visible change |
| --- | --- | --- |
| West, x below -180 | Verdant Reach | Green mineral stone, hanging vines, planted roof canopies, flowers and ferns |
| Centre, x from -180 through 240 | Amethyst Ward | Violet minerals, faceted roof jewels held in curved cages and tilted gold rings |
| East, x above 240 | Dawnstone Court | Lighter warm stone, gold details, sun-disc roof rings and taller ribbed crowns |

All three add radial stained-glass panels, facade medallions and elevated crystal lanterns. These are art styles, not new gameplay factions. Existing detailed-building selection and graphics budgets still apply; not every building is replaced.

Selected existing parks now have corner groves containing branching trees, folded leaves, ferns, crystal flowers and an open ring sanctuary. The central paths are left open. Placement rejects pockets near recorded mission POIs, pickups, parking and walk nodes. Some parks intentionally remain unchanged.

The existing ordinary roadside trees, cars and characters have NOT been replaced in this sprint. Do not judge whether the update loaded by looking at those alone. The new sanctuary is scenery, not a usable teleport gate or a new creature encounter.

## Confirm that the right version loaded

In the Unity Console, look for:

`Crystal City 02D architecture is active.`

When a park qualifies, another message begins:

`Crystal City 02D is active: three architecture styles`

The Hierarchy names of transformed buildings begin `Crystal City 02D |`. These logs and names are version checks, not proof of rendering quality.

## What was preserved

Road layout, vehicle physics, gameplay collision generation, controls, camera, difficulty, police, quests, creature logic, save data and existing generated assets were not modified. Your committed `CrystalGardenDressing.cs.meta` was retained byte-for-byte.

## Testing status

Offline source-contract checks and independent placement arithmetic were run. Unity compilation, EditMode tests, actual screenshots, visual quality, gameplay and performance remain unverified here. The new EditMode tests are provided for local execution, not reported as passed.

After pulling, the first useful check is: the game compiles, the same street has the new glass and roof landmarks, and movement/driving still work. See `Docs/BUILD_02D_QA.md` for a fuller checklist.

## Rollback

Keep this on the art branch until approved. Close Unity and preserve any uncommitted local work first. To undo only 02D, revert its single commit on the same branch; do not hard-reset the project or discard local assets. Earlier art work remains in history. No merge into main is part of this update.
