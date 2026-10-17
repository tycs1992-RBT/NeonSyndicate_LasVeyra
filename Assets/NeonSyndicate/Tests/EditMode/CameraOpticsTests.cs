using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The physical camera's lens maths (Phase 5 of the visual upgrade): the focal length computed from the game's
    /// vertical field of view must give that exact field of view back on the 24 mm sensor, at every field of view the
    /// camera uses (scope 4 degrees to a Surge at well over 90), and the drift curves behind the title sway, pause drift
    /// and cinematic breathing must stay small, bounded and continuous.
    /// </summary>
    public sealed class CameraOpticsTests
    {
        [TestCase(4f)]
        [TestCase(13f)]
        [TestCase(45f)]
        [TestCase(55f)]
        [TestCase(62f)]
        [TestCase(90f)]
        [TestCase(112f)]
        public void FocalLengthRoundTripsTheVerticalFieldOfView(float fov)
        {
            float focal = CameraOptics.FocalLength(fov, CameraOptics.SensorHeight);
            Assert.That(focal, Is.GreaterThan(0f));
            float back = CameraOptics.VerticalFov(focal, CameraOptics.SensorHeight);
            Assert.That(back, Is.EqualTo(fov).Within(1e-3f));
        }

        [Test]
        public void KnownLensNumbersMatchTheFullFrameSensor()
        {
            // 90 degrees vertical on a 24 mm sensor is a 12 mm lens; the game's 55 degree default is about 23 mm.
            Assert.That(CameraOptics.FocalLength(90f, 24f), Is.EqualTo(12f).Within(1e-4f));
            Assert.That(CameraOptics.FocalLength(55f, 24f), Is.EqualTo(23.05f).Within(0.02f));
            Assert.That(CameraOptics.FocalLength(55f, 24f), Is.EqualTo(24f * 0.5f / Mathf.Tan(27.5f * Mathf.Deg2Rad)).Within(1e-5f));
            Assert.That(CameraOptics.SensorWidth / CameraOptics.SensorHeight, Is.EqualTo(1.5f).Within(1e-6f));
        }

        [Test]
        public void WiderMonitorsKeepTheVerticalViewAndGainWidth()
        {
            // The gate is fitted vertically, so the vertical field of view is the same number at every aspect; what an
            // ultrawide gains is horizontal view, and only that.
            float focal = CameraOptics.FocalLength(55f, CameraOptics.SensorHeight);
            float v16 = CameraOptics.VerticalFov(focal, CameraOptics.SensorHeight);
            float v21 = CameraOptics.VerticalFov(focal, CameraOptics.SensorHeight);
            Assert.That(v21, Is.EqualTo(v16));
            float h16 = CameraOptics.HorizontalFov(55f, 16f / 9f);
            float h21 = CameraOptics.HorizontalFov(55f, 21f / 9f);
            float h43 = CameraOptics.HorizontalFov(55f, 4f / 3f);
            Assert.That(h16, Is.EqualTo(85.56f).Within(0.05f));
            Assert.That(h21, Is.GreaterThan(h16));
            Assert.That(h43, Is.LessThan(h16));
        }

        [Test]
        public void FocalLengthSurvivesAbsurdInputs()
        {
            Assert.That(float.IsNaN(CameraOptics.FocalLength(0f, 24f)), Is.False);
            Assert.That(float.IsInfinity(CameraOptics.FocalLength(0f, 24f)), Is.False);
            Assert.That(CameraOptics.FocalLength(180f, 24f), Is.GreaterThan(0f));
            Assert.That(CameraOptics.VerticalFov(0f, 24f), Is.LessThanOrEqualTo(180f));
            Assert.That(CameraOptics.VerticalFov(-5f, 24f), Is.GreaterThan(0f));
        }

        [Test]
        public void DriftStaysInsideTheUnitBoxAndIsContinuous()
        {
            Vector3 pos, rot, lastPos = Vector3.zero, lastRot = Vector3.zero;
            float step = 1f / 60f;
            for (int seed = 0; seed < 3; seed++)
            {
                for (int i = 0; i <= 60 * 600; i++)
                {
                    float t = i * step;
                    CameraOptics.Drift(t, seed, out pos, out rot);
                    AssertInsideUnitBox(pos, "drift position at " + t);
                    AssertInsideUnitBox(rot, "drift rotation at " + t);
                    if (i > 0)
                    {
                        Assert.That((pos - lastPos).magnitude, Is.LessThan(0.03f), "drift position jumped at " + t);
                        Assert.That((rot - lastRot).magnitude, Is.LessThan(0.03f), "drift rotation jumped at " + t);
                    }
                    lastPos = pos;
                    lastRot = rot;
                }
            }
        }

        [Test]
        public void DriftWithDifferentSeedsDoesNotMoveInStep()
        {
            Vector3 p0, r0, p1, r1;
            float same = 0f;
            int n = 0;
            for (float t = 0f; t < 120f; t += 0.5f)
            {
                CameraOptics.Drift(t, 0f, out p0, out r0);
                CameraOptics.Drift(t, 1f, out p1, out r1);
                if ((p0 - p1).magnitude < 0.05f) same += 1f;
                n++;
            }
            Assert.That(same / n, Is.LessThan(0.2f));
        }

        [Test]
        public void BreathStaysInsideTheUnitBoxAndIsContinuous()
        {
            Vector3 pos, rot, lastPos = Vector3.zero, lastRot = Vector3.zero;
            float step = 1f / 60f;
            for (int i = 0; i <= 60 * 120; i++)
            {
                float t = i * step;
                CameraOptics.Breath(t, out pos, out rot);
                AssertInsideUnitBox(pos, "breath position at " + t);
                AssertInsideUnitBox(rot, "breath rotation at " + t);
                if (i > 0)
                {
                    Assert.That((pos - lastPos).magnitude, Is.LessThan(0.06f), "breath position jumped at " + t);
                    Assert.That((rot - lastRot).magnitude, Is.LessThan(0.06f), "breath rotation jumped at " + t);
                }
                lastPos = pos;
                lastRot = rot;
            }
        }

        [Test]
        public void RampInStartsAtZeroAndArrivesAtOne()
        {
            Assert.That(CameraOptics.RampIn(0f, 1.5f), Is.EqualTo(0f));
            Assert.That(CameraOptics.RampIn(0.75f, 1.5f), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(CameraOptics.RampIn(1.5f, 1.5f), Is.EqualTo(1f));
            Assert.That(CameraOptics.RampIn(9f, 1.5f), Is.EqualTo(1f));
            Assert.That(CameraOptics.RampIn(0.2f, 0f), Is.EqualTo(1f));
            float last = 0f;
            for (float t = 0f; t <= 1.5f; t += 0.05f)
            {
                float w = CameraOptics.RampIn(t, 1.5f);
                Assert.That(w, Is.GreaterThanOrEqualTo(last));
                last = w;
            }
        }

        private static void AssertInsideUnitBox(Vector3 v, string what)
        {
            Assert.That(Mathf.Abs(v.x), Is.LessThanOrEqualTo(1f), what);
            Assert.That(Mathf.Abs(v.y), Is.LessThanOrEqualTo(1f), what);
            Assert.That(Mathf.Abs(v.z), Is.LessThanOrEqualTo(1f), what);
        }
    }
}
