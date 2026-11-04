using UnityEngine;
namespace NeonSyndicate
{
    internal sealed partial class CreatureSculptor
    {
        private void ProwlerAnatomyH3()
        {
            if(!_k.StormProwler04OF || _bodyTube==null || _detail==0)return;
            Vector2 hide=Pal(_k.Hide),belly=Pal(_k.Belly);
            // Rounded scapular and haunch masses, with real articulated foreleg/hock joint coverage.
            // Append outside the authoritative core ranges. Skeleton, original radius and attack volumes stay exact.
            foreach(LegPlan leg in _legs)
            {
                Vector3 axis=Nrm(leg.Knee-leg.Hip),at=Vector3.Lerp(leg.Hip,leg.Knee,leg.Front?.28f:.18f);
                EmitEllipsoid(at,new Vector3(_H*(leg.Front?.12f:.15f),_H*.17f,_H*.14f),QMath.FromTo(Vector3.up,axis),5,8,hide,W2(leg.GirdleBone,leg.Upper,.7f));
                EmitEllipsoid(leg.Knee,new Vector3(.075f,.10f,.08f)*_H,QMath.Identity,4,7,hide,W2(leg.Upper,leg.Lower,.5f));
                // Metacarpal tendons and three separated claw sheaths make the paws read as animal limbs.
                for(int toe=-1;toe<=1;toe++)
                {
                    Vector3 root=leg.Paw+Vector3.right*toe*.035f*_H;
                    Strut(new[]{root+Vector3.up*.05f*_H,root+new Vector3(0,.035f,.10f)*_H,root+new Vector3(0,.005f,.15f)*_H},.014f*_H,belly,W1(leg.Foot));
                }
            }
            // A lower-jaw throat ruff leaves the real mouth and independent jaw hinge unobstructed.
            for(int sign=-1;sign<=1;sign+=2)
                QuartzScaleOnTube(_headTube,.20f,sign>0?320:220,_headLen*.30f,_headLen*.18f,_headLen*.035f,hide,belly);
        }
    }
    public sealed partial class CreaturePose
    {
        private void GothicReadabilityH3(ref CreaturePoseInput p,ref Overlay o)
        {
            if(!p.Grounded || p.Flying || p.Hurt>.05f || p.Action==CreatureAction.Death || p.Action==CreatureAction.Stagger)return;
            if(_look.StormProwler04OF && p.Action==CreatureAction.Pounce && p.ActionT>=0 && p.ActionT< p.Windup)
            {
                float gather=QMath.SmoothRange(0,Mathf.Max(.01f,p.Windup),p.ActionT);
                // A still muzzle and elevated tail counterbalance the lowered shoulder; no extra lunge or damage.
                o.ChestPitch-=gather*4;o.HeadPitch+=gather*3;o.TailBase+=gather*8;
                o.CrouchRear+=gather*.025f*_size;
            }
            if(p.RegionalArt04OG==1 && p.ActionT>=0 && p.ActionT<1 && p.Action!=CreatureAction.None)
            {
                Vector3 beat=FrontierPerformance04OG.ArtBeat(1,p.ActionT);
                o.Roll-=beat.x*4;o.ChestPitch-=beat.x*5;o.HeadYaw+=beat.x*5-beat.y*6;
                o.CrouchRear+=beat.y*.035f*_size;o.NeckPitch+=beat.y*5;
            }
        }
    }
}
