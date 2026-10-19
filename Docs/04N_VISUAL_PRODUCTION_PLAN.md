# 04N Visual Production Plan: Make the Game Look and Feel Different

Status: actionable art and presentation specification, not a claim of existing rendered results.

## 1. What is currently real

The project largely generates city, character meshes, rigs, animation posing, shaders and audio from original C# source. 04N source HEAD includes district geometry in CityKingdomGeometry04N and hero detailing in BodyBuilder.Kingdom04N. Existing Docs/VisualRebirth04A/ART_BIBLE.md provides geometry/material region canon. These are source additions; full fidelity and final Unity visuals are not proven by unit tests or offline OBJ exports.

Achieving the desired cinematic quality requires deliberate character mesh/topology, physically coherent authored animation, materials, lighting, silhouette, sound and interactive scene direction. Do not claim a full-quality sculpt exists because a vertex-count test passed.

## 2. Art direction in practical order

First 10-second impression:
1. Malik reads as a leading playable character, with a distinct silhouette, face, layered clothing and weapon.
2. Street reads as a place: buildings have mass, real doors, occupancy, roof hierarchy and convincing street scale.
3. Color differentiates districts but does not substitute for geometry.
4. The screen has a dramatic focal point and readable travel direction.
5. Civilians move in believable daily patterns and react to local threats.
6. A supernatural hint is visible without a mandatory battle in the protected opening district.

First real battle:
1. Enemy combat role is recognizable before attack.
2. Actual melee/gun contact matches recoil, poise and stagger.
3. Crystal focus effect direction is readable.
4. A bonded companion enters the action with anticipation and a believable landing/impact.
5. An earned Synergy Art looks qualitatively different from a normal explosion.
6. Control returns within a bounded, testable recovery window.

## 3. Immediate hero slice kit

Primary surface: Las Veyra Core -> Cinder Dispatch / Pressure House -> Sunken Row clinic.

Required art kit:
- 2 distinctive structural facade families with matching door/window scale.
- 1 focal arrival architectural landmark.
- 1 street-level market or service cluster.
- 1 functional clinic or dispatch interior with readable workstation and circulation.
- 1 major street canopy, drainage/industrial interface and logical elevated detail.
- 1 cohesive material palette per neighborhood with retained masks/normals and restrained emissive response.
- 1 clear non-hostile civilian behavior pass and 1 faction/hostile silhouette pass.

Secondary hero route: Underheart Gardens -> Workshop.
- Original door/stair and gameplay progression retained.
- Geological or constructed vertical language, legible route, distinct crystal ecology and interior lighting.
- One real traversal obstacle/puzzle and one actual interaction that feeds progression.
- No new walls or set dressing across existing boss/event hitboxes and safe passage.

## 4. Hero characters and rigs

Malik priority: face readability, neck-shoulder-torso proportion, elbows and wrists, convincing hands on gun/blade, cloth or armor layers, hip and knee contact, readable locomotion at camera distance, body interaction with Third Eye/casting.

One showcase bonded companion priority: recognizable species anatomy, grounded footplant/wing behavior, idle-to-action transition, attack anticipation, reachable point of contact, hit response, retreat and return.

One showcase caster priority: role-defining silhouette, actual ranged attack aim, obvious tell and consequence, damageability, interrupt and recovery.

Use existing procedurally authored rigs as the fallback or LOD tier. For richer authored models, use a documented DCC pipeline with original ownership or licensed provenance, existing humanoid/body motion retargeting, importer settings, scale, material configuration, sockets and collision. Never overwrite existing asset GUIDs or ship only an external high-resolution source file disconnected from runtime.

## 5. Shot and animation standards

Each completed companion Synergy Art needs a six-beat sequence:
1. Readable start: target and partner identified.
2. Player anticipation.
3. Partner movement and coordinated timing.
4. Mechanical impact fired once through gameplay authority.
5. Brief dramatic camera/effect/audio emphasis.
6. Exit and confirmed control recovery.

Three priority signatures:
- Cataclysm Wing, quartz_guide, distinct creature-body or crystal-body intervention.
- Aegis Breaker, ash_warden, defensive bracing and powerful guarded counter.
- Prismatic Snare, storm_skimmer, ranged/mobility and crystal snare/disruption.

These are distinct creative targets, subject to actual archetype anatomy. Do not force every body to share humanoid animation.

For each signature deliver existing combat hookup, actual pose or clip authored resource, cue markers, camera-safe anchor, collision fallback, visible VFX, audio cue, cancel route, screenshot and animation test. No mandatory repeated button mashing.

## 6. Source-to-game art acceptance

For every new asset record:
- In-game purpose, original owner and player route.
- Generator or source file, license/provenance if external.
- Mesh/rig source, triangle/vertex budget and LOD relation.
- Materials, texture color-space and shader handling for Built-in and optional URP.
- Human scale, rotation and attachment sockets.
- Correct new collision only for intended solid objects.
- GUID/.meta status and LFS presence where appropriate.
- Actual game object reference at runtime.
- Before/after capture path and named matching camera.
- Native render/profile, if actually executed.

Synthetic image previews, mesh export statistics, source tests and video concept footage are supplemental design artifacts, NOT runtime screenshots.

## 7. Composition and presentation rules

- Geometry first, materials second, lighting third, VFX fourth, cinematics last.
- Preserve a usable street width, legible entrance prompts and unobstructed service NPCs.
- Introduce silhouette layers: skyline, frontage, street-level functional detail and readable moving people.
- Materials look good in both daylight and nighttime; reasonable crystal emission, roughness and shading; no relying on bloom to sell the design.
- City combat uses less visual noise than isolated boss arenas.
- Player/target silhouette and telegraph must remain readable through effects.
- Use animated accent lights and weather effects with bounded CPU/GPU cost.
- If a full imported character pipeline is desired, explicitly amend the original procedural-only development rule in CLAUDE.md with a safe import protocol first. Do not silently violate it.

## 8. Actual visual evidence checklist

Twelve matched real frames:
1. Malik front 3/4 arrival, daylight.
2. Malik running with equipped weapon.
3. Original corridor and upgraded skyline, daylight.
4. Corridor at night with gameplay UI.
5. Sunken Row clinic exterior.
6. One original service interior with interaction.
7. Crystal caster winding up and firing.
8. Mixed creature encounter with readable multiple enemy roles.
9. Cataclysm Wing at impact.
10. Aegis Breaker and Prismatic Snare at readable keyframes.
11. Bonded companion locomotion and recovery.
12. Underheart route showing unique spatial/material language.

Capture each with Git SHA, scene/route, seed/time/weather, camera, resolution/preset, exact save state, image/video file, observed issues and whether the result passed native review. Source-only art work should say NOT RUN.

## 9. Measurable player test

One independent tester should be able to:
- Identify Malik and what he is holding at normal combat camera distance.
- Tell which of three partner Arts is activating before it lands.
- Reach Mara or Dispatch without invisible obstructions.
- Notice at least two regions are structurally different even with effects low.
- Recognize a ranged caster tell before impact.
- Recover after an interrupted finisher without losing camera or controls.
- Tell real earned weapon/Forge differences during the follow-up encounter.

Document failures as defects and polish targets. A claim of cinematic quality without this real test remains aspirational.

## 10. Performance, LOD and delivery

Set provisional 60 FPS as a target only, pending actual minimum/recommended hardware. Measure CPU/GPU frame separately in representative surface and Underheart play, creature battle, drake plus Synergy, menu transitions and high particle overlap. Track memory and streaming spikes; keep Low settings gameplay legible. A distinctive building facade may have cheaper far mesh, material and emission variants. Distinct characters may use simplified distant crowds while hero rigs retain body fidelity.

Art and gameplay must be committed and reproducible with source hashes and metadata, and a licensed native Unity host must ultimately validate it before the sprint is called visually complete.
