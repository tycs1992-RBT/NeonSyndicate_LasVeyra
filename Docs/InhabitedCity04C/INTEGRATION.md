# 04C implementation and preservation map

The active project is `/workspace/NeonSyndicate_LasVeyra`, branched from verified 04B commit `c49144150cfeb2f105f628f7f9d6ed85c1ccb218`. Unity remains pinned to `6000.3.16f1`. Native import, gameplay and performance acceptance have not run in this cloud environment.

## Existing owners and narrow additions

| Behavior | Existing owner | 04C integration |
| --- | --- | --- |
| Single city, player and boot | `GameRoot`, `Game`, `WorldSystems` | Prepare venue bindings before city construction; activate services and local life after existing systems. One optional `InhabitedCitySystem`. |
| City parcels and outdoor navigation | `CityLayout`, `CityBuilder` | Select eight existing parcels. Replace their solid lower building geometry at `BuildBuilding`, before the original solid collider is emitted. No road, POI or spawn relocation. |
| Existing fantasy appearance | `ArtRebirth04A`, `VisualRebirth04ABatch` | Reuse surface families and upper tier/crown geometry. Preserve the source building's height and roof, with a solid upper volume above the occupied floors. Only one-tier, noncritical parcels qualify. |
| Home, clinic, meal and vehicle transactions | `WorldServices` | Relocate the existing interaction only after its room is available. Keep original use callback, price, eligibility and conditions. Furniture proxies reuse the home callback. |
| Crafting and material ownership | `CraftingSystem`, `ForgeDb`, `MaterialStore` | Bind the actual Dante Forge to the interior station; preserve recipe, faction, combat and wanted checks. No second inventory or wallet. |
| Journal, contracts and payouts | `QuestBook`, JSON catalog and `QuestSteps` | Five authored Quiet Signal quests; a generic floor-aware `Venue` step verifies physical actions. Existing completion ledger and reward code remain authoritative. |
| Local discoveries and resume identity | `SaveData`, `WorldSystems.Capture/Restore` | Append `InhabitedCitySave`. Normalize known venue, action and record IDs. No scene IDs, bodies, coroutines or arbitrary interior coordinate serialization. |
| Optional inhabitants | `NpcController`, existing rig and health/crime path | Distinct assistants and visitors, finite shared population admission, floor-aware local movement and scoped cleanup. Canonical outdoor characters are not cloned. |
| Optional depot defense | `CreatureDirector` | One or two existing Glass Mites, tagged ownership, explicit opt-in, no reinforcements or kill payouts; peaceful alternative. |
| Input, map and interaction | `Interactable`, `UIKit`, `Gps`, `Blip`, existing menu | Physical hosts use the existing binding/controller path. PLACES tracks exterior entrances; active indoor objectives use room anchors. |
| Indoor presentation | `Atmosphere`, `AudioManager`, `PlayerCamera`, `InnerWorld` | Scoped shelter, local room sound and camera cap. Refuse player-initiated unsupported Veil shift indoors before spending energy. Outdoor powers and weather remain owned by their original systems. |

## In-place entry and collision lifetime

All destinations use real openings in the existing city, with a continuous floor finish 12mm above curb height to clear the original lot dressing. There is no interior teleport, additive scene, fade, door loading coroutine or second bootstrap. Walking across an open threshold changes the volume-derived indoor context; it does not transfer input ownership. Reversing direction, pausing or crossing twice cannot enqueue a transition.

Rooms are constructed under an inactive temporary root. Both model LODs, material sources, required anchors and shell dimensions resolve before the replacement becomes active. If any build step fails, the staged root is released and `CityBuilder` runs the original building path. That venue is reported unavailable; the original exterior service is retained. A dependent chapter cannot start against an unavailable room.

Eight modest shells, their floors, stairs and collision remain resident for the city's lifetime. This is an intentional bounded strategy, not a claim that interior geometry streams. Optional residents, sound and practical lights are limited by proximity and quality. Visual LOD never removes a floor or the only exit. Disabling the runtime component releases its own activities and people but keeps the physical rooms; next-boot feature switches regenerate the original city when disabled.

Principal openings are 2.8 m wide and 3.2 m high before the 12mm finish; the vehicle bay is 6.4 m wide. Source stairs use 24 treads, a 0.163 m first rise followed by 0.175 m rises and 0.32 m run, a 2.4 m clear walking width and a separate floor opening. These are authored dimensions and numerical checks, not proof that the native controller has walked them.

Original footprint records remain unchanged, so ordinary outdoor population and creature exclusions still recognize the building. Final pedestrian and police offsets also reject available venue footprints. Existing actors may physically enter through the door; entering does not clear pursuit, force peace, cancel missions, or stop outdoor timers. Indoor service use requires the correct floor, physical range and unobstructed line of sight. Flying players must land to use normal foot services; their flight upgrades are untouched.

## Save and interruption policy

Inside saves record the stable venue ID and resume at a checked exterior point at that same venue. The check rejects building interiors and roads, verifies a walkable ground height, and tests the return capsule against world, characters, vehicles and ragdolls. A deterministic small set of nearby candidates is tried. If none is safe, the existing save/home fallback remains; no unchecked point is forced. The same exterior resolution can operate with 04C disabled on the next boot.

This intentionally does not restore an exact stair tread, active creature or dialogue panel. Existing quest completion and money/material saves retain their authority. Finite physical action facts let a restarted incomplete chapter skip work already recorded without paying its reward twice. Ordinary archive reading is stored separately from active chapter action proof.

New-game/load clears only local transient channels and tagged encounter bodies, restores the normalized ledger, and retains permanent reading/service points. Return-to-title, disable and destruction clean owned actors and audio; complete destruction also unregisters UI pages and HUD delegates. Static mesh/material caches and QA state reset through `Game.ResetStatics`, including domain-reload-disabled entry.

## Actual neighborhood placement

The generated seed `61724` puts the canonical clinic and forge in different districts. Their identities were preserved instead of moving story POIs or inventing duplicate Mara/Dante services. The concentration at arrival is Veyra Lofts, Cinder Diner, the new archive in the same block and the observatory in the adjacent block. The clinic, forge, garage and depot extend that itinerary across the existing city.

Production-source pedestrian graph estimates are about 12 m from Free Story arrival to home, 83 m to the diner, 152 m to the observatory and 201 m to the archive. Home to the canonical clinic is about 644 m; clinic to the canonical forge about 1,574 m. These are graph/endpoint estimates with unchanged source POIs, not measured native walking paths. The compact home-clinic-forge circuit requested in the brief is therefore not achieved. PLACES and chapter GPS identify the real destination instead of masking the distance with fast travel.

The default new story still starts the existing Crystal Manor prologue. Free Story uses the original home arrival. Existing saves keep their established spawn policy except for the documented safe exterior resume when saved inside an 04C venue.

## Why the old city could look unchanged

Source inspection shows 04A's `Enabled`, `City`, `Gardens`, `Strongholds` and `Atmosphere` settings default on and persist in local PlayerPrefs. Geometry settings apply at world generation. `CityBuilder.Fantasy04A` checks resource readiness and emits `VISUAL04A_COVERAGE`, with explicit legacy fallback and intentional exclusions. A TERRITORIES menu alone does not establish any of those flags or rendered resources in the user's session.

The cloud cannot read the user's PlayerPrefs or Editor log. It therefore cannot conclude that the active Windows art path failed. The baseline audit did verify and repair malformed metadata and 56 serialized material shader references; see [BASELINE_AND_METADATA.md](BASELINE_AND_METADATA.md). Runtime 04A materials also have their own supported shader construction path, so the malformed reference is not presented as a reproduced explanation for the user's appearance report.

The detailed path also runs through `DistrictArchitecture.Contains`, selecting starter buildings and bounded city landmarks before the ordinary 04A fallback. Offline baseline illustrations use the ordinary geometry budget and omit those possible extra ornaments; they are not matched native 04B captures. Native coverage logs and the user's actual settings are still required to establish which path rendered their previous session.

04C changes the selected lower facades and navigable spaces directly. It does not depend on increasing bloom or renaming a version label. Successful replacements own their signs, preventing the old floating POI sign from overlapping their new entrance. Failed replacements retain the original signs and geometry.

## Native evidence boundary

The authored scene data, mesh payloads, source compilation comparisons, pure tests and Blender views can reveal source defects. They cannot establish Unity imports, collision behavior, actual room lighting, camera clearance, runtime LOD, resident grounding, completed gameplay, Windows builds or stable frame times. Follow [ACCEPTANCE.md](ACCEPTANCE.md) on a backed-up test save before treating this branch as playtested.
