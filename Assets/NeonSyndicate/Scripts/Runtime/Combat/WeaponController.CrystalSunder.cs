using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Sunderheart (the Foreman's signature): a press launches a slow 24 m/s orb with a slight drop (0.35 heat, one a
    /// second). On its first contact a contained gravity pulse pulls minor hostiles it can see a bounded distance for
    /// 0.35 s (bosses only lose capped poise; civilians, cars and scenery never move), then the mineral rupture deals
    /// explosion damage: 3.2 B at the centre, 1.4 B splash within 3.5 m, line-of-sight gated, at most six victims. Keep
    /// holding after a launch to concentrate the next orb (2.5 m, 1.5x centre, 0.5 heat); a real release fires it.
    /// </summary>
    public sealed partial class WeaponController
    {
        public const float OrbRadius = 0.2f;
        public const float OrbGravity = 2.5f;

        private bool SunderTrigger(WeaponDef d, Vector3 target, bool held, bool pressed)
        {
            CrystalProfile p = CrystalFocusDb.Get(CrystalBehavior.Sunderheart);
            if (p == null) return false;
            bool fired = false;
            if (pressed && !_crCharging && !_crArmed && CanAct)
            {
                fired = SunderLaunch(d, target, false);
                if (fired && held)
                {
                    _crArmed = true;
                    _crCharge = 0f;
                    _crChargeReady = false;
                }
            }
            // The concentration is full exactly when the launch cycle is ready again, so the release always fires.
            if (held && _crArmed) AdvanceCrystalCharge(CrystalBehavior.Sunderheart, Mathf.Max(p.ChargeSeconds, d.Interval));
            return fired;
        }

        private bool SunderLaunch(WeaponDef d, Vector3 target, bool concentrated)
        {
            CrystalProfile p = CrystalFocusDb.Get(CrystalBehavior.Sunderheart);
            if (p == null || _crHeat == null) return false;
            int key = CrystalKey(d);
            if (!_crHeat.CanFire(key))
            {
                CrystalDeny("Overheated");
                _cooldown = Mathf.Max(_cooldown, 0.12f);
                return false;
            }
            if (concentrated && _cooldown > 0.05f) return false;
            Vector3 muzzle = MuzzlePosition;
            if (!CrystalLegalOrigin(muzzle, OrbRadius))
            {
                CrystalDeny("Blocked");
                _cooldown = Mathf.Max(_cooldown, 0.15f);
                return false;
            }
            if (!CrystalShots.CanAdmit(1))
            {
                CrystalShots.CountRefused();
                CrystalRefused();
                _cooldown = Mathf.Max(_cooldown, 0.15f);
                return false;
            }
            Vector3 dir = CrystalAimDir(muzzle, target);
            CrystalShotSpec spec = CrystalShots.NewSpec(this, CrystalBehavior.Sunderheart, d);
            if (spec == null) return false;
            spec.Concentrated = concentrated;
            spec.Damage = d.Damage * (concentrated ? CrystalShotRules.ConcentratedCentreScale : 1f);
            spec.Secondary = CrystalSecondary(d, p);
            spec.Radius = CrystalShotRules.RuptureRadius(concentrated, p.Radius);
            spec.Crit = CrystalCritRoll(d);
            // A small lift so the slow orb's drop lands near the crosshair at mid range.
            Vector3 vel = dir * p.Speed + Vector3.up * (OrbGravity * 0.35f);
            if (!CrystalShots.Launch(spec, CrystalShotKind.Orb, muzzle, vel, OrbRadius, OrbGravity, p.Range, p.Range / p.Speed + 0.6f, null))
            {
                CrystalShots.DiscardSpec(spec);
                CrystalRefused();
                return false;
            }
            _cooldown = d.Interval;
            CrystalAfterShot(d, CrystalBehavior.Sunderheart, muzzle, dir, concentrated ? p.ChargedHeat : p.Heat, target, 90f);
            if (Game.Cam != null) Game.Cam.Shake(concentrated ? 0.1f : 0.05f);
            return true;
        }
    }
}
