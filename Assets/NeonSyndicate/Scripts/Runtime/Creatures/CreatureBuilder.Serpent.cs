using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Dune Wyrm: a 14 metre burrowing worm. The body is one tube from the blunt tail to the head, armoured in
    /// overlapping rings (each plate rises toward its rear edge and drops into a dark groove), with crystal studs down the
    /// back and glowing ember vents along the flanks. The head is a thick collar around a ring maw: five armoured petals
    /// close into a blunt dome and open wide like a flower, showing rings of crystal teeth and a throat that burns like
    /// a furnace, with the glowing core (the weak point) deep inside.
    /// Skeleton: Root (the transform, under the head), Head, the five petals (Maw1..Maw5, children of the head, Maw1 on
    /// top) and the segments Seg01..SegNN (children of the root, front to back): the pose places every segment along
    /// the trail the head left, so the segments carry procedural positions as well as rotations.
    /// </summary>
    internal sealed partial class CreatureSculptor
    {
        private float _wR, _wSeg, _wLen, _wRim;
        private int _wHead;
        private int[] _wSegs = new int[0];

        private void BuildSerpent()
        {
            float s = _s;
            float L = Mathf.Max(2f, _k.BodyLength * s);
            float R = Mathf.Max(0.05f, _k.ShoulderHeight * s * 0.5f * Mathf.Clamp(_k.Bulk, 0.6f, 1.6f));
            int n = Mathf.Clamp(_k.Segments, 6, 40);
            _wR = R;
            _wLen = L;
            _wRim = 0.5f * R;
            // The head collar and maw take the front; the rest is split into equal segments.
            _wSeg = (L - 1.1f * R) / n;
            _tile = 0.22f * Mathf.Sqrt(R);
            // ---- skeleton
            int root = Bone("Root", -1, Vector3.zero);
            _sk.Root = root;
            _wHead = Bone("Head", root, new Vector3(0f, R, 0f));
            _sk.Head = _wHead;
            int[] maw = new int[5];
            for (int k = 0; k < 5; k++)
            {
                float a = (90f + k * 72f) * Mathf.Deg2Rad;
                maw[k] = Bone("Maw" + (k + 1), _wHead, new Vector3(Mathf.Cos(a) * 0.93f * R, R + Mathf.Sin(a) * 0.93f * R, _wRim));
            }
            _sk.Jaw = maw[0];
            _wSegs = new int[n];
            for (int i = 0; i < n; i++) _wSegs[i] = Bone("Seg" + (i + 1).ToString("00"), root, new Vector3(0f, R, -(i + 1) * _wSeg));
            _sk.Spine = (int[])_wSegs.Clone();
            _sk.Chest = _wSegs[Mathf.Min(1, n - 1)];
            _sk.Neck = new int[0];
            _sk.Tail = new int[0];
            BeginCore();
            CreatureTube body = WyrmBody(R, n);
            EndCore();
            WyrmStuds(body, R, n);
            WyrmMaw(R, maw);
            // ---- hit volumes: a capsule per segment, the head, and the core in the throat
            Hit(_wHead, new Vector3(0f, R, -0.3f * R), new Vector3(0f, R, _wRim + 0.4f * R), R * 1.08f);
            for (int i = 0; i < n; i++)
            {
                Vector3 a = _bm[_wSegs[i]] + new Vector3(0f, 0f, _wSeg * 0.5f);
                Vector3 b = _bm[_wSegs[i]] - new Vector3(0f, 0f, _wSeg * 0.5f);
                float taper = i < n - 3 ? 1f : Mathf.Lerp(1f, 0.55f, (i - (n - 4)) / 3f);
                Hit(_wSegs[i], a, b, R * 0.98f * taper);
            }
            Hit(_wHead, new Vector3(0f, R, -0.85f * R), new Vector3(0f, R, -0.85f * R), 0.34f * R, true);
            // ---- metrics: the transform is under the head, the body trails up to its whole length behind it
            _aim = new Vector3(0f, R, -1.2f * _wSeg);
            _headPoint = new Vector3(0f, R * 1.15f, 0f);
            _mouthPoint = new Vector3(0f, R, _wRim + 0.3f * R);
            _radiusOverride = 1.25f * R;
            _hasPoseOverride = true;
            _poseOverride = new Bounds(new Vector3(0f, 0.05f * L, -0.3f * L), new Vector3(2.1f * L, 1.5f * L, 1.75f * L));
        }

        /// <summary>Linear weights along the body between the head and the segments (z is the model-space position).</summary>
        private BoneWeight WyrmWeight(float z)
        {
            int n = _wSegs.Length;
            if (z >= 0f) return W1(_wHead);
            float x = -z / _wSeg;
            int i = Mathf.FloorToInt(x);
            float f = x - i;
            int a = i <= 0 ? _wHead : _wSegs[Mathf.Min(n - 1, i - 1)];
            int b = _wSegs[Mathf.Min(n - 1, i)];
            if (i >= n) return W1(_wSegs[n - 1]);
            return W2(a, b, f);
        }

        private CreatureTube WyrmBody(float R, int n)
        {
            float zTail = -(n * _wSeg + 0.55f * R);
            float zFront = _wRim;
            float len = zFront - zTail;
            Vector3[] ctrl = { new Vector3(0f, R * 0.9f, zTail), new Vector3(0f, R, zTail + 0.25f * len), new Vector3(0f, R, zTail + 0.75f * len), new Vector3(0f, R, zFront) };
            Func<float, float> T = z => (z - zTail) / len;
            float tTailEnd = T(-(n - 2.5f) * _wSeg);
            float tCollar = T(-0.45f * R);
            float[] keys =
            {
                0f, 0.22f * R, 0.2f * R, 0.2f * R, 2f,
                T(zTail + 0.35f * R), 0.62f * R, 0.6f * R, 0.56f * R, 2f,
                tTailEnd, 0.9f * R, 0.9f * R, 0.84f * R, 2.05f,
                T(-0.6f * n * _wSeg), 1f * R, 1f * R, 0.92f * R, 2.1f,
                T(-0.25f * n * _wSeg), 1.04f * R, 1.04f * R, 0.95f * R, 2.1f,
                tCollar, 1.04f * R, 1.04f * R, 0.95f * R, 2.1f,
                T(0f), 1.24f * R, 1.24f * R, 1.1f * R, 2.15f,
                1f, 1.12f * R, 1.12f * R, 1.02f * R, 2.1f
            };
            CreatureTube tb = new CreatureTube(ctrl, Vector3.up, keys);
            float seg = _wSeg;
            // Overlapping ring plates: each rises toward its rear edge, then drops into the groove before the next.
            Func<float, float> Phase = z =>
            {
                float x = (-z + 0.5f * seg) / seg;
                return x - Mathf.Floor(x);
            };
            tb.Bump = (t, th) =>
            {
                float z = zTail + t * len;
                if (z > -0.5f * R) return 0.04f * R * Mathf.Clamp01((z + 0.5f * R) / (0.3f * R)) * (1f - SS(0.85f, 1f, t));
                float p = Phase(z);
                // Each plate is a little different in size, so the rings read as grown armour.
                int plateIndex = Mathf.FloorToInt((-z + 0.5f * seg) / seg);
                float vary = 1f + 0.035f * Mathf.Sin(plateIndex * 2.39f + 0.7f);
                float plate = p < 0.88f ? 0.015f + 0.11f * Mathf.Pow(p / 0.88f, 2f) : 0.125f * (1f - SS(0.88f, 1f, p)) + 0.005f;
                float belly = 1f - 0.7f * GA(th, 270f, 40f);
                float fade = SS(0f, 0.08f, t);
                return (plate + (vary - 1f)) * R * belly * fade;
            };
            tb.Weight = (t, th) => WyrmWeight(zTail + t * len);
            Vector2 hide = Pal(_k.Hide), groove = Pal(_k.Accent), belly = Pal(_k.Belly);
            Func<float, float, Vector2> region = (t, th) =>
            {
                float z = zTail + t * len;
                bool isBelly = th > 235f && th < 305f;
                if (z > -0.45f * R) return hide;
                float p = Phase(z);
                if (p > 0.92f && t > 0.03f) return groove;
                return isBelly ? belly : hide;
            };
            // Rings: placed per plate so the rise and the groove are both resolved.
            System.Collections.Generic.List<float> rings = new System.Collections.Generic.List<float>();
            float[] plateP = _detail == 2 ? new[] { 0f, 0.25f, 0.48f, 0.68f, 0.84f, 0.9f, 0.95f } : (_detail == 1 ? new[] { 0f, 0.45f, 0.8f, 0.9f } : new[] { 0f, 0.88f });
            int tipRings = Segs(5, 4, 3);
            for (int i = 0; i < tipRings; i++) rings.Add(T(zTail + 0.55f * R * i / tipRings));
            for (int i = n; i >= 1; i--)
            {
                float zFrontPlate = -(i - 0.5f) * seg;
                foreach (float p in plateP)
                {
                    float z = zFrontPlate - p * seg;
                    if (z < -0.45f * R && z > zTail + 0.55f * R) rings.Add(T(z));
                }
            }
            rings.Sort();
            float[] head = { -0.45f * R, -0.2f * R, 0f, 0.2f * R, 0.38f * R, _wRim };
            foreach (float z in head) rings.Add(T(z));
            rings.Sort();
            System.Collections.Generic.List<float> clean = new System.Collections.Generic.List<float>();
            foreach (float t in rings) if (clean.Count == 0 || t > clean[clean.Count - 1] + 1e-4f) clean.Add(Mathf.Clamp01(t));
            EmitTube(tb, clean.ToArray(), Segs(30, 20, 12), region, new[] { 235f, 305f }, true, false);
            return tb;
        }

        /// <summary>Crystal studs down the back, ember vents along the flanks and a crown of crystal spikes on the collar.</summary>
        private void WyrmStuds(CreatureTube tb, float R, int n)
        {
            Vector2 crystal = Pal(_k.Crystal), glow = Pal(_k.Glow);
            float zTail = -(n * _wSeg + 0.55f * R), len = _wRim - zTail;
            Func<float, float> T = z => (z - zTail) / len;
            int every = _detail == 0 ? 2 : 1;
            for (int i = 0; i < n - 1; i += every)
            {
                float z = -(i + 0.75f) * _wSeg;
                float t = T(z);
                float big = Mathf.Lerp(1f, 0.55f, (float)i / n);
                for (int side = -1; side <= 1; side++)
                {
                    if (side != 0 && (_detail < 2 || i % 2 == 1)) continue;
                    float th = 90f + side * 24f;
                    Vector3 p, nr;
                    tb.Frame(t, th, out p, out nr);
                    Vector3 dir = Nrm(nr + Vector3.back * 0.45f);
                    float l = (side == 0 ? 0.42f : 0.26f) * R * big;
                    EmitCrystal(p - nr * 0.04f * R, dir, Vector3.right, l, l * 0.3f, 5, 0.32f, crystal, WyrmWeight(z));
                }
                if (_detail > 0 && i > 0 && i < n - 2)
                {
                    // A pair of ember vents low on the flanks: narrow slits in the plate.
                    for (int side = 0; side < 2; side++)
                    {
                        float th = side == 0 ? 352f : 188f;
                        float zc = -(i + 0.62f) * _wSeg;
                        float t0 = T(zc - 0.035f * _wSeg), t1 = T(zc + 0.035f * _wSeg);
                        EmitPatch(tb, t0, t1, th - 16f, th + 16f, 1, 3, 0.004f * R, 0f, glow, glow);
                    }
                }
            }
            // Crown: short crystal spikes around the collar, swept back.
            int crown = Segs(8, 6, 4);
            for (int k = 0; k < crown; k++)
            {
                float th = 90f + (k - (crown - 1) * 0.5f) * (200f / crown);
                Vector3 p, nr;
                tb.Frame(T(-0.1f * R), th, out p, out nr);
                Vector3 dir = Nrm(nr * 0.8f + Vector3.back * 0.6f);
                float l = (0.62f - 0.22f * Mathf.Abs(th - 90f) / 100f) * R;
                EmitCrystal(p - nr * 0.05f * R, dir, Vector3.forward, l, l * 0.28f, 5, 0.35f, crystal, W1(_wHead));
            }
        }

        /// <summary>The maw: an inward throat that glows deeper in, rings of crystal teeth and five petals.</summary>
        private void WyrmMaw(float R, int[] maw)
        {
            Vector2 crystal = Pal(_k.Crystal), dark = Pal(_k.Accent), mouth = Pal(_k.Mouth), glow = Pal(_k.Glow), hide = Pal(_k.Hide);
            float rim = _wRim;
            // Throat: from the rim back into the body, seen from inside.
            Vector3[] tc = { new Vector3(0f, R, rim), new Vector3(0f, R, rim - 0.3f * R), new Vector3(0f, R, -0.6f * R), new Vector3(0f, R, -1.3f * R) };
            float[] tk =
            {
                0f, 1.1f * R, 1.1f * R, 1.02f * R, 2.1f,
                0.1f, 0.86f * R, 0.86f * R, 0.8f * R, 2f,
                0.55f, 0.6f * R, 0.6f * R, 0.58f * R, 2f,
                1f, 0.36f * R, 0.36f * R, 0.36f * R, 2f
            };
            CreatureTube th = new CreatureTube(tc, Vector3.up, tk);
            th.Weight = (t, a) => t < 0.55f ? W1(_wHead) : W2(_wHead, _wSegs[0], SS(0.55f, 1f, t) * 0.5f);
            int v0 = _d.Vertices.Count, t0 = _d.Triangles.Count;
            EmitTube(th, Rings(0f, 1f, Segs(8, 6, 4)), Segs(22, 16, 10), (t, a) => t < 0.22f ? dark : mouth, null, false, true);
            FlipRange(v0, t0);
            // The core deep in the throat.
            EmitEllipsoid(new Vector3(0f, R, -0.85f * R), new Vector3(0.3f, 0.3f, 0.36f) * R, QMath.Identity, Segs(7, 5, 4), Segs(10, 8, 6), glow, W1(_wHead));
            // Rings of crystal teeth pointing in and back.
            int[] counts = { Segs(16, 12, 9), Segs(13, 10, 7), Segs(10, 0, 0) };
            float[] zs = { rim - 0.04f * R, rim - 0.36f * R, rim - 0.7f * R };
            float[] rs = { 0.8f * R, 0.68f * R, 0.56f * R };
            float[] ls = { 0.34f * R, 0.28f * R, 0.22f * R };
            for (int ring = 0; ring < 3; ring++)
            {
                for (int k = 0; k < counts[ring]; k++)
                {
                    float a = (k + 0.5f * ring) / counts[ring] * Mathf.PI * 2f;
                    Vector3 radial = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                    Vector3 basePos = new Vector3(0f, R, zs[ring]) + radial * rs[ring];
                    Vector3 dir = Nrm(-radial * 0.75f + Vector3.back * 0.55f);
                    EmitCrystal(basePos, dir, Vector3.back, ls[ring] * Rand(0.85f, 1.1f), ls[ring] * 0.22f, 4, 0.45f, crystal, W1(_wHead), 0.12f);
                }
            }
            // Petals: closed they make a blunt armoured dome over the mouth; each opens outward on its hinge at the rim.
            int nu = Segs(7, 5, 3), nv = Segs(6, 4, 3);
            float hingeR = 0.95f * R, dome = 0.8f * R;
            for (int k = 0; k < 5; k++)
            {
                float phi = 90f + k * 72f;
                int bone = maw[k];
                Func<float, float, Vector3> P = (u, v) =>
                {
                    float ang = (phi + (v - 0.5f) * 70f * (1f - 0.8f * u)) * Mathf.Deg2Rad;
                    float r = hingeR * Mathf.Cos(u * Mathf.PI * 0.5f * 0.94f);
                    float z = rim + dome * Mathf.Sin(u * Mathf.PI * 0.5f);
                    // A low keel down the middle of each petal.
                    r += 0.05f * R * (1f - Mathf.Abs(v - 0.5f) * 2f) * Mathf.Sin(u * Mathf.PI);
                    return new Vector3(Mathf.Cos(ang) * r, R + Mathf.Sin(ang) * r, z);
                };
                Vector3 outward = new Vector3(Mathf.Cos(phi * Mathf.Deg2Rad), Mathf.Sin(phi * Mathf.Deg2Rad), 0.6f);
                EmitSheet(nu, nv, P, (u, v) => W1(bone), Nrm(outward), 0.07f * R, hide, dark);
                // Teeth along the inside of each petal, hidden when the dome is shut.
                int teeth = Segs(3, 2, 1);
                for (int j = 0; j < teeth; j++)
                {
                    float u = 0.25f + 0.5f * j / Mathf.Max(1, teeth - 1);
                    Vector3 p = P(u, 0.5f);
                    Vector3 axis = new Vector3(0f, R, p.z);
                    Vector3 inward = Nrm(axis - p);
                    EmitCrystal(p + inward * 0.03f * R, Nrm(inward + Vector3.back * 0.3f), Vector3.forward, 0.22f * R, 0.06f * R, 4, 0.5f, crystal, W1(bone), 0.1f);
                }
            }
        }
    }
}
