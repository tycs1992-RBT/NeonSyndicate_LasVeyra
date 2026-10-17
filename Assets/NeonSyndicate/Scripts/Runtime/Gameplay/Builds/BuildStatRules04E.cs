using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Every 04E build number that combines several sources goes through here, so gear, the ward, its aspects, a brew and
    /// the BUILD page preview all read one calculation and no bonus can stack past its cap (gear affixes, ward aspects,
    /// pinnacle unlocks and UI rebuilds never add twice). Engine-free, tested by Builds04ETests.
    /// </summary>
    public static class BuildStatRules04E
    {
        /// <summary>The most the MoveSpeed affixes of all worn gear may add together.</summary>
        public const float MaxMoveAffix = 0.12f;
        /// <summary>The Void Screen's repositioning window adds this much on-foot speed while it lasts.</summary>
        public const float VoidMoveBonus = 0.3f;
        /// <summary>The highest on-foot speed multiplier any combination reaches.</summary>
        public const float MaxMoveScale = 1.35f;
        /// <summary>The parry window bonus cap the gear already used before 04E (seconds).</summary>
        public const float MaxParryBonus = 0.12f;
        /// <summary>What Frostglass adds to the parry window, inside the same cap.</summary>
        public const float FrostglassParryBonus = 0.05f;
        /// <summary>Heat (fire) damage never drops below this fraction from build sources combined.</summary>
        public const float MinHeatScale = 0.25f;
        /// <summary>The Coolant Draught brew's heat multiplier.</summary>
        public const float CoolantHeatScale = 0.7f;
        /// <summary>Gadget power bonuses (mods and sets) raise the ward's capacity by at most this fraction.</summary>
        public const float MaxWardPowerBonus = 0.6f;
        /// <summary>The Kiln Crown pinnacle's capacity bonus (one flag, never stacked by repeats).</summary>
        public const float CrownCapacityBonus = 0.25f;
        /// <summary>The starter ward runs at this fraction of the full Buckler until the level that normally unlocks it.</summary>
        public const float StarterCapacityScale = 0.8f;

        /// <summary>On-foot movement multiplier from the MoveSpeed affix total and an active Void Screen window.</summary>
        public static float MoveSpeedScale(float affixTotal, bool voidWindow)
        {
            float k = 1f + Mathf.Clamp(affixTotal, 0f, MaxMoveAffix) + (voidWindow ? VoidMoveBonus : 0f);
            return Mathf.Min(MaxMoveScale, k);
        }

        /// <summary>The parry window bonus from gear affixes plus Frostglass, capped where gear was always capped.</summary>
        public static float ParryBonus(float gearParryAffix, bool frostglass)
        {
            return Mathf.Clamp(Mathf.Max(0f, gearParryAffix) + (frostglass ? FrostglassParryBonus : 0f), 0f, MaxParryBonus);
        }

        /// <summary>
        /// The multiplier on a fire hit before the Ember Mantle buffer absorbs its share: the Coolant Draught. The steady
        /// result after the mantle is HeatTakenWithMantle.
        /// </summary>
        public static float HeatScale(bool coolant)
        {
            return coolant ? CoolantHeatScale : 1f;
        }

        /// <summary>The fraction of a fire hit that lands when the mantle still has buffer, never below MinHeatScale.</summary>
        public static float HeatTakenWithMantle(bool coolant, bool mantleBuffered)
        {
            float k = HeatScale(coolant) * (mantleBuffered ? 1f - WardRules04E.MantleAbsorbShare : 1f);
            return Mathf.Max(MinHeatScale, k);
        }

        /// <summary>
        /// Applies the heat rules to one fire hit: coolant first, then the mantle's share from its buffer, never below the
        /// floor. Returns the amount that lands; absorbedByMantle is what the buffer paid.
        /// </summary>
        public static float ApplyHeat(float amount, bool coolant, WardHeatBuffer mantle, out float absorbedByMantle)
        {
            absorbedByMantle = 0f;
            if (!(amount > 0f)) return 0f;
            float floor = amount * MinHeatScale;
            float a = amount * HeatScale(coolant);
            if (mantle != null) absorbedByMantle = mantle.Absorb(a, a - floor);
            a -= absorbedByMantle;
            return Mathf.Max(floor, a);
        }

        /// <summary>Total ward capacity bonus multiplier from gadget power, capped, times the crown.</summary>
        public static float WardBonusScale(float gadgetPowerBonus, bool crown)
        {
            return (1f + Mathf.Clamp(gadgetPowerBonus, 0f, MaxWardPowerBonus)) * (crown ? 1f + CrownCapacityBonus : 1f);
        }
    }
}
