using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Cinderline: black chitin in segments like an insect's abdomen, red glowing seams between the plates, exhaust gills
    /// flaring from the rear flanks, and three thin spines instead of a barrel. The ammo is a fuel capsule half sunk into
    /// the left flank.
    /// </summary>
    public static partial class LootGunModels
    {
        private static void Cinderline(Frame f)
        {
            MeshBuilder b = f.B;
            float z0 = f.Z0, len = f.Len, h = f.H, w = f.W, cy = f.Cy;
            int segs = f.Rocket ? 4 : (f.Small ? 2 : (len > 0.3f ? 4 : 3));
            float k = f.Body == 1 ? 0.82f : 1f;
            float gap = len * 0.035f;
            float segLen = (len - gap * (segs - 1)) / segs;
            for (int i = 0; i < segs; i++)
            {
                float zs = z0 + i * (segLen + gap);
                float sc = (0.85f + 0.15f * Mathf.Sin(Mathf.PI * (i + 0.5f) / segs)) * k;
                float rx = w * 0.5f * sc, ry = h * 0.5f * sc;
                // Tuned bodies rake each plate: the rear ring sits a little higher.
                float rake = f.Body == 3 ? h * 0.08f : 0f;
                Vector3[] c = { new Vector3(0f, cy + rake, zs), new Vector3(0f, cy + rake * 0.4f, zs + segLen * 0.55f), new Vector3(0f, cy, zs + segLen) };
                Vector2[] r = { new Vector2(rx * 0.85f, ry * 0.85f), new Vector2(rx, ry), new Vector2(rx * 0.9f, ry * 0.92f) };
                Loft(b, 0, c, r, 5, 90f, i == 0, i == segs - 1, Vector3.up);
                if (i > 0)
                    LoftZ(b, 2, 0f, cy, new[] { zs - gap - 0.002f, zs + 0.002f }, new[] { rx * 0.8f, rx * 0.8f }, new[] { ry * 0.8f, ry * 0.8f }, 5, 90f, false);
                if (f.Body == 2)
                    Fin(b, 0, new Vector3(0f, cy + ry * 0.9f, zs + segLen * 0.1f), new Vector3(0f, cy + ry * 0.9f, zs + segLen * 0.9f), new Vector3(0f, cy + ry * 1.35f, zs + segLen * 0.2f), 0.004f);
            }
            f.Top = cy + h * 0.5f * k;

            // Exhaust gills on the rear flanks, flaring out toward the back.
            int gills = f.Rocket ? 3 : 2;
            for (int s = -1; s <= 1; s += 2)
                for (int g = 0; g < gills; g++)
                {
                    float z = z0 + len * (0.12f + 0.12f * g);
                    Box(b, 0, new Vector3(s * w * 0.5f * k, cy - h * 0.02f, z), new Vector3(0.0025f, h * 0.55f * k, Mathf.Max(0.012f, len * 0.08f)), -8f, 0f, s * 28f, 7);
                    Box(b, 2, new Vector3(s * w * 0.44f * k, cy - h * 0.02f, z + 0.002f), new Vector3(0.002f, h * 0.4f * k, Mathf.Max(0.008f, len * 0.05f)), -8f, 0f, 0f, s > 0 ? 1 : 2);
                }

            if (f.HasElement)
                Shard(b, 4, new Vector3(0f, f.Top - 0.002f, f.Zc - len * 0.22f), new Vector3(0f, f.Top - 0.002f, f.Zc + len * 0.22f), h * 0.1f, 3, 0.5f, 90f);

            CinderlineSpines(f);
            if (f.HasStock) CinderlineStock(f);
            CinderlineCapsule(f);

            for (int i = 0; i < segs; i++)
            {
                float zs = z0 + i * (segLen + gap);
                float sc = (0.85f + 0.15f * Mathf.Sin(Mathf.PI * (i + 0.5f) / segs)) * k;
                f.AddVein(new Vector3(w * 0.5f * sc * 0.82f, cy + h * 0.18f, zs + segLen * 0.08f), new Vector3(1f, 0.4f, 0f).normalized);
                f.AddVein(new Vector3(w * 0.5f * sc * 0.88f, cy + h * 0.2f, zs + segLen * 0.55f), new Vector3(1f, 0.4f, 0f).normalized);
            }
            f.VeinGap = h * 0.12f;
        }

        /// <summary>The three spines: thin, parallel, converging slightly on the muzzle, rooted in a short collar.</summary>
        private static void CinderlineSpines(Frame f)
        {
            MeshBuilder b = f.B;
            float y = f.BarrelY;
            float zEnd = f.Z1 + f.BLen;
            float s = Mathf.Max(f.BR * 1.2f, 0.008f);
            if (f.Rocket) s = f.H * 0.28f;
            float collar = Mathf.Max(s * 1.8f, f.W * 0.3f);
            LoftZ(b, 1, 0f, y, new[] { f.Z1 - 0.006f, f.Z1 + 0.014f }, new[] { collar, collar * 0.75f }, new[] { collar, collar * 0.75f }, 5, 90f, false);
            float sr = Mathf.Max(f.BR * 0.35f, 0.0028f) * (f.Barrel == 3 ? 1.5f : 1f);
            if (f.Rocket) sr = 0.008f;
            for (int k = 0; k < 3; k++)
            {
                float a = (90f + k * 120f) * Mathf.Deg2Rad;
                Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                Vector3 from = new Vector3(0f, y, f.Z1 + 0.01f) + d * s;
                Vector3 tip = new Vector3(0f, y, zEnd) + d * (s * 0.55f);
                Spike(b, 1, from, tip, sr, 3);
                if (f.Barrel == 2)
                    for (int j = 0; j < 2; j++)
                    {
                        Vector3 p = Vector3.Lerp(from, tip, 0.3f + 0.3f * j);
                        Spike(b, 2, p, p + d * (sr * 3f) - new Vector3(0f, 0f, sr * 3f), sr * 0.6f, 3);
                    }
            }
            if (f.Barrel == 3) Halo(b, 2, new Vector3(0f, y, f.Z1 + f.BLen * 0.5f), QMath.Identity, s * 0.85f, Mathf.Max(0.0025f, sr * 0.6f), 5);
        }

        /// <summary>Cinderline stocks: a tail of two or three plates, a single spine with a cross pad, or a heavy vented tail.</summary>
        private static void CinderlineStock(Frame f)
        {
            MeshBuilder b = f.B;
            float z0 = f.Z0, zE = z0 - f.SLen, h = f.H, w = f.W, cy = f.Cy;
            if (f.StockVariant == 2)
            {
                float rr = Mathf.Max(0.004f, h * 0.08f);
                // A single curved spine ending in a chitin shield with a glowing slit.
                Loft(b, 0, new[] { new Vector3(0f, cy - h * 0.05f, z0 + 0.01f), new Vector3(0f, cy + h * 0.12f, (z0 + zE) * 0.5f), new Vector3(0f, cy - h * 0.1f, zE + 0.01f) },
                    new[] { new Vector2(rr * 1.4f, rr * 1.4f), new Vector2(rr, rr), new Vector2(rr * 1.2f, rr * 1.2f) }, 4, 45f, false, false, Vector3.up);
                LoftZ(b, 0, 0f, cy - h * 0.15f, new[] { zE + 0.014f, zE }, new[] { w * 0.22f, w * 0.3f }, new[] { h * 0.45f, h * 0.6f }, 5, 90f, true);
                Shard(b, 2, new Vector3(0f, cy - h * 0.5f, zE + 0.015f), new Vector3(0f, cy + h * 0.2f, zE + 0.015f), 0.003f, 3, 0.5f, 90f);
                return;
            }
            int n = f.StockVariant == 3 ? 3 : 2;
            float fat = f.StockVariant == 1 ? 1.12f : 1f;
            float zEnd = f.StockVariant == 3 ? zE - 0.0245f : zE;
            float gap = 0.006f;
            float seg = (z0 - zEnd - gap * n) / n;
            for (int i = 0; i < n; i++)
            {
                float za = z0 - gap * (i + 1) - seg * i;
                float sc = (1f - 0.1f * i) * fat;
                float yA = cy - h * 0.08f * i, yB = cy - h * 0.08f * (i + 1);
                Loft(b, 0, new[] { new Vector3(0f, yA, za), new Vector3(0f, (yA + yB) * 0.5f, za - seg * 0.5f), new Vector3(0f, yB, za - seg) },
                    new[] { new Vector2(w * 0.4f, h * 0.42f) * sc, new Vector2(w * 0.44f, h * 0.5f) * sc, new Vector2(w * 0.4f, h * 0.48f) * sc }, 5, 90f, false, i == n - 1, Vector3.up);
                LoftZ(b, 2, 0f, yA, new[] { za + gap + 0.002f, za - 0.002f }, new[] { w * 0.33f * sc, w * 0.33f * sc }, new[] { h * 0.35f * sc, h * 0.35f * sc }, 5, 90f, false);
            }
            if (f.StockVariant == 3)
                for (int s = -1; s <= 1; s += 2)
                    Box(b, 0, new Vector3(s * w * 0.42f, cy - h * 0.1f, z0 - seg * 0.8f), new Vector3(0.0025f, h * 0.5f, 0.02f), -8f, 0f, s * 28f, 7);
        }

        /// <summary>The fuel capsule: half sunk into the left flank, glowing at its seam (a ring tank on the drum variant).</summary>
        private static void CinderlineCapsule(Frame f)
        {
            if (f.Rocket || (f.Small && f.Mag == 0)) return;
            MeshBuilder b = f.B;
            float s = f.AmmoScale;
            float x = -f.W * 0.45f, y = f.Cy - f.H * 0.15f, z = f.AmmoZ;
            if (f.Mag == 3)
            {
                float R = f.H * 0.36f * s;
                Halo(b, 0, new Vector3(x - 0.004f, y, z), QMath.AY(90f), R, f.H * 0.13f * s, 6);
                Shard(b, 2, new Vector3(x - 0.012f, y, z), new Vector3(x + 0.004f, y, z), R * 0.6f, 6, 0.5f, 0f);
                return;
            }
            float[] lens = { 0.09f, 0.15f, 0.05f };
            float L = lens[Mathf.Clamp(f.Mag, 0, 2)] * s;
            float r = Mathf.Max(0.008f, f.H * 0.25f) * s;
            LoftZ(b, 0, x, y, new[] { z - L * 0.5f, z - L * 0.3f, z + L * 0.3f, z + L * 0.5f }, new[] { 0f, r, r, 0f }, new[] { 0f, r, r, 0f }, 5, 90f, false);
            LoftZ(b, 2, x, y, new[] { z - 0.004f, z + 0.004f }, new[] { r * 1.08f, r * 1.08f }, new[] { r * 1.08f, r * 1.08f }, 5, 90f, false);
        }
    }
}
