using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public enum CityRevision04O { Legacy04N = 0, Crystalline04O = 1, GothicRealm04OB = 2, FracturedWilds04OC = 3, CrystalRealms04OD = 4, CrystalUnbound04OE = 5, LivingFrontier04OG = 6 }
    public enum MassingFamily04O { CivicScar = 1, TerracedHearth, ForgeCathedral, CanalCompound, SignalRuin, TowerGarden, FringeRuin, RoofReliquary }

    public sealed class SpineSite04O
    {
        public string Id, Title;
        public Vector3 Position;
        public float Height, Radius;
        public int Character;
    }
    public sealed class SpineLink04O
    {
        public string From, To;
        public readonly List<Vector3> Points = new List<Vector3>();
    }
    public sealed class CityPassage04O
    {
        public int BlockId;
        public Vector3 A, B;
        public const float Width = 6f;
    }

    /// <summary>A one-way, explicit revision of ordinary parcels. Canonical spatial receipts never move.</summary>
    public sealed partial class CityLayout
    {
        public CityRevision04O Revision04O { get; private set; }
        public readonly List<SpineSite04O> Spine04O = new List<SpineSite04O>();
        public readonly List<SpineLink04O> SpineLinks04O = new List<SpineLink04O>();
        public readonly List<CityPassage04O> Passages04O = new List<CityPassage04O>();
        private readonly Dictionary<long,List<int>> _propsForRoutes04O = new Dictionary<long,List<int>>();
        private int _propCountForRoutes04O = -1;
        public int RebuiltLots04O { get; private set; }
        public float OriginalBuiltArea04O { get; private set; }
        public float RevisedBuiltArea04O { get; private set; }

        public bool ApplyProfile04O(CityRevision04O revision, IReadOnlyList<VenueBinding04C> venues)
        {
            if (revision == Revision04O) return true;
            if (revision == CityRevision04O.LivingFrontier04OG) return ApplyLivingFrontier04OG(venues);
            if (revision == CityRevision04O.CrystalUnbound04OE) return ApplyCrystalUnbound04OE(venues);
            if (revision == CityRevision04O.CrystalRealms04OD) return ApplyCrystalRealms04OD(venues);
            if (revision == CityRevision04O.FracturedWilds04OC) return ApplyFracturedWilds04OC(venues);
            if (revision == CityRevision04O.GothicRealm04OB) return ApplyGothic04OB(venues);
            // Without the original venue selection, fail closed before touching any parcel.
            if (revision != CityRevision04O.Crystalline04O || Revision04O != CityRevision04O.Legacy04N || venues == null || venues.Count < 12) return false;
            HashSet<string> venueIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<Lot> allLots = new HashSet<Lot>();
            foreach(Block block in Blocks) foreach(Lot lot in block.Lots) allLots.Add(lot);
            foreach(VenueBinding04C venue in venues)
                if(venue==null || venue.Definition==null || !venueIds.Add(venue.Definition.Id) ||
                    (venue.Lot!=null && !allLots.Contains(venue.Lot))) return false;
            HashSet<Lot> protectedLots = new HashSet<Lot>();
            foreach (VenueBinding04C venue in venues) if (venue != null && venue.Lot != null) protectedLots.Add(venue.Lot);
            foreach (Block block in Blocks)
            foreach (Lot lot in block.Lots)
            {
                foreach (Building building in lot.Buildings) OriginalBuiltArea04O += building.Footprint.Area;
                if (protectedLots.Contains(lot) || lot.CustomBuilt || lot.Poi != PoiType.None || !string.IsNullOrEmpty(lot.PoiName) ||
                    lot.Use == LotUse.Special || lot.Use == LotUse.Casino || lot.Use == LotUse.Park || lot.Use == LotUse.Cemetery || lot.Buildings.Count == 0) continue;
                bool protectedRoof = false;
                foreach (Building building in lot.Buildings)
                    if (building.Roof == RoofType.Helipad || building.Roof == RoofType.Antenna || !string.IsNullOrEmpty(building.Sign)) protectedRoof = true;
                if (protectedRoof) continue;
                bool receiptRoof=false;
                foreach(Building building in lot.Buildings) if(HoldsReceipt04O(building.Footprint))receiptRoof=true;
                if(receiptRoof)
                {
                    // Retain the exact original collectible roof and seed. Four real structural piers replace its curtain walls.
                    foreach(Building building in lot.Buildings) if(building.Height>=12f)building.RebirthFamily04O=(int)MassingFamily04O.RoofReliquary;
                    continue;
                }
                RectF area = lot.Area.Inset(2.5f);
                if (area.Width < 14f || area.Depth < 14f) continue;
                int seed = lot.Buildings[0].Seed;
                MassingFamily04O family = CityRebirthRules04O.Family(block.District);
                bool hub = CityRebirthRules04O.InHub(area.Center(0f), venues);
                lot.Buildings.Clear();
                if (!hub)
                {
                    // Keep the original road and parcel identities but let the wider surface become shattered fringe.
                    float width = Mathf.Min(10f, area.Width * .45f), depth = Mathf.Min(9f, area.Depth * .45f);
                    RectF ruin = new RectF(area.X0, area.Z1 - depth, area.X0 + width, area.Z1);
                    AddMass04O(lot, ruin, MassingFamily04O.FringeRuin, seed, 0);
                    RebuiltLots04O++; continue;
                }
                // Split the street wall around a six metre mineral court, rather than making a short old box.
                float split = area.CenterX;
                AddMass04O(lot, new RectF(area.X0, area.Z0, split - 3f, area.Z1), family, seed, 0);
                AddMass04O(lot, new RectF(split + 3f, area.Z0 + area.Depth * .18f, area.X1, area.Z1), family, seed, 1);
                RebuiltLots04O++;
            }
            foreach (Block block in Blocks)
            {
                foreach (Lot lot in block.Lots) foreach (Building building in lot.Buildings) RevisedBuiltArea04O += building.Footprint.Area;
                TryPassage04O(block);
            }
            Revision04O = revision;
            PlanSpine04O(venues);
            ConnectSpine04O();
            BuildSpatialHashes();
            return true;
        }

        private void AddMass04O(Lot lot, RectF area, MassingFamily04O family, int seed, int wing)
        {
            float height = CityRebirthRules04O.Height(family, seed, wing);
            lot.Buildings.Add(new Building { Footprint = area, Height = height, Tiers = 2, Seed = seed + wing,
                Style = FacadeStyle.Brick, Roof = RoofType.Flat, RebirthFamily04O = (int)family, RebirthWing04O = wing });
        }
        private bool HoldsReceipt04O(RectF footprint)
        {
            RectF test = footprint.Inset(-4f);
            foreach (List<Vector3> points in new[] { DataChipSpots, CrystalShardSpots, SpellStoneSpots, CreatureSpots, PickupSpots, RadioMastSpots })
                foreach (Vector3 point in points) if (test.Contains(point)) return true;
            foreach (StuntJumpDef jump in StuntJumps) if (test.Contains(jump.Position)) return true;
            return false;
        }
        private void TryPassage04O(Block block)
        {
            List<float> axes=new List<float>{block.Inner.CenterX};
            foreach(Lot lot in block.Lots)
                if(lot.Buildings.Count>0 && lot.Buildings[0].RebirthFamily04O>0) axes.Add(lot.Area.CenterX);
            int admitted=0;float previous=float.MinValue;
            foreach(float x in axes)
            {
                if(admitted>=2 || Mathf.Abs(x-previous)<8f)continue;
                Vector3 a=new Vector3(x,CurbHeight,block.Curb.Z0+2f),b=new Vector3(x,CurbHeight,block.Curb.Z1-2f);
                // Six metre court, with 20cm spare at both walls. A 3.1m probe incorrectly rejected a 6m gap.
                if(!ClearPassage04O(a,b,2.8f))continue;
                int south=ClosestBlockWalk04O(block,a),north=ClosestBlockWalk04O(block,b);
                if(south<0 || north<0 || south==north)continue;
                int na=AddWalkNode(a,block.Id),nb=AddWalkNode(b,block.Id);
                AddWalkLink(south,na,false,-1,false);AddWalkLink(na,nb,false,-1,false);AddWalkLink(nb,north,false,-1,false);
                block.WalkNodes.Add(na);block.WalkNodes.Add(nb);
                Passages04O.Add(new CityPassage04O{BlockId=block.Id,A=a,B=b});admitted++;previous=x;
            }
        }
        private int ClosestBlockWalk04O(Block block, Vector3 point)
        {
            int best = -1; float distance = 100f;
            foreach (int id in block.WalkNodes)
            {
                float d = MathUtil.FlatDistance(WalkNodes[id].Pos,point);
                if (d < distance && ClearPassage04O(point,WalkNodes[id].Pos,.8f)) { best = id; distance = d; }
            }
            return best;
        }
        public bool ClearPassage04O(Vector3 a, Vector3 b, float radius)
        {
            int count = Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/1.5f));
            for (int i=0;i<=count;i++)
            {
                Vector3 p=Vector3.Lerp(a,b,i/(float)count);
                if (radius<=3.1f ? IsInsideBuilding(p,radius) : IsInsideBuildingRaw(p,radius)) return false;
                if (NearRouteProp04O(p,radius+1f)) return false;
            }
            return true;
        }
        private bool NearRouteProp04O(Vector3 point,float radius)
        {
            if(_propCountForRoutes04O!=Props.Count)
            {
                _propsForRoutes04O.Clear();
                for(int i=0;i<Props.Count;i++)HashAdd(_propsForRoutes04O,Cell(Props[i].Pos.x),Cell(Props[i].Pos.z),i);
                _propCountForRoutes04O=Props.Count;
            }
            for(int x=Cell(point.x-radius);x<=Cell(point.x+radius);x++)
            for(int z=Cell(point.z-radius);z<=Cell(point.z+radius);z++)
            {
                List<int> indices;if(!_propsForRoutes04O.TryGetValue(Key(x,z),out indices))continue;
                foreach(int i in indices)if(MathUtil.FlatSqrDistance(point,Props[i].Pos)<radius*radius)return true;
            }
            return false;
        }
        private void ConnectSpine04O()
        {
            for(int i=1;i<Spine04O.Count;i++)
            {
                SpineSite04O a=Spine04O[i-1],b=Spine04O[i];
                int from=NearestNode(a.Position),to=NearestNode(b.Position);
                if(from<0 || to<0)continue;
                SpineLink04O link=new SpineLink04O{From=a.Id,To=b.Id};
                link.Points.AddRange(FindRoute(Nodes[from].Pos,Nodes[to].Pos));
                if(link.Points.Count>=2)SpineLinks04O.Add(link);
            }
        }
        private void PlanSpine04O(IReadOnlyList<VenueBinding04C> venues)
        {
            Poi home=FindPoi("Veyra Lofts");
            Vector3 origin=home == null ? Vector3.zero : home.Position;
            Vector3[] desired={ origin+(home==null?Vector3.forward:home.Facing)*60f, new Vector3(-420f,0f,-60f), new Vector3(-540f,0f,430f), origin };
            foreach (VenueBinding04C venue in venues) if(venue.Definition.Id == VenueIds04E.Pressure) desired[3]=venue.Entrance;
            string[] ids={"civic_scar","forge_seam","fringe_rupture","underheart_crown"};
            string[] names={"The Heart's Broken Rib","The Working Seam","The Listening Rupture","The Root Below"};
            HashSet<Building> used=new HashSet<Building>();
            for(int segment=0;segment<4;segment++)
            {
                Building best=null;float distance=float.MaxValue;
                foreach(Block block in Blocks) foreach(Lot lot in block.Lots) foreach(Building building in lot.Buildings)
                {
                    if(building.RebirthFamily04O==0 || used.Contains(building) || Mathf.Min(building.Footprint.Width,building.Footprint.Depth)<7f) continue;
                    float d=MathUtil.FlatSqrDistance(building.Footprint.Center(0f),desired[segment]);
                    if(d<distance){distance=d;best=building;}
                }
                if(best==null) continue; used.Add(best);
                best.RebirthSpine04O=segment+1;
                Spine04O.Add(new SpineSite04O{Id=ids[segment],Title=names[segment],Position=best.Footprint.Center(best.Height+CityLayout.CurbHeight),
                    Height=segment==0?58f:segment==1?38f:segment==2?66f:32f,Radius=Mathf.Min(best.Footprint.Width,best.Footprint.Depth)*.35f,Character=segment});
            }
        }
    }

    /// <summary>Deterministic profile rules. No seed consumption or mutable global geometry.</summary>
    public static class CityRebirthRules04O
    {
        public static void ResetStatics() { }
        public static bool Known(int revision) { return revision >= 0 && revision <= 6; }
        public static MassingFamily04O Family(District district)
        {
            switch(district)
            {
                case District.LasVeyraCore:return MassingFamily04O.CivicScar;
                case District.SunkenRow:return MassingFamily04O.CanalCompound;
                case District.OldSilver:return MassingFamily04O.ForgeCathedral;
                case District.GraveFrequency:return MassingFamily04O.SignalRuin;
                case District.GlassHeights:case District.SkyrailOrbit:return MassingFamily04O.TowerGarden;
                default:return MassingFamily04O.TerracedHearth;
            }
        }
        public static bool InHub(Vector3 point,IReadOnlyList<VenueBinding04C> venues)
        {
            foreach(VenueBinding04C venue in venues)
            {
                if(venue.Building==null)continue;
                string id=venue.Definition.Id;
                float radius=id=="lofts"?220f:id=="clinic"?155f:id=="forge"?145f:id=="garage"?135f:0f;
                if(radius>0f && MathUtil.FlatSqrDistance(point,venue.Outside)<radius*radius)return true;
            }
            // Separate signal and elevated garden settlements retain their named canonical regions.
            return MathUtil.FlatSqrDistance(point,new Vector3(-530f,0f,450f))<145f*145f ||
                MathUtil.FlatSqrDistance(point,new Vector3(650f,0f,350f))<155f*155f;
        }
        public static float Height(MassingFamily04O family,int seed,int wing)
        {
            uint hash=unchecked((uint)seed*2654435761u+(uint)wing*2246822519u);
            float variance=(hash%9u)*.7f;
            if(family==MassingFamily04O.FringeRuin)return 3.2f+(hash%3u)*.5f;
            return family==MassingFamily04O.TowerGarden ? 24f+variance+wing*8f :
                family==MassingFamily04O.SignalRuin ? 16f+variance+wing*11f : family==MassingFamily04O.ForgeCathedral?12f+variance+wing*5f:9f+variance+wing*3f;
        }
    }
}
