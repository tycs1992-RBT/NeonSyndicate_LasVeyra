using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>How one part variant or one maker changes a gun's numbers (multipliers start at 1, additions at 0).</summary>
    public struct LootMod
    {
        public string Name;
        public string Prefix;
        public float Damage, Interval, Hip, Aim, Recoil, Range, Clip, Reload, Handling, ElementPower;
        public float CritChance, CritMult, ElementChance;

        public static LootMod Make(string name, string prefix, float damage = 1f, float interval = 1f, float hip = 1f, float aim = 1f, float recoil = 1f,
            float range = 1f, float clip = 1f, float reload = 1f, float handling = 1f, float critChance = 0f, float critMult = 0f, float elementChance = 0f, float elementPower = 1f)
        {
            return new LootMod
            {
                Name = name, Prefix = prefix, Damage = damage, Interval = interval, Hip = hip, Aim = aim, Recoil = recoil, Range = range, Clip = clip,
                Reload = reload, Handling = handling, CritChance = critChance, CritMult = critMult, ElementChance = elementChance, ElementPower = elementPower
            };
        }
    }

    /// <summary>
    /// The loot gun generator. Pure and deterministic: the same seed and request always give the same gun, and a saved
    /// record (archetype, maker, rarity, level, parts, element, traits) always rebuilds the same numbers and name. Stats
    /// follow clear formulas: archetype base, then the maker and the seven parts (their combined effect is clamped so no
    /// mix runs away), then rarity and item level. Engine-free (Mathf and GRand only), tested offline.
    /// </summary>
    public static class LootGenerator
    {
        public const int PartCount = 7;
        public const int Variants = 4;
        public const int MaxLevel = 50;
        public const int MaxTraits = 4;
        public const int ArchetypeCount = 8;
        public const int MakerCount = 6;

        // ------------------------------------------------------------------ tables (index = LootArchetype)

        private static readonly float[] BaseDamage = { 24f, 62f, 15f, 27f, 12f, 130f, 88f, 18f };
        private static readonly float[] BaseInterval = { 0.18f, 0.5f, 0.075f, 0.11f, 0.8f, 1f, 0.7f, 0.1f };
        private static readonly int[] BasePellets = { 1, 1, 1, 1, 8, 1, 1, 1 };
        private static readonly float[] BaseHip = { 1.5f, 1.9f, 3.2f, 2.4f, 6.5f, 7f, 1.5f, 2f };
        private static readonly float[] BaseAim = { 0.4f, 0.3f, 1.4f, 0.6f, 4.5f, 0f, 0.4f, 0.7f };
        private static readonly float[] BaseRecoil = { 3f, 8f, 2.1f, 3.2f, 10f, 13f, 9f, 1.6f };
        private static readonly float[] BaseRange = { 120f, 140f, 90f, 220f, 45f, 700f, 120f, 160f };
        private static readonly int[] BaseClip = { 14, 6, 30, 30, 6, 5, 4, 0 };
        private static readonly float[] BaseReload = { 1.2f, 1.9f, 1.5f, 1.8f, 1.9f, 2.4f, 2.6f, 1.7f };
        private static readonly float[] BaseCritChance = { 0.06f, 0.08f, 0.04f, 0.05f, 0.03f, 0.1f, 0f, 0.06f };
        private static readonly float[] BaseCritMult = { 1.6f, 1.8f, 1.5f, 1.6f, 1.5f, 2f, 1f, 1.6f };
        private static readonly float[] ArchetypeWeight = { 16f, 10f, 15f, 18f, 13f, 10f, 8f, 10f };
        /// <summary>Heat a crystal caster adds per shot before parts (about 15 shots to overheat).</summary>
        public const float BaseHeat = 0.065f;

        /// <summary>Damage factor per rarity: the shop guns sit at Uncommon (1.0).</summary>
        public static readonly float[] RarityPower = { 0.85f, 1f, 1.1f, 1.2f, 1.32f, 1.45f };

        public static readonly string[] MakerShort = { "Saltglass", "Brassjaw", "Veyra", "Cinderline", "Dunehold", "Oddwire" };
        public static readonly string[] MakerFull = { "Saltglass Optics", "Brassjaw Foundry", "Veyra Crystalworks", "Cinderline Rapid Arms", "Dunehold Munitions", "Oddwire Labs" };
        public static readonly string[] MakerStyle =
        {
            "Precise: tight spread and big critical hits, a little slower. Long, pale ceramic guns with fine scopes.",
            "Heavy hitting: huge damage per shot, slow and kicking. Chunky brass blocks and shrouded barrels.",
            "Elemental: every gun carries an element, and it hits harder. Faceted crystal fins that glow.",
            "Fast firing: very high fire rate, lighter rounds. Compact, angular, red and black with vents.",
            "High capacity: huge magazines, slower reloads. Boxy sand-coloured guns with drums.",
            "Gimmick: one extra trait on every gun. Mismatched parts and bare glowing coils."
        };

        private static readonly LootMod[] MakerMods =
        {
            LootMod.Make("Saltglass", null, interval: 1.06f, hip: 0.75f, aim: 0.7f, range: 1.1f, handling: 1.05f, critChance: 0.03f, critMult: 0.35f),
            LootMod.Make("Brassjaw", null, damage: 1.18f, interval: 1.14f, recoil: 1.25f, clip: 0.85f, handling: 0.92f),
            LootMod.Make("Veyra", null, damage: 0.96f, elementChance: 0.15f, elementPower: 1.3f),
            LootMod.Make("Cinderline", null, damage: 0.9f, interval: 0.84f, hip: 1.15f, recoil: 0.9f, reload: 0.9f),
            LootMod.Make("Dunehold", null, damage: 0.97f, recoil: 0.95f, clip: 1.55f, reload: 1.12f),
            LootMod.Make("Oddwire", null, damage: 0.97f, interval: 0.98f)
        };

        /// <summary>The part variants: [part][variant]. Variant 0 is always the plain part with no effect.</summary>
        private static readonly LootMod[][] PartMods =
        {
            new[]
            {
                LootMod.Make("Standard frame", null),
                LootMod.Make("Lightweight frame", "Featherweight", handling: 1.15f, recoil: 1.1f),
                LootMod.Make("Reinforced frame", "Bulwark", damage: 1.06f, handling: 0.9f),
                LootMod.Make("Tuned frame", "Tuned", interval: 0.94f, hip: 1.08f)
            },
            new[]
            {
                LootMod.Make("Standard barrel", null),
                LootMod.Make("Long barrel", "Long", aim: 0.85f, range: 1.25f, handling: 0.95f),
                LootMod.Make("Ported barrel", "Ported", recoil: 0.8f),
                LootMod.Make("Heavy barrel", "Heavy", damage: 1.08f, interval: 1.05f)
            },
            new[]
            {
                LootMod.Make("Standard grip", null),
                LootMod.Make("Steady grip", "Steady", aim: 0.92f, recoil: 0.88f),
                LootMod.Make("Quick grip", "Nimble", reload: 0.9f, handling: 1.1f),
                LootMod.Make("Hair trigger", "Hairtrigger", interval: 0.93f, recoil: 1.08f)
            },
            new[]
            {
                LootMod.Make("No stock", null),
                LootMod.Make("Fixed stock", "Braced", recoil: 0.85f),
                LootMod.Make("Skeleton stock", "Skeletal", recoil: 0.95f, handling: 1.08f),
                LootMod.Make("Padded stock", "Cushioned", recoil: 0.75f, handling: 0.92f)
            },
            new[]
            {
                LootMod.Make("Iron sights", null),
                LootMod.Make("Red dot", "Keen", aim: 0.9f),
                LootMod.Make("Holo sight", "Holo", aim: 0.85f, critChance: 0.02f),
                LootMod.Make("Long scope", "Farseeing", aim: 0.75f, range: 1.15f, handling: 0.95f, critChance: 0.03f, critMult: 0.1f)
            },
            new[]
            {
                LootMod.Make("Standard magazine", null),
                LootMod.Make("Extended magazine", "Extended", clip: 1.35f, reload: 1.1f),
                LootMod.Make("Quick magazine", "Swift", clip: 0.9f, reload: 0.8f),
                LootMod.Make("Drum magazine", "Bottomless", clip: 1.7f, reload: 1.25f, handling: 0.92f)
            },
            new[]
            {
                LootMod.Make("No accessory", null),
                LootMod.Make("Laser sight", "Lasered", hip: 0.8f),
                LootMod.Make("Muzzle brake", "Braked", recoil: 0.85f),
                LootMod.Make("Element coil", "Coiled", critMult: 0.05f, elementChance: 0.08f)
            }
        };

        private static readonly string[][] Models =
        {
            new[] { "Dart", "Snapcap", "Pocketbolt", "Quickhand" },
            new[] { "Mauler", "Anvil", "Gavel", "Boomhand" },
            new[] { "Buzzer", "Chatter", "Swarm", "Whirr" },
            new[] { "Patrol", "Tempest", "Rampart", "Steadfast" },
            new[] { "Breacher", "Doorkicker", "Cloudburst", "Bramble" },
            new[] { "Longsight", "Farcall", "Stillwater", "Nightglass" },
            new[] { "Boomtube", "Thunderclap", "Skyhammer", "Roofbreaker" },
            new[] { "Prism", "Shardcaster", "Glowbore", "Refractor" }
        };

        private static readonly string[][] ElementPrefixes =
        {
            new string[0],
            new[] { "Arcing", "Crackling" },
            new[] { "Searing", "Blazing" },
            new[] { "Rimed", "Glacial" },
            new[] { "Hollow", "Umbral" }
        };

        public static readonly string[] PartNames = { "Body", "Barrel", "Grip", "Stock", "Sight", "Magazine", "Accessory" };

        // ------------------------------------------------------------------ names and words

        public static string ArchetypeName(LootArchetype a, bool rocket)
        {
            switch (a)
            {
                case LootArchetype.Pistol: return "Pistol";
                case LootArchetype.HandCannon: return "Hand Cannon";
                case LootArchetype.Smg: return "SMG";
                case LootArchetype.AssaultRifle: return "Assault Rifle";
                case LootArchetype.Shotgun: return "Shotgun";
                case LootArchetype.Sniper: return "Sniper Rifle";
                case LootArchetype.Launcher: return rocket ? "Rocket Launcher" : "Grenade Launcher";
                default: return "Crystal Caster";
            }
        }

        public static string PartName(LootPart part, int variant, LootArchetype a)
        {
            if (a == LootArchetype.CrystalCaster && part == LootPart.Magazine)
            {
                string[] cells = { "Standard cell", "Deep cell", "Quick vent cell", "Reservoir cell" };
                return cells[Mathf.Clamp(variant, 0, 3)];
            }
            return PartMods[(int)part][Mathf.Clamp(variant, 0, Variants - 1)].Name;
        }

        public static string TraitName(LootTrait t)
        {
            switch (t)
            {
                case LootTrait.Ricochet: return "Ricochet";
                case LootTrait.Piercing: return "Piercing";
                case LootTrait.ExplosiveRounds: return "Explosive Rounds";
                case LootTrait.ChainLightning: return "Chain Lightning";
                case LootTrait.HomingRockets: return "Micro Rockets";
                case LootTrait.SplitShot: return "Split Shot";
                case LootTrait.BurstFire: return "Burst Fire";
                case LootTrait.ChargeShot: return "Charge Shot";
                case LootTrait.DoubleBarrel: return "Double Barrel";
                case LootTrait.RefillOnKill: return "Refill on Kill";
                case LootTrait.LifeSteal: return "Life Steal";
                case LootTrait.CritCascade: return "Critical Cascade";
                case LootTrait.EmptyNova: return "Empty Nova";
                case LootTrait.Penetrator: return "Penetrator";
                default: return "Spin Up";
            }
        }

        public static string TraitText(LootTrait t, LootArchetype a)
        {
            switch (t)
            {
                case LootTrait.Ricochet: return "Rounds that hit a wall bounce toward the nearest enemy.";
                case LootTrait.Piercing: return "Rounds pass through up to two enemies.";
                case LootTrait.ExplosiveRounds: return "Every hit bursts and splashes enemies standing nearby.";
                case LootTrait.ChainLightning: return a == LootArchetype.Launcher ? "Each blast arcs lightning to two enemies outside it." : "Hits can arc lightning to two more enemies nearby.";
                case LootTrait.HomingRockets: return "Every third shot also fires a small homing rocket.";
                case LootTrait.SplitShot: return "Each shot splits into three.";
                case LootTrait.BurstFire: return "Each pull of the trigger fires a three-round burst.";
                case LootTrait.ChargeShot: return "Hold the trigger to charge, release to fire up to 80% harder.";
                case LootTrait.DoubleBarrel: return "Fires two rounds at once.";
                case LootTrait.RefillOnKill: return "Kills put a quarter of the magazine back.";
                case LootTrait.LifeSteal: return "Heals you for part of the damage you deal.";
                case LootTrait.CritCascade: return "Critical hits raise your critical chance for a few seconds.";
                case LootTrait.EmptyNova: return a == LootArchetype.CrystalCaster ? "Overheating releases a nova around you." : "Emptying the magazine releases a nova around you.";
                case LootTrait.Penetrator: return "Rounds punch through thin cover.";
                default: return "Fire rate keeps climbing while you hold the trigger.";
            }
        }

        // ------------------------------------------------------------------ trait rules

        /// <summary>Whether a trait can appear on an archetype (launchers only get blast-friendly traits, and so on).</summary>
        public static bool TraitValid(LootArchetype a, LootTrait t)
        {
            bool hitscan = a != LootArchetype.Launcher;
            switch (t)
            {
                case LootTrait.Ricochet:
                case LootTrait.Piercing:
                case LootTrait.ExplosiveRounds:
                case LootTrait.CritCascade:
                case LootTrait.Penetrator:
                    return hitscan;
                case LootTrait.ChainLightning:
                case LootTrait.LifeSteal:
                case LootTrait.EmptyNova:
                    return true;
                case LootTrait.HomingRockets:
                    return hitscan && a != LootArchetype.Sniper;
                case LootTrait.SplitShot:
                    return a == LootArchetype.Pistol || a == LootArchetype.Smg || a == LootArchetype.AssaultRifle || a == LootArchetype.CrystalCaster;
                case LootTrait.BurstFire:
                    return a == LootArchetype.Pistol || a == LootArchetype.Smg || a == LootArchetype.AssaultRifle;
                case LootTrait.ChargeShot:
                    return a == LootArchetype.Pistol || a == LootArchetype.HandCannon || a == LootArchetype.CrystalCaster;
                case LootTrait.DoubleBarrel:
                    return a == LootArchetype.HandCannon || a == LootArchetype.Shotgun;
                case LootTrait.RefillOnKill:
                    return a != LootArchetype.CrystalCaster;
                case LootTrait.SpinUp:
                    return a == LootArchetype.Smg || a == LootArchetype.AssaultRifle || a == LootArchetype.CrystalCaster;
                default:
                    return false;
            }
        }

        /// <summary>Pairs that change how the trigger works cannot share a gun.</summary>
        public static bool TraitsCompatible(LootTrait a, LootTrait b)
        {
            if (a == b) return false;
            return !(Pair(a, b, LootTrait.ChargeShot, LootTrait.DoubleBarrel) || Pair(a, b, LootTrait.ChargeShot, LootTrait.BurstFire) ||
                     Pair(a, b, LootTrait.ChargeShot, LootTrait.SpinUp) || Pair(a, b, LootTrait.BurstFire, LootTrait.SpinUp));
        }

        private static bool Pair(LootTrait a, LootTrait b, LootTrait x, LootTrait y)
        {
            return (a == x && b == y) || (a == y && b == x);
        }

        /// <summary>True when every trait is valid for the archetype, none repeats, no pair clashes and there are not too many.</summary>
        public static bool TraitSetValid(LootGun g)
        {
            if (g.Traits.Count > MaxTraits) return false;
            for (int i = 0; i < g.Traits.Count; i++)
            {
                if (!TraitValid(g.Archetype, g.Traits[i])) return false;
                for (int j = i + 1; j < g.Traits.Count; j++) if (!TraitsCompatible(g.Traits[i], g.Traits[j])) return false;
            }
            return true;
        }

        /// <summary>How many traits a freshly rolled gun of this rarity gets (before the gimmick maker's extra one).</summary>
        public static int TraitCountFor(Rarity r, float roll)
        {
            switch (r)
            {
                case Rarity.Common: return 0;
                case Rarity.Uncommon: return roll < 0.3f ? 1 : 0;
                case Rarity.Rare: return 1;
                case Rarity.Epic: return 2;
                case Rarity.Legendary: return 2;
                default: return 3;
            }
        }

        /// <summary>Adds random valid traits (no clashes) until the gun has count of them or nothing fits.</summary>
        public static void AddTraits(LootGun g, int count, GRand r)
        {
            List<LootTrait> pool = new List<LootTrait>();
            for (int guard = 0; guard < 8 && g.Traits.Count < Mathf.Min(count, MaxTraits); guard++)
            {
                pool.Clear();
                for (int t = 0; t <= (int)LootTrait.SpinUp; t++)
                {
                    LootTrait tr = (LootTrait)t;
                    if (!TraitValid(g.Archetype, tr)) continue;
                    bool ok = true;
                    for (int k = 0; k < g.Traits.Count; k++) if (!TraitsCompatible(tr, g.Traits[k])) ok = false;
                    if (ok) pool.Add(tr);
                }
                if (pool.Count == 0) break;
                g.Traits.Add(pool[r.Range(0, pool.Count)]);
            }
        }

        // ------------------------------------------------------------------ generation

        /// <summary>Element chance for a generated gun of this rarity (the elemental maker and crystal casters always roll one).</summary>
        public static float ElementChanceFor(Rarity r)
        {
            switch (r)
            {
                case Rarity.Common: return 0.05f;
                case Rarity.Uncommon: return 0.15f;
                case Rarity.Rare: return 0.3f;
                case Rarity.Epic: return 0.5f;
                case Rarity.Legendary: return 0.7f;
                default: return 1f;
            }
        }

        /// <summary>
        /// Rolls a gun. archetype, maker and unique are -1 for "any". Legendary and Mythic guns are always named uniques
        /// (a unique keeps its own rarity, archetype, maker, element and signature traits; the seed still picks its parts).
        /// </summary>
        public static LootGun Generate(int seed, int level, Rarity rarity, int archetype = -1, int maker = -1, int unique = -1)
        {
            GRand r = new GRand(seed);
            LootGun g = new LootGun { Seed = seed, Level = Mathf.Clamp(level, 1, MaxLevel), Rarity = rarity };
            if (unique < 0 && rarity >= Rarity.Legendary)
            {
                List<int> pool = LootUniques.OfRarity(rarity, archetype);
                if (pool.Count == 0) pool = LootUniques.OfRarity(rarity, -1);
                if (pool.Count > 0) unique = pool[r.Range(0, pool.Count)];
            }
            LootUniqueDef u = LootUniques.Get(unique);
            if (u != null)
            {
                g.Unique = unique;
                g.Rarity = u.Rarity;
                g.Archetype = u.Archetype;
                g.Maker = u.Maker;
            }
            else
            {
                g.Archetype = archetype >= 0 && archetype < ArchetypeCount ? (LootArchetype)archetype : PickArchetype(r);
                g.Maker = maker >= 0 && maker < MakerCount ? (LootMaker)maker : (LootMaker)r.Range(0, MakerCount);
            }
            for (int i = 0; i < PartCount; i++) g.Parts[i] = r.Range(0, Variants);
            float elementRoll = r.Value();
            float traitRoll = r.Value();
            if (u != null)
            {
                g.Element = u.Element;
                g.Traits.AddRange(u.Traits);
            }
            else
            {
                bool elemental = g.Maker == LootMaker.Veyra || g.Archetype == LootArchetype.CrystalCaster || elementRoll < ElementChanceFor(g.Rarity);
                g.Element = elemental ? (Element)(1 + r.Range(0, 4)) : Element.None;
                int count = TraitCountFor(g.Rarity, traitRoll) + (g.Maker == LootMaker.Oddwire ? 1 : 0);
                AddTraits(g, count, r);
            }
            Refresh(g);
            return g;
        }

        private static LootArchetype PickArchetype(GRand r)
        {
            float total = 0f;
            for (int i = 0; i < ArchetypeWeight.Length; i++) total += ArchetypeWeight[i];
            float x = r.Value() * total;
            for (int i = 0; i < ArchetypeWeight.Length; i++)
            {
                x -= ArchetypeWeight[i];
                if (x < 0f) return (LootArchetype)i;
            }
            return LootArchetype.AssaultRifle;
        }

        /// <summary>Clamps every saved field into range (old or hand-edited saves) and recomputes name, flavor and stats.</summary>
        public static void Refresh(LootGun g)
        {
            if (g == null) return;
            g.Archetype = (LootArchetype)Mathf.Clamp((int)g.Archetype, 0, ArchetypeCount - 1);
            g.Maker = (LootMaker)Mathf.Clamp((int)g.Maker, 0, MakerCount - 1);
            g.Rarity = (Rarity)Mathf.Clamp((int)g.Rarity, 0, (int)Rarity.Mythic);
            g.Level = Mathf.Clamp(g.Level, 1, MaxLevel);
            g.Element = (Element)Mathf.Clamp((int)g.Element, 0, 4);
            for (int i = 0; i < PartCount; i++) g.Parts[i] = Mathf.Clamp(g.Parts[i], 0, Variants - 1);
            // Pistols and hand cannons have no stock.
            if (g.Archetype == LootArchetype.Pistol || g.Archetype == LootArchetype.HandCannon) g.Parts[(int)LootPart.Stock] = 0;
            if (LootUniques.Get(g.Unique) == null) g.Unique = -1;
            for (int i = g.Traits.Count - 1; i >= 0; i--)
            {
                bool bad = (int)g.Traits[i] < 0 || (int)g.Traits[i] > (int)LootTrait.SpinUp || !TraitValid(g.Archetype, g.Traits[i]);
                for (int j = 0; j < i && !bad; j++) if (!TraitsCompatible(g.Traits[i], g.Traits[j])) bad = true;
                if (bad) g.Traits.RemoveAt(i);
            }
            while (g.Traits.Count > MaxTraits) g.Traits.RemoveAt(g.Traits.Count - 1);
            g.Stats = ComputeStats(g);
            g.Name = MakeName(g);
            g.Flavor = MakeFlavor(g);
        }

        public static bool IsRocket(LootGun g)
        {
            return g.Archetype == LootArchetype.Launcher && g.Parts[(int)LootPart.Body] >= 2;
        }

        public static float LevelPower(int level)
        {
            return 1f + 0.008f * (Mathf.Clamp(level, 1, MaxLevel) - 1);
        }

        /// <summary>Item level for a drop: the player's level plus a bonus for tougher enemies.</summary>
        public static int ItemLevelFor(int playerLevel, int bonus)
        {
            return Mathf.Clamp(playerLevel + bonus, 1, MaxLevel);
        }

        // ------------------------------------------------------------------ stats

        private struct Acc
        {
            public float Damage, Interval, Hip, Aim, Recoil, Range, Clip, Reload, Handling, ElementPower, CritChance, CritMult, ElementChance;

            public void Apply(LootMod m)
            {
                Damage *= m.Damage;
                Interval *= m.Interval;
                Hip *= m.Hip;
                Aim *= m.Aim;
                Recoil *= m.Recoil;
                Range *= m.Range;
                Clip *= m.Clip;
                Reload *= m.Reload;
                Handling *= m.Handling;
                ElementPower *= m.ElementPower;
                CritChance += m.CritChance;
                CritMult += m.CritMult;
                ElementChance += m.ElementChance;
            }
        }

        /// <summary>The gun's final numbers from its record.</summary>
        public static LootStats ComputeStats(LootGun g)
        {
            int a = (int)g.Archetype;
            bool rocket = IsRocket(g);
            bool caster = g.Archetype == LootArchetype.CrystalCaster;
            float damage = rocket ? 92f : BaseDamage[a];
            float interval = rocket ? 0.85f : BaseInterval[a];
            float hip = rocket ? 1.2f : BaseHip[a];
            float aim = rocket ? 0.2f : BaseAim[a];
            float recoil = rocket ? 12f : BaseRecoil[a];
            float range = rocket ? 400f : BaseRange[a];
            int clip = rocket ? 2 : BaseClip[a];

            Acc m = new Acc { Damage = 1f, Interval = 1f, Hip = 1f, Aim = 1f, Recoil = 1f, Range = 1f, Clip = 1f, Reload = 1f, Handling = 1f, ElementPower = 1f };
            m.Apply(MakerMods[(int)g.Maker]);
            for (int p = 0; p < PartCount; p++) m.Apply(PartMods[p][g.Parts[p]]);
            if (g.Maker == LootMaker.Oddwire)
            {
                // The gimmick maker's guns are never quite the same twice: a small seeded wobble.
                GRand w = new GRand(g.Seed ^ 0x51ED);
                m.Hip *= w.Range(0.9f, 1.1f);
                m.Recoil *= w.Range(0.9f, 1.15f);
                m.Range *= w.Range(0.9f, 1.1f);
            }
            // No mix of maker and parts may run away: the per-shot factor and the damage-per-second factor are clamped.
            m.Damage = Mathf.Clamp(m.Damage, 0.8f, 1.25f);
            float dpsFactor = m.Damage / m.Interval;
            float clamped = Mathf.Clamp(dpsFactor, 0.85f, 1.2f);
            if (!Mathf.Approximately(clamped, dpsFactor)) m.Interval = m.Damage / clamped;

            int r = (int)g.Rarity;
            float power = RarityPower[r] * LevelPower(g.Level);
            LootStats s = new LootStats();
            s.Damage = damage * m.Damage * power;
            s.Interval = Mathf.Max(0.04f, interval * m.Interval);
            s.Pellets = BasePellets[a];
            s.SpreadHip = hip * m.Hip * (1f - 0.03f * r);
            s.SpreadAim = Mathf.Min(s.SpreadHip, aim * m.Aim * (1f - 0.03f * r));
            s.Recoil = recoil * m.Recoil;
            s.Range = range * m.Range;
            s.Handling = Mathf.Clamp(m.Handling, 0.6f, 1.5f);
            s.Reload = (caster ? 1.7f : BaseReload[a]) * m.Reload / Mathf.Sqrt(s.Handling);
            s.Clip = caster ? 0 : Mathf.Max(1, Mathf.RoundToInt(clip * m.Clip * (1f + 0.04f * r)));
            s.HeatPerShot = caster ? BaseHeat / Mathf.Max(0.5f, m.Clip) : 0f;
            bool launcher = g.Archetype == LootArchetype.Launcher;
            s.CritChance = launcher ? 0f : Mathf.Clamp(BaseCritChance[a] + m.CritChance + 0.01f * r, 0f, 0.5f);
            s.CritMultiplier = launcher ? 1f : Mathf.Clamp(BaseCritMult[a] + m.CritMult + 0.05f * r, 1.2f, 3f);
            if (g.Element == Element.None)
            {
                s.ElementChance = 0f;
                s.ElementPower = 0f;
            }
            else
            {
                s.ElementChance = Mathf.Clamp(0.14f + 0.03f * r + m.ElementChance + (caster ? 0.12f : 0f), 0.05f, 0.6f);
                s.ElementPower = (0.8f + 0.12f * r) * m.ElementPower;
            }
            if (launcher)
            {
                s.Rocket = rocket;
                s.ProjectileSpeed = (rocket ? 50f : 38f) * Mathf.Sqrt(m.Range);
                s.BlastRadius = (rocket ? 6.5f : 5.5f) * (1f + 0.03f * r);
            }
            int sight = g.Parts[(int)LootPart.Sight];
            if (g.Archetype == LootArchetype.Sniper) s.ScopeFov = sight == 3 ? 9f : 14f;
            else if ((g.Archetype == LootArchetype.AssaultRifle || caster) && sight == 3) s.ScopeFov = 30f;
            return s;
        }

        // ------------------------------------------------------------------ damage maths

        /// <summary>
        /// Body-shot damage of one pull of the trigger with every pellet landing, no criticals or headshots: what the
        /// "never one-shots an elite" rule is measured on. Launchers count the direct hit plus the blast at its centre.
        /// </summary>
        public static float PullDamage(LootGun g)
        {
            LootStats s = g.Stats;
            if (g.Archetype == LootArchetype.Launcher) return s.Damage * 1.5f;
            float d = s.Damage * Mathf.Max(1, s.Pellets);
            if (g.HasTrait(LootTrait.DoubleBarrel)) d *= 1.3f;
            if (g.HasTrait(LootTrait.BurstFire)) d *= 2.7f;
            if (g.HasTrait(LootTrait.SplitShot)) d *= 1.5f;
            if (g.HasTrait(LootTrait.ChargeShot)) d *= 1.8f;
            return d;
        }

        /// <summary>Seconds from one pull of the trigger to the next (charge-shot guns: tapping, without charging).</summary>
        public static float CycleTime(LootGun g)
        {
            LootStats s = g.Stats;
            if (g.HasTrait(LootTrait.BurstFire)) return s.Interval * 3f;
            if (g.HasTrait(LootTrait.SpinUp)) return s.Interval * 0.82f;
            return s.Interval;
        }

        /// <summary>
        /// Damage per pull and seconds per pull in the gun's best way of firing: a charge-shot gun either taps (normal
        /// damage, normal rate) or fully charges every shot (80% more damage, a slower cycle), whichever deals more.
        /// </summary>
        private static void BestCycle(LootGun g, out float pull, out float cycle)
        {
            pull = PullDamage(g);
            cycle = Mathf.Max(0.04f, CycleTime(g));
            if (!g.HasTrait(LootTrait.ChargeShot)) return;
            float tapPull = pull / 1.8f;
            float chargedCycle = cycle + ChargeSeconds;
            if (tapPull / cycle >= pull / chargedCycle) pull = tapPull;
            else cycle = chargedCycle;
        }

        /// <summary>Seconds a charge shot takes to charge fully.</summary>
        public const float ChargeSeconds = 0.8f;

        /// <summary>Damage per second while the trigger is held (criticals averaged in when asked).</summary>
        public static float BurstDps(LootGun g, bool withCrits)
        {
            float pull, cycle;
            BestCycle(g, out pull, out cycle);
            float dps = pull / cycle;
            if (withCrits) dps *= 1f + g.Stats.CritChance * (g.Stats.CritMultiplier - 1f);
            return dps;
        }

        /// <summary>Damage per second over a whole magazine and its reload (or a full heat cycle and the vent).</summary>
        public static float SustainedDps(LootGun g)
        {
            LootStats s = g.Stats;
            float pull, cycle;
            BestCycle(g, out pull, out cycle);
            float shots;
            if (s.HeatPerShot > 0f) shots = Mathf.Ceil(1f / s.HeatPerShot);
            else
            {
                int perPull = g.HasTrait(LootTrait.BurstFire) ? 3 : (g.HasTrait(LootTrait.DoubleBarrel) ? 2 : 1);
                shots = Mathf.Max(1f, Mathf.Floor((float)s.Clip / perPull));
            }
            return shots * pull / (shots * cycle + s.Reload);
        }

        // ------------------------------------------------------------------ text

        public static string MakeName(LootGun g)
        {
            LootUniqueDef u = LootUniques.Get(g.Unique);
            if (u != null) return u.Name;
            // Separate generators for the model word and the prefix, so an infusion (new element) keeps the model word.
            GRand rm = new GRand(g.Seed ^ 0x2F6B);
            GRand rp = new GRand(g.Seed ^ 0x6A09);
            string model;
            if (g.Archetype == LootArchetype.Launcher) model = Models[6][(IsRocket(g) ? 2 : 0) + rm.Range(0, 2)];
            else model = Models[(int)g.Archetype][rm.Range(0, 4)];
            string prefix = null;
            if (g.Element != Element.None)
            {
                string[] e = ElementPrefixes[(int)g.Element];
                prefix = e[rp.Range(0, e.Length)];
            }
            else
            {
                List<string> words = new List<string>();
                for (int p = 0; p < PartCount; p++)
                {
                    string w = PartMods[p][g.Parts[p]].Prefix;
                    if (!string.IsNullOrEmpty(w)) words.Add(w);
                }
                if (words.Count > 0) prefix = words[rp.Range(0, words.Count)];
            }
            string maker = MakerShort[(int)g.Maker];
            return string.IsNullOrEmpty(prefix) ? maker + " " + model : prefix + " " + maker + " " + model;
        }

        public static string MakeFlavor(LootGun g)
        {
            LootUniqueDef u = LootUniques.Get(g.Unique);
            if (u != null) return u.Flavor;
            string s = GearSystem.RarityName(g.Rarity) + " " + ArchetypeName(g.Archetype, IsRocket(g)).ToLowerInvariant() + " from " + MakerFull[(int)g.Maker] + ".";
            if (g.Ascensions > 0) s += " Raised at the Forge.";
            return s;
        }

        /// <summary>The traits as one line ("Ricochet, Life Steal"), or an empty string.</summary>
        public static string TraitLine(LootGun g)
        {
            if (g.Traits.Count == 0) return "";
            string s = "";
            for (int i = 0; i < g.Traits.Count; i++) s += (i > 0 ? ", " : "") + TraitName(g.Traits[i]);
            return s;
        }

        // ------------------------------------------------------------------ the weapon definition

        public static WeaponSlot SlotFor(LootArchetype a)
        {
            switch (a)
            {
                case LootArchetype.Pistol:
                case LootArchetype.HandCannon: return WeaponSlot.Handgun;
                case LootArchetype.Smg: return WeaponSlot.SMG;
                case LootArchetype.Shotgun: return WeaponSlot.Shotgun;
                case LootArchetype.Sniper: return WeaponSlot.Sniper;
                case LootArchetype.Launcher: return WeaponSlot.Heavy;
                default: return WeaponSlot.Rifle;
            }
        }

        public static AmmoType AmmoFor(LootGun g)
        {
            switch (g.Archetype)
            {
                case LootArchetype.Pistol:
                case LootArchetype.HandCannon: return AmmoType.Pistol;
                case LootArchetype.Smg: return AmmoType.SMG;
                case LootArchetype.AssaultRifle: return AmmoType.Rifle;
                case LootArchetype.Shotgun: return AmmoType.Shell;
                case LootArchetype.Sniper: return AmmoType.Sniper;
                case LootArchetype.Launcher: return IsRocket(g) ? AmmoType.Rocket : AmmoType.Grenade40;
                default: return AmmoType.None;
            }
        }

        /// <summary>The largest reserve of an ammo type (matches the fixed arsenal's caps).</summary>
        public static int MaxAmmoFor(AmmoType t)
        {
            switch (t)
            {
                case AmmoType.Pistol: return 360;
                case AmmoType.SMG: return 600;
                case AmmoType.Shell: return 160;
                case AmmoType.Rifle: return 600;
                case AmmoType.Sniper: return 80;
                case AmmoType.Grenade40: return 36;
                case AmmoType.Rocket: return 20;
                default: return 0;
            }
        }

        /// <summary>
        /// Builds the live weapon definition a loot slot hands to the inventory, the weapon controller and the HUD. Burst
        /// and charge guns fire one press at a time; everything else keeps its archetype's fire mode.
        /// </summary>
        public static WeaponDef ToWeaponDef(LootGun g, WeaponId slotId)
        {
            LootStats s = g.Stats;
            bool rocket = IsRocket(g);
            FireKind fire;
            switch (g.Archetype)
            {
                case LootArchetype.Smg:
                case LootArchetype.AssaultRifle:
                case LootArchetype.CrystalCaster: fire = FireKind.Auto; break;
                case LootArchetype.Launcher: fire = FireKind.Projectile; break;
                default: fire = FireKind.Semi; break;
            }
            if (fire == FireKind.Auto && (g.HasTrait(LootTrait.BurstFire) || g.HasTrait(LootTrait.ChargeShot))) fire = FireKind.Semi;
            HoldType hold = g.Archetype == LootArchetype.Pistol || g.Archetype == LootArchetype.HandCannon || g.Archetype == LootArchetype.Smg
                ? HoldType.Pistol : (rocket ? HoldType.Heavy : HoldType.Rifle);
            SfxId sound;
            switch (g.Archetype)
            {
                case LootArchetype.Pistol: sound = SfxId.Pistol; break;
                case LootArchetype.HandCannon: sound = SfxId.Revolver; break;
                case LootArchetype.Smg: sound = SfxId.SMG; break;
                case LootArchetype.AssaultRifle: sound = SfxId.Rifle; break;
                case LootArchetype.Shotgun: sound = SfxId.Shotgun; break;
                case LootArchetype.Sniper: sound = SfxId.Sniper; break;
                case LootArchetype.Launcher: sound = rocket ? SfxId.RocketLaunch : SfxId.GrenadeLauncher; break;
                default: sound = SfxId.Silenced; break;
            }
            float[] muzzle = { 0.7f, 1f, 0.8f, 1.1f, 1.5f, 1.2f, 1.4f, 0.9f };
            AmmoType ammo = AmmoFor(g);
            int mask = 0;
            for (int i = 0; i < g.Traits.Count; i++) mask |= 1 << (int)g.Traits[i];
            string traits = TraitLine(g);
            return new WeaponDef
            {
                Id = slotId,
                Name = g.Name,
                Blurb = g.Flavor + (traits.Length > 0 ? " " + traits + "." : ""),
                Slot = SlotFor(g.Archetype),
                Ammo = ammo,
                Fire = fire,
                Hold = hold,
                Damage = s.Damage,
                Interval = s.Interval,
                Pellets = s.Pellets,
                SpreadHip = s.SpreadHip,
                SpreadAim = s.SpreadAim,
                Recoil = s.Recoil,
                Range = s.Range,
                ClipSize = s.Clip,
                MaxAmmo = MaxAmmoFor(ammo),
                ReloadTime = s.Reload,
                ProjectileSpeed = s.ProjectileSpeed,
                BlastRadius = s.BlastRadius,
                Sound = sound,
                MuzzleSize = muzzle[(int)g.Archetype] * (rocket ? 1.4f : 1f),
                DriveBy = g.Archetype == LootArchetype.Pistol || g.Archetype == LootArchetype.HandCannon || g.Archetype == LootArchetype.Smg,
                ScopeFov = s.ScopeFov,
                Loot = g,
                Element = g.Element,
                ElementChance = s.ElementChance,
                ElementPower = s.ElementPower,
                CritChance = s.CritChance,
                CritMultiplier = s.CritMultiplier,
                Handling = s.Handling,
                HeatPerShot = s.HeatPerShot,
                TraitMask = mask
            };
        }
    }
}
