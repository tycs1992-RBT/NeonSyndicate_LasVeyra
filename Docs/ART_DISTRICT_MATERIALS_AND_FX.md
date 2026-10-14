# Demonstration district material and crystal effect library

This pass adds original authored surface data and two custom shaders. It does not replace materials throughout the existing city. The ten repeating surface sets use 512 x 512 maps so the new demonstration district has a bounded texture budget. These are designed surface textures, not scanned materials or final close-up film assets.

## Palette and material use

Warm carved sandstone and dark basalt establish the architectural mass. Restrained bronze, deep teal ceramic and opaque blue glazing form the trim. Cyan crystal is the main magical accent. Amber crystal is reserved for entrances and ritual points. Forest accents use jade foliage and plum bark. Keep a large majority of visible surfaces non-emissive so crystal accents remain legible.

| Material key | Surface and intended use |
| --- | --- |
| `sandstone` | Dressed warm stone, offset courses, recessed mortar and mineral grain |
| `basalt` | Dark volcanic plinths, road edging and foundations |
| `bronze` | Brushed, engraved trim with restrained green patina |
| `ceramic` | Glazed teal tile insets and shop details |
| `glass` | Opaque cobalt privacy glazing, no refraction or transparent sorting |
| `crystal` | Cyan mineral veins, faceted highlights and slow emissive rim |
| `crystalWarm` | Amber and rose variation for important entrances |
| `foliage` | Opaque modeled leaves with subtle vein relief |
| `bark` | Vertical ridged fantasy cedar bark |
| `fabric` | Woven dusty rose awning cloth |
| `dragonHide` | Repeating sculpted blue-grey overlapping scales |
| `dragonWing` | Plum leather membrane with periodic vein relief |
| `particle` | Shared depth-tested crystal mote material |

## Integration API

```csharp
renderer.sharedMaterial = DistrictMaterials.Get("sandstone");
DistrictCrystalFx.Attach(crystalTransform, new Vector3(0f, 1f, 0f), 1f, false);
```

Use `sharedMaterial` or `sharedMaterials`. Calling `renderer.material` would create a separate material for each object and prevent useful sharing. Meshes using normal maps need UV0 and correctly generated tangents. Repeating UVs should describe approximately one 2 m tile per UV unit for general architecture; adjust the mesh UVs, not the shared material, for exceptional surfaces. Small creature details can use denser UVs.

`Game.ResetStatics()` calls both `DistrictMaterials.ResetStatics()` and `DistrictCrystalFx.ResetStatics()`. The material reset destroys only runtime-created copies. Resource textures are never mutated or destroyed by this code. All cache initialization happens when first requested, with no runtime texture generation.

## Texture inventory and packing

Resources are under `Assets/NeonSyndicate/Resources/ArtDistrict/Textures`.

There are 32 PNGs: ten surface sets with three maps each, one shared crystal vein map and one analytic mote sprite. All images are 512 x 512. `texture_manifest.json` lists their SHA-256 hashes. `tools/generate_district_textures.py` reproduces them using numpy and Pillow from fixed analytic formulas and seeds. No downloaded image, photographic scan or third-party source art is used.

| Suffix | Channels | Import requirements |
| --- | --- | --- |
| `_Albedo` | RGB color | sRGB on, opaque |
| `_Normal` | Tangent-space normal | Unity Normal map type, sRGB off, no grayscale conversion |
| `_Mask` | R metallic, G occlusion, B roughness, A smoothness | sRGB off, alpha preserved |
| `crystal_Emission` | RGB vein mask | sRGB off |
| `mote` | White RGB and radial alpha | Alpha preserved |

The `.meta` files set mipmaps, 512 maximum size, normal texture handling, 4x anisotropy, trilinear filtering and no CPU read/write copy. The data texture masks use roughness in blue for authoring inspection; Standard and URP Lit sample metallic from red and smoothness from alpha. Occlusion reads green. The script supplies the corresponding shader keywords for each pipeline. Texture compression is requested; final GPU formats and actual memory use must be confirmed in Unity for each build target. At BC1/BC3/BC5-equivalent compression, the set is roughly 8 to 11 MiB including mipmaps. This is an estimate, not a profiler measurement.

## Render pipeline behavior

Normal surfaces use the project's existing `Mats.Textured` pipeline mapping. Built-in uses Standard; the optional URP path uses URP Lit. All architectural materials are opaque, including glass, to avoid transparency sorting and preserve depth testing. Shadow and reflection behavior therefore follows the established renderer path.

`DistrictCrystal.shader` has an optional URP subshader guarded by ShaderLab `PackageRequirements`, followed by a Built-in Standard surface subshader. The guard follows the pattern in the existing character shader. URP pass coverage includes forward PBR lighting, shadow casting, depth, normals and object motion vectors. Both paths use mineral vein emission and a modest rim pulse driven by shader time, avoiding per-renderer material updates. If the custom shader resource is absent or unsupported, the C# material factory uses a standard emissive material.

`DistrictMote.shader` is additive and depth-tested with depth writes disabled. It does not sample scene color or depth, distort the image, add a full-screen effect or draw through walls. The URP subshader is package-guarded. Emission relies on existing bloom where enabled; no bloom setting is changed by this library.

Unity's relevant reference: https://docs.unity.com/en-us/engine/6000.3/manual/materials-and-shaders/shaders/writing-custom/shader-writing/writing-shader-tags/require-package

## Effect and performance limits

- At most 12 active emitters, with at most 24 particles each, for a 288-particle cap from this library.
- Five particles per second per active emitter and a lifetime of 2 to 3.2 seconds.
- Camera distance gate at 58 m, checked four times per second.
- Emitters stop and clear inside 1.5 m of the camera to reduce first-person obstruction.
- Maximum projected particle size is 1.8 percent of the viewport dimension.
- No effect point lights, collider queries, screen overlays, persistent allocations in Update or per-frame material instances.
- Offscreen particle simulation pauses. Distance gates clear particles, so re-entering does not accumulate a backlog.

The cap bounds these effects; it does not guarantee a specific frame rate for the complete city. Active effects are admitted in update order, rather than sorted by distance, to avoid a sorting allocation and central manager. In this small demonstration district the number of attached effects should remain near or below the active cap.

## Verification and remaining art

The local checks can validate PNG dimensions, deterministic output, metadata, packed channels, normal-vector ranges and shader structure. They cannot prove Unity import success, shader compilation, GPU frame time, reflection quality or the intended appearance. Check both pipelines in Unity 6000.3.16f1 and a Windows player build, including a repeat Play session with domain reload disabled.

Review close-up UV stretching, normal-map direction, mortar scale, specular aliasing, emission clipping at night, day readability and the first-person walk through crystals. Run the existing golden screenshots and performance capture before and after this pass. Check that optional pipeline switching does not produce pink materials.

Further art required for a production visual target: high-resolution hero texture painting where the camera approaches within arm's reach, additional architectural variations and wear masks, authored spell cast/travel/impact sequences, elemental effect identities, and a final lighting-driven material calibration pass. This library supplies ambient crystal resonance, not replacement visuals for every combat spell.
