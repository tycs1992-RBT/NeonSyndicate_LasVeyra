using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Turns the place words in quest and event data into map positions, using only the city layout (no engine calls,
    /// so the validator, the tests and the offline tools get exactly the points the game uses). Heights are left at
    /// the layout's values; the runtime snaps them to the ground.
    /// </summary>
    public static class QuestPlaces
    {
        public const string DistrictPrefix = "District:";
        public const string SitePrefix = "Site:";

        /// <summary>
        /// Clear ground kept around every boss arena (the margin the creature ecology keeps too): contract hunting grounds,
        /// contract destinations and world events never sit closer to an arena's centre than its radius plus this.
        /// </summary>
        public const float ArenaMargin = 30f;

        private static CityLayout _arenaLayout;
        private static readonly List<ArenaZone> _arenas = new List<ArenaZone>();

        /// <summary>Resolves a 04E place token in the live world (an exact 3D point). False when it cannot resolve now.</summary>
        public delegate bool TokenResolve(string token, out Vector3 pos);

        /// <summary>
        /// The live resolver for 04E place tokens (Venue:, Under:, Cast:, Arsenal:), set by the quest book at boot. Null
        /// offline and in tests: tokens then do not resolve here (the validator checks them with CampaignRules04E).
        /// </summary>
        public static TokenResolve TokenResolver;

        /// <summary>Drops the cached boss arenas (rebuilt for the next layout asked about) and the live token resolver.</summary>
        public static void ResetStatics()
        {
            _arenaLayout = null;
            _arenas.Clear();
            TokenResolver = null;
        }

        private static bool ResolveToken(string at, out Vector3 pos)
        {
            pos = Vector3.zero;
            TokenResolve r = TokenResolver;
            if (r == null) return false;
            try { return r(at, out pos); }
            catch (System.Exception e)
            {
                Log.Exception("Quest place token " + at, e);
                return false;
            }
        }

        /// <summary>True when the word names a place: a token, a site key, a POI name or "District:Name".</summary>
        public static bool IsKnownPlace(CityLayout L, string at)
        {
            if (at == null) return false;
            if (at.StartsWith("Realm:")) return at == "Realm:hearth-lens" || at == "Realm:oath-seal" || at == "Realm:oath-witness" || at == "Realm:storm-crossing" || at == "Realm:storm-spring" || at == "Realm:storm-vein" || at == "Realm:oath-terrace" || at == "Realm:storm-cavern" || at == "Realm:storm-ledge" || at == "Realm:castellan" || at == "Realm:sovereign" || at == "Realm:gallery" || at == "Realm:storm-shrine" || at == "Realm:threshold" || at == "Realm:grotto" || at == "Realm:observation" || at == "Realm:forge-scar" || at == "Realm:azure";
            if (QuestCatalog.Contains(QuestCatalog.PlaceTokens, at)) return true;
            if (CampaignRules04E.IsPlaceToken(at)) return CampaignRules04E.PlaceTokenError(at) == null;
            District d;
            if (at.StartsWith(DistrictPrefix)) return TryDistrict(at.Substring(DistrictPrefix.Length), out d) && !CityLayout.IsWilderness(d);
            if (L == null) return true;
            return L.Site(at) != null || L.FindPoi(at) != null;
        }

        /// <summary>A POI name or site key (what givers and destinations must be).</summary>
        public static bool IsNamedPlace(CityLayout L, string at)
        {
            if (string.IsNullOrEmpty(at) || L == null) return false;
            return L.Site(at) != null || L.FindPoi(at) != null;
        }

        public static bool TryDistrict(string name, out District d)
        {
            d = District.LasVeyraCore;
            if (string.IsNullOrEmpty(name)) return false;
            foreach (District x in (District[])System.Enum.GetValues(typeof(District)))
            {
                if (x.ToString() == name)
                {
                    d = x;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Resolves At plus a world-axis offset. giver, previous and player stand in for the tokens of the same names
        /// (previous falls back to giver). salt picks the sidewalk for "District:Name" deterministically.
        /// </summary>
        public static bool Resolve(CityLayout L, string at, float ox, float oz, Vector3 giver, Vector3 previous, bool hasPrevious, Vector3 player, int salt, out Vector3 pos)
        {
            pos = Vector3.zero;
            Vector3 basePos;
            if (string.IsNullOrEmpty(at) || at == "Previous") basePos = hasPrevious ? previous : giver;
            else if (at == "Giver") basePos = giver;
            else if (at == "Player") basePos = player;
            else if (at == "World") basePos = Vector3.zero;
            else if (at.StartsWith("Realm:"))
            {
                if (L == null || !L.ResolveRealmAnchor04OC(at, out basePos)) return false;
            }
            else if (CampaignRules04E.IsPlaceToken(at))
            {
                // 04E tokens are exact live points (a room, an Underheart anchor, a cast post): resolved by the runtime only.
                if (!ResolveToken(at, out basePos)) return false;
            }
            else if (at.StartsWith(DistrictPrefix))
            {
                District d;
                if (L == null || !TryDistrict(at.Substring(DistrictPrefix.Length), out d)) return false;
                if (!WalkPointIn(L, d, salt, out basePos)) return false;
            }
            else
            {
                if (L == null) return false;
                SpecialSite s = L.Site(at);
                if (s != null) basePos = s.Position;
                else
                {
                    Poi p = L.FindPoi(at);
                    if (p == null) return false;
                    basePos = p.Position;
                }
            }
            pos = basePos + new Vector3(ox, 0f, oz);
            return true;
        }

        /// <summary>A sidewalk point in a district picked by salt (the same rule as MissionManager.WalkPointIn).</summary>
        public static bool WalkPointIn(CityLayout L, District d, int salt, out Vector3 pos)
        {
            pos = Vector3.zero;
            if (L == null) return false;
            int count = 0;
            for (int i = 0; i < L.WalkNodes.Count; i++) if (CityLayout.DistrictAt(L.WalkNodes[i].Pos) == d) count++;
            if (count == 0) return false;
            int pick = (int)(((long)salt * 7919L % count + count) % count);
            for (int i = 0; i < L.WalkNodes.Count; i++)
            {
                if (CityLayout.DistrictAt(L.WalkNodes[i].Pos) != d) continue;
                if (pick-- == 0)
                {
                    pos = L.WalkNodes[i].Pos;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Where a guild giver stands: its place plus offset, or a short step to the side of a POI's door.</summary>
        public static bool GiverPosition(CityLayout L, string at, float ox, float oz, out Vector3 pos)
        {
            pos = Vector3.zero;
            if (string.IsNullOrEmpty(at)) return false;
            if (CampaignRules04E.IsPlaceToken(at))
            {
                // A cast member is a real person carrying their own marker (campaign guidance), never a legacy giver
                // cylinder; place tokens (an Underheart anchor, a room point) resolve live.
                if (at.StartsWith(CampaignRules04E.CastPrefix, System.StringComparison.Ordinal)) return false;
                if (!ResolveToken(at, out pos)) return false;
                pos += new Vector3(ox, 0f, oz);
                return true;
            }
            if (L == null) return false;
            SpecialSite s = L.Site(at);
            if (s != null)
            {
                pos = s.Position + new Vector3(ox, 0f, oz);
                return true;
            }
            Poi p = L.FindPoi(at);
            if (p == null) return false;
            if (ox == 0f && oz == 0f)
            {
                // Beside the door, so the story mission's marker on the POI itself stays clear.
                Vector3 f = p.Facing.sqrMagnitude > 0.01f ? p.Facing.normalized : Vector3.forward;
                pos = p.Position + MathUtil.RightOf(f) * 4.5f;
            }
            else pos = p.Position + new Vector3(ox, 0f, oz);
            return true;
        }

        /// <summary>A readable name for a named place: the site's name, the POI's own name, else PlaceName of the point.</summary>
        public static string DisplayName(CityLayout L, string at, Vector3 p)
        {
            if (L != null && !string.IsNullOrEmpty(at))
            {
                SpecialSite s = L.Site(at);
                if (s != null) return s.Name;
                if (L.FindPoi(at) != null) return at;
            }
            return PlaceName(L, p);
        }

        /// <summary>A readable name for a map point: the nearest site within its radius, else the district.</summary>
        public static string PlaceName(CityLayout L, Vector3 p)
        {
            if (L != null)
            {
                foreach (SpecialSite s in L.Sites)
                {
                    if (MathUtil.FlatDistance(s.Position, p) < s.Radius + 30f) return s.Name;
                }
            }
            return CityLayout.DistrictName(CityLayout.DistrictAt(p));
        }

        // ================================================================== habitats (contracts)

        /// <summary>True when contracts can send the player into this habitat.</summary>
        public static bool HabitatSupported(string habitat)
        {
            return QuestCatalog.Contains(QuestCatalog.ContractHabitats, habitat);
        }

        /// <summary>The first habitat of a species that contracts support, picked by seed when it has several.</summary>
        public static string HabitatFor(CreatureSpecies sp, int seed)
        {
            if (sp == null || sp.Habitats == null) return "";
            List<string> ok = new List<string>();
            foreach (string h in sp.Habitats) if (HabitatSupported(h)) ok.Add(h);
            if (ok.Count == 0) return "";
            return ok[(int)(((long)seed % ok.Count + ok.Count) % ok.Count)];
        }

        /// <summary>
        /// A hunting ground for a habitat: a deterministic pick (by seed) among candidate points, with a readable place
        /// name. Inner means any city sidewalk (the hunt happens inside the Inner World there).
        /// </summary>
        public static bool HabitatPoint(CityLayout L, string habitat, int seed, out Vector3 point, out string place)
        {
            point = Vector3.zero;
            place = "";
            if (L == null) return false;
            List<Vector3> cands = new List<Vector3>();
            HabitatCandidates(L, habitat, cands);
            if (cands.Count == 0) return false;
            GRand r = new GRand(seed);
            point = cands[r.Range(0, cands.Count)];
            switch (habitat)
            {
                case Habitat.Frost: place = "the Frostline snowfields"; break;
                case Habitat.FrostLake: place = "Rimeglass Lake"; break;
                case Habitat.Quarry: place = "the Quartz Badlands Quarry"; break;
                case Habitat.Canyon: place = "Glass Row Canyon"; break;
                case Habitat.Badlands: place = "the Quartz Badlands"; break;
                case Habitat.Parks: place = ParkName(L, point); break;
                case Habitat.Construction: place = "a yard in " + CityLayout.DistrictName(CityLayout.DistrictAt(point)); break;
                case Habitat.Grave: place = "Grave Frequency"; break;
                case Habitat.Inner: place = "the Inner World over " + CityLayout.DistrictName(CityLayout.DistrictAt(point)); break;
                default: place = CityLayout.DistrictName(CityLayout.DistrictAt(point)); break;
            }
            return true;
        }

        private static string ParkName(CityLayout L, Vector3 p)
        {
            Block b = L.BlockAt(p);
            if (b != null)
            {
                foreach (Lot lot in b.Lots) if (lot.Use == LotUse.Cemetery) return "Grave Frequency Cemetery";
            }
            return "a park in " + CityLayout.DistrictName(CityLayout.DistrictAt(p));
        }

        /// <summary>Every candidate point for a habitat, in a stable order.</summary>
        public static void HabitatCandidates(CityLayout L, string habitat, List<Vector3> into)
        {
            into.Clear();
            switch (habitat)
            {
                case Habitat.Frost:
                    foreach (Vector3 p in L.SnowRockSpots) AddWild(L, into, p + new Vector3(4f, 0f, 3f), District.Frostline);
                    for (int i = 0; i < L.PineSpots.Count; i += 9) AddWild(L, into, L.PineSpots[i] + new Vector3(5f, 0f, -4f), District.Frostline);
                    break;
                case Habitat.FrostLake:
                    for (int i = 0; i < 12; i++)
                    {
                        float a = i / 12f * Mathf.PI * 2f;
                        float rr = CityLayout.FrostLakeShore(a) + 7f;
                        AddWild(L, into, CityLayout.FrostLakeCenter + new Vector3(Mathf.Cos(a) * rr, 0f, Mathf.Sin(a) * rr), District.Frostline);
                    }
                    break;
                case Habitat.Badlands:
                    for (int i = 0; i < L.CactusSpots.Count; i += 3) AddWild(L, into, L.CactusSpots[i] + new Vector3(3f, 0f, 3f), District.QuartzBadlands);
                    for (int i = 0; i < L.RockSpots.Count; i += 3) AddWild(L, into, L.RockSpots[i] + new Vector3(-3f, 0f, 3f), District.QuartzBadlands);
                    break;
                case Habitat.Quarry:
                case Habitat.Canyon:
                {
                    SpecialSite s = L.Site(habitat == Habitat.Quarry ? "Quarry" : "Canyon");
                    if (s == null) break;
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i / 8f * Mathf.PI * 2f + 0.3f;
                        Vector3 p = s.Position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (s.Radius * 0.45f);
                        // The quarry floor is the Shard Golem Foreman's arena: its hunting grounds ring the quarry instead.
                        Vector3 q = PushOutOfArenas(L, p);
                        if (q == p) into.Add(p);
                        else AddWild(L, into, q, CityLayout.DistrictAt(q));
                    }
                    break;
                }
                case Habitat.Parks:
                    foreach (Block b in L.Blocks) if (b.IsPark) into.Add(b.Inner.Center(CityLayout.CurbHeight));
                    break;
                case Habitat.Construction:
                    foreach (Block b in L.Blocks)
                        foreach (Lot lot in b.Lots)
                            if (lot.Use == LotUse.Empty || lot.Use == LotUse.Parking && (b.District == District.AshUnderpass || b.District == District.SunkenRow))
                                into.Add(lot.Area.Center(CityLayout.CurbHeight));
                    break;
                case Habitat.Grave:
                    for (int i = 0; i < L.WalkNodes.Count; i += 7) if (CityLayout.DistrictAt(L.WalkNodes[i].Pos) == District.GraveFrequency) into.Add(L.WalkNodes[i].Pos);
                    break;
                case Habitat.CityNight:
                case Habitat.Inner:
                    for (int i = 0; i < L.WalkNodes.Count; i += 23)
                    {
                        District d = CityLayout.DistrictAt(L.WalkNodes[i].Pos);
                        if (!CityLayout.IsWilderness(d)) into.Add(L.WalkNodes[i].Pos);
                    }
                    break;
            }
            // No hunting ground inside (or at the edge of) a boss arena: walking in there would start the boss fight.
            RemoveArenaPoints(L, into);
        }

        private static void AddWild(CityLayout L, List<Vector3> into, Vector3 p, District want)
        {
            if (CityLayout.DistrictAt(p) != want) return;
            if (Mathf.Abs(p.x) > CityLayout.MapHalf - 30f || Mathf.Abs(p.z) > CityLayout.MapHalf - 30f) return;
            if (L.InsideRidge(p, 6f) || L.IsInsideBuilding(p, 3f)) return;
            if (CityLayout.IsOnFrozenLake(p.x, p.z) && want == District.Frostline && MathUtil.FlatDistance(p, CityLayout.FrostLakeCenter) < CityLayout.FrostLakeRadius * 0.9f) return;
            if (MathUtil.FlatDistance(p, CityLayout.WardenOutpostCenter) < CityLayout.WardenOutpostRadius + 30f) return;
            if (MathUtil.FlatDistance(p, CityLayout.TitanHollowCenter) < CityLayout.TitanHollowRadius + 15f) return;
            into.Add(p);
        }

        // ================================================================== event anchors

        /// <summary>
        /// Candidate spawn points for an event anchor in a ring from minDist to maxDist around a point (stable order).
        /// "Site:Key" accepts the site even a little outside the ring (sites are rare, so the band is wider); a site
        /// that is a boss arena (the quarry) offers points on the ground ringing the arena instead of its centre.
        /// No candidate is ever inside a boss arena or its margin.
        /// </summary>
        public static void AnchorCandidates(CityLayout L, string anchor, Vector3 around, float minDist, float maxDist, List<Vector3> into)
        {
            into.Clear();
            if (L == null || string.IsNullOrEmpty(anchor)) return;
            if (anchor.StartsWith(SitePrefix))
            {
                SpecialSite s = L.Site(anchor.Substring(SitePrefix.Length));
                if (s == null) return;
                ArenaZone a;
                if (InBossArena(L, s.Position, ArenaMargin, out a))
                {
                    District want = CityLayout.DistrictAt(s.Position);
                    float rr = a.Radius + ArenaMargin + 4f;
                    for (int i = 0; i < 8; i++)
                    {
                        float ang = i / 8f * Mathf.PI * 2f;
                        Vector3 p = a.Center + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * rr;
                        float pd = MathUtil.FlatDistance(p, around);
                        if (pd < minDist * 0.6f || pd > maxDist * 1.5f) continue;
                        if (CityLayout.DistrictAt(p) != want || L.InsideRidge(p, 5f) || L.IsInsideBuilding(p, 3f)) continue;
                        if (Mathf.Abs(p.x) > CityLayout.MapHalf - 30f || Mathf.Abs(p.z) > CityLayout.MapHalf - 30f) continue;
                        into.Add(p);
                    }
                    RemoveArenaPoints(L, into);
                    return;
                }
                float d = MathUtil.FlatDistance(s.Position, around);
                if (d >= minDist * 0.6f && d <= maxDist * 1.5f) into.Add(s.Position);
                return;
            }
            switch (anchor)
            {
                case "Sidewalk":
                    for (int i = 0; i < L.WalkNodes.Count; i += 3) Ring(into, L.WalkNodes[i].Pos, around, minDist, maxDist);
                    break;
                case "Road":
                    for (int i = 0; i < L.Lanes.Count; i++)
                    {
                        LanePath lane = L.Lanes[i];
                        if (lane.IsConnector || lane.Length < 40f) continue;
                        Ring(into, lane.PointAt(lane.Length * 0.5f), around, minDist, maxDist);
                    }
                    break;
                case "Parking":
                    for (int i = 0; i < L.ParkingSpots.Count; i += 2) Ring(into, L.ParkingSpots[i], around, minDist, maxDist);
                    break;
                case "Park":
                    foreach (Block b in L.Blocks) if (b.IsPark) Ring(into, b.Inner.Center(CityLayout.CurbHeight), around, minDist, maxDist);
                    break;
                case "Plaza":
                    foreach (Block b in L.Blocks)
                        foreach (Lot lot in b.Lots)
                            if (lot.Use == LotUse.Plaza || lot.Use == LotUse.Court || lot.Use == LotUse.Parking) Ring(into, lot.Area.Center(CityLayout.CurbHeight), around, minDist, maxDist);
                    break;
                case "Lot":
                    foreach (Block b in L.Blocks)
                        foreach (Lot lot in b.Lots)
                            if (lot.Use == LotUse.Empty || lot.Use == LotUse.Parking || lot.Use == LotUse.Court) Ring(into, lot.Area.Center(CityLayout.CurbHeight), around, minDist, maxDist);
                    break;
                case "Canal":
                    for (float x = -760f; x <= 760f; x += 20f)
                    {
                        Ring(into, new Vector3(x, CityLayout.CurbHeight, CityLayout.CanalZ + CityLayout.CanalHalfWidth + 2.5f), around, minDist, maxDist);
                        Ring(into, new Vector3(x + 10f, CityLayout.CurbHeight, CityLayout.CanalZ - CityLayout.CanalHalfWidth - 2.5f), around, minDist, maxDist);
                    }
                    break;
                case "Lamp":
                    for (int i = 0; i < L.StreetLampSpots.Count; i += 2) Ring(into, L.StreetLampSpots[i], around, minDist, maxDist);
                    break;
                case "Desert":
                    foreach (Vector3 p in L.CactusSpots) WildRing(L, into, p + new Vector3(3f, 0f, 3f), around, minDist, maxDist, District.QuartzBadlands);
                    foreach (Vector3 p in L.RockSpots) WildRing(L, into, p + new Vector3(-3f, 0f, 3f), around, minDist, maxDist, District.QuartzBadlands);
                    break;
                case "Snow":
                    foreach (Vector3 p in L.SnowRockSpots) WildRing(L, into, p + new Vector3(4f, 0f, 3f), around, minDist, maxDist, District.Frostline);
                    for (int i = 0; i < L.PineSpots.Count; i += 4) WildRing(L, into, L.PineSpots[i] + new Vector3(5f, 0f, -4f), around, minDist, maxDist, District.Frostline);
                    break;
                case "Any":
                {
                    District d = CityLayout.DistrictAt(around);
                    if (d == District.Frostline) AnchorCandidates(L, "Snow", around, minDist, maxDist, into);
                    else if (d == District.QuartzBadlands) AnchorCandidates(L, "Desert", around, minDist, maxDist, into);
                    else AnchorCandidates(L, "Sidewalk", around, minDist, maxDist, into);
                    break;
                }
            }
            RemoveArenaPoints(L, into);
        }

        private static void Ring(List<Vector3> into, Vector3 p, Vector3 around, float minDist, float maxDist)
        {
            float d = MathUtil.FlatDistance(p, around);
            if (d >= minDist && d <= maxDist) into.Add(p);
        }

        private static void WildRing(CityLayout L, List<Vector3> into, Vector3 p, Vector3 around, float minDist, float maxDist, District want)
        {
            float d = MathUtil.FlatDistance(p, around);
            if (d < minDist || d > maxDist) return;
            if (CityLayout.DistrictAt(p) != want) return;
            if (L.InsideRidge(p, 5f) || L.IsInsideBuilding(p, 3f)) return;
            into.Add(p);
        }

        // ================================================================== boss arenas

        private static readonly ArenaZone[] NoArenas = new ArenaZone[0];

        /// <summary>
        /// The boss arenas of a city, placed exactly as CreatureDirector.InitBosses places them (keep the two in step):
        /// the Glacier Titan in Titan Hollow, the Shard Golem Foreman on the quarry floor, the Mirror Wraith Queen at
        /// Crystal Manor and the Mirage Djinn on the Mirage Flats (the same search as CreatureDirector.FindDjinnFlats).
        /// Pure layout maths, so contracts, events, the validator and the offline tools keep clear of the arenas the game
        /// uses. Cached for the last layout asked about.
        /// </summary>
        public static IList<ArenaZone> BossArenas(CityLayout L)
        {
            if (L == null) return NoArenas;
            if (_arenaLayout == L && _arenas.Count > 0) return _arenas;
            _arenas.Clear();
            _arenaLayout = L;
            _arenas.Add(new ArenaZone { BossId = "glacier_titan", Center = CityLayout.TitanHollowCenter, Radius = CityLayout.TitanHollowRadius * 0.82f });
            SpecialSite q = L.Site("Quarry");
            if (q != null) _arenas.Add(new ArenaZone { BossId = "golem_foreman", Center = q.Position, Radius = q.Radius * 0.8f });
            SpecialSite m = L.Site("CrystalManor");
            if (m != null) _arenas.Add(new ArenaZone { BossId = "wraith_queen", Center = m.Position, Radius = Mathf.Max(30f, m.Radius), Inner = true });
            _arenas.Add(new ArenaZone { BossId = "mirage_djinn", Center = DjinnFlats(L), Radius = 50f });
            return _arenas;
        }

        /// <summary>The arena a boss fights in on this city (false for species without one).</summary>
        public static bool BossArenaOf(CityLayout L, string bossId, out ArenaZone arena)
        {
            IList<ArenaZone> all = BossArenas(L);
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].BossId != bossId) continue;
                arena = all[i];
                return true;
            }
            arena = new ArenaZone();
            return false;
        }

        /// <summary>True when p lies within a boss arena's radius plus margin.</summary>
        public static bool InBossArena(CityLayout L, Vector3 p, float margin)
        {
            ArenaZone a;
            return InBossArena(L, p, margin, out a);
        }

        /// <summary>True when p lies within a boss arena's radius plus margin; arena is the first such arena.</summary>
        public static bool InBossArena(CityLayout L, Vector3 p, float margin, out ArenaZone arena)
        {
            IList<ArenaZone> all = BossArenas(L);
            for (int i = 0; i < all.Count; i++)
            {
                if (MathUtil.FlatDistance(all[i].Center, p) >= all[i].Radius + margin) continue;
                arena = all[i];
                return true;
            }
            arena = new ArenaZone();
            return false;
        }

        /// <summary>A point inside an arena's margin, moved straight out from the arena's centre to just past the margin (other points are returned as they are).</summary>
        public static Vector3 PushOutOfArenas(CityLayout L, Vector3 p)
        {
            ArenaZone a;
            if (!InBossArena(L, p, ArenaMargin, out a)) return p;
            Vector3 d = MathUtil.Flat(p - a.Center);
            d = d.sqrMagnitude > 0.01f ? d.normalized : Vector3.right;
            Vector3 q = a.Center + d * (a.Radius + ArenaMargin + 6f);
            q.y = p.y;
            return q;
        }

        private static void RemoveArenaPoints(CityLayout L, List<Vector3> list)
        {
            for (int i = list.Count - 1; i >= 0; i--) if (InBossArena(L, list[i], ArenaMargin)) list.RemoveAt(i);
        }

        /// <summary>
        /// The Mirage Flats: a deterministic open spot deep in the south-east Badlands, about (1400, -1400) on the default
        /// city. A copy of CreatureDirector.FindDjinnFlats (desert ground far from roads, ridges, lots, landmarks and
        /// creature spots, searched from the far corner in); QuestFixTests checks the two agree.
        /// </summary>
        private static Vector3 DjinnFlats(CityLayout layout)
        {
            Vector3 fallback = new Vector3(1350f, 0f, -1350f);
            for (float depth = 1400f; depth >= 1050f; depth -= 50f)
            {
                for (int k = 0; k <= 12; k++)
                {
                    float off = (k + 1) / 2 * 50f;
                    Vector3 p = k % 2 == 0 ? new Vector3(depth, 0f, -depth + off) : new Vector3(depth - off, 0f, -depth);
                    if (Mathf.Min(p.x, -p.z) < 1000f) continue;
                    if (CityLayout.DistrictAt(p) != District.QuartzBadlands || CityLayout.IsFrostline(p.x, p.z)) continue;
                    if (layout.InsideRidge(p, 45f) || layout.NearAnyRoad(p, 70f)) continue;
                    if (NearDesertLot(layout, p, 80f) || NearSite(layout, p, 160f) || NearSpot(layout.CreatureSpots, p, 60f)) continue;
                    return p;
                }
            }
            return fallback;
        }

        private static bool NearDesertLot(CityLayout layout, Vector3 p, float margin)
        {
            for (int i = 0; i < layout.DesertLots.Count; i++)
            {
                RectF r = layout.DesertLots[i].Area;
                float dx = Mathf.Max(0f, Mathf.Max(r.X0 - p.x, p.x - r.X1));
                float dz = Mathf.Max(0f, Mathf.Max(r.Z0 - p.z, p.z - r.Z1));
                if (Mathf.Sqrt(dx * dx + dz * dz) < margin) return true;
            }
            return false;
        }

        private static bool NearSite(CityLayout layout, Vector3 p, float margin)
        {
            for (int i = 0; i < layout.Sites.Count; i++) if (MathUtil.FlatDistance(layout.Sites[i].Position, p) < layout.Sites[i].Radius + margin) return true;
            return false;
        }

        private static bool NearSpot(List<Vector3> spots, Vector3 p, float margin)
        {
            for (int i = 0; i < spots.Count; i++) if (MathUtil.FlatDistance(spots[i], p) < margin) return true;
            return false;
        }
    }

    /// <summary>A boss arena as the quest and event data see it: whose it is, where, and how far it reaches.</summary>
    public struct ArenaZone
    {
        public string BossId;
        public Vector3 Center;
        /// <summary>The arena's radius (the boss fight starts within 72 percent of it once the arena is found).</summary>
        public float Radius;
        /// <summary>The arena exists only inside the Inner World (Crystal Manor).</summary>
        public bool Inner;
    }
}
