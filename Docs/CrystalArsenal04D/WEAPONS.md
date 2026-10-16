# Crystal Arsenal 04D: the six crystal weapons

Status: source written and compile-checked offline in all twelve configurations, pure rules tested offline.
**Native Unity: NOT RUN.** Nothing here has been fired in the Unity editor yet; how the weapons look, sound and feel,
and whether hits land where the crosshair says, must be checked by Tyler in Unity (steps at the end).

## What changed, in plain words

A crystal focus is still a normal loot gun (same ARSENAL, same four slots, same backpack, same Forge). What is new is
what happens when you pull the trigger with one in your hands. Before 04D a crystal caster fired an invisible instant
bullet. Now each of the six foci has its own behaviour, and the old instant-bullet code never runs for them:

| Focus | What the trigger does |
|---|---|
| Shardwake | Fires a visible crystal shard that flies at 95 m/s (you can watch it cross a courtyard). Keep holding after a shot and a fan of three weaker shards charges; let go to fire the fan. |
| Prism Thread | Hold fire for a real continuous beam. It stops at the first wall, heats up while it is on, and grows a little stronger (up to 1.25x) when you aim and keep it on one target. |
| Gyre Crescent | Throws a spinning disc that can bank off walls twice, then flies back to your hand. Press fire again to call it back early. While aiming, dotted dots show where it will bank. |
| Sunderheart | Launches a slow heavy orb. Where it lands it briefly pulls small enemies in, then bursts. Keep holding after a launch to concentrate the next orb (smaller, stronger burst). |
| Mirage Choir | Hold aim over enemies to lock up to three, then fire five shards that steer toward the locks. Shards still hit walls; a lock that goes behind cover is lost. |
| Mirrorwake | Hold fire to charge, let go for an instant precision pulse. A pulse on a bare wall plants a mirror focus; the next pulse aimed at that focus bends off it toward an enemy. |

## Files (all in `Assets/NeonSyndicate/Scripts/Runtime/Combat`)

| File | What it holds |
|---|---|
| `WeaponController.Crystal.cs` | The dispatch: exclusive trigger ownership, per-frame update, real-release detection, cancellation, vents, heat mirroring into `Heat` and `Overheated`, legal muzzle check, noise report, rig feed. |
| `WeaponController.CrystalShardwake.cs` | Shardwake shot and charged fan. |
| `WeaponController.CrystalPrism.cs` | Prism Thread beam (sweep, query, tick loop, ramp, loop audio, beam visual). |
| `WeaponController.CrystalGyre.cs` | Gyre Crescent throw, early recall, catch recovery, bank preview. |
| `WeaponController.CrystalSunder.cs` | Sunderheart launch and concentrated orb. |
| `WeaponController.CrystalChoir.cs` | Mirage Choir lock acquisition (bounded 10 Hz scan) and the guided volley. |
| `WeaponController.CrystalMirror.cs` | Mirrorwake charge, direct pulse, focus planting, two-leg redirect. |
| `WeaponController.CrystalHit.cs` | The one crystal damage path (like `LootHit`) and impact effects and sounds. |
| `WeaponController.CrystalHud.cs` | The crosshair overlay (`HudOverlays` key `crystal_weapons`). |
| `CrystalShots.cs`, `.Types.cs`, `.Query.cs`, `.Sim.cs`, `.Debug.cs` | The projectile owner: pooling, admission cap, the shared swept query, per-kind simulation, F2 page. |
| `CrystalHeat.cs` | Pure heat book keyed by item uid. |
| `CrystalBeamIntegrator.cs` | Pure beam time integrator. |
| `CrystalShotRules.cs` | Pure rules: saturation, admission, disc, locks, volley budget, focus, redirect, pull. |
| `Tests/EditMode/CrystalWeapons04DTests.cs` | 28 offline tests of the three pure classes. |

## Trigger ownership and cancelling

- `TryFire` asks `CrystalTrigger` first. For a focus it always answers "taken", so the old hitscan volley, the legacy
  charge shot and burst code can never also fire. NPCs never carry foci; if one somehow did, it would do nothing.
- `PlayerController` only calls `TryFire` while fire is held (or a click is buffered for 0.3 s) and gameplay owns the
  input. So a frame without a held call can mean two things. The controller tells them apart in `LateUpdate`:
  - something blocks gameplay (menu, phone, map or weapon wheel, pause, game state not Playing, the window lost focus,
    death, ragdoll, a vehicle, controls disabled): every charge, the beam and the locks are **cancelled, nothing fires**;
  - the physical button is still down but the call was skipped: a dodge, guard, swing, climb or getting into a car
    cancels; a body that is still turning to face the aim only pauses the charge;
  - the physical button is up and nothing blocks: this is a **real release** and a full charge fires.
- Swapping weapons (or equipping another gun into the slot in hand) and `CancelActions` (knockdowns, vehicles) cancel
  too. Losing window focus calls `OnApplicationFocus(false)` and cancels at once.
- New game, load and title call `CrystalShots.ClearAll`, which ends every projectile and resets every controller's
  charges, beam, locks, heat book and Mirrorwake focus.

## Heat (per item, one cooling loop)

- Heat belongs to the focus's inventory **uid**, not to the slot (`CrystalHeat`). Two foci never share heat, and
  swapping away from an overheated focus and straight back finds it still venting.
- Every focus, in hand or in the backpack, cools on one gameplay clock inside `CrystalUpdate` (paused time does not
  count): after 0.35 s without new heat it drains 0.25 per second. The legacy caster cooling is skipped for foci, so
  there is only one cooling loop.
- Reaching 1.0 overheats: a forced vent of the profile's vent time (1.6 to 2.2 s) during which it cannot fire.
- Reload (R, tap D-pad down) starts a manual vent: shorter the cooler the focus (vent time x heat, at least 40 percent),
  draining from the current heat; it stops the beam and any charge.
- A Resonance Burst still vents the focus (the old synergy), but at most once every 4 s per uid, so a beam cannot burst,
  vent and burst again forever. The legacy caster's own heat is put aside while a focus is in hand and restored after.
- The focus's heat is copied into the controller's `Heat`, `Overheated` and `Charge` every frame, so the existing HUD
  heat bar, charge bar and "Venting..." text work unchanged.

## Projectiles (`CrystalShots`)

- Bodies are the art pass's `CrystalFocusModels.CreateShot` visuals, pooled per kind (64 kept per kind; that is a
  retention limit, not the active cap). A missing or failing art body falls back to a plain glowing sphere so a shot is
  never invisible.
- They move on the gameplay clock (frozen while paused, slowed in slow motion) in substeps of at most 0.05 s. A frame
  longer than 0.2 s simulates only 0.2 s, so a hitch moves bodies less, never faster.
- Every substep sweeps a sphere from the previous to the next position (`CrystalShots.Query`): triggers ignored, the
  shooter and the shooter's own vehicle excluded (and the player's own shield face), hits sorted nearest first, a body
  with several colliders counted once by its damage receiver, corpses passed through, and the nearest blocker ending
  the list so nothing behind a wall is ever hit.
- **Saturation fails closed:** if the hit buffer (16) comes back full, a separate single sweep must prove the nearest
  blocker; if it cannot, the shot is quenched. The beam does the same and stops at the muzzle.
- **Legal muzzle:** before any shot or beam frame the line from the chest to the muzzle must be clear and the muzzle
  must not sit inside geometry. Otherwise the shot is refused with the denied sound and a crossed "BLOCKED" ring at the
  crosshair; it never fires from the far side of a wall.
- **Admission:** at most 64 live bodies (32 on the Low graphics preset). A shot event (one shard, a three-shard fan, a
  five-shard volley) is admitted whole or refused whole, with the denied sound and "TOO MANY SHOTS". Damage is never
  silently reduced.
- Every body has a shot id, its event's weapon snapshot (weapon uid, one critical roll, the volley budget), the owner,
  a lifetime, a victim set and an end reason (counted on the F2 page). Release resets every field, clears trails and
  particles and restores the art body's scale.

## Damage

- One damage path (`CrystalStrike`), built like `LootHit`: `DamageInfo` through `IDamageable` (so armour plates,
  creature parts, damage numbers and Resonance Bursts apply), `WeaponId` of the firing slot, the gun's element,
  `ProgressionHooks.GunMultiplier`, `DamageScale` and `LootCombat.ElementVsTarget`. Crystal shots damage any
  `IDamageable` (practice targets included).
- Direct weapon hits (shards, fan, disc, beam ticks, pulses, redirect) are `DamageType.Bullet`; Sunderheart's rupture
  centre and splash are `DamageType.Explosion` (so Sunderheart does not set off Resonance Bursts or charge the crystal
  bar, as the contract asks).
- One critical roll and one skill multiplier roll per shot event (a fan, a volley, a disc throw, a pulse). One element
  proc roll per shot event. The beam rolls its element proc and renews its skill roll once per 0.5 s of contact.
- Mirage Choir volley budget per actor: the first three shards of one volley on the same actor deal full damage, the
  fourth and fifth half (so five shards on one target deal four shards' worth).
- Hit markers, the "Critical" text, `Fx.Impact` chips and `CrystalSounds.PlayImpact` (stone, armour, weak point,
  crystal, flesh, air), limited to 8 crystal impacts a frame and one impact sound every 0.06 s.
- Noise is reported like `FireGun`: `Threats.Report` and `Game.Wanted.OnPlayerGunfire` at most every 0.3 s. Inside the
  practice lane (`CrystalPractice.SuppressNoise`) the wanted report is skipped so practice is crime free.

## The six behaviours in detail

Numbers in B are the profile's multiples of the reference damage (18 at level 1). Profile values come from
`CrystalFocusDb` and were not changed.

**Shardwake.** Press: one shard, 1.0 B, 95 m/s, 55 m, 0.09 heat, 0.25 s cycle, swept radius 0.06 m. Holding on after
the shot charges 0.8 s (chime when full); a real release fires three shards at -6, 0 and +6 degrees, 0.55 B each,
0.2 heat, one shot event. From a vehicle (drive-by) single shots work, no charge.

**Prism Thread.** Hold: the beam turns toward the camera's aim point at 240 degrees a second (90 while aiming), swept
radius 0.18 m (0.08 while aiming), 42 m. It stops at the nearest blocker and is drawn exactly to where damage lands.
`CrystalBeamIntegrator` turns frame time into 0.1 s ticks: each tick is one hit on the first receiver (one logical
target), 0.36 B, and 0.028 heat (0.28 a second). At most 0.1 s of beam is integrated per frame; the rest is discarded
and counted. The partial tick carries over between frames and between presses, so tapping cannot mint ticks.
Maintained aimed contact on one target ramps to 1.25x over 1.5 s; no contact, a new target or hip fire resets it.
Start and stop of the loop sound are keyed by the controller's instance id. The visual hides instantly on any stop.

**Gyre Crescent.** Press: disc at 30 m/s, swept radius 0.22 m, 1.3 B outbound, 0.26 heat, 1.43 s cycle. Banks off
static World geometry (only) at most twice; any other surface, a third wall or 32 m of travel start the return. Press
again while it is out: early recall. On the way back it steers to the hand, sweeping every step; a wall in the way
quenches it (no damage, the cradle refills). Each actor: at most one outbound and one return hit (0.65 B); at most
three actors per disc. Cars, props and turrets stop the disc. After the catch the cradle stays empty 0.35 s (rig
`DiscOut`). Hard lifetime 5 s. Aim with the disc in the cradle to see the dotted bank preview.

**Sunderheart.** Press: orb 24 m/s with a slight drop (gravity 2.5 m/s2 and a small lift), swept radius 0.2 m, 38 m,
0.35 heat, one a second. First contact (or the end of its range) starts a 0.35 s pulse: every minor hostile character
whose chest the pulse centre can see (line test against static World) gets one bounded nudge toward the centre through
its own knockdown (people) or push (creatures): at most 4 m/s. Bosses are never moved; they lose at most 12 percent of
their poise. Civilians, companions, staff, cars and scenery never move. Then the rupture: explosion damage on at most
six receivers inside 3.5 m that the centre can see, nearest first; the receiver the orb struck (or the nearest within
1 m) takes the centre hit 3.2 B, the rest 1.4 B. Splash touches only foes and practice-style receivers, never civilians,
cars, props or shields. Holding on after a launch concentrates the next orb (1.0 s, full when the cycle is ready):
2.5 m radius, 4.8 B centre (1.5x), 0.5 heat.

**Mirage Choir.** While aiming (right mouse, LT) the focus scans 10 times a second for the hostile nearest the reticle
inside the 18 degree cone (9 degrees either side) within 48 m with a clear line from the muzzle; 0.35 s of steady aim
locks it, up to three. Never locked: civilians, clerks and venue staff, companions, peaceful or calm wildlife, an
unprovoked territorial creature, a hidden or burrowed creature. Held locks are dropped when they die, leave sight or
range, stop being hostile, or drift beyond 35 degrees from the reticle; letting go of aim clears them. Press: five
shards, 0.45 B each, 42 m/s, 180 degrees a second turn rate, 0.24 heat, 0.9 s cycle, spread over the locks round robin
(no locks: a straight fan of +-8 degrees). A shard checks its lock's line of sight 10 times a second; once it is lost
the shard flies straight for good and never switches target. Every shard collides with the world.

**Mirrorwake.** Hold: 0.6 s charge (chime when full); a release before full fires nothing ("HOLD TO CHARGE"). Release
when full: an instant pulse along the legal muzzle path, swept radius 0.06 m, 60 m, 3.0 B to the first receiver,
0.32 heat, 1.1 s cycle. If it hits nobody and ends on static World geometry within 25 m it plants the single focus
(the art pass's focus marker, visual only, no collider) for 8 s of gameplay time; a new plant moves it. A charged pulse
aimed within 1.2 m of the focus travels muzzle to focus first (that leg is checked on its own; a wall in between ends
the pulse there and the focus stays). Someone standing on that leg takes the direct pulse and the pulse stops. When the
focus is reached it is consumed and the pulse bends to the nearest hostile within 30 m of the focus that the focus can
see (same eligibility as locks, never the direct victim), checked again on its own; the redirected hit deals the
profile's redirect value (1.0 B). The beam visual shows both legs (`ShowTwo`). The focus also disappears when the
player dies and on new game, load and title.

## HUD and rig

- Crosshair overlay: a crossed ring with the reason on refusals (BLOCKED, OVERHEATED, TOO MANY SHOTS, NOT FROM A
  VEHICLE, FOCUS BLOCKED), a shrinking ring and bar while charging (FAN READY, CONCENTRATED, PULSE READY), the beam's
  FOCUSING bar, Mirage Choir locks as gold corner brackets with "LOCK 1..3" and the lock being acquired as a thin
  filling ring, the Gyre bank preview dots, and the Mirrorwake focus ring with seconds left (REDIRECT when aimed at it).
- The existing weapon panel, heat bar and charge bar read the mirrored `Heat`, `Overheated` and `Charge`.
- `CrystalFocusRig` on the held model (and on the first-person viewmodel when one is found) gets Heat, Overheated,
  Charge, Firing, DiscOut, Locks and FocusPlanted every frame and `Pulse()` on every shot.

## F2 debug page "Crystal weapons" (Order 52)

Spawn each focus into the backpack (then Pause > ARSENAL to equip), vent every focus, clear crystal shots, reset the
beam totals. It shows live bodies against the cap, launched, refused and quenched counts, saturated queries and how
bodies ended, the beam integrator totals (ticks, contact ticks, simulated and discarded seconds, damage, heat, proc
rolls, ramp) and every focus's heat by uid with its vent and Resonance vent cooldown.

## Numbers added by this worker (not in the profile)

Shard radius 0.06 m, choir shard 0.07 m, disc 0.22 m, orb 0.2 m, pulse 0.06 m; beam radius 0.18 m hip and 0.08 m aimed,
sweep 240 and 90 degrees a second; projectile substep 0.05 s and frame cap 0.2 s; hit buffer 16; disc catch distance
0.9 m, catch recovery 0.35 s, lifetime 5 s; orb gravity 2.5 m/s2 with a 0.875 m/s lift; pull 0.35 s, nudge at most
4 m/s, boss poise cap 12 percent, centre zone 1 m; choir fan +-8 degrees, lock cone 9 degrees either side, keep cone
35 degrees, scan every 0.1 s, volley budget 3 full then half; Mirrorwake aim tolerance 1.2 m and plant range 25 m
(from the contract); beam element proc and skill roll every 0.5 s of contact; manual vent time = vent x heat (at least
40 percent); Resonance vent once per 4 s per uid (from the profile constant). One open question for the owner: the
profile's Mirrorwake `Secondary` is documented as B, so the redirected pulse deals 1.0 B (a third of a direct pulse).
If "redirected pulse share" was meant as 100 percent, change `Secondary` in `CrystalFocusDb` to 3.0; no code change.

## How Tyler tests it in Unity

1. Open the project, press Play, start Free Story. Press **F2**, open the **Crystal weapons** tab, press **Spawn**
   for each focus. Close F2, open **Pause > ARSENAL** and equip a focus into a slot (pad: Menu > ARSENAL, LB and RB to
   the tab, A to equip). Select it with the number key, the weapon wheel or a tap of D-pad left or right.
2. **Shardwake:** click left mouse (pad RT) at a wall 30 m away: watch the shard travel and chip the wall. Hold the
   button after a shot: the ring shrinks, a chime plays, "FAN READY"; let go: three shards fan out. Hold the charge and
   press Esc: nothing fires.
3. **Prism Thread:** hold left mouse (RT): a continuous beam that stops at walls. Aim with right mouse (LT) and keep it
   on one enemy: the FOCUSING bar fills. Watch the heat bar; at 100 percent it vents. Press R (tap D-pad down) to vent
   early. Alt-tab or open the map mid-beam: the beam and its sound stop at once.
4. **Gyre Crescent:** hold right mouse (LT) near a wall: dotted bank preview. Click to throw, click again to recall.
   Throw it down a corridor and step around a corner so the way back is blocked: it disappears, the cradle refills.
5. **Sunderheart:** fire at a group of small enemies: they are drawn in briefly, then the burst. Hold after a launch
   for about a second and let go: a concentrated orb (smaller, stronger burst). Fire beside a wall with enemies behind
   it: they are neither pulled nor hurt.
6. **Mirage Choir:** hold right mouse (LT) over enemies: gold corner brackets appear one by one (up to three). Click:
   five shards curve to them. Aim at a civilian or a calm habitat creature: no lock appears.
7. **Mirrorwake:** hold left mouse (RT) until the chime, let go at a bare wall within 25 m: a mirror focus appears.
   Aim at it (the ring turns gold, "REDIRECT"), hold and release: the pulse goes to the focus and bends to a nearby
   enemy (two beam segments).
8. Swap weapons while a focus is overheated and swap straight back: it is still venting. Check the F2 page heat list.
9. Try every focus with the muzzle pressed into a wall: "BLOCKED" and a short blip, no damage on the far side.

Look for: projectiles visible at normal camera distance, beams ending exactly where enemies flinch, no sound left
playing after a stop, readable crosshair hints, and frame rate while many shots fly (Low preset caps at 32).

## Known limits

- NOT RUN in Unity: no gameplay, visual, audio or performance claim.
- The crystal bar (Resonance energy) charge is normalized per hit by the gun's interval, so each Mirage Choir shard and
  each Gyre Crescent hit charges like a full shot; the existing per-second bucket cap still bounds the total.
- The first-person viewmodel rig is found by searching under the camera every half second; if the art pass's
  viewmodel has no `CrystalFocusRig` it simply is not animated.
- Sunderheart's pull on people uses their existing knockdown (a short ragdoll), which is the only knockback that
  respects collision for humans today; creatures get a push.
- Assistance hostility uses existing flags: an unprovoked territorial creature is never locked or redirected onto even
  if it is about to attack; it becomes eligible the moment it is provoked or engaged.
