# 04J THE SHARD WAR — install and test the existing game

Status: SOURCE-INTEGRATED, UNITY-UNVERIFIED. This is NOT an installable Windows binary or a claim of a playable build. Read Docs/ShardWar04J/TEST_LEDGER.md and FEATURE_STATUS.md.

## Windows update (same Unity project, no new project, no merge)
1. Close Unity and back up existing saves to a different folder. Review and preserve local uncommitted work first.
2. Open PowerShell in the existing repository folder, for example C:\games\NeonSyndicate_LasVeyra.
3. Execute:

```powershell
git status --short
git fetch origin
git branch -a
git switch feature/shard-war-horde-renaissance-04j
git pull --ff-only origin feature/shard-war-horde-renaissance-04j
git lfs install
git lfs pull
git lfs fsck
git rev-parse HEAD
```

If git switch says the branch does not exist locally, use `git switch --track origin/feature/shard-war-horde-renaissance-04j` (only then). Never reset/force-push local changes, and never merge into main. Check disk space before hydration. Expected Editor 6000.3.16f1, revision a56f230f6470, Input System 1.17.0, Built-in renderer with original optional URP support. Install exact Editor via Unity Hub and activate a legitimate Unity license.

## Import and actual engine checks
Open the existing checkout in Unity Hub, NOT Create New Project. Check Console before Play. Investigate and record the FIRST import/compile error if any. When the Editor compiles, exit Unity for batch tests and run:

```powershell
powershell -ExecutionPolicy Bypass -File tools\test.ps1 -Platform EditMode
powershell -ExecutionPolicy Bypass -File tools\test.ps1 -Platform PlayMode
powershell -ExecutionPolicy Bypass -File tools\build.ps1 -Target Windows -Development
```

The Windows build script targets Builds\Windows\NeonSyndicate.exe. Only install/use that executable after a successful compile, full build, launch and reach-Free-Roam test. No executable was built by the connected-agent session.

## Real player test: first five minutes
New Free Roam: from Veyra Lofts, observe safe arrival areas and original station officers. Walk 60–140 m outside the protected welcome radius, look away from prospective spawn sites, and wait a few spawn cycles at daylight. Find Hollows and Glass Mites normally, then repeat at night. At F2 > Creatures read new Last/Planned/Placed/Reject counters. No-spawn must be accompanied by its ledger, current location, weather and graphics preset. Repeat outside parks, worksites and Grave Frequency. Verify no mobs are inside doors, roads, quest markers or physics props.

Fight a non-Mite crystal creature or crystal-armed gang leader; if it drops a focus weapon, verify the rotating physical model, beam, Use prompt, inventory add and equip, then save/quit/reload on a COPIED legacy save. Normal enemies are not guaranteed drops. Random Legendary casters can be Veil Whisper or Prism Riot; a random kill must not grant boss-only unique 17–22. Full-backpack handling, uniqueness and rarity pickup limits require manual validation.

## Story and preserved saves
In a copied campaign save separately test M00 Third Eye glass wake, Wardens and escape/prologue skip, M03 False King optional 20-second alpha pack, and M08 Signal War tower Hollows, elite shade and caster guards. Fail, retry and abandon each where supported; confirm all tagged summoned creatures clean up. Replay 04G courier, 04H Quiet Circuit and the five 04I Ascension chapters. Compare before/after save IDs, owned weapons, QuestBook steps, bonds quartz_guide/ash_warden/storm_skimmer, three earned fusion chassis, residences and vehicles. Never overwrite original saves in an experimental editor run.

## If source CI passed but Unity fails
Read Logs from the test/build scripts and the first Unity Console error; a source syntax pass is not native C# API typechecking. Capture exact Editor version, log, scene, steps, and bug ticket before modifying 04I or main. See Docs/ShardWar04J/PRESERVATION_AND_RISKS.md.
