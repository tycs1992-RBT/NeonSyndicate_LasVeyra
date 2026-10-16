# 04C venue inventory

All eight stable venue IDs have authored layouts and integrated functions. The following bindings come from the executed production CityLayout/selection source for seed 61724. Coordinates are rounded world `(x, y, z)` at the original facade threshold; the interior floor finish is 12mm higher. They are source bindings, not native placement observations. Runtime PLACES and `INHABITED_CITY_04C` report actual available/loaded/skipped counts and reasons.

| Stable ID / title | Existing identity / source building seed | Threshold `(x, y, z)` | Actual authored space |
| --- | --- | --- | --- |
| `lofts`, Veyra Lofts | Canonical Veyra Lofts / 970617 | `(-13.852, 0.150, -65.727)` | Two playable floors: communal lobby and furnished personal apartment with a partitioned sleeping area. |
| `clinic`, Mara Solane Street Clinic | Canonical clinic / 131595 | `(-27.848, 0.150, -662.922)` | One open floor, defined reception/waiting, screened treatment and supply zones. |
| `forge`, Dante Rook's Crystal Forge | Canonical Dante Rook / 584924 | `(-643.013, 0.150, 288.407)` | One workroom with hearth, real Forge host, material storage and repair bench. |
| `inn`, Cinder Diner | Canonical Cinder Diner / 382485 | `(-34.600, 0.150, -14.467)` | One public floor with dining, counter, cooking/storage impression and courier corner. |
| `archive`, Resonance Archive | New civic use of a noncritical existing parcel / 479130 | `(-153.172, 0.150, -13.712)` | One reference floor with desk, shelves, three research stations and exhibit. No upper gallery is implemented. |
| `observatory`, Sundial Survey Observatory | New civic use of a noncritical existing parcel / 958118 | `(-84.050, 0.150, 13.941)` | Two playable floors: public survey desk and upstairs instrument room with supported balcony/open city view. |
| `garage`, Jax Calder Customs | Canonical Jax Calder Customs / 129420 | `(-639.587, 0.150, -648.003)` | One open vehicle bay with parts, engine stand, work lights and pedestrian enquiry position. |
| `depot`, Rift Freight Depot | Noncritical warehouse parcel / 173154 | `(-265.833, 0.150, -367.343)` | Two freight rooms separated by a partition, connected by central and alternate side passages. |

All entries are continuous walk-through spaces, including the 6.4m garage aperture. No entry uses teleportation or an additive scene. Ground-floor public openings are 2.8m wide; home/observatory stairs connect their two floors. These are source dimensions and source route checks; normal-play movement/vehicle fit remains unrun.

Home, diner, archive and observatory form the local concentration across three existing blocks. The archive's preferred candidate is rejected when protected/tiered; the deterministic safe fallback above is the actual selected default-seed parcel. Canonical clinic, forge and garage remain in their original districts. The compact home-clinic-forge target is not achieved; source graph estimates are documented in DELIVERY.md.

## Functions and population

| ID | Physical function | Optional resident / visitor ownership |
| --- | --- | --- |
| `lofts` | Original home services at lobby/bed/desk; chapter offers 1/2; receiver and earned display | One new Nessa Vale slot, then bounded visitors. Canonical story characters are not copied. |
| `clinic` | Existing treatment, supply manifest and finite inspection | Clinic care assistant and waiting visitors. |
| `forge` | Original full Forge, real recipe transaction or free chapter calibration; stage 3 offer | Workshop assistant and visitors; original Dante contact is preserved. |
| `inn` | Existing meal, independent useful directions and alternate chapter investigation | Diner service assistant and visitors, including courier conversation position. |
| `archive` | Three saved readable records and QuestBook recovery offer | Archive desk assistant and visitors. |
| `observatory` | Final contract offer downstairs; lens/tuner/focus activity upstairs | Survey attendant and visitors. |
| `garage` | Original vehicle-only repair/customization, free pedestrian instructions | Bay service assistant and visitors, outside the reserved driving volume. |
| `depot` | Explicit peaceful or finite combat recovery, then retrieval | No ambient civilian hosts. One Story/two higher-difficulty tagged Glass Mites only after explicit acceptance. |

Services and exits do not depend on successful body admission. People load near/inside venues using shared admission thresholds and owned caps; see RESIDENTS_AND_SHELTER.md. Runtime resident count zero while far away is not a placement failure.

## Availability and fallback

The source audit selects 8/8 candidates at seeds 61724, 1, 2016 and 92017. Seed 77231 selects 7/8: its canonical diner is protected/tiered and retains the original building/service. This is an explicit safe skip, not an eighth installed room. Other failed resources or unsuitable parcels similarly retain the original path, and required chapter offers explain unavailable dependencies. Live availability is **NOT VERIFIED** in this environment for any seed.

Inside saves remember the stable venue ID and resume at a checked exterior candidate for that venue, with the original home fallback if all candidates are blocked. Binding changes do not serialize raw object IDs or unvalidated interior positions. See INTEGRATION.md for the full contract.
