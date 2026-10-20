using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Procedural creature bodies: a bone hierarchy, one smooth skinned mesh and collision volumes for every species in
    /// the bestiary, built entirely in code. Bodies are sculpted from curved tube sweeps (superellipse sections on a
    /// spline with anatomical bumps: shoulder blades, haunches, rib cages, brows, jaw lines, knees and hocks) with
    /// separate pieces for ears, horns, crystal spines and plates (faceted, flat-shaded), glowing eyes and seams, claws
    /// and teeth, thin double-sided wing membranes with glowing veins, and layered flight feathers. Pieces overlap and
    /// blend their weights across joints so nothing cracks when posed. Pure C# (no engine calls), so builds can run on
    /// worker threads and the exact output can be inspected offline.
    /// Bones bind with identity rotations (CreatureSkeleton.LocalPositions are the offsets); the mesh uses the character
    /// palette (UV0: rows 8 hides, 9 crystal, 10 glow), the detail map on UV1 and carries tangents, like people.
    /// detail: 0 crowd (about a quarter of the hero budget), 1 standard (about half), 2 hero.
    /// </summary>
    public static class CreatureBuilder
    {
        public static CreatureBody Build(CreatureLook look, int detail)
        {
            if (look == null) look = CreatureLooks.For("neon_jackal", 0);
            return new CreatureSculptor(look, detail).Run();
        }
    }

    /// <summary>
    /// One creature build for CreatureBuilder.Build. A fresh instance per build keeps every build independent, so several
    /// worker threads can build at once. Split by plan: CreatureBuilder.Quadruped.cs (four-legged bodies, also the
    /// trunk, head, legs and tail of the winged plan), CreatureBuilder.Winged.cs (wings, membranes, feathers, the
    /// wyvern), CreatureBuilder.Serpent.cs (the wyrm), CreatureBuilder.Small.cs (wisp and mite).
    /// </summary>
    internal sealed partial class CreatureSculptor
    {
        private readonly CreatureLook _k;
        private readonly int _detail;
        private readonly float _s;
        private readonly BodyData _d = new BodyData();
        private readonly CreatureSkeleton _sk = new CreatureSkeleton();
        private readonly CreatureBody _b = new CreatureBody();
        /// <summary>Bind position of every bone in model space.</summary>
        private readonly List<Vector3> _bm = new List<Vector3>();
        /// <summary>Vertex ranges that count for Radius and Height (everything except tails, wings and crystals).</summary>
        private readonly List<Vector2Int> _core = new List<Vector2Int>();
        private uint _rng;
        /// <summary>Detail-map tile size in metres (UV1 = surface distance / tile).</summary>
        private float _tile = 0.06f;
        private Vector3 _aim, _headPoint, _mouthPoint;
        /// <summary>How far poses can reach past the bind box (metres per axis) for PoseBounds.</summary>
        private Vector3 _poseGrow;
        /// <summary>Plans whose body is not centred on the transform (the wyrm) set their own Radius and PoseBounds.</summary>
        private float _radiusOverride = -1f;
        /// <summary>Body vertices behind this z (a heavy tail grown out of the trunk) do not count for Radius.</summary>
        private float _radiusMinZ = float.MinValue;
        private bool _hasPoseOverride;
        private Bounds _poseOverride;

        public CreatureSculptor(CreatureLook look, int detail)
        {
            _k = look;
            _detail = Mathf.Clamp(detail, 0, 2);
            _s = Mathf.Clamp(look.Scale <= 0f ? 1f : look.Scale, 0.05f, 20f);
            _rng = (uint)(look.Seed * 2654435761u + 0x6C8E9CF5u);
            if (_rng == 0u) _rng = 1u;
        }

        public CreatureBody Run()
        {
            _b.Look = _k;
            _b.Skeleton = _sk;
            _b.Mesh = _d;
            switch (_k.Plan)
            {
                case CreaturePlan.Winged: BuildWinged(); break;
                case CreaturePlan.Serpent: BuildSerpent(); break;
                case CreaturePlan.Wisp: BuildWisp(); break;
                case CreaturePlan.Mite: BuildMite(); break;
                default: BuildQuadruped(); break;
            }
            if(_k.Plan!=CreaturePlan.Winged) AnatomyMotion04K();
            AscensionCreature04OH();
            Finish();
            return _b;
        }

        // ================================================================== small helpers

        private int Segs(int hero, int std, int crowd) { return _detail == 2 ? hero : (_detail == 1 ? std : crowd); }

        private static Vector2 Pal(Vector2Int cell) { return BodyBuilder.Pal(cell.y, cell.x); }

        private static BoneWeight W1(int b) { return BodyBuilder.W1(b); }
        private static BoneWeight W2(int a, int b, float wb) { return BodyBuilder.W2(a, b, wb); }
        private static BoneWeight W3(int a, int b, float wb, int c, float wc) { return BodyBuilder.W3(a, b, wb, c, wc); }

        private static Vector3 Nrm(Vector3 v) { return CreatureTube.Nrm(v); }

        /// <summary>Gaussian bump (zero beyond three sigma).</summary>
        private static float G(float x, float mu, float sigma)
        {
            float d = (x - mu) / sigma;
            d *= d;
            return d > 9f ? 0f : (float)Math.Exp(-d);
        }

        /// <summary>Angular gaussian in degrees (wraps around).</summary>
        private static float GA(float theta, float mu, float sigma)
        {
            float a = (theta - mu) % 360f;
            if (a > 180f) a -= 360f;
            else if (a < -180f) a += 360f;
            float d = a / sigma;
            d *= d;
            return d > 9f ? 0f : (float)Math.Exp(-d);
        }

        private static float SS(float a, float b, float x) { return QMath.SmoothRange(a, b, x); }

        private static float Wrap360(float th)
        {
            th %= 360f;
            return th < 0f ? th + 360f : th;
        }

        /// <summary>Deterministic per-creature random number in [0, 1).</summary>
        private float Rand()
        {
            _rng ^= _rng << 13;
            _rng ^= _rng >> 17;
            _rng ^= _rng << 5;
            return (_rng & 0xFFFFFF) / 16777216f;
        }

        private float Rand(float a, float b) { return a + (b - a) * Rand(); }

        // ================================================================== skeleton

        /// <summary>Adds a bone at a model-space position (its local offset is measured from the parent's bind position).</summary>
        private int Bone(string name, int parent, Vector3 model)
        {
            Vector3 local = parent >= 0 ? model - _bm[parent] : model;
            int i = _sk.Add(name, parent, local);
            _bm.Add(model);
            return i;
        }

        // ================================================================== emission

        private int AddVertex(Vector3 p, Vector3 n, Vector2 uv, Vector2 detail, BoneWeight w, Vector4 tangent)
        {
            _d.Vertices.Add(p);
            _d.Normals.Add(n);
            _d.Palette.Add(uv);
            _d.Detail.Add(detail);
            _d.Weights.Add(w);
            _d.Tangents.Add(tangent);
            return _d.Vertices.Count - 1;
        }

        private int AddVertex(Vector3 p, Vector3 n, Vector2 uv, Vector2 detail, BoneWeight w)
        {
            Vector3 t = Nrm(Vector3.Cross(n, Mathf.Abs(n.y) < 0.95f ? Vector3.up : Vector3.right));
            return AddVertex(p, n, uv, detail, w, new Vector4(t.x, t.y, t.z, 1f));
        }

        /// <summary>A tangent along dir made exactly perpendicular to the normal (w is the bitangent sign).</summary>
        private static Vector4 Tan(Vector3 dir, Vector3 n, float w)
        {
            Vector3 t = dir - n * Vector3.Dot(dir, n);
            if (t.sqrMagnitude < 1e-14f) t = Vector3.Cross(n, Mathf.Abs(n.y) < 0.95f ? Vector3.up : Vector3.right);
            t = Nrm(t);
            return new Vector4(t.x, t.y, t.z, w < 0f ? -1f : 1f);
        }

        /// <summary>Adds a triangle wound so its face points along the vertex normals (Unity front faces are clockwise).</summary>
        private void Tri(int a, int b, int c)
        {
            List<Vector3> v = _d.Vertices;
            Vector3 n = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
            // Sheets and fans pinch to a point at some edges: a triangle with no area is never drawn, so it is skipped.
            if (n.sqrMagnitude < 1e-16f) return;
            Vector3 avg = _d.Normals[a] + _d.Normals[b] + _d.Normals[c];
            if (Vector3.Dot(n, avg) < 0f)
            {
                _d.Triangles.Add(a); _d.Triangles.Add(c); _d.Triangles.Add(b);
            }
            else
            {
                _d.Triangles.Add(a); _d.Triangles.Add(b); _d.Triangles.Add(c);
            }
        }

        private void Quad(int a, int b, int c, int d)
        {
            Tri(a, b, c);
            Tri(a, c, d);
        }

        /// <summary>Turns the vertices from vStart and the triangles from tStart inside out (a throat seen from within).</summary>
        private void FlipRange(int vStart, int tStart)
        {
            for (int i = vStart; i < _d.Vertices.Count; i++)
            {
                _d.Normals[i] = -_d.Normals[i];
                Vector4 t = _d.Tangents[i];
                _d.Tangents[i] = new Vector4(t.x, t.y, t.z, -t.w);
            }
            for (int i = tStart; i + 2 < _d.Triangles.Count; i += 3)
            {
                int b = _d.Triangles[i + 1];
                _d.Triangles[i + 1] = _d.Triangles[i + 2];
                _d.Triangles[i + 2] = b;
            }
        }

        private int _coreStart = -1;

        /// <summary>Marks the vertices emitted until EndCore as body (they count for Radius and Height).</summary>
        private void BeginCore() { _coreStart = _d.Vertices.Count; }

        private void EndCore()
        {
            if (_coreStart >= 0 && _d.Vertices.Count > _coreStart) _core.Add(new Vector2Int(_coreStart, _d.Vertices.Count));
            _coreStart = -1;
        }

        /// <summary>Evenly spaced ring parameters between t0 and t1.</summary>
        private static float[] Rings(float t0, float t1, int count)
        {
            count = Mathf.Max(2, count);
            float[] r = new float[count];
            for (int i = 0; i < count; i++) r[i] = Mathf.Lerp(t0, t1, (float)i / (count - 1));
            return r;
        }

        /// <summary>Ring parameters packed toward both ends (rounded tips need the resolution where the radius changes fast).</summary>
        private static float[] RingsEased(float t0, float t1, int count, float endBias)
        {
            count = Mathf.Max(2, count);
            float[] r = new float[count];
            for (int i = 0; i < count; i++)
            {
                float u = (float)i / (count - 1);
                float e = u - endBias * Mathf.Sin(u * Mathf.PI * 2f) / (Mathf.PI * 2f);
                r[i] = Mathf.Lerp(t0, t1, e);
            }
            return r;
        }

        // Scratch rows for EmitTube (reused, so a tube does not allocate per ring).
        private Vector3[] _rowP = new Vector3[64], _rowN = new Vector3[64];
        private Vector4[] _rowT = new Vector4[64];
        private BoneWeight[] _rowW = new BoneWeight[64];
        private float[] _rowU = new float[64];

        /// <summary>
        /// Emits a tube surface. ringTs: the t of every ring, ascending. region(t, theta) gives the palette cell; rings are
        /// duplicated where it changes along the tube and columns are split at the theta breaks (ascending, degrees) so a
        /// colour never bleeds across a triangle. Caps close hidden ends with a small fan.
        /// </summary>
        private void EmitTube(CreatureTube tb, float[] ringTs, int segs, Func<float, float, Vector2> region, float[] breaks, bool capStart, bool capEnd)
        {
            float[] br = breaks == null || breaks.Length == 0 ? new[] { 0f } : breaks;
            int ns = br.Length;
            int[] cols = new int[ns];
            float[] th0 = new float[ns], th1 = new float[ns];
            int maxCols = 0;
            for (int i = 0; i < ns; i++)
            {
                th0[i] = br[i];
                th1[i] = i + 1 < ns ? br[i + 1] : br[0] + 360f;
                cols[i] = Mathf.Max(1, Mathf.RoundToInt(segs * (th1[i] - th0[i]) / 360f));
                maxCols = Mathf.Max(maxCols, cols[i] + 1);
            }
            if (_rowP.Length < maxCols)
            {
                _rowP = new Vector3[maxCols]; _rowN = new Vector3[maxCols]; _rowT = new Vector4[maxCols];
                _rowW = new BoneWeight[maxCols]; _rowU = new float[maxCols];
            }
            int nr = ringTs.Length;
            int[,] above = new int[nr, ns], below = new int[nr, ns];
            float vAcc = 0f;
            Vector3 prevC = tb.Center(ringTs[0]);
            for (int r = 0; r < nr; r++)
            {
                float t = ringTs[r];
                CreatureTube.Ring rb = tb.EvalRing(t - CreatureTube.StepT), rm = tb.EvalRing(t), ra = tb.EvalRing(t + CreatureTube.StepT);
                vAcc += (rm.C - prevC).magnitude;
                prevC = rm.C;
                float perimeter = Mathf.PI * (rm.A + (rm.BU + rm.BD) * 0.5f);
                for (int i = 0; i < ns; i++)
                {
                    float thMid = Wrap360((th0[i] + th1[i]) * 0.5f);
                    Vector2 uvA = r > 0 ? region((ringTs[r - 1] + t) * 0.5f, thMid) : region(t, thMid);
                    Vector2 uvB = r < nr - 1 ? region((t + ringTs[r + 1]) * 0.5f, thMid) : uvA;
                    int nc = cols[i];
                    for (int c = 0; c <= nc; c++)
                    {
                        float th = Mathf.Lerp(th0[i], th1[i], (float)c / nc);
                        tb.Frame(ref rb, ref rm, ref ra, th, out _rowP[c], out _rowN[c], out _rowT[c]);
                        _rowW[c] = tb.WeightAt(t, Wrap360(th));
                        _rowU[c] = (th - br[0]) / 360f * perimeter / _tile;
                    }
                    above[r, i] = _d.Vertices.Count;
                    for (int c = 0; c <= nc; c++) AddVertex(_rowP[c], _rowN[c], uvA, new Vector2(_rowU[c], vAcc / _tile), _rowW[c], _rowT[c]);
                    if (uvB == uvA) below[r, i] = above[r, i];
                    else
                    {
                        below[r, i] = _d.Vertices.Count;
                        for (int c = 0; c <= nc; c++) AddVertex(_rowP[c], _rowN[c], uvB, new Vector2(_rowU[c], vAcc / _tile), _rowW[c], _rowT[c]);
                    }
                }
            }
            for (int r = 0; r < nr - 1; r++)
                for (int i = 0; i < ns; i++)
                {
                    int a0 = below[r, i], b0 = above[r + 1, i];
                    for (int c = 0; c < cols[i]; c++) Quad(a0 + c, a0 + c + 1, b0 + c + 1, b0 + c);
                }
            if (capStart) CapTube(tb, ringTs[0], below, 0, cols, th0, region, true);
            if (capEnd) CapTube(tb, ringTs[nr - 1], above, nr - 1, cols, th0, region, false);
        }

        private void CapTube(CreatureTube tb, float t, int[,] idx, int r, int[] cols, float[] th0, Func<float, float, Vector2> region, bool start)
        {
            CreatureTube.Ring ring = tb.EvalRing(t);
            Vector3 n = start ? -ring.F : ring.F;
            Vector3 tip = ring.C + n * Mathf.Min(ring.A, Mathf.Min(ring.BU, ring.BD)) * 0.35f;
            for (int i = 0; i < cols.Length; i++)
            {
                Vector2 uv = region(t, Wrap360(th0[i] + 1f));
                int ci = AddVertex(tip, n, uv, Vector2.zero, tb.WeightAt(t, 0f));
                for (int c = 0; c < cols[i]; c++)
                {
                    // The cap shares the ring's position but needs its own vertices for the palette cell and flat normal.
                    int a = AddVertex(_d.Vertices[idx[r, i] + c], n, uv, Vector2.zero, _d.Weights[idx[r, i] + c]);
                    int b = AddVertex(_d.Vertices[idx[r, i] + c + 1], n, uv, Vector2.zero, _d.Weights[idx[r, i] + c + 1]);
                    Tri(ci, a, b);
                }
            }
        }

        /// <summary>An ellipsoid (eyes, pads, joints) rigid to one weight.</summary>
        private void EmitEllipsoid(Vector3 center, Vector3 radii, Quaternion rot, int rings, int segs, Vector2 uv, BoneWeight w)
        {
            int first = _d.Vertices.Count;
            for (int r = 0; r <= rings; r++)
            {
                float phi = Mathf.PI * r / rings;
                float sp = Mathf.Sin(phi), cp = Mathf.Cos(phi);
                for (int j = 0; j <= segs; j++)
                {
                    float th = 2f * Mathf.PI * j / segs;
                    Vector3 unit = new Vector3(sp * Mathf.Cos(th), cp, sp * Mathf.Sin(th));
                    Vector3 p = new Vector3(unit.x * radii.x, unit.y * radii.y, unit.z * radii.z);
                    Vector3 n = Nrm(new Vector3(unit.x / Mathf.Max(1e-5f, radii.x), unit.y / Mathf.Max(1e-5f, radii.y), unit.z / Mathf.Max(1e-5f, radii.z)));
                    if (n.sqrMagnitude < 0.5f) n = rot * Vector3.up;
                    Vector3 wn = rot * n;
                    AddVertex(center + rot * p, wn, uv, new Vector2(j * 0.25f, r * 0.25f), w, Tan(rot * new Vector3(-Mathf.Sin(th), 0f, Mathf.Cos(th)), wn, 1f));
                }
            }
            for (int r = 0; r < rings; r++)
                for (int j = 0; j < segs; j++)
                {
                    int a = first + r * (segs + 1) + j;
                    int b = a + segs + 1;
                    // The pole rows collapse to a point: only the half of each quad with area is kept.
                    if (r > 0) Tri(a, a + 1, b + 1);
                    if (r < rings - 1) Tri(a, b + 1, b);
                }
        }

        /// <summary>
        /// A faceted crystal: an N-sided prism that tapers into a pointed tip, flat shaded (every face has its own
        /// vertices and normal), slightly irregular so a ridge of them reads as grown, not stamped. The base sits a
        /// little inside the body it grows from.
        /// </summary>
        private void EmitCrystal(Vector3 basePos, Vector3 dir, Vector3 sideHint, float length, float radius, int sides, float tipFrac, Vector2 uv, BoneWeight w, float jag = 0.18f, float flat = 1f)
        {
            dir = Nrm(dir);
            if (dir.sqrMagnitude < 0.5f) dir = Vector3.up;
            Vector3 u = sideHint - dir * Vector3.Dot(sideHint, dir);
            if (u.sqrMagnitude < 1e-8f) u = Vector3.Cross(dir, Mathf.Abs(dir.y) < 0.9f ? Vector3.up : Vector3.right);
            u = Nrm(u);
            Vector3 v = Vector3.Cross(dir, u);
            sides = Mathf.Clamp(sides, 3, 12);
            float bodyLen = length * (1f - tipFrac);
            Vector3[] baseRing = new Vector3[sides], topRing = new Vector3[sides];
            float spin = Rand(0f, Mathf.PI * 2f);
            for (int k = 0; k < sides; k++)
            {
                float ang = spin + Mathf.PI * 2f * k / sides + Rand(-jag, jag) * 0.6f;
                float rr = radius * (1f + Rand(-jag, jag));
                Vector3 radial = u * (Mathf.Cos(ang) * rr) + v * (Mathf.Sin(ang) * rr * flat);
                baseRing[k] = basePos + radial;
                topRing[k] = basePos + dir * (bodyLen * (1f + Rand(-jag, jag) * 0.5f)) + radial * 0.82f;
            }
            Vector3 tip = basePos + dir * length + (u * Rand(-1f, 1f) + v * Rand(-1f, 1f)) * radius * 0.12f;
            Vector3 axisMid = basePos + dir * (bodyLen * 0.5f);
            for (int k = 0; k < sides; k++)
            {
                int k1 = (k + 1) % sides;
                Vector3 a = baseRing[k], b = baseRing[k1], c = topRing[k1], d = topRing[k];
                Vector3 n = Nrm(Vector3.Cross(b - a, d - a));
                if (Vector3.Dot(n, (a + b + c + d) * 0.25f - axisMid) < 0f) n = -n;
                Vector4 tan = Tan(b - a, n, 1f);
                int i0 = AddVertex(a, n, uv, new Vector2(0f, 0f), w, tan);
                int i1 = AddVertex(b, n, uv, new Vector2(1f, 0f), w, tan);
                int i2 = AddVertex(c, n, uv, new Vector2(1f, 1f), w, tan);
                int i3 = AddVertex(d, n, uv, new Vector2(0f, 1f), w, tan);
                Quad(i0, i1, i2, i3);
                Vector3 nt = Nrm(Vector3.Cross(c - d, tip - d));
                if (Vector3.Dot(nt, (c + d + tip) / 3f - (basePos + dir * bodyLen)) < 0f) nt = -nt;
                Vector4 tant = Tan(c - d, nt, 1f);
                int j0 = AddVertex(d, nt, uv, new Vector2(0f, 1f), w, tant);
                int j1 = AddVertex(c, nt, uv, new Vector2(1f, 1f), w, tant);
                int j2 = AddVertex(tip, nt, uv, new Vector2(0.5f, 1.5f), w, tant);
                Tri(j0, j1, j2);
            }
        }

        /// <summary>A small smooth curved cone (claws, teeth, horns) along a quadratic arc from base toward tip.</summary>
        private void EmitHorn(Vector3 basePos, Vector3 tipPos, Vector3 bendDir, float bend, float radius, int rings, int segs, Vector2 uv, BoneWeight wBase, BoneWeight wTip, float flatten = 1f, float roundTip = 0.1f, bool buriedBase = false)
        {
            Vector3 mid = (basePos + tipPos) * 0.5f + Nrm(bendDir) * bend;
            Vector3[] ctrl = { basePos, Vector3.Lerp(basePos, mid, 0.66f), Vector3.Lerp(mid, tipPos, 0.4f), tipPos };
            Vector3 axis = tipPos - basePos;
            Vector3 upHint = bendDir.sqrMagnitude > 1e-8f ? bendDir : Vector3.Cross(axis, Vector3.right);
            float tipR = radius * roundTip;
            float[] keys =
            {
                0f, radius, radius * flatten, radius * flatten, 2f,
                0.55f, radius * 0.62f, radius * 0.62f * flatten, radius * 0.62f * flatten, 2f,
                0.92f, tipR * 2.2f, tipR * 2.2f * flatten, tipR * 2.2f * flatten, 2f,
                1f, tipR, tipR, tipR, 2f
            };
            CreatureTube tb = new CreatureTube(ctrl, upHint, keys);
            bool same = wBase.boneIndex0 == wTip.boneIndex0 && Mathf.Approximately(wBase.weight0, wTip.weight0);
            tb.Weight = (t, th) => same ? wBase : (t < 0.5f ? wBase : wTip);
            EmitTube(tb, RingsEased(0f, 1f, rings, 0.4f), segs, (t, th) => uv, null, !buriedBase, !buriedBase);
        }

        /// <summary>
        /// One tuft of fur: a small flattened cone growing out of the body (wide across the surface, thin outward), bent
        /// along its length, with smooth normals and no caps (its root is buried in the body). 3 rings of 6.
        /// </summary>
        private void EmitTuft(Vector3 root, Vector3 tip, Vector3 normal, float width, float flat, Vector2 uv, BoneWeight w)
        {
            Vector3 axis = tip - root;
            float len = axis.magnitude;
            if (len < 1e-5f) return;
            Vector3 dir = axis / len;
            Vector3 side = Vector3.Cross(normal, dir);
            if (side.sqrMagnitude < 1e-10f) side = Vector3.Cross(dir, Mathf.Abs(dir.y) < 0.9f ? Vector3.up : Vector3.right);
            side = Nrm(side);
            Vector3 up = Nrm(Vector3.Cross(dir, side));
            const int segs = 6;
            float[] us = { 0f, 0.45f, 1f };
            float[] rs = { 1f, 0.62f, 0.04f };
            int first = _d.Vertices.Count;
            for (int r = 0; r < 3; r++)
            {
                float u = us[r];
                // The tuft bows away from the body toward its tip.
                Vector3 c = root + dir * (len * u) + up * (len * 0.12f * u * u);
                for (int j = 0; j <= segs; j++)
                {
                    float a = Mathf.PI * 2f * j / segs;
                    float cs = Mathf.Cos(a), sn = Mathf.Sin(a);
                    Vector3 radial = side * (cs * width * 0.5f * rs[r]) + up * (sn * width * 0.5f * rs[r] * flat);
                    Vector3 n = Nrm(side * (cs * flat) + up * sn + dir * 0.25f);
                    AddVertex(c + radial, n, uv, new Vector2(j * 0.2f, u), w, Tan(side * -sn + up * cs, n, 1f));
                }
            }
            for (int r = 0; r < 2; r++)
                for (int j = 0; j < segs; j++)
                {
                    int a = first + r * (segs + 1) + j;
                    int b = a + segs + 1;
                    Quad(a, a + 1, b + 1, b);
                }
        }

        /// <summary>
        /// A fringe of fur tufts around a tube between two angles at t (cheek ruffs, chest and elbow tufts, the back of the
        /// thighs). sweep is the direction the fur lies; each tuft leans out of the surface by lift.
        /// </summary>
        private void FurFringe(CreatureTube tb, float t, float dt, float thA, float thB, int count, float length, float width, Vector3 sweep, float lift, Vector2 uv)
        {
            for (int i = 0; i < count; i++)
            {
                float u = count == 1 ? 0.5f : (i + Rand(0.25f, 0.75f)) / count;
                float th = Mathf.Lerp(thA, thB, u);
                float tt = t + Rand(-dt, dt);
                Vector3 p, n;
                tb.Frame(tt, th, out p, out n);
                Vector3 s = Nrm(sweep - n * Vector3.Dot(sweep, n));
                Vector3 dir = Nrm(s + n * lift);
                float len = length * Rand(0.75f, 1.2f);
                EmitTuft(p - n * (width * 0.25f), p + dir * len, n, width * Rand(0.85f, 1.15f), 0.32f, uv, tb.WeightAt(tt, Wrap360(th)));
            }
        }

        /// <summary>
        /// A strip lying on a tube along a curve in (t, theta) (stripes and markings): curve(s) gives (t, theta) for s in
        /// 0..1 and width(s) the strip's width in metres. It floats a hair above the surface and takes its weights.
        /// </summary>
        private void EmitSurfaceStrip(CreatureTube tb, Func<float, Vector2> curve, Func<float, float> width, int n, Vector2 uv, float offset)
        {
            int prevA = -1, prevB = -1;
            for (int i = 0; i <= n; i++)
            {
                float s = (float)i / n;
                Vector2 c = curve(s);
                Vector3 p, nrm;
                tb.Frame(c.x, c.y, out p, out nrm);
                Vector2 c2 = curve(Mathf.Min(1f, s + 0.02f)), c1 = curve(Mathf.Max(0f, s - 0.02f));
                Vector3 along = tb.Point(c2.x, c2.y, 0f) - tb.Point(c1.x, c1.y, 0f);
                Vector3 across = Vector3.Cross(nrm, along);
                if (across.sqrMagnitude < 1e-14f) continue;
                across = Nrm(across) * (Mathf.Max(0.0004f, width(s)) * 0.5f);
                BoneWeight w = tb.WeightAt(c.x, Wrap360(c.y));
                Vector4 tan = Tan(along, nrm, 1f);
                Vector3 q = p + nrm * offset;
                int a = AddVertex(q - across, nrm, uv, Vector2.zero, w, tan);
                int b = AddVertex(q + across, nrm, uv, Vector2.zero, w, tan);
                if (prevA >= 0) Quad(prevA, prevB, b, a);
                prevA = a;
                prevB = b;
            }
        }

        /// <summary>A thin strip that follows a tube at theta(t) (glowing seams and veins).</summary>
        private void EmitRibbon(CreatureTube tb, float t0, float t1, Func<float, float> theta, float width, int n, Vector2 uv, float offset)
        {
            int prevA = -1, prevB = -1;
            for (int i = 0; i <= n; i++)
            {
                float t = Mathf.Lerp(t0, t1, (float)i / n);
                float th = theta(t);
                Vector3 p0, nrm;
                tb.Frame(t, th, out p0, out nrm);
                Vector3 p = p0 + nrm * offset;
                Vector3 around = tb.Point(t, th + 2f, 0f) - tb.Point(t, th - 2f, 0f);
                if (around.sqrMagnitude < 1e-12f) around = Vector3.Cross(nrm, tb.Center(t + 0.01f) - tb.Center(t));
                float taper = Mathf.Max(0f, Mathf.Sin(Mathf.Clamp01((float)i / n) * Mathf.PI));
                around = Nrm(around) * (width * 0.5f * Mathf.Lerp(0.35f, 1f, Mathf.Sqrt(taper)));
                BoneWeight w = tb.WeightAt(t, Wrap360(th));
                Vector4 tan = Tan(tb.Center(t + 0.01f) - tb.Center(t - 0.01f), nrm, 1f);
                int a = AddVertex(p - around, nrm, uv, Vector2.zero, w, tan);
                int b = AddVertex(p + around, nrm, uv, Vector2.zero, w, tan);
                if (prevA >= 0) Quad(prevA, prevB, b, a);
                prevA = a;
                prevB = b;
            }
        }

        /// <summary>
        /// A curved panel lying on a tube (armour plates, inner ears, pads). thickness above 0 adds side walls so it reads
        /// as a solid piece.
        /// </summary>
        private void EmitPatch(CreatureTube tb, float t0, float t1, float thA, float thB, int nt, int nth, float offset, float thickness, Vector2 uv, Vector2 edgeUV)
        {
            int[,] top = new int[nt + 1, nth + 1];
            for (int i = 0; i <= nt; i++)
            {
                float t = Mathf.Lerp(t0, t1, (float)i / nt);
                for (int j = 0; j <= nth; j++)
                {
                    float th = Mathf.Lerp(thA, thB, (float)j / nth);
                    Vector3 sp, n;
                    tb.Frame(t, th, out sp, out n);
                    top[i, j] = AddVertex(sp + n * (offset + thickness), n, uv, new Vector2(j * 0.3f, i * 0.3f), tb.WeightAt(t, Wrap360(th)));
                }
            }
            for (int i = 0; i < nt; i++)
                for (int j = 0; j < nth; j++) Quad(top[i, j], top[i, j + 1], top[i + 1, j + 1], top[i + 1, j]);
            if (thickness <= 0f) return;
            Vector3 mid = tb.Point((t0 + t1) * 0.5f, (thA + thB) * 0.5f, offset);
            int perim = 2 * (nt + nth);
            for (int k = 0; k < perim; k++)
            {
                float ta, tha, tb2, thb;
                PatchBorder(k, nt, nth, t0, t1, thA, thB, out ta, out tha);
                PatchBorder(k + 1, nt, nth, t0, t1, thA, thB, out tb2, out thb);
                Vector3 sa, na, sb, nb;
                tb.Frame(ta, tha, out sa, out na);
                tb.Frame(tb2, thb, out sb, out nb);
                Vector3 a0 = sa + na * offset, a1 = sa + na * (offset + thickness);
                Vector3 b0 = sb + nb * offset, b1 = sb + nb * (offset + thickness);
                Vector3 nrm = Vector3.Cross(b0 - a0, na);
                if (nrm.sqrMagnitude < 1e-14f) continue;
                nrm = Nrm(nrm);
                if (Vector3.Dot(nrm, (a0 + b0) * 0.5f - mid) < 0f) nrm = -nrm;
                BoneWeight wa = tb.WeightAt(ta, Wrap360(tha)), wb = tb.WeightAt(tb2, Wrap360(thb));
                int i0 = AddVertex(a0, nrm, edgeUV, Vector2.zero, wa);
                int i1 = AddVertex(a1, nrm, edgeUV, Vector2.zero, wa);
                int i2 = AddVertex(b1, nrm, edgeUV, Vector2.zero, wb);
                int i3 = AddVertex(b0, nrm, edgeUV, Vector2.zero, wb);
                Quad(i0, i1, i2, i3);
            }
        }

        private static void PatchBorder(int k, int nt, int nth, float t0, float t1, float thA, float thB, out float t, out float th)
        {
            int perim = 2 * (nt + nth);
            k %= perim;
            int i, j;
            if (k <= nth) { i = 0; j = k; }
            else if (k <= nth + nt) { i = k - nth; j = nth; }
            else if (k <= 2 * nth + nt) { i = nt; j = nth - (k - nth - nt); }
            else { i = nt - (k - 2 * nth - nt); j = 0; }
            t = Mathf.Lerp(t0, t1, (float)i / nt);
            th = Mathf.Lerp(thA, thB, (float)j / nth);
        }

        /// <summary>
        /// A thin double-sided skinned sheet (wing membranes, flight feathers): P(u, v) is the mid-surface, W(u, v) the
        /// weights, up a hint for which face is the top. Both faces are emitted half a thickness apart with their own
        /// normals, and the border gets a narrow rim so the sheet never shows a hairline gap edge-on.
        /// </summary>
        private void EmitSheet(int nu, int nv, Func<float, float, Vector3> P, Func<float, float, BoneWeight> W, Vector3 up, float thickness, Vector2 uvTop, Vector2 uvBottom, Func<float, float, Vector2> uvAt = null)
        {
            Vector3[,] p = new Vector3[nu + 1, nv + 1];
            Vector3[,] n = new Vector3[nu + 1, nv + 1];
            for (int i = 0; i <= nu; i++)
                for (int j = 0; j <= nv; j++) p[i, j] = P((float)i / nu, (float)j / nv);
            for (int i = 0; i <= nu; i++)
                for (int j = 0; j <= nv; j++)
                {
                    Vector3 du = p[Mathf.Min(nu, i + 1), j] - p[Mathf.Max(0, i - 1), j];
                    Vector3 dv = p[i, Mathf.Min(nv, j + 1)] - p[i, Mathf.Max(0, j - 1)];
                    Vector3 nn = Vector3.Cross(du, dv);
                    if (nn.sqrMagnitude < 1e-16f) nn = up;
                    nn = Nrm(nn);
                    if (Vector3.Dot(nn, up) < 0f) nn = -nn;
                    n[i, j] = nn;
                }
            float h = thickness * 0.5f;
            for (int side = 0; side < 2; side++)
            {
                float sg = side == 0 ? 1f : -1f;
                int first = _d.Vertices.Count;
                for (int i = 0; i <= nu; i++)
                    for (int j = 0; j <= nv; j++)
                    {
                        float u = (float)i / nu, v = (float)j / nv;
                        Vector2 uv = uvAt != null ? uvAt(u, v) : (side == 0 ? uvTop : uvBottom);
                        Vector3 du = p[Mathf.Min(nu, i + 1), j] - p[Mathf.Max(0, i - 1), j];
                        AddVertex(p[i, j] + n[i, j] * (h * sg), n[i, j] * sg, uv, new Vector2(u * 4f, v * 4f), W(u, v), Tan(du, n[i, j] * sg, sg));
                    }
                for (int i = 0; i < nu; i++)
                    for (int j = 0; j < nv; j++)
                    {
                        int a = first + i * (nv + 1) + j;
                        int b = a + nv + 1;
                        Quad(a, a + 1, b + 1, b);
                    }
            }
        }

        /// <summary>
        /// One feather: a tapered double-sided vane with a raised shaft, rounded tip and a little curl. root: where the quill
        /// enters the wing, dir: along the feather, normal: the top face direction.
        /// </summary>
        private void EmitFeather(Vector3 root, Vector3 dir, Vector3 normal, float length, float width, float curl, Vector2 uv, Vector2 tipUV, float tipFrom, BoneWeight w, int nu, int nv)
        {
            dir = Nrm(dir);
            Vector3 side = Nrm(Vector3.Cross(normal, dir));
            Vector3 up = Nrm(Vector3.Cross(dir, side));
            Func<float, float, Vector3> P = (u, v) =>
            {
                float x = (v - 0.5f) * 2f;
                // Width profile: narrow quill, full vane, a blunt rounded end (the shaft runs a little past the vanes).
                float tipK = Mathf.Clamp01((u - 0.6f) / 0.4f);
                float prof = Mathf.Sqrt(Mathf.Clamp01(u * 6f)) * (1f - 0.62f * tipK * tipK * tipK);
                float half = width * 0.5f * prof;
                // The trailing vane is wider than the leading one, like a real flight feather.
                float lat = x * half * (x > 0f ? 1.15f : 0.85f);
                float shaft = (1f - Mathf.Abs(x)) * width * 0.06f;
                float droop = curl * length * u * u - Mathf.Abs(x) * half * 0.12f;
                float along = u * length * (1f + 0.07f * tipK * (1f - x * x));
                return root + dir * along + side * lat + up * (shaft + droop);
            };
            EmitSheet(nu, nv, P, (u, v) => w, up, Mathf.Max(0.002f, width * 0.03f), uv, uv, (u, v) => u >= tipFrom ? tipUV : uv);
        }

        // ================================================================== hit volumes and finish

        /// <summary>A capsule from a to b (model space at bind) carried by a bone.</summary>
        private void Hit(int bone, Vector3 a, Vector3 b, float radius, bool weak = false)
        {
            CreatureHitVolume h = new CreatureHitVolume();
            h.Bone = bone;
            Vector3 c = (a + b) * 0.5f;
            h.Center = c - _bm[bone];
            Vector3 d = b - a;
            float len = d.magnitude;
            h.Axis = len > 1e-5f ? d / len : Vector3.up;
            h.Radius = radius;
            h.Length = len > 1e-5f ? len + radius * 2f : 0f;
            h.WeakPoint = weak;
            _b.HitVolumes.Add(h);
        }

        private void Finish()
        {
            List<Vector3> v = _d.Vertices;
            // Normalise weights (four influences at most) and make every index valid.
            int bones = Mathf.Max(1, _sk.Count);
            for (int i = 0; i < _d.Weights.Count; i++)
            {
                BoneWeight w = _d.Weights[i];
                float sum = w.weight0 + w.weight1 + w.weight2 + w.weight3;
                if (w.boneIndex0 < 0 || w.boneIndex0 >= bones) { w.boneIndex0 = 0; }
                if (w.weight1 <= 0f || w.boneIndex1 < 0 || w.boneIndex1 >= bones) { w.boneIndex1 = 0; w.weight1 = 0f; }
                if (w.weight2 <= 0f || w.boneIndex2 < 0 || w.boneIndex2 >= bones) { w.boneIndex2 = 0; w.weight2 = 0f; }
                if (w.weight3 <= 0f || w.boneIndex3 < 0 || w.boneIndex3 >= bones) { w.boneIndex3 = 0; w.weight3 = 0f; }
                sum = w.weight0 + w.weight1 + w.weight2 + w.weight3;
                if (sum < 1e-6f) { w = W1(Mathf.Max(0, _sk.Root)); }
                else if (Mathf.Abs(sum - 1f) > 1e-5f)
                {
                    float k = 1f / sum;
                    w.weight0 *= k; w.weight1 *= k; w.weight2 *= k; w.weight3 *= k;
                }
                _d.Weights[i] = w;
            }
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue), max = -min;
            for (int i = 0; i < v.Count; i++)
            {
                min = Vector3.Min(min, v[i]);
                max = Vector3.Max(max, v[i]);
            }
            if (v.Count == 0) { min = Vector3.zero; max = Vector3.one * 0.1f; }
            _b.Bounds = new Bounds((min + max) * 0.5f, max - min);
            float radius = 0f, height = 0f;
            foreach (Vector2Int range in _core)
                for (int i = range.x; i < range.y; i++)
                {
                    Vector3 p = v[i];
                    height = Mathf.Max(height, p.y);
                    if (p.z < _radiusMinZ) continue;
                    radius = Mathf.Max(radius, Mathf.Sqrt(p.x * p.x + p.z * p.z));
                }
            if (_core.Count == 0)
            {
                radius = Mathf.Max(Mathf.Max(Mathf.Abs(min.x), Mathf.Abs(max.x)), Mathf.Max(Mathf.Abs(min.z), Mathf.Abs(max.z)));
                height = max.y;
            }
            _b.Radius = _radiusOverride > 0f ? _radiusOverride : radius;
            _b.Height = height;
            _b.AimPoint = _aim;
            _b.HeadPoint = _headPoint;
            _b.MouthPoint = _mouthPoint;
            Vector3 grow = _poseGrow;
            Bounds pb = _b.Bounds;
            pb.Expand(grow * 2f);
            // Poses never go below the ground by more than the grow allows, but the box must include the ground line.
            pb.Encapsulate(new Vector3(pb.center.x, Mathf.Min(0f, pb.min.y), pb.center.z));
            if (_hasPoseOverride) pb = _poseOverride;
            _b.PoseBounds = pb;
        }
    }
}
