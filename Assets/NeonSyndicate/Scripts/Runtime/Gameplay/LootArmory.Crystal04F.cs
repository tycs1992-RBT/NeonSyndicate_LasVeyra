using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class LootArmory
    {
        /// <summary>Cards use the live crystal definition, rather than inert legacy magazine and pellet figures.</summary>
        public static CardStats ConvertedStats04F(WeaponDef d)
        {
            CrystalBehavior b = WeaponController.BehaviorOf(d);
            CrystalProfile p = CrystalFocusDb.Get(b);
            if (d == null || p == null) return new CardStats();
            bool beam = b == CrystalBehavior.PrismThread;
            float interval = beam ? CrystalFocusDb.BeamTick : Mathf.Max(0.04f, d.Interval);
            float primary = beam ? d.Damage / interval : d.Damage;
            float pull = d.Damage * (b == CrystalBehavior.MirageChoir ? p.Count : 1);
            float shots = Mathf.Ceil(1f / Mathf.Max(0.001f, d.HeatPerShot));
            return new CardStats
            {
                Valid = true, Heat = true, Focus = true, Beam = beam,
                Damage = primary, Dps = pull / interval,
                SustainedDps = shots * pull / (shots * interval + p.VentSeconds),
                Rate = beam ? 0f : 60f / interval,
                Mag = beam ? 1f / Mathf.Max(0.01f, p.Heat) : shots,
                Reload = p.VentSeconds, Accuracy = 100f / (1f + 2f * d.SpreadAim),
                Range = p.Range, Crit = d.CritChance * 100f
            };
        }
    }
}
