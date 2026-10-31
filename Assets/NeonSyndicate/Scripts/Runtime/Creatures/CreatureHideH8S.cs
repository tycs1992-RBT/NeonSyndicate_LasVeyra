using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The surface relief of the H8S creature look, engine-free: tileable height fields for the detail normal that the
    /// creature material puts on the second UV set in place of the people's fabric weave. Three families: chitin
    /// plates (domed cells with deep seams), overlapping scales (half offset shingle rows with hairline cracks) and
    /// cracked basalt (columnar fractures and grain). Per cell strength still comes from the palette's detail channel,
    /// so crystal facets stay flat while hides and plates take the relief. Built from the same HeightField and
    /// SurfaceMaps helpers as the H8S stone, so the result is tileable by construction.
    /// </summary>
    public static class CreatureHideH8S
    {
        /// <summary>Surface families (CreatureLook.SurfaceH8S): 0 keeps the shared character material.</summary>
        public const int None = 0, ChitinPlates = 1, Scales = 2, Basalt = 3;
        public const int FamilyCount = 4;

        /// <summary>Normal map strength (slope scale) per family at 256 texels.</summary>
        public static float NormalStrength(int family)
        {
            switch (family)
            {
                case ChitinPlates: return 5.5f;
                case Scales: return 4.5f;
                case Basalt: return 6f;
                default: return 2.2f;
            }
        }

        /// <summary>A tileable n by n height field in [0,1] for a family (texture size n, a power of two).</summary>
        public static float[] Height(int family, int n)
        {
            switch (family)
            {
                case ChitinPlates: return Plates(n);
                case Scales: return Shingles(n);
                case Basalt: return Columns(n);
                default: return HeightField.Fbm(n, 8, 3, 3101);
            }
        }

        /// <summary>The RGB-encoded tangent-space normal texels of a family (A 255), from Height.</summary>
        public static Color32[] Normal(int family, int n)
        {
            float[] h = Height(family, n);
            return SurfaceMaps.Normal(h, n, NormalStrength(family) * n / 256f);
        }

        /// <summary>
        /// Chitin: jittered cells, each a low dome that falls into a deep seam at the cell border, a few cells sunk a
        /// little (the plates are not all one height), and a fine grain over everything.
        /// </summary>
        private static float[] Plates(int n)
        {
            int count = n * n;
            float[] f1 = new float[count], edge = new float[count], id = new float[count];
            HeightField.Cells(n, 6, 3201, 0.85f, f1, edge, id);
            float[] grain = HeightField.Fbm(n, Mathf.Min(n, 32), 3, 3202, 0.55f);
            float[] h = new float[count];
            for (int i = 0; i < count; i++)
            {
                // The dome rises away from the seam and flattens toward the plate centre.
                float dome = Smooth(0f, 0.22f, edge[i]);
                dome = Mathf.Sqrt(dome);
                float sink = id[i] < 0.3f ? 0.12f : 0f;
                h[i] = 0.18f + dome * 0.62f - sink + (grain[i] - 0.5f) * 0.1f;
            }
            HeightField.Clamp(h);
            return h;
        }

        /// <summary>
        /// Scales: rows of shingles, each row offset by half a scale, every scale a rounded shield that overlaps the row
        /// below (its lower edge stands proud), with hairline cracks across a few scales.
        /// </summary>
        private static float[] Shingles(int n)
        {
            int count = n * n;
            const float rows = 7f;
            float[] cracks = HeightField.Fbm(n, 12, 3, 3301, 0.5f, 3f, 1f);
            float[] h = new float[count];
            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;
                float rv = v * rows;
                int row = Mathf.FloorToInt(rv);
                float fv = rv - row;
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n;
                    float ru = u * rows + (row % 2) * 0.5f;
                    float fu = ru - Mathf.Floor(ru);
                    float dx = (fu - 0.5f) * 2f, dy = fv;
                    // Shield outline: a rounded lower edge, the scale thickest just above it and thinning under the next row.
                    float across = 1f - dx * dx;
                    float lip = Smooth(0f, 0.14f, dy) * (1f - Smooth(0.55f, 1f, dy) * 0.55f);
                    float shield = Mathf.Clamp01(across * 1.15f) * lip;
                    int cx = Mathf.FloorToInt(ru), cell = row * 16 + cx;
                    float crackSeed = HeightField.Hash01(cell, 7, 3302);
                    float crack = crackSeed < 0.35f ? Smooth(0.52f, 0.5f, Mathf.Abs(cracks[y * n + x] - 0.5f) + 0.5f) * 0.25f : 0f;
                    h[y * n + x] = 0.2f + shield * 0.7f - crack;
                }
            }
            HeightField.Clamp(h);
            return h;
        }

        /// <summary>Basalt: big cells with sharp fractures between them, each cell's face slightly tilted, rough grain.</summary>
        private static float[] Columns(int n)
        {
            int count = n * n;
            float[] f1 = new float[count], edge = new float[count], id = new float[count];
            HeightField.Cells(n, 4, 3401, 0.95f, f1, edge, id);
            float[] rough = HeightField.Fbm(n, 6, 4, 3402, 0.5f);
            float[] fine = HeightField.Fbm(n, Mathf.Min(n, 48), 2, 3403, 0.6f);
            float[] h = new float[count];
            for (int i = 0; i < count; i++)
            {
                float fracture = 1f - Smooth(0f, 0.07f, edge[i]);
                float face = 0.55f + (id[i] - 0.5f) * 0.3f;
                h[i] = face - fracture * 0.45f + (rough[i] - 0.5f) * 0.18f + (fine[i] - 0.5f) * 0.08f;
            }
            HeightField.Clamp(h);
            return h;
        }

        private static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }
    }
}
