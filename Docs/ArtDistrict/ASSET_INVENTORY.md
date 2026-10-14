# Asset inventory

## Included original assets

The new geometry, textures and shaders were authored for this project during this delivery. No purchased, downloaded or third-party model/texture pack was imported. Existing content is retained with its original provenance. These are original modular/source-generated assets, not commissioned film-quality scans or sculpted production characters.

| Category | Included |
|---|---|
| Modular geometry | 23 OBJ source meshes, 10,270 total unique triangles |
| Textures | 32 PNG maps at 512 x 512, 10 surface triplets plus mineral-emission and mote masks |
| Runtime materials | 13 shared material keys; dragonHide/dragonWing are available art-library options, not applied to the existing palette-based drake |
| Shaders | Built-in and package-gated URP crystal and mote shaders |
| Creature | Quartz Drake mesh additions at 3 detail levels; procedural animation additions |
| Demonstration | One Core Avenue street, roof gardens, embedded crystals, one cosmetic rooftop drake when a suitable roof is found |
| Reusable source | Model/texture generators, import rules, editable library command, static validator |

## Modular geometry

Resource folder: `Assets/NeonSyndicate/Resources/ArtDistrict/Models`. Unit scale is metres. Y is up; facade modules face positive Z. Imported assets have no colliders. The imported mesh-prefabs can be reused directly, or wrapped through the editable-library command.

| Mesh | Triangles | Intended material | Use |
|---|---:|---|---|
| `block` | 12 | sandstone | Unit backing volume. Scale to wall or glass panel dimensions. |
| `arch_frame` | 1,020 | sandstone | Thirteen segmented sandstone voussoirs with dressed jamb blocks and layered spring mouldings. |
| `arch_infill` | 104 | glass | Convex arched pane paired with arch_frame, opaque glass material for economical rendering. |
| `window_frame` | 652 | bronze | Arched bronze window with raised stepped mouldings, corner bosses and double mullion. |
| `window_infill` | 88 | glass | Recessed arched pane paired with window_frame. |
| `lattice_panel` | 768 | bronze | Open geometric diamond screen with nested eight-point rosettes; actual open spaces. |
| `balcony` | 1,316 | bronze | Solid projecting deck, rounded rails, turned posts, central fan and open diamond balusters. Mount above y3. |
| `cornice` | 300 | sandstone | Layered ogee roof moulding with nineteen individually modelled dentils. |
| `pilaster` | 400 | sandstone | Fluted engaged column with stepped base and scalloped capital; front projection is 0.30m. |
| `parapet` | 196 | sandstone | Rooftop crest with five faceted petal battlements and a dressed coping course. |
| `dome` | 638 | ceramic | Flattened ceramic cupola, ribbed base profile and modest pointed crown; scale uniformly for roofs. |
| `crystal_cluster` | 238 | crystal | Seven asymmetrical six-sided crystal prisms with bevelled bases and pointed terminations. |
| `crystal_shard` | 34 | crystalWarm | Standalone warm crystal for wall insets and small effects. 0.4m wide and 1m high. |
| `lantern` | 570 | bronze | Six-sided pierced bronze lantern cage, domed crown and suspension ring. Place lantern_core at same origin. |
| `lantern_core` | 44 | crystalWarm | Emissive six-sided inset for lantern, paired origin. Use no realtime point light by default. |
| `planter` | 238 | ceramic | Octagonal elongated planter with stepped foot, flared body and thick rolled lip; open top. |
| `tree_trunk` | 656 | bark | Sculptural six-limbed desert tree with tapered curved boughs and exposed buttress roots. Pair tree_canopy at same origin. |
| `tree_canopy` | 1,772 | foliage | Layered umbrella canopy with 88 folded lanceolate leaves around smooth opaque crown cores. No alpha overdraw. Pair origin with tree_trunk. |
| `tree_canopy_lod` | 60 | foliage | Low-detail single-volume canopy alternative for distant trees, same tree origin. |
| `canopy` | 760 | fabric | Draped textile awning with radial seams and scalloped front valance. Wall z0, front z1.5; mount above y3. |
| `canopy_bracket` | 108 | bronze | Two swept support arms paired with canopy origin, plus front rail. |
| `obelisk` | 156 | basalt | Chamfered civic plinth with recessed tiers, tapering faceted body and crystal mounting seat. |
| `rosette` | 140 | bronze | Eight-point star medallion for facade crests. Geometric invented motif. |

Unique-kit triangles count each source mesh once, not all placed copies. Runtime district triangle and draw-call counts require Unity capture. Exact dimensions, bounds, hashes and paired origins are in `mesh_inventory.json`. The district uses a subset; unused pieces are included for extension.

## Materials and creature

Texture triplets use `*_Albedo`, `*_Normal`, and `*_Mask`. Packed mask channels are R metallic, G occlusion, B roughness, A smoothness. Normals and packed data are linear; the shader uses the separate linear mineral emission mask. See `Docs/ART_DISTRICT_MATERIALS_AND_FX.md` and the texture manifest for importer data and hashes.

Quartz Drake additions are attached to existing bones and use the existing palette material. The hero additions are approximately 8,820 vertices / 3,780 triangles, standard 3,668 / 1,572, and crowd 70 / 30. These figures are derived from the mesh source, not a measured Unity renderer capture. Secondary motion adds neck breathing, small jaw movement, asymmetric wing settling and flight-tip lag. No new skeletal bones, imported animation clips, attack logic or collision volumes are introduced. The rooftop version disables all hit colliders and has no combat or companion ownership.

## Assets still needed

| Priority | Art package | Acceptance scope |
|---|---|---|
| Next | In-engine district look development | Imported orientation, UV scale, shader compilation, nighttime readability, close-up normals, LOD transitions and screenshots on the target GPU |
| Next | Production humanoid hero and NPC set | Sculpt/retopology, clean skinning, faces/hair/clothing, PBR texture sets, animation coverage and crowd LODs; current humanoids retained |
| Next | Hero dragon production pass | Detailed sculpt and bakes, deformation review, authored facial/wing/landing clips, sound and VFX synchronization; current upgrade remains procedural |
| Later | Crystal Manor architecture and rooms | Exterior hero kit, actual navigable interior, furnishing, evolving-room state variants and story-stage collision/navigation review; original Manor story site retained |
| Later | Mystical forest biome | Hero trees, foliage range, rocks, roots, ground blend layers, wind, collision and streaming review; this demo has rooftop garden assets only |
| Later | Dimensional realm environments | Terrain and landmark kits, realm-specific sky/lighting/material sets and transition composition; existing Veil remains unchanged |
| Later | Complete fantasy spell suite | Authored casting/impact/area/sustain effects with telegraph clarity, sounds and graphics tiers; this release adds ambient crystal resonance and shader effects |
| Later | Environment production polish | Interior views, signage typography, decals, damage/weather variants, reflection/light-probe strategy and measured occlusion/streaming |

The new modules have no external asset-license dependency.
