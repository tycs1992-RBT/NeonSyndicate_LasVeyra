using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Kilnback (Ashshell Warden + bike): layered mineral shell plates (a stepped hump behind the rider and two caps over
    /// the fairing) with glowing ember seams, low side skirts that keep the thruster pods clear, and a forward brace: a
    /// blunt prow on pistons that pushes forward during the brace ram, held by two brace arms.
    /// </summary>
    public static partial class FusionFormGeometry
    {
        private static void BuildKilnback(FormBlueprint bp)
        {
            FusionFrame f = bp.Frame;
            bool low = bp.Low;
            float k = Scale(f);
            int facets = low ? 4 : 6;

            FormPart body = NewPart("Kilnback Body", Vector3.zero, FormPartRole.Static, 0, -1);
            MeshBuilder m = body.Mesh;

            // ---- side skirts: low plates along both flanks, outside the pods and under the rider's legs
            float skirtTop = Mathf.Min(0.38f, f.PegY - 0.07f);
            float skirtBottom = 0.2f;
            float[] skirtZ = { f.TailZ + 0.08f, f.SeatZ0, f.TankZ1 + 0.03f, f.NoseZ - 0.12f };
            for (int s = 0; s < skirtZ.Length - 1; s++)
            {
                float za = skirtZ[s] + (s > 0 ? -0.04f : 0f), zb = skirtZ[s + 1];
                float ba, ta, ha, bb2, tb, hb2;
                f.HullAt(za, out ba, out ta, out ha);
                f.HullAt(zb, out bb2, out tb, out hb2);
                float ia = Mathf.Max(ha, f.PodR) + 0.06f, ib = Mathf.Max(hb2, f.PodR) + 0.06f;
                for (int side = -1; side <= 1; side += 2)
                {
                    HexaSided(m, FormSlots.Shell, new[]
                    {
                        new Vector3(ia, skirtBottom, za), new Vector3(ia + 0.07f, skirtBottom, za), new Vector3(ib + 0.07f, skirtBottom, zb), new Vector3(ib, skirtBottom, zb),
                        new Vector3(ia, skirtTop, za), new Vector3(ia + 0.04f, skirtTop, za), new Vector3(ib + 0.04f, skirtTop, zb), new Vector3(ib, skirtTop, zb)
                    }, side);
                    Rod(m, FormSlots.Crystal, new Vector3(side * (ia + 0.045f), skirtTop + 0.004f, za + 0.03f), new Vector3(side * (ib + 0.045f), skirtTop + 0.004f, zb - 0.03f), 0.011f, 0.011f, 4);
                }
            }

            // ---- brace arms from the front skirts to the prow, and the pistons the prow rides on
            float bn, tn, hn;
            f.HullAt(f.NoseZ - 0.14f, out bn, out tn, out hn);
            for (int side = -1; side <= 1; side += 2)
            {
                float bt, tt, ht;
                f.HullAt(f.TankZ1 + 0.1f, out bt, out tt, out ht);
                Rod(m, FormSlots.Metal, new Vector3(side * (Mathf.Max(ht, f.PodR) + 0.1f), skirtTop - 0.02f, f.TankZ1 + 0.1f), new Vector3(side * (hn + 0.05f), 0.46f, f.NoseZ - 0.12f), 0.03f, 0.026f, facets + 2);
                Rod(m, FormSlots.Metal, new Vector3(side * 0.09f, 0.36f, f.NoseZ - 0.32f), new Vector3(side * 0.09f, 0.36f, f.NoseZ + 0.06f), 0.026f, 0.026f, facets + 2);
            }
            bp.Add(body);

            // ---- shell plates: two caps over the fairing (ahead of the bars) and a stepped hump behind the rider
            float frontStart = f.BarZ + 0.22f;
            float frontEnd = f.NoseZ - 0.1f;
            int frontPlates = 2;
            for (int i = 0; i < frontPlates; i++)
            {
                float za = Mathf.Lerp(frontStart, frontEnd, i / (float)frontPlates) - (i > 0 ? 0.05f : 0f);
                float zb = Mathf.Lerp(frontStart, frontEnd, (i + 1) / (float)frontPlates);
                AddPlate(bp, "Front Plate " + i, f, za, zb, 0.08f, 0.07f, 0.1f, low, i, facets);
            }
            float rearStart = f.SeatZ0 - 0.16f;
            float rearEnd = f.TailZ - 0.14f * k;
            float[] lift = low ? new[] { 0.16f, 0.24f } : new[] { 0.14f, 0.24f, 0.16f };
            for (int i = 0; i < lift.Length; i++)
            {
                // Plates run from the back of the bike forward so each one overlaps the next like scales.
                float zb = Mathf.Lerp(rearStart, rearEnd, i / (float)lift.Length);
                float za = Mathf.Lerp(rearStart, rearEnd, (i + 1) / (float)lift.Length) - 0.05f;
                // AddPlate lifts the hump over the courier top box on the skiff (FusionFrame.TailStackTop).
                AddPlate(bp, "Hump Plate " + i, f, Mathf.Min(za, zb), Mathf.Max(za, zb), lift[i], 0.08f, 0.06f, low, frontPlates + i, facets);
            }

            // ---- the prow: a blunt wedge over the nose with an ember chevron, on the pistons
            FormPart prow = NewPart("Brace Prow", Vector3.zero, FormPartRole.Prow, 0, -1);
            MeshBuilder pm = prow.Mesh;
            float pz0 = f.NoseZ - 0.14f, pz1 = f.NoseZ + 0.38f * k;
            float pw0 = hn + 0.08f, pw1 = 0.1f * k;
            Hexa(pm, FormSlots.Shell, new[]
            {
                new Vector3(-pw0, 0.24f, pz0), new Vector3(pw0, 0.24f, pz0), new Vector3(pw1, 0.3f, pz1), new Vector3(-pw1, 0.3f, pz1),
                new Vector3(-pw0 * 0.8f, f.NoseTop + 0.1f, pz0), new Vector3(pw0 * 0.8f, f.NoseTop + 0.1f, pz0), new Vector3(pw1 * 0.9f, f.NoseTop - 0.12f, pz1), new Vector3(-pw1 * 0.9f, f.NoseTop - 0.12f, pz1)
            });
            Box(pm, FormSlots.Metal, new Vector3(0f, 0.3f + (f.NoseTop - 0.12f - 0.3f) * 0.5f, pz1 + 0.012f), new Vector3(pw1 * 2.2f, f.NoseTop - 0.42f, 0.03f), QMath.Identity);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 a = new Vector3(side * pw0 * 0.75f, f.NoseTop + 0.06f, pz0 + 0.06f);
                Vector3 c = new Vector3(0f, (f.NoseTop + 0.3f) * 0.5f, pz1 - 0.02f);
                Rod(pm, FormSlots.Crystal, a, c, 0.014f, 0.012f, 4);
                if (!low) Spike(pm, FormSlots.Shell, new Vector3(side * pw0 * 0.55f, f.NoseTop + 0.05f, pz0 + 0.12f), new Vector3(side * pw0 * 0.7f, f.NoseTop + 0.16f, pz0 - 0.02f), 0.03f, facets);
            }
            bp.Add(prow);
        }

        /// <summary>One gable-shaped mineral plate between z0 and z1 with an ember seam along its front edge and (High) nubs.</summary>
        private static void AddPlate(FormBlueprint bp, string name, FusionFrame f, float z0, float z1, float ridgeLift, float outerPad, float lowLift, bool low, int index, int facets)
        {
            float b0, t0, h0, b1, t1, h1;
            f.HullAt(z0, out b0, out t0, out h0);
            f.HullAt(z1, out b1, out t1, out h1);
            float top0 = Mathf.Max(t0, TailTopAt(f, z0));
            float top1 = Mathf.Max(t1, TailTopAt(f, z1));
            float ridge0 = top0 + ridgeLift, ridge1 = top1 + ridgeLift;
            float outer0 = h0 + outerPad, outer1 = h1 + outerPad;
            float low0 = b0 + lowLift, low1 = b1 + lowLift;
            FormPart plate = NewPart(name, Vector3.zero, FormPartRole.Plate, 0, -1);
            plate.Index = index;
            for (int side = -1; side <= 1; side += 2)
            {
                ShellHalf(plate.Mesh, FormSlots.Shell, z0, z1, outer0, outer1, low0, low1, ridge0, ridge1, 0.05f, side);
                Rod(plate.Mesh, FormSlots.Crystal, new Vector3(0f, ridge1 + 0.004f, z1 - 0.012f), new Vector3(side * (outer1 + 0.03f), low1 + 0.03f, z1 - 0.012f), 0.012f, 0.012f, 4);
            }
            if (!low)
            {
                for (int n = 0; n < 2; n++)
                {
                    float z = Mathf.Lerp(z0, z1, 0.3f + n * 0.4f);
                    float y = Mathf.Lerp(ridge0, ridge1, Mathf.InverseLerp(z0, z1, z));
                    Spike(plate.Mesh, FormSlots.Shell, new Vector3(0f, y - 0.01f, z), new Vector3(0f, y + 0.06f, z - 0.03f), 0.03f, facets);
                }
            }
            bp.Add(plate);
        }
    }
}
