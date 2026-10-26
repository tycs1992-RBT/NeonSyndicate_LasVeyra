# FR01E: source and art repaired, native acceptance open

Fetched HEAD: 954ed0f67c127b49db5de974d742393ad6c5ca5b. Exact tested source: 9f519edbba508470234b8490603b89dd83c398b1. Normalized source/art: f8f41b77a66e3e23fcbd9ce11abaafa55161ad68. C# SHA256: c468e1a0ab6114bbe3844533de4d2d20b89a332bb6729d26fd4ca8c43d631429. The final evidence-only descendant is named in PR12 and the delivery response.

**Overall: PARTIAL.** Real source and editable asset corrections are delivered. Native Unity and owner visual acceptance are open. No score, playable-release approval or next sprint is certified.

## What visibly changes when enabled

- The original gateway now has a 14m pointed arch, 13m between the new base proxies, 23m total source width and unequal roof crowns. Its original opening was stretched to about 39m. The corrected model uses scale 1, preserving stone and doorway proportions.
- Lower-detail house walls and tower masonry now reach their roofs and cornices. Previously LOD1/LOD2 deleted vertical courses without redistributing them, leaving large holes at ordinary viewing distances. Nine existing exports and their editable Blender source were corrected. All fifteen textures and the other twenty-four exports are unchanged.
- Existing arcade and blind-bay meshes now follow the actual sanctuary route edges. Sixteen bounded proposals replace the old mostly-refused fixed grid. Eight pass the source route/parcel rules at the first source candidate; the corrected static source review shows six bays with the later clear gate. Native solids/triggers can reduce these counts further. This is route framing inside FR01, not new buildings or FR02.
- Asymmetric imported house bounds are centered on the original footprint and ground plane, rather than shifted by the old bound-center offset. The same two nonservice masses, seeds 961727 and 961728, qualify. Single-LOD H8R reuse now uses the far silhouette threshold instead of culling at the near-detail threshold.

Pause > EXPLORE retains Gothic Heart Renaissance and Route: Gothic Heart. Default stays OFF. The comparison explanation now includes Gothic Heart. No camera takeover or teleport is used.

## Gate admission and preservation

The natural-sized piers cannot fit the old home-court crossroad while retaining all public envelopes. The bounded planner therefore samples the existing connected court-to-sanctuary path. It does not widen the model or relax the original 3.4m source clearance and its safety margins. Eighteen exact candidates are tested once; the runtime additionally probes the full tower heights and overhead arch, all World solids and all trigger volumes.

The first source-clear proposal is around (8.7, 0.15, 58.3). Independent triangle/box review of the retained H3/H5 geometry refuses the early positions. Its first static-clear proposal is candidate 9, (12, 0.15, 75.1). That is source evidence, not native admission. The actual Unity collision/trigger result remains authoritative. Every physical refusal retains its candidate and collider/trigger name. If all fail, the gate is absent, the original world stays and the ordinary route button reports FAILED admission.

Side dressing reserves the whole admitted gateway so it cannot intersect the new piers or arch. Activation also refuses people or vehicles already in a base. Failed visual captures no longer inflate the showing-mass count. An incomplete three-LOD core catalog fails closed. Original city and service collision, H3/H5, NPCs, quests, story M00-M11, BONDS, Forge, seventeen discoveries, historical banks, revision 10, character metadata, L1/L2/L3 and the L3 frost repair are unchanged. CHANGE_MANIFEST.json records the narrow changes and protected trees. No shared city renderer is hidden.

## Executed tests

| Check | Result |
|---|---|
| Genuine managed API, external / bundled NUnit | 12/12 + 12/12 PASS |
| Runtime / Editor / EditMode boundaries | 3/3 PASS |
| C#9 syntax | 12 modes, zero errors |
| FR01 plus FR01E pure cases | 24/24 PASS, 13 new |
| Fresh baseline full NUnit | 2369 passed, 47 failed, 64 excluded, raw FAILED |
| Final source full NUnit | 2382 passed, 47 failed, 64 excluded, raw FAILED |
| Exact failure comparison | No introduced/resolved failure names or changed reasons |
| L1 / L2 / L3 guards | 51/51, 65/65, 63/63 PASS |
| General ascension / H3 / FR01 assets | 26/26, 78/78, 144/144 PASS |
| Metadata / references and regression cases | PASS, no invalid GUIDs or unresolved references |
| Actual Blender FBX reimports | 33/33 PASS |
| Strict independent whole-kit parse | 33/33 PASS |
| Independent full-height wall support | 9/9 PASS, before 2/9 |

Fresh H5 baseline and edited source both report 46/48: Original actor/gameplay authority / Characters/HumanoidPose.cs; All prior tests and exclusions preserved. Source CI 40354951419 and PR run 40354963357 fail at those same assertions, after passing the new FR01E strict/support step and the L1/L2/L3/FR01/Redspire steps. Later skipped steps remain skipped. LFS normalization run 40354951444 passed. Final remote hydration and final-head runs are recorded in the publication seal and PR12. No preservation assertion was weakened.

Raw XML/log archives and JSON are retained. TEST_LEDGER.md explains the early focused-filter mistake and corrected pure run. Managed compilation and NUnit are not Unity Test Runner.

## Evidence correction and remaining visual blockers

The previous FR01 DCC recipe looked for nonexistent Models/*.obj files and silently omitted the retained H3/H5 meshes. Those historical images are not reliable full-context composition evidence. Their receipts remain intact; the old entry point now forwards to the corrected recipe, which fails on missing model files.

The new labeled DCC views decode the actual CMH1 mesh files, include 355 retained placements and 98 compiled structural/family meshes, and compare legacy OFF, prior FR01 and FR01E from matched low viewpoints. Five unrefined service shells are represented only by their explicit approximate source envelopes: inn, garage, depot, pressure and observatory. The source artifact is not a complete simulated game scene. Static source probes do not simulate actors, triggers or native contact. NPC absence in DCC is not proof that the game is empty. Native terrain, post-processing, imported materials and performance are not reproduced.

The retained H3/H5 Archive, its tall mineral piers and remaining service/background forms still require actual hero-view review. The two replacement houses alone do not certify a transformed skyline. The original collider versus imported-shell relationship also needs real walking, camera and earned traversal tests. If the ordinary view still reads as a modern city with a gate attached, artistic acceptance is FAILED or PARTIAL, never PASS from these renders.

Native Unity executable was absent from PATH, environment and checked install locations. Native import/shaders, Test Runner, PlayMode, actual saves, controllers, interaction/NPC/companion traversal, collision/camera/vehicle/flight, screenshots, Profiler and Windows build/launch are individually **NOT RUN**. L1/L2/L3 native acceptance also stays open. NATIVE_ACCEPTANCE.json records each gate. No numerical visual score is valid yet.

The existing QA-mode suspension policy is retained. Automated QA captures/flythrough hide FR01 temporarily, so they cannot be used as its matched appearance/performance proof. Capture ordinary HUD-on play and attach the Unity Profiler to that actual route instead. QA suspension/restoration itself still needs native testing.

## Owner review and next sprint

Follow OWNER_WALKTHROUGH.md for the ordinary 5 to 15 minute route. Each art sprint must now name a clearly visible change, its ordinary route and matched OFF/ON view. Source delivery can be partial; visible acceptance requires actual Unity evidence and the owner's confirmation. This is a mandatory continuation rule, not automatic approval.

PR12 stays draft on its original base and unmerged. PR11 is untouched. No force push, reset, rebase, save migration, renderer/default change, FR02 or L4 occurred. FR02 is not authorized. The next action is the smallest native import/route review, followed by focused corrections to its first demonstrated failure.
