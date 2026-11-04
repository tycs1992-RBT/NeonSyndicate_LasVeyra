# Shardwake Vale

**Choose Shardwake Vale > Play Shardwake Vale** (or open `Assets/Scenes/ShardwakeVale.unity` and press Play). Step by step for a fresh checkout: [START_SHARDWAKE_VALE.md](../../START_SHARDWAKE_VALE.md). Malik starts on the ochre shelf. Dante is under the swept roof to the left, Three Arch Bridge is ahead, and the forked violet Split Crown stands beyond it.

## Open on Windows

1. Fetch `pilot/shardwake-vale` and open that branch in its own checkout. Use Git LFS for the existing character resources. The original district branch and project remain separately launchable.
2. Open the checkout's project folder in Unity Hub with Editor **6000.3.16f1**. In the Project window double-click `Assets/Scenes/ShardwakeVale.unity`, or use **Shardwake Vale > Open Shardwake Vale**. Press Play and focus the Game tab.
3. For the original game, stop Play and open `Assets/Scenes/NeonSyndicate.unity`. Its build settings and scene are unchanged. **Shardwake Vale > Build Shardwake Vale for Windows (pilot only)**, or `tools\build_vale.ps1` with Unity closed, builds a player whose only scene, and therefore its startup scene, is the Vale (`ValeLaunch` refuses any list that contains the city scene). It requires Windows Build Support. `tools\build.ps1` still builds the original game.

## One review pass

From spawn, see whether you can identify the workshop, crossing and destination before following the objective marker. Speak to Dante at the open side of the workshop. Follow the copper caps over the bridge and the earth path up the far bank. At the Crown, watch the amber warning become a blue wave. Step back or jump, move around the formation for a clear shot, and cast Sunbolt at each exposed brass collar. Take the released seed, retrace the bridge, speak to Dante and fit the seed at the furnace. Look for the warm hearth, turning bellows wheel and clasp on Malik's wrist. At Dante, Replay starts a fresh commission. The intended duration is approximately five minutes and has not been timed in native Play.

| Action | Keyboard and mouse | Gamepad |
| --- | --- | --- |
| Move, look | WASD or arrows, mouse | Left stick, right stick |
| Sprint, jump | Hold Left Shift, Space | Hold B, A |
| Talk, take, install | E | X |
| Focus next collar, fire | Tab, left click or B | Right stick press, right trigger |
| Pause, replay at Dante after completion | Esc, R | Start, Y |

## Editing the opening

The Hierarchy contains **01 - Terrain and river**, **02 - Dante workshop**, **03 - Three arch pack bridge**, **04 - Split Crown landmark**, **Malik - start and facing** and **Opening camera**. Move the ordinary scene transforms, then save the scene. The conversation anchor, furnace clamp and wheel move with the workshop. The bridge owns its crossing coordinates and far checkpoint. The Crown owns the aiming faces and seed. If you move a structure, refit its connection to the ground. For opening framing, adjust the camera and spawn facing together.

Meshes, palette shaders, materials and commission text are under `Assets/ShardwakeVale`. The banks, deck, shop, rocks and plinths use the same mesh for drawing and collision. Pines, planting and painted trail have no walking colliders. Editable modeling source is in `tools/shardwake`. The scene launches from its committed meshes. The offline author requires an explicit `--replace-scene` to overwrite an already edited scene.

## What belongs to the pilot

- Malik uses `Appearance.Malik()`, Dante uses `QuestWorld.Look("Dante", ...)`, and both use the existing `HumanoidRig`, `BodyBuilder` and `HumanoidAnimator`. The established input backends and strict `QuestJson` reader are reused. Sunbolt takes its color, energy cost and cooldown from `Abilities.Spells[Abilities.SunboltIndex]`.
- The scene owns its movement, camera, encounter, dialogue and furnace completion. `Assets/ShardwakeVale/HearthCommission.json` holds the two conversations and stage objectives. Existing city quest places, combat registries, companions, traffic, crafting inventory and city saves remain with the original game.
- The HUD explicitly identifies the Vale pilot loadout. The isolated starting profile has awakened Third Eye and knows Sunbolt. Progress is recorded only under `Application.persistentDataPath/ShardwakeVale/pilot-v1.json`. On Windows this normally resolves to `%USERPROFILE%/AppData/LocalLow/Neon Syndicate/Neon Syndicate Las Veyra/ShardwakeVale/pilot-v1.json`. Stages and grounded collars persist. On re-entry Malik starts at the appropriate bank. An unreadable bank is preserved while the session runs in memory.
- The two global runtime startup hooks are on `GameRoot`. Registry reset still runs. The scene ownership guard returns before city bootstrap. The automatic Editor Play/idle cast and legacy world-art preparation hooks also respect this scene. The pilot needs neither the city nor a Fantasy World preview session.

## Evidence and resume

### Art sprint: Dante's doorway to the first arch

Matched offline scene previews, BEFORE (fd41d8a) and AFTER, both rendered by the same `tools/shardwake/preview.py` with the same scene lighting so only the game's assets differ: [spawn](SplitCrown/art2-spawn-compare.png), [Dante's doorway](SplitCrown/art2-doorway-compare.png), [bridge view](SplitCrown/art2-bridge-compare.png), and close views of a [broadleaf tree](SplitCrown/art2-tree-close-compare.png) and the [ravine bank](SplitCrown/art2-ravine-bank-compare.png). Cameras are in `SplitCrown/cameras.json` and `SplitCrown/review-shots.json`. The previews copy the shaders on the CPU; they do not show Unity's shadow cascades, anti-aliasing, wind, animation or HUD.

- **Foliage** (`tools/shardwake/vale_foliage.py`, `Materials/ValeFoliage.shader`, `Materials/FoliageAtlas.png`): a painted atlas of branching leaf sprays, needle fronds, grass, flowers and fern, drawn as alpha-cutout cards lit from both sides, with light through the leaves toward the sun, sun shadows from the cutout and a gentle wind. Three broadleaf trees (flared trunk, buttress roots running into the ground, forking limbs, overlapping sprays with gaps) and two conifers (drooping whorls with missing branches) replace the faceted canopies and every stacked-cone pine. Irregular grass groups with occasional flowers and ferns replace the flower rows; the walking line stays clear.
- **Ravine** (`vale_ravine.py`): stepped rock shelves with distinct strata bands, broken lips, soil pockets with plants, moss under deep lips, a dark wet band at the waterline, boulders breaking the shoreline, and a river that is deep teal mid-channel, paler in the shallows, rippled and streaked with foam behind the piers. All of it draws over the unchanged terrain collider; the three arches, the deck and the crossing are untouched.
- **Doorway** (`vale_workshop.py`): drum-built stone columns with recessed joints, plinths and chamfered capitals; a timber frame that shows how the roof is carried (plate beams on timber bolsters, knee braces, rafters and purlins under both slopes); fitted flagstones on the apron; banked embers in the kiln mouth whose warm light falls on the flagstones in front of it; small stones in loose groups beside the first steps. Overlays only, so the hall's colliders, Dante and the furnace clamp are unchanged.
- **Crown** (`vale_art.crown3`, `crown_base`): two broad faceted halves with heavy shoulders, broken slanted tops and a clear V split at the same heights as before, a wider seam with a soft violet halo that still reads from the doorway, and a low mineral apron of shards and rock that joins it to the hillside and stays low near the three collars. The far ridges lost their violet so the strongest violet is the destination.
- Bark and worn timber get their own grain channel (alpha of `SurfaceGrain.png`).
- Existing project art was checked first: the WR02 canopies and the WR02-B elder and cedar use geometric leaf cushions on a flat green tile, so they would not fix the prototype read. Everything in this pass is generated by the scripts above (original, CC0 like the rest of the pilot); nothing was bought or generated by a paid service.

### Visual sprint (opening view, bridge, Crown, Sunbolt)

Matched views, all offline scene previews (software renders of the saved scene, never a Unity Game view):

- [Spawn before](SplitCrown/spawn-before.png) and [spawn after](SplitCrown/spawn-after.png), same camera (`SplitCrown/cameras.json` "spawn"), same size, side by side in [spawn-compare.png](SplitCrown/spawn-compare.png).
- [Bridge view before](SplitCrown/bridge-view-before.png) and [after](SplitCrown/bridge-view-after.png) from the review camera in `SplitCrown/review-shots.json`: the crossing, the ravine and the Crown.
- [Sunbolt cast to hit](SplitCrown/sunbolt-cast.png): gather, flight, burst with the collar flare, settled.
- [From Dante's doorway](SplitCrown/doorway-after.png) toward the Crown (`review-shots.json` "dante_doorway"), for the review question.

The before images come from `tools/shardwake/review.py` at 5481038, which copies the old shader (fixed painted light, no shadows). The after images come from `tools/shardwake/preview.py`, which copies the new `ValePalette.shader` per pixel: the scene's sun with a soft shadow map, the trilight ambient as spherical harmonics, the surface grain chosen by each vertex's surface code, the warm-to-cool haze and the self-lit sky and seam. Neither reproduces Unity's shadow cascades, anti-aliasing, animation or HUD, so judge the real look in the Editor.

What changed in the scene (all editable meshes under `Assets/ShardwakeVale/Meshes`, made by `tools/shardwake/vale_art.py` through `author_scene.py --replace-scene`):

- Four irregular broadleaf canopy trees (spreading, tall, leaning, twin trunk) replace the repeated pines in the four groups nearest the spawn; pines stay farther out for depth.
- The path edge: half-buried rock, ground cover tufts, moss mats over the paving and two old stumps whose roots cross the edge (`PathEdge`, no collider).
- Three Arch Bridge: coursed spandrels with worn lower stone, protruding voussoirs with keystones, a recessed barrel vault in shadow, river piers with cutwaters. The deck, curbs and parapets keep their exact walking geometry.
- The Split Crown: two leaning crystal slabs with broad faces around a narrow cleft, darker inner faces and a thin luminous seam, taller (about 22 m and 17 m) so the tips meet sky from the spawn. The stone seat, collar and copper struts are unchanged; collisions near the collars are unchanged.
- Three distinct collars: brass fins with an ember ring, patinated copper with teal plates, twin tilted lilac and bronze rings.
- Four ridge bands (warm sage near to pale violet with snow far) with a notch on the spawn's line of sight, and a painted sky dome.
- Lighting and material: the sun now casts soft shadows from the west at a lower angle, the ambient is cooler overhead and warmer below, and `ValePalette.shader` adds wrapped sun light, a world-space grain tile (`Materials/SurfaceGrain.png`) and distance haze. The camera far clip is 650 m for the sky.

Sunbolt (`SunboltFx` pure rules with `SunboltFxTests`, presentation in `ShardwakeVale.Sunbolt.cs`): after the journey accepts a cast (energy and cooldown charged there as before), light gathers in Malik's right hand for 0.2 s while the arm points, then the bolt leaves at the original 23 m/s with a tapered, twisting gold trail. On arrival `ShardwakeJourney.Ground` decides: a grounded collar gets a burst and flares warm, then settles to its spent look; anything else gets a small dim fizzle and no collar light. A refused cast (shielded, blocked, too far, recovering) shows only the existing tip and spends nothing. The blue fault wave, its timing and its colours are unchanged.

### Earlier evidence

[Opening camera](opening.png) and [bridge view](crossing.png) are software renders of the checked-in Unity meshes. The opening includes an offline idle pose skinned from the same Malik and Dante character definitions. The illustrations approximate painted directional light and haze. They omit the live HUD, native shading and animation.

[Verification](verification.json) records the managed and physical source checks. `tools/shardwake/check_scene.py` probes all three legs of the route against serialized renderer/collider meshes. The project has a Unity EditMode fixture in `ShardwakeValeTests.cs` for imported components, assets and a real deck ray hit. Run it in **Window > General > Test Runner > EditMode** when an Editor is available.

Keep the next change in this scene until the opening view and full return have been played on Windows. To leave the pilot, stop Play and open the legacy scene. For a technical rollback, branch from the original parent `9affc809a7a250aae51a123441e9ccde515fc0bd` in a separate worktree. Use the review branch's forward history to recover individual pilot changes. The legacy branch, PRs and save banks do not require a rollback.

### Reproduce the offline checks

With Python 3, NumPy, Pillow and PyYAML installed, run `python tools/shardwake/check_scene.py` from the repository root. It reads the saved scene and mesh buffers directly. The GitHub **Shardwake Vale scene and route** job runs this check on pilot pushes.

For the managed checks, supply the genuine Editor Data folder and its template ScriptAssemblies folder to `tools/check_unity_classic_wr02g.py`. Give that output to `tools/shardwake/compile_matrix.py` to compare player/editor, classic/both/new input and Built-in/URP reference configurations. `tools/shardwake/run_checks.py --tests` runs the isolated commission and save fixtures and exports actual posed actor geometry. Supply its output to `tools/shardwake/review.py <output-folder> --actors <actor-folder>` to reproduce the two software views.
