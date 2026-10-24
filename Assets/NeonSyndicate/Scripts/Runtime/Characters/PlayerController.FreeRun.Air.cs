using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The free-running moves in the air (numbers in FreeRunRules): short wall runs along a wall to either side with a
    /// wall jump off them, the landing roll that turns a long drop into forward speed, and the high dive (crouch in
    /// mid-air over a real drop) that steers, flips feet-first at the last moment and lands in the canal with a splash
    /// or on stone in a heroic three-point landing. Also the presentation hook that hands every free-running pose to
    /// the animator (TravelPose values 8 and up, HumanoidPose.FreeRun.cs).
    /// </summary>
    public sealed partial class PlayerController
    {
        // ================================================================== plain air step

        /// <summary>One ordinary airborne frame (gravity, the controller move, the ground check) when a move consumed the frame.</summary>
        private void FreeRunAirStep(float dt)
        {
            _vy -= (MoonJump ? 7f : TravelMath.FallGravity) * dt;
            _airPeakSpeed = MoonJump ? 0f : Mathf.Max(_airPeakSpeed, -_vy);
            Vector3 motion = (_vel + Vector3.up * _vy + _external) * dt;
            _external = Vector3.MoveTowards(_external, Vector3.zero, dt * 12f);
            Controller.Move(motion);
            _grounded = Controller.isGrounded || (_vy <= 0f && GroundProbe());
            if (_grounded)
            {
                _vy = -2f;
                _airPeakSpeed = 0f;
            }
            Anim.Velocity = _vel;
            Anim.VerticalVelocity = _vy;
            Anim.Grounded = _grounded;
            Anim.CrouchWeight = 0f;
            Anim.SitWeight = 0f;
            Anim.Hold = Weapons != null ? Weapons.Current.Hold : HoldType.None;
        }

        // ================================================================== landing roll

        /// <summary>
        /// Falling fast with the ground about to arrive: when Malik is running, or crouch was tapped in time, the frame is
        /// taken over so the touchdown becomes a roll instead of the ordinary hard landing.
        /// </summary>
        private bool TryLandingRoll(float dt)
        {
            if (_vy > -FreeRunRules.RollMinFall) return false;
            float groundSpeed = _vel.magnitude;
            if (!FreeRunRules.WantsRoll(-_vy, groundSpeed, Time.time - _crouchTapAt)) return false;
            float reach = -_vy * dt * 1.6f + 0.35f;
            RaycastHit hit;
            Vector3 origin = transform.position + Vector3.up * (Controller.radius + 0.05f);
            if (!Physics.SphereCast(origin, Controller.radius * 0.9f, Vector3.down, out hit, reach, LandMask, QueryTriggerInteraction.Ignore)) return false;
            if (hit.normal.y < 0.6f) return false;
            float fallSpeed = Mathf.Max(_airPeakSpeed, -_vy);
            FreeRunAirStep(dt);
            if (_grounded) StartLandRoll(fallSpeed, groundSpeed);
            return true;
        }

        /// <summary>A heavy travel landing (leap, glide, flight) while moving rolls out instead of the crouch-and-rise. Called from TravelTouchdown.</summary>
        private bool FreeRunRollOnTouchdown(float fallSpeed, float groundSpeed)
        {
            if (groundSpeed < FreeRunRules.RollMinGroundSpeed || IsDead) return false;
            StartLandRoll(Mathf.Min(fallSpeed, FreeRunRules.RollSafeFall), groundSpeed);
            return true;
        }

        private void StartLandRoll(float fallSpeed, float groundSpeed)
        {
            Vector3 flat = MathUtil.Flat(_vel);
            _rollDir = flat.sqrMagnitude > 0.25f ? flat.normalized : MathUtil.Flat(transform.forward).normalized;
            if (_rollDir.sqrMagnitude < 0.5f) _rollDir = Vector3.forward;
            _rollSpeed = FreeRunRules.RollSpeed(flat.magnitude);
            _fr = FreeRunState.LandRoll;
            _frTime = 0f;
            _grounded = true;
            _vy = -2f;
            _airPeakSpeed = 0f;
            _crouchTapAt = -99f;
            Crouched = false;
            Aiming = false;
            AimingDownSights = false;
            _yaw = MathUtil.YawOf(_rollDir);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            float damage = FreeRunRules.RollDamage(fallSpeed);
            if (damage > 0f) Health.TakeDamage(DamageInfo.Make(damage, transform.position, Vector3.down, null, DamageType.Fall));
            NoteLandingNoise(fallSpeed, true);
            float k = Mathf.InverseLerp(FreeRunRules.RollMinFall, FreeRunRules.RollSafeFall, fallSpeed);
            if (Game.Fx != null) Game.Fx.TravelLanding(transform.position, 0.3f + 0.5f * k);
            TravelSound(k > 0.5f ? TravelSounds.Clip.LandHeavy : TravelSounds.Clip.LandSoft, 0.4f + 0.4f * k, 1.05f);
            if (k > 0.3f) TravelShake(0.08f + 0.15f * k, 0.3f + 0.3f * k, 0.15f, 0.2f);
        }

        private void UpdateLandRoll(float dt, bool canControl)
        {
            _frTime += dt;
            float phase = _frTime / FreeRunRules.RollSeconds;
            float stick;
            Vector3 wish = TravelWish(canControl, out stick);
            Combat.Tick(dt, false, wish);
            Aiming = false;
            AimingDownSights = false;
            Sprinting = false;
            Crouched = false;
            _vel = _rollDir * (_rollSpeed * FreeRunRules.RollMotion(phase));
            _vy = -2f;
            Controller.Move((_vel + Vector3.up * _vy + _external) * dt);
            _external = Vector3.MoveTowards(_external, Vector3.zero, dt * 12f);
            _grounded = Controller.isGrounded || GroundProbe();
            if (!_grounded)
            {
                // Rolled off an edge: back to a plain fall with the roll's speed.
                FreeRunEnd();
                _vy = 0f;
                _airPeakSpeed = 0f;
                SetFreeRunGroundAnim();
                return;
            }
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            // Up on the feet, or out early into a run or a jump once the roll is mostly done.
            bool early = phase > 0.65f && canControl && (Act.Jump || (stick > 0.5f && Vector3.Dot(wish, _rollDir) < 0.2f));
            if (phase >= 1f || early)
            {
                FreeRunEnd();
                _vel = _rollDir * (_rollSpeed * FreeRunRules.RollMotion(1f));
            }
            Anim.Velocity = _vel;
            Anim.VerticalVelocity = _vy;
            Anim.Grounded = true;
            Anim.CrouchWeight = 0f;
            Anim.SitWeight = 0f;
            Anim.Hold = Weapons != null ? Weapons.Current.Hold : HoldType.None;
            Anim.Aiming = false;
        }

        // ================================================================== wall run

        /// <summary>A wall close beside Malik while he runs along it in the air: run along the wall for a moment.</summary>
        private bool TryStartWallRun(float dt, bool canControl)
        {
            Vector3 flat = _vel;
            float speed = flat.magnitude;
            if (speed < FreeRunRules.WallRunMinSpeed || _vy < FreeRunRules.WallRunSink || _vy > 7f) return false;
            float stick;
            Vector3 wish = TravelWish(canControl, out stick);
            Vector3 dir = flat / speed;
            if (stick < 0.3f || Vector3.Dot(wish, dir) < 0.3f) return false;
            Vector3 right = MathUtil.RightOf(dir);
            Vector3 eye = transform.position + Vector3.up * 1f;
            for (int s = 1; s >= -1; s -= 2)
            {
                RaycastHit hit;
                if (!Physics.Raycast(eye, right * s, out hit, FreeRunRules.WallRunSideReach, ClimbMask, QueryTriggerInteraction.Ignore)) continue;
                if (!FreeRunRules.WallClimbable(hit.normal.y)) continue;
                Vector3 n = MathUtil.Flat(hit.normal);
                if (n.sqrMagnitude < 1e-4f) continue;
                n.Normalize();
                float into = Vector3.Dot(dir, n);
                float since = Time.time - _lastWallJumpAt;
                bool same = Vector3.Dot(n, _lastWallN) > 0.9f;
                if (!FreeRunRules.WallRunAllowed(speed, into, _vy, since, same)) continue;
                // The wall must go on a little ahead, or this is a corner post.
                if (!Physics.Raycast(eye + dir * 1.2f, right * s, FreeRunRules.WallRunSideReach + 0.4f, ClimbMask, QueryTriggerInteraction.Ignore)) continue;
                StartWallRun(n, s, flat, speed);
                return true;
            }
            return false;
        }

        private void StartWallRun(Vector3 n, float side, Vector3 flat, float speed)
        {
            _fr = FreeRunState.WallRun;
            _frTime = 0f;
            _wallN = n;
            _frSide = side;
            _wrPhase = 0f;
            Vector3 tangent = FreeRunRules.WallTangent(flat, n);
            _wrSpeed = FreeRunRules.WallRunForward(speed, 0f, 1f);
            _vel = tangent * _wrSpeed;
            _vy = Mathf.Max(_vy, FreeRunRules.WallRunLift);
            _grounded = false;
            _airPeakSpeed = 0f;
            Crouched = false;
            Aiming = false;
            AimingDownSights = false;
            Combat.ReleaseLock();
            _yaw = MathUtil.YawOf(tangent);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            TravelSound(TravelSounds.Clip.Whoosh, 0.3f, 1.3f);
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.Footstep, transform.position, 0.35f, 0.15f, 18f);
        }

        private void UpdateWallRun(float dt, bool canControl)
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
            RaycastHit wall;
            bool haveWall = Physics.Raycast(pos + Vector3.up * 1f, -_wallN, out wall, 1.3f, ClimbMask, QueryTriggerInteraction.Ignore) && FreeRunRules.WallClimbable(wall.normal.y);
            if (!haveWall || _frTime > FreeRunRules.WallRunSeconds)
            {
                FreeRunEnd();
                _vy = Mathf.Min(_vy, 1f);
                SetFreeRunGroundAnim();
                return;
            }
            Vector3 n = MathUtil.Flat(wall.normal);
            if (n.sqrMagnitude > 1e-4f) _wallN = n.normalized;
            Vector3 tangent = FreeRunRules.WallTangent(_vel, _wallN);
            if (canControl && Act.Jump)
            {
                WallJumpOff(tangent, 0f, true, _wrSpeed);
                return;
            }
            if (canControl && (Act.Crouch || (stick > 0.6f && Vector3.Dot(wish, _wallN) > 0.75f && _frTime > 0.2f)))
            {
                LetGoOfWall(1.5f);
                _vel += tangent * (_wrSpeed * 0.6f);
                SetFreeRunGroundAnim();
                return;
            }
            _wrSpeed = FreeRunRules.WallRunForward(_wrSpeed, _frTime, dt);
            _vy = FreeRunRules.WallRunVertical(_vy, dt);
            Vector3 before = pos;
            CollisionFlags flags = Controller.Move((tangent * _wrSpeed + Vector3.up * _vy - _wallN * 0.8f + _external) * dt);
            _external = Vector3.MoveTowards(_external, Vector3.zero, dt * 12f);
            Vector3 moved = transform.position - before;
            float along = Vector3.Dot(moved, tangent);
            _vel = tangent * _wrSpeed;
            bool grounded = (flags & CollisionFlags.Below) != 0 || Controller.isGrounded || (_vy <= 0f && GroundProbe());
            if (grounded)
            {
                FreeRunEnd();
                _grounded = true;
                _vy = -2f;
                SetFreeRunGroundAnim();
                return;
            }
            if ((flags & CollisionFlags.Sides) != 0 && along < _wrSpeed * dt * 0.3f && _frTime > 0.1f)
            {
                // Something ahead on the wall: drop off it.
                FreeRunEnd();
                _vel = tangent * 1.5f + _wallN * 1f;
                _vy = Mathf.Min(_vy, 0f);
                SetFreeRunGroundAnim();
                return;
            }
            _wrPhase += along * 1.3f;
            float prevYaw = _yaw;
            _yaw = Mathf.MoveTowardsAngle(_yaw, MathUtil.YawOf(tangent), 900f * dt);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            TrackTurn(prevYaw, _yaw, dt);
            if (transform.position.y < WorldBounds.KillY) Respawn(Game.Root != null ? Game.Root.SafeRespawnPoint(transform.position) : Vector3.up * 2f, _yaw);
            _footTimer -= dt * _wrSpeed;
            if (_footTimer <= 0f)
            {
                _footTimer = 1.1f;
                if (Game.Audio != null) Game.Audio.PlayVar(SfxId.Footstep, transform.position, 0.3f, 0.15f, 18f);
            }
            Anim.Velocity = Vector3.zero;
            Anim.VerticalVelocity = 0f;
            Anim.Grounded = false;
            Anim.CrouchWeight = 0f;
            Anim.SitWeight = 0f;
            Anim.Hold = Weapons != null ? Weapons.Current.Hold : HoldType.None;
            Anim.Aiming = false;
        }

        // ================================================================== dive

        /// <summary>Crouch in mid-air over a real drop (from a plain jump, a leap, a glide or flight): the high dive.</summary>
        private bool TryStartDive()
        {
            if (IsDead || IsRagdolled || InVehicle || _climbing) return false;
            float drop = DropBelow();
            if (!FreeRunRules.CanDive(drop, _vy)) return false;
            StartDive(drop);
            return true;
        }

        private void StartDive(float drop)
        {
            // Leave any travel power cleanly; the dive owns the fall from here.
            _travel = TravelMode.None;
            _gliding = false;
            _leapRise = false;
            _resumeSurge = false;
            _jumpHeldFromAir = false;
            _leapCharging = false;
            SetSurgeStepping(false);
            _fr = FreeRunState.Dive;
            _frTime = 0f;
            _slamArmed = false;
            _diveHeight = drop;
            Vector3 flat = MathUtil.Flat(_vel);
            if (flat.magnitude > FreeRunRules.DiveSteerSpeed) flat = flat.normalized * FreeRunRules.DiveSteerSpeed;
            _vel = flat;
            _vy = Mathf.Min(_vy, -1f);
            _grounded = false;
            _airPeakSpeed = 0f;
            Crouched = false;
            Aiming = false;
            AimingDownSights = false;
            Sprinting = false;
            Combat.ReleaseLock();
            if (flat.sqrMagnitude > 0.5f) _yaw = MathUtil.YawOf(flat);
            else if (Game.Cam != null) _yaw = Game.Cam.Yaw;
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            TravelSound(TravelSounds.Clip.Whoosh, 0.6f, 0.7f);
            TravelShake(0.06f, 0.2f, 0.1f, 0.15f);
        }

        private void UpdateDive(float dt, bool canControl)
        {
            _frTime += dt;
            float stick;
            Vector3 wish = TravelWish(canControl, out stick);
            Combat.Tick(dt, false, wish);
            Aiming = false;
            AimingDownSights = false;
            Sprinting = false;
            Crouched = false;
            // Pull up into a glide (the air powers), or arm the slam with a second press of crouch (powers layer).
            if (canControl && Act.Jump && LeapOpen && _frTime > 0.25f)
            {
                FreeRunEnd();
                EnterAir(false, false);
                _jumpHeldFromAir = true;
                BeginGlide();
                TravelCoastAir(dt);
                return;
            }
            if (stick > 0.15f) _vel = Vector3.MoveTowards(_vel, wish * FreeRunRules.DiveSteerSpeed, FreeRunRules.DiveSteerAccel * dt);
            else _vel = Vector3.MoveTowards(_vel, Vector3.zero, 1.2f * dt);
            _vy = FreeRunRules.DiveVertical(_vy, dt);
            _diveHeight = DropBelow();
            Vector3 before = transform.position;
            CollisionFlags flags = Controller.Move((_vel + Vector3.up * _vy + _external) * dt);
            _external = Vector3.MoveTowards(_external, Vector3.zero, dt * 12f);
            if ((flags & CollisionFlags.Sides) != 0) _vel = MathUtil.Flat(transform.position - before) / dt;
            bool grounded = (flags & CollisionFlags.Below) != 0 || Controller.isGrounded || GroundProbe();
            if (grounded && _frTime > 0.05f)
            {
                FinishDive(-_vy);
                return;
            }
            if (transform.position.y < WorldBounds.KillY) Respawn(Game.Root != null ? Game.Root.SafeRespawnPoint(transform.position) : Vector3.up * 2f, _yaw);
            float prevYaw = _yaw;
            if (_vel.sqrMagnitude > 1f) _yaw = Mathf.MoveTowardsAngle(_yaw, MathUtil.YawOf(_vel), 300f * dt);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            TrackTurn(prevYaw, _yaw, dt);
            Anim.Velocity = _vel;
            Anim.VerticalVelocity = _vy;
            Anim.Grounded = false;
            Anim.CrouchWeight = 0f;
            Anim.SitWeight = 0f;
            Anim.Hold = Weapons != null ? Weapons.Current.Hold : HoldType.None;
            Anim.Aiming = false;
        }

        /// <summary>The dive arrives: a splash in the canal, else the heroic landing (and the slam's shockwave when armed).</summary>
        private void FinishDive(float fallSpeed)
        {
            Vector3 p = transform.position;
            bool water = FreeRunRules.IsCanalWater(p.x, p.z, p.y);
            FreeRunEnd();
            NoteLandingNoise(water ? fallSpeed * 0.5f : fallSpeed, false);
            _grounded = true;
            _vy = -2f;
            _airPeakSpeed = 0f;
            if (water)
            {
                if (Game.Fx != null)
                {
                    Game.Fx.Splash(new Vector3(p.x, FreeRunRules.WaterSurfaceY, p.z), 1.2f + 0.8f * FreeRunRules.HeroLandingWeight(fallSpeed));
                    Game.Fx.WaterGeyser(new Vector3(p.x, FreeRunRules.WaterSurfaceY, p.z), 0.5f);
                }
                if (Game.Audio != null) Game.Audio.PlayVar(SfxId.Splash, p, 0.9f, 0.1f, 40f);
                _vel = MathUtil.Flat(_vel) * 0.3f;
                StartLanding(0.3f);
                Pad.Rumble(0.3f, 0.15f, 0.15f);
                SetFreeRunGroundAnim();
                return;
            }
            float w = FreeRunRules.HeroLandingWeight(fallSpeed);
            StartLanding(FreeRunRules.HeroLandingSeconds);
            if (Game.Fx != null)
            {
                Game.Fx.TravelLanding(p, 0.8f + 0.9f * w);
                if (w > 0.45f) Game.Fx.TravelBoom(p + Vector3.up * 0.2f, transform.forward);
            }
            TravelSound(TravelSounds.Clip.LandHeavy, 0.7f + 0.3f * w, 0.9f);
            TravelShake(0.2f + 0.35f * w, 0.6f + 0.4f * w, 0.4f, 0.3f);
            Vector3 flat = MathUtil.Flat(_vel);
            if (flat.magnitude > 2.5f) flat = flat.normalized * 2.5f;
            _vel = flat;
            SetFreeRunGroundAnim();
        }

        // ================================================================== presentation

        /// <summary>
        /// Hands the free-running pose to the animator (called from UpdateTravelPresentation before the travel power
        /// poses). True while a free-running state is active; the pose it leaves behind eases out like a travel pose.
        /// </summary>
        private bool FreeRunPresentation(float dt)
        {
            if (_fr == FreeRunState.None) return false;
            TravelPose pose = TravelPose.None;
            Vector3 lean = Vector3.zero;
            float speed = 0f;
            switch (_fr)
            {
                case FreeRunState.WallClimb:
                    pose = TravelPose.WallClimb;
                    lean = new Vector3(_climbUpSmooth, _climbSideSmooth, _climbPhase);
                    break;
                case FreeRunState.LedgeHang:
                    pose = TravelPose.LedgeHang;
                    lean = new Vector3(0f, _climbSideSmooth, 0f);
                    break;
                case FreeRunState.WallRun:
                    pose = TravelPose.WallRun;
                    speed = _wrSpeed;
                    lean = new Vector3(Mathf.InverseLerp(3f, FreeRunRules.WallRunSpeed, _wrSpeed), _wrPhase, _frSide);
                    break;
                case FreeRunState.LandRoll:
                    pose = TravelPose.LandRoll;
                    speed = _vel.magnitude;
                    lean = new Vector3(Mathf.Clamp01(_frTime / FreeRunRules.RollSeconds), 0f, 0f);
                    break;
                case FreeRunState.Takedown:
                    pose = TravelPose.Takedown;
                    float td = _frTime < _tdApproach ? 0.3f * Mathf.Clamp01(_frTime / Mathf.Max(0.01f, _tdApproach)) : 0.3f + 0.7f * Mathf.Clamp01((_frTime - _tdApproach) / StealthRules.StrikeSeconds);
                    lean = new Vector3(td, _tdKind == TakedownKind.Ground ? 0f : 1f, 0f);
                    break;
                case FreeRunState.Dive:
                    pose = TravelPose.Dive;
                    speed = Mathf.Sqrt(_vel.sqrMagnitude + _vy * _vy);
                    lean = new Vector3(FreeRunRules.DivePitch(_vy), FreeRunRules.DiveFlip(_diveHeight), _slamArmed ? 1f : 0f);
                    if (Game.Fx != null && speed > 14f) Game.Fx.TravelStreaks(transform.position + Vector3.up, _vel + Vector3.up * _vy, Mathf.InverseLerp(14f, FreeRunRules.DiveTerminal, speed));
                    break;
            }
            _pose = pose;
            _poseBlend = 1f;
            _lean = Vector3.Lerp(_lean, lean, MathUtil.Damp(10f, dt));
            // Cycle positions and phases are not smoothed (the climb and wall run stride, the roll's progress).
            if (_fr == FreeRunState.WallClimb) _lean.z = lean.z;
            if (_fr == FreeRunState.WallRun) _lean.y = lean.y;
            if (_fr == FreeRunState.LandRoll || _fr == FreeRunState.Takedown) _lean.x = lean.x;
            Anim.Travel = _pose;
            Anim.TravelBlend = 1f;
            Anim.TravelLean = _lean;
            TravelSpeed = speed;
            return true;
        }
    }
}
