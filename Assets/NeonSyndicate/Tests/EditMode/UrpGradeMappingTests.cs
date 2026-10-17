using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The shared grade turned into URP volume parameters: every mood and preset stays inside URP's ranges, the bloom
    /// threshold and the eye adaptation numbers come from PostGrade rather than private copies, and the parameter
    /// inverses survive URP's own conversions (ported here from its ColorUtils).
    /// </summary>
    public sealed class UrpGradeMappingTests
    {
        private float _brightness;
        private bool _grain, _chromatic;

        [SetUp]
        public void SaveSettings()
        {
            _brightness = Settings.Brightness;
            _grain = Settings.FilmGrain;
            _chromatic = Settings.ChromaticAberration;
            Settings.Brightness = 0f;
            Settings.FilmGrain = true;
            Settings.ChromaticAberration = true;
            PostGrade.ResetStatics();
        }

        [TearDown]
        public void RestoreSettings()
        {
            Settings.Brightness = _brightness;
            Settings.FilmGrain = _grain;
            Settings.ChromaticAberration = _chromatic;
            PostGrade.ResetStatics();
        }

        private struct Mood { public string Name; public float Night, Inner, Frost, Rift; }

        private static readonly Mood[] Moods =
        {
            new Mood { Name = "day", Night = 0f },
            new Mood { Name = "dusk", Night = 0.5f },
            new Mood { Name = "night", Night = 1f },
            new Mood { Name = "frostline day", Night = 0f, Frost = 1f },
            new Mood { Name = "inner world", Night = 0.6f, Inner = 1f },
            new Mood { Name = "rift at night", Night = 1f, Rift = 1f },
        };

        private static PostGrade.Values Grade(Mood m, GraphicsPreset q)
        {
            PostGrade.InnerWorld = m.Inner;
            PostGrade.Frost = m.Frost;
            PostGrade.Rift = m.Rift;
            return PostGrade.Compute(m.Night, q);
        }

        [Test]
        public void EveryMoodAndPresetStaysInsideUrpRanges()
        {
            foreach (Mood m in Moods)
            foreach (GraphicsPreset q in System.Enum.GetValues(typeof(GraphicsPreset)))
            foreach (float adapt in new[] { -0.9f, 0f, 1.8f })
            {
                PostGrade.Values g = Grade(m, q);
                UrpGradeMapping.Result r = UrpGradeMapping.Compute(g, q, m.Night, adapt, 0.45f);
                string where = m.Name + " " + q + " adapt " + adapt;
                Assert.That(r.Contrast, Is.InRange(-100f, 100f), where + " contrast");
                Assert.That(r.Saturation, Is.InRange(-100f, 100f), where + " saturation");
                Assert.That(r.VignetteIntensity, Is.InRange(0f, 1f), where + " vignette");
                Assert.That(r.ExactVignette, Is.InRange(0f, 2f), where + " exact vignette");
                Assert.That(r.ChromaticIntensity, Is.InRange(0f, 1f), where + " chromatic");
                Assert.That(r.GrainIntensity, Is.InRange(0f, 1f), where + " grain");
                Assert.That(r.GrainResponse, Is.InRange(0f, 1f), where + " grain response");
                Assert.That(r.MotionBlurIntensity, Is.InRange(0f, 1f), where + " motion blur");
                Assert.That(r.MotionBlurClamp, Is.InRange(0f, 0.2f), where + " motion blur clamp");
                Assert.That(r.BloomScatter, Is.InRange(0f, 1f), where + " scatter");
                Assert.That(r.BloomMaxIterations, Is.InRange(5, 8), where + " iterations");
                Assert.That(r.BloomIntensity, Is.GreaterThanOrEqualTo(0f), where + " bloom");
                Assert.That(r.BloomThreshold, Is.GreaterThan(0f), where + " threshold");
                Assert.That(TestData.Finite(r.PostExposure) && TestData.Finite(r.Gain.x) && TestData.Finite(r.Lift.x) && TestData.Finite(r.Midtones.x), where + " finite");
                Assert.That(r.ShadowsEnd, Is.EqualTo(r.HighlightsStart), where + " bands meet");
            }
        }

        [Test]
        public void BloomThresholdAndLevelsFollowTheSharedGrade()
        {
            foreach (Mood m in Moods)
            foreach (GraphicsPreset q in System.Enum.GetValues(typeof(GraphicsPreset)))
            {
                PostGrade.Values g = Grade(m, q);
                UrpGradeMapping.Result r = UrpGradeMapping.Compute(g, q, m.Night, 0f, 0f);
                Assert.That(r.BloomThreshold, Is.EqualTo(UrpGradeMapping.LinearToGamma(g.BloomThreshold)).Within(1e-6f), m.Name + " " + q);
                Assert.That(r.BloomMaxIterations, Is.EqualTo(PostMath.BloomLevels(q)), m.Name + " " + q + " levels");
            }
            // More levels: more scatter and more intensity to carry the extra energy, fitted per level count.
            float prevS = 0f, prevI = 0f;
            for (int n = 4; n <= 8; n++)
            {
                float s, i;
                UrpGradeMapping.BloomWeights(n, out s, out i);
                Assert.That(s, Is.GreaterThan(prevS).And.LessThan(1f), n + " levels scatter");
                Assert.That(i, Is.GreaterThan(prevI), n + " levels intensity");
                prevS = s;
                prevI = i;
            }
        }

        [Test]
        public void ExposureCarriesTheAdaptationAndThePreExposure()
        {
            PostGrade.Values g = Grade(Moods[0], GraphicsPreset.High);
            UrpGradeMapping.Result still = UrpGradeMapping.Compute(g, GraphicsPreset.High, 0f, 0f, 0f);
            UrpGradeMapping.Result opened = UrpGradeMapping.Compute(g, GraphicsPreset.High, 0f, 1.5f, 0f);
            Assert.That(opened.PostExposure - still.PostExposure, Is.EqualTo(1.5f).Within(1e-5f), "adaptation adds EV one to one");
            // The offset matched offline between the fitted curve (with its pre-exposure) and URP's full transform.
            float expected = Mathf.Log(g.Exposure, 2f) + UrpGradeMapping.Tuning.Default.AcesExposureMatch;
            Assert.That(still.PostExposure, Is.EqualTo(expected).Within(1e-4f));
            Assert.That(UrpGradeMapping.Tuning.Default.AcesExposureMatch, Is.EqualTo(Mathf.Log(PostGrade.AcesPreExposure, 2f)).Within(0.1f), "the offset is the pre-exposure plus a small residual");
        }

        [Test]
        public void ChromaticAberrationAndMotionBlurMatchNeonPostsLengths()
        {
            PostGrade.Values g = Grade(Moods[0], GraphicsPreset.High);
            g.Chromatic = 0.012f;
            UrpGradeMapping.Result r = UrpGradeMapping.Compute(g, GraphicsPreset.High, 0f, 0f, 0.45f);
            // Red-to-blue spread at the corner: NeonPost 2 x amount x |d|^3, URP 16/3 x 0.05 x intensity x |d|^3.
            float spreadBuiltIn = 2f * 0.012f, spreadUrp = 16f / 3f * 0.05f * r.ChromaticIntensity;
            Assert.That(spreadUrp, Is.EqualTo(spreadBuiltIn).Within(1e-6f));
            // Blur length and cap for a camera moving 0.02 uv per frame.
            float uvVel = 0.02f;
            float lenBuiltIn = Mathf.Min(uvVel * 0.45f, 0.04f), lenUrp = r.MotionBlurIntensity * Mathf.Min(2f * uvVel, r.MotionBlurClamp);
            Assert.That(lenUrp, Is.EqualTo(lenBuiltIn).Within(1e-5f));
            Assert.That(r.MotionBlurIntensity * r.MotionBlurClamp, Is.EqualTo(0.04f).Within(1e-5f), "cap");
            UrpGradeMapping.Result off = UrpGradeMapping.Compute(g, GraphicsPreset.High, 0f, 0f, 0f);
            Assert.That(off.MotionBlurIntensity, Is.EqualTo(0f));
        }

        [Test]
        public void ParameterInversesSurviveUrpsConversions()
        {
            Color lift = new Color(0.012f, 0.002f, 0.02f), gain = new Color(1.06f, 0.9f, 1.04f);
            Vector3 l2, gm2, g2;
            PrepareLiftGammaGain(UrpGradeMapping.LiftParameter(lift), new Vector4(1f, 1f, 1f, 0f), UrpGradeMapping.GainParameter(gain), out l2, out gm2, out g2);
            Assert.That(l2.x, Is.EqualTo(lift.r).Within(1e-4f));
            Assert.That(l2.y, Is.EqualTo(lift.g).Within(1e-4f));
            Assert.That(l2.z, Is.EqualTo(lift.b).Within(1e-4f));
            Assert.That(g2.x, Is.EqualTo(gain.r).Within(1e-4f));
            Assert.That(g2.y, Is.EqualTo(gain.g).Within(1e-4f));
            Assert.That(g2.z, Is.EqualTo(gain.b).Within(1e-4f));
            Assert.That(gm2.x, Is.EqualTo(1f).Within(1e-4f));
            Vector3 band = PrepareBand(UrpGradeMapping.FactorParameter(new Color(0.9f, 1f, 1.3f)));
            Assert.That(band.x, Is.EqualTo(0.9f).Within(1e-4f));
            Assert.That(band.y, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(band.z, Is.EqualTo(1.3f).Within(1e-4f));
            Assert.That(UrpGradeMapping.GammaToLinear(UrpGradeMapping.LinearToGamma(1.05f)), Is.EqualTo(1.05f).Within(1e-4f));
            Assert.That(UrpGradeMapping.GammaToLinear(UrpGradeMapping.LinearToGamma(0.002f)), Is.EqualTo(0.002f).Within(1e-6f));
        }

        // ---- URP's ColorUtils, ported: what the shader receives for the Lift Gamma Gain and band parameters.

        private static Vector3 PrepareBand(Vector4 p)
        {
            float w = p.w * (p.w < 0f ? 1f : 4f);
            return new Vector3(Mathf.Max(UrpGradeMapping.GammaToLinear(p.x) + w, 0f), Mathf.Max(UrpGradeMapping.GammaToLinear(p.y) + w, 0f), Mathf.Max(UrpGradeMapping.GammaToLinear(p.z) + w, 0f));
        }

        private static float Lum(Vector3 c)
        {
            return c.x * 0.2126729f + c.y * 0.7151522f + c.z * 0.072175f;
        }

        private static void PrepareLiftGammaGain(Vector4 inLift, Vector4 inGamma, Vector4 inGain, out Vector3 lift, out Vector3 gamma, out Vector3 gain)
        {
            Vector3 l = new Vector3(UrpGradeMapping.GammaToLinear(inLift.x) * 0.15f, UrpGradeMapping.GammaToLinear(inLift.y) * 0.15f, UrpGradeMapping.GammaToLinear(inLift.z) * 0.15f);
            float lumLift = Lum(l);
            lift = new Vector3(l.x - lumLift + inLift.w, l.y - lumLift + inLift.w, l.z - lumLift + inLift.w);
            Vector3 gm = new Vector3(UrpGradeMapping.GammaToLinear(inGamma.x) * 0.8f, UrpGradeMapping.GammaToLinear(inGamma.y) * 0.8f, UrpGradeMapping.GammaToLinear(inGamma.z) * 0.8f);
            float lumGamma = Lum(gm), gw = inGamma.w + 1f;
            gamma = new Vector3(1f / Mathf.Max(gm.x - lumGamma + gw, 1e-3f), 1f / Mathf.Max(gm.y - lumGamma + gw, 1e-3f), 1f / Mathf.Max(gm.z - lumGamma + gw, 1e-3f));
            Vector3 gn = new Vector3(UrpGradeMapping.GammaToLinear(inGain.x) * 0.8f, UrpGradeMapping.GammaToLinear(inGain.y) * 0.8f, UrpGradeMapping.GammaToLinear(inGain.z) * 0.8f);
            float lumGain = Lum(gn), nw = inGain.w + 1f;
            gain = new Vector3(gn.x - lumGain + nw, gn.y - lumGain + nw, gn.z - lumGain + nw);
        }
    }
}
