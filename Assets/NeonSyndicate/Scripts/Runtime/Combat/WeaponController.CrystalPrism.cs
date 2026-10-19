using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Prism Thread: a true sustained beam while fire is held. Each frame the beam sweeps from the legal muzzle toward
    /// the camera's aim point at a limited turn rate (slower and narrower while aiming), stops at the nearest blocker and
    /// draws exactly to the point where damage lands. Damage, heat and element procs happen only on the integrator's
    /// 0.1 s grid (one logical target per tick, frame-rate independent, hitches discarded); maintained aimed contact on
    /// one target ramps to 1.25x over 1.5 s and resets when contact breaks. The beam visual hides instantly on any stop.
    /// </summary>
    public sealed partial class WeaponController
    {
        public const float BeamRadiusHip = 0.18f;
        public const float BeamRadiusAimed = 0.08f;
        /// <summary>Beam sweep rates in degrees per second (hip, aimed): short controlled sweeps, never room-wide.</summary>
        public const float BeamSweepHip = 240f;
        public const float BeamSweepAimed = 90f;

        private bool _crBeamOn;
        private long _beamSerial04N;
        private Vector3 _crBeamDir = Vector3.forward;
        private Vector3 _crBeamEnd;
        private bool _crBeamBlocked;
        private float _crBeamImpactAt;
        /// <summary>The beam's skill multiplier for the current proc window (below zero: roll at the next contact tick).</summary>
        private float _crBeamSkill = -1f;
        private readonly CrystalBeamVisual[] _crBeams = new CrystalBeamVisual[7];

        /// <summary>True while the Prism Thread beam is on.</summary>
        public bool CrystalBeamOn { get { return _crBeamOn; } }

        private bool PrismTrigger(WeaponDef d, Vector3 target, bool held)
        {
            if (!held)
            {
                if (_crBeamOn) StopBeam("released");
                return false;
            }
            return BeamFrame(d, target, Time.deltaTime);
        }

        private bool BeamFrame(WeaponDef d, Vector3 target, float dt)
        {
            CrystalProfile p = CrystalFocusDb.Get(CrystalBehavior.PrismThread);
            if (p == null) return false;
            int key = CrystalKey(d);
            if (!_crHeat.CanFire(key))
            {
                StopBeam("overheated");
                CrystalDeny("Overheated");
                return false;
            }
            Vector3 muzzle = MuzzlePosition;
            if (!CrystalLegalOrigin(muzzle, 0.05f))
            {
                StopBeam("blocked");
                CrystalDeny("Blocked");
                return false;
            }
            if (!CrystalChargeReady(CrystalBehavior.PrismThread, false, target)) { StopBeam("charge"); return false; }
            bool aimed = Game.Player != null && Game.Player.AimingDownSights;
            Vector3 want = CrystalAimDir(muzzle, target);
            if (!_crBeamOn)
            {
                _crBeamOn = true;
                _beamSerial04N=ResonanceSystem.NextAction04N();
                _crBeamDir = want;
                CrystalSounds.StartLoop(CrystalLoopKey, CrystalBehavior.PrismThread, muzzle);
                LastShotTime = Time.time;
                ShotsFired++;
                if (Game.Stats != null) Game.Stats.Add(StatId.ShotsFired, 1);
                CrystalRigPulse();
            }
            else
            {
                float rate = (aimed ? BeamSweepAimed : BeamSweepHip) * Mathf.Deg2Rad * Mathf.Max(0f, dt);
                _crBeamDir = Vector3.RotateTowards(_crBeamDir, want, rate, 0f).normalized;
            }
            float radius = aimed ? BeamRadiusAimed : BeamRadiusHip;
            Vector3 end = muzzle + _crBeamDir * p.Range;
            IDamageable tgt = null;
            Collider tgtCol = null;
            Vector3 tgtNormal = -_crBeamDir;
            Collider blockCol = null;
            Vector3 blockNormal = -_crBeamDir;
            bool blocked = false;
            int n = CrystalShots.Query(muzzle, radius, _crBeamDir, p.Range, transform, IgnoreVehicle != null ? IgnoreVehicle.Body : null, IsPlayer);
            if (n < 0)
            {
                // Saturated with no proven nearest blocker: the beam is stopped at the muzzle (fail closed).
                end = muzzle + _crBeamDir * 0.3f;
                blocked = true;
            }
            else if (n > 0)
            {
                // One logical target per tick: the first receiver before the nearest blocker.
                CrystalCastHit c = CrystalShots.CastAt(0);
                end = c.Point;
                if (c.Blocker)
                {
                    blocked = true;
                    blockCol = c.Collider;
                    blockNormal = c.Normal;
                }
                else
                {
                    tgt = c.Target;
                    tgtCol = c.Collider;
                    tgtNormal = c.Normal;
                }
            }
            _crBeamEnd = end;
            _crBeamBlocked = blocked;
            CrystalBeamVisual v = CrystalBeamFor(CrystalBehavior.PrismThread);
            float ramp = _crBeam.RampProgress;
            if (n < 0 || v == null) { StopBeam("path or visual unavailable"); return false; }
            if (v != null) v.Show(muzzle, end, aimed ? 0.05f : 0.09f, 0.75f + 0.25f * ramp + (aimed ? 0.15f : 0f));
            if (tgt == null)
            {
                _crBeam.BreakContact();
                _crBeamSkill = -1f;
            }
            int ticks = _crBeam.Advance(dt);
            CrystalShotSpec spec = null;
            for (int i = 0; i < ticks; i++)
            {
                if (!_crHeat.CanFire(key)) break;
                if (!CrystalChargeReady(CrystalBehavior.PrismThread, false, target)) { StopBeam("charge"); return false; }
                CrystalPayShot(CrystalBehavior.PrismThread, false);
                int id = tgt != null && tgt.IsAlive ? CrystalShots.ActorId(tgt) : 0;
                float mul = _crBeam.TickRamp(id, aimed);
                bool proc = _crBeam.TickProc(id != 0);
                float dealt = 0f;
                if (id != 0)
                {
                    if (spec == null) spec = CrystalLocalSpec(d, CrystalBehavior.PrismThread, false);
                    // The skill critical is rolled once per 0.5 s proc window of contact, never per tick or frame.
                    if (proc) _crBeamSkill = -1f;
                    spec.SkillMul = _crBeamSkill;
                    bool fx = Time.time - _crBeamImpactAt > 0.2f;
                    if (fx) _crBeamImpactAt = Time.time;
                    bool killed;
                    dealt = CrystalStrike(spec, tgt, tgtCol, end, tgtNormal, _crBeamDir, d.Damage * mul, DamageType.Bullet, 0.3f, proc, fx, out killed);
                    _crBeamSkill = spec.SkillMul;
                }
                float heat = d.HeatPerShot;
                _crBeam.Record(dealt, heat, id != 0);
                if (_crHeat.Add(key, heat, p.VentSeconds))
                {
                    CrystalSounds.PlayVent(CrystalBehavior.PrismThread, muzzle);
                    CrystalFlash("Overheated", new Color(1f, 0.45f, 0.3f));
                    StopBeam("overheated");
                    MirrorCrystalHeat(d);
                    return false;
                }
            }
            if (blocked && blockCol != null && Time.time - _crBeamImpactAt > 0.25f)
            {
                _crBeamImpactAt = Time.time;
                CrystalBlockerImpact(blockCol, end, blockNormal);
            }
            CrystalSounds.UpdateLoop(CrystalLoopKey, muzzle, Mathf.Clamp01(0.6f + 0.4f * ramp));
            CrystalNoise(muzzle, end, 55f);
            MirrorCrystalHeat(d);
            return true;
        }

        /// <summary>Stops the beam: the visual hides at once, the loop stops, contact breaks (the partial tick carries over).</summary>
        private void StopBeam(string reason)
        {
            if (!_crBeamOn) return;
            _crBeamOn = false;
            _crBeamSkill = -1f;
            if (_crBeam != null) _crBeam.BreakContact();
            CrystalSounds.StopLoop(CrystalLoopKey);
            CrystalBeamVisual v = _crBeams[(int)CrystalBehavior.PrismThread];
            if (v != null) v.Hide();
        }

        /// <summary>This controller's beam presentation for a behavior (made once, world-space lines).</summary>
        private CrystalBeamVisual CrystalBeamFor(CrystalBehavior b)
        {
            int i = (int)b;
            if (i <= 0 || i >= _crBeams.Length) return null;
            if (_crBeams[i] == null)
            {
                try { _crBeams[i] = CrystalFocusModels.CreateBeam(b, transform); }
                catch (System.Exception e) { Log.Exception("Crystal beam visual", e); }
            }
            return _crBeams[i];
        }

        private void HideCrystalBeams()
        {
            for (int i = 0; i < _crBeams.Length; i++) if (_crBeams[i] != null) _crBeams[i].Hide();
            _crPulseHideAt = 0f;
        }
    }
}
