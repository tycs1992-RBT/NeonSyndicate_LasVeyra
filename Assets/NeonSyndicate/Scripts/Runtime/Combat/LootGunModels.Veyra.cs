using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Veyra: no solid frame at all. Crystal shards float around a glowing prism core, held apart by energy (small
    /// gaps everywhere, glowing nodes in the gaps), and instead of a barrel a crystal lance tapers to a point, ringed
    /// by orbiting splinters. The ammo is a floating cluster of small crystals under the core.
    /// </summary>
    public static partial class LootGunModels
    {
        private static void Veyra(Frame f)
        {
            MeshBuilder b = f.B;
            float z0 = f.Z0, len = f.Len, h = f.H, w = f.W, cy = f.Cy;
            int segs = f.Small ? 2 : 3;
            int around = f.Body == 1 ? 2 : (f.Body == 2 ? 4 : 3);
            float gap = f.Rocket ? 0.015f : 0.006f;
            float segLen = (len - gap * (segs - 1)) / segs;
            float m = Mathf.Min(w, h);
            float coreR = m * 0.18f;
            float sr = m * 0.24f * (f.Body == 2 ? 1.1f : (f.Body == 1 ? 0.85f : 1f));
            float ox = w * 0.34f, oy = h * 0.36f;
            if (f.Body == 2) { ox *= 1.08f; oy *= 1.08f; }

            // The prism core runs the whole length, visible through every gap.
            LoftZ(b, f.Core(2), 0f, cy, new[] { z0 + 0.008f, f.Z1 - 0.004f }, new[] { coreR, coreR }, new[] { coreR, coreR }, 3, 90f, true);
            for (int i = 0; i < segs; i++)
            {
                float zs = z0 + i * (segLen + gap);
                float stagger = f.Body == 3 && i % 2 == 1 ? 180f / around : 0f;
                for (int j = 0; j < around; j++)
                {
                    float a = (90f + stagger + j * 360f / around) * Mathf.Deg2Rad;
                    Vector3 d = new Vector3(Mathf.Cos(a) * ox, Mathf.Sin(a) * oy, 0f);
                    // Tuned bodies sweep each shard back: the rear tip flares out, the front tucks in.
                    Vector3 from = new Vector3(0f, cy, zs) + d * (f.Body == 3 ? 1.25f : 1f);
                    Vector3 to = new Vector3(0f, cy, zs + segLen) + d * (f.Body == 3 ? 0.8f : 1f);
                    Shard(b, 0, from, to, sr, 4, 0.4f, 45f);
                }
                if (i > 0) Shard(b, 2, new Vector3(0f, cy, zs - gap - 0.004f), new Vector3(0f, cy, zs + 0.004f), coreR * 1.5f, 3, 0.5f, 90f);
            }
            f.Top = cy + oy + sr * 0.6f;

            VeyraLance(f);
            if (f.HasStock) VeyraStock(f);
            VeyraCluster(f);

            // Veins run along the outer faces of the lower right shards.
            float a2 = (90f + 2f * 360f / around) * Mathf.Deg2Rad;
            if (around == 2) a2 = 0f;
            Vector3 dv = new Vector3(Mathf.Abs(Mathf.Cos(a2)) * ox + sr * 0.6f, Mathf.Sin(a2) * oy * 0.5f, 0f);
            for (int i = 0; i < segs; i++)
            {
                float zs = z0 + i * (segLen + gap);
                f.AddVein(new Vector3(dv.x * 0.8f, cy + dv.y, zs + segLen * 0.12f), Vector3.right);
                f.AddVein(new Vector3(dv.x, cy + dv.y, zs + segLen * 0.4f), Vector3.right);
                f.AddVein(new Vector3(dv.x * 0.75f, cy + dv.y, zs + segLen * 0.88f), Vector3.right);
            }
            f.VeinGap = sr * 0.5f;
        }

        /// <summary>The crystal lance: thick near the body, a long taper to a point at the muzzle.</summary>
        private static void VeyraLance(Frame f)
        {
            MeshBuilder b = f.B;
            float y = f.BarrelY;
            float zEnd = f.Z1 + f.BLen;
            float zA = f.Z1 + 0.003f;
            float lr = Mathf.Max(f.BR * 1.4f, 0.008f) * (f.Barrel == 3 ? 1.35f : 1f);
            if (f.Rocket) lr = f.H * 0.3f;
            Shard(b, 0, new Vector3(0f, y, zA), new Vector3(0f, y, zEnd), lr, 4, 0.15f, 45f);
            float zo = Mathf.Min(0.06f, f.BLen * 0.4f);
            for (int k = 0; k < 3; k++)
            {
                float a = (30f + k * 120f) * Mathf.Deg2Rad;
                Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * (lr * 2.1f);
                Shard(b, 2, new Vector3(0f, y, zA + 0.004f) + d, new Vector3(0f, y, zA + 0.004f + zo) + d * 0.8f, lr * 0.35f, 3, 0.4f, 90f);
            }
            if (f.Barrel == 2)
                for (int s = -1; s <= 1; s += 2)
                    Fin(b, 0, new Vector3(s * lr * 0.6f, y, zA + f.BLen * 0.1f), new Vector3(s * lr * 0.3f, y, zA + f.BLen * 0.75f), new Vector3(s * lr * 2.6f, y, zA + f.BLen * 0.2f), 0.003f);
            if (f.Barrel == 3)
                for (int k = 0; k < 3; k++)
                {
                    float a = (90f + k * 120f) * Mathf.Deg2Rad;
                    Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                    Vector3 p = new Vector3(0f, y, zA + 0.006f) + d * (lr * 0.9f);
                    Shard(b, 0, p, p + d * (lr * 2.2f) - new Vector3(0f, 0f, lr * 2.4f), lr * 0.45f, 3, 0.35f, 90f);
                }
        }

        /// <summary>Veyra stocks: a trail of shrinking floating shards, a V of two thin shards, or a big butt crystal.</summary>
        private static void VeyraStock(Frame f)
        {
            MeshBuilder b = f.B;
            float z0 = f.Z0, zE = z0 - f.SLen, h = f.H, w = f.W, cy = f.Cy;
            float m = Mathf.Min(w, h);
            float gap = 0.008f;
            if (f.StockVariant == 2)
            {
                for (int s = -1; s <= 1; s += 2)
                    Shard(b, 0, new Vector3(0f, cy - h * 0.05f, z0 - gap), new Vector3(0f, cy + s * h * 0.45f, zE), m * 0.12f, 4, 0.3f, 45f);
                Shard(b, 2, new Vector3(0f, cy - h * 0.55f, zE + 0.004f), new Vector3(0f, cy + h * 0.55f, zE + 0.004f), m * 0.14f, 3, 0.5f, 90f);
                return;
            }
            int n = f.StockVariant == 1 ? 3 : 2;
            float zEnd = f.StockVariant == 3 ? zE + 0.03f : zE;
            float step = (z0 - gap - zEnd) / n;
            for (int i = 0; i < n; i++)
            {
                float za = z0 - gap - i * step;
                float sz = (f.StockVariant == 1 ? 0.3f : 0.24f) * (1f - 0.15f * i);
                Shard(b, 0, new Vector3(0f, cy - h * 0.06f * i, za), new Vector3(0f, cy - h * 0.06f * (i + 1), za - step + gap), m * sz * 1.4f, 4, 0.55f, 45f);
            }
            if (f.StockVariant == 3)
                Shard(b, f.Core(2), new Vector3(0f, cy - h * 0.2f, zEnd - 0.004f), new Vector3(0f, cy - h * 0.2f, zE - 0.0245f), m * 0.5f, 5, 0.35f, 90f, 1.6f);
        }

        /// <summary>The ammo cluster: small crystals floating under the core around a glowing node.</summary>
        private static void VeyraCluster(Frame f)
        {
            if (f.Rocket || (f.Small && f.Mag == 0)) return;
            MeshBuilder b = f.B;
            float s = f.AmmoScale;
            float m = Mathf.Min(f.W, f.H) * s;
            Vector3 node = new Vector3(0f, f.Cy - f.H * 0.5f - 0.012f - m * 0.3f, f.AmmoZ);
            int count = f.Mag == 1 ? 3 : (f.Mag == 2 ? 1 : (f.Mag == 3 ? 4 : 2));
            float L = (f.Mag == 1 ? 0.07f : 0.045f) * s;
            for (int i = 0; i < count; i++)
            {
                float a = (count == 1 ? 270f : 200f + i * 140f / Mathf.Max(1, count - 1)) * Mathf.Deg2Rad;
                if (f.Mag == 3) a = (45f + i * 90f) * Mathf.Deg2Rad;
                Vector3 d = f.Mag == 3 ? new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * (m * 0.35f) : new Vector3(Mathf.Cos(a) * 0.4f, Mathf.Sin(a), 0.5f).normalized * L;
                Vector3 from = f.Mag == 3 ? node + d - new Vector3(0f, 0f, L * 0.5f) : node + d * 0.15f;
                Vector3 to = f.Mag == 3 ? node + d + new Vector3(0f, 0f, L * 0.5f) : node + d;
                Shard(b, 0, from, to, m * 0.14f, 4, 0.4f, 45f);
            }
            Shard(b, f.Core(2), node - new Vector3(0f, 0f, 0.006f), node + new Vector3(0f, 0f, 0.006f), m * 0.12f, 4, 0.5f, 45f);
        }
    }
}
