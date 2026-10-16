using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Shardwake: a press fires one visible 95 m/s swept shard (1.0 B, 0.09 heat, 4 a second). Keep holding after the
    /// shot and a 0.8 s charge builds; a real release with the charge full fires a fan of three shards (0.55 B each,
    /// +-6 degrees, 0.2 heat), deliberately weaker per shard than three primaries. A menu, blocked input, death, a swap or
    /// a dodge cancels the charge without firing. From a vehicle only single shots fire (no charge).
    /// </summary>
    public sealed partial class WeaponController
    {
        public const float ShardRadius = 0.06f;
        public const float FanHalfSpread = 6f;

        private bool ShardwakeTrigger(WeaponDef d, Vector3 target, bool held, bool pressed, float spread, bool vehicle)
        {
            CrystalProfile p = CrystalFocusDb.Get(CrystalBehavior.Shardwake);
            if (p == null) return false;
            bool fired = false;
            if (pressed && !_crCharging && !_crArmed && CanAct)
            {
                fired = ShardwakeShot(d, p, target, spread);
                if (fired && held && !vehicle)
                {
                    _crArmed = true;
                    _crCharge = 0f;
                    _crChargeReady = false;
                }
            }
            if (held && _crArmed && !vehicle) AdvanceCrystalCharge(CrystalBehavior.Shardwake, p.ChargeSeconds);
            return fired;
        }

        /// <summary>Charges while the trigger stays down after a shot (the chime plays once at full).</summary>
        private void AdvanceCrystalCharge(CrystalBehavior b, float seconds)
        {
            _crCharging = true;
            if (_crHeat != null && !_crHeat.CanFire(_crUid))
            {
                // An overheat mid-charge ends it: nothing to release.
                _crCharging = false;
                _crArmed = false;
                _crCharge = 0f;
                _crChargeReady = false;
                return;
            }
            _crCharge = Mathf.Min(1f, _crCharge + Time.deltaTime / Mathf.Max(0.05f, seconds));
            if (_crCharge >= 1f && !_crChargeReady)
            {
                _crChargeReady = true;
                CrystalSounds.PlayChargeReady(b, MuzzlePosition);
            }
            Charge = _crCharge;
        }

        private bool ShardwakeShot(WeaponDef d, CrystalProfile p, Vector3 target, float spread)
        {
            int key = CrystalKey(d);
            if (!_crHeat.CanFire(key))
            {
                CrystalDeny("Overheated");
                _cooldown = 0.12f;
                return false;
            }
            Vector3 muzzle = MuzzlePosition;
            if (!CrystalLegalOrigin(muzzle, ShardRadius))
            {
                CrystalDeny("Blocked");
                _cooldown = 0.15f;
                return false;
            }
            if (!CrystalShots.CanAdmit(1))
            {
                CrystalShots.CountRefused();
                CrystalRefused();
                _cooldown = 0.15f;
                return false;
            }
            Vector3 dir = Spread(CrystalAimDir(muzzle, target), spread * 0.5f);
            CrystalShotSpec spec = CrystalShots.NewSpec(this, CrystalBehavior.Shardwake, d);
            if (spec == null)
            {
                CrystalDeny("Not ready");
                return false;
            }
            spec.Secondary = CrystalSecondary(d, p);
            spec.Crit = CrystalCritRoll(d);
            if (!CrystalShots.Launch(spec, CrystalShotKind.Shard, muzzle, dir * p.Speed, ShardRadius, 0f, p.Range, p.Range / p.Speed + 0.25f, null))
            {
                CrystalShots.DiscardSpec(spec);
                CrystalShots.CountRefused();
                CrystalRefused();
                return false;
            }
            _cooldown = d.Interval;
            CrystalAfterShot(d, CrystalBehavior.Shardwake, muzzle, dir, p.Heat, target, 65f);
            return true;
        }

        /// <summary>The charged fan: three shards as one shot event (one crit roll, one proc per actor), admitted whole or refused.</summary>
        private bool ShardwakeFan(WeaponDef d, Vector3 target)
        {
            CrystalProfile p = CrystalFocusDb.Get(CrystalBehavior.Shardwake);
            if (p == null || _crHeat == null) return false;
            int key = CrystalKey(d);
            if (!_crHeat.CanFire(key))
            {
                CrystalDeny("Overheated");
                return false;
            }
            Vector3 muzzle = MuzzlePosition;
            if (!CrystalLegalOrigin(muzzle, ShardRadius))
            {
                CrystalDeny("Blocked");
                return false;
            }
            int count = Mathf.Max(1, p.Count);
            if (!CrystalShots.CanAdmit(count))
            {
                CrystalShots.CountRefused();
                CrystalRefused();
                return false;
            }
            Vector3 aim = CrystalAimDir(muzzle, target);
            CrystalShotSpec spec = CrystalShots.NewSpec(this, CrystalBehavior.Shardwake, d);
            if (spec == null) return false;
            spec.Secondary = CrystalSecondary(d, p);
            spec.Crit = CrystalCritRoll(d);
            int launched = 0;
            for (int i = 0; i < count; i++)
            {
                Vector3 dir = CrystalShotRules.RotateAboutUp(aim, CrystalShotRules.FanOffset(i, count, FanHalfSpread)).normalized;
                if (CrystalShots.Launch(spec, CrystalShotKind.FanShard, muzzle, dir * p.Speed, ShardRadius, 0f, p.Range, p.Range / p.Speed + 0.25f, null)) launched++;
            }
            if (launched == 0)
            {
                CrystalShots.DiscardSpec(spec);
                CrystalRefused();
                return false;
            }
            _cooldown = Mathf.Max(_cooldown, d.Interval);
            CrystalAfterShot(d, CrystalBehavior.Shardwake, muzzle, aim, p.ChargedHeat, target, 65f);
            if (Game.Cam != null) Game.Cam.Shake(0.06f);
            return true;
        }
    }
}
