using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The small plans. The Hollow Shade: a floating knot of smoke (a lumpy core wrapped in puffs and rising wisps) with
    /// one burning eye, splinters of smoky crystal caught in it and six or seven long tendrils hanging below, each a
    /// chain of five bones under the Core bone (CreatureSkeleton.Tendrils holds the first bone of each chain). The Glass
    /// Mite: a palm-sized faceted crystal insect (abdomen, thorax, head, six legs and two glass wings, under 300
    /// vertices) on a Body bone with two wing bones (Wing.L, Wing.R).
    /// </summary>
    internal sealed partial class CreatureSculptor
    {
        // ================================================================== hollow shade

        private void BuildWisp()
        {
            float s = _s;
            float size = Mathf.Max(0.2f, _k.BodyLength * s);
            float rc = 0.24f * size * Mathf.Clamp(_k.Bulk, 0.6f, 1.5f);
            float hc = Mathf.Max(rc * 1.5f, _k.ShoulderHeight * s);
            int tendrils = Mathf.Clamp(_k.Tendrils, 3, 10);
            _tile = 0.08f * Mathf.Sqrt(size);
            Vector2 smoke = Pal(_k.Hide), smoke2 = Pal(_k.Accent), glow = Pal(_k.Glow), crystal = Pal(_k.Crystal), black = Pal(new Vector2Int(15, 9));
            int root = Bone("Root", -1, Vector3.zero);
            _sk.Root = root;
            Vector3 c = new Vector3(0f, hc, 0f);
            int core = Bone("Core", root, c);
            _sk.Head = core;
            _sk.Chest = core;
            _sk.Spine = new[] { core };
            // ---- tendril skeleton: chains hanging from the underside, spread around and trailing a little behind
            int bonesPer = 5;
            int[] firsts = new int[tendrils];
            Vector3[][] paths = new Vector3[tendrils][];
            int[][] chains = new int[tendrils][];
            for (int k = 0; k < tendrils; k++)
            {
                float psi = (k + 0.5f) / tendrils * Mathf.PI * 2f + Rand(-0.2f, 0.2f);
                Vector3 radial = new Vector3(Mathf.Sin(psi), 0f, Mathf.Cos(psi));
                Vector3 start = c + radial * (rc * 0.62f) + Vector3.down * (rc * 0.55f);
                float len = size * Rand(0.78f, 0.98f);
                Vector3[] pts = new Vector3[5];
                for (int j = 0; j < 5; j++)
                {
                    float u = j / 4f;
                    // Down, drifting outward and back, with a slow S.
                    Vector3 off = Vector3.down * (len * u * 0.92f) + radial * (rc * 0.5f * u) + Vector3.back * (len * 0.22f * u * u) + Vector3.Cross(radial, Vector3.up) * (0.06f * len * Mathf.Sin(u * 5f + k));
                    pts[j] = start + off;
                }
                pts[4].y = Mathf.Max(pts[4].y, 0.12f * size);
                paths[k] = pts;
                chains[k] = new int[bonesPer];
                int parent = core;
                for (int j = 0; j < bonesPer; j++)
                {
                    float u = j / (float)bonesPer;
                    Vector3 bp = PathPoint(pts, u);
                    chains[k][j] = Bone("Tendril" + (k + 1) + "." + (j + 1), parent, bp);
                    parent = chains[k][j];
                }
                firsts[k] = chains[k][0];
            }
            _sk.Tendrils = firsts;
            // ---- the core: a lumpy ball of smoke
            BeginCore();
            Vector3[] cc = { c + Vector3.down * rc, c + Vector3.down * (rc * 0.3f), c + Vector3.up * (rc * 0.3f), c + Vector3.up * rc };
            float[] ck =
            {
                0f, 0.05f * rc, 0.05f * rc, 0.05f * rc, 2f,
                0.12f, 0.62f * rc, 0.62f * rc, 0.62f * rc, 2f,
                0.5f, 1f * rc, 1f * rc, 1f * rc, 2f,
                0.86f, 0.7f * rc, 0.7f * rc, 0.7f * rc, 2f,
                1f, 0.05f * rc, 0.05f * rc, 0.05f * rc, 2f
            };
            CreatureTube ball = new CreatureTube(cc, Vector3.forward, ck);
            float seed = Rand(0f, 10f);
            ball.Bump = (t, th) =>
            {
                float a = th * Mathf.Deg2Rad;
                float lump = Mathf.Sin(a * 3f + t * 9f + seed) * 0.5f + Mathf.Sin(a * 5f - t * 13f + seed * 2f) * 0.3f + Mathf.Sin(a * 2f + t * 21f) * 0.2f;
                return lump * 0.12f * rc * Mathf.Sin(Mathf.PI * t);
            };
            ball.Weight = (t, th) => W1(core);
            EmitTube(ball, RingsEased(0f, 1f, Segs(18, 12, 8), 0.3f), Segs(26, 18, 10), (t, th) => smoke, null, true, true);
            // Puffs around it, and a few rising wisps of smoke curling back.
            int puffs = Segs(9, 6, 3);
            for (int i = 0; i < puffs; i++)
            {
                float a = Rand(0f, Mathf.PI * 2f), e = Rand(-0.6f, 0.9f);
                Vector3 dir = new Vector3(Mathf.Cos(a) * Mathf.Cos(e), Mathf.Sin(e), Mathf.Sin(a) * Mathf.Cos(e));
                if (dir.z > 0.6f && Mathf.Abs(dir.y) < 0.4f) dir = Nrm(dir + Vector3.back * 0.9f);
                float pr = rc * Rand(0.32f, 0.5f);
                EmitEllipsoid(c + dir * (rc * 0.78f), new Vector3(pr, pr * 0.8f, pr), QMath.AngleAxis(Rand(0f, 360f), Vector3.up), Segs(6, 5, 4), Segs(9, 7, 5), i % 3 == 0 ? smoke2 : smoke, W1(core));
            }
            EndCore();
            int wisps = Segs(5, 3, 2);
            for (int i = 0; i < wisps; i++)
            {
                float a = (i - (wisps - 1) * 0.5f) * 0.5f;
                Vector3 basePos = c + new Vector3(Mathf.Sin(a) * rc * 0.5f, rc * 0.75f, -rc * 0.1f);
                float wl = rc * Rand(1.3f, 2f);
                float curl = Rand(0.6f, 1.2f);
                float phase0 = Rand(0f, 6f);
                Func<float, float, Vector3> P = (u, v) =>
                {
                    float x = (v - 0.5f) * 2f;
                    float width = rc * 0.32f * (1f - u) * (1f - 0.3f * u);
                    Vector3 along = new Vector3(Mathf.Sin(u * 4f + phase0) * 0.25f * wl * u, wl * u * 0.85f, -wl * u * u * 0.7f * curl);
                    return basePos + along + new Vector3(x * width, 0f, 0f);
                };
                EmitSheet(Segs(7, 5, 3), 1, P, (u, v) => W1(core), Vector3.forward, 0.004f * size, smoke2, smoke2);
            }
            // The eye: an almond of light with a dark slit, set in the front of the smoke.
            Vector3 eyePos = c + new Vector3(0f, rc * 0.12f, rc * 0.92f);
            EmitEllipsoid(eyePos, new Vector3(0.36f, 0.17f, 0.1f) * rc, QMath.Identity, Segs(8, 6, 4), Segs(14, 10, 8), glow, W1(core));
            EmitEllipsoid(eyePos + new Vector3(0f, 0f, 0.07f * rc), new Vector3(0.05f, 0.15f, 0.04f) * rc, QMath.Identity, 4, 8, black, W1(core));
            // Splinters of smoky crystal caught in the smoke.
            int shards = Segs(7, 5, 3);
            for (int i = 0; i < shards; i++)
            {
                float a = Rand(0f, Mathf.PI * 2f), e = Rand(-0.3f, 1f);
                Vector3 dir = new Vector3(Mathf.Cos(a) * Mathf.Cos(e), Mathf.Sin(e), Mathf.Sin(a) * Mathf.Cos(e));
                if (dir.z > 0.55f) dir = Nrm(dir + Vector3.back);
                EmitCrystal(c + dir * (rc * 0.85f), Nrm(dir + Vector3.up * 0.3f), Vector3.up, rc * Rand(0.45f, 0.75f), rc * 0.1f, 4, 0.4f, crystal, W1(core));
            }
            // ---- tendrils: tapering tubes with a glowing seam and a burning tip
            for (int k = 0; k < tendrils; k++)
            {
                Vector3[] pts = paths[k];
                int[] ch = chains[k];
                float r0 = 0.11f * rc;
                CreatureTube tt = new CreatureTube(new[] { pts[0] + (c - pts[0]) * 0.25f, pts[1], pts[2], pts[3], pts[4] }, Vector3.forward, new float[]
                {
                    0f, r0 * 1.6f, r0 * 1.6f, r0 * 1.6f, 2f,
                    0.15f, r0, r0, r0, 2f,
                    0.6f, r0 * 0.6f, r0 * 0.6f, r0 * 0.6f, 2f,
                    0.95f, r0 * 0.22f, r0 * 0.22f, r0 * 0.22f, 2f,
                    1f, r0 * 0.08f, r0 * 0.08f, r0 * 0.08f, 2f
                });
                tt.Weight = (t, th) =>
                {
                    float x = Mathf.Clamp(t * bonesPer - 0.5f, 0f, bonesPer - 1.001f);
                    int i = Mathf.FloorToInt(x);
                    return W2(ch[i], ch[Mathf.Min(bonesPer - 1, i + 1)], x - i);
                };
                EmitTube(tt, Rings(0f, 1f, Segs(16, 10, 6)), Segs(9, 7, 5), (t, th) => t > 0.9f ? glow : smoke, null, false, true);
                if (_detail > 0) EmitRibbon(tt, 0.12f, 0.92f, t => 90f + 40f * Mathf.Sin(t * 6f + k), r0 * 0.45f, Segs(12, 8, 0), glow, 0.0015f);
                Hit(ch[0], pts[0], PathPoint(pts, 0.45f), r0 * 1.6f);
            }
            Hit(core, c, c, rc * 1.08f);
            Hit(core, eyePos, eyePos, 0.3f * rc, true);
            _aim = c;
            _headPoint = eyePos;
            _mouthPoint = eyePos + new Vector3(0f, -0.25f * rc, 0.08f * rc);
            _poseGrow = new Vector3(size * 0.9f, size * 0.6f, size * 0.9f);
        }

        /// <summary>Point at u (0..1) along a polyline, by equal spans.</summary>
        private static Vector3 PathPoint(Vector3[] pts, float u)
        {
            float x = Mathf.Clamp01(u) * (pts.Length - 1);
            int i = Mathf.Clamp((int)x, 0, pts.Length - 2);
            return Vector3.Lerp(pts[i], pts[i + 1], x - i);
        }

        // ================================================================== glass mite

        private void BuildMite()
        {
            float s = _s;
            float len = Mathf.Max(0.01f, _k.BodyLength * s);
            float h = Mathf.Max(len * 0.2f, _k.ShoulderHeight * s);
            _tile = 0.02f;
            Vector2 body = Pal(_k.Hide), dark = Pal(_k.Accent), glow = Pal(_k.Glow), wing = Pal(_k.Membrane);
            int root = Bone("Root", -1, Vector3.zero);
            _sk.Root = root;
            Vector3 c = new Vector3(0f, h, 0f);
            int b = Bone("Body", root, c);
            _sk.Chest = b;
            _sk.Head = b;
            _sk.Spine = new[] { b };
            int wl = Bone("Wing.L", b, c + new Vector3(-0.06f * len, 0.16f * len, 0.05f * len));
            int wr = Bone("Wing.R", b, c + new Vector3(0.06f * len, 0.16f * len, 0.05f * len));
            BeginCore();
            // Abdomen, thorax and head: faceted bipyramids of crystal.
            Bipyramid(c + new Vector3(0f, 0.02f * len, -0.22f * len), Vector3.forward, 0.62f * len, 0.2f * len, 5, body, W1(b));
            Bipyramid(c + new Vector3(0f, 0.03f * len, 0.12f * len), Vector3.forward, 0.3f * len, 0.13f * len, 4, body, W1(b));
            Vector3 head = c + new Vector3(0f, 0.02f * len, 0.33f * len);
            Bipyramid(head, Nrm(new Vector3(0f, -0.2f, 1f)), 0.2f * len, 0.1f * len, 4, dark, W1(b));
            EndCore();
            for (int side = -1; side <= 1; side += 2)
                Bipyramid(head + new Vector3(side * 0.06f * len, 0.03f * len, 0.03f * len), Vector3.right * side, 0.06f * len, 0.035f * len, 3, glow, W1(b));
            // Six legs: thin three-sided struts with a knee, splayed out and down to the ground.
            for (int i = 0; i < 6; i++)
            {
                int side = i < 3 ? -1 : 1;
                int row = i % 3;
                float z = (0.18f - row * 0.14f) * len;
                Vector3 hip = c + new Vector3(side * 0.08f * len, -0.02f * len, z);
                Vector3 knee = hip + new Vector3(side * 0.16f * len, 0.07f * len, (row - 1) * -0.07f * len);
                Vector3 foot = new Vector3(hip.x + side * 0.26f * len, 0.002f, z + (row - 1) * -0.13f * len);
                Strut(new[] { hip, knee, foot }, 0.022f * len, dark, W1(b));
            }
            // Glass wings lying back over the abdomen.
            for (int side = -1; side <= 1; side += 2)
            {
                int bone = side < 0 ? wl : wr;
                Vector3 r0 = _bm[bone];
                Vector3 tip = r0 + new Vector3(side * 0.2f * len, 0.03f * len, -0.55f * len);
                Vector3 mid = r0 + new Vector3(side * 0.24f * len, 0.02f * len, -0.2f * len);
                Func<float, float, Vector3> P = (u, v) => Vector3.Lerp(Vector3.Lerp(r0, mid, v), Vector3.Lerp(r0 + new Vector3(side * 0.02f * len, 0f, -0.12f * len), tip, v), u);
                EmitSheet(1, 1, P, (u, v) => W1(bone), Vector3.up, 0.004f * len, wing, wing);
            }
            Hit(b, c + new Vector3(0f, 0f, -0.3f * len), c + new Vector3(0f, 0f, 0.35f * len), 0.2f * len);
            Hit(b, c + new Vector3(0f, 0.02f * len, -0.22f * len), c + new Vector3(0f, 0.02f * len, -0.22f * len), 0.16f * len, true);
            _aim = c;
            _headPoint = head;
            _mouthPoint = head + new Vector3(0f, -0.02f * len, 0.1f * len);
            _poseGrow = new Vector3(len * 0.6f, len * 6f, len * 0.6f);
        }

        /// <summary>Two pyramids base to base along axis (flat shaded, one vertex set per face).</summary>
        private void Bipyramid(Vector3 center, Vector3 axis, float length, float radius, int sides, Vector2 uv, BoneWeight w)
        {
            axis = Nrm(axis);
            Vector3 u = Vector3.Cross(axis, Mathf.Abs(axis.y) < 0.9f ? Vector3.up : Vector3.right);
            u = Nrm(u);
            Vector3 v = Vector3.Cross(axis, u);
            Vector3 front = center + axis * (length * 0.55f), back = center - axis * (length * 0.45f);
            Vector3[] ring = new Vector3[sides];
            float spin = Mathf.PI / sides;
            for (int k = 0; k < sides; k++)
            {
                float a = spin + Mathf.PI * 2f * k / sides;
                ring[k] = center + u * (Mathf.Cos(a) * radius) + v * (Mathf.Sin(a) * radius * 0.85f);
            }
            for (int k = 0; k < sides; k++)
            {
                Vector3 a = ring[k], bb = ring[(k + 1) % sides];
                for (int end = 0; end < 2; end++)
                {
                    Vector3 tip = end == 0 ? front : back;
                    Vector3 n = Nrm(Vector3.Cross(bb - a, tip - a));
                    if (Vector3.Dot(n, (a + bb + tip) / 3f - center) < 0f) n = -n;
                    Vector4 tan = Tan(bb - a, n, 1f);
                    int i0 = AddVertex(a, n, uv, new Vector2(0f, 0f), w, tan);
                    int i1 = AddVertex(bb, n, uv, new Vector2(1f, 0f), w, tan);
                    int i2 = AddVertex(tip, n, uv, new Vector2(0.5f, 1f), w, tan);
                    Tri(i0, i1, i2);
                }
            }
        }

        /// <summary>A thin three-sided strut through the points, closing to a point at the last one (insect legs).</summary>
        private void Strut(Vector3[] pts, float radius, Vector2 uv, BoneWeight w)
        {
            int first = _d.Vertices.Count;
            int rings = pts.Length - 1;
            for (int r = 0; r < rings; r++)
            {
                Vector3 dir = Nrm(pts[r + 1] - pts[r]);
                Vector3 a = Nrm(Vector3.Cross(dir, Vector3.up));
                if (a.sqrMagnitude < 0.5f) a = Vector3.right;
                Vector3 bb = Vector3.Cross(dir, a);
                float rr = radius * (1f - 0.35f * r);
                for (int k = 0; k < 3; k++)
                {
                    float ang = Mathf.PI * 2f * k / 3f;
                    Vector3 n = a * Mathf.Cos(ang) + bb * Mathf.Sin(ang);
                    AddVertex(pts[r] + n * rr, n, uv, new Vector2(k / 3f, r), w, Tan(dir, n, 1f));
                }
            }
            Vector3 lastDir = Nrm(pts[rings] - pts[rings - 1]);
            int tipV = AddVertex(pts[rings], lastDir, uv, new Vector2(0.5f, rings), w, Tan(Vector3.Cross(lastDir, Vector3.up), lastDir, 1f));
            for (int r = 0; r < rings - 1; r++)
                for (int k = 0; k < 3; k++)
                {
                    int a0 = first + r * 3 + k, a1 = first + r * 3 + (k + 1) % 3;
                    int b0 = a0 + 3, b1 = a1 + 3;
                    Quad(a0, a1, b1, b0);
                }
            int lr = first + (rings - 1) * 3;
            for (int k = 0; k < 3; k++) Tri(lr + k, lr + (k + 1) % 3, tipV);
        }
    }
}
