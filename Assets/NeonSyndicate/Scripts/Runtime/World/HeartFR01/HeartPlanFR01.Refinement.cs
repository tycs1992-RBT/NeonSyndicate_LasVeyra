using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>A proposed existing-route visual. Source and native admission both remain mandatory.</summary>
    public sealed class HeartSideFR01
    {
        public string Module;public Vector3 Position;public float Yaw,Radius;
        public HeartSideFR01(string module,Vector3 position,float yaw,float radius)
        {Module=module;Position=position;Yaw=yaw;Radius=radius;}
    }
    public static partial class HeartPlanFR01
    {
        public const int GateCandidates=18;
        public const float GatePierOffset=9f,GateClearance=3.4f;
        /// <summary>A compact gateway on the existing court-to-sanctuary link, never stretched across other routes.</summary>
        public static bool GateCandidate(CityLayout l,int index,out Vector3 at,out float yaw,out string reason)
        {
            at=Vector3.zero;yaw=0;reason="Wrong world or gate candidate.";
            if(l==null || !l.GothicCivicH5 || index<0 || index>=GateCandidates)return false;
            RealmRoute04OB home=null,sanctuary=null;
            foreach(RealmRoute04OB r in l.RealmRoutes04OB){if(r.Id=="home-court")home=r;if(r.Id=="sanctuary-approach")sanctuary=r;}
            reason="Original home-court and sanctuary route connection missing.";
            if(home==null || sanctuary==null || home.Points.Count<2 || sanctuary.Points.Count<2 || MathUtil.FlatDistance(home.Points[home.Points.Count-1],sanctuary.Points[0])>.01f)return false;
            int segment=index/9;int sample=index%9;
            if(segment+1>=sanctuary.Points.Count)return false;
            Vector3 a=sanctuary.Points[segment],b=sanctuary.Points[segment+1],forward=MathUtil.Flat(b-a).normalized;
            if(MathUtil.FlatDistance(a,b)<20f)return false;
            at=Vector3.Lerp(a,b,segment==0?.65f+sample*.025f:.35f+sample*.065f);yaw=MathUtil.YawOf(forward);Vector3 right=MathUtil.RightOf(forward);
            foreach(int sign in new[]{-1,1})
            {
                Vector3 pier=at+right*sign*GatePierOffset;
                if(!Clear(l,pier,GateClearance)){reason="Candidate "+index+", "+(sign<0?"left":"right")+" pier: "+SourceRefusal(l,pier,GateClearance);return false;}
            }
            reason="Both piers clear original parcels, routes, receipts and sites.";return true;
        }
        private static string SourceRefusal(CityLayout l,Vector3 p,float radius)
        {
            if(!InSlice(l,p))return "outside bounded revision-10 slice.";
            if(l.IsInsideBuilding(p,radius+1))return "original building envelope.";
            foreach(RealmRoute04OB r in l.RealmRoutes04OB)for(int i=1;i<r.Points.Count;i++)if(SegmentDistance(p,r.Points[i-1],r.Points[i])<r.Width*.5f+radius+1)return "original route "+r.Id+".";
            foreach(List<Vector3> list in new[]{l.DataChipSpots,l.CrystalShardSpots,l.SpellStoneSpots,l.PickupSpots})foreach(Vector3 q in list)if(MathUtil.FlatDistance(p,q)<radius+3)return "original collectible or pickup.";
            foreach(SpecialSite s in l.Sites)if(MathUtil.FlatDistance(p,s.Position)<radius+8)return "original site "+s.Name+".";
            return "original source clearance refused.";
        }
        /// <summary>Keep side pieces away from the whole new gateway, including its overhead arch.</summary>
        public static bool OutsideGate(Vector3 p,float radius,Vector3 gate,float yaw)
        {
            Vector3 delta=MathUtil.Flat(p-gate),forward=MathUtil.DirFromYaw(yaw),right=MathUtil.RightOf(forward);
            return Mathf.Abs(Vector3.Dot(delta,right))>12f+radius || Mathf.Abs(Vector3.Dot(delta,forward))>4.5f+radius;
        }
        /// <summary>Reuse the delivered bay kit along actual route edges, rather than fixed grid rows that mostly refuse.</summary>
        public static List<HeartSideFR01> StreetSides(CityLayout l)
        {
            List<HeartSideFR01> result=new List<HeartSideFR01>();if(l==null || !l.GothicCivicH5)return result;
            foreach(RealmRoute04OB r in l.RealmRoutes04OB)if(r.Id=="sanctuary-approach")
            for(int segment=1;segment<r.Points.Count && segment<=2;segment++)
            {
                Vector3 a=r.Points[segment-1],b=r.Points[segment],f=MathUtil.Flat(b-a).normalized;
                for(int sign=-1;sign<=1;sign+=2)for(int i=1;i<=4;i++)
                {
                    Vector3 p=Vector3.Lerp(a,b,i*.2f)+MathUtil.RightOf(f)*sign*(r.Width*.5f+7f);
                    result.Add(new HeartSideFR01(i%2==0?"HeartBlindBay":"HeartArcade",p,MathUtil.YawOf(f)+sign*90,3f));
                }
            }
            return result;
        }
        public static float LodThreshold(int index,int count)
        {return index==count-1?.025f:index==0?.45f:.15f;}
        /// <summary>Fit asymmetric imported bounds to the original parcel without moving its physical identity.</summary>
        public static bool Fit(Building b,Vector3 center,Vector3 size,out Vector3 position,out Vector3 scale)
        {
            position=Vector3.zero;scale=Vector3.one;
            if(b==null || !Finite(center) || !Finite(size) || size.x<=.001f || size.y<=.001f || size.z<=.001f || b.Height<=0 || b.Footprint.Width<=0 || b.Footprint.Depth<=0)return false;
            scale=new Vector3(b.Footprint.Width/size.x,b.Height/size.y,b.Footprint.Depth/size.z);
            position=b.Footprint.Center(CityLayout.CurbHeight)-Vector3.Scale(new Vector3(center.x,center.y-size.y*.5f,center.z),scale);return true;
        }
        private static bool Finite(Vector3 p)
        {return !float.IsNaN(p.x)&&!float.IsNaN(p.y)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.x)&&!float.IsInfinity(p.y)&&!float.IsInfinity(p.z);}
    }
}
