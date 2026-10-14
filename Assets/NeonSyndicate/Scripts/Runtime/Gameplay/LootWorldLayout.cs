using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The three kinds of loot chest in the world.</summary>
    public enum LootChestKind
    {
        /// <summary>A wood-and-glass pod anyone can open.</summary>
        Common = 0,
        /// <summary>Needs a Chest Key, guarded by a crew, better odds.</summary>
        Sealed = 1,
        /// <summary>Only exists inside the Inner World. Epic or better, a pack wakes when it opens.</summary>
        Vault = 2
    }

    /// <summary>Which part of the map a chest stands in (decides its materials and guards).</summary>
    public enum LootRegion
    {
        City = 0,
        Badlands = 1,
        Frostline = 2
    }

    /// <summary>Where a hidden Satchel is tucked away.</summary>
    public enum LootCacheKind
    {
        /// <summary>On a flat roof.</summary>
        Rooftop = 0,
        /// <summary>On the setback ledge of a stepped tower.</summary>
        Ledge = 1,
        /// <summary>In the canal, under a bridge deck.</summary>
        Bridge = 2
    }

    /// <summary>One chest spot: a stable index (saves remember it), a map point and its kind.</summary>
    public sealed class LootChestSite
    {
        public int Index;
        public Vector3 Pos;
        public float Yaw;
        public LootChestKind Kind;
        public LootRegion Region;
        public string Place = "";
    }

    /// <summary>One hidden Satchel spot: a stable index, the exact point (rooftops keep their height) and its kind.</summary>
    public sealed class LootCacheSite
    {
        public int Index;
        public Vector3 Pos;
        public float Yaw;
        public LootCacheKind Kind;
        public string Place = "";
    }

    /// <summary>
    /// Where the loot chests and hidden Satchels are, worked out from the city layout and a seed alone (no engine calls),
    /// so the game, the EditMode tests and the offline tools get exactly the same spots. Chests stand on open ground in
    /// parks, empty lots and plazas, in the Badlands sand and the Frostline snow, never on roads, sidewalks, in
    /// buildings, on the ridge, inside a boss arena or a named site. Satchels sit on flat roofs, on the ledges of stepped
    /// towers and under the canal bridges: places the travel powers reach. Changing this placement moves every saved
    /// chest, so treat a shipped layout as frozen.
    /// </summary>
    public static class LootWorldLayout
    {
        public const int ChestCount = 140;
        public const int CacheCount = 40;
        public const int VaultCount = 12;
        /// <summary>Common chests per region (City, Badlands, Frostline).</summary>
        public static readonly int[] CommonQuota = { 38, 36, 26 };
        /// <summary>Sealed chests per region (City, Badlands, Frostline).</summary>
        public static readonly int[] SealedQuota = { 8, 12, 8 };
        public const int RooftopCount = 24;
        public const int LedgeCount = 8;
        public const int BridgeCount = 8;

        /// <summary>Closest two city chests may stand (metres).</summary>
        public const float CitySpacing = 42f;
        /// <summary>Closest two chests out in the wild may stand (metres).</summary>
        public const float WildSpacing = 70f;
        /// <summary>Clear ground kept to the nearest road edge in the city (covers the sidewalk) and in the wild.</summary>
        public const float CityRoadClear = 6f;
        public const float WildRoadClear = 14f;

        // ================================================================== chests

        /// <summary>Every chest spot, in index order: city, Badlands, Frostline, then the Inner World vaults.</summary>
        public static List<LootChestSite> Chests(CityLayout L, int seed)
        {
            List<LootChestSite> all = new List<LootChestSite>();
            if (L == null) return all;
            List<Vector3> city = new List<Vector3>();
            CityCandidates(L, city);
            List<Vector3> bad = new List<Vector3>();
            WildCandidates(L, District.QuartzBadlands, bad);
            List<Vector3> frost = new List<Vector3>();
            WildCandidates(L, District.Frostline, frost);
            Shuffle(city, new GRand(seed * 31 + 1));
            city = Interleave(city);
            Shuffle(bad, new GRand(seed * 31 + 2));
            Shuffle(frost, new GRand(seed * 31 + 3));
            List<Vector3> taken = new List<Vector3>();
            Pick(L, city, LootRegion.City, CommonQuota[0], SealedQuota[0], CitySpacing, taken, all, seed);
            Pick(L, bad, LootRegion.Badlands, CommonQuota[1], SealedQuota[1], WildSpacing, taken, all, seed);
            Pick(L, frost, LootRegion.Frostline, CommonQuota[2], SealedQuota[2], WildSpacing, taken, all, seed);
            // Vaults: city ground again (the Inner World mirrors the city), spread wide and clear of the outer chests.
            List<Vector3> veil = new List<Vector3>(city);
            Shuffle(veil, new GRand(seed * 31 + 4));
            veil = Interleave(veil);
            GRand yaw = new GRand(seed * 31 + 5);
            List<Vector3> vaults = new List<Vector3>();
            foreach (Vector3 p in veil)
            {
                if (vaults.Count >= VaultCount) break;
                if (TooClose(p, vaults, 140f) || TooClose(p, taken, 25f)) continue;
                vaults.Add(p);
                all.Add(new LootChestSite { Index = all.Count, Pos = p, Yaw = yaw.Range(0f, 360f), Kind = LootChestKind.Vault, Region = LootRegion.City, Place = QuestPlaces.PlaceName(L, p) });
            }
            return all;
        }

        private static void Pick(CityLayout L, List<Vector3> cands, LootRegion region, int common, int sealedCount, float spacing, List<Vector3> taken, List<LootChestSite> into, int seed)
        {
            int total = common + sealedCount;
            GRand yaw = new GRand(seed * 131 + (int)region * 17 + 7);
            int n = 0;
            foreach (Vector3 p in cands)
            {
                if (n >= total) break;
                if (TooClose(p, taken, spacing)) continue;
                // Sealed chests are spread evenly through the picks.
                bool isSealed = (n + 1) * sealedCount / total > n * sealedCount / total;
                taken.Add(p);
                into.Add(new LootChestSite { Index = into.Count, Pos = p, Yaw = yaw.Range(0f, 360f), Kind = isSealed ? LootChestKind.Sealed : LootChestKind.Common, Region = region, Place = QuestPlaces.PlaceName(L, p) });
                n++;
            }
        }

        private static bool TooClose(Vector3 p, List<Vector3> others, float d)
        {
            float d2 = d * d;
            for (int i = 0; i < others.Count; i++)
            {
                float dx = others[i].x - p.x, dz = others[i].z - p.z;
                if (dx * dx + dz * dz < d2) return true;
            }
            return false;
        }

        /// <summary>
        /// Deals the points out district by district (one from each city district in turn, keeping each district's own
        /// order), so the picks spread over the whole city instead of piling up where the parks are.
        /// </summary>
        public static List<Vector3> Interleave(List<Vector3> points)
        {
            SortedDictionary<int, List<Vector3>> by = new SortedDictionary<int, List<Vector3>>();
            foreach (Vector3 p in points)
            {
                int d = (int)CityLayout.DistrictAt(p);
                List<Vector3> l;
                if (!by.TryGetValue(d, out l)) by[d] = l = new List<Vector3>();
                l.Add(p);
            }
            List<Vector3> o = new List<Vector3>(points.Count);
            for (int i = 0; o.Count < points.Count; i++)
                foreach (List<Vector3> l in by.Values)
                    if (i < l.Count) o.Add(l[i]);
            return o;
        }

        /// <summary>A stable Fisher-Yates shuffle.</summary>
        public static void Shuffle(List<Vector3> list, GRand r)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = r.Range(0, i + 1);
                Vector3 t = list[i];
                list[i] = list[j];
                list[j] = t;
            }
        }

        /// <summary>Open city ground for chests: inside parks, empty lots, courts and plazas, in a stable order.</summary>
        public static void CityCandidates(CityLayout L, List<Vector3> into)
        {
            into.Clear();
            foreach (Block b in L.Blocks)
            {
                if (CityLayout.IsWilderness(b.District)) continue;
                if (b.IsPark)
                {
                    RectF r = b.Inner.Inset(5f);
                    if (!r.Valid) continue;
                    for (float x = r.X0; x <= r.X1; x += 14f)
                        for (float z = r.Z0; z <= r.Z1; z += 14f)
                            AddCity(L, into, new Vector3(x, CityLayout.CurbHeight, z));
                    continue;
                }
                foreach (Lot lot in b.Lots)
                {
                    if (lot.Poi != PoiType.None || lot.CustomBuilt) continue;
                    bool open = (lot.Use == LotUse.Empty || lot.Use == LotUse.Plaza || lot.Use == LotUse.Court) && lot.Buildings.Count == 0;
                    if (!open)
                    {
                        // Back yards and alley corners of built lots, wherever the buildings leave room.
                        if (lot.Use == LotUse.Casino || lot.Use == LotUse.Special || lot.Use == LotUse.Cemetery || lot.Use == LotUse.Parking) continue;
                        RectF y = lot.Area.Inset(2.5f);
                        if (!y.Valid) continue;
                        AddCity(L, into, new Vector3(y.X0, CityLayout.CurbHeight, y.Z0));
                        AddCity(L, into, new Vector3(y.X1, CityLayout.CurbHeight, y.Z0));
                        AddCity(L, into, new Vector3(y.X0, CityLayout.CurbHeight, y.Z1));
                        AddCity(L, into, new Vector3(y.X1, CityLayout.CurbHeight, y.Z1));
                        continue;
                    }
                    RectF a = lot.Area.Inset(3f);
                    if (!a.Valid) continue;
                    // Plazas keep their centre for fountains and statues: chests go to the quarters.
                    if (lot.Use != LotUse.Plaza) AddCity(L, into, a.Center(CityLayout.CurbHeight));
                    float qx = a.Width * 0.25f, qz = a.Depth * 0.25f;
                    AddCity(L, into, new Vector3(a.CenterX - qx, CityLayout.CurbHeight, a.CenterZ - qz));
                    AddCity(L, into, new Vector3(a.CenterX + qx, CityLayout.CurbHeight, a.CenterZ + qz));
                }
            }
        }

        private static void AddCity(CityLayout L, List<Vector3> into, Vector3 p)
        {
            if (CityChestSpotOk(L, p)) into.Add(p);
        }

        /// <summary>True for a city point where a chest may stand.</summary>
        public static bool CityChestSpotOk(CityLayout L, Vector3 p)
        {
            District d = CityLayout.DistrictAt(p);
            if (CityLayout.IsWilderness(d)) return false;
            if (Mathf.Abs(p.x) > CityLayout.CoreHalf - 5f || Mathf.Abs(p.z) > CityLayout.CoreHalf - 5f) return false;
            if (Mathf.Abs(p.z - CityLayout.CanalZ) < CityLayout.CanalBandHalf + 3f) return false;
            // Cheapest and most selective tests first (the spot lists hold thousands of points).
            if (L.IsInsideBuilding(p, 3.5f) || L.NearAnyRoad(p, CityRoadClear)) return false;
            if (QuestPlaces.InBossArena(L, p, QuestPlaces.ArenaMargin)) return false;
            foreach (Poi poi in L.Pois) if (MathUtil.FlatDistance(poi.Position, p) < 8f) return false;
            if (Near(L.TreeSpots, p, 3f) || Near(L.PalmSpots, p, 3f) || Near(L.ParkingSpots, p, 4f) || Near(L.StreetLampSpots, p, 2f)) return false;
            return true;
        }

        /// <summary>Open wild ground in one wilderness district: beside rocks, cacti and pines first, then a coarse grid.</summary>
        public static void WildCandidates(CityLayout L, District want, List<Vector3> into)
        {
            into.Clear();
            if (want == District.QuartzBadlands)
            {
                foreach (Vector3 p in L.RockSpots) AddWild(L, into, p + new Vector3(-3.5f, 0f, 3f), want);
                foreach (Vector3 p in L.CactusSpots) AddWild(L, into, p + new Vector3(3f, 0f, 3f), want);
            }
            else
            {
                foreach (Vector3 p in L.SnowRockSpots) AddWild(L, into, p + new Vector3(4f, 0f, 3f), want);
                for (int i = 0; i < L.PineSpots.Count; i += 3) AddWild(L, into, L.PineSpots[i] + new Vector3(5f, 0f, -4f), want);
            }
            float h = CityLayout.MapHalf - 40f;
            for (float x = -h; x <= h; x += 40f)
                for (float z = -h; z <= h; z += 40f)
                    AddWild(L, into, new Vector3(x, 0f, z), want);
        }

        private static void AddWild(CityLayout L, List<Vector3> into, Vector3 p, District want)
        {
            if (WildChestSpotOk(L, p, want)) into.Add(new Vector3(p.x, 0f, p.z));
        }

        /// <summary>True for a Badlands or Frostline point where a chest may stand.</summary>
        public static bool WildChestSpotOk(CityLayout L, Vector3 p, District want)
        {
            // Cheap tests first: the road test walks every road edge.
            if (CityLayout.DistrictAt(p) != want) return false;
            if (Mathf.Abs(p.x) > CityLayout.MapHalf - 40f || Mathf.Abs(p.z) > CityLayout.MapHalf - 40f) return false;
            if (CityLayout.IsOnFrozenLake(p.x, p.z) || MathUtil.FlatDistance(p, CityLayout.FrostLakeCenter) < CityLayout.FrostLakeRadius * 1.25f) return false;
            foreach (SpecialSite s in L.Sites) if (MathUtil.FlatDistance(p, s.Position) < s.Radius + 12f) return false;
            foreach (Lot lot in L.DesertLots) if (lot.Area.Inset(-10f).Contains(p)) return false;
            foreach (StuntJumpDef j in L.StuntJumps) if (MathUtil.FlatDistance(p, j.Position) < 12f) return false;
            foreach (Poi poi in L.Pois) if (MathUtil.FlatDistance(poi.Position, p) < 20f) return false;
            if (QuestPlaces.InBossArena(L, p, QuestPlaces.ArenaMargin + 10f)) return false;
            if (L.InsideRidge(p, 6f) || L.IsInsideBuilding(p, 4f) || L.NearAnyRoad(p, WildRoadClear)) return false;
            return true;
        }

        private static bool Near(List<Vector3> spots, Vector3 p, float d)
        {
            float d2 = d * d;
            for (int i = 0; i < spots.Count; i++)
            {
                float dx = spots[i].x - p.x, dz = spots[i].z - p.z;
                if (dx * dx + dz * dz < d2) return true;
            }
            return false;
        }

        // ================================================================== hidden satchels

        /// <summary>Every Satchel spot, in index order: rooftops, ledges, then under the bridges.</summary>
        public static List<LootCacheSite> Caches(CityLayout L, int seed)
        {
            List<LootCacheSite> all = new List<LootCacheSite>();
            if (L == null) return all;
            List<Vector3> roofs = new List<Vector3>();
            List<Vector3> ledges = new List<Vector3>();
            RoofCandidates(L, roofs, ledges);
            Shuffle(roofs, new GRand(seed * 53 + 1));
            Shuffle(ledges, new GRand(seed * 53 + 2));
            List<Vector3> bridges = new List<Vector3>();
            BridgeCandidates(L, bridges);
            Shuffle(bridges, new GRand(seed * 53 + 3));
            List<Vector3> taken = new List<Vector3>();
            int ledgeTarget = Mathf.Min(LedgeCount, ledges.Count);
            int bridgeTarget = Mathf.Min(BridgeCount, bridges.Count);
            int roofTarget = CacheCount - ledgeTarget - bridgeTarget;
            PickCaches(L, roofs, LootCacheKind.Rooftop, roofTarget, 110f, taken, all, seed);
            PickCaches(L, ledges, LootCacheKind.Ledge, ledgeTarget, 90f, taken, all, seed);
            PickCaches(L, bridges, LootCacheKind.Bridge, bridgeTarget, 60f, taken, all, seed);
            return all;
        }

        private static void PickCaches(CityLayout L, List<Vector3> cands, LootCacheKind kind, int count, float spacing, List<Vector3> taken, List<LootCacheSite> into, int seed)
        {
            GRand yaw = new GRand(seed * 151 + (int)kind * 13 + 3);
            int n = 0;
            for (int pass = 0; pass < 3 && n < count; pass++)
            {
                // Relax the spacing when the city runs short of spots.
                float sp = spacing * (pass == 0 ? 1f : (pass == 1 ? 0.6f : 0.3f));
                foreach (Vector3 p in cands)
                {
                    if (n >= count) break;
                    if (TooClose(p, taken, sp)) continue;
                    taken.Add(p);
                    into.Add(new LootCacheSite { Index = into.Count, Pos = p, Yaw = yaw.Range(0f, 360f), Kind = kind, Place = QuestPlaces.PlaceName(L, p) });
                    n++;
                }
            }
        }

        /// <summary>
        /// Roof spots (flat single-tier roofs from 12 to 70 m up) and ledge spots (the first setback of a stepped tower).
        /// Heights and setbacks follow CityBuilder.BuildBuilding exactly (the same GRand draws on the building's seed).
        /// </summary>
        public static void RoofCandidates(CityLayout L, List<Vector3> roofs, List<Vector3> ledges)
        {
            roofs.Clear();
            ledges.Clear();
            foreach (Block b in L.Blocks)
            {
                if (CityLayout.IsWilderness(b.District)) continue;
                foreach (Lot lot in b.Lots)
                {
                    if (lot.CustomBuilt || lot.Poi != PoiType.None) continue;
                    foreach (Building bld in lot.Buildings)
                    {
                        RectF fp = bld.Footprint;
                        if (!fp.Valid) continue;
                        float minSide = Mathf.Min(fp.Width, fp.Depth);
                        int tiers = Mathf.Clamp(bld.Tiers, 1, 3);
                        if (tiers == 1)
                        {
                            if (bld.Roof != RoofType.Flat && bld.Roof != RoofType.FlatWithProps) continue;
                            if (bld.Height < 12f || bld.Height > 70f || minSide < 8f) continue;
                            // A quarter of the way to a corner: clear of the parapet and of a centred mast.
                            roofs.Add(new Vector3(fp.CenterX - fp.Width * 0.25f, CityLayout.CurbHeight + bld.Height, fp.CenterZ + fp.Depth * 0.25f));
                            continue;
                        }
                        Vector3 ledge;
                        if (LedgeSpot(bld, out ledge)) ledges.Add(ledge);
                    }
                }
            }
        }

        /// <summary>
        /// The middle of the south ledge on the first setback of a stepped tower, or false when the tower has no usable
        /// ledge. Mirrors CityBuilder.BuildBuilding's draws: tier 0 top = Height x Range(0.45, 0.65); tier 1 inset =
        /// min side x (0.1 + Range(0.02, 0.06)), with Range(0.75, 0.88) drawn first for three tiers.
        /// </summary>
        public static bool LedgeSpot(Building bld, out Vector3 spot)
        {
            spot = Vector3.zero;
            RectF fp = bld.Footprint;
            int tiers = Mathf.Clamp(bld.Tiers, 1, 3);
            if (tiers < 2 || !fp.Valid) return false;
            float minSide = Mathf.Min(fp.Width, fp.Depth);
            if (minSide < 14f) return false;
            GRand r = new GRand(bld.Seed);
            float top0 = bld.Height * r.Range(0.45f, 0.65f);
            if (tiers == 3) r.Range(0.75f, 0.88f);
            float inset1 = minSide * (0.1f + r.Range(0.02f, 0.06f));
            RectF t1 = fp.Inset(inset1);
            if (!t1.Valid || t1.Width < 3f || t1.Depth < 3f || inset1 * 0.5f < 0.8f) return false;
            if (top0 < 8f) return false;
            spot = new Vector3(fp.CenterX, CityLayout.CurbHeight + top0, fp.Z0 + inset1 * 0.5f);
            return true;
        }

        /// <summary>Spots on the canal bed under each bridge deck, just above the shallow water.</summary>
        public static void BridgeCandidates(CityLayout L, List<Vector3> into)
        {
            into.Clear();
            foreach (RoadEdge e in L.Edges)
            {
                if (!e.HasBridge) continue;
                float x = L.Nodes[e.A].Pos.x;
                // The canal bed runs between the two ramps (70 m in from each end at x = -900 and 900).
                if (Mathf.Abs(x) > 820f) continue;
                into.Add(new Vector3(x, BridgeSatchelY, CityLayout.CanalZ + 3f));
            }
        }

        /// <summary>Height of a Satchel under a bridge: on a footing just above the canal's shallow water.</summary>
        public const float BridgeSatchelY = -CityLayout.CanalDepth + 0.62f;
    }
}
