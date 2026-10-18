## Post-release crystal creature restoration — unexecuted verification supplement

The prior table (1,342 passed / 46 existing native-call failures / 53 NeedsEngine excluded) is an executed result for the older 04I release snapshot **before** this follow-up. A subsequent arrival-art/stand-down follow-up was also not compiled or tested. The new crystal population, narrative enemies and loot changes introduce additional C# source beyond both snapshots. **No fresh NUnit or Unity Editor run has taken place for the new 04I branch head.**

Current-turn checks actually executed: authenticated GitHub branch readback; source-file fetch and update results; comparison against pre-restoration head `d0622cf49854cea79f66180a7483e8ba18e5cb6d`; static brace/parenthesis/bracket count on five edited C# files; all matched. These are **not a C# compiler, Roslyn parser, NUnit execution or actual playtest**.

New test methods authored in `CreatureLogicTests.cs`: `CityCrystalHordesExistByDayAndGrowMoreDangerousAtNight` and `CrystalHordeElitesAndFocusDropsStayBoundedWithoutTinySwarmFarming`. The old `OuterCitySpawnsOnlyAtNight` assertion was intentionally replaced because the restored city ecology changes that gameplay requirement. Existing `StarterSafetyTests`, `CreatureLogicTests`, `LootDropTests`, mission data, and old-saves must be rerun and compared by actual failure identity.

Native test discriminators: (a) city spawn off-camera and ground/door collision at Low through Ultra; (b) existing starter safety zones; (c) M00/M03 optional waves and cleanup after death/abandon/retry; (d) real player kill/drop pickup, no rewards from NoRewards habitat residents; (e) repeated loads preserving IDs/catalogs; (f) daytime/night population frame time, not estimated FPS.

---

# 04I executed test ledger

## Exact release source

Baseline 04H `50365486bda9d1be5843f4015ba544c8e7f48d72` was freshly cloned and re-executed in a detached comparison worktree. Final gameplay source: `5ea614832129f59fa1c1d8eee0cb1118dc0c9b7b`; its following handoff commit adds docs/tools/evidence. Immutable release C# snapshot: `43f98aad3b7dcf916a030e50ea4e3a84b36b748eb9a27d927b621aa7a1add44b`,732 runtime,10 Editor and109 EditMode files. The release matrix, separate assembly compilation, final NUnit and geometry probe use that source. Existing content JSON plus the new episode was validated with the corresponding compiled schema.

| Actually executed | Result | Meaning |
|---|---|---|
| Baseline managed API matrix |12/12 pass | Genuine metadata, not Unity import |
| Release managed API matrix |12/12 pass | Player/Editor x classic/both/input-only x Built-in/URP |
| Separate assembly boundaries |3/3 pass | Runtime, Editor, EditMode |
| Diagnostic comparison |No introduced/removed warning messages in all 12 |28 classic,26 both,28 input-only warnings per pipeline/source group |
| Baseline external NUnit |1420 discovered;1367 executed;1321 passed;46 failed;53 NeedsEngine excluded | Fresh execution of actual04H, not copied historical numbers |
| Final external NUnit |1441 discovered;1388 executed;1342 passed;46 failed;53 NeedsEngine excluded | Same46 names/messages, no introduced failure |
| New04I cases |21/21 passed |7 art,8 contact/stuck recovery,3 vehicle,3 episode/progression |
| Raw NUnit / comparison exits |46 /0 | Wrapper0 means no new regression; it is not an all-green native suite |
| Roslyn syntax |12/12 pass,0 syntax errors | Parsing only, separate from API compilation |
| Metadata/GUID auditor |1654 sidecars valid;13/13 auditor regressions pass | No missing/invalid/duplicate/unresolved metadata/reference |
| Preservation |6/6 checks pass;2379 old protected files exact | SaveData73/EventSave13/GuidanceSave6 unchanged; WeaponId/catalog exact |
| LFS |437/437 required actual binary hashes matched,151868856bytes; fetch/pull/fsck pass | No new binary object introduced |
| Actual geometry |7 actor samples,9 companion stage samples,32 venue preset/LOD samples,18 fusion samples,2 costume samples executed | Actual builders, not rendering/FPS |
| Existing asset audits |Territory290/290; venue292/292 pass | Numerical geometry, not native traversal |
| Independent Python models |Discovery27/27; forts24/24 pass | Historical reference models, not the C# runtime |
| Hygiene |git diff --check and Python tool compilation pass | No package/pipeline migration |

Evidence/ contains invocations, source/reference hashes, counts, failure identities/messages, exact new cases and actual generated geometry. The53 NeedsEngine exclusions are unchanged. No failure was recategorized to manufacture a pass. The46 external failures remain native ECall/Resources-dependent cases. Native import is the discriminating next test.

## Native environment evidence

The official pinned Editor archive was retrieved, but restoration hit `No space left on device`. The restored executable then exited139 before creating a Unity log or test XML. ELF inspection reported:

```text
missing section headers at 100780280
```

A launch using the prior restored installation exited127:

```text
error while loading shared libraries: libratify.so: file too short
```

A fresh targeted archive extraction still produced an executable failing the ELF integrity inspection; no further launch was attempted. Exact operations/exits and the inspected binary hash are in Evidence/native-environment.json. A supported complete Editor was not established on this host. These are installation/environment failures, not demonstrated game C# failures.

The historical 04H error `Failed to connect to LicenseClient-root channel (error code: 0x80000000)` remains in the preserved04H ledger. It was **not reproduced as a valid licensing probe in04I**; startup here failed earlier. No license bypass was attempted.

Native project import, EditMode/PlayMode, Free Roam, physics, actual save/reload, controller input, screenshots, Windows build and Profiler results: **BLOCKED / NOT EXECUTED**. No FPS or successful player build is claimed.

## Real intermediate failures and review fixes

- A new resident pose initially used nonexistent `HumanoidAnimator.LookAt`, producing two managed compiler errors. It was corrected to the existing `HeadLook` owner; release compilation passes.
- Initial episode validation found twelve data errors: long objectives and two incorrect region identifiers. The existing110-character limit was retained; content now uses actual `pyre_gardens` and `crucible_workshops` IDs. A wrong care anchor was corrected to the authored `care_station` before release.
- Five first-phase checkpoint flags violated the existing first-phase invariant. They were removed, leaving14 real restart checkpoints; original checkpoint tests remain strict.
- Existing tests enumerated five optional sprint jobs. The five actual new chapter IDs were appended explicitly, and the expected total became ten; original16-main/12-side frozen IDs remain unchanged. The intermediate full run had47 failures, the extra one being that obsolete count. The exact release has only the original46.
- Review replaced a cached absolute companion formation point with a cached formation choice and continuously updated target; actual movement remains swept. NPC dialogue was revised to stay in-world. Final source was recompiled and the full suite rerun after these edits.
- The historical managed checker hardcoded a licensing-failure label. The tool and evidence annotation now say it does not execute Unity; real native failures are recorded separately. Test outcomes/identities were not changed.

## Reproduction commands

Use genuine local dependencies: .NET SDK8.0.408/Roslyn; actual Unity6000.3.16f1 managed modules; Editor-bundled URP/Core metadata; official Input System1.17.0 built from its401 source files (source hash recorded in input117-build.json); matching NUnit/NUnitLite3.14.0. The incomplete native installation does not turn these external checks into a Unity import. No engine API stubs were used.

```bash
python3 tools/check_04g_compile.py --root . --dotnet "$DOTNET" --editor-data "$EDITOR_DATA" --packages "$TEMPLATE_ASSEMBLIES" --input-system "$INPUT117_DLL" --nunit "$NUNIT" --output Logs/04I/release-matrix
python3 tools/check_04g_assemblies.py --matrix Logs/04I/release-matrix --dotnet "$DOTNET" --output Logs/04I/release-assemblies
python3 tools/offline04f_tests.py --root . --dotnet "$DOTNET" --unity-references "$PLAYER_REFS" --nunit "$NUNIT" --nunitlite "$NUNITLITE" --assembly Logs/04I/release-matrix/player-classic-builtin.dll --baseline-report <executed-04H-test-report> --output Logs/04I/release-tests
python3 tools/measure_04i_geometry.py --dotnet "$DOTNET" --assembly Logs/04I/release-matrix/player-classic-builtin.dll --unity-references "$PLAYER_REFS" --output Logs/04I/release-geometry
python3 tools/check_visual_rebirth_04a_syntax.py --dotnet "$DOTNET" --report Logs/04I/release-syntax.json
python3 tools/check_unity_metadata_04c.py --report Logs/04I/metadata.json
python3 tools/offline04f_preservation.py --baseline 50365486bda9d1be5843f4015ba544c8e7f48d72 --report Logs/04I/preservation.json
python3 tools/check_living_territories_04b_lfs.py --report Logs/04I/lfs.json
python3 tools/check_living_territories_04b_assets.py --report Logs/04I/territory-assets.json
python3 tools/check_inhabited_city_04c_geometry.py --report Logs/04I/venue-geometry.json
python3 tools/test_crystal_discovery_02g_model.py --report Logs/04I/discovery-model.json
python3 tools/test_strongholds_02i_model.py --report Logs/04I/fort-model.json
git lfs fetch origin feature/crystal-city-ascension-04i
git lfs pull
git lfs fsck
git diff --check
```

Run the baseline commands in a separate exact 04H worktree. Historical installer unit tests require external release fixtures absent from this repository and are not a configured game test target here; they are not claimed passed. START_HERE_04I.md gives native Windows test/build commands and the normal play route.
