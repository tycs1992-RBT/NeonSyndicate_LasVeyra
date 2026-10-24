using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The H8S accessibility maths shared by the companion card, the barks, the quest tracker, the guidance glyphs and
    /// the overhead cast markers: the text size range, scaled font sizes (never past what a row can hold), the
    /// high-contrast colour for a marker (the same hue, brighter and fully opaque, over a dark outline) and the marker
    /// size factor. The settings themselves live in Settings (PlayerPrefs, NSLV_ prefix). Engine-free and tested.
    /// </summary>
    public static class AccessibilityRulesH8S
    {
        public const float MinTextScale = 1f;
        public const float MaxTextScale = 1.5f;
        /// <summary>Glyphs and overhead markers grow by this much in high contrast.</summary>
        public const float HighContrastSize = 1.25f;
        /// <summary>The outline drawn behind a high-contrast glyph, as a share of its size.</summary>
        public const float OutlineShare = 0.22f;

        /// <summary>The text size setting kept in its range (a broken stored value becomes 1).</summary>
        public static float ClampTextScale(float s)
        {
            if (float.IsNaN(s) || float.IsInfinity(s)) return MinTextScale;
            return Mathf.Clamp(s, MinTextScale, MaxTextScale);
        }

        /// <summary>A font size scaled by the text setting, capped at maxSize (the tallest the row can hold).</summary>
        public static int Font(int baseSize, float scale, int maxSize)
        {
            int size = Mathf.RoundToInt(baseSize * ClampTextScale(scale));
            return Mathf.Clamp(size, baseSize, Mathf.Max(baseSize, maxSize));
        }

        /// <summary>
        /// A marker colour for the current contrast setting: unchanged when off; when on, the same hue pushed toward full
        /// brightness and saturation and made opaque, so it stands out against fog, crystal glow and snow alike.
        /// </summary>
        public static Color MarkerColor(Color c, bool highContrast)
        {
            if (!highContrast) return c;
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            float min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            if (max < 1e-4f || (max - min) < 0.08f * max) return new Color(1f, 1f, 1f, 1f);
            // Brightest value first (the hue's ratios kept), then at least 85 percent saturation: the distances from the
            // top channel are stretched so the lowest channel lands at 0.15 at most.
            float r = c.r / max, g = c.g / max, b = c.b / max, low = min / max;
            float stretch = 1f - low < 0.85f ? 0.85f / Mathf.Max(1e-4f, 1f - low) : 1f;
            return new Color(1f - (1f - r) * stretch, 1f - (1f - g) * stretch, 1f - (1f - b) * stretch, 1f);
        }

        /// <summary>The colour of the outline behind a high-contrast glyph (near black, opaque by the glyph's alpha).</summary>
        public static Color OutlineColor(float alpha)
        {
            return new Color(0.02f, 0.02f, 0.03f, Mathf.Clamp01(alpha) * 0.9f);
        }

        /// <summary>The size factor for glyphs and overhead markers.</summary>
        public static float MarkerScale(bool highContrast)
        {
            return highContrast ? HighContrastSize : 1f;
        }
    }
}
