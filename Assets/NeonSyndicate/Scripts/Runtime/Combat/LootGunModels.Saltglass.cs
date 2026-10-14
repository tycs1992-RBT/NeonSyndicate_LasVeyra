using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Saltglass: grown pale ceramic. A smooth shell with a ridge along the spine, bone ribs wrapped over it unevenly,
    /// two fins of different sizes on opposite sides, and a split "tuning fork" emitter instead of a barrel, its tines
    /// sparking with the element. The ammo is a seed pod set at a forward slant under the shell.
    /// </summary>
    public static partial class LootGunModels
    {
        private static readonly float[] SaltT = { 0f, 0.12f, 0.4f, 0.75f, 1f };
        private static readonly float[] SaltRx = { 0.28f, 0.5f, 0.56f, 0.46f, 0.32f };
        private static readonly float[] SaltRy = { 0.3f, 0.52f, 0.58f, 0.5f, 0.38f };

        private static void Saltglass(Frame f)
        {
            MeshBuilder b = f.B;
            float z0 = f.Z0, len = f.Len, h = f.H, w = f.W, cy = f.Cy;
            float k = f.Body == 1 ? 0.75f : 1f;
            float rise = f.Body == 3 ? h * 0.22f : h * 0.14f;
            int n = SaltT.Length;
            Vector3[] c = new Vector3[n];
            Vector2[] r = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float t = SaltT[i];
                c[i] = new Vector3(0f, cy - h * 0.08f + rise * t, z0 + len * t);
                r[i] = new Vector2(w * SaltRx[i] * k, h * SaltRy[i] * k);
            }
            Loft(b, 0, c, r, 5, 90f, true, true, Vector3.up);
            f.Top = cy + h * 0.5f * k + rise * 0.5f;

            // Bone ribs, swept back and longer on the left than on the right.
            int ribs = f.Rocket ? 4 : (f.Small ? 2 : 3);
            if (f.Body == 1) ribs++;
            float tube = Mathf.Max(0.003f, h * 0.07f);
            for (int i = 0; i < ribs; i++)
            {
                float t = 0.22f + 0.56f * (ribs == 1 ? 0.5f : i / (float)(ribs - 1));
                float rx = w * 0.56f * k + tube, ry = h * 0.58f * k + tube;
                float y = cy - h * 0.08f + rise * t;
                float z = z0 + len * t;
                float[] ang = { -20f, 95f, 215f };
                Vector3[] rc = new Vector3[3];
                Vector2[] rr = new Vector2[3];
                for (int j = 0; j < 3; j++)
                {
                    float a = ang[j] * Mathf.Deg2Rad;
                    float sweep = j != 1 ? -len * 0.04f : 0f;
                    float bulge = j == 1 ? 1.12f : 1f;
                    rc[j] = new Vector3(Mathf.Cos(a) * rx * bulge, y + Mathf.Sin(a) * ry * bulge, z + sweep);
                    rr[j] = new Vector2(tube, tube * (j != 1 ? 0.6f : 1f));
                }
                Loft(b, 2, rc, rr, 3, 90f, false, false, Vector3.forward);
            }

            // Asymmetric fins: a tall swept one on the left, a small low one on the right.
            float zc = f.Zc;
            Fin(b, 0, new Vector3(-w * 0.35f * k, f.Top - h * 0.2f, zc - len * 0.18f), new Vector3(-w * 0.3f * k, f.Top - h * 0.15f, zc + len * 0.18f),
                new Vector3(-w * 0.85f, f.Top + h * 0.45f, zc - len * 0.32f), 0.004f);
            Fin(b, 0, new Vector3(w * 0.45f * k, cy - h * 0.2f, z0 + len * 0.28f), new Vector3(w * 0.45f * k, cy - h * 0.2f, z0 + len * 0.5f),
                new Vector3(w * 0.85f, cy - h * 0.55f, z0 + len * 0.18f), 0.003f);
            if (f.Body == 3)
                Fin(b, 2, new Vector3(0f, f.Top - 0.003f, z0 + len * 0.2f), new Vector3(0f, f.Top - 0.003f, z0 + len * 0.7f), new Vector3(0f, f.Top + h * 0.35f, z0 + len * 0.12f), 0.003f);
            if (f.Body == 2)
            {
                for (int s = -1; s <= 1; s += 2)
                    LoftZ(b, 2, s * w * 0.46f, cy - h * 0.05f, new[] { z0 + len * 0.18f, z0 + len * 0.35f, z0 + len * 0.65f, z0 + len * 0.82f },
                        new[] { 0f, w * 0.22f, w * 0.2f, 0f }, new[] { 0f, h * 0.36f, h * 0.3f, 0f }, 5, 90f, false);
            }

            // Element: a glowing filament laid along the spine ridge.
            if (f.HasElement)
                Shard(b, 4, new Vector3(0f, cy + h * 0.5f * k - h * 0.08f + rise * 0.2f, z0 + len * 0.2f), new Vector3(0f, cy + h * 0.5f * k - h * 0.08f + rise * 0.8f, z0 + len * 0.8f), h * 0.12f, 3, 0.5f, 90f);

            SaltglassFork(f);
            if (f.HasStock) SaltglassStock(f);
            SaltglassPod(f);

            float veinK = k * 0.9f;
            for (int i = 0; i < 4; i++)
            {
                float t = 0.1f + 0.27f * i;
                float rxT = Mathf.Lerp(0.5f, 0.32f, Mathf.InverseLerp(0.12f, 1f, t));
                if (t < 0.4f) rxT = Mathf.Lerp(0.5f, 0.56f, Mathf.InverseLerp(0.12f, 0.4f, t));
                f.AddVein(new Vector3(w * rxT * veinK, cy - h * 0.08f + rise * t, z0 + len * t), Vector3.right);
            }
            f.VeinGap = h * 0.16f * k;
        }

        /// <summary>The tuning fork: a short stem that splits into two tines, with a spark of the element between them.</summary>
        private static void SaltglassFork(Frame f)
        {
            MeshBuilder b = f.B;
            float y = f.BarrelY;
            float zA = f.Z1 - 0.006f, zEnd = f.Z1 + f.BLen;
            float zSplit = f.Z1 + f.BLen * 0.3f;
            float stemR = Mathf.Max(f.BR * 1.1f, 0.006f);
            float spread = Mathf.Max(f.BR * 1.5f, 0.01f) * (f.Barrel == 3 ? 1.2f : 1f);
            LoftZ(b, 1, 0f, y, new[] { zA, zSplit }, new[] { stemR * 1.4f, stemR }, new[] { stemR * 1.4f, stemR }, 4, 45f, false);
            float pr = stemR * (f.Barrel == 3 ? 0.85f : 0.7f);
            float run = zEnd - zSplit;
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3[] c =
                {
                    new Vector3(0f, y, zSplit - 0.002f), new Vector3(s * spread * 0.8f, y, zSplit + run * 0.25f),
                    new Vector3(s * spread, y, zEnd - run * 0.12f), new Vector3(s * spread * 0.85f, y, zEnd)
                };
                Vector2[] r = { new Vector2(pr * 0.8f, pr * 1.3f), new Vector2(pr * 0.7f, pr * 1.3f), new Vector2(pr * 0.6f, pr * 1.1f), Vector2.zero };
                Loft(b, 1, c, r, 4, 45f, false, false, Vector3.up);
                if (f.Barrel == 2)
                    for (int j = 0; j < 2; j++)
                    {
                        Vector3 p = Vector3.Lerp(c[1], c[2], 0.25f + 0.5f * j);
                        Spike(b, 1, p, p + new Vector3(s * pr * 3f, 0f, -run * 0.08f), pr * 0.6f, 3);
                    }
            }
            if (f.Barrel == 3)
                LoftZ(b, 2, 0f, y, new[] { zSplit - 0.02f, zSplit - 0.008f, zSplit + 0.006f }, new[] { stemR * 1.2f, stemR * 2f, stemR * 1.1f },
                    new[] { stemR * 1.2f, stemR * 2f, stemR * 1.1f }, 5, 90f, false);
            if (f.HasElement)
                Shard(b, 4, new Vector3(0f, y, zSplit + run * 0.15f), new Vector3(0f, y, zEnd - 0.004f), spread * 0.35f, 3, 0.5f, 90f);
        }

        /// <summary>Saltglass stocks: a grown tail, a tail with a keel, two bone struts, or a tail with a flared pad.</summary>
        private static void SaltglassStock(Frame f)
        {
            MeshBuilder b = f.B;
            float z0 = f.Z0, zE = z0 - f.SLen, h = f.H, w = f.W, cy = f.Cy;
            switch (f.StockVariant)
            {
                case 2:
                    {
                        Vector2 rr = new Vector2(Mathf.Max(0.004f, h * 0.09f), Mathf.Max(0.004f, h * 0.09f));
                        Loft(b, 0, new[] { new Vector3(0f, cy + h * 0.2f, z0 + 0.012f), new Vector3(0f, cy + h * 0.22f, (z0 + zE) * 0.5f), new Vector3(0f, cy + h * 0.05f, zE + 0.008f) },
                            new[] { rr * 1.3f, rr, rr * 1.2f }, 4, 45f, false, false, Vector3.up);
                        Loft(b, 0, new[] { new Vector3(0f, cy - h * 0.3f, z0 + 0.012f), new Vector3(0f, cy - h * 0.42f, (z0 + zE) * 0.5f), new Vector3(0f, cy - h * 0.45f, zE + 0.008f) },
                            new[] { rr * 1.3f, rr, rr * 1.2f }, 4, 45f, false, false, Vector3.up);
                        LoftZ(b, 2, 0f, cy - h * 0.18f, new[] { zE + 0.012f, zE }, new[] { w * 0.36f, w * 0.4f }, new[] { h * 0.6f, h * 0.7f }, 5, 90f, true);
                    }
                    break;
                default:
                    {
                        float fat = f.StockVariant == 1 ? 1.15f : 1f;
                        Vector3[] c = { new Vector3(0f, cy - h * 0.05f, z0 + 0.012f), new Vector3(0f, cy - h * 0.15f, (z0 + zE) * 0.5f), new Vector3(0f, cy - h * 0.22f, zE) };
                        Vector2[] r = { new Vector2(w * 0.42f, h * 0.45f) * fat, new Vector2(w * 0.3f, h * 0.36f) * fat, new Vector2(w * 0.38f, h * 0.62f) * fat };
                        Loft(b, 0, c, r, 5, 90f, false, true, Vector3.up);
                        if (f.StockVariant == 1)
                            Fin(b, 2, new Vector3(0f, cy - h * 0.45f, z0 - f.SLen * 0.15f), new Vector3(0f, cy - h * 0.6f, zE + 0.02f), new Vector3(0f, cy - h * 1.05f, zE + f.SLen * 0.25f), 0.004f);
                        if (f.StockVariant == 3)
                            LoftZ(b, 2, 0f, cy - h * 0.22f, new[] { zE + 0.004f, zE - 0.0245f }, new[] { w * 0.4f, w * 0.5f }, new[] { h * 0.65f, h * 0.85f }, 5, 90f, true);
                    }
                    break;
            }
        }

        /// <summary>The seed pod: set into the underside at a forward slant (twin pods on the drum variant).</summary>
        private static void SaltglassPod(Frame f)
        {
            if (f.Rocket || (f.Small && f.Mag == 0)) return;
            float s = f.AmmoScale;
            float[] lens = { 0.1f, 0.15f, 0.06f, 0.09f };
            float L = lens[Mathf.Clamp(f.Mag, 0, 3)] * s;
            float pr = Mathf.Max(0.009f, f.W * 0.3f) * s;
            int sub = f.A == LootArchetype.CrystalCaster ? f.Core(2) : 2;
            int pods = f.Mag == 3 ? 2 : 1;
            for (int p = 0; p < pods; p++)
            {
                float x = pods == 1 ? 0f : (p == 0 ? -pr * 0.95f : pr * 0.95f);
                float rr = pods == 1 ? pr : pr * 0.8f;
                Vector3 base0 = new Vector3(x, f.Cy - f.H * 0.35f, f.AmmoZ);
                Vector3 d1 = new Vector3(0f, -0.85f, 0.5f).normalized;
                Vector3 d2 = new Vector3(0f, -0.6f, 0.8f).normalized;
                Vector3[] c = { base0, base0 + d1 * (L * 0.35f), base0 + d1 * (L * 0.6f) + d2 * (L * 0.2f), base0 + d1 * (L * 0.6f) + d2 * (L * 0.42f) };
                Vector2[] r = { new Vector2(rr * 0.8f, rr * 0.8f), new Vector2(rr, rr), new Vector2(rr * 0.75f, rr * 0.75f), Vector2.zero };
                Loft(f.B, sub, c, r, 4, 45f, false, false, Vector3.right);
            }
        }
    }
}
