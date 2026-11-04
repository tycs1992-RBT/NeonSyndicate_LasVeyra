# Shardwake Vale

**Open `Assets/Scenes/ShardwakeVale.unity` and press Play.** Malik starts on the ochre shelf. Dante is under the swept roof to the left, Three Arch Bridge is ahead, and the forked violet Split Crown stands beyond it.

## Open on Windows

1. Fetch `pilot/shardwake-vale` and open that branch in its own checkout. Use Git LFS for the existing character resources. The original district branch and project remain separately launchable.
2. Open the checkout's project folder in Unity Hub with Editor **6000.3.16f1**. In the Project window double-click `Assets/Scenes/ShardwakeVale.unity`, or use **Shardwake Vale > Open pilot scene**. Press Play and focus the Game tab.
3. For the original game, stop Play and open `Assets/Scenes/NeonSyndicate.unity`. Its build settings and scene are unchanged. An optional **Shardwake Vale > Build Windows pilot** command passes only the Vale scene to the build pipeline and requires Windows Build Support.

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

[Opening camera](opening.png) and [bridge view](crossing.png) are software renders of the checked-in Unity meshes. The opening includes an offline idle pose skinned from the same Malik and Dante character definitions. The illustrations approximate painted directional light and haze. They omit the live HUD, native shading and animation.

[Verification](verification.json) records the managed and physical source checks. `tools/shardwake/check_scene.py` probes all three legs of the route against serialized renderer/collider meshes. The project has a Unity EditMode fixture in `ShardwakeValeTests.cs` for imported components, assets and a real deck ray hit. Run it in **Window > General > Test Runner > EditMode** when an Editor is available.

Keep the next change in this scene until the opening view and full return have been played on Windows. To leave the pilot, stop Play and open the legacy scene. For a technical rollback, branch from the original parent `9affc809a7a250aae51a123441e9ccde515fc0bd` in a separate worktree. Use the review branch's forward history to recover individual pilot changes. The legacy branch, PRs and save banks do not require a rollback.

### Reproduce the offline checks

With Python 3, NumPy, Pillow and PyYAML installed, run `python tools/shardwake/check_scene.py` from the repository root. It reads the saved scene and mesh buffers directly. The GitHub **Shardwake Vale scene and route** job runs this check on pilot pushes.

For the managed checks, supply the genuine Editor Data folder and its template ScriptAssemblies folder to `tools/check_unity_classic_wr02g.py`. Give that output to `tools/shardwake/compile_matrix.py` to compare player/editor, classic/both/new input and Built-in/URP reference configurations. `tools/shardwake/run_checks.py --tests` runs the isolated commission and save fixtures and exports actual posed actor geometry. Supply its output to `tools/shardwake/review.py <output-folder> --actors <actor-folder>` to reproduce the two software views.
