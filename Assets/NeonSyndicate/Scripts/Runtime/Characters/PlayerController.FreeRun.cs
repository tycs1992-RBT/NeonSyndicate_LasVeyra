using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Free-running across the gothic city (the numbers live in FreeRunRules, the poses in HumanoidPose.FreeRun.cs):
    /// climbing tall walls and stone faces by hand (Jump facing a wall that is too tall to mantle), catching and hanging
    /// from ledges in mid-air, mantling onto roofs through the ordinary climb, wall jumps, sprint vaults, short wall runs,
    /// running leaps off edges, a landing roll out of long drops and a head-first high dive that lands in the canal or
    /// in a heroic three-point landing. This file holds the state machine, the climb and the hang; the air moves are in
    /// PlayerController.FreeRun.Air.cs. Everything moves through the one CharacterController, so walls, roofs and the
    /// world's colliders keep their authority; every state falls back to ordinary movement when its surface is gone.
    /// Hooks: PlayerController.Travel.cs calls UpdateFreeRun from UpdateTravelModes, FreeRunReset from TravelReset,
    /// FreeRunPresentation from UpdateTravelPresentation and FreeRunRollOnTouchdown from TravelTouchdown.
    /// </summary>
    public sealed partial class PlayerController
    {
        /// <summary>The free-running or hero move in progress (None for ordinary movement).</summary>
        public FreeRunState FreeRunState { get { return _fr; } }
        /// <summary>A free-running move owns Malik's movement this frame.</summary>
        public bool FreeRunning { get { return _fr != FreeRunState.None; } }
        /// <summary>Seconds in the current free-running state.</summary>
        public float FreeRunTime { get { return _frTime; } }
        /// <summary>The wall normal (pointing away from the wall) of the climb, hang or wall run in progress.</summary>
        public Vector3 FreeRunWallNormal { get { return _wallN; } }
        /// <summary>Which side the wall run's wall is on: -1 left, 1 right.</summary>
        public float WallRunSide { get { return _frSide; } }
        /// <summary>Height above the ground during a dive (for the camera and the flip into the landing).</summary>
        public float DiveHeight { get { return _diveHeight; } }
        /// <summary>The dive will end in a dive slam (powers layer).</summary>
        public bool SlamArmed { get { return _slamArmed; } }

        private FreeRunState _fr;
        private float _frTime;
        private Vector3 _wallN = Vector3.back;
        private Vector3 _wallPt;
        private float _frSide = 1f;
        private float _climbBurst;
        private float _climbUpSmooth;
        private float _climbSideSmooth;
        private float _climbPhase;
        private float _hangAway;
        private float _wrSpeed;
        private float _wrPhase;
        private Vector3 _rollDir = Vector3.forward;
        private float _rollSpeed;
        private float _crouchTapAt = -99f;
        private float _lastWallJumpAt = -99f;
        private Vector3 _lastWallN;
        private float _diveHeight;
        private bool _slamArmed;
        private bool _handsBusy;
        private float _vaultProbeTimer;
        private readonly DoubleTap _crouchTaps = new DoubleTap();

        private static int ClimbMask { get { return Layers.WorldMask; } }
        private static int LandMask { get { return Layers.GroundMask | (1 << Layers.VehicleShell); } }

        // ================================================================== hooks used by PlayerController.Travel.cs

        /// <summary>
        /// Runs the free-running state in progress, or looks for a move to start from ordinary movement (climbs, ledge
        /// grabs, wall runs, running leaps, rolls, dives). True means a move owned the frame; false means the ordinary
        /// on-foot movement runs.
        /// </summary>
        private bool UpdateFreeRun(float dt, bool canControl)
        {
            switch (_fr)
            {
                case FreeRunState.WallClimb: UpdateWallClimb(dt, canControl); return true;
                case FreeRunState.LedgeHang: UpdateLedgeHang(dt, canControl); return true;
                case FreeRunState.WallRun: UpdateWallRun(dt, canControl); return true;
                case FreeRunState.LandRoll: UpdateLandRoll(dt, canControl); return true;
                case FreeRunState.Dive: UpdateDive(dt, canControl); return true;
                case FreeRunState.Takedown: UpdateTakedown(dt, canControl); return true;
                case FreeRunState.Grab: UpdateGrab(dt, canControl); return true;
                case FreeRunState.Beam: UpdateBeam(dt, canControl); return true;
                case FreeRunState.RushStrike: UpdateRushStrike(dt, canControl); return true;
                default:
                    if (_fr != FreeRunState.None)
                    {
                        // A state this build does not run (the stealth and power layers add theirs above).
                        FreeRunEnd();
                    }
                    break;
            }
            if (!canControl || _climbing || _enter != EnterPhase.None || InVehicle || IsRagdolled || IsDead) return false;
            if (Game.Cam != null && Game.Cam.InCinematic) return false;
            if (Combat.State != PlayerCombat.Phase.Free || Combat.Guarding || _leapCharging) return false;
            if (!_grounded) return FreeRunAirTriggers(dt, canControl);
            return FreeRunGroundTriggers(dt, canControl);
        }

        /// <summary>Clears every free-running state (teleports, ragdolls, vehicles, climbs). Called from TravelReset.</summary>
        private void FreeRunReset()
        {
            _fr = FreeRunState.None;
            _frTime = 0f;
            _climbBurst = 0f;
            _hangAway = 0f;
            _slamArmed = false;
            _crouchTaps.Clear();
            ReleaseHeld();
            SetHandsBusy(false);
        }

        /// <summary>Ends the current move and goes back to ordinary movement (the pose eases out through the travel presentation).</summary>
        private void FreeRunEnd()
        {
            _fr = FreeRunState.None;
            _frTime = 0f;
            _climbBurst = 0f;
            _hangAway = 0f;
            SetHandsBusy(false);
        }

        /// <summary>Hands-only moves holster the held weapon (the pose solver's carry would otherwise put the hands on it).</summary>
        private void SetHandsBusy(bool busy)
        {
            if (busy == _handsBusy) return;
            _handsBusy = busy;
            if (Weapons != null && !IsDead) Weapons.SetHidden(busy || InVehicle);
        }

        // ================================================================== triggers

        private bool FreeRunGroundTriggers(float dt, bool canControl)
        {
            // The use button on a person who has not noticed Malik: the silent takedown (PlayerController.Stealth.cs);
            // on one who has, or crouched beside a light empty vehicle: the super-strength grab (PlayerController.Grab.cs).
            if (Act.Hands && (TryStartTakedown() || TryStartGrab())) return true;
            bool jump = Act.Jump;
            Vector3 flat = _vel;
            float speed = flat.magnitude;
            // A sprint carries Malik over low obstacles and up low ledges by itself (one short probe per frame while sprinting).
            if (Sprinting && speed > 5f && !Crouched && !Aiming && _landTimer <= 0f)
            {
                _vaultProbeTimer -= dt;
                if (_vaultProbeTimer <= 0f)
                {
                    _vaultProbeTimer = 0.06f;
                    if (LowObstacleAhead() && TryClimb()) return true;
                }
            }
            if (!jump || Crouched || _landTimer > 0f) return false;
            // A sprint jump off an edge is a running leap.
            if (FreeRunRules.EdgeLeapWanted(Sprinting, speed, GroundAhead(FreeRunRules.EdgeProbeAhead)))
            {
                _jumpTaps.Press(Time.time);
                float vy;
                _vel = FreeRunRules.EdgeLeapVelocity(flat, speed, out vy);
                _vy = vy;
                _grounded = false;
                _airPeakSpeed = 0f;
                _yaw = MathUtil.YawOf(_vel);
                transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
                if (Game.Audio != null) Game.Audio.PlayVar(SfxId.Jump, transform.position, 0.45f, 0.1f, 15f);
                TravelSound(TravelSounds.Clip.Whoosh, 0.3f, 1.25f);
                FreeRunAirStep(dt);
                return true;
            }
            // Facing a wall: a mantle when the top is in reach (the ordinary climb), else a hand-over-hand climb up it.
            if (TryClimb()) return true;
            if (TryStartWallClimb()) return true;
            return false;
        }

        private bool FreeRunAirTriggers(float dt, bool canControl)
        {
            // The use button above a person: the air takedown (PlayerController.Stealth.cs).
            if (Act.Hands && TryStartTakedown()) return true;
            bool jump = Act.Jump;
            if (Act.Crouch)
            {
                // Crouch in mid-air: a high dive over a real drop, otherwise the roll timing for the landing.
                if (TryStartDive()) return true;
                _crouchTapAt = Time.time;
            }
            if (jump)
            {
                if (TryClimb()) return true;
                if (TryStartWallClimb()) return true;
            }
            if (_vy < 1f && TryGrabLedge()) return true;
            if (TryStartWallRun(dt, canControl)) return true;
            return TryLandingRoll(dt);
        }

        // ================================================================== probes

        /// <summary>Something between knee and chest height is right in front (a fence, a bench, a car, a low ledge).</summary>
        private bool LowObstacleAhead()
        {
            int mask = Layers.WorldMask | (1 << Layers.Debris) | (1 << Layers.VehicleShell);
            Vector3 fwd = transform.forward;
            Vector3 feet = transform.position;
            RaycastHit hit;
            if (!Physics.Raycast(feet + Vector3.up * 0.7f, fwd, out hit, 0.95f, mask, QueryTriggerInteraction.Ignore) || hit.normal.y > 0.5f) return false;
            // Nothing at head height: it is low enough to vault or step onto.
            return !Physics.Raycast(feet + Vector3.up * 1.75f, fwd, 1.1f, mask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Ground within a short drop this far ahead of the feet.</summary>
        private bool GroundAhead(float ahead)
        {
            Vector3 fwd = _vel.sqrMagnitude > 1f ? _vel.normalized : transform.forward;
            Vector3 from = transform.position + fwd * ahead + Vector3.up * 0.6f;
            return Physics.Raycast(from, Vector3.down, FreeRunRules.EdgeDropMin + 0.6f, LandMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Distance from the feet down to the ground (a large number over a void).</summary>
        private float DropBelow()
        {
            RaycastHit hit;
            Vector3 from = transform.position + Vector3.up * 0.3f;
            if (Physics.Raycast(from, Vector3.down, out hit, 120f, LandMask, QueryTriggerInteraction.Ignore)) return Mathf.Max(0f, hit.distance - 0.3f);
            return 120f;
        }

        /// <summary>A climbable wall ahead at chest and head height (too tall for the ordinary mantle).</summary>
        private bool TallWallAhead(out RaycastHit chest)
        {
            Vector3 fwd = transform.forward;
            Vector3 feet = transform.position;
            if (!Physics.Raycast(feet + Vector3.up * 1f, fwd, out chest, FreeRunRules.ClimbReach, ClimbMask, QueryTriggerInteraction.Ignore)) return false;
            if (!FreeRunRules.WallClimbable(chest.normal.y)) return false;
            RaycastHit head;
            if (!Physics.Raycast(feet + Vector3.up * 1.9f, fwd, out head, FreeRunRules.ClimbReach + 0.35f, ClimbMask, QueryTriggerInteraction.Ignore)) return false;
            return FreeRunRules.WallClimbable(head.normal.y);
        }

        /// <summary>The roof or top edge above a wall point: a walkable surface found from above, just inside the wall plane.</summary>
        private bool TopAbove(Vector3 wallPoint, Vector3 wallNormal, float maxAbove, float minAbove, out RaycastHit top)
        {
            Vector3 over = wallPoint - wallNormal * 0.35f;
            over.y = transform.position.y + maxAbove + 0.4f;
            if (!Physics.Raycast(over, Vector3.down, out top, maxAbove + 0.4f - minAbove, ClimbMask, QueryTriggerInteraction.Ignore)) return false;
            return top.normal.y > 0.7f;
        }

        /// <summary>A standing body fits at a point (for mantle and hang destinations).</summary>
        private bool BodyFits(Vector3 feet)
        {
            return !Physics.CheckCapsule(feet + Vector3.up * 0.45f, feet + Vector3.up * 1.5f, 0.28f, ClimbMask, QueryTriggerInteraction.Ignore);
        }

        // ================================================================== mantle (hands the finish to the ordinary climb)

        /// <summary>Climbs onto a top edge through the ordinary climb interpolation (UpdateClimb), from a climb or a hang.</summary>
        private void BeginMantle(Vector3 dest, float topY)
        {
            _climbFrom = transform.position;
            _climbTo = dest;
            _climbTop = topY;
            _climbT = 0f;
            _climbDuration = FreeRunRules.MantleSeconds;
            _climbing = true;
            Controller.enabled = false;
            _vel = Vector3.zero;
            _vy = 0f;
            _airPeakSpeed = 0f;
            FreeRunEnd();
            if (Anim != null)
            {
                Anim.Travel = TravelPose.None;
                Anim.TravelBlend = 0f;
            }
            _pose = TravelPose.None;
            _poseBlend = 0f;
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.Jump, transform.position, 0.3f, 0.1f, 12f);
        }

        /// <summary>Tries to mantle the top of the wall being climbed or hung from.</summary>
        private bool TryMantleFromWall()
        {
            RaycastHit top;
            Vector3 probe = transform.position - _wallN * 0.2f + Vector3.up * 1f;
            if (!TopAbove(probe, _wallN, FreeRunRules.MantleMaxHeight, FreeRunRules.MantleMinHeight, out top)) return false;
            float h = top.point.y - transform.position.y;
            if (!FreeRunRules.CanMantle(h)) return false;
            Vector3 dest = top.point - _wallN * FreeRunRules.MantleInset + Vector3.up * 0.03f;
            dest.y = top.point.y + 0.03f;
            if (!BodyFits(dest)) return false;
            BeginMantle(dest, top.point.y);
            return true;
        }

        // ================================================================== wall climb

        private bool TryStartWallClimb()
        {
            if (Aiming || Crouched) return false;
            RaycastHit chest;
            if (!TallWallAhead(out chest)) return false;
            Vector3 n = MathUtil.Flat(chest.normal);
            if (n.sqrMagnitude < 1e-4f) return false;
            _wallN = n.normalized;
            _wallPt = chest.point;
            _fr = FreeRunState.WallClimb;
            _frTime = 0f;
            // The first grab is a hop onto the wall (a full one from the ground, a short one caught in the air).
            _climbBurst = FreeRunRules.ClimbBurstSeconds * (_grounded ? 1f : 0.5f);
            _climbUpSmooth = 0f;
            _climbSideSmooth = 0f;
            _climbPhase = 0f;
            _grounded = false;
            _airPeakSpeed = 0f;
            _vel = Vector3.zero;
            _vy = 0f;
            Crouched = false;
            Aiming = false;
            AimingDownSights = false;
            Sprinting = false;
            Combat.ReleaseLock();
            _yaw = MathUtil.YawOf(-_wallN);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            SetHandsBusy(true);
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.Jump, transform.position, 0.3f, 0.1f, 12f);
            return true;
        }

        private void UpdateWallClimb(float dt, bool canControl)
        {
            _frTime += dt;
            float stick;
            Vector3 wish = TravelWish(canControl, out stick);
            Combat.Tick(dt, false, wish);
            Aiming = false;
            AimingDownSights = false;
            Sprinting = false;
            Crouched = false;
            Vector3 pos = transform.position;
            // The wall under the hands this frame.
            RaycastHit wall;
            bool haveWall = Physics.Raycast(pos + Vector3.up * 1f, -_wallN, out wall, 1.4f, ClimbMask, QueryTriggerInteraction.Ignore) && FreeRunRules.WallClimbable(wall.normal.y);
            if (haveWall)
            {
                Vector3 n = MathUtil.Flat(wall.normal);
                if (n.sqrMagnitude > 1e-4f) _wallN = n.normalized;
                _wallPt = wall.point;
            }
            else
            {
                // The wall ended at the chest: the top is right there, else there is nothing to hold.
                if (TryMantleFromWall()) return;
                LetGoOfWall(0.6f);
                return;
            }
            Vector3 tangent = MathUtil.RightOf(-_wallN);
            float intoWall = Vector3.Dot(wish, -_wallN);
            float side = stick > 0.15f ? Mathf.Clamp(Vector3.Dot(wish, tangent), -1f, 1f) : 0f;
            float up = stick > 0.15f ? FreeRunRules.ClimbInput(intoWall, canControl ? Act.StickForward : 0f) : 0f;
            if (canControl && Act.Hands && TryStartTakedown()) return;
            if (canControl && Act.Crouch)
            {
                LetGoOfWall(0.8f);
                return;
            }
            if (canControl && Act.Jump)
            {
                if (up > 0.3f)
                {
                    _climbBurst = FreeRunRules.ClimbBurstSeconds;
                    TravelSound(TravelSounds.Clip.Whoosh, 0.25f, 1.4f);
                }
                else
                {
                    WallJumpOff(tangent, side, false, 0f);
                    return;
                }
            }
            // Sideways only where the wall goes on.
            if (Mathf.Abs(side) > 0.05f)
            {
                Vector3 beside = pos + tangent * (Mathf.Sign(side) * 0.5f) + Vector3.up * 1f;
                if (!Physics.Raycast(beside, -_wallN, 1.4f, ClimbMask, QueryTriggerInteraction.Ignore)) side = 0f;
            }
            float burst = _climbBurst > 0f ? 1f : 0f;
            if (_climbBurst > 0f) _climbBurst -= dt;
            // Up: onto the roof when the top is in reach of the hands.
            if ((up > 0.2f || burst > 0f) && TryMantleFromWall()) return;
            Vector3 step = FreeRunRules.ClimbStep(up, side, burst, _wallN, tangent, dt);
            Vector3 before = pos;
            CollisionFlags flags = Controller.Move(step + _external * dt);
            _external = Vector3.MoveTowards(_external, Vector3.zero, dt * 12f);
            Vector3 moved = transform.position - before;
            // Climbing down onto the ground ends the climb on the feet.
            if (up < -0.1f && ((flags & CollisionFlags.Below) != 0 || Controller.isGrounded || GroundProbe()))
            {
                FreeRunEnd();
                _grounded = true;
                _vy = -2f;
                _vel = Vector3.zero;
                SetFreeRunGroundAnim();
                return;
            }
            _yaw = Mathf.MoveTowardsAngle(_yaw, MathUtil.YawOf(-_wallN), 720f * dt);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            _climbUpSmooth = Mathf.Lerp(_climbUpSmooth, Mathf.Clamp(up + burst, -1f, 1f), MathUtil.Damp(10f, dt));
            _climbSideSmooth = Mathf.Lerp(_climbSideSmooth, side, MathUtil.Damp(10f, dt));
            _climbPhase += (Mathf.Abs(moved.y) + Mathf.Abs(Vector3.Dot(moved, tangent))) * 0.9f;
            if (transform.position.y < WorldBounds.KillY) Respawn(Game.Root != null ? Game.Root.SafeRespawnPoint(transform.position) : Vector3.up * 2f, _yaw);
            _vel = Vector3.zero;
            _vy = 0f;
            Anim.Velocity = Vector3.zero;
            Anim.VerticalVelocity = 0f;
            Anim.Grounded = false;
            Anim.CrouchWeight = 0f;
            Anim.SitWeight = 0f;
            Anim.Hold = HoldType.None;
            Anim.Aiming = false;
        }

        /// <summary>Drops off the wall or ledge into an ordinary fall, nudged a little away from it.</summary>
        private void LetGoOfWall(float push)
        {
            FreeRunEnd();
            _vel = _wallN * push;
            _vy = 0f;
            _grounded = false;
            _airPeakSpeed = 0f;
        }

        /// <summary>Jumps off a wall (from a climb, a hang or a wall run) into an ordinary jump the air code continues.</summary>
        private void WallJumpOff(Vector3 tangent, float side, bool fromRun, float runSpeed)
        {
            float vy;
            Vector3 v = FreeRunRules.WallJumpVelocity(_wallN, tangent, side, fromRun, runSpeed, out vy);
            float stickIgnored;
            Vector3 wish = TravelWish(true, out stickIgnored);
            float away = Mathf.Max(0f, Vector3.Dot(wish, _wallN));
            v += _wallN * (away * 2.5f);
            FreeRunEnd();
            _vel = v;
            _vy = vy;
            _grounded = false;
            _airPeakSpeed = 0f;
            _lastWallJumpAt = Time.time;
            _lastWallN = _wallN;
            _jumpTaps.Clear();
            _yaw = MathUtil.YawOf(v.sqrMagnitude > 0.1f ? v : _wallN);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.Jump, transform.position, 0.4f, 0.1f, 15f);
            TravelSound(TravelSounds.Clip.Whoosh, 0.3f, 1.2f);
        }

        private void SetFreeRunGroundAnim()
        {
            Anim.Velocity = _vel;
            Anim.VerticalVelocity = _vy;
            Anim.Grounded = _grounded;
            Anim.CrouchWeight = 0f;
            Anim.SitWeight = 0f;
            Anim.Hold = Weapons != null ? Weapons.Current.Hold : HoldType.None;
        }

        // ================================================================== ledge hang

        /// <summary>Falling past a ledge while moving toward its wall: the hands catch it.</summary>
        private bool TryGrabLedge()
        {
            Vector3 flat = _vel;
            float stick;
            Vector3 wish = TravelWish(true, out stick);
            Vector3 dir = flat.sqrMagnitude > 1f ? flat.normalized : (stick > 0.2f ? wish.normalized : transform.forward);
            Vector3 pos = transform.position;
            RaycastHit wall;
            if (!Physics.Raycast(pos + Vector3.up * 1.2f, dir, out wall, 1.05f, ClimbMask, QueryTriggerInteraction.Ignore)) return false;
            if (!FreeRunRules.WallClimbable(wall.normal.y)) return false;
            Vector3 n = MathUtil.Flat(wall.normal);
            if (n.sqrMagnitude < 1e-4f) return false;
            n.Normalize();
            float toward = Mathf.Max(Vector3.Dot(flat, -n) * 0.25f, Vector3.Dot(wish, -n));
            RaycastHit top;
            if (!TopAbove(wall.point, n, FreeRunRules.GrabMaxHeight, FreeRunRules.GrabMinHeight, out top)) return false;
            float h = top.point.y - pos.y;
            if (!FreeRunRules.LedgeGrabbable(h, _vy, toward)) return false;
            Vector3 ledge = new Vector3(wall.point.x, top.point.y, wall.point.z);
            Vector3 root = FreeRunRules.HangRoot(ledge, n, Controller.radius);
            if (!BodyFits(root)) return false;
            StartLedgeHang(ledge, n, root);
            return true;
        }

        private void StartLedgeHang(Vector3 ledge, Vector3 n, Vector3 root)
        {
            _fr = FreeRunState.LedgeHang;
            _frTime = 0f;
            _wallN = n;
            _wallPt = ledge;
            _hangAway = 0f;
            _climbSideSmooth = 0f;
            _vel = Vector3.zero;
            _vy = 0f;
            _grounded = false;
            _airPeakSpeed = 0f;
            Crouched = false;
            Aiming = false;
            AimingDownSights = false;
            Sprinting = false;
            Combat.ReleaseLock();
            Controller.Move(root - transform.position);
            _yaw = MathUtil.YawOf(-_wallN);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            SetHandsBusy(true);
            TravelSound(TravelSounds.Clip.LandSoft, 0.35f, 1.3f);
            Pad.Rumble(0.25f, 0.1f, 0.08f);
        }

        private void UpdateLedgeHang(float dt, bool canControl)
        {
            _frTime += dt;
            float stick;
            Vector3 wish = TravelWish(canControl, out stick);
            Combat.Tick(dt, false, wish);
            Aiming = false;
            AimingDownSights = false;
            Sprinting = false;
            Crouched = false;
            Vector3 tangent = MathUtil.RightOf(-_wallN);
            float intoWall = Vector3.Dot(wish, -_wallN);
            float up = stick > 0.15f ? FreeRunRules.ClimbInput(intoWall, canControl ? Act.StickForward : 0f) : 0f;
            float side = stick > 0.3f ? Mathf.Clamp(Vector3.Dot(wish, tangent), -1f, 1f) : 0f;
            if (canControl && Act.Hands && TryStartTakedown()) return;
            if (canControl && Act.Crouch)
            {
                LetGoOfWall(0.8f);
                return;
            }
            if (canControl && Act.Jump)
            {
                if (TryMantleFromWall()) return;
                WallJumpOff(tangent, side, false, 0f);
                return;
            }
            if (up > 0.5f && _frTime > 0.15f && TryMantleFromWall()) return;
            if (up < -0.5f) _hangAway += dt;
            else _hangAway = 0f;
            if (_hangAway > FreeRunRules.HangLetGoSeconds)
            {
                LetGoOfWall(0.6f);
                return;
            }
            Vector3 pos = transform.position;
            // Shimmy along the ledge while it goes on.
            if (Mathf.Abs(side) > 0.05f)
            {
                Vector3 next = pos + tangent * (Mathf.Sign(side) * 0.55f);
                RaycastHit wall;
                bool more = Physics.Raycast(next + Vector3.up * 1.2f, -_wallN, out wall, 1.2f, ClimbMask, QueryTriggerInteraction.Ignore) && FreeRunRules.WallClimbable(wall.normal.y);
                RaycastHit top;
                if (more && TopAbove(wall.point, _wallN, FreeRunRules.GrabMaxHeight, FreeRunRules.GrabMinHeight, out top) && Mathf.Abs(top.point.y - _wallPt.y) < 0.25f)
                {
                    _wallPt = new Vector3(wall.point.x, top.point.y, wall.point.z);
                    Vector3 n = MathUtil.Flat(wall.normal);
                    if (n.sqrMagnitude > 1e-4f) _wallN = n.normalized;
                }
                else side = 0f;
            }
            // Stay hung under the ledge: the feet HangDrop below it, the body a little off the wall.
            Vector3 along = pos - _wallPt;
            along.y = 0f;
            along -= _wallN * Vector3.Dot(along, _wallN);
            Vector3 target = FreeRunRules.HangRoot(_wallPt + along, _wallN, Controller.radius) + tangent * (side * FreeRunRules.HangShimmySpeed * dt);
            Controller.Move(target - pos);
            _climbSideSmooth = Mathf.Lerp(_climbSideSmooth, side, MathUtil.Damp(8f, dt));
            _yaw = Mathf.MoveTowardsAngle(_yaw, MathUtil.YawOf(-_wallN), 720f * dt);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            _vel = Vector3.zero;
            _vy = 0f;
            Anim.Velocity = Vector3.zero;
            Anim.VerticalVelocity = 0f;
            Anim.Grounded = false;
            Anim.CrouchWeight = 0f;
            Anim.SitWeight = 0f;
            Anim.Hold = HoldType.None;
            Anim.Aiming = false;
        }
    }
}
