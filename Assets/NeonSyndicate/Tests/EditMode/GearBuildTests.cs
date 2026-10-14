using System.Collections.Generic;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// Phase 6 gear: the six build sets appended after the three classic sets (2, 3 and 4 piece bonuses), the gun hit
    /// and damage taken maths they feed, gear score item levels and scores, and the city tier tables and drop rules.
    /// </summary>
    public sealed class GearBuildTests
    {
        private static int[] Pieces(int set, int n)
        {
            int[] p = new int[GearBuildRules.SetCount];
            if (set >= 0) p[set] = n;
            return p;
        }

        private static bool Clean(string s)
        {
            if (s == null) return false;
            foreach (char c in s) if (c > 126) return false;
            return true;
        }

        // ------------------------------------------------------------------ sets

        [Test]
        public void BuildSetsAreAppendedAfterTheClassicThree()
        {
            Assert.That(GearSystem.SetNames.Length, Is.EqualTo(GearBuildRules.SetCount));
            Assert.That(GearSystem.SetBonus2.Length, Is.EqualTo(GearBuildRules.SetCount));
            Assert.That(GearSystem.SetBonus3.Length, Is.EqualTo(GearBuildRules.SetCount));
            Assert.That(GearSystem.SetBonus4.Length, Is.EqualTo(GearBuildRules.SetCount));
            Assert.That(GearSystem.SetNames[0], Is.EqualTo("Neon Revenant"), "saves store the set index: the classic sets keep theirs");
            Assert.That(GearSystem.SetNames[1], Is.EqualTo("Frostline Warden"));
            Assert.That(GearSystem.SetNames[2], Is.EqualTo("Rift Walker"));
            HashSet<string> names = new HashSet<string>();
            for (int s = 0; s < GearBuildRules.SetCount; s++)
            {
                Assert.That(names.Add(GearSystem.SetNames[s]), Is.True, "unique set names");
                Assert.That(Clean(GearSystem.SetNames[s]) && Clean(GearSystem.SetBonus2[s]) && Clean(GearSystem.SetBonus3[s]) && Clean(GearSystem.SetBonus4[s]), Is.True, "plain ASCII: set " + s);
                Assert.That(GearSystem.SetBonus2[s].Length > 0 && GearSystem.SetBonus4[s].Length > 0, Is.True);
                Assert.That(GearSystem.SetBonus3[s].Length > 0, Is.EqualTo(s >= GearBuildRules.FirstBuildSet), "only the build sets have a 3 piece bonus");
                Assert.That(GearSystem.SetBonus(s, 2), Is.EqualTo(GearSystem.SetBonus2[s]));
                Assert.That(GearSystem.SetBonus(s, 4), Is.EqualTo(GearSystem.SetBonus4[s]));
            }
            Assert.That(GearSystem.SetBonus(99, 2), Is.EqualTo(""));
            Assert.That(GearSystem.SetBonus(3, 1), Is.EqualTo(""));
        }

        [Test]
        public void TiersFollowPieceCounts()
        {
            Assert.That(GearBuildRules.TierOf(0, 1), Is.EqualTo(0));
            Assert.That(GearBuildRules.TierOf(0, 2), Is.EqualTo(2));
            Assert.That(GearBuildRules.TierOf(0, 3), Is.EqualTo(2), "classic sets have no 3 piece bonus");
            Assert.That(GearBuildRules.TierOf(0, 4), Is.EqualTo(4));
            Assert.That(GearBuildRules.TierOf(GearBuildRules.SetGearwright, 3), Is.EqualTo(3));
            Assert.That(GearBuildRules.TierOf(GearBuildRules.SetGearwright, 6), Is.EqualTo(4));
        }

        [Test]
        public void NoSetsMeansNoBonuses()
        {
            BuildBonuses b = GearBuildRules.Resolve(Pieces(-1, 0));
            Assert.That(b.GunDamageFar + b.GunDamageNear + b.GearCritChance + b.CritCooldownCut + b.KillArmor + b.ArmorRegen + b.HighArmorDamageCut +
                        b.GadgetCooldown + b.GadgetPower + b.GadgetDuration + b.KillCooldownCut + b.GunKillEnergy + b.PowerDamage + b.BurstCooldownCut +
                        b.KillHeal + b.ScannerDuration + b.MarkedDamage, Is.EqualTo(0f));
            Assert.That(b.CloseKillReload || b.MarkedKillPulse, Is.False);
            BuildBonuses classic = GearBuildRules.Resolve(Pieces(0, 4));
            Assert.That(classic.GunDamageFar + classic.KillArmor + classic.GadgetCooldown, Is.EqualTo(0f), "classic sets keep their effects inside GearSystem");
            Assert.That(GearBuildRules.Resolve(null).KillArmor, Is.EqualTo(0f));
        }

        [Test]
        public void EachBuildSetTurnsOnItsBonusesInOrder()
        {
            BuildBonuses d2 = GearBuildRules.Resolve(Pieces(GearBuildRules.SetLongsight, 2)), d3 = GearBuildRules.Resolve(Pieces(GearBuildRules.SetLongsight, 3)), d4 = GearBuildRules.Resolve(Pieces(GearBuildRules.SetLongsight, 4));
            Assert.That(d2.GunDamageFar, Is.GreaterThan(0f));
            Assert.That(d2.GearCritChance, Is.EqualTo(0f));
            Assert.That(d3.GearCritChance, Is.GreaterThan(0f));
            Assert.That(d3.CritCooldownCut, Is.EqualTo(0f));
            Assert.That(d4.CritCooldownCut, Is.GreaterThan(0f));
            Assert.That(d4.GunDamageFar, Is.EqualTo(d2.GunDamageFar), "lower tiers stay on");

            BuildBonuses b3 = GearBuildRules.Resolve(Pieces(GearBuildRules.SetStoneward, 3)), b4 = GearBuildRules.Resolve(Pieces(GearBuildRules.SetStoneward, 4));
            Assert.That(b3.KillArmor > 0f && b3.ArmorRegen > 0f && b3.HighArmorDamageCut == 0f, Is.True);
            Assert.That(b4.HighArmorDamageCut, Is.GreaterThan(0f));

            BuildBonuses t2 = GearBuildRules.Resolve(Pieces(GearBuildRules.SetGearwright, 2)), t4 = GearBuildRules.Resolve(Pieces(GearBuildRules.SetGearwright, 4));
            Assert.That(t2.GadgetCooldown, Is.GreaterThan(0f));
            Assert.That(t2.GadgetPower, Is.EqualTo(0f));
            Assert.That(t4.GadgetPower > 0f && t4.GadgetDuration > 0f && t4.KillCooldownCut > 0f, Is.True);

            BuildBonuses r4 = GearBuildRules.Resolve(Pieces(GearBuildRules.SetResonant, 4));
            Assert.That(r4.GunKillEnergy > 0f && r4.PowerDamage > 0f && r4.BurstCooldownCut > 0f, Is.True);

            BuildBonuses a3 = GearBuildRules.Resolve(Pieces(GearBuildRules.SetAlley, 3)), a4 = GearBuildRules.Resolve(Pieces(GearBuildRules.SetAlley, 4));
            Assert.That(a3.GunDamageNear > 0f && a3.KillHeal > 0f && !a3.CloseKillReload, Is.True);
            Assert.That(a4.CloseKillReload, Is.True);

            BuildBonuses v4 = GearBuildRules.Resolve(Pieces(GearBuildRules.SetVanta, 4));
            Assert.That(v4.ScannerDuration > 0f && v4.MarkedDamage > 0f && v4.MarkedKillPulse, Is.True);

            // Two sets at once (2 + 2) both apply.
            int[] mix = Pieces(GearBuildRules.SetGearwright, 2);
            mix[GearBuildRules.SetStoneward] = 2;
            BuildBonuses m = GearBuildRules.Resolve(mix);
            Assert.That(m.GadgetCooldown > 0f && m.KillArmor > 0f, Is.True);
        }

        [Test]
        public void GunHitsUseRangeAndGearCrits()
        {
            BuildBonuses none = new BuildBonuses();
            bool crit;
            Assert.That(GearBuildRules.GunHitMultiplier(none, 30f, 0f, out crit), Is.EqualTo(1f));
            Assert.That(crit, Is.False, "no chance, no crit");
            BuildBonuses far = GearBuildRules.Resolve(Pieces(GearBuildRules.SetLongsight, 3));
            Assert.That(GearBuildRules.GunHitMultiplier(far, 30f, 0.99f, out crit), Is.EqualTo(1f + far.GunDamageFar).Within(1e-5f));
            Assert.That(GearBuildRules.GunHitMultiplier(far, 10f, 0.99f, out crit), Is.EqualTo(1f).Within(1e-5f), "no far bonus up close");
            Assert.That(GearBuildRules.GunHitMultiplier(far, 30f, 0.01f, out crit), Is.EqualTo((1f + far.GunDamageFar) * GearBuildRules.GearCritMultiplier).Within(1e-5f));
            Assert.That(crit, Is.True);
            BuildBonuses near = GearBuildRules.Resolve(Pieces(GearBuildRules.SetAlley, 2));
            Assert.That(GearBuildRules.GunHitMultiplier(near, 5f, 0.5f, out crit), Is.EqualTo(1f + near.GunDamageNear).Within(1e-5f));
            Assert.That(GearBuildRules.GunHitMultiplier(near, 20f, 0.5f, out crit), Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void HighArmorCutsDamageTaken()
        {
            BuildBonuses b = GearBuildRules.Resolve(Pieces(GearBuildRules.SetStoneward, 4));
            Assert.That(GearBuildRules.DamageTakenScale(b, 80f, 100f), Is.EqualTo(1f - b.HighArmorDamageCut).Within(1e-5f));
            Assert.That(GearBuildRules.DamageTakenScale(b, 50f, 100f), Is.EqualTo(1f), "exactly half is not above half");
            Assert.That(GearBuildRules.DamageTakenScale(b, 10f, 0f), Is.EqualTo(1f));
            Assert.That(GearBuildRules.DamageTakenScale(new BuildBonuses(), 100f, 100f), Is.EqualTo(1f));
        }

        [Test]
        public void ActiveLinesListEveryBonusWorn()
        {
            int[] p = Pieces(GearBuildRules.SetVanta, 3);
            p[0] = 4;
            List<string> lines = GearBuildRules.ActiveLines(p, GearSystem.SetNames, GearSystem.SetBonus2, GearSystem.SetBonus3, GearSystem.SetBonus4);
            Assert.That(lines.Count, Is.EqualTo(4), "classic 2 and 4, Vanta 2 and 3");
            Assert.That(lines[0], Does.StartWith("Neon Revenant 2:"));
            Assert.That(lines[3], Does.StartWith("Vanta Shroud 3:"));
            Assert.That(GearBuildRules.ActiveLines(null, GearSystem.SetNames, null, null, null).Count, Is.EqualTo(0));
        }

        // ------------------------------------------------------------------ gear score

        [Test]
        public void ItemLevelsAndScores()
        {
            Assert.That(GearScoreRules.GearItemLevel(1, 0), Is.EqualTo(3));
            Assert.That(GearScoreRules.GearItemLevel(16, 5), Is.EqualTo(58));
            Assert.That(GearScoreRules.GearItemLevel(40, 5), Is.EqualTo(GearScoreRules.MaxItemLevel));
            Assert.That(GearScoreRules.GearItemLevel(0, -3), Is.EqualTo(3), "bad records still give a sane level");
            Assert.That(GearScoreRules.GunItemLevel(1, 0), Is.EqualTo(1));
            Assert.That(GearScoreRules.GunItemLevel(50, 3), Is.EqualTo(56));
            Assert.That(GearScoreRules.GunItemLevel(50, 9), Is.EqualTo(GearScoreRules.MaxItemLevel));
            for (int r = 1; r <= (int)Rarity.Mythic; r++)
                Assert.That(GearScoreRules.ItemScore(40, (Rarity)r), Is.GreaterThan(GearScoreRules.ItemScore(40, (Rarity)(r - 1))), "rarer scores higher");
            Assert.That(GearScoreRules.ItemScore(30, Rarity.Epic), Is.GreaterThan(GearScoreRules.ItemScore(20, Rarity.Epic)));
            Assert.That(GearScoreRules.ItemScore(50, Rarity.Mythic), Is.EqualTo(100));
            Assert.That(GearScoreRules.Total(new List<int> { 10, 20, -5 }), Is.EqualTo(30), "negative entries never lower the total");
            Assert.That(GearScoreRules.Total(null), Is.EqualTo(0));
        }

        [Test]
        public void TiersFollowTheScore()
        {
            Assert.That(GearScoreRules.TierFor(0), Is.EqualTo(1));
            Assert.That(GearScoreRules.TierFor(99), Is.EqualTo(1));
            Assert.That(GearScoreRules.TierFor(100), Is.EqualTo(2));
            Assert.That(GearScoreRules.TierFor(249), Is.EqualTo(2));
            Assert.That(GearScoreRules.TierFor(250), Is.EqualTo(3));
            Assert.That(GearScoreRules.TierFor(450), Is.EqualTo(4));
            Assert.That(GearScoreRules.TierFor(700), Is.EqualTo(5));
            Assert.That(GearScoreRules.TierFor(99999), Is.EqualTo(5));
            Assert.That(GearScoreRules.NextTierScore(1), Is.EqualTo(100));
            Assert.That(GearScoreRules.NextTierScore(4), Is.EqualTo(700));
            Assert.That(GearScoreRules.NextTierScore(5), Is.EqualTo(-1));
            for (int t = 1; t <= GearScoreRules.TierCount; t++) Assert.That(Clean(GearScoreRules.TierLabel(t)), Is.True);
        }

        [Test]
        public void TierTablesOnlyGrow()
        {
            Assert.That(GearScoreRules.EnemyHealthScale(1), Is.EqualTo(1f), "tier 1 changes nothing");
            Assert.That(GearScoreRules.EnemyDamageScale(1), Is.EqualTo(1f));
            Assert.That(GearScoreRules.LootLevelBonus(1), Is.EqualTo(0));
            Assert.That(GearScoreRules.RarityBumpChance(1), Is.EqualTo(0f));
            Assert.That(GearScoreRules.EnemyLevelOffset(1), Is.EqualTo(0));
            for (int t = 2; t <= GearScoreRules.TierCount; t++)
            {
                Assert.That(GearScoreRules.EnemyHealthScale(t), Is.GreaterThan(GearScoreRules.EnemyHealthScale(t - 1)));
                Assert.That(GearScoreRules.EnemyDamageScale(t), Is.GreaterThan(GearScoreRules.EnemyDamageScale(t - 1)));
                Assert.That(GearScoreRules.EnemyDamageScale(t), Is.LessThan(GearScoreRules.EnemyHealthScale(t)), "enemies get tougher faster than they get deadlier");
                Assert.That(GearScoreRules.LootLevelBonus(t), Is.GreaterThan(GearScoreRules.LootLevelBonus(t - 1)));
                Assert.That(GearScoreRules.RarityBumpChance(t), Is.GreaterThan(GearScoreRules.RarityBumpChance(t - 1)));
                Assert.That(GearScoreRules.EnemyLevelOffset(t), Is.GreaterThan(GearScoreRules.EnemyLevelOffset(t - 1)));
            }
            Assert.That(GearScoreRules.EnemyHealthScale(0), Is.EqualTo(1f), "out of range tiers clamp");
            Assert.That(GearScoreRules.EnemyHealthScale(9), Is.EqualTo(GearScoreRules.EnemyHealthScale(5)));
            Assert.That(GearScoreRules.EnemyLevel(10, 3), Is.EqualTo(14));
        }

        [Test]
        public void TierFeedsTheLootLevelThroughTheGeneratorRule()
        {
            for (int lvl = 1; lvl <= 55; lvl += 6)
            {
                Assert.That(GearScoreRules.LootItemLevel(lvl, 2, 1), Is.EqualTo(LootGenerator.ItemLevelFor(lvl, 2)), "tier 1 is the generator's own rule");
                Assert.That(GearScoreRules.LootItemLevel(lvl, 0, 5), Is.EqualTo(LootGenerator.ItemLevelFor(lvl, GearScoreRules.LootLevelBonus(5))));
                Assert.That(GearScoreRules.LootItemLevel(lvl, 0, 5), Is.LessThanOrEqualTo(LootGenerator.MaxLevel));
            }
            Assert.That(GearScoreRules.GearPowerBonus(1), Is.EqualTo(0));
            Assert.That(GearScoreRules.GearPowerBonus(5), Is.EqualTo(3));
        }

        [Test]
        public void RarityBumpRespectsTheTier()
        {
            Assert.That(GearScoreRules.BumpRarity(Rarity.Common, 1, 0f), Is.EqualTo(Rarity.Common), "tier 1 never bumps");
            Assert.That(GearScoreRules.BumpRarity(Rarity.Common, 3, 0f), Is.EqualTo(Rarity.Uncommon));
            Assert.That(GearScoreRules.BumpRarity(Rarity.Epic, 5, 0.2f), Is.EqualTo(Rarity.Legendary));
            Assert.That(GearScoreRules.BumpRarity(Rarity.Epic, 5, 0.3f), Is.EqualTo(Rarity.Epic), "roll above the chance");
            Assert.That(GearScoreRules.BumpRarity(Rarity.Legendary, 5, 0f), Is.EqualTo(Rarity.Legendary), "Mythic is never reached by a bump");
            Assert.That(GearScoreRules.BumpRarity(Rarity.Mythic, 5, 0f), Is.EqualTo(Rarity.Mythic));
        }

        [Test]
        public void LoadoutsLandInSensibleTiers()
        {
            // The starter kit: a Rare Shell, an Uncommon pair of Gauntlets and an Epic Weapon Core at power 2, no loot guns.
            int starter = GearScoreRules.Total(new List<int> { GearScoreRules.GearScore(2, 0, Rarity.Rare), GearScoreRules.GearScore(2, 0, Rarity.Uncommon), GearScoreRules.GearScore(2, 0, Rarity.Epic) });
            Assert.That(GearScoreRules.TierFor(starter), Is.EqualTo(1), "starter score " + starter);
            // Mid game: six Epic pieces at power 8 and four Epic guns at level 20.
            List<int> mid = new List<int>();
            for (int i = 0; i < 6; i++) mid.Add(GearScoreRules.GearScore(8, 1, Rarity.Epic));
            for (int i = 0; i < 4; i++) mid.Add(GearScoreRules.GunScore(20, 0, Rarity.Epic));
            Assert.That(GearScoreRules.TierFor(GearScoreRules.Total(mid)), Is.EqualTo(3), "mid score " + GearScoreRules.Total(mid));
            // End game: six Legendary pieces at power 15 +5 and four Legendary guns at level 48.
            List<int> end = new List<int>();
            for (int i = 0; i < 6; i++) end.Add(GearScoreRules.GearScore(15, 5, Rarity.Legendary));
            for (int i = 0; i < 4; i++) end.Add(GearScoreRules.GunScore(48, 1, Rarity.Legendary));
            Assert.That(GearScoreRules.TierFor(GearScoreRules.Total(end)), Is.EqualTo(5), "end score " + GearScoreRules.Total(end));
        }
    }
}
