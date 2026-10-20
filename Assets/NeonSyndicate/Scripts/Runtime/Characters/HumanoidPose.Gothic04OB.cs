using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class HumanoidPose
    {
        private void GroundedRecovery04OB(ref PoseInput p,ref Body b)
        {
            if(_creature || !p.Grounded || p.Ride!=RidePose.None || p.Sit>.01f || p.TravelBlend>.01f || p.Cover>.01f ||
                p.Aiming || p.AttackT>=0f || p.DodgeT>=0f || p.StaggerT>=0f || p.SwingT>=0f || p.ThrowT>=0f || p.ParryT>=0f || p.CrystalCast04G>.01f)return;
            // Weapon-ready head lead and shoulder breathing settle before the original hand and foot IK.
            if(p.Ready>.1f && p.Guard<.1f)
            {
                float quiet=1-Mathf.Clamp01(_vel.magnitude/2.5f),breath=Mathf.Sin(_idleT*1.8f)*quiet;
                float lead=Mathf.Clamp(_yawRate*.028f,-3f,3f)*Mathf.Clamp01(p.Ready);
                b.Chest*=QMath.AX(breath*.6f)*QMath.AY(-lead*.35f);
                b.Neck*=QMath.AX(-breath*.35f)*QMath.AY(lead);
            }
            // Landing compression and low-ready torso settle through the existing foot plants and weapon-hand IK.
            float weight=Mathf.Clamp01(_land);b.Chest*=QMath.AX(weight*4f);b.Neck*=QMath.AX(-weight*2.5f);
            if(p.Hold==HoldType.Melee && p.Ready>.1f)
            {
                float brace=Mathf.Clamp01(p.Ready)*(1f-Mathf.Clamp01(_vel.magnitude/3f));
                b.Chest*=QMath.AY(-2.5f*brace);b.Head*=QMath.AY(2f*brace);
                if(p.Blade && p.Guard<.1f)
                {b.Chest*=QMath.AZ(-1.7f*brace);b.Neck*=QMath.AZ(1.4f*brace);}
            }
        }
    }
}
