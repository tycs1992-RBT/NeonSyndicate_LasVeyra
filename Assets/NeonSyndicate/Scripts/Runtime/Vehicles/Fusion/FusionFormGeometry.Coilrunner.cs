using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Coilrunner (Stormskimmer + bike): a row of dorsal spines over the tail that flare when the arc dash is ready,
    /// swept side fins that follow the steering, a coil rail with ring coils on the tail, low conduits along both flanks
    /// to a ring around the nose, two forward antennae and an emitter orb. The discharge arc runs along that path.
    /// </summary>
    public static partial class FusionFormGeometry
    {
        private static void BuildCoilrunner(FormBlueprint bp)
        {
            FusionFrame f = bp.Frame;
            bool low = bp.Low;
            float k = Scale(f);
            int facets = low ? 4 : 6;
            int round = low ? 6 : 10;

            FormPart body = NewPart("Coilrunner Body", Vector3.zero, FormPartRole.Static, 0, -1);
            MeshBuilder m = body.Mesh;

            // ---- coil rail on the tail with ring coils threaded on it
            float railZ0 = f.SeatZ0 - 0.14f, railZ1 = f.TailZ - 0.08f;
            float rb0, rt0, rh0, rb1, rt1, rh1;
            f.HullAt(railZ0, out rb0, out rt0, out rh0);
            f.HullAt(railZ1, out rb1, out rt1, out rh1);
            float railY0 = TailTopAt(f, railZ0) + 0.05f, railY1 = TailTopAt(f, railZ1) + 0.05f;
            Rod(m, FormSlots.Metal, new Vector3(0f, railY0, railZ0), new Vector3(0f, railY1, railZ1), 0.018f, 0.018f, facets + 2);
            int coils = low ? 3 : 4;
            for (int i = 0; i < coils; i++)
            {
                float s = (i + 0.5f) / coils;
                float z = Mathf.Lerp(railZ0, railZ1, s);
                float y = Mathf.Lerp(railY0, railY1, s);
                m.Cylinder(FormSlots.Crystal, new Vector3(0f, y, z - 0.016f), 0.075f, 0.075f, 0.032f, round, QMath.AX(90f), false, UVMode.Fixed);
                m.Cylinder(FormSlots.Metal, new Vector3(0f, y, z - 0.02f), 0.06f, 0.06f, 0.04f, round, QMath.AX(90f), false, UVMode.Fixed);
                bp.ArcPath.Add(new Vector3(0f, y, z));
            }
            bp.ArcPath.Reverse();

            // ---- conduits low along both flanks (under the rider's legs) to the nose ring, with glowing nodes
            float lowY = Mathf.Min(0.34f, f.PegY - 0.09f);
            float[] cz = { f.TailZ + 0.12f, f.SeatZ0 - 0.05f, f.TankZ1, f.NoseZ - 0.12f };
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 prev = Vector3.zero;
                for (int i = 0; i < cz.Length; i++)
                {
                    float b, t, h;
                    f.HullAt(cz[i], out b, out t, out h);
                    Vector3 p = new Vector3(side * (Mathf.Max(h, f.PodR) + 0.05f), lowY, cz[i]);
                    if (i > 0) Rod(m, FormSlots.Metal, prev, p, 0.016f, 0.016f, facets);
                    m.Sphere(FormSlots.Glow, p, new Vector3(0.026f, 0.026f, 0.026f), 3, facets + 2, QMath.Identity);
                    if (side > 0 && i > 0) bp.ArcPath.Add(p);
                    prev = p;
                }
            }
            float lampY = f.LampLocal.y;
            m.Cylinder(FormSlots.Crystal, new Vector3(0f, lampY, f.NoseZ - 0.11f), 0.12f * k, 0.11f * k, 0.05f, round, QMath.AX(90f), false, UVMode.Fixed);
            bp.ArcPath.Add(new Vector3(0f, lampY + 0.12f * k, f.NoseZ - 0.08f));

            // ---- antennae over the nose
            for (int side = -1; side <= 1; side += 2)
                Spike(m, FormSlots.Crystal, new Vector3(side * 0.07f, f.NoseTop + 0.02f, f.NoseZ - 0.14f), new Vector3(side * 0.16f * k, f.NoseTop + 0.3f * k, f.NoseZ + 0.24f * k), 0.022f, facets);
            bp.Add(body);

            // ---- the emitter orb between the antennae
            FormPart emitter = NewPart("Emitter", new Vector3(0f, f.NoseTop + 0.05f, f.NoseZ + 0.1f * k), FormPartRole.Emitter, 0, -1);
            emitter.Mesh.Sphere(FormSlots.Glow, Vector3.zero, new Vector3(0.05f, 0.05f, 0.05f) * k, low ? 4 : 6, round, QMath.Identity);
            emitter.Mesh.Cylinder(FormSlots.Metal, new Vector3(0f, -0.07f * k, 0f), 0.02f, 0.035f, 0.035f, facets + 2, QMath.Identity, true, UVMode.Fixed);
            emitter.CastShadows = false;
            bp.Add(emitter);
            bp.ArcPath.Add(emitter.Position);

            // ---- dorsal spines (they flare when the dash is ready)
            int spines = low ? 4 : 6;
            float sz0 = f.SeatZ0 - 0.16f, sz1 = f.TailZ - 0.05f;
            for (int i = 0; i < spines; i++)
            {
                float s = spines > 1 ? i / (float)(spines - 1) : 0f;
                float z = Mathf.Lerp(sz0, sz1, s);
                float y = TailTopAt(f, z) + 0.03f;
                float h = (0.18f + 0.14f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(0.15f + s * 0.8f))) * k;
                FormPart spine = NewPart("Spine " + i, new Vector3(0f, y, z), FormPartRole.Spine, 0, -1);
                spine.Index = i;
                Spike(spine.Mesh, FormSlots.Crystal, Vector3.zero, new Vector3(0f, h, -h * 0.55f), 0.035f * k, facets);
                spine.Mesh.Sphere(FormSlots.Glow, new Vector3(0f, 0.01f, 0f), new Vector3(0.028f, 0.02f, 0.028f), 3, facets + 2, QMath.Identity);
                bp.Add(spine);
            }

            // ---- swept side fins (anhedral; they follow the steering)
            float finZ = f.SeatZ0 - 0.3f;
            float fb, ft, fh;
            f.HullAt(finZ, out fb, out ft, out fh);
            for (int side = -1; side <= 1; side += 2)
            {
                FormPart fin = NewPart(side < 0 ? "Fin L" : "Fin R", new Vector3(side * (fh + 0.01f), fb + 0.2f, finZ), FormPartRole.Fin, side, -1);
                fin.Rotation = side < 0 ? QMath.AZ(16f) : QMath.AZ(-16f);
                float len = 0.52f * k, cr = 0.36f * k, ct = 0.1f * k;
                Panel(fin.Mesh, FormSlots.Membrane, len, cr, ct, 0.02f, 0.3f * k, 0.4f, side);
                Rod(fin.Mesh, FormSlots.Crystal, new Vector3(0f, 0.004f, cr * 0.4f), new Vector3(side * len, 0.004f, ct * 0.4f - 0.3f * k), 0.014f, 0.009f, facets);
                if (!low) Rod(fin.Mesh, FormSlots.Glow, new Vector3(side * len * 0.15f, 0.013f, -cr * 0.2f), new Vector3(side * len * 0.85f, 0.013f, -ct * 0.3f - 0.26f * k), 0.006f, 0.006f, 4);
                bp.Add(fin);
            }
        }

        /// <summary>The top of the tail at a z, raised over the courier top box on the skiff.</summary>
        private static float TailTopAt(FusionFrame f, float z)
        {
            float b, t, h;
            f.HullAt(z, out b, out t, out h);
            if (f.TopBox && z < f.TailZ + 0.25f) t = Mathf.Max(t, f.TailStackTop);
            return t;
        }
    }
}
