using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Malik's travel powers on foot (the numbers live in TravelMath, the unlocks and sounds in TravelPowers):
    /// Surge, super speed switched on with the crouch button while sprinting (momentum steering, automatic vaults,
    /// pedestrians knocked aside, a stumble on a hard wall hit); the Super Leap, charged by holding Jump while crouched
    /// or taken out of a Surge; Glide, holding Jump in the air; and Flight, double-tapping Jump in the air. Leaps,
    /// glides and flights always land safely (no fall damage). Every frame this file also sets the animator's travel
    /// pose (HumanoidAnimator.Travel, TravelBlend, TravelLean) and drives the travel effects.
    /// TravelLean is in degrees: x is the forward pitch (0 upright, 90 flat face down), z the bank (positive banks
    /// right into a right turn, negative left, the same sign as HumanoidAnimator.RideLean).
    /// </summary>
    public sealed partial class PlayerController
    {
        /// <summary>The travel power moving Malik right now (None for ordinary movement).</summary>
        public TravelMode TravelState { get { return _travel; } }
        /// <summary>Speed in metres per second while a travel power is active (0 otherwise), for the HUD and the sounds.</summary>
        public float TravelSpeed { get; private set; }
        /// <summary>Gliding (only in the Air travel state).</summary>
        public bool Gliding { get { return _travel == TravelMode.Air && _gliding; } }
        /// <summary>Holding Jump while crouched to charge a Super Leap.</summary>
        public bool LeapCharging { get { return _leapCharging; } }
        /// <summary>The Super Leap's charge, 0 (a tap) to 1 (full).</summary>
        public float LeapCharge { get { return _leapCharge; } }
        /// <summary>The body's bank in degrees while traveling (positive banks right), for the camera's roll.</summary>
        public float TravelBank { get { return _lean.z; } }

        private TravelMode _travel;
        private float _travelTime;
        private bool _gliding;
        private bool _leapRise;
        private bool _resumeSurge;
        private bool _jumpHeldFromAir;
        private readonly DoubleTap _jumpTaps = new DoubleTap();
        private bool _leapCharging;
        private float _leapCharge;
        private float _landTimer;
        private float _landDuration = 0.5f;
        private float _surgeAir;
        private bool _surgeVault;
        private bool _boomDone;
        private float _glideYaw;
        private float _travelYaw;
        private float _turnRate;
        private float _flyStrafe;
        private Vector3 _lean;
        private TravelPose _pose;
        private float _poseBlend;
        private float _ghostTimer;
        private float _dustTimer;
        private float _sprintFor;
        private bool _surgeStepping;
        private float _baseStep = 0.42f;
        private float _baseSlope = 52f;

        // ================================================================== availability

        private bool TravelAllowed { get { return !InVehicle && !IsRagdolled && !IsDead && (Game.Cam == null || !Game.Cam.InCinematic); } }
        private bool SurgeOpen { get { return TravelAllowed && (Game.Travel == null || Game.Travel.SurgeUnlocked); } }
        private bool LeapOpen { get { return TravelAllowed && Game.Travel != null && Game.Travel.LeapUnlocked; } }
        private bool FlightOpen { get { return TravelAllowed && Game.Travel != null && Game.Travel.FlightUnlocked; } }

        private void NoteTravelUse(TravelPower p)
        {
            if (Game.Travel != null) Game.Travel.NoteUse(p);
        }

        private void TravelHint(TravelPower p)
        {
            if (Game.Travel != null) Game.Travel.Hint(p);
        }

        private void TravelSound(TravelSounds.Clip c, float volume, float pitch)
        {
            if (Game.Travel != null) Game.Travel.PlaySound(c, volume, pitch);
        }

        private void TravelShake(float amount, float rumbleLow, float rumbleHigh, float seconds)
        {
            if (Game.Cam != null) Game.Cam.Shake(amount);
            Pad.Rumble(rumbleLow, rumbleHigh, seconds);
        }

        /// <summary>The camera-relative stick direction (flat, length 0..1) and how far the stick is pushed.</summary>
        private Vector3 TravelWish(bool canControl, out float stick)
        {
            Vector2 input = canControl ? GInput.Move : Vector2.zero;
            stick = Mathf.Clamp01(input.magnitude);
            Vector3 camFwd = Game.Cam != null ? Game.Cam.FlatForward : transform.forward;
            Vector3 wish = MathUtil.RightOf(camFwd) * input.x + camFwd * input.y;
            if (wish.sqrMagnitude > 1f) wish.Normalize();
            return wish;
        }

        // ================================================================== hooks used by PlayerController.cs

        /// <summary>Runs Surge, the air powers or flight when one is active. False means ordinary on-foot movement runs.</summary>
        private bool UpdateTravelModes(float dt, bool canControl)
        {
            switch (_travel)
            {
                case TravelMode.Surge: UpdateSurge(dt, canControl); return true;
                case TravelMode.Air: UpdateTravelAir(dt, canControl); return true;
                case TravelMode.Flight: UpdateFlight(dt, canControl); return true;
                default: return false;
            }
        }

        /// <summary>The crouch button was pressed: while sprinting it starts the Surge instead (true means no crouch).</summary>
        private bool TryStartSurge(bool sprintKey, Vector3 wish)
        {
            if (!sprintKey || wish.sqrMagnitude < 0.1f || !_grounded || Crouched || !SurgeOpen) return false;
            if (Combat.State != PlayerCombat.Phase.Free || Combat.Guarding || GInput.Aim || _hipFire > 0f) return false;
            _travel = TravelMode.Surge;
            _travelTime = 0f;
            _surgeAir = 0f;
            _surgeVault = false;
            _boomDone = false;
            Combat.ReleaseLock();
            if (_vel.sqrMagnitude < 4f) _vel = wish.normalized * TravelMath.SprintSpeed;
            SetSurgeStepping(true);
            NoteTravelUse(TravelPower.Surge);
            TravelSound(TravelSounds.Clip.Whoosh, 0.5f, 1.15f);
            TravelShake(0.08f, 0.25f, 0.1f, 0.15f);
            return true;
        }

        /// <summary>Sprinting costs stamina only in a fight; outside one it is free and stamina keeps refilling.</summary>
        private void UpdateSprintStamina(float dt)
        {
            if (!Sprinting)
            {
                _sprintFor = 0f;
                return;
            }
            _sprintFor += dt;
            if (CombatDirector.InCombat) Combat.SpendStamina(dt * TravelMath.SprintStaminaPerSecond);
            else Combat.Stamina = Mathf.Min(Combat.MaxStamina, Combat.Stamina + dt * TravelMath.SprintRestPerSecond * SkillMods.StaminaRegen);
            if (_sprintFor > 1.2f && SurgeOpen) TravelHint(TravelPower.Surge);
        }

        /// <summary>Ground speed multiplier: zero while charging a leap, easing back in while recovering from a landing.</summary>
        private float TravelGroundSpeedScale()
        {
            if (_leapCharging) return 0f;
            if (_landTimer <= 0f) return 1f;
            float p = 1f - _landTimer / Mathf.Max(0.01f, _landDuration);
            return Mathf.Clamp01((p - 0.35f) / 0.5f);
        }

        /// <summary>
        /// Grounded Jump handling before the ordinary jump. Crouched with the Super Leap unlocked, Jump starts a charge,
        /// holding builds it and letting go leaps. True when it used the input (no ordinary jump this frame).
        /// </summary>
        private bool TravelGroundJump(bool jump, bool canControl, Vector3 wish, float dt)
        {
            if (jump) _jumpTaps.Press(Time.time);
            if (_landTimer > _landDuration * 0.5f) return true;
            if (_leapCharging)
            {
                if (!Crouched || !LeapOpen || Combat.State != PlayerCombat.Phase.Free || Combat.Guarding || Aiming)
                {
                    _leapCharging = false;
                    return true;
                }
                if (canControl && Act.JumpHeld)
                {
                    _leapCharge = Mathf.Min(1f, _leapCharge + dt / TravelMath.LeapChargeSeconds);
                    return true;
                }
                LaunchLeap(_leapCharge, wish);
                return true;
            }
            if (Crouched && LeapOpen)
            {
                if (jump)
                {
                    _leapCharging = true;
                    _leapCharge = 0f;
                    return true;
                }
                TravelHint(TravelPower.Leap);
            }
            return false;
        }

        /// <summary>Ordinary airborne frames: a double tap of Jump takes off into flight, a fresh hold of Jump while falling glides.</summary>
        private void TravelAirborne(bool jump, bool canControl)
        {
            _leapCharging = false;
            bool held = canControl && Act.JumpHeld;
            if (!held) _jumpHeldFromAir = false;
            if (jump)
            {
                if (_jumpTaps.Press(Time.time) && FlightOpen)
                {
                    StartFlight();
                    return;
                }
                _jumpHeldFromAir = true;
            }
            if (_jumpHeldFromAir && held && _vy <= 0f && LeapOpen)
            {
                EnterAir(false, false);
                _jumpHeldFromAir = true;
                BeginGlide();
            }
        }

        /// <summary>Stops every travel power at once (teleports, ragdolls, vehicles, climbing) and clears the travel pose.</summary>
        private void TravelReset()
        {
            _travel = TravelMode.None;
            _gliding = false;
            _leapRise = false;
            _resumeSurge = false;
            _leapCharging = false;
            _leapCharge = 0f;
            _surgeVault = false;
            _landTimer = 0f;
            _jumpHeldFromAir = false;
            _jumpTaps.Clear();
            SetSurgeStepping(false);
            TravelSpeed = 0f;
            _pose = TravelPose.None;
            _poseBlend = 0f;
            _lean = Vector3.zero;
            _turnRate = 0f;
            if (Anim != null)
            {
                Anim.Travel = TravelPose.None;
                Anim.TravelBlend = 0f;
                Anim.TravelLean = Vector3.zero;
            }
        }

        /// <summary>A big hit (20 damage or more) ends a Surge.</summary>
        private void TravelOnDamaged(DamageInfo info)
        {
            if (_travel == TravelMode.Surge && info.Amount >= 20f) EndSurge();
        }

        // ================================================================== Surge

        private void SetSurgeStepping(bool on)
        {
            if (Controller == null || on == _surgeStepping) return;
            _surgeStepping = on;
            if (on)
            {
                _baseStep = Controller.stepOffset;
                _baseSlope = Controller.slopeLimit;
                Controller.stepOffset = 0.62f;
                Controller.slopeLimit = 65f;
            }
            else
            {
                Controller.stepOffset = _baseStep;
                Controller.slopeLimit = _baseSlope;
            }
        }

        /// <summary>Ends the Surge; the momentum carries over and the ordinary movement code brakes it.</summary>
        private void EndSurge()
        {
            if (_travel != TravelMode.Surge) return;
            _travel = TravelMode.None;
            _surgeVault = false;
            SetSurgeStepping(false);
        }

        private void UpdateSurge(float dt, bool canControl)
        {
            _travelTime += dt;
            float stick;
            Vector3 wish = TravelWish(canControl, out stick);
            Combat.Tick(dt, canControl, wish);
            WeaponDef d = Weapons.Current;
            bool aimInput = canControl && d.Fire != FireKind.Melee && GInput.Aim;
            Aiming = false;
            AimingDownSights = false;
            Sprinting = false;
            Crouched = false;
            Vector3 flat = _vel;
            float speed = flat.magnitude;
            float yaw = speed > 0.5f ? MathUtil.YawOf(flat) : _yaw;
            // Aiming, attacking, rolling, guarding, a stagger or the toggle end it (feet planted again).
            bool stop = aimInput || _hipFire > 0f || Combat.State != PlayerCombat.Phase.Free || Combat.Guarding || (canControl && Act.SurgeToggle) || !SurgeOpen;
            if (stop)
            {
                EndSurge();
                TravelCoast(dt, d);
                return;
            }
            bool braking = false;
            if (stick > 0.2f)
            {
                float wishYaw = MathUtil.YawOf(wish);
                float rate = TravelMath.SurgeTurnRate(speed) * Mathf.Rad2Deg;
                if (Mathf.Abs(Mathf.DeltaAngle(yaw, wishYaw)) > TravelMath.SurgeReverseAngle && speed > 12f)
                {
                    braking = true;
                    speed = Mathf.MoveTowards(speed, 0f, TravelMath.SurgeBrake * dt);
                }
                yaw = Mathf.MoveTowardsAngle(yaw, wishYaw, rate * dt);
                if (!braking) speed = TravelMath.SurgeStep(speed, TravelMath.SurgeTargetSpeed(stick), dt);
            }
            else
            {
                speed = Mathf.MoveTowards(speed, 0f, TravelMath.SurgeBrake * dt);
                if (speed < TravelMath.SurgeEndSpeed)
                {
                    _vel = MathUtil.DirFromYaw(yaw) * speed;
                    EndSurge();
                    TravelCoast(dt, d);
                    return;
                }
            }
            Vector3 dir = MathUtil.DirFromYaw(yaw);
            if (!_boomDone && speed >= TravelMath.SurgeBoomSpeed) SurgeBoom(dir);
            if (speed < TravelMath.SurgeBoomSpeed - 6f) _boomDone = false;

            // Jump: a long leap once the Super Leap is unlocked, a running hop before that.
            bool jump = canControl && Act.Jump;
            if (jump) _jumpTaps.Press(Time.time);
            if (jump && (_grounded || _surgeAir < 0.15f) && !_surgeVault)
            {
                if (LeapOpen)
                {
                    LaunchSurgeLeap(speed, dir);
                    TravelCoastAir(dt);
                    return;
                }
                _vy = TravelMath.SurgeHopSpeed;
                _grounded = false;
                _surgeVault = true;
                TravelSound(TravelSounds.Clip.Whoosh, 0.3f, 1.3f);
            }
            if (_grounded && speed > 9f && !_surgeVault) TrySurgeVault(dir, speed);
            ClearSurgePath(dir, speed);
            _vel = dir * speed;
            // Feet stay on slopes and down steps at speed; off a real edge the fall starts from rest.
            if (_grounded && !_surgeVault) _vy = -Mathf.Max(3f, speed * 0.5f);
            else _vy -= TravelMath.FallGravity * dt;
            Vector3 before = transform.position;
            bool wasGrounded = _grounded;
            CollisionFlags flags = Controller.Move((_vel + Vector3.up * _vy + _external) * dt);
            _external = Vector3.MoveTowards(_external, Vector3.zero, dt * 12f);
            _grounded = Controller.isGrounded || (_vy <= 0f && GroundProbe());
            if (_surgeVault && _grounded && _vy <= 0f) _surgeVault = false;
            if (wasGrounded && !_grounded && !_surgeVault && _vy < 0f) _vy = 0f;
            if (_grounded) _surgeAir = 0f;
            else _surgeAir += dt;
            // Walls: a hard head-on hit stumbles, a glancing one runs on along the wall.
            if ((flags & CollisionFlags.Sides) != 0)
            {
                Vector3 moved = MathUtil.Flat(transform.position - before) / dt;
                if (speed > TravelMath.WallStumbleSpeed && Vector3.Dot(moved, dir) < speed * 0.35f)
                {
                    SurgeWallHit(dir, speed);
                    return;
                }
                if (moved.sqrMagnitude > 1f) _vel = moved;
            }
            // Off a roof or into a long drop: the air takes over, with a safe landing.
            if (!_grounded && (_surgeVault ? _surgeAir > 1.6f : _surgeAir > 0.3f))
            {
                EnterAir(false, true);
                return;
            }
            float prevYaw = _yaw;
            _yaw = MathUtil.YawOf(_vel.sqrMagnitude > 0.01f ? _vel : dir);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            TrackTurn(prevYaw, _yaw, dt);
            if (transform.position.y < -60f) Respawn(Game.Root != null ? Game.Root.SafeRespawnPoint(transform.position) : Vector3.up * 2f, _yaw);

            // A light, quick patter of footsteps (capped so top speed is not a drum roll).
            if (_grounded)
            {
                _footTimer -= dt * Mathf.Min(speed, 16f);
                if (_footTimer <= 0f)
                {
                    _footTimer = 1.25f;
                    if (Game.Audio != null) Game.Audio.PlayVar(SfxId.Footstep, transform.position, 0.3f, 0.2f, 18f);
                }
            }
            Anim.Velocity = _vel;
            Anim.VerticalVelocity = _vy;
            Anim.Grounded = _grounded;
            Anim.CrouchWeight = 0f;
            Anim.SitWeight = 0f;
            Anim.Hold = d.Hold;
        }

        /// <summary>
        /// Looks ahead for a low obstacle (a car, a bench, a low wall) and hops over it in time. Walls taller than
        /// SurgeVaultMaxHeight are not vaulted (a hard hit stumbles, a glancing one slides along).
        /// </summary>
        private void TrySurgeVault(Vector3 dir, float speed)
        {
            int mask = Layers.WorldMask | (1 << Layers.Debris) | (1 << Layers.VehicleShell);
            Vector3 feet = transform.position;
            RaycastHit hit;
            if (!Physics.SphereCast(feet + Vector3.up * 0.55f, 0.25f, dir, out hit, speed * 0.3f + 1f, mask, QueryTriggerInteraction.Ignore)) return;
            if (hit.normal.y > 0.6f) return;
            // Too early to jump: wait until the obstacle is about a rise time away.
            if (hit.distance > speed * 0.26f + 0.8f) return;
            float reach = TravelMath.SurgeVaultMaxHeight + 0.6f;
            Vector3 probe = hit.point + dir * 0.45f;
            probe.y = feet.y + reach;
            RaycastHit top;
            if (!Physics.Raycast(probe, Vector3.down, out top, reach, mask, QueryTriggerInteraction.Ignore)) return;
            float h = top.point.y - feet.y;
            if (h < Controller.stepOffset || h > TravelMath.SurgeVaultMaxHeight) return;
            _vy = TravelMath.LaunchSpeed(h + 0.6f, TravelMath.FallGravity);
            _surgeVault = true;
            _grounded = false;
            TravelSound(TravelSounds.Clip.Whoosh, 0.35f, 1.3f);
        }

        /// <summary>
        /// People in the path: those right ahead are knocked aside (no wanted level for a civilian brushed past; plowing
        /// into police is assault on an officer as usual), those a little further ahead leap out of the way.
        /// </summary>
        private void ClearSurgePath(Vector3 dir, float speed)
        {
            if (speed < 9f) return;
            Vector3 p = transform.position;
            Vector3 right = MathUtil.RightOf(dir);
            float contact = 1.4f + speed * 0.04f;
            for (int i = 0; i < NpcController.All.Count; i++)
            {
                NpcController n = NpcController.All[i];
                if (n == null || n.IsDead || n.IsRagdolled || n.InVehicle || n.IsCreature) continue;
                Vector3 to = n.transform.position - p;
                if (to.y > 2f || to.y < -2f) continue;
                to.y = 0f;
                float along = Vector3.Dot(to, dir);
                if (along < -0.3f || along > speed * 0.4f + 2f) continue;
                float side = Vector3.Dot(to, right);
                if (Mathf.Abs(side) > 2.4f) continue;
                if (along < contact && Mathf.Abs(side) < 1.1f) KnockAside(n, dir, right, side, speed);
                else if (speed > 12f && n.State != NpcState.Flee && !n.IsHostileToPlayer) n.Flee(p, 3f);
            }
        }

        private void KnockAside(NpcController n, Vector3 dir, Vector3 right, float side, float speed)
        {
            float k = Mathf.InverseLerp(9f, TravelMath.SurgeTopSpeed, speed);
            float s = side >= 0f ? 1f : -1f;
            Vector3 push = right * s * Mathf.Lerp(3f, 7f, k) + dir * Mathf.Lerp(2f, 6f, k) + Vector3.up * Mathf.Lerp(1.5f, 3f, k);
            n.Knockdown(push, n.transform.position + Vector3.up, 1.2f);
            bool cop = n.Role == NpcRole.Police || n.Role == NpcRole.Swat;
            if (cop && Game.Wanted != null) Game.Wanted.ReportCrime(CrimeType.AttackCop, n.transform.position, n.gameObject);
            if (Game.Cam != null) Game.Cam.Shake(0.05f + 0.08f * k);
        }

        private void SurgeBoom(Vector3 dir)
        {
            _boomDone = true;
            if (Game.Fx != null) Game.Fx.TravelBoom(transform.position, dir);
            TravelSound(TravelSounds.Clip.Boom, 0.7f, 1f);
            TravelShake(0.18f, 0.5f, 0.2f, 0.25f);
        }

        /// <summary>A hard head-on wall hit at speed: bounce off and stumble (crouch-and-rise), no damage.</summary>
        private void SurgeWallHit(Vector3 dir, float speed)
        {
            EndSurge();
            _vel = -dir * Mathf.Min(4f, speed * 0.12f);
            _vy = 2.5f;
            _grounded = false;
            StartLanding(0.6f);
            if (Game.Fx != null) Game.Fx.TravelLanding(transform.position + dir * 0.3f, 0.6f);
            TravelSound(TravelSounds.Clip.LandHeavy, 0.8f, 1.1f);
            TravelShake(0.35f, 0.7f, 0.3f, 0.3f);
            Anim.Velocity = _vel;
            Anim.Grounded = false;
        }

        private void LaunchSurgeLeap(float speed, Vector3 dir)
        {
            _vel = dir * Mathf.Min(speed * 0.92f, TravelMath.SurgeTopSpeed);
            _vy = TravelMath.LaunchSpeed(TravelMath.SurgeLeapHeight(speed), TravelMath.LeapGravity);
            EnterAir(true, true);
            NoteTravelUse(TravelPower.Leap);
            if (Game.Fx != null) Game.Fx.TravelLanding(transform.position, 0.5f);
            TravelSound(TravelSounds.Clip.Whoosh, 0.8f, 0.95f);
            TravelShake(0.15f, 0.4f, 0.15f, 0.2f);
        }

        // ================================================================== Super Leap, air and glide

        private void LaunchLeap(float charge, Vector3 wish)
        {
            _leapCharging = false;
            Crouched = false;
            _vy = TravelMath.LaunchSpeed(TravelMath.LeapHeight(charge), TravelMath.LeapGravity);
            Vector3 fwd = wish.sqrMagnitude > 0.04f ? wish : Vector3.zero;
            _vel = fwd * TravelMath.LeapForwardSpeed + _vel * 0.3f;
            EnterAir(true, false);
            NoteTravelUse(TravelPower.Leap);
            if (Game.Fx != null) Game.Fx.TravelLanding(transform.position, 0.35f + 0.5f * charge);
            TravelSound(TravelSounds.Clip.Whoosh, 0.8f, 1.05f - 0.15f * charge);
            TravelShake(0.12f + 0.15f * charge, 0.35f + 0.3f * charge, 0.1f, 0.2f);
        }

        /// <summary>
        /// Switches to the Air travel state (falls land safely). leap: rising from a leap uses the lighter leap gravity.
        /// fromSurge: landing with the stick still pushed runs straight on in the Surge.
        /// </summary>
        private void EnterAir(bool leap, bool fromSurge)
        {
            SetSurgeStepping(false);
            _travel = TravelMode.Air;
            _travelTime = 0f;
            _gliding = false;
            _leapRise = leap;
            _resumeSurge = fromSurge;
            _jumpHeldFromAir = false;
            _surgeVault = false;
            _grounded = false;
            Combat.ReleaseLock();
            _glideYaw = _vel.sqrMagnitude > 0.25f ? MathUtil.YawOf(_vel) : _yaw;
        }

        private void BeginGlide()
        {
            _gliding = true;
            _leapRise = false;
            _glideYaw = _vel.sqrMagnitude > 1f ? MathUtil.YawOf(_vel) : _yaw;
            NoteTravelUse(TravelPower.Glide);
            TravelSound(TravelSounds.Clip.Whoosh, 0.35f, 1.2f);
        }

        private void UpdateTravelAir(float dt, bool canControl)
        {
            _travelTime += dt;
            float stick;
            Vector3 wish = TravelWish(canControl, out stick);
            Combat.Tick(dt, false, wish);
            WeaponDef d = Weapons.Current;
            bool aimInput = canControl && d.Fire != FireKind.Melee && GInput.Aim;
            Aiming = aimInput || (canControl && _hipFire > 0f && (d.IsGun || d.Fire == FireKind.Throw));
            AimingDownSights = Aiming && aimInput;
            Sprinting = false;
            Crouched = false;
            bool jumpDown = canControl && Act.Jump;
            bool held = canControl && Act.JumpHeld;
            if (!held) _jumpHeldFromAir = false;
            if (jumpDown)
            {
                if (_jumpTaps.Press(Time.time) && FlightOpen)
                {
                    StartFlight();
                    TravelCoastAir(dt);
                    return;
                }
                _jumpHeldFromAir = true;
                if (_vel.magnitude < 10f && TryClimb())
                {
                    _travel = TravelMode.None;
                    _gliding = false;
                    return;
                }
            }
            bool wantGlide = LeapOpen && _jumpHeldFromAir && held && _vy <= 0f;
            if (wantGlide && !_gliding) BeginGlide();
            else if (!wantGlide && _gliding) _gliding = false;

            Vector3 h = _vel;
            if (_gliding)
            {
                // Steer with the stick; with it centred the glide drifts toward where the camera looks.
                if (stick > 0.25f) _glideYaw = Mathf.MoveTowardsAngle(_glideYaw, MathUtil.YawOf(wish), TravelMath.GlideTurnRate * dt);
                else if (Game.Cam != null) _glideYaw = Mathf.MoveTowardsAngle(_glideYaw, Game.Cam.Yaw, TravelMath.GlideCameraTurnRate * dt);
                h = Vector3.MoveTowards(h, MathUtil.DirFromYaw(_glideYaw) * TravelMath.GlideForward, TravelMath.GlideAccel * dt);
                _vy = TravelMath.GlideVertical(_vy, dt);
                if (FlightOpen) TravelHint(TravelPower.Flight);
            }
            else
            {
                float hs = h.magnitude;
                if (hs > TravelMath.AirSpeed)
                {
                    // Fast arcs (out of a Surge) keep their momentum: gentle drag and steering.
                    float yawH = MathUtil.YawOf(h);
                    if (stick > 0.2f) yawH = Mathf.MoveTowardsAngle(yawH, MathUtil.YawOf(wish), TravelMath.AirTurnRate * dt);
                    h = MathUtil.DirFromYaw(yawH) * Mathf.MoveTowards(hs, TravelMath.AirSpeed, TravelMath.AirDrag * dt);
                }
                else if (stick > 0.1f) h = Vector3.MoveTowards(h, wish * TravelMath.AirSpeed, TravelMath.AirAccel * dt);
                else h = Vector3.MoveTowards(h, Vector3.zero, 1.5f * dt);
                float g = _leapRise && _vy > 0f ? TravelMath.LeapGravity : TravelMath.FallGravity;
                _vy = Mathf.Max(_vy - g * dt, -TravelMath.TerminalFall);
                if (_vy < -2f && LeapOpen) TravelHint(TravelPower.Glide);
            }
            if (_vy <= 0f) _leapRise = false;

            float prevYaw = _yaw;
            if (Aiming && Game.Cam != null) _yaw = Mathf.MoveTowardsAngle(_yaw, Game.Cam.Yaw, 1100f * dt);
            else if (h.sqrMagnitude > 1f) _yaw = Mathf.MoveTowardsAngle(_yaw, MathUtil.YawOf(h), 540f * dt);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            TrackTurn(prevYaw, _yaw, dt);

            Vector3 before = transform.position;
            CollisionFlags flags = Controller.Move((h + Vector3.up * _vy + _external) * dt);
            _external = Vector3.MoveTowards(_external, Vector3.zero, dt * 12f);
            if ((flags & CollisionFlags.Sides) != 0) h = MathUtil.Flat(transform.position - before) / dt;
            if ((flags & CollisionFlags.Above) != 0 && _vy > 0f) _vy = 0f;
            _vel = h;
            bool grounded = Controller.isGrounded || (_vy <= 0f && GroundProbe());
            if (grounded && _travelTime > 0.05f)
            {
                TravelTouchdown(-_vy, h.magnitude);
                SetTravelGroundAnim(d);
                return;
            }
            if (transform.position.y < -60f) Respawn(Game.Root != null ? Game.Root.SafeRespawnPoint(transform.position) : Vector3.up * 2f, _yaw);
            Anim.Velocity = _vel;
            Anim.VerticalVelocity = _vy;
            Anim.Grounded = false;
            Anim.CrouchWeight = 0f;
            Anim.SitWeight = 0f;
            Anim.Hold = d.Hold;
        }

        // ================================================================== Flight

        private void StartFlight()
        {
            SetSurgeStepping(false);
            _travel = TravelMode.Flight;
            _travelTime = 0f;
            _gliding = false;
            _leapRise = false;
            _leapCharging = false;
            _jumpHeldFromAir = false;
            _grounded = false;
            Crouched = false;
            Combat.ReleaseLock();
            _vy = Mathf.Max(_vy, 3f);
            _travelYaw = _vel.sqrMagnitude > 1f ? MathUtil.YawOf(_vel) : _yaw;
            NoteTravelUse(TravelPower.Flight);
            TravelHint(TravelPower.Flight);
            TravelSound(TravelSounds.Clip.Whoosh, 0.8f, 0.8f);
            TravelShake(0.1f, 0.3f, 0.15f, 0.2f);
        }

        /// <summary>Double-tap in flight: stop flying and fall (safe landing; a fresh hold of Jump glides).</summary>
        private void StopFlying()
        {
            EnterAir(false, false);
            TravelSound(TravelSounds.Clip.Whoosh, 0.4f, 0.65f);
        }

        private void UpdateFlight(float dt, bool canControl)
        {
            _travelTime += dt;
            Vector2 input = canControl ? GInput.Move : Vector2.zero;
            if (input.sqrMagnitude > 1f) input.Normalize();
            _flyStrafe = input.x;
            Combat.Tick(dt, false, Vector3.zero);
            WeaponDef d = Weapons.Current;
            bool aimInput = canControl && d.Fire != FireKind.Melee && GInput.Aim;
            Aiming = aimInput || (canControl && _hipFire > 0f && (d.IsGun || d.Fire == FireKind.Throw));
            AimingDownSights = Aiming && aimInput;
            Sprinting = false;
            Crouched = false;
            bool jumpDown = canControl && Act.Jump;
            bool locked = Game.Travel != null && !Game.Travel.FlightUnlocked;
            if (locked || (jumpDown && _jumpTaps.Press(Time.time) && _travelTime > 0.2f))
            {
                StopFlying();
                TravelCoastAir(dt);
                return;
            }
            bool up = canControl && Act.FlyUpHeld;
            bool down = canControl && Act.FlyDownHeld;
            bool fast = canControl && Act.FlyFastHeld && !Aiming && input.y > 0.3f;
            // Steer with the stick and the camera: the camera's tilt tilts the flight path (look down to dive).
            float camYaw = Game.Cam != null ? Game.Cam.Yaw : _yaw;
            float camPitch = Game.Cam != null ? Game.Cam.Pitch : PlayerCamera.FootPitch;
            float pitch = TravelMath.FlightPitch(camPitch) * Mathf.Deg2Rad;
            Vector3 flatF = MathUtil.DirFromYaw(camYaw);
            Vector3 fwd3 = flatF * Mathf.Cos(pitch) - Vector3.up * Mathf.Sin(pitch);
            Vector3 move = fwd3 * input.y + MathUtil.RightOf(flatF) * input.x;
            float vert = (up ? 1f : 0f) - (down ? 1f : 0f);
            Vector3 target = move * TravelMath.FlightSpeed(fast, Aiming) + Vector3.up * vert * TravelMath.FlightClimbSpeed(fast, Aiming);
            Vector3 vel = _vel + Vector3.up * _vy;
            float accel = target.sqrMagnitude > vel.sqrMagnitude ? (fast ? TravelMath.FlyFastAccel : TravelMath.FlyAccel) : TravelMath.FlyDecel;
            vel = Vector3.MoveTowards(vel, target, accel * dt);
            vel.y = TravelMath.CeilingClamp(transform.position.y, vel.y);
            float bob = target.sqrMagnitude < 0.01f ? Mathf.Cos(_travelTime * 2.2f) * 0.22f : 0f;

            // Face the way you fly; aiming faces the camera; hovering turns slowly with the camera.
            Vector3 hv = MathUtil.Flat(vel);
            float targetYaw = camYaw;
            float rate = 220f;
            if (Aiming) rate = 1100f;
            else if (hv.sqrMagnitude > 9f)
            {
                targetYaw = MathUtil.YawOf(hv);
                rate = 540f;
            }
            _yaw = Mathf.MoveTowardsAngle(_yaw, targetYaw, rate * dt);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

            // Buildings stop or slide the flight; the controller's sweep never lets it pass through.
            Vector3 before = transform.position;
            CollisionFlags flags = Controller.Move((vel + Vector3.up * bob + _external) * dt);
            _external = Vector3.MoveTowards(_external, Vector3.zero, dt * 12f);
            if ((flags & (CollisionFlags.Sides | CollisionFlags.Above)) != 0)
            {
                Vector3 actual = (transform.position - before) / dt;
                actual.y -= bob;
                float v0 = vel.magnitude;
                if (v0 > 22f && actual.magnitude < v0 * 0.35f)
                {
                    TravelSound(TravelSounds.Clip.LandHeavy, 0.6f, 1.25f);
                    TravelShake(0.3f, 0.5f, 0.2f, 0.2f);
                    vel = -vel / v0 * 3f;
                }
                else vel = actual;
            }
            hv = MathUtil.Flat(vel);
            _vel = hv;
            _vy = vel.y;
            // The bank follows how fast the flight path turns; slow drifting does not bank.
            if (hv.sqrMagnitude > 25f)
            {
                float heading = MathUtil.YawOf(hv);
                TrackTurn(_travelYaw, heading, dt);
                _travelYaw = heading;
            }
            else
            {
                _travelYaw = hv.sqrMagnitude > 0.01f ? MathUtil.YawOf(hv) : _yaw;
                TrackTurn(0f, 0f, dt);
            }

            // Land by coming down onto the ground (sinking or diving), never by skimming along it.
            bool below = (flags & CollisionFlags.Below) != 0 || (vel.y < -0.5f && GroundProbe());
            if (below && _travelTime > 0.3f && (down || vel.y < -1f))
            {
                TravelTouchdown(-vel.y, hv.magnitude);
                SetTravelGroundAnim(d);
                return;
            }
            if (transform.position.y < -60f) Respawn(Game.Root != null ? Game.Root.SafeRespawnPoint(transform.position) : Vector3.up * 2f, _yaw);
            Anim.Velocity = _vel;
            Anim.VerticalVelocity = _vy;
            Anim.Grounded = false;
            Anim.CrouchWeight = 0f;
            Anim.SitWeight = 0f;
            Anim.Hold = d.Hold;
        }

        // ================================================================== shared movement helpers

        /// <summary>One plain move with the current velocity, for the frame a Surge ends (no stall at speed).</summary>
        private void TravelCoast(float dt, WeaponDef d)
        {
            if (_grounded) _vy = -2f;
            else _vy -= TravelMath.FallGravity * dt;
            Controller.Move((_vel + Vector3.up * _vy) * dt);
            _grounded = Controller.isGrounded || (_vy <= 0f && GroundProbe());
            SetTravelGroundAnim(d);
        }

        /// <summary>One ballistic move for the frame a leap or flight starts.</summary>
        private void TravelCoastAir(float dt)
        {
            Controller.Move((_vel + Vector3.up * _vy) * dt);
            _grounded = false;
            Anim.Velocity = _vel;
            Anim.VerticalVelocity = _vy;
            Anim.Grounded = false;
        }

        private void SetTravelGroundAnim(WeaponDef d)
        {
            Anim.Velocity = _vel;
            Anim.VerticalVelocity = _vy;
            Anim.Grounded = _grounded;
            Anim.CrouchWeight = 0f;
            Anim.SitWeight = 0f;
            Anim.Hold = d.Hold;
        }

        /// <summary>
        /// Touching down after a leap, glide or flight: never fall damage; fast arrivals are a heavy landing. An air
        /// phase that came out of a Surge runs straight on in the Surge when the stick is still pushed.
        /// </summary>
        private void TravelTouchdown(float fallSpeed, float groundSpeed)
        {
            bool heavy = TravelMath.IsHeavyLanding(fallSpeed, groundSpeed);
            bool stick = ControlsEnabled && Game.IsPlaying && !GInput.GameplayBlocked && GInput.Move.sqrMagnitude > 0.09f;
            if (_travel == TravelMode.Air && _resumeSurge && stick && SurgeOpen && groundSpeed > TravelMath.SurgeEndSpeed)
            {
                _travel = TravelMode.Surge;
                _gliding = false;
                _leapRise = false;
                _resumeSurge = false;
                _jumpHeldFromAir = false;
                _surgeAir = 0f;
                _surgeVault = false;
                _grounded = true;
                _airPeakSpeed = 0f;
                _vy = -2f;
                SetSurgeStepping(true);
                if (Game.Fx != null) Game.Fx.TravelLanding(transform.position, heavy ? 0.6f : 0.3f);
                TravelSound(heavy ? TravelSounds.Clip.LandHeavy : TravelSounds.Clip.LandSoft, heavy ? 0.6f : 0.4f, 1.1f);
                if (heavy) TravelShake(0.15f, 0.4f, 0.15f, 0.2f);
                return;
            }
            _resumeSurge = false;
            _travel = TravelMode.None;
            _gliding = false;
            _leapRise = false;
            _jumpHeldFromAir = false;
            SetSurgeStepping(false);
            _grounded = true;
            _airPeakSpeed = 0f;
            _vy = -2f;
            if (heavy)
            {
                StartLanding(0.5f);
                if (Game.Fx != null) Game.Fx.TravelLanding(transform.position, 1f);
                TravelSound(TravelSounds.Clip.LandHeavy, 0.9f, 1f);
                TravelShake(0.3f, 0.6f, 0.25f, 0.3f);
                float s = _vel.magnitude;
                if (s > 16f) _vel *= 16f / s;
            }
            else
            {
                StartLanding(0.2f);
                if (Game.Fx != null) Game.Fx.TravelLanding(transform.position, 0.3f);
                TravelSound(TravelSounds.Clip.LandSoft, 0.6f, 1f);
            }
        }

        private void StartLanding(float seconds)
        {
            _landDuration = Mathf.Max(0.05f, seconds);
            _landTimer = _landDuration;
            _pose = TravelPose.Land;
            _poseBlend = 1f;
        }

        /// <summary>Smoothed turn rate in degrees per second (positive turns right), for the bank.</summary>
        private void TrackTurn(float fromYaw, float toYaw, float dt)
        {
            float rate = dt > 0f ? Mathf.Clamp(Mathf.DeltaAngle(fromYaw, toYaw) / dt, -720f, 720f) : 0f;
            _turnRate = Mathf.Lerp(_turnRate, rate, MathUtil.Damp(8f, dt));
        }

        // ================================================================== pose and effects

        /// <summary>
        /// Every frame after movement: ends travel where it cannot continue (in or getting into a vehicle, climbing),
        /// then sets the animator's travel pose, blend and lean and spawns the travel effects. The pose eases in; when
        /// a power ends the last pose is kept while TravelBlend eases out, then Travel returns to None.
        /// </summary>
        private void UpdateTravelPresentation(float dt)
        {
            if (InVehicle || _enter != EnterPhase.None || _climbing)
            {
                if (_travel != TravelMode.None || _leapCharging || _landTimer > 0f || _poseBlend > 0f) TravelReset();
                return;
            }
            if (_landTimer > 0f) _landTimer = Mathf.Max(0f, _landTimer - dt);
            TravelPose want = TravelPose.None;
            float wantBlend = 1f;
            Vector3 lean = Vector3.zero;
            float speed = 0f;
            switch (_travel)
            {
                case TravelMode.Surge:
                    speed = _vel.magnitude;
                    want = TravelPose.Surge;
                    wantBlend = Mathf.Lerp(0.35f, 1f, Mathf.InverseLerp(TravelMath.SprintSpeed, 18f, speed));
                    lean = new Vector3(Mathf.Lerp(8f, 22f, speed / TravelMath.SurgeTopSpeed), 0f, TravelMath.BankFromTurn(_turnRate, 0.12f, 25f));
                    break;
                case TravelMode.Air:
                    speed = Mathf.Sqrt(_vel.sqrMagnitude + _vy * _vy);
                    if (_gliding)
                    {
                        want = TravelPose.Glide;
                        lean = new Vector3(25f, 0f, TravelMath.BankFromTurn(_turnRate, 0.25f, 35f));
                    }
                    else
                    {
                        // The leap pose for the rise; it eases out on the way down so the ordinary fall pose shows.
                        want = TravelPose.Leap;
                        wantBlend = _vy > -3f ? 1f : 0.25f;
                        lean = new Vector3(Mathf.Clamp(_vel.magnitude * 1.2f, 0f, 30f), 0f, 0f);
                    }
                    break;
                case TravelMode.Flight:
                    speed = Mathf.Sqrt(_vel.sqrMagnitude + _vy * _vy);
                    want = speed < 4.5f ? TravelPose.Hover : (speed > 30f ? TravelPose.FlyFast : TravelPose.Fly);
                    float drift = Vector3.Dot(_vel, transform.forward);
                    float hoverLean = Mathf.Clamp(drift * 3f, -12f, 18f);
                    float bank = speed > 5f ? TravelMath.BankFromTurn(_turnRate, 0.3f, 50f) : _flyStrafe * 12f;
                    lean = new Vector3(TravelMath.FlightBodyPitch(_vel.magnitude, _vy, hoverLean), 0f, bank);
                    break;
            }
            if (_travel == TravelMode.None && _landTimer > 0f)
            {
                _pose = TravelPose.Land;
                _poseBlend = Mathf.SmoothStep(0f, 1f, _landTimer / _landDuration);
            }
            else if (want != TravelPose.None)
            {
                _pose = want;
                _poseBlend = Mathf.MoveTowards(_poseBlend, wantBlend, dt * 5f);
            }
            else
            {
                _poseBlend = Mathf.MoveTowards(_poseBlend, 0f, dt * 4f);
                if (_poseBlend <= 0f) _pose = TravelPose.None;
            }
            _lean = Vector3.Lerp(_lean, lean, MathUtil.Damp(6f, dt));
            Anim.Travel = _pose;
            Anim.TravelBlend = _poseBlend;
            Anim.TravelLean = _lean;
            TravelSpeed = speed;
            TravelEffects(dt, speed);
        }

        private void TravelEffects(float dt, float speed)
        {
            Fx fx = Game.Fx;
            if (fx == null || _travel == TravelMode.None) return;
            if (_travel == TravelMode.Surge)
            {
                float k = Mathf.InverseLerp(12f, TravelMath.SurgeTopSpeed, speed);
                if (k > 0f) fx.TravelStreaks(transform.position + Vector3.up * 1.1f, _vel, k);
                _dustTimer -= dt;
                if (_grounded && speed > 10f && _dustTimer <= 0f)
                {
                    _dustTimer = 0.05f;
                    fx.TravelDust(transform.position, _vel, k);
                }
                _ghostTimer -= dt;
                bool firstPerson = Game.Cam != null && Game.Cam.IsFirstPerson;
                if (speed > 14f && _ghostTimer <= 0f && !firstPerson && Rig != null)
                {
                    _ghostTimer = 0.075f;
                    fx.TravelAfterimage(Rig.Body, 0.3f);
                }
                return;
            }
            Vector3 v = _vel + Vector3.up * _vy;
            float s = Mathf.InverseLerp(_travel == TravelMode.Flight ? 22f : 18f, TravelMath.FlyFast, v.magnitude);
            if (s > 0f) fx.TravelStreaks(transform.position + Vector3.up, v, s);
        }
    }
}
