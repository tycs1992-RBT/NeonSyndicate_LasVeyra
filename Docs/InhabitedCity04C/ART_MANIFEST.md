# 04C art and asset manifest

Original project-owned authoring, metric Y-up geometry, no purchased/scraped models or external art service. `Resources/InhabitedCity04C/asset_manifest.json` records every mesh variant, editable source path, bounds, triangle count and runtime payload hash. `venues.json` supplies stable bindings; `Layouts/*.json` supplies furnishings, anchors, lights and physical signs. The production `InhabitedCity04CComposition.Expand` supplies shared structural geometry.

## Editable source and runtime consumption

| Asset family | Editable/generated source | Runtime use |
| --- | --- | --- |
| 16 model masters | `Resources/InhabitedCity04C/Models/*_high.obj` and `*_low.obj`; deterministic Python generator | `Meshes/*.bytes`, CMH1 position/normal/UV data decoded by the existing fallback decoder. Runtime does not depend on OBJ import success. |
| 8 destination assemblies | `Layouts/lofts,clinic,forge,inn,archive,observatory,garage,depot.json` | Real rooms built transactionally at CityBuilder's original building emission point. |
| Timber and limewash | Original `Textures/Timber_Albedo.png`, `Plaster_Albedo.png`; source function in generator | Shared cached material variants on the active existing pipeline's Lit shader. Clinic finish clears inherited masonry normal/metal/occlusion masks. |
| Inherited 04A modules/surfaces | Explicit `source: 04A` nodes and existing material manifest | Shared lantern/plant/crystal modules, surface maps, upper Tier/Crown geometry and static material batching. Original asset GUIDs remain intact. |
| Local room sound | `InhabitedResidentRules04C.RoomTone` mathematical source | Optional four-second harmonic clips generated at runtime. No recorded speech or remote audio API. |

Model masters are door frame, timber bench, chair frame, bed frame, folded bedding, bookcase, service counter, medical cot, tool rack, ventilation hood, pottery set, record stack, survey scope, round table, banded freight crate and engine stand. There are **16 masters and 32 high/low variants**, not 32 distinct props or gameplay venues. The library totals 11,536 high and 6,976 low triangles before room repetition and preserved upper architecture. Bookcase and record stack intentionally share already-coarse structural topology between LOD files; the full low library still reduces triangles.

Meshes use sensible ground/base pivots and authored UVs/normals, with finite/winding/degeneracy/OBJ-to-payload/bounds checks. CMH1 assets and editable models use Git LFS. Generator metadata creation preserves an existing `.meta` verbatim instead of regenerating valid identifiers. Editable OBJ files are retained beside runtime sources for reproducibility; imported OBJ duplication/build-size cost still needs native packaging review.

## Structural and collider intent

Critical floors, ceilings, walls, stairs, railings and open thresholds stay resident. Structural boxes are actual slab/wall pieces; furniture uses original shaped masters. Solid furniture uses the low mesh as static collision. Decorative objects and text use no gameplay collision where explicitly authored. Source geometric reviews inspect both high and low visible triangles, collider surfaces, floor support, headroom and static route connections. They do not execute Unity physics.

Source expansion produces the following descriptor counts. These are **not observed runtime GameObject, renderer, draw-call or performance counts**; native QA records those separately.

| Venue | Expanded source nodes | Exported collider descriptors |
| --- | ---: | ---: |
| Lofts | 206 | 151 |
| Clinic | 108 | 69 |
| Forge | 82 | 42 |
| Diner | 112 | 76 |
| Archive | 129 | 84 |
| Observatory | 191 | 159 |
| Garage | 58 | 26 |
| Depot | 82 | 49 |

The original upper building height/04A Tier/Crown and solid roof support are retained above occupied floors. Protected helipads, antennas, custom lots and tiered canonical candidates are rejected rather than flattened. Original building footprint records remain outdoor spawn exclusions. A failed staged venue leaves the original building path intact.

Native render costs are unmeasured. Static parts batch by surface through the existing 04A batching path. Optional nearby practicals are shadowless and limited to at most three globally, or one on Low/ambience-off. Resident admission/streaming is separate from immutable shell collision. Low detail never removes a required floor, doorway, stair, clue host or exit.

## Review evidence and remaining art work

`Previews/` contains 17 labeled, provenance-checked offline source views, not Unity screenshots. Its README describes approximate lighting/material/font, ordinary-budget baseline limitations, static resident poses and omitted native dressing/Forge prop. Source review produced actual material, light, partition, support and sign changes; images were regenerated afterward.

Native import, actual shaders/LODs, camera/ceiling appearance, Windows build size, live occupants, performance and final art acceptance remain unrun. Archive has one reference floor and no upper gallery; unsupported seating animation, animated doors/lifts and every-floor interiors were not added. Prioritize the Windows review before expanding optional art.
