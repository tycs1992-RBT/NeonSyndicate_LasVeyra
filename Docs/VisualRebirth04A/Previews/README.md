# Offline visual evidence

Every image here is a Blender render of the shipped source geometry. These are **not Unity screenshots**. Each image has that limitation burned into its footer. They demonstrate model shape, composition and approximate surface response; they cannot demonstrate Unity import, gameplay, traversal, frame rate, culling, actual post processing or renderer parity.

The fort renders read the same `Resources/VisualRebirth04A/Layouts/*.json` files that the game loads. Their new meshes come directly from the runtime CMH1 binary files. The original 02I physical fort and six default construction modules are included. The default module ornaments are extracted by executing `StrongholdLayout04A.SocketParts` with the actual default slot values. No castle composition is invented by the renderer.

The sanctuary views consume the game's `Gardens/sanctuary.json` and reproduce the two nine-metre-wide paving strips from `VisualRebirth04ANeighborhoods.BuildGarden`. The existing animated creature is omitted rather than represented by a substitute mesh. Garden candidate placement in the surrounding live world remains unverified.

`hearthspire_before.png` uses the actual 03A2 mesh generator and layout methods, executed through a managed recording adapter that writes their vertex arrays. The exact old base-fort `batch.Part` statements are extracted from `StrongholdView.Rebuild`. The adapter supplies vector arithmetic and mesh recording, not a functioning Unity runtime. This geometry export is not a Unity compilation test. Its source hashes are recorded in `preview_manifest.json`.

The cameras, light sources, plain review ground and Blender material response are an offline review rig. Fort and garden views omit their surrounding city. All views omit the player, NPCs, animation, collision, original core-orbit animation and Unity post effects. Materials use the shipped albedo, normal, metallic/smoothness and emission maps where available. Old 03A2 procedural surface maps are reproduced for the baseline, but inherited older accent materials are approximate. Do not treat these images as a pixel-accurate engine comparison.

## Evidence and refinement

The initial rendered pass exposed disconnected roof ribs and columns in the Hearthspire pavilions. The shipping layout was corrected so each roof rests on its columns, with ribs underneath. The first tree renders exposed sparse crown coverage. The original tree geometry was revised with overlapping leaf blades distributed along outer branches, while retaining bounded LOD geometry. The initial Sunscar render showed too much green copper patina on the extraction rig; its shipping material family was changed to scorched iron.

The final previews are regenerated from those source changes. A second ground-level check revealed that the original vault module included vertical arch piers; the module was rebuilt as overhead diagonal ribs terminating on the existing corner capitals. Sanctuary views also prompted denser layered planting in the four safe quadrants, while retaining the wide cross paths. The Hearthspire before/after flight pair uses the same camera, light and exposure. Other views intentionally inspect different regions, scale and viewpoints. The kit and tree catalog images show actual source meshes in a labeled review arrangement; those arrangements are not runtime locations.

## City architecture samples

The `city_spawn`, `city_verdant`, `city_dawnstone` and `city_obsidian` images render actual parcels from `CityLayout` seed 61724. The source exporter executes the real ordinary-budget `VisualRebirth04AArchitecture.Tier` and `Crown` methods with the real `MeshBuilder` geometry routines. The sample JSON records the original world coordinate, footprint, archetype, seed and triangle count. Each building is centered on the review ground solely for inspection; these are not screenshots of the assembled city or invented reference buildings.

The initial source render exposed an overly rectangular silhouette with giant pointed glazing and undersized roof ornaments. That evidence prompted a second source pass on ordinary city roof mass, facade proportions and structure. The final sample images document the resulting source geometry. Nearby buildings, terrain, actual streamed district LOD selection and gameplay visibility still require Unity testing.

`CitySource` stores the exact preview mesh snapshots. Whole-city `city_all` views load all 1,319 eligible buildings at the ordinary geometry budget. They omit roads, terrain, the capped high-detail landmark layer, strongholds and live gameplay; the closer flight camera intentionally frames the urban core. The 383 MB intermediate meshes stay outside the repository. The authoritative geometry remains the C# generator and the reusable source kit. Regenerate the snapshots with the city coverage/export tool when changing those methods, then render the corresponding city views again.

## Reproduce

Install Blender 4.x, Python with Pillow and NumPy, and .NET 8. The optional baseline adapter runs through direct Roslyn so it does not require a package restore. Set up a `dotnet` executable on PATH, or pass its full path to `--dotnet`.

```sh
python3 tools/render_visual_rebirth_04a.py --export-baseline --dotnet /path/to/dotnet
python3 tools/render_visual_rebirth_04a.py --views all --samples 64
```

`--views all` includes the four committed city sample snapshots when present. To regenerate the samples from edited C# and render the fresh exports directly:

```sh
python3 tools/run_city_coverage_04a.py --dotnet /path/to/dotnet --geometry --examples-only
python3 tools/render_visual_rebirth_04a.py --views city_spawn,city_verdant,city_dawnstone,city_obsidian --city-dir Docs/VisualRebirth04A/Reports/CityGeometry --samples 32
```

To regenerate the full-city export outside the repository and render both the overview and closer flight framing:

```sh
python3 tools/run_city_coverage_04a.py --dotnet /path/to/dotnet --geometry --whole-city /tmp/vr04a-world
python3 tools/render_visual_rebirth_04a.py --views city_all,city_all_flight --city-dir /tmp/vr04a-world --samples 32
```

Use a local temporary directory instead of `/tmp/vr04a-world` on Windows. The full export is about 383 MB, contains 4,331,028 triangles, and is intentionally excluded from Git. The source recipe and hashes are retained. Render workers default to at most four CPU threads; override with `--threads`. Use `--output-dir` for scratch review passes.

The recording adapter and intermediate meshes live under the operating system temporary directory. No Unity project files, imported meshes, scene files or save files are modified by the renderer. `preview_manifest.json` records the source layout hashes, geometry and texture hashes, cameras, render dimensions and samples. `visual_rebirth_04a_gallery.blend` is a 4.3 MB compressed gallery with all 28 referenced texture files packed, for inspection outside Unity; OBJ source meshes and the deterministic generator remain the engine-independent authoring deliverables.

The environment's Blender build does not include OpenImageDenoise. The renderer therefore keeps denoising disabled and uses ordinary CPU path tracing. Residual sampling noise is visible in some images. Blender rendering timings and memory use are not Unity performance measurements.
