# Creature-vehicle fusion and the personal garage (Underheart and Bonds 04E)

This page explains the FUSION work stream: your own bike as a durable personal chassis, the fused forms (Ventwing,
Kilnback, Coilrunner), how fusing and separating work and stay safe, Jax's bay, the handling courses, and what is and is
not proven yet.

Status, plainly: the code compiles in all twelve configurations and the rules, the state machine, the form geometry,
the bay and the courses are tested offline (no Unity editor, no GPU here). Nothing on this page has been run in Unity.
How the forms look, how fusing feels, the handling, the camera and the frame rate must be checked in a native session
(the checklist is at the end).

## 1. What the player does

1. Your bike is yours. After the prologue (or from the first second of Free Roam) the game keeps a personal chassis
   record: the bronze Vanta Stilettra story bike, with its own number (chassis #1). Phone > Bring my bike always brings
   that chassis, with its paint, upgrades and the damage it had. Riding or stealing another bike never makes it yours.
2. In "Two Hearts, One Chassis" (`uh05_05_chassis`) you ride your bike into Jax Calder's garage and stop in the marked
   bay. With an Attuned companion (the quartz drakeling, Glint by default) the HUD panel says
   "Hold N to fuse with Glint". Hold N (or hold LB and the D-pad down on a pad) for 0.6 s.
3. The form grows out of the bike in under a second: wings unfold from the flanks, the drake head cowl and tail
   stabilizer appear, the companion is inside. The banner says VENTWING and tells you the ability.
4. Ride Jax's handling loop: U (LB + D-pad up) is a vent burst hop on the ground; hold U in the air to glide while the
   vents heat up. Brake hard at the turn. Ride back.
5. Hold N again to separate. Glint steps off beside the bike on verified ground. The coupler cools for 6 s.
6. After the quest you own the fusion license (granted by the reward ledger). Later companions (the Ashshell Warden,
   the Stormskimmer) calibrate their forms once in Jax's bay; after that they fuse anywhere with room.

## 2. Controls (on your bike, in the driver seat)

| Action | Keyboard | Pad |
|---|---|---|
| Fuse, or separate (hold 0.6 s) | Hold N | Hold LB, hold D-pad down |
| What would fusing need right now (tap) | Tap N | LB, tap D-pad down |
| Form ability (vent burst, brace ram, arc dash) | U | LB, press D-pad up |
| Ventwing glide (in the air) | Hold U | Hold LB, hold D-pad up |
| Jax's bay services (parked in the bay, not fused) | U | LB, press D-pad up |
| Close the bay services card | Backspace, U or Escape | B (Circle) |

On foot N and U belong to the companion (BONDS stream). These bindings are the ones the integrator already documented
in `Docs/CONTROLS.md`; no new binding was added. The hold is the command, so a tap never fuses by accident.

## 3. The personal chassis (Game.Garage, `Gameplay/Garage/`)

- One record per owned chassis (`ChassisRecord`): a uid that is never reused, the class (sport, cruiser, skiff),
  paint, health share, engine and armour upgrade levels, the applied look, the unlocked looks, the grip tune, the
  source, and whether it was wrecked. The live bike carries the uid in `Vehicle.ChassisUid`; every other vehicle has 0.
- Exactly one live body per record. Binding a new body clears the uid from any stale body, so two bodies of one
  chassis never exist.
- Where records come from: the story bike in the prologue and in Start After the Prologue (`AdoptStoryBike`), the Free
  Roam start or a loaded save past the prologue (`EnsurePersonal` at session start creates the bronze sport chassis),
  a reward (`GrantChassis(class, source)`, idempotent per class), or a paid registration at Jax's bay
  (`RegisterFromVehicle`, 750, refused for cars, the police bike, wrecks, job vehicles, a bike not stopped in the bay,
  a full garage of 6, or too little money).
- Where bodies come from: the bike parked beside you at session start, the safehouse bike when your chassis has no body
  in the world, and Bring my bike (it moves your free body to a curb spot, or builds a new body from the record).
- Health is sampled from the live body into the record twice a second, so a body cleaned up far away leaves an exact
  record behind. A wrecked chassis is marked; the next delivery comes back patched up to 40% (disclosed in the courier's
  line). Fusing and separating never write chassis health.
- Looks (trims) are cosmetic only: real geometry under the bike's lean pivot, rebuilt on apply (never stacked), no
  stats. Unlocking a look gives it to every owned chassis and every future one.
- The grip tune (`tune_grip`) is the Jax side-quest handling sidegrade: grip x1.08, top speed x0.97, switched in the bay.

## 4. The single state machine (`FusionMachine`, pure, `Vehicles/Fusion/`)

States: Separate, FusionRequested, Validating, TransitionIn, Fused, SeparationRequested, TransitionOut, Recovery. One
machine holds the only state. The legal moves are a fixed table (`FusionRules.IsLegal`); the machine throws on any
other move and the tests walk long random-ish sequences to prove it never makes one.

Fusing, in order:
1. Validate (section 5). A refusal changes nothing.
2. Build the new representation hidden (the form geometry on the bike) and run the physical checks against real
   colliders (section 6). On any failure the partial build is removed and nothing else has changed.
3. Snapshot the companion (`Game.Bonds.Snapshot`), then hide the free body through its own lifecycle
   (`Game.Bonds.SetFused(uid, true)`). If that throws, the form is removed, the body is restored and the snapshot put
   back.
4. TransitionIn (0.85 s): the chassis is held still, the form unfolds. Leaving the bike, a wreck, death or a reset during
   this phase rolls everything back with no cooldown spent.
5. Fused: the handling profile is applied, the Fused event fires, the form ability is live.

Separating: the runtime first finds a verified spot for the companion (section 6). With none, the request is refused
and you stay fused. Otherwise TransitionOut (0.6 s, held still), then `SetFused(uid, false)`, the body is placed on
the verified spot, `RestoreSnapshot` returns exactly what it had, the form is removed, the coupler cools 6 s, the
Separated event fires.

Forced recovery (always ends Separate, deterministic): the chassis wrecked or gone, you died, you got off or were
thrown off, a passenger climbed on, the Veil opened, more than 1.5 s in an Underheart space too tight for vehicles, a
scripted `ForceSeparate(reason)` (the Kiln Regent's resonance disruption), or an internal fault. The companion returns
at the best verified spot near the bike (or near you when the bike is gone; underground, the nearest rest point); with
no verified spot its own recall brings it to you. Cooldown 15 s, unless the fusion never finished unfolding (then 0).

Save and load: no fused frame is ever saved. New game, load and return to title drop the live fusion (always separate);
the garage saves the chassis records, the unlocks, the form meters and the fusion cooldown.

## 5. Validation (FusionRules.Validate, in this order, first failure wins)

garage and bonds loaded; you alive and in control; in a vehicle; in the driver seat; a hover bike (cars are not
supported); not the police bike; not a job vehicle; your personal chassis (else the garage's reason, for example
"This bike isn't yours. Phone > Bring my bike delivers your Vanta Stilettra, or register this one at Jax's bay.");
not wrecked; at least 15% health and not burning; upright; at least two hover rays on the ground; not already fused or
transitioning; coupler cooldown over; not in a story mission, a job that forbids vehicles, or another job (races, taxi
work; campaign quests and contracts are allowed); not inside the Veil; a companion with you; its archetype has a form;
rank Attuned or higher; not downed; `Game.Bonds.CanFuse` agrees; the fusion license, or the first-fusion exception
(the `uh05_05_chassis` quest is running, you are in Jax's bay, the companion is the drakeling); the form calibrated,
or this fusion calibrates it (stopped in Jax's bay); no passenger (refused, never deleted); slow enough (3 m/s; 0.8 m/s
for the first fusion or a calibration); underground only in a vehicle-capable cell (`Game.Underheart.VehicleSpaceAt`);
enough headroom when the Underheart reports it.

## 6. Physical checks (runtime, real colliders)

- Overhead: a ray up from the bike must stay clear for the form's height plus 0.45 m (at least 2.2 m).
- Swept transition box: the box the form occupies while it unfolds, sampled from the same pose function the renderer
  uses (every unfold stage from folded to parked), is checked with an overlap against the world, vehicles, vehicle
  shells and debris (the bike's own colliders excluded; the 0.3 m above the ground ignored, the form has no colliders).
- The returning companion: candidate spots beside, behind, ahead and on a ring; ground under it (slope under 45
  degrees), the same street or underground level, a capsule sized to the archetype clear, no Underheart heat hazard,
  and an open line from the bike.
- The fused bike's physics body is unchanged (no colliders added), so a form can never wedge the bike, push through
  walls or pass mission geometry. Abilities are velocity changes the world still stops.

## 7. No refill, and every multiplier written down

- Form meters (heat, cooldown, charges, recharge, timed effect) change only with time and use, fused or not. Fusing
  and separating never touch them, and they are saved, so a reload is not a refill either.
- The companion comes back with exactly the snapshot taken when it fused (health, assist charge, assist cooldown).
- Player health, armour, the ward, ammo, nitro charges and claim entitlements are never touched by fusion.
- Explicit damage multipliers (`FusionRules`):

| Form | Incoming chassis damage | While bracing | Rider damage | Rider, braced, hit from the front |
|---|---|---|---|---|
| Ventwing | x1.0 | n/a | x1.0 | n/a |
| Kilnback | x0.7 | x0.4 | x0.85 | x0.6 |
| Coilrunner | x1.1 | n/a | x1.0 | n/a |

The chassis multiplier is applied in `Vehicle.ApplyDamage` (one listed hook line), the rider multiplier through
`PlayerDamageHooks` (key `fusion_rider_04e`, order 30). No form ever makes the rider invulnerable.

## 8. The forms (real geometry under `Parts.LeanRoot`)

All parts are procedural meshes (`FusionFormGeometry`, pure blueprints; `FusionFormView` builds renderers, no
colliders), parented under the bike's lean pivot, so they bank and bob with the bike and leave the rider's Seat,
HandleL, HandleR, FootL and FootR anchors untouched. Tests prove no vertex of any form, in any pose, enters the rider's
torso, arm or leg space, and nothing comes within 0.15 m of the road.

- Ventwing (drakeling): crystal ribs on the fairing ahead of the knees; a drake head cowl over the nose (skull, snout,
  jaw, brow ridges, glowing eye slits, swept crystal horns ending ahead of the handlebars, a crest) with a pulsing crystal
  emitter at the snout tip; two three-panel articulated wings hinged behind the rider's legs, folded flat against the
  flanks at rest, spreading with speed (about 3 m span) and lifting while gliding; a tail stabilizer (a vertebral boom on
  a strut, crystal spikes, a steering fin, two pitching vanes). Glide trails leave the wing tips while gliding.
- Kilnback (Ashshell Warden): layered mineral shell plates (two caps over the fairing, a stepped hump behind the rider,
  lifted over the courier top box on the skiff) with glowing ember seams; low side skirts that keep the thruster pods
  clear; a forward brace: a blunt prow with an ember chevron on pistons, held by two brace arms. Bracing pushes the prow
  forward and the seams flare.
- Coilrunner (Stormskimmer): a row of dorsal crystal spines on the tail that stand up when the dash is ready; two swept,
  drooping side fins that follow the steering; a coil rail with ring coils on the tail; low conduits with glowing nodes
  along both flanks to a ring around the nose; two forward antennae and an emitter orb. The discharge arc runs along that
  path and out to each struck target.

Each form is recognizable by outline alone (wings and horns; plated hump and prow; spines, fins and antennae), and a
test checks the structures and the widths differ. Low variants (Low graphics preset) drop facets and repeated details
(fewer ribs, plates, spines and coils, no finger struts) but keep every outline: a test requires under 75% of the
vertices and at least 90% of each extent. Budgets: Ventwing 3348 vertices (Low 1628), Kilnback 2192 (1264), Coilrunner
2948 (1700). Meshes are built once per form, class and quality and shared; materials are one cached palette per form.
The form hum loop and all effects are children of the form, so they stop when it ends.

## 9. Handling and abilities (`FusionRules.Handling`, `FusionRules.Ability`, applied in `Vehicle.BikeStep`)

| Form | Drive | Top speed | Grip | Brake | Steer | Other |
|---|---|---|---|---|---|---|
| Ventwing Updraft | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 | glide gravity 0.32, sink at most 3.2 m/s, air yaw x1.6 |
| Ventwing Vent Brake | 1.0 | 1.0 | 1.0 | 1.25 | 1.0 | glide gravity 0.45, sink 4.5 m/s, air brake 6 m/s2 |
| Kilnback | 0.85 | 0.88 | 1.08 | 1.05 | 0.85 | armoured (section 7) |
| Coilrunner | 1.0 | 1.0 | 1.05 | 1.35 | 0.92 | strong brakes, calmer steering |

No form raises top speed (tested). The Ventwing profile comes from the campaign choice `fusion_profile` (updraft or
ventbrake) until you pick one in Jax's bay; it is saved in `GarageSave.Profile`.

| Ability | Cost and limit | Effect |
|---|---|---|
| Vent burst (Ventwing, U on the ground) | heat +0.34, cooldown 2.2 s, needs 4 m headroom | +5.2 m/s up and +3 m/s forward (never past top speed); scorches hostiles behind for 18 fire damage |
| Glide (Ventwing, hold U in the air) | heat +0.16 per second; at full heat the vents overheat and nothing fires until they cool to 35% | carries part of gravity, caps the sink; hot vents (over 75%) cut drive by 15% |
| Brace ram (Kilnback, U) | heat +0.3, cooldown 6 s, lasts 1.2 s | +3.5 m/s forward (never past top speed); harder shell; each hostile in the box ahead is struck once: 55 damage, shoved, creatures staggered 0.7 s (bosses 0.25 s) |
| Arc dash (Coilrunner, U) | 2 charges, one back every 5 s, 0.8 s between uses | +6.5 m/s forward, never past 1.12 x top speed; 0.25 s later a discharge strikes up to 6 hostiles in line of sight within 7 m: 32 damage, stagger 0.5 s (bosses 0.2 s) |

Hits only ever reach hostile NPCs and targetable creatures (never civilians, police that are not after you, companions
or you), with `Source` = the player and `Origin` = `FusedVehicle`: one attribution, one payout.

## 10. The compatibility matrix (honest)

| | Sport (Vanta Stilettra) | Cruiser (Anvil Duskhauler) | Skiff (Kestrel Skiff) |
|---|---|---|---|
| Ventwing (quartz drakeling) | built, offline-tested, primary target | built, offline-tested | built, offline-tested |
| Kilnback (Ashshell Warden) | built, offline-tested | built, offline-tested | built, offline-tested |
| Coilrunner (Stormskimmer) | built, offline-tested | built, offline-tested | built, offline-tested |

"Built, offline-tested" means: one parametric builder per form fitted to each bike's real proportions (checked against
the bike builder's seat point and dimensions), geometry tested for envelope, rider clearance, road clearance and the Low
silhouette, handling and abilities tested as maths. None of the nine has been seen or ridden in Unity yet. Ventwing on
the sport bike is the combination the first route uses and the one to check first.
Not supported, with a reason on screen: every car class ("Cars are not supported: fusion couples with a hover bike
chassis"), the police bike ("never a personal chassis"), and any other bike model. Other named characters' chassis are
not implemented (the data model has an owner-free record per chassis, but no ally pairing exists).

## 11. Jax's bay (`Game.Fusion.TryBay`)

- If the garage venue's layout has the `bay` anchor (the VENUES stream appends it), the bay is that anchor, radius 2.8 m.
- Otherwise the bay is computed from the garage venue's own frame: local (0, 0.012, 6), the middle of the marked
  vehicle bay (stripes at x -2.8 and 2.8, z 0.25 to 13.75), clear of the repair point at z 10.5. In this sandbox the
  anchor is missing, so this computed bay is what runs (the STATUS page says which). A test checks it lies inside the
  layout's `vehicle_bay` keepout and inside the garage building on the default city.
- If the walk-in garage did not load at all, the bay falls back to the lot 5 m outside the Jax Calder Customs door
  (radius 4 m), so the first-fusion quest is never blocked.
- "In the bay" means within the radius (flat) and within 2.5 m height; "parked" adds a speed under 1 m/s.

Bay services (U while parked in the bay, not fused): switch the Ventwing handling profile (free, reversible); cycle the
look of your chassis (if any are unlocked); switch the grip tune (if unlocked); choose which owned chassis is your
personal one (Bring my bike brings it next); register the bike you are on (if it is not yours).

## 12. The three courses (`Game.Fusion.TryCourse`)

| Id | Where | Ability uses | Notes |
|---|---|---|---|
| `jax_handling` | out from the garage door about 200 m by road and back | 1 | ability checkpoint 1, hard brake at the turn |
| `jax_profile_two` | to the door's right about 230 m by road and back | 2 | for the second-profile side quest |
| `canal_crossing` | Underheart anchors canals.portal_bottom, canals.transit, canals.crossing, canals.exit_gardens | 1 | resolves once the Underheart is built (not in this sandbox) |

Road courses follow the real road graph (`CityLayout.FindRoute`), sampled every 45 m (at most 9 checkpoints, radius
7 m), end back at the garage door, and mark `BrakeAt` (the farthest point) and `AbilityAt`. The quest Course step counts
`AbilityUses`, which resets at every fusion. A test resolves both Jax loops on the default city.

## 13. Interfaces for the other streams

- `Game.Garage`: `Personal`, `Owned`, `Find(uid)`, `IsPersonal(v)`, `RecordOf(v)`, `WhyNotPersonal(v)` (for the Park
  step's refusal), `BodyOf(uid)`, `PersonalBody`, `EnsurePersonal()`, `AdoptStoryBike(v)`, `RegisterFromVehicle(v, out
  reason)`, `GrantChassis(class, source)`, `SetPersonal(uid)`, `FusionUnlocked`, `UnlockFusion(source)` (also calibrates
  the Ventwing), `FormUnlocked(form)`, `UnlockForm(form, source)`, `UnlockTrim(id, source)`, `ApplyTrim(id)`,
  `SetTune(id)`, `Profile`, `ActiveProfile`, `SetProfile(id)`, `SpawnBody(record, ground, yaw, trafficParked)`.
- Reward ids for BUILDS: looks `trim_quartz_crown`, `trim_ember_fins`, `trim_frost_plates`, `trim_storm_rails`
  (`ChassisTrims.All`); the handling sidegrade is `UnlockTrim("tune_grip")` (the ids `uh_handling_sidegrade` and
  `handling_sidegrade` are accepted as the same tune); the second vehicle class is `GrantChassis("cruiser", ...)`;
  the license is `UnlockFusion(...)`.
- `Game.Fusion`: `State`, `IsFused`, `ActiveForm`, `AbilityUses`, `CanFuse(out reason)`, `RequestFuse(out reason)`,
  `RequestSeparate(out reason)`, `ForceSeparate(reason)` (BOSSES: the Kiln Regent's opening), `TryCourse(id, out
  course)`, `TryBay(out center, out radius)`, `InBay(v)`, `ParkedInBay(v)`, `BaySource`, `LastReason`, `CooldownLeft`,
  `Chassis`, `Gliding`, `MetersOf(form)`, events `Fused(form)`, `Separated`, `AbilityUsed(form)` (the burst, ram or
  dash; the glide is not counted).
- From BONDS this stream needs: `Active`, `RankOf`, `CanFuse(uid, out reason)`, `SetFused(uid, fused)` (hide or restore
  the body through its own lifecycle; while fused the companion deals no damage), `Snapshot(uid)`,
  `RestoreSnapshot(s)`, `ActiveBody` (after `SetFused(uid, false)` the fusion system places that transform on the
  verified spot; BONDS should keep it there rather than snap it elsewhere), and `CompanionRecord.State == Downed`.
  In this sandbox Bonds is the foundation stub, so fusion correctly refuses with "Bonds are not implemented in this
  build" until the merge.
- From CAMPAIGN: the first-fusion exception keys on `Game.Quests.ActiveId == "uh05_05_chassis"`; the Fuse, Course and
  Park steps read the members above; the profile choice is the `fusion_profile` choice key.

## 14. Save data (append-only)

`SaveData.Garage` (`GarageSave`): Version, NextUid, PersonalUid, Chassis, FusionUnlocked, Forms, Profile (foundation),
plus UnlockedTrims, Meters (`FormMeterSave`: Form, Heat, Cooldown, Charges, Recharge, Overheated) and FusionCooldown.
`ChassisRecord` appended Wrecked, ArmorUpgrade and Tune. Field freeze tests cover GarageSave, ChassisRecord and
FormMeterSave. Loading normalizes the block: a missing block is empty (nothing granted), broken lists are rebuilt,
duplicate or invalid uids fixed, health and paint clamped (a broken health value becomes full, a wreck stays a wreck),
unknown forms and profiles dropped, locked looks and tunes cleared, meters clamped to their limits, the personal uid
pointed at an owned chassis, the uid counter moved past every uid in use.

## 15. HUD, STATUS and debug

- A panel at the lower right (left of the speedometer) while you ride your own bike with a companion, while fused, or in
  Jax's bay: the chassis number and companion, "Hold N to fuse with Glint" when everything passes (or the one reason it
  does not), the transition bar, the form's heat bar or charge pips and cooldown, the ability hint, a gold hold-progress
  bar along its bottom, and short result toasts above it. It stays hidden on bikes that are not yours.
- STATUS rows: "garage" (personal chassis, stored or in the world, forms calibrated, owned count) and "fusion" (state,
  what is missing, where the bay came from).
- F2 > Fusion (order 64): live state, the validation result, the bay source and distance, every form's meters, and
  debug buttons (grant the license, calibrate every form, spawn your chassis here, force a safe separation, make the
  companion Attuned through Bonds, resolve the courses).

## 16. Files

Added: `Gameplay/Garage/GarageRules.cs`, `Gameplay/Garage/PersonalGarage.Body.cs`, `Vehicles/Fusion/FusionRules.cs`,
`FusionMachine.cs`, `FusionCourses.cs`, `FusionFormGeometry.cs` (+ `.Ventwing.cs`, `.Kilnback.cs`, `.Coilrunner.cs`,
`.Trims.cs`), `FusionArt.cs`, `FusionFormView.cs`, `ChassisTrimArt.cs`, `FusionSystem.Requests.cs`,
`FusionSystem.Abilities.cs`, `FusionSystem.Bay.cs`, `FusionSystem.Ui.cs`, `Tests/EditMode/Fusion04ETests.cs`, this page.
Replaced bodies (signatures kept): `Gameplay/Garage/PersonalGarage.cs`, `Vehicles/Fusion/FusionSystem.cs`.
Changed: `Vehicles/Vehicle.Bike.cs` (handling hooks), `Vehicles/TrafficManager.Bikes.cs` (personal chassis delivery),
`UI/Phone.cs` (one comment line).
Hooks outside ownership: `Vehicles/Vehicle.cs` (one line in `ApplyDamage`: the fused damage multiplier) and
`Gameplay/StoryMissions.cs` (one line each in `SkipPrologue` and the prologue mission to adopt the story bike).

## 17. Offline proof (what was actually run, without Unity)

- Twelve compile configurations: `/home/claude/tools/check_urp.sh /home/claude/agents/04e/fusion
  /home/claude/agents/04e/out_fusion/urp` gave 0 errors and 63 warnings, the same 63 as the foundation (all CS0618 in
  `QA/LivingCity04CQa.cs` and `QA/LivingTerritories04BQa.cs`).
- EditMode tests: `cd /home/claude/agents/04e/fusion && /home/claude/tools/check_tests.sh /home/claude/agents/04e/fusion
  /home/claude/agents/04e/out_fusion/tests` gave 787 passed, 25 failed, 47 skipped. The foundation was 748 passed, 25
  failed, 47 skipped; the 25 failures are the identical set (native `Resources.Load` calls offline), and the 39 new
  passes are `Fusion04ETests`.
- `Fusion04ETests` covers: every validation refusal and its message; the first-fusion exception (quest, bay, drakeling,
  parked); calibration only in the bay; separation rules; the transition table; fuse and separate restoring the same
  companion exactly; a failed build, a thrown build and a failed companion hide all rolling back at no cost; aborts and
  recoveries (wreck, death, dismount, loss) always ending separate; refused separations staying fused; every state the
  machine enters being legal; toggling never refilling heat, cooldowns, charges or the companion; meters improving only
  with time; ability impulses never passing their cap; handling never raising top speed; the explicit damage table;
  the 3 x 3 compatibility matrix; garage normalization of empty, old and broken blocks (meters included); uid reuse;
  registration refusals; delivery health; trims never stacking or touching handling; every form on every class at both
  qualities fitting its envelopes and never entering the rider's space; Low keeping each outline; the three structures
  being distinct; the frame matching the real bike models; trims clear of the rider; the computed bay inside the
  garage's marked bay and building on the default city; both Jax road loops resolving on the default city; course
  sampling; and the field freezes for GarageSave, ChassisRecord and FormMeterSave.
- Unity meta files: `python3 /home/claude/cms/tools/make_metas.py /home/claude/agents/04e/fusion` (32-character GUIDs).
- Every changed file scanned: no U+2010 to U+2015 dash characters, no non-ASCII characters.

## 18. What a native Unity session must check

1. Free Roam: the bike beside the Veyra Lofts door is bronze; get on, Phone > Bring my bike says "chassis #1". Steal a
   traffic bike, ride it away, call Bring my bike: the bronze bike comes, not the stolen one.
2. F2 > Fusion > "Debug: Attuned companion" (needs the merged Bonds), "Debug: grant fusion license", sit on your bike in
   open street: the panel shows "Hold N to fuse with Glint". Hold N, then hold LB + D-pad down on a pad.
3. Watch the Ventwing appear: wings folded flat along the flanks, unfolding to the parked spread; head cowl over the nose
   (not over the handlebars or the rider's hands); tail boom and fin behind; nothing through the rider's legs, arms or
   head, also while leaning hard; the emitter pulse; the hum.
4. Ride: wings spread with speed; vent burst hop on flat ground; hold U in the air: wings lift, glide trails, slower
   sink, heat bar rising, overheat stops the glide; landing and braking with Updraft and with Vent Brake (bay card).
5. Separate in the open and next to a wall: Glint appears on the open side, never inside a wall or under the road.
   Try it boxed in by cars: it should refuse with the reason and stay fused.
6. Crash hard while fused, get blown up, fall off at speed, die: every time the form vanishes and Glint reappears near you.
7. Jax's garage: drive into the bay, stop; the panel shows JAX'S BAY; U opens the bay card (mouse, keyboard and pad
   focus, B closes). Fusing there in the quest without the license. Check the computed bay sits in the marked stripes.
8. Kilnback and Coilrunner on all three bikes (F2 calibrate every form), High and Low graphics: silhouettes, the hump on
   the skiff's top box, the prow on the brace, spines flaring, fins steering, the discharge arc to enemies.
9. Save while fused, load: you are separate, the bike and companion are there, the cooldown and meters are as saved.
10. Performance: fuse and separate twenty times; watch for growing object counts (meshes are shared and cached; form
    objects are destroyed on separation).

## 19. Honestly unfinished

- Nothing here has been run in Unity: looks, feel, handling numbers and camera framing are unverified.
- The companion's placement after separation depends on BONDS keeping its body where fusion put it.
- The quest steps (Fuse, Course, Park) and the course markers are CAMPAIGN's; this stream only supplies the data.
- The bay anchor from VENUES is not in this sandbox; the computed bay is used and labelled.
- Combat fusion while moving is allowed only up to 3 m/s by design; faster fusing is future work once collision and
  cancellation have been tested natively.
- No ally or other character chassis, no car or skiff (water) chassis: labelled unsupported.
