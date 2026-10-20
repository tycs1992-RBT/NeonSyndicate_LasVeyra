using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>A frame-clipped sweep of actual held-model endpoints against gameplay capsules. No frontal-cone substitute.</summary>
    public struct BladeSweep04N
    {
        public Vector3 PreviousHilt,PreviousTip,Hilt,Tip;public float Radius;
        public static bool HistoryValid(float age,float travel){return age>=0 && age<=.2f && travel>=0 && travel<3f;}
        public static bool Local(WeaponId id,out Vector3 hilt,out Vector3 tip,out float radius)
        {
            hilt=tip=Vector3.zero;radius=.045f;
            WeaponDef live=Weapons.Get(id);
            if(live!=null && RegionalArsenal04OE.IsWeapon(live.Loot))
            {hilt=new Vector3(0,0,.17f);tip=RegionalArsenal04OE.Tip(live.Loot.Unique);radius=live.Loot.Unique==RegionalArsenal04OE.Oathblade?.07f:.09f;return true;}
            switch(id)
            {
                case WeaponId.VoltKatana:hilt=new Vector3(0,0,.165f);tip=new Vector3(0,.018f,.94f);return true;
                case WeaponId.Switchblade:hilt=new Vector3(0,0,.075f);tip=new Vector3(0,0,.28f);return true;
                case WeaponId.CrystalMaul:hilt=new Vector3(0,0,.78f);tip=new Vector3(0,0,1.05f);radius=.20f;return true;
                default:return false;
            }
        }
        public static BladeSweep04N Clip(Vector3 oldHilt,Vector3 oldTip,Vector3 hilt,Vector3 tip,float radius,float from,float to)
        {return new BladeSweep04N{PreviousHilt=Vector3.Lerp(oldHilt,hilt,Mathf.Clamp01(from)),PreviousTip=Vector3.Lerp(oldTip,tip,Mathf.Clamp01(from)),Hilt=Vector3.Lerp(oldHilt,hilt,Mathf.Clamp01(to)),Tip=Vector3.Lerp(oldTip,tip,Mathf.Clamp01(to)),Radius=radius};}
        public bool Contact(Vector3 low,Vector3 high,float bodyRadius,out Vector3 hilt,out Vector3 contact)
        {
            hilt=contact=Vector3.zero;float rr=Radius+Mathf.Clamp(bodyRadius,.05f,3f);rr*=rr;
            // Subdivide endpoint travel, including both blade segments and tip motion. Bounded even after a slow frame.
            float travel=Mathf.Max(Vector3.Distance(PreviousTip,Tip),Vector3.Distance(PreviousHilt,Hilt));
            int slices=Mathf.Clamp(Mathf.CeilToInt(travel/.06f),1,48);
            Vector3 pt=PreviousTip;
            for(int i=0;i<=slices;i++)
            {
                float t=i/(float)slices;Vector3 h=Vector3.Lerp(PreviousHilt,Hilt,t),p=Vector3.Lerp(PreviousTip,Tip,t),w;
                if(MeleeContact04H.Closest04I(h,p,low,high,out w)<=rr || (i>0 && MeleeContact04H.Closest04I(pt,p,low,high,out w)<=rr))
                {hilt=h;contact=w;return true;}pt=p;
            }
            return false;
        }
    }
    public sealed partial class WeaponController
    {
        public bool BladeSegment04N(WeaponId id,bool mirror,out Vector3 hilt,out Vector3 tip,out float radius)
        {
            hilt=tip=Vector3.zero;radius=0;Vector3 a,b;
            if(!BladeSweep04N.Local(id,out a,out b,out radius) || Current.Id!=id)return false;
            GameObject model=id==WeaponId.Switchblade && mirror?_modelL:_model;
            if(model==null || !model.activeInHierarchy)return false;
            hilt=model.transform.TransformPoint(a);tip=model.transform.TransformPoint(b);return true;
        }
    }
}
