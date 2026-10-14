using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Quaternion helpers written in plain C# (same conventions as UnityEngine.Quaternion: +Z forward, +Y up,
    /// Euler order Z then X then Y). The character mesh builder and the pose solver use these so the exact same
    /// code runs inside Unity and in the offline character tests.
    /// </summary>
    public static class QMath
    {
        public static readonly Quaternion Identity = new Quaternion(0f, 0f, 0f, 1f);

        public static Quaternion AngleAxis(float degrees, Vector3 axis)
        {
            float m = axis.magnitude;
            if (m < 1e-6f) return Identity;
            float h = degrees * Mathf.Deg2Rad * 0.5f;
            float s = Mathf.Sin(h) / m;
            return new Quaternion(axis.x * s, axis.y * s, axis.z * s, Mathf.Cos(h));
        }

        public static Quaternion AX(float deg) { float h = deg * Mathf.Deg2Rad * 0.5f; return new Quaternion(Mathf.Sin(h), 0f, 0f, Mathf.Cos(h)); }
        public static Quaternion AY(float deg) { float h = deg * Mathf.Deg2Rad * 0.5f; return new Quaternion(0f, Mathf.Sin(h), 0f, Mathf.Cos(h)); }
        public static Quaternion AZ(float deg) { float h = deg * Mathf.Deg2Rad * 0.5f; return new Quaternion(0f, 0f, Mathf.Sin(h), Mathf.Cos(h)); }

        /// <summary>Same as Quaternion.Euler: rotate about Z, then X, then Y.</summary>
        public static Quaternion Euler(float x, float y, float z)
        {
            return AY(y) * AX(x) * AZ(z);
        }

        public static Quaternion Inverse(Quaternion q)
        {
            float d = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
            if (d < 1e-12f) return Identity;
            float k = 1f / d;
            return new Quaternion(-q.x * k, -q.y * k, -q.z * k, q.w * k);
        }

        public static Quaternion Normalize(Quaternion q)
        {
            float m = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            if (m < 1e-9f) return Identity;
            float k = 1f / m;
            return new Quaternion(q.x * k, q.y * k, q.z * k, q.w * k);
        }

        public static float Dot(Quaternion a, Quaternion b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
        }

        /// <summary>Spherical interpolation along the shortest arc (t clamped to 0..1).</summary>
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t)
        {
            t = Mathf.Clamp01(t);
            float d = Dot(a, b);
            if (d < 0f)
            {
                b = new Quaternion(-b.x, -b.y, -b.z, -b.w);
                d = -d;
            }
            if (d > 0.9995f)
            {
                return Normalize(new Quaternion(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t, a.w + (b.w - a.w) * t));
            }
            float th = Mathf.Acos(Mathf.Clamp(d, -1f, 1f));
            float s = Mathf.Sin(th);
            float wa = Mathf.Sin((1f - t) * th) / s;
            float wb = Mathf.Sin(t * th) / s;
            return new Quaternion(a.x * wa + b.x * wb, a.y * wa + b.y * wb, a.z * wa + b.z * wb, a.w * wa + b.w * wb);
        }

        /// <summary>Rotation taking direction a onto direction b.</summary>
        public static Quaternion FromTo(Vector3 a, Vector3 b)
        {
            float ma = a.magnitude, mb = b.magnitude;
            if (ma < 1e-6f || mb < 1e-6f) return Identity;
            a /= ma;
            b /= mb;
            float d = Vector3.Dot(a, b);
            if (d < -0.99999f)
            {
                Vector3 axis = Vector3.Cross(Vector3.right, a);
                if (axis.sqrMagnitude < 1e-6f) axis = Vector3.Cross(Vector3.up, a);
                return AngleAxis(180f, axis.normalized);
            }
            Vector3 c = Vector3.Cross(a, b);
            return Normalize(new Quaternion(c.x, c.y, c.z, 1f + d));
        }

        /// <summary>Same as Quaternion.LookRotation: +Z along forward, +Y as close to up as possible.</summary>
        public static Quaternion LookRotation(Vector3 forward, Vector3 up)
        {
            Vector3 f = forward.normalized;
            if (f.sqrMagnitude < 1e-8f) return Identity;
            Vector3 r = Vector3.Cross(up, f);
            if (r.sqrMagnitude < 1e-8f) r = Vector3.Cross(Mathf.Abs(f.y) < 0.9f ? Vector3.up : Vector3.forward, f);
            r.Normalize();
            Vector3 u = Vector3.Cross(f, r);
            return FromBasis(r, u, f);
        }

        /// <summary>Rotation whose local X, Y, Z axes map to the given orthonormal vectors.</summary>
        public static Quaternion FromBasis(Vector3 x, Vector3 y, Vector3 z)
        {
            float m00 = x.x, m01 = y.x, m02 = z.x;
            float m10 = x.y, m11 = y.y, m12 = z.y;
            float m20 = x.z, m21 = y.z, m22 = z.z;
            float tr = m00 + m11 + m22;
            Quaternion q;
            if (tr > 0f)
            {
                float s = Mathf.Sqrt(tr + 1f) * 2f;
                q = new Quaternion((m21 - m12) / s, (m02 - m20) / s, (m10 - m01) / s, 0.25f * s);
            }
            else if (m00 > m11 && m00 > m22)
            {
                float s = Mathf.Sqrt(1f + m00 - m11 - m22) * 2f;
                q = new Quaternion(0.25f * s, (m01 + m10) / s, (m02 + m20) / s, (m21 - m12) / s);
            }
            else if (m11 > m22)
            {
                float s = Mathf.Sqrt(1f + m11 - m00 - m22) * 2f;
                q = new Quaternion((m01 + m10) / s, 0.25f * s, (m12 + m21) / s, (m02 - m20) / s);
            }
            else
            {
                float s = Mathf.Sqrt(1f + m22 - m00 - m11) * 2f;
                q = new Quaternion((m02 + m20) / s, (m12 + m21) / s, 0.25f * s, (m10 - m01) / s);
            }
            return Normalize(q);
        }

        /// <summary>Framerate-independent smoothing factor for lerps (same as MathUtil.Damp).</summary>
        public static float Damp(float sharpness, float dt)
        {
            return 1f - Mathf.Exp(-sharpness * dt);
        }

        public static float Smooth01(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

        /// <summary>Remaps x from [a,b] to [0,1] with smoothstep easing.</summary>
        public static float SmoothRange(float a, float b, float x)
        {
            return Smooth01((x - a) / (b - a));
        }
    }
}
