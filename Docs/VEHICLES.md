# Hover bikes and the faster story opening

Las Veyra's motorcycles are hover bikes: one long hull on two glowing thruster pods, ridden astride with the rider in
full view. This page explains the bikes, how to get one, the phone's "Bring my bike", the shorter opening mission and
the new "Start After the Prologue" button, and what to check in Unity. Nothing on this page has been run in Unity yet:
the code compiles in all twelve configurations and the maths is tested offline, but the look and the feel need you.

## The four bikes

| Bike | Make | What it is | Top speed (offline sim) | 0 to 60 mph (offline sim) |
|---|---|---|---|---|
| Vanta Stilettra | Vanta | Sport bike, the story bike. Two seats | about 58 m/s (130 mph) | 1.8 s |
| Anvil Duskhauler | Anvil | Heavy cruiser: long, low, sturdy. Two seats | about 50 m/s | 2.3 s |
| Kestrel Skiff | Kestrel | Cheap courier bike with a top box. Two seats | about 47 m/s | 2.2 s |
| Metro Response Courser | Metro | LVPD pursuit bike: siren, red and blue strobes. One seat | about 56 m/s | 1.9 s |

The quickest car off the line, the Bishop Vektor, takes about 2.4 s to 60 mph on the same maths, so every bike beats it.

How they handle:

- Quick throttle, hard brakes, tight steering at low speed that calms down at high speed.
- Turning is limited by grip, so a bike never spins out from steering alone. Hold the handbrake at speed to let the
  tail step out for a drift.
- The bike leans into turns: the harder and faster the turn, the deeper the lean (up to about 38 degrees). Only the
  model leans; the physics body stays upright, so a bike can never tip over.
- It bobs gently on its thrusters, the front pod turns with the handlebars, and the handlebars turn with your steering.
- A hard crash (or an explosion next to it) throws the rider off. You ragdoll, get up on your own, and can climb back on.
  Riding into the blockade in the prologue is a good way to see it.
- Riders sit in the open: bullets reach a rider more easily than a driver behind car doors.
- Drive-by works like in a car: one-handed guns only, Right Mouse to aim and Left Mouse to fire (LB and RB on a pad).

## Getting on and off

- Walk up to a bike and press F (Y or Triangle on a pad). You step to whichever side is nearer and hop on in about half a
  second. There are no doors.
- If someone is riding it, the same press shoves them off the far side and you take the bike (that is a carjacking).
- Press F again to get off. At speed you are thrown off and roll instead.
- A companion (Luma in the prologue) hops on behind you as a pillion passenger.

## Where to find bikes

- Every safehouse has a bike parked beside its door (a Stilettra at Veyra Lofts, a Duskhauler at the Starlite Motel).
  If you ride it away, a fresh one turns up later when you come back.
- Free Roam starts beside the Veyra Lofts bike.
- About one vehicle in twelve in traffic is a bike, and some parked vehicles are bikes.
- The police sometimes patrol on the Courser, and while you are wanted an LVPD bike joins the chase now and then (up to
  two at three stars and more). Bikes stay in the city streets, not the desert.
- When you load a save from after the prologue (or a Free Roam save) and no bike is nearby, one is parked beside you.

## Bring my bike

Open the phone (P, or hold Back/View on a pad) and choose "Bring my bike". A courier parks your bike at a curb or road
spot 20 to 40 m away (open ground about 22 m away when there are no roads), with a blue "B" marker on the map for 40
seconds.

- It brings the last bike you rode. If that bike is gone (wrecked, or cleaned up far away), you get a new one of the same
  model and colour. If you never rode a bike in this session, you get a Kestrel Skiff.
- There is a 30 second cooldown between deliveries.
- It refuses while you are already riding, and during jobs that forbid it (races).
- Your own bike is kept in the world while it is within 900 m of you.

## The new opening (mission 1, "The Night the Third Eye Opened")

Every story beat and every line's meaning are kept; the waiting and the long run are gone.

| | Before | After |
|---|---|---|
| Opening scene | 2 lines, then walk to Luma, press E, 2 more lines | One scene of 3 lines. Enter: next line. Backspace or B: skip the whole scene |
| First vehicle you can use | A car 214 m away (about 220 m on foot around the canyon's rock wall, which the map arrow points straight through) | A hover bike 4.7 m from where Malik wakes, usable as soon as the scene ends (about 20 s after New Game) |
| Walks | 10 m to Luma, 34 m to the crystal, about 220 m to the car | 24 m to the crystal (or ride there), the bike is 26 m from the crystal and 32 m from the middle of the fight |
| Phone call before the car | Waits 4 s | No wait: Mara calls while you walk to the bike |
| Waiting for Luma at the car | Up to 12 s | Up to 2 s, then she hops on behind you |
| Ride to the blockade | 50 m by car | 276 m by bike across the desert, a straight line clear of the canyon walls |
| Ride to Mara's clinic | 2,418 m by road | The same 2,418 m, on the bike |
| Waiting for Luma at the clinic | Up to 10 s before the talk | None: the talk starts as you get off and Luma walks over during it |
| Time to the escape ride | About 2.5 minutes at best, more if you got lost behind the canyon wall | About 1.5 minutes (sooner if you skip the scene) |
| Whole mission | About 6 minutes at best | About 4 to 4.5 minutes |

Also changed: the hunters gather a little closer (22 m instead of 30 m), so the first fight starts sooner. During the
prologue the story bike cannot be wrecked. After the mission the bike is yours and stays parked outside the clinic, so
the ride to Jax Calder for mission 2 (669 m by road) starts on it.

Dialogue everywhere: Enter (A on a pad) now moves on one line at a time. Before, one press could skip a whole
conversation by accident. Backspace (B on a pad) skips the rest of the conversation on purpose.

## Start After the Prologue

A new button on the title screen, under Free Roam. It starts a new story at mission 2 with everything the prologue
gives: the Third Eye, the Sunbolt spell, full crystal energy, the Vector sidearm with 75 rounds, the $2,500 reward, the
story flag (Mara's clinic is free, the Mara and Ion phone contacts), and the "new safehouse" note. Malik stands outside
Mara's clinic with his hover bike parked beside him, Jax Calder's job is on the map, and the game autosaves. If you
already have saves, it asks "Start a new story after the prologue?" first, like New Game does.

## What to test in Unity

1. New Game: the scene shows the bike to the right of Malik. Press Backspace (B on a pad) to skip the scene, then
   hop on and ride to the crystal. Check the time to your first ride.
2. The bike's look: hull, pods, glow strips, headlight at night, underglow, the bob, the handlebars turning. Check that
   nothing pokes through and that the pods hover just above the ground.
3. The rider on the bike (this depends on the animation worker's riding pose): seated on the saddle, feet on the pegs,
   hands on the grips, leaning with the bike. Luma behind with her arms around Malik.
4. Handling: acceleration, top speed on a long road (the HUD speed should reach about 130 mph on the Stilettra),
   steering at low and high speed, the lean in turns, the handbrake drift, landing jumps. It must never tip over.
5. Crash into a wall or a car at speed: you are thrown off, get up, and can climb back on. Ride into the blockade.
6. Drive-by from a bike, getting shot while riding, and stealing a bike from an NPC rider (the shove).
7. The prologue escape: Luma hops on within about 2 seconds; at the clinic the talk starts right away.
8. Start After the Prologue, Free Roam (bike by the Veyra Lofts door), and loading an autosave made after the prologue.
9. Bring my bike from a street, from the desert, and right after it was used (the cooldown message), and during a race
   (it should refuse).
10. Get one star and watch for an LVPD bike joining the chase.

## For developers

- Data: `Vehicles/VehicleData.cs` (`VehicleClass.Bike`, `BodyStyle.Bike`, `VehicleDef.IsBike`, `VehicleDef.PoliceUnit`,
  the four bikes appended after the cars so old indices never move).
- Model: `Vehicles/BikeModels.cs`. Everything visual hangs under a lean pivot. Each seat has a `RideAnchors` root
  (Seat, HandleL, HandleR, FootL, FootR) that is handed to `HumanoidAnimator.RideRoot`; the hand anchors follow the
  handlebars every frame.
- Physics and riders: `Vehicles/Vehicle.Bike.cs` (hover suspension, grip-capped yaw, roll stabiliser, lean, throwing
  riders). The seat transforms follow the Seat anchors but stay upright, so the rider's root sits on the saddle and the
  animator leans the body by `RideLean`.
- Pure maths with tests: `Vehicles/BikeMath.cs` and `Tests/EditMode/BikeTests.cs` (lean, steering, grip, drive curve,
  top speed and acceleration, the roster, and the opening's distances on the default city through `PrologueLayout`).
- World: `Vehicles/TrafficManager.Bikes.cs` (safehouse bikes, start bike, police bikes, your bike, Bring my bike) and
  `Vehicles/BikeSpots.cs` (finds clear ground for a bike).
- Player: `Characters/PlayerController.Bike.cs` (hop on, shove a rider off).
- Story: `Gameplay/StoryMissions.cs` (the new M00, `PrologueLayout`, `SkipPrologue`), `Gameplay/MissionManager.cs`
  (line-by-line dialogue, skippable scenes, `ForbidVehicles`, the prologue skip request). No save fields were added.
