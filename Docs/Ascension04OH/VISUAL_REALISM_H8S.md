# Visual realism H8S: what changed and how to tune it

Goal: a grounded, moody gothic look for the Gothic Heart (world revision 10) without switching renderer, following the
owner's canon palette (charcoal and weathered stone, tarnished bronze, amber lanterns, local violet and turquoise
mineral glow). Everything here is presentation only: no collider, layout, save, quest or input change. Nothing was run
in Unity; the code compiles in all twelve configurations and the pure maths is tested offline.

## What you will see (Built-in, Medium and up unless noted)

1. **Light**: the sun no longer passes straight overhead. Its daily circle leans 30 degrees (noon at 60 degrees), so at
   the Free Roam start (15:30) shadows are longer and walls have a lit and a shaded side. Sunrise and sunset are still
   6:00 and 18:00, so night, lamps and creatures keep their timing. The day sun is a touch warmer; sky fill is a little
   lower (dark stone bounces less light than the old pale streets).
2. **Grade** (shared by Built-in and URP through `Core/PostGrade.cs`): lower day exposure and key so sunlit stone is no
   longer pushed into the curve's bright shoulder, saturation 0.9 by day, contrast 1.1, cool charcoal shadows under warm
   bronze highlights, a firmer vignette, bloom only on real emissives. Night is a little deeper; the Veil is a little
   less saturated; the Frostline stays paler than the city.
3. **Haze**: cooler blue-grey haze replaces the sky-blue fog on clear days, denser air (a quarter of the light gone at
   300 m, half at 460 m), and on High and Ultra a taller, denser height fog that swallows the horizon line.
4. **Sky**: a real bug fixed. The project renders in the gamma colour space, and the NeonSky dome wrote linear values,
   so wherever it ran the sky came out a dark navy. It is now encoded like Unity's own skyboxes; its night glow is scaled
   to keep a deep night. If NeonSky is off, Unity's procedural sky gets a deeper, greyer day.
5. **Contact shadows**: ambient occlusion reaches further and darker (High 1.0 m, Ultra 1.25 m), grounding wall bases,
   door reveals, steps and people.
6. **Materials**: the bright striped desert sand is now weathered mineral earth (dust drifts, pebbles, packed cracked
   patches). The gothic kit stone is dressed ashlar with per-block tone, grey mortar, chipped edges, soot and rain
   streaks; paving is worn flagstone with dirty joints and polished centres; trims and kit roofs are tarnished bronze;
   kit crystals are glassy faceted amethyst and turquoise with straight cleavage lines that carry a calm local glow.
   The ancient modules (arches, vaults, columns) lose their purple cast for neutral weathered stone, each placement
   slides its stone pattern so neighbours differ, roofs stay dark slate. Venue stone and paving are darker and
   weathered; old crystal glow is halved. Large surfaces carry a macro weathering layer that breaks visible tiling.

## Where to look in Unity

Free Roam at Veyra Lofts (court floor, Lofts walls and arches, the crystals over the sign), then fly up over the
settlement (ground, roofs, distance haze, horizon), then wait for dusk (warm low sun, long shadows) and night (deep sky,
lanterns, crystals). F2 > Lighting shows the sun elevation and the ambient mode.

## How to dial it down (exact names)

| Too strong? | Change | File |
|---|---|---|
| Too dark overall | `Exposure` day value 0.82 (raise toward 0.98), `AdaptKey` 0.15 (toward 0.2) | `Core/PostGrade.cs` (`Compute`) |
| Too grey | `Saturation` day 0.9 (toward 1.0) | `Core/PostGrade.cs` |
| Too much haze | `DayFogDensity` 0.0018, `DayHazeMix` 0.6, `HeightFogDensity` 0.0014 | `Core/RealismRulesH8S.cs` |
| Shadows too long | `SunPathTiltDegrees` 30 (0 = old overhead sun); `SunPathSide` flips the lean | `Core/RealismRulesH8S.cs` |
| Shade too dark | `DayFillGain` 1.3 (old 1.5) | `Core/RealismRulesH8S.cs` |
| AO too heavy | `AmbientOcclusionSettings` radius and strength | `Core/PostMath.cs` |
| Crystals too bright or dim | `CrystalGlow` 0.85, `LegacyCrystalGlowScale` 0.5 | `Core/RealismMaterialsH8S.cs` |
| Night sky too dark | `NightGlowScale` 0.12 | `Core/RealismRulesH8S.cs` |
| Stone or ground colours | palette constants and the `Ground`, `Ashlar`, `Flagstone`, `Bronze`, `Crystal` generators | `Core/RealismSurfacesH8S.cs` |
| Macro weathering too blotchy | `MacroTiling*` and the `Macro` generator amplitudes | `Core/RealismMaterialsH8S.cs`, `RealismSurfacesH8S.cs` |

Low keeps the old materials (the generated maps are Medium and up).

## Golden screenshot loop

Before pulling this branch: Neon Syndicate > QA > Capture Golden Shots (same Game view size every time, for example
1920x1080). After: capture again, run Compare Golden Shots, and send the report. Big differences are intended here
(noon shots now have a 60 degree sun, new ground and stone); check that nothing is broken (pink materials, black sky,
blown-out night).

## Previews (offline, honest)

- Texture PNGs: produced by the real generator code (`RealismSurfacesH8S`) through an offline harness that links the
  branch's runtime DLL; the "before" maps come from the baseline DLL. They show texture content only, not lighting.
- Grade-only before and after: the owner's three screenshots, with today's grade inverted to scene light and the new
  `PostGrade.Compute` applied through the real C# code (plus a port of the shader's composite). Lighting, haze, sky and
  materials do not appear in these; HUD pixels are regraded too in the preview (in game the HUD is drawn after the grade).

## Changed files

New: `Core/RealismRulesH8S.cs`, `Core/RealismSurfacesH8S.cs`, `Core/RealismTexturesH8S.cs`,
`Core/RealismMaterialsH8S.cs`, `World/Atmosphere.RealismH8S.cs`, `Tests/EditMode/VisualRealismH8STests.cs`.
Edited (small hooks or numbers): `Core/PostGrade.cs`, `Core/PostMath.cs`, `Core/NeonPost.Volumetrics.cs`, `Core/Mats.cs`
(the sand material), `World/Atmosphere.cs` (one line), `World/Atmosphere.Sun.cs`, `World/Atmosphere.Bounce.cs`,
`World/SkyLighting.cs`, `World/SkyGround.cs`, `World/GothicRealmMaterials04OB.cs`, `World/AscensionArtH2.cs`,
`ArtRebirth04A/VisualRebirth04AAssets.cs`, `Resources/NeonSky.shader`, `Tests/EditMode/PostGradeTests.cs` (saturation
floor lowered to 0.88 on purpose). The edited files are outside the `check_ascension_04oh` owner whitelist; guards were
not run in this sprint by owner decision.

## Known limits

- No true grime band at wall bases: the meshes have one UV set and the Standard shader has no world height; it needs a
  second UV channel or a custom world shader (a later pass).
- Per-building colour variation comes from the world-space macro layer and per-placement UV shifts, not separate
  materials (that would multiply draw calls).
- The gamma-space sky and ambient balance was modelled offline against the screenshots; it must be judged in Unity.
