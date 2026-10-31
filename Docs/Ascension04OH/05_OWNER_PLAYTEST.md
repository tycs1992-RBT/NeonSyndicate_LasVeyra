# 05 Owner playtest: the H8S segment

This is the script for Tyler's first native playtest of H8S. Nothing in it has been run in Unity yet; every timing and
outcome below is something you record, not something already measured. Follow OWNER_GUIDE_H8S.md section 0 first (keep
your 154 local files on a safety branch, then pull with Git LFS).

## What you need

- Unity 6000.3.16f1 with the project open and the Console visible. Keep the renderer and preset you normally use.
- A save that has Dante Rook unlocked (story mission 4 done) on Gothic Civic H5 (revision 10), or Free Roam on revision 10.
  Do not reset a quest receipt to replay the Hearth Witness: if it is already paid, play the route without the quest.
- A stopwatch (your phone) and, for the newcomer capture, a screen recorder (Windows Game Bar: Win+Alt+R starts and
  stops a recording; OBS also works).
- A gamepad, if you have one, for the parity pass.

## A. Newcomer capture (60 to 120 seconds)

Record the first one to two minutes a new player would see, without the review windows:

1. Title > Gothic Civic H5 (revision 10). Start recording when the world appears.
2. Walk out of Veyra Lofts across the home court toward the Forge. Let the quest tracker and markers guide you; do not
   open the map.
3. Talk to Dante Rook (E, or gamepad Y). Listen to his first line.
4. Jump down from one ledge and keep walking.
5. Stop recording between 60 and 120 seconds.

Watch it back and write down: Was it clear where to go? Did Dante look alive at his post (working, then turning to you)?
Did anything look broken (floating feet, clipping, flicker, a stuck companion)? Send the video or the notes.

## B. The segment, timed (the 4 to 8 minute target)

Start the stopwatch when you leave the Veyra Lofts door and stop it when Dante closes the survey at the Forge. Write the
time at each numbered point. The offline estimate is about 10.5 minutes at the default run, about 7 minutes sprinting:
your measured time is the real answer.

| # | Where | Do | Look for | Time |
|---|---|---|---|---|
| 1 | Veyra Lofts door | Leave home, cross the court | Tracker shows distance and direction; companion follows without blocking the door | |
| 2 | The Forge | Talk to Dante, accept The Hearth Witness | His giver marker shows the offer; dialogue opens with E / Y; the Forge station still opens | |
| 3 | Home-side stair, reading porch (`Realm:hearth-lens`) | Climb, read the impression | The stair and porch hold you; the hint toast appears once | |
| 4 | Oath witness chamber (`Realm:oath-witness`) | Compare, fight 2 glass mites | Mites come one at a time; red "!" or orange "!!" warnings match the ground rings; blows land in turn | |
| 5 | Azure shrine (`Realm:storm-shrine`) | Fight the 1 neon jackal | The pounce lane locks before the jump and a sidestep works; kill chime and short freeze | |
| 6 | Root spring (`Realm:storm-spring`) | Follow the lower cavern, survey | Grounded route, no falling through; the companion keeps up | |
| 7 | Forge door (`Venue:forge.exit`) | Return, Dante closes the survey | Green turn-in marker the whole way; reward paid once | |

Total time: ________ (target 4 to 8 minutes; record run, sprint or travel power use).

## C. Review passes (explicit, reversible)

Open Neon Syndicate > QA > 04OH Ascension Asset Acceptance and use the H8S buttons. Close each review before the next.

1. H8S Malik living motion review: repeat point 3 to 5 with the review running. Jump off the reading porch (a heavy
   landing should play only after real air time) and dodge during a landing (it must stop at once). Try the touchdown
   slider at 0.40 and 0.80 and note which reads better.
2. H8S Dante living cast review: at point 2, check his imported visual, the hammer, no doubled apron, the quiet listening
   idle when you stand still nearby, and that E / Y and the Forge still work.
3. H8S Forge gallery furnishings review: at point 2, the anvil stays inside the station's green collider box.
4. H8S revision 10 route review: draw the route and check no overlay hides a path, prop or trigger along points 1 to 7.

## D. Acceptance checklist

| Check | Pass | Fail | Notes |
|---|---|---|---|
| Project imports; first Console error recorded (or clean) | | | |
| Segment playable start to finish | | | |
| Reward paid exactly once (cash 60, lumen 12, glass_chitin 1, seed_core 1, forgewrights +1) | | | |
| Save on bank 10 mid-quest, reload, finish: no duplicate reward, no lost equipment or companion | | | |
| Banks 0 to 9 still load (spot-check one) | | | |
| Keyboard and mouse pass | | | |
| Gamepad pass | | | |
| Pause > ACCESS options change text size and markers, and persist after a restart | | | |
| Companion never blocks a doorway or corridor for more than a moment | | | |
| No stuck enemy (one that never attacks or never leaves) | | | |
| Each review restores everything on End and on stopping Play mode | | | |
| Frame rate acceptable on your PC (write the number and your settings) | | | |

## D2. Also new in this delivery (quick checks)

| Check | Pass | Fail | Notes |
|---|---|---|---|
| Fresh revision-10 save: Dante's first Forge offer is The Hearth Witness (marker and request point agree) | | | |
| Dante stays at the Forge during the Hearth Witness; the green check stands over him on the walk back | | | |
| The Answering Lamps, The Margin Note, The Grotto Listener, The Answering Cavern each start, finish and pay once (F2 > Quests > Start skips requirements) | | | |
| The Margin Note choice: both options work; save and reload after each quest repeats no reward | | | |
| No enemy spawns inside rock in the grotto or the cavern; the terrace shades can reach you | | | |
| A dropped gun's card says UPGRADE, ABOUT THE SAME or WEAKER, matching its first row's green or red number | | | |
| GEAR tab: "IF YOU EQUIP IT" block and the city tier bar read clearly; text fits at ACCESS 150% | | | |
| SKILLS: the next rank preview matches what learning it does; level up at a shrine, then LEAVE, shows the summary banner | | | |
| H9 Malik and H9 Dante reviews (OWNER_GUIDE_H8S.md section 3, steps 6 and 7) restore everything on End | | | |

## E. What to send back

The newcomer video or notes, the timing table, the checklist, the first Console error (with its stack) if any, your PC's
CPU, GPU, resolution and quality preset, and anything that looked wrong with a screenshot. Native results are only what
you record here; nothing is marked passed automatically.
