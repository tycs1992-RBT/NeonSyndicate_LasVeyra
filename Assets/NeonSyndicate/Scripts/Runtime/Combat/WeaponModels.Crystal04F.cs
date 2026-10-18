using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Original forged crystal blades. Closed diamond sections have real thickness, a distal taper,
    /// dark structural spine, cut facets, wrapped hilt and a clear guard. All share the existing model cache.</summary>
    public static partial class WeaponModels
    {
        /// <summary>Pure authored geometry, exposed so source tests can verify thickness and taper without a GPU.</summary>
        public static void BuildBlade04F(MeshBuilder b, bool dagger)
        {
            float length = dagger ? 0.28f : 0.94f;
            float guard = dagger ? 0.075f : 0.165f;
            float width = dagger ? 0.022f : 0.032f;
            // Leather winding has twelve separate diagonal cuts around a robust octagonal grip.
            for (int i = 0; i < 12; i++)
            {
                float z = Mathf.Lerp(-0.095f, guard - 0.022f, i / 11f);
                Section04F(b, 0, new Vector3(0f, 0f, z), new Vector3(0f, 0f, z + 0.016f),
                    0.0156f, 0.0148f, 0.0135f, 0.013f, 8, i * 17f);
            }
            Section04F(b, 1, new Vector3(0f, 0f, -0.115f), new Vector3(0f, 0f, -0.092f),
                0.022f, 0.015f, 0.016f, 0.011f, 6, 30f);
            // Quillons sweep toward the blade. Their faceted profile remains readable without emissive material.
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 a = new Vector3(0f, 0f, guard), c = new Vector3(side * width * 2f, 0f, guard + 0.012f);
                Vector3 tip = new Vector3(side * width * 2.8f, 0f, guard + 0.062f);
                Section04F(b, 1, a, c, 0.016f, 0.013f, 0.012f, 0.01f, 6, 0f);
                Section04F(b, 1, c, tip, 0.012f, 0.01f, 0.003f, 0.003f, 6, 0f);
            }
            float[] zKeys = { guard + 0.012f, guard + 0.08f, length * 0.45f,
                length * 0.78f, length * 0.91f, length };
            float[] widths = { width * 0.7f, width, width * 0.86f, width * 0.66f, width * 0.44f, 0.0005f };
            for (int i = 0; i < zKeys.Length - 1; i++)
            {
                float bendA = dagger ? 0f : Mathf.Pow(zKeys[i] / length, 2f) * 0.018f;
                float bendB = dagger ? 0f : Mathf.Pow(zKeys[i + 1] / length, 2f) * 0.018f;
                Section04F(b, 1, new Vector3(0f, bendA, zKeys[i]), new Vector3(0f, bendB, zKeys[i + 1]),
                    widths[i], widths[i] * 0.24f, widths[i + 1], widths[i + 1] * 0.24f, 4, 45f);
                // Two narrow set-in crystal cutting facets, not a broad luminous rectangle.
                for (int side = -1; side <= 1; side += 2)
                    Section04F(b, 2, new Vector3(side * widths[i] * 0.7f, bendA, zKeys[i]),
                        new Vector3(side * widths[i + 1] * 0.7f, bendB, zKeys[i + 1]),
                        widths[i] * 0.16f, 0.003f, widths[i + 1] * 0.16f, 0.002f, 4, 45f);
            }
            // A small guard-set crystal identifies the power source without obscuring the silhouette.
            Section04F(b, 2, new Vector3(0f, -0.014f, guard - 0.012f), new Vector3(0f, -0.014f, guard + 0.034f),
                0.012f, 0.006f, 0.002f, 0.002f, 6, 0f);
        }

        /// <summary>Radial loft between two authored cross sections, with flat facet normals and closed end caps.</summary>
        private static void Section04F(MeshBuilder b, int sub, Vector3 a, Vector3 c,
            float ax, float ay, float cx, float cy, int sides, float phase)
        {
            Vector3 f = (c - a).normalized;
            Vector3 u = Vector3.Cross(Mathf.Abs(f.y) < 0.9f ? Vector3.up : Vector3.right, f).normalized;
            Vector3 v = Vector3.Cross(f, u).normalized;
            for (int i = 0; i < sides; i++)
            {
                float th0 = (phase + i * 360f / sides) * Mathf.Deg2Rad;
                float th1 = (phase + (i + 1) * 360f / sides) * Mathf.Deg2Rad;
                Vector3 p0 = a + u * (Mathf.Cos(th0) * ax) + v * (Mathf.Sin(th0) * ay);
                Vector3 p1 = a + u * (Mathf.Cos(th1) * ax) + v * (Mathf.Sin(th1) * ay);
                Vector3 p2 = c + u * (Mathf.Cos(th1) * cx) + v * (Mathf.Sin(th1) * cy);
                Vector3 p3 = c + u * (Mathf.Cos(th0) * cx) + v * (Mathf.Sin(th0) * cy);
                Vector3 n = Vector3.Cross(p1 - p0, p3 - p0).normalized;
                if (Vector3.Dot(n, (p0 + p1) * 0.5f - a) < 0f)
                    b.Quad(sub, p3, p2, p1, p0, -n, Vector2.zero, Vector2.up, Vector2.one, Vector2.right);
                else b.Quad(sub, p0, p1, p2, p3, n, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
                b.Triangle(sub, a, p1, p0, -f, Vector2.zero, Vector2.right, Vector2.one);
                b.Triangle(sub, c, p3, p2, f, Vector2.zero, Vector2.right, Vector2.one);
            }
        }
    }
}
