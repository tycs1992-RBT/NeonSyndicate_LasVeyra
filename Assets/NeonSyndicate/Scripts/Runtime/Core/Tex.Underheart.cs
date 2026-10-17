using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Procedural textures for the Underheart (04E): cave rock in five mineral palettes, poured concrete, transit tile,
    /// terrazzo, grating, iron brick, riveted plate, veined archive stone, pale ash, ember soil, molten lava (with its glow
    /// map), hazard stripes and woven blanket. All tileable value noise, cached like the rest of Tex.
    /// </summary>
    public static partial class Tex
    {
        /// <summary>Cave rock: strata bands, fractures and mineral veins between two palette colours. Palettes: 0 basalt,
        /// 1 ember (gardens), 2 cool (service tunnel and refuge rock), 3 ash, 4 foundry.</summary>
        public static Texture2D UhRock(int palette)
        {
            Color dark, light, vein;
            int seed;
            switch (palette)
            {
                case 1: dark = new Color(0.20f, 0.10f, 0.07f); light = new Color(0.46f, 0.26f, 0.17f); vein = new Color(1f, 0.45f, 0.12f); seed = 311; break;
                case 2: dark = new Color(0.16f, 0.18f, 0.20f); light = new Color(0.38f, 0.42f, 0.44f); vein = new Color(0.45f, 0.85f, 0.85f); seed = 313; break;
                case 3: dark = new Color(0.42f, 0.41f, 0.40f); light = new Color(0.74f, 0.72f, 0.70f); vein = new Color(0.86f, 0.84f, 0.80f); seed = 317; break;
                case 4: dark = new Color(0.16f, 0.08f, 0.06f); light = new Color(0.36f, 0.20f, 0.14f); vein = new Color(1f, 0.38f, 0.10f); seed = 331; break;
                default: dark = new Color(0.07f, 0.07f, 0.08f); light = new Color(0.24f, 0.23f, 0.24f); vein = new Color(0.95f, 0.42f, 0.12f); seed = 307; break;
            }
            return Make("uhrock" + palette, 256, 256, true, (x, y, u, v) =>
            {
                float n = Fbm(u, v, 4, 6, seed);
                float strata = Mathf.Sin((v * 9f + n * 2.4f) * Mathf.PI * 2f) * 0.5f + 0.5f;
                float crack = Mathf.Abs(Fbm(u * 2f, v * 2f, 6, 3, seed + 5) - 0.5f);
                Color c = Color.Lerp(dark, light, Mathf.Clamp01(n * 0.65f + strata * 0.35f));
                if (crack < 0.018f) c *= 0.55f;
                float veinN = Fbm(u, v, 8, 3, seed + 9);
                if (palette != 3 && Mathf.Abs(veinN - 0.5f) < 0.012f) c = Color.Lerp(c, vein, 0.55f);
                c *= 0.94f + MathUtil.Hash01(x * 7 + seed, y * 3 + 11) * 0.1f;
                c.a = 1f;
                return c;
            });
        }

        /// <summary>Glow map for the rock veins (only the vein texels emit).</summary>
        public static Texture2D UhRockVeins(int palette)
        {
            int seed = palette == 1 ? 311 : (palette == 4 ? 331 : 307);
            Color vein = palette == 2 ? new Color(0.3f, 0.8f, 0.8f) : new Color(1f, 0.42f, 0.1f);
            return Make("uhrockveins" + palette, 256, 256, true, (x, y, u, v) =>
            {
                float veinN = Fbm(u, v, 8, 3, seed + 9);
                float k = Mathf.Clamp01(1f - Mathf.Abs(veinN - 0.5f) / 0.014f);
                return new Color(vein.r * k, vein.g * k, vein.b * k, 1f);
            });
        }

        /// <summary>Poured utility concrete with formwork seams and damp stains.</summary>
        public static Texture2D UhConcrete()
        {
            return Make("uhconcrete", 256, 256, true, (x, y, u, v) =>
            {
                float n = Fbm(u, v, 5, 5, 401);
                float stain = Mathf.Clamp01((Fbm(u, v, 3, 3, 409) - 0.55f) * 4f);
                Color c = Color.Lerp(new Color(0.42f, 0.42f, 0.41f), new Color(0.58f, 0.57f, 0.55f), n);
                c = Color.Lerp(c, new Color(0.26f, 0.27f, 0.26f), stain * 0.6f);
                if (y % 64 < 2 || x % 128 < 2) c *= 0.72f;
                c.a = 1f;
                return c;
            });
        }

        /// <summary>Glazed transit tile (pale green) with grout lines, a darker band and age.</summary>
        public static Texture2D UhTile()
        {
            return Make("uhtile", 256, 256, true, (x, y, u, v) =>
            {
                bool grout = x % 32 < 2 || y % 16 < 2;
                float n = Fbm(u, v, 6, 4, 421);
                Color c = y < 48 ? new Color(0.20f, 0.36f, 0.34f) : new Color(0.62f, 0.74f, 0.68f);
                c *= 0.88f + n * 0.2f + MathUtil.Hash01(x / 32 + 3, y / 16 + 7) * 0.08f;
                if (grout) c = new Color(0.24f, 0.25f, 0.24f);
                c.a = 1f;
                return c;
            });
        }

        /// <summary>Terrazzo platform floor: pale with flecks and a yellow safety line near one edge.</summary>
        public static Texture2D UhTerrazzo()
        {
            return Make("uhterrazzo", 256, 256, true, (x, y, u, v) =>
            {
                float n = Fbm(u, v, 6, 4, 431);
                Color c = Color.Lerp(new Color(0.48f, 0.47f, 0.45f), new Color(0.62f, 0.61f, 0.58f), n);
                float h = MathUtil.Hash01(x * 5 + 1, y * 7 + 3);
                if (h > 0.985f) c = new Color(0.25f, 0.42f, 0.42f);
                else if (h > 0.97f) c = new Color(0.75f, 0.72f, 0.66f);
                c.a = 1f;
                return c;
            });
        }

        /// <summary>Steel grating: a dark square lattice with bright worn edges.</summary>
        public static Texture2D UhGrating()
        {
            return Make("uhgrating", 128, 128, true, (x, y, u, v) =>
            {
                bool bar = x % 16 < 3 || y % 16 < 3;
                Color c = bar ? new Color(0.42f, 0.40f, 0.38f) : new Color(0.05f, 0.05f, 0.05f);
                if (bar && (x % 16 == 0 || y % 16 == 0)) c = new Color(0.62f, 0.58f, 0.52f);
                c.a = 1f;
                return c;
            });
        }

        /// <summary>Iron-red brick for the workshops, soot stained toward the top of each tile repeat.</summary>
        public static Texture2D UhBrick()
        {
            return Make("uhbrick", 256, 256, true, (x, y, u, v) =>
            {
                int row = y / 16;
                int off = (row % 2) * 16;
                bool mortar = y % 16 < 2 || (x + off) % 32 < 2;
                float n = Fbm(u, v, 6, 4, 441);
                Color c = Color.Lerp(new Color(0.30f, 0.12f, 0.08f), new Color(0.50f, 0.22f, 0.14f), n * 0.7f + MathUtil.Hash01((x + off) / 32, row) * 0.3f);
                c = Color.Lerp(c, new Color(0.08f, 0.07f, 0.07f), Mathf.Clamp01(v * 0.5f));
                if (mortar) c = new Color(0.28f, 0.26f, 0.24f);
                c.a = 1f;
                return c;
            });
        }

        /// <summary>Riveted iron plates with heat tint.</summary>
        public static Texture2D UhPlate()
        {
            return Make("uhplate", 256, 256, true, (x, y, u, v) =>
            {
                float n = Fbm(u, v, 4, 5, 451);
                Color c = Color.Lerp(new Color(0.16f, 0.14f, 0.13f), new Color(0.32f, 0.27f, 0.24f), n);
                c = Color.Lerp(c, new Color(0.35f, 0.18f, 0.24f), Mathf.Clamp01((Fbm(u, v, 2, 3, 457) - 0.6f) * 3f) * 0.5f);
                if (x % 128 < 2 || y % 128 < 2) c *= 0.5f;
                int rx = x % 128, ry = y % 128;
                if ((rx - 8) * (rx - 8) + (ry - 8) * (ry - 8) < 10 || (rx - 120) * (rx - 120) + (ry - 8) * (ry - 8) < 10) c = new Color(0.5f, 0.46f, 0.4f);
                c.a = 1f;
                return c;
            });
        }

        /// <summary>Pale veined stone for the archive's columns and statues.</summary>
        public static Texture2D UhMarble()
        {
            return Make("uhmarble", 256, 256, true, (x, y, u, v) =>
            {
                float n = Fbm(u, v, 4, 6, 461);
                float vein = Mathf.Abs(Mathf.Sin((u * 3f + v + n * 2.5f) * Mathf.PI * 2f));
                Color c = Color.Lerp(new Color(0.78f, 0.77f, 0.74f), new Color(0.88f, 0.87f, 0.84f), n);
                if (vein < 0.06f) c = Color.Lerp(c, new Color(0.45f, 0.44f, 0.46f), 0.6f);
                c.a = 1f;
                return c;
            });
        }

        /// <summary>Pale ash floor: soft drifts with darker scuffs.</summary>
        public static Texture2D UhAsh()
        {
            return Make("uhash", 256, 256, true, (x, y, u, v) =>
            {
                float n = Fbm(u, v, 5, 5, 471);
                Color c = Color.Lerp(new Color(0.55f, 0.54f, 0.52f), new Color(0.72f, 0.71f, 0.68f), n);
                if (Fbm(u, v, 12, 2, 477) < 0.32f) c *= 0.86f;
                c.a = 1f;
                return c;
            });
        }

        /// <summary>Ember soil for the gardens: dark loam with glowing specks.</summary>
        public static Texture2D UhSoil()
        {
            return Make("uhsoil", 256, 256, true, (x, y, u, v) =>
            {
                float n = Fbm(u, v, 6, 5, 481);
                Color c = Color.Lerp(new Color(0.10f, 0.06f, 0.04f), new Color(0.26f, 0.15f, 0.09f), n);
                if (MathUtil.Hash01(x * 3 + 5, y * 11 + 2) > 0.992f) c = new Color(0.95f, 0.45f, 0.12f);
                c.a = 1f;
                return c;
            });
        }

        /// <summary>Molten lava: bright channels between a dark crust (the albedo; UhLavaGlow is the emission).</summary>
        public static Texture2D UhLava()
        {
            return Make("uhlava", 256, 256, true, (x, y, u, v) =>
            {
                float n = Fbm(u, v, 4, 5, 491);
                float flow = Mathf.Abs(Mathf.Sin((v * 3f + n * 1.8f) * Mathf.PI * 2f));
                float hot = Mathf.Clamp01(1f - flow * 2.2f + (n - 0.5f));
                Color c = Color.Lerp(new Color(0.12f, 0.04f, 0.02f), new Color(1f, 0.55f, 0.12f), hot);
                c.a = 1f;
                return c;
            });
        }

        public static Texture2D UhLavaGlow()
        {
            return Make("uhlavaglow", 256, 256, true, (x, y, u, v) =>
            {
                float n = Fbm(u, v, 4, 5, 491);
                float flow = Mathf.Abs(Mathf.Sin((v * 3f + n * 1.8f) * Mathf.PI * 2f));
                float hot = Mathf.Clamp01(1f - flow * 2.2f + (n - 0.5f));
                return new Color(1f * hot, 0.42f * hot, 0.08f * hot, 1f);
            });
        }

        /// <summary>Yellow and black hazard stripes.</summary>
        public static Texture2D UhStripes()
        {
            return Make("uhstripes", 64, 64, true, (x, y, u, v) =>
            {
                bool yellow = ((x + y) / 16) % 2 == 0;
                Color c = yellow ? new Color(0.95f, 0.72f, 0.1f) : new Color(0.06f, 0.06f, 0.06f);
                c.a = 1f;
                return c;
            });
        }

        /// <summary>Woven blanket: two-colour weave with a border.</summary>
        public static Texture2D UhWeave()
        {
            return Make("uhweave", 128, 128, true, (x, y, u, v) =>
            {
                bool warp = (x / 4 + y / 4) % 2 == 0;
                Color a = new Color(0.55f, 0.22f, 0.16f), b = new Color(0.82f, 0.62f, 0.32f);
                Color c = warp ? a : b;
                if (y % 64 < 6) c = new Color(0.18f, 0.32f, 0.38f);
                c *= 0.9f + MathUtil.Hash01(x, y) * 0.12f;
                c.a = 1f;
                return c;
            });
        }
    }
}
