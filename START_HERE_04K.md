# Start here: 04K Renaissance in Motion

Draft PR #6: https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/pull/6. Development branch `feature/renaissance-in-motion-04k` targets preserved 04J `221cefe8fa2fb3e7f9c927f6a2eeb7399ab1eb75`. **Source-integrated, native-unverified, partial production sprint.** Read Docs/RenaissanceMotion04K/FEATURE_STATUS.md for all 18 modules and ACCEPTANCE_CHECKLIST.md for all 32 gates.

## Update the existing Windows project

Close Unity and back up saves outside the checkout. Check local changes before switching; preserve any local work. Do not reset or merge main.

```powershell
git status --short
git fetch origin
git switch feature/renaissance-in-motion-04k
git pull --ff-only origin feature/renaissance-in-motion-04k
git lfs install
git lfs pull
git lfs fsck
git rev-parse HEAD
```

Git switch can create a tracking branch when only the matching remote branch exists. If it reports a local conflict, preserve that work and resolve it without a forced checkout. Open this same project in Unity Hub **6000.3.16f1**, revision **a56f230f6470**, Built-in default (original optional URP unchanged), Input System 1.17.0.

## Compile and test before building

```powershell
powershell -ExecutionPolicy Bypass -File tools\test.ps1 -Platform EditMode
powershell -ExecutionPolicy Bypass -File tools\test.ps1 -Platform PlayMode
```

Fix the first real Console error and retain Logs/test XML. Only after import/tests succeed:

```powershell
powershell -ExecutionPolicy Bypass -File tools\build.ps1 -Target Windows -Development
```

The existing build script writes Builds/Windows/NeonSyndicate.exe. No new executable was made here. Launch it and test a copied old save as well as New Free Roam before accepting the release.

## First five minutes (native verification route, not previously played)

Start normal New Free Roam. Inspect Malik in the normal third-person camera, then the Veyra Lofts arrival façades. Open JOURNAL and track **Vanguard: The Fractured Bell**. Reach Corin Pell in Cinder Dispatch, accept the optional job, read receiver A, enter the Archive and carry the trace to Tamsin at Pressure House. Inspect the two distinct ceiling/wall structures while using the original doors and stair. Check F2 > Creatures outside quiet starter venues for day/night natural creature rejection counters. This route has not been timed; the whole expedition targets 30–60 minutes, unmeasured.

## Complete physical expedition

Dispatch receiver -> Archive manifest/courier -> Pressure House service stair -> Lantern Refuge -> workshop stores (four mites + one Hollow, no reinforcements) -> protected garden habitat/manual or earned-companion relay -> Refuge quiet beats -> workshop convoy passage (one grounded Fractured Bell drake) -> recover sealed caster -> carry alarm trace back through stair -> Tamsin -> Corin -> physically exit Dispatch. The caster remains pending in BUILD rewards if inventory is full. Completing the route unlocks paid Vanguard Edge Temper and Bellglass Ward Wash at the existing Forge. No free bond or exclusive focus is granted.

Controller/keyboard: use E / Y (Triangle); pause Esc / Start; JOURNAL and inventory tabs with mouse or LB/RB; D-pad/stick navigation, A confirm, B back. **Light: left mouse / RB; guard/parry: right mouse / LB.** Existing heavy/dodge/focus mappings remain in Docs/CONTROLS.md; no new bindings. Follow the dynamically mapped QUIET BEAT/assist prompts. Native gamepad testing remains open.

## Evidence and next action

External API 12/12; separate assemblies 3/3; syntax 12/12; external NUnit 1,358 passes / 46 engine-call failures / 53 exclusions with all seven new cases passing, no new regressions. Native Editor one-attempt blocker: licensing IPC 0x80000000 then Package Manager IPC. No native screenshots, PlayMode, save test, Profiler or Windows build/launch claim.

Record a licensed import first, then the route and M00/M03/M08/old-save/bonds/fusion regression, then seed-61724 matched frames at 1920x1080 clear 15:30 and all presets. Broader sculpted art, authored clips, faction investigation and cinematic expansion remain deferred. No 04L branch has been started.
