# 04A city architecture and coverage

The existing city generator now selects the 04A architecture for every eligible ordinary building. Selected Asterfall, starter and district landmarks use the same composition with a richer near LOD and a cheaper far LOD. The existing landmark selection budgets remain in place. Ordinary pieces are merged into the existing 160 metre material chunks, with no per-window or per-roof-piece GameObjects.

The new architecture uses thick mineral piers, carved recessed arcades, windows capped at six metres rather than facade-height glass sheets, clerestories, layered wall shoulders, planted loggias, broad perimeter roof galleries and faceted rooftop chambers. Five deterministic compositions change the roof arrangement, asymmetry, window proportions and architectural rhythm. West, central, east and southern quarters use verdant, amethyst, dawnstone and obsidian palettes respectively. Shared paving, furniture and roof-service surfaces use the matching textured material palette.

The ordinary tree path uses the original tree positions and trunk collider dimensions with authored willow or cypress leaf meshes and branching trunks. Geometry joins the existing city chunks. No gameplay, NPC, road, parking, POI, save, flight, stronghold socket or crystal progression data is changed by this city pass.

## Measured planned coverage

`Reports/planned_city_coverage.json` was produced by executing the real, unchanged C# CityLayout generator for seed 61724 and the 04A eligibility rules. A small managed math adapter supplies the arithmetic API because the available older Unity reference assemblies cannot execute their native internal calls under the standalone .NET runtime. This is planned coverage, not a claim that Unity instantiated or rendered these objects.

| Existing archetype | Eligible buildings | Distinct deployed compositions |
| --- | ---: | ---: |
| Apartment | 140 | 5 |
| Brick | 272 | 5 |
| Casino | 25 | 5 |
| DarkGlass | 47 | 5 |
| GoldGlass | 29 | 5 |
| Hospital | 3 | 3 |
| OfficeGlass | 54 | 5 |
| Police | 3 | 3 |
| Stucco | 408 | 5 |
| StuccoPink | 147 | 5 |
| Warehouse | 191 | 5 |
| Total | 1319 | |

The generated layout contains 1323 building records. Four custom-authored Frostline warehouse records are deliberately excluded. All 24 helipad buildings retain their landing pads and receive facade treatment; their roofs receive no new ornament. The report also records coverage separately for each district/archetype pair, the exact seeds and bounds of all buildings, 397 road edges and 174 POIs.

There are 558 ordinary tree placements in the city: 254 tree spots plus 304 sidewalk-tree props. The 04A selection assigns 178 cypress and 380 willow canopies. The current authored low meshes and retained trunk/branch geometry total 757672 triangles for those placements. Palms retain their existing path.

## Geometry validation and cost

`Reports/city_geometry_counts.json` executes the actual `VisualRebirth04AArchitecture.Tier` and `Crown` composition, the original `MeshBuilder` CPU primitive methods and the original seeded tier formulas over all 1319 eligible buildings. Materials and resource reads are recording adapters; imported geometry comes from the shipped CMH1 files. The report records source hashes and the limitations of this extraction.

The complete ordinary-budget architecture contains 4331028 triangles, averaging about 3284 per building. The largest three-tier building contains 14646 triangles. These figures include the replacement shells and roof structures; they are not a net triangle increase over 02E. They exclude roads, retained roof equipment, strongholds, gardens, and the additional near meshes of selected landmarks. Including ordinary trees gives 5088700 triangles before those other systems. Unity frame rate, GPU cost, memory use and boot time remain unmeasured.

The full-city CPU geometry audit found:

- Zero nonfinite vertices or degenerate triangles.
- Zero vertex normals pointing against their triangle face.
- Zero vertices beyond the original parcel plus 0.13 metres (the 0.12 metre relief allowance plus numerical tolerance).
- Zero roof-ornament triangle bounds intersecting the central 4.2 metre landing square.

The roof test uses a conservative triangle-bounds overlap check. Roof galleries, turrets and spires are decorative. Their upper surfaces do not add usable platforms or collision. The existing physical roof deck and its central landing route remain authoritative. All actual world traversal, camera collision and landing behavior still require the Windows Unity checklist.

The source review led to a substantial second pass: broader roof chambers and galleries, smaller glazing and stronger stone structure. A full-city geometry audit then caught narrow-facade arch protrusions and excessive anisotropic scaling of smooth-normal vines. The final export narrows those arches and uses uniform vine scales. The subsequent full-city audit passed all the checks above. Offline images demonstrate the exported source geometry in a review rig, not Unity gameplay rendering or final in-game presentation.

## Reproduction and runtime evidence

With an installed .NET 8 SDK:

```text
python tools/run_city_coverage_04a.py --dotnet /path/to/dotnet --geometry
```

The command refreshes the coverage report, geometry counts, invariant checks and four actual generated parcel exports in `Reports/CityGeometry`. `--examples-only` refreshes those four exports quickly without replacing the full-city count report. An optional `--whole-city /external/scratch/path` writes the complete city geometry to an external review directory; this export is several hundred megabytes and is not a Unity asset or screenshot.

In Unity, look for `VISUAL04A_COVERAGE` JSON lines in the player log. Each district/archetype group reports seen, eligible, detailed, ordinary, fallback, excluded, protected helipad roofs and the deployed variant bitmask. The following summary includes art failures and tree counts. This runtime result must be checked against the planned report after import and startup on Windows.

If any required model, material or texture is absent, complete preflight fails before the existing facade is suppressed. The previous 02D/02E city architecture remains available. Turning off the 04A city or garden option selects the corresponding earlier path at the next world build. The 04A mesh/material caches and architecture preflight are reset through the existing game reset registry.
