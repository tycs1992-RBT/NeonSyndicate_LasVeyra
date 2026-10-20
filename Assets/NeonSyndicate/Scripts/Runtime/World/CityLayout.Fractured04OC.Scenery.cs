using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    public sealed class WildScenery04OC
    {
        public Vector3 Position, Size;
        public int Kind, Region, Seed;
    }
    public sealed partial class CityLayout
    {
        public readonly List<WildScenery04OC> WildScenery04OC = new List<WildScenery04OC>();
        /// <summary>Only admitted unowned ground can rise. Roads, doors, saved receipts and the authored paths stay flat.</summary>
        public float WildClearance04OC(Vector3 point)
        {
            float clearance=80f;
            foreach(Block block in Blocks)foreach(Lot lot in block.Lots)foreach(Building b in lot.Buildings)
                clearance=Mathf.Min(clearance,RectDistance04OC(b.Footprint,point)-4f);
            foreach(Lot lot in DesertLots)foreach(Building b in lot.Buildings)clearance=Mathf.Min(clearance,RectDistance04OC(b.Footprint,point)-5f);
            foreach(RoadEdge e in Edges)clearance=Mathf.Min(clearance,RectDistance04OC(EdgeRect(e),point)-5f);
            foreach(RealmRoute04OB route in RealmRoutes04OB)for(int i=1;i<route.Points.Count;i++)
                clearance=Mathf.Min(clearance,SegmentDistance04OC(point,route.Points[i-1],route.Points[i])-route.Width*.5f-3f);
            foreach(Vector3 center in new[]{RealmThreshold04OB,RealmChallenge04OC,AzureGlade04OC})clearance=Mathf.Min(clearance,MathUtil.FlatDistance(center,point)-23f);
            foreach(List<Vector3> list in new[]{DataChipSpots,CrystalShardSpots,SpellStoneSpots,CreatureSpots,PickupSpots,ParkingSpots,RadioMastSpots})
                foreach(Vector3 p in list)clearance=Mathf.Min(clearance,MathUtil.FlatDistance(point,p)-5f);
            foreach(SpecialSite site in Sites)clearance=Mathf.Min(clearance,MathUtil.FlatDistance(point,site.Position)-site.Radius-8f);
            foreach(PropSpot prop in Props)clearance=Mathf.Min(clearance,MathUtil.FlatDistance(point,prop.Pos)-3f);
            if(Mathf.Abs(point.z-CanalZ)<CanalBandHalf+10f)return 0f;
            return Mathf.Max(0f,clearance);
        }
        private static float RectDistance04OC(RectF rect,Vector3 p)
        {float x=Mathf.Max(rect.X0-p.x,0f,p.x-rect.X1),z=Mathf.Max(rect.Z0-p.z,0f,p.z-rect.Z1);return Mathf.Sqrt(x*x+z*z);}
        public static float SegmentDistance04OC(Vector3 p,Vector3 a,Vector3 b)
        {Vector3 d=MathUtil.Flat(b-a);float t=d.sqrMagnitude<.0001f?0f:Mathf.Clamp01(Vector3.Dot(MathUtil.Flat(p-a),d)/d.sqrMagnitude);return MathUtil.FlatDistance(p,a+d*t);}
        private void PlanWildScenery04OC()
        {
            // A finite grid, jittered by integer seed. No random stream changes to original content or receipt order.
            for(int x=-740;x<=740;x+=36)for(int z=-740;z<=740;z+=36)
            {
                int seed=unchecked(Seed+x*73856093+z*19349663);uint h=unchecked((uint)seed);
                // Avalanche before choosing a role. A 36m grid leaves the low two raw seed bits constant.
                h^=h>>16;h=unchecked(h*2246822519u);h^=h>>13;
                Vector3 at=new Vector3(x+(h%13u)-6f,CurbHeight,z+((h/17u)%13u)-6f);
                if(InSettlement04OC(at))continue;
                float clearance=WildClearance04OC(at);if(clearance<5f)continue;
                int kind=(h%7u)==0u?2:(h%4u)==0u?1:0;
                if(kind==0 && clearance<12f)continue;
                float radius=Mathf.Min(clearance-1.5f,kind==2?10f:kind==1?4.5f:3f);
                if(radius<2.5f)continue;
                WildScenery04OC.Add(new WildScenery04OC{Position=at,Seed=seed,Region=Region04OC(at),Kind=kind,
                    Size=new Vector3(radius*1.4f,kind==2?14f+h%14u:kind==1?7f+h%6u:10f+h%9u,radius*1.4f)});
            }
            // Meaningful nearby relief in deliberately freed ground, not a decorative distant skyline.
            foreach(Vector3 p in new[]{new Vector3(-137,0,-5),new Vector3(-161,0,64),new Vector3(-181,0,108),new Vector3(-264,0,98),new Vector3(146,0,175)})
            {
                if(WildClearance04OC(p)<7f)continue;
                WildPatches04OC.Add(new WildPatch04OC{Area=new RectF(p.x-15,p.z-15,p.x+15,p.z+15),Seed=Seed+(int)p.x,Region=Region04OC(p),Height=5f});
            }
        }
        /// <summary>Height never exceeds the clearance-derived slope, so protected paths and all original receipts remain ground-admissible.</summary>
        public float WildHeight04OC(WildPatch04OC patch,Vector3 p)
        {
            float u=Mathf.Clamp01((p.x-patch.Area.X0)/patch.Area.Width),v=Mathf.Clamp01((p.z-patch.Area.Z0)/patch.Area.Depth);
            float bell=Mathf.Sin(u*Mathf.PI)*Mathf.Sin(v*Mathf.PI);
            float height=Mathf.Min(patch.Height,Mathf.Min(patch.Area.Width,patch.Area.Depth)*.16f)*bell*bell;
            return Mathf.Min(height,WildClearance04OC(p)*.22f);
        }
    }
}
