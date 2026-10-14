# Cover and enemy tactics (Phase 6)

Gunfights now play like a tactical cover shooter: Malik snaps to cover, slides along it, pops out to shoot or fires
blind, rounds corners and dashes from cover to cover, and armed enemies do the same against him: they take cover facing
him, peek and shoot, pin him down, send flankers round and flush him out with grenades. Every enemy squad mixes five
roles. Nothing here has been run inside Unity yet: the rules are tested offline and the code compiles in all twelve
configurations, but the feel (camera, timing, poses, how enemies move) must be checked in the editor (list at the end).

## Taking cover

There is no hand placed cover anywhere. Any collider of the built city counts: building walls, parked hover cars,
crates, barriers, low walls, planters. Cover is found with two probes from where Malik would stand:

- a probe at waist height (0.9 m) must hit a near vertical surface (a curb or a step does not count);
- a probe at head height (1.55 m) decides **low cover** (open above: crouch behind it) or **high cover** (blocked:
  stand behind it);
- side probes 0.45 m to each side find the cover's **ends** (where you can lean out or round a corner).

Walk up to cover (within 1.8 m, in the direction you move or the camera looks) and a ring with the button marks the
spot: green for low cover, blue for high. Press **X** (keyboard) or **A / Cross** (pad, only while the prompt shows; with
no prompt A jumps as before). Malik slides into place facing the cover, crouched behind low cover or standing turned
along high cover, and the camera moves to the shoulder on the side he is looking along.

## In cover

| What | Keyboard and mouse | Controller |
|---|---|---|
| Slide along the cover (stops at its ends) | Movement keys | Left stick |
| Pop out and aim | Hold right mouse | Hold LT |
| Fire aimed, or blind fire without aiming | Left mouse | RT |
| Round a corner at an open end | Push toward the end, tap X | Push toward the end, tap A |
| Dash to the cover you are looking at | Hold X | Hold A |
| Vault over low cover / climb high cover up to about 2.3 m | Space | Tap A |
| Leave | Tap X or pull away | B (a roll out) or pull away |

- **Popping out**: behind low cover Malik rises into a half crouch and aims over the top. Behind high cover he steps
  0.62 m out at the nearest open end (on a pillar, the side he is looking along). In the middle of a long high wall
  there is nothing to lean out of: the HUD says "Move to an edge of high cover to lean out".
- **Blind fire**: firing without aiming raises the gun over low cover (or out past the edge of high cover; over the top
  in the middle of a high wall) while the head stays down. It takes 0.12 s to come up and its spread is the hip spread
  times 2.4 plus 3.5 degrees. Works with every gun, loot guns included.
- **Corner swap**: at an open end, push toward it and tap the cover button: Malik moves round onto the side face of the
  same obstacle (a car's nose, a crate's side). When there is no side face (a thin wall) he just turns to look that way
  and the camera swaps shoulder.
- **Cover-to-cover dash**: look at other cover between 2 and 13 m away, within 16 degrees of the crosshair, with a clear
  run to it. A gold ring marks it; hold the cover button (0.32 s, the ring fills) and Malik sprints there at 7.6 m/s in
  a low run and snaps into it. A roll cancels the dash.
- **Leaving**: pull the stick away from the cover for 0.18 s, tap X, or tap B (that rolls out). Rolling, a melee
  strike, being staggered, a vehicle, a travel power or the cover disappearing (a car driving off) also end it.

### Protection

Hits from the front of the cover (within 70 degrees of the way it faces) are reduced:

| | Bullets and crystal power shots | Explosions |
|---|---|---|
| Hidden behind low cover | 22% gets through | 50% |
| Hidden behind high cover | 15% | 50% |
| Blind firing | 35% | 50% |
| Popped out (low / high) | 60% / 65% | 80% |
| Flanked (hit from the side or behind), melee, fire, falls | full damage | full damage |

The cover panel at the bottom of the HUD flashes "PROTECTED -78%" when the cover soaks a hit. Enemies in cover get a
milder version (the same table moved a third of the way back to full damage) so a fight never stalls. This multiplies
the difficulty's own damage scale and the gear armour, and runs after dodges, guards and parries.

## Enemy tactics

Armed gang members, police, SWAT and hostile crews (bounty crews, mission enemies) use tactics when they fight the
player with a gun. Creatures, companions, melee enemies and fights between NPCs keep the older fighting code, and
everyone falls back to it while the player is in a vehicle.

- **Cover**: an enemy scans about 11 m around itself for cover that hides it from the player, near its preferred
  range, not taken by a squadmate and not the player's own spot (high cover only where it ends, so it can lean out).
  It runs there (shooting on the move only when close), then hides and peeks out on a cycle: hidden for about 1.4 s,
  out shooting for about 1.9 s (Riflemen on Balanced; longer hiding on Story, shorter on Veteran). Cover the player
  has walked round is abandoned at once.
- **Pinning fire**: while you hide, enemies who saw you in the last 3 seconds keep firing at the cover you went behind
  (wide spread, cracking into its edge and over the top). Only a few enemies get real shots at once (the existing shot
  tokens: 1 on Story, 2 on Balanced, 4 on Veteran); the rest fire wide.
- **Flanking**: once there are three or more enemies and you are in cover, one (two on Veteran, or on Balanced with five
  or more) runs round to a point about 80 degrees to the side at 8.5 to 12 m. A flanker that gets a clear shot past
  your cover opens fire at once. Badly hurt enemies never flank.
- **Grenades**: stay in one cover spot too long (9 s on Story, 6 on Balanced, 4 on Veteran) and a Rifleman or Engineer
  lobs a grenade at it (aim error 3 m / 1.8 m / 1 m). The squad shares one grenade timer (18 / 12 / 8 s). A blinking
  "GRENADE" ring marks where it will land.

### Roles

Each enemy gets a role the first time it fights you with a gun. Ambient enemies switch to the role's gun; mission
enemies, chest and bounty crews and champions keep the gun they were given and get a role that suits it.

| Role | Plays like | Gun (ambient) | Numbers |
|---|---|---|---|
| Rifleman | takes cover, peeks, pins, flanks, throws grenades | Arc Carbine | prefers 15 m |
| Rusher | charges straight in, cover to cover only from far away, strafes at close range | Glass Shot Pump | prefers 4 m, 15% faster |
| Heavy | walks in the open hosing a machine gun in long bursts, never takes cover | Cinder LMG | 62% speed, +160 armour (80 Story, 208 Veteran), more poise |
| Marksman | perches in cover far back; a red laser tracks you, then holds still, then one big shot | Long Watch | prefers 28 m; laser 2.1 / 1.5 / 1.1 s; shot damage 40% / 55% / 75% |
| Engineer | drops an auto turret, then fights like a Rifleman | Coil Burst SMG | turret every 40 / 28 / 20 s, at most 1 / 2 / 2 standing |

Role weights (Rifleman, Rusher, Heavy, Marksman, Engineer): police 55, 15, 0, 15, 15; SWAT 35, 15, 25, 15, 10; gangs
40, 30, 8, 10, 12; hostile crews 35, 20, 15, 15, 15. Heavies, Marksmen and Engineers count half on Story and 1.3 times
on Veteran.

The Marksman's laser tracks you for the first three quarters of its time and then locks: that is the moment to get
behind something or dodge. It beeps when it starts. Losing sight of you cancels it.

The **auto turret** (a small tripod with a red sensor eye) unfolds next to the Engineer, turns at 110 degrees a second,
fires bursts of 4 to 7 shots (7 damage each, before the difficulty's damage scale) at you inside 30 m with line of sight,
and folds away after 34 s. It has 60 / 90 / 120 health; shoot it to break it.

## HUD

- The cover prompt: a ring and "[X] COVER (low)" on the spot (the pad button when you last used the pad).
- In cover, a panel at the bottom: LOW or HIGH COVER, AIMING or BLIND FIRE, the open ends, the button hints, and the
  PROTECTED flash.
- The dash target: a gold ring that fills while you hold the button.
- "GRENADE" warnings, the lean-out hint and the first three times you take cover a hint line with the buttons.
- F1 lists the cover key.

## Debug (F2 > Cover, Order 50)

Show every cover spot around you (green low, blue high, gold arrows at open ends, red labels on spots enemies hold),
switch enemy cover tactics off (back to the older gun fighting) and on, spawn a squad of four Riflemen, Rushers,
Heavies, Marksmen or Engineers (or a mixed squad of five) about 24 m ahead of the camera, clear them, and watch the
live numbers (enemies using tactics, flankers, turrets, the grenade timer, each enemy's role, tactic and range).

## Files

- `AI/CoverRules.cs` (pure): probe heights, low or high, damage reduction, sliding, leaning, corners, blind spread, dash
  targets, enemy cover scoring. Tested by `Tests/EditMode/CoverTests.cs`.
- `AI/TacticsRules.cs` (pure): roles, weights, profiles, the tactic choice, grenade, flank and turret allowances, flank
  points, grenade lobs. Tested by `Tests/EditMode/TacticsTests.cs`.
- `AI/CoverFinder.cs`: the physics probes (spots, edges, refresh, corners, dash targets), the ICoverUser interface and
  the CoverDamageFilter that wraps a character's damage filter.
- `AI/CoverDirector.cs` and `CoverDirector.Ui.cs` (`Game.Cover`, boot step "Cover tactics"): squad bookkeeping, hints,
  the HUD and the debug page.
- `AI/NpcController.Tactics.cs` and `NpcController.Roles.cs`: enemy tactics and roles (hooked from `UpdateFight`).
- `AI/TacticalTurret.cs`: the Engineer's turret.
- `Characters/PlayerController.Cover.cs` (hooked from `Update` and `UpdateWeapons`), `PlayerCamera.Cover.cs` and
  `HumanoidPose.Cover.cs` (the cover poses, fed by `HumanoidAnimator.Cover`, `CoverHigh`, `CoverSide`, `CoverBlind`).

Nothing is saved (squads, claims and turrets only live during a fight).

## What to check in Unity

1. Walk up to a low wall, a parked car and a building wall: the prompt shows the right colour on the right spot; X and
   pad A take cover; with no prompt pad A still jumps and climbs.
2. Slide along each: Malik stops at the ends, the camera follows to the shoulder he looks along, low cover keeps his
   head under the top, and the camera still sees over it.
3. Pop out (RMB / LT) over low cover and at both ends of high cover; aim, fire, reload. Blind fire with a pistol, an
   SMG, a rifle, a shotgun and a loot gun: the gun over the top, bullets leave above the cover.
4. Round the corner of a car and a crate (push toward the end, tap X / A). Dash between two cars (look, hold X / A).
   Vault a low wall (Space / A), climb a chest-high crate stack from high cover, leave with X, B and by pulling away.
5. F2 > Cover: spawn each squad. Watch Riflemen take cover facing you and peek, the flanker run round, the grenade come
   after you sit still, the Heavy walk in, the Marksman's laser (and that the lock gives time to duck), the Engineer's
   turret (shoot it). Try Story and Veteran.
6. Ordinary street fights with gangs and police: enemies use cover and roles; police arrests at one star still work.
