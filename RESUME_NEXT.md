# RESUME NEXT: Crystal Combat and Living City 04F

## Exact identity

Repository: tycs1992-RBT/NeonSyndicate_LasVeyra
Branch: feature/crystal-combat-citylife-04f
Verified implementation commit: 6deac1bd551d2337b5c4518bab8cc8567217b7f7
Verified implementation tree: b50fca7772f727a3667ef1a8ce706a955f75e9c3
Original preserved 04E baseline: 4c690c6248f0ff7f00098598e6e42e409d70f84f
C# source SHA-256: fe56fd93575b420a7dbf7a4efde95eb99befa7843643886307e847f4fb3ab6a7
Unity: 6000.3.16f1 (a56f230f6470), Built-in default, existing optional URP preserved.

The handoff documentation commit follows the exact implementation commit above. It changes no gameplay source.
Resolve the current branch head with `git ls-remote --heads origin feature/crystal-combat-citylife-04f` or `git rev-parse HEAD`; do not mistake the original 04E SHA for the new implementation. main and 04E were never moved or force-pushed.

## What is published

- New Free Roam blade/talon/maul and shard-focus kit; stable saved weapon-ID conversion and original six signatures.
- Saved, bounded Resonance Charge with melee/victory recovery, actual safe cradles, three finite found cells, truthful live item cards, zero-charge melee fallback and separate heat/stamina/travel owners.
- Actor-safe NPC crystal casts, four Warden roles, local finite dispatch/containment, safe spawn checks and bounded existing-squad marshal support.
- Three autonomous events in existing WorldEvents. Encounter-owned faction fighters and drake, physically separated rescue/reunion, finite ending/cleanup, saved receipts/cooldowns and no observer aggression.
- Three connected indoor side jobs beside the existing campaign, clearer cast names/facing/shared giver state, 04F STATUS/session identification and honest first-companion prerequisite routing.
- Weighted finger rigs, grip solver, layered face/cloth/armor and weapon geometry, paired stance and Quartz anatomy/animation changes. Existing crowds keep their low-detail transform budget.
- Reusable managed verification tools, eight compact evidence JSONs, START_HERE_04F.md and the feature/combat/city/art/test handoff docs.

## Verification and actual limits

3 of 3 available managed API compile configurations passed against genuine pinned Unity modules, with no new diagnostics; 9 Editor/URP rows unavailable. All 12 syntax configurations passed. NUnit: 1285 passed, 46 unchanged native ECall failures, 53 NeedsEngine excluded; all 45 new 04F cases passed. Save/catalog preservation, 1621 metadata sidecars, one TerritoryPlacement and 437 actual LFS payload hashes passed. Every source/configuration used the same immutable C# snapshot.

Unity import/native compile, Play Mode, controls, physics, actual screenshots, Windows build and performance: NOT RUN. No claim of reference-image parity, measured FPS or commercial release readiness. `Docs/CrystalCombat04F/FEATURE_STATUS.md` separates integrated source from partial and unvalidated work.

Known partials: event drakeling is handler-owned and non-targetable, with no independent damageable health; NPC bolts use bounded transient objects rather than pooling; old trigger-changing loot traits are dormant in crystal forms and disclosed; global flash reduction and fuller investigate/warn police behavior remain partial; no courier interior escort, facial blendshapes, imported sculpted hero or new authored animation clips. Detailed source mesh improvements are delivered, but native visual quality is unproven.

## Earliest broken or unverified behavior

No newly introduced test failure remains in executed comparisons. The first native test has not run, so the first unknown is importing this branch and entering a normal new Free Roam session. Do not label that unknown as a passed playtest. First observed baseline-only failure is ArtDistrictStarterSelectionTests.AnchorsMatchExistingSpawnMathAndAreCached, SecurityException from native ECall; the ledger retains all 46 identities/reasons. Historical asset-host scripts have separately documented stale baseline expectations.

Review fixes already included: side quests cannot bypass finite incident budgets; station patrols cannot replenish while wanted; rescue cannot succeed with a dead handler; filtered court placement retries its fallback; buffered heavy intent and refused maul cooldown are corrected; NPC bolt query saturation fails closed and loose bolts clear on session reset. Do not reimplement these from scratch.

## Next three executable actions

1. Close Unity, preserve local work and a separate pre-04F save, fetch/switch/pull the existing 04F branch, `git lfs pull`, `git lfs fsck`. Open the same project in Unity 6000.3.16f1, then run `powershell -ExecutionPolicy Bypass -File tools\test.ps1`. Record the first real Console error and actual EditMode result. Do not change the pipeline or project version to hide an import error.
2. Follow START_HERE_04F.md in a new Free Roam, then an old save. Verify all three melee styles, six focus behaviors, zero-charge fallback, a physical refill, finite cell receipts, autonomous dispute/drake rescue, one real indoor job and original bond/fusion route. Save, abandon, exit and reload. Fix the first native blocker before adding more systems.
3. Capture controlled 04E/04F views at seed 61724, same camera, time, weather and quality, including Malik face/hands, wardens, drakeling, daylight weapons and interiors. Run the existing perf flythrough plus an actual fight on all four presets. Use real failures and frame-time/GC evidence to prioritize pathfinding, bolt pooling and art refinement.

Resume command for an already configured clone:

```powershell
git fetch origin
git switch feature/crystal-combat-citylife-04f
git pull --ff-only origin feature/crystal-combat-citylife-04f
git lfs pull
git rev-parse HEAD
```

## Publication and worktree

Implementation checkpoint was committed through the authenticated GitHub integration, its ref read back, fetched into the local clone and compared with the local index tree. Existing binary assets were hydrated and hash-verified; no new LFS objects are required. The final handoff and PR are on the same feature branch. No gameplay source is intentionally uncommitted or unpublished. Verify `git status --short` before continuing, since an Editor session can generate local setup files.

---

## Historical 04E and 04D handoff, preserved below

# RESUME NEXT: exact continuation state (04E)

## Identity
Repository: tycs1992-RBT/NeonSyndicate_LasVeyra
Branch: feature/underheart-bonds-04e (not merged into main; never force-pushed)
Base: feature/crystal-arsenal-04d at 0dbfc0b1a778c0df388ed75f8b15beed45b19554 (ancestry verified)
Checkpoints this run: 847a1a1 (integrator foundation), 0c1406a (shared hooks), 11bc7ef (all work streams merged),
e06643a (cross-stream integration tests), 9315fe4 (independent review fixes), d18f9ec (docs), 34a3012 (descent window,
held world events, HUD and boss polish), 55da4f2 (choice consequences, boss Resonance, second review fixes). The
documentation commit that adds this
section is the branch head; verify with `git ls-remote --heads origin feature/underheart-bonds-04e`.
Worktree: cloud clone at /home/claude/neonsyndicate_lasveyra (ephemeral). Owner project: C:\games\NeonSyndicate_LasVeyra.
Handoff timestamp: 2026-10-16 America/New_York. This file plus `START_HERE_04E.md` is the updated handoff.

## What changed in this run (source, wired into normal play)
- Guidance: named cast at real posts, shared giver state for marker, map, journal and dialogue, leads for new and old
  saves, floor and underground routing, STATUS page, session chip (`Gameplay/Guidance04E`, `UI/MapOverlays.cs`).
- Four interiors: Cinder Dispatch, Pressure House, Bondkeeper's Refuge, Jax's garage bay.
- The Underheart: six region variants under the Pressure House, a real stair, an optional vehicle portal, shortcuts,
  heat hazards, negative-depth ground, camera and kill planes (`World/Underheart`, `WorldBounds`, `WorldApertures`).
- Builds, the starter ward and a once-per-token reward ledger (`Gameplay/Builds`, BUILD page on K).
- Three bonded companions and three fused bike forms on the owned chassis (`Gameplay/Bonds`, `Vehicles/Fusion`,
  `Gameplay/Garage`).
- Nine underground species, four bosses, sixteen main quests, twelve side quests, eight contract grammars.
- Review fixes in 9315fe4 (care step, props underground, safehouse on load, starter ward on load, HUD layout, boss
  assists, trim ids, rescue order, arena reset, autosave once).
- 34a3012: the Act IV descent window changes heat, light and an optional elite detour; running world events hold
  underground; older HUD overlaps moved into HudLayout04E; bestiary region labels; boss lock-on, aim assist and damage
  numbers; one light per boss fight.
- 55da4f2: `matron_fate` and `regent_end` change residents, refuge lines, a refuge stall and board jobs; Resonance
  marks and bursts on the 04E bosses; surface saves never load underground after a rest; checkpoint autosave retry.
Full table: `Docs/Underheart04E/FEATURE_STATUS.md`.

## Current unfinished or dirty work
None in the clone after the documentation commit.
Known gaps: the vent lift sidegrade was cut; finger bones (`HandPose`) are still a stub; at HUD scale 1.3 to 1.5 the
older HUD overlaps itself; the deep's heat and lights do not change with `regent_end` (residents, lines, stall and board
do); frame cost of every 04E area is unmeasured. Every campaign choice now changes play (a test enforces it). A second
independent review ran on 9315fe4: no blocker, no high; its medium and lows are fixed in 55da4f2.

## Tests (55da4f2)
- 12/12 configurations, 0 errors, 63 pre-existing warnings (offline compile against the Unity 6000.3.16f1 reference
  source; not native).
- 1169 passed, 25 failed (the same engine-only `Resources.Load` tests as at 0dbfc0b), 53 skipped NeedsEngine.
  SaveRecovery04DTests 18/18 passed offline (first execution).
- Metadata: 1537 sidecars valid, one TerritoryPlacement. Dash scan clean.
- Native Unity import, play, build and performance: NOT RUN. Commands and the acceptance matrix are in
  `Docs/Underheart04E/TEST_LEDGER.md`.

## Last reproducible failure
None outstanding offline. Last fixed (review R1 critical): the Care step in `uh05_03_bond` could never finish because the
Bondkeeper station's prompt won the tie; quest prompts now take priority (`Interactables.QuestPriority`).
Next discriminating test: in Unity, press E at the care station during `uh05_03_bond` and confirm the step completes.

## Next three executable actions
1. Native import and compile in Unity 6000.3.16f1, then `tools/test.ps1`. Success: no compile errors, the 25
   `Resources.Load` tests pass in the editor.
2. Play the first route in `START_HERE_04E.md` from a new Free Roam start, then load an old save. Success: chip and
   STATUS rows present, each step completes without debug, the fused bike separates cleanly, save and reload underground
   resumes at an anchor.
3. Fix the first native defects found, then capture golden shots and a perf run (including the Kiln Regent arena and a
   fused canal ride) before any art polish.

## Decisions and constraints
- Stable ids: quest ids `uh05_*`, bond ids `quartz_guide`, `ash_warden`, `storm_skimmer`, forms `ventwing`, `kilnback`,
  `coilrunner`, tokens `boss04e:<id>`, `quest:<qid>:<reward>`, `kit:<preset>`. Six new SaveData blocks are frozen in
  `SaveDataTests`. Appended species, materials and trims only.
- Controls: N and U on foot (BONDS) and in your vehicle (FUSION); pad LB + D-pad chords; K for BUILD.

## Visual upgrade run (after 04E, same branch)
Phases 0 to 5 are commits e66ea56, 75ff2f6, 4c1d8c3, 7658f4a, 99126aa, f94a604 (one per phase, each revertable alone).
Summary and what remains: `Docs/VisualUpgrade/SUMMARY.md`; per-phase docs with Unity checklists beside it. Offline gates
at the head: 12/12 configurations, 0 errors; 1261 passed, 25 engine-only failures, 53 skipped. Nothing run in Unity;
frame cost unmeasured. Next: the owner captures "after" golden shots, runs Compare and the Perf flythrough, and sends the
report; then tune from real footage (ambient brightness, TAA ghosting, boot time at High, clutter density).

---

# History: 04D continuation state (kept for provenance)

## Read first: finishing review after 9e88434

The following original handoff is historical to `9e884340226c9e06b9a2f843c9e1628ef921a011`.
A later focused save-recovery patch is documented in
`Docs/CrystalArsenal04D/FINISH_REVIEW.md`. Continue the current branch head, not an older master.

Source review found that resaving after backup recovery could copy the rejected main over the valid backup.
`SaveFileTransaction` now rotates only a main snapshot accepted by the same validator, on both installation paths.
18 new cases in `SaveRecovery04DTests` are authored but NOT RUN in the follow-up environment.
There was no installed C# or Unity runtime available to execute them. The older 730-pass result below has not
been rerun for this patch; do not add these cases to its passing total or call this native-validated.

Next: run SaveRecovery04DTests and CrystalFoundation04DTests, then the full pinned Unity import/test route.
Preserve the six weapon behaviors, IDs, boss entitlements and existing review fixes. Native footage should guide
further hand/muzzle, beam, telegraph, audio and performance polish. This patch changes no gameplay assets,
packages, saves schema or existing valid metadata. Nothing is merged into main.

---

## Identity
Repository: tycs1992-RBT/NeonSyndicate_LasVeyra
Branch: feature/crystal-arsenal-04d (not merged into main; never force-pushed)
Starting HEAD: 351f8ce99b61c354a7404521da28e1eb4a9fdcc2 (remote head checked 2026-10-16; no newer branch head existed)
Code commits this run: 7cba383 (integrator foundation), 42f6f5c (cross-pass interfaces), 0d39154 (six foci,
arsenal, encounters, bosses, art), aa52393 (independent review fixes). The documentation commit that adds this file
is the branch head; verify with `git ls-remote --heads origin feature/crystal-arsenal-04d`.
Worktree: cloud clone at /home/claude/neonsyndicate_lasveyra (ephemeral). The owner's usual project is
C:\games\NeonSyndicate_LasVeyra; pull the branch there.
Handoff timestamp: 2026-10-16 16:25 America/New_York.

## What changed in this run
Implemented in source (see `Docs/CrystalArsenal04D/CHECKPOINT.md` for the full table):
- Six crystal foci (Shardwake, Prism Thread, Gyre Crescent, Sunderheart, Mirage Choir, Mirrorwake) as guaranteed-only
  uniques 17 to 22, with an exclusive trigger dispatch on `WeaponController` (`WeaponController.Crystal*.cs`), pooled
  swept projectiles (`CrystalShots*.cs`), a frame-rate independent beam (`CrystalBeamIntegrator`), per-uid heat
  (`CrystalHeat`), procedural models, rigs, beam visual and synthesized audio (`CrystalFocusModels*.cs`,
  `CrystalFocusRig.cs`, `CrystalBeamVisual.cs`, `Audio/CrystalSounds*.cs`).
- `Game.Arsenal` (`Gameplay/CrystalArsenal/CrystalArsenal*.cs`): durable once-per-token entitlements with pending
  claims, resonance case at Veyra Lofts, practice lane, Prism Thread unlock, contract board, first-win boss signatures,
  retroactive claims, ARSENAL claims strip and crystal item cards (`LootArmory.Ui.cs`), Forge guards
  (`ForgeScreen.Armory.cs`). Save block `SaveData.CrystalArsenal`.
- `Game.Encounters` (`CrystalEncounters*.cs`, `EncounterRules04D.cs`, `LocalQuiet.cs`, `AI/EncounterRole04D*.cs`):
  the finite Resonance Breach and the local quiet aftermath with producer hooks.
- Boss upgrades in `Creatures/CreatureBoss.*04D.cs` and `BossProfiles04D.cs` (Foreman, Djinn, Queen; Titan unchanged).
- `SaveFileTransaction` behind `SaveSystem` (verified write with backup and fallback read; `SaveSystem.Delete`).

## Current unfinished or dirty work
None in the cloud clone after the documentation commit. Offline preview PNGs are outside Git (LFS upload blocked
from the cloud environment) and were delivered to the owner directly.

## Tests
- `/home/claude/tools/check_urp.sh <root> <out>` on aa52393: 12/12 configurations, 0 errors, 63 pre-existing warnings.
  Offline compile against assemblies built from the Unity 6000.3.16f1 C# reference source. Not native.
- `cd <root> && /home/claude/tools/check_tests.sh <root> <out>` on aa52393: 730 passed, 25 failed (pre-existing
  `Resources.LoadAll` tests, identical at 351f8ce), 47 skipped NeedsEngine.
- `python3 tools/check_unity_metadata_04c.py --report <file>`: 1,298 sidecars valid, one TerritoryPlacement.
These tools live in the cloud session, not in the repository; on a fresh machine use `tools/test.ps1` with Unity.
Native Unity import/compile: NOT RUN.
Native gameplay/build/performance: NOT RUN.

## Last reproducible failure
None outstanding offline. The last fixed defect (review finding 1): opening the case paused the game, so the board's
START was always refused. Fixed in aa52393 by starting the contract after the screen closes.
Next discriminating test: in Unity, press START on the board and confirm the breach marker appears.

## Next three executable actions
1. Native import and compile on the owner's machine: open the branch in Unity 6000.3.16f1, read the Console, run
   `tools/test.ps1` (EditMode). Success: no compile errors; the 25 `Resources.LoadAll` tests pass in the editor.
2. Native play route from `START_HERE_04D.md` (Free Roam, case, practice, breach, Foreman, save and reload). Success:
   Sunderheart survives reload and fires orbs; quiet holds two minutes after the breach.
3. Polish from real footage: hand and muzzle alignment, telegraph readability, beam daylight contrast, then profile a
   busy fight (QA > Perf flythrough plus a manual fight) before raising any counts.

## Decisions and constraints
- Stable IDs: uniques 17 to 22 (never reorder `LootUniques.All`), tokens `starter:*`, `contract:resonance_breach`,
  `boss:golem_foreman|mirage_djinn|wraith_queen`; `SaveData.CrystalArsenal` and its child classes are frozen in tests.
- Decisions D-04D-01 to D-04D-13 in `Docs/CrystalArsenal04D/DECISION_LOG.md` (Gyre Crescent name, B = 18 at level 1,
  no new input action, full-power Mirrorwake redirect, save transaction).
- Debug tabs: F2 > Crystal weapons (52), Crystal arsenal (53), Encounters (54).

## Recovery
Every checkpoint is on the remote branch. The 04C branch (`feature/inhabited-city-04c`, 8542117) is untouched for
comparison. Keep a copy of your pre-04D save outside the project before the first native test; saves now also keep a
`.bak` beside each slot.

## Owner handoff
Readiness: source-validated, native testing pending.
Normal play route: `START_HERE_04D.md` (explicitly unrun natively).
Do not restart implementation; inspect the state above first. The next session should run native checks and polish,
not rebuild 04D.
