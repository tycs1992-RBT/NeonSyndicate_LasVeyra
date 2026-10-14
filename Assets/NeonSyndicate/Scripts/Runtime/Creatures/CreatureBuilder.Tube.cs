using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// A smooth tube swept along a curved centre line: the creature equivalent of BodyBuilder's Loft. The centre line is
    /// a centripetal Catmull-Rom spline through control points (no loops or overshoot), re-parameterised by arc length
    /// (t = 0 at the first point, 1 at the last). Cross-sections are superellipses with separate upper and lower radii,
    /// keyed along t and interpolated with monotone cubics (smooth, never negative), carried on rotation-minimising
    /// frames so a neck that rises and turns forward or a tail that curls keeps its back on top. Radial bumps add anatomy
    /// (shoulder blades, haunches, brows, knees) and every point has bone weights.
    /// Theta convention: 0 degrees is the frame's side axis (the creature's right for a forward tube), 90 its up axis
    /// (the back), 180 the left side and 270 the underside.
    /// </summary>
    internal sealed class CreatureTube
    {
        /// <summary>Finite-difference steps for normals and tangents (fraction of the length, and degrees).</summary>
        public const float StepT = 0.0035f, StepTheta = 1.2f;
        private const int SamplesPerSpan = 20;

        private readonly Vector3[] _p, _f, _u, _v;
        private readonly float[] _l;
        private readonly float[] _ctrlT;
        /// <summary>Centre-line length in metres.</summary>
        public readonly float Length;
        public float[] T, A, BU, BD, E;
        private float[] _mA, _mBU, _mBD, _mE;
        /// <summary>Radial offset (metres) at (t, theta degrees).</summary>
        public Func<float, float, float> Bump;
        public Func<float, float, BoneWeight> Weight;

        /// <summary>
        /// keys: groups of five (t, half width, upper radius, lower radius, superellipse exponent). upHint: the direction
        /// the section's 90 degree axis should face at the start (projected off the centre line).
        /// </summary>
        public CreatureTube(Vector3[] ctrl, Vector3 upHint, float[] keys)
        {
            int n = ctrl.Length;
            if (n < 2) ctrl = new[] { ctrl.Length > 0 ? ctrl[0] : Vector3.zero, (ctrl.Length > 0 ? ctrl[0] : Vector3.zero) + Vector3.forward * 0.01f };
            n = ctrl.Length;
            int spans = n - 1;
            int count = spans * SamplesPerSpan + 1;
            _p = new Vector3[count];
            _f = new Vector3[count];
            _u = new Vector3[count];
            _v = new Vector3[count];
            _l = new float[count];
            _ctrlT = new float[n];
            for (int s = 0; s < spans; s++)
            {
                Vector3 p0 = s > 0 ? ctrl[s - 1] : ctrl[0] * 2f - ctrl[1];
                Vector3 p1 = ctrl[s], p2 = ctrl[s + 1];
                Vector3 p3 = s + 2 < n ? ctrl[s + 2] : ctrl[n - 1] * 2f - ctrl[n - 2];
                for (int k = 0; k < SamplesPerSpan; k++) _p[s * SamplesPerSpan + k] = CatmullRom(p0, p1, p2, p3, (float)k / SamplesPerSpan);
            }
            _p[count - 1] = ctrl[n - 1];
            // Arc length.
            _l[0] = 0f;
            for (int i = 1; i < count; i++) _l[i] = _l[i - 1] + Dist(_p[i], _p[i - 1]);
            Length = Mathf.Max(1e-5f, _l[count - 1]);
            for (int i = 0; i < count; i++) _l[i] /= Length;
            for (int c = 0; c < n; c++) _ctrlT[c] = _l[Mathf.Min(count - 1, c * SamplesPerSpan)];
            // Tangents by central differences, then parallel-transported frames.
            for (int i = 0; i < count; i++)
            {
                Vector3 a = _p[Mathf.Max(0, i - 1)], b = _p[Mathf.Min(count - 1, i + 1)];
                Vector3 d = b - a;
                _f[i] = d.sqrMagnitude > 1e-14f ? Nrm(d) : (i > 0 ? _f[i - 1] : Vector3.forward);
            }
            Vector3 v0 = upHint - _f[0] * Vector3.Dot(upHint, _f[0]);
            if (v0.sqrMagnitude < 1e-8f) v0 = Vector3.Cross(_f[0], Vector3.right);
            if (v0.sqrMagnitude < 1e-8f) v0 = Vector3.Cross(_f[0], Vector3.forward);
            _v[0] = Nrm(v0);
            _u[0] = Nrm(Vector3.Cross(_v[0], _f[0]));
            for (int i = 1; i < count; i++)
            {
                Vector3 v = Transport(_f[i - 1], _f[i], _v[i - 1]);
                v -= _f[i] * Vector3.Dot(v, _f[i]);
                if (v.sqrMagnitude < 1e-10f) v = _v[i - 1];
                _v[i] = Nrm(v);
                _u[i] = Nrm(Vector3.Cross(_v[i], _f[i]));
            }
            SetKeys(keys);
        }

        public void SetKeys(float[] keys)
        {
            int n = keys.Length / 5;
            T = new float[n]; A = new float[n]; BU = new float[n]; BD = new float[n]; E = new float[n];
            for (int i = 0; i < n; i++)
            {
                T[i] = keys[i * 5];
                A[i] = keys[i * 5 + 1];
                BU[i] = keys[i * 5 + 2];
                BD[i] = keys[i * 5 + 3];
                E[i] = keys[i * 5 + 4];
            }
            Prepare();
        }

        public void Prepare()
        {
            _mA = Slopes(T, A);
            _mBU = Slopes(T, BU);
            _mBD = Slopes(T, BD);
            _mE = Slopes(T, E);
        }

        /// <summary>The t of control point i (where the centre line passes it).</summary>
        public float CtrlT(int i) { return _ctrlT[Mathf.Clamp(i, 0, _ctrlT.Length - 1)]; }

        // ------------------------------------------------------------------ evaluation

        public struct Ring
        {
            public float T, A, BU, BD, E;
            public Vector3 C, U, V, F;
        }

        public Ring EvalRing(float t)
        {
            Ring r;
            r.T = t;
            Sample(t, out r.C, out r.F, out r.U, out r.V);
            r.A = Mathf.Max(0.0004f, Eval(A, _mA, t));
            r.BU = Mathf.Max(0.0004f, Eval(BU, _mBU, t));
            r.BD = Mathf.Max(0.0004f, Eval(BD, _mBD, t));
            r.E = Mathf.Clamp(Eval(E, _mE, t), 1.6f, 8f);
            return r;
        }

        public Vector3 PointOn(ref Ring ring, float thetaDeg, float offset)
        {
            float th = thetaDeg * Mathf.Deg2Rad;
            float cs = (float)Math.Cos(th), sn = (float)Math.Sin(th);
            float k = 2f / ring.E;
            float acs = cs < 0f ? -cs : cs, asn = sn < 0f ? -sn : sn;
            float px = ring.A * (cs < 0f ? -1f : 1f) * (acs < 1e-7f ? 0f : (k == 1f ? acs : (float)Math.Pow(acs, k)));
            float pz = (sn < 0f ? -ring.BD : ring.BU) * (asn < 1e-7f ? 0f : (k == 1f ? asn : (float)Math.Pow(asn, k)));
            Vector3 radial = new Vector3(ring.U.x * px + ring.V.x * pz, ring.U.y * px + ring.V.y * pz, ring.U.z * px + ring.V.z * pz);
            float r = (float)Math.Sqrt(radial.x * radial.x + radial.y * radial.y + radial.z * radial.z);
            Vector3 dir = r > 1e-9f ? radial / r : (ring.U * cs + ring.V * sn);
            float bump = Bump != null ? Bump(ring.T, thetaDeg) : 0f;
            return ring.C + radial + dir * (bump + offset);
        }

        public Vector3 Point(float t, float thetaDeg, float offset)
        {
            Ring r = EvalRing(t);
            return PointOn(ref r, thetaDeg, offset);
        }

        /// <summary>Surface point, outward normal and tangent (around the ring, w = bitangent sign) from five samples.</summary>
        public void Frame(ref Ring below, ref Ring mid, ref Ring above, float thetaDeg, out Vector3 p, out Vector3 n, out Vector4 tangent)
        {
            p = PointOn(ref mid, thetaDeg, 0f);
            Vector3 dt = PointOn(ref above, thetaDeg, 0f) - PointOn(ref below, thetaDeg, 0f);
            Vector3 dth = PointOn(ref mid, thetaDeg + StepTheta, 0f) - PointOn(ref mid, thetaDeg - StepTheta, 0f);
            n = Vector3.Cross(dt, dth);
            Vector3 outward = p - mid.C;
            if (n.sqrMagnitude < 1e-24f) n = outward;
            if (n.sqrMagnitude < 1e-24f) n = mid.F;
            n = Nrm(n);
            if (Vector3.Dot(n, outward) < 0f) n = -n;
            Vector3 tu = dth - n * Vector3.Dot(dth, n);
            if (tu.sqrMagnitude < 1e-14f) tu = Vector3.Cross(n, Mathf.Abs(n.y) < 0.95f ? Vector3.up : Vector3.right);
            tu = Nrm(tu);
            float w = Vector3.Dot(Vector3.Cross(n, tu), dt) < 0f ? -1f : 1f;
            tangent = new Vector4(tu.x, tu.y, tu.z, w);
        }

        public void Frame(float t, float thetaDeg, out Vector3 p, out Vector3 n)
        {
            Ring below = EvalRing(t - StepT), mid = EvalRing(t), above = EvalRing(t + StepT);
            Vector4 tangent;
            Frame(ref below, ref mid, ref above, thetaDeg, out p, out n, out tangent);
        }

        public BoneWeight WeightAt(float t, float theta)
        {
            return Weight != null ? Weight(t, theta) : BodyBuilder.W1(0);
        }

        public Vector3 Center(float t)
        {
            Vector3 c, f, u, v;
            Sample(t, out c, out f, out u, out v);
            return c;
        }

        public void Axes(float t, out Vector3 forward, out Vector3 side, out Vector3 up)
        {
            Vector3 c;
            Sample(t, out c, out forward, out side, out up);
        }

        /// <summary>Section radius toward theta at t (without bumps).</summary>
        public float RadiusAt(float t, float thetaDeg)
        {
            Ring r = EvalRing(t);
            return (PointOn(ref r, thetaDeg, 0f) - r.C).magnitude - (Bump != null ? Bump(t, thetaDeg) : 0f);
        }

        // ------------------------------------------------------------------ internals

        private void Sample(float t, out Vector3 c, out Vector3 f, out Vector3 u, out Vector3 v)
        {
            int last = _p.Length - 1;
            if (t <= 0f)
            {
                c = _p[0] + _f[0] * (t * Length);
                f = _f[0]; u = _u[0]; v = _v[0];
                return;
            }
            if (t >= 1f)
            {
                c = _p[last] + _f[last] * ((t - 1f) * Length);
                f = _f[last]; u = _u[last]; v = _v[last];
                return;
            }
            int lo = 0, hi = last;
            while (hi - lo > 1)
            {
                int m = (lo + hi) >> 1;
                if (_l[m] <= t) lo = m; else hi = m;
            }
            float span = _l[hi] - _l[lo];
            float s = span > 1e-9f ? (t - _l[lo]) / span : 0f;
            // Hermite between samples (tangents scaled by the chord) keeps the centre line smooth between samples.
            float h = Dist(_p[hi], _p[lo]);
            float s2 = s * s, s3 = s2 * s;
            c = (2f * s3 - 3f * s2 + 1f) * _p[lo] + (s3 - 2f * s2 + s) * h * _f[lo] + (-2f * s3 + 3f * s2) * _p[hi] + (s3 - s2) * h * _f[hi];
            f = Nrm(Vector3.Lerp(_f[lo], _f[hi], s));
            Vector3 vv = Vector3.Lerp(_v[lo], _v[hi], s);
            vv -= f * Vector3.Dot(vv, f);
            v = vv.sqrMagnitude > 1e-12f ? Nrm(vv) : _v[lo];
            u = Nrm(Vector3.Cross(v, f));
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float u)
        {
            // Centripetal (alpha 0.5) Barry-Goldman form.
            float t0 = 0f;
            float t1 = t0 + Mathf.Max(1e-4f, Mathf.Sqrt(Dist(p0, p1)));
            float t2 = t1 + Mathf.Max(1e-4f, Mathf.Sqrt(Dist(p1, p2)));
            float t3 = t2 + Mathf.Max(1e-4f, Mathf.Sqrt(Dist(p2, p3)));
            float t = Mathf.Lerp(t1, t2, u);
            Vector3 a1 = (t1 - t) / (t1 - t0) * p0 + (t - t0) / (t1 - t0) * p1;
            Vector3 a2 = (t2 - t) / (t2 - t1) * p1 + (t - t1) / (t2 - t1) * p2;
            Vector3 a3 = (t3 - t) / (t3 - t2) * p2 + (t - t2) / (t3 - t2) * p3;
            Vector3 b1 = (t2 - t) / (t2 - t0) * a1 + (t - t0) / (t2 - t0) * a2;
            Vector3 b2 = (t3 - t) / (t3 - t1) * a2 + (t - t1) / (t3 - t1) * a3;
            return (t2 - t) / (t2 - t1) * b1 + (t - t1) / (t2 - t1) * b2;
        }

        /// <summary>Rotates v by the smallest rotation that takes direction a to direction b (both unit).</summary>
        private static Vector3 Transport(Vector3 a, Vector3 b, Vector3 v)
        {
            Vector3 axis = Vector3.Cross(a, b);
            float s = axis.magnitude;
            float c = Vector3.Dot(a, b);
            if (s < 1e-7f) return v;
            axis /= s;
            // Rodrigues' rotation.
            return v * c + Vector3.Cross(axis, v) * s + axis * (Vector3.Dot(axis, v) * (1f - c));
        }

        private float Eval(float[] p, float[] m, float t)
        {
            int n = T.Length;
            if (n == 1 || t <= T[0]) return p[0];
            if (t >= T[n - 1]) return p[n - 1];
            int k = 0;
            while (k < n - 2 && t > T[k + 1]) k++;
            float h = T[k + 1] - T[k];
            float s = (t - T[k]) / h;
            float s2 = s * s, s3 = s2 * s;
            return (2f * s3 - 3f * s2 + 1f) * p[k] + (s3 - 2f * s2 + s) * h * m[k] + (-2f * s3 + 3f * s2) * p[k + 1] + (s3 - s2) * h * m[k + 1];
        }

        /// <summary>Fritsch-Carlson monotone cubic slopes (same as Loft): smooth curves, no overshoot.</summary>
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

        private static float Dist(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x, y = a.y - b.y, z = a.z - b.z;
            return (float)Math.Sqrt(x * x + y * y + z * z);
        }

        public static Vector3 Nrm(Vector3 v)
        {
            float m = (float)Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
            return m > 1e-20f ? v / m : Vector3.zero;
        }
    }
}
