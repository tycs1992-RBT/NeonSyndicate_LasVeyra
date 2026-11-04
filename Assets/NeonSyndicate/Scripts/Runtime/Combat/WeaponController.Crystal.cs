using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// 04D crystal arsenal dispatch on the weapon controller (owner: the weapons runtime). A crystal focus is a loot gun
    /// whose unique index carries a CrystalBehavior; when one is in hand this dispatch owns the trigger exclusively, so
    /// the normal hitscan volley never also fires. Heat is owned per item uid (CrystalHeat), never by the slot, and is
    /// mirrored into Heat and Overheated so the existing HUD bar works; the legacy caster cooling never runs for a focus.
    /// One partial per behavior (CrystalShardwake, CrystalPrism, CrystalGyre, CrystalSunder, CrystalChoir, CrystalMirror),
    /// shared damage in CrystalHit and the crosshair overlay in CrystalHud.
    ///
    /// Trigger release: PlayerController only calls TryFire while fire is held (or a press is buffered) and gameplay owns
    /// the input. A frame without a held call is a real release only when the raw trigger is up and nothing blocks
    /// gameplay; a menu, pause, death, ragdoll, vehicle, lost focus or a swap cancels a charge without firing it.
    /// </summary>
    public sealed partial class WeaponController
    {
        private CrystalHeat _crHeat;
        private float _castPulse04G;
        private CrystalBeamIntegrator _crBeam;
        private bool _crRegistered;
        /// <summary>The heat key (item uid) of the focus in hand last frame, 0 when none.</summary>
        private int _crUid;
        private CrystalBehavior _crBehavior;
        private int _crTriggerFrame = -1;
        private bool _crHeld;
        private float _crCharge;
        private bool _crCharging;
        /// <summary>A press shot just fired and the trigger is still down: holding on charges the variation.</summary>
        private bool _crArmed;
        private bool _crChargeReady;
        private float _crDeniedAt = -99f;
        private float _crFlashUntil;
        private string _crFlash = "";
        private Color _crFlashColor = Color.white;
        private CrystalFocusRig _crRig;
        private GameObject _crRigModel;
        private CrystalFocusRig _crViewRig;
        private float _crViewRigLookAt;
        private bool _crMirrored;
        private float _crLegacyHeat;
        private bool _crLegacyOverheated;
        private string _crLastCancel = "";
        private CrystalShotSpec _crLocalSpec;

        /// <summary>The player's crystal heat book (debug page), or null before any focus was held.</summary>
        internal CrystalHeat CrystalHeatBook { get { return _crHeat; } }
        /// <summary>The Prism Thread integrator (debug page), or null before the beam was used.</summary>
        internal CrystalBeamIntegrator CrystalBeamBook { get { return _crBeam; } }
        /// <summary>Why the last crystal charge, beam or lock was cancelled (debug).</summary>
        public string CrystalLastCancel { get { return _crLastCancel; } }
        /// <summary>The crystal behavior in hand (None for every other weapon).</summary>
        public CrystalBehavior CrystalInHand { get { return _crBehavior; } }

        /// <summary>Clears the statics of the crystal controller partials (only a reusable work list today).</summary>
        public static void CrystalResetStatics()
        {
            _crRedirectWork.Clear();
        }

        /// <summary>The crystal behavior of a weapon definition (None for every fixed weapon and ordinary loot gun).</summary>
        public static CrystalBehavior BehaviorOf(WeaponDef d)
        {
            return CrystalCombatPolicy04F.Behavior(d);
        }

        /// <summary>The heat key of a focus: its stable inventory uid (a never-adopted gun falls back to its slot).</summary>
        private static int CrystalKey(WeaponDef d)
        {
            if (d == null) return 0;
            return d.Loot != null && d.Loot.Uid > 0 ? d.Loot.Uid : -1 - (int)d.Id;
        }

        private void EnsureCrystal()
        {
            if (_crHeat == null) _crHeat = new CrystalHeat();
            if (_crBeam == null) _crBeam = new CrystalBeamIntegrator();
            if (_crLocalSpec == null) _crLocalSpec = new CrystalShotSpec();
            if (!_crRegistered)
            {
                _crRegistered = true;
                CrystalShots.Register(this);
            }
        }

        /// <summary>The gameplay clock of the crystal heat book (seconds; frozen while paused).</summary>
        private float CrystalNow { get { return _crHeat != null ? _crHeat.Now : 0f; } }

        // ------------------------------------------------------------------ dispatch

        /// <summary>Crystal trigger dispatch. Returns true when it took the press (fired is the result to pass back).</summary>
        private bool CrystalTrigger(WeaponDef d, Vector3 target, bool held, bool pressed, float spread, out bool fired)
        {
            fired = false;
            CrystalBehavior b = BehaviorOf(d);
            if (b == CrystalBehavior.None) return false;
            // NPCs never carry loot guns; a focus in the wrong hands simply does nothing (never the hitscan path).
            if (!IsPlayer) return true;
            int frame = Time.frameCount;
            if (_crTriggerFrame == frame) return true;
            _crTriggerFrame = frame;
            _crHeld = held;
            EnsureCrystal();
            SyncCrystalHand(d, b);
            string block = CrystalBlockReason();
            bool vehicle = block == "vehicle";
            if (block != null && !(vehicle && b == CrystalBehavior.Shardwake))
            {
                if (CrystalBusy) CrystalCancel(block);
                else if (pressed && vehicle) CrystalDeny("Not from a vehicle");
                return true;
            }
            switch (b)
            {
                case CrystalBehavior.Shardwake:
                    fired = ShardwakeTrigger(d, target, held, pressed, spread, vehicle);
                    break;
                case CrystalBehavior.PrismThread:
                    fired = PrismTrigger(d, target, held);
                    break;
                case CrystalBehavior.GyreCrescent:
                    fired = GyreTrigger(d, target, pressed);
                    break;
                case CrystalBehavior.Sunderheart:
                    fired = SunderTrigger(d, target, held, pressed);
                    break;
                case CrystalBehavior.MirageChoir:
                    fired = ChoirTrigger(d, target, pressed);
                    break;
                case CrystalBehavior.Mirrorwake:
                    fired = MirrorTrigger(d, held);
                    break;
            }
            return true;
        }

        /// <summary>Per-frame crystal update. Returns true when it owned heat for the weapon in hand (skip legacy cooling).</summary>
        private bool CrystalUpdate(WeaponDef d, float dt)
        {
            CrystalBehavior b = BehaviorOf(d);
            if (_crHeat == null && b == CrystalBehavior.None) return false;
            if (!IsPlayer) return b != CrystalBehavior.None;
            EnsureCrystal();
            // The one cooling loop: every focus cools on the gameplay clock, in hand or not (dt is zero while paused).
            _crHeat.Tick(dt);
            SyncCrystalHand(d, b);
            UpdateFocusMarker();
            if (b == CrystalBehavior.None)
            {
                if (Anim != null) Anim.CrystalCast04G = 0f;
                _castPulse04G = 0f;
                if (_crMirrored) RestoreLegacyHeat();
                CrystalHeatCuesH8S(d, b);
                return false;
            }
            MirrorCrystalHeat(d);
            // H8S: heat band, counts and one cue per crossing (WeaponController.CrystalCuesH8S).
            CrystalHeatCuesH8S(d, b);
            switch (b)
            {
                case CrystalBehavior.GyreCrescent:
                    GyreUpdate(d);
                    break;
                case CrystalBehavior.MirageChoir:
                    ChoirUpdate(d, dt);
                    break;
                case CrystalBehavior.Mirrorwake:
                    MirrorUpdate(d);
                    break;
            }
            UpdatePulseVisual();
            FeedCrystalRig(b);
            return true;
        }

        /// <summary>Notices a new focus in hand (a swap, or a different gun equipped into the same slot) and cancels the old one's pending actions.</summary>
        private void SyncCrystalHand(WeaponDef d, CrystalBehavior b)
        {
            int key = b != CrystalBehavior.None ? CrystalKey(d) : 0;
            if (key == _crUid && b == _crBehavior) return;
            if (_crBehavior != CrystalBehavior.None) CrystalCancel("swap");
            _crUid = key;
            _crBehavior = b;
            _crDiscWasOut = false;
        }

        /// <summary>Called from LateUpdate: release or cancel a crystal charge, beam or lock.</summary>
        private void CrystalLateUpdate()
        {
            if (!IsPlayer || _crHeat == null || _crBehavior == CrystalBehavior.None) return;
            bool called = _crTriggerFrame == Time.frameCount;
            bool held = called && _crHeld;
            string block = CrystalBlockReason();
            if (block != null)
            {
                if (CrystalBusy) CrystalCancel(block);
                return;
            }
            if (_crBeamOn && !held) StopBeam("released");
            if ((_crCharging || _crArmed) && !held)
            {
                if (CrystalRawTriggerHeld())
                {
                    // Still physically held but not passed in: a dodge, guard or climb cancels; a body still turning to aim waits.
                    if (CrystalPlayerBusy()) CrystalCancel("action");
                }
                else CrystalRelease();
            }
        }

        /// <summary>A real trigger release: fires a completed charge (fan, concentrated orb, precision pulse).</summary>
        private void CrystalRelease()
        {
            bool ready = _crChargeReady;
            bool charging = _crCharging;
            float charge = _crCharge;
            _crCharging = false;
            _crArmed = false;
            _crCharge = 0f;
            _crChargeReady = false;
            WeaponDef d = Current;
            if (BehaviorOf(d) != _crBehavior) return;
            Vector3 aim = Game.Player != null ? Game.Player.AimPoint : MuzzlePosition + transform.forward * 30f;
            switch (_crBehavior)
            {
                case CrystalBehavior.Shardwake:
                    if (ready) ShardwakeFan(d, aim);
                    break;
                case CrystalBehavior.Sunderheart:
                    if (ready) SunderLaunch(d, aim, true);
                    break;
                case CrystalBehavior.Mirrorwake:
                    if (ready) MirrorPulse(d, aim);
                    else if (charging && charge > 0.05f) CrystalFlash("Hold to charge", new Color(0.8f, 0.9f, 1f));
                    break;
            }
        }

        private bool CrystalBusy
        {
            get { return _crCharging || _crArmed || _crBeamOn || (_crLocks != null && _crLocks.Count > 0); }
        }

        /// <summary>Stops any crystal charge, beam, lock or pending release without firing (swap, menu, death, vehicle).</summary>
        public void CrystalCancel(string reason)
        {
            bool any = CrystalBusy;
            _crCharging = false;
            _crArmed = false;
            _crCharge = 0f;
            _crChargeReady = false;
            if (_crBehavior != CrystalBehavior.None) Charge = 0f;
            StopBeam(reason);
            ClearCrystalLocks();
            HideCrystalBeams();
            _crPreviewCount = 0;
            if (reason == "death" || reason == "session") RemoveFocus();
            if (any) _crLastCancel = reason ?? "";
        }

        /// <summary>New game, load or title (CrystalShots.ClearAll): every pending action, the heat book and the focus are cleared.</summary>
        public void CrystalSessionReset(string reason)
        {
            CrystalCancel("session");
            if (_crHeat != null) _crHeat.Clear();
            if (_crBeam != null) _crBeam.ResetAll();
            _crDiscWasOut = false;
            _crDiscReadyAt = 0f;
            if (_crMirrored) RestoreLegacyHeat();
        }

        /// <summary>Reload pressed with a crystal focus in hand: start its forced vent. Returns true when handled.</summary>
        private bool CrystalManualVent(WeaponDef d)
        {
            CrystalBehavior b = BehaviorOf(d);
            if (b == CrystalBehavior.None) return false;
            if (!IsPlayer) return true;
            EnsureCrystal();
            CrystalProfile p = CrystalFocusDb.Get(b);
            if (p == null) return true;
            int key = CrystalKey(d);
            // A vent stops the beam and any charge (it is a deliberate action), locks stay.
            _crCharging = false;
            _crArmed = false;
            _crCharge = 0f;
            _crChargeReady = false;
            StopBeam("vent");
            if (_crHeat.StartManualVent(key, p.VentSeconds))
            {
                CrystalSounds.PlayVent(b, MuzzlePosition);
                CrystalFlash("Venting", new Color(1f, 0.7f, 0.4f));
                MirrorCrystalHeat(d);
            }
            return true;
        }

        /// <summary>The Resonance Burst's vent for a crystal focus. Returns true when it handled the refill.</summary>
        private bool CrystalResonanceVent(WeaponDef d, out int result)
        {
            result = 0;
            CrystalBehavior b = BehaviorOf(d);
            if (b == CrystalBehavior.None) return false;
            if (!IsPlayer) return true;
            EnsureCrystal();
            // At most once per ResonanceVentCooldown per uid: a beam cannot burst, vent and burst again on one contact.
            if (_crHeat.ResonanceVent(CrystalKey(d)))
            {
                result = 1;
                CrystalSounds.PlayVent(b, MuzzlePosition);
                MirrorCrystalHeat(d);
            }
            return true;
        }

        // ------------------------------------------------------------------ gating

        /// <summary>Why the player may not act with a focus right now, or null when gameplay owns the trigger.</summary>
        private string CrystalBlockReason()
        {
            if (!IsPlayer) return null;
            if (GInput.GameplayBlocked) return "menu";
            if (GInput.Broken) return "input";
            if (Game.State != GameState.Playing) return "state";
            if (GameTime.Paused) return "paused";
            if (!Application.isFocused) return "focus";
            PlayerController p = Game.Player;
            if (p == null || p.Weapons != this) return "player";
            if (p.IsDead) return "death";
            if (p.IsRagdolled) return "ragdoll";
            if (!p.ControlsEnabled) return "controls";
            if (p.InVehicle || InVehicle) return "vehicle";
            return null;
        }

        /// <summary>The physical fire button (mouse or pad trigger), regardless of who owns input.</summary>
        private static bool CrystalRawTriggerHeld()
        {
            return GInput.Mouse(0) || Pad.Held(PadButton.RT);
        }

        /// <summary>The player is doing something else deliberate (dodge, guard, swing, climb, getting into a car).</summary>
        private static bool CrystalPlayerBusy()
        {
            PlayerController p = Game.Player;
            if (p == null) return true;
            if (p.Climbing || p.Entering) return true;
            return p.Combat != null && (p.Combat.State != PlayerCombat.Phase.Free || p.Combat.Guarding);
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus && CrystalBusy) CrystalCancel("focus");
        }

        private void OnDisable()
        {
            if (CrystalBusy) CrystalCancel("disabled");
            HideCrystalBeams();
        }

        private void OnDestroy()
        {
            if (_crMirrored) RestoreLegacyHeat();
            if (_crBeamOn) CrystalSounds.StopLoop(CrystalLoopKey);
            RemoveFocus();
            if (_crFocusMarker != null) Destroy(_crFocusMarker);
            if (_crRegistered) CrystalShots.Unregister(this);
            _crRegistered = false;
        }

        // ------------------------------------------------------------------ heat mirror

        /// <summary>Shows the focus's own heat in Heat and Overheated (the HUD heat text reads those).</summary>
        private void MirrorCrystalHeat(WeaponDef d)
        {
            if (_crHeat == null) return;
            if (!_crMirrored)
            {
                // Keep the legacy caster's shared heat aside so swapping back to it neither resets nor inherits heat.
                _crLegacyHeat = Heat;
                _crLegacyOverheated = Overheated;
                _crMirrored = true;
            }
            int key = CrystalKey(d);
            Heat = _crHeat.HeatOf(key);
            Overheated = _crHeat.IsVenting(key);
            Charge = _crCharge;
        }

        private void RestoreLegacyHeat()
        {
            Heat = _crLegacyHeat;
            Overheated = _crLegacyOverheated;
            Charge = 0f;
            _crMirrored = false;
        }

        // ------------------------------------------------------------------ shared shot helpers

        /// <summary>
        /// The legal muzzle start: the line from the chest to the muzzle must be clear (a muzzle pushed through a wall
        /// never fires from the far side) and the muzzle must not sit inside geometry. False means the shot is refused.
        /// </summary>
        private bool CrystalLegalOrigin(Vector3 muzzle, float radius)
        {
            int mask = InVehicle ? (Layers.WorldMask | (1 << Layers.Debris)) : CrystalShots.BlockerMask;
            if (!InVehicle)
            {
                Vector3 chest = transform.position + Vector3.up * 1.35f;
                if (Physics.Linecast(chest, muzzle, mask, QueryTriggerInteraction.Ignore)) return false;
            }
            return !Physics.CheckSphere(muzzle, Mathf.Max(0.02f, radius), mask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>The direction from the origin to the camera's aim point (camera forward when the point is too close or behind).</summary>
        private Vector3 CrystalAimDir(Vector3 origin, Vector3 target)
        {
            Vector3 fwd = Game.Cam != null ? Game.Cam.AimRay.direction : transform.forward;
            Vector3 dir = target - origin;
            if (dir.sqrMagnitude < 1f || Vector3.Dot(dir, fwd) <= 0f) dir = fwd;
            return dir.normalized;
        }

        /// <summary>A spec for a hit dealt directly by the controller (beam ticks, pulses): no allocation, refreshed per use.</summary>
        private CrystalShotSpec CrystalLocalSpec(WeaponDef d, CrystalBehavior b, bool crit)
        {
            CrystalShotSpec s = _crLocalSpec ?? (_crLocalSpec = new CrystalShotSpec());
            s.Reset();
            s.Owner = this;
            s.Behavior = b;
            s.ActionSerial04N=b==CrystalBehavior.PrismThread?_beamSerial04N:ResonanceSystem.NextAction04N();
            s.FocusUid04N=d.Loot!=null && d.Loot.Unique>=17 && d.Loot.Unique<=22?d.Loot.Uid:0;
            s.Uid = d.Loot != null ? d.Loot.Uid : 0;
            s.WeaponId = (int)d.Id;
            s.Damage = d.Damage;
            s.Element = d.Element;
            s.ElementChance = d.ElementChance;
            s.ElementPower = d.ElementPower;
            s.Crit = crit;
            s.CritMultiplier = Mathf.Max(1f, d.CritMultiplier);
            return s;
        }

        /// <summary>The one critical roll of a shot event.</summary>
        private static bool CrystalCritRoll(WeaponDef d)
        {
            return d != null && d.CritChance > 0f && Random.value < d.CritChance;
        }

        /// <summary>Secondary damage of a focus, scaled from its live primary (so Forge changes carry over).</summary>
        private static float CrystalSecondary(WeaponDef d, CrystalProfile p)
        {
            if (d == null || p == null || p.Primary <= 0f) return 0f;
            return d.Damage * (p.Secondary / p.Primary);
        }

        /// <summary>
        /// Everything after one shot event: heat (an overheat starts the forced vent), shot sound, rig kick, recoil,
        /// stats and the noise report.
        /// </summary>
        private void CrystalAfterShot(WeaponDef d, CrystalBehavior b, Vector3 muzzle, Vector3 dir, float heat, Vector3 end, float noiseRadius)
        {
            CrystalProfile p = CrystalFocusDb.Get(b);
            CrystalProfile paid = CrystalFocusDb.Get(b);
            CrystalPayShot(b, paid != null && heat > paid.Heat + 0.0001f);
            LastShotTime = Time.time;
            ShotsFired++;
            if (Game.Stats != null) Game.Stats.Add(StatId.ShotsFired, 1);
            if (_crHeat != null && p != null && _crHeat.Add(CrystalKey(d), heat, p.VentSeconds))
            {
                CrystalSounds.PlayVent(b, muzzle);
                CrystalFlash("Overheated", new Color(1f, 0.45f, 0.3f));
            }
            CrystalSounds.PlayShot(b, muzzle);
            CrystalRigPulse();
            Bloom = Mathf.Min(Bloom + d.Recoil * 0.12f, 5f);
            if (Anim != null) Anim.Recoil = Mathf.Min(Anim.Recoil + d.Recoil * 1.6f, 22f);
            if (Game.Cam != null) Game.Cam.AddRecoil(d.Recoil);
            CrystalNoise(muzzle, end, noiseRadius);
            MirrorCrystalHeat(d);
        }

        /// <summary>Reports the noise like FireGun: a threat event and the wanted system's gunfire rule, at most every 0.3 s.</summary>
        private void CrystalNoise(Vector3 muzzle, Vector3 end, float radius)
        {
            if (Time.time - _lastThreat <= 0.3f) return;
            _lastThreat = Time.time;
            Threats.Report(muzzle, radius, gameObject, ThreatKind.Gunshot);
            // The practice lane is a crime-free range (CrystalPractice); everywhere else the usual gunfire rule applies.
            if (IsPlayer && Game.Wanted != null && !CrystalPractice.SuppressNoise) Game.Wanted.OnPlayerGunfire(muzzle, end, CrystalAimAtCreature());
        }

        private static bool CrystalAimAtCreature()
        {
            PlayerController p = Game.Player;
            if (p == null || p.AimTarget == null) return false;
            int gang;
            return TeamTag.Of(p.AimTarget.gameObject, out gang) == Team.Creature;
        }

        /// <summary>A refused action: the crosshair says why and the denied blip plays (rate limited).</summary>
        private void CrystalDeny(string why)
        {
            CrystalFlash(why, new Color(1f, 0.35f, 0.3f));
            float now = Time.unscaledTime;
            if (now - _crDeniedAt < 0.35f) return;
            _crDeniedAt = now;
            CrystalSounds.PlayDenied(MuzzlePosition);
        }

        /// <summary>A shot event the projectile owner could not admit: the cap is full (or the owner never booted).</summary>
        private void CrystalRefused()
        {
            CrystalDeny(CrystalShots.Instance == null ? "Not ready" : "Too many shots");
        }

        private void CrystalFlash(string text, Color c)
        {
            _crFlash = text ?? "";
            _crFlashColor = c;
            _crFlashUntil = Time.unscaledTime + 0.7f;
        }

        /// <summary>The loop key of this controller's beam audio (never two loops for one shooter).</summary>
        private int CrystalLoopKey { get { return GetInstanceID(); } }

        // ------------------------------------------------------------------ rig

        private CrystalFocusRig HandRig()
        {
            if (_model != _crRigModel)
            {
                _crRigModel = _model;
                _crRig = _model != null ? _model.GetComponentInChildren<CrystalFocusRig>(true) : null;
            }
            return _crRig;
        }

        private CrystalFocusRig ViewRig(CrystalBehavior b)
        {
            if (Game.Cam == null || !Game.Cam.IsFirstPerson) return null;
            if ((_crViewRig == null || _crViewRig.Behavior != b) && Time.unscaledTime >= _crViewRigLookAt)
            {
                _crViewRigLookAt = Time.unscaledTime + 0.5f;
                _crViewRig = Game.Cam.GetComponentInChildren<CrystalFocusRig>(false);
            }
            return _crViewRig != null && _crViewRig.Behavior == b ? _crViewRig : null;
        }

        private void FeedCrystalRig(CrystalBehavior b)
        {
            _castPulse04G = Mathf.MoveTowards(_castPulse04G, 0f, Time.deltaTime * 4f);
            if (Anim != null)
            {
                Anim.CrystalStyle04G = b;
                Anim.CrystalCast04G = Mathf.Max(_castPulse04G, _crBeamOn ? 1f : Mathf.Clamp01(_crCharge));
            }
            bool discOut = b == CrystalBehavior.GyreCrescent && (_crDiscWasOut || CrystalNow < _crDiscReadyAt);
            int locks = b == CrystalBehavior.MirageChoir && _crLocks != null ? _crLocks.Count : 0;
            CrystalFocusRig hand = HandRig();
            CrystalFocusRig view = ViewRig(b);
            for (int i = 0; i < 2; i++)
            {
                CrystalFocusRig r = i == 0 ? hand : view;
                if (r == null) continue;
                r.Heat = Heat;
                r.Overheated = Overheated;
                r.Charge = _crCharge;
                r.Firing = _crBeamOn;
                r.DiscOut = discOut;
                r.Locks = locks;
                r.FocusPlanted = _crFocusActive;
            }
        }

        private void CrystalRigPulse()
        {
            _castPulse04G = 1f;
            CrystalFocusRig hand = HandRig();
            if (hand != null) hand.Pulse();
            CrystalFocusRig view = ViewRig(_crBehavior);
            if (view != null) view.Pulse();
        }

        /// <summary>
        /// Never assisted, pulled or redirected onto: civilians, clerks and venue staff, companions and peaceful people
        /// (by team tag and role), whatever they are doing.
        /// </summary>
        public static bool CrystalProtected(CharacterBase c)
        {
            if (c == null || c.IsPlayer) return true;
            NpcController n = c as NpcController;
            if (n != null)
            {
                if (n.IsCreature) return false;
                if (n.Pacifist) return true;
                if (n.Role == NpcRole.Civilian || n.Role == NpcRole.Clerk || n.Role == NpcRole.Companion) return true;
            }
            int gang;
            Team t = TeamTag.Of(c.gameObject, out gang);
            return t == Team.Civilian || t == Team.Companion || t == Team.Player;
        }
    }
}
