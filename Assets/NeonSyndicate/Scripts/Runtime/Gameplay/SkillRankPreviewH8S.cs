using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>What one effect of a skill node gives now, at its next rank, and in total from every source.</summary>
    public struct SkillEffectPreviewH8S
    {
        public SkillStat Stat;
        public bool Multiply;
        /// <summary>This node's contribution at the current rank (a fraction, a flat amount, seconds or a factor).</summary>
        public float Now;
        /// <summary>The contribution at the next rank (equal to Now when the node is fully learned).</summary>
        public float Next;
        /// <summary>The stat in effect now and after the next rank, every source together (skills, Forge, guilds).</summary>
        public float TotalNow, TotalNext;
        /// <summary>True when totals were given (a live value set was supplied).</summary>
        public bool HasTotals;
        /// <summary>True when the next rank raises the node but a cap holds the total where it is.</summary>
        public bool Capped;
    }

    /// <summary>
    /// H8S skill readability: what the next rank of a skill node adds, in numbers. For every effect of the node it gives
    /// this node's contribution now and at the next rank, and (from the live value set that SkillTree builds from the
    /// learned skills, Forge upgrades and brews, and guild perks) the stat in effect before and after, exactly as
    /// SkillBook.Compute and SkillModValues.Finish would produce once the rank is learned, caps included. Plus the plain
    /// words for each stat. Presentation only. Engine-free, tested by ProgressionReadoutH8STests.
    /// </summary>
    public static class SkillRankPreviewH8S
    {
        private enum Kind { Factor, Chance, Flat, Millis, CritFactor }

        /// <summary>This node's contribution of one effect after ranks ranks (a factor for multiplying effects).</summary>
        public static float Contribution(SkillEffect e, int ranks)
        {
            int r = Mathf.Max(0, ranks);
            if (!e.Multiply) return e.PerRank * r;
            float k = 1f;
            for (int i = 0; i < r; i++) k *= e.PerRank;
            return k;
        }

        /// <summary>
        /// Fills into (cleared first) with one preview per effect of the node at the given rank. live is the value set in
        /// effect now (may be null: no totals); scratch is any spare value set the caller keeps (it is overwritten).
        /// Returns the count (0 for a node without stat effects, such as most signatures).
        /// </summary>
        public static int Preview(SkillNode n, int rank, SkillModValues live, SkillModValues scratch, List<SkillEffectPreviewH8S> into)
        {
            if (into == null) return 0;
            into.Clear();
            if (n == null || n.Effects == null) return 0;
            int r = Mathf.Clamp(rank, 0, n.MaxRank);
            bool grows = r < n.MaxRank;
            bool totals = live != null && scratch != null;
            if (totals)
            {
                scratch.CopyFrom(live);
                if (grows)
                {
                    foreach (SkillEffect e in n.Effects)
                    {
                        if (e.Multiply) scratch.Multiply(e.Stat, e.PerRank);
                        else scratch.Add(e.Stat, e.PerRank);
                    }
                }
                scratch.Finish();
            }
            foreach (SkillEffect e in n.Effects)
            {
                SkillEffectPreviewH8S p = new SkillEffectPreviewH8S
                {
                    Stat = e.Stat,
                    Multiply = e.Multiply,
                    Now = Contribution(e, r),
                    Next = Contribution(e, grows ? r + 1 : r),
                    HasTotals = totals
                };
                if (totals)
                {
                    p.TotalNow = live.Get(e.Stat);
                    p.TotalNext = scratch.Get(e.Stat);
                    p.Capped = grows && Mathf.Abs(p.TotalNext - p.TotalNow) < 1e-5f && Mathf.Abs(p.Next - p.Now) > 1e-6f;
                }
                into.Add(p);
            }
            return into.Count;
        }

        // ------------------------------------------------------------------ words

        private static Kind KindOf(SkillStat s)
        {
            switch (s)
            {
                case SkillStat.CritChance:
                case SkillStat.MaterialFind:
                case SkillStat.Discount:
                case SkillStat.RepGain:
                case SkillStat.FrostResist:
                case SkillStat.ElementProc:
                case SkillStat.GunPointBlank:
                case SkillStat.GunLongShot:
                case SkillStat.SalvageBonus:
                    return Kind.Chance;
                case SkillStat.StaminaMax:
                case SkillStat.HealthMax:
                case SkillStat.EnergyMax:
                case SkillStat.TonicCharges:
                case SkillStat.KillEnergy:
                    return Kind.Flat;
                case SkillStat.DodgeIFrames:
                case SkillStat.ParryWindow:
                    return Kind.Millis;
                case SkillStat.CritDamage:
                    return Kind.CritFactor;
                default:
                    return Kind.Factor;
            }
        }

        /// <summary>The plain name of a stat, capitalised ("Melee damage", "Dodge roll stamina cost").</summary>
        public static string Label(SkillStat s)
        {
            switch (s)
            {
                case SkillStat.MeleeDamage: return "Melee damage";
                case SkillStat.GunDamage: return "Gun damage";
                case SkillStat.PowerDamage: return "Crystal power damage";
                case SkillStat.CreatureDamage: return "Damage to creatures";
                case SkillStat.CritChance: return "Critical hit chance";
                case SkillStat.CritDamage: return "Critical hit damage";
                case SkillStat.StaminaMax: return "Max stamina";
                case SkillStat.StaminaRegen: return "Stamina refill speed";
                case SkillStat.DodgeCost: return "Dodge roll stamina cost";
                case SkillStat.DodgeIFrames: return "Dodge invulnerability";
                case SkillStat.GuardCost: return "Guard stamina cost";
                case SkillStat.ParryWindow: return "Parry window";
                case SkillStat.HealthMax: return "Max health";
                case SkillStat.DamageTaken: return "Damage taken";
                case SkillStat.TonicCharges: return "Shard Tonic charges";
                case SkillStat.TonicHeal: return "Shard Tonic healing";
                case SkillStat.EnergyMax: return "Max crystal energy";
                case SkillStat.EnergyRegen: return "Crystal energy refill speed";
                case SkillStat.PowerCost: return "Crystal power energy cost";
                case SkillStat.VeilDrain: return "Veil energy drain";
                case SkillStat.VeilShiftCost: return "Veil shift cost";
                case SkillStat.MaterialFind: return "Creature loot chance";
                case SkillStat.LumenGain: return "Lumen from enemies";
                case SkillStat.CashGain: return "Cash earned";
                case SkillStat.Discount: return "Shop and Forge discount";
                case SkillStat.VehicleGrip: return "Tire grip";
                case SkillStat.VehicleBoost: return "Nitro push";
                case SkillStat.CreatureSense: return "Creature notice distance";
                case SkillStat.RepGain: return "Reputation gains";
                case SkillStat.MoveSpeed: return "Sprint speed";
                case SkillStat.RiposteDamage: return "Riposte damage";
                case SkillStat.FrostResist: return "Damage cut deep in the Frostline";
                case SkillStat.KillEnergy: return "Crystal energy per takedown";
                case SkillStat.ElementProc: return "Element trigger chance";
                case SkillStat.GunPointBlank: return "Gun damage within 8 m";
                case SkillStat.GunLongShot: return "Gun damage beyond 30 m";
                case SkillStat.SalvageBonus: return "Material sale price";
                default: return s.ToString();
            }
        }

        private static string Pct(float fraction)
        {
            return UpgradeVerdictRulesH8S.SignedPercent(Mathf.RoundToInt(fraction * 100f));
        }

        /// <summary>A node's contribution in words: "+4%", "-50%" (a halving factor), "+15", "+20 ms", "+0.5x".</summary>
        public static string FormatContribution(SkillStat s, bool multiply, float v)
        {
            if (multiply) return Pct(v - 1f);
            switch (KindOf(s))
            {
                case Kind.Flat: return UpgradeVerdictRulesH8S.Signed(Mathf.RoundToInt(v));
                case Kind.Millis: return UpgradeVerdictRulesH8S.Signed(Mathf.RoundToInt(v * 1000f)) + " ms";
                case Kind.CritFactor: return (v >= 0f ? "+" : "-") + Mathf.Abs(v).ToString("0.0#", System.Globalization.CultureInfo.InvariantCulture) + "x";
                default: return Pct(v);
            }
        }

        /// <summary>A stat in effect in words, measured from its base: "+20%", "-12%", "+45", "+40 ms", "x2.0".</summary>
        public static string FormatTotal(SkillStat s, float v)
        {
            switch (KindOf(s))
            {
                case Kind.Flat: return UpgradeVerdictRulesH8S.Signed(Mathf.RoundToInt(v));
                case Kind.Millis: return UpgradeVerdictRulesH8S.Signed(Mathf.RoundToInt(v * 1000f)) + " ms";
                case Kind.CritFactor: return "x" + v.ToString("0.0#", System.Globalization.CultureInfo.InvariantCulture);
                case Kind.Chance: return Pct(v);
                default: return Pct(v - SkillModValues.BaseOf(s));
            }
        }

        /// <summary>
        /// The node line for one effect: "Melee damage: +4% at rank 1", "Melee damage: +8% now, +12% at rank 3" or, fully
        /// learned, "Melee damage: +12% (all 3 ranks)".
        /// </summary>
        public static string EffectLine(SkillNode n, int rank, SkillEffectPreviewH8S p)
        {
            int max = n != null ? n.MaxRank : 1;
            int r = Mathf.Clamp(rank, 0, max);
            string label = Label(p.Stat) + ": ";
            if (r >= max) return label + FormatContribution(p.Stat, p.Multiply, p.Now) + (max > 1 ? " (all " + max + " ranks)" : "");
            if (r == 0) return label + FormatContribution(p.Stat, p.Multiply, p.Next) + " at rank 1";
            return label + FormatContribution(p.Stat, p.Multiply, p.Now) + " now, " + FormatContribution(p.Stat, p.Multiply, p.Next) + " at rank " + (r + 1);
        }

        /// <summary>The in-effect line for one effect: "All sources: +20% → +24%", "All sources: +60% (at its limit)" or, fully learned, "All sources: +24%".</summary>
        public static string TotalLine(SkillNode n, int rank, SkillEffectPreviewH8S p)
        {
            if (!p.HasTotals) return "";
            bool grows = n != null && rank < n.MaxRank;
            if (!grows) return "All sources: " + FormatTotal(p.Stat, p.TotalNow);
            if (p.Capped) return "All sources: " + FormatTotal(p.Stat, p.TotalNow) + " (at its limit)";
            return "All sources: " + FormatTotal(p.Stat, p.TotalNow) + UpgradeVerdictRulesH8S.To + FormatTotal(p.Stat, p.TotalNext);
        }

        /// <summary>
        /// The words after learning a rank, from the previews taken just before it: "Melee damage +4% → +8%, all sources
        /// +16% → +20%" joined for every effect (empty for a node without stat effects). The all-sources part is given
        /// for a single-effect node only, so the status line stays one line.
        /// </summary>
        public static string LearnedSummary(List<SkillEffectPreviewH8S> before)
        {
            if (before == null || before.Count == 0) return "";
            string text = "";
            bool totals = before.Count == 1;
            for (int i = 0; i < before.Count; i++)
            {
                SkillEffectPreviewH8S p = before[i];
                string part = Label(p.Stat) + " " + FormatContribution(p.Stat, p.Multiply, p.Now) + UpgradeVerdictRulesH8S.To + FormatContribution(p.Stat, p.Multiply, p.Next);
                if (totals && p.HasTotals && !p.Capped) part += ", all sources " + FormatTotal(p.Stat, p.TotalNow) + UpgradeVerdictRulesH8S.To + FormatTotal(p.Stat, p.TotalNext);
                text += (i > 0 ? "; " : "") + part;
            }
            return text;
        }
    }
}
