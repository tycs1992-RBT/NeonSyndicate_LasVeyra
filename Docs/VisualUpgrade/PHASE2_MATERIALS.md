# Phase 2: materials and surfaces

Visual upgrade, Phase 2 (materials and surfaces). Built-in Render Pipeline forward, the URP beta kept in step through
the shared property names. Nothing in gameplay, physics, saves, input, quests or placement changed; every edit is on the
material property side, in the texture generators, or in the two character shaders and one new foliage shader.

## What changed, in plain words

Before this phase almost every surface in Las Veyra was a flat colour, or a colour texture with at best a normal map
derived from the colour's brightness. Light could not tell asphalt from plastic. Now every material family gets a full
physically based map set generated from one height field: the colour (albedo) is painted from the relief, the normal
map is the relief's slope, an occlusion map darkens its crevices, and a mask carries metallic and per-texel smoothness
(puddle hollows on asphalt glossier than the aggregate, grout and rust matte, polished edges). Dirt gathers in cavities
and edges catch light on every kind. Large flat surfaces also tile a fine detail normal and albedo so light raking
across a road or a wall shows texture. Characters' skin, cloth and hair get their own shading terms, and leaves get
wrap lighting and back-light translucency. Low keeps every old material untouched.

## Files

New (all under `Assets/NeonSyndicate/`):

| File | What it holds |
|---|---|
| `Scripts/Runtime/Core/HeightField.cs` | Pure tileable height-field maths: lattice value noise (four reads per texel per octave, no hashes in the loop), fractal noise, per-texel grain, jittered cell (Voronoi) fields, wrapped resample and box blur, the `SeamExcess` tileability metric |
| `Scripts/Runtime/Core/SurfaceFields.cs` | `SurfaceKind` (34 families) and `SurfaceFields` (Height, Tone, Mask, Aux) |
| `Scripts/Runtime/Core/SurfaceHeight.cs` (plus the partials `SurfaceHeight.Micro.cs`, `SurfaceHeight.Underheart.cs`, `SurfaceHeight.Facade.cs`) | Pure generators, one per kind: asphalt aggregate, cracks and puddle hollows; paver slabs with bevels and grout; concrete pores, bug holes and form lines; turf blades; sand ripples and pebbles; dirt clods and dry cracks; rock strata and fractures (badlands, granite, warden stone, Veil rock and the five Underheart palettes share two generators); roof gravel; clay tiles that overlap; corrugated ridges with rust pits; plank wood with knots; palm bark fissures; snow crust plates and drifts; frost-dusted sand; lake ice fractures; water ripples; brushed metal, cast grain, rubber pores, car paint orange peel with sill grime and stone chips, woven fibre, leaf veins; the Underheart's glazed tile, terrazzo, grating, iron brick, riveted plate, veined stone, ash, ember soil, lava crust and blanket weave; and the facade relief (recessed windows, raised frames, brick courses) |
| `Scripts/Runtime/Core/SurfaceMaps.cs` | Pure derivations: normal map texels, cavity and convexity (two blur radii), occlusion, the packed mask (R metallic, G occlusion, A smoothness), the car-paint flake normal |
| `Scripts/Runtime/Core/SurfaceColor.cs` | Pure colouring per kind (the old palettes, now driven by the relief), per-kind smoothness variation and metallic variation, the wet cap, normal strength, occlusion strength and shade gain per kind |
| `Scripts/Runtime/Core/Tex.Jobs.cs` | Background generation: every kind is queued on 1 to 6 low-priority worker threads at the first request (large kinds first); a kind the main thread needs before a worker started it is computed inline; a generation counter drops stale work after `Tex.ResetStatics`; a large kind's retained data is released 60 s after its last request (`TrimStale`) and recomputed if asked again |
| `Scripts/Runtime/Core/Tex.Pbr.cs` | Texture sizes per preset and per machine, the albedo (per palette), normal, mask (per metallic and smoothness) and glow textures of a set, the facade relief and mask, the shared detail maps, the flake normal |
| `Scripts/Runtime/Core/Mats.Pbr.cs` | `PbrLevel`, `PbrEnabled`, `DetailEnabled`; `Pbr(...)` and `Micro(...)` family builders; `ApplyRelief` and `ApplyDetail` (public, used by the packaged art passes); the foliage material; the reworked rain sheen registry |
| `Resources/NeonFoliage.shader` | Built-in surface shader: Standard lighting plus wrap lighting and back-light translucency per light, alpha tested, `_Cull` property |
| `Tests/EditMode/SurfaceMapsTests.cs` | 16 tests: every kind in range, tileable, deterministic and level-stable across sizes; noise, cells, blur and resample tile; normals decode to unit vectors; cavities and edges land on the relief; the mask packs; the wet cap holds for every kind; micro kinds stay near white; grime sits at the sill; facade windows recess; flake normals; preset sizes and the core cap |

Changed:

| File | Change |
|---|---|
| `Core/Mats.cs` | Every world family (Asphalt, Highway, lane paint, Sidewalk, Curb, Concrete, Grass, Sand, Dirt, Rock, Water, Roof, RoofTile, Metal, DarkMetal, Corrugated, Chrome, Rubber, Wood, PalmTrunk, Weed, PalmLeaf, PalmDead, TreeLeaf, Cactus, Fence, Trim, Plastic, RedPlastic, WhitePaint, OrangeCone), the facade builder, CarPaint, CarInterior and Solid keep the old material on Low and build the PBR set from Medium up. The rain sheen lists moved to Mats.Pbr.cs. `ResetStatics` calls `ResetPbrStatics` |
| `Core/Mats.Zones.cs` | Snow (crust relief, sparkle glow from the same field), SnowThin, SnowPeak, Ice, PineNeedles (foliage), FrostRock (granite palette), WardenStone, WardenBanner (fibre relief), InnerRock (Veil palette) |
| `Core/Mats.Underheart.cs` | UhRock (one relief, five palettes, vein glow from the same veins), UhConcrete, UhConcreteFloor, UhTile, UhTerrazzo, UhGrating, UhBrick, UhPlate, UhMarble, UhAsh, UhWeave, UhSoil (ember glow from the specks), UhLava (crust relief, flow glow), UhWater (ripples), UhRail, UhIron, UhCopper, UhBlackStone, UhCloth |
| `Core/MatsUrp.cs` | `UrpApplyMaskKeywords` (metallic workflow, `_METALLICSPECGLOSSMAP`, `_Smoothness` 1); the property mapping note covers the new maps |
| `Core/Tex.cs` | `ResetStatics` resets the job state |
| `Resources/NeonCharacter.shader` | Custom lighting model `NeonChar` on top of `LightingStandard`: skin scatter (wrap plus thin-skin transmission), cloth sheen, hair strand highlight; three new colour properties, every old property kept |
| `Resources/NeonCharacterURP.shader` | The same three terms for the main light after `UniversalFragmentPBR`; the three properties in the `UnityPerMaterial` buffer |
| `Scripts/Editor/NeonSyndicateSetup.cs` | Four `Standard_Pbr_<n>.mat` keep materials (normal plus metallic map, with and without emission and detail); the set regenerates once on a project from before this phase |
| `Scripts/Editor/NeonSyndicateUrp.cs` | Four `Lit_Pbr_<n>.mat` keep materials with their maps assigned |
| `ArtDistrict/DistrictMaterials.cs`, `ArtDistrict/CrystalCity02FAssets.cs`, `ArtRebirth04A/VisualRebirth04AAssets.cs`, `Gameplay/Strongholds/CitadelKit03A2.cs` | Detail maps on their stone, paving, roof and soil surfaces on High and up (one line each, property side only) |

## What each preset gets

| Preset | Materials |
|---|---|
| Low | Exactly the old materials: the old albedo generators, the old brightness-derived normals where they existed, flat colours elsewhere. No new texture is generated and no new shader is used. Settings text in this document calls this `PbrLevel` 0 |
| Medium | Every family gets the generated set: albedo, normal map, mask (metallic, occlusion, smoothness variation). Large kinds at 256, small-object kinds at 128. The foliage shader and the character shading terms. No detail maps |
| High | The same at 512 (large) and 256 (small), plus detail albedo and normal maps on roads, pavements, concrete, turf, sand, dirt, rock, snow, facades, the Underheart floors and walls, the packaged art stone, and the metallic flake detail normal on car paint. Facade relief at 512 |
| Ultra | 1024 on the hero kinds (asphalt, pavers, concrete, sand, rock, snow crust, Underheart rock and concrete), 512 on the other large kinds, 256 small |

Machine cap: the maps are generated in C# at boot, so `Tex.SizeCapForCores` limits the size to 256 on one or two
cores, 512 on three or four, 1024 above, whatever the preset. A preset change in the settings menu applies to materials
built afterwards; the full set takes effect on the next launch (nothing rebuilds live).

## Where the maps go (Standard shader and URP Lit)

`Mats.ApplyRelief`: `_BumpMap` and `_BumpScale` with `_NORMALMAP`; `_MetallicGlossMap` with `_METALLICGLOSSMAP`
(URP `_METALLICSPECGLOSSMAP`), `_GlossMapScale` 1 (URP `_Smoothness` 1) so the mask alpha is the smoothness;
`_OcclusionMap` and `_OcclusionStrength` (URP adds `_OCCLUSIONMAP`). `Mats.ApplyDetail`: `_DetailAlbedoMap`,
`_DetailNormalMap`, `_DetailNormalMapScale`, `_UVSec` 0, the tiling on `_DetailAlbedoMap`, under `_DETAIL_MULX2`
(URP adds `_DetailAlbedoMapScale`). Normal maps are RGB with x in R and y in G, which `UnpackNormal` reads on desktop
(x times A, A is 1). Albedo RGB24, normal RGB24 (linear), mask RGBA32 (linear), all with mips, uploaded non-readable.

The mask alpha is absolute smoothness, so one kind serves several materials (asphalt 0.18 and the highway 0.2 share
one height field but get their own mask). Rain: `RegisterWet(m, nominal)` records the property the sheen drives:
`_Glossiness` (plain materials, lerped to 0.82 as before) or `_GlossMapScale` (`_Smoothness` on URP) for masked
materials, scaled from 1 to 0.82 over the nominal smoothness. The mask of a wet-registered surface caps its texels at
nominal over 0.82 (`SurfaceColor.Smoothness`), so the glossiest texel lands exactly on 1 when soaked and the shader
never sees a smoothness above 1 (the `WetSmoothnessNeverExceedsOneUnderTheRainScale` test checks every kind).

## Skin, cloth, hair and foliage

`NeonCharacter.shader` keeps its properties and the palette pipeline; BodyBuilder and Appearance are untouched. The
palette row of the texel picks the model: row 0 skin, rows 1 and 3 cloth (3 at half strength), row 2 hair. Skin gets
an energy-conserving wrapped diffuse past the terminator in a scatter colour that deepens with the skin tone
(`_SkinScatter`, default a warm red at strength 0.6) and a transmission term from the light bent through the surface,
weighted by the silhouette (ears and fingers are thin exactly where the silhouette is; there is no thickness map, so
this is the approximation). Cloth gets a grazing sheen (`_ClothSheen`). Hair gets a Kajiya-Kay strand highlight along
the loft direction (tangent-space Y, which BodyBuilder's explicit tangents make the ring-to-ring direction)
(`_HairAniso`). The Built-in shader applies them per light (base and additive passes); the URP twin applies them for
the main light only (additional lights keep plain PBR) and compiles only with the URP package, as before.

`NeonFoliage.shader` (Built-in only, Medium and up): Standard lighting plus wrap lighting (`_Wrap`) and a back-light
tint (`_Translucency`, `_TransStrength`) per light, alpha tested so the weed cards and the solid leaves share it, and a
leaf micro normal (midrib, side veins, cell relief). The leaf meshes are closed solids and the weed cards carry both
faces, so culling stays Back (`_Cull` is there for future single-sided cards). On URP the same `Mats` getters hand out
the URP Lit materials they did before (a Built-in-only feature, documented here).

## Wear

Every kind: cavities (grout, cracks, pits, seams) darken the albedo, lose smoothness and darken the occlusion channel;
convex edges and ridges lighten the albedo and gain smoothness. Kind-specific: asphalt puddle hollows glossy and a
little darker, cracks rough; paver grime; concrete damp stains glossier; rust on corrugated sheet is rough and no
longer metallic; car paint has grime rising from the sill of every panel face (dull, darker), stone chips that show
bare metal, orange-peel undulation, and on High a metallic flake detail normal under a 0.9 clearcoat smoothness;
brushed metal scratches and tarnish; ice fractures and frost bloom matte; glazed tile grout matte; terrazzo flecks
polished, scuff trails dull; riveted plate heat tint; marble veins.

## Packaged art passes (02D ArtDistrict, 02F, 03A2 Citadel, 04A, 04C)

Inventory: `Resources/ArtDistrict` (25 OBJ meshes, 47 textures as albedo, normal and mask sets, 2 shaders, 14 keep
materials), `Resources/CrystalCity02F` (28 meshes with CMH1 fallbacks, 16 textures: gilt, leaf, moonstone, verdant
sets), `Resources/CrystalStrongholds02I` (26 meshes, fortstone set and a banner), `Resources/VisualRebirth04A` (95
meshes, 48 textures in 12 families with albedo, normal, mask and emission, 56 `.mat` files that no code loads; the
materials are built from `material_manifest` in code), `Resources/InhabitedCity04C` (32 meshes, plaster and timber
albedo). All of them were already built in code with albedo, normal, metallic and smoothness mask and occlusion
(`DistrictMaterials`, `CrystalCity02FAssets`, `StrongholdAssets`, `VisualRebirth04AAssets`), so they were not flat.
What they lacked was the detail layer: this phase adds `Mats.ApplyDetail` to the ArtDistrict sandstone, basalt and
promenade, the 02F stone, the 04A paving, soil, stone and roof, and the Citadel stone, on High and up. Not changed:
the 04C plaster (`ArtInhabitedCity04C.cs`, outside this phase's file list) deliberately strips its normal and mask
maps for a clean finish; a one-line `Mats.ApplyRelief` with the Pores micro kind at `_BumpScale` 0.3 would give it
pores without masonry joints, if wanted.

## Cost

Textures are generated once, never per frame, and cached in `Tex`. Counts: 34 kinds, each albedo plus normal plus
one mask per (metallic, smoothness) pair that uses it (about 50 masks in a full session), 11 facade reliefs and
masks, 4 detail albedo maps, the flake normal. Memory with mips, High: about 110 MB of textures (22 large kinds at
512 times three maps, 11 facades, small kinds); Ultra: about 200 MB; Medium: about 30 MB; Low: unchanged.

CPU, measured offline with the .NET 8 build of the pure code on this sandbox (a loaded two-core machine, so take
these as upper bounds; Unity's IL2CPP player should be similar, the Mono editor slower): one large kind at 512 costs
about 60 to 130 ms for the fields and 70 to 130 ms for the maps (two blurs, normal, albedo, gloss, occlusion, mask),
a small kind at 256 about 25 ms, a facade relief at 512 about 40 ms. Serial totals: about 1 s at 256 (Medium),
3.5 to 5 s at 512 (High), 12 to 24 s if every kind were 1024 (which is why Ultra keeps 1024 for the eight hero kinds
only, about 7 s serial). This is well above the few hundred milliseconds asked for, so the work is not left on the main
thread: `Tex.Jobs.cs` runs the pure generation on background threads started at the first request, while the main
thread goes on building the city. The main thread computes a kind inline only when it asks for one no worker has
started (the first one or two kinds at boot, about 150 ms each at High), and waits only for a kind already in
progress. Texture upload (SetPixels32 and mips) stays on the main thread, about 3 to 5 ms per 512 map. With four or
more cores the expected main-thread cost at High is a few hundred milliseconds spread over the boot steps; on two
cores the size cap drops the maps to 256, which keeps the same bound. `Tex.SurfaceJobsInline`,
`Tex.SurfaceJobsOnWorkers`, `Tex.SurfaceJobsTrimmed` and `Tex.SurfaceMsOnMainThread` report what actually happened in a session (worth a log
line in an F2 page later; not added here, the debug pages are outside this phase's files).

Rendering cost: one extra texture fetch per map on every opaque surface (normal, mask, occlusion share the mask), two
more with detail maps; the character shader adds a handful of ALU per light; the foliage shader the same. Nothing new
on Low.

## What could not be done here, and why

- Nothing was run in Unity. There is no editor or GPU on this machine; the compile checks and the offline tests prove
  the code builds in the twelve configurations and that the maths is right, not how it looks.
- Parallax or height blending was not added (not asked for; it would need the `_PARALLAXMAP` variant and a height
  texture per kind, which the fields already provide if wanted later).
- Hair anisotropy uses the mesh tangent frame, not a flow map; it will look right on lofted hair and arbitrary on
  flat hair pieces.
- Skin transmission has no thickness map (silhouette weighting instead).
- URP: the character terms run for the main light only; foliage keeps URP Lit.
- A worker that stalls for ten seconds is abandoned and the kind is computed on the main thread with a warning (a
  guard, never expected to trigger).
- The preset change applies to new materials only until the next launch.
- A finished boot could release the retained float fields of the large kinds (about 10 MB per large kind at 512 is
  kept in managed memory so later palettes and mask variants need no recomputation); `Tex` trims a large kind's data
  60 s after its last use and recomputes it if asked again, and a call at the end of `GameRoot.Boot` would free it
  sooner (GameRoot is outside this phase's files).

## What Tyler must look at in Unity

Capture Golden Shots before and after (same resolution), High preset unless stated, then compare. Expect "big"
differences everywhere a ground, wall or vehicle is in frame; that is the intent. Judge:

1. `core_street` at 12:00: the asphalt should show aggregate and a few cracks with light raking across it, lane paint
   slightly rough, the pavers bevelled with dark grout; facades should show recessed windows (the frames catch light
   at the top edge, the recess is darker at the bottom) and plaster grain close up. Nothing should look embossed or
   plastic: if the relief reads too strong, lower the `bumpScale` arguments in `Mats.cs` (asphalt 0.7, sidewalk 0.7,
   facades 0.6).
2. `core_street` at 22:00: the glass of the office towers should reflect the neon (windows smooth and a little
   metallic now, walls rough); puddle hollows on the asphalt should pick up the signs. Then set rain in the F2 menu and
   check the sheen still comes up evenly and nothing turns white or black (the wet scale is capped at 1).
3. `strip_street` and `sunken_row_canal`: car paint should show grime along the sills of each panel, a soft
   orange-peel reflection, and (High) a faint flake sparkle; chrome and dark metal should be brushed, tyres pored.
   The canal water should ripple in its reflections.
4. `old_silver` and `desert_motel`: corrugated sheet with rough rust patches, plank wood with knots and seams, roof
   tiles that overlap, palm trunks with fissures, sand with ripples and pebbles, rock with strata. Cactus, palms and
   tree leaves should light up at their edges when the sun is behind them (translucency) and never go flat black on
   the shadow side (wrap).
5. `frostline_outpost` at 12:00 and 22:00: snow with crust plates and glitter at night, lake ice with fractures and
   frost bloom, granite and the warden stone, the banner weave.
6. `veil_core_street`: the Veil rock (violet palette) and the crystals (unchanged).
7. Walk into the Underheart: cave rock in each region (the veins should glow along the same lines the relief shows),
   the service tunnel concrete with form lines, the transit tile with matte grout, terrazzo with polished flecks,
   grating, iron brick, riveted plates with rivet heads, the archive stone, ash, ember soil with glowing specks in
   the cracks, lava with a dark raised crust over bright channels (the flow still scrolls).
8. Characters, close up at 12:00 and under a street lamp at 22:00: skin should have a soft warm wrap at the terminator
   and a faint glow at ears and fingers against the light, cloth a soft sheen at grazing angles, hair a highlight
   band that moves with the camera. If skin reads sunburnt, lower `_SkinScatter` alpha (0.6) on the character
   material; if hair looks greasy, lower `_HairAniso` alpha (0.35).
9. Settings: switch to Low and restart: everything must look exactly as before this phase. Medium: the maps at 256,
   no detail. Ultra: 1024 on the roads, pavers, concrete, sand, rock, snow and Underheart rock.
10. Boot time and the console: look for `[Tex] Surface ... failed` warnings (a generator threw; the family then falls
    back to a plain material) and compare the time to the title screen before and after on High. The Perf flythrough
    before and after should show no more than the cost of the extra texture fetches (expect a small, flat increase).
11. URP beta (Switch to URP, Repair Project Setup to regenerate `ShaderKeepURP`): the same maps on Lit, the character
    shader's new terms for the sun, leaves on plain Lit.
12. Build a Windows player and check the roads and facades keep their normal, mask and detail maps (the
    `Standard_Pbr_0..3` keep materials hold the variants); if a surface comes out flat only in the build, run Neon
    Syndicate > Repair Project Setup and rebuild.
