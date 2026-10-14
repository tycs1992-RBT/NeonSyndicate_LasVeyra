using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One distant mountain around the map edge: a half-buried low-poly ellipsoid.</summary>
    public struct MountainSpec
    {
        public Vector3 Position;
        public Vector3 Radii;
        /// <summary>Rotation around the vertical axis in degrees.</summary>
        public float Yaw;
        /// <summary>0 = bare rock, 1 = a heavy snow cap (north and west edges, strongest around the Frostline).</summary>
        public float Snow;
    }

    /// <summary>A flat-topped desert mesa inside the map walls.</summary>
    public struct MesaSpec
    {
        public Vector3 Position;
        public float Radius;
        public float TopRadius;
        public float Height;
    }

    /// <summary>One leaning ice pillar of the Titan Hollow ring.</summary>
    public struct IcePillarSpec
    {
        public Vector3 Position;
        public float Radius;
        public float Height;
        /// <summary>Rotation of the prism around its own axis in degrees.</summary>
        public float Yaw;
        /// <summary>Tilt toward the arena centre in degrees.</summary>
        public float Lean;
    }

    /// <summary>
    /// The Frostline: the snow zone beyond the north-west beltway. Region queries are static and pure so vehicles, weather,
    /// impacts and the map all agree on where the snow is. New content uses its own random generator (seeded from the
    /// layout seed) and is generated after everything else, so the existing city, collectibles and sites never move.
    /// </summary>
    public sealed partial class CityLayout
    {
        // ================================================================== region

        /// <summary>East edge of the Frostline (the Canyon at x 300 and the Crystal Manor at x 470 stay desert).</summary>
        public const float FrostEastX = 120f;
        /// <summary>South edge of the Frostline east of x -900; it curves down toward the west wall from there.</summary>
        public const float FrostSouthZ = 1080f;
        /// <summary>How far the south edge drops along the west side of the zone (to about z 970 at the wall).</summary>
        public const float FrostWestDrop = 110f;
        /// <summary>Largest offset the snow noise adds to FrostDepth, in metres.</summary>
        public const float SnowNoiseMax = 36f;
        /// <summary>Width (in snow-field metres) of the frost-dusted band between bare sand and deep snow.</summary>
        public const float SnowThinBand = 10f;

        public static readonly Vector3 FrostLakeCenter = new Vector3(-760f, 0f, 1330f);
        public const float FrostLakeRadius = 70f;
        public static readonly Vector3 WardenOutpostCenter = new Vector3(-330f, 0f, 1185f);
        public const float WardenOutpostRadius = 48f;
        public static readonly Vector3 TitanHollowCenter = new Vector3(-1050f, 0f, 1255f);
        public const float TitanHollowRadius = 58f;
        public const string WardenOutpostPoiName = "Frost Wardens Outpost";

        /// <summary>
        /// Signed distance in metres into the Frostline (positive inside, negative outside), without the noisy snow edge.
        /// The zone is everything west of x 120 and north of the south edge (z 1080, curving down to about z 970 at the
        /// west wall), well clear of the beltway, the quarry, the Canyon and the Crystal Manor.
        /// </summary>
        public static float FrostDepth(float x, float z)
        {
            float t = Mathf.Clamp01((-900f - x) / 650f);
            float southEdge = FrostSouthZ - FrostWestDrop * t * t * (3f - 2f * t);
            return Mathf.Min(z - southEdge, FrostEastX - x);
        }

        /// <summary>True inside the Frostline district (the noise-free region used for gameplay).</summary>
        public static bool IsFrostline(float x, float z)
        {
            return FrostDepth(x, z) > 0f;
        }

        /// <summary>How deep into the cold a point is, 0..1: zero just outside the edge, one 150 m in (mood, fog, snowfall, grade).</summary>
        public static float FrostAmount(float x, float z)
        {
            float t = Mathf.Clamp01((FrostDepth(x, z) + 25f) / 175f);
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// The snow field that shapes the ground: FrostDepth plus three octaves of value noise (at most +-36 m), so the edge
        /// wanders, throws drift islands into the sand and leaves bare patches near the edge. Below 0 is sand, 0 up to
        /// SnowThinBand is frost-dusted ground, above that deep snow.
        /// </summary>
        public static float SnowCover(float x, float z)
        {
            float d = FrostDepth(x, z);
            // Far from the edge the noise cannot change the band, so skip it (keeps per-frame grip queries cheap).
            if (d < -SnowNoiseMax - 1f || d > SnowNoiseMax + SnowThinBand + 1f) return d;
            return d + SnowNoise(x, z);
        }

        /// <summary>True where the ground is snow (deep or frost-dusted).</summary>
        public static bool IsSnowGround(float x, float z)
        {
            return SnowCover(x, z) >= 0f;
        }

        /// <summary>The wilderness districts: no pedestrians or city ambience, sparse or no traffic, off-road grip.</summary>
        public static bool IsWilderness(District d)
        {
            return d == District.QuartzBadlands || d == District.Frostline;
        }

        private static float SnowNoise(float x, float z)
        {
            return (ValueNoise(x / 85f, z / 85f, 9131) - 0.5f) * 44f
                 + (ValueNoise(x / 31f, z / 31f, 1187) - 0.5f) * 20f
                 + (ValueNoise(x / 11f, z / 11f, 4271) - 0.5f) * 8f;
        }

        /// <summary>Smooth value noise in [0,1] on an integer lattice (pure C#, no engine calls).</summary>
        private static float ValueNoise(float x, float z, int seed)
        {
            int x0 = Mathf.FloorToInt(x);
            int z0 = Mathf.FloorToInt(z);
            float fx = x - x0, fz = z - z0;
            fx = fx * fx * (3f - 2f * fx);
            fz = fz * fz * (3f - 2f * fz);
            float a = LatticeValue(x0, z0, seed);
            float b = LatticeValue(x0 + 1, z0, seed);
            float c = LatticeValue(x0, z0 + 1, seed);
            float d = LatticeValue(x0 + 1, z0 + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fz);
        }

        private static float LatticeValue(int x, int z, int seed)
        {
            unchecked
            {
                return MathUtil.Hash01(x * 7919 + seed * 104729, z * 6271 - seed * 15485863);
            }
        }

        /// <summary>Shoreline radius of Rimeglass Lake in the direction of a world angle (radians): a gently irregular disc.</summary>
        public static float FrostLakeShore(float angle)
        {
            return FrostLakeRadius * (1f + 0.07f * Mathf.Sin(angle * 3f + 0.8f) + 0.045f * Mathf.Sin(angle * 7f + 2.1f) + 0.025f * Mathf.Sin(angle * 13f + 0.3f));
        }

        /// <summary>True on the frozen surface of Rimeglass Lake (slick for tyres and boots).</summary>
        public static bool IsOnFrozenLake(float x, float z)
        {
            float dx = x - FrostLakeCenter.x, dz = z - FrostLakeCenter.z;
            float d2 = dx * dx + dz * dz;
            float max = FrostLakeRadius * 1.16f;
            if (d2 > max * max) return false;
            return Mathf.Sqrt(d2) < FrostLakeShore(Mathf.Atan2(dz, dx));
        }

        /// <summary>
        /// Snow on a mountain by where it stands: a full cap along the north and west edges around the Frostline, fading to
        /// a light dusting toward the north-east and south-west corners, bare rock on the south and east edges.
        /// </summary>
        public static float MountainSnow(Vector3 p)
        {
            float ang = Mathf.Atan2(p.z, p.x) * Mathf.Rad2Deg; // 0 east, 90 north, 180 west
            if (ang < -90f) ang += 360f;
            if (ang < 45f || ang > 225f) return 0f;
            if (ang < 80f) return Mathf.Lerp(0.3f, 1f, (ang - 45f) / 35f);
            if (ang > 165f) return Mathf.Lerp(1f, 0.3f, (ang - 165f) / 60f);
            return 1f;
        }

        // ================================================================== content lists

        /// <summary>Desert flora points that fell inside the snow: drawn as snow-dusted rocks instead of cacti and rocks.</summary>
        public readonly List<Vector3> FrostFloraSpots = new List<Vector3>();
        public readonly List<Vector3> PineSpots = new List<Vector3>();
        public readonly List<Vector3> SnowRockSpots = new List<Vector3>();
        public readonly List<Vector3> IceCrystalSpots = new List<Vector3>();
        /// <summary>Low wind-blown snow mounds, mostly along the edge of the snow.</summary>
        public readonly List<Vector3> SnowDriftSpots = new List<Vector3>();
        public readonly List<IcePillarSpec> IcePillars = new List<IcePillarSpec>();
        /// <summary>Fire braziers of the Frost Wardens outpost (gameplay can use them as warmth points).</summary>
        public readonly List<Vector3> BrazierSpots = new List<Vector3>();
        /// <summary>The 64 mountains around the map edge (built by CityBuilder, used here to keep new content out of them).</summary>
        public readonly List<MountainSpec> Mountains = new List<MountainSpec>();
        public readonly List<MesaSpec> Mesas = new List<MesaSpec>();
        /// <summary>The Frost Wardens outpost lot (also in DesertLots): footprints of the longhouse, tower and huts.</summary>
        public Lot WardenOutpost { get; private set; }

        // ================================================================== generation

        /// <summary>Runs last in Generate: ridge specs, the three Frostline sites, the outpost POI and the snow flora.</summary>
        private void BuildFrostline()
        {
            BuildRidge();
            AddSite("FrostLake", "Rimeglass Lake", FrostLakeCenter, FrostLakeRadius);
            AddSite("WardenOutpost", WardenOutpostPoiName, WardenOutpostCenter, WardenOutpostRadius);
            AddSite("TitanHollow", "Titan Hollow", TitanHollowCenter, TitanHollowRadius);
            BuildWardenOutpost();
            BuildTitanHollow();
            FrostFloraSpots.RemoveAll(p => NearFrostSite(p, 6f) || IsOnFrozenLake(p.x, p.z));
            GRand r = new GRand(Seed * 29 + 11);
            ScatterPines(r);
            ScatterSnowRocks(r);
            ScatterIceCrystals(r);
            ScatterDrifts(r);
        }

        /// <summary>
        /// The mountains and mesas exactly as CityBuilder has always drawn them: same generator, same calls in the same
        /// order (the third mesa size component is still drawn so the sequence matches).
        /// </summary>
        private void BuildRidge()
        {
            Mountains.Clear();
            Mesas.Clear();
            GRand r = new GRand(Seed * 3 + 99);
            float H = MapHalf;
            for (int i = 0; i < 64; i++)
            {
                float a = i / 64f * Mathf.PI * 2f + r.Range(-0.03f, 0.03f);
                float dist = H + r.Range(120f, 380f);
                Vector3 p = new Vector3(Mathf.Cos(a) * dist, 0f, Mathf.Sin(a) * dist);
                // Push to the square boundary so mountains hug the walls.
                float m = Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.z));
                p *= (H + r.Range(80f, 300f)) / m;
                Vector3 radii = new Vector3(r.Range(160f, 320f), r.Range(90f, 260f), r.Range(140f, 280f));
                float yaw = r.Range(0f, 360f);
                Mountains.Add(new MountainSpec { Position = p, Radii = radii, Yaw = yaw, Snow = MountainSnow(p) });
            }
            for (int i = 0; i < 6; i++)
            {
                Vector3 p = new Vector3(r.Range(-H + 150f, H - 150f), 0f, r.Range(-H + 150f, H - 150f));
                if (Mathf.Abs(p.x) < RingHalf + 120f && Mathf.Abs(p.z) < RingHalf + 120f) continue;
                if (NearAnyRoad(p, 60f)) continue;
                Vector3 size = new Vector3(r.Range(40f, 90f), r.Range(18f, 40f), r.Range(40f, 90f));
                Mesas.Add(new MesaSpec { Position = p, Radius = size.x * 0.5f, TopRadius = size.x * 0.42f, Height = size.y });
            }
        }

        /// <summary>True inside (or within margin of) a mountain's ground footprint or a mesa.</summary>
        public bool InsideRidge(Vector3 p, float margin)
        {
            for (int i = 0; i < Mountains.Count; i++)
            {
                MountainSpec m = Mountains[i];
                float dx = p.x - m.Position.x, dz = p.z - m.Position.z;
                float rx = m.Radii.x + margin, rz = m.Radii.z + margin;
                if (dx * dx + dz * dz > Mathf.Max(rx, rz) * Mathf.Max(rx, rz)) continue;
                // Undo the yaw: Unity's yaw turns +Z toward +X, so local = (dx cos - dz sin, dx sin + dz cos).
                float yr = m.Yaw * Mathf.Deg2Rad;
                float c = Mathf.Cos(yr), s = Mathf.Sin(yr);
                float lx = dx * c - dz * s;
                float lz = dx * s + dz * c;
                if ((lx * lx) / (rx * rx) + (lz * lz) / (rz * rz) < 1f) return true;
            }
            for (int i = 0; i < Mesas.Count; i++)
            {
                MesaSpec m = Mesas[i];
                float r = m.Radius + margin;
                if (MathUtil.FlatSqrDistance(p, m.Position) < r * r) return true;
            }
            return false;
        }

        private static bool NearFrostSite(Vector3 p, float margin)
        {
            if (MathUtil.FlatDistance(p, FrostLakeCenter) < FrostLakeRadius * 1.16f + margin) return true;
            if (MathUtil.FlatDistance(p, WardenOutpostCenter) < WardenOutpostRadius + 12f + margin) return true;
            if (MathUtil.FlatDistance(p, TitanHollowCenter) < TitanHollowRadius + margin) return true;
            return false;
        }

        private bool NearCollectible(Vector3 p, float margin)
        {
            float m2 = margin * margin;
            foreach (Vector3 q in DataChipSpots) if (MathUtil.FlatSqrDistance(p, q) < m2) return true;
            foreach (Vector3 q in CrystalShardSpots) if (MathUtil.FlatSqrDistance(p, q) < m2) return true;
            foreach (Vector3 q in SpellStoneSpots) if (MathUtil.FlatSqrDistance(p, q) < m2) return true;
            foreach (Vector3 q in CreatureSpots) if (MathUtil.FlatSqrDistance(p, q) < m2) return true;
            foreach (StuntJumpDef j in StuntJumps) if (MathUtil.FlatSqrDistance(p, j.Position) < (margin + 30f) * (margin + 30f)) return true;
            return false;
        }

        /// <summary>A spot for new snow content: deep enough in the snow and clear of sites, roads, lots, the ridge and collectibles.</summary>
        private bool FrostSpotFree(Vector3 p, float minSnow, float siteMargin)
        {
            if (p.x < -MapHalf + 12f || p.x > MapHalf - 12f || p.z > MapHalf - 12f) return false;
            if (SnowCover(p.x, p.z) < minSnow) return false;
            if (NearFrostSite(p, siteMargin)) return false;
            if (NearAnyRoad(p, 12f) || NearDesertLot(p, 8f)) return false;
            if (InsideRidge(p, 4f)) return false;
            if (NearCollectible(p, 6f)) return false;
            return true;
        }

        private static Vector3 RandomFrostPoint(GRand r)
        {
            return new Vector3(r.Range(-MapHalf + 20f, FrostEastX + 30f), 0f, r.Range(FrostSouthZ - FrostWestDrop - 20f, MapHalf - 15f));
        }

        private void BuildWardenOutpost()
        {
            Vector3 c = WardenOutpostCenter;
            Lot lot = new Lot { Area = new RectF(c.x - 46f, c.z - 40f, c.x + 46f, c.z + 40f), Use = LotUse.Special, PoiName = WardenOutpostPoiName, Seed = 61, CustomBuilt = true };
            // Local +z is north; the gate in the south palisade faces the beltway and the Fuel+ station.
            lot.Buildings.Add(OutpostBuilding(c, 0f, 16f, 26f, 11f, 6.5f, 62));    // longhouse
            lot.Buildings.Add(OutpostBuilding(c, -27f, -21f, 6f, 6f, 14f, 63));    // watchtower by the gate
            lot.Buildings.Add(OutpostBuilding(c, 24f, 2f, 9f, 7f, 4.5f, 64));      // bunk hut
            lot.Buildings.Add(OutpostBuilding(c, -24f, 6f, 8f, 7f, 4.5f, 65));     // storehouse
            lot.Entrance = new Vector3(c.x, 0f, c.z - 44f);
            lot.Facing = Vector3.back;
            DesertLots.Add(lot);
            WardenOutpost = lot;
            BrazierSpots.Add(new Vector3(c.x - 5.5f, 0f, c.z - 37.5f));
            BrazierSpots.Add(new Vector3(c.x + 5.5f, 0f, c.z - 37.5f));
            BrazierSpots.Add(new Vector3(c.x - 7f, 0f, c.z + 7.5f));
            BrazierSpots.Add(new Vector3(c.x + 7f, 0f, c.z + 7.5f));
            AddPoi(PoiType.None, lot.Entrance, lot.Facing, WardenOutpostPoiName, District.Frostline, -1);
        }

        private static Building OutpostBuilding(Vector3 c, float lx, float lz, float w, float d, float h, int seed)
        {
            RectF fp = new RectF(c.x + lx - w * 0.5f, c.z + lz - d * 0.5f, c.x + lx + w * 0.5f, c.z + lz + d * 0.5f);
            return new Building { Footprint = fp, Height = h, Style = FacadeStyle.Warehouse, Roof = RoofType.Gable, Seed = seed };
        }

        private void BuildTitanHollow()
        {
            Vector3 c = TitanHollowCenter;
            float ring = TitanHollowRadius * 0.82f;
            const float entry = 350f; // the opening faces east, toward the lake and the outpost
            const int count = 16;
            for (int i = 0; i < count; i++)
            {
                float a = i * 360f / count + (MathUtil.Hash01(i, 771) - 0.5f) * 8f;
                if (Mathf.Abs(Mathf.DeltaAngle(a, entry)) < 24f) continue;
                float rad = a * Mathf.Deg2Rad;
                float rr = ring + (MathUtil.Hash01(i, 772) - 0.5f) * 6f;
                Vector3 p = c + new Vector3(Mathf.Cos(rad) * rr, 0f, Mathf.Sin(rad) * rr);
                IcePillars.Add(new IcePillarSpec
                {
                    Position = p,
                    Radius = 1.5f + MathUtil.Hash01(i, 773) * 1.2f,
                    Height = 9f + MathUtil.Hash01(i, 774) * 10f,
                    Yaw = MathUtil.Hash01(i, 775) * 60f,
                    Lean = 4f + MathUtil.Hash01(i, 776) * 7f
                });
            }
        }

        private void ScatterPines(GRand r)
        {
            SpotGrid grid = new SpotGrid(8f);
            // Forest stands: tight groups of pines deep in the snow.
            int stands = 0;
            for (int attempt = 0; attempt < 600 && stands < 46; attempt++)
            {
                Vector3 c = RandomFrostPoint(r);
                if (!FrostSpotFree(c, 24f, 22f)) continue;
                stands++;
                int want = r.Range(6, 15);
                float radius = r.Range(12f, 26f);
                int placed = 0;
                for (int k = 0; k < want * 3 && placed < want; k++)
                {
                    Vector2 o = r.InsideUnitCircle() * radius;
                    Vector3 p = new Vector3(c.x + o.x, 0f, c.z + o.y);
                    if (!FrostSpotFree(p, 14f, 6f) || grid.TooClose(p, 3.4f)) continue;
                    grid.Add(p);
                    PineSpots.Add(p);
                    placed++;
                }
            }
            // Lone pines scattered between the stands.
            int lone = 0;
            for (int attempt = 0; attempt < 500 && lone < 90; attempt++)
            {
                Vector3 p = RandomFrostPoint(r);
                if (!FrostSpotFree(p, 14f, 8f) || grid.TooClose(p, 6f)) continue;
                grid.Add(p);
                PineSpots.Add(p);
                lone++;
            }
        }

        private void ScatterSnowRocks(GRand r)
        {
            int placed = 0;
            for (int attempt = 0; attempt < 400 && placed < 60; attempt++)
            {
                Vector3 p = RandomFrostPoint(r);
                if (!FrostSpotFree(p, 4f, 6f) || TooClose(SnowRockSpots, p, 18f) || TooClose(PineSpots, p, 4f)) continue;
                SnowRockSpots.Add(p);
                placed++;
            }
        }

        private void ScatterIceCrystals(GRand r)
        {
            // A ring of clusters along the lake shore.
            for (int i = 0; i < 10; i++)
            {
                float a = (i / 10f + r.Range(-0.03f, 0.03f)) * Mathf.PI * 2f;
                float rr = FrostLakeShore(a) + r.Range(4f, 9f);
                Vector3 p = FrostLakeCenter + new Vector3(Mathf.Cos(a) * rr, 0f, Mathf.Sin(a) * rr);
                if (InsideRidge(p, 2f) || NearCollectible(p, 5f)) continue;
                IceCrystalSpots.Add(p);
            }
            // Shards around the Titan Hollow ring.
            for (int i = 0; i < 12; i++)
            {
                float a = r.Range(0f, Mathf.PI * 2f);
                float rr = TitanHollowRadius * r.Range(0.95f, 1.25f);
                Vector3 p = TitanHollowCenter + new Vector3(Mathf.Cos(a) * rr, 0f, Mathf.Sin(a) * rr);
                if (InsideRidge(p, 2f) || NearCollectible(p, 5f)) continue;
                IceCrystalSpots.Add(p);
            }
            // Clusters out in the open snow.
            int placed = 0;
            for (int attempt = 0; attempt < 400 && placed < 40; attempt++)
            {
                Vector3 p = RandomFrostPoint(r);
                if (!FrostSpotFree(p, 16f, 8f) || TooClose(IceCrystalSpots, p, 30f) || TooClose(PineSpots, p, 4f)) continue;
                IceCrystalSpots.Add(p);
                placed++;
            }
        }

        private void ScatterDrifts(GRand r)
        {
            // Mostly along the wandering snow edge, where wind piles it up, plus a few out in the deep snow.
            int placed = 0;
            for (int attempt = 0; attempt < 2500 && placed < 150; attempt++)
            {
                Vector3 p = RandomFrostPoint(r);
                float s = SnowCover(p.x, p.z);
                bool edge = s > 2f && s < 34f;
                if (!edge && !(s >= 34f && r.Chance(0.12f))) continue;
                if (NearFrostSite(p, 4f) || NearAnyRoad(p, 10f) || NearDesertLot(p, 6f) || InsideRidge(p, 2f) || NearCollectible(p, 4f)) continue;
                if (TooClose(SnowDriftSpots, p, 12f) || TooClose(PineSpots, p, 3f)) continue;
                SnowDriftSpots.Add(p);
                placed++;
            }
        }

        /// <summary>Coarse spatial hash for minimum-spacing checks while scattering.</summary>
        private sealed class SpotGrid
        {
            private readonly float _cell;
            private readonly Dictionary<long, List<Vector3>> _cells = new Dictionary<long, List<Vector3>>();

            public SpotGrid(float cell)
            {
                _cell = cell;
            }

            private long KeyOf(int cx, int cz)
            {
                return ((long)cx << 32) ^ (uint)cz;
            }

            public void Add(Vector3 p)
            {
                long k = KeyOf(Mathf.FloorToInt(p.x / _cell), Mathf.FloorToInt(p.z / _cell));
                List<Vector3> l;
                if (!_cells.TryGetValue(k, out l))
                {
                    l = new List<Vector3>();
                    _cells[k] = l;
                }
                l.Add(p);
            }

            public bool TooClose(Vector3 p, float min)
            {
                int cx = Mathf.FloorToInt(p.x / _cell), cz = Mathf.FloorToInt(p.z / _cell);
                int r = Mathf.CeilToInt(min / _cell);
                float m2 = min * min;
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dz = -r; dz <= r; dz++)
                    {
                        List<Vector3> l;
                        if (!_cells.TryGetValue(KeyOf(cx + dx, cz + dz), out l)) continue;
                        for (int i = 0; i < l.Count; i++) if (MathUtil.FlatSqrDistance(l[i], p) < m2) return true;
                    }
                }
                return false;
            }
        }
    }

    /// <summary>
    /// Pure helper that turns a ground tile into bare sand, frost-dusted and deep-snow triangles along the snow field
    /// (CityLayout.SnowCover). Every grid cell is split into two triangles; inside a triangle the field is linear, so
    /// clipping it by the two band thresholds gives exact, watertight band polygons whose edges wander with the noise.
    /// Uniform cells are merged into row runs to keep the vertex count down.
    /// </summary>
    public static class SnowMesher
    {
        /// <summary>Grid spacing in metres for tiles the snow edge passes through.</summary>
        public const float Step = 4f;

        /// <summary>Band of a snow-field value: 0 sand, 1 frost-dusted, 2 deep snow.</summary>
        public static int BandOf(float field)
        {
            if (field < 0f) return 0;
            return field < CityLayout.SnowThinBand ? 1 : 2;
        }

        /// <summary>
        /// True when any part of the tile could carry snow. Depth grows toward the west and north, so its largest value on
        /// a tile is at the north-west corner; adding the noise ceiling gives a safe upper bound.
        /// </summary>
        public static bool MayHaveSnow(float x0, float z0, float x1, float z1)
        {
            return CityLayout.FrostDepth(x0, z1) + CityLayout.SnowNoiseMax + 1f >= 0f;
        }

        /// <summary>
        /// Emits the tile [x0,x1] x [z0,z1] at height y into three builders (sand, frost-dusted, deep snow), with world UVs
        /// (metres / tile). Returns false (and emits nothing) when the whole tile is bare sand, so callers can keep their
        /// usual sand quad.
        /// </summary>
        public static bool Build(float x0, float z0, float x1, float z1, float y, MeshBuilder sand, float sandTile, MeshBuilder thin, float thinTile, MeshBuilder deep, float deepTile)
        {
            int nx = Mathf.Max(1, Mathf.CeilToInt((x1 - x0) / Step - 0.001f));
            int nz = Mathf.Max(1, Mathf.CeilToInt((z1 - z0) / Step - 0.001f));
            float sx = (x1 - x0) / nx, sz = (z1 - z0) / nz;
            float[,] f = new float[nx + 1, nz + 1];
            int bandMin = 2, bandMax = 0;
            for (int i = 0; i <= nx; i++)
            {
                float x = i == nx ? x1 : x0 + i * sx;
                for (int j = 0; j <= nz; j++)
                {
                    float z = j == nz ? z1 : z0 + j * sz;
                    float v = CityLayout.SnowCover(x, z);
                    f[i, j] = v;
                    int b = BandOf(v);
                    if (b < bandMin) bandMin = b;
                    if (b > bandMax) bandMax = b;
                }
            }
            if (bandMax == 0) return false;
            MeshBuilder[] builders = { sand, thin, deep };
            float[] tiles = { sandTile, thinTile, deepTile };
            if (bandMin == bandMax)
            {
                Rect(builders[bandMin], tiles[bandMin], x0, z0, x1, z1, y);
                return true;
            }
            for (int j = 0; j < nz; j++)
            {
                float za = j == 0 ? z0 : z0 + j * sz;
                float zb = j + 1 == nz ? z1 : z0 + (j + 1) * sz;
                int runStart = -1, runBand = -1;
                for (int i = 0; i <= nx; i++)
                {
                    int cellBand = -1;
                    if (i < nx)
                    {
                        int b00 = BandOf(f[i, j]), b10 = BandOf(f[i + 1, j]), b01 = BandOf(f[i, j + 1]), b11 = BandOf(f[i + 1, j + 1]);
                        if (b00 == b10 && b00 == b01 && b00 == b11) cellBand = b00;
                    }
                    // Close the current run when the band changes or a mixed cell interrupts it.
                    if (runStart >= 0 && cellBand != runBand)
                    {
                        float rx0 = runStart == 0 ? x0 : x0 + runStart * sx;
                        float rx1 = i == nx ? x1 : x0 + i * sx;
                        Rect(builders[runBand], tiles[runBand], rx0, za, rx1, zb, y);
                        runStart = -1;
                        runBand = -1;
                    }
                    if (i == nx) break;
                    if (cellBand >= 0)
                    {
                        if (runStart < 0)
                        {
                            runStart = i;
                            runBand = cellBand;
                        }
                        continue;
                    }
                    float xa = i == 0 ? x0 : x0 + i * sx;
                    float xb = i + 1 == nx ? x1 : x0 + (i + 1) * sx;
                    Vector3 v00 = new Vector3(xa, f[i, j], za), v01 = new Vector3(xa, f[i, j + 1], zb);
                    Vector3 v11 = new Vector3(xb, f[i + 1, j + 1], zb), v10 = new Vector3(xb, f[i + 1, j], za);
                    // Both triangles are clockwise seen from above (the y component carries the field value here).
                    ClipTriangle(v00, v01, v11, y, builders, tiles);
                    ClipTriangle(v00, v11, v10, y, builders, tiles);
                }
            }
            return true;
        }

        private static void Rect(MeshBuilder mb, float tile, float x0, float z0, float x1, float z1, float y)
        {
            Vector3 a = new Vector3(x0, y, z0), b = new Vector3(x0, y, z1), c = new Vector3(x1, y, z1), d = new Vector3(x1, y, z0);
            mb.Quad(0, a, b, c, d, Vector3.up, UV(a, tile), UV(b, tile), UV(c, tile), UV(d, tile));
        }

        private static Vector2 UV(Vector3 p, float tile)
        {
            return new Vector2(p.x / tile, p.z / tile);
        }

        private static readonly List<Vector3> _polyA = new List<Vector3>(8);
        private static readonly List<Vector3> _polyB = new List<Vector3>(8);

        /// <summary>Splits one triangle (field value in y) into its band pieces and emits each as a fan.</summary>
        private static void ClipTriangle(Vector3 a, Vector3 b, Vector3 c, float y, MeshBuilder[] builders, float[] tiles)
        {
            for (int band = 0; band < 3; band++)
            {
                _polyA.Clear();
                _polyA.Add(a);
                _polyA.Add(b);
                _polyA.Add(c);
                if (band > 0)
                {
                    Clip(_polyA, _polyB, band == 1 ? 0f : CityLayout.SnowThinBand, true);
                    Swap();
                }
                if (band < 2)
                {
                    Clip(_polyA, _polyB, band == 0 ? 0f : CityLayout.SnowThinBand, false);
                    Swap();
                }
                if (_polyA.Count < 3) continue;
                MeshBuilder mb = builders[band];
                float tile = tiles[band];
                Vector3 p0 = Flat(_polyA[0], y);
                for (int k = 1; k + 1 < _polyA.Count; k++)
                {
                    Vector3 p1 = Flat(_polyA[k], y), p2 = Flat(_polyA[k + 1], y);
                    float cross = (p1.x - p0.x) * (p2.z - p0.z) - (p1.z - p0.z) * (p2.x - p0.x);
                    if (cross > -1e-6f) continue; // sliver (clockwise from above has a negative x/z cross)
                    mb.Triangle(0, p0, p1, p2, Vector3.up, UV(p0, tile), UV(p1, tile), UV(p2, tile));
                }
            }
        }

        private static void Swap()
        {
            _polyA.Clear();
            _polyA.AddRange(_polyB);
        }

        private static Vector3 Flat(Vector3 v, float y)
        {
            return new Vector3(v.x, y, v.z);
        }

        /// <summary>Sutherland-Hodgman clip of a convex polygon by field &gt;= t (keepAbove) or field &lt; t.</summary>
        private static void Clip(List<Vector3> input, List<Vector3> output, float t, bool keepAbove)
        {
            output.Clear();
            int n = input.Count;
            for (int k = 0; k < n; k++)
            {
                Vector3 p = input[k], q = input[(k + 1) % n];
                bool pin = keepAbove ? p.y >= t : p.y < t;
                bool qin = keepAbove ? q.y >= t : q.y < t;
                if (pin) output.Add(p);
                if (pin != qin) output.Add(Cross(p, q, t));
            }
        }

        /// <summary>Where the field crosses t on segment pq, computed in a fixed vertex order so neighbouring triangles agree exactly.</summary>
        private static Vector3 Cross(Vector3 p, Vector3 q, float t)
        {
            if (q.x < p.x || (q.x == p.x && q.z < p.z))
            {
                Vector3 tmp = p;
                p = q;
                q = tmp;
            }
            float s = (t - p.y) / (q.y - p.y);
            return new Vector3(p.x + (q.x - p.x) * s, t, p.z + (q.z - p.z) * s);
        }
    }
}
