using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The ten Las Veyra districts (from the Mythic Las Veyra realm list) plus the open desert and the snow zone.</summary>
    public enum District
    {
        LasVeyraCore,
        GlassHeights,
        MirageStrip,
        OldSilver,
        BrassProjects,
        AshUnderpass,
        SunkenRow,
        GraveFrequency,
        SkyrailOrbit,
        QuartzBadlands,
        /// <summary>The snow zone beyond the north-west beltway (wilderness, like the Quartz Badlands).</summary>
        Frostline
    }

    public enum RoadKind
    {
        Street,
        Avenue,
        Boulevard,
        Highway,
        Rural
    }

    public enum LotUse
    {
        Building,
        Park,
        Parking,
        Plaza,
        House,
        Warehouse,
        Casino,
        Tower,
        Projects,
        Special,
        Cemetery,
        Court,
        Empty
    }

    public enum RoofType
    {
        Flat,
        FlatWithProps,
        Gable,
        Helipad,
        Antenna
    }

    public enum PoiType
    {
        None,
        Safehouse,
        Hospital,
        Police,
        GunShop,
        Respray,
        GasStation,
        Clinic,
        Garage,
        Casino,
        Bar,
        Diner,
        ConvenienceStore,
        Payphone,
        ATM,
        Vending,
        Motel,
        FoodCart,
        Property,
        Contact,
        Slots,
        Clothing
    }

    public struct RectF
    {
        public float X0, Z0, X1, Z1;

        public RectF(float x0, float z0, float x1, float z1)
        {
            X0 = Mathf.Min(x0, x1);
            Z0 = Mathf.Min(z0, z1);
            X1 = Mathf.Max(x0, x1);
            Z1 = Mathf.Max(z0, z1);
        }

        public float Width { get { return X1 - X0; } }
        public float Depth { get { return Z1 - Z0; } }
        public float CenterX { get { return (X0 + X1) * 0.5f; } }
        public float CenterZ { get { return (Z0 + Z1) * 0.5f; } }
        public Vector3 Center(float y) { return new Vector3(CenterX, y, CenterZ); }
        public float Area { get { return Width * Depth; } }

        public bool Contains(float x, float z)
        {
            return x >= X0 && x <= X1 && z >= Z0 && z <= Z1;
        }

        public bool Contains(Vector3 p)
        {
            return Contains(p.x, p.z);
        }

        public RectF Inset(float d)
        {
            return new RectF(X0 + d, Z0 + d, X1 - d, Z1 - d);
        }

        public RectF Inset(float dx, float dz)
        {
            return new RectF(X0 + dx, Z0 + dz, X1 - dx, Z1 - dz);
        }

        public bool Overlaps(RectF o, float margin)
        {
            return X0 < o.X1 + margin && X1 > o.X0 - margin && Z0 < o.Z1 + margin && Z1 > o.Z0 - margin;
        }

        public bool Valid { get { return X1 > X0 && Z1 > Z0; } }

        public override string ToString()
        {
            return "[" + X0.ToString("0") + "," + Z0.ToString("0") + " .. " + X1.ToString("0") + "," + Z1.ToString("0") + "]";
        }
    }

    public sealed class RoadNode
    {
        public int Id;
        public Vector3 Pos;
        public readonly List<int> Edges = new List<int>();
        public bool Signal;
        /// <summary>Half extents of the intersection box (x from N-S roads, z from E-W roads).</summary>
        public float HalfX = 4f;
        public float HalfZ = 4f;
        public int SignalOffsetSeed;
    }

    public sealed class RoadEdge
    {
        public int Id;
        public int A;
        public int B;
        public RoadKind Kind;
        public int LanesPerDir;
        public float Width;
        public float Median;
        public bool AlongX;
        public string Name;
        public float SpeedLimit;
        /// <summary>True for segments crossing the canal (deck over water).</summary>
        public bool HasBridge;
        public float BridgeZ0;
        public float BridgeZ1;
        public bool ParkingLane;

        public float LaneWidth { get { return Kind == RoadKind.Highway ? 3.7f : 3.5f; } }
    }

    public sealed class Building
    {
        public RectF Footprint;
        public float Height;
        public FacadeStyle Style;
        public int Tiers = 1;
        public bool GroundShops;
        public RoofType Roof;
        public string Sign;
        public int NeonColor;
        public int Seed;
        public int RebirthFamily04O, RebirthWing04O, RebirthSpine04O;
        public int GothicFamily04OB;
        public bool RoofPad04OB;
    }

    public sealed class Lot
    {
        public RectF Area;
        public LotUse Use;
        public readonly List<Building> Buildings = new List<Building>();
        public PoiType Poi;
        public string PoiName;
        /// <summary>Point on the sidewalk in front of the lot entrance.</summary>
        public Vector3 Entrance;
        /// <summary>Outward direction from the lot toward the street.</summary>
        public Vector3 Facing;
        public int Seed;
        /// <summary>
        /// True for desert lots whose buildings are modelled by hand (the Frost Wardens outpost): the footprints still count
        /// for spawning and building checks, but the city builder skips its facade boxes and ground pad for them.
        /// </summary>
        public bool CustomBuilt;
    }

    public sealed class Block
    {
        public int Id;
        /// <summary>Outer curb rectangle (sidewalk included).</summary>
        public RectF Curb;
        /// <summary>Inner area available for lots (inside the sidewalk).</summary>
        public RectF Inner;
        public District District;
        public readonly List<Lot> Lots = new List<Lot>();
        public bool IsPark;
        public bool AlongCanalNorth;
        public bool AlongCanalSouth;
        public int Seed;
        public readonly List<int> WalkNodes = new List<int>();
    }

    public sealed class LanePath
    {
        public int Id;
        public readonly List<Vector3> Points = new List<Vector3>();
        public readonly List<float> Cumulative = new List<float>();
        public float Length;
        public float SpeedLimit;
        public int EdgeId = -1;
        /// <summary>Intersection node this path ends at (straight lanes) or crosses (connectors).</summary>
        public int NodeId = -1;
        public bool IsConnector;
        public int LaneIndex;
        public int LanesInDir;
        public bool EndsAtSignal;
        /// <summary>Signal group: true when this lane approaches its intersection travelling along Z (north-south).</summary>
        public bool AxisNS;
        public readonly List<int> Next = new List<int>();
        public int TurnType; // 0 straight, 1 right, -1 left (connectors)
        public RoadKind Kind;
        /// <summary>Adjacent lane (same road, same direction) toward the center line, or -1.</summary>
        public int LeftNeighbor = -1;
        /// <summary>Adjacent lane (same road, same direction) toward the curb, or -1.</summary>
        public int RightNeighbor = -1;

        public void Finish()
        {
            Cumulative.Clear();
            float total = 0f;
            Cumulative.Add(0f);
            for (int i = 1; i < Points.Count; i++)
            {
                total += Vector3.Distance(Points[i - 1], Points[i]);
                Cumulative.Add(total);
            }
            Length = total;
        }

        public Vector3 PointAt(float distance)
        {
            if (Points.Count == 0) return Vector3.zero;
            if (distance <= 0f) return Points[0];
            if (distance >= Length) return Points[Points.Count - 1];
            int lo = 0;
            int hi = Cumulative.Count - 1;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) >> 1;
                if (Cumulative[mid] <= distance) lo = mid; else hi = mid;
            }
            float segLen = Cumulative[hi] - Cumulative[lo];
            float t = segLen > 1e-5f ? (distance - Cumulative[lo]) / segLen : 0f;
            return Vector3.Lerp(Points[lo], Points[hi], t);
        }

        public Vector3 DirectionAt(float distance)
        {
            if (Points.Count < 2) return Vector3.forward;
            Vector3 a = PointAt(distance - 1f);
            Vector3 b = PointAt(distance + 1f);
            Vector3 d = b - a;
            d.y = 0f;
            return d.sqrMagnitude > 1e-6f ? d.normalized : (Points[Points.Count - 1] - Points[0]).normalized;
        }

        /// <summary>Distance along the path of the closest point to p.</summary>
        public float Project(Vector3 p)
        {
            float best = float.MaxValue;
            float bestDist = 0f;
            for (int i = 1; i < Points.Count; i++)
            {
                float t;
                Vector3 c = MathUtil.ClosestPointOnSegment(Points[i - 1], Points[i], p, out t);
                float d = MathUtil.FlatSqrDistance(c, p);
                if (d < best)
                {
                    best = d;
                    bestDist = Cumulative[i - 1] + (Cumulative[i] - Cumulative[i - 1]) * t;
                }
            }
            return bestDist;
        }
    }

    public sealed class WalkNode
    {
        public int Id;
        public Vector3 Pos;
        public int BlockId = -1;
        public readonly List<int> Links = new List<int>();
    }

    public sealed class WalkLink
    {
        public int Id;
        public int A;
        public int B;
        public bool Crosswalk;
        public int SignalNode = -1;
        /// <summary>True when the crossing goes over a north-south road (peds wait for north-south traffic to stop).</summary>
        public bool CrossesNSRoad;
    }

    public sealed class Poi
    {
        public int Id;
        public PoiType Type;
        public Vector3 Position;
        public Vector3 Facing;
        public string Name;
        public District District;
        public int BlockId = -1;
    }

    public sealed class SpecialSite
    {
        public string Key;
        public string Name;
        public Vector3 Position;
        public float Radius;
    }

    public struct StuntJumpDef
    {
        public Vector3 Position;
        public float Yaw;
        public float Height;
        public float Length;
    }
}
