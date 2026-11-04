# H8S animation mapping

Status: source integration and offline tests. Native Unity playback, feel and foot contact are NOT RUN. Owner approval
is NOT GIVEN. Everything here runs only inside explicit Editor reviews; the ordinary game still uses the original
procedural actor and its original procedural landing dip.

## The data

`Assets/NeonSyndicate/Art/ProductionH8S/Animations/MalikFullBodyH8S.json`, schema 2, candidate id
`MALIK-H8R-REVIEW-01` (the owner-selected human, same id as H8R), source
`ArtSource/AscensionH8S/MalikVeyrH8S.blend` (sha256 `0180aa40...e969`), 17 clips, 701 samples, 30 samples per second,
no root motion, no events.

Each sample stores 17 canonical joints in actor-local space (positions, Blender axes reflected to `(-x, z, -y)`, so +z is
forward) and world rotation deltas from the bind pose. At runtime `MalikMotionH8.Retarget` keeps the joint directions
and rescales every segment to the actor's own bone lengths, so the same data fits Malik and a different body (Dante).

| Index | Joint | RigLayout bone |
|---|---|---|
| 0 | hips | Hips |
| 1 | spine | Spine |
| 2 | chest | Chest |
| 3 | neck | Neck |
| 4 | head | Head |
| 5, 6, 7 | left upper arm, forearm, hand | UpperArmL, ForearmL, HandL |
| 8, 9, 10 | right upper arm, forearm, hand | UpperArmR, ForearmR, HandR |
| 11, 12, 13 | left thigh, shin, foot | ThighL, ShinL, FootL |
| 14, 15, 16 | right thigh, shin, foot | ThighR, ShinR, FootR |

The 30 finger bones of the 47-bone gameplay rig are never written by authored data: the original grip owner keeps
them. The imported visual's 53 source bones follow the 47 gameplay bones through the original adapter (`ImportedVisual04OD`)
and its declared passive descendants.

## Schema 2 contract (`MalikMotionH8.ValidCurated`)

Schema 1 files (H7, H8, H8R) are validated exactly as before. A schema 2 file is accepted only when all of these hold,
otherwise the review refuses and the original pose stays:

- candidate id `MALIK-H8R-REVIEW-01`, the fixed space string, no root motion, no events, a 64-hex source hash;
- exactly these 17 clips in this order: H7_IdleBreath, H7_Walk, H7_Run, H7_SwordReady, H7_OathCut, H8_AegisLink,
  H8_CrystalCast, H8_Dodge, H8_FireAim, H8_FireRecoil, H8_FlightLanding, H8_FlightTakeoff, H8_Guard_Brace,
  H8_JumpLanding, H8_Landing_Supplied, H8_Listening_Supplied, H8_SurgeRise;
- loop flags true only for the first four, H8_FireAim and H8_Listening_Supplied;
- uniform 30 Hz tracks: sample count = round(duration x 30) + 1, sample k at k/30 s (within 2 ms), complete 17-joint
  frames;
- continuity: no joint moves more than 0.30 m or turns more than 50 degrees between two samples (delivered maxima
  0.25 m and 44.8 degrees, both in the skipped airborne lead-in of the supplied landing). The inherited H8R guard
  brace keeps its reviewed limits of 0.35 m and 100 degrees: it snaps 92.8 degrees and 0.29 m between its third and
  fourth samples while the guard weight is still ramping in. That is a recorded source defect to repair in the next
  export, not hidden;
- the bind is the selected adult skeleton: hips between 0.9 and 1.2 m, head above the hips, mirrored limbs within
  1 cm;
- the supplied landing lasts 1.2667 s and the listening loop 2.5 s (each plus or minus 0.01 s);
- the shared bounds: finite values, unit quaternions, bounded hips, closed loops, strictly increasing time.

The Editor review also refuses data whose `sourceSha256` no longer matches the Blender source on disk. That check
now uses `ProjectFileHashH8S`: the original receipt hash only reads `Assets/` paths, so the H8, H8R and H8S full-body
reviews could never match their `ArtSource/` Blender hash in Unity (found by the Dante review builder, fixed here). Quaternion
interpolation uses `QMath.Slerp`, which takes the short arc when consecutive samples sit in opposite hemispheres (19 such
pairs exist in the delivery and are harmless); a test checks every midpoint stays between its two samples.

Missing data falls back, never half-plays: schema 1 data has no landing or listening clip, so the H8R review keeps the
original landing and refuses the quiet loop; a missing landing clip falls back to the other landing, and with both
missing the original procedural dip remains.

## Which clip plays when (player review)

Selection stays in `MalikMotionH8.Select` and runs inside the original solver before feet, smoothing, reach limits,
weapon IK and the controller. Unchanged from H8R except the landing, which runs first:

| Situation | Clip | Phase source | What stays original |
|---|---|---|---|
| Touchdown after at least 0.35 s of clean air time, shorter than 0.85 s | H8_JumpLanding (the reviewed H8 dip, now played for the first time) | seconds since touchdown | controller, feet, collision, camera, held weapon grip |
| Touchdown after 0.85 s or more of clean air time | H8_Landing_Supplied from 0.40 s (its touchdown frame) at 1.15x | seconds since touchdown | as above |
| Standing, no weapon or a sword at rest | H7_IdleBreath | clock | |
| Walking or running forward | H7_Walk, blended into H7_Run above 3 m/s | original gait phase | cadence, direction-aware IK |
| Sword at the ready | H7_SwordReady | clock | |
| Right sword cut | H7_OathCut | original attack windows | blade aim, support hand, fingers |
| Dodge | H8_Dodge | original dodge timer | |
| Guardian cooperation | H8_AegisLink | original synergy timer | right arm, legs, pelvis |
| Aiming a gun | H8_FireAim at 0.45 weight | clock | both arms, muzzle, legs, pelvis |
| Gun recoil | H8_FireRecoil | restarts per shot | gun owner, effects, ammo, hits |
| Casting | H8_CrystalCast | held mid-cast | right arm, legs, pelvis |
| Guard | H8_Guard_Brace | guard weight | |

Still source-only (no gameplay playback): H8_FlightLanding, H8_FlightTakeoff and H8_SurgeRise. The listening loop is
never used on the player.

## Facing of the supplied clips

The two supplied retargets stand turned about the vertical axis: measured from the hip line, the listening loop faces
13.3 degrees off forward and the landing 13 to 21 degrees, while every clip authored on Malik's own rig faces exactly
forward. `MalikMotionH8.FacingCorrection` measures each clip's mean hip-line yaw once and `TurnSample` removes it from
every sampled position and rotation, so both play facing where the controller faces. The inherited guard brace also
turns its hips about 12 degrees; it is left as reviewed (it reads as a bladed fighting stance) and noted for review.

## The landing (`LandingTriggerH8S`, `HumanoidPose.LandingH8S`)

- Starts only on the grounded edge that follows at least 0.35 s in the air. Anything unsafe during the flight (combat
  ready stance, travel power, seat, ride, cover, combat move, guard, dodge, reload, aim, cast, carry, or a review block
  such as death, ragdoll, get-up or a gesture) cancels the landing for that flight. A teleport (respawn, restart, fast
  travel) forgets the flight.
- Blends in over 0.08 s and out over the last 0.25 s; depth grows with air time (0.45 at 0.35 s to 1.0 from about
  0.96 s); weight falls by up to 70 percent while the player keeps moving, so a running landing stays mostly the
  original gait.
- Cancels immediately when an unsafe state appears, and never resumes; taking off again cancels it.
- The supplied heavy action has no root translation (the export zeroed it), so its knees bend while its hips stay put.
  The landing lowers the pelvis until the lower foot is back at rest height (capped at 0.46 m at the actor's scale), which
  turns the bend into a real crouch with the feet on the ground.
- Because there is no root translation, the touchdown frame cannot be read from the data. The default 0.40 s (knees
  near 75 degrees) reads the clip as a deep absorbing crouch and stand-up: offline, a 1 s fall dips the hips about
  0.21 m and recovers within about 1 s. Around 0.80 s (legs nearly straight) it reads as a light landing (about 0.05 m).
  The independent reviewer argued for the later frame; it needs a visual judgement, so the H8S review panel has a
  touchdown slider from 0.30 to 0.85 s.
- Free hands take the authored balance arms; the heavy landing hands them back from 55 to 85 percent of its length
  because the end of the supplied action drifts toward folded arms. A carried gun or blade keeps its original grip: the
  whole arm moves rigidly with its shoulder and the chest turn.
- Switch and touchdown: H8S review panel. Both are kept by the animator and pushed every frame, so a rebuilt pose keeps
  them.

## The quiet listening loop (`QuietMotionH8S`, `HumanoidPose.QuietH8S`, `HumanoidAnimator.QuietH8S`)

- Eligible actor: only the exact live canonical Dante Rook (`Game.Campaign.CastActor(CastIds04E.Dante)`), checked when
  the review starts and every frame. No substitute is spawned; dialogue, Forge service, schedule, navigation, faction and
  damage keep their owners.
- Fades out completely (in about 0.17 s) for walking faster than 0.25 m/s, air, seats, rides, travel, cover, guard, ready
  stance, aim, any combat move, reload, cast, carry or panic, and for original gestures (greeting, inspect, point, hands
  up, cower, phone), get-up, freeze and directed dialogue performance. Fades back in over about 0.6 s.
- Head-look takes the head and neck back over about 0.2 s (eased, not switched); the loop keeps the spine, chest and
  arms meanwhile.
- A held tool or weapon keeps both arms original (Dante holds his hammer in the left hand, so the review uses torso only
  while it is visible). Free hands fold: the authored arm shape is carried into the actor's own hips frame, placed on his
  shoulders, and each wrist is kept at least 0.13 m in front of the chest joint so the folded pose of a different body
  never sinks into his.
- The pose owner revalidates any data it is handed, so a malformed set never reaches the solver.
- Legs, feet and pelvis stay original.
- Review: Neon Syndicate > 04OH > H8S Dante quiet listening review (on the procedural Dante), and automatically inside
  the H8S Dante living cast review (on his imported visual), which ends it first on every exit.

## Tests

`CuratedMotionH8STests` (65 offline cases): the delivered data contract and its rejections (wrong id, order, loop
flags, sampling, rotation and position jumps, hidden root drift, a foreign skeleton, duration, root motion, events,
lineage), short-arc interpolation and loop closure, facing correction, the landing detector (short hops, small and
heavy landings, unsafe flights including the ready stance, cancellation, teleport, movement, missing clips, bad
timesteps, touchdown range), landings through the real solver (crouch depth, light late touchdown, full recovery,
schema 1 and switch fallbacks, dodge and review-block takeover, guard brace), the quiet loop (eligibility, blend and
yield, eased head-look, held tool, revalidation, refusal without the clip) and the H8S review contract. An independent
review of the first version found nine issues; all are fixed above. Offline reference harness, not native Unity.
