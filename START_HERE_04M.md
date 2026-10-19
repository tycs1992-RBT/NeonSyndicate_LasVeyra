# Start here: 04M The Living Crystal World

Branch `feature/living-crystal-world-04m`; [draft PR #8](https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/pull/8) targets supplied 04L `8c6c3804b70898e745479affecdfe5f66d95ad1e`. Tested source `b38a0e649e9e3c8f8df49aa28b8519dcac8e7cb5`, tree `8e3b4d6773bb25a53a031ff7e8543e2d29e79ca2`, C# snapshot `7e894569f4925304477ea4fe17e9403ff8a3aa79f9dab836d1267f9def67e4ef`. Later handoff/evidence commits retain this tested C# snapshot; resolve delivery HEAD from PR #8 or git. **Production sprint PARTIAL. Native Unity, gameplay/save/render/profile/Windows acceptance NOT RUN.**

This is substantive Unity source delivery, not a native-tested Windows executable. Full24-module disposition is in Docs/LivingWorld04M/FEATURE_STATUS.md. Original04K/04L content and acceptance stay open.

## Update the existing project

Back up saves separately and preserve local edits. Close Unity, then use ordinary fast-forward updates:

```powershell
git status --short
git fetch origin
git switch feature/living-crystal-world-04m
git pull --ff-only origin feature/living-crystal-world-04m
git lfs pull
git rev-parse HEAD
```

Open this same project in licensed Unity6000.3.16f1/a56f230f6470. No package upgrade/HDRP conversion/new ZIP is needed. Retain the first Console error. Run:

```powershell
powershell -ExecutionPolicy Bypass -File tools\test.ps1 -Platform EditMode
powershell -ExecutionPolicy Bypass -File tools\test.ps1 -Platform PlayMode
```

After import/tests and native checks pass:

```powershell
powershell -ExecutionPolicy Bypass -File tools\build.ps1 -Target Windows -Development
```

Launch Builds/Windows/NeonSyndicate.exe and test it separately. No such build was produced here.

## First60seconds / first5minutes (review targets, not measured)

1. Choose normal New Free Roam. At Veyra Lofts, inspect lower masonry/canopies/transoms across the arrival corridor. Use the normal camera, then walk to Cinder Dispatch through its existing entrance.
2. Visit the original receivers and talk to Corin. Check04L dialogue/Pause/Back/skip still restores control. Walk to Pressure House; inspect the new wall heat exchangers without losing the original stair route.
3. Pause > FIELDWORK. Track Pressure House condensate, return through its actual door and use E/Y to recover4 coolant salt when calm, on foot and on the same floor. The small deposit appears only when its placement is safe. Verify receipt/depletion and material delta; do not count a missing/refused placement as acceptance.

## Integrated45–75minute acceptance route (NOT PLAYED)

4. Visit Bondkeeper's Refuge, inspect its new feeding niches, and use an existing owned-companion service where entitled. Never grant a bond for this test. Observe at least two protected habitat species forage/rest/watch and return; take notes across weather/day-night. Use normal safe transport and original roads between locations.
5. FIELDWORK tracks Quarry survey scree and canyon crystal. Follow original roads; recover4 shards and4 mite dust outside boss arenas. Visit the Warden outpost cold trap for3 frost essence. Normal region threats, doors and travel rules remain. Lose wanted status before recovery; no debug teleport.
6. Visit Dante's existing forge in Old Silver. With Neutral standing and money, craft Courier's Temper from2 coolant salt+1 shard+$90 at the full forge. Verify move/gun tradeoff and buff timer. Craft a second new treatment with earned ingredients and confirm the first workshop treatment is replaced. Do not erase older legacy brews. Save/quit/reload: time/craft/material state must persist.
7. Enter Mara's real clinic in Sunken Row and compare the contrasting street fronts. Use Menders donation/restitution if needed. Visit Order and Forgewright boards. Complete the new patient-home escort, civic-signal rift and condenser delivery when posted; templates are seeded/rotating, so normal refresh/later postings may be required. Check physical pickup/handover, closure, payment and saved serials. No guaranteed fixed-time posting is claimed.
8. Outside the protected starter area, observe normal daylight/night Hollow/Mite packs. Record actual F2 rejection reasons only if placement fails. If a new muster/dispute appears normally, first watch safely, then use E to record the challenge, contribute real damage, interrupt the leader, win or retreat beyond65m for5seconds. Confirm no reinforcements, no observer payment and no repeat loot/payment. Test declined, failed and completed variants.
9. Inspect/capture an original claimable relay. Watch the ownership lantern, original banner/staff/tiers and once-only claim receipt. Reload and verify ownership/light state, material delta and defense window; test loss/reversion using legitimate game actions.
10. Use the original Pressure House stair/Underheart entrance. Recover the Garden seed and Workshop cog through existing regions/doors; test nearby original interactions and exit. Surface sources/events must not leak underground. Visit Ilse's unlocked Refuge stall after the original Regent choice, compare rank quote, buy and sell a bundle with full earned resale perks and confirm a cash loss. No boss material should appear for sale.
11. Abandon/retry a job, die/respawn, cross the Veil, enter/exit a venue with an earned companion, recall/drive/fly/fuse where legitimately owned, then return to a calm world. Save, quit and reload; deposits cannot pay twice and recipe/quest/event/bond/fusion state must persist.

Run this on a fresh save and two backed-up real legacy saves. Also run original M00–M11, prologue skip, both Mercy/Power choices, M00/M03/M08 crystal waves,04G courier,04H Quiet Circuit, five04I chapters,04K Fractured Bell and04L Veil investigation. Actual duration, input parity and performance must be measured on the running game.

## Controls and current limit

E/Y(Triangle) interact; F/Y enter/exit vehicle; Enter/A next dialogue; Backspace/B skip; Esc/Start Pause; LB/RB menu tabs; normal stick/D-pad/A/B navigation. FIELDWORK uses visible paged tracking buttons. V/hold D-pad up changes camera; F12 captures a real screenshot. Full original bindings stay in Docs/CONTROLS.md.

Same host's inherited licensing/UPM IPC blocker was not retried for04M. Native import, EditMode/PlayMode, screenshots, visual comparison, copied-save gameplay, measured duration, Profiler and Windows build/launch are NOT RUN. Source/managed checks must not be called a native playtest.
