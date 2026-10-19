using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class HumanoidPose
    {
        private float _sideAcc04L;
        private void WeightBalance04L(ref PoseInput p, ref Body b)
        {
            if(_creature || !p.Grounded || p.Sit>.1f || p.Ride!=RidePose.None || p.TravelBlend>.01f || p.Cover>.01f
                || p.Aiming || p.Ready>.1f || p.Guard>.1f || p.AttackT>=0f || p.SwingT>=0f || p.DodgeT>=0f
                || p.CrystalCast04G>.01f || p.StaggerT>=0f || p.ThrowT>=0f || p.DrinkT>=0f || p.ParryT>=0f || p.ParcelCarry04G)return;
            // Counterbalance a sidestep/start/stop through the torso, with a stable head. The existing pelvis,
            // root controller, foot plants and IK remain the locomotion authority.
            float roll=Mathf.Clamp(-_sideAcc04L*.35f,-4f,4f);
            float pivot=Mathf.Clamp(-_yawRate*.018f,-3f,3f)*(1f-SS(1f,3f,_vel.magnitude));
            b.Chest*=QMath.AZ(roll)*QMath.AY(pivot);
            b.Neck*=QMath.AZ(-roll*.55f)*QMath.AY(-pivot*.5f);
        }
    }
}
