using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Hero power poses (TravelPose Grab, Beam and RushStrike, chosen by PlayerController.Hero.cs and .Grab.cs): the
    /// lift and overhead carry with a two-handed throw, the Shard Gaze stance with both hands thrust forward along the
    /// aim and the body braced, and the rush strike's fists-first lunge. Plain math like the rest of the solver.
    /// </summary>
    public sealed partial class HumanoidPose
    {
        /// <summary>Poses the body for one hero TravelPose; false when the value is not one of them.</summary>
        private bool HeroPose(TravelPose pose, ref PoseInput p, ref Body t)
        {
            switch (pose)
            {
                case TravelPose.Grab: GrabPose(ref p, ref t); return true;
                case TravelPose.Beam: BeamPose(ref p, ref t); return true;
                case TravelPose.RushStrike: RushStrikePose(ref p, ref t); return true;
                default: return false;
            }
        }

        // ---- grab: a crouched two-handed lift, then both arms locked overhead, then the throw forward
        private void GrabPose(ref PoseInput p, ref Body t)
        {
            float s = _s;
            float lift = Mathf.Clamp01(p.TravelLean.x);
            float throwT = Mathf.Clamp01(p.TravelLean.y);
            Body a = t;
            float liftE = E3(lift);
            float heave = SS(0f, 0.5f, throwT) * (1f - SS(0.55f, 1f, throwT));
            float follow = SS(0.3f, 0.8f, throwT);
            // The body: a deep crouch for the lift rising to a braced stand, a lean back under the weight, then a lunge forward for the throw.
            float crouch = Mathf.Lerp(0.3f, 0.04f, liftE) + 0.08f * follow;
            a.Pelvis = new Vector3(0f, _h0 - crouch * s, (-0.04f + 0.08f * follow) * s);
            a.Spine = QMath.AX(Mathf.Lerp(26f, -10f, liftE) + 32f * follow);
            a.Chest = QMath.AX(Mathf.Lerp(8f, -6f, liftE) + 10f * follow);
            a.Neck = QMath.AX(-8f + 6f * follow);
            a.Head = QMath.AX(Mathf.Lerp(-16f, 4f, liftE));
            // Hands: from a grab at chest height in front to the carry point overhead; the throw swings them forward and down.
            Vector3 low = S(0f, 0.95f, 0.5f);
            Vector3 high = S(0f, 2.05f, 0.2f);
            Vector3 thrown = S(0f, 1.3f, 0.75f);
            Vector3 centre = Vector3.Lerp(Vector3.Lerp(low, high, liftE), thrown, Mathf.Max(heave * 0.3f, follow));
            float spread = Mathf.Lerp(0.28f, 0.22f, liftE) * s;
            a.AL = IkArm(ref a, -1, centre + new Vector3(-spread, 0f, 0f), new Vector3(-0.8f, 0.2f, -0.4f));
            a.AR = IkArm(ref a, 1, centre + new Vector3(spread, 0f, 0f), new Vector3(0.8f, 0.2f, -0.4f));
            a.AL.Wrist = QMath.AX(-60f * liftE);
            a.AR.Wrist = QMath.AX(-60f * liftE);
            a.Urgency = 1f;
            Blend(ref t, ref a, 1f);
        }

        // ---- Shard Gaze: braced stance, both hands thrust forward together along the aim
        private void BeamPose(ref PoseInput p, ref Body t)
        {
            float s = _s;
            float charge = Mathf.Clamp01(p.TravelLean.x);
            float pitch = Mathf.Clamp(p.TravelLean.y, -70f, 70f);
            Body a = t;
            a.Pelvis = new Vector3(0f, _h0 - 0.1f * s, -0.03f * s);
            a.Hips = QMath.AY(-18f);
            a.Spine = QMath.AY(12f) * QMath.AX(8f + pitch * 0.12f);
            a.Chest = QMath.AY(8f) * QMath.AX(4f + pitch * 0.1f);
            a.Neck = QMath.AX(pitch * 0.25f);
            a.Head = QMath.AX(pitch * 0.3f);
            Vector3 ankle = new Vector3(0f, _ankleH, 0f);
            a.LL.Ankle = S(-0.2f, 0f, 0.22f) + ankle;
            a.LR.Ankle = S(0.18f, 0f, -0.28f) + ankle;
            a.LL.KneeDir = QMath.AY(-10f) * Vector3.forward;
            a.LR.KneeDir = QMath.AY(24f) * Vector3.forward;
            a.LL.Foot = QMath.AY(-8f);
            a.LR.Foot = QMath.AY(26f);
            a.LL.Lift = a.LR.Lift = 0f;
            a.Plant = 1f;
            // Both hands forward at the chest line, rotated to the aim pitch about the shoulders, pushed out as the beam charges.
            Quaternion aim = QMath.AX(pitch);
            float reach = Mathf.Lerp(0.3f, 0.58f, E3(charge));
            Vector3 handL = S(-0.1f, 1.34f, 0f) + aim * S(0f, 0f, reach);
            Vector3 handR = S(0.1f, 1.34f, 0f) + aim * S(0f, 0f, reach);
            a.AL = IkArm(ref a, -1, handL, new Vector3(-0.7f, -0.5f, -0.3f));
            a.AR = IkArm(ref a, 1, handR, new Vector3(0.7f, -0.5f, -0.3f));
            a.AL.Wrist = QMath.AX(-85f);
            a.AR.Wrist = QMath.AX(-85f);
            a.Urgency = 0.6f;
            Blend(ref t, ref a, 1f);
        }

        // ---- rush strike: a horizontal fists-first lunge, legs trailing, the body stretched along the line
        private void RushStrikePose(ref PoseInput p, ref Body t)
        {
            float ph = Mathf.Clamp01(p.TravelLean.x);
            float stretch = SS(0f, 0.25f, ph) * (1f - SS(0.8f, 1f, ph));
            Body a = t;
            AirBody(ref a, _h0 + 0.05f * _s, -8f, -6f, 6f, new Vector3(-0.1f, -0.84f, -0.1f), new Vector3(0.1f, -0.8f, -0.2f), 55f);
            a.AR = FkArm(ref a, 1, 172f, 4f, 4f, 0f, QMath.AX(-8f));
            a.AL = FkArm(ref a, -1, -30f, 20f, 60f, 0f, QMath.AX(-8f));
            a.Urgency = 1f;
            RotateBody(ref a, QMath.AX(Mathf.Lerp(20f, 82f, stretch)), a.Pelvis);
            Blend(ref t, ref a, Mathf.Max(stretch, 0.4f));
        }
    }
}
