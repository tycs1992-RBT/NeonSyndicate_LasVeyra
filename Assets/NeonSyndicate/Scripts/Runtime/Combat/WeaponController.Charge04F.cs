using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class WeaponController
    {
        /// <summary>Admission is checked before creating effects or damage. Payment happens only after an admitted shot.</summary>
        private bool CrystalChargeReady(CrystalBehavior behavior, bool charged, Vector3 aim)
        {
            if (!IsPlayer) return true;
            float cost = CrystalCombatPolicy04F.ShotCost(behavior, charged);
            if (Game.CrystalCombat == null || !Game.CrystalCombat.CanSpend(cost))
            {
                CrystalDeny("Resonance depleted: use melee or a charging cradle");
                return false;
            }
            CrystalProfile p = CrystalFocusDb.Get(behavior);
            Vector3 muzzle = MuzzlePosition;
            int n = CrystalShots.Query(muzzle, 0.05f, CrystalAimDir(muzzle, aim), p != null ? p.Range : 45f, transform,
                IgnoreVehicle != null ? IgnoreVehicle.Body : null, true);
            if (n < 0) { CrystalDeny("Shot path busy"); return false; }
            if (n > 0)
            {
                CharacterHealth health = CrystalShots.CastAt(0).Target as CharacterHealth;
                if (health != null && health.Owner != null && CrystalProtected(health.Owner))
                {
                    CrystalDeny("Protected person: lower your focus");
                    return false;
                }
            }
            return true;
        }

        private void CrystalPayShot(CrystalBehavior behavior, bool charged)
        {
            if (IsPlayer && Game.CrystalCombat != null) Game.CrystalCombat.Spend(CrystalCombatPolicy04F.ShotCost(behavior, charged));
        }
    }
}
