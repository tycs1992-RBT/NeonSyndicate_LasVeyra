# Build 02D: Asset inventory and implementation notes

## New reusable geometry

`CrystalCity02DGeometry` contains twelve original mesh definitions, each with separate near and far geometry. They are constructed at runtime, cached and shared. They are not downloaded models, imported character rigs or hand-painted production assets.

| Key | Geometry | Use |
| --- | --- | --- |
| orbit | Closed faceted tubular ring | Roof orreries, sun discs and sanctuary rings |
| shard | Offset double-terminated mineral | Roof jewels and crystal plants |
| crown | Six curved ribs and base ring | Jewel cages and stone sanctuary supports |
| trunk | Curved trunk, roots and six branches | Roof and garden trees |
| leaves | Folded, double-sided leaves along branching stems | Replaces the old canopy inside the district roof art path |
| vine | Hanging stems with paired leaves | Facade drapery and garden trees |
| fern | Radiating fronds and paired leaflets | Planting pockets |
| bloom | Two radial petal layers | Crystal flowers |
| rosette | Radial stained-glass petals | Facade and roof medallions |
| buttress | Curved segmented structural rib | Dawnstone facade relief |
| lantern | Open mineral cage | Elevated facade lights, with emissive jewel but no new Light |
| relic | Irregular suspended stone fragment | Sparse rooftop sky relics |

Positions, normals, UVs and triangle winding are authored by the generator. Degenerate triangles are skipped. Meshes are combined by material into the existing building LOD groups or a new per-garden LOD group. The source mesh cache remains readable for batching and tests; combined output meshes release CPU copies and are owned by the existing OwnedMeshes cleanup component.

## Materials and patterns

Three palettes clone the existing district surface materials and retain their original surface maps. The new patterns are three 256x256 RGBA textures with mipmaps: radial stained glass, leaf veins and a rune mask. These are simple procedural patterns, not a new high-resolution scanned PBR texture pack.

Crystal materials reuse the existing project shader/fallback path. No new shader, rendering pipeline, postprocessing, camera effect, global fog, clock change or package is introduced. Surface clones are opaque; there is no blue fullscreen layer. Emission does not mean additional physical scene lighting.

New geometry and patterns were authored in this development pass. No third-party art was downloaded or redistributed. Pre-existing assets and their provenance are retained; this inventory is not a new license audit of the older project.

## Automatic integration

The existing CityBuilder calls DistrictBuilding for selected buildings and CrystalGardenDressing for parks. Those entry points remain the integration paths. DistrictBuilding now has a focused partial file for 02D detail. Constructor-derived geographical style changes bay spacing, materials, lower facade relief and rooftop composition.

Game.ResetStatics already calls DistrictArchitecture.ResetStatics. That reset now also clears the 02D source meshes, cloned materials, generated textures and garden counters. No new bootstrap, input or UI system is needed.

## Boundaries and costs

- Detailed building selection keeps the earlier Low/Medium/High/Ultra caps of 35/80/145/210 city landmarks, plus the original special selections. No additional city-wide selection expansion in this sprint.
- Gardens have caps of 8/18/30/40; selected locations may be fewer after clearance checks.
- Each generated garden uses material-batched near and far meshes, not one GameObject per leaf or flower.
- Gardens cull at a relative screen height of 0.016 and switch LOD at 0.06. Buildings retain their previous LOD thresholds.
- Source meshes have reduced-detail variants; Low graphics uses those in the close pass too.
- No new colliders, rigidbodies or per-decoration Update loops are added.
- Existing global crystal-mote caps are retained.

## Still unfinished

Ordinary sidewalk trees, vehicles, character models, creature animation, authored interiors, full realm forests, functional new portals, new quests and city-wide asset replacement are not part of 02D. Floating relics are static scenery. New foliage does not yet have wind animation. Materials and emissive strength need a real day/night visual review. This is not a promise of a particular review score or concept-art equivalence.
