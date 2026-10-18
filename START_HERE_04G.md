# Crystal Combat Renaissance and Living City 04G

Continue the existing project on `feature/crystal-combat-living-city-04g`. Source checkpoints: `9eb33c7` (combat, companions, physical courier and city reactions) and `f2eabb2` (character geometry). Companion cue fixes are `5b04833` and `8bfdc35`. The final handoff commit follows them. PR #2 stays a draft targeting 04F. Never merge this branch into main.

## Pull on Windows

Close Unity. Preserve any local changes shown by the first command, and keep a separate copy of your old saves.

```powershell
cd C:\games\NeonSyndicate_LasVeyra
git status --short
git fetch origin
git switch feature/crystal-combat-living-city-04g
git pull --ff-only origin feature/crystal-combat-living-city-04g
git lfs pull
git lfs fsck
git rev-parse HEAD
```

Open this same folder through Unity Hub with **6000.3.16f1**, revision `a56f230f6470`. Keep the existing Built-in default and current packages. Optional URP remains supported in source; no migration is required for this playtest. Do not create a new project or copy Assets into a blank scene.

The current source uses the 04G title and STATUS label. In Play Mode select **New Free Roam**. Existing saves keep their equipment, quests, bonds and fusion progress. The previous additive starter-kit claim remains available to an old save that needs the melee kit.

## First play route

1. Equip **Asterblade**. Tap through three light cuts, buffer early and late follow-ups, guard during legal recovery, parry a readable melee strike or crystal lance, dodge and attack near the end of the roll. The last light cut completes its recovery before a new chain.
2. Equip **Riftfang Pair** and **Basalt Crown** using the existing weapon wheel/COMBAT page. Compare dagger slip-cut and pursuit with the maul's slower roll strike and heavy advance. Hold a heavy, queue the next move, and confirm stamina, legal defense timing and the six-second slam recovery. A refused attack must not skip a chain link. Open and close pause during one active strike and confirm it does not hit the same victim again.
3. Test the six existing crystal signatures. Check charge and heat separately, cancellation on dodge/swap/menu, zero-charge melee fallback, wall interception and protected people. Check a shot near a busy doorway; a saturated query should quench rather than tunnel through it.
4. In JOURNAL, track **A Light Carried Home**, the optional side job from **Corin Pell** at Cinder Dispatch. The existing optional CONTRACT guidance can also point to it. Tracking selects this eligible job at his shared giver even when he has another offer. Talk to Corin, collect the visible reserve at receiver A, walk through the actual doors to the **Pressure House fuse cabinet**, interact to fit it and remain nearby for three seconds. Report to **Tamsin Reyes**, then leave through the front door. Payment is 160 cash, 30 Lumen and 3 shard chunks, once for the completed quest.
5. During that delivery, enter a vehicle or take a hit. Recover the dropped parcel using its marker. Save during carrying, abandon and resume, and die/restart the checkpoint: one parcel should return at Dispatch, with no reward. Save after fitting: the report checkpoint should resume without another carry. Abandoning must clear the carry pose and prompts.
6. Observe a normal faction event. The cache combines Sentinel/Weaver roles within the old budget. Watching must remain safe. In a drake event, an opposing Weaver can pressure the handler-owned drakeling using an existing volley. Check the 90 HP status, hurt interruption, slower injured assistance and physical reunion. It must never enter BONDS or award creature XP/loot. The event still ends without endless waves.
7. With an existing bonded companion, follow through a doorway and stairs, turn sharply, back into it, wait, recall, assist, rest and reload. Compare relaxed and combat postures. Harmonized companions gain up to 12 percent shorter assist recovery from purchased tuning. Test the original rescue, evolution, care and fusion route; no companion is granted automatically.
8. Inspect Malik's collar, strap, hands and face in the near camera; compare Sentinel mantle/greaves, Weaver coat and duelist asymmetry. Look at actual casting and carrying poses. Observe existing venue residents shelter from nearby violence and resume their stations. Their idle greetings acknowledge the saved delivery when another quest offer does not take precedence.

Existing controls remain: light mouse tap / RB, heavy mouse hold / RT, guard right mouse with melee / LB, dodge Left Shift / B, lock-on and switching through the existing bindings, interact E / the existing pad interact action. See `Docs/CONTROLS.md` for pad-family labels, weapon wheel and BONDS/FUSION chords. No new keys are introduced.

## Native tests

Close the Editor before the command-line test runner:

```powershell
powershell -ExecutionPolicy Bypass -File tools\test.ps1 -UnityPath "C:\Program Files\Unity\Hub\Editor\6000.3.16f1\Editor\Unity.exe"
```

The Editor's Test Runner can also run the entire EditMode suite. Keep the Console errors and actual XML/log. Then open Free Roam and test the route above. Use the existing Build menu or `tools\build.ps1` only after import and tests pass.

The implementation host launched the exact Editor, but its licensing client failed before import/tests. No playable build, native test pass, screenshot or FPS claim is supplied. External compilation and test evidence are in `Docs/CrystalCombat04G/TEST_LEDGER.md` and `Evidence/`.

## Report useful findings

Send the actual commit from STATUS or `git rev-parse HEAD`, graphics preset, save age, weapon/quest/companion, the first Console error and a short reproducible action. For visual comparisons use the same seed 61724, camera, time, weather and resolution on 04F and 04G. Capture player, Sentinel, Weaver, duelist, drake, parcel and doorway views. Performance needs real Profiler/frame-time evidence; mesh counts alone do not establish a performance improvement.

Read `Docs/CrystalCombat04G/FEATURE_STATUS.md` for completed source work, partial systems and native validation gaps. `RESUME_NEXT.md` pins the continuation state.
