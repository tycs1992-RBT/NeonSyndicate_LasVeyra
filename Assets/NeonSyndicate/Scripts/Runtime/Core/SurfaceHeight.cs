using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Pure height-field generators, one per SurfaceKind: asphalt aggregate and cracks, paver grout, concrete pores and
    /// form lines, brick courses, corrugated ridges, roof tile overlap, wood grain, bark fissures, rock strata, snow
    /// crust, gravel, and the Underheart's floors and walls. Every generator returns tileable n by n fields in [0,1]
    /// (see SurfaceFields) and uses only HeightField and Mathf, so it is tested offline. Slow noise is built
    /// at 256 and resampled, with the fine octaves added at full size, so a 1024 field costs little more than a 512 one.
    /// </summary>
    public static partial class SurfaceHeight
    {
        /// <summary>Kinds that cover large surfaces (ground, walls, floors) and get the full preset texture size.</summary>
        public static bool IsLarge(SurfaceKind kind)
        {
            switch (kind)
            {
                case SurfaceKind.Asphalt:
                case SurfaceKind.Pavers:
                case SurfaceKind.Concrete:
                case SurfaceKind.Grass:
                case SurfaceKind.Sand:
                case SurfaceKind.Dirt:
                case SurfaceKind.Rock:
                case SurfaceKind.SnowCrust:
                case SurfaceKind.SnowThin:
                case SurfaceKind.Ice:
                case SurfaceKind.UhRock:
                case SurfaceKind.UhConcrete:
                case SurfaceKind.UhTile:
                case SurfaceKind.UhTerrazzo:
                case SurfaceKind.UhBrick:
                case SurfaceKind.UhPlate:
                case SurfaceKind.UhMarble:
                case SurfaceKind.UhAsh:
                case SurfaceKind.UhSoil:
                case SurfaceKind.UhLava:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Builds the fields of a kind at n by n texels (n a power of two, 32 or more).</summary>
        public static SurfaceFields Build(SurfaceKind kind, int n)
        {
            if (n < 32) n = 32;
            switch (kind)
            {
                case SurfaceKind.Asphalt: return Asphalt(n);
                case SurfaceKind.Pavers: return Pavers(n);
                case SurfaceKind.Concrete: return Concrete(n, 2, 1, 311);
                case SurfaceKind.Grass: return Grass(n);
                case SurfaceKind.Sand: return Sand(n);
                case SurfaceKind.Dirt: return Dirt(n);
                case SurfaceKind.Rock: return Rock(n, 71, 6f);
                case SurfaceKind.Gravel: return Gravel(n);
                case SurfaceKind.RoofTiles: return RoofTiles(n);
                case SurfaceKind.Corrugated: return Corrugated(n);
                case SurfaceKind.Wood: return Wood(n);
                case SurfaceKind.Bark: return Bark(n);
                case SurfaceKind.SnowCrust: return SnowCrust(n);
                case SurfaceKind.SnowThin: return SnowThin(n);
                case SurfaceKind.Ice: return Ice(n);
                case SurfaceKind.Ripples: return Ripples(n);
                case SurfaceKind.Brushed: return Brushed(n);
                case SurfaceKind.Grain: return Grain(n);
                case SurfaceKind.Pores: return Pores(n);
                case SurfaceKind.PaintPeel: return PaintPeel(n);
                case SurfaceKind.Fibre: return Fibre(n);
                case SurfaceKind.Leaf: return Leaf(n);
                case SurfaceKind.UhRock: return Rock(n, 307, 9f);
                case SurfaceKind.UhConcrete: return Concrete(n, 4, 2, 401);
                case SurfaceKind.UhTile: return UhTile(n);
                case SurfaceKind.UhTerrazzo: return UhTerrazzo(n);
                case SurfaceKind.UhGrating: return UhGrating(n);
                case SurfaceKind.UhBrick: return UhBrick(n);
                case SurfaceKind.UhPlate: return UhPlate(n);
                case SurfaceKind.UhMarble: return UhMarble(n);
                case SurfaceKind.UhAsh: return UhAsh(n);
                case SurfaceKind.UhSoil: return UhSoil(n);
                case SurfaceKind.UhLava: return UhLava(n);
                default: return UhWeave(n);
            }
        }

        // ------------------------------------------------------------------ shared helpers

        /// <summary>Fractal noise built at 256 and resampled, with the octaves finer than 256 added at full size.</summary>
        private static float[] Fbm(int n, int baseCells, int octaves, int seed, float gain = 0.5f, float stretchU = 1f, float stretchV = 1f)
        {
            int m = Mathf.Min(n, 256);
            if (m == n) return HeightField.Fbm(m, baseCells, octaves, seed, gain, stretchU, stretchV);
            // The coarse octaves at 256, resampled; the octaves the small field could not hold are added at full
            // size, each with the amplitude it would have had, so every size shares the same overall level.
            float[] small = new float[m * m];
            float amp = 0.5f;
            float norm = 0f;
            int cells = baseCells;
            for (int i = 0; i < octaves; i++)
            {
                if (cells <= m) HeightField.AddNoise(small, m, cells, seed + i * 31, amp, stretchU, stretchV);
                if (cells <= n) norm += amp;
                amp *= gain;
                cells *= 2;
            }
            float[] h = HeightField.Resample(small, m, n);
            amp = 0.5f;
            cells = baseCells;
            for (int i = 0; i < octaves; i++)
            {
                if (cells > m && cells <= n) HeightField.AddNoise(h, n, cells, seed + i * 31, amp, stretchU, stretchV);
                amp *= gain;
                cells *= 2;
            }
            if (norm > 0f) for (int i = 0; i < h.Length; i++) h[i] /= norm;
            return h;
        }

        private static float[] Mul(float[] a, float k)
        {
            float[] r = new float[a.Length];
            for (int i = 0; i < a.Length; i++) r[i] = a[i] * k;
            return r;
        }

        /// <summary>Thin lines where a noise field crosses 0.5 (hairline cracks), width in field units.</summary>
        private static float[] Cracks(int n, int cells, int seed, float width)
        {
            float[] f = Fbm(n, cells, 3, seed);
            float[] c = new float[n * n];
            for (int i = 0; i < c.Length; i++) c[i] = Mathf.Clamp01(1f - Mathf.Abs(f[i] - 0.5f) / width);
            return c;
        }

        private static SurfaceFields Finish(int n, float[] height, float[] tone, float[] mask, float[] aux)
        {
            HeightField.Clamp(height);
            if (tone != null) HeightField.Clamp(tone);
            if (mask != null) HeightField.Clamp(mask);
            if (aux != null) HeightField.Clamp(aux);
            return new SurfaceFields(n, height, tone, mask, aux);
        }

        // ------------------------------------------------------------------ ground

        /// <summary>Asphalt: coarse undulation, dense aggregate, a few cracks (Mask) and shallow puddle hollows (Aux).</summary>
        private static SurfaceFields Asphalt(int n)
        {
            float[] tone = Fbm(n, 8, 4, 11);
            float[] agg = Fbm(n, 64, 3, 12, 0.6f);
            float[] grain = HeightField.Grain(n, 13);
            float[] crack = Cracks(n, 3, 77, 0.012f);
            float[] crack2 = Cracks(n, 5, 78, 0.008f);
            float[] puddle = Fbm(n, 2, 3, 14);
            float[] h = new float[n * n];
            float[] mask = new float[n * n];
            float[] aux = new float[n * n];
            for (int i = 0; i < h.Length; i++)
            {
                float c = Mathf.Max(crack[i], crack2[i] * 0.8f);
                float p = Mathf.Clamp01((puddle[i] - 0.58f) / 0.16f);
                p = p * p * (3f - 2f * p);
                h[i] = 0.55f + (tone[i] - 0.5f) * 0.2f + (agg[i] - 0.5f) * 0.3f + (grain[i] - 0.5f) * 0.12f - c * 0.35f - p * 0.1f;
                mask[i] = c;
                aux[i] = p;
            }
            return Finish(n, h, tone, mask, aux);
        }

        /// <summary>Concrete pavers, 4 by 4 per tile: bevelled slabs, recessed grout (Mask), hairline cracks and grime (Aux).</summary>
        private static SurfaceFields Pavers(int n)
        {
            float[] tone = Fbm(n, 8, 4, 21);
            float[] grain = Fbm(n, 48, 2, 22, 0.6f);
            float[] crack = Cracks(n, 3, 91, 0.007f);
            float[] grime = Fbm(n, 3, 3, 93);
            float[] h = new float[n * n];
            float[] mask = new float[n * n];
            float[] aux = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;
                float tv = HeightField.Frac(v * 4f);
                int sy = Mathf.FloorToInt(v * 4f);
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n;
                    float tu = HeightField.Frac(u * 4f);
                    int sx = Mathf.FloorToInt(u * 4f);
                    int i = y * n + x;
                    float edge = Mathf.Min(HeightField.EdgeDistance(u * 4f), HeightField.EdgeDistance(v * 4f));
                    float grout = Mathf.Clamp01(1f - edge / 0.025f);
                    float bevel = Mathf.Clamp01((edge - 0.025f) / 0.03f);
                    float slab = (HeightField.Hash01(sx * 13 + sy * 7, 3, 0) - 0.5f) * 0.08f;
                    float tilt = ((tu - 0.5f) * (HeightField.Hash01(sx, sy + 9, 0) - 0.5f) + (tv - 0.5f) * (HeightField.Hash01(sx + 4, sy, 0) - 0.5f)) * 0.08f;
                    h[i] = 0.6f + slab + tilt + (grain[i] - 0.5f) * 0.08f + (tone[i] - 0.5f) * 0.04f - (1f - bevel) * 0.2f - grout * 0.25f - crack[i] * 0.2f;
                    mask[i] = Mathf.Max(grout, crack[i] * 0.7f);
                    aux[i] = Mathf.Clamp01((grime[i] - 0.55f) * 2.5f);
                }
            }
            return Finish(n, h, tone, mask, aux);
        }

        /// <summary>
        /// Poured concrete: pores and bug holes (Mask), formwork seams (rows by cols per tile) and damp stains (Aux).
        /// </summary>
        private static SurfaceFields Concrete(int n, int seamRows, int seamCols, int seed)
        {
            float[] tone = Fbm(n, 6, 5, seed);
            float[] poreN = Fbm(n, 48, 2, seed + 3, 0.5f);
            float[] stain = Fbm(n, 3, 3, seed + 7);
            float[] grain = HeightField.Grain(n, seed + 9);
            float[] h = new float[n * n];
            float[] mask = new float[n * n];
            float[] aux = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;
                float seamV = Mathf.Clamp01(1f - HeightField.EdgeDistance(v * seamRows) / seamRows / 0.006f);
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n;
                    int i = y * n + x;
                    float seamU = Mathf.Clamp01(1f - HeightField.EdgeDistance(u * seamCols) / seamCols / 0.006f);
                    float seam = Mathf.Max(seamU, seamV);
                    float pore = Mathf.Clamp01((0.3f - poreN[i]) / 0.08f);
                    float hole = grain[i] > 0.996f ? 1f : 0f;
                    float p = Mathf.Max(pore, hole);
                    h[i] = 0.6f + (tone[i] - 0.5f) * 0.14f + (grain[i] - 0.5f) * 0.05f - p * 0.3f - seam * 0.2f;
                    mask[i] = Mathf.Max(p, seam);
                    aux[i] = Mathf.Clamp01((stain[i] - 0.55f) * 4f);
                }
            }
            return Finish(n, h, tone, mask, aux);
        }

        /// <summary>Turf: dense blade streaks (stretched along v), tufts (Mask) and bare patches (Aux).</summary>
        private static SurfaceFields Grass(int n)
        {
            float[] tone = Fbm(n, 8, 5, 41);
            float[] blades = Fbm(n, 24, 3, 42, 0.55f, 2f, 0.5f);
            float[] tuft = Fbm(n, 12, 2, 43);
            float[] bare = Fbm(n, 3, 3, 44);
            float[] grain = HeightField.Grain(n, 45);
            float[] h = new float[n * n];
            float[] mask = new float[n * n];
            float[] aux = new float[n * n];
            for (int i = 0; i < h.Length; i++)
            {
                float t = Mathf.Clamp01((tuft[i] - 0.6f) * 3f);
                h[i] = 0.5f + (blades[i] - 0.5f) * 0.4f + (grain[i] - 0.5f) * 0.15f + t * 0.15f + (tone[i] - 0.5f) * 0.1f;
                mask[i] = t;
                aux[i] = Mathf.Clamp01((bare[i] - 0.62f) * 5f);
            }
            return Finish(n, h, tone, mask, aux);
        }

        /// <summary>Desert sand: wind ripples, fine grain, pebbles (Mask) and darker damp hollows (Aux).</summary>
        private static SurfaceFields Sand(int n)
        {
            float[] tone = Fbm(n, 6, 5, 51);
            float[] grain = HeightField.Grain(n, 52);
            // Pebble cells are at least four texels wide, so a pebble is a disc at every size (fewer at tiny sizes).
            int pebbleCells = Mathf.Min(40, n / 4);
            float[] pebbles = new float[n * n];
            HeightField.Cells(n, pebbleCells, 53, 0.9f, pebbles, null, null);
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
                    float ripple = Mathf.Sin((v * 10f + tone[i] * 0.12f) * Mathf.PI * 2f) * 0.5f + 0.5f;
                    ripple = Mathf.Pow(ripple, 1.5f);
                    float pebble = grain[i] > 0.992f ? 1f : 0f;
                    float peb = Mathf.Clamp01(1f - pebbles[i] / 0.2f) * (HeightField.Hash01(Mathf.FloorToInt(u * pebbleCells), Mathf.FloorToInt(v * pebbleCells), 5) > 0.9f ? 1f : 0f);
                    h[i] = 0.45f + ripple * 0.25f + (grain[i] - 0.5f) * 0.1f + (tone[i] - 0.5f) * 0.1f + peb * 0.25f;
                    mask[i] = Mathf.Max(pebble, peb);
                    aux[i] = Mathf.Clamp01((0.4f - tone[i]) * 3f);
                }
            }
            return Finish(n, h, tone, mask, aux);
        }

        /// <summary>Dirt: clods and footprints from a cell field, dry cracks (Mask) and pebbles (Aux).</summary>
        private static SurfaceFields Dirt(int n)
        {
            float[] tone = Fbm(n, 7, 5, 61);
            float[] clods = new float[n * n];
            HeightField.Cells(n, 10, 62, 1f, clods, null, null);
            float[] fine = Fbm(n, 40, 2, 63, 0.6f);
            float[] crack = Cracks(n, 6, 64, 0.01f);
            float[] grain = HeightField.Grain(n, 65);
            float[] h = new float[n * n];
            float[] mask = new float[n * n];
            float[] aux = new float[n * n];
            for (int i = 0; i < h.Length; i++)
            {
                float clod = Mathf.Clamp01(1f - clods[i] / 0.7f);
                float pebble = grain[i] > 0.97f ? 1f : 0f;
                h[i] = 0.4f + clod * 0.3f + (fine[i] - 0.5f) * 0.2f + (tone[i] - 0.5f) * 0.1f - crack[i] * 0.25f + pebble * 0.15f;
                mask[i] = crack[i];
                aux[i] = pebble;
            }
            return Finish(n, h, tone, mask, aux);
        }

        /// <summary>Rock: strata bands (stratas per tile), fractures (Mask) and mineral veins (Aux). Shared by the badlands and the Underheart.</summary>
        private static SurfaceFields Rock(int n, int seed, float strataCount)
        {
            float[] tone = Fbm(n, 4, 6, seed);
            float[] warp = Fbm(n, 3, 3, seed + 1);
            float[] frac = Cracks(n, 6, seed + 5, 0.014f);
            float[] vein = Cracks(n, 8, seed + 9, 0.01f);
            float[] grain = HeightField.Grain(n, seed + 11);
            float[] fine = Fbm(n, 32, 2, seed + 13, 0.6f);
            float[] h = new float[n * n];
            float[] mask = new float[n * n];
            float[] aux = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;
                for (int x = 0; x < n; x++)
                {
                    int i = y * n + x;
                    float strata = Mathf.Sin((v * strataCount + warp[i] * 0.6f) * Mathf.PI * 2f) * 0.5f + 0.5f;
                    strata = Mathf.Pow(strata, 2f);
                    h[i] = 0.4f + strata * 0.25f + (tone[i] - 0.5f) * 0.3f + (fine[i] - 0.5f) * 0.15f + (grain[i] - 0.5f) * 0.05f - frac[i] * 0.3f;
                    mask[i] = frac[i];
                    aux[i] = vein[i];
                }
            }
            return Finish(n, h, tone, mask, aux);
        }

        /// <summary>Tar roof gravel: packed round stones from a dense cell field.</summary>
        private static SurfaceFields Gravel(int n)
        {
            float[] tone = Fbm(n, 6, 3, 81);
            float[] f1 = new float[n * n];
            float[] id = new float[n * n];
            HeightField.Cells(n, 24, 82, 1f, f1, null, id);
            float[] grain = HeightField.Grain(n, 83);
            float[] h = new float[n * n];
            float[] mask = new float[n * n];
            for (int i = 0; i < h.Length; i++)
            {
                float stone = Mathf.Clamp01(1f - f1[i] / 0.65f);
                stone = Mathf.Sqrt(stone) * (0.7f + id[i] * 0.3f);
                h[i] = 0.25f + stone * 0.6f + (grain[i] - 0.5f) * 0.08f;
                mask[i] = id[i];
            }
            return Finish(n, h, tone, mask, null);
        }

        /// <summary>Clay roof tiles, 8 rows of 6 staggered tiles: each tile ramps up to its lower lip over the one below.</summary>
        private static SurfaceFields RoofTiles(int n)
        {
            float[] tone = Fbm(n, 6, 3, 85);
            float[] grain = HeightField.Grain(n, 86);
            float[] h = new float[n * n];
            float[] mask = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;
                float row = HeightField.Frac(v * 8f);
                int ry = Mathf.FloorToInt(v * 8f);
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n;
                    float cu = u * 6f + (ry % 2) * 0.5f;
                    float col = HeightField.Frac(cu);
                    int cx = Mathf.FloorToInt(cu);
                    int i = y * n + x;
                    float curve = Mathf.Sin(col * Mathf.PI) * 0.35f;
                    float gap = Mathf.Clamp01(1f - HeightField.EdgeDistance(cu) / 0.04f);
                    float lip = row < 0.12f ? row / 0.12f : 1f;
                    float wobble = (HeightField.Hash01(cx + ry * 7, 5, 0) - 0.5f) * 0.06f;
                    h[i] = 0.3f + row * 0.3f * lip + curve + wobble + (grain[i] - 0.5f) * 0.04f - gap * 0.3f;
                    mask[i] = Mathf.Max(gap, row < 0.06f ? 1f : 0f);
                }
            }
            return Finish(n, h, tone, mask, null);
        }

        /// <summary>Corrugated sheet: 16 ridges across, rust pits (Mask) and dents (Aux).</summary>
        private static SurfaceFields Corrugated(int n)
        {
            float[] tone = Fbm(n, 4, 3, 91);
            float[] rust = Fbm(n, 8, 4, 92);
            float[] dent = Fbm(n, 3, 2, 94);
            float[] grain = HeightField.Grain(n, 95);
            float[] h = new float[n * n];
            float[] mask = new float[n * n];
            float[] aux = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n;
                    int i = y * n + x;
                    float ridge = Mathf.Sin(u * Mathf.PI * 2f * 16f) * 0.5f + 0.5f;
                    float r = Mathf.Clamp01((rust[i] - 0.62f) * 3f);
                    h[i] = 0.2f + ridge * 0.6f + (dent[i] - 0.5f) * 0.1f - r * (grain[i] * 0.15f);
                    mask[i] = r;
                    aux[i] = Mathf.Clamp01((dent[i] - 0.6f) * 4f);
                }
            }
            return Finish(n, h, tone, mask, aux);
        }

        /// <summary>Plank wood, 5 planks across: grain stretched along v, plank seams (Mask), knots (Aux).</summary>
        private static SurfaceFields Wood(int n)
        {
            float[] tone = Fbm(n, 3, 4, 101);
            float[] grain = Fbm(n, 6, 4, 102, 0.5f, 4f, 0.5f);
            float[] fine = HeightField.Grain(n, 103);
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
                    int plank = Mathf.FloorToInt(u * 5f);
                    float seam = Mathf.Clamp01(1f - HeightField.EdgeDistance(u * 5f) / 0.02f);
                    float rings = Mathf.Sin((u * 5f + grain[i] * 2.5f + plank * 0.37f) * Mathf.PI * 2f * 3f) * 0.5f + 0.5f;
                    float kx = HeightField.Frac(u * 5f) - 0.5f, ky = HeightField.Frac(v + plank * 0.31f) - 0.5f;
                    float knot = HeightField.Hash01(plank, 7, 0) > 0.5f ? Mathf.Clamp01(1f - Mathf.Sqrt(kx * kx * 9f + ky * ky * 6f) / 0.5f) : 0f;
                    h[i] = 0.55f + (rings - 0.5f) * 0.15f + (grain[i] - 0.5f) * 0.1f + (fine[i] - 0.5f) * 0.05f - seam * 0.3f - knot * 0.1f;
                    mask[i] = seam;
                    aux[i] = knot;
                }
            }
            return Finish(n, h, tone, mask, aux);
        }

        /// <summary>Palm bark: deep vertical fissures (stretched along v), leaf scar rings (Mask).</summary>
        private static SurfaceFields Bark(int n)
        {
            float[] tone = Fbm(n, 4, 3, 111);
            float[] fiss = Fbm(n, 12, 3, 112, 0.5f, 1f, 0.25f);
            float[] fine = HeightField.Grain(n, 113);
            float[] h = new float[n * n];
            float[] mask = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;
                float ring = HeightField.Frac(v * 16f);
                for (int x = 0; x < n; x++)
                {
                    int i = y * n + x;
                    float deep = Mathf.Pow(fiss[i], 1.6f);
                    float scar = ring < 0.15f ? 1f - ring / 0.15f : 0f;
                    h[i] = 0.3f + deep * 0.5f + (fine[i] - 0.5f) * 0.08f + scar * 0.2f + (tone[i] - 0.5f) * 0.08f;
                    mask[i] = scar;
                }
            }
            return Finish(n, h, tone, mask, null);
        }

        /// <summary>Wind-packed snow crust: drift ripples, powder grain, broken crust plates (Mask) and glitter (Aux).</summary>
        private static SurfaceFields SnowCrust(int n)
        {
            float[] tone = Fbm(n, 6, 5, 211);
            float[] grain = HeightField.Grain(n, 212);
            float[] plates = new float[n * n];
            HeightField.Cells(n, 6, 213, 1f, null, plates, null);
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
                    float drift = Mathf.Sin((u * 3f + v * 5f + tone[i] * 0.6f) * Mathf.PI * 2f) * 0.5f + 0.5f;
                    float crack = Mathf.Clamp01(1f - plates[i] / 0.03f) * (tone[i] > 0.55f ? 1f : 0f);
                    h[i] = 0.3f + drift * drift * 0.4f + tone[i] * 0.25f + (grain[i] - 0.5f) * 0.08f - crack * 0.15f;
                    mask[i] = crack;
                    aux[i] = grain[i] > 0.9925f ? 1f : 0f;
                }
            }
            return Finish(n, h, tone, mask, aux);
        }

        /// <summary>Frost-dusted sand: the sand relief under blotches of thin snow (Mask is the snow cover).</summary>
        private static SurfaceFields SnowThin(int n)
        {
            SurfaceFields sand = Sand(n);
            float[] cover = Fbm(n, 8, 4, 223);
            float[] grain = HeightField.Grain(n, 224);
            float[] h = new float[n * n];
            float[] mask = new float[n * n];
            for (int i = 0; i < h.Length; i++)
            {
                float m = Mathf.Clamp01((cover[i] + (grain[i] - 0.5f) * 0.1f - 0.45f) / 0.12f);
                m = m * m * (3f - 2f * m);
                h[i] = Mathf.Lerp(sand.Height[i], 0.75f + (grain[i] - 0.5f) * 0.06f, m);
                mask[i] = m;
            }
            return Finish(n, h, sand.Tone, mask, sand.Aux);
        }

        /// <summary>Lake ice: an almost flat sheet with fracture grooves (Mask), frost bloom (Aux) and trapped bubbles.</summary>
        private static SurfaceFields Ice(int n)
        {
            float[] tone = Fbm(n, 4, 5, 431);
            float[] frost = Fbm(n, 8, 3, 433);
            float[] big = new float[n * n];
            float[] small = new float[n * n];
            HeightField.Cells(n, 5, 401, 0.7f, null, big, null);
            HeightField.Cells(n, 11, 409, 0.7f, null, small, null);
            float[] hair = Cracks(n, 4, 417, 0.006f);
            float[] grain = HeightField.Grain(n, 434);
            float[] h = new float[n * n];
            float[] mask = new float[n * n];
            float[] aux = new float[n * n];
            for (int i = 0; i < h.Length; i++)
            {
                float c = Mathf.Clamp01(1f - big[i] / 0.03f);
                c = Mathf.Max(c, Mathf.Clamp01(1f - small[i] / 0.04f) * 0.65f);
                c = Mathf.Max(c, hair[i] * 0.45f);
                float bubble = grain[i] > 0.996f ? 1f : 0f;
                h[i] = 0.85f - c * 0.45f + (tone[i] - 0.5f) * 0.08f - bubble * 0.1f;
                mask[i] = c;
                aux[i] = Mathf.Clamp01((frost[i] - 0.6f) * 1.6f);
            }
            return Finish(n, h, tone, mask, aux);
        }

        /// <summary>Gentle water ripples for the normal map of canals and cooled pools.</summary>
        private static SurfaceFields Ripples(int n)
        {
            float[] tone = Fbm(n, 4, 3, 501);
            float[] h = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n;
                    int i = y * n + x;
                    float a = Mathf.Sin((u * 5f + v * 2f + tone[i] * 0.4f) * Mathf.PI * 2f);
                    float b = Mathf.Sin((u * -3f + v * 6f + tone[i] * 0.3f) * Mathf.PI * 2f);
                    h[i] = 0.5f + a * 0.2f + b * 0.15f + (tone[i] - 0.5f) * 0.2f;
                }
            }
            return Finish(n, h, tone, null, null);
        }
    }
}
