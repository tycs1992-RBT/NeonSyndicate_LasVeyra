# Actual 3D source, provenance and DCC bridge

New geometry is original C# authored in this sprint, in the existing project mesh/skin owners. No paid, extracted-game, stock FBX, concept-crop billboard or generated reference image was imported. Existing repo assets remain under their original provenance. No Blender executable is installed on this host.

Runtime path: `Appearance.Malik` / existing cast and roles -> `BodyBuilder.Build` -> `BuildMotion04K` -> existing BodyFactory/HumanoidRig. The skin shares UV0 palette, UV1 detail, normals, tangents and existing bone indices. Hero stays below 13,500 vertices, standard below 8,200, crowd unchanged. Measured Malik hero: 12,988 vertices / 18,092 triangles, standard 7,711. These counts establish integration/budget, not visual fidelity or frame time.

Creatures: `CreatureBuilder`/`CreatureSculptor.AnatomyMotion04K`; winged anatomy precedes optional Quartz Regalia so the original prefix-preservation contract still passes. Mites retain shared swarm bodies. CompanionBodyFactory -> CompanionSculpt -> Expression04K uses existing saved evolution 0–2 and bone chains. No new skeleton or collision volumes are authored. Detailed budgets and all nine stage exports appear in Evidence/geometry.json and art-export.json.

Named cast: ServiceTorso04I now adds a rounded dispatch yoke for the Tablet costume and asymmetric maintenance shoulder/instrument loops for the Wrench costume. These are the existing Corin/Tamsin mesh batches, with OwnedMeshes lifetime and shared materials.

## Reproducible export

Run `tools/export_motion04k_art.py --dotnet <dotnet8> --assembly <verified-game.dll> --unity-references <exact-6000.3.16f1-managed-modules> --output <review-directory>`.

The exporter was compiled and executed here. It exported Malik and three species x three stages: **10 actual weighted skins**, each checked for stream lengths, normalized weights and valid indices. Palette colors, UVs, detail UVs, joints, weights, skeleton names/parents/world positions and triangles are inspectable JSON. No runtime files are replaced by these exports.

For an editable handoff: `blender --background --python tools/import_motion04k_blender.py -- <skin.json> <review.blend>`. The bridge creates mesh, palette material, UVs, vertex groups and armature. The Python source syntax was checked; **Blender execution, FBX export and Unity reimport were NOT RUN**. Sculpt/retopologize/texture there, preserve scale/axes and original ownership/anchors, export FBX with original bone names, add eligible binaries to LFS with stable .meta, then test native import/deformation before replacing a runtime owner. No professional imported model or authored clip is claimed.
