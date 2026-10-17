# Visual upgrade, Phase 0: audit

Revision audited: `becd362` on `feature/underheart-bonds-04e`. Audited from source in a cloud session with no Unity
editor and no GPU. Nothing here is a measurement.

## Pipeline, version, platform

- Unity 6000.3.16f1 (Unity 6.3 LTS), Windows standalone, DX12 in the owner's editor.
- **Built-in Render Pipeline is active** (`GraphicsSettings.m_CustomRenderPipeline` is empty, every quality level has no
  custom pipeline). The URP beta exists in source behind `NS_URP` but the URP package is not in `Packages/manifest.json`,
  so the owner's project does not run it. HDRP is not installed anywhere.

### Decision: stay on Built-in and push it; no HDRP migration

Reasons, in order:
1. The post stack (`Core/NeonPost.cs`, `Resources/NeonPost.shader`) runs through `OnRenderImage`, which HDRP does not
   support. HDRP would need the whole stack rewritten as a custom pass or Volume components.
2. Every material is procedural through `Core/Mats.cs` against the Standard shader's property names; HDRP/Lit uses
   different names and workflows, so the whole `Mats` layer, `MatsUrp` and the custom character, drake and post shaders
   would be rewritten.
3. The twelve compile configurations (editor and player, three input modes, Built-in and URP) are the project's
   contract. HDRP would add a thirteenth and fourteenth with no offline reference assemblies to check them.
4. No GPU or editor exists in this session. A pipeline migration that cannot be seen once is the kind of change that
   ships a black screen or magenta city.
5. The owner's "before" golden shots were captured on Built-in, so an upgrade on the same pipeline can be judged against
   them with the existing Compare Golden Shots tool.

Built-in forward still supports everything that matters for the look: physically based materials with normal,
occlusion and detail maps, cascaded soft shadows, realtime reflection probes, a custom physically based sky, a sun
shaft pass, strong screen-space ambient occlusion, bloom, ACES, grain, vignette, depth of field and FXAA or MSAA.
What it cannot give: screen-space reflections, DLSS, hardware ray tracing. Those are stated as not delivered.

## Inventory

### Lighting
- `World/Atmosphere.cs`: one sun (soft shadows, strength 0.8, follows the clock), one moon (no shadows), Trilight
  ambient, exponential squared fog, a procedural sky material, reflection intensity by time, a Veil colour override,
  interior shelter dimming, lightning flashes.
- Quality presets in `Core/Settings.cs` (`ApplyGraphics`): Low hard shadows 45 m, 1 cascade, 2 pixel lights, no MSAA;
  Medium soft 80 m, 2 cascades, 4 pixel lights, MSAA 2; High 130 m, 4 cascades, 8 pixel lights, MSAA 4, VeryHigh shadow
  resolution; Ultra 190 m, 4 cascades, 12 pixel lights, MSAA 8.
- Point lights are created in 25 source files (street lights, venues, the Underheart regions, bosses, vehicles,
  explosions through the shared `Fx` flash pool, the 04A ambience scheduler). The Underheart caps its lit lights at
  8 (5 on Low) and 7 beside a boss fight.
- Global illumination: none. No lightmaps (the city is generated at runtime, so nothing can be baked), no light probes,
  no realtime GI. Reflection probes only in `ArtRebirth04A/VisualRebirth04AAmbience.cs` and the crystal district.

### Materials and textures
- `Core/Mats.cs` (76 material entry points, most cached per colour), `Mats.Zones.cs` (14), `Mats.Underheart.cs` (23).
  Shader families: Standard, Unlit/Color, Unlit/Texture, Sprites/Default, particle additive, `NeonSyndicate/Character`.
- `Core/Tex.cs` plus `Tex.Zones.cs` and `Tex.Underheart.cs`: asphalt, pavers, weeds, concrete, grass, sand, dirt,
  rock, gravel, roof tiles, corrugated, wood, bark, billboard, storefront, character palette, metal and gloss map,
  emission, detail normal, soft dot, smoke, blood, bullet hole, scorch, skid. One generic `NormalFrom(height)` exists
  and is rarely used; there are no occlusion maps and no detail maps.
- Characters: `Resources/NeonCharacter.shader` (surface Standard, palette, metal and gloss, emission, one detail normal,
  a rim term). No subsurface approximation, no cloth sheen.
- Packaged art from the 02D, 02F, 02G, 02I, 03A2, 04A, 04B and 04C passes under `Resources`: 216 `.obj` meshes,
  102 `.png`, 90 `.mat`, 192 `.bytes`. So the project is mostly code-generated with a layer of packaged meshes in the
  crystal district, citadel, strongholds, territories and inhabited venues.

### Post-processing (`Core/NeonPost.cs`)
- Bloom, ambient occlusion (High and up), camera motion blur (Medium and up), eye adaptation, ACES grade from the
  shared `Core/PostGrade.cs`, FXAA when MSAA is below 4x, vignette, film grain. No depth of field, no chromatic
  aberration, no TAA, no volumetrics.
- URP mirror: `NeonUrpPost`, `UrpGradeMapping`, `NeonPostFeature`, `Resources/NeonPostURP.shader`.

### Camera
- `Characters/PlayerCamera.cs` and partials: foot, vehicle, first person, ragdoll, title flyover, `SetCinematic`.
  Plain `fieldOfView` from settings, no physical camera properties, damped follow already.

### Geometry
- Meshes from `MeshBuilder`, `Shapes`, `MeshKit`, `BodyBuilder`, `CreatureBuilder`, vehicle and weapon model files, the
  Underheart builder. Normals and tangents need an audit before normal maps land (Phase 4).

## "Before" screenshots

The owner captured a golden set on 2026-10-17 with Neon Syndicate > QA > Capture Golden Shots at 1920x1080 on Built-in:
14 fixed views at 12:00 and 22:00 (`QA/GoldenViews.cs`). The three headline views for the side by side are
`core_street`, `strip_street` and `frostline_outpost`; the compare report covers all 28 images.

## What cannot be done from this session

- Frame rate at the screenshot positions: no GPU. Every new effect is preset-gated (nothing new on Low) and the owner
  measures with QA > Run Perf Flythrough before and after.
- Screen-space reflections and DLSS: not available in Built-in forward.
- Seeing the result: the owner's after capture and Compare Golden Shots report are the proof.
