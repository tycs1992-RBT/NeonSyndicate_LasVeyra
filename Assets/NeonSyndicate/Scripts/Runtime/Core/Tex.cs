using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Procedural texture factory (tileable value noise, patterns, decals, facades). All results are cached.
    /// </summary>
    public static partial class Tex
    {
        private static readonly Dictionary<string, Texture2D> _cache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<Texture2D, Texture2D> _normals = new Dictionary<Texture2D, Texture2D>();

        public static void ResetStatics()
        {
            _cache.Clear();
            _normals.Clear();
        }

        // ------------------------------------------------------------------ noise

        private static float Lattice(int x, int y, int period, int seed)
        {
            if (period > 0)
            {
                x = ((x % period) + period) % period;
                y = ((y % period) + period) % period;
            }
            return MathUtil.Hash01(x * 7919 + seed * 104729, y * 6271 + seed * 15485863);
        }

        private static float Smooth(float t)
        {
            return t * t * (3f - 2f * t);
        }

        /// <summary>Tileable value noise in [0,1]. Coordinates in lattice units; period in lattice cells (0 = not tiling).</summary>
        public static float Noise(float x, float y, int period, int seed)
        {
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float fx = Smooth(x - x0);
            float fy = Smooth(y - y0);
            float a = Lattice(x0, y0, period, seed);
            float b = Lattice(x0 + 1, y0, period, seed);
            float c = Lattice(x0, y0 + 1, period, seed);
            float d = Lattice(x0 + 1, y0 + 1, period, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        /// <summary>Fractal tileable noise. u,v in [0,1); baseCells lattice cells across the texture.</summary>
        public static float Fbm(float u, float v, int baseCells, int octaves, int seed)
        {
            float total = 0f;
            float amp = 0.5f;
            float norm = 0f;
            int cells = baseCells;
            for (int i = 0; i < octaves; i++)
            {
                total += Noise(u * cells, v * cells, cells, seed + i * 31) * amp;
                norm += amp;
                amp *= 0.5f;
                cells *= 2;
            }
            return total / Mathf.Max(0.0001f, norm);
        }

        private static Color32 C(Color c)
        {
            return new Color32((byte)(Mathf.Clamp01(c.r) * 255f), (byte)(Mathf.Clamp01(c.g) * 255f), (byte)(Mathf.Clamp01(c.b) * 255f), (byte)(Mathf.Clamp01(c.a) * 255f));
        }

        private delegate Color PixelFn(int x, int y, float u, float v);

        private static Texture2D Make(string key, int w, int h, bool mips, PixelFn fn)
        {
            Texture2D cached;
            if (_cache.TryGetValue(key, out cached) && cached != null) return cached;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, mips);
            t.name = key;
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = mips ? FilterMode.Trilinear : FilterMode.Bilinear;
            t.anisoLevel = 4;
            Color32[] px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float v = (y + 0.5f) / h;
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w;
                    px[y * w + x] = C(fn(x, y, u, v));
                }
            }
            t.SetPixels32(px);
            t.Apply(mips, false);
            _cache[key] = t;
            return t;
        }

        public static Texture2D NormalFrom(Texture2D source, float strength)
        {
            if (source == null) return null;
            Texture2D existing;
            if (_normals.TryGetValue(source, out existing) && existing != null) return existing;
            int w = source.width;
            int h = source.height;
            Color32[] src = source.GetPixels32();
            Color32[] dst = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float l = Lum(src[y * w + ((x - 1 + w) % w)]);
                    float r = Lum(src[y * w + ((x + 1) % w)]);
                    float d = Lum(src[((y - 1 + h) % h) * w + x]);
                    float u = Lum(src[((y + 1) % h) * w + x]);
                    Vector3 n = new Vector3((l - r) * strength, (d - u) * strength, 1f).normalized;
                    dst[y * w + x] = new Color32((byte)((n.x * 0.5f + 0.5f) * 255f), (byte)((n.y * 0.5f + 0.5f) * 255f), (byte)((n.z * 0.5f + 0.5f) * 255f), 255);
                }
            }
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, true, true);
            t.name = source.name + "_N";
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Trilinear;
            t.SetPixels32(dst);
            t.Apply(true, false);
            _normals[source] = t;
            return t;
        }

        private static float Lum(Color32 c)
        {
            return (c.r * 0.299f + c.g * 0.587f + c.b * 0.114f) / 255f;
        }

        // ------------------------------------------------------------------ ground

        public static Texture2D Asphalt()
        {
            return Make("asphalt", 256, 256, true, (x, y, u, v) =>
            {
                float n = Fbm(u, v, 8, 5, 11);
                float speck = MathUtil.Hash01(x, y * 3 + 7);
                float g = 0.13f + n * 0.09f;
                if (speck > 0.985f) g += 0.12f;
                else if (speck < 0.02f) g -= 0.04f;
                float crack = Mathf.Abs(Fbm(u * 0.5f + 0.3f, v * 0.5f, 4, 3, 77) - 0.5f);
                if (crack < 0.012f) g *= 0.6f;
                return new Color(g, g, g * 1.04f, 1f);
            });
        }

        public static Texture2D Pavers()
        {
            return Make("pavers", 256, 256, true, (x, y, u, v) =>
            {
                float n = Fbm(u, v, 8, 4, 21);
                float tu = Mathf.Repeat(u * 4f, 1f);
                float tv = Mathf.Repeat(v * 4f, 1f);
                bool joint = tu < 0.03f || tv < 0.03f;
                int tile = Mathf.FloorToInt(u * 4f) * 13 + Mathf.FloorToInt(v * 4f) * 7;
                float tone = 0.52f + MathUtil.Hash01(tile, 3) * 0.08f + n * 0.08f;
                if (joint) tone *= 0.55f;
                // Hairline cracks and grime patches, like a sun-baked city sidewalk.
                float crack = Mathf.Abs(Fbm(u * 0.75f + 0.31f, v * 0.75f, 3, 4, 91) - 0.5f);
                if (crack < 0.007f) tone *= 0.58f;
                float grime = Fbm(u, v, 3, 3, 93);
                if (grime > 0.62f) tone *= 1f - (grime - 0.62f) * 0.5f;
                return new Color(tone, tone * 0.98f, tone * 0.94f, 1f);
            });
        }

        /// <summary>
        /// Weed tuft cards: left half green weeds, right half dry straw grass, tuft base at v = 0. Transparent pixels
        /// carry the nearest blade color and every mip level gets a little more alpha, so cutout tufts keep their
        /// body at a distance instead of dissolving.
        /// </summary>
        public static Texture2D Weeds()
        {
            Texture2D cached;
            if (_cache.TryGetValue("weeds", out cached) && cached != null) return cached;
            const int S = 128, W = S * 2;
            float[] cr = new float[W * S], cg = new float[W * S], cb = new float[W * S], ca = new float[W * S];
            GRand r = new GRand(77);
            for (int variant = 0; variant < 2; variant++)
            {
                float cx = S * (0.5f + variant);
                bool dry = variant == 1;
                int blades = dry ? 56 : 64;
                for (int i = 0; i < blades; i++)
                {
                    float x0 = cx + r.Range(-0.24f, 0.24f) * S;
                    float len = S * r.Range(0.32f, 0.92f);
                    float lean = (x0 - cx) / S * r.Range(0.8f, dry ? 2.2f : 1.8f) + r.Range(-0.25f, 0.25f);
                    if (dry && r.Chance(0.12f)) lean *= 2.2f;
                    float width = r.Range(0.9f, 1.7f);
                    Color col = dry
                        ? new Color(r.Range(0.6f, 0.72f), r.Range(0.52f, 0.62f), r.Range(0.32f, 0.4f))
                        : new Color(r.Range(0.28f, 0.42f), r.Range(0.42f, 0.52f), r.Range(0.14f, 0.21f));
                    StrokeBlade(cr, cg, cb, ca, W, S, x0, len, lean, width, col);
                }
                for (int i = 0; i < 5; i++)
                {
                    // Seed heads: small flowers on the green weeds, foxtails on the dry grass.
                    float x0 = cx + r.Range(-0.14f, 0.14f) * S;
                    float len = S * r.Range(0.68f, 0.95f);
                    float lean = r.Range(-0.28f, 0.28f);
                    StrokeBlade(cr, cg, cb, ca, W, S, x0, len, lean, 0.6f, dry ? new Color(0.66f, 0.58f, 0.38f) : new Color(0.38f, 0.46f, 0.22f));
                    Color head = dry ? new Color(0.77f, 0.7f, 0.5f) : (i % 2 == 0 ? new Color(0.88f, 0.75f, 0.24f) : new Color(0.92f, 0.91f, 0.84f));
                    Stamp(cr, cg, cb, ca, W, S, x0 + lean * len, len * 0.97f, dry ? 1.6f : 2.4f, head, dry ? 4f : 1f);
                }
            }
            // Darken toward the base (self shadowing inside the tuft).
            for (int y = 0; y < S; y++)
            {
                float shade = 0.5f + 0.5f * Mathf.Pow(y / (float)S, 0.6f);
                for (int x = 0; x < W; x++) { int i = y * W + x; cr[i] *= shade; cg[i] *= shade; cb[i] *= shade; }
            }
            // Bleed blade colors outward into the transparent area.
            bool[] has = new bool[W * S];
            for (int i = 0; i < has.Length; i++) has[i] = ca[i] > 0.03f;
            for (int pass = 0; pass < 10; pass++)
            {
                bool[] next = (bool[])has.Clone();
                for (int y = 0; y < S; y++)
                {
                    for (int x = 0; x < W; x++)
                    {
                        int i = y * W + x;
                        if (has[i]) continue;
                        float sr = 0f, sg = 0f, sb = 0f; int n = 0;
                        if (x > 0 && has[i - 1]) { sr += cr[i - 1]; sg += cg[i - 1]; sb += cb[i - 1]; n++; }
                        if (x < W - 1 && has[i + 1]) { sr += cr[i + 1]; sg += cg[i + 1]; sb += cb[i + 1]; n++; }
                        if (y > 0 && has[i - W]) { sr += cr[i - W]; sg += cg[i - W]; sb += cb[i - W]; n++; }
                        if (y < S - 1 && has[i + W]) { sr += cr[i + W]; sg += cg[i + W]; sb += cb[i + W]; n++; }
                        if (n == 0) continue;
                        cr[i] = sr / n; cg[i] = sg / n; cb[i] = sb / n;
                        next[i] = true;
                    }
                }
                has = next;
            }
            Texture2D t = new Texture2D(W, S, TextureFormat.RGBA32, true);
            t.name = "weeds";
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Trilinear;
            t.anisoLevel = 4;
            int w = W, h = S;
            for (int level = 0; level < t.mipmapCount; level++)
            {
                Color32[] px = new Color32[w * h];
                float boost = 1f + level * 0.3f;
                for (int i = 0; i < px.Length; i++) px[i] = C(new Color(cr[i], cg[i], cb[i], Mathf.Clamp01(ca[i] * boost)));
                t.SetPixels32(px, level);
                if (w == 1 && h == 1) break;
                // Box-filter down to the next level.
                int nw = Mathf.Max(1, w / 2), nh = Mathf.Max(1, h / 2);
                float[] nr = new float[nw * nh], ng = new float[nw * nh], nb = new float[nw * nh], na = new float[nw * nh];
                for (int y = 0; y < nh; y++)
                {
                    for (int x = 0; x < nw; x++)
                    {
                        int o = y * nw + x;
                        for (int dy = 0; dy < 2; dy++)
                        {
                            for (int dx = 0; dx < 2; dx++)
                            {
                                int sx = Mathf.Min(w - 1, x * 2 + dx), sy = Mathf.Min(h - 1, y * 2 + dy);
                                int i = sy * w + sx;
                                nr[o] += cr[i] * 0.25f; ng[o] += cg[i] * 0.25f; nb[o] += cb[i] * 0.25f; na[o] += ca[i] * 0.25f;
                            }
                        }
                    }
                }
                cr = nr; cg = ng; cb = nb; ca = na; w = nw; h = nh;
            }
            t.Apply(false, false);
            _cache["weeds"] = t;
            return t;
        }

        // A tapered blade from (x0, 0) curving toward its tip, stamped as overlapping soft discs.
        private static void StrokeBlade(float[] cr, float[] cg, float[] cb, float[] ca, int W, int S, float x0, float len, float lean, float width, Color col)
        {
            float tipX = x0 + lean * len, tipY = len * 0.98f;
            float ctlX = x0 + lean * len * 0.25f, ctlY = len * 0.55f;
            int steps = Mathf.Max(8, Mathf.CeilToInt(len * 1.5f));
            for (int k = 0; k <= steps; k++)
            {
                float t = k / (float)steps, it = 1f - t;
                float x = it * it * x0 + 2f * it * t * ctlX + t * t * tipX;
                float y = 2f * it * t * ctlY + t * t * tipY;
                Stamp(cr, cg, cb, ca, W, S, x, y, width * (1f - t) + 0.35f, col, 1f);
            }
        }

        private static void Stamp(float[] cr, float[] cg, float[] cb, float[] ca, int W, int S, float px, float py, float radius, Color col, float stretchY)
        {
            int x0 = Mathf.FloorToInt(px - radius - 1f), x1 = Mathf.CeilToInt(px + radius + 1f);
            int y0 = Mathf.FloorToInt(py - radius * stretchY - 1f), y1 = Mathf.CeilToInt(py + radius * stretchY + 1f);
            for (int y = Mathf.Max(0, y0); y <= Mathf.Min(S - 1, y1); y++)
            {
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(W - 1, x1); x++)
                {
                    float dx = x + 0.5f - px, dy = (y + 0.5f - py) / stretchY;
                    float a = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    if (a <= 0f) continue;
                    int i = y * W + x;
                    if (a >= ca[i]) { cr[i] = col.r; cg[i] = col.g; cb[i] = col.b; }
                    ca[i] = Mathf.Max(ca[i], a);
                }
            }
        }

        public static Texture2D Concrete()
        {
            return Make("concrete", 256, 256, true, (x, y, u, v) =>
            {
                float n = Fbm(u, v, 6, 5, 31);
                float fine = MathUtil.Hash01(x * 5, y * 11) * 0.05f;
                float g = 0.48f + n * 0.14f + fine;
                if (Mathf.Repeat(v * 2f, 1f) < 0.01f) g *= 0.75f;
                return new Color(g, g * 0.99f, g * 0.96f, 1f);
            });
        }

        public static Texture2D Grass()
        {
            return Make("grass", 256, 256, true, (x, y, u, v) =>
            {
                float n = Fbm(u, v, 8, 5, 41);
                float blade = MathUtil.Hash01(x * 3, y);
                Color a = new Color(0.12f, 0.30f, 0.10f);
                Color b = new Color(0.28f, 0.45f, 0.16f);
                Color c = Color.Lerp(a, b, n);
                if (blade > 0.9f) c *= 1.2f;
                return c;
            });
        }

        public static Texture2D Sand()
        {
            return Make("sand", 256, 256, true, (x, y, u, v) =>
            {
                float n = Fbm(u, v, 6, 5, 51);
                float ripple = Mathf.Sin((v + n * 0.12f) * 60f) * 0.5f + 0.5f;
                Color a = new Color(0.72f, 0.58f, 0.40f);
                Color b = new Color(0.86f, 0.72f, 0.52f);
                Color c = Color.Lerp(a, b, n * 0.7f + ripple * 0.3f);
                if (MathUtil.Hash01(x, y + 99) > 0.992f) c *= 0.7f;
                return c;
            });
        }

        public static Texture2D Dirt()
        {
            return Make("dirt", 256, 256, true, (x, y, u, v) =>
            {
                float n = Fbm(u, v, 7, 5, 61);
                Color c = Color.Lerp(new Color(0.36f, 0.27f, 0.18f), new Color(0.55f, 0.43f, 0.30f), n);
                if (MathUtil.Hash01(x * 2, y) > 0.97f) c *= 0.75f;
                return c;
            });
        }

        public static Texture2D Rock()
        {
            return Make("rock", 256, 256, true, (x, y, u, v) =>
            {
                float n = Fbm(u, v, 4, 6, 71);
                float strata = Mathf.Sin(v * 40f + n * 6f) * 0.5f + 0.5f;
                Color c = Color.Lerp(new Color(0.48f, 0.30f, 0.20f), new Color(0.72f, 0.50f, 0.34f), n * 0.6f + strata * 0.4f);
                return c;
            });
        }

        public static Texture2D Gravel()
        {
            return Make("gravel", 128, 128, true, (x, y, u, v) =>
            {
                float n = Fbm(u, v, 16, 3, 81);
                float g = 0.18f + n * 0.12f + MathUtil.Hash01(x, y) * 0.06f;
                return new Color(g, g, g * 1.02f, 1f);
            });
        }

        public static Texture2D RoofTiles()
        {
            return Make("rooftiles", 128, 128, true, (x, y, u, v) =>
            {
                float row = Mathf.Repeat(v * 8f, 1f);
                float col = Mathf.Repeat(u * 6f + (Mathf.FloorToInt(v * 8f) % 2) * 0.5f, 1f);
                float shade = 0.55f + row * 0.35f;
                if (col < 0.06f) shade *= 0.7f;
                return new Color(0.62f * shade, 0.28f * shade, 0.18f * shade, 1f);
            });
        }

        public static Texture2D Corrugated()
        {
            return Make("corrugated", 128, 128, true, (x, y, u, v) =>
            {
                float ridge = Mathf.Sin(u * Mathf.PI * 32f) * 0.5f + 0.5f;
                float n = Fbm(u, v, 4, 3, 91);
                float g = 0.38f + ridge * 0.18f + n * 0.1f;
                float rust = Fbm(u, v, 8, 4, 92);
                Color c = new Color(g, g, g * 1.03f);
                if (rust > 0.68f) c = Color.Lerp(c, new Color(0.45f, 0.22f, 0.1f), (rust - 0.68f) * 2.5f);
                return c;
            });
        }

        public static Texture2D Wood()
        {
            return Make("wood", 128, 128, true, (x, y, u, v) =>
            {
                float plank = Mathf.Repeat(u * 5f, 1f);
                float grain = Fbm(u * 0.3f, v, 8, 4, 101);
                float g = 0.4f + grain * 0.25f;
                if (plank < 0.05f) g *= 0.5f;
                return new Color(g * 0.85f, g * 0.6f, g * 0.38f, 1f);
            });
        }

        public static Texture2D Bark()
        {
            return Make("bark", 64, 128, true, (x, y, u, v) =>
            {
                float ring = Mathf.Repeat(v * 16f, 1f);
                float n = Fbm(u, v, 4, 3, 111);
                float g = 0.32f + n * 0.12f;
                if (ring < 0.15f) g *= 0.7f;
                return new Color(g * 1.1f, g * 0.85f, g * 0.6f, 1f);
            });
        }

        public static Texture2D Billboard()
        {
            return Make("billboard", 256, 128, true, (x, y, u, v) =>
            {
                int panel = Mathf.FloorToInt(u * 4f);
                Color bg = Mats.NeonColors[(panel * 3 + 1) % Mats.NeonColors.Length] * 0.55f;
                float lu = Mathf.Repeat(u * 4f, 1f);
                float circle = new Vector2(lu - 0.35f, v - 0.55f).magnitude;
                Color c = bg;
                if (circle < 0.22f) c = Color.Lerp(Color.white, Mats.NeonColors[(panel * 5 + 2) % Mats.NeonColors.Length], 0.4f);
                if (v < 0.25f && lu > 0.1f && lu < 0.9f) c = new Color(0.95f, 0.95f, 0.95f);
                if (v < 0.25f && Mathf.Repeat(lu * 18f, 1f) < 0.35f && v > 0.08f && v < 0.18f) c = new Color(0.1f, 0.1f, 0.12f);
                if (lu < 0.02f || lu > 0.98f || v < 0.02f || v > 0.98f) c = new Color(0.08f, 0.08f, 0.08f);
                c.a = 1f;
                return c;
            });
        }

        public static Texture2D Storefront()
        {
            return Make("storefront", 256, 128, true, (x, y, u, v) =>
            {
                float pane = Mathf.Repeat(u * 4f, 1f);
                bool frame = pane < 0.05f || v < 0.08f || v > 0.9f || (v > 0.68f && v < 0.72f);
                if (frame) return new Color(0.12f, 0.12f, 0.13f, 1f);
                int idx = Mathf.FloorToInt(u * 4f);
                float lit = MathUtil.Hash01(idx, 17);
                Color inner = Color.Lerp(new Color(0.2f, 0.25f, 0.28f), new Color(0.75f, 0.68f, 0.5f), lit * 0.6f);
                inner += new Color(0.1f, 0.12f, 0.14f) * (1f - v);
                inner.a = 1f;
                return inner;
            });
        }

        public static Texture2D StorefrontGlow()
        {
            return Make("storefrontglow", 256, 128, true, (x, y, u, v) =>
            {
                float pane = Mathf.Repeat(u * 4f, 1f);
                bool frame = pane < 0.05f || v < 0.08f || v > 0.9f || (v > 0.68f && v < 0.72f);
                if (frame) return Color.black;
                int idx = Mathf.FloorToInt(u * 4f);
                float lit = MathUtil.Hash01(idx, 17);
                float g = 0.35f + lit * 0.65f;
                return new Color(g, g * 0.92f, g * 0.75f, 1f);
            });
        }

        /// <summary>A facade texture covering 16 m wide by 14 m tall (4 window bays by 4 floors).</summary>
        public static void Facade(FacadeStyle style, out Texture2D albedo, out Texture2D glow)
        {
            string key = "facade_" + style;
            Texture2D a;
            Texture2D g;
            if (_cache.TryGetValue(key, out a) && _cache.TryGetValue(key + "_glow", out g) && a != null && g != null)
            {
                albedo = a;
                glow = g;
                return;
            }

            Color wall;
            Color window;
            Color frameColor;
            float winW;
            float winH;
            float litChance;
            bool glassCurtain = false;
            switch (style)
            {
                case FacadeStyle.OfficeGlass: wall = new Color(0.28f, 0.42f, 0.5f); window = new Color(0.15f, 0.32f, 0.42f); frameColor = new Color(0.55f, 0.6f, 0.65f); winW = 0.92f; winH = 0.8f; litChance = 0.45f; glassCurtain = true; break;
                case FacadeStyle.DarkGlass: wall = new Color(0.06f, 0.07f, 0.09f); window = new Color(0.05f, 0.08f, 0.12f); frameColor = new Color(0.15f, 0.15f, 0.18f); winW = 0.94f; winH = 0.82f; litChance = 0.5f; glassCurtain = true; break;
                case FacadeStyle.GoldGlass: wall = new Color(0.55f, 0.42f, 0.2f); window = new Color(0.5f, 0.38f, 0.16f); frameColor = new Color(0.3f, 0.25f, 0.15f); winW = 0.9f; winH = 0.8f; litChance = 0.4f; glassCurtain = true; break;
                case FacadeStyle.Apartment: wall = new Color(0.62f, 0.6f, 0.55f); window = new Color(0.12f, 0.16f, 0.2f); frameColor = new Color(0.85f, 0.85f, 0.82f); winW = 0.45f; winH = 0.5f; litChance = 0.55f; break;
                case FacadeStyle.Brick: wall = new Color(0.48f, 0.2f, 0.14f); window = new Color(0.1f, 0.13f, 0.16f); frameColor = new Color(0.82f, 0.8f, 0.74f); winW = 0.4f; winH = 0.55f; litChance = 0.5f; break;
                case FacadeStyle.Stucco: wall = new Color(0.82f, 0.74f, 0.6f); window = new Color(0.14f, 0.18f, 0.2f); frameColor = new Color(0.55f, 0.42f, 0.3f); winW = 0.38f; winH = 0.45f; litChance = 0.45f; break;
                case FacadeStyle.StuccoPink: wall = new Color(0.85f, 0.62f, 0.6f); window = new Color(0.14f, 0.18f, 0.22f); frameColor = new Color(0.95f, 0.92f, 0.88f); winW = 0.4f; winH = 0.45f; litChance = 0.45f; break;
                case FacadeStyle.Warehouse: wall = new Color(0.45f, 0.45f, 0.46f); window = new Color(0.2f, 0.25f, 0.28f); frameColor = new Color(0.3f, 0.3f, 0.32f); winW = 0.6f; winH = 0.18f; litChance = 0.25f; break;
                case FacadeStyle.Casino: wall = new Color(0.42f, 0.1f, 0.38f); window = new Color(0.9f, 0.75f, 0.3f); frameColor = new Color(0.95f, 0.85f, 0.4f); winW = 0.8f; winH = 0.6f; litChance = 0.85f; break;
                case FacadeStyle.Hospital: wall = new Color(0.9f, 0.9f, 0.92f); window = new Color(0.2f, 0.32f, 0.42f); frameColor = new Color(0.7f, 0.72f, 0.75f); winW = 0.7f; winH = 0.5f; litChance = 0.7f; break;
                default: wall = new Color(0.18f, 0.24f, 0.42f); window = new Color(0.12f, 0.16f, 0.22f); frameColor = new Color(0.85f, 0.85f, 0.9f); winW = 0.6f; winH = 0.5f; litChance = 0.6f; break;
            }

            int seed = (int)style * 97 + 5;
            const int size = 256;
            Color32[] apx = new Color32[size * size];
            Color32[] gpx = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                float v = (y + 0.5f) / size;
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    int bay = Mathf.FloorToInt(u * 4f);
                    int floor = Mathf.FloorToInt(v * 4f);
                    float bu = Mathf.Repeat(u * 4f, 1f);
                    float bv = Mathf.Repeat(v * 4f, 1f);
                    float n = Fbm(u, v, 8, 3, seed);
                    bool inWindow = Mathf.Abs(bu - 0.5f) < winW * 0.5f && Mathf.Abs(bv - 0.55f) < winH * 0.5f;
                    bool edge = inWindow && (Mathf.Abs(Mathf.Abs(bu - 0.5f) - winW * 0.5f) < 0.025f || Mathf.Abs(Mathf.Abs(bv - 0.55f) - winH * 0.5f) < 0.03f);
                    Color col;
                    Color glowCol = Color.black;
                    if (inWindow && !edge)
                    {
                        float lit = MathUtil.Hash01(bay + seed * 3, floor * 17 + seed);
                        bool isLit = lit < litChance;
                        float refl = (1f - bv) * 0.25f + n * 0.15f;
                        col = window + new Color(refl, refl, refl * 1.1f) * (glassCurtain ? 1.3f : 0.7f);
                        if (isLit)
                        {
                            float warm = MathUtil.Hash01(bay * 5 + 1, floor * 3 + seed);
                            Color lc = warm > 0.7f ? new Color(0.7f, 0.85f, 1f) : new Color(1f, 0.85f, 0.55f);
                            float blinds = Mathf.Repeat(bv * 12f, 1f) < 0.12f && warm > 0.5f ? 0.6f : 1f;
                            glowCol = lc * (0.55f + warm * 0.45f) * blinds;
                        }
                    }
                    else if (edge)
                    {
                        col = frameColor;
                    }
                    else
                    {
                        col = wall * (0.88f + n * 0.24f);
                        if (style == FacadeStyle.Brick)
                        {
                            float rowB = Mathf.Repeat(v * 64f, 1f);
                            float colB = Mathf.Repeat(u * 32f + (Mathf.FloorToInt(v * 64f) % 2) * 0.5f, 1f);
                            if (rowB < 0.12f || colB < 0.06f) col = new Color(0.62f, 0.6f, 0.56f);
                        }
                        if (glassCurtain && (bu < 0.03f || bu > 0.97f)) col = frameColor;
                        if (bv < 0.04f) col *= 0.8f;
                    }
                    col.a = 1f;
                    glowCol.a = 1f;
                    apx[y * size + x] = C(col);
                    gpx[y * size + x] = C(glowCol);
                }
            }
            a = new Texture2D(size, size, TextureFormat.RGBA32, true);
            a.name = key;
            a.wrapMode = TextureWrapMode.Repeat;
            a.filterMode = FilterMode.Trilinear;
            a.anisoLevel = 6;
            a.SetPixels32(apx);
            a.Apply(true, false);
            g = new Texture2D(size, size, TextureFormat.RGBA32, true);
            g.name = key + "_glow";
            g.wrapMode = TextureWrapMode.Repeat;
            g.filterMode = FilterMode.Trilinear;
            g.SetPixels32(gpx);
            g.Apply(true, false);
            _cache[key] = a;
            _cache[key + "_glow"] = g;
            albedo = a;
            glow = g;
        }

        // ------------------------------------------------------------------ characters

        public const int PaletteSize = 16;

        /// <summary>16x16 palette: row 0 skin, row 1 cloth, row 2 hair, row 3 special (shoes, police, gangs).</summary>
        public static Texture2D CharacterPalette()
        {
            Texture2D cached;
            if (_cache.TryGetValue("charpalette", out cached) && cached != null) return cached;
            Texture2D t = new Texture2D(PaletteSize, PaletteSize, TextureFormat.RGBA32, false);
            t.name = "charpalette";
            t.filterMode = FilterMode.Point;
            t.wrapMode = TextureWrapMode.Clamp;
            Color32[] px = new Color32[PaletteSize * PaletteSize];
            for (int row = 0; row < PaletteSize; row++)
                for (int col = 0; col < PaletteSize; col++) px[row * PaletteSize + col] = C(CharacterPaletteData.Albedo(row, col));
            t.SetPixels32(px);
            t.Apply(false, false);
            _cache["charpalette"] = t;
            return t;
        }

        /// <summary>Row 5 of the character palette: armour plates and tech surfaces.</summary>
        public static readonly Color[] ArmorColors = CharacterPaletteData.Armor;

        /// <summary>Row 6: natural irises 0..5, sclera 6, dark 7, lit (glowing) irises 8..15.</summary>
        public static readonly Color[] EyeColors = CharacterPaletteData.Eyes;

        /// <summary>Per palette cell: R metallic, G fabric-detail strength, A smoothness (linear).</summary>
        public static Texture2D CharacterMG()
        {
            Texture2D cached;
            if (_cache.TryGetValue("charmg", out cached) && cached != null) return cached;
            Texture2D t = new Texture2D(PaletteSize, PaletteSize, TextureFormat.RGBA32, false, true);
            t.name = "charmg";
            t.filterMode = FilterMode.Point;
            t.wrapMode = TextureWrapMode.Clamp;
            Color[] px = new Color[PaletteSize * PaletteSize];
            for (int row = 0; row < PaletteSize; row++)
                for (int col = 0; col < PaletteSize; col++) px[row * PaletteSize + col] = CharacterPaletteData.MetalDetailSmooth(row, col);
            t.SetPixels(px);
            t.Apply(false, false);
            _cache["charmg"] = t;
            return t;
        }

        /// <summary>Emission per palette cell: the neon row and lit irises glow, everything else is dark.</summary>
        public static Texture2D CharacterEmission()
        {
            Texture2D cached;
            if (_cache.TryGetValue("charem", out cached) && cached != null) return cached;
            Texture2D t = new Texture2D(PaletteSize, PaletteSize, TextureFormat.RGBA32, false);
            t.name = "charem";
            t.filterMode = FilterMode.Point;
            t.wrapMode = TextureWrapMode.Clamp;
            Color32[] px = new Color32[PaletteSize * PaletteSize];
            for (int row = 0; row < PaletteSize; row++)
                for (int col = 0; col < PaletteSize; col++) px[row * PaletteSize + col] = C(CharacterPaletteData.Emission(row, col));
            t.SetPixels32(px);
            t.Apply(false, false);
            _cache["charem"] = t;
            return t;
        }

        /// <summary>Tileable fabric and tech-weave detail normal map (linear, RGB encoded, used with the second UV set).</summary>
        public static Texture2D CharacterDetail()
        {
            Texture2D cached;
            if (_cache.TryGetValue("chardetail", out cached) && cached != null) return cached;
            const int n = 128;
            float[] h = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = (float)x / n, v = (float)y / n;
                    float weaveA = Mathf.Sin(u * Mathf.PI * 2f * 16f) * 0.5f + 0.5f;
                    float weaveB = Mathf.Sin(v * Mathf.PI * 2f * 16f) * 0.5f + 0.5f;
                    bool over = ((Mathf.FloorToInt(u * 16f) + Mathf.FloorToInt(v * 16f)) & 1) == 0;
                    float weave = over ? weaveA : weaveB;
                    float quilt = Mathf.Abs(Mathf.Sin((u + v) * Mathf.PI * 2f * 2f)) * Mathf.Abs(Mathf.Sin((u - v) * Mathf.PI * 2f * 2f));
                    h[y * n + x] = weave * 0.6f + Mathf.Pow(quilt, 0.35f) * 0.4f;
                }
            }
            Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, true, true);
            t.name = "chardetail";
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Trilinear;
            t.anisoLevel = 4;
            Color32[] px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = h[y * n + ((x + 1) % n)] - h[y * n + ((x - 1 + n) % n)];
                    float dy = h[((y + 1) % n) * n + x] - h[((y - 1 + n) % n) * n + x];
                    Vector3 nn = new Vector3(-dx * 2.2f, -dy * 2.2f, 1f).normalized;
                    px[y * n + x] = new Color32((byte)((nn.x * 0.5f + 0.5f) * 255f), (byte)((nn.y * 0.5f + 0.5f) * 255f), (byte)((nn.z * 0.5f + 0.5f) * 255f), 255);
                }
            }
            t.SetPixels32(px);
            t.Apply(true, false);
            _cache["chardetail"] = t;
            return t;
        }

        /// <summary>UV at the center of a palette cell.</summary>
        public static Vector2 PaletteUV(int row, int column)
        {
            return new Vector2((column + 0.5f) / PaletteSize, (row + 0.5f) / PaletteSize);
        }

        // ------------------------------------------------------------------ fx and decals

        public static Texture2D SoftDot()
        {
            return Make("softdot", 64, 64, true, (x, y, u, v) =>
            {
                float d = new Vector2(u - 0.5f, v - 0.5f).magnitude * 2f;
                float a = Mathf.Clamp01(1f - d);
                a = a * a;
                return new Color(1f, 1f, 1f, a);
            });
        }

        public static Texture2D SmokePuff()
        {
            return Make("smokepuff", 64, 64, true, (x, y, u, v) =>
            {
                float d = new Vector2(u - 0.5f, v - 0.5f).magnitude * 2f;
                float n = Fbm(u, v, 4, 4, 121);
                float a = Mathf.Clamp01(1f - d) * (0.55f + n * 0.6f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(a * 1.2f));
            });
        }

        public static Texture2D BloodSplat()
        {
            return Make("bloodsplat", 64, 64, true, (x, y, u, v) =>
            {
                Vector2 p = new Vector2(u - 0.5f, v - 0.5f);
                float ang = Mathf.Atan2(p.y, p.x);
                float r = 0.25f + Fbm(ang / (Mathf.PI * 2f) + 0.5f, 0.3f, 8, 3, 131) * 0.22f;
                float d = p.magnitude;
                float a = d < r ? 1f : 0f;
                float drop = MathUtil.Hash01(x / 6, y / 6);
                if (d > r && d < 0.48f && drop > 0.93f) a = 1f;
                return new Color(0.35f, 0.01f, 0.01f, a * 0.92f);
            });
        }

        public static Texture2D BulletHole()
        {
            return Make("bullethole", 32, 32, true, (x, y, u, v) =>
            {
                float d = new Vector2(u - 0.5f, v - 0.5f).magnitude * 2f;
                if (d < 0.3f) return new Color(0.02f, 0.02f, 0.02f, 1f);
                if (d < 0.75f) return new Color(0.15f, 0.14f, 0.13f, (0.75f - d) * 1.6f);
                return new Color(0f, 0f, 0f, 0f);
            });
        }

        public static Texture2D Scorch()
        {
            return Make("scorch", 64, 64, true, (x, y, u, v) =>
            {
                float d = new Vector2(u - 0.5f, v - 0.5f).magnitude * 2f;
                float n = Fbm(u, v, 4, 4, 141);
                float a = Mathf.Clamp01((1f - d) * 1.6f) * (0.6f + n * 0.5f);
                return new Color(0.03f, 0.03f, 0.03f, Mathf.Clamp01(a));
            });
        }

        public static Texture2D Skid()
        {
            return Make("skid", 32, 64, true, (x, y, u, v) =>
            {
                float edge = Mathf.Min(u, 1f - u) * 2f;
                float n = MathUtil.Hash01(x, y / 2);
                float a = Mathf.Clamp01(edge * 2.5f) * (0.5f + n * 0.3f);
                return new Color(0.02f, 0.02f, 0.02f, a * 0.65f);
            });
        }
    }
}
