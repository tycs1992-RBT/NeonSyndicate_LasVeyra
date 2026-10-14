# Status and Testing

An honest account of what has and hasn't been checked. Newest first: Phase 5's loot world is right below, then Phase 4,
then Phase 3, and the older notes follow in the order they were written.

## Phase 5 (loot world): chests, hidden satchels, Chest Keys, crews, the Gunsmith arc

**Not run in the Unity editor yet.** Built: 140 chests in three kinds (common pods, sealed chests with level-scaled
guard crews and a daily bounty board, Veil Vaults only inside the Inner World), 40 hidden satchels, Chest Keys (crews,
champions, satchels, gun shops), the Collector phone contact, the Third Eye LOOT sense, the ARSENAL tally, the F2 "Loot
world" page and the 8-quest Gunsmith arc with the new GiveGun step. Checked: all twelve compile configurations clean,
the EditMode tests offline (15 new in `LootWorldTests`, the save freeze list updated), the offline quest validator
(27 quests, 0 errors, 0 warnings) and the offline logic tests. Not checked: anything visual (the chest glow, the hum,
placement against real colliders, the HUD panel's position), game feel and frame rate. The checklist is at the end of
`Docs/LOOTWORLD.md`.

## Phase 4: loot guns, travel powers, hover bikes, a faster opening and movement feel

**Nothing in Phase 4 has been run in the Unity editor yet.** Like Phase 3, it was compile-checked and tested offline.
Phase 3 was played once (creatures and controller shooting felt good); Phase 4 answers that playtest.

### What was built

- **Loot guns and champions:** random guns from a seed (8 types, 6 invented makers, 7 parts, six rarities, four
  elements, 15 traits, 17 named uniques), four equipped loot slots and a 30-gun backpack (Esc > ARSENAL), drops scaled
  by enemy tier and difficulty with a pity rule, champion enemies with affixes, weapon cores and the Forge ARMORY tab,
  a rotating loot stock in gun shops, saves that rebuild every gun exactly. See `Docs/LOOT.md`.
- **Travel powers:** jog 4.4 and sprint 8.2 m/s (sprint free outside fights), Surge to 30 m/s, Super Leap up to 28 m,
  Glide, and Flight (20 m/s cruise, 45 m/s fast) with their own camera, effects and sounds. See `Docs/TRAVEL.md`.
- **Hover bikes and the opening:** four hover bikes with visible riders, bikes in traffic, parked and at safehouses,
  Bring my bike on the phone, Start After the Prologue on the title screen, and a rebuilt prologue: a bike 4.7 m from
  where Malik wakes (it was a car about 220 m away on foot around a rock wall), no long waits, the escape ride about
  1.5 minutes after New Game, the whole prologue about 4 to 4.5 minutes. See `Docs/VEHICLES.md`.
- **Movement feel:** a new gait checked against published human walking and running data, weapons held with both hands
  by inverse kinematics (rifles shouldered, pistols in both hands), poses for every travel power and for riding. See
  `Docs/ANIMATION.md` and the before and after pictures in `Docs/Phase4_Previews`.
- **Left for the next pass (Fable):** character looks (faces, hair, finger bones so hands wrap the grip, skin and cloth
  materials) and city graphics (window interiors, wet streets, fog and light shafts, sky).

### How it was checked

| Check | Result |
|---|---|
| Compile, 12 configurations (Built-in and URP, 3 input modes, editor and player) | 0 errors, 0 warnings |
| EditMode tests, run offline | 221 passed, 0 failed, 5 skipped (they need Unity itself) |
| Logic checks against the compiled game code | 16,056 passed, 0 failed |
| Creature builder and pose solver checks | 205 passed, 0 failed |
| Quest and event data validator | 0 errors, 0 warnings |
| World logic and city layout dump | all passed; nothing that existed moved |
| Walk at 1.55 m/s against human reference (knee at heel strike, loading, swing peak, body bob) | 4, 18, 64 degrees and 4.0 cm (reference about 5, 15 to 20, 60 to 65 degrees and about 4 cm; before: 11, 33, 75 degrees) |
| Aiming (offline) | barrel within 2 degrees of the aim point, support hand within 2 cm of the foregrip |
| House rules scan (long dashes, other games' names) | clean |

Four workers built the parts in separate copies; the merge had no conflicts. No independent code review has been run
on Phase 4 yet (Phase 3's review found real bugs, so expect a few here too).

### Not checked yet

How everything looks and feels at full speed in Unity: gun models and beams, champion fights, the travel powers'
speed, camera and effects, flight over the city, the bikes' handling and lean, the rider and two-handed poses on the
real characters, the new opening's pacing, and frame rate with afterimages and flight.

### Test in Unity first (Phase 4)

Take screenshots with F12 and copy the first red Console error if anything breaks.

1. **New Game.** Skip the opening scene with Backspace or B. The bike should be a few steps away. Time how long until
   you ride. Send: a shot of Malik on the bike and the time it took.
2. **The walk.** Walk, jog and sprint on a flat street; turn and stop. Send: a shot mid-stride from the side, and say
   if anything still looks off (knees, arms, feet sliding).
3. **Guns in hand.** Aim a rifle and a pistol in all directions with the controller, fire and reload. Send: a shot of
   each aim.
4. **Loot.** F2 > Loot > Every unique: walk the line of beams, take a few (tap Y), equip one (hold Y), open Esc >
   ARSENAL. Then F2 > Loot > spawn a champion and kill it. Send: a shot of the beams and of the ARSENAL tab.
5. **Travel powers** (Free Roam, or F2 > Travel > unlock all): sprint then L3 for Surge, crouch then hold and release A
   for a leap, hold A to glide, double-tap A to fly (A up, L3 down, B fast). Send: a shot while flying over the city
   and say how the speed and camera feel.
6. **Bikes.** Ride at speed, lean through turns, crash into something, climb back on, then Phone > Bring my bike.
   Send: a shot of the rider leaning in a turn.
7. **Save and load** with loot guns equipped: the same guns (names and numbers) come back.
8. **Golden shots and the flythrough** (Neon Syndicate > QA) as in Phase 3, so the graphics pass has a baseline.

## Phase 3: creatures, quests, the Veil, the Frostline, progression, the URP beta and tools

**Nothing in Phase 3 has been run in the Unity editor yet.** It was compile-checked and tested offline only. Your
first session in Unity is the real test. The checklist is at the end of this section.

### What was built

- **Tools:** `CLAUDE.md` (the rules for AI sessions), one-command build and test scripts (`tools/build.ps1`,
  `test.ps1`, `build.sh`, `test.sh`, driving `BuildScripts` in the editor code), Unity Test Framework EditMode tests,
  golden screenshots with a compare report, and a frame-rate flythrough (Neon Syndicate > QA). See `Docs/TOOLING.md`.
- **URP beta:** an opt-in Universal Render Pipeline path (Neon Syndicate > Graphics > Switch to URP (beta), and Switch
  back to Built-in). Built-in stays the default. See `Docs/URP_MIGRATION.md`.
- **Creatures:** 19 species (15 regular ones plus 4 bosses with three phases each), built and animated in code:
  four-legged beasts, flyers, a burrowing serpent, a floating wisp, a mite swarm, and humanoid creatures on the people
  rig (Hollows, wraiths, golems, the Djinn). Packs and swarms, attacks that warn first, spawning by zone, time and
  weather, loot, the BESTIARY tab and creature health bars. See `Docs/CREATURES.md`.
- **World:** the Frostline snow zone and the Inner World (the Veil: hold the Third Eye about 0.7 s to shift; a tap is
  still bullet time), with zone banners on the HUD. See `Docs/ZONES.md`.
- **Quests and world events as data:** 19 guild quests in four arcs, 19 contract templates and 46 world events. All of
  it is JSON checked by a strict validator, with the JOURNAL tab to follow it. See `Docs/QUESTS_AND_EVENTS.md`.
- **Progression:** 30 crafting materials, 14 factions with reputation, the Forge (gear, infusions, brews, upgrades),
  three skill trees with 90 skills, the SKILLS and FACTIONS tabs and the F2 Progress page. See `Docs/PROGRESSION.md`.
- **Under the hood:** one shared way for systems to save, reset, and add pause tabs, F2 pages and HUD layers without
  editing the core files, and seven new save blocks (old saves still load).

### How it was checked

- **Compiles with no errors in 12 configurations:** Built-in and URP, each with the classic Input Manager, with both
  input systems and with the Input System only, each for the editor and for a Windows player (against Unity's
  reference assemblies, without a Unity editor).
- **The EditMode tests were run offline** against the compiled game code. The few tests that need Unity itself (they
  use JsonUtility) are skipped there and must run in Unity (step 13 below).
- **Offline harnesses:** a before and after dump of the city layout (nothing that existed moved), logic checks run
  against the built game code, a quest and event data checker (every file read twice in strict mode, then validated,
  plus checks against the whole city), an HLSL syntax check of the URP shaders and a numeric comparison of the URP
  colour grade with the Built-in one, and offline renders of every creature in `Docs/Phase3_Previews` (they show shape
  and pose, not the in-game lighting).
- **An independent code review** of the Phase 3 code found bugs, among them flyers that did not fly, problems with
  bosses in quests, a stun lock from Glass Mite swarms and a gang reputation spiral. Every finding (2 blockers, 14
  major issues and about 40 smaller ones) was fixed, each with offline tests where the logic allows, before the final
  checks below. What changed is described in `Docs/CREATURES.md` (see "What to check in Unity first", item 7),
  `Docs/QUESTS_AND_EVENTS.md`, `Docs/PROGRESSION.md` and `Docs/ZONES.md`.

**Final numbers (after the review fixes):**

| Check | Result |
|---|---|
| Compile, 12 configurations (Built-in and URP, 3 input modes, editor and player) | 0 errors, 0 warnings |
| EditMode tests, run offline | 159 passed, 0 failed, 5 skipped (they need Unity itself) |
| Logic checks against the compiled game code | 16,040 passed, 0 failed |
| Creature builder and pose solver checks | 205 passed, 0 failed |
| Quest and event data validator (19 quests, 161 steps, 46 events) | 0 errors, 0 warnings |
| World logic (snow, zones, Veil) and city layout dump | all passed; nothing that existed moved |
| House rules scan (long dashes, other games' names) | clean |

### Not checked yet

Everything that needs the Unity editor or a graphics card: how creatures look and move at full speed, whether they
stand on the ground, flight, boss fights, how quests flow, the Veil transition, the snow, where anvils and markers
stand, how readable the menus are, controller feel, balance, frame rate, the URP path and builds.

### Test in Unity first

Do these in order. Each step says what to look for and what to send back. Take screenshots with **F12** (it works in
menus too; the pictures go to the Photos folder, opened by Neon Syndicate > Open Save and Photo Folder). If a step
breaks, note it, copy the first red error from the Console (Window > General > Console) with a line about what you
were doing, and go on to the next step.

1. **Open the project.** Unity Hub > Projects > open `NeonSyndicate_LasVeyra` with Unity 6000.3.16f1.
   Send: nothing, unless Unity shows an error window (screenshot it).
2. **Let setup run.** The first import takes a few minutes and downloads two packages (Input System and Test
   Framework). If Unity asks to restart for input handling, say yes. When it settles, open the Console.
   Send: a screenshot of the Console if it shows any red errors.
3. **Press Play**, then pick **Free Roam** on the title screen (the Third Eye and the Veil are unlocked there). The
   loading screen lists any system that failed to start.
   Send: a screenshot if the loading screen lists problems, otherwise one shot of the first street you see.
4. **F2 > Creatures.** Switch **Spawns hostile** off first, so the creatures stay calm while you look. Spawn one of
   each family with the buttons: four-legged beasts (Neon Jackal, Frost Stalker, Crystal Basilisk), flyers (Violet
   Wyvern, Rime Griffin, Sun Griffin, Quartz Drake), the Glass Mite swarm, the Hollow Shade, and the humanoid creatures
   (Hollow, Mirror Wraith, Shard Golem, Sand Golem, Snow Hollow). Spawn the Dune Wyrm out in the desert (first
   F2 > Teleport > Starlite Motel). Look for: feet on the ground (no floating, no sinking), walking and turning without
   sliding, flyers taking off and circling. Only the flyers, the mite swarm and the Hollow Shade (a wisp) should be off
   the ground; the Dune Wyrm travels under the sand (a dust trail) and erupts. Then switch **Spawns hostile** on and
   fight a jackal pack and a golem: warnings before attacks, parries, hit sparks, loot toasts.
   Send: a shot of a few creatures side by side, a flyer in the air, and a shot of anything floating, sinking or
   sliding (say which creature).
5. **A boss fight.** F2 > Creatures > "Fight a boss in its arena": start with the Shard Golem Foreman, then the Glacier
   Titan if there is time. Look for: the boss bar, ground rings that fill before they burst, a roar and summons at two
   thirds and one third of its health, the death burst and the drop.
   Send: a shot with the boss bar and a ring on the ground, and one of the drop. Say whether the fight felt fair.
6. **A guild quest from the journal.** Esc > JOURNAL > AVAILABLE, choose "Medicine Run" (the Menders) and press Set
   waypoint. Drive to Mara Solane's clinic, walk into the Q marker, press E (Y on a controller) and ACCEPT. Play it
   through: the trailer park, the jackals, the crate, and the drive back within five minutes. Look for: the tracker
   line under the objective, QUEST COMPLETE and the reward. If the marker is missing, use F2 > Quests > Start instead.
   If there is time, also try a boss quest: F2 > Quests > Start on "The Titan Wakes".
   Send: a shot of the HUD with the tracker line, and the QUEST COMPLETE screen.
7. **The Veil.** Hold T or Caps Lock (LB + RB on a controller): a ring of dots fills, then the world shifts (red-violet
   sky, crystal growths, people and traffic gone, violet street lights, a VEIL timer by the energy bar). A quick tap
   should still give bullet time. Hold again to come back. Once, stay inside until the energy runs out (you are thrown
   back with a stagger). Try it with the keyboard and with a controller.
   Send: a shot inside the Veil on a street, with the HUD showing.
8. **The Frostline.** Drive north-west out of the city (or F2 > Teleport > Frost Wardens Outpost). Look for: the THE
   FROSTLINE banner, a soft snow edge (not a straight line), falling snow, slippery ice on Rimeglass Lake, the outpost
   with its flickering braziers, and footsteps crunching in the snow.
   Send: a shot of the snow edge from the car, one of the outpost and one of the lake.
9. **The Forge.** First F2 > Progress > "+20 of every material". Then go to Dante Rook's anvil in Old Silver (the
   orange A on the map). Look for: the anvil standing free (not in a wall, not on the road). Use it with E (Y): the
   game pauses and LB and RB switch tabs. Forge a Rare Shell and craft a Mender's Draught (a 10-minute brew; its timer
   shows top left). Esc or B closes the Forge without opening the pause menu.
   Send: a shot of the Forge screen and one of the anvil in the street.
10. **SKILLS and FACTIONS.** F2 > Progress > "+5 skill points", then Esc > SKILLS. Learn Heavy Hands with the mouse,
    then another skill with a controller (D-pad to it, A, then A again). Open FACTIONS and point at a faction to read
    its details.
    Send: a shot of each tab at your normal Game view size, and say if any text is too small or overlaps.
11. **Save and load.** Esc > SAVE / LOAD, save to slot 1, go to the title screen and load slot 1. Check that the
    learned skills, reputation, materials, the finished quest, bestiary entries and the brew (with the time it had
    left) came back, and that you load in the normal city (the Veil is never saved).
    Send: a note of anything that did not come back.
12. **Golden shots and the flythrough.** Set the Game view to 1920x1080. Run Neon Syndicate > QA > Capture Golden
    Shots (about two minutes, hands off), then Neon Syndicate > QA > Run Perf Flythrough (about 80 seconds). Neon
    Syndicate > QA > Open QA Output Folder opens the golden shots; the Perf folder sits one level up.
    Send: the new GoldenShots folder (zipped) and the `.txt` summary from the Perf folder. They become the first
    baseline for later comparisons.
13. **The automatic tests (any time).** Window > General > Test Runner > EditMode > Run All.
    Send: a screenshot of the result (all green, or the names of the red ones).

The URP beta has its own checklist in `Docs/URP_MIGRATION.md`. Try it only after everything above works on Built-in.

## Original build: checked

- **Compiles cleanly** against the Unity 6000.3 reference assemblies, with zero errors and zero warnings, for both the editor and a Windows player build. The editor setup scripts compile too.
- **653 automated logic checks pass**, run outside Unity against the compiled game code:
  - weapon inventory: giving, ammo, reloading, cycling, save and restore
  - wanted level rules: crimes, star thresholds, evasion timing
  - the city: generation is deterministic, every landmark, contact and shop exists, 300 random GPS routes resolve, every street race route stays on roads
  - every synthesized sound effect and every radio station song: no silence, no clipping, no invalid samples
  - data tables: vehicles, gangs, properties, powers, achievements, cheat codes
- **Five independent code reviews** (boot and menus, missions, player and combat, vehicles and AI, world and audio) found about 100 issues, from crashes and soft-locks to balance problems. All of them were fixed. Four more review rounds then checked the fixes themselves and found about 30 follow-up problems, mostly in AI traffic (queues at red lights, passing wrecks, head-on standoffs). Those were fixed too.

## Original build: not yet checked

- **The original build has since been played in Unity by the owner; the Phase 2 work below has not.** The build environment has no Unity editor or GPU. Expect the first session to need tuning:
  - car handling, AI driving and pedestrian behavior
  - difficulty, prices and rewards
  - lighting, fog and colors
  - frame rate on your computer (Settings has Low, Medium, High and Ultra)
- Some bugs only appear in play. If something breaks, open the Console (Window > General > Console) and copy the first red error.

## Requirements

- Unity 6000.3 (built against 6000.3.16f1). Other Unity 6 versions will probably work, with an upgrade prompt.
- Built-in Render Pipeline (the default). Two packages, which Unity downloads on first open: Input System 1.17.0 (for
  controllers) and Test Framework 1.6.0 (for the automatic tests). The optional URP beta installs the Universal RP
  package when you switch to it.
- A mid-range PC from the last six or seven years should manage High settings. Use Medium or Low on laptops.


## October update: camera and sunny look (Unity) plus the 3D browser version

Unity project changes (compiled cleanly for editor and Windows player; all 653 logic checks still pass):

- **Camera framing** matched to the browser build: on foot 3.0 m behind at head height with a 0.42 m right-shoulder offset and a 6 degree downward tilt; aiming pulls in to 2.0 m over the shoulder; the chase camera sits lower and closer (car in the lower middle of the frame); first-person driving now looks out from the driver's eyes; default field of view 55 (old saved default of 62 is migrated).
- **Sunny look:** new games in Free Roam and the title flyover start in the afternoon (the story prologue still begins at night, as written), an in-game hour now lasts two real minutes, stronger warm sun, bluer sky tint, brighter sky and ground ambient, thinner daytime haze.
- **Palms:** smoother curved trunks, ten arched fronds that droop toward the tips, and a skirt of dead fronds.
- **Power lines:** wooden poles every 36 m with crossarms and three sagging wires along side streets outside downtown, the Strip and Glass Heights.
- **Colour grade:** `ColorGrade.cs` with `Resources/NeonGrade.shader` (soft highlight roll-off, saturation, warm highlights, cool shadows, vignette). If the shader is missing or unsupported it switches itself off, so the game always renders. Toggle: `Settings.Bloom`.

Browser version (`Browser3D/`): tested in headless Chrome with software rendering, including scripted play-throughs of all three missions, police chases, death and arrest, and a randomized stress session with no script errors. Not yet played by a person on real hardware.

## October update 2: framing and streets matched to the reference shots

Both builds now frame the action the way the three reference screenshots do.

- **On foot:** the default view centers the character about 4 m ahead with the camera about 2 m up and tilted down 7.5 degrees, so the street fills the lower frame. **V** cycles standard, far (5.8 m), first person and a close over-the-shoulder view (2.5 m, character on the left). Aiming still pulls in to 2.0 m over the shoulder. Leaving a car restores the on-foot tilt.
- **Chase camera:** about 6 m behind a coupe and 2 m up, tilted down 9 degrees, so the horizon sits near the top third and the road fills the lower half. The camera now follows the car exactly and smooths only its offset, so at speed it no longer falls back and shrinks the car (Unity got the same fix). The chase pivot in Unity uses the interpolated car transform instead of the physics center of mass, which removes jitter.
- **Streets (browser):** weed tufts along chain-link and wall bases, the crack where lots meet the sidewalk, curb gutters, building foundations, pole bases, dirt lots, lawns and the desert shoulder, denser in the rougher districts (about 32,000 instanced tufts, roughly 25 extra draw calls; they shrink away past 150 m). Sidewalks and lots have hairline cracks cut into the normal map and grime patches. Distant hills fade into haze, the grade has a little more warmth and contrast, light paint colours read white instead of metallic grey, about one pedestrian in ten is a jogger, the chapel block has a corner liquor store beside a fenced weedy lot, and the car wash has palms.
- **Streets (Unity):** cutout weed tufts (Standard shader, cutout mode, double-sided cards, alpha kept solid in the smaller mip levels) along lot edges, building bases and dirt lots, weighted by district; hairline cracks and grime in the sidewalk pavers.
- Checked: Unity compiles cleanly (editor and player); 653 logic checks pass. Browser: smoke, play, combat, police, all three missions, race, chaos, views, UI and performance runs in headless Chrome with no script errors. The race mission test was fixed to spawn its car near the start line (it used to spawn one far away, where the traffic cleanup removed it).
- Still true: the art is procedural and stylized, not photographic, and nothing has been played on real hardware yet.

## October update 3: new direction, phase 1 (browser build)

Agreed direction: a futuristic city of enhanced humans with an Inner World layer, mythical creatures instead of a crime-sim feel, zones that get harder (the city, the Inner World, the Frostline, later the Badlands), progression in the spirit of an open-world RPG (skill trees, companions, guilds, loot), designed for co-op (PvE first, PvP later). The Draybie creatures are being removed from this game.

Phase 1, built in the browser version only:

- Hover cars (no wheels drawn; glowing thruster pads, underglow, gentle bob; wrecks drop to the ground).
- Future-city dressing: neon trim on roofs, storefront bands and tower corners; faintly glowing palms and trees; cool street light; holographic billboards with new ads; enhanced citizens with glowing implants (and one on the player).
- The Inner World on **T**: flash transition, violet and crimson sky, crimson fog, a rift in the sky, about 8,800 instanced crystal growths, drifting spores, an uneasy drone, empty streets, and Hollows that hunt the player. Gunfire there raises no heat.
- The Frostline north of the city: snow ground fading into the desert, packed-snow road with plowed mounds, the FROSTLINE gate, outpost buildings with snow roofs, a radio mast, about 560 snow-laden pines, 150 boulders, 64 ice formations, a frozen lake, falling snow, wind, snow caps on the whole mountain ring, and Frost Stalkers.
- HUD: Third Eye indicator, crystal-shard heat icons, zone names; title tagline.
- Fix: sidewalks now fill around the player at once (spawning used to sample random links across the whole city, so only two or three people appeared at a time).
- Checked in headless Chrome: smoke, combat, all three missions, the randomized chaos run, a Third Eye stress run (rapid toggling, fighting Hollows, both zones at once) with no script errors; creatures take damage and die. Unity is unchanged in this phase.


## Phase 2: people, movement, combat, controller, gear, graphics (Unity)

What changed:

- **Characters:** `BodyBuilder` lofts every body part from smooth superellipse cross-sections with anatomy bumps (collarbones, pecs, shoulder blades, calves, knees), sculpted heads with eyes, brows, noses, lips and ears, seven hair styles, beards in a natural shade of the hair colour, jackets, techwear seams, bodysuits, tactical belts, knee guards, armour plates, visors and implants. One mesh per person, four-bone weights, palette, metal and smoothness, emission and a fabric detail normal map, drawn by `Resources/NeonCharacter.shader`. The shoulders were reshaped so arms grow out of a sloped trapezius instead of sitting on top like pads. `BodyFactory` prebuilds pedestrians, officers and SWAT on worker threads; a standard body costs about 8 ms of one worker thread and almost nothing on the main thread.
- **Animation:** `HumanoidPose` is a pure-math pose solver driven by `HumanoidAnimator`. Feet are placed with two-bone IK along a gait cycle (heel strike, roll over the ball, toe-off, a swing that leaves and meets the ground at ground speed), the pelvis rides only as high as the planted legs allow, strides are capped by leg length, and strafes open the hips for crossover steps while the shoulders stay on the target. Standing feet stay planted in the world and step when they drift or the body turns. Attacks are key poses (wind-up, strike, follow-through) for all 14 move types, mirrored for the left hand, with lunge steps; plus rolls, backsteps, staggers, guards (boxer, blade, two-handed, energy palm), parries, drinking and jumps. Guns still aim in world space with recoil. Off-screen characters refresh at an eighth of the rate.
- **Combat, controller, gear, shrines:** as listed in the README. Difficulty Balanced is the default: enemies deal 62 percent damage, at most two swing at once, wind-ups run about 0.5 to 1 s for regular enemies, and health regenerates to 60 percent out of combat.
- **Graphics:** `NeonPost` replaces the old colour grade with a full stack (HDR bloom mip chain, half-resolution ambient occlusion with a depth-aware blur, camera motion blur that keeps the player sharp, chromatic aberration, ACES tonemap, lift, gain and split toning by time of day, vignette, grain, FXAA and sharpening) and a time-sliced live reflection probe on High and Ultra.
- **Reskin:** hover cars, Echo Relics, heat crystals.

How it was checked:

- Compiles cleanly in six configurations (editor and player; classic input, Both, Input System only), editor scripts included.
- 2,133 logic checks pass (up from 653). New ones: 36 generated bodies across all three detail levels, the palette and bodysuit colours, the pose solver in 30 scenarios on four body sizes (finite and normalised rotations, feet never below ground, no foot skating, hips height sane, feet arrive with the body after a teleport), every melee move's timing and reach, enemy wind-ups long enough to read, and level-up costs.
- Offline renders of the real mesh and pose code in a three.js viewer: walk, jog and sprint strips, strafe and backpedal, every attack with a stand-in blade, roll, stagger and drinking.
- The post shader passes an HLSL syntax check (glslang); its grade was previewed on reference screenshots.
- An independent code review of the new runtime code found eight issues, all fixed: bloom and occlusion read upside down when MSAA hands the effect an unflipped source, a mesh collider on the underglow quad clashing with the car's rigidbody, characters popping into view with a stale pose, headlight glows not riding the hover bob, motion blur streaking across camera cuts, the reflection probe moving mid-capture, relic gear lost to a full inventory, and the cutout shader variant missing from builds.

Not checked yet: everything that needs a GPU or the Unity editor (how the post effects and shader look on your monitor, frame rate, animation feel at full speed with real input, controller feel).

Browser build: the Phase 2 balance (62 percent damage to the player, regeneration to 60 percent after 5 s, camera pulled back 1.2 m while a fight is on) passed the smoke, combat and Phase 1 regression runs in headless Chrome with no script errors.

### Night picture fix (after the first Unity session)

The first Phase 2 screenshot at night (the opening mission at the Crystal Manor ruins) showed a flat navy screen with only glowing things visible. Cause: the grade applied contrast around 0.5 in linear light, which clips everything darker than roughly 18% grey to pure black, and the night shadow tint then painted that black navy. The measured navy in the screenshot, (14, 22, 59), matches what the old grade outputs for black.

- Contrast now pivots on middle grey with a power curve, so shadows deepen but never clip.
- Night shadow tint and lift are much subtler.
- New eye adaptation (auto exposure): the scene's average brightness is measured on the GPU every frame and the exposure eases toward it, up to about +1.5 stops at night and +0.45 by day. It can be switched off in the settings.
- New Brightness slider (minus 1 to plus 1 stop) in Esc > Settings > Graphics.
- Night ambient light (the neon glow off the clouds) and moonlight are brighter.
- The mission marker column is fainter, so the player and the NPC inside it stay visible.
- Luma carries a crystal lantern in the opening mission, because the ruins sit outside the street lamps.

Checked: all six compile configurations clean, every post shader pass compiles as HLSL, 2,183 logic checks pass, and the old and new grade were compared in an offline simulation. Not yet seen in Unity.

### Phase 2 motion pass (swings, footwork, dodges)

Frame-by-frame checks at 60 fps turned up problems the earlier strips at 20 fps hid. All fixed in `HumanoidPose` (plus the dodge curve in `PlayerCombat`):

- **Swings went through the body.** Blade directions were interpolated along the straight chord, so the overhead chop, the two-handed slam and the bat swing rotated the blade backwards through the torso, and the forehand and backhand slashes swept behind the head instead of across the front. Big swings now carry a mid-swing pass-through: the hand travels an arc and the blade sweeps over the top or across the front, its cutting edge carried along the same arc. Overhead raises go up beside the head instead of through the face.
- **The hand could flip mid-swing.** The wrist limit counted the roll about the forearm with the bend, so a rolling grip hit the limit and the hand (and blade) snapped round for a frame. Bend (85 degrees) and roll (110 degrees) are now limited separately.
- **The spin cut never spun.** Smoothing took the short way round a full turn, so the body twisted 90 degrees and back. The full turn is now applied after smoothing, the arms are filtered in the pelvis frame (so they ride with any fast turn, rolls included), and the spin is a short hop instead of feet grinding round on the floor.
- **Feet slid during starts, stops, turns, lock-on footwork and dodges.** Measured with the player controller's real acceleration and the game's dodge curve:

| Move | Fastest planted-foot slide before | After |
|---|---|---|
| Walk, then stop | 5.9 m/s | 0.2 m/s |
| Jog, then stop | 5.9 m/s | 0.2 m/s |
| Lock-on strafe right, stop, strafe left | 6.4 m/s | 0.3 m/s |
| Lock-on reversal (forward to backpedal) | 3.9 m/s | 0.3 m/s |
| Running 90 degree corner | 1.8 m/s | 0.3 m/s |
| Running 180 degree turnaround | 5.2 m/s | 0.3 m/s (one foot pivots on the spot for a few frames) |
| Dodge roll | 11.0 m/s | 0.9 m/s (first frame only) |
| Backstep | 21.7 m/s | 0.8 m/s (first frame only) |
| Spin cut | 15.7 m/s | 0.2 m/s |

  How: a foot that touches the ground (stance, plus the toe-off and landing moments of the swing) is held in the world, heading included, and the offset from the gait fades out while the foot is in the air, so a lagging speed estimate, speeding up, braking or turning never drags it. Planted feet are always flat on the ground (they used to hover if the body stopped mid-stride), handing the feet between planting and walking no longer makes them jump, braking keeps a quicker cadence for a moment, a planted foot the body has already moved past ends its contact sooner (a quick step instead of a deep lunge), and the fighting stance only takes the feet once they are fully planted, so they step into it.
- **Dodges.** The dodge distance curve now starts and ends at rest (it used to launch at 15 m/s on the first frame, faster than any foot could follow) and keeps the momentum you had along the dodge direction for the first quarter, so rolling out of a run flows. Feet push off where they stand, trail behind the body, and plant again as soon as they land.

Checked: the six-configuration compile; 2,183 logic checks (50 new: blade clearance from the body and a sweep through the front for every big swing, a full-circle spin cut, planted feet through walk and jog starts and stops, lock-on footwork, a lock-on reversal, rolls, backsteps and the spin cut on the game's own curves, and the dodge curve itself); steady walking, jogging, sprinting, strafing and backpedalling are unchanged (heel and ball slides about 0.2 m/s at most); the pose solver costs about 10 microseconds per character per frame. Offline renders of the swings, the spin cut, the roll and footprint plots are in `Docs/Phase2_Previews`.

Still to tune with real input: in a hard 180 degree turnaround at full speed one foot swivels on the spot for a few frames, and the first frame of a dodge can shift a foot by about 1.5 cm.

