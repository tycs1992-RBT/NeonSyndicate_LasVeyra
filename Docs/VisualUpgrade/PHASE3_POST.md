# Phase 3: post-processing (one cinematic grade)

Built-in Render Pipeline, the camera's `NeonPost` image pipeline. Everything here was compiled for all twelve
configurations and checked by offline tests and an offline shader syntax check; nothing in this phase has been run in
Unity or on a GPU. Section 7 lists exactly what to look at.

## 1. What the frame does now, in order

1. Volumetrics (Phase 1's `NeonPost.Volumetrics.cs`; this phase ships a pass-through stub with the same file name).
2. Ambient occlusion, half resolution, horizon based (High and Ultra). With temporal anti-aliasing on, it is multiplied
   into the image before the temporal resolve so the resolve also settles its sampling noise.
3. Temporal anti-aliasing (High and Ultra, new "Temporal AA" setting).
4. Depth of field (only the title flyover, cinematic shots and the pause menu background; new "Depth of field" setting).
5. Bloom from the settled image: prefilter with firefly clamp and soft knee, five to eight mips, tent upsample.
6. Eye adaptation measured on the settled image.
7. One composite: chromatic aberration (new setting, off by default), sharpen, motion blur (camera reprojection on
   Medium, per-object motion vectors on High and Ultra), occlusion (when no temporal pass applied it), bloom, exposure
   and adaptation, the fitted ACES curve with one stop of pre-exposure, saturation, contrast, lift and gain, split tone,
   vignette, luminance-weighted grain, dither.
8. FXAA (quality-39 style) only where neither multisampling nor temporal anti-aliasing runs.

The HUD, menus and phone are IMGUI drawn after `OnRenderImage`, so no effect here (grain, blur, depth of field,
temporal history) ever touches them. During QA captures (`QaTools.Busy`) depth of field is off and film grain is
already off; the eye adaptation state is re-measured instantly after a capture because `QaSession.End` calls
`PlayerCamera.CutHistory`, which resets it.

## 2. Decisions on the directive's items

| Item | Shipped |
|---|---|
| 1 Tonemapping | ACES, now the fitted reference curve (RRT and ODT with the sRGB input and output matrices) in place of the older luminance-only fit. `PostGrade.AcesPreExposure` (2.0, one stop) keeps middle grey at the display value the game was graded around (0.267 display linear), so overall brightness is unchanged while highlights roll off and saturated colours desaturate like film. Eye adaptation feeds the curve in EV with the target, limits and strength from `PostGrade.Values` (night may open up to 2.4 EV, 3.0 EV underground, before the 0.75 strength; day may close by 1.2 EV). Tests pin middle grey, monotonicity, "scene white stays below display white at noon" and "a 0.02 cellar wall at full night adaptation stays above 0.05". |
| 2 Bloom | 5 (Low), 6 (Medium), 7 (High), 8 (Ultra) mips from half resolution, tent upsample, 0.72 falloff per level, threshold 1.05 by day to 0.9 by night in linear scene units (0.8 in the Veil), soft knee at half the threshold, source clamp at 18 and a luminance-weighted four-tap prefilter against fireflies. The threshold now lives in `PostGrade.Values.BloomThreshold` for both pipelines. |
| 3 Grading | In `PostGrade.Compute`: lift raised to about 0.012 (0.008 to 0.02 at night, cooler), contrast 1.04 day to 1.07 night, saturation 1.06 to 1.10, split tone cool shadows (blue-leaning `ShadowTint`) and warm highlights (`HighlightTint`), day and night, with the Frostline, Inner World and rift weights bending the same fields as before. URP gets identical values through `UrpGradeMapping` (refitted, section 5). |
| 4 Lens | Vignette 0.14 day to 0.20 night (was 0.22 to 0.34), 0.30 in the Veil, 0.36 at a rift. Chromatic aberration behind the new `Settings.ChromaticAberration` toggle, default off; on, it is 0.003 (barely perceptible) on Medium and up plus the existing Veil and rift fringing. Film grain 0.012 day to 0.02 night (was 0.018 to 0.03), weighted by luminance in the shader (strongest in shadows, almost none in highlights); never on the HUD. |
| 5 Depth of field | Shipped, Built-in: hexagonal bokeh at half resolution (three one-sided gathers, taps weighted by their own circle of confusion so foreground spills and backgrounds do not leak over sharp foreground), full resolution composite. Driven by `PlayerCamera.FocusDistance` (distance to what the flyover or the cinematic shot looks at) with a range of 0.5 to 0.6 of the focus distance to full blur, 10 px radius at 1080p (scales with height); behind the pause menu focus is the player (`DistanceToPivot`), 7 px. Never on gameplay cameras, never in first person, never during QA. Medium and up, `Settings.DepthOfField`. |
| 6 Motion blur | Camera reprojection kept on Medium at its previous low strength (0.45, capped at 0.04 uv, the player sharp by distance). Per-object motion blur shipped on High and Ultra from `DepthTextureMode.MotionVectors` (`_CameraMotionVectorsTexture`, which Unity fills for skinned meshes and per-object motion in Built-in): 8 samples (12 on Ultra) along the pixel's own vector with reconstruction weights (depth order and velocity cones), so a static background does not smear into a moving object and the HUD is untouched. No tile-max pass, so blur never extends outside the moving object's own footprint: that is the "low intensity" asked for. The same strength and cap as before. |
| 7 Anti-aliasing | Shipped: temporal anti-aliasing in Built-in on High and Ultra (`Settings.TemporalAA`, default on), with MSAA kept as the presets set it (4x High, 8x Ultra), plus a post-resolve sharpen (0.24 High, 0.30 Ultra). Projection jitter (Halton 2,3, eight phases) between `OnPreCull` and `OnPostRender` only, so aim rays and screen-to-world see the clean matrix; motion vectors from Unity's pass; 3x3 closest-depth velocity dilation; Catmull-Rom history fetch; variance clipping in YCoCg with a tighter box in motion; feedback 0.92 at rest to 0.72 at 40 px per frame, none for off-screen history; luminance-weighted blend. The first-person weapon writes its own motion vectors (it is a mesh renderer under the camera) so it does not ghost; the HUD is excluded by construction. FXAA upgraded to a quality-39 style edge search (12 steps, sub-pixel blending) for the no-MSAA paths (Low) and alongside 2x MSAA on Medium. With Temporal AA off, High and Ultra fall back to MSAA plus the previous sharpen. |
| 8 Ambient occlusion | Horizon based (ground truth per-slice integral over normals reconstructed from depth): High 2 slices of 6 steps per side over 0.6 m, Ultra 3 slices of 8 steps over 0.8 m with a stronger falloff exponent; radius in world metres projected per pixel and capped at 32 (48 Ultra) half-res pixels; half resolution, depth-aware blur, depth-weighted upsample in the apply; the direction set rotates through the jitter cycle when TAA is on, which the resolve averages (temporal stabilisation). Fades out past 180 m as before. |
| 9 Upscaling | Not available: DLSS, FSR 2 and XeSS need a scriptable render pipeline (URP or HDRP) in Unity; the Built-in pipeline has no hook for them. Nothing shipped; no claim made. |
| 10 QA | Depth of field is off while `QaTools.Busy`; film grain was already off; chromatic aberration is off by default; the capture's 45 settle frames cover the eight-frame jitter cycle several times, so temporal anti-aliasing and the rotating occlusion directions converge before each shot (a converged TAA image is a supersampled average and differs between shots by well under a code value). Eye adaptation re-measures instantly after a capture through `CutHistory`. |

## 3. Files

| File | Change |
|---|---|
| `Scripts/Runtime/Core/NeonPost.cs` | Rewritten as `public sealed partial class NeonPost`. Stage order above; `OnPreCull` asks for depth and motion vectors only when an effect reads them and applies the jitter; `OnPostRender` clears it; `UpdateUnderground` feeds `PostGrade.Underground`; adaptation reads its numbers from `PostGrade.Values`; the probe and rim light are unchanged. |
| `Scripts/Runtime/Core/NeonPost.Taa.cs` (new) | Jitter, history buffers (two persistent HDR textures, re-created on resize), the resolve, `ReleaseHistory`. |
| `Scripts/Runtime/Core/NeonPost.Ao.cs` (new) | The occlusion pass and blur, parameters per preset from `PostMath`. |
| `Scripts/Runtime/Core/NeonPost.Bloom.cs` (new) | The mip chain (was inline in NeonPost.cs). |
| `Scripts/Runtime/Core/NeonPost.Dof.cs` (new) | `DepthOfFieldRequest` (static, shared with URP), the four depth of field passes. |
| `Scripts/Runtime/Core/NeonPost.Volumetrics.cs` (new, stub) | `internal RenderTexture ApplyVolumetrics(RenderTexture src)` returning `src`. Phase 1 replaces this file. The call sits first in the chain (before AO and TAA, so the temporal filter can smooth a ray march) and the returned texture is never released by the caller. |
| `Scripts/Runtime/Core/PostMath.cs` (new, pure) | Halton jitter, projection offset, feedback curve, circle of confusion, bloom levels and energy, occlusion presets, depth of field radius, motion blur scale. |
| `Scripts/Runtime/Core/PostGrade.cs` | New `Underground` weight, `AcesPreExposure`, `Values.BloomThreshold`, `AdaptKey`, `AdaptMinEv`, `AdaptMaxEv`, `AdaptStrength`; the cinematic grade numbers; `AcesFitted` and `AcesFittedGrey` (the shader's curve in managed code, for tests and offline fits). |
| `Scripts/Runtime/Core/UrpGradeMapping.cs` | Threshold from `Values`, levels from `PostMath`, bloom weights for 7 and 8 levels, refitted `Tuning` (section 5). |
| `Scripts/Runtime/Core/NeonUrpPost.cs` (URP beta, `NS_URP`) | Adaptation numbers from `Values`; TAA on High and Ultra when the setting is on (SMAA otherwise); camera-and-objects motion blur on High and Ultra; a `DepthOfField` (bokeh, 50 mm, f/2.8 or f/4 behind the menu) obeying the same request. The URP bokeh kernel is URP's own, so it approximates the Built-in look rather than matching it. |
| `Scripts/Runtime/Core/NeonPostFeature.cs`, `Resources/NeonPostURP.shader` | Unchanged (the luminance measure and the exact vignette still match). |
| `Resources/NeonPost.shader` | Passes 0 to 8 kept in place (prefilter, down, up, AO, AO blur, composite, FXAA, luminance, adaptation), passes 9 to 14 added (TAA, apply AO, DOF prefilter, DOF gather, DOF hexagon, DOF composite). ACES replaced by the fitted RRT and ODT curve with matrices; composite reads the pre-exposure, the motion blur mode and the depth-weighted occlusion upsample. |
| `Scripts/Runtime/Core/Settings.cs` | Appended `TemporalAA` (default on), `DepthOfField` (on), `ChromaticAberration` (off), with their load and save lines (`NSLV_taa`, `NSLV_dof`, `NSLV_chroma`). `ApplyGraphics` untouched. |
| `Scripts/Runtime/UI/MenuSystem.cs` | Graphics page: "Temporal AA" on the second toggle row, a third row with "Depth of field" and "Chromatic aberration" (same `Toggle` helper, so pad focus and navigation work as for the others); the preset blurb rewritten. |
| `Scripts/Runtime/Characters/PlayerCamera.cs` | Read-only additions only: `InFlyover`, `FocusDistance` (recorded in `UpdateFlyover` and `UpdateCinematic`). No behaviour change. |
| `Tests/EditMode/PostGradeTests.cs` | Extended for the new fields, the chromatic toggle, the split tone, the underground headroom, the tone curve. |
| `Tests/EditMode/PostMathTests.cs`, `Tests/EditMode/UrpGradeMappingTests.cs` (new) | 10 and 5 tests. |

## 4. What each preset gets

| | Low | Medium | High | Ultra |
|---|---|---|---|---|
| Bloom mips | 5 | 6 | 7 | 8 |
| Anti-aliasing | FXAA (quality 39 style) | 2x MSAA + FXAA | 4x MSAA + TAA (or 4x MSAA + sharpen with TAA off) | 8x MSAA + TAA (same fallback) |
| Ambient occlusion | off | off | horizon based, 2 x 6, 0.6 m | horizon based, 3 x 8, 0.8 m, stronger |
| Motion blur | off | camera reprojection | per-object, 8 samples | per-object, 12 samples |
| Depth of field (menu, flyover, cinematics) | off | on | on | on |
| Chromatic aberration (setting on) | off | 0.003 | 0.003 | 0.003 |
| Film grain, vignette, split tone, adaptation | all | all | all | all |

Low pays for nothing new. Medium's additions are the extra bloom mip, the better FXAA and the depth of field outside
play. The motion vector pass (High and Ultra) is one extra pass over moving renderers plus a full-screen camera pass,
shared by TAA and the object motion blur.

## 5. Pipelines in step (offline harness)

`/home/claude/agents/urp/work/harness` (a copy pointed at this sandbox, `dotnet run -c Release -- fit`,
`fitgrade`, `verify`) compares a port of the new Built-in composite with a port of URP's uber pass. Refit:
exposure offset 0.96 EV (the pre-exposure plus a residual, was 0.98 against the old curve), saturation factor 1.00
(was 1.10), contrast scale 0.94 (was 0.84), mid-tone tint scale 0.50 (was 0.80), vignette smoothness 0.23; bloom
weights 7 levels scatter 0.678, gain 3.101; 8 levels 0.694, 3.216 (4 to 6 reproduce the earlier fit exactly).
Result, 8-bit sRGB rms / worst channel, High preset, no bloom:

| Mood | Greys | Colours | Greys over the screen (exact vignette) |
|---|---|---|---|
| Day | 1.27 / 5.0 | 1.53 / 15.4 | 1.62 / 5.0 |
| Dusk | 0.73 / 2.6 | 1.23 / 15.1 | 0.91 / 2.6 |
| Night | 0.82 / 3.3 | 1.35 / 14.7 | 1.30 / 4.0 |
| Frostline day | 1.40 / 5.7 | 1.52 / 8.7 | 2.42 / 8.9 |
| Inner World | 3.78 / 12.3 | 4.07 / 25.5 | 4.66 / 12.3 |
| Rift at night | 0.70 / 2.0 | 1.14 / 15.2 | 1.17 / 3.9 |

Colours now agree to about 1.5 steps rms (they were 5 to 8 before, because the old luminance fit and URP's reference
transform handled saturation differently). The table in `Docs/URP_MIGRATION.md` describes the old fit and is now stale
(that file is outside this phase's ownership; it needs the numbers above).

## 6. What could not be done, and open items

- Nothing here was seen on a GPU. The shader passed an offline syntax check of all 15 passes (vertex and fragment,
  linear and gamma) with glslang against a stand-in of UnityCG; that proves the HLSL parses, not that it looks right.
- Upscaling (item 9): impossible on the Built-in pipeline, see the table.
- Volumetrics: the stub's contract is the one agreed (same file name, `ApplyVolumetrics(src)` returns the image to
  continue with, the caller never releases it). The call is placed before AO and TAA rather than immediately before
  bloom, on purpose, so the temporal filter can settle a noisy ray march; if Phase 1 needs it after TAA, move the one
  line in `NeonPost.OnRenderImage`.
- Motion vectors for the procedural character shader rely on Unity's internal motion vector pass for renderers whose
  shader has none of its own (that is how Built-in works for skinned meshes); if ghosting appears on people in Unity,
  the fix is a `MotionVectors` pass in `NeonCharacter.shader` (a Phase 2 file).
- URP: depth of field uses URP's bokeh with a fixed 50 mm lens and f/2.8 (f/4 behind the menu), an approximation;
  `MotionBlurMode.CameraAndObjects` compiles against the URP 17.3 reference assemblies but was not run.
- `Docs/URP_MIGRATION.md` harness table is stale (section 5). `Docs/STATUS_AND_TESTING.md` should gain the checklist
  below; both are outside this phase's file list.

## 7. What Tyler must look at in Unity

Capture Golden Shots before pulling this phase (same Game view size, for example 1920x1080), then after, and run
Compare Golden Shots. Expected: a global "big" change on every view (new tone curve, lifted blacks, split tone,
lighter vignette), so judge the images, not the heatmap totals, for this phase.

1. `core_street` at 22:00 and `strip_street` at 22:00: neon signs should glow wide and soft with no halo on plain
   lit walls, and bright saturated signs should roll toward white in their cores instead of clipping to flat colour.
   Shadows under awnings slightly blue, lit faces slightly warm. No black crush in doorways.
2. `city_aerial` at 12:00 and `glass_heights` at 12:00: the sky and sunlit facades must not blow out; middle tones
   should look the same brightness as before this phase.
3. `veil_core_street` at 22:00: violet shadows and crimson highlights still read as the Veil; the vignette is lighter
   than before; no colour fringing unless "Chromatic aberration" is switched on.
4. `frostline_outpost` at 12:00: cool, slightly desaturated snow, no magenta cast.
5. In play on High: stand still and look at thin railings, wires and tower edges (temporal anti-aliasing should remove
   crawling); then spin the camera fast: no smearing of the HUD, no double image of the first-person weapon (press V
   to first person), no ghost trail behind a passing hover car. Toggle "Temporal AA" off in Settings > Graphics and
   compare the same spot.
6. In play on High: walk past a parked car and a doorway and look at the contact shadow where the car meets the road
   and in the door reveal (ambient occlusion); there should be no dark halo floating off the silhouette and no
   flicker when you strafe. Compare with "Ambient occlusion" off.
7. Drive a hover bike on High: the road should blur with speed while the bike and rider stay sharp; a car crossing
   in front should blur along its own motion. On Medium only the camera motion blurs (as before).
8. Title screen: the city flyover should have a soft far distance and soft near towers with the look point sharp;
   bokeh shapes should read as hexagons on bright lamps. Pause the game: the player stays sharp and the city behind
   the menu softens. Toggle "Depth of field" off: both go sharp. Confirm no depth of field ever appears while
   playing, driving or aiming.
9. Night interiors (the Underheart) with "Eye adaptation" on: walk from a lit street into a dark cellar; the picture
   should open up over about two seconds and stay readable without turning grey or noisy; then step back out into
   neon and watch it settle without a flash.
10. Settings > Graphics on a gamepad: D-pad through the new toggles (Temporal AA, Depth of field, Chromatic
    aberration), press A to flip them, Save settings, restart, confirm they persist.
11. Run the Perf flythrough on High before and after and compare the .txt summaries: the motion vector pass, TAA and
    the new occlusion are the three new costs; if p95 moves by more than about 15 percent, turn off Temporal AA first
    and retest to see which of them it is.
