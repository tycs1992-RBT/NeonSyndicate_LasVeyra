using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>One bounded proposal derived from an original public route. Not a lot, service or saved identity.</summary>
    public sealed class HeartLivingFR02A
    {
        public string Key,Module,Route;public Vector3 Position,Size;public float Yaw;public bool House;
    }
    public static partial class HeartPlanFR01
    {
        public const int LivingHouseLimit=18,LivingPropLimit=18;
        public static bool LivingSlice(CityLayout l,Vector3 p)
        {return l!=null && l.GothicCivicH5 && Math.Abs(p.x-l.RealmCourt04OB.x)<145 && p.z>l.RealmCourt04OB.z-110 && p.z<l.RealmCourt04OB.z+200;}
        public static Vector3 LivingSize(string module)
        {
            switch(module)
            {
                case "StreetTownhouse":return new Vector3(8.2f,12,11);
                case "StreetResidence":return new Vector3(9.2f,19,12);
                case "StreetMerchant":return new Vector3(10.2f,12.5f,13.2f);
                case "StreetWorkshop":return new Vector3(12,12.5f,12);
                case "StreetGuild":return new Vector3(10.2f,13,13);
                case "StreetCourtRow":return new Vector3(15.8f,12,10);
                case "StreetRuin":return new Vector3(9,8.5f,12);
                case "StreetCorner":return new Vector3(9.2f,16,12);
                case "StreetGallery":return new Vector3(12.2f,13,13);
                case "CourtMarket":return new Vector3(6.4f,4.9f,4.8f);
                case "CourtWorkyard":return new Vector3(4,2,2.8f);
                case "CourtGarden":return new Vector3(4.2f,2,3.8f);
                case "CourtLantern":return new Vector3(1.1f,4.4f,1.1f);
                default:return Vector3.zero;
            }
        }
        private static string LivingFamily(int n)
        {
            switch(n%9){case 0:return "StreetTownhouse";case 1:return "StreetMerchant";case 2:return "StreetResidence";case 3:return "StreetWorkshop";case 4:return "StreetGuild";case 5:return "StreetCorner";case 6:return "StreetGallery";case 7:return "StreetCourtRow";default:return "StreetRuin";}
        }
        /// <summary>Conservative horizontal reservation. Rotated bounds include all roof and gallery overhangs.</summary>
        public static RectF LivingFootprint(HeartLivingFR02A p,float margin=0)
        {
            float c=Mathf.Abs(Mathf.Cos(p.Yaw*Mathf.Deg2Rad)),s=Mathf.Abs(Mathf.Sin(p.Yaw*Mathf.Deg2Rad));
            float x=(c*p.Size.x+s*p.Size.z)*.5f+margin,z=(s*p.Size.x+c*p.Size.z)*.5f+margin;
            return new RectF(p.Position.x-x,p.Position.z-z,p.Position.x+x,p.Position.z+z);
        }
        /// <summary>Classifies actual original owners. Receipt roofs and service lots cannot be erased for art coverage.</summary>
        public static string LivingClassification(CityLayout l,Lot lot,Building b)
        {
            if(b==null || lot==null || !LivingSlice(l,b.Footprint.Center(0)))return "Unsafe or unknown, preserve original";
            if(lot.CustomBuilt || lot.Poi!=PoiType.None || !string.IsNullOrEmpty(lot.PoiName) || b.GothicFamily04OB==0)return "Protected gameplay/service owner";
            if(b.RoofPad04OB || b.GothicFamily04OB>=7 || b.Roof!=RoofType.Flat)return "Protected gameplay/service owner";
            foreach(List<Vector3> points in new[]{l.DataChipSpots,l.CrystalShardSpots,l.SpellStoneSpots,l.PickupSpots,l.RadioMastSpots})foreach(Vector3 point in points)if(b.Footprint.Inset(-4).Contains(point))return "Protected gameplay/service owner";
            // Original ordinary families have open halls, stairs or ledges. A closed shell is not automatically compatible.
            return "Requires a native collision review";
        }
        public static List<HeartLivingFR02A> LivingProposals(CityLayout l)
        {
            List<HeartLivingFR02A> result=new List<HeartLivingFR02A>();if(l==null || !l.GothicCivicH5)return result;
            int family=0;
            foreach(string id in new[]{"home-court","sanctuary-approach","forge-market","sanctuary-observatory","dispatch-clinic","home-dispatch","service-crossing","garage-foot-return","bondkeeper","office","depot","inn"})
            foreach(RealmRoute04OB route in l.RealmRoutes04OB)if(route.Id==id)
            for(int segment=1;segment<route.Points.Count;segment++)
            {
                Vector3 a=route.Points[segment-1],b=route.Points[segment],f=MathUtil.Flat(b-a).normalized,right=MathUtil.RightOf(f);
                float length=MathUtil.FlatDistance(a,b);int samples=Mathf.Max(1,Mathf.FloorToInt(length/13));
                for(int row=0;row<3;row++)for(int i=0;i<samples;i++)for(int sign=-1;sign<=1;sign+=2)
                {
                    string module=LivingFamily(family++);Vector3 size=LivingSize(module);
                    float offset=route.Width*.5f+size.z*.5f+2.6f+row*15;
                    Vector3 at=Vector3.Lerp(a,b,(i+.5f)/samples)+right*sign*offset;
                    result.Add(new HeartLivingFR02A{Key=id+"/"+segment+"/"+row+"/"+i+"/"+sign,Route=id,Module=module,Position=at,Size=size,Yaw=MathUtil.YawOf(-right*sign),House=true});
                }
            }
            // A public market/work court alongside its existing Forge route. No invented merchant or service position.
            foreach(RealmRoute04OB route in l.RealmRoutes04OB)if(route.Id=="forge-market")
            for(int segment=1;segment<route.Points.Count;segment++)
            {
                Vector3 a=route.Points[segment-1],b=route.Points[segment],f=MathUtil.Flat(b-a).normalized,right=MathUtil.RightOf(f);
                for(int row=0;row<2;row++)for(int i=0;i<7;i++)for(int sign=-1;sign<=1;sign+=2)
                {
                    string module=i%3==0?"CourtMarket":i%3==1?"CourtWorkyard":row==0?"CourtGarden":"CourtLantern";Vector3 size=LivingSize(module);
                    result.Add(new HeartLivingFR02A{Key="market/"+segment+"/"+row+"/"+i+"/"+sign,Route=route.Id,Module=module,Size=size,Position=Vector3.Lerp(a,b,(i+.5f)/7)+right*sign*(route.Width*.5f+size.z*.5f+2.6f+row*6),Yaw=MathUtil.YawOf(-right*sign)});
                }
            }
            return result;
        }
        public static bool LivingClear(CityLayout l,HeartLivingFR02A p,IList<HeartLivingFR02A> admitted,out string reason)
        {
            reason="Outside bounded revision-10 neighborhood or invalid reservation.";
            if(p==null || !Finite(p.Position) || !Finite(p.Size) || float.IsNaN(p.Yaw) || float.IsInfinity(p.Yaw) || !LivingSlice(l,p.Position) || p.Size.x<=0 || p.Size.y<=0 || p.Size.z<=0)return false;
            RectF area=LivingFootprint(p,1);
            reason="Original parcel or service envelope.";
            foreach(Block block in l.Blocks)foreach(Lot lot in block.Lots)foreach(Building b in lot.Buildings)if(area.Overlaps(b.Footprint,3))return false;
            reason="Original public route, including its full width.";
            foreach(RealmRoute04OB r in l.RealmRoutes04OB)for(int i=1;i<r.Points.Count;i++)
                if(LivingCrosses(p,r.Points[i-1],r.Points[i],r.Width*.5f+2)){reason="Original public route "+r.Id+", including its full width.";return false;}
            reason="Original pedestrian graph.";
            foreach(WalkLink link in l.WalkLinks)if(LivingCrosses(p,l.WalkNodes[link.A].Pos,l.WalkNodes[link.B].Pos,2.5f))return false;
            reason="Original vehicle corridor.";
            foreach(RoadEdge edge in l.Edges)if(area.Overlaps(l.EdgeRect(edge),4))return false;
            reason="Original receipt, spawn, fixture or quest approach.";
            foreach(List<Vector3> points in new[]{l.DataChipSpots,l.CrystalShardSpots,l.SpellStoneSpots,l.PickupSpots,l.CreatureSpots,l.ParkingSpots,l.RadioMastSpots})foreach(Vector3 q in points)if(area.Inset(-5).Contains(q))return false;
            foreach(PropSpot prop in l.Props)if(area.Inset(-2).Contains(prop.Pos))return false;
            foreach(Poi poi in l.Pois)if(area.Inset(-10).Contains(poi.Position))return false;
            foreach(SpecialSite site in l.Sites)if(area.Inset(-site.Radius-3).Contains(site.Position))return false;
            reason="Existing FR01 compact gateway reservation.";
            for(int i=0;i<GateCandidates;i++){Vector3 at;float yaw;string why;if(GateCandidate(l,i,out at,out yaw,out why) && !OutsideGate(p.Position,Mathf.Max(p.Size.x,p.Size.z)*.5f,at,yaw))return false;}
            reason="Delivered FR01 street-bay reservation.";
            foreach(HeartSideFR01 side in StreetSides(l))if(Clear(l,side.Position,side.Radius) && area.Inset(-side.Radius-1).Contains(side.Position))return false;
            reason="Another admitted neighborhood module.";
            if(admitted!=null)foreach(HeartLivingFR02A other in admitted)if(area.Overlaps(LivingFootprint(other),1))return false;
            reason="Source envelopes clear; native solid, trigger and actor checks still required.";return true;
        }
        private static bool Crosses(RectF r,Vector3 a,Vector3 b)
        {
            // Slab clipping, including endpoints and tangency, avoids gaps from point sampling on a long link.
            float lo=0,hi=1;Vector3 delta=b-a;
            return ClipAxis(a.x,delta.x,r.X0,r.X1,ref lo,ref hi) && ClipAxis(a.z,delta.z,r.Z0,r.Z1,ref lo,ref hi);
        }
        public static bool LivingCrosses(HeartLivingFR02A p,Vector3 a,Vector3 b,float margin)
        {
            Vector3 forward=MathUtil.DirFromYaw(p.Yaw),right=MathUtil.RightOf(forward),aa=a-p.Position,bb=b-p.Position;
            RectF local=new RectF(-p.Size.x*.5f-margin,-p.Size.z*.5f-margin,p.Size.x*.5f+margin,p.Size.z*.5f+margin);
            return Crosses(local,new Vector3(Vector3.Dot(aa,right),0,Vector3.Dot(aa,forward)),new Vector3(Vector3.Dot(bb,right),0,Vector3.Dot(bb,forward)));
        }
        private static bool ClipAxis(float at,float delta,float min,float max,ref float lo,ref float hi)
        {
            if(Mathf.Abs(delta)<.00001f)return at>=min && at<=max;
            float a=(min-at)/delta,b=(max-at)/delta;if(a>b){float swap=a;a=b;b=swap;}
            lo=Mathf.Max(lo,a);hi=Mathf.Min(hi,b);return lo<=hi;
        }
    }
}
