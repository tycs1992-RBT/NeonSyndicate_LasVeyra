using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Mirage Choir: the Djinn's signature, grown rather than built. A sinuous sandstone stem with two swept leaf wings and
    /// gold filigree rises up and to the right into a large off-centre seed pod: a hewn sandstone cup ringed by curling
    /// gold sepals, holding a cluster of five big rose crystal seeds that point forward. A forward-curving thorn under the
    /// stem takes the support hand and a curling tendril with a gold crescent rests on the shoulder. Each held lock
    /// lifts one seed and lights it; a volley fans all five out of the pod and they settle back into formation,
    /// relighting one after another.
    /// </summary>
    public static partial class CrystalFocusModels
    {
        /// <summary>The pod centre: the seed cluster sits off the stem's axis, up and to the right.</summary>
        private static readonly Vector3 ChoirPod = new Vector3(0.02f, 0.105f, 0.235f);

        private static CrystalModelGeometry BuildMirageChoir()
        {
            CrystalModelGeometry m = new CrystalModelGeometry();
            MeshBuilder b = m.Body.Mesh;
            Vector3 pod = ChoirPod;

            GripShape(b, SubStructure, 6, 30f, 0.0135f, 0.021f, 0.07f);
            Vector3 gripAxis = Nrm(new Vector3(0f, 0.107f, 0.03f));
            Torus(b, SubMetal, new Vector3(0f, 0.032f, 0.005f), QMath.FromTo(Vector3.forward, gripAxis), 0.0168f, 0.0022f, 8);
            Torus(b, SubMetal, new Vector3(0f, -0.054f, -0.018f), QMath.FromTo(Vector3.forward, gripAxis), 0.0192f, 0.0022f, 8);

            // The stem: sweeping up and to the right into the pod.
            LoftZ(b, SubStructure,
                new[] { new Vector2(0f, 0.072f), new Vector2(0f, 0.073f), new Vector2(0.002f, 0.075f), new Vector2(0.006f, 0.081f), new Vector2(0.012f, 0.09f), new Vector2(0.017f, 0.099f) },
                new[] { -0.075f, -0.055f, 0f, 0.07f, 0.13f, 0.18f },
                new[] { 0.009f, 0.016f, 0.019f, 0.016f, 0.013f, 0.014f },
                new[] { 0.011f, 0.022f, 0.025f, 0.019f, 0.016f, 0.017f }, 7, 10f, true, 0.07f, 31);
            Collar(b, SubMetal, new Vector2(0.002f, 0.075f), 0.0f, 0.0205f, 0.0265f, 0.007f, 7);
            Collar(b, SubMetal, new Vector2(0.011f, 0.089f), 0.125f, 0.0145f, 0.0175f, 0.006f, 7);
            // Two swept leaf wings with gold edges.
            ChoirLeaf(b, new Vector3(-0.014f, 0.084f, 0.105f), new Vector3(-0.017f, 0.078f, 0.02f), new Vector3(-0.058f, 0.118f, -0.012f));
            ChoirLeaf(b, new Vector3(0.019f, 0.088f, 0.118f), new Vector3(0.021f, 0.081f, 0.05f), new Vector3(0.05f, 0.106f, 0.03f));
            // Rose glints along the stem.
            for (int i = 0; i < 3; i++)
            {
                float z = 0.03f + i * 0.04f;
                Shard(b, SubActive, new Vector3(0.019f + i * 0.003f, 0.079f + i * 0.004f, z - 0.01f), new Vector3(0.018f + i * 0.003f, 0.081f + i * 0.004f, z + 0.01f), 0.0028f, 3, 0.5f, 0f);
            }

            // The curling shoulder tendril ending in a gold crescent.
            Loft(b, SubStructure, new[] { new Vector3(0f, 0.069f, -0.07f), new Vector3(0f, 0.06f, -0.12f), new Vector3(0f, 0.052f, -0.17f), new Vector3(0f, 0.05f, -0.215f) },
                new[] { new Vector2(0.011f, 0.011f), new Vector2(0.009f, 0.009f), new Vector2(0.0088f, 0.0088f), new Vector2(0.0105f, 0.0105f) }, 6, 0f, false, false, Vector3.up, 0.06f, 32);
            Loft(b, SubStructure, new[] { new Vector3(0f, 0.055f, -0.135f), new Vector3(0f, 0.032f, -0.148f), new Vector3(0f, 0.018f, -0.172f), new Vector3(0f, 0.024f, -0.196f), new Vector3(0f, 0.038f, -0.2f) },
                new[] { new Vector2(0.006f, 0.006f), new Vector2(0.0052f, 0.0052f), new Vector2(0.0045f, 0.0045f), new Vector2(0.0035f, 0.0035f), Vector2.zero }, 5, 0f, false, false, Vector3.right);
            Vector3[] butt = ArcPath(new Vector3(0f, 0.054f, -0.192f), QMath.Identity, 0.05f, 128f, 232f, 8);
            Vector2[] br = new Vector2[butt.Length];
            for (int i = 0; i < butt.Length; i++) br[i] = i == 0 || i == butt.Length - 1 ? new Vector2(0.002f, 0.004f) : new Vector2(0.006f, 0.011f);
            Loft(b, SubMetal, butt, br, 5, 0f, false, false, Vector3.right);

            // A forward-curving thorn under the stem (the support hand's place).
            Loft(b, SubStructure, new[] { new Vector3(0.006f, 0.065f, 0.118f), new Vector3(0.005f, 0.03f, 0.13f), new Vector3(0.004f, 0.004f, 0.148f), new Vector3(0.003f, -0.013f, 0.186f) },
                new[] { new Vector2(0.0115f, 0.0125f), new Vector2(0.0105f, 0.0115f), new Vector2(0.0078f, 0.0082f), Vector2.zero }, 6, 30f, false, false, Vector3.forward, 0.05f, 33);

            // The pod: a hewn sandstone cup ringed by five curling gold sepals.
            Loft(b, SubStructure, new[] { pod + new Vector3(0f, 0f, -0.062f), pod + new Vector3(0f, 0f, -0.04f), pod + new Vector3(0f, 0f, -0.014f), pod + new Vector3(0f, 0f, 0.004f) },
                new[] { new Vector2(0.012f, 0.012f), new Vector2(0.028f, 0.028f), new Vector2(0.036f, 0.036f), new Vector2(0.032f, 0.032f) }, 9, 0f, true, false, Vector3.up, 0.08f, 34);
            // Inside of the cup (so the rim reads open from the front).
            Gem(b, SubCrystal, pod + new Vector3(0f, 0f, -0.006f), Vector3.forward, 0.03f, 0.004f, 9, 0f, Vector3.up);
            for (int k = 0; k < 5; k++)
            {
                float a = (54f + k * 72f) * Mathf.Deg2Rad;
                Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                Vector3 tan = new Vector3(-d.y, d.x, 0f);
                Vector3[] sp = { pod + d * 0.03f + new Vector3(0f, 0f, -0.012f), pod + d * 0.046f + new Vector3(0f, 0f, 0.018f), pod + d * 0.043f + new Vector3(0f, 0f, 0.05f), pod + d * 0.034f + new Vector3(0f, 0f, 0.068f) };
                Vector2[] sr = { new Vector2(0.004f, 0.012f), new Vector2(0.0035f, 0.012f), new Vector2(0.003f, 0.008f), Vector2.zero };
                Loft(b, SubMetal, sp, sr, 4, 45f, true, false, d);
            }

            // Five big seeds in the pod, each pivoting at its base.
            for (int i = 0; i < 5; i++)
            {
                float a = (90f + i * 72f) * Mathf.Deg2Rad;
                Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                float len = i == 0 ? 0.136f : 0.118f + (i % 2) * 0.01f;
                Vector3 basePos = pod + d * 0.015f + new Vector3(0f, 0f, -0.012f);
                CrystalPartGeometry seed = m.Add("Seed " + i, CrystalPartRole.Seed, i, basePos);
                seed.Rotation = QMath.FromTo(Vector3.forward, Nrm(Vector3.forward + d * 0.12f));
                seed.Aux = d;
                seed.Shadows = false;
                Shard(seed.Mesh, SubActive, Vector3.zero, new Vector3(0f, 0f, len), 0.0118f, 6, 0.3f, 30f);
                Shard(seed.Mesh, SubCore, new Vector3(0f, 0f, len * 0.16f), new Vector3(0f, 0f, len * 0.5f), 0.006f, 4, 0.5f, 45f);
                Collar(seed.Mesh, SubCrystal, Vector2.zero, 0.01f, 0.0098f, 0.0098f, 0.012f, 6);
                Vector3 tipLocal = new Vector3(0f, 0f, len);
                Vector3 awayLocal = QMath.Inverse(seed.Rotation) * d;
                seed.Axis = OpenAxis(tipLocal, awayLocal, Nrm(Vector3.Cross(Vector3.forward, awayLocal)));
            }

            m.Muzzle = new Vector3(0.012f, pod.y, 0.375f);
            m.Foregrip = new Vector3(0.005f, 0.028f, 0.132f);
            m.TwoHanded = true;
            return m;
        }

        /// <summary>A thin sandstone leaf from two points on the stem out to a tip, with a gold edge along its outer side.</summary>
        private static void ChoirLeaf(MeshBuilder b, Vector3 front, Vector3 back, Vector3 tip)
        {
            Fin(b, SubStructure, front, back, tip, 0.003f);
            Vector3 mid = Vector3.Lerp(front, tip, 0.5f);
            Loft(b, SubMetal, new[] { front, mid, tip }, new[] { new Vector2(0.0018f, 0.0018f), new Vector2(0.0022f, 0.0022f), Vector2.zero }, 4, 45f, false, false, Vector3.up);
        }
    }
}
