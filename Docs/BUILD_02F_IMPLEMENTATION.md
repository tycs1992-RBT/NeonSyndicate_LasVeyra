# Build 02F implementation and asset inventory

## Implemented, not merely planned

The new runtime consists of six focused C# files. Two existing files receive small integration changes: one coroutine call in `CityBuilder.Build` and one owned-cache reset call in `DistrictArchitecture.ResetStatics`. The 02E facade, road, tree, vehicle and collision implementations are otherwise preserved byte-for-byte in those files.

| File | Responsibility |
| --- | --- |
| CrystalCity02FData.cs | Serializable art composition schema, validated model/material IDs and transforms |
| CrystalCity02FPlanner.cs | Deterministic roof selection, top-tier footprint reconstruction, scale limits and garden clearance arithmetic |
| CrystalCity02FAssets.cs | Read-only imported model cache and owned material creation using the existing pipeline-aware material helpers |
| CrystalCity02FComposer.cs | Material-batched near/far output meshes, grouped into LODs, with small separate rotating ring groups |
| CrystalCity02FMotion.cs | One world controller for five authored ring motions, at most two active nearby lights and full-map marker ownership |
| CrystalCity02FWorld.cs | Automatic optional build sequence, bounded placements, exception isolation and diagnostic counts |

## Real asset files

Fourteen original model designs ship in OBJ form with separate lower-detail versions, for 28 model files total. All have explicit UV coordinates, normals and triangulated geometry. Their importer settings disable automatic colliders, cameras, lights and animation and retain CPU mesh readability for combination. The models are not a dependency on a purchased asset pack.

Models: island, terrace, pavilion, astrolabe, crescent, crystal fan, branching trunk, leaf canopy, hanging vine, luminous cap plants, rune disc, light seed, banner and fountain arc.

Four 512-pixel surface sets ship as PNG files: moonstone, verdant stone, gilt and leaf. Each set includes an albedo, tangent-space normal, metallic/occlusion/smoothness mask and emission map. Normal and mask maps use linear import; color maps use color import and mipmaps. The old crystal shader/material path remains in use for crystal elements. No new shader or rendering pipeline is installed.

`Resources/CrystalCity02F/city02f_blueprints.json` contains nine reusable compositions. The three main landmarks, arrival crest, lantern pair, planter detail, fountain halo, plaza inlay and moonwell glade can be edited independently from the city generator. Runtime geometry does not depend on manually placed scene prefabs.

## Placement and safety

Large art attaches above ordinary roofs, never to helipads, antenna roofs or custom-built special lots. The top roof footprint is reconstructed using a separate GRand seeded identically to the original tier generator. The game generator's random state and layout objects are not modified. A two-sided footprint margin is reserved. Baseline vertical clearance is 12 metres; hosts already using tall detailed crowns receive an additional 22 metres of separation. This is deliberately a floating architectural treatment.

Arrival crests sit above the existing home/clinic interaction points. Lantern crystals use the old lamp-head positions. Planter detail is scaled into the existing planted box. Fountain arcs are thin static decorative ribbons above existing basins, not a fluid simulation. Park pockets reject known nearby POIs, pickups, parking and walk segments. These clearance checks are based on generated layout data, not a full physics query of every possible future encounter.

## Performance bounds

The large landmark count is at most three on all graphics settings, with reduced geometry on Low. Authored compositions contain five animated ring groups, below the controller's hard cap of eight. Low leaves those rings stationary. Otherwise nearby rings use the existing scaled game time, so pauses and slow motion are respected.

Extra street lantern pairs are capped at 12/24/42/60 for Low/Medium/High/Ultra, with planters capped at half those counts. Fountains cap at six, plaza inlays at eight, and glades at 3/6/9/9. Spatial cells suppress duplicates. Small stationary details share 160-metre and palette batches rather than spawning a renderer for each plant.

Each group has detail and lower-detail meshes. Imported source meshes remain shared and readable. Runtime combined meshes release their CPU copies and are registered with the project's existing OwnedMeshes component. Imported models and textures are never destroyed by the new cache reset. Only owned materials are destroyed. No new per-leaf or per-street-object Update loops exist.

Lights are shadowless, range-limited, night-gated and chosen by distance, with no more than two active (one on Medium, none on Low). No global fog, skybox, weather, time, camera or postprocessing state is changed.

These are design bounds, not measured frame-rate guarantees. Source import, combination, shadow rendering and additional materials still cost memory and frame time. See the QA file for what was actually executed.

## Art ownership and future editing

All new mesh shapes, procedural surface maps and composition definitions were authored in this sprint. No third-party models, stock images, font files or commercial character designs are included. Existing game assets retain their existing provenance; this document is not an audit of their licensing history.

The model recipe is included as `tools/generate_crystal_city_02f_assets.py`. Regeneration intentionally overwrites this kit's generated files; preserve artist edits first. Optional inspection tooling requires Python plus the packages listed in its header, but the Unity game and installer do not require Python. Future artists can replace the OBJ designs while preserving pivots, units and .meta identifiers, or add a new model ID to the catalog.

## Not implemented

No new character or vehicle redesign, creature AI, boss fight, multiplayer, skill tree, voice acting, playable aerial platforms, portal traversal or storyline chapter. No automatic time-of-day override. Banner and vine meshes are static; only the authored celestial ring groups rotate. The previous approved game remains the gameplay foundation.

## Measured source geometry counts (not frame-rate measurements)

| Composition | Near triangles | Far triangles | Moving parts |
| --- | ---: | ---: | ---: |
| aster_observatory | 11,904 | 3,762 | 2 |
| dawn_atrium | 7,464 | 2,342 | 2 |
| moonroot_garden | 12,160 | 4,260 | 1 |

Counts come from the shipped OBJ files, before any native importer differences. Both detail meshes may be resident even though one is rendered. Actual frame time, memory and visibility still need Unity profiling.
