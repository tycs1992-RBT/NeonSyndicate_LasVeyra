# RESUME NEXT: exact continuation state (04D)

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
