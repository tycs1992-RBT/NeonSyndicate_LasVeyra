using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The loot world's save block: opened chests (with when), discovered chests, found Satchels, keys and tallies.</summary>
    [Serializable]
    public sealed class LootWorldSave
    {
        /// <summary>Indices of chests that are open right now.</summary>
        public List<int> Opened = new List<int>();
        /// <summary>Game hour (LootWorldRules.GameHours) each chest in Opened was opened at, same order.</summary>
        public List<float> OpenedAt = new List<float>();
        /// <summary>Chests the player has seen (they show on the map).</summary>
        public List<int> Discovered = new List<int>();
        /// <summary>Hidden Satchels found (never respawn).</summary>
        public List<int> Caches = new List<int>();
        /// <summary>Sealed chests whose guard crew is down this cycle (they come back when the chest respawns).</summary>
        public List<int> CrewDown = new List<int>();
        public int Keys;
        /// <summary>Chests opened in this playthrough, counting respawns.</summary>
        public int ChestsOpened;
        /// <summary>Collector rewards handed over (0 to 3).</summary>
        public int CollectorClaimed;
        /// <summary>Game day the bounty board was last posted on (-1: never).</summary>
        public int BountyDay = -1;
        /// <summary>Chest indices of today's posted bounties.</summary>
        public List<int> Bounties = new List<int>();
        public int BountiesDone;
    }

    /// <summary>
    /// The rules of chests, keys, Satchels, guards and the Collector: loot tables by chest kind and player level,
    /// respawn timing, guard scaling, bounty pay and picks, key prices and Satchel rewards. Pure maths, tested offline.
    /// </summary>
    public static class LootWorldRules
    {
        /// <summary>A chest refills two in-game days after it was opened.</summary>
        public const float RespawnHours = 48f;
        /// <summary>The minimap shows chests this close.</summary>
        public const float MinimapRange = 60f;
        /// <summary>A chest seen this close counts as discovered (it stays on the map).</summary>
        public const float DiscoverRange = 60f;
        /// <summary>Satchels found for each Collector reward, and the reward's rarity.</summary>
        public static readonly int[] CollectorMilestones = { 10, 25, 40 };
        public static readonly Rarity[] CollectorRewards = { Rarity.Rare, Rarity.Epic, Rarity.Legendary };
        /// <summary>Bounties posted on the board each game day.</summary>
        public const int BountiesPerDay = 3;
        public const int MaxKeys = 99;

        /// <summary>Absolute game time in hours: day 1 at midnight is 0.</summary>
        public static float GameHours(int day, float hours)
        {
            return Mathf.Max(0, day - 1) * 24f + Mathf.Clamp(hours, 0f, 24f);
        }

        /// <summary>True once a chest opened at openedAt has refilled at game time now.</summary>
        public static bool Respawned(float openedAt, float now)
        {
            return now - openedAt >= RespawnHours;
        }

        /// <summary>Game hours until a chest refills (0 when it already has).</summary>
        public static float HoursLeft(float openedAt, float now)
        {
            return Mathf.Max(0f, RespawnHours - (now - openedAt));
        }

        // ------------------------------------------------------------------ loot tables

        // Rarity weights per chest kind at level 1: Common, Uncommon, Rare, Epic, Legendary, Mythic.
        private static readonly float[][] BaseWeights =
        {
            new[] { 42f, 38f, 15.5f, 4f, 0.5f, 0f },
            new[] { 0f, 14f, 52f, 27.5f, 6f, 0.5f },
            new[] { 0f, 0f, 0f, 72f, 24.5f, 3.5f }
        };

        /// <summary>
        /// A chest's rarity weights at a player level (a copy). Higher levels move weight from Common and Uncommon up
        /// to Rare, Epic and Legendary (level appropriate loot); Veteran favours Epic and better like enemy drops do.
        /// </summary>
        public static float[] RarityWeights(LootChestKind kind, int level, bool veteran)
        {
            float[] w = (float[])BaseWeights[Mathf.Clamp((int)kind, 0, BaseWeights.Length - 1)].Clone();
            float t = Mathf.Clamp01((level - 1) / 40f);
            w[0] *= 1f - 0.75f * t;
            w[1] *= 1f - 0.35f * t;
            w[2] *= 1f + 0.8f * t;
            w[3] *= 1f + 1.2f * t;
            w[4] *= 1f + 1.6f * t;
            w[5] *= 1f + 1.6f * t;
            if (veteran)
            {
                w[3] *= 1.4f;
                w[4] *= 1.5f;
                w[5] *= 1.8f;
            }
            return w;
        }

        /// <summary>Picks a rarity for a chest; roll is uniform in [0, 1).</summary>
        public static Rarity RollRarity(LootChestKind kind, int level, bool veteran, float roll)
        {
            float[] w = RarityWeights(kind, level, veteran);
            float total = 0f;
            for (int i = 0; i < w.Length; i++) total += w[i];
            float x = Mathf.Clamp01(roll) * total;
            int last = 0;
            for (int i = 0; i < w.Length; i++)
            {
                if (w[i] <= 0f) continue;
                last = i;
                x -= w[i];
                if (x < 0f) return (Rarity)i;
            }
            return (Rarity)last;
        }

        /// <summary>The lowest rarity a chest kind can give (vaults: Epic).</summary>
        public static Rarity Floor(LootChestKind kind)
        {
            float[] w = BaseWeights[Mathf.Clamp((int)kind, 0, BaseWeights.Length - 1)];
            for (int i = 0; i < w.Length; i++) if (w[i] > 0f) return (Rarity)i;
            return Rarity.Common;
        }

        /// <summary>Guns in a chest: common 1 or 2, sealed 2, vault 2 or 3; roll is uniform in [0, 1).</summary>
        public static int GunCount(LootChestKind kind, float roll)
        {
            switch (kind)
            {
                case LootChestKind.Sealed: return 2;
                case LootChestKind.Vault: return roll < 0.4f ? 3 : 2;
                default: return roll < 0.35f ? 2 : 1;
            }
        }

        /// <summary>Which drop source a chest counts as for the item level bonus (sealed +1, vault +2).</summary>
        public static LootSource SourceFor(LootChestKind kind)
        {
            switch (kind)
            {
                case LootChestKind.Sealed: return LootSource.Elite;
                case LootChestKind.Vault: return LootSource.Champion;
                default: return LootSource.Normal;
            }
        }

        /// <summary>Cash in a chest, scaled by level; roll is uniform in [0, 1).</summary>
        public static int Cash(LootChestKind kind, int level, float roll)
        {
            int[] lo = { 80, 300, 900 };
            int[] hi = { 260, 800, 2200 };
            int k = Mathf.Clamp((int)kind, 0, 2);
            float baseCash = Mathf.Lerp(lo[k], hi[k], Mathf.Clamp01(roll));
            return Mathf.RoundToInt(baseCash * (1f + 0.04f * (Mathf.Clamp(level, 1, LootGenerator.MaxLevel) - 1)) / 10f) * 10;
        }

        /// <summary>The crafting materials of each region (the Inner World vaults use Veil materials).</summary>
        public static string[] RegionMaterials(LootRegion region, LootChestKind kind)
        {
            if (kind == LootChestKind.Vault) return new[] { "hollow_essence", "void_thread", "shade_essence", "rift_shard" };
            switch (region)
            {
                case LootRegion.Badlands: return new[] { "shard_chunk", "jackal_fang", "glass_chitin", "sand_core" };
                case LootRegion.Frostline: return new[] { "rime_pelt", "frost_essence", "frost_fang", "rime_feather" };
                default: return new[] { "glass_chitin", "mite_dust", "jackal_fang", "shard_chunk", "hollow_essence" };
            }
        }

        /// <summary>
        /// The materials a chest holds, added to into as (id, count): one or two common ones, more for sealed chests and
        /// vaults, a weapon core now and then (always a Prism Core in a vault).
        /// </summary>
        public static void RollMaterials(LootChestKind kind, LootRegion region, GRand r, List<KeyValuePair<string, int>> into)
        {
            string[] pool = RegionMaterials(region, kind);
            int stacks = kind == LootChestKind.Common ? 1 + (r.Value() < 0.4f ? 1 : 0) : 2;
            for (int i = 0; i < stacks; i++) Add(into, pool[r.Range(0, pool.Length)], kind == LootChestKind.Common ? r.Range(1, 3) : r.Range(2, 4));
            float core = kind == LootChestKind.Common ? 0.08f : (kind == LootChestKind.Sealed ? 0.5f : 1f);
            if (r.Value() < core) Add(into, LootDropRules.ElementCores[r.Range(0, LootDropRules.ElementCores.Length)], 1);
            if (kind == LootChestKind.Vault) Add(into, LootDropRules.CorePrism, 1);
            else if (kind == LootChestKind.Sealed && r.Value() < 0.15f) Add(into, LootDropRules.CorePrism, 1);
        }

        private static void Add(List<KeyValuePair<string, int>> into, string id, int n)
        {
            if (n <= 0 || string.IsNullOrEmpty(id)) return;
            for (int i = 0; i < into.Count; i++)
            {
                if (into[i].Key != id) continue;
                into[i] = new KeyValuePair<string, int>(id, into[i].Value + n);
                return;
            }
            into.Add(new KeyValuePair<string, int>(id, n));
        }

        // ------------------------------------------------------------------ guards and bounties

        /// <summary>Guards around a sealed chest: 3 at level 1, one more every 12 levels, one more on a posted bounty (at most 7).</summary>
        public static int GuardCount(int level, bool bounty)
        {
            return Mathf.Clamp(3 + (Mathf.Clamp(level, 1, LootGenerator.MaxLevel) - 1) / 12 + (bounty ? 1 : 0), 3, 7);
        }

        /// <summary>Guard health multiplier: grows 3.5% a level (x2.7 at level 50), 25% more on a bounty.</summary>
        public static float GuardHealthScale(int level, bool bounty)
        {
            return (1f + 0.035f * (Mathf.Clamp(level, 1, LootGenerator.MaxLevel) - 1)) * (bounty ? 1.25f : 1f);
        }

        /// <summary>The crew's leader is a champion from level 8 (always on a bounty), a Shard-Touched elite before that.</summary>
        public static bool LeaderIsChampion(int level, bool bounty)
        {
            return bounty || level >= 8;
        }

        /// <summary>Chance a fallen guard drops a Chest Key (the leader always does).</summary>
        public static float GuardKeyChance(bool leader, bool veteran)
        {
            if (leader) return 1f;
            return veteran ? 0.35f : 0.25f;
        }

        /// <summary>Chance a champion anywhere drops a Chest Key.</summary>
        public static float ChampionKeyChance(bool veteran)
        {
            return veteran ? 0.75f : 0.6f;
        }

        /// <summary>The cash a posted bounty pays when its whole crew is down.</summary>
        public static int BountyCash(int level)
        {
            return 1500 + 120 * Mathf.Clamp(level, 1, LootGenerator.MaxLevel);
        }

        /// <summary>
        /// Today's bounties: up to count sealed chests (indices from sealedIndices) that are not open, picked the same way
        /// for the same seed and day.
        /// </summary>
        public static List<int> PickBounties(IList<int> sealedIndices, ICollection<int> opened, int seed, int day, int count)
        {
            List<int> pool = new List<int>();
            foreach (int i in sealedIndices) if (opened == null || !opened.Contains(i)) pool.Add(i);
            GRand r = new GRand(seed * 7919 + day * 104729 + 17);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = r.Range(0, i + 1);
                int t = pool[i];
                pool[i] = pool[j];
                pool[j] = t;
            }
            if (pool.Count > count) pool.RemoveRange(count, pool.Count - count);
            return pool;
        }

        // ------------------------------------------------------------------ keys, Satchels and the Collector

        /// <summary>What a gun shop asks for a Chest Key.</summary>
        public static int KeyPrice(int level)
        {
            return 2500 + 100 * Mathf.Clamp(level, 1, LootGenerator.MaxLevel);
        }

        /// <summary>True when the Satchel at this index holds a Chest Key (every third one), otherwise a weapon core.</summary>
        public static bool CacheGivesKey(int index)
        {
            return index % 3 == 1;
        }

        /// <summary>The weapon core in a Satchel that holds no key: every tenth a Prism Core, the rest cycle the elements.</summary>
        public static string CacheCore(int index)
        {
            if (index % 10 == 9) return LootDropRules.CorePrism;
            return LootDropRules.ElementCores[index % LootDropRules.ElementCores.Length];
        }

        /// <summary>Collector rewards earned with this many Satchels found (0 to 3).</summary>
        public static int CollectorEarned(int found)
        {
            int n = 0;
            for (int i = 0; i < CollectorMilestones.Length; i++) if (found >= CollectorMilestones[i]) n++;
            return n;
        }

        /// <summary>Satchels still needed for the next Collector reward (0 when all are earned).</summary>
        public static int NextMilestone(int found)
        {
            for (int i = 0; i < CollectorMilestones.Length; i++) if (found < CollectorMilestones[i]) return CollectorMilestones[i];
            return 0;
        }

        /// <summary>The index of a named unique gun by its name (case and surrounding spaces ignored), or -1.</summary>
        public static int UniqueByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return -1;
            string n = name.Trim();
            for (int i = 0; i < LootUniques.All.Length; i++)
                if (string.Equals(LootUniques.All[i].Name, n, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }

        // ------------------------------------------------------------------ text

        /// <summary>A compass word for the direction from one point to another (N is +Z, E is +X).</summary>
        public static string Compass(Vector3 from, Vector3 to)
        {
            float dx = to.x - from.x, dz = to.z - from.z;
            if (dx * dx + dz * dz < 1f) return "here";
            float a = Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;
            if (a < 0f) a += 360f;
            string[] names = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            return names[Mathf.RoundToInt(a / 45f) % 8];
        }

        public static string KindName(LootChestKind kind)
        {
            switch (kind)
            {
                case LootChestKind.Sealed: return "Sealed chest";
                case LootChestKind.Vault: return "Veil Vault";
                default: return "Chest";
            }
        }

        public static string CacheKindName(LootCacheKind kind)
        {
            switch (kind)
            {
                case LootCacheKind.Ledge: return "on a tower ledge";
                case LootCacheKind.Bridge: return "under a canal bridge";
                default: return "on a rooftop";
            }
        }
    }
}
