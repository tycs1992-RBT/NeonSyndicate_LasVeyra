using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Brassjaw: a living brass carapace. A dark soft underbody covered by overlapping brass plates split down the
    /// spine like beetle wings, rear edges lifted, and a segmented throat that ends in a jaw of hooked mandibles that
    /// open around the muzzle. The ammo is a gizzard sac tucked under the body, slanting back.
    /// </summary>
    public static partial class LootGunModels
    {
        private static void Brassjaw(Frame f)
        {
            MeshBuilder b = f.B;
            float z0 = f.Z0, len = f.Len, h = f.H, w = f.W, cy = f.Cy;
            float k = f.Body == 1 ? 0.85f : 1f;
            LoftZ(b, 1, 0f, cy, new[] { z0, z0 + len * 0.3f, f.Z1 }, new[] { w * 0.4f * k, w * 0.52f * k, w * 0.32f * k },
                new[] { h * 0.4f * k, h * 0.52f * k, h * 0.34f * k }, 4, 90f, true);

            // Wing plates: front plates sit over the ones behind, split at the spine, rear edges lifted.
            int plates = Mathf.Clamp(Mathf.RoundToInt(len / 0.085f), 2, 4);
            if (f.Body == 1) plates = Mathf.Max(2, plates - 1);
            float pitch = len / plates;
            float plateLen = pitch * (f.Body == 1 ? 0.85f : 1.3f);
            float lift = h * (f.Body == 3 ? 0.2f : 0.1f);
            for (int i = 0; i < plates; i++)
            {
                float zFront = f.Z1 - len * 0.04f - i * pitch;
                float zBack = Mathf.Max(z0 - len * 0.02f, zFront - plateLen);
                // Plates widen toward the rear like a beetle's abdomen; each rear edge flares over the next plate.
                float grow = 0.9f + 0.1f * i;
                float rx = w * 0.55f * grow * k, ry = h * 0.58f * grow * k;
                // Only the rearmost plate shows its underside (the camera sits behind the gun).
                bool under = i == plates - 1;
                Plate(b, 0, 0f, cy, zBack, zFront, rx, ry, 8f, 87f, 2, lift, 0.16f, under);
                Plate(b, 0, 0f, cy, zBack, zFront, rx, ry, 93f, 172f, 2, lift, 0.16f, under);
                if (f.Body == 3)
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Vector3 p = new Vector3(s * rx * 0.55f, cy + ry * 0.85f + lift, zBack + 0.004f);
                        Spike(b, 0, p, p + new Vector3(s * 0.004f, h * 0.18f, -pitch * 0.45f), Mathf.Max(0.003f, h * 0.06f), 3);
                    }
            }
            f.Top = cy + h * 0.58f * k + lift * 0.5f;
            if (f.Body == 2)
            {
                // Lower elytra: one long plate down each flank.
                Plate(b, 0, 0f, cy, z0 + len * 0.05f, f.Z1 - len * 0.1f, w * 0.6f, h * 0.6f, -48f, -8f, 1, 0f, 0.05f, false);
                Plate(b, 0, 0f, cy, z0 + len * 0.05f, f.Z1 - len * 0.1f, w * 0.6f, h * 0.6f, 188f, 228f, 1, 0f, 0.05f, false);
            }

            BrassjawJaw(f);
            if (f.HasStock) BrassjawStock(f);
            BrassjawSac(f);

            for (int i = 0; i < 4; i++)
            {
                float t = 0.08f + 0.29f * i;
                f.AddVein(new Vector3(w * 0.47f * k, cy - h * 0.12f, z0 + len * t), new Vector3(1f, -0.25f, 0f).normalized);
            }
            f.VeinGap = h * 0.11f;
        }

        /// <summary>The segmented throat and the mandibles that close around the muzzle.</summary>
        private static void BrassjawJaw(Frame f)
        {
            MeshBuilder b = f.B;
            float y = f.BarrelY;
            float zEnd = f.Z1 + f.BLen;
            float jawLen = Mathf.Clamp(f.BLen * 0.45f, 0.03f, 0.1f);
            if (f.Rocket) jawLen = f.BLen;
            float zJaw = zEnd - jawLen;
            float rT = Mathf.Max(f.BR * 1.3f, 0.009f) * (f.Barrel == 3 ? 1.3f : 1f);
            if (f.Rocket) rT = f.H * 0.32f;
            int seg = zJaw - f.Z1 < 0.03f ? 0 : Mathf.Max(1, Mathf.RoundToInt((zJaw - f.Z1) / 0.1f));
            float[] z = new float[seg + 2];
            float[] r = new float[seg + 2];
            z[0] = Mathf.Min(f.Z1 - 0.008f, zJaw - 0.012f);
            r[0] = rT * 1.1f;
            for (int s = 1; s <= seg; s++)
            {
                z[s] = f.Z1 + (zJaw - f.Z1) * s / (seg + 0.5f);
                r[s] = s % 2 == 0 ? rT : rT * 0.84f;
            }
            z[seg + 1] = zJaw;
            r[seg + 1] = rT * 1.35f;
            LoftZ(b, 1, 0f, y, z, r, r, 5, 90f, false);
            // The throat's glowing tongue (dark when there is no element).
            Spike(b, f.Core(1), new Vector3(0f, y, zJaw - 0.004f), new Vector3(0f, y, zJaw + jawLen * 0.35f), rT * 0.9f, 5, false, 90f);

            float jawR = Mathf.Max(rT * 0.5f, 0.005f);
            int count = f.Barrel == 3 ? 3 : 2;
            for (int m = 0; m < count; m++)
            {
                // Left and right mandibles; the heavy barrel adds a shorter lower one.
                Vector3 side = m == 2 ? Vector3.down : (m == 0 ? Vector3.right : Vector3.left);
                float reach = m == 2 ? 0.75f : 1f;
                float zTip = zJaw + jawLen * reach;
                Vector3 o = new Vector3(0f, y, 0f);
                Vector3[] c =
                {
                    o + side * (rT * 1.1f) + new Vector3(0f, 0f, zJaw - 0.012f),
                    o + side * (rT * 1.25f + jawLen * 0.3f) + new Vector3(0f, 0f, zJaw + jawLen * 0.3f * reach),
                    o + side * (rT * 0.9f + jawLen * 0.25f) + new Vector3(0f, 0f, zJaw + jawLen * 0.72f * reach),
                    o + side * (rT * 0.35f) + new Vector3(0f, 0f, zTip)
                };
                Vector2[] rr = { new Vector2(jawR, jawR * 1.5f), new Vector2(jawR * 0.85f, jawR * 1.3f), new Vector2(jawR * 0.6f, jawR), Vector2.zero };
                Loft(b, 0, c, rr, 3, 90f, false, false, m == 2 ? Vector3.right : Vector3.up);
                if (f.Barrel == 2 && m < 2)
                    for (int t = 0; t < 2; t++)
                    {
                        Vector3 p = Vector3.Lerp(c[1], c[2], 0.3f + 0.45f * t);
                        Spike(b, 1, p, p - side * (jawR * 2.4f) + new Vector3(0f, 0f, jawLen * 0.05f), jawR * 0.45f, 3);
                    }
            }
        }

        /// <summary>Brassjaw stocks: a plated tail (plain, solid or heavy) or two curved brass horns to a pad.</summary>
        private static void BrassjawStock(Frame f)
        {
            MeshBuilder b = f.B;
            float z0 = f.Z0, zE = z0 - f.SLen, h = f.H, w = f.W, cy = f.Cy;
            if (f.StockVariant == 2)
            {
                float rr = Mathf.Max(0.004f, h * 0.08f);
                for (int s = -1; s <= 1; s += 2)
                    Loft(b, 0, new[] { new Vector3(s * w * 0.25f, cy + h * 0.1f, z0 + 0.012f), new Vector3(s * w * 0.42f, cy + h * 0.05f, (z0 + zE) * 0.5f), new Vector3(s * w * 0.2f, cy - h * 0.2f, zE + 0.01f) },
                        new[] { new Vector2(rr * 1.3f, rr * 1.3f), new Vector2(rr, rr), new Vector2(rr * 0.9f, rr * 0.9f) }, 4, 45f, false, false, Vector3.up);
                LoftZ(b, 1, 0f, cy - h * 0.2f, new[] { zE + 0.014f, zE }, new[] { w * 0.3f, w * 0.34f }, new[] { h * 0.45f, h * 0.55f }, 5, 90f, true);
                return;
            }
            // A tapering, drooping tail of shrinking wing plates over a dark core, like a beetle's abdomen.
            float fat = f.StockVariant == 1 ? 1.15f : (f.StockVariant == 3 ? 1.1f : 1f);
            float zEnd = f.StockVariant == 3 ? zE - 0.0245f : zE;
            float drop = h * 0.3f;
            Loft(b, 1, new[] { new Vector3(0f, cy - h * 0.05f, z0 + 0.01f), new Vector3(0f, cy - h * 0.05f - drop * 0.4f, (z0 + zEnd) * 0.5f), new Vector3(0f, cy - h * 0.05f - drop, zEnd) },
                new[] { new Vector2(w * 0.34f, h * 0.4f) * fat, new Vector2(w * 0.26f, h * 0.32f) * fat, new Vector2(w * 0.16f, h * 0.3f) * fat }, 4, 90f, false, true, Vector3.up);
            int n = f.StockVariant == 3 ? 3 : 2;
            float step = (z0 - zEnd) / n;
            for (int i = 0; i < n; i++)
            {
                float zF = z0 + 0.012f - i * step;
                float sc = (1f - 0.22f * i) * fat;
                float y = cy - h * 0.05f - drop * (i + 0.5f) / n;
                Plate(b, 0, 0f, y, zF - step * 1.05f, zF, w * 0.4f * sc, h * 0.5f * sc, 10f, 170f, 2, h * 0.08f, 0.12f, false);
            }
        }

        /// <summary>The gizzard sac under the body, slanting back (a double sac on the drum variant).</summary>
        private static void BrassjawSac(Frame f)
        {
            if (f.Rocket || (f.Small && f.Mag == 0)) return;
            float s = f.AmmoScale;
            float[] lens = { 0.08f, 0.13f, 0.05f, 0.07f };
            float L = lens[Mathf.Clamp(f.Mag, 0, 3)] * s;
            float pr = Mathf.Max(0.01f, f.W * 0.28f) * s;
            int sacs = f.Mag == 3 ? 2 : 1;
            for (int i = 0; i < sacs; i++)
            {
                Vector3 base0 = new Vector3(0f, f.Cy - f.H * 0.3f, f.AmmoZ - i * L * 0.7f);
                Vector3 d = new Vector3(0f, -0.8f, -0.6f).normalized;
                Vector3[] c = { base0, base0 + d * (L * 0.3f), base0 + d * (L * 0.75f), base0 + d * L };
                Vector2[] r = { new Vector2(pr * 0.7f, pr * 0.7f), new Vector2(pr * 1.1f, pr * 1.05f), new Vector2(pr, pr * 0.95f), new Vector2(pr * 0.4f, pr * 0.4f) };
                Loft(f.B, 0, c, r, 4, 45f, false, true, Vector3.right);
            }
        }
    }
}
