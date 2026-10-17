# 04E venues and the arrival scene

Work stream VENUES of Underheart and Bonds 04E. This page records the four new walk-in venues (Cinder Dispatch, The
Pressure House, Bondkeeper's Refuge, The Resonance Office), the parcels they actually bind on the default city (seed
61724), every anchor other streams address, the Pressure House stairwell, and the arrival scene around the ordinary Veyra
Lofts arrival.

Status labels used below: **source implemented** (code and data exist), **offline tested** (the pure parts ran in the
offline EditMode suite, see Tests), **native NOT RUN** (nothing here has been seen or played in the Unity editor; no
claim is made about how it looks, feels or performs).

## 1. Parcels on the default city (seed 61724)

Chosen with an offline harness that runs the real `CityLayout(61724).Generate()` and `InhabitedCityBindings04C.Select`
over the shipped `venues.json` (the same approach as the 04C validation). The eight 04C venues bind first and are
untouched; the four 04E entries are appended in `VenueIds04E.All` order.

The arrival is (-6.00, 0.15, -65.73), facing east (yaw 90), beside the Veyra Lofts door at (-13.85, 0.15, -65.73).
Veyra Avenue's roadway is x -7.5 to 7.5. "Walk" is the shortest route over the city's pedestrian graph plus a straight
crossing of the avenue (the player can cross directly); "straight" is the flat distance from the arrival to the door.

| Venue (id) | Existing building | Door (facade threshold) | Faces | Room yaw | Straight | Walk | Why this parcel |
|---|---|---|---|---|---|---|---|
| Cinder Dispatch (`dispatch`) | seed 855223, block 100, 86.9 m office tower, roof antenna | (14.12, 0.15, -66.18) | west, onto Veyra Avenue | 90 | 20.1 m | 28 m | The only building directly across the avenue: its door is within 1.3 degrees of straight ahead from the arrival. |
| The Pressure House (`pressure`) | seed 434831, block 100, 63.7 m apartment block | (13.70, 0.15, -30.03) | west, onto Veyra Avenue | 90 | 40.8 m | 43 m | Same block and same sidewalk as the dispatch, 36 m north, so the warm conduit runs along one forecourt without crossing a road. |
| Bondkeeper's Refuge (`bondkeeper`) | seed 703831, block 86, 76.0 m office block | (-36.67, 0.15, 13.80) | south, onto Core Avenue | 0 | 85.2 m | 91 m | Directly across Core Avenue from Cinder Diner (28 m) and 47 m from the observatory door: it concentrates early content around home, diner and observatory. |
| The Resonance Office (`office`) | seed 472154, block 69, 33.7 m office block | (-209.19, 0.15, -14.45) | north, onto Core Avenue | 180 | 209.6 m | 257 m | Two doors (56 m) from the Resonance Archive on the same Core Avenue sidewalk: a records row for the Act III ledger investigation, a meaningful walk or ride from home. |

Distances between doors: dispatch to Lofts 28 m, dispatch to Pressure House 36 m, refuge to diner 28 m, refuge to
observatory 47 m, office to archive 56 m, office to observatory 128 m. The 04C anchors (`clinic` 598 m, `forge` 729 m,
`garage` 861 m) are not moved or duplicated.

Building footprints, room frames (16 m wide by 18 m deep inside each original footprint) and doorsteps:

| Venue | Footprint (x by z) | Original width x depth along the door | Doorstep (`Outside`) |
|---|---|---|---|
| dispatch | 14.1..53.1 by -81.6..-50.8 | 30.8 x 39.0 | (11.62, 0.15, -66.18) |
| pressure | 13.7..53.5 by -46.5..-13.5 | 33.0 x 39.8 | (11.20, 0.15, -30.03) |
| bondkeeper | -59.3..-14.1 by 13.8..64.0 | 45.2 x 50.2 | (-36.67, 0.15, 11.30) |
| office | -236.4..-182.0 by -46.5..-14.5 | 54.4 x 32.3 | (-209.19, 0.15, -11.95) |

### Selection rules (InhabitedCityBindings04C)

- The eight 04C rules and their eligibility are unchanged (`Eligible`, `ValidDefinition` for the eight, `Door`).
- 04E entries read `OffsetX/OffsetZ` and `FacingX/FacingZ` in the **Veyra Lofts frame** (x to the right of the Lofts door
  looking out, z straight out of it), so in any city the dispatch is placed facing the arrival. `FramePoint04E`,
  `FrameDirection04E` (snapped to a world axis because blocks are axis aligned).
- 04E doors must open onto the block's public sidewalk ring within 4.5 m (`DoorOnSidewalk04E`), never an alley, and stay
  6 m from any point-of-interest door (`NearPoiDoor04E`).
- They never take a point-of-interest lot, a `CustomBuilt` lot or a building an earlier venue took (the eight bind first).
- One exception: **Cinder Dispatch may take a one-tier building with a roof antenna** (`EligibleFor`), because the
  arrival scene rebuilds that roof mast as the dispatch's crystal receiver crown, so the silhouette is kept rather than
  flattened. Helipads and tiered towers stay protected for every venue. Without this exception no building faces the
  arrival across Veyra Avenue.
- `PreferredBuildingSeed` pins the default city's choice; other cities fall back to the nearest eligible facade within
  `MaxDistance` of the frame point. Seeds 1, 2016, 77231 and 92017 either bind a sidewalk-facing parcel or skip with an
  explicit `Skipped:` status (the original building and city stay).

## 2. The rooms (one storey each)

Layouts are data: `Resources/InhabitedCity04C/Layouts/{dispatch,pressure,bondkeeper,office}.json` (04C layout format,
expanded by the shipped `InhabitedCity04CComposition`). Every room has the 04C shell (open 2.8 m doorway with the carved
frame, sconces, two pendant lights, name board, wainscot, beams), furniture from the shipped 04C, 04A and 04B meshes,
2 to 5 practical lights (the venue view keeps at most the nearest three on), physical signs, the 04C required anchors
(`service`, `activity`, `staff`, `visitor1`, `visitor2`, `exit`, plus `_next` resident routes) and every anchor in
`VenueIds04E.ExtraAnchors`. `service` is where the named staff member stands (guidance spawns the cast there), facing the
door with clear floor in front.

Local frame: x right (-8..8, wall faces at +-7.7), z into the room (front wall 0 to 0.3, back wall face 17.7), y up from
the floor finish (0.012). World positions are for seed 61724.

### Cinder Dispatch (style 3: basalt, iron, ember crystal)
Dispatcher desk with console and ledger along the left wall and the dispatch board behind it; a referrals board by the
waiting bench ("CLINIC: MARA, SUNKEN ROW / FORGE: DANTE, OLD SILVER / CUSTOMS: JAX, ASH UNDERPASS"), so the dispatch
coordinates the real services rather than copying them; receiver bench A (a working crystal
receiver) and receiver bench B (a tilted, cloth-covered faulty one) on the right wall; Ion's call screen with console and
speakers on the back wall; the crystal-repair fitting worktable with tool rack, jig, vise and task light (back right);
records shelf and a crate of spare cores (back left). Exterior: a warm signal strip under the name board and a cable run
toward the frontage mast.

| Anchor | Local | World |
|---|---|---|
| service (Corin Pell) | (-3.50, 0.01, 7.40) | (21.52, 0.16, -62.68) |
| receiver_a | (5.45, 0.01, 4.40) | (18.52, 0.16, -71.63) |
| receiver_b | (5.45, 0.01, 9.40) | (23.52, 0.16, -71.63) |
| fitting | (4.40, 0.01, 13.90) | (28.02, 0.16, -70.58) |
| ion_screen | (-1.60, 0.01, 15.30) | (29.42, 0.16, -64.58) |

### The Pressure House (style 3)
Maintenance desk with log and screen near the door (left); the fuse cabinet on the left wall (doors, indicator lamps,
warning plate, cable trunks, a scorch mark); a tool rack; the nest corner (split crate, bedding, a ring of stones, warm
ember shards, the cracked glowing wall vent); the vent line on the back wall (two risers, header, hand wheel valve,
gauge, floor drain); a pressure manifold on stands beside the stair whose pipe drops into the shaft; spare crates behind
the stair. Exterior: a pipe header with glowing joints and a downpipe over the right window, a louvred vent on the left.

| Anchor | Local | World |
|---|---|---|
| service (Tamsin Reyes) | (-4.60, 0.01, 5.20) | (18.90, 0.16, -25.43) |
| fuse_cabinet | (-6.00, 0.01, 9.60) | (23.30, 0.16, -24.03) |
| nest | (-4.40, 0.01, 14.30) | (28.00, 0.16, -25.63) |
| vent_valve | (0.60, 0.01, 15.80) | (29.50, 0.16, -30.63) |
| stair_top | (5.70, 0.01, 2.30) | (16.00, 0.16, -35.73) |

### Bondkeeper's Refuge (style 2: limestone, patina, teal crystal)
The caretaker's desk near the door; the treatment bench (a cot with blanket, overhead lamp, supply shelf with jars, a
warming crystal and a basin) used as the care station; a planted habitat enclosure (timber fence with a gated opening,
soil, ferns, reeds, a stone, a basin, a log, a warm crystal, a vine); the indoor training yard (three mats, two low
hurdles, a target post, a hoop on a stand); a perch (post, arm, ring perch, crystal finial, feed tray); a visitor bench
and a window planter. Exterior: flowering entry vessels and an awning. It is a care room, not a cage or a bestiary.

| Anchor | Local | World |
|---|---|---|
| service (Odile Marsh) | (-3.90, 0.01, 4.40) | (-40.57, 0.16, 18.20) |
| care_station | (3.95, 0.01, 9.00) | (-32.72, 0.16, 22.80) |
| habitat | (4.10, 0.01, 12.90) | (-32.57, 0.16, 26.70) |
| training | (-4.60, 0.01, 9.60) | (-41.27, 0.16, 23.40) |
| perch | (-5.30, 0.01, 8.40) | (-41.97, 0.16, 22.20) |

### The Resonance Office (style 1: warm limestone, copper, gilt)
The clerk counter with a gate, ledgers, a bell, a chair and a hanging CLERK sign (right front); the records wall (three
record cases with a rolling ladder, left); the public terminal (the 04B study terminal, right); a full partition with a
1.8 m doorway into the back records room (desk, chair, files, a filing case, a sealed records crate, its own lamp);
faction banners and seal on the partition; the testimony bench and lectern (left front). Exterior: civic columns, a
header and a seal plaque. The back room is a real room behind a real doorway, on the same floor.

| Anchor | Local | World |
|---|---|---|
| service (Ansel Whit) | (2.40, 0.01, 5.30) | (-211.59, 0.16, -19.75) |
| records | (-5.75, 0.01, 9.80) | (-203.44, 0.16, -24.25) |
| terminal | (4.15, 0.01, 11.60) | (-213.34, 0.16, -26.05) |
| back_room | (4.50, 0.01, 16.00) | (-213.69, 0.16, -30.45) |
| testimony | (-4.40, 0.01, 3.55) | (-204.79, 0.16, -18.00) |

### Jax Calder Customs: the `bay` anchor
Appended (only change to `garage.json`): `bay` at local (0, 0.012, 7.0), yaw 0, the centre of the existing vehicle bay;
world (-632.59, 0.16, -648.00). The fusion stream reads it for `TryBay`.

## 3. The Pressure House stairwell

The opening is exactly `VenueIds04E.StairX0..StairX1` by `StairZ0..StairZ1` (local x 4.3 to 7.1, z 3.0 to 14.6), on the
right side of the room. The composition replaces the room's single floor slab with four slabs around it (same 12 mm
finish, top at 0.012) so no floor spans the shaft; the slab area is the room minus exactly the opening. Guard rails
(top rail at 1.06 m, mid rail, toe board, posts every 1.45 m) stand on both long sides (x 4.27 and 7.13) and across the
back (z 14.63); the front end toward the door is open, marked by a flush gold edge line. Nothing solid and no anchor
except `stair_top` stands in the approach (x 4.3 to 7.1, z 1.2 to 3.0); the composition refuses a Pressure House source
that puts a solid box or a resident anchor there.

World (seed 61724): opening x 16.70 to 28.30, z -37.13 to -34.33; top tread start (16.90, 0.15, -35.73); landing at the
bottom (27.90, -6.00, -35.73). The Underheart stream registers the aperture from this binding, cuts the ground under it,
and builds every tread, the shaft walls and the service level below the venue floor (not this stream).

Ambient sparks: short spark bursts at the fuse cabinet, the vent valve and the nest vent (through the shared `Fx.Sparks`
pool) with a soft heat tick, only while the player is within 32 m. They run until `uh05_02_sparks` is complete, or until
the campaign calls `ArrivalScene04E.SetPressureSparks(false)` when the vent is diverted (`true` forces them on). Inside the
Pressure House the audio zone becomes `ServiceInterior` (only from `Surface`; the Underheart owns zones underground).

## 4. The arrival scene (ArrivalScene04E)

Placement is planned by the engine-free `ArrivalPlan04E` in each venue's frame, then checked again at build time against
the ground (level forecourt), colliders (World, Debris, Vehicles) and interaction points. The plan checks every ground
piece against roads and intersections (0.5 m), building footprints, the lot line (pieces stay on the forecourt, off the
public sidewalk), every venue door zone, its own doorway strip, points of interest (2 m), lot entrances, pedestrian
links (0.9 m), street props, lamps and trees, parking spots, pickups and collectibles, the safehouse bike spots, special
sites and the resonance case's first preferred spot.

| Piece | World position | What it is |
|---|---|---|
| Receiver mast | (12.82, 0.15, -72.88), 20.1 m from the arrival, 21 degrees right of straight ahead | 17.4 m tapering lattice on a basalt plinth, braced to the dispatch facade, ring of six crystal prisms around an emitter, street dish, aviation lamp, plinth plaque "CINDER DISPATCH / RECEIVER ONLINE". Slow pulse (3.4 s; 1.7 s and brighter while the conduit is lit). Receiver hum loop within 45 m, a ping every 14 s. |
| Core crates | (12.72, 0.15, -70.23) | Strapped crates of receiver cores on a pallet, one open with a glowing core (0.55 m clear of the facade, beyond the window sill). |
| Walk-in sign | (12.72, 0.15, -62.68) | Two-sided sign "DISPATCH DESK / WALK IN" with a lamp strip. |
| Beacon | (12.87, 0.15, -57.58) | Post with a slowly turning amber lens. |
| Roof receiver crown | (33.60, 87.06, -66.18) | Replaces the tower's roof antenna: basalt housing, 11 m lattice mast, ring of eight crystal prisms with the same pulse, aviation lamp. Reads on the skyline. |
| Warm conduit | riser (13.84, -59.23), run along x 11.92 to (11.92, -32.03), riser (13.42, -32.03) | Riser out of the dispatch wall with a hand wheel and gauge, a low pipe on saddles along the forecourt 0.42 m inside the lot line, glowing joint collars every 2.4 m in three chase groups (ember glow; a chase toward the Pressure House when `SetConduitLit(true)`), riser into the Pressure House door jamb with a glowing collar. Low steppable colliders (0.32 m). Never on the road, never across a doorway (it stops 2.0 m beside the Pressure House door and 6.95 m beside the dispatch door). |
| Lofts lanterns | (-13.43, 2.50, -67.83) and (-13.43, 2.50, -63.63) | Wall lanterns either side of the open Lofts doorway, night-scaled warm lamps. |
| Lofts runner | (-12.55, 0.15, -65.73) | Flush welcome runner in front of the doorway (no collision). |
| Nessa's dispatch board | (-12.50, 0.15, -70.03) | Notice board with pinned slips: "CINDER DISPATCH / NEIGHBORHOOD WORK / ASK NESSA VALE". |
| Lofts planter | (-12.65, 0.15, -72.03) | Planter with greenery. |
| Companion perch | (-32.07, 0.15, 12.50) | Outside Bondkeeper's Refuge: post, branch, ring perch with a soft teal finial, a water trough and a plaque "COMPANION PERCH / CARE INSIDE". |

`ConduitPath` (the walkable route beside the pipe, for guidance): (11.62, -66.18) dispatch doorstep, (10.72, -59.23),
(10.72, -32.03), (11.20, -30.03) Pressure House doorstep, all at y 0.15.

Nessa's post: `ArrivalScene04E.TryNessaPost` returns (-12.70, 0.15, -68.78), yaw 90 (facing the arrival), on the Lofts
forecourt between her board and the doorway, 3 m south of the door axis. The resonance case's first preferred spot,
(-11.82, -61.84) on the north side of the door, is kept free: all Lofts ground dressing is on the south side.

Lights: two shadowless point lights near the player at most (dispatch frontage, Lofts entrance; the conduit end light
only while lit), one on Low. The nearest venue keeps its own three, so the arrival runs at most five point lights.

## 5. Interfaces other streams use

- `ArrivalScene04E.Build(parent)`, `Built`, `ConduitPath`, `SetConduitLit(bool)` (frozen signatures), plus `ConduitLit`,
  `Plan`, `Status`, `SetPressureSparks(bool)`, `ClearPressureSparksOverride()`, `PressureSparksActive`,
  `TryNessaPost(out pos, out yaw)`, `BlocksNewVenueDoor(point, radius)`, `PlacePurpose(id)`.
- A session reset (new game or load, through `IWorldSystem`) dims the conduit and returns the sparks to automatic.
- `InhabitedCityBindings04C.LocalToWorld`, `WorldToLocal`, `VenueAxes`, `InDoorZone04E` (pure).
- `InhabitedCity04CComposition.StairApproachZ04E`, `StairRailHeight04E`.
- ModuleStatus rows: `venues_04e` ("New venues (04E)", count "of 4 new venues built", each skipped venue with its
  reason) and `arrival_04e` ("Arrival neighborhood", count of set pieces built, each skipped piece with its reason).
  Note that GameRoot's existing `inhabited_04c` row now counts twelve venues.

## 6. Residents

- Assistants (the 04C host slot) for the four kinds: "Dispatch runner" (orange jacket and cap), "Pressure House fitter"
  (grey workwear, beanie), "Refuge handler" (green jacket, braids), "Records assistant" (navy suit, glasses). They are
  never the named cast; guidance spawns Corin Pell, Tamsin Reyes, Odile Marsh and Ansel Whit at the `service` anchors.
- Room tones: four new four-second periodic tones (receiver hum, low pressure drone, warm hum, quiet civic hum); the
  eight 04C tones are unchanged (`VenueRoomTones04E.For`).
- A body whose slot becomes reserved through `ResidentReservations04E` (for example Nessa taking the Lofts host slot)
  now leaves; previously only new spawns were refused.
- Residents never stand within 1.3 m of a venue's 04E interaction anchors (receivers, fitting, stair top, care station,
  and so on), in addition to the 04C exit, service and activity anchors.

## 7. Hooks outside this stream's files

- `Gameplay/CrystalArsenal/CrystalArsenal.Case.cs`: the case spot check and the practice lane footprint refuse a spot in
  front of a 04E venue door (`BlocksNewVenueDoor`). Two lines.
- `Gameplay/CrystalArsenal/CrystalEncounters.Site.cs`: the breach centre (with its 12 m open radius) and its anchors
  refuse a 04E door zone. Two lines. On seed 61724 none of the four breach candidates touches a new door.
- `Gameplay/InhabitedCity/InhabitedCitySystem.UI.cs`: the PLACES page describes the four new venues
  (`ArrivalScene04E.PlacePurpose`). One line.
- Tests: `InhabitedCitySave04CTests.cs` (the native catalog test now expects twelve entries) and
  `InhabitedCityGeometry04CTests.cs` (the native build test covers the four new rooms, plus a native stairwell test).

## 8. Honest gaps

- Nothing here has run in Unity. The look of the rooms, the mast, the crown and the conduit, the readability of the
  signs (TextMesh sizes were estimated from the 04C signs), the lighting and the spark effect are unverified.
- The stair treads, shaft and service level are the Underheart stream's; until that work is merged the Pressure House
  floor has an open, railed opening over the original ground (the ground slab is only cut by the Underheart aperture
  code). With the foundation alone the opening shows the existing lot surface about a centimetre below the floor finish,
  so nobody can fall; the descent only exists once the Underheart stream's treads and aperture are merged.
- The dispatch takes an antenna-roof tower; its original antenna is replaced by the receiver crown (by design). If the
  crown is skipped at runtime (roof not found), the tower shows no antenna.
- The Resonance Office's back room is on the same floor (no upper gallery). The refuge's "training yard" is an indoor
  training floor.
- Other seeds: the four venues bind only where a sidewalk-facing parcel exists near the frame point; otherwise they skip
  with a reason, and the arrival pieces and conduit skip with reasons too (the conduit needs the Pressure House on the
  dispatch's frontage line).
- Walking distances are measured on the pedestrian graph plus a direct crossing; the actual route a player takes may be
  longer.

## 9. What a native Unity session must check

1. Start a new Free Roam session. From the arrival, Cinder Dispatch's frontage should be straight ahead across Veyra
   Avenue with the receiver mast to the right of its door, pulsing slowly; the basalt tower should keep a crown at the
   top. Check the mast's top is not clipped awkwardly by the default camera and that nothing floats.
2. Walk into each of the four rooms and back out through the doorway. Check floors, furniture collision, the signs'
   sizes and legibility, the practical lights, residents standing where expected (not on interaction points), and the
   room tone.
3. In the Pressure House: the railed opening is where the Underheart stair begins; walk the approach (between the door
   and the stair) and confirm nothing blocks it; confirm the rails stop you on both long sides and the back; watch the
   sparks near the fuse cabinet and the vent; listen for the muffled zone inside.
4. Follow the conduit from the dispatch to the Pressure House door; step over it; call `ArrivalScene04E.SetConduitLit(true)`
   (or play `uh05_01_knock`) and check the joints chase and brighten.
5. Check the Lofts entrance: lanterns lit at night, the runner flush, Nessa's board legible, the resonance case still
   placing beside the door (north side), the safehouse bike still spawning.
6. STATUS page: `venues_04e` shows 4 of 4 and `arrival_04e` its count with no skipped pieces.
7. Run the native EditMode tests: `InhabitedCityGeometry04CTests` (all twelve rooms build with an open door, and the
   Pressure House stairwell test) and `InhabitedCitySave04CTests`.
8. Golden screenshots before and after (the 04E arrival changes the default views near the Lofts): same resolution and
   preset, then Compare Golden Shots.

## 10. Tests

Offline (run from the sandbox root: `/home/claude/tools/check_tests.sh <sandbox> <out>`):
`Venues04ETests` (bindings with distances, sidewalk and point-of-interest rules, other seeds, the catalog, the antenna
exception, the Lofts frame, door zones, all four layouts' anchors inside their rooms and clear of furniture, resident
routes, furniture inside the shell and out of the doorway, the stairwell at the constants with rails and a clear approach,
refusal of a blocked stairwell or a missing anchor, the eight originals' geometry fingerprints, the arrival plan and the
conduit, room tones and costumes) and `InhabitedCityBindings04CTests` (twelve bindings, the eight identical with and
without the appended entries on five seeds). Native only: `InhabitedCityGeometry04CTests`, `InhabitedCitySave04CTests`.
