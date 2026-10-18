using System.Collections.Generic;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>Pure response, ownership and reward regressions. Physics, graphics and autonomous motion require Unity.</summary>
    public sealed class CityCombat04FTests
    {
        [TestCase(DifficultyLevel.Story)]
        [TestCase(DifficultyLevel.Balanced)]
        [TestCase(DifficultyLevel.Veteran)]
        public void LostOfficersNeverReplenishTheTotalIncidentBudget(DifficultyLevel difficulty)
        {
            for (int severity = 1; severity <= 5; severity++)
            {
                int budget = CityCombatRules04F.IncidentOfficers(difficulty, severity);
                int spent = 0;
                for (int attempt = 0; attempt < 100; attempt++)
                {
                    // The current squad may all be gone. That opens active capacity, never lifetime capacity.
                    int request = CityCombatRules04F.RemainingDispatch(budget, spent, 3);
                    spent += request;
                    Assert.That(spent, Is.LessThanOrEqualTo(budget));
                }
                Assert.That(spent, Is.EqualTo(budget));
                Assert.That(CityCombatRules04F.RemainingDispatch(budget, spent, 99), Is.Zero);
            }
        }

        [Test]
        public void LocalPursuitsEndButStoryPursuitsKeepTheirAuthority()
        {
            Assert.That(CityCombatRules04F.LocalCanDispatch(false, false, 20f, 2), Is.True);
            Assert.That(CityCombatRules04F.LocalCanDispatch(false, false, CityCombatRules04F.IncidentSeconds, 2), Is.False);
            Assert.That(CityCombatRules04F.LocalCanDispatch(false, true, 20f, 2), Is.False);
            Assert.That(CityCombatRules04F.LocalCanDispatch(false, false, 20f, 0), Is.False);
            Assert.That(CityCombatRules04F.LocalCanDispatch(true, true, 900f, 0), Is.True);
        }

        [Test]
        public void OrdinarySideMissionsNeverPromoteLocalPursuitToStory()
        {
            Assert.That(CityCombatRules04F.StoryOwnsPursuit(false, true, false, false), Is.False);
            Assert.That(CityCombatRules04F.StoryOwnsPursuit(false, true, true, false), Is.True);
            Assert.That(CityCombatRules04F.StoryOwnsPursuit(false, true, true, true), Is.False);
            Assert.That(CityCombatRules04F.StoryOwnsPursuit(false, false, true, false), Is.False);
            Assert.That(CityCombatRules04F.StoryOwnsPursuit(true, false, false, false), Is.True);
        }

        [Test]
        public void ObserversAndDifferentEventsAreNeverCombatTargets()
        {
            Assert.That(CityCombatRules04F.OpposedEventTarget(true, 0, 1, false, false), Is.True);
            Assert.That(CityCombatRules04F.OpposedEventTarget(true, 0, 0, false, false), Is.False);
            Assert.That(CityCombatRules04F.OpposedEventTarget(false, 0, 1, false, false), Is.False);
            Assert.That(CityCombatRules04F.OpposedEventTarget(false, 0, -1, true, false), Is.False);
            Assert.That(CityCombatRules04F.OpposedEventTarget(false, 0, -1, true, true), Is.True);
        }

        [Test]
        public void FactionDuelFocusDoesNotOscillateButRespectsDeathRangeAndEventOwnership()
        {
            Assert.That(CityCombatRules04F.RetainEventOpponent(true, true, 100f, 92f), Is.True);
            Assert.That(CityCombatRules04F.RetainEventOpponent(true, true, 180f, 100f), Is.True);
            Assert.That(CityCombatRules04F.RetainEventOpponent(true, true, 210f, 100f), Is.False);
            Assert.That(CityCombatRules04F.RetainEventOpponent(false, true, 1f, 10f), Is.False);
            Assert.That(CityCombatRules04F.RetainEventOpponent(true, false, 1f, 10f), Is.False);
            Assert.That(CityCombatRules04F.RetainEventOpponent(true, true, 961f, 961f), Is.False);
            Assert.That(CityCombatRules04F.RetainEventOpponent(true, true, float.NaN, 4f), Is.False);
            Assert.That(CityCombatRules04F.RetainEventOpponent(true, true, 4f, float.PositiveInfinity), Is.False);
        }

        [Test]
        public void StoodDownOrHeldEventActorsCannotRemainTargets()
        {
            Assert.IsTrue(CityCombatRules04F.EligibleEventRival(true, true, true, false, false, false));
            Assert.IsFalse(CityCombatRules04F.EligibleEventRival(true, true, true, false, true, false));
            Assert.IsFalse(CityCombatRules04F.EligibleEventRival(true, true, true, false, false, true));
            Assert.IsFalse(CityCombatRules04F.EligibleEventRival(true, true, true, true, false, false));
            Assert.IsFalse(CityCombatRules04F.EligibleEventRival(true, true, false, false, false, false));
            Assert.IsFalse(CityCombatRules04F.EligibleEventRival(true, false, true, false, false, false));
            Assert.IsFalse(CityCombatRules04F.EligibleEventRival(false, true, true, false, false, false));
        }

        [Test]
        public void OrdinaryNpcAttacksRequireExplicitNonProtectedTargets()
        {
            Assert.That(CityCombatRules04F.OrdinaryTarget(true, false, false, false), Is.True);
            Assert.That(CityCombatRules04F.OrdinaryTarget(false, false, false, false), Is.False);
            Assert.That(CityCombatRules04F.OrdinaryTarget(true, true, false, false), Is.False);
            Assert.That(CityCombatRules04F.OrdinaryTarget(true, false, true, false), Is.False);
            Assert.That(CityCombatRules04F.OrdinaryTarget(true, false, true, true), Is.True);
        }

        [Test]
        public void RescueNeverCompletesFromMediationWhenTheHandlerDiedOnThatFrame()
        {
            Assert.That(CityCombatRules04F.RescueReady(false, true, true, true), Is.False);
            Assert.That(CityCombatRules04F.RescueReady(true, false, true, true), Is.False);
            Assert.That(CityCombatRules04F.RescueReady(true, true, false, false), Is.False);
            Assert.That(CityCombatRules04F.RescueReady(true, true, true, false), Is.True);
            Assert.That(CityCombatRules04F.RescueReady(true, true, false, true), Is.True);
        }

        [Test]
        public void CleanupAndWatchingCannotEarnAReward()
        {
            Assert.That(CityCombatRules04F.Earned(false, true, false), Is.False);
            Assert.That(CityCombatRules04F.Earned(true, false, false), Is.False);
            Assert.That(CityCombatRules04F.Earned(true, true, true), Is.False);
            Assert.That(CityCombatRules04F.Earned(true, true, false), Is.True);
        }

        [Test]
        public void RewardReceiptSurvivesRestoreWithoutAnotherGrant()
        {
            CityRewardLedger04F ledger = new CityRewardLedger04F();
            Assert.That(ledger.TryClaim("ev_04f_warden_passage:1"), Is.True);
            Assert.That(ledger.TryClaim("ev_04f_warden_passage:1"), Is.False);
            List<string> saved = new List<string>(ledger.Tokens);
            CityRewardLedger04F restored = new CityRewardLedger04F();
            restored.Restore(saved);
            Assert.That(restored.Count, Is.EqualTo(1));
            Assert.That(restored.TryClaim("ev_04f_warden_passage:1"), Is.False);
            Assert.That(restored.TryClaim("ev_04f_warden_passage:2"), Is.True);
            restored.Restore(null);
            Assert.That(restored.Count, Is.Zero);
            Assert.That(restored.TryClaim(null), Is.False);
        }

        [Test]
        public void CityEventsHaveBoundedBodiesAndDistinctExistingFactions()
        {
            Assert.That(CityCombatRules04F.FightersPerSide(true), Is.EqualTo(2));
            Assert.That(CityCombatRules04F.FightersPerSide(false), Is.LessThanOrEqualTo(3));
            Assert.That(CityCombatRules04F.ProjectileCap(true), Is.LessThan(CityCombatRules04F.ProjectileCap(false)));
            foreach (string faction in new[] { "frost_wardens", "frequency_order", "hollow_cult", "forgewrights", "ember_court" })
                Assert.That(FactionDb.Get(faction), Is.Not.Null);
            foreach (string kind in new[] { "CrystalDispute", "DrakeRescue", "CrystalCache" })
            {
                Assert.That(CityCombatRules04F.IsCityKind(kind), Is.True);
                Assert.That(QuestCatalog.EventFields(kind).Contains("FactionA"), Is.True);
                Assert.That(QuestCatalog.EventFields(kind).Contains("FactionB"), Is.True);
                Assert.That(QuestCatalog.EventRequired(kind), Does.Contain("Seconds"));
            }
        }
    }
}
