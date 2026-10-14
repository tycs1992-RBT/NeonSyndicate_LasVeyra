# Visual Renaissance 01C: player vehicle handling

Implemented in the existing vehicle systems. This is a bounded player handling adjustment, not a replacement driving system. No Unity session, Rigidbody simulation, controller session or desktop build was run here. Improved driving feel remains an expectation until the play checks below pass.

## What the source audit found

1. Car steering lock decreased immediately with speed. A normal car's maximum lock fell from 34 degrees at rest to 26.67 degrees at 5 m/s. Its steering assist then requested an uncapped geometric yaw rate, even when the tire friction circle could not produce that turn. More steering alone could therefore add rotation without enough grip to redirect travel.
2. Bikes already capped yaw by grip, but sideways force only opposed existing lateral velocity. There was no force supporting the current turn, so rotation could get ahead of the direction of travel. The yaw request also used the full grip budget, leaving little correction margin at full lock.
3. Bike visual bank came directly from measured angular velocity, including curb and impact spikes. The mesh could bank up to 38 degrees. Physics roll recovery also applied a fixed 35% rotational correction each physics tick when roll passed about 30 degrees.
4. `DirectSteer` is shared by analog players and AI. It is not a valid player-ownership test. The new handling branches use `PlayerDriving`.

## Implemented changes and exact limits

| Area | Player behavior in 01C | Existing non-player behavior |
|---|---|---|
| Car steering lock | `min(45, baseLock * 1.12) / (1 + max(0, abs(speed) - 5) * 0.08)` | Original formula retained |
| Bike steering lock | `min(45, baseLock * 1.20) / (1 + max(0, abs(speed) - 4) * 0.055)` | Original formula retained |
| Steering response | 150 deg/s car, 180 deg/s bike; fades to 62% between 8 and 35 m/s; return or reversal multiplier 1.4; analog multiplier 1.2 | Original rates retained |
| Car assisted turn | Desired yaw spends at most 88% of calculated lateral grip; tire damage, weather, terrain, grip skill and temporary grip bonus are included | Original AI assist retained |
| Bike assisted turn | Desired yaw spends at most 82% of available lateral grip, which still scales with ground contacts | Original bike yaw retained |
| Yaw correction | Exponential response with gain 6/s car and 8/s bike; acceleration capped at 3.5 and 6 rad/s squared respectively; scaled by ground contacts and motion from 0.2 to 2 m/s | Original AI torque retained |
| Assistance eligibility | At least two suspension contacts; car assistance also requires body up.y above 0.35; no stationary pivoting | Original AI checks retained |
| Bike sideways support | Supports measured turn acceleration, capped at 82% of grip, plus slide recovery of 6/s; total force remains inside the existing grip limit | Original lateral damping retained |
| Bike visible bank | At most 30 degrees, damped at 8/s entering and 12/s recovering; 90 deg/s entering and 120 deg/s recovering | Original 38 degree bound and smoothing retained |
| Bank near a stop | Zero through 1 m/s, fades to full amount at 5 m/s; no reverse bank; raw impact yaw is limited by available grip | Original lean formula retained |
| Bike physics roll | Restoring spring 100/s squared, damping 20/s, acceleration bounded to 32 rad/s squared | Original spring 90, damping 18 retained |
| Emergency roll correction | Begins around 50 degrees; correction fraction is `1 - exp(-7 * fixedDt)` (about 13.1% at 50 Hz), instead of the earlier 35% tick correction | Original approximately 30 degree threshold retained |

The existing handbrake still releases grip for a deliberate drift. Car yaw assistance is disabled while the handbrake is held. Bikes keep their earlier drift torque and lateral damping above the existing 6 m/s drift threshold; the new cornering support is bypassed there. Visual bank smoothing remains active during a player drift.

The physical roll spring sign was checked and preserved. Positive steering and yaw turn right, positive rider lean banks right, and the rendered bike uses negative local Z rotation for that bank.

### Steering comparison, source calculation

These are maximum steering angles, not measured corner radii or gameplay outcomes.

| Speed | Normal car before | Normal car 01C | Bike before | Bike 01C |
|---|---:|---:|---:|---:|
| 0 m/s | 34.00 deg | 38.08 deg | 32.00 deg | 38.40 deg |
| 5 m/s | 26.67 deg | 38.08 deg | 26.12 deg | 36.40 deg |
| 15 m/s | 18.63 deg | 21.16 deg | 19.10 deg | 23.93 deg |
| 30 m/s | 12.83 deg | 12.69 deg | 13.62 deg | 15.80 deg |
| 40 m/s | 10.63 deg | 10.02 deg | 11.43 deg | 12.89 deg |

Higher bike steering lock does not grant unlimited corner speed because its desired yaw remains grip-capped.

## Files and scope

- `Vehicle.cs`: player lock and steering smoothing; grip-bounded car yaw assistance.
- `Vehicle.Bike.cs`: player steering, bounded yaw, lateral turn support, visual lean and roll recovery.
- `BikeMath.cs`: additive engine-free player helper methods. Existing traffic helpers and generic bike helpers retain their previous definitions.
- `PlayerVehicleHandlingTests.cs`: ten new EditMode test methods for mathematical handling contracts.
- `VEHICLE_NUMERICAL_CHECKS.json`: offline numerical evidence and steering values.

`VehicleData.cs` and `TrafficDriver.cs` were audited but did not require handling changes. Vehicle indexes, roster, acceleration, top speeds, drive force, boost, damage, mission flags, save fields and input mappings were preserved. Normal player entry already disables the traffic controller. Non-player drivers retain their previous steering and tire/yaw behavior, including when the player is a passenger. No new per-frame allocation, runtime package or static cache was added.

## Verification performed

- Parsed the three edited runtime C# files and new test file with the C# tree-sitter grammar. This is a syntax check, not Unity or C# compilation.
- Evaluated the new scalar expressions from the actual `BikeMath.cs` syntax tree with a small Python mathematical harness. The harness uses double precision and selected `Mathf` equivalents; it does not reproduce Rigidbody forces or a C# runtime.
- Checked lock symmetry and monotonicity, yaw grip budgets across speeds and surfaces, reverse yaw, steering reversal, yaw convergence at 25/50/100 Hz, slide recovery during a bounded turn, upright lean recovery, impact bank limits and roll restoring direction.
- Compared the pre-existing `BikeMath` methods with the previous project and checked the player-only integration branches.

In the mathematical scenario beginning with 3 m/s lateral slip at 12 m/s forward speed and 0.5 rad/s yaw, lateral slip converged below 0.00002 m/s after two seconds. The 30 degree bank returned below 0.001 degree after one second at each tested step size. These figures describe the isolated equations, not an observed game vehicle.

The ten new NUnit tests are supplied but were not executed because Unity and a C# compiler were unavailable.

## Required Unity checks

1. Import and compile in the project's Unity version. Run `PlayerVehicleHandlingTests` and the existing `BikeTests`, followed by the normal test suite and desktop build gates. Cover editor and Windows player, all three input configurations, and both supported rendering configurations.
2. Use keyboard and gamepad to make repeated 90 degree turns at about 5, 10 and 15 m/s in a normal car, sports car, heavy vehicle and each bike. Check turn-in, steering reversal, exit from corners and braking while turning.
3. Drive a slalom at low speed, then at 30 to 40 m/s. Verify fast steering does not introduce oscillation, unexplained spinning or a steering lock change after changing input device.
4. Reverse through a corner, stop with steering held, and park. Confirm reverse turns the correct way, no pivoting appears at rest, and bike bank settles upright.
5. Hold and release handbrake through a turn. Intentional drift should remain available and recovery should feel controllable. Check nitro, vehicle grip skills, temporary grip bonuses and popped tires.
6. Repeat representative turns on wet road, sand, snow and ice. Lower grip must still matter. The new assistance must not appear to glue the vehicle to those surfaces.
7. Ride over curbs, shallow slopes and uneven terrain; land a jump with steering released and with steering held. Inspect visual bank, seat/hand alignment, camera shake, roll recovery and accidental rider ejection.
8. Enter and exit a moving vehicle, take over an AI-driven vehicle, ride as a passenger, then resume driving. Check missions using vehicles and the story bike. Observe traffic intersections, pursuits and AI bikes for regressions.

## Still proposed

No custom per-model tuning, assist-setting UI, new tire simulation, vehicle camera rewrite or replacement vehicle rig is included. Consider individual heavy-vehicle lock tuning and an optional handling preference only after the Unity play checks provide evidence. No claim is made that the current tuning has already solved the reported feel problem.
