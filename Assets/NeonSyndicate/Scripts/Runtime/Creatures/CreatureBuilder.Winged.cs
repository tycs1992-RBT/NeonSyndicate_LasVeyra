using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Winged bodies: the quadruped trunk, head, legs and tail (no forelegs for the wyvern, whose wings are its arms),
    /// plus two wings bound spread out flat. Membrane wings (wyvern, drake) have arm, forearm, hand and three finger
    /// bones; the membrane is a thin double-sided sheet in panels between the fingers and from the last finger back to
    /// the flank, scalloped along the trailing edge, with glowing veins on both faces. Feathered wings (griffins) have
    /// arm, forearm and hand bones carrying a fleshy leading edge, a base sheet and layered primaries, secondaries,
    /// tertials and coverts.
    /// </summary>
    internal sealed partial class CreatureSculptor
    {
        private struct WingPlan
        {
            public int Side;
            public Vector3 S, E, Wr, Hd;
            public Vector3[] Tips;
            public int Arm, Fore, Hand;
            public int[] Fingers;
            public float Len;
        }

        private readonly List<WingPlan> _wings = new List<WingPlan>();

        /// <summary>Bind fan of a membrane wing's three finger bones: degrees forward of straight out from the hand.</summary>
        internal static readonly float[] MembraneFan = { 21f, -11.5f, -42f };
        /// <summary>Bind fan of the three bones that carry a feathered wing's primaries (closing it folds the primaries).</summary>
        internal static readonly float[] FeatherFan = { 8f, -32f, -72f };

        /// <summary>Feathered wings (griffins) rather than membranes.</summary>
        internal static bool IsFeathered(CreatureLook k) { return k.Feathers || k.Coat == 2; }

        private bool Feathered { get { return IsFeathered(_k); } }

        private void BuildWinged()
        {
            _hasForelegs = _k.Forelegs;
            QuadLayout();
            QuadSkeleton();
            WingSkeleton();
            TileForCoat();
            BeginCore();
            QuadBody();
            QuadHead();
            for (int i = 0; i < _legs.Count; i++) QuadLeg(_legs[i]);
            EndCore();
            QuadTail();
            QuadDetails();
            if (Feathered) GriffinPlumage();
            foreach (WingPlan w in _wings)
            {
                if (Feathered) FeatherWing(w);
                else MembraneWing(w);
            }
            AnatomyMotion04K();
            // H8S obsidian look, before the quartz regalia so the regalia's own vertex order is kept.
            ObsidianH8S();
            if (_k.CrystalRegalia) QuartzRegalia();
            QuadHits();
            foreach (WingPlan w in _wings)
            {
                // Thin capsules along the arm and forearm, and across the hand or the middle finger.
                float r = 0.05f * _H * (Feathered ? 1f : 0.9f);
                Hit(w.Arm, w.S, w.E, r * 1.4f);
                Hit(w.Fore, w.E, w.Wr, r);
                if (w.Fingers.Length > 1) Hit(w.Fingers[1], w.Hd, Vector3.Lerp(w.Hd, w.Tips[1], 0.85f), r * 0.6f);
                else Hit(w.Hand, w.Wr, w.Hd + (w.Hd - w.Wr) * 1.4f, r * 0.8f);
            }
            QuadMetrics();
            float half = Mathf.Max(0.5f, _k.WingSpan * _s * 0.5f);
            // Wings sweep up and down by most of their length when flapping, and forward when flaring to land.
            _poseGrow = new Vector3(Mathf.Max(_poseGrow.x, 0.45f * half), Mathf.Max(_poseGrow.y, half * 1.1f), Mathf.Max(_poseGrow.z, half * 1.1f));
        }

        // ------------------------------------------------------------------ skeleton

        private void WingSkeleton()
        {
            float half = Mathf.Max(0.3f, _k.WingSpan * _s * 0.5f);
            bool feathers = Feathered;
            bool wyvern = !_hasForelegs;
            List<WingChain> chains = new List<WingChain>();
            for (int side = -1; side <= 1; side += 2)
            {
                WingPlan w = new WingPlan();
                w.Side = side;
                float sx = side;
                // Wings root high on the back behind the withers; a wyvern's wings are its arms, rooted at the shoulders.
                w.S = wyvern ? new Vector3(sx * 0.7f * _W, 0.78f * _H, _zS - 0.1f * _H) : new Vector3(sx * 0.62f * _W, 0.93f * _H, _zS - 0.3f * _H);
                float len = half - Mathf.Abs(w.S.x);
                w.Len = len;
                Vector3 dA = Nrm(new Vector3(sx, 0.12f, -0.16f)), dF = Nrm(new Vector3(sx, 0.02f, 0.2f)), dH = Nrm(new Vector3(sx, 0f, 0.04f));
                float rA = feathers ? 0.27f : 0.24f, rF = feathers ? 0.33f : 0.34f, rH = feathers ? 0.17f : 0.05f;
                w.E = w.S + dA * (rA * len);
                w.Wr = w.E + dF * (rF * len);
                w.Hd = w.Wr + dH * (rH * len);
                w.Arm = Bone("WingArm" + (side < 0 ? ".L" : ".R"), _chest, w.S);
                w.Fore = Bone("WingFore" + (side < 0 ? ".L" : ".R"), w.Arm, w.E);
                w.Hand = Bone("WingHand" + (side < 0 ? ".L" : ".R"), w.Fore, w.Wr);
                // Three bones fan from the hand: a membrane wing's long fingers (the leading one runs to the wing tip), or
                // the carriers of a feathered wing's primaries.
                float[] fan = feathers ? FeatherFan : MembraneFan;
                float[] fl = feathers ? new[] { 0.4f, 0.38f, 0.34f } : new[] { 0.44f, 0.39f, 0.33f };
                w.Fingers = new int[3];
                w.Tips = new Vector3[3];
                for (int f = 0; f < 3; f++)
                {
                    float a = fan[f] * Mathf.Deg2Rad;
                    Vector3 fd = Nrm(new Vector3(sx * Mathf.Cos(a), 0.02f * (1 - f), Mathf.Sin(a)));
                    w.Fingers[f] = Bone("WingFinger" + (f + 1) + (side < 0 ? ".L" : ".R"), w.Hand, w.Hd);
                    w.Tips[f] = w.Hd + fd * (fl[f] * len);
                }
                _wings.Add(w);
                WingChain c = new WingChain();
                c.Arm = w.Arm; c.Fore = w.Fore; c.Hand = w.Hand; c.Side = side;
                chains.Add(c);
            }
            _sk.Wings = chains.ToArray();
        }

        // ------------------------------------------------------------------ weights helper

        /// <summary>Blend of two weights (up to eight influences merged, the four strongest kept and normalised).</summary>
        private static BoneWeight Blend(BoneWeight a, BoneWeight b, float t)
        {
            t = Mathf.Clamp01(t);
            int[] bi = new int[8];
            float[] bw = new float[8];
            int n = 0;
            void Add(int bone, float w)
            {
                if (w <= 1e-5f) return;
                for (int i = 0; i < n; i++) if (bi[i] == bone) { bw[i] += w; return; }
                bi[n] = bone; bw[n] = w; n++;
            }
            float ta = 1f - t;
            Add(a.boneIndex0, a.weight0 * ta); Add(a.boneIndex1, a.weight1 * ta); Add(a.boneIndex2, a.weight2 * ta); Add(a.boneIndex3, a.weight3 * ta);
            Add(b.boneIndex0, b.weight0 * t); Add(b.boneIndex1, b.weight1 * t); Add(b.boneIndex2, b.weight2 * t); Add(b.boneIndex3, b.weight3 * t);
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                    if (bw[j] > bw[i]) { float tw = bw[i]; bw[i] = bw[j]; bw[j] = tw; int tb = bi[i]; bi[i] = bi[j]; bi[j] = tb; }
            int keep = Mathf.Min(4, n);
            float sum = 0f;
            for (int i = 0; i < keep; i++) sum += bw[i];
            BoneWeight r = new BoneWeight();
            if (keep == 0 || sum < 1e-6f) return a;
            r.boneIndex0 = bi[0]; r.weight0 = bw[0] / sum;
            if (keep > 1) { r.boneIndex1 = bi[1]; r.weight1 = bw[1] / sum; }
            if (keep > 2) { r.boneIndex2 = bi[2]; r.weight2 = bw[2] / sum; }
            if (keep > 3) { r.boneIndex3 = bi[3]; r.weight3 = bw[3] / sum; }
            return r;
        }

        // ------------------------------------------------------------------ membrane wings

        private void MembraneWing(WingPlan w)
        {
            float len = w.Len;
            float sx = w.Side;
            Vector2 hide = Pal(_k.Hide), mem = Pal(_k.Membrane), glow = Pal(_k.Glow), claw = Pal(_k.Claw);
            int arm = w.Arm, fore = w.Fore, hand = w.Hand;
            int[] fg = w.Fingers;
            float th = Mathf.Max(0.006f, 0.0045f * len);
            int nu = Segs(8, 5, 4), nv = Segs(5, 3, 3);
            // Arm and forearm: one muscular strut, thick at the shoulder, tapering to a bony wrist.
            Vector3 inner = w.S - new Vector3(sx * 0.12f * len, 0.02f * len, 0f);
            Vector3[] ac = { inner, w.S, w.E, w.Wr, w.Hd };
            float r0 = 0.06f * len;
            CreatureTube at = new CreatureTube(ac, Vector3.up, new float[] { 0f, 0.1f, 0.1f, 0.1f, 2f });
            float tS = at.CtrlT(1), tE = at.CtrlT(2), tW = at.CtrlT(3);
            at.SetKeys(new[]
            {
                0f, r0 * 1.1f, r0 * 1.2f, r0 * 1.2f, 2.1f,
                tS, r0 * 1.15f, r0 * 1.1f, r0 * 1.15f, 2.2f,
                Mathf.Lerp(tS, tE, 0.5f), r0 * 0.85f, r0 * 0.75f, r0 * 0.8f, 2.2f,
                tE, r0 * 0.6f, r0 * 0.55f, r0 * 0.6f, 2.1f,
                Mathf.Lerp(tE, tW, 0.5f), r0 * 0.42f, r0 * 0.36f, r0 * 0.4f, 2.1f,
                tW, r0 * 0.36f, r0 * 0.3f, r0 * 0.34f, 2f,
                1f, r0 * 0.25f, r0 * 0.22f, r0 * 0.22f, 2f
            });
            int chest = _chest;
            at.Weight = (t, a) =>
            {
                if (t < tS) return W2(chest, arm, SS(0f, tS, t));
                if (t < tE) return W2(arm, fore, SS(tE - 0.08f, tE + 0.03f, t));
                if (t < tW) return W2(fore, hand, SS(tW - 0.05f, tW + 0.02f, t));
                return W1(hand);
            };
            EmitTube(at, RingsEased(0f, 1f, Segs(16, 11, 7), 0.2f), Segs(12, 9, 6), (t, a) => hide, null, true, true);
            // Fingers: long bony rods tapering to a claw point.
            for (int f = 0; f < fg.Length; f++)
            {
                Vector3 tip = w.Tips[f];
                float fr = r0 * (0.26f - f * 0.03f);
                EmitHorn(w.Hd, tip, Vector3.up, 0.01f * len, fr, Segs(6, 5, 4), Segs(7, 6, 5), hide, W1(fg[f]), W1(fg[f]), 0.8f, 0.08f, true);
            }
            // A hooked thumb claw at the wrist (a wyvern walks on it).
            EmitHorn(w.Wr + Vector3.forward * (0.01f * len), w.Wr + Nrm(new Vector3(sx * 0.2f, -0.3f, 1f)) * (0.09f * len), Vector3.down, 0.015f * len, 0.025f * len, 5, 6, claw, W1(hand), W1(hand), 1f, 0.1f, true);
            // Panels between the fingers: scalloped trailing edge, a little camber.
            Vector3 up = Vector3.up;
            for (int f = 0; f < fg.Length - 1; f++)
            {
                Vector3 a = w.Tips[f], b = w.Tips[f + 1];
                Vector3 mid = Vector3.Lerp(a, b, 0.5f);
                Vector3 ctl = Vector3.Lerp(mid, w.Hd, 0.32f);
                int fa = fg[f], fb = fg[f + 1];
                float span = (a - b).magnitude;
                Func<float, float, Vector3> P = (u, v) =>
                {
                    Vector3 edge = (1f - v) * (1f - v) * a + 2f * v * (1f - v) * ctl + v * v * b;
                    Vector3 p = Vector3.Lerp(w.Hd, edge, u);
                    // Camber: the sheet bellies up a little between its rods.
                    p.y += Mathf.Sin(Mathf.PI * v) * Mathf.Sin(Mathf.PI * Mathf.Min(1f, u * 1.1f)) * 0.04f * span;
                    return p;
                };
                Func<float, float, BoneWeight> Wt = (u, v) => Blend(Blend(W1(hand), W1(fa), SS(0f, 0.25f, u)), Blend(W1(hand), W1(fb), SS(0f, 0.25f, u)), v);
                EmitSheet(nu, nv, P, Wt, up, th, mem, mem);
                Veins(P, Wt, up, th, len, 2, SeamUV);
            }
            // The big panel from the last finger back to the flank and along the arm to the shoulder.
            Vector3 hip = new Vector3(sx * 0.7f * _W, 0.66f * _H, _zB + 0.32f * _trunk);
            Vector3 f3 = w.Tips[fg.Length - 1];
            Vector3 scal = Vector3.Lerp(Vector3.Lerp(f3, hip, 0.5f), w.E, 0.22f);
            // Bony edge: f3 tip, hand, wrist, elbow, shoulder. Trailing edge: f3 tip, scallop, hip, along the body to the shoulder.
            Vector3[] bony = { f3, w.Hd, w.Wr, w.E, w.S };
            Vector3[] trail = { f3, scal, hip, Vector3.Lerp(hip, w.S, 0.5f) + new Vector3(sx * 0.15f * _W, -0.05f * _H, 0f), w.S };
            int last = fg[fg.Length - 1];
            int[] bonyB = { last, hand, hand, fore, arm };
            int pelvis = _pelvis, spine = _spine;
            Func<float, Vector3> B1 = s => Poly(bony, s);
            Func<float, Vector3> B2 = s =>
            {
                Vector3 q = Poly(trail, s);
                return q;
            };
            Func<float, BoneWeight> W1s = s =>
            {
                float x = s * (bony.Length - 1);
                int i = Mathf.Clamp((int)x, 0, bony.Length - 2);
                return W2(bonyB[i], bonyB[i + 1], x - i);
            };
            Func<float, BoneWeight> W2s = s =>
            {
                // Trailing edge: the last finger near the tip, the body (hips, then spine, then chest) along the flank.
                if (s < 0.5f) return Blend(W1(last), W1(pelvis), SS(0.1f, 0.5f, s));
                return W2(pelvis, spine >= 0 ? spine : chest, SS(0.5f, 1f, s));
            };
            Func<float, float, Vector3> PP = (s, v) =>
            {
                Vector3 p = Vector3.Lerp(B1(s), B2(s), v);
                p.y += Mathf.Sin(Mathf.PI * v) * Mathf.Sin(Mathf.PI * s) * 0.03f * len;
                return p;
            };
            Func<float, float, BoneWeight> WP = (s, v) => Blend(W1s(s), W2s(s), SS(0f, 1f, v));
            EmitSheet(Segs(12, 7, 5), nv + 1, PP, WP, up, th, mem, mem);
            Veins(PP, WP, up, th, len, 4, SeamUV, true);
            // Leading edge membrane in front of the arm and forearm (the wing's front web).
            Vector3[] lead = { w.S, w.E, w.Wr };
            Vector3 fwd = Vector3.forward * (0.07f * len);
            Func<float, float, Vector3> PL = (s, v) => Poly(lead, s) + fwd * (v * Mathf.Sin(Mathf.PI * s));
            Func<float, float, BoneWeight> WL = (s, v) => s < 0.5f ? W2(arm, fore, SS(0.3f, 0.5f, s)) : W2(fore, hand, SS(0.85f, 1f, s));
            EmitSheet(Segs(6, 4, 3), 1, PL, WL, up, th, mem, mem);
        }

        /// <summary>Point at s (0..1, by segment count) along a polyline.</summary>
        private static Vector3 Poly(Vector3[] pts, float s)
        {
            float x = Mathf.Clamp01(s) * (pts.Length - 1);
            int i = Mathf.Clamp((int)x, 0, pts.Length - 2);
            return Vector3.Lerp(pts[i], pts[i + 1], x - i);
        }

        /// <summary>
        /// Glowing veins on both faces of a membrane panel, floating a hair above each face. Finger panels: P(u, v) with u
        /// out from the hand, so veins radiate from the hand toward the scalloped edge. The big arm panel (fromBone):
        /// P(s, v) with v out from the arm, so veins run from the bony edge across to the trailing edge at a few points
        /// along the wing, curving gently toward the tip like real wing veins.
        /// </summary>
        private void Veins(Func<float, float, Vector3> P, Func<float, float, BoneWeight> W, Vector3 up, float th, float len, int count, Vector2 glow, bool fromBone = false)
        {
            if (_detail == 0) return;
            int n = Segs(10, 6, 0);
            float width = 0.0085f * len;
            for (int k = 0; k < count; k++)
            {
                float vc = (k + 1f) / (count + 1f);
                float bend = Rand(-0.04f, 0.04f);
                for (int face = 0; face < 2; face++)
                {
                    float sg = face == 0 ? 1f : -1f;
                    int prevA = -1, prevB = -1;
                    for (int i = 0; i <= n; i++)
                    {
                        float a = Mathf.Lerp(0.06f, 0.9f, (float)i / n);
                        // (x, y): the panel coordinates of this point of the vein.
                        float x, y;
                        if (fromBone) { x = Mathf.Clamp01(vc * 0.9f + 0.05f - 0.12f * a * a + bend * a); y = a; }
                        else { x = a; y = Mathf.Clamp01(vc + bend * a); }
                        Vector3 p = P(x, y);
                        Vector3 along = fromBone ? P(x, Mathf.Min(1f, y + 0.02f)) - P(x, Mathf.Max(0f, y - 0.02f)) : P(Mathf.Min(1f, x + 0.02f), y) - P(Mathf.Max(0f, x - 0.02f), y);
                        Vector3 across = fromBone ? P(Mathf.Min(1f, x + 0.02f), y) - P(Mathf.Max(0f, x - 0.02f), y) : P(x, Mathf.Min(1f, y + 0.02f)) - P(x, Mathf.Max(0f, y - 0.02f));
                        Vector3 nrm = Vector3.Cross(along, across);
                        if (nrm.sqrMagnitude < 1e-14f) nrm = up;
                        nrm = Nrm(nrm);
                        if (Vector3.Dot(nrm, up) < 0f) nrm = -nrm;
                        nrm *= sg;
                        Vector3 side = Vector3.Cross(nrm, along);
                        if (side.sqrMagnitude < 1e-14f) continue;
                        side = Nrm(side) * (width * 0.5f * Mathf.Lerp(1f, 0.35f, (float)i / n));
                        Vector3 q = p + nrm * (th * 0.5f + 0.0015f * len);
                        BoneWeight bw = W(x, y);
                        Vector4 tan = Tan(along, nrm, sg);
                        int ia = AddVertex(q - side, nrm, glow, Vector2.zero, bw, tan);
                        int ib = AddVertex(q + side, nrm, glow, Vector2.zero, bw, tan);
                        if (prevA >= 0) Quad(prevA, prevB, ib, ia);
                        prevA = ia;
                        prevB = ib;
                    }
                }
            }
        }

        // ------------------------------------------------------------------ feathered wings

        private void FeatherWing(WingPlan w)
        {
            float len = w.Len;
            float sx = w.Side;
            Vector2 hide = Pal(_k.Hide), fea = Pal(_k.Membrane), tip = Pal(_k.Accent), glow = Pal(_k.Glow);
            int arm = w.Arm, fore = w.Fore, hand = w.Hand, chest = _chest;
            // Fleshy leading edge from the shoulder to the end of the hand, feathered over.
            Vector3 inner = w.S - new Vector3(sx * 0.1f * len, 0.02f * len, 0.02f * len);
            Vector3[] ac = { inner, w.S, w.E, w.Wr, w.Hd };
            float r0 = 0.065f * len;
            CreatureTube at = new CreatureTube(ac, Vector3.up, new float[] { 0f, 0.1f, 0.1f, 0.1f, 2f });
            float tS = at.CtrlT(1), tE = at.CtrlT(2), tW = at.CtrlT(3);
            at.SetKeys(new[]
            {
                0f, r0 * 1.05f, r0 * 0.9f, r0 * 0.9f, 2.1f,
                tS, r0 * 1.05f, r0 * 0.8f, r0 * 0.85f, 2.2f,
                Mathf.Lerp(tS, tE, 0.5f), r0 * 0.9f, r0 * 0.55f, r0 * 0.6f, 2.3f,
                tE, r0 * 0.75f, r0 * 0.45f, r0 * 0.5f, 2.3f,
                tW, r0 * 0.5f, r0 * 0.32f, r0 * 0.34f, 2.2f,
                1f, r0 * 0.25f, r0 * 0.18f, r0 * 0.18f, 2f
            });
            at.Weight = (t, a) =>
            {
                if (t < tS) return W2(chest, arm, SS(0f, tS, t));
                if (t < tE) return W2(arm, fore, SS(tE - 0.08f, tE + 0.03f, t));
                if (t < tW) return W2(fore, hand, SS(tW - 0.05f, tW + 0.02f, t));
                return W1(hand);
            };
            EmitTube(at, RingsEased(0f, 1f, Segs(14, 10, 6), 0.2f), Segs(12, 9, 6), (t, a) => hide, null, true, true);
            Vector3 back = Vector3.back;
            Vector3 up = Vector3.up;
            Vector3 outward = new Vector3(sx, 0f, 0f);
            // A base web under the feathers so no light shows between them.
            Vector3[] lead = { w.S, w.E, w.Wr, w.Hd };
            Func<float, float, Vector3> PB = (s, v) =>
            {
                Vector3 l = Poly(lead, s);
                float chord = Mathf.Lerp(0.28f, 0.2f, s) * len;
                return l + Vector3.Lerp(back, Nrm(back + outward * 0.4f), s) * (chord * v) + Vector3.down * (0.01f * len * v);
            };
            int[] fg = w.Fingers;
            // Primaries ride the fan bones by their angle, so closing the fan stacks them along the hand.
            Func<float, BoneWeight> FanW = a =>
            {
                if (a >= FeatherFan[1]) return W2(fg[0], fg[1], Mathf.InverseLerp(FeatherFan[0], FeatherFan[1], a));
                return W2(fg[1], fg[2], Mathf.InverseLerp(FeatherFan[1], FeatherFan[2], a));
            };
            // The inner wing (over the arm) is partly carried by the chest, so it lies back along the body when folded.
            Func<float, BoneWeight, BoneWeight> Inner = (k, bw) => Blend(W1(chest), bw, k);
            Func<float, float, BoneWeight> WB = (s, v) =>
            {
                if (s < 0.33f) return Inner(Mathf.Lerp(0.15f, 1f, s / 0.33f), W2(arm, fore, SS(0.25f, 0.33f, s)));
                if (s < 0.66f) return W2(fore, hand, SS(0.6f, 0.68f, s));
                return Blend(W1(hand), W1(fg[2]), v * SS(0.66f, 0.8f, s));
            };
            EmitSheet(Segs(8, 6, 4), 2, PB, WB, up, 0.006f * len, fea, fea);
            int nP = Segs(8, 7, 5), nS = Segs(8, 6, 4);
            int fnu = Segs(3, 3, 2), fnv = Segs(2, 1, 1);
            float fw = 0.09f * len;
            // Primaries: from the hand, fanning from the wing tip back to the trailing edge.
            for (int i = 0; i < nP; i++)
            {
                float u = (float)i / (nP - 1);
                Vector3 root = Vector3.Lerp(w.Hd, w.Wr, u * 0.85f) + up * (0.004f * len * i);
                float ang = Mathf.Lerp(-8f, 72f, u);
                Vector3 dir = Nrm(QMath.AY(sx > 0 ? ang : -ang) * outward + back * 0.05f);
                float fl = Mathf.Lerp(0.42f, 0.36f, u) * len * (1f - 0.08f * Mathf.Abs(u - 0.3f));
                EmitFeather(root, dir, up, fl, fw * Mathf.Lerp(0.85f, 1.05f, u), 0.06f, fea, tip, 0.78f, FanW(-ang), fnu, fnv);
            }
            // Secondaries along the forearm, pointing back, overlapping like shingles.
            for (int i = 0; i < nS; i++)
            {
                float u = (i + 0.5f) / nS;
                Vector3 root = Vector3.Lerp(w.Wr, w.E, u) + up * (0.003f * len * (nS - i));
                Vector3 dir = Nrm(back + outward * Mathf.Lerp(0.35f, 0.1f, u));
                float fl = Mathf.Lerp(0.34f, 0.3f, u) * len;
                BoneWeight bw = u < 0.15f ? W2(fore, hand, 0.4f) : W1(fore);
                EmitFeather(root, dir, up, fl, fw * 1.05f, 0.04f, fea, tip, 0.8f, bw, fnu, fnv);
            }
            // Tertials near the elbow and along the arm.
            for (int i = 0; i < 3; i++)
            {
                float u = (i + 0.5f) / 3f;
                Vector3 root = Vector3.Lerp(w.E, w.S, u * 0.8f) + up * (0.01f * len);
                Vector3 dir = Nrm(back - outward * 0.12f * u);
                EmitFeather(root, dir, up, (0.22f - 0.05f * u) * len, fw * 1.1f, 0.03f, fea, fea, 2f, Inner(Mathf.Lerp(0.35f, 0.15f, u), u < 0.5f ? W2(fore, arm, 0.5f) : W1(arm)), fnu, fnv);
            }
            // Coverts: a row of short feathers over the bases of the flight feathers.
            int nC = Segs(8, 4, 0);
            for (int i = 0; i < nC; i++)
            {
                float u = (i + 0.5f) / nC;
                Vector3 root = Poly(lead, 1f - u) + back * (0.03f * len) + up * (0.02f * len);
                Vector3 dir = Nrm(back + outward * 0.25f * (1f - u));
                BoneWeight bw = u < 0.33f ? Blend(W1(hand), W1(fg[2]), 0.8f) : (u < 0.66f ? W1(fore) : Inner(0.4f, W2(fore, arm, 0.5f)));
                EmitFeather(root, dir, up, 0.13f * len, fw * 0.95f, 0.02f, hide, hide, 2f, bw, 2, 1);
            }
            // A thread of light down the shafts of the outer primaries (a dim inner light in the H8S look).
            if (_detail > 0)
            {
                glow = SeamUV;
                for (int i = 0; i < Mathf.Min(4, nP); i++)
                {
                    float u = (float)i / (nP - 1);
                    Vector3 root = Vector3.Lerp(w.Hd, w.Wr, u * 0.85f) + up * (0.004f * len * i);
                    float ang = Mathf.Lerp(-8f, 72f, u);
                    Vector3 dir = Nrm(QMath.AY(sx > 0 ? ang : -ang) * outward + back * 0.05f);
                    float fl = Mathf.Lerp(0.42f, 0.36f, u) * len * (1f - 0.08f * Mathf.Abs(u - 0.3f));
                    Vector3 a0 = root + dir * (0.2f * fl), a1 = root + dir * (0.92f * fl);
                    Vector3 across = Nrm(Vector3.Cross(up, dir)) * (0.004f * len);
                    for (int face = 0; face < 2; face++)
                    {
                        float sg = face == 0 ? 1f : -1f;
                        Vector3 n = up * sg;
                        Vector3 lift = n * (0.006f * len) + up * (fw * 0.06f + 0.06f * fl * 0.5f * 0.5f);
                        Vector4 tan = Tan(dir, n, sg);
                        BoneWeight fw1 = FanW(-ang);
                        int i0 = AddVertex(a0 - across + lift, n, glow, Vector2.zero, fw1, tan);
                        int i1 = AddVertex(a0 + across + lift, n, glow, Vector2.zero, fw1, tan);
                        int i2 = AddVertex(a1 + across * 0.3f + lift + up * (0.06f * fl * 0.25f), n, glow, Vector2.zero, fw1, tan);
                        int i3 = AddVertex(a1 - across * 0.3f + lift + up * (0.06f * fl * 0.25f), n, glow, Vector2.zero, fw1, tan);
                        Quad(i0, i1, i2, i3);
                    }
                }
            }
        }

        // ------------------------------------------------------------------ griffin plumage

        /// <summary>Feathered ruff down the neck and over the chest, and the sun griffin's crest.</summary>
        private void GriffinPlumage()
        {
            if (_detail == 0) return;
            Vector2 fea = Pal(_k.Hide), tip = Pal(_k.Hide), glow = Pal(_k.Glow);
            CreatureTube nt = _neckTube;
            int rows = Segs(3, 2, 0), per = Segs(9, 6, 0);
            float H = _H;
            for (int r = 0; r < rows; r++)
            {
                float t = Mathf.Lerp(0.85f, 0.25f, (float)r / Mathf.Max(1, rows - 1));
                for (int i = 0; i < per; i++)
                {
                    float th = (i + (r % 2) * 0.5f) / per * 360f;
                    Vector3 p, n;
                    nt.Frame(t, th, out p, out n);
                    Vector3 along = Nrm(nt.Center(t - 0.05f) - nt.Center(t + 0.05f));
                    Vector3 dir = Nrm(along + n * 0.25f + Vector3.down * 0.25f);
                    EmitFeather(p - n * (0.01f * H), dir, n, 0.2f * H * Rand(0.85f, 1.15f), 0.075f * H, 0.1f, fea, tip, 2f, nt.WeightAt(t, Wrap360(th)), 2, 1);
                }
            }
            // Chest: a shield of long feathers hanging over the breast.
            int nT = _bodyTrunkCount;
            for (int i = 0; i < Segs(9, 6, 0); i++)
            {
                float th = Mathf.Lerp(210f, 330f, (i + 0.5f) / 9f);
                float t = _bodyT[nT - 1] - 0.02f - 0.02f * (i % 2);
                Vector3 p, n;
                _bodyTube.Frame(t, th, out p, out n);
                EmitFeather(p - n * (0.01f * H), Nrm(Vector3.down + Vector3.back * 0.4f + n * 0.25f), n, 0.24f * H, 0.09f * H, 0.08f, fea, tip, 2f, _bodyTube.WeightAt(t, Wrap360(th)), 2, 1);
            }
            if (_k.Crest)
            {
                // Sun griffin: a fan of long crest feathers swept back from the crown, their tips lit.
                for (int i = 0; i < 5; i++)
                {
                    float u = i / 4f;
                    Vector3 p, n;
                    _headTube.Frame(Mathf.Lerp(0.18f, 0.5f, u), 90f, out p, out n);
                    Vector3 dir = Nrm(-_headFwd + _headUp * Mathf.Lerp(0.9f, 0.35f, u));
                    EmitFeather(p - n * (0.01f * _headLen), dir, Vector3.right, (0.55f - 0.25f * u) * _headLen, 0.09f * _headLen, -0.05f, Pal(_k.Hide), SeamUV, 0.82f, W1(_head), 3, 1);
                }
            }
        }
    }
}
