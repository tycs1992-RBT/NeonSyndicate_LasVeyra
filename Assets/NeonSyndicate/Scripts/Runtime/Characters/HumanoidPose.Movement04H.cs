using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class HumanoidPose
    {
        /// <summary>Upper-body braking and turn anticipation; suppressed for actions that already own the body.</summary>
        private void Anticipation04H(ref PoseInput p, ref Body b)
        {
            if (!p.Grounded || p.AttackT>=0f || p.DodgeT>=0f || p.StaggerT>=0f || p.SwingT>=0f ||
                p.Sit>.1f || p.Ride!=RidePose.None || p.TravelBlend>.1f || p.Cover>.1f) return;
            float quiet=1f-Mathf.Max(_guard,Mathf.Clamp01(p.CrystalCast04G));
            float brake=Mathf.Clamp(-_fwdAcc,-4f,8f)*quiet;
            float turn=Mathf.Clamp(_yawRate*.018f,-6f,6f)*quiet;
            b.Spine=b.Spine*QMath.AX(-brake*.45f);
            b.Chest=b.Chest*QMath.AY(turn*.45f)*QMath.AX(-brake*.3f);
            b.Neck=b.Neck*QMath.AY(turn*.3f);
            b.Head=b.Head*QMath.AY(turn*.25f);
        }
    }
}
