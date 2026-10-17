# Phase 4: geometry, detail and VFX

The fourth pass of the visual upgrade. The city, its people, creatures, vehicles and weapons are still generated in
code; this pass does not replace any of that. It does four things: lists what a modelled asset would improve (for the
owner's approval, nothing deleted), fills the empty flat spaces with decals and collider-less clutter, fixes the
shading of the curved primitives every builder uses, and adds budgeted ambient particles plus a secondary impact
burst. Everything new is preset gated: Low gets nothing new, Medium the light share, High the full set, Ultra the
dense set. Nothing here changes gameplay, physics, colliders, AI, input, quests or saves.

## 1. What changed, file by file

| File | Change |
|---|---|
| `Core/MeshKit.cs` | `MeshBuilder.QuadSmooth` and `TriangleSmooth` (one normal per corner). `Cylinder` shades its side smooth with one shared normal per rim vertex when it has `SmoothSides` (6) or more sides, with the correct tilt for frustums; caps stay flat. `Sphere` shades smooth with true ellipsoid normals when it has 6 or more segments. Posts and markers with 3 to 5 sides keep their flat faces. Vertices, winding, UVs and tangents are unchanged, so every collider built from these meshes is identical. |
| `World/Decals.cs` | New, pure. `DressKind`, `DressItem`, `Keepout`, `DressingRules`: preset shares (Medium 30 percent, High 72 percent, Ultra all), per-block caps, clearances (points of interest 7 m, venue doors 6 m, lot entrances 3 m, parked cars 3.6 m, furniture 1.2 m, walk line 1.1 m), district grit, the open-strip facade rule (`FacesOpen`), walk-line and footprint tests. |
| `World/Decals.Planner.cs` | New, pure. `DressingPlanner` plans the full set once from the layout seed: facade grime, litter, cable runs and planters along every street-facing wall (most walls stand 1 to 3 m back on a forecourt, so the pieces sit on the forecourt, off the sidewalk's walk line); poster scraps and water stains on plain facades; curb drain stains; manholes, parking-slot oil, tyre marks and worn paint at signalled approaches on the roads; lot clutter and oil in the empty, warehouse, parking and project lots; grass cards in parks and cemeteries; scrub around the Badlands cacti and rocks; gas station and motel stains. Every item carries the lowest preset that shows it. |
| `World/Clutter.cs` | New, pure. Roof gear for every plain single-tier flat roof (452 on the default city): a pipe run, vents, an air unit, a hatch, a dish, up to 7 pieces by area. |
| `World/Decals.Tex.cs` | New. `DecalTex` (grime, oil, drain fan, manhole, paint wear, a four-scrap poster sheet, water stain; 48 to 128 px, cached, reset through the statics hub) and `DecalMats` (fade-transparent Standard materials through `Mats.Textured` and `Mats.MakeTransparent`, so the URP beta gets the same materials). Skid and scorch reuse the hit-effect materials. |
| `World/Decals.Builder.cs` | New, `CityBuilder` partial. Builds one root per tier (`Dressing Medium`, `High`, `Ultra`) under the city root, merged per 480 m cell and per material; decals are quads 12 mm above their surface (above the 25 mm lane paint where they must cover it) with per-item depth jitter; clutter casts and receives shadows, decals and foliage cards receive only. A tier not built at boot is built on demand (coroutine, 2500 items per frame). The plan skips art-district fronts and walk-in venues (`DistrictArchitecture.Contains`, `InhabitedCitySystem.ReplacesBuilding`), keeps out of venue footprints and doors, and out of every registered ground opening (`WorldApertures`). |
| `World/Clutter.Builder.cs` | New, `CityBuilder` partial. The clutter meshes: bottles (standing and lying, two glass colours), cans, paper, cardboard boxes (closed, open flap, flattened), crates, pallets (stacks of 1 to 3), tyre stacks, cable reels, wheelie bins, cable runs, planters, roof vents (two designs), roof pipes with supports and an elbow, air units, hatches, dishes. All collider-less. |
| `World/Decals.Layers.cs` | New. `DressingLayers` on the city root: watches `Settings.Graphics`, hides tiers above the preset, builds and shows missing ones. Also the statics hub `DressingLayers.ResetStatics` (textures, materials, clutter materials, ambient spots) and the F2 page **Dressing** (Order 56): item counts per tier, alive particle spots, and a hide switch for before and after comparisons. |
| `World/CityDressing.cs` | `Weed` now writes through the shared `WeedCards(MeshBuilder, ...)`, which the dressing layers reuse for grass and scrub. `BuildProps` ends by building the dressing tiers, attaching `DressingLayers` and registering the city's ambient spots; each step is wrapped so a failure cannot stop the city build. |
| `Effects/AmbientVfxRules.cs` | New, pure. Budgets: emitters alive (Medium 6, High 14, Ultra 22), cull distance (38, 60, 85 m), particle caps and rates per family and preset, which families run on Medium (motes, embers, snow motes only), and `Select` (nearest allowed spots within the cull distance, bounded). |
| `Effects/AmbientVfx.cs` | New. The spot registry (`Register`, `RegisterUnder` for spots that die with a transform, `Prune`, `RegisterCity`). City spots: night steam at a third of the curb drains in the Core, Old Silver, Sunken Row, Ash Underpass and Brass Projects. |
| `Effects/AmbientVfxRunner.cs` | New. The runner: every 0.4 s ranks spots, binds the chosen ones to pooled particle systems (box emission, noise drift, additive soft dot or puff material, no shadows, no probes), keeps a slot 1.5 s before a nearer spot may take it. Owns the roaming Frostline snow motes (follow the camera while the ground under it is snow), the dust volumes inside every walk-in venue (the Lofts denser), and the neon sign flicker (14 facade signs, night only, High and Ultra). |
| `Effects/Fx.Polish.cs` | New, `Fx` partial. `ImpactPolish`: a same-frame secondary burst by surface (concrete and dirt chips plus a hanging puff, wood splinters, a hotter metal spark scatter with a wisp of smoke, glass glitter), through the existing pooled emitters, at most 6 polished impacts per 0.1 s, half strength on Medium, nothing on Low. |
| `Combat/Fx.cs` | One line: `Impact` calls `ImpactPolish` after its existing switch (outside this phase's file list; see Open items). |
| `World/Underheart/UnderheartBuilder.Props.cs` | `Lantern` registers a dust-mote volume under every lit lantern (its lamp colour, sized by range), anchored to the region so it goes when the region does. |
| `World/Underheart/UnderheartBuilder.Dressing.cs` | Heat haze over every lava channel and hot vent plus low embers over lava (`AmbientHeat`, from the Basalt dressing), ember volumes in the Foundry Crown chamber and the Regent arena (`AmbientEmbers`), spore volumes in the gardens (`AmbientSpores`). Dressing only; no fixture, hazard or collider changed. |
| `Core/Game.cs` | One line: `DressingLayers.ResetStatics()` in `Game.ResetStatics`. |
| `Tests/EditMode/Phase4DressingTests.cs` | 16 tests: tier partition and preset gating, keepout and walk-line helpers, the open-strip facade rule, plan determinism and size, every kind planned, tier growth and the Medium share, per-block caps, clearance from points of interest, lot entrances, parked cars and street furniture, buildings and walk lines, roof gear only on plain flat roofs at roof height, wall decals horizontal and above ground, venue footprints and doors kept clear, ambient budgets and selection, smooth cylinder and sphere normals (unit length, shared at edges, outward, flat below 6 sides). |

Offline proof: all twelve compile configurations build with 0 errors and the same 63 warnings as the baseline;
the offline test run is 1185 passed (1169 baseline plus 16 new), 25 engine-only failures, 53 skipped, no new
failures.

## 2. What each preset gets

| | Low | Medium | High | Ultra |
|---|---|---|---|---|
| Decals, clutter, roof gear, foliage cards (default city, 25,600 planned pieces) | none | about 8,100 (the Medium share plus every manhole) | about 18,600 | all 25,600 |
| Ambient particle emitters alive | 0 | up to 6, within 38 m; dust motes, embers, snow motes only | up to 14, within 60 m; every family | up to 22, within 85 m; 1.5x caps and rates |
| Night steam at drains, heat haze, spores | off | off | on | on |
| Neon sign flicker | off | off | on | on |
| Impact polish | off | half counts | full | full |
| Smooth shading on cylinders and spheres | always (it is free: the same vertex count) | | | |

Changing the preset in Settings takes effect without a restart: tiers above the new preset hide at once, a missing
tier is built over a few frames.

Budget notes. A dressing piece is a few merged primitives; the offline estimate for the default city is about
320,000 vertices for the Medium tier, 440,000 more for High and 310,000 more for Ultra (about 1.07 million in all),
in roughly 350 static renderers per tier (480 m cells times the materials used in them; the usual frustum culling
applies, and only the cells around the player are on screen).
Decals are transparent quads with no depth write, so they cost fill only where they are seen. Ambient systems are
capped per family (dust 90, haze 40, embers 70, snow 160, steam 30, spores 60 particles on High) and never more than
the preset's emitter count. The impact polish never more than doubles a hit's particles and is throttled per 0.1 s.

## 3. Candidate list: code-generated families that a modelled asset would improve

Nothing in this table was deleted or replaced. Counts are from the default city (seed 61724) through the offline
layout. "Needs" is what a replacement must provide to drop in without touching gameplay: the pivot the builder
assumes, the scale, and LODs. The packaged assets already present are listed after the table.

| Family | Built in | Instances | Needs for a replacement |
|---|---|---|---|
| Buildings, plain facade boxes (Stucco 397, Brick 272, Warehouse 185, StuccoPink 145, Apartment 140, OfficeGlass 53, DarkGlass 47, GoldGlass 29, Police 3, Hospital 3) | `CityBuilder.BuildBuilding` (boxes with facade textures, tiers, storefronts), `CityBuilder.Fantasy02E/04A` themed variants | 1,299 buildings, 1 to 3 tiers | Modular facade kits per style (ground floor, repeat floor, cornice, corner) sized to the 16 m x 14 m facade tile, pivot at the footprint corner at curb height; LOD1 at 80 m, LOD2 billboard beyond 300 m; the footprint box collider stays |
| Roofs: flat (452), flat with props (735), antenna (88), helipad (24) | `CityBuilder.BuildRoofProps`, now plus `Clutter.cs` roof gear | 1,299 | Roof kit (parapet, vents, air units, tanks, access hut), pivot at roof plane centre, 1 LOD |
| Street lamps | `CityDressing.BuildProp` (StreetLamp02E first) | 2,652 | Pole with arm and head, pivot at the base, 7.4 m tall, head at (0, 7.18, 2.15) local, LOD1 at 60 m; the capsule collider stays |
| Benches, picnic tables, planters, hoops, tombstones, billboards | `CityDressing.BuildProp` | 440, 40, 165, 42, 550, 12 | One mesh each, pivot at ground centre, facing +Z, LOD1; box colliders stay |
| Breakable furniture: trash cans, hydrants, parking meters, news boxes, mailboxes, dumpsters, barrels, cones | `PropStreamer.Spawn` (World/CityRuntime.cs), streamed near the player | 681, 429, 291, 282, 243, 84, 62, cones per site | Rigid props with the same pivot (ground centre) and the same size, since the rigidbody and collider sizes are set from it; no LOD needed (streamed within 300 m) |
| Fountains | `CityDressing.BuildProp` (FountainCrown02E) | 7 | Basin and crown, radius 2.8 or 4.5, pivot at ground centre |
| Monorail pillars and beam | `BuildProp` (MonorailPillar), `BuildMonorail` | 19 pillars, 80 m beam segments | Pillar 1.4 x 11 m with cap; beam 2.2 x 1.2 m profile extruded |
| Trees (sphere canopies on cylinder trunks) | `CityDressing.Tree` (StreetTree02E first), `TreeSpots` | 304 sidewalk, 254 park and lot | Trunk plus canopy with cards, pivot at ground, 3.2 to 4.5 m, LOD1 cards, LOD2 billboard; the capsule collider stays. Needs the foliage shader (see Open items) |
| Palms | `CityDressing.Palm` (PalmCrown02E crown) | 277 sidewalk, 154 lots | Curved trunk 8.5 to 12.5 m, frond cards, pivot at ground |
| Cacti and Badlands rocks | `Cactus`, `RockProp` | 181, 87 | Three cactus forms 2.5 to 5.5 m; rock set 1.2 to 5 m, pivot at ground, convex collider stays (built from the sphere mesh, so a rock replacement must keep its own collider) |
| Mountains and mesas | `BuildMountains` (ellipsoids and frustums, rock texture) | 64 ridges, 2 mesas | Sculpted ridge meshes or a heightfield skirt; pivot at ridge centre; far geometry only, one LOD |
| Power poles and wires | `BuildPowerLines`, `Wire` (sagging box segments) | per residential street, 36 m apart | Pole with crossarm 10.4 m, pivot at ground; wires as a line renderer or a 6-segment mesh |
| Traffic signals | `BuildSignals`, `SignalHead` | per signalled node, up to 4 heads | Pole, arm and 3-lamp head; the lamp quads must stay separate submeshes (the controller swaps their materials) |
| Canal walls, bridges, railings | `BuildCanal` | one canal, 1,800 m | Wall and railing modules per 3 m; colliders stay |
| Desert sites: gas station canopy and pumps, airstrip, quarry walls, canyon walls, Crystal Manor pillars | `BuildDesertSites` | 1 to 18 pieces per site | Site kits; the rock walls want sculpted meshes with the same box collider extents |
| Vehicles: Hatch, Sedan, Coupe, Wedge, SUV, Pickup, Van, BoxTruck, Bus, bikes, helicopter, fusion forms | `Vehicles/VehicleModels.cs`, `BikeModels`, `Helicopter`, `Vehicles/Fusion/*` | 9 car bodies, 1 bike family, 1 helicopter, 3 fusion forms | Body shell per style with the wheel positions the data defines, pivot at the chassis centre, doors optional; wheel colliders and body collider stay; paint material slots as today (CarPaint, CarGlass, lights) |
| People (one lofted skinned mesh per person) | `Characters/BodyBuilder*` (analytic normals and tangents already), `BodyFactory` | every pedestrian, police, NPC | A rigged human with the `RigLayout` bone names and the palette UV convention (UV0 palette, UV1 detail); finger bones welcome (HandPose is still a stub); LOD1 at 40 m |
| Creatures, 19 species (quadruped, winged, serpent, wisp, mite) and the humanoid creatures | `Creatures/CreatureBuilder`, `BodyBuilder.Creatures/Golems/Hollows/Wraiths/Djinn` | per spawn | Rigged species meshes with the `CreatureRig` bone names and the breakable-part groups of `CreaturePartsDb` |
| Weapons (21 ids), loot guns (8 types x 6 makers x 7 parts), crystal foci (6), gadgets (6 x 2) | `Combat/WeaponModels`, `LootGunModels.*`, `CrystalFocusModels.*`, `GadgetModels` | per weapon | Parts with the `Foregrip` child every two-handed model carries and the muzzle anchor; loot parts must keep the per-maker part slots |
| Underheart fixtures: lanterns, crates, barrels, cots, tables, shelves, tents, valve wheels, columns, statues, anvils, furnaces, catwalks, rails, pipes, rubble | `World/Underheart/UnderheartBuilder.Props.cs` | per cell (six layout variants) | Fixture set with the same local pivots (base centre; lanterns hang from `hang`); solid furniture keeps the box colliders the builder adds |
| Quest props, forge stations, chests, pickups, turrets, build fixtures | `QuestProps`, `ForgeStations`, `LootWorld.Chests`, `Pickups`, `TacticalTurret`, `BuildFixtures04E` (Shapes boxes, cylinders, balls) | per instance | Small prop set, pivot at ground centre; interactable triggers stay |

Packaged assets already present (mesh and material files under `Assets/NeonSyndicate/Resources`, loaded by code):

- `ArtDistrict` (02D): arch frame and infill, balcony, block, canopy and bracket, cornice, crystal cluster and shard,
  dome, lantern and core, lattice panel, obelisk, parapet, pilaster, planter, rosette, tree canopy and trunk, window
  frame and infill (24 meshes, 14 materials, 32 textures). Used by `DistrictArchitecture` on the Asterfall street and
  up to six starter frontages.
- `CrystalCity02F`: astrolabe, banner, canopy, crescent, crystal fan, island, light seed, luminous caps, pavilion, rune
  disc, terrace, trunk, vine, water arc, each with a LOD (28 meshes). Bounded landmark compositions.
- `CrystalStrongholds02I`: banner, canopy, dais, foundation, gate and trim, heart, inlay, podium, spire, tower,
  trellis, wall, each with a LOD (26 meshes). Stronghold views.
- `VisualRebirth04A`: 47 part families, each high and low (arcade, balcony, banner, bench, bridge, buttress, capital,
  column, crystal cluster, cypress, drill, fern, flower, fountain, guardian, lantern, living canopy and trunk, market
  awning, monolith, mushroom, observatory, ore, planter, portal, railing, reeds, rib vault, rock, roof and finial,
  root arch, ruin, sigil, sky ring, spire, stair, terrace, threshold, tower, vine, wall panel, ward pylon, willow,
  window), 56 materials, 48 textures. The 04A city and stronghold rebirth.
- `InhabitedCity04C`: bed frame, bookcase, chair frame, door frame, engine stand, freight crate, mattress, medical cot,
  pottery set, record stack, round table, service counter, survey scope, timber bench, tool rack, ventilation hood,
  each high and low (32 meshes). Venue interiors.
- `LivingTerritories04B`: canopy shelter, crystal conduit, resonance armillary, rift extractor, rootwater heart, study
  terminal, high and low (12 meshes).
- Shaders: `NeonCharacter`, `NeonPost`, `CrystalDrake` and their URP twins; `ShaderKeep` materials.

The gaps the table shows, in the order the owner is most likely to see them: street lamps and benches (seen in every
eye-level view), the plain facade kits (every street), trees and palms (the sphere canopies read as spheres), the
breakable furniture, cars, then people and creatures.

## 4. Mesh correctness audit

- `MeshBuilder.Box`: hard edges (one normal per face), correct as it was.
- `MeshBuilder.Cylinder`: was faceted (one normal per side face). Now smooth from 6 sides up with a shared, correctly
  tilted normal per rim vertex; 3 to 5 sided pieces (posts, pyramid markers, the chevron) stay faceted on purpose.
- `MeshBuilder.Sphere`: was faceted (one normal per quad). Now smooth from 6 segments up with true ellipsoid normals.
- Tangents: `MeshBuilder.ToMesh` already calls `RecalculateTangents` whenever a submesh has triangles, so the Phase 2
  normal maps have tangents on every builder mesh. Meshes built in `UVMode.Fixed` (palette lookups) have degenerate
  UVs and therefore meaningless tangents, which is fine because those materials carry no normal map.
- `BodyBuilder` and `CreatureBuilder` compute analytic normals and tangents from the loft (checked, not changed).
- Shadows: `Shapes.MeshObject` sets the cast mode and leaves receive on; the city's merged meshes cast except the
  listed ground, water, lamp head, weed card and snow materials (intended: ground casts nothing useful, cards shadow
  badly); the Underheart casts only near the surface (budget, intended); particles and decals never cast. The new
  clutter casts and receives; decals and foliage cards receive only.
- Colliders: untouched. The rock and mesa colliders are built from the same vertex lists, which did not change.

## 5. Foliage and the wind request

Grass cards (parks, cemeteries) and scrub tufts (around every Badlands cactus and rock) use the existing weed cards
through `Mats.Weed`, both faces, up-facing normals so they take the ground light. Counts: about 2,300 grass cards and
1,200 scrub tufts on the default city, split across the tiers like everything else.

Request to the material pass (owner of `Mats` and `Tex`): a foliage material with a vertex-offset sway (world-space
sine on the card tops, amplitude 2 to 4 cm, phase from world position) that the weed cards, the tree canopies and the
palm fronds can share. The card mesh is ready for it: every card's top vertices are the ones with the larger local Y,
and a sway weight can be derived from UV y (0 at the ground, 1 at the tip) without a mesh change. Until then the cards
are still.

## 6. What could not be done, and why

- No primitive was replaced with a modelled asset. The directive asks for approval first; section 3 is that list.
- True heat shimmer (screen refraction) needs a distortion pass in `NeonPost` and its URP twin, which this phase does
  not own. The Underheart haze is an additive, nearly transparent rising puff over the lava and vents; it reads as
  warm air but does not bend the picture. Request to the post-processing pass: a refraction decal pass fed by the
  `HeatHaze` spots.
- Clutter has no colliders by instruction, so the player walks through a wheelie bin or a roof vent. It is kept out
  of walk lines, doors and quest points so that rarely matters; roof vents are the most likely place to notice it.
- Venue-interior light shafts are approximated by one dust volume per interior, placed in the view's frame rather
  than per lamp, because the venue lights are private to `VenueView04C`.
- `LocalQuiet` is a spawn-time rule for aggressive producers and has no geometry; static dressing is not a spawn, so
  it does not consult it. Underheart floor paths are respected through the existing dressing `Free` rule (doorways,
  anchors, hazards, lift cages); this phase adds no Underheart clutter, only particle spots.
- The one-line hook in `Combat/Fx.cs` (the Fx class lives there, not under Effects) is outside this phase's file list.
  It adds a same-frame secondary burst and changes no timing or damage; please review it with the phase.
- The facade decals assume the plain generated box. Buildings the art passes or venues replaced are skipped by
  `DressingPlanner`'s context; 02E and 04A themed buildings that keep the box footprint still get them.

## 7. What the owner must look at in Unity

Capture Golden Shots before and after (same resolution), then Compare. Then these, in order:

1. **Settings > Graphics = Low**, start Free Roam: no decals, no clutter, no roof gear, no new particles. F2 > Dressing
   must say every tier "not built" and 0 spots alive. This is the regression guard.
2. **High, `core_street` at 12:00 and 22:00**: grime at the foot of the walls, oil in the empty parking slots,
   manholes in the lane centres, worn paint and tyre marks at the signalled approach. Nothing should float, z-fight or
   sit on the crosswalk. Walk the sidewalk: no bottle or can under your feet on the walk line.
3. **High, `old_silver` and `ash_railyard` at 12:00**: poster scraps at eye height and water stains under the sills,
   cable runs at the wall foot, crates, pallets, tyre stacks and bins in the yards, a scorch mark or two in the empty
   lots. Check that no clutter sits inside a building or blocks a lot entrance.
4. **High, `skyrail_orbit` at 12:00** (the rooftop view): vents, pipes, air units, hatches and dishes on the plain flat
   roofs. Fly or leap onto one: the pieces are walk-through by design; say whether that bothers you.
5. **High, `glass_heights` and `strip_street` at 12:00**: planters at the tower bases and the Strip, not in the
   walkway; the walk-in venue doors stay clear.
6. **High, `grave_frequency` at 12:00**: grass cards in the cemetery and the park lots; **`desert_motel`**: oil at
   the motel and gas station, scrub tufts around the cacti and rocks.
7. **High, `core_street` at 22:00, stand still 20 s**: thin steam breathing out of a few curb drains; at least one
   facade sign flickers; neither should happen at 12:00.
8. **High, `frostline_outpost` at 22:00 and 12:00**: fine glittering snow motes drift around you while you stand on
   snow; they stop when you leave the snow.
9. **Enter the Veyra Lofts and the Cinder Diner**: slow dust motes hang in the room (denser in the Lofts); none
   outside the door.
10. **The Underheart, Basalt level and the Foundry Crown**: dust under the lanterns, warm rising haze over the lava
    channels and hot vents, embers drifting in the Crown chamber and the Regent arena, pale spores in the gardens.
    F2 > Dressing shows spots alive below the preset's maximum (14 on High).
11. **Ultra, then back to Medium without restarting**: the Ultra tier appears over a few frames (F2 > Dressing shows
    it building), and dropping to Medium hides High and Ultra at once. Medium should look like a light scatter, not
    empty.
12. **Shoot concrete, metal, wood, dirt and a window on High**: a second small burst follows each hit in the same
    frame (chips and dust, hotter sparks with a wisp of smoke, splinters, dirt, glass glitter). Compare with Low:
    only the old effect.
13. **Shading**: look at a street lamp pole, a palm trunk, a tree canopy and a Badlands rock up close at 12:00. The
    curved surfaces should shade smoothly instead of showing flat facets; the 4-sided posts and the radio mast legs
    still look faceted on purpose. If any rock looks "soft" in a way you dislike, say so: the rock is the one case
    where faceting could be argued for.
14. **Perf flythrough before and after on High and Ultra**: compare the averages and the p95; the dressing is about
    350 extra static renderers per tier (culled like the city chunks) plus up to 22 small particle systems.

Record the golden compare, the two perf summaries and anything from the list above that looks wrong; the planner,
budgets and materials are all adjustable without touching anything else.
