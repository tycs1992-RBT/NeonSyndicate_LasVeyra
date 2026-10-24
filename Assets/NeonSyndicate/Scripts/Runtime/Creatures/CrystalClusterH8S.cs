using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One shard of a cluster: a hexagonal prism that tapers into a sharp tip. Positions and the direction are in
    /// whatever frame the planner was given (the cluster's local frame, or model space after Place).</summary>
    public struct CrystalPrismH8S
    {
        /// <summary>Centre of the base ring.</summary>
        public Vector3 Base;
        /// <summary>Unit growth direction.</summary>
        public Vector3 Dir;
        public float Length;
        public float Radius;
        /// <summary>Fraction of the length taken by the pointed tip.</summary>
        public float TipFraction;
        /// <summary>Spin of the hexagon around its axis, radians.</summary>
        public float Twist;
        /// <summary>Palette shade of the body: 0 deep (the small root shards), 1 mid (the leader and the long shards). Tips take the next lighter shade.</summary>
        public int Shade;
    }

    /// <summary>
    /// Plans and meshes the crystal shard clusters of the H8S creature look: a leader shard with shorter companions
    /// fanning out around it at varied angles and sizes (never a uniform comb), each a six sided prism with a sharp tip,
    /// grown out of a low basalt socket. Engine-free and deterministic from a seed, so the builder (worker threads), the
    /// boss regalia, the part shards and the offline preview all agree. Frame convention for Plan: the cluster's local
    /// frame, X and Y along the surface, Z out of the surface; Place maps it onto a surface point.
    /// </summary>
    public static class CrystalClusterH8S
    {
        /// <summary>Hexagonal prisms throughout: the reference's shards are six sided.</summary>
        public const int Sides = 6;
        /// <summary>Shortest companion as a fraction of the cluster size.</summary>
        public const float MinLength = 0.3f;
        /// <summary>Companions tilt away from the leader by at least this many degrees.</summary>
        public const float MinFanDegrees = 14f;
        public const float MaxFanDegrees = 42f;

        /// <summary>
        /// Plans count shards (1 to 12) for a cluster of the given size (the leader's length, metres) spread over a disc of
        /// this radius on the surface. lean tilts the whole cluster (radians) toward the local X axis (callers pass the
        /// body's rake direction). Writes into the array and returns the number planned.
        /// </summary>
        public static int Plan(uint seed, int count, float size, float spread, float lean, CrystalPrismH8S[] into)
        {
            return Plan(seed, count, size, spread, lean, LeaderWanderDegrees, into);
        }

        /// <summary>How far the leader may wander off the leaned normal (degrees) in the beasts' clusters.</summary>
        public const float LeaderWanderDegrees = 8f;

        /// <summary>Plan with a chosen leader wander: the boss regalia keeps its hero shards nearly straight (structure, not noise).</summary>
        public static int Plan(uint seed, int count, float size, float spread, float lean, float wanderDegrees, CrystalPrismH8S[] into)
        {
            count = Mathf.Clamp(count, 1, Mathf.Min(12, into.Length));
            size = Mathf.Max(0.001f, size);
            spread = Mathf.Max(0f, spread);
            uint s = seed == 0u ? 0x9E3779B9u : seed;
            float leanX = Mathf.Sin(lean), leanZ = Mathf.Cos(lean);
            // The leader: longest, thickest, nearly central, pointing along the leaned normal with a little wander.
            CrystalPrismH8S leader = new CrystalPrismH8S();
            leader.Length = size * Range(ref s, 0.95f, 1f);
            leader.Radius = size * Range(ref s, 0.15f, 0.19f);
            float a0 = Range(ref s, 0f, Mathf.PI * 2f);
            leader.Base = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * (spread * Range(ref s, 0f, 0.18f));
            leader.Dir = Tilt(new Vector3(leanX, 0f, leanZ), Range(ref s, 0f, Mathf.Max(0f, wanderDegrees)), Range(ref s, 0f, Mathf.PI * 2f));
            leader.TipFraction = Range(ref s, 0.3f, 0.42f);
            leader.Twist = Range(ref s, 0f, Mathf.PI * 2f);
            leader.Shade = 1;
            into[0] = leader;
            // Companions step down in length from the leader to MinLength in equal shares (with a little jitter that never
            // reorders them), so no two shards in a cluster are the same length.
            float step = (1f - MinLength) / count;
            float golden = 2.39996f;
            float ang = a0 + Mathf.PI + Range(ref s, -0.4f, 0.4f);
            for (int i = 1; i < count; i++)
            {
                float length = size * (1f - step * i) * Range(ref s, 0.96f, 1f);
                CrystalPrismH8S p = new CrystalPrismH8S();
                p.Length = length;
                p.Radius = Mathf.Max(0.0005f, Mathf.Pow(length / size, 0.8f) * size * Range(ref s, 0.16f, 0.22f));
                ang += golden + Range(ref s, -0.35f, 0.35f);
                float dist = spread * Range(ref s, 0.35f, 1f);
                Vector3 radial = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                p.Base = radial * dist;
                // Fan: away from the leader, further for the outer, shorter shards, plus a wander around that line.
                float fan = Mathf.Lerp(MinFanDegrees, MaxFanDegrees, Mathf.Clamp01(dist / Mathf.Max(0.001f, spread))) + Range(ref s, 0f, 6f);
                Vector3 away = Vector3.Lerp(leader.Dir, radial, Mathf.Sin(fan * Mathf.Deg2Rad));
                p.Dir = Tilt(Nrm(away), Range(ref s, 0f, 7f), Range(ref s, 0f, Mathf.PI * 2f));
                if (Vector3.Dot(p.Dir, leader.Dir) > Mathf.Cos(MinFanDegrees * Mathf.Deg2Rad)) p.Dir = Nrm(Vector3.Lerp(leader.Dir, radial, Mathf.Sin((MinFanDegrees + 2f) * Mathf.Deg2Rad)));
                p.TipFraction = Range(ref s, 0.32f, 0.5f);
                p.Twist = Range(ref s, 0f, Mathf.PI * 2f);
                p.Shade = length > size * 0.6f ? 1 : 0;
                into[i] = p;
            }
            return count;
        }

        /// <summary>Maps a planned prism from the cluster frame (X, Y on the surface, Z outward) onto a surface point with that basis.</summary>
        public static CrystalPrismH8S Place(CrystalPrismH8S p, Vector3 origin, Vector3 x, Vector3 y, Vector3 z)
        {
            CrystalPrismH8S r = p;
            r.Base = origin + x * p.Base.x + y * p.Base.y + z * p.Base.z;
            r.Dir = Nrm(x * p.Dir.x + y * p.Dir.y + z * p.Dir.z);
            return r;
        }

        /// <summary>Vertices one prism adds (Faces): six side quads and six tip triangles, flat shaded.</summary>
        public const int PrismVertices = Sides * 7;
        public const int PrismTriangles = Sides * 3;

        /// <summary>
        /// Appends the faces of a prism in its frame: flat shaded (every face its own vertices), the base ring sunk a little
        /// so it reads as grown from the surface, irregular by jag (0 is a perfect hexagon). kind gets 0 for body vertices
        /// and 1 for the tip triangles, so a caller can paint the tip a lighter shade. Triangles are wound clockwise seen
        /// from outside (Unity's front face).
        /// </summary>
        public static void Faces(CrystalPrismH8S p, uint seed, float jag, List<Vector3> v, List<Vector3> n, List<int> kind, List<int> tris)
        {
            Faces(p, seed, jag, false, v, n, kind, tris);
        }

        /// <summary>
        /// Faces with the mirror flag of a twin: the frame's reference axes lie in the body's mirror plane (up, or forward
        /// for a near vertical shard) and the across axis is negated, so a prism whose base and direction are the mirror
        /// image of another's, built with the same seed, comes out as its exact mirror image, jag for jag.
        /// </summary>
        public static void Faces(CrystalPrismH8S p, uint seed, float jag, bool mirror, List<Vector3> v, List<Vector3> n, List<int> kind, List<int> tris)
        {
            Vector3 dir = Nrm(p.Dir);
            if (dir.sqrMagnitude < 0.5f) dir = Vector3.up;
            Vector3 u = Vector3.Cross(dir, Mathf.Abs(dir.y) < 0.9f ? Vector3.up : Vector3.forward);
            u = Nrm(u);
            Vector3 w = Vector3.Cross(dir, u);
            if (mirror) u = -u;
            uint s = seed == 0u ? 0x51ED27u : seed;
            float bodyLen = p.Length * (1f - Mathf.Clamp(p.TipFraction, 0.05f, 0.9f));
            Vector3[] baseRing = new Vector3[Sides], topRing = new Vector3[Sides];
            for (int k = 0; k < Sides; k++)
            {
                float a = p.Twist + Mathf.PI * 2f * k / Sides + Range(ref s, -jag, jag) * 0.5f;
                float rr = p.Radius * (1f + Range(ref s, -jag, jag));
                Vector3 radial = u * (Mathf.Cos(a) * rr) + w * (Mathf.Sin(a) * rr);
                baseRing[k] = p.Base + radial - dir * (p.Radius * 0.25f);
                topRing[k] = p.Base + dir * (bodyLen * (1f + Range(ref s, -jag, jag) * 0.35f)) + radial * 0.84f;
            }
            Vector3 tip = p.Base + dir * p.Length + (u * Range(ref s, -1f, 1f) + w * Range(ref s, -1f, 1f)) * (p.Radius * 0.1f);
            Vector3 axisMid = p.Base + dir * (bodyLen * 0.5f);
            for (int k = 0; k < Sides; k++)
            {
                int k1 = (k + 1) % Sides;
                Vector3 a = baseRing[k], b = baseRing[k1], c = topRing[k1], d = topRing[k];
                Vector3 fn = Nrm(Vector3.Cross(b - a, d - a));
                if (Vector3.Dot(fn, (a + b + c + d) * 0.25f - axisMid) < 0f) fn = -fn;
                int i0 = v.Count;
                v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                n.Add(fn); n.Add(fn); n.Add(fn); n.Add(fn);
                kind.Add(0); kind.Add(0); kind.Add(0); kind.Add(0);
                AddTri(v, tris, fn, i0, i0 + 1, i0 + 2);
                AddTri(v, tris, fn, i0, i0 + 2, i0 + 3);
                Vector3 tn = Nrm(Vector3.Cross(c - d, tip - d));
                if (Vector3.Dot(tn, (c + d + tip) / 3f - (p.Base + dir * bodyLen)) < 0f) tn = -tn;
                int j0 = v.Count;
                v.Add(d); v.Add(c); v.Add(tip);
                n.Add(tn); n.Add(tn); n.Add(tn);
                kind.Add(1); kind.Add(1); kind.Add(1);
                AddTri(v, tris, tn, j0, j0 + 1, j0 + 2);
            }
        }

        /// <summary>Vertices the socket adds: a ring of side quads, four per side.</summary>
        public static int SocketVertices(int sides) { return Mathf.Max(5, sides) * 4; }

        /// <summary>
        /// A low, irregular basalt mound the shards grow out of: a truncated cone from a ring just under the surface to a
        /// smaller raised ring (open on top, the shards fill it). In the cluster frame (Z out). kind is 2 for every vertex.
        /// </summary>
        public static void Socket(uint seed, int sides, float radius, float height, List<Vector3> v, List<Vector3> n, List<int> kind, List<int> tris)
        {
            sides = Mathf.Clamp(sides, 5, 10);
            uint s = seed == 0u ? 0x2F6B1u : seed;
            Vector3[] lo = new Vector3[sides], hi = new Vector3[sides];
            float spin = Range(ref s, 0f, Mathf.PI * 2f);
            for (int k = 0; k < sides; k++)
            {
                float a = spin + Mathf.PI * 2f * k / sides + Range(ref s, -0.12f, 0.12f);
                float rl = radius * Range(ref s, 0.9f, 1.12f), rh = radius * Range(ref s, 0.5f, 0.66f);
                lo[k] = new Vector3(Mathf.Cos(a) * rl, Mathf.Sin(a) * rl, -height * 0.4f);
                hi[k] = new Vector3(Mathf.Cos(a) * rh, Mathf.Sin(a) * rh, height * Range(ref s, 0.8f, 1.1f));
            }
            for (int k = 0; k < sides; k++)
            {
                int k1 = (k + 1) % sides;
                Vector3 a = lo[k], b = lo[k1], c = hi[k1], d = hi[k];
                Vector3 fn = Nrm(Vector3.Cross(b - a, d - a));
                if (Vector3.Dot(fn, new Vector3((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f, 0f)) < 0f) fn = -fn;
                int i0 = v.Count;
                v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                n.Add(fn); n.Add(fn); n.Add(fn); n.Add(fn);
                kind.Add(2); kind.Add(2); kind.Add(2); kind.Add(2);
                AddTri(v, tris, fn, i0, i0 + 1, i0 + 2);
                AddTri(v, tris, fn, i0, i0 + 2, i0 + 3);
            }
        }

        /// <summary>
        /// A whole cluster as flat triangles with one palette cell per vertex, in the frame of a surface point: the socket
        /// (when asked) in socketUV, the shards in shadeUV[shade] with their tips one shade lighter. Appends to the lists;
        /// triangles are wound for Unity. The boss regalia, the part shards and the offline boss preview share it.
        /// </summary>
        public static void Cluster(Vector3 at, Vector3 normal, Vector3 rake, float size, float spread, int shards, float lean, uint seed, bool socket,
            Vector2 socketUV, Vector2[] shadeUV, List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> tris)
        {
            Cluster(at, normal, rake, size, spread, shards, lean, seed, socket, socketUV, shadeUV, 1f, LeaderWanderDegrees, false, v, n, uv, tris);
        }

        /// <summary>
        /// The whole-cluster writer with the regalia's extra controls: thickness scales every shard's radius (slender bosses
        /// wear slender shards), wanderDegrees bounds the leader's wander, and mirror builds the exact mirror image of the
        /// same seed (a right piece whose position, normal and rake are the left piece's mirrored comes out as its twin).
        /// </summary>
        public static void Cluster(Vector3 at, Vector3 normal, Vector3 rake, float size, float spread, int shards, float lean, uint seed, bool socket,
            Vector2 socketUV, Vector2[] shadeUV, float thickness, float wanderDegrees, bool mirror, List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> tris)
        {
            Vector3 z = Nrm(normal);
            if (z.sqrMagnitude < 0.5f) z = Vector3.up;
            Vector3 x = rake - z * Vector3.Dot(rake, z);
            if (x.sqrMagnitude < 1e-8f) x = Vector3.Cross(z, Mathf.Abs(z.y) < 0.9f ? Vector3.up : Vector3.right);
            x = Nrm(x);
            Vector3 y = Vector3.Cross(z, x);
            // A mirrored frame is left handed: the planned shards land as the mirror image, and the socket's winding flips.
            if (mirror) y = -y;
            CrystalPrismH8S[] prisms = new CrystalPrismH8S[12];
            int count = Plan(seed, shards, size, spread, lean, wanderDegrees, prisms);
            if (thickness != 1f)
                for (int i = 0; i < count; i++) prisms[i].Radius *= Mathf.Max(0.1f, thickness);
            List<Vector3> lv = new List<Vector3>(64), ln = new List<Vector3>(64);
            List<int> lk = new List<int>(64), lt = new List<int>(128);
            Vector3 origin = at - z * (size * 0.04f);
            if (socket)
            {
                Socket(seed ^ 0x5bd1e995u, 7, spread * 1.25f + size * 0.12f, size * 0.1f, lv, ln, lk, lt);
                Append(lv, ln, lk, lt, origin, x, y, z, socketUV, socketUV, socketUV, mirror, v, n, uv, tris);
            }
            for (int i = 0; i < count; i++)
            {
                lv.Clear(); ln.Clear(); lk.Clear(); lt.Clear();
                CrystalPrismH8S p = Place(prisms[i], origin, x, y, z);
                // Faces builds in the placed frame with its own cross products and winds along the normal, so no flip here.
                Faces(p, seed + (uint)(i * 7919), 0.14f, mirror, lv, ln, lk, lt);
                int shade = Mathf.Clamp(p.Shade, 0, 2);
                Append(lv, ln, lk, lt, Vector3.zero, Vector3.right, Vector3.up, Vector3.forward, shadeUV[shade], shadeUV[Mathf.Min(2, shade + 1)], socketUV, false, v, n, uv, tris);
            }
        }

        private static void Append(List<Vector3> lv, List<Vector3> ln, List<int> lk, List<int> lt, Vector3 origin, Vector3 x, Vector3 y, Vector3 z,
            Vector2 bodyUV, Vector2 tipUV, Vector2 socketUV, bool flip, List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> tris)
        {
            int first = v.Count;
            for (int i = 0; i < lv.Count; i++)
            {
                Vector3 lp = lv[i], lnn = ln[i];
                v.Add(origin + x * lp.x + y * lp.y + z * lp.z);
                n.Add(Nrm(x * lnn.x + y * lnn.y + z * lnn.z));
                int k = lk[i];
                uv.Add(k == 1 ? tipUV : (k == 2 ? socketUV : bodyUV));
            }
            if (flip)
                for (int i = 0; i + 2 < lt.Count; i += 3) { tris.Add(first + lt[i]); tris.Add(first + lt[i + 2]); tris.Add(first + lt[i + 1]); }
            else
                for (int i = 0; i < lt.Count; i++) tris.Add(first + lt[i]);
        }

        /// <summary>The greatest angle in degrees between any two shard directions (a measure of how much a cluster fans).</summary>
        public static float FanDegrees(CrystalPrismH8S[] prisms, int count)
        {
            float worst = 0f;
            for (int i = 0; i < count; i++)
                for (int j = i + 1; j < count; j++)
                {
                    float d = Mathf.Acos(Mathf.Clamp(Vector3.Dot(Nrm(prisms[i].Dir), Nrm(prisms[j].Dir)), -1f, 1f)) * Mathf.Rad2Deg;
                    if (d > worst) worst = d;
                }
            return worst;
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>Adds a triangle wound so its face points along the given normal (Unity front faces are clockwise).</summary>
        private static void AddTri(List<Vector3> v, List<int> tris, Vector3 fn, int a, int b, int c)
        {
            Vector3 cross = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
            if (cross.sqrMagnitude < 1e-16f) return;
            if (Vector3.Dot(cross, fn) < 0f) { tris.Add(a); tris.Add(c); tris.Add(b); }
            else { tris.Add(a); tris.Add(b); tris.Add(c); }
        }

        /// <summary>Tilts a unit direction by degrees around a perpendicular axis chosen by phase (radians).</summary>
        private static Vector3 Tilt(Vector3 dir, float degrees, float phase)
        {
            Vector3 d = Nrm(dir);
            Vector3 u = Vector3.Cross(d, Mathf.Abs(d.y) < 0.9f ? Vector3.up : Vector3.right);
            if (u.sqrMagnitude < 1e-8f) u = Vector3.right;
            u = Nrm(u);
            Vector3 w = Vector3.Cross(d, u);
            Vector3 side = u * Mathf.Cos(phase) + w * Mathf.Sin(phase);
            float t = degrees * Mathf.Deg2Rad;
            return Nrm(d * Mathf.Cos(t) + side * Mathf.Sin(t));
        }

        private static float Next(ref uint s)
        {
            s ^= s << 13;
            s ^= s >> 17;
            s ^= s << 5;
            return (s & 0xFFFFFF) / 16777216f;
        }

        private static float Range(ref uint s, float a, float b) { return a + (b - a) * Next(ref s); }

        private static Vector3 Nrm(Vector3 v)
        {
            float m = Mathf.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
            return m > 1e-20f ? v / m : Vector3.zero;
        }
    }
}
