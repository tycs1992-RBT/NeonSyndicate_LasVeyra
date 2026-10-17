# Phase 5: camera and motion

Part of the visual upgrade run (see `Docs/VisualUpgrade/`). Built-in Render Pipeline; nothing here is pipeline
specific, and the URP beta compiles unchanged. Hard rule kept: camera follow timing, field of view, controls, hit
frames, dodge windows, attack commits and every other gameplay-read value are identical to before. Everything in this
phase is either a lens description the renderer reads, or an additive visual offset that is removed again before any
game logic runs.

Compile and test proof: all twelve configurations build with 0 errors and the baseline 63 warnings
(`out_p5/urp`); the offline EditMode run is 1188 passed (baseline 1169 plus 19 new), the same 25 engine-only failures
(`Resources.Load` tests) and 53 skipped.

## 1. Physical camera

`PlayerCamera.Create` now calls `SetupPhysical`, which turns the main camera physical:

| Property | Value | Why |
|---|---|---|
| `usePhysicalProperties` | true | The renderer describes the camera as a lens on a sensor |
| `sensorSize` | 36 x 24 mm | Full-frame sensor |
| `gateFit` | Vertical | The vertical field of view is the one the game sets, at every monitor aspect; an ultrawide gains width only (test `WiderMonitorsKeepTheVerticalViewAndGainWidth`) |
| `lensShift` | 0, 0 | No tilt-shift |
| `focalLength` | computed from the vertical field of view every time it is set | `CameraOptics.FocalLength(fov, 24)`; the 55 degree default is a 23.05 mm lens, a 90 degree Surge is 12 mm, the 4 degree scope is 343 mm |
| `aperture` | f/2.8 in cinematic, title and pause views; f/8 in play | For the Phase 3 depth of field to read; play stays stopped down because depth of field is off there |
| `shutterSpeed`, `iso`, `bladeCount` | 1/120 s, 200, 7 | Lens description only. The post stack must keep its own eye adaptation for exposure; these are not an exposure change |
| `focusDistance` | written every frame from `FocusDistance` | See below |

Every field of view write in the camera goes through one method, `PlayerCamera.SetFov` (PlayerCamera.Motion.cs),
which writes both `fieldOfView` and the derived `focalLength`, so the projection is exactly the old one whichever of
the two Unity treats as the source in physical mode. `QaRunner.ApplyPose` (QA folder, not touched) still writes
`fieldOfView` directly after calling `SetCinematic`; the cinematic path lerps to the same number within its 3 second
settle, so golden shots frame as before either way.

Read-only values for the post stack (all on `Game.Cam`, null-check it):

- `FocusDistance` (metres): the character or vehicle pivot on foot and driving (`DistanceToPivot`), 4 m in first
  person, the look point of a cinematic, the orbit's look point on the title flyover. Also mirrored into
  `Camera.focusDistance`.
- `DepthOfFieldView`: true for cinematics (`InCinematic`), the title flyover (`IsTitleView`) and the pause menu
  (`IsPauseView`). The directive wants depth of field on cinematic and menu cameras only; gate it on this.
- `CurrentFov`: the vertical field of view last written, degrees.
- `CinematicBreath`: true while a breathing cinematic shot runs (see below), so a hand-held shot can also soften its
  focus if the post stack wants that.
- `Camera.aperture` as in the table.

The pure maths lives in `Characters/CameraOptics.cs` (no engine calls, no mutable state, so no `ResetStatics` is
needed): `FocalLength`, `VerticalFov`, `HorizontalFov`, the `Drift` and `Breath` curves and `RampIn`. Tests:
`Tests/EditMode/CameraOpticsTests.cs` (round trip at every field of view the camera uses, the known lens numbers,
aspect behaviour, bounded and continuous drift curves, the ramp).

## 2. Subtle camera motion (never on a gameplay camera)

All three live in `Characters/PlayerCamera.Motion.cs` and share one mechanism: after the camera has been placed for
the frame, `ApplyOffset` adds a small position and rotation in the camera's own frame and remembers the base pose;
the first thing the next `LateUpdate` does is `RestoreBasePose`, so camera state, the aim ray, aim assist and the
smoothing all start from the camera's own pose and never see the offset. `Settings.CameraShake` off disables all
three (a reduced-motion switch; Settings.cs itself was not edited).

| Motion | Where | Amplitude | Notes |
|---|---|---|---|
| Title sway | the title flyover (`UpdateFlyover`), `TitleSway` field | 1.5 m, 0.6 degrees, two slow incommensurate frequencies per axis | Rides on top of the existing orbit; the orbit speed is unchanged |
| Pause drift | the gameplay camera while `Game.State == Paused` (the pause menu draws the live world under a 55 percent black panel), `PauseDrift` field | 2 cm, 0.3 degrees | Eased in over 1.5 s after the pause begins so the menu never pops open mid-drift. It stops being applied on the first frame play resumes: the gameplay camera is untouched from its first live frame, at the price of a sub-centimetre, sub-degree release at the moment the menu closes (itself a UI cut). Reset whenever the camera is not in the gameplay branch |
| Cinematic breath | `SetCinematic(position, lookAt, fov, breathe: true)` only | 1.5 cm, 0.25 degrees, a 4 second breathing cycle with a faint tremor | Default off: the existing three-argument `SetCinematic` turns it off, so `QaRunner` and every current cutscene are deterministic as before. `ClearCinematic` clears it. Eased in over 1 s from the cut |

No caller opts into the breath yet: mission and campaign code is outside this phase's file ownership. To use it, a
cutscene would call the four-argument `SetCinematic` for the shots that should feel hand-held.

Camera shake, recoil, the crosshair and `AimRay` are untouched.

## 3. Animation blending audit

What was read: `HumanoidAnimator`, `HumanoidPose` (`Solve`, `Smooth`, `Attack`, `Roll`, `Backstep`, `Stagger`,
`Carry`, `AimPost`, `SupportWeight`), the `Cover`, `Ride`, `Travel` and `Weapons` partials, `CreaturePose`
(`Solve`, `PhasesOf`) and `Creature.Motion.Animate`.

| Transition | Finding | Action |
|---|---|---|
| Weapon hold change (pistol to rifle, melee) | The arm targets change in one frame, but the whole upper body passes through the solver's output filter (`Smooth`, 20 Hz, about 50 ms time constant, 95 percent in 150 ms) and the support hand eases on and off at 8 per second | Already blended. A second crossfade in the animator would double-smooth; left as is |
| Cover enter and exit | `_coverW` moves at 6 per second (167 ms), side and blind fire eased too | Already blended |
| Travel power start and end | `_travelW` at 6 per second plus the travel system's own `TravelBlend` | Already blended |
| Ride mount and dismount | `_seatW` at 3.5 per second (286 ms); the root is teleported onto the seat by gameplay, where `ResetFeet` resets the filter on purpose (the position cut hides it) | Already blended; the teleport is gameplay |
| Dodge start | `Roll` fades in over the first 16 percent of the dodge, `Backstep` over the first 10 percent, both from zero | Already blended; the invulnerability window is read from `DodgeT` by gameplay and is not touched |
| Humanoid attack wind-up | `Attack` weights the wind-up key by `E3(t / W)` from zero; the strike key follows the hit frame unchanged | Already blended; hit frame untouched |
| Humanoid attack cut short (stagger, cancel) | The attack pose drops out in one frame but the output filter runs at its snappy 34 Hz during actions (about 90 ms to 95 percent) and the stagger itself eases in over 180 ms | Already blended by the filter; left as is |
| Aim on and off | `_aimW` at 9 per second (111 ms) | Already blended |
| Hands up, cower, phone, gestures, fingers | 4 to 5 per second, fingers damped at 14 Hz | Already blended |
| Ragdoll get-up | 0.9 s blend from the lying pose | Already blended |
| Telegraph glow (humanoid NPCs) | The wind-up flare switched on and off in one frame (`Anim.Telegraph` is set to 0 at the hit) | **Changed, visual only**: `HumanoidAnimator` keeps rises instant (the warning cue is unchanged) and eases falls at 24 per second, so a full flare fades over about 110 ms (`_glowShown`, `GlowFallPerSecond`) |
| Creature action wind-up | `Phases.W` eases every action in over the first 12 percent (or half the wind-up) and out over the last 18 percent | Already blended; `Windup` and `Strike` fractions the runtime reads are untouched |
| Creature action cut short (stagger, brain decision, boss phase, cancel) | **A real snap**: the action pose at full weight dropped to the gait in one frame, since `CreaturePose` has no output filter | **Changed, visual only**: `CreaturePose.CancelBlend.cs` keeps the last written pose when the action identity changes or restarts while the previous one still carried weight, and fades the new output from it over `CancelBlendSeconds` (120 ms) by the cut action's weight. Only `LocalRotations` and `LocalPositions` (what the rig copies to its bones) are blended; the solver's state, `ModelPosition`, the action timers and the hit frames are untouched. Reset and teleports clear it. An action that runs to its end never starts a fade. Tests: `CreatureCancelBlendTests` |
| Creature glow at attack end | `_glow` eases down at 6 per second during the strike but jumps to 1 the frame `_atk` clears, and the warn and stagger levels switch in one frame | **Changed, visual only**: `Creature.Motion.Animate` eases falls at 24 per second, rises stay instant (`_glowShown`) |
| Creature animation at distance | Poses update every 2, 3 or 6 frames by camera distance (and every 8 off screen) | A deliberate level of detail, not a blend problem; left as is |

Because the creature fade moves the bones the rig's hit volumes and head anchor hang from, those follow the visible
body during the 120 ms fade, which is the point: the visible mesh and what the player can hit stay the same thing.
`GaitTests`, `HumanoidPoseTests` and `QuartzRegaliaTests` pass unchanged.

## 4. What each graphics preset gets

Nothing in this phase has a GPU cost, so nothing is tied to `Settings.Graphics`: the physical camera is a different
description of the same projection, the motion is three additions to a transform, and the glow and pose fades are a
few lerps that already ran. Depth of field itself belongs to Phase 3 and its presets; this phase only supplies the
numbers it reads.

## 5. Not done, and why

- No `Settings` toggle for the sway and drift: `Core/Settings.cs` is owned by other agents this run. The existing
  `Settings.CameraShake` acts as the reduced-motion switch, and `PlayerCamera.TitleSway` and `PauseDrift` are the
  per-feature switches. A dedicated setting can be added by the Settings owner and wired to those two fields.
- No cutscene opts into the cinematic breath (mission code is out of scope). It is ready for the four-argument
  `SetCinematic`.
- No animator-side crossfade on weapon hold changes: the solver's output filter already blends them (see the table);
  adding another would change the feel of every swap for no visible gain.
- `QaRunner.ApplyPose` still writes `Cam.fieldOfView` directly (QA folder, not owned here). It is harmless, since the
  cinematic path converges to the same number well inside the settle time, but routing it through a public
  `PlayerCamera` setter would be tidier.
- `Game.ResetStatics` was not edited: this phase adds no mutable static state (`CameraOptics` is constants and pure
  functions).

## 6. What the owner must look at in Unity

The before captures must come from the baseline build, the after captures from this one, at the same Game view
resolution (1920x1080), then Neon Syndicate > QA > Compare Golden Shots.

1. **Golden shots must not change.** All 14 views at 12:00 and 22:00. The physical camera reproduces the old
   projection exactly, so the heatmaps should be flat apart from Phase 1 to 4 changes. Any framing difference
   (objects larger, smaller or shifted at the edges) in `core_street`, `strip_street` or `city_aerial` means the
   gate fit or focal length conversion is wrong: report it with the compare report.
2. **Ultrawide check, if a 21:9 monitor is available** (or set the Game view to 21:9): stand still in `core_street`
   framing at 12:00 and compare the height of a building edge with the 16:9 view. Buildings must be the same height on
   screen; only more street is visible at the sides.
3. **Field of view in play.** In Free Roam at 12:00 on Veyra Avenue: walk, sprint, aim down sights with a pistol and
   a rifle, scope a sniper and scroll its zoom, ride a bike, drive a car fast and brake, Surge, glide and fly. Every
   zoom should feel exactly as before. If any view looks wider or tighter than the baseline, say which.
4. **Title flyover sway.** Watch the title screen for a minute: a slow, soft float on top of the orbit, never a wobble.
   Turn Camera Shake off in Settings and confirm it stops.
5. **Pause drift.** Pause in Free Roam at 22:00 in `strip_street` with the character in frame; watch for 15 s. The
   world behind the panel should drift very slowly after about a second. Unpause: the camera must be exactly where it
   was and the next frames must feel like a plain unpause (a tiny release is expected and accepted; report it if it
   reads as a jolt). Repeat while aiming down sights and while in a car.
6. **Creature cancel fade.** In Free Roam, fight a quartz drake or neon jackal: interrupt its attacks with a heavy hit
   that staggers it, and watch the wind-up pose melt into the stagger instead of popping. Hit frames and stagger
   timing are untouched; if any attack lands later or earlier than in the baseline, report it.
7. **Glow fades.** The wind-up flare on humanoid enemies and creatures still lights instantly; at the hit it should
   fade out over a tenth of a second instead of switching off.
8. **Perf flythrough** before and after (Neon Syndicate > QA > Perf flythrough) at 21:00; the summaries should match
   within noise, since this phase adds no GPU work.
