# H2 original mesh kit

Source: tools/author_ascension_h2.py. Actual interchangeable OBJ files: Assets/NeonSyndicate/Resources/AscensionH2/Models. Runtime high/low CMH1 streams: AscensionH2/Meshes. Eight original 256-square material families have sRGB albedo, linear tangent normals and linear RGBA masks (metallic, occlusion, unused, smoothness). License declaration: original project-authored CC0-1.0. No external art, photograph, vendor model or concept image is represented as 3D production art.

| Module | Shape / role | Runtime use |
|---|---|---|
| portal_bay | Deep pointed voussoir arch, coursed piers, inset moulding and plinth | Oath gallery and witness |
| flying_buttress | Stepped pier, curved flying support, pinnacle | Outside nave |
| ribbed_vault | Pier-free pointed shell, transverse/diagonal ribs and keystone | Gallery and witness roof |
| cluster_column | Five shafts, stepped capital/base | Gallery piers |
| slate_roof | Steep pitched overlapping courses and ridge | Nave skyline |
| reliquary_door | Deep layered pointed threshold | Witness approach |
| tracery_wall | Actual pierced lancets and mullion | Side galleries |
| gallery_rail | Open miniature arches and piers | Raised gallery edge |
| processional_stair | Individual treads, smooth low-detail ramp | SOURCE ONLY, not placed |
| mineral_pier | Asymmetric substantial mineral shafts | Masonry penetrations |
| broken_apse | Curved broken masonry perimeter | Witness sanctuary backdrop |
| forge_bay | Deep service portal, smoke hood, structural sides | SOURCE ONLY, old Forge shell retained |
| basalt_strata | Layered irregular geological mass | Azure walls |
| root_causeway | Three swept, tapered mineral roots | Azure canopy and crossing edges |
| cavern_shell | Asymmetric broad cave roof and stratum sides | Cave and vein segments |
| fern_colony | Solid tapered leaf fan, no alpha cards/collider | Azure floor dressing |
| crystal_seam | Broken rooted mineral line | Cave/ecology accents |
| castellan_mantle | Open segmented metal ribs | Existing phase/parts vane pivots, revision 8 only |
| record_lens | Circular mineral-reading frame and plinth | Original physical Interact props |

Metres, +Y up, +Z forward. Geometry uses planar UV projections at 2 metres per repeat; no baked lightmaps or UV2 are claimed. Asset-specific bounds and actual high/low counts are in asset_manifest.json and ASSET_MANIFEST_H2.json. This is an offline parameter-authored static kit, not a sculpted/scanned/imported professional art package. Low geometry is selected at generation for Low graphics and used consistently for collision. It is not a runtime distance-LOD transition.

The existing original chunk owner merges by material and uses its original collision buffers. No GameObject per stone or decorative leaf; foliage and mantle carry no collider. Roofs and structure do. Asset readiness resolves every mesh and material before the route suppresses H1 structures; a missing/corrupt source logs one warning and falls back. The new static caches reset from Game.ResetStatics. Existing shader/pipeline authority remains Mats.StandardShader; no package or renderer assignment changed.

Five-seed tests exercise actual transformed asset triangles and real ground for body/companion-height lanes and inherited walking paths. That is not PhysX, a camera/vehicle/flight playtest or profiling. Actual native inspection remains required for roof collision, visual seams, scale, light/shadow, texture resolution and camera catchment.

Unreplaced categories: original home and service shells; central boss court; wider settlement buildings/skyline; ordinary NPCs; final hero/creature bodies; professional foliage/rock sculpt detail; final material art and audio. Native quality and performance approval remain open.
