using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The skull measurement behind the live hair (LiveSkullMeasure, LiveSkullFit): on a synthetic head (a sphere with
    /// ears, brows, eyes and a collar) the envelope sits on the surface without sinking into it, heights turn into rows,
    /// the landmarks land where the features were placed, the clearance stays outside the collar, and bad input is
    /// refused with a reason. Engine-free; native Unity is NOT RUN here.
    /// </summary>
    public sealed class LiveSkullFitTests
    {
        /// <summary>The synthetic head: a sphere of radius SphereRadius centred at SphereCentre, cut below the neck line.</summary>
        internal static readonly Vector3 SphereCentre = new Vector3(0f, 0.12f, 0.012f);
        internal const float SphereRadius = 0.1f;
        internal const float EarTopY = 0.14f, EarBottomY = 0.085f, BrowY = 0.165f, EyeLineY = 0.148f, CollarRadius = 0.135f;

        /// <summary>Points on the sphere (a spiral, dense enough that every cone holds several), without the neck opening.</summary>
        internal static List<Vector3> Skull()
        {
            List<Vector3> points = new List<Vector3>();
            const int n = 2600;
            for (int i = 0; i < n; i++)
            {
                float y = 1f - 2f * (i + 0.5f) / n;
                float r = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
                float a = i * 2.399963f;
                Vector3 p = SphereCentre + new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r) * SphereRadius;
                if (p.y < 0.03f) continue;
                points.Add(p);
            }
            // Ears: small blobs standing out of the sides.
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 40; i++)
                {
                    float t = (float)i / 39f;
                    float y = Mathf.Lerp(EarBottomY, EarTopY, t);
                    float bulge = Mathf.Sin(t * Mathf.PI) * 0.012f;
                    points.Add(new Vector3(side * (SphereRadius + 0.004f + bulge), y, SphereCentre.z - 0.016f + (i % 3) * 0.004f));
                }
            return points;
        }

        internal static List<Vector3> Brows()
        {
            List<Vector3> points = new List<Vector3>();
            for (int i = 0; i < 30; i++)
            {
                float x = Mathf.Lerp(-0.045f, 0.045f, i / 29f);
                float z = SphereCentre.z + Mathf.Sqrt(Mathf.Max(0f, SphereRadius * SphereRadius - x * x - (BrowY - SphereCentre.y) * (BrowY - SphereCentre.y))) + 0.003f;
                points.Add(new Vector3(x, BrowY - (i % 2) * 0.006f, z));
            }
            return points;
        }

        internal static List<Vector3> Eyes()
        {
            List<Vector3> points = new List<Vector3>();
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 12; i++)
                    points.Add(new Vector3(side * 0.03f + (i % 4) * 0.004f, EyeLineY + (i / 4) * 0.004f - 0.004f, SphereCentre.z + 0.09f));
            return points;
        }

        /// <summary>A stand collar ring and a coat below the ear line.</summary>
        internal static List<Vector3> Cover()
        {
            List<Vector3> points = new List<Vector3>();
            for (int i = 0; i < 72; i++)
            {
                float a = i * Mathf.PI * 2f / 72f;
                for (int k = 0; k < 6; k++)
                {
                    float y = 0.07f - k * 0.04f;
                    points.Add(new Vector3(Mathf.Sin(a) * CollarRadius, y, SphereCentre.z + Mathf.Cos(a) * CollarRadius));
                }
            }
            return points;
        }

        internal static LiveSkullFit Head()
        {
            return LiveSkullMeasure.Measure(Skull(), Brows(), Eyes(), Cover());
        }

        [Test]
        public void SyntheticHeadIsValidWithItsLandmarks()
        {
            LiveSkullFit fit = Head();
            Assert.IsTrue(fit.Valid, fit.Reason);
            Assert.AreEqual(SphereCentre.y + SphereRadius, fit.Crown, 0.002f, "crown");
            Assert.AreEqual(EarTopY, fit.EarTop, 0.004f, "ear top");
            Assert.AreEqual(EarBottomY, fit.EarBottom, 0.004f, "ear bottom");
            Assert.Greater(fit.EarX, SphereRadius + 0.01f, "the ears stand out of the sphere");
            Assert.AreEqual(BrowY, fit.BrowTop, 0.001f, "brow top");
            Assert.AreEqual(EyeLineY, fit.EyeY, 0.004f, "eye line");
            Assert.Less(fit.Chin, fit.EyeY - 0.05f, "the chin is well below the eyes");
            Assert.Less(fit.Nape, fit.EarBottom, "the nape is below the ears");
            Assert.AreEqual(0f, fit.Centre.x, 1e-6f, "the centre sits on the symmetry plane");
            Assert.Greater(fit.Centre.y, fit.EyeY);
            Assert.Less(fit.Centre.y, fit.Crown);
            float ear = LiveHairShells.EarAzimuth(fit);
            Assert.That(ear, Is.InRange(85f, 125f), "the ears sit a little behind the centre");
        }

        [Test]
        public void EnvelopeSitsOnTheSurfaceWithoutSinkingIn()
        {
            LiveSkullFit fit = Head();
            Assert.IsTrue(fit.Valid, fit.Reason);
            float worstIn = 0f, worstOut = 0f;
            string whereOut = "";
            for (float theta = 12f; theta <= 105f; theta += 6f)
                for (float phi = -180f; phi < 180f; phi += 12f)
                {
                    Vector3 p = fit.Surface(theta, phi, 0f);
                    float d = (p - SphereCentre).magnitude - SphereRadius;
                    // The ears make the sides bulge on purpose; measure the sphere elsewhere.
                    bool nearEar = Mathf.Abs(p.y - (EarTopY + EarBottomY) * 0.5f) < 0.052f && Mathf.Abs(p.x) > 0.06f;
                    if (nearEar) continue;
                    if (-d > worstIn) worstIn = -d;
                    if (d > worstOut) { worstOut = d; whereOut = "theta " + theta + " phi " + phi + " at " + p; }
                }
            Assert.LessOrEqual(worstIn, 0.0015f, "the envelope never sinks more than 1.5 mm into the skin");
            Assert.LessOrEqual(worstOut, 0.012f, "the envelope never floats more than 12 mm off the skin: " + whereOut);
        }

        [Test]
        public void PolarAtHeightFindsTheRowOfAHeight()
        {
            LiveSkullFit fit = Head();
            foreach (float phi in new[] { 0f, 60f, -90f, 180f })
            {
                float y = SphereCentre.y + SphereRadius * Mathf.Cos(70f * Mathf.Deg2Rad);
                float theta = fit.PolarAtHeight(phi, y);
                Assert.That(theta, Is.InRange(10f, 170f), "a height on the skull is a row");
                Assert.AreEqual(y, fit.HeightAt(theta, phi), 0.004f, "the row lands on the height at " + phi);
            }
            Assert.AreEqual(0f, fit.PolarAtHeight(0f, fit.Crown + 0.05f), "above the crown is the pole");
            Assert.AreEqual(180f, fit.PolarAtHeight(0f, -1f), "a height under the head is never reached");
        }

        [Test]
        public void ClearanceStaysOutsideTheCollarAndNeverInsideTheSkull()
        {
            LiveSkullFit fit = Head();
            for (float phi = -180f; phi < 180f; phi += 30f)
            {
                float atCollar = fit.ClearanceAt(phi, 0.0f);
                Assert.GreaterOrEqual(atCollar, CollarRadius - 0.004f, "the collar ring is cleared at " + phi);
                float justBelowEar = fit.ClearanceAt(phi, fit.EarTop - 0.01f);
                float skull = fit.HorizontalRadius(fit.PolarAtHeight(phi, fit.EarTop - 0.01f), phi);
                Assert.GreaterOrEqual(justBelowEar, skull - 0.001f, "never inside the skull at " + phi);
            }
            Assert.GreaterOrEqual(fit.ClearanceAt(180f, fit.EarTop - LiveSkullFit.ClearDepth + 0.01f), CollarRadius * 0.5f, "bands below the coat continue it, fading a tenth per band");
            Assert.AreEqual(0.07f, fit.CollarTopAt(180f), 0.002f, "the collar's top is known per column");
            Assert.AreEqual(LiveSkullFit.NoCollar, LiveSkullMeasure.Measure(Skull(), null, null, null).CollarTopAt(0f), 1e-4f, "a bare neck has no collar top");
        }

        [Test]
        public void RefusesHeadsThatAreNotHeads()
        {
            LiveSkullFit few = LiveSkullMeasure.Measure(new List<Vector3> { Vector3.up, Vector3.one }, null, null, null);
            Assert.IsFalse(few.Valid);
            StringAssert.Contains("too few", few.Reason);
            List<Vector3> tiny = new List<Vector3>();
            foreach (Vector3 p in Skull()) tiny.Add(p * 0.2f);
            LiveSkullFit small = LiveSkullMeasure.Measure(tiny, null, null, null);
            Assert.IsFalse(small.Valid);
            StringAssert.Contains("metres", small.Reason);
            Assert.IsFalse(LiveSkullMeasure.Measure(null, null, null, null).Valid);
        }

        [Test]
        public void FallbacksWithoutBrowsEyesOrCoverStillMeasure()
        {
            LiveSkullFit fit = LiveSkullMeasure.Measure(Skull(), null, null, null);
            Assert.IsTrue(fit.Valid, fit.Reason);
            Assert.Greater(fit.BrowTop, fit.EyeY, "brows above the eyes by the fallback rule");
            Assert.Less(fit.EyeY, fit.EarTop, "eyes near the ear line by the fallback rule");
            for (float phi = -180f; phi < 180f; phi += 45f)
                Assert.Greater(fit.ClearanceAt(phi, fit.EarTop - 0.1f), 0.05f, "the clearance holds the skull itself when nothing covers the neck");
        }

        [Test]
        public void RadiusWithoutEarsFollowsTheSkullAtTheEars()
        {
            LiveSkullFit fit = Head();
            float ear = LiveHairShells.EarAzimuth(fit);
            float theta = fit.PolarAtHeight(ear, (EarTopY + EarBottomY) * 0.5f);
            Assert.Greater(fit.RadiusAt(theta, ear), fit.RadiusWithoutEars(theta, ear) + 0.005f, "the ear bulge is smoothed away");
            Assert.AreEqual(fit.RadiusAt(theta, 0f), fit.RadiusWithoutEars(theta, 0f), 1e-5f, "the face is untouched");
            Assert.AreEqual(fit.RadiusAt(20f, ear), fit.RadiusWithoutEars(20f, ear), 1e-5f, "the crown is untouched");
        }

        [Test]
        public void DirectionsAndPolarAnglesAgree()
        {
            LiveSkullFit fit = Head();
            Vector3 p = fit.Surface(63f, -40f, 0.01f);
            float r, theta, phi;
            fit.Polar(p, out r, out theta, out phi);
            Assert.AreEqual(63f, theta, 0.01f);
            Assert.AreEqual(-40f, phi, 0.01f);
            Assert.AreEqual(fit.RadiusAt(63f, -40f) + 0.01f, r, 1e-4f);
            Assert.AreEqual(1f, LiveSkullFit.Direction(90f, 0f).z, 1e-5f, "azimuth 0 faces forward");
            Assert.AreEqual(1f, LiveSkullFit.Direction(90f, 90f).x, 1e-5f, "azimuth 90 is the right ear");
        }
    }
}
