# Visual upgrade: one-page summary

Branch `feature/underheart-bonds-04e`. One commit per phase so any phase reverts alone:

| Phase | Commit | What changed | Doc |
|---|---|---|---|
| 0 Audit | `e66ea56` | Pipeline decision (stay on Built-in, no HDRP), inventory, what cannot be done from the cloud | `PHASE0_AUDIT.md` |
| 1 Lighting | `75ff2f6` | Physical sun colour and intensity, cascades per preset, sky-model ambient with real ground bounce, per-area realtime reflection probes, physically based procedural sky, sun and moon shafts, height fog, Underheart lamp cones | `PHASE1_LIGHTING.md` |
| 2 Materials | `4c1d8c3` | Full PBR on every material family (normal, occlusion, smoothness variation, wear), detail maps on large flat surfaces, facade relief, car clearcoat, skin and cloth and hair shading, foliage translucency, URP mapping | `PHASE2_MATERIALS.md` |
| 3 Post | `7658f4a` | ACES refit, multi-scale bloom, cinematic grade, light vignette and grain, optional chromatic aberration, bokeh depth of field (menus and cutscenes only), per-object motion blur, TAA, horizon-based AO, FXAA quality preset | `PHASE3_POST.md` |
| 4 Geometry | `99126aa` | Smooth normals, seeded decals and clutter (about 25,600 pieces, collider-less, keep-outs), ambient VFX (dust, haze, embers, spores, snow, steam, flicker), impact polish, candidate list for modeled assets | `PHASE4_GEOMETRY.md` |
| 5 Camera | `f94a604` | Physical camera with the same perspective, title and pause motion, opt-in cinematic breath, creature cancel blends | `PHASE5_CAMERA.md` |

Gates after every phase: all twelve compile configurations at 0 errors and the pre-existing 63 warnings; the offline
EditMode suite with no new failures (1169 before, 1261 after; the same 25 engine-only `Resources.Load` tests fail at
every revision; 53 skipped). Metadata and the dash scan clean at every phase. **Nothing was run in Unity or on a GPU.**

## What each preset pays

- **Low**: nothing new. Every old material, light, post stage and dressing count stays exactly as it was.
- **Medium**: physical sun, 2 cascades at 80 m, procedural sky, one camera reflection probe, 256 normal maps and
  detail maps, 5-mip bloom, FXAA quality, light decals and clutter, a few ambient emitters, camera motion blur.
- **High**: 4 cascades at 150 m, sky ambient with bounce, nearest 4 probes at 128, 512 maps, TAA, AO, per-object
  motion blur, sun shafts and height fog, full decals and clutter, 14 emitters, sign flicker.
- **Ultra**: 260 m shadows, 8 probes at 256, 1024 maps on hero surfaces, stronger AO, denser dressing, 22 emitters.

## Frame rate

Not measured: no GPU in the build environment. Run Neon Syndicate > QA > Run Perf Flythrough on the "before" build
and on this head at the same preset, and compare the two `.txt` summaries. If a preset drops below your target, the
cheapest things to step back, in order: AO to half strength, TAA off (MSAA returns), probes to Medium, clutter tier down
one, shafts off. Every one is a switch in Settings or on the F2 pages Lighting (47) and Dressing (56).

## Before and after

Your golden capture from 2026-10-17 is the "before" set. After pulling this head: QA > Capture Golden Shots at the same
1920x1080 Game view, then QA > Compare Golden Shots, pick the old folder then the new one; `report.html` shows before,
after and a heatmap for all 28 views. The three headline views: `core_street`, `strip_street`, `frostline_outpost`.

## What remains

- **Asset replacements awaiting your approval**: the candidate table in `PHASE4_GEOMETRY.md` section 3 (street lamps,
  benches, facade kits, trees and palms, breakable furniture, vehicles, then people and creatures). Nothing was
  replaced or deleted. Note the project rule against commercial asset products: replacements would be commissioned or
  built originals.
- **Not possible on Built-in**: screen-space reflections, DLSS, hardware ray tracing.
- **Cut for risk**: lamp light shafts in screen space (sun and moon only); baked GI (nothing can bake in a city that is
  generated at runtime; the bounce is an approximation and says so).
- **Unfinished by design**: plaster in the 04C venues keeps its flat look (one call to add pores); foliage wind sway
  needs the foliage material to read a sway weight; a true refraction pass for heat haze.
- **First things to judge in Unity**: the ambient is about half as bright as before (darker, bluer shade; one knob,
  `SkyMath.UrbanBounceGain`), TAA ghosting on people and the first-person weapon, boot time at High (texture
  generation runs on worker threads during the city build), and whether Ultra clutter density is too busy.
