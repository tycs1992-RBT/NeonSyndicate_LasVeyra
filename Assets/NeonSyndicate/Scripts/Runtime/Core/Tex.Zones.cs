using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Procedural textures for the Frostline (snow, frost-dusted sand, lake ice) and the Frost Wardens' banner.</summary>
    public static partial class Tex
    {
        /// <summary>Sparse glitter texels shared by the snow albedo and the snow sparkle (emission) map.</summary>
        private static bool SnowSparkle(int x, int y)
        {
            return MathUtil.Hash01(x * 3 + 17, y * 5 + 91) > 0.9925f;
        }

        /// <summary>Wind-packed snow: cool white crests, blue-grey hollows along tileable drift ripples, fine grain and glitter.</summary>
        public static Texture2D Snow()
        {
            return Make("snow", 256, 256, true, (x, y, u, v) =>
            {
                float n = Fbm(u, v, 6, 5, 211);
                float drift = Mathf.Sin((u * 3f + v * 5f + n * 0.6f) * Mathf.PI * 2f) * 0.5f + 0.5f;
                float shade = Mathf.Clamp01(n * 0.6f + drift * drift * 0.4f);
                Color hollow = new Color(0.72f, 0.78f, 0.87f);
                Color crest = new Color(0.9f, 0.92f, 0.95f);
                Color c = Color.Lerp(hollow, crest, shade);
                c *= 0.97f + MathUtil.Hash01(x * 7 + 3, y * 13 + 5) * 0.05f;
                if (SnowSparkle(x, y)) c = new Color(1f, 1f, 1f);
                c.a = 1f;
                return c;
            });
        }

        /// <summary>Height for the snow normal map: soft drift ripples and powdery grain.</summary>
        public static Texture2D SnowBump()
        {
            return Make("snowbump", 256, 256, true, (x, y, u, v) =>
            {
                float n = Fbm(u, v, 6, 5, 211);
                float drift = Mathf.Sin((u * 3f + v * 5f + n * 0.6f) * Mathf.PI * 2f) * 0.5f + 0.5f;
                float grain = MathUtil.Hash01(x * 5 + 11, y * 3 + 29);
                float h = drift * 0.55f + n * 0.35f + grain * 0.1f;
                return new Color(h, h, h, 1f);
            });
        }

        /// <summary>Emission map for the snow: only the glitter texels glow, faintly, so fresh snow twinkles.</summary>
        public static Texture2D SnowSparkleMap()
        {
            return Make("snowsparkle", 256, 256, true, (x, y, u, v) =>
            {
                if (!SnowSparkle(x, y)) return new Color(0f, 0f, 0f, 1f);
                float tint = MathUtil.Hash01(x + 101, y + 307);
                return new Color(0.85f + tint * 0.15f, 0.92f, 1f, 1f);
            });
        }

        /// <summary>
        /// Frost-dusted sand for the soft edge of the snow: the desert sand palette, slightly cooled, under blotches of
        /// thin snow that cover about half of it.
        /// </summary>
        public static Texture2D SnowThin()
        {
            return Make("snowthin", 256, 256, true, (x, y, u, v) =>
            {
                float n = Fbm(u, v, 6, 5, 51);
                float ripple = Mathf.Sin((v + n * 0.12f) * 60f) * 0.5f + 0.5f;
                Color sand = Color.Lerp(new Color(0.72f, 0.58f, 0.40f), new Color(0.86f, 0.72f, 0.52f), n * 0.7f + ripple * 0.3f);
                sand = Color.Lerp(sand, new Color(0.62f, 0.6f, 0.58f), 0.2f);
                float cover = Fbm(u, v, 8, 4, 223) + (MathUtil.Hash01(x * 3 + 1, y * 7 + 2) - 0.5f) * 0.1f;
                float m = Mathf.Clamp01((cover - 0.45f) / 0.12f);
                m = m * m * (3f - 2f * m);
                Color snow = Color.Lerp(new Color(0.78f, 0.82f, 0.89f), new Color(0.9f, 0.92f, 0.95f), n);
                Color c = Color.Lerp(sand, snow, m);
                if (m > 0.5f && SnowSparkle(x, y)) c = new Color(1f, 1f, 1f);
                c.a = 1f;
                return c;
            });
        }

        /// <summary>Distance to the nearest Voronoi edge (F2 - F1) on a tileable jittered grid, in texture units.</summary>
        private static float CrackDistance(float u, float v, int cells, int seed)
        {
            float gx = u * cells, gy = v * cells;
            int cx = Mathf.FloorToInt(gx), cy = Mathf.FloorToInt(gy);
            float f1 = 9f, f2 = 9f;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int px = cx + dx, py = cy + dy;
                    int wx = ((px % cells) + cells) % cells, wy = ((py % cells) + cells) % cells;
                    float jx = px + 0.15f + 0.7f * MathUtil.Hash01(wx * 31 + seed, wy * 17 + 3);
                    float jy = py + 0.15f + 0.7f * MathUtil.Hash01(wx * 13 + 7, wy * 29 + seed);
                    float d = Mathf.Sqrt((gx - jx) * (gx - jx) + (gy - jy) * (gy - jy));
                    if (d < f1)
                    {
                        f2 = f1;
                        f1 = d;
                    }
                    else if (d < f2) f2 = d;
                }
            }
            return (f2 - f1) / cells;
        }

        private static float IceCrack(float u, float v)
        {
            float big = CrackDistance(u, v, 5, 401);
            float small = CrackDistance(u, v, 11, 409);
            float hair = Mathf.Abs(Fbm(u * 0.5f + 0.17f, v * 0.5f, 4, 3, 417) - 0.5f);
            float c = Mathf.Clamp01(1f - big / 0.006f);
            c = Mathf.Max(c, Mathf.Clamp01(1f - small / 0.0035f) * 0.65f);
            c = Mathf.Max(c, Mathf.Clamp01(1f - hair / 0.006f) * 0.45f);
            return c;
        }

        /// <summary>Lake ice: deep blue-grey clear ice with white fracture lines, hairline cracks and trapped bubbles.</summary>
        public static Texture2D Ice()
        {
            return Make("ice", 256, 256, true, (x, y, u, v) =>
            {
                float n = Fbm(u, v, 4, 5, 431);
                Color deep = new Color(0.32f, 0.46f, 0.58f);
                Color clear = new Color(0.55f, 0.72f, 0.84f);
                Color c = Color.Lerp(deep, clear, n);
                float frost = Fbm(u, v, 8, 3, 433);
                if (frost > 0.6f) c = Color.Lerp(c, new Color(0.82f, 0.88f, 0.94f), (frost - 0.6f) * 1.6f);
                float crack = IceCrack(u, v);
                c = Color.Lerp(c, new Color(0.9f, 0.96f, 1f), crack);
                if (MathUtil.Hash01(x * 11 + 5, y * 7 + 13) > 0.996f) c = Color.Lerp(c, Color.white, 0.6f);
                c.a = 1f;
                return c;
            });
        }

        /// <summary>Height for the ice normal map: cracks are thin grooves in an almost flat sheet.</summary>
        public static Texture2D IceBump()
        {
            return Make("icebump", 256, 256, true, (x, y, u, v) =>
            {
                float h = 0.85f - IceCrack(u, v) * 0.6f + Fbm(u, v, 8, 2, 437) * 0.08f;
                return new Color(h, h, h, 1f);
            });
        }

        /// <summary>The Frost Wardens' banner: a deep blue field, a silver border and a six-point frost star (v = 1 at the top).</summary>
        public static Texture2D WardenBanner()
        {
            return Make("wardenbanner", 64, 128, true, (x, y, u, v) =>
            {
                Color field = Color.Lerp(new Color(0.05f, 0.11f, 0.24f), new Color(0.08f, 0.18f, 0.34f), Fbm(u, v, 4, 3, 441));
                Color silver = new Color(0.82f, 0.87f, 0.92f);
                if (u < 0.07f || u > 0.93f || v > 0.95f) return silver;
                if (v < 0.12f) return (Mathf.FloorToInt(u * 8f) & 1) == 0 ? silver * 0.85f : field * 0.7f;
                // Six-point star centred in the upper half.
                float dx = (u - 0.5f) * 2f, dy = (v - 0.62f) * 4f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Atan2(dy, dx);
                float arm = Mathf.Abs(Mathf.Cos(a * 3f));
                float limit = 0.18f + 0.55f * Mathf.Pow(arm, 6f);
                if (r < limit) return Color.Lerp(silver, Color.white, 1f - r / Mathf.Max(0.01f, limit));
                if (Mathf.Abs(r - 0.82f) < 0.05f) return silver * 0.9f;
                return field;
            });
        }
    }
}
