# WR02-A foundry

This is a small external production toolchain, not a new Unity world, AI, damage or character system. The first two assets are original local authorship. Tripo is optional and was not connected. No paid generation was executed.

Blender 4.5.3 was used for production; Unity target remains 6000.3.16f1 and C# 9. The scripts run with automatic Python execution disabled. Inspect logs and exit codes. A failed staged output is not promoted over an existing derived delivery.

## Repeat the two local demonstrations

Compile inspect_fbx.c with the pinned ufbx revision 5955c5c0b042ac2dc6f32955ab206aaec0620cfc used by the repository. From the repository root:

```sh
cc -O1 -I/path/to/ufbx tools/foundry/inspect_fbx.c /path/to/ufbx/ufbx.c -lm -o /path/to/foundry-inspect
python tools/foundry/bake_materials.py
blender -b -Y --python-exit-code 2 --python tools/foundry/author_heroes.py -- --asset architecture --contract ArtSource/FoundryWR02A/CrystalBasilisk.original-contract.json
blender -b -Y --python-exit-code 2 --python tools/foundry/author_heroes.py -- --asset creature --contract ArtSource/FoundryWR02A/CrystalBasilisk.original-contract.json
blender -b -Y --python-exit-code 2 --python tools/foundry/process_asset.py -- --brief ArtSource/FoundryWR02A/SanctuaryArchive.brief.json --inspector /path/to/foundry-inspect
blender -b -Y --python-exit-code 2 --python tools/foundry/process_asset.py -- --brief ArtSource/FoundryWR02A/CrystalBasiliskCandidate.brief.json --inspector /path/to/foundry-inspect
python tools/foundry/check_motion.py --report Logs/Foundry/motion.json
python tools/foundry/seal_delivery.py
```

Existing derived outputs require --replace-derived. That flag only works while their recorded hashes still match; unrelated edits refuse replacement. Authoring source, input GLB, processed source, exact recipe, brief, materials, collider policy and final FBX hashes remain traceable. Export headers can include tool timestamps; a reproducible recipe is not a claim of bit-identical files across Blender versions.

The authoring .blend files retain semantic editable parts. The imported render files are batched by shared material/protection role. The processor preserves existing UVs, rejects nonfinite data and unweighted skins, repairs only documented static degeneracies, uses uniform metre transforms, keeps protected hero silhouette geometry, creates three LODs, normalizes/prunes reduced skin influences to four, and requires an independent successful parse before moving staged files into the asset directory. Skinned seams are welded before reduction and armature deformation remains after the geometry modifier. Animation sampling uses 30 Hz before GLB import, avoiding resampling drift.

The architecture uses the existing five FR01 material families. The creature adds only Hide and Keratin and shares AmethystBronze. Metallic/smoothness packing, normal-map import and material references use the existing Heart preparation lifecycle. No renderer migration is performed.

Collider declarations are separate candidates, never active Unity physics. The two demonstrations require ORIGINAL_AUTHORITY_ONLY, produce zero new colliders and preserve original H5/CreatureRig physics. Explicit future box declarations must have an owner, finite positive dimensions, containment and required portal clearance; they still need native acceptance. No automatic convex hull can fill a functional doorway. The original basilisk hit declarations are preserved in its collider manifest.

## Optional text/image generation

```sh
python tools/foundry/tripo_v3.py doctor
python tools/foundry/tripo_v3.py quote --brief ArtSource/FoundryWR02A/SanctuaryArchive.brief.json
python tools/foundry/factory_queue.py plan
```

Keys only come from the local environment. doctor has not established a live connection here. submit requires an exact separately approved request and credit ceiling; no approval file is shipped. fetch resumes an existing task instead of resubmitting it and stores no signed model URL. See Docs/WorldRewrite/WR02A/PROVIDER_STATUS.md.

Downloaded models remain quarantined under .foundry-local until their provenance and license are reviewed. Future licensed inputs use OWNER_APPROVED_LICENSED_INPUT with an exact source hash and reviewed license id. They are not relabelled CC0. The first two locally authored demonstrations use ORIGINAL_PROJECT_CC0.

factory_queue.py processes only approved local inputs, with an explicit Blender and independent inspector executable. It refuses batch processing until BOTH exact first assets have native import, motion/collision, ordinary gameplay and owner acceptance receipts. It never submits paid generation automatically. The proposed categories are briefs, not a generated library.

## Unity

Stop Play. Run Neon Syndicate > Live World > Prepare World Rewrite, then Neon Syndicate > Acceptance > Verify Fantasy World. The original PLAY FANTASY WORLD temporary session selects the prepared Archive crown through the existing WR01 transaction. Failed preparation or admission retains the original. The creature is a prepared skinned candidate only; the existing functioning creature is never swapped. Actual native results remain NOT RUN.
