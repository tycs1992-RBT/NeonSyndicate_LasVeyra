using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Underheart's floors and walls (04E): glazed transit tile, terrazzo, steel grating, iron brick, riveted plate,
    /// veined archive stone, ash, ember soil, lava crust and the blanket weave. Cave rock and poured concrete share the
    /// ground generators in SurfaceHeight.cs.
    /// </summary>
    public static partial class SurfaceHeight
    {
        // ------------------------------------------------------------------ Underheart

        /// <summary>Glazed transit tile, 8 by 16 per tile: raised glossy tiles over recessed grout (Mask), age crazing (Aux).</summary>
        private static SurfaceFields UhTile(int n)
        {
            float[] tone = Fbm(n, 6, 4, 421);
            float[] craze = Cracks(n, 10, 422, 0.006f);
            float[] h = new float[n * n];
            float[] mask = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n;
                    int i = y * n + x;
                    float edge = Mathf.Min(HeightField.EdgeDistance(u * 8f) / 8f, HeightField.EdgeDistance(v * 16f) / 16f);
                    float grout = Mathf.Clamp01(1f - edge / 0.006f);
                    float bevel = Mathf.Clamp01((edge - 0.006f) / 0.01f);
                    float tile = HeightField.Hash01(Mathf.FloorToInt(u * 8f) + 3, Mathf.FloorToInt(v * 16f) + 7, 0);
                    h[i] = 0.7f - grout * 0.35f - (1f - bevel) * 0.15f + (tile - 0.5f) * 0.04f + (tone[i] - 0.5f) * 0.02f - craze[i] * 0.05f;
                    mask[i] = grout;
                }
            }
            return Finish(n, h, tone, mask, craze);
        }

        /// <summary>Terrazzo: a polished matrix with hard stone flecks (Mask) and scuff trails (Aux).</summary>
        private static SurfaceFields UhTerrazzo(int n)
        {
            float[] tone = Fbm(n, 6, 4, 431);
            float[] f1 = new float[n * n];
            float[] id = new float[n * n];
            HeightField.Cells(n, 36, 432, 1f, f1, null, id);
            float[] scuff = Fbm(n, 4, 3, 433, 0.5f, 3f, 0.5f);
            float[] h = new float[n * n];
            float[] mask = new float[n * n];
            float[] aux = new float[n * n];
            for (int i = 0; i < h.Length; i++)
            {
                float fleck = id[i] > 0.55f && f1[i] < 0.2f + id[i] * 0.15f ? 1f : 0f;
                h[i] = 0.6f + fleck * 0.06f + (tone[i] - 0.5f) * 0.03f;
                mask[i] = fleck * (0.4f + id[i] * 0.6f);
                aux[i] = Mathf.Clamp01((scuff[i] - 0.6f) * 4f);
            }
            return Finish(n, h, tone, mask, aux);
        }

        /// <summary>Steel grating, 8 bars across: raised bars (Mask) over dark gaps.</summary>
        private static SurfaceFields UhGrating(int n)
        {
            float[] tone = Fbm(n, 4, 3, 441);
            float[] grain = HeightField.Grain(n, 442);
            float[] h = new float[n * n];
            float[] mask = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;
                float bv = HeightField.Frac(v * 8f);
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n;
                    float bu = HeightField.Frac(u * 8f);
                    int i = y * n + x;
                    bool bar = bu < 0.19f || bv < 0.19f;
                    float worn = (bu < 0.02f || bv < 0.02f || (bu > 0.17f && bu < 0.19f) || (bv > 0.17f && bv < 0.19f)) ? 1f : 0f;
                    h[i] = bar ? 0.85f + (grain[i] - 0.5f) * 0.06f - worn * 0.1f : 0.1f;
                    mask[i] = bar ? 1f : 0f;
                }
            }
            return Finish(n, h, tone, mask, null);
        }

        /// <summary>Iron-red brick, 16 courses of 8: recessed mortar (Mask), soot toward the top of each repeat (Aux).</summary>
        private static SurfaceFields UhBrick(int n)
        {
            float[] tone = Fbm(n, 6, 4, 451);
            float[] grain = HeightField.Grain(n, 452);
            float[] pit = Fbm(n, 40, 2, 453, 0.6f);
            float[] h = new float[n * n];
            float[] mask = new float[n * n];
            float[] aux = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;
                int row = Mathf.FloorToInt(v * 16f);
                float rv = HeightField.Frac(v * 16f);
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n;
                    float cu = u * 8f + (row % 2) * 0.5f;
                    float ru = HeightField.Frac(cu);
                    int col = Mathf.FloorToInt(cu);
                    int i = y * n + x;
                    float mortar = Mathf.Max(Mathf.Clamp01(1f - rv / 0.12f), Mathf.Clamp01(1f - ru / 0.06f));
                    float brick = (HeightField.Hash01(col, row, 0) - 0.5f) * 0.1f;
                    float chip = Mathf.Clamp01((0.3f - pit[i]) / 0.1f);
                    h[i] = 0.65f + brick - mortar * 0.35f - chip * 0.15f + (grain[i] - 0.5f) * 0.06f;
                    mask[i] = mortar;
                    aux[i] = Mathf.Clamp01(v * 0.6f + (tone[i] - 0.5f) * 0.3f);
                }
            }
            return Finish(n, h, tone, mask, aux);
        }

        /// <summary>Riveted iron plates, 2 by 2 per tile: panel seams and rivet heads (Mask), heat tint (Aux).</summary>
        private static SurfaceFields UhPlate(int n)
        {
            float[] tone = Fbm(n, 4, 5, 461);
            float[] heat = Fbm(n, 2, 3, 462);
            float[] grain = HeightField.Grain(n, 463);
            float[] dent = Fbm(n, 6, 2, 464);
            float[] h = new float[n * n];
            float[] mask = new float[n * n];
            float[] aux = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;
                float pv = HeightField.Frac(v * 2f);
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n;
                    float pu = HeightField.Frac(u * 2f);
                    int i = y * n + x;
                    float seam = Mathf.Max(Mathf.Clamp01(1f - HeightField.EdgeDistance(u * 2f) / 0.008f), Mathf.Clamp01(1f - HeightField.EdgeDistance(v * 2f) / 0.008f));
                    float rivet = 0f;
                    for (int k = 0; k < 4; k++)
                    {
                        float rx = (k & 1) == 0 ? 0.06f : 0.94f;
                        float ry = (k & 2) == 0 ? 0.06f : 0.94f;
                        float dx = (pu - rx) * 2f, dy = (pv - ry) * 2f;
                        rivet = Mathf.Max(rivet, Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) / 0.045f));
                    }
                    rivet = Mathf.Sqrt(rivet);
                    h[i] = 0.5f + (dent[i] - 0.5f) * 0.08f + (grain[i] - 0.5f) * 0.04f - seam * 0.3f + rivet * 0.35f;
                    mask[i] = Mathf.Max(seam, rivet);
                    aux[i] = Mathf.Clamp01((heat[i] - 0.6f) * 3f);
                }
            }
            return Finish(n, h, tone, mask, aux);
        }

        /// <summary>Veined archive stone: polished, with faint vein grooves (Mask).</summary>
        private static SurfaceFields UhMarble(int n)
        {
            float[] tone = Fbm(n, 4, 6, 471);
            float[] h = new float[n * n];
            float[] mask = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n;
                    int i = y * n + x;
                    float vein = Mathf.Abs(Mathf.Sin((u * 3f + v + tone[i] * 2.5f) * Mathf.PI * 2f));
                    float m = Mathf.Clamp01(1f - vein / 0.06f);
                    h[i] = 0.7f - m * 0.06f + (tone[i] - 0.5f) * 0.03f;
                    mask[i] = m;
                }
            }
            return Finish(n, h, tone, mask, null);
        }

        /// <summary>Ash floor: soft drifts with scuffs (Mask) and scattered cinders (Aux).</summary>
        private static SurfaceFields UhAsh(int n)
        {
            float[] tone = Fbm(n, 5, 5, 481);
            float[] scuff = Fbm(n, 12, 2, 482);
            float[] grain = HeightField.Grain(n, 483);
            float[] h = new float[n * n];
            float[] mask = new float[n * n];
            float[] aux = new float[n * n];
            for (int i = 0; i < h.Length; i++)
            {
                float s = Mathf.Clamp01((0.32f - scuff[i]) * 6f);
                float cinder = grain[i] > 0.995f ? 1f : 0f;
                h[i] = 0.5f + (tone[i] - 0.5f) * 0.4f + (grain[i] - 0.5f) * 0.1f - s * 0.12f + cinder * 0.15f;
                mask[i] = s;
                aux[i] = cinder;
            }
            return Finish(n, h, tone, mask, aux);
        }

        /// <summary>Ember soil: loam clods with glowing specks (Aux) in the cracks (Mask).</summary>
        private static SurfaceFields UhSoil(int n)
        {
            float[] tone = Fbm(n, 6, 5, 491);
            float[] clods = new float[n * n];
            HeightField.Cells(n, 14, 492, 1f, clods, null, null);
            float[] crack = Cracks(n, 8, 493, 0.012f);
            float[] grain = HeightField.Grain(n, 494);
            float[] h = new float[n * n];
            float[] mask = new float[n * n];
            float[] aux = new float[n * n];
            for (int i = 0; i < h.Length; i++)
            {
                float clod = Mathf.Clamp01(1f - clods[i] / 0.7f);
                h[i] = 0.4f + clod * 0.3f + (tone[i] - 0.5f) * 0.2f + (grain[i] - 0.5f) * 0.1f - crack[i] * 0.3f;
                mask[i] = crack[i];
                aux[i] = grain[i] > 0.992f ? 1f : 0f;
            }
            return Finish(n, h, tone, mask, aux);
        }

        /// <summary>Molten lava: raised dark crust plates over low bright channels (Mask is how hot a texel is).</summary>
        private static SurfaceFields UhLava(int n)
        {
            float[] tone = Fbm(n, 4, 5, 495);
            float[] crust = Fbm(n, 24, 2, 496, 0.6f);
            float[] h = new float[n * n];
            float[] mask = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;
                for (int x = 0; x < n; x++)
                {
                    int i = y * n + x;
                    float flow = Mathf.Abs(Mathf.Sin((v * 3f + tone[i] * 1.8f) * Mathf.PI * 2f));
                    float hot = Mathf.Clamp01(1f - flow * 2.2f + (tone[i] - 0.5f));
                    h[i] = 0.3f + (1f - hot) * 0.5f + (crust[i] - 0.5f) * 0.2f * (1f - hot);
                    mask[i] = hot;
                }
            }
            return Finish(n, h, tone, mask, null);
        }

        /// <summary>Woven blanket: a coarse two-colour weave (Mask is the warp) with a border band (Aux).</summary>
        private static SurfaceFields UhWeave(int n)
        {
            float[] tone = Fbm(n, 4, 2, 497);
            float[] grain = HeightField.Grain(n, 498);
            float[] h = new float[n * n];
            float[] mask = new float[n * n];
            float[] aux = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n;
                    int i = y * n + x;
                    bool warp = ((Mathf.FloorToInt(u * 32f) + Mathf.FloorToInt(v * 32f)) & 1) == 0;
                    float a = Mathf.Sin(u * Mathf.PI * 2f * 32f) * 0.5f + 0.5f;
                    float b = Mathf.Sin(v * Mathf.PI * 2f * 32f) * 0.5f + 0.5f;
                    h[i] = 0.3f + (warp ? a : b) * 0.5f + (grain[i] - 0.5f) * 0.08f;
                    mask[i] = warp ? 1f : 0f;
                    aux[i] = HeightField.Frac(v * 2f) < 0.09f ? 1f : 0f;
                }
            }
            return Finish(n, h, tone, mask, aux);
        }
    }
}
