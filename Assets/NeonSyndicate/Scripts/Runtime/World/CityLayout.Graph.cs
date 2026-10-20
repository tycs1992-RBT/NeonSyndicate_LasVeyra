using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public enum PropKind
    {
        StreetLamp,
        Hydrant,
        Bench,
        TrashCan,
        NewsBox,
        BusStop,
        SidewalkTree,
        Palm,
        Mailbox,
        ParkingMeter,
        Payphone,
        ATM,
        Vending,
        FoodCart,
        Planter,
        Fountain,
        Hoop,
        Tombstone,
        Billboard,
        Cone,
        Barrel,
        Dumpster,
        MonorailPillar,
        PicnicTable
    }

    public struct PropSpot
    {
        public Vector3 Pos;
        public float Yaw;
        public PropKind Kind;
        public int Variant;
    }

    public sealed partial class CityLayout
    {
        public readonly List<PropSpot> Props = new List<PropSpot>();

        // ================================================================== POIs

        private sealed class PoiRequest
        {
            public PoiType Type;
            public District[] Districts;
            public string Name;
            public FacadeStyle? Style;
        }

        private void AssignPois()
        {
            List<PoiRequest> requests = new List<PoiRequest>
            {
                new PoiRequest { Type = PoiType.Hospital, Districts = new[] { District.LasVeyraCore }, Name = "Saint Lumen Medical Center", Style = FacadeStyle.Hospital },
                new PoiRequest { Type = PoiType.Hospital, Districts = new[] { District.BrassProjects, District.SkyrailOrbit }, Name = "Brass Projects Hospital", Style = FacadeStyle.Hospital },
                new PoiRequest { Type = PoiType.Clinic, Districts = new[] { District.SunkenRow }, Name = "Mara Solane Street Clinic", Style = FacadeStyle.Hospital },
                new PoiRequest { Type = PoiType.Police, Districts = new[] { District.LasVeyraCore }, Name = "LVPD Central", Style = FacadeStyle.Police },
                new PoiRequest { Type = PoiType.Police, Districts = new[] { District.OldSilver }, Name = "LVPD Old Silver Precinct", Style = FacadeStyle.Police },
                new PoiRequest { Type = PoiType.Police, Districts = new[] { District.SkyrailOrbit }, Name = "LVPD Strip Division", Style = FacadeStyle.Police },
                new PoiRequest { Type = PoiType.GunShop, Districts = new[] { District.LasVeyraCore }, Name = "Gun Vault Downtown" },
                new PoiRequest { Type = PoiType.GunShop, Districts = new[] { District.AshUnderpass }, Name = "Gun Vault Ash Underpass" },
                new PoiRequest { Type = PoiType.GunShop, Districts = new[] { District.BrassProjects }, Name = "Gun Vault Brass Projects" },
                new PoiRequest { Type = PoiType.GunShop, Districts = new[] { District.GraveFrequency }, Name = "Gun Vault Grave Frequency" },
                new PoiRequest { Type = PoiType.Respray, Districts = new[] { District.OldSilver }, Name = "Neon Respray Old Silver" },
                new PoiRequest { Type = PoiType.Respray, Districts = new[] { District.SunkenRow }, Name = "Neon Respray Sunken Row" },
                new PoiRequest { Type = PoiType.Respray, Districts = new[] { District.SkyrailOrbit }, Name = "Neon Respray Skyrail" },
                new PoiRequest { Type = PoiType.Garage, Districts = new[] { District.AshUnderpass }, Name = "Jax Calder Customs" },
                new PoiRequest { Type = PoiType.Safehouse, Districts = new[] { District.LasVeyraCore }, Name = "Veyra Lofts" },
                new PoiRequest { Type = PoiType.Bar, Districts = new[] { District.OldSilver }, Name = "The Rook's Nest" },
                new PoiRequest { Type = PoiType.Bar, Districts = new[] { District.GraveFrequency }, Name = "Static Saints Tavern" },
                new PoiRequest { Type = PoiType.Diner, Districts = new[] { District.LasVeyraCore }, Name = "Cinder Diner" },
                new PoiRequest { Type = PoiType.Diner, Districts = new[] { District.SunkenRow }, Name = "Canal Street Diner" },
                new PoiRequest { Type = PoiType.Diner, Districts = new[] { District.SkyrailOrbit }, Name = "Orbit Diner" },
                new PoiRequest { Type = PoiType.ConvenienceStore, Districts = new[] { District.LasVeyraCore }, Name = "Sundial 24/7 Core" },
                new PoiRequest { Type = PoiType.ConvenienceStore, Districts = new[] { District.OldSilver }, Name = "Sundial 24/7 Old Silver" },
                new PoiRequest { Type = PoiType.ConvenienceStore, Districts = new[] { District.SunkenRow }, Name = "Sundial 24/7 Canal" },
                new PoiRequest { Type = PoiType.ConvenienceStore, Districts = new[] { District.BrassProjects }, Name = "Sundial 24/7 Brass" },
                new PoiRequest { Type = PoiType.ConvenienceStore, Districts = new[] { District.AshUnderpass }, Name = "Sundial 24/7 Ashline" },
                new PoiRequest { Type = PoiType.ConvenienceStore, Districts = new[] { District.GraveFrequency }, Name = "Sundial 24/7 Signal Hill" },
                new PoiRequest { Type = PoiType.Clothing, Districts = new[] { District.GlassHeights, District.LasVeyraCore }, Name = "Thread Theory" },
                new PoiRequest { Type = PoiType.Clothing, Districts = new[] { District.OldSilver }, Name = "Thread Theory Old Silver" },
                new PoiRequest { Type = PoiType.Contact, Districts = new[] { District.GlassHeights }, Name = "Iris Moon" },
                new PoiRequest { Type = PoiType.Contact, Districts = new[] { District.OldSilver }, Name = "Dante Rook" },
                new PoiRequest { Type = PoiType.Contact, Districts = new[] { District.SkyrailOrbit }, Name = "Nico Ash" },
                new PoiRequest { Type = PoiType.Contact, Districts = new[] { District.BrassProjects }, Name = "June Pyre" },
            };

            HashSet<Lot> used = new HashSet<Lot>();
            foreach (PoiRequest req in requests)
            {
                Lot lot;
                Block block;
                if (!PickLot(req.Districts, used, out lot, out block)) continue;
                used.Add(lot);
                lot.Poi = req.Type;
                lot.PoiName = req.Name;
                if (req.Style.HasValue)
                {
                    foreach (Building bld in lot.Buildings) bld.Style = req.Style.Value;
                }
                if (lot.Buildings.Count > 0 && req.Type != PoiType.Hospital && req.Type != PoiType.Police)
                {
                    Building bld = lot.Buildings[0];
                    bld.GroundShops = true;
                    if (req.Type == PoiType.Respray || req.Type == PoiType.Garage) bld.Height = Mathf.Min(bld.Height, 9f);
                    if (req.Type == PoiType.GunShop || req.Type == PoiType.ConvenienceStore || req.Type == PoiType.Diner || req.Type == PoiType.Bar) bld.Height = Mathf.Min(bld.Height, 14f);
                }
                ComputeEntrance(block, lot);
                AddPoi(req.Type, lot.Entrance, lot.Facing, req.Name, block.District, block.Id);
            }

            // Casinos on the Strip.
            foreach (Block b in Blocks)
            {
                if (b.District != District.MirageStrip) continue;
                foreach (Lot lot in b.Lots)
                {
                    if (lot.Use != LotUse.Casino || string.IsNullOrEmpty(lot.PoiName)) continue;
                    ComputeEntrance(b, lot);
                    lot.Poi = PoiType.Casino;
                    AddPoi(PoiType.Casino, lot.Entrance, lot.Facing, lot.PoiName, b.District, b.Id);
                }
            }

            // Desert POIs.
            foreach (Lot lot in DesertLots)
            {
                if (lot.Poi == PoiType.Motel)
                {
                    AddPoi(PoiType.Motel, lot.Entrance, lot.Facing, lot.PoiName, District.QuartzBadlands, -1);
                    AddPoi(PoiType.Safehouse, lot.Entrance + new Vector3(0f, 0f, -18f), lot.Facing, "Starlite Motel Room 7", District.QuartzBadlands, -1);
                    AddPoi(PoiType.Contact, lot.Entrance + new Vector3(-4f, 0f, 10f), lot.Facing, "Luma", District.QuartzBadlands, -1);
                }
                else if (lot.Poi == PoiType.GasStation)
                {
                    AddPoi(PoiType.GasStation, lot.Entrance, lot.Facing, lot.PoiName, District.QuartzBadlands, -1);
                    AddPoi(PoiType.ConvenienceStore, lot.Entrance + lot.Facing * 0.1f, lot.Facing, lot.PoiName + " Mart", District.QuartzBadlands, -1);
                }
            }
            // Mara and Jax contacts at their buildings.
            foreach (Poi p in Pois.ToArray())
            {
                if (p.Type == PoiType.Clinic) AddPoi(PoiType.Contact, p.Position + MathUtil.RightOf(p.Facing) * 3f, p.Facing, "Mara Solane", p.District, p.BlockId);
                if (p.Type == PoiType.Garage) AddPoi(PoiType.Contact, p.Position + MathUtil.RightOf(p.Facing) * 3f, p.Facing, "Jax Calder", p.District, p.BlockId);
                if (p.Type == PoiType.Police && p.Name == "LVPD Central") AddPoi(PoiType.Contact, p.Position + MathUtil.RightOf(p.Facing) * -3f, p.Facing, "Officer Valez", p.District, p.BlockId);
                if (p.Type == PoiType.Casino && p.Name == "LOTUS CASINO") AddPoi(PoiType.Contact, p.Position + MathUtil.RightOf(p.Facing) * 4f, p.Facing, "Sable Knox", p.District, p.BlockId);
                if (p.Type == PoiType.Casino) AddPoi(PoiType.Slots, p.Position + MathUtil.RightOf(p.Facing) * -4f, p.Facing, p.Name + " Slots", p.District, p.BlockId);
            }

            // Street furniture POIs: payphones, ATMs, vending machines, food carts.
            GRand r = new GRand(Seed * 13 + 7);
            PlaceSidewalkPois(PoiType.Payphone, 26, r);
            PlaceSidewalkPois(PoiType.ATM, 22, r);
            PlaceSidewalkPois(PoiType.Vending, 34, r);
            PlaceSidewalkPois(PoiType.FoodCart, 12, r);

            // Purchasable properties: one business per district from the original property flavor list.
            string[] propertyNames =
            {
                "Circuit Ward Night Market", "Old Mile Pawn and Loan", "Glass Pier Records", "Rust Canal Boathouse", "Battery Yard Scrap Co.",
                "Signal Hill Radio Shack", "Lotus Row Noodle House", "Neon Mesa Car Wash", "Sunset Exchange Lounge", "Palm Terminal Hangar 3"
            };
            District[] propertyDistricts =
            {
                District.LasVeyraCore, District.OldSilver, District.GlassHeights, District.SunkenRow, District.AshUnderpass,
                District.GraveFrequency, District.BrassProjects, District.SkyrailOrbit, District.MirageStrip, District.SkyrailOrbit
            };
            for (int i = 0; i < propertyNames.Length; i++)
            {
                Lot lot;
                Block block;
                if (!PickLot(new[] { propertyDistricts[i] }, used, out lot, out block)) continue;
                used.Add(lot);
                lot.Poi = PoiType.Property;
                lot.PoiName = propertyNames[i];
                ComputeEntrance(block, lot);
                AddPoi(PoiType.Property, lot.Entrance, lot.Facing, propertyNames[i], block.District, block.Id);
            }
        }

        private void AddPoi(PoiType type, Vector3 pos, Vector3 facing, string name, District district, int blockId)
        {
            Pois.Add(new Poi { Id = Pois.Count, Type = type, Position = pos, Facing = facing, Name = name, District = district, BlockId = blockId });
        }

        private bool PickLot(District[] districts, HashSet<Lot> used, out Lot result, out Block resultBlock)
        {
            result = null;
            resultBlock = null;
            float bestScore = float.MinValue;
            foreach (Block b in Blocks)
            {
                if (Array.IndexOf(districts, b.District) < 0 || b.IsPark) continue;
                foreach (Lot lot in b.Lots)
                {
                    if (used.Contains(lot) || lot.Buildings.Count == 0) continue;
                    if (lot.Use == LotUse.Casino || lot.Use == LotUse.Park || lot.Use == LotUse.Cemetery) continue;
                    if (!TouchesInnerEdge(b, lot)) continue;
                    // Prefer lots of reasonable size, closer to the district center, with deterministic tie-break.
                    float size = Mathf.Min(lot.Area.Width, lot.Area.Depth);
                    float score = -Mathf.Abs(size - 32f) + MathUtil.Hash01(lot.Seed, Seed) * 20f;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        result = lot;
                        resultBlock = b;
                    }
                }
            }
            return result != null;
        }

        private static bool TouchesInnerEdge(Block b, Lot lot)
        {
            const float eps = 0.6f;
            return Mathf.Abs(lot.Area.X0 - b.Inner.X0) < eps || Mathf.Abs(lot.Area.X1 - b.Inner.X1) < eps || Mathf.Abs(lot.Area.Z0 - b.Inner.Z0) < eps || Mathf.Abs(lot.Area.Z1 - b.Inner.Z1) < eps;
        }

        private void ComputeEntrance(Block b, Lot lot)
        {
            const float eps = 0.6f;
            float half = SidewalkWidth * 0.5f;
            // candidate sides: 0 west, 1 east, 2 south, 3 north
            List<int> sides = new List<int>();
            if (Mathf.Abs(lot.Area.X0 - b.Inner.X0) < eps) sides.Add(0);
            if (Mathf.Abs(lot.Area.X1 - b.Inner.X1) < eps) sides.Add(1);
            if (Mathf.Abs(lot.Area.Z0 - b.Inner.Z0) < eps && !b.AlongCanalNorth) sides.Add(2);
            if (Mathf.Abs(lot.Area.Z1 - b.Inner.Z1) < eps && !b.AlongCanalSouth) sides.Add(3);
            if (sides.Count == 0)
            {
                if (Mathf.Abs(lot.Area.Z0 - b.Inner.Z0) < eps) sides.Add(2);
                else if (Mathf.Abs(lot.Area.Z1 - b.Inner.Z1) < eps) sides.Add(3);
                else sides.Add(0);
            }
            // Prefer the side that faces the Strip for casinos, otherwise deterministic choice.
            int side = sides[Mathf.Abs(lot.Seed) % sides.Count];
            if (lot.Use == LotUse.Casino)
            {
                if (lot.Area.CenterX < StripX && sides.Contains(1)) side = 1;
                else if (lot.Area.CenterX > StripX && sides.Contains(0)) side = 0;
            }
            switch (side)
            {
                case 0: lot.Entrance = new Vector3(b.Inner.X0 - half, CurbHeight, lot.Area.CenterZ); lot.Facing = Vector3.left; break;
                case 1: lot.Entrance = new Vector3(b.Inner.X1 + half, CurbHeight, lot.Area.CenterZ); lot.Facing = Vector3.right; break;
                case 2: lot.Entrance = new Vector3(lot.Area.CenterX, CurbHeight, b.Inner.Z0 - half); lot.Facing = Vector3.back; break;
                default: lot.Entrance = new Vector3(lot.Area.CenterX, CurbHeight, b.Inner.Z1 + half); lot.Facing = Vector3.forward; break;
            }
        }

        private void PlaceSidewalkPois(PoiType type, int count, GRand r)
        {
            int placed = 0;
            int guard = 0;
            while (placed < count && guard < count * 40)
            {
                guard++;
                Block b = Blocks[r.Range(0, Blocks.Count)];
                if (b.Curb.Width < 30f || b.Curb.Depth < 30f) continue;
                int side = r.Range(0, 4);
                if (side == 2 && b.AlongCanalNorth) continue;
                if (side == 3 && b.AlongCanalSouth) continue;
                float t = r.Range(0.2f, 0.8f);
                Vector3 pos;
                Vector3 facing;
                float inward = SidewalkWidth - 0.9f;
                switch (side)
                {
                    case 0: pos = new Vector3(b.Curb.X0 + inward, CurbHeight, Mathf.Lerp(b.Curb.Z0, b.Curb.Z1, t)); facing = Vector3.left; break;
                    case 1: pos = new Vector3(b.Curb.X1 - inward, CurbHeight, Mathf.Lerp(b.Curb.Z0, b.Curb.Z1, t)); facing = Vector3.right; break;
                    case 2: pos = new Vector3(Mathf.Lerp(b.Curb.X0, b.Curb.X1, t), CurbHeight, b.Curb.Z0 + inward); facing = Vector3.back; break;
                    default: pos = new Vector3(Mathf.Lerp(b.Curb.X0, b.Curb.X1, t), CurbHeight, b.Curb.Z1 - inward); facing = Vector3.forward; break;
                }
                bool tooClose = false;
                foreach (Poi p in Pois)
                {
                    if (MathUtil.FlatSqrDistance(p.Position, pos) < 64f) { tooClose = true; break; }
                }
                if (tooClose) continue;
                AddPoi(type, pos, facing, type.ToString(), b.District, b.Id);
                placed++;
            }
        }

        // ================================================================== lanes

        private float CrossBand(RoadNode n)
        {
            bool inCore = Mathf.Abs(n.Pos.x) <= CoreHalf + 1f && Mathf.Abs(n.Pos.z) <= CoreHalf + 1f;
            return inCore && n.Edges.Count >= 2 ? SidewalkWidth + 0.6f : 0.5f;
        }

        private float NodeHalfAlong(RoadNode n, bool alongX)
        {
            return alongX ? n.HalfX : n.HalfZ;
        }

        private void BuildLaneGraph()
        {
            // incoming[node] -> list of (pathId); outgoing[node] -> list of pathIds
            Dictionary<int, List<int>> incoming = new Dictionary<int, List<int>>();
            Dictionary<int, List<int>> outgoing = new Dictionary<int, List<int>>();
            foreach (RoadNode n in Nodes)
            {
                incoming[n.Id] = new List<int>();
                outgoing[n.Id] = new List<int>();
            }

            foreach (RoadEdge e in Edges)
            {
                for (int dir = 0; dir < 2; dir++)
                {
                    RoadNode from = Nodes[dir == 0 ? e.A : e.B];
                    RoadNode to = Nodes[dir == 0 ? e.B : e.A];
                    Vector3 d = MathUtil.Flat(to.Pos - from.Pos);
                    float len = d.magnitude;
                    if (len < 1f) continue;
                    d /= len;
                    Vector3 right = MathUtil.RightOf(d);
                    float startD = NodeHalfAlong(from, e.AlongX) + CrossBand(from);
                    float endD = NodeHalfAlong(to, e.AlongX) + CrossBand(to);
                    if (startD + endD > len - 4f)
                    {
                        float scale = (len - 4f) / Mathf.Max(0.01f, startD + endD);
                        startD *= scale;
                        endD *= scale;
                    }
                    int firstLaneId = Lanes.Count;
                    for (int lane = 0; lane < e.LanesPerDir; lane++)
                    {
                        float offset = e.Median * 0.5f + (lane + 0.5f) * e.LaneWidth;
                        Vector3 p0 = from.Pos + d * startD + right * offset;
                        Vector3 p1 = to.Pos - d * endD + right * offset;
                        LanePath path = new LanePath
                        {
                            Id = Lanes.Count,
                            EdgeId = e.Id,
                            NodeId = to.Id,
                            LaneIndex = lane,
                            LanesInDir = e.LanesPerDir,
                            EndsAtSignal = to.Signal,
                            AxisNS = !e.AlongX,
                            SpeedLimit = e.SpeedLimit,
                            Kind = e.Kind
                        };
                        float segLen = Vector3.Distance(p0, p1);
                        int steps = Mathf.Max(1, Mathf.CeilToInt(segLen / 30f));
                        for (int s = 0; s <= steps; s++) path.Points.Add(Vector3.Lerp(p0, p1, s / (float)steps));
                        path.Finish();
                        Lanes.Add(path);
                        incoming[to.Id].Add(path.Id);
                        outgoing[from.Id].Add(path.Id);
                    }
                    for (int lane = 0; lane < e.LanesPerDir; lane++)
                    {
                        LanePath lp = Lanes[firstLaneId + lane];
                        lp.LeftNeighbor = lane > 0 ? firstLaneId + lane - 1 : -1;
                        lp.RightNeighbor = lane < e.LanesPerDir - 1 ? firstLaneId + lane + 1 : -1;
                    }
                }
            }

            // Connectors through each node.
            foreach (RoadNode n in Nodes)
            {
                List<int> ins = incoming[n.Id];
                List<int> outs = outgoing[n.Id];
                foreach (int inId in ins)
                {
                    LanePath pin = Lanes[inId];
                    Vector3 dIn = pin.DirectionAt(pin.Length);
                    int connections = 0;
                    foreach (int outId in outs)
                    {
                        LanePath pout = Lanes[outId];
                        bool deadEnd = n.Edges.Count == 1;
                        if (pout.EdgeId == pin.EdgeId && !deadEnd) continue;
                        Vector3 dOut = pout.DirectionAt(0f);
                        float dot = Vector3.Dot(dIn, dOut);
                        float cross = dIn.x * dOut.z - dIn.z * dOut.x;
                        int turn = dot > 0.7f ? 0 : (dot < -0.7f ? 2 : (cross < 0f ? 1 : -1));
                        if (turn == 2 && !deadEnd) continue;
                        if (!LaneAllowed(pin, pout, turn)) continue;
                        AddConnector(n, pin, pout, turn);
                        connections++;
                    }
                    if (connections == 0)
                    {
                        // Guarantee progress: connect to the first non-reverse outgoing lane, or reverse at dead ends.
                        foreach (int outId in outs)
                        {
                            LanePath pout = Lanes[outId];
                            if (pout.EdgeId == pin.EdgeId && n.Edges.Count > 1) continue;
                            Vector3 dOut = pout.DirectionAt(0f);
                            float dot = Vector3.Dot(dIn, dOut);
                            float cross = dIn.x * dOut.z - dIn.z * dOut.x;
                            int turn = dot > 0.7f ? 0 : (dot < -0.7f ? 2 : (cross < 0f ? 1 : -1));
                            AddConnector(n, pin, pout, turn);
                            break;
                        }
                    }
                }
            }
        }

        private static bool LaneAllowed(LanePath pin, LanePath pout, int turn)
        {
            int inL = pin.LanesInDir;
            int outL = pout.LanesInDir;
            if (turn == 0)
            {
                int target = Mathf.Min(pin.LaneIndex, outL - 1);
                if (inL > outL && pin.LaneIndex >= outL) target = outL - 1;
                return pout.LaneIndex == target;
            }
            if (turn == 1)
            {
                return pin.LaneIndex == inL - 1 && pout.LaneIndex == outL - 1;
            }
            if (turn == -1)
            {
                return pin.LaneIndex == 0 && pout.LaneIndex == 0;
            }
            // u-turn at dead ends: innermost to innermost
            return pin.LaneIndex == 0 && pout.LaneIndex == 0;
        }

        private void AddConnector(RoadNode n, LanePath pin, LanePath pout, int turn)
        {
            Vector3 a = pin.Points[pin.Points.Count - 1];
            Vector3 b = pout.Points[0];
            Vector3 dIn = pin.DirectionAt(pin.Length);
            Vector3 dOut = pout.DirectionAt(0f);
            LanePath c = new LanePath
            {
                Id = Lanes.Count,
                IsConnector = true,
                NodeId = n.Id,
                AxisNS = pin.AxisNS,
                EndsAtSignal = false,
                TurnType = turn,
                SpeedLimit = turn == 0 ? Mathf.Min(pin.SpeedLimit, pout.SpeedLimit) : (turn == 2 ? 4f : 7.5f),
                LaneIndex = pout.LaneIndex,
                LanesInDir = pout.LanesInDir,
                Kind = pout.Kind
            };
            if (turn == 0)
            {
                int steps = Mathf.Max(2, Mathf.CeilToInt(Vector3.Distance(a, b) / 8f));
                for (int s = 0; s <= steps; s++) c.Points.Add(Vector3.Lerp(a, b, s / (float)steps));
            }
            else if (turn == 2)
            {
                Vector3 mid = (a + b) * 0.5f + dIn * 9f;
                Vector3 c1 = a + dIn * 9f;
                Vector3 c2 = b - dOut * 9f;
                for (int s = 0; s <= 6; s++) c.Points.Add(MathUtil.Bezier(a, c1, mid, s / 6f));
                for (int s = 1; s <= 6; s++) c.Points.Add(MathUtil.Bezier(mid, c2, b, s / 6f));
            }
            else
            {
                Vector3 control;
                if (!LineIntersect(a, dIn, b, dOut, out control)) control = (a + b) * 0.5f;
                for (int s = 0; s <= 8; s++) c.Points.Add(MathUtil.Bezier(a, control, b, s / 8f));
            }
            c.Finish();
            Lanes.Add(c);
            pin.Next.Add(c.Id);
            c.Next.Add(pout.Id);
        }

        private static bool LineIntersect(Vector3 p, Vector3 dp, Vector3 q, Vector3 dq, out Vector3 hit)
        {
            float den = dp.x * dq.z - dp.z * dq.x;
            if (Mathf.Abs(den) < 1e-5f)
            {
                hit = p;
                return false;
            }
            float t = ((q.x - p.x) * dq.z - (q.z - p.z) * dq.x) / den;
            hit = p + dp * t;
            return true;
        }

        // ================================================================== walk graph

        private int AddWalkNode(Vector3 p, int blockId)
        {
            WalkNode w = new WalkNode { Id = WalkNodes.Count, Pos = p, BlockId = blockId };
            WalkNodes.Add(w);
            return w.Id;
        }

        private void AddWalkLink(int a, int b, bool crosswalk, int signalNode, bool crossesNS)
        {
            if (a == b) return;
            WalkLink l = new WalkLink { Id = WalkLinks.Count, A = a, B = b, Crosswalk = crosswalk, SignalNode = signalNode, CrossesNSRoad = crossesNS };
            WalkLinks.Add(l);
            WalkNodes[a].Links.Add(l.Id);
            WalkNodes[b].Links.Add(l.Id);
        }

        private readonly Dictionary<int, int[]> _blockCorners = new Dictionary<int, int[]>();

        private void BuildWalkGraph()
        {
            float half = SidewalkWidth * 0.5f;
            foreach (Block b in Blocks)
            {
                RectF w = b.Curb.Inset(half);
                // Corners: SW, SE, NE, NW (clockwise from south-west when viewed from above is SW->NW->NE->SE; we keep ring order SW, SE, NE, NW).
                Vector3 sw = new Vector3(w.X0, CurbHeight, w.Z0);
                Vector3 se = new Vector3(w.X1, CurbHeight, w.Z0);
                Vector3 ne = new Vector3(w.X1, CurbHeight, w.Z1);
                Vector3 nw = new Vector3(w.X0, CurbHeight, w.Z1);
                Vector3[] corners = { sw, se, ne, nw };
                int[] cornerIds = new int[4];
                int first = -1;
                int prev = -1;
                for (int c = 0; c < 4; c++)
                {
                    Vector3 from = corners[c];
                    Vector3 to = corners[(c + 1) % 4];
                    int cid = AddWalkNode(from, b.Id);
                    cornerIds[c] = cid;
                    b.WalkNodes.Add(cid);
                    if (prev >= 0) AddWalkLink(prev, cid, false, -1, false);
                    if (first < 0) first = cid;
                    prev = cid;
                    float len = Vector3.Distance(from, to);
                    int steps = Mathf.Max(1, Mathf.RoundToInt(len / 22f));
                    for (int s = 1; s < steps; s++)
                    {
                        int mid = AddWalkNode(Vector3.Lerp(from, to, s / (float)steps), b.Id);
                        b.WalkNodes.Add(mid);
                        AddWalkLink(prev, mid, false, -1, false);
                        prev = mid;
                    }
                }
                AddWalkLink(prev, first, false, -1, false);
                _blockCorners[b.Id] = cornerIds;
            }

            // Crosswalks at intersections.
            foreach (RoadNode n in Nodes)
            {
                bool inCore = Mathf.Abs(n.Pos.x) <= CoreHalf + 1f && Mathf.Abs(n.Pos.z) <= CoreHalf + 1f;
                if (!inCore) continue;
                int ne = CornerNear(n, 1, 1, 2);
                int nw = CornerNear(n, -1, 1, 3);
                int se = CornerNear(n, 1, -1, 1);
                int sw = CornerNear(n, -1, -1, 0);
                bool north = false, south = false, east = false, west = false;
                foreach (int eid in n.Edges)
                {
                    RoadEdge e = Edges[eid];
                    Vector3 other = Nodes[e.A == n.Id ? e.B : e.A].Pos;
                    Vector3 d = other - n.Pos;
                    if (Mathf.Abs(d.z) > Mathf.Abs(d.x)) { if (d.z > 0f) north = true; else south = true; }
                    else { if (d.x > 0f) east = true; else west = true; }
                }
                int sig = n.Signal ? n.Id : -1;
                if (north && nw >= 0 && ne >= 0 && WalkNodes[nw].BlockId != WalkNodes[ne].BlockId) AddWalkLink(nw, ne, true, sig, true);
                if (south && sw >= 0 && se >= 0 && WalkNodes[sw].BlockId != WalkNodes[se].BlockId) AddWalkLink(sw, se, true, sig, true);
                if (east && ne >= 0 && se >= 0 && WalkNodes[ne].BlockId != WalkNodes[se].BlockId) AddWalkLink(ne, se, true, sig, false);
                if (west && nw >= 0 && sw >= 0 && WalkNodes[nw].BlockId != WalkNodes[sw].BlockId) AddWalkLink(nw, sw, true, sig, false);
            }

            // Sidewalks over the canal bridges.
            foreach (RoadEdge e in Edges)
            {
                if (!e.HasBridge) continue;
                float x = Nodes[e.A].Pos.x;
                float hw = e.Width * 0.5f;
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 southProbe = new Vector3(x + side * (hw + 3f), 0f, CanalZ - CanalBandHalf - 3f);
                    Vector3 northProbe = new Vector3(x + side * (hw + 3f), 0f, CanalZ + CanalBandHalf + 3f);
                    Block south = null, north = null;
                    foreach (Block b in Blocks)
                    {
                        if (south == null && b.Curb.Contains(southProbe)) south = b;
                        if (north == null && b.Curb.Contains(northProbe)) north = b;
                    }
                    if (south == null || north == null) continue;
                    int[] sc, nc;
                    if (!_blockCorners.TryGetValue(south.Id, out sc) || !_blockCorners.TryGetValue(north.Id, out nc)) continue;
                    // West side (side -1): south block NE corner to north block SE corner. East side: south NW to north SW.
                    int a = side < 0 ? sc[2] : sc[3];
                    int bnode = side < 0 ? nc[1] : nc[0];
                    AddWalkLink(a, bnode, false, -1, false);
                }
            }
        }

        /// <summary>Finds the walk corner of the block in quadrant (sx, sz) of node n. cornerIndex: 0 SW, 1 SE, 2 NE, 3 NW of that block.</summary>
        private int CornerNear(RoadNode n, int sx, int sz, int cornerIndex)
        {
            Vector3 probe = new Vector3(n.Pos.x + sx * (n.HalfX + 3f), 0f, n.Pos.z + sz * (n.HalfZ + 3f));
            foreach (Block b in Blocks)
            {
                if (!b.Curb.Contains(probe)) continue;
                int[] corners;
                if (!_blockCorners.TryGetValue(b.Id, out corners)) return -1;
                // The block corner facing the node is opposite to the quadrant sign.
                int idx;
                if (sx > 0 && sz > 0) idx = 0;       // block NE of node: its SW corner
                else if (sx < 0 && sz > 0) idx = 1;  // block NW of node: its SE corner
                else if (sx > 0 && sz < 0) idx = 3;  // block SE of node: its NW corner
                else idx = 2;                        // block SW of node: its NE corner
                int cid = corners[idx];
                if (MathUtil.FlatDistance(WalkNodes[cid].Pos, n.Pos) > Mathf.Max(n.HalfX, n.HalfZ) + SidewalkWidth * 3f) return -1;
                return cid;
            }
            return -1;
        }

        // ================================================================== dressing

        private void AddProp(Vector3 p, float yaw, PropKind kind, int variant)
        {
            Props.Add(new PropSpot { Pos = p, Yaw = yaw, Kind = kind, Variant = variant });
        }

        private void PlaceStreetDressing()
        {
            GRand r = new GRand(Seed * 17 + 1);
            float lampInset = 0.6f;
            foreach (Block b in Blocks)
            {
                RectF c = b.Curb;
                // Four sides: yaw points from lamp toward the road.
                for (int side = 0; side < 4; side++)
                {
                    if (side == 2 && b.AlongCanalNorth) continue;
                    if (side == 3 && b.AlongCanalSouth) continue;
                    Vector3 a, bb;
                    float yaw;
                    switch (side)
                    {
                        case 0: a = new Vector3(c.X0 + lampInset, CurbHeight, c.Z0 + 6f); bb = new Vector3(c.X0 + lampInset, CurbHeight, c.Z1 - 6f); yaw = -90f; break;
                        case 1: a = new Vector3(c.X1 - lampInset, CurbHeight, c.Z0 + 6f); bb = new Vector3(c.X1 - lampInset, CurbHeight, c.Z1 - 6f); yaw = 90f; break;
                        case 2: a = new Vector3(c.X0 + 6f, CurbHeight, c.Z0 + lampInset); bb = new Vector3(c.X1 - 6f, CurbHeight, c.Z0 + lampInset); yaw = 180f; break;
                        default: a = new Vector3(c.X0 + 6f, CurbHeight, c.Z1 - lampInset); bb = new Vector3(c.X1 - 6f, CurbHeight, c.Z1 - lampInset); yaw = 0f; break;
                    }
                    float len = Vector3.Distance(a, bb);
                    int lamps = Mathf.Max(1, Mathf.RoundToInt(len / 32f));
                    for (int i = 0; i <= lamps; i++)
                    {
                        Vector3 p = Vector3.Lerp(a, bb, lamps == 0 ? 0.5f : i / (float)lamps);
                        AddProp(p, yaw, PropKind.StreetLamp, 0);
                        StreetLampSpots.Add(p);
                    }
                    // Smaller furniture between lamps.
                    int extras = Mathf.RoundToInt(len / 20f);
                    for (int i = 0; i < extras; i++)
                    {
                        float t = r.Range(0.08f, 0.92f);
                        Vector3 p = Vector3.Lerp(a, bb, t);
                        Vector3 inward = -MathUtil.DirFromYaw(yaw);
                        float roll = r.Value();
                        PropKind kind;
                        if (b.District == District.MirageStrip || b.District == District.SkyrailOrbit) kind = roll < 0.35f ? PropKind.Palm : (roll < 0.55f ? PropKind.Bench : (roll < 0.75f ? PropKind.Planter : PropKind.TrashCan));
                        else if (b.District == District.AshUnderpass) kind = roll < 0.3f ? PropKind.Dumpster : (roll < 0.5f ? PropKind.Barrel : (roll < 0.7f ? PropKind.Hydrant : PropKind.TrashCan));
                        else kind = roll < 0.18f ? PropKind.Hydrant : (roll < 0.36f ? PropKind.TrashCan : (roll < 0.5f ? PropKind.NewsBox : (roll < 0.62f ? PropKind.Bench : (roll < 0.72f ? PropKind.Mailbox : (roll < 0.86f ? PropKind.SidewalkTree : PropKind.ParkingMeter)))));
                        Vector3 offset = kind == PropKind.SidewalkTree || kind == PropKind.Palm ? Vector3.zero : inward * 2.2f;
                        AddProp(p + offset, yaw, kind, r.Range(0, 4));
                    }
                }

                // Park contents.
                foreach (Lot lot in b.Lots)
                {
                    if (lot.Use == LotUse.Park)
                    {
                        int trees = Mathf.Clamp(Mathf.RoundToInt(lot.Area.Area / 180f), 4, 90);
                        for (int i = 0; i < trees; i++)
                        {
                            Vector3 p = new Vector3(r.Range(lot.Area.X0 + 3f, lot.Area.X1 - 3f), CurbHeight, r.Range(lot.Area.Z0 + 3f, lot.Area.Z1 - 3f));
                            if (b.District == District.MirageStrip || b.District == District.SkyrailOrbit || r.Chance(0.3f)) PalmSpots.Add(p); else TreeSpots.Add(p);
                        }
                        int benches = Mathf.Clamp(Mathf.RoundToInt(lot.Area.Area / 900f), 2, 18);
                        for (int i = 0; i < benches; i++)
                        {
                            Vector3 p = new Vector3(r.Range(lot.Area.X0 + 4f, lot.Area.X1 - 4f), CurbHeight, r.Range(lot.Area.Z0 + 4f, lot.Area.Z1 - 4f));
                            AddProp(p, r.Range(0f, 360f), r.Chance(0.5f) ? PropKind.Bench : PropKind.PicnicTable, 0);
                        }
                        if (lot.Area.Area > 6000f) AddProp(lot.Area.Center(CurbHeight), 0f, PropKind.Fountain, 0);
                    }
                    else if (lot.Use == LotUse.Plaza)
                    {
                        AddProp(lot.Area.Center(CurbHeight), 0f, PropKind.Fountain, 1);
                        for (int i = 0; i < 4; i++) AddProp(lot.Area.Center(CurbHeight) + MathUtil.DirFromYaw(i * 90f + 45f) * 7f, i * 90f, PropKind.Bench, 0);
                    }
                    else if (lot.Use == LotUse.Court)
                    {
                        AddProp(new Vector3(lot.Area.CenterX, CurbHeight, lot.Area.Z0 + 3f), 0f, PropKind.Hoop, 0);
                        AddProp(new Vector3(lot.Area.CenterX, CurbHeight, lot.Area.Z1 - 3f), 180f, PropKind.Hoop, 0);
                    }
                    else if (lot.Use == LotUse.Cemetery)
                    {
                        for (float x = lot.Area.X0 + 6f; x < lot.Area.X1 - 6f; x += 5f)
                            for (float z = lot.Area.Z0 + 6f; z < lot.Area.Z1 - 6f; z += 6f)
                                if (MathUtil.Hash01(Mathf.RoundToInt(x), Mathf.RoundToInt(z)) > 0.25f) AddProp(new Vector3(x, CurbHeight, z), 0f, PropKind.Tombstone, MathUtil.Hash(Mathf.RoundToInt(x), Mathf.RoundToInt(z)) % 3);
                        for (int i = 0; i < 25; i++) TreeSpots.Add(new Vector3(r.Range(lot.Area.X0 + 3f, lot.Area.X1 - 3f), CurbHeight, r.Range(lot.Area.Z0 + 3f, lot.Area.Z1 - 3f)));
                    }
                    else if (lot.Use == LotUse.Parking)
                    {
                        RectF a = lot.Area.Inset(3f);
                        for (float x = a.X0 + 2f; x < a.X1 - 2f; x += 3.2f)
                        {
                            for (float z = a.Z0 + 4f; z < a.Z1 - 4f; z += 12f)
                            {
                                if (r.Chance(0.35f))
                                {
                                    ParkingSpots.Add(new Vector3(x, CurbHeight, z + 2.5f));
                                    ParkingYaws.Add(r.Chance(0.5f) ? 0f : 180f);
                                }
                            }
                        }
                    }
                }
            }

            // Strip median: palms and monorail pillars.
            for (float z = -CoreHalf + 20f; z < CoreHalf - 20f; z += 24f)
            {
                if (Mathf.Abs(z - CanalZ) < CanalBandHalf + 4f) continue;
                if (NearNodeZ(StripX, z, 22f)) continue;
                AddProp(new Vector3(StripX, 0f, z), 0f, PropKind.Palm, 0);
            }
            for (float z = -CoreHalf + 10f; z < CoreHalf - 10f; z += 48f)
            {
                if (Mathf.Abs(z - CanalZ) < CanalBandHalf + 2f) continue;
                if (NearNodeZ(StripX, z, 16f)) continue;
                AddProp(new Vector3(StripX, 0f, z + 12f), 0f, PropKind.MonorailPillar, 0);
            }

            // Billboards on some rooftops along avenues.
            foreach (Block b in Blocks)
            {
                if (r.Chance(0.12f) && !b.IsPark)
                {
                    foreach (Lot lot in b.Lots)
                    {
                        if (lot.Buildings.Count == 0) continue;
                        Building bld = lot.Buildings[0];
                        if (bld.Height < 8f || bld.Height > 40f) continue;
                        Vector3 top = new Vector3(bld.Footprint.CenterX, CurbHeight + bld.Height, bld.Footprint.CenterZ);
                        AddProp(top, r.Pick(new[] { 0f, 90f, 180f, 270f }), PropKind.Billboard, r.Range(0, 4));
                        break;
                    }
                }
            }

            // On-street parking spots along streets with a parking lane.
            foreach (RoadEdge e in Edges)
            {
                if (!e.ParkingLane) continue;
                RoadNode a = Nodes[e.A];
                RoadNode bnode = Nodes[e.B];
                for (int dir = 0; dir < 2; dir++)
                {
                    RoadNode from = dir == 0 ? a : bnode;
                    RoadNode to = dir == 0 ? bnode : a;
                    Vector3 d = MathUtil.Flat(to.Pos - from.Pos);
                    float len = d.magnitude;
                    d /= Mathf.Max(0.01f, len);
                    Vector3 right = MathUtil.RightOf(d);
                    float offset = e.LanesPerDir * e.LaneWidth + 1.25f;
                    float start = NodeHalfAlong(from, e.AlongX) + 12f;
                    float end = len - NodeHalfAlong(to, e.AlongX) - 12f;
                    for (float s = start; s < end; s += 7f)
                    {
                        if (e.HasBridge)
                        {
                            Vector3 test = from.Pos + d * s;
                            if (test.z > e.BridgeZ0 - 6f && test.z < e.BridgeZ1 + 6f) continue;
                        }
                        if (r.Chance(0.45f))
                        {
                            Vector3 p = from.Pos + d * s + right * offset;
                            ParkingSpots.Add(p);
                            ParkingYaws.Add(MathUtil.YawOf(d));
                        }
                    }
                }
            }
        }

        private bool NearNodeZ(float x, float z, float margin)
        {
            foreach (RoadNode n in Nodes)
            {
                if (Mathf.Abs(n.Pos.x - x) < 1f && Mathf.Abs(n.Pos.z - z) < n.HalfZ + margin) return true;
            }
            return false;
        }

        // ================================================================== collectibles, jumps, pickups

        private void PlaceCollectiblesAndJumps()
        {
            GRand r = new GRand(Seed * 23 + 5);
            // Data chips: 5 per city district, tucked into gaps between buildings and park corners, plus desert sites.
            District[] cityDistricts = { District.LasVeyraCore, District.GlassHeights, District.MirageStrip, District.OldSilver, District.BrassProjects, District.AshUnderpass, District.SunkenRow, District.GraveFrequency, District.SkyrailOrbit };
            foreach (District d in cityDistricts)
            {
                int placed = 0;
                int guard = 0;
                while (placed < 5 && guard < 400)
                {
                    guard++;
                    Block b = Blocks[r.Range(0, Blocks.Count)];
                    if (b.District != d) continue;
                    Lot lot = b.Lots.Count > 0 ? b.Lots[r.Range(0, b.Lots.Count)] : null;
                    if (lot == null) continue;
                    Vector3 p = new Vector3(r.Range(lot.Area.X0 + 1f, lot.Area.X1 - 1f), CurbHeight + 0.6f, r.Range(lot.Area.Z0 + 1f, lot.Area.Z1 - 1f));
                    if (IsInsideBuildingRaw(p, 1.2f)) continue;
                    if (TooClose(DataChipSpots, p, 60f)) continue;
                    DataChipSpots.Add(p);
                    placed++;
                }
            }
            // Desert chips.
            Vector3[] desertChips =
            {
                new Vector3(1345f, 0.6f, -330f), new Vector3(1400f, 0.6f, 760f), new Vector3(-1350f, 0.6f, 740f), new Vector3(-1300f, 0.6f, -650f),
                new Vector3(320f, 0.6f, 1460f), new Vector3(-1420f, 0.6f, 1420f), new Vector3(1450f, 0.6f, -1450f)
            };
            DataChipSpots.AddRange(desertChips);
            // Canal bed chips.
            DataChipSpots.Add(new Vector3(-600f, -CanalDepth + 0.6f, CanalZ));
            DataChipSpots.Add(new Vector3(650f, -CanalDepth + 0.6f, CanalZ));
            while (DataChipSpots.Count < 50)
            {
                Vector3 p = new Vector3(r.Range(-MapHalf + 100f, MapHalf - 100f), 0.6f, r.Range(-MapHalf + 100f, MapHalf - 100f));
                if (Mathf.Abs(p.x) < CoreHalf + 60f && Mathf.Abs(p.z) < CoreHalf + 60f) continue;
                if (NearAnyRoad(p, 8f)) continue;
                DataChipSpots.Add(p);
            }
            if (DataChipSpots.Count > 50) DataChipSpots.RemoveRange(50, DataChipSpots.Count - 50);

            // Crystal shards: parks, cemetery, canyon, quarry.
            foreach (Block b in Blocks)
            {
                if (!b.IsPark || CrystalShardSpots.Count >= 12) continue;
                Vector3 p = new Vector3(r.Range(b.Inner.X0 + 3f, b.Inner.X1 - 3f), CurbHeight + 0.8f, r.Range(b.Inner.Z0 + 3f, b.Inner.Z1 - 3f));
                CrystalShardSpots.Add(p);
            }
            SpecialSite canyon = Site("Canyon");
            SpecialSite quarry = Site("Quarry");
            for (int i = 0; CrystalShardSpots.Count < 20 && i < 40; i++)
            {
                SpecialSite s = i % 2 == 0 ? canyon : quarry;
                Vector2 o = r.InsideUnitCircle() * s.Radius * 0.7f;
                Vector3 p = s.Position + new Vector3(o.x, 0.8f, o.y);
                if (TooClose(CrystalShardSpots, p, 15f)) continue;
                CrystalShardSpots.Add(p);
            }

            // Spell stones: one signature location per district theme.
            SpellStoneSpots.Add(BlockCenterIn(District.GlassHeights, true, r));
            SpellStoneSpots.Add(BlockCenterIn(District.LasVeyraCore, false, r));
            SpellStoneSpots.Add(new Vector3(StripX, 0.9f, 300f));
            SpellStoneSpots.Add(BlockCenterIn(District.OldSilver, false, r));
            SpellStoneSpots.Add(BlockCenterIn(District.BrassProjects, false, r));
            SpellStoneSpots.Add(BlockCenterIn(District.AshUnderpass, false, r));
            SpellStoneSpots.Add(new Vector3(0f, -CanalDepth + 0.9f, CanalZ));
            SpellStoneSpots.Add(BlockCenterIn(District.GraveFrequency, true, r));
            SpellStoneSpots.Add(BlockCenterIn(District.SkyrailOrbit, false, r));
            SpellStoneSpots.Add(quarry.Position + new Vector3(0f, 0.9f, 0f));

            // Creature sightings (night only): parks and desert.
            foreach (Block b in Blocks)
            {
                if (!b.IsPark || CreatureSpots.Count >= 6) continue;
                CreatureSpots.Add(b.Inner.Center(CurbHeight));
            }
            CreatureSpots.Add(canyon.Position + new Vector3(30f, 0f, 20f));
            CreatureSpots.Add(quarry.Position + new Vector3(-20f, 0f, 30f));
            CreatureSpots.Add(new Vector3(-1400f, 0f, 1300f));
            CreatureSpots.Add(new Vector3(1300f, 0f, -1250f));

            // Stunt jumps: desert ramps with long clear landings, plus parking lots and parks with open space.
            float[,] desertJumps =
            {
                { 1150f, -600f, 0f }, { -1150f, 300f, 90f }, { 600f, 1150f, 270f }, { -600f, -1150f, 180f },
                { 1300f, 1100f, 45f }, { -1300f, -1100f, 225f }, { 1200f, 820f, 90f }, { -1200f, 820f, 0f }
            };
            for (int i = 0; i < desertJumps.GetLength(0); i++)
            {
                StuntJumps.Add(new StuntJumpDef { Position = new Vector3(desertJumps[i, 0], 0f, desertJumps[i, 1]), Yaw = desertJumps[i, 2], Height = 2.6f, Length = 8f });
            }
            int cityJumps = 0;
            foreach (Block b in Blocks)
            {
                if (cityJumps >= 6) break;
                foreach (Lot lot in b.Lots)
                {
                    if (lot.Use != LotUse.Park && lot.Use != LotUse.Parking) continue;
                    if (lot.Area.Width < 60f && lot.Area.Depth < 60f) continue;
                    bool alongX = lot.Area.Width >= lot.Area.Depth;
                    float yaw = alongX ? 90f : 0f;
                    Vector3 start = alongX ? new Vector3(lot.Area.X0 + 10f, CurbHeight, lot.Area.CenterZ) : new Vector3(lot.Area.CenterX, CurbHeight, lot.Area.Z0 + 10f);
                    StuntJumps.Add(new StuntJumpDef { Position = start, Yaw = yaw, Height = 1.8f, Length = 6f });
                    cityJumps++;
                    break;
                }
            }

            // Pickup spots near useful places and scattered through alleys.
            foreach (Poi p in Pois)
            {
                if (p.Type == PoiType.Hospital || p.Type == PoiType.Clinic || p.Type == PoiType.Police) PickupSpots.Add(p.Position + MathUtil.RightOf(p.Facing) * 6f + Vector3.up * 0.4f);
            }
            int guard2 = 0;
            while (PickupSpots.Count < 70 && guard2 < 3000)
            {
                guard2++;
                Block b = Blocks[r.Range(0, Blocks.Count)];
                Lot lot = b.Lots.Count > 0 ? b.Lots[r.Range(0, b.Lots.Count)] : null;
                if (lot == null) continue;
                Vector3 p = new Vector3(r.Range(lot.Area.X0 + 1f, lot.Area.X1 - 1f), CurbHeight + 0.4f, r.Range(lot.Area.Z0 + 1f, lot.Area.Z1 - 1f));
                if (IsInsideBuildingRaw(p, 1f)) continue;
                if (TooClose(PickupSpots, p, 45f)) continue;
                PickupSpots.Add(p);
            }
        }

        private Vector3 BlockCenterIn(District d, bool preferPark, GRand r)
        {
            Block best = null;
            foreach (Block b in Blocks)
            {
                if (b.District != d) continue;
                if (preferPark && b.IsPark) { best = b; break; }
                if (best == null) best = b;
            }
            if (best == null) return new Vector3(0f, 1f, 0f);
            // Find a spot inside the block that is not inside a building.
            for (int i = 0; i < 60; i++)
            {
                Vector3 p = new Vector3(r.Range(best.Inner.X0 + 1f, best.Inner.X1 - 1f), CurbHeight + 0.9f, r.Range(best.Inner.Z0 + 1f, best.Inner.Z1 - 1f));
                if (!IsInsideBuildingRaw(p, 1.5f)) return p;
            }
            // Fallback: sidewalk corner.
            return new Vector3(best.Curb.X0 + 2f, CurbHeight + 0.9f, best.Curb.Z0 + 2f);
        }

        private static bool TooClose(List<Vector3> list, Vector3 p, float minDist)
        {
            float m2 = minDist * minDist;
            for (int i = 0; i < list.Count; i++) if (MathUtil.FlatSqrDistance(list[i], p) < m2) return true;
            return false;
        }

        private bool IsInsideBuildingRaw(Vector3 p, float margin)
        {
            foreach (Block b in Blocks)
            {
                if (!b.Curb.Inset(-margin).Contains(p)) continue;
                foreach (Lot lot in b.Lots)
                    foreach (Building bld in lot.Buildings)
                        if (bld.Footprint.Inset(-margin).Contains(p)) return true;
            }
            foreach (Lot lot in DesertLots)
                foreach (Building bld in lot.Buildings)
                    if (bld.Footprint.Inset(-margin).Contains(p)) return true;
            return false;
        }

        // ================================================================== spatial queries

        private static long Key(int cx, int cz)
        {
            return ((long)cx << 32) ^ (uint)cz;
        }

        private static int Cell(float v)
        {
            return Mathf.FloorToInt(v / HashCell);
        }

        private static void HashAdd(Dictionary<long, List<int>> hash, int cx, int cz, int id)
        {
            long k = Key(cx, cz);
            List<int> l;
            if (!hash.TryGetValue(k, out l))
            {
                l = new List<int>();
                hash[k] = l;
            }
            if (l.Count == 0 || l[l.Count - 1] != id) l.Add(id);
        }

        private void BuildSpatialHashes()
        {
            _laneHash.Clear();
            _walkHash.Clear();
            _blockHash.Clear();
            _edgeHash.Clear();
            foreach (LanePath p in Lanes)
            {
                for (int i = 0; i < p.Points.Count; i++)
                {
                    HashAdd(_laneHash, Cell(p.Points[i].x), Cell(p.Points[i].z), p.Id);
                    if (i > 0)
                    {
                        Vector3 mid = (p.Points[i - 1] + p.Points[i]) * 0.5f;
                        HashAdd(_laneHash, Cell(mid.x), Cell(mid.z), p.Id);
                    }
                }
            }
            foreach (WalkNode w in WalkNodes) HashAdd(_walkHash, Cell(w.Pos.x), Cell(w.Pos.z), w.Id);
            foreach (Block b in Blocks)
            {
                for (int cx = Cell(b.Curb.X0); cx <= Cell(b.Curb.X1); cx++)
                    for (int cz = Cell(b.Curb.Z0); cz <= Cell(b.Curb.Z1); cz++)
                        HashAdd(_blockHash, cx, cz, b.Id);
            }
            foreach (RoadEdge e in Edges)
            {
                RectF rr = EdgeRect(e);
                for (int cx = Cell(rr.X0); cx <= Cell(rr.X1); cx++)
                    for (int cz = Cell(rr.Z0); cz <= Cell(rr.Z1); cz++)
                        HashAdd(_edgeHash, cx, cz, e.Id);
            }
        }

        public RectF EdgeRect(RoadEdge e)
        {
            Vector3 a = Nodes[e.A].Pos;
            Vector3 b = Nodes[e.B].Pos;
            float h = e.Width * 0.5f;
            if (e.AlongX) return new RectF(Mathf.Min(a.x, b.x), a.z - h, Mathf.Max(a.x, b.x), a.z + h);
            return new RectF(a.x - h, Mathf.Min(a.z, b.z), a.x + h, Mathf.Max(a.z, b.z));
        }

        public bool NearAnyRoad(Vector3 p, float margin)
        {
            foreach (RoadEdge e in Edges)
            {
                if (EdgeRect(e).Inset(-margin).Contains(p)) return true;
            }
            return false;
        }

        public Block BlockAt(Vector3 p)
        {
            List<int> l;
            if (!_blockHash.TryGetValue(Key(Cell(p.x), Cell(p.z)), out l)) return null;
            for (int i = 0; i < l.Count; i++)
            {
                Block b = Blocks[l[i]];
                if (b.Curb.Contains(p)) return b;
            }
            return null;
        }

        public bool IsInsideBuilding(Vector3 p, float margin)
        {
            if (Gothic04OB)
            {
                // A moved parcel can end in another hash cell. A wide encounter or companion probe must search its full radius.
                float radius=Mathf.Max(0f,margin);List<int> nearby;
                for(int x=Cell(p.x-radius);x<=Cell(p.x+radius);x++)for(int z=Cell(p.z-radius);z<=Cell(p.z+radius);z++)
                    if(_blockHash.TryGetValue(Key(x,z),out nearby))
                        foreach(int id in nearby)foreach(Lot lot in Blocks[id].Lots)foreach(Building bld in lot.Buildings)
                            if(GothicRealmGeometry04OB.BlocksGround04OB(bld,p,margin))return true;
                foreach(Lot lot in DesertLots)foreach(Building bld in lot.Buildings)if(bld.Footprint.Inset(-margin).Contains(p))return true;
                return false;
            }
            Block b = BlockAt(p);
            if (b != null)
            {
                foreach (Lot lot in b.Lots)
                    foreach (Building bld in lot.Buildings)
                        if (bld.Footprint.Inset(-margin).Contains(p)) return true;
                return false;
            }
            foreach (Lot lot in DesertLots)
                foreach (Building bld in lot.Buildings)
                    if (bld.Footprint.Inset(-margin).Contains(p)) return true;
            return false;
        }

        public RoadEdge RoadAt(Vector3 p, float margin)
        {
            List<int> l;
            if (!_edgeHash.TryGetValue(Key(Cell(p.x), Cell(p.z)), out l)) return null;
            for (int i = 0; i < l.Count; i++)
            {
                RoadEdge e = Edges[l[i]];
                if (EdgeRect(e).Inset(-margin).Contains(p)) return e;
            }
            return null;
        }

        public bool IsOnRoadOrIntersection(Vector3 p)
        {
            if (RoadAt(p, 0f) != null) return true;
            int n = NearestNode(p);
            if (n < 0) return false;
            RoadNode node = Nodes[n];
            return Mathf.Abs(p.x - node.Pos.x) <= node.HalfX && Mathf.Abs(p.z - node.Pos.z) <= node.HalfZ;
        }

        public int NearestNode(Vector3 p)
        {
            int best = -1;
            float bd = float.MaxValue;
            for (int i = 0; i < Nodes.Count; i++)
            {
                float d = MathUtil.FlatSqrDistance(Nodes[i].Pos, p);
                if (d < bd)
                {
                    bd = d;
                    best = i;
                }
            }
            return best;
        }

        /// <summary>Nearest lane within maxDist. Returns -1 if none. along = distance along the lane.</summary>
        public int NearestLane(Vector3 p, float maxDist, out float along, bool straightOnly)
        {
            along = 0f;
            int best = -1;
            float bestD = maxDist * maxDist;
            int r = Mathf.CeilToInt(maxDist / HashCell);
            int cx = Cell(p.x), cz = Cell(p.z);
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dz = -r; dz <= r; dz++)
                {
                    List<int> l;
                    if (!_laneHash.TryGetValue(Key(cx + dx, cz + dz), out l)) continue;
                    for (int i = 0; i < l.Count; i++)
                    {
                        LanePath lane = Lanes[l[i]];
                        if (straightOnly && lane.IsConnector) continue;
                        float a = lane.Project(p);
                        Vector3 c = lane.PointAt(a);
                        float d = MathUtil.FlatSqrDistance(c, p);
                        if (d < bestD)
                        {
                            bestD = d;
                            best = lane.Id;
                            along = a;
                        }
                    }
                }
            }
            return best;
        }

        /// <summary>Nearest straight lane within maxDist whose direction is within about 80 degrees of heading. -1 if none.</summary>
        public int NearestLaneHeading(Vector3 p, float maxDist, Vector3 heading, out float along)
        {
            along = 0f;
            int best = -1;
            float bestD = maxDist * maxDist;
            Vector3 h = MathUtil.Flat(heading);
            if (h.sqrMagnitude < 0.0001f) return -1;
            h.Normalize();
            int r = Mathf.CeilToInt(maxDist / HashCell);
            int cx = Cell(p.x), cz = Cell(p.z);
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dz = -r; dz <= r; dz++)
                {
                    List<int> l;
                    if (!_laneHash.TryGetValue(Key(cx + dx, cz + dz), out l)) continue;
                    for (int i = 0; i < l.Count; i++)
                    {
                        LanePath lane = Lanes[l[i]];
                        if (lane.IsConnector) continue;
                        float a = lane.Project(p);
                        Vector3 c = lane.PointAt(a);
                        float d = MathUtil.FlatSqrDistance(c, p);
                        if (d >= bestD || Vector3.Dot(lane.DirectionAt(a), h) < 0.2f) continue;
                        bestD = d;
                        best = lane.Id;
                        along = a;
                    }
                }
            }
            return best;
        }

        public void LanesNear(Vector3 p, float radius, List<int> output)
        {
            output.Clear();
            int r = Mathf.CeilToInt(radius / HashCell);
            int cx = Cell(p.x), cz = Cell(p.z);
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dz = -r; dz <= r; dz++)
                {
                    List<int> l;
                    if (!_laneHash.TryGetValue(Key(cx + dx, cz + dz), out l)) continue;
                    for (int i = 0; i < l.Count; i++) if (!output.Contains(l[i])) output.Add(l[i]);
                }
            }
        }

        public int NearestWalkNode(Vector3 p, float maxDist)
        {
            int best = -1;
            float bestD = maxDist * maxDist;
            int r = Mathf.CeilToInt(maxDist / HashCell);
            int cx = Cell(p.x), cz = Cell(p.z);
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dz = -r; dz <= r; dz++)
                {
                    List<int> l;
                    if (!_walkHash.TryGetValue(Key(cx + dx, cz + dz), out l)) continue;
                    for (int i = 0; i < l.Count; i++)
                    {
                        float d = MathUtil.FlatSqrDistance(WalkNodes[l[i]].Pos, p);
                        if (d < bestD)
                        {
                            bestD = d;
                            best = l[i];
                        }
                    }
                }
            }
            return best;
        }

        public void WalkNodesNear(Vector3 p, float radius, List<int> output)
        {
            output.Clear();
            int r = Mathf.CeilToInt(radius / HashCell);
            int cx = Cell(p.x), cz = Cell(p.z);
            float r2 = radius * radius;
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dz = -r; dz <= r; dz++)
                {
                    List<int> l;
                    if (!_walkHash.TryGetValue(Key(cx + dx, cz + dz), out l)) continue;
                    for (int i = 0; i < l.Count; i++)
                        if (MathUtil.FlatSqrDistance(WalkNodes[l[i]].Pos, p) <= r2) output.Add(l[i]);
                }
            }
        }

        public Poi NearestPoi(Vector3 p, PoiType type)
        {
            Poi best = null;
            float bd = float.MaxValue;
            foreach (Poi poi in Pois)
            {
                if (poi.Type != type) continue;
                float d = MathUtil.FlatSqrDistance(poi.Position, p);
                if (d < bd) { bd = d; best = poi; }
            }
            return best;
        }

        public Poi FindPoi(string name)
        {
            foreach (Poi poi in Pois) if (poi.Name == name) return poi;
            return null;
        }

        // ================================================================== routing

        /// <summary>
        /// A* over the road graph. Returns a polyline from 'from' to 'to' following road centerlines,
        /// or an empty list if no route exists.
        /// </summary>
        public List<Vector3> FindRoute(Vector3 from, Vector3 to)
        {
            List<Vector3> result = new List<Vector3>();
            int startEdge = NearestEdge(from);
            int goalEdge = NearestEdge(to);
            if (startEdge < 0 || goalEdge < 0) return result;
            RoadEdge se = Edges[startEdge];
            RoadEdge ge = Edges[goalEdge];
            Vector3 startProj = ProjectOnEdge(se, from);
            Vector3 goalProj = ProjectOnEdge(ge, to);

            if (startEdge == goalEdge)
            {
                result.Add(from);
                result.Add(startProj);
                result.Add(goalProj);
                result.Add(to);
                return result;
            }

            int n = Nodes.Count;
            float[] g = new float[n];
            int[] parent = new int[n];
            bool[] closed = new bool[n];
            for (int i = 0; i < n; i++) { g[i] = float.MaxValue; parent[i] = -1; }
            List<int> open = new List<int>();
            Action<int, float> seed = (node, cost) =>
            {
                if (cost < g[node])
                {
                    g[node] = cost;
                    parent[node] = -1;
                    if (!open.Contains(node)) open.Add(node);
                }
            };
            seed(se.A, Vector3.Distance(startProj, Nodes[se.A].Pos));
            seed(se.B, Vector3.Distance(startProj, Nodes[se.B].Pos));
            HashSet<int> goals = new HashSet<int> { ge.A, ge.B };
            int reached = -1;
            float bestTotal = float.MaxValue;
            while (open.Count > 0)
            {
                int bi = 0;
                float bf = float.MaxValue;
                for (int i = 0; i < open.Count; i++)
                {
                    int node = open[i];
                    float f = g[node] + MathUtil.FlatDistance(Nodes[node].Pos, goalProj);
                    if (f < bf) { bf = f; bi = i; }
                }
                int cur = open[bi];
                open.RemoveAt(bi);
                if (closed[cur]) continue;
                closed[cur] = true;
                if (goals.Contains(cur))
                {
                    float total = g[cur] + Vector3.Distance(Nodes[cur].Pos, goalProj);
                    if (total < bestTotal)
                    {
                        bestTotal = total;
                        reached = cur;
                    }
                    if (bf >= bestTotal) break;
                }
                foreach (int eid in Nodes[cur].Edges)
                {
                    RoadEdge e = Edges[eid];
                    int nb = e.A == cur ? e.B : e.A;
                    if (closed[nb]) continue;
                    float cost = g[cur] + MathUtil.FlatDistance(Nodes[cur].Pos, Nodes[nb].Pos) * (e.Kind == RoadKind.Highway ? 0.8f : 1f);
                    if (cost < g[nb])
                    {
                        g[nb] = cost;
                        parent[nb] = cur;
                        if (!open.Contains(nb)) open.Add(nb);
                    }
                }
            }
            if (reached < 0) return result;
            List<int> chain = new List<int>();
            int c = reached;
            int guard = 0;
            while (c >= 0 && guard < n)
            {
                chain.Add(c);
                c = parent[c];
                guard++;
            }
            chain.Reverse();
            result.Add(from);
            result.Add(startProj);
            foreach (int node in chain) result.Add(Nodes[node].Pos);
            result.Add(goalProj);
            result.Add(to);
            return result;
        }

        public int NearestEdge(Vector3 p)
        {
            int best = -1;
            float bd = float.MaxValue;
            for (int i = 0; i < Edges.Count; i++)
            {
                Vector3 c = ProjectOnEdge(Edges[i], p);
                float d = MathUtil.FlatSqrDistance(c, p);
                if (d < bd)
                {
                    bd = d;
                    best = i;
                }
            }
            return best;
        }

        public Vector3 ProjectOnEdge(RoadEdge e, Vector3 p)
        {
            float t;
            Vector3 c = MathUtil.ClosestPointOnSegment(Nodes[e.A].Pos, Nodes[e.B].Pos, p, out t);
            c.y = 0f;
            return c;
        }
    }
}
