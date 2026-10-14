# Crystal Gothic Proof Kit — Build and Review

## What is implemented
An editor-only generator at `Assets/NeonSyndicate/Editor/CrystalGothicKitBuilder.cs` creates original reusable mesh assets, materials and a `CG_ProofBlock.prefab` consisting of faceted mineral buttresses, a gilded pointed-arch facade, an amethyst crown and crystalline trees. This is an **art proof**, not a full city replacement.

Generated assets are created inside `Assets/NeonSyndicate/Art/CrystalGothicProof`. They are saved as ordinary Unity assets so they can be reused and committed to Git. No third-party models or textures are required or claimed.

## How to generate and preview in Unity
1. In GitHub Desktop, switch to `art/crystal-fantasy-block-proof` and pull latest changes.
2. Open the SAME local project in Unity 6000.3.16f1. Check Console for compile errors before continuing.
3. Choose `Neon Syndicate > Art > Build Crystal Gothic Proof Kit`. Unity will generate 4 mesh assets, 5 materials and 1 prefab.
4. Open the city scene. Move Scene view to the facade you want to transform.
5. Choose `Neon Syndicate > Art > Place Crystal Gothic Proof Block In Scene`.
6. Select the newly placed `CG | Crystal Gothic Proof Block` and move/rotate/scale it alongside a building frontage. It is visual-only, with no colliders.
7. Inspect from street-level and drive around it. Save the scene if you approve the placement.
8. Commit the generated mesh/material/prefab assets and any approved scene changes to this branch through GitHub Desktop.

## Important limitations
- The generator has not been compiled or run in a Unity Editor by the assistant. It must pass local compilation.
- The new prefab is a single proof facade; it does NOT automatically replace all city buildings.
- No production-quality rigged creatures, interiors, custom texture maps or optimized LOD meshes are included.
- Crystal emission may require bloom/postprocessing to visibly glow, but the generator does not change camera postprocessing.
- The builder deletes and recreates generated mesh assets on repeated builds; avoid hand-editing those generated mesh assets.
- No original gameplay scripts, world coordinates, colliders, navigation, traffic or mission triggers were modified.

## Approval gate
Capture before and after screenshots from the same position, verify no new Console errors, and confirm the street and vehicle routes still work. If the proof feels too geometric, request a new asset-modeling pass before applying it city-wide.

## Next development work
Create refined authored facades with UV-mapped surface textures, asymmetrical silhouettes, proper LOD meshes, environmental storytelling and a dedicated creature sanctuary. Integrate only after this proof is reviewed.
