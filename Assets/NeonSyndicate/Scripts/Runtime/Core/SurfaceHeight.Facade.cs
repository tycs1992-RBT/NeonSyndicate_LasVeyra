using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The building facade relief: the same four-by-four bay geometry as the painted facade (Tex.Facade), with recessed
    /// windows, raised frames, glass-curtain mullions, brick courses or stucco grain, and a plinth line per floor.
    /// </summary>
    public static partial class SurfaceHeight
    {
        /// <summary>
        /// A building facade's relief for the normal map: 4 window bays by 4 floors, windows recessed with raised
        /// frames, a glass curtain's mullions, brick courses or stucco grain on the wall, and a plinth line per floor.
        /// The bay geometry (winW, winH) matches Tex.Facade so the relief sits exactly under the painted windows.
        /// </summary>
        public static SurfaceFields Facade(int n, float winW, float winH, bool glassCurtain, bool brick, int seed)
        {
            float[] tone = Fbm(n, 8, 3, seed);
            float[] grain = HeightField.Grain(n, seed + 1);
            float[] h = new float[n * n];
            float[] mask = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;
                float bv = HeightField.Frac(v * 4f);
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n;
                    float bu = HeightField.Frac(u * 4f);
                    int i = y * n + x;
                    float du = Mathf.Abs(bu - 0.5f) - winW * 0.5f;
                    float dv = Mathf.Abs(bv - 0.55f) - winH * 0.5f;
                    bool inWindow = du < 0f && dv < 0f;
                    bool edge = inWindow && (Mathf.Abs(du) < 0.025f || Mathf.Abs(dv) < 0.03f);
                    float wall = 0.7f;
                    if (brick)
                    {
                        float rowB = HeightField.Frac(v * 64f);
                        float colB = HeightField.Frac(u * 32f + (Mathf.FloorToInt(v * 64f) % 2) * 0.5f);
                        if (rowB < 0.12f || colB < 0.06f) wall = 0.55f;
                        else wall += (HeightField.Hash01(Mathf.FloorToInt(u * 32f), Mathf.FloorToInt(v * 64f), 0) - 0.5f) * 0.05f;
                    }
                    else wall += (tone[i] - 0.5f) * 0.06f + (grain[i] - 0.5f) * 0.04f;
                    float value;
                    if (edge) value = 0.8f;
                    else if (inWindow) value = glassCurtain ? 0.62f : 0.4f;
                    else value = wall;
                    if (glassCurtain && !inWindow && (bu < 0.03f || bu > 0.97f)) value = 0.78f;
                    if (bv < 0.04f) value -= 0.08f;
                    h[i] = value;
                    mask[i] = inWindow && !edge ? 1f : 0f;
                }
            }
            return Finish(n, h, tone, mask, null);
        }
    }
}
