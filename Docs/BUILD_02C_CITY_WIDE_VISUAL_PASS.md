# Build 02C — Living Crystal City (GitHub branch build)

## Implemented source changes
- `DistrictArchitecture.cs`: crystal-gothic facade selection now extends to a deterministic 112 m city grid (up to 35/80/145/210 buildings depending on Low/Medium/High/Ultra graphics), while retaining the original Asterfall and starter buildings. The existing EditMode synthetic selection test remains isolated via a >100-block guard.
- `DistrictArchitecture.cs`: skyline crests add mineral buttresses, crystal shards and crowns above the roofs of selected buildings.
- `CityBuilder.cs`: inner-city roads reuse the existing mineral promenade material. Original road geometry, collisions, lane graph, markings and vehicle handling remain unchanged.
- `CrystalGardenDressing.cs`: selected existing parks receive visual-only crystal gardens with a crystal tree, a central mineral heart and six smaller shards. No physics colliders, AI obstacles, mission triggers or player interactions.
- `CityBuilder.cs`: non-hero buildings inside the inner city can receive chunk-batched stone gables and three pointed crystal spires. No per-building GameObjects are created for these rooftop accents.

## Scope and limits
This is a larger automatic procedural **visual dressing** pass, not a replacement of every original mesh. It covers the inner city (roughly +/-800 m) while preserving desert/peripheral areas and unique sites. Buildings outside the high-detail facade budget retain their original walls with optional mineral gables and rooftop crystal spires. This is NOT yet equivalent to cinematic concept-art quality; hand-authored geometry, rigged creatures and high-resolution texture assets remain future work.

## Installation: only one pull, then Play
1. Close Unity. In GitHub Desktop, switch to `art/crystal-fantasy-block-proof` and click **Fetch origin** then **Pull origin**.
2. Open `C:\games\NeonSyndicate_LasVeyra` in Unity 6000.3.16f1.
3. Wait for Unity to import/compile. If there are red errors, STOP and send the Console screenshot; do not click Ignore.
4. Press Play and choose Free Story Mode. You do NOT need to place any prefabs or use the editor Art menu.
5. Explore beyond the original Gothic district: look for mineral roof gables, pointed crystal crowns, dark stone roads and small crystal gardens in city parks.

## Test checklist (not yet performed)
- [ ] Unity compilation: no red Console errors.
- [ ] Free Story boot and city generation complete.
- [ ] Walk and drive through multiple neighborhoods; check vehicle/AI routes.
- [ ] Gothic facades, ordinary rooftops and crystal parks render.
- [ ] No missing/pink materials or blocking screen overlays.
- [ ] FPS and RAM compared against previous build in same camera/quality settings.
- [ ] Missions, combat, interaction and save/load unchanged.
- [ ] Screenshot from the same street/camera position before and after.

## Performance guardrails
- City facade selection is limited by graphics preset.
- Ordinary rooftop decorations are merged into existing chunk meshes.
- Crystal park art uses a deterministic sparse distribution and LOD culling.
- No gameplay colliders or physics were added.
- **Potential risk:** increasing the number of detailed facades can raise city-generation time and memory. If startup slows, report the graphics preset, load time and memory usage. Reduce the landmark budget before changing gameplay code.

## Rollback
Switch GitHub Desktop back to `development/visual-renaissance-01c` (after committing or stashing any local edits). Do not merge this art branch into main until Unity verification passes.

## Deferred art tasks
- Replace conventional facade geometry with fully authored, UV-mapped original Gothic-crystal mesh sets.
- Build dedicated creature habitats, rigged creature models and animations.
- Add crystal-powered vehicle visual designs.
- Create custom terrain textures, murals, environmental storytelling and skybox.
- Profile city streaming and batch counts before raising budgets.
