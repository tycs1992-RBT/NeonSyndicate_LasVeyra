using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Bounded active-frame weapon volumes. Pure trajectory, shared by runtime contact and regression probes.</summary>
    public static class MeleeContact04H
    {
        public static void ResetStatics() { }
        public const int Slices = 24;
        public static bool Supported(WeaponId weapon, MeleeMove move)
        {
            return move != null && move.Arc < 300f && move.Kind != MoveKind.Riposte &&
                (weapon == WeaponId.VoltKatana || weapon == WeaponId.Switchblade || weapon == WeaponId.CrystalMaul);
        }
        public static float Radius(WeaponId weapon)
        { return weapon == WeaponId.CrystalMaul ? .24f : weapon == WeaponId.Switchblade ? .12f : .16f; }

        /// <summary>Root-local hilt and contact tip. Active phase only, deliberately independent of render frame rate.</summary>
        public static void Segment(WeaponId weapon, MeleeMove move, float phase, bool mirror, out Vector3 hilt, out Vector3 tip)
        {
            float u = Mathf.Clamp01(phase);
            float reach = move.Range + .45f;
            bool thrust = move.Anim == MeleeAnim.Stab || move.Anim == MeleeAnim.Thrust;
            bool overhead = move.Anim == MeleeAnim.Overhead || move.Anim == MeleeAnim.Slam;
            float side = mirror || move.Anim == MeleeAnim.SlashLeft ? -1f : 1f;
            if (thrust)
            {
                hilt = new Vector3(side*.16f,1.12f,.35f+u*.15f);
                tip = new Vector3(side*.05f,1.1f,Mathf.Lerp(reach*.65f,reach,u));
            }
            else if (overhead)
            {
                float angle = Mathf.Lerp(-48f,60f,u)*Mathf.Deg2Rad;
                hilt = new Vector3(side*.10f,1.2f,.4f);
                tip = hilt + new Vector3(0f,-Mathf.Sin(angle),Mathf.Cos(angle))*Mathf.Max(.4f,reach-.4f);
            }
            else
            {
                float arc = Mathf.Clamp(move.Arc,35f,155f);
                float a = Mathf.Lerp(-arc*.5f,arc*.5f,u)*Mathf.Deg2Rad*side;
                Vector3 radial = new Vector3(Mathf.Sin(a),0f,Mathf.Cos(a));
                hilt = new Vector3(radial.x*.32f,1.15f,radial.z*.32f);
                tip = new Vector3(radial.x*reach,Mathf.Lerp(1.28f,.94f,u),radial.z*reach);
            }
        }

        /// <summary>Squared closest distance of two closed segments, including points and parallel segments.</summary>
        public static float SegmentDistanceSq(Vector3 p, Vector3 q, Vector3 a, Vector3 b)
        {
            Vector3 witness;
            return Closest04I(p,q,a,b,out witness);
        }

        private static float Closest04I(Vector3 p,Vector3 q,Vector3 a,Vector3 b,out Vector3 witness)
        {
            witness=p;
            Vector3 d=q-p,e=b-a,r=p-a;
            float dd=Vector3.Dot(d,d),ee=Vector3.Dot(e,e),de=Vector3.Dot(d,e);
            float dr=Vector3.Dot(d,r),er=Vector3.Dot(e,r),s,t;
            if (dd<1e-8f && ee<1e-8f) return r.sqrMagnitude;
            if (dd<1e-8f) { s=0f;t=Mathf.Clamp01(er/ee); }
            else
            {
                if(ee<1e-8f) { t=0f;s=Mathf.Clamp01(-dr/dd); }
                else
                {
                    float det=dd*ee-de*de;
                    s=det>1e-8f?Mathf.Clamp01((de*er-dr*ee)/det):0f;
                    t=(de*s+er)/ee;
                    if(t<0f){t=0f;s=Mathf.Clamp01(-dr/dd);}
                    else if(t>1f){t=1f;s=Mathf.Clamp01((de-dr)/dd);}
                }
            }
            witness=p+d*s;
            return (witness-a-e*t).sqrMagnitude;
        }

        /// <summary>Capsule against a swept blade strip. Fixed phase slices avoid low-frame-rate cone-only hits.</summary>
        public static bool Touches(WeaponId weapon, MeleeMove move, float from, float to, bool mirror,
            Vector3 targetLow, Vector3 targetHigh, float bodyRadius)
        {
            Vector3 hilt,contact;
            return Contact04I(weapon,move,from,to,mirror,targetLow,targetHigh,bodyRadius,out hilt,out contact);
        }

        /// <summary>Returns a witness on the actual approximate swept strip for the runtime cover checks.</summary>
        public static bool Contact04I(WeaponId weapon,MeleeMove move,float from,float to,bool mirror,
            Vector3 targetLow,Vector3 targetHigh,float bodyRadius,out Vector3 hilt,out Vector3 contact)
        {
            hilt=contact=Vector3.zero;
            if(move==null || float.IsNaN(from)||float.IsNaN(to)||float.IsInfinity(from)||float.IsInfinity(to)||from>to) return false;
            from=Mathf.Clamp01(from);to=Mathf.Clamp01(to);
            Vector3 prevH,prevT;
            Segment(weapon,move,from,mirror,out prevH,out prevT);
            float radius=Radius(weapon)+Mathf.Clamp(bodyRadius,.05f,3f);
            float rr=radius*radius;
            Vector3 witness;
            if(Closest04I(prevH,prevT,targetLow,targetHigh,out witness)<=rr)
            {hilt=prevH;contact=witness;return true;}
            int first=Mathf.FloorToInt(from*Slices)+1;
            for(int index=first;index<=Slices+1;index++)
            {
                float phase=Mathf.Min(to,(float)index/Slices);
                Vector3 h,t;
                Segment(weapon,move,phase,mirror,out h,out t);
                if(Closest04I(h,t,targetLow,targetHigh,out witness)<=rr)
                {hilt=h;contact=witness;return true;}
                // Track four radial rails as well as the blade's current segment, filling the small swept strip.
                for(int rail=0;rail<=4;rail++)
                {
                    float f=rail*.25f;
                    if(Closest04I(Vector3.Lerp(prevH,prevT,f),Vector3.Lerp(h,t,f),targetLow,targetHigh,out witness)<=rr)
                    {hilt=(prevH+h)*.5f;contact=witness;return true;}
                }
                if(phase>=to) break;
                prevH=h;prevT=t;
            }
            return false;
        }
    }
}
