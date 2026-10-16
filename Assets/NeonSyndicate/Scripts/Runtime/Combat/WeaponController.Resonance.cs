using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The Resonance Burst's reward on the weapon side: a full magazine from the reserve, or a vented crystal caster.</summary>
    public sealed partial class WeaponController
    {
        /// <summary>
        /// Refills the current gun's magazine from its reserve (ResonanceRules.RefillRounds) and stops a reload in progress;
        /// a crystal caster sheds all its heat instead. Returns the rounds moved (1 for a vented caster, 0 for nothing).
        /// </summary>
        public int ResonanceRefill()
        {
            WeaponDef d = Current;
            if (d == null) return 0;
            int crystal;
            if (CrystalResonanceVent(d, out crystal)) return Mathf.Max(0, crystal);
            if (d.Overheats)
            {
                if (Heat <= 0.01f && !Overheated) return 0;
                Overheated = false;
                Heat = 0f;
                _ventTimer = 0f;
                return 1;
            }
            if (!d.UsesAmmo || d.ClipSize <= 0) return 0;
            int i = (int)d.Id;
            if (i < 0 || i >= Inventory.Clip.Length) return 0;
            int ammoIndex = (int)d.Ammo;
            int take = ResonanceRules.RefillRounds(d.ClipSize, Inventory.Clip[i], Inventory.Ammo[ammoIndex]);
            if (take <= 0) return 0;
            Inventory.Clip[i] += take;
            Inventory.Ammo[ammoIndex] -= take;
            if (Reloading) CancelReload();
            return take;
        }
    }
}
