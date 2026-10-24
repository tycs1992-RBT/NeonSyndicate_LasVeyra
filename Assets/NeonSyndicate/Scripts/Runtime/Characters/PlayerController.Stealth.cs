using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Malik's side of stealth (rules in StealthRules, the enemy side in NpcController.StealthH8S.cs and
    /// Creature.StealthH8S.cs, the HUD in StealthHud): how far his movement can be heard, the silent takedown of a
    /// person who has not noticed him (from behind on the ground, dropping from a roof edge or a ledge, or out of the
    /// air after a leap or glide) with the use button, and the Third Eye's sight pulse that lights enemies and
    /// interactables through walls for a few seconds whenever the Third Eye opens. The takedown is a FreeRunState and
    /// moves through the one CharacterController; the kill goes through the ordinary damage path, silently.
    /// </summary>
    public sealed partial class PlayerController
    {
        /// <summary>How far Malik's movement carries right now (metres); zero standing still.</summary>
        public float StealthNoiseRadius { get; private set; }
        /// <summary>The person a takedown would hit if the use button were pressed now (null for none).</summary>
        public NpcController TakedownTarget { get; private set; }
        /// <summary>The kind of takedown available on TakedownTarget.</summary>
        public TakedownKind TakedownAvailable { get; private set; }
        /// <summary>The target of the takedown in progress.</summary>
        public NpcController TakedownVictim { get { return _fr == FreeRunState.Takedown ? _tdTarget : null; } }
        /// <summary>Seconds since the Third Eye's sight pulse began (negative when there has been none).</summary>
        public float SightPulseAge { get { return _sightPulseAt < 0f ? -1f : Time.time - _sightPulseAt; } }
        /// <summary>The sight pulse is lighting things up right now.</summary>
        public bool SightPulseActive { get { float a = SightPulseAge; return a >= 0f && a <= StealthRules.PulseSeconds; } }
        /// <summary>Where the sight pulse started (its ring spreads from here).</summary>
        public Vector3 SightPulseOrigin { get { return _sightPulseOrigin; } }

        private float _sightPulseAt = -99f;
        private Vector3 _sightPulseOrigin;
        private bool _eyeWasOn;
        private float _noiseBurst;
        private float _noiseBurstUntil;
        private float _prevPeakH8S;
        private bool _prevGroundedH8S = true;
        private float _tdScanTimer;
        private NpcController _tdTarget;
        private TakedownKind _tdKind;
        private float _tdApproach;
        private Vector3 _tdFrom;
        private Vector3 _tdTo;
        private bool _tdHit;
        private bool _stealthHudRegistered;

        // ================================================================== every frame (from UpdateTravelPresentation)

        /// <summary>Noise, the Third Eye pulse and the takedown prompt. Runs every frame whatever Malik is doing.</summary>
        private void StealthFrame(float dt)
        {
            if (!_stealthHudRegistered)
            {
                _stealthHudRegistered = true;
                StealthHud.Register();
            }
            // Landing noise: the frame the feet come down after a fall (the roll and the dive report their own).
            if (_grounded && !_prevGroundedH8S && _fr == FreeRunState.None && _prevPeakH8S > 4f) NoteLandingNoise(_prevPeakH8S, false);
            _prevGroundedH8S = _grounded;
            _prevPeakH8S = _airPeakSpeed;
            float noise = IsDead ? 0f : StealthRules.MovementNoise(Crouched, _vel.magnitude, Sprinting, _travel == TravelMode.Surge, InVehicle);
            if (Time.time < _noiseBurstUntil) noise = Mathf.Max(noise, _noiseBurst);
            StealthNoiseRadius = noise;
            // The Third Eye opening sends out the sight pulse.
            bool eye = Game.Abilities != null && Game.Abilities.ThirdEyeActive;
            if (eye && !_eyeWasOn && !IsDead)
            {
                _sightPulseAt = Time.time;
                _sightPulseOrigin = ChestPosition;
                if (Game.Audio != null) Game.Audio.Play2D(SfxId.CrystalChime, 0.45f);
            }
            _eyeWasOn = eye;
            _tdScanTimer -= dt;
            if (_tdScanTimer <= 0f)
            {
                _tdScanTimer = 0.1f;
                ScanTakedown();
            }
            TrackThrownBody();
        }

        /// <summary>A landing was heard this far around (a roll is quieter).</summary>
        private void NoteLandingNoise(float fallSpeed, bool rolled)
        {
            float r = StealthRules.LandingNoise(fallSpeed, rolled);
            if (r <= _noiseBurst && Time.time < _noiseBurstUntil) return;
            _noiseBurst = r;
            _noiseBurstUntil = Time.time + 0.8f;
        }

        /// <summary>A hunter gave up on Malik: when nobody within earshot is still alarmed, say so.</summary>
        public void OnHunterLostH8S()
        {
            if (Game.Hud == null || IsDead) return;
            Vector3 p = transform.position;
            for (int i = 0; i < NpcController.All.Count; i++)
            {
                NpcController n = NpcController.All[i];
                if (n == null || n.IsDead || !n.AlarmedH8S) continue;
                if (MathUtil.FlatSqrDistance(n.transform.position, p) < 60f * 60f) return;
            }
            Game.Hud.CombatText("They lost you", new Color(0.7f, 0.9f, 1f));
        }

        // ================================================================== the takedown prompt

        private bool StealthAirborne { get { return !_grounded && (_fr == FreeRunState.None || _fr == FreeRunState.Dive) && !_climbing; } }
        private bool StealthAtEdge { get { return _fr == FreeRunState.LedgeHang || _fr == FreeRunState.WallClimb || _grounded; } }

        /// <summary>Finds the person a takedown would hit from where Malik is now (every tenth of a second).</summary>
        private void ScanTakedown()
        {
            TakedownTarget = null;
            TakedownAvailable = TakedownKind.None;
            if (IsDead || IsRagdolled || InVehicle || _climbing || _enter != EnterPhase.None || Game.Cam == null) return;
            if (_fr != FreeRunState.None && _fr != FreeRunState.LedgeHang && _fr != FreeRunState.WallClimb) return;
            if (_travel == TravelMode.Surge || _travel == TravelMode.Flight) return;
            if (Combat.State != PlayerCombat.Phase.Free) return;
            // The use button belongs to a prompt or a vehicle door while one is there.
            if (Focus != null || EnterCandidate != null) return;
            bool airborne = StealthAirborne;
            bool atEdge = StealthAtEdge;
            Vector3 pos = transform.position;
            NpcController best = null;
            TakedownKind bestKind = TakedownKind.None;
            float bestScore = float.MaxValue;
            for (int i = 0; i < NpcController.All.Count; i++)
            {
                NpcController n = NpcController.All[i];
                if (n == null || !n.StealthTargetableH8S || n.StealthHeldH8S) continue;
                Vector3 np = n.transform.position;
                float flat = MathUtil.FlatDistance(pos, np);
                if (flat > StealthRules.AirReach + 0.5f) continue;
                float heightAbove = pos.y - np.y;
                Vector3 toMe = MathUtil.Flat(pos - np);
                float facingDot = toMe.sqrMagnitude > 0.01f ? Vector3.Dot(n.transform.forward, toMe.normalized) : 1f;
                float awareness = n.AlarmedH8S ? StealthRules.Alert : n.AwarenessH8S;
                TakedownKind kind = StealthRules.Available(heightAbove, flat, facingDot, awareness, airborne, atEdge);
                if (kind == TakedownKind.None) continue;
                if (Physics.Linecast(ChestPosition, n.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                float score = flat + Mathf.Abs(heightAbove) * 0.2f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = n;
                    bestKind = kind;
                }
            }
            TakedownTarget = best;
            TakedownAvailable = bestKind;
        }

        // ================================================================== the takedown

        /// <summary>The use button with a takedown prompt showing (and nothing else claiming the button): start it.</summary>
        private bool TryStartTakedown()
        {
            if (TakedownTarget == null || TakedownAvailable == TakedownKind.None) return false;
            if (Focus != null || FindVehicleToEnter() != null) return false;
            NpcController n = TakedownTarget;
            if (n == null || !n.StealthTargetableH8S) return false;
            StartTakedown(n, TakedownAvailable);
            return true;
        }

        private void StartTakedown(NpcController n, TakedownKind kind)
        {
            // Leave any travel power, climb or hang; the takedown owns the body until the strike is done.
            _travel = TravelMode.None;
            _gliding = false;
            _leapRise = false;
            _resumeSurge = false;
            _jumpHeldFromAir = false;
            _leapCharging = false;
            SetSurgeStepping(false);
            _fr = FreeRunState.Takedown;
            _frTime = 0f;
            _tdTarget = n;
            _tdKind = kind;
            _tdHit = false;
            _tdFrom = transform.position;
            Vector3 fwd = MathUtil.Flat(n.transform.forward);
            if (fwd.sqrMagnitude < 0.01f) fwd = MathUtil.Flat(n.transform.position - transform.position);
            fwd = fwd.sqrMagnitude > 0.01f ? fwd.normalized : Vector3.forward;
            _tdTo = n.transform.position - fwd * StealthRules.StrikeStandOff;
            float heightAbove = _tdFrom.y - n.transform.position.y;
            _tdApproach = StealthRules.ApproachSeconds(kind, heightAbove);
            n.BeginHeldH8S(_tdApproach + StealthRules.StrikeSeconds + 0.25f);
            _vel = Vector3.zero;
            _vy = 0f;
            _airPeakSpeed = 0f;
            Crouched = false;
            Aiming = false;
            AimingDownSights = false;
            Sprinting = false;
            Combat.ReleaseLock();
            _yaw = MathUtil.YawOf(fwd);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            SetHandsBusy(true);
            TakedownTarget = null;
            TakedownAvailable = TakedownKind.None;
            TravelSound(TravelSounds.Clip.Whoosh, kind == TakedownKind.Ground ? 0.2f : 0.4f, 1.3f);
        }

        private void UpdateTakedown(float dt, bool canControl)
        {
            _frTime += dt;
            Combat.Tick(dt, false, Vector3.zero);
            Aiming = false;
            AimingDownSights = false;
            Sprinting = false;
            Crouched = false;
            NpcController n = _tdTarget;
            if (n == null || (n.IsDead && !_tdHit) || n.InVehicle)
            {
                EndTakedown();
                return;
            }
            Vector3 pos = transform.position;
            if (_frTime < _tdApproach)
            {
                Vector3 want = StealthRules.ApproachPoint(_tdFrom, _tdTo, _frTime / _tdApproach, _tdKind);
                Controller.Move(want - pos);
                Anim.Grounded = _tdKind == TakedownKind.Ground;
            }
            else
            {
                float u = (_frTime - _tdApproach) / StealthRules.StrikeSeconds;
                if (!_tdHit && u >= StealthRules.StrikeHitAt)
                {
                    _tdHit = true;
                    if (MathUtil.FlatDistance(transform.position, n.transform.position) < StealthRules.GroundReach + 0.6f)
                    {
                        Vector3 dir = MathUtil.Flat(n.transform.position - transform.position);
                        n.TakedownKillH8S(this, n.ChestPosition, dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward);
                        CombatDirector.HitStop(0.06f);
                        if (Game.Cam != null) Game.Cam.Shake(0.14f);
                        Pad.Rumble(0.5f, 0.3f, 0.18f);
                        if (Game.Audio != null) Game.Audio.PlayVar(SfxId.Punch, n.ChestPosition, 0.45f, 0.1f, 14f);
                        if (Game.Hud != null) Game.Hud.HitMarker(true);
                    }
                }
                // Keep the feet down through the strike (a drop takedown arrives from above).
                _vy = Controller.isGrounded ? -2f : _vy - TravelMath.FallGravity * dt;
                Controller.Move(Vector3.up * _vy * dt);
                Anim.Grounded = true;
                if (u >= 1f)
                {
                    EndTakedown();
                    return;
                }
            }
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            _vel = Vector3.zero;
            Anim.Velocity = Vector3.zero;
            Anim.VerticalVelocity = 0f;
            Anim.CrouchWeight = 0f;
            Anim.SitWeight = 0f;
            Anim.Hold = HoldType.None;
            Anim.Aiming = false;
        }

        private void EndTakedown()
        {
            _tdTarget = null;
            FreeRunEnd();
            _grounded = Controller.isGrounded || GroundProbe();
            _vy = _grounded ? -2f : 0f;
            _vel = Vector3.zero;
            _airPeakSpeed = 0f;
            SetFreeRunGroundAnim();
        }
    }
}
