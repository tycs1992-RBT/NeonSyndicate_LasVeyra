using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Mirrorwake: the Queen's signature, a diamond-profile obsidian housing with a dorsal blade, a swept keel for the
    /// support hand and a single tapered spine to the shoulder; ahead of it a dark three-sided prism floats on the emitter
    /// axis, ringed by a fan of six hovering mirror facets with glowing rims, twisted like fan blades. Charging draws the
    /// facets in until they close around the prism like a cone; after the pulse they spring back out and settle. While
    /// a focus is planted the top facet is missing from the fan (it is the focus).
    /// </summary>
    public static partial class CrystalFocusModels
    {
        private const float MirrorAxisY = 0.075f;
        /// <summary>The point on the prism the facets converge on.</summary>
        private static readonly Vector3 MirrorFocusPoint = new Vector3(0f, MirrorAxisY, 0.3f);

        private static CrystalModelGeometry BuildMirrorwake()
        {
            CrystalModelGeometry m = new CrystalModelGeometry();
            MeshBuilder b = m.Body.Mesh;
            float ay = MirrorAxisY;

            GripShape(b, SubStructure, 5, 0f, 0.0142f, 0.021f, 0.06f);
            Vector3 gripAxis = Nrm(new Vector3(0f, 0.107f, 0.03f));
            Torus(b, SubMetal, new Vector3(0f, 0.033f, 0.005f), QMath.FromTo(Vector3.forward, gripAxis), 0.0172f, 0.0022f, 6);

            // Diamond-profile obsidian housing with a dorsal blade, silver edges and violet slits.
            LoftZ(b, SubStructure, new[] { new Vector2(0f, 0.08f) },
                new[] { -0.105f, -0.065f, -0.005f, 0.06f, 0.12f, 0.162f },
                new[] { 0.004f, 0.018f, 0.023f, 0.022f, 0.017f, 0.01f },
                new[] { 0.005f, 0.026f, 0.043f, 0.039f, 0.024f, 0.012f }, 6, 90f, true);
            Fin(b, SubStructure, new Vector3(0f, 0.118f, -0.035f), new Vector3(0f, 0.114f, 0.075f), new Vector3(0f, 0.158f, -0.07f), 0.0045f);
            Loft(b, SubMetal, new[] { new Vector3(0f, 0.114f, 0.075f), new Vector3(0f, 0.158f, -0.07f) },
                new[] { new Vector2(0.0028f, 0.0028f), new Vector2(0.0012f, 0.0012f) }, 4, 45f, true, false, Vector3.up);
            for (int s = -1; s <= 1; s += 2)
            {
                Box(b, SubActive, new Vector3(0.0215f * s, 0.088f, 0.015f), new Vector3(0.003f, 0.003f, 0.075f), QMath.AY(-5f * s) * QMath.AX(-8f));
                Box(b, SubActive, new Vector3(0.0205f * s, 0.07f, 0.04f), new Vector3(0.003f, 0.003f, 0.06f), QMath.AY(-5f * s) * QMath.AX(6f));
                Shard(b, SubMetal, new Vector3(0.022f * s, 0.08f, -0.06f), new Vector3(0.023f * s, 0.08f, 0.12f), 0.0035f, 4, 0.4f, 45f, 0.5f);
            }

            // A single tapered spine back to the shoulder, ending in an angular silver heel.
            Loft(b, SubStructure, new[] { new Vector3(0f, 0.088f, -0.09f), new Vector3(0f, 0.075f, -0.17f), new Vector3(0f, 0.062f, -0.238f) },
                new[] { new Vector2(0.012f, 0.014f), new Vector2(0.0095f, 0.012f), new Vector2(0.0065f, 0.0085f) }, 3, 90f, false, true, Vector3.up);
            Fin(b, SubMetal, new Vector3(0f, 0.094f, -0.232f), new Vector3(0f, 0.026f, -0.238f), new Vector3(0f, 0.058f, -0.256f), 0.013f);

            // A swept keel under the front of the housing (the support hand's place), silver edged.
            Plate4(b, SubStructure, SubMetal, new Vector3(0f, 0.052f, 0.055f), new Vector3(0f, 0.052f, 0.165f), new Vector3(0f, 0.002f, 0.142f), new Vector3(0f, -0.012f, 0.1f), 0.016f);
            // A silver socket ring the prism floats out of.
            Torus(b, SubMetal, new Vector3(0f, ay, 0.168f), QMath.Identity, 0.026f, 0.0034f, 9);

            // The dark prism: a three-sided crystal pointed at both ends, turning slowly, its edges glowing.
            CrystalPartGeometry prism = m.Add("Prism", CrystalPartRole.Prism, 0, new Vector3(0f, ay, 0.29f));
            prism.Axis = Vector3.forward;
            Loft(prism.Mesh, SubStructure, new[] { new Vector3(0f, 0f, -0.095f), new Vector3(0f, 0f, -0.062f), new Vector3(0f, 0f, 0.062f), new Vector3(0f, 0f, 0.108f) },
                new[] { Vector2.zero, new Vector2(0.029f, 0.029f), new Vector2(0.029f, 0.029f), Vector2.zero }, 3, 90f, false, false, Vector3.up);
            for (int k = 0; k < 3; k++)
            {
                float a = (90f + k * 120f) * Mathf.Deg2Rad;
                Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 0.0285f;
                Loft(prism.Mesh, SubActive, new[] { new Vector3(d.x, d.y, -0.06f), new Vector3(d.x, d.y, 0.06f) },
                    new[] { new Vector2(0.0018f, 0.0018f), new Vector2(0.0018f, 0.0018f) }, 4, 45f, true, true, Vector3.up);
            }
            Gem(prism.Mesh, SubCore, new Vector3(0f, 0f, 0.098f), Vector3.forward, 0.008f, 0.014f, 6, 0f, Vector3.up);
            Gem(prism.Mesh, SubCore, new Vector3(0f, 0f, 0f), Vector3.forward, 0.017f, 0.05f, 3, 90f, Vector3.up);

            // The fan of six mirror facets around the prism, twisted like fan blades so they read from any side.
            Vector3 focus = MirrorFocusPoint;
            for (int i = 0; i < 6; i++)
            {
                float th = 90f + i * 60f;
                float a = th * Mathf.Deg2Rad;
                Vector3 radial = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                Vector3 at = focus + radial * 0.084f + new Vector3(0f, 0f, (i % 2 == 0 ? -0.008f : 0.008f));
                CrystalPartGeometry facet = m.Add("Facet " + i, CrystalPartRole.Facet, i, at);
                // Local +Y runs outward along the radial; twisted 40 degrees about it, the outer end leaning back like a dish.
                facet.Rotation = QMath.AZ(th - 90f) * QMath.AX(-20f) * QMath.AY(40f);
                facet.Aux = focus;
                facet.Shadows = false;
                MirrorFacet(facet.Mesh, 0.036f, 0.052f, 0.022f, 0.0045f);
                // Converging tilts the outer end forward (toward +Z).
                facet.Axis = OpenAxis(new Vector3(0f, 0.052f, 0f), QMath.Inverse(facet.Rotation) * Vector3.forward, Vector3.right);
            }

            m.Muzzle = new Vector3(0f, ay, 0.42f);
            m.Foregrip = new Vector3(0f, 0.022f, 0.13f);
            m.TwoHanded = true;
            return m;
        }

        /// <summary>A kite-shaped mirror facet in the local XY plane (long axis +Y): mirror faces, glowing rim.</summary>
        private static void MirrorFacet(MeshBuilder b, float down, float up, float halfWidth, float thick)
        {
            Plate4(b, SubMetal, SubActive,
                new Vector3(0f, -down, 0f), new Vector3(halfWidth, up * 0.1f, 0f), new Vector3(0f, up, 0f), new Vector3(-halfWidth, up * 0.1f, 0f), thick);
        }
    }
}
