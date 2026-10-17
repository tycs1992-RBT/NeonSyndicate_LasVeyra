using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The procedural PBR surfaces (Phase 2 of the visual upgrade): every height-field generator is in range,
    /// tileable and deterministic; normals decode to unit vectors; cavities and edges land where the relief says;
    /// the mask packs its channels; the wet smoothness cap keeps the rain sheen at or under 1; the texture sizes
    /// follow the graphics presets. Pure maths only, so it all runs offline.
    /// </summary>
    public sealed class SurfaceMapsTests
    {
        private const int N = 64;

        private static SurfaceKind[] Kinds { get { return (SurfaceKind[])System.Enum.GetValues(typeof(SurfaceKind)); } }

        private static void AssertUnit(float[] h, string what)
        {
            for (int i = 0; i < h.Length; i++)
            {
                Assert.That(float.IsNaN(h[i]) || float.IsInfinity(h[i]), Is.False, what + " texel " + i + " is not finite");
                Assert.That(h[i], Is.InRange(0f, 1f), what + " texel " + i);
            }
        }

        [Test]
        public void EveryKindIsInRangeTileableAndSized()
        {
            foreach (SurfaceKind kind in Kinds)
            {
                SurfaceFields f = SurfaceHeight.Build(kind, N);
                Assert.That(f.Size, Is.EqualTo(N), kind.ToString());
                Assert.That(f.Height.Length, Is.EqualTo(N * N), kind.ToString());
                Assert.That(f.Tone.Length, Is.EqualTo(N * N), kind.ToString());
                AssertUnit(f.Height, kind + " height");
                AssertUnit(f.Tone, kind + " tone");
                if (f.Mask != null) AssertUnit(f.Mask, kind + " mask");
                if (f.Aux != null) AssertUnit(f.Aux, kind + " aux");
                Assert.That(HeightField.SeamExcess(f.Height, N), Is.LessThan(0.1f), kind + " height does not tile");
                Assert.That(HeightField.SeamExcess(f.Tone, N), Is.LessThan(0.1f), kind + " tone does not tile");
                float mean = HeightField.Mean(f.Height);
                Assert.That(mean, Is.InRange(0.15f, 0.9f), kind + " height mean");
            }
        }

        [Test]
        public void EveryKindIsDeterministicAndSizeIndependent()
        {
            foreach (SurfaceKind kind in Kinds)
            {
                SurfaceFields a = SurfaceHeight.Build(kind, N);
                SurfaceFields b = SurfaceHeight.Build(kind, N);
                for (int i = 0; i < a.Height.Length; i += 7) Assert.That(b.Height[i], Is.EqualTo(a.Height[i]), kind + " is not deterministic");
                // Twice the size keeps the same overall level (the relief gets finer, not brighter or darker).
                SurfaceFields big = SurfaceHeight.Build(kind, N * 2);
                Assert.That(HeightField.Mean(big.Height), Is.EqualTo(HeightField.Mean(a.Height)).Within(0.06f), kind + " changes level with size");
            }
        }

        [Test]
        public void LargeKindsAreTheGroundWallsAndFloors()
        {
            Assert.That(SurfaceHeight.IsLarge(SurfaceKind.Asphalt), Is.True);
            Assert.That(SurfaceHeight.IsLarge(SurfaceKind.UhRock), Is.True);
            Assert.That(SurfaceHeight.IsLarge(SurfaceKind.Brushed), Is.False);
            Assert.That(SurfaceHeight.IsLarge(SurfaceKind.PaintPeel), Is.False);
            Assert.That(SurfaceHeight.IsLarge(SurfaceKind.Wood), Is.False);
        }

        [Test]
        public void NoiseIsTileableInRangeAndSeeded()
        {
            float[] a = HeightField.Fbm(N, 4, 4, 11);
            float[] b = HeightField.Fbm(N, 4, 4, 12);
            AssertUnit(a, "fbm");
            Assert.That(HeightField.SeamExcess(a, N), Is.LessThan(0.02f));
            float diff = 0f;
            for (int i = 0; i < a.Length; i++) diff += Mathf.Abs(a[i] - b[i]);
            Assert.That(diff / a.Length, Is.GreaterThan(0.05f), "different seeds give the same noise");
            float[] stretched = HeightField.Fbm(N, 4, 3, 5, 0.5f, 4f, 0.5f);
            Assert.That(HeightField.SeamExcess(stretched, N), Is.LessThan(0.02f), "stretched noise does not tile");
            float[] grain = HeightField.Grain(N, 3);
            AssertUnit(grain, "grain");
            Assert.That(HeightField.Mean(grain), Is.EqualTo(0.5f).Within(0.05f));
        }

        [Test]
        public void CellsBlurAndResampleKeepTiling()
        {
            float[] f1 = new float[N * N];
            float[] edge = new float[N * N];
            float[] id = new float[N * N];
            HeightField.Cells(N, 6, 9, 1f, f1, edge, id);
            for (int i = 0; i < f1.Length; i++)
            {
                Assert.That(f1[i], Is.InRange(0f, 1.5f));
                Assert.That(edge[i], Is.GreaterThanOrEqualTo(-1e-4f));
                Assert.That(id[i], Is.InRange(0f, 1f));
            }
            Assert.That(HeightField.SeamExcess(f1, N), Is.LessThan(0.05f), "cell distance does not tile");
            float[] h = HeightField.Fbm(N, 4, 3, 21);
            float[] blurred = HeightField.Blur(h, N, 3);
            Assert.That(HeightField.Mean(blurred), Is.EqualTo(HeightField.Mean(h)).Within(1e-3f), "blur changes the mean");
            Assert.That(HeightField.SeamExcess(blurred, N), Is.LessThan(0.02f));
            float variance = 0f, blurredVariance = 0f;
            float mean = HeightField.Mean(h);
            for (int i = 0; i < h.Length; i++)
            {
                variance += (h[i] - mean) * (h[i] - mean);
                blurredVariance += (blurred[i] - mean) * (blurred[i] - mean);
            }
            Assert.That(blurredVariance, Is.LessThan(variance), "blur does not smooth");
            float[] up = HeightField.Resample(h, N, N * 2);
            Assert.That(up.Length, Is.EqualTo(N * N * 4));
            Assert.That(HeightField.Mean(up), Is.EqualTo(mean).Within(0.01f));
            Assert.That(HeightField.SeamExcess(up, N * 2), Is.LessThan(0.02f));
            Assert.That(HeightField.Sample(h, N, 0.5f / N, 0.5f / N), Is.EqualTo(h[0]).Within(1e-4f), "texel centre sample");
        }

        [Test]
        public void SeamExcessCatchesANonTilingPattern()
        {
            float[] bad = new float[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++) bad[y * N + x] = (x + 0.5f) / N;
            Assert.That(HeightField.SeamExcess(bad, N), Is.GreaterThan(0.5f));
            float[] good = new float[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++) good[y * N + x] = 0.5f + 0.5f * Mathf.Sin((x + 0.5f) / N * Mathf.PI * 4f);
            Assert.That(HeightField.SeamExcess(good, N), Is.LessThan(0.01f));
        }

        [Test]
        public void NormalsDecodeToUnitVectorsAndFlatIsFlat()
        {
            float[] flat = HeightField.Flat(N, 0.4f);
            Color32[] n = SurfaceMaps.Normal(flat, N, 3f);
            Assert.That(n[0].r, Is.EqualTo(127).Within(1));
            Assert.That(n[0].g, Is.EqualTo(127).Within(1));
            Assert.That(n[0].b, Is.EqualTo(255));
            Assert.That(n[0].a, Is.EqualTo(255));
            foreach (SurfaceKind kind in Kinds)
            {
                SurfaceFields f = SurfaceHeight.Build(kind, N);
                Color32[] px = SurfaceMaps.Normal(f.Height, N, SurfaceColor.NormalStrength(kind, N));
                for (int i = 0; i < px.Length; i += 5)
                {
                    Vector3 v = SurfaceMaps.Decode(px[i]);
                    Assert.That(v.magnitude, Is.EqualTo(1f).Within(0.03f), kind + " normal " + i);
                    Assert.That(v.z, Is.GreaterThan(0.2f), kind + " normal " + i + " points sideways");
                }
            }
            // A ramp rising along x tilts the normal toward negative x.
            float[] ramp = new float[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++) ramp[y * N + x] = 0.5f + 0.4f * Mathf.Sin((x + 0.5f) / N * Mathf.PI * 2f);
            Color32[] r = SurfaceMaps.Normal(ramp, N, 4f);
            Assert.That(SurfaceMaps.Decode(r[2]).x, Is.LessThan(-0.05f));
        }

        [Test]
        public void CavityAndConvexityFollowTheRelief()
        {
            float[] flat = HeightField.Flat(N, 0.5f);
            SurfaceShade s = SurfaceMaps.Shade(flat, N, 6f);
            Assert.That(HeightField.Mean(s.Cavity), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(HeightField.Mean(s.Convex), Is.EqualTo(0f).Within(1e-5f));
            float[] pit = HeightField.Flat(N, 0.6f);
            int c = (N / 2) * N + N / 2;
            pit[c] = 0.1f;
            pit[c + 1] = 0.1f;
            pit[c + N] = 0.1f;
            pit[c + N + 1] = 0.1f;
            SurfaceShade p = SurfaceMaps.Shade(pit, N, 6f);
            Assert.That(p.Cavity[c], Is.GreaterThan(0.3f), "the pit is a cavity");
            Assert.That(p.Convex[c], Is.EqualTo(0f).Within(1e-5f), "the pit is not an edge");
            Assert.That(p.Cavity[5], Is.EqualTo(0f).Within(1e-4f), "far texels are open");
            Assert.That(p.Convex[c - 1], Is.GreaterThan(0f), "the pit's rim is convex");
            Assert.That(SurfaceMaps.Occlusion(0f, 0.85f), Is.EqualTo(1f));
            Assert.That(SurfaceMaps.Occlusion(1f, 0.85f), Is.EqualTo(0.15f).Within(1e-5f));
        }

        [Test]
        public void MaskPacksItsChannels()
        {
            float[] metal = { 0f, 1f, 0.5f };
            float[] occ = { 1f, 0.5f, 0f };
            float[] smooth = { 0.25f, 0.75f, 1f };
            Color32[] px = SurfaceMaps.PackMask(3, metal, 0f, occ, smooth, 0f);
            Assert.That(px[1].r, Is.EqualTo(255));
            Assert.That(px[1].g, Is.EqualTo(128).Within(1));
            Assert.That(px[1].a, Is.EqualTo(191).Within(1));
            Assert.That(px[0].a, Is.EqualTo(64).Within(1));
            Color32[] constant = SurfaceMaps.PackMask(2, null, 0.6f, null, null, 0.3f);
            Assert.That(constant[1].r, Is.EqualTo(153).Within(1));
            Assert.That(constant[1].g, Is.EqualTo(255));
            Assert.That(constant[1].a, Is.EqualTo(77).Within(1));
        }

        [Test]
        public void WetSmoothnessNeverExceedsOneUnderTheRainScale()
        {
            foreach (SurfaceKind kind in Kinds)
            {
                SurfaceFields f = SurfaceHeight.Build(kind, N);
                SurfaceShade s = SurfaceMaps.Shade(f.Height, N, SurfaceColor.ShadeGain(kind));
                float[] variation = SurfaceColor.GlossVariation(kind, f, s);
                foreach (float nominal in new[] { 0.12f, 0.18f, 0.3f })
                {
                    float[] wet = SurfaceColor.Smoothness(nominal, variation, true);
                    float scale = SurfaceColor.WetTarget / nominal;
                    float dryMax = 0f;
                    for (int i = 0; i < wet.Length; i++)
                    {
                        Assert.That(wet[i] * scale, Is.LessThanOrEqualTo(1.0001f), kind + " at " + nominal + " texel " + i);
                        if (wet[i] > dryMax) dryMax = wet[i];
                    }
                    Assert.That(dryMax, Is.GreaterThan(0f));
                }
                float[] dry = SurfaceColor.Smoothness(0.5f, variation, false);
                AssertUnit(dry, kind + " dry smoothness");
                float[] metal = SurfaceColor.MetallicVariation(kind, f, 0.7f);
                if (metal != null) AssertUnit(metal, kind + " metallic");
            }
        }

        [Test]
        public void AlbedoIsOpaqueFiniteAndMicroKindsStayNearWhite()
        {
            foreach (SurfaceKind kind in Kinds)
            {
                SurfaceFields f = SurfaceHeight.Build(kind, N);
                SurfaceShade s = SurfaceMaps.Shade(f.Height, N, SurfaceColor.ShadeGain(kind));
                int palettes = kind == SurfaceKind.UhRock ? 5 : (kind == SurfaceKind.Rock ? 4 : (kind == SurfaceKind.Concrete ? 3 : 1));
                for (int palette = 0; palette < palettes; palette++)
                {
                    Color32[] px = SurfaceColor.Albedo(kind, f, s, palette);
                    Assert.That(px.Length, Is.EqualTo(N * N));
                    double sum = 0.0;
                    for (int i = 0; i < px.Length; i++)
                    {
                        Assert.That(px[i].a, Is.EqualTo(255), kind + " alpha");
                        sum += (px[i].r + px[i].g + px[i].b) / 3.0;
                    }
                    float mean = (float)(sum / px.Length) / 255f;
                    Assert.That(mean, Is.InRange(0.02f, 1f), kind + " palette " + palette + " mean");
                    bool micro = kind == SurfaceKind.Brushed || kind == SurfaceKind.Grain || kind == SurfaceKind.Pores || kind == SurfaceKind.Fibre || kind == SurfaceKind.Leaf || kind == SurfaceKind.Ripples;
                    if (micro) Assert.That(mean, Is.GreaterThan(0.75f), kind + " should stay near white so the material colour tints it");
                }
            }
        }

        [Test]
        public void PaintGrimeSitsAtTheSill()
        {
            SurfaceFields paint = SurfaceHeight.Build(SurfaceKind.PaintPeel, N);
            float bottom = 0f, top = 0f;
            for (int x = 0; x < N; x++)
            {
                bottom += paint.Mask[x];
                top += paint.Mask[(N - 1) * N + x];
            }
            Assert.That(bottom / N, Is.GreaterThan(0.4f), "grime at the bottom of the panel");
            Assert.That(top / N, Is.EqualTo(0f).Within(1e-4f), "no grime at the top");
        }

        [Test]
        public void FacadeReliefRecessesTheWindows()
        {
            SurfaceFields f = SurfaceHeight.Facade(N, 0.6f, 0.5f, false, false, 5);
            float windows = 0f, walls = 0f;
            int wc = 0, nc = 0;
            for (int i = 0; i < f.Length; i++)
            {
                if (f.Mask[i] > 0.5f) { windows += f.Height[i]; wc++; }
                else { walls += f.Height[i]; nc++; }
            }
            Assert.That(wc, Is.GreaterThan(0));
            Assert.That(nc, Is.GreaterThan(0));
            Assert.That(windows / wc, Is.LessThan(walls / nc - 0.15f), "windows sit behind the wall");
            Assert.That(HeightField.SeamExcess(f.Height, N), Is.LessThan(0.1f));
            SurfaceFields brick = SurfaceHeight.Facade(N * 2, 0.4f, 0.55f, false, true, 9);
            Assert.That(HeightField.SeamExcess(brick.Height, N * 2), Is.LessThan(0.1f));
            SurfaceFields glass = SurfaceHeight.Facade(N, 0.92f, 0.8f, true, false, 1);
            Assert.That(HeightField.Mean(glass.Mask), Is.GreaterThan(HeightField.Mean(f.Mask)), "a glass curtain is mostly window");
        }

        [Test]
        public void FlakeNormalsAreUnitAndMostlyFlat()
        {
            Color32[] px = SurfaceMaps.FlakeNormal(N, 2, 0.35f, 0.45f, 71);
            int flat = 0;
            for (int i = 0; i < px.Length; i++)
            {
                Vector3 v = SurfaceMaps.Decode(px[i]);
                Assert.That(v.magnitude, Is.EqualTo(1f).Within(0.03f));
                if (px[i].b == 255) flat++;
            }
            Assert.That(flat, Is.GreaterThan(px.Length / 3), "most of the clearcoat is flat between flakes");
            Assert.That(flat, Is.LessThan(px.Length), "some flakes tilt");
        }

        [Test]
        public void TextureSizesFollowThePresets()
        {
            Assert.That(Tex.PbrSizeFor(GraphicsPreset.Medium, SurfaceKind.Asphalt), Is.EqualTo(256));
            Assert.That(Tex.PbrSizeFor(GraphicsPreset.Medium, SurfaceKind.Brushed), Is.EqualTo(128));
            Assert.That(Tex.PbrSizeFor(GraphicsPreset.High, SurfaceKind.Asphalt), Is.EqualTo(512));
            Assert.That(Tex.PbrSizeFor(GraphicsPreset.High, SurfaceKind.Wood), Is.EqualTo(256));
            Assert.That(Tex.PbrSizeFor(GraphicsPreset.Ultra, SurfaceKind.Asphalt), Is.EqualTo(1024));
            Assert.That(Tex.PbrSizeFor(GraphicsPreset.Ultra, SurfaceKind.UhTile), Is.EqualTo(512));
            Assert.That(Tex.PbrSizeFor(GraphicsPreset.Ultra, SurfaceKind.Grain), Is.EqualTo(256));
            Assert.That(Tex.FacadeReliefSizeFor(GraphicsPreset.Medium), Is.EqualTo(256));
            Assert.That(Tex.FacadeReliefSizeFor(GraphicsPreset.Ultra), Is.EqualTo(512));
            Assert.That(Tex.SizeCapForCores(2), Is.EqualTo(256));
            Assert.That(Tex.SizeCapForCores(4), Is.EqualTo(512));
            Assert.That(Tex.SizeCapForCores(8), Is.EqualTo(1024));
            Assert.That(Tex.PbrSizeFor(GraphicsPreset.Ultra, SurfaceKind.Asphalt, 512), Is.EqualTo(512));
            Assert.That(Tex.PbrSizeFor(GraphicsPreset.Medium, SurfaceKind.Grain, 256), Is.EqualTo(128));
            Assert.That(Mats.PbrLevelFor(GraphicsPreset.Low), Is.EqualTo(0));
            Assert.That(Mats.PbrLevelFor(GraphicsPreset.Medium), Is.EqualTo(1));
            Assert.That(Mats.PbrLevelFor(GraphicsPreset.High), Is.EqualTo(2));
            Assert.That(Mats.PbrLevelFor(GraphicsPreset.Ultra), Is.EqualTo(3));
            foreach (SurfaceKind kind in Kinds)
                foreach (GraphicsPreset p in System.Enum.GetValues(typeof(GraphicsPreset)))
                {
                    int size = Tex.PbrSizeFor(p, kind);
                    Assert.That(size, Is.InRange(128, 1024), kind + " " + p);
                    Assert.That(size & (size - 1), Is.EqualTo(0), kind + " " + p + " is not a power of two");
                }
        }

        [Test]
        public void HashIsInRangeAndVaries()
        {
            int distinct = 0;
            float last = -1f;
            for (int i = 0; i < 200; i++)
            {
                float h = HeightField.Hash01(i, i * 3, 7);
                Assert.That(h, Is.InRange(0f, 1f));
                if (Mathf.Abs(h - last) > 1e-6f) distinct++;
                last = h;
            }
            Assert.That(distinct, Is.GreaterThan(150));
            Assert.That(HeightField.Hash01(3, 4, 5), Is.EqualTo(HeightField.Hash01(3, 4, 5)));
            Assert.That(HeightField.Hash01(3, 4, 5), Is.Not.EqualTo(HeightField.Hash01(3, 4, 6)));
        }
    }
}
