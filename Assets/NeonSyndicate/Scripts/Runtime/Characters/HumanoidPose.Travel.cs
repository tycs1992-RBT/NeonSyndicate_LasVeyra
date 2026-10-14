using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Travel power poses (PoseInput.Travel with TravelBlend): super speed runs through the gait itself (a deep lean,
    /// hard arm pump, high knees and a stylised fast cadence); a leap crouches, springs up fully extended and tucks at
    /// the apex; gliding spreads the arms with the legs trailing; flight tilts the body into the direction of travel
    /// (TravelLean x pitch, z bank), flat out with one fist forward; hovering is upright with a gentle drift; a heavy
    /// landing sinks into a deep crouch with a hand on the ground and rises as the blend fades. Every pose cross-fades.
    /// </summary>
    public sealed partial class HumanoidPose
    {
        private readonly float[] _travelW = new float[8];

        /// <summary>Eases each travel pose's weight toward the request (before the gait, which reads the Surge weight).</summary>
        private void TravelWeights(ref PoseInput p, float dt)
        {
            float blend = Mathf.Clamp01(p.TravelBlend);
            for (int k = 1; k < _travelW.Length; k++)
            {
                float target = (int)p.Travel == k ? blend : 0f;
                _travelW[k] = Mathf.MoveTowards(_travelW[k], target, dt * 6f);
            }
            _surge = _travelW[(int)TravelPose.Surge];
        }

        /// <summary>Blends the active travel poses over the on-foot pose.</summary>
        private void Travel(ref PoseInput p, ref Body b)
        {
            for (int k = (int)TravelPose.Leap; k < _travelW.Length; k++)
            {
                float w = _travelW[k];
                if (w <= 0.001f) continue;
                Body t = b;
                switch ((TravelPose)k)
                {
                    case TravelPose.Leap: LeapPose(ref p, ref t); break;
                    case TravelPose.Glide: GlidePose(ref p, ref t); break;
                    case TravelPose.Fly: FlyPose(ref p, ref t, false); break;
                    case TravelPose.FlyFast: FlyPose(ref p, ref t, true); break;
                    case TravelPose.Hover: HoverPose(ref p, ref t); break;
                    default: LandPose(ref t); break;
                }
                Blend(ref b, ref t, E3(w));
            }
        }

        /// <summary>Turns a whole posed body about a pivot (the pelvis for flight poses).</summary>
        private static void RotateBody(ref Body t, Quaternion r, Vector3 pivot)
        {
            t.Pelvis = pivot + r * (t.Pelvis - pivot);
            t.Hips = r * t.Hips;
            t.AL = RotateArm(t.AL, r, pivot);
            t.AR = RotateArm(t.AR, r, pivot);
            t.LL = RotateLeg(t.LL, r, pivot);
            t.LR = RotateLeg(t.LR, r, pivot);
        }

        private static Arm RotateArm(Arm a, Quaternion r, Vector3 pivot)
        {
            a.Hand = pivot + r * (a.Hand - pivot);
            a.Elbow = pivot + r * (a.Elbow - pivot);
            a.Blade = r * a.Blade;
            a.Edge = r * a.Edge;
            return a;
        }

        private static Leg RotateLeg(Leg l, Quaternion r, Vector3 pivot)
        {
            l.Ankle = pivot + r * (l.Ankle - pivot);
            l.KneeDir = r * l.KneeDir;
            l.Foot = r * l.Foot;
            return l;
        }

        /// <summary>Upright body hanging from the pelvis: torso angles, legs relative to the pelvis, nothing planted.</summary>
        private void AirBody(ref Body t, float pelvisY, float spine, float chest, float head, Vector3 ankleL, Vector3 ankleR, float toe)
        {
            t.Pelvis = new Vector3(0f, pelvisY, 0f);
            t.Hips = QMath.Identity;
            t.Spine = QMath.AX(spine);
            t.Chest = QMath.AX(chest);
            t.Neck = QMath.AX(head * 0.4f);
            t.Head = QMath.AX(head * 0.6f);
            t.LL.Ankle = t.Pelvis + S(ankleL.x, ankleL.y, ankleL.z);
            t.LR.Ankle = t.Pelvis + S(ankleR.x, ankleR.y, ankleR.z);
            t.LL.KneeDir = t.LR.KneeDir = Vector3.forward;
            t.LL.Foot = QMath.AY(-5f) * QMath.AX(toe);
            t.LR.Foot = QMath.AY(5f) * QMath.AX(toe);
            t.LL.Lift = t.LR.Lift = 0f;
            t.Plant = 0f;
            t.Urgency = 0f;
        }

        private Quaternion LeanRot(Vector3 lean, float pitch)
        {
            return QMath.AZ(-lean.z) * QMath.AX(pitch);
        }

        // ---- leap: crouch, explosive extension, tuck at the apex, legs reaching down on the way down
        private void LeapPose(ref PoseInput p, ref Body t)
        {
            if (p.Grounded)
            {
                t.Pelvis = new Vector3(0f, _h0 - 0.3f * _s, -0.05f * _s);
                t.Hips = QMath.Identity;
                t.Spine = QMath.AX(28f);
                t.Chest = QMath.AX(10f);
                t.Neck = QMath.AX(-12f);
                t.Head = QMath.AX(-18f);
                Vector3 ankle = new Vector3(0f, _ankleH, 0f);
                t.LL.Ankle = S(-0.13f, 0f, 0.04f) + ankle;
                t.LR.Ankle = S(0.13f, 0f, -0.02f) + ankle;
                t.LL.Foot = QMath.AY(-8f);
                t.LR.Foot = QMath.AY(8f);
                t.LL.KneeDir = QMath.AY(-8f) * Vector3.forward;
                t.LR.KneeDir = QMath.AY(8f) * Vector3.forward;
                t.LL.Lift = t.LR.Lift = 0f;
                t.AL = FkArm(ref t, -1, -50f, 14f, 25f, 0f, QMath.AX(-10f));
                t.AR = FkArm(ref t, 1, -50f, 14f, 25f, 0f, QMath.AX(-10f));
                t.Plant = 1f;
                t.Urgency = 1f;
                return;
            }
            float vy = p.VerticalVelocity;
            float tuck = 1f - SS(1.5f, 7f, Mathf.Abs(vy));
            float fall = SS(1.5f, 7f, -vy);
            // Extension: straight body, toes pointed, arms thrown up.
            Body ext = t;
            AirBody(ref ext, _h0, -6f, -4f, 4f, new Vector3(-0.08f, -0.86f, -0.1f), new Vector3(0.08f, -0.86f, -0.08f), 55f);
            ext.AL = FkArm(ref ext, -1, 160f, 22f, 15f, 0f, QMath.AX(-10f));
            ext.AR = FkArm(ref ext, 1, 160f, 22f, 15f, 0f, QMath.AX(-10f));
            // Tuck: knees to the chest, arms round the shins.
            Body tk = t;
            AirBody(ref tk, _h0, 26f, 16f, 10f, new Vector3(-0.1f, -0.36f, 0.12f), new Vector3(0.1f, -0.36f, 0.12f), 30f);
            tk.LL.KneeDir = tk.LR.KneeDir = Nrm(new Vector3(0f, 0.6f, 1f));
            tk.AL = IkArm(ref tk, -1, tk.Pelvis + S(-0.13f, -0.06f, 0.4f), new Vector3(-0.8f, -0.2f, -0.4f));
            tk.AR = IkArm(ref tk, 1, tk.Pelvis + S(0.13f, -0.06f, 0.4f), new Vector3(0.8f, -0.2f, -0.4f));
            // Falling: legs reaching down a little apart, arms out for balance.
            Body fl = t;
            AirBody(ref fl, _h0, 6f, 2f, -6f, new Vector3(-0.12f, -0.8f, 0.06f), new Vector3(0.12f, -0.82f, 0f), 20f);
            fl.AL = FkArm(ref fl, -1, 20f, 58f, 30f, 0f, QMath.AX(-10f));
            fl.AR = FkArm(ref fl, 1, 20f, 58f, 30f, 0f, QMath.AX(-10f));
            Blend(ref ext, ref tk, tuck);
            Blend(ref ext, ref fl, fall);
            t = ext;
        }

        // ---- glide: arms spread wide, legs trailing, body pitched forward and banking
        private void GlidePose(ref PoseInput p, ref Body t)
        {
            float pitch = Mathf.Clamp(Mathf.Max(50f, p.TravelLean.x), 0f, 88f);
            AirBody(ref t, _h0, -8f, -6f, -pitch * 0.55f, new Vector3(-0.17f, -0.84f, -0.06f), new Vector3(0.17f, -0.84f, -0.06f), 50f);
            float sway = Mathf.Sin(_idleT * 1.3f) * 3f;
            t.AL = FkArm(ref t, -1, -15f + sway, 82f, 12f, 0f, QMath.AX(-6f));
            t.AR = FkArm(ref t, 1, -15f - sway, 82f, 12f, 0f, QMath.AX(-6f));
            RotateBody(ref t, LeanRot(p.TravelLean, pitch), t.Pelvis);
        }

        // ---- flight: tilted into the direction of travel; flat out, one fist forward
        private void FlyPose(ref PoseInput p, ref Body t, bool fast)
        {
            float pitch = fast ? Mathf.Clamp(Mathf.Max(80f, p.TravelLean.x), 0f, 95f) : Mathf.Clamp(p.TravelLean.x, -30f, 88f);
            AirBody(ref t, _h0, -4f, -3f, -pitch * (fast ? 0.75f : 0.6f), new Vector3(-0.06f, -0.85f, -0.04f), new Vector3(0.06f, -0.84f, -0.06f), fast ? 60f : 45f);
            if (fast)
            {
                t.AR = FkArm(ref t, 1, 176f, 3f, 4f, 0f, QMath.AX(-8f));
                t.AL = FkArm(ref t, -1, -8f, 7f, 10f, 0f, QMath.AX(-10f));
            }
            else
            {
                float sway = Mathf.Sin(_idleT * 1.2f) * 2.5f;
                t.AL = FkArm(ref t, -1, -12f + sway, 22f, 18f, 0f, QMath.AX(-10f));
                t.AR = FkArm(ref t, 1, -12f - sway, 22f, 18f, 0f, QMath.AX(-10f));
            }
            RotateBody(ref t, LeanRot(p.TravelLean, pitch), t.Pelvis);
        }

        // ---- hover: upright, legs together, toes down, a slow bob and drift
        private void HoverPose(ref PoseInput p, ref Body t)
        {
            float bob = Mathf.Sin(_idleT * 1.6f);
            AirBody(ref t, _h0 + 0.02f * _s * bob, 2f, 1f, -2f, new Vector3(-0.07f, -0.8f, 0.03f), new Vector3(0.07f, -0.8f, 0.01f), 35f);
            t.LL.KneeDir = t.LR.KneeDir = Nrm(new Vector3(0f, 0.15f, 1f));
            t.AL = FkArm(ref t, -1, 8f + 3f * bob, 18f + 3f * Mathf.Sin(_idleT * 1.1f), 20f, 0f, QMath.AX(-10f));
            t.AR = FkArm(ref t, 1, 8f - 3f * bob, 18f + 3f * Mathf.Sin(_idleT * 1.1f + 1f), 20f, 0f, QMath.AX(-10f));
            RotateBody(ref t, QMath.AZ(-p.TravelLean.z * 0.5f) * QMath.AX(p.TravelLean.x * 0.5f), t.Pelvis);
        }

        // ---- heavy landing: deep crouch, one knee forward, the right hand on the ground; rises as the blend fades
        private void LandPose(ref Body t)
        {
            Vector3 ankle = new Vector3(0f, _ankleH, 0f);
            t.Pelvis = new Vector3(0f, _h0 - 0.47f * _s, -0.02f * _s);
            t.Hips = QMath.AY(-10f);
            t.Spine = QMath.AX(32f);
            t.Chest = QMath.AX(14f);
            t.Neck = QMath.AX(-10f);
            t.Head = QMath.AX(-22f);
            t.LL.Ankle = S(-0.16f, 0f, 0.22f) + ankle;
            t.LR.Ankle = S(0.15f, 0f, -0.3f) + ankle;
            t.LL.Foot = QMath.AY(-12f);
            t.LR.Foot = QMath.AY(18f);
            t.LL.KneeDir = QMath.AY(-14f) * Vector3.forward;
            t.LR.KneeDir = QMath.AY(10f) * Vector3.forward;
            t.LL.Lift = t.LR.Lift = 0f;
            t.AR = IkArm(ref t, 1, S(0.24f, 0.07f, 0.34f), new Vector3(0.7f, 0.3f, -0.6f));
            t.AL = FkArm(ref t, -1, 25f, 55f, 25f, 0f, QMath.AX(-10f));
            t.Plant = 1f;
            t.Urgency = 1f;
        }
    }
}
