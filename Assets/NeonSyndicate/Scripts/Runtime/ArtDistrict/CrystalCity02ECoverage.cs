using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Pure coverage and geometry budgets. Detail quality must never decide whether a normal building is themed.</summary>
    public static class CrystalCity02ECoverage
    {
        public const int MaximumBays = 4;
        public const int MaximumRegisters = 2;
        public const float FrameProjection = 0.10f;

        public static bool CanTheme(Building building, Lot lot)
        {
            if (building == null || lot == null || lot.CustomBuilt) return false;
            RectF f = building.Footprint;
            return f.Valid && Finite(f.X0) && Finite(f.X1) && Finite(f.Z0) && Finite(f.Z1) &&
                f.Width >= 3f && f.Depth >= 3f && Finite(building.Height) && building.Height > 0.5f;
        }

        public static bool IsCityProp(Vector3 p)
        {
            return Finite(p.x) && Finite(p.y) && Finite(p.z) &&
                Mathf.Abs(p.x) <= CityLayout.RingHalf && Mathf.Abs(p.z) <= CityLayout.RingHalf;
        }

        public static int Bays(float width, CrystalCityStyle style)
        {
            if (!Finite(width) || width < 2f) return 0;
            float spacing = style == CrystalCityStyle.VerdantReach ? 11f :
                (style == CrystalCityStyle.DawnstoneCourt ? 8f : 9f);
            return Mathf.Clamp(Mathf.FloorToInt(width / spacing), 1, MaximumBays);
        }

        public static int Registers(float height)
        {
            if (!Finite(height) || height < 2.4f) return 0;
            return Mathf.Clamp(Mathf.FloorToInt(height / 14f), 1, MaximumRegisters);
        }

        public static bool AllowRoofOrnament(Building building, RectF roof)
        {
            // Roof access and landing surfaces keep priority over visual decoration.
            return building != null && building.Roof != RoofType.Helipad &&
                roof.Valid && roof.Width >= 9f && roof.Depth >= 9f && building.Height > 5f;
        }

        private static bool Finite(float v) { return !float.IsNaN(v) && !float.IsInfinity(v); }
    }
}
