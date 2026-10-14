# Animation (Phase 4)

Everything a person does on screen comes from one pose solver (`Characters/HumanoidPose.cs` plus its partial files) that
works out every bone each frame from what the character is doing. Phase 4 reworked how people walk and run, how they
hold weapons, and added poses for the travel powers and the hover bike. Nothing here has been run inside Unity yet:
the numbers below come from the offline solver and the pictures from an offline renderer using the game's real
character meshes. Please check the list at the end in the editor.

## What changed, in plain words

- **Walking is upright.** The old walk reached too far forward with each step, so the body sank between steps and the
  knees stayed bent (a crouch). Now the heel lands closer, the knee is nearly straight when the heel touches down, bends
  about 15 to 20 degrees as the body takes the weight, straightens again over the foot, and the body rises and falls
  about 4 cm per step. The arms hang relaxed and swing opposite the legs with soft elbows.
- **Running has a real float.** Jogging and sprinting now have a moment with both feet off the ground, the knee folds
  high behind (about 100 degrees jogging, 125 sprinting), the elbows pump at about 90 degrees front to back (no more
  arms held out in front), and the body leans a little more the faster you go (about 5 degrees jogging, 9 sprinting).
  The old sprint dropped into huge lunging splits; that is gone.
- **Every speed from a stroll to 10 m/s** picks its own step rate and step length from published human data, so the new
  jog (4.4 m/s) and sprint (8.2 m/s) look right, and feet that are on the ground stay put (foot sliding went down at
  almost every speed, see the table).
- **Small differences between women and men**: women step a little narrower and quicker with a bit more hip sway.
- **Weapons sit in the hand.** A gun's grip now sits inside the curl of the fingers with the slide or receiver above the
  index finger, and the solver aims the hand so the barrel points exactly where you aim. Rifles are shouldered (stock in
  the shoulder pocket, head dipped to the stock), pistols are held out in both hands at eye height, a minigun is braced
  at the hip, a launcher rests on the shoulder. The left hand is placed on the gun after the right hand is solved, so it
  can never float off the weapon. Recoil kicks the muzzle up; a reload takes the left hand to the magazine, down to a
  pouch on the hip and back. Not aiming, rifles sit at a low ready (stock in the shoulder, muzzle down) with both hands
  on them; pistols hang at the side muzzle down.
- **Two-handed melee weapons** keep both fists on the haft through the carry and every swing.
- **Travel powers** have their own body poses (set by the travel code through `HumanoidAnimator.Travel`,
  `TravelBlend` and `TravelLean`): super speed (deep lean, hard arm pump, high knees, a stylised fast stride that still
  reads at 30 m/s), the leap (crouch, explosive stretch, tuck at the top, legs reaching down on the way down), the glide
  (arms spread, legs trailing, banking), flight (tilted into the direction of travel), flat-out flight (body level, one
  fist forward), hovering (upright, legs together, gentle drift) and the heavy landing (deep crouch, a hand on the
  ground, rising as the blend fades). They all fade in and out smoothly.
- **Riding the hover bike** (`HumanoidAnimator.Ride = Bike`): astride the seat with the knees bent and gripping, feet on
  the foot pegs, hands on the handlebars, crouching lower over the bars with speed, leaning with the bike and looking
  where it steers. A one-handed drive-by keeps the left hand on the bar. If the bike model is missing an anchor, a
  typical bike layout around the seat is used instead.
- **Sitting at a car wheel** now holds the actual rim: both hands turn with the steering, and a drive-by keeps the left
  hand on the wheel.
- **Finger grips are ready.** Every frame the animator asks for the right finger pose per hand (pistol, rifle, support
  hand, handle, handlebar, fist, open hand for casting, relaxed, a phone grip) and eases between them. The hands have no
  finger bones yet, so nothing moves today; the later character pass only has to fill in `HandPose.Apply`.

## What to look at in Unity

1. Walk around (hold the walk key), then jog and sprint on flat ground: upright walk, straight knee at heel strike,
   feet not sliding, a visible float when sprinting, arms pumping rather than held forward.
2. Lock on to an enemy and strafe left and right, backpedal, stop and start, turn on the spot: feet stepping cleanly.
3. Crouch-walk, jump, dodge-roll, backstep, guard and parry, get staggered, drink a tonic: these should look as before.
4. Pick up a rifle: low ready while walking and jogging, both hands on the gun. Aim (gamepad left trigger) in every
   direction: stock in the shoulder, head near the sight, the barrel on the crosshair. Fire on full auto (recoil) and
   reload (left hand to the magazine and back).
5. Pistol and SMG: two-handed aim at eye height; one-handed out of a car window and from the hover bike.
6. Launcher (on the shoulder) and minigun (at the hip) aimed in several directions.
7. A two-handed melee weapon (the maul, the bat): both hands on the handle while carrying and swinging.
8. Every travel power, including the transitions between them, and the heavy landing.
9. The hover bike at low and high speed, leaning both ways, with and without a drive-by.
10. Children and the humanoid creatures (golems, hollows, wraiths, the djinn) walking, running and fighting.

## Measurements (offline solver, Malik, 60 frames a second)

Human references are adult walking and running data. Hip angles are the thigh against the upright pelvis bone; clinical
gait reports measure against a pelvis tilted forward about 10 degrees, so add 10 to compare with them (the clinical
figure is in brackets).

| | Reference | Before (Phase 3) | After (Phase 4) |
|---|---|---|---|
| **Walk 1.55 m/s** steps a minute | about 115 to 125 | 130 | 130 |
| hip range, degrees | -10 to +30 (clinical) | -22 to +47 (-12 to +57) | -24 to +28 (-14 to +38) |
| knee at heel strike | about 5 | 11 | 4 |
| knee taking the weight | 15 to 20 | 33 | 18 |
| knee at mid-stance | about 5 | 25 (crouched) | 4 |
| knee peak in swing | 60 to 65 | 75 | 64 |
| ankle: lifting / pushing off | about +10 / -15 to -20 | +19 / -10 | +10 / -17 |
| trunk lean | a few degrees at most | 1.3 | 1.0 |
| pelvis rotation / drop, each way | a few degrees | 4.8 / 3.4 | 4.5 / 3.5 |
| body rise and fall | about 4 cm | 4.4 cm | 4.0 cm |
| shoulder swing / elbow | about 20 to 30 each way / 15 to 25 | -26 to +19 / 15 to 45 | -28 to +18 / 10 to 29 |
| **Jog** (old 3.9 m/s, new 4.4 m/s) flight share | yes | 35% | 45% |
| knee at contact / mid-stance / swing peak | 15 to 25 / 40 to 45 / 90 to 110 | 5 / 43 / 91 | 16 / 44 / 100 |
| trunk lean / elbow | about 5 / about 90, pumping | 5.8 / 81 to 100 | 4.8 / 77 to 101 |
| body rise and fall | 6 to 9 cm | 11.5 cm | 5.7 cm |
| arms against legs (correlation, 1 = perfect) | strongly opposite | 0.44 | 0.92 |
| **Sprint** (old 6.8 m/s, new 8.2 m/s) hip range | about -30 to +60 | -65 to +79 | -40 to +55 |
| knee at contact / swing peak | 20 to 30 / 115 to 130 | 18 / 115 | 24 / 125 |
| trunk lean | 5 to 10 | 9.6 | 8.7 |
| body rise and fall | 5 to 6 cm | 12.6 cm (26 cm at 8.2 m/s) | 6.0 cm |

**Foot sliding** (fastest a heel or ball that is on the ground moves, metres a second; lower is better):

| Speed (m/s) | 1.0 | 1.55 | 2.6 | 3.9 | 4.4 | 6.8 | 8.2 | 10 | woman 1.55 | crouch 1.9 |
|---|---|---|---|---|---|---|---|---|---|---|
| Before | 0.17 | 0.16 | 0.29 | 0.26 | 0.19 | 0.20 | 0.22 | 0.24 | 0.30 | 0.23 |
| After | 0.05 | 0.08 | 0.09 | 0.13 | 0.15 | 0.21 | 0.24 | 0.29 | 0.08 | 0.12 |

Aiming (offline): the barrel follows the aim to within 2 degrees for rifles, pistols, SMGs, the launcher and the
minigun, and the left hand ends within 2 cm of the support point (rifle, pistol, SMG, launcher, minigun) or 2 cm at
the far reach of a long machine gun's foregrip.

## The hand frame and the attach table (for the weapon and character passes)

`WeaponGrip.Attach` (Characters/WeaponGrip.cs) places a weapon model under the hand bone using the table in
`Characters/WeaponGrip.Table.cs`, which the pose solver reads too, so the two can never disagree.

Hand frame (both hands, bind pose, arm hanging at the side; origin at the wrist, which is the hand bone): **-Y** runs
from the wrist to the knuckles and fingertips, **+Z** is the thumb and index side, **-Z** the little finger side; the
palm faces **-X** on the right hand and **+X** on the left. The resting finger curl wraps a handle whose axis runs
along Z at about **(-0.035, -0.105, 0)** metres in the right hand at scale 1 (X mirrored on the left). Weapon models
have their origin at the grip, +Z along the barrel or blade, +Y up.

| Hold | Position in the right hand (m, scale 1) | Rotation (Euler degrees) | Notes |
|---|---|---|---|
| None | (0, -0.075, 0.015) | (90, 0, 0) | unchanged: gauntlets sit over the hand |
| Pistol | (-0.034, -0.106, -0.008) | (96, 0, 0) | grip in the curl, slide above the index finger |
| Rifle | (-0.034, -0.106, -0.008) | (118, 0, 0) | steeper grip: the forearm rises to the gun from below |
| Heavy | (-0.036, -0.108, -0.006) | (118, 0, 0) | rear grip of a launcher or minigun |
| Melee | (-0.028, -0.090, 0.012) | (90, 180, 0) | handle on the palm, the blade's edge (model -Y) to the thumb side |
| Thrown | (-0.036, -0.112, 0.004) | (90, 0, 0) | grenade or bottle in the curl |

Positions are multiplied by the rig scale; the left hand uses the mirror image (x negated, rotation mirrored, model
scale x = -1), as before. Euler (90, 0, 0) lays the barrel along the knuckle line with the top of the gun on the thumb
side; values above 90 tip the barrel toward the little finger. To adjust a hold, change its row: the game and the solver
both follow.

The **Foregrip** child of a two-handed model marks the axis of whatever the support hand wraps (the middle of the
handguard, or a vertical grip's centre). Without one, the animator estimates it from the mesh bounds (rifles under the
handguard a little under halfway to the muzzle, a hip-held heavy weapon on its top handle), and a pistol without one is
held with the left hand cupping the right. A heavy model whose bounds reach more than 0.3 m behind the grip is treated
as a shoulder launcher.

## Notes for the travel and bike code

- `TravelLean` is in degrees: x pitches the body forward, z banks it (positive leans right, matching `RideLean`, where
  negative leans left). Glide uses at least 50 degrees of pitch and flat-out flight at least 80.
- The rider's root is expected to sit at the bike's `Seat` anchor, upright and turning with the bike (yaw only); the
  animator applies `RideLean` to the body itself. Anchors are read every frame from `RideRoot`'s children
  (`RideAnchors` names, found at any depth). Speed for the forward crouch comes from the rider's own movement.

## Files

- `Characters/HumanoidPose.cs`: gait (cadence, stride, stance share, heel-to-toe roll, the knee-driven pelvis height,
  swing leg curves), planting, attacks and dodges as before.
- `Characters/HumanoidPose.Weapons.cs`: carries, aiming, recoil, reloads, the support hand, finger grip choice.
- `Characters/HumanoidPose.Travel.cs` and `Characters/HumanoidPose.Ride.cs`: travel power poses, car seat and bike.
- `Characters/WeaponGrip.cs` and `Characters/WeaponGrip.Table.cs`: weapon placement and the attach table.
- `Characters/HumanoidAnimator.cs`: feeds the solver (aim, recoil, reload, the held model's foregrip and stock, bike
  anchors, travel poses) and drives the finger grips.
- `Tests/EditMode/GaitTests.cs`: joint angles per speed, foot sliding from 0.5 to 10 m/s, arms against legs, travel and
  riding poses, aimed guns following the aim with the support hand on the gun.
- Previews in `Docs/Phase4_Previews`: `01_walk.png`, `02_jog.png`, `03_sprint.png`, `04_rifle_aim.png`,
  `05_pistol_aim.png` (top row Phase 3, bottom row Phase 4; the weapons there are stand-ins built from the same boxes
  as the game's models).
