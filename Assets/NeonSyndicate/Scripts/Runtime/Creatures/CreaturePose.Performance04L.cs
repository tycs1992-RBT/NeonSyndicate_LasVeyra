using System;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class CreaturePose
    {
        private void Attention04L(ref CreaturePoseInput p, ref Overlay o)
        {
            if(p.ActionT>=0f && p.Action!=CreatureAction.None || _hurt>.05f || _speed>.2f || p.Flying || !p.Grounded)return;
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
