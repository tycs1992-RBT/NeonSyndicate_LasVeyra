using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Engine-free city composition rules. Art changes never rewrite layout, collision or saved IDs.</summary>
    public static class VisualRebirth04ACityPlan
    {
        public const int Variations = 5;
        public const float MaximumFacadeProjection = .12f;
        public const float MinimumClearRoofSpan = 4.2f;
        public const int MaximumOrdinaryBays = 3;
        public const int MaximumOrdinaryRegisters = 2;

        public static void ResetStatics() { }

        /// <summary>Four restrained regional palettes, with the southern rift quarter taking precedence.</summary>
        public static int StyleAt(float x, float z)
        {
            if (z < -380f) return 3;
            if (x < -180f) return 2;
            if (x > 180f) return 1;
            return 0;
        }

        public static int Variant(int seed, FacadeStyle archetype)
        {
            // Unsigned arithmetic also handles int.MinValue, unlike Abs(seed).
            uint value = unchecked((uint)seed * 2654435761u + (uint)archetype * 2246822519u);
            value ^= value >> 11;
            return (int)(value % Variations);
        }

        public static bool CanTheme(Building building, Lot lot)
        {
            return CrystalCity02ECoverage.CanTheme(building, lot);
        }

        public static int Bays(float width, FacadeStyle archetype, bool detailed)
        {
            if (!Finite(width) || width < 3f) return 0;
            float span = archetype == FacadeStyle.Warehouse ? 12f :
                (archetype == FacadeStyle.Apartment || archetype == FacadeStyle.Brick ? 7f : 9f);
            return Mathf.Clamp(Mathf.FloorToInt(width / span), 1, detailed ? 5 : MaximumOrdinaryBays);
        }

        public static int Registers(float height, bool detailed)
        {
            if (!Finite(height) || height < 2.4f) return 0;
            return Mathf.Clamp(Mathf.FloorToInt(height / 16f), 1, detailed ? 3 : MaximumOrdinaryRegisters);
        }

        /// <summary>Only narrow perimeter caps are allowed. The central roof remains a usable landing area.</summary>
        public static float PerimeterRadius(Building building, RectF roof)
        {
            if (building == null || building.Roof == RoofType.Helipad || !roof.Valid ||
                !Finite(roof.Width) || !Finite(roof.Depth) || building.Height < 5f) return 0f;
            float span = Mathf.Min(roof.Width, roof.Depth);
            if (span < 10f) return 0f;
            return Mathf.Clamp((span - MinimumClearRoofSpan - .5f) * .245f, .55f, 6f);
        }

        public static float CrownHeight(Building building, float radius, int style)
        {
            if (building == null || radius <= 0f || !Finite(building.Height)) return 0f;
            float multiplier = style == 1 ? 1.3f : (style == 2 ? 1.6f : 2.8f);
            return Mathf.Min(12f, Mathf.Max(1.6f, Mathf.Min(building.Height * .24f, radius * multiplier)));
        }

        public static string Exclusion(Building building, Lot lot)
        {
            if (building == null || lot == null) return "missing-layout-record";
            if (lot.CustomBuilt) return "custom-authored-lot";
            if (!CanTheme(building, lot)) return "invalid-or-subscale-footprint";
            return string.Empty;
        }

        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    }
}
