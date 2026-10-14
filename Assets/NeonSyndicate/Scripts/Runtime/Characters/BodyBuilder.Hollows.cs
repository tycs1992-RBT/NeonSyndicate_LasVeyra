using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class BodyBuilder
    {
        // ================================================================== Hollows

        /// <summary>
        /// A Hollow (or, frozen, a Snow Hollow): tall and gaunt with long limbs and long clawed fingers, a mask-like
        /// elongated skull with two burning slit eyes and a crest of spines, a smoky black body with a sheen, thin violet
        /// seams and rib veins, and smoke wisps streaming from the shoulders and forearms like dark flames. The Snow
        /// Hollow is the same body in frost white under jagged pale-blue ice plates, with icicles and blue light.
        /// </summary>
        private void BuildHollow(bool snow)
        {
            Vector2 skin = snow ? Ice : HollowSkin;
            Vector2 seam = snow ? FrostGlow : HollowSeam;
            HollowTorso(skin);
            HollowHead(snow, skin, seam);
            for (int side = 0; side < 2; side++)
            {
                HollowArm(side, skin, snow);
                HollowLeg(side, skin, snow);
            }
            HollowSeams(seam, snow);
            if (snow) SnowPlates();
            HollowTrails(snow);
        }

        /// <summary>A loft between two points whose cross-sections stay square to its axis (U right, V forward-ish).</summary>
        private static Loft TiltLoft(Vector3 p0, Vector3 p1, float[] keys)
        {
            Vector3 ax = Loft.Nrm(p1 - p0);
            Vector3 v = Loft.Nrm(Vector3.forward - ax * Vector3.Dot(Vector3.forward, ax));
            Vector3 u = Loft.Nrm(Vector3.Cross(v, ax));
            if (Vector3.Dot(u, Vector3.right) < 0f) u = -u;
            return new Loft(p0, p1, u, v, keys);
        }

        /// <summary>Piecewise-linear lookup in (x, y) pairs sorted by x.</summary>
        private static float Curve(float[] xy, float x)
        {
            if (x <= xy[0]) return xy[1];
            for (int i = 2; i < xy.Length; i += 2)
                if (x <= xy[i]) return Mathf.Lerp(xy[i - 1], xy[i + 1], (x - xy[i - 2]) / Mathf.Max(1e-6f, xy[i] - xy[i - 2]));
            return xy[xy.Length - 1];
        }

        private void HollowTorso(Vector2 skin)
        {
            float s = _s;
            // Hunched trapezius rising to a thin neck, a deep ribcage, a wasp waist and narrow, bony hips.
            float[] ys = { 0.94f, 0.965f, 1f, 1.04f, 1.09f, 1.15f, 1.22f, 1.29f, 1.36f, 1.43f, 1.49f, 1.54f, 1.575f, 1.605f, 1.63f, 1.655f, 1.675f, 1.69f };
            float[] a = { 0.035f, 0.088f, 0.11f, 0.116f, 0.09f, 0.083f, 0.108f, 0.126f, 0.136f, 0.142f, 0.154f, 0.17f, 0.182f, 0.172f, 0.14f, 0.1f, 0.068f, 0.036f };
            float[] b = { 0.035f, 0.064f, 0.073f, 0.072f, 0.058f, 0.057f, 0.079f, 0.094f, 0.101f, 0.1f, 0.092f, 0.084f, 0.078f, 0.072f, 0.066f, 0.058f, 0.05f, 0.034f };
            float[] cv = { 0f, -0.004f, -0.008f, -0.01f, -0.017f, -0.015f, -0.003f, 0.007f, 0.011f, 0.008f, -0.004f, -0.014f, -0.02f, -0.022f, -0.018f, -0.01f, 0f, 0.012f };
            float[] e = { 2f, 2.1f, 2.2f, 2.2f, 2f, 2f, 2.2f, 2.35f, 2.4f, 2.4f, 2.35f, 2.3f, 2.25f, 2.2f, 2.1f, 2.05f, 2f, 2f };
            float bw = Mathf.Lerp(0.94f, 1.06f, Mathf.InverseLerp(0.75f, 1.4f, _L.Bulk));
            Loft L = VLoft(Vector3.zero, ys, a, b, cv, e, bw, bw);
            float y0 = ys[0] * s, span = (ys[ys.Length - 1] - ys[0]) * s;
            L.Bump = (t, th) =>
            {
                float y = (y0 + t * span) / s;
                float k = 0f;
                // Ribs sloping down toward the front, a sunken belly, sharp hip bones, shoulder blades and vertebrae.
                float front = Mathf.Cos(Mathf.DeltaAngle(90f, th) * Mathf.Deg2Rad);
                float ribPhase = (y - 1.17f + 0.03f * front) / 0.046f;
                float ribs = Mathf.Max(0f, Mathf.Sin(ribPhase * Mathf.PI * 2f));
                k += 0.0055f * ribs * ribs * SS(1.16f, 1.2f, y) * (1f - SS(1.38f, 1.42f, y)) * (1f - GA(th, 270f, 30f)) * (1f - 0.6f * GA(th, 90f, 9f));
                k += 0.004f * GA(th, 90f, 7f) * SS(1.28f, 1.32f, y) * (1f - SS(1.48f, 1.52f, y));
                k -= 0.012f * G(y, 1.11f, 0.04f) * GA(th, 90f, 38f);
                k += 0.013f * G(y, 1.025f, 0.024f) * (GA(th, 28f, 16f) + GA(th, 152f, 16f));
                k += 0.011f * G(y, 1.44f, 0.05f) * (GA(th, 238f, 20f) + GA(th, 302f, 20f));
                k += 0.006f * GA(th, 270f, 6f) * (0.5f + 0.5f * Mathf.Cos((y - 1f) / 0.034f * Mathf.PI * 2f)) * SS(1f, 1.05f, y) * (1f - SS(1.6f, 1.66f, y));
                k += 0.006f * G(y, 1.55f, 0.01f) * (GA(th, 58f, 16f) + GA(th, 122f, 16f));
                return k * s;
            };
            float yTop = ys[ys.Length - 1] * s;
            L.Weight = (t, th) =>
            {
                Vector3 p = L.Center(t);
                float x = L.Point(t, th, 0f).x;
                return TorsoWeight(p.y, x, Mathf.Cos(th * Mathf.Deg2Rad), yTop - 0.07f * s, yTop);
            };
            _torso = L;
            List<float> rt = new List<float>();
            int bodyRings = Segs(22, 15, 10), topRings = Segs(12, 8, 5);
            float split = (1.5f - ys[0]) / (ys[ys.Length - 1] - ys[0]);
            for (int i = 0; i < bodyRings; i++) rt.Add(split * i / bodyRings);
            for (int i = 0; i <= topRings; i++)
            {
                float u = (float)i / topRings;
                rt.Add(Mathf.Lerp(split, 1f, 1f - Mathf.Pow(1f - u, 1.6f)));
            }
            EmitLoft(L, 0f, 1f, 0, Segs(26, 20, 14), t => skin, true, false, false, rt.ToArray());
        }

        private void HollowHead(bool snow, Vector2 skin, Vector2 seam)
        {
            float s = _s;
            Vector3 hb = _L.World[RigLayout.Head];
            // Neck: thin, thrust forward, corded.
            Vector3 n0 = new Vector3(0f, 1.6f * s, -0.01f * s);
            Vector3 n1 = hb + new Vector3(0f, 0.06f * s, 0.02f * s);
            float[] nk =
            {
                0f, 0.066f * s, 0.06f * s, 0f, -0.004f * s, 2f,
                0.3f, 0.05f * s, 0.048f * s, 0f, 0f, 2f,
                0.62f, 0.043f * s, 0.044f * s, 0f, 0.002f * s, 2f,
                1f, 0.04f * s, 0.042f * s, 0f, 0.002f * s, 2f
            };
            Loft N = TiltLoft(n0, n1, nk);
            N.Bump = (t, th) => s * (0.006f * G(t, 0.5f, 0.24f) * (GA(th, 62f, 11f) + GA(th, 118f, 11f)) + 0.004f * GA(th, 270f, 9f) * (0.5f + 0.5f * Mathf.Cos(t * 18f)));
            N.Weight = (t, th) =>
            {
                if (t < 0.32f) return W2(RigLayout.Chest, RigLayout.Neck, SS(0.02f, 0.32f, t));
                if (t < 0.72f) return W1(RigLayout.Neck);
                return W2(RigLayout.Neck, RigLayout.Head, SS(0.72f, 0.98f, t));
            };
            _neck = N;
            EmitLoft(N, 0f, 1f, Segs(9, 7, 5), Segs(16, 12, 10), t => skin, false, false);

            // Skull: long and narrow, swept back, a heavy brow, deep sockets, sharp cheekbones, a keel down the mask.
            _headCenter = hb + new Vector3(0f, 0.072f * s, 0.03f * s);
            float[] ys = { -0.115f, -0.105f, -0.085f, -0.055f, -0.025f, 0.005f, 0.035f, 0.065f, 0.095f, 0.125f, 0.15f, 0.168f };
            float[] a = { 0.008f, 0.024f, 0.038f, 0.048f, 0.056f, 0.062f, 0.067f, 0.069f, 0.066f, 0.056f, 0.036f, 0.008f };
            float[] b = { 0.008f, 0.03f, 0.05f, 0.066f, 0.08f, 0.092f, 0.102f, 0.11f, 0.114f, 0.108f, 0.086f, 0.02f };
            float[] cv = { 0.05f, 0.046f, 0.036f, 0.024f, 0.012f, 0f, -0.01f, -0.022f, -0.036f, -0.05f, -0.064f, -0.07f };
            float[] e = { 2f, 2f, 2.1f, 2.2f, 2.3f, 2.3f, 2.3f, 2.25f, 2.2f, 2.1f, 2f, 2f };
            Loft H = VLoft(_headCenter, ys, a, b, cv, e, 1f, 1f);
            _headY0 = ys[0] * s;
            _headY1 = ys[ys.Length - 1] * s;
            float span = _headY1 - _headY0;
            H.Bump = (t, th) =>
            {
                float y = (_headY0 + t * span) / s;
                float d = Mathf.DeltaAngle(90f, th);
                float k = 0f;
                k += 0.012f * G(y, 0.05f, 0.012f) * GA(d, 0f, 52f);
                k -= 0.013f * G(y, 0.027f, 0.012f) * (GA(d, -24f, 13f) + GA(d, 24f, 13f));
                k += 0.008f * G(y, -0.01f, 0.015f) * (GA(d, -48f, 14f) + GA(d, 48f, 14f));
                k -= 0.008f * G(y, -0.052f, 0.02f) * (GA(d, -50f, 16f) + GA(d, 50f, 16f));
                k += 0.006f * GA(d, 0f, 7f) * SS(-0.1f, -0.06f, y) * (1f - SS(0.035f, 0.055f, y));
                k += 0.006f * G(y, -0.072f, 0.02f) * (GA(d, -76f, 14f) + GA(d, 76f, 14f));
                k -= 0.006f * G(y, 0.062f, 0.02f) * (GA(d, -80f, 14f) + GA(d, 80f, 14f));
                k += 0.008f * GA(th, 270f, 10f) * SS(0.05f, 0.12f, y);
                return k * s;
            };
            H.Weight = (t, th) => W1(RigLayout.Head);
            _head = H;
            List<float> rt = new List<float>();
            float step = _detail == 2 ? 0.0085f : (_detail == 1 ? 0.013f : 0.02f);
            for (float y = ys[0]; y < ys[ys.Length - 1]; y += step) rt.Add((y - ys[0]) / (ys[ys.Length - 1] - ys[0]));
            rt.Add(1f);
            EmitLoft(H, 0f, 1f, 0, Segs(32, 24, 16), t => skin, true, true, true, rt.ToArray());
            Func<float, float> ht = y => (y * s - _headY0) / span;
            // The mask: a glossy plate over the face with a pointed brow, narrowing to the chin.
            Vector2 mask = snow ? DeepIce : Obsidian;
            float[] maskW = { -0.1f, 16f, -0.075f, 30f, -0.04f, 42f, -0.01f, 50f, 0.03f, 54f, 0.055f, 47f, 0.068f, 28f, 0.077f, 6f };
            EmitShapedPatch(H, ht(-0.1f), ht(0.077f), u =>
            {
                float hw = Curve(maskW, Mathf.Lerp(-0.1f, 0.077f, u));
                return new Vector2(90f - hw, 90f + hw);
            }, Segs(10, 7, 5), Segs(10, 7, 5), 0.0012f * s, 0.0022f * s, mask, mask);
            // Burning slit eyes, slanted up at the outer corners: a white-hot core in a glowing aura.
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                float th = 90f - sx * 23f;
                Vector3 p = H.Point(ht(0.027f), th, 0f);
                Vector3 nrm = H.Normal(ht(0.027f), th);
                Vector3 fwd = Vector3.Lerp(Vector3.forward, nrm, 0.5f);
                EmitSlitEye(p + nrm * 0.0036f * s, fwd, sx, 0.064f * s, 0.019f * s, 0.006f * s, 17f, seam, W1(RigLayout.Head));
                EmitSlitEye(p + nrm * 0.0052f * s, fwd, sx, 0.044f * s, 0.0085f * s, 0.008f * s, 17f, snow ? RimWhite : HollowEye, W1(RigLayout.Head));
            }
            // A glowing crack down the mask, and light leaking back along the temples from the eyes.
            EmitVein(H, ht(0.07f), 90f, ht(-0.098f), 90f, 2.5f, _look.Implant + 3, 0.0034f * s, Segs(10, 7, 5), seam, 0.0042f * s);
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                EmitVein(H, ht(0.03f), 90f - sx * 46f, ht(0.075f), 90f - sx * 122f, 3f, _look.Implant + 11 + side, 0.003f * s, Segs(8, 6, 4), seam, 0.0016f * s);
            }
            // A crest of spines sweeping back and up from the crown: smoke shards, or icicles on a Snow Hollow.
            int spines = Segs(7, 5, 5);
            for (int i = 0; i < spines; i++)
            {
                float u = (float)i / (spines - 1);
                float lat = (u - 0.5f) * 2f;
                float y = Mathf.Lerp(0.14f, 0.05f, Mathf.Abs(lat));
                float th = 270f + lat * 38f;
                Vector3 root = H.Point(ht(y), th, -0.008f * s);
                Vector3 dir = Loft.Nrm(new Vector3(lat * 0.55f, 0.42f - Mathf.Abs(lat) * 0.4f, -1f));
                float len = Mathf.Lerp(0.4f, 0.22f, Mathf.Abs(lat)) * s * Rand(0.92f, 1.08f);
                Vector3 across = Loft.Nrm(Vector3.Cross(dir, Vector3.up));
                if (across.sqrMagnitude < 0.5f) across = Vector3.right;
                Vector2 face = snow ? DeepIce : HollowSmoke;
                Vector2 bevel = snow ? Ice : (i % 2 == 0 ? seam : HollowSkin);
                EmitShard(root, dir, across, len, Mathf.Lerp(0.052f, 0.036f, Mathf.Abs(lat)) * s, 0.011f * s, -0.22f, Segs(4, 3, 2), face, bevel, u2 => W1(RigLayout.Head));
            }
        }

        private void HollowArm(int side, Vector2 skin, bool snow)
        {
            float s = _s;
            float sx = side == 0 ? -1f : 1f;
            int up = side == 0 ? RigLayout.UpperArmL : RigLayout.UpperArmR;
            int fore = side == 0 ? RigLayout.ForearmL : RigLayout.ForearmR;
            int hand = side == 0 ? RigLayout.HandL : RigLayout.HandR;
            Vector3 j = _L.World[up], el = _L.World[fore], wr = _L.World[hand];
            float k = Mathf.Lerp(0.9f, 1.08f, Mathf.InverseLerp(0.75f, 1.4f, _L.Bulk));
            float[] uk =
            {
                -0.07f, 0.01f, 0.012f, -0.04f, 0f, 2f,
                -0.04f, 0.028f, 0.032f, -0.025f, 0f, 2.1f,
                0f, 0.04f, 0.044f, -0.01f, 0.002f, 2.2f,
                0.08f, 0.045f, 0.046f, 0f, 0.003f, 2.2f,
                0.22f, 0.04f, 0.042f, 0f, 0.004f, 2.1f,
                0.5f, 0.034f, 0.036f, 0f, 0.004f, 2f,
                0.8f, 0.03f, 0.032f, 0f, 0.002f, 2f,
                0.95f, 0.031f, 0.033f, 0f, -0.004f, 2f,
                1.04f, 0.03f, 0.032f, 0f, -0.006f, 2f
            };
            Loft U = LimbLoft(j, el, uk, k, sx);
            U.Bump = (t, th) => s * (0.013f * G(t, 0.98f, 0.045f) * GA(th, 270f, 26f) + 0.005f * G(t, 0.12f, 0.1f) * GA(th, side == 0 ? 180f : 0f, 50f));
            U.Weight = UpperArmWeight(up, fore);
            _upper[side] = U;
            float[] fk =
            {
                -0.04f, 0.03f, 0.032f, 0f, -0.004f, 2f,
                0.1f, 0.034f, 0.033f, 0f, 0f, 2.1f,
                0.35f, 0.03f, 0.028f, 0f, 0.002f, 2.1f,
                0.7f, 0.022f, 0.022f, 0f, 0.001f, 2.1f,
                0.95f, 0.018f, 0.02f, 0f, 0f, 2.2f,
                1f, 0.018f, 0.02f, 0f, 0f, 2.2f
            };
            Loft F = LimbLoft(el, wr, fk, k, sx);
            float lat = side == 0 ? 180f : 0f;
            F.Bump = (t, th) => s * 0.004f * GA(th, lat + sx * 40f, 12f) * SS(0.05f, 0.2f, t) * (1f - SS(0.8f, 0.95f, t));
            F.Weight = ForearmWeight(up, fore, hand);
            _fore[side] = F;
            EmitLoft(U, -0.07f, 1.04f, Segs(16, 12, 8), Segs(14, 11, 8), t => skin, true, false);
            EmitLoft(F, -0.04f, 1f, Segs(12, 9, 6), Segs(12, 10, 8), t => skin, false, false);
            // Hand: a narrow palm and four long, jointed fingers ending in hooked claws.
            Vector2 claw = snow ? DeepIce : Obsidian;
            float palmLen = 0.075f * s;
            float[] pk =
            {
                -0.05f, 0.018f, 0.022f, 0f, 0f, 2f,
                0.2f, 0.016f, 0.03f, 0f, 0.002f, 2.4f,
                0.6f, 0.014f, 0.034f, -0.002f, 0.001f, 2.8f,
                1f, 0.012f, 0.031f, -0.003f, -0.002f, 2.6f
            };
            Loft P = LimbLoft(wr, wr + Vector3.down * palmLen, pk, 1f, sx);
            P.Weight = (t, th) => t < 0.12f ? W2(hand, fore, 0.3f * (1f - SS(-0.05f, 0.12f, t))) : W1(hand);
            _palm[side] = P;
            EmitLoft(P, -0.05f, 1f, Segs(6, 5, 4), Segs(12, 10, 8), t => skin, false, true);
            int fingers = _detail == 0 ? 3 : 4;
            for (int fi = 0; fi < fingers; fi++)
            {
                float zz = fingers == 4 ? Mathf.Lerp(0.024f, -0.024f, fi / 3f) : Mathf.Lerp(0.02f, -0.02f, fi / 2f);
                float len = (fingers == 4 ? (fi == 1 || fi == 2 ? 0.15f : 0.13f) : 0.14f) * s;
                Vector3 bp = wr + new Vector3(-sx * 0.004f * s, -palmLen + 0.005f * s, zz * s);
                Vector3 dir = Loft.Nrm(new Vector3(-sx * 0.12f, -1f, zz * 1.6f));
                EmitClawFinger(bp, dir, new Vector3(-sx, 0f, 0f), len, 0.0075f * s, 0.32f, 0.05f * s, skin, claw, hand);
            }
            Vector3 tb = wr + new Vector3(-sx * 0.008f * s, -0.025f * s, 0.028f * s);
            EmitClawFinger(tb, Loft.Nrm(new Vector3(-sx * 0.5f, -0.75f, 0.45f)), new Vector3(-sx, 0f, 0f), 0.085f * s, 0.009f * s, 0.25f, 0.04f * s, skin, claw, hand);
        }

        /// <summary>A long finger curling toward curlDir, then a hooked claw continuing the curl to a point.</summary>
        private void EmitClawFinger(Vector3 basePt, Vector3 dir, Vector3 curlDir, float length, float radius, float curl, float clawLen, Vector2 uv, Vector2 clawUV, int bone)
        {
            dir = Loft.Nrm(dir);
            Vector3 cu = Loft.Nrm(curlDir - dir * Vector3.Dot(curlDir, dir));
            Vector3 cv = Vector3.Cross(dir, cu);
            float c = curl * length;
            float[] keys =
            {
                0f, radius, radius, 0f, 0f, 2f,
                0.35f, radius * 0.92f, radius * 0.88f, c * 0.12f, 0f, 2f,
                0.7f, radius * 0.86f, radius * 0.8f, c * 0.49f, 0f, 2f,
                1f, radius * 0.74f, radius * 0.7f, c, 0f, 2f
            };
            Loft Fg = new Loft(basePt, basePt + dir * length, cu, cv, keys);
            Fg.Weight = (t, th) => W1(bone);
            EmitLoft(Fg, 0f, 1f, _detail == 0 ? 3 : 5, Segs(7, 6, 5), t => uv, false, false);
            Vector3 tip = Fg.Center(1f);
            Vector3 tan = Loft.Nrm(dir * length + cu * (2f * c));
            Vector3 cu2 = Loft.Nrm(cu - tan * Vector3.Dot(cu, tan));
            float[] ck =
            {
                0f, radius * 0.8f, radius * 0.76f, 0f, 0f, 2f,
                0.45f, radius * 0.62f, radius * 0.55f, clawLen * 0.08f, 0f, 2f,
                1f, radius * 0.06f, radius * 0.06f, clawLen * 0.32f, 0f, 2f
            };
            Loft C = new Loft(tip - tan * radius * 0.5f, tip + tan * clawLen, cu2, Vector3.Cross(tan, cu2), ck);
            C.Weight = (t, th) => W1(bone);
            EmitLoft(C, 0f, 1f, _detail == 0 ? 3 : 4, Segs(7, 6, 5), t => clawUV, false, true);
        }

        private void HollowLeg(int side, Vector2 skin, bool snow)
        {
            float s = _s;
            float sx = side == 0 ? -1f : 1f;
            int th = side == 0 ? RigLayout.ThighL : RigLayout.ThighR;
            int sh = side == 0 ? RigLayout.ShinL : RigLayout.ShinR;
            int ft = side == 0 ? RigLayout.FootL : RigLayout.FootR;
            Vector3 hip = _L.World[th], knee = _L.World[sh], ankle = _L.World[ft];
            float k = Mathf.Lerp(0.9f, 1.08f, Mathf.InverseLerp(0.75f, 1.4f, _L.Bulk));
            float[] tk =
            {
                -0.14f, 0.06f, 0.07f, -0.02f, -0.01f, 2.2f,
                -0.04f, 0.075f, 0.078f, -0.008f, -0.004f, 2.3f,
                0.1f, 0.071f, 0.072f, 0f, 0.004f, 2.2f,
                0.35f, 0.058f, 0.06f, 0.002f, 0.008f, 2.1f,
                0.65f, 0.047f, 0.049f, 0.001f, 0.006f, 2f,
                0.9f, 0.04f, 0.044f, 0f, 0.006f, 2f,
                1.04f, 0.042f, 0.046f, 0f, 0.01f, 2f
            };
            Loft T = LimbLoft(hip, knee, tk, k, sx);
            T.Bump = (t, a) => s * (0.013f * G(t, 1f, 0.04f) * GA(a, 90f, 26f) + 0.004f * G(t, 0.45f, 0.2f) * GA(a, 90f, 30f));
            T.Weight = ThighWeight(th, sh);
            _thigh[side] = T;
            float[] sk =
            {
                -0.04f, 0.043f, 0.047f, 0f, 0.008f, 2f,
                0.12f, 0.042f, 0.048f, 0f, -0.004f, 2f,
                0.32f, 0.038f, 0.047f, 0f, -0.011f, 2.1f,
                0.6f, 0.03f, 0.033f, 0f, -0.004f, 2f,
                0.85f, 0.022f, 0.024f, 0f, 0f, 2f,
                1.06f, 0.025f, 0.027f, 0f, 0.002f, 2f
            };
            Loft S = LimbLoft(knee, ankle, sk, k, sx);
            S.Bump = (t, a) => s * 0.0035f * GA(a, 90f, 10f) * SS(0.1f, 0.3f, t) * (1f - SS(0.8f, 0.95f, t));
            S.Weight = ShinWeight(th, sh, ft);
            _shin[side] = S;
            EmitLoft(T, -0.14f, 1.04f, Segs(16, 11, 8), Segs(14, 12, 10), t => skin, false, false);
            EmitLoft(S, -0.04f, 1.06f, Segs(14, 10, 7), Segs(14, 12, 10), t => skin, false, false);
            // Foot: long and narrow, high-arched, three clawed toes gripping the ground.
            Vector2 claw = snow ? DeepIce : Obsidian;
            Vector3 p0 = new Vector3(ankle.x, 0f, ankle.z - 0.065f * s);
            Vector3 p1 = new Vector3(ankle.x + sx * 0.008f * s, 0f, ankle.z + 0.15f * s);
            float[] fk =
            {
                -0.02f, 0.016f, 0.024f, 0f, 0.024f, 2.4f,
                0.06f, 0.03f, 0.046f, 0f, 0.046f, 2.8f,
                0.3f, 0.033f, 0.058f, 0f, 0.058f, 2.8f,
                0.55f, 0.034f, 0.042f, 0f, 0.042f, 3f,
                0.85f, 0.037f, 0.026f, 0f, 0.026f, 3f,
                1.02f, 0.03f, 0.017f, 0f, 0.017f, 2.6f
            };
            for (int i = 0; i < fk.Length; i += 6) { fk[i + 1] *= s; fk[i + 2] *= s; fk[i + 4] *= s; }
            Loft Fo = new Loft(p0, p1, Vector3.right, Vector3.up, fk);
            Fo.Weight = (t, a) => t > 0.12f && t < 0.38f && Mathf.Sin(a * Mathf.Deg2Rad) > 0.6f ? W2(ft, sh, 0.25f) : W1(ft);
            _foot[side] = Fo;
            EmitLoft(Fo, -0.02f, 1.02f, Segs(10, 8, 6), Segs(14, 11, 9), t => skin, true, true);
            for (int ti = 0; ti < 3; ti++)
            {
                float splay = (ti - 1) * 0.022f * s;
                float len = (ti == 1 ? 0.07f : 0.058f) * s;
                Vector3 bp = new Vector3(ankle.x + sx * 0.004f * s + splay, 0.022f * s, ankle.z + 0.125f * s);
                Vector3 dir = Loft.Nrm(new Vector3((ti - 1) * 0.25f, -0.06f, 1f));
                // The toe curls down so its claw tip just meets the ground.
                EmitClawFinger(bp, dir, Vector3.down, len, 0.011f * s, 0.08f, 0.022f * s, skin, claw, ft);
            }
        }

        /// <summary>The thin glowing seams: down every limb, the spine, rib veins, collarbones, neck cords and a cracked heart.</summary>
        private void HollowSeams(Vector2 seam, bool snow)
        {
            float s = _s;
            float wMain = 0.011f * s, wVein = 0.0065f * s, off = 0.0022f * s;
            for (int side = 0; side < 2; side++)
            {
                float lat = side == 0 ? 180f : 0f;
                float sx = side == 0 ? -1f : 1f;
                EmitVein(_upper[side], 0.04f, 0.98f, u => lat + sx * 18f * u, wMain, Segs(12, 9, 6), seam, off, 0.12f);
                EmitVein(_fore[side], -0.02f, 0.97f, u => lat + sx * 18f - sx * 30f * u, wMain, Segs(12, 9, 6), seam, off, 0.12f);
                EmitVein(_thigh[side], 0f, 0.98f, u => lat + sx * 10f * u, wMain, Segs(12, 9, 6), seam, off, 0.12f);
                EmitVein(_shin[side], 0f, 1f, u => lat + sx * 10f - sx * 70f * u, wMain, Segs(12, 9, 6), seam, off, 0.12f);
                if (_detail > 0)
                {
                    EmitVein(_fore[side], 0.15f, 90f + sx * 30f, 0.92f, 90f + sx * 5f, 6f, _look.Implant + 40 + side, wVein, 8, seam, off);
                    EmitVein(_thigh[side], 0.05f, 110f, 0.95f, 80f, 8f, _look.Implant + 50 + side, wVein, 8, seam, off);
                }
            }
            float y0 = _torso.P0.y, span = _torso.P1.y - _torso.P0.y;
            Func<float, float> ty = y => (y * s - y0) / span;
            EmitVein(_torso, ty(0.98f), ty(1.64f), u => 270f, wMain, Segs(16, 12, 8), seam, off + 0.004f * s, 0.08f);
            int ribs = Segs(4, 3, 2);
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                for (int r = 0; r < ribs; r++)
                {
                    float y = 1.21f + r * 0.058f;
                    EmitVein(_torso, ty(y), ty(y + 0.055f), u => 90f + sx * (10f + 120f * u), wVein * (1f - r * 0.08f), Segs(12, 9, 6), seam, off + 0.0035f * s, 0.2f);
                }
                EmitVein(_torso, ty(1.535f), ty(1.56f), u => 90f + sx * (12f + 64f * u), wVein, 8, seam, off + 0.004f * s, 0.2f);
                EmitVein(_neck, 0.12f, 0.92f, u => 90f + sx * (34f + 10f * u), wVein * 0.8f, Segs(8, 6, 4), seam, off, 0.15f);
            }
            // A cracked heart: short jagged rifts bursting from one point over the breastbone.
            float tc = ty(1.43f);
            float[] burst = { 0.07f, 9f, 0.035f, -20f, -0.05f, 14f, -0.065f, -8f, 0.012f, 24f };
            int rays = _detail == 0 ? 3 : 5;
            for (int i = 0; i < rays; i++)
            {
                float dt = burst[i * 2] / (span / s), dth = burst[i * 2 + 1];
                EmitVein(_torso, tc, 90f, tc + dt, 90f + dth, 5f, _look.Implant + 70 + i, wVein * 1.35f, 6, seam, off + 0.0055f * s);
            }
        }

        /// <summary>A jagged ice plate: a shaped patch whose edges step in and out row by row, like broken ice.</summary>
        private void IcePlate(Loft lf, float t0, float t1, float c0, float c1, float hw, int seed, float offset, float thick)
        {
            int nt = Segs(5, 4, 3), nth = Segs(4, 3, 2);
            EmitShapedPatch(lf, t0, t1, u =>
            {
                int row = Mathf.RoundToInt(u * nt);
                float c = Mathf.Lerp(c0, c1, u);
                // Rows step in and out sharply and the ends taper to points, so every plate reads as a broken shard.
                float end = Mathf.Min(u, 1f - u) * 2f;
                float taper = Mathf.Lerp(0.35f, 1f, Mathf.Sqrt(Mathf.Clamp01(end * 1.6f)));
                float j0 = (Hash01(seed + row * 17) - 0.5f) * 0.9f, j1 = (Hash01(seed + row * 23 + 5) - 0.5f) * 0.9f;
                return new Vector2(c - hw * taper * (1f + j0), c + hw * taper * (1f + j1));
            }, nt, nth, offset, thick, Snow, DeepIce);
        }

        /// <summary>Snow Hollow: jagged pale-blue ice plates over the frost-white body, dark cracks and blue light.</summary>
        private void SnowPlates()
        {
            float s = _s;
            float y0 = _torso.P0.y, span = _torso.P1.y - _torso.P0.y;
            Func<float, float> ty = y => (y * s - y0) / span;
            float th = 0.009f * s, off = 0.003f * s;
            int seed = _look.Implant * 3;
            IcePlate(_torso, ty(1.34f), ty(1.52f), 64f, 70f, 17f, seed + 1, off, th);
            IcePlate(_torso, ty(1.34f), ty(1.52f), 116f, 110f, 17f, seed + 2, off, th);
            if (_detail > 0)
            {
                IcePlate(_torso, ty(1.17f), ty(1.31f), 74f, 78f, 13f, seed + 3, off, th);
                IcePlate(_torso, ty(1.17f), ty(1.31f), 106f, 102f, 13f, seed + 4, off, th);
                IcePlate(_torso, ty(1.38f), ty(1.56f), 244f, 238f, 20f, seed + 5, off, th);
                IcePlate(_torso, ty(1.38f), ty(1.56f), 296f, 302f, 20f, seed + 6, off, th);
            }
            if (_detail == 2)
            {
                IcePlate(_torso, ty(0.99f), ty(1.07f), 30f, 26f, 18f, seed + 7, off, th * 0.8f);
                IcePlate(_torso, ty(0.99f), ty(1.07f), 150f, 154f, 18f, seed + 8, off, th * 0.8f);
            }
            for (int side = 0; side < 2; side++)
            {
                float lat = side == 0 ? 180f : 0f;
                float sx = side == 0 ? -1f : 1f;
                IcePlate(_upper[side], -0.08f, 0.32f, lat - sx * 10f, lat + sx * 10f, 95f, seed + 10 + side, off, th * 1.25f);
                IcePlate(_fore[side], 0.1f, 0.78f, lat - sx * 20f, lat, 62f, seed + 12 + side, off, th);
                if (_detail == 0) continue;
                IcePlate(_thigh[side], 0.14f, 0.62f, 86f, 94f, 34f, seed + 14 + side, off, th);
                IcePlate(_shin[side], 0.1f, 0.74f, 90f, 90f, 30f, seed + 16 + side, off, th);
            }
            // Cracks: dark fissures across the frost, a few of them lit from inside.
            int cracks = Segs(12, 8, 3);
            for (int i = 0; i < cracks; i++)
            {
                float t0 = Mathf.Lerp(0.08f, 0.94f, Hash01(seed + i * 31));
                float th0 = Hash01(seed + i * 37) * 360f;
                float t1 = Mathf.Clamp01(t0 + (Hash01(seed + i * 41) - 0.5f) * 0.18f);
                float th1 = th0 + (Hash01(seed + i * 43) - 0.5f) * 70f;
                EmitVein(_torso, t0, th0, t1, th1, 6f, seed + i, (i % 3 == 0 ? 0.0045f : 0.0032f) * s, 6, i % 3 == 0 ? FrostGlow : DeepIce, 0.0018f * s);
            }
            for (int side = 0; side < 2; side++)
            {
                float lat = side == 0 ? 180f : 0f;
                EmitVein(_thigh[side], 0.65f, lat + 60f, 0.98f, lat + 10f, 8f, seed + 80 + side, 0.0035f * s, 6, DeepIce, 0.0018f * s);
                EmitVein(_shin[side], 0.75f, 60f, 1f, 120f, 8f, seed + 82 + side, 0.0035f * s, 6, DeepIce, 0.0018f * s);
                EmitVein(_upper[side], 0.4f, lat + 90f, 0.9f, lat + 140f, 8f, seed + 84 + side, 0.0032f * s, 6, FrostGlow, 0.0018f * s);
            }
        }

        /// <summary>Smoke wisps (or icicles) streaming from the shoulders and forearms, and a ragged hem at the waist.</summary>
        private void HollowTrails(bool snow)
        {
            float s = _s;
            int seed = _look.Implant;
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                int up = side == 0 ? RigLayout.UpperArmL : RigLayout.UpperArmR;
                int fore = side == 0 ? RigLayout.ForearmL : RigLayout.ForearmR;
                float lat = side == 0 ? 180f : 0f;
                // Shoulders: dark flames rising off the top and back of the shoulder, curling back.
                int n = snow ? Segs(4, 3, 2) : Segs(5, 4, 2);
                for (int i = 0; i < n; i++)
                {
                    float u = n == 1 ? 0.5f : (float)i / (n - 1);
                    float th = lat - sx * Mathf.Lerp(-25f, 85f, u);
                    float t = Mathf.Lerp(-0.03f, 0.14f, u);
                    Vector3 root = _upper[side].Point(t, th, -0.006f * s);
                    Vector3 dir = Loft.Nrm(new Vector3(sx * Mathf.Lerp(0.6f, 0.15f, u), Mathf.Lerp(1f, 0.7f, u), -Mathf.Lerp(0.2f, 0.9f, u)));
                    float len = Mathf.Lerp(0.26f, 0.46f, Hash01(seed + side * 7 + i * 3)) * s;
                    if (snow) Icicle(root, Loft.Nrm(dir + Vector3.up * 0.4f), len * 0.65f, 0.032f * s, up);
                    else Flame(root, dir, len, Mathf.Lerp(0.05f, 0.068f, u) * s, seed + side * 10 + i, i % 2 == 0, up);
                }
                // Forearms: streamers trailing back off the outer forearm.
                int nf = Segs(2, 2, 1);
                for (int i = 0; i < nf; i++)
                {
                    float t = i == 0 ? 0.28f : 0.58f;
                    float th = lat - sx * (i == 0 ? 50f : 75f);
                    Vector3 root = _fore[side].Point(t, th, -0.005f * s);
                    Vector3 dir = Loft.Nrm(new Vector3(sx * 0.35f, i == 0 ? 0.35f : 0.1f, -1f));
                    float len = (i == 0 ? 0.36f : 0.28f) * s * Rand(0.9f, 1.1f);
                    if (snow) Icicle(root, Loft.Nrm(new Vector3(sx * 0.4f, -0.5f, -0.8f)), len * 0.6f, 0.028f * s, fore);
                    else Flame(root, dir, len, 0.06f * s, seed + 30 + side * 10 + i, true, fore);
                }
            }
            // Waist: a ragged hem, long at the back and short at the front.
            float y0 = _torso.P0.y, span = _torso.P1.y - _torso.P0.y;
            int nBack = snow ? Segs(5, 3, 3) : Segs(5, 4, 3), nFront = snow ? Segs(2, 0, 0) : Segs(3, 3, 2);
            int nw = nBack + nFront;
            for (int i = 0; i < nw; i++)
            {
                bool frontHem = i >= nBack;
                float u = frontHem ? (nFront == 1 ? 0.5f : (float)(i - nBack) / (nFront - 1)) : (float)i / (nBack - 1);
                float th = frontHem ? Mathf.Lerp(62f, 118f, u) : Mathf.Lerp(205f, 335f, u);
                Vector3 root = _torso.Point((1.05f * s - y0) / span, th, -0.006f * s);
                Vector3 outDir = new Vector3(Mathf.Cos(th * Mathf.Deg2Rad), 0f, Mathf.Sin(th * Mathf.Deg2Rad));
                float len = (frontHem ? Mathf.Lerp(0.2f, 0.28f, 1f - Mathf.Abs(u - 0.5f) * 2f) : Mathf.Lerp(0.38f, 0.56f, 1f - Mathf.Abs(u - 0.5f) * 2f)) * s * Rand(0.9f, 1.1f);
                int thighBone = Mathf.Cos(th * Mathf.Deg2Rad) < 0f ? RigLayout.ThighL : RigLayout.ThighR;
                float side01 = Mathf.Abs(Mathf.Cos(th * Mathf.Deg2Rad));
                if (snow)
                {
                    Icicle(root, Loft.Nrm(Vector3.down + outDir * 0.25f), len * 0.42f, 0.03f * s, RigLayout.Hips);
                    continue;
                }
                Vector3 drift = outDir * 0.08f + (frontHem ? Vector3.forward * 0.03f : Vector3.back * 0.05f);
                Vector3 across = Loft.Nrm(Vector3.Cross(Vector3.down, outDir));
                int sd = seed + 60 + i;
                Vector3[] pts = WispPoints(root, Vector3.down, drift, len, sd);
                Vector3[] sides = new Vector3[pts.Length];
                float[] wid = new float[pts.Length];
                for (int q = 0; q < pts.Length; q++) { sides[q] = across; wid[q] = Mathf.Lerp(frontHem ? 0.075f : 0.085f, 0.04f, (float)q / (pts.Length - 1)) * s; }
                EmitTatter(pts, sides, wid, sd, HollowSmoke, HollowSeam, i % 2 == 1, q => W2(RigLayout.Hips, thighBone, 0.55f * side01 * (float)q / (pts.Length - 1)));
            }
        }

        /// <summary>The centre line of a wisp: from root along dir, drifting and curling as it goes.</summary>
        private Vector3[] WispPoints(Vector3 root, Vector3 dir, Vector3 drift, float len, int seed)
        {
            int n = Segs(7, 5, 4);
            Vector3[] pts = new Vector3[n];
            float ph = Hash01(seed) * 6.283f;
            Vector3 side = Vector3.Cross(dir, drift.sqrMagnitude > 1e-6f ? drift : Vector3.forward);
            side = side.sqrMagnitude > 1e-8f ? Loft.Nrm(side) : Vector3.right;
            for (int i = 0; i < n; i++)
            {
                float u = (float)i / (n - 1);
                Vector3 curl = side * (Mathf.Sin(u * 5.5f + ph) * 0.055f * u * _s);
                pts[i] = root + dir * (len * u) + drift * (_s * u * (0.6f + 0.6f * u)) + curl;
            }
            return pts;
        }

        /// <summary>A rising smoke flame: wide at its root, curling, ending in a forked tip, its broad face turned by a seeded angle.</summary>
        private void Flame(Vector3 root, Vector3 dir, float len, float width, int seed, bool glow, int bone)
        {
            Vector3 drift = Vector3.back * 0.06f + new Vector3(0f, 0.02f, 0f);
            Vector3[] pts = WispPoints(root, dir, drift, len, seed);
            Vector3[] sides = new Vector3[pts.Length];
            float[] wid = new float[pts.Length];
            float ang = Hash01(seed * 5 + 1) * 140f - 70f;
            for (int q = 0; q < pts.Length; q++)
            {
                float u = (float)q / (pts.Length - 1);
                Vector3 along = q < pts.Length - 1 ? pts[q + 1] - pts[q] : pts[q] - pts[q - 1];
                Vector3 baseSide = Vector3.Cross(Loft.Nrm(along), Vector3.forward);
                if (baseSide.sqrMagnitude < 1e-6f) baseSide = Vector3.right;
                Vector3 a = Loft.Nrm(baseSide);
                Vector3 b = Vector3.Cross(Loft.Nrm(along), a);
                float r = (ang + 25f * u) * Mathf.Deg2Rad;
                sides[q] = Loft.Nrm(a * Mathf.Cos(r) + b * Mathf.Sin(r));
                wid[q] = width * (u < 0.25f ? Mathf.Lerp(0.7f, 1f, u / 0.25f) : Mathf.Lerp(1f, 0.35f, (u - 0.25f) / 0.75f)) * (0.88f + 0.24f * Hash01(seed + q * 7));
            }
            EmitTatter(pts, sides, wid, seed, HollowSmoke, HollowSeam, glow, q => W1(bone));
        }

        private void Icicle(Vector3 root, Vector3 dir, float len, float width, int bone)
        {
            Vector3 across = Loft.Nrm(Vector3.Cross(dir, Vector3.forward));
            if (across.sqrMagnitude < 0.5f) across = Vector3.right;
            EmitShard(root, dir, across, len, width, width * 0.55f, 0.06f, Segs(3, 3, 2), DeepIce, FrostGlow, u => W1(bone));
        }
    }
}
