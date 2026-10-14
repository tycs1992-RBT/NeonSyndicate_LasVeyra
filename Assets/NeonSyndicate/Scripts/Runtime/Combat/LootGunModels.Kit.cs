using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The low-poly shape kit the loot gun makers build from: lofted shells along a path, spikes, crystal shards, thin
    /// fins, halos (rings around an axis), arched carapace plates and flat glowing ribbons. Every face is flat shaded
    /// and wound outward automatically from an "inside" point, so shapes can be placed freely. Engine-free (QMath
    /// rotations, no Quaternion.Euler), so the offline harness can build every gun without Unity.
    /// </summary>
    public static partial class LootGunModels
    {
        private static readonly Vector2 FixedUv = new Vector2(0.5f, 0.5f);

        /// <summary>Normalizes by hand: Vector3.normalized returns zero below 1e-5, and gun parts are millimetres.</summary>
        private static Vector3 Nrm(Vector3 v)
        {
            float m = v.magnitude;
            return m > 1e-12f ? v / m : Vector3.zero;
        }

        private static Quaternion Rot(float pitch, float yaw, float roll)
        {
            return QMath.Euler(pitch, yaw, roll);
        }

        /// <summary>A box, optionally pitched (about X), yawed and rolled. faceMask as MeshBuilder.Box.</summary>
        private static void Box(MeshBuilder b, int sub, Vector3 c, Vector3 s, float pitch = 0f, float roll = 0f, float yaw = 0f, int faceMask = 63)
        {
            b.FixedUV = FixedUv;
            b.Box(sub, c, s, Rot(pitch, yaw, roll), UVMode.Fixed, faceMask);
        }

        /// <summary>A quad wound so its normal points away from the inside point.</summary>
        private static void Face(MeshBuilder b, int sub, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 inside)
        {
            Vector3 n = Vector3.Cross(p1 - p0, p2 - p0);
            if (n.sqrMagnitude < 1e-14f) n = Vector3.Cross(p2 - p0, p3 - p0);
            Vector3 mid = (p0 + p1 + p2 + p3) * 0.25f;
            if (Vector3.Dot(n, mid - inside) < 0f)
            {
                Vector3 t = p1;
                p1 = p3;
                p3 = t;
                n = -n;
            }
            float m = n.magnitude;
            n = m > 1e-9f ? n / m : Vector3.up;
            b.Quad(sub, p0, p1, p2, p3, n, FixedUv, FixedUv, FixedUv, FixedUv);
        }

        /// <summary>A triangle wound so its normal points away from the inside point.</summary>
        private static void Tri(MeshBuilder b, int sub, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 inside)
        {
            Vector3 n = Vector3.Cross(p1 - p0, p2 - p0);
            Vector3 mid = (p0 + p1 + p2) / 3f;
            if (Vector3.Dot(n, mid - inside) < 0f)
            {
                Vector3 t = p1;
                p1 = p2;
                p2 = t;
                n = -n;
            }
            float m = n.magnitude;
            n = m > 1e-9f ? n / m : Vector3.up;
            b.Triangle(sub, p0, p1, p2, n, FixedUv, FixedUv, FixedUv);
        }

        /// <summary>
        /// A shell lofted through rings: ring i sits at c[i], at right angles to the path, with half sizes r[i] (x along
        /// the side axis, y along the up axis). A ring with zero size is a point, so ends can taper to a tip. upHint
        /// picks the ring's up axis (Vector3.up for paths along Z, Vector3.right for curves in the side plane).
        /// </summary>
        private static void Loft(MeshBuilder b, int sub, Vector3[] c, Vector2[] r, int sides, float phaseDeg, bool capStart, bool capEnd, Vector3 upHint)
        {
            int n = c.Length;
            if (n < 2) return;
            Vector3[][] ring = new Vector3[n][];
            Vector3[] tang = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                Vector3 t = c[Mathf.Min(n - 1, i + 1)] - c[Mathf.Max(0, i - 1)];
                t = t.sqrMagnitude > 1e-16f ? Nrm(t) : Vector3.forward;
                tang[i] = t;
                Vector3 u = upHint - t * Vector3.Dot(upHint, t);
                if (u.sqrMagnitude < 1e-6f)
                {
                    Vector3 alt = Mathf.Abs(t.y) < 0.9f ? Vector3.up : Vector3.forward;
                    u = alt - t * Vector3.Dot(alt, t);
                }
                u = Nrm(u);
                Vector3 s = Vector3.Cross(u, t);
                ring[i] = new Vector3[sides];
                for (int k = 0; k < sides; k++)
                {
                    float a = (phaseDeg + k * 360f / sides) * Mathf.Deg2Rad;
                    ring[i][k] = c[i] + s * (Mathf.Cos(a) * r[i].x) + u * (Mathf.Sin(a) * r[i].y);
                }
            }
            for (int i = 0; i < n - 1; i++)
            {
                Vector3 inside = (c[i] + c[i + 1]) * 0.5f;
                bool p0 = r[i].x < 1e-6f && r[i].y < 1e-6f;
                bool p1 = r[i + 1].x < 1e-6f && r[i + 1].y < 1e-6f;
                if (p0 && p1) continue;
                for (int k = 0; k < sides; k++)
                {
                    int k1 = (k + 1) % sides;
                    if (p0) Tri(b, sub, c[i], ring[i + 1][k], ring[i + 1][k1], inside);
                    else if (p1) Tri(b, sub, ring[i][k], ring[i][k1], c[i + 1], inside);
                    else Face(b, sub, ring[i][k], ring[i][k1], ring[i + 1][k1], ring[i + 1][k], inside);
                }
            }
            if (capStart && r[0].x > 1e-6f)
                for (int k = 0; k < sides; k++) Tri(b, sub, c[0], ring[0][k], ring[0][(k + 1) % sides], c[0] + tang[0] * 0.01f);
            if (capEnd && r[n - 1].x > 1e-6f)
                for (int k = 0; k < sides; k++) Tri(b, sub, c[n - 1], ring[n - 1][k], ring[n - 1][(k + 1) % sides], c[n - 1] - tang[n - 1] * 0.01f);
        }

        /// <summary>A loft along Z with round rings: z positions and radii, centred on (x, y).</summary>
        private static void LoftZ(MeshBuilder b, int sub, float x, float y, float[] z, float[] rx, float[] ry, int sides, float phaseDeg, bool caps)
        {
            Vector3[] c = new Vector3[z.Length];
            Vector2[] r = new Vector2[z.Length];
            for (int i = 0; i < z.Length; i++)
            {
                c[i] = new Vector3(x, y, z[i]);
                r[i] = new Vector2(rx[i], ry[i]);
            }
            Loft(b, sub, c, r, sides, phaseDeg, caps, caps, Vector3.up);
        }

        /// <summary>A pointed spike from a base ring to a tip (no base cap unless asked).</summary>
        private static void Spike(MeshBuilder b, int sub, Vector3 from, Vector3 tip, float r, int sides, bool cap = false, float phaseDeg = 90f)
        {
            Loft(b, sub, new[] { from, tip }, new[] { new Vector2(r, r), Vector2.zero }, sides, phaseDeg, cap, false, UpFor(tip - from));
        }

        /// <summary>A crystal shard: pointed at both ends, widest at the waist (0 to 1 along it).</summary>
        private static void Shard(MeshBuilder b, int sub, Vector3 from, Vector3 to, float r, int sides, float waist = 0.4f, float phaseDeg = 45f, float flat = 1f)
        {
            Vector3 mid = Vector3.Lerp(from, to, waist);
            Loft(b, sub, new[] { from, mid, to }, new[] { Vector2.zero, new Vector2(r, r * flat), Vector2.zero }, sides, phaseDeg, false, false, UpFor(to - from));
        }

        private static Vector3 UpFor(Vector3 dir)
        {
            return Mathf.Abs(Nrm(dir).y) > 0.9f ? Vector3.forward : Vector3.up;
        }

        /// <summary>A thin triangular fin with real thickness (8 triangles).</summary>
        private static void Fin(MeshBuilder b, int sub, Vector3 p0, Vector3 p1, Vector3 p2, float thick)
        {
            Vector3 n = Nrm(Vector3.Cross(p1 - p0, p2 - p0)) * (thick * 0.5f);
            Vector3 cen = (p0 + p1 + p2) / 3f;
            Vector3 a0 = p0 + n, a1 = p1 + n, a2 = p2 + n, b0 = p0 - n, b1 = p1 - n, b2 = p2 - n;
            Tri(b, sub, a0, a1, a2, cen);
            Tri(b, sub, b0, b1, b2, cen);
            Face(b, sub, a0, a1, b1, b0, cen);
            Face(b, sub, a1, a2, b2, b1, cen);
            Face(b, sub, a2, a0, b0, b2, cen);
        }

        /// <summary>
        /// A halo: a ring of triangular cross-section around an axis through centre, in the plane given by the rotation
        /// (identity is a ring around the Z axis). segments around, ring radius R, tube radius t.
        /// </summary>
        private static void Halo(MeshBuilder b, int sub, Vector3 centre, Quaternion rot, float R, float t, int segments)
        {
            Vector3[] prev = null;
            Vector3 prevC = Vector3.zero;
            for (int j = 0; j <= segments; j++)
            {
                float a = j * Mathf.PI * 2f / segments;
                Vector3 radial = rot * new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                Vector3 axis = rot * Vector3.forward;
                Vector3 c = centre + radial * R;
                Vector3[] cur = new Vector3[3];
                for (int k = 0; k < 3; k++)
                {
                    float q = (90f + k * 120f) * Mathf.Deg2Rad;
                    cur[k] = c + radial * (Mathf.Sin(q) * t) + axis * (Mathf.Cos(q) * t);
                }
                if (prev != null)
                {
                    Vector3 inside = (c + prevC) * 0.5f;
                    for (int k = 0; k < 3; k++)
                    {
                        int k1 = (k + 1) % 3;
                        Face(b, sub, prev[k], prev[k1], cur[k1], cur[k], inside);
                    }
                }
                prev = cur;
                prevC = c;
            }
        }

        /// <summary>
        /// A curved carapace plate over the top of an axis along Z: an arc from angle a0 to a1 (degrees, 90 is straight
        /// up, measured from +X) of radius (rx, ry), from z0 to z1, double sided, pitched about its front edge so the
        /// rear lifts like a beetle wing. The rear radius grows by flare. Without underside it is one-sided (cheaper).
        /// </summary>
        private static void Plate(MeshBuilder b, int sub, float cx, float cy, float z0, float z1, float rx, float ry, float a0, float a1, int segs, float lift, float flare, bool underside = true)
        {
            Vector3[] front = new Vector3[segs + 1];
            Vector3[] back = new Vector3[segs + 1];
            for (int k = 0; k <= segs; k++)
            {
                float a = Mathf.Lerp(a0, a1, k / (float)segs) * Mathf.Deg2Rad;
                Vector3 d = new Vector3(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry, 0f);
                front[k] = new Vector3(cx, cy, z1) + d;
                back[k] = new Vector3(cx, cy, z0) + d * (1f + flare) + new Vector3(0f, lift, 0f);
            }
            float th = Mathf.Min(rx, ry) * 0.08f + 0.001f;
            for (int k = 0; k < segs; k++)
            {
                Vector3 axisMid = new Vector3(cx, cy + lift * 0.5f, (z0 + z1) * 0.5f);
                Face(b, sub, front[k], front[k + 1], back[k + 1], back[k], axisMid);
                if (!underside) continue;
                // Underside, pulled in by the thickness, faces the axis.
                Vector3 i0 = Vector3.Lerp(front[k], axisMid, th / Mathf.Max(0.001f, rx));
                Vector3 i1 = Vector3.Lerp(front[k + 1], axisMid, th / Mathf.Max(0.001f, rx));
                Vector3 i2 = Vector3.Lerp(back[k + 1], axisMid, th / Mathf.Max(0.001f, rx));
                Vector3 i3 = Vector3.Lerp(back[k], axisMid, th / Mathf.Max(0.001f, rx));
                Vector3 outside = (front[k] + back[k + 1]) - axisMid;
                Face(b, sub, i0, i1, i2, i3, outside);
            }
        }

        /// <summary>A flat strip along a path, facing away from the given inside points (rarity veins, seams).</summary>
        private static void Ribbon(MeshBuilder b, int sub, Vector3[] path, Vector3[] outward, float width)
        {
            for (int i = 0; i < path.Length - 1; i++)
            {
                Vector3 t = path[i + 1] - path[i];
                Vector3 o0 = outward[i], o1 = outward[i + 1];
                Vector3 a0 = Nrm(Vector3.Cross(t, o0)) * (width * 0.5f);
                Vector3 a1 = Nrm(Vector3.Cross(t, o1)) * (width * 0.5f);
                Vector3 inside = (path[i] + path[i + 1]) * 0.5f - Nrm(o0 + o1) * 0.01f;
                Face(b, sub, path[i] - a0, path[i] + a0, path[i + 1] + a1, path[i + 1] - a1, inside);
            }
        }

        /// <summary>A low-poly egg or orb (3 rings, given segments).</summary>
        private static void Egg(MeshBuilder b, int sub, Vector3 c, Vector3 radii, int segments, Quaternion rot)
        {
            b.FixedUV = FixedUv;
            b.Sphere(sub, c, radii, 3, segments, rot);
        }
    }
}
