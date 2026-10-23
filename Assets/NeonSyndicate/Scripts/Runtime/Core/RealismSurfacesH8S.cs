using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// One generated H8S surface: albedo (sRGB bytes, the game renders in the gamma colour space), tangent-space normal,
    /// the Standard and URP Lit mask (R metallic, G occlusion, A smoothness) and, for crystals, an emission map. Plain
    /// arrays, n by n, tileable.
    /// </summary>
    public sealed class RealismMapsH8S
    {
        public int Size;
        public Color32[] Albedo;
        public Color32[] Normal;
        public Color32[] Mask;
        public Color32[] Emission;
    }

    /// <summary>
    /// The H8S material generators (engine-free, VisualRealismH8STests): weathered mineral ground for the open land (it
    /// replaces the bright striped desert sand), gothic ashlar masonry, worn flagstone paving, tarnished bronze, and glassy
    /// amethyst and turquoise crystal, plus low-frequency macro maps for the detail slot that break visible tiling. Colours
    /// come from the owner's canon palette (Gothic Heart charcoal, weathered stone and bronze, Amethyst violets, Azure
    /// teals); every map is tileable and derived from one set of fields so albedo, relief, gloss and occlusion agree.
    /// Sizes are chosen by the caller (the graphics preset); the maths does not depend on Unity.
    /// </summary>
    public static class RealismSurfacesH8S
    {
        // ------------------------------------------------------------------ the canon palette (sRGB swatches)

        public static readonly Color HeartCharcoal = RealismRulesH8S.Hex(0x15, 0x19, 0x1f);
        public static readonly Color HeartStone = RealismRulesH8S.Hex(0x31, 0x37, 0x3f);
        public static readonly Color HeartWeathered = RealismRulesH8S.Hex(0x66, 0x5b, 0x4e);
        public static readonly Color HeartBronze = RealismRulesH8S.Hex(0xad, 0x87, 0x4f);
        public static readonly Color HeartParchment = RealismRulesH8S.Hex(0xc9, 0xb9, 0x8d);
        public static readonly Color AmethystDeep = RealismRulesH8S.Hex(0x32, 0x21, 0x42);
        public static readonly Color AmethystMid = RealismRulesH8S.Hex(0x64, 0x3f, 0x83);
        public static readonly Color AmethystGlow = RealismRulesH8S.Hex(0x9d, 0x67, 0xc1);
        public static readonly Color AmethystLight = RealismRulesH8S.Hex(0xc6, 0xa2, 0xe4);
        public static readonly Color AzureDeep = RealismRulesH8S.Hex(0x13, 0x50, 0x58);
        public static readonly Color AzureMid = RealismRulesH8S.Hex(0x17, 0x8d, 0x98);
        public static readonly Color AzureGlow = RealismRulesH8S.Hex(0x46, 0xc4, 0xc8);
        public static readonly Color AzureLight = RealismRulesH8S.Hex(0x9b, 0xdb, 0xd3);

        /// <summary>Macro map kinds for the detail slot.</summary>
        public const int MacroStone = 0, MacroGround = 1, MacroPaving = 2;

        // ------------------------------------------------------------------ shared helpers

        private static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        private static float Frac(float x) { return x - Mathf.Floor(x); }

        private static Color Lerp(Color a, Color b, float t) { return Color.Lerp(a, b, Mathf.Clamp01(t)); }

        private static Color32 Pack(Color c)
        {
            return new Color32(SurfaceMaps.ToByte(c.r), SurfaceMaps.ToByte(c.g), SurfaceMaps.ToByte(c.b), 255);
        }

        private static Color32[] Masks(int count, float[] metallic, float metallicConst, float[] occlusion, float[] smoothness)
        {
            return SurfaceMaps.PackMask(count, metallic, metallicConst, occlusion, smoothness, 0f);
        }

        /// <summary>
        /// A running-bond course layout: rows across the tile, each with its own block count, random block widths and a
        /// random phase, so joints stagger and the pattern still tiles (widths are fractions of the period).
        /// </summary>
        private sealed class Courses
        {
            public readonly int Rows;
            private readonly float[] _phase;
            private readonly float[][] _bounds;

            public Courses(int rows, int minBlocks, int maxBlocks, int seed)
            {
                Rows = rows;
                _phase = new float[rows];
                _bounds = new float[rows][];
                for (int r = 0; r < rows; r++)
                {
                    int k = minBlocks + Mathf.FloorToInt(HeightField.Hash01(r, 3, seed) * (maxBlocks - minBlocks + 1));
                    k = Mathf.Clamp(k, minBlocks, maxBlocks);
                    float[] w = new float[k];
                    float sum = 0f;
                    for (int j = 0; j < k; j++) { w[j] = 0.7f + 0.6f * HeightField.Hash01(r, j + 11, seed); sum += w[j]; }
                    float[] b = new float[k + 1];
                    for (int j = 0; j < k; j++) b[j + 1] = b[j] + w[j] / sum;
                    b[k] = 1f;
                    _bounds[r] = b;
                    _phase[r] = HeightField.Hash01(r, 29, seed);
                }
            }

            /// <summary>Block lookup: row, block id, distances (tile units) to the block's left, right, bottom and top edges.</summary>
            public void Locate(float u, float v, out int row, out int id, out float dl, out float dr, out float db, out float dt)
            {
                float rv = v * Rows;
                row = Mathf.Min(Rows - 1, Mathf.FloorToInt(rv));
                float fv = rv - row;
                db = fv / Rows;
                dt = (1f - fv) / Rows;
                float[] b = _bounds[row];
                float up = Frac(u + _phase[row]);
                int j = 0;
                while (j < b.Length - 2 && up >= b[j + 1]) j++;
                dl = up - b[j];
                dr = b[j + 1] - up;
                id = row * 16 + j;
            }
        }

        // ------------------------------------------------------------------ weathered mineral ground (the open land)

        /// <summary>
        /// Weathered mineral ground for one 12 m world tile (it replaces the old desert sand): broad patches of pale dust
        /// and darker compacted earth in the Gothic Heart's weathered stone tones, fine dust ripples only in drifts (no
        /// long regular stripes), scattered pebbles and a few stones, and hairline cracks where the earth is packed.
        /// </summary>
        public static RealismMapsH8S Ground(int n)
        {
            int count = n * n;
            float[] patches = HeightField.Fbm(n, 3, 5, 701, 0.55f);
            float[] fine = HeightField.Fbm(n, Mathf.Min(n, 48), 3, 702, 0.6f);
            float[] drift = HeightField.Fbm(n, 4, 4, 703);
            float[] warp = HeightField.Fbm(n, 6, 3, 704);
            float[] packed = HeightField.Fbm(n, 5, 4, 705);
            float[] grain = HeightField.Grain(n, 706);
            int pebbleCells = Mathf.Clamp(n / 6, 8, 160);
            float[] pebF1 = new float[count], pebId = new float[count];
            HeightField.Cells(n, pebbleCells, 707, 0.85f, pebF1, null, pebId);
            int stoneCells = Mathf.Clamp(n / 28, 4, 40);
            float[] stoneF1 = new float[count], stoneId = new float[count];
            HeightField.Cells(n, stoneCells, 708, 0.8f, stoneF1, null, stoneId);
            int crackCells = Mathf.Clamp(n / 40, 3, 24);
            float[] crackEdge = new float[count];
            HeightField.Cells(n, crackCells, 709, 1f, null, crackEdge, null);

            Color dust = RealismRulesH8S.Hex(0x7c, 0x71, 0x62);
            Color earth = RealismRulesH8S.Hex(0x4f, 0x47, 0x3d);
            Color grey = RealismRulesH8S.Hex(0x5d, 0x5b, 0x57);
            float[] h = new float[count];
            Color[] col = new Color[count];
            float[] smooth = new float[count], occ = new float[count];
            float tau = Mathf.PI * 2f;
            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n;
                    int i = y * n + x;
                    // Dust ripples: integer wave numbers keep the tile seamless, a slow warp bends them, and they only
                    // form in drifts (about a third of the ground), so no straight stripes run across the land.
                    float phase = tau * (31f * u + 13f * v) + (warp[i] - 0.5f) * 9f;
                    float ripple = 0.5f + 0.5f * Mathf.Sin(phase);
                    float drifts = Smooth(0.52f, 0.7f, drift[i]);
                    float pack = Smooth(0.55f, 0.72f, packed[i]) * (1f - drifts);
                    // Pebbles: a small share of cells hold a pebble, a disc with a rounded top.
                    float pebOn = pebId[i] > 0.9f ? 1f : 0f;
                    float peb = pebOn * Smooth(0.42f, 0.18f, pebF1[i]);
                    float stoneOn = stoneId[i] > 0.94f ? 1f : 0f;
                    float stone = stoneOn * Smooth(0.36f, 0.16f, stoneF1[i]);
                    float crack = pack * Smooth(0.035f, 0.0f, crackEdge[i]);
                    float hh = 0.45f + (patches[i] - 0.5f) * 0.18f + (fine[i] - 0.5f) * 0.1f + (grain[i] - 0.5f) * 0.05f;
                    hh += (ripple - 0.5f) * 0.07f * drifts;
                    hh += Mathf.Sqrt(peb) * 0.11f + Mathf.Sqrt(stone) * 0.24f;
                    hh -= crack * 0.12f;
                    h[i] = hh;

                    Color c = Lerp(earth, dust, patches[i] * 1.25f - 0.12f + (fine[i] - 0.5f) * 0.3f);
                    c = Lerp(c, grey, Smooth(0.6f, 0.85f, warp[i]) * 0.45f);
                    c = Lerp(c, dust * 1.04f, drifts * (0.25f + ripple * 0.15f));
                    c *= 1f - pack * 0.1f;
                    float pebTone = HeightField.Hash01(Mathf.RoundToInt(pebId[i] * 4096f), 1, 711);
                    Color pebColor = Lerp(RealismRulesH8S.Hex(0x4c, 0x47, 0x40), RealismRulesH8S.Hex(0x86, 0x7c, 0x6c), pebTone);
                    c = Lerp(c, pebColor, peb);
                    Color stoneColor = Lerp(RealismRulesH8S.Hex(0x48, 0x45, 0x40), HeartWeathered, HeightField.Hash01(Mathf.RoundToInt(stoneId[i] * 4096f), 2, 712));
                    c = Lerp(c, stoneColor, stone);
                    c *= 1f - crack * 0.35f;
                    c *= 0.94f + grain[i] * 0.12f;
                    col[i] = c;
                    smooth[i] = 0.07f + pack * 0.05f + peb * 0.14f + stone * 0.16f;
                    occ[i] = 1f - crack * 0.5f;
                }
            }
            return Finish(n, h, col, smooth, occ, null, 0f, 1.5f, 0.15f);
        }

        // ------------------------------------------------------------------ gothic ashlar masonry

        /// <summary>
        /// Gothic ashlar for one 3.33 m world tile (the 04O-B stone): six staggered courses of dressed blocks in charcoal
        /// and weathered stone, each block its own tone, recessed dark mortar, chipped arrises, worn lighter edges, soot
        /// blotches and faint rain streaks, with rougher mortar and softer polished edges in the gloss.
        /// </summary>
        public static RealismMapsH8S Ashlar(int n)
        {
            const float tile = 1f / 0.3f;
            int count = n * n;
            Courses bond = new Courses(6, 3, 5, 811);
            float[] area = HeightField.Fbm(n, 3, 4, 812);
            float[] face = HeightField.Fbm(n, 12, 3, 813, 0.55f);
            float[] fine = HeightField.Fbm(n, Mathf.Min(n, 64), 3, 821, 0.6f);
            float[] chips = HeightField.Fbm(n, 20, 3, 814, 0.55f);
            float[] soot = HeightField.Fbm(n, 4, 4, 815);
            float[] streak = HeightField.Fbm(n, 6, 3, 816, 0.5f, 6f, 1f);
            float[] grain = HeightField.Grain(n, 817);
            float[] h = new float[count];
            Color[] col = new Color[count];
            float[] smooth = new float[count], occ = new float[count];
            Color mortar = RealismRulesH8S.Hex(0x4a, 0x47, 0x42);
            Color brown = RealismRulesH8S.Hex(0x58, 0x4f, 0x44);
            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n;
                    int i = y * n + x;
                    int row, id;
                    float dl, dr, db, dt;
                    bond.Locate(u, v, out row, out id, out dl, out dr, out db, out dt);
                    float edge = Mathf.Min(Mathf.Min(dl, dr), Mathf.Min(db, dt)) * tile;
                    float chip = chips[i];
                    float eroded = edge - chip * chip * chip * 0.028f;
                    float joint = 0.007f;
                    float bevel = Mathf.Clamp01((eroded - joint) / 0.018f);
                    bool inJoint = eroded < joint;
                    float bh = HeightField.Hash01(id, 7, 818);
                    float hh;
                    if (inJoint) hh = 0.22f + (grain[i] - 0.5f) * 0.04f;
                    else hh = 0.3f + Mathf.Sqrt(bevel) * 0.4f + (face[i] - 0.5f) * 0.1f + (fine[i] - 0.5f) * 0.06f + (bh - 0.5f) * 0.05f - (grain[i] > 0.985f ? 0.04f : 0f);
                    h[i] = hh;

                    float pick = HeightField.Hash01(id, 9, 819);
                    Color c = Lerp(HeartStone, HeartWeathered, pick * 0.8f + (area[i] - 0.5f) * 0.5f);
                    c = Lerp(c, brown, HeightField.Hash01(id, 21, 819) > 0.7f ? 0.55f : 0f);
                    float warm = HeightField.Hash01(id, 13, 820) - 0.5f;
                    c = new Color(c.r * (1f + warm * 0.05f), c.g, c.b * (1f - warm * 0.05f), 1f);
                    c *= 0.86f + face[i] * 0.18f + (fine[i] - 0.5f) * 0.22f + (grain[i] - 0.5f) * 0.08f;
                    c *= 1f - Smooth(0.55f, 0.82f, soot[i]) * 0.3f;
                    c *= 1f - Smooth(0.45f, 0.8f, streak[i]) * 0.1f;
                    float arris = inJoint ? 0f : (1f - bevel);
                    c *= 1f + arris * 0.1f;
                    if (inJoint) c = mortar * (0.8f + grain[i] * 0.3f) * (1f - Smooth(0.55f, 0.82f, soot[i]) * 0.25f);
                    col[i] = c;
                    smooth[i] = inJoint ? 0.06f : 0.15f + bh * 0.12f + (1f - bevel) * 0.05f + (fine[i] - 0.5f) * 0.06f - Smooth(0.55f, 0.82f, soot[i]) * 0.04f;
                    occ[i] = inJoint ? 0.62f + grain[i] * 0.1f : 1f;
                }
            }
            return Finish(n, h, col, smooth, occ, null, 0f, 2.6f, 0.3f);
        }

        // ------------------------------------------------------------------ worn flagstone paving

        /// <summary>
        /// Worn flagstones for one 3 m world tile (the 04O-B paving): four rows of irregular slabs in weathered stone and
        /// cool grey, dirt in the joints, chipped and rounded edges, slight slab tilt, fine cracks across a quarter of the
        /// slabs, and centres polished smoother and paler by feet.
        /// </summary>
        public static RealismMapsH8S Flagstone(int n)
        {
            const float tile = 3f;
            int count = n * n;
            Courses bond = new Courses(4, 2, 4, 911);
            float[] area = HeightField.Fbm(n, 3, 4, 912);
            float[] face = HeightField.Fbm(n, 10, 3, 913, 0.55f);
            float[] fine = HeightField.Fbm(n, Mathf.Min(n, 64), 3, 919, 0.6f);
            float[] chips = HeightField.Fbm(n, 20, 3, 914, 0.55f);
            float[] stain = HeightField.Fbm(n, 5, 4, 915);
            float[] crackN = HeightField.Fbm(n, 6, 3, 916);
            float[] grain = HeightField.Grain(n, 917);
            float[] h = new float[count];
            Color[] col = new Color[count];
            float[] smooth = new float[count], occ = new float[count];
            Color dark = RealismRulesH8S.Hex(0x55, 0x50, 0x49);
            Color light = RealismRulesH8S.Hex(0x7e, 0x74, 0x66);
            Color cool = RealismRulesH8S.Hex(0x63, 0x64, 0x66);
            Color dirt = RealismRulesH8S.Hex(0x3a, 0x35, 0x2e);
            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n;
                    int i = y * n + x;
                    int row, id;
                    float dl, dr, db, dt;
                    bond.Locate(u, v, out row, out id, out dl, out dr, out db, out dt);
                    float edge = Mathf.Min(Mathf.Min(dl, dr), Mathf.Min(db, dt)) * tile;
                    float chip = chips[i];
                    float eroded = edge - chip * chip * chip * 0.035f;
                    float joint = 0.008f;
                    float bevel = Mathf.Clamp01((eroded - joint) / 0.024f);
                    bool inJoint = eroded < joint;
                    float sh = HeightField.Hash01(id, 5, 918);
                    float tiltU = (HeightField.Hash01(id, 6, 918) - 0.5f) * 0.06f, tiltV = (HeightField.Hash01(id, 7, 918) - 0.5f) * 0.06f;
                    float centre = Mathf.Clamp01(Mathf.Min(Mathf.Min(dl, dr), Mathf.Min(db, dt)) * tile / 0.25f);
                    bool cracked = HeightField.Hash01(id, 8, 918) > 0.75f;
                    float crack = cracked && !inJoint ? Smooth(0.006f, 0.0f, Mathf.Abs(crackN[i] - 0.5f)) * bevel : 0f;
                    float hh;
                    if (inJoint) hh = 0.18f + (grain[i] - 0.5f) * 0.05f;
                    else hh = 0.35f + Mathf.Sqrt(bevel) * 0.3f + (face[i] - 0.5f) * 0.07f + (fine[i] - 0.5f) * 0.05f + (dl - dr) * tiltU * 4f + (db - dt) * tiltV * 4f - crack * 0.08f;
                    h[i] = hh;

                    Color c = Lerp(dark, light, sh * 0.8f + (area[i] - 0.5f) * 0.4f);
                    if (HeightField.Hash01(id, 9, 918) > 0.72f) c = Lerp(c, cool, 0.7f);
                    c *= 0.88f + face[i] * 0.14f + (fine[i] - 0.5f) * 0.2f + (grain[i] - 0.5f) * 0.07f;
                    c *= 1f + centre * 0.05f;
                    c *= 1f - Smooth(0.58f, 0.85f, stain[i]) * 0.18f;
                    c *= 1f - crack * 0.25f;
                    if (inJoint) c = dirt * (0.8f + grain[i] * 0.4f);
                    col[i] = c;
                    smooth[i] = inJoint ? 0.05f : 0.18f + centre * 0.14f + sh * 0.05f + (fine[i] - 0.5f) * 0.05f - crack * 0.08f;
                    occ[i] = inJoint ? 0.6f : 1f - crack * 0.3f;
                }
            }
            return Finish(n, h, col, smooth, occ, null, 0f, 2.2f, 0.25f);
        }

        // ------------------------------------------------------------------ tarnished bronze

        /// <summary>
        /// Tarnished bronze for trims and roofs (the 04O-B metal, the canon's tarnished bronze): hammered panels with
        /// standing seams, dark warm bronze where it is handled and exposed, dark oxide in broad blotches and faint
        /// verdigris in recesses; metallic and gloss fall where the oxide and verdigris lie.
        /// </summary>
        public static RealismMapsH8S Bronze(int n)
        {
            int count = n * n;
            int dentCells = Mathf.Clamp(n / 10, 6, 64);
            float[] dentF1 = new float[count];
            HeightField.Cells(n, dentCells, 1011, 0.9f, dentF1, null, null);
            float[] oxide = HeightField.Fbm(n, 4, 5, 1012);
            float[] verd = HeightField.Fbm(n, 7, 4, 1013);
            float[] fine = HeightField.Fbm(n, Mathf.Min(n, 32), 3, 1014, 0.6f, 1.5f, 0.6f);
            float[] grain = HeightField.Grain(n, 1015);
            float[] h = new float[count];
            Color[] col = new Color[count];
            float[] smooth = new float[count], occ = new float[count], metal = new float[count];
            Color bronze = RealismRulesH8S.Hex(0x5e, 0x48, 0x2f);
            Color dark = RealismRulesH8S.Hex(0x2b, 0x28, 0x24);
            Color green = RealismRulesH8S.Hex(0x3e, 0x51, 0x4a);
            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;
                float seamV = Frac(v * 3f);
                float seam = Smooth(0.02f, 0.0f, Mathf.Min(seamV, 1f - seamV));
                for (int x = 0; x < n; x++)
                {
                    int i = y * n + x;
                    float dent = 1f - Mathf.Clamp01(dentF1[i]);
                    float hh = 0.5f + dent * dent * 0.1f + seam * 0.25f + (fine[i] - 0.5f) * 0.03f;
                    h[i] = hh;
                    float ox = Smooth(0.45f, 0.72f, oxide[i]);
                    float vg = Smooth(0.62f, 0.85f, verd[i]) * (0.4f + 0.6f * (1f - dent));
                    Color c = Lerp(bronze, dark, ox * 0.8f);
                    c = Lerp(c, green, vg * 0.7f);
                    c *= 0.9f + (fine[i] - 0.5f) * 0.12f + (grain[i] - 0.5f) * 0.06f + dent * 0.08f;
                    c *= 1f + seam * 0.12f;
                    col[i] = c;
                    metal[i] = Mathf.Lerp(Mathf.Lerp(0.82f, 0.35f, ox), 0.05f, vg);
                    smooth[i] = Mathf.Lerp(Mathf.Lerp(0.5f, 0.24f, ox), 0.14f, vg) + seam * 0.06f + (grain[i] - 0.5f) * 0.03f;
                    occ[i] = 1f - vg * 0.25f;
                }
            }
            return Finish(n, h, col, smooth, occ, metal, 0f, 1.6f, 0.3f);
        }

        // ------------------------------------------------------------------ glassy crystal

        /// <summary>
        /// Glassy mineral crystal (amethyst, or turquoise when teal): flat tilted sub-facets in the normal map so light
        /// breaks across the face, a deep body that is lighter where light passes through a facet, cloudy inclusions, and
        /// thin internal fractures that carry the glow. Smooth and non-metallic so the sky's reflection does the work;
        /// the emission map keeps the glow local (fractures and facet edges, a faint body), never a flat neon wash.
        /// </summary>
        public static RealismMapsH8S Crystal(int n, bool teal)
        {
            int count = n * n;
            int facetCells = 7;
            float[] f1 = new float[count], edge = new float[count], id = new float[count];
            HeightField.Cells(n, facetCells, teal ? 1111 : 1121, 0.9f, f1, edge, id);
            float[] cloud = HeightField.Fbm(n, 5, 4, teal ? 1112 : 1122);
            float[] fine = HeightField.Fbm(n, Mathf.Min(n, 48), 2, teal ? 1113 : 1123, 0.6f);
            Color deep = teal ? AzureDeep : AmethystDeep;
            Color mid = teal ? AzureMid : AmethystMid;
            Color glow = teal ? AzureGlow : AmethystGlow;
            Color light = teal ? AzureLight : AmethystLight;
            Color32[] normal = new Color32[count];
            Color32[] albedo = new Color32[count];
            Color32[] emission = new Color32[count];
            float[] smooth = new float[count];
            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n;
                    int i = y * n + x;
                    int cellId = Mathf.RoundToInt(id[i] * 65536f);
                    float tx = (HeightField.Hash01(cellId, 1, 1131) - 0.5f) * 0.7f;
                    float ty = (HeightField.Hash01(cellId, 2, 1131) - 0.5f) * 0.7f;
                    float ridge = Smooth(0.06f, 0.0f, edge[i]);
                    // Facet edges blend back toward the surface normal so each facet reads as a separate cut plane.
                    float fx = tx * (1f - ridge), fy = ty * (1f - ridge);
                    float inv = 1f / Mathf.Sqrt(fx * fx + fy * fy + 1f);
                    normal[i] = new Color32((byte)((fx * inv * 0.5f + 0.5f) * 255f), (byte)((fy * inv * 0.5f + 0.5f) * 255f), (byte)((inv * 0.5f + 0.5f) * 255f), 255);
                    // Cleavage: straight parallel fracture planes (integer slopes keep the tile seamless); each facet shows
                    // one family at its own phase, and only some facets are fractured, so lines stop at facet edges.
                    bool fractured = HeightField.Hash01(cellId, 6, 1131) < 0.6f;
                    float a = HeightField.Hash01(cellId, 4, 1131) < 0.5f ? 3f * u + v : -2f * u + 3f * v;
                    float s = Frac(a * 2f + HeightField.Hash01(cellId, 5, 1131));
                    float d = Mathf.Min(s, 1f - s);
                    float fracture = fractured ? Smooth(0.018f, 0.0f, d) : 0f;
                    float through = HeightField.Hash01(cellId, 3, 1131);
                    Color body = Lerp(deep, mid, through * 0.7f + (cloud[i] - 0.5f) * 0.6f + (fine[i] - 0.5f) * 0.2f);
                    body = Lerp(body, light, fracture * 0.5f + ridge * 0.14f);
                    albedo[i] = Pack(body);
                    float e = 0.1f + fracture * 0.85f + ridge * 0.32f + Smooth(0.6f, 0.9f, cloud[i]) * 0.14f;
                    emission[i] = Pack(glow * Mathf.Clamp01(e));
                    smooth[i] = 0.9f - fracture * 0.22f - ridge * 0.08f;
                }
            }
            RealismMapsH8S maps = new RealismMapsH8S();
            maps.Size = n;
            maps.Albedo = albedo;
            maps.Normal = normal;
            maps.Mask = Masks(count, null, 0f, null, smooth);
            maps.Emission = emission;
            return maps;
        }

        // ------------------------------------------------------------------ macro variation (detail slot)

        /// <summary>
        /// A low-frequency macro map for the Standard (and URP Lit) multiply-by-two detail slot: 0.5 is neutral, so it only
        /// varies the surface at the scale of whole walls and yards (weathering blotches, soot, faint vertical streaks on
        /// stone; pale dust and dark earth patches on ground; dirt on paving) and breaks up the repetition of the main map.
        /// Tiled a fraction of once per main repeat by the material, so one macro period spans tens of metres.
        /// </summary>
        public static Color32[] Macro(int n, int kind)
        {
            int count = n * n;
            int seed = 1201 + kind * 37;
            float[] large = HeightField.Fbm(n, 2, 5, seed, 0.55f);
            float[] mid = HeightField.Fbm(n, 5, 4, seed + 1);
            float[] hue = HeightField.Fbm(n, 3, 3, seed + 2);
            float[] streak = HeightField.Fbm(n, 5, 3, seed + 3, 0.5f, 7f, 1f);
            Color32[] px = new Color32[count];
            for (int i = 0; i < count; i++)
            {
                float k;
                float warmth = (hue[i] - 0.5f) * 2f;
                if (kind == MacroGround)
                {
                    k = 1f + (large[i] - 0.5f) * 0.42f + (mid[i] - 0.5f) * 0.16f;
                    warmth *= 0.05f;
                }
                else if (kind == MacroPaving)
                {
                    k = 1f + (large[i] - 0.5f) * 0.24f - Smooth(0.6f, 0.85f, mid[i]) * 0.14f;
                    warmth *= 0.03f;
                }
                else
                {
                    k = 1f + (large[i] - 0.5f) * 0.28f - Smooth(0.58f, 0.85f, mid[i]) * 0.2f - Smooth(0.5f, 0.85f, streak[i]) * 0.07f;
                    warmth *= 0.035f;
                }
                // Neutral is 0.5 (the slot multiplies by two), with a gentle warm or cool lean.
                float r = 0.5f * k * (1f + warmth), g = 0.5f * k, b = 0.5f * k * (1f - warmth);
                px[i] = new Color32(SurfaceMaps.ToByte(r), SurfaceMaps.ToByte(g), SurfaceMaps.ToByte(b), 255);
            }
            return px;
        }

        // ------------------------------------------------------------------ finishing

        /// <summary>Normal from the height field, cavity dirt and occlusion from its shading terms, and the packed mask.</summary>
        private static RealismMapsH8S Finish(int n, float[] h, Color[] col, float[] smooth, float[] occ, float[] metal, float metalConst, float normalStrength, float cavityDirt)
        {
            int count = n * n;
            HeightField.Clamp(h);
            SurfaceShade shade = SurfaceMaps.Shade(h, n, 6f);
            Color32[] albedo = new Color32[count];
            float[] o = new float[count];
            for (int i = 0; i < count; i++)
            {
                float wear = (1f - shade.Cavity[i] * cavityDirt) * (1f + shade.Convex[i] * 0.08f);
                Color c = col[i] * wear;
                albedo[i] = Pack(c);
                o[i] = Mathf.Clamp01(occ[i] * SurfaceMaps.Occlusion(shade.Cavity[i], 0.7f));
                smooth[i] = Mathf.Clamp01(smooth[i] - shade.Cavity[i] * 0.06f);
            }
            RealismMapsH8S maps = new RealismMapsH8S();
            maps.Size = n;
            maps.Albedo = albedo;
            maps.Normal = SurfaceMaps.Normal(h, n, normalStrength * n / 256f);
            maps.Mask = Masks(count, metal, metalConst, o, smooth);
            return maps;
        }

        /// <summary>Mean albedo of a map in sRGB 0..1 (tests and the owner guide's palette check).</summary>
        public static Color MeanAlbedo(Color32[] px)
        {
            double r = 0, g = 0, b = 0;
            for (int i = 0; i < px.Length; i++) { r += px[i].r; g += px[i].g; b += px[i].b; }
            double k = 1.0 / (255.0 * Mathf.Max(1, px.Length));
            return new Color((float)(r * k), (float)(g * k), (float)(b * k), 1f);
        }
    }
}
