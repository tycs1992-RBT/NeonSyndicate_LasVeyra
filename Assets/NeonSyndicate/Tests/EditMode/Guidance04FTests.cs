using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>Tests the actual 04F quest data and rescue dependency route without spawning Unity objects.</summary>
    public sealed class Guidance04FTests
    {
        private static Dictionary<string, QuestDef> Quests()
        {
            List<string> errors = new List<string>();
            List<KeyValuePair<string, QuestFile>> files = QuestTestData.Load<QuestFile>("Quests", errors, new QuestJsonTrace());
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
            Dictionary<string, QuestDef> all = new Dictionary<string, QuestDef>();
            foreach (KeyValuePair<string, QuestFile> file in files)
            {
                QuestDefaults.Normalize(file.Value);
                foreach (QuestDef quest in file.Value.Quests) all.Add(quest.Id, quest);
            }
            return all;
        }

        [Test]
        public void FirstCompanionRouteFollowsRealPrerequisitesWithoutSkippingTheRescue()
        {
            Dictionary<string, QuestDef> all = Quests();
            HashSet<string> done = new HashSet<string>();
            Func<string, QuestDef> find = id => all.ContainsKey(id) ? all[id] : null;
            Assert.That(GuidanceRules04F.CompanionLead(find, done.Contains), Is.EqualTo("uh05_01_knock"));
            done.Add("uh05_01_knock");
            Assert.That(GuidanceRules04F.CompanionLead(find, done.Contains), Is.EqualTo("uh05_02_sparks"));
            done.Add("uh05_02_sparks");
            Assert.That(GuidanceRules04F.CompanionLead(find, done.Contains), Is.EqualTo(GuidanceRules04F.RescueQuest));
            done.Add(GuidanceRules04F.RescueQuest);
            Assert.That(GuidanceRules04F.CompanionLead(find, done.Contains), Is.Empty);
            Assert.That(all[GuidanceRules04F.RescueQuest].Giver, Is.EqualTo("Cast:caretaker"));
        }

        [Test]
        public void MissingOrCyclicRescueContentCannotInventACompanionLead()
        {
            Assert.That(GuidanceRules04F.CompanionLead(null, id => false), Is.Empty);
            Assert.That(GuidanceRules04F.CompanionLead(id => null, id => false), Is.Empty);
            QuestDef cycle = new QuestDef { Id = GuidanceRules04F.RescueQuest };
            cycle.Requires.Quests.Add(cycle.Id);
            Assert.That(GuidanceRules04F.CompanionLead(id => cycle, id => false), Is.Empty);
        }

        [Test]
        public void TheFourSessionChoicesAreIndependentAndDoNotNeedAnAvailableStoryLead()
        {
            NextOption04E story = new NextOption04E { Kind = 0, Button = "STORY" };
            NextOption04E contract = new NextOption04E { Kind = 1, Button = "CONTRACT" };
            NextOption04E recharge = new NextOption04E { Kind = 2, Button = "RECHARGE" };
            NextOption04E companion = new NextOption04E { Kind = 3, Button = "COMPANION" };
            List<NextOption04E> choices = GuidanceRules04F.SessionChoices(story, contract, recharge, companion);
            Assert.That(choices.Count, Is.EqualTo(4));
            for (int i = 0; i < choices.Count; i++) Assert.That(choices[i].Kind, Is.EqualTo(i));
            choices = GuidanceRules04F.SessionChoices(null, null, recharge, companion);
            Assert.That(choices.Count, Is.EqualTo(2));
            Assert.That(choices[0].Button, Is.EqualTo("RECHARGE"));
            Assert.That(choices[1].Button, Is.EqualTo("COMPANION"));
        }

        [Test]
        public void ThreeConnectedJobsUseRealIndoorWorkAndLeaveThroughExistingDoors()
        {
            Dictionary<string, QuestDef> all = Quests();
            string[] ids = { GuidanceRules04F.ReserveJob, GuidanceRules04F.PressureJob, GuidanceRules04F.TemperJob };
            string[] venues = { "dispatch", "pressure", "forge" };
            HashSet<string> distinctVenues = new HashSet<string>();
            for (int i = 0; i < ids.Length; i++)
            {
                QuestDef q = all[ids[i]];
                Assert.That(q.Kind, Is.EqualTo("Side"), q.Id);
                Assert.That(q.Campaign, Is.EqualTo("underheart"), q.Id);
                Assert.That(QuestBook.IsCampaign(q), Is.True, "existing finite campaign lifecycle applies");
                Assert.That(q.Requires.Quests, Is.EquivalentTo(new[] { i == 0 ? "uh05_01_knock" : ids[i - 1] }));
                QuestStep enter = q.Phases[0].Steps[0];
                Assert.That(enter.Type, Is.EqualTo("GoTo"));
                Assert.That(enter.OnFoot, Is.True);
                Assert.That(enter.Radius, Is.LessThanOrEqualTo(1.5f));
                Assert.That(enter.At, Is.EqualTo("Venue:" + venues[i] + ".activity"));
                distinctVenues.Add(venues[i]);
                Assert.That(q.Phases[1].Checkpoint, Is.True);
                int indoorActions = 0;
                bool steadyWork = false;
                foreach (QuestStep step in q.Phases[1].Steps)
                {
                    Assert.That(step.At.StartsWith("Venue:" + venues[i] + ".", StringComparison.Ordinal), Is.True);
                    Assert.That(CampaignRules04E.PlaceTokenError(step.At), Is.Null, step.At);
                    if (step.Type == "Interact") indoorActions++;
                    if (step.Type == "Assist")
                    {
                        steadyWork = true;
                        Assert.That(step.Seconds, Is.InRange(5f, 7f), "manual alternative works before bonding");
                    }
                }
                Assert.That(indoorActions, Is.GreaterThanOrEqualTo(2));
                Assert.That(steadyWork, Is.True);
                QuestPhase last = q.Phases[q.Phases.Count - 1];
                Assert.That(last.Steps[last.Steps.Count - 1].At, Is.EqualTo("Venue:" + venues[i] + ".exit"));
            }
            Assert.That(distinctVenues.Count, Is.EqualTo(3));
        }

        [Test]
        public void InteriorJobsPayOnlyOnFiniteQuestCompletionAndNeverGrantOrReplaceABond()
        {
            Dictionary<string, QuestDef> all = Quests();
            int count = 0;
            foreach (QuestDef quest in all.Values)
            {
                if (!GuidanceRules04F.IsInteriorContract(quest.Id)) continue;
                count++;
                Assert.That(quest.NoAmbient, Is.True);
                Assert.That(quest.Reward.Cash, Is.InRange(1, 200));
                Assert.That(quest.Reward.Materials.Count, Is.GreaterThan(0));
                Assert.That(quest.Claims, Is.Empty, "ordinary completion ledger owns payment, not a second entitlement");
                foreach (QuestPhase phase in quest.Phases)
                    foreach (QuestStep step in phase.Steps)
                    {
                        Assert.That(step.Type, Is.Not.EqualTo("Give"), "restarting a work step pays nothing");
                        Assert.That(step.Type, Is.Not.EqualTo("GiveGun"));
                        Assert.That(step.Type, Is.Not.EqualTo("Rescue"), "the existing rescue remains the only initial bond");
                        Assert.That(step.Type, Is.Not.EqualTo("BondRank"));
                        Assert.That(step.Type, Is.Not.EqualTo("Spawn"), "no indoor spawned bodies block a doorway");
                    }
            }
            Assert.That(count, Is.EqualTo(3));
            Assert.That(QuestIds04E.Main.Length, Is.EqualTo(16));
            Assert.That(QuestIds04E.Side.Length, Is.EqualTo(12));
        }

        [Test]
        public void TrackedOptionalWorkDoesNotDisappearBehindAnotherGiversMainOffer()
        {
            Dictionary<string, QuestDef> all = Quests();
            List<QuestDef> offered = new List<QuestDef> { all["uh05_02_sparks"], all[GuidanceRules04F.PressureJob] };
            QuestDef selected = GuidanceRules04F.TrackedOffer(offered, GuidanceRules04F.PressureJob, id => false, q => true);
            Assert.That(selected, Is.SameAs(all[GuidanceRules04F.PressureJob]));
            Assert.That(GuidanceRules04F.TrackedOffer(offered, GuidanceRules04F.PressureJob, id => false, q => false), Is.Null,
                "a tracked locked job cannot bypass its prerequisites");
            Assert.That(GuidanceRules04F.TrackedOffer(offered, GuidanceRules04F.PressureJob, id => true, q => true), Is.Null,
                "completed contracts are never offered again");
        }

        [Test]
        public void SessionAndStatusIdentifyTheCurrentSprint()
        {
            Assert.That(ModuleStatus.Label, Does.Contain("04F"));
            Assert.That(new GuidanceSave().CityChoicesOffered04F, Is.False, "old saves get one eligible encounter choice card, never a new companion");
        }
    }
}
