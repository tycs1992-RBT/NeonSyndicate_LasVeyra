# Optional imported art

ASSET REQUIRED. No professional FBX, texture or animation was supplied for 04O-D.

Use Malik, Dante, VeilglassCastellan or StormrootSovereign subfolders, each with Models, Textures, Materials, Animations and Prefabs. Keep the FBX and textures under existing Git LFS rules. Commit every Unity metadata sidecar.

Create an ImportedModelRegistry04OD asset at Resources/ImportedModels04OD.asset. Keep Enabled false until native acceptance. Original identities: malik, npc:Dante Rook, creature:veilglass_castellan, creature:stormroot_sovereign. The original pose, collision, weapons and health remain authoritative. A complete explicit bone adapter and required sockets are mandatory. The runtime rejects missing art, incomplete bones, gameplay scripts, physics, invalid materials or unsupported shaders and retains the procedural renderer.

The initial adapter follows all original pose bones; it does not claim retargeted authored animation or support arbitrary extra weighted bones. Death and ragdoll restore the original renderer. Native import, weapon alignment, expressions, LOD, graphics presets and performance remain NOT RUN. See Docs/CityRebirth04O/IMPORTED_CHARACTER_GUIDE_04OD.md.
