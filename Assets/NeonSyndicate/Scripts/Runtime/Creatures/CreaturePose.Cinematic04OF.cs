using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class CreaturePose
    {
        private void StormProwlerPose04OF(ref CreaturePoseInput p,ref Overlay o)
        {
            if(!_look.StormProwler04OF || _hurt>.05f || !p.Grounded || p.Flying || p.Action==CreatureAction.Death)return;
            if(p.Action==CreatureAction.Pounce && p.ActionT>=0 && p.ActionT<p.Windup)
            {
                float prepare=QMath.SmoothRange(0,Mathf.Max(.01f,p.Windup),p.ActionT);
                o.NeckPitch-=prepare*8;o.HeadPitch+=prepare*5;o.TailBase+=prepare*12;
                o.CrouchFront+=prepare*.06f*_size;o.CrouchRear+=prepare*.025f*_size;
                o.EarsBack=Mathf.Max(o.EarsBack,prepare*.65f);
                // Original pounce controller and paw plants still own reach, strike timing and root motion.
                return;
            }
            if(p.Action==CreatureAction.Pounce && p.ActionT>=p.Strike && p.ActionT<=1)
            {float recover=1-QMath.SmoothRange(p.Strike,1,p.ActionT);o.HeadPitch+=recover*7;o.TailBase-=recover*8;return;}
            if(p.ActionT>=0 && p.Action!=CreatureAction.None)return;
            float quiet=1-Mathf.Clamp01(_speed/1.8f);
            o.HeadRoll+=quiet*Mathf.Sin(_clock*.6f)*3;
            o.NeckYaw+=quiet*Mathf.Sin(_clock*.35f)*4;
            o.TailYaw+=Mathf.Sin(_clock*1.6f)*5*(1-Mathf.Clamp01(_alert));
        }
    }
}
