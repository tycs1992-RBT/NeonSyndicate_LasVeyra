using System.Collections.Generic;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// Loot drops and rewards: rarity tables per source and difficulty, drop chances, the pity rule, weapon cores, the
    /// Forge's ARMORY maths, prices, champion rolls, and the save record round trip on an offline stand-in for JsonUtility.
    /// </summary>
    public sealed class LootDropTests
    {
        private static Dictionary<Rarity, int> Histogram(LootSource s, bool veteran, int n)
        {
            Dictionary<Rarity, int> h = new Dictionary<Rarity, int>();
            for (int i = 0; i < n; i++)
            {
                Rarity r = LootDropRules.RollRarity(s, veteran, (i + 0.5f) / n);
                int c;
                h.TryGetValue(r, out c);
                h[r] = c + 1;
            }
            return h;
        }

        private static int Count(Dictionary<Rarity, int> h, Rarity atLeast)
        {
            int n = 0;
            foreach (KeyValuePair<Rarity, int> kv in h) if (kv.Key >= atLeast) n += kv.Value;
            return n;
        }

        [Test]
        public void RarityTablesRespectEachSource()
        {
            const int n = 20000;
            Dictionary<Rarity, int> normal = Histogram(LootSource.Normal, false, n);
            int commonish;
            normal.TryGetValue(Rarity.Common, out commonish);
            int unc;
            normal.TryGetValue(Rarity.Uncommon, out unc);
            Assert.That(commonish + unc, Is.GreaterThan(n * 85 / 100), "normal enemies drop mostly Common and Uncommon");
            Assert.That(Count(Histogram(LootSource.Elite, false, n), Rarity.Rare), Is.EqualTo(n), "elites drop Rare or better");
            Assert.That(Count(Histogram(LootSource.EliteCreature, false, n), Rarity.Rare), Is.EqualTo(n), "elite creatures drop Rare or better");
            Assert.That(Count(Histogram(LootSource.Champion, false, n), Rarity.Epic), Is.EqualTo(n), "champions drop Epic or better");
            Assert.That(Count(Histogram(LootSource.GreatBeast, false, n), Rarity.Epic), Is.EqualTo(n), "great beasts drop Epic or better");
            Dictionary<Rarity, int> boss = Histogram(LootSource.Boss, false, n);
            Assert.That(Count(boss, Rarity.Legendary), Is.EqualTo(n), "bosses drop Legendary or better");
            Assert.That(Count(boss, Rarity.Mythic), Is.InRange(n / 10, n / 4), "bosses sometimes drop Mythic");
            Assert.That(LootDropRules.Floor(LootSource.Champion), Is.EqualTo(Rarity.Epic));
            Assert.That(LootDropRules.Floor(LootSource.Boss), Is.EqualTo(Rarity.Legendary));
            // Frequencies follow the weights.
            float[] w = LootDropRules.RarityWeights(LootSource.Normal, false);
            float total = 0f;
            foreach (float f in w) total += f;
            for (int r = 0; r < w.Length; r++)
            {
                int got;
                normal.TryGetValue((Rarity)r, out got);
                Assert.That(got / (float)n, Is.EqualTo(w[r] / total).Within(0.002f), "Normal table, rarity " + (Rarity)r);
            }
        }

        [Test]
        public void VeteranImprovesTheOdds()
        {
            const int n = 20000;
            foreach (LootSource s in new[] { LootSource.Normal, LootSource.Elite, LootSource.Champion, LootSource.Boss, LootSource.EliteCreature, LootSource.GreatBeast })
            {
                Rarity top = s == LootSource.Boss ? Rarity.Mythic : (s == LootSource.Champion || s == LootSource.GreatBeast ? Rarity.Legendary : Rarity.Epic);
                Assert.That(Count(Histogram(s, true, n), top), Is.GreaterThan(Count(Histogram(s, false, n), top)), s + " on Veteran");
            }
            Assert.That(LootDropRules.GunChance(LootSource.Normal, NpcRole.Gang, true), Is.GreaterThan(LootDropRules.GunChance(LootSource.Normal, NpcRole.Gang, false)));
        }

        [Test]
        public void DropChancesAreSane()
        {
            foreach (LootSource s in new[] { LootSource.Elite, LootSource.Champion, LootSource.Boss })
                Assert.That(LootDropRules.GunChance(s, NpcRole.Gang, false), Is.EqualTo(1f), s + " always drops a gun");
            Assert.That(LootDropRules.GunChance(LootSource.Normal, NpcRole.Civilian, true), Is.EqualTo(0f), "civilians never drop loot guns");
            foreach (NpcRole role in new[] { NpcRole.Gang, NpcRole.Police, NpcRole.Swat, NpcRole.Enemy })
            {
                float c = LootDropRules.GunChance(LootSource.Normal, role, false);
                Assert.That(c, Is.InRange(0.03f, 0.2f), role + " drops a gun now and then");
                Assert.That(LootDropRules.GunChance(LootSource.Normal, role, true), Is.LessThanOrEqualTo(1f));
            }
            Assert.That(LootDropRules.GunChance(LootSource.Creature, NpcRole.Enemy, false), Is.LessThan(0.05f), "creatures rarely drop guns");
            Assert.That(LootDropRules.GunChance(LootSource.EliteCreature, NpcRole.Enemy, false), Is.GreaterThan(LootDropRules.GunChance(LootSource.Creature, NpcRole.Enemy, false)));
            Assert.That(LootDropRules.LevelBonus(LootSource.Boss), Is.GreaterThan(LootDropRules.LevelBonus(LootSource.Normal)));
        }

        [Test]
        public void PityGuaranteesAnEpic()
        {
            int counter = 0;
            for (int i = 1; i < LootDropRules.PityLimit; i++)
            {
                Assert.That(LootDropRules.ApplyPity(Rarity.Common, ref counter), Is.EqualTo(Rarity.Common), "drop " + i);
                Assert.That(counter, Is.EqualTo(i));
            }
            Assert.That(LootDropRules.ApplyPity(Rarity.Uncommon, ref counter), Is.EqualTo(Rarity.Epic), "the long dry run ends in an Epic");
            Assert.That(counter, Is.EqualTo(0));
            LootDropRules.ApplyPity(Rarity.Rare, ref counter);
            LootDropRules.ApplyPity(Rarity.Legendary, ref counter);
            Assert.That(counter, Is.EqualTo(0), "an Epic or better resets the count");
        }

        [Test]
        public void CoresDropFromToughFoes()
        {
            List<KeyValuePair<string, int>> got = new List<KeyValuePair<string, int>>();
            for (int i = 0; i < 200; i++)
            {
                got.Clear();
                LootDropRules.RollCores(LootSource.Champion, false, new GRand(i + 1), Element.None, got);
                int elemental = 0;
                foreach (KeyValuePair<string, int> kv in got)
                {
                    Assert.That(MaterialDb.Get(kv.Key), Is.Not.Null, kv.Key + " is a real material");
                    if (System.Array.IndexOf(LootDropRules.ElementCores, kv.Key) >= 0) elemental += kv.Value;
                }
                Assert.That(elemental, Is.GreaterThanOrEqualTo(1), "a champion always drops a weapon core");
                got.Clear();
                LootDropRules.RollCores(LootSource.Boss, false, new GRand(i + 7), Element.Frost, got);
                int frost = 0, prism = 0;
                foreach (KeyValuePair<string, int> kv in got)
                {
                    if (kv.Key == LootDropRules.CoreFrost) frost += kv.Value;
                    if (kv.Key == LootDropRules.CorePrism) prism += kv.Value;
                }
                Assert.That(frost, Is.EqualTo(2), "a boss drops two cores of its element");
                Assert.That(prism, Is.EqualTo(1), "and a prism core");
                got.Clear();
                LootDropRules.RollCores(LootSource.Normal, true, new GRand(i + 3), Element.None, got);
                Assert.That(got.Count, Is.EqualTo(0), "ordinary enemies drop no cores");
            }
            foreach (string id in new[] { LootDropRules.CoreVolt, LootDropRules.CoreEmber, LootDropRules.CoreFrost, LootDropRules.CoreVoid, LootDropRules.CorePrism, LootDropRules.CoreApex })
                Assert.That(MaterialDb.Get(id), Is.Not.Null, id + " is in the material catalog");
        }

        private static void CheckCostIds(ForgeCost c, string what)
        {
            foreach (KeyValuePair<string, int> kv in c.Materials)
            {
                Assert.That(MaterialDb.Get(kv.Key), Is.Not.Null, what + ": " + kv.Key + " is a real material");
                Assert.That(kv.Value, Is.GreaterThan(0), what + ": positive count");
            }
        }

        [Test]
        public void ForgeArmoryMathIsHonest()
        {
            for (int i = 0; i < 300; i++)
            {
                LootGun g = LootGenerator.Generate(31337 + i * 101, 1 + i % 40, (Rarity)(i % 6));
                Element e = (Element)(1 + i % 4);
                if (LootForgeMath.CanInfuse(g, e))
                {
                    ForgeCost c = LootForgeMath.InfuseCost(g, e);
                    CheckCostIds(c, "infuse");
                    Assert.That(c.Materials[0].Key, Is.EqualTo(LootDropRules.CoreFor(e)), "infusing takes that element's core");
                    LootGun n = LootForgeMath.Infused(g, e);
                    Assert.That(n.Element, Is.EqualTo(e));
                    Assert.That(n.Rarity == g.Rarity && n.Level == g.Level && n.Uid == g.Uid, Is.True, "nothing else changes");
                    Assert.That(n.Parts, Is.EqualTo(g.Parts), "parts kept");
                    Assert.That(n.Version, Is.GreaterThan(g.Version), "version bumped so models refresh");
                }
                if (LootForgeMath.CanAscend(g))
                {
                    ForgeCost c = LootForgeMath.AscendCost(g);
                    CheckCostIds(c, "ascend");
                    Assert.That(c.Cash, Is.GreaterThan(0));
                    LootGun n = LootForgeMath.Ascended(g);
                    Assert.That(n.Rarity, Is.EqualTo(g.Rarity + 1), "one rarity step");
                    int changed = 0;
                    for (int p = 0; p < LootGenerator.PartCount; p++) if (n.Parts[p] != g.Parts[p]) changed++;
                    Assert.That(changed, Is.EqualTo(1), "exactly one part rerolled");
                    foreach (LootTrait t in g.Traits) Assert.That(n.HasTrait(t), Is.True, "trait kept: " + t);
                    Assert.That(LootGenerator.TraitSetValid(n), Is.True, "traits still valid");
                    if (n.Rarity == Rarity.Mythic)
                    {
                        bool heart = false;
                        foreach (KeyValuePair<string, int> kv in c.Materials) heart |= System.Array.IndexOf(ForgeDb.Hearts, kv.Key) >= 0;
                        Assert.That(heart, Is.True, "a Mythic ascension needs a boss heart");
                    }
                }
                else
                {
                    Assert.That(g.Rarity, Is.EqualTo(Rarity.Mythic), "only Mythic guns cannot ascend");
                    Assert.That(LootForgeMath.AscendCost(g).Materials.Count, Is.EqualTo(0));
                }
                Assert.That(LootForgeMath.LevelCost(g, g.Level).Cash, Is.EqualTo(0), "no cost to stay at the same level");
                if (g.Level < LootGenerator.MaxLevel)
                {
                    LootGun up = LootForgeMath.Leveled(g, g.Level + 5);
                    Assert.That(up.Level, Is.GreaterThan(g.Level));
                    Assert.That(up.Stats.Damage, Is.GreaterThan(g.Stats.Damage), "a higher level hits harder");
                    CheckCostIds(LootForgeMath.LevelCost(g, g.Level + 5), "level");
                }
                Assert.That(LootForgeMath.Leveled(g, 1).Level, Is.EqualTo(g.Level), "raising never lowers the level");
                ForgeCost y = LootForgeMath.ScrapYield(g);
                CheckCostIds(y, "scrap");
                Assert.That(y.Materials.Count, Is.GreaterThan(0), "scrapping always gives something back");
            }
            // Costs and yields grow with rarity.
            int lastAscend = 0;
            for (int r = 0; r < (int)Rarity.Mythic; r++)
            {
                LootGun g = LootGenerator.Generate(77 + r, 10, (Rarity)r);
                int cash = LootForgeMath.AscendCost(g).Cash;
                Assert.That(cash, Is.GreaterThan(lastAscend), "ascending a rarer gun costs more");
                lastAscend = cash;
            }
        }

        [Test]
        public void PricesFollowRarityAndLevel()
        {
            int last = 0;
            for (int r = 0; r <= (int)Rarity.Mythic; r++)
            {
                LootGun g = LootGenerator.Generate(55 + r, 10, (Rarity)r);
                int v = LootEconomy.SellValue(g);
                Assert.That(v, Is.GreaterThan(last), "rarer sells for more");
                Assert.That(LootEconomy.BuyPrice(g), Is.EqualTo(v * 4), "a shop asks four times what it pays");
                last = v;
                LootGun high = g.Clone();
                high.Level = 40;
                LootGenerator.Refresh(high);
                Assert.That(LootEconomy.SellValue(high), Is.GreaterThan(v), "a higher level sells for more");
            }
            for (int stock = 1; stock < 40; stock++)
            {
                for (int i = 0; i < LootEconomy.ShopSlots; i++)
                {
                    LootGun a = LootEconomy.ShopGun(stock, i, 12);
                    Assert.That(a.Rarity, Is.LessThanOrEqualTo(Rarity.Rare), "shops sell Common to Rare");
                    Assert.That(LootEconomy.ShopGun(stock, i, 12).Name, Is.EqualTo(a.Name), "the stock is seeded");
                }
            }
        }

        [Test]
        public void ChampionsRollOneToThreeAffixes()
        {
            for (int i = 0; i < 500; i++)
            {
                int m = ChampionRules.RollAffixes(new GRand(i + 1), false);
                Assert.That(ChampionRules.Count(m), Is.InRange(1, 3), "affix count");
                Assert.That(m >> ChampionRules.AffixCount, Is.EqualTo(0), "only known affixes");
                Assert.That(ChampionRules.Count(ChampionRules.RollAffixes(new GRand(i + 1), true)), Is.InRange(2, 3), "Veteran champions carry at least two");
                string name = ChampionRules.MakeName(new GRand(i + 9));
                Assert.That(name.Split(' ').Length, Is.GreaterThanOrEqualTo(2), name);
                Assert.That(name.IndexOf('-') < 0, Is.True, name);
                Assert.That(ChampionRules.AffixLine(m).Length, Is.GreaterThan(0));
            }
            Assert.That(ChampionRules.HealthMultiplier, Is.GreaterThan(3.2f), "champions are tougher than Shard-Touched elites");
        }

        [Test]
        public void BeamsAndTimersGrowWithRarity()
        {
            for (int r = 1; r <= (int)Rarity.Mythic; r++)
            {
                Assert.That(LootDropRules.BeamHeight((Rarity)r), Is.GreaterThan(LootDropRules.BeamHeight((Rarity)(r - 1))), "beam height " + (Rarity)r);
                Assert.That(LootDropRules.DespawnSeconds((Rarity)r), Is.GreaterThanOrEqualTo(LootDropRules.DespawnSeconds((Rarity)(r - 1))), "despawn " + (Rarity)r);
            }
            Assert.That(LootDropRules.BeamHeight(Rarity.Legendary), Is.GreaterThan(10f), "Legendary beams tower over the street");
        }

        /// <summary>What JsonUtility does to a record: a field-by-field copy into a fresh object (lists copied).</summary>
        private static LootGunSave StandIn(LootGunSave s)
        {
            LootGunSave c = new LootGunSave
            {
                Uid = s.Uid, Seed = s.Seed, Archetype = s.Archetype, Maker = s.Maker, Rarity = s.Rarity, Level = s.Level,
                Element = s.Element, Favorite = s.Favorite, Unique = s.Unique, Ascensions = s.Ascensions
            };
            c.Parts.AddRange(s.Parts);
            c.Traits.AddRange(s.Traits);
            return c;
        }

        [Test]
        public void SaveRecordsRebuildIdenticalGuns()
        {
            for (int i = 0; i < 400; i++)
            {
                LootGun g = LootGenerator.Generate(2024 + i * 37, 1 + i % 50, (Rarity)(i % 6));
                g.Uid = i + 1;
                g.Favorite = i % 3 == 0;
                if (i % 5 == 0 && LootForgeMath.CanAscend(g)) g = LootForgeMath.Ascended(g);
                LootGun back = LootRecords.FromSave(StandIn(LootRecords.ToSave(g)));
                Assert.That(back.Name, Is.EqualTo(g.Name), "name " + i);
                Assert.That(back.Uid == g.Uid && back.Favorite == g.Favorite && back.Unique == g.Unique && back.Ascensions == g.Ascensions, Is.True, "fields " + i);
                Assert.That(back.Rarity == g.Rarity && back.Level == g.Level && back.Element == g.Element && back.Archetype == g.Archetype && back.Maker == g.Maker, Is.True, "record " + i);
                Assert.That(back.Parts, Is.EqualTo(g.Parts), "parts " + i);
                Assert.That(back.Traits, Is.EqualTo(g.Traits), "traits " + i);
                Assert.That(back.Stats.Damage, Is.EqualTo(g.Stats.Damage), "damage " + i);
                Assert.That(back.Stats.Clip, Is.EqualTo(g.Stats.Clip), "magazine " + i);
            }
            // Garbage from a hand-edited save is clamped instead of crashing.
            LootGunSave bad = new LootGunSave { Uid = 5, Archetype = 99, Maker = -3, Rarity = 42, Level = 900, Element = 17, Unique = 999 };
            bad.Parts.AddRange(new[] { 9, -1, 2 });
            bad.Traits.AddRange(new[] { 77, (int)LootTrait.SpinUp });
            LootGun fixedUp = LootRecords.FromSave(bad);
            Assert.That(fixedUp.Level, Is.EqualTo(LootGenerator.MaxLevel));
            Assert.That(fixedUp.Unique, Is.EqualTo(-1));
            Assert.That(LootGenerator.TraitSetValid(fixedUp), Is.True);
            Assert.That(LootRecords.FromSave(null), Is.Null);
            // An old save has no loot block content: an empty armory.
            LootSave empty = new LootSave();
            Assert.That(empty.Guns.Count == 0 && empty.Slots.Count == 0 && empty.NextUid == 1, Is.True);
        }
    }
}
