using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Paired-talon offhand performance using the established IK skeleton and existing attack timing.
    /// The attacking hand keeps its authored strike. The other talon guards the torso then follows the recoil.</summary>
    public sealed partial class HumanoidPose
    {
        private void PairedBladeMotion04F(ref PoseInput p, ref Body b)
        {
            if (!p.PairedBlades || p.Hold != HoldType.Melee || !p.Blade || p.TwoHanded ||
                p.Ride != RidePose.None || p.Sit > 0.1f || p.TravelBlend > 0.2f || p.Cover > 0.3f) return;
            bool attack = p.AttackT >= 0f;
            bool opposite = attack && p.AttackMirror;
            int side = opposite ? 1 : -1;
            float sign = side;
            Quaternion chest = ChestRot(ref b);
            Vector3 centre = ChestPos(ref b);
            float ready = Mathf.Max(_ready, _guard);
            Vector3 hand = centre + chest * S(sign * 0.18f, -0.2f, 0.15f + ready * 0.13f);
            Vector3 blade = chest * new Vector3(-sign * 0.16f, Mathf.Lerp(-0.5f, 0.55f, ready), 0.8f);
            if (_guard > 0.01f)
            {
                hand = Vector3.Lerp(hand, centre + chest * S(sign * 0.11f, -0.07f, 0.31f), _guard);
                blade = Vector3.Lerp(blade, chest * new Vector3(-sign * 0.64f, 0.72f, 0.3f), _guard);
            }
            if (attack)
            {
                float wind = Mathf.Clamp(p.AttackWindup, 0.05f, 0.9f);
                float hit = Mathf.Clamp(p.AttackActive, 0.02f, 0.5f);
                float strike = SS(wind - 0.08f, wind + hit, p.AttackT);
                float recoil = 1f - SS(wind + hit, 1f, p.AttackT);
                float sweep = strike * recoil;
                hand += chest * S(-sign * 0.11f * sweep, 0.06f * sweep, 0.12f * sweep);
                blade = Vector3.Lerp(blade, chest * new Vector3(-sign * 0.7f, 0.22f, 0.68f), sweep);
            }
            Arm offhand = IkArm(ref b, side, hand, chest * new Vector3(sign * 0.75f, -0.65f, -0.2f),
                Nrm(blade), chest * Vector3.up, 1f);
            if (opposite) b.AR = offhand;
            else b.AL = offhand;
        }
    }
}
