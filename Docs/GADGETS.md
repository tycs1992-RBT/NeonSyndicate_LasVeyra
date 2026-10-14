# Gadgets

Gadgets are Malik's tech skills: six gadgets with two variants each, carried in two gadget slots and run on
**cooldowns, not crystal energy**, so they sit beside guns and crystal powers instead of competing with them. They
unlock as you level up, gadget **mods** drop from kills, and the build sets in `Docs/PROGRESSION.md` (section 8,
BUILDS) can shorten their cooldowns and make them stronger.

## Controls

| Action | Keyboard and mouse | Controller |
|---|---|---|
| Use the gadget in slot 1 | Hold 1 (a ring shows where it lands), let go to deploy | Hold D-pad left, let go to deploy |
| Use the gadget in slot 2 | Hold 2, let go | Hold D-pad right, let go |
| Aim where it lands | The crosshair (aiming down sights is not needed) | The right stick |
| Choose gadgets, socket and salvage mods | Pause menu > GADGETS | Pause menu > GADGETS (LB and RB switch tabs, D-pad and A) |
| Pick up a gadget mod | Walk over the small chip in a short light beam | The same |

- While a gadget sits in slot 1 or 2, number keys 1 and 2 use it and no longer pick the melee weapon or the handgun.
  The mouse wheel, the weapon wheel and keys 3 to 8 still switch weapons; an empty slot gives its number key back.
- On a controller a **tap** of D-pad left or right still switches weapons (decided when you let go, like the reload
  tap on D-pad down). Holding it for 0.3 s starts the gadget instead.
- The ring under the crosshair turns red where the gadget cannot go (no ground, too far up or down, behind a wall it
  would have to pass through); letting go there does nothing and costs no cooldown. Thrown gadgets land at least
  1.5 m ahead and no farther than their range, and stop short of walls. The Palisade shows a ghost of the wall.
- Self-cast gadgets (the drone, the handheld shield and the Ripple scanner) work on you the moment you let go.
- A gadget on cooldown buzzes and shows its seconds left. Each slot has one live gadget: using it again replaces the
  old one. Gadgets cannot be used in vehicles, while knocked down or in menus.
- Three first-time hints teach the keys during fights (F2 > Gear shows them again).

## The gadgets

Base numbers at level 1 with no mods; damage, healing and shields grow with the player's level (x1 at level 1, about
x2 at level 50). Every hit goes through the shared damage API with the player as the source, so kills, loot,
reputation, the bestiary and the wanted level count exactly as for your own weapons (a turret that shoots a police
officer is a crime you committed).

| Gadget | Variant | Unlock | Cooldown | What it does |
|---|---|---|---|---|
| Chime Scanner | **Ripple** | level 1 | 18 s | A pulse that marks every enemy within 40 m through walls for 10 s: a marker, health and armor bars and the distance over each one |
| Chime Scanner | **Lance** | level 8 | 20 s | A narrow pulse along the crosshair (60 m, 22 degree cone) that marks and **exposes** for 8 s: exposed enemies take 15% more damage |
| Spindle Turret | **Needle** | level 2 | 40 s | A tripod turret thrown up to 18 m: 7 damage rifle shots, 5 a second, at the nearest enemy it can see within 32 m, for 20 s |
| Spindle Turret | **Kiln** | level 9 | 38 s | A flame turret (14 m throw): a 9 m, 40 degree cone of fire, 20 fire damage a second plus burning, for 14 s |
| Skitter Mine | **Hound** | level 3 | 22 s | A legged mine thrown up to 20 m that arms in 0.6 s, scuttles to the nearest enemy within 20 m and bursts: 120 damage in 4 m (full in the middle, 40% at the edge, less through cover). Waits as a proximity mine; bursts after 12 s anyway |
| Skitter Mine | **Litter** | level 10 | 26 s | A canister that splits into four small seekers, each chasing a different enemy: 45 damage in 2.6 m each |
| Lattice Shield | **Palisade** | level 4 | 35 s | A 3.2 m wall of hard light placed up to 10 m away, facing away from you: 450 shield points for 30 s. Enemy bullets and blasts stop on its face; yours pass out through the back; people and cars walk through it |
| Lattice Shield | **Buckler** | level 11 | 28 s | A handheld shield that stays in front of you, facing where you aim: 260 shield points for 12 s while you shoot |
| Vapor Launcher | **Balm** | level 5 | 28 s | A canister lobbed up to 20 m that bursts into a 4.5 m healing cloud for 9 s: 10 health a second while you stand in it |
| Vapor Launcher | **Scald** | level 12 | 26 s | The same cloud, burning: 8 fire damage a second plus burning to every enemy inside |
| Moth Drone | **Gadfly** | level 6 | 45 s | A four-rotor drone that rides your shoulder for 25 s and zaps the nearest enemy it can see within 24 m: 9 damage every 0.55 s |
| Moth Drone | **Suture** | level 13 | 50 s | A healing drone that follows you for 18 s: 7 health and 2 armor a second |

Each new gadget announces itself with a GADGET UNLOCKED banner and goes into an empty slot if you have one (a new
game starts with the Ripple scanner in slot 1). Old saves are given every gadget their level has earned on first load.
Shields blink in their last two seconds and when nearly broken, then shatter.

## Gadget mods

Mods are chips that drop from kills (Shard-Touched elites 25%, champions 60%, bosses always, ordinary fighters 3%,
creatures 2%, elite creatures 15%, great beasts 40%; Veteran x1.25). The rarity uses the gun drop tables and the city
tier's rarity bump. Socket one mod per slot in the GADGETS tab; it improves whatever gadget is in that slot.

| Mod | Common | Mythic | Effect |
|---|---|---|---|
| Quickcharge | 4 to 6% | 19 to 21% | Shorter cooldown |
| Overclock | 5 to 8% | 27 to 31% | More damage, healing and shield points |
| Longcell | 6 to 9% | 31 to 34% | Longer duration (and longer scanner marks) |
| Wideband | 5 to 7% | 22 to 24% | Bigger blast and cloud radius, longer reach |

Mods and the Gearwright's Wake set add up, but a cooldown is never cut by more than half. Up to 30 mods are kept;
salvaging one pays Lumen (15 for Common up to 640 for Mythic). Mods lying on the ground vanish after five minutes.

## HUD

Bottom right: the two gadget slots, each with its key (1 / 2, or HOLD D-LEFT / HOLD D-RIGHT on a controller), a
ring of dots that fills as the cooldown runs down (it pulses when ready), the seconds left, the variant name, a small
dot in the rarity colour when a mod is socketed and a bar for the time the deployed gadget has left. Under them: the
gear score and the city tier. While a key is held, a line under the crosshair says "Let go to deploy" or "No room to
deploy there".

## Debug (F2 > Gear, Order 51)

Unlock every gadget (saved) or go back to level unlocks, reset cooldowns, show the hints again, drop a mod of each
rarity, wear four Legendary pieces of any build set, force a city tier (not saved), spawn three hostile targets 14 to
18 m ahead (and remove them), plus the gear score breakdown and every active set bonus.

## Saves

`SaveData.Gadgets` (`GadgetSave`): the gadget in each slot, the mod socketed in each slot, the mods, the next mod id,
the F2 unlock flag, the hint count and which unlock banners have been shown. Appended to the save and to the
`SaveDataTests` freeze list (with freeze lists of its own). Cooldowns and deployed gadgets are not saved.

## Merge notes (for the other Phase 6 work)

- **Resonance Bursts**: the combat loop should call `if (Game.Gadgets != null) Game.Gadgets.OnResonanceBurst();` when a
  burst lands. That is all Resonant Circuit's 4 piece bonus needs; until then it does nothing.
- **Armor plates**: the Ripple marks draw the enemy's `CharacterHealth.Armor` as a blue bar. If enemy armor plates use a
  separate pool, point `DrawMarks` (GadgetSystem.Hud.cs) at it.
- **Cover**: the shields' bullet faces are on an unnamed layer, 15 (`GadgetModels.ShieldLayer`), that collides with
  nothing but is inside `Layers.ShootMask`. If the cover finder probes with a mask that includes layer 15, either
  exclude it or keep it on purpose (a Palisade would then count as high cover).
- Shield faces rely on Unity's default "Queries Hit Backfaces" being off (it is: the project does not change it).

## Files

| File | What |
|---|---|
| `Gameplay/GadgetRules.cs` | Pure: the twelve variants, unlock levels, cooldown, power, duration and radius maths, mods, drop chances, the cooldown timer, the save classes |
| `Gameplay/GadgetSystem.cs` | `Game.Gadgets`: slots, unlocks, mods, input and deploy, kill and set hooks, hints, save |
| `Gameplay/GadgetSystem.Units.cs` | The deployed gadgets (turret, mines, drone, shields, clouds, pulses), scanner marks, mod pickups |
| `Gameplay/GadgetSystem.Hud.cs`, `.Ui.cs`, `.Debug.cs` | HUD slots and preview, the GADGETS tab, F2 > Gear |
| `Combat/GadgetModels.cs`, `Audio/GadgetSounds.cs` | Procedural models and synthesized sounds |
| `Gameplay/GearBuildRules.cs`, `GearScoreRules.cs`, `GearScore.cs` | Build sets, gear score and city tier (pure rules plus the live state) |
| `Tests/EditMode/GadgetTests.cs`, `GearBuildTests.cs` | Offline tests |

## What to test in Unity

Nothing here has been run in Unity yet. In order:

1. Start a new game, finish the opening until you can move: a GADGET UNLOCKED banner, the Ripple scanner in slot 1 at
   the bottom right. Tap 1 near enemies: a violet ring sweeps out, markers with health bars show through walls.
2. F2 > Gear > Unlock every gadget, then Spawn 3 hostile targets. In the GADGETS tab put the Needle turret in slot 2;
   hold 2, watch the ring follow the crosshair (red against a wall), let go: the turret flies out, unfolds and shoots.
3. Try every variant on the targets. Check: mines walk to targets and burst, the Litter splits four ways, the Palisade
   stops enemy bullets from the front while your shots pass from behind, the Buckler follows your aim, the Balm cloud
   heals you, Scald sets enemies burning, Gadfly zaps, Suture heals, Lance exposes (red markers, "EXPOSED").
4. On a controller: tap D-pad left and right to switch weapons, hold them to use gadgets.
5. F2 > Gear > Drop a mod of each rarity, walk over them, socket one, check the cooldown number on the left drops.
6. Wear each build set from F2 > Gear and read the green ACTIVE lines on the GEAR tab. Force tier 5, spawn targets,
   check they take longer to kill and that gun drops come at higher item levels.
7. Look for: model scale and placement (turret on its feet, drone at the shoulder, wall upright), the HUD slots not
   overlapping anything at 16:9 and ultrawide, sound volumes, and frame rate with several gadgets out.
