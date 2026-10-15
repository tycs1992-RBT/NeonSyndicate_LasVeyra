# Living Territories 04B art and traversable compositions

The 04B layer reuses the 04A material library, leaf geometry, modular architecture and shared material batching. It adds six original hero masters with high and low source variants, rather than duplicating the 47 existing masters. All six are authored in `tools/generate_living_territories_04b_art.py`; their editable OBJ and identical runtime CMH1 triangle payloads are shipped together under `Resources/LivingTerritories04B`. No external art or animation is imported.

| New master | Purpose and structural reading |
| --- | --- |
| resonance_armillary | Three grounded buttresses cradle overlapping astronomical rings around a visible central crystal. |
| rootwater_heart | Four spreading root feet support split branches and a woven mineral seed cup. |
| rift_extractor | Anchored extraction frame, paired flywheels, piston braces, suspended drill and accessible service side. |
| canopy_shelter | Four root posts, braided rafters and layered leaf roof; usable ground or platform shelter. |
| crystal_conduit | A six metre supported service pipe with three flange mounts, inspection port and control handle. |
| study_terminal | An accessible writing desk with sculpted legs, drawers and a sloping lectern. |

The source library totals 14,322 triangles at high detail and 5,158 at low detail. These are library totals, not a whole-scene triangle or performance claim. The 04A albedo, normal, metallic/occlusion/smoothness and emission maps remain shared across the active pipeline's native Lit materials. Fourteen material roles remain shared and are never duplicated for each prop.

Seven JSON compositions define the actual native and offline positions, scales, rotations, material roles and intended solids:

- `relay_amethyst`: Resonance Court, sheltered studies, instrument centerpiece and observatory.
- `relay_verdant`: Rootwater Heart, broad leaf layers, root arches and planted approaches.
- `relay_obsidian`: Shatterworks Extractor, grounded machinery, damaged bastion walls, ore and pipework.
- `habitat_sanctuary`: neutral observation court, sheltered study point, foliage at several depths and an open large-creature rear space.
- `habitat_canopy`: an elevated sheltered observation deck and a distinct aerial ring.
- `habitat_rift`: fractured basalt pockets, extraction equipment and a hazardous nesting arch.
- `habitat_transition`: planted roadside-free verge, weathered rocks and clear roaming space.

Each relay family includes three stable secondary silhouettes in bounded side pockets, selected by the site variant. Four ground-supported stone or soil quadrants, layered planting and Amethyst vaulted conversations turn the isolated kit into composed places. The compact `Props/home_ledger.json` supplies a physical study terminal and a finite nine-crystal ownership display for Hearthspire; `TerritoryHome04BDisplay.SetProgress` updates it from the existing territory ledger.

Each family uses a separately surveyed 48 metre plot. The full west/east approach remains nine metres wide, and a nine metre southern approach reaches the central interaction space. Cover and vegetation stay in side pockets. A fly-to overlook has a genuine mesh collision deck at five metres, four grounded columns, three rail edges and an open arrival side. It is explicitly a flying destination, not an advertised stair route. The original Hearthspire western stairs and six construction sockets are not edited by these art files.

Collision uses the exact low source mesh of each `solid` node. Matching solid geometry remains visible on Low and at the far LOD. Thin cross paving follows the surveyed ground and adds no raised collider. Decorative crystals, cloth, leaf canopies and sky rings have no collision. Fortification groups add visible side-pocket cover at tier one and reinforced braces and supported service conduits at tier two. Three extra crystals illuminate the owned pylon state. Hub-only resource glyphs sit at local z4.5; their bright layer extinguishes after the saved finite-cache claim, leaving a worked metal rune. Relay attunement happens beside the visible pylon, at local z8. None closes an approach or creates an invisible wall. Before purchasing a fortification, `CanInstallTier` transforms the inactive low source collider bounds and checks character, vehicle and ragdoll occupancy; it does not count ground or enable future collision during the check.

`ArtTerritories04B.Build` creates an inactive root and activates it only after all sources validate and all material batches finish. A failure destroys only its unfinished root. `ArtTerritories04B.ResetStatics` releases the six-master source cache; final combined meshes belong to the existing `OwnedMeshes` lifecycle. The additive `VisualRebirth04ABatch.PartMeshes` method lets the new kit use the same bounded batching and LOD logic as 04A without altering its source assets.

Native import, collider traversal, LOD appearance and runtime character clearance remain NOT RUN without Unity. Offline source tests and labeled Blender previews are supporting evidence, not native acceptance. Actual placement can skip any unsafe plot. Refer to the QA report for evidence, exact counts and unresolved native checks.

`Validation/art_provenance.json` records the GUID, SHA256 and byte count for all 33 authored source assets. Regeneration compared 66 payload and metadata files and reproduced all bytes exactly. Native Unity import remains NOT RUN.
