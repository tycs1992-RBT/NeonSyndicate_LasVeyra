using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Shardwake: a compact asymmetric mineral focus held like a pistol. A real stone grip with finger ridges, a lopsided
    /// mineral palm block with a heavier right cheek, and in front of it a bright shard core suspended in open air inside
    /// three unequal protective ribs (a long one arching over the top, a medium one on the right, a short one low on the
    /// left). The core spins up and draws back while charging, the ribs flare open, and a discharge kicks the core back.
    /// </summary>
    public static partial class CrystalFocusModels
    {
        /// <summary>The emitter axis height of Shardwake and the point its core floats on.</summary>
        private const float ShardwakeAxisY = 0.076f;

        private static CrystalModelGeometry BuildShardwake()
        {
            CrystalModelGeometry m = new CrystalModelGeometry();
            MeshBuilder b = m.Body.Mesh;
            float ay = ShardwakeAxisY;

            // Grip: mineral handle with finger ridges, a crystal pommel at the heel and a silver collar at the top.
            GripShape(b, SubStructure, 6, 30f, 0.0135f, 0.021f, 0.08f);
            Shard(b, SubCrystal, new Vector3(0f, -0.056f, -0.022f), new Vector3(0.002f, -0.084f, -0.042f), 0.009f, 5, 0.3f, 90f);
            Vector3 gripAxis = Nrm(new Vector3(0f, 0.107f, 0.03f));
            Torus(b, SubMetal, new Vector3(0f, 0.034f, 0.005f), QMath.FromTo(Vector3.forward, gripAxis), 0.0165f, 0.0026f, 8);
            Torus(b, SubMetal, new Vector3(0f, -0.046f, -0.014f), QMath.FromTo(Vector3.forward, gripAxis), 0.019f, 0.0024f, 8);

            // Palm block: a lopsided mineral mass over the grip, narrowing to a blunt point under the core.
            LoftZ(b, SubStructure,
                new[] { new Vector2(0.002f, 0.062f), new Vector2(0.004f, 0.061f), new Vector2(0.006f, 0.062f), new Vector2(0.007f, 0.06f), new Vector2(0.007f, 0.057f), new Vector2(0.006f, 0.054f) },
                new[] { -0.044f, -0.03f, -0.004f, 0.028f, 0.056f, 0.078f },
                new[] { 0.009f, 0.016f, 0.02f, 0.02f, 0.015f, 0f },
                new[] { 0.011f, 0.022f, 0.027f, 0.026f, 0.019f, 0f }, 7, 12f, true, 0.14f, 11);
            // The heavier right cheek (outside face of a right-hand hold, the side the shoulder camera sees).
            LoftZ(b, SubStructure,
                new[] { new Vector2(0.017f, 0.053f), new Vector2(0.021f, 0.051f), new Vector2(0.02f, 0.05f), new Vector2(0.016f, 0.052f) },
                new[] { -0.018f, 0.004f, 0.04f, 0.066f },
                new[] { 0f, 0.011f, 0.01f, 0f },
                new[] { 0f, 0.016f, 0.014f, 0f }, 5, 20f, false, 0.16f, 12);
            // Glowing seams across the cheek (they heat up with the focus).
            for (int i = 0; i < 3; i++)
            {
                float z = -0.004f + i * 0.019f;
                Shard(b, SubActive, new Vector3(0.0315f, 0.043f + i * 0.002f, z - 0.007f), new Vector3(0.0305f, 0.06f + i * 0.0015f, z + 0.006f), 0.0022f, 3, 0.5f, 0f);
            }
            // Mineral growth on the top left, swept back.
            Shard(b, SubCrystal, new Vector3(-0.006f, 0.084f, -0.012f), new Vector3(-0.012f, 0.104f, -0.034f), 0.0055f, 4, 0.3f, 45f);
            Shard(b, SubCrystal, new Vector3(-0.012f, 0.078f, 0.004f), new Vector3(-0.022f, 0.094f, -0.012f), 0.0045f, 4, 0.3f, 45f);
            Shard(b, SubCrystal, new Vector3(-0.002f, 0.082f, 0.018f), new Vector3(-0.004f, 0.097f, 0.004f), 0.0035f, 4, 0.3f, 45f);

            // Suspended shard core: a long bright crystal floating ahead of the palm block.
            CrystalPartGeometry core = m.Add("Core", CrystalPartRole.Core, 0, new Vector3(0f, ay, 0.148f));
            core.Axis = Vector3.forward;
            Shard(core.Mesh, SubCore, new Vector3(0f, 0f, -0.066f), new Vector3(0f, 0f, 0.066f), 0.0145f, 6, 0.36f, 0f);
            Shard(core.Mesh, SubCrystal, new Vector3(0.006f, 0.004f, -0.05f), new Vector3(0.009f, 0.007f, -0.012f), 0.0042f, 4, 0.5f, 45f);
            Shard(core.Mesh, SubCrystal, new Vector3(-0.006f, -0.004f, -0.044f), new Vector3(-0.009f, -0.006f, -0.004f), 0.0038f, 4, 0.5f, 45f);

            // Three unequal protective ribs, each pivoting at its root on the palm block.
            ShardwakeRib(m, b, 0, new[]
            {
                new Vector3(-0.006f, 0.082f, 0.028f), new Vector3(-0.013f, 0.099f, 0.066f), new Vector3(-0.014f, 0.104f, 0.115f),
                new Vector3(-0.009f, 0.099f, 0.165f), new Vector3(-0.003f, 0.09f, 0.203f), new Vector3(0f, 0.085f, 0.222f)
            }, 0.0068f, 0.0105f);
            ShardwakeRib(m, b, 1, new[]
            {
                new Vector3(0.021f, 0.066f, 0.052f), new Vector3(0.031f, 0.073f, 0.088f), new Vector3(0.03f, 0.079f, 0.128f),
                new Vector3(0.021f, 0.08f, 0.162f), new Vector3(0.013f, 0.078f, 0.18f)
            }, 0.0062f, 0.0095f);
            ShardwakeRib(m, b, 2, new[]
            {
                new Vector3(-0.012f, 0.043f, 0.054f), new Vector3(-0.018f, 0.052f, 0.085f), new Vector3(-0.015f, 0.062f, 0.111f),
                new Vector3(-0.009f, 0.066f, 0.126f)
            }, 0.006f, 0.0085f);

            m.Muzzle = new Vector3(0f, ay, 0.226f);
            m.TwoHanded = false;
            return m;
        }

        /// <summary>One rib: a tapered mineral band along the path (model space), with a silver collar at its root on the
        /// body and glowing studs along its inner edge. The rib is a moving part that flares away from the core axis.</summary>
        private static void ShardwakeRib(CrystalModelGeometry m, MeshBuilder body, int index, Vector3[] path, float rw, float rt)
        {
            Vector3 root = path[0];
            CrystalPartGeometry rib = m.Add("Rib " + (char)('A' + index), CrystalPartRole.Rib, index, root);
            Vector3[] local = new Vector3[path.Length];
            Vector2[] r = new Vector2[path.Length];
            for (int i = 0; i < path.Length; i++)
            {
                local[i] = path[i] - root;
                float k = i / (float)(path.Length - 1);
                float taper = i == path.Length - 1 ? 0f : Mathf.Lerp(1f, 0.45f, k);
                r[i] = new Vector2(rw * taper, rt * taper);
            }
            Loft(rib.Mesh, SubStructure, local, r, 5, 18f, true, false, Vector3.up, 0.12f, 20 + index);
            // Glowing studs along the rib, nudged toward the core axis.
            for (int i = 1; i < path.Length - 1; i++)
            {
                Vector3 p = local[i];
                Vector3 toAxis = Nrm(new Vector3(0f, ShardwakeAxisY, path[i].z) - path[i]);
                Vector3 c = p + toAxis * (rt * 0.9f);
                Vector3 t = Nrm(local[i + 1] - local[i - 1]);
                Shard(rib.Mesh, SubActive, c - t * 0.006f, c + t * 0.006f, 0.0024f, 4, 0.5f, 45f);
            }
            Vector3 tip = local[local.Length - 1];
            Vector3 away = Nrm(new Vector3(path[path.Length - 1].x, path[path.Length - 1].y - ShardwakeAxisY, 0f));
            if (away.sqrMagnitude < 0.5f) away = Nrm(new Vector3(root.x, root.y - ShardwakeAxisY, 0f));
            rib.Axis = OpenAxis(tip, away, Nrm(Vector3.Cross(Nrm(tip), away)));
            Torus(body, SubMetal, root - Nrm(local[1]) * 0.002f, QMath.FromTo(Vector3.forward, Nrm(local[1])), rw * 1.25f, 0.0018f, 7);
        }

        /// <summary>
        /// Picks the sign of a rotation axis so that turning by a positive angle moves the given point (in the part's
        /// space) further along "away". The rig opens ribs, tines, jaws and lids with positive angles.
        /// </summary>
        private static Vector3 OpenAxis(Vector3 point, Vector3 away, Vector3 axisGuess)
        {
            Vector3 axis = axisGuess.sqrMagnitude > 1e-8f ? Nrm(axisGuess) : Vector3.right;
            Vector3 moved = QMath.AngleAxis(10f, axis) * point;
            return Vector3.Dot(moved - point, away) >= 0f ? axis : -axis;
        }
    }
}
