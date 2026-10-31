# L3: The Living Ground

## Focused L3 preservation repair

The final repair status and fresh test evidence are in [LiveGroundL3/Repair/FINAL_STATUS.md](LiveGroundL3/Repair/FINAL_STATUS.md). That report supersedes the original delivery checkpoint below. Native Unity gates remain NOT RUN.

## What visibly changes

The western Redspire Badlands gains two original 1K PBR surfaces: compact cracked oxidized earth and coarse stony sand. The existing floor between rocks, raised swells and ground patches use the maps. A world-coordinate blend varies the two families and feathers the existing noisy Redspire border, inner belt and snow edge. L2's original models remain unchanged. Ground geometry, movement, collision, road surfaces, structures, ponds, discoveries and rewards keep their existing owners.

This is a reversible source and art candidate. The separate local setting starts OFF. It has not been seen or accepted in native Unity by this worker. L1 and L2 native gates remain open. V0 has the owner's earlier reported play pass.

## What to check in Unity

Before the timer: GitHub Desktop, select feature/04oh-ascension-art-foundation, Fetch origin then Pull origin, including the LFS publication descendant. Open the existing project with Unity 6000.3.16f1. Wait for import/preparation. Keep the existing renderer, revision 10 and normal Free Roam. Do not delete or migrate saves. If preparation needs a retry, stop Play and choose Neon Syndicate > Live World > Prepare Redspire ground. The Console prefix is LIVE_GROUND.

The five-minute observation timer can begin on arrival; travel duration depends on your current location.

1. First check L1: Pause > EXPLORE > Route: Mirrorwell pavilion. Follow normal GPS, walk up the steps without jumping, bump a pillar, walk out a rear/side gap and orbit the normal camera. If it fails, stop broader acceptance and send the first red Console error and F2 > Live world placement record. L1 is not accepted by L3 source tests.
2. Pause > EXPLORE. Set Redspire kit ON and Redspire ground ON, then choose Route: Redspire art walk. Follow ordinary GPS west toward the actual mixed cluster near Hollowwind Maze or Redwatch Tower. No teleport or new reward is involved. The route still needs at least one prepared L2 family and a safe ground/capsule result.
3. Look at Malik's feet, then the ground between a nearby boulder and a spire. Expect reddish sediment plates, fine fissures, coarse fragments and a rough surface, with variation over four-metre repeats. Keep the HUD on and capture one close shot and one wide view. The surface is normal-mapped, not displaced geometry.
4. Keep camera, quality, time and weather comparable. Use Compare legacy look for 20 seconds, then close Pause. It temporarily restores selected people, world art, L2 kit and L3 ground, then returns to your selected options without writing preferences. The original collision stays. F2 > Wilds and the EXPLORE ground line distinguish loaded maps from fallback.
5. Walk and dodge near a boulder, make a sword move, pass in a vehicle, land from legitimately available flight and approach an existing creature. Check camera and ground contact. Save normally, return to title and reload. Confirm the same equipment, companion eligibility, quests and discoveries, no duplicate payment and one world representation.

Afterward, try all four L2/L3 ON/OFF combinations, cross the southern/northern Redspire boundary, revisit Mirrorwell, and compare daylight/evening and Medium/Low. Turning ground OFF is the lower-cost original-material fallback. Run the existing Golden Shots and Perf flythrough at matched seed, weather, time, resolution and preset on the same PC. Send the first full Console error, LIVE_GROUND/LIVE_WILDS lines, F2 > Wilds and the HUD-on matched views. If it looks unchanged, that is a failed player-visible gate requiring a focused repair, not permission to begin L4.

## Implementation and recoverability

- Authoring: tools/author_redspire_ground_l3.py generates actual tileable color, tangent normal and RGB-zero/alpha-smoothness maps. Python 3 with numpy, scipy and Pillow reproduces them. The fixed seeds are 61724 and 61725. This is original procedural geology, not scanned or photographic art.
- Editable Blender source: ArtSource/RedspireL3/RedspireGroundL3.blend. Portable relative references point to the supplied maps and unchanged L2 maps. Height images are source evidence only. Run Blender in background with tools/render_redspire_ground_l3.py for neutral source tiles and matched comparisons.
- Assets: Assets/NeonSyndicate/Art/LiveGround/Redspire, with manifest, CC0-1.0 credits, stable GUIDs and import settings. Base color is sRGB; normals and metallic/smoothness are linear; normal green is +Y; Repeat and mipmaps are enabled; textures stay 1024 square.
- Preparation: LiveGroundSetupL3 runs in the original LiveWorld pre-Play/build lifecycle. It validates hydrated asset hashes, maps and shader errors and creates an ignored Resources/LiveGroundPrepared catalog. Missing source invalidates stale prepared references. URP shader source stays outside Assets until the optional package is present; it is generated only into the ignored preparation folder.
- Runtime: CityBuilder's actual base floor and bare parts of the snow transition receive one shared proxy of their existing material. WildsBuilder wraps only Dirt/Sand/GlassSand/DryTurf slots. Rocks, snow, water and structures are not assigned the ground shader. Original legacy normal/mask/macro response is carried into the masked material. Native shader parity remains to be checked.
- Boundary: a single 512-square linear mask samples the existing noisy compass rule and FrostDepth/SnowCover. It is generated once per session, not per frame or toggle. It changes neither BiomeAt nor a plan. The visual feather is 24 m, with no altitude or camera dependency. Slope biases the two ground families without affecting collision.
- Setting: NSLV_redspire_ground_l3, local PlayerPrefs, default OFF. There is no new save block. L2's preference remains independent. Toggling swaps shared material state; it does not rebuild chunks or recook collision. L1 lease rebuilds automatically reuse the shared materials. Comparison/save/load/title/QA cleanup remains in the original lifetime; title/loading suspend ground presentation.
- Cost: six resident uncompressed 1K maps plus mipmaps, one 512 mask, at most four shared ground proxies and more shader samples. No new terrain mesh, renderer, collider, per-piece object or Update component is introduced. Native memory, frame cost and draw measurements are NOT RUN. No FPS promise or performance acceptance is made.

## Evidence and gate status

Read LiveGroundL3/VERIFICATION.json and TEST_LEDGER.md for exact checked source and raw outcomes. Earlier L2 reports remain historical and are not rewritten. Native Unity import, shader compilation, EditMode/PlayMode, actual saves/devices, collision/camera, screenshots, Profiler and Windows build/launch are NOT RUN. The two new NeedsEngine tests compile but require the native Test Runner: prepared maps/fallback/shared-material round trip, and all four appearance combinations with the actual original collider and scoped L1 lease.

DCC images under LiveGroundL3 are labeled Blender source renders. Before images use an explicitly approximate prior ground palette; they are not reproductions of the Unity procedural shader. After images use the delivered PBR maps and unchanged L2 source meshes. These are material-authoring evidence, not gameplay screenshots or proof of the runtime mask.

SOURCE IMPLEMENTED and ART ASSET PRODUCED are separate from NATIVE IMPORTED, NATIVE PLAYTESTED, OWNER SEEN and PERFORMANCE ACCEPTED. Production activation remains NOT AUTHORIZED; OFF is the shipped default. No L4 or unrelated gameplay expansion is included.

## Current limits, honestly

There is no usable Unity Editor host here. Shader code and native preparation still need the first real Unity import. If the shader or map fails, original materials remain, and the preparation refusal is retained. Optional URP, lighting/reflection response, normal orientation on slopes, actual border appearance, contact and frame pacing cannot be accepted from managed API metadata or Blender. The map mask has a 6.25 m texel footprint before filtering, so the 24 m blend must be judged in motion. Original procedural collision and L2 visual-envelope fitting still need native contact judgment. Additional near-camera geometry was intentionally omitted: the surface itself is this sprint's deliverable.
