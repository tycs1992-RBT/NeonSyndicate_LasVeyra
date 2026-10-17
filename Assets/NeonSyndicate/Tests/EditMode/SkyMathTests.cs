using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// Phase 1 lighting maths: the sun's path, the Chapman approximation against a brute-force integral, the sun's
    /// colour and strength by elevation, the Kelvin to RGB conversion, the sky model's ambient, the ground bounce and
    /// the shadow cascade splits.
    /// </summary>
    public sealed class SkyMathTests
    {
        private const float Re = SkyMath.PlanetRadius, Ra = SkyMath.AtmosphereTop;

        [Test]
        public void SunPathMatchesTheClock()
        {
            Assert.That(SkyMath.SunElevationDegrees(12f), Is.EqualTo(90f).Within(0.01f), "noon overhead");
            Assert.That(SkyMath.SunElevationDegrees(6f), Is.EqualTo(0f).Within(0.01f), "sunrise on the horizon");
            Assert.That(SkyMath.SunElevationDegrees(18f), Is.EqualTo(0f).Within(0.01f), "sunset on the horizon");
            Assert.That(SkyMath.SunElevationDegrees(0f), Is.EqualTo(-90f).Within(0.01f), "midnight below");
            Assert.That(SkyMath.SunElevationDegrees(9f), Is.EqualTo(45f).Within(0.01f));
            Vector3 noon = SkyMath.SunDirection(12f);
            Assert.That(noon.y, Is.EqualTo(1f).Within(0.001f), "noon points straight up");
            Vector3 morning = SkyMath.SunDirection(8f);
            Assert.That(morning.y, Is.EqualTo(Mathf.Sin(30f * Mathf.Deg2Rad)).Within(0.001f), "8:00 is 30 degrees up");
            Assert.That(morning.magnitude, Is.EqualTo(1f).Within(0.001f));
            Vector3 moon = SkyMath.MoonDirection(0f);
            Assert.That(moon.y, Is.GreaterThan(0.9f), "the moon is high at midnight");
        }

        [Test]
        public void DirectionFromAngleMatchesUnityLightEuler()
        {
            // A light with Euler x 90 shines straight down, so the direction toward it is straight up.
            Vector3 up = SkyMath.DirectionFromAngle(90f, 35f);
            Assert.That(up.y, Is.EqualTo(1f).Within(0.0001f));
            // Euler x 0 and yaw 0 shines along +z, so the light lies toward -z.
            Vector3 flat = SkyMath.DirectionFromAngle(0f, 0f);
            Assert.That(flat.z, Is.EqualTo(-1f).Within(0.0001f));
            Assert.That(flat.y, Is.EqualTo(0f).Within(0.0001f));
            // Yaw 90 turns +z toward +x, so the light lies toward -x.
            Vector3 yawed = SkyMath.DirectionFromAngle(0f, 90f);
            Assert.That(yawed.x, Is.EqualTo(-1f).Within(0.0001f));
        }

        [Test]
        public void NightFactorFadesThroughTwilight()
        {
            Assert.That(SkyMath.NightFactor(30f), Is.EqualTo(0f));
            Assert.That(SkyMath.NightFactor(-20f), Is.EqualTo(1f));
            float dusk = SkyMath.NightFactor(-4f);
            Assert.That(dusk, Is.GreaterThan(0.3f).And.LessThan(0.7f), "civil twilight is half way");
            Assert.That(SkyMath.NightFactor(-8f), Is.GreaterThan(dusk));
        }

        /// <summary>Brute-force optical depth in scale heights along a ray from sea level.</summary>
        private static float NumericChapman(float cosChi, float H)
        {
            float r0 = Re;
            float s = Mathf.Sqrt(Mathf.Max(0f, 1f - cosChi * cosChi));
            float b = r0 * cosChi;
            float disc = b * b - (r0 * r0 - Ra * Ra);
            float t = -b + Mathf.Sqrt(disc);
            int n = 40000;
            double ds = t / n, acc = 0.0;
            for (int i = 0; i < n; i++)
            {
                double tt = (i + 0.5) * ds;
                double px = s * tt, py = r0 + cosChi * tt;
                double r = System.Math.Sqrt(px * px + py * py);
                if (r < Re) return SkyMath.Blocked;
                acc += System.Math.Exp(-(r - Re) / H) * ds;
            }
            return (float)(acc / H);
        }

        [Test]
        public void ChapmanApproximationTracksTheIntegral()
        {
            float X = Re / SkyMath.RayleighScaleHeight;
            Assert.That(SkyMath.Chapman(X, 0f, 1f), Is.EqualTo(1f).Within(0.05f), "a vertical ray is one scale height of air");
            foreach (float deg in new[] { 0f, 30f, 60f, 75f, 85f, 90f })
            {
                float cs = Mathf.Cos(deg * Mathf.Deg2Rad);
                float approx = SkyMath.Chapman(X, 0f, cs);
                float exact = NumericChapman(cs, SkyMath.RayleighScaleHeight);
                Assert.That(approx / exact, Is.EqualTo(1f).Within(0.16f), "zenith angle " + deg);
            }
            Assert.That(SkyMath.Chapman(X, 0f, Mathf.Cos(90f * Mathf.Deg2Rad)), Is.GreaterThan(30f), "about 35 air masses at the horizon");
            Assert.That(SkyMath.Chapman(X, 0f, -0.1f), Is.EqualTo(SkyMath.Blocked), "a ray into the ground is blocked");
            // From high up a ray slightly below the horizontal still grazes the air.
            Assert.That(SkyMath.Chapman(X, 60000f / SkyMath.RayleighScaleHeight, -0.05f), Is.LessThan(SkyMath.Blocked));
        }

        [Test]
        public void SunTransmittanceReddensAndFadesTowardTheHorizon()
        {
            Color high = SkyMath.SunTransmittance(60f, 2f, 1f);
            Color low = SkyMath.SunTransmittance(5f, 2f, 1f);
            Color set = SkyMath.SunTransmittance(0f, 2f, 1f);
            Assert.That(high.r, Is.GreaterThan(high.g).And.GreaterThan(high.b), "blue scatters out first");
            Assert.That(high.r, Is.GreaterThan(0.85f));
            Assert.That(low.r / Mathf.Max(0.001f, low.b), Is.GreaterThan(4f), "a low sun is strongly red");
            Assert.That(SkyMath.Luminance(low), Is.LessThan(SkyMath.Luminance(high)));
            Assert.That(SkyMath.Luminance(set), Is.LessThan(0.03f), "almost nothing gets through at the horizon");
            Color below = SkyMath.SunTransmittance(-3f, 2f, 1f);
            Assert.That(SkyMath.Luminance(below), Is.EqualTo(0f));
            Color hazy = SkyMath.SunTransmittance(30f, 2f, 4f);
            Assert.That(SkyMath.Luminance(hazy), Is.LessThan(SkyMath.Luminance(SkyMath.SunTransmittance(30f, 2f, 1f))), "haze dims the sun");
        }

        [Test]
        public void SunIntensityFollowsElevation()
        {
            float noon = SkyMath.SunIntensity(90f, 0f, 1f);
            Assert.That(noon, Is.EqualTo(SkyMath.SunPeakIntensity).Within(0.001f), "the peak is the overhead sun");
            float mid = SkyMath.SunIntensity(30f, 0f, 1f);
            float low = SkyMath.SunIntensity(5f, 0f, 1f);
            float rim = SkyMath.SunIntensity(0.2f, 0f, 1f);
            Assert.That(mid, Is.LessThan(noon).And.GreaterThan(low));
            Assert.That(low, Is.GreaterThan(rim));
            Assert.That(rim, Is.GreaterThan(0f));
            Assert.That(SkyMath.SunIntensity(-1f, 0f, 1f), Is.EqualTo(0f), "off below the horizon");
            Assert.That(SkyMath.SunIntensity(60f, 1f, 1f), Is.EqualTo(SkyMath.SunIntensity(60f, 0f, 1f) * 0.45f).Within(0.001f), "full cloud takes 55 percent");
            // Monotonic from the horizon to the zenith.
            float prev = 0f;
            for (float e = 0f; e <= 90f; e += 2.5f)
            {
                float v = SkyMath.SunIntensity(e, 0f, 1f);
                Assert.That(v, Is.GreaterThanOrEqualTo(prev - 0.0001f), "elevation " + e);
                prev = v;
            }
        }

        [Test]
        public void KelvinToRgbFollowsThePlanckianLocus()
        {
            // Reference chromaticities of the Planckian locus (CIE 1931 x, y).
            Vector2 k2000 = SkyMath.KelvinToXy(2000f);
            Assert.That(k2000.x, Is.EqualTo(0.5267f).Within(0.01f));
            Assert.That(k2000.y, Is.EqualTo(0.4133f).Within(0.01f));
            Vector2 k4000 = SkyMath.KelvinToXy(4000f);
            Assert.That(k4000.x, Is.EqualTo(0.3805f).Within(0.01f));
            Assert.That(k4000.y, Is.EqualTo(0.3768f).Within(0.01f));
            Vector2 k6500 = SkyMath.KelvinToXy(6500f);
            Assert.That(k6500.x, Is.EqualTo(0.3135f).Within(0.01f));
            Assert.That(k6500.y, Is.EqualTo(0.3237f).Within(0.01f));
            Vector2 k10000 = SkyMath.KelvinToXy(10000f);
            Assert.That(k10000.x, Is.EqualTo(0.2807f).Within(0.01f));
            Assert.That(k10000.y, Is.EqualTo(0.2884f).Within(0.01f));
            Color candle = SkyMath.KelvinToRgb(1900f);
            Assert.That(candle.r, Is.EqualTo(1f).Within(0.001f));
            Assert.That(candle.b, Is.LessThan(0.15f), "candle light has almost no blue");
            Color daylight = SkyMath.KelvinToRgb(6500f);
            Assert.That(daylight.r, Is.EqualTo(daylight.b).Within(0.08f), "6500 K is close to white");
            Color blue = SkyMath.KelvinToRgb(12000f);
            Assert.That(blue.b, Is.GreaterThan(blue.r), "a hot blackbody is blue");
            Assert.That(SkyMath.KelvinToRgb(500f), Is.EqualTo(SkyMath.KelvinToRgb(1667f)), "clamped below");
        }

        [Test]
        public void SunColorTemperatureDropsAsTheSunSets()
        {
            float high = SkyMath.ApparentKelvin(SkyMath.SunColor(60f, 1f));
            float mid = SkyMath.ApparentKelvin(SkyMath.SunColor(20f, 1f));
            float low = SkyMath.ApparentKelvin(SkyMath.SunColor(5f, 1f));
            float set = SkyMath.ApparentKelvin(SkyMath.SunColor(0.5f, 1f));
            Assert.That(high, Is.InRange(4800f, 6000f), "a high sun reads like daylight: " + high);
            Assert.That(mid, Is.LessThan(high));
            Assert.That(low, Is.LessThan(mid).And.InRange(1600f, 3600f), "a sun 5 degrees up is orange: " + low);
            Assert.That(set, Is.LessThanOrEqualTo(low), "and redder still at the horizon: " + set);
            Assert.That(SkyMath.Luminance(SkyMath.SunColor(5f, 1f)), Is.EqualTo(1f).Within(0.01f), "the colour is normalised");
        }

        [Test]
        public void SkyScatterIsBlueOverheadAndWarmTowardALowSun()
        {
            Vector3 sunHigh = SkyMath.SunDirection(11f);
            Color zenith = SkyMath.Scatter(Vector3.up, sunHigh, 2f, 1f);
            Assert.That(zenith.b, Is.GreaterThan(zenith.r * 1.5f), "the zenith is blue");
            Vector3 sunLow = new Vector3(0f, Mathf.Sin(5f * Mathf.Deg2Rad), -Mathf.Cos(5f * Mathf.Deg2Rad));
            Color towardSun = SkyMath.Scatter(new Vector3(0f, 0.05f, -1f).normalized, sunLow, 2f, 1f);
            Color awaySun = SkyMath.Scatter(new Vector3(0f, 0.05f, 1f).normalized, sunLow, 2f, 1f);
            Assert.That(towardSun.r, Is.GreaterThan(towardSun.b * 2f), "the horizon toward a low sun is red");
            Assert.That(SkyMath.Luminance(towardSun), Is.GreaterThan(SkyMath.Luminance(awaySun)), "and brighter than the far side");
            Color nightZenith = SkyMath.Scatter(Vector3.up, new Vector3(0f, -0.5f, -0.866f), 2f, 1f);
            Assert.That(SkyMath.Luminance(nightZenith), Is.LessThan(0.0005f), "no scattered sunlight deep in the night");
            Color hazy = SkyMath.Scatter(new Vector3(0f, 0.1f, 1f).normalized, sunHigh, 2f, 4f);
            Color clear = SkyMath.Scatter(new Vector3(0f, 0.1f, 1f).normalized, sunHigh, 2f, 1f);
            Assert.That(hazy.r / Mathf.Max(0.0001f, hazy.b), Is.GreaterThan(clear.r / Mathf.Max(0.0001f, clear.b)), "haze whitens the horizon");
        }

        [Test]
        public void SkyAmbientIsAUsefulFractionOfTheSun()
        {
            float sunIrr = SkyMath.SunIrradiance(SkyMath.SunPeakIntensity);
            Color upper, horizon, mean;
            SkyMath.SkyAmbient(SkyMath.SunDirection(11f), sunIrr, 1f, out upper, out horizon, out mean);
            float shade = SkyMath.Luminance(mean);
            float sun = SkyMath.SunIntensity(75f, 0f, 1f);
            Assert.That(shade / sun, Is.InRange(0.08f, 0.35f), "a grey card in the shade shows a tenth to a third of one in the sun: " + (shade / sun));
            Assert.That(upper.b, Is.GreaterThan(upper.r), "blue overhead");
            Assert.That(SkyMath.Luminance(horizon), Is.GreaterThan(SkyMath.Luminance(upper)), "the horizon band is brighter than the zenith");
            Color nUpper, nHorizon, nMean;
            SkyMath.SkyAmbient(SkyMath.SunDirection(1f), sunIrr, 1f, out nUpper, out nHorizon, out nMean);
            Assert.That(SkyMath.Luminance(nMean), Is.LessThan(0.002f), "the model alone is dark at night (the night glow is added on top)");
        }

        [Test]
        public void CloudAndGroundTermsBehave()
        {
            Color skyMean = new Color(0.2f, 0.25f, 0.35f);
            Color lit = SkyMath.CloudRadiance(Color.white, 1.45f, 1f, skyMean);
            Color dusk = SkyMath.CloudRadiance(Color.white, 1.45f, 0f, skyMean);
            Assert.That(SkyMath.Luminance(lit), Is.GreaterThan(SkyMath.Luminance(dusk)), "sunlit cloud tops are brighter");
            Assert.That(SkyMath.Luminance(dusk), Is.GreaterThan(0f), "the sky still lights them");
            Color sand = SkyMath.GroundRadiance(SkyGround.Albedo(GroundKind.Sand), Color.white, 1.45f, 0.8f, skyMean, Color.black);
            Color asphalt = SkyMath.GroundRadiance(SkyGround.Albedo(GroundKind.Asphalt), Color.white, 1.45f, 0.8f, skyMean, Color.black);
            Color snow = SkyMath.GroundRadiance(SkyGround.Albedo(GroundKind.Snow), Color.white, 1.45f, 0.8f, skyMean, Color.black);
            Assert.That(SkyMath.Luminance(snow), Is.GreaterThan(SkyMath.Luminance(sand)).And.GreaterThan(SkyMath.Luminance(asphalt) * 4f), "snow bounces most, asphalt least");
            Assert.That(sand.r, Is.GreaterThan(sand.b), "sand bounces warm light");
            Color night = SkyMath.GroundRadiance(SkyGround.Albedo(GroundKind.Asphalt), Color.white, 0f, -0.5f, Color.black, new Color(0.1f, 0.08f, 0.2f));
            Assert.That(night.b, Is.GreaterThan(0f), "the neon glow keeps the ground from going black");
            Assert.That(night.r, Is.LessThan(night.b));
        }

        [Test]
        public void GroundClassificationReadsTheCity()
        {
            CityLayout L = TestData.City;
            Assert.That(SkyGround.Classify(L, 1400f, 0f), Is.EqualTo(GroundKind.Sand), "east desert");
            Assert.That(SkyGround.Classify(L, -1000f, 1400f), Is.EqualTo(GroundKind.Snow), "deep Frostline");
            Assert.That(SkyGround.Classify(L, 0f, CityLayout.CanalZ), Is.EqualTo(GroundKind.Water), "canal");
            Assert.That(SkyGround.Classify(null, 0f, 0f), Is.EqualTo(GroundKind.Asphalt), "no layout: asphalt");
            RoadEdge e = L.Edges[0];
            Vector3 mid = (L.Nodes[e.A].Pos + L.Nodes[e.B].Pos) * 0.5f;
            Assert.That(SkyGround.Classify(L, mid.x, mid.z), Is.EqualTo(GroundKind.Asphalt), "the middle of a road");
            bool park = false;
            foreach (Block b in L.Blocks)
                if (b.IsPark) { park = true; Assert.That(SkyGround.Classify(L, b.Inner.CenterX, b.Inner.CenterZ), Is.EqualTo(GroundKind.Grass), "a park block"); break; }
            Assert.That(park, Is.True, "the city has a park");
            Color c = SkyGround.SampleAlbedo(L, mid.x, mid.z, 9f);
            Assert.That(c.a, Is.EqualTo(1f));
            Assert.That(SkyMath.Luminance(c), Is.InRange(0.05f, 0.4f));
            foreach (GroundKind k in System.Enum.GetValues(typeof(GroundKind)))
            {
                Color a = SkyGround.Albedo(k);
                Assert.That(a.r, Is.InRange(0f, 1f)); Assert.That(a.g, Is.InRange(0f, 1f)); Assert.That(a.b, Is.InRange(0f, 1f));
            }
        }

        [Test]
        public void CascadeSplitsSuitAStreetCamera()
        {
            float[] four = SkyMath.CascadeSplits(150f, 4);
            Assert.That(four.Length, Is.EqualTo(3));
            Assert.That(four[0], Is.LessThan(four[1]).And.LessThan(0.2f), "the first cascade is the nearest tenth or so");
            Assert.That(four[1], Is.LessThan(four[2]));
            Assert.That(four[2], Is.LessThan(0.6f), "the far cascade still gets a good share");
            Assert.That(four[0] * 150f, Is.InRange(8f, 25f), "first split in metres: " + four[0] * 150f);
            float[] two = SkyMath.CascadeSplits(80f, 2);
            Assert.That(two.Length, Is.EqualTo(1));
            Assert.That(two[0], Is.InRange(0.1f, 0.5f));
            Assert.That(SkyMath.CascadeSplits(45f, 1).Length, Is.EqualTo(0));
            float[] ultra = SkyMath.CascadeSplits(SkyMath.ShadowDistance(GraphicsPreset.Ultra), SkyMath.ShadowCascades(GraphicsPreset.Ultra));
            Assert.That(ultra[0] * SkyMath.ShadowDistance(GraphicsPreset.Ultra), Is.InRange(15f, 40f), "Ultra's first cascade reaches past the street");
            Assert.That(SkyMath.ShadowDistance(GraphicsPreset.Ultra), Is.GreaterThan(SkyMath.ShadowDistance(GraphicsPreset.High)));
            Assert.That(SkyMath.ShadowCascades(GraphicsPreset.Low), Is.EqualTo(1));
            Assert.That(SkyMath.ShadowCascades(GraphicsPreset.High), Is.EqualTo(4));
        }

        [Test]
        public void PhaseFunctionsIntegrateToOne()
        {
            // Integrate over the sphere: 2 pi times the integral over cos theta from -1 to 1.
            float r = 0f, m = 0f;
            int n = 20000;
            for (int i = 0; i < n; i++)
            {
                float c = -1f + (i + 0.5f) * 2f / n;
                r += SkyMath.RayleighPhase(c) * 2f / n;
                m += SkyMath.MiePhase(c, SkyMath.MieG) * 2f / n;
            }
            Assert.That(r * 2f * Mathf.PI, Is.EqualTo(1f).Within(0.01f));
            Assert.That(m * 2f * Mathf.PI, Is.EqualTo(1f).Within(0.02f));
        }

        [Test]
        public void MoonPhaseCycles()
        {
            float a = SkyMath.MoonPhase(1), b = SkyMath.MoonPhase(1 + 30);
            Assert.That(a, Is.InRange(0f, 1f));
            Assert.That(b, Is.EqualTo(a).Within(0.03f), "about a month later the phase is back");
        }
    }
}
