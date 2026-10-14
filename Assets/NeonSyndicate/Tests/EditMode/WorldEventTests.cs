using System.Collections.Generic;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>The world-event director's selection rules (EventSelector): place, Veil, time, weather, story, cooldowns and the seeded weighted pick.</summary>
    public sealed class WorldEventTests
    {
        private static EventTemplate T(string id, float weight = 1f)
        {
            return new EventTemplate { Id = id, Title = id, Kind = "Swarm", Anchor = "Sidewalk", Species = "glass_mite", Weight = weight };
        }

        private static EventSituation Night(District d)
        {
            return new EventSituation { District = d, Hours = 23f, Weather = Weather.Clear, Story = 5, ThirdEye = true, Now = 5000f };
        }

        [Test]
        public void HourWindowsWrapPastMidnight()
        {
            Assert.That(EventSelector.HourInWindow(23f, 20f, 5f), Is.True);
            Assert.That(EventSelector.HourInWindow(3f, 20f, 5f), Is.True);
            Assert.That(EventSelector.HourInWindow(12f, 20f, 5f), Is.False);
            Assert.That(EventSelector.HourInWindow(5f, 20f, 5f), Is.False, "the end hour is excluded");
            Assert.That(EventSelector.HourInWindow(10f, 9f, 17f), Is.True);
            Assert.That(EventSelector.HourInWindow(18f, 9f, 17f), Is.False);
            Assert.That(EventSelector.HourInWindow(13f, 0f, 0f), Is.True, "no window means any time");
            Assert.That(EventSelector.HourInWindow(25f, 0f, 2f), Is.True, "hours wrap around the day");
        }

        [Test]
        public void DistrictsDefaultToTheCity()
        {
            EventTemplate t = T("a");
            Assert.That(EventSelector.DistrictAllowed(t, District.SunkenRow), Is.True);
            Assert.That(EventSelector.DistrictAllowed(t, District.Frostline), Is.False, "the wild needs its own templates");
            t.Districts.Add("Wild");
            Assert.That(EventSelector.DistrictAllowed(t, District.QuartzBadlands), Is.True);
            Assert.That(EventSelector.DistrictAllowed(t, District.SunkenRow), Is.False);
            t.Districts.Clear();
            t.Districts.Add("GraveFrequency");
            Assert.That(EventSelector.DistrictAllowed(t, District.GraveFrequency), Is.True);
            Assert.That(EventSelector.DistrictAllowed(t, District.OldSilver), Is.False);
            t.Districts.Add("Any");
            Assert.That(EventSelector.DistrictAllowed(t, District.Frostline), Is.True);
        }

        [Test]
        public void TheVeilSeparatesOuterAndInnerEvents()
        {
            EventTemplate outer = T("o");
            EventTemplate inner = T("i");
            inner.Veil = "Inner";
            EventTemplate both = T("b");
            both.Veil = "Any";
            Assert.That(EventSelector.VeilAllowed(outer, false), Is.True);
            Assert.That(EventSelector.VeilAllowed(outer, true), Is.False);
            Assert.That(EventSelector.VeilAllowed(inner, true), Is.True);
            Assert.That(EventSelector.VeilAllowed(inner, false), Is.False);
            Assert.That(EventSelector.VeilAllowed(both, true) && EventSelector.VeilAllowed(both, false), Is.True);
        }

        [Test]
        public void WhyNotGivesTheFirstReason()
        {
            EventTemplate t = T("a");
            t.HourMin = 20f;
            t.HourMax = 5f;
            t.Weathers.Add("Rain");
            t.Story = 3;
            t.ThirdEye = true;
            t.Cooldown = 300f;
            EventSituation s = Night(District.SunkenRow);
            Assert.That(EventSelector.WhyNot(t, s, null, null), Is.EqualTo("wrong weather"));
            s.Weather = Weather.Rain;
            Assert.That(EventSelector.WhyNot(t, s, null, null), Is.Null);
            s.Hours = 12f;
            Assert.That(EventSelector.WhyNot(t, s, null, null), Is.EqualTo("wrong time of day"));
            s.Hours = 23f;
            s.Story = 1;
            Assert.That(EventSelector.WhyNot(t, s, null, null), Is.EqualTo("later in the story"));
            s.FreeRoam = true;
            Assert.That(EventSelector.WhyNot(t, s, null, null), Is.Null, "free roam has the whole story");
            s.ThirdEye = false;
            Assert.That(EventSelector.WhyNot(t, s, null, null), Is.EqualTo("needs the Third Eye"));
            s.ThirdEye = true;
            Assert.That(EventSelector.WhyNot(t, s, id => 4900f, null), Is.EqualTo("cooling down"));
            Assert.That(EventSelector.WhyNot(t, s, id => 4600f, null), Is.Null, "300 s later it may come back");
            Assert.That(EventSelector.WhyNot(t, s, null, new List<string> { "a" }), Is.EqualTo("already running"));
            s.Inner = true;
            Assert.That(EventSelector.WhyNot(t, s, null, null), Is.EqualTo("outside the Inner World only"));
        }

        [Test]
        public void DefaultsForWeightAndCooldown()
        {
            EventTemplate t = T("a", 0f);
            Assert.That(EventSelector.WeightOf(t), Is.EqualTo(1f));
            Assert.That(EventSelector.CooldownOf(t), Is.EqualTo(QuestDefaults.EventCooldown));
        }

        [Test]
        public void SeededPicksAreDeterministicAndFollowTheWeights()
        {
            List<EventTemplate> pool = new List<EventTemplate> { T("a", 1f), T("b", 2f), T("c", 3f), T("d", 0.5f) };
            GRand r1 = new GRand(77), r2 = new GRand(77);
            for (int i = 0; i < 500; i++) Assert.That(EventSelector.PickWeighted(pool, r1), Is.SameAs(EventSelector.PickWeighted(pool, r2)));
            Dictionary<string, int> n = new Dictionary<string, int> { { "a", 0 }, { "b", 0 }, { "c", 0 }, { "d", 0 } };
            GRand r = new GRand(2024);
            const int total = 65000;
            for (int i = 0; i < total; i++) n[EventSelector.PickWeighted(pool, r).Id]++;
            Assert.That(n["a"] / (float)total, Is.EqualTo(1f / 6.5f).Within(0.012f));
            Assert.That(n["b"] / (float)total, Is.EqualTo(2f / 6.5f).Within(0.012f));
            Assert.That(n["c"] / (float)total, Is.EqualTo(3f / 6.5f).Within(0.012f));
            Assert.That(n["d"] / (float)total, Is.EqualTo(0.5f / 6.5f).Within(0.012f));
        }

        [Test]
        public void PickOnlyChoosesEligibleTemplates()
        {
            EventTemplate day = T("day");
            day.HourMin = 8f;
            day.HourMax = 18f;
            EventTemplate night = T("night");
            night.HourMin = 20f;
            night.HourMax = 4f;
            EventTemplate frost = T("frost");
            frost.Districts.Add("Frostline");
            List<EventTemplate> pool = new List<EventTemplate> { day, night, frost };
            List<EventTemplate> scratch = new List<EventTemplate>();
            GRand r = new GRand(5);
            for (int i = 0; i < 100; i++) Assert.That(EventSelector.Pick(pool, Night(District.OldSilver), null, null, r, scratch), Is.SameAs(night));
            Assert.That(EventSelector.Pick(pool, Night(District.OldSilver), null, new List<string> { "night" }, r, scratch), Is.Null, "nothing left");
            EventSituation snow = Night(District.Frostline);
            snow.Hours = 12f;
            Assert.That(EventSelector.Pick(pool, snow, null, null, r, scratch), Is.SameAs(frost));
            Assert.That(EventSelector.PickWeighted(new List<EventTemplate>(), r), Is.Null);
        }
    }
}
