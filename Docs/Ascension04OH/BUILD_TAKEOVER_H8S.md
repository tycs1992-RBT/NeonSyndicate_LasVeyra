# Living Cast and Crystal Frontier: complete builder takeover

Copy the instruction below into the next builder session, or direct the builder to read this entire file. This is an implementation order and a factual handoff. Preserve newer work if the branch advances.

## Your role and deliverable

You are my continuing senior Unity game-development studio and hands-on builder for Crystal Manor Saga / Neon Syndicate: Las Veyra. I am a non-programmer. Take responsibility for engineering, character modeling and rigging, animation, environments, combat and creatures, mission integration, UX, performance, builds and QA. Make actual code, editable 3D art, runtime assets and playable changes. Inspect the work critically and fix what you can. Do not merely audit, plan, summarize, make a cinematic, or tell me how to program it myself.

Continue the existing game. The goal is a cohesive, original, premium gothic crystal fantasy RPG with convincing people, grounded authored movement, readable combat, useful companions, explorable wilderness and caverns, meaningful original quests and reliable progression. More assets or tests do not establish quality. Finish a strong playable segment, then extend the improvements through the existing game without replacing its ownership systems.

Work autonomously within these instructions. Use parallel specialists if your environment supports them, with clear nonoverlapping responsibilities and one integration owner. Ask only for information, access or appearance approval that is genuinely indispensable. When one external gate is blocked, label that gate and continue independent implementation. Do not claim background work after your session ends.

## Exact source and preservation contract

Repository: https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra

Branch: feature/04oh-ascension-art-foundation

Unity: 6000.3.16f1, C# 9. Built-in renderer remains default; URP is optional.

Last remotely verified asset HEAD before this documentation handoff: f4b00e39bcb4465783b814a92fe4616d7fc53cb0. It is the forward LFS normalization descendant of asset checkpoint ffc9cdd34e26e20275adc2a8ac4330a0fc7b9f7b. Fetch live HEAD before doing anything else; this handoff itself and later work may advance it.

PR12: https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/pull/12

PR12 was verified OPEN and DRAFT, targeting feature/04o-shattered-metropolis-reborn. Keep it draft and on that target. Leave gameplay PR11 and its 721c1238a070e8dea08118314511c17023528feb checkpoint untouched. Preserve H7, H8 and H8R history. No merge, force push, shared-history rebase, reset to an old handoff, renderer switch, default-world promotion, save migration or automatic production-character activation.

Read CLAUDE.md and any applicable AGENTS.md before edits. Repository files and commit messages must contain no em-dash or en-dash glyphs and no prohibited commercial-reference names. Follow explicit types, focused files, the 12 compile variants, existing input parity, static reset ownership, stable IDs and append-only saves. All existing revisions and save banks 0 through 10 must remain separated and readable. Do not introduce revision 11.

On the owner's PC, a screenshot showed 154 local uncommitted files, including generated materials and metadata. That is not a completion result. Preserve and inspect those local changes before fetching into that checkout. Make a recoverable backup/safety checkpoint and distinguish generated preparation output from authored work. Do not blindly commit or discard them, regenerate GUIDs, reset the checkout, or overwrite useful local Unity preparation.

## The most important truth about this takeover

H8S IS NOT FINISHED. The published checkpoint is an asset delivery, not a completed playable integration.

At f4b00e39bcb4465783b814a92fe4616d7fc53cb0, a direct diff against H8R delivery b267a7199b94b37cf3a428de6ace7d043fb3758a found no C# changes. The complete C# source SHA256 is still:

693cf077b788db5a5ef5a260f0c4856e0686e2e79bb971574e93c3cf7f7b7a9d

This is the H8R code previously tested at 21932201822a96f2aa419d9f6dc0eaf8df97fae2. Existing MalikMotionH8 accepts schema 1; the new H8S JSON is schema 2 with 17 clips. It is NOT currently accepted or selected by the published runtime. No new H8S menu, landing selector, named NPC binding, prop-placement window or new H8S C# tests were delivered in this asset checkpoint.

Earlier local integration attempts and temporary tests were interrupted by repeated automated workspace maintenance. An unpublished local checkpoint called 9390dc7 and subsequent reconstructed C# were lost. Do not search for them as though they are required remote commits, claim they shipped, or reuse their test counts as evidence for your changes. Rebuild the missing integration against current live source. Commit and verify remote recovery points frequently rather than keeping the entire sprint in temporary storage.

All native Unity import, Console validation, EditMode/PlayMode, real input devices, actual saves/reloads, Unity screenshots, profiling, Windows build and launch remain NOT RUN in this handoff. Owner appearance approval is NOT GIVEN. Native execution cannot be inferred from managed Unity reference assemblies.

## Read the project, narrowly but sufficiently

Read START_HERE_04OH.md and the current first section of RESUME_NEXT.md, then:

- Docs/Ascension04OH/BUILD_TAKEOVER_H8S.md, RECOVERY_H8S.md, CONTINUATION_H8S.md, ASSET_INTAKE_H8S.json and LICENSE_RECEIPT_H8S.md.
- CONTINUATION_H8R.md, FEATURE_STATUS_H8R.md, INTEGRATION_H8R.md, TEST_LEDGER_H8R.md, OWNER_GUIDE_H8R.md, NATIVE_ACCEPTANCE_H8R.md, VISUAL_EVIDENCE_H8R.md and DELIVERY_H8R.json in the same directory.
- H8_FIRE_RECOIL_CONTINUATION.md, relevant H7/H8 motion contracts, art manifests and the current 04O world handoffs.
- Docs/CONTROLS.md, CREATURES.md, TRAVEL.md and QUESTS_AND_EVENTS.md.

Some earlier H8S handoff text predates the final asset exports and still says exports are open. The files and receipts at the verified asset HEAD establish that exports were subsequently delivered. They do not establish runtime integration or native acceptance. If the original 00_START_HERE.md, 02_MASTER_SPRINT_04OH_H8S.md, 03_THREE_ARCHIVE_INTAKE.md and 05_OWNER_PLAYTEST.md package is present, read it too. A prior worker did not receive the separately named package files and instead followed the complete consolidated directive pasted by the owner. Do not invent their contents or stall independent work when they are absent.

Map exact live owners before editing. Relevant existing paths, relative to Assets/NeonSyndicate/Scripts:

- Runtime/Characters/PlayerController and its Travel partial own movement and physical travel.
- Runtime/Characters/HumanoidPose.MalikH8.cs and MalikMotionH8.cs own authored sampling through the original pose solver.
- Runtime/Characters/HumanoidAnimator.MalikH8.cs owns explicit full-body review assignment.
- Runtime/Characters/ImportedVisual04OD.cs, ImportedVisual04OD.ReviewH6.cs, ImportedBindH3, RigLayout, SkinWeightsH5 and existing weapon IK/socket owners own the skin adapter and bone policy.
- Runtime/Characters/MalikReviewH8R.cs contains the exact selected-human review contract.
- Editor/ProductionMalikH6.cs and its H8/H8R partials prepare and inspect real candidates. Editor/WorldArtReviewH8R.cs owns the existing art overlay. Reuse these entry points and preserve H6/H7/H8/H8R reviews.
- Runtime/Gameplay/Guidance04E/CampaignDirector.cs, CastRoster04E.cs and CastCostumeBuilder04E.cs own canonical actors, roles, dialogue staging and costumes.
- Runtime/World/CityLayout.Fractured04OC.cs resolves realm anchors. Existing world builders/colliders and NPC navigation remain authoritative.
- Original TravelPowers, companion eligibility/impact, quest runner/receipts, CraftingSystem, camera, rendering and save owners must be located and retained.

Do not create a second player controller, gameplay-root Animator, root-motion motor, camera, collision authority, attack event, resource spender, reward wallet or companion unlock system.

## Assets that actually exist

### Owner-selected Malik

The canonical supplied foundation is the owner's version-five human. Do not silently substitute an older version-four upload, a mannequin, a donor base character or a new generic hero.

Preserve ArtSource/AscensionH8R/MalikVeyrH8R.blend. The newer derived candidate is ArtSource/AscensionH8S/MalikVeyrH8S.blend, SHA256:

0180aa40897a6ef631122452d444274c0767b67d2cecfd837f2fa37a6365e969

It retains the actual Malik body, 53 source bones and 47 gameplay semantic mappings, selected skin, fitted eyes, grooming, project wardrobe and restrained mineral detail. H8S adds a continuous rolled collar and neckline edits, plus two supplied retargets. These edits are already in the source; do not blindly apply them twice.

Actual exports live under Assets/NeonSyndicate/Art/ProductionH8S/Models/:

- MalikVeyrH8S.fbx, MalikVeyrH8S_LOD1.fbx and MalikVeyrH8S_LOD2.fbx.
- export-stats.json records real vertices, triangles and material batches.

The hero retains candidate ID MALIK-H8R-REVIEW-01. The H8S candidate.json records its exact derivative export path, source and file hashes. Keep the candidate disabled and ownerApproved false. The ordinary actor remains fallback until later explicit approval.

Textures are in ProductionH8S/Textures. Matched real Blender renders are in Docs/Ascension04OH/EvidenceH8S/SourceBefore and SourceAfter. They are SOURCE 3D RENDERS, not Unity screenshots or acceptance. Inspect both images and manifests. Visible quality remains unfinished: groom can read as bristles, facial/eyelid/skin response needs attention, and collar/lapel/garment surfaces still need critical inspection. The existing hero LOD0 is expensive, particularly groom and cloth; measure and optimize instead of calling it a production budget.

### Actual motions

ProductionH8S/Animations/MalikFullBodyH8S.json contains 17 clips and 701 samples, schema 2, same Malik candidate ID. The source contains the existing 15 actions plus:

- H8_Landing_Supplied, 39 samples, approximately 1.2667 seconds, non-looping.
- H8_Listening_Supplied, 76 samples, 2.5 seconds, closed loop.

Both have individual actual FBX exports. Donor actions are NinjaJump_Land and Idle_FoldArms_Loop from the non-root-motion supplied animation FBX. The derivative uses complete matching 53-bone rest-axis quaternion retargeting, zero translations and unit scales. Do not adopt donor root curves as player displacement. Do not confuse a loop source with a fully integrated NPC gesture; original hand/tool contact may require torso-only presentation.

H8R already has guard, locomotion, sword, dodge and partial cast/aim/recoil/eligible companion art through the original owner. Four jump/Surge/flight actions remain source-only. Do not claim all 17 actions have gameplay playback.

Delivered reproducible export tools: tools/export_selected_malik_h8s.py, export_motion_h8s.py, export_forge_cast_h8s.py and export_forge_props_h8s.py. Inspect arguments and receipts before running. Editable source is delivered; not every interrupted authoring script was published. Do not execute scripts bundled in third-party archives.

### Distinct NPC candidates

H8R studies remain at ArtSource/AscensionH8R/GuildArtisanStudy.blend and CourtDefenderStudy.blend. The guild study is FEMALE; the court study is MALE. Both retain 65 weighted bones, including real weighted terminal bones. Do not substitute the female study for a canonical male character or strip weighted leaf bones for convenience.

ArtSource/AscensionH8S/DanteRookH8S.blend is the distinct male study fitted with work shirt/trousers, closed boots, curved forge apron, long hair and beard, with normalized forward orientation. Its source SHA256 is f763a694c36fd6d88eb099f69d5d97a88261908fef97b4f983c3c31ab367013a.

ProductionH8S/NPC contains DanteRookH8S.fbx and two LODs, real skin/eye/normal textures and candidate.json. Candidate ID is DANTE-H8S-REVIEW-01, disabled/unapproved. Exported vertices are 16614, 10033 and 5068, with 9 skins at each LOD. These are source counts, not native draw calls or a crowd budget. Anatomy remains stylized and unapproved; improve it without face-cloning Malik.

Canonical actor is CastIds04E.Dante, exact stable value dante. Campaign.CastActor(CastIds04E.Dante) returns the original actor. Canon is male, bearded, long-haired forgewright at the Forge, with established story gate, apron/tool role and original hammer. Bind visual-only to that actor after verifying source. Do not spawn a substitute NPC. Keep dialogue, quest giver, faction, schedule, inventory, navigation, damage and service permissions intact. The second female candidate is conditional on a compatible verified identity, not mandatory filler.

### World and props

H8R's six original modules remain SanctuaryPortal, ProcessionalVault, SanctuaryStair, ShardwoodElder, RootMineralBank and RootSpringArch under ArtSource/WorldFoundationH8R and the matching Assets art folder. Existing H8R review places four decorative overlays. It is not a physical cavern or wilderness rebuild. SanctuaryStair was intentionally not placed; do not place it without genuinely climbable geometry and native grounding/camera proof.

ArtSource/AscensionH8S/ForgeGalleryFurnishings.blend is editable source for three curated furnishings. ProductionH8S/Props contains Anvil.fbx, Workbench.fbx, BookStand.fbx, real metal/furniture base-color, normal, ORM and derived metallic/smoothness maps, plus candidate.json. Packed conversion is metallic.R = ORM.B, packed alpha = 255 - ORM.G. Base color is sRGB; normals and packed channels are linear. Use the existing Built-in/optional-URP material owners.

These props are not placed in published gameplay. An intended narrow integration is an anvil visual fitting within the existing Forge collider, plus decorative workbench/book stand at safe gallery positions. Verify native scale and all rendered-bound corners before hiding original art. The original Forge station was observed as Forge (Dante Rook's forge), with a BoxCollider centered (-.2,.45,.05), size (1.5,.9,.6). A proposed anvil scale .7 and station-local height .5 needs real verification, not blind acceptance. Preserve original station functionality and collider. IgnoreRaycast decorative visuals must not steal E/Y interaction, camera collision or line of sight. Cleanup must restore original renderer states and remove every temporary object, including on partial setup failure.

## Archive rights and missing-file policy

The exact three inspected archives are:

| Archive | SHA256 |
|---|---|
| Universal Animation Library 2[Standard].zip | 4008ea208a604773a2b2177d965f0f5d3195498b5bf838c3f5785d68e95f2a68 |
| Universal Base Characters[Standard].zip | fdbf1804c90dfc1ea03e992bff7da2dfd1a79318e13270a660180f9308455f40 |
| Fantasy Props MegaKit[Standard].zip | 8b6f7e806d222e585478f0e1bdc6b271bbc7bc6f84dd6af8ca703a7c64f0cb1e |

Actual root licenses were inspected and recorded as CC0-1.0 by Quaternius. Read the intake and license receipt; possession or the word Standard alone was not the basis for admission. Preserve all provenance. Only the curated derivatives above were admitted; remaining archive contents are not automatically approved production content. Do not publish whole ZIPs, execute bundled code, buy assets, circumvent licensing or assume every future source has the same rights.

Exact useful member paths include the animation archive's Unity/UAL2_Standard.fbx, the base archive's Hairstyles/Origin at 0/FBX (Unity)/Hair_Long.fbx and Hair_Beard.fbx, and the prop archive's Exports/FBX/Anvil.fbx, Workbench.fbx and BookStand.fbx. Root Textures files differ from duplicate exports and alternate-engine normal maps. Match exact paths/hashes and validate dependencies. Quarantine selected extraction; reject traversal, links, dangerous names, decompression bombs and conflicting licenses. Preserve every skin influence and weighted accessory bone.

Locate the original archives in the owner's attached files or accessible folders. If unavailable, request only the exact needed archive while continuing with published editable sources, exports and code. Existing derived assets do not require wholesale re-import.

## Execution order and acceptance

1. Reconcile live HEAD and local work. Pull LFS, distinguish real binaries from pointer text, inspect receipts and all .meta/GUID relationships. Do not render or parse pointer files as assets. Record an ownership matrix and current native capability. Keep the intake short enough to implement useful work.

2. If a licensed usable Unity 6000.3.16f1 Editor is available, open the unchanged project first and record the FIRST real Console/import/compilation failure. Fix that before expansion. Do not repeatedly retry the same licensing/UPM IPC blocker without a material changed condition. Continue source/DCC work when native is blocked; mark each native acceptance NOT RUN and provide a no-code Windows checklist.

3. Critically finish the actual selected Malik. Inspect neutral front/profile/three-quarter and real head-turn, run, planted stop, sword sweep, guard, dodge, two-handed gun, cast and flight poses. Improve eyes, hair/beard, skin, seams, collar/hem and equipment intersections with actual geometry, weights, corrective behavior and materials. Preserve complete influences and all 53/47 mappings. No glowing skin or camera concealment. Only crystals, selected trims and appropriate focus details emit. Respect reduced effects. Prepare the new export through the existing reversible review, with original fallback normal.

4. Integrate a small curated motion set into the existing H8 owner. Preserve schema-1 compatibility and reject invalid schema-2 data, wrong IDs, malformed tracks, events and root motion. A landing should trigger only after real airborne time followed by the grounded edge, blend in/out over a finite window and yield immediately to death/ragdoll/override, travel, combat, guard, dodge, reload, aim and other unsafe interruptions. Keep original feet, hand contacts and motor authoritative. NPC quiet motion must require the exact eligible actor and yield to original dialogue/head-look/gesture ownership. Validate source hashes and duration, quaternion interpolation, loop closure and missing-track fallback. No per-frame allocations or a second bone writer.

5. Prepare and bind Dante's visual through the original adapter in an explicit opt-in review. Retain exact canonical identity and brain. Share one actual skeleton across three LOD skins, retain weighted passive paths, disable imported Animators/controllers/root motion, validate sockets, native axes and complete weights. Original four-weight rejection is not permission to prune. Use transient Unlimited review only if needed, with restoration on every exit. Handle original costume overlap without hiding the hammer or functional components. Use distance/presence policy for this candidate, not global crowd replacement. Confirm real E/Y dialogue and Forge service.

6. Build one coherent route in existing Gothic Civic H5, revision 10. Favor home/court, Forge/gallery, Amethyst investigation, Azure shrine/root spring and safe Forge return. Upgrade vaults, stone, foliage, roots, crystals and cavern treatment with a coherent palette and good silhouettes. Existing floors/collision remain authoritative until a tightly scoped replacement has native traversal/navigation proof. Floating overlays remain VISUAL PREVIEW. Check slopes, stairs, ground probes, camera, quest triggers, NPC paths, creature territory and line of sight. Native proof, rather than decorative prop count, establishes a physical rebuild.

7. Complete the original Hearth Witness loop where legitimately eligible. Quest file is Assets/NeonSyndicate/Resources/Quests/ascension_h3_hearth_witness.json, ID fg_04oh_h3_hearth_witness. Its revision-9 requirement already matches revision 10 through existing QuestLogic.WorldMatches; do not rewrite it to create a new quest/bank. Route uses original Forge giver, Realm:hearth-lens, Realm:oath-witness, Realm:storm-shrine, Realm:storm-spring and Venue:forge.exit. Existing finite counts are two glass mites with maxAlive 1 and one regional jackal with maxAlive 1. Verify actual prerequisites and resolver before play. Use a legitimate eligible slot if already completed; never reset receipts or grant equipment/companion rights for a demo. Reward remains cash 60, lumen 12, glass_chitin 1, seed_core 1 and original forgewright reputation 1, under existing owners. Preserve M00-M11, Luma, both First Laws, all item/focus/boss/companion IDs and permanent equipment.

8. Play real alert/windup/dodge/guard/cast/sword/gun contact, damage, death/despawn and return. Companion art requires legitimately earned eligibility and exactly one existing impact/cost. Save and reload through the original bank, confirm no duplicated reward or lost equipment, keyboard/gamepad parity and safe failure/retry. A 4-to-8-minute coherent segment is the first quality target; measure actual timing instead of claiming it. Provide a 60-to-120-second newcomer capture and a complete-route acceptance script. No cinematic or manual fake replaces gameplay.

9. Extend demonstrated improvements through the existing game: stronger named-cast presentation, role-aware idle/conversation and existing schedules; consistent architecture and regional materials; readable creature tells and finite encounter pacing; clearer original quest guidance, focus/companion readability and camera/lighting/accessibility. Audit bosses, equipment, interiors, wilderness and progression for real friction. Implement the highest-value fixes through their established systems. Do not grow a new pile of disconnected props, duplicate features or empty menu entries. Keep unapproved appearance and physical variants reversible until explicit acceptance.

## Validation, delivery and persistence

Use focused tests early. Run tools/check_04g_compile.py for both genuine external and matching bundled NUnit APIs across all 12 configurations, tools/check_04g_assemblies.py for three boundaries, the 12-mode syntax checker, relevant FBX/motion/skin/asset receipt checks, original ownership/content/quest/save guards, and LFS plus metadata/GUID audits. Use genuine Unity API assemblies; syntax stubs are not equivalent. Serialize or cap compiler concurrency if resource contention causes exit -9; do not hide failed rows.

The historical H8R full external NUnit baseline is 1875 passed, 46 exact engine-dependent failures and 58 NeedsEngine exclusions, raw suite FAILED. Compare failure names AND messages. New derivatives, changed code and new tests need their own result. Do not present historical counts as a fresh run. Hydrate older LFS assets too: a previous temporary run gained 14 resource failures when old mesh resources remained pointers. Do not exclude those failures instead of fixing hydration.

Native validation must separately record import/Console, EditMode, PlayMode, real devices, actual saves, screenshots, profiling and Windows build/launch. Never invent screenshots, FPS, test passes or executables. Profile actual hardware, resolution, quality, CPU/render/GPU where available, draw calls, skins, materials, GC and memory. A 60 FPS target is not a measured result.

Make recoverable commits after source/intake, hero/motion, NPC, route and evidence stages. Verify each remote ref, re-fetch before pushing and never force. In the previous host CLI pushes lacked credentials, but the authenticated repository connection could create normal forward commits. Existing tools/publish_h7_lfs.py and .github/workflows/04oh-h7-art-lfs.yml already cover H8S sources, assets and evidence; their normal forward bot commits normalize uploaded binary objects into LFS without rewriting history. Prefer ordinary authenticated Git/LFS when available; preserve the workflow and verify LFS completion. Do not publish huge unreviewed archives. Never let a completed stage exist only in temporary files.

Update PR12 accurately, retain draft/target, leave PR11 unchanged. Maintain the complete required handoff under Docs/Ascension04OH: CONTINUATION_H8S.md, FEATURE_STATUS_H8S.md, ASSET_INTAKE_H8S.json, LICENSE_RECEIPT_H8S.md, ANIMATION_MAPPING_H8S.md, NPC_BINDINGS_H8S.md, WORLD_ROUTE_H8S.md, TEST_LEDGER_H8S.md, NATIVE_ACCEPTANCE_H8S.md, OWNER_GUIDE_H8S.md, DELIVERY_H8S.json and 05_OWNER_PLAYTEST.md, plus actual evidence. Update the top of START_HERE_04OH.md and RESUME_NEXT.md, preserving older history.

Before final delivery, self-audit every claim: What can the player actually see, speak to, traverse, fight, earn and reload? Which files changed and which original owner controls it? Did either identity get replaced incorrectly? Are camera/foot/weapon contacts proved? Are paths physical or overlays? Are rights and LFS complete? Did two owners emit motion, hits, costs or rewards? Did old banks and earned eligibility survive? Which checks are source-only, native, blocked or NOT RUN? What remains visually poor?

Report exact fetched HEAD, tested code SHA and source checksum, final remote SHA, changed asset paths, actual gameplay changes, rights, test counts/failures/exclusions, each native status, risks to old saves, remaining art deficiencies, no-code review steps, PR URL, unfinished work and the next highest-value step. Never call the whole sprint finished when only asset exports or source integration exist.

Begin by fetching current source and preserving local work, then implement. You are my builder, not just my planner.
