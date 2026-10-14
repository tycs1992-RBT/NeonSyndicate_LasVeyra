using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The review fixes to progression, the Veil and the QA views, as pure logic: gang kills that cost reputation (not
    /// scripted fights, not self-defense in a war), grudges that fade in a reasonable amount of play, Prismatic Infusion
    /// never lowering a Weapon Core, the Veil's real cost and drain, the paramedic bonus in the max-health formula, pause
    /// tabs that fit at every aspect ratio, and the golden views keeping their old order with the two zone looks appended.
    /// </summary>
    public sealed class ProgressionFixTests
    {
        [TearDown]
        public void Clean()
        {
            SkillMods.ResetStatics();
        }

        // ------------------------------------------------------------------ gang kills

        [Test]
        public void ScriptedAndSelfDefenseKillsCostNothing()
        {
            float hostile = FactionRules.HeatFloor(FactionRank.Hostile);
            float hunted = FactionRules.HeatFloor(FactionRank.Hunted);
            Assert.That(FactionRules.GangKillCounts(false, 0.3f, 0f, 0f), Is.True, "a calm gang: the kill costs");
            Assert.That(FactionRules.GangKillCounts(true, 0.3f, 0f, 0f), Is.False, "mission, rampage, quest and event enemies never cost");
            Assert.That(FactionRules.GangKillCounts(false, 0.8f, 0f, 2f), Is.True, "the first seconds of a fight the player started still cost");
            Assert.That(FactionRules.GangKillCounts(false, 0.8f, 0f, FactionRules.WarGraceSeconds + 0.5f), Is.False, "a war that has lasted: self-defense");
            Assert.That(FactionRules.GangKillCounts(false, hostile, hostile, 0f), Is.False, "a Hostile gang attacks on sight: self-defense");
            Assert.That(FactionRules.GangKillCounts(false, hunted, hunted, 0f), Is.False, "a Hunted gang attacks on sight: self-defense");
            Assert.That(FactionRules.GangKillCounts(false, 0.85f, 0.85f, 0f), Is.False, "the story's floor (Ember Court after the treaty)");
            Assert.That(FactionRules.GangKillCounts(false, FactionRules.AttackOnSightHeat, 0f, 99f), Is.True, "exactly on the line is not at war");
            Assert.That(hostile, Is.GreaterThan(FactionRules.AttackOnSightHeat), "Hostile keeps the heat past the attack line");
        }

        [Test]
        public void NormalPlayCannotSpiralIntoHunted()
        {
            List<KeyValuePair<string, float>> d = new List<KeyValuePair<string, float>>();
            // An Ember Court rampage (25 scripted kills) and two Glass Jury story missions: no reputation at all.
            RepTrack t = new RepTrack();
            for (int i = 0; i < 25 + 2 + 6; i++) Kill(t, i < 25 ? 0 : 6, true, 0.95f, 0f, 30f, d);
            Assert.That(t.Get("ember_court"), Is.EqualTo(0), "the rampage cost nothing");
            Assert.That(t.Get("glass_jury"), Is.EqualTo(0), "the story missions cost nothing");
            // A street fight the player starts with a neutral gang: a kill every 2 s for 40 s, heat past the line from the start.
            t.Clear();
            for (int i = 0; i < 20; i++) Kill(t, 0, false, 0.95f, 0f, i * 2f, d);
            int afterFight = t.Get("ember_court");
            Assert.That(afterFight, Is.LessThan(0), "starting the fight costs something");
            Assert.That(afterFight, Is.GreaterThan(-20), "one fight does not make the gang Hostile (" + afterFight + ")");
            // Once Hostile, every kill is self-defense: thirty more kills change nothing.
            t.Set("ember_court", -25, null);
            for (int i = 0; i < 30; i++) Kill(t, 0, false, FactionRules.HeatFloor(FactionRank.Hostile), FactionRules.HeatFloor(FactionRank.Hostile), 0f, d);
            Assert.That(t.Get("ember_court"), Is.EqualTo(-25), "no spiral at Hostile");
        }

        /// <summary>FactionSystem.OnNpcKilled in miniature: the rule decides, then the kill deltas go through the track.</summary>
        private static void Kill(RepTrack t, int gang, bool scripted, float heat, float floor, float atWar, List<KeyValuePair<string, float>> d)
        {
            if (!FactionRules.GangKillCounts(scripted, heat, floor, atWar)) return;
            d.Clear();
            FactionRules.GangKill(gang, false, false, d);
            foreach (KeyValuePair<string, float> kv in d) t.Apply(kv.Key, kv.Value, 0f, true, null);
        }

        // ------------------------------------------------------------------ forgiveness

        [Test]
        public void GrudgesFadeInAReasonableAmountOfPlay()
        {
            Assert.That(FactionRules.HourForgiveness(0f), Is.EqualTo(0f), "nothing to forgive");
            Assert.That(FactionRules.HourForgiveness(30f), Is.EqualTo(0f), "good standing does not fade");
            Assert.That(FactionRules.HourForgiveness(-0.2f), Is.EqualTo(0.2f).Within(1e-5f), "never past zero");
            Assert.That(FactionRules.HourForgiveness(-60f), Is.GreaterThan(FactionRules.HourForgiveness(-20f)), "deep grudges fade faster");
            // In-game hours (two real minutes each) until each grudge is Neutral again.
            Assert.That(HoursToNeutral(-21), Is.LessThanOrEqualTo(2), "just Hostile");
            Assert.That(HoursToNeutral(-25), Is.LessThanOrEqualTo(5), "a few minutes of play");
            Assert.That(HoursToNeutral(-59), Is.InRange(10, 24), "the bottom of Hostile: about 40 real minutes");
            Assert.That(HoursToNeutral(-100), Is.InRange(16, 36), "Hunted: about an hour");
            // The steady part alone is the documented minimum: a day of hourly fades does at least what Forgive says.
            foreach (int rep in new[] { -100, -60, -25, -10, -3 })
            {
                float r = rep;
                for (int h = 0; h < 24; h++) r += FactionRules.HourForgiveness(r);
                Assert.That(r, Is.GreaterThanOrEqualTo(FactionRules.Forgive(rep) - 0.001f), "a day from " + rep);
                Assert.That(r, Is.LessThanOrEqualTo(0f), "a day from " + rep + " stops at zero");
            }
        }

        private static int HoursToNeutral(int start)
        {
            RepTrack t = new RepTrack();
            t.Set("roof_oni", start, null);
            for (int h = 1; h <= 500; h++)
            {
                float exact = t.Exact("roof_oni");
                t.Apply("roof_oni", FactionRules.HourForgiveness(exact), 0f, false, null);
                if (FactionRules.RankOf(t.Get("roof_oni")) >= FactionRank.Neutral) return h;
            }
            return int.MaxValue;
        }

        // ------------------------------------------------------------------ the Forge

        [Test]
        public void PrismaticInfusionNeverLowersTheCore()
        {
            Assert.That(ForgeMath.PrismaticProc(0f), Is.EqualTo(0.1f).Within(1e-6f));
            Assert.That(ForgeMath.PrismaticProc(0.15f), Is.EqualTo(0.25f).Within(1e-6f));
            Assert.That(ForgeMath.PrismaticProc(0.25f), Is.EqualTo(0.3f).Within(1e-6f), "up to the cap");
            Assert.That(ForgeMath.PrismaticProc(0.3f), Is.EqualTo(0.3f).Within(1e-6f), "at the cap it stays");
            Assert.That(ForgeMath.PrismaticProc(0.38f), Is.EqualTo(0.38f).Within(1e-6f), "a high-rarity roll above the cap is kept");
            for (float v = 0f; v <= 0.6f; v += 0.01f)
            {
                float after = ForgeMath.PrismaticProc(v);
                Assert.That(after, Is.GreaterThanOrEqualTo(v), "never lower from " + v);
                Assert.That(after, Is.LessThanOrEqualTo(Mathf.Max(v, ForgeMath.PrismaticCap) + 1e-6f), "never past the cap from " + v);
            }
        }

        // ------------------------------------------------------------------ the Veil

        [Test]
        public void VeilCostAndDrainUseTheSkillNumbers()
        {
            SkillMods.ResetStatics();
            Assert.That(InnerWorld.ShiftCost, Is.EqualTo(InnerWorld.EnterCost), "no skills: the base cost");
            SkillMods.VeilShiftCost = 0.48f;
            Assert.That(InnerWorld.ShiftCost, Is.EqualTo(12f).Within(1e-4f), "Thin Veil and Veil Walker make it cheaper");
            Assert.That(InnerWorld.DrainRate(false, 1f, 0f), Is.EqualTo(InnerWorld.DrainPerSecond).Within(1e-5f));
            Assert.That(InnerWorld.DrainRate(false, 0.5f, 0f), Is.EqualTo(1f).Within(1e-5f), "Veil Walker halves the drain");
            Assert.That(InnerWorld.DrainRate(true, 0.5f, 0f), Is.EqualTo(0f), "a scripted scene pauses the Veil's drain");
            Assert.That(InnerWorld.DrainRate(false, 1f, 12f), Is.EqualTo(14f).Within(1e-5f), "the Third Eye's bullet time drains on top");
            Assert.That(InnerWorld.DrainRate(true, 1f, 9f), Is.EqualTo(9f).Within(1e-5f), "the Third Eye still drains in a paused Veil");
        }

        // ------------------------------------------------------------------ max health

        [Test]
        public void ParamedicBonusIsPartOfMaxHealth()
        {
            Assert.That(Progression.MaxHealthFor(0, 0f, 0f, false), Is.EqualTo(200f));
            Assert.That(Progression.MaxHealthFor(0, 0f, 0f, true), Is.EqualTo(250f), "the milestone's +50");
            Assert.That(Progression.MaxHealthFor(10, 30f, 15f, true), Is.EqualTo(200f + 140f + 30f + 15f + Progression.ParamedicHealthBonus), "every source adds up");
            Assert.That(Progression.ParamedicMilestone, Is.EqualTo("job_paramedic_10"), "the Activities id the saves hold");
        }

        // ------------------------------------------------------------------ pause tabs

        /// <summary>A stand-in bold UI font: "SAVE / LOAD", the widest tab label, is about 6.4 times the font size wide.</summary>
        private static float WidestLabel(int size)
        {
            return 6.39f * size;
        }

        [Test]
        public void PauseTabsFitAtEveryAspectRatio()
        {
            // Menu space is 1080 high, so the width is 1080 times the aspect ratio.
            float[] widths = { 1920f, 1728f, 1440f, 1350f, 2520f, 3840f };
            foreach (float w in widths)
            {
                TabLayout t = UiFit.Tabs(11, w, WidestLabel);
                Assert.That(t.Rows * t.PerRow, Is.GreaterThanOrEqualTo(11), w + ": every tab has a place");
                Assert.That(t.Rows, Is.InRange(1, 2), w + ": one or two rows");
                Assert.That(WidestLabel(t.Font), Is.LessThanOrEqualTo(t.Width - UiFit.TabPadding + 0.01f), w + ": the widest label fits its tab at " + t.Font);
                Assert.That(t.Font, Is.GreaterThanOrEqualTo(UiFit.TabMinReadableFont), w + ": readable");
                float right = 60f + t.PerRow * (t.Width + UiFit.TabGap);
                Assert.That(right, Is.LessThanOrEqualTo(w - 260f + 0.01f), w + ": clear of the RESUME button");
            }
            TabLayout wide = UiFit.Tabs(11, 1920f, WidestLabel);
            Assert.That(wide.Rows == 1 && wide.Font == 18, Is.True, "16:9 keeps its single row at 18 points");
            TabLayout few = UiFit.Tabs(8, 1920f, WidestLabel);
            Assert.That(few.Font, Is.EqualTo(22), "a few tabs keep 22 points");
            Assert.That(UiFit.Tabs(11, 1350f, WidestLabel).Rows, Is.EqualTo(2), "5:4 wraps onto two rows");
            Assert.That(UiFit.LargestFont(22, 12, 10f, WidestLabel), Is.EqualTo(12), "nothing fits: the smallest size");
            Assert.That(UiFit.LargestFont(22, 12, 1000f, WidestLabel), Is.EqualTo(22), "everything fits: the largest size");
        }

        // ------------------------------------------------------------------ golden views

        private static readonly string[] OldViews =
        {
            "city_aerial", "core_street", "strip_street", "strip_aerial", "glass_heights", "old_silver",
            "sunken_row_canal", "ash_railyard", "grave_frequency", "skyrail_orbit", "desert_motel", "crystal_manor"
        };

        [Test]
        public void GoldenViewsKeepTheirOrderAndAddTheZoneLooks()
        {
            CityLayout L = TestData.City;
            List<GoldenView> views = GoldenViews.Build(L);
            Assert.That(views.Count, Is.EqualTo(OldViews.Length + 2));
            for (int i = 0; i < OldViews.Length; i++)
            {
                Assert.That(views[i].Id, Is.EqualTo(OldViews[i]), "view " + i + " keeps its place so old captures still compare");
                Assert.That(views[i].Inner || views[i].FollowCamera, Is.False, views[i].Id + " is an ordinary view");
            }
            GoldenView frost = views[OldViews.Length];
            Assert.That(frost.FollowCamera && !frost.Inner, Is.True, "the Frostline view measures the cold at the camera");
            Assert.That(CityLayout.DistrictAt(frost.Position), Is.EqualTo(District.Frostline));
            Assert.That(CityLayout.FrostAmount(frost.Position.x, frost.Position.z), Is.GreaterThan(0.9f), "deep enough for the full snow look");
            Assert.That(CityLayout.IsOnFrozenLake(frost.Position.x, frost.Position.z), Is.False);
            Assert.That(L.InsideRidge(frost.Position, 10f), Is.False, "not inside a mountain");
            Vector3 dir = (frost.LookAt - frost.Position).normalized;
            for (float d = 0f; d <= 60f; d += 2f)
            {
                Vector3 p = frost.Position + dir * d;
                Assert.That(Nearest(L.PineSpots, p), Is.GreaterThan(6f), "no pine in front of the camera " + d + " m ahead");
                Assert.That(Nearest(L.SnowRockSpots, p), Is.GreaterThan(4f), "no boulder in front of the camera " + d + " m ahead");
                Assert.That(Nearest(L.IceCrystalSpots, p), Is.GreaterThan(4f), "no ice crystal in front of the camera " + d + " m ahead");
            }
            GoldenView veil = views[OldViews.Length + 1];
            Assert.That(veil.Inner && veil.FollowCamera, Is.True, "the Veil view is taken inside the Inner World, with the growths around the camera");
            Assert.That(CityLayout.FrostAmount(veil.Position.x, veil.Position.z), Is.EqualTo(0f), "the Veil view is in the city");
        }

        private static float Nearest(List<Vector3> spots, Vector3 p)
        {
            float best = float.MaxValue;
            foreach (Vector3 q in spots) best = Math.Min(best, MathUtil.FlatDistance(p, q));
            return best;
        }
    }
}
