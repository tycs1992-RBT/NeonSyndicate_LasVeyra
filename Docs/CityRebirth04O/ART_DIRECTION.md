# Implemented geometry and remaining art work

The owner's Windows Unity screenshot was inspected as the before-reference: long straight avenue, tall repeated window walls, modest skyline variation and road-map-dominated HUD. Its exact source commit and camera coordinates are unknown. Its hash and dimensions are in Evidence/native-status.json. It proves an owner Editor can render that scene, not this branch's acceptance.

The shipping generators replace ordinary wall masses with six different structural forms. Civic halls have faceted lower bodies and pitched mineral vaults; hearth residences use staggered terraces; forge buildings combine a steep nave with an off-axis polygonal furnace tower; canal compounds stand on piers; signal ruins separate broken wings; garden towers use polygonal stacked bodies. Two additional treatments serve low fringe remains and collectible roof reliquaries. Sparse deep-looking slits and structural ribs replace repeated window registers on these bodies.

CityRebirthGeometry04O emits closed solids with outward triangles and world UV projections. Exact non-box collision uses the same mesh function. Shared procedural stone, metal, glass/crystal and paving come from the current Built-in/URP bank. Missing optional bank data falls back once per builder to the original materials, preserving boot and geometry. No HDRP conversion, particle-count increase or new lighting flood was added.

Ordinary bodies merge through original160m material chunks. Four Spine groups have near/far variants; Low retains their principal ribs with cheaper rings. New Spine and merged chunk meshes have explicit OwnedMeshes destruction. Actual costs and appearance still need native profiling and inspection.

Geometry/compiled-geometry-review.png renders actual exported vertices under neutral offline lighting. It is clearly labelled NOT UNITY. Four sample OBJs and all25 export hashes are provided. These artifacts diagnose structural shape; they do not validate materials, shadows, clipping, daylight/night or performance. More ornament, believable openings, canal support composition, foliage and hero prop detail remain production work.
