# 04B validation evidence

These reports describe checks actually executed in the Linux cloud workspace. Unity 6000.3.16f1 is unavailable: the [final native test attempt](unity_test_attempt.txt) exits 3 with `TESTS NOT RUN`. Native Unity compilation, asset import, EditMode and PlayMode execution, Windows player tests, physics, live rendering and frame-time measurements have **not** run. The checked-in native tests and capture tools are ready for that next stage.

The preserved 04A baseline is `1afbaa9b7e311f977219412a1fb345bf6857c5a6`. Its audit is in [BASELINE_04A.md](BASELINE_04A.md). The verified baseline repair for humanoid `NoRewards` handling is isolated in commit `5d7c92d`.

| Evidence | Executed result | What it establishes |
| --- | --- | --- |
| [Supplemental compiler comparison](diagnostic_comparison.json), [working log](working.log), [baseline log](baseline.log) | 418 working runtime/test sources; 35 baseline and 35 working errors; zero introduced errors | Real Roslyn C# 9 semantic diagnostics against genuine older Unity 2021.3 references. These references lack Unity 6 APIs, so this is not a successful pinned Unity compilation. Only the existing `PhysicsMaterial` spelling is aliased in both scratch copies. |
| [Configuration syntax](syntax_configurations.json) | 12 configurations; zero syntax errors | Real Roslyn parsing for editor/player, three input modes and Built-in/URP. Does not resolve platform APIs or build a player. |
| [Ledger tests](book_tests.json) | 14/14 NUnit cases | Actual unchanged engine-free ledger/data/JSON-reader source: old/null/corrupt saves, field and ID freeze, guards, explicit capture arming, damage/range/pause cancellation, duplicate rewards, claim caps, rest cooldown, fortification tiers and cloned snapshots. |
| [Event source validation](events_source_validation.json) | Six new templates valid with zero errors/warnings; 52 shipped unique IDs; 20/20 NUnit cases | Actual parser, catalog, selector and pure rules, exact extracted strict-validator methods, and real reference ID tables. Existing ambient selection and invalid-template rejection are covered. Event coroutines and native actors are not executed. |
| [Ecology rules and source geometry](ecology_source_checks.json) | 100/100 managed rule cases; 26/26 high/low source slot envelopes | Actual managed selection/cap/hysteresis rules and conservative numerical geometry. Does not prove live creature AI or collision behavior. |
| [Ecology lifecycle](ecology_lifecycle_checks.json) | 16/16 cases | Actual scheduler/cleanup methods with recording host doubles; allocation is suppressed. Covers retained pause, resume, title/death/Veil, owner disable, failed-body timeout and idempotent cleanup. |
| [Art and placement geometry](assets_spatial.json) | 284/284 checks | Actual CMH1 payloads and OBJ parity, normals, UVs, mesh budgets, material roles, bounds, approaches, core/staff/defender clearance, landing support/headroom, home ledger display and metadata. Uses source triangles and authored transforms, not native MeshCollider queries. |
| [Art reproducibility](art_provenance.json), [selected geometry budgets](art_geometry_budgets.json) | 66 regenerated payload/metadata files identical; 33 GUID/hash inventory entries; 22 selected state/variant budgets | Reproducible original assets and exact source triangle counts. Does not establish import or runtime performance. |
| [City candidate preflight](placement_candidates.json) | Five city seeds; 13 locations per seed; zero invariant failures | Actual city layout and placement source with the disclosed managed math adapter. Execution stops by throwing at the first native ground ray. Recorded coordinates are provisional source-safe candidates, not verified live installed sites. Original preferred fort anchors are reserved; actual 04A court colliders and live interactable positions require Unity. |
| [Preservation](preservation.json) | 13/13 checks | Exact protected baseline assets/settings/input/travel/02G/02I data, append-only save fields, narrow reviewed host edits, reset/boot order, source payloads, LFS rules and metadata. Removing the two-line encounter guard recovers the original stronghold runtime byte for byte. |
| [NoRewards regression](no_rewards_regression.json) | Original method fails two humanoid exclusions; repaired method passes all 12 cases | Exact production method executed with recording reward/notification doubles. This verifies the branch repair, not native death animation or scene lifecycle. |

The six new art masters total 14,322 high-detail and 5,158 low-detail triangles across 12 mesh variants. Those are library geometry counts, not live scene draw-call, memory or GPU measurements. CPU source previews under `../Previews` are explicitly labeled and are not in-game screenshots.

## Reproduce the offline checks

Run from the repository root. The paths below describe the prepared cloud environment; substitute installed SDK/reference paths elsewhere. Python geometry checks require NumPy and Pillow. The comparison tool needs the genuine older Unity modules and NUnit; it must not be substituted with fabricated API stubs.

```bash
python3 tools/run_living_territories_04b_book.py --dotnet /workspace/.tools/dotnet/dotnet --nunit /workspace/.tools/nunit/lib/netstandard2.0/nunit.framework.dll --report Docs/LivingTerritories04B/Validation/book_tests.json
python3 tools/run_living_territories_04b_events.py --dotnet /workspace/.tools/dotnet/dotnet --nunit /workspace/.tools/nunit/lib/netstandard2.0/nunit.framework.dll --report Docs/LivingTerritories04B/Validation/events_source_validation.json
python3 tools/check_territory_ecology_04b.py --dotnet /workspace/.tools/dotnet/dotnet --report Docs/LivingTerritories04B/Validation/ecology_source_checks.json
python3 tools/check_ecology_lifecycle_04b.py --dotnet /workspace/.tools/dotnet/dotnet --report Docs/LivingTerritories04B/Validation/ecology_lifecycle_checks.json
python3 tools/check_living_territories_04b_assets.py --report Docs/LivingTerritories04B/Validation/assets_spatial.json
python3 tools/check_living_territories_04b_reproducibility.py --confirm-regenerate
python3 tools/check_living_territories_04b_preservation.py --report Docs/LivingTerritories04B/Validation/preservation.json
python3 tools/run_living_territories_04b_placement.py --dotnet /workspace/.tools/dotnet/dotnet --catalog Assets/NeonSyndicate/Resources/LivingTerritories04B/territories_catalog.json --report Docs/LivingTerritories04B/Validation/placement_candidates.json
python3 tools/check_visual_rebirth_04a_syntax.py --dotnet /workspace/.tools/dotnet/dotnet --report Docs/LivingTerritories04B/Validation/syntax_configurations.json
python3 tools/compile_visual_rebirth_04a.py --dotnet /workspace/.tools/dotnet/dotnet --unity-references /workspace/.tools/unity-references/lib/netstandard2.0 --nunit /workspace/.tools/nunit/lib/netstandard2.0/nunit.framework.dll --output /workspace/.tools/04b-final-compile --baseline 1afbaa9b7e311f977219412a1fb345bf6857c5a6
```

Native acceptance must include the pinned editor and input/render configurations, old and new save loading, actual site-survey failures, navigation and flight landings, tier installation around actors, body-worker caps, event/capture/fort mutual exclusion, abort paths, home propagation, and actual screenshots/performance captures. Capturing the current moment during an original fort raid must preserve the active raid; that capture uses a separate moment job instead of `QaTools.Busy`.
