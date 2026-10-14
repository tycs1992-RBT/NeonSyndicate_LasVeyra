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

        [SetUp]
        public void SaveSettings()
        {
            _brightness = Settings.Brightness;
            _grain = Settings.FilmGrain;
        }

        [TearDown]
        public void RestoreSettings()
        {
            Settings.Brightness = _brightness;
            Settings.FilmGrain = _grain;
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
            foreach (float n in nights)
            foreach (float iw in inner)
            foreach (float fr in frost)
            foreach (float rf in rift)
            {
                Settings.Brightness = b;
                Settings.FilmGrain = grain;
                PostGrade.InnerWorld = iw;
                PostGrade.Frost = fr;
                PostGrade.Rift = rf;
                PostGrade.Values v = PostGrade.Compute(n, q);
                string where = "night " + n + ", inner " + iw + ", frost " + fr + ", rift " + rf + ", " + q + ", brightness " + b + ", grain " + grain;
                CheckSane(v, grain, where);
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
            CheckSane(wild, Settings.FilmGrain, "out-of-range weights");
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

        private static void CheckSane(PostGrade.Values v, bool grain, string where)
        {
            if (!TestData.Finite(v.BloomIntensity) || !TestData.Finite(v.Exposure) || !TestData.Finite(v.Saturation) || !TestData.Finite(v.Contrast)
                || !TestData.Finite(v.Vignette) || !TestData.Finite(v.Chromatic) || !TestData.Finite(v.Grain)
                || !TestData.Finite(v.BloomTint) || !TestData.Finite(v.Lift) || !TestData.Finite(v.Gain) || !TestData.Finite(v.ShadowTint) || !TestData.Finite(v.HighlightTint))
                Assert.Fail("NaN or infinity in the grade at " + where);
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
