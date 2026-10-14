using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Four-legged bodies (Neon Jackal, Frost Stalker, Crystal Basilisk) and the shared trunk, neck, head, legs and tail
    /// of the winged plan. Skeleton: Root at the middle of the trunk, Pelvis behind it (hind legs and tail), Spine and
    /// Chest in front (forelegs through a scapula bone, the neck chain, Head, Jaw and the ears). Legs are digitigrade:
    /// upper, lower, foot (metatarsus or metacarpus) and toe bones, standing in a natural bent stance at bind.
    /// </summary>
    internal sealed partial class CreatureSculptor
    {
        /// <summary>
        /// Canonical proportions per body style, in units of the shoulder height H (measured on real animals and then
        /// stylised): trunk length from the buttock to the point of the shoulder, chest half width, joint heights, where
        /// the hip joint sits, neck and head carriage and head length.
        /// </summary>
        private struct Style
        {
            public float Trunk, BodyWidth, ShoulderY, HipY, HipZ, NeckPitch, HeadPitch, HeadLen, CanonLH;
            public int NeckBones;
        }

        private static Style StyleOf(int headStyle)
        {
            switch (headStyle)
            {
                case 1: return new Style { Trunk = 1.36f, BodyWidth = 0.2f, ShoulderY = 0.6f, HipY = 0.68f, HipZ = 0.22f, NeckPitch = 22f, HeadPitch = 10f, HeadLen = 0.34f, CanonLH = 2.45f, NeckBones = 2 };
                case 2: return new Style { Trunk = 2.0f, BodyWidth = 0.34f, ShoulderY = 0.52f, HipY = 0.52f, HipZ = 0.3f, NeckPitch = 9f, HeadPitch = 4f, HeadLen = 0.7f, CanonLH = 3.4f, NeckBones = 2 };
                case 3: return new Style { Trunk = 1.58f, BodyWidth = 0.2f, ShoulderY = 0.6f, HipY = 0.7f, HipZ = 0.24f, NeckPitch = 46f, HeadPitch = 10f, HeadLen = 0.46f, CanonLH = 2.6f, NeckBones = 4 };
                case 4: return new Style { Trunk = 1.38f, BodyWidth = 0.17f, ShoulderY = 0.6f, HipY = 0.7f, HipZ = 0.22f, NeckPitch = 54f, HeadPitch = 16f, HeadLen = 0.4f, CanonLH = 2.15f, NeckBones = 3 };
                default: return new Style { Trunk = 1.25f, BodyWidth = 0.125f, ShoulderY = 0.62f, HipY = 0.72f, HipZ = 0.2f, NeckPitch = 38f, HeadPitch = 14f, HeadLen = 0.42f, CanonLH = 2.0f, NeckBones = 2 };
            }
        }

        /// <summary>One leg as planned at bind (model space).</summary>
        private struct LegPlan
        {
            public bool Front;
            public int Side;
            public Vector3 Girdle, Hip, Knee, Ankle, Paw, ToeTip;
            public float Sum;
            public int GirdleBone, Upper, Lower, Foot, Toe;
        }

        // ------------------------------------------------------------------ layout
        private Style _st;
        private float _L, _H, _W, _trunk, _zB, _zS, _headLen, _hw, _neckLen;
        private Vector3 _neckBase, _headBase, _headFwd, _headUp, _occiput, _nose, _jawHinge, _tailBase;
        private Vector3[] _neckPts, _tailPts;
        private int _root, _pelvis, _spine, _chest, _head, _jaw, _earL = -1, _earR = -1;
        private int[] _neckBones = new int[0], _tailBones = new int[0];
        private readonly List<LegPlan> _legs = new List<LegPlan>();
        private CreatureTube _bodyTube, _headTube, _jawTube, _tailTube;
        /// <summary>Body tube t at each control point: trunk landmarks (TrunkU), then the neck points.</summary>
        private float[] _bodyT;
        private int _bodyTrunkCount;
        private bool _hasForelegs = true;

        // Trunk profile per body style at landmarks u (0 buttock .. 1 point of the shoulder, beyond 1 the chest front):
        // back height and belly height (x H), half width (x chest half width W).
        private static readonly float[] TrunkU = { -0.04f, 0.03f, 0.13f, 0.3f, 0.45f, 0.62f, 0.78f, 0.88f, 1f, 1.07f };
        private static readonly float[][] TrunkBack =
        {
            new[] { 0.82f, 0.92f, 0.965f, 0.95f, 0.945f, 0.965f, 0.99f, 1f, 0.95f, 0.86f },
            new[] { 0.8f, 0.9f, 0.94f, 0.925f, 0.92f, 0.94f, 0.98f, 1f, 0.95f, 0.86f },
            new[] { 0.86f, 0.95f, 0.99f, 1f, 1f, 1f, 0.99f, 0.97f, 0.92f, 0.86f },
            new[] { 0.82f, 0.92f, 0.95f, 0.935f, 0.93f, 0.95f, 0.985f, 1f, 0.95f, 0.86f },
            new[] { 0.8f, 0.9f, 0.94f, 0.925f, 0.92f, 0.94f, 0.98f, 1f, 0.95f, 0.86f }
        };
        private static readonly float[][] TrunkBelly =
        {
            new[] { 0.66f, 0.54f, 0.48f, 0.56f, 0.63f, 0.55f, 0.485f, 0.49f, 0.53f, 0.6f },
            new[] { 0.62f, 0.5f, 0.44f, 0.5f, 0.53f, 0.48f, 0.45f, 0.455f, 0.5f, 0.58f },
            new[] { 0.62f, 0.48f, 0.42f, 0.38f, 0.36f, 0.37f, 0.4f, 0.44f, 0.5f, 0.56f },
            new[] { 0.64f, 0.5f, 0.44f, 0.5f, 0.52f, 0.46f, 0.42f, 0.43f, 0.48f, 0.56f },
            new[] { 0.62f, 0.5f, 0.44f, 0.5f, 0.53f, 0.47f, 0.44f, 0.45f, 0.5f, 0.58f }
        };
        private static readonly float[][] TrunkWidth =
        {
            new[] { 0.32f, 0.78f, 1f, 0.86f, 0.74f, 0.92f, 1.02f, 1f, 0.88f, 0.74f },
            new[] { 0.34f, 0.8f, 1f, 0.92f, 0.86f, 0.96f, 1.02f, 1f, 0.9f, 0.76f },
            new[] { 0.42f, 0.8f, 0.96f, 1f, 1.03f, 1.02f, 0.96f, 0.9f, 0.78f, 0.66f },
            new[] { 0.36f, 0.8f, 1f, 0.9f, 0.86f, 0.96f, 1.04f, 1f, 0.88f, 0.74f },
            new[] { 0.34f, 0.8f, 1f, 0.92f, 0.86f, 0.96f, 1.02f, 1f, 0.9f, 0.76f }
        };
        private static readonly float[] TrunkExp = { 2f, 2.1f, 2.2f, 2.2f, 2.2f, 2.3f, 2.3f, 2.3f, 2.2f, 2.1f };

        private int StyleRow { get { return Mathf.Clamp(_k.HeadStyle, 0, 4); } }

        private void QuadLayout()
        {
            _st = StyleOf(_k.HeadStyle);
            float s = _s;
            _L = Mathf.Max(0.2f, _k.BodyLength) * s;
            _H = Mathf.Max(0.1f, _k.ShoulderHeight) * s;
            _W = _st.BodyWidth * _H * Mathf.Pow(Mathf.Max(0.4f, _k.Bulk), 0.75f);
            // The trunk follows the canonical build, stretched a little toward the look's nose-to-rump length.
            float stretch = Mathf.Clamp(_L / _H / _st.CanonLH, 0.85f, 1.2f);
            _trunk = _st.Trunk * _H * stretch;
            _zB = -_trunk * 0.5f;
            _zS = _trunk * 0.5f;
            _headLen = _st.HeadLen * _H * Mathf.Max(0.5f, _k.HeadSize);
            _hw = 0.2f * _headLen;
            _neckLen = Mathf.Max(0.05f, _k.NeckLength) * s;
            float np = _st.NeckPitch * Mathf.Deg2Rad, hp = _st.HeadPitch * Mathf.Deg2Rad;
            _neckBase = new Vector3(0f, 0.8f * _H, _zS - 0.05f * _H);
            // The neck rises in a gentle S: steeper at the base, levelling toward the head.
            Vector3 dirA = new Vector3(0f, Mathf.Sin(np + 0.2f), Mathf.Cos(np + 0.2f));
            Vector3 dirB = new Vector3(0f, Mathf.Sin(np - 0.25f), Mathf.Cos(np - 0.25f));
            int nn = _st.NeckBones;
            _neckPts = new Vector3[nn + 1];
            Vector3 p = _neckBase;
            for (int i = 0; i <= nn; i++)
            {
                _neckPts[i] = p;
                float u = (i + 0.5f) / nn;
                p += Nrm(Vector3.Lerp(dirA, dirB, u)) * (_neckLen / nn);
            }
            _headBase = _neckPts[nn];
            _headFwd = new Vector3(0f, -Mathf.Sin(hp), Mathf.Cos(hp));
            _headUp = new Vector3(0f, Mathf.Cos(hp), Mathf.Sin(hp));
            // The head axis runs from the back of the skull, a little above and behind the atlas joint, to the nose.
            _occiput = _headBase + _headUp * (0.1f * _headLen) - _headFwd * (0.05f * _headLen);
            _nose = _occiput + _headFwd * _headLen;
            _jawHinge = _occiput + _headFwd * (0.27f * _headLen) - _headUp * (0.15f * _headLen);
            // Tail: from the top of the rump, back and down, curving.
            float tailLen = Mathf.Max(0.05f, _k.TailLength) * s;
            int ts = Mathf.Clamp(_k.TailSegments, 2, 12);
            float croup = TrunkBack[StyleRow][2] * _H;
            _tailBase = new Vector3(0f, croup - 0.05f * _H, _zB + 0.05f * _H);
            if (_k.HeadStyle == 2 || _k.HeadStyle == 3) _tailBase = new Vector3(0f, (TrunkBack[StyleRow][1] + TrunkBelly[StyleRow][1]) * 0.5f * _H + 0.04f * _H, _zB - 0.01f * _H);
            _tailPts = new Vector3[ts + 1];
            float droop0, droop1;
            switch (_k.HeadStyle)
            {
                case 1: droop0 = 62f; droop1 = -8f; break;    // cat: hangs, then curls out and up
                case 2: droop0 = 12f; droop1 = 4f; break;     // lizard: low, just clear of the ground
                case 3: droop0 = 14f; droop1 = -4f; break;    // dragon: held out, the tip lifting
                case 4: droop0 = 50f; droop1 = 10f; break;    // griffin: lion tail
                default: droop0 = 72f; droop1 = 52f; break;   // jackal brush: hangs low toward the hocks
            }
            p = _tailBase;
            for (int i = 0; i <= ts; i++)
            {
                _tailPts[i] = p;
                float a = Mathf.Lerp(droop0, droop1, (i + 0.5f) / ts) * Mathf.Deg2Rad;
                p += new Vector3(0f, -Mathf.Sin(a), -Mathf.Cos(a)) * (tailLen / ts);
                if (p.y < 0.1f * _H) p.y = 0.1f * _H;
            }
            PlanLegs();
        }

        private void PlanLegs()
        {
            _legs.Clear();
            float sprawl = Mathf.Clamp01(_k.Sprawl);
            float legK = Mathf.Max(0.5f, _k.LegLength);
            int hs = _k.HeadStyle;
            for (int f = 0; f < 2; f++)
            {
                bool front = f == 0;
                if (front && !_hasForelegs) continue;
                for (int side = -1; side <= 1; side += 2)
                {
                    LegPlan lp = new LegPlan();
                    lp.Front = front;
                    lp.Side = side;
                    float sx = side;
                    Vector3 joint;
                    float r1, r2, r3, a1, a2, a3;
                    Vector3 d1, d2, d3;
                    if (front)
                    {
                        joint = new Vector3(sx * Mathf.Lerp(0.6f, 0.92f, sprawl) * _W, _st.ShoulderY * _H, _zS - Mathf.Lerp(0.07f, 0.12f, sprawl) * _H);
                        // Humerus slopes back to the elbow under the chest, the forearm drops nearly straight, the
                        // pastern leans forward to the paw.
                        r1 = hs == 1 ? 0.27f : 0.25f; r2 = hs == 1 ? 0.35f : 0.38f; r3 = 0.08f;
                        a1 = hs == 1 ? -44f : -48f; a2 = 4f; a3 = 16f;
                        d1 = Dir(a1, 0f); d2 = Dir(a2, 0f); d3 = Dir(a3, 0f);
                        if (sprawl > 0f)
                        {
                            Vector3 s1 = Nrm(new Vector3(sx * 0.86f, -0.32f, -0.38f)), s2 = Nrm(new Vector3(sx * 0.08f, -0.99f, 0.1f)), s3 = Nrm(new Vector3(sx * 0.2f, -0.94f, 0.26f));
                            d1 = Nrm(Vector3.Lerp(d1, s1, sprawl)); d2 = Nrm(Vector3.Lerp(d2, s2, sprawl)); d3 = Nrm(Vector3.Lerp(d3, s3, sprawl));
                            r1 = Mathf.Lerp(r1, 0.42f, sprawl); r2 = Mathf.Lerp(r2, 0.36f, sprawl); r3 = Mathf.Lerp(r3, 0.1f, sprawl);
                        }
                    }
                    else
                    {
                        joint = new Vector3(sx * Mathf.Lerp(0.7f, 0.88f, sprawl) * _W, _st.HipY * _H, _zB + _st.HipZ * _H);
                        r1 = 0.33f; r2 = 0.35f; r3 = hs == 1 ? 0.19f : 0.22f;
                        a1 = hs == 1 ? 30f : 35f; a2 = hs == 1 ? -46f : -50f; a3 = 8f;
                        d1 = Dir(a1, 0f); d2 = Dir(a2, 0f); d3 = Dir(a3, 0f);
                        if (sprawl > 0f)
                        {
                            Vector3 s1 = Nrm(new Vector3(sx * 0.88f, -0.24f, 0.4f)), s2 = Nrm(new Vector3(sx * 0.1f, -0.97f, -0.22f)), s3 = Nrm(new Vector3(sx * 0.18f, -0.9f, 0.4f));
                            d1 = Nrm(Vector3.Lerp(d1, s1, sprawl)); d2 = Nrm(Vector3.Lerp(d2, s2, sprawl)); d3 = Nrm(Vector3.Lerp(d3, s3, sprawl));
                            r1 = Mathf.Lerp(r1, 0.46f, sprawl); r2 = Mathf.Lerp(r2, 0.38f, sprawl); r3 = Mathf.Lerp(r3, 0.12f, sprawl);
                        }
                    }
                    float pad = 0.03f * _H;
                    float vert = -(r1 * d1.y + r2 * d2.y + r3 * d3.y);
                    float scale = (joint.y - pad) / Mathf.Max(0.05f, vert);
                    // A longer-legged look raises the body instead: the bones get longer and bend a little more.
                    float sum = (r1 + r2 + r3) * scale;
                    lp.Sum = sum * Mathf.Lerp(1f, legK, 0.25f);
                    lp.Hip = joint;
                    lp.Knee = joint + d1 * (r1 * scale);
                    lp.Ankle = lp.Knee + d2 * (r2 * scale);
                    lp.Paw = lp.Ankle + d3 * (r3 * scale);
                    lp.Paw.y = pad;
                    float pawLen = (sprawl > 0.5f ? 0.2f : (hs == 1 ? 0.13f : 0.11f)) * _H;
                    Vector3 toeDir = Nrm(new Vector3(sx * 0.22f * sprawl, 0f, 1f));
                    lp.ToeTip = lp.Paw + toeDir * pawLen + Vector3.down * (pad * 0.6f);
                    lp.Girdle = front ? new Vector3(sx * 0.3f * _W, 0.9f * _H, _zS - 0.2f * _H) : joint + new Vector3(-sx * 0.25f * _W, 0.14f * _H, 0.03f * _H);
                    if (front && sprawl > 0.5f) lp.Girdle = new Vector3(sx * 0.6f * _W, 0.82f * _H, _zS - 0.2f * _H);
                    _legs.Add(lp);
                }
            }
        }

        /// <summary>A unit direction pointing down, tilted forward by fwdDeg (negative: backward) and outward by outDeg.</summary>
        private static Vector3 Dir(float fwdDeg, float outDeg)
        {
            float a = fwdDeg * Mathf.Deg2Rad, b = outDeg * Mathf.Deg2Rad;
            return Nrm(new Vector3(Mathf.Sin(b), -Mathf.Cos(a) * Mathf.Cos(b), Mathf.Sin(a)));
        }

        // ------------------------------------------------------------------ skeleton

        private void QuadSkeleton()
        {
            float shoulderZ = _zS - 0.1f * _H, hipZ = _zB + _st.HipZ * _H;
            _root = Bone("Root", -1, new Vector3(0f, 0.74f * _H, (shoulderZ + hipZ) * 0.5f));
            _sk.Root = _root;
            _pelvis = Bone("Pelvis", _root, new Vector3(0f, _st.HipY * _H + 0.1f * _H, hipZ - 0.02f * _H));
            _spine = Bone("Spine", _root, new Vector3(0f, 0.76f * _H, Mathf.Lerp(_bm[_root].z, shoulderZ, 0.5f)));
            _chest = Bone("Chest", _spine, new Vector3(0f, _st.ShoulderY * _H + 0.16f * _H, shoulderZ));
            _sk.Pelvis = _pelvis;
            _sk.Chest = _chest;
            _sk.Spine = new[] { _root, _spine };
            int nn = _neckPts.Length - 1;
            _neckBones = new int[nn];
            int parent = _chest;
            for (int i = 0; i < nn; i++)
            {
                _neckBones[i] = Bone("Neck" + (i + 1), parent, _neckPts[i]);
                parent = _neckBones[i];
            }
            _sk.Neck = _neckBones;
            _head = Bone("Head", parent, _headBase);
            _sk.Head = _head;
            _jaw = Bone("Jaw", _head, _jawHinge);
            _sk.Jaw = _jaw;
            if (_k.EarStyle > 0)
            {
                float tEar = _k.EarStyle == 1 ? 0.15f : (_k.EarStyle == 3 ? 0.08f : 0.2f);
                // Ears sit on the skull's surface: higher on the feline's round cranium.
                Vector3 ec = _occiput + _headFwd * (tEar * _headLen) + _headUp * ((_k.HeadStyle == 1 ? 0.27f : 0.17f) * _headLen);
                float ex = (_k.EarStyle == 1 ? 0.12f : 0.15f) * _headLen;
                _earL = Bone("Ear.L", _head, ec + Vector3.left * ex);
                _earR = Bone("Ear.R", _head, ec + Vector3.right * ex);
            }
            int ts = _tailPts.Length - 1;
            _tailBones = new int[ts];
            parent = _pelvis;
            for (int i = 0; i < ts; i++)
            {
                _tailBones[i] = Bone("Tail" + (i + 1), parent, _tailPts[i]);
                parent = _tailBones[i];
            }
            _sk.Tail = _tailBones;
            List<LegChain> chains = new List<LegChain>();
            for (int i = 0; i < _legs.Count; i++)
            {
                LegPlan lp = _legs[i];
                string sd = lp.Side < 0 ? ".L" : ".R";
                string pre = lp.Front ? "Fore" : "Hind";
                int top = lp.Front ? _chest : _pelvis;
                lp.GirdleBone = -1;
                if (lp.Front)
                {
                    lp.GirdleBone = Bone("Scapula" + sd, _chest, lp.Girdle);
                    top = lp.GirdleBone;
                }
                lp.Upper = Bone(pre + "Upper" + sd, top, lp.Hip);
                lp.Lower = Bone(pre + "Lower" + sd, lp.Upper, lp.Knee);
                lp.Foot = Bone(pre + "Foot" + sd, lp.Lower, lp.Ankle);
                lp.Toe = Bone(pre + "Toe" + sd, lp.Foot, lp.Paw);
                _legs[i] = lp;
                LegChain c = new LegChain();
                c.Upper = lp.Upper; c.Lower = lp.Lower; c.Foot = lp.Foot; c.Toe = lp.Toe;
                c.Front = lp.Front;
                c.Side = lp.Side;
                c.Length = lp.Hip.y;
                chains.Add(c);
            }
            _sk.Legs = chains.ToArray();
        }

        // ================================================================== quadruped

        private void BuildQuadruped()
        {
            QuadLayout();
            QuadSkeleton();
            TileForCoat();
            BeginCore();
            QuadBody();
            QuadHead();
            for (int i = 0; i < _legs.Count; i++) QuadLeg(_legs[i]);
            EndCore();
            QuadTail();
            QuadDetails();
            QuadHits();
            QuadMetrics();
        }

        private void QuadMetrics()
        {
            // A heavy tail grown out of the trunk does not make the body wider for spacing and melee reach.
            if (MergedTail) _radiusMinZ = _zB - 0.05f * _H;
            _aim = Vector3.Lerp(_bm[_chest], _bm[_root], 0.3f);
            _headPoint = _occiput + _headFwd * (_headLen * 0.35f) + _headUp * (_headLen * 0.04f);
            if (_mouthPoint == Vector3.zero) _mouthPoint = _nose - _headUp * (_headLen * 0.09f) - _headFwd * (_headLen * 0.04f);
            // Death sprawls the legs out sideways, pounces and lunges carry the body forward, rearing lifts it.
            float reach = Mathf.Max(_trunk * 0.5f, _H * 0.7f);
            float leg = Mathf.Max(0.05f, _H * _k.LegLength * 0.8f);
            // A long tail can swing up over the back when the body leaps.
            float tail = Mathf.Max(0.05f, _k.TailLength * _s);
            _poseGrow = new Vector3(reach * 1.5f + leg * 0.3f, Mathf.Max(reach * 1.2f, tail * 0.9f), reach * 2f + leg * 0.6f);
        }

        private void TileForCoat()
        {
            float size = Mathf.Sqrt(Mathf.Max(0.05f, _H));
            switch (_k.Coat)
            {
                case 1: _tile = 0.11f * size; break;
                case 2: _tile = 0.06f * size; break;
                case 3: _tile = 0.25f * size; break;
                default: _tile = 0.05f * size; break;
            }
        }

        // ------------------------------------------------------------------ body: trunk (with the tail for reptiles and dragons) and neck

        /// <summary>Heavy tails (lizards, dragons) grow out of the trunk as one surface; furred tails are their own piece.</summary>
        private bool MergedTail { get { return _k.HeadStyle == 2 || _k.HeadStyle == 3; } }
        private CreatureTube _neckTube;
        /// <summary>Trunk tube t where the merged tail meets the rump (0 when the tail is separate).</summary>
        private float _tailJoinT;

        private void QuadBody()
        {
            int row = StyleRow;
            int nT = TrunkU.Length;
            List<Vector3> ctrl = new List<Vector3>();
            List<float> keys = new List<float>();
            float[] back = TrunkBack[row], belly = TrunkBelly[row], width = TrunkWidth[row];
            bool merged = MergedTail;
            int tailCount = 0;
            if (merged)
            {
                // From the tail tip forward to the base, then on into the trunk (the rump landmark is skipped).
                Vector3 tipDir = Nrm(_tailPts[_tailPts.Length - 1] - _tailPts[_tailPts.Length - 2]);
                ctrl.Add(_tailPts[_tailPts.Length - 1] + tipDir * (Mathf.Max(0.05f, _k.TailLength) * _s / (_tailPts.Length - 1) * 0.5f));
                for (int i = _tailPts.Length - 1; i >= 1; i--) ctrl.Add(_tailPts[i]);
                tailCount = ctrl.Count;
            }
            for (int i = merged ? 1 : 0; i < nT; i++)
            {
                float z = _zB + TrunkU[i] * _trunk;
                float cy = (back[i] + belly[i]) * 0.5f * _H;
                ctrl.Add(new Vector3(0f, cy, z));
            }
            // A closing ring rounds off the front of the chest.
            ctrl.Add(new Vector3(0f, (back[nT - 1] * 0.88f + belly[nT - 1] * 1.1f) * 0.5f * _H, _zB + 1.12f * _trunk));
            _bodyTrunkCount = nT;
            CreatureTube tb = new CreatureTube(ctrl.ToArray(), Vector3.up, new float[] { 0f, 0.1f, 0.1f, 0.1f, 2f });
            _bodyT = new float[nT];
            int first = merged ? tailCount - 1 : 0;
            for (int i = 0; i < nT; i++) _bodyT[i] = tb.CtrlT(first + i + (merged ? 0 : 0));
            if (merged) _bodyT[0] = tb.CtrlT(tailCount - 1);
            _tailJoinT = merged ? tb.CtrlT(tailCount - 1) : 0f;
            float tailR = 0f;
            if (merged)
            {
                // Tail sections: from a fine tip to the size of the rump, a little taller than wide.
                float baseA = width[1] * _W * 0.85f, baseB = (back[1] - belly[1]) * 0.5f * _H * 0.82f;
                tailR = baseA;
                int nTail = tailCount;
                for (int i = 0; i < nTail; i++)
                {
                    float u = (float)i / (nTail - 1);
                    float k = i == 0 ? 0.02f : Mathf.Lerp(0.1f, 1f, Mathf.Pow(u, 0.85f));
                    keys.Add(tb.CtrlT(i)); keys.Add(baseA * k * (i == 0 ? 1f : 0.92f)); keys.Add(baseB * k * 1.05f); keys.Add(baseB * k); keys.Add(2.2f);
                }
            }
            for (int i = merged ? 1 : 0; i < nT; i++)
            {
                float half = (back[i] - belly[i]) * 0.5f * _H;
                float a = width[i] * _W;
                keys.Add(_bodyT[i]); keys.Add(a); keys.Add(half * (i == 0 ? 0.7f : 1f)); keys.Add(half * (i == 0 ? 0.8f : 1f)); keys.Add(TrunkExp[i]);
            }
            keys.Add(1f); keys.Add(width[nT - 1] * _W * 0.45f); keys.Add((back[nT - 1] - belly[nT - 1]) * 0.22f * _H); keys.Add((back[nT - 1] - belly[nT - 1]) * 0.3f * _H); keys.Add(2f);
            tb.SetKeys(keys.ToArray());
            int hs = _k.HeadStyle;
            float tHip = _bodyT[2], tWither = _bodyT[7], tFront = _bodyT[nT - 1], tRibs = _bodyT[6], tWaist = _bodyT[4], tJoin = _tailJoinT;
            bool lizard = hs == 2;
            float H = _H, W = _W;
            tb.Bump = (t, th) =>
            {
                float k = 0f;
                // Shoulder blades: two ridges either side of the withers, and the triceps mass behind each shoulder.
                k += 0.045f * H * G(t, tWither, 0.04f) * (GA(th, 56f, 18f) + GA(th, 124f, 18f));
                k += 0.03f * H * G(t, tWither - 0.025f, 0.045f) * (GA(th, 345f, 26f) + GA(th, 195f, 26f));
                // Haunches: rounded muscle over each hip, carried down the outside of the thigh.
                k += 0.045f * H * G(t, tHip + 0.01f, 0.05f) * (GA(th, 25f, 30f) + GA(th, 155f, 30f));
                k += 0.03f * H * G(t, tHip + 0.025f, 0.045f) * (GA(th, 330f, 30f) + GA(th, 210f, 30f));
                // Rib cage swells low on the flanks; the spine leaves a soft groove along the back.
                k += 0.02f * H * G(t, tRibs, 0.05f) * (GA(th, 320f, 26f) + GA(th, 220f, 26f));
                k -= 0.01f * H * GA(th, 90f, 8f) * SS(tHip, tHip + 0.04f, t) * (1f - SS(tWither - 0.02f, tWither + 0.03f, t));
                // Breastbone keel at the front of the chest, and the tucked belly behind the ribs.
                k += 0.025f * H * G(t, tFront - 0.01f, 0.035f) * GA(th, 270f, 26f);
                k -= 0.015f * H * G(t, tWaist, 0.045f) * GA(th, 270f, 40f);
                if (lizard) k += 0.022f * H * G(t, (tHip + tWither) * 0.5f, 0.16f) * (GA(th, 0f, 16f) + GA(th, 180f, 16f));
                // A ridge along the top of a heavy tail.
                if (tJoin > 0f) k += 0.035f * H * GA(th, 90f, 10f) * SS(0.05f, tJoin * 0.5f, t) * (1f - SS(tJoin, tJoin + 0.05f, t));
                return k;
            };
            // Weights: tail bones along a merged tail, Pelvis over the rump and hips, Root mid-trunk, Spine then Chest
            // toward the shoulders; the flanks over the haunches and shoulders follow the thighs and shoulder blades a little.
            List<int> ab = new List<int>();
            List<float> at = new List<float>();
            if (merged)
            {
                for (int i = _tailBones.Length - 1; i >= 0; i--)
                {
                    ab.Add(_tailBones[i]);
                    at.Add(tb.CtrlT(tailCount - 1 - i));
                }
            }
            ab.Add(_pelvis); at.Add(_bodyT[2]);
            ab.Add(_root); at.Add(_bodyT[4]);
            ab.Add(_spine); at.Add(_bodyT[5]);
            ab.Add(_chest); at.Add(_bodyT[7]);
            int[] anchorsB = ab.ToArray();
            float[] anchorsT = at.ToArray();
            int hipL = -1, hipR = -1, scL = -1, scR = -1;
            foreach (LegPlan lp in _legs)
            {
                if (lp.Front) { if (lp.Side < 0) scL = lp.GirdleBone; else scR = lp.GirdleBone; }
                else { if (lp.Side < 0) hipL = lp.Upper; else hipR = lp.Upper; }
            }
            tb.Weight = (t, th) =>
            {
                BoneWeight w = AlongAnchors(anchorsB, anchorsT, t);
                float cs = Mathf.Cos(th * Mathf.Deg2Rad), sn = Mathf.Sin(th * Mathf.Deg2Rad);
                float low = SS(0.35f, -0.6f, sn) * SS(0.1f, 0.65f, Mathf.Abs(cs));
                float haunch = 0.42f * low * G(t, tHip + 0.015f, 0.045f);
                float shoulder = 0.32f * SS(-0.7f, 0.3f, sn) * SS(0.25f, 0.75f, Mathf.Abs(cs)) * G(t, tWither - 0.015f, 0.04f);
                int leg = -1;
                float lw = 0f;
                if (haunch > shoulder && haunch > 0.02f) { leg = cs > 0f ? hipR : hipL; lw = haunch; }
                else if (shoulder > 0.02f) { leg = cs > 0f ? scR : scL; lw = shoulder; }
                if (leg < 0) return w;
                return W3(w.boneIndex0, w.boneIndex1, w.weight1, leg, lw);
            };
            _bodyTube = tb;
            List<float> rt = new List<float>();
            if (merged)
            {
                int tr = Segs(16, 11, 7);
                for (int i = 0; i < tr; i++) rt.Add(tJoin * i / tr);
            }
            int trunkRings = Segs(26, 16, 11);
            float t0 = merged ? tJoin : 0f;
            for (int i = 0; i <= trunkRings; i++)
            {
                float u = (float)i / trunkRings;
                rt.Add(Mathf.Lerp(t0, 1f, u - 0.1f * Mathf.Sin(u * Mathf.PI * 2f) / (Mathf.PI * 2f)));
            }
            Vector2 hide = Pal(_k.Hide), belly2 = Pal(_k.Belly);
            bool lightBelly = _k.Belly != _k.Hide;
            EmitTube(tb, rt.ToArray(), Segs(28, 20, 13), (t, th) =>
            {
                if (!lightBelly) return hide;
                bool under = th > 238f && th < 302f;
                if (under && t > tRibs - 0.03f) return belly2;
                if (lizard && under && t > tJoin * 0.6f) return belly2;
                return hide;
            }, lightBelly ? new[] { 0f, 238f, 302f } : null, !merged, true);
            QuadNeck();
        }

        /// <summary>Linear blend between the two anchor bones around t.</summary>
        private static BoneWeight AlongAnchors(int[] bones, float[] ts, float t)
        {
            if (t <= ts[0]) return W1(bones[0]);
            int n = ts.Length;
            if (t >= ts[n - 1]) return W1(bones[n - 1]);
            int k = 0;
            while (k < n - 2 && t > ts[k + 1]) k++;
            return W2(bones[k], bones[k + 1], SS(ts[k], ts[k + 1], t));
        }

        /// <summary>
        /// The neck rises from deep inside the chest (so the chest front stays round and full) to inside the skull. Deep and
        /// narrow at the root, it slims to just under the size of the skull, the throat a little fuller than the nape.
        /// </summary>
        private void QuadNeck()
        {
            int row = StyleRow;
            int nT = TrunkU.Length;
            float[] back = TrunkBack[row], belly = TrunkBelly[row], width = TrunkWidth[row];
            int nn = _neckPts.Length;
            int hs = _k.HeadStyle;
            List<Vector3> ctrl = new List<Vector3>();
            ctrl.Add(new Vector3(0f, 0.66f * _H, _zS - 0.22f * _H));
            ctrl.Add(new Vector3(0f, 0.72f * _H, _zS - 0.08f * _H));
            for (int i = 0; i < nn; i++) ctrl.Add(_neckPts[i] + (i == 0 ? new Vector3(0f, 0f, 0.02f * _H) : Vector3.zero));
            ctrl.Add(_occiput + _headFwd * (_headLen * 0.2f) - _headUp * (_headLen * 0.05f));
            CreatureTube tb = new CreatureTube(ctrl.ToArray(), Vector3.up, new float[] { 0f, 0.1f, 0.1f, 0.1f, 2f });
            float rootDepth = (back[nT - 2] - belly[nT - 2]) * 0.5f * _H * 0.82f;
            float rootWidth = width[nT - 2] * _W * 0.8f;
            float topDepth = (hs == 3 ? 0.14f : (hs == 1 ? 0.21f : (hs == 2 ? 0.1f : (hs == 4 ? 0.19f : 0.16f)))) * _headLen;
            float topWidth = (hs == 3 ? 0.14f : (hs == 1 ? 0.22f : (hs == 2 ? 0.2f : (hs == 4 ? 0.19f : 0.15f)))) * _headLen;
            List<float> keys = new List<float>();
            keys.Add(0f); keys.Add(rootWidth * 0.7f); keys.Add(rootDepth * 0.7f); keys.Add(rootDepth * 0.7f); keys.Add(2.1f);
            keys.Add(tb.CtrlT(1)); keys.Add(rootWidth); keys.Add(rootDepth * 0.95f); keys.Add(rootDepth * 1.05f); keys.Add(2.2f);
            for (int i = 1; i < nn; i++)
            {
                float u = (float)i / nn;
                float e = Mathf.Pow(u, hs == 1 ? 0.5f : 0.55f);
                keys.Add(tb.CtrlT(2 + i)); keys.Add(Mathf.Lerp(rootWidth, topWidth, e)); keys.Add(Mathf.Lerp(rootDepth, topDepth, e) * 0.9f); keys.Add(Mathf.Lerp(rootDepth, topDepth, e) * 1.1f); keys.Add(2.1f);
            }
            keys.Add(tb.CtrlT(nn + 1)); keys.Add(topWidth * 0.95f); keys.Add(topDepth * 0.82f); keys.Add(topDepth * 1.05f); keys.Add(2.05f);
            keys.Add(1f); keys.Add(topWidth * 0.7f); keys.Add(topDepth * 0.6f); keys.Add(topDepth * 0.75f); keys.Add(2f);
            tb.SetKeys(keys.ToArray());
            float tTop = tb.CtrlT(nn + 1);
            float hl = _headLen;
            tb.Bump = (t, th) =>
            {
                // The nape ridge and the throat muscles either side of the windpipe.
                float k = 0.012f * hl * GA(th, 90f, 16f) * SS(0.2f, 0.4f, t);
                k += 0.01f * hl * (GA(th, 300f, 22f) + GA(th, 240f, 22f)) * SS(0.25f, 0.45f, t) * (1f - SS(tTop - 0.05f, tTop, t));
                return k;
            };
            List<int> ab = new List<int> { _chest, _chest };
            List<float> at = new List<float> { 0f, tb.CtrlT(1) };
            for (int i = 0; i < _neckBones.Length; i++) { ab.Add(_neckBones[i]); at.Add(tb.CtrlT(2 + i) + (i == 0 ? 0.03f : 0f)); }
            ab.Add(_head); at.Add(tTop + 0.02f);
            int[] anchorsB = ab.ToArray();
            float[] anchorsT = at.ToArray();
            tb.Weight = (t, th) => AlongAnchors(anchorsB, anchorsT, t);
            _neckTube = tb;
            Vector2 hide = Pal(_k.Hide), bellyUV = Pal(_k.Belly);
            bool lightBelly = _k.Belly != _k.Hide;
            float tStart = 0.08f;
            EmitTube(tb, RingsEased(tStart, 1f, Segs(16, 11, 7), 0.15f), Segs(26, 18, 12), (t, th) => lightBelly && th > 238f && th < 302f ? bellyUV : hide, lightBelly ? new[] { 0f, 238f, 302f } : null, true, true);
        }

        // ------------------------------------------------------------------ head

        // Head profiles per style, along the head axis from the back of the skull (t 0) to the nose (t 1), in head
        // lengths: half width, height above the axis, depth below it (to the lip line on the muzzle), exponent.
        private static readonly float[][] HeadKeys =
        {
            // canine: domed cranium, wide cheekbones, a clear stop and a long tapering muzzle
            new[] { 0f, 0.07f, 0.07f, 0.07f, 2f, 0.05f, 0.17f, 0.17f, 0.15f, 2f, 0.16f, 0.22f, 0.21f, 0.18f, 2.1f, 0.3f, 0.25f, 0.19f, 0.2f, 2.2f,
                    0.4f, 0.21f, 0.16f, 0.18f, 2.2f, 0.5f, 0.145f, 0.115f, 0.13f, 2.3f, 0.66f, 0.115f, 0.105f, 0.1f, 2.4f, 0.84f, 0.1f, 0.095f, 0.085f, 2.4f,
                    0.95f, 0.085f, 0.08f, 0.07f, 2.4f, 1f, 0.03f, 0.03f, 0.025f, 2f },
            // feline: round skull, very wide cheekbones, a short broad face with full whisker pads and a blunt nose
            new[] { 0f, 0.14f, 0.14f, 0.14f, 2f, 0.1f, 0.3f, 0.33f, 0.27f, 2f, 0.25f, 0.37f, 0.37f, 0.31f, 2.1f, 0.42f, 0.38f, 0.36f, 0.31f, 2.2f,
                    0.56f, 0.35f, 0.3f, 0.29f, 2.2f, 0.66f, 0.29f, 0.22f, 0.26f, 2.3f, 0.76f, 0.26f, 0.18f, 0.23f, 2.4f, 0.87f, 0.24f, 0.16f, 0.2f, 2.5f,
                    0.95f, 0.21f, 0.14f, 0.16f, 2.4f, 0.985f, 0.16f, 0.11f, 0.12f, 2.2f, 1f, 0.08f, 0.06f, 0.06f, 2f },
            // lizard: flat, wide and long
            new[] { 0f, 0.11f, 0.07f, 0.07f, 2.4f, 0.06f, 0.22f, 0.12f, 0.11f, 2.5f, 0.2f, 0.27f, 0.14f, 0.12f, 2.6f, 0.4f, 0.25f, 0.13f, 0.1f, 2.6f,
                    0.6f, 0.2f, 0.11f, 0.08f, 2.6f, 0.8f, 0.155f, 0.09f, 0.06f, 2.6f, 0.94f, 0.11f, 0.07f, 0.05f, 2.4f, 1f, 0.035f, 0.025f, 0.02f, 2f },
            // dragon: long and angular
            new[] { 0f, 0.08f, 0.08f, 0.07f, 2.2f, 0.07f, 0.17f, 0.16f, 0.13f, 2.2f, 0.2f, 0.2f, 0.17f, 0.14f, 2.3f, 0.36f, 0.19f, 0.15f, 0.13f, 2.4f,
                    0.5f, 0.15f, 0.12f, 0.1f, 2.5f, 0.7f, 0.12f, 0.1f, 0.08f, 2.6f, 0.88f, 0.1f, 0.09f, 0.065f, 2.5f, 0.96f, 0.08f, 0.075f, 0.05f, 2.3f,
                    1f, 0.025f, 0.025f, 0.018f, 2f },
            // beaked: the cranium only (the beak is its own piece), over the first 62 percent of the head
            new[] { 0f, 0.07f, 0.07f, 0.07f, 2f, 0.08f, 0.18f, 0.2f, 0.17f, 2f, 0.25f, 0.23f, 0.23f, 0.21f, 2.1f, 0.45f, 0.22f, 0.21f, 0.21f, 2.2f,
                    0.65f, 0.18f, 0.17f, 0.17f, 2.2f, 0.85f, 0.14f, 0.14f, 0.14f, 2.3f, 1f, 0.11f, 0.11f, 0.12f, 2.3f }
        };

        private void QuadHead()
        {
            int row = StyleRow;
            float hl = _headLen;
            Vector3 hf = _headFwd, hu = _headUp;
            int hs = _k.HeadStyle;
            bool beak = hs == 4;
            float len = beak ? 0.62f : 1f;
            Vector3 a0 = _occiput, a1 = _occiput + hf * (hl * len);
            Vector3[] ctrl = { a0, Vector3.Lerp(a0, a1, 0.33f), Vector3.Lerp(a0, a1, 0.66f), a1 };
            float[] src = HeadKeys[row];
            float[] keys = new float[src.Length];
            for (int i = 0; i < src.Length; i += 5)
            {
                keys[i] = src[i];
                keys[i + 1] = src[i + 1] * hl;
                keys[i + 2] = src[i + 2] * hl;
                keys[i + 3] = src[i + 3] * hl;
                keys[i + 4] = src[i + 4];
            }
            CreatureTube tb = new CreatureTube(ctrl, hu, keys);
            float tEye = hs == 1 ? 0.58f : (hs == 2 ? 0.34f : (hs == 3 ? 0.36f : (hs == 4 ? 0.62f : 0.42f)));
            float tStop = hs == 1 ? 0.68f : (hs == 2 ? 0.48f : (hs == 3 ? 0.5f : 0.5f));
            tb.Bump = (t, th) =>
            {
                float k = 0f;
                // Brow ridges over the eyes, sockets below them, cheekbones under and behind the eyes, the nasal bridge.
                k += 0.022f * hl * G(t, tEye - 0.05f, 0.06f) * (GA(th, 50f, 15f) + GA(th, 130f, 15f));
                k -= 0.016f * hl * G(t, tEye + 0.01f, 0.04f) * (GA(th, 32f, 12f) + GA(th, 148f, 12f));
                k += 0.016f * hl * G(t, tEye - 0.06f, 0.07f) * (GA(th, 350f, 16f) + GA(th, 190f, 16f));
                k += 0.008f * hl * SS(tStop, tStop + 0.12f, t) * GA(th, 90f, 13f);
                // Lip line: the edge of the upper jaw stands out a little over the lower.
                k += 0.008f * hl * SS(tStop - 0.1f, tStop + 0.05f, t) * (GA(th, 296f, 9f) + GA(th, 244f, 9f));
                if (hs == 3)
                {
                    // Dragon: heavy brow crests running back toward the horns and a ridged snout.
                    k += 0.035f * hl * G(t, 0.3f, 0.12f) * (GA(th, 64f, 11f) + GA(th, 116f, 11f));
                    k += 0.012f * hl * G(t, 0.84f, 0.07f) * (GA(th, 72f, 12f) + GA(th, 108f, 12f));
                }
                if (hs == 1) k += 0.014f * hl * G(t, 0.84f, 0.06f) * (GA(th, 18f, 24f) + GA(th, 162f, 24f));
                if (hs == 0) k += 0.01f * hl * G(t, 0.88f, 0.05f) * (GA(th, 20f, 22f) + GA(th, 160f, 22f));
                return k;
            };
            int head = _head;
            tb.Weight = (t, th) => W1(head);
            _headTube = tb;
            Vector2 hide = Pal(_k.Hide), mouth = Pal(_k.Mouth), belly = Pal(_k.Belly);
            bool lizard = hs == 2;
            float tMouth = tStop - 0.06f;
            EmitTube(tb, RingsEased(0f, 1f, Segs(24, 16, 10), 0.35f), Segs(26, 18, 12), (t, th) =>
            {
                if (th > 252f && th < 288f && t > tMouth && !beak) return mouth;
                if (lizard && th > 228f && th < 312f) return belly;
                return hide;
            }, beak ? null : new[] { 0f, 252f, 288f }, false, beak);
            if (!beak)
            {
                // Lower jaw: hinged under the cheek, its top surface closing against the palate.
                Func<float, float> lip = t => tb.RadiusAt(t, 270f);
                float tH = 0.27f, tC = 0.97f;
                Vector3 hinge = _jawHinge;
                int n = 4;
                Vector3[] jc = new Vector3[n + 1];
                float jd = (hs == 2 ? 0.035f : (hs == 1 ? 0.06f : 0.05f)) * hl;
                for (int i = 0; i <= n; i++)
                {
                    float u = (float)i / n;
                    float t = Mathf.Lerp(tH - 0.05f, tC, u);
                    float drop = lip(Mathf.Max(t, tStop - 0.1f)) + jd * Mathf.Lerp(1.15f, 0.7f, u) * 0.82f;
                    if (u < 0.25f) drop = Mathf.Lerp(0.17f * hl, drop, u / 0.25f);
                    jc[i] = _occiput + hf * (t * hl) - hu * drop;
                }
                float ja = hs == 1 ? 1.25f : (hs == 2 ? 1.35f : (hs == 3 ? 1.1f : 1.12f));
                float[] jk =
                {
                    0f, 0.15f * hl * ja, jd * 1.1f, jd * 1.2f, 2f,
                    0.2f, 0.13f * hl * ja, jd * 1.05f, jd * 1.15f, 2.2f,
                    0.5f, 0.09f * hl * ja, jd * 0.9f, jd, 2.4f,
                    0.85f, 0.07f * hl * ja, jd * 0.75f, jd * 0.85f, 2.4f,
                    0.96f, 0.05f * hl * ja, jd * 0.55f, jd * 0.65f, 2.2f,
                    1f, 0.015f * hl, 0.012f * hl, 0.012f * hl, 2f
                };
                CreatureTube jt = new CreatureTube(jc, hu, jk);
                int jaw = _jaw;
                jt.Weight = (t, th) => t < 0.1f ? W2(head, jaw, SS(-0.02f, 0.1f, t)) : W1(jaw);
                _jawTube = jt;
                EmitTube(jt, RingsEased(0f, 1f, Segs(16, 11, 7), 0.3f), Segs(18, 14, 10), (t, th) => th > 48f && th < 132f && t > 0.12f ? mouth : (lizard ? belly : hide), new[] { 48f, 132f }, true, false);
                _mouthPoint = _occiput + hf * (0.93f * hl) - hu * (lip(0.93f) + jd * 0.2f);
                if (_detail > 0) Teeth(tb, jt, tStop);
            }
            else Beak();
            Eyes(tb, tEye);
            Nose(tb);
            if (_k.EarStyle > 0) Ears();
        }

        private void Teeth(CreatureTube head, CreatureTube jaw, float tStop)
        {
            Vector2 ivory = Pal(new Vector2Int(11, 9));
            Vector2 crystal = Pal(_k.Crystal);
            Vector3 down = -_headUp;
            float hl = _headLen;
            int hs = _k.HeadStyle;
            int extra = _detail == 2 ? 3 : 1;
            for (int side = -1; side <= 1; side += 2)
            {
                float th = side > 0 ? 284f : 256f;
                // Fangs: long upper canines near the front of the muzzle, the lower ones just behind and inside.
                float fang = (hs == 1 ? 0.085f : (hs == 3 ? 0.08f : 0.07f)) * hl;
                Vector3 baseU = head.Point(0.88f, th, -0.004f * hl);
                EmitHorn(baseU, baseU + down * fang - _headFwd * (0.006f * hl), _headFwd, 0.006f * hl, 0.011f * hl, 4, 6, hs == 3 ? crystal : ivory, W1(_head), W1(_head), 1f, 0.1f, true);
                Vector3 baseL = jaw.Point(0.84f, side > 0 ? 70f : 110f, -0.002f * hl);
                EmitHorn(baseL, baseL + _headUp * (fang * 0.6f) - _headFwd * (0.004f * hl), _headFwd, 0.003f * hl, 0.009f * hl, 3, 5, ivory, W1(_jaw), W1(_jaw), 1f, 0.1f, true);
                for (int i = 0; i < extra; i++)
                {
                    float t = Mathf.Lerp(0.82f, tStop + 0.04f, (i + 1f) / (extra + 1f));
                    Vector3 b = head.Point(t, th - side * 2f, -0.003f * hl);
                    EmitHorn(b, b + down * (0.03f * hl), _headFwd, 0.002f * hl, 0.007f * hl, 3, 4, hs == 3 ? crystal : ivory, W1(_head), W1(_head), 1f, 0.1f, true);
                }
            }
        }

        private void Eyes(CreatureTube head, float tEye)
        {
            Vector2 glow = Pal(_k.Glow);
            float hl = _headLen;
            int hs = _k.HeadStyle;
            float side = hs == 2 ? 30f : (hs == 1 ? 34f : (hs == 4 ? 14f : (hs == 3 ? 24f : 36f)));
            for (int s = -1; s <= 1; s += 2)
            {
                float th = s > 0 ? side : 180f - side;
                Vector3 p, n;
                head.Frame(tEye, th, out p, out n);
                Vector3 fwd, sd, up;
                head.Axes(tEye, out fwd, out sd, out up);
                // A slanted slit: long along the head, thin, the outer corner raised toward the temple.
                Vector3 along = Nrm(fwd - n * Vector3.Dot(fwd, n));
                Vector3 across = Nrm(Vector3.Cross(n, along));
                float tilt = (hs == 2 ? 6f : 18f) * s;
                Quaternion rot = QMath.AngleAxis(tilt, n) * QMath.FromBasis(across, n, along);
                float len = (hs == 1 ? 0.085f : (hs == 4 ? 0.05f : (hs == 2 ? 0.045f : 0.055f))) * hl;
                float tall = (hs == 1 ? 0.04f : (hs == 4 ? 0.026f : 0.016f)) * hl;
                EmitEllipsoid(p - n * (0.004f * hl), new Vector3(tall, 0.01f * hl, len), rot, Segs(6, 5, 4), Segs(12, 10, 8), glow, W1(_head));
            }
        }

        private void Nose(CreatureTube head)
        {
            int hs = _k.HeadStyle;
            if (hs == 4) return;
            float hl = _headLen;
            Vector2 nose = Pal(new Vector2Int(15, 9));
            Quaternion r = QMath.FromBasis(Vector3.right, _headUp, _headFwd);
            if (hs == 0 || hs == 1)
            {
                Vector3 tip = head.Point(0.975f, 90f, 0f);
                float w = hs == 1 ? 0.06f : 0.05f;
                EmitEllipsoid(tip - _headUp * (0.02f * hl) + _headFwd * (0.004f * hl), new Vector3(w * hl, 0.03f * hl, 0.03f * hl), r, Segs(6, 5, 4), Segs(12, 10, 8), nose, W1(_head));
            }
            else
            {
                // Reptiles: two small nostril pits as dark beads.
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector3 p = head.Point(0.94f, s > 0 ? 60f : 120f, -0.004f * hl);
                    EmitEllipsoid(p, new Vector3(0.012f * hl, 0.007f * hl, 0.016f * hl), r, 3, 6, nose, W1(_head));
                }
            }
        }

        private void Ears()
        {
            int style = _k.EarStyle;
            float hl = _headLen;
            Vector2 hide = Pal(_k.Hide), inner = Pal(_k.Accent), glow = Pal(_k.Glow);
            for (int s = -1; s <= 1; s += 2)
            {
                int bone = s < 0 ? _earL : _earR;
                Vector3 b = _bm[bone];
                if (style == 3)
                {
                    // Griffin: tufts of feathers swept back from the crown.
                    for (int f = 0; f < 3; f++)
                    {
                        Vector3 dir = Nrm(-_headFwd * 0.85f + _headUp * (0.5f - f * 0.14f) + Vector3.right * (s * (0.28f + f * 0.08f)));
                        EmitFeather(b - _headFwd * (f * 0.03f * hl), dir, Nrm(_headUp + Vector3.right * s * 0.3f), (0.32f - f * 0.06f) * hl, 0.07f * hl, 0.08f, hide, Pal(_k.Accent), 0.72f, W1(bone), 3, 2);
                    }
                    continue;
                }
                float earH = style == 1 ? 0.47f * hl : 0.27f * hl;
                float earW = style == 1 ? 0.19f * hl : 0.24f * hl;
                Vector3 up = Nrm(_headUp + Vector3.right * (s * (style == 1 ? 0.24f : 0.55f)) - _headFwd * (style == 1 ? 0.02f : 0.2f));
                Vector3 face = Nrm(_headFwd + Vector3.right * (s * 0.35f));
                Vector3 tipP = b + up * earH;
                Vector3[] ctrl = { b - up * (0.12f * earH), b + up * (0.3f * earH) + face * (0.02f * earH), b + up * (0.68f * earH) + face * (0.01f * earH), tipP - face * (0.03f * earH) };
                float[] keys =
                {
                    0f, earW * 0.8f, earW * 0.2f, earW * 0.32f, 2f,
                    0.14f, earW * 0.92f, earW * 0.16f, earW * 0.3f, 2f,
                    0.42f, earW * 0.72f, earW * 0.13f, earW * 0.24f, 2f,
                    0.76f, earW * (style == 1 ? 0.38f : 0.55f), earW * 0.1f, earW * 0.17f, 2f,
                    1f, earW * (style == 1 ? 0.035f : 0.2f), earW * 0.04f, earW * 0.06f, 2f
                };
                CreatureTube tb = new CreatureTube(ctrl, face, keys);
                // Cup: the front face dishes deeply into a shell, so the inner ear catches the light.
                tb.Bump = (t, th) => -earW * 0.3f * SS(0.06f, 0.28f, t) * (1f - SS(0.7f, 0.98f, t)) * GA(th, 90f, 38f);
                int head = _head;
                tb.Weight = (t, th) => t < 0.14f ? W2(head, bone, SS(-0.05f, 0.14f, t)) : W1(bone);
                EmitTube(tb, RingsEased(0f, 1f, Segs(10, 8, 6), 0.2f), Segs(14, 10, 8), (t, th) => th > 48f && th < 132f && t > 0.1f ? inner : hide, new[] { 48f, 132f }, true, true);
                if (style == 1 && _detail > 0)
                {
                    // Thin neon lines just inside the rims of the ears.
                    EmitRibbon(tb, 0.16f, 0.86f, t => 50f, 0.022f * earW, Segs(8, 6, 4), glow, 0.0015f * hl);
                    EmitRibbon(tb, 0.16f, 0.86f, t => 130f, 0.022f * earW, Segs(8, 6, 4), glow, 0.0015f * hl);
                }
            }
        }

        /// <summary>Griffin beak: a hooked upper mandible over a shorter lower one, both horn, set on the front of the skull.</summary>
        private void Beak()
        {
            float hl = _headLen;
            Vector3 hf = _headFwd, hu = _headUp;
            Vector2 horn = Pal(_k.Claw), mouth = Pal(_k.Mouth), hide = Pal(_k.Hide);
            Vector3 b0 = _occiput + hf * (0.56f * hl) + hu * (0.01f * hl);
            Vector3[] up =
            {
                b0,
                b0 + hf * (0.16f * hl) + hu * (0.03f * hl),
                b0 + hf * (0.32f * hl) + hu * (0.0f * hl),
                b0 + hf * (0.42f * hl) - hu * (0.07f * hl),
                b0 + hf * (0.41f * hl) - hu * (0.15f * hl)
            };
            float[] uk =
            {
                0f, 0.1f * hl, 0.095f * hl, 0.08f * hl, 2.2f,
                0.28f, 0.078f * hl, 0.08f * hl, 0.06f * hl, 2.4f,
                0.58f, 0.052f * hl, 0.055f * hl, 0.04f * hl, 2.6f,
                0.84f, 0.026f * hl, 0.026f * hl, 0.022f * hl, 2.4f,
                1f, 0.006f * hl, 0.006f * hl, 0.006f * hl, 2f
            };
            CreatureTube ub = new CreatureTube(up, hu, uk);
            int head = _head, jaw = _jaw;
            ub.Weight = (t, th) => W1(head);
            EmitTube(ub, RingsEased(0f, 1f, Segs(13, 10, 7), 0.3f), Segs(16, 12, 8), (t, th) => th > 248f && th < 292f && t < 0.6f ? mouth : horn, new[] { 0f, 248f, 292f }, true, true);
            Vector3 l0 = _occiput + hf * (0.5f * hl) - hu * (0.07f * hl);
            Vector3[] lo = { l0, l0 + hf * (0.16f * hl) - hu * (0.005f * hl), l0 + hf * (0.3f * hl) - hu * (0.02f * hl) };
            float[] lk = { 0f, 0.085f * hl, 0.04f * hl, 0.045f * hl, 2.2f, 0.5f, 0.06f * hl, 0.032f * hl, 0.036f * hl, 2.4f, 1f, 0.012f * hl, 0.01f * hl, 0.01f * hl, 2f };
            CreatureTube lb = new CreatureTube(lo, hu, lk);
            lb.Weight = (t, th) => W1(jaw);
            EmitTube(lb, RingsEased(0f, 1f, Segs(10, 8, 6), 0.3f), Segs(14, 10, 8), (t, th) => th > 60f && th < 120f ? mouth : horn, new[] { 60f, 120f }, true, true);
            // A feathered throat under the skull joins the lower mandible to the neck.
            Vector3[] th2 = { _jawHinge - hf * (0.06f * hl), _jawHinge + hf * (0.12f * hl), l0 + hf * (0.02f * hl) };
            float[] tk = { 0f, 0.12f * hl, 0.07f * hl, 0.08f * hl, 2f, 0.6f, 0.11f * hl, 0.065f * hl, 0.075f * hl, 2.1f, 1f, 0.085f * hl, 0.045f * hl, 0.05f * hl, 2.2f };
            CreatureTube tt = new CreatureTube(th2, hu, tk);
            tt.Weight = (t, th) => t < 0.15f ? W2(head, jaw, SS(0f, 0.15f, t)) : W1(jaw);
            EmitTube(tt, Rings(0f, 1f, Segs(8, 6, 4)), Segs(14, 10, 8), (t, th) => hide, null, true, true);
            _nose = up[3];
            _mouthPoint = b0 + hf * (0.3f * hl) - hu * (0.07f * hl);
        }

        // ------------------------------------------------------------------ legs

        private readonly List<CreatureTube> _legTubes = new List<CreatureTube>();

        private void QuadLeg(LegPlan lp)
        {
            float S = lp.Sum;
            int hs = _k.HeadStyle;
            float bulk = Mathf.Lerp(1f, Mathf.Max(0.6f, _k.Bulk), 0.6f);
            // Limb mass per build: lean canid, massive big cat, heavy lizard, powerful dragon, griffin.
            float massUp = hs == 1 ? 1.55f : (hs == 2 ? 1.75f : (hs == 3 ? 1.5f : (hs == 4 ? 1.3f : 1f)));
            float massLo = hs == 1 ? 1.62f : (hs == 2 ? 1.4f : (hs == 3 ? 1.38f : (hs == 4 ? 1.2f : 1f)));
            float sprawl = Mathf.Clamp01(_k.Sprawl);
            Vector3 fwdHint = Nrm(Vector3.forward + Vector3.right * (lp.Side * 0.15f * sprawl));
            Vector3 entry = lp.Front ? lp.Girdle + new Vector3(0f, 0.04f * S, 0f) : lp.Hip + new Vector3(-lp.Side * 0.05f * S, 0.18f * S, -0.02f * S);
            Vector3[] ctrl = { entry, lp.Front ? lp.Girdle : Vector3.Lerp(entry, lp.Hip, 0.5f), lp.Hip, lp.Knee, lp.Ankle, lp.Paw };
            CreatureTube tb = new CreatureTube(ctrl, fwdHint, new float[] { 0f, 0.1f, 0.1f, 0.1f, 2f });
            float tE = tb.CtrlT(1), tH = tb.CtrlT(2), tK = tb.CtrlT(3), tA = tb.CtrlT(4);
            List<float> keys = new List<float>();
            void K(float t, float a, float bu, float bd)
            {
                float m = Mathf.Lerp(massUp, massLo, SS(tH, tK + 0.05f, t)) * bulk;
                keys.Add(t); keys.Add(a * S * m); keys.Add(bu * S * m); keys.Add(bd * S * m); keys.Add(2.1f);
            }
            if (lp.Front)
            {
                K(0f, 0.06f, 0.1f, 0.1f);
                K(tE, 0.075f, 0.13f, 0.12f);
                K(Mathf.Lerp(tE, tH, 0.55f), 0.09f, 0.14f, 0.12f);
                K(tH, 0.095f, 0.13f, 0.11f);
                K(Mathf.Lerp(tH, tK, 0.5f), 0.082f, 0.09f, 0.12f);
                K(tK, 0.062f, 0.065f, 0.075f);
                K(Mathf.Lerp(tK, tA, 0.45f), 0.056f, 0.06f, 0.055f);
                K(tA, 0.042f, 0.04f, 0.045f);
                K(Mathf.Lerp(tA, 1f, 0.5f), 0.042f, 0.035f, 0.036f);
                K(1f, 0.048f, 0.036f, 0.036f);
            }
            else
            {
                K(0f, 0.055f, 0.1f, 0.1f);
                K(tE, 0.08f, 0.15f, 0.14f);
                K(tH, 0.088f, 0.17f, 0.15f);
                K(Mathf.Lerp(tH, tK, 0.5f), 0.08f, 0.14f, 0.15f);
                K(tK, 0.06f, 0.06f, 0.07f);
                K(Mathf.Lerp(tK, tA, 0.4f), 0.05f, 0.045f, 0.062f);
                K(tA, 0.034f, 0.03f, 0.04f);
                K(Mathf.Lerp(tA, 1f, 0.5f), 0.032f, 0.028f, 0.028f);
                K(1f, 0.04f, 0.03f, 0.03f);
            }
            tb.SetKeys(keys.ToArray());
            _legTubes.Add(tb);
            bool front = lp.Front;
            tb.Bump = (t, th) =>
            {
                float k = 0f;
                if (front)
                {
                    k += 0.035f * S * G(t, tH, 0.035f) * GA(th, 90f, 40f);           // point of the shoulder
                    k += 0.04f * S * G(t, tK, 0.03f) * GA(th, 270f, 30f);            // elbow (olecranon)
                    k += 0.012f * S * G(t, tA, 0.025f) * GA(th, 90f, 50f);           // wrist
                }
                else
                {
                    k += 0.03f * S * G(t, tK, 0.03f) * GA(th, 90f, 40f);             // kneecap
                    k += 0.035f * S * G(t, tA, 0.03f) * GA(th, 270f, 26f);           // point of the hock
                    k += 0.02f * S * G(t, Mathf.Lerp(tK, tA, 0.35f), 0.05f) * GA(th, 270f, 40f); // calf
                }
                return k;
            };
            int girdle = lp.Front ? lp.GirdleBone : _pelvis;
            int top = lp.Front ? _chest : _pelvis;
            int up = lp.Upper, lo = lp.Lower, ft = lp.Foot, toe = lp.Toe;
            tb.Weight = (t, th) =>
            {
                if (t < tE) return front ? W2(top, girdle, SS(0f, tE, t)) : W1(girdle);
                if (t < tH) return W2(girdle, up, SS(tE + (tH - tE) * 0.3f, tH + 0.02f, t));
                if (t < tK) return W2(up, lo, SS(tK - 0.06f, tK + 0.03f, t));
                if (t < tA) return W2(lo, ft, SS(tA - 0.04f, tA + 0.03f, t));
                return W2(ft, toe, SS(0.96f, 1.02f, t));
            };
            Vector2 hide = Pal(_k.Hide);
            Vector2 lower = _k.Coat == 2 && lp.Front ? Pal(_k.Claw) : hide;
            float tScale = _k.Coat == 2 && lp.Front ? tK + 0.08f : 2f;
            EmitTube(tb, RingsEased(0f, 1f, Segs(20, 13, 9), 0.15f), Segs(16, 11, 8), (t, th) => t > tScale ? lower : (_k.Coat == 2 && !lp.Front ? Pal(_k.Accent) : (_k.Coat == 2 ? hide : hide)), null, true, false);
            Paw(lp, bulk * (hs == 1 ? 1.4f : (hs == 3 ? 1.35f : (hs == 2 ? 1.1f : 1f))));
        }

        private void Paw(LegPlan lp, float bulk)
        {
            float S = lp.Sum;
            bool bird = _k.Coat == 2 && lp.Front;
            float sprawl = Mathf.Clamp01(_k.Sprawl);
            Vector3 fwd = Nrm(new Vector3(lp.ToeTip.x - lp.Paw.x, 0f, lp.ToeTip.z - lp.Paw.z));
            Vector3 sd = Nrm(Vector3.Cross(Vector3.up, fwd));
            float pawLen = (lp.ToeTip - lp.Paw).magnitude;
            float pad = lp.Paw.y;
            Vector2 hide = Pal(_k.Hide), claw = Pal(_k.Claw), padUV = Pal(new Vector2Int(15, 9));
            int toe = lp.Toe, foot = lp.Foot;
            if (bird)
            {
                // Eagle forefoot: scaled toes spread forward and a hind talon, each ending in a curved claw.
                for (int f = 0; f < 4; f++)
                {
                    float ang = f < 3 ? (f - 1) * 22f : 180f;
                    Vector3 dir = Nrm(QMath.AY(ang) * fwd + Vector3.down * 0.18f);
                    float len = (f == 3 ? 0.55f : (f == 1 ? 1f : 0.85f)) * pawLen * 1.1f;
                    Vector3 a = lp.Paw + Vector3.down * (pad * 0.3f);
                    Vector3 e = a + dir * len;
                    e.y = Mathf.Max(e.y, pad * 0.35f);
                    EmitHorn(a, e, Vector3.up, 0.08f * len, 0.05f * S * bulk, Segs(4, 4, 3), Segs(7, 6, 5), claw, W1(toe), W1(toe), 1f, 0.5f);
                    EmitHorn(e, e + dir * (0.32f * len) + Vector3.down * (pad * 0.9f), Vector3.up, 0.1f * len, 0.026f * S * bulk, Segs(4, 3, 3), Segs(6, 5, 4), Pal(new Vector2Int(10, 9)), W1(toe), W1(toe), 1f, 0.1f, true);
                }
                return;
            }
            float pw = (sprawl > 0.5f ? 0.07f : 0.055f) * S * bulk;
            Vector3 heel = lp.Paw - fwd * (pawLen * 0.32f) + Vector3.up * (pad * 0.25f);
            Vector3 front = lp.Paw + fwd * (pawLen * 0.82f) + Vector3.down * (pad * 0.35f);
            Vector3[] ctrl = { heel, lp.Paw + Vector3.down * (pad * 0.1f), Vector3.Lerp(lp.Paw, front, 0.6f) + Vector3.down * (pad * 0.25f), front };
            float[] keys =
            {
                0f, pw * 0.55f, pw * 0.45f, pw * 0.4f, 2.2f,
                0.25f, pw * 0.85f, pw * 0.62f, pad * 0.9f, 2.4f,
                0.6f, pw * 1.0f, pw * 0.5f, pad * 0.75f, 2.6f,
                0.88f, pw * 0.85f, pw * 0.36f, pad * 0.5f, 2.4f,
                1f, pw * 0.3f, pw * 0.15f, pad * 0.2f, 2f
            };
            CreatureTube tb = new CreatureTube(ctrl, Vector3.up, keys);
            int toes = sprawl > 0.5f ? 5 : 4;
            tb.Bump = (t, th) =>
            {
                float k = 0f;
                // Toes: lobes across the front of the paw.
                for (int i = 0; i < toes; i++)
                {
                    float ang = 90f + (i - (toes - 1) * 0.5f) * (sprawl > 0.5f ? 34f : 28f);
                    k += 0.12f * pw * G(t, 0.78f, 0.1f) * GA(th, ang, 12f);
                }
                k -= 0.08f * pw * G(t, 0.8f, 0.12f) * (GA(th, 90f + 14f, 5f) + GA(th, 90f - 14f, 5f));
                return k;
            };
            tb.Weight = (t, th) => t < 0.2f ? W2(foot, toe, SS(0f, 0.2f, t)) : W1(toe);
            EmitTube(tb, RingsEased(0f, 1f, Segs(10, 7, 5), 0.3f), Segs(14, 10, 8), (t, th) => th > 215f && th < 325f ? padUV : hide, new[] { 0f, 215f, 325f }, true, true);
            if (_detail > 0)
            {
                for (int i = 0; i < toes; i++)
                {
                    float ang = 90f + (i - (toes - 1) * 0.5f) * (sprawl > 0.5f ? 34f : 28f);
                    Vector3 cp = tb.Point(0.9f, ang, 0f);
                    Vector3 dir = Nrm(fwd + Vector3.down * 0.9f + sd * Mathf.Cos(ang * Mathf.Deg2Rad) * 0.4f);
                    float cl = (sprawl > 0.5f ? 0.09f : 0.06f) * S;
                    EmitHorn(cp - fwd * (0.01f * S), cp + dir * cl, Vector3.up, 0.25f * cl, 0.014f * S * bulk, Segs(4, 3, 3), 5, claw, W1(toe), W1(toe), 1f, 0.1f, true);
                }
            }
        }

        // ------------------------------------------------------------------ tail

        private void QuadTail()
        {
            int n = _tailPts.Length;
            float tailLen = Mathf.Max(0.05f, _k.TailLength) * _s;
            int style = _k.TailStyle;
            int[] bones = _tailBones;
            if (MergedTail)
            {
                TailTip(_bodyTube, 0f, 0.035f, bones[bones.Length - 1], tailLen, true);
                return;
            }
            Vector3 dir0 = Nrm(_tailPts[1] - _tailPts[0]);
            Vector3 last = _tailPts[n - 1] + Nrm(_tailPts[n - 1] - _tailPts[n - 2]) * (tailLen / (n - 1) * 0.6f);
            List<Vector3> ctrl = new List<Vector3>();
            // Start inside the rump (forward and below the tail root) so the tail grows out of the body.
            ctrl.Add(_tailPts[0] + new Vector3(0f, -0.06f * _H, 0.1f * _H));
            for (int i = 0; i < n; i++) ctrl.Add(_tailPts[i]);
            ctrl.Add(last);
            float H = _H;
            float baseR = (style == 1 ? 0.045f : (style == 2 ? 0.032f : 0.05f)) * H * Mathf.Lerp(1f, _k.Bulk, 0.5f);
            float[] keys;
            if (style == 1)
                // Brush: thin at the root, full through the middle, a soft point.
                keys = new[] { 0f, baseR * 1.3f, baseR * 1.3f, baseR * 1.3f, 2f, 0.1f, baseR, baseR, baseR, 2f, 0.42f, baseR * 1.9f, baseR * 1.9f, baseR * 1.85f, 2f,
                               0.72f, baseR * 1.8f, baseR * 1.8f, baseR * 1.75f, 2f, 0.92f, baseR * 1.05f, baseR * 1.05f, baseR * 1f, 2f, 1f, baseR * 0.08f, baseR * 0.08f, baseR * 0.08f, 2f };
            else if (style == 2)
                keys = new[] { 0f, baseR * 1.4f, baseR * 1.4f, baseR * 1.4f, 2f, 0.1f, baseR, baseR, baseR, 2f, 0.6f, baseR * 0.66f, baseR * 0.66f, baseR * 0.66f, 2f,
                               0.97f, baseR * 0.5f, baseR * 0.5f, baseR * 0.5f, 2f, 1f, baseR * 0.3f, baseR * 0.3f, baseR * 0.3f, 2f };
            else
                // Cat: thick and even, the tip rounded.
                keys = new[] { 0f, baseR * 1.4f, baseR * 1.4f, baseR * 1.4f, 2f, 0.1f, baseR * 1.05f, baseR * 1.05f, baseR * 1.05f, 2f, 0.5f, baseR * 0.88f, baseR * 0.88f, baseR * 0.88f, 2f,
                               0.88f, baseR * 0.78f, baseR * 0.78f, baseR * 0.78f, 2f, 0.97f, baseR * 0.58f, baseR * 0.58f, baseR * 0.58f, 2f, 1f, baseR * 0.12f, baseR * 0.12f, baseR * 0.12f, 2f };
            CreatureTube tb = new CreatureTube(ctrl.ToArray(), Vector3.up, keys);
            float[] boneT = new float[n];
            for (int i = 0; i < n; i++) boneT[i] = tb.CtrlT(i + 1);
            if (style == 1) tb.Bump = (t, th) => baseR * 0.22f * SS(0.15f, 0.35f, t) * (1f - SS(0.9f, 1f, t)) * Mathf.Cos((th * 7f + t * 300f) * Mathf.Deg2Rad);
            int pelvis = _pelvis;
            tb.Weight = (t, th) =>
            {
                if (t <= boneT[0]) return W2(pelvis, bones[0], SS(boneT[0] * 0.3f, boneT[0], t));
                for (int i = 0; i < bones.Length - 1; i++)
                    if (t < boneT[i + 1]) return W2(bones[i], bones[i + 1], SS(Mathf.Lerp(boneT[i], boneT[i + 1], 0.2f), boneT[i + 1], t));
                return W1(bones[bones.Length - 1]);
            };
            _tailTube = tb;
            Vector2 hide = Pal(_k.Hide), tip = style == 1 ? Pal(_k.Belly) : (style == 2 ? Pal(_k.Accent) : hide);
            Vector2 body = _k.Coat == 2 ? Pal(_k.Accent) : hide;
            float tipFrom = style == 1 ? 0.86f : 2f;
            bool rings = _k.Accent != _k.Hide && _k.HeadStyle == 1;
            EmitTube(tb, RingsEased(0f, 1f, Segs(style == 1 ? 22 : 18, 14, 9), 0.25f), Segs(style == 1 ? 18 : 14, 11, 8), (t, th) =>
            {
                if (t > tipFrom) return tip;
                // Frost stalker: banded tail with a dark tip.
                if (rings && t > 0.3f && (Mathf.Repeat(t * 7.5f, 1f) < 0.32f || t > 0.93f)) return Pal(_k.Accent);
                return body;
            }, null, true, false);
            TailTip(tb, 1f, 0f, bones[bones.Length - 1], tailLen, false);
        }

        /// <summary>The end of a tail: a lion tuft or a crystal blade (at t = tipT of the tube, pointing away from tipT + toward).</summary>
        private void TailTip(CreatureTube tb, float tipT, float inward, int bone, float tailLen, bool reversed)
        {
            int style = _k.TailStyle;
            float tIn = reversed ? tipT + 0.03f : tipT - 0.03f;
            Vector3 e = tb.Center(tipT), d = Nrm(tb.Center(tipT) - tb.Center(tIn));
            if (style == 2)
            {
                // Lion tuft: a brush of long hair at the tip.
                for (int i = 0, tufts = Segs(6, 4, 3); i < tufts; i++)
                {
                    Vector3 off = QMath.AngleAxis(i * 360f / tufts, d) * Nrm(Vector3.Cross(d, Vector3.right));
                    EmitHorn(e - d * (0.04f * tailLen), e + d * (0.12f * tailLen) + off * (0.035f * tailLen), off, 0.01f * tailLen, 0.03f * tailLen, Segs(4, 3, 3), Segs(6, 5, 4), Pal(_k.Accent), W1(bone), W1(bone), 1f, 0.3f);
                }
            }
            else if (style == 3)
            {
                // A crystal blade at the tip of the tail, flat and wide like a spearhead, with a smaller fin above it.
                Vector3 b = tb.Center(reversed ? tipT + 0.06f : tipT - 0.06f);
                float bl = 0.17f * tailLen;
                Vector2 c = Pal(_k.Crystal);
                EmitCrystal(b, d, Vector3.right, bl, 0.26f * bl, 4, 0.55f, c, W1(bone), 0.05f, 0.32f);
                EmitCrystal(b - d * (0.03f * tailLen), Nrm(d + Vector3.up * 0.7f), Vector3.right, bl * 0.55f, 0.18f * bl, 4, 0.6f, c, W1(bone), 0.08f, 0.38f);
                EmitCrystal(b - d * (0.06f * tailLen), Nrm(d + Vector3.down * 0.5f), Vector3.right, bl * 0.4f, 0.14f * bl, 4, 0.6f, c, W1(bone), 0.08f, 0.38f);
            }
        }

        // ------------------------------------------------------------------ crystals, plates and glowing seams

        /// <summary>
        /// A ridge of crystal spines along a tube's top line from t0 to t1, raked back toward lower t (or higher t when
        /// rakeToHigher), sized by a bell over the ridge, each with two small side crystals on the larger ones.
        /// </summary>
        private void SpineRow(CreatureTube tb, float t0, float t1, int count, float size, float rake, bool rakeToHigher, Vector2 uv, float theta = 90f)
        {
            for (int i = 0; i < count; i++)
            {
                float u = count == 1 ? 0.5f : (float)i / (count - 1);
                float t = Mathf.Lerp(t0, t1, u);
                float bell = Mathf.Lerp(0.55f, 1f, Mathf.Sin(Mathf.Lerp(0.15f, 0.85f, u) * Mathf.PI)) * Rand(0.85f, 1.12f);
                float len = size * bell;
                Vector3 p, nrm;
                tb.Frame(t, theta, out p, out nrm);
                Vector3 along = Nrm(tb.Center(t + 0.01f) - tb.Center(t - 0.01f));
                Vector3 back = rakeToHigher ? along : -along;
                Vector3 dir = Nrm(nrm + back * rake);
                BoneWeight w = tb.WeightAt(t, theta);
                EmitCrystal(p - nrm * (0.18f * len), dir, Vector3.Cross(along, nrm), len, 0.17f * len, 5, 0.38f, uv, w);
                if (_detail > 0 && bell > 0.8f)
                {
                    for (int sgn = -1; sgn <= 1; sgn += 2)
                    {
                        Vector3 sp, sn;
                        float th = theta + sgn * 14f;
                        tb.Frame(t, th, out sp, out sn);
                        Vector3 side = Nrm(Vector3.Cross(along, sn)) * sgn;
                        EmitCrystal(sp - sn * (0.12f * len), Nrm(sn + back * rake * 0.8f - side * 0.3f), along, len * 0.42f, 0.18f * len * 0.42f, 5, 0.4f, uv, tb.WeightAt(t, Wrap360(th)));
                    }
                }
            }
        }

        private void QuadDetails()
        {
            CreatureTube tb = _bodyTube;
            Vector2 crystal = Pal(_k.Crystal), glow = Pal(_k.Glow);
            int nT = _bodyTrunkCount;
            int hs = _k.HeadStyle;
            float tHip = _bodyT[2], tWither = _bodyT[7], tWaist = _bodyT[4];
            int n = _k.Spines;
            if (n > 0)
            {
                if (hs == 0)
                {
                    // Jackal: spines up the back of the neck and along the back to the loin.
                    int neckN = Mathf.Max(2, n / 3);
                    SpineRow(_neckTube, 0.62f, 0.3f, neckN, 0.08f * _H, 0.9f, false, crystal);
                    SpineRow(tb, tWither - 0.01f, Mathf.Lerp(tWaist, tHip, 0.3f), n - neckN, 0.12f * _H, 0.75f, false, crystal);
                }
                else if (hs == 3)
                {
                    // Dragon: a crest from the skull down the neck, the back and the tail.
                    int neckN = Mathf.Max(3, n / 3), tailN = Mathf.Max(3, n / 3), backN = Mathf.Max(2, n - neckN - tailN);
                    SpineRow(_neckTube, 0.9f, 0.25f, neckN, 0.1f * _H, 0.8f, false, crystal);
                    SpineRow(tb, tWither, tHip, backN, 0.14f * _H, 0.7f, false, crystal);
                    if (_tailJoinT > 0f) SpineRow(tb, _tailJoinT * 0.95f, _tailJoinT * 0.25f, tailN, 0.09f * _H, 0.7f, false, crystal);
                }
                else if (hs == 1)
                {
                    // Stalker: a few ice crystals along the spine between the shoulder plates.
                    SpineRow(tb, tWither + 0.01f, tWither - 0.07f, n, 0.07f * _H, 0.9f, false, crystal);
                }
            }
            if (_k.Plates == 1) ShoulderPlates();
            if (_k.Plates == 2 || _k.Plates == 3) BackArmour();
            // Glowing seams: a line along each flank (bright on the jackal, a dimmer accent on the scaled creatures).
            if (_detail > 0 && (hs == 0 || hs == 2))
            {
                float w = 0.011f * _H;
                float a = tHip + 0.03f, b = tWither - 0.04f;
                float lift = hs == 2 ? -6f : 0f;
                EmitRibbon(tb, a, b, t => 18f + lift + 9f * Mathf.Sin((t - a) / (b - a) * Mathf.PI * 2f), w, Segs(18, 12, 6), glow, 0.003f * _H);
                EmitRibbon(tb, a, b, t => 162f - lift - 9f * Mathf.Sin((t - a) / (b - a) * Mathf.PI * 2f), w, Segs(18, 12, 6), glow, 0.003f * _H);
                if (hs == 0 && _headTube != null) EmitRibbon(_headTube, 0.14f, 0.44f, t => 90f, 0.012f * _headLen, Segs(8, 6, 4), glow, 0.001f * _headLen);
            }
            WeakCrystal();
            CoatDetails();
        }

        /// <summary>
        /// Secondary shapes that break up the smooth surfaces: fur tufts (cheek ruffs, the throat and chest, elbows and the
        /// backs of the thighs, a stalker's long belly fur) and the frost stalker's stripes.
        /// </summary>
        private void CoatDetails()
        {
            int hs = _k.HeadStyle;
            if (_detail == 0 || (hs != 0 && hs != 1)) return;
            Vector2 hide = Pal(_k.Hide), accent = Pal(_k.Accent);
            float H = _H, hl = _headLen;
            Vector3 back = Vector3.back;
            bool hero = _detail == 2;
            // Cheek ruff: fur sweeping back from behind the jaw.
            for (int s = -1; s <= 1; s += 2)
            {
                float a0 = s > 0 ? 300f : 200f, a1 = s > 0 ? 350f : 240f;
                FurFringe(_headTube, 0.24f, 0.03f, a0, a1, hero ? 5 : 3, 0.16f * hl, 0.075f * hl, -_headFwd * 0.8f + Vector3.down * 0.35f + Vector3.right * (s * 0.3f), 0.25f, hide);
            }
            // Throat ruff on the upper neck.
            FurFringe(_neckTube, 0.72f, 0.05f, 205f, 335f, hero ? 8 : 5, 0.09f * H, 0.05f * H, Vector3.down * 0.9f + back * 0.5f, 0.2f, hs == 1 ? Pal(_k.Belly) : hide);
            // Chest tuft at the front of the brisket.
            int nT = _bodyTrunkCount;
            FurFringe(_bodyTube, _bodyT[nT - 1] + 0.01f, 0.012f, 248f, 292f, hero ? 5 : 3, 0.08f * H, 0.045f * H, Vector3.down + back * 0.4f, 0.15f, Pal(_k.Belly));
            for (int i = 0; i < _legTubes.Count && i < _legs.Count; i++)
            {
                LegPlan lp = _legs[i];
                CreatureTube lt = _legTubes[i];
                if (lp.Front)
                {
                    // Elbow tufts at the back of the forearm.
                    float tK = lt.CtrlT(3);
                    FurFringe(lt, tK + 0.04f, 0.02f, 250f, 290f, hero ? 3 : 2, 0.07f * H, 0.035f * H, Vector3.down + back * 0.6f, 0.3f, hide);
                }
                else
                {
                    // Britches: long fur down the back of the thigh.
                    float tH = lt.CtrlT(2), tK = lt.CtrlT(3);
                    FurFringe(lt, Mathf.Lerp(tH, tK, 0.45f), 0.06f, 235f, 305f, hero ? 5 : 3, 0.09f * H, 0.05f * H, Vector3.down + back * 0.5f, 0.25f, hide);
                }
            }
            if (hs == 1)
            {
                // Long belly fur hanging from the underside, and the frost stripes.
                float tA = _bodyT[3], tB = _bodyT[7];
                for (int s = -1; s <= 1; s += 2)
                    FurFringe(_bodyTube, (tA + tB) * 0.5f, (tB - tA) * 0.5f, s > 0 ? 272f : 248f, s > 0 ? 292f : 268f, hero ? 7 : 4, 0.07f * H, 0.05f * H, Vector3.down, 0.1f, Pal(_k.Belly));
                Stripes();
            }
        }

        /// <summary>Frost stalker markings: tapered stripes across the back and flanks, the neck and the brow.</summary>
        private void Stripes()
        {
            Vector2 mark = Pal(_k.Accent);
            CreatureTube tb = _bodyTube;
            float tA = _bodyT[2] + 0.02f, tB = _bodyT[7] - 0.02f;
            int n = Segs(9, 6, 0);
            float H = _H;
            for (int i = 0; i < n; i++)
            {
                float tc = Mathf.Lerp(tA, tB, (i + Rand(0.3f, 0.7f)) / n);
                float reach = Rand(55f, 80f);
                float wMax = Rand(0.028f, 0.042f) * H;
                for (int s = -1; s <= 1; s += 2)
                {
                    float sk = Rand(-0.012f, 0.012f);
                    // Each stripe starts near the spine and sweeps down the flank, curving forward, tapering to a point.
                    EmitSurfaceStrip(tb, u => new Vector2(tc + sk * u + 0.018f * Mathf.Sin(u * Mathf.PI), s > 0 ? 86f - u * reach : 94f + u * reach),
                        u => wMax * Mathf.Sin(Mathf.Clamp01(u * 1.15f) * Mathf.PI) * (1f - 0.35f * u), 8, mark, 0.0015f * H);
                }
            }
            // Neck bands and two short marks over the brow.
            for (int i = 0; i < Segs(4, 3, 0); i++)
            {
                float tc = Mathf.Lerp(0.3f, 0.75f, (i + 0.5f) / 4f);
                for (int s = -1; s <= 1; s += 2)
                    EmitSurfaceStrip(_neckTube, u => new Vector2(tc + 0.02f * u, s > 0 ? 88f - u * 70f : 92f + u * 70f), u => 0.03f * H * Mathf.Sin(u * Mathf.PI), 6, mark, 0.0015f * H);
            }
            for (int s = -1; s <= 1; s += 2)
                EmitSurfaceStrip(_headTube, u => new Vector2(0.2f + 0.22f * u, 90f + s * (14f + 6f * u)), u => 0.022f * _headLen * Mathf.Sin(u * Mathf.PI), 6, mark, 0.001f * _headLen);
        }

        private void ShoulderPlates()
        {
            CreatureTube tb = _bodyTube;
            Vector2 crystal = Pal(_k.Crystal);
            float tW = _bodyT[7];
            for (int s = -1; s <= 1; s += 2)
            {
                // Overlapping icy slabs over each shoulder blade, flat and faceted, raked back like folded wings.
                int count = Segs(5, 4, 3);
                for (int i = 0; i < count; i++)
                {
                    float t = tW + 0.025f - i * 0.03f;
                    float th = s > 0 ? 50f - i * 7f : 130f + i * 7f;
                    Vector3 p, n;
                    tb.Frame(t, th, out p, out n);
                    Vector3 back = Nrm(tb.Center(t - 0.02f) - tb.Center(t + 0.02f));
                    float len = (0.44f - i * 0.05f) * _H * Rand(0.9f, 1.1f);
                    EmitCrystal(p - n * (0.03f * _H), Nrm(n * 0.6f + back * 0.85f), n, len, 0.42f * len, 5, 0.42f, crystal, tb.WeightAt(t, Wrap360(th)), 0.1f, 0.4f);
                }
            }
        }

        /// <summary>
        /// Armoured back (basilisk, drake): rows of low hexagonal crystal plates either side of the spine, a crest of
        /// taller crystals down the middle, and for the drake a second, smaller row of plates along the flanks.
        /// </summary>
        private void BackArmour()
        {
            CreatureTube tb = _bodyTube;
            Vector2 crystal = Pal(_k.Crystal);
            int nT = _bodyTrunkCount;
            float t0 = _bodyT[1] + 0.01f, t1 = _bodyT[nT - 2];
            int rows = Segs(9, 6, 4);
            for (int i = 0; i < rows; i++)
            {
                float u = (i + 0.5f) / rows;
                float t = Mathf.Lerp(t0, t1, u);
                float mid = Mathf.Sin(Mathf.Lerp(0.2f, 0.95f, u) * Mathf.PI);
                Vector3 along = Nrm(tb.Center(t + 0.01f) - tb.Center(t - 0.01f));
                for (int c = -1; c <= 1; c += 2)
                {
                    float th = 90f + c * 30f;
                    Vector3 p, n;
                    tb.Frame(t, th, out p, out n);
                    float len = (0.075f + 0.04f * mid) * _H * Rand(0.9f, 1.1f);
                    // A low plate: short, wide and flattened against the hide, leaning back.
                    EmitCrystal(p - n * (0.4f * len), Nrm(n + -along * 0.55f), along, len, 0.55f * len, 6, 0.5f, crystal, tb.WeightAt(t, Wrap360(th)), 0.08f, 0.45f);
                    if (_k.Plates == 3 && _detail > 0)
                    {
                        float th2 = 90f + c * 58f;
                        tb.Frame(t, th2, out p, out n);
                        EmitCrystal(p - n * (0.35f * len), Nrm(n - along * 0.6f), along, len * 0.7f, 0.5f * len * 0.7f, 5, 0.5f, crystal, tb.WeightAt(t, Wrap360(th2)), 0.1f, 0.45f);
                    }
                }
            }
            if (_k.HeadStyle == 2) SpineRow(tb, t1, t0, Mathf.Max(3, _k.Spines + 3), 0.16f * _H, 0.55f, false, crystal);
        }

        private void WeakCrystal()
        {
            int hs = _k.HeadStyle;
            Vector2 glow = Pal(_k.Glow), crystal = Pal(_k.Crystal);
            if (hs == 2 || hs == 3 || hs == 4)
            {
                // Basilisk crown gem, dragon and griffin brow gem.
                Vector3 p, n;
                float t = hs == 2 ? 0.16f : (hs == 4 ? 0.42f : 0.3f);
                _headTube.Frame(t, 90f, out p, out n);
                float r = (hs == 2 ? 0.045f : 0.032f) * _headLen;
                EmitCrystal(p - n * (r * 0.6f), Nrm(n + _headFwd * 0.25f), _headFwd, r * 2.4f, r, 6, 0.45f, glow, W1(_head), 0.06f);
                Hit(_head, p + n * (r * 0.4f), p + n * (r * 0.4f), r * 1.6f, true);
                if (hs == 2) Crown();
                else if (hs == 3) Horns();
                return;
            }
            // Jackal and stalker: a heart-stone between the shoulder blades, a small burning gem ringed by crystal shards.
            float tn = _bodyT[7] + 0.012f;
            Vector3 pp, nn;
            _bodyTube.Frame(tn, 90f, out pp, out nn);
            float rr = 0.03f * _H;
            BoneWeight bw = _bodyTube.WeightAt(tn, 90f);
            EmitCrystal(pp - nn * (rr * 0.6f), Nrm(nn + Vector3.forward * 0.2f), Vector3.right, rr * 2.4f, rr, 6, 0.45f, glow, bw, 0.06f);
            for (int i = 0; i < 4; i++)
            {
                Vector3 off = QMath.AngleAxis(i * 90f + 45f, nn) * Vector3.forward;
                EmitCrystal(pp + off * (rr * 1.1f) - nn * (rr * 0.5f), Nrm(nn + off * 0.55f), off, rr * Rand(1.6f, 2.2f), rr * 0.45f, 5, 0.45f, crystal, bw);
            }
            Hit(bw.boneIndex0, pp + nn * (rr * 0.8f), pp + nn * (rr * 0.8f), rr * 2.2f, true);
        }

        /// <summary>Basilisk: a crown of crystal horns around the back of the skull.</summary>
        private void Crown()
        {
            Vector2 crystal = Pal(_k.Crystal);
            int n = Segs(9, 7, 5);
            for (int i = 0; i < n; i++)
            {
                float u = (float)i / (n - 1);
                float th = Mathf.Lerp(-10f, 190f, u);
                float t = 0.1f + 0.06f * Mathf.Sin(u * Mathf.PI);
                Vector3 p, nr;
                _headTube.Frame(t, Wrap360(th), out p, out nr);
                float len = _hw * (0.9f + 0.9f * Mathf.Sin(u * Mathf.PI)) * Rand(0.85f, 1.15f);
                Vector3 dir = Nrm(nr * 0.85f - _headFwd * 0.7f + _headUp * 0.35f);
                EmitCrystal(p - nr * (0.06f * _hw), dir, _headFwd, len, 0.14f * len + 0.025f * _hw, 5, 0.4f, crystal, W1(_head));
            }
        }

        /// <summary>Dragon horns: swept back from the brow, with smaller crystal points along the jaw line.</summary>
        private void Horns()
        {
            Vector2 crystal = Pal(_k.Crystal), horn = Pal(new Vector2Int(14, 9));
            float hl = _headLen;
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 b = _headTube.Point(0.24f, s > 0 ? 62f : 118f, -0.04f * _hw);
                if (_k.Horns == 3)
                {
                    // Ram curls.
                    Vector3 c0 = b, c1 = b - _headFwd * (0.35f * hl) + Vector3.right * (s * 0.2f * hl) + _headUp * (0.1f * hl);
                    Vector3 c2 = c1 - _headUp * (0.3f * hl) + Vector3.right * (s * 0.1f * hl);
                    Vector3 c3 = c2 + _headFwd * (0.18f * hl) + Vector3.right * (s * 0.05f * hl);
                    float r = 0.16f * _hw * 1.6f;
                    float[] keys = { 0f, r, r, r, 2.2f, 0.5f, r * 0.75f, r * 0.75f, r * 0.75f, 2.2f, 0.9f, r * 0.3f, r * 0.3f, r * 0.3f, 2f, 1f, r * 0.08f, r * 0.08f, r * 0.08f, 2f };
                    CreatureTube ht = new CreatureTube(new[] { c0, c1, c2, c3 }, _headUp, keys);
                    int head = _head;
                    ht.Weight = (t, th) => W1(head);
                    EmitTube(ht, RingsEased(0f, 1f, Segs(14, 8, 6), 0.3f), Segs(10, 7, 6), (t, th) => t > 0.55f ? crystal : horn, null, true, true);
                }
                else
                {
                    Vector3 tip = b - _headFwd * (0.62f * hl) + _headUp * (0.22f * hl) + Vector3.right * (s * 0.12f * hl);
                    EmitHorn(b, tip, _headUp, 0.06f * hl, 0.15f * _hw * 1.5f, Segs(10, 6, 5), Segs(10, 7, 6), crystal, W1(_head), W1(_head));
                    if (_detail > 0)
                    {
                        Vector3 b2 = _headTube.Point(0.3f, s > 0 ? 20f : 160f, -0.03f * _hw);
                        EmitHorn(b2, b2 - _headFwd * (0.32f * hl) + Vector3.right * (s * 0.1f * hl) + _headUp * (0.05f * hl), _headUp, 0.03f * hl, 0.08f * _hw * 1.5f, 6, 8, crystal, W1(_head), W1(_head));
                    }
                }
            }
            // Cheek spikes along the jaw line.
            if (_detail > 0)
                for (int s = -1; s <= 1; s += 2)
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 p, n;
                        _headTube.Frame(0.3f + i * 0.08f, s > 0 ? 335f : 205f, out p, out n);
                        EmitCrystal(p - n * (0.02f * _hw), Nrm(n - _headFwd * 0.8f), _headFwd, (0.35f - i * 0.08f) * _hw, 0.07f * _hw, 4, 0.5f, crystal, W1(_head));
                    }
        }

        // ------------------------------------------------------------------ hit volumes

        private void QuadHits()
        {
            int nT = _bodyTrunkCount;
            CreatureTube tb = _bodyTube;
            // Trunk: two capsules along the body tube's centre line, sized by its sections.
            Vector3 rump = tb.Center(_bodyT[1]), mid = tb.Center(_bodyT[4]), front = tb.Center(_bodyT[nT - 2]);
            float rRear = Mathf.Max(tb.RadiusAt(_bodyT[2], 0f), tb.RadiusAt(_bodyT[2], 90f) * 0.9f);
            float rFront = Mathf.Max(tb.RadiusAt(_bodyT[7], 0f), tb.RadiusAt(_bodyT[7], 90f) * 0.85f);
            Hit(_pelvis, rump, mid, rRear);
            Hit(_chest, mid, front, rFront);
            if (_neckBones.Length > 0 && _neckTube != null) Hit(_neckBones[0], _neckTube.Center(0.25f), _neckTube.Center(0.85f), _neckTube.RadiusAt(0.55f, 90f) * 1.05f);
            Hit(_head, _occiput + _headFwd * (0.12f * _headLen), _nose - _headFwd * (0.1f * _headLen), 0.16f * _headLen);
            foreach (LegPlan lp in _legs)
            {
                float lr = _H * (lp.Front ? 0.07f : 0.1f) * (_k.HeadStyle == 2 ? 1.3f : 1f) * Mathf.Lerp(1f, _k.Bulk, 0.5f);
                Hit(lp.Upper, lp.Hip, lp.Knee, lr);
                Hit(lp.Lower, lp.Knee, lp.Ankle, lr * 0.6f);
                Hit(lp.Foot, lp.Ankle, lp.Paw, lr * 0.45f);
            }
            if (_tailBones.Length > 1) Hit(_tailBones[0], _tailPts[0], _tailPts[Mathf.Min(2, _tailPts.Length - 1)], Mathf.Max(0.02f, _tailTube != null ? _tailTube.RadiusAt(0.25f, 0f) : _bodyTube.RadiusAt(_tailJoinT * 0.8f, 0f)));
        }
    }
}
