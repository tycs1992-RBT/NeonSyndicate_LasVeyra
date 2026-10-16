using System.Collections.Generic;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>Chapter data, existing phase-graph logic and the finite activity authorization rules.</summary>
    public sealed class QuietSignal04CTests
    {
        private static List<QuestDef> Chapter()
        {
            List<string> errors = new List<string>();
            List<QuestDef> quests = new List<QuestDef>();
            foreach (KeyValuePair<string, QuestFile> file in QuestTestData.Load<QuestFile>("Quests", errors, new QuestJsonTrace()))
                foreach (QuestDef quest in file.Value.Quests) if (QuietSignal04C.IsChapterQuest(quest.Id)) quests.Add(quest);
            Assert.That(errors, Is.Empty);
            quests.Sort((a, b) => a.Order.CompareTo(b.Order));
            return quests;
        }

        [Test]
        public void FiveStagesUseExistingPrerequisitesAndPhysicalVenuePresenters()
        {
            List<QuestDef> quests = Chapter();
            Assert.That(quests.Count, Is.EqualTo(5));
            string[] venues = { "lofts", "lofts", "forge", "archive", "observatory" };
            string[] presenters = { "Nessa Vale", "Nessa Vale", "Workshop assistant", "Archive desk assistant", "Survey attendant" };
            for (int i = 0; i < quests.Count; i++)
            {
                Assert.That(quests[i].Id, Is.EqualTo(QuietSignal04C.QuestIds[i]));
                Assert.That(quests[i].VenueGiver, Is.EqualTo(venues[i]));
                Assert.That(quests[i].Presenter, Is.EqualTo(presenters[i]));
                Assert.That(quests[i].Requires.ThirdEye, Is.False);
                Assert.That(quests[i].Requires.Quests.Count, Is.EqualTo(i == 0 ? 0 : 1));
                if (i > 0) Assert.That(quests[i].Requires.Quests[0], Is.EqualTo(quests[i - 1].Id));
            }
        }

        [Test]
        public void ChapterPassesTheActualStrictQuestValidator()
        {
            List<string> errors = new List<string>();
            QuestJsonTrace trace = new QuestJsonTrace();
            List<KeyValuePair<string, QuestFile>> files = new List<KeyValuePair<string, QuestFile>>();
            foreach (KeyValuePair<string, QuestFile> file in QuestTestData.Load<QuestFile>("Quests", errors, trace))
                if (file.Key.Contains("quiet_signal_04c")) files.Add(file);
            QuestReport report = new QuestReport();
            QuestValidator.ValidateQuests(files, QuestTestData.Refs, report, trace);
            Assert.That(errors, Is.Empty);
            Assert.That(report.Quests, Is.EqualTo(5));
            Assert.That(report.Errors, Is.Empty, report.Details());
            Assert.That(report.Warnings, Is.Empty, report.Details());
        }

        [Test]
        public void BothInvestigationAndDepotRoutesConvergeAndEveryPhaseCanFinish()
        {
            List<QuestDef> quests = Chapter();
            foreach (QuestDef quest in quests)
            {
                bool[] reachable = QuestLogic.Reachable(quest);
                for (int i = 0; i < quest.Phases.Count; i++)
                {
                    Assert.That(reachable[i], Is.True, quest.Id + ": " + quest.Phases[i].Id);
                    Assert.That(QuestLogic.CanFinishFrom(quest, i), Is.True, quest.Id + ": " + quest.Phases[i].Id);
                }
            }
            Assert.That(QuestLogic.Successors(quests[2], 0).Count, Is.EqualTo(2));
            Assert.That(QuestLogic.Successors(quests[3], 0).Count, Is.EqualTo(2));
            Assert.That(QuestLogic.NextPhase(quests[3], 1, null), Is.EqualTo(3));
            Assert.That(QuestLogic.NextPhase(quests[3], 2, null), Is.EqualTo(3));
        }

        [Test]
        public void RetryableStepsDoNotGrantWalletRewardsOrRespawningMaterialCaches()
        {
            List<QuestDef> quests = Chapter();
            int materialAwards = 0;
            foreach (QuestDef quest in quests)
            {
                materialAwards += quest.Reward.Materials.Count;
                foreach (QuestPhase phase in quest.Phases)
                    foreach (QuestStep step in phase.Steps)
                    {
                        Assert.That(new[] { "Venue", "Message", "Choice", "Talk" }, Does.Contain(step.Type));
                        Assert.That(step.Look, Is.Empty, "Venue staff must not be duplicated by quest-spawned NPCs.");
                        Assert.That(step.Caches, Is.Zero);
                        Assert.That(step.Type, Is.Not.EqualTo("Give"));
                        Assert.That(step.Type, Is.Not.EqualTo("AddRep"));
                    }
            }
            Assert.That(materialAwards, Is.EqualTo(2));
            Assert.That(quests[0].Reward.Cash, Is.EqualTo(150));
            Assert.That(quests[0].Reward.Materials[0].Id, Is.EqualTo("shard_chunk"));
            Assert.That(quests[0].Reward.Materials[1].Id, Is.EqualTo("hollow_essence"));
        }

        [Test]
        public void EveryVenueActionHasAnAuthoredAnchorAndItsOwningQuest()
        {
            HashSet<string> used = new HashSet<string>();
            foreach (QuestDef quest in Chapter()) foreach (QuestPhase phase in quest.Phases) foreach (QuestStep step in phase.Steps)
            {
                if (step.Type != "Venue") continue;
                Assert.That(QuietSignal04C.ValidAction(step.VenueId, step.ActionId), Is.True);
                Assert.That(QuietSignal04C.AnchorFor(step.VenueId, step.ActionId), Is.Not.Empty);
                Assert.That(QuietSignal04C.ActionQuestId(step.VenueId, step.ActionId), Is.EqualTo(quest.Id));
                Assert.That(used.Add(QuietSignal04C.ActionKey(step.VenueId, step.ActionId)), Is.True);
            }
            Assert.That(used.Count, Is.EqualTo(16));
        }

        [TestCase("calibrate_typo", false)]
        [TestCase("calibrate", true)]
        public void UnknownActionIdsAreRejected(string action, bool valid)
        {
            Assert.That(QuietSignal04C.ValidAction("forge", action), Is.EqualTo(valid));
        }

        [TestCase(true, true, "clear_threat", true)]
        [TestCase(false, true, "clear_threat", false)]
        [TestCase(true, false, "clear_threat", false)]
        [TestCase(true, true, "retrieve_safely", false)]
        public void DepotCombatRequiresTheExplicitCurrentRouteAndSafeOccupiedRoom(bool inside, bool safe, string action, bool allowed)
        {
            Assert.That(QuietSignal04C.MayUseAction(QuietSignal04C.QuestIds[3], "depot", action, "depot", "clear_threat", inside, safe), Is.EqualTo(allowed));
        }

        [Test]
        public void AnotherQuestCannotAuthorizeChapterActions()
        {
            Assert.That(QuietSignal04C.MayUseAction("existing_other_quest", "depot", "clear_threat", "depot", "clear_threat", true, true), Is.False);
        }

        [TestCase(0, 1)]
        [TestCase(1, 2)]
        [TestCase(2, 2)]
        [TestCase(999, 2)]
        public void DepotBudgetNeverExceedsTwoAuthoredPositions(int difficulty, int count)
        {
            Assert.That(QuietSignal04C.DepotThreatCount(difficulty), Is.EqualTo(count));
        }

        [TestCase(2, 2, 2, false, true)]
        [TestCase(2, 2, 1, false, false)]
        [TestCase(2, 1, 1, false, false)]
        [TestCase(2, 2, 2, true, false)]
        [TestCase(0, 0, 0, false, false)]
        public void DespawnAndPartialSpawnNeverMasqueradeAsCombatCompletion(int expected, int spawned, int deaths, bool invalid, bool complete)
        {
            Assert.That(QuietSignal04C.DepotCompleted(expected, spawned, deaths, invalid), Is.EqualTo(complete));
        }

        [Test]
        public void ValidatorRejectsCrossQuestAndVeilVenueActions()
        {
            QuestDef quest = Chapter()[0];
            QuestStep step = quest.Phases[0].Steps[0];
            step.VenueId = "depot";
            step.ActionId = "clear_threat";
            step.Veil = "Inner";
            QuestFile file = new QuestFile();
            file.Quests.Add(quest);
            QuestReport report = new QuestReport();
            QuestValidator.ValidateQuests(new List<KeyValuePair<string, QuestFile>> { new KeyValuePair<string, QuestFile>("invalid_venue", file) }, QuestTestData.Refs, report, null);
            Assert.That(report.Details(), Does.Contain("different quest"));
            Assert.That(report.Details(), Does.Contain("separate from the Veil"));
        }
    }
}
