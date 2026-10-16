using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Prism Thread: a split tuning fork in ivory ceramic and silver. Behind the grip a resonator hoop with a crystal
    /// strung across it (the shoulder brace), over the grip a teardrop resonance chamber with glowing windows, then the
    /// rounded crotch of the fork, from which two long tines run forward with an open channel between them (no barrel),
    /// and an open carrying loop under the fork for the support hand. A faceted lens floats in the channel. While the beam is on the tines part slightly (the emitter
    /// opens) and the lens stops spinning and steadies; overheating spreads the tines wider and they glow hot.
    /// </summary>
    public static partial class CrystalFocusModels
    {
        private const float PrismAxisY = 0.075f;

        private static CrystalModelGeometry BuildPrismThread()
        {
            CrystalModelGeometry m = new CrystalModelGeometry();
            MeshBuilder b = m.Body.Mesh;
            float ay = PrismAxisY;

            GripShape(b, SubStructure, 6, 30f, 0.014f, 0.021f, 0.05f);
            Torus(b, SubMetal, new Vector3(0f, -0.05f, -0.016f), QMath.FromTo(Vector3.forward, Nrm(new Vector3(0f, 0.107f, 0.03f))), 0.0195f, 0.0024f, 8);

            // Resonance chamber: a ceramic teardrop over the grip, narrowing into the fork, with glowing side windows.
            LoftZ(b, SubStructure, new[] { new Vector2(0f, 0.079f) },
                new[] { -0.095f, -0.08f, -0.04f, 0.025f, 0.095f, 0.145f, 0.176f },
                new[] { 0.008f, 0.02f, 0.028f, 0.03f, 0.024f, 0.017f, 0.012f },
                new[] { 0.012f, 0.03f, 0.042f, 0.045f, 0.036f, 0.026f, 0.02f }, 10, 18f, true);
            Collar(b, SubMetal, new Vector2(0f, 0.079f), -0.045f, 0.0285f, 0.0425f, 0.006f, 10);
            Collar(b, SubMetal, new Vector2(0f, 0.079f), 0.1f, 0.0245f, 0.0365f, 0.006f, 10);
            for (int s = -1; s <= 1; s += 2)
            {
                Shard(b, SubActive, new Vector3(0.0285f * s, 0.083f, -0.025f), new Vector3(0.0275f * s, 0.081f, 0.07f), 0.009f, 4, 0.45f, 0f, 0.35f);
                Shard(b, SubCrystal, new Vector3(0.025f * s, 0.066f, -0.01f), new Vector3(0.024f * s, 0.064f, 0.045f), 0.006f, 4, 0.45f, 0f, 0.35f);
            }
            Box(b, SubActive, new Vector3(0f, 0.1235f, 0.03f), new Vector3(0.005f, 0.003f, 0.08f), QMath.Identity);

            // Rear resonator hoop with a crystal strung across it (it is also the shoulder brace).
            Vector3 hoop = new Vector3(0f, 0.095f, -0.165f);
            Torus(b, SubStructure, hoop, QMath.AY(90f), 0.062f, 0.0085f, 16);
            Torus(b, SubMetal, hoop, QMath.AY(90f), 0.0525f, 0.0028f, 16);
            Shard(b, SubCrystal, hoop + new Vector3(0f, -0.052f, 0f), hoop + new Vector3(0f, 0.052f, 0f), 0.011f, 4, 0.5f, 45f);
            Shard(b, SubActive, hoop + new Vector3(0f, -0.012f, 0f), hoop + new Vector3(0f, 0.012f, 0f), 0.013f, 4, 0.5f, 0f);

            // The rounded crotch of the fork, joining the chamber to the two tines.
            Vector3 fork = new Vector3(0f, ay, 0.198f);
            Vector3[] crotch = ArcPath(fork, QMath.Identity, 0.024f, 90f, 270f, 10);
            Vector2[] cr = new Vector2[crotch.Length];
            for (int i = 0; i < cr.Length; i++) cr[i] = new Vector2(0.0072f, 0.0112f);
            Loft(b, SubStructure, crotch, cr, 6, 0f, true, true, Vector3.right);
            Collar(b, SubMetal, new Vector2(0f, ay), 0.176f, 0.0125f, 0.0215f, 0.005f, 8);

            // An open carrying loop under the fork (the support hand's place).
            Vector3[] loop =
            {
                new Vector3(0f, 0.05f, 0.2f), new Vector3(0f, 0.022f, 0.214f), new Vector3(0f, -0.003f, 0.206f),
                new Vector3(0f, -0.006f, 0.172f), new Vector3(0f, 0.012f, 0.148f), new Vector3(0f, 0.045f, 0.138f)
            };
            Vector2[] lr = new Vector2[loop.Length];
            for (int i = 0; i < lr.Length; i++) lr[i] = new Vector2(0.0068f, 0.0068f);
            Loft(b, SubStructure, loop, lr, 6, 0f, false, false, Vector3.right);
            Gem(b, SubMetal, new Vector3(0f, -0.005f, 0.189f), Vector3.forward, 0.0095f, 0.008f, 6, 0f, Vector3.up);

            // The two tines with their glowing inner faces, and the floating lens between them.
            PrismTine(m, 0, new Vector3(0f, ay + 0.024f, 0.2f), 1f);
            PrismTine(m, 1, new Vector3(0f, ay - 0.024f, 0.2f), -1f);
            CrystalPartGeometry lens = m.Add("Lens", CrystalPartRole.Lens, 0, new Vector3(0f, ay, 0.37f));
            lens.Axis = Vector3.forward;
            Gem(lens.Mesh, SubCore, Vector3.zero, Vector3.forward, 0.0165f, 0.0068f, 10, 0f, Vector3.up);
            Torus(lens.Mesh, SubMetal, Vector3.zero, QMath.Identity, 0.0205f, 0.0019f, 10);
            for (int k = 0; k < 4; k++)
            {
                float a = (45f + k * 90f) * Mathf.Deg2Rad;
                Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                Shard(lens.Mesh, SubActive, d * 0.0185f, d * 0.0255f, 0.0025f, 3, 0.4f, 90f);
            }

            m.Muzzle = new Vector3(0f, ay, 0.5f);
            m.Foregrip = new Vector3(0f, -0.004f, 0.188f);
            m.TwoHanded = true;
            return m;
        }

        /// <summary>One tine of the fork: a long flattened bar bowing outward and curling in at the tip, sign +1 upper.</summary>
        private static void PrismTine(CrystalModelGeometry m, int index, Vector3 root, float sign)
        {
            CrystalPartGeometry tine = m.Add(index == 0 ? "Tine Upper" : "Tine Lower", CrystalPartRole.Tine, index, root);
            Vector3[] path =
            {
                Vector3.zero,
                new Vector3(0f, 0.004f * sign, 0.08f),
                new Vector3(0f, 0.0065f * sign, 0.17f),
                new Vector3(0f, 0.004f * sign, 0.255f),
                new Vector3(0f, -0.003f * sign, 0.305f),
                new Vector3(0f, -0.008f * sign, 0.322f)
            };
            Vector2[] r =
            {
                new Vector2(0.0112f, 0.0072f), new Vector2(0.0108f, 0.0068f), new Vector2(0.0098f, 0.0062f),
                new Vector2(0.0088f, 0.0055f), new Vector2(0.0062f, 0.0042f), Vector2.zero
            };
            Loft(tine.Mesh, SubStructure, path, r, 6, 0f, true, false, Vector3.up);
            // Glowing strip on the channel side of the tine.
            Vector3[] strip = new Vector3[5];
            Vector2[] sr = new Vector2[5];
            for (int i = 0; i < 5; i++)
            {
                strip[i] = path[i] - new Vector3(0f, (r[i].y * 0.88f) * sign, 0f);
                sr[i] = i == 4 ? new Vector2(0.002f, 0.0008f) : new Vector2(0.0055f, 0.0014f);
            }
            strip[0].z = 0.008f;
            Loft(tine.Mesh, SubActive, strip, sr, 4, 45f, true, true, Vector3.up);
            // Silver tip cap.
            Spike(tine.Mesh, SubMetal, path[4] + new Vector3(0f, 0f, -0.012f), path[5] + new Vector3(0f, 0f, 0.004f), 0.0062f, 5, false, 90f);
            tine.Axis = OpenAxis(path[5], new Vector3(0f, sign, 0f), Vector3.right);
        }

        /// <summary>A short elliptical collar band around an axis along Z (centre x, y), slightly larger than the shell it wraps.</summary>
        private static void Collar(MeshBuilder b, int sub, Vector2 centre, float z, float rx, float ry, float len, int sides)
        {
            LoftZ(b, sub, new[] { centre }, new[] { z - len * 0.5f, z + len * 0.5f }, new[] { rx, rx }, new[] { ry, ry }, sides, 180f / sides, true);
        }
    }
}
