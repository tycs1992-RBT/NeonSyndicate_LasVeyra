using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>Shared normalized beat: gathering, contact and release. Original partner callback owns the impact.</summary>
    public static class CinematicMotion04OF
    {
        public static void ResetStatics() { }
        public static Vector3 Beat(float progress)
        {
            if(float.IsNaN(progress) || float.IsInfinity(progress) || progress<0 || progress>1)return Vector3.zero;
            float gather=QMath.SmoothRange(0,.72f,progress)*(1-QMath.SmoothRange(.72f,.85f,progress));
            float contact=Mathf.Max(0,1-Mathf.Abs(progress-.85f)/.13f);
            float release=QMath.SmoothRange(.85f,1,progress);
            return new Vector3(gather,contact,release);
        }
    }
    public sealed partial class HumanoidPose
    {
        private void DefenderWard04OG(ref PoseInput p,ref Body b)
        {
            if(p.FrontierWard04OG<=0 || p.AttackT>=0 || p.DodgeT>=0 || p.StaggerT>=0 || p.Ride!=RidePose.None)return;
            float weight=Mathf.Clamp01(p.FrontierWard04OG);b.Chest*=QMath.AY(10*weight);
            Quaternion frame=ChestRot(ref b);Arm shield=IkArm(ref b,-1,ChestPos(ref b)+frame*S(-.16f,.03f,.33f),frame*Vector3.left,frame*Vector3.forward,frame*Vector3.up,1);
            b.AL=Lerp(b.AL,shield,weight);
        }
        private void FrontierBossStance04OG(ref PoseInput p,ref Body b)
        {
            if(p.FrontierBossKind04OG<=0 || !p.Grounded || p.AttackT>=0 || p.StaggerT>=0 || p.DodgeT>=0)return;
            bool azure=p.FrontierBossKind04OG==2;float phase=Mathf.Clamp(p.FrontierBossPhase04OG,0,2),tell=Mathf.Clamp01(p.CrystalCast04G);
            b.Spine*=QMath.AX((azure?3f:-2f)*phase-tell*(azure?7f:4f));
            b.Chest*=QMath.AY((azure?3f:-5f)*phase)*QMath.AX(tell*5);
            b.Neck*=QMath.AY((azure?-2f:4f)*phase)*QMath.AX(-tell*3);
            b.Plant=Mathf.Max(b.Plant,tell*.8f);
        }
        private float _regionalSynergyWeight04OF,_regionalSynergyTime04OF;
        private int _regionalSynergyKind04OF;
        private void RegionalSynergy04OF(ref PoseInput p,ref Body b,float dt)
        {
            bool blocked=_creature || !p.Grounded || p.Ride!=RidePose.None || p.TravelBlend>.01f || p.Cover>.01f ||
                p.AttackT>=0 || p.DodgeT>=0 || p.StaggerT>=0 || p.Guard>.1f || p.ThrowT>=0 || p.DrinkT>=0 || p.ParryT>=0 || p.Aiming;
            if(blocked){_regionalSynergyWeight04OF=0;return;}
            if(p.RegionalSynergyKind04OF>0)
            {_regionalSynergyKind04OF=p.RegionalSynergyKind04OF;_regionalSynergyTime04OF=Mathf.Clamp01(p.RegionalSynergyT04OF);}
            else _regionalSynergyTime04OF=Mathf.MoveTowards(_regionalSynergyTime04OF,1,dt*2);
            _regionalSynergyWeight04OF=Mathf.MoveTowards(_regionalSynergyWeight04OF,p.RegionalSynergyKind04OF>0?1:0,dt*7);
            float weight=_regionalSynergyWeight04OF;if(weight<=0)return;
            Vector3 beat=CinematicMotion04OF.Beat(_regionalSynergyTime04OF);
            bool guardian=_regionalSynergyKind04OF==1;
            // The right hand and weapon IK are untouched. A guardian braces; a storm partner gets a precise aiming palm.
            b.Chest*=QMath.AY((guardian?-9f:8f)*beat.x*weight);
            b.Spine*=QMath.AX((guardian?5f:2f)*beat.x*weight-3f*beat.y*weight);
            b.Neck*=QMath.AY((guardian?7f:-6f)*beat.x*weight);
            // Defensive cooperation braces down; storm offense coils through the hips before release.
            b.Pelvis+=S(0,-(guardian?.025f:.012f)*beat.x*weight,0);
            b.Hips*=QMath.AY((guardian?-3f:6f)*beat.x*weight);
            b.Plant=Mathf.Max(b.Plant,beat.x*weight*.8f);
            Quaternion frame=ChestRot(ref b);
            Vector3 gather=guardian?S(-.26f,.03f,.19f):S(-.16f,-.10f,.31f);
            Vector3 strike=guardian?S(-.12f,-.06f,.42f):S(-.09f,-.08f,.45f);
            Vector3 local=Vector3.Lerp(gather,strike,beat.y);
            Arm support=IkArm(ref b,-1,ChestPos(ref b)+frame*local,frame*new Vector3(-.8f,-.6f,-.2f),frame*Vector3.up,frame*Vector3.right,1);
            b.AL=Lerp(b.AL,support,weight*(1-beat.z*.8f));
        }
    }
}
