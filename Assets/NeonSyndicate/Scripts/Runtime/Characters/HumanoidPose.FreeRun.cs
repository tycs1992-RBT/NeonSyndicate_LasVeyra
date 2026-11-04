using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Free-running poses (TravelPose values 8 and up, chosen by PlayerController.FreeRun.*.cs): the hand-over-hand
    /// wall climb with its limbs alternating along the climb, the ledge hang with dangling legs and a hand-over-hand
    /// shimmy, the wall run tilted into the wall, the landing roll (the dodge roll's authored tuck), and the head-first
    /// dive that sweeps from a swan dive into an arrow and flips feet-first into the landing crouch. Plain math like
    /// the rest of the solver, so the offline tests can run it. The wall is always straight ahead of the root (+z).
    /// </summary>
    public sealed partial class HumanoidPose
    {
        /// <summary>Poses the body for one free-running or hero TravelPose; false when the value is not one of them.</summary>
        private bool FreeRunPose(TravelPose pose, ref PoseInput p, ref Body t)
        {
            switch (pose)
            {
                case TravelPose.WallClimb: WallClimbPose(ref p, ref t); return true;
                case TravelPose.LedgeHang: LedgeHangPose(ref p, ref t); return true;
                case TravelPose.WallRun: WallRunPose(ref p, ref t); return true;
                case TravelPose.LandRoll: LandRollPose(ref p, ref t); return true;
                case TravelPose.Dive: DivePose(ref p, ref t); return true;
                case TravelPose.Takedown: TakedownPose(ref p, ref t); return true;
                default: return HeroPose(pose, ref p, ref t);
            }
        }

        /// <summary>A wall-side limb cycle: 0..1 along the stride for a cycle this long in metres.</summary>
        private static float Cycle(float metres, float cycleLength)
        {
            float u = metres / Mathf.Max(0.1f, cycleLength);
            return u - Mathf.Floor(u);
        }

        // ---- wall climb: half crouched against the wall, hands reaching up in turn, feet on the stone
        private void WallClimbPose(ref PoseInput p, ref Body t)
        {
            float s = _s;
            float up = Mathf.Clamp(p.TravelLean.x, -1f, 1f);
            float side = Mathf.Clamp(p.TravelLean.y, -1f, 1f);
            float ph = Cycle(p.TravelLean.z, 0.9f);
            float c = Mathf.Cos(ph * Mathf.PI * 2f);
            float effort = Mathf.Abs(up) + Mathf.Abs(side) * 0.6f;
            t.Pelvis = new Vector3(0f, _h0 - 0.2f * s, -0.07f * s);
            t.Hips = QMath.AX(8f);
            t.Spine = QMath.AX(12f + 6f * up);
            t.Chest = QMath.AX(4f);
            t.Neck = QMath.AX(-14f - 8f * Mathf.Max(0f, up));
            t.Head = QMath.AX(-18f - 10f * Mathf.Max(0f, up));
            t.LL.Lift = t.LR.Lift = 0f;
            t.Plant = 0f;
            t.Urgency = 1f;
            // Hands: high on the wall, one reaching above the other as the climb goes.
            float baseY = 0.86f + 0.1f * Mathf.Max(0f, up);
            float amp = (0.06f + 0.12f * effort);
            Vector3 handL = t.Pelvis + S(-0.3f + side * 0.1f, baseY + amp * c, 0.38f);
            Vector3 handR = t.Pelvis + S(0.3f + side * 0.1f, baseY - amp * c, 0.38f);
            t.AL = IkArm(ref t, -1, handL, new Vector3(-0.9f, -0.1f, -0.4f));
            t.AR = IkArm(ref t, 1, handR, new Vector3(0.9f, -0.1f, -0.4f));
            t.AL.Wrist = QMath.AX(-70f);
            t.AR.Wrist = QMath.AX(-70f);
            // Feet: toes on the wall, one stepping up while the other pushes.
            float footAmp = 0.05f + 0.13f * effort;
            t.LL.Ankle = t.Pelvis + S(-0.17f + side * 0.08f, -0.52f - footAmp * c, 0.3f);
            t.LR.Ankle = t.Pelvis + S(0.17f + side * 0.08f, -0.52f + footAmp * c, 0.3f);
            t.LL.KneeDir = Nrm(new Vector3(-0.55f, 0.45f, 0.7f));
            t.LR.KneeDir = Nrm(new Vector3(0.55f, 0.45f, 0.7f));
            t.LL.Foot = QMath.AY(-6f) * QMath.AX(48f);
            t.LR.Foot = QMath.AY(6f) * QMath.AX(48f);
        }

        // ---- ledge hang: stretched from the hands on the edge, legs dangling, a hand-over-hand shimmy
        private void LedgeHangPose(ref PoseInput p, ref Body t)
        {
            float s = _s;
            float side = Mathf.Clamp(p.TravelLean.y, -1f, 1f);
            float sway = Mathf.Sin(_idleT * 1.4f);
            float shimmy = Mathf.Abs(side) * Mathf.Sin(_idleT * 9f);
            t.Pelvis = new Vector3(0.015f * s * sway + side * 0.03f * s, _h0 - 0.02f * s, -0.1f * s);
            t.Hips = QMath.AX(-4f);
            t.Spine = QMath.AX(-6f);
            t.Chest = QMath.AX(-4f);
            t.Neck = QMath.AX(-16f);
            t.Head = QMath.AX(-22f);
            t.LL.Lift = t.LR.Lift = 0f;
            t.Plant = 0f;
            t.Urgency = 0.5f;
            float handY = FreeRunRules.HangDrop - 0.03f;
            Vector3 handL = S(-0.26f + side * 0.1f + 0.05f * shimmy, handY / s, 0.3f);
            Vector3 handR = S(0.26f + side * 0.1f - 0.05f * shimmy, handY / s, 0.3f);
            t.AL = IkArm(ref t, -1, handL, new Vector3(-0.8f, 0.2f, -0.5f));
            t.AR = IkArm(ref t, 1, handR, new Vector3(0.8f, 0.2f, -0.5f));
            t.AL.Wrist = QMath.AX(-80f);
            t.AR.Wrist = QMath.AX(-80f);
            t.LL.Ankle = t.Pelvis + S(-0.1f, -0.8f, 0.06f + 0.03f * sway);
            t.LR.Ankle = t.Pelvis + S(0.1f, -0.79f, 0.02f - 0.03f * sway);
            t.LL.KneeDir = t.LR.KneeDir = Nrm(new Vector3(0f, 0.1f, 1f));
            t.LL.Foot = QMath.AY(-5f) * QMath.AX(40f);
            t.LR.Foot = QMath.AY(5f) * QMath.AX(40f);
        }

        // ---- wall run: a sprint stride tilted into the wall, the inner foot kicking off the stone
        private void WallRunPose(ref PoseInput p, ref Body t)
        {
            float s = _s;
            float k = Mathf.Clamp01(p.TravelLean.x);
            float ph = Cycle(p.TravelLean.y, 1.5f);
            float side = Mathf.Clamp(p.TravelLean.z, -1f, 1f);
            float a = ph * Mathf.PI * 2f;
            float sinA = Mathf.Sin(a);
            float cosA = Mathf.Cos(a);
            AirBody(ref t, _h0 - 0.08f * s, 10f, 6f, -8f, new Vector3(-0.14f, -0.75f + 0.22f * Mathf.Max(0f, sinA), 0.3f * cosA), new Vector3(0.14f, -0.75f + 0.22f * Mathf.Max(0f, -sinA), -0.3f * cosA), 25f);
            t.LL.KneeDir = Nrm(new Vector3(-0.2f + side * 0.3f, 0.3f, 1f));
            t.LR.KneeDir = Nrm(new Vector3(0.2f + side * 0.3f, 0.3f, 1f));
            // Arms pump against the legs; the wall-side arm stays a little lower, ready to push off.
            float pump = 40f * (0.5f + 0.5f * k);
            t.AL = FkArm(ref t, -1, 20f - pump * cosA, 12f + (side < 0f ? 10f : 0f), 70f, 0f, QMath.AX(-10f));
            t.AR = FkArm(ref t, 1, 20f + pump * cosA, 12f + (side > 0f ? 10f : 0f), 70f, 0f, QMath.AX(-10f));
            t.Urgency = 1f;
            RotateBody(ref t, QMath.AZ(-side * 22f) * QMath.AX(14f + 6f * k), t.Pelvis);
        }

        // ---- landing roll: the dodge roll's tuck, entered already diving (the feet left the ground long ago)
        private void LandRollPose(ref PoseInput p, ref Body t)
        {
            float ph = Mathf.Clamp01(p.TravelLean.x);
            Roll(ref t, 0.1f + 0.9f * ph, Vector3.forward);
        }

        // ---- takedown: a low step in (or a compact drop from above), both hands to the neck, a sharp pull and twist, release
        private void TakedownPose(ref PoseInput p, ref Body t)
        {
            float s = _s;
            float ph = Mathf.Clamp01(p.TravelLean.x);
            float above = Mathf.Clamp01(p.TravelLean.y);
            float approach = 1f - SS(0.2f, 0.34f, ph);
            float grab = SS(0.26f, 0.42f, ph) * (1f - SS(0.78f, 1f, ph));
            float pull = SS(0.42f, 0.6f, ph) * (1f - SS(0.72f, 0.95f, ph));
            Body a = t;
            // The step in: low and leaning forward, arms coming up; from above a compact drop with the knees drawn up.
            float crouch = Mathf.Lerp(0.14f, 0.32f, approach) + 0.1f * grab;
            a.Pelvis = new Vector3(0f, _h0 - crouch * s, -0.04f * s);
            a.Hips = QMath.AY(-14f * pull) * QMath.AX(8f + 10f * approach);
            a.Spine = QMath.AY(-12f * pull) * QMath.AX(16f + 8f * grab - 10f * pull);
            a.Chest = QMath.AX(6f);
            a.Neck = QMath.AX(-8f);
            a.Head = QMath.AX(-10f);
            Vector3 ankle = new Vector3(0f, _ankleH, 0f);
            if (above * approach > 0.01f)
            {
                a.LL.Ankle = a.Pelvis + S(-0.12f, -0.48f, 0.18f);
                a.LR.Ankle = a.Pelvis + S(0.12f, -0.5f, 0.14f);
                a.LL.KneeDir = a.LR.KneeDir = Nrm(new Vector3(0f, 0.5f, 1f));
                a.Plant = 0f;
            }
            else
            {
                a.LL.Ankle = S(-0.14f, 0f, 0.26f - 0.1f * pull) + ankle;
                a.LR.Ankle = S(0.15f, 0f, -0.22f) + ankle;
                a.LL.KneeDir = QMath.AY(-8f) * Vector3.forward;
                a.LR.KneeDir = QMath.AY(10f) * Vector3.forward;
                a.Plant = 1f;
            }
            a.LL.Foot = QMath.AY(-8f);
            a.LR.Foot = QMath.AY(10f);
            a.LL.Lift = a.LR.Lift = 0f;
            // Hands: reaching forward to the neck for the grab, pulled back to the chest with the twist, then lowered.
            Vector3 reachL = S(-0.14f, 1.38f, 0.5f), reachR = S(0.16f, 1.42f, 0.46f);
            Vector3 pullL = S(-0.2f, 1.2f, 0.16f), pullR = S(0.14f, 1.3f, 0.1f);
            Vector3 handL = Vector3.Lerp(Vector3.Lerp(S(-0.22f, 0.8f, 0.2f), reachL, Mathf.Max(grab, 1f - approach)), pullL, pull);
            Vector3 handR = Vector3.Lerp(Vector3.Lerp(S(0.22f, 0.78f, 0.22f), reachR, Mathf.Max(grab, 1f - approach)), pullR, pull);
            a.AL = IkArm(ref a, -1, handL, new Vector3(-0.8f, -0.3f, -0.3f));
            a.AR = IkArm(ref a, 1, handR, new Vector3(0.8f, -0.1f, -0.4f));
            a.Urgency = 1f;
            float w = Mathf.Min(SS(0f, 0.06f, ph), 1f - SS(0.9f, 1f, ph));
            Blend(ref t, ref a, w);
        }

        // ---- dive: swan dive into an arrow, flipping feet-first into the landing crouch at the end
        private void DivePose(ref PoseInput p, ref Body t)
        {
            float pitch = Mathf.Clamp(p.TravelLean.x, 60f, 160f);
            float flip = Mathf.Clamp01(p.TravelLean.y);
            float slam = Mathf.Clamp01(p.TravelLean.z);
            float arrow = SS(100f, 140f, pitch);
            Body d = t;
            AirBody(ref d, _h0, -6f, -4f, -10f + 8f * arrow, new Vector3(-0.07f, -0.86f, -0.03f), new Vector3(0.07f, -0.86f, -0.05f), 60f);
            // Arms: spread wide for the swan, sweeping forward together as the dive gathers speed.
            float swing = Mathf.Lerp(-25f, 172f, arrow);
            float abduct = Mathf.Lerp(82f, 8f, arrow);
            d.AL = FkArm(ref d, -1, swing, abduct, 6f, 0f, QMath.AX(-8f));
            d.AR = FkArm(ref d, 1, swing, abduct, 6f, 0f, QMath.AX(-8f));
            float bodyPitch = pitch;
            if (slam > 0f)
            {
                // The slam: less head-down, the right fist leading straight at the ground, knees drawn up.
                Body sl = d;
                sl.AR = FkArm(ref sl, 1, 150f, 6f, 4f, 0f, QMath.AX(-8f));
                sl.AL = FkArm(ref sl, -1, -40f, 30f, 25f, 0f, QMath.AX(-8f));
                sl.LL.Ankle = sl.Pelvis + S(-0.1f, -0.5f, 0.2f);
                sl.LR.Ankle = sl.Pelvis + S(0.1f, -0.55f, 0.15f);
                sl.LL.KneeDir = sl.LR.KneeDir = Nrm(new Vector3(0f, 0.6f, 1f));
                Blend(ref d, ref sl, slam);
                bodyPitch = Mathf.Lerp(pitch, 75f, slam);
            }
            RotateBody(ref d, QMath.AX(bodyPitch), d.Pelvis);
            // Near the ground the dive flips upright into the landing crouch (the heroic landing).
            if (flip > 0.001f)
            {
                Body land = t;
                LandPose(ref land);
                Blend(ref d, ref land, E3(flip));
            }
            t = d;
        }
    }
}
