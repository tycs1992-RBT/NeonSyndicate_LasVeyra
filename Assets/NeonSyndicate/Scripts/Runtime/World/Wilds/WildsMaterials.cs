using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The materials of the wilds, one per WildMat: the city's own procedural materials (rock, sand, dirt, grass, bark,
    /// leaves, wood, metal, water, ice, snow) tinted per land where a land needs its own colour, and a few plain or glowing
    /// colours (bone, cloth, rust, quartz, embers, runes). Built once per session on the active render pipeline.
    /// </summary>
    public static class WildsMaterials
    {
        private static readonly Dictionary<WildMat, Material> Cache = new Dictionary<WildMat, Material>();

        public static void ResetStatics()
        {
            Cache.Clear();
        }

        /// <summary>The material of one slot (never null; a plain grey if anything fails).</summary>
        public static Material Get(WildMat slot)
        {
            Material m;
            if (Cache.TryGetValue(slot, out m) && m != null) return m;
            try { m = Make(slot); }
            catch (System.Exception e)
            {
                Log.Exception("Wilds material " + slot, e);
                m = null;
            }
            if (m == null) m = Mats.Solid(new Color(0.5f, 0.5f, 0.5f), 0.2f);
            Cache[slot] = m;
            return m;
        }

        private static Material Tinted(Material source, string name, Color tint)
        {
            Material m = new Material(source);
            m.name = name;
            m.color = new Color(source.color.r * tint.r, source.color.g * tint.g, source.color.b * tint.b, source.color.a);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", m.color);
            return m;
        }

        private static Material Make(WildMat slot)
        {
            switch (slot)
            {
                case WildMat.RedRock: return Tinted(Mats.Rock, "Wilds Red Sandstone", new Color(0.92f, 0.52f, 0.36f));
                case WildMat.PaleRock: return Tinted(Mats.Rock, "Wilds Pale Sandstone", new Color(1.02f, 0.88f, 0.7f));
                case WildMat.DarkRock: return Tinted(Mats.Rock, "Wilds Glass Basalt", new Color(0.42f, 0.38f, 0.48f));
                case WildMat.GreyRock: return Mats.Rock;
                case WildMat.FrostRock: return Mats.FrostRock;
                case WildMat.Sand: return Mats.Sand;
                case WildMat.Crystal:
                {
                    Material m = Mats.Standard("Wilds Quartz", new Color(0.78f, 0.62f, 1f), 0.92f, 0.1f);
                    Mats.SetEmission(m, new Color(0.42f, 0.26f, 0.95f) * 0.9f, null);
                    return m;
                }
                case WildMat.Bark: return Mats.PalmTrunk;
                case WildMat.Leaf: return Mats.TreeLeaf;
                case WildMat.DryGrass: return Mats.Solid(new Color(0.6f, 0.53f, 0.3f), 0.08f);
                case WildMat.Juniper: return Mats.PineNeedles;
                case WildMat.PalmLeaf: return Mats.PalmLeaf;
                case WildMat.Bone: return Mats.Solid(new Color(0.86f, 0.82f, 0.72f), 0.25f);
                case WildMat.Stone: return Tinted(Mats.Concrete, "Wilds Old Stone", new Color(0.92f, 0.86f, 0.74f));
                case WildMat.Adobe: return Tinted(Mats.Concrete, "Wilds Adobe", new Color(0.95f, 0.72f, 0.52f));
                case WildMat.Wood: return Mats.Wood;
                case WildMat.Cloth: return Mats.Solid(new Color(0.7f, 0.62f, 0.48f), 0.05f);
                case WildMat.ClothRed: return Mats.Solid(new Color(0.6f, 0.16f, 0.12f), 0.05f);
                case WildMat.Metal: return Mats.Metal;
                case WildMat.Rust: return Mats.Solid(new Color(0.42f, 0.22f, 0.12f), 0.2f);
                case WildMat.Water: return Mats.Water;
                case WildMat.Ice: return Mats.Ice;
                case WildMat.Snow: return Mats.SnowPeak;
                case WildMat.Ember: return Mats.Emissive(new Color(1f, 0.42f, 0.1f), 3f);
                case WildMat.Glow: return Mats.Emissive(new Color(0.3f, 0.9f, 1f), 2.5f);
                case WildMat.Salt: return Mats.Solid(new Color(0.9f, 0.88f, 0.84f), 0.3f);
                case WildMat.Shadow: return Mats.Solid(new Color(0.02f, 0.02f, 0.02f), 0f);
                case WildMat.Dirt: return Tinted(Mats.Dirt, "Wilds Red Earth", new Color(1f, 0.66f, 0.5f));
                case WildMat.DryTurf: return Tinted(Mats.Grass, "Wilds Dry Turf", new Color(0.85f, 0.78f, 0.48f));
                case WildMat.GlassSand: return Tinted(Mats.Sand, "Wilds Glass Sand", new Color(0.58f, 0.54f, 0.66f));
                default: return null;
            }
        }
    }
}
