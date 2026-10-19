# Start here: 04L The Veil Remembers

Branch `feature/the-veil-remembers-04l`; draft PR [#7](https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/pull/7) targets preserved 04K `df9072930596202b2a8f0fc23c9499f76faada6a`. Latest tested gameplay source `c6aca60ca18634c120ebf0798643856a1c761cbc`, tree `d4be4008813635c5e47ce9beb71a29ceff4634dd`, C# snapshot `21fd7724c9e58228d6cf6d351dda08da8a20f05cb784aaa6d40f40880b0a645d`. Later evidence/handoff commits retain this source snapshot; resolve final HEAD from PR #7 or git. **Production sprint PARTIAL; native-unverified.**

This is a source delivery for review, not a native-tested Windows release. Full24-module disposition: [FEATURE_STATUS](Docs/CinematicSaga04L/FEATURE_STATUS.md). Exact commands/results: [TEST_LEDGER](Docs/CinematicSaga04L/TEST_LEDGER.md). Original04K source and unfinished acceptance are preserved.

## Update the existing Windows project

Close Unity, back up saves outside the checkout and preserve any local edits. Do not force switch/reset or merge main.

```powershell
git status --short
git fetch origin
git switch feature/the-veil-remembers-04l
git pull --ff-only origin feature/the-veil-remembers-04l
git lfs install
git lfs pull
git lfs fsck
git rev-parse HEAD
```

If there is no local branch, normal git switch creates the tracking branch from origin when unambiguous. Resolve local changes safely if it refuses. Open this same project in Unity Hub6000.3.16f1/a56f230f6470, Built-in default; pinned Input1.17.0 and existing optional URP remain unchanged.

## Import and test before building

```powershell
powershell -ExecutionPolicy Bypass -File tools\test.ps1 -Platform EditMode
powershell -ExecutionPolicy Bypass -File tools\test.ps1 -Platform PlayMode
```

Retain Logs/XML and fix the first real Console error. After native compilation/tests and play checks succeed:

```powershell
powershell -ExecutionPolicy Bypass -File tools\build.ps1 -Target Windows -Development
```

The original builder targets Builds/Windows/NeonSyndicate.exe. No new executable exists from this session. A successful build still needs a real launch, Free Roam and copied-old-save test.

## Normal player route: first60seconds and five minutes

1. Choose normal New Free Roam. At Veyra Lofts, look above the original doors for rose tracery; use normal camera V/hold D-pad up to view Malik closely, stop after a sidestep/pivot and watch blink/head/chest settling. Performance visuals require native review; this route has not been played here.
2. Use Pause > Settings > Subtitle size90–140%. Talk to an available named giver; repeat their ordinary greeting when no offer/service owns that interaction. Observe physical gaze/gestures and safe nearby camera framing. Enter/A advances, Backspace/B skips. Pause and resume; return to manual camera after the conversation.
3. Open JOURNAL and track **The Veil Remembers**. Follow the existing map/GPS to Corin Pell at Cinder Dispatch, accept the optional job and inspect receiverA. This is the first investigation interaction, not a new compulsory main chapter. Travel times depend on the save; five minutes is a review target, unmeasured.

## Complete optional investigation

Corin/Dispatch receiverA -> Resonance Archive record1 -> carry trace to Bondkeeper's Refuge -> Odile/real care station -> Tamsin/Pressure House -> real service stair -> Lantern Refuge -> protected garden habitat -> manual or earned companion exploration Assist -> compare response -> refuge QUIET BEAT relay -> Tamsin -> workshop stores -> challenge **one elite Hollow Shade and three mites**, no reinforcements -> recover trace -> physically carry it back up the service stair to Archive record1 -> file it -> report to Corin -> exit Dispatch.

Bring a normal earned melee/caster loadout and healing. No companion is required. An owned companion may use real care/assist; if the roster exists but none is active, use BONDS to bring one along for care. A player with no roster can continue alone. Habitat residents remain protected. Use the dynamically mapped assist/QUIET BEAT prompts. The paid earned04K Forge treatments and caster receipt remain available through their old owners.

At completion, check the once-only $180/60 Lumen/four shard/two salt payment. Save, quit and reload; the completion must remain and may not pay again. Abandon/retry before victory and after the fight, recover dropped courier evidence after injury, then finish/report/exit and confirm a calm world. The exact optional quest is `uh05_04l_01_veil_memory`; 20–40 minutes is an unmeasured target.

## New corrective-checkpoint smoke test (native NOT RUN here)

During the normal M00 opening, confirm the wide Manor reveal remains through all lines. Press Escape/Start mid-line, wait, resume with controller Back and verify that press did not skip the conversation. Nearby directed conversations must frame Malik on his close/shoulder lines and the contact on theirs. Skip and abort/retry; verify camera angles, controls and subtitle cleanup. Repeat load/death/vehicle-entry interruptions and leave both First Law choices explicit. Native EditMode now includes five CinematicNative04LTests cases.

## Original-story and legacy-save acceptance

Use a separate fresh New Story save for the actual M00 opening/Third Eye, Luma/bike/blockade/clinic. Continue to Jax/M01, Nico/M02, Dante/M03; review the new physical contact scenes without cheats. Use copied legitimate checkpoint saves to verify M04 rescue, M05 ambush, M06 market, M07 timer, M08 three transmitters, M09 truck/betrayal, M10 Luma van rescue and M11 three anchors/First Law. Explicitly choose and verify **both** Mercy and Power on separate copies; skipping dialogue must never choose a law. Test the existing prologue-skip menu route too.

On copies after chapters1/6/10/12, verify acknowledgements know only completed events; gear/UIDs, focus charge/cells/heat, BONDS recall/care/rescue/evolution, all three fusions, 04G courier, 04H Quiet Circuit, five04I chapters and04K Fractured Bell must retain progression. Test a real earned crystal pickup with a full backpack and reload. No actual player save has been tested here.

## Controls and current blocker

Use E / Y(Triangle); light left mouse/RB; heavy hold left mouse/RT; guard/parry right mouse/LB; dodge tap Shift/B; get in/out F/Y. Dialogue Enter/A next, Backspace/B skip. Pause Esc/Start; tabs LB/RB; D-pad/stick and A confirm/B back. Original complete controls remain in Docs/CONTROLS.md; no new bindings. F12 captures a real screenshot.

This host's native baseline attempt repaired the preferences directory but failed licensing IPC0x80000000, then UPM IPC; no XML. Native import, EditMode/PlayMode, actual screenshots, performance and Windows build/launch are NOT RUN. External verification passes with zero introduced regressions; it is not a native gameplay pass. Review exact limits in the module matrix before acceptance.
