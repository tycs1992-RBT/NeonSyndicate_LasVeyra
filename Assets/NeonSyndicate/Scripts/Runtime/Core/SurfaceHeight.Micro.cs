using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Micro surfaces for small tinted objects: brushed metal, cast grain, rubber pores, car paint with sill grime, woven
    /// fibre and leaf skin. Their albedo is near white (SurfaceColor) so the material colour tints it; one normal and mask
    /// texture per kind serves every colour.
    /// </summary>
    public static partial class SurfaceHeight
    {
        // ------------------------------------------------------------------ micro surfaces for small objects

        /// <summary>Brushed metal: fine streaks along u, scratches (Mask) and tarnish (Aux).</summary>
        private static SurfaceFields Brushed(int n)
        {
            float[] tone = Fbm(n, 4, 3, 601);
            float[] streak = Fbm(n, 4, 4, 602, 0.5f, 0.25f, 4f);
            float[] grain = HeightField.Grain(n, 603);
            float[] scratchN = Fbm(n, 8, 2, 604, 0.5f, 0.5f, 3f);
            float[] h = new float[n * n];
            float[] mask = new float[n * n];
            float[] aux = new float[n * n];
            for (int i = 0; i < h.Length; i++)
            {
                float scratch = Mathf.Clamp01(1f - Mathf.Abs(scratchN[i] - 0.5f) / 0.01f);
                h[i] = 0.5f + (streak[i] - 0.5f) * 0.3f + (grain[i] - 0.5f) * 0.06f - scratch * 0.2f;
                mask[i] = scratch;
                aux[i] = Mathf.Clamp01((tone[i] - 0.55f) * 3f);
            }
            return Finish(n, h, tone, mask, aux);
        }

        /// <summary>Generic fine grain for painted and plastic parts: a soft cast texture with slight dents (Aux).</summary>
        private static SurfaceFields Grain(int n)
        {
            float[] tone = Fbm(n, 3, 3, 611);
            float[] fine = Fbm(n, 32, 3, 612, 0.55f);
            float[] grain = HeightField.Grain(n, 613);
            float[] h = new float[n * n];
            float[] aux = new float[n * n];
            for (int i = 0; i < h.Length; i++)
            {
                h[i] = 0.5f + (fine[i] - 0.5f) * 0.3f + (grain[i] - 0.5f) * 0.1f;
                aux[i] = Mathf.Clamp01((tone[i] - 0.6f) * 4f);
            }
            return Finish(n, h, tone, null, aux);
        }

        /// <summary>Rubber and fabric: dense pits (Mask) in a matte skin.</summary>
        private static SurfaceFields Pores(int n)
        {
            float[] tone = Fbm(n, 3, 3, 621);
            float[] f1 = new float[n * n];
            HeightField.Cells(n, 32, 622, 1f, f1, null, null);
            float[] grain = HeightField.Grain(n, 623);
            float[] h = new float[n * n];
            float[] mask = new float[n * n];
            for (int i = 0; i < h.Length; i++)
            {
                float pit = Mathf.Clamp01(1f - f1[i] / 0.35f);
                h[i] = 0.6f - pit * pit * 0.3f + (grain[i] - 0.5f) * 0.08f;
                mask[i] = pit;
            }
            return Finish(n, h, tone, mask, null);
        }

        /// <summary>Car paint: orange-peel undulation, sill grime rising from v = 0 (Mask) and stone chips (Aux).</summary>
        private static SurfaceFields PaintPeel(int n)
        {
            float[] tone = Fbm(n, 3, 2, 631);
            float[] peel = Fbm(n, 24, 2, 632, 0.5f);
            float[] grimeN = Fbm(n, 6, 3, 633);
            float[] grain = HeightField.Grain(n, 634);
            float[] h = new float[n * n];
            float[] mask = new float[n * n];
            float[] aux = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;
                for (int x = 0; x < n; x++)
                {
                    int i = y * n + x;
                    float grime = Mathf.Clamp01((0.16f - v) / 0.12f + (grimeN[i] - 0.5f) * 0.5f) * Mathf.Clamp01(1f - v / 0.4f);
                    float chip = grain[i] > 0.9985f ? 1f : 0f;
                    h[i] = 0.5f + (peel[i] - 0.5f) * 0.12f - chip * 0.3f;
                    mask[i] = grime;
                    aux[i] = chip;
                }
            }
            return Finish(n, h, tone, mask, aux);
        }

        /// <summary>Woven fibre for cloth, banners and turf detail: a fine over-under weave.</summary>
        private static SurfaceFields Fibre(int n)
        {
            float[] tone = Fbm(n, 4, 3, 641);
            float[] grain = HeightField.Grain(n, 642);
            float[] h = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n;
                    int i = y * n + x;
                    float a = Mathf.Sin(u * Mathf.PI * 2f * 24f) * 0.5f + 0.5f;
                    float b = Mathf.Sin(v * Mathf.PI * 2f * 24f) * 0.5f + 0.5f;
                    bool over = ((Mathf.FloorToInt(u * 24f) + Mathf.FloorToInt(v * 24f)) & 1) == 0;
                    h[i] = 0.3f + (over ? a : b) * 0.5f + (grain[i] - 0.5f) * 0.08f + (tone[i] - 0.5f) * 0.1f;
                }
            }
            return Finish(n, h, tone, null, null);
        }

        /// <summary>Leaf skin: a midrib along v with side veins (Mask) in a soft cellular surface.</summary>
        private static SurfaceFields Leaf(int n)
        {
            float[] tone = Fbm(n, 4, 3, 651);
            float[] cellsF = new float[n * n];
            HeightField.Cells(n, 20, 652, 1f, null, cellsF, null);
            float[] h = new float[n * n];
            float[] mask = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n;
                    int i = y * n + x;
                    float rib = HeightField.Band(u, 0.5f, 0.012f, 0.02f);
                    float side = HeightField.Band(HeightField.Frac(v * 10f + Mathf.Abs(u - 0.5f) * 1.6f + 0.5f), 0.5f, 0.04f, 0.1f);
                    float vein = Mathf.Max(rib, side * 0.6f);
                    float cell = Mathf.Clamp01(cellsF[i] / 0.1f);
                    h[i] = 0.45f + cell * 0.2f + vein * 0.3f + (tone[i] - 0.5f) * 0.1f;
                    mask[i] = vein;
                }
            }
            return Finish(n, h, tone, mask, null);
        }
    }
}
