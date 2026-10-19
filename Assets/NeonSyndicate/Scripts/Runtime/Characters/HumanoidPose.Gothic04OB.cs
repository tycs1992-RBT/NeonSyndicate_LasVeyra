using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class HumanoidPose
    {
        private void GroundedRecovery04OB(ref PoseInput p,ref Body b)
        {
            if(_creature || !p.Grounded || p.Ride!=RidePose.None || p.Sit>.01f || p.TravelBlend>.01f || p.Cover>.01f ||
                p.Aiming || p.AttackT>=0f || p.DodgeT>=0f || p.StaggerT>=0f || p.SwingT>=0f || p.ThrowT>=0f || p.ParryT>=0f || p.CrystalCast04G>.01f)return;
            // Landing compression and low-ready torso settle through the existing foot plants and weapon-hand IK.
            float weight=Mathf.Clamp01(_land);b.Chest*=QMath.AX(weight*4f);b.Neck*=QMath.AX(-weight*2.5f);
            if(p.Hold==HoldType.Melee && p.Ready>.1f)
            {
                float brace=Mathf.Clamp01(p.Ready)*(1f-Mathf.Clamp01(_vel.magnitude/3f));
                b.Chest*=QMath.AY(-2.5f*brace);b.Head*=QMath.AY(2f*brace);
            }
        }
    }
}
