# The Lofts terrace and Dante's Forge

The supported PLAY FANTASY WORLD route now has two separate landmarks. From home, turn back toward the Lofts to see the planted upper edge, a slatted seat under a projecting stone canopy and amber glazing set deep into the seating bay. On the court-to-market walk, Dante's flared exhaust crown rises above the lower galleries. Follow the existing front arch to Dante and the anvil. On the southern return, the rock shelf and irregular pine group give the eye a middle distance before the Forge and Archive.

## Where the geometry lives

| Place | Geometry and access | Production owner |
|---|---|---|
| Lofts, door (0,0.15,-34), yaw 180 | A 17.4 m deck bears above the two-storey room. The surveyed 4 x 4 m open-sky landing spans local X -6 to -2, Z 1 to 5 at local Y 9.16. It connects on foot to the sheltered seat. Access from below is by earned flight. The existing indoor stairs still serve the upstairs rooms. The raised window is glazing, with a stone sill, deep reveals and a back wall. | VenueView04C.StageScienceService > HeartPlanFR01.ScienceService > LoftsTerrace |
| Forge, door (75,0.15,100), yaw 0 | Offset pitches, an actual cut through the side gallery over the core, a collector, a lined square-to-octagonal throat and a flared crown at local Y 16.34. The centre of the bore is open from below. The existing condenser and front vents remain. A small amber lens on the neck responds to the open Forgewright station screen, including its existing debug entry. It dims on close. | Same service emitter > ForgeMonitor. ForgeScreen.DanteStationInUse exposes the station's existing read-only state to HeartPresentationFR01.ScienceEmission. |
| Shardwood return, (-76,30) looking into the settlement | Two grounded stone shelves near (-48,47) and (-40,51), uneven pine heights and non-solid boughs. Each site uses the existing route, aperture, parcel and receipt protections. The main path keeps its original grade. | CityBuilder.StageScienceCourt > ScienceCourt and ScienceApproach > ScienceCircuit > CircuitReturnRidge |
| Preview and physical switching | Each stage supplies the retained source and the candidate's actual solid triangles to HeartPresentationFR01.StageScienceDistrict. Only leaf geometry is excluded. The new glass, seat, rails, piers, roof edges and lined crown participate in the existing triangle-difference occupancy guard. | HeartPlanFR01.ScienceClearance.cs and HeartPresentationFR01.ScienceDistrict.cs |

Editable source is in Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPlanFR01.ScienceLandmarks.cs and HeartPlanFR01.ScienceCircuit.cs. The service call site is HeartPlanFR01.ScienceDistrict.cs. Stone, slate, root timber, iron, basalt, planting and contained crystal reuse the existing H2/H3/H5 material sources. Planar two-metre UVs span the roof panels, soffits and exposed fascia. The amber glass and service lens use renderer-local overrides with the existing crystal material. The lighting heartbeat and earned vision owner remain authoritative. Existing material assets, GUIDs, traffic, Malik, interactions, quest data and saves were preserved.

## Matched evidence

[Home and workshop](lofts_forge_landmarks.png) | [Court and return](lofts_forge_sightlines.png) | [Flat silhouettes](lofts_forge_silhouettes.png) | [Supplemental upper views](lofts_forge_upper_surfaces.png)

World metres, Y up. The look point fixes the direction together with the eye. Ground cameras are 1.67 m above the civic path. The baseline uses the actual bb700625 compiled mesh. Both sides include the same source-expanded Lofts, Forge and Archive rooms. The silhouette camera isolates the switched shell in venue-local coordinates.

| Camera | Eye XYZ | Look XYZ | Before |
|---|---|---|---|
| Home approach | -6, 1.82, -14 | 0, 6, -41 | Main continuous vault above the lower screened bays |
| Court toward Forge | 10, 1.82, 61 | 75, 7.5, 106 | Foreground gallery arch overlaps the low workshop roof |
| Forge market junction | 74, 1.82, 78 | 76, 7, 105 | Barrel roof blends into two vaulted side aisles |
| Return toward settlement | -76, 1.82, 30 | -5, 6, 66 | Existing crystal, planting bed, roofs and a flat interval between them |
| Isolated silhouettes | -14, 1.67, -35 | 0, 7, 10 | Same local perspective and scale for both buildings and revisions |

The software renderer samples actual geometry, UVs and albedo under approximate directional daylight with glow off. Retained room material roles and terrain are approximate. It omits other native batches, normal-map lighting, cast shadows, characters, fog volumes and the Game View camera. The upper views use illustrative flight positions. The source ray report checks first hits on the glazing, seat, crown, indicator, middle shelf and farther Archive roof. [Visibility measurements](SF06_EVIDENCE/visibility.json)

## Verification and reproduction

Use tools/check_unity_classic_wr02g.py with official 6000.3.16f1 managed references. Run tools/export_science_frontier.py --circuit against each compiled assembly. For the camera export add --landmark-rooms and use a separate output folder. Run tools/render_science_landmarks.py BEFORE_ROOMS AFTER_ROOMS OUT WORK and tools/check_science_landmark_views.py AFTER_ROOMS REPORT. The standard export goes to tools/check_science_circuit_walk.py. Run the district, frontier, grotto and circuit pure test harnesses on the compiled editor assembly. [Verification receipt](SF06_EVIDENCE/verification.json)

The Unity Editor executable and Blender were absent, DISPLAY was unset and the environment denied AF_UNIX socket creation with EPERM. Player and combined editor/test managed compilation used Unity's references. Live import, Game View, collision cooking, companion traversal, earned flight, materials, save/reload and runtime acceptance remain pending. ScienceCircuitNativeTests includes repeated preparation and occupied terrace/crown restoration cases for an actual Editor.

## One owner pass

In Unity 6000.3.16f1, run Acceptance > Verify Fantasy World, then PLAY FANTASY WORLD on a separate test bank. Leave home in daylight, turn back from home-court, and cross to Dante with a companion. Check the entrance, Dante and anvil, then walk the Archive, woodland and both grotto mouths back through Shardwood. Compare normal night and already-earned Third Eye. With earned flight, land on the open terrace and the Forge rim. Request End review while occupied, clear everyone from the changed surface, then re-enter twice and save/reload the test bank. Capture the same ground views if a roof floats, a landing fails or a service is blocked.

For each reported giant, aim directly at it and use F2 > Fantasy proof > Inspect structure at camera centre. Keep its Game View, renderer path and bounds, mesh, paired physical owner if reported, and actual collider hits. The family-9 seeds 809556, 32874 and 53496 and optional aster_observatory at seed 584924 remain unconfirmed. The latter is proposed at (75,28.15,115) by the optional roof planner. There is no matched owner capture or confirmed renderer/collider association for either report. If FR01/FR02A refuses, retain the first refusal or exception and build stamp. Neither defect has a reproduced fix.

## Resume and rollback

Start state bb7006256377ff22d414a6576648e6fd61291085. First published checkpoint e496c8d94a8a9c5ba69c7235f66e35776fa59373. The separate recovery snapshot contains all 181 local_delta files, SHA256 9ec36c5db92eeb99ef737de75d3a7d389875c81f66ba0d5229f6ef43d4b75e42. Resume from the actual latest remote HEAD. Keep PR #12 draft on feature/04o-shattered-metropolis-reborn.

Immediate rollback is End review after clearing occupied surfaces, or Stop Play. To undo this sprint on the branch, forward-revert its final evidence and refinement commit first, then e496c8d94a8a9c5ba69c7235f66e35776fa59373. Review any intervening edits before publishing. The two commits leave the earlier district circuit independently recoverable.

Next, resolve any demonstrated native defect from the combined pass. If the two landmarks read as intended, the southern straight of the existing Mineral service circuit is a candidate for a contained organic carriage pilot. Its RoadKind.Street edge runs (-150,-150) to (155,-150), 305 m long and 12.2 m wide on a level grade. Start in the middle, clear of the approaches at each end. Scope animal locomotion, gait, harness, turning and stopping separately with TrafficManager and TrafficDriver. Traffic, parked and mission vehicles are unchanged by this sprint.
