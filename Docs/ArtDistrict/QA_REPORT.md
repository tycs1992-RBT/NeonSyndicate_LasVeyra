# QA report: Asterfall art demonstration

## Release status

**Implementation and offline asset/source checks complete. Unity acceptance pending.** This ZIP is a complete updated source project, not a tested desktop build or a claim of final AAA fidelity. There is no Unity Editor, C# semantic compiler, shader compiler, target GPU profiler or playtest environment available in this session.

## Checks performed

| Check | Result | Evidence / limitation |
|---|---|---|
| ZIP foundation and file preservation | PASS offline | No original files deleted; 508 of 517 original files byte-identical, nine reviewed source changes |
| Protected gameplay comparison | PASS offline | 153 protected original files unchanged, including layout, input, camera, UI, save and gameplay content |
| OBJ structure and geometry | PASS offline | 23 assets; 10,270 unique triangles; finite data, valid indices/UVs/normals, nonzero triangle areas, valid winding |
| Texture integrity | PASS offline | All 32 PNGs fully decoded, 512px dimensions and metadata checked |
| Asset manifests | PASS offline | 55 model/texture hashes verified against shipped data |
| Normal/mask data | PASS offline | Normal-vector lengths and complementary roughness/smoothness channels checked by texture authoring validation |
| C# syntax | PASS parser only | Entire current Assets C# tree parsed without syntax errors; not type checking or Unity compilation |
| Integration review | PASS source review | Existing tier colliders retained, helipads excluded, art fallback before collision emission, atmosphere applied through owner |
| UI/input isolation | PASS source review | New world art adds no Canvas, OnGUI overlay or input binding; runtime readability still requires playtest |
| Unity import / EditMode tests | NOT RUN | Required before accepting the release |
| Play Mode / save / story regression | NOT RUN | Must compare against original project in Unity |
| Built-in and optional URP rendering | NOT RUN | GPU shader compilation, player variant retention and look development unverified |
| Desktop frame rate / memory | NOT MEASURED | No claimed fps, hardware minimum or measured draw-call budget |
| New gameplay screenshots | NOT CAPTURED | Included contact sheet is software-rendered OBJ inspection, explicitly labeled not Unity |

Four initial PNG files failed full decoding during validation. They were regenerated with atomic writes and verified again; the shipped maps pass. Review also caught and fixed an enum/field name collision, an emission importer mismatch, helipad exclusion, failed-art boot behavior and runtime shader-variant retention.

## Automated verification included

Run from the project folder with Python and Pillow installed:

```sh
python tools/validate_art_district.py --baseline /path/to/original.zip --report /path/to/new-report.json --diff /path/to/new-diff.txt
```

Omit `--baseline` if only validating assets. `OFFLINE_VALIDATION.json` and `BASELINE_CHANGES.diff` record the final package checks. The C# syntax parser is an additional delivery check using tree-sitter; it is not a Unity dependency.

In Unity, open Window > General > Test Runner and run the existing EditMode suite plus `ArtDistrictTests` and `QuartzRegaliaTests`. The new tests cover district boundaries, helipad/Manor exclusion, imported mesh channels/readability/no-colliders, shared material maps, dragon rig/collision/mesh invariance, skin weights, deterministic generation, combat pose preservation and locomotion finite values.

## Required Unity acceptance pass

1. **Clean import:** open Unity 6000.3.16f1, let packages resolve, confirm zero Console errors. Confirm model orientation, material retention generation and no magenta assets. Run the full test suite.
2. **Default seed / scope:** use Free Roam and the editor visit command. Confirm both street frontages are upgraded; inspect the edge of the demo district. Confirm surrounding city and story sites retain their original generation.
3. **Collision / movement:** walk and sprint past every frontage, use cover, fire at building surfaces, drive both lanes, use flight/leap/roofs, and test parking/traffic. Decorative roof and facade pieces are visual only and can be penetrated by players/projectiles; approve those visible mismatches or add a separately reviewed collision pass later.
4. **Cameras / HUD:** compare first-person, third-person, aim and vehicle views at 1080p, 1440p and an ultrawide aspect. Inspect canopies, balconies, crystals and corners for clipping. Verify the HUD, quest text, reticle, menus and prompts remain readable.
5. **Lighting:** capture clear noon, golden hour, night, rain, sandstorm, Frostline and Veil transitions. Confirm local haze does not accumulate, the rest of the map is unchanged and original weather still dominates. Check that normal maps and emissive crystals remain legible.
6. **Dragon:** compare upgraded regular species and the roof exhibit. Inspect folded wings, flight, bite, breath, roar, hit flashes and death; verify original weak points and hit volumes. The exhibit must not register as an enemy, accept hits, give loot or create a companion UI.
7. **Performance:** record baseline vs district on the same desktop. Use Profiler, Frame Debugger and the existing QA perf tools. Record 60-second median/p95 CPU and GPU milliseconds, draw calls, triangles, GC allocation, peak memory and boot time. Inspect LOD changes while approaching in both camera modes. The intended 60fps target is not a measured result.
8. **Lifecycle:** enter/exit Play Mode repeatedly with domain reload on and off; start new games, load an existing save, pause, return to title and re-enter. Check for leaked materials/meshes/lights or stale particle limits.
9. **Standalone player:** build Windows and test the above outside the editor. Confirm packed surface shader variants are present. Use the existing build scripts and preserve their logs.
10. **Optional URP matrix:** after Built-in passes, use the supplied migration workflow and run the same tests. The project requires the original twelve editor/player, input-mode and renderer configurations; none was compiled here. The pre-existing unconditional optional assembly references also need verification on clean package import.

## Known limitations

- No measured performance result, shipping build, engine screenshot or in-engine visual approval is supplied.
- Geometry is a deliberately bounded modular demonstration. The rest of the world is not visually transformed.
- Existing humanoids and original Manor/story logic remain. Future production art needs are listed in ASSET_INVENTORY.md.
- The dragon remains an improved procedural skinned asset; the rooftop display does not implement taming, traversal or companion AI.
- New haze is distance fog/ambient tuning, not volumetric fog or baked global illumination. Glazing is opaque. New crystal motes are ambient effects, not a replacement for all spell casting/impact systems.
- LODs use a hard switch for cross-renderer reliability. Their transition distances require in-engine approval.
- Source preservation and syntax success cannot establish that the complete project compiles or that all gameplay remains working. Unity testing is the final acceptance gate.
