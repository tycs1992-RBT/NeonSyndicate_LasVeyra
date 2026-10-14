using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The live numbers of the build sets (the six sets added in Phase 6, indexes 3 to 8 of GearSystem.SetNames).
    /// Every field is zero or false when the matching bonus is not active.
    /// </summary>
    public struct BuildBonuses
    {
        /// <summary>Extra gun damage on hits beyond GearBuildRules.FarRange.</summary>
        public float GunDamageFar;
        /// <summary>Extra gun damage on hits within GearBuildRules.NearRange.</summary>
        public float GunDamageNear;
        /// <summary>Chance per gun hit of a gear critical (x GearBuildRules.GearCritMultiplier).</summary>
        public float GearCritChance;
        /// <summary>Seconds taken off both gadget cooldowns by each gear critical.</summary>
        public float CritCooldownCut;
        /// <summary>Armor restored by each kill.</summary>
        public float KillArmor;
        /// <summary>Armor regained per second once GearBuildRules.ArmorRegenDelay has passed without being hit.</summary>
        public float ArmorRegen;
        /// <summary>Damage taken is cut by this fraction while armor is above half.</summary>
        public float HighArmorDamageCut;
        /// <summary>Gadget cooldowns are this fraction shorter.</summary>
        public float GadgetCooldown;
        /// <summary>Gadget damage, healing and shields are this fraction stronger.</summary>
        public float GadgetPower;
        /// <summary>Gadgets last this fraction longer.</summary>
        public float GadgetDuration;
        /// <summary>Seconds taken off both gadget cooldowns by a kill while a gadget is deployed.</summary>
        public float KillCooldownCut;
        /// <summary>Crystal energy restored by a kill with a gun in hand.</summary>
        public float GunKillEnergy;
        /// <summary>Extra crystal power damage.</summary>
        public float PowerDamage;
        /// <summary>Seconds taken off both gadget cooldowns by each Resonance Burst.</summary>
        public float BurstCooldownCut;
        /// <summary>Fraction of max health healed by each kill.</summary>
        public float KillHeal;
        /// <summary>Kills within NearRange refill the magazine of the gun in hand from its reserve.</summary>
        public bool CloseKillReload;
        /// <summary>Scanner marks last this fraction longer.</summary>
        public float ScannerDuration;
        /// <summary>Marked enemies take this fraction more damage.</summary>
        public float MarkedDamage;
        /// <summary>Killing a marked enemy sends a free Ripple pulse from its body.</summary>
        public bool MarkedKillPulse;
    }

    /// <summary>
    /// Resolves the gear set bonuses of a build: how many pieces of each set are worn and which 2, 3 and 4 piece
    /// bonuses that turns on. The three classic sets (0 to 2) keep their 2 and 4 piece effects inside GearSystem; this
    /// class owns the numbers of the six build sets. Engine-free, tested by GearBuildTests.
    /// </summary>
    public static class GearBuildRules
    {
        /// <summary>The first set index that this class resolves (the classic three come before it).</summary>
        public const int FirstBuildSet = 3;
        public const int SetLongsight = 3;
        public const int SetStoneward = 4;
        public const int SetGearwright = 5;
        public const int SetResonant = 6;
        public const int SetAlley = 7;
        public const int SetVanta = 8;
        public const int SetCount = 9;

        public const float FarRange = 25f;
        public const float NearRange = 10f;
        public const float GearCritMultiplier = 1.6f;
        public const float ArmorRegenDelay = 4f;
        /// <summary>A gear critical cuts gadget cooldowns at most this often (shotgun pellets would stack otherwise).</summary>
        public const float CritCutGap = 0.25f;
        /// <summary>Seconds between free Ripple pulses from marked kills.</summary>
        public const float MarkedPulseGap = 3f;
        /// <summary>Radius of the free Ripple pulse.</summary>
        public const float MarkedPulseRadius = 20f;

        /// <summary>The bonus tier a piece count reaches: 0, 2, 3 or 4. The classic sets have no 3 piece bonus.</summary>
        public static int TierOf(int set, int pieces)
        {
            if (pieces >= 4) return 4;
            if (pieces >= 3 && set >= FirstBuildSet) return 3;
            if (pieces >= 2) return 2;
            return 0;
        }

        /// <summary>Turns piece counts (indexed by set) into the live bonuses of the build sets.</summary>
        public static BuildBonuses Resolve(IList<int> pieces)
        {
            BuildBonuses b = new BuildBonuses();
            if (pieces == null) return b;
            int dead = Count(pieces, SetLongsight), bul = Count(pieces, SetStoneward), tin = Count(pieces, SetGearwright);
            int res = Count(pieces, SetResonant), hol = Count(pieces, SetAlley), van = Count(pieces, SetVanta);
            if (dead >= 2) b.GunDamageFar = 0.12f;
            if (dead >= 3) b.GearCritChance = 0.08f;
            if (dead >= 4) b.CritCooldownCut = 0.4f;
            if (bul >= 2) b.KillArmor = 15f;
            if (bul >= 3) b.ArmorRegen = 4f;
            if (bul >= 4) b.HighArmorDamageCut = 0.15f;
            if (tin >= 2) b.GadgetCooldown = 0.15f;
            if (tin >= 3)
            {
                b.GadgetPower = 0.25f;
                b.GadgetDuration = 0.2f;
            }
            if (tin >= 4) b.KillCooldownCut = 2f;
            if (res >= 2) b.GunKillEnergy = 6f;
            if (res >= 3) b.PowerDamage = 0.12f;
            if (res >= 4) b.BurstCooldownCut = 3f;
            if (hol >= 2) b.GunDamageNear = 0.12f;
            if (hol >= 3) b.KillHeal = 0.06f;
            if (hol >= 4) b.CloseKillReload = true;
            if (van >= 2) b.ScannerDuration = 0.5f;
            if (van >= 3) b.MarkedDamage = 0.1f;
            if (van >= 4) b.MarkedKillPulse = true;
            return b;
        }

        private static int Count(IList<int> pieces, int set)
        {
            return set >= 0 && set < pieces.Count ? pieces[set] : 0;
        }

        /// <summary>
        /// One gun hit's multiplier from the build: the range bonus for its distance, then a gear critical when critRoll
        /// (uniform in [0, 1)) falls under the chance.
        /// </summary>
        public static float GunHitMultiplier(BuildBonuses b, float distance, float critRoll, out bool crit)
        {
            float k = 1f;
            if (distance >= FarRange) k += b.GunDamageFar;
            if (distance <= NearRange) k += b.GunDamageNear;
            crit = b.GearCritChance > 0f && critRoll < b.GearCritChance;
            if (crit) k *= GearCritMultiplier;
            return k;
        }

        /// <summary>Damage taken multiplier from the build (Stoneward Choir's 4 piece while armor is above half).</summary>
        public static float DamageTakenScale(BuildBonuses b, float armor, float maxArmor)
        {
            if (b.HighArmorDamageCut <= 0f || maxArmor <= 0f) return 1f;
            return armor > maxArmor * 0.5f ? 1f - b.HighArmorDamageCut : 1f;
        }

        /// <summary>
        /// Text lines for the gear page: one per active bonus ("Stoneward Choir 3: armor regenerates ..."), classic sets
        /// included, in set order.
        /// </summary>
        public static List<string> ActiveLines(IList<int> pieces, string[] names, string[] bonus2, string[] bonus3, string[] bonus4)
        {
            List<string> lines = new List<string>();
            if (pieces == null || names == null) return lines;
            for (int s = 0; s < pieces.Count && s < names.Length; s++)
            {
                int n = pieces[s];
                if (n >= 2 && bonus2 != null && s < bonus2.Length && !string.IsNullOrEmpty(bonus2[s])) lines.Add(names[s] + " 2: " + bonus2[s]);
                if (n >= 3 && bonus3 != null && s < bonus3.Length && !string.IsNullOrEmpty(bonus3[s])) lines.Add(names[s] + " 3: " + bonus3[s]);
                if (n >= 4 && bonus4 != null && s < bonus4.Length && !string.IsNullOrEmpty(bonus4[s])) lines.Add(names[s] + " 4: " + bonus4[s]);
            }
            return lines;
        }
    }
}
