using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The cosmetic chassis trims as pure blueprints: a quartz crown of spikes on the nose and tail, copper ember fins on
    /// the tail, pale frostglass plates on the flanks, chrome storm rails with a glow strip low along the hull, or a
    /// glowing coupler line with crystal nodes along the fairing. They never touch the rider's space and carry no stats.
    /// </summary>
    public static partial class FusionFormGeometry
    {
        public static FormBlueprint BuildTrim(string trimId, string chassisClass, bool low)
        {
            FormBlueprint bp = new FormBlueprint { Form = trimId ?? "", ChassisClass = chassisClass ?? FusionIds.Sport, Low = low };
            bp.Frame = FusionFrame.For(bp.ChassisClass);
            FusionFrame f = bp.Frame;
            int facets = low ? 4 : 6;
            FormPart part = NewPart("Chassis Trim", Vector3.zero, FormPartRole.Static, 0, -1);
            MeshBuilder m = part.Mesh;
            switch (trimId)
            {
                case ChassisTrims.QuartzCrown:
                {
                    float z = Mathf.Max(f.BarZ + 0.24f, f.NoseZ - 0.32f);
                    float b, t, h;
                    f.HullAt(z, out b, out t, out h);
                    int n = low ? 2 : 3;
                    for (int i = 0; i < n; i++)
                    {
                        float x = n > 1 ? Mathf.Lerp(-0.05f, 0.05f, i / (float)(n - 1)) : 0f;
                        Spike(m, FormSlots.Crystal, new Vector3(x, t - 0.01f, z + i * 0.04f), new Vector3(x * 2.2f, t + 0.16f - Mathf.Abs(x), z + 0.1f), 0.028f, facets);
                    }
                    float tz = f.TailZ + 0.06f;
                    float tt = TailTopAt(f, tz);
                    for (int side = -1; side <= 1; side += 2)
                        Spike(m, FormSlots.Crystal, new Vector3(side * 0.04f, tt - 0.01f, tz), new Vector3(side * 0.09f, tt + 0.14f, tz - 0.08f), 0.024f, facets);
                    break;
                }
                case ChassisTrims.EmberFins:
                {
                    float z = f.TailZ + 0.18f;
                    float b, t, h;
                    f.HullAt(z, out b, out t, out h);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector3 a = new Vector3(side * h * 0.9f, t - 0.06f, z);
                        Vector3[] c =
                        {
                            new Vector3(0f, 0f, -0.16f), new Vector3(0.012f, 0f, -0.16f), new Vector3(0.012f, 0f, 0.14f), new Vector3(0f, 0f, 0.14f),
                            new Vector3(0.1f, 0.2f, -0.24f), new Vector3(0.11f, 0.2f, -0.24f), new Vector3(0.11f, 0.2f, -0.12f), new Vector3(0.1f, 0.2f, -0.12f)
                        };
                        for (int i = 0; i < 8; i++) c[i] += new Vector3(h * 0.9f, t - 0.06f, z);
                        HexaSided(m, FormSlots.Membrane, c, side);
                        Rod(m, FormSlots.Crystal, a + new Vector3(0f, 0f, 0.14f), a + new Vector3(side * 0.105f, 0.2f, -0.12f), 0.01f, 0.008f, 4);
                    }
                    break;
                }
                case ChassisTrims.FrostPlates:
                {
                    float[,] spans = { { f.LegFrontZ + 0.04f, Mathf.Max(f.LegFrontZ + 0.2f, f.NoseZ - 0.3f) }, { f.TailZ + 0.12f, f.SeatZ0 - 0.24f } };
                    for (int s = 0; s < 2; s++)
                    {
                        float za = spans[s, 0], zb = spans[s, 1];
                        if (zb - za < 0.08f) continue;
                        float ba, ta, ha, bb2, tb, hb2;
                        f.HullAt(za, out ba, out ta, out ha);
                        f.HullAt(zb, out bb2, out tb, out hb2);
                        float ya = Mathf.Min(ba + 0.25f, f.SeatY - 0.12f), yb = Mathf.Min(bb2 + 0.25f, f.SeatY - 0.12f);
                        for (int side = -1; side <= 1; side += 2)
                        {
                            HexaSided(m, FormSlots.Membrane, new[]
                            {
                                new Vector3(ha + 0.004f, ba + 0.05f, za), new Vector3(ha + 0.024f, ba + 0.05f, za), new Vector3(hb2 + 0.024f, bb2 + 0.05f, zb), new Vector3(hb2 + 0.004f, bb2 + 0.05f, zb),
                                new Vector3(ha * 0.9f + 0.004f, ya, za + 0.04f), new Vector3(ha * 0.9f + 0.024f, ya, za + 0.04f), new Vector3(hb2 * 0.9f + 0.024f, yb, zb - 0.04f), new Vector3(hb2 * 0.9f + 0.004f, yb, zb - 0.04f)
                            }, side);
                        }
                    }
                    break;
                }
                case ChassisTrims.StormRails:
                {
                    float y = Mathf.Min(0.36f, f.PegY - 0.08f);
                    float[] z = { f.TailZ + 0.1f, f.SeatZ0, f.TankZ1, f.NoseZ - 0.15f };
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector3 prev = Vector3.zero;
                        for (int i = 0; i < z.Length; i++)
                        {
                            float b, t, h;
                            f.HullAt(z[i], out b, out t, out h);
                            Vector3 p = new Vector3(side * (Mathf.Max(h, f.PodR) + 0.02f), y, z[i]);
                            if (i > 0)
                            {
                                Rod(m, FormSlots.Metal, prev, p, 0.014f, 0.014f, facets);
                                Rod(m, FormSlots.Glow, prev + new Vector3(0f, -0.02f, 0f), p + new Vector3(0f, -0.02f, 0f), 0.006f, 0.006f, 4);
                            }
                            prev = p;
                        }
                    }
                    break;
                }
                case ChassisTrims.CouplerGlow:
                {
                    // A metal channel with a glowing crystal line along both flanks of the fairing, ahead of the rider's
                    // hands and knees, with small coupler nodes along it.
                    float za = Mathf.Max(f.BarZ + 0.24f, f.LegFrontZ + 0.06f);
                    float zb = f.NoseZ - 0.12f;
                    if (zb - za < 0.1f) break;
                    int samples = low ? 3 : 4;
                    int nodes = low ? 2 : 3;
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector3 prev = Vector3.zero;
                        for (int i = 0; i < samples; i++)
                        {
                            float z = Mathf.Lerp(za, zb, i / (float)(samples - 1));
                            float b, t, h;
                            f.HullAt(z, out b, out t, out h);
                            Vector3 p = new Vector3(side * (h + 0.012f), Mathf.Lerp(b, t, 0.5f), z);
                            if (i > 0)
                            {
                                Rod(m, FormSlots.Metal, prev, p, 0.012f, 0.012f, facets);
                                Rod(m, FormSlots.Glow, prev + new Vector3(side * 0.008f, 0f, 0f), p + new Vector3(side * 0.008f, 0f, 0f), 0.006f, 0.006f, 4);
                            }
                            prev = p;
                        }
                        for (int k = 0; k < nodes; k++)
                        {
                            float z = Mathf.Lerp(za, zb, (k + 0.5f) / nodes);
                            float b, t, h;
                            f.HullAt(z, out b, out t, out h);
                            Vector3 p = new Vector3(side * (h + 0.012f), Mathf.Lerp(b, t, 0.5f), z);
                            Rod(m, FormSlots.Crystal, p, p + new Vector3(side * 0.03f, 0f, 0f), 0.016f, 0.01f, facets);
                        }
                    }
                    break;
                }
            }
            part.CastShadows = false;
            bp.Add(part);
            return bp;
        }
    }
}
