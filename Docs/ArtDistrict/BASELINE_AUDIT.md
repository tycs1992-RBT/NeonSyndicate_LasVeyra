# Baseline rendering and preservation audit

Source: the supplied Unity project ZIP. This audit describes inspected code and files, not a Unity play session.

## Rendering foundation

The project targets Unity 6000.3.16f1. Its default path is the Built-in Render Pipeline. An optional URP beta switch exists in the editor, but the supplied package manifest does not include URP. The visual demonstration retains the existing rendering choice.

The supplied Assets folder contains 317 C# files, four shaders, ten JSON files and three assembly definitions. It has no imported meshes, textures, materials, animation clips, prefabs, scenes or meta files. The game constructs its city, characters, creatures, vehicles, materials, audio and UI at runtime. Reference PNGs elsewhere in the ZIP are documentation images, not imported game textures.

`NeonSyndicateSetup` generates the main scene and shader retention materials on editor import. `Mats` provides pipeline-aware materials. Existing post processing provides bloom, ambient occlusion, color grading, FXAA and optional motion blur. Atmosphere owns time, weather, sky, sun, ambient light and distance fog.

## Scope and preservation requirements

| Existing system | Visual-pass constraint |
|---|---|
| CityLayout and road graph | Preserve generation, footprints, roads and site coordinates |
| CityBuilder | Replace selected visuals while retaining original collision volumes |
| Atmosphere | Blend district presentation with time, weather and realm state |
| PlayerCamera | Retain near, mid, far, first-person, aiming, vehicle and travel modes |
| IMGUI HUD and menus | Preserve screen layout, input ownership and existing controls |
| Story, quests, events, saves | Preserve data, IDs, save schemas and mission behavior |
| Creature gameplay | Keep upgraded appearance independent of combat balance and spawn logic |
| Runtime resources | Reset caches on reentry and release runtime-owned visual assets |

The original Crystal Manor story site remains at x 470, y 0, z 1470, with a 45 metre site radius. Its ruin lot spans x 445 to 495 and z 1445 to 1495. Presentation additions must not silently replace mission geometry or move that story target.

`GameRoot` disables template scene cameras, audio listeners and directional lights because it constructs its own runtime presentation. Therefore an editor-only lighting arrangement would not itself upgrade the playable game. The demonstrated visuals need to be integrated into runtime creation.

World colliders also affect camera collision, bullet traces, line of sight, vehicle wheels and cover. Decorative relief and crystal ornaments should not add unnoticed collision barriers. A replacement shell must retain any original building colliders skipped by the visual branch.

## Existing desktop optimization

City geometry is merged by material into 160 metre spatial chunks. Street lights use a capped nearby pool without shadows. Breakable props stream near the player. Creature rigs share character materials and reduce work for distant/offscreen actors. Low, Medium, High and Ultra presets already control shadow distance, cascades, view distance and lighting budgets.

New art should use shared materials, modest texture sizes, bounded particles, a small number of extra lights, distance culling and conservative mesh budgets. These are design provisions; smooth frame rates require measurement on a real desktop GPU.

## Validation limits

This work environment has no Unity editor, Unity Hub, C# compiler, Blender or shader compiler available. It cannot prove editor import, script/shader compilation, live animation, control behavior, UI appearance or frame rate. Historical project documents mention earlier offline harnesses; those reports are not validation of this delivery.

The delivered offline validator checks mesh data, textures, metadata and source preservation. Run the supplied Unity QA checklist before accepting the visual pass for production. In particular, test fresh import, all camera modes, clear and adverse weather, realm transitions, reentry without domain reload and a Windows player build.

The original assembly definitions name URP assemblies even when the URP package is absent. Whether this exact editor accepts the optional-package arrangement has not been tested here. Treat a clean Built-in import as an acceptance gate; no compilation failure is asserted solely from this source inspection.
