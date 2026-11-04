using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The three strand textures of the live hair: long strands root to tip, a short crop, and stubble dots.</summary>
    public enum LiveHairTextureKind
    {
        /// <summary>Strands running the whole tile from the roots (bottom) to ragged tips (top), for the shells, drapes and braids.</summary>
        Long = 0,
        /// <summary>Short staggered strands for cropped hair and short beards.</summary>
        Crop = 1,
        /// <summary>Dots for a few days' growth.</summary>
        Stubble = 2
    }

    /// <summary>
    /// Paints the procedural strand textures of the live hair as plain pixel arrays (engine-free; the runtime wraps them
    /// in Texture2D). The albedo holds a greyscale strand value in RGB, darker at the roots and brighter toward the
    /// ends, and the strand coverage in alpha (so the strand shells are alpha-tested while the base cap ignores the
    /// alpha and stays solid); the normal map comes from the rounded profile of every strand. Deterministic per seed.
    /// </summary>
    public static class LiveHairTexture
    {
        /// <summary>The square size the runtime uses.</summary>
        public const int Size = 512;
        /// <summary>The normal map's slope strength.</summary>
        public const float NormalStrength = 1.6f;

        /// <summary>The strand albedo (RGB value, A coverage), size by size, row 0 at the roots.</summary>
        public static Color32[] Albedo(int size, LiveHairTextureKind kind, int seed)
        {
            float[] value, alpha, height;
            Paint(size, kind, seed, out value, out alpha, out height);
            Color32[] px = new Color32[size * size];
            for (int i = 0; i < px.Length; i++)
            {
                byte v = (byte)Mathf.RoundToInt(Mathf.Clamp01(value[i]) * 255f);
                px[i] = new Color32(v, v, v, (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha[i]) * 255f));
            }
            return px;
        }

        /// <summary>The tangent-space normal map of the strands (RGB, alpha 255), wrapping across the tile.</summary>
        public static Color32[] Normal(int size, LiveHairTextureKind kind, int seed)
        {
            float[] value, alpha, height;
            Paint(size, kind, seed, out value, out alpha, out height);
            Color32[] px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float l = height[y * size + (x - 1 + size) % size], r = height[y * size + (x + 1) % size];
                    float d = height[Mathf.Max(0, y - 1) * size + x], u = height[Mathf.Min(size - 1, y + 1) * size + x];
                    Vector3 n = new Vector3((l - r) * NormalStrength, (d - u) * NormalStrength, 1f).normalized;
                    px[y * size + x] = new Color32((byte)((n.x * 0.5f + 0.5f) * 255f), (byte)((n.y * 0.5f + 0.5f) * 255f), (byte)((n.z * 0.5f + 0.5f) * 255f), 255);
                }
            return px;
        }

        /// <summary>The mean coverage of a tile (tests and the F2 page).</summary>
        public static float Coverage(Color32[] albedo)
        {
            if (albedo == null || albedo.Length == 0) return 0f;
            float sum = 0f;
            for (int i = 0; i < albedo.Length; i++) sum += albedo[i].a;
            return sum / (255f * albedo.Length);
        }

        /// <summary>The mean coverage of the rows between two fractions of the tile height (0 roots, 1 tips).</summary>
        public static float Coverage(Color32[] albedo, int size, float from, float to)
        {
            int y0 = Mathf.Clamp(Mathf.FloorToInt(from * size), 0, size - 1), y1 = Mathf.Clamp(Mathf.CeilToInt(to * size), y0 + 1, size);
            float sum = 0f;
            int n = 0;
            for (int y = y0; y < y1; y++)
                for (int x = 0; x < size; x++) { sum += albedo[y * size + x].a; n++; }
            return n > 0 ? sum / (255f * n) : 0f;
        }

        private static void Paint(int size, LiveHairTextureKind kind, int seed, out float[] value, out float[] alpha, out float[] height)
        {
            value = new float[size * size];
            alpha = new float[size * size];
            height = new float[size * size];
            for (int i = 0; i < value.Length; i++) value[i] = 0.62f;
            GRand r = new GRand(seed * 7919 + (int)kind * 131 + 17);
            float px = size / 256f;
            switch (kind)
            {
                case LiveHairTextureKind.Long:
                {
                    // Strands gather in locks: a clump centre with a gentle common sway, each strand jittered around it.
                    int clumps = 36;
                    for (int c = 0; c < clumps; c++)
                    {
                        float cx = (c + r.Range(0.1f, 0.9f)) * size / clumps, camp = r.Range(0.6f, 3f) * px, cwaves = r.Range(0.25f, 0.9f), cphase = r.Range(0f, 6.28f);
                        int count = r.Range(5, 10);
                        for (int k = 0; k < count; k++)
                        {
                            float x0 = cx + r.Range(-4.5f, 4.5f) * px, amp = r.Range(0.3f, 1.4f) * px, waves = r.Range(0.8f, 2.6f), phase = r.Range(0f, 6.28f);
                            float w0 = r.Range(1.6f, 3.6f) * px, w1 = r.Range(0.6f, 1.4f) * px, end = r.Range(0.7f, 1f), shade = 0.42f + 0.58f * Mathf.Pow(r.Value(), 1.3f);
                            Strand(size, value, alpha, height, t => x0 + Mathf.Sin(t * cwaves * 6.28f + cphase) * camp + Mathf.Sin(t * waves * 6.28f + phase) * amp, 0f, end, w0, w1, shade, true);
                        }
                    }
                    break;
                }
                case LiveHairTextureKind.Crop:
                    for (int k = 0; k < 1500; k++)
                    {
                        float x0 = r.Range(0f, size), tilt = r.Range(-12f, 12f) * px, start = r.Range(0f, 0.9f), len = r.Range(0.08f, 0.22f);
                        float w0 = r.Range(1.2f, 2.6f) * px, w1 = r.Range(0.6f, 1.2f) * px, shade = 0.42f + 0.58f * Mathf.Pow(r.Value(), 1.3f);
                        float s0 = start;
                        Strand(size, value, alpha, height, t => x0 + (t - s0) * tilt, start, Mathf.Min(1f, start + len), w0, w1, shade, false);
                    }
                    break;
                default:
                    // About a quarter of the tile in dots of a hair's breadth, whatever the size.
                    int dots = Mathf.Max(60, Mathf.RoundToInt(size * size / 16f));
                    for (int k = 0; k < dots; k++)
                    {
                        float cx = r.Range(0f, size), cy = r.Range(0f, size), rad = r.Range(0.9f, 1.7f) * Mathf.Max(0.8f, px), shade = r.Range(0.28f, 0.6f);
                        Dot(size, value, alpha, height, cx, cy, rad, shade);
                    }
                    break;
            }
        }

        /// <summary>One strand from v0 to v1 of the tile along a centre line x(v), tapering from w0 to w1 pixels, with a rounded profile.</summary>
        private static void Strand(int size, float[] value, float[] alpha, float[] height, System.Func<float, float> centre, float v0, float v1, float w0, float w1, float shade, bool gradient)
        {
            int y0 = Mathf.Clamp(Mathf.FloorToInt(v0 * size), 0, size - 1), y1 = Mathf.Clamp(Mathf.CeilToInt(v1 * size), 0, size);
            for (int y = y0; y < y1; y++)
            {
                float v = (y + 0.5f) / size;
                float t = Mathf.InverseLerp(v0, v1, v);
                float half = Mathf.Lerp(w0, w1, t) * 0.5f;
                float cx = centre(v);
                // Darker at the roots, brightest two thirds down, a little dimmer again at the very tip; the tip also thins to nothing.
                float tone = shade * (gradient ? Mathf.Lerp(0.72f, 1f, Mathf.SmoothStep(0f, 1f, t * 1.4f)) * (1f - 0.12f * Mathf.Pow(t, 6f)) : Mathf.Lerp(0.8f, 1f, t));
                float tipFade = gradient ? Mathf.Clamp01((1f - t) * 4f) : Mathf.Clamp01((1f - t) * 6f) * Mathf.Clamp01(t * 6f);
                int xa = Mathf.FloorToInt(cx - half - 1f), xb = Mathf.CeilToInt(cx + half + 1f);
                for (int x = xa; x <= xb; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - cx) / Mathf.Max(0.3f, half);
                    if (dx >= 1.25f) continue;
                    float cover = Mathf.Clamp01((1.25f - dx) * 2.5f) * tipFade;
                    if (cover <= 0.01f) continue;
                    int i = y * size + ((x % size) + size) % size;
                    float profile = Mathf.Sqrt(Mathf.Clamp01(1f - dx * dx));
                    if (cover > alpha[i] * 0.5f)
                    {
                        value[i] = Mathf.Lerp(value[i], tone * (0.82f + 0.18f * profile), Mathf.Clamp01(cover));
                        height[i] = Mathf.Max(height[i], profile * cover);
                    }
                    if (cover > alpha[i]) alpha[i] = cover;
                }
            }
        }

        private static void Dot(int size, float[] value, float[] alpha, float[] height, float cx, float cy, float rad, float shade)
        {
            int xa = Mathf.FloorToInt(cx - rad - 1f), xb = Mathf.CeilToInt(cx + rad + 1f), ya = Mathf.FloorToInt(cy - rad - 1f), yb = Mathf.CeilToInt(cy + rad + 1f);
            for (int y = ya; y <= yb; y++)
                for (int x = xa; x <= xb; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy)) / rad;
                    if (d >= 1.3f) continue;
                    float cover = Mathf.Clamp01((1.3f - d) * 3f);
                    int i = ((y % size) + size) % size * size + ((x % size) + size) % size;
                    if (cover > alpha[i]) { alpha[i] = cover; value[i] = shade; height[i] = Mathf.Max(height[i], Mathf.Sqrt(Mathf.Clamp01(1f - d * d))); }
                }
        }
    }
}
