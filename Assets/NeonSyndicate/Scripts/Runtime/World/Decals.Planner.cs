using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// What the planner needs to know about the live city that the layout alone does not carry: buildings the art
    /// passes or the walk-in venues replaced (their facades are not the plain boxes), venue footprints and doors.
    /// Everything is a plain list or delegate so the planner stays engine-free and testable.
    /// </summary>
    public sealed class DressContext
    {
        /// <summary>True for a building whose facade is not the plain generated box (skip wall decals and roof gear).</summary>
        public Func<Building, bool> SkipBuilding;
        /// <summary>True for a ground point inside a walk-in venue's footprint.</summary>
        public Func<Vector3, bool> InsideVenue;
        /// <summary>Walk-in venue doors (kept clear).</summary>
        public readonly List<Vector3> VenueDoors = new List<Vector3>();
    }

    /// <summary>
    /// Plans the full dressing set for a city deterministically from the layout seed: every item carries the lowest
    /// preset that shows it, so the same plan serves Medium, High and Ultra and a preset change never moves a piece.
    /// Decals and clutter never get colliders, so the plan keeps them out of walk lines, doors, quest points, parked
    /// cars, street furniture, venue footprints and ground openings instead of relying on physics to sort it out.
    /// </summary>
    public sealed partial class DressingPlanner
    {
        private readonly CityLayout L;
        private readonly DressContext _ctx;
        private readonly List<Keepout> _keep = new List<Keepout>();
        private readonly List<Keepout> _near = new List<Keepout>();
        public readonly List<DressItem> Items = new List<DressItem>();
        /// <summary>Items per block id (full set), for the per-block caps and the tests.</summary>
        public readonly Dictionary<int, int> DecalsPerBlock = new Dictionary<int, int>();
        public readonly Dictionary<int, int> ClutterPerBlock = new Dictionary<int, int>();

        private const int Salt = 7340;

        public DressingPlanner(CityLayout layout, DressContext ctx)
        {
            L = layout;
            _ctx = ctx ?? new DressContext();
        }

        /// <summary>All the keepout circles of the city (doors, quest points, furniture, parked cars).</summary>
        public IList<Keepout> Keepouts { get { return _keep; } }

        public void Plan()
        {
            Items.Clear();
            BuildKeepouts();
            foreach (Block b in L.Blocks) PlanBlock(b);
            foreach (Block b in L.Blocks) PlanRoofs(b);
            foreach (RoadEdge e in L.Edges) PlanRoad(e);
            foreach (RoadNode n in L.Nodes) PlanIntersection(n);
            PlanDesertLots();
            PlanScrub();
        }

        // ------------------------------------------------------------------ keepouts

        private void BuildKeepouts()
        {
            _keep.Clear();
            foreach (Poi p in L.Pois) _keep.Add(new Keepout(p.Position, DressingRules.PoiClearance));
            foreach (SpecialSite s in L.Sites) _keep.Add(new Keepout(s.Position, s.Radius + 4f));
            foreach (Vector3 d in _ctx.VenueDoors) _keep.Add(new Keepout(d, DressingRules.VenueDoorClearance));
            foreach (Block b in L.Blocks)
                foreach (Lot lot in b.Lots)
                    _keep.Add(new Keepout(lot.Entrance, DressingRules.EntranceClearance));
            foreach (Lot lot in L.DesertLots) _keep.Add(new Keepout(lot.Entrance, DressingRules.EntranceClearance));
            foreach (PropSpot p in L.Props) _keep.Add(new Keepout(p.Pos, PropClearance(p.Kind)));
            foreach (Vector3 t in L.TreeSpots) _keep.Add(new Keepout(t, DressingRules.FurnitureClearance));
            foreach (Vector3 t in L.PalmSpots) _keep.Add(new Keepout(t, DressingRules.FurnitureClearance));
            foreach (Vector3 p in L.ParkingSpots) _keep.Add(new Keepout(p, DressingRules.ParkingClearance));
            foreach (Vector3 p in L.PickupSpots) _keep.Add(new Keepout(p, 1.5f));
            foreach (Vector3 p in L.DataChipSpots) _keep.Add(new Keepout(p, 1.5f));
            foreach (Vector3 p in L.CrystalShardSpots) _keep.Add(new Keepout(p, 1.5f));
            foreach (Vector3 p in L.SpellStoneSpots) _keep.Add(new Keepout(p, 1.5f));
            foreach (Vector3 p in L.CreatureSpots) _keep.Add(new Keepout(p, 2.5f));
            foreach (StuntJumpDef j in L.StuntJumps) _keep.Add(new Keepout(j.Position, j.Length + 4f));
            foreach (WorldAperture a in WorldApertures.All)
            {
                RectF r = a.Rect;
                float radius = Mathf.Sqrt(r.Width * r.Width + r.Depth * r.Depth) * 0.5f + DressingRules.ApertureClearance;
                _keep.Add(new Keepout(r.CenterX, r.CenterZ, radius));
            }
        }

        private static float PropClearance(PropKind k)
        {
            switch (k)
            {
                case PropKind.Fountain: return 6f;
                case PropKind.Billboard: return 5f;
                case PropKind.MonorailPillar: return 2f;
                case PropKind.PicnicTable: return 2f;
                case PropKind.Bench: return 1.6f;
                default: return DressingRules.FurnitureClearance;
            }
        }

        /// <summary>Collects the keepouts that can touch a rectangle so the per-point tests stay cheap.</summary>
        private void Gather(RectF r, float margin)
        {
            _near.Clear();
            for (int i = 0; i < _keep.Count; i++)
            {
                Keepout k = _keep[i];
                if (k.X + k.Radius < r.X0 - margin || k.X - k.Radius > r.X1 + margin) continue;
                if (k.Z + k.Radius < r.Z0 - margin || k.Z - k.Radius > r.Z1 + margin) continue;
                _near.Add(k);
            }
        }

        private bool Free(Vector3 p, float radius)
        {
            if (!DressingRules.Clear(_near, p, radius)) return false;
            if (_ctx.InsideVenue != null && _ctx.InsideVenue(p)) return false;
            return true;
        }

        private bool Skip(Building bld)
        {
            return _ctx.SkipBuilding != null && _ctx.SkipBuilding(bld);
        }

        // ------------------------------------------------------------------ items

        private DressItem Add(DressKind kind, Vector3 pos, float yaw, Vector2 size, GRand r, int variant)
        {
            DressItem it = new DressItem();
            it.Kind = kind;
            it.Pos = pos;
            it.Yaw = yaw;
            it.Size = size;
            it.Variant = variant;
            it.Seed = (int)(r.NextUInt() & 0x7fffffff);
            it.Tier = DressingRules.TierFor(r.Value());
            Items.Add(it);
            return it;
        }

        private void Count(Dictionary<int, int> d, int block)
        {
            int n;
            d.TryGetValue(block, out n);
            d[block] = n + 1;
        }

        private int CountOf(Dictionary<int, int> d, int block)
        {
            int n;
            d.TryGetValue(block, out n);
            return n;
        }

        // ------------------------------------------------------------------ blocks

        private void PlanBlock(Block b)
        {
            float grit = DressingRules.Grit(b.District);
            GRand r = new GRand(b.Seed * 31 + Salt);
            Gather(b.Curb, 8f);
            float y = CityLayout.CurbHeight;
            foreach (Lot lot in b.Lots)
            {
                foreach (Building bld in lot.Buildings)
                {
                    RectF f = bld.Footprint;
                    if (!f.Valid) continue;
                    bool plain = !Skip(bld);
                    for (int side = 0; side < 4; side++)
                    {
                        if (!DressingRules.FacesOpen(b, bld, side)) continue;
                        Vector3 a, e;
                        DressingRules.SideSegment(f, side, out a, out e);
                        Vector3 n = DressingRules.SideNormal(side);
                        FacadeBase(b, r, a, e, n, y, grit, bld, plain);
                    }
                }
                LotClutter(b, lot, r, y, grit);
                LotFoliage(b, lot, r, y);
            }
            CurbDrains(b, r, grit);
        }

        /// <summary>Grime, litter, cables, planters and posters along one facade that faces the sidewalk.</summary>
        private void FacadeBase(Block b, GRand r, Vector3 a, Vector3 e, Vector3 n, float y, float grit, Building bld, bool plain)
        {
            float len = Vector3.Distance(a, e);
            if (len < 3f) return;
            Vector3 dir = (e - a) / len;
            float yaw = MathUtil.YawOf(dir);
            bool fancy = grit < 0.25f;
            // Grime at the foot of the wall (every district, more where it is rougher).
            for (float t = r.Range(1f, 3f); t < len - 1f; t += r.Range(2.6f, 5.5f))
            {
                if (CountOf(DecalsPerBlock, b.Id) >= DressingRules.MaxDecalsPerBlock) break;
                if (!r.Chance(0.35f + grit * 0.6f)) continue;
                Vector3 p = a + dir * t + n * r.Range(0.3f, 0.5f);
                p.y = y;
                if (!Free(p, 0.6f)) continue;
                Add(DressKind.Grime, p, yaw + r.Range(-6f, 6f), new Vector2(r.Range(1.1f, 2.3f), r.Range(0.55f, 0.85f)), r, r.Range(0, 2));
                Count(DecalsPerBlock, b.Id);
            }
            // Litter in the band between the wall and the walk line.
            for (float t = r.Range(0.5f, 4f); t < len - 0.5f; t += r.Range(2.5f, 7f))
            {
                if (CountOf(ClutterPerBlock, b.Id) >= DressingRules.MaxClutterPerBlock) break;
                if (!r.Chance(0.15f + grit * 0.55f)) continue;
                Vector3 p = a + dir * t + n * r.Range(0.2f, 0.7f);
                p.y = y;
                if (DressingRules.OnWalkLine(b, p.x, p.z) || !Free(p, 0.35f) || DressingRules.InsideBuilding(b, p.x, p.z, 0.05f)) continue;
                float pick = r.Value();
                DressKind k = pick < 0.34f ? DressKind.Bottle : pick < 0.62f ? DressKind.Can : pick < 0.82f ? DressKind.PaperBall : DressKind.CardboardBox;
                if (k == DressKind.CardboardBox && grit < 0.45f) k = DressKind.Can;
                Add(k, p, r.Range(0f, 360f), new Vector2(r.Range(0.85f, 1.2f), 0f), r, r.Range(0, 3));
                Count(ClutterPerBlock, b.Id);
            }
            // A cable run along the wall foot in the industrial and project districts.
            if (grit >= 0.6f && len > 8f && r.Chance(0.3f) && CountOf(ClutterPerBlock, b.Id) < DressingRules.MaxClutterPerBlock)
            {
                float cl = r.Range(2.5f, Mathf.Min(7f, len - 2f));
                float t0 = r.Range(0.6f, len - cl - 0.6f);
                Vector3 p = a + dir * (t0 + cl * 0.5f) + n * 0.12f;
                p.y = y;
                if (Free(p, 0.3f))
                {
                    Add(DressKind.Cable, p, yaw, new Vector2(cl, 0f), r, 0);
                    Count(ClutterPerBlock, b.Id);
                }
            }
            // Planters hugging the wall where the city is kept.
            if (fancy && len > 10f)
            {
                for (float t = r.Range(2f, 8f); t < len - 2f; t += r.Range(14f, 28f))
                {
                    if (CountOf(ClutterPerBlock, b.Id) >= DressingRules.MaxClutterPerBlock) break;
                    if (!r.Chance(0.6f)) continue;
                    Vector3 p = a + dir * t + n * 0.5f;
                    p.y = y;
                    if (DressingRules.OnWalkLine(b, p.x, p.z) || !Free(p, 0.6f)) continue;
                    Add(DressKind.Planter, p, yaw, new Vector2(r.Range(0.8f, 1.1f), 0f), r, r.Range(0, 2));
                    Count(ClutterPerBlock, b.Id);
                }
            }
            if (!plain) return;
            // Poster scraps on the wall at eye height (not on the kept facades).
            if (grit >= 0.3f)
            {
                for (float t = r.Range(1.5f, 6f); t < len - 1f; t += r.Range(5f, 13f))
                {
                    if (CountOf(DecalsPerBlock, b.Id) >= DressingRules.MaxDecalsPerBlock) break;
                    if (!r.Chance(grit * 0.7f)) continue;
                    Vector3 p = a + dir * t;
                    p.y = y + r.Range(1.0f, 1.8f);
                    Vector3 foot = p; foot.y = y;
                    if (!Free(foot, 0.4f)) continue;
                    DressItem it = Add(DressKind.PosterScrap, p, yaw, new Vector2(r.Range(0.42f, 0.7f), r.Range(0.5f, 0.82f)), r, r.Range(0, 4));
                    it.Normal = n;
                    Count(DecalsPerBlock, b.Id);
                }
            }
            // Water stains running down from the sills higher up.
            if (bld.Height > 6.5f)
            {
                for (float t = r.Range(2f, 8f); t < len - 1f; t += r.Range(6f, 16f))
                {
                    if (CountOf(DecalsPerBlock, b.Id) >= DressingRules.MaxDecalsPerBlock) break;
                    if (!r.Chance(0.25f + grit * 0.5f)) continue;
                    int floors = Mathf.Max(1, Mathf.FloorToInt((bld.Height - 4f) / 3.2f));
                    float sill = 3.4f + r.Range(0, floors) * 3.2f;
                    if (sill > bld.Height - 1.2f) continue;
                    Vector3 p = a + dir * t;
                    p.y = y + sill;
                    DressItem it = Add(DressKind.WaterStain, p, yaw, new Vector2(r.Range(0.25f, 0.5f), r.Range(0.9f, 1.9f)), r, r.Range(0, 2));
                    it.Normal = n;
                    Count(DecalsPerBlock, b.Id);
                }
            }
        }

        /// <summary>Crates, pallets, tyres, reels, bins and boxes in the empty, warehouse and parking lots.</summary>
        private void LotClutter(Block b, Lot lot, GRand r, float y, float grit)
        {
            bool yard = lot.Use == LotUse.Empty || lot.Use == LotUse.Warehouse || lot.Use == LotUse.Parking || lot.Use == LotUse.Projects;
            if (!yard) return;
            float density = lot.Use == LotUse.Parking ? 0.35f : 1f;
            int n = Mathf.Min(16, Mathf.FloorToInt(lot.Area.Area / 110f * density * (0.4f + grit)));
            for (int i = 0; i < n; i++)
            {
                if (CountOf(ClutterPerBlock, b.Id) >= DressingRules.MaxClutterPerBlock) break;
                Vector3 p = new Vector3(r.Range(lot.Area.X0 + 1.2f, lot.Area.X1 - 1.2f), y, r.Range(lot.Area.Z0 + 1.2f, lot.Area.Z1 - 1.2f));
                if (DressingRules.InsideBuilding(b, p.x, p.z, 1.1f) || !Free(p, 0.9f)) continue;
                float pick = r.Value();
                DressKind k;
                if (lot.Use == LotUse.Parking) k = pick < 0.5f ? DressKind.TyreStack : pick < 0.8f ? DressKind.WheelieBin : DressKind.CardboardBox;
                else k = pick < 0.26f ? DressKind.Crate : pick < 0.48f ? DressKind.Pallet : pick < 0.64f ? DressKind.TyreStack : pick < 0.76f ? DressKind.CableReel : pick < 0.9f ? DressKind.WheelieBin : DressKind.CardboardBox;
                Add(k, p, r.Range(0f, 360f), new Vector2(r.Range(0.8f, 1.25f), 0f), r, r.Range(0, 3));
                Count(ClutterPerBlock, b.Id);
            }
            // Oil where the cars stand, scorch where fires were lit.
            if (lot.Use == LotUse.Parking)
            {
                int stains = Mathf.Min(10, Mathf.FloorToInt(lot.Area.Area / 160f));
                for (int i = 0; i < stains; i++)
                {
                    if (CountOf(DecalsPerBlock, b.Id) >= DressingRules.MaxDecalsPerBlock) break;
                    Vector3 p = new Vector3(r.Range(lot.Area.X0 + 1f, lot.Area.X1 - 1f), y, r.Range(lot.Area.Z0 + 1f, lot.Area.Z1 - 1f));
                    if (DressingRules.InsideBuilding(b, p.x, p.z, 0.5f) || !Free(p, 0.6f)) continue;
                    Add(DressKind.OilStain, p, r.Range(0f, 360f), new Vector2(r.Range(0.8f, 1.7f), r.Range(0.8f, 1.7f)), r, r.Range(0, 2));
                    Count(DecalsPerBlock, b.Id);
                }
            }
            if (lot.Use == LotUse.Empty && grit >= 0.8f && r.Chance(0.5f) && CountOf(DecalsPerBlock, b.Id) < DressingRules.MaxDecalsPerBlock)
            {
                Vector3 p = new Vector3(r.Range(lot.Area.X0 + 2f, lot.Area.X1 - 2f), y, r.Range(lot.Area.Z0 + 2f, lot.Area.Z1 - 2f));
                if (!DressingRules.InsideBuilding(b, p.x, p.z, 1f) && Free(p, 1.2f))
                {
                    Add(DressKind.Scorch, p, r.Range(0f, 360f), new Vector2(r.Range(1.6f, 2.6f), r.Range(1.6f, 2.6f)), r, 0);
                    Count(DecalsPerBlock, b.Id);
                }
            }
        }

        /// <summary>Denser grass cards across the parks and cemeteries (the layout already greens these lots).</summary>
        private void LotFoliage(Block b, Lot lot, GRand r, float y)
        {
            bool green = lot.Use == LotUse.Park || lot.Use == LotUse.Cemetery || (b.IsPark && lot.Use == LotUse.Plaza);
            if (!green) return;
            float per = lot.Use == LotUse.Cemetery ? 34f : 16f;
            int n = Mathf.Min(DressingRules.MaxFoliagePerLot, Mathf.FloorToInt(lot.Area.Area / per));
            for (int i = 0; i < n; i++)
            {
                Vector3 p = new Vector3(r.Range(lot.Area.X0 + 0.4f, lot.Area.X1 - 0.4f), y, r.Range(lot.Area.Z0 + 0.4f, lot.Area.Z1 - 0.4f));
                if (DressingRules.InsideBuilding(b, p.x, p.z, 0.4f) || !Free(p, 0.3f)) continue;
                Add(DressKind.GrassTuft, p, r.Range(0f, 360f), new Vector2(r.Range(0.6f, 1.3f), 0f), r, r.Chance(0.2f) ? 1 : 0);
            }
        }

        /// <summary>Drain stains where the curb meets the road, a few per block side.</summary>
        private void CurbDrains(Block b, GRand r, float grit)
        {
            RectF c = b.Curb;
            for (int side = 0; side < 4; side++)
            {
                if (side == 2 && b.AlongCanalNorth) continue;
                if (side == 3 && b.AlongCanalSouth) continue;
                Vector3 a, e;
                DressingRules.SideSegment(c, side, out a, out e);
                Vector3 n = DressingRules.SideNormal(side);
                float len = Vector3.Distance(a, e);
                Vector3 dir = len > 0.01f ? (e - a) / len : Vector3.zero;
                for (float t = r.Range(6f, 14f); t < len - 4f; t += r.Range(16f, 30f))
                {
                    if (CountOf(DecalsPerBlock, b.Id) >= DressingRules.MaxDecalsPerBlock) break;
                    if (!r.Chance(0.3f + grit * 0.6f)) continue;
                    Vector3 p = a + dir * t + n * 0.5f;
                    p.y = 0f;
                    if (!Free(p, 0.6f)) continue;
                    DressItem it = Add(DressKind.DrainStain, p, MathUtil.YawOf(n), new Vector2(r.Range(0.9f, 1.4f), r.Range(0.8f, 1.2f)), r, r.Range(0, 2));
                    it.Normal = Vector3.up;
                    Count(DecalsPerBlock, b.Id);
                }
            }
        }

        // ------------------------------------------------------------------ roads

        private void PlanRoad(RoadEdge e)
        {
            if (e.HasBridge || e.Kind == RoadKind.Rural) return;
            RoadNode a = L.Nodes[e.A], bnode = L.Nodes[e.B];
            Vector3 dir = MathUtil.Flat(bnode.Pos - a.Pos);
            float len = dir.magnitude;
            if (len < 24f) return;
            dir /= len;
            Vector3 right = MathUtil.RightOf(dir);
            float sa = e.AlongX ? a.HalfX : a.HalfZ;
            float sb = e.AlongX ? bnode.HalfX : bnode.HalfZ;
            GRand r = new GRand(e.Id * 97 + Salt + 11);
            Gather(new RectF(Mathf.Min(a.Pos.x, bnode.Pos.x) - e.Width, Mathf.Min(a.Pos.z, bnode.Pos.z) - e.Width, Mathf.Max(a.Pos.x, bnode.Pos.x) + e.Width, Mathf.Max(a.Pos.z, bnode.Pos.z) + e.Width), 4f);
            District d = CityLayout.DistrictAt((a.Pos + bnode.Pos) * 0.5f);
            if (CityLayout.IsWilderness(d) && e.Kind != RoadKind.Highway) return;
            float grit = DressingRules.Grit(d);
            // Manholes in the lane centres at a third and two thirds of the span (Medium already shows them).
            if (e.Kind != RoadKind.Highway)
            {
                float lane = e.Median * 0.5f + e.LaneWidth * 0.5f;
                for (int k = 0; k < 2; k++)
                {
                    float s = sa + (len - sa - sb) * (k == 0 ? 0.33f : 0.67f);
                    Vector3 p = a.Pos + dir * s + right * (k == 0 ? lane : -lane);
                    p.y = 0f;
                    if (!Free(p, 0.5f)) continue;
                    DressItem it = Add(DressKind.Manhole, p, 0f, new Vector2(0.78f, 0.78f), r, 0);
                    it.Tier = GraphicsPreset.Medium;
                }
            }
            // Oil in the empty parking slots (the layout parks a car on 45 percent of them).
            if (e.ParkingLane)
            {
                float offset = e.LanesPerDir * e.LaneWidth + 1.25f;
                for (int side = 0; side < 2; side++)
                {
                    Vector3 from = side == 0 ? a.Pos : bnode.Pos;
                    Vector3 dd = side == 0 ? dir : -dir;
                    Vector3 rr = MathUtil.RightOf(dd);
                    float start = (side == 0 ? sa : sb) + 12f;
                    float end = len - (side == 0 ? sb : sa) - 12f;
                    for (float s = start; s < end; s += 7f)
                    {
                        if (!r.Chance(0.22f + grit * 0.4f)) continue;
                        Vector3 p = from + dd * s + rr * offset;
                        p.y = 0f;
                        if (!Free(p, 0.9f)) continue;
                        Add(DressKind.OilStain, p, r.Range(0f, 360f), new Vector2(r.Range(0.7f, 1.4f), r.Range(0.7f, 1.4f)), r, r.Range(0, 2));
                    }
                }
            }
            // Tyre marks along the busier lanes.
            if (e.Kind != RoadKind.Street && r.Chance(0.4f))
            {
                float s = r.Range(sa + 10f, len - sb - 10f);
                float off = (e.Median * 0.5f + e.LaneWidth * r.Range(0.3f, 0.7f)) * (r.Chance(0.5f) ? 1f : -1f);
                Vector3 p = a.Pos + dir * s + right * off;
                p.y = 0f;
                if (Free(p, 0.5f)) Add(DressKind.Skid, p, MathUtil.YawOf(dir) + r.Range(-5f, 5f), new Vector2(0.34f, r.Range(3f, 7f)), r, 0);
            }
        }

        /// <summary>Worn paint and braking marks on the approaches to the signalled crossings.</summary>
        private void PlanIntersection(RoadNode n)
        {
            if (!n.Signal) return;
            GRand r = new GRand(n.Id * 53 + Salt + 23);
            Gather(new RectF(n.Pos.x - 16f, n.Pos.z - 16f, n.Pos.x + 16f, n.Pos.z + 16f), 2f);
            foreach (int eid in n.Edges)
            {
                RoadEdge e = L.Edges[eid];
                if (e.HasBridge) continue;
                Vector3 other = L.Nodes[e.A == n.Id ? e.B : e.A].Pos;
                Vector3 dir = MathUtil.Flat(other - n.Pos).normalized;
                Vector3 right = MathUtil.RightOf(dir);
                float half = e.AlongX ? n.HalfX : n.HalfZ;
                // Traffic arriving at this node drives on its right-hand side: the lane just right of the centre
                // line when looking back toward the node, so left of dir here.
                float lane = e.Median * 0.5f + e.LaneWidth * 0.5f;
                Vector3 p = n.Pos + dir * (half + CityLayout.SidewalkWidth + 3.4f) - right * lane;
                p.y = 0f;
                if (!Free(p, 0.5f)) continue;
                Add(DressKind.MarkingWear, p, MathUtil.YawOf(dir), new Vector2(e.LaneWidth * 0.95f, 3.4f), r, r.Range(0, 2));
                if (r.Chance(0.35f))
                {
                    Vector3 q = p + dir * r.Range(2f, 5f) + right * r.Range(-0.6f, 0.6f);
                    Add(DressKind.Skid, q, MathUtil.YawOf(dir) + r.Range(-4f, 4f), new Vector2(0.32f, r.Range(2.5f, 5f)), r, 0);
                }
            }
        }

        // ------------------------------------------------------------------ desert lots and scrub

        private void PlanDesertLots()
        {
            foreach (Lot lot in L.DesertLots)
            {
                if (lot.CustomBuilt) continue;
                GRand r = new GRand(lot.Seed * 17 + Salt + 5);
                Gather(lot.Area, 6f);
                float y = 0.01f;
                if (lot.Poi == PoiType.GasStation)
                {
                    Vector3 c = lot.Area.Center(0f);
                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 p = c + new Vector3(i < 2 ? -2.5f : 2.5f, 0f, i % 2 == 0 ? -2.5f : 2.5f) + new Vector3(r.Range(-1.2f, 1.2f), 0f, r.Range(-1.2f, 1.2f) + 1.4f);
                        p.y = y;
                        if (!Free(p, 0.6f)) continue;
                        Add(DressKind.OilStain, p, r.Range(0f, 360f), new Vector2(r.Range(0.9f, 1.6f), r.Range(0.9f, 1.6f)), r, r.Range(0, 2));
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 p = new Vector3(r.Range(lot.Area.X0 + 2f, lot.Area.X1 - 2f), y, r.Range(lot.Area.Z0 + 2f, lot.Area.Z1 - 2f));
                        if (MathUtil.FlatDistance(p, c) < 9f || !Free(p, 0.9f)) continue;
                        Add(r.Chance(0.5f) ? DressKind.TyreStack : DressKind.Crate, p, r.Range(0f, 360f), new Vector2(r.Range(0.8f, 1.2f), 0f), r, r.Range(0, 3));
                    }
                }
                else if (lot.Poi == PoiType.Motel)
                {
                    int n = Mathf.Min(6, Mathf.FloorToInt(lot.Area.Area / 240f));
                    for (int i = 0; i < n; i++)
                    {
                        Vector3 p = new Vector3(r.Range(lot.Area.X0 + 2f, lot.Area.X1 - 2f), y, r.Range(lot.Area.Z0 + 2f, lot.Area.Z1 - 2f));
                        bool inside = false;
                        foreach (Building bld in lot.Buildings) if (DressingRules.Inside(bld.Footprint, p.x, p.z, 1f)) { inside = true; break; }
                        if (inside || !Free(p, 0.9f)) continue;
                        Add(DressKind.OilStain, p, r.Range(0f, 360f), new Vector2(r.Range(0.8f, 1.4f), r.Range(0.8f, 1.4f)), r, r.Range(0, 2));
                    }
                }
            }
        }

        /// <summary>Dry scrub tufts around the Badlands cacti and rocks the layout already places.</summary>
        private void PlanScrub()
        {
            GRand r = new GRand(Salt + 77);
            PlanScrubAround(L.CactusSpots, r, 1.2f, 4.5f, 2, 6);
            PlanScrubAround(L.RockSpots, r, 1.6f, 6f, 3, 8);
        }

        private void PlanScrubAround(List<Vector3> spots, GRand r, float rMin, float rMax, int nMin, int nMax)
        {
            foreach (Vector3 s in spots)
            {
                if (CityLayout.IsSnowGround(s.x, s.z)) continue;
                int n = r.Range(nMin, nMax + 1);
                for (int i = 0; i < n; i++)
                {
                    float ang = r.Range(0f, Mathf.PI * 2f), dist = r.Range(rMin, rMax);
                    Vector3 p = s + new Vector3(Mathf.Cos(ang) * dist, 0f, Mathf.Sin(ang) * dist);
                    p.y = s.y;
                    Add(DressKind.ScrubTuft, p, r.Range(0f, 360f), new Vector2(r.Range(0.7f, 1.5f), 0f), r, 1);
                }
            }
        }
    }
}
