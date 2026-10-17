using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The engine-free numbers behind the Built-in post pipeline: the temporal jitter pattern and blend weights, the
    /// depth of field circle of confusion, the bloom chain and the occlusion presets.
    /// </summary>
    public sealed class PostMathTests
    {
        [Test]
        public void HaltonSequenceMatchesTheTextbookValues()
        {
            Assert.That(PostMath.Halton(1, 2), Is.EqualTo(0.5f).Within(1e-6f));
            Assert.That(PostMath.Halton(2, 2), Is.EqualTo(0.25f).Within(1e-6f));
            Assert.That(PostMath.Halton(3, 2), Is.EqualTo(0.75f).Within(1e-6f));
            Assert.That(PostMath.Halton(4, 2), Is.EqualTo(0.125f).Within(1e-6f));
            Assert.That(PostMath.Halton(1, 3), Is.EqualTo(1f / 3f).Within(1e-6f));
            Assert.That(PostMath.Halton(2, 3), Is.EqualTo(2f / 3f).Within(1e-6f));
            Assert.That(PostMath.Halton(3, 3), Is.EqualTo(1f / 9f).Within(1e-6f));
            Assert.That(PostMath.Halton(0, 2), Is.EqualTo(0f));
        }

        [Test]
        public void JitterStaysInsideThePixelRepeatsPerCycleAndCoversIt()
        {
            Vector2 sum = Vector2.zero;
            for (int f = 0; f < PostMath.TaaJitterCycle; f++)
            {
                Vector2 j = PostMath.TaaJitterPixels(f);
                Assert.That(j.x, Is.InRange(-0.5f, 0.5f), "x of frame " + f);
                Assert.That(j.y, Is.InRange(-0.5f, 0.5f), "y of frame " + f);
                Assert.That(PostMath.TaaJitterPixels(f + PostMath.TaaJitterCycle), Is.EqualTo(j), "the pattern repeats");
                Assert.That(PostMath.TaaJitterPixels(f - PostMath.TaaJitterCycle), Is.EqualTo(j), "negative frames wrap too");
                for (int g = 0; g < f; g++) Assert.That(Vector2.Distance(PostMath.TaaJitterPixels(g), j), Is.GreaterThan(0.05f), "frames " + g + " and " + f + " differ");
                sum += j;
            }
            Assert.That(sum.magnitude / PostMath.TaaJitterCycle, Is.LessThan(0.1f), "the pattern is centred");
        }

        [Test]
        public void ProjectionOffsetIsTwoClipUnitsPerScreen()
        {
            Vector2 o = PostMath.TaaProjectionOffset(new Vector2(0.5f, -0.25f), 1920, 1080);
            Assert.That(o.x, Is.EqualTo(1f / 1920f).Within(1e-7f));
            Assert.That(o.y, Is.EqualTo(-0.5f / 1080f).Within(1e-7f));
            Assert.That(PostMath.TaaProjectionOffset(Vector2.one, 0, 0).x, Is.EqualTo(2f), "a zero size cannot divide by zero");
        }

        [Test]
        public void HistoryFeedbackFallsWithMotionAndVanishesOffScreen()
        {
            Assert.That(PostMath.TaaFeedback(0f, true), Is.EqualTo(PostMath.TaaFeedbackStill));
            Assert.That(PostMath.TaaFeedback(100f, true), Is.EqualTo(PostMath.TaaFeedbackMoving));
            Assert.That(PostMath.TaaFeedback(0f, false), Is.EqualTo(0f));
            float prev = 1f;
            for (float v = 0f; v <= 60f; v += 2f)
            {
                float fb = PostMath.TaaFeedback(v, true);
                Assert.That(fb, Is.LessThanOrEqualTo(prev).And.InRange(0.5f, 0.95f), "velocity " + v);
                prev = fb;
            }
        }

        [Test]
        public void CircleOfConfusionIsSignedAndClamped()
        {
            Assert.That(PostMath.CircleOfConfusion(10f, 10f, 0.5f), Is.EqualTo(0f).Within(1e-6f), "sharp at the focus");
            Assert.That(PostMath.CircleOfConfusion(12.5f, 10f, 0.5f), Is.EqualTo(0.5f).Within(1e-5f), "half blurred behind");
            Assert.That(PostMath.CircleOfConfusion(15f, 10f, 0.5f), Is.EqualTo(1f).Within(1e-5f), "fully blurred at focus times one plus range");
            Assert.That(PostMath.CircleOfConfusion(5f, 10f, 0.5f), Is.EqualTo(-1f).Within(1e-5f), "fully blurred in front");
            Assert.That(PostMath.CircleOfConfusion(1000f, 10f, 0.5f), Is.EqualTo(1f), "far clamps");
            Assert.That(PostMath.CircleOfConfusion(0f, 10f, 0.5f), Is.EqualTo(-1f), "near clamps");
            Assert.That(PostMath.CircleOfConfusion(3f, 0f, 0f), Is.InRange(-1f, 1f), "a zero focus or range cannot divide by zero");
        }

        [Test]
        public void BloomChainHasAtLeastFiveLevelsAndGrowsWithThePreset()
        {
            int prev = 0;
            foreach (GraphicsPreset q in System.Enum.GetValues(typeof(GraphicsPreset)))
            {
                int n = PostMath.BloomLevels(q);
                Assert.That(n, Is.GreaterThanOrEqualTo(5).And.LessThanOrEqualTo(8), q.ToString());
                Assert.That(n, Is.GreaterThanOrEqualTo(prev), q + " has no fewer levels than the preset below");
                prev = n;
            }
            Assert.That(PostMath.BloomEnergy(1), Is.EqualTo(1f).Within(1e-6f));
            Assert.That(PostMath.BloomEnergy(5), Is.EqualTo(1f + 0.72f + 0.5184f + 0.373248f + 0.26873856f).Within(1e-5f));
            // More levels add little energy: the preset change is width, not brightness.
            Assert.That(PostMath.BloomEnergy(8) / PostMath.BloomEnergy(5), Is.LessThan(1.2f));
        }

        [Test]
        public void OcclusionPresetsAreBoundedAndUltraIsTheStrongest()
        {
            float rHigh, sHigh, rUltra, sUltra;
            int slHigh, stHigh, slUltra, stUltra;
            PostMath.AmbientOcclusionSettings(GraphicsPreset.High, out rHigh, out sHigh, out slHigh, out stHigh);
            PostMath.AmbientOcclusionSettings(GraphicsPreset.Ultra, out rUltra, out sUltra, out slUltra, out stUltra);
            Assert.That(rHigh, Is.InRange(0.3f, 1.5f));
            Assert.That(rUltra, Is.GreaterThanOrEqualTo(rHigh).And.LessThanOrEqualTo(1.5f));
            Assert.That(sUltra, Is.GreaterThan(sHigh));
            Assert.That(slUltra * stUltra, Is.GreaterThan(slHigh * stHigh), "Ultra takes more samples");
            Assert.That(slUltra * stUltra * 2, Is.LessThanOrEqualTo(64), "the sample budget stays modest");
        }

        [Test]
        public void DepthOfFieldRadiusScalesWithTheScreenAndIsSmallerBehindTheMenu()
        {
            Assert.That(PostMath.DofMaxRadius(false, 1080), Is.EqualTo(10f).Within(1e-5f));
            Assert.That(PostMath.DofMaxRadius(true, 1080), Is.EqualTo(7f).Within(1e-5f));
            Assert.That(PostMath.DofMaxRadius(false, 2160), Is.EqualTo(20f).Within(1e-5f));
            Assert.That(PostMath.DofMaxRadius(false, 100), Is.GreaterThan(2f), "tiny windows keep a usable radius");
        }

        [Test]
        public void MotionBlurScaleIsClampedAroundSixtyFramesPerSecond()
        {
            Assert.That(PostMath.MotionBlurScale(1f / 60f), Is.EqualTo(0.45f).Within(1e-5f));
            Assert.That(PostMath.MotionBlurScale(1f / 240f), Is.EqualTo(0.9f).Within(1e-5f), "fast frames cannot blur beyond twice");
            Assert.That(PostMath.MotionBlurScale(1f), Is.EqualTo(0.3f).Within(1e-5f), "a hitch cannot shrink the blur below two thirds");
        }
    }
}
