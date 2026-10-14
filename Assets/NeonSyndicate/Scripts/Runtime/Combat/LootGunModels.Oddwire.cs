using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Oddwire: built from whatever was at hand. A core wrapped in copper coils that do not match (different sizes,
    /// off centre, tilted), a capacitor bulb on one side, loose wires running as tubes, tesla rings around a bare rod
    /// instead of a barrel, and an orb hovering where a sight would be. The ammo is a mismatched battery canister.
    /// </summary>
    public static partial class LootGunModels
    {
        private static readonly float[] OddCoilT = { 0.2f, 0.47f, 0.74f, 0.6f };
        private static readonly float[] OddCoilR = { 1.12f, 1.28f, 1.06f, 1.2f };
        private static readonly float[] OddCoilL = { 0.12f, 0.07f, 0.15f, 0.05f };
        private static readonly float[] OddCoilY = { 0.004f, -0.003f, 0.006f, 0.008f };
        private static readonly float[] OddCoilTilt = { 7f, -10f, 4f, 14f };

        private static void Oddwire(Frame f)
        {
            MeshBuilder b = f.B;
            float z0 = f.Z0, len = f.Len, h = f.H, w = f.W;
            float k = f.Body == 1 ? 0.72f : 1f;
            float cy = f.Body == 3 ? f.Cy - h * 0.08f : f.Cy;
            LoftZ(b, 0, 0f, cy, new[] { z0, z0 + len * 0.15f, f.Z1 - len * 0.1f, f.Z1 }, new[] { w * 0.36f * k, w * 0.48f * k, w * 0.48f * k, w * 0.4f * k },
                new[] { h * 0.36f * k, h * 0.48f * k, h * 0.48f * k, h * 0.4f * k }, 5, 90f, true);

            int coils = f.Small ? 2 : 3;
            if (f.Body == 2) coils++;
            for (int i = 0; i < coils; i++)
            {
                float cl = Mathf.Max(0.012f, OddCoilL[i] * len);
                float rr = OddCoilR[i] * (f.Body == 1 ? 1.15f : 1f);
                Vector3 centre = new Vector3(0f, cy + OddCoilY[i] * (h / 0.07f), z0 + len * OddCoilT[i]);
                Quaternion rot = QMath.Euler(90f + OddCoilTilt[i], 0f, 0f);
                b.FixedUV = FixedUv;
                b.Cylinder(1, centre - rot * Vector3.up * (cl * 0.5f), w * 0.48f * k * rr, w * 0.48f * k * rr, cl, 6, rot, false, UVMode.Fixed);
            }
            if (f.Body == 3)
            {
                // A tall coil standing on top, off centre.
                Quaternion up = QMath.Euler(0f, 0f, 8f);
                b.Cylinder(1, new Vector3(w * 0.12f, cy + h * 0.3f, z0 + len * 0.3f), w * 0.2f, w * 0.17f, h * 0.55f, 5, up, true, UVMode.Fixed);
            }

            // Capacitor bulb on the right (a second one on the left when reinforced), glowing with the element.
            Vector3 bulb = new Vector3(w * 0.55f * k + 0.004f, cy + h * 0.1f, z0 + len * 0.35f);
            Vector3 bulbR = new Vector3(Mathf.Max(0.008f, w * 0.26f), Mathf.Max(0.009f, h * 0.32f), Mathf.Max(0.012f, len * 0.11f));
            Egg(b, f.Core(2), bulb, bulbR, 5, QMath.Identity);
            if (f.Body == 2) Egg(b, 2, new Vector3(-bulb.x, cy - h * 0.05f, z0 + len * 0.6f), bulbR * 0.8f, 5, QMath.Identity);
            f.Top = cy + h * 0.48f * k;

            // Loose wires: bulb to the emitter root, and from the rear over the top to the orb.
            float wr = 0.0022f;
            Vector2 wv = new Vector2(wr, wr);
            Vector3 root = new Vector3(0f, f.BarrelY + Mathf.Max(f.BR, 0.006f) + 0.002f, f.Z1 + 0.004f);
            Loft(b, 2, new[] { bulb + new Vector3(0f, bulbR.y * 0.8f, 0f), Vector3.Lerp(bulb, root, 0.5f) + new Vector3(-0.002f, h * 0.45f, 0f), root },
                new[] { wv, wv, wv }, 3, 90f, false, false, Vector3.right);
            Loft(b, 2, new[] { new Vector3(-w * 0.25f, cy + h * 0.2f, z0 + 0.004f), new Vector3(-w * 0.35f, f.Top + h * 0.3f, (z0 + f.SightZ) * 0.5f), new Vector3(-0.004f, f.Top, f.SightZ - 0.006f) },
                new[] { wv, wv, wv }, 3, 90f, false, false, Vector3.right);

            OddwireTesla(f);
            if (f.HasStock) OddwireStock(f);
            OddwireCanister(f);

            for (int i = 0; i < 4; i++)
            {
                float t = 0.08f + 0.28f * i;
                f.AddVein(new Vector3(w * 0.44f * k, cy - h * 0.12f, z0 + len * t), new Vector3(1f, -0.25f, 0f).normalized);
            }
            f.VeinGap = h * 0.13f;
        }

        /// <summary>The bare rod with a ball tip, ringed by tesla rings that shrink toward the muzzle.</summary>
        private static void OddwireTesla(Frame f)
        {
            MeshBuilder b = f.B;
            float y = f.BarrelY;
            float zEnd = f.Z1 + f.BLen;
            float rodR = Mathf.Max(f.BR * 0.6f, 0.004f) * (f.Barrel == 3 ? 1.5f : 1f);
            float tipR = rodR * 1.8f;
            if (f.Rocket) { rodR = 0.012f; tipR = 0.022f; }
            LoftZ(b, f.Core(1), 0f, y, new[] { f.Z1 - 0.006f, zEnd - tipR }, new[] { rodR, rodR }, new[] { rodR, rodR }, 5, 90f, false);
            Egg(b, 1, new Vector3(0f, y, zEnd - tipR), new Vector3(tipR, tipR, tipR), 4, QMath.Identity);
            int rings = f.Small || f.Rocket ? 2 : (f.BLen > 0.45f ? 4 : 3);
            if (f.Barrel == 2 && !f.Rocket) rings++;
            float R0 = Mathf.Max(f.BR * 2.4f, 0.014f) * (f.Barrel == 3 ? 1.3f : 1f);
            if (f.Rocket) R0 = f.H * 0.5f;
            float zA = f.Z1 + Mathf.Max(0.006f, f.BLen * 0.15f), zB = zEnd - Mathf.Max(tipR * 2.5f, f.BLen * 0.2f);
            if (zB < zA) zB = zA;
            for (int i = 0; i < rings; i++)
            {
                float t = rings == 1 ? 0f : i / (float)(rings - 1);
                float tilt = f.Barrel == 2 ? (i % 2 == 0 ? 12f : -10f) : 0f;
                float R = R0 * Mathf.Lerp(1f, 0.62f, t) * (f.Barrel == 2 && i % 2 == 1 ? 0.8f : 1f);
                Halo(b, 1, new Vector3(0f, y, Mathf.Lerp(zA, zB, t)), QMath.AX(tilt), R, Mathf.Max(0.0025f, R * 0.14f), 5);
            }
        }

        /// <summary>Oddwire stocks: a rod with a pad, a rod with a fat coil, a helix spring, or a spring with a bulb.</summary>
        private static void OddwireStock(Frame f)
        {
            MeshBuilder b = f.B;
            float z0 = f.Z0, zE = z0 - f.SLen, h = f.H, w = f.W, cy = f.Cy;
            int v = f.StockVariant;
            float rod = Mathf.Max(0.004f, h * 0.08f);
            LoftZ(b, 1, 0f, cy - h * 0.1f, new[] { z0 + 0.008f, zE + 0.01f }, new[] { rod, rod }, new[] { rod, rod }, 4, 45f, false);
            float padZ = v == 3 ? zE - 0.0245f + 0.006f : zE + 0.006f;
            Box(b, 0, new Vector3(0f, cy - h * 0.15f, padZ), new Vector3(w * 0.6f, h * 1.15f, 0.012f), 5f);
            if (v == 1)
            {
                Quaternion rot = QMath.Euler(96f, 0f, 0f);
                b.Cylinder(1, new Vector3(0f, cy - h * 0.1f, z0 - f.SLen * 0.25f), h * 0.32f, h * 0.36f, f.SLen * 0.35f, 6, rot, true, UVMode.Fixed);
            }
            if (v >= 2)
            {
                const int n = 10;
                Vector3[] c = new Vector3[n];
                Vector2[] r = new Vector2[n];
                float hr = h * 0.3f;
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)(n - 1);
                    float a = t * Mathf.PI * 2f * 2.5f;
                    c[i] = new Vector3(Mathf.Cos(a) * hr, cy - h * 0.1f + Mathf.Sin(a) * hr, Mathf.Lerp(z0 + 0.004f, zE + 0.014f, t));
                    r[i] = new Vector2(0.0025f, 0.0025f);
                }
                Loft(b, 1, c, r, 3, 90f, false, false, Vector3.up);
            }
            if (v == 3) Egg(b, f.Core(2), new Vector3(0f, cy + h * 0.25f, z0 - f.SLen * 0.55f), new Vector3(w * 0.25f, h * 0.3f, h * 0.3f), 5, QMath.Identity);
        }

        /// <summary>The battery canister: on the left flank with a wire into the body (two odd sizes on the drum variant).</summary>
        private static void OddwireCanister(Frame f)
        {
            if (f.Rocket || (f.Small && f.Mag == 0)) return;
            MeshBuilder b = f.B;
            float s = f.AmmoScale;
            float[] lens = { 0.08f, 0.13f, 0.045f, 0.07f };
            float L = lens[Mathf.Clamp(f.Mag, 0, 3)] * s;
            float r = Mathf.Max(0.008f, f.H * 0.26f) * s;
            int n = f.Mag == 3 ? 2 : 1;
            for (int i = 0; i < n; i++)
            {
                float sx = i == 0 ? -1f : 1f;
                float rr = i == 0 ? r : r * 0.7f;
                float LL = i == 0 ? L : L * 0.6f;
                float x = sx * (f.W * 0.5f + rr * 0.6f), y = f.Cy - f.H * 0.28f, z = f.AmmoZ + i * L * 0.3f;
                LoftZ(b, 2, x, y, new[] { z - LL * 0.5f, z - LL * 0.38f, z + LL * 0.38f, z + LL * 0.5f }, new[] { rr * 0.7f, rr, rr, rr * 0.7f }, new[] { rr * 0.7f, rr, rr, rr * 0.7f }, 6, 90f, true);
                LoftZ(b, 1, x, y, new[] { z + LL * 0.5f - 0.002f, z + LL * 0.5f + 0.006f }, new[] { rr * 0.35f, rr * 0.3f }, new[] { rr * 0.35f, rr * 0.3f }, 5, 90f, true);
                Vector2 wv = new Vector2(0.002f, 0.002f);
                Loft(b, 1, new[] { new Vector3(x, y + rr * 0.9f, z - LL * 0.3f), new Vector3(x * 0.8f, y + rr * 1.6f, z - LL * 0.45f), new Vector3(x * 0.4f, f.Cy, z - LL * 0.4f) },
                    new[] { wv, wv, wv }, 3, 90f, false, false, Vector3.forward);
            }
        }
    }
}
