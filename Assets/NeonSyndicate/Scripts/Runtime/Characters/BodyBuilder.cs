using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Raw skinned mesh data produced by BodyBuilder (turned into a Mesh by HumanoidRig).</summary>
    public sealed class BodyData
    {
        public readonly List<Vector3> Vertices = new List<Vector3>(6000);
        public readonly List<Vector3> Normals = new List<Vector3>(6000);
        public readonly List<Vector2> Palette = new List<Vector2>(6000);
        public readonly List<Vector2> Detail = new List<Vector2>(6000);
        public readonly List<BoneWeight> Weights = new List<BoneWeight>(6000);
        public readonly List<Vector4> Tangents = new List<Vector4>(6000);
        public readonly List<int> Triangles = new List<int>(24000);
    }

    /// <summary>
    /// A lofted surface: elliptical (superellipse) cross-sections swept along an axis, with key rings smoothly
    /// interpolated (monotone cubic), radial bumps for anatomy, and per-vertex bone weights. Torso, limbs, hands,
    /// shoes, neck and the head are all lofts, which is what makes the bodies smooth instead of boxy.
    /// </summary>
    internal sealed class Loft
    {
        public Vector3 P0, P1, U, V;
        public float[] T, A, B, CU, CV, E;
        private float[] _mA, _mB, _mCU, _mCV, _mE;
        public Func<float, float, float> Bump;
        public Func<float, float, BoneWeight> Weight;

        public Loft(Vector3 p0, Vector3 p1, Vector3 u, Vector3 v, float[] keys)
        {
            P0 = p0;
            P1 = p1;
            U = u;
            V = v;
            int n = keys.Length / 6;
            T = new float[n]; A = new float[n]; B = new float[n]; CU = new float[n]; CV = new float[n]; E = new float[n];
            for (int i = 0; i < n; i++)
            {
                T[i] = keys[i * 6];
                A[i] = keys[i * 6 + 1];
                B[i] = keys[i * 6 + 2];
                CU[i] = keys[i * 6 + 3];
                CV[i] = keys[i * 6 + 4];
                E[i] = keys[i * 6 + 5];
            }
            Prepare();
        }

        public void Prepare()
        {
            _mA = Slopes(T, A);
            _mB = Slopes(T, B);
            _mCU = Slopes(T, CU);
            _mCV = Slopes(T, CV);
            _mE = Slopes(T, E);
        }

        public void ScaleWidths(float ka, float kb)
        {
            for (int i = 0; i < A.Length; i++)
            {
                A[i] *= ka;
                B[i] *= kb;
            }
            Prepare();
        }

        /// <summary>Fritsch-Carlson monotone cubic slopes: smooth curves with no overshoot (radii never go negative).</summary>
        private static float[] Slopes(float[] t, float[] p)
        {
            int n = p.Length;
            float[] m = new float[n];
            if (n < 2) return m;
            float[] d = new float[n - 1];
            for (int i = 0; i < n - 1; i++) d[i] = (p[i + 1] - p[i]) / Mathf.Max(1e-6f, t[i + 1] - t[i]);
            m[0] = d[0];
            m[n - 1] = d[n - 2];
            for (int i = 1; i < n - 1; i++) m[i] = d[i - 1] * d[i] <= 0f ? 0f : (d[i - 1] + d[i]) * 0.5f;
            for (int i = 0; i < n - 1; i++)
            {
                if (Mathf.Abs(d[i]) < 1e-9f) { m[i] = 0f; m[i + 1] = 0f; continue; }
                float a = m[i] / d[i], b = m[i + 1] / d[i];
                float s = a * a + b * b;
                if (s > 9f)
                {
                    float tau = 3f / Mathf.Sqrt(s);
                    m[i] = tau * a * d[i];
                    m[i + 1] = tau * b * d[i];
                }
            }
            return m;
        }

        private float Eval(float[] p, float[] m, float t)
        {
            int n = T.Length;
            if (t <= T[0]) return p[0];
            if (t >= T[n - 1]) return p[n - 1];
            int k = 0;
            while (k < n - 2 && t > T[k + 1]) k++;
            float h = T[k + 1] - T[k];
            float s = (t - T[k]) / h;
            float s2 = s * s, s3 = s2 * s;
            return (2f * s3 - 3f * s2 + 1f) * p[k] + (s3 - 2f * s2 + s) * h * m[k] + (-2f * s3 + 3f * s2) * p[k + 1] + (s3 - s2) * h * m[k + 1];
        }

        public float MinT { get { return T[0]; } }
        public float MaxT { get { return T[T.Length - 1]; } }

        public Vector3 Center(float t)
        {
            return P0 + (P1 - P0) * t + U * Eval(CU, _mCU, t) + V * Eval(CV, _mCV, t);
        }

        public void Ring(float t, out Vector3 c, out float a, out float b, out float e)
        {
            c = Center(t);
            a = Mathf.Max(0.0005f, Eval(A, _mA, t));
            b = Mathf.Max(0.0005f, Eval(B, _mB, t));
            e = Mathf.Max(1.6f, Eval(E, _mE, t));
        }

        /// <summary>One evaluated cross-section (centre, radii, exponent), reused for every point around it.</summary>
        public struct RingEval
        {
            public float T, A, B, E;
            public Vector3 C;
        }

        /// <summary>Finite-difference steps for normals and tangents (loft parameter and degrees).</summary>
        public const float StepT = 0.006f, StepTheta = 1.2f;

        public RingEval EvalRing(float t)
        {
            RingEval r;
            r.T = t;
            Ring(t, out r.C, out r.A, out r.B, out r.E);
            return r;
        }

        public Vector3 PointOn(ref RingEval ring, float thetaDeg, float offset)
        {
            float th = thetaDeg * Mathf.Deg2Rad;
            float cs = (float)Math.Cos(th), sn = (float)Math.Sin(th);
            float k = 2f / ring.E;
            float acs = cs < 0f ? -cs : cs, asn = sn < 0f ? -sn : sn;
            float px = ring.A * (cs < 0f ? -1f : 1f) * (acs < 1e-7f ? 0f : (float)Math.Pow(acs, k));
            float pz = ring.B * (sn < 0f ? -1f : 1f) * (asn < 1e-7f ? 0f : (float)Math.Pow(asn, k));
            Vector3 radial = new Vector3(U.x * px + V.x * pz, U.y * px + V.y * pz, U.z * px + V.z * pz);
            float r = (float)Math.Sqrt(radial.x * radial.x + radial.y * radial.y + radial.z * radial.z);
            Vector3 dir = r > 1e-9f ? radial / r : (U * cs + V * sn);
            float bump = Bump != null ? Bump(ring.T, thetaDeg) : 0f;
            return ring.C + radial + dir * (bump + offset);
        }

        public Vector3 Point(float t, float thetaDeg, float offset)
        {
            RingEval r = EvalRing(t);
            return PointOn(ref r, thetaDeg, offset);
        }

        /// <summary>
        /// Surface point, outward normal and tangent (along the ring, w = bitangent sign) from five samples. The rings
        /// at t - StepT, t and t + StepT are passed in so a whole ring of vertices evaluates its cross-sections once.
        /// </summary>
        public void Frame(ref RingEval below, ref RingEval mid, ref RingEval above, float thetaDeg, out Vector3 p, out Vector3 n, out Vector4 tangent)
        {
            p = PointOn(ref mid, thetaDeg, 0f);
            Vector3 dt = PointOn(ref above, thetaDeg, 0f) - PointOn(ref below, thetaDeg, 0f);
            Vector3 dth = PointOn(ref mid, thetaDeg + StepTheta, 0f) - PointOn(ref mid, thetaDeg - StepTheta, 0f);
            n = Vector3.Cross(dt, dth);
            Vector3 outward = p - mid.C;
            if (n.sqrMagnitude < 1e-24f) n = outward;
            if (n.sqrMagnitude < 1e-24f) n = (P1 - P0);
            n = Nrm(n);
            if (Vector3.Dot(n, outward) < 0f) n = -n;
            Vector3 tu = dth - n * Vector3.Dot(dth, n);
            if (tu.sqrMagnitude < 1e-12f) tu = Vector3.Cross(n, Vector3.up);
            if (tu.sqrMagnitude < 1e-12f) tu = Vector3.Cross(n, Vector3.right);
            tu = Nrm(tu);
            float w = Vector3.Dot(Vector3.Cross(n, tu), dt) < 0f ? -1f : 1f;
            tangent = new Vector4(tu.x, tu.y, tu.z, w);
        }

        public void Frame(float t, float thetaDeg, out Vector3 p, out Vector3 n)
        {
            RingEval below = EvalRing(t - StepT), mid = EvalRing(t), above = EvalRing(t + StepT);
            Vector4 tangent;
            Frame(ref below, ref mid, ref above, thetaDeg, out p, out n, out tangent);
        }

        public Vector3 Normal(float t, float thetaDeg)
        {
            Vector3 p, n;
            Frame(t, thetaDeg, out p, out n);
            return n;
        }

        public BoneWeight WeightAt(float t, float theta)
        {
            return Weight != null ? Weight(t, theta) : BodyBuilder.W1(0);
        }

        /// <summary>Exact normalize. Vector3.Normalize zeroes anything shorter than 1e-5, which tiny finite differences hit.</summary>
        public static Vector3 Nrm(Vector3 v)
        {
            float m = Mathf.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
            return m > 1e-20f ? v / m : Vector3.zero;
        }
    }

    /// <summary>
    /// Procedural futuristic humans: anatomical lofted bodies (pecs, glutes, calves, deltoids, collarbones), sculpted
    /// heads with real noses, lips, brows, eyes and ears, eleven hair styles and headwear, street clothes, techwear,
    /// bodysuits and tactical armour with glowing seams, implants and gear plates. Smooth four-bone skinning.
    /// Pure C# (no engine calls), so the exact output can be inspected offline. Humanoid creatures (Appearance.Creature)
    /// are built by the creature half of this class in BodyBuilder.Creatures.cs.
    /// </summary>
    public sealed partial class BodyBuilder
    {
        public const int PaletteSize = 16;

        private readonly BodyData _d = new BodyData();
        private readonly Appearance _look;
        private readonly RigLayout _L;
        private readonly int _detail;
        private readonly float _s;
        private Loft _torso, _neck, _head;
        private readonly Loft[] _upper = new Loft[2], _fore = new Loft[2], _palm = new Loft[2], _thigh = new Loft[2], _shin = new Loft[2], _foot = new Loft[2];
        private Vector3 _headCenter;

        private BodyBuilder(Appearance look, RigLayout layout, int detail)
        {
            _look = look;
            _L = layout;
            _detail = Mathf.Clamp(detail, 0, 2);
            _s = layout.Scale;
        }

        /// <summary>detail: 0 crowd, 1 standard, 2 hero (player, companions, elites).</summary>
        public static BodyData Build(Appearance look, RigLayout layout, int detail)
        {
            BodyBuilder b = new BodyBuilder(look, layout, detail);
            if (look.Creature != HumanoidCreature.None) b.BuildCreature();
            else b.BuildAll();
            return b._d;
        }

        // ================================================================== palette

        public static Vector2 Pal(int row, int col)
        {
            return new Vector2((Mathf.Clamp(col, 0, PaletteSize - 1) + 0.5f) / PaletteSize, (Mathf.Clamp(row, 0, PaletteSize - 1) + 0.5f) / PaletteSize);
        }

        private Vector2 SkinUV { get { return Pal(0, _look.Skin); } }
        private Vector2 HairUV { get { return Pal(2, _look.Hair); } }
        /// <summary>A darker natural shade of the hair colour for beards, brows and shaved stubble.</summary>
        private Vector2 BeardUV { get { return Pal(2, CharacterPaletteData.BeardShadeOffset + Mathf.Clamp(_look.Hair, 0, 5)); } }
        private Vector2 ShirtUV { get { return Pal(_look.ShirtRow, _look.Shirt); } }
        private Vector2 PantsUV { get { return Pal(_look.PantsRow, _look.Pants); } }
        private Vector2 ShoesUV { get { return Pal(_look.ShoesRow, _look.Shoes); } }
        private Vector2 JacketUV { get { return _look.Jacket ? Pal(_look.JacketRow, _look.JacketColor) : ShirtUV; } }
        private Vector2 GlowUV { get { return Pal(4, Mathf.Max(0, _look.Glow - 1)); } }
        private bool HasGlow { get { return _look.Glow > 0; } }
        private Vector2 ArmorUV { get { return Pal(5, _look.ArmorColor); } }
        private Vector2 DarkTechUV { get { return Pal(5, 2); } }
        /// <summary>Accent for knee guards and trims: gunmetal on dark clothes, graphite on mid tones, white gloss on light ones.</summary>
        private Vector2 TrimUV
        {
            get
            {
                Color c = CharacterPaletteData.Albedo(_look.PantsRow, _look.Pants);
                float lum = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
                return lum < 0.32f ? Pal(5, 0) : (lum > 0.6f ? Pal(5, 3) : Pal(5, 10));
            }
        }
        private Vector2 SoleUV { get { return Pal(5, 15); } }
        private Vector2 LipUV { get { return Pal(7, _look.Skin); } }
        private Vector2 HatUV { get { return Pal(_look.HatRow, _look.Hat); } }
        private bool Suit { get { return _look.SuitStyle >= 2; } }
        private bool Gloves { get { return _look.SuitStyle >= 2 || _look.GauntletStyle > 0 || (_look.SuitStyle == 1 && (_look.Shirt & 1) == 1); } }
        private bool LongSleeves { get { return _look.LongSleeves || _look.Jacket || _look.SuitStyle >= 2; } }

        // ================================================================== weights

        public static BoneWeight W1(int b)
        {
            BoneWeight w = new BoneWeight();
            w.boneIndex0 = b;
            w.weight0 = 1f;
            return w;
        }

        /// <summary>Two-bone blend: wb is the weight of bone b.</summary>
        public static BoneWeight W2(int a, int b, float wb)
        {
            wb = Mathf.Clamp01(wb);
            if (wb < 0.001f) return W1(a);
            if (wb > 0.999f) return W1(b);
            BoneWeight w = new BoneWeight();
            if (wb > 0.5f)
            {
                w.boneIndex0 = b; w.weight0 = wb;
                w.boneIndex1 = a; w.weight1 = 1f - wb;
            }
            else
            {
                w.boneIndex0 = a; w.weight0 = 1f - wb;
                w.boneIndex1 = b; w.weight1 = wb;
            }
            return w;
        }

        public static BoneWeight W3(int a, int b, float wb, int c, float wc)
        {
            float wa = Mathf.Max(0f, 1f - wb - wc);
            int[] idx = { a, b, c };
            float[] ws = { wa, Mathf.Max(0f, wb), Mathf.Max(0f, wc) };
            // Sort descending (three elements).
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 2 - i; j++)
                    if (ws[j] < ws[j + 1]) { float tw = ws[j]; ws[j] = ws[j + 1]; ws[j + 1] = tw; int ti = idx[j]; idx[j] = idx[j + 1]; idx[j + 1] = ti; }
            float sum = ws[0] + ws[1] + ws[2];
            if (sum < 1e-6f) return W1(a);
            BoneWeight w = new BoneWeight();
            w.boneIndex0 = idx[0]; w.weight0 = ws[0] / sum;
            w.boneIndex1 = idx[1]; w.weight1 = ws[1] / sum;
            w.boneIndex2 = idx[2]; w.weight2 = ws[2] / sum;
            return w;
        }

        /// <summary>Gaussian bump (zero beyond three sigma, which also skips the exponential for far features).</summary>
        private static float G(float x, float mu, float sigma)
        {
            float d = (x - mu) / sigma;
            d *= d;
            return d > 9f ? 0f : (float)Math.Exp(-d);
        }

        /// <summary>Angular gaussian: theta and mu in degrees, wraps around.</summary>
        private static float GA(float theta, float mu, float sigma)
        {
            float a = (theta - mu) % 360f;
            if (a > 180f) a -= 360f;
            else if (a < -180f) a += 360f;
            float d = a / sigma;
            d *= d;
            return d > 9f ? 0f : (float)Math.Exp(-d);
        }

        private static float SS(float a, float b, float x)
        {
            return QMath.SmoothRange(a, b, x);
        }

        // ================================================================== emission

        private int AddVertex(Vector3 p, Vector3 n, Vector2 uv, Vector2 detail, BoneWeight w)
        {
            Vector3 t = Loft.Nrm(Vector3.Cross(n, Mathf.Abs(n.y) < 0.95f ? Vector3.up : Vector3.right));
            return AddVertex(p, n, uv, detail, w, new Vector4(t.x, t.y, t.z, 1f));
        }

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

        /// <summary>Adds a triangle wound so its face points along the vertex normals (Unity front faces are clockwise).</summary>
        private void Tri(int a, int b, int c)
        {
            List<Vector3> v = _d.Vertices;
            Vector3 n = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
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

        /// <summary>Theta (degrees) for column j of segs: uniform, or concentrated toward the front (90 degrees).</summary>
        private static float Theta(int j, int segs, bool frontDense)
        {
            float s = (float)j / segs;
            if (!frontDense) return s * 360f;
            float u = s * 2f - 1f;
            float d = 180f * (0.42f * u + 0.58f * Mathf.Sign(u) * Mathf.Pow(Mathf.Abs(u), 1.8f));
            return 270f + d + 180f;
        }

        /// <summary>
        /// Emits a loft between t0 and t1. region(t) gives the palette cell; rings are duplicated where it changes so
        /// colours never bleed. Caps close the ends.
        /// </summary>
        private void EmitLoft(Loft lf, float t0, float t1, int rings, int segs, Func<float, Vector2> region, bool capStart, bool capEnd, bool frontDense = false, float[] ringTs = null)
        {
            int count = ringTs != null ? ringTs.Length : rings;
            int[] startIdx = new int[count];
            int[] endIdx = new int[count];
            float[] ts = new float[count];
            for (int r = 0; r < count; r++) ts[r] = ringTs != null ? ringTs[r] : Mathf.Lerp(t0, t1, (float)r / (count - 1));
            float vAcc = 0f;
            Vector3 prevC = lf.Center(ts[0]);
            Vector2 prevUV = region(ts[0]);
            for (int r = 0; r < count; r++)
            {
                float t = ts[r];
                Vector3 c = lf.Center(t);
                vAcc += (c - prevC).magnitude;
                prevC = c;
                Vector2 uvAbove = r > 0 ? region((ts[r - 1] + t) * 0.5f) : region(t);
                Vector2 uvBelow = r < count - 1 ? region((t + ts[r + 1]) * 0.5f) : uvAbove;
                endIdx[r] = EmitRing(lf, t, segs, uvAbove, vAcc, frontDense);
                startIdx[r] = uvBelow == uvAbove ? endIdx[r] : EmitRing(lf, t, segs, uvBelow, vAcc, frontDense);
            }
            for (int r = 0; r < count - 1; r++)
            {
                int a0 = startIdx[r], b0 = endIdx[r + 1];
                for (int j = 0; j < segs; j++) Quad(a0 + j, a0 + j + 1, b0 + j + 1, b0 + j);
            }
            if (capStart) Cap(lf, ts[0], startIdx[0], segs, region(ts[0]), true);
            if (capEnd) Cap(lf, ts[count - 1], endIdx[count - 1], segs, region(ts[count - 1]), false);
        }

        private int EmitRing(Loft lf, float t, int segs, Vector2 uv, float v, bool frontDense)
        {
            int first = _d.Vertices.Count;
            Vector3 c;
            float a, b, e;
            lf.Ring(t, out c, out a, out b, out e);
            float perimeter = Mathf.PI * (a + b);
            // Tangent follows the detail U (around the ring), bitangent runs along the loft.
            Loft.RingEval below = lf.EvalRing(t - Loft.StepT), mid = lf.EvalRing(t), above = lf.EvalRing(t + Loft.StepT);
            for (int j = 0; j <= segs; j++)
            {
                float th = Theta(j, segs, frontDense);
                Vector3 p, n;
                Vector4 tangent;
                lf.Frame(ref below, ref mid, ref above, th, out p, out n, out tangent);
                Vector2 det = new Vector2((float)j / segs * perimeter / 0.07f, v / 0.07f);
                AddVertex(p, n, uv, det, lf.WeightAt(t, th), tangent);
            }
            return first;
        }

        private void Cap(Loft lf, float t, int ringStart, int segs, Vector2 uv, bool start)
        {
            Vector3 c;
            float a, b, e;
            lf.Ring(t, out c, out a, out b, out e);
            Vector3 axis = Loft.Nrm(lf.P1 - lf.P0);
            Vector3 n = start ? -axis : axis;
            Vector3 tip = c + n * Mathf.Min(a, b) * 0.35f;
            int ci = AddVertex(tip, n, uv, Vector2.zero, lf.WeightAt(t, 0f));
            for (int j = 0; j < segs; j++) Tri(ci, ringStart + j, ringStart + j + 1);
        }

        /// <summary>
        /// A curved panel lying on a loft (armour plate, jacket shell, visor). thickness > 0 adds side walls so it
        /// reads as a solid piece.
        /// </summary>
        private void EmitPatch(Loft lf, float t0, float t1, float th0, float th1, int nt, int nth, float offset, float thickness, Vector2 uv, Vector2 edgeUV)
        {
            int[,] top = new int[nt + 1, nth + 1];
            for (int i = 0; i <= nt; i++)
            {
                float t = Mathf.Lerp(t0, t1, (float)i / nt);
                for (int j = 0; j <= nth; j++)
                {
                    float th = Mathf.Lerp(th0, th1, (float)j / nth);
                    Vector3 sp, n;
                    lf.Frame(t, th, out sp, out n);
                    Vector3 p = sp + n * (offset + thickness);
                    top[i, j] = AddVertex(p, n, uv, new Vector2(j * 0.3f, i * 0.3f), lf.WeightAt(t, th));
                }
            }
            for (int i = 0; i < nt; i++)
                for (int j = 0; j < nth; j++) Quad(top[i, j], top[i, j + 1], top[i + 1, j + 1], top[i + 1, j]);
            if (thickness <= 0f) return;
            // Side walls around the border, facing outward from the panel.
            List<Vector2> border = new List<Vector2>();
            for (int j = 0; j <= nth; j++) border.Add(new Vector2(0, j));
            for (int i = 1; i <= nt; i++) border.Add(new Vector2(i, nth));
            for (int j = nth - 1; j >= 0; j--) border.Add(new Vector2(nt, j));
            for (int i = nt - 1; i >= 1; i--) border.Add(new Vector2(i, 0));
            border.Add(new Vector2(0, 0));
            float tm = (t0 + t1) * 0.5f, thm = (th0 + th1) * 0.5f;
            Vector3 mid = lf.Point(tm, thm, 0f) + lf.Normal(tm, thm) * offset;
            for (int k = 0; k < border.Count - 1; k++)
            {
                Vector2 pa = border[k], pb = border[k + 1];
                float ta = Mathf.Lerp(t0, t1, pa.x / nt), tha = Mathf.Lerp(th0, th1, pa.y / nth);
                float tb = Mathf.Lerp(t0, t1, pb.x / nt), thb = Mathf.Lerp(th0, th1, pb.y / nth);
                Vector3 na = lf.Normal(ta, tha), nb = lf.Normal(tb, thb);
                Vector3 sa = lf.Point(ta, tha, 0f), sb = lf.Point(tb, thb, 0f);
                Vector3 a0 = sa + na * offset, a1 = sa + na * (offset + thickness);
                Vector3 b0 = sb + nb * offset, b1 = sb + nb * (offset + thickness);
                Vector3 along = b0 - a0;
                Vector3 nrm = Vector3.Cross(along, na);
                Vector3 toOut = (a0 + b0) * 0.5f - mid;
                if (nrm.sqrMagnitude < 1e-12f) continue;
                nrm = Loft.Nrm(nrm);
                if (Vector3.Dot(nrm, toOut) < 0f) nrm = -nrm;
                BoneWeight wa = lf.WeightAt(ta, tha), wb = lf.WeightAt(tb, thb);
                int i0 = AddVertex(a0, nrm, edgeUV, Vector2.zero, wa);
                int i1 = AddVertex(a1, nrm, edgeUV, Vector2.zero, wa);
                int i2 = AddVertex(b1, nrm, edgeUV, Vector2.zero, wb);
                int i3 = AddVertex(b0, nrm, edgeUV, Vector2.zero, wb);
                Quad(i0, i1, i2, i3);
            }
        }

        /// <summary>A thin glowing strip that follows a loft at a fixed angle (suit seams, implants).</summary>
        private void EmitRibbon(Loft lf, float t0, float t1, float theta, float width, int n, Vector2 uv, float offset = 0.0025f)
        {
            int prevA = -1, prevB = -1;
            for (int i = 0; i <= n; i++)
            {
                float t = Mathf.Lerp(t0, t1, (float)i / n);
                Vector3 nrm = lf.Normal(t, theta);
                Vector3 p = lf.Point(t, theta, 0f) + nrm * offset;
                Vector3 around = lf.Point(t, theta + 2f, 0f) - lf.Point(t, theta - 2f, 0f);
                if (around.sqrMagnitude < 1e-12f) around = Vector3.Cross(nrm, (lf.P1 - lf.P0));
                around = Loft.Nrm(around) * (width * 0.5f);
                BoneWeight w = lf.WeightAt(t, theta);
                int a = AddVertex(p - around, nrm, uv, Vector2.zero, w);
                int b = AddVertex(p + around, nrm, uv, Vector2.zero, w);
                if (prevA >= 0) Quad(prevA, prevB, b, a);
                prevA = a;
                prevB = b;
            }
        }

        /// <summary>A ring-shaped glowing band around a loft between two angles (horizontal seams, cuffs).</summary>
        private void EmitBand(Loft lf, float t, float th0, float th1, float height, int n, Vector2 uv, float offset = 0.0025f)
        {
            float span = Mathf.Max(0.0001f, lf.MaxT - lf.MinT);
            float axisLen = Mathf.Max(0.01f, (lf.P1 - lf.P0).magnitude);
            float dt = height / axisLen * 0.5f;
            EmitPatch(lf, t - dt, t + dt, th0, th1, 1, n, offset, 0f, uv, uv);
        }

        /// <summary>An ellipsoid (eyes, ears, buns, pouches) rigid to one weight.</summary>
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
                    Vector3 n = Loft.Nrm(new Vector3(unit.x / Mathf.Max(1e-5f, radii.x), unit.y / Mathf.Max(1e-5f, radii.y), unit.z / Mathf.Max(1e-5f, radii.z)));
                    AddVertex(center + rot * p, rot * n, uv, new Vector2(j * 0.25f, r * 0.25f), w);
                }
            }
            for (int r = 0; r < rings; r++)
                for (int j = 0; j < segs; j++)
                {
                    int a = first + r * (segs + 1) + j;
                    int b = a + segs + 1;
                    Quad(a, a + 1, b + 1, b);
                }
        }

        /// <summary>
        /// A layer fitted over the head (hair, beard, caps, masks): rows run from yTop down to edge(theta);
        /// thickness(v, theta) shapes the volume and thins to the scalp at the edge.
        /// </summary>
        private void EmitShell(float yTop, Func<float, float> edgeY, float thFrom, float thTo, int rows, int cols, Func<float, float, float> thickness, Func<float, float, Vector2> uv)
        {
            int[,] idx = new int[rows + 1, cols + 1];
            for (int i = 0; i <= rows; i++)
            {
                float v = (float)i / rows;
                for (int j = 0; j <= cols; j++)
                {
                    float th = Mathf.Lerp(thFrom, thTo, (float)j / cols);
                    float y = Mathf.Lerp(yTop, edgeY(th), v);
                    float t = HeadT(y);
                    float th2 = th;
                    Vector3 n = _head.Normal(t, th2);
                    Vector3 p = _head.Point(t, th2, 0f) + n * thickness(v, th);
                    idx[i, j] = AddVertex(p, n, uv(v, th), new Vector2(j * 0.4f, i * 0.4f), W1(RigLayout.Head));
                }
            }
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++) Quad(idx[i, j], idx[i, j + 1], idx[i + 1, j + 1], idx[i + 1, j]);
        }

        // ================================================================== build

        private int Segs(int hero, int std, int crowd)
        {
            return _detail == 2 ? hero : (_detail == 1 ? std : crowd);
        }

        private void BuildAll()
        {
            BuildTorso();
            BuildNeck();
            BuildHead();
            for (int side = 0; side < 2; side++)
            {
                BuildArm(side);
                BuildHand(side);
                BuildLeg(side);
                BuildFoot(side);
            }
            BuildClothing();
            BuildHair();
            BuildFace();
            BuildHeadwear();
            BuildGear();
            BuildImplants();
            BuildCrystalTailoring04F();
            BuildFaceAnatomy04F();
            BuildPresentation04G();
        }

        // ------------------------------------------------------------------ torso

        private float BulkW { get { return Mathf.Lerp(0.92f, 1.14f, Mathf.InverseLerp(0.75f, 1.4f, _L.Bulk)); } }

        /// <summary>Torso loft extent in unscaled body heights (crotch to the base of the neck).</summary>
        private const float TorsoBottom = 0.785f, TorsoTop = 1.55f;

        private void BuildTorso()
        {
            float s = _s;
            float y0 = TorsoBottom * s, y1 = TorsoTop * s;
            // The top rings form the trapezius slope: wide at the shoulder line, flattening onto the neck, so the arms
            // hang from one continuous shoulder instead of sitting on top like pads.
            float[] ys = { 0.785f, 0.81f, 0.86f, 0.93f, 0.99f, 1.05f, 1.12f, 1.2f, 1.29f, 1.37f, 1.43f, 1.468f, 1.49f, 1.505f, 1.52f, 1.535f, 1.55f };
            float[] a = { 0.035f, 0.105f, 0.16f, 0.168f, 0.158f, 0.145f, 0.15f, 0.163f, 0.176f, 0.186f, 0.19f, 0.188f, 0.178f, 0.155f, 0.12f, 0.088f, 0f };
            float[] b = { 0.035f, 0.085f, 0.108f, 0.112f, 0.106f, 0.098f, 0.102f, 0.11f, 0.117f, 0.116f, 0.104f, 0.092f, 0.085f, 0.079f, 0.072f, 0.066f, 0f };
            float[] cv = { 0f, -0.005f, -0.012f, -0.012f, -0.004f, 0.004f, 0.01f, 0.014f, 0.014f, 0.008f, -0.002f, -0.01f, -0.012f, -0.012f, -0.01f, -0.008f, -0.004f };
            float[] e = { 2f, 2.2f, 2.3f, 2.4f, 2.4f, 2.3f, 2.3f, 2.35f, 2.4f, 2.4f, 2.35f, 2.3f, 2.25f, 2.2f, 2.1f, 2.05f, 2f };
            bool f = _look.Female;
            float bw = BulkW;
            float[] keys = new float[ys.Length * 6];
            for (int i = 0; i < ys.Length; i++)
            {
                float y = ys[i];
                float ai = a[i] * bw;
                float bi = b[i] * Mathf.Lerp(0.95f, 1.1f, Mathf.InverseLerp(0.75f, 1.4f, _L.Bulk));
                if (f)
                {
                    ai += 0.013f * G(y, 0.9f, 0.06f);
                    ai -= 0.014f * G(y, 1.08f, 0.05f);
                    bi -= 0.006f * G(y, 1.08f, 0.06f);
                }
                ai *= Mathf.Lerp(1f, _L.ShoulderWidth, SS(1.2f, 1.42f, y));
                if (i == ys.Length - 1)
                {
                    // The top ring tucks inside the neck so no gap opens around it (the neck does not scale with bulk).
                    float nk = f ? 0.86f : 1f;
                    ai = 0.054f * nk;
                    bi = 0.05f * nk;
                }
                keys[i * 6] = (y * s - y0) / (y1 - y0);
                keys[i * 6 + 1] = ai * s;
                keys[i * 6 + 2] = bi * s;
                keys[i * 6 + 3] = 0f;
                keys[i * 6 + 4] = cv[i] * s;
                keys[i * 6 + 5] = e[i];
            }
            Loft L = new Loft(new Vector3(0f, y0, 0f), new Vector3(0f, y1, 0f), Vector3.right, Vector3.forward, keys);
            float span = y1 - y0;
            L.Bump = (t, th) =>
            {
                float y = (y0 + t * span) / s;
                float k = 0f;
                if (f) k += 0.021f * G(y, 1.29f, 0.045f) * (GA(th, 68f, 17f) + GA(th, 112f, 17f));
                else k += 0.008f * G(y, 1.3f, 0.05f) * (GA(th, 65f, 18f) + GA(th, 115f, 18f));
                k += 0.012f * G(y, 0.88f, 0.05f) * (GA(th, 245f, 22f) + GA(th, 295f, 22f));
                k += 0.006f * G(y, 1.36f, 0.06f) * (GA(th, 238f, 20f) + GA(th, 302f, 20f));
                k += 0.0035f * G(y, 1.12f, 0.05f) * GA(th, 90f, 22f);
                k -= 0.004f * GA(th, 270f, 8f) * SS(0.95f, 1.05f, y) * (1f - SS(1.42f, 1.48f, y));
                k += 0.004f * G(y, 1.455f, 0.012f) * (GA(th, 70f, 14f) + GA(th, 110f, 14f));
                return k * s;
            };
            float shoulderX = Mathf.Abs(_L.World[RigLayout.UpperArmR].x) / s;
            L.Weight = (t, th) =>
            {
                float y = (y0 + t * span) / s;
                float cs = Mathf.Cos(th * Mathf.Deg2Rad);
                if (y < 0.95f)
                {
                    float thighW = 0.42f * SS(0.93f, 0.8f, y) * SS(0.25f, 0.8f, Mathf.Abs(cs));
                    return W2(RigLayout.Hips, cs > 0f ? RigLayout.ThighR : RigLayout.ThighL, thighW);
                }
                if (y < 1.04f) return W2(RigLayout.Hips, RigLayout.Spine, SS(0.95f, 1.04f, y));
                if (y < 1.13f) return W1(RigLayout.Spine);
                if (y < 1.25f) return W2(RigLayout.Spine, RigLayout.Chest, SS(1.13f, 1.25f, y));
                if (y < 1.34f) return W1(RigLayout.Chest);
                // Upper chest: the outer shoulder follows the arm a little, the ring around the neck follows the neck.
                float x = Mathf.Abs(L.Point(t, th, 0f).x) / s;
                float wArm = 0.42f * SS(1.34f, 1.45f, y) * SS(shoulderX * 0.55f, shoulderX * 1.02f, x);
                float wNeck = 0.6f * SS(1.5f, 1.55f, y) * (1f - SS(0.05f, 0.085f, x));
                return W3(RigLayout.Chest, RigLayout.Neck, wNeck, cs > 0f ? RigLayout.UpperArmR : RigLayout.UpperArmL, wArm);
            };
            _torso = L;
            float belt = (0.985f * s - y0) / span;
            float beltH = 0.012f * s / span;
            // Rings: even down the body, packed at the shoulders where the slope turns onto the neck.
            List<float> ringTs = new List<float>();
            int bodyRings = Segs(22, 15, 10), topRings = Segs(12, 8, 5);
            float split = (1.4f * s - y0) / span;
            for (int i = 0; i < bodyRings; i++) ringTs.Add(split * i / bodyRings);
            for (int i = 0; i <= topRings; i++)
            {
                float u = (float)i / topRings;
                ringTs.Add(Mathf.Lerp(split, 1f, 1f - Mathf.Pow(1f - u, 1.6f)));
            }
            EmitLoft(L, 0f, 1f, 0, Segs(26, 20, 14), t =>
            {
                if (Suit) return ShirtUV;
                if (Mathf.Abs(t - belt) < beltH) return Pal(3, _look.SuitStyle == 1 ? 7 : 0);
                return t < belt ? PantsUV : ShirtUV;
            }, true, false, false, ringTs.ToArray());
        }

        // ------------------------------------------------------------------ neck

        private void BuildNeck()
        {
            float s = _s;
            Vector3 p0 = new Vector3(0f, 1.46f * s, -0.012f * s);
            Vector3 p1 = new Vector3(0f, 1.7f * s, 0.012f * s);
            float k = _look.Female ? 0.86f : 1f;
            float[] keys =
            {
                0f, 0.078f * s * k, 0.07f * s, 0f, -0.006f * s, 2f,
                0.25f, 0.063f * s * k, 0.06f * s * k, 0f, -0.002f * s, 2f,
                0.55f, 0.058f * s * k, 0.056f * s * k, 0f, 0.002f * s, 2f,
                0.85f, 0.055f * s * k, 0.055f * s * k, 0f, 0.004f * s, 2f,
                1f, 0.052f * s * k, 0.052f * s * k, 0f, 0.004f * s, 2f
            };
            Loft L = new Loft(p0, p1, Vector3.right, Vector3.forward, keys);
            bool male = !_look.Female;
            L.Bump = (t, th) => male ? 0.004f * s * G(t, 0.58f, 0.08f) * GA(th, 90f, 14f) : 0f;
            L.Weight = (t, th) =>
            {
                if (t < 0.32f) return W2(RigLayout.Chest, RigLayout.Neck, SS(0.02f, 0.32f, t));
                if (t < 0.72f) return W1(RigLayout.Neck);
                return W2(RigLayout.Neck, RigLayout.Head, SS(0.72f, 0.98f, t));
            };
            _neck = L;
            bool collar = Suit || _look.SuitStyle == 1;
            EmitLoft(L, 0f, 1f, Segs(8, 6, 5), Segs(18, 14, 10), t => collar && t < 0.38f ? ShirtUV : SkinUV, false, false);
        }

        // ------------------------------------------------------------------ head

        private float _headY0, _headY1;
        /// <summary>The face was designed long; heights below the ear line are compressed by this factor.</summary>
        private const float LowerFace = 0.76f;
        private const float UpperFace = 0.95f;

        /// <summary>Maps an actual head height back to the design height used by the face features.</summary>
        private static float FaceY(float yActual)
        {
            return yActual < 0f ? yActual / LowerFace : yActual / UpperFace;
        }

        /// <summary>Maps a design height (as written in the face tables) to the actual head height (unscaled).</summary>
        private static float HY(float yDesign)
        {
            return yDesign < 0f ? yDesign * LowerFace : yDesign * UpperFace;
        }

        private float HeadT(float yRel)
        {
            return Mathf.InverseLerp(_headY0, _headY1, yRel);
        }

        private void BuildHead()
        {
            float s = _s;
            Vector3 hb = _L.World[RigLayout.Head];
            _headCenter = hb + new Vector3(0f, 0.074f * s, 0.014f * s);
            bool f = _look.Female;
            float[] ys = { -0.128f, -0.118f, -0.1f, -0.08f, -0.055f, -0.025f, 0.005f, 0.035f, 0.07f, 0.1f, 0.125f, 0.143f, 0.153f };
            for (int i = 0; i < ys.Length; i++) ys[i] = ys[i] < 0f ? ys[i] * LowerFace : ys[i] * UpperFace;
            float[] a = { 0.012f, 0.03f, f ? 0.04f : 0.046f, f ? 0.052f : 0.058f, f ? 0.064f : 0.068f, 0.075f, 0.079f, 0.082f, 0.082f, 0.075f, 0.058f, 0.034f, 0.01f };
            float[] b = { 0.012f, 0.034f, 0.058f, 0.074f, 0.086f, 0.094f, 0.099f, 0.101f, 0.1f, 0.092f, 0.074f, 0.046f, 0.012f };
            float[] cv = { f ? 0.044f : 0.05f, f ? 0.04f : 0.044f, 0.03f, 0.016f, 0.006f, 0.002f, 0f, -0.004f, -0.008f, -0.012f, -0.014f, -0.014f, -0.014f };
            float[] e = { 2f, 2f, 2.1f, 2.2f, 2.2f, 2.25f, 2.3f, 2.3f, 2.2f, 2.1f, 2f, 2f, 2f };
            _headY0 = ys[0] * s;
            _headY1 = ys[ys.Length - 1] * s;
            float span = _headY1 - _headY0;
            float[] keys = new float[ys.Length * 6];
            for (int i = 0; i < ys.Length; i++)
            {
                keys[i * 6] = (ys[i] * s - _headY0) / span;
                keys[i * 6 + 1] = a[i] * s;
                keys[i * 6 + 2] = b[i] * s;
                keys[i * 6 + 3] = 0f;
                keys[i * 6 + 4] = cv[i] * s;
                keys[i * 6 + 5] = e[i];
            }
            Loft L = new Loft(_headCenter + new Vector3(0f, _headY0, 0f), _headCenter + new Vector3(0f, _headY1, 0f), Vector3.right, Vector3.forward, keys);
            float nose = f ? 0.78f : 1f, lips = f ? 1.3f : 1f, brow = f ? 0.6f : 1f, chin = f ? 0.6f : 1f;
            L.Bump = (t, th) =>
            {
                float y = FaceY((_headY0 + t * span) / s);
                float d = Mathf.DeltaAngle(90f, th);
                float k = 0f;
                // Nose: base, tip, bridge, with flared nostrils.
                if (y > -0.056f && y < 0.045f)
                {
                    float h;
                    if (y < -0.035f) h = Mathf.Lerp(0.008f, 0.024f, (y + 0.056f) / 0.021f);
                    else if (y < 0.0f) h = Mathf.Lerp(0.024f, 0.012f, (y + 0.035f) / 0.035f);
                    else h = Mathf.Lerp(0.012f, 0.002f, y / 0.045f);
                    float w = y < -0.03f ? 11f : (y < 0.01f ? 8f : 6.5f);
                    k += h * nose * GA(d, 0f, w);
                    k += 0.006f * nose * G(y, -0.043f, 0.007f) * (GA(d, -11f, 5f) + GA(d, 11f, 5f));
                }
                k -= 0.007f * G(y, 0.032f, 0.014f) * (GA(d, -21f, 9f) + GA(d, 21f, 9f));
                k += 0.005f * brow * G(y, 0.052f, 0.01f) * (GA(d, -22f, 14f) + GA(d, 22f, 14f));
                k += 0.006f * G(y, -0.008f, 0.022f) * (GA(d, -46f, 16f) + GA(d, 46f, 16f));
                k += 0.004f * lips * G(y, -0.062f, 0.007f) * GA(d, 0f, 13f);
                k += 0.005f * lips * G(y, -0.077f, 0.007f) * GA(d, 0f, 12f);
                k -= 0.003f * G(y, -0.0695f, 0.003f) * GA(d, 0f, 14f);
                k += 0.007f * chin * G(y, -0.104f, 0.014f) * GA(d, 0f, 20f);
                k += 0.004f * chin * G(y, -0.075f, 0.02f) * (GA(d, -70f, 15f) + GA(d, 70f, 15f));
                k += 0.008f * G(y, 0.04f, 0.05f) * GA(th, 270f, 55f);
                k -= 0.003f * G(y, 0.05f, 0.02f) * (GA(d, -80f, 12f) + GA(d, 80f, 12f));
                return k * s;
            };
            L.Weight = (t, th) => W1(RigLayout.Head);
            _head = L;
            // Rings concentrated on the face (lips, nose and eyes need the resolution).
            List<float> rt = new List<float>();
            float step = _detail == 2 ? 0.0055f : (_detail == 1 ? 0.008f : 0.012f);
            for (float y = -0.128f; y < 0.06f; y += step) rt.Add((HY(y) * s - _headY0) / span);
            for (float y = 0.06f; y <= 0.153f + 1e-4f; y += step * 1.8f) rt.Add((HY(Mathf.Min(y, 0.153f)) * s - _headY0) / span);
            if (rt[rt.Count - 1] < 0.999f) rt.Add(1f);
            EmitLoft(L, 0f, 1f, 0, Segs(40, 28, 18), t => SkinUV, true, true, true, rt.ToArray());
            // Ears.
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                Vector3 ec = _headCenter + new Vector3(sx * 0.081f * s, HY(0.008f) * s, -0.006f * s);
                EmitEllipsoid(ec, new Vector3(0.011f, 0.028f, 0.018f) * s, QMath.AY(sx * -12f) * QMath.AX(-8f), Segs(6, 5, 4), Segs(10, 8, 6), SkinUV, W1(RigLayout.Head));
            }
        }

        private void BuildFace()
        {
            float s = _s;
            float span = _headY1 - _headY0;
            int er = Segs(7, 6, 4), es = Segs(12, 10, 8);
            // Eyes: sclera, iris (lit for enhanced eyes), pupil.
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                float th = 90f - sx * 19f;
                float t = (HY(0.032f) * s - _headY0) / span;
                Vector3 surf = _head.Point(t, th, 0f);
                Vector3 fwd = Vector3.forward;
                Vector3 ec = surf - fwd * 0.0098f * s - Vector3.right * sx * 0.001f;
                float rEye = 0.0122f * s;
                EmitEllipsoid(ec, new Vector3(rEye, rEye, rEye), QMath.Identity, er, es, Pal(6, 6), W1(RigLayout.Head));
                Quaternion look = QMath.AY(sx * 4f);
                Vector3 front = ec + look * new Vector3(0f, 0f, rEye * 0.93f);
                EmitEllipsoid(front, new Vector3(0.0058f, 0.0058f, 0.0016f) * s, look, Segs(4, 3, 3), es, Pal(6, Mathf.Clamp(_look.EyeColor, 0, 15)), W1(RigLayout.Head));
                if (_look.EyeColor < 8) EmitEllipsoid(front + look * new Vector3(0f, 0f, 0.0011f * s), new Vector3(0.0025f, 0.0025f, 0.0008f) * s, look, 3, Segs(8, 6, 6), Pal(3, 0), W1(RigLayout.Head));
                // Upper eyelid: hides the top of the eyeball so the eyes look relaxed, not staring.
                float lo = sx > 0f ? 90f - 30f : 90f + 10f, hi = sx > 0f ? 90f - 10f : 90f + 30f;
                EmitPatch(_head, (HY(0.0355f) * s - _headY0) / span, (HY(0.046f) * s - _headY0) / span, lo, hi, 2, Segs(5, 4, 3), 0.0045f * s, 0.0012f * s, SkinUV, SkinUV);
                // Eyebrow: a thin band that follows the brow ridge.
                float blo = sx > 0f ? 90f - 34f : 90f + 8f, bhi = sx > 0f ? 90f - 8f : 90f + 34f;
                EmitPatch(_head, (HY(0.0505f) * s - _headY0) / span, (HY(0.0585f) * s - _headY0) / span, blo, bhi, 1, Segs(6, 4, 3), 0.0016f * s, 0.0012f * s, BeardUV, BeardUV);
            }
            // Lip tint and the mouth line.
            float tl = (HY(-0.0705f) * s - _headY0) / span;
            float dtl = 0.0085f * s / span;
            EmitPatch(_head, tl - dtl, tl + dtl, 81f, 99f, 2, Segs(6, 4, 3), 0.0006f * s, 0f, LipUV, LipUV);
            if (_look.Beard) BuildBeard();
        }

        private void BuildBeard()
        {
            float s = _s;
            float y0 = HY(-0.128f) * s;
            EmitShell(y0, th =>
            {
                float d = Mathf.Abs(Mathf.DeltaAngle(90f, th));
                if (d < 16f) return HY(-0.086f) * s;
                if (d < 40f) return HY(Mathf.Lerp(-0.086f, -0.05f, (d - 16f) / 24f)) * s;
                return HY(Mathf.Lerp(-0.05f, 0.0f, Mathf.Clamp01((d - 40f) / 50f))) * s;
            }, -5f, 185f, Segs(5, 4, 3), Segs(26, 18, 12), (v, th) => (0.0055f * (1f - v * 0.5f)) * s, (v, th) => BeardUV);
            // Moustache: a slim band that sits just above the upper lip.
            if (_detail == 0) return;
            float span = _headY1 - _headY0;
            float tm = (HY(-0.0565f) * s - _headY0) / span;
            EmitPatch(_head, tm - 0.0026f * s / span, tm + 0.0026f * s / span, 77f, 103f, 1, 8, 0.0014f * s, 0.0012f * s, BeardUV, BeardUV);
        }

        // ------------------------------------------------------------------ arms and hands

        private void BuildArm(int side)
        {
            float s = _s;
            float sx = side == 0 ? -1f : 1f;
            int up = side == 0 ? RigLayout.UpperArmL : RigLayout.UpperArmR;
            int fore = side == 0 ? RigLayout.ForearmL : RigLayout.ForearmR;
            int hand = side == 0 ? RigLayout.HandL : RigLayout.HandR;
            Vector3 j = _L.World[up], el = _L.World[fore], wr = _L.World[hand];
            float k = Mathf.Lerp(0.9f, 1.16f, Mathf.InverseLerp(0.75f, 1.4f, _L.Bulk)) * (_look.Female ? 0.86f : 1f);
            float[] uk =
            {
                -0.1f, 0.012f, 0.014f, -0.036f, 0f, 2f,
                -0.065f, 0.036f, 0.042f, -0.021f, 0f, 2.1f,
                -0.02f, 0.052f, 0.056f, -0.007f, 0.002f, 2.2f,
                0.05f, 0.059f, 0.06f, 0.0f, 0.003f, 2.2f,
                0.2f, 0.056f, 0.058f, 0.0f, 0.004f, 2.1f,
                0.42f, 0.051f, 0.056f, 0f, 0.006f, 2.1f,
                0.7f, 0.046f, 0.05f, 0f, 0.004f, 2f,
                0.92f, 0.042f, 0.044f, 0f, 0f, 2f,
                1.04f, 0.041f, 0.043f, 0f, -0.002f, 2f
            };
            for (int i = 0; i < uk.Length; i += 6)
            {
                uk[i + 1] *= s * k;
                uk[i + 2] *= s * k;
                uk[i + 3] *= s * sx;
                uk[i + 4] *= s;
            }
            Loft U = new Loft(j, el, Vector3.right, Vector3.forward, uk);
            float lat = side == 0 ? 180f : 0f;
            U.Bump = (t, th) => s * (0.005f * G(t, 0.45f, 0.18f) * GA(th, 90f, 35f) + 0.004f * G(t, 0.35f, 0.2f) * GA(th, 270f, 40f));
            U.Weight = (t, th) =>
            {
                if (t < 0.18f) return W2(up, RigLayout.Chest, 0.45f * (1f - SS(-0.1f, 0.18f, t)));
                if (t > 0.85f) return W2(up, fore, 0.5f * SS(0.85f, 1.04f, t));
                return W1(up);
            };
            _upper[side] = U;
            float[] fk =
            {
                -0.04f, 0.041f, 0.043f, 0f, -0.002f, 2f,
                0.12f, 0.046f, 0.045f, 0f, 0.002f, 2.1f,
                0.3f, 0.044f, 0.041f, 0f, 0.003f, 2.1f,
                0.55f, 0.034f, 0.037f, 0f, 0.002f, 2.2f,
                0.82f, 0.023f, 0.03f, 0f, 0f, 2.4f,
                1f, 0.021f, 0.029f, 0f, 0f, 2.4f
            };
            for (int i = 0; i < fk.Length; i += 6)
            {
                fk[i + 1] *= s * k;
                fk[i + 2] *= s * k;
                fk[i + 4] *= s;
            }
            Loft F = new Loft(el, wr, Vector3.right, Vector3.forward, fk);
            F.Weight = (t, th) =>
            {
                if (t < 0.15f) return W2(fore, up, 0.5f * (1f - SS(-0.04f, 0.15f, t)));
                if (t > 0.85f) return W2(fore, hand, 0.5f * SS(0.85f, 1f, t));
                return W1(fore);
            };
            _fore[side] = F;
            bool longS = LongSleeves;
            Vector2 sleeve = _look.Jacket ? JacketUV : ShirtUV;
            int ur = Segs(16, 12, 8), us = Segs(16, 12, 10);
            EmitLoft(U, -0.1f, 1.04f, ur, us, t => longS || t < 0.48f ? sleeve : SkinUV, true, false);
            EmitLoft(F, -0.04f, 1f, Segs(12, 9, 6), us, t => longS && t < 0.9f ? (t > 0.84f && !Suit ? Pal(3, 7) : sleeve) : (Gloves && t > 0.9f ? DarkTechUV : SkinUV), false, false);
        }

        private void BuildHand(int side)
        {
            float s = _s;
            float sx = side == 0 ? -1f : 1f;
            int hand = side == 0 ? RigLayout.HandL : RigLayout.HandR;
            int fore = side == 0 ? RigLayout.ForearmL : RigLayout.ForearmR;
            Vector3 wr = _L.World[hand];
            float hs = s * (_look.Female ? 0.9f : 1f);
            float palmLen = 0.1f * hs;
            float[] pk =
            {
                -0.05f, 0.021f, 0.029f, 0f, 0f, 2f,
                0.15f, 0.022f, 0.036f, 0f, 0.002f, 2.4f,
                0.5f, 0.02f, 0.043f, -0.002f, 0.002f, 2.8f,
                0.85f, 0.018f, 0.044f, -0.003f, 0f, 3f,
                1f, 0.016f, 0.04f, -0.004f, -0.002f, 2.8f
            };
            for (int i = 0; i < pk.Length; i += 6)
            {
                pk[i + 1] *= hs;
                pk[i + 2] *= hs;
                pk[i + 3] *= hs * sx;
                pk[i + 4] *= hs;
            }
            Loft P = new Loft(wr, wr + Vector3.down * palmLen, Vector3.right, Vector3.forward, pk);
            P.Weight = (t, th) => t < 0.12f ? W2(hand, fore, 0.3f * (1f - SS(-0.05f, 0.12f, t))) : W1(hand);
            _palm[side] = P;
            Vector2 uv = Gloves ? DarkTechUV : SkinUV;
            EmitLoft(P, -0.05f, 1f, Segs(7, 6, 4), Segs(14, 10, 8), t => uv, false, true);
            if (_detail == 0)
            {
                // Crowd: mitten fingers in one piece.
                float[] mk = { 0f, 0.014f, 0.038f, -0.004f * sx, -0.002f, 2.6f, 0.6f, 0.012f, 0.033f, -0.014f * sx, -0.001f, 2.4f, 1f, 0.008f, 0.02f, -0.03f * sx, 0f, 2.2f };
                for (int i = 0; i < mk.Length; i += 6) { mk[i + 1] *= hs; mk[i + 2] *= hs; mk[i + 3] *= hs; mk[i + 4] *= hs; }
                Loft M = new Loft(wr + Vector3.down * palmLen, wr + Vector3.down * (palmLen + 0.075f * hs), Vector3.right, Vector3.forward, mk);
                M.Weight = (t, th) => W1(hand);
                EmitLoft(M, 0f, 1f, 4, 8, t => uv, false, true);
            }
            else
            {
                BuildFingers04F(side, uv, hs);
            }
        }

        // ------------------------------------------------------------------ legs and feet

        private void BuildLeg(int side)
        {
            float s = _s;
            float sx = side == 0 ? -1f : 1f;
            int th = side == 0 ? RigLayout.ThighL : RigLayout.ThighR;
            int sh = side == 0 ? RigLayout.ShinL : RigLayout.ShinR;
            int ft = side == 0 ? RigLayout.FootL : RigLayout.FootR;
            Vector3 hip = _L.World[th], knee = _L.World[sh], ankle = _L.World[ft];
            float k = Mathf.Lerp(0.92f, 1.14f, Mathf.InverseLerp(0.75f, 1.4f, _L.Bulk)) * (_look.Female ? 0.95f : 1f);
            float fem = _look.Female ? 0.006f : 0f;
            float[] tk =
            {
                -0.16f, 0.07f, 0.085f, -0.025f, -0.012f, 2.2f,
                -0.04f, 0.085f + fem, 0.094f, -0.01f, -0.004f, 2.3f,
                0.1f, 0.088f + fem, 0.092f, 0f, 0.006f, 2.3f,
                0.3f, 0.08f + fem * 0.5f, 0.084f, 0.002f, 0.01f, 2.2f,
                0.55f, 0.07f, 0.072f, 0.002f, 0.01f, 2.1f,
                0.8f, 0.058f, 0.06f, 0f, 0.008f, 2f,
                0.95f, 0.052f, 0.056f, 0f, 0.01f, 2f,
                1.04f, 0.05f, 0.055f, 0f, 0.012f, 2f
            };
            for (int i = 0; i < tk.Length; i += 6)
            {
                tk[i + 1] *= s * k;
                tk[i + 2] *= s * k;
                tk[i + 3] *= s * sx;
                tk[i + 4] *= s;
            }
            Loft T = new Loft(hip, knee, Vector3.right, Vector3.forward, tk);
            T.Bump = (t, a) => s * (0.006f * G(t, 0.4f, 0.22f) * GA(a, 90f, 40f) + 0.008f * G(t, 1f, 0.05f) * GA(a, 90f, 25f));
            T.Weight = (t, a) =>
            {
                if (t < 0.08f) return W2(th, RigLayout.Hips, 0.45f * (1f - SS(-0.16f, 0.08f, t)));
                if (t > 0.85f) return W2(th, sh, 0.5f * SS(0.85f, 1.04f, t));
                return W1(th);
            };
            _thigh[side] = T;
            float[] sk =
            {
                -0.04f, 0.05f, 0.054f, 0f, 0.01f, 2f,
                0.1f, 0.052f, 0.058f, 0f, -0.004f, 2f,
                0.3f, 0.05f, 0.062f, 0f, -0.014f, 2.1f,
                0.55f, 0.04f, 0.046f, 0f, -0.006f, 2f,
                0.8f, 0.031f, 0.034f, 0f, -0.002f, 2f,
                0.97f, 0.03f, 0.032f, 0f, 0f, 2f,
                1.06f, 0.032f, 0.034f, 0f, 0.002f, 2f
            };
            for (int i = 0; i < sk.Length; i += 6)
            {
                sk[i + 1] *= s * k;
                sk[i + 2] *= s * k;
                sk[i + 4] *= s;
            }
            Loft S = new Loft(knee, ankle, Vector3.right, Vector3.forward, sk);
            S.Weight = (t, a) =>
            {
                if (t < 0.12f) return W2(sh, th, 0.5f * (1f - SS(-0.04f, 0.12f, t)));
                if (t > 0.9f) return W2(sh, ft, 0.4f * SS(0.9f, 1.06f, t));
                return W1(sh);
            };
            _shin[side] = S;
            int ls = Segs(16, 12, 10);
            EmitLoft(T, -0.16f, 1.04f, Segs(16, 11, 8), ls, t => PantsUV, false, false);
            EmitLoft(S, -0.04f, 1.06f, Segs(14, 10, 7), ls, t => PantsUV, false, false);
        }

        private void BuildFoot(int side)
        {
            float s = _s;
            float sx = side == 0 ? -1f : 1f;
            int ft = side == 0 ? RigLayout.FootL : RigLayout.FootR;
            int sh = side == 0 ? RigLayout.ShinL : RigLayout.ShinR;
            Vector3 ankle = _L.World[ft];
            float len = 0.265f * s;
            Vector3 p0 = new Vector3(ankle.x, 0f, ankle.z - 0.075f * s);
            Vector3 p1 = new Vector3(ankle.x + sx * 0.012f * s, 0f, ankle.z + 0.19f * s);
            float fs = s * (_look.Female ? 0.9f : 1f);
            float[] keys =
            {
                -0.02f, 0.02f, 0.03f, 0f, 0.03f, 2.5f,
                0.05f, 0.04f, 0.05f, 0f, 0.05f, 3f,
                0.25f, 0.046f, 0.062f, 0f, 0.062f, 3f,
                0.45f, 0.048f, 0.048f, 0f, 0.048f, 3.2f,
                0.7f, 0.05f, 0.034f, 0f, 0.034f, 3.4f,
                0.9f, 0.045f, 0.026f, 0f, 0.026f, 3f,
                1f, 0.03f, 0.02f, 0f, 0.02f, 2.6f,
                1.03f, 0.012f, 0.012f, 0f, 0.012f, 2.2f
            };
            for (int i = 0; i < keys.Length; i += 6)
            {
                keys[i + 1] *= fs;
                keys[i + 2] *= fs;
                keys[i + 4] *= fs;
            }
            Loft F = new Loft(p0, p1, Vector3.right, Vector3.up, keys);
            F.Weight = (t, th) => t > 0.12f && t < 0.38f && Mathf.Sin(th * Mathf.Deg2Rad) > 0.6f ? W2(ft, sh, 0.25f) : W1(ft);
            _foot[side] = F;
            EmitLoft(F, -0.02f, 1.03f, Segs(12, 9, 7), Segs(16, 12, 10), t => ShoesUV, true, true);
            // Sole.
            EmitPatch(F, 0f, 1f, 200f, 340f, Segs(8, 6, 4), Segs(6, 4, 3), 0.0025f * s, 0f, SoleUV, SoleUV);
            if (HasGlow && _look.SuitStyle >= 1)
            {
                EmitRibbon(F, 0.06f, 0.9f, sx > 0 ? 345f : 195f, 0.005f * s, 6, GlowUV, 0.003f * s);
            }
        }

        // ------------------------------------------------------------------ clothing

        private void BuildClothing()
        {
            float s = _s;
            float y0 = TorsoBottom * s, span = (TorsoTop - TorsoBottom) * s;
            Func<float, float> ty = y => (y * s - y0) / span;
            // Jacket: an outer shell over the torso, open at the front, with a collar.
            if (_look.Jacket)
            {
                float t0 = ty(0.9f), t1 = ty(1.532f);
                int nt = Segs(10, 8, 5), nth = Segs(12, 9, 6);
                EmitPatch(_torso, t0, t1, 104f, 436f, nt, nth * 2, 0.011f * s, 0.004f * s, JacketUV, JacketUV);
                EmitPatch(_neck, 0.02f, 0.4f, 125f, 415f, 2, nth, 0.012f * s, 0.004f * s, JacketUV, JacketUV);
                if (HasGlow && _look.SuitStyle >= 1)
                {
                    EmitRibbon(_torso, t0, t1, 104f, 0.006f * s, 8, GlowUV, 0.0165f * s);
                    EmitRibbon(_torso, t0, t1, 76f, 0.006f * s, 8, GlowUV, 0.0165f * s);
                }
            }
            if (HasGlow && _look.SuitStyle >= 1)
            {
                // Glowing seams: down the outside of the arms and legs, two lines on the chest.
                for (int side = 0; side < 2; side++)
                {
                    float lat = side == 0 ? 180f : 0f;
                    float w = 0.006f * s;
                    if (LongSleeves)
                    {
                        EmitRibbon(_upper[side], 0.02f, 0.97f, lat, w, 7, GlowUV, (_look.Jacket ? 0.004f : 0.0025f) * s);
                        EmitRibbon(_fore[side], 0.02f, 0.84f, lat, w, 6, GlowUV, 0.0025f * s);
                    }
                    EmitRibbon(_thigh[side], 0.02f, 0.97f, lat, w, 8, GlowUV);
                    EmitRibbon(_shin[side], 0.04f, 0.9f, lat, w, 7, GlowUV);
                }
                if (!_look.Jacket)
                {
                    EmitRibbon(_torso, ty(1.0f), ty(1.42f), 58f, 0.006f * s, 8, GlowUV);
                    EmitRibbon(_torso, ty(1.0f), ty(1.42f), 122f, 0.006f * s, 8, GlowUV);
                }
            }
            // Techwear and bodysuits: moulded knee guards in a tech trim tone, and a cargo pocket cut from the trousers.
            if ((_look.SuitStyle == 1 || _look.SuitStyle == 2) && _look.StriderStyle == 0)
            {
                Vector2 trim = TrimUV;
                for (int side = 0; side < 2; side++)
                {
                    EmitPatch(_thigh[side], 0.84f, 1.03f, 58f, 122f, 3, Segs(6, 4, 3), 0.003f * s, 0.004f * s, trim, trim);
                    if (HasGlow) EmitBand(_thigh[side], 0.86f, 62f, 118f, 0.004f * s, 4, GlowUV, 0.0078f * s);
                    if (_look.SuitStyle == 1)
                    {
                        float lat = side == 0 ? 180f : 0f;
                        EmitPatch(_thigh[side], 0.24f, 0.46f, lat - 24f, lat + 24f, 2, 3, 0.002f * s, 0.006f * s, PantsUV, PantsUV);
                    }
                }
            }
            // Tactical: a utility belt with pouches.
            if (_look.SuitStyle == 3)
            {
                float tb = ty(0.99f);
                EmitBand(_torso, tb, 0f, 360f, 0.03f * s, Segs(24, 16, 12), Pal(3, 0), 0.006f * s);
                for (int k = 0; k < 4; k++)
                {
                    float th = 30f + k * 40f + (k > 1 ? 80f : 0f);
                    EmitPatch(_torso, tb - 0.02f, tb + 0.035f, th - 9f, th + 9f, 1, 2, 0.008f * s, 0.016f * s, Pal(3, 7), Pal(3, 0));
                }
            }
        }

        // ------------------------------------------------------------------ hair and headwear

        private float Hairline(float th, bool full)
        {
            float d = Mathf.Abs(Mathf.DeltaAngle(90f, th));
            float s = _s;
            float front = full ? 0.088f : 0.085f;
            float temple = full ? 0.07f : 0.062f;
            float sides = full ? -0.01f : 0.034f;
            float back = full ? -0.075f : -0.056f;
            float y;
            if (d < 50f) y = Mathf.Lerp(front, temple, d / 50f);
            else if (d < 95f) y = Mathf.Lerp(temple, sides, (d - 50f) / 45f);
            else y = Mathf.Lerp(sides, back, (d - 95f) / 85f);
            return HY(y) * s;
        }

        private void BuildHair()
        {
            if (_look.Headwear == Headwear.Helmet) return;
            float s = _s;
            HairStyle hs = _look.HairStyle;
            if (hs == HairStyle.Bald) return;
            float top = HY(0.153f) * s;
            int rows = Segs(9, 7, 5), cols = Segs(36, 26, 18);
            bool capOn = _look.Headwear == Headwear.Cap || _look.Headwear == Headwear.PoliceCap || _look.Headwear == Headwear.Beanie;
            Vector2 hair = HairUV;
            Vector2 tips = HasGlow && (_look.Implant & 16) != 0 ? GlowUV : hair;
            // Shaved sides: a dark stubble shadow under dark hair; lighter hair shaved that short just shows the scalp.
            bool stubble = _look.Hair <= 1;
            Vector2 shaved = BeardUV;
            switch (hs)
            {
                case HairStyle.Short:
                    EmitShell(top, th => Hairline(th, false), 0f, 360f, rows, cols, (v, th) => s * Mathf.Lerp(0.011f, 0.003f, v * v), (v, th) => hair);
                    break;
                case HairStyle.Undercut:
                {
                    // Shaved sides and back, a long swept top that ends in a dyed fringe.
                    if (stubble) EmitShell(top, th => Hairline(th, false), 0f, 360f, rows, cols, (v, th) => s * 0.0022f, (v, th) => shaved);
                    EmitShell(top, th => HY(Mathf.Lerp(0.074f, 0.06f, Mathf.Abs(Mathf.DeltaAngle(90f, th)) / 180f)) * s, 0f, 360f, Segs(6, 5, 4), cols, (v, th) =>
                    {
                        float d = Mathf.Abs(Mathf.DeltaAngle(90f, th));
                        float swoop = 0.012f * Mathf.Clamp01(1f - d / 50f) * Mathf.Sin(v * Mathf.PI);
                        return s * (Mathf.Lerp(0.024f, 0.004f, v * v) + swoop);
                    }, (v, th) => v > 0.7f && Mathf.Abs(Mathf.DeltaAngle(90f, th)) < 60f ? tips : hair);
                    break;
                }
                case HairStyle.Mohawk:
                {
                    if (stubble) EmitShell(top, th => Hairline(th, false), 0f, 360f, rows, cols, (v, th) => s * 0.002f, (v, th) => shaved);
                    // The crest: a row of tall blades from forehead to nape.
                    for (int i = 0; i < 10; i++)
                    {
                        float u = i / 9f;
                        float thAt = u < 0.5f ? 90f : 270f;
                        float yy = u < 0.5f ? HY(Mathf.Lerp(0.09f, 0.152f, u * 2f)) * s : HY(Mathf.Lerp(0.152f, -0.03f, (u - 0.5f) * 2f)) * s;
                        Vector3 p = _head.Point(HeadT(yy), thAt, 0.002f * s);
                        Vector3 n = _head.Normal(HeadT(yy), thAt);
                        float hgt = Mathf.Lerp(0.03f, 0.055f, Mathf.Sin(u * Mathf.PI)) * s;
                        Quaternion q = QMath.FromBasis(Vector3.right, n, Loft.Nrm(Vector3.Cross(Vector3.right, n)));
                        EmitEllipsoid(p + n * hgt * 0.5f, new Vector3(0.006f * s, hgt * 0.55f, 0.016f * s), q, 4, 6, i > 6 && HasGlow ? tips : hair, W1(RigLayout.Head));
                    }
                    break;
                }
                case HairStyle.Long:
                case HairStyle.Braids:
                case HairStyle.Bun:
                {
                    bool full = hs == HairStyle.Long;
                    EmitShell(top, th => Hairline(th, full), 0f, 360f, rows, cols, (v, th) => s * Mathf.Lerp(full ? 0.015f : 0.011f, 0.003f, v * v), (v, th) => hair);
                    if (hs == HairStyle.Long && !capOn)
                    {
                        // A drape down the back to the shoulder blades.
                        Vector3 a0 = _headCenter + new Vector3(0f, 0.03f * s, -0.075f * s);
                        Vector3 a1 = _headCenter + new Vector3(0f, -0.31f * s, -0.1f * s);
                        float[] dk = { 0f, 0.075f * s, 0.03f * s, 0f, 0f, 2.4f, 0.35f, 0.095f * s, 0.032f * s, 0f, -0.012f * s, 2.6f, 0.8f, 0.105f * s, 0.026f * s, 0f, -0.03f * s, 2.8f, 1f, 0.095f * s, 0.016f * s, 0f, -0.035f * s, 2.6f };
                        Loft D = new Loft(a0, a1, Vector3.right, Vector3.forward, dk);
                        D.Weight = (t, th) => t < 0.35f ? W1(RigLayout.Head) : (t < 0.7f ? W2(RigLayout.Head, RigLayout.Neck, SS(0.35f, 0.7f, t)) : W2(RigLayout.Neck, RigLayout.Chest, SS(0.7f, 1f, t) * 0.7f));
                        EmitLoft(D, 0f, 1f, Segs(9, 7, 5), Segs(16, 12, 8), t => t > 0.85f && HasGlow && _look.SuitStyle > 0 ? tips : hair, false, true);
                    }
                    if (hs == HairStyle.Bun)
                    {
                        Vector3 bc = _headCenter + new Vector3(0f, HY(0.105f) * s, -0.075f * s);
                        EmitEllipsoid(bc, new Vector3(0.038f, 0.036f, 0.036f) * s, QMath.Identity, Segs(6, 5, 4), Segs(10, 8, 6), hair, W1(RigLayout.Head));
                    }
                    if (hs == HairStyle.Braids)
                    {
                        float[] ths = { 240f, 270f, 300f };
                        for (int b = 0; b < ths.Length; b++)
                        {
                            Vector3 bp = _head.Point(HeadT(HY(-0.02f) * s), ths[b], 0.004f * s);
                            Vector3 end = bp + new Vector3(Mathf.Cos(ths[b] * Mathf.Deg2Rad) * 0.03f * s, -0.24f * s, -0.04f * s);
                            float r = 0.011f * s;
                            float[] bk = { 0f, r, r, 0f, 0f, 2f, 0.9f, r * 0.8f, r * 0.8f, 0f, 0f, 2f, 1f, r * 0.5f, r * 0.5f, 0f, 0f, 2f };
                            Loft Bl = new Loft(bp, end, Vector3.right, Vector3.forward, bk);
                            Bl.Weight = (t, th) => t < 0.4f ? W1(RigLayout.Head) : W2(RigLayout.Head, RigLayout.Neck, SS(0.4f, 1f, t));
                            EmitLoft(Bl, 0f, 1f, 6, 6, t => t > 0.85f && HasGlow ? tips : hair, false, true);
                        }
                    }
                    break;
                }
            }
        }

        private void BuildHeadwear()
        {
            float s = _s;
            float top = HY(0.153f) * s;
            int cols = Segs(32, 24, 16);
            switch (_look.Headwear)
            {
                case Headwear.Cap:
                case Headwear.PoliceCap:
                {
                    bool police = _look.Headwear == Headwear.PoliceCap;
                    EmitShell(top, th => HY(0.03f) * s, 0f, 360f, Segs(6, 5, 4), cols, (v, th) => s * (0.016f + (police ? 0.012f * (1f - v) : 0f)), (v, th) => HatUV);
                    // Brim: a curved plate out front.
                    float tb = HeadT(HY(0.032f) * s);
                    int n = 10;
                    int[] inner = new int[n + 1], outer = new int[n + 1], innerB = new int[n + 1], outerB = new int[n + 1];
                    for (int i = 0; i <= n; i++)
                    {
                        float th = Mathf.Lerp(35f, 145f, (float)i / n);
                        Vector3 p = _head.Point(tb, th, 0.016f * s);
                        Vector3 dir = new Vector3(Mathf.Cos(th * Mathf.Deg2Rad), 0f, Mathf.Sin(th * Mathf.Deg2Rad));
                        float reach = Mathf.Lerp(0.035f, 0.075f, Mathf.Sin(Mathf.Lerp(0f, Mathf.PI, (float)i / n))) * s;
                        Vector3 q = p + dir * reach + Vector3.down * 0.012f * s;
                        inner[i] = AddVertex(p, Vector3.up, HatUV, Vector2.zero, W1(RigLayout.Head));
                        outer[i] = AddVertex(q, Vector3.up, HatUV, Vector2.zero, W1(RigLayout.Head));
                        innerB[i] = AddVertex(p - Vector3.up * 0.004f * s, Vector3.down, HatUV, Vector2.zero, W1(RigLayout.Head));
                        outerB[i] = AddVertex(q - Vector3.up * 0.004f * s, Vector3.down, HatUV, Vector2.zero, W1(RigLayout.Head));
                    }
                    for (int i = 0; i < n; i++)
                    {
                        Quad(inner[i], inner[i + 1], outer[i + 1], outer[i]);
                        Quad(innerB[i], outerB[i], outerB[i + 1], innerB[i + 1]);
                    }
                    if (police) EmitPatch(_head, HeadT(HY(0.05f) * s), HeadT(HY(0.075f) * s), 82f, 98f, 1, 2, 0.03f * s, 0.002f * s, Pal(3, 4), Pal(3, 4));
                    break;
                }
                case Headwear.Beanie:
                    EmitShell(top, th => HY(Mathf.Lerp(0.035f, -0.02f, Mathf.Abs(Mathf.DeltaAngle(90f, th)) / 180f)) * s, 0f, 360f, Segs(7, 5, 4), cols, (v, th) => s * (0.02f + 0.006f * (1f - v)), (v, th) => HatUV);
                    break;
                case Headwear.Helmet:
                {
                    EmitShell(top, th =>
                    {
                        float d = Mathf.Abs(Mathf.DeltaAngle(90f, th));
                        return HY(d < 60f ? 0.062f : Mathf.Lerp(0.062f, -0.075f, Mathf.Clamp01((d - 60f) / 50f))) * s;
                    }, 0f, 360f, Segs(8, 6, 4), cols, (v, th) => s * 0.03f, (v, th) => HatUV);
                    break;
                }
                case Headwear.Bandana:
                    EmitShell(HY(-0.03f) * s, th => HY(-0.125f) * s, -10f, 190f, Segs(4, 3, 3), Segs(20, 14, 10), (v, th) => s * 0.007f, (v, th) => HatUV);
                    break;
            }
            // Glasses become a slim visor band.
            if (_look.Glasses && _look.VisorStyle == 0)
                EmitPatch(_head, HeadT(HY(0.02f) * s), HeadT(HY(0.042f) * s), 30f, 150f, 1, Segs(14, 10, 8), 0.015f * s, 0.002f * s, HasGlow ? GlowUV : Pal(5, 13), Pal(5, 13));
        }

        // ------------------------------------------------------------------ gear plates

        private void BuildGear()
        {
            float s = _s;
            float y0 = TorsoBottom * s, span = (TorsoTop - TorsoBottom) * s;
            Func<float, float> ty = y => (y * s - y0) / span;
            Vector2 armor = ArmorUV;
            Vector2 edge = Pal(5, 2);
            int sh = _look.ShellStyle;
            float jacketPad = _look.Jacket ? 0.016f : 0f;
            if (sh >= 1)
            {
                EmitPatch(_torso, ty(1.12f), ty(1.43f), 26f, 154f, Segs(6, 5, 3), Segs(10, 8, 6), (0.014f + jacketPad) * s, 0.01f * s, armor, edge);
                if (HasGlow) EmitRibbon(_torso, ty(1.16f), ty(1.38f), 90f, 0.007f * s, 5, GlowUV, (0.0262f + jacketPad) * s);
            }
            if (sh >= 2)
            {
                for (int side = 0; side < 2; side++)
                {
                    float lat = side == 0 ? 180f : 0f;
                    EmitPatch(_upper[side], -0.09f, 0.34f, lat - 115f, lat + 115f, 4, Segs(10, 8, 6), 0.014f * s, 0.008f * s, armor, edge);
                }
            }
            if (sh >= 3)
            {
                EmitPatch(_torso, ty(1.12f), ty(1.43f), 206f, 334f, Segs(6, 5, 3), Segs(10, 8, 6), (0.014f + jacketPad) * s, 0.01f * s, armor, edge);
                EmitPatch(_torso, ty(0.99f), ty(1.1f), 40f, 140f, 2, Segs(8, 6, 4), 0.012f * s, 0.008f * s, armor, edge);
            }
            if (_look.GauntletStyle > 0)
            {
                for (int side = 0; side < 2; side++)
                {
                    float lat = side == 0 ? 180f : 0f;
                    EmitPatch(_fore[side], 0.12f, 0.86f, lat - 100f, lat + 100f, 4, Segs(8, 6, 5), 0.01f * s, 0.008f * s, armor, edge);
                    if (HasGlow) EmitRibbon(_fore[side], 0.2f, 0.78f, lat, 0.006f * s, 5, GlowUV, 0.0205f * s);
                    EmitPatch(_palm[side], 0.05f, 0.85f, lat - 70f, lat + 70f, 3, 5, 0.006f * s, 0.005f * s, armor, edge);
                }
            }
            if (_look.StriderStyle > 0 || _look.SuitStyle == 3)
            {
                for (int side = 0; side < 2; side++)
                {
                    EmitPatch(_shin[side], 0.08f, 0.82f, 20f, 160f, 4, Segs(8, 6, 4), 0.012f * s, 0.009f * s, armor, edge);
                    EmitPatch(_thigh[side], 0.86f, 1.03f, 40f, 140f, 2, 4, 0.012f * s, 0.01f * s, armor, edge);
                }
            }
            if (_look.VisorStyle == 1)
            {
                EmitPatch(_head, HeadT(HY(0.017f) * s), HeadT(HY(0.046f) * s), 18f, 162f, 2, Segs(16, 12, 8), 0.018f * s, 0.003f * s, HasGlow ? GlowUV : Pal(5, 13), Pal(5, 2));
            }
            else if (_look.VisorStyle == 2)
            {
                EmitPatch(_head, HeadT(HY(-0.06f) * s), HeadT(HY(0.07f) * s), 12f, 168f, Segs(6, 4, 3), Segs(16, 12, 8), 0.034f * s, 0.004f * s, Pal(5, 13), Pal(5, 2));
                if (HasGlow) EmitRibbon(_head, HeadT(HY(0.0f) * s), HeadT(HY(0.06f) * s), 90f, 0.006f * s, 4, GlowUV, 0.0395f * s);
                if (HasGlow) EmitBand(_head, HeadT(HY(0.03f) * s), 30f, 150f, 0.006f * s, 12, GlowUV, 0.0395f * s);
            }
        }

        // ------------------------------------------------------------------ implants

        private void BuildImplants()
        {
            int im = _look.Implant;
            if (im == 0) return;
            float s = _s;
            Vector2 glow = HasGlow ? GlowUV : Pal(4, 1);
            if ((im & 1) != 0)
            {
                EmitRibbon(_head, HeadT(0.0f), HeadT(HY(0.09f) * s), 4f, 0.0045f * s, 5, glow, 0.0018f * s);
                EmitEllipsoid(_head.Point(HeadT(HY(0.095f) * s), 6f, 0.002f * s), new Vector3(0.005f, 0.005f, 0.003f) * s, QMath.AY(90f), 3, 6, glow, W1(RigLayout.Head));
            }
            if ((im & 4) != 0)
            {
                EmitRibbon(_head, HeadT(HY(-0.095f) * s), HeadT(HY(-0.04f) * s), 148f, 0.004f * s, 5, glow, 0.0016f * s);
                EmitRibbon(_head, HeadT(HY(-0.085f) * s), HeadT(HY(-0.05f) * s), 138f, 0.003f * s, 4, glow, 0.0016f * s);
            }
            if ((im & 8) != 0)
            {
                for (int side = 0; side < 2; side++)
                {
                    float lat = side == 0 ? 180f : 0f;
                    float off = LongSleeves ? 0.0035f * s : 0.0018f * s;
                    EmitRibbon(_fore[side], 0.2f, 0.8f, lat + (side == 0 ? -30f : 30f), 0.0075f * s, 6, glow, off);
                }
            }
        }
    }
}
