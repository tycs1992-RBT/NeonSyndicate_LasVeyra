# H8S owner guide: try it in Unity without writing code

## 0. Protect the 154 local files on your PC first

A screenshot from your PC showed 154 uncommitted local files (mostly materials and .meta files that Unity generated
while preparing earlier reviews). They are not a result, but some of them may be useful preparation. Keep them before
you pull anything:

1. Close Unity.
2. In GitHub Desktop, look at the Changes list. Do not press Discard.
3. Choose Branch > New branch, name it `safety/local-unity-prep` and create it. Your local changes come with you to the
   new branch.
4. Write the summary "Local Unity preparation snapshot (not reviewed)", tick all files and press Commit. If you want a
   backup online, press Publish branch (it stays separate from the art branch and from PR12).
5. Switch back to `feature/04oh-ascension-art-foundation` (Current branch menu). The Changes list is now empty.
6. Press Fetch origin, then Pull origin. Wait until Git LFS has downloaded the real models and images (text pointer
   files cannot import).

Nothing in this delivery needs those 154 files. If Unity regenerates similar files after the pull, that is normal. If
you want them compared, publish the safety branch and say so.

## 1. Open the project

Open the project with Unity 6000.3.16f1. Keep the renderer and quality preset you normally use. If the import shows an
error, copy the FIRST red Console message with its full stack before trying anything else.

## 2. What changed in normal play (no switch needed)

- Combat: ground rings now use the same colours as the "!" (red, can be blocked) and "!!" (orange, cannot) icons and
  their edge is where the blow really lands; pounces and charges show a lane that locks before the jump, so a sidestep
  works; blows on you land a moment apart; kills give a short freeze and a rising chime; bosses announce PHASE 2 / 3
  and do not swing during the roar; the crystal focus heat bar warns before an overheat; the poise strip pulses before a
  stagger. Details and a try-list: COMBAT_READABILITY_H8S.md.
- Companions: a companion card (name, health, mode, assist cooldowns), short subtitle barks, smarter spacing in
  corridors and doorways, sheltering behind you when hurt. Named cast members work, read or keep watch at their posts,
  turn and greet you, then go back to work. Dante's Forge marker shows the Hearth Witness states. The quest tracker
  gives distance and direction. Pause > ACCESS has text size, high-contrast markers and bark options. Details:
  CAST_AND_COMPANIONS_H8S.md.
- The Forge in revision 10: Dante offers The Hearth Witness first, and he stays at his post while it runs, so you hand
  the survey to him in person. Iris and Mara also stay put during their own quests.
- Four new side quests on Gothic Civic H5: The Answering Lamps (Nessa, Lofts door), The Margin Note (Iris, archive; one
  choice), The Grotto Listener (Mara, clinic; after story mission 1) and The Answering Cavern (Dante, after the Hearth
  Witness). F2 > Quests > Start skips their requirements for a quick look. Details: QUESTS_H8S.md.
- Gear and growth: gun and gear cards say UPGRADE, ABOUT THE SAME or WEAKER and by how much; the GEAR tab shows what you
  would lose and which set bonuses switch on or off; a thin bar shows progress to the next city tier; SKILLS shows what
  the next rank adds; leaving the shrine after a level up shows a summary banner; the Forge armory shows before and
  after. Details and ten check steps: PROGRESSION_READABILITY_H8S.md.

## 3. Explicit reviews (nothing is approved, enabled or saved)

Open Neon Syndicate > QA > 04OH Ascension Asset Acceptance. The H8S buttons are listed under "H8S living cast".

1. **Malik living motion.** Press "Open H8S selected Malik living motion review". In Edit mode press "Prepare actual
   H8S model and materials (disabled)". Start the game normally on Gothic Civic H5 (revision 10). Press "Start
   reversible Editor gameplay review". Walk, run, stop, turn, use an owned sword, guard, dodge, aim and fire an owned
   gun, cast an earned focus. Jump and drop from a ledge: a landing should play only after real air time, and a dodge
   or a travel power should cut it at once. Try the "Heavy landing touchdown" slider at about 0.40 and about 0.80 and
   note which looks right. Revert or close the window to restore the normal Malik.
2. **Dante living cast.** Press "H8S Dante living cast review" once in Edit mode (it prepares a disabled copy), then
   start the game on Medium graphics or higher with Dante unlocked (story mission 4 or Free Roam), walk into the Forge
   and press it again near him. Talk to him with E (or gamepad Y), open the Forge station, walk away past 30 m and come
   back. He also gets the quiet listening idle (torso only while he holds his hammer). Press "End H8S Dante living cast
   review" and also try stopping Play mode during the review: everything must return to normal. NPC_BINDINGS_H8S.md
   lists what to look for (expect some stretching at the neck and shoulders: the supplied study is built differently
   from the game's Dante).
3. **Dante quiet listening (on his normal body).** Press "Open H8S Dante quiet listening review" and start it near him.
4. **Forge furnishings.** Press "H8S Forge gallery furnishings review" once in Edit mode, then inside the Forge in Play
   mode. Check the anvil stays inside the green collider box from every side and that E / Y still open the Forge.
   WORLD_ROUTE_H8S.md has the full list.
5. **Revision 10 route.** Press "H8S revision 10 route review" in Play mode on Gothic Civic H5. It draws the Hearth
   Witness route and adds restrained VISUAL PREVIEW accents that never block a path. Walk the quest if it is legitimately
   available and time it from Dante back to the Forge.
6. **H9 refined Malik.** Use "Open H9 selected Malik refined review (reversible)" in the acceptance window (or the menu Neon Syndicate > 04OH > H9 Selected Malik refined review) the same way as step 1. Compare it with H8S: the
   groom, eyelids and lashes, collar and revers, armour fit and boots over the trousers.
7. **H9 Dante refit.** Use Neon Syndicate > 04OH > H9 Dante living cast review and End H9 Dante living cast review the same way as step 2. His neck and shoulders
   should no longer stretch (the refit matches the game's Dante); the boots may sink about 3 cm.

The first Prepare of a Dante review makes Unity write its import settings into 20 tracked .meta files (the list is in
NPC_BINDINGS_H8S.md). That is expected: commit them as "Dante review importer settings" or discard them. They are not
part of your 154 old files.

## 4. Please record

For each step: what you saw, any Console errors, your hardware, renderer and quality preset. Keep personal save files
private. If something looks wrong, a screenshot plus the first Console error is enough for the next builder.

## 5. Status

Native import, play, saves, devices, screenshots, performance and the Windows build are NOT RUN on this host. Owner
appearance approval is NOT GIVEN. The candidates stay disabled; the ordinary procedural actors remain the fallback.
