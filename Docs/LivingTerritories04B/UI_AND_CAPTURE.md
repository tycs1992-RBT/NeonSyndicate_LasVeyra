# 04B interface and native visual capture

This document describes implemented tools. Native Unity execution is NOT RUN in the cloud.

## Player interaction

04B uses the existing contextual interaction path. On foot, use E on keyboard or the gamepad's north face button (Y / Triangle). The existing HUD chooses the nearest available physical interaction and translates its button prompt for the active device. Capturing, harvesting and improving a core require the physical site and the runtime's safety checks.

The TERRITORIES pause page supports the existing mouse, keyboard arrow keys plus Enter, and gamepad D-pad plus south face button navigation. Tracking sets an ordinary GPS waypoint; an active story mission's route retains priority. The caretaker's ledger opens this same pause page through `MenuSystem.OpenRegisteredPage`, so normal pause, cursor, input blocking and close behavior apply.

## Actual screenshots on Windows

Open Unity 6000.3.16f1 and run the game. Use a separate test save. Finish active missions, pursuits and encounters before the camera tour.

1. Choose **Neon Syndicate > QA > Capture Visual Rebirth 04A Looks** to retain the existing comparison route.
2. Choose **Neon Syndicate > QA > Capture Living Territories 04B Looks**. The tour includes matched 04A city/Hearthspire view IDs and views of successfully registered 04B sites at noon and night. Ambient streaming follows the camera so actual wildlife/staff can load. The player is parked and protected using the existing QA session. No capture, reward or ownership transition is simulated.
3. Keep the Game view visible. Esc or the pad Menu button cancels and restores the session. The tour can take several minutes because each view waits for population/lighting streaming and records 120 frames.
4. During an actual player capture, defense or noncombat interaction, use **Neon Syndicate > QA > Capture Current Territory Moment**. It saves the current Game view including HUD and a state record without changing the camera, clock, player health or encounter. It must be invoked during gameplay, rather than from a paused menu. This passive screenshot never sets the normal QA busy flag, since that flag intentionally cancels existing stronghold raids. Verify that a manual current-moment capture leaves the actual active raid intact during Windows acceptance.
5. Use **Neon Syndicate > QA > Open QA Output Folder**. Files are below the normal game's `GoldenShots` folder in persistent data. The 04B tour writes PNGs, `manifest.json` and `territory-look-tests.json`; current moments write `moment.png` and `territory-moment.json`.

The tour's registered anchors are actual runtime placements, not preferred catalog coordinates. Missing/skipped sites are not fabricated or rendered at another location. Run the same seed, display resolution, graphics preset and pipeline when comparing 04A and 04B. The screenshot manifest includes clock, camera, hardware and graphics settings. Film grain is suppressed for the tour as in the existing capture tool; current moments retain the player's view.

## What the native reports measure

Each tour image records mean, median, 95th and 99th percentile frame intervals from 120 sampled frames; Unity/managed allocated memory; live GameObject, renderer, NPC and creature counts; draw calls, SetPass calls and triangle counters when supported. Main-thread profiler nanoseconds and the latest GPU frame timing are reported only when the platform supplies them. Unsupported counters use -1. Hardware, graphics preset, frame cap and v-sync are recorded. Requested next-load 04B switches and the flags actually captured at city boot are recorded separately, so changing a switch before restarting cannot be mistaken for a completed comparison.

Object counts include all currently loaded active objects, not only the site's geometry. Counts are gathered after frame sampling to keep the object enumeration itself out of that interval. Camera-focused ambient streaming is identified explicitly. No local measurements exist until these tools actually run in Unity. Frame cap, v-sync, editor overhead, warm-up and background workloads affect the measurements. These samples are not a validated GPU benchmark or evidence that a target FPS was reached.

The camera tour does not verify traversal, saves, gamepad control, damage interruption, ownership transitions or combat balance. Record those flows through actual play and current-moment captures, then complete the acceptance checklist before merging.
