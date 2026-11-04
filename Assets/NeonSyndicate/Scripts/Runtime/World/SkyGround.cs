using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>What the ground is made of under a point, for the bounce light.</summary>
    public enum GroundKind
    {
        Asphalt,
        Concrete,
        Pavers,
        Grass,
        Sand,
        Snow,
        Water,
        Rock
    }

    /// <summary>
    /// The ground under the camera for the bounce light (engine-free, tested by SkyMathTests): classifies a map point
    /// from the city layout (snow, the canal, the desert, roads, parks, plazas and sidewalks), gives each kind a linear
    /// albedo in the colour of its material, and averages a cross of samples so the colour does not jump at a curb.
    /// Atmosphere.Bounce.cs feeds the result to SkyMath.GroundRadiance for the sky dome's lower half and the Trilight
    /// ground colour, which is how the sand's warmth, the snow's cold brightness and the park's green reach people and walls.
    /// </summary>
    public static class SkyGround
    {
        /// <summary>Linear albedo of each ground kind (the colour of the procedural material, a little darker as surfaces are).</summary>
        public static Color Albedo(GroundKind kind)
        {
            switch (kind)
            {
                case GroundKind.Concrete: return new Color(0.32f, 0.31f, 0.29f);
                case GroundKind.Pavers: return new Color(0.30f, 0.28f, 0.26f);
                case GroundKind.Grass: return new Color(0.11f, 0.20f, 0.07f);
                // H8S realism: the open ground is weathered mineral earth now (was 0.44, 0.36, 0.25 for bright sand).
                case GroundKind.Sand: return new Color(0.19f, 0.165f, 0.135f);
                case GroundKind.Snow: return new Color(0.66f, 0.70f, 0.78f);
                case GroundKind.Water: return new Color(0.05f, 0.11f, 0.13f);
                case GroundKind.Rock: return new Color(0.26f, 0.23f, 0.20f);
                default: return new Color(0.085f, 0.085f, 0.09f);
            }
        }

        /// <summary>The ground kind at a map point (the layout may be null: then the city's asphalt).</summary>
        public static GroundKind Classify(CityLayout layout, float x, float z)
        {
            if (CityLayout.IsSnowGround(x, z)) return GroundKind.Snow;
            bool inCore = Mathf.Abs(x) <= CityLayout.CoreHalf && Mathf.Abs(z) <= CityLayout.CoreHalf;
            if (inCore && Mathf.Abs(z - CityLayout.CanalZ) <= CityLayout.CanalHalfWidth) return GroundKind.Water;
            District d = CityLayout.DistrictAt(x, z);
            if (d == District.QuartzBadlands) return GroundKind.Sand;
            if (d == District.Frostline) return GroundKind.Rock;
            if (layout == null) return GroundKind.Asphalt;
            Vector3 p = new Vector3(x, 0f, z);
            if (layout.RoadAt(p, 0f) != null) return GroundKind.Asphalt;
            Block b = layout.BlockAt(p);
            if (b == null) return GroundKind.Asphalt;
            if (b.IsPark) return GroundKind.Grass;
            if (b.Inner.Contains(x, z))
            {
                foreach (Lot lot in b.Lots)
                {
                    if (!lot.Area.Contains(x, z)) continue;
                    switch (lot.Use)
                    {
                        case LotUse.Park: return GroundKind.Grass;
                        case LotUse.Parking: return GroundKind.Asphalt;
                        case LotUse.Plaza: return GroundKind.Pavers;
                        default: return GroundKind.Concrete;
                    }
                }
                return GroundKind.Concrete;
            }
            return GroundKind.Pavers;
        }

        /// <summary>
        /// The albedo under a point averaged over a cross of five samples (the point and four at the given radius), so a
        /// camera on the curb sees a mix of road and sidewalk rather than one or the other.
        /// </summary>
        public static Color SampleAlbedo(CityLayout layout, float x, float z, float radius)
        {
            Color sum = Albedo(Classify(layout, x, z)) * 2f;
            sum += Albedo(Classify(layout, x + radius, z));
            sum += Albedo(Classify(layout, x - radius, z));
            sum += Albedo(Classify(layout, x, z + radius));
            sum += Albedo(Classify(layout, x, z - radius));
            Color c = sum / 6f;
            c.a = 1f;
            return c;
        }
    }
}
