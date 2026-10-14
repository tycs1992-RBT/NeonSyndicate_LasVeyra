using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Pure-logic generator for the city of Las Veyra: road grid, highway ring, canal, blocks, lots, buildings,
    /// points of interest, the traffic lane graph and the sidewalk graph. No scene objects are created here,
    /// so the whole layout can be generated and tested outside Unity.
    /// </summary>
    public sealed partial class CityLayout
    {
        public const float CoreHalf = 800f;
        public const float RingHalf = 1000f;
        public const float MapHalf = 1600f;
        public const float SidewalkWidth = 4f;
        public const float CurbHeight = 0.15f;
        public const float CanalZ = -560f;
        public const float CanalHalfWidth = 16f;   // water channel half width
        public const float CanalBandHalf = 21f;    // channel plus embankment walks
        public const float CanalDepth = 5.5f;
        public const float StripX = 380f;

        public readonly int Seed;
        public readonly List<RoadNode> Nodes = new List<RoadNode>();
        public readonly List<RoadEdge> Edges = new List<RoadEdge>();
        public readonly List<Block> Blocks = new List<Block>();
        public readonly List<LanePath> Lanes = new List<LanePath>();
        public readonly List<WalkNode> WalkNodes = new List<WalkNode>();
        public readonly List<WalkLink> WalkLinks = new List<WalkLink>();
        public readonly List<Poi> Pois = new List<Poi>();
        public readonly List<SpecialSite> Sites = new List<SpecialSite>();
        public readonly List<Lot> DesertLots = new List<Lot>();
        public readonly List<Vector3> DataChipSpots = new List<Vector3>();
        public readonly List<Vector3> CrystalShardSpots = new List<Vector3>();
        public readonly List<Vector3> SpellStoneSpots = new List<Vector3>();
        public readonly List<Vector3> CreatureSpots = new List<Vector3>();
        public readonly List<StuntJumpDef> StuntJumps = new List<StuntJumpDef>();
        public readonly List<Vector3> PickupSpots = new List<Vector3>();
        public readonly List<Vector3> ParkingSpots = new List<Vector3>();
        public readonly List<float> ParkingYaws = new List<float>();
        public readonly List<Vector3> StreetLampSpots = new List<Vector3>();
        public readonly List<Vector3> TreeSpots = new List<Vector3>();
        public readonly List<Vector3> PalmSpots = new List<Vector3>();
        public readonly List<Vector3> CactusSpots = new List<Vector3>();
        public readonly List<Vector3> RockSpots = new List<Vector3>();
        public readonly List<Vector3> RadioMastSpots = new List<Vector3>();

        private readonly List<GridLine> _xLines = new List<GridLine>();
        private readonly List<GridLine> _zLines = new List<GridLine>();
        private int[,] _nodeGrid;
        private readonly Dictionary<long, List<int>> _laneHash = new Dictionary<long, List<int>>();
        private readonly Dictionary<long, List<int>> _walkHash = new Dictionary<long, List<int>>();
        private readonly Dictionary<long, List<int>> _blockHash = new Dictionary<long, List<int>>();
        private readonly Dictionary<long, List<int>> _edgeHash = new Dictionary<long, List<int>>();
        private const float HashCell = 50f;
        private GRand _rng;

        private sealed class GridLine
        {
            public float Pos;
            public RoadKind Kind;
            public bool Canal;
            public string Name;
        }

        public CityLayout(int seed)
        {
            Seed = seed;
        }

        public IList<float> XLinePositions
        {
            get
            {
                List<float> l = new List<float>();
                for (int i = 0; i < _xLines.Count; i++) l.Add(_xLines[i].Pos);
                return l;
            }
        }

        public IList<float> ZLinePositions
        {
            get
            {
                List<float> l = new List<float>();
                for (int i = 0; i < _zLines.Count; i++) l.Add(_zLines[i].Pos);
                return l;
            }
        }

        // ================================================================== generation entry

        public void Generate()
        {
            _rng = new GRand(Seed);
            BuildGridLines();
            BuildGridGraph();
            ApplySuperblocks();
            BuildHighwayAndDesertRoads();
            FinalizeNodes();
            BuildBlocks();
            BuildLotsAndBuildings();
            BuildDesertSites();
            AssignPois();
            BuildLaneGraph();
            BuildWalkGraph();
            PlaceStreetDressing();
            PlaceCollectiblesAndJumps();
            BuildSpatialHashes();
            // New Frostline content comes last, from its own generator, so nothing generated above moves.
            BuildFrostline();
        }

        // ================================================================== grid lines

        private static readonly string[] StreetRoots =
        {
            "Old Mile", "Echo Park", "Signal Hill", "Neon Mesa", "Metro Foundry", "Quartz Heights", "Cinder Flats", "Rust Canal",
            "Lotus Row", "Battery Yard", "Ashline", "Palm Terminal", "Mirage Bay", "Harbor Loop", "Glass Pier", "Steel Garden",
            "Crown Underpass", "Vanta Market", "Sunset Exchange", "Circuit Ward"
        };

        private static string Ordinal(int n)
        {
            int m100 = n % 100;
            string suffix = "th";
            if (m100 < 11 || m100 > 13)
            {
                if (n % 10 == 1) suffix = "st";
                else if (n % 10 == 2) suffix = "nd";
                else if (n % 10 == 3) suffix = "rd";
            }
            return n + suffix;
        }

        private void BuildGridLines()
        {
            float[] xAnchors = { -800f, -520f, -250f, 0f, 250f, StripX, 520f, 800f };
            RoadKind[] xKinds = { RoadKind.Avenue, RoadKind.Avenue, RoadKind.Avenue, RoadKind.Avenue, RoadKind.Avenue, RoadKind.Boulevard, RoadKind.Avenue, RoadKind.Avenue };
            float[] zAnchors = { -800f, CanalZ, -300f, 0f, 150f, 350f, 560f, 800f };
            RoadKind[] zKinds = { RoadKind.Avenue, RoadKind.Street, RoadKind.Avenue, RoadKind.Avenue, RoadKind.Avenue, RoadKind.Avenue, RoadKind.Avenue, RoadKind.Avenue };

            Subdivide(_xLines, xAnchors, xKinds, 112f, -1);
            Subdivide(_zLines, zAnchors, zKinds, 96f, 1);

            int rootIndex = 0;
            for (int i = 0; i < _xLines.Count; i++)
            {
                GridLine g = _xLines[i];
                if (Mathf.Abs(g.Pos - StripX) < 1f) g.Name = "Mirage Strip Boulevard";
                else if (Mathf.Abs(g.Pos) < 1f) g.Name = "Veyra Avenue";
                else
                {
                    g.Name = StreetRoots[rootIndex % StreetRoots.Length] + (g.Kind == RoadKind.Avenue ? " Avenue" : " Street");
                    rootIndex++;
                }
            }
            int ordinal = 1;
            for (int i = 0; i < _zLines.Count; i++)
            {
                GridLine g = _zLines[i];
                if (g.Canal) { g.Name = "Sunken Row Canal"; continue; }
                if (Mathf.Abs(g.Pos) < 1f) g.Name = "Core Avenue";
                else g.Name = Ordinal(ordinal) + (g.Kind == RoadKind.Avenue ? " Avenue" : " Street");
                ordinal++;
            }
        }

        private void Subdivide(List<GridLine> output, float[] anchors, RoadKind[] kinds, float target, int canalAxisFlag)
        {
            output.Clear();
            for (int a = 0; a < anchors.Length; a++)
            {
                GridLine line = new GridLine { Pos = anchors[a], Kind = kinds[a] };
                if (canalAxisFlag > 0 && Mathf.Abs(anchors[a] - CanalZ) < 0.5f) line.Canal = true;
                output.Add(line);
                if (a == anchors.Length - 1) break;
                float gap = anchors[a + 1] - anchors[a];
                int parts = Mathf.Max(1, Mathf.RoundToInt(gap / target));
                float step = gap / parts;
                for (int p = 1; p < parts; p++)
                {
                    float jitter = _rng.Range(-step * 0.09f, step * 0.09f);
                    output.Add(new GridLine { Pos = anchors[a] + step * p + jitter, Kind = RoadKind.Street });
                }
            }
        }

        public static float RoadWidth(RoadKind kind, out int lanesPerDir, out float median, out bool parking)
        {
            parking = false;
            switch (kind)
            {
                case RoadKind.Street: lanesPerDir = 1; median = 0f; parking = true; return 12.2f;
                case RoadKind.Avenue: lanesPerDir = 2; median = 1f; return 15f;
                case RoadKind.Boulevard: lanesPerDir = 3; median = 6f; return 27f;
                case RoadKind.Highway: lanesPerDir = 3; median = 2f; return 26f;
                default: lanesPerDir = 1; median = 0f; return 10f;
            }
        }

        private static float SpeedFor(RoadKind kind)
        {
            switch (kind)
            {
                case RoadKind.Street: return 12f;
                case RoadKind.Avenue: return 15.5f;
                case RoadKind.Boulevard: return 15f;
                case RoadKind.Highway: return 29f;
                default: return 22f;
            }
        }

        private static float HalfWidthOf(GridLine line)
        {
            if (line.Canal) return CanalBandHalf;
            int l; float m; bool p;
            return RoadWidth(line.Kind, out l, out m, out p) * 0.5f;
        }

        // ================================================================== road graph

        private int AddNode(Vector3 pos)
        {
            RoadNode n = new RoadNode { Id = Nodes.Count, Pos = pos };
            Nodes.Add(n);
            return n.Id;
        }

        private RoadEdge AddEdge(int a, int b, RoadKind kind, string name)
        {
            int lanes; float median; bool parking;
            float w = RoadWidth(kind, out lanes, out median, out parking);
            Vector3 pa = Nodes[a].Pos;
            Vector3 pb = Nodes[b].Pos;
            RoadEdge e = new RoadEdge
            {
                Id = Edges.Count,
                A = a,
                B = b,
                Kind = kind,
                LanesPerDir = lanes,
                Width = w,
                Median = median,
                AlongX = Mathf.Abs(pb.x - pa.x) > Mathf.Abs(pb.z - pa.z),
                Name = name,
                SpeedLimit = SpeedFor(kind),
                ParkingLane = parking
            };
            Edges.Add(e);
            Nodes[a].Edges.Add(e.Id);
            Nodes[b].Edges.Add(e.Id);
            return e;
        }

        private void BuildGridGraph()
        {
            _nodeGrid = new int[_xLines.Count, _zLines.Count];
            for (int i = 0; i < _xLines.Count; i++)
            {
                for (int j = 0; j < _zLines.Count; j++)
                {
                    _nodeGrid[i, j] = _zLines[j].Canal ? -1 : AddNode(new Vector3(_xLines[i].Pos, 0f, _zLines[j].Pos));
                }
            }
            // E-W edges along each z line
            for (int j = 0; j < _zLines.Count; j++)
            {
                if (_zLines[j].Canal) continue;
                for (int i = 0; i < _xLines.Count - 1; i++)
                {
                    AddEdge(_nodeGrid[i, j], _nodeGrid[i + 1, j], _zLines[j].Kind, _zLines[j].Name);
                }
            }
            // N-S edges along each x line (bridging over the canal line)
            for (int i = 0; i < _xLines.Count; i++)
            {
                int j = 0;
                while (j < _zLines.Count - 1)
                {
                    int j2 = j + 1;
                    bool bridge = false;
                    while (j2 < _zLines.Count && _zLines[j2].Canal)
                    {
                        j2++;
                        bridge = true;
                    }
                    if (j2 >= _zLines.Count) break;
                    RoadEdge e = AddEdge(_nodeGrid[i, j], _nodeGrid[i, j2], _xLines[i].Kind, _xLines[i].Name);
                    if (bridge)
                    {
                        e.HasBridge = true;
                        e.BridgeZ0 = CanalZ - CanalHalfWidth - 1f;
                        e.BridgeZ1 = CanalZ + CanalHalfWidth + 1f;
                    }
                    j = j2;
                }
            }
        }

        private int XIndexNear(float x)
        {
            int best = 0;
            float bd = float.MaxValue;
            for (int i = 0; i < _xLines.Count; i++)
            {
                float d = Mathf.Abs(_xLines[i].Pos - x);
                if (d < bd) { bd = d; best = i; }
            }
            return best;
        }

        private int ZIndexNear(float z)
        {
            int best = 0;
            float bd = float.MaxValue;
            for (int j = 0; j < _zLines.Count; j++)
            {
                if (_zLines[j].Canal) continue;
                float d = Mathf.Abs(_zLines[j].Pos - z);
                if (d < bd) { bd = d; best = j; }
            }
            return best;
        }

        private sealed class Merge
        {
            public int I0, I1, J0, J1;
            public LotUse Use;
            public District? Force;
            public string Tag;
        }

        private readonly List<Merge> _merges = new List<Merge>();

        private void ApplySuperblocks()
        {
            // Echo Park: big central park at the top of Glass Heights.
            AddMerge(XIndexNear(-125f), XIndexNear(125f), ZIndexNear(560f), ZIndexNear(800f), LotUse.Park, null, "Echo Park");
            // Ash Underpass rail yard.
            AddMerge(XIndexNear(-800f), XIndexNear(-520f), ZIndexNear(-800f), ZIndexNear(CanalZ - 90f), LotUse.Warehouse, District.AshUnderpass, "Battery Rail Yard");
            // Grave Frequency cemetery.
            AddMerge(XIndexNear(-700f), XIndexNear(-520f), ZIndexNear(560f), ZIndexNear(800f), LotUse.Cemetery, District.GraveFrequency, "Grave Frequency Cemetery");
            // Casino resorts: west side of the Strip merges pairs of blocks.
            int si = XIndexNear(250f);
            int sb = XIndexNear(StripX);
            int se = XIndexNear(520f);
            for (int j = ZIndexNear(-300f); j + 2 < _zLines.Count; j += 3)
            {
                int j2 = j + 2;
                if (_zLines[j2].Pos > 790f) break;
                if (HasCanalBetween(j, j2)) continue;
                AddMerge(si, sb, j, j2, LotUse.Casino, District.MirageStrip, "Resort");
            }
            for (int j = ZIndexNear(-150f); j + 2 < _zLines.Count; j += 3)
            {
                int j2 = j + 2;
                if (_zLines[j2].Pos > 790f) break;
                if (HasCanalBetween(j, j2)) continue;
                AddMerge(sb, se, j, j2, LotUse.Casino, District.MirageStrip, "Resort");
            }
            foreach (Merge m in _merges) RemoveInteriorEdges(m);
        }

        private bool HasCanalBetween(int j0, int j1)
        {
            for (int j = j0; j <= j1; j++) if (_zLines[j].Canal) return true;
            return false;
        }

        private void AddMerge(int i0, int i1, int j0, int j1, LotUse use, District? force, string tag)
        {
            if (i1 <= i0 || j1 <= j0) return;
            if (HasCanalBetween(j0, j1)) return;
            Merge m = new Merge { I0 = i0, I1 = i1, J0 = j0, J1 = j1, Use = use, Force = force, Tag = tag };
            foreach (Merge other in _merges)
            {
                bool overlap = m.I0 < other.I1 && m.I1 > other.I0 && m.J0 < other.J1 && m.J1 > other.J0;
                if (overlap) return;
            }
            _merges.Add(m);
        }

        private void RemoveInteriorEdges(Merge m)
        {
            float x0 = _xLines[m.I0].Pos, x1 = _xLines[m.I1].Pos, z0 = _zLines[m.J0].Pos, z1 = _zLines[m.J1].Pos;
            for (int e = 0; e < Edges.Count; e++)
            {
                RoadEdge edge = Edges[e];
                if (edge == null) continue;
                Vector3 a = Nodes[edge.A].Pos;
                Vector3 b = Nodes[edge.B].Pos;
                Vector3 mid = (a + b) * 0.5f;
                bool inside = mid.x > x0 + 0.5f && mid.x < x1 - 0.5f && mid.z > z0 + 0.5f && mid.z < z1 - 0.5f;
                if (inside) RemoveEdge(e);
            }
        }

        private void RemoveEdge(int e)
        {
            RoadEdge edge = Edges[e];
            if (edge == null) return;
            Nodes[edge.A].Edges.Remove(e);
            Nodes[edge.B].Edges.Remove(e);
            Edges[e] = null;
        }

        private int FindNodeAt(float x, float z)
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                if (Nodes[i] == null) continue;
                if (Mathf.Abs(Nodes[i].Pos.x - x) < 0.5f && Mathf.Abs(Nodes[i].Pos.z - z) < 0.5f) return i;
            }
            return -1;
        }

        private void BuildHighwayAndDesertRoads()
        {
            float R = RingHalf;
            // Junction points on each ring side (sorted along the side).
            List<float> north = new List<float> { -R, 0f, 300f, R };
            List<float> south = new List<float> { -R, 0f, R };
            List<float> east = new List<float> { -R, -350f, 0f, 750f, R };
            List<float> west = new List<float> { -R, -700f, 0f, 700f, R };
            Dictionary<string, int> ringNode = new Dictionary<string, int>();
            Func<float, float, int> node = (x, z) =>
            {
                string key = Mathf.RoundToInt(x) + "," + Mathf.RoundToInt(z);
                int id;
                if (!ringNode.TryGetValue(key, out id))
                {
                    id = AddNode(new Vector3(x, 0f, z));
                    ringNode[key] = id;
                }
                return id;
            };
            for (int i = 0; i < north.Count - 1; i++) AddEdge(node(north[i], R), node(north[i + 1], R), RoadKind.Highway, "Badlands Beltway");
            for (int i = 0; i < south.Count - 1; i++) AddEdge(node(south[i], -R), node(south[i + 1], -R), RoadKind.Highway, "Badlands Beltway");
            for (int i = 0; i < east.Count - 1; i++) AddEdge(node(R, east[i]), node(R, east[i + 1]), RoadKind.Highway, "Badlands Beltway");
            for (int i = 0; i < west.Count - 1; i++) AddEdge(node(-R, west[i]), node(-R, west[i + 1]), RoadKind.Highway, "Badlands Beltway");

            // Connectors from the city edge to the ring.
            ConnectToRing(FindNodeAt(0f, CoreHalf), node(0f, R), "Veyra Avenue North");
            ConnectToRing(FindNodeAt(0f, -CoreHalf), node(0f, -R), "Veyra Avenue South");
            ConnectToRing(FindNodeAt(CoreHalf, 0f), node(R, 0f), "Core Avenue East");
            ConnectToRing(FindNodeAt(-CoreHalf, 0f), node(-R, 0f), "Core Avenue West");

            // Rural spurs to desert sites.
            int motel = AddNode(new Vector3(1230f, 0f, -350f));
            AddEdge(node(R, -350f), motel, RoadKind.Rural, "Starlite Road");
            int air = AddNode(new Vector3(1260f, 0f, 750f));
            AddEdge(node(R, 750f), air, RoadKind.Rural, "Airfield Road");
            int canyon = AddNode(new Vector3(300f, 0f, 1380f));
            AddEdge(node(300f, R), canyon, RoadKind.Rural, "Canyon Road");
            int quarry = AddNode(new Vector3(-1270f, 0f, 700f));
            AddEdge(node(-R, 700f), quarry, RoadKind.Rural, "Quarry Road");
            int trailer = AddNode(new Vector3(-1220f, 0f, -700f));
            AddEdge(node(-R, -700f), trailer, RoadKind.Rural, "Trailer Row");

            AddSite("StarliteMotel", "Starlite Motel", new Vector3(1290f, 0f, -350f), 60f);
            AddSite("Airstrip", "Palm Terminal Airstrip", new Vector3(1340f, 0f, 750f), 140f);
            AddSite("Canyon", "Glass Row Canyon", new Vector3(300f, 0f, 1450f), 90f);
            AddSite("Quarry", "Quartz Badlands Quarry", new Vector3(-1340f, 0f, 700f), 110f);
            AddSite("TrailerPark", "Dust Devil Trailer Park", new Vector3(-1290f, 0f, -700f), 60f);
            AddSite("CrystalManor", "Crystal Manor Ruins", new Vector3(470f, 0f, 1470f), 45f);
            AddSite("Blockade", "Canyon Road Blockade", new Vector3(300f, 0f, 1230f), 30f);
        }

        private void ConnectToRing(int cityNode, int ringNodeId, string name)
        {
            if (cityNode < 0 || ringNodeId < 0) return;
            AddEdge(cityNode, ringNodeId, RoadKind.Avenue, name);
        }

        private void AddSite(string key, string name, Vector3 pos, float radius)
        {
            Sites.Add(new SpecialSite { Key = key, Name = name, Position = pos, Radius = radius });
        }

        public SpecialSite Site(string key)
        {
            for (int i = 0; i < Sites.Count; i++) if (Sites[i].Key == key) return Sites[i];
            return null;
        }

        private void FinalizeNodes()
        {
            // Compact: drop removed edges and isolated nodes, renumber ids.
            List<RoadEdge> liveEdges = new List<RoadEdge>();
            foreach (RoadEdge e in Edges) if (e != null) liveEdges.Add(e);
            HashSet<int> usedNodes = new HashSet<int>();
            foreach (RoadEdge e in liveEdges) { usedNodes.Add(e.A); usedNodes.Add(e.B); }
            Dictionary<int, int> remap = new Dictionary<int, int>();
            List<RoadNode> liveNodes = new List<RoadNode>();
            for (int i = 0; i < Nodes.Count; i++)
            {
                if (Nodes[i] == null || !usedNodes.Contains(i)) continue;
                remap[i] = liveNodes.Count;
                RoadNode n = Nodes[i];
                n.Id = liveNodes.Count;
                n.Edges.Clear();
                liveNodes.Add(n);
            }
            Nodes.Clear();
            Nodes.AddRange(liveNodes);
            Edges.Clear();
            for (int i = 0; i < liveEdges.Count; i++)
            {
                RoadEdge e = liveEdges[i];
                e.Id = i;
                e.A = remap[e.A];
                e.B = remap[e.B];
                Edges.Add(e);
                Nodes[e.A].Edges.Add(e.Id);
                Nodes[e.B].Edges.Add(e.Id);
            }
            // Rebuild node grid lookups for blocks.
            for (int i = 0; i < _xLines.Count; i++)
                for (int j = 0; j < _zLines.Count; j++)
                    _nodeGrid[i, j] = _zLines[j].Canal ? -1 : FindNodeAt(_xLines[i].Pos, _zLines[j].Pos);

            foreach (RoadNode n in Nodes)
            {
                float hx = 3f, hz = 3f;
                foreach (int eid in n.Edges)
                {
                    RoadEdge e = Edges[eid];
                    if (e.AlongX) hz = Mathf.Max(hz, e.Width * 0.5f);
                    else hx = Mathf.Max(hx, e.Width * 0.5f);
                }
                n.HalfX = hx;
                n.HalfZ = hz;
                bool inCore = Mathf.Abs(n.Pos.x) <= CoreHalf + 1f && Mathf.Abs(n.Pos.z) <= CoreHalf + 1f;
                n.Signal = n.Edges.Count >= 3 && (inCore || IsRingJunction(n));
                n.SignalOffsetSeed = MathUtil.Hash(n.Id, Seed);
            }
        }

        private bool IsRingJunction(RoadNode n)
        {
            bool onRing = Mathf.Abs(Mathf.Abs(n.Pos.x) - RingHalf) < 1f || Mathf.Abs(Mathf.Abs(n.Pos.z) - RingHalf) < 1f;
            return onRing;
        }

        // ================================================================== blocks

        public static District DistrictAt(float x, float z)
        {
            if (Mathf.Abs(x) > CoreHalf + 25f || Mathf.Abs(z) > CoreHalf + 25f) return FrostDepth(x, z) > 0f ? District.Frostline : District.QuartzBadlands;
            if (x < -250f)
            {
                if (z > 350f) return District.GraveFrequency;
                if (z > -300f) return District.OldSilver;
                return District.AshUnderpass;
            }
            if (x < 250f)
            {
                if (z > 150f) return District.GlassHeights;
                if (z > -300f) return District.LasVeyraCore;
                return District.SunkenRow;
            }
            if (x < 520f) return District.MirageStrip;
            if (z > 100f) return District.SkyrailOrbit;
            return District.BrassProjects;
        }

        public static District DistrictAt(Vector3 p)
        {
            return DistrictAt(p.x, p.z);
        }

        public static string DistrictName(District d)
        {
            switch (d)
            {
                case District.LasVeyraCore: return "Las Veyra Core";
                case District.GlassHeights: return "Glass Heights";
                case District.MirageStrip: return "Mirage Strip";
                case District.OldSilver: return "Old Silver";
                case District.BrassProjects: return "Brass Projects";
                case District.AshUnderpass: return "Ash Underpass";
                case District.SunkenRow: return "Sunken Row";
                case District.GraveFrequency: return "Grave Frequency";
                case District.SkyrailOrbit: return "Skyrail Orbit";
                case District.Frostline: return "The Frostline";
                default: return "Quartz Badlands";
            }
        }

        private Merge MergeContaining(int i, int j)
        {
            foreach (Merge m in _merges)
                if (i >= m.I0 && i < m.I1 && j >= m.J0 && j < m.J1) return m;
            return null;
        }

        private void BuildBlocks()
        {
            HashSet<Merge> done = new HashSet<Merge>();
            for (int i = 0; i < _xLines.Count - 1; i++)
            {
                for (int j = 0; j < _zLines.Count - 1; j++)
                {
                    Merge m = MergeContaining(i, j);
                    int i0 = i, i1 = i + 1, j0 = j, j1 = j + 1;
                    if (m != null)
                    {
                        if (done.Contains(m)) continue;
                        done.Add(m);
                        i0 = m.I0; i1 = m.I1; j0 = m.J0; j1 = m.J1;
                    }
                    float x0 = _xLines[i0].Pos + HalfWidthOf(_xLines[i0]);
                    float x1 = _xLines[i1].Pos - HalfWidthOf(_xLines[i1]);
                    float z0 = _zLines[j0].Pos + HalfWidthOf(_zLines[j0]);
                    float z1 = _zLines[j1].Pos - HalfWidthOf(_zLines[j1]);
                    if (x1 - x0 < 12f || z1 - z0 < 12f) continue;
                    Block b = new Block
                    {
                        Id = Blocks.Count,
                        Curb = new RectF(x0, z0, x1, z1),
                        Seed = MathUtil.Hash(i * 131 + j, Seed)
                    };
                    b.Inner = b.Curb.Inset(SidewalkWidth);
                    b.District = m != null && m.Force.HasValue ? m.Force.Value : DistrictAt(b.Curb.CenterX, b.Curb.CenterZ);
                    b.AlongCanalNorth = _zLines[j0].Canal;
                    b.AlongCanalSouth = _zLines[j1].Canal;
                    if (m != null)
                    {
                        Lot lot = new Lot { Area = b.Inner, Use = m.Use, Seed = b.Seed, PoiName = m.Tag };
                        b.Lots.Add(lot);
                        b.IsPark = m.Use == LotUse.Park || m.Use == LotUse.Cemetery;
                    }
                    Blocks.Add(b);
                }
            }
        }

        // ================================================================== lots and buildings

        private void BuildLotsAndBuildings()
        {
            foreach (Block b in Blocks)
            {
                GRand r = new GRand(b.Seed);
                if (b.Lots.Count > 0)
                {
                    // Pre-assigned superblock (park, casino, rail yard, cemetery).
                    Lot lot = b.Lots[0];
                    FillSuperLot(b, lot, r);
                    continue;
                }
                // Occasional small parks and parking lots.
                float parkChance = 0.05f;
                if (b.District == District.BrassProjects || b.District == District.SunkenRow || b.District == District.OldSilver) parkChance = 0.1f;
                if (b.District == District.GlassHeights) parkChance = 0.04f;
                if (r.Chance(parkChance))
                {
                    Lot park = new Lot { Area = b.Inner, Use = LotUse.Park, Seed = b.Seed };
                    b.Lots.Add(park);
                    b.IsPark = true;
                    continue;
                }
                if (b.District == District.MirageStrip)
                {
                    Lot strip = new Lot { Area = b.Inner, Seed = r.Range(0, 1000000) };
                    b.Lots.Add(strip);
                    if (b.Inner.Area > 5200f && _casinoIndex < CasinoNames.Length)
                    {
                        strip.Use = LotUse.Casino;
                        AddCasino(strip, r);
                    }
                    else
                    {
                        strip.Use = LotUse.Building;
                        Building hotel = AddBlockBuilding(strip, r, r.Range(18f, 60f), r.Chance(0.5f) ? FacadeStyle.Casino : FacadeStyle.GoldGlass, true, 3f);
                        hotel.Sign = r.Pick(new[] { "HOTEL", "MOTEL", "LOUNGE", "WEDDINGS", "BUFFET", "SHOWS", "PAWN", "LIQUOR", "SLOTS", "DINER" });
                    }
                    continue;
                }
                List<RectF> pieces = new List<RectF>();
                float maxLot = LotSizeFor(b.District);
                SplitRect(b.Inner, maxLot, r, pieces, 0);
                foreach (RectF piece in pieces)
                {
                    Lot lot = new Lot { Area = piece, Seed = r.Range(0, 1000000) };
                    b.Lots.Add(lot);
                    DecideLot(b, lot, r);
                }
            }
        }

        private static float LotSizeFor(District d)
        {
            switch (d)
            {
                case District.GlassHeights: return 70f;
                case District.LasVeyraCore: return 55f;
                case District.OldSilver: return 24f;
                case District.SunkenRow: return 34f;
                case District.BrassProjects: return 48f;
                case District.AshUnderpass: return 60f;
                case District.GraveFrequency: return 36f;
                case District.SkyrailOrbit: return 65f;
                default: return 45f;
            }
        }

        private void SplitRect(RectF rect, float maxSize, GRand r, List<RectF> output, int depth)
        {
            bool tooWide = rect.Width > maxSize * 1.15f;
            bool tooDeep = rect.Depth > maxSize * 1.15f;
            if ((!tooWide && !tooDeep) || depth > 6)
            {
                output.Add(rect);
                return;
            }
            bool splitX = tooWide && (!tooDeep || rect.Width >= rect.Depth);
            float t = r.Range(0.38f, 0.62f);
            if (splitX)
            {
                float x = rect.X0 + rect.Width * t;
                SplitRect(new RectF(rect.X0, rect.Z0, x, rect.Z1), maxSize, r, output, depth + 1);
                SplitRect(new RectF(x, rect.Z0, rect.X1, rect.Z1), maxSize, r, output, depth + 1);
            }
            else
            {
                float z = rect.Z0 + rect.Depth * t;
                SplitRect(new RectF(rect.X0, rect.Z0, rect.X1, z), maxSize, r, output, depth + 1);
                SplitRect(new RectF(rect.X0, z, rect.X1, rect.Z1), maxSize, r, output, depth + 1);
            }
        }

        private void DecideLot(Block b, Lot lot, GRand r)
        {
            RectF a = lot.Area;
            float centerDist = new Vector2(a.CenterX, a.CenterZ).magnitude;
            switch (b.District)
            {
                case District.GlassHeights:
                    lot.Use = LotUse.Tower;
                    AddTower(lot, r, r.Range(70f, 210f) * Mathf.Lerp(1.15f, 0.75f, Mathf.Clamp01(centerDist / 700f)), r.Pick(new[] { FacadeStyle.OfficeGlass, FacadeStyle.DarkGlass, FacadeStyle.GoldGlass, FacadeStyle.OfficeGlass }));
                    break;
                case District.LasVeyraCore:
                    if (r.Chance(0.08f)) { lot.Use = LotUse.Parking; break; }
                    if (r.Chance(0.06f)) { lot.Use = LotUse.Plaza; break; }
                    lot.Use = LotUse.Building;
                    AddBlockBuilding(lot, r, r.Range(22f, 95f), r.Pick(new[] { FacadeStyle.OfficeGlass, FacadeStyle.Apartment, FacadeStyle.Brick, FacadeStyle.DarkGlass, FacadeStyle.Apartment }), true, 2f);
                    break;
                case District.OldSilver:
                    lot.Use = LotUse.Building;
                    AddBlockBuilding(lot, r, r.Range(7f, 16f), r.Pick(new[] { FacadeStyle.Stucco, FacadeStyle.StuccoPink, FacadeStyle.Brick, FacadeStyle.Stucco }), r.Chance(0.75f), 0.4f);
                    break;
                case District.SunkenRow:
                    if (r.Chance(0.1f)) { lot.Use = LotUse.Parking; break; }
                    lot.Use = LotUse.Building;
                    AddBlockBuilding(lot, r, r.Range(8f, 24f), r.Pick(new[] { FacadeStyle.Brick, FacadeStyle.Apartment, FacadeStyle.Stucco, FacadeStyle.Warehouse }), r.Chance(0.5f), 1.2f);
                    break;
                case District.BrassProjects:
                    if (r.Chance(0.22f)) { lot.Use = LotUse.Court; break; }
                    lot.Use = LotUse.Projects;
                    AddBlockBuilding(lot, r, r.Range(30f, 58f), FacadeStyle.Apartment, false, 6f);
                    break;
                case District.AshUnderpass:
                    if (r.Chance(0.15f)) { lot.Use = LotUse.Parking; break; }
                    lot.Use = LotUse.Warehouse;
                    AddBlockBuilding(lot, r, r.Range(7f, 15f), FacadeStyle.Warehouse, false, 4f);
                    break;
                case District.GraveFrequency:
                    if (r.Chance(0.12f)) { lot.Use = LotUse.Empty; RadioMastSpots.Add(a.Center(CurbHeight)); break; }
                    lot.Use = LotUse.Building;
                    AddBlockBuilding(lot, r, r.Range(8f, 22f), r.Pick(new[] { FacadeStyle.Brick, FacadeStyle.Stucco, FacadeStyle.Warehouse }), r.Chance(0.4f), 2.5f);
                    break;
                case District.SkyrailOrbit:
                    if (r.Chance(0.18f)) { lot.Use = LotUse.Parking; break; }
                    lot.Use = LotUse.Building;
                    AddBlockBuilding(lot, r, r.Range(14f, 55f), r.Pick(new[] { FacadeStyle.OfficeGlass, FacadeStyle.Warehouse, FacadeStyle.DarkGlass }), r.Chance(0.4f), 3f);
                    break;
                case District.MirageStrip:
                    lot.Use = LotUse.Casino;
                    AddCasino(lot, r);
                    break;
                default:
                    lot.Use = LotUse.Building;
                    AddBlockBuilding(lot, r, r.Range(8f, 20f), FacadeStyle.Stucco, false, 2f);
                    break;
            }
        }

        private Building AddBlockBuilding(Lot lot, GRand r, float height, FacadeStyle style, bool shops, float setback)
        {
            RectF a = lot.Area;
            float sx = Mathf.Min(setback + r.Range(0f, 1.5f), a.Width * 0.2f);
            float sz = Mathf.Min(setback + r.Range(0f, 1.5f), a.Depth * 0.2f);
            RectF fp = a.Inset(sx, sz);
            if (!fp.Valid || fp.Width < 5f || fp.Depth < 5f) fp = a.Inset(0.5f);
            Building bld = new Building
            {
                Footprint = fp,
                Height = Mathf.Max(5f, height),
                Style = style,
                GroundShops = shops,
                Roof = height > 40f ? (r.Chance(0.3f) ? RoofType.Antenna : RoofType.FlatWithProps) : (r.Chance(0.6f) ? RoofType.FlatWithProps : RoofType.Flat),
                Tiers = height > 70f ? r.Range(1, 3) : 1,
                Seed = r.Range(0, 1000000),
                NeonColor = r.Range(0, 8)
            };
            lot.Buildings.Add(bld);
            return bld;
        }

        private void AddTower(Lot lot, GRand r, float height, FacadeStyle style)
        {
            RectF a = lot.Area;
            RectF fp = a.Inset(Mathf.Min(6f, a.Width * 0.18f), Mathf.Min(6f, a.Depth * 0.18f));
            if (!fp.Valid) fp = a.Inset(1f);
            Building bld = new Building
            {
                Footprint = fp,
                Height = height,
                Style = style,
                GroundShops = true,
                Roof = r.Chance(0.35f) ? RoofType.Helipad : RoofType.Antenna,
                Tiers = r.Range(2, 4),
                Seed = r.Range(0, 1000000),
                NeonColor = r.Range(0, 8)
            };
            lot.Buildings.Add(bld);
        }

        private static readonly string[] CasinoNames =
        {
            "MIRAGE PALACE", "LOTUS CASINO", "GOLDEN ORBIT", "THE NEON SPHINX", "CINDER ROYALE", "QUARTZ CROWN",
            "THE GLASS SAINT", "STATIC GARDEN", "EMBER COURT HOTEL", "VEYRA GRAND", "SKYRAIL TOWERS", "THE BRASS LANTERN"
        };

        private int _casinoIndex;

        private void AddCasino(Lot lot, GRand r)
        {
            RectF a = lot.Area;
            float podiumH = r.Range(14f, 24f);
            RectF podium = a.Inset(Mathf.Min(5f, a.Width * 0.12f), Mathf.Min(6f, a.Depth * 0.1f));
            if (!podium.Valid) podium = a.Inset(1f);
            string name = _casinoIndex < CasinoNames.Length ? CasinoNames[_casinoIndex] : null;
            _casinoIndex++;
            Building pod = new Building { Footprint = podium, Height = podiumH, Style = FacadeStyle.Casino, GroundShops = true, Roof = RoofType.Flat, Seed = r.Range(0, 1000000), Sign = name ?? "CASINO", NeonColor = r.Range(0, 8) };
            lot.Buildings.Add(pod);
            float tw = Mathf.Min(podium.Width * 0.55f, 40f);
            float td = Mathf.Min(podium.Depth * 0.4f, 34f);
            float cx = podium.CenterX + r.Range(-podium.Width * 0.15f, podium.Width * 0.15f);
            float cz = podium.CenterZ + r.Range(-podium.Depth * 0.2f, podium.Depth * 0.2f);
            RectF towerFp = new RectF(cx - tw * 0.5f, cz - td * 0.5f, cx + tw * 0.5f, cz + td * 0.5f);
            Building tower = new Building { Footprint = towerFp, Height = podiumH + r.Range(50f, 140f), Style = r.Chance(0.5f) ? FacadeStyle.GoldGlass : FacadeStyle.Casino, Roof = RoofType.Antenna, Tiers = r.Range(1, 3), Seed = r.Range(0, 1000000), NeonColor = r.Range(0, 8) };
            lot.Buildings.Add(tower);
            lot.PoiName = name;
        }

        private void FillSuperLot(Block b, Lot lot, GRand r)
        {
            switch (lot.Use)
            {
                case LotUse.Casino:
                    AddCasino(lot, r);
                    break;
                case LotUse.Warehouse:
                    {
                        // rail yard: long sheds with open ground between
                        RectF a = lot.Area;
                        int sheds = 3;
                        for (int s = 0; s < sheds; s++)
                        {
                            float z0 = a.Z0 + 10f + s * (a.Depth - 20f) / sheds;
                            float z1 = z0 + (a.Depth - 20f) / sheds * 0.45f;
                            Building shed = new Building { Footprint = new RectF(a.X0 + 15f, z0, a.X1 - 40f, z1), Height = r.Range(9f, 14f), Style = FacadeStyle.Warehouse, Roof = RoofType.Flat, Seed = r.Range(0, 100000) };
                            lot.Buildings.Add(shed);
                        }
                    }
                    break;
            }
        }

        // ================================================================== desert sites

        private void BuildDesertSites()
        {
            GRand r = new GRand(Seed * 7 + 3);
            // Starlite Motel: L-shaped motel wings and office.
            Lot motel = new Lot { Area = new RectF(1255f, -395f, 1335f, -300f), Use = LotUse.Special, Poi = PoiType.Motel, PoiName = "Starlite Motel", Seed = 11 };
            motel.Buildings.Add(new Building { Footprint = new RectF(1300f, -392f, 1332f, -305f), Height = 7f, Style = FacadeStyle.StuccoPink, Roof = RoofType.Flat, Sign = "STARLITE MOTEL", NeonColor = 0, Seed = 5 });
            motel.Buildings.Add(new Building { Footprint = new RectF(1262f, -392f, 1300f, -376f), Height = 6.5f, Style = FacadeStyle.StuccoPink, Roof = RoofType.Flat, Seed = 6 });
            motel.Entrance = new Vector3(1295f, CurbHeight, -350f);
            motel.Facing = Vector3.left;
            DesertLots.Add(motel);
            // Gas stations along the beltway.
            AddGasStation(new Vector3(1030f, 0f, 300f), "Fuel+ Beltway East");
            AddGasStation(new Vector3(-1030f, 0f, -300f), "Fuel+ Beltway West");
            AddGasStation(new Vector3(-400f, 0f, 1030f), "Fuel+ Beltway North");
            // Airstrip hangar and tower.
            Lot air = new Lot { Area = new RectF(1270f, 700f, 1420f, 800f), Use = LotUse.Special, PoiName = "Palm Terminal Airstrip", Seed = 21 };
            air.Buildings.Add(new Building { Footprint = new RectF(1280f, 770f, 1330f, 798f), Height = 14f, Style = FacadeStyle.Warehouse, Roof = RoofType.Flat, Seed = 22, Sign = "PALM TERMINAL" });
            air.Buildings.Add(new Building { Footprint = new RectF(1340f, 785f, 1348f, 793f), Height = 24f, Style = FacadeStyle.OfficeGlass, Roof = RoofType.Antenna, Seed = 23 });
            DesertLots.Add(air);
            // Trailer park.
            Lot trailers = new Lot { Area = new RectF(1240f - 2520f, -760f, 1340f - 2520f, -640f), Use = LotUse.Special, PoiName = "Dust Devil Trailer Park", Seed = 31 };
            for (int i = 0; i < 10; i++)
            {
                float tx = trailers.Area.X0 + 8f + (i % 5) * 18f;
                float tz = trailers.Area.Z0 + 15f + (i / 5) * 60f;
                trailers.Buildings.Add(new Building { Footprint = new RectF(tx, tz, tx + 5f, tz + 14f), Height = 3.4f, Style = i % 2 == 0 ? FacadeStyle.Warehouse : FacadeStyle.Stucco, Roof = RoofType.Flat, Seed = 40 + i });
            }
            DesertLots.Add(trailers);
            // Crystal Manor ruins (story site): broken walls, restored later by the story.
            Lot manor = new Lot { Area = new RectF(445f, 1445f, 495f, 1495f), Use = LotUse.Special, PoiName = "Crystal Manor", Seed = 51 };
            manor.Buildings.Add(new Building { Footprint = new RectF(450f, 1485f, 490f, 1490f), Height = 6f, Style = FacadeStyle.Stucco, Roof = RoofType.Flat, Seed = 52 });
            manor.Buildings.Add(new Building { Footprint = new RectF(450f, 1450f, 455f, 1485f), Height = 4.5f, Style = FacadeStyle.Stucco, Roof = RoofType.Flat, Seed = 53 });
            manor.Buildings.Add(new Building { Footprint = new RectF(485f, 1458f, 490f, 1485f), Height = 3.2f, Style = FacadeStyle.Stucco, Roof = RoofType.Flat, Seed = 54 });
            DesertLots.Add(manor);
            // Desert flora.
            for (int i = 0; i < 520; i++)
            {
                Vector3 p = new Vector3(r.Range(-MapHalf + 60f, MapHalf - 60f), 0f, r.Range(-MapHalf + 60f, MapHalf - 60f));
                if (Mathf.Abs(p.x) < CoreHalf + 40f && Mathf.Abs(p.z) < CoreHalf + 40f) continue;
                if (NearAnyRoad(p, 9f) || NearDesertLot(p, 12f)) continue;
                bool cactus = r.Chance(0.62f);
                // Points in the snow become snow-dusted rocks; the random sequence is exactly the same either way.
                if (FrostAmount(p.x, p.z) > 0.02f || IsSnowGround(p.x, p.z)) FrostFloraSpots.Add(p);
                else if (cactus) CactusSpots.Add(p);
                else RockSpots.Add(p);
            }
        }

        private void AddGasStation(Vector3 center, string name)
        {
            // Station sits beside the beltway, outside the road.
            Vector3 c = center;
            Lot lot = new Lot { Area = new RectF(c.x - 22f, c.z - 18f, c.x + 22f, c.z + 18f), Use = LotUse.Special, Poi = PoiType.GasStation, PoiName = name, Seed = MathUtil.Hash((int)c.x, (int)c.z) };
            Vector3 outward = Mathf.Abs(c.x) > Mathf.Abs(c.z) ? new Vector3(Mathf.Sign(c.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(c.z));
            Vector3 shopCenter = c + outward * 12f;
            Vector3 shopHalf = Mathf.Abs(outward.x) > 0.5f ? new Vector3(5f, 0f, 9f) : new Vector3(9f, 0f, 5f);
            lot.Buildings.Add(new Building { Footprint = new RectF(shopCenter.x - shopHalf.x, shopCenter.z - shopHalf.z, shopCenter.x + shopHalf.x, shopCenter.z + shopHalf.z), Height = 4.5f, Style = FacadeStyle.Stucco, Roof = RoofType.Flat, Sign = "FUEL+", NeonColor = 2, Seed = lot.Seed });
            lot.Entrance = shopCenter - outward * (Mathf.Abs(outward.x) > 0.5f ? shopHalf.x + 1.5f : shopHalf.z + 1.5f);
            lot.Entrance.y = CurbHeight;
            lot.Facing = -outward;
            DesertLots.Add(lot);
        }

        private bool NearDesertLot(Vector3 p, float margin)
        {
            foreach (Lot l in DesertLots)
            {
                if (l.Area.Inset(-margin).Contains(p)) return true;
            }
            foreach (SpecialSite s in Sites)
            {
                if (MathUtil.FlatDistance(p, s.Position) < s.Radius + margin) return true;
            }
            return false;
        }
    }
}
