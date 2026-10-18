# Living World Renaissance 04H playtest

This is the original project continued on `feature/living-world-renaissance-04h`, from 04G `86a6c05d62a7f59b0c97e72cd3c2462d16202a04`. Draft PR #3 targets 04G. PR #2 is preserved. Do not merge into main.

The source is integrated and externally checked. Unity licensing failed on the implementation host before import, so the route below is a **native playtest candidate**, not a claimed completed playtest.

## Pull on Windows

Close Unity. Keep a separate copy of your existing save folder and preserve any local edits reported by `git status`.

```powershell
cd C:\games\NeonSyndicate_LasVeyra
git status --short
git fetch origin
git switch feature/living-world-renaissance-04h
git pull --ff-only origin feature/living-world-renaissance-04h
git lfs pull
git lfs fsck
git rev-parse HEAD
```

If this older checkout fetches only 04G and does not recognize 04H, run `git fetch origin feature/living-world-renaissance-04h`, then `git switch -c feature/living-world-renaissance-04h FETCH_HEAD` only if the local 04H branch does not exist. Never reset or discard local work to follow this guide.

Open this same folder in Unity Hub with **6000.3.16f1**, revision **a56f230f6470**. Keep Built-in as the default and Input System **1.17.0**. Existing optional URP support is preserved; this playtest needs no pipeline migration or new paid asset. Do not create a blank replacement project.

## Import, then test

Allow the existing setup to finish. Record the first Console error if import fails. Close the Editor before command-line tests:

```powershell
powershell -ExecutionPolicy Bypass -File tools\test.ps1 -UnityPath "C:\Program Files\Unity\Hub\Editor\6000.3.16f1\Editor\Unity.exe"
```

This is the actual native EditMode route. The external NUnit host cannot execute Unity engine calls and retains 46 baseline failures; those external limitations are not a substitute for native results. Use the exact XML/log from your run. After import/tests, enter Play Mode and select **New Free Roam**.

## Normal Free Roam route

1. At the unchanged arrival, inspect Malik from the normal third-person camera and the near view. Look for shaped swept hair, cheek/jaw contours, coat seams and hip pouches. Walk toward the existing fantasy frontages near Veyra Lofts/Asterfall and inspect recessed rose tracery, radial metalwork and stepped reveals above the ground route. Compare the same seed/preset/time against 04G using VISUAL_EVIDENCE.md.
2. Equip **Asterblade**, **Riftfang Pair** and **Basalt Crown** from the existing kit/wheel/COMBAT page. Compare their three-cut chain, lateral rapid cuts and committed heavy blows. Attack near the edge of range, beside a narrow wall and across a frame hitch. Each victim should be hit once per strike. Wide area moves/ripostes retain their established resolver. Test a breakable too; repeated active frames must not repeatedly bash it.
3. Buffer a follow-up, dodge during legal recovery, guard, parry and riposte. Pause/resume during a hit, swap weapons, run out of stamina and recover. The small combat cue distinguishes committed strikes from legal defensive recovery; it is not an extra input or invulnerability window.
4. Use the starter **Wayfarer Shard Focus**, then other already owned focuses/signatures through the existing arsenal and progression. Test charge, heat, cancel, a protected civilian, a busy doorway, a muzzle inside cover, returning disc outbound/return and beam contact. All six signatures retain saved ownership; 04H does not grant them for free. See START_HERE_04D.md for the original arsenal acquisition route.
5. Observe a naturally occurring faction event without attacking. It must remain safe to watch and end within its existing lifetime. Look for a Weaver taking cover and visibly winding up each exposed cast, distinct melee roles and no added waves. Then deliberately intervene in a separate encounter to test ordinary combat.
6. Find a normal handler/drake encounter. The drake can choose approved local event opponents independently, flank, bite nearby opponents, cast a committed lance, retreat when injured and recover beside a safe handler. Check displayed intent/HP, interruption and physical rescue/reunion. It must never enter BONDS, award creature loot or attack a passive observer.
7. Travel with an already owned companion through actual doors, corners and stairs. Compare wall sliding, following and turning. Test wait/recall/assist/rest/rescue/evolution and original fusion/vehicle transitions, then reload. 04H does not bypass the original companion rescue or grant a bond.
8. In JOURNAL, track **The Quiet Circuit**. Talk to **Tamsin Reyes** at the **Pressure House**. Tracking selects this eligible optional offer at her shared giver. The existing optional CONTRACT guidance also recognizes it. It is available without a new story prerequisite.
9. At the marked fuse cabinet, listen for **QUIET BEAT**, then use **E / Y / Triangle** once. Align three separate beats. An early press loses nothing. Leaving pauses the rhythm; injury resets this node. A nearby companion exploration assist can steady one seal, at most once per node, using its normal cooldown. The job remains completable alone.
10. Follow the normal doors and map to **Cinder Dispatch receiver B**, then its **fitting**. Repeat the diagnosis. Return to Tamsin and leave through the **Pressure House front door**. Only normal quest completion pays **180 cash, 45 Lumen, 5 shard chunks and 2 coolant salt**, once.
11. Visit the existing Forge. **Stillwater Tonic** is now known: **120 cash, 3 shard chunks and 1 coolant salt** for six minutes of **12% faster stamina recovery and 5% lower guard cost**. Confirm real consumption, no duplicate unlock/payment and restoration through save/load. It is a learned recipe, not an automatically granted free brew.
12. Save during a node, abandon, take damage, die/restart and reload after reporting. An incomplete node restarts from its checkpoint; completed earlier nodes remain behind that checkpoint. After completion, talking to Tamsin can acknowledge the repair when no higher-priority quest offer owns her greeting. Retest the 04G courier and original Underheart/BONDS/FUSION routes.

All input uses existing actions. Melee: light mouse tap/RB, heavy mouse hold/RT, guard right mouse/LB, dodge Left Shift/B. Interaction uses E/north face button. Full bindings and companion chords remain in Docs/CONTROLS.md. Compare keyboard/mouse and controller; do not infer controller acceptance from compilation.

## Presentation and performance

Turn the existing reduced-effects option on and off: global pooled flashes and the damage overlay should reduce, with attack tells retained. Disable camera shake and confirm it stops new shake. Walk through a doorway and watch the camera pull in immediately and ease back out without crossing a wall. Repeat Low/Medium/High/Ultra and one optional URP import if you use it.

Use existing QA golden/performance tools and Unity Profiler on comparable scenes. Record frame-time percentiles, GC allocations, physics queries, actor counts, draw calls and skinning on the same hardware. No FPS or Windows build performance is predicted here.

After successful import, tests and route, use `tools\build.ps1` or the existing Windows build menu. Report commit, preset, save age, location, weapon/quest/companion, first Console error and a reproducible action. FEATURE_STATUS.md lists all 18 modules and the deferred production work.
