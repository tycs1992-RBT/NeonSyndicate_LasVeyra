# 04H visual implementation evidence

No Unity screenshot or gameplay render was captured. The exact Editor could not initialize licensing. The numbers below come from executing the actual C# mesh builders against genuine Unity managed modules on .NET. They establish geometry integration and bounds, not visual quality, frame time, draw calls or reference-image parity.

## Character geometry and render path

`Characters/BodyBuilder.Build` calls `BuildAll`, including `BuildRenaissance04H`. BodyFactory and HumanoidRig consume the resulting data through the existing single skinned body renderer and character material. Rig indices, 47-bone detailed/17-bone crowd consumers, weighted fingers, saved Appearance and attachment consumers are retained.

New geometry includes tapered swept undercut locks, cheek/jaw/nasal contours in the actual head surface, fitted hip pouches, coat pockets and weighted chest seams. Sentinels gain a raised neck/dorsal silhouette; Weavers gain vertical collar vanes. Existing dense outfits omit optional 04H accessories when they cannot fit their original detail budget. This is original code-authored geometry, not an imported sculpted character, textured hair asset, blendshape system or replacement rig.

| Deterministic actor | Detail | 04G vertices | 04H vertices | 04H triangles |
|---|---:|---:|---:|---:|
| Malik hero | 2 | 10873 | 11616 | 17016 |
| Malik standard | 1 | 6424 | 6729 | 9204 |
| Blade Warden | 1 | 5914 | 5914 | 8116 |
| Heavy Sentinel | 2 | 12197 | 12317 | 16808 |
| Focus Weaver | 1 | 6591 | 6711 | 8708 |
| Rift duelist | 1 | 6710 | 6710 | 8786 |
| Warden crowd | 0 | 3036 | 3036 | 4064 |

Seeds are the same as 04G: Warden 100, rival 123. The actual dense-outfit regression failed at 8,282 vertices before the accessory admission fix. The final body tests retain the original 4,200/8,200/13,500 limits. Surface contour changes need not increase vertex counts. No extra character lights, scene primitives or renderers are spawned for this work.

## World geometry and render path

`VisualRebirth04AArchitecture.Tier` adds the new tier-zero frontage through `RenaissanceFrontage04H`/`Tracery04H`. Both ordinary CityBuilder.Fantasy04A chunk geometry and DistrictBuilding.AddTier04A near/far material batches already consume Tier. Recessed mineral rings, radial metal spokes and stepped reveals are therefore present in the normal city construction path, not a separate demonstration scene.

The same actual mesh method is executed by `tools/measure_04h_geometry.py`. For footprint (-10,-8) to (10,8), seed 100, style 2, tier 0 to 12 metres:

| Preset | Near added vertices | Far added vertices |
|---|---:|---:|
| Low | 760 | 224 |
| Medium | 760 | 224 |
| High | 1520 | 224 |
| Ultra | 1904 | 224 |

Low/Medium use one detailed face; High/Ultra use two, with Ultra a denser ring. Existing district selection budgets also remain 35/80/145/210 landmarks. Ordinary city chunks use the coarser path. New test samples stay inside the old parcel plus the established 0.12-metre relief envelope, above ground routes. Sample near geometry spans y=3.83 to 7.82 metres. No road, door, collision, quest anchor, renderer per ornament or point light is added. The rods use four planar sections rather than repeated spheres at each ring joint.

Existing imported architecture, textures, surfaces and optional URP material handling remain authoritative. No new district layout, interior shell, binary model pack or broad texture replacement is claimed.

## Animation and presentation integration

HumanoidPose.Movement04H applies bounded braking and turn anticipation using real smoothed acceleration/yaw, suppressing it for attacks, dodges, staggers, riding, travel and cover. Existing action keys and IK still own weapon grips and attack timing. NPC drakes play actual bite/breath actions with callback strike fractions, injury interruptions and retreat movement. These are procedural poses, not new animation clips.

Actual combat/contact states drive recovery prompts, crystal casts, impact flashes and camera behavior. Reduced effects uses the existing saved choice. The camera pulls in immediately for walls and eases out after clearance; no cinematic shake or frame-time benefit is asserted without native observation.

## Reproducible native capture recipe

Use separate 04G and 04H checkouts, the same seed 61724, graphics preset, 1920x1080 resolution, clear weather and 15:30 clock. Do not reuse a save after testing rewards without retaining its original copy. Record exact commit, pipeline, camera view, clock and preset with every frame. Existing QA tools can hide HUD and run golden/performance captures.

| Capture | Anchor and camera settings | What to compare |
|---|---|---|
| Arrival | New Free Roam at unchanged Veyra Lofts arrival, Mid view, FootPitch 7.5, player yaw unchanged, no combat/lock | Malik back/hip silhouette, surrounding themed frontage |
| Hero close | Malik stationary; camera position = player feet + player forward*1.4 + up*1.6; look = feet + up*1.55; FOV 50 | Hair locks, facial surface contours, collar and chest seams |
| Coat/profile | Camera = feet + player right*2 + up*1.2; look = feet + up*1.1; FOV 50 | Pouches, pockets, weighted clothing; inspect during walk/stop/turn |
| Facade | Choose a tier-zero themed building near arrival; record seed/footprint. At chosen decorated face center, camera = face center + outward*12 + up*5.8; look = face center + up*5.8; FOV 50 | Rose tracery, depth, spokes and stepped reveals; same building both commits |
| Faction roles | Same deterministic encounter/role looks at 8 metres, Mid view, daylight | Sentinel neck silhouette, Weaver collar and cover/casting posture |
| Drake | Same handler-owned event body/seed and clock; normal Mid gameplay camera | Bite/lance windup, flank, retreat, collision and injured pose |
| Interior work | Pressure House fuse cabinet and Dispatch receiver B; normal Mid view, no special lighting | Physical diagnostic crystal, prompt/readability, companion approach |

The transform recipes above can be entered through the existing PlayerCamera.SetCinematic API/QA tooling when producing controlled comparisons. The normal route in START_HERE_04H uses ordinary controls and physical locations. Repeat daylight, 22:00, warm interiors and all four presets. Capture a native 30/60/high-frame-rate combat sequence, not only stills. These comparisons remain **not executed** here.

Remaining art production includes imported/sculpted hero and creature replacements, textured hair, dedicated facial animation, new authored clips, broader faction silhouettes and handcrafted environment composition. No offline concept art is presented as finished game art.
