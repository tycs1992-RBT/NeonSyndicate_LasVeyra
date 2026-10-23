# H8S test ledger

All results below come from an **offline reference harness, not native Unity**: Roslyn (dotnet SDK 8.0.131) compiling
against .NET Standard 2.1 facades and reference-source builds of Unity's managed engine and editor assemblies, NUnit,
and a small test runner that honours `[Category("NeedsEngine")]` by skipping those tests. It proves the code compiles in
all twelve configurations and that the pure logic and offline tests pass. It does not import assets, run the engine,
render, play, save or measure performance. Native Unity results are in NATIVE_ACCEPTANCE_H8S.md: all NOT RUN.

## Baseline (fetched HEAD ae5003fc2b3b4e5754c00b95aaaaa97169b8ff3c, C# source SHA256 693cf077...7a9d)

| Check | Result |
|---|---|
| Twelve compile configurations (editor and Windows player x classic / both / Input System only x Built-in / URP) | 12/12 clean, no errors, no warnings |
| Offline NUnit | 1894 passed, 27 failed, 58 skipped (NeedsEngine) |
| The 27 failures | engine-dependent mesh and geometry fixtures, listed by name below; identical in every later run |
| Guard scripts (tools/check_*.py runnable without an Editor) | 42 compared: 21 pass, 19 already fail at the baseline (historical guards whose files later phases legitimately changed), 2 need a genuine Editor (check_04g_compile, check_04g_assemblies: BLOCKED, no Unity install) |
| Unity metadata audit | passed, 2109 metadata files, 0 invalid |

The 27 baseline failure names: ArtDistrictTests (2), CharacterArt04FTests.StandardAndHeroMeshesActuallyWeightEveryFingerJoint
(1, 2), CitadelArt03A2Tests (6), CrystalCity02DTests (2), CrystalCity02ETests (1), CrystalCity02FTests (6),
CrystalCity02HTests (1), StrongholdRebirth04ATests (7). They decode or measure packaged meshes that need the engine.
They are not hidden: every run below compares the full failure list by name and it never changed.

These numbers differ from the historical H8R ledger (1875 passed, 46 failed, 58 excluded) because that run used the
genuine managed Unity modules on another host; the two harnesses are not comparable, so every H8S comparison is made
against this baseline only.

## Checkpoints

| Commit | Content | 12 configurations | Offline NUnit | Guards (new failures) |
|---|---|---|---|---|
| e31e259 | H8S motion: schema 2, landing, quiet loop, H8S review, guard script | 12/12 clean | 1944 passed, same 27 failed, 58 skipped | 0 |
| 333a1a5 | Review fixes on the motion work | focused suites 115/115 | (covered by the next full run) | |
| 21f5d32 | Dante living cast review; companions, cast staging, guidance, ACCESS | builders' own full runs: 12/12 | Dante branch 1943 / 27; cast branch 1917 / 27 | Dante 0; cast 1 expected owner-list entry, added to the guard |
| 8652b18 | Combat readability, Forge and route previews, integration hooks, docs | 12/12 clean | 2075 passed, same 27 failed, 58 skipped | 0 of 43 (includes check_living_cast_h8s) |

## New tests (offline), counted in the 8652b18 run

| Suite | Tests | Covers |
|---|---|---|
| CuratedMotionH8STests | 59 | schema 2 contract and rejections, facing correction, short-arc interpolation, loop closure, landing detector, landings through the real solver, quiet loop, H8S review contract, delivered receipt |
| DanteReviewH8STests | 39 | Dante review contract refusals, bone table, passive bones, sockets, bind alignment on the real skeleton, receipts |
| LivingCastPresenceH8STests | 10 | presence hysteresis, debounce, fallback |
| ForgeGalleryFitH8STests | 10 | anvil fit in the Forge collider, gallery spots, sight lines |
| WorldRouteH8STests | 6 | route stops against the quest, real speeds, clearance, offline revision-10 resolution |
| CombatTellsH8STests | 13 | tell language, true reach marks, minimum wind-ups |
| CombatPacingH8STests | 9 | blow spacing cadence, boss phase beats |
| CombatFeedbackH8STests | 7 | hit weight and kill feedback |
| FocusHeatH8STests | 5 | crystal focus heat cues |
| CastCompanionsH8STests | 23 | barks, spacing, target preference, card, guidance words, hints, idle beats, accessibility maths |
| Total | 181 | |

## Guard script added

`tools/check_living_cast_h8s.py`: frozen owners byte-identical to ae5003f, the Hearth Witness file and every shipped quest
and event file unchanged, curated motion lineage and clip list, candidate receipts hash-matched and still disabled, no
prepared copy or enabled registry committed, no motor, damage, cost, camera, input, time, save or event writer in the
motion code, the authored body still before the original feet, no world revision 11, no em-dash or en-dash in changed
text, LFS for every changed binary, a .meta for every new Assets file. 49/49 at e31e259.

## Independent reviews

| Reviewed | Findings | Status |
|---|---|---|
| H8S motion (e31e259) | 9: supplied clips 13 to 21 degrees off forward, quiet arm frame, loose continuity bounds, mid-tuck touchdown, one-frame head switch, teleport, ready stance, lost switch, test gaps | all fixed in 333a1a5 |
| Combat readability | 5: phantom blow from an abandoned swing (high), lane overshoot, unbooked slams, stacked chimes, tautological tests | being fixed by the combat builder |
| Companions, cast, guidance | 8: boss assist regression (high), shelter flip-flop, lead-the-way reversal, staging versus the quiet review, pause double-tap, tracker flicker, self-confirming test, one hint wording | being fixed by the cast builder |
