using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Shading terms derived from a height field: Cavity is how far a texel sits below its surroundings (crevices,
    /// grout, cracks: they collect dirt and lose gloss), Convex how far above (edges and ridges: they get polished and
    /// catch light). Both are 0..1 and tileable.
    /// </summary>
    public sealed class SurfaceShade
    {
        public readonly float[] Cavity;
        public readonly float[] Convex;

        public SurfaceShade(float[] cavity, float[] convex)
        {
            Cavity = cavity;
            Convex = convex;
        }
    }

    /// <summary>
    /// Pure derivations from a height field: the tangent-space normal map, the cavity and convexity (curvature) terms,
    /// the occlusion channel and the packed Standard-shader mask (R metallic, G occlusion, A smoothness). Only Mathf
    /// and plain arrays, so the maths is tested offline; Tex uploads the results.
    /// </summary>
    public static class SurfaceMaps
    {
        /// <summary>Normal map texels (x in R, y in G, z in B, A 255) from a tileable height field; strength scales the slope.</summary>
        public static Color32[] Normal(float[] h, int n, float strength)
        {
            Color32[] dst = new Color32[n * n];
            int[] before;
            int[] after;
            HeightField.WrapIndices(n, out before, out after);
            for (int y = 0; y < n; y++)
            {
                int up = after[y] * n;
                int down = before[y] * n;
                int row = y * n;
                for (int x = 0; x < n; x++)
                {
                    float nx = (h[row + before[x]] - h[row + after[x]]) * strength;
                    float ny = (h[down + x] - h[up + x]) * strength;
                    float inv = 1f / Mathf.Sqrt(nx * nx + ny * ny + 1f);
                    dst[row + x] = new Color32((byte)((nx * inv * 0.5f + 0.5f) * 255f), (byte)((ny * inv * 0.5f + 0.5f) * 255f), (byte)((inv * 0.5f + 0.5f) * 255f), 255);
                }
            }
            return dst;
        }

        /// <summary>
        /// Cavity and convexity from the difference between the height and two blurred copies (a narrow and a wide
        /// radius, in texels), scaled by gain. Narrow catches grout and cracks, wide catches hollows and ridges.
        /// </summary>
        public static SurfaceShade Shade(float[] h, int n, float gain)
        {
            int narrow = Mathf.Max(1, n / 128);
            int wide = Mathf.Max(2, n / 32);
            float[] bn = HeightField.Blur(h, n, narrow);
            float[] bw = HeightField.Blur(h, n, wide);
            float[] cavity = new float[n * n];
            float[] convex = new float[n * n];
            for (int i = 0; i < h.Length; i++)
            {
                float dn = bn[i] - h[i];
                float dw = bw[i] - h[i];
                float below = Mathf.Max(dn * 2f, dw);
                float above = Mathf.Max(-dn * 2f, -dw);
                cavity[i] = Mathf.Clamp01(below * gain);
                convex[i] = Mathf.Clamp01(above * gain);
            }
            return new SurfaceShade(cavity, convex);
        }

        /// <summary>Occlusion in 0..1 (1 open): cavities darken by strength.</summary>
        public static float Occlusion(float cavity, float strength)
        {
            return Mathf.Clamp01(1f - cavity * strength);
        }

        /// <summary>
        /// Packs the Standard and URP Lit mask texels: R metallic, G occlusion, B unused, A smoothness. Any array may be
        /// null, in which case that channel is the given constant.
        /// </summary>
        public static Color32[] PackMask(int count, float[] metallic, float metallicConst, float[] occlusion, float[] smoothness, float smoothnessConst)
        {
            Color32[] dst = new Color32[count];
            for (int i = 0; i < count; i++)
            {
                float m = metallic != null ? metallic[i] : metallicConst;
                float o = occlusion != null ? occlusion[i] : 1f;
                float s = smoothness != null ? smoothness[i] : smoothnessConst;
                dst[i] = new Color32(ToByte(m), ToByte(o), 0, ToByte(s));
            }
            return dst;
        }

        /// <summary>
        /// Metallic-flake normal map for car paint: tiny random facets (cells texels wide), each tilted by up to tilt
        /// (in slope units), with a flat base between them. Tiled many times over a panel it reads as flake sparkle.
        /// </summary>
        public static Color32[] FlakeNormal(int n, int cell, float tilt, float density, int seed)
        {
            Color32[] dst = new Color32[n * n];
            int cells = Mathf.Max(1, n / Mathf.Max(1, cell));
            for (int y = 0; y < n; y++)
            {
                int cy = (y * cells / n) % cells;
                for (int x = 0; x < n; x++)
                {
                    int cx = (x * cells / n) % cells;
                    float on = HeightField.Hash01(cx, cy, seed);
                    float nx = 0f, ny = 0f;
                    if (on < density)
                    {
                        nx = (HeightField.Hash01(cx, cy, seed + 5) - 0.5f) * 2f * tilt;
                        ny = (HeightField.Hash01(cx, cy, seed + 9) - 0.5f) * 2f * tilt;
                    }
                    float inv = 1f / Mathf.Sqrt(nx * nx + ny * ny + 1f);
                    dst[y * n + x] = new Color32((byte)((nx * inv * 0.5f + 0.5f) * 255f), (byte)((ny * inv * 0.5f + 0.5f) * 255f), (byte)((inv * 0.5f + 0.5f) * 255f), 255);
                }
            }
            return dst;
        }

        /// <summary>Decodes a normal texel back to a unit vector (for tests and checks).</summary>
        public static Vector3 Decode(Color32 c)
        {
            return new Vector3(c.r / 255f * 2f - 1f, c.g / 255f * 2f - 1f, c.b / 255f * 2f - 1f);
        }

        public static byte ToByte(float v)
        {
            return (byte)(Mathf.Clamp01(v) * 255f + 0.5f);
        }
    }
}
