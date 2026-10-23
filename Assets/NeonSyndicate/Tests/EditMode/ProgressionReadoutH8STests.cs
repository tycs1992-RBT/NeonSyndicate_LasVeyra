using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// H8S progression readability: the upgrade calls on loot cards follow the card's own first row for real generated
    /// guns, the Forge summary reports the real before and after, gear calls follow item score against the worn piece and
    /// list exactly what equipping would lose and which set bonuses GearBuildRules turns on or off, the city tier
    /// countdown lands exactly on the next threshold, the skill preview's totals equal what learning the rank produces in
    /// a real SkillBook for every node and rank, and the level-up summary names each raised attribute.
    /// </summary>
    public sealed class ProgressionReadoutH8STests
    {
        [TearDown]
        public void ClearSlots()
        {
            Weapons.ResetStatics();
        }

        private static LootGun RandomGun(GRand r, int i)
        {
            Rarity rarity = (Rarity)r.Range(0, 6);
            int level = r.Range(1, LootGenerator.MaxLevel + 1);
            return LootGenerator.Generate(7001 + i * 104729, level, rarity);
        }

        // ------------------------------------------------------------------ loot guns

        [Test]
        public void GunCallsUseTheCardsFirstRowAndAgreeWithItsDirection()
        {
            GRand r = new GRand(4242);
            int upgrades = 0, weaker = 0;
            for (int i = 0; i < 400; i++)
            {
                LootGun a = RandomGun(r, i * 2);
                LootGun b = RandomGun(r, i * 2 + 1);
                LootArmory.CardStats sa = LootArmory.StatsOf(a);
                LootArmory.CardStats sb = LootArmory.StatsOf(b);
                if (sa.RegionalMelee || sb.RegionalMelee) continue;
                GunVerdictH8S v = UpgradeVerdictRulesH8S.JudgeGun(sa, sb);
                string w = a.Name + " vs " + b.Name;
                Assert.That(v.Verdict, Is.Not.EqualTo(UpgradeVerdictH8S.None), w + ": two guns always get a call");
                // The call reads the same number as the card's first row (heat-cycle damage per second).
                Assert.That(v.Candidate, Is.EqualTo(sa.SustainedDps), w);
                Assert.That(v.Baseline, Is.EqualTo(sb.SustainedDps), w);
                float rel = (sa.SustainedDps - sb.SustainedDps) / sb.SustainedDps;
                if (rel > 0.01f) Assert.That(v.Percent, Is.GreaterThan(0), w + ": more damage per second shows a plus");
                if (rel < -0.01f) Assert.That(v.Percent, Is.LessThan(0), w + ": less damage per second shows a minus");
                Assert.That(v.Verdict == UpgradeVerdictH8S.Upgrade, Is.EqualTo(v.Percent >= UpgradeVerdictRulesH8S.BandPercent), w);
                Assert.That(v.Verdict == UpgradeVerdictH8S.Weaker, Is.EqualTo(v.Percent <= -UpgradeVerdictRulesH8S.BandPercent), w);
                Assert.That(UpgradeVerdictRulesH8S.GunHeadline(v), Is.Not.Empty, w);
                if (v.Verdict == UpgradeVerdictH8S.Upgrade) upgrades++;
                if (v.Verdict == UpgradeVerdictH8S.Weaker) weaker++;
            }
            Assert.That(upgrades, Is.GreaterThan(20), "real drops produce upgrades");
            Assert.That(weaker, Is.GreaterThan(20), "and weaker guns");
        }

        [Test]
        public void AGunMatchesItselfAndABladeNeverGetsAGunCall()
        {
            LootGun g = LootGenerator.Generate(321, 12, Rarity.Epic, (int)LootArchetype.AssaultRifle);
            GunVerdictH8S self = UpgradeVerdictRulesH8S.JudgeGun(LootArmory.StatsOf(g), LootArmory.StatsOf(g));
            Assert.That(self.Verdict, Is.EqualTo(UpgradeVerdictH8S.Similar));
            Assert.That(self.Percent, Is.EqualTo(0));
            Assert.That(UpgradeVerdictRulesH8S.GunHeadline(self), Is.EqualTo("ABOUT THE SAME (within " + UpgradeVerdictRulesH8S.BandPercent + "%)"));

            LootGun blade = LootGenerator.Generate(15, 5, Rarity.Legendary, -1, -1, 23);
            LootGun other = LootGenerator.Generate(15, 5, Rarity.Legendary, -1, -1, 24);
            Assert.That(LootArmory.StatsOf(blade).RegionalMelee, Is.True, "unique 23 is a regional blade");
            Assert.That(UpgradeVerdictRulesH8S.JudgeGun(LootArmory.StatsOf(blade), LootArmory.StatsOf(g)).Verdict, Is.EqualTo(UpgradeVerdictH8S.None));
            Assert.That(UpgradeVerdictRulesH8S.JudgeGun(LootArmory.StatsOf(g), LootArmory.StatsOf(blade)).Verdict, Is.EqualTo(UpgradeVerdictH8S.None));
            GunVerdictH8S blades = UpgradeVerdictRulesH8S.JudgeGun(LootArmory.StatsOf(blade), LootArmory.StatsOf(other));
            Assert.That(blades.Blade, Is.True, "two blades compare their base blade damage");
            Assert.That(blades.Candidate, Is.EqualTo(LootArmory.StatsOf(blade).Damage));
            Assert.That(blades.Baseline, Is.EqualTo(LootArmory.StatsOf(other).Damage));
            Assert.That(UpgradeVerdictRulesH8S.GunHeadline(blades), Does.Contain("blade damage").Or.StartWith("ABOUT THE SAME"));
            Assert.That(UpgradeVerdictRulesH8S.GunDetail(blades, "Other blade", 10, 9), Is.EqualTo("vs Other blade. Item score 10 vs 9."));
            Assert.That(UpgradeVerdictRulesH8S.GunDetail(blades, "Service Pistol", 10, -1), Is.EqualTo("vs Service Pistol. Item score 10."), "a fixed weapon has no item score");
        }

        [Test]
        public void ForgeSummaryReportsTheRealBeforeAndAfter()
        {
            for (int a = 0; a < LootGenerator.ArchetypeCount; a++)
            {
                LootGun g = LootGenerator.Generate(900 + a * 37, 8, Rarity.Rare, a);
                LootGun up = LootForgeMath.Leveled(g, 28);
                LootArmory.CardStats before = LootArmory.StatsOf(g), after = LootArmory.StatsOf(up);
                GunVerdictH8S v = UpgradeVerdictRulesH8S.JudgeGun(after, before);
                int s0 = UpgradeVerdictRulesH8S.GunItemScore(g), s1 = UpgradeVerdictRulesH8S.GunItemScore(up);
                Assert.That(s0, Is.EqualTo(GearScoreRules.GunScore(8, 0, Rarity.Rare)));
                Assert.That(s1, Is.EqualTo(GearScoreRules.GunScore(28, 0, Rarity.Rare)));
                Assert.That(s1, Is.GreaterThan(s0), "raising the level raises the item score");
                string text = UpgradeVerdictRulesH8S.ChangeSummary(v, s0, s1);
                Assert.That(text, Does.Contain("item score " + s0 + UpgradeVerdictRulesH8S.To + s1).Or.Contain("Item score " + s0 + UpgradeVerdictRulesH8S.To + s1), g.Name);
                Assert.That(text, Does.Contain(Mathf.RoundToInt(before.SustainedDps) + UpgradeVerdictRulesH8S.To + Mathf.RoundToInt(after.SustainedDps)), g.Name + ": the real damage per second before and after");
                Assert.That(text, Does.Contain(UpgradeVerdictRulesH8S.SignedPercent(v.Percent)), g.Name);

                LootGun asc = LootForgeMath.Ascended(g);
                Assert.That(UpgradeVerdictRulesH8S.GunItemScore(asc), Is.GreaterThan(s0), "an ascension raises the item score (rarity and two item levels)");
            }
        }

        [Test]
        public void PickupWhatIfAddsOrSwapsScoresAndNamesTierCrossings()
        {
            Assert.That(UpgradeVerdictRulesH8S.ScoreAfterSwap(200, -1, 30), Is.EqualTo(230), "an empty slot adds the whole score");
            Assert.That(UpgradeVerdictRulesH8S.ScoreAfterSwap(200, 20, 30), Is.EqualTo(210));
            Assert.That(UpgradeVerdictRulesH8S.ScoreAfterSwap(10, 40, 0), Is.EqualTo(0), "never below zero");
            int edge = GearScoreRules.TierThreshold[2];
            string up = UpgradeVerdictRulesH8S.EquipWhatIf(1, "Old Gun", edge - 5, edge + 5);
            Assert.That(up, Does.Contain("loot slot 2"));
            Assert.That(up, Does.Contain("replaces Old Gun"));
            Assert.That(up, Does.Contain("reaches CITY TIER " + GearScoreRules.TierFor(edge + 5)));
            Assert.That(UpgradeVerdictRulesH8S.EquipWhatIf(0, null, edge + 5, edge - 5), Does.Contain("(empty)").And.Contain("drops to CITY TIER " + GearScoreRules.TierFor(edge - 5)));
            Assert.That(UpgradeVerdictRulesH8S.EquipWhatIf(0, null, 10, 20), Does.Not.Contain("CITY TIER"));
        }

        // ------------------------------------------------------------------ gear

        private static GearItem Shell(Rarity r, int power, int seed, AffixType[] forced = null, int set = -1)
        {
            return GearSystem.GenerateSeeded(GearSlot.Shell, r, power, seed, forced, Element.None, set);
        }

        [Test]
        public void GearCallFollowsItemScoreAgainstTheWornPiece()
        {
            GearItem low = Shell(Rarity.Common, 2, 11);
            GearItem high = Shell(Rarity.Epic, 8, 12);
            int ls = GearScoreRules.GearScore(low.Power, low.Upgrade, low.Rarity), hs = GearScoreRules.GearScore(high.Power, high.Upgrade, high.Rarity);
            GearVerdictH8S up = UpgradeVerdictRulesH8S.JudgeGear(high, low);
            Assert.That(up.Verdict, Is.EqualTo(UpgradeVerdictH8S.Upgrade));
            Assert.That(up.Delta, Is.EqualTo(hs - ls));
            Assert.That(UpgradeVerdictRulesH8S.GearBadge(up), Is.EqualTo("UPGRADE +" + (hs - ls)));
            GearVerdictH8S down = UpgradeVerdictRulesH8S.JudgeGear(low, high);
            Assert.That(down.Verdict, Is.EqualTo(UpgradeVerdictH8S.Weaker));
            Assert.That(UpgradeVerdictRulesH8S.GearBadge(down), Is.EqualTo("WEAKER -" + (hs - ls)));
            GearVerdictH8S empty = UpgradeVerdictRulesH8S.JudgeGear(high, null);
            Assert.That(empty.Verdict, Is.EqualTo(UpgradeVerdictH8S.EmptySlot));
            Assert.That(empty.Delta, Is.EqualTo(hs), "an empty slot gains the whole item score");
            Assert.That(UpgradeVerdictRulesH8S.JudgeGear(high, high).Verdict, Is.EqualTo(UpgradeVerdictH8S.Equipped));
            GearItem visor = GearSystem.GenerateSeeded(GearSlot.Visor, Rarity.Common, 2, 13, null, Element.None, -1);
            Assert.That(UpgradeVerdictRulesH8S.JudgeGear(high, visor).Verdict, Is.EqualTo(UpgradeVerdictH8S.None), "a piece from another slot is no comparison");

            // An upgrade on the worn piece (UpgradeScale feeds the item level) closes the gap.
            GearItem twin = Shell(Rarity.Epic, 8, 99);
            Assert.That(UpgradeVerdictRulesH8S.JudgeGear(twin, high).Verdict, Is.EqualTo(UpgradeVerdictH8S.Similar), "same power and rarity score the same");
            high.Upgrade = 3;
            Assert.That(UpgradeVerdictRulesH8S.JudgeGear(twin, high).Verdict, Is.EqualTo(UpgradeVerdictH8S.Weaker), "the upgraded worn piece now scores higher");
        }

        [Test]
        public void AffixChangesListWhatTheWornPieceLoses()
        {
            GearItem worn = Shell(Rarity.Common, 3, 21, new[] { AffixType.MaxHealth, AffixType.Armor });
            GearItem cand = Shell(Rarity.Common, 5, 22, new[] { AffixType.Armor, AffixType.BulletResist });
            List<AffixChangeH8S> changes = new List<AffixChangeH8S>();
            UpgradeVerdictRulesH8S.AffixChanges(cand, worn, changes);
            AffixChangeH8S lost = changes.Find(c => c.Type == AffixType.MaxHealth);
            Assert.That(lost.Before, Is.EqualTo(worn.Get(AffixType.MaxHealth)));
            Assert.That(lost.After, Is.EqualTo(0f));
            Assert.That(UpgradeVerdictRulesH8S.AffixChangeLine(lost), Is.EqualTo("Loses " + GearSystem.AffixText(AffixType.MaxHealth, worn.Get(AffixType.MaxHealth))));
            AffixChangeH8S gained = changes.Find(c => c.Type == AffixType.BulletResist);
            Assert.That(gained.Gain, Is.True);
            Assert.That(UpgradeVerdictRulesH8S.AffixChangeLine(gained), Does.StartWith("Gains "));
            bool armorDiffers = Mathf.Abs(worn.Get(AffixType.Armor) - cand.Get(AffixType.Armor)) > 1e-4f;
            Assert.That(changes.Exists(c => c.Type == AffixType.Armor), Is.EqualTo(armorDiffers), "a shared affix is listed only when its total differs");
            Assert.That(changes.Count, Is.EqualTo(2 + (armorDiffers ? 1 : 0)), "nothing else changes");
            Assert.That(UpgradeVerdictRulesH8S.AffixChanges(cand, cand, changes), Is.EqualTo(0));
        }

        [Test]
        public void SetChangesMatchWhatTheBuildRulesTurnOnAndOff()
        {
            int[] counts = new int[GearSystem.SetNames.Length];
            counts[GearBuildRules.SetStoneward] = 3;
            counts[GearBuildRules.SetLongsight] = 1;
            GearItem worn = Shell(Rarity.Rare, 6, 31, null, GearBuildRules.SetStoneward);
            GearItem cand = Shell(Rarity.Rare, 6, 32, null, GearBuildRules.SetLongsight);
            Assert.That(worn.Set, Is.EqualTo(GearBuildRules.SetStoneward));
            List<SetChangeH8S> changes = new List<SetChangeH8S>();
            Assert.That(UpgradeVerdictRulesH8S.SetChanges(counts, cand, worn, changes), Is.EqualTo(2));
            SetChangeH8S off = changes[0], on = changes[1];
            Assert.That(off.Set, Is.EqualTo(GearBuildRules.SetStoneward));
            Assert.That(off.TurnsOn, Is.False);
            Assert.That(off.Tier, Is.EqualTo(3));
            Assert.That(on.Set, Is.EqualTo(GearBuildRules.SetLongsight));
            Assert.That(on.TurnsOn, Is.True);
            Assert.That(on.Tier, Is.EqualTo(2));

            // The same swap through the real bonus resolver: exactly those two bonuses change.
            int[] after = (int[])counts.Clone();
            after[GearBuildRules.SetStoneward]--;
            after[GearBuildRules.SetLongsight]++;
            BuildBonuses b0 = GearBuildRules.Resolve(counts), b1 = GearBuildRules.Resolve(after);
            Assert.That(b0.ArmorRegen, Is.GreaterThan(0f));
            Assert.That(b1.ArmorRegen, Is.EqualTo(0f), "Stoneward's 3 piece bonus really turns off");
            Assert.That(b0.GunDamageFar, Is.EqualTo(0f));
            Assert.That(b1.GunDamageFar, Is.GreaterThan(0f), "Longsight's 2 piece bonus really turns on");
            Assert.That(b1.KillArmor, Is.EqualTo(b0.KillArmor), "Stoneward's 2 piece bonus stays");
            Assert.That(UpgradeVerdictRulesH8S.SetChangeLine(off), Does.Contain("OFF").And.Contain(GearSystem.SetBonus(GearBuildRules.SetStoneward, 3)));
            Assert.That(UpgradeVerdictRulesH8S.SetChangeLine(on), Does.Contain("ON").And.Contain(GearSystem.SetBonus(GearBuildRules.SetLongsight, 2)));

            // A classic set has no 3 piece bonus: its third piece changes nothing and points at the 4 piece bonus.
            counts[0] = 2;
            GearItem classic = Shell(Rarity.Rare, 6, 33, null, 0);
            UpgradeVerdictRulesH8S.SetChanges(counts, classic, null, changes);
            Assert.That(changes.Count, Is.EqualTo(1));
            Assert.That(changes[0].Tier, Is.EqualTo(0));
            Assert.That(UpgradeVerdictRulesH8S.NextBonusPieces(0, 3), Is.EqualTo(4));
            Assert.That(UpgradeVerdictRulesH8S.SetChangeLine(changes[0]), Does.Contain("4 pieces turn on " + GearSystem.SetBonus(0, 4)));
            Assert.That(UpgradeVerdictRulesH8S.NextBonusPieces(GearBuildRules.SetVanta, 0), Is.EqualTo(2));
            Assert.That(UpgradeVerdictRulesH8S.NextBonusPieces(GearBuildRules.SetVanta, 4), Is.EqualTo(-1));
            // Swapping a piece for one of the same set changes no count.
            GearItem sameSet = Shell(Rarity.Rare, 7, 34, null, GearBuildRules.SetStoneward);
            Assert.That(UpgradeVerdictRulesH8S.SetChanges(counts, sameSet, worn, changes), Is.EqualTo(0));
        }

        // ------------------------------------------------------------------ city tier

        [Test]
        public void TierProgressCountsDownExactlyToTheNextThreshold()
        {
            float lastFraction = -1f;
            int lastTier = 0;
            for (int score = 0; score <= 900; score++)
            {
                TierProgressH8S p = UpgradeVerdictRulesH8S.TierProgress(score);
                Assert.That(p.Tier, Is.EqualTo(GearScoreRules.TierFor(score)), "score " + score);
                if (p.Tier != lastTier) lastFraction = -1f;
                lastTier = p.Tier;
                if (p.Top)
                {
                    Assert.That(p.Tier, Is.EqualTo(GearScoreRules.TierCount));
                    Assert.That(p.ToGo, Is.EqualTo(0));
                    Assert.That(p.Fraction, Is.EqualTo(1f));
                    Assert.That(UpgradeVerdictRulesH8S.TierProgressLine(p), Does.StartWith("Top city tier"));
                    continue;
                }
                Assert.That(p.ToGo, Is.GreaterThan(0), "score " + score);
                Assert.That(GearScoreRules.TierFor(score + p.ToGo), Is.EqualTo(p.Tier + 1), "exactly ToGo more reaches the next tier (" + score + ")");
                Assert.That(GearScoreRules.TierFor(score + p.ToGo - 1), Is.EqualTo(p.Tier), "one less does not (" + score + ")");
                Assert.That(p.Fraction, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
                Assert.That(p.Fraction, Is.GreaterThan(lastFraction), "the bar only fills within a tier");
                lastFraction = p.Fraction;
                Assert.That(UpgradeVerdictRulesH8S.TierProgressLine(p), Does.StartWith(p.ToGo + " more gear score reaches CITY TIER " + (p.Tier + 1)));
            }
        }

        // ------------------------------------------------------------------ skills

        /// <summary>
        /// A valid book: every node of the other two trees and of the earlier tiers of n's tree fully learned, and n at
        /// the given rank (prerequisites always sit in earlier tiers, so all of it passes SkillBook's own checks).
        /// </summary>
        private static SkillBook BookWith(SkillNode n, int rank)
        {
            SkillSave s = new SkillSave();
            foreach (SkillNode o in SkillDb.All)
            {
                if (o == n) continue;
                if (o.Tree != n.Tree || o.Tier < n.Tier)
                {
                    s.Learned.Add(o.Id);
                    s.Ranks.Add(o.MaxRank);
                }
            }
            if (rank > 0)
            {
                s.Learned.Add(n.Id);
                s.Ranks.Add(rank);
            }
            SkillBook b = new SkillBook();
            Assert.That(b.Restore(s, 1000), Is.True);
            Assert.That(b.Rank(n.Id), Is.EqualTo(rank), n.Id + " is held at rank " + rank);
            return b;
        }

        private static SkillModValues Values(SkillBook b)
        {
            SkillModValues v = new SkillModValues();
            b.Compute(v);
            v.Finish();
            return v;
        }

        [Test]
        public void NextRankPreviewEqualsWhatLearningTheRankProduces()
        {
            SkillModValues scratch = new SkillModValues();
            List<SkillEffectPreviewH8S> preview = new List<SkillEffectPreviewH8S>();
            int effects = 0;
            foreach (SkillNode n in SkillDb.All)
            {
                if (n.Effects.Length == 0) continue;
                for (int r = 0; r < n.MaxRank; r++)
                {
                    SkillModValues live = Values(BookWith(n, r));
                    SkillModValues learned = Values(BookWith(n, r + 1));
                    Assert.That(SkillRankPreviewH8S.Preview(n, r, live, scratch, preview), Is.EqualTo(n.Effects.Length), n.Id);
                    for (int i = 0; i < preview.Count; i++)
                    {
                        SkillStat stat = n.Effects[i].Stat;
                        Assert.That(preview[i].TotalNow, Is.EqualTo(live.Get(stat)).Within(1e-5f), n.Id + " now, rank " + r);
                        Assert.That(preview[i].TotalNext, Is.EqualTo(learned.Get(stat)).Within(1e-5f), n.Id + " after learning rank " + (r + 1));
                        Assert.That(preview[i].Next, Is.Not.EqualTo(preview[i].Now), n.Id + ": the next rank always adds to the skill itself");
                        Assert.That(SkillRankPreviewH8S.EffectLine(n, r, preview[i]), Does.Contain(r == 0 ? "at rank 1" : "at rank " + (r + 1)), n.Id);
                        effects++;
                    }
                }
                // Fully learned: no next rank, the totals stand still.
                SkillModValues full = Values(BookWith(n, n.MaxRank));
                SkillRankPreviewH8S.Preview(n, n.MaxRank, full, scratch, preview);
                for (int i = 0; i < preview.Count; i++)
                {
                    Assert.That(preview[i].Next, Is.EqualTo(preview[i].Now), n.Id);
                    Assert.That(preview[i].TotalNext, Is.EqualTo(preview[i].TotalNow), n.Id);
                    Assert.That(SkillRankPreviewH8S.TotalLine(n, n.MaxRank, preview[i]), Does.Not.Contain(UpgradeVerdictRulesH8S.To), n.Id);
                }
            }
            Assert.That(effects, Is.GreaterThan(100), "every rank of every stat node was checked");
        }

        [Test]
        public void ACapHoldsTheTotalAndThePreviewSaysSo()
        {
            SkillNode keenEye = SkillDb.Get("mind_keen_eye");
            SkillModValues live = new SkillModValues();
            live.Add(SkillStat.CritChance, 0.7f);
            live.Finish();
            List<SkillEffectPreviewH8S> preview = new List<SkillEffectPreviewH8S>();
            SkillRankPreviewH8S.Preview(keenEye, 1, live, new SkillModValues(), preview);
            Assert.That(preview[0].Capped, Is.True);
            Assert.That(preview[0].TotalNow, Is.EqualTo(SkillModValues.MaxOf(SkillStat.CritChance)));
            Assert.That(preview[0].TotalNext, Is.EqualTo(preview[0].TotalNow));
            Assert.That(SkillRankPreviewH8S.TotalLine(keenEye, 1, preview[0]), Does.Contain("at its limit"));
        }

        [Test]
        public void EveryStatASkillChangesReadsInPlainWords()
        {
            HashSet<SkillStat> used = new HashSet<SkillStat>();
            foreach (SkillNode n in SkillDb.All) foreach (SkillEffect e in n.Effects) used.Add(e.Stat);
            foreach (SkillStat s in used)
            {
                Assert.That(SkillRankPreviewH8S.Label(s), Is.Not.EqualTo(s.ToString()), s + " has a plain name");
                Assert.That(SkillRankPreviewH8S.Label(s), Does.Not.Contain("_"));
            }
            List<SkillEffectPreviewH8S> p = new List<SkillEffectPreviewH8S>();
            SkillModValues live = new SkillModValues(), scratch = new SkillModValues();
            SkillNode heavy = SkillDb.Get("body_heavy_hands");
            SkillRankPreviewH8S.Preview(heavy, 1, live, scratch, p);
            Assert.That(SkillRankPreviewH8S.EffectLine(heavy, 1, p[0]), Is.EqualTo("Melee damage: +4% now, +8% at rank 2"));
            Assert.That(SkillRankPreviewH8S.TotalLine(heavy, 1, p[0]), Is.EqualTo("All sources: 0%" + UpgradeVerdictRulesH8S.To + "+4%"));
            SkillRankPreviewH8S.Preview(heavy, heavy.MaxRank, live, scratch, p);
            Assert.That(SkillRankPreviewH8S.EffectLine(heavy, heavy.MaxRank, p[0]), Is.EqualTo("Melee damage: +12% (all 3 ranks)"));
            SkillNode walker = SkillDb.Get(SkillDb.VeilWalker);
            SkillRankPreviewH8S.Preview(walker, 0, live, scratch, p);
            Assert.That(SkillRankPreviewH8S.EffectLine(walker, 0, p[0]), Is.EqualTo("Veil energy drain: -50% at rank 1"), "a halving factor reads as -50%");
            SkillNode reflexes = SkillDb.Get("body_quick_reflexes");
            SkillRankPreviewH8S.Preview(reflexes, 0, live, scratch, p);
            Assert.That(SkillRankPreviewH8S.EffectLine(reflexes, 0, p[0]), Is.EqualTo("Parry window: +20 ms at rank 1"));
            SkillNode telling = SkillDb.Get(SkillDb.TellingBlow);
            SkillRankPreviewH8S.Preview(telling, 0, live, scratch, p);
            Assert.That(SkillRankPreviewH8S.TotalLine(telling, 0, p[0]), Is.EqualTo("All sources: x1.5" + UpgradeVerdictRulesH8S.To + "x2.0"), "critical hits go from one and a half to double");
            Assert.That(SkillRankPreviewH8S.LearnedSummary(p), Does.StartWith("Critical hit damage +0.0x" + UpgradeVerdictRulesH8S.To + "+0.5x"));
            Assert.That(SkillRankPreviewH8S.Preview(SkillDb.Get(SkillDb.AdrenalSurge), 0, live, scratch, p), Is.EqualTo(0), "a signature without numbers shows no block");
        }

        // ------------------------------------------------------------------ level ups

        private static LevelSnapshotH8S Snap(int level, int vigor, int endurance, int might, int focus)
        {
            return new LevelSnapshotH8S
            {
                Valid = true,
                Level = level,
                Vigor = vigor,
                Endurance = endurance,
                Might = might,
                Focus = focus,
                MaxHealth = Progression.MaxHealthFor(vigor, 0f, 0f, false),
                MaxStamina = 100f + endurance * 5f,
                MaxEnergy = 100f + focus * 6f,
                Melee = 1f + might * 0.035f,
                Gun = 1f + might * 0.015f,
                Power = 1f + focus * 0.03f
            };
        }

        [Test]
        public void LevelUpSummaryNamesEveryRaisedAttributeWithItsStat()
        {
            LevelSnapshotH8S a = Snap(6, 5, 2, 4, 1);
            LevelSnapshotH8S b = Snap(7, 6, 2, 4, 1);
            Assert.That(LevelUpSummaryH8S.Title(a, b), Is.EqualTo("LEVEL 7"));
            string body = LevelUpSummaryH8S.Body(a, b, 3);
            string to = UpgradeVerdictRulesH8S.To;
            Assert.That(body, Does.Contain("Vigor 5" + to + "6: max health " + Mathf.RoundToInt(Progression.MaxHealthFor(5, 0f, 0f, false)) + to + Mathf.RoundToInt(Progression.MaxHealthFor(6, 0f, 0f, false))));
            Assert.That(body, Does.StartWith("+1 skill point, 3 to spend in Pause > SKILLS."), "skill points come first");
            Assert.That(body, Does.Not.Contain("Might").And.Not.Contain("Focus").And.Not.Contain("Endurance"));

            LevelSnapshotH8S c = Snap(8, 6, 3, 5, 1);
            Assert.That(LevelUpSummaryH8S.Title(a, c), Is.EqualTo("LEVEL 6" + to + "8"));
            string two = LevelUpSummaryH8S.Body(a, c, -1);
            Assert.That(two, Is.EqualTo("+2 skill points. Vigor +1, Endurance +1, Might +1."), "three risen attributes use the short form");
            LevelSnapshotH8S d = Snap(8, 5, 2, 5, 1);
            string might = LevelUpSummaryH8S.Body(a, d, -1);
            Assert.That(might, Does.Contain("Might 4" + to + "5: melee x").And.Contain(", guns x"), "one attribute keeps its full clause");
            Assert.That(LevelUpSummaryH8S.Title(b, a), Is.Empty, "no banner without a rise");
            Assert.That(LevelUpSummaryH8S.Body(b, b, 1), Is.Empty);
            Assert.That(LevelUpSummaryH8S.Rose(new LevelSnapshotH8S(), b), Is.False, "an uncaptured start never posts");
        }

        [Test]
        public void LevelUpBannerStaysWithinItsTwoLinesInTheWorstCase()
        {
            LevelSnapshotH8S low = Snap(1, 0, 0, 0, 0);
            low.MaxHealth = 9999f;
            low.MaxStamina = 9999f;
            low.MaxEnergy = 9999f;
            int longest = 0;
            for (int mask = 1; mask < 16; mask++)
            {
                LevelSnapshotH8S high = Snap(99, (mask & 1) != 0 ? 40 : 0, (mask & 2) != 0 ? 40 : 0, (mask & 4) != 0 ? 40 : 0, (mask & 8) != 0 ? 40 : 0);
                high.MaxHealth = 19999f;
                high.MaxStamina = 19999f;
                high.MaxEnergy = 19999f;
                string body = LevelUpSummaryH8S.Body(low, high, 999);
                int risen = ((mask & 1) != 0 ? 1 : 0) + ((mask & 2) != 0 ? 1 : 0) + ((mask & 4) != 0 ? 1 : 0) + ((mask & 8) != 0 ? 1 : 0);
                Assert.That(body.Length, Is.LessThanOrEqualTo(risen > 2 ? LevelUpSummaryH8S.ShortFormMax : LevelUpSummaryH8S.LongFormMax), body);
                Assert.That(body, Does.StartWith("+98 skill points, 999 to spend in Pause > SKILLS."));
                longest = Mathf.Max(longest, body.Length);
            }
            Assert.That(longest, Is.LessThanOrEqualTo(LevelUpSummaryH8S.LongFormMax));
            Assert.That(LevelUpReadoutH8S.StillTimely(5f), Is.True);
            Assert.That(LevelUpReadoutH8S.StillTimely(LevelUpReadoutH8S.MaxWaitSeconds + 1f), Is.False, "a banner held back (HUD hidden) is dropped");
        }

        [Test]
        public void HoldLineJudgesTheGunItReplacesNotTheGunInHand()
        {
            // The reported case: the card compares with a weak pistol in hand and says UPGRADE, while holding E would replace a
            // stronger loot gun in slot 1. The hold line must say how the new gun does against that slot gun.
            LootArmory.CardStats pistol = LootArmory.StatsOf(Weapons.Get(WeaponId.VectorSidearm));
            Assert.That(pistol.Valid, Is.True);
            GRand r = new GRand(77);
            bool found = false;
            for (int i = 0; i < 4000 && !found; i++)
            {
                LootGun fresh = RandomGun(r, 50000 + i * 2);
                LootGun slotGun = RandomGun(r, 50001 + i * 2);
                LootArmory.CardStats sf = LootArmory.StatsOf(fresh), ss = LootArmory.StatsOf(slotGun);
                if (sf.RegionalMelee || ss.RegionalMelee) continue;
                GunVerdictH8S card = UpgradeVerdictRulesH8S.JudgeGun(sf, pistol);
                GunVerdictH8S own = UpgradeVerdictRulesH8S.JudgeGun(sf, ss);
                if (card.Verdict != UpgradeVerdictH8S.Upgrade || own.Verdict != UpgradeVerdictH8S.Weaker) continue;
                found = true;
                int total = UpgradeVerdictRulesH8S.GunItemScore(slotGun) + 150;
                string line = LootArmory.HoldWhatIfH8S(fresh, slotGun, 0, total);
                Assert.That(line, Does.Contain("loot slot 1 (replaces " + slotGun.Name + ": WEAKER " + UpgradeVerdictRulesH8S.SignedPercent(own.Percent) + ")"), line);
                Assert.That(line, Does.Not.Contain("UPGRADE"), "the card's call against the pistol does not leak into the hold line");
                int after = UpgradeVerdictRulesH8S.ScoreAfterSwap(total, UpgradeVerdictRulesH8S.GunItemScore(slotGun), UpgradeVerdictRulesH8S.GunItemScore(fresh));
                Assert.That(line, Does.Contain("gear score " + total + UpgradeVerdictRulesH8S.To + after));
                string empty = LootArmory.HoldWhatIfH8S(fresh, null, 1, 50);
                Assert.That(empty, Does.Contain("loot slot 2 (empty)").And.Not.Contain("WEAKER").And.Not.Contain("UPGRADE"));
            }
            Assert.That(found, Is.True, "real drops include a gun that beats the pistol but not the slot gun");
        }

        [Test]
        public void ForgeSummaryLeavesOutNumbersThatDidNotChange()
        {
            GunVerdictH8S flat = new GunVerdictH8S { Verdict = UpgradeVerdictH8S.Similar, Baseline = 112.2f, Candidate = 111.8f, Percent = 0 };
            Assert.That(UpgradeVerdictRulesH8S.ChangeSummary(flat, 20, 20), Is.Empty, "nothing changed, nothing said");
            Assert.That(UpgradeVerdictRulesH8S.ChangeSummary(flat, 20, 22), Is.EqualTo("Item score 20" + UpgradeVerdictRulesH8S.To + "22."));
            LootGun g = LootGenerator.Generate(4321, 14, Rarity.Rare, (int)LootArchetype.Smg);
            LootGun infused = LootForgeMath.Infused(g, g.Element == Element.Volt ? Element.Ember : Element.Volt);
            GunVerdictH8S v = UpgradeVerdictRulesH8S.JudgeGun(LootArmory.StatsOf(infused), LootArmory.StatsOf(g));
            string text = UpgradeVerdictRulesH8S.ChangeSummary(v, UpgradeVerdictRulesH8S.GunItemScore(g), UpgradeVerdictRulesH8S.GunItemScore(infused));
            bool same = Mathf.RoundToInt(v.Baseline) == Mathf.RoundToInt(v.Candidate);
            Assert.That(text.Contains("damage per second"), Is.EqualTo(!same), "an infusion mentions damage per second only when it moved: " + text);
        }

        [Test]
        public void ReadoutStaticsResetAndALevelUpWithoutAPlayerWaitsForNothing()
        {
            ProgressionHooks.ResetStatics();
            Assert.That(LevelUpReadoutH8S.Pending, Is.False);
            LevelUpReadoutH8S.Begin(5);
            Assert.That(LevelUpReadoutH8S.Pending, Is.False, "no progression system: nothing to compare later");
            ProgressionReadoutStaticsH8S.ResetStatics();
            Assert.That(LevelUpReadoutH8S.Pending, Is.False);
        }
    }
}
