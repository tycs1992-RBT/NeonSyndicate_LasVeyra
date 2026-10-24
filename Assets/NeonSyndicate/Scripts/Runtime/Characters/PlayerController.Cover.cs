using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Snap-to-cover for Malik (numbers in CoverRules, probes in CoverFinder, HUD and hints in CoverDirector).
    /// Near a wall, car, crate or barrier a prompt marks the spot: X on the keyboard, or A on a pad while the prompt
    /// shows, takes cover there (crouched behind low cover, standing behind high cover). In cover: move along it with
    /// the stick, aim to pop out (up over low cover, a step out at the edge of high cover), fire without aiming for
    /// blind fire, push toward an open end and tap the cover button to round the corner, look at another cover spot and
    /// hold the cover button to dash to it. Leave by pulling away, tapping X, or B (a roll). A on a pad (Space on the
    /// keyboard) vaults over low cover or climbs high cover that is low enough. Frontal hits are reduced through
    /// CoverDamageFilter.
    /// </summary>
    public sealed partial class PlayerController : ICoverUser
    {
        // ================================================================== public state (HUD, camera, enemy tactics)

        /// <summary>In cover this frame (the cover update ran last frame; vehicles, ragdolls and climbs end it).</summary>
        public bool InCover { get { return _cover && Time.frameCount - _coverFrame <= 2; } }
        /// <summary>The cover spot in use (valid only while InCover).</summary>
        public CoverSpot CoverNow { get { return _cs; } }
        /// <summary>Popped out to aim (over low cover, or leaning out at an edge of high cover).</summary>
        public bool CoverPopped { get; private set; }
        /// <summary>Firing without aiming from cover: the gun is held over or around it.</summary>
        public bool CoverBlindFiring { get { return InCover && _blindHold > 0f; } }
        /// <summary>The way Malik looks along the cover: -1 left, 1 right.</summary>
        public int CoverFacingSide { get { return _coverSide; } }
        /// <summary>Seconds in the current cover spot (enemies flush a player who stays too long).</summary>
        public float CoverTime { get { return InCover ? _coverTime : 0f; } }
        /// <summary>A cover prompt is showing (and where).</summary>
        public bool CoverPromptVisible { get { return _hasPrompt && !_cover && !_dashing && Time.frameCount - _promptFrame <= 2; } }
        public CoverSpot CoverPrompt { get { return _prompt; } }
        /// <summary>A cover-to-cover dash target is marked (and where), and how far the hold has filled (0..1).</summary>
        public bool CoverDashVisible { get { return InCover && _hasDash && !CoverPopped; } }
        public CoverSpot CoverDashSpot { get { return _dashSpot; } }
        public float CoverDashHold { get { return _coverGesture.Active && _hasDash ? Mathf.Clamp01(_coverGesture.HeldFor / CoverRules.HoldSeconds) : 0f; } }
        public bool CoverDashing { get { return _dashing && Time.frameCount - _coverFrame <= 2; } }
        /// <summary>Aiming in the middle of high cover: there is nothing to lean out of (the HUD says to move to an edge).</summary>
        public bool CoverEdgeHint { get { return InCover && _edgeHint > 0f; } }
        /// <summary>The damage filter wrapper (its LastTime tells the HUD a hit was softened by the cover).</summary>
        public CoverDamageFilter CoverFilter { get { return _coverFilter; } }

        // ICoverUser
        public bool CoverActive { get { return InCover && !IsDead; } }
        public CoverHeight CoverHeightNow { get { return _cs.Height; } }
        public Vector3 CoverNormalNow { get { return _cs.Normal; } }
        public bool CoverExposed { get { return InCover && _exposed; } }
        public bool CoverIsPlayer { get { return true; } }

        // ================================================================== state

        private bool _cover;
        private int _coverFrame = -10;
        private CoverSpot _cs;
        private Vector3 _coverBase;
        private int _coverSide = 1;
        private float _coverTime;
        private bool _exposed;
        private float _blindHold;
        private float _blindUp;
        private float _peekOff;
        private int _peekSide = 1;
        private float _refreshTimer;
        private float _pullAway;
        private float _edgeHint;
        private float _stuckFor;
        private bool _hasPrompt;
        private CoverSpot _prompt;
        private int _promptFrame = -10;
        private float _promptTimer;
        private bool _hasDash;
        private CoverSpot _dashSpot;
        private float _dashScanTimer;
        private bool _dashing;
        private CoverSpot _dashGoal;
        private float _dashT, _dashDur, _dashStall;
        private bool _pressFromPad;
        private readonly HoldGesture _coverGesture = new HoldGesture(CoverRules.HoldSeconds, 0.08f);
        private CoverDamageFilter _coverFilter;

        // ================================================================== hook used by PlayerController.cs

        /// <summary>
        /// Runs cover (taking it, moving in it, leaving it, dashing between spots). True means cover moved Malik this
        /// frame and the ordinary on-foot movement is skipped; false means ordinary movement runs.
        /// </summary>
        private bool UpdateCover(float dt, bool canControl)
        {
            // A vehicle, a ragdoll, a climb or a teleport skipped the cover update: forget the cover.
            if ((_cover || _dashing) && Time.frameCount - _coverFrame > 2)
            {
                if (_cover) ExitCover();
                _dashing = false;
            }
            // Travel powers and free-running moves (climbs, hangs, wall runs, rolls, dives) run instead of cover.
            if (_travel != TravelMode.None || _fr != FreeRunState.None)
            {
                if (_cover) ExitCover();
                _dashing = false;
                _hasPrompt = false;
                return false;
            }
            if (_dashing)
            {
                _coverFrame = Time.frameCount;
                return UpdateCoverDash(dt, canControl);
            }
            if (!_cover)
            {
                UpdateCoverPrompt(dt, canControl);
                if (_hasPrompt && canControl && Combat.State == PlayerCombat.Phase.Free && !Combat.Guarding && (Act.CoverKey || Act.PadCoverDown))
                {
                    EnterCover(_prompt);
                    return true;
                }
                return false;
            }
            _coverFrame = Time.frameCount;
            return UpdateInCover(dt, canControl);
        }

        /// <summary>
        /// Firing while in cover (from UpdateWeapons): popped out, the ordinary aimed fire runs; without aiming the gun
        /// comes up over (or around) the cover and fires blind with a wide spread. True means the shot was handled here.
        /// </summary>
        private bool CoverFire(WeaponDef d, bool held, bool press)
        {
            if (CoverDashing) return true;
            if (!InCover || CoverPopped || !d.IsGun) return false;
            _blindHold = 0.55f;
            if (_blindUp < CoverRules.BlindRaise) return true;
            if (d.Fire == FireKind.Spin) Weapons.HoldSpin();
            float spread = CoverRules.BlindSpread(Weapons.CurrentSpread(false, 0f, false));
            if (Weapons.TryFire(AimPoint, held, press, spread)) _pressBuffer = 0f;
            return true;
        }

        // ================================================================== the prompt

        private void UpdateCoverPrompt(float dt, bool canControl)
        {
            _promptTimer -= dt;
            if (_promptTimer > 0f)
            {
                if (_hasPrompt) _promptFrame = Time.frameCount;
                return;
            }
            _promptTimer = 0.1f;
            _hasPrompt = false;
            if (!_grounded || !canControl || Combat.State != PlayerCombat.Phase.Free || Weapons == null) return;
            Vector3 feet = transform.position;
            Vector3 camFwd = Game.Cam != null ? Game.Cam.FlatForward : transform.forward;
            Vector3 move = MathUtil.Flat(_vel);
            CoverSpot s;
            // Where Malik is heading first, then where the camera looks, then a little to either side of it.
            if ((move.sqrMagnitude > 0.8f && CoverFinder.ProbeFront(feet, move, CoverRules.PromptRange, out s))
                || CoverFinder.ProbeFront(feet, camFwd, CoverRules.PromptRange, out s)
                || CoverFinder.ProbeFront(feet, Quaternion.Euler(0f, 32f, 0f) * camFwd, CoverRules.PromptRange, out s)
                || CoverFinder.ProbeFront(feet, Quaternion.Euler(0f, -32f, 0f) * camFwd, CoverRules.PromptRange, out s))
            {
                _prompt = s;
                _hasPrompt = true;
                _promptFrame = Time.frameCount;
            }
        }

        // ================================================================== entering and leaving

        private void EnterCover(CoverSpot spot)
        {
            _cover = true;
            _coverFrame = Time.frameCount;
            _cs = spot;
            _coverBase = new Vector3(spot.Position.x, transform.position.y, spot.Position.z);
            _coverTime = 0f;
            _blindHold = 0f;
            _blindUp = 0f;
            _peekOff = 0f;
            _pullAway = 0f;
            _stuckFor = 0f;
            _hasDash = false;
            _hasPrompt = false;
            _refreshTimer = 0.15f;
            _coverGesture.Reset();
            // Look along the cover toward the side the camera faces.
            Vector3 camFwd = Game.Cam != null ? Game.Cam.FlatForward : transform.forward;
            float along = Vector3.Dot(camFwd, spot.Right);
            if (along < -0.1f) _coverSide = -1;
            else if (along > 0.1f) _coverSide = 1;
            if (Game.Cam != null) Game.Cam.ShoulderLeft = _coverSide < 0;
            if (Combat != null) Combat.ReleaseLock();
            _coverFilter = CoverDamageFilter.Attach(Health, this);
            Sprinting = false;
            Aiming = false;
            AimingDownSights = false;
            _vel = Vector3.zero;
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.Land, transform.position, 0.28f, 0.1f, 14f);
            if (Game.Cover != null) Game.Cover.OnPlayerEnteredCover(spot);
        }

        private void ExitCover()
        {
            if (!_cover) return;
            _cover = false;
            CoverPopped = false;
            _exposed = false;
            _blindHold = 0f;
            _blindUp = 0f;
            _peekOff = 0f;
            _hasDash = false;
            _coverGesture.Reset();
            if (_cs.Height == CoverHeight.Low) Crouched = false;
            if (Anim != null)
            {
                Anim.Cover = 0f;
                Anim.CoverBlind = 0f;
            }
        }

        // ================================================================== in cover

        private bool UpdateInCover(float dt, bool canControl)
        {
            if (!Controller.enabled) Controller.enabled = true;
            WeaponDef d = Weapons.Current;
            // Rolls, strikes and staggers leave cover and run through the ordinary movement this same frame.
            if (canControl && (Act.Dodge || Act.PadLight || (d.Fire == FireKind.Melee && (GInput.GameMouseDown(0) || Act.PadHeavy))))
            {
                ExitCover();
                return false;
            }
            if (Combat.State == PlayerCombat.Phase.Staggered || Combat.State == PlayerCombat.Phase.Attacking || Combat.State == PlayerCombat.Phase.Dodging)
            {
                ExitCover();
                return false;
            }
            Vector2 input = canControl ? GInput.Move : Vector2.zero;
            Vector3 camFwd = Game.Cam != null ? Game.Cam.FlatForward : transform.forward;
            Vector3 wish = MathUtil.RightOf(camFwd) * input.x + camFwd * input.y;
            if (wish.sqrMagnitude > 1f) wish.Normalize();
            Combat.Tick(dt, canControl, Vector3.zero);
            UpdateSprintStamina(dt);
            Stamina = Combat.Stamina / Mathf.Max(1f, Combat.MaxStamina);
            Sprinting = false;

            // Keep the surface current (a car may drive off, a wall may curve); more often while sliding along it.
            Vector3 right = _cs.Right;
            float lateralIn = Vector3.Dot(wish, right);
            _refreshTimer -= dt;
            if (_refreshTimer <= 0f)
            {
                _refreshTimer = Mathf.Abs(lateralIn) > 0.15f ? 0.06f : 0.25f;
                CoverSpot s = _cs;
                if (!CoverFinder.Refresh(_coverBase, ref s))
                {
                    ExitCover();
                    return false;
                }
                _cs = s;
                right = _cs.Right;
            }

            // The cover button: a hold dashes to the marked spot, a tap rounds a corner, leaves (X) or vaults (A, low cover).
            bool pressed = canControl && (Act.CoverKey || Act.PadCoverDown);
            if (pressed) _pressFromPad = Act.PadCoverDown;
            bool held = canControl && (_pressFromPad ? Act.PadCoverHeld : Act.CoverKeyHeld);
            HoldGesture.Result g = _coverGesture.Update(pressed, held, Time.unscaledDeltaTime);
            if (g == HoldGesture.Result.Hold && _hasDash && !CoverPopped)
            {
                StartDash();
                return true;
            }
            if (g == HoldGesture.Result.Tap)
            {
                if (CoverRules.WantsCorner(lateralIn, _cs.OpenLeft, _cs.OpenRight))
                {
                    RoundCorner(lateralIn < 0f ? -1 : 1);
                    return true;
                }
                if (!_pressFromPad)
                {
                    ExitCover();
                    return false;
                }
                if (_cs.Height == CoverHeight.Low ? TryVault() : TryCoverClimb()) return true;
            }
            if (canControl && Act.JumpKey && (_cs.Height == CoverHeight.Low ? TryVault() : TryCoverClimb())) return true;

            // Aim to pop out: stand up over low cover, step out at an edge of high cover.
            bool aimInput = canControl && GInput.Aim && d.Fire != FireKind.Melee;
            int peekSide = 0;
            if (_cs.Height == CoverHeight.High && aimInput)
            {
                peekSide = CoverRules.PeekSide(_cs.OpenLeft, _cs.OpenRight, _coverSide);
                if (peekSide != 0) _peekSide = peekSide;
                else _edgeHint = 1.2f;
            }
            _edgeHint -= dt;
            float wantOff = _cs.Height == CoverHeight.High && aimInput && peekSide != 0 ? CoverRules.PeekStep : 0f;
            _peekOff = Mathf.MoveTowards(_peekOff, wantOff, dt * 4.5f);
            CoverPopped = aimInput;
            _exposed = aimInput && (_cs.Height == CoverHeight.Low || _peekOff > CoverRules.PeekStep * 0.4f);
            Aiming = CoverPopped;
            AimingDownSights = CoverPopped;
            Crouched = _cs.Height == CoverHeight.Low && !CoverPopped;

            // Slide along the cover (never past its ends), or pull away from it to leave.
            float lateral = CoverPopped || Combat.State == PlayerCombat.Phase.Drinking ? 0f : CoverRules.ClampLateral(lateralIn, _cs.OpenLeft, _cs.OpenRight);
            if (Mathf.Abs(lateralIn) > 0.25f && !CoverPopped)
            {
                int side = lateralIn < 0f ? -1 : 1;
                if (side != _coverSide)
                {
                    _coverSide = side;
                    if (Game.Cam != null) Game.Cam.ShoulderLeft = side < 0;
                }
            }
            if (!CoverPopped && CoverRules.PullingAway(wish, _cs.Normal))
            {
                _pullAway += dt;
                if (_pullAway > CoverRules.PullAwaySeconds)
                {
                    ExitCover();
                    return false;
                }
            }
            else _pullAway = 0f;
            float speed = (_cs.Height == CoverHeight.Low ? CoverRules.MoveSpeedLow : CoverRules.MoveSpeedHigh) * Combat.SpeedScale;
            _coverBase += right * lateral * speed * dt;
            // Hug the surface: the base stays one standoff from the wall plane.
            float off = Vector3.Dot(_coverBase - _cs.WallPoint, _cs.Normal);
            _coverBase += _cs.Normal * (CoverRules.Standoff - off) * Mathf.Min(1f, dt * 10f);

            // Move the body to the base (plus the lean out of high cover), with gravity.
            Vector3 target = _coverBase + right * _peekSide * _peekOff;
            Vector3 to = MathUtil.Flat(target - transform.position);
            float maxStep = 6f * dt;
            if (to.magnitude > maxStep) to = to.normalized * maxStep;
            if (_grounded && _vy < 0f) _vy = -2f;
            else if (!_grounded) _vy -= 19.6f * dt;
            Vector3 before = transform.position;
            Controller.Move(to + Vector3.up * _vy * dt);
            _grounded = Controller.isGrounded || (_vy <= 0f && GroundProbe());
            _coverBase.y = transform.position.y;
            Vector3 moved = MathUtil.Flat(transform.position - before);
            _vel = moved / Mathf.Max(0.0001f, dt);
            // Blocked from reaching the spot for a while (a person or a moving car in the way): give the cover up.
            if (MathUtil.Flat(target - transform.position).sqrMagnitude > 0.7f * 0.7f) _stuckFor += dt;
            else _stuckFor = 0f;
            if (_stuckFor > 0.6f || !_grounded && _vy < -6f)
            {
                ExitCover();
                return true;
            }

            // Face the surface; face the aim when popped out.
            float wallYaw = MathUtil.YawOf(-_cs.Normal);
            float wantYaw = CoverPopped && Game.Cam != null ? Game.Cam.Yaw : wallYaw;
            _yaw = Mathf.MoveTowardsAngle(_yaw, wantYaw, 900f * dt);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

            // Blind fire timing (CoverFire sets the hold; the gun takes BlindRaise to come up).
            if (_blindHold > 0f)
            {
                _blindHold -= dt;
                _blindUp += dt;
            }
            else _blindUp = 0f;

            // Dash target along the camera's aim.
            _dashScanTimer -= dt;
            if (_dashScanTimer <= 0f)
            {
                _dashScanTimer = 0.15f;
                _hasDash = !CoverPopped && Game.Cam != null && CoverFinder.DashTarget(Game.Cam.AimRay, transform.position, _coverBase, out _dashSpot);
            }
            _coverTime += dt;

            // Footsteps while sliding along.
            float hs = _vel.magnitude;
            if (hs > 0.5f)
            {
                _footTimer -= dt * hs;
                if (_footTimer <= 0f)
                {
                    _footTimer = 1.25f;
                    if (Game.Audio != null) Game.Audio.PlayVar(SfxId.Footstep, transform.position, 0.12f, 0.15f, 14f);
                }
            }

            // Pose: blind fire in the middle of high cover goes over the top (the low-cover arm pose, standing).
            bool blind = _blindHold > 0f;
            int blindSide = _cs.Height == CoverHeight.High ? CoverRules.PeekSide(_cs.OpenLeft, _cs.OpenRight, _coverSide) : 0;
            Anim.Velocity = _vel;
            Anim.VerticalVelocity = _vy;
            Anim.Grounded = _grounded;
            Anim.CrouchWeight = Crouched ? 1f : 0f;
            Anim.SitWeight = 0f;
            Anim.Hold = d.Hold;
            Anim.Cover = 1f;
            Anim.CoverHigh = _cs.Height == CoverHeight.High && !(blind && blindSide == 0);
            Anim.CoverSide = blind && blindSide != 0 ? blindSide : (CoverPopped && _cs.Height == CoverHeight.High ? _peekSide : _coverSide);
            Anim.CoverBlind = blind ? 1f : 0f;
            if (blind && Rig != null) Anim.AimDirection = (AimPoint - Rig.Chest.position).normalized;
            return true;
        }

        // ================================================================== vault, corner, dash

        /// <summary>Over low cover to the far side (or onto it when it is too deep to clear, like a car).</summary>
        private bool TryVault()
        {
            Vector3 fwd = -_cs.Normal;
            float feetY = transform.position.y;
            float top = _cs.TopY;
            if (top - feetY > 1.5f) return false;
            int mask = CoverFinder.Mask;
            for (float dist = 0.45f; dist <= 2.8f; dist += 0.3f)
            {
                Vector3 p = _cs.WallPoint + fwd * dist;
                RaycastHit g;
                float from = top + 0.6f;
                if (!Physics.Raycast(new Vector3(p.x, from, p.z), Vector3.down, out g, from - feetY + 1.8f, Layers.GroundMask, QueryTriggerInteraction.Ignore)) continue;
                if (g.point.y > top - 0.2f) continue;
                if (Mathf.Abs(g.point.y - feetY) > 1.6f) continue;
                Vector3 dest = g.point + Vector3.up * 0.03f;
                if (Physics.CheckCapsule(dest + Vector3.up * 0.45f, dest + Vector3.up * 1.5f, 0.28f, mask, QueryTriggerInteraction.Ignore)) continue;
                ExitCover();
                _yaw = MathUtil.YawOf(fwd);
                transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
                _climbFrom = transform.position;
                _climbTo = dest;
                _climbTop = top;
                _climbT = 0f;
                _climbDuration = 0.42f + dist * 0.08f;
                _climbing = true;
                Controller.enabled = false;
                _vel = Vector3.zero;
                _vy = 0f;
                if (Game.Audio != null) Game.Audio.PlayVar(SfxId.Jump, transform.position, 0.3f, 0.1f, 12f);
                return true;
            }
            // Too deep to clear in one move: climb onto it instead.
            float yaw = MathUtil.YawOf(fwd);
            ExitCover();
            _yaw = yaw;
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            return TryClimb();
        }

        /// <summary>High cover that is low enough to climb (up to about 2.3 m): climb onto it, like Jump outside cover.</summary>
        private bool TryCoverClimb()
        {
            _yaw = MathUtil.YawOf(-_cs.Normal);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (!TryClimb()) return false;
            ExitCover();
            return true;
        }

        /// <summary>Round the open end of the cover onto the obstacle's side face; with no side face, look that way instead.</summary>
        private void RoundCorner(int side)
        {
            CoverSpot s;
            if (CoverFinder.AroundCorner(_cs, side, out s))
            {
                EnterCover(s);
                _coverSide = side;
                if (Game.Cam != null) Game.Cam.ShoulderLeft = side < 0;
                return;
            }
            _coverSide = side;
            if (Game.Cam != null) Game.Cam.ShoulderLeft = side < 0;
        }

        private void StartDash()
        {
            _dashGoal = _dashSpot;
            Vector3 to = MathUtil.Flat(_dashGoal.Position - transform.position);
            _dashDur = CoverRules.DashDuration(to.magnitude);
            _dashT = 0f;
            _dashStall = 0f;
            ExitCover();
            _dashing = true;
            _coverFrame = Time.frameCount;
            Crouched = false;
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.Whoosh, transform.position, 0.35f, 0.15f, 16f);
            if (Game.Cover != null) Game.Cover.OnPlayerDashed();
        }

        private bool UpdateCoverDash(float dt, bool canControl)
        {
            if (!Controller.enabled) Controller.enabled = true;
            // A roll cancels the dash (the ordinary movement runs it this frame).
            if (canControl && Act.Dodge)
            {
                _dashing = false;
                return false;
            }
            _dashT += dt;
            Vector3 to = MathUtil.Flat(_dashGoal.Position - transform.position);
            float dist = to.magnitude;
            if (dist < 0.35f || _dashT > _dashDur + 0.6f)
            {
                _dashing = false;
                CoverSpot g = _dashGoal;
                if (dist < 0.9f && CoverFinder.Refresh(transform.position, ref g)) EnterCover(g);
                return true;
            }
            Vector3 dir = to / dist;
            Vector3 step = dir * Mathf.Min(dist, CoverRules.DashSpeed * dt);
            if (_grounded && _vy < 0f) _vy = -2f;
            else if (!_grounded) _vy -= 19.6f * dt;
            Vector3 before = transform.position;
            Controller.Move(step + Vector3.up * _vy * dt);
            _grounded = Controller.isGrounded || (_vy <= 0f && GroundProbe());
            float moved = MathUtil.Flat(transform.position - before).magnitude;
            if (moved < step.magnitude * 0.25f) _dashStall += dt;
            else _dashStall = 0f;
            if (_dashStall > 0.25f)
            {
                _dashing = false;
                return false;
            }
            _vel = dir * CoverRules.DashSpeed;
            _yaw = Mathf.MoveTowardsAngle(_yaw, MathUtil.YawOf(dir), 1400f * dt);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            Combat.Tick(dt, false, Vector3.zero);
            Aiming = false;
            AimingDownSights = false;
            Sprinting = true;
            Anim.Velocity = _vel;
            Anim.VerticalVelocity = _vy;
            Anim.Grounded = _grounded;
            Anim.CrouchWeight = 0.45f;
            Anim.SitWeight = 0f;
            Anim.Hold = Weapons.Current.Hold;
            Anim.Cover = 0f;
            Anim.CoverBlind = 0f;
            return true;
        }
    }
}
