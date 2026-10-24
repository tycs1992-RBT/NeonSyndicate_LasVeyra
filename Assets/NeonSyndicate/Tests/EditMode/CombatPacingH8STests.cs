using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// H8S pacing rules: the impact cadence (ImpactCadenceH8S) that keeps enemy blows on the player a moment apart, and
    /// the boss phase beat (BossPhaseRulesH8S): a safe roar, dropped unshown rings, arrival grace and the phase banner.
    /// </summary>
    public sealed class CombatPacingH8STests
    {
        [Test]
        public void GapsShrinkWithDifficultyButNeverVanish()
        {
            float story = ImpactCadenceH8S.MinGap(DifficultyLevel.Story);
            float balanced = ImpactCadenceH8S.MinGap(DifficultyLevel.Balanced);
            float veteran = ImpactCadenceH8S.MinGap(DifficultyLevel.Veteran);
            Assert.That(story, Is.GreaterThan(balanced));
            Assert.That(balanced, Is.GreaterThan(veteran));
            Assert.That(veteran, Is.GreaterThanOrEqualTo(0.2f), "even on Veteran two blows never land in the same instant");
            Assert.That(story, Is.LessThan(ImpactCadenceH8S.Memory), "a landed blow is remembered for longer than any gap");
        }

        [Test]
        public void TwoBlowsCannotLandInsideTheGap()
        {
            ImpactCadenceH8S c = new ImpactCadenceH8S();
            float gap = 0.35f;
            c.Reserve(1, 10f, 10.6f);
            Assert.That(c.Fits(2, 10.6f, gap), Is.False, "same instant");
            Assert.That(c.Fits(2, 10.9f, gap), Is.False, "0.3 s apart is inside 0.35");
            Assert.That(c.Fits(2, 10.3f, gap), Is.False, "before it, inside the gap");
            Assert.That(c.Fits(2, 10.96f, gap), Is.True, "just over 0.35 s after it");
            Assert.That(c.Fits(2, 10.24f, gap), Is.True, "just over 0.35 s before it");
            Assert.That(c.Fits(1, 10.61f, gap), Is.True, "an owner never blocks itself");
            Assert.That(c.FitsWith(2, 10.7f, gap, null, 0), Is.False, "the runtime asks Fits and the live swings together");
            Assert.That(c.Count, Is.EqualTo(1), "asking plans nothing");
        }

        [Test]
        public void ReplanningReleaseAndForgetting()
        {
            ImpactCadenceH8S c = new ImpactCadenceH8S();
            c.Reserve(7, 0f, 1f);
            c.Reserve(7, 0f, 2f);
            Assert.That(c.Count, Is.EqualTo(1), "an owner has one plan at a time");
            float at;
            Assert.That(c.PlanOf(7, out at), Is.True);
            Assert.That(at, Is.EqualTo(2f));
            c.Release(7);
            Assert.That(c.Count, Is.EqualTo(0), "a broken blow frees its moment at once");
            Assert.That(c.PlanOf(7, out at), Is.False);
            c.Reserve(8, 0f, 1f);
            c.Prune(1.5f);
            Assert.That(c.Count, Is.EqualTo(1), "a blow that just landed still guards the gap after it");
            Assert.That(c.Fits(9, 1.2f, 0.35f), Is.False);
            c.Prune(1f + ImpactCadenceH8S.Memory + 0.01f);
            Assert.That(c.Count, Is.EqualTo(0), "long landed blows are forgotten");
            c.Reserve(1, 0f, 1f);
            c.Clear();
            Assert.That(c.Count, Is.EqualTo(0));
        }

        [Test]
        public void AWaitedBlowStartsOnceTheGapPasses()
        {
            foreach (DifficultyLevel d in new[] { DifficultyLevel.Story, DifficultyLevel.Balanced, DifficultyLevel.Veteran })
            {
                float gap = ImpactCadenceH8S.MinGap(d);
                ImpactCadenceH8S c = new ImpactCadenceH8S();
                c.Reserve(1, 0f, 0.52f);
                float[] live = { 0.93f };
                const float windup = 0.41f;
                float t = 0f;
                int asks = 0;
                // The attacker asks, waits RetrySeconds when its blow would clash, and asks again (CadenceWaitsH8S).
                while (!c.FitsWith(2, t + windup, gap, live, 1))
                {
                    t += ImpactCadenceH8S.RetrySeconds;
                    c.Prune(t);
                    asks++;
                    Assert.That(asks, Is.LessThan(40), d + ": the wait always ends");
                }
                float lands = t + windup;
                Assert.That(asks, Is.GreaterThan(0), d + ": it had to wait");
                Assert.That(Mathf.Abs(lands - 0.52f), Is.GreaterThanOrEqualTo(gap), d + ": a gap after the planned creature blow");
                Assert.That(Mathf.Abs(lands - 0.93f), Is.GreaterThanOrEqualTo(gap), d + ": a gap after the person's swing");
                Assert.That(c.FitsWith(2, lands - ImpactCadenceH8S.RetrySeconds, gap, live, 1), Is.False, d + ": it started at the first beat that fits, not later");
                Assert.That(lands, Is.LessThanOrEqualTo(0.93f + gap + ImpactCadenceH8S.RetrySeconds + 1e-4f), d + ": at most one beat after the gap passes");
            }
        }

        [Test]
        public void OnlySwingsThatCanStillLandCount()
        {
            Assert.That(Swing(), Is.True, "a fighter on its feet, in its fight, winding up on the player from 3 m");
            Assert.That(Swing(swingT: 0.5f), Is.False, "stale: its wind-up is over");
            Assert.That(Swing(hasSwing: false), Is.False);
            Assert.That(Swing(dead: true), Is.False);
            Assert.That(Swing(ragdolled: true), Is.False, "knocked down mid-swing");
            Assert.That(Swing(staggered: true), Is.False, "reeling");
            Assert.That(Swing(inVehicle: true), Is.False);
            Assert.That(Swing(fighting: false), Is.False, "out of its fight state");
            Assert.That(Swing(atPlayer: false), Is.False, "aimed at someone else");
            Assert.That(Swing(distance: ImpactCadenceH8S.SwingReach + 0.5f), Is.False, "too far from the player to matter");
            Assert.That(Swing(distance: ImpactCadenceH8S.SwingReach - 0.5f), Is.True);
            Assert.That(Swing(distance: float.NaN), Is.False);
        }

        private static bool Swing(bool hasSwing = true, float swingT = 0.2f, bool dead = false, bool atPlayer = true, bool fighting = true,
            bool ragdolled = false, bool staggered = false, bool inVehicle = false, float distance = 3f)
        {
            return ImpactCadenceH8S.SwingCounts(hasSwing, swingT, 0.5f, dead, atPlayer, fighting, ragdolled, staggered, inVehicle, distance);
        }

        [Test]
        public void ABrokenTimeOrAFullBookNeverBreaksTheRule()
        {
            ImpactCadenceH8S c = new ImpactCadenceH8S();
            Assert.That(c.Fits(1, float.NaN, 0.3f), Is.False);
            Assert.That(c.Fits(1, float.PositiveInfinity, 0.3f), Is.False);
            c.Reserve(1, 0f, float.NaN);
            Assert.That(c.Count, Is.EqualTo(0), "broken times are never planned");
            for (int i = 0; i < ImpactCadenceH8S.Capacity + 5; i++) c.Reserve(100 + i, 0f, 1f + i);
            Assert.That(c.Count, Is.EqualTo(ImpactCadenceH8S.Capacity), "the book never grows past its capacity");
            float at;
            Assert.That(c.PlanOf(100, out at), Is.False, "the earliest plan was forgotten for the newest");
            Assert.That(c.PlanOf(100 + ImpactCadenceH8S.Capacity + 4, out at), Is.True);
        }

        [Test]
        public void LiveSwingsCountToo()
        {
            float[] live = { 2f, 3f, 0f };
            Assert.That(ImpactCadenceH8S.FitsAmong(live, 2, 2.1f, 0.3f), Is.False);
            Assert.That(ImpactCadenceH8S.FitsAmong(live, 2, 2.5f, 0.3f), Is.True);
            Assert.That(ImpactCadenceH8S.FitsAmong(live, 2, 0.1f, 0.3f), Is.True, "entries past count are not read");
            Assert.That(ImpactCadenceH8S.FitsAmong(live, 99, 0.1f, 0.3f), Is.False, "count is clamped to the array");
            Assert.That(ImpactCadenceH8S.FitsAmong(null, 3, 1f, 0.3f), Is.True);
            Assert.That(ImpactCadenceH8S.FitsAmong(live, 2, float.NaN, 0.3f), Is.False);
        }

        [Test]
        public void ThePhaseRoarIsSafeAndShorterThanTheScriptedGap()
        {
            foreach (DifficultyLevel d in new[] { DifficultyLevel.Story, DifficultyLevel.Balanced, DifficultyLevel.Veteran })
            {
                float hold = BossPhaseRulesH8S.HoldSeconds(d);
                Assert.That(hold, Is.GreaterThanOrEqualTo(1.5f), d + ": long enough to watch the roar");
                Assert.That(hold, Is.LessThan(BossPhaseRulesH8S.ScriptedPhaseGap), d + ": the boss can swing again before its next scripted move");
                float grace = BossPhaseRulesH8S.ArrivalGrace(d);
                Assert.That(grace, Is.InRange(0.5f, 1.5f), d + ": arrivals wait a moment, not a long time");
            }
            Assert.That(BossPhaseRulesH8S.HoldSeconds(DifficultyLevel.Story), Is.GreaterThan(BossPhaseRulesH8S.HoldSeconds(DifficultyLevel.Veteran)));
        }

        private sealed class PendingRing
        {
            public string Id;
            public bool Shown;
            public bool Arrival;
        }

        [Test]
        public void APhaseDropsOnlyRingsThatNeverAppeared()
        {
            List<PendingRing> pending = new List<PendingRing>
            {
                new PendingRing { Id = "on the floor", Shown = true },
                new PendingRing { Id = "waiting 1" },
                new PendingRing { Id = "support arrival", Arrival = true },
                new PendingRing { Id = "waiting 2" },
                new PendingRing { Id = "arrival shown", Shown = true, Arrival = true },
            };
            List<string> released = new List<string>();
            int n = BossPhaseRulesH8S.DropUnshown(pending, r => r.Shown, r => r.Arrival, r => released.Add(r.Id));
            Assert.That(n, Is.EqualTo(2));
            Assert.That(released, Is.EquivalentTo(new[] { "waiting 1", "waiting 2" }), "each dropped ring lets go of its mark");
            Assert.That(pending.ConvertAll(r => r.Id), Is.EqualTo(new List<string> { "on the floor", "support arrival", "arrival shown" }),
                "a ring already on the floor still bursts and arrivals still come, in their order");
            Assert.That(BossPhaseRulesH8S.DropUnshown(pending, r => r.Shown, r => r.Arrival, null), Is.EqualTo(0), "a second phase finds nothing more to drop");
            Assert.That(BossPhaseRulesH8S.DropUnshown<PendingRing>(null, r => r.Shown, r => r.Arrival, null), Is.EqualTo(0));
        }

        [Test]
        public void TheBannerFadesInHoldsAndFadesOut()
        {
            float life = BossPhaseRulesH8S.BannerSeconds;
            Assert.That(BossPhaseRulesH8S.BannerAlpha(0f, life), Is.EqualTo(0f));
            Assert.That(BossPhaseRulesH8S.BannerAlpha(-1f, life), Is.EqualTo(0f));
            Assert.That(BossPhaseRulesH8S.BannerAlpha(float.NaN, life), Is.EqualTo(0f));
            Assert.That(BossPhaseRulesH8S.BannerAlpha(life * 0.5f, life), Is.EqualTo(1f));
            Assert.That(BossPhaseRulesH8S.BannerAlpha(life, life), Is.EqualTo(0f));
            Assert.That(BossPhaseRulesH8S.BannerAlpha(life - 0.1f, life), Is.InRange(0.01f, 0.5f), "fading out at the end");
            Assert.That(BossPhaseRulesH8S.PhaseTitle(1, 3), Is.EqualTo("PHASE 2 / 3"));
            Assert.That(BossPhaseRulesH8S.PhaseTitle(9, 3), Is.EqualTo("PHASE 3 / 3"));
            Assert.That(BossPhaseRulesH8S.PhaseTitle(-2, 0), Is.EqualTo("PHASE 1 / 1"));
        }
    }
}
