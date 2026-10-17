using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The decal textures of the dressing pass (oil, drains, manholes, worn paint, posters, water stains, grime),
    /// generated once and cached like Tex. They live here rather than in Tex because the material pass owns Tex;
    /// the noise helpers are Tex's own so the look stays in one family.
    /// </summary>
    public static class DecalTex
    {
        private static readonly Dictionary<string, Texture2D> _cache = new Dictionary<string, Texture2D>();

        public static void ResetStatics()
        {
            _cache.Clear();
        }

        private delegate Color PixelFn(int x, int y, float u, float v);

        private static Texture2D Make(string key, int w, int h, PixelFn fn)
        {
            Texture2D cached;
            if (_cache.TryGetValue(key, out cached) && cached != null) return cached;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, true);
            t.name = "decal_" + key;
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Trilinear;
            t.anisoLevel = 4;
            Color32[] px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float v = (y + 0.5f) / h;
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w;
                    Color c = fn(x, y, u, v);
                    px[y * w + x] = new Color32((byte)(Mathf.Clamp01(c.r) * 255f), (byte)(Mathf.Clamp01(c.g) * 255f), (byte)(Mathf.Clamp01(c.b) * 255f), (byte)(Mathf.Clamp01(c.a) * 255f));
                }
            }
            t.SetPixels32(px);
            t.Apply(true, false);
            _cache[key] = t;
            return t;
        }

        private static float Radial(float u, float v)
        {
            return new Vector2(u - 0.5f, v - 0.5f).magnitude * 2f;
        }

        /// <summary>Soft dark blob with a ragged edge: dirt at the foot of a wall.</summary>
        public static Texture2D Grime()
        {
            return Make("grime", 96, 64, (x, y, u, v) =>
            {
                float d = new Vector2((u - 0.5f) * 2f, (v - 0.5f) * 2.4f).magnitude;
                float n = Tex.Fbm(u, v, 5, 4, 211);
                float a = Mathf.Clamp01((1.05f - d) * 1.4f) * (0.35f + n * 0.75f);
                float tone = 0.05f + n * 0.08f;
                return new Color(tone, tone * 0.95f, tone * 0.9f, Mathf.Clamp01(a) * 0.62f);
            });
        }

        /// <summary>Dark oil pool with a slick, faintly iridescent rim.</summary>
        public static Texture2D OilStain()
        {
            return Make("oil", 96, 96, (x, y, u, v) =>
            {
                Vector2 p = new Vector2(u - 0.5f, v - 0.5f);
                float ang = Mathf.Atan2(p.y, p.x) / (Mathf.PI * 2f) + 0.5f;
                float rim = 0.3f + Tex.Fbm(ang, 0.37f, 6, 3, 311) * 0.17f;
                float d = p.magnitude;
                float core = Mathf.Clamp01((rim - d) * 9f);
                float n = Tex.Fbm(u, v, 4, 3, 313);
                float a = core * (0.55f + n * 0.45f);
                // The rim picks up a thin film of colour; the centre is near black.
                float edge = Mathf.Clamp01(1f - Mathf.Abs(d - rim + 0.04f) * 14f) * core;
                Color c = Color.Lerp(new Color(0.02f, 0.02f, 0.025f), new Color(0.25f, 0.12f, 0.3f), edge * 0.6f);
                return new Color(c.r, c.g, c.b, Mathf.Clamp01(a) * 0.9f);
            });
        }

        /// <summary>A fan of dark streaks spreading from one edge: the run-off below a curb drain.</summary>
        public static Texture2D DrainStain()
        {
            return Make("drain", 64, 64, (x, y, u, v) =>
            {
                // The apex sits at v = 1 (the curb), streaks spread toward v = 0 (the road).
                float spread = 1f - v;
                float cx = (u - 0.5f) / Mathf.Max(0.08f, 0.12f + spread * 0.5f);
                float fan = Mathf.Clamp01(1f - Mathf.Abs(cx));
                float streak = 0.5f + 0.5f * Mathf.Sin(u * 61f + Tex.Noise(u * 9f, v * 3f, 0, 41) * 4f);
                float fade = Mathf.Clamp01(1.1f - spread * 1.1f);
                float a = fan * fade * (0.35f + streak * 0.5f);
                float tone = 0.08f + Tex.Fbm(u, v, 4, 3, 43) * 0.07f;
                return new Color(tone, tone, tone * 1.05f, Mathf.Clamp01(a) * 0.7f);
            });
        }

        /// <summary>Round iron cover: outer ring, bolt dimples and a hatched centre; alpha outside the disc.</summary>
        public static Texture2D Manhole()
        {
            return Make("manhole", 96, 96, (x, y, u, v) =>
            {
                float d = Radial(u, v);
                if (d > 0.98f) return new Color(0f, 0f, 0f, 0f);
                float n = Tex.Fbm(u, v, 6, 3, 511);
                float tone = 0.16f + n * 0.09f;
                float ring = Mathf.Clamp01(1f - Mathf.Abs(d - 0.86f) * 18f);
                float inner = Mathf.Clamp01(1f - Mathf.Abs(d - 0.62f) * 22f);
                float hatch = d < 0.6f ? (((x / 6 + y / 6) & 1) == 0 ? 0.04f : -0.03f) : 0f;
                float ang = Mathf.Atan2(v - 0.5f, u - 0.5f);
                float bolt = d > 0.68f && d < 0.8f ? Mathf.Clamp01(Mathf.Cos(ang * 8f) * 6f - 4.5f) : 0f;
                tone = tone - ring * 0.09f - inner * 0.06f + hatch + bolt * 0.1f;
                float shade = Mathf.Lerp(1f, 0.55f, Mathf.Clamp01((d - 0.92f) * 16f));
                return new Color(tone * shade, (tone + 0.005f) * shade, (tone + 0.012f) * shade, 1f);
            });
        }

        /// <summary>Asphalt-toned noise that eats into painted lines where tyres roll over them.</summary>
        public static Texture2D MarkingWear()
        {
            return Make("wear", 64, 96, (x, y, u, v) =>
            {
                float n = Tex.Fbm(u, v, 3, 4, 611);
                float n2 = Tex.Fbm(u * 1.7f, v, 7, 2, 613);
                float edge = Mathf.Clamp01(Mathf.Min(u, 1f - u) * 5f) * Mathf.Clamp01(Mathf.Min(v, 1f - v) * 4f);
                float a = Mathf.Clamp01((n - 0.32f) * 2.2f) * (0.5f + n2 * 0.5f) * edge;
                float tone = 0.11f + n2 * 0.06f;
                return new Color(tone, tone, tone * 1.03f, Mathf.Clamp01(a) * 0.85f);
            });
        }

        /// <summary>Four torn paper scraps in one sheet (one per quadrant), faded print and ragged edges.</summary>
        public static Texture2D PosterScrap()
        {
            return Make("poster", 128, 128, (x, y, u, v) =>
            {
                int qx = u < 0.5f ? 0 : 1, qy = v < 0.5f ? 0 : 1;
                int q = qx + qy * 2;
                float lu = (u - qx * 0.5f) * 2f, lv = (v - qy * 0.5f) * 2f;
                // Ragged rectangle: the edge wobbles with noise and the corners are torn off.
                float wob = Tex.Fbm(lu, lv, 5, 3, 711 + q * 7) * 0.12f;
                float inside = Mathf.Min(Mathf.Min(lu, 1f - lu), Mathf.Min(lv, 1f - lv));
                float a = Mathf.Clamp01((inside - 0.06f - wob) * 12f);
                float tear = Tex.Fbm(lu, lv, 3, 2, 719 + q * 13);
                if (tear > 0.74f && inside < 0.3f) a = 0f;
                // Print: a tinted ground, a band and some "text" rows, faded by the sun.
                Color paper = q == 0 ? new Color(0.78f, 0.72f, 0.58f) : q == 1 ? new Color(0.6f, 0.68f, 0.72f) : q == 2 ? new Color(0.76f, 0.56f, 0.5f) : new Color(0.66f, 0.7f, 0.56f);
                Color ink = q == 0 ? new Color(0.5f, 0.12f, 0.1f) : q == 1 ? new Color(0.1f, 0.12f, 0.3f) : q == 2 ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.12f, 0.3f, 0.2f);
                float band = lv > 0.62f && lv < 0.86f ? 1f : 0f;
                float rows = lv < 0.5f && lv > 0.12f && ((int)(lv * 28f) % 3 == 0) && lu > 0.12f && lu < 0.88f ? (Tex.Noise(lu * 40f, lv * 60f, 0, 731) > 0.45f ? 1f : 0f) : 0f;
                float fade = 0.55f + Tex.Fbm(lu, lv, 2, 3, 733) * 0.45f;
                Color c = Color.Lerp(paper, ink, Mathf.Max(band * 0.85f, rows * 0.7f)) * fade;
                return new Color(c.r, c.g, c.b, a);
            });
        }

        /// <summary>A vertical run of damp, dark at the top where the sill drips and fading toward the bottom.</summary>
        public static Texture2D WaterStain()
        {
            return Make("waterstain", 48, 128, (x, y, u, v) =>
            {
                // The sill is at v = 1, the stain fades toward v = 0.
                float width = 0.22f + Tex.Noise(v * 14f, 0.5f, 0, 811) * 0.2f;
                float cx = Mathf.Abs(u - 0.5f);
                float body = Mathf.Clamp01((width - cx) * 9f);
                float streak = 0.6f + 0.4f * Mathf.Sin(u * 50f + Tex.Noise(u * 6f, v * 20f, 0, 813) * 3f);
                float fade = Mathf.Pow(v, 1.6f);
                float a = body * streak * fade;
                float tone = 0.12f + Tex.Fbm(u, v, 3, 3, 815) * 0.06f;
                return new Color(tone, tone, tone * 1.08f, Mathf.Clamp01(a) * 0.55f);
            });
        }
    }

    /// <summary>The transparent decal materials of the dressing pass (built from Mats helpers, cached here).</summary>
    public static class DecalMats
    {
        private static readonly Dictionary<string, Material> _cache = new Dictionary<string, Material>();

        public static void ResetStatics()
        {
            _cache.Clear();
        }

        private static Material Fade(string key, string name, Texture2D tex, float smoothness, float metallic)
        {
            Material m;
            if (_cache.TryGetValue(key, out m) && m != null) return m;
            m = Mats.Textured(name, tex, Color.white, smoothness, metallic, null, 0f);
            Mats.MakeTransparent(m, true);
            _cache[key] = m;
            return m;
        }

        public static Material Grime { get { return Fade("grime", "Decal Grime", DecalTex.Grime(), 0.08f, 0f); } }
        public static Material OilStain { get { return Fade("oil", "Decal Oil", DecalTex.OilStain(), 0.8f, 0.1f); } }
        public static Material DrainStain { get { return Fade("drain", "Decal Drain", DecalTex.DrainStain(), 0.2f, 0f); } }
        public static Material Manhole { get { return Fade("manhole", "Decal Manhole", DecalTex.Manhole(), 0.42f, 0.65f); } }
        public static Material MarkingWear { get { return Fade("wear", "Decal Paint Wear", DecalTex.MarkingWear(), 0.15f, 0f); } }
        public static Material PosterScrap { get { return Fade("poster", "Decal Poster", DecalTex.PosterScrap(), 0.1f, 0f); } }
        public static Material WaterStain { get { return Fade("waterstain", "Decal Water Stain", DecalTex.WaterStain(), 0.3f, 0f); } }

        /// <summary>The material for a decal kind (the skid and scorch marks reuse the hit-effect materials).</summary>
        public static Material For(DressKind kind)
        {
            switch (kind)
            {
                case DressKind.Grime: return Grime;
                case DressKind.OilStain: return OilStain;
                case DressKind.DrainStain: return DrainStain;
                case DressKind.Manhole: return Manhole;
                case DressKind.MarkingWear: return MarkingWear;
                case DressKind.Skid: return Mats.Skid;
                case DressKind.Scorch: return Mats.Scorch;
                case DressKind.PosterScrap: return PosterScrap;
                case DressKind.WaterStain: return WaterStain;
                default: return null;
            }
        }
    }
}
