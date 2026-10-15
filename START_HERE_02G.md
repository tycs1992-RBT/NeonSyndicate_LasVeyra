# Build 02G: Crystal City Discovery

This installs over Build 02F in the existing Unity project. It adds playable, optional exploration activities to the three landmark regions. It does not replace the approved environment, add enemies, reset your save or require manual prefab placement.

## Apply and play

1. Close the Unity Editor. Extract the update ZIP and double-click APPLY_02G.cmd.
2. Confirm C:\games\NeonSyndicate_LasVeyra and type Y. The installer expects the current art/crystal-fantasy-block-proof branch and the 02F files. If validation stops, do not force an overwrite.
3. Open the same project in Unity 6000.3.16f1. Let it import/compile, press Play and choose Free Story Mode.

Use the ZIP, not Fetch/Pull, to install this release. It has not been pushed to GitHub. No new Unity project is needed.

## Your new gameplay loop

Open the full map and look for **D** markers. They point to ground shrines near the landmark regions, not the floating platforms. The older * markers still show the decorative 02F landmarks.

- Approach the MEMORY CORE on foot. Use E, or Y on the controller layout (the existing north-face interact button).
- Read the inscription. It gives an order involving **SUN, ROOT and STAR**.
- Walk to each named sigil and interact in that order. The matching glyphs light up and play short chimes.
- A wrong sigil only resets that shrine's three-step sequence. You lose no resources, health or money.
- Return to the MEMORY CORE when the sequence completes. Claim the story memory and its reward.
- Finish and claim all three shrines, then use a core for the one-time circuit reward.

Pause > DISCOVERY keeps the clues, unlocked memories, reward status and a waypoint button. Clues have no time limit. You can reread them and complete the sites in any order.

## Authored rewards

| Shrine | Lumen | Crafting material |
| --- | --- | --- |
| Aster Crown: The First Light | 160 | 1 Volt Core |
| Dawnspire: A Promise Kept | 180 | 1 Ember Core |
| Moonroot: Where We Belong | 200 | 1 Void Core |
| All three claimed | none extra | 1 Prism Core |

The materials enter the existing satchel and can be used by the existing Forge systems. These are not new spells, creatures or an economy rewrite. Reward credits are one-time per save/playthrough; repeated clicks do not intentionally farm them. A full material inventory leaves that material pending instead of deleting it.

## Save before leaving

Use the existing SAVE / LOAD menu before quitting or stopping Play Mode. The new activity state is included in the selected game save. The update does NOT secretly overwrite an autosave or persist progress merely because you stopped Play Mode.

Old saves without a discovery block begin with unvisited shrines. A new game resets this activity. Wrong-rune attempts and partly paid rewards are restored from the activity block. Chosen shrine tracking is remembered; click Track again to recreate a player waypoint after loading.

## Verify the update

The Console reports `Crystal Discovery 02G | Ready. n/3 ground shrines.` Actual safe placements determine n. The scene hierarchy has `Crystal City Discovery` under Systems. The pause menu has DISCOVERY.

If fewer than three safe sites were found, DISCOVERY explains the unavailable sites. The planner does not bulldoze a road, force a collider through scenery or silently reduce completion requirements. Send the Ready line, city seed and first error if any.

Shrine interaction defers during story cutscenes/missions, inside the Inner World, in vehicles, while wanted, and for five seconds after taking damage. It does not grant invincibility or disable existing enemies. Return on foot after reaching safety.

## Testing status

C# compilation, native Unity tests, live gameplay, live placement and Windows execution of the PowerShell installer have NOT been run here. Executed source/asset checks and Python reference tests are documented separately. Test in Unity before merging to main.
