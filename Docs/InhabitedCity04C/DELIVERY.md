# Master Build 04C: The Inhabited City

All eight destination layouts, their runtime construction and their service/activity integration are implemented on `feature/inhabited-city-04c`. This is a reviewable source delivery for Windows testing. **Native Unity acceptance is pending**, because Unity 6000.3.16f1 is unavailable in the cloud. No native appearance, gameplay, build or performance result is claimed.

The original 04B base is `c49144150cfeb2f105f628f7f9d6ed85c1ccb218`. Work resumed from the published unfinished checkpoint `5a7693e21b3aadd6d31c2dc3eee60f845e5acbff`. Published development checkpoints include `d7e74d4` (resident admission), `bb3171f` (remaining five layouts) and `d698c23` (room refinement and physical services). No merge into main is part of this delivery.

## Destinations and actual functions

Every venue uses a physical opening in its existing city parcel. There are no interior teleports, detached underground rooms or additive-scene transitions. Collision stays resident; optional people, lights and sound are bounded separately. The table describes implemented source behavior, not completed native playthroughs.

| Destination | Playable space and furnishings | Existing function or integrated activity |
| --- | --- | --- |
| Veyra Lofts | Communal lobby, public ledger, real stairs, furnished upstairs apartment and partitioned sleeping area, supported balcony | Original safehouse/rest/save/wardrobe callback at lobby, bed and desk. Nessa's chapter requests. Display follows actual 04B ownership and final chapter completion. No stash is advertised. |
| Mara Solane Street Clinic | Reception, waiting bench, screened treatment area, cot, wash vessels and supply nook; clean original plaster finish | The original treatment and free/paid eligibility, plus manifest and supply inspection through QuestBook. |
| Dante Rook's Crystal Forge | Workshop, ventilated hearth, tool rack, material shelves, recipe ledger and repair workbench | Original complete Forge station, inventory, recipes, discounts and faction/combat rules. Real salve crafting or a free finite loaned-tool calibration for the chapter. |
| Cinder Diner | Public dining tables/chairs, counter, cooking hood, ceramics, provision shelves and courier corner | Original $40 meal restoring 80 health; persistent useful directions and the chapter's alternate investigation route. |
| Resonance Archive | Reference desk, three reading stations, shelves, study table and receiver exhibit | Three authored records with saved reading history; the existing QuestBook offers the recovery contract at its desk. No second reputation economy. |
| Sundial Survey Observatory | Ground-floor survey desk, actual stairs to an upper instrument room, three survey instruments and an opening/balcony toward the existing city | Final chapter offer, lens alignment, tuning and steady-focus calibration. Existing flight powers remain unchanged. |
| Jax Calder Customs | Wide open vehicle bay, parts shelves, engine stand, tool rack, work lights and pedestrian enquiry desk | Original vehicle-only repair/paint/customization trigger inside the bay; free on-foot instructions. Original roads and vehicle-delivery triggers retain their positions. |
| Rift Freight Depot | Loading room and store room, central aperture and alternate side passage, cargo cover, bypass console and relay bench | Optional chapter operation: peaceful isolation or explicitly accepted finite defense, followed by actual retrieval. No unsolicited battle, reinforcements or kill payouts. |

The Quiet Signal consists of five saved, nonrepeatable quests in the existing JSON/QuestBook system. Requests move from home to the forge, archive and observatory. The investigation choice changes the destination; the depot choice changes the operation. Finite rewards total $620 plus the first stage's 2 Shard Chunks and 2 Hollow Essence, with bounded existing faction reputation. Existing quest completion owns the payouts; entering a room or rereading a record grants none. See [QUIET_SIGNAL.md](QUIET_SIGNAL.md) and [SERVICES_AND_CAPTURE.md](SERVICES_AND_CAPTURE.md) for exact rules and costs.

## What changes at ordinary arrival

The existing Free Story arrival stays at Veyra Lofts. Its lower frontage now has an open carved threshold, readable physical name, visible lobby, framed windows, a supported balcony and a usable apartment above. Cinder Diner has fabric and timber frontage, the archive a civic entrance and shelves, and the adjacent observatory a vertical instrument destination. Other selected buildings have purpose-specific fronts, including the forge flue and wide garage opening.

Sixteen original model masters have editable OBJ sources and runtime high/low payloads. The library contains 11,536 high and 6,976 low triangles, before room repetition and preserved upper architecture. Two original reusable surface textures provide timber and clinic limewash; other explicit surfaces reuse the existing 04A library. Source layouts are deterministic data assemblies, with no manual scene placement required. Source models, textures and helpers are project-owned; no third-party models or paid services are required.

The canonical clinic and forge are in different districts. **The compact home-clinic-forge circuit is not achieved.** Their original identities, story anchors and map references were retained. The concentrated arrival neighborhood is home, diner, archive and observatory across the existing two-block area. Clinic, forge, garage and depot extend the guided route through the original city.

## First play route

1. Use the ordinary Free Story start, or an existing test save. Find Veyra Lofts at its original map icon, walk through the open door, use the lobby, climb the stairs, open/cancel the bed or desk's original safehouse menu, and leave through the same doorway.
2. Accept Nessa's request in the lobby. After its first stage, use the journal and PLACES entrance tracking to reach the real Mara clinic and Dante forge. Enter each building to use its physical service and chapter station. Offers after the repair appear inside the forge.
3. Investigate the archive records or diner courier, accept the recovery contract at the archive desk, visit the depot and choose the peaceful bypass or the explicit encounter on separate test saves.
4. Visit the observatory, go upstairs and calibrate its instruments, then return home to install the receiver. Separately drive a vehicle into the customs bay, purchase/cancel the original service and reverse out.

Production walking-graph estimates for seed 61724 are about 12 m from arrival to home, 83 m to diner, 152 m to observatory and 201 m to archive. Home-to-clinic is about 644 m and clinic-to-forge about 1,574 m. These are source estimates, not native walked distances; live PLACES/GPS uses actual entrance positions. The 20-40 minute pacing target is unmeasured. The original new-story prologue and existing save spawn policy are preserved, except for the documented safe exterior resume when a save is captured inside.

## Validation and evidence

Run `python3 tools/validate_inhabited_city_04c.py` with the retained SDK/reference paths or pass its command-line overrides. It writes exact command output and exit status to `Validation/`. Source render verification runs separately after frames finish. See [INTEGRATION.md](INTEGRATION.md) for ownership and [ACCEPTANCE.md](ACCEPTANCE.md) for the complete Windows test itinerary.

| Evidence category | Executed result / limit |
| --- | --- |
| Venue, ledger and service source logic | 48 managed/NUnit cases, zero failures; production CityLayout/binding/ledger/service guards with disclosed arithmetic/host adapters. |
| Chapter | 5 quests, 13 phases, 20 steps; strict validation has no errors/warnings; 23 chapter/retained guild-chain NUnit cases pass. Native coroutines and payouts are unrun. |
| Residents | 73 source rule/lifecycle cases pass, including matching-mission admission, cleanup and new-session retry reset. Live bodies/physics/audio are unrun. |
| Geometry and assets | 610 numerical checks pass for actual production room expansion and high/low mesh sources; 133 staffed-route support/clearance checks pass. This does not prove controller traversal. |
| Metadata | 1,227 regular sidecars audited; no invalid GUIDs, duplicates, missing/orphan sidecars or unresolved serialized references; all 13 format regressions pass. Native imports are unrun. |
| Preservation | All 10 checks pass; 1,765 protected existing files/payloads remain exact against repaired baseline, including original assets/settings/travel/fort/territory content. Native save loading and gameplay are unrun. |
| Retained 04B checks | 290 asset checks, 100 ecology rules and 26 source habitat checks pass. Six 04B event templates validate and 20 pure event tests pass. This is not a full native 04B regression. |
| C# | Twelve conditional syntax configurations pass. Supplemental compilation of 445 runtime/test sources against genuine older Unity references has the same 35 API diagnostics as 04B and zero introduced errors. Pinned runtime/Editor compilation is NOT RUN. |
| Visuals | Labeled Blender source previews and recorded input hashes, with real room/parcel source, approximate lighting/materials/font and static existing-model idle poses. No Unity game renders. See `Previews/README.md`. |
| Native tests/build/performance | Repository test runner exits 3, explicitly TESTS NOT RUN because the pinned Editor is missing. No PlayMode, Windows player build, native controller/service/quest playthrough, frame time or memory acceptance. |

## Preservation and limits requiring Windows review

The metadata repair remains intact: exactly one canonical TerritoryPlacement implementation, two formerly malformed metadata IDs repaired, and the invalid 04A shader references corrected without regenerating valid identifiers. The separate locally repaired Windows filename/GUID was never accessible. Preserve that working pair and other edits before switching branches; follow [BASELINE_AND_METADATA.md](BASELINE_AND_METADATA.md) instead of copying a renamed duplicate into the project or deleting the full cache.

The default seed selects eight suitable parcels. Seed 77231 retains the original diner because its canonical building is protected/tiered; dependent chapter availability reflects missing venues rather than opening an unsafe shell. Other layouts/seeds can also fall back. Native tests must confirm import success and all runtime fallback paths.

Inside saves resume at a verified exterior candidate for the same venue, with the existing home fallback if all candidates are blocked. Exact interior positions, transient people, health and running combat bodies are not serialized. Eight critical shells stay resident. Optional resident caps and at most three nearby shadowless practicals are intended limits, not measured performance guarantees.

Shared camera-following weather particles are suppressed while roofed, including particles that would otherwise be seen outside through windows. Indoor fog reduction also affects that window view. Native rendering must judge these scoped compromises. Services do not depend on a cashier successfully spawning, and optional room disable never removes an occupied floor.

The next recommended development focus is the pinned Windows acceptance pass: continuous normal-play entry > service > exit, both storeys, garage driving, all five quest stages, old-save restoration, 20 repeated entries, Low/default/High, 04B/02I regression and actual screenshots/frame timings. Refine against that evidence before expanding the neighborhood further.
