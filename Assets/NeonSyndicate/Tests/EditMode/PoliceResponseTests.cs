using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>Engine-free wanted and dispatch regression cases. These do not claim to exercise scene-side AI.</summary>
    public sealed class PoliceResponseTests
    {
        [Test]
        public void PeacefulFiveMinutesNeverCreatesWantedHeat()
        {
            WantedLogic wanted = new WantedLogic();
            for (int i = 0; i < 300; i++) wanted.Tick(1f, false, Vector3.zero, 1.35f);
            Assert.That(wanted.Stars, Is.Zero);
            Assert.That(wanted.Heat, Is.Zero);
            foreach (DifficultyLevel difficulty in new[] { DifficultyLevel.Story, DifficultyLevel.Balanced, DifficultyLevel.Veteran })
            {
                Assert.That(PoliceResponseRules.Cars(difficulty, 0), Is.Zero);
                Assert.That(PoliceResponseRules.AvailableOfficers(difficulty, 0, 0), Is.Zero);
                Assert.That(PoliceResponseRules.CanRecruit(difficulty, 0, 0, 0, 2), Is.False);
            }
        }

        [Test]
        public void CarCrewsUseTheSameBudgetAsFootResponders()
        {
            foreach (DifficultyLevel difficulty in new[] { DifficultyLevel.Story, DifficultyLevel.Balanced, DifficultyLevel.Veteran })
                for (int stars = 1; stars <= 5; stars++)
                {
                    int cap = PoliceResponseRules.Officers(difficulty, stars);
                    int active = 0, cars = 0;
                    // Repeated dispatch tries mimic the original pile-on failure over a sustained pursuit.
                    for (int tick = 0; tick < 120; tick++)
                    {
                        if (PoliceResponseRules.CanRecruit(difficulty, stars, cars, active, 2)) { active += 2; cars++; }
                        int available = PoliceResponseRules.AvailableOfficers(difficulty, stars, active);
                        active += available > 2 ? 2 : available;
                        Assert.That(active, Is.LessThanOrEqualTo(cap));
                    }
                    Assert.That(PoliceResponseRules.AvailableOfficers(difficulty, stars, active), Is.Zero);
                    Assert.That(PoliceResponseRules.CanRecruit(difficulty, stars, cars, active, 1), Is.False);
                }
        }

        [Test]
        public void PartialCrewAndAlreadyOverBudgetCannotSlipThrough()
        {
            Assert.That(PoliceResponseRules.CanRecruit(DifficultyLevel.Story, 1, 0, 1, 2), Is.False);
            Assert.That(PoliceResponseRules.CanRecruit(DifficultyLevel.Story, 1, 0, 1, 1), Is.True);
            Assert.That(PoliceResponseRules.CanRecruit(DifficultyLevel.Story, 1, 1, 0, 1), Is.False);
            Assert.That(PoliceResponseRules.CanRecruit(DifficultyLevel.Veteran, 5, 0, 0, 0), Is.False);
            Assert.That(PoliceResponseRules.AvailableOfficers(DifficultyLevel.Story, 1, 10), Is.Zero);
        }

        [Test]
        public void ResponseGrowsWithStarsAndDifficulty()
        {
            for (int stars = 1; stars <= 5; stars++)
            {
                Assert.That(PoliceResponseRules.Officers(DifficultyLevel.Story, stars), Is.LessThan(PoliceResponseRules.Officers(DifficultyLevel.Balanced, stars)));
                Assert.That(PoliceResponseRules.Officers(DifficultyLevel.Balanced, stars), Is.LessThan(PoliceResponseRules.Officers(DifficultyLevel.Veteran, stars)));
                Assert.That(PoliceResponseRules.Cars(DifficultyLevel.Story, stars), Is.LessThanOrEqualTo(PoliceResponseRules.Cars(DifficultyLevel.Balanced, stars)));
                Assert.That(PoliceResponseRules.Officers(DifficultyLevel.Story, stars), Is.GreaterThanOrEqualTo(PoliceResponseRules.Officers(DifficultyLevel.Story, stars - 1)));
            }
            Assert.That(PoliceResponseRules.DispatchSeconds(DifficultyLevel.Story), Is.GreaterThan(PoliceResponseRules.DispatchSeconds(DifficultyLevel.Veteran)));
            Assert.That(PoliceResponseRules.InitialDelay(DifficultyLevel.Story), Is.GreaterThan(0f));
        }

        [Test]
        public void StorySlowsMinorCrimeEscalationButKeepsSeriousConsequences()
        {
            WantedLogic story = new WantedLogic(), veteran = new WantedLogic();
            for (int i = 0; i < 12; i++)
            {
                story.Commit(CrimeType.Gunfire, Vector3.zero, PoliceResponseRules.HeatScale(DifficultyLevel.Story));
                veteran.Commit(CrimeType.Gunfire, Vector3.zero, PoliceResponseRules.HeatScale(DifficultyLevel.Veteran));
            }
            Assert.That(story.Stars, Is.EqualTo(1));
            Assert.That(veteran.Stars, Is.GreaterThan(story.Stars));
            story.Clear();
            story.Commit(CrimeType.KillCop, Vector3.zero, PoliceResponseRules.HeatScale(DifficultyLevel.Story));
            Assert.That(story.Stars, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void ExplicitMissionWantedAndFrozenPursuitRemainIntact()
        {
            WantedLogic wanted = new WantedLogic();
            wanted.Set(4, new Vector3(12f, 0f, 20f));
            wanted.Frozen = true;
            wanted.Tick(300f, false, new Vector3(500f, 0f, 500f), PoliceResponseRules.EscapeScale(DifficultyLevel.Story));
            Assert.That(wanted.Stars, Is.EqualTo(4));
            Assert.That(wanted.Unseen, Is.Zero);
        }

        [Test]
        public void StoryEscapeIsQuickerOnlyWhenUnseen()
        {
            WantedLogic story = new WantedLogic(), veteran = new WantedLogic();
            story.Set(1, Vector3.zero); veteran.Set(1, Vector3.zero);
            story.Tick(60f, true, Vector3.zero, PoliceResponseRules.EscapeScale(DifficultyLevel.Story));
            Assert.That(story.Stars, Is.EqualTo(1));
            Assert.That(story.Tick(11f, false, Vector3.zero, PoliceResponseRules.EscapeScale(DifficultyLevel.Story)), Is.True);
            Assert.That(veteran.Tick(11f, false, Vector3.zero, PoliceResponseRules.EscapeScale(DifficultyLevel.Veteran)), Is.False);
        }

        [Test]
        public void DelayedCallsCannotCommitThroughPauseOrMissionSuppression()
        {
            Assert.That(PoliceResponseRules.MayCompleteCall(true, false, false), Is.True);
            Assert.That(PoliceResponseRules.MayCompleteCall(false, false, false), Is.False);
            Assert.That(PoliceResponseRules.MayCompleteCall(true, true, false), Is.False);
            Assert.That(PoliceResponseRules.MayCompleteCall(true, false, true), Is.False);
        }
    }
}
