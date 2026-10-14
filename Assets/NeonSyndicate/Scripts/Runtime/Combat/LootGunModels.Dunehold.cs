using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Dunehold: sandstone shells grown in a spiral. The body is a nautilus coil at the rear whose last whorl runs
    /// forward to an open aperture, a conch horn is the emitter, the stock curls into a smaller coil, and the magazine is
    /// a drum of eggs turning on an axle along the top.
    /// </summary>
    public static partial class LootGunModels
    {
        /// <summary>Growth per radian of a nautilus spiral (three times wider per turn).</summary>
        private const float NautilusK = 0.1748f;

        private static void Dunehold(Frame f)
        {
            MeshBuilder b = f.B;
            float z0 = f.Z0, len = f.Len, h = f.H, w = f.W, cy = f.Cy;
            float kb = f.Body == 1 ? 0.85f : 1f;
            // An open coil: the tube stays thin enough against the turn that the inner whorl shows inside the outer one.
            float rhoEnd = h * 0.85f * kb, tEnd = h * 0.36f * kb;
            float wr = Mathf.Clamp(w / h, 0.7f, 1.5f) * 1.05f;
            float back = Mathf.Exp(-NautilusK * Mathf.PI * 0.5f);
            // The coil rides on top at the rear; its last whorl leaves along the bottom and runs forward as the body.
            Vector3 C = new Vector3(0f, cy + rhoEnd, z0 + back * (rhoEnd + tEnd));
            float th0 = f.Body == 1 ? -1.5f * Mathf.PI : (f.Body == 3 ? -2.3f * Mathf.PI : -2f * Mathf.PI);
            const int spiral = 7;
            Vector3[] c = new Vector3[spiral + 3];
            Vector2[] r = new Vector2[spiral + 3];
            for (int i = 0; i < spiral; i++)
            {
                float th = Mathf.Lerp(th0, 0f, i / (float)(spiral - 1));
                float g = Mathf.Exp(NautilusK * th);
                c[i] = C + new Vector3(0f, -Mathf.Cos(th), Mathf.Sin(th)) * (rhoEnd * g);
                float t = i == 0 ? 0f : tEnd * g;
                r[i] = new Vector2(t, t * wr);
            }
            Vector3 p0 = c[spiral - 1];
            float tBody = tEnd * 1.35f;
            c[spiral] = new Vector3(0f, cy, p0.z + (f.Z1 - p0.z) * 0.45f);
            r[spiral] = new Vector2(tBody, tBody * wr);
            c[spiral + 1] = new Vector3(0f, cy, f.Z1 - 0.006f);
            r[spiral + 1] = new Vector2(tBody * 1.1f, tBody * 1.1f * wr);
            c[spiral + 2] = new Vector3(0f, cy, f.Z1);
            r[spiral + 2] = new Vector2(tBody * 1.3f, tBody * 1.3f * wr);
            Loft(b, 0, c, r, 5, 90f, false, true, Vector3.right);
            float shellTop = cy + tBody;
            f.ShellTop = shellTop;

            // The umbilicus at the coil's eye: bosses when reinforced, glowing with the element.
            if (f.Body == 2 || f.HasElement)
                for (int s = -1; s <= 1; s += 2)
                {
                    float ox = tEnd * 0.4f * wr;
                    if (f.Body == 2) Egg(b, 2, C + new Vector3(s * ox, 0f, 0f), new Vector3(tEnd * 0.6f, rhoEnd * 0.42f, rhoEnd * 0.42f), 4, QMath.AZ(90f));
                    if (f.HasElement) Shard(b, 4, C + new Vector3(s * ox * 0.5f, 0f, 0f), C + new Vector3(s * (ox + tEnd * 0.9f), 0f, 0f), rhoEnd * 0.25f, 3, 0.3f, 90f);
                }
            if (f.Body == 3)
                Fin(b, 2, new Vector3(0f, shellTop - 0.003f, c[spiral].z - len * 0.1f), new Vector3(0f, shellTop - 0.003f, f.Z1 - len * 0.05f), new Vector3(0f, shellTop + h * 0.3f, c[spiral].z - len * 0.12f), 0.004f);

            float coilFront = C.z + rhoEnd * Mathf.Exp(-NautilusK * Mathf.PI * 1.5f) + tEnd * 0.5f;
            DuneholdDrum(f, shellTop, coilFront);
            DuneholdHorn(f);
            if (f.HasStock) DuneholdStock(f);

            float lat = tBody * wr * 0.88f;
            f.AddVein(new Vector3(tEnd * wr * 0.85f, cy, p0.z - rhoEnd * 0.2f), Vector3.right);
            f.AddVein(new Vector3(lat * 0.95f, cy, p0.z + (c[spiral].z - p0.z) * 0.5f), Vector3.right);
            f.AddVein(new Vector3(lat, cy, c[spiral].z), Vector3.right);
            f.AddVein(new Vector3(lat * 1.08f, cy, f.Z1 - 0.008f), Vector3.right);
            f.VeinGap = tBody * 0.45f;
        }

        /// <summary>The egg drum: an axle on two claws along the top, in front of the coil, with a ring of eggs around it.</summary>
        private static void DuneholdDrum(Frame f, float shellTop, float coilFront)
        {
            MeshBuilder b = f.B;
            int[] counts = { 4, 6, 3, 5 };
            int mag = Mathf.Clamp(f.Mag, 0, 3);
            int count = counts[mag];
            if (f.A == LootArchetype.Pistol) count = Mathf.Max(3, count - 1);
            if (f.Rocket) count = Mathf.Min(count, 3);
            float er = Mathf.Max(0.0065f, f.H * 0.19f) * (mag == 3 ? 1.2f : (mag == 2 ? 0.85f : 1f));
            if (f.A == LootArchetype.Launcher && !f.Rocket) er *= 1.3f;
            if (f.Rocket) er = f.H * 0.14f;
            float eL = er * (mag == 1 ? 1.9f : 1.5f);
            float L = eL * 2.3f;
            float axleR = er * 0.4f;
            float ra = er * 1.15f + axleR;
            float dz = Mathf.Max(f.Z0 + f.Len * 0.55f, coilFront + L * 0.5f);
            dz = Mathf.Min(dz, f.Z1 - 0.03f - L * 0.5f);
            float axleY = shellTop + ra * 0.55f;
            LoftZ(b, 1, 0f, axleY, new[] { dz - L * 0.5f, dz + L * 0.5f }, new[] { axleR, axleR }, new[] { axleR, axleR }, 4, 45f, true);
            for (int s = -1; s <= 1; s += 2)
                Spike(b, 1, new Vector3(0f, shellTop - 0.004f, dz + s * L * 0.56f), new Vector3(0f, axleY, dz + s * L * 0.46f), Mathf.Max(0.004f, axleR * 1.2f), 3);
            int sub = f.Core(2);
            for (int j = 0; j < count; j++)
            {
                float a = (90f + 180f / count + j * 360f / count) * Mathf.Deg2Rad;
                Vector3 ec = new Vector3(Mathf.Cos(a) * ra, axleY + Mathf.Sin(a) * ra, dz);
                Egg(b, sub, ec, new Vector3(er, eL, er), 4, QMath.AX(90f));
            }
            f.Top = axleY + ra + er * 0.8f;
            f.SightZ = f.Rocket ? f.Z1 - 0.15f : f.Z1 - 0.025f;
        }

        /// <summary>The conch horn emitter: narrows, then flares to a lip with the element glowing inside.</summary>
        private static void DuneholdHorn(Frame f)
        {
            MeshBuilder b = f.B;
            float y = f.BarrelY;
            float zEnd = f.Z1 + f.BLen;
            float eR = Mathf.Max(f.BR * 1.25f, 0.008f) * (f.Barrel == 3 ? 1.3f : 1f);
            if (f.Rocket) eR = f.H * 0.35f;
            float[] z = { f.Z1 - 0.01f, f.Z1 + f.BLen * 0.45f, zEnd - 0.008f, zEnd };
            float[] rr = { eR * 1.15f, eR * 0.8f, eR * 0.95f, eR * 1.4f };
            Vector3[] c = new Vector3[4];
            Vector2[] r = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                c[i] = new Vector3(0f, y, z[i]);
                r[i] = new Vector2(rr[i], rr[i]);
            }
            Loft(b, 0, c, r, 5, 90f, false, false, Vector3.up);
            Spike(b, f.Core(1), new Vector3(0f, y, zEnd - 0.014f), new Vector3(0f, y, zEnd - 0.002f), eR * 1.2f, 5, false, 90f);
            if (f.Barrel == 2)
            {
                // A spiral growth ridge winding up the horn, as a flat strip facing out.
                int n = f.Rocket ? 5 : 8;
                Vector3[] hc = new Vector3[n];
                Vector3[] ho = new Vector3[n];
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)(n - 1);
                    float a = (90f + 75f * i) * Mathf.Deg2Rad;
                    Vector3 radial = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                    float rad = Mathf.Lerp(eR * 1.15f, eR * 0.95f, Mathf.Clamp01(t * 2f)) + 0.0012f;
                    hc[i] = new Vector3(0f, y, Mathf.Lerp(f.Z1 + 0.004f, zEnd - 0.012f, t)) + radial * rad;
                    ho[i] = radial;
                }
                Ribbon(b, 2, hc, ho, 0.005f);
            }
            if (f.Barrel == 3) Halo(b, 2, new Vector3(0f, y, f.Z1 + 0.012f), QMath.Identity, eR * 1.45f, Mathf.Max(0.003f, eR * 0.25f), f.Rocket ? 4 : 5);
        }

        /// <summary>Dunehold stocks: a tail that curls into a small coil (thin and open on the skeletal one, with a pad on the heavy one).</summary>
        private static void DuneholdStock(Frame f)
        {
            MeshBuilder b = f.B;
            float z0 = f.Z0, zE = z0 - f.SLen, h = f.H, w = f.W, cy = f.Cy;
            int v = f.StockVariant;
            float tube = (v == 2 ? 0.14f : (v == 1 ? 0.34f : 0.28f)) * h;
            float rC = h * (v == 3 ? 0.62f : (v == 2 ? 0.6f : 0.52f));
            float zEnd = v == 3 ? zE + 0.012f : zE;
            Vector3 cc = new Vector3(0f, cy - h * 0.12f - rC, zEnd + rC + tube * 0.7f);
            Vector3[] c =
            {
                new Vector3(0f, cy - h * 0.04f, z0 + 0.012f), new Vector3(0f, cy - h * 0.08f, (z0 + cc.z) * 0.5f),
                cc + new Vector3(0f, rC, 0f), cc + new Vector3(0f, rC * 0.15f, -rC), cc + new Vector3(0f, -rC * 0.75f, -rC * 0.2f), cc + new Vector3(0f, -rC * 0.25f, rC * 0.4f)
            };
            float wr = Mathf.Clamp(w / h, 0.7f, 1.5f);
            Vector2[] r =
            {
                new Vector2(tube * 1.5f, tube * 1.5f * wr), new Vector2(tube, tube * wr), new Vector2(tube * 0.9f, tube * 0.9f * wr),
                new Vector2(tube * 0.75f, tube * 0.75f * wr), new Vector2(tube * 0.5f, tube * 0.5f * wr), Vector2.zero
            };
            Loft(b, 0, c, r, 4, 45f, false, false, Vector3.right);
            if (v == 3) Egg(b, 2, new Vector3(0f, cc.y, zE - 0.012f), new Vector3(w * 0.42f, h * 0.7f, 0.0125f), 5, QMath.Identity);
        }
    }
}
