using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class HumanoidPose
    {
        private void AscensionWeight04OH(ref PoseInput p,ref Body b)
        {
            if(_creature || !p.Grounded || p.TravelBlend>.01f || p.Ride!=RidePose.None || p.Sit>.01f || p.Cover>.01f ||
                p.AttackT>=0 || p.DodgeT>=0 || p.StaggerT>=0 || p.Aiming || p.ThrowT>=0 || p.DrinkT>=0 || p.ParryT>=0)return;
            float quiet=1-Mathf.Clamp01(_vel.magnitude/1.6f),ready=Mathf.Clamp01(p.Ready);
            // Opposed rib cage/neck breathing and a subtle asymmetric hip load give the hero a grounded ready stance.
            float breath=Mathf.Sin(_idleT*1.7f)*quiet;
            b.Pelvis+=S(.006f*quiet*(1-ready),-.010f*quiet*ready,0);
            b.Spine*=QMath.AX(breath*.65f)*QMath.AY(-ready*quiet*2);
            b.Chest*=QMath.AZ(-quiet*1.2f)*QMath.AX(-breath*.35f);
            b.Neck*=QMath.AZ(quiet*.9f)*QMath.AY(Mathf.Clamp(_yawRate*.018f,-3,3));
        }
    }
}
