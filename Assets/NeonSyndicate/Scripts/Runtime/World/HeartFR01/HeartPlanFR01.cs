using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class CityLayout { internal HeartPresentationFR01 HeartArtFR01; }
    /// <summary>Read-only admission against the original revision, routes, parcels and receipts.</summary>
    public static partial class HeartPlanFR01
    {
        public static void ResetStatics() { }
        public static bool Active(bool selected,bool compare,bool suspended,bool ready)
        { return selected && !compare && !suspended && ready; }
        public static float SegmentDistance(Vector3 p,Vector3 a,Vector3 b)
        { Vector3 d=MathUtil.Flat(b-a);float t=d.sqrMagnitude>.001f?Mathf.Clamp01(Vector3.Dot(MathUtil.Flat(p-a),d)/d.sqrMagnitude):0;return MathUtil.FlatDistance(p,a+d*t); }
        public static bool InSlice(CityLayout l,Vector3 p)
        { return l!=null && l.GothicCivicH5 && Mathf.Abs(p.x-l.RealmCourt04OB.x)<135 && p.z>l.RealmCourt04OB.z-80 && p.z<l.RealmCourt04OB.z+150; }
        public static bool Eligible(CityLayout l,Building b)
        {
            if(b==null || !InSlice(l,b.Footprint.Center(0)) || b.GothicFamily04OB<1 || b.GothicFamily04OB>6 || b.RoofPad04OB || b.Roof!=RoofType.Flat || b.Height<6 || b.Footprint.Width<10 || b.Footprint.Depth<10)return false;
            foreach(List<Vector3> list in new[]{l.DataChipSpots,l.CrystalShardSpots,l.SpellStoneSpots,l.PickupSpots})foreach(Vector3 p in list)if(b.Footprint.Inset(-3).Contains(p.x,p.z))return false;
            return true;
        }
        public static bool Clear(CityLayout l,Vector3 at,float radius)
        {
            if(!InSlice(l,at) || l.IsInsideBuilding(at,radius+1))return false;
            foreach(RealmRoute04OB r in l.RealmRoutes04OB)for(int i=1;i<r.Points.Count;i++)if(SegmentDistance(at,r.Points[i-1],r.Points[i])<r.Width*.5f+radius+1)return false;
            foreach(List<Vector3> list in new[]{l.DataChipSpots,l.CrystalShardSpots,l.SpellStoneSpots,l.PickupSpots})foreach(Vector3 p in list)if(MathUtil.FlatDistance(at,p)<radius+3)return false;
            foreach(SpecialSite s in l.Sites)if(MathUtil.FlatDistance(at,s.Position)<radius+8)return false;
            return true;
        }
        public static bool Gate(CityLayout l,out Vector3 at,out float yaw,int first=0)
        {
            at=Vector3.zero;yaw=0;string reason;
            for(int i=Mathf.Max(0,first);i<GateCandidates;i++)if(GateCandidate(l,i,out at,out yaw,out reason))return true;
            return false;
        }
    }
}
