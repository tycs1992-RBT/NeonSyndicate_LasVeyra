using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Gyre Crescent: a press throws the faceted disc at 30 m/s (1.3 B outbound, 0.26 heat). It banks off static World
    /// geometry at most twice, turns back after 32 m of travel (or when fire is pressed again: the early recall) and
    /// returns to the hand along swept legal paths, striking each actor at most once out and once back (0.65 B) and at
    /// most three actors in all. A blocked return quenches the disc: the cradle refills after a short recovery and no
    /// free attack happens. While aiming with the disc in the cradle a dotted bank preview shows the predicted path.
    /// </summary>
    public sealed partial class WeaponController
    {
        public const float DiscRadius = 0.22f;
        /// <summary>Seconds the cradle stays empty after the disc is back (the recovery before the next throw).</summary>
        public const float DiscRecovery = 0.35f;
        public const float DiscLifetime = 5f;

        private bool _crDiscWasOut;
        private float _crDiscReadyAt;
        private readonly Vector3[] _crPreview = new Vector3[4];
        private int _crPreviewCount;

        private bool GyreTrigger(WeaponDef d, Vector3 target, bool pressed)
        {
            if (!pressed) return false;
            if (CrystalShots.HasDisc(this))
            {
                if (CrystalShots.RecallDisc(this))
                {
                    CrystalFlash("Recall", new Color(0.7f, 0.95f, 1f));
                    return true;
                }
                return false;
            }
            if (CrystalNow < _crDiscReadyAt || !CanAct) return false;
            CrystalProfile p = CrystalFocusDb.Get(CrystalBehavior.GyreCrescent);
            if (p == null) return false;
            int key = CrystalKey(d);
            if (!_crHeat.CanFire(key))
            {
                CrystalDeny("Overheated");
                _cooldown = 0.12f;
                return false;
            }
            Vector3 muzzle = MuzzlePosition;
            if (!CrystalLegalOrigin(muzzle, DiscRadius))
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
            Vector3 dir = CrystalAimDir(muzzle, target);
            CrystalShotSpec spec = CrystalShots.NewSpec(this, CrystalBehavior.GyreCrescent, d);
            if (spec == null) return false;
            spec.Secondary = CrystalSecondary(d, p);
            spec.Crit = CrystalCritRoll(d);
            if (!CrystalShots.Launch(spec, CrystalShotKind.Disc, muzzle, dir * p.Speed, DiscRadius, 0f, p.Range, DiscLifetime, null))
            {
                CrystalShots.DiscardSpec(spec);
                CrystalRefused();
                return false;
            }
            _crDiscWasOut = true;
            _crPreviewCount = 0;
            _cooldown = d.Interval;
            CrystalAfterShot(d, CrystalBehavior.GyreCrescent, muzzle, dir, p.Heat, target, 50f);
            return true;
        }

        private void GyreUpdate(WeaponDef d)
        {
            bool discOut = CrystalShots.HasDisc(this);
            if (_crDiscWasOut && !discOut) _crDiscReadyAt = CrystalNow + DiscRecovery;
            _crDiscWasOut = discOut;
            PlayerController p = Game.Player;
            if (!discOut && p != null && p.AimingDownSights && CrystalBlockReason() == null) ComputeBankPreview(p.AimPoint);
            else _crPreviewCount = 0;
        }

        /// <summary>The disc's predicted path from the muzzle: up to two banks off static World geometry within the travel budget.</summary>
        private void ComputeBankPreview(Vector3 target)
        {
            CrystalProfile p = CrystalFocusDb.Get(CrystalBehavior.GyreCrescent);
            _crPreviewCount = 0;
            if (p == null) return;
            Vector3 pos = MuzzlePosition;
            if (!CrystalLegalOrigin(pos, DiscRadius)) return;
            Vector3 dir = CrystalAimDir(pos, target);
            float left = p.Range;
            _crPreview[_crPreviewCount++] = pos;
            Rigidbody veh = IgnoreVehicle != null ? IgnoreVehicle.Body : null;
            for (int bank = 0; bank <= Mathf.Max(0, p.Count) && left > 0.5f && _crPreviewCount < _crPreview.Length; bank++)
            {
                int n = CrystalShots.Query(pos, DiscRadius, dir, left, transform, veh, IsPlayer);
                CrystalCastHit wall = new CrystalCastHit();
                bool found = false;
                for (int i = 0; i < n; i++)
                {
                    CrystalCastHit c = CrystalShots.CastAt(i);
                    if (c.Blocker)
                    {
                        wall = c;
                        found = true;
                    }
                }
                if (n < 0 || !found)
                {
                    _crPreview[_crPreviewCount++] = pos + dir * left;
                    return;
                }
                Vector3 hitPos = wall.Point + wall.Normal * (DiscRadius + 0.03f);
                _crPreview[_crPreviewCount++] = hitPos;
                left -= wall.Distance;
                if (!wall.StaticWorld || bank >= p.Count) return;
                pos = hitPos;
                dir = Vector3.Reflect(dir, wall.Normal).normalized;
            }
        }
    }
}
