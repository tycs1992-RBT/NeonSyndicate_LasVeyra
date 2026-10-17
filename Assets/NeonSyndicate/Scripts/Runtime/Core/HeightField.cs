using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Fast tileable height field maths for the procedural PBR surfaces (Phase 2 of the visual upgrade). Everything
    /// here works on plain float arrays of n by n texels in [0,1] and uses only Mathf, so it runs and is tested
    /// offline. The noise is value noise evaluated from a precomputed lattice per octave (four array reads and a few
    /// multiplies per texel per octave) instead of four hashes per octave, and the inner loops use precomputed wrapped
    /// indices instead of modulo, which is what keeps a 512 texture at a few milliseconds per pass. Every function
    /// keeps the result tileable: lattices wrap, blurs wrap, and sampling wraps.
    /// </summary>
    public static class HeightField
    {
        /// <summary>A cheap integer hash to [0,1) (multiply and xorshift, no division), used for lattices and grain.</summary>
        public static float Hash01(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1274126177);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }

        /// <summary>A new field filled with one value.</summary>
        public static float[] Flat(int n, float value)
        {
            float[] h = new float[n * n];
            for (int i = 0; i < h.Length; i++) h[i] = value;
            return h;
        }

        /// <summary>Smoothstep weight for noise interpolation.</summary>
        private static float Smooth(float t)
        {
            return t * t * (3f - 2f * t);
        }

        /// <summary>Index of the texel one step before and after each column (or row), wrapping.</summary>
        public static void WrapIndices(int n, out int[] before, out int[] after)
        {
            before = new int[n];
            after = new int[n];
            for (int i = 0; i < n; i++)
            {
                before[i] = i == 0 ? n - 1 : i - 1;
                after[i] = i == n - 1 ? 0 : i + 1;
            }
        }

        /// <summary>
        /// Adds one octave of tileable value noise (cells lattice cells across the texture) times amplitude to h.
        /// stretchU and stretchV scale the cell counts per axis (more cells along an axis means finer streaks).
        /// </summary>
        public static void AddNoise(float[] h, int n, int cells, int seed, float amplitude, float stretchU = 1f, float stretchV = 1f)
        {
            if (cells < 1) cells = 1;
            int cu = Mathf.Max(1, Mathf.RoundToInt(cells * stretchU));
            int cv = Mathf.Max(1, Mathf.RoundToInt(cells * stretchV));
            int lw = Mathf.Max(cu, cv);
            float[] lat = new float[lw * lw];
            for (int y = 0; y < lw; y++)
                for (int x = 0; x < lw; x++)
                    lat[y * lw + x] = Hash01(x, y, seed);
            int[] x0 = new int[n];
            int[] x1 = new int[n];
            float[] fx = new float[n];
            for (int x = 0; x < n; x++)
            {
                float gx = (x + 0.5f) / n * cu;
                int ix = (int)gx;
                fx[x] = Smooth(gx - ix);
                x0[x] = ix % cu;
                x1[x] = (ix + 1) % cu;
            }
            for (int y = 0; y < n; y++)
            {
                float gy = (y + 0.5f) / n * cv;
                int iy = (int)gy;
                float fy = Smooth(gy - iy);
                int y0 = (iy % cv) * lw;
                int y1 = ((iy + 1) % cv) * lw;
                int row = y * n;
                for (int x = 0; x < n; x++)
                {
                    float a = lat[y0 + x0[x]];
                    float b = lat[y0 + x1[x]];
                    float c = lat[y1 + x0[x]];
                    float d = lat[y1 + x1[x]];
                    float top = a + (b - a) * fx[x];
                    float bottom = c + (d - c) * fx[x];
                    h[row + x] += (top + (bottom - top) * fy) * amplitude;
                }
            }
        }

        /// <summary>Tileable fractal noise in [0,1]: baseCells cells across, octaves doubling, amplitude falling by gain.</summary>
        public static float[] Fbm(int n, int baseCells, int octaves, int seed, float gain = 0.5f, float stretchU = 1f, float stretchV = 1f)
        {
            float[] h = new float[n * n];
            float amp = 0.5f;
            float norm = 0f;
            int cells = baseCells;
            for (int i = 0; i < octaves; i++)
            {
                if (cells > n) break;
                AddNoise(h, n, cells, seed + i * 31, amp, stretchU, stretchV);
                norm += amp;
                amp *= gain;
                cells *= 2;
            }
            if (norm > 0f) for (int i = 0; i < h.Length; i++) h[i] /= norm;
            return h;
        }

        /// <summary>Per-texel white noise in [0,1) (fine grain; it tiles because it is per texel).</summary>
        public static float[] Grain(int n, int seed)
        {
            float[] h = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                int row = y * n;
                for (int x = 0; x < n; x++) h[row + x] = Hash01(x, y, seed);
            }
            return h;
        }

        /// <summary>
        /// Tileable jittered cell (Voronoi) field: f1 is the distance to the nearest feature point in cell units, edge
        /// is f2 minus f1 (0 on cell borders), id is a per-cell random value in [0,1). Any output may be null.
        /// </summary>
        public static void Cells(int n, int cells, int seed, float jitter, float[] f1, float[] edge, float[] id)
        {
            if (cells < 1) cells = 1;
            float[] jx = new float[cells * cells];
            float[] jy = new float[cells * cells];
            float[] ids = new float[cells * cells];
            for (int y = 0; y < cells; y++)
            {
                for (int x = 0; x < cells; x++)
                {
                    int i = y * cells + x;
                    jx[i] = 0.5f + (Hash01(x, y, seed) - 0.5f) * jitter;
                    jy[i] = 0.5f + (Hash01(x, y, seed + 7) - 0.5f) * jitter;
                    ids[i] = Hash01(x, y, seed + 13);
                }
            }
            float scale = (float)cells / n;
            for (int py = 0; py < n; py++)
            {
                float gy = (py + 0.5f) * scale;
                int cy = (int)gy;
                int rowOut = py * n;
                for (int px = 0; px < n; px++)
                {
                    float gx = (px + 0.5f) * scale;
                    int cx = (int)gx;
                    float best = 81f, second = 81f, bestId = 0f;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int ny = cy + dy;
                        int wy = ny < 0 ? ny + cells : (ny >= cells ? ny - cells : ny);
                        int rowCell = wy * cells;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = cx + dx;
                            int wx = nx < 0 ? nx + cells : (nx >= cells ? nx - cells : nx);
                            int i = rowCell + wx;
                            float ex = nx + jx[i] - gx;
                            float ey = ny + jy[i] - gy;
                            float d = ex * ex + ey * ey;
                            if (d < best)
                            {
                                second = best;
                                best = d;
                                bestId = ids[i];
                            }
                            else if (d < second) second = d;
                        }
                    }
                    int o = rowOut + px;
                    float b = Mathf.Sqrt(best);
                    if (f1 != null) f1[o] = b;
                    if (edge != null) edge[o] = Mathf.Sqrt(second) - b;
                    if (id != null) id[o] = bestId;
                }
            }
        }

        /// <summary>Bilinear tileable sample at texture coordinates (u, v in [0,1), wrapping).</summary>
        public static float Sample(float[] h, int n, float u, float v)
        {
            float gx = u * n - 0.5f;
            float gy = v * n - 0.5f;
            int x0 = Mathf.FloorToInt(gx);
            int y0 = Mathf.FloorToInt(gy);
            float fx = gx - x0;
            float fy = gy - y0;
            x0 = ((x0 % n) + n) % n;
            y0 = ((y0 % n) + n) % n;
            int x1 = (x0 + 1) % n;
            int y1 = (y0 + 1) % n;
            float a = h[y0 * n + x0], b = h[y0 * n + x1], c = h[y1 * n + x0], d = h[y1 * n + x1];
            float top = a + (b - a) * fx;
            float bottom = c + (d - c) * fx;
            return top + (bottom - top) * fy;
        }

        /// <summary>Resamples a tileable field to another size (bilinear, texel centres aligned).</summary>
        public static float[] Resample(float[] src, int srcN, int n)
        {
            if (srcN == n) return (float[])src.Clone();
            float[] dst = new float[n * n];
            int[] x0 = new int[n];
            int[] x1 = new int[n];
            float[] fx = new float[n];
            for (int x = 0; x < n; x++)
            {
                float gx = (x + 0.5f) / n * srcN - 0.5f;
                int ix = Mathf.FloorToInt(gx);
                fx[x] = gx - ix;
                x0[x] = ((ix % srcN) + srcN) % srcN;
                x1[x] = (x0[x] + 1) % srcN;
            }
            for (int y = 0; y < n; y++)
            {
                float gy = (y + 0.5f) / n * srcN - 0.5f;
                int iy = Mathf.FloorToInt(gy);
                float fy = gy - iy;
                int y0 = (((iy % srcN) + srcN) % srcN) * srcN;
                int y1 = (((iy + 1) % srcN + srcN) % srcN) * srcN;
                int row = y * n;
                for (int x = 0; x < n; x++)
                {
                    float a = src[y0 + x0[x]], b = src[y0 + x1[x]], c = src[y1 + x0[x]], d = src[y1 + x1[x]];
                    float top = a + (b - a) * fx[x];
                    float bottom = c + (d - c) * fx[x];
                    dst[row + x] = top + (bottom - top) * fy;
                }
            }
            return dst;
        }

        /// <summary>Tileable box blur of radius r (separable; running sums, no per-texel modulo).</summary>
        public static float[] Blur(float[] h, int n, int r)
        {
            if (r <= 0) return (float[])h.Clone();
            if (r > n / 2 - 1) r = Mathf.Max(0, n / 2 - 1);
            float[] tmp = new float[n * n];
            float[] dst = new float[n * n];
            float inv = 1f / (2 * r + 1);
            float[] padded = new float[n + 2 * r + 1];
            for (int y = 0; y < n; y++)
            {
                int row = y * n;
                for (int k = 0; k < padded.Length; k++) padded[k] = h[row + ((k - r + n) % n)];
                float sum = 0f;
                for (int k = 0; k < 2 * r + 1; k++) sum += padded[k];
                for (int x = 0; x < n; x++)
                {
                    tmp[row + x] = sum * inv;
                    sum += padded[x + 2 * r + 1] - padded[x];
                }
            }
            float[] colSum = new float[n];
            for (int k = -r; k <= r; k++)
            {
                int row = ((k + n) % n) * n;
                for (int x = 0; x < n; x++) colSum[x] += tmp[row + x];
            }
            for (int y = 0; y < n; y++)
            {
                int row = y * n;
                int add = ((y + r + 1) % n) * n;
                int sub = ((y - r + n) % n) * n;
                for (int x = 0; x < n; x++)
                {
                    dst[row + x] = colSum[x] * inv;
                    colSum[x] += tmp[add + x] - tmp[sub + x];
                }
            }
            return dst;
        }

        /// <summary>Rescales a field so its minimum is 0 and its maximum is 1 (a flat field becomes 0.5).</summary>
        public static void Normalize(float[] h)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            for (int i = 0; i < h.Length; i++)
            {
                if (h[i] < lo) lo = h[i];
                if (h[i] > hi) hi = h[i];
            }
            float range = hi - lo;
            if (range < 1e-6f)
            {
                for (int i = 0; i < h.Length; i++) h[i] = 0.5f;
                return;
            }
            float inv = 1f / range;
            for (int i = 0; i < h.Length; i++) h[i] = (h[i] - lo) * inv;
        }

        /// <summary>Clamps every value to [0,1].</summary>
        public static void Clamp(float[] h)
        {
            for (int i = 0; i < h.Length; i++) h[i] = h[i] < 0f ? 0f : (h[i] > 1f ? 1f : h[i]);
        }

        public static float Mean(float[] h)
        {
            double sum = 0.0;
            for (int i = 0; i < h.Length; i++) sum += h[i];
            return h.Length == 0 ? 0f : (float)(sum / h.Length);
        }

        /// <summary>Fraction of the texture (0..1) wrapped into [0,1).</summary>
        public static float Frac(float f)
        {
            return f - Mathf.Floor(f);
        }

        /// <summary>A soft band: 1 inside |t - centre| &lt; halfWidth, falling to 0 over feather.</summary>
        public static float Band(float t, float centre, float halfWidth, float feather)
        {
            float d = Mathf.Abs(t - centre) - halfWidth;
            if (d <= 0f) return 1f;
            if (feather <= 0f) return 0f;
            return Mathf.Clamp01(1f - d / feather);
        }

        /// <summary>Distance of a cell-local coordinate (0..1) to the nearest cell border, 0 at the border, 0.5 in the middle.</summary>
        public static float EdgeDistance(float t)
        {
            float f = Frac(t);
            return f < 0.5f ? f : 1f - f;
        }

        /// <summary>
        /// The largest amount by which a wrap-around step (last to first column, or row) exceeds the largest step
        /// between neighbouring interior texels of the same row or column. Near zero for a tileable field, including
        /// one with hard edges inside it; clearly positive when the pattern does not close on itself.
        /// </summary>
        public static float SeamExcess(float[] h, int n)
        {
            float worst = 0f;
            for (int y = 0; y < n; y++)
            {
                int row = y * n;
                float maxStep = 0f;
                for (int x = 0; x < n - 1; x++)
                {
                    float d = Mathf.Abs(h[row + x] - h[row + x + 1]);
                    if (d > maxStep) maxStep = d;
                }
                float seam = Mathf.Abs(h[row] - h[row + n - 1]);
                if (seam - maxStep > worst) worst = seam - maxStep;
            }
            for (int x = 0; x < n; x++)
            {
                float maxStep = 0f;
                for (int y = 0; y < n - 1; y++)
                {
                    float d = Mathf.Abs(h[y * n + x] - h[(y + 1) * n + x]);
                    if (d > maxStep) maxStep = d;
                }
                float seam = Mathf.Abs(h[x] - h[(n - 1) * n + x]);
                if (seam - maxStep > worst) worst = seam - maxStep;
            }
            return worst;
        }
    }
}
