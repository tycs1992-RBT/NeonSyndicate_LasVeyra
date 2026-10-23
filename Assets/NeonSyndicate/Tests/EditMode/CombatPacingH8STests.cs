using NUnit.Framework;

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
            Assert.That(c.TryReserve(1, 10f, 10.6f, gap), Is.True);
            Assert.That(c.Fits(2, 10.6f, gap), Is.False, "same instant");
            Assert.That(c.Fits(2, 10.9f, gap), Is.False, "0.3 s apart is inside 0.35");
            Assert.That(c.Fits(2, 10.3f, gap), Is.False, "before it, inside the gap");
            Assert.That(c.Fits(2, 10.96f, gap), Is.True, "just over 0.35 s after it");
            Assert.That(c.Fits(2, 10.24f, gap), Is.True, "just over 0.35 s before it");
            Assert.That(c.Fits(1, 10.61f, gap), Is.True, "an owner never blocks itself");
            Assert.That(c.TryReserve(2, 10f, 10.7f, gap), Is.False);
            Assert.That(c.Count, Is.EqualTo(1), "a refused blow plans nothing");
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
        public void NextFitFindsTheFirstFreeMoment()
        {
            ImpactCadenceH8S c = new ImpactCadenceH8S();
            c.Reserve(1, 0f, 1f);
            c.Reserve(2, 0f, 1.3f);
            float t = c.NextFit(3, 1f, 0.3f);
            Assert.That(c.Fits(3, t, 0.3f), Is.True);
            Assert.That(t, Is.EqualTo(1.6f).Within(1e-3f), "after both planned blows");
            Assert.That(c.NextFit(3, 2.5f, 0.3f), Is.EqualTo(2.5f), "an open moment stays as asked");
            Assert.That(c.NextFit(1, 1f, 0.3f), Is.EqualTo(1.6f).Within(1e-3f), "its own plan is ignored, the other still counts");
            Assert.That(c.Fits(1, c.NextFit(1, 1f, 0.3f), 0.3f), Is.True);
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
            Assert.That(BossPhaseRulesH8S.ScriptedPhaseGap, Is.EqualTo(3f), "the upgraded fights' original phase gap");
        }

        [Test]
        public void OnlyRingsThatNeverAppearedAreDropped()
        {
            Assert.That(BossPhaseRulesH8S.DropsAtPhase(false, false), Is.True, "scheduled, never shown: never telegraphed, so it goes");
            Assert.That(BossPhaseRulesH8S.DropsAtPhase(true, false), Is.False, "a ring on the floor was telegraphed: it still bursts");
            Assert.That(BossPhaseRulesH8S.DropsAtPhase(false, true), Is.False, "a support arrival is the phase's own event");
            Assert.That(BossPhaseRulesH8S.DropsAtPhase(true, true), Is.False);
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
