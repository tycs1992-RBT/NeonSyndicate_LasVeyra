using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    public sealed class WildPatch04OC
    {
        public RectF Area;
        public int Seed, Region;
        public float Height;
    }
    public sealed class WildStructure04OC
    {
        public int Seed, BlockId;
        public string Identity, Disposition;
        public RectF Before, After;
        public float BeforeHeight, AfterHeight;
    }
    /// <summary>Explicit revision 3, derived from the original owners and compact courts. Never mutates revision 2.</summary>
    public sealed partial class CityLayout
    {
        public bool Fractured04OC { get { return Revision04O == CityRevision04O.FracturedWilds04OC || Realms04OD; } }
        public readonly List<WildPatch04OC> WildPatches04OC = new List<WildPatch04OC>();
        public readonly List<WildStructure04OC> WildCoverage04OC = new List<WildStructure04OC>();
        public Vector3 RealmChallenge04OC { get { return Realms04OD ? CastellanCourt04OD : Fractured04OC ? new Vector3(-210f,CurbHeight,28f) : RealmThreshold04OB; } }
        public Vector3 AzureGlade04OC { get { return Realms04OD ? SovereignCourt04OD : new Vector3(-270f,CurbHeight,158f); } }
        public bool ResolveRealmAnchor04OC(string token, out Vector3 point)
        {
            point=Vector3.zero;if(!Gothic04OB)return false;
            if(GothicHeartH3 && token=="Realm:hearth-lens"){point=HearthLensH3;return true;}
            if (Frontier04OG && ResolveFrontierAnchor04OG(token,out point)) return true;
            if (Unbound04OE && ResolveUnboundAnchor04OE(token,out point)) return true;
            switch(token)
            {
                case "Realm:castellan":if(!Realms04OD)return false;point=RealmChallenge04OC;return true;
                case "Realm:sovereign":if(!Realms04OD)return false;point=AzureGlade04OC;return true;
                case "Realm:gallery":if(!Realms04OD)return false;point=RealmChallenge04OC+new Vector3(0,0,-18);return true;
                case "Realm:storm-shrine":if(!Realms04OD)return false;point=AzureGlade04OC+new Vector3(0,0,18);return true;
                case "Realm:threshold":point=RealmThreshold04OB;return true;
                case "Realm:grotto":point=RealmGrotto04OB;return true;
                case "Realm:observation":if(!Fractured04OC)return false;point=new Vector3(-25f,CurbHeight,-8f);return true;
                case "Realm:forge-scar":if(!Fractured04OC)return false;point=new Vector3(128f,CurbHeight,132f);return true;
                case "Realm:azure":if(!Fractured04OC)return false;point=AzureGlade04OC+Vector3.back*20f;return true;
                default:return false;
            }
        }
        private bool ApplyFracturedWilds04OC(IReadOnlyList<VenueBinding04C> venues)
        {
            if(Revision04O!=CityRevision04O.Legacy04N || !ApplyGothic04OB(venues))return false;
            Revision04O=CityRevision04O.FracturedWilds04OC;
            // Move the actual cave envelope and its corresponding lot, rather than only its marker.
            foreach(Block block in Blocks)foreach(Lot lot in block.Lots)foreach(Building b in lot.Buildings)
                if(b.GothicFamily04OB==8)
                {
                    Vector3 delta=RealmGrotto04OB-new Vector3(12f,CurbHeight,316f);
                    b.Footprint=Translate04OB(b.Footprint,delta);lot.Area=Translate04OB(lot.Area,delta);
                    block.Curb=lot.Area.Inset(-3f);block.Inner=lot.Area;
                }
            RealmRoutes04OB.RemoveAll(r=>r.Id=="crystal-threshold" || r.Id=="grotto-through-route" || r.Id=="grotto-return" || r.Id.StartsWith("neighbor-path/"));
            AddRealmRoute04OB("sanctuary-observatory",7f,new Vector3(12,0,92),new Vector3(28,0,126),new Vector3(25,0,164),new Vector3(45,0,180),new Vector3(68,0,186));
            AddRealmRoute04OB("shardwood-threshold",7f,new Vector3(-20,0,6),new Vector3(-38,0,30),new Vector3(-76,0,30),RealmThreshold04OB);
            AddRealmRoute04OB("grotto-through-route",5f,RealmThreshold04OB,new Vector3(-108,0,48),new Vector3(-110,0,60),RealmGrotto04OB,new Vector3(-110,0,85));
            AddRealmRoute04OB("grotto-return",5f,new Vector3(-110,0,85),new Vector3(-128,0,85),new Vector3(-128,0,48),RealmThreshold04OB);
            AddRealmRoute04OB("sanctuary-wilds",5f,new Vector3(-38,0,72),new Vector3(-38,0,92),new Vector3(-76,0,92),new Vector3(-110,0,85));
            AddRealmRoute04OB("amethyst-challenge",7f,RealmThreshold04OB,new Vector3(-158,0,28),RealmChallenge04OC);
            AddRealmRoute04OB("azure-expedition",6f,RealmChallenge04OC,new Vector3(-250,0,62),new Vector3(-270,0,102),AzureGlade04OC);
            AddRealmRoute04OB("azure-return",5f,AzureGlade04OC,new Vector3(-168,0,150),new Vector3(-128,0,136),new Vector3(-128,0,85));
            AddRealmRoute04OB("forge-scar",6f,new Vector3(75,0,96),new Vector3(100,0,92),new Vector3(128,0,104),new Vector3(128,0,132),new Vector3(152,0,152));
            AddRealmRoute04OB("first-phenomenon",5f,new Vector3(-8,0,-12),new Vector3(-25,0,-8),new Vector3(-20,0,6));
            HashSet<Building> service=new HashSet<Building>();foreach(VenueBinding04C v in venues)service.Add(v.Building);
            RealmBuildings04OB.Clear();
            foreach(Block block in Blocks)foreach(Lot lot in block.Lots)
            for(int i=lot.Buildings.Count-1;i>=0;i--)
            {
                Building b=lot.Buildings[i];Vector3 at=b.Footprint.Center(0f);
                WildStructure04OC row=new WildStructure04OC{Seed=b.Seed,BlockId=block.Id,Identity=lot.PoiName??"",Before=b.Footprint,BeforeHeight=b.Height};
                bool receipt=HoldsReceipt04O(b.Footprint) || b.Roof==RoofType.Helipad || b.Roof==RoofType.Antenna;
                bool named=lot.Poi!=PoiType.None || !string.IsNullOrEmpty(lot.PoiName) || !string.IsNullOrEmpty(b.Sign);
                if(service.Contains(b))row.Disposition="preserved real service owner, vaulted original host";
                else if(b.GothicFamily04OB==8)row.Disposition="natural terrain, hollow walk-through grotto";
                else if(receipt && !lot.CustomBuilt)
                {
                    bool stunt=false;foreach(StuntJumpDef jump in StuntJumps)if(b.Footprint.Inset(-4f).Contains(jump.Position))stunt=true;
                    if(!named && !stunt && b.Height>18f)
                    {
                        float oldHeight=b.Height;b.Height=10f+Math.Abs(b.Seed%3)*3f;
                        // New profile only: ordered receipt identity/x/z stay, roof-bearing y follows the actual landing.
                        foreach(List<Vector3> list in new[]{DataChipSpots,CrystalShardSpots,SpellStoneSpots,CreatureSpots,PickupSpots,RadioMastSpots})
                        for(int n=0;n<list.Count;n++)
                            if(b.Footprint.Inset(-.2f).Contains(list[n]) && list[n].y>=oldHeight-1f)
                                list[n]+=Vector3.up*(b.Height-oldHeight);
                        row.Disposition="deliberate scenic ruin, receipt identity retained on lowered new-profile landing";
                    }
                    else row.Disposition="protected original roof plane, fractured open mineral reliquary";
                    b.GothicFamily04OB=9;b.RebirthFamily04O=0;
                }
                else if(lot.CustomBuilt)row.Disposition="preserved real gameplay owner, custom canonical structure";
                else if(named)row.Disposition="preserved named gameplay owner, gothic silhouette";
                else if(!InSettlement04OC(at) || IntersectsWildRoute04OC(b.Footprint,3f))
                {
                    lot.Buildings.RemoveAt(i);row.Disposition="natural terrain, former unowned filler removed";
                    if(b.Footprint.Width>12f && b.Footprint.Depth>12f)WildPatches04OC.Add(new WildPatch04OC{Area=b.Footprint,Seed=b.Seed,Region=Region04OC(at),Height=3f+Math.Abs(b.Seed%7)});
                }
                else row.Disposition="intentionally inhabited compact settlement";
                row.After=lot.Buildings.Contains(b)?b.Footprint:new RectF();row.AfterHeight=lot.Buildings.Contains(b)?b.Height:0f;WildCoverage04OC.Add(row);
                if(lot.Buildings.Contains(b) && b.GothicFamily04OB>0)RealmBuildings04OB.Add(b);
            }
            // Nonpersistent grid furniture is not an excuse for blocked new routes. Saved ordered points stay untouched.
            Props.RemoveAll(p=>(!InSettlement04OC(p.Pos) && !ProtectedWildFixture04OC(p.Pos) &&
                (p.Kind==PropKind.StreetLamp||p.Kind==PropKind.Hydrant||p.Kind==PropKind.TrashCan||p.Kind==PropKind.NewsBox||p.Kind==PropKind.ParkingMeter||p.Kind==PropKind.SidewalkTree||p.Kind==PropKind.Palm||p.Kind==PropKind.Planter)) || IntersectsWildRoute04OC(new RectF(p.Pos.x-.8f,p.Pos.z-.8f,p.Pos.x+.8f,p.Pos.z+.8f),1.5f));
            foreach(List<Vector3> points in new[]{TreeSpots,PalmSpots,RockSpots,StreetLampSpots})points.RemoveAll(p=>(!InSettlement04OC(p) && (points!=StreetLampSpots || !ProtectedWildFixture04OC(p))) || IntersectsWildRoute04OC(new RectF(p.x-1,p.z-1,p.x+1,p.z+1),2f));
            Landscape04OB.RemoveAll(r=>IntersectsWildRoute04OC(new RectF(r.Position.x-r.Size.x*.6f,r.Position.z-r.Size.z*.6f,r.Position.x+r.Size.x*.6f,r.Position.z+r.Size.z*.6f),3f));
            BuildSpatialHashes();ReauthorWalks04OB(venues);BuildSpatialHashes();
            PlanWildScenery04OC();
            return true;
        }
        private bool ProtectedWildFixture04OC(Vector3 point)
        {
            if(NearAnyRoad(point,8f))return true;
            foreach(Poi poi in Pois)if(MathUtil.FlatDistance(point,poi.Position)<28f)return true;
            foreach(SpecialSite site in Sites)if(MathUtil.FlatDistance(point,site.Position)<site.Radius+8f)return true;
            foreach(List<Vector3> list in new[]{DataChipSpots,CrystalShardSpots,SpellStoneSpots,CreatureSpots,PickupSpots,RadioMastSpots})
                foreach(Vector3 receipt in list)if(MathUtil.FlatDistance(point,receipt)<6f)return true;
            return false;
        }
        public static bool InSettlement04OC(Vector3 p)
        {
            // A finite lived-in heart, not a map-wide tower suburb. Named remote sites retain their own gameplay owners.
            return p.x>=-100f && p.x<=140f && p.z>=-118f && p.z<=220f;
        }
        public static int Region04OC(Vector3 p){return p.x< -150f && p.z>95f?1:p.x>80f?2:0;}
        private bool IntersectsWildRoute04OC(RectF area,float margin)
        {
            foreach(RealmRoute04OB route in RealmRoutes04OB)for(int i=1;i<route.Points.Count;i++)
            {
                float length=Vector3.Distance(route.Points[i-1],route.Points[i]);int n=Mathf.Max(1,Mathf.CeilToInt(length/2f));
                for(int k=0;k<=n;k++)if(area.Inset(-route.Width*.5f-margin).Contains(Vector3.Lerp(route.Points[i-1],route.Points[i],k/(float)n)))return true;
            }
            foreach(Vector3 center in new[]{RealmThreshold04OB,RealmChallenge04OC,AzureGlade04OC})if(area.Inset(-21f).Contains(center))return true;
            return false;
        }
    }
}
