using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Pure colouring of the procedural PBR surfaces: the albedo per kind from its fields (the same palettes the old
    /// Tex generators used, now driven by the relief so paint, relief and shading agree), the per-texel smoothness
    /// variation (wet puddles, rough rust, matte grout, polished edges) and the per-texel metallic where it varies
    /// (rust, stone chips). Wear is applied to every kind: dirt darkens cavities and edges catch light. Micro kinds for
    /// small tinted objects return near-white albedo so the material colour does the tinting.
    /// </summary>
    public static class SurfaceColor
    {
        /// <summary>The relative smoothness a texel may gain over the nominal value on a wet-registered surface, so the
        /// rain sheen (which scales smoothness up to 0.82 over the nominal) never exceeds 1.</summary>
        public const float WetTarget = 0.82f;

        /// <summary>Normal map slope strength per kind (a 1024 field with the same strength keeps the same slope).</summary>
        public static float NormalStrength(SurfaceKind kind, int size)
        {
            float s;
            switch (kind)
            {
                case SurfaceKind.Asphalt: s = 1.6f; break;
                case SurfaceKind.Pavers: s = 2.4f; break;
                case SurfaceKind.Concrete: s = 1.4f; break;
                case SurfaceKind.Grass: s = 1.2f; break;
                case SurfaceKind.Sand: s = 1.6f; break;
                case SurfaceKind.Dirt: s = 2f; break;
                case SurfaceKind.Rock: s = 3f; break;
                case SurfaceKind.Gravel: s = 2.4f; break;
                case SurfaceKind.RoofTiles: s = 2.4f; break;
                case SurfaceKind.Corrugated: s = 3.2f; break;
                case SurfaceKind.Wood: s = 1.6f; break;
                case SurfaceKind.Bark: s = 2.6f; break;
                case SurfaceKind.SnowCrust: s = 1.8f; break;
                case SurfaceKind.SnowThin: s = 1.5f; break;
                case SurfaceKind.Ice: s = 2f; break;
                case SurfaceKind.Ripples: s = 1.2f; break;
                case SurfaceKind.Brushed: s = 0.5f; break;
                case SurfaceKind.Grain: s = 0.6f; break;
                case SurfaceKind.Pores: s = 1.2f; break;
                case SurfaceKind.PaintPeel: s = 0.5f; break;
                case SurfaceKind.Fibre: s = 1.4f; break;
                case SurfaceKind.Leaf: s = 1.6f; break;
                case SurfaceKind.UhRock: s = 3.2f; break;
                case SurfaceKind.UhConcrete: s = 1.6f; break;
                case SurfaceKind.UhTile: s = 2f; break;
                case SurfaceKind.UhTerrazzo: s = 0.8f; break;
                case SurfaceKind.UhGrating: s = 3f; break;
                case SurfaceKind.UhBrick: s = 2.4f; break;
                case SurfaceKind.UhPlate: s = 2.2f; break;
                case SurfaceKind.UhMarble: s = 1f; break;
                case SurfaceKind.UhAsh: s = 1.6f; break;
                case SurfaceKind.UhSoil: s = 2.2f; break;
                case SurfaceKind.UhLava: s = 2.4f; break;
                default: s = 1.8f; break;
            }
            // The slope per texel halves each time the size doubles; scale so the normal reads the same at every size.
            return s * size / 256f;
        }

        /// <summary>How strongly cavities darken the occlusion channel.</summary>
        public static float OcclusionStrength(SurfaceKind kind)
        {
            switch (kind)
            {
                case SurfaceKind.Brushed:
                case SurfaceKind.Grain:
                case SurfaceKind.PaintPeel:
                case SurfaceKind.Ripples:
                case SurfaceKind.UhMarble:
                case SurfaceKind.UhTerrazzo:
                    return 0.35f;
                case SurfaceKind.Ice:
                    return 0.5f;
                default:
                    return 0.85f;
            }
        }

        /// <summary>Gain for the cavity and convexity terms (how much relief counts as a crevice or an edge).</summary>
        public static float ShadeGain(SurfaceKind kind)
        {
            switch (kind)
            {
                case SurfaceKind.UhGrating:
                case SurfaceKind.UhPlate:
                case SurfaceKind.Pavers:
                case SurfaceKind.UhTile:
                case SurfaceKind.UhBrick:
                    return 4f;
                default:
                    return 6f;
            }
        }

        /// <summary>Per-texel smoothness offsets from the nominal value (dirt in crevices, polish on edges, kind features).</summary>
        public static float[] GlossVariation(SurfaceKind kind, SurfaceFields f, SurfaceShade s)
        {
            int count = f.Length;
            float[] g = new float[count];
            for (int i = 0; i < count; i++)
            {
                float mask = f.Mask != null ? f.Mask[i] : 0f;
                float aux = f.Aux != null ? f.Aux[i] : 0f;
                float v = s.Convex[i] * 0.12f - s.Cavity[i] * 0.15f + (f.Tone[i] - 0.5f) * 0.06f;
                switch (kind)
                {
                    case SurfaceKind.Asphalt: v += aux * 0.3f - mask * 0.1f; break;
                    case SurfaceKind.Pavers: v -= aux * 0.1f + mask * 0.1f; break;
                    case SurfaceKind.Concrete:
                    case SurfaceKind.UhConcrete: v += aux * 0.15f - mask * 0.1f; break;
                    case SurfaceKind.Grass: v -= mask * 0.05f; break;
                    case SurfaceKind.Sand: v += aux * 0.08f; break;
                    case SurfaceKind.Dirt: v += aux * 0.1f; break;
                    case SurfaceKind.Rock:
                    case SurfaceKind.UhRock: v += aux * 0.25f - mask * 0.1f; break;
                    case SurfaceKind.Corrugated: v -= mask * 0.35f; break;
                    case SurfaceKind.Wood: v += aux * 0.1f - mask * 0.1f; break;
                    case SurfaceKind.SnowCrust: v -= aux * 0.1f; break;
                    case SurfaceKind.SnowThin: v -= mask * 0.15f; break;
                    case SurfaceKind.Ice: v -= mask * 0.4f + aux * 0.3f; break;
                    case SurfaceKind.Brushed: v -= mask * 0.3f + aux * 0.2f; break;
                    case SurfaceKind.Grain: v -= aux * 0.1f; break;
                    case SurfaceKind.Pores: v -= mask * 0.1f; break;
                    case SurfaceKind.PaintPeel: v -= mask * 0.5f + aux * 0.6f; break;
                    case SurfaceKind.Leaf: v += mask * 0.1f; break;
                    case SurfaceKind.UhTile: v -= mask * 0.5f + aux * 0.1f; break;
                    case SurfaceKind.UhTerrazzo: v += mask * 0.2f - aux * 0.25f; break;
                    case SurfaceKind.UhGrating: v -= (1f - mask) * 0.3f; break;
                    case SurfaceKind.UhBrick: v -= mask * 0.1f + aux * 0.05f; break;
                    case SurfaceKind.UhPlate: v -= aux * 0.2f; break;
                    case SurfaceKind.UhMarble: v -= mask * 0.2f; break;
                    case SurfaceKind.UhAsh: v -= aux * 0.05f; break;
                    case SurfaceKind.UhLava: v += mask * 0.3f; break;
                }
                g[i] = v;
            }
            return g;
        }

        /// <summary>Per-texel metallic, or null when the whole surface keeps the nominal value.</summary>
        public static float[] MetallicVariation(SurfaceKind kind, SurfaceFields f, float metallic)
        {
            if (kind != SurfaceKind.Corrugated && kind != SurfaceKind.PaintPeel && kind != SurfaceKind.UhPlate && kind != SurfaceKind.Brushed) return null;
            int count = f.Length;
            float[] m = new float[count];
            for (int i = 0; i < count; i++)
            {
                float mask = f.Mask != null ? f.Mask[i] : 0f;
                float aux = f.Aux != null ? f.Aux[i] : 0f;
                switch (kind)
                {
                    case SurfaceKind.Corrugated: m[i] = metallic * (1f - mask); break;
                    case SurfaceKind.PaintPeel: m[i] = Mathf.Max(metallic * (1f - mask * 0.6f), aux * 0.85f); break;
                    case SurfaceKind.UhPlate: m[i] = metallic * (1f - aux * 0.35f); break;
                    default: m[i] = metallic * (1f - aux * 0.3f); break;
                }
            }
            return m;
        }

        /// <summary>Absolute smoothness texels: nominal plus variation, clamped; on a wet surface the gain is capped so the
        /// rain scale (WetTarget over nominal) stays at or under 1.</summary>
        public static float[] Smoothness(float nominal, float[] variation, bool wet)
        {
            float[] s = new float[variation.Length];
            float maxUp = wet ? nominal * (1f / WetTarget - 1f) : 1f;
            for (int i = 0; i < s.Length; i++)
            {
                float v = variation[i];
                if (v > maxUp) v = maxUp;
                s[i] = Mathf.Clamp01(nominal + v);
            }
            return s;
        }

        /// <summary>The albedo texels of a kind (palette picks a colour set for kinds that have several).</summary>
        public static Color32[] Albedo(SurfaceKind kind, SurfaceFields f, SurfaceShade s, int palette)
        {
            int n = f.Size;
            Color32[] px = new Color32[f.Length];
            float dirt = Dirt(kind);
            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;
                for (int x = 0; x < n; x++)
                {
                    int i = y * n + x;
                    Color c = Base(kind, f, i, v, palette);
                    float wear = (1f - s.Cavity[i] * dirt) * (1f + s.Convex[i] * 0.25f);
                    c.r = Mathf.Clamp01(c.r * wear);
                    c.g = Mathf.Clamp01(c.g * wear);
                    c.b = Mathf.Clamp01(c.b * wear);
                    px[i] = new Color32(SurfaceMaps.ToByte(c.r), SurfaceMaps.ToByte(c.g), SurfaceMaps.ToByte(c.b), 255);
                }
            }
            return px;
        }

        private static float Dirt(SurfaceKind kind)
        {
            switch (kind)
            {
                case SurfaceKind.Ripples:
                case SurfaceKind.Ice:
                case SurfaceKind.SnowCrust:
                    return 0.12f;
                case SurfaceKind.Brushed:
                case SurfaceKind.Grain:
                case SurfaceKind.PaintPeel:
                case SurfaceKind.UhMarble:
                case SurfaceKind.UhTerrazzo:
                    return 0.2f;
                default:
                    return 0.4f;
            }
        }

        private static Color Lerp(Color a, Color b, float t)
        {
            return Color.Lerp(a, b, Mathf.Clamp01(t));
        }

        private static Color Base(SurfaceKind kind, SurfaceFields f, int i, float v, int palette)
        {
            float h = f.Height[i];
            float tone = f.Tone[i];
            float mask = f.Mask != null ? f.Mask[i] : 0f;
            float aux = f.Aux != null ? f.Aux[i] : 0f;
            Color c;
            float g;
            switch (kind)
            {
                case SurfaceKind.Asphalt:
                    g = 0.13f + tone * 0.09f + (h - 0.55f) * 0.12f;
                    if (mask > 0.3f) g *= 1f - mask * 0.4f;
                    g *= 1f - aux * 0.18f;
                    return new Color(g, g, g * 1.04f);
                case SurfaceKind.Pavers:
                    g = 0.52f + tone * 0.08f + (h - 0.6f) * 0.3f;
                    g *= 1f - mask * 0.45f;
                    g *= 1f - aux * 0.5f;
                    return new Color(g, g * 0.98f, g * 0.94f);
                case SurfaceKind.Concrete:
                    if (palette == 2)
                    {
                        g = 0.6f + tone * 0.06f + (h - 0.6f) * 0.1f;
                        g *= 1f - mask * 0.3f;
                        return new Color(g, g * 0.98f, g * 0.94f);
                    }
                    g = 0.48f + tone * 0.14f + (h - 0.6f) * 0.12f;
                    g *= 1f - mask * 0.3f;
                    c = new Color(g, g * 0.99f, g * 0.96f);
                    return Lerp(c, new Color(0.3f, 0.3f, 0.29f), aux * 0.5f);
                case SurfaceKind.UhConcrete:
                    c = Lerp(new Color(0.42f, 0.42f, 0.41f), new Color(0.58f, 0.57f, 0.55f), tone + (h - 0.6f) * 0.5f);
                    c = Lerp(c, new Color(0.26f, 0.27f, 0.26f), aux * 0.6f);
                    return c * (1f - mask * 0.3f);
                case SurfaceKind.Grass:
                    c = Lerp(new Color(0.12f, 0.30f, 0.10f), new Color(0.28f, 0.45f, 0.16f), tone);
                    if (h > 0.72f) c *= 1.2f;
                    c = Lerp(c, new Color(0.09f, 0.24f, 0.08f), mask * 0.5f);
                    return Lerp(c, new Color(0.36f, 0.27f, 0.18f), aux);
                case SurfaceKind.Sand:
                    c = Lerp(new Color(0.72f, 0.58f, 0.40f), new Color(0.86f, 0.72f, 0.52f), tone * 0.6f + h * 0.4f);
                    if (mask > 0.5f) c *= 0.7f;
                    return c * (1f - aux * 0.15f);
                case SurfaceKind.Dirt:
                    c = Lerp(new Color(0.36f, 0.27f, 0.18f), new Color(0.55f, 0.43f, 0.30f), tone * 0.7f + h * 0.3f);
                    c *= 1f - mask * 0.35f;
                    return Lerp(c, new Color(0.5f, 0.47f, 0.42f), aux);
                case SurfaceKind.Rock:
                    return RockColor(palette, tone, h, mask, aux);
                case SurfaceKind.UhRock:
                    return UhRockColor(palette, tone, h, mask, aux);
                case SurfaceKind.Gravel:
                    g = 0.16f + tone * 0.1f + mask * 0.1f + (h - 0.5f) * 0.12f;
                    return new Color(g, g, g * 1.02f);
                case SurfaceKind.RoofTiles:
                    g = 0.42f + h * 0.55f + (tone - 0.5f) * 0.1f;
                    g *= 1f - mask * 0.3f;
                    return new Color(0.62f * g, 0.28f * g, 0.18f * g);
                case SurfaceKind.Corrugated:
                    g = 0.28f + h * 0.3f + tone * 0.1f - aux * 0.05f;
                    c = new Color(g, g, g * 1.03f);
                    return Lerp(c, new Color(0.45f, 0.22f, 0.1f), mask * 0.9f);
                case SurfaceKind.Wood:
                    g = 0.33f + h * 0.3f + tone * 0.1f;
                    g *= 1f - mask * 0.5f;
                    g *= 1f - aux * 0.35f;
                    return new Color(g * 0.85f, g * 0.6f, g * 0.38f);
                case SurfaceKind.Bark:
                    g = 0.24f + h * 0.22f + tone * 0.08f;
                    g *= 1f - mask * 0.3f;
                    return new Color(g * 1.1f, g * 0.85f, g * 0.6f);
                case SurfaceKind.SnowCrust:
                    c = Lerp(new Color(0.72f, 0.78f, 0.87f), new Color(0.9f, 0.92f, 0.95f), h);
                    c *= 1f - mask * 0.1f;
                    if (aux > 0.5f) c = Color.white;
                    return c;
                case SurfaceKind.SnowThin:
                    c = Lerp(new Color(0.72f, 0.58f, 0.40f), new Color(0.86f, 0.72f, 0.52f), tone * 0.6f + h * 0.4f);
                    c = Lerp(c, new Color(0.62f, 0.6f, 0.58f), 0.2f);
                    return Lerp(c, Lerp(new Color(0.78f, 0.82f, 0.89f), new Color(0.9f, 0.92f, 0.95f), tone), mask);
                case SurfaceKind.Ice:
                    c = Lerp(new Color(0.32f, 0.46f, 0.58f), new Color(0.55f, 0.72f, 0.84f), tone);
                    c = Lerp(c, new Color(0.82f, 0.88f, 0.94f), aux);
                    return Lerp(c, new Color(0.9f, 0.96f, 1f), mask);
                case SurfaceKind.Ripples:
                    return Color.white;
                case SurfaceKind.Brushed:
                    g = 0.92f + tone * 0.08f - mask * 0.12f - aux * 0.1f;
                    return new Color(g, g, g);
                case SurfaceKind.Grain:
                    g = 0.94f + h * 0.06f - aux * 0.08f;
                    return new Color(g, g, g);
                case SurfaceKind.Pores:
                    g = 0.9f + (1f - mask) * 0.1f;
                    return new Color(g, g, g);
                case SurfaceKind.PaintPeel:
                    c = new Color(1f - mask * 0.45f, 1f - mask * 0.5f, 1f - mask * 0.56f);
                    return Lerp(c, new Color(0.5f, 0.5f, 0.52f), aux);
                case SurfaceKind.Fibre:
                    g = 0.85f + h * 0.15f;
                    return new Color(g, g, g);
                case SurfaceKind.Leaf:
                    g = 0.88f + h * 0.1f + mask * 0.14f;
                    return new Color(g * 0.98f, g, g * 0.95f);
                case SurfaceKind.UhTile:
                    c = v < 0.1875f ? new Color(0.20f, 0.36f, 0.34f) : new Color(0.62f, 0.74f, 0.68f);
                    c *= 0.88f + tone * 0.2f + (h - 0.7f) * 0.3f;
                    c = Lerp(c, new Color(0.24f, 0.25f, 0.24f), mask);
                    return c * (1f - aux * 0.08f);
                case SurfaceKind.UhTerrazzo:
                    c = Lerp(new Color(0.48f, 0.47f, 0.45f), new Color(0.62f, 0.61f, 0.58f), tone);
                    if (mask > 0.75f) c = new Color(0.25f, 0.42f, 0.42f);
                    else if (mask > 0.1f) c = new Color(0.75f, 0.72f, 0.66f);
                    return c * (1f - aux * 0.1f);
                case SurfaceKind.UhGrating:
                    if (mask < 0.5f) return new Color(0.05f, 0.05f, 0.05f);
                    return h < 0.8f ? new Color(0.62f, 0.58f, 0.52f) : new Color(0.42f, 0.40f, 0.38f);
                case SurfaceKind.UhBrick:
                    c = Lerp(new Color(0.30f, 0.12f, 0.08f), new Color(0.50f, 0.22f, 0.14f), tone * 0.6f + h * 0.4f);
                    c = Lerp(c, new Color(0.08f, 0.07f, 0.07f), aux * 0.6f);
                    return Lerp(c, new Color(0.28f, 0.26f, 0.24f), mask);
                case SurfaceKind.UhPlate:
                    c = Lerp(new Color(0.16f, 0.14f, 0.13f), new Color(0.32f, 0.27f, 0.24f), tone);
                    c = Lerp(c, new Color(0.35f, 0.18f, 0.24f), aux * 0.5f);
                    if (mask > 0.3f && h > 0.65f) c = Lerp(c, new Color(0.5f, 0.46f, 0.4f), mask);
                    else if (mask > 0.3f) c *= 0.5f;
                    return c;
                case SurfaceKind.UhMarble:
                    c = Lerp(new Color(0.78f, 0.77f, 0.74f), new Color(0.88f, 0.87f, 0.84f), tone);
                    return Lerp(c, new Color(0.45f, 0.44f, 0.46f), mask * 0.6f);
                case SurfaceKind.UhAsh:
                    c = Lerp(new Color(0.55f, 0.54f, 0.52f), new Color(0.72f, 0.71f, 0.68f), tone);
                    c *= 1f - mask * 0.14f;
                    return Lerp(c, new Color(0.15f, 0.12f, 0.1f), aux);
                case SurfaceKind.UhSoil:
                    c = Lerp(new Color(0.10f, 0.06f, 0.04f), new Color(0.26f, 0.15f, 0.09f), tone * 0.7f + h * 0.3f);
                    c *= 1f - mask * 0.35f;
                    return Lerp(c, new Color(0.95f, 0.45f, 0.12f), aux);
                case SurfaceKind.UhLava:
                    return Lerp(new Color(0.12f, 0.04f, 0.02f), new Color(1f, 0.55f, 0.12f), mask);
                default:
                    c = mask > 0.5f ? new Color(0.55f, 0.22f, 0.16f) : new Color(0.82f, 0.62f, 0.32f);
                    if (aux > 0.5f) c = new Color(0.18f, 0.32f, 0.38f);
                    return c * (0.86f + h * 0.2f);
            }
        }

        /// <summary>Rock palettes: 0 badlands sandstone, 1 Frostline granite, 2 warden stone, 3 Veil rock.</summary>
        private static Color RockColor(int palette, float tone, float h, float fracture, float vein)
        {
            Color dark, light, veinColor;
            switch (palette)
            {
                case 1: dark = new Color(0.34f, 0.36f, 0.41f); light = new Color(0.52f, 0.54f, 0.58f); veinColor = new Color(0.7f, 0.7f, 0.72f); break;
                case 2: dark = new Color(0.28f, 0.3f, 0.34f); light = new Color(0.42f, 0.44f, 0.48f); veinColor = new Color(0.55f, 0.56f, 0.6f); break;
                case 3: dark = new Color(0.09f, 0.05f, 0.11f); light = new Color(0.2f, 0.13f, 0.24f); veinColor = new Color(0.45f, 0.2f, 0.55f); break;
                default: dark = new Color(0.48f, 0.30f, 0.20f); light = new Color(0.72f, 0.50f, 0.34f); veinColor = new Color(0.82f, 0.68f, 0.5f); break;
            }
            Color c = Lerp(dark, light, tone * 0.5f + h * 0.5f);
            c *= 1f - fracture * 0.45f;
            return Lerp(c, veinColor, vein * 0.4f);
        }

        /// <summary>Underheart rock palettes: 0 basalt, 1 ember, 2 cool, 3 ash, 4 foundry (the vein colour glows).</summary>
        public static Color UhRockVein(int palette)
        {
            switch (palette)
            {
                case 1: return new Color(1f, 0.45f, 0.12f);
                case 2: return new Color(0.45f, 0.85f, 0.85f);
                case 3: return new Color(0.86f, 0.84f, 0.80f);
                case 4: return new Color(1f, 0.38f, 0.10f);
                default: return new Color(0.95f, 0.42f, 0.12f);
            }
        }

        private static Color UhRockColor(int palette, float tone, float h, float fracture, float vein)
        {
            Color dark, light;
            switch (palette)
            {
                case 1: dark = new Color(0.20f, 0.10f, 0.07f); light = new Color(0.46f, 0.26f, 0.17f); break;
                case 2: dark = new Color(0.16f, 0.18f, 0.20f); light = new Color(0.38f, 0.42f, 0.44f); break;
                case 3: dark = new Color(0.42f, 0.41f, 0.40f); light = new Color(0.74f, 0.72f, 0.70f); break;
                case 4: dark = new Color(0.16f, 0.08f, 0.06f); light = new Color(0.36f, 0.20f, 0.14f); break;
                default: dark = new Color(0.07f, 0.07f, 0.08f); light = new Color(0.24f, 0.23f, 0.24f); break;
            }
            Color c = Lerp(dark, light, tone * 0.55f + h * 0.45f);
            c *= 1f - fracture * 0.45f;
            if (palette != 3) c = Lerp(c, UhRockVein(palette), vein * 0.55f);
            return c;
        }
    }
}
