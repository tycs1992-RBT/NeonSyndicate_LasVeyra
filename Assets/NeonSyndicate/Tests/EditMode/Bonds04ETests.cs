using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// Underheart and Bonds 04E, BONDS stream: the frozen save fields, ranks and trust, care cooldowns, assists and the
    /// brace, crystal tuning costs, names, the no-refill fusion snapshot rules, the roster and its normalization, the
    /// rescue state machine, the breadcrumb trail, follow decisions, the three archetypes and their real companion
    /// bodies. All engine-free.
    /// </summary>
    public sealed class Bonds04ETests
    {
        // ================================================================== save freeze

        /// <summary>Every BondSave field that has shipped, with its type. Append new fields; never edit or remove a line.</summary>
        private static readonly string[] BondSaveFields =
        {
            "Version:Int32", "NextUid:Int32", "ActiveUid:Int32", "Companions:List<CompanionRecord>", "HintsShown:Int32"
        };

        /// <summary>Every CompanionRecord field that has shipped, with its type. Append new fields; never edit or remove a line.</summary>
        private static readonly string[] CompanionRecordFields =
        {
            "Uid:Int32", "Archetype:String", "Name:String", "Rank:Int32", "Trust:Int32", "Health01:Single", "State:Int32",
            "Evolution:Int32", "Aspects:List<String>", "CareCooldown:Single", "Source:String", "Tuning:Int32",
            "Specialization:String", "RecoverIn:Single", "ContractCooldown:Single", "CareCount:Int32", "Seed:Int32",
            "Owner:String", "Milestones:List<String>"
        };

        [Test]
        public void BondSaveFieldsAreOnlyEverAppended()
        {
            CheckFields(typeof(BondSave), BondSaveFields);
            CheckFields(typeof(CompanionRecord), CompanionRecordFields);
            Assert.That(Attribute.IsDefined(typeof(BondSave), typeof(SerializableAttribute)), Is.True);
            Assert.That(Attribute.IsDefined(typeof(CompanionRecord), typeof(SerializableAttribute)), Is.True);
            FieldInfo f = typeof(SaveData).GetField("Bonds");
            Assert.That(f, Is.Not.Null, "SaveData.Bonds stays");
            Assert.That(f.FieldType, Is.EqualTo(typeof(BondSave)));
            Assert.That(new SaveData().Bonds, Is.Not.Null, "old saves get an empty block");
        }

        [Test]
        public void CompanionStateNumbersAreFrozen()
        {
            Assert.That((int)CompanionState.Resting, Is.EqualTo(0));
            Assert.That((int)CompanionState.Following, Is.EqualTo(1));
            Assert.That((int)CompanionState.Waiting, Is.EqualTo(2));
            Assert.That((int)CompanionState.Assisting, Is.EqualTo(3));
            Assert.That((int)CompanionState.Downed, Is.EqualTo(4));
            Assert.That((int)CompanionState.Fused, Is.EqualTo(5));
            Assert.That((int)CompanionState.Rescue, Is.EqualTo(6));
        }

        private static void CheckFields(Type type, string[] frozen)
        {
            Dictionary<string, string> now = new Dictionary<string, string>();
            foreach (FieldInfo f in type.GetFields(BindingFlags.Public | BindingFlags.Instance)) now[f.Name] = TypeName(f.FieldType);
            foreach (string entry in frozen)
            {
                string[] parts = entry.Split(':');
                string t;
                Assert.That(now.TryGetValue(parts[0], out t), Is.True, type.Name + "." + parts[0] + " was renamed or removed.");
                Assert.That(t, Is.EqualTo(parts[1]), type.Name + "." + parts[0] + " changed type.");
            }
            Assert.That(now.Count, Is.EqualTo(frozen.Length), type.Name + " gained a field: add it to the frozen list in Bonds04ETests.");
        }

        private static string TypeName(Type t)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>)) return "List<" + t.GetGenericArguments()[0].Name + ">";
            return t.Name;
        }

        // ================================================================== ranks, trust and care

        private static CompanionRecord Rec(string archetype, int rank)
        {
            BondSave d = new BondSave();
            bool created;
            CompanionRecord r = BondRoster.Grant(d, archetype, "", "test", rank, out created);
            Assert.That(created, Is.True);
            return r;
        }

        [Test]
        public void TrustRaisesRanksButNeverPastHarmonizedAndRescuedWaitsForCare()
        {
            CompanionRecord r = Rec(BondIds.QuartzGuide, BondIds.RankStray);
            Assert.That(BondRules.AddTrust(r, 900), Is.EqualTo(900));
            Assert.That(r.Rank, Is.EqualTo(BondIds.RankStray), "a rescued companion becomes Bonded through care, not trust");
            CompanionRecord b = Rec(BondIds.AshWarden, BondIds.RankBonded);
            BondRules.AddTrust(b, 149);
            Assert.That(b.Rank, Is.EqualTo(BondIds.RankBonded));
            BondRules.AddTrust(b, 1);
            Assert.That(b.Rank, Is.EqualTo(BondIds.RankAttuned));
            BondRules.AddTrust(b, 5000);
            Assert.That(b.Trust, Is.EqualTo(BondRules.MaxTrust));
            Assert.That(b.Rank, Is.EqualTo(BondIds.RankHarmonized), "Awakened needs the awaken entitlement");
        }

        [Test]
        public void ALaterRescueNeverBenchesATrustedOrFusedCompanion()
        {
            Assert.That(BondRules.RescueKeepsActive(false, 0, false), Is.False, "the first rescue becomes the active companion");
            Assert.That(BondRules.RescueKeepsActive(true, BondIds.RankStray, false), Is.False, "a companion still at Rescued gives way to the newcomer");
            Assert.That(BondRules.RescueKeepsActive(true, BondIds.RankBonded, false), Is.True, "a Bonded companion stays active");
            Assert.That(BondRules.RescueKeepsActive(true, BondIds.RankAttuned, false), Is.True, "an Attuned companion (the fusion partner) stays active");
            Assert.That(BondRules.RescueKeepsActive(true, BondIds.RankAwakened, false), Is.True);
            Assert.That(BondRules.RescueKeepsActive(true, BondIds.RankStray, true), Is.True, "a fused companion is never swapped out mid-fusion");
        }

        [Test]
        public void RaiseRankNeverLowersAndLiftsTrustToTheFloor()
        {
            CompanionRecord r = Rec(BondIds.QuartzGuide, BondIds.RankStray);
            Assert.That(BondRules.RaiseRank(r, BondIds.RankAttuned), Is.True);
            Assert.That(r.Rank, Is.EqualTo(BondIds.RankAttuned));
            Assert.That(r.Trust, Is.GreaterThanOrEqualTo(BondRules.TrustFloor(BondIds.RankAttuned)));
            Assert.That(BondRules.RaiseRank(r, BondIds.RankBonded), Is.False);
            Assert.That(r.Rank, Is.EqualTo(BondIds.RankAttuned));
            Assert.That(BondRules.RaiseRank(r, BondIds.RankAwakened), Is.True);
            Assert.That(r.Evolution, Is.EqualTo(2), "Awakened carries the final visual evolution");
            Assert.That(BondRules.RaiseRank(r, 99), Is.False, "clamped to Awakened");
        }

        [Test]
        public void CareSealsTheBondHealsAndIsBoundedByItsCooldown()
        {
            CompanionRecord r = Rec(BondIds.QuartzGuide, BondIds.RankStray);
            r.Health01 = 0.3f;
            CareOutcome first = BondRules.Care(r, "Glint");
            Assert.That(first.Performed, Is.True);
            Assert.That(r.Rank, Is.EqualTo(BondIds.RankBonded), "the first care seals the bond");
            Assert.That(first.TrustGained, Is.EqualTo(BondRules.TrustCare));
            Assert.That(r.Health01, Is.EqualTo(1f));
            int trust = r.Trust;
            for (int i = 0; i < 50; i++)
            {
                CareOutcome again = BondRules.Care(r, "Glint");
                Assert.That(again.Performed, Is.True, "the interaction still happens");
                Assert.That(again.OnCooldown, Is.True);
                Assert.That(again.TrustGained, Is.EqualTo(0), "no farming during the cooldown");
            }
            Assert.That(r.Trust, Is.EqualTo(trust));
            BondRules.TickTimers(r, BondRules.CareCooldown + 1f);
            Assert.That(BondRules.Care(r, "Glint").TrustGained, Is.EqualTo(BondRules.TrustCare));
            Assert.That(r.CareCount, Is.EqualTo(2));
        }

        [Test]
        public void DownedCompanionsRecoverAndAreNeverLost()
        {
            CompanionRecord r = Rec(BondIds.StormSkimmer, BondIds.RankBonded);
            float max = BondRules.MaxHealth(r.Archetype, r.Rank, r.Tuning);
            Assert.That(BondRules.Hurt(r, max * 0.5f, max), Is.False);
            Assert.That(BondRules.Hurt(r, max * 5f, max), Is.True);
            Assert.That(r.State, Is.EqualTo((int)CompanionState.Downed));
            Assert.That(r.RecoverIn, Is.EqualTo(BondRules.RecoverSeconds));
            Assert.That(BondRules.Hurt(r, max, max), Is.False, "a downed companion takes no more harm");
            Assert.That(BondRules.TickTimers(r, BondRules.RecoverSeconds - 1f), Is.False);
            Assert.That(BondRules.TickTimers(r, 2f), Is.True);
            Assert.That(r.State, Is.EqualTo((int)CompanionState.Following));
            Assert.That(r.Health01, Is.EqualTo(BondRules.RecoverHealth));
            BondRules.Hurt(r, max * 5f, max);
            CareOutcome o = BondRules.Care(r, "Skein");
            Assert.That(o.Revived, Is.True, "care brings a downed companion back at once");
            Assert.That(r.State, Is.EqualTo((int)CompanionState.Following));
            Assert.That(r.Health01, Is.EqualTo(1f));
        }

        [Test]
        public void QuestTrustPaysCampaignQuestsOnceAndContractsOnACooldown()
        {
            CompanionRecord r = Rec(BondIds.QuartzGuide, BondIds.RankBonded);
            Assert.That(BondRules.QuestTrust(r, "uh05_04_descent"), Is.EqualTo(BondRules.TrustMainQuest));
            Assert.That(BondRules.QuestTrust(r, "uh05_04_descent"), Is.EqualTo(0), "a replayed quest never pays twice");
            Assert.That(BondRules.QuestTrust(r, "uh05_s07_supply"), Is.EqualTo(BondRules.TrustSideQuest));
            Assert.That(BondRules.QuestTrust(r, "uhc_salvage"), Is.EqualTo(BondRules.TrustContract));
            Assert.That(BondRules.QuestTrust(r, "uhc_salvage"), Is.EqualTo(0), "contracts wait for their cooldown");
            BondRules.TickTimers(r, BondRules.ContractTrustCooldown + 1f);
            Assert.That(BondRules.QuestTrust(r, "uhc_courier"), Is.EqualTo(BondRules.TrustContract));
            Assert.That(BondRules.QuestTrust(r, "guild_runner_1"), Is.EqualTo(0), "legacy quests do not pay bond trust");
        }

        [Test]
        public void EvolutionStageFollowsRankAndKeepsGrantedCosmetics()
        {
            Assert.That(BondRules.EvolutionStage(BondIds.RankBonded, 0), Is.EqualTo(0));
            Assert.That(BondRules.EvolutionStage(BondIds.RankHarmonized, 0), Is.EqualTo(1));
            Assert.That(BondRules.EvolutionStage(BondIds.RankAwakened, 0), Is.EqualTo(2));
            Assert.That(BondRules.EvolutionStage(BondIds.RankBonded, 1), Is.EqualTo(1), "a cosmetic stage is kept");
            Assert.That(BondRules.EvolutionStage(BondIds.RankBonded, 7), Is.EqualTo(2));
        }

        // ================================================================== assists and the brace

        [Test]
        public void AssistNumbersGrowWithRankAndTuningAndRespectTheirSpecializations()
        {
            foreach (string a in BondIds.Archetypes)
            {
                float prev = -1f;
                for (int rank = 0; rank <= 4; rank++)
                {
                    float hp = BondRules.MaxHealth(a, rank, 0);
                    Assert.That(hp, Is.GreaterThan(0f));
                    Assert.That(hp, Is.GreaterThanOrEqualTo(prev));
                    prev = hp;
                    Assert.That(BondRules.CombatCooldown(a, rank, ""), Is.GreaterThanOrEqualTo(8f));
                    Assert.That(BondRules.ExploreCooldown(a, ""), Is.GreaterThan(0f));
                }
                Assert.That(BondRules.MaxHealth(a, 1, 3), Is.GreaterThan(BondRules.MaxHealth(a, 1, 0)));
            }
            Assert.That(BondRules.AssistDamage(BondIds.QuartzGuide, 4, 3, ""), Is.GreaterThan(BondRules.AssistDamage(BondIds.QuartzGuide, 1, 0, "")));
            Assert.That(BondRules.AssistDamage(BondIds.QuartzGuide, 3, 0, BondRules.SpecPrism), Is.GreaterThan(BondRules.AssistDamage(BondIds.QuartzGuide, 3, 0, BondRules.SpecLantern)));
            Assert.That(BondRules.AssistDamage(BondIds.AshWarden, 4, 3, ""), Is.EqualTo(0f), "the brace deals no damage");
            Assert.That(BondRules.RevealLength(BondIds.QuartzGuide, BondRules.SpecLantern), Is.GreaterThan(BondRules.RevealLength(BondIds.QuartzGuide, "")));
            BraceStats plain = BondRules.Brace(3, 0, "");
            BraceStats bulwark = BondRules.Brace(3, 0, BondRules.SpecBulwark);
            Assert.That(bulwark.Capacity, Is.GreaterThan(plain.Capacity));
            Assert.That(bulwark.HalfAngle, Is.GreaterThan(plain.HalfAngle));
            Assert.That(BondRules.CombatCooldown(BondIds.AshWarden, 3, BondRules.SpecBulwark), Is.GreaterThan(BondRules.CombatCooldown(BondIds.AshWarden, 3, "")));
            Assert.That(BondRules.Brace(3, 0, BondRules.SpecKilnHeart).HeatShade, Is.GreaterThan(0f));
            Assert.That(BondRules.DisruptSeconds(true, false, 4, ""), Is.EqualTo(0f), "bosses are never stun locked");
            Assert.That(BondRules.DisruptSeconds(false, true, 1, ""), Is.LessThan(BondRules.DisruptSeconds(false, false, 1, "")));
        }

        [Test]
        public void TheBraceTakesHitsFromItsConeUntilItsCapacityRunsOut()
        {
            BraceStats s = BondRules.Brace(1, 0, "");
            float cap = s.Capacity;
            Vector3 braceDir = Vector3.forward;
            // A hit travelling toward the player from the front (attacker ahead): absorbed by share.
            float took = BondAssistRules.Absorb(braceDir, Vector3.back, 20f, s, ref cap, true, 1f);
            Assert.That(took, Is.EqualTo(20f * s.Absorb).Within(1e-4f));
            Assert.That(cap, Is.EqualTo(s.Capacity - took).Within(1e-4f));
            // From behind: nothing.
            Assert.That(BondAssistRules.Absorb(braceDir, Vector3.forward, 20f, s, ref cap, true, 1f), Is.EqualTo(0f));
            // Out of cover range: nothing.
            Assert.That(BondAssistRules.Absorb(braceDir, Vector3.back, 20f, s, ref cap, true, s.CoverRange + 1f), Is.EqualTo(0f));
            // Not braceable (falls, vehicles, unblockable): nothing.
            Assert.That(BondAssistRules.Braceable(DamageType.Fall, false, true), Is.False);
            Assert.That(BondAssistRules.Braceable(DamageType.Vehicle, false, true), Is.False);
            Assert.That(BondAssistRules.Braceable(DamageType.Melee, true, true), Is.False);
            Assert.That(BondAssistRules.Braceable(DamageType.Fire, false, false), Is.False, "sourceless hazard heat is not a hit");
            Assert.That(BondAssistRules.Braceable(DamageType.Bullet, false, true), Is.True);
            // The capacity is a hard cap: a huge hit cannot take more than what is left, and a spent shell takes nothing.
            float big = BondAssistRules.Absorb(braceDir, Vector3.back, 100000f, s, ref cap, true, 1f);
            Assert.That(big, Is.LessThanOrEqualTo(s.Capacity));
            Assert.That(cap, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(BondAssistRules.Absorb(braceDir, Vector3.back, 20f, s, ref cap, true, 1f), Is.EqualTo(0f));
        }

        [Test]
        public void KilnHeartShadesOnlyHazardHeatAndOnlyNearTheWarden()
        {
            BraceStats s = BondRules.Brace(3, 0, BondRules.SpecKilnHeart);
            Assert.That(BondAssistRules.HeatShade(DamageType.Fire, false, 10f, s, 1f), Is.EqualTo(5f).Within(1e-4f));
            Assert.That(BondAssistRules.HeatShade(DamageType.Fire, true, 10f, s, 1f), Is.EqualTo(0f), "an attacker's fire goes through the cone rules");
            Assert.That(BondAssistRules.HeatShade(DamageType.Fire, false, 10f, s, s.CoverRange + 1f), Is.EqualTo(0f));
            Assert.That(BondAssistRules.HeatShade(DamageType.Fire, false, 10f, BondRules.Brace(3, 0, ""), 1f), Is.EqualTo(0f));
        }

        [Test]
        public void AssistDecisionsAndRefusalsAreClearAndCorrect()
        {
            AssistRefusal why;
            Assert.That(BondAssistRules.Decide(true, false, true, out why), Is.EqualTo(AssistIntent.Combat));
            Assert.That(BondAssistRules.Decide(false, true, false, out why), Is.EqualTo(AssistIntent.Refuse));
            Assert.That(why, Is.EqualTo(AssistRefusal.Neutral));
            Assert.That(BondAssistRules.Decide(false, false, true, out why), Is.EqualTo(AssistIntent.Refuse));
            Assert.That(why, Is.EqualTo(AssistRefusal.NoTarget));
            Assert.That(BondAssistRules.Decide(false, false, false, out why), Is.EqualTo(AssistIntent.Explore));
            Assert.That(BondAssistRules.ValidateTarget(BondRules.AssistRange + 1f, true, true, 0f, false), Is.EqualTo(AssistRefusal.TooFar));
            Assert.That(BondAssistRules.ValidateTarget(10f, false, true, 0f, false), Is.EqualTo(AssistRefusal.Occluded));
            Assert.That(BondAssistRules.ValidateTarget(10f, true, false, 0f, false), Is.EqualTo(AssistRefusal.Unreachable));
            Assert.That(BondAssistRules.ValidateTarget(10f, true, true, 6f, false), Is.EqualTo(AssistRefusal.Unreachable));
            Assert.That(BondAssistRules.ValidateTarget(10f, true, true, 6f, true), Is.EqualTo(AssistRefusal.None), "a flyer reaches higher");
            Assert.That(BondAssistRules.ValidateTarget(10f, true, true, 0f, false), Is.EqualTo(AssistRefusal.None));
            Assert.That(BondAssistRules.ValidateCompanion(true, BondIds.RankStray, 1, true, false, 0f, false, false), Is.EqualTo(AssistRefusal.NotBonded));
            Assert.That(BondAssistRules.ValidateCompanion(true, BondIds.RankStray, 1, false, false, 0f, false, false), Is.EqualTo(AssistRefusal.None), "a rescued companion can already lead the way");
            Assert.That(BondAssistRules.ValidateCompanion(true, 1, (int)CompanionState.Downed, false, false, 0f, false, false), Is.EqualTo(AssistRefusal.Downed));
            Assert.That(BondAssistRules.ValidateCompanion(true, 1, 1, true, false, 3f, false, false), Is.EqualTo(AssistRefusal.Cooldown));
            Assert.That(BondAssistRules.ValidateCompanion(true, 1, 1, true, true, 0f, false, false), Is.EqualTo(AssistRefusal.Busy));
            Assert.That(BondAssistRules.ValidateCompanion(true, 2, 1, true, false, 0f, true, false), Is.EqualTo(AssistRefusal.Fused));
            Assert.That(BondAssistRules.ValidateCompanion(true, 2, 1, true, false, 0f, false, true), Is.EqualTo(AssistRefusal.InVehicle));
            Assert.That(BondAssistRules.ValidateCompanion(false, 2, 1, true, false, 0f, false, false), Is.EqualTo(AssistRefusal.NoCompanion));
            foreach (AssistRefusal r in Enum.GetValues(typeof(AssistRefusal)))
            {
                string t = BondAssistRules.RefusalText(r, "Glint", 4f);
                if (r == AssistRefusal.None) Assert.That(t, Is.Empty);
                else Assert.That(t, Is.Not.Empty, r.ToString());
                AssertNoDashes(t);
            }
        }

        // ================================================================== tuning, specializations, names

        [Test]
        public void TuningCostsUseRealMaterialsAndRiseWithEachLevel()
        {
            foreach (string a in BondIds.Archetypes)
            {
                Assert.That(BondRules.TuningCost(a, 0), Is.Empty);
                Assert.That(BondRules.TuningCost(a, BondRules.MaxTuning + 1), Is.Empty);
                int prevValue = 0;
                for (int level = 1; level <= BondRules.MaxTuning; level++)
                {
                    BondCost[] cost = BondRules.TuningCost(a, level);
                    Assert.That(cost, Is.Not.Empty, a + " level " + level);
                    int value = 0;
                    foreach (BondCost c in cost)
                    {
                        MaterialDef m = MaterialDb.Get(c.Material);
                        Assert.That(m, Is.Not.Null, a + " uses unknown material " + c.Material);
                        Assert.That(c.Count, Is.GreaterThan(0));
                        value += m.Value * c.Count;
                    }
                    Assert.That(value, Is.GreaterThan(prevValue), a + " level " + level + " costs more than the last");
                    prevValue = value;
                    Assert.That(BondRules.TuningEffect(a, level), Is.Not.Empty);
                }
            }
        }

        [Test]
        public void EachArchetypeHasTwoDistinctSpecializations()
        {
            HashSet<string> all = new HashSet<string>();
            foreach (string a in BondIds.Archetypes)
            {
                string[] s = BondRules.Specializations(a);
                Assert.That(s.Length, Is.EqualTo(2));
                foreach (string id in s)
                {
                    Assert.That(all.Add(id), Is.True, "specializations are unique: " + id);
                    Assert.That(BondRules.IsSpecOf(a, id), Is.True);
                    Assert.That(BondRules.SpecTitle(id), Is.Not.EqualTo("None"));
                    Assert.That(BondRules.SpecText(id), Is.Not.Empty);
                    AssertNoDashes(BondRules.SpecText(id));
                }
            }
            Assert.That(BondRules.IsSpecOf(BondIds.QuartzGuide, BondRules.SpecBulwark), Is.False);
            Assert.That(BondRules.IsSpecOf(BondIds.QuartzGuide, ""), Is.False);
        }

        [Test]
        public void NamesAreSanitizedAndFallBackToTheDefault()
        {
            Assert.That(BondRules.SanitizeName("  Glimmer   Tail  ", "Glint"), Is.EqualTo("Glimmer Tail"));
            Assert.That(BondRules.SanitizeName("", "Glint"), Is.EqualTo("Glint"));
            Assert.That(BondRules.SanitizeName("   ", "Glint"), Is.EqualTo("Glint"));
            Assert.That(BondRules.SanitizeName("<b>!!</b>", "Glint"), Is.EqualTo("bb"));
            Assert.That(BondRules.SanitizeName("Ash\u2014Heart\u2013One", "Hob"), Is.EqualTo("AshHeartOne"), "typographic dashes are dropped");
            Assert.That(BondRules.SanitizeName("O'Kell-Two", "Hob"), Is.EqualTo("O'Kell-Two"));
            Assert.That(BondRules.SanitizeName("ABCDEFGHIJKLMNOPQRSTUVWXYZ", "Hob").Length, Is.EqualTo(BondRules.MaxNameLength));
            Assert.That(BondRules.SanitizeName("\t\n", "Skein"), Is.EqualTo("Skein"));
        }

        [Test]
        public void FusionSnapshotsNeverRefillHealthOrShortenCooldowns()
        {
            Assert.That(BondRules.RestoredHealth(0.4f, 1f), Is.EqualTo(0.4f), "fusing and separating never heals");
            Assert.That(BondRules.RestoredHealth(0.8f, 0.5f), Is.EqualTo(0.5f), "damage taken while fused is kept");
            Assert.That(BondRules.RestoredHealth(0.8f, -3f), Is.EqualTo(0f));
            Assert.That(BondRules.RestoredCooldown(10f, 0f, 3f), Is.EqualTo(7f), "time passes, the cooldown is not reset");
            Assert.That(BondRules.RestoredCooldown(10f, 9f, 3f), Is.EqualTo(9f));
            Assert.That(BondRules.RestoredCooldown(2f, 0f, 30f), Is.EqualTo(0f));
        }

        // ================================================================== roster and normalization

        [Test]
        public void GrantsNeverDuplicateAndTheFirstCompanionBecomesActive()
        {
            BondSave d = new BondSave();
            bool created;
            CompanionRecord a = BondRoster.Grant(d, BondIds.QuartzGuide, "Glint", "rescue", 0, out created);
            Assert.That(created, Is.True);
            Assert.That(d.ActiveUid, Is.EqualTo(a.Uid));
            CompanionRecord again = BondRoster.Grant(d, BondIds.QuartzGuide, "Other", "rescue", 3, out created);
            Assert.That(created, Is.False);
            Assert.That(again, Is.SameAs(a));
            Assert.That(a.Name, Is.EqualTo("Glint"));
            Assert.That(a.Rank, Is.EqualTo(0));
            Assert.That(BondRoster.Grant(d, "not_a_companion", "", "", 1, out created), Is.Null);
            CompanionRecord b = BondRoster.Grant(d, BondIds.AshWarden, "", "quest", 1, out created);
            Assert.That(b.Name, Is.EqualTo("Hob"), "the default name");
            Assert.That(b.Uid, Is.Not.EqualTo(a.Uid));
            Assert.That(d.ActiveUid, Is.EqualTo(a.Uid), "a later grant does not steal the active slot");
            Assert.That(b.State, Is.EqualTo((int)CompanionState.Resting), "it waits at home until it is brought along");
            Assert.That(a.State, Is.EqualTo((int)CompanionState.Following));
            Assert.That(BondRoster.SetActive(d, b.Uid), Is.True);
            Assert.That(BondRoster.Active(d), Is.SameAs(b));
            Assert.That(BondRoster.SetActive(d, 999), Is.False);
            Assert.That(d.Companions.Count, Is.EqualTo(2));
            Assert.That(a.Seed, Is.Not.EqualTo(0));
            Assert.That(a.Owner, Is.EqualTo("player"));
        }

        [Test]
        public void SavesFoldTransientStatesAndNeverStoreTheFusedFrame()
        {
            BondSave d = new BondSave();
            bool created;
            CompanionRecord a = BondRoster.Grant(d, BondIds.QuartzGuide, "", "", 2, out created);
            CompanionRecord b = BondRoster.Grant(d, BondIds.AshWarden, "", "", 1, out created);
            CompanionRecord c = BondRoster.Grant(d, BondIds.StormSkimmer, "", "", 1, out created);
            a.State = (int)CompanionState.Fused;
            b.State = (int)CompanionState.Waiting;
            c.State = (int)CompanionState.Downed;
            c.RecoverIn = 30f;
            BondSave s = BondRoster.CopyForSave(d);
            Assert.That(s.Companions[0].State, Is.EqualTo((int)CompanionState.Following));
            Assert.That(s.Companions[1].State, Is.EqualTo((int)CompanionState.Following));
            Assert.That(s.Companions[2].State, Is.EqualTo((int)CompanionState.Downed), "downed is durable: it keeps resting");
            Assert.That(a.State, Is.EqualTo((int)CompanionState.Fused), "the live block is untouched");
            s.Companions[0].Name = "Changed";
            Assert.That(a.Name, Is.Not.EqualTo("Changed"), "the save is a deep copy");
        }

        [Test]
        public void NormalizeMakesOldPartialAndMalformedBlocksSafe()
        {
            BondSave d = new BondSave();
            d.Companions = null;
            BondRoster.Normalize(d);
            Assert.That(d.Companions, Is.Not.Null.And.Empty);
            Assert.That(d.ActiveUid, Is.EqualTo(0), "nothing is granted");

            BondSave m = new BondSave { NextUid = 1, ActiveUid = 77 };
            m.Companions.Add(null);
            m.Companions.Add(new CompanionRecord { Uid = 5, Archetype = BondIds.QuartzGuide, Name = "", Rank = 1, Trust = 10, Health01 = float.NaN, State = (int)CompanionState.Fused, Specialization = BondRules.SpecPrism, Aspects = null, Milestones = null });
            m.Companions.Add(new CompanionRecord { Uid = 5, Archetype = BondIds.QuartzGuide, Name = "Better", Rank = 3, Trust = 500 });
            m.Companions.Add(new CompanionRecord { Uid = 0, Archetype = BondIds.AshWarden, Rank = 9, Trust = -4, Health01 = 7f, State = 42, Tuning = 12, Evolution = -1, CareCooldown = float.NaN });
            m.Companions.Add(new CompanionRecord { Uid = 9, Archetype = "future_beast", Name = "Kept", Rank = 2 });
            int fixes = BondRoster.Normalize(m);
            Assert.That(fixes, Is.GreaterThan(0));
            Assert.That(m.Companions.Count, Is.EqualTo(3), "null and the weaker duplicate are gone, the unknown kind is kept");
            CompanionRecord q = BondRoster.FindArchetype(m, BondIds.QuartzGuide);
            Assert.That(q.Name, Is.EqualTo("Better"), "the higher rank wins a duplicate");
            CompanionRecord w = BondRoster.FindArchetype(m, BondIds.AshWarden);
            Assert.That(w.Uid, Is.GreaterThan(0));
            Assert.That(w.Rank, Is.EqualTo(BondIds.RankAwakened));
            Assert.That(w.Trust, Is.GreaterThanOrEqualTo(BondRules.TrustFloor(w.Rank)));
            Assert.That(w.Health01, Is.InRange(0f, 1f));
            Assert.That(w.State, Is.EqualTo((int)CompanionState.Resting), "a companion that is not active rests at home");
            Assert.That(w.Tuning, Is.EqualTo(BondRules.MaxTuning));
            Assert.That(w.Evolution, Is.EqualTo(0));
            Assert.That(w.CareCooldown, Is.EqualTo(0f));
            Assert.That(w.Name, Is.EqualTo("Hob"));
            HashSet<int> uids = new HashSet<int>();
            foreach (CompanionRecord r in m.Companions) Assert.That(uids.Add(r.Uid), Is.True, "uids are unique");
            foreach (CompanionRecord r in m.Companions) Assert.That(m.NextUid, Is.GreaterThan(r.Uid));
            CompanionRecord active = BondRoster.Active(m);
            Assert.That(active, Is.Not.Null);
            Assert.That(BondIds.Has(active.Archetype), Is.True, "an unknown kind never becomes active");

            BondSave s = new BondSave();
            s.Companions.Add(new CompanionRecord { Uid = 1, Archetype = BondIds.StormSkimmer, Rank = 1, Specialization = BondRules.SpecUpdraft, State = (int)CompanionState.Downed, RecoverIn = -5f });
            BondRoster.Normalize(s);
            Assert.That(s.Companions[0].Specialization, Is.Empty, "a specialization needs Harmonized");
            Assert.That(s.Companions[0].RecoverIn, Is.GreaterThan(0f), "a downed companion keeps a recovery time");
            Assert.That(s.ActiveUid, Is.EqualTo(1));
        }

        // ================================================================== rescue

        [Test]
        public void RescueClampsFlareAndTheWarningWaitsForDistance()
        {
            RescueRules r = new RescueRules(3);
            Assert.That(r.Phase, Is.EqualTo(RescuePhase.Clamped));
            Assert.That(r.ClampsLeft, Is.EqualTo(3));
            Assert.That(r.OpenClamp(0), Is.True);
            Assert.That(r.Flaring, Is.True);
            Assert.That(r.CanWorkClamp, Is.False, "no clamp work while it warns");
            Assert.That(r.OpenClamp(1), Is.False);
            // Standing close: the warning never ends.
            for (int i = 0; i < 100; i++) r.Tick(0.1f, 2f, false);
            Assert.That(r.Flaring, Is.True);
            Assert.That(r.TooClose, Is.True);
            // Backing off: it settles.
            for (int i = 0; i < 40; i++) r.Tick(0.1f, RescueRules.KeepDistance + 1f, false);
            Assert.That(r.Flaring, Is.False);
            Assert.That(r.OpenClamp(0), Is.False, "an open clamp stays open");
            Assert.That(r.OpenClamp(1), Is.True);
            for (int i = 0; i < 40; i++) r.Tick(0.1f, 10f, false);
            Assert.That(r.OpenClamp(2), Is.True);
            Assert.That(r.Phase, Is.EqualTo(RescuePhase.Approach));
        }

        [Test]
        public void RescueApproachTuningAndNamingComplete()
        {
            RescueRules r = new RescueRules(0);
            Assert.That(r.Phase, Is.EqualTo(RescuePhase.Approach));
            // Rushing in makes it flare.
            r.Tick(0.1f, 3f, true);
            Assert.That(r.Flaring, Is.True);
            for (int i = 0; i < 50; i++) r.Tick(0.1f, 8f, false);
            Assert.That(r.Flaring, Is.False);
            for (int i = 0; i < 40; i++) r.Tick(0.1f, 8f, false);
            Assert.That(r.Calm, Is.EqualTo(1f));
            r.Tick(0.1f, 2f, false);
            Assert.That(r.Phase, Is.EqualTo(RescuePhase.Tuning));
            // A press while the ring is wide open misses and only restarts the beat.
            Assert.That(r.PressTune(), Is.False);
            Assert.That(r.Misses, Is.EqualTo(1));
            Assert.That(r.Beats, Is.EqualTo(0));
            int guard = 0;
            while (r.Phase == RescuePhase.Tuning && guard++ < 400)
            {
                r.Tick(0.05f, 2f, false);
                if (r.Ring <= RescueRules.BeatWindow) r.PressTune();
            }
            Assert.That(r.Phase, Is.EqualTo(RescuePhase.Naming));
            Assert.That(r.Beats, Is.EqualTo(RescueRules.BeatsNeeded));
            r.Named();
            Assert.That(r.Phase, Is.EqualTo(RescuePhase.Done));
            Assert.That(r.Progress, Is.EqualTo(1f));
        }

        [Test]
        public void AttackingTheCreatureOnlyResetsTheApproach()
        {
            RescueRules r = new RescueRules(1);
            r.OpenClamp(0);
            for (int i = 0; i < 40; i++) r.Tick(0.1f, 10f, false);
            for (int i = 0; i < 40; i++) r.Tick(0.1f, 6f, false);
            r.Tick(0.1f, 2f, false);
            Assert.That(r.Phase, Is.EqualTo(RescuePhase.Tuning));
            r.Attacked();
            Assert.That(r.Phase, Is.EqualTo(RescuePhase.Approach));
            Assert.That(r.Calm, Is.EqualTo(0f));
            Assert.That(r.Beats, Is.EqualTo(0));
            Assert.That(r.ClampOpen[0], Is.True, "released clamps stay released");
            Assert.That(r.FlareLeft, Is.GreaterThanOrEqualTo(RescueRules.PanicSeconds - 0.01f));
            Assert.That(r.Resets, Is.EqualTo(1));
            // Still completable afterwards: no permanent failure.
            for (int i = 0; i < 80; i++) r.Tick(0.1f, 10f, false);
            for (int i = 0; i < 40; i++) r.Tick(0.1f, 6f, false);
            r.Tick(0.1f, 2f, false);
            Assert.That(r.Phase, Is.EqualTo(RescuePhase.Tuning));
            Assert.That(r.Objective("quartz drakeling"), Is.Not.Empty);
            AssertNoDashes(r.Objective("quartz drakeling"));
            r.Abort();
            Assert.That(r.Phase, Is.EqualTo(RescuePhase.Aborted));
        }

        // ================================================================== trail and follow

        [Test]
        public void TrailRecordsSpacedPointsBreaksOnTeleportsAndKeepsIdsAcrossTheRing()
        {
            CompanionTrail t = new CompanionTrail();
            Assert.That(t.Record(Vector3.zero, false, false), Is.True);
            Assert.That(t.Record(new Vector3(0.2f, 0f, 0f), false, false), Is.False, "too close");
            Assert.That(t.Record(new Vector3(1f, 0f, 0f), false, true), Is.True);
            Assert.That(t.Count, Is.EqualTo(2));
            int seg = t.Segment;
            Assert.That(t.Record(new Vector3(500f, 0f, 0f), false, false), Is.True);
            Assert.That(t.Segment, Is.Not.EqualTo(seg), "a teleport starts a new segment");
            Assert.That(t.Count, Is.EqualTo(1));
            t.Clear();
            for (int i = 0; i < CompanionTrail.Capacity + 40; i++) t.Record(new Vector3(i * 1f, 0f, 0f), false, i % 10 == 3);
            Assert.That(t.Count, Is.EqualTo(CompanionTrail.Capacity));
            TrailCrumb oldest = t[0];
            Assert.That(t.IndexOfId(oldest.Id), Is.EqualTo(0));
            Assert.That(t.IndexOfId(oldest.Id - 1), Is.EqualTo(-1), "overwritten crumbs are gone");
            Assert.That(t.IndexOfId(t.Newest.Id), Is.EqualTo(t.Count - 1));
            int near = t.NearestIndex(new Vector3(oldest.P.x + 5.1f, 0f, 0f), 2f);
            Assert.That(t[near].P.x, Is.EqualTo(oldest.P.x + 5f).Within(0.01f));
            Assert.That(t.NearestIndex(new Vector3(0f, 100f, 0f), 5f), Is.EqualTo(-1));
            int hazardIdx = -1;
            for (int i = 0; i < t.Count; i++) if (t[i].Hazard) { hazardIdx = i; break; }
            Assert.That(hazardIdx, Is.GreaterThanOrEqualTo(0));
            Assert.That(t[t.NextSafeIndex(hazardIdx, 5)].Hazard, Is.False);
        }

        [Test]
        public void RepathIsBoundedAndPicksTheFurthestClearCrumb()
        {
            CompanionTrail t = new CompanionTrail();
            for (int i = 0; i < 60; i++) t.Record(new Vector3(i, 0f, 0f), false, false);
            int calls = 0;
            int idx = t.FurthestClear(Vector3.zero, 0, 8, (a, b) => { calls++; return b.x <= 30f; });
            Assert.That(calls, Is.LessThanOrEqualTo(8), "at most the probe budget");
            Assert.That(t[idx].P.x, Is.LessThanOrEqualTo(30f));
            Assert.That(t[idx].P.x, Is.GreaterThan(20f), "it prefers the furthest clear point it probed");
            Assert.That(t.FurthestClear(Vector3.zero, 0, 8, (a, b) => false), Is.EqualTo(-1));
            Assert.That(t.FurthestClear(Vector3.zero, 0, 0, (a, b) => true), Is.EqualTo(-1));
        }

        [Test]
        public void FollowRulesKeepItBesideThePlayerAndUnderTheCeiling()
        {
            Vector3 right = CompanionFollowRules.Slot(Vector3.zero, Vector3.forward, 1, 2f);
            Vector3 left = CompanionFollowRules.Slot(Vector3.zero, Vector3.forward, -1, 2f);
            Assert.That(right.x, Is.GreaterThan(0f));
            Assert.That(left.x, Is.LessThan(0f));
            Assert.That(right.z, Is.LessThan(0f), "a little behind");
            CompanionProfile p = CompanionArchetypes.QuartzGuide;
            float prev = 0f;
            for (float d = 0f; d < 30f; d += 1f)
            {
                float s = CompanionFollowRules.Speed(d, p, false);
                Assert.That(s, Is.GreaterThanOrEqualTo(prev - 1e-4f));
                Assert.That(s, Is.LessThanOrEqualTo(p.SprintSpeed + 1e-4f));
                prev = s;
            }
            Assert.That(CompanionFollowRules.HoverY(0f, float.PositiveInfinity, 1.7f, 0.5f), Is.EqualTo(1.7f).Within(1e-4f));
            Assert.That(CompanionFollowRules.HoverY(0f, 2.2f, 1.7f, 0.5f), Is.LessThanOrEqualTo(2.2f - 0.5f - 0.35f + 1e-4f), "under a low ceiling");
            Assert.That(CompanionFollowRules.HoverY(0f, 0.5f, 1.7f, 0.5f), Is.EqualTo(0.3f).Within(1e-4f), "never into the floor");
            List<Vector3> offs = new List<Vector3>();
            CompanionFollowRules.RecallOffsets(Vector3.forward, 1, offs);
            Assert.That(offs.Count, Is.EqualTo(12));
            Assert.That(offs[0].z, Is.LessThan(0f), "behind first");
            foreach (Vector3 o in offs)
            {
                Assert.That(o.y, Is.EqualTo(0f));
                Assert.That(o.magnitude, Is.InRange(2f, 3.6f));
            }
            Assert.That(CompanionFollowRules.ShouldIdle(2f, 2.2f, true, true), Is.True);
            Assert.That(CompanionFollowRules.ShouldIdle(2f, 2.2f, false, true), Is.False, "not through a wall");
            Assert.That(CompanionFollowRules.ShouldIdle(2f, 2.2f, true, false), Is.False, "not across floors");
            Assert.That(CompanionFollowRules.CanHop(6f, 0f, CompanionLocomotion.Walker), Is.False);
            Assert.That(CompanionFollowRules.CanHop(6f, 0f, CompanionLocomotion.WalkerGlider), Is.True);
            Assert.That(CompanionFollowRules.CanHop(2f, 1f, CompanionLocomotion.Walker), Is.True);
            Assert.That(CompanionFollowRules.NeedsCatchUp(CompanionFollowRules.CatchUpDistance + 1f), Is.True);
        }

        [Test]
        public void StuckMeterEscalatesFromRepathToRecall()
        {
            StuckMeter m = new StuckMeter();
            Assert.That(m.Update(0f, 0f, 1f), Is.False, "not trying to move is not stuck");
            bool fired = false;
            for (int i = 0; i < 30 && !fired; i++) fired = m.Update(0f, 0.1f, 0.05f);
            Assert.That(fired, Is.True);
            Assert.That(m.Attempts, Is.EqualTo(1));
            Assert.That(m.ShouldRecall, Is.False);
            for (int k = 0; k < 2; k++)
            {
                fired = false;
                for (int i = 0; i < 30 && !fired; i++) fired = m.Update(0f, 0.1f, 0.05f);
            }
            Assert.That(m.ShouldRecall, Is.True);
            for (int i = 0; i < 40; i++) m.Update(0.1f, 0.1f, 0.05f);
            Assert.That(m.Attempts, Is.EqualTo(0), "progress clears the escalation");
        }

        // ================================================================== archetypes and bodies

        [Test]
        public void ThreeArchetypesAreDistinctInMovementAndAssists()
        {
            Assert.That(CompanionArchetypes.All.Length, Is.EqualTo(3));
            HashSet<CompanionLocomotion> loco = new HashSet<CompanionLocomotion>();
            HashSet<ExploreAssistKind> explore = new HashSet<ExploreAssistKind>();
            HashSet<CombatAssistKind> combat = new HashSet<CombatAssistKind>();
            for (int i = 0; i < BondIds.Archetypes.Length; i++)
            {
                string id = BondIds.Archetypes[i];
                CompanionProfile p = CompanionArchetypes.Get(id);
                Assert.That(p, Is.Not.Null, id);
                Assert.That(p, Is.SameAs(CompanionArchetypes.All[i]));
                Assert.That(p.Title, Is.EqualTo(BondIds.Title(id)));
                Assert.That(FusionIds.FormFor(id), Is.Not.Empty);
                loco.Add(p.Locomotion);
                explore.Add(p.Explore);
                combat.Add(p.Combat);
                foreach (string t in new[] { p.Role, p.ExploreTitle, p.ExploreText, p.CombatTitle, p.CombatText }) { Assert.That(t, Is.Not.Empty); AssertNoDashes(t); }
            }
            Assert.That(loco.Count, Is.EqualTo(3));
            Assert.That(explore.Count, Is.EqualTo(3));
            Assert.That(combat.Count, Is.EqualTo(3));
            Assert.That(CompanionArchetypes.Get("unknown"), Is.Null);
            Assert.That(CompanionArchetypes.CanFly(CompanionArchetypes.AshWarden), Is.False);
            Assert.That(CompanionArchetypes.CanFly(CompanionArchetypes.StormSkimmer), Is.True);
        }

        [Test]
        public void AssistAnchorListsNameRealUnderheartAnchors()
        {
            foreach (string[] list in new[] { BondSystem.RevealAnchors, BondSystem.MechanismAnchors, BondSystem.RelayAnchors })
                foreach (string id in list) Assert.That(UnderheartIds.HasAnchor(id), Is.True, id);
        }

        [TestCase("quartz_guide")]
        [TestCase("ash_warden")]
        [TestCase("storm_skimmer")]
        public void CompanionBodiesAddRealSkinnedGeometryWithoutTouchingTheRig(string archetype)
        {
            CreatureLook look = CompanionLooks.For(archetype, 4242, 0);
            CreatureBody plain = CreatureBuilder.Build(look, 1);
            CreatureBody body = CompanionBodyFactory.BuildNow(archetype, 4242, 0, "", 1);
            CollectionAssert.AreEqual(plain.Skeleton.Names, body.Skeleton.Names);
            CollectionAssert.AreEqual(plain.Skeleton.Parents, body.Skeleton.Parents);
            CollectionAssert.AreEqual(plain.Skeleton.LocalPositions, body.Skeleton.LocalPositions);
            Assert.That(body.HitVolumes.Count, Is.EqualTo(plain.HitVolumes.Count));
            int added = body.Mesh.Vertices.Count - plain.Mesh.Vertices.Count;
            Assert.That(added, Is.GreaterThan(150), "real companion geometry");
            Assert.That(added, Is.LessThan(9000), "within a sane budget");
            for (int i = 0; i < plain.Mesh.Vertices.Count; i++) Assert.That(body.Mesh.Vertices[i], Is.EqualTo(plain.Mesh.Vertices[i]));
            AssertValidSkin(body);
            Bounds pb = body.PoseBounds.size.sqrMagnitude > 1e-6f ? body.PoseBounds : body.Bounds;
            for (int i = 0; i < body.Mesh.Vertices.Count; i++) Assert.That(Inside(pb, body.Mesh.Vertices[i]), Is.True, "the renderer bounds hold vertex " + i);
        }

        [Test]
        public void CompanionBodiesAreDeterministicAndEvolutionsAddForm()
        {
            foreach (string a in BondIds.Archetypes)
            {
                CreatureBody x = CompanionBodyFactory.BuildNow(a, 99, 0, "", 1);
                CreatureBody y = CompanionBodyFactory.BuildNow(a, 99, 0, "", 1);
                CollectionAssert.AreEqual(x.Mesh.Vertices, y.Mesh.Vertices, a);
                CollectionAssert.AreEqual(x.Mesh.Triangles, y.Mesh.Triangles, a);
                string spec = BondRules.Specializations(a)[0];
                CreatureBody h = CompanionBodyFactory.BuildNow(a, 99, 1, spec, 1);
                CreatureBody w = CompanionBodyFactory.BuildNow(a, 99, 2, spec, 1);
                Assert.That(h.Mesh.Vertices.Count, Is.GreaterThan(x.Mesh.Vertices.Count), a + " Harmonized adds accents");
                Assert.That(w.Mesh.Vertices.Count, Is.GreaterThan(h.Mesh.Vertices.Count), a + " Awakened adds its final form");
                AssertValidSkin(w);
                string other = BondRules.Specializations(a)[1];
                CreatureBody o = CompanionBodyFactory.BuildNow(a, 99, 1, other, 1);
                Assert.That(o.Mesh.Vertices.Count, Is.Not.EqualTo(h.Mesh.Vertices.Count), a + " specializations look different");
            }
        }

        [Test]
        public void TheThreeSilhouettesDiffer()
        {
            CreatureBody drake = CompanionBodyFactory.BuildNow(BondIds.QuartzGuide, 7, 0, "", 0);
            CreatureBody warden = CompanionBodyFactory.BuildNow(BondIds.AshWarden, 7, 0, "", 0);
            CreatureBody skimmer = CompanionBodyFactory.BuildNow(BondIds.StormSkimmer, 7, 0, "", 0);
            Assert.That(drake.Look.Plan, Is.EqualTo(CreaturePlan.Winged));
            Assert.That(drake.Look.Forelegs, Is.True, "the drakeling walks on four legs");
            Assert.That(warden.Look.Plan, Is.EqualTo(CreaturePlan.Quadruped));
            Assert.That(skimmer.Look.Plan, Is.EqualTo(CreaturePlan.Winged));
            Assert.That(skimmer.Look.Forelegs, Is.False, "the skimmer has no forelegs");
            Assert.That(drake.Height, Is.LessThan(warden.Height), "the drakeling is the pocket-sized one");
            Assert.That(warden.Bounds.size.z, Is.GreaterThan(drake.Bounds.size.z * 0.9f));
            Assert.That(skimmer.Bounds.size.x, Is.GreaterThan(warden.Bounds.size.x), "the skimmer's wings span widest");
            Assert.That(drake.Look.CrystalRegalia, Is.True, "the drakeling keeps the quartz regalia");
            Assert.That(drake.Height, Is.InRange(0.2f, 0.9f));
        }

        private static void AssertValidSkin(CreatureBody b)
        {
            BodyData d = b.Mesh;
            Assert.That(d.Normals.Count, Is.EqualTo(d.Vertices.Count));
            Assert.That(d.Tangents.Count, Is.EqualTo(d.Vertices.Count));
            Assert.That(d.Palette.Count, Is.EqualTo(d.Vertices.Count));
            Assert.That(d.Detail.Count, Is.EqualTo(d.Vertices.Count));
            Assert.That(d.Weights.Count, Is.EqualTo(d.Vertices.Count));
            Assert.That(d.Triangles.Count % 3, Is.EqualTo(0));
            for (int i = 0; i < d.Vertices.Count; i++)
            {
                Vector3 v = d.Vertices[i];
                Assert.That(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z), Is.False, "vertex " + i);
                Assert.That(d.Normals[i].magnitude, Is.EqualTo(1f).Within(0.02f), "normal " + i);
                BoneWeight w = d.Weights[i];
                Assert.That(w.weight0 + w.weight1 + w.weight2 + w.weight3, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(w.boneIndex0, Is.InRange(0, b.Skeleton.Count - 1));
            }
            foreach (int index in d.Triangles) Assert.That(index, Is.InRange(0, d.Vertices.Count - 1));
        }

        /// <summary>Bounds.Contains is native Unity code; this is the same test in plain maths (with a millimetre of slack).</summary>
        private static bool Inside(Bounds b, Vector3 p)
        {
            Vector3 min = b.center - b.extents, max = b.center + b.extents;
            const float e = 0.001f;
            return p.x >= min.x - e && p.y >= min.y - e && p.z >= min.z - e && p.x <= max.x + e && p.y <= max.y + e && p.z <= max.z + e;
        }

        private static void AssertNoDashes(string s)
        {
            if (string.IsNullOrEmpty(s)) return;
            foreach (char c in s) Assert.That(c < '\u2010' || c > '\u2015', Is.True, "typographic dash in: " + s);
        }
    }
}
