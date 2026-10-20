using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    public sealed class RealmRock04OB
    { public Vector3 Position, Size; public int Seed; public bool Teal; }
    public sealed class RealmRoute04OB
    {
        public string Id;
        public float Width=7f;
        public readonly List<Vector3> Points=new List<Vector3>();
    }
    public sealed class RealmRelocation04OB
    {
        public string Id;
        public Vector3 Before, After;
        public float OldYaw, NewYaw;
    }
    public sealed class RealmCoverage04OB
    {
        public int Seed, SourceIndex;
        internal Building Source;
        public string Identity, Disposition;
        public RectF Before, After;
    }
    /// <summary>Revision 2 only. Retains original identities, lists and runtime owners; re-authors their spatial policy.</summary>
    public sealed partial class CityLayout
    {
        public bool Gothic04OB { get { return Revision04O==CityRevision04O.GothicRealm04OB || Fractured04OC; } }
        public readonly List<RealmRock04OB> Landscape04OB=new List<RealmRock04OB>();
        public readonly List<RealmRoute04OB> RealmRoutes04OB=new List<RealmRoute04OB>();
        public readonly List<RealmRelocation04OB> Relocations04OB=new List<RealmRelocation04OB>();
        public readonly List<RealmCoverage04OB> Coverage04OB=new List<RealmCoverage04OB>();
        public readonly List<Building> RealmBuildings04OB=new List<Building>();
        public int OriginalBlockCount04OB { get; private set; }
        public Vector3 RealmThreshold04OB { get { return Fractured04OC ? new Vector3(-108f,CurbHeight,28f) : new Vector3(12f,CurbHeight,288f); } }
        public Vector3 RealmGrotto04OB { get { return Fractured04OC ? new Vector3(-110f,CurbHeight,74f) : new Vector3(12f,CurbHeight,316f); } }
        public Vector3 RealmCourt04OB { get { return new Vector3(0f,CurbHeight,34f); } }

        private bool ApplyGothic04OB(IReadOnlyList<VenueBinding04C> venues)
        {
            if(Revision04O!=CityRevision04O.Legacy04N || venues==null || venues.Count!=12)return false;
            if(!ResolveCompactHosts04OB(venues))return false;
            HashSet<string> unique=new HashSet<string>(StringComparer.Ordinal);
            HashSet<Lot> hosts=new HashSet<Lot>();
            foreach(Block block in Blocks)foreach(Lot lot in block.Lots)hosts.Add(lot);
            foreach(VenueBinding04C venue in venues)
                if(venue==null || venue.Definition==null || venue.Building==null || venue.Lot==null ||
                    !hosts.Contains(venue.Lot) || !unique.Add(venue.Definition.Id))return false;
            // All receipts are inspected before mutation. Revisions 0 and 1 never reach this method.
            OriginalBlockCount04OB=Blocks.Count;
            Dictionary<Lot,VenueBinding04C> selected=new Dictionary<Lot,VenueBinding04C>();
            foreach(VenueBinding04C venue in venues)selected.Add(venue.Lot,venue);
            foreach(Block block in Blocks)foreach(Lot lot in block.Lots)foreach(Building building in lot.Buildings)
                Coverage04OB.Add(new RealmCoverage04OB{Source=building,SourceIndex=Coverage04OB.Count,Seed=building.Seed,Identity=lot.PoiName??"",Before=building.Footprint,After=building.Footprint,Disposition="pending"});
            // Named sites, POIs and ordered collectible lists remain. Only their owning footprint is translated when moved.
            foreach(VenueBinding04C venue in venues)MoveHost04OB(venue);
            ClearArrival04OB(selected);
            Revision04O=CityRevision04O.GothicRealm04OB;
            ReauthorRoads04OB();
            ReauthorMasses04OB(selected);
            AuthorCourts04OB();
            ClearGrottoUtilities04OB();
            FurnishCourts04OB();
            BuildSpatialHashes();
            ReauthorWalks04OB(venues);
            BuildSpatialHashes();
            PlanLandscape04OB();
            foreach(RealmCoverage04OB item in Coverage04OB)
            {
                Building found=null;Lot owner=null;
                foreach(Block block in Blocks)foreach(Lot lot in block.Lots)foreach(Building b in lot.Buildings)if(b==item.Source){found=b;owner=lot;break;}
                if(found!=null)
                {
                    item.After=found.Footprint;
                    item.Disposition=found.GothicFamily04OB>0?"gothic family "+found.GothicFamily04OB:
                        FindHost04OB(venues,found)!=null?"original service with rebuilt vaulted shell":
                        found.RebirthFamily04O==8?"transformed supported receipt/helipad/antenna roof":
                        owner.CustomBuilt?"retained custom-built canonical structure":"retained low special/receipt structure";
                }
                else item.Disposition="noninteractive filler consolidated into authored settlement and mineral landscape";
            }
            return true;
        }
        private bool ResolveCompactHosts04OB(IReadOnlyList<VenueBinding04C> venues)
        {
            // The old nearby selector may legitimately leave a noncanonical venue unbound on another seed.
            // In a new geography, select one unused original parcel rather than copying a service or loosening old profiles.
            HashSet<Building> used=new HashSet<Building>();HashSet<Lot> lots=new HashSet<Lot>();HashSet<string> ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(Block b in Blocks)foreach(Lot l in b.Lots)lots.Add(l);
            foreach(VenueBinding04C v in venues)
            {
                if(v==null || !InhabitedCityBindings04C.ValidDefinition(v.Definition) || !ids.Add(v.Definition.Id) || (v.Lot!=null&&!lots.Contains(v.Lot)))return false;
                if(v.Building!=null)used.Add(v.Building);
            }
            foreach(VenueBinding04C v in venues)
            {
                if(v.Building!=null)continue;bool canonical=!string.IsNullOrEmpty(v.Definition.PoiName);
                Poi poi=canonical?FindPoi(v.Definition.PoiName):null;Building chosen=null;Lot owner=null;float score=float.MaxValue;
                Vector3 desired=poi!=null?poi.Position:FindPoi("Veyra Lofts").Position;
                foreach(Block block in Blocks)foreach(Lot lot in block.Lots)
                {
                    if(lot.CustomBuilt || (canonical?lot.PoiName!=v.Definition.PoiName:lot.Poi!=PoiType.None||!string.IsNullOrEmpty(lot.PoiName)))continue;
                    foreach(Building b in lot.Buildings)
                    {
                        if(used.Contains(b) || (canonical ? !b.Footprint.Valid || b.Roof==RoofType.Helipad || b.Roof==RoofType.Antenna : !InhabitedCityBindings04C.EligibleFor(v.Definition,b)))continue;
                        float d=MathUtil.FlatSqrDistance(b.Footprint.Center(0f),desired);if(d>=score)continue;score=d;chosen=b;owner=lot;
                    }
                }
                if(chosen==null)return false;
                v.Lot=owner;v.Building=chosen;v.Poi=poi;v.Outward=canonical?poi.Facing:owner.Facing;
                float width,depth,yaw;Vector3 origin;InhabitedCityBindings04C.Door(chosen.Footprint,v.Outward,out origin,out width,out depth,out yaw);
                v.Origin=v.Entrance=origin;v.Width=width;v.Depth=depth;v.Yaw=yaw;v.Outside=origin+v.Outward*3f;used.Add(chosen);
            }
            return true;
        }
        private static VenueBinding04C FindHost04OB(IReadOnlyList<VenueBinding04C> venues,Building b)
        { foreach(VenueBinding04C v in venues)if(v.Building==b)return v;return null; }
        private void MoveHost04OB(VenueBinding04C v)
        {
            Vector3 door;Vector3 outward;float width=30f,depth=30f,height=13.5f;
            switch(v.Definition.Id)
            {
                case "lofts":door=new Vector3(0f,CurbHeight,-34f);outward=Vector3.forward;break;
                case "dispatch":door=new Vector3(-46f,CurbHeight,4f);outward=Vector3.right;break;
                case "clinic":door=new Vector3(-42f,CurbHeight,72f);outward=Vector3.right;break;
                case "forge":door=new Vector3(75f,CurbHeight,100f);outward=Vector3.back;height=16f;break;
                case "pressure":door=new Vector3(38f,CurbHeight,12f);outward=Vector3.left;break;
                case "bondkeeper":door=new Vector3(38f,CurbHeight,-46f);outward=Vector3.left;break;
                case "office":door=new Vector3(-65f,CurbHeight,-65f);outward=Vector3.right;break;
                case "archive":door=new Vector3(-6f,CurbHeight,122f);outward=Vector3.back;depth=48f;height=22f;break;
                case "inn":door=new Vector3(-68f,CurbHeight,115f);outward=Vector3.right;break;
                case "garage":door=new Vector3(118f,CurbHeight,-70f);outward=Vector3.back;break;
                case "depot":door=new Vector3(110f,CurbHeight,40f);outward=Vector3.back;break;
                default:door=new Vector3(68f,CurbHeight,190f);outward=Vector3.back;height=21f;break;
            }
            RectF old=v.Building.Footprint;Vector3 before=v.Origin;float oldYaw=v.Yaw,oldHeight=v.Building.Height;
            float yaw=outward.x>.5f?270f:outward.x<-.5f?90f:outward.z>.5f?180f:0f;
            Vector3 a=InhabitedCityBindings04C.LocalToWorld(door,yaw,new Vector3(-width*.5f,0f,0f));
            Vector3 b=InhabitedCityBindings04C.LocalToWorld(door,yaw,new Vector3(width*.5f,0f,depth));
            RectF target=new RectF(a.x,a.z,b.x,b.z);
            foreach(List<Vector3> points in new[]{DataChipSpots,CrystalShardSpots,SpellStoneSpots,CreatureSpots,PickupSpots,RadioMastSpots})
                for(int i=0;i<points.Count;i++)if(old.Contains(points[i]))
                {
                    Vector3 local=InhabitedCityBindings04C.WorldToLocal(before,oldYaw,points[i]);
                    // Stable list indices and normalized roof position. Receipt remains on a real retained-height landing.
                    local.x=Mathf.Clamp(local.x/Mathf.Max(1f,v.Width)*width,-width*.45f,width*.45f);
                    local.z=Mathf.Clamp(local.z/Mathf.Max(1f,v.Depth)*depth,1f,depth-1f);
                    if(local.y>oldHeight-2f)
                    {
                        v.Building.RoofPad04OB=true;
                        local.x=-11.75f;local.z=depth*.72f+(i%3-1)*1.2f;
                        local.y=height+(local.y-oldHeight);
                    }
                    points[i]=InhabitedCityBindings04C.LocalToWorld(door,yaw,local);
                }
            foreach(SpecialSite site in Sites)if(old.Contains(site.Position))
            {Vector3 local=InhabitedCityBindings04C.WorldToLocal(before,oldYaw,site.Position);site.Position=InhabitedCityBindings04C.LocalToWorld(door,yaw,local);}
            for(int i=0;i<StuntJumps.Count;i++)if(old.Contains(StuntJumps[i].Position))
            {StuntJumpDef jump=StuntJumps[i];Vector3 local=InhabitedCityBindings04C.WorldToLocal(before,oldYaw,jump.Position);local.y+=height-oldHeight;jump.Position=InhabitedCityBindings04C.LocalToWorld(door,yaw,local);jump.Yaw+=yaw-oldYaw;StuntJumps[i]=jump;}
            foreach(Block block in Blocks)
            {
                if(!block.Lots.Remove(v.Lot))continue;
                // A service can occupy one building in a larger lot. Keep the other actual buildings and roof receipts.
                Lot remnant=new Lot{Area=v.Lot.Area,Use=v.Lot.Use,Seed=v.Lot.Seed,Entrance=v.Lot.Entrance,Facing=v.Lot.Facing};
                foreach(Building original in v.Lot.Buildings)if(original!=v.Building)remnant.Buildings.Add(original);
                if(remnant.Buildings.Count>0)block.Lots.Add(remnant);
                break;
            }
            // The original host object moves. There is no duplicate clerk, stock, service or reward ledger.
            v.Lot.Buildings.Clear();v.Lot.Buildings.Add(v.Building);v.Building.Footprint=target;v.Building.Height=height;v.Building.Tiers=1;
            v.Building.GothicFamily04OB=0;
            v.Lot.Area=target.Inset(-2f);v.Lot.Entrance=door;v.Lot.Facing=outward;
            Block destination=new Block{Id=Blocks.Count,Curb=target.Inset(-3f),Inner=target.Inset(-1f),District=v.Poi!=null?v.Poi.District:CityLayout.DistrictAt(door),Seed=v.Lot.Seed};
            destination.Lots.Add(v.Lot);Blocks.Add(destination);
            v.Origin=v.Entrance=door;v.Yaw=yaw;v.Width=width;v.Depth=depth;v.Outward=outward;v.Outside=door+outward*4f;
            if(v.Poi!=null){v.Poi.Position=door+outward*3.25f;v.Poi.Facing=outward;v.Poi.BlockId=destination.Id;}
            Relocations04OB.Add(new RealmRelocation04OB{Id=v.Definition.Id,Before=before,After=door,OldYaw=oldYaw,NewYaw=yaw});
        }
        private void ClearArrival04OB(Dictionary<Lot,VenueBinding04C> selected)
        {
            RectF clear=new RectF(-140f,-140f,145f,325f);
            // Move any other canonical host out of the authored courts, with all ordered point receipts attached to it.
            int extra=0;
            for(int i=0;i<OriginalBlockCount04OB;i++)foreach(Lot lot in Blocks[i].Lots)
            {
                if(!lot.Area.Overlaps(clear,0f))continue;
                if(lot.Poi!=PoiType.None || !string.IsNullOrEmpty(lot.PoiName) || lot.CustomBuilt || HoldsReceipt04O(lot.Area))
                {
                    Vector3 destination=DetachedCourt04OB(lot,clear,extra++);
                    Vector3 delta=destination-lot.Area.Center(0f);
                    RectF old=lot.Area;lot.Area=Translate04OB(old,delta);lot.Entrance+=delta;
                    foreach(Building b in lot.Buildings)b.Footprint=Translate04OB(b.Footprint,delta);
                    foreach(Poi p in Pois)if(p.BlockId==i && (old.Inset(-8f).Contains(p.Position)||p.Name==lot.PoiName))p.Position+=delta;
                    foreach(List<Vector3> points in new[]{DataChipSpots,CrystalShardSpots,SpellStoneSpots,CreatureSpots,PickupSpots,ParkingSpots,StreetLampSpots,TreeSpots,PalmSpots,RockSpots,RadioMastSpots})
                        for(int n=0;n<points.Count;n++)if(old.Contains(points[n]))points[n]+=delta;
                    foreach(SpecialSite site in Sites)if(old.Contains(site.Position))site.Position+=delta;
                    Relocations04OB.Add(new RealmRelocation04OB{Id=string.IsNullOrEmpty(lot.PoiName)?"receipt-lot/"+lot.Seed:lot.PoiName,Before=old.Center(CurbHeight),After=lot.Area.Center(CurbHeight)});
                    // Detached original block remains its stable ID. Its new lot gets a matching appended spatial block.
                    Block moved=new Block{Id=Blocks.Count,Curb=lot.Area.Inset(-3f),Inner=lot.Area,District=Blocks[i].District,Seed=lot.Seed};
                    moved.Lots.Add(lot);Blocks.Add(moved);
                    foreach(Poi poi in Pois)if(poi.Name==lot.PoiName || (poi.BlockId==i && lot.Area.Inset(-8f).Contains(poi.Position)))poi.BlockId=moved.Id;
                }
                else {lot.Buildings.Clear();lot.Use=LotUse.Empty;}
            }
            // Clear only noninteractive filler that would overlap a relocated protected receipt parcel.
            for(int i=0;i<OriginalBlockCount04OB;i++)foreach(Lot lot in Blocks[i].Lots)
            {
                if(lot.Poi!=PoiType.None || !string.IsNullOrEmpty(lot.PoiName) || lot.CustomBuilt || HoldsReceipt04O(lot.Area))continue;
                foreach(Block moved in Blocks.GetRange(OriginalBlockCount04OB,Blocks.Count-OriginalBlockCount04OB))
                    foreach(Lot placed in moved.Lots)if(lot.Area.Overlaps(placed.Area,2f))lot.Buildings.Clear();
            }
            // Remove shared moved lot references from old blocks, preserving block indices.
            for(int i=0;i<OriginalBlockCount04OB;i++)Blocks[i].Lots.RemoveAll(lot=>Blocks.FindIndex(OriginalBlockCount04OB,b=>b.Lots.Contains(lot))>=0);
            Props.RemoveAll(p=>clear.Contains(p.Pos));
            foreach(List<Vector3> points in new[]{StreetLampSpots,TreeSpots,PalmSpots,RockSpots})points.RemoveAll(p=>clear.Contains(p));
            // Unowned parking is not serialized by index. Keep it outside pedestrian courts and the threshold arena.
            for(int i=ParkingSpots.Count-1;i>=0;i--)if(clear.Contains(ParkingSpots[i])){ParkingSpots.RemoveAt(i);if(i<ParkingYaws.Count)ParkingYaws.RemoveAt(i);}
            ParkingSpots.Add(new Vector3(132f,0f,-104f));ParkingYaws.Add(0f);
        }
        private Vector3 DetachedCourt04OB(Lot moving,RectF clear,int ordinal)
        {
            for(int attempt=0;attempt<120;attempt++)
            {
                int index=(attempt+ordinal*3)%120;
                Vector3 candidate=new Vector3(-250f-(index%4)*140f,0f,-220f-(index/4)*145f);
                if(candidate.z< -780f)candidate=new Vector3(-250f-(index%4)*140f,0f,390f+(index/4-4)*130f);
                if(Mathf.Abs(candidate.z)>760f)continue;
                RectF test=Translate04OB(moving.Area,candidate-moving.Area.Center(0f)).Inset(-3f);
                bool blocked=false;
                foreach(Block block in Blocks)foreach(Lot lot in block.Lots)
                {
                    if(lot==moving || clear.Overlaps(lot.Area,0f))continue;
                    if((lot.Poi!=PoiType.None || !string.IsNullOrEmpty(lot.PoiName) || lot.CustomBuilt || HoldsReceipt04O(lot.Area)) && test.Overlaps(lot.Area,3f))blocked=true;
                }
                foreach(SpecialSite site in Sites)if(test.Inset(-Mathf.Min(25f,site.Radius)).Contains(site.Position))blocked=true;
                if(!blocked)return candidate;
            }
            throw new InvalidOperationException("No safe court for protected parcel "+moving.Seed+". Compact preview cannot relocate this seed.");
        }
        private static RectF Translate04OB(RectF r,Vector3 d){return new RectF(r.X0+d.x,r.Z0+d.z,r.X1+d.x,r.Z1+d.z);}
        private void ReauthorMasses04OB(Dictionary<Lot,VenueBinding04C> selected)
        {
            foreach(Block block in Blocks)foreach(Lot lot in block.Lots)
            {
                if(selected.ContainsKey(lot))continue;
                foreach(Building b in lot.Buildings)
                {
                    if(lot.CustomBuilt){continue;}
                    if(b.Roof==RoofType.Helipad || b.Roof==RoofType.Antenna){if(b.Height>=12f)b.RebirthFamily04O=8;continue;}
                    if(HoldsReceipt04O(b.Footprint))
                    {
                        if(b.Height>=12f)b.RebirthFamily04O=8;
                        else {b.GothicFamily04OB=7;RealmBuildings04OB.Add(b);}
                        continue;
                    }
                    b.GothicFamily04OB=1+Math.Abs(b.Seed%6);
                    b.Height=GothicRealmGeometry04OB.FamilyHeight(b.GothicFamily04OB);
                    // Large ordinary office parcels become human-scale compounds around a true central void.
                    if(lot.Poi==PoiType.None && string.IsNullOrEmpty(lot.PoiName))
                    {
                        Vector3 c=b.Footprint.Center(0f);float w=Mathf.Min(24f,b.Footprint.Width),d=Mathf.Min(28f,b.Footprint.Depth);
                        b.Footprint=new RectF(c.x-w*.5f,c.z-d*.5f,c.x+w*.5f,c.z+d*.5f);
                        if(Mathf.Abs(c.x)>560f || Mathf.Abs(c.z)>540f){b.GothicFamily04OB=6;b.Height=7f;}
                    }
                    RealmBuildings04OB.Add(b);
                }
            }
            // Deliberately placed residences and work yards, not a repeated modern block grid.
            Vector3[] homes={new Vector3(-112,0,-23),new Vector3(-108,0,49),new Vector3(-106,0,171),new Vector3(105,0,-13),new Vector3(113,0,158),new Vector3(-34,0,213),new Vector3(20,0,223)};
            for(int i=0;i<homes.Length;i++)
            {
                Vector3 c=homes[i];int family=i%6+1;Building b=new Building{Seed=Seed+900000+i,Footprint=new RectF(c.x-11f,c.z-13f,c.x+11f,c.z+13f),Height=GothicRealmGeometry04OB.FamilyHeight(family),GothicFamily04OB=family,Tiers=1};
                Lot lot=new Lot{Seed=b.Seed,Area=b.Footprint.Inset(-2f),Use=LotUse.House};lot.Buildings.Add(b);
                Block block=new Block{Id=Blocks.Count,Curb=lot.Area.Inset(-2f),Inner=lot.Area,District=District.LasVeyraCore,Seed=b.Seed};block.Lots.Add(lot);Blocks.Add(block);RealmBuildings04OB.Add(b);
            }
            // This surface grotto occupies the already-cleared threshold court, not an old quest/receipt parcel.
            Building cave=new Building{Seed=Seed+900100,Footprint=new RectF(-1f,306f,25f,324f),Height=12f,GothicFamily04OB=8,Tiers=1};
            Lot caveLot=new Lot{Seed=cave.Seed,Area=cave.Footprint,Use=LotUse.House};caveLot.Buildings.Add(cave);
            Block caveBlock=new Block{Id=Blocks.Count,Curb=cave.Footprint.Inset(-2f),Inner=cave.Footprint,District=District.LasVeyraCore,Seed=cave.Seed};
            caveBlock.Lots.Add(caveLot);Blocks.Add(caveBlock);RealmBuildings04OB.Add(cave);
        }
        private void ReauthorRoads04OB()
        {
            // Keep the canal bridges, highway, wilderness and outer access. Remove the surplus inner street grid itself.
            for(int i=0;i<Edges.Count;i++)
            {
                RoadEdge edge=Edges[i];Vector3 a=Nodes[edge.A].Pos,b=Nodes[edge.B].Pos;
                if(edge.Kind!=RoadKind.Highway && edge.Kind!=RoadKind.Rural && !edge.HasBridge &&
                    Mathf.Abs(a.x)<790f && Mathf.Abs(a.z)<790f && Mathf.Abs(b.x)<790f && Mathf.Abs(b.z)<790f)Edges[i]=null;
            }
            // Live Edges may contain nulls until FinalizeNodes. New IDs append without using removed edges.
            Vector3[] ring={new Vector3(-150,0,-150),new Vector3(155,0,-150),new Vector3(155,0,252),new Vector3(-150,0,252)};
            int[] ids=new int[4];for(int i=0;i<4;i++)ids[i]=RealmNode04OB(ring[i]);
            for(int i=0;i<4;i++)AddEdge(ids[i],ids[(i+1)%4],RoadKind.Street,"Mineral service circuit");
            foreach(int i in new[]{0,1,2,3})
            {
                Vector3 start=ring[i];RoadNode nearest=null;float best=float.MaxValue;
                foreach(RoadEdge edge in Edges)
                {
                    if(edge==null || edge.Kind!=RoadKind.Highway)continue;
                    foreach(int end in new[]{edge.A,edge.B})
                    {
                        Vector3 p=Nodes[end].Pos;if(i<2?p.z>-900f:p.z<900f)continue;
                        float score=MathUtil.FlatSqrDistance(p,start);if(score<best){best=score;nearest=Nodes[end];}
                    }
                }
                if(nearest==null)continue;
                Vector3 elbow=new Vector3(start.x,0f,nearest.Pos.z);
                int bend=RealmNode04OB(elbow);RoadEdge stem=AddEdge(ids[i],bend,RoadKind.Rural,"Crystal realm approach");
                if(Mathf.Min(start.z,elbow.z)<CanalZ-CanalHalfWidth && Mathf.Max(start.z,elbow.z)>CanalZ+CanalHalfWidth){stem.HasBridge=true;stem.BridgeZ0=CanalZ-CanalBandHalf;stem.BridgeZ1=CanalZ+CanalBandHalf;}
                if(MathUtil.FlatDistance(elbow,nearest.Pos)>1f)AddEdge(bend,nearest.Id,RoadKind.Rural,"Realm beltway return");
            }
            FinalizeNodes();Lanes.Clear();BuildLaneGraph();
            // Old road props cannot float over a new courtyard or block a new vehicle approach.
            Props.RemoveAll(p=>NearAnyRoad(p.Pos,1.5f) && p.Kind!=PropKind.StreetLamp);
            foreach(Block block in Blocks)foreach(Lot lot in block.Lots)if(lot.Poi==PoiType.None && string.IsNullOrEmpty(lot.PoiName) && !HoldsReceipt04O(lot.Area))
                lot.Buildings.RemoveAll(b=>NearAnyRoad(b.Footprint.Center(0f),Mathf.Max(b.Footprint.Width,b.Footprint.Depth)*.55f));
        }
        private int RealmNode04OB(Vector3 point)
        {int old=FindNodeAt(point.x,point.z);if(old>=0)return old;int id=Nodes.Count;Nodes.Add(new RoadNode{Id=id,Pos=point});return id;}
        private void AuthorCourts04OB()
        {
            AddRealmRoute04OB("home-court",9f,new Vector3(0,0,-30),new Vector3(-8,0,-12),new Vector3(0,0,14),new Vector3(0,0,38));
            AddRealmRoute04OB("dispatch-clinic",7f,new Vector3(-42,0,4),new Vector3(-20,0,6),new Vector3(-12,0,34),new Vector3(-22,0,63),new Vector3(-38,0,72));
            AddRealmRoute04OB("home-dispatch",7f,new Vector3(-8,0,-12),new Vector3(-20,0,6));
            AddRealmRoute04OB("service-crossing",7f,new Vector3(-20,0,6),new Vector3(20,0,12));
            AddRealmRoute04OB("court-crossing",7f,new Vector3(-12,0,34),new Vector3(0,0,38));
            AddRealmRoute04OB("garage-foot-return",7f,new Vector3(118,0,-112),new Vector3(24,0,-112),new Vector3(24,0,-82),new Vector3(24,0,-20),new Vector3(0,0,14));
            AddRealmRoute04OB("pressure-return",7f,new Vector3(34,0,12),new Vector3(20,0,12),new Vector3(0,0,38));
            AddRealmRoute04OB("sanctuary-approach",8f,new Vector3(0,0,38),new Vector3(12,0,66),new Vector3(12,0,92),new Vector3(-6,0,118));
            AddRealmRoute04OB("forge-market",8f,new Vector3(12,0,66),new Vector3(42,0,68),new Vector3(74,0,78),new Vector3(75,0,96));
            AddRealmRoute04OB("crystal-threshold",8f,new Vector3(12,0,92),new Vector3(28,0,126),new Vector3(25,0,164),new Vector3(8,0,190),new Vector3(0,0,252),new Vector3(12,0,288));
            AddRealmRoute04OB("grotto-through-route",4f,new Vector3(12,0,288),new Vector3(12,0,304),new Vector3(12,0,316),new Vector3(12,0,327));
            AddRealmRoute04OB("grotto-return",4f,new Vector3(12,0,327),new Vector3(-3,0,327),new Vector3(-3,0,304),new Vector3(12,0,288));
            AddRealmRoute04OB("bondkeeper",7f,new Vector3(34,0,-46),new Vector3(18,0,-40),new Vector3(18,0,-26),new Vector3(0,0,-30));
            AddRealmRoute04OB("office",7f,new Vector3(-61,0,-65),new Vector3(-42,0,-60),new Vector3(-25,0,-30),new Vector3(0,0,-30));
            AddRealmRoute04OB("garage",9f,new Vector3(118,0,-74),new Vector3(118,0,-112),new Vector3(155,0,-112));
            AddRealmRoute04OB("depot",7f,new Vector3(110,0,36),new Vector3(96,0,30),new Vector3(42,0,38),new Vector3(0,0,38));
            AddRealmRoute04OB("inn",7f,new Vector3(-64,0,115),new Vector3(-42,0,110),new Vector3(-38,0,72));
            AddRealmRoute04OB("observatory",7f,new Vector3(68,0,186),new Vector3(45,0,180),new Vector3(25,0,164));
        }
        private void ClearGrottoUtilities04OB()
        {
            // The former grid left a hydrant in the cave's north mouth. Keep its prop index,
            // but move this demonstrated nonpersistent street fixture to the actual service roadside.
            RectF approach=new RectF(-7f,300f,31f,334f);int placed=0;
            for(int i=0;i<Props.Count;i++)
            {
                PropSpot prop=Props[i];if(prop.Kind!=PropKind.Hydrant || !approach.Contains(prop.Pos))continue;
                for(int n=placed;n<12;n++)
                {
                    Vector3 target=new Vector3(145f,CurbHeight,242f-n*4f);
                    if(IsInsideBuildingRaw(target,1f))continue;
                    bool occupied=false;for(int j=0;j<Props.Count;j++)if(j!=i && MathUtil.FlatDistance(Props[j].Pos,target)<2.8f){occupied=true;break;}
                    if(occupied)continue;
                    prop.Pos=target;Props[i]=prop;placed=n+1;_propCountForRoutes04O=-1;break;
                }
            }
        }
        private void FurnishCourts04OB()
        {
            Vector3[] benches={new Vector3(-30,0,30),new Vector3(-30,0,44),new Vector3(34,0,87),new Vector3(42,0,88)};
            foreach(Vector3 at in benches)Props.Add(new PropSpot{Pos=new Vector3(at.x,CurbHeight,at.z),Kind=PropKind.Bench,Yaw=90f,Variant=1});
            foreach(Vector3 at in new[]{new Vector3(-30,0,37),new Vector3(34,0,96),new Vector3(50,0,89)})
                Props.Add(new PropSpot{Pos=new Vector3(at.x,CurbHeight,at.z),Kind=PropKind.Planter,Variant=1});
            foreach(Vector3 at in new[]{new Vector3(-31,0,51),new Vector3(54,0,86)})
            {
                Vector3 point=new Vector3(at.x,CurbHeight,at.z);Props.Add(new PropSpot{Pos=point,Kind=PropKind.StreetLamp,Variant=1});
                StreetLampSpots.Add(point); // The existing pooled light owner retains its cap and day/night policy.
            }
        }
        private void AddRealmRoute04OB(string id,float width,params Vector3[] points)
        {RealmRoute04OB route=new RealmRoute04OB{Id=id,Width=width};foreach(Vector3 p in points)route.Points.Add(new Vector3(p.x,CurbHeight,p.z));RealmRoutes04OB.Add(route);}
        private void ReauthorWalks04OB(IReadOnlyList<VenueBinding04C> venues)
        {
            // Preserve outer path identities, but reject any formerly valid link now crossing a moved solid.
            foreach(Block block in Blocks)block.WalkNodes.Clear();
            WalkNodes.Clear();WalkLinks.Clear();BuildWalkGraph();BuildSpatialHashes();
            int oldNodeCount=WalkNodes.Count;
            List<WalkLink> retained=new List<WalkLink>();
            foreach(WalkLink link in WalkLinks)
                if(ClearPassage04O(WalkNodes[link.A].Pos,WalkNodes[link.B].Pos,.65f))retained.Add(link);
            foreach(WalkNode node in WalkNodes)node.Links.Clear();WalkLinks.Clear();
            foreach(WalkLink old in retained)AddWalkLink(old.A,old.B,old.Crosswalk,-1,old.CrossesNSRoad);
            foreach(RealmRoute04OB route in RealmRoutes04OB)
            {
                int previous=-1;
                foreach(Vector3 point in route.Points)
                {
                    int id=RealmWalkNode04OB(point);
                    if(previous>=0 && ClearPassage04O(WalkNodes[previous].Pos,point,1.8f))AddWalkLink(previous,id,false,-1,false);
                    previous=id;
                }
            }
            // Bind real doors to the closest admitted courtyard endpoint, not a duplicate nearby service.
            foreach(VenueBinding04C venue in venues)
            {
                int id=RealmWalkNode04OB(venue.Outside);int best=-1;float distance=float.MaxValue;
                foreach(WalkNode node in WalkNodes)
                {
                    float d=MathUtil.FlatSqrDistance(node.Pos,venue.Outside);
                    if(node.Id==id || node.Links.Count==0 || d>distance || d>65f*65f || !ClearPassage04O(node.Pos,venue.Outside,.7f))continue;
                    distance=d;best=node.Id;
                }
                if(best>=0)AddWalkLink(id,best,false,-1,false);
            }
            // Join the authored streets to nearby surviving paths. Every added graph link is physically paved too.
            for(int id=oldNodeCount;id<WalkNodes.Count;id++)
            {
                int best=-1;float distance=18f*18f;
                for(int other=0;other<oldNodeCount;other++)
                {
                    if(WalkNodes[other].Links.Count==0)continue;
                    float d=MathUtil.FlatSqrDistance(WalkNodes[id].Pos,WalkNodes[other].Pos);
                    if(d<.04f || d>=distance || !ClearPassage04O(WalkNodes[id].Pos,WalkNodes[other].Pos,1.8f))continue;
                    bool linked=false;foreach(int link in WalkNodes[id].Links)if(WalkLinks[link].A==other || WalkLinks[link].B==other)linked=true;
                    if(linked)continue;distance=d;best=other;
                }
                if(best<0)continue;
                Vector3 a=WalkNodes[id].Pos,b=WalkNodes[best].Pos;RoadEdge road=RoadAt((a+b)*.5f,0f);
                AddWalkLink(id,best,road!=null,-1,road!=null&&!road.AlongX);
                AddRealmRoute04OB("neighbor-path/"+id,4f,a,b);
            }
            CompactWalkGraph04OB();
            BuildSpatialHashes();
        }
        private void CompactWalkGraph04OB()
        {
            // Walk node IDs are session-local, never saved. The old civilian spawner can select isolated nodes, so remove them.
            Dictionary<int,int> map=new Dictionary<int,int>();List<WalkNode> active=new List<WalkNode>();
            foreach(WalkNode node in WalkNodes)if(node.Links.Count>0){map.Add(node.Id,active.Count);active.Add(node);}
            List<WalkLink> links=new List<WalkLink>(WalkLinks);
            foreach(Block block in Blocks)
            {
                List<int> kept=new List<int>();foreach(int old in block.WalkNodes){int next;if(map.TryGetValue(old,out next))kept.Add(next);}
                block.WalkNodes.Clear();block.WalkNodes.AddRange(kept);
            }
            WalkNodes.Clear();WalkNodes.AddRange(active);WalkLinks.Clear();
            for(int i=0;i<WalkNodes.Count;i++){WalkNodes[i].Id=i;WalkNodes[i].Links.Clear();}
            foreach(WalkLink link in links)AddWalkLink(map[link.A],map[link.B],link.Crosswalk,link.SignalNode,link.CrossesNSRoad);
        }
        private void PlanLandscape04OB()
        {
            for(int i=0;i<6;i++)
            {
                Vector3 proposed=new Vector3(i<3?-133f:137f,CurbHeight,-22f+(i%3)*112f);
                foreach(float offset in new[]{0f,9f,-9f,18f,-18f})
                {
                    Vector3 at=proposed+Vector3.forward*offset;Vector3 size=new Vector3(12f,24f+(i%3)*5f,38f);
                    RectF footprint=new RectF(at.x-7.5f,at.z-20f,at.x+7.5f,at.z+20f);bool blocked=false;
                    foreach(Block block in Blocks)foreach(Lot lot in block.Lots)foreach(Building b in lot.Buildings)if(b.Footprint.Overlaps(footprint,1.8f))blocked=true;
                    foreach(RoadEdge edge in Edges)if(EdgeRect(edge).Overlaps(footprint,2f))blocked=true;
                    foreach(RealmRoute04OB route in RealmRoutes04OB)foreach(Vector3 point in route.Points)if(footprint.Inset(-route.Width*.5f-2f).Contains(point))blocked=true;
                    foreach(System.Collections.Generic.List<Vector3> points in new[]{DataChipSpots,CrystalShardSpots,SpellStoneSpots,CreatureSpots,PickupSpots,RadioMastSpots})foreach(Vector3 point in points)if(footprint.Inset(-2f).Contains(point))blocked=true;
                    if(blocked)continue;
                    Landscape04OB.Add(new RealmRock04OB{Position=at,Size=size,Seed=Seed+i*311,Teal=i>=3});break;
                }
            }
        }
        private int RealmWalkNode04OB(Vector3 point)
        {
            foreach(WalkNode node in WalkNodes)if(MathUtil.FlatSqrDistance(node.Pos,point)<.04f)return node.Id;
            return AddWalkNode(point,-1);
        }
    }
}
