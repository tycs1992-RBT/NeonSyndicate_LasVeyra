using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class BodyBuilder
    {
        // ================================================================== Mirage Djinn

        /// <summary>
        /// The Mirage Djinn: a broad, heavily muscled bare torso of deep blue skin carved with glowing amber sigils, a
        /// person's face (built by the people half of this class and repainted) with pointed ears, burning gold eyes, a
        /// spiked gold crown and a braided beard, a gold collar, belt, armbands and bracers, and below the waist a
        /// swirling tail of sand and smoke banded in a helix, wrapped in smoke ribbons and orbited by drifting stones.
        /// The legs exist for the rig only: the tail hangs from the hips and follows both legs, which the pose solver
        /// gathers together and trails behind while the Djinn floats (RigLayout.Floating).
        /// </summary>
        private void BuildDjinn()
        {
            DjinnTorso();
            DjinnHead();
            for (int side = 0; side < 2; side++) DjinnArm(side);
            DjinnTail();
            DjinnJewels();
            DjinnSigils();
        }

        /// <summary>
        /// Builds a person's head and face (BuildHead, BuildFace) at another detail level in a helper builder for the same
        /// look and rig, appends its mesh and adopts its head loft, so face features can be placed on it afterwards.
        /// </summary>
        private void BuildHeadAt(int detail)
        {
            BodyBuilder h = new BodyBuilder(_look, _L, detail);
            h.BuildHead();
            h.BuildFace();
            int offset = _d.Vertices.Count;
            BodyData src = h._d;
            _d.Vertices.AddRange(src.Vertices);
            _d.Normals.AddRange(src.Normals);
            _d.Palette.AddRange(src.Palette);
            _d.Detail.AddRange(src.Detail);
            _d.Weights.AddRange(src.Weights);
            _d.Tangents.AddRange(src.Tangents);
            for (int i = 0; i < src.Triangles.Count; i++) _d.Triangles.Add(src.Triangles[i] + offset);
            _head = h._head;
            _headCenter = h._headCenter;
            _headY0 = h._headY0;
            _headY1 = h._headY1;
        }

        /// <summary>Repaints vertices from first on: rows the map returns a cell for take that cell.</summary>
        private void Repaint(int first, Func<int, int, Vector2> map)
        {
            int n = PaletteSize;
            for (int i = first; i < _d.Palette.Count; i++)
            {
                Vector2 uv = _d.Palette[i];
                _d.Palette[i] = map(Mathf.Clamp((int)(uv.y * n), 0, n - 1), Mathf.Clamp((int)(uv.x * n), 0, n - 1));
            }
        }

        /// <summary>A bone weight from up to five influences: the four strongest are kept, normalised and sorted.</summary>
        private static BoneWeight WN(int b0, float w0, int b1, float w1, int b2, float w2, int b3, float w3, int b4, float w4)
        {
            int[] bi = { b0, b1, b2, b3, b4 };
            float[] bw = { Mathf.Max(0f, w0), Mathf.Max(0f, w1), Mathf.Max(0f, w2), Mathf.Max(0f, w3), Mathf.Max(0f, w4) };
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4 - i; j++)
                    if (bw[j] < bw[j + 1]) { float tw = bw[j]; bw[j] = bw[j + 1]; bw[j + 1] = tw; int ti = bi[j]; bi[j] = bi[j + 1]; bi[j + 1] = ti; }
            float sum = bw[0] + bw[1] + bw[2] + bw[3];
            if (sum < 1e-6f) return W1(b0);
            BoneWeight w = new BoneWeight();
            w.boneIndex0 = bi[0]; w.weight0 = bw[0] / sum;
            w.boneIndex1 = bw[1] > 0f ? bi[1] : 0; w.weight1 = bw[1] / sum;
            w.boneIndex2 = bw[2] > 0f ? bi[2] : 0; w.weight2 = bw[2] / sum;
            w.boneIndex3 = bw[3] > 0f ? bi[3] : 0; w.weight3 = bw[3] / sum;
            return w;
        }

        private void DjinnTorso()
        {
            float s = _s;
            // A V-taper: narrow waist, lats and pectorals swelling to the armpits; the shoulders are the arms' deltoids.
            float[] ys = { 0.9f, 0.94f, 0.98f, 1.02f, 1.07f, 1.13f, 1.19f, 1.26f, 1.32f, 1.37f, 1.42f, 1.46f, 1.5f, 1.53f, 1.555f, 1.575f };
            float[] a = { 0.05f, 0.13f, 0.15f, 0.138f, 0.13f, 0.14f, 0.16f, 0.19f, 0.215f, 0.228f, 0.232f, 0.226f, 0.205f, 0.17f, 0.125f, 0.07f };
            float[] b = { 0.05f, 0.094f, 0.108f, 0.1f, 0.098f, 0.104f, 0.116f, 0.13f, 0.142f, 0.148f, 0.144f, 0.134f, 0.12f, 0.108f, 0.094f, 0.074f };
            float[] cv = { 0f, 0f, 0.003f, 0.008f, 0.012f, 0.014f, 0.016f, 0.02f, 0.026f, 0.026f, 0.016f, 0.004f, -0.004f, -0.008f, -0.008f, -0.004f };
            float[] e = { 2f, 2.1f, 2.2f, 2.2f, 2.2f, 2.25f, 2.3f, 2.3f, 2.3f, 2.3f, 2.3f, 2.25f, 2.2f, 2.1f, 2.05f, 2f };
            float bw = Mathf.Lerp(0.94f, 1.06f, Mathf.InverseLerp(0.75f, 1.4f, _L.Bulk));
            Loft L = VLoft(Vector3.zero, ys, a, b, cv, e, bw, bw);
            float y0 = ys[0] * s, span = (ys[ys.Length - 1] - ys[0]) * s;
            L.Bump = (t, th) =>
            {
                float y = (y0 + t * span) / s;
                float k = 0f;
                // Heavy pectorals with a crisp lower edge, a carved six-pack, obliques, lats and a deep spine groove.
                float pecY = y > 1.34f ? G(y, 1.36f, 0.06f) : G(y, 1.36f, 0.032f);
                k += 0.024f * pecY * (GA(th, 62f, 22f) + GA(th, 118f, 22f));
                float absMask = SS(0.99f, 1.03f, y) * (1f - SS(1.24f, 1.28f, y)) * GA(th, 90f, 16f);
                float absRow = Mathf.Max(0f, Mathf.Sin((y - 0.99f) / 0.084f * Mathf.PI * 2f + 0.6f));
                float absCol = Mathf.Max(0f, Mathf.Abs(Mathf.Sin(Mathf.DeltaAngle(90f, th) / 13f * Mathf.PI * 0.5f)));
                k += 0.009f * absMask * absRow * (0.4f + 0.6f * absCol);
                k -= 0.004f * GA(th, 90f, 3f) * SS(1.0f, 1.04f, y) * (1f - SS(1.3f, 1.34f, y));
                k += 0.01f * G(y, 1.08f, 0.05f) * (GA(th, 30f, 16f) + GA(th, 150f, 16f));
                k += 0.016f * G(y, 1.3f, 0.07f) * (GA(th, 210f, 20f) + GA(th, 330f, 20f));
                k += 0.012f * G(y, 1.41f, 0.05f) * (GA(th, 236f, 20f) + GA(th, 304f, 20f));
                k -= 0.008f * GA(th, 270f, 7f) * SS(1.02f, 1.08f, y) * (1f - SS(1.48f, 1.53f, y));
                k += 0.007f * G(y, 1.497f, 0.012f) * (GA(th, 62f, 16f) + GA(th, 118f, 16f));
                return k * s;
            };
            float yTop = ys[ys.Length - 1] * s;
            L.Weight = (t, th) =>
            {
                Vector3 p = L.Center(t);
                return TorsoWeight(p.y, L.Point(t, th, 0f).x, Mathf.Cos(th * Mathf.Deg2Rad), yTop - 0.06f * s, yTop);
            };
            _torso = L;
            List<float> rt = new List<float>();
            int bodyRings = Segs(22, 15, 10), topRings = Segs(12, 8, 5);
            float split = (1.42f - ys[0]) / (ys[ys.Length - 1] - ys[0]);
            for (int i = 0; i < bodyRings; i++) rt.Add(split * i / bodyRings);
            for (int i = 0; i <= topRings; i++)
            {
                float u = (float)i / topRings;
                rt.Add(Mathf.Lerp(split, 1f, 1f - Mathf.Pow(1f - u, 1.6f)));
            }
            EmitLoft(L, 0f, 1f, 0, Segs(28, 22, 14), t => DjinnSkin, true, false, false, rt.ToArray());
            // A thick, corded neck.
            Vector3 hb = _L.World[RigLayout.Head];
            float[] nk =
            {
                0f, 0.098f * s, 0.086f * s, 0f, -0.006f * s, 2f,
                0.3f, 0.078f * s, 0.072f * s, 0f, -0.002f * s, 2f,
                0.62f, 0.068f * s, 0.066f * s, 0f, 0.002f * s, 2f,
                1f, 0.06f * s, 0.06f * s, 0f, 0.004f * s, 2f
            };
            Loft N = TiltLoft(new Vector3(0f, 1.47f * s, -0.012f * s), hb + new Vector3(0f, 0.07f * s, 0.012f * s), nk);
            N.Bump = (t, th) => s * 0.008f * G(t, 0.5f, 0.25f) * (GA(th, 60f, 12f) + GA(th, 120f, 12f));
            N.Weight = (t, th) =>
            {
                if (t < 0.32f) return W2(RigLayout.Chest, RigLayout.Neck, SS(0.02f, 0.32f, t));
                if (t < 0.72f) return W1(RigLayout.Neck);
                return W2(RigLayout.Neck, RigLayout.Head, SS(0.72f, 0.98f, t));
            };
            _neck = N;
            EmitLoft(N, 0f, 1f, Segs(8, 6, 5), Segs(18, 14, 10), t => DjinnSkin, false, false);
        }

        private void DjinnHead()
        {
            float s = _s;
            int first = _d.Vertices.Count;
            // A person's head and face at the Djinn's scale, repainted: blue skin, dark brows and lips, gold eyes. It is
            // built one detail level down (a 3.5 m giant never shows its face as close as a person does) and spliced in.
            BuildHeadAt(Mathf.Max(0, _detail - 1));
            Repaint(first, (row, col) =>
            {
                if (row == 0) return DjinnSkin;
                if (row == 6 || row == 3) return DjinnEye;
                return DjinnSmoke;
            });
            float span = _headY1 - _headY0;
            Func<float, float> ht = y => (HY(y) * s - _headY0) / span;
            // Burning eyes over the irises: almond-shaped, slanted.
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                float th = 90f - sx * 19f;
                Vector3 p = _head.Point(ht(0.032f), th, 0f);
                Vector3 nrm = _head.Normal(ht(0.032f), th);
                EmitSlitEye(p + nrm * 0.0016f * s, Vector3.Lerp(Vector3.forward, nrm, 0.4f), sx, 0.03f * s, 0.011f * s, 0.008f * s, 14f, DjinnEye, W1(RigLayout.Head));
                // Pointed ears sweeping up and back over the person's ears.
                Vector3 ear = _headCenter + new Vector3(sx * 0.083f * s, HY(0.02f) * s, -0.012f * s);
                Quaternion q = QMath.AY(sx * -18f) * QMath.AZ(sx * -28f) * QMath.AX(24f);
                EmitEllipsoid(ear + new Vector3(sx * 0.01f, 0.036f, -0.012f) * s, new Vector3(0.01f, 0.068f, 0.024f) * s, q, Segs(6, 5, 4), Segs(10, 8, 6), DjinnSkin, W1(RigLayout.Head));
            }
            // Crown: a gold band round the brow with tall spikes, the tallest over the forehead, and an amber gem.
            float tb0 = ht(0.078f), tb1 = ht(0.104f);
            EmitPatch(_head, tb0, tb1, 0f, 360f, 2, Segs(28, 20, 14), 0.004f * s, 0.009f * s, DjinnGold, DjinnGold);
            int spikes = Segs(9, 7, 5);
            for (int i = 0; i < spikes; i++)
            {
                float u = (float)i / spikes;
                float th = 90f + u * 360f;
                float front = 0.5f + 0.5f * Mathf.Cos(u * Mathf.PI * 2f);
                Vector3 root = _head.Point(tb1, th, 0.008f * s);
                Vector3 outward = Loft.Nrm(_head.Point(tb1, th, 0f) - _headCenter);
                Vector3 dir = Loft.Nrm(Vector3.up * 1.5f + outward * 0.45f);
                float len = Mathf.Lerp(0.05f, 0.15f, front * front) * s;
                EmitShard(root, dir, Loft.Nrm(Vector3.Cross(dir, outward)), len, Mathf.Lerp(0.022f, 0.034f, front) * s, 0.008f * s, 0f, Segs(3, 3, 2), DjinnGold, DjinnGold, v => W1(RigLayout.Head));
            }
            EmitEllipsoid(_head.Point((tb0 + tb1) * 0.5f, 90f, 0.016f * s), new Vector3(0.013f, 0.018f, 0.008f) * s, QMath.Identity, Segs(6, 5, 4), Segs(10, 8, 6), DjinnSigil, W1(RigLayout.Head));
            // A braided beard hanging from the chin, bound in gold.
            Vector3 chin = _head.Point(ht(-0.12f), 90f, -0.006f * s);
            Vector3 tip = chin + new Vector3(0f, -0.2f * s, 0.035f * s);
            float br = 0.02f * s;
            float[] bk = { 0f, br * 1.2f, br, 0f, 0f, 2f, 0.4f, br, br * 0.9f, 0f, 0.004f * s, 2f, 0.85f, br * 0.6f, br * 0.55f, 0f, 0.008f * s, 2f, 1f, br * 0.2f, br * 0.2f, 0f, 0.01f * s, 2f };
            Loft B = TiltLoft(chin, tip, bk);
            B.Bump = (t, th) => 0.0035f * s * Mathf.Abs(Mathf.Sin(t * 22f + th * Mathf.Deg2Rad * 2f));
            B.Weight = (t, th) => W1(RigLayout.Head);
            EmitLoft(B, 0f, 1f, Segs(9, 7, 5), Segs(10, 8, 6), t => (t > 0.3f && t < 0.38f) || (t > 0.66f && t < 0.73f) ? DjinnGold : DjinnSmoke, false, true);
        }

        private void DjinnArm(int side)
        {
            float s = _s;
            float sx = side == 0 ? -1f : 1f;
            int up = side == 0 ? RigLayout.UpperArmL : RigLayout.UpperArmR;
            int fore = side == 0 ? RigLayout.ForearmL : RigLayout.ForearmR;
            int hand = side == 0 ? RigLayout.HandL : RigLayout.HandR;
            Vector3 j = _L.World[up], el = _L.World[fore], wr = _L.World[hand];
            float k = Mathf.Lerp(0.92f, 1.08f, Mathf.InverseLerp(0.75f, 1.4f, _L.Bulk));
            float[] uk =
            {
                -0.19f, 0.012f, 0.014f, -0.05f, 0f, 2f,
                -0.16f, 0.048f, 0.052f, -0.044f, 0f, 2f,
                -0.12f, 0.064f, 0.068f, -0.034f, 0.002f, 2f,
                -0.07f, 0.076f, 0.08f, -0.02f, 0.003f, 2f,
                0f, 0.083f, 0.086f, -0.006f, 0.004f, 2f,
                0.12f, 0.084f, 0.086f, 0f, 0.006f, 2.05f,
                0.3f, 0.077f, 0.083f, 0f, 0.01f, 2.1f,
                0.55f, 0.074f, 0.082f, 0f, 0.012f, 2.1f,
                0.8f, 0.064f, 0.068f, 0f, 0.004f, 2f,
                1.04f, 0.062f, 0.064f, 0f, -0.004f, 2f
            };
            Loft U = LimbLoft(j, el, uk, k, sx);
            float lat = side == 0 ? 180f : 0f;
            U.Bump = (t, th) => s * (0.014f * G(t, 0.55f, 0.18f) * GA(th, 90f, 34f) + 0.01f * G(t, 0.5f, 0.2f) * GA(th, 270f, 40f) + 0.008f * G(t, 0.12f, 0.12f) * GA(th, lat, 60f));
            U.Weight = UpperArmWeight(up, fore);
            _upper[side] = U;
            float[] fk =
            {
                -0.04f, 0.062f, 0.064f, 0f, -0.004f, 2f,
                0.12f, 0.07f, 0.066f, 0f, 0.004f, 2.1f,
                0.35f, 0.066f, 0.06f, 0f, 0.004f, 2.1f,
                0.65f, 0.054f, 0.05f, 0f, 0.002f, 2.2f,
                0.9f, 0.044f, 0.044f, 0f, 0f, 2.3f,
                1f, 0.042f, 0.043f, 0f, 0f, 2.3f
            };
            Loft F = LimbLoft(el, wr, fk, k, sx);
            F.Bump = (t, th) => s * 0.008f * G(t, 0.25f, 0.15f) * GA(th, lat + sx * 50f, 30f);
            F.Weight = ForearmWeight(up, fore, hand);
            _fore[side] = F;
            EmitLoft(U, -0.19f, 1.04f, Segs(16, 12, 8), Segs(16, 12, 10), t => DjinnSkin, true, false);
            EmitLoft(F, -0.04f, 1f, Segs(10, 8, 6), Segs(16, 12, 10), t => DjinnSkin, false, false);
            DjinnHand(side);
            // Gold armband and a deep gold bracer with an amber channel round it.
            float rim = _detail == 0 ? 0f : 1f;
            EmitPatch(U, 0.34f, 0.42f, 0f, 360f, 1, Segs(20, 16, 12), 0.003f * s + 0.008f * s * (1f - rim), 0.008f * s * rim, DjinnGold, DjinnGold);
            EmitPatch(F, 0.46f, 0.92f, 0f, 360f, 3, Segs(20, 16, 12), 0.003f * s, 0.011f * s, DjinnGold, DjinnGold);
            EmitBand(F, 0.69f, 0f, 360f, 0.012f * s, Segs(20, 16, 12), DjinnSigil, 0.0148f * s);
        }

        private void DjinnTail()
        {
            float s = _s;
            // One column from the waist to a curling tip just above the ground, billowing out below the belt.
            Vector3 top = new Vector3(0f, 1.0f * s, 0f), bottom = new Vector3(0f, 0.09f * s, 0f);
            float sideCurl = Hash01(_look.Implant) < 0.5f ? -1f : 1f;
            float[] keys =
            {
                0f, 0.15f, 0.108f, 0f, 0.01f, 2.2f,
                0.1f, 0.19f, 0.142f, 0f, 0.02f, 2.2f,
                0.24f, 0.205f, 0.16f, 0f, 0.026f, 2.1f,
                0.42f, 0.172f, 0.142f, 0.012f * sideCurl, 0.004f, 2f,
                0.6f, 0.12f, 0.108f, 0.04f * sideCurl, -0.04f, 2f,
                0.76f, 0.074f, 0.07f, 0.075f * sideCurl, -0.1f, 2f,
                0.9f, 0.036f, 0.036f, 0.11f * sideCurl, -0.16f, 2f,
                1f, 0.006f, 0.006f, 0.15f * sideCurl, -0.2f, 2f
            };
            for (int i = 0; i < keys.Length; i += 6) { keys[i + 1] *= s; keys[i + 2] *= s; keys[i + 3] *= s; keys[i + 4] *= s; }
            Loft T = new Loft(top, bottom, Vector3.right, Vector3.forward, keys);
            float twist = 1f;
            T.Bump = (t, th) => s * 0.014f * Mathf.Sin((th - 430f * twist * t) * Mathf.Deg2Rad * 2f) * SS(0.05f, 0.2f, t) * (1f - SS(0.8f, 1f, t)) * (1f - 0.5f * t);
            T.Weight = (t, th) =>
            {
                float c = Mathf.Cos(th * Mathf.Deg2Rad);
                float left = Mathf.Clamp01(0.5f - c * 0.9f), right = 1f - left;
                float wLeg = 0.92f * SS(0.08f, 0.5f, t);
                float wShin = 0.85f * SS(0.5f, 0.92f, t);
                return WN(RigLayout.Hips, 1f - wLeg,
                    RigLayout.ThighL, wLeg * left * (1f - wShin), RigLayout.ThighR, wLeg * right * (1f - wShin),
                    RigLayout.ShinL, wLeg * left * wShin, RigLayout.ShinR, wLeg * right * wShin);
            };
            // Sand and smoke wound round it in two helical bands (each quad takes one colour, so the bands stay crisp).
            int rings = Segs(20, 14, 10), segs = Segs(20, 16, 12);
            float[] ts = new float[rings + 1];
            for (int r = 0; r <= rings; r++) ts[r] = Mathf.Pow((float)r / rings, 0.92f);
            int band = segs / 4;
            EmitTwistedLoft(T, ts, segs, 430f * twist, j => (j / band) % 2 == 0 ? DjinnSand : DjinnSmoke);
            // Smoke ribbons spiralling down round the column, edged with amber light.
            int ribbons = Segs(2, 2, 1);
            for (int rb = 0; rb < ribbons; rb++)
            {
                int n = Segs(14, 10, 7);
                Vector3[] pts = new Vector3[n];
                Vector3[] sides = new Vector3[n];
                float[] wid = new float[n];
                int[] bone = new int[n];
                float ph = rb * 360f / ribbons + 40f;
                for (int i = 0; i < n; i++)
                {
                    float u = (float)i / (n - 1);
                    float t = Mathf.Lerp(0.12f, 0.9f, u);
                    float th = ph + u * 540f;
                    Vector3 p = T.Point(t, th, 0.03f * s);
                    Vector3 c = T.Center(t);
                    pts[i] = p;
                    Vector3 axis = T.Center(Mathf.Min(1f, t + 0.02f)) - T.Center(Mathf.Max(0f, t - 0.02f));
                    sides[i] = Loft.Nrm(axis);
                    wid[i] = Mathf.Lerp(0.09f, 0.035f, u) * s;
                }
                EmitTatter(pts, sides, wid, _look.Implant + rb * 13, DjinnSmoke, DjinnSigil, true, i => T.WeightAt(Mathf.Lerp(0.12f, 0.9f, (float)i / (n - 1)), 0f));
            }
            // Loose smoke peeling off the column and trailing back.
            int wisps = Segs(5, 3, 0);
            for (int i = 0; i < wisps; i++)
            {
                float t = Mathf.Lerp(0.25f, 0.8f, (i + 0.5f) / wisps);
                float th = 200f + i * 67f + Hash01(_look.Implant + i * 9) * 30f;
                Vector3 root = T.Point(t, th, -0.01f * s);
                Vector3 outward = Loft.Nrm(root - T.Center(t));
                Vector3 dir = Loft.Nrm(outward * 0.8f + Vector3.back * 0.9f + Vector3.up * 0.25f);
                int n = Segs(6, 5, 4);
                Vector3[] pts = WispPoints(root, dir, Vector3.up * 0.04f, Mathf.Lerp(0.3f, 0.18f, t) * s, _look.Implant + 50 + i);
                Vector3[] sides = new Vector3[pts.Length];
                float[] wid = new float[pts.Length];
                for (int q = 0; q < pts.Length; q++)
                {
                    float u = (float)q / (pts.Length - 1);
                    sides[q] = Loft.Nrm(Vector3.Cross(dir, outward) + Vector3.up * 0.3f);
                    wid[q] = Mathf.Lerp(0.07f, 0.025f, u) * s;
                }
                BoneWeight bw = T.WeightAt(t, th);
                EmitTatter(pts, sides, wid, _look.Implant + 70 + i, i % 2 == 0 ? DjinnSmoke : DjinnSand, DjinnSigil, i % 3 == 0, q => bw);
            }
            // Stones and sand clots whirling round the column.
            int stones = Segs(8, 6, 3);
            for (int i = 0; i < stones; i++)
            {
                float u = (i + 0.5f) / stones;
                float t = Mathf.Lerp(0.2f, 0.85f, u);
                float th = i * 137.5f + Hash01(_look.Implant + i) * 40f;
                Vector3 c = T.Point(t, th, (0.07f + 0.06f * Hash01(_look.Implant + i * 3)) * s);
                float r = Mathf.Lerp(0.035f, 0.016f, u) * s * (0.8f + 0.4f * Hash01(_look.Implant + i * 5));
                EmitCutStone(c, new Vector3(r, r * 0.8f, r * 0.9f), QMath.Euler(i * 31f, i * 57f, i * 13f), 0.38f, 2, 0.4f, _look.Implant + i * 11, Paint.Topped(DjinnSand, Sandstone, 0.5f), T.WeightAt(t, th), float.NegativeInfinity);
            }
        }

        /// <summary>
        /// A loft whose columns wind round it (each column turns twistDeg from the first ring to the last), emitted quad by
        /// quad in columnUV(column): colour bands follow the quad edges, so a helix stays crisp with no stair steps.
        /// </summary>
        private void EmitTwistedLoft(Loft lf, float[] ts, int segs, float twistDeg, Func<int, Vector2> columnUV)
        {
            int rings = ts.Length;
            Vector3[,] p = new Vector3[rings, segs + 1], n = new Vector3[rings, segs + 1];
            Vector4[,] tg = new Vector4[rings, segs + 1];
            BoneWeight[,] w = new BoneWeight[rings, segs + 1];
            float t0 = ts[0], t1 = ts[rings - 1];
            for (int r = 0; r < rings; r++)
            {
                Loft.RingEval below = lf.EvalRing(ts[r] - Loft.StepT), mid = lf.EvalRing(ts[r]), above = lf.EvalRing(ts[r] + Loft.StepT);
                float rot = twistDeg * (ts[r] - t0) / Mathf.Max(1e-5f, t1 - t0);
                for (int j = 0; j <= segs; j++)
                {
                    float th = 360f * j / segs + rot;
                    lf.Frame(ref below, ref mid, ref above, th, out p[r, j], out n[r, j], out tg[r, j]);
                    w[r, j] = lf.WeightAt(ts[r], th);
                }
            }
            for (int j = 0; j < segs; j++)
            {
                Vector2 uv = columnUV(j);
                for (int r = 0; r < rings - 1; r++)
                {
                    int a = AddVertex(p[r, j], n[r, j], uv, new Vector2(j * 0.5f, r * 0.5f), w[r, j], tg[r, j]);
                    int b = AddVertex(p[r, j + 1], n[r, j + 1], uv, new Vector2((j + 1) * 0.5f, r * 0.5f), w[r, j + 1], tg[r, j + 1]);
                    int c = AddVertex(p[r + 1, j + 1], n[r + 1, j + 1], uv, new Vector2((j + 1) * 0.5f, (r + 1) * 0.5f), w[r + 1, j + 1], tg[r + 1, j + 1]);
                    int d = AddVertex(p[r + 1, j], n[r + 1, j], uv, new Vector2(j * 0.5f, (r + 1) * 0.5f), w[r + 1, j], tg[r + 1, j]);
                    Quad(a, b, c, d);
                }
            }
        }

        /// <summary>A big hand: a broad palm, four thick fingers and a thumb, each ending in a pointed gold nail.</summary>
        private void DjinnHand(int side)
        {
            float s = _s;
            float sx = side == 0 ? -1f : 1f;
            int hand = side == 0 ? RigLayout.HandL : RigLayout.HandR;
            int fore = side == 0 ? RigLayout.ForearmL : RigLayout.ForearmR;
            Vector3 wr = _L.World[hand];
            float hs = s * 1.3f;
            float palmLen = 0.1f * hs;
            float[] pk =
            {
                -0.05f, 0.022f, 0.03f, 0f, 0f, 2f,
                0.15f, 0.023f, 0.037f, 0f, 0.002f, 2.4f,
                0.5f, 0.021f, 0.044f, -0.002f, 0.002f, 2.8f,
                0.85f, 0.019f, 0.045f, -0.003f, 0f, 3f,
                1f, 0.017f, 0.041f, -0.004f, -0.002f, 2.8f
            };
            for (int i = 0; i < pk.Length; i += 6) { pk[i + 1] *= hs; pk[i + 2] *= hs; pk[i + 3] *= hs * sx; pk[i + 4] *= hs; }
            Loft P = new Loft(wr, wr + Vector3.down * palmLen, Vector3.right, Vector3.forward, pk);
            P.Weight = (t, th) => t < 0.12f ? W2(hand, fore, 0.3f * (1f - SS(-0.05f, 0.12f, t))) : W1(hand);
            _palm[side] = P;
            EmitLoft(P, -0.05f, 1f, Segs(7, 6, 4), Segs(14, 10, 8), t => DjinnSkin, false, true);
            int fingers = _detail == 0 ? 3 : 4;
            float[] zOff = { 0.028f, 0.009f, -0.01f, -0.027f };
            float[] len = { 0.072f, 0.08f, 0.076f, 0.06f };
            for (int fi = 0; fi < fingers; fi++)
            {
                int k = fingers == 4 ? fi : fi + (fi > 0 ? 1 : 0);
                Vector3 bp = wr + new Vector3(-sx * 0.004f * hs, -palmLen + 0.004f * hs, zOff[k] * hs);
                Vector3 dir = Loft.Nrm(new Vector3(-sx * 0.2f, -1f, k < 2 ? 0.04f : -0.03f));
                EmitClawFinger(bp, dir, new Vector3(-sx, 0f, 0f), len[k] * hs * 0.86f, (k == 3 ? 0.0084f : 0.0096f) * hs, 0.4f, 0.016f * hs, DjinnSkin, DjinnGold, hand);
            }
            Vector3 tb = wr + new Vector3(-sx * 0.008f * hs, -0.032f * hs, 0.03f * hs);
            EmitClawFinger(tb, Loft.Nrm(new Vector3(-sx * 0.55f, -0.62f, 0.5f)), new Vector3(-sx, 0f, 0f), 0.056f * hs, 0.0115f * hs, 0.2f, 0.014f * hs, DjinnSkin, DjinnGold, hand);
        }

        /// <summary>Gold collar over the shoulders, a gold belt with an amber clasp, and cloth flaps over the top of the tail.</summary>
        private void DjinnJewels()
        {
            float s = _s;
            float y0 = _torso.P0.y, span = _torso.P1.y - _torso.P0.y;
            Func<float, float> ty = y => (y * s - y0) / span;
            // A broad gold collar spreading over the collarbones and shoulders (crowd bodies drop the plates' rims).
            float rim = _detail == 0 ? 0f : 1f;
            EmitShapedPatch(_torso, ty(1.468f), ty(1.56f), u => new Vector2(-2f, 362f), 3, Segs(32, 24, 16), 0.005f * s + 0.009f * s * (1f - rim), 0.009f * s * rim, DjinnGold, DjinnGold);
            EmitBand(_torso, ty(1.49f), 0f, 360f, 0.01f * s, Segs(32, 24, 16), DjinnSigil, 0.0145f * s);
            // Belt with a sigil clasp.
            EmitPatch(_torso, ty(0.95f), ty(1.02f), 0f, 360f, 2, Segs(30, 22, 14), 0.012f * s + 0.012f * s * (1f - rim), 0.012f * s * rim, DjinnGold, DjinnGold);
            EmitEllipsoid(_torso.Point(ty(0.985f), 90f, 0.03f * s), new Vector3(0.03f, 0.03f, 0.012f) * s, QMath.Identity, Segs(7, 6, 4), Segs(12, 10, 8), DjinnSigil, W1(RigLayout.Hips));
            // Front and back flaps of deep cloth hanging from the belt over the top of the tail.
            for (int f = 0; f < 2; f++)
            {
                float th = f == 0 ? 90f : 270f;
                Vector3 root = _torso.Point(ty(0.96f), th, 0.02f * s);
                Vector3 outward = new Vector3(0f, 0f, f == 0 ? 1f : -1f);
                int n = Segs(6, 5, 4);
                Vector3[] pts = new Vector3[n];
                Vector3[] sides = new Vector3[n];
                float[] wid = new float[n];
                for (int i = 0; i < n; i++)
                {
                    float u = (float)i / (n - 1);
                    pts[i] = root + Vector3.down * (0.36f * s * u) + outward * (0.05f * s * u * u + 0.012f * s);
                    sides[i] = Vector3.right;
                    wid[i] = Mathf.Lerp(0.16f, 0.11f, u) * s;
                }
                EmitTatter(pts, sides, wid, _look.Implant + 90 + f, DjinnSmoke, DjinnGold, false, i => W1(RigLayout.Hips));
            }
        }

        /// <summary>Glowing amber sigils: a ringed seal on the breastbone, runes along the arms, marks on the face.</summary>
        private void DjinnSigils()
        {
            float s = _s;
            float y0 = _torso.P0.y, span = _torso.P1.y - _torso.P0.y;
            float off = 0.0025f * s, w = 0.009f * s;
            Func<float, float> ty = y => (y * s - y0) / span;
            // The seal: a ring of strokes round the breastbone with a vertical rune through it.
            float yc = 1.3f;
            float r = 0.075f;
            int n = Segs(16, 12, 8);
            for (int i = 0; i < n; i++)
            {
                if (i % 4 == 3) continue;
                float a0 = i * Mathf.PI * 2f / n, a1 = (i + 0.8f) * Mathf.PI * 2f / n;
                float t0 = ty(yc + Mathf.Sin(a0) * r), t1 = ty(yc + Mathf.Sin(a1) * r);
                float th0 = 90f + Mathf.Cos(a0) * r / 0.15f * 57.3f * 0.55f, th1 = 90f + Mathf.Cos(a1) * r / 0.15f * 57.3f * 0.55f;
                EmitVein(_torso, t0, th0, t1, th1, 0f, i, w * 0.8f, 3, DjinnSigil, off + 0.004f * s);
            }
            float dy = 0.045f, dth = 9f;
            EmitVein(_torso, ty(yc + dy), 90f, ty(yc), 90f + dth, 0f, 3, w * 0.8f, 3, DjinnSigil, off + 0.005f * s);
            EmitVein(_torso, ty(yc), 90f + dth, ty(yc - dy), 90f, 0f, 4, w * 0.8f, 3, DjinnSigil, off + 0.005f * s);
            EmitVein(_torso, ty(yc - dy), 90f, ty(yc), 90f - dth, 0f, 5, w * 0.8f, 3, DjinnSigil, off + 0.005f * s);
            EmitVein(_torso, ty(yc), 90f - dth, ty(yc + dy), 90f, 0f, 6, w * 0.8f, 3, DjinnSigil, off + 0.005f * s);
            for (int k = -1; k <= 1; k++)
                EmitVein(_torso, ty(yc + r + 0.015f), 90f + k * 9f, ty(yc + r + 0.06f - Mathf.Abs(k) * 0.015f), 90f + k * 15f, 0f, 7 + k, w * 0.75f, 3, DjinnSigil, off + 0.005f * s);
            // Rune lines down the obliques and across the back.
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                EmitVein(_torso, ty(1.03f), 90f + sx * 34f, ty(1.22f), 90f + sx * 52f, 2f, 10 + side, w * 0.8f, 6, DjinnSigil, off);
                EmitVein(_torso, ty(1.28f), 270f + sx * 20f, ty(1.43f), 270f + sx * 50f, 2f, 12 + side, w * 0.8f, 6, DjinnSigil, off);
                float lat = side == 0 ? 180f : 0f;
                // A spiral on each shoulder and a rune ladder down the forearm.
                EmitVein(_upper[side], 0.02f, 0.32f, u => lat + sx * (-60f + u * 300f), w * 0.85f, Segs(14, 10, 7), DjinnSigil, off, 0.15f);
                // A zigzag rune running down the forearm into the bracer.
                EmitVein(_fore[side], 0.06f, 0.44f, u => lat + sx * (55f + 22f * Mathf.Sin(u * Mathf.PI * 5f)), w * 0.75f, Segs(16, 12, 8), DjinnSigil, off, 0.1f);
            }
            // Face: a mark down the forehead and lines under the eyes.
            float hs = _headY1 - _headY0;
            Func<float, float> ht = y => (HY(y) * s - _headY0) / hs;
            EmitVein(_head, ht(0.077f), 90f, ht(0.05f), 90f, 0f, 40, 0.005f * s, 4, DjinnSigil, 0.0018f * s);
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                EmitVein(_head, ht(0.017f), 90f - sx * 14f, ht(-0.02f), 90f - sx * 30f, 0f, 41 + side, 0.0035f * s, 4, DjinnSigil, 0.0018f * s);
            }
        }
    }
}
