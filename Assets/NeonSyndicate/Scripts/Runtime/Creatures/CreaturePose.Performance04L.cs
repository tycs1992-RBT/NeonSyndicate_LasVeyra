using System;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class CreaturePose
    {
        private float _forageWeight04M,_restWeight04M,_habitClock04M;
        private void Attention04L(ref CreaturePoseInput p, ref Overlay o)
        {
            if(p.ActionT>=0f && p.Action!=CreatureAction.None || _hurt>.05f || _speed>.2f || p.Flying || !p.Grounded)return;
            float dt=Mathf.Clamp(_clock-_habitClock04M,0f,.1f);_habitClock04M=_clock;
            _forageWeight04M=Mathf.MoveTowards(_forageWeight04M,p.Forage04M,dt*1.5f);
            _restWeight04M=Mathf.MoveTowards(_restWeight04M,p.Rest04M,dt*.8f);
            o.HeadPitch+=_forageWeight04M*(18f+(float)Math.Sin(_clock*2.5f)*3f)+_restWeight04M*9f;
            o.Crouch+=_restWeight04M*.18f;
            o.EarsBack+=_restWeight04M*.25f;
            float wave=(float)Math.Sin(_clock*.7f);
            // Distinct quiet listening, expressed by existing skeletons; no movement/root/hitbox or attack edits.
            switch(_plan)
            {
                case CreaturePlan.Quadruped:
                    o.EarsBack+=(wave+1f)*.08f;o.HeadPitch+=(float)Math.Sin(_clock*.9f)*2f;break;
                case CreaturePlan.Winged:
                    o.NeckYaw+=wave*2f;o.Jaw=Mathf.Max(o.Jaw,(float)Math.Max(0,-wave)*.025f);break;
            }
        }
    }
}
