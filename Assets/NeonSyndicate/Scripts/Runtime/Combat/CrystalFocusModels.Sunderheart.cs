using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Sunderheart: the Foreman's heavy stone-and-brass focus, braced at the hip with the support hand on a brass carry
    /// handle over the top. A rough basalt block banded in brass, three blunt stone jaws forming a cage at the front, and
    /// inside the cage a suspended compressed core (a cracked dark shell over an amber glow) circled by three amber
    /// pebbles. Charging compresses the core and pulls the pebbles in; a launch flexes the jaws and flings the pebbles out.
    /// </summary>
    public static partial class CrystalFocusModels
    {
        private const float SunderAxisY = 0.1f;
        private const float SunderCoreZ = 0.268f;

        private static CrystalModelGeometry BuildSunderheart()
        {
            CrystalModelGeometry m = new CrystalModelGeometry();
            MeshBuilder b = m.Body.Mesh;
            float ay = SunderAxisY;

            // A thick brass-wrapped rear grip.
            GripShape(b, SubStructure, 6, 30f, 0.016f, 0.023f, 0.06f);
            Vector3 gripAxis = Nrm(new Vector3(0f, 0.107f, 0.03f));
            for (int i = 0; i < 3; i++)
                Torus(b, SubMetal, new Vector3(0f, 0.02f - i * 0.03f, -0.002f - i * 0.009f), QMath.FromTo(Vector3.forward, gripAxis), 0.02f + i * 0.0012f, 0.0028f, 8);

            // The basalt block.
            LoftZ(b, SubStructure, new[] { new Vector2(0f, ay) },
                new[] { -0.2f, -0.186f, -0.12f, -0.02f, 0.08f, 0.15f, 0.172f },
                new[] { 0.04f, 0.06f, 0.068f, 0.06f, 0.058f, 0.054f, 0.044f },
                new[] { 0.044f, 0.064f, 0.073f, 0.064f, 0.062f, 0.057f, 0.047f }, 9, 22.5f, true, 0.09f, 41);
            // Rough stone slabs staggered along the flanks, and stone knuckles along the top of the chamber.
            for (int s = -1; s <= 1; s += 2)
            {
                Plate4(b, SubStructure, SubStructure,
                    new Vector3(0.067f * s, ay + 0.034f, -0.17f), new Vector3(0.068f * s, ay + 0.04f, -0.09f), new Vector3(0.069f * s, ay - 0.03f, -0.085f), new Vector3(0.066f * s, ay - 0.038f, -0.16f), 0.016f);
                Plate4(b, SubStructure, SubStructure,
                    new Vector3(0.061f * s, ay + 0.028f, -0.03f), new Vector3(0.06f * s, ay + 0.03f, 0.05f), new Vector3(0.06f * s, ay - 0.025f, 0.06f), new Vector3(0.061f * s, ay - 0.034f, -0.02f), 0.014f);
                Plate4(b, SubStructure, SubStructure,
                    new Vector3(0.056f * s, ay + 0.012f, 0.078f), new Vector3(0.052f * s, ay + 0.018f, 0.142f), new Vector3(0.052f * s, ay - 0.02f, 0.148f), new Vector3(0.056f * s, ay - 0.026f, 0.084f), 0.012f);
            }
            for (int k = 0; k < 3; k++)
            {
                float z = -0.185f + k * 0.045f;
                Plate4(b, SubStructure, SubStructure,
                    new Vector3(-0.026f, ay + 0.07f, z), new Vector3(0.026f, ay + 0.07f, z), new Vector3(0.022f, ay + 0.076f, z + 0.036f), new Vector3(-0.022f, ay + 0.076f, z + 0.036f), 0.014f);
            }
            // Brass bands.
            Collar(b, SubMetal, new Vector2(0f, ay), -0.198f, 0.05f, 0.054f, 0.01f, 8);
            Collar(b, SubMetal, new Vector2(0f, ay), -0.065f, 0.068f, 0.072f, 0.012f, 8);
            Collar(b, SubMetal, new Vector2(0f, ay), 0.11f, 0.067f, 0.071f, 0.014f, 8);
            // Rear vent ring: glows as the focus heats and flickers while venting.
            Torus(b, SubMetal, new Vector3(0f, ay, -0.203f), QMath.Identity, 0.03f, 0.004f, 10);
            for (int k = 0; k < 6; k++)
            {
                float a = (30f + k * 60f) * Mathf.Deg2Rad;
                Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                Shard(b, SubActive, new Vector3(0f, ay, -0.198f) + d * 0.012f, new Vector3(0f, ay, -0.206f) + d * 0.024f, 0.0035f, 3, 0.5f, 90f);
            }
            // Amber seams along the flanks.
            for (int s = -1; s <= 1; s += 2)
                Box(b, SubActive, new Vector3(0.0715f * s, ay, -0.128f), new Vector3(0.004f, 0.008f, 0.05f), QMath.Identity);

            // The brass carry handle over the top (the support hand's place).
            Vector3[] handle =
            {
                new Vector3(0f, ay + 0.06f, -0.045f), new Vector3(0f, ay + 0.1f, -0.015f), new Vector3(0f, ay + 0.113f, 0.03f),
                new Vector3(0f, ay + 0.113f, 0.09f), new Vector3(0f, ay + 0.1f, 0.13f), new Vector3(0f, ay + 0.058f, 0.152f)
            };
            Loft(b, SubMetal, handle, new[]
            {
                new Vector2(0.009f, 0.012f), new Vector2(0.0085f, 0.0095f), new Vector2(0.0095f, 0.0105f),
                new Vector2(0.0095f, 0.0105f), new Vector2(0.0085f, 0.0095f), new Vector2(0.009f, 0.012f)
            }, 6, 30f, false, false, Vector3.right);

            // Three blunt stone jaws caging the core.
            for (int i = 0; i < 3; i++) SunderJaw(m, i, ay);

            // Suspended compressed core and its pebbles.
            Vector3 coreAt = new Vector3(0f, ay, SunderCoreZ);
            CrystalPartGeometry core = m.Add("Core", CrystalPartRole.Core, 0, coreAt);
            CrackedOrb(core.Mesh, SubStructure, SubCore, Vector3.zero, 0.036f, 5, 8, 0.14f, 0.86f);
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f * Mathf.Deg2Rad;
                Vector3 off = Nrm(new Vector3(Mathf.Cos(a), Mathf.Sin(a) * 0.8f, Mathf.Sin(a) * 0.35f)) * 0.056f;
                CrystalPartGeometry peb = m.Add("Pebble " + i, CrystalPartRole.Pebble, i, coreAt + off);
                peb.Aux = coreAt;
                peb.Shadows = false;
                Orb(peb.Mesh, SubCrystal, Vector3.zero, new Vector3(0.0085f, 0.0075f, 0.011f), 3, 5, QMath.AZ(i * 50f));
            }

            m.Muzzle = new Vector3(0f, ay, 0.372f);
            m.Foregrip = new Vector3(0f, ay + 0.112f, 0.06f);
            m.TwoHanded = true;
            return m;
        }

        /// <summary>One stone jaw rooted on the block's front at 90, 210 or 330 degrees, curling in over the core.</summary>
        private static void SunderJaw(CrystalModelGeometry m, int i, float ay)
        {
            float a = (90f + i * 120f) * Mathf.Deg2Rad;
            Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            Vector3 root = new Vector3(0f, ay, 0.15f) + d * 0.046f;
            CrystalPartGeometry jaw = m.Add("Jaw " + i, CrystalPartRole.Jaw, i, root);
            Vector3[] path = { Vector3.zero, d * 0.013f + new Vector3(0f, 0f, 0.07f), d * 0.008f + new Vector3(0f, 0f, 0.14f), d * -0.012f + new Vector3(0f, 0f, 0.198f) };
            Vector2[] r = { new Vector2(0.015f, 0.013f), new Vector2(0.013f, 0.012f), new Vector2(0.01f, 0.0095f), new Vector2(0.006f, 0.0055f) };
            Loft(jaw.Mesh, SubStructure, path, r, 5, 18f, true, true, d, 0.14f, 50 + i);
            Torus(jaw.Mesh, SubMetal, new Vector3(0f, 0f, 0.012f) + d * 0.002f, QMath.Identity, 0.0155f, 0.003f, 7);
            Spike(jaw.Mesh, SubMetal, path[3] - new Vector3(0f, 0f, 0.008f), path[3] + new Vector3(0f, 0f, 0.016f) - d * 0.006f, 0.0062f, 5, false, 90f);
            // An amber glint on the inside face of the jaw.
            Shard(jaw.Mesh, SubActive, path[1] - d * 0.012f - new Vector3(0f, 0f, 0.02f), path[2] - d * 0.009f, 0.0032f, 3, 0.5f, 90f);
            jaw.Axis = OpenAxis(path[3], d, Nrm(Vector3.Cross(Vector3.forward, d)));
        }
    }
}
