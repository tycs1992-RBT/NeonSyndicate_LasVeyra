using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Materials for the Frostline and the Inner World. They use the same Standard shader setups as the rest of the city
    /// (albedo, normal map, emission; no new keyword combinations), so they ship with the existing shader-keep materials.
    /// </summary>
    public static partial class Mats
    {
        // ------------------------------------------------------------------ Frostline

        /// <summary>Deep snow: drift-rippled albedo and normals, faint glitter from an emission map.</summary>
        public static Material Snow
        {
            get
            {
                return Cached("snow", () =>
                {
                    Material m = Textured("Snow", Tex.Snow(), Color.white, 0.32f, 0f, Tex.NormalFrom(Tex.SnowBump(), 2.4f), 0.45f);
                    SetEmission(m, new Color(0.38f, 0.42f, 0.5f), Tex.SnowSparkleMap());
                    return m;
                });
            }
        }

        /// <summary>The soft edge of the snow: sand under blotches of thin snow.</summary>
        public static Material SnowThin { get { return Cached("snowthin", () => Textured("Frost Dusted Sand", Tex.SnowThin(), Color.white, 0.1f, 0f, Tex.NormalFrom(Tex.SnowThin(), 1.3f), 0.35f)); } }

        /// <summary>Flat snow for low-poly shapes without texture coordinates (mountain caps, snow on rocks and roofs).</summary>
        public static Material SnowPeak { get { return Cached("snowpeak", () => Standard("Snow Cap", new Color(0.86f, 0.89f, 0.95f), 0.3f, 0f)); } }

        /// <summary>Rimeglass Lake: glossy blue ice with fracture lines.</summary>
        public static Material Ice { get { return Cached("lakeice", () => Textured("Lake Ice", Tex.Ice(), new Color(0.92f, 0.97f, 1f), 0.9f, 0.05f, Tex.NormalFrom(Tex.IceBump(), 2f), 0.3f)); } }

        /// <summary>Glacier ice for the Titan Hollow pillars: glossy, with a cold inner glow that reads at night.</summary>
        public static Material GlacierIce
        {
            get
            {
                return Cached("glacierice", () =>
                {
                    Material m = Standard("Glacier Ice", new Color(0.58f, 0.78f, 0.9f), 0.9f, 0.1f);
                    SetEmission(m, new Color(0.06f, 0.16f, 0.24f), null);
                    return m;
                });
            }
        }

        /// <summary>Glowing ice crystal clusters out in the snow.</summary>
        public static Material IceCrystal
        {
            get
            {
                return Cached("icecrystal", () =>
                {
                    Material m = Standard("Ice Crystal", new Color(0.55f, 0.88f, 1f), 0.93f, 0.15f);
                    SetEmission(m, new Color(0.22f, 0.62f, 0.95f) * 0.9f, null);
                    return m;
                });
            }
        }

        public static Material PineNeedles { get { return Cached("pineneedles", () => Standard("Pine Needles", new Color(0.09f, 0.22f, 0.15f), 0.08f, 0f)); } }
        public static Material FrostRock { get { return Cached("frostrock", () => Standard("Granite", new Color(0.4f, 0.42f, 0.47f), 0.14f, 0f)); } }
        public static Material WardenStone { get { return Cached("wardenstone", () => Standard("Warden Stone", new Color(0.34f, 0.36f, 0.4f), 0.1f, 0f)); } }
        public static Material WardenBanner { get { return Cached("wardenbanner", () => Textured("Warden Banner", Tex.WardenBanner(), Color.white, 0.15f, 0f, null, 0f)); } }

        /// <summary>Snowflakes for the Frostline snowfall particles.</summary>
        public static Material Snowflake { get { return Cached("snowflake", () => SpriteMat("Snowflake", new Color(1f, 1f, 1f, 0.85f), Tex.SoftDot())); } }

        // ------------------------------------------------------------------ Inner World

        /// <summary>Glowing violet crystal growths of the Inner World.</summary>
        public static Material InnerCrystal
        {
            get
            {
                return Cached("innercrystal", () =>
                {
                    Material m = Standard("Veil Crystal", new Color(0.5f, 0.14f, 0.62f), 0.92f, 0.2f);
                    SetEmission(m, new Color(0.85f, 0.25f, 1f) * 1.5f, null);
                    return m;
                });
            }
        }

        /// <summary>The crimson variant of the Inner World crystals.</summary>
        public static Material InnerCrystalCrimson
        {
            get
            {
                return Cached("innercrystalcrimson", () =>
                {
                    Material m = Standard("Veil Crystal Crimson", new Color(0.62f, 0.08f, 0.2f), 0.9f, 0.2f);
                    SetEmission(m, new Color(1f, 0.16f, 0.35f) * 1.4f, null);
                    return m;
                });
            }
        }

        /// <summary>Dark violet rock of the floating islands.</summary>
        public static Material InnerRock { get { return Cached("innerrock", () => Standard("Veil Rock", new Color(0.13f, 0.08f, 0.15f), 0.3f, 0f)); } }

        /// <summary>
        /// Tints the street lamp heads from their warm sodium glow toward Inner World violet (0 = normal, 1 = fully violet).
        /// Takes effect with the next SetNightFactor call.
        /// </summary>
        public static void SetLampTint(float inner)
        {
            Material lamp;
            if (!_cache.TryGetValue("lamphead", out lamp) || lamp == null) return;
            int i = _nightEmissive.IndexOf(lamp);
            if (i < 0) return;
            Color warm = new Color(1f, 0.72f, 0.38f) * 2.2f;
            Color violet = new Color(0.72f, 0.25f, 1f) * 2.6f;
            _nightEmissiveColors[i] = Color.Lerp(warm, violet, Mathf.Clamp01(inner));
        }
    }
}
