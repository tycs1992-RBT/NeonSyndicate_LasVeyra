# 04I art and rig pipeline

The recovered Master Bible archive and its nested Complete Visual Reference Pack were read, including the category contact sheet and production briefs. They guide layered midnight fabrics, restrained mineral trim, crystal enforcement silhouettes, organic companion anatomy and deep supported urban architecture. The pack itself labels its boards/tiles as exploratory concept images, not rigged models. Existing Malik, cast, bond species and fusion identities remain authoritative. No collage crop, AI concept picture or unintegrated model is delivered as a game asset.

## Actual production path

`BodyBuilder.Build -> BuildAll -> BuildAscension04I` emits closed lofted coat tails and forged armor into the existing single weighted BodyData mesh. The head loft's surface function supplies the changed face planes; its original finite-difference frame solver recomputes normals/tangents from that surface. Coat tails blend pelvis/thigh weights, cuffs follow forearms and armor follows chest/spine. Original bone indices, rig bind poses, appearance/save fields, palette/detail material, finger consumers and gameplay hitboxes remain intact. Optional details are admitted only within the original 4200/8200/13500 crowd/standard/hero ceilings. Dense outfits may omit new optional pieces. Crowd detail 0 has no new geometry.

Corin's Tablet and Tamsin's Wrench costume profiles add a harness and curved work bib to their existing bone attachment builder. OwnedMeshes retains disposal; existing costume materials are reused. Each of these formerly empty torso layers can add one small costume renderer. This is source-authored costume geometry, not a new face rig.

`CompanionBodyFactory.BuildNow -> CreatureBuilder -> CompanionSculpt.Apply -> Anatomy04I` integrates species scutes, Quartz frills and Stormskimmer sails in the original single skinned creature mesh. Existing evolution 0/1/2 and bone indices drive the additions. Bounds include new surfaces. UID0 handler/event bodies use this same pipeline but remain separate from player bonds. No new creature skeleton or free elemental-drake roster is introduced.

`FusionFormGeometry.Build -> AscensionFairing04I` adds closed mineral cheek/radiator geometry to the existing first body part. No extra form part, rig, collider, ownership record or material slot is introduced. Low uses two rows, high five; original rider/leg/transformation envelope tests remain strict.

`VenueView04C.Initialize -> Ascension04I -> CityAscensionGeometry04I.Venue` builds pointed vault strips, supported portals, fluted jambs, overhead pipes and mineral relief directly into the existing per-material venue near/far batch. Existing original imported architecture and texture assets remain loaded through ArtInhabitedCity04C. Low selects the generated far geometry; other presets use bounded near detail and existing LOD switching. No new light/collider/navigation manager is added. The four venue IDs are lofts, dispatch, pressure and bondkeeper.

## Reproduction and limits

Run `tools/measure_04i_geometry.py` against the compiled assembly as documented in TEST_LEDGER.md. It executes actual builders, not substitute geometry or engine API stubs. Evidence/geometry.json contains the deterministic counts; Evidence/baseline-geometry.json is the same probe on actual 04H.

These are original procedural surfaces integrated with the game's established rig/material pipeline. No imported sculpted human, textured hair replacement, new animation clip, blendshape library, professional scanned skin or new FBX is claimed. Native deformation, weapon grip and reference-image parity remain unverified. A licensed Unity art pass should evaluate the documented close/mid/far views before further geometry is added.
