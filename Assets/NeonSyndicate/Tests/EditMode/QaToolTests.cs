using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The QA capture tools' pure parts: golden views start in the open with stable file names, the flythrough route
    /// stays in the open for 60 to 90 seconds, and the statistics and image comparison maths are right.
    /// </summary>
    public sealed class QaToolTests
    {
        // ------------------------------------------------------------------ golden views

        [Test]
        public void GoldenViewsAreUniqueAndInsideTheMap()
        {
            List<GoldenView> views = GoldenViews.Build(TestData.City);
            Assert.That(views.Count, Is.InRange(10, 16), "about twelve views");
            HashSet<string> ids = new HashSet<string>();
            HashSet<District> districts = new HashSet<District>();
            foreach (GoldenView v in views)
            {
                Assert.That(ids.Add(v.Id), Is.True, "unique id " + v.Id);
                foreach (char ch in v.Id) Assert.That((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '_', Is.True, v.Id + " is safe as a file name");
                Assert.That(string.IsNullOrEmpty(v.Title), Is.False, v.Id + " title");
                Assert.That(TestData.Finite(v.Position) && TestData.Finite(v.LookAt), Is.True, v.Id + " finite");
                Assert.That(TestData.InsideMap(v.Position), Is.True, v.Id + " camera inside the map");
                Assert.That(Vector3.Distance(v.Position, v.LookAt), Is.GreaterThan(20f), v.Id + " looks at something away from the camera");
                Assert.That(v.Fov, Is.InRange(20f, 90f), v.Id + " field of view");
                Assert.That(v.Position.y, Is.GreaterThan(1f), v.Id + " above the ground");
                districts.Add(CityLayout.DistrictAt(v.Position));
            }
            Assert.That(districts.Count, Is.GreaterThanOrEqualTo(8), "views cover most districts and the desert");
            Assert.That(districts.Contains(District.QuartzBadlands), Is.True, "at least one desert view");
        }

        [Test]
        public void GoldenCamerasStartInTheOpen()
        {
            CityLayout L = TestData.City;
            foreach (GoldenView v in GoldenViews.Build(L))
            {
                string inside = TestData.BuildingAt(L, v.Position, 1.5f, 3f);
                Assert.That(inside, Is.Null, v.Id + " camera starts inside " + inside);
                Vector3 dir = (v.LookAt - v.Position).normalized;
                for (float d = 1f; d <= 20f; d += 1f)
                {
                    string blocked = TestData.BuildingAt(L, v.Position + dir * d, 0f, 0f);
                    Assert.That(blocked, Is.Null, v.Id + " looks straight into " + blocked + " " + d + " m ahead");
                }
            }
        }

        [Test]
        public void GoldenFileNamesAreStable()
        {
            GoldenView v = new GoldenView("strip_street", "test", Vector3.zero, Vector3.forward, 55f);
            Assert.That(GoldenViews.FileName(v, 12f), Is.EqualTo("strip_street_1200.png"));
            Assert.That(GoldenViews.FileName(v, 22f), Is.EqualTo("strip_street_2200.png"));
            Assert.That(GoldenViews.ClockStamp(21.5f), Is.EqualTo("2130"));
            Assert.That(GoldenViews.ClockStamp(0f), Is.EqualTo("0000"));
            Assert.That(GoldenViews.Hours, Is.EqualTo(new[] { 12f, 22f }), "noon and 22:00");
        }

        // ------------------------------------------------------------------ flythrough route

        [Test]
        public void FlythroughLastsSixtyToNinetySeconds()
        {
            PerfPath path = PerfPath.CreateDefault();
            Assert.That(path.Duration, Is.InRange(60f, 90f), "duration");
            Assert.That(path.Length, Is.GreaterThan(1500f), "length in meters");
            Assert.That((path.PositionAt(0f) - PerfPath.DefaultPoints()[0].Position).sqrMagnitude, Is.LessThan(1e-4f), "starts at the first point");
        }

        [Test]
        public void FlythroughStaysInTheOpen()
        {
            CityLayout L = TestData.City;
            PerfPath path = PerfPath.CreateDefault();
            HashSet<District> districts = new HashSet<District>();
            Vector3 prev = path.PositionAt(0f);
            for (float t = 0f; t <= path.Duration; t += 1f / 30f)
            {
                Vector3 p = path.PositionAt(t);
                Assert.That(TestData.Finite(p), Is.True, "finite at " + t);
                Assert.That(TestData.InsideMap(p), Is.True, "inside the map at " + t);
                if (p.y < 6f) Assert.Fail("camera dips to " + p.y.ToString("0.0") + " m at " + t.ToString("0.0") + " s (traffic lights reach about 6 m)");
                string inside = TestData.BuildingAt(L, p, 1f, 3f);
                if (inside != null) Assert.Fail("camera flies through " + inside + " at " + t.ToString("0.0") + " s");
                if (Vector3.Distance(prev, p) > 2.5f) Assert.Fail("camera jumps " + Vector3.Distance(prev, p).ToString("0.0") + " m in one 30 fps frame at " + t.ToString("0.0") + " s");
                Vector3 look = path.LookAtAt(t);
                if (!TestData.Finite(look) || (look - p).sqrMagnitude < 1f) Assert.Fail("no look target at " + t.ToString("0.0") + " s");
                districts.Add(CityLayout.DistrictAt(p));
                prev = p;
            }
            Assert.That(districts.Count, Is.GreaterThanOrEqualTo(5), "the route crosses several districts");
        }

        // ------------------------------------------------------------------ statistics

        [Test]
        public void PercentilesAndWorstFrames()
        {
            List<float> v = new List<float>();
            for (int i = 1; i <= 100; i++) v.Add(i);
            v.Reverse();
            Assert.That(PerfStats.Average(v), Is.EqualTo(50.5f).Within(1e-4f));
            Assert.That(PerfStats.Percentile(v, 0f), Is.EqualTo(1f));
            Assert.That(PerfStats.Percentile(v, 50f), Is.EqualTo(50.5f).Within(1e-4f));
            Assert.That(PerfStats.Percentile(v, 99f), Is.EqualTo(99.01f).Within(1e-3f));
            Assert.That(PerfStats.Percentile(v, 100f), Is.EqualTo(100f));
            Assert.That(PerfStats.Max(v), Is.EqualTo(100f));
            Assert.That(PerfStats.Min(v), Is.EqualTo(1f));
            Assert.That(PerfStats.Sum(v), Is.EqualTo(5050f));
            Assert.That(PerfStats.CountAbove(v, 90f), Is.EqualTo(10));
            int[] worst = PerfStats.WorstIndices(v, 3);
            Assert.That(worst, Is.EqualTo(new[] { 0, 1, 2 }), "largest first (the list was reversed)");
            Assert.That(PerfStats.Percentile(new List<float>(), 50f), Is.EqualTo(0f), "empty list");
            Assert.That(PerfStats.WorstIndices(new List<float>(), 5), Is.Empty);
        }

        // ------------------------------------------------------------------ image comparison

        [Test]
        public void IdenticalImagesShowNoChange()
        {
            Color32[] a = Gradient(16, 8), b = Gradient(16, 8), heat = new Color32[16 * 8];
            ImageDiffResult r = ImageDiff.Compare(a, b, 16, 8, ImageDiff.DefaultThreshold, heat);
            Assert.That(r.MeanAbsDiff, Is.EqualTo(0f));
            Assert.That(r.PercentChanged, Is.EqualTo(0f));
            Assert.That(r.ChangedPixels, Is.EqualTo(0));
            Assert.That(r.MaxDiff, Is.EqualTo(0));
            foreach (Color32 c in heat) Assert.That(c.r == c.g && c.g == c.b, Is.True, "unchanged pixels are grey in the heatmap");
        }

        [Test]
        public void ChangedPixelsAreCounted()
        {
            Color32[] a = Gradient(10, 10), b = Gradient(10, 10);
            for (int i = 0; i < 25; i++) b[i] = new Color32((byte)(a[i].r + 50), a[i].g, a[i].b, 255);
            ImageDiffResult r = ImageDiff.Compare(a, b, 10, 10, 12, null);
            Assert.That(r.ChangedPixels, Is.EqualTo(25));
            Assert.That(r.PercentChanged, Is.EqualTo(25f).Within(1e-4f));
            Assert.That(r.MaxDiff, Is.EqualTo(50));
            Assert.That(r.MeanAbsDiff, Is.EqualTo(25f * 50f / 300f).Within(1e-4f), "mean over every channel of every pixel");
            ImageDiffResult loose = ImageDiff.Compare(a, b, 10, 10, 60, null);
            Assert.That(loose.ChangedPixels, Is.EqualTo(0), "changes under the threshold do not count");
        }

        [Test]
        public void MismatchedImagesCountAsFullyChanged()
        {
            ImageDiffResult r = ImageDiff.Compare(Gradient(4, 4), Gradient(2, 2), 4, 4, 12, null);
            Assert.That(r.PercentChanged, Is.EqualTo(100f));
        }

        [Test]
        public void ResizeKeepsTheCorners()
        {
            Color32[] src = Gradient(8, 4);
            Color32[] dst = ImageDiff.Resize(src, 8, 4, 16, 8);
            Assert.That(dst.Length, Is.EqualTo(16 * 8));
            Assert.That(dst[0].r, Is.EqualTo(src[0].r), "bottom left");
            Assert.That(dst[16 * 8 - 1].r, Is.EqualTo(src[8 * 4 - 1].r), "top right");
            Color32 hot = ImageDiff.HeatColor(1f), cold = ImageDiff.HeatColor(0f);
            Assert.That(hot.r + hot.g + hot.b, Is.GreaterThan(cold.r + cold.g + cold.b), "big changes glow brighter");
        }

        private static Color32[] Gradient(int w, int h)
        {
            Color32[] px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = new Color32((byte)(x * 255 / Mathf.Max(1, w - 1) * 3 / 4), (byte)(y * 255 / Mathf.Max(1, h - 1) * 3 / 4), 64, 255);
            return px;
        }
    }
}
