# Citadel Renaissance 03A2: implementation and verification

## Baseline and scope

Baseline is the user's uploaded working Strongholds source at 4a4c26443b8fda3dbfd30f09e612989f9bbd9f5e, art/crystal-fantasy-block-proof. The original StrongholdView.cs blob was recovered and checked against Git object c6e2466f4ef633a64f8f419fc6dd9322746fb0c4 before editing.

StrongholdView.cs has only four integration changes: make the class partial, call the optional decoration pass before the existing atomic visual/physics swap, add a successful-module identifier to the nearby core sign, and dispose the instance-owned art kit. The original collider calls remain identical.

No changes to StrongholdBook, StrongholdPlacement, StrongholdData, StrongholdSystem, TravelPowers, save files, catalog IDs, or the city generation pipeline are included. The existing three sites are redesigned rather than adding competing bases or another control system.

## New source files

- CitadelGeometry03A2.cs: twelve reusable mesh definitions, UVs, normals, winding and low-detail variants. Direct Mesh creation avoids imported-model Read/Write dependency.
- CitadelKit03A2.cs: per-fort source-mesh/material/texture ownership. Existing shader paths are reused through clones, never mutated globally.
- CitadelLayout03A2.cs: explicit exterior and module compositions. Initial Amethyst, Verdant and hostile Riftforge identities, plus the existing selectable Dawnstone theme.
- StrongholdView.Citadel03A2.cs: material-batched near/far rendering, original stone/cloth retinting, optional-failure fallback and one-time diagnostic.
- CitadelArt03A2Tests.cs: native EditMode coverage, included but not run here.

Each new C# file has a stable paired Unity .meta file. There are no new mutable static caches or runtime Update components. Lifecycle follows the existing fort rebuild and destruction path.

## Mesh inventory

| ID | Authored form | Main use |
| --- | --- | --- |
| arch | Two circular arcs meeting at a pointed apex, thick separated stone blocks | Layered entrance and rear skyline |
| column | Fluted tapered shaft with stepped base and capital | Gateway and perimeter |
| ring | Closed three-dimensional tubular band | Starcourt and landing-pad trim |
| crystal | Offset, double-terminated mineral | Pylons, core backdrop and Riftforge fins |
| root | Curved trunk, spreading roots and branching arms | Verdant center and planters |
| canopy | Folded, double-sided leaves along branches | Layered tree crowns |
| fern | Fronds with stems and paired leaflets | Courtyard planting |
| flower | Two rings of folded petals | Crystal blooms |
| banner | Subdivided draped cloth and notched hem | Faction-colored standards |
| fin | Curved structural rib | Gate and roof-line supports |
| plinth | Stepped polygonal pedestal | Outside planting beds |
| lancet | Faceted narrow pointed inlay | Small readable mineral details |

Detailed variants use more segments, flutes and leaves; the tiny lancet deliberately shares the same inexpensive topology. Source meshes remain readable for combining. Combined output meshes use OwnedMeshes and release their CPU copies after upload.

## Material approach and provenance

All new geometry and surface-pattern algorithms were authored in this pass. No third-party models or textures were downloaded or redistributed. The new maps are runtime-generated 256x256 RGBA maps with mipmaps: stone albedo, stone normal, woven sigil cloth, leaf veins and mineral variation. This is not a new set of high-resolution scanned materials.

The current Standard/optional URP-compatible materials are cloned through StrongholdAssets. Inherited emission and metallic maps are cleared before setting stone, cloth and foliage; glow is reserved for mineral accents. Existing terrain, fog, clock, camera grading, music and lighting are untouched. New decoration adds no additional real-time Light components.

## Performance guardrails, not measured results

The pass is limited to the existing three forts. Parts are combined by material into two LODs, with thresholds 0.095 and 0.008. Fine planting is omitted at Low quality and in the far LOD. The source kit is cached per fort and is reused when a socket or theme changes. There are no per-leaf GameObjects, extra colliders or per-frame decoration scans.

At most eight surface roles yield up to sixteen additional renderers per decorated fort for both LOD sets combined; only the selected LOD renders. Additional source/combined mesh memory and generated map memory are real costs. Startup time, draw calls, peak memory and frame times have not been measured.

## Preservation and limitations

The original six sockets, core/challenge points, fort IDs, flight multipliers, material inventories and reward guards are unchanged. Ornamental gateway columns remain outside the six-metre central approach, and the large compositions are designed for the already-checked plot. Native placement, visual overlap, player-camera clipping and actual driving still require local review.

The new structures are decorative exterior geometry. They do not create new landable roofs or claimable territories. Existing combat continues to use the original collision. In particular, the upper portions of decorative arches and branches do not gain collision in this art pass.

Sunscar's unowned Riftforge style is presentation only; it does not rewrite state.Theme, ownership, enemy species or faction reputation. Capturing it restores the already-saved selectable appearance on rebuild.

## Executed checks

The source audit performs lexical delimiter checks, confirms the exact baseline Git blob, reverses only the four intended StrongholdView edits to recover that baseline, compares collision-call text, checks geometry IDs and integration/lifecycle hooks, and checks a few independent placement equations. See BUILD_03A2_SOURCE_AUDIT.json and tools/check_citadel_03a2.py.

These checks are NOT C# compilation, execution of the mesh generator, a Unity playtest or a visual/performance review. No native Unity tests were run. The environment did not have a Unity Editor or C# compiler available.

## Native acceptance checklist

- [ ] Import and compile without new errors in Unity 6000.3.16f1.
- [ ] Run CitadelArt03A2Tests and the existing Strongholds tests.
- [ ] Visit Hearthspire, Galehook and Sunscar. Confirm the 03A2 Console line and core sign.
- [ ] Compare each original fort with the new exterior at identical camera/time settings.
- [ ] Claim, capture and change theme without losing previously owned forts or echoes.
- [ ] Cycle all four build modules, every rotation and Empty at all six sockets.
- [ ] Fly, land on the existing decks and leave through the gateway.
- [ ] Pause during a raid; resume, save and reload; verify rewards and flight bonuses.
- [ ] Repeat Play Mode with domain reload on and off; check for duplicate art or missing materials.
- [ ] Compare Low/High frame time and startup memory against the baseline before raising detail budgets.
- [ ] Verify ordinary city missions, driving and Discovery shrines remain unchanged.

## Technical references

Unity Mesh.CombineMeshes API: https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/mesh/combinemeshes
Unity Mesh.UploadMeshData API: https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/mesh/uploadmeshdata

These references describe API behavior, not validation of this project.
