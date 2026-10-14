using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Gear score and the city tier. Gear score is one number for the whole loadout: every equipped gear piece (six
    /// slots) and loot gun (four slots) scores its item level times a rarity factor, and the scores add up (0 to about
    /// 1,200). The city tier (1 to 5) follows from it and scales new enemies (level, health, damage) and drop quality
    /// (item level bonus and a chance to bump the rarity one step). Engine-free, tested by GearScoreTests.
    /// </summary>
    public static class GearScoreRules
    {
        public const int MaxItemLevel = 60;
        public const int TierCount = 5;

        /// <summary>Score per item level by rarity: Common to Mythic.</summary>
        public static readonly float[] RarityFactor = { 1f, 1.1f, 1.25f, 1.45f, 1.7f, 2f };

        /// <summary>Gear score needed for each tier (tier 1 at index 0).</summary>
        public static readonly int[] TierThreshold = { 0, 100, 250, 450, 700 };
        public static readonly string[] TierNames = { "Quiet Streets", "Neon Heat", "Shard Season", "Veil Fever", "Crown of Glass" };
        private static readonly int[] EnemyLevels = { 0, 2, 4, 7, 10 };
        private static readonly float[] EnemyHealth = { 1f, 1.12f, 1.28f, 1.5f, 1.8f };
        private static readonly float[] EnemyDamage = { 1f, 1.06f, 1.14f, 1.25f, 1.38f };
        private static readonly int[] LootLevels = { 0, 1, 2, 4, 6 };
        private static readonly float[] RarityBump = { 0f, 0.06f, 0.12f, 0.18f, 0.25f };

        // ------------------------------------------------------------------ item scores

        /// <summary>A gear piece's item level: three per point of power, two per upgrade.</summary>
        public static int GearItemLevel(int power, int upgrade)
        {
            return Mathf.Clamp(Mathf.Max(1, power) * 3 + Mathf.Max(0, upgrade) * 2, 1, MaxItemLevel);
        }

        /// <summary>A loot gun's item level: its level plus two per ascension.</summary>
        public static int GunItemLevel(int level, int ascensions)
        {
            return Mathf.Clamp(Mathf.Max(1, level) + Mathf.Max(0, ascensions) * 2, 1, MaxItemLevel);
        }

        public static int ItemScore(int itemLevel, Rarity r)
        {
            int k = Mathf.Clamp((int)r, 0, RarityFactor.Length - 1);
            return Mathf.RoundToInt(Mathf.Clamp(itemLevel, 0, MaxItemLevel) * RarityFactor[k]);
        }

        public static int GearScore(int power, int upgrade, Rarity r)
        {
            return ItemScore(GearItemLevel(power, upgrade), r);
        }

        public static int GunScore(int level, int ascensions, Rarity r)
        {
            return ItemScore(GunItemLevel(level, ascensions), r);
        }

        public static int Total(IList<int> scores)
        {
            int t = 0;
            if (scores == null) return 0;
            for (int i = 0; i < scores.Count; i++) t += Mathf.Max(0, scores[i]);
            return t;
        }

        // ------------------------------------------------------------------ tiers

        /// <summary>The city tier (1 to 5) a gear score reaches.</summary>
        public static int TierFor(int score)
        {
            int tier = 1;
            for (int i = 1; i < TierThreshold.Length; i++) if (score >= TierThreshold[i]) tier = i + 1;
            return tier;
        }

        /// <summary>Gear score that reaches the next tier, or -1 at the top.</summary>
        public static int NextTierScore(int tier)
        {
            return tier >= 1 && tier < TierCount ? TierThreshold[tier] : -1;
        }

        private static int Idx(int tier) { return Mathf.Clamp(tier, 1, TierCount) - 1; }

        public static string TierName(int tier) { return TierNames[Idx(tier)]; }
        public static string TierLabel(int tier) { return "CITY TIER " + Mathf.Clamp(tier, 1, TierCount) + ": " + TierName(tier).ToUpperInvariant(); }
        public static int EnemyLevelOffset(int tier) { return EnemyLevels[Idx(tier)]; }
        public static float EnemyHealthScale(int tier) { return EnemyHealth[Idx(tier)]; }
        public static float EnemyDamageScale(int tier) { return EnemyDamage[Idx(tier)]; }
        public static int LootLevelBonus(int tier) { return LootLevels[Idx(tier)]; }
        public static float RarityBumpChance(int tier) { return RarityBump[Idx(tier)]; }

        /// <summary>The level shown for enemies: the player's level plus the tier's offset.</summary>
        public static int EnemyLevel(int playerLevel, int tier)
        {
            return Mathf.Max(1, playerLevel) + EnemyLevelOffset(tier);
        }

        /// <summary>
        /// Item level for a gun drop: the generator's own rule (player level plus the source's bonus) with the tier's
        /// bonus added to the source bonus. This is the only input the tier feeds into LootGenerator.
        /// </summary>
        public static int LootItemLevel(int playerLevel, int sourceBonus, int tier)
        {
            return LootGenerator.ItemLevelFor(playerLevel, sourceBonus + LootLevelBonus(tier));
        }

        /// <summary>Extra power for gear drops: half the tier's loot level bonus.</summary>
        public static int GearPowerBonus(int tier)
        {
            return LootLevelBonus(tier) / 2;
        }

        /// <summary>
        /// Drop quality: a roll (uniform in [0, 1)) under the tier's chance lifts the rarity one step. Legendary never
        /// becomes Mythic this way, so Mythic stays special.
        /// </summary>
        public static Rarity BumpRarity(Rarity r, int tier, float roll)
        {
            if (r >= Rarity.Legendary) return r;
            return roll < RarityBumpChance(tier) ? r + 1 : r;
        }

        /// <summary>One line for the HUD corner ("GS 342  CITY TIER 3").</summary>
        public static string ShortLine(int score, int tier)
        {
            return "GS " + score + "   TIER " + Mathf.Clamp(tier, 1, TierCount);
        }
    }
}
