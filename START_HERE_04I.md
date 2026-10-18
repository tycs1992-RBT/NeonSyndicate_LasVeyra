## Latest 04I update: visible crystal street factions

In addition to the restored creature horde steps below, locate a normal faction hangout in an existing gang district. Ordinary existing crews can now include a single visually identifiable **crystal blade Warden, Focus Weaver, Rift Duelist or heavy Sentinel leader**. Their civilian aggression/heat rules are unchanged; observe before intervening. Station police patrols now visibly carry crystal Blade Warden/Weaver weapons instead of looking like ordinary ballistic police. These are **the same original crew/patrol NPC counts**, not unlimited spawned reinforcement waves. Crystal-role opponents have improved earned weapon-focus drop chances but must actually be defeated by the player to roll a drop. Use the ordinary item pickup and saved inventory.

A source-only CI workflow has executed the initial 04I creature restoration tests for wiring, C# syntax, and asset GUID metadata. Full Unity native validation is still required: verify factions retain their crystal weapon on engaging, no duplicate waves, safety radius, dropped focus pickup, and old save content. Keep the branch in draft until a licensed editor import and actual gameplay run.

# 04I Crystal Enemies Restored — first Windows playtest

The latest source follow-up restores ambient crystal creatures in **daytime ordinary city streets** (previously always empty), expands nighttime activity, raises stronger-enemy and crystal-focus-drop opportunities, and adds genuine magical enemy waves to the opening and False King main-story chapters. This section supersedes the old night-only creature expectations without replacing the original 04I quest playtest below.

**Update from GitHub:** After backing up existing saves and closing Unity, follow the 04I `git status`, `git fetch`, `git switch`, `git pull --ff-only`, `git lfs pull`, `git lfs fsck` commands below. Open **the same project** in Unity **6000.3.16f1**, not a blank project. No new ZIP, scene or package is needed.

**Actual creature checks to perform, with no cheats:**

1. **Free Roam daylight:** Start at Veyra Lofts. Protected arrival streets remain a safe observation area unless you deliberately provoke hostility. Leave the Lofts/clinic welcome-radius regions and explore ordinary city sidewalks. Wait for the existing off-camera creature-spawn cycle (approximately 12-20 seconds per attempt with jitter; physical placement may be rejected near crowds/buildings), then look for **packs of Hollows or Glass Mites**. A normal city street should no longer have zero eligible creature species at midday.
2. **Free Roam at night:** Repeat in different districts. Parks favor **Neon Jackal packs**, worksites/quarries favor **Glass Mites and Shard Golems**, Grave Frequency favors **Hollow Shades**, with citywide generic packs still possible. Some non-Mite packs can contain a single stronger Shard-Touched elite. Body and attack-token limits are unchanged.
3. **Main Story — M00:** Start the original prologue without skipping it, acquire the Third Eye, defeat the original human Hunters, and check the additional glass swarm and Jackals. Opening Hunters and the later blockade now include visually and mechanically distinct crystal Wardens, Weavers, Sentinels and Duelists. You may fight the creatures for drops or escape on the story bike; Luma's mission must not softlock.
4. **Main Story — M03:** Defeat the False King, then look for the larger crystal breach and **Shard-Touched Pack Alpha**. Fight or leave the yard; the stage has a maximum 20-second optional combat window. All leftover summoned creatures must be removed on normal pass/fail/retry.
5. **Earned drops:** Defeat an eligible non-Mite creature, elite, or crystal-armed NPC **as the player**. Creature kills retain materials and Lumen; stronger variants can drop actual collectible focus weapons and gear through the original loot system. Pick a focus up and inspect/equip it through the old inventory and save/reload. Do not expect a guaranteed weapon from every ordinary kill.
6. **Safety/performance:** Verify passive civilians are not attacked by unrelated events, welcome-area creatures do not automatically swarm the player, normal city fights remain bounded, weather/graphics preset changes respect spawn budgets, and old saved games keep their equipment, quests, bonds and fusion.

**Unverified here:** I authored source and NUnit assertions but did not run Unity compilation, Play Mode or the new test cases. Run `tools/test.ps1` with the pinned Editor (command below) and retain Console/XML before calling this a native-pass build. Earlier 04I numerical and external test totals belong to the previous checkpoint.

---

# Crystal City Ascension 04I playtest

This is the existing Unity project on `feature/crystal-city-ascension-04i`, based on04H commit `50365486bda9d1be5843f4015ba544c8e7f48d72`. Draft PR #4 targets04H. Never merge into main. It is a source-integrated candidate, not a claimed native-playtested release. Read Docs/CrystalCity04I/FEATURE_STATUS.md for all 20 module states.

## Windows installation

Close Unity. Keep a separate backup of your existing save folder and preserve local edits shown by git status.

```powershell
cd C:\games\NeonSyndicate_LasVeyra
git status --short
git fetch origin
git switch feature/crystal-city-ascension-04i
git pull --ff-only origin feature/crystal-city-ascension-04i
git lfs pull
git lfs fsck
git rev-parse HEAD
```

If the branch is absent locally, use `git fetch origin feature/crystal-city-ascension-04i` then `git switch -c feature/crystal-city-ascension-04i FETCH_HEAD` only when that local branch does not exist. Keep local work; never reset it to follow this guide. A first checkout can use `git clone --branch feature/crystal-city-ascension-04i https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra.git`.

Open this same folder in Unity Hub with **6000.3.16f1**, revision **a56f230f6470**. Keep Built-in default and Input System **1.17.0**. No pipeline switch or paid asset is required. Allow import/setup to complete and record the first Console error if any.

Close the Editor before command-line native tests:

```powershell
powershell -ExecutionPolicy Bypass -File tools\test.ps1 -UnityPath "C:\Program Files\Unity\Hub\Editor\6000.3.16f1\Editor\Unity.exe"
```

The cloud host did not reach native import. Its external engine-call failures do not predict your native result. Use the actual XML/log from your machine. After successful import/tests, enter Play Mode and choose **New Free Roam**.

## Normal Free Roam route

1. Inspect Malik at the unchanged Lofts arrival, then walk across to Cinder Dispatch. Compare the longer split coat, face/cuffs, supported portal and vaulted room against 04H. Enter Pressure House too: its overhead services and vaults should be visible without blocking the real stair, work points or door. Corin has a dispatch harness; Tamsin has a shaped work bib/pockets. Use Low/Medium/High/Ultra and daylight/night.
2. Equip Asterblade, Riftfang Pair and Basalt Crown through the existing wheel/COMBAT page. Test light chains, heavy recovery, dodge, guard, parry and riposte. Test narrow cover, hilt inside cover, multiple victims, pause, weapon swap and hitches at30/60/high frame rates. A victim should take at most one hit per strike. Compare heavy settling and actual contact feedback.
3. Cast the starter Wayfarer Shard Focus and other already owned signatures. All six retain original acquisition/ownership;04I does not free-grant them. Test charge, heat, reload/cancel, walls, protected civilians, busy doorways, returning discs and beams. Use existing physical charging cradles and sustainable melee. Existing START_HERE_04D/04F cover arsenal acquisition.
4. Travel with an owned companion, or follow the original BONDS rescue route if you want one. Inspect all three species/stages when legitimately owned. Test follow, doorway slot, blocked walking at high frame rates, wait/recall/care/rest/assist/rescue/evolution, save/reload and vehicle transitions. The new episode is fully completable manually.
5. In JOURNAL track **Ascension: The Broken Ledger**. Talk to **Corin Pell inside Cinder Dispatch**. Follow receiverA, Archive records/Iris, receiverB rhythm and carried-sample markers to Tamsin. Injury/vehicle entry/large displacement drops a courier parcel: recover its marker. Report and leave Dispatch for chapter1 payment.
6. Track **Ascension: A Passage Kept** at Corin. At the Dispatch door use interact to request the crossing. It honors ordinary encounter budgets, safe placement and cooldowns; a recent handler event can require420 seconds of gameplay quiet time. Move away from crowded doors when placement asks, or abandon/return later. At the neutral incident seal, mediate or protect, allow actual handler/drake reunion, and collect the sealed report. Watching alone does not complete it. Visit the Refuge care/training markers, then report/exit. The NPC drake stays handler-owned, and the incident has no separate payout during this chapter.
7. Track **Ascension: The Garden Remembers** at Tamsin. Take the existing Pressure House service stair to Lantern Refuge, then the Pyre Gardens habitat/vista. Rhythm and manual/companion assists identify the trail. Leave passive residents safe and return by the real route to report/exit.
8. Track **Ascension: The Missing Coupler**. Reach the Crucible Workshops coupler court. Fight the declared sentinel and two glasswings, with no reinforcements. If another encounter owns the arena, retry from the checkpoint after it clears. Carry the coupler back on foot, recover if dropped, then report/exit. Ascendant Edge Oil becomes known at the Forge; it still costs160 cash,4 shards,2 salt,1 ember crystal.
9. Track **Ascension: Crystal City Ascension** at Corin. Align Dispatch receivers, the Pressure House cabinet, and the real sample test at Dante forge. Return to Corin and leave through Dispatch's front door for final payment. Cityward Infusion becomes known:140 cash,3 glass chitin,2 salt. Verify real consumption, timed effects and no duplicate completion payments.
10. At each chapter test death, abandon, saving mid-node/mid-carry and reload. Earlier checkpoints/completions should remain; a current unfinished node/carry phase restarts at its source. Test completed passage receipt, event cooldown restoration, new-game/session cleanup and a copied old save. Retest the04G courier, Quiet Circuit, original Underheart and fusion quests.
11. Drive an ordinary car and bike, then your legitimately owned fused chassis. Test braking, steering, reverse, handbrake drift/release and collision recovery. Countersteer is bounded and yields to strong deliberate input; no traffic physics or top-speed rewrite is intended. Inspect all three new fused fairings and original rider/wing/coil/brace transitions.

No new hidden control: interactE / Y / Triangle, light mouse tap / RB, heavy mouse hold / RT, guard right mouse / LB, dodge Shift / B. All original companion/fusion chords remain in Docs/CONTROLS.md. Compare keyboard/mouse and controller in Unity; source checks are not device acceptance.

For matched visuals use Docs/CrystalCity04I/VISUAL_EVIDENCE.md. Use the existing QA golden/Perf tools and Unity Profiler on equivalent scenes/settings; record frame times, allocations, physics, actors, skinning and draw calls without assuming an FPS result. After native tests and play route pass, use tools\build.ps1 or the existing Windows build menu. Supply commit, preset, save age, location and first reproducible Console error with feedback.
