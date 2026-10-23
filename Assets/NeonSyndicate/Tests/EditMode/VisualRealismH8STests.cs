using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// H8S visual realism, the pure parts: the leaning sun path keeps the clock's sunrise and sunset, the haze and fill
    /// rules stay in range and fade with night and the Veil, and the generated surfaces are tileable, in the canon
    /// palette, deterministic and sane (normals unit length, macro maps neutral on average).
    /// </summary>
    public sealed class VisualRealismH8STests
    {
        [Test]
        public void SunPathLeansButKeepsSunriseAndSunset()
        {
            float tilt = RealismRulesH8S.SunPathTiltDegrees;
            Vector3 rise = RealismRulesH8S.SunDirection(0f, SkyMath.SunYaw, tilt, 1f);
            Vector3 set = RealismRulesH8S.SunDirection(180f, SkyMath.SunYaw, tilt, 1f);
            Assert.That(rise.y, Is.EqualTo(0f).Within(1e-4f), "sunrise on the horizon");
            Assert.That(set.y, Is.EqualTo(0f).Within(1e-4f), "sunset on the horizon");
            Assert.That((rise - SkyMath.DirectionFromAngle(0f, SkyMath.SunYaw)).magnitude, Is.LessThan(1e-4f), "same sunrise point as the old path");
            Vector3 noon = RealismRulesH8S.SunDirection(90f, SkyMath.SunYaw, tilt, 1f);
            float noonElevation = Mathf.Asin(noon.y) * Mathf.Rad2Deg;
            Assert.That(noonElevation, Is.EqualTo(90f - tilt).Within(0.01f), "the noon sun stands at 90 minus the tilt");
            Assert.That(RealismRulesH8S.SunElevationDegrees(90f, tilt), Is.EqualTo(noonElevation).Within(0.01f));
            for (float a = -10f; a <= 190f; a += 7f)
                Assert.That(RealismRulesH8S.SunDirection(a, SkyMath.SunYaw, tilt, -1f).magnitude, Is.EqualTo(1f).Within(1e-4f), "unit length at " + a);
            // Tilt 0 is exactly the old path.
            for (float a = 0f; a <= 180f; a += 15f)
                Assert.That((RealismRulesH8S.SunDirection(a, SkyMath.SunYaw, 0f, 1f) - SkyMath.DirectionFromAngle(a, SkyMath.SunYaw)).magnitude, Is.LessThan(1e-4f), "old path at " + a);
            // 15:30 (the Free Roam start) gives longer shadows than before.
            float angle = SkyMath.SunAngleDegrees(15.5f);
            Assert.That(RealismRulesH8S.SunElevationDegrees(angle, tilt), Is.LessThan(SkyMath.SunElevationDegrees(15.5f)));
        }

        [Test]
        public void HazeAndFillRulesStayInRangeAndFadeAtNightAndInTheVeil()
        {
            Color fog = new Color(0.49f, 0.68f, 0.88f);
            Color dayHaze = RealismRulesH8S.HazeColor(fog, 1f, 1f, 0f, 0f);
            Assert.That(dayHaze.b - dayHaze.r, Is.LessThan(fog.b - fog.r), "the clear-day haze is greyer than the old sky-blue fog");
            Assert.That(RealismRulesH8S.HazeColor(fog, 0f, 1f, 0f, 0f), Is.EqualTo(fog), "night keeps its fog colour");
            Assert.That(RealismRulesH8S.HazeColor(fog, 1f, 1f, 1f, 0f), Is.EqualTo(fog), "the Veil keeps its fog colour");
            Assert.That(RealismRulesH8S.ExtraFogDensity(1f, 0f, 0f), Is.EqualTo(RealismRulesH8S.DayFogDensity - RealismRulesH8S.OldDayFogDensity).Within(1e-7f));
            Assert.That(RealismRulesH8S.ExtraFogDensity(0f, 0f, 0f), Is.EqualTo(0f));
            Assert.That(RealismRulesH8S.ExtraFogDensity(1f, 0f, 1f), Is.EqualTo(0f), "the Frostline keeps its own air");
            float lost300 = RealismRulesH8S.FogAmount(RealismRulesH8S.DayFogDensity, 300f);
            Assert.That(lost300, Is.InRange(0.15f, 0.4f), "a real but readable haze at 300 m");
            Assert.That(RealismRulesH8S.FogAmount(RealismRulesH8S.DayFogDensity, 30f), Is.LessThan(0.01f), "combat range stays clear");
            Assert.That(RealismRulesH8S.HeightFogAmount(300f, 2f, 0f), Is.InRange(0.1f, 0.5f), "height haze along the ground");
            Assert.That(RealismRulesH8S.HeightFogAmount(300f, 120f, 0f), Is.LessThan(RealismRulesH8S.HeightFogAmount(300f, 2f, 0f)), "thinner high up");
            Assert.That(RealismRulesH8S.FillGain(0f), Is.EqualTo(RealismRulesH8S.DayFillGain));
            Assert.That(RealismRulesH8S.FillGain(1f), Is.EqualTo(SkyMath.UrbanBounceGain), "night keeps the old gain");
            Assert.That(RealismRulesH8S.ReflectionScale(0f), Is.InRange(0.5f, 1f));
            float k = RealismRulesH8S.SkyAmbientIntensity(new Color(0.23f, 0.33f, 0.5f), new Color(0.1f, 0.18f, 0.32f));
            Assert.That(k, Is.InRange(0.2f, 1.6f));
            Assert.That(RealismRulesH8S.SkyAmbientIntensity(new Color(0.2f, 0.2f, 0.2f), Color.black), Is.EqualTo(1f), "a black dome falls back to 1");
            Assert.That(RealismRulesH8S.SkyAmbientIntensity(new Color(1f, 1f, 1f), new Color(1e-4f, 1e-4f, 1e-4f)), Is.EqualTo(1.6f), "clamped");
            Assert.That(RealismRulesH8S.NightCloudScale(1f), Is.EqualTo(RealismRulesH8S.NightGlowScale).Within(1e-5f));
            Assert.That(RealismRulesH8S.LinearToGamma(RealismRulesH8S.GammaToLinear(0.5f)), Is.EqualTo(0.5f).Within(0.01f));
        }

        [Test]
        public void GeneratedSurfacesAreSaneTileableAndInThePalette()
        {
            const int n = 64;
            RealismMapsH8S[] all =
            {
                RealismSurfacesH8S.Ground(n), RealismSurfacesH8S.Ashlar(n), RealismSurfacesH8S.Flagstone(n),
                RealismSurfacesH8S.Bronze(n), RealismSurfacesH8S.Crystal(n, false), RealismSurfacesH8S.Crystal(n, true)
            };
            foreach (RealismMapsH8S m in all)
            {
                Assert.That(m.Size, Is.EqualTo(n));
                Assert.That(m.Albedo.Length, Is.EqualTo(n * n));
                Assert.That(m.Normal.Length, Is.EqualTo(n * n));
                Assert.That(m.Mask.Length, Is.EqualTo(n * n));
                for (int i = 0; i < m.Normal.Length; i += 37)
                {
                    Vector3 v = SurfaceMaps.Decode(m.Normal[i]);
                    Assert.That(v.magnitude, Is.InRange(0.9f, 1.1f), "unit normal");
                    Assert.That(v.z, Is.GreaterThan(0.3f), "normals face out of the surface");
                }
                // Seamless: the first and last columns and rows differ no more than neighbouring texels do on average.
                Assert.That(EdgeJump(m.Albedo, n), Is.LessThan(InteriorJump(m.Albedo, n) * 2.5f + 6f), "tiles without a seam");
            }
            Color ground = RealismSurfacesH8S.MeanAlbedo(all[0].Albedo);
            Assert.That(RealismRulesH8S.Luminance(ground), Is.InRange(0.25f, 0.5f), "the ground is weathered earth, not bright sand: " + ground);
            Assert.That(ground.r - ground.b, Is.InRange(0f, 0.15f), "a muted warm grey, not saturated yellow");
            Color stone = RealismSurfacesH8S.MeanAlbedo(all[1].Albedo);
            Assert.That(RealismRulesH8S.Luminance(stone), Is.InRange(0.15f, 0.4f), "charcoal to weathered stone: " + stone);
            Color crystal = RealismSurfacesH8S.MeanAlbedo(all[4].Albedo);
            Assert.That(crystal.b, Is.GreaterThan(crystal.g), "amethyst is violet");
            Color turquoise = RealismSurfacesH8S.MeanAlbedo(all[5].Albedo);
            Assert.That(turquoise.g, Is.GreaterThan(turquoise.r), "turquoise is teal");
            Assert.That(all[4].Emission, Is.Not.Null, "crystals carry a local glow map");
            Assert.That(all[0].Emission, Is.Null, "the ground never glows");
            // Deterministic.
            Assert.That(RealismSurfacesH8S.Ashlar(n).Albedo[777].r, Is.EqualTo(all[1].Albedo[777].r));
        }

        [Test]
        public void MacroMapsAreNeutralOnAverage()
        {
            foreach (int kind in new[] { RealismSurfacesH8S.MacroStone, RealismSurfacesH8S.MacroGround, RealismSurfacesH8S.MacroPaving })
            {
                Color32[] px = RealismSurfacesH8S.Macro(64, kind);
                Color mean = RealismSurfacesH8S.MeanAlbedo(px);
                Assert.That(mean.g, Is.InRange(0.42f, 0.56f), "the multiply-by-two slot keeps the average colour (kind " + kind + ")");
                byte lo = 255, hi = 0;
                foreach (Color32 c in px) { if (c.g < lo) lo = c.g; if (c.g > hi) hi = c.g; }
                Assert.That(hi - lo, Is.GreaterThan(12), "it actually varies the surface (kind " + kind + ")");
            }
            Vector2 a = RealismMaterialsH8S.ModuleUvShift(new Vector3(10f, 0f, 20f));
            Vector2 b = RealismMaterialsH8S.ModuleUvShift(new Vector3(14f, 0f, 20f));
            Assert.That(a, Is.Not.EqualTo(b), "neighbouring modules get different shifts");
            Assert.That(RealismMaterialsH8S.ModuleUvShift(new Vector3(10f, 0f, 20f)), Is.EqualTo(a), "deterministic");
        }

        private static float EdgeJump(Color32[] px, int n)
        {
            float sum = 0f;
            for (int i = 0; i < n; i++)
            {
                sum += Mathf.Abs(px[i * n].g - px[i * n + n - 1].g);
                sum += Mathf.Abs(px[i].g - px[(n - 1) * n + i].g);
            }
            return sum / (2f * n);
        }

        private static float InteriorJump(Color32[] px, int n)
        {
            float sum = 0f;
            int count = 0;
            for (int y = 0; y < n; y++)
                for (int x = 1; x < n; x++) { sum += Mathf.Abs(px[y * n + x].g - px[y * n + x - 1].g); count++; }
            return sum / count;
        }
    }
}
