using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Presentation envelopes sampled by action progress, never accumulated per rendered frame.</summary>
    public static class MotionBeat04K
    {
        public static float Contact(float t,float windup,float active)
        {
            if(float.IsNaN(t)||float.IsInfinity(t)||float.IsNaN(windup)||float.IsNaN(active)||
                float.IsInfinity(windup)||float.IsInfinity(active)||windup<=0f||active<=0f||t<windup||t>windup+active) return 0f;
            return (float)Math.Sin(Math.PI*(t-windup)/active);
        }
        public static float Recover(float t,float windup,float active)
        {
            float end=windup+active;
            if(float.IsNaN(t)||float.IsInfinity(t)||float.IsNaN(end)||float.IsInfinity(end)||end>=1f||t<end||t>=1f) return 0f;
            float u=(t-end)/(1f-end);
            return (float)Math.Sin(Math.PI*u)*(1f-u);
        }
    }

    public sealed partial class HumanoidPose
    {
        private float _motionClock04K;
        private void Motion04K(ref PoseInput p,ref Body b,float dt)
        {
            _motionClock04K=(_motionClock04K+dt)%120f;
            if(p.Ride!=RidePose.None||!p.Grounded||p.Sit>.1f||p.DodgeT>=0f||p.StaggerT>=0f||p.Cover>.1f||p.TravelBlend>.1f) return;
            if(p.AttackT>=0f)
            {
                float contact=MotionBeat04K.Contact(p.AttackT,p.AttackWindup,p.AttackActive);
                float recover=MotionBeat04K.Recover(p.AttackT,p.AttackWindup,p.AttackActive);
                // Heavy heel-set, paired-blade shoulder chase, deliberate single-blade recovery.
                float amount=p.TwoHanded?1f:p.PairedBlades?.45f:.65f;
                b.Pelvis.y-=.009f*_s*amount*contact;
                b.Neck=b.Neck*QMath.AX(-2f*amount*contact+1.5f*recover);
                return;
            }
            if(p.SwingT>=0f||p.ParryT>=0f||p.ThrowT>=0f||p.DrinkT>=0f||p.Aiming||p.CrystalCast04G>.01f) return;
            float breathe=Mathf.Sin(_motionClock04K*1.7f)*(.4f+.25f*_ready);
            b.Chest=b.Chest*QMath.AX(breathe);
            if(_ready>.1f)
                b.Neck=b.Neck*QMath.AY(Mathf.Sin(_motionClock04K*.65f)*1.8f*_ready);
        }
    }
}
