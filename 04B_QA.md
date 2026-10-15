# 04B QA and Windows acceptance

## Current evidence

The approved 04A base is `1afbaa9b7e311f977219412a1fb345bf6857c5a6`; the development branch is `feature/living-territories-04b`. The cloud environment has Python, .NET 8, genuine older Unity reference assemblies and Blender 4.3.2, but no Unity 6000.3.16f1 editor. `tools/test.sh` exits 3 with TESTS NOT RUN. No native Console, import, gameplay, physics, screenshot, Windows build or GPU performance pass is claimed.

| Executed offline check | Result |
|---|---|
| Initial 04A source/asset audit | 1,889/1,889; 296 required LFS payload hashes matched |
| Verified baseline NoRewards regression | Repaired method 12/12; original fails the two humanoid no-reward cases |
| Supplemental Roslyn C# 9 comparison | 418 sources; 35 known older-reference errors on each side, zero introduced diagnostics |
| Supported preprocessor syntax | All 12 editor/player, input and render combinations parse without syntax errors |
| Actual engine-free ledger NUnit tests | 14/14 |
| Actual event NUnit tests and strict content validation | 20/20; six templates valid; all 52 shipped IDs unique |
| Ecology managed rules / geometry / recorded-host lifecycle | 100/100, 26/26, 16/16 |
| Source mesh/spatial/metadata audit | 284/284 |
| Repeatable art generation | 66/66 payload and metadata files identical |
| Preservation checks | 13/13; 1,628 protected files and 68,314,612 bytes unchanged |
| Actual city/placement source preflight | Five seeds, 13 locations each; no source invariant failures; stops before native Physics |

Detailed commands, source hashes, logs and scope disclosures are in [Validation/README.md](Docs/LivingTerritories04B/Validation/README.md). The supplemental compiler is not a successful Unity build. The metadata and numerical geometry checks are not Unity import or MeshCollider tests. Population poses in offline images do not prove AI, animations in motion or live placement.

## Required local sequence

All boxes below start **NOT RUN**. Record Unity version, Windows version, CPU, GPU/driver, RAM, resolution, graphics preset, render pipeline, input configuration, city seed, save provenance and exact tested commit. Back up existing saves outside the game directory. Never overwrite the original save during acceptance.

1. [ ] On the original 04A branch/base, import the existing project, confirm zero red compiler errors, run EditMode tests, and play Free Story Mode. Record any baseline errors before switching to 04B.
2. [ ] On 04B, import all LFS assets, compile, run EditMode tests including TerritoryBook, TerritorySave, TerritoryPlacement, TerritoryArt, TerritoryEvent, TerritoryEcology and CreatureNoRewards. Build a Windows player. Exercise the supported input/render configurations without changing project dependencies.
3. [ ] Start fresh Story difficulty and move for five minutes without an unsolicited hostile/police cascade. Verify existing walking, camera, combat, jumping, super speed, flight, driving and gamepad controls.
4. [ ] Inspect `04B_SITE`, `04B_HOME`, `04B_HABITAT_SKIP` and spawn logs. Record actual coordinates, attempted candidates and skipped sites. Compare against [Site inventory](Docs/LivingTerritories04B/SITE_INVENTORY.md). Test a second seed; never interpret provisional source candidates as successful native placements.
5. [ ] Walk through the inhabited Amethyst hub; use a resource or civilian activity; fly to the supported overlook and land. Check both side approaches, steps, collision visibility at each LOD, roof clearance and interactions through walls.
6. [ ] Visit Verdant's planted sanctuary and canopy habitat. Observe actual moving residents, complete peaceful care and inspect the bestiary. Check day/night/weather arrival choices, storm restrictions, 170m load/255m unload hysteresis, body caps, off-camera transitions and missing-body cleanup. Calm animals must not hunt on approach.
7. [ ] Explicitly challenge a relay. Confirm expected difficulty/Low counts, valid species and no ordinary kill rewards. Kill every tracked defender; ownership must remain unchanged until the second explicit core action and six safe seconds. Interrupt by damage, departure, flight, ragdoll and vehicle entry. Pause during combat and channel; it must not complete in the background.
8. [ ] Abort by leaving, death, title, load, mission, Veil, missing defender and failed body initialization. Partial spawns must clean up without a reward. Starting 02I raids, territory captures and events must be mutually exclusive.
9. [ ] Complete capture. Observe owned crystals/banner cue, O marker, finite material cache, saved discovery and Hearthspire progress. Try repeat interaction, reload and replay; no duplicate capture, resource, event or care rewards.
10. [ ] Buy each tier beside the physical core. Check exact MaterialStore deductions, solid visible cover, recovery 25/20 then 50/40, saved 180-second recharge and stronger requested defense wards. Place a person/vehicle in future cover bounds and confirm the upgrade refuses before spending. Walking far away or selecting another relay must not permit remote construction.
11. [ ] Save and reload an existing pre-04B save, explicit null territory data, fresh game and completed 04B save. Verify original cash, materials, quests, creatures, factions, 02G discovery, travel, three original forts, all six socket settings, themes and infused echoes. Return to title and resume repeatedly with domain reload disabled.
12. [ ] Visit Hearthspire, consult the actual caretaker ledger, care for its resident and inspect the finite nine-crystal display. Verify the original western stair, landing surfaces and construction sockets remain accessible. Disable 04A gardens and confirm the dependent home annex skips safely.
13. [ ] Complete all six event loops with keyboard and gamepad, including wrong resonance order, damaged patient, lost observation LOS, interrupted tending, failed rift deadline and departure. Verify one-time rewards, 600-second success/30-second failure cooldowns, load cleanup and zero endless reinforcements. Test Story, Balanced, Veteran and Low caps.
14. [ ] Drive the adjoining roads, fly through the busiest region, shift to/from the Veil, visit a 02G shrine, challenge/capture both original rival forts, customize the original home and verify original +8% maximum flight/+6% maximum climb echoes.
15. [ ] Disable each 04B feature separately, restart Play Mode and verify the prior game survives without stale actors/markers/meshes/lights. Re-enable and confirm the saved ledger returns without duplication. Test 04A on/04B off, 04A off/04B on and both on.

## Native visual and performance evidence

Use the existing 04A route and **Neon Syndicate > QA > Capture Living Territories 04B Looks** under the same seed/resolution/preset/pipeline. The new route records actual placed anchors, noon/night, original matched city/home IDs, populations, object/render counts, memory, draw/SetPass/triangle counters when available, 120 frame samples and percentiles. Hardware and pending-versus-active switches are included. Missing platform counters are -1, never fabricated numbers.

Use **Capture Current Territory Moment** during real player capture, defense and civilian play. It retains the camera, HUD and active encounter; confirm it also preserves original 02I raids. Capture without the ordinary QA busy flag is intentional. A camera tour cannot demonstrate gameplay acceptance.

Compare 04A and 04B at a steady site, crowded hub, flying across streaming boundaries, live raid and worst-case night scene. Record CPU/GPU timings if available, allocated memory, draw calls and frame-time p50/p95/p99. Check actual Low and High. A 1080p/60 FPS goal is aspirational; no cloud FPS result exists. Supported overlooks and body/cover occupancy must be examined from player and flight cameras.

## Source budgets and remaining limitations

Six original mesh masters total 14,322 high and 5,158 low triangles. Exact selected layout/state totals are in [art_geometry_budgets.json](Docs/LivingTerritories04B/Validation/art_geometry_budgets.json). Dense high-detail ferns dominate several planted courts; Low and far LODs sharply reduce them. Those are source counts, not memory or draw-call measurements. All thirteen regions/habitats are resident geometry after boot with distance LOD; only actors stream. This is a known memory cost to profile.

Preset resident-creature caps are 3/5/8/10 and staff caps 2/4/6/8 (Low through highest). Spawning is time-sliced; wildlife reserves four director slots for encounters. Captures use at most two authored wardens in this catalog; explicit events cap at 2/3/4 by difficulty and two on Low. New accent lights are shadowless, three created and at most two active nearby at night; none on Low. Existing shared 04A effects retain their old caps.

Native terrain, collider import, look quality, controller ergonomics, loading times, game balance and frame performance remain open acceptance items. Conservative surveys can skip relays, actors or the home footer. The rift wildlife is dangerous when provoked; no hostile army appears automatically. The courier aid uses existing cower/idle poses, canopy fieldwork observes a ground animal, and offline actor pictures are static procedural poses. There is no new motion-captured animation pack, taming, companion following, network PvP or server authority.

The render review corrected grounded root bounds, sparse planting, missing Amethyst shade, floating banner supports and an incorrect offline paving scale. These refinements are in the shared source. Actual Unity appearance still needs the native captures above. Merge and public release remain pending local acceptance.
