# 01B asset inventory

This records the delta from Master Build 01. `Docs/ArtDistrict/ASSET_INVENTORY.md` remains the complete historical starting inventory.

| Asset group | Delivered state | Source location |
| --- | --- | --- |
| Modular architecture | 23 OBJ files retained; eight revised, 15 byte-identical. Same GUIDs. Unique source triangles reduced from 10,270 to 9,150 (10.91%). This is not a measured scene triangle or draw-call count. | `Assets/NeonSyndicate/Resources/ArtDistrict/Models` |
| Revised modules | `arch_frame`, `arch_infill`, `window_frame`, `window_infill`, `cornice`, `pilaster`, `parapet`, `dome` | Same source names and Resources paths as Master01 |
| Texture library | 32 PNG files retained, 31 revised. Ten albedo/normal/mask sets plus packed crystal pattern and mote. Same existing resolution and import GUIDs. | `Assets/NeonSyndicate/Resources/ArtDistrict/Textures` |
| New material variants | `promenade`, `ceramicViolet`, `crystalViolet`. Reuse existing map sets, no extra texture files. | `Scripts/Runtime/ArtDistrict/DistrictMaterials.cs` |
| Building crystals | Revised opaque shader with lit facets, mineral inclusions and fracture emission; Built-in and guarded URP subshaders. | `Resources/ArtDistrict/Shaders/DistrictCrystal.shader` |
| Ambient FX | Same mote shader/texture and emitter count. Orbital motion and partial trails added in existing component. | `Scripts/Runtime/ArtDistrict/DistrictCrystalFx.cs` |
| Active drake surface | One shared material, three 16 x 16 palette maps and two 256 x 256 repeating detail maps generated once at runtime. Approximately 686 KiB uncompressed RGBA32 including detail mips. Released by registered reset. | `Scripts/Runtime/Creatures/CrystalDrakeSurface.cs` |
| Drake shaders | New `CrystalDrake.shader` and `CrystalDrakeURP.shader`, derived from the project's existing character shaders. Generic character shaders are unchanged. | `Assets/NeonSyndicate/Resources` |
| Drake geometry/animation | Retained Master01 QuartzRegalia mesh, rig and pose solver; no new sculpt, animation clips or facial rig. | Existing creature builder/pose source |
| Artist dragon map samples | `dragonHide` and `dragonWing` maps refined in the library. They are available samples, not the active skinned drake shader's UV palette. | Existing ArtDistrict texture library |
| Comparison evidence | Twelve user-reference thumbnails with provenance, actual before/after module geometry, and source-based change table. No Unity-rendered after image exists. | `Docs/ArtDistrict01B/VISUAL_COMPARISON.html` |

## Origin and integration

All new geometry and procedural map code are included for editing. No external asset-store package was installed, and no reference image was used as a game texture or traced into a model. Reference thumbnails are included only for the user's comparison report, not as game assets.

No paid production asset is claimed to be supplied. The remaining asset list specifies the authored 3D models, rigs, maps and clips needed for the target quality.

Import in the project's recorded Unity version. The existing editor tools retain runtime material variants and can generate editable material/prefab copies. Those copies are preserved on reruns and are not live links to runtime art.
