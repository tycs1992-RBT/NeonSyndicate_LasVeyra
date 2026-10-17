using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The shared colour grade (both render paths use it): every combination of night, the Inner World, Frostline cold
    /// and rift pulses, at every graphics preset, brightness and grain setting, gives finite values in sane ranges.
    /// </summary>
    public sealed class PostGradeTests
    {
        private float _brightness;
        private bool _grain;
        private bool _chromatic;

        [SetUp]
        public void SaveSettings()
        {
            _brightness = Settings.Brightness;
            _grain = Settings.FilmGrain;
            _chromatic = Settings.ChromaticAberration;
        }

        [TearDown]
        public void RestoreSettings()
        {
            Settings.Brightness = _brightness;
            Settings.FilmGrain = _grain;
            Settings.ChromaticAberration = _chromatic;
            PostGrade.ResetStatics();
        }

        [Test]
        public void EveryCombinationIsFiniteAndSane()
        {
            float[] nights = { 0f, 0.25f, 0.5f, 0.75f, 1f };
            float[] inner = { 0f, 0.5f, 1f };
            float[] frost = { 0f, 0.5f, 1f };
            float[] rift = { 0f, 0.6f, 1f };
            float[] brightness = { -1f, 0f, 1f };
            int checkedCount = 0;
            foreach (GraphicsPreset q in System.Enum.GetValues(typeof(GraphicsPreset)))
            foreach (float b in brightness)
            foreach (bool grain in new[] { false, true })
            foreach (bool chroma in new[] { false, true })
            foreach (float under in new[] { 0f, 1f })
            foreach (float n in nights)
            foreach (float iw in inner)
            foreach (float fr in frost)
            foreach (float rf in rift)
            {
                Settings.Brightness = b;
                Settings.FilmGrain = grain;
                Settings.ChromaticAberration = chroma;
                PostGrade.InnerWorld = iw;
                PostGrade.Frost = fr;
                PostGrade.Rift = rf;
                PostGrade.Underground = under;
                PostGrade.Values v = PostGrade.Compute(n, q);
                string where = "night " + n + ", inner " + iw + ", frost " + fr + ", rift " + rf + ", " + q + ", brightness " + b + ", grain " + grain + ", chromatic " + chroma + ", underground " + under;
                CheckSane(v, grain, chroma, where);
                checkedCount++;
            }
            Assert.That(checkedCount, Is.GreaterThan(1000));
        }

        [Test]
        public void OutOfRangeWeightsAreClamped()
        {
            Settings.Brightness = 0f;
            PostGrade.InnerWorld = 5f;
            PostGrade.Frost = -3f;
            PostGrade.Rift = 9f;
            PostGrade.Values wild = PostGrade.Compute(1f, GraphicsPreset.High);
            PostGrade.InnerWorld = 1f;
            PostGrade.Frost = 0f;
            PostGrade.Rift = 1f;
            PostGrade.Values tame = PostGrade.Compute(1f, GraphicsPreset.High);
            Assert.That(wild.Saturation, Is.EqualTo(tame.Saturation).Within(1e-5f), "saturation");
            Assert.That(wild.Contrast, Is.EqualTo(tame.Contrast).Within(1e-5f), "contrast");
            Assert.That(wild.Vignette, Is.EqualTo(tame.Vignette).Within(1e-5f), "vignette");
            Assert.That(wild.Chromatic, Is.EqualTo(tame.Chromatic).Within(1e-5f), "chromatic aberration");
            Assert.That(wild.BloomIntensity, Is.EqualTo(tame.BloomIntensity).Within(1e-5f), "bloom");
            CheckSane(wild, Settings.FilmGrain, Settings.ChromaticAberration, "out-of-range weights");
        }

        [Test]
        public void ChromaticAberrationFollowsItsSetting()
        {
            Settings.Brightness = 0f;
            PostGrade.InnerWorld = 1f;
            PostGrade.Rift = 1f;
            Settings.ChromaticAberration = false;
            Assert.That(PostGrade.Compute(1f, GraphicsPreset.Ultra).Chromatic, Is.EqualTo(0f), "off: no fringing even in the Veil beside a rift");
            Settings.ChromaticAberration = true;
            Assert.That(PostGrade.Compute(1f, GraphicsPreset.Ultra).Chromatic, Is.GreaterThan(0.01f), "on: the Veil and rift fringing");
            PostGrade.ResetStatics();
            float baseAmount = PostGrade.Compute(0f, GraphicsPreset.High).Chromatic;
            Assert.That(baseAmount, Is.GreaterThan(0f).And.LessThanOrEqualTo(0.004f), "on: barely perceptible in the Outer City");
            Assert.That(PostGrade.Compute(0f, GraphicsPreset.Low).Chromatic, Is.EqualTo(0f), "Low never pays for it");
        }

        [Test]
        public void SplitToneIsCoolInTheShadowsAndWarmInTheHighlights()
        {
            Settings.Brightness = 0f;
            PostGrade.ResetStatics();
            foreach (float night in new[] { 0f, 0.5f, 1f })
            {
                PostGrade.Values v = PostGrade.Compute(night, GraphicsPreset.High);
                Assert.That(v.ShadowTint.b, Is.GreaterThan(v.ShadowTint.r), "shadows lean blue at night " + night);
                Assert.That(v.HighlightTint.r, Is.GreaterThan(v.HighlightTint.b), "highlights lean warm at night " + night);
                Assert.That(v.Lift.r + v.Lift.g + v.Lift.b, Is.GreaterThan(0.02f), "blacks are lifted at night " + night);
                Assert.That(v.Contrast, Is.InRange(1.02f, 1.1f), "contrast stays controlled at night " + night);
                Assert.That(v.Saturation, Is.InRange(1.0f, 1.12f), "saturation stays gentle at night " + night);
                Assert.That(v.Vignette, Is.LessThanOrEqualTo(0.22f), "vignette stays light at night " + night);
            }
        }

        [Test]
        public void UndergroundWidensTheAdaptationHeadroomOnly()
        {
            Settings.Brightness = 0f;
            PostGrade.ResetStatics();
            PostGrade.Values above = PostGrade.Compute(1f, GraphicsPreset.High);
            PostGrade.Underground = 1f;
            PostGrade.Values below = PostGrade.Compute(1f, GraphicsPreset.High);
            Assert.That(below.AdaptMaxEv, Is.GreaterThan(above.AdaptMaxEv));
            Assert.That(below.AdaptMinEv, Is.EqualTo(above.AdaptMinEv));
            Assert.That(below.AdaptKey, Is.EqualTo(above.AdaptKey));
            Assert.That(below.Exposure, Is.EqualTo(above.Exposure));
            Assert.That(below.Contrast, Is.EqualTo(above.Contrast));
        }

        [Test]
        public void NightAdaptsFurtherThanDayAndStaysBounded()
        {
            Settings.Brightness = 0f;
            PostGrade.ResetStatics();
            PostGrade.Values day = PostGrade.Compute(0f, GraphicsPreset.High);
            PostGrade.Values night = PostGrade.Compute(1f, GraphicsPreset.High);
            Assert.That(night.AdaptMaxEv, Is.GreaterThan(day.AdaptMaxEv), "night may open up more than day");
            Assert.That(night.AdaptKey, Is.LessThan(day.AdaptKey), "night aims darker than day");
            // The brightest an all-black night cellar can be pushed: under three stops after the strength.
            Assert.That(night.AdaptMaxEv * night.AdaptStrength, Is.LessThan(3f));
            // The most noon can be pulled down: about one stop.
            Assert.That(day.AdaptMinEv * day.AdaptStrength, Is.GreaterThan(-1.2f));
        }

        [Test]
        public void TheToneCurveIsMonotonicAndKeepsMiddleGreyWhereTheGameWasGraded()
        {
            float prev = -1f;
            for (int i = 0; i <= 400; i++)
            {
                float x = i * 0.05f;
                float y = PostGrade.AcesFittedGrey(x * PostGrade.AcesPreExposure);
                Assert.That(y, Is.GreaterThanOrEqualTo(prev).And.InRange(0f, 1f), "scene " + x);
                prev = y;
            }
            Assert.That(PostGrade.AcesFittedGrey(0f), Is.EqualTo(0f).Within(1e-3f), "black stays black");
            // The older luminance fit put 0.18 at 0.267 display linear; the pre-exposure keeps that brightness.
            Assert.That(PostGrade.AcesFittedGrey(0.18f * PostGrade.AcesPreExposure), Is.EqualTo(0.267f).Within(0.02f), "middle grey");
            // No blown noon: a scene white of 1.0 at the day exposure stays below display white.
            Assert.That(PostGrade.AcesFittedGrey(0.98f * PostGrade.AcesPreExposure), Is.LessThan(0.9f), "scene white rolls off");
            Assert.That(PostGrade.AcesFittedGrey(16f * PostGrade.AcesPreExposure), Is.GreaterThan(0.97f), "very bright saturates toward white");
            // No crushed night interiors: a dark 0.02 at the night exposure with the largest adaptation stays readable.
            Settings.Brightness = 0f;
            PostGrade.ResetStatics();
            PostGrade.Underground = 1f;
            PostGrade.Values night = PostGrade.Compute(1f, GraphicsPreset.High);
            float lifted = 0.02f * night.Exposure * Mathf.Pow(2f, night.AdaptMaxEv * night.AdaptStrength) * PostGrade.AcesPreExposure;
            Assert.That(PostGrade.AcesFittedGrey(lifted), Is.GreaterThan(0.05f), "a dark cellar wall at full adaptation");
        }

        [Test]
        public void TheToneCurveKeepsGreysNeutralAndColoursInRange()
        {
            foreach (float v in new[] { 0.01f, 0.1f, 0.5f, 1f, 4f })
            {
                Vector3 g = PostGrade.AcesFitted(new Vector3(v, v, v));
                Assert.That(g.x, Is.EqualTo(g.y).Within(2e-3f), "grey stays grey at " + v);
                Assert.That(g.y, Is.EqualTo(g.z).Within(2e-3f), "grey stays grey at " + v);
                Assert.That(g.x, Is.EqualTo(PostGrade.AcesFittedGrey(v)).Within(2e-3f), "grey axis matches at " + v);
            }
            Vector3 neon = PostGrade.AcesFitted(new Vector3(8f, 0.2f, 6f));
            Assert.That(neon.x, Is.InRange(0f, 1f));
            Assert.That(neon.y, Is.InRange(0f, 1f));
            Assert.That(neon.z, Is.InRange(0f, 1f));
            // A very bright saturated colour desaturates toward white instead of clipping to a flat hue.
            Vector3 dim = PostGrade.AcesFitted(new Vector3(0.5f, 0.02f, 0.4f));
            float dimSat = (Mathf.Max(dim.x, dim.y, dim.z) - Mathf.Min(dim.x, dim.y, dim.z)) / Mathf.Max(dim.x, dim.y, dim.z);
            float neonSat = (Mathf.Max(neon.x, neon.y, neon.z) - Mathf.Min(neon.x, neon.y, neon.z)) / Mathf.Max(neon.x, neon.y, neon.z);
            Assert.That(neonSat, Is.LessThan(dimSat), "highlights desaturate");
        }

        [Test]
        public void ResetStaticsClearsTheMoodWeights()
        {
            PostGrade.InnerWorld = 1f;
            PostGrade.Frost = 1f;
            PostGrade.Rift = 1f;
            PostGrade.ResetStatics();
            Assert.That(PostGrade.InnerWorld, Is.EqualTo(0f));
            Assert.That(PostGrade.Frost, Is.EqualTo(0f));
            Assert.That(PostGrade.Rift, Is.EqualTo(0f));
        }

        private static void CheckSane(PostGrade.Values v, bool grain, bool chromatic, string where)
        {
            if (!TestData.Finite(v.BloomIntensity) || !TestData.Finite(v.Exposure) || !TestData.Finite(v.Saturation) || !TestData.Finite(v.Contrast)
                || !TestData.Finite(v.Vignette) || !TestData.Finite(v.Chromatic) || !TestData.Finite(v.Grain) || !TestData.Finite(v.BloomThreshold)
                || !TestData.Finite(v.AdaptKey) || !TestData.Finite(v.AdaptMinEv) || !TestData.Finite(v.AdaptMaxEv) || !TestData.Finite(v.AdaptStrength)
                || !TestData.Finite(v.BloomTint) || !TestData.Finite(v.Lift) || !TestData.Finite(v.Gain) || !TestData.Finite(v.ShadowTint) || !TestData.Finite(v.HighlightTint))
                Assert.Fail("NaN or infinity in the grade at " + where);
            Range(v.BloomThreshold, 0.5f, 1.5f, "bloom threshold", where);
            Range(v.AdaptKey, -5f, 0f, "adaptation key", where);
            Range(v.AdaptMinEv, -3f, 0f, "adaptation minimum", where);
            Range(v.AdaptMaxEv, 0f, 4f, "adaptation maximum", where);
            Range(v.AdaptStrength, 0f, 1f, "adaptation strength", where);
            if (!chromatic && v.Chromatic != 0f) Assert.Fail("chromatic aberration is on although the setting is off at " + where);
            Range(v.BloomIntensity, 0f, 0.5f, "bloom intensity", where);
            Range(v.Exposure, 0.35f, 2.5f, "exposure", where);
            Range(v.Saturation, 0.8f, 1.35f, "saturation", where);
            Range(v.Contrast, 0.9f, 1.25f, "contrast", where);
            Range(v.Vignette, 0f, 0.6f, "vignette", where);
            Range(v.Chromatic, 0f, 0.03f, "chromatic aberration", where);
            Range(v.Grain, 0f, 0.05f, "film grain", where);
            if (!grain && v.Grain != 0f) Assert.Fail("film grain is on although the setting is off at " + where);
            RangeRgb(v.BloomTint, 0.5f, 1.2f, "bloom tint", where);
            RangeRgb(v.Gain, 0.8f, 1.2f, "gain", where);
            RangeRgb(v.Lift, -0.05f, 0.05f, "lift", where);
            RangeRgb(v.ShadowTint, -0.05f, 0.05f, "shadow tint", where);
            RangeRgb(v.HighlightTint, -0.05f, 0.08f, "highlight tint", where);
        }

        private static void Range(float x, float min, float max, string what, string where)
        {
            if (x < min || x > max) Assert.Fail(what + " " + x + " outside " + min + ".." + max + " at " + where);
        }

        private static void RangeRgb(Color c, float min, float max, string what, string where)
        {
            Range(c.r, min, max, what + " red", where);
            Range(c.g, min, max, what + " green", where);
            Range(c.b, min, max, what + " blue", where);
        }
    }
}
