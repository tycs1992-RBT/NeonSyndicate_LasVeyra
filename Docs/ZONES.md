# Zones: the Frostline and the Inner World

Two new places, both built at runtime like the rest of Las Veyra. Nothing existing moved: the layout proof (LayoutTest, see the end) shows every collectible, site, POI, pickup spot, parking spot, prop, lamp, tree, road and block in exactly the same place as before.

## The Frostline (snow zone)

**Where.** The north-west of the map outside the city ring: everything west of x 120 and north of z 1080, with the south edge curving down to about z 970 toward the west wall. The Canyon (300, 1450) and the Crystal Manor (470, 1470) stay desert. It is its own district, `District.Frostline` ("The Frostline"), appended after `QuartzBadlands`.

**What is there.**

- Snow ground with a soft, wandering edge: a frost-dusted band, drift islands pushed into the sand and bare patches near the border, never a straight line. Roads stay clear.
- About 565 pines (layered cones under snow), snow-dusted boulders instead of cacti, clusters of ice crystals, drifts, and snow caps on the north and west mountains (heaviest around the Frostline, fading toward the corners).
- **Rimeglass Lake**, a frozen lake with glossy cracked ice. Site key `FrostLake`, centre (-760, 1330), radius 70.
- **Frost Wardens Outpost**: palisade, lodges, a watchtower, banners and four braziers with real flickering lights. Site key `WardenOutpost`, centre (-330, 1185); POI "Frost Wardens Outpost" (type None) at its gate.
- **Titan Hollow**, an arena ringed by 14 leaning ice pillars for the Glacier Titan. Site key `TitanHollow`, centre (-1050, 1255), radius 58.

Use `MissionManager.SitePos("FrostLake", fallback)` and friends, or `MissionManager.PoiPos(CityLayout.WardenOutpostPoiName, fallback)`.

**Rules.** No pedestrians and no traffic in the Frostline. Like the Quartz Badlands it is wilderness: `CityLayout.IsWilderness(d)` is true for both, so there are no police patrols, no anonymous crime calls and no city ambience. Off-road grip: desert sand 0.78 (unchanged), snow 0.62, the frozen lake 0.42. Bullets throw up a white snow puff and leave no hole, and footsteps crunch in the snow (roads and the lake ice keep the normal step).

**Weather and mood.** `Atmosphere.FrostAmount` follows the player from 0 at the edge to 1 about 150 m in: falling snow (much heavier in storms; storms take the place of sandstorms there), colder and thicker fog, cooler light and ambient, and `PostGrade.Frost` for the colour grade. The minimap and the pause map show the snow, the lake, the pines and the pillars.

**Zone banner.** Crossing into the Frostline (from the city or the badlands) shows "THE FROSTLINE / Danger: high"; leaving the city for the open desert shows "QUARTZ BADLANDS / Danger: moderate". Each banner waits 45 s before it can show again, and never shows after a load, a respawn or a teleport, or over another banner.

**Pure queries** (no engine calls, safe anywhere): `CityLayout.FrostDepth`, `IsFrostline`, `FrostAmount`, `SnowCover`, `IsSnowGround`, `IsOnFrozenLake`, `FrostLakeShore`, `MountainSnow`. Layout lists: `PineSpots`, `SnowRockSpots`, `IceCrystalSpots`, `SnowDriftSpots`, `FrostFloraSpots`, `IcePillars`, `BrazierSpots`, `Mountains`, `Mesas`, `WardenOutpost`.

## The Inner World (the Veil)

**Shifting.** The Veil opens together with the Third Eye (`Abilities.ThirdEyeUnlocked`, also `InnerWorld.Unlocked`). A tap of the Third Eye input is bullet time exactly as before (it is decided on release); holding it for 0.7 s shifts through the Veil instead and never also starts bullet time. A ring of dots around the screen centre fills while you hold.

| | Keyboard | Controller |
|---|---|---|
| On foot | Hold T or Caps Lock | Hold LB + RB |
| In a vehicle | Hold T or Caps Lock | Press LB + RB together and hold |

Tap-only inputs keep toggling bullet time at once: the D-pad Up tap in vehicles, and Caps Lock on macOS with the classic Input Manager (it reports the caps toggle, not the key), where T is the key to hold. In vehicles both bumpers pressed within 0.12 s of each other are the Third Eye chord once it is unlocked; holding LB to aim first and then pressing RB still fires a drive-by.

Inside the Veil the hold always works, even without the Third Eye (a quest can carry a player in before it is unlocked), so there is always a way back out. Without the Third Eye a tap does nothing there.

**Cost.** Entering costs 25 crystal energy, less with the Veil skills, the Frequency Order's Veil Attunement and brews (`InnerWorld.ShiftCost`). Inside, energy drains at 2 per second (less with the same bonuses) and does not refill; at zero the Veil throws you back with a stagger and a message. The Third Eye's bullet time burns its own 12 per second on top (9 after the Law of Power). Leaving by choice is free. Player shifts are at least 1 s apart. The HUD shows a VEIL tag with the seconds left at the real drain (both drains and every bonus) beside the energy bar, which turns violet, and the hold ring asks for the real cost.

**What changes inside** (blended over 1.2 s):

- Red-violet sky, a low crimson sun, violet fog, darker violet ambient; `PostGrade.InnerWorld` follows the blend.
- The Veil is dry: no rain or snow, no wet roads, no rain hiss or weather wind, no thunder and no lightning, whatever the weather outside (it all comes back as you return).
- Pedestrians and moving traffic vanish (the spawners pause and get back exactly the state they had when you return). The player's car, parked cars, police, mission characters and anyone already after you stay: the Veil is no free escape. Gang heat keeps cooling and bodies keep being cleared while you are inside.
- Street lights burn violet at any hour.
- Crystal growths spring up on sidewalks, lots and open ground within 80 m (pooled, one per 12 m world cell, always the same spot), great spires 30 to 89 m tall stand around the city, and islands of rock and crystal float in the haze. All of it is scenery without colliders.
- Each shift plays a distortion pulse: flash, camera shake, a short slow motion, a whoosh and chime, rumble.

The Veil is never saved: a new game, a load and the title screen always start in the Outer City.

**API** (`Game.Inner`, created right after Atmosphere):

- `IsInner`, `Current` (Outer or Inner), `Blend` (0..1, eased), `Pulse` (shift flash, 0..1).
- `Shift(bool toInner, bool instant)` moves the world without any energy cost (`instant` skips the blend and the pulse).
- `PlayerShift()` is the player's own shift with the energy rules; returns false when it cannot happen.
- `event Action<bool> Shifted` is raised on every shift (true when entering). A throwing listener is logged and does not stop the others.
- `CrystalSpotsNear(Vector3 centre, float radius, List<Vector3> into)` gives the growth positions near a point (deterministic, available inside or out, radius capped at 300 m): good spawn and loot points for creatures and missions.
- `DrainPaused` stops the energy drain for scripted scenes (cleared when the Veil closes).
- `InnerWorld.ShiftCost` (static) is what a player shift costs right now; `DrainNow(abilities)` is the energy lost per real second inside right now (the Veil's drain with its bonuses, nothing while `DrainPaused`, plus the Third Eye while it is open); `InnerWorld.DrainRate(paused, multiplier, other)` is the same sum as pure maths.
- `FocusOverride` and `FocusPoint` (also on `Atmosphere`, for the Frostline cold) make the growths and spires gather around a point other than the player; only the QA golden shots use them.
- Placement is pure: `CityLayout.VeilCrystalAt(cx, cz, out VeilCrystalSpot)` and `CityLayout.VeilSpireAt(...)`.

## Tuning

- `InnerWorld.EnterCost` (25), `DrainPerSecond` (2), `HoldSeconds` (0.7), `BlendSeconds` (1.2), `CrystalRadius` (80 m); the skill and perk multipliers are `SkillMods.VeilShiftCost` and `SkillMods.VeilDrain`, the Third Eye's drain is `Abilities.ThirdEyeDrain`.
- Growth density: `VeilBlockChance` (0.42, city blocks) and `VeilOpenChance` (0.2, open ground) in CityLayout.Veil.cs.
- Who stays in the Veil: `InnerWorld.VanishPeople` and `TrafficManager.ClearDrivenTraffic` (police and pursuers stay on purpose).
- Frostline shape: `CityLayout.FrostEastX`, `FrostSouthZ`, `FrostWestDrop`; snow edge: `SnowNoiseMax`, `SnowThinBand`. Moving the zone moves the snow, the weather and the map, but the three sites have fixed centres.

## How it was checked

- The project compiles with no errors or new warnings in all six configurations (runtime and editor scripts; editor and player; legacy input, both, new input only).
- LayoutTest (an offline harness that compiles the layout code against the Unity reference assemblies): the before/after dump comparison reports the existing layout unchanged; Frostline checks (districts, snow bands, sites in deep snow, no snow on roads or the desert landmarks, flora routing, pine spacing, the mountain sequence identical to the old one, snow mesh areas adding up) and Veil checks (placement off roads, buildings, the canal, the lake and the ridge; determinism; counts near sample points; spires in view everywhere) pass on the game seed and six other seeds.
- LogicTest, run against the built player assembly: the tap and hold gesture (taps, holds, frame hitches, resets, 30 to 240 fps) and the snow footstep clip.
- The Veil's cost and drain helpers are checked offline by `ProgressionFixTests.VeilCostAndDrainUseTheSkillNumbers`; the QA golden shots now include a Frostline view and a Veil view (see TOOLING.md).
- Not checked in the Unity editor or on hardware: how it looks and feels, frame rate, and the controller chords on a real pad.
