using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Materials for the Underheart (04E). They reuse the Standard (Built-in) and Lit (URP) setups the city already ships
    /// (albedo, normal map, emission, fade transparency), so no new shader variants are needed. Shared and cached: the
    /// builder merges geometry per material, so each region draws in a handful of calls.
    /// </summary>
    public static partial class Mats
    {
        /// <summary>Cave rock (palettes as Tex.UhRock: 0 basalt, 1 ember, 2 cool, 3 ash, 4 foundry). Warm palettes glow faintly
        /// along their mineral veins.</summary>
        public static Material UhRock(int palette)
        {
            return Cached("uh_rock" + palette, () =>
            {
                Texture2D t = Tex.UhRock(palette);
                Material m = Textured("Underheart Rock " + palette, t, Color.white, palette == 3 ? 0.08f : 0.16f, 0f, Tex.NormalFrom(t, 3.2f), 0.85f);
                if (palette == 0 || palette == 1 || palette == 4) SetEmission(m, new Color(0.7f, 0.32f, 0.1f) * 0.9f, Tex.UhRockVeins(palette));
                else if (palette == 2) SetEmission(m, new Color(0.2f, 0.5f, 0.5f) * 0.5f, Tex.UhRockVeins(palette));
                return m;
            });
        }

        public static Material UhConcrete { get { return Cached("uh_concrete", () => Textured("Underheart Concrete", Tex.UhConcrete(), Color.white, 0.12f, 0f, Tex.NormalFrom(Tex.UhConcrete(), 1.6f), 0.4f)); } }
        public static Material UhConcreteFloor { get { return Cached("uh_concretefloor", () => Textured("Underheart Service Floor", Tex.UhConcrete(), new Color(0.72f, 0.78f, 0.74f), 0.28f, 0f, null, 0f)); } }
        public static Material UhTile { get { return Cached("uh_tile", () => Textured("Underheart Transit Tile", Tex.UhTile(), Color.white, 0.62f, 0f, Tex.NormalFrom(Tex.UhTile(), 1.2f), 0.3f)); } }
        public static Material UhTerrazzo { get { return Cached("uh_terrazzo", () => Textured("Underheart Terrazzo", Tex.UhTerrazzo(), Color.white, 0.45f, 0f, null, 0f)); } }
        public static Material UhGrating { get { return Cached("uh_grating", () => Textured("Underheart Grating", Tex.UhGrating(), Color.white, 0.5f, 0.7f, Tex.NormalFrom(Tex.UhGrating(), 3f), 0.6f)); } }
        public static Material UhBrick { get { return Cached("uh_brick", () => Textured("Underheart Iron Brick", Tex.UhBrick(), Color.white, 0.14f, 0f, Tex.NormalFrom(Tex.UhBrick(), 2.4f), 0.7f)); } }
        public static Material UhPlate { get { return Cached("uh_plate", () => Textured("Underheart Riveted Plate", Tex.UhPlate(), Color.white, 0.42f, 0.75f, Tex.NormalFrom(Tex.UhPlate(), 2f), 0.5f)); } }
        public static Material UhMarble { get { return Cached("uh_marble", () => Textured("Underheart Archive Stone", Tex.UhMarble(), Color.white, 0.5f, 0f, Tex.NormalFrom(Tex.UhMarble(), 1.4f), 0.3f)); } }
        public static Material UhAsh { get { return Cached("uh_ash", () => Textured("Underheart Ash", Tex.UhAsh(), Color.white, 0.04f, 0f, Tex.NormalFrom(Tex.UhAsh(), 1.8f), 0.5f)); } }
        public static Material UhStripes { get { return Cached("uh_stripes", () => Textured("Underheart Hazard Stripes", Tex.UhStripes(), Color.white, 0.3f, 0f, null, 0f)); } }
        public static Material UhWeave { get { return Cached("uh_weave", () => Textured("Underheart Blanket", Tex.UhWeave(), Color.white, 0.02f, 0f, null, 0f)); } }

        public static Material UhSoil
        {
            get
            {
                return Cached("uh_soil", () =>
                {
                    Material m = Textured("Underheart Ember Soil", Tex.UhSoil(), Color.white, 0.06f, 0f, Tex.NormalFrom(Tex.UhSoil(), 2f), 0.6f);
                    SetEmission(m, new Color(0.5f, 0.2f, 0.05f), Tex.UhSoil());
                    return m;
                });
            }
        }

        /// <summary>Molten lava: bright flow lines that glow (UhScrollLava moves them).</summary>
        public static Material UhLava
        {
            get
            {
                return Cached("uh_lava", () =>
                {
                    Material m = Textured("Underheart Lava", Tex.UhLava(), Color.white, 0.55f, 0f, null, 0f);
                    SetEmission(m, new Color(1f, 0.55f, 0.2f) * 2.4f, Tex.UhLavaGlow());
                    return m;
                });
            }
        }

        /// <summary>Slides the lava texture so the channels visibly flow (one shared material, called once per frame).</summary>
        public static void UhScrollLava(float time)
        {
            Material m;
            if (!_cache.TryGetValue("uh_lava", out m) || m == null) return;
            m.mainTextureOffset = new Vector2(time * 0.012f, time * 0.035f);
        }

        /// <summary>Crystal glow: 0 ember orange, 1 cool teal (refuge lanterns), 2 gold, 3 red, 4 white-hot core, 5 blue cooling.</summary>
        public static Material UhCrystal(int kind)
        {
            return Cached("uh_crystal" + kind, () =>
            {
                Color c, e;
                float i;
                switch (kind)
                {
                    case 1: c = new Color(0.45f, 0.95f, 0.88f); e = new Color(0.3f, 0.9f, 0.85f); i = 2.2f; break;
                    case 2: c = new Color(1f, 0.82f, 0.4f); e = new Color(1f, 0.72f, 0.3f); i = 2.0f; break;
                    case 3: c = new Color(1f, 0.25f, 0.18f); e = new Color(1f, 0.2f, 0.12f); i = 2.2f; break;
                    case 4: c = new Color(1f, 0.9f, 0.7f); e = new Color(1f, 0.78f, 0.5f); i = 4.5f; break;
                    case 5: c = new Color(0.4f, 0.75f, 1f); e = new Color(0.25f, 0.6f, 1f); i = 2.6f; break;
                    default: c = new Color(1f, 0.5f, 0.15f); e = new Color(1f, 0.45f, 0.12f); i = 2.4f; break;
                }
                Material m = Standard("Underheart Crystal " + kind, c, 0.9f, 0.1f);
                SetEmission(m, e * i, null);
                return m;
            });
        }

        /// <summary>A cooled pool's water surface (fade transparency, glossy blue).</summary>
        public static Material UhWater
        {
            get
            {
                return Cached("uh_water", () =>
                {
                    Material m = Standard("Underheart Cooled Pool", new Color(0.16f, 0.42f, 0.55f, 0.62f), 0.95f, 0f);
                    MakeTransparent(m, true);
                    SetEmission(m, new Color(0.04f, 0.14f, 0.2f), null);
                    return m;
                });
            }
        }

        /// <summary>The heat warning ring around vents and lava edges (the runtime pulses it per renderer).</summary>
        public static Material UhHeatRing
        {
            get
            {
                return Cached("uh_heatring", () =>
                {
                    Material m = Standard("Underheart Heat Ring", new Color(1f, 0.35f, 0.1f), 0.3f, 0f);
                    SetEmission(m, new Color(1f, 0.3f, 0.06f) * 1.6f, null);
                    return m;
                });
            }
        }

        /// <summary>The blue ring around a cooling vent.</summary>
        public static Material UhCoolRing
        {
            get
            {
                return Cached("uh_coolring", () =>
                {
                    Material m = Standard("Underheart Cool Ring", new Color(0.3f, 0.7f, 1f), 0.4f, 0f);
                    SetEmission(m, new Color(0.25f, 0.6f, 1f) * 1.8f, null);
                    return m;
                });
            }
        }

        public static Material UhRail { get { return Cached("uh_rail", () => Standard("Underheart Safety Rail", new Color(0.92f, 0.7f, 0.14f), 0.45f, 0.3f)); } }
        public static Material UhIron { get { return Cached("uh_iron", () => Standard("Underheart Iron", new Color(0.22f, 0.2f, 0.19f), 0.38f, 0.8f)); } }
        public static Material UhCopper { get { return Cached("uh_copper", () => Standard("Underheart Copper Pipe", new Color(0.62f, 0.36f, 0.22f), 0.55f, 0.85f)); } }
        public static Material UhBlackStone { get { return Cached("uh_blackstone", () => Standard("Underheart Black Stone", new Color(0.08f, 0.08f, 0.09f), 0.62f, 0f)); } }
        public static Material UhCloth(int kind)
        {
            Color c = kind == 1 ? new Color(0.24f, 0.36f, 0.42f) : (kind == 2 ? new Color(0.6f, 0.5f, 0.32f) : new Color(0.48f, 0.2f, 0.16f));
            return Cached("uh_cloth" + kind, () => Standard("Underheart Cloth " + kind, c, 0.02f, 0f));
        }
    }
}
