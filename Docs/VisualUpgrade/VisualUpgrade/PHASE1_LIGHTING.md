# Phase 1: Lighting

Built-in Render Pipeline, DX12. Everything here is generated at runtime (no baked data), gated by `Settings.Graphics`
and switchable off one piece at a time. Nothing in gameplay, physics, AI, input, quests or saves changed. Nothing in this
document was seen running: the code compiles in all twelve configurations and the maths is tested offline, but the look,
the feel and the frame cost have to be judged in Unity (checklist at the end).

## What changed, file by file

| File | Change |
|---|---|
| `Scripts/Runtime/World/SkyMath.cs` (new, pure) | The lighting maths: the sun's path for the clock; a single-scattering sky model (Rayleigh and Mie in a spherical atmosphere, sun transmittance through an approximate Chapman function, a multiple-scattering gain of 3); the sun's intensity and colour by elevation from that same transmittance; a Kelvin to linear RGB conversion from the Planckian locus (1667 to 25000 K) and an "apparent Kelvin" readout; the sky's ambient in three bands evaluated in 17 directions; the cloud and ground bounce terms; cascade splits, shadow distance and cascade count per preset. |
| `Scripts/Runtime/World/SkyGround.cs` (new, pure) | Classifies the ground under a map point from the city layout (snow, canal water, desert sand, Frostline rock, road asphalt, park grass, plaza pavers, sidewalk concrete), gives each a linear albedo, and averages a cross of five samples. |
| `Scripts/Runtime/World/Atmosphere.Sun.cs` (new partial) | `ApplySunAndMoon`: sun colour from the filtered solar spectrum (noon about 5150 K, 5 degrees up about 3050 K, at the horizon under 2000 K), intensity from the atmosphere's transmittance (peak 1.45 unchanged), the Frostline cooling and the Veil's low crimson sun exactly as before; the moon follows a 29.5 day phase. `ApplySunQuality`: soft shadows, VeryHigh shadow resolution on High and Ultra, bias per preset. |
| `Scripts/Runtime/World/Atmosphere.Sky.cs` (new partial) | Picks the sky material (NeonSky on Medium and up when `Settings.ProceduralSky`, Unity's procedural skybox otherwise) and feeds it every frame: sun and moon, haze from the weather, clouds, the night glow, the ground bounce colour, the star rotation, the Frostline and Veil weights, lightning. |
| `Scripts/Runtime/World/Atmosphere.Bounce.cs` (new partial) | The bounce approximation (section below): the Trilight ambient from the sky model plus the ground albedo under the camera; Skybox ambient mode on High and Ultra outdoors with `DynamicGI.UpdateEnvironment` throttled to real changes; the environment stamp the probes watch. |
| `Scripts/Runtime/World/Atmosphere.cs` | `Init` calls `ApplySunQuality`, `ChooseSkyMaterial`, creates `ReflectionProbes` and registers the Lighting debug page; `Apply` hands the sun, ambient and sky blocks to the partials, the day fog colour follows the model's horizon band, the environment refresh moved to the bounce partial; `OnGraphicsApplied` and `OnDestroy` added. The weather, frost, Veil, interior and Underheart layers are untouched. |
| `Scripts/Runtime/World/ReflectionProbeRules.cs` (new, pure) | The probe budget per preset, the 15 city areas (nine districts matching `CityLayout.DistrictAt`, the canal, four desert quadrants, the Frostline), Underheart region boxes, venue room boxes, and the nearest-area selection. |
| `Scripts/Runtime/World/ReflectionProbes.cs` (new) | The runtime probe system (section below). |
| `Scripts/Runtime/World/SkyLighting.cs` (new) | The reset hub (`Game.ResetStatics` calls it) and the preset hook (`Settings.ApplyGraphics` calls it). |
| `Scripts/Runtime/World/SkyLightingDebug.cs` (new) | F2 tab "Lighting" (Order 47): sun elevation, Kelvin, strength, ambient mode, ground kind and albedo, Trilight colours, shadow settings, probe list, volumetrics status, and a button per switch. |
| `Scripts/Runtime/Core/Settings.cs` | New switches `PhysicalSun`, `ProceduralSky`, `SkyAmbient`, `AreaReflections`, `VolumetricLight`, `HeightFog` (saved under `NSLV_`). `ApplyGraphics`: shadow distance and cascades from `SkyMath` (Low 45 m and 1, Medium 80 m and 2, High 150 m and 4, Ultra 260 m and 4), `StableFit`, cascade splits for a street camera (High 16, 35, 67 m; Ultra 28, 59, 111 m), shadow resolution Medium on Low, High on Medium, VeryHigh on High and Ultra, near plane offset 2, then `SkyLighting.OnGraphicsApplied()`. |
| `Scripts/Runtime/Core/NeonPost.Volumetrics.cs` (new partial) | `ApplyVolumetrics(src)`: sun or moon light shafts and analytic height fog (section below). A small sibling component releases its material and texture with the camera. |
| `Scripts/Runtime/Core/NeonPost.cs` | Two lines: `partial` on the class, and `src = ApplyVolumetrics(src);` before the bloom pass. |
| `Scripts/Runtime/Core/Game.cs` | One line: `SkyLighting.ResetStatics()`. |
| `Resources/NeonSky.shader` (new) | The sky dome shader (section below). |
| `Resources/NeonShafts.shader` (new) | The shafts and height fog passes. |
| `Scripts/Runtime/World/Underheart/UnderheartBuilder.Shafts.cs` (new partial) | Soft additive light cones under hanging Underheart lamps, capped per cell, switched per preset by `UnderheartLampShafts`. |
| `Scripts/Runtime/World/Underheart/UnderheartBuilder.cs` | One line in `Build`: `Step("lamp shafts", BuildLampShafts);` after the landmarks (outside this phase's file list, reported). |
| `Tests/EditMode/SkyMathTests.cs`, `Tests/EditMode/ReflectionProbeTests.cs` (new) | 21 tests (listed below). |

Shaders in `Resources` ship with every build, so `EnsureShaderKeep` did not need a change.

## 1. The sun

- Colour: `SkyMath.SunColor` filters a 5778 K blackbody through the atmosphere's transmittance at the sun's elevation and
  normalises the result to luminance 1, so the light that falls on the city has the same colour as the sun disc the
  dome draws. Apparent temperature by elevation: 90 deg 5150 K, 45 deg 4950 K, 20 deg 4400 K, 10 deg 3750 K, 5 deg
  3050 K, 2 deg 2300 K. The Frostline still cools it (45 percent toward snow white) and the Veil still replaces it with
  the crimson sun.
- Intensity: `SkyMath.SunIntensity` is the peak (1.45, unchanged) times how much light gets through relative to a sun
  overhead, fading in over the last degree above the horizon: 60 deg 1.42, 30 deg 1.29, 10 deg 0.87, 5 deg 0.55,
  2 deg 0.22. Clouds still take up to 55 percent. The old `day` ramp reached full strength at 16 degrees; the new sun
  is weaker and much redder in the first hour after sunrise and before sunset, which is the point.
- Shadows: soft on Medium and up, hard on Low; `ShadowResolution.VeryHigh` on High and Ultra; 4 cascades on High (150 m)
  and Ultra (260 m) with the first cascade covering the nearest 16 m (High) or 28 m (Ultra), where the player and the
  street are; `StableFit`; a little less bias on Ultra.
- The moon: a 29.5 day phase scales its light (full moon about three times a half moon) and the dome draws the matching
  disc; colour from 4100 K.
- Off switch: `Settings.PhysicalSun` restores the hand-tuned warm and noon colours and the old intensity ramp (the
  shadows and the dome stay).

## 2. Bounce light (what it is and what it is not)

No baking is possible in a city that is generated when the game starts, so this is the best runtime approximation:

- The ambient comes from the sky model itself. `SkyMath.SkyAmbient` evaluates the same scattering the dome draws in
  17 directions and gives the upper sky, the horizon band and the cosine-weighted mean; a grey card in the shade then
  shows about a fifth of one in the noon sun (`UrbanBounceGain` 1.5 stands in for the light that city facades and roads
  throw back and forth; the raw single-scattering model alone would give a tenth). The old Trilight sky was about half
  as bright as the sun, which is why shadows used to look flat; expect darker, bluer shadows now.
- The ground term comes from the real ground. `SkyGround` reads the layout under the camera (asphalt on a road, pavers
  and concrete on the sidewalk, grass in a park, sand in the Badlands, snow in the Frostline, water on the canal), eases
  the albedo over half a second, and `SkyMath.GroundRadiance` lights it with the sun, the sky and, at night, the
  street's neon glow. That colour is the Trilight ground colour and the lower half of the sky dome, so sand warms
  people and walls from below, snow brightens them, a park tints them green.
- On High and Ultra, outdoors, the ambient switches to Unity's Skybox mode: `DynamicGI.UpdateEnvironment` renders the
  dome (ground half included) into the ambient probe, which gives real directionality (a bright warm horizon toward the
  sunset lights the side of a face that way) instead of three flat colours. The refresh is throttled: it runs only
  when the quantised sun elevation, cloud, fog, dust, frost, Veil, night, haze or ground albedo changed, no more often
  than every 1.5 s (0.5 s on Ultra, 4 s on Medium and below), or when a Veil shift forces it. The urban gain rides on
  `RenderSettings.ambientIntensity` so the dome itself stays physical.
- Below High, when `Settings.SkyAmbient` is off, and whenever the player is underground (the Underheart layer lerps
  the Trilight colours), the Trilight carries the same numbers, so switching modes is nearly seamless.
- The night keeps its look: the old violet-blue night ambient (0.13, 0.12, 0.24) is added back as the neon glow term,
  following the game's own `NightFactor`, so nothing goes pitch black.

What this cannot do, compared with real global illumination: light does not bounce from one wall onto the next; a
courtyard in shadow gets the same ambient as an open plaza (ambient occlusion in NeonPost darkens the corners, nothing
raises the open ground); neon signs do not light the facade across the street; the ground term is one colour for the
whole view, taken under the camera, so a character standing on grass while the camera is on the road gets the road's
bounce. Light probes or a real GI solution would be needed for those.

Two things were lost on purpose in Skybox mode: the small Trilight tints the Art District and the 04A ambience add
(they still shape the fog, and they still work on Medium, Low and underground), and the lightning's ambient flash now
rides on `ambientIntensity`.

## 3. Reflection probes

`ReflectionProbes` (created by `Atmosphere.Init`, under the Atmosphere object):

- Areas: the nine core districts as the exact rectangles of `CityLayout.DistrictAt`, the Sunken Row canal as a low
  band, four desert quadrants, one Frostline box; one box per Underheart region (added when `Game.Underheart.Ready`,
  world-space bounds of its cells); one box per inhabited venue room (added when the venue stands, the room's local box
  turned by its yaw). District captures sit 7 m above the nearest road node to the district centre so they never start
  inside a building; cave captures sit inside the cell nearest the region's centre; room captures at eye height in
  the middle of the room.
- Budget: Low none; Medium one 64 probe that follows the camera, re-rendered every 6 s (NeonPost's own camera probe only
  runs on High and up, so Medium had none before); High the nearest 4 areas at 128; Ultra the nearest 8 at 256. Nearest
  is the distance to the box (0 inside), interiors before caves before districts on ties.
- Rendering: `ViaScripting` with `IndividualFaces` time slicing, one probe in flight at a time; a probe renders when it
  comes into the live set or when `Atmosphere.EnvironmentStamp` moved past its last render, never more often than every
  5 s (High) or 3 s (Ultra) per probe. World layer only, no shadows, HDR, far clip a little past the box.
- Blending: probes blend at their box edges (25 m districts, 6 m caves, 1.5 m rooms). Interiors (importance 24) win
  over caves (22) over districts (20) over NeonPost's camera probe (10), and interiors and caves use box projection, so
  chrome in a venue reflects the room and not the sky.
- Note: the Underheart's merged shell meshes have `reflectionProbeUsage = Off` (04E), so its probes affect props,
  vehicles, companions and the player, not the walls.
- Off switch: `Settings.AreaReflections`. The system is Built-in only (`RenderPath.IsURP` skips it).

For the Phase 3 owner of NeonPost.cs: `NeonPost.UpdateProbe` keeps rendering its 400 m camera probe every 1.2 to 2.5 s on
High and Ultra, which the area probes now outrank everywhere. Gating it with `!ReflectionProbes.Active` saves that work.

## 4. Sky and fog

`Resources/NeonSky.shader` (Medium and up, `Settings.ProceduralSky`; Low keeps Unity's procedural skybox):

- Per pixel single scattering with 8 samples along the view ray and the Chapman transmittance toward the sun at each
  sample, the same constants as `SkyMath` (Rayleigh 5.8e-6, 13.5e-6, 33.1e-6 per metre, scale height 8 km; Mie 2.1e-5
  times the haze, scale height 1.2 km, g 0.76; multiple scattering gain 3). Haze rises with cloud, fog, dust, rain and
  frost; a sandstorm tints the Mie term orange.
- Sun disc with limb darkening (0.55 deg radius, larger in the Frostline air and in the Veil), reddened by the
  transmittance; a moon with phases and a thin halo.
- Night: hashed stars with magnitudes, colour and twinkle, a faint galaxy band, both turning once a day about a tilted
  pole, visible only where the sky is dark and clear; the city's neon glow on the horizon and a floor at the zenith so
  the night is never black.
- Clouds: four octaves of value noise (three on Medium) on a 1.5 km plane, coverage from the weather, lit tops from
  `SkyMath.CloudRadiance`, dark cores, a rim where the sun is behind the deck, scrolling with the wind. The weather
  states that used to just grey the sky now show an overcast.
- Horizon haze pulls toward the scene fog colour, and the day fog colour itself now comes from the model's horizon band,
  so the buildings' fog meets the dome without a seam.
- The ground hemisphere shows the ground bounce colour (section 2), meeting the haze at the horizon.
- The Veil override follows `InnerWorld.Blend` as before: red-violet sky, dark plum horizon and ground, the low crimson
  sun, no stars or clouds.

Height fog: the Standard shader's fog hook is per material and distance only, so height fog could not be done on the
dome or in the materials without touching every material. It is done instead as part of the volumetrics post pass
(section 5), which reads depth: exponential height fog (density 0.00035 at ground level, scale height 14 m, more in
rain and at night) integrated in closed form along each pixel's ray, plus a ground fog band (a 2.5 m slab above the
ground under the camera) that rises at dawn (sun from -4 to 3 degrees, gone by 12) and stays in the Frostline. Off
underground and inside roofed venues. The distance fog of `RenderSettings` is unchanged. Off switch:
`Settings.HeightFog`; High and up only.

## 5. Volumetric light

`NeonPost.Volumetrics.cs` and `Resources/NeonShafts.shader`, called once before the bloom pass, High and up,
`Settings.VolumetricLight`:

- Seed at quarter resolution: only sky pixels (depth at the far plane) within a radius of the light's screen position,
  in the frame's own HDR colour clamped to 6, so the sun disc and the bright sky around it drive the rays and every
  tower, person and vehicle cuts a dark band into them (occlusion-aware).
- Two radial blurs toward the light (12 taps each, a short then a long one), then a full-resolution composite with the
  height fog.
- The light is the sun whenever it is above the horizon, else the moon on clear nights at about half strength. The
  strength fades as the light leaves the screen and is lower in the Veil; underground it is off. Lamp shafts at night
  were judged too costly as a screen-space effect (one radial pass per lamp); the Underheart lamp cones below cover
  the lamp case as geometry instead.
- The pass needs the camera depth texture, which `NeonPost.OnPreCull` requests only for ambient occlusion or motion
  blur (both on by default on High). If both are turned off the pass skips itself and reports "no depth texture" on
  the Lighting page. For the Phase 3 owner: adding `|| WantVolumetrics` to that OnPreCull condition removes the
  dependency.

Underheart lamp cones (`UnderheartBuilder.Shafts.cs`): after the landmarks, every practical light that hangs at least
2.2 m above its cell's floor and below its ceiling gets an additive 14-sided cone (4.5 m or down to 0.3 m above the floor,
a 4 by 64 gradient bright at the lamp), tinted with the lamp's colour at a fifth of its intensity, parented to the cell's
render group so it streams with the region. At most 4 are built per cell; the preset shows Low 0, Medium 2, High 3,
Ultra 4 (`UnderheartLampShafts.ApplyPreset`). They are geometry, not lights: `UnderheartLightBudget` is untouched.

## 6 and 7

Ambient occlusion stays in NeonPost (Phase 3). The Underheart lighting budget rules are unchanged.

## What each preset gets

| | Low | Medium | High | Ultra |
|---|---|---|---|---|
| Sun colour and intensity from the model | yes | yes | yes | yes |
| Shadows | hard, 45 m, 1 cascade, Medium res | soft, 80 m, 2 cascades, High res | soft, 150 m, 4 cascades, VeryHigh | soft, 260 m, 4 cascades, VeryHigh |
| Sky dome | Unity procedural (old) | NeonSky, 3 cloud octaves | NeonSky, 4 octaves | NeonSky, 4 octaves |
| Ambient | Trilight from the model and ground | Trilight | Skybox mode outdoors, refresh at most every 1.5 s | Skybox mode, at most every 0.5 s |
| Reflection probes | none | one camera probe, 64, every 6 s | nearest 4 areas, 128, 5 s per probe | nearest 8 areas, 256, 3 s per probe |
| Light shafts and height fog | no | no | yes | yes |
| Underheart lamp cones per cell | 0 | 2 | 3 | 4 |

Switches (all in Settings, saved, and on the F2 Lighting page): PhysicalSun, ProceduralSky, SkyAmbient,
AreaReflections, VolumetricLight, HeightFog. There is no menu entry for them yet; the settings menu is UI code outside
this phase.

## Checks

- `check_urp.sh`: 0 errors, 63 warnings (the baseline's 63 CS0618), all twelve configurations.
- `check_tests.sh`: 1190 passed (1169 baseline plus 21 new), 25 failed (the baseline's engine-only Resources.Load
  failures), 53 skipped. New tests: `SkyMathTests` (sun path and Euler mapping, night factor, the Chapman
  approximation against a 40000-step integral within 16 percent, transmittance reddening and fading, sun intensity
  monotonic by elevation, Kelvin to xy against four Planckian reference points, apparent Kelvin falling toward the
  horizon, blue zenith and red horizon in the scatter model, the sky to sun ratio between 0.08 and 0.35, cloud and
  ground terms, ground classification on the real city layout, cascade splits, phase functions integrating to 1, the
  moon cycle) and `ReflectionProbeTests` (budget per preset, the 15 city areas matching `DistrictAt` corner by corner
  and covering every street-level point, nearest ordering, venue boxes turning with the room, Underheart areas for all
  six variants, the lamp cone cap).
- Both new shaders parse and compile to SPIR-V under glslang's HLSL front end against a stub of UnityCG (a syntax and
  type check only; Unity's own compiler on DX12 is the authority).
- No U+2010 to U+2015 characters in any changed file; no names of other games or products.

## Could not be done, and why

- Height fog inside the materials: the Standard shader's fog hook has no height term and every material would need a
  custom shader. Done in the post pass instead (needs the depth texture, so High and up).
- Night lamp shafts as a screen-space effect: one radial pass per lamp is not cheap; the Underheart cones are geometry
  instead, and street lamps have none.
- Light bouncing between walls, darker courtyards, neon lighting the far facade: needs probes or GI (section 2).
- The settings menu has no rows for the six new switches (UI code, outside this phase); the F2 Lighting page has them.
- URP: the dome, the probes and the volumetrics are Built-in only; the sun colour and intensity, the Trilight ambient and
  the ground bounce apply on both paths. The URP beta compiles.

## What Tyler must look at in Unity

Capture Golden Shots before switching to this build, then after, and run Compare Golden Shots. Judge these in order:

1. `core_street` and `old_silver` at 12:00: shadows should be noticeably darker and slightly blue, the sunlit side warm
   (noon reads about 5150 K). If the street looks too dark overall, that is the ambient drop from section 2: say so,
   the `UrbanBounceGain` constant in SkyMath.cs is the one knob.
2. The same two views at 22:00: the night should look as it did (violet-blue, never black). Stars and the galaxy band
   should be visible above the Badlands (`desert_motel` at 22:00) and faint over the city.
3. Set the clock to 6:20 and 17:40 (F2 or cheats) on `strip_aerial` and `city_aerial`: a red horizon toward the sun,
   a deep blue sky opposite, the sun disc red and dim, long warm shadows, light shafts between the towers when the sun
   is in or near the frame, and a low fog band on the ground at dawn.
4. `frostline_outpost` at 12:00: snow should brighten people and walls from below (the ground bounce) and a thin
   ground fog should hang over the snow.
5. `desert_motel` at 12:00: sand should warm the undersides of things; the horizon haze should meet the dome without a
   visible seam.
6. Weather: set Cloudy, Rain, Storm, Fog, Sandstorm in turn and look up: an actual cloud deck should drift; a
   sandstorm tints it orange; lightning should flash the sky.
7. `veil_core_street` inside the Veil: the red-violet sky, dark plum horizon and crimson sun must still read as before.
8. Walk into an inhabited venue and look at chrome and glass: they should reflect the room (box projection), not the
   sky. In the Underheart: lamp cones under the hanging lanterns, and reflections of the cave on the player's gear.
9. Toggle each switch on the F2 Lighting page with the camera still: each one should change the picture and the page's
   status line; none should throw.
10. Perf flythrough before and after on High and on Ultra: the new costs are the dome's per-pixel scattering, the
    environment refreshes (a cubemap render plus convolution each), the probe renders (time sliced), the quarter-res
    shafts plus one full-screen composite, and 4-cascade shadows at 150 or 260 m. Report the p95 and the worst frames;
    the budget knobs are the refresh intervals in `Atmosphere.Bounce.cs` and `ReflectionProbeRules.MinRenderInterval`.
