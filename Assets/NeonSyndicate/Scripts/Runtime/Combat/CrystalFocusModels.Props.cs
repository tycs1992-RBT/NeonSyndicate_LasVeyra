using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The world props of the arsenal: the resonance case (a stone plinth with two lidded glass compartments and a tall
    /// glowing spire between them, so it reads from down the street) and the practice target (a crystal post with a brass
    /// cradle holding a double-sided bullseye with a glowing ring). Both have their origin on the ground and face +Z.
    /// </summary>
    public static partial class CrystalFocusModels
    {
        /// <summary>Compartment centres along X, and the hinge line of the lids.</summary>
        private const float CaseCompX = 0.34f;
        private const float CaseFloorY = 0.53f;
        private const float CaseHingeY = 0.95f;

        /// <summary>The resonance case as plain data.</summary>
        public static CrystalModelGeometry BuildCaseGeometry()
        {
            CrystalModelGeometry m = new CrystalModelGeometry();
            MeshBuilder b = m.Body.Mesh;
            Quaternion id = QMath.Identity;

            // Plinth: foot, block, brass trim and top slab.
            Box(b, SubStructure, new Vector3(0f, 0.06f, 0f), new Vector3(1.4f, 0.12f, 0.7f), id);
            Box(b, SubStructure, new Vector3(0f, 0.3f, 0f), new Vector3(1.3f, 0.36f, 0.6f), id);
            Box(b, SubMetal, new Vector3(0f, 0.465f, 0f), new Vector3(1.33f, 0.03f, 0.63f), id);
            Box(b, SubStructure, new Vector3(0f, 0.505f, 0f), new Vector3(1.36f, 0.05f, 0.66f), id);
            // A resonance emblem on the front face, and two brass feet strips.
            Gem(b, SubCore, new Vector3(0f, 0.3f, 0.302f), Vector3.forward, 0.07f, 0.02f, 6, 90f, Vector3.up);
            Torus(b, SubMetal, new Vector3(0f, 0.3f, 0.304f), id, 0.09f, 0.008f, 12);
            for (int s = -1; s <= 1; s += 2)
            {
                Box(b, SubMetal, new Vector3(0.5f * s, 0.3f, 0.302f), new Vector3(0.12f, 0.24f, 0.01f), id);
                Box(b, SubActive, new Vector3(0.5f * s, 0.3f, 0.308f), new Vector3(0.025f, 0.18f, 0.006f), id);
            }

            // Two compartments: corner posts, glass panes, a glowing cradle inside each.
            for (int c = 0; c < 2; c++)
            {
                float cx = c == 0 ? -CaseCompX : CaseCompX;
                for (int px = -1; px <= 1; px += 2)
                    for (int pz = -1; pz <= 1; pz += 2)
                        Box(b, SubMetal, new Vector3(cx + 0.275f * px, 0.74f, 0.25f * pz), new Vector3(0.04f, 0.42f, 0.04f), id);
                Box(b, SubMetal, new Vector3(cx, 0.94f, -0.25f), new Vector3(0.59f, 0.03f, 0.04f), id);
                Box(b, SubCrystal, new Vector3(cx, 0.74f, 0.25f), new Vector3(0.51f, 0.38f, 0.01f), id);
                Box(b, SubCrystal, new Vector3(cx, 0.74f, -0.25f), new Vector3(0.51f, 0.38f, 0.01f), id);
                Box(b, SubCrystal, new Vector3(cx - 0.275f, 0.74f, 0f), new Vector3(0.01f, 0.38f, 0.46f), id);
                Box(b, SubCrystal, new Vector3(cx + 0.275f, 0.74f, 0f), new Vector3(0.01f, 0.38f, 0.46f), id);
                // The cradle: a hexagonal stone pedestal with two glowing prongs.
                Loft(b, SubStructure, new[] { new Vector3(cx, CaseFloorY, 0f), new Vector3(cx, CaseFloorY + 0.05f, 0f), new Vector3(cx, CaseFloorY + 0.07f, 0f) },
                    new[] { new Vector2(0.1f, 0.1f), new Vector2(0.085f, 0.085f), new Vector2(0.07f, 0.07f) }, 6, 0f, false, true, Vector3.forward);
                for (int s = -1; s <= 1; s += 2)
                    Shard(b, SubActive, new Vector3(cx + 0.05f * s, CaseFloorY + 0.06f, 0f), new Vector3(cx + 0.075f * s, CaseFloorY + 0.16f, 0f), 0.012f, 4, 0.3f, 45f);
                m.Anchor(c == 0 ? "Slot A" : "Slot B", new Vector3(cx, CaseFloorY + 0.14f, 0f));
                CrystalPartGeometry lid = m.Add(c == 0 ? "Lid A" : "Lid B", CrystalPartRole.Lid, c, new Vector3(cx, CaseHingeY, -0.27f));
                // Lid frame, glass top and a crystal knob at the front, all relative to the back hinge.
                Box(lid.Mesh, SubMetal, new Vector3(0f, 0.012f, 0.27f), new Vector3(0.6f, 0.024f, 0.04f), id);
                Box(lid.Mesh, SubMetal, new Vector3(0f, 0.012f, 0.53f), new Vector3(0.6f, 0.024f, 0.04f), id);
                Box(lid.Mesh, SubMetal, new Vector3(-0.28f, 0.012f, 0.27f), new Vector3(0.04f, 0.024f, 0.56f), id);
                Box(lid.Mesh, SubMetal, new Vector3(0.28f, 0.012f, 0.27f), new Vector3(0.04f, 0.024f, 0.56f), id);
                Box(lid.Mesh, SubCrystal, new Vector3(0f, 0.016f, 0.27f), new Vector3(0.52f, 0.008f, 0.5f), id);
                Shard(lid.Mesh, SubActive, new Vector3(0f, 0.024f, 0.53f), new Vector3(0f, 0.06f, 0.55f), 0.018f, 5, 0.35f, 90f);
                lid.Axis = OpenAxis(new Vector3(0f, 0f, 0.56f), Vector3.up, Vector3.right);
            }

            // The resonance spire between the compartments.
            Shard(b, SubActive, new Vector3(0f, 0.52f, 0f), new Vector3(0f, 1.48f, 0f), 0.06f, 6, 0.32f, 0f);
            Shard(b, SubCrystal, new Vector3(0.03f, 0.52f, 0.03f), new Vector3(0.09f, 1.08f, 0.07f), 0.034f, 5, 0.3f, 20f);
            Shard(b, SubCrystal, new Vector3(-0.03f, 0.52f, -0.02f), new Vector3(-0.08f, 1.0f, -0.08f), 0.03f, 5, 0.3f, 20f);
            Torus(b, SubMetal, new Vector3(0f, 0.62f, 0f), QMath.AX(90f), 0.07f, 0.011f, 10);
            Torus(b, SubMetal, new Vector3(0f, 1.0f, 0f), QMath.AX(90f), 0.05f, 0.008f, 10);
            // Crystal growth at the plinth's front corners.
            for (int s = -1; s <= 1; s += 2)
            {
                Shard(b, SubCrystal, new Vector3(0.6f * s, 0.12f, 0.29f), new Vector3(0.665f * s, 0.34f, 0.33f), 0.03f, 5, 0.3f, 10f);
                Shard(b, SubCrystal, new Vector3(0.57f * s, 0.12f, 0.31f), new Vector3(0.59f * s, 0.24f, 0.37f), 0.02f, 4, 0.3f, 45f);
            }
            return m;
        }

        /// <summary>The practice target as plain data.</summary>
        public static CrystalModelGeometry BuildTargetGeometry()
        {
            CrystalModelGeometry m = new CrystalModelGeometry();
            MeshBuilder b = m.Body.Mesh;

            // Stone base, a crystal post up to a brass crescent cradle that holds the bullseye from below (nothing crosses
            // its face), and a crystal finial above the ring.
            Loft(b, SubStructure, new[] { new Vector3(0f, 0f, 0f), new Vector3(0f, 0.08f, 0f), new Vector3(0f, 0.13f, 0f) },
                new[] { new Vector2(0.32f, 0.32f), new Vector2(0.28f, 0.28f), new Vector2(0.16f, 0.16f) }, 6, 0f, true, true, Vector3.forward);
            Loft(b, SubCrystal, new[] { new Vector3(0f, 0.12f, 0f), new Vector3(0f, 0.6f, 0f), new Vector3(0f, 0.97f, 0f) },
                new[] { new Vector2(0.085f, 0.085f), new Vector2(0.075f, 0.075f), new Vector2(0.068f, 0.068f) }, 6, 0f, false, true, Vector3.forward);
            Torus(b, SubMetal, new Vector3(0f, 0.2f, 0f), QMath.AX(90f), 0.092f, 0.014f, 8);
            Torus(b, SubMetal, new Vector3(0f, 0.9f, 0f), QMath.AX(90f), 0.08f, 0.012f, 8);
            Vector3 eyeAt = new Vector3(0f, 1.35f, 0f);
            Vector3[] cradle = ArcPath(eyeAt, QMath.AY(90f), 0.39f, 205f, 335f, 12);
            Vector2[] cr = new Vector2[cradle.Length];
            for (int i = 0; i < cr.Length; i++)
            {
                float k = Mathf.Sin(Mathf.PI * i / (cradle.Length - 1));
                cr[i] = i == 0 || i == cradle.Length - 1 ? new Vector2(0.006f, 0.012f) : new Vector2(Mathf.Lerp(0.014f, 0.026f, k), 0.032f);
            }
            Loft(b, SubMetal, cradle, cr, 6, 0f, false, false, Vector3.forward);
            Loft(b, SubMetal, new[] { new Vector3(0f, 0.95f, 0f), new Vector3(0f, 0.97f, 0f) }, new[] { new Vector2(0.075f, 0.075f), new Vector2(0.06f, 0.06f) }, 6, 0f, true, true, Vector3.forward);
            Shard(b, SubActive, eyeAt + new Vector3(0f, 0.37f, 0f), eyeAt + new Vector3(0f, 0.56f, 0f), 0.045f, 6, 0.3f, 0f);
            Shard(b, SubCrystal, eyeAt + new Vector3(-0.37f, 0.06f, 0f), eyeAt + new Vector3(-0.47f, 0.16f, 0f), 0.022f, 4, 0.35f, 45f);
            Shard(b, SubCrystal, eyeAt + new Vector3(0.37f, 0.06f, 0f), eyeAt + new Vector3(0.47f, 0.16f, 0f), 0.022f, 4, 0.35f, 45f);

            // The bullseye: concentric double-sided rings, a moving part so a hit can make it shiver.
            CrystalPartGeometry eye = m.Add("Bullseye", CrystalPartRole.Ring, 0, new Vector3(0f, 1.35f, 0f));
            MeshBuilder e = eye.Mesh;
            Annulus(e, SubStructure, 0.27f, 0.35f, 0.04f, 24);
            Annulus(e, SubActive, 0.17f, 0.27f, 0.032f, 24);
            Annulus(e, SubMetal, 0.08f, 0.17f, 0.036f, 24);
            Gem(e, SubCore, Vector3.zero, Vector3.forward, 0.08f, 0.03f, 12, 0f, Vector3.up);
            return m;
        }

        /// <summary>A flat ring facing +Z and -Z between radii r0 and r1, thickness th, with its outer and inner rims.</summary>
        private static void Annulus(MeshBuilder b, int sub, float r0, float r1, float th, int segs)
        {
            float hz = th * 0.5f;
            for (int k = 0; k < segs; k++)
            {
                float a0 = k * Mathf.PI * 2f / segs, a1 = (k + 1) * Mathf.PI * 2f / segs;
                Vector3 i0 = new Vector3(Mathf.Cos(a0) * r0, Mathf.Sin(a0) * r0, 0f), i1 = new Vector3(Mathf.Cos(a1) * r0, Mathf.Sin(a1) * r0, 0f);
                Vector3 o0 = new Vector3(Mathf.Cos(a0) * r1, Mathf.Sin(a0) * r1, 0f), o1 = new Vector3(Mathf.Cos(a1) * r1, Mathf.Sin(a1) * r1, 0f);
                Vector3 f = new Vector3(0f, 0f, hz);
                Face(b, sub, i0 + f, o0 + f, o1 + f, i1 + f, Vector3.zero);
                Face(b, sub, i0 - f, o0 - f, o1 - f, i1 - f, Vector3.zero);
                Face(b, sub, o0 + f, o1 + f, o1 - f, o0 - f, Vector3.zero);
                // Inner rim faces the centre (the inside point sits far outside this segment).
                Vector3 mid = (i0 + i1) * 0.5f;
                Face(b, sub, i0 + f, i1 + f, i1 - f, i0 - f, mid * 4f);
            }
        }
    }
}
