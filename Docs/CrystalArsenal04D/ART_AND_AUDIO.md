# Crystal Arsenal 04D: models and audio

Status: built and checked offline (compile in all twelve configurations, EditMode tests, an offline mesh harness with
preview renders). **Not run in Unity.** Nothing here has been seen in the game yet: lighting, bloom, animation feel,
sound mix and frame rate all need Tyler's eyes and ears (checklist at the end).

## What was built

Everything is procedural C#, like the rest of the game. Files:

| File | What it holds |
|---|---|
| `Combat/CrystalFocusModels.cs` | The frozen API (`Create`, `CreateShot`, `CreateBeam`, `CreateFocusMarker`, `CreatePickup`, `CreateCase`, `CreatePracticeTarget`), the enums `CrystalShotKind` and `CrystalImpact`, mesh baking, caching, materials, `CrystalArtStatics` |
| `Combat/CrystalFocusModels.Kit.cs` | The engine-free shape kit (lofts, shards, gems, fins, plates, tori, cracked orbs, glow cards) and the plain-data model classes `CrystalModelGeometry` and `CrystalPartGeometry` |
| `Combat/CrystalFocusModels.Shardwake.cs` ... `.Mirrorwake.cs` | One file per focus |
| `Combat/CrystalFocusModels.Shots.cs` | Shot bodies and the Mirrorwake focus marker |
| `Combat/CrystalFocusModels.Props.cs` | The resonance case and the practice target |
| `Combat/CrystalFocusModels.Palette.cs` | The colour palettes (plain data) |
| `Combat/CrystalFocusModels.Pose.cs` | The pure pose and glow maths the rig uses (`PosePart`, `GlowOf`, `SpinRate`) |
| `Combat/CrystalFocusRig.cs` | The rig component (same public fields as before, plus `Bind`, `SetCompartmentOpen`) |
| `Combat/CrystalBeamVisual.cs` | The beam presentation |
| `Audio/CrystalSounds.cs`, `CrystalSounds.Synth.cs`, `CrystalSoundsDriver.cs` | The crystal sound set (moved out of the models file; same static API) |
| `Tests/EditMode/CrystalArt04DTests.cs` | 16 pure tests (budgets, conventions, poses, sounds) |

Every model is a static body mesh (the root, so the hand pose reads its bounds) plus moving-part children. Meshes are
built once per look and shared by every instance; materials come from a cached per-family palette. Nothing allocates a
material per frame or per shot: the rig changes glow through one reused `MaterialPropertyBlock`, the beam only changes
positions, widths and vertex colours. No visual has a collider.

## The six foci

All follow the loot gun conventions: origin at the grip, +Z along the emitter, +Y up, a `Muzzle` child, a `Foregrip`
child on two-handed foci only (every hold except Pistol). Structure and metal are matte or metallic and never glow;
emission lives only on the active facets and cores. No familiar firearm parts: no barrels, magazines, slides, sights or
skeleton stocks. The two-handed foci each have their own brace and their own support-hand feature.

| Focus | Hold | Silhouette | Moving parts and states | Triangles |
|---|---|---|---|---|
| Shardwake | Pistol (cupped) | Lilac mineral grip with finger ridges and a crystal pommel, a lopsided hewn palm block with a heavier right cheek and glowing seams, a bright shard core floating ahead of it inside three unequal stone ribs (a long one arching over the top, a medium one on the right, a short one low on the left) | Core spins slowly and bobs; charging spins it up, draws it back and swells it; ribs flare open on charge and kick on a shot; venting makes the core tremble and the ribs open | 650 |
| Prism Thread | Rifle | Ivory ceramic tuning fork: a resonator hoop with a crystal strung across it (the shoulder brace), a teardrop resonance chamber with glowing windows, the rounded crotch of the fork, two long tines with an open channel between them, an open carrying loop under the fork for the support hand | The floating lens spins slowly; while the beam is on the tines part (the emitter opens) and the lens stops and steadies; overheated, the tines spread wide and glow hot | 1136 |
| Gyre Crescent | Pistol (cupped) | Dark slate grip with copper bands carrying an open crescent cradle that opens forward, a big faceted teal disc standing upright in it on copper hub caps, copper fins and horn spurs | The disc spins in the cradle; when it is out the cradle is visibly empty and its groove pulses; when it comes back it re-forms from small with a bright fast spin | 1000 |
| Sunderheart | Heavy (hip braced, support hand on the top handle) | A hewn basalt block banded in brass with stone slabs and knuckles, a brass carry handle over the top, three blunt stone jaws caging a suspended compressed core (a cracked dark shell over an amber glow) circled by three amber pebbles | Core tumbles; concentrating compresses it and pulls the pebbles in; a launch flexes the jaws, kicks the core back and flings the pebbles out; the rear vent ring glows with heat | 1159 |
| Mirage Choir | Rifle | A sinuous sandstone stem with two swept leaf wings and gold filigree rising into a large off-centre seed pod (up and right): a hewn cup ringed by curling gold sepals holding five big rose crystal seeds; a forward-curving thorn for the support hand; a curling tendril with a gold crescent on the shoulder | Each lock lifts and lights one seed; a volley fans all five out of the pod and they settle back, relighting one after another | 962 |
| Mirrorwake | Rifle | A diamond-profile obsidian housing with a dorsal blade, a swept keel for the support hand, a single tapered spine to the shoulder; ahead of it a dark three-sided prism floating on the emitter axis inside a fan of six mirror facets twisted like fan blades | Charging draws the facets in until they close around the prism; a pulse snaps them shut and they spring back out and settle; while a focus is planted the top facet is missing from the fan (it is the focus) | 486 |

Budget: about 1,500 triangles per held focus. All six are under it.

Hand points (model space, metres):

| Focus | Muzzle | Foregrip | Root bounds rear z |
|---|---|---|---|
| Shardwake | (0, 0.076, 0.226) | none (pistol cup) | about -0.05 |
| Prism Thread | (0, 0.075, 0.500) | (0, -0.004, 0.188) on the loop | -0.23 (hoop: shoulder pocket) |
| Gyre Crescent | (0, 0.118, 0.169) | none (pistol cup) | about -0.03 |
| Sunderheart | (0, 0.100, 0.372) | (0, 0.212, 0.060) on the handle | -0.20 (stays a hip-braced heavy, above -0.3) |
| Mirage Choir | (0.012, 0.105, 0.375) | (0.005, 0.028, 0.132) on the thorn | -0.24 (crescent: shoulder pocket) |
| Mirrorwake | (0, 0.075, 0.420) | (0, 0.022, 0.130) on the keel | -0.26 (heel: shoulder pocket) |

The Mirage Choir muzzle sits 12 mm right of the centre line because its pod is deliberately off-centre.

## Shots, beam, marker and props

Shot bodies (+Z forward, at most 200 triangles, no shadows):

| Kind | Look | Triangles |
|---|---|---|
| Shard | A bright 8-sided mineral shard with two trailing splinters and a short refractive tail (crossed glow cards that fade in over a short lead and out to nothing), plus a small glow | 60 |
| FanShard | Smaller, flatter, with two swept barbs and a shorter tail | 44 |
| Disc | The same faceted disc that leaves the Gyre Crescent cradle, drawn larger (0.19 m), spinning fast about its axle, over a soft glow card | 190 |
| Orb | A cracked dark shell over a dense amber core, tumbling, three orbiting amber pebbles, inside a soft glow | 122 |
| ChoirShard | A slim rose cross-bladed shard with a bright spine and a short tail | 50 |

The Disc and Orb bodies carry a `CrystalFocusRig` (role Shot) that spins them; the others are static meshes.

Beam (`CrystalBeamVisual`): a narrow bright core line inside a wider translucent shimmering sleeve, a soft glow where it
leaves the emitter and a contact glow where it stops; `ShowTwo` adds the second core and sleeve from the focus to the
target with a glint at the bend. Prism Thread is cyan-white, Mirrorwake violet-white.
- Prism Thread is sustained: call `Show` every frame; `Hide` cuts every part at once. As a safety net only, a beam that
  nobody has refreshed for 0.25 s of real time hides itself.
- Mirrorwake is a pulse: call `Show` or `ShowTwo` once per shot; it fades out by itself over 0.25 s. `Hide` still cuts it.

Focus marker (`CreateFocusMarker`): one hovering mirror facet (0.36 m tall) with a glowing rim and a bright core stud,
over a soft glow card, facing +Z. It turns slowly, bobs and scales in each time it is enabled. Place it a little off the
surface with +Z along the surface normal.

Resonance case (`CreateCase`): origin on the ground, about 1.4 m wide, 0.72 m deep, lids at 1.1 m, the glowing spire
to 1.5 m, front facing +Z. A stone plinth with brass trim and a front emblem, two glass compartments (left = starter,
right = second compartment), each with a glowing cradle inside and a hinged lid, crystal growth at the front corners.
- `CrystalFocusRig.SetCompartmentOpen(0 or 1, true or false)` on the returned object opens or shuts a lid (the lid swings
  up and back over about 0.6 s).
- Empty children `Slot A` and `Slot B` mark where a display copy (`CreatePickup`) can rest on each cradle.

Practice target (`CreatePracticeTarget`): origin on the ground, a crystal post to 1 m, a brass cradle holding a
double-sided bullseye (outer stone ring, glowing ring, brass ring, bright core) of radius 0.35 m centred at
(0, 1.35, 0), a crystal finial to 1.9 m. Nothing crosses the bullseye's face. `CrystalFocusRig.Pulse()` on the returned
object makes the bullseye flash and shiver for a hit.

## The rig

`CrystalFocusRig` keeps its public fields exactly (Behavior, Heat, Overheated, Charge, Firing, DiscOut, Locks,
FocusPlanted, virtual Pulse). The weapons runtime writes the fields every frame; the rig smooths them and, in
LateUpdate, poses every moving part and sets the glow:

- Idle: cores, lenses and prisms spin and bob, Mirrorwake's facets drift, pebbles orbit.
- Heat: active facets warm from 0.55 to 1.3 times their base glow.
- Overheated (vent): active facets and cores flicker hot and shift toward the family's vent colour; ribs, tines and jaws
  open; Shardwake's core trembles.
- Charge: per family (ribs flare and the core spins up, pebbles pull in and the core compresses, facets converge); a short
  flare when the charge completes.
- Firing: the tines part and the lens steadies.
- Pulse: a kick that decays in about a third of a second; Mirage Choir's volley fan; the target's hit flash.
- DiscOut: the disc leaves the cradle; when it returns it re-forms with a bright spin-up.
- Locks: one lifted, brighter seed per lock.
- FocusPlanted: the top facet leaves the fan.

The rig uses the game's scaled time, so a pause freezes it. The posing and glow maths are pure
(`CrystalFocusModels.PosePart` and `GlowOf`), shared by the game, the harness and the tests.

## Sounds

Synthesized with the project's `AudioSynth` primitives, like the loot and gadget sounds. One clip per job:

| Call | Sound |
|---|---|
| `PlayShot` | Shardwake: glassy tink with a short refractive echo. Prism Thread: a rising ignite (at most every 0.4 s; the beam itself is the loop). Gyre Crescent: a whirring throw with a metallic ring. Sunderheart: a dense low stone thoom with a brass ring. Mirage Choir: five staggered glass chirps. Mirrorwake: a precision crack with a mirrored ring |
| `PlayChargeReady` | Two-note glass chime, pitched per family |
| `StartLoop` / `UpdateLoop` / `StopLoop` | Prism Thread: the beam's ignite, a seamless humming sustain (louder and higher with intensity) and a falling cooling release with crystalline ticks, so the stop is unmistakable. Other families: a reedy charge hum that rises with intensity |
| `PlayImpact` | Stone: dull chip and grit. Armor: metallic ping. WeakPoint: bright crack with a chime. Crystal: glassy shatter. Flesh: soft thud. Air: a quench fizz |
| `PlayVent` | Steam hiss with cooling ticks, pitched per family |
| `PlayDenied` | A short muted double blip |
| `PlayReward` | A rising crystal chord |
| `StopAll` | Fades every loop out in 30 ms |

Rules the code keeps:
- One loop per key: `StartLoop` on a key that already has a loop, even one still fading, reuses it. `StopLoop` fades it
  in 60 ms (no click). Four loop voices, ten one-shot voices.
- Gain caps: one-shots normalized to 0.8 peak and played at 0.8 volume or less, loops normalized to 0.5 and played at
  0.45 to 0.6; every one-shot has a minimum gap per clip (30 to 500 ms) so a burst of calls cannot stack.
- Every one-shot ends at silence; the loops are built so the wrap is seamless (tested).
- The bank (19 clips, about 12 s of audio, roughly 100 to 200 ms of synthesis offline) is built a clip per frame once
  anything asks for it: `CrystalSounds.Init(parent)` if a system calls it, otherwise the first focus model
  (`Prewarm`) or the first sound. A clip needed before its turn is made on the spot.
- Loops fade on real time, so a stop during pause or slow motion completes. The game's pause also pauses the listener.

## Previews and the harness

Offline renders (flat shaded, the game's palette and pose maths; glow drawn unlit; not in-game captures) in
`Docs/CrystalArsenal04D/Previews/`:

- `focus_shardwake.png`, `focus_prismthread.png`, `focus_gyrecrescent.png`, `focus_sunderheart.png`,
  `focus_miragechoir.png`, `focus_mirrorwake.png`: side, front three-quarter, over-the-shoulder and top views with the
  Muzzle (red), Foregrip (green) and grip origin (yellow) marked, plus two animated states each.
- `foci_lineup.png`: all six at one scale, side and over the shoulder.
- `foci_silhouettes.png`: pure black silhouettes at about on-screen size, three views each.
- `shots.png`, `case_and_target.png`, `focus_marker.png`.
- `harness_report.txt`: the checks and the triangle, size, Muzzle and Foregrip numbers.

The harness is a copy of the loot gun harness at
`/tmp/claude-0/-home-claude/2986159d-8764-5bf4-b761-ef4129d8df63/scratchpad/CrystalArtTest` (`run.sh [out_dir]` rebuilds
the art sandbox runtime and runs it). It checks every part for finite data, winding, degenerate triangles and parent
order, the triangle budgets, the Muzzle, Foregrip, grip and brace rules, and the pose states.

## Hand-off notes for the other parts of 04D

- Weapons: feed the held model's rig every frame and call `Pulse()` on each shot or launch. The first-person viewmodel
  (`PlayerCamera`) builds its own copy of the model; its rig idles unless it is fed too. A one-line call to
  `CrystalSounds.Init(transform)` in `CrystalShots.Ensure` would start the sound bank at boot (optional; it otherwise
  starts at the first focus model).
- Arsenal: open the case compartments with `SetCompartmentOpen`, flash a target with `Pulse()`, rest display copies on
  `Slot A` and `Slot B`. The practice target collider should cover the bullseye disc (radius 0.35 m at 1.35 m) and the
  post (radius 0.09 m, 0 to 1 m).

## What to check in Unity (NOT RUN)

1. Equip each focus (F2 > Crystal weapons spawns them): look at it in third person, first person, aiming and while
   strafing and flying. Check the grip in the hand, the support hand on the loop, handle, thorn and keel, and that the
   rifle braces rest at the shoulder.
2. Watch every state: Shardwake charge (ribs flare), Prism Thread firing and overheating (tines), Gyre Crescent throw
   and return (empty cradle, re-forming disc), Sunderheart concentrate and launch, Mirage Choir locks and volley,
   Mirrorwake charge, pulse and a planted focus.
3. Check the glow under daylight and at night: active facets should glow, structure should not, and bloom should not
   wash the models out.
4. Fire at walls, armour, weak points, crystal and empty air and listen: each impact distinct, no stacked spikes; hold
   the beam, release, swap, pause and die mid-beam: no stuck or doubled loop.
5. Look at the resonance case from down the street and up close (glass, lids), and at the practice targets.
6. Frame time with many shots in flight (64 on normal presets).

## Known limitations

- No level-of-detail variants (the meshes are small, but a far LOD was not made).
- Glass and glow look were judged only in offline flat renders; the real materials (Standard or URP Lit, Particles
  Additive for glows and beams) were not seen.
- The rig has no "aiming" input (the frozen fields have none), so there is no separate aim pose beyond charge and fire.
- The beam has no light; contact glow is a soft card only.
