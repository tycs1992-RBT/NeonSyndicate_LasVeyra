using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public enum CityRevision04O { Legacy04N = 0, Crystalline04O = 1 }
    public enum MassingFamily04O { CivicScar = 1, TerracedHearth, ForgeCathedral, CanalCompound, SignalRuin, TowerGarden }

    public sealed class SpineSite04O
    {
        public string Id, Title;
        public Vector3 Position;
        public float Height, Radius;
        public int Character;
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
        public readonly List<CityPassage04O> Passages04O = new List<CityPassage04O>();
        public int RebuiltLots04O { get; private set; }
        public float OriginalBuiltArea04O { get; private set; }
        public float RevisedBuiltArea04O { get; private set; }

        public bool ApplyProfile04O(CityRevision04O revision, IReadOnlyList<VenueBinding04C> venues)
        {
            if (revision == Revision04O) return true;
            // Without the original venue selection, fail closed before touching any parcel.
            if (revision != CityRevision04O.Crystalline04O || Revision04O != CityRevision04O.Legacy04N || venues == null || venues.Count == 0) return false;
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
                    if (building.Roof == RoofType.Helipad || building.Roof == RoofType.Antenna || !string.IsNullOrEmpty(building.Sign) || HoldsReceipt04O(building.Footprint)) protectedRoof = true;
                if (protectedRoof) continue;
                RectF area = lot.Area.Inset(2.5f);
                if (area.Width < 14f || area.Depth < 14f) continue;
                int seed = lot.Buildings[0].Seed;
                MassingFamily04O family = CityRebirthRules04O.Family(block.District);
                lot.Buildings.Clear();
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
            // The entire centre strip must be clear, including unchanged/service buildings and old props.
            float x = block.Inner.CenterX;
            Vector3 a = new Vector3(x, CurbHeight, block.Curb.Z0 + 2f), b = new Vector3(x, CurbHeight, block.Curb.Z1 - 2f);
            if (!ClearPassage04O(a,b,3.1f)) return;
            int south = ClosestBlockWalk04O(block,a), north = ClosestBlockWalk04O(block,b);
            if (south < 0 || north < 0 || south == north) return;
            int na = AddWalkNode(a,block.Id), nb = AddWalkNode(b,block.Id);
            AddWalkLink(south,na,false,-1,false); AddWalkLink(na,nb,false,-1,false); AddWalkLink(nb,north,false,-1,false);
            block.WalkNodes.Add(na); block.WalkNodes.Add(nb);
            Passages04O.Add(new CityPassage04O { BlockId = block.Id, A = a, B = b });
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
                if (IsInsideBuildingRaw(p,radius)) return false;
                foreach (PropSpot prop in Props) if (MathUtil.FlatSqrDistance(p,prop.Pos) < (radius+1f)*(radius+1f)) return false;
            }
            return true;
        }
        private void PlanSpine04O(IReadOnlyList<VenueBinding04C> venues)
        {
            Poi home=FindPoi("Veyra Lofts");
            Vector3 origin=home == null ? Vector3.zero : home.Position;
            Vector3[] desired={ origin, new Vector3(-420f,0f,-60f), new Vector3(-540f,0f,430f), origin };
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
        public static bool Known(int revision) { return revision == 0 || revision == 1; }
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
        public static float Height(MassingFamily04O family,int seed,int wing)
        {
            uint hash=unchecked((uint)seed*2654435761u+(uint)wing*2246822519u);
            float variance=(hash%9u)*.7f;
            return family==MassingFamily04O.TowerGarden ? 24f+variance+wing*8f :
                family==MassingFamily04O.SignalRuin ? 16f+variance+wing*11f : family==MassingFamily04O.ForgeCathedral?12f+variance+wing*5f:9f+variance+wing*3f;
        }
    }
}
