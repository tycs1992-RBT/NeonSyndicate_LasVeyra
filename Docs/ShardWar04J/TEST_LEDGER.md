# Executed evidence and missing native validation — 04J

## GitHub baseline
04I checked head: 1b0f8275ad3952784939168a80e704f22863cf69. Verified from live refs/heads GitHub API before branch creation. No preexisting 04J branch. 04J started at that exact SHA. GitHub App reports repository push rights. Prior 04I draft PR #4 and main were not edited or merged.

## Real NEW execution
Source CI: https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/actions/runs/39191627006
Run 39191627006, at commit 2d55ce1c5b52c3c075843211d9dcbf45eba0f82c, reported completed SUCCESS. Steps independently reported success: checkout of exact 04J tree, .NET SDK setup, Python 04J source wiring checker, adapted 04I preserved wiring checker, all-file Roslyn syntax parsing across 12 defined configurations, Unity .meta/GUID auditor, git diff --check versus live 04I, evidence artifact publication.

A later 04J source revision additionally integrates deterministic repeat-pack weighting into the original CreatureDirector and authors CreatureLogicTests.ShardWarAmbientPackDiversityRespectsFiniteBudgetAndRetainsBothSpecies; no native NUnit run of that test exists. The latest CI workflow must be checked for the exact final handoff SHA, not assumed from the historical first 04J CI pass.

Source validation proves only these precise assertions and parse passes. It is NOT a Unity C# typechecked build, and tests in Assets/NeonSyndicate/Tests/EditMode were authored but not executed. Historical external NUnit result 1,342 passes/46 ECall failures/53 exclusions belongs to EARLIER 04I checkpoint, not this 04J tree. Do not copy those counts forward. LFS pointer/hash count 437 belongs to an earlier 04I baseline; not hydrated or reverified here.

## Local host
Local Git binary exists but GitHub network does not resolve (git ls-remote failed 'Could not resolve host: github.com'). Local dotnet, csc and Unity editor executable not found. Local disk had ~30GB free at check; a licensed Unity Editor installation was NOT attempted without binaries/license. No Unity import, native EditMode/PlayMode, editor Console XML, physics spawn trace, Windows build, screenshot, profiler, in-game inventory save/reload or 30–60-minute play session was performed.

## Following CI changes
This handoff documentation commit occurs AFTER the successful initial run. Recheck latest HEAD source CI via Actions before accepting the final exact revision; no earlier pass should be represented as a pass for any later code changes.

## Tests to run on Windows
Run tools/test.ps1 -Platform EditMode, tools/test.ps1 -Platform PlayMode, tools/build.ps1 -Target Windows -Development on exact editor 6000.3.16f1 after Git LFS fsck, and attach first errors and XML. In Free Roam run both times of day; inspect F2 spawn counters, normal/hard crystal opponents, original M00/M03/M08, drop/equip/full backpack/rare+unique/save reload, copy of old saves, 04G courier/04H/04I chapters, four graphics tiers and frame profiler. Claim native acceptance ONLY after real evidence.
