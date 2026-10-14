using System.Collections.Generic;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>Quest rules in plain C#: requirements, the phase graph (Next, END, choices), contract pay and picks.</summary>
    public sealed class QuestLogicTests
    {
        private static QuestDef Linear(params string[] phases)
        {
            QuestDef q = new QuestDef { Id = "q", Title = "Q" };
            foreach (string id in phases)
            {
                QuestPhase p = new QuestPhase { Id = id };
                p.Steps.Add(new QuestStep { Type = "Wait", Seconds = 1f });
                q.Phases.Add(p);
            }
            return q;
        }

        private static QuestStep Choice(params string[] targets)
        {
            QuestStep s = new QuestStep { Type = "Choice", Text = "Pick" };
            foreach (string t in targets) s.Options.Add(new ChoiceOption { Label = t, Phase = t });
            return s;
        }

        // ================================================================== requirements

        [Test]
        public void RequirementsAreCheckedInOrder()
        {
            QuestDef q = new QuestDef { Id = "b", Title = "B" };
            q.Requires.Story = 1;
            q.Requires.ThirdEye = true;
            q.Requires.Quests.Add("a");
            q.Requires.Visited = "WardenOutpost";
            q.Requires.Faction = "frost_wardens";
            q.Requires.Rep = 20;
            QuestProgress p = new QuestProgress();
            Assert.That(QuestLogic.MissingRequirement(q, p, id => "The A Quest"), Is.EqualTo("Continue the story first"));
            p.Story = 1;
            Assert.That(QuestLogic.MissingRequirement(q, p, null), Is.EqualTo("Needs the Third Eye"));
            p.ThirdEye = true;
            Assert.That(QuestLogic.MissingRequirement(q, p, id => "The A Quest"), Is.EqualTo("Finish The A Quest"));
            p.Completed.Add("a");
            Assert.That(QuestLogic.MissingRequirement(q, p, null), Is.EqualTo("Reach the Frost Wardens Outpost"));
            p.Visited.Add("WardenOutpost");
            Assert.That(QuestLogic.MissingRequirement(q, p, null), Does.Contain("20 reputation with the Frost Wardens"));
            p.Rep = id => id == "frost_wardens" ? 20 : 0;
            Assert.That(QuestLogic.MissingRequirement(q, p, null), Is.Null);
        }

        [Test]
        public void FreeRoamSkipsOnlyTheStoryGate()
        {
            QuestDef q = new QuestDef { Id = "a" };
            q.Requires.Story = 3;
            q.Requires.ThirdEye = true;
            QuestProgress p = new QuestProgress { FreeRoam = true };
            Assert.That(QuestLogic.MissingRequirement(q, p, null), Is.EqualTo("Needs the Third Eye"));
            p.ThirdEye = true;
            Assert.That(QuestLogic.MissingRequirement(q, p, null), Is.Null);
        }

        // ================================================================== phases

        [Test]
        public void PhasesRunInOrderAndEndAfterTheLast()
        {
            QuestDef q = Linear("a", "b", "c");
            Assert.That(QuestLogic.NextPhase(q, 0, null), Is.EqualTo(1));
            Assert.That(QuestLogic.NextPhase(q, 1, null), Is.EqualTo(2));
            Assert.That(QuestLogic.NextPhase(q, 2, null), Is.EqualTo(-1), "after the last phase the quest is complete");
            Assert.That(QuestLogic.CountSteps(q), Is.EqualTo(3));
        }

        [Test]
        public void NextAndEndRedirect()
        {
            QuestDef q = Linear("a", "b", "c");
            q.Phases[0].Next = "c";
            q.Phases[2].Next = "END";
            Assert.That(QuestLogic.NextPhase(q, 0, null), Is.EqualTo(2));
            Assert.That(QuestLogic.NextPhase(q, 2, null), Is.EqualTo(-1));
            Assert.That(QuestLogic.NextPhase(q, 0, "b"), Is.EqualTo(1), "a choice's jump wins over Next");
            Assert.That(QuestLogic.NextPhase(q, 0, "nowhere"), Is.EqualTo(-2), "unknown targets are reported, not followed");
            bool[] reach = QuestLogic.Reachable(q);
            Assert.That(reach, Is.EqualTo(new[] { true, false, true }), "b is skipped by a's Next");
        }

        [Test]
        public void ChoicesBranchAndBothBranchesCanFinish()
        {
            QuestDef q = Linear("start", "left", "right", "end");
            q.Phases[0].Steps.Add(Choice("left", "right"));
            q.Phases[1].Next = "end";
            Assert.That(QuestLogic.Successors(q, 0), Is.EquivalentTo(new[] { 1, 2 }));
            Assert.That(QuestLogic.Reachable(q), Is.EqualTo(new[] { true, true, true, true }));
            for (int i = 0; i < 4; i++) Assert.That(QuestLogic.CanFinishFrom(q, i), Is.True, "phase " + i);
        }

        [Test]
        public void LoopsWithoutAnExitAreDetected()
        {
            QuestDef q = Linear("a", "b");
            q.Phases[1].Next = "a";
            Assert.That(QuestLogic.CanFinishFrom(q, 0), Is.False);
            q.Phases[1].Steps.Add(Choice("a", "END"));
            Assert.That(QuestLogic.CanFinishFrom(q, 0), Is.True, "a choice can leave the loop");
        }

        // ================================================================== rewards

        [Test]
        public void ContractPayGrowsWithDangerDistanceAndLoyaltyWithinBounds()
        {
            ContractDef t = new ContractDef { Id = "c", Guild = "frost_wardens", Kind = "Hunt", Cash = 300, Lumen = 30, Rep = 2 };
            CreatureSpecies mite = CreatureSpeciesDb.Get("glass_mite");
            CreatureSpecies stalker = CreatureSpeciesDb.Get("frost_stalker");
            CreatureSpecies drake = CreatureSpeciesDb.Get("quartz_drake");
            QuestReward small = QuestLogic.ScaleContractReward(t, mite, 3, 100f, 0);
            QuestReward big = QuestLogic.ScaleContractReward(t, stalker, 3, 100f, 0);
            Assert.That(big.Cash, Is.GreaterThan(small.Cash), "more danger pays more");
            Assert.That(QuestLogic.ScaleContractReward(t, stalker, 3, 2000f, 0).Cash, Is.GreaterThan(big.Cash), "longer trips pay more");
            Assert.That(QuestLogic.ScaleContractReward(t, stalker, 3, 100f, 8).Cash, Is.GreaterThan(big.Cash), "loyal hunters earn more");
            QuestReward huge = QuestLogic.ScaleContractReward(t, drake, 20, 5000f, 99);
            Assert.That(huge.Cash, Is.LessThanOrEqualTo(12000));
            Assert.That(huge.Lumen, Is.LessThanOrEqualTo(2500));
            Assert.That(huge.Rep[0].Amount, Is.InRange(1, 15));
            QuestReward none = QuestLogic.ScaleContractReward(new ContractDef { Guild = "menders", Kind = "Courier" }, null, 1, 0f, 0);
            Assert.That(none.Cash, Is.GreaterThanOrEqualTo(50), "never pays nothing");
            Assert.That(none.Rep[0].Faction, Is.EqualTo("menders"));
            Assert.That(big.Materials.Count, Is.GreaterThan(0), "hunts add the quarry's first loot material");
            Assert.That(big.Materials[0].Id, Is.EqualTo(stalker.Loot[0].Material));
        }

        [Test]
        public void ScalingRewardsKeepsReputationAndSkillPointsNeedTheFullReward()
        {
            QuestReward r = new QuestReward { Cash = 1000, Lumen = 200, SkillPoints = 1 };
            r.Materials.Add(new MaterialAmount { Id = "rime_pelt", Count = 4 });
            r.Rep.Add(new RepAmount { Faction = "menders", Amount = 5 });
            QuestReward half = QuestLogic.Scaled(r, 0.5f);
            Assert.That(half.Cash, Is.EqualTo(500));
            Assert.That(half.Lumen, Is.EqualTo(100));
            Assert.That(half.Materials[0].Count, Is.EqualTo(2));
            Assert.That(half.Rep[0].Amount, Is.EqualTo(5), "reputation is not scaled");
            Assert.That(half.SkillPoints, Is.EqualTo(0));
            Assert.That(QuestLogic.Scaled(r, 1f).SkillPoints, Is.EqualTo(1));
            Assert.That(QuestLogic.Scaled(r, 99f).Cash, Is.EqualTo(4000), "scaling is capped at 4x");
        }

        [Test]
        public void RewardTextListsEverything()
        {
            QuestReward r = new QuestReward { Cash = 1200, Lumen = 180, SkillPoints = 2 };
            r.Materials.Add(new MaterialAmount { Id = "rime_feather", Count = 2 });
            r.Rep.Add(new RepAmount { Faction = "frost_wardens", Amount = 4 });
            r.Rep.Add(new RepAmount { Faction = "static_saints", Amount = -3 });
            string s = QuestLogic.RewardText(r);
            Assert.That(s, Does.Contain("180 Lumen"));
            Assert.That(s, Does.Contain("2 skill points"));
            Assert.That(s, Does.Contain("2 Rime Feather"));
            Assert.That(s, Does.Contain("+4 Frost Wardens"));
            Assert.That(s, Does.Contain("-3 Static Saints"));
        }

        [Test]
        public void TemplatePicksRespectReputationAndAreDeterministic()
        {
            List<ContractDef> all = new List<ContractDef>
            {
                new ContractDef { Id = "easy", Guild = "menders", Weight = 1f },
                new ContractDef { Id = "hard", Guild = "menders", Weight = 1f, MinRep = 30 },
                new ContractDef { Id = "other", Guild = "forgewrights", Weight = 1f }
            };
            for (int seed = 1; seed < 200; seed++)
            {
                ContractDef t = QuestLogic.PickTemplate(all, "menders", 0, seed);
                Assert.That(t.Id, Is.EqualTo("easy"), "low reputation never sees the hard job");
                Assert.That(QuestLogic.PickTemplate(all, "menders", 50, seed), Is.SameAs(QuestLogic.PickTemplate(all, "menders", 50, seed)));
            }
            int hard = 0;
            for (int seed = 1; seed <= 2000; seed++) if (QuestLogic.PickTemplate(all, "menders", 50, seed).Id == "hard") hard++;
            Assert.That(hard, Is.InRange(850, 1150), "equal weights split evenly");
            Assert.That(QuestLogic.PickTemplate(all, "frost_wardens", 0, 1), Is.Null, "no templates, no pick");
        }
    }
}
