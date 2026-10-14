# Neon Syndicate: Las Veyra (Master Build)

An open-world action game set in Las Veyra, a neon city of enhanced humans and hover traffic. Take on 12 story missions, fight gangs, hunters and the police with blades, fists, guns and crystal powers, collect mythic gear and level up at Resonance Shrines. This is one Unity 6 project that combines all six earlier Neon Syndicate versions. Everything (the city, cars, people, weapons, sounds, music and menus) is generated when you press Play, so there are no art or sound files to download.

## New in Phase 4

**Not run inside Unity yet.** It compiles and passes the offline tests; your playtest is the real test. The checklist is in [Docs/STATUS_AND_TESTING.md](Docs/STATUS_AND_TESTING.md).

- **Loot guns:** random guns in six rarities (Common to Mythic) from six invented makers, built from parts, with elements (Volt, Ember, Frost, Void) and special traits such as ricochet, explosive rounds, chain lightning, homing micro-rockets and charge shots. Legendary and Mythic guns are named uniques. Light beams in the rarity colour mark drops. **Champion** enemies (named, with powers like Shielded or Vampiric) and creature bosses drop the best guns and **weapon cores**. **Try it:** tap E (Y) on a dropped gun to take it, hold to equip it; Esc > **ARSENAL** for your four loot slots and backpack; the Forge's **ARMORY** tab uses cores to add an element or raise a gun's rarity. **F2 > Loot** spawns guns and champions for testing. See [Docs/LOOT.md](Docs/LOOT.md).
- **Faster on foot and travel powers:** sprinting is faster and free outside fights. **Surge** (super speed): press Left Ctrl (L3) while sprinting. **Super Leap:** crouch, then hold and release Space (A). **Glide:** hold Space (A) in the air. **Flight:** double-tap Space (A) in the air. Leap and Glide unlock after the first story mission, Flight after the second; Free Roam has everything. See [Docs/TRAVEL.md](Docs/TRAVEL.md).
- **Hover bikes and a faster opening:** four hover bikes with the rider in view, leaning into turns. The story now starts with a bike a few steps away, the waits are gone, and Enter (A) shows the next line while Backspace (B) skips a whole scene. The title screen has **Start After the Prologue**, and Phone > **Bring my bike** delivers your bike nearby. See [Docs/VEHICLES.md](Docs/VEHICLES.md).
- **Movement feel:** an upright, natural walk and run built from real human movement data, rifles shouldered with both hands on the gun, pistols held in both hands, and new poses for every travel power and for riding. Before and after pictures: [Docs/Phase4_Previews](Docs/Phase4_Previews). See [Docs/ANIMATION.md](Docs/ANIMATION.md).
- **Coming next (Fable pass):** character looks (faces, hair, real fingers on the grip, skin and cloth materials) and city graphics (lit rooms behind windows, wet streets, fog and light shafts, a better sky).

## New in Phase 3

**Not run inside Unity yet.** Everything below compiles and passes offline tests, but nobody has played it. The checklist for your first session is in [Docs/STATUS_AND_TESTING.md](Docs/STATUS_AND_TESTING.md). To try it, start a **Free Roam** game from the title screen: the Third Eye and the Veil are unlocked there, and three of the four guilds already have work for you.

- **Creatures:** 19 kinds of mythical creature, built and animated in code like the people. Glass Mite swarms, Neon Jackal packs, golems, Hollows, Mirror Wraiths, the Crystal Basilisk, wyverns, griffins, the burrowing Dune Wyrm and the Quartz Drake, plus four bosses with three phases each (Glacier Titan, Shard Golem Foreman, Mirage Djinn, Mirror Wraith Queen). They show up by place, time of day and weather (in the city only at night). They warn before every attack and drop crafting materials. **Try it:** press **F2**, open the **Creatures** tab, and spawn any of them next to you or jump straight into a boss fight.
- **The Veil (Inner World):** hold the Third Eye (**T** or **Caps Lock**, **LB + RB** on a controller) for about 0.7 seconds to shift into the red-violet mirror city. Hold again to come back. A quick tap is still bullet time. Shifting costs crystal energy, and the Veil drains it while you are inside.
- **The Frostline:** a snow zone north-west of the city, with Rimeglass Lake, the Frost Wardens Outpost and Titan Hollow. **Try it:** drive north-west out of the city. A banner tells you when you cross in.
- **Guild quests, contracts and world events:** 19 quests in four guild story arcs, 19 kinds of repeatable job on the guild contract boards, and 46 random world events (in the city, the Badlands, the Frostline and the Inner World). They are written as data files and checked by a validator. **Try it:** Esc > **JOURNAL** shows what you can do; **F2 > Quests** and **F2 > Events** start any of them directly.
- **Progression:** 30 crafting materials from creatures. Reputation with 14 factions (the eight gangs, four guilds, a company and a cult) changes prices, how gangs treat you and what the guilds teach you. **The Forge** turns materials into gear, infusions, brews and upgrades: Dante Rook's anvil in Old Silver (an orange **A** on the map), one at the Frost Wardens Outpost, and a small one beside every shrine. Three skill trees (Body, Mind, Shard) hold 90 skills. **Try it:** Esc > **SKILLS** and **FACTIONS**. **F2 > Progress** hands out skill points, materials and reputation for testing.
- **New pause menu tabs:** SKILLS, JOURNAL, FACTIONS and BESTIARY. They work with the mouse and with a controller (LB and RB switch tabs).
- **Graphics:** an optional URP beta (**Neon Syndicate > Graphics > Switch to URP (beta)**). The Built-in pipeline stays the default.
- **Tools:** one command builds the game or runs the automatic tests (`tools/build.ps1` and `tools/test.ps1` on Windows, `tools/build.sh` and `tools/test.sh` on the Mac). Golden screenshots, a before and after compare report and a frame-rate flythrough are under **Neon Syndicate > QA**. See [Docs/TOOLING.md](Docs/TOOLING.md).

Where this is heading: [Docs/ROADMAP.md](Docs/ROADMAP.md) compares the game with the long-term blueprint, item by item.

## New in Phase 2

- **People:** smooth, sculpted bodies instead of boxes, with real faces (eyes, brows, noses, lips), hair styles, techwear with glowing seams, implants, armour and visors. Every body is one mesh with four-bone skinning, built on worker threads so spawning never stutters.
- **Movement:** a new procedural animation system with feet planted by IK. Walks, jogs and sprints have heel strike, toe-off, pelvis sway and arm swing; strafes use crossover steps; feet stay put and step when you turn on the spot. Planted feet hold their spot through starts, stops, sharp turns, lock-on footwork and dodges instead of sliding. Landing dips, rolls, backsteps, staggers, guards, parries and drinking all have full-body motion.
- **Combat:** fights are deliberate and winnable. Overhead chops come over the top, slashes and swings cut across the front, and the spin cut really spins. The camera pulls back in a fight. Enemies telegraph every swing with a glow and wind-up, only one or two attack at once, and you have stamina, a dodge roll with invincibility frames, guard and parry, ripostes, light combos, charged heavies and healing Shard Tonics. Three difficulties (Story, Balanced, Veteran) with Balanced as the default.
- **Controller:** full gamepad play through Unity's Input System (Xbox, PlayStation, Switch Pro and others), with rumble, the PlayStation light bar, aim assist, menu navigation and button prompts that follow your device. Keyboard and mouse still work at the same time.
- **Mythic gear and progression:** six gear slots, six rarities up to Mythic, affixes, elements and three sets; Lumen from fights, levels and attributes (Vigor, Endurance, Might, Focus), Resonance Shrines to rest, level up, upgrade and fast travel; elite enforcers and mission bosses drop the best gear. Three mythic weapons: Volt Katana, Crystal Maul, Arc Gauntlets.
- **Graphics:** HDR bloom, filmic tonemapping with a day and night grade, ambient occlusion, camera motion blur, chromatic aberration, film grain, FXAA and sharpening, a live city reflection probe, and a physically based character shader with rim light and hit flash. All of it follows the graphics preset and the settings menu.
- **Las Veyra reskin in Unity:** hover cars with thruster pads and underglow, ten Echo Relics (each holds Epic or better gear) instead of the old creature collectibles, and the wanted level shown as heat crystals.

## Play the 3D version in your browser (no Unity needed)

Open **Browser3D/NeonSyndicate3D.html** in Chrome, click **Play**, and you are on a Las Veyra street: hover cars, neon and enhanced people, a third-person camera (standard, close shoulder and far), first person on **V**, the Inner World on **T** (crystal growths, a red-violet sky and creatures called Hollows), the snowy Frostline north of the city with Frost Stalkers, plus traffic, police and three missions. It has the Phase 2 balance too: hits land softer, health creeps back between fights, and the camera pulls back while you fight. Details: [Browser3D/README.md](Browser3D/README.md).

## Play it in 5 steps

1. **Install Unity.** Get Unity Hub from unity.com/download. In Hub, go to Installs and add **Unity 6.3** (any 6000.3 version; this project was built for 6000.3.16f1).
2. **Unzip** this master zip to a short path, for example `C:\Games\NeonSyndicate`.
3. **Add the project.** In Unity Hub: Projects > Add > Add project from disk, then pick the `NeonSyndicate_LasVeyra` folder (the one with `Assets`, `Packages` and `ProjectSettings` inside). If Hub says the editor version is missing, let it install it or pick your 6.3 version.
4. **Open it.** The first import takes a few minutes and downloads the Input System package (for controllers) and the Test Framework (for the automatic tests). If Unity asks to enable the new input backends or says it needs to restart to fix input handling, click **Yes** or **Restart Unity now**. The game scene is created and opened for you.
5. **Press Play** (the triangle at the top), or use the menu **Neon Syndicate > Play**. Click inside the game window to capture the mouse, then pick **New Game**.

Tip: in the Game tab, set the play mode dropdown to **Play Maximized** for a bigger picture.

## Make a standalone game

Menu **Neon Syndicate > Build Game for Windows**. When it finishes, the `Builds/Windows` folder opens. Double-click `NeonSyndicate.exe` to play. To share it, send the whole `Windows` folder, not just the .exe.

## Quick controls

| Action | Keyboard and mouse | Controller |
|---|---|---|
| Move, look | W A S D, mouse | Left stick, right stick |
| Sprint / dodge roll | Hold / tap Left Shift | Hold / tap B (Circle) |
| Light / heavy attack | Left mouse / hold left mouse | RB / RT (R1 / R2) |
| Guard and parry, aim a gun | Right mouse | LB (L1), LT (L2) |
| Lock on | Middle mouse or Z | R3 |
| Heal (Shard Tonic) | Q | X (Square) |
| Jump | Space | A (Cross) |
| Use, get in a vehicle | E, F | Y (Triangle) |
| Third Eye / cast a power | Caps Lock or T / B | LB + RB / LB + RT |
| Shift through the Veil (Inner World) | Hold T or Caps Lock | Hold LB + RB |
| Surge (super speed) | Left Ctrl while sprinting | L3 while sprinting |
| Super Leap / Glide / Fly | Crouch, then hold and release Space / hold Space in the air / double-tap Space in the air | Same with A (Cross) |
| Take / equip a dropped gun | Tap E / hold E | Tap Y / hold Y (Triangle) |
| Gear, map, phone, pause | I, M, P, Esc | Pause menu, View (Touchpad), hold View, Menu (Options) |
| Drive | W A S D, Space handbrake | RT gas, LT brake, left stick |

Full list, including driving: [Docs/CONTROLS.md](Docs/CONTROLS.md). Press **F1** in the game for a quick reference.
Cheat codes and the **F2** debug menu: [Docs/CHEATS.md](Docs/CHEATS.md).

## What's in the game

- A large city with nine districts, a desert, a canal, an airstrip, day and night, and six kinds of weather
- 12 story missions: a father, a daughter whose signal is opening the Veil, and the Shard Warden who wants her, ending in Mercy or Power ([Docs/STORY.md](Docs/STORY.md)), plus Free Roam (skip the story) from the title screen
- Street races, rampages, payphone contracts, taxi, vigilante and paramedic jobs, store robberies, properties, casino slots
- 17 weapons plus fists, 30 vehicles, carjacking, a five-star wanted level, police helicopter, eight gangs
- Collectibles (data chips, crystal shards, spell stones, Echo Relics), stunt jumps, achievements, three save slots plus autosave, six radio stations
- Mythic gear, levels and attributes, Resonance Shrines, three difficulties, full controller support
- Phase 3: 19 creature species and four bosses, the Frostline and the Inner World, guild quests, contracts and world events, 14 factions, the Forge and three skill trees
- Phase 4: random loot guns with rarities, traits and weapon cores, champion enemies, super speed, super leap, glide and flight, four hover bikes, a faster story opening, and a natural walk with two-handed weapon holds

Details and where each piece came from: [Docs/FEATURES_AND_SOURCES.md](Docs/FEATURES_AND_SOURCES.md).

## Honest status

This part describes Phase 2 as it was delivered. For Phase 3, see **New in Phase 3** at the top of this page.

You have played the earlier build in Unity. **The Phase 2 changes have not been run inside Unity yet**, because the computer that built them has no Unity editor or GPU. What was checked:

- The code compiles cleanly in six configurations: editor and Windows player, with the classic Input Manager, with Both, and with the Input System only.
- 2,183 automated logic checks pass, including 36 generated bodies (no broken geometry, valid skin weights, feet on the ground) and the pose solver across walking, running, sprinting, strafing, backpedalling, crouching, turning, every attack, rolls, staggers, guards and jumps, with no foot skating for adult and child-sized bodies. Newer checks cover blades staying clear of the body in every big swing, the spin cut turning a full circle, and planted feet through starts, stops, lock-on footwork and dodges with the game's real acceleration.
- Characters and animations were rendered offline with the real mesh and pose code and checked frame by frame at 60 fps. A few of those renders are in `Docs/Phase2_Previews`.
- The post-processing shader passes an HLSL syntax check; an independent review of the new code found eight runtime issues (upside-down bloom with MSAA, a collider error on hover cars, characters popping in with a stale pose, and smaller ones), all fixed.

Expect tuning in the first session: combat timing and damage, animation details, the strength of the post effects, and frame rate. See [Docs/STATUS_AND_TESTING.md](Docs/STATUS_AND_TESTING.md).

## If something goes wrong

- **Red error about "Input System" or "UnityEngine.Input", or the controller does nothing:** use **Neon Syndicate > Repair Project Setup**, then restart Unity. (Manual fix: Edit > Project Settings > Player > Other Settings > Active Input Handling: **Both**. Window > Package Manager should list **Input System**.)
- **Picture too dark or too bright:** Esc > Settings > Graphics has a **Brightness** slider and an **Eye adaptation** toggle. Night is meant to be moody but readable.
- **Post effects look too strong or too soft:** Esc > Settings > Graphics has bloom (post effects), ambient occlusion, motion blur, film grain and eye adaptation toggles; the preset picks the rest.
- **Pink or magenta objects:** the Built-in Render Pipeline is the default and the recommended choice. Run **Neon Syndicate > Repair Project Setup**. An optional URP beta exists: **Neon Syndicate > Graphics > Switch to URP (beta)** (see [Docs/URP_MIGRATION.md](Docs/URP_MIGRATION.md)). If magenta shows up on URP, note which object it is, then go back with **Neon Syndicate > Graphics > Switch back to Built-in**. Don't switch pipelines any other way, and don't use HDRP.
- **Low frame rate:** press Esc > Settings and lower the graphics quality (Low, Medium, High, Ultra) and the traffic density.
- **Mouse not working in game:** click inside the game window. Esc pauses and frees the mouse.
- **Anything else:** open Window > General > Console, copy the first red error, and send it to Claude with a note about what you were doing.
- **Saves and screenshots:** **Neon Syndicate > Open Save and Photo Folder**. To start completely fresh: **Neon Syndicate > Delete All Saves**.

## What's in this folder

| Folder | What it holds |
|---|---|
| `Assets/NeonSyndicate/Scripts/Runtime` | The game itself, about 91,000 lines of C#, grouped by area: AI, Audio, Characters, Combat, Core, Creatures, Gameplay, QA, UI, Vehicles, World |
| `Assets/NeonSyndicate/Resources` | Four shaders (two for the default Built-in pipeline, two for the URP beta) and the quest and world event data files (`Quests`, `Events`) |
| `Assets/NeonSyndicate/Scripts/Editor` | One-click project setup and the Neon Syndicate menu (play, build, repair, saves, graphics, QA) |
| `Assets/NeonSyndicate/Tests/EditMode` | The automatic tests of the game's logic (run them with the test scripts or Unity's Test Runner) |
| `Docs` | Controls, cheats, features and sources, testing status, tooling, one page per Phase 3 system, the roadmap, and offline renders of the Phase 2 characters (`Docs/Phase2_Previews`) and the Phase 3 creatures (`Docs/Phase3_Previews`) |
| `tools` | The build and test scripts for Windows and the Mac ([Docs/TOOLING.md](Docs/TOOLING.md)) |
| `Browser3D` | The browser version (see above) |
| `Archive/Originals` | The six original zips, untouched |
| `Archive/Skyline_Web_Prototype` | The browser prototype. Open `index.html` in a web browser |
| `Packages`, `ProjectSettings` | Unity project files. Unity fills in the rest on first open |
