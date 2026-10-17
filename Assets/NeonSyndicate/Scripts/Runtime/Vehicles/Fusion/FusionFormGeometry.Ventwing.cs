using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Ventwing (quartz drakeling + bike): crystal ribs on the fairing, a drake head cowl with horns, eyes and a crystal
    /// emitter over the nose, three-panel articulated wings that fold flat against the flanks and spread with speed and
    /// glide, and a tail stabilizer (a vertebral boom, a steering fin and two pitching vanes).
    /// </summary>
    public static partial class FusionFormGeometry
    {
        private static void BuildVentwing(FormBlueprint bp)
        {
            FusionFrame f = bp.Frame;
            bool low = bp.Low;
            float k = Scale(f);
            int facets = low ? 4 : 6;
            int round = low ? 6 : 10;

            FormPart body = NewPart("Ventwing Body", Vector3.zero, FormPartRole.Static, 0, -1);
            MeshBuilder m = body.Mesh;

            // ---- crystal ribs on the fairing, ahead of the rider's knees
            int ribs = low ? 2 : 3;
            float rz0 = f.LegFrontZ + 0.14f;
            float rz1 = Mathf.Min(f.NoseZ - 0.32f, rz0 + 0.36f);
            for (int i = 0; i < ribs; i++)
            {
                float z = Mathf.Lerp(rz0, rz1, ribs > 1 ? i / (float)(ribs - 1) : 0f);
                float bottom, top, half;
                f.HullAt(z, out bottom, out top, out half);
                float baseY = bottom + 0.08f;
                float rise = Mathf.Min(0.24f, f.SeatY - 0.1f - baseY);
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 a = new Vector3(side * (half + 0.004f), baseY, z);
                    Vector3 tip = new Vector3(side * (half + 0.12f * k), baseY + rise, z - 0.07f);
                    Rod(m, FormSlots.Crystal, a, tip, 0.03f, 0.011f, facets - 1);
                    Box(m, FormSlots.Metal, a + new Vector3(side * 0.012f, 0f, 0f), new Vector3(0.03f, 0.05f, 0.06f), QMath.Identity);
                }
            }

            // ---- head cowl: skull, snout, jaw, brows, eyes, horns and a crest, all ahead of the handlebars
            float z0 = f.BarZ + 0.24f;
            float z1 = f.NoseZ + 0.26f * k;
            float zm = Mathf.Lerp(z0, z1, 0.45f);
            float b0, t0, h0, bm, tm, hm;
            f.HullAt(z0, out b0, out t0, out h0);
            f.HullAt(Mathf.Min(zm, f.NoseZ), out bm, out tm, out hm);
            float jawY = f.NoseBottom;
            float skullTop0 = t0 + 0.04f, skullTopM = Mathf.Max(tm, f.NoseTop + 0.16f);
            float hw0 = h0 * 1.25f, hwm = Mathf.Max(hm, f.HalfW * 0.9f) * 1.15f;
            Hexa(m, FormSlots.Shell, new[]
            {
                new Vector3(-hw0, jawY - 0.04f, z0), new Vector3(hw0, jawY - 0.04f, z0), new Vector3(hwm, jawY - 0.04f, zm), new Vector3(-hwm, jawY - 0.04f, zm),
                new Vector3(-hw0 * 0.7f, skullTop0, z0), new Vector3(hw0 * 0.7f, skullTop0, z0), new Vector3(hwm * 0.72f, skullTopM, zm), new Vector3(-hwm * 0.72f, skullTopM, zm)
            });
            float hwn = 0.07f * k;
            Hexa(m, FormSlots.Shell, new[]
            {
                new Vector3(-hwm, jawY - 0.04f, zm), new Vector3(hwm, jawY - 0.04f, zm), new Vector3(hwn, jawY + 0.04f, z1), new Vector3(-hwn, jawY + 0.04f, z1),
                new Vector3(-hwm * 0.72f, skullTopM, zm), new Vector3(hwm * 0.72f, skullTopM, zm), new Vector3(hwn * 0.8f, f.NoseTop + 0.03f, z1), new Vector3(-hwn * 0.8f, f.NoseTop + 0.03f, z1)
            });
            Hexa(m, FormSlots.Shell, new[]
            {
                new Vector3(-hwm * 0.85f, jawY - 0.12f, zm + 0.04f), new Vector3(hwm * 0.85f, jawY - 0.12f, zm + 0.04f), new Vector3(0.05f, jawY - 0.06f, z1 - 0.08f), new Vector3(-0.05f, jawY - 0.06f, z1 - 0.08f),
                new Vector3(-hwm * 0.9f, jawY - 0.04f, zm + 0.04f), new Vector3(hwm * 0.9f, jawY - 0.04f, zm + 0.04f), new Vector3(0.06f, jawY, z1 - 0.08f), new Vector3(-0.06f, jawY, z1 - 0.08f)
            });
            for (int side = -1; side <= 1; side += 2)
            {
                // Brow ridge over each eye, and the eye itself (a glowing slit).
                Box(m, FormSlots.Shell, new Vector3(side * hwm * 0.78f, skullTopM - 0.015f, zm - 0.06f), new Vector3(0.1f, 0.05f, 0.26f), QMath.AY(side * 8f) * QMath.AZ(-side * 10f));
                Box(m, FormSlots.Glow, new Vector3(side * hwm * 1.0f, skullTopM - 0.1f, zm + 0.02f), new Vector3(0.022f, 0.045f, 0.11f), QMath.AY(side * 6f));
                // Swept horns, ending ahead of the handlebars (never over the rider's hands).
                Spike(m, FormSlots.Crystal, new Vector3(side * hwm * 0.62f, skullTopM, zm - 0.02f), new Vector3(side * (hwm * 0.62f + 0.13f * k), skullTopM + 0.2f * k, z0 + 0.02f), 0.032f, facets);
                if (!low)
                    Spike(m, FormSlots.Crystal, new Vector3(side * hwm * 0.9f, skullTopM - 0.06f, zm + 0.08f), new Vector3(side * (hwm + 0.12f * k), skullTopM + 0.06f, zm - 0.16f), 0.022f, facets);
                // Cheek plates where the cowl meets the fairing.
                Box(m, FormSlots.Metal, new Vector3(side * (h0 + 0.01f), b0 + 0.12f, z0 + 0.04f), new Vector3(0.025f, 0.12f, 0.16f), QMath.Identity);
            }
            int crest = low ? 2 : 3;
            for (int i = 0; i < crest; i++)
            {
                float z = Mathf.Lerp(z0 + 0.05f, zm + 0.12f, crest > 1 ? i / (float)(crest - 1) : 0f);
                float y = Mathf.Lerp(skullTop0, skullTopM, Mathf.InverseLerp(z0, zm, z));
                Spike(m, FormSlots.Crystal, new Vector3(0f, y - 0.01f, z), new Vector3(0f, y + 0.1f, z - 0.06f), 0.025f, facets);
            }

            // ---- tail stabilizer: a vertebral boom over the tail with crystal spikes
            float zA = f.TailZ + 0.28f;
            float zB = f.TailZ - 0.5f * k;
            float yA = f.TailStackTop + 0.03f, yB = f.TailStackTop - 0.06f;
            float sb, st, sh;
            f.HullAt(zA, out sb, out st, out sh);
            // A mounting strut from the hull up to the boom (on the skiff the boom clears the courier top box).
            Rod(m, FormSlots.Metal, new Vector3(0f, st - 0.02f, zA + 0.06f), new Vector3(0f, yA, zA), 0.03f * k, 0.028f * k, round);
            int vertebrae = low ? 1 : 3;
            for (int i = 0; i < vertebrae; i++)
            {
                float s0 = i / (float)vertebrae, s1 = (i + 1) / (float)vertebrae;
                Vector3 a = Vector3.Lerp(new Vector3(0f, yA, zA), new Vector3(0f, yB, zB), Mathf.Max(0f, s0 - 0.03f));
                Vector3 c = Vector3.Lerp(new Vector3(0f, yA, zA), new Vector3(0f, yB, zB), s1);
                Rod(m, FormSlots.Shell, a, c, Mathf.Lerp(0.07f, 0.035f, s0) * k, Mathf.Lerp(0.07f, 0.035f, s1) * k, round);
            }
            int tailSpikes = low ? 2 : 3;
            for (int i = 0; i < tailSpikes; i++)
            {
                float s = (i + 0.5f) / tailSpikes;
                Vector3 a = Vector3.Lerp(new Vector3(0f, yA, zA), new Vector3(0f, yB, zB), s);
                float r = Mathf.Lerp(0.07f, 0.035f, s) * k;
                Spike(m, FormSlots.Crystal, a + new Vector3(0f, r * 0.7f, 0f), a + new Vector3(0f, r * 0.7f + 0.12f, -0.07f), 0.024f, facets);
            }
            Box(m, FormSlots.Metal, new Vector3(0f, yA - 0.02f, zA + 0.04f), new Vector3(0.16f * k, 0.06f, 0.1f), QMath.Identity);
            bp.Add(body);

            // ---- the steering fin and the two pitching vanes at the boom's end
            Vector3 boomEnd = new Vector3(0f, yB, zB + 0.06f);
            FormPart fin = NewPart("Tail Fin", boomEnd + new Vector3(0f, 0.02f, 0f), FormPartRole.TailFin, 0, -1);
            Hexa(fin.Mesh, FormSlots.Membrane, new[]
            {
                new Vector3(-0.012f, 0f, -0.04f), new Vector3(0.012f, 0f, -0.04f), new Vector3(0.012f, 0f, 0.32f * k), new Vector3(-0.012f, 0f, 0.32f * k),
                new Vector3(-0.008f, 0.4f * k, -0.14f), new Vector3(0.008f, 0.4f * k, -0.14f), new Vector3(0.008f, 0.4f * k, 0.04f), new Vector3(-0.008f, 0.4f * k, 0.04f)
            });
            Rod(fin.Mesh, FormSlots.Crystal, new Vector3(0f, 0f, 0.32f * k), new Vector3(0f, 0.4f * k, 0.04f), 0.016f, 0.01f, facets);
            bp.Add(fin);
            for (int side = -1; side <= 1; side += 2)
            {
                FormPart vane = NewPart(side < 0 ? "Tail Vane L" : "Tail Vane R", boomEnd + new Vector3(side * 0.03f, -0.02f, 0.04f), FormPartRole.TailVane, side, -1);
                Panel(vane.Mesh, FormSlots.Membrane, 0.32f * k, 0.2f * k, 0.1f * k, 0.02f, 0.06f, 0.5f, side);
                Rod(vane.Mesh, FormSlots.Crystal, new Vector3(0f, 0f, 0.1f * k), new Vector3(side * 0.32f * k, 0f, 0.05f * k - 0.06f), 0.012f, 0.008f, facets);
                bp.Add(vane);
            }

            // ---- the emitter at the snout tip (it pulses with the vents' heat)
            FormPart emitter = NewPart("Emitter", new Vector3(0f, f.LampLocal.y, z1 + 0.01f), FormPartRole.Emitter, 0, -1);
            emitter.Mesh.Sphere(FormSlots.Glow, Vector3.zero, new Vector3(0.055f, 0.055f, 0.035f) * k, low ? 4 : 6, round, QMath.Identity);
            emitter.Mesh.Cylinder(FormSlots.Metal, new Vector3(0f, 0f, -0.035f), 0.07f * k, 0.064f * k, 0.03f, round, QMath.AX(90f), false, UVMode.Fixed);
            emitter.CastShadows = false;
            bp.Add(emitter);

            // ---- three-panel wings hinged behind the rider's legs
            float hingeZ = f.SeatZ0 - 0.26f;
            float hb, ht, hh;
            f.HullAt(hingeZ, out hb, out ht, out hh);
            float hingeY = Mathf.Clamp(f.PillionY - 0.27f, hb + 0.08f, ht - 0.08f);
            float rootLen = 0.5f * k, midLen = 0.48f * k, tipLen = 0.42f * k;
            float c0 = 0.42f * k, c1 = 0.36f * k, c2 = 0.28f * k, c3 = 0.06f * k;
            for (int side = -1; side <= 1; side += 2)
            {
                FormPart root = NewPart(side < 0 ? "Wing Root L" : "Wing Root R", new Vector3(side * (hh + 0.02f), hingeY, hingeZ), FormPartRole.WingRoot, side, -1);
                WingPanel(root.Mesh, rootLen, c0, c1, 0.04f, 0.3f, side, low, facets);
                Box(root.Mesh, FormSlots.Metal, Vector3.zero, new Vector3(0.07f, 0.07f, 0.12f), QMath.Identity);
                int ri = bp.Add(root);
                FormPart mid = NewPart(side < 0 ? "Wing Mid L" : "Wing Mid R", new Vector3(side * rootLen, 0f, -0.04f), FormPartRole.WingMid, side, ri);
                WingPanel(mid.Mesh, midLen, c1, c2, 0.05f, 0.3f, side, low, facets);
                int mi = bp.Add(mid);
                FormPart tip = NewPart(side < 0 ? "Wing Tip L" : "Wing Tip R", new Vector3(side * midLen, 0f, -0.05f), FormPartRole.WingTip, side, mi);
                WingPanel(tip.Mesh, tipLen, c2, c3, 0.14f, 0.35f, side, low, facets);
                Spike(tip.Mesh, FormSlots.Crystal, new Vector3(side * tipLen * 0.92f, 0f, c3 * 0.35f - 0.14f), new Vector3(side * (tipLen + 0.09f * k), 0f, -0.2f), 0.018f, facets);
                bp.TrailParts.Add(bp.Add(tip));
            }
        }

        /// <summary>A wing panel: a membrane with a crystal spar along its leading edge and (High) finger struts.</summary>
        private static void WingPanel(MeshBuilder b, float len, float chordRoot, float chordTip, float sweep, float lead, int side, bool low, int facets)
        {
            Panel(b, FormSlots.Membrane, len, chordRoot, chordTip, 0.022f, sweep, lead, side);
            Vector3 a = new Vector3(0f, 0.004f, chordRoot * lead);
            Vector3 c = new Vector3(side * len, 0.004f, chordTip * lead - sweep);
            Rod(b, FormSlots.Crystal, a, c, 0.02f, 0.013f, facets);
            if (low) return;
            for (int i = 1; i <= 2; i++)
            {
                float s = i / 3f;
                Vector3 from = new Vector3(side * len * s * 0.4f, 0.014f, 0f);
                Vector3 to = new Vector3(side * len * Mathf.Lerp(0.55f, 0.95f, s), 0.014f, -Mathf.Lerp(chordRoot, chordTip, s) * (1f - lead) * 0.85f - sweep * s);
                Rod(b, FormSlots.Shell, from, to, 0.008f, 0.006f, 4);
            }
        }
    }
}
