using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// 04D crystal arsenal dispatch on the weapon controller (owner: the weapons runtime). A crystal focus is a loot gun
    /// whose unique index carries a CrystalBehavior; when one is in hand this dispatch owns the trigger exclusively, so
    /// the normal hitscan volley never also fires. Heat is owned per item uid (CrystalHeat), never by the slot.
    /// </summary>
    public sealed partial class WeaponController
    {
        /// <summary>The crystal behavior of a weapon definition (None for every fixed weapon and ordinary loot gun).</summary>
        public static CrystalBehavior BehaviorOf(WeaponDef d)
        {
            return d != null && d.Loot != null ? CrystalFocusDb.BehaviorOfUnique(d.Loot.Unique) : CrystalBehavior.None;
        }

        /// <summary>Crystal trigger dispatch. Returns true when it took the press (fired is the result to pass back).</summary>
        private bool CrystalTrigger(WeaponDef d, Vector3 target, bool held, bool pressed, float spread, out bool fired)
        {
            fired = false;
            return false;
        }

        /// <summary>Per-frame crystal update. Returns true when it owned heat for the weapon in hand (skip legacy cooling).</summary>
        private bool CrystalUpdate(WeaponDef d, float dt)
        {
            return false;
        }

        /// <summary>Called from LateUpdate: release or cancel a crystal charge, beam or lock.</summary>
        private void CrystalLateUpdate() { }

        /// <summary>Stops any crystal charge, beam, lock or pending release without firing (swap, menu, death, vehicle).</summary>
        public void CrystalCancel(string reason) { }

        /// <summary>Reload pressed with a crystal focus in hand: start its forced vent. Returns true when handled.</summary>
        private bool CrystalManualVent(WeaponDef d)
        {
            return false;
        }

        /// <summary>The Resonance Burst's vent for a crystal focus. Returns true when it handled the refill.</summary>
        private bool CrystalResonanceVent(WeaponDef d, out int result)
        {
            result = 0;
            return false;
        }
    }
}
