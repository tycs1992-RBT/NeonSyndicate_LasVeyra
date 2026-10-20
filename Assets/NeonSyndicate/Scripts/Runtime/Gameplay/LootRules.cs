using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Who dropped a gun: decides the drop chance, the rarity table and the weapon cores.</summary>
    public enum LootSource
    {
        Normal = 0,
        /// <summary>A Shard-Touched elite.</summary>
        Elite = 1,
        Champion = 2,
        /// <summary>A creature boss (Glacier Titan, Mirage Djinn, Shard Golem Foreman, Mirror Wraith Queen).</summary>
        Boss = 3,
        Creature = 4,
        EliteCreature = 5,
        /// <summary>The great beasts (Dune Wyrm, Quartz Drake).</summary>
        GreatBeast = 6
    }

    /// <summary>
    /// Drop tables: how often a kill drops a gun, which rarity it rolls, the pity rule and the weapon cores. Veteran
    /// difficulty drops more often and favours Epic and better. Pure maths, tested offline.
    /// </summary>
    public static class LootDropRules
    {
        /// <summary>The toughest single body shot a gun may deal: a gang member (110 health) made Shard-Touched (x3.2).</summary>
        public const float WeakestEliteHealth = 352f;
        /// <summary>Gun drops in a row without an Epic or better before the next one is forced to Epic.</summary>
        public const int PityLimit = 20;
        /// <summary>Most gun drops lying in the world at once (the oldest, lowest rarity one goes first).</summary>
        public const int MaxLiveDrops = 32;

        public const string CoreVolt = "core_volt";
        public const string CoreEmber = "core_ember";
        public const string CoreFrost = "core_frost";
        public const string CoreVoid = "core_void";
        public const string CorePrism = "core_prism";
        public const string CoreApex = "core_apex";
        public static readonly string[] ElementCores = { CoreVolt, CoreEmber, CoreFrost, CoreVoid };

        // Rarity weights per source: Common, Uncommon, Rare, Epic, Legendary, Mythic.
        private static readonly float[][] Weights =
        {
            new[] { 60f, 31f, 7.5f, 1.3f, 0.2f, 0f },
            new[] { 0f, 0f, 62f, 30f, 7.4f, 0.6f },
            new[] { 0f, 0f, 0f, 74f, 23f, 3f },
            new[] { 0f, 0f, 0f, 0f, 85f, 15f },
            new[] { 55f, 33f, 10f, 2f, 0f, 0f },
            new[] { 0f, 0f, 60f, 32f, 7.5f, 0.5f },
            new[] { 0f, 0f, 0f, 70f, 27f, 3f }
        };

        private static readonly float[] VeteranBoost = { 1f, 1f, 1f, 1.4f, 1.5f, 1.8f };

        /// <summary>The rarity weights for a source (a copy). Veteran boosts Epic (x1.4), Legendary (x1.5) and Mythic (x1.8).</summary>
        public static float[] RarityWeights(LootSource s, bool veteran)
        {
            float[] w = (float[])Weights[Mathf.Clamp((int)s, 0, Weights.Length - 1)].Clone();
            if (veteran) for (int i = 0; i < w.Length; i++) w[i] *= VeteranBoost[i];
            return w;
        }

        /// <summary>Picks a rarity from the source's table; roll is a uniform number in [0, 1).</summary>
        public static Rarity RollRarity(LootSource s, bool veteran, float roll)
        {
            float[] w = RarityWeights(s, veteran);
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

        /// <summary>The lowest rarity a source can drop (elites Rare, champions Epic, bosses Legendary).</summary>
        public static Rarity Floor(LootSource s)
        {
            float[] w = Weights[Mathf.Clamp((int)s, 0, Weights.Length - 1)];
            for (int i = 0; i < w.Length; i++) if (w[i] > 0f) return (Rarity)i;
            return Rarity.Common;
        }

        /// <summary>Chance that a kill drops a gun at all. Elites, champions and bosses always do.</summary>
        public static float GunChance(LootSource s, NpcRole role, bool veteran)
        {
            float c;
            switch (s)
            {
                case LootSource.Normal:
                    switch (role)
                    {
                        case NpcRole.Gang: c = 0.1f; break;
                        case NpcRole.Police: c = 0.06f; break;
                        case NpcRole.Swat: c = 0.14f; break;
                        case NpcRole.Enemy: c = 0.12f; break;
                        default: c = 0f; break;
                    }
                    break;
                case LootSource.Creature: c = 0.02f; break;
                case LootSource.EliteCreature: c = 0.25f; break;
                case LootSource.GreatBeast: c = 0.6f; break;
                default: return 1f;
            }
            return Mathf.Min(1f, c * (veteran ? 1.25f : 1f));
        }

        /// <summary>Item level bonus over the player's level for tougher sources.</summary>
        public static int LevelBonus(LootSource s)
        {
            switch (s)
            {
                case LootSource.Elite:
                case LootSource.EliteCreature: return 1;
                case LootSource.Champion:
                case LootSource.GreatBeast: return 2;
                case LootSource.Boss: return 3;
                default: return 0;
            }
        }

        /// <summary>
        /// The pity rule: every gun drop below Epic counts up; the drop that reaches PityLimit becomes Epic. Any Epic or
        /// better resets the count. Returns the rarity to use.
        /// </summary>
        public static Rarity ApplyPity(Rarity rolled, ref int counter)
        {
            if (rolled >= Rarity.Epic)
            {
                counter = 0;
                return rolled;
            }
            counter++;
            if (counter >= PityLimit)
            {
                counter = 0;
                return Rarity.Epic;
            }
            return rolled;
        }

        public static string CoreFor(Element e)
        {
            switch (e)
            {
                case Element.Volt: return CoreVolt;
                case Element.Ember: return CoreEmber;
                case Element.Frost: return CoreFrost;
                case Element.Void: return CoreVoid;
                default: return null;
            }
        }

        /// <summary>
        /// Weapon cores a kill drops, added to into as (material id, count). hint picks the elemental core's element
        /// (Element.None for a random one). Champions always drop one; bosses drop several and sometimes an Apex Core.
        /// </summary>
        public static void RollCores(LootSource s, bool veteran, GRand r, Element hint, List<KeyValuePair<string, int>> into)
        {
            float k = veteran ? 1.25f : 1f;
            int elemental = 0, prism = 0, apex = 0;
            switch (s)
            {
                case LootSource.Elite:
                    if (r.Value() < 0.35f * k) elemental = 1;
                    break;
                case LootSource.EliteCreature:
                    if (r.Value() < 0.25f * k) elemental = 1;
                    break;
                case LootSource.Champion:
                    elemental = 1;
                    if (r.Value() < 0.3f * k) prism = 1;
                    break;
                case LootSource.GreatBeast:
                    if (r.Value() < 0.5f * k) elemental = 1;
                    if (r.Value() < 0.15f * k) prism = 1;
                    break;
                case LootSource.Boss:
                    elemental = 2;
                    prism = 1;
                    if (r.Value() < 0.25f * k) apex = 1;
                    break;
            }
            for (int i = 0; i < elemental; i++)
            {
                string id = hint != Element.None ? CoreFor(hint) : ElementCores[r.Range(0, ElementCores.Length)];
                Add(into, id, 1);
            }
            Add(into, CorePrism, prism);
            Add(into, CoreApex, apex);
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

        /// <summary>How long a gun drop lies in the world before it fades (seconds).</summary>
        public static float DespawnSeconds(Rarity r)
        {
            switch (r)
            {
                case Rarity.Common:
                case Rarity.Uncommon: return 120f;
                case Rarity.Rare: return 180f;
                default: return 300f;
            }
        }

        /// <summary>Height of the light beam over a drop (metres): Legendary and Mythic tower over the street.</summary>
        public static float BeamHeight(Rarity r)
        {
            float[] h = { 1.6f, 2.4f, 3.6f, 6f, 14f, 22f };
            return h[Mathf.Clamp((int)r, 0, h.Length - 1)];
        }
    }

    /// <summary>Loot gun prices: what a gun shop pays and asks, and the shop's rotating stock.</summary>
    public static class LootEconomy
    {
        private static readonly int[] SellBase = { 40, 90, 220, 600, 1600, 4000 };

        public static int SellValue(LootGun g)
        {
            if (g == null) return 0;
            return Mathf.RoundToInt(SellBase[Mathf.Clamp((int)g.Rarity, 0, 5)] * (1f + 0.04f * (g.Level - 1)));
        }

        public static int BuyPrice(LootGun g)
        {
            return SellValue(g) * 4;
        }

        /// <summary>Number of loot guns a gun shop lists at a time.</summary>
        public const int ShopSlots = 4;

        /// <summary>One of the shop's guns: Common to Rare, seeded by the stock roll and the slot.</summary>
        public static LootGun ShopGun(int stockSeed, int index, int level)
        {
            GRand r = new GRand(stockSeed * 92821 + index * 7919 + 17);
            float x = r.Value();
            Rarity rarity = x < 0.5f ? Rarity.Common : (x < 0.88f ? Rarity.Uncommon : Rarity.Rare);
            return LootGenerator.Generate((int)r.NextUInt() & 0x7fffffff, level, rarity);
        }
    }

    /// <summary>
    /// The Forge's ARMORY maths: infusing an element, ascending a rarity step, raising the item level and scrapping.
    /// Every change returns a new gun so the screen can show the before and after side by side. Pure, tested offline.
    /// </summary>
    public static class LootForgeMath
    {
        public static ForgeCost InfuseCost(LootGun g, Element e)
        {
            ForgeCost c = new ForgeCost();
            if (g == null || e == Element.None) return c;
            c.Add(LootDropRules.CoreFor(e), g.Rarity >= Rarity.Legendary ? 2 : 1);
            c.Cash = 300 * (1 + (int)g.Rarity);
            return c;
        }

        public static bool CanInfuse(LootGun g, Element e)
        {
            return g != null && !RegionalArsenal04OE.IsWeapon(g) && e != Element.None && g.Element != e;
        }

        public static LootGun Infused(LootGun g, Element e)
        {
            LootGun n = g.Clone();
            n.Element = e;
            n.Version++;
            LootGenerator.Refresh(n);
            return n;
        }

        public static bool CanAscend(LootGun g)
        {
            return g != null && g.Rarity < Rarity.Mythic;
        }

        /// <summary>The boss heart a Mythic ascension asks for (matches the gun's element).</summary>
        public static string HeartFor(Element e)
        {
            switch (e)
            {
                case Element.Volt: return "foreman_core";
                case Element.Ember: return "quartz_heart";
                case Element.Frost: return "titan_heart";
                case Element.Void: return "queen_mirror";
                default: return "mirage_heart";
            }
        }

        public static ForgeCost AscendCost(LootGun g)
        {
            ForgeCost c = new ForgeCost();
            if (!CanAscend(g)) return c;
            switch (g.Rarity + 1)
            {
                case Rarity.Uncommon:
                    c.Add("shard_chunk", 3);
                    c.Cash = 500;
                    break;
                case Rarity.Rare:
                    c.Add(LootDropRules.CorePrism, 1).Add("mite_dust", 4);
                    c.Cash = 1500;
                    break;
                case Rarity.Epic:
                    c.Add(LootDropRules.CorePrism, 2).Add("golem_core", 1);
                    c.Cash = 4000;
                    break;
                case Rarity.Legendary:
                    c.Add(LootDropRules.CoreApex, 1).Add(LootDropRules.CorePrism, 2);
                    c.Cash = 10000;
                    break;
                default:
                    c.Add(LootDropRules.CoreApex, 2).Add(HeartFor(g.Element), 1);
                    c.Cash = 25000;
                    break;
            }
            return c;
        }

        /// <summary>The part an ascension rerolls (never the stock of a pistol or hand cannon, which has none).</summary>
        public static int AscendPart(LootGun g)
        {
            int p = (int)((uint)(g.Seed * 7 + g.Ascensions * 3 + 1) % LootGenerator.PartCount);
            if (p == (int)LootPart.Stock && (g.Archetype == LootArchetype.Pistol || g.Archetype == LootArchetype.HandCannon)) p = (int)LootPart.Sight;
            return p;
        }

        /// <summary>One rarity step up: one part rerolled to a different variant, every trait kept, a new one added when the rarity calls for more.</summary>
        public static LootGun Ascended(LootGun g)
        {
            LootGun n = g.Clone();
            if (!CanAscend(g)) return n;
            n.Rarity = g.Rarity + 1;
            int part = AscendPart(g);
            GRand r = new GRand(g.Seed * 31 + 977 * (g.Ascensions + 1));
            int v = r.Range(0, LootGenerator.Variants - 1);
            if (v >= g.Parts[part]) v++;
            n.Parts[part] = v;
            int want = LootGenerator.TraitCountFor(n.Rarity, 0f) + (n.Maker == LootMaker.Oddwire ? 1 : 0);
            if (n.Traits.Count < want) LootGenerator.AddTraits(n, want, r);
            n.Ascensions++;
            n.Version++;
            LootGenerator.Refresh(n);
            return n;
        }

        public static ForgeCost LevelCost(LootGun g, int toLevel)
        {
            ForgeCost c = new ForgeCost();
            if (g == null) return c;
            int delta = Mathf.Clamp(toLevel, 1, LootGenerator.MaxLevel) - g.Level;
            if (delta <= 0) return c;
            c.Cash = 60 * delta * (1 + (int)g.Rarity);
            c.Add("shard_chunk", (delta + 3) / 4);
            if (g.Rarity >= Rarity.Epic) c.Add(LootDropRules.CorePrism, (delta + 14) / 15);
            return c;
        }

        public static LootGun Leveled(LootGun g, int toLevel)
        {
            LootGun n = g.Clone();
            n.Level = Mathf.Clamp(Mathf.Max(g.Level, toLevel), 1, LootGenerator.MaxLevel);
            n.Version++;
            LootGenerator.Refresh(n);
            return n;
        }

        /// <summary>What scrapping a gun gives back (materials only, listed as a cost-shaped bundle with no cash).</summary>
        public static ForgeCost ScrapYield(LootGun g)
        {
            ForgeCost c = new ForgeCost();
            if (g == null) return c;
            string core = LootDropRules.CoreFor(g.Element);
            if (core == null) core = LootDropRules.ElementCores[(int)((uint)g.Seed % 4u)];
            switch (g.Rarity)
            {
                case Rarity.Common: c.Add("shard_chunk", 1); break;
                case Rarity.Uncommon: c.Add("shard_chunk", 2).Add("mite_dust", 1); break;
                case Rarity.Rare: c.Add("shard_chunk", 2).Add(core, 1); break;
                case Rarity.Epic: c.Add(LootDropRules.CorePrism, 1).Add(core, 1); break;
                case Rarity.Legendary: c.Add(LootDropRules.CorePrism, 2).Add(core, 2); break;
                default: c.Add(LootDropRules.CoreApex, 1).Add(LootDropRules.CorePrism, 2); break;
            }
            return c;
        }
    }

    /// <summary>Converts guns to and from their save records (JsonUtility-friendly: public fields and lists).</summary>
    public static class LootRecords
    {
        public static LootGunSave ToSave(LootGun g)
        {
            LootGunSave s = new LootGunSave
            {
                Uid = g.Uid, Seed = g.Seed, Archetype = (int)g.Archetype, Maker = (int)g.Maker, Rarity = (int)g.Rarity, Level = g.Level,
                Element = (int)g.Element, Favorite = g.Favorite, Unique = g.Unique, Ascensions = g.Ascensions
            };
            for (int i = 0; i < g.Parts.Length; i++) s.Parts.Add(g.Parts[i]);
            for (int i = 0; i < g.Traits.Count; i++) s.Traits.Add((int)g.Traits[i]);
            return s;
        }

        /// <summary>Rebuilds a gun from its record (out-of-range values are clamped by Refresh). Null for a null record.</summary>
        public static LootGun FromSave(LootGunSave s)
        {
            if (s == null) return null;
            LootGun g = new LootGun
            {
                Uid = s.Uid, Seed = s.Seed, Archetype = (LootArchetype)s.Archetype, Maker = (LootMaker)s.Maker, Rarity = (Rarity)s.Rarity,
                Level = s.Level, Element = (Element)s.Element, Favorite = s.Favorite, Unique = s.Unique, Ascensions = s.Ascensions
            };
            if (s.Parts != null) for (int i = 0; i < g.Parts.Length && i < s.Parts.Count; i++) g.Parts[i] = s.Parts[i];
            if (s.Traits != null) foreach (int t in s.Traits) g.Traits.Add((LootTrait)t);
            LootGenerator.Refresh(g);
            return g;
        }
    }
}

namespace NeonSyndicate
{
    /// <summary>The champion affixes: each changes how a champion fights.</summary>
    public enum ChampionAffix
    {
        /// <summary>Takes 30% less damage and wears heavy armour.</summary>
        Armored = 0,
        /// <summary>A shield (armour) that refills when it has not been hit for a few seconds.</summary>
        Shielded = 1,
        /// <summary>Runs and walks a third faster.</summary>
        Swift = 2,
        /// <summary>Explodes a moment after it dies: step back.</summary>
        Volatile = 3,
        /// <summary>Heals when it hurts you.</summary>
        Vampiric = 4,
        /// <summary>Regains health when left alone for a few seconds.</summary>
        Regenerating = 5
    }

    /// <summary>Champion rolls: one to three affixes and an invented name. Pure, tested offline.</summary>
    public static class ChampionRules
    {
        public const int AffixCount = 6;
        /// <summary>Champion health compared with the same enemy before promotion (a Shard-Touched elite is x3.2).</summary>
        public const float HealthMultiplier = 5.5f;

        private static readonly string[] First = { "Vex", "Marlo", "Sable", "Kade", "Juno", "Rook", "Tamsin", "Brix", "Ozzie", "Lark", "Dorian", "Cass", "Rhett", "Ivo", "Zara", "Moss" };
        private static readonly string[] Epithet = { "the Unbroken", "Ironjaw", "the Pale", "the Quiet", "Glasshand", "the Furnace", "Rustvein", "Stormcoat", "Grinwire", "the Tollkeeper", "Brassknuckle", "the Lantern" };

        /// <summary>A bit mask of one to three different affixes (Veteran rolls one more, still at most three).</summary>
        public static int RollAffixes(GRand r, bool veteran)
        {
            float x = r.Value();
            int count = x < 0.5f ? 1 : (x < 0.85f ? 2 : 3);
            if (veteran) count = Mathf.Min(3, count + 1);
            int mask = 0;
            int guard = 0;
            while (Count(mask) < count && guard++ < 32) mask |= 1 << r.Range(0, AffixCount);
            return mask;
        }

        public static int Count(int mask)
        {
            int n = 0;
            for (int i = 0; i < AffixCount; i++) if ((mask & (1 << i)) != 0) n++;
            return n;
        }

        public static string MakeName(GRand r)
        {
            return First[r.Range(0, First.Length)] + " " + Epithet[r.Range(0, Epithet.Length)];
        }

        public static string AffixName(ChampionAffix a)
        {
            return a.ToString();
        }

        /// <summary>The affixes as one line ("Armored, Swift").</summary>
        public static string AffixLine(int mask)
        {
            string s = "";
            for (int i = 0; i < AffixCount; i++)
            {
                if ((mask & (1 << i)) == 0) continue;
                s += (s.Length > 0 ? ", " : "") + AffixName((ChampionAffix)i);
            }
            return s;
        }
    }
}
