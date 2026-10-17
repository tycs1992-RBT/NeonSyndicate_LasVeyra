using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// 04E: surface world events while the player is in the Underheart (UndergroundEventRules04E). A running event is
    /// held below the street and resumes on the way back up, or ends quietly as expired (never a failure, never a reward)
    /// when holding is not safe. All engine-free.
    /// </summary>
    public sealed class UndergroundEvents04ETests
    {
        private static UndergroundEventAction Decide(bool under, bool held, bool live, float seconds, bool changed = false, bool done = false, bool territory = false)
        {
            return UndergroundEventRules04E.Decide(under, held, done, territory, live, seconds, changed);
        }

        [Test]
        public void OnTheSurfaceNothingChanges()
        {
            Assert.That(Decide(false, false, false, 0f), Is.EqualTo(UndergroundEventAction.Run));
            Assert.That(Decide(false, false, true, 0f), Is.EqualTo(UndergroundEventAction.Run));
            Assert.That(Decide(false, false, true, 0f, true), Is.EqualTo(UndergroundEventAction.Run), "nothing was held, so nothing to compare");
        }

        [Test]
        public void ARunningEventIsHeldWhenThePlayerGoesBelow()
        {
            Assert.That(Decide(true, false, false, 0f), Is.EqualTo(UndergroundEventAction.Hold), "dormant: held");
            Assert.That(Decide(true, false, true, 0f), Is.EqualTo(UndergroundEventAction.Hold), "live: held for the grace");
            Assert.That(Decide(true, true, false, 120f), Is.EqualTo(UndergroundEventAction.Hold));
            Assert.That(Decide(true, true, true, 5f), Is.EqualTo(UndergroundEventAction.Hold));
        }

        [Test]
        public void ADormantEventWaitsLongALiveOneOnlyForTheShortGrace()
        {
            float dormant = UndergroundEventRules04E.DormantHoldSeconds, live = UndergroundEventRules04E.LiveGraceSeconds;
            Assert.That(live, Is.EqualTo(12f), "the same allowance as walking out of range on the surface");
            Assert.That(dormant, Is.GreaterThan(live));
            Assert.That(Decide(true, true, false, dormant - 0.01f), Is.EqualTo(UndergroundEventAction.Hold));
            Assert.That(Decide(true, true, false, dormant), Is.EqualTo(UndergroundEventAction.Expire));
            Assert.That(Decide(true, true, true, live - 0.01f), Is.EqualTo(UndergroundEventAction.Hold));
            Assert.That(Decide(true, true, true, live), Is.EqualTo(UndergroundEventAction.Expire));
            Assert.That(Decide(true, true, true, float.NaN), Is.EqualTo(UndergroundEventAction.Expire), "a broken clock never holds forever");
        }

        [Test]
        public void BackOnTheSurfaceAnUnchangedEventResumes()
        {
            Assert.That(Decide(false, true, false, 200f), Is.EqualTo(UndergroundEventAction.Resume));
            Assert.That(Decide(false, true, true, 8f), Is.EqualTo(UndergroundEventAction.Resume));
            Assert.That(Decide(false, true, true, 30f), Is.EqualTo(UndergroundEventAction.Expire), "past the grace: expired, not resumed");
        }

        [Test]
        public void AnythingThatChangedWhileHeldEndsItQuietly()
        {
            // A creature or person of the event died or vanished during the hold: resuming could pay a reward for
            // a despawn or fail the player for blows they could not answer, so it expires instead.
            Assert.That(Decide(true, true, true, 2f, true), Is.EqualTo(UndergroundEventAction.Expire));
            Assert.That(Decide(true, true, false, 2f, true), Is.EqualTo(UndergroundEventAction.Expire));
            Assert.That(Decide(false, true, true, 2f, true), Is.EqualTo(UndergroundEventAction.Expire));
            Assert.That(Decide(false, true, false, 2f, true), Is.EqualTo(UndergroundEventAction.Expire));
        }

        [Test]
        public void TerritoryActivitiesEndAtOnceAndFinishedEventsAreLeftAlone()
        {
            Assert.That(Decide(true, false, true, 0f, false, false, true), Is.EqualTo(UndergroundEventAction.Expire));
            Assert.That(Decide(true, false, true, 0f, false, true, true), Is.EqualTo(UndergroundEventAction.Run), "already over: normal cleanup");
            Assert.That(Decide(true, true, true, 100f, true, true), Is.EqualTo(UndergroundEventAction.Run));
        }

        [Test]
        public void LiveMeansMetWokenEscortingOrWithCreaturesOut()
        {
            Assert.That(UndergroundEventRules04E.IsLive(false, false, false, 0), Is.False, "dormant");
            Assert.That(UndergroundEventRules04E.IsLive(true, false, false, 0), Is.True, "engaged");
            Assert.That(UndergroundEventRules04E.IsLive(false, true, false, 0), Is.True, "woken");
            Assert.That(UndergroundEventRules04E.IsLive(false, false, true, 0), Is.True, "escorting someone");
            Assert.That(UndergroundEventRules04E.IsLive(false, false, false, 2), Is.True, "creatures out");
            Assert.That(UndergroundEventRules04E.ActorsChanged(3, 3), Is.False);
            Assert.That(UndergroundEventRules04E.ActorsChanged(3, 2), Is.True);
            Assert.That(UndergroundEventRules04E.ActorsChanged(0, 0), Is.False);
        }

        [Test]
        public void ExpiryNeverReadsAsAFailureOrAReward()
        {
            Assert.That(UndergroundEventRules04E.ExpireLine("Glass Swarm", false, false), Is.Empty, "never met: silent");
            string engaged = UndergroundEventRules04E.ExpireLine("Glass Swarm", true, false);
            Assert.That(engaged, Does.Contain("Glass Swarm"));
            string territory = UndergroundEventRules04E.ExpireLine("Relay defense", true, true);
            Assert.That(territory, Does.Contain("terminal"));
            foreach (string line in new[] { engaged, territory, UndergroundEventRules04E.ResumeLine("Glass Swarm", true) })
            {
                string low = line.ToLowerInvariant();
                Assert.That(low, Does.Not.Contain("fail"));
                Assert.That(low, Does.Not.Contain("reward"));
                Assert.That(low, Does.Not.Contain("lumen"));
                foreach (char ch in line) Assert.That(ch < (char)0x2010 || ch > (char)0x2015, Is.True, "no dash characters in " + line);
            }
            Assert.That(UndergroundEventRules04E.ResumeLine("Glass Swarm", false), Is.Empty);
        }

        [Test]
        public void AFullStayBelowPlaysOutAsDescribed()
        {
            // Frame by frame at 10 frames a second: a dormant event goes below for a minute and comes back.
            bool held = false;
            float seconds = 0f;
            for (int i = 0; i < 600; i++)
            {
                UndergroundEventAction a = Decide(true, held, false, seconds);
                Assert.That(a, Is.EqualTo(UndergroundEventAction.Hold), "frame " + i);
                if (held) seconds += 0.1f;
                held = true;
            }
            Assert.That(Decide(false, held, false, seconds), Is.EqualTo(UndergroundEventAction.Resume));
            // A live event that stays below past its grace expires while still below.
            held = false;
            seconds = 0f;
            UndergroundEventAction last = UndergroundEventAction.Run;
            for (int i = 0; i < 200 && last != UndergroundEventAction.Expire; i++)
            {
                last = Decide(true, held, true, seconds);
                if (held) seconds += 0.1f;
                held = true;
            }
            Assert.That(last, Is.EqualTo(UndergroundEventAction.Expire));
            Assert.That(seconds, Is.EqualTo(UndergroundEventRules04E.LiveGraceSeconds).Within(0.15f));
        }
    }
}
