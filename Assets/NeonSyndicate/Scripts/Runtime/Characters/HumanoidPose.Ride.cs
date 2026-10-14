using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Seated poses. At a car wheel: reclined, feet on the pedals, both hands on the rim turning with Steer. Astride a
    /// hover bike (PoseInput.Ride): on the seat, knees bent and gripping the tank, feet on the pegs, hands on the bars
    /// with a handlebar grip, crouching lower over the bars with speed, leaning with the bike and looking where it steers.
    /// Missing anchors fall back to a typical bike layout around the seat. A one-handed drive-by (AimPost) keeps the
    /// other hand on the wheel or the bar.
    /// </summary>
    public sealed partial class HumanoidPose
    {
        private float _seatW, _rootSpeed;
        private bool _bike;
        private Vector3 _speedRoot;
        private bool _speedInit;

        /// <summary>Seat weight (eased) and the root's own speed (a seated rider gets no velocity from the animator).</summary>
        private void SeatWeights(ref PoseInput p, float dt)
        {
            bool bike = p.Ride == RidePose.Bike;
            float target = bike ? 1f : Mathf.Clamp01(p.Sit);
            if (target > 0.001f) _bike = bike;
            _seatW = Mathf.MoveTowards(_seatW, target, dt * 3.5f);
            Vector3 r = p.RootPosition;
            if (_speedInit)
            {
                Vector3 d = r - _speedRoot;
                d.y = 0f;
                float v = d.magnitude / dt;
                if (v < 80f) _rootSpeed = Mathf.Lerp(_rootSpeed, v, QMath.Damp(4f, dt));
            }
            _speedRoot = r;
            _speedInit = true;
        }

        private bool Has(ref PoseInput p, int bit) { return (p.RideAnchors & bit) != 0; }

        private Vector3 SeatPoint(ref PoseInput p) { return Has(ref p, 1) ? p.SeatPos : Vector3.zero; }

        private Vector3 Handle(ref PoseInput p, int side)
        {
            if (side < 0 && Has(ref p, 2)) return p.HandleL;
            if (side > 0 && Has(ref p, 4)) return p.HandleR;
            return SeatPoint(ref p) + S(0.3f * side, 0.6f, 0.52f);
        }

        private Vector3 Peg(ref PoseInput p, int side)
        {
            if (side < 0 && Has(ref p, 8)) return p.PegL;
            if (side > 0 && Has(ref p, 16)) return p.PegR;
            return SeatPoint(ref p) + S(0.2f * side, -0.36f, 0.16f);
        }

        /// <summary>The seated lower body and torso (before the feet logic, so nothing tries to plant the feet).</summary>
        private void Seat(ref PoseInput p, ref Body b)
        {
            if (_seatW <= 0.001f) return;
            Body t = b;
            Vector3 ankle = new Vector3(0f, _ankleH, 0f);
            if (_bike)
            {
                float lean = Mathf.Clamp(p.RideLean, -60f, 60f);
                float fast = SS(4f, 30f, _rootSpeed);
                float steer = Mathf.Clamp(p.Steer, -1f, 1f);
                t.Pelvis = SeatPoint(ref p) + S(0f, 0.13f, -0.02f);
                t.Hips = QMath.AZ(-lean) * QMath.AX(12f);
                t.Spine = QMath.AX(14f + 12f * fast) * QMath.AZ(-lean * 0.15f);
                t.Chest = QMath.AX(8f + 10f * fast);
                t.Neck = QMath.AX(-10f - 8f * fast) * QMath.AY(steer * 10f);
                t.Head = QMath.AX(-14f - 10f * fast) * QMath.AY(steer * 18f) * QMath.AZ(lean * 0.45f);
                for (int side = -1; side <= 1; side += 2)
                {
                    Leg l;
                    Quaternion foot = QMath.AZ(-lean) * QMath.AY(side * 6f) * QMath.AX(12f);
                    l.Ankle = Peg(ref p, side) - foot * new Vector3(0f, -_ankleH, _ball * 0.75f);
                    l.KneeDir = Nrm(new Vector3(-0.3f * side, 0.2f, 1f));
                    l.Foot = foot;
                    l.Lift = 0f;
                    if (side < 0) t.LL = l;
                    else t.LR = l;
                }
            }
            else
            {
                t.Pelvis = new Vector3(0f, _h0 - 0.48f * _s, 0.1f * _s);
                t.Hips = QMath.AX(-6f);
                t.Spine = QMath.AX(-3f);
                t.Chest = QMath.AX(2f);
                t.Neck = QMath.AX(3f);
                t.Head = QMath.AX(4f) * QMath.AY(Mathf.Clamp(p.Steer, -1f, 1f) * 12f);
                t.LL.Ankle = S(-0.13f, 0.04f, 0.6f) + ankle;
                t.LR.Ankle = S(0.13f, 0.04f, 0.6f) + ankle;
                t.LL.Foot = QMath.AY(-4f) * QMath.AX(-6f);
                t.LR.Foot = QMath.AY(4f) * QMath.AX(-6f);
                t.LL.KneeDir = t.LR.KneeDir = Nrm(new Vector3(0f, 0.7f, 0.7f));
                t.LL.Lift = t.LR.Lift = 0f;
            }
            t.Plant = 0f;
            t.Urgency = 0f;
            Blend(ref b, ref t, QMath.Smooth01(_seatW));
            if (_seatW > 0.5f) b.Plant = 0f;
        }

        /// <summary>Hands on the wheel rim or the handlebars (after smoothing, so they stay exactly on the bars).</summary>
        private void SeatHands(ref PoseInput p, ref Body b)
        {
            if (_seatW <= 0.001f) return;
            float w = QMath.Smooth01(_seatW);
            float hs = HandScale;
            Vector3 handleR = new Vector3(WeaponGrip.HandleX, WeaponGrip.HandleY, 0f) * hs, handleL = WeaponGrip.HandleLeft(hs);
            Quaternion rC = ChestRot(ref b);
            Vector3 gripL, gripR;
            Quaternion rotL, rotR;
            if (_bike)
            {
                gripL = Handle(ref p, -1);
                gripR = Handle(ref p, 1);
                Vector3 bar = Nrm(gripR - gripL);
                if (bar.sqrMagnitude < 0.5f) bar = Vector3.right;
                // Palms down over the grips, fingers forward, thumbs pointing in along the bar.
                Vector3 fwd = Nrm(Vector3.Cross(bar, Vector3.up));
                if (fwd.z < 0f) fwd = -fwd;
                Vector3 up = Nrm(Vector3.Cross(fwd, bar));
                if (up.y < 0f) up = -up;
                rotR = QMath.FromBasis(up, -fwd, -bar);
                rotL = QMath.FromBasis(-up, -fwd, bar);
            }
            else
            {
                // Wheel in front of the chest, tilted back; hands at a quarter to three, turning with Steer.
                Vector3 c = ChestPos(ref b) + rC * S(0f, -0.06f, 0.4f);
                Vector3 n = rC * Nrm(new Vector3(0f, 0.42f, -1f));
                Quaternion turn = QMath.AngleAxis(-Mathf.Clamp(p.Steer, -1f, 1f) * 55f, n);
                Vector3 right = turn * (rC * Vector3.right);
                Vector3 tangent = Nrm(Vector3.Cross(n, right));
                if (tangent.y < 0f) tangent = -tangent;
                gripR = c + right * 0.17f * _s;
                gripL = c - right * 0.17f * _s;
                Vector3 fingers = Nrm(Vector3.Cross(right, tangent));
                if (Vector3.Dot(fingers, rC * Vector3.forward) < 0f) fingers = -fingers;
                rotR = QMath.FromBasis(right, -fingers, tangent);
                rotL = QMath.FromBasis(right, -fingers, tangent);
            }
            Arm l = IkArm(ref b, -1, gripL - rotL * handleL, rC * new Vector3(-0.8f, -0.5f, -0.2f));
            l.Blade = rotL * Vector3.down;
            l.Edge = rotL * Vector3.forward;
            l.HandW = 1f;
            Arm r = IkArm(ref b, 1, gripR - rotR * handleR, rC * new Vector3(0.8f, -0.5f, -0.2f));
            r.Blade = rotR * Vector3.down;
            r.Edge = rotR * Vector3.forward;
            r.HandW = 1f;
            b.AL = Lerp(b.AL, l, w);
            b.AR = Lerp(b.AR, r, w);
            if (w > 0.5f)
            {
                GripL = HandHold.Handlebar;
                GripR = HandHold.Handlebar;
            }
        }
    }
}
