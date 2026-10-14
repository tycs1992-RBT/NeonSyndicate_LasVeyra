# Neon Syndicate on the Universal Render Pipeline (beta)

Phase 1 of the roadmap: an opt-in path that renders the game with Unity's Universal Render Pipeline (URP 17.3, the
version that ships with Unity 6000.3). The **Built-in Render Pipeline stays the default** and behaves exactly as
before. Nothing changes for anyone who never uses the menus below.

**Honest status:** the URP path compiles cleanly against the real URP 17.3 and SRP Core 17.3 sources, the two URP
shaders pass an HLSL syntax check against the real URP shader library, and the colour grade mapping was checked
numerically against ports of both pipelines' shader code. **Nothing of it has run in a Unity editor or on a GPU yet**:
every visual result, the menu flow, the frame rate and builds are untested. The checklist at the end is what to look at
in the first session.

## Switching

All three menus are under **Neon Syndicate > Graphics**. Leave Play mode first.

### Switch to URP (beta)

1. Confirms what it will do.
2. If the URP package is missing, installs it through the Package Manager with no version string, so Unity picks the
   version made for this editor (17.3.0 on 6000.3), and waits. Unity then recompiles the scripts and reloads; the
   switch carries on by itself after that reload (a SessionState flag plus an `[InitializeOnLoad]` hook). If the
   install fails, a dialog says why and nothing is changed.
3. Creates, if missing:
   - `Assets/NeonSyndicate/Rendering/NeonURP_Renderer.asset`: a Universal Renderer in **Forward+**, with the
     **Screen Space Ambient Occlusion** feature (radius 0.45 m, intensity 1.2, applied to direct light at 0.85 like
     NeonPost's composite, fading out at 180 m, half resolution) and the **Neon Post** feature (see below).
   - `Assets/NeonSyndicate/Rendering/NeonURP.asset`: the pipeline asset. HDR with 64-bit colour (the half-float
     buffer the Built-in camera uses), Low Dynamic Range colour grading, SRP Batcher on, MSAA off, depth and opaque
     textures on, main and additional light shadows with soft shadows (medium filter), reflection probe blending and box
     projection, the High preset's shadow distance and cascades. An existing asset is left as it is, so tuning sticks.
4. Assigns NeonURP as the default render pipeline and on every quality level, and saves.
5. Builds the URP shader keep materials in `Assets/NeonSyndicate/Resources/ShaderKeepURP` (see Builds).
6. Says what it did. Running it again only fills in what is missing.

URP itself may add its global settings asset to the project the first time it becomes active; that is normal.

### Switch back to Built-in

Clears the default pipeline and every quality level's pipeline, removes the URP shader keep materials (so Built-in
builds do not carry URP shaders) and saves. The pipeline asset, the renderer and the URP package stay, so switching
again is quick. To remove URP completely, also remove the Universal RP package in the Package Manager. Running it on a
Built-in project only says there is nothing to change.

### Render pipeline status

Shows the active pipeline, whether the URP package is installed and compiled into the game's scripts, every quality
level's pipeline asset, whether the assets and renderer features exist, whether the keep materials exist, and the
colour space.

**Colour space:** the switch does not change it, because that would change the Built-in look too. URP is made for
Linear (Project Settings > Player > Other Settings > Color Space); the status dialog reminds you if it is Gamma. Both
paths handle either space.

## What runs differently on URP

Everything is still made in code. `RenderPath.IsURP` (true when the active pipeline asset is a URP asset) decides
which path each part takes. URP-only code is compiled only when the URP package is installed: both asmdefs define
`NS_URP` from the package (`versionDefines`) and reference URP's runtime and Core runtime by name (Unity ignores
those references while the package is absent).

### Materials (`Mats.cs`, `MatsUrp.cs`)

| Built-in | URP | Notes |
|---|---|---|
| Standard (colour, smoothness, metallic, normal map, emission) | Universal Render Pipeline/Lit | `Material.color` and `mainTexture` land on `_BaseColor` and `_BaseMap`; `_Glossiness` becomes `_Smoothness`; normal map, emission and metallic keep their names |
| Standard Fade | Lit, Transparent, Alpha blend | as URP's own inspector sets it up |
| Standard Transparent (premultiplied) | Lit, Transparent, Alpha blend, Preserve Specular | `_ALPHAPREMULTIPLY_ON`, blend One / OneMinusSrcAlpha |
| Standard Cutout (weeds) | Lit with Alpha Clipping | |
| Standard with `_METALLICGLOSSMAP` (character fallback) | Lit with `_METALLICSPECGLOSSMAP` | |
| Unlit/Color, Unlit/Texture | Universal Render Pipeline/Unlit | |
| Legacy Particles/Additive | Universal Render Pipeline/Particles/Unlit, additive | tint doubled and alpha times 1.75 to match the legacy shader's doubling |
| Sprites/Default | Sprites/Default (unchanged) | its pass has no LightMode tag, so URP draws it like Built-in; the minimap's `_Color` property blocks keep working |
| NeonSyndicate/Character | NeonSyndicate/Character URP | below |
| Wet road sheen | drives `_Smoothness` instead of `_Glossiness` | |

Materials made on a Standard shader keep the Standard setup on either pipeline (the URP setup only applies to URP
shaders), and every Built-in code path is the same as before.

### Characters (`Resources/NeonCharacterURP.shader`)

A hand-written URP twin of `NeonCharacter.shader`: same properties, same surface (palette albedo, per-cell metal,
smoothness and fabric detail strength, glowing seams, detail normals on the second UV set, rim light, wetness, hit
flash, glow pulse), lit by URP's physically based lighting: main light and additional lights (including Forward+'s
clustered loop and vertex lights), cascaded and screen space shadows, additional light shadows, soft shadow filters,
fog, light probes (per pixel, per vertex or mixed), reflection probes (blending, box projection, atlas), screen space
ambient occlusion and light cookies. Passes: forward (LightMode UniversalForwardOnly, so it also draws if the renderer
is ever set to Deferred), ShadowCaster, DepthOnly, DepthNormals (vertex normals, so fabric detail does not speckle the
ambient occlusion) and MotionVectors (for TAA and motion blur).

SRP Batcher: every material property sits in one UnityPerMaterial buffer shared by all passes. `HumanoidRig` sets
`_HitFlash` and `_Glow` per renderer with a MaterialPropertyBlock; Unity draws such renderers outside the SRP Batcher
with the block's values, so they stay correct, just unbatched (see Follow-ups for the batched alternative).

If the shader is missing or unsupported, characters fall back to URP Lit with the same palette maps.

### Post-processing (`NeonUrpPost.cs`, `UrpGradeMapping.cs`, `NeonPostFeature.cs`)

URP never calls `OnRenderImage`, so NeonPost's own chain cannot run. On URP NeonPost only keeps its live reflection
probe and the character rim light, and adds **NeonUrpPost** to the game camera. NeonUrpPost owns a global Volume
(priority 100) with a profile built at runtime and fills it every frame from the same numbers NeonPost uses
(`PostGrade.Compute`, including the Inner World, Frostline and rift weights, and the settings menu). `UrpGradeMapping`
converts them to URP's units:

| NeonPost | URP | How it is matched |
|---|---|---|
| Bloom (soft knee threshold, 4 to 6 mip levels added with weight 0.72) | Bloom, Gaussian filter, same threshold and clamp | URP reads the threshold in gamma; scatter and intensity are least-squares fits of the per-level weights; iterations and high quality filtering by preset |
| ACES filmic tonemap (Narkowicz fit) | Tonemapping ACES | URP's ACES is a different curve: +0.98 EV and 10 percent more saturation match it (grey ramp within one 8-bit step) |
| Exposure, brightness setting, eye adaptation | Color Adjustments post exposure | in EV |
| Contrast around middle grey, saturation | Color Adjustments contrast and saturation | URP pivots contrast higher and crushes near black harder, so contrast is scaled by 0.84 and the gain compensates the pivot |
| Lift, gain, shadow and highlight tints | Lift Gamma Gain plus Shadows Midtones Highlights | tints at black and white go into lift and gain exactly, the midtone band corrects the crossover; colours are pre-desaturated because URP saturates after these steps |
| Vignette after tonemapping | NeonPostFeature's exact vignette | URP's Vignette (before tonemapping, fitted) only stands in if the feature is missing |
| Chromatic aberration | Chromatic Aberration | same red to blue spread |
| Film grain | Film Grain, Thin2 | URP's grain is multiplicative; strength matched in the mid tones |
| Camera motion blur by reprojection | Motion Blur, Camera Only | same blur length and 0.04 cap; quality by preset |
| Ambient occlusion | URP's SSAO renderer feature | on when post effects and ambient occlusion are on, High and Ultra |
| FXAA plus sharpen / MSAA | SMAA (Low, Medium, High quality) or TAA (Ultra, high quality, 0.25 contrast adaptive sharpening) | MSAA is off while URP's post-processing runs |

The camera also gets: post-processing on or off with the **Post effects** setting (it turns the whole stack off, as
on Built-in; with it off the preset's MSAA comes back so edges stay smooth), dithering, HDR, a volume stack that updates
every frame, and a TAA and motion vector history reset on every camera cut (`PlayerCamera.CutHistory` reaches it
through NeonPost).

**NeonPostFeature** (a URP renderer feature, both passes in the Render Graph API that URP 17 requires):

- **Eye adaptation.** Before post-processing, a pass draws NeonPost's luminance measurement (log2 luminance, four
  taps per texel, centre weighted) of the HDR scene into a 64 x 64 half-float target, builds its mip chain and reads
  the 1 x 1 level back with an asynchronous GPU readback. NeonUrpPost eases the exposure toward it with NeonPost's
  rates (2.6 brighter, 1.1 darker), key and limits, and snaps after camera cuts. The reading arrives a frame or two
  late, which an adaptation over seconds does not show. Off when the device has no asynchronous readback (the exposure
  then stays fixed, as with the setting off).
- **Exact vignette.** After URP's tonemapping, grading and grain, a pass multiplies NeonPost's vignette onto the image
  in place. URP renders the post-processed image into an intermediate texture in linear colour when a pass follows it,
  so the multiply is NeonPost's formula exactly (with a gamma correction in a Gamma colour space project).

Both passes only run for the camera that carries NeonUrpPost, and only with post-processing on.

### Graphics presets (`UrpGraphics.cs`, hooked from `Settings.ApplyGraphics`)

At runtime the preset is mapped onto the active URP asset from the same quality settings the Built-in path sets:

| | Low | Medium | High | Ultra |
|---|---|---|---|---|
| Shadow distance (m) | 45 | 80 | 130 | 190 |
| Cascades (Unity's split values) | 1 | 2 | 4 | 4 |
| Main light shadow map | 1024 | 1024 | 4096 | 4096 |
| Additional light shadow atlas | 1024 | 1024 | 2048 | 2048 |
| Render scale | 0.85 | 1 | 1 | 1 |
| Per-object light limit (Forward only) | 2 | 4 | 8 | 8 |
| Sun soft shadow filter | low | medium | high | high |
| Anti-aliasing | SMAA low | SMAA medium | SMAA high | TAA |
| Motion blur quality | (off) | low | medium | high |

Always: HDR on, Low Dynamic Range grading, MSAA off while post-processing runs, SSAO on at High and Ultra (when its
setting is on). The asset's own values are saved the first time and put back when the game quits, which in the editor
is when Play mode ends, so playing never permanently edits the asset (renderer feature on and off states too).

### Other

- **Minimap without fog.** The minimap camera's `NoFogCamera` relies on Built-in camera messages URP does not send;
  `RenderPath` turns fog off and on around that camera with URP's camera callbacks instead.
- **Reflections and rim light** keep working as on Built-in (NeonPost.Update runs on both pipelines).

### Builds (shader keep materials)

Every material is made in code, so a build only contains the URP keyword variants some asset uses. Switch to URP makes
small materials in `Resources/ShaderKeepURP` with the same `Mats` helpers the game uses: URP Lit opaque, Fade and
Transparent with and without normal maps and emission, the character fallback, alpha clipping, Unlit and the additive
particles, plus a tiny texture they reference (URP recomputes keywords from a material's maps and flags when it
imports it). A build step recreates them if they are missing while URP is active and warns if the scripts were
compiled without URP. The two URP shaders live in Resources, so they always ship.

## What was verified

All in the build environment (no Unity editor, no GPU). Exact commands:

1. **Built-in configurations unchanged and clean:**
   `/home/claude/tools/check_at.sh /home/claude/agents/urp/NeonSyndicate_LasVeyra /home/claude/agents/urp/build`
   Runtime (editor and Windows player, with the Input Manager, with Both, with the Input System only) and the editor
   scripts: zero errors, zero warnings in all six.
2. **URP configurations clean:** `/home/claude/agents/urp/check_urp.sh` runs the six above, then the same six with
   `NS_URP` defined against reference assemblies built from the real URP 17.3.0 and SRP Core 17.3.0 sources (Unity's
   Graphics repository, 6000.3 branch); only the two assemblies the asmdefs name are passed, as Unity does. Zero errors,
   zero warnings in all twelve. How the references were built, and every deviation from a real editor, is written up
   next to them (`/home/claude/agents/urp/refs/README.md`).
3. **Shaders:** `python3 -I /home/claude/agents/urp/work/hlsl/check_urp_shaders.py` on both URP shaders. Method: like
   Unity's compiler, it puts the SubShader's HLSLINCLUDE in front of each pass, defines the platform (D3D11 and
   Vulkan, desktop), the stage and a keyword set, and compiles the vertex and fragment entry points to SPIR-V with
   glslangValidator, with the real URP and Core ShaderLibrary as the include root. Keyword sets: no keywords; the main
   variant on the asset the switch creates (cascaded soft sun shadows, Forward+, additional light shadows, probe
   blending, box projection and atlas, SSAO, exponential squared fog); and three more mixes (vertex lights and per
   vertex light probes, screen space shadows and mixed probes, light cookies, instancing, other fog modes and soft
   shadow levels). Every pass of both shaders: 56 compiles, all clean. glslang is not Unity's compiler, so the
   library copy it reads gets three documented adaptations: `#include_with_pragmas` read as `#include`, and the
   `min16float` and `half` twins of float functions dropped, because glslang treats both types as float and reports the
   pairs as duplicates (FXC and DXC accept them). A deliberate typo and a bad include were caught, so the check bites.
4. **Grade mapping:** an offline harness compiles `PostGrade.cs`, `Settings.cs`, `RenderPath.cs` and
   `UrpGradeMapping.cs` from the project and compares ports of NeonPost's composite and of URP's uber pass (ACES,
   LDR LUT, vignette, grain, chromatic aberration, motion blur): `dotnet run -c Release -- verify` in
   `/home/claude/agents/urp/work/harness`. Result (8-bit sRGB steps, rms / worst channel, High preset, no bloom):

   | Mood | Greys | Colours | Greys over the whole screen (exact vignette) | Same with URP's vignette instead |
   |---|---|---|---|---|
   | Day | 0.89 / 3.1 | 5.80 / 59.7 | 1.24 / 4.1 | 9.31 / 31.5 |
   | Dusk | 0.99 / 3.4 | 6.03 / 60.1 | 1.03 / 3.4 | 12.52 / 38.4 |
   | Night | 1.23 / 4.3 | 6.32 / 60.4 | 1.96 / 6.4 | 15.68 / 48.6 |
   | Frostline day | 0.88 / 3.1 | 4.96 / 50.5 | 2.09 / 8.9 | 9.00 / 31.5 |
   | Inner World | 3.78 / 12.3 | 8.43 / 75.2 | 4.52 / 12.3 | 22.78 / 77.6 |
   | Rift at night | 1.11 / 3.4 | 6.46 / 60.5 | 1.80 / 6.3 | 24.24 / 76.9 |

   Also: parameters survive URP's own conversions (maximum error 2e-7), chromatic spread 0.0240 on both, motion blur
   length 0.0090 and cap 0.0400 on both. Without the compensation the grey ramp is off by 26 steps rms.

## What is not tested

Everything that needs Unity or a GPU:

- the menu flow: the package install, the resume after the reload, the asset creation through `SerializedObject`,
  the assignment and the dialogs;
- every pixel: lighting, shadows, materials, the character shader, the volume values, the Render Graph passes, the
  readback, the exact vignette;
- frame rate, memory and GPU cost of Forward+, SSAO, SMAA and TAA, the 64-bit colour buffer and the extra copy the
  vignette pass causes;
- builds with URP (shader stripping, the keep materials);
- XR, consoles, mobile and the Deferred renderer.

## Known differences

- **Bright saturated colours** differ more than greys (about 6 steps rms, up to about 60 on the most saturated): URP's
  ACES handles hue and gamut differently and no grade setting can bend that back.
- **Inner World** greys are about 4 steps rms off (its harder contrast and strong tints sit at the edge of what the
  URP controls can match).
- **Additive particles** cannot clamp their alpha like the legacy shader, so the softest edges of glows differ slightly.
- **Transparent surfaces** cast solid shadows where Standard dithered them.
- **Camera motion blur** cannot keep near objects (the player) sharp the way NeonPost's depth fade does.
- **Grain** is multiplicative, so there is none in pure black.
- **Low preset shadows** use URP's cheapest soft filter instead of hard shadows (URP's per-light setting has no hard
  option and the global switch is not public API).
- **Sun shadow bias**: URP uses the pipeline asset's bias, not the Light's Built-in bias values.
- **The procedural skybox's sun disk** comes from engine values URP may set differently; check it.
- **The settings menu text** ("Ultra adds 8x MSAA") describes the Built-in path.

## Checklist for the first URP session

1. **Pink or magenta surfaces.** Switch, press Play, drive around the city, the Frostline and the Inner World. Any
   magenta object means a shader URP cannot draw: note what it is. The Console names the shader.
2. **Lighting match.** Same place, same time of day, both pipelines: sun direction and strength, shadow softness and
   distance, neon signs and windows at night, street lights, car headlights, the wet road sheen, reflections on cars.
3. **Post match.** Bloom on neon, the night grade (deep blues, warm highlights), Inner World violet shadows and lens
   fringing, the rift pulse, the vignette, grain, motion blur when turning the car, eye adaptation stepping from a dark
   alley into a lit plaza. Toggle each effect in the settings menu on both pipelines.
4. **Characters.** Seams and eyes glow, the rim light changes from warm by day to blue at night, hit flash, wet look in
   rain, shadows cast and received, no shimmering detail.
5. **Frame rate.** Each preset on both pipelines (Settings > Show FPS), same spot.
6. **Golden screenshots.** Pick five to ten fixed views (title flyover, a downtown street at noon and at night, the
   Frostline, the Inner World, a fight), take a screenshot on Built-in and on URP (the phone's camera saves PNGs to the
   Photos folder: Neon Syndicate > Open Save and Photo Folder), compare side by side. The numbers to tune are the constants at the top of `UrpGradeMapping.cs`.
7. **Switch back** to Built-in and confirm the game looks exactly as before.
8. **A Windows build** on URP, run it, check for missing effects or magenta.

## Later: the Unity 6.7 features

The URP side is organised so each one is a local change. These notes come from the URP 17.3 sources and the roadmap;
the 6.7 APIs themselves were not available to check.

- **Ground truth ambient occlusion:** would replace the SSAO feature on `NeonURP_Renderer` and be switched in
  `UrpGraphics` by the same rule (ambient occlusion setting, High and Ultra). The character shader already writes
  depth and normals.
- **Screen space reflections:** another renderer feature or volume override fed by the depth, normals and opaque
  textures this setup already produces. Wet streets and car paint would gain the most; NeonPost's live reflection probe
  stays as the fallback beyond the screen.
- **AgX tonemapping:** a different tone curve. Either switch both pipelines to AgX for a deliberate new look, or refit
  the constants in `UrpGradeMapping.Tuning` with the harness's tone fit against AgX instead of ACES.
- **DLSS 4.5 upscaling:** URP 17.3 already carries an upscaler framework with a DLSS upscaler behind
  `ENABLE_UPSCALER_FRAMEWORK` and the NVIDIA module. It would become a preset choice in `UrpGraphics` (render scale
  below 1 with the upscaler as the asset's upscaling filter). A temporal upscaler takes TAA's place, so NeonUrpPost
  would stop choosing TAA when one is active; the eye adaptation pass already follows dynamic resolution.
- **Surface Cache GI:** URP 17.3 has an experimental `SurfaceCacheGlobalIlluminationRendererFeature` (behind the
  `SURFACE_CACHE` define). It would be added to the renderer for Ultra. For the neon to light the city, the game's
  emissive materials would need their global illumination flag set to realtime emissive (today `Mats.SetEmission` sets
  none), a one-line change on the URP path.

## Follow-ups outside this change (optional)

- `Game.ResetStatics` could call `RenderPath.ResetStatics();` next to `PostGrade.ResetStatics();`. Not required: the
  method already runs on its own at startup (`RuntimeInitializeOnLoadMethod`).
- `HumanoidRig` could pass hit flash and glow through `SkinnedMeshRenderer.SetShaderUserValue` (one 32-bit value per
  renderer, available in Unity 6.3; the two values would be packed into it) instead of a MaterialPropertyBlock, so
  characters stay SRP batched; the URP character shader would then unpack the user value.
- The README's line about pink or magenta objects still says not to convert to URP. Suggested wording: "Pink or
  magenta objects: on the default Built-in pipeline run Neon Syndicate > Repair Project Setup. To try URP use Neon
  Syndicate > Graphics > Switch to URP (beta); Switch back to Built-in undoes it (Docs/URP_MIGRATION.md)."

## Files

Runtime (`Assets/NeonSyndicate/Scripts/Runtime/Core`): `RenderPath.cs` (pipeline detection, camera hooks),
`MatsUrp.cs` (URP material setup), `UrpGradeMapping.cs` (grade conversion, pure math), and only with URP installed
`NeonUrpPost.cs`, `NeonPostFeature.cs`, `NeonEyeAdaptation.cs`, `UrpGraphics.cs`. Small hooks in `Mats.cs`,
`NeonPost.cs` and `Settings.cs`, and the two asmdefs. Shaders: `Assets/NeonSyndicate/Resources/NeonCharacterURP.shader`
and `NeonPostURP.shader`. Editor: `Assets/NeonSyndicate/Scripts/Editor/NeonSyndicateUrp.cs`.
