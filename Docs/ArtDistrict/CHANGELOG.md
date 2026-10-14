# Changelog: Asterfall art demonstration 1.0

## Visible changes

- Introduces Asterfall Promenade on both sides of Core Avenue near x22 to x238 and z0. It replaces selected building visuals instead of rebuilding the map.
- Adds original sandstone arches, bronze latticework, modeled cornices, balconies, pilasters, ceramic roof domes, lanterns, fabric canopies, crystal clusters and rooftop gardens.
- Adds normal-mapped roughness/metallic material variation and an opaque crystal shader with emissive mineral veins, a grazing rim and restrained time pulse.
- Adds sparse world-space crystal particles, capped globally and hidden near the camera to protect first-person readability.
- Adds local atmospheric color/haze and at most four nonshadow crystal lights without forcing the weather, time or global graphics preset.
- Upgrades the existing Quartz Drake at its three detail levels and adds secondary procedural motion. One cosmetic exhibit can appear on a suitable district roof.
- Updates the displayed title/version to GideoVamesAI: Crystal Manor Saga / Asterfall Art District 1.0. Project folders, save identifiers and content IDs retain their original names.

## Technical changes

- Adds `Runtime/ArtDistrict` for architecture, material caching, effects, lighting and the display drake.
- Reuses the original building tier layout and collider-emission code. Excludes helipads to preserve their separate landing collider. Does not modify CityLayout or navigation data.
- Completes each optional art build before original physics is emitted. Missing resources or failed art construction falls back to the original building visuals.
- Merges each building by material, supplies a simpler second LOD, releases combined mesh CPU data after upload and explicitly owns generated meshes.
- Keeps imported source meshes readable and supplies explicit normals, UVs, tangents/import rules and stable metadata.
- Adds automatic active-pipeline shader retention at editor initialization and before player builds for normal/metallic/occlusion material combinations.
- Resets new mutable static caches from the existing Game reset path for repeated Play Mode sessions.
- Adds editor-only visit/repair/library commands, with on-foot and no-active-mission checks. No new player input bindings or in-game UI panels.
- Adds four district EditMode tests and twelve parameterized creature regression cases. These tests are included but not run here.

## Preservation and scope

The complete original project is included. Nine original source files were changed for narrow visual/integration hooks; the exact diff is `BASELINE_CHANGES.diff`. No original file was deleted. Story/quest/event data, player controls, cameras, UI, save schema, combat rules and city layout were preserved. Static comparison verifies 153 protected original files unchanged, but this does not prove runtime equivalence.

Full-world restyling, complete Manor interiors/evolving rooms, new humanoid production art, large forests/realms and a complete spell suite are deferred. No package upgrade or renderer migration was made. The separate Browser3D build and archived projects are unchanged.
