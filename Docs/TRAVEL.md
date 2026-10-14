# Travel powers (Phase 4)

Getting around Las Veyra should feel like a superhero open-world game: you run faster every day, and three travel
powers (super speed, the super leap with its glide, and flight) are how you cross the city. This page explains what
each power does, the buttons on keyboard and controller, when each one unlocks, and what to check in Unity.

Nothing here has been run in Unity yet. The code compiles in all twelve configurations and the maths is tested
offline, but how it looks and feels (speed, camera, animation, effects, sound) must be checked by playing.

## Faster everyday movement

| | Before | Now |
|---|---|---|
| Walk | 1.55 m/s | 1.55 m/s |
| Jog | 3.9 m/s | 4.4 m/s |
| Sprint | 6.8 m/s | 8.2 m/s (skills like Quick Feet and Gale Runner still add to it) |
| Speeding up to a full sprint | a little under half a second | about a third of a second |
| Stopping from a sprint | about 0.3 seconds | about a quarter of a second |

Sprinting no longer costs stamina outside a fight. In a fight (the game's usual "in combat" rule, which stays on for
six seconds after the last hostile action) sprinting drains stamina exactly as before.

## The powers

### Surge (super speed), from the start

- **Turn it on**: while sprinting, press the crouch button (**Left Ctrl**, or **L3** on a controller). Before this
  change that button did nothing while sprinting, so it clashes with nothing.
- **What happens**: you ramp from a sprint to about 30 m/s (67 mph) in about 1.2 seconds. You do not need to keep
  holding sprint. A light push on the stick gives a slower Surge, a full push the top speed.
- **Steering** has momentum: at top speed you turn in a wide arc (about 20 m across), at lower speed much tighter.
  Pulling the stick the opposite way brakes hard and turns you round.
- Curbs, steps and slopes are climbed at speed; low things (cars, benches, low walls up to about 1.9 m) are vaulted
  automatically. Run off a roof and you fall safely, and if you keep the stick pushed you run straight on when you land.
- **People** right in front of you are knocked aside; people a little further ahead leap out of the way. Brushing past
  a civilian gives no wanted level. Plowing into police counts as attacking an officer (the normal rules).
- **Walls**: hit a wall head-on at speed and you bounce off and stumble for a moment (no damage). Glance off it and you
  run on along it.
- **Jump during a Surge** for a long leap (up to about 16 m high and about 60 m long) once the Super Leap is unlocked;
  before that it is a running hop.
- **It ends** when you let go of the stick, press the crouch button again, aim, attack, roll, guard, take a big hit
  (20 damage or more), get knocked down or get into a vehicle.
- **Looks and sounds**: the field of view widens, the camera drops lower and pulls back, speed streaks rush past, a
  trail of see-through afterimages follows you, dust kicks up behind, a soft boom goes off when you hit top speed, and
  the wind gets louder the faster you go.

### Super Leap and Glide, after the first story mission

- **Super Leap**: crouch (**Left Ctrl** or **L3**), then hold **Space** (or **A / Cross**) and let go. A quick tap
  leaps about 12 m high; holding for about a second charges a full leap of about 28 m. The HUD shows the charge.
  Push the stick while you let go to leap forward. You never take fall damage after a leap.
- **Glide**: in the air, press and hold **Space** (or **A / Cross**). You sink slowly (about 3.5 m/s) and sail forward
  (about 14 m/s). Steer with the stick, or leave the stick alone and the glide drifts toward where the camera looks.
  Let go to drop. Gliding works after a leap, after a Surge leap, or simply after falling off something.
- **Landing**: a hard landing is a heavy crouch-and-rise with a dust ring and a camera shake; a glide lands softly.

### Flight, after the second story mission

- **Take off**: double-tap **Space** (or **A / Cross**) in the air. A jump followed quickly by a second press works,
  and so does a double-tap while gliding or falling.
- **Fly**: the stick (or W A S D) moves you, the camera steers. The camera's tilt tilts your flight: look down and push
  forward to dive, look up to climb. The camera's normal resting angle flies level.
- **Rise and sink**: hold **Space / A** to rise, hold **Left Ctrl / L3** to sink.
- **Speed**: about 20 m/s (45 mph) cruising; hold **Left Shift** (or **B / Circle**) with the stick forward to fly
  fast, about 45 m/s (100 mph).
- **Hover**: let go of everything and you hover in place with a gentle bob. You can aim (**right mouse / LT**) and
  shoot (**left mouse / RT**) while flying; aiming slows you to a hover so you can line up shots.
- **Stop flying**: double-tap **Space / A** again. You fall (safely) and can glide by holding the button.
- **Land**: sink onto the ground for a soft landing, or dive into it at speed for a heavy landing.
- **Limits**: you cannot climb above about 450 m (climbing slows down and stops). Buildings stop you or slide you
  along; you never pass through them. Hitting a wall hard at speed bounces you back a little (no damage).
- **Camera**: pulls back and widens with speed, follows with a slight lag, banks gently into turns and still avoids
  walls.

## Rules

- **Unlocks**: Surge is available from the start. The Super Leap and Glide unlock when the first story mission is
  passed, Flight when the second is passed. A banner and a button hint announce each one. Free Roam has everything
  from the start (with a short introduction). Once earned, an unlock is saved. Old saves get the unlocks their story
  progress implies.
- **Cost**: travel powers are free (no energy, no stamina).
- **Wanted level**: travel powers are always allowed, even while wanted. They add no heat by themselves. Police cars
  and police helicopters chase you as usual; flying high gets you away from cars but not from helicopters.
- **Where**: everywhere on foot, including inside the Veil. Not in vehicles, not in cutscenes and not while knocked
  down. Getting into a vehicle, climbing a ledge or being knocked down ends any power.
- **Button hints**: the first few times each power is relevant, a hint shows the buttons for the device you used last
  (keyboard or controller). Each power's hint stops after you have used it three times.
- **HUD**: while a power is active, a small panel at the bottom centre shows its name, your speed (mph), your altitude
  in flight, and a bar (speed, or the Super Leap's charge).

## Debug tools

F2 opens the debug menu; its **Travel** tab has:

- **Unlock all travel powers** (saved with the game until you switch it off),
- **Back to story unlocks** (the unlocks follow the story progress again),
- **Show the button hints again**,
- live numbers: the current power, speed and height, and how often each power has been used.

## What to test in Unity

1. **Everyday speed**: jog and sprint around the city on keyboard and controller. Does it feel faster without feeling
   slippery? Stamina should stay full while sprinting outside fights and drain while sprinting in a fight.
2. **Surge**: sprint, press Left Ctrl / L3. Check the ramp to top speed, the wide turns, the U-turn brake, letting go
   to stop, pressing the button again to stop, and that aiming or attacking stops it.
3. **Surge in the city**: run up curbs and stairs, vault over cars and benches, run off a roof (you should land safely
   and keep running if the stick is pushed), hit a wall head-on (stumble) and at an angle (slide along).
4. **Crowds**: Surge through pedestrians (they should be knocked aside or jump away, no wanted level), then into a
   police officer (you should become wanted).
5. **Effects and sound**: field of view, the lower camera, streaks, afterimages, dust, the boom at top speed, the wind.
   Check frame rate while surging through a busy street (the afterimages bake the body mesh about 13 times a second).
6. **Super Leap**: tap and full charge from a crouch, with and without the stick pushed; no fall damage on landing;
   the heavy landing pose and dust.
7. **Glide**: hold Jump after a leap's peak, steer with the stick, then with the camera only; let go to drop.
8. **Flight**: double-tap from the ground and from a glide; cruise, fast, rise, sink, hover, aim and shoot while
   hovering; dive and climb by looking down and up; fly into buildings (no passing through); fly up to the ceiling;
   double-tap to stop; land softly and at speed.
9. **Unlocks**: start a new story game (only Surge works), pass mission 1 (Leap and Glide unlock with a banner), pass
   mission 2 (Flight). Load an old save past mission 2 (everything should work). Start Free Roam (everything works).
10. **Animation**: the animation pass draws the poses (Surge, Leap, Glide, Fly, Fly fast, Hover, Land). Check that the
    body leans and banks the right way in flight and glides, and that poses blend in and out without pops.
11. **Edge cases**: get knocked down mid-flight, open the phone or map mid-flight (you should hover), enter a vehicle
    right after a Surge, use the powers inside the Veil, and use them while wanted.

## Tuning

All the numbers live in one file, `Scripts/Runtime/Gameplay/TravelMath.cs`, with a plain-English line each
(speeds, ramp time, turning grip, leap heights and charge time, glide sink and speed, flight speeds, the ceiling, the
double-tap window, which story mission unlocks what). The EditMode tests in `Tests/EditMode/TravelTests.cs` check
that the numbers still do what this page says.

## For developers

- Files: `Gameplay/TravelMath.cs` (pure numbers and rules, tested), `Gameplay/TravelPowers.cs` (Game.Travel: unlocks,
  saves, announcements, hints, sounds) and `TravelPowers.Ui.cs` (HUD panel, F2 page), `Characters/PlayerController.Travel.cs`
  (the movement, poses and effects), `Characters/PlayerCamera.Travel.cs` (camera framing), `Combat/Fx.Travel.cs`
  (streaks, dust, boom, landing rings, afterimages), `Audio/TravelSounds.cs` (synthesized sounds and their player).
- Input: `Act.JumpHeld`, `Act.SurgeToggle`, `Act.FlyUpHeld`, `Act.FlyDownHeld`, `Act.FlyFastHeld` (no new keys; they
  reuse Jump, Crouch and Sprint).
- Animator contract, set for the player every frame: `HumanoidAnimator.Travel` (Surge, Leap, Glide, Fly, FlyFast,
  Hover, Land; None otherwise), `TravelBlend` (eases in; when a power ends the last pose is kept while the blend eases
  out to 0, then Travel returns to None; for Land the blend starts at 1 on impact and falls to 0 as the body rises) and
  `TravelLean` in degrees: x is the forward pitch (0 upright, 90 flat face down, above 90 diving head first), z is the
  bank (positive banks right into a right turn, negative left, the same sign as `RideLean`).
- Save: the `TravelSave` block (unlocks, the debug unlock, use counts for the hints).
