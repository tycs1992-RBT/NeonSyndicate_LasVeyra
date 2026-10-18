# 04G character and animation evidence

No actual Unity screenshots are available. The installed Editor could not initialize licensing, so reference-image parity and normal-camera appearance remain unvalidated. This document records actual mesh generation and source integration, not offline concept art presented as a game render.

| Actor sample | Detail | Starting 04G vertices | New vertices | New triangles |
|---|---:|---:|---:|---:|
| Malik hero | 2 | 10617 | 10873 | 16100 |
| Malik standard | 1 | 6184 | 6424 | 8856 |
| Blade Warden | 1 | 6179 | 5914 | 8116 |
| Heavy Sentinel | 2 | 12017 | 12197 | 16760 |
| Focus Weaver | 1 | 6676 | 6591 | 8660 |
| Rift duelist | 1 | 6530 | 6710 | 8786 |
| Warden crowd | 0 | 3224 | 3036 | 4064 |

These repeatable samples use the same appearance seed (100 for Wardens, 123 for the rival), body generator and quality detail at both revisions. The factory now selects an existing helmet for enforcement instead of a cap, which reduces those sample counts despite added tailoring. Lower vertex count is not a frame-time measurement. Existing budgets remain 4,200 crowd, 8,200 standard and 13,500 hero vertices.

Geometry is authored in BodyBuilder.Presentation04G.cs and emitted through the existing single skinned mesh. Closed plates have bevelled faces and back surfaces and bind to existing chest, thigh, shin and forearm bones. Sentinel mantle/greaves, Weaver split coat and duelist asymmetric mantle are different silhouettes. Coat collars, strap and buckles affect Malik and relevant clothed NPCs. Facial folds follow the exact existing head loft. No new renderer, light, paid asset or runtime download is introduced. The new tailoring itself is skipped at crowd detail; the enforcement headwear factory still uses its existing crowd model.

Animation is procedural, not new authored clips. HumanoidPose.Casting04G consumes actual player/NPC crystal windup state and the courier carry flag. Distinct maul and dagger attack keys, phase durations and mirror hand selection feed the existing IK and weighted finger system. Swing audio starts at active frames. Companion action callbacks occur at their configured strike fraction, after the windup. These source paths compile; the actual hand/focus/parcel placement and sound alignment require native observation.

Native comparison route: use seed 61724, the same graphics preset, time, weather, resolution and camera view on 04F and 04G. Capture Malik front/side, hands and face, a guarded blade, a dagger follow-up, a maul windup/recovery, Weaver casting, Sentinel silhouette, courier carry, drake turning/landing and a normal city doorway. Run the existing golden and performance tools and retain actual screenshots and profiler output. No before/after visual or FPS improvement is claimed until this is done.

Deferred art production: sculpted/imported hero and creature replacements, facial blendshapes, authored animation clip production and full cinematic reference fidelity. The existing art-direction review remains in Docs/CrystalCombat04F/VISUAL_COMPARISON.md.
