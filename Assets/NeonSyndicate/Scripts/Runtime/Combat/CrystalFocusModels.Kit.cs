using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// What a moving part of a crystal model does, so the rig knows how to pose it (cores bob and spin, ribs and tines
    /// open, discs spin and leave, pebbles orbit, seeds fan out, facets converge, lids open, rings wobble).
    /// </summary>
    public enum CrystalPartRole
    {
        Body = 0,
        Core = 1,
        Rib = 2,
        Tine = 3,
        Lens = 4,
        Disc = 5,
        Horn = 6,
        Pebble = 7,
        Jaw = 8,
        Seed = 9,
        Facet = 10,
        Prism = 11,
        Lid = 12,
        Ring = 13,
        Glow = 14
    }

    /// <summary>
    /// One part of a procedural crystal model, as plain data: its geometry in its own space (pivot at the origin) and
    /// where it sits on its parent. Part 0 is always the static body; the rest are the moving parts the rig animates.
    /// </summary>
    public sealed class CrystalPartGeometry
    {
        public string Name;
        public CrystalPartRole Role;
        /// <summary>Which rib, seed, facet or lid this is (0 based within its role).</summary>
        public int Index;
        /// <summary>Index of the parent part (-1 only for part 0, the body).</summary>
        public int Parent = -1;
        /// <summary>Pivot position on the parent, in the parent's space.</summary>
        public Vector3 Position;
        public Quaternion Rotation = QMath.Identity;
        /// <summary>The axis this part turns about when it opens, spins or tilts, in its own space.</summary>
        public Vector3 Axis = Vector3.right;
        /// <summary>Role data in the parent's space: the point facets converge on, the core pebbles orbit, and so on.</summary>
        public Vector3 Aux;
        /// <summary>Five submeshes: 0 matte structure, 1 metal, 2 passive crystal, 3 active facets, 4 core.</summary>
        public readonly MeshBuilder Mesh = new MeshBuilder(CrystalFocusModels.SubCount);
        /// <summary>Drawn with the additive glow material (tails, glow cards) instead of the palette.</summary>
        public bool Additive;
        /// <summary>Casts a shadow (off for glow cards and tiny parts).</summary>
        public bool Shadows = true;
    }

    /// <summary>A whole crystal model as plain data: its parts and the emitter and support-hand points.</summary>
    public sealed class CrystalModelGeometry
    {
        public readonly List<CrystalPartGeometry> Parts = new List<CrystalPartGeometry>();
        /// <summary>The emitter point (the "Muzzle" child), model space.</summary>
        public Vector3 Muzzle;
        /// <summary>The support-hand point (the "Foregrip" child), only meaningful when TwoHanded.</summary>
        public Vector3 Foregrip;
        public bool TwoHanded;
        /// <summary>Named anchor points the props expose as empty children (case slots), model space.</summary>
        public readonly List<string> AnchorNames = new List<string>();
        public readonly List<Vector3> AnchorPoints = new List<Vector3>();

        public CrystalModelGeometry()
        {
            CrystalPartGeometry body = new CrystalPartGeometry { Name = "Body", Role = CrystalPartRole.Body };
            Parts.Add(body);
        }

        public CrystalPartGeometry Body { get { return Parts[0]; } }

        /// <summary>Adds a moving part pivoting at pos on the body (or on another part).</summary>
        public CrystalPartGeometry Add(string name, CrystalPartRole role, int index, Vector3 pos, int parent = 0)
        {
            CrystalPartGeometry p = new CrystalPartGeometry { Name = name, Role = role, Index = index, Position = pos, Parent = parent };
            Parts.Add(p);
            return p;
        }

        public void Anchor(string name, Vector3 at)
        {
            AnchorNames.Add(name);
            AnchorPoints.Add(at);
        }
    }

    /// <summary>
    /// The low-poly shape kit the crystal models are built from: lofted shells along a path, crystal shards, gems,
    /// bands along arcs, fins, plates, rings and orbs. Every face is flat shaded and wound outward automatically from an
    /// "inside" point, so shapes can be placed freely. Engine-free (QMath rotations), so the offline harness and the
    /// EditMode tests build every model without Unity.
    /// </summary>
    public static partial class CrystalFocusModels
    {
        /// <summary>Submesh slots shared by every crystal model.</summary>
        public const int SubStructure = 0, SubMetal = 1, SubCrystal = 2, SubActive = 3, SubCore = 4;
        public const int SubCount = 5;

        private static readonly Vector2 FixedUv = new Vector2(0.5f, 0.5f);

        /// <summary>Normalizes by hand: Vector3.normalized returns zero below 1e-5, and model parts are millimetres.</summary>
        private static Vector3 Nrm(Vector3 v)
        {
            float m = v.magnitude;
            return m > 1e-12f ? v / m : Vector3.zero;
        }

        /// <summary>A quad wound so its normal points away from the inside point.</summary>
        private static void Face(MeshBuilder b, int sub, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 inside)
        {
            Vector3 n = Vector3.Cross(p1 - p0, p2 - p0);
            if (n.sqrMagnitude < 1e-16f) n = Vector3.Cross(p2 - p0, p3 - p0);
            Vector3 mid = (p0 + p1 + p2 + p3) * 0.25f;
            if (Vector3.Dot(n, mid - inside) < 0f)
            {
                Vector3 t = p1;
                p1 = p3;
                p3 = t;
                n = -n;
            }
            float m = n.magnitude;
            n = m > 1e-12f ? n / m : Vector3.up;
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
            n = m > 1e-12f ? n / m : Vector3.up;
            b.Triangle(sub, p0, p1, p2, n, FixedUv, FixedUv, FixedUv);
        }

        /// <summary>
        /// A shell lofted through rings: ring i sits at c[i], at right angles to the path, with half sizes r[i] (x along
        /// the side axis, y along the up axis). A ring with zero size is a point, so ends can taper to a tip. upHint
        /// picks the ring's up axis. With four sides, phase 45 gives a rectangle (half sizes times 0.707). jitter (0 to
        /// about 0.2) pushes every ring point in or out by a fixed pseudo-random amount, for hewn stone and mineral.
        /// </summary>
        private static void Loft(MeshBuilder b, int sub, Vector3[] c, Vector2[] r, int sides, float phaseDeg, bool capStart, bool capEnd, Vector3 upHint, float jitter = 0f, int seed = 0)
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
                    float j = jitter > 0f ? 1f + jitter * Hash(i, k, seed) : 1f;
                    ring[i][k] = c[i] + s * (Mathf.Cos(a) * r[i].x * j) + u * (Mathf.Sin(a) * r[i].y * j);
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

        /// <summary>A fixed pseudo-random number from -1 to 1 for a ring point (same every build).</summary>
        private static float Hash(int i, int k, int seed)
        {
            uint h = (uint)(i * 73856093) ^ (uint)(k * 19349663) ^ (uint)(seed * 83492791 + 1);
            h ^= h >> 13;
            h *= 0x5bd1e995u;
            h ^= h >> 15;
            return (h & 0xFFFF) / 32767.5f - 1f;
        }

        /// <summary>A loft along Z with ring centres (x, y) and half sizes per ring.</summary>
        private static void LoftZ(MeshBuilder b, int sub, Vector2[] centre, float[] z, float[] rx, float[] ry, int sides, float phaseDeg, bool caps, float jitter = 0f, int seed = 0)
        {
            Vector3[] c = new Vector3[z.Length];
            Vector2[] r = new Vector2[z.Length];
            for (int i = 0; i < z.Length; i++)
            {
                Vector2 cc = centre[Mathf.Min(i, centre.Length - 1)];
                c[i] = new Vector3(cc.x, cc.y, z[i]);
                r[i] = new Vector2(rx[i], ry[i]);
            }
            Loft(b, sub, c, r, sides, phaseDeg, caps, caps, Vector3.up, jitter, seed);
        }

        private static Vector3 UpFor(Vector3 dir)
        {
            return Mathf.Abs(Nrm(dir).y) > 0.9f ? Vector3.forward : Vector3.up;
        }

        /// <summary>A crystal shard: pointed at both ends, widest at the waist (0 to 1 along it).</summary>
        private static void Shard(MeshBuilder b, int sub, Vector3 from, Vector3 to, float r, int sides, float waist = 0.4f, float phaseDeg = 45f, float flat = 1f)
        {
            Vector3 mid = Vector3.Lerp(from, to, waist);
            Loft(b, sub, new[] { from, mid, to }, new[] { Vector2.zero, new Vector2(r, r * flat), Vector2.zero }, sides, phaseDeg, false, false, UpFor(to - from));
        }

        /// <summary>A pointed spike from a base ring to a tip (base cap optional).</summary>
        private static void Spike(MeshBuilder b, int sub, Vector3 from, Vector3 tip, float r, int sides, bool cap = false, float phaseDeg = 90f)
        {
            Loft(b, sub, new[] { from, tip }, new[] { new Vector2(r, r), Vector2.zero }, sides, phaseDeg, cap, false, UpFor(tip - from));
        }

        /// <summary>A gem: a flat bipyramid around an axis (a faceted disc or lens), radius R, half thickness h.</summary>
        private static void Gem(MeshBuilder b, int sub, Vector3 centre, Vector3 axis, float R, float h, int sides, float phaseDeg, Vector3 upHint)
        {
            Vector3 ax = Nrm(axis);
            Loft(b, sub, new[] { centre - ax * h, centre, centre + ax * h }, new[] { Vector2.zero, new Vector2(R, R), Vector2.zero }, sides, phaseDeg, false, false, upHint);
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

        /// <summary>A flat four-cornered plate with real thickness (12 triangles); faces and rim may use different submeshes.</summary>
        private static void Plate4(MeshBuilder b, int faceSub, int rimSub, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float thick)
        {
            Vector3 n = Nrm(Vector3.Cross(p1 - p0, p2 - p0)) * (thick * 0.5f);
            Vector3 cen = (p0 + p1 + p2 + p3) * 0.25f;
            Face(b, faceSub, p0 + n, p1 + n, p2 + n, p3 + n, cen);
            Face(b, faceSub, p0 - n, p1 - n, p2 - n, p3 - n, cen);
            Face(b, rimSub, p0 + n, p1 + n, p1 - n, p0 - n, cen);
            Face(b, rimSub, p1 + n, p2 + n, p2 - n, p1 - n, cen);
            Face(b, rimSub, p2 + n, p3 + n, p3 - n, p2 - n, cen);
            Face(b, rimSub, p3 + n, p0 + n, p0 - n, p3 - n, cen);
        }

        /// <summary>A box of half sizes h, rotated (faceMask as MeshBuilder.Box: 1 +X, 2 -X, 4 +Y, 8 -Y, 16 +Z, 32 -Z).</summary>
        private static void Box(MeshBuilder b, int sub, Vector3 c, Vector3 size, Quaternion rot, int faceMask = 63)
        {
            b.FixedUV = FixedUv;
            b.Box(sub, c, size, rot, UVMode.Fixed, faceMask);
        }

        /// <summary>
        /// A torus: a ring of triangular cross-section around an axis through centre, in the plane given by the rotation
        /// (identity is a ring around the Z axis). segments around, ring radius R, tube radius t.
        /// </summary>
        private static void Torus(MeshBuilder b, int sub, Vector3 centre, Quaternion rot, float R, float t, int segments)
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

        /// <summary>Points along an arc in the plane of rot (identity: the YZ plane, angle 0 along +Z, 90 along +Y).</summary>
        private static Vector3[] ArcPath(Vector3 centre, Quaternion rot, float R, float a0, float a1, int segments)
        {
            Vector3[] p = new Vector3[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.Lerp(a0, a1, i / (float)segments) * Mathf.Deg2Rad;
                p[i] = centre + rot * new Vector3(0f, Mathf.Sin(a), Mathf.Cos(a)) * R;
            }
            return p;
        }

        /// <summary>A low-poly orb (ellipsoid if radii differ).</summary>
        private static void Orb(MeshBuilder b, int sub, Vector3 c, Vector3 radii, int rings, int segments, Quaternion rot)
        {
            b.FixedUV = FixedUv;
            b.Sphere(sub, c, radii, rings, segments, rot);
        }

        /// <summary>
        /// A cracked orb: the shell of a low-poly sphere with every face shrunk toward its own centre (gap 0 to 1), so a
        /// glowing inner orb shows through the seams. Shell faces are double sided (outer and inner).
        /// </summary>
        private static void CrackedOrb(MeshBuilder b, int shellSub, int coreSub, Vector3 c, float R, int rings, int segments, float gap, float coreScale)
        {
            for (int r = 0; r < rings; r++)
            {
                float t0 = Mathf.PI * r / rings, t1 = Mathf.PI * (r + 1) / rings;
                for (int s = 0; s < segments; s++)
                {
                    float p0 = Mathf.PI * 2f * s / segments, p1 = Mathf.PI * 2f * (s + 1) / segments;
                    float jitter = 1f + 0.06f * Mathf.Sin(r * 3.1f + s * 1.7f);
                    Vector3 a = c + SpherePt(t0, p0) * R * jitter;
                    Vector3 bb = c + SpherePt(t0, p1) * R * jitter;
                    Vector3 cc = c + SpherePt(t1, p1) * R * jitter;
                    Vector3 d = c + SpherePt(t1, p0) * R * jitter;
                    if (r == 0)
                    {
                        Vector3 m = (a + cc + d) / 3f;
                        Tri(b, shellSub, Vector3.Lerp(a, m, gap), Vector3.Lerp(cc, m, gap), Vector3.Lerp(d, m, gap), c);
                    }
                    else if (r == rings - 1)
                    {
                        Vector3 m = (a + bb + d) / 3f;
                        Tri(b, shellSub, Vector3.Lerp(a, m, gap), Vector3.Lerp(bb, m, gap), Vector3.Lerp(d, m, gap), c);
                    }
                    else
                    {
                        Vector3 m = (a + bb + cc + d) * 0.25f;
                        Face(b, shellSub, Vector3.Lerp(a, m, gap), Vector3.Lerp(bb, m, gap), Vector3.Lerp(cc, m, gap), Vector3.Lerp(d, m, gap), c);
                    }
                }
            }
            Orb(b, coreSub, c, Vector3.one * (R * coreScale), Mathf.Max(2, rings - 1), Mathf.Max(4, segments - 2), QMath.Identity);
        }

        private static Vector3 SpherePt(float theta, float phi)
        {
            return new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi));
        }

        /// <summary>
        /// A double-sided card with explicit UVs for the additive glow material (the glow texture is a soft radial dot,
        /// so v from 0.5 at the head to 1 at the end fades along the card). Corners in order around the card.
        /// </summary>
        private static void GlowCard(MeshBuilder b, int sub, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector2 u0, Vector2 u1, Vector2 u2, Vector2 u3)
        {
            Vector3 n = Nrm(Vector3.Cross(p1 - p0, p2 - p0));
            if (n.sqrMagnitude < 0.5f) n = Vector3.up;
            b.Quad(sub, p0, p1, p2, p3, n, u0, u1, u2, u3);
            b.Quad(sub, p3, p2, p1, p0, -n, u3, u2, u1, u0);
        }

        /// <summary>
        /// A tail of two crossed glow cards behind a shot: brightest at zHead, fading in over a short lead in front of it
        /// (so there is no hard edge) and fading out to nothing at zHead - length.
        /// </summary>
        private static void Tail(MeshBuilder b, int sub, float length, float halfWidth, float zHead)
        {
            float lead = Mathf.Min(0.08f, length * 0.15f);
            Vector2 fl = new Vector2(0f, 0.2f), fr = new Vector2(1f, 0.2f), hl = new Vector2(0f, 0.5f), hr = new Vector2(1f, 0.5f), tl = new Vector2(0f, 1f), tr = new Vector2(1f, 1f);
            float w0 = halfWidth, w1 = halfWidth * 0.25f;
            for (int k = 0; k < 2; k++)
            {
                Vector3 side = k == 0 ? Vector3.right : Vector3.up;
                Vector3 f0 = -side * w0 + new Vector3(0f, 0f, zHead + lead), f1 = side * w0 + new Vector3(0f, 0f, zHead + lead);
                Vector3 h0 = -side * w0 + new Vector3(0f, 0f, zHead), h1 = side * w0 + new Vector3(0f, 0f, zHead);
                Vector3 t0 = -side * w1 + new Vector3(0f, 0f, zHead - length), t1 = side * w1 + new Vector3(0f, 0f, zHead - length);
                GlowCard(b, sub, f0, f1, h1, h0, fl, fr, hr, hl);
                GlowCard(b, sub, h0, h1, t1, t0, hl, hr, tr, tl);
            }
        }

        /// <summary>A flat camera-agnostic glow star: three crossed square cards around a point (full soft dot each).</summary>
        private static void GlowStar(MeshBuilder b, int sub, Vector3 c, float half)
        {
            Vector2 a = new Vector2(0f, 0f), bb = new Vector2(1f, 0f), cc = new Vector2(1f, 1f), d = new Vector2(0f, 1f);
            GlowCard(b, sub, c + new Vector3(-half, -half, 0f), c + new Vector3(half, -half, 0f), c + new Vector3(half, half, 0f), c + new Vector3(-half, half, 0f), a, bb, cc, d);
            GlowCard(b, sub, c + new Vector3(0f, -half, -half), c + new Vector3(0f, -half, half), c + new Vector3(0f, half, half), c + new Vector3(0f, half, -half), a, bb, cc, d);
            GlowCard(b, sub, c + new Vector3(-half, 0f, -half), c + new Vector3(half, 0f, -half), c + new Vector3(half, 0f, half), c + new Vector3(-half, 0f, half), a, bb, cc, d);
        }

        /// <summary>
        /// A hand grip at the origin: an organic handle raked slightly back with a palm swell and finger ridges, the
        /// same envelope as the loot guns so the hand pose fits (top y 0.045, heel y -0.062).
        /// </summary>
        private static void GripShape(MeshBuilder b, int sub, int sides, float phase, float hx, float hz, float ridge)
        {
            Vector3 top = new Vector3(0f, 0.045f, 0.004f), bot = new Vector3(0f, -0.062f, -0.026f);
            Vector3 swell = new Vector3(0f, 0f, 0.013f);
            Vector3[] c =
            {
                top,
                Vector3.Lerp(top, bot, 0.25f) + swell * 0.7f,
                Vector3.Lerp(top, bot, 0.45f) + swell,
                Vector3.Lerp(top, bot, 0.65f) + swell * 0.8f,
                Vector3.Lerp(top, bot, 0.85f) + swell * 0.3f,
                bot
            };
            Vector2[] r =
            {
                new Vector2(hx * 0.9f, hz * 0.9f),
                new Vector2(hx * (1.05f + ridge), hz * (1.08f + ridge)),
                new Vector2(hx * 1.0f, hz * 1.02f),
                new Vector2(hx * (1.08f + ridge), hz * (1.15f + ridge)),
                new Vector2(hx * 1.02f, hz * 1.2f),
                new Vector2(hx * 0.92f, hz * 1.12f)
            };
            Loft(b, sub, c, r, sides, phase, false, true, Vector3.forward);
        }
    }
}
