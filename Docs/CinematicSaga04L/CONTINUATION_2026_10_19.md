# 04L continuation: camera ownership and reliable pause

Continued the intact published 04L checkpoint `621b11ad8af379977091e4e8ab26177e879adeca`, preserving exact 04K `df9072930596202b2a8f0fc23c9499f76faada6a`. Draft PR [#7](https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/pull/7) still targets 04K. This is corrective implementation for L03/L04/L05/L16/L22/L24; production/native status remains PARTIAL. Other module dispositions remain in FEATURE_STATUS.md.

## Demonstrated source defects and implemented behavior

- `TickPerformance04L` previously wrote `SetCinematic` even when it did not own the camera. This replaced the authored M00 Manor reveal with the generic dialogue frame. Actor performance now runs under that shot without changing its camera.
- Close/shoulder shots previously framed the partner even during Malik's lines. Both angles now frame the speaking character and remain on the same side of the established axis. Wide/reveal forms retain their existing composition.
- `MenuSystem.Update` rejected Escape/Start whenever GameState was Cutscene. It now opens the existing pause menu while retaining Cutscene state. GameTime pauses the mission runner, cinematic camera and humanoid pose updates. Closing resumes that state; its confirm/Back press cannot also advance/skip dialogue on that frame.
- Authored `Cinematic` now restores its captured control/hint/state in `finally`, disposes nested Talk and clears only a camera it acquired. Talk disposal clears its subtitle. Automatic direction refuses climbing, entering, airborne and already control-locked play states.
- The source CI PR branch filter named a nonexistent branch. It now matches the actual 04K PR base.

No new mission, persistent field, reward, asset, package, binding or manager was introduced.

## Executed verification

Fresh starting-checkpoint API compile: 12/12; external NUnit 1,385 pass / 46 unchanged engine-call failures / 54 exclusions. Current snapshot: `21fd7724c9e58228d6cf6d351dda08da8a20f05cb784aaa6d40f40880b0a645d` (753 Runtime, 10 Editor, 115 EditMode files).

Current: 12/12 managed API configurations, 3/3 assembly boundaries, 12 syntax configurations with zero errors; external NUnit **1,389 pass / 46 fail / 58 NeedsEngine exclusions**. Raw result remains Failed/exit46. Against the freshly executed starting checkpoint there are zero introduced failure names or changed reasons. Four added engine-independent reverse-shot cases pass at different world axes. Four additional native lifecycle cases are authored/API-compiled and NOT RUN; together with the earlier camera test there are five native 04L cases.

Metadata: 1,683 sidecars and 13 auditor regressions pass. Current preservation guard and inherited 04I/J/K guards pass. Git LFS fsck and whitespace check pass. Exact reports are tracked in Evidence/resume-*; prior evidence remains immutable. Source push CI 39262810494 and PR CI 39262819238 both completed successfully at c6aca60.

## Native blocker and required player acceptance

A fresh host capability probe rejects AF_UNIX socket bind with errno1 Operation not permitted; loopback TCP bind works, preferences directory exists, exact Editor is installed, Windows module is absent. The previous exact-Editor attempt failed licensing/UPM IPC and produced no XML. No new Editor run, licence validation, imported scene, PlayMode, visual frame, old-save gameplay, profile or Windows build was achieved. No permission bypass was attempted.

On a supported licensed host: run native EditMode including CinematicNative04LTests, then start normal New Story. Watch the original Manor reveal through its dialogue, pause with Escape/Start, wait, resume with controller Back and confirm the line remains. Advance to Luma/Malik close-ups, skip, abort, die and reload copied checkpoints; camera/manual view, controls, subtitles and mission objective must restore. Check a giver conversation, a transition and both First Law choices. Repeat the full original and optional routes in START_HERE_04L.md. Render/framing/comfort and all 04K carryover acceptance remain open.
