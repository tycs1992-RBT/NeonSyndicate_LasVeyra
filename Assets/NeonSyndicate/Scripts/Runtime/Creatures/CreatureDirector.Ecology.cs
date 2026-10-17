using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The ambient ecology: every few seconds it reads the player's surroundings (place, time, weather, world), picks a
    /// species from CreatureEcology's table, orders the group's bodies from the worker threads and, once they are built,
    /// places the group 60 to 140 m away out of the camera's view (flyers higher up), never inside a building, on a road
    /// or in the canal, and well away from mission objectives. Live groups and bodies stay under the graphics preset's
    /// caps (High: 6 groups, 18 bodies; a mite is a quarter body). Respects AmbientEnabled.
    /// </summary>
    public sealed partial class CreatureDirector
    {
        private sealed class GroupPlan
        {
            public CreatureSpecies Species;
            public SpawnSpot Spot;
            public Vector3 Center;
            public bool Air;
            public float Height;
            public int Count;
            public int Seed;
            public bool Elite;
            public float Started;
            public float NextCheck;
            public BodyJob Shared;
            public readonly List<BodyJob> Jobs = new List<BodyJob>();
        }

        private sealed class AmbientGroup
        {
            public string Species;
            public float Cost;
            public readonly List<CharacterBase> Members = new List<CharacterBase>();
        }

        public const float SpawnMin = 60f;
        public const float SpawnMax = 140f;

        private readonly List<SpawnCandidate> _candidates = new List<SpawnCandidate>();
        private readonly List<GroupPlan> _plans = new List<GroupPlan>();
        private readonly List<AmbientGroup> _ambient = new List<AmbientGroup>();
        private readonly List<Vector3> _spotScratch = new List<Vector3>();
        private readonly List<RectF> _rectScratch = new List<RectF>();
        private readonly List<RectF> _parks = new List<RectF>();
        private readonly List<RectF> _sites = new List<RectF>();
        private readonly List<RectF> _cemetery = new List<RectF>();
        private readonly List<Vector3> _graveWalks = new List<Vector3>();
        private float _nextAttempt;
        private bool _ambientWas = true;
        private Vector3 _quarry, _canyon;
        private float _quarryR, _canyonR;
        private bool _hasQuarry, _hasCanyon;

        /// <summary>The surroundings the spawner read last (debug page).</summary>
        public HabitatContext LastHabitat { get; private set; }

        private void InitEcology(CityLayout layout)
        {
            _parks.Clear();
            _sites.Clear();
            _cemetery.Clear();
            _graveWalks.Clear();
            _nextAttempt = Time.time + 15f;
            if (layout == null) return;
            foreach (Block b in layout.Blocks)
            {
                foreach (Lot lot in b.Lots)
                {
                    if (lot.Use == LotUse.Park) _parks.Add(lot.Area);
                    else if (lot.Use == LotUse.Cemetery) _cemetery.Add(lot.Area);
                    // Construction ground: empty dirt lots, and the dirt yards of the Ash Underpass warehouses.
                    else if (lot.Use == LotUse.Empty || (lot.Use == LotUse.Warehouse && b.District == District.AshUnderpass)) _sites.Add(lot.Area);
                }
            }
            foreach (WalkNode w in layout.WalkNodes)
                if (CityLayout.DistrictAt(w.Pos) == District.GraveFrequency) _graveWalks.Add(w.Pos);
            SpecialSite q = layout.Site("Quarry");
            if (q != null) { _quarry = q.Position; _quarryR = q.Radius; _hasQuarry = true; }
            SpecialSite c = layout.Site("Canyon");
            if (c != null) { _canyon = c.Position; _canyonR = c.Radius; _hasCanyon = true; }
        }

        /// <summary>What surrounds a point now: world, district, parks, construction ground, the lake, the quarry, the canyon.</summary>
        public HabitatContext Habitat(Vector3 p)
        {
            HabitatContext c = new HabitatContext();
            c.Inner = Game.Inner != null && Game.Inner.IsInner;
            District d = CityLayout.DistrictAt(p);
            c.City = !CityLayout.IsWilderness(d);
            c.Night = Game.Atmo != null ? Game.Atmo.NightFactor : 0f;
            c.Weather = Game.Atmo != null ? Game.Atmo.Current : Weather.Clear;
            if (c.City)
            {
                c.Park = NearAnyRect(_parks, p, SpawnMax);
                c.Construction = NearAnyRect(_sites, p, SpawnMax);
                c.Grave = d == District.GraveFrequency;
            }
            else
            {
                c.Frost = CityLayout.IsFrostline(p.x, p.z);
                c.Badlands = !c.Frost;
                c.FrostLake = c.Frost && MathUtil.FlatDistance(p, CityLayout.FrostLakeCenter) < CityLayout.FrostLakeRadius + 160f;
                c.Quarry = _hasQuarry && MathUtil.FlatDistance(p, _quarry) < _quarryR + 150f;
                c.Canyon = _hasCanyon && MathUtil.FlatDistance(p, _canyon) < _canyonR + 260f;
            }
            return c;
        }

        private static bool NearAnyRect(List<RectF> rects, Vector3 p, float range)
        {
            for (int i = 0; i < rects.Count; i++) if (RectDistance(rects[i], p) < range) return true;
            return false;
        }

        private static float RectDistance(RectF r, Vector3 p)
        {
            float dx = Mathf.Max(0f, Mathf.Max(r.X0 - p.x, p.x - r.X1));
            float dz = Mathf.Max(0f, Mathf.Max(r.Z0 - p.z, p.z - r.Z1));
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        // ================================================================== the spawn loop

        private void UpdateEcology(float dt)
        {
            UpdatePlans();
            if (AmbientEnabled != _ambientWas)
            {
                _ambientWas = AmbientEnabled;
                if (!AmbientEnabled) ClearAmbientAround();
            }
            if (!AmbientEnabled || Time.time < _nextAttempt) return;
            PlayerController p = Game.Player;
            if (p == null || p.IsDead || QaTools.Busy)
            {
                _nextAttempt = Time.time + 3f;
                return;
            }
            // 04E: ambient surface spawns pause while the player is underground (the Underheart has its own residents).
            if (WorldBounds.IsUnderground(Game.PlayerPosition))
            {
                _nextAttempt = Time.time + 3f;
                return;
            }
            HabitatContext ctx = Habitat(Game.PlayerPosition);
            LastHabitat = ctx;
            _nextAttempt = Time.time + CreatureEcology.SpawnInterval(ctx) * (0.75f + 0.5f * (float)_rng.NextDouble());
            TryPlanGroup(ctx);
        }

        /// <summary>Ambient wildlife that is not fighting the player fades out (a mission switched the ecology off).</summary>
        private void ClearAmbientAround()
        {
            Vector3 pp = Game.PlayerPosition;
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                CharacterBase b = _live[i];
                if (b == null) { _live.RemoveAt(i); continue; }
                if (b.IsDead || !IsAmbient(b) || EngagedWithPlayer(b)) continue;
                if (MathUtil.FlatDistance(b.transform.position, pp) < 40f) continue;
                Remove(b, true);
                _live.RemoveAt(i);
            }
            CancelPlans();
        }

        /// <summary>Living ambient groups and their body cost (groups still being built count too).</summary>
        private void CountAmbient(out int groups, out float bodies)
        {
            groups = 0;
            bodies = 0f;
            for (int g = _ambient.Count - 1; g >= 0; g--)
            {
                AmbientGroup a = _ambient[g];
                int alive = 0;
                for (int i = a.Members.Count - 1; i >= 0; i--)
                {
                    CharacterBase b = a.Members[i];
                    if (b == null || b.IsDead) { a.Members.RemoveAt(i); continue; }
                    alive++;
                }
                if (alive == 0) { _ambient.RemoveAt(g); continue; }
                groups++;
                bodies += alive * a.Cost;
            }
            for (int i = 0; i < _plans.Count; i++)
            {
                groups++;
                bodies += _plans[i].Count * CreatureEcology.BodyCost(_plans[i].Species);
            }
            // 04E: live companion and rescue bodies use the same body budget (BONDS hook).
            bodies += CompanionBudget.BodyCost;
        }

        private int AmbientOfSpecies(string id)
        {
            int n = 0;
            for (int g = 0; g < _ambient.Count; g++)
            {
                if (_ambient[g].Species != id) continue;
                for (int i = 0; i < _ambient[g].Members.Count; i++)
                {
                    CharacterBase b = _ambient[g].Members[i];
                    if (b != null && !b.IsDead) n++;
                }
            }
            for (int i = 0; i < _plans.Count; i++) if (_plans[i].Species.Id == id) n += _plans[i].Count;
            return n;
        }

        /// <summary>Ambient groups alive and bodies used right now, and the caps (debug page).</summary>
        public void AmbientStats(out int groups, out float bodies, out int capGroups, out float capBodies)
        {
            CountAmbient(out groups, out bodies);
            CreatureEcology.Caps(Settings.Graphics, out capGroups, out capBodies);
        }

        private void TryPlanGroup(HabitatContext ctx)
        {
            int capGroups;
            float capBodies;
            CreatureEcology.Caps(Settings.Graphics, out capGroups, out capBodies);
            int groups;
            float bodies;
            CountAmbient(out groups, out bodies);
            float room = capBodies - bodies;
            if (groups >= capGroups || room < 0.24f) return;
            CreatureEcology.Candidates(ctx, _candidates);
            for (int i = _candidates.Count - 1; i >= 0; i--)
            {
                SpawnCandidate s = _candidates[i];
                CreatureSpecies sp = CreatureSpeciesDb.Get(s.Species);
                bool drop = sp == null || AmbientOfSpecies(s.Species) + s.Min > CreatureEcology.MaxAlive(sp) || CreatureEcology.BodyCost(sp) * Mathf.Min(s.Min, 4) > room + 0.01f;
                if (drop) _candidates.RemoveAt(i);
            }
            SpawnCandidate pick;
            if (!CreatureEcology.Pick(_candidates, (float)_rng.NextDouble(), out pick)) return;
            CreatureSpecies species = CreatureSpeciesDb.Get(pick.Species);
            if (species == null) return;
            int n = CreatureEcology.GroupSize(pick, (float)_rng.NextDouble(), Settings.Graphics);
            float cost = CreatureEcology.BodyCost(species);
            n = Mathf.Min(n, Mathf.FloorToInt(room / cost + 0.001f), CreatureEcology.MaxAlive(species) - AmbientOfSpecies(species.Id));
            if (n < 1) return;
            Vector3 center;
            bool air;
            float height;
            if (!FindSpawnPoint(pick.Spot, species, out center, out air, out height)) return;
            GroupPlan plan = new GroupPlan();
            plan.Species = species;
            plan.Spot = pick.Spot;
            plan.Center = center;
            plan.Air = air;
            plan.Height = height;
            plan.Count = n;
            plan.Seed = _rng.Next(1, int.MaxValue / 2);
            plan.Elite = !species.GreatBeast && !species.Boss && species.Id != "glass_mite" && _rng.NextDouble() < (ctx.IsNight ? 0.06 : 0.035);
            plan.Started = Time.time;
            if (species.Id == "glass_mite") plan.Shared = SwarmBody(plan.Seed);
            else for (int i = 0; i < n; i++) plan.Jobs.Add(RequestBody(species, plan.Seed + i * 7919, plan.Elite && i == 0));
            _plans.Add(plan);
        }

        /// <summary>Groups whose bodies are built step into the world (still out of view, or they wait a moment).</summary>
        private void UpdatePlans()
        {
            for (int i = _plans.Count - 1; i >= 0; i--)
            {
                GroupPlan plan = _plans[i];
                if (Time.time - plan.Started > 25f)
                {
                    CancelPlan(plan);
                    _plans.RemoveAt(i);
                    continue;
                }
                if (Time.time < plan.NextCheck) continue;
                plan.NextCheck = Time.time + 0.2f;
                if (!PlanReady(plan)) continue;
                // The player may have turned toward the spot while the bodies were building: find another, or wait.
                Vector3 test = plan.Center + Vector3.up * (plan.Air ? 0f : 1f);
                float dist = MathUtil.FlatDistance(plan.Center, Game.PlayerPosition);
                if (SeenByCamera(test) || dist < SpawnMin * 0.75f || dist > SpawnMax * 1.3f || LocalQuiet.Blocks(plan.Center))
                {
                    Vector3 c;
                    bool air;
                    float h;
                    if (!FindSpawnPoint(plan.Spot, plan.Species, out c, out air, out h))
                    {
                        plan.NextCheck = Time.time + 1f;
                        continue;
                    }
                    plan.Center = c;
                    plan.Air = air;
                    plan.Height = h;
                }
                _plans.RemoveAt(i);
                PlaceGroup(plan);
            }
        }

        private bool PlanReady(GroupPlan plan)
        {
            if (plan.Shared != null) return BodyReady(plan.Shared);
            for (int i = 0; i < plan.Jobs.Count; i++) if (!BodyReady(plan.Jobs[i])) return false;
            return true;
        }

        private void PlaceGroup(GroupPlan plan)
        {
            CreatureSpecies sp = plan.Species;
            AmbientGroup ag = new AmbientGroup();
            ag.Species = sp.Id;
            ag.Cost = CreatureEcology.BodyCost(sp);
            CreatureGroup g = sp.IsHumanoid ? null : NewGroup(sp, plan.Center);
            if (g != null) g.BodySeed = plan.Seed;
            float spacing = GroupSpacing(sp);
            for (int i = 0; i < plan.Count; i++)
            {
                Vector3 p = GroupSlot(plan.Center, i, plan.Count, spacing);
                if (i > 0 && !SlotFree(p, sp)) p = plan.Center + Random.insideUnitSphere * 0.3f;
                if (plan.Air) p.y = plan.Center.y + (i % 3) * 2.5f;
                CreatureSpawnOptions o = new CreatureSpawnOptions();
                o.Hostile = false;
                o.Persistent = false;
                o.Elite = plan.Elite && i == 0;
                o.Seed = plan.Seed + i * 7919;
                BodyJob job = plan.Shared ?? (i < plan.Jobs.Count ? plan.Jobs[i] : null);
                CharacterBase b = SpawnBody(sp, p, (float)_rng.NextDouble() * 360f, o, job, true, g);
                if (b == null) continue;
                SetHome(b, plan.Center);
                ag.Members.Add(b);
            }
            if (ag.Members.Count > 0) _ambient.Add(ag);
        }

        private static void SetHome(CharacterBase b, Vector3 home)
        {
            Creature c = b as Creature;
            if (c != null) { c.Home = home; return; }
            NpcController n = b as NpcController;
            if (n != null) n.Home = home;
        }

        private void CancelPlan(GroupPlan plan)
        {
            for (int i = 0; i < plan.Jobs.Count; i++) plan.Jobs[i].Cancelled = true;
        }

        private void CancelPlans()
        {
            for (int i = 0; i < _plans.Count; i++) CancelPlan(_plans[i]);
            _plans.Clear();
        }

        private void ClearEcologyPlans()
        {
            CancelPlans();
            _ambient.Clear();
            _nextAttempt = Time.time + 6f;
        }

        // ================================================================== where

        /// <summary>
        /// A point for a group of a species at a kind of spot: 60 to 140 m from the player, hidden from the camera, on
        /// open ground (or perched on the canyon rim), away from mission objectives. air: flyers start circling at height.
        /// </summary>
        private bool FindSpawnPoint(SpawnSpot spot, CreatureSpecies sp, out Vector3 pos, out bool air, out float height)
        {
            pos = Vector3.zero;
            air = false;
            height = 0f;
            Vector3 pp = Game.PlayerPosition;
            bool winged = IsWinged(sp);
            PrepareSpot(spot, pp);
            for (int attempt = 0; attempt < 16; attempt++)
            {
                Vector3 c;
                if (!SpotCandidate(spot, pp, out c)) continue;
                float dist = MathUtil.FlatDistance(c, pp);
                if (dist < SpawnMin || dist > SpawnMax) continue;
                float gy;
                if (!GroundFor(spot, c, pp, out gy)) continue;
                c.y = gy;
                if (!GroundOk(spot, sp, c)) continue;
                bool up = winged && spot != SpawnSpot.CanyonRim && _rng.NextDouble() < 0.6;
                float h = up ? Mathf.Max(sp.CruiseHeight, 14f) * (0.9f + 0.3f * (float)_rng.NextDouble()) : 0f;
                Vector3 at = c + Vector3.up * (up ? h : 1f);
                if (up && Physics.CheckSphere(at, 3f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                if (SeenByCamera(at) || NearObjective(c)) continue;
                pos = up ? at : c;
                air = up;
                height = h;
                return true;
            }
            return false;
        }

        private static bool IsWinged(CreatureSpecies sp)
        {
            return sp != null && !sp.IsHumanoid && sp.Flies && sp.Id != "hollow_shade";
        }

        /// <summary>Per attempt: the nearby rectangles or points a spot kind picks from.</summary>
        private void PrepareSpot(SpawnSpot spot, Vector3 pp)
        {
            _rectScratch.Clear();
            _spotScratch.Clear();
            switch (spot)
            {
                case SpawnSpot.Park:
                    for (int i = 0; i < _parks.Count; i++) if (RectDistance(_parks[i], pp) < SpawnMax) _rectScratch.Add(_parks[i]);
                    break;
                case SpawnSpot.Construction:
                    for (int i = 0; i < _sites.Count; i++) if (RectDistance(_sites[i], pp) < SpawnMax) _rectScratch.Add(_sites[i]);
                    break;
                case SpawnSpot.Sidewalk:
                    for (int i = 0; i < _graveWalks.Count; i++)
                    {
                        float d = MathUtil.FlatDistance(_graveWalks[i], pp);
                        if (d > SpawnMin && d < SpawnMax) _spotScratch.Add(_graveWalks[i]);
                    }
                    for (int i = 0; i < _cemetery.Count; i++) if (RectDistance(_cemetery[i], pp) < SpawnMax) _rectScratch.Add(_cemetery[i]);
                    break;
                case SpawnSpot.Inner:
                    if (Game.Inner != null) Game.Inner.CrystalSpotsNear(pp, SpawnMax, _spotScratch);
                    break;
            }
        }

        private bool SpotCandidate(SpawnSpot spot, Vector3 pp, out Vector3 c)
        {
            c = pp;
            float a = (float)_rng.NextDouble() * Mathf.PI * 2f;
            switch (spot)
            {
                case SpawnSpot.Park:
                case SpawnSpot.Construction:
                {
                    if (_rectScratch.Count == 0) return false;
                    RectF r = _rectScratch[_rng.Next(_rectScratch.Count)].Inset(3f);
                    if (r.Width <= 1f || r.Depth <= 1f) return false;
                    c = new Vector3(Mathf.Lerp(r.X0, r.X1, (float)_rng.NextDouble()), pp.y, Mathf.Lerp(r.Z0, r.Z1, (float)_rng.NextDouble()));
                    return true;
                }
                case SpawnSpot.Sidewalk:
                {
                    bool useCemetery = _rectScratch.Count > 0 && (_spotScratch.Count == 0 || _rng.NextDouble() < 0.35);
                    if (useCemetery)
                    {
                        RectF r = _rectScratch[_rng.Next(_rectScratch.Count)].Inset(4f);
                        c = new Vector3(Mathf.Lerp(r.X0, r.X1, (float)_rng.NextDouble()), pp.y, Mathf.Lerp(r.Z0, r.Z1, (float)_rng.NextDouble()));
                        return true;
                    }
                    if (_spotScratch.Count == 0) return false;
                    c = _spotScratch[_rng.Next(_spotScratch.Count)];
                    return true;
                }
                case SpawnSpot.Quarry:
                {
                    if (!_hasQuarry) return false;
                    float r = Mathf.Sqrt((float)_rng.NextDouble()) * _quarryR * 0.78f;
                    c = _quarry + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                    return true;
                }
                case SpawnSpot.FrostLake:
                {
                    float r = CityLayout.FrostLakeShore(a) + 8f + (float)_rng.NextDouble() * 40f;
                    c = CityLayout.FrostLakeCenter + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                    return true;
                }
                case SpawnSpot.CanyonRim:
                {
                    if (!_hasCanyon) return false;
                    int side = _rng.NextDouble() < 0.5 ? -1 : 1;
                    int i = _rng.Next(8);
                    c = new Vector3(_canyon.x + side * (28f + (i % 3) * 3f), 0f, _canyon.z - 120f + i * 30f);
                    return true;
                }
                case SpawnSpot.Inner:
                {
                    if (_spotScratch.Count > 0 && _rng.NextDouble() < 0.75)
                    {
                        // Beside a crystal growth, not inside it.
                        Vector3 s = _spotScratch[_rng.Next(_spotScratch.Count)];
                        c = s + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 3.5f;
                        return true;
                    }
                    break;
                }
            }
            float d = SpawnMin + (float)_rng.NextDouble() * (SpawnMax - SpawnMin);
            c = pp + new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d);
            return true;
        }

        /// <summary>Ground height at a candidate: one ray from well above (the canyon rim: the top of the rock wall).</summary>
        private static bool GroundFor(SpawnSpot spot, Vector3 c, Vector3 pp, out float y)
        {
            y = 0f;
            float top = spot == SpawnSpot.CanyonRim ? 140f : pp.y + 60f;
            RaycastHit hit;
            if (!Physics.Raycast(new Vector3(c.x, top, c.z), Vector3.down, out hit, top + 200f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return false;
            if (hit.normal.y < 0.7f) return false;
            y = hit.point.y;
            if (spot == SpawnSpot.CanyonRim) return y > 15f;
            // The streets, parks, desert and snowfields are all near y = 0: a ray that stopped on a roof, an awning, a
            // car or a tree is not ground.
            return y < 2.5f && y > -1.5f;
        }

        /// <summary>Never in a building, on a road, in the canal or inside a ridge; open space for the body; the right zone.</summary>
        private bool GroundOk(SpawnSpot spot, CreatureSpecies sp, Vector3 c)
        {
            CityLayout L = Game.Layout ?? _layout;
            if (L == null) return true;
            if (InArena(c, 30f)) return false;
            if (LocalQuiet.Blocks(c)) return false;
            if (L.IsInsideBuilding(c, 2f)) return false;
            bool wild = CityLayout.IsWilderness(CityLayout.DistrictAt(c));
            if (spot != SpawnSpot.CanyonRim)
            {
                if (L.RoadAt(c, 1.5f) != null && sp.Id != "dune_wyrm") return false;
                if (wild && L.InsideRidge(c, 6f)) return false;
            }
            if (!wild && Mathf.Abs(c.z - CityLayout.CanalZ) < CityLayout.CanalBandHalf + 2f) return false;
            switch (spot)
            {
                case SpawnSpot.Open:
                    if (!wild && !(Game.Inner != null && Game.Inner.IsInner)) return false;
                    if (sp.Tier == CreatureTier.Frostline && !CityLayout.IsFrostline(c.x, c.z)) return false;
                    if (sp.Tier == CreatureTier.Badlands && CityLayout.IsFrostline(c.x, c.z)) return false;
                    if (sp.Id == "neon_jackal" && CityLayout.IsFrostline(c.x, c.z)) return false;
                    break;
                case SpawnSpot.FrostLake:
                    if (CityLayout.IsOnFrozenLake(c.x, c.z)) return false;
                    break;
            }
            return SlotFree(c, sp);
        }

        /// <summary>Room for the body: nothing solid (props, trees, cars) where it would stand.</summary>
        private static bool SlotFree(Vector3 c, CreatureSpecies sp)
        {
            float r = sp.GreatBeast || sp.Boss ? 2.6f : (sp.Id == "glass_mite" ? 0.3f : 0.8f);
            if (sp.Id == "dune_wyrm") return true;
            int mask = Layers.WorldMask | (1 << Layers.Vehicles) | (1 << Layers.Debris);
            return !Physics.CheckSphere(c + Vector3.up * (r + 0.25f), r, mask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>True when the camera could see a point (in the view and not hidden behind a wall).</summary>
        private static bool SeenByCamera(Vector3 p)
        {
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            if (cam == null) return false;
            Vector3 v = cam.WorldToViewportPoint(p);
            if (v.z <= 0f || v.x < -0.15f || v.x > 1.15f || v.y < -0.15f || v.y > 1.15f) return false;
            return !Physics.Linecast(cam.transform.position, p, Layers.WorldMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Keeps the wildlife away from mission and quest objectives (their map blips).</summary>
        private static bool NearObjective(Vector3 c)
        {
            for (int i = 0; i < Blip.All.Count; i++)
            {
                Blip b = Blip.All[i];
                if (b == null || (b.Kind != BlipKind.Objective && b.Kind != BlipKind.Destination)) continue;
                if (MathUtil.FlatSqrDistance(b.WorldPosition, c) < 70f * 70f) return true;
            }
            return false;
        }
    }
}
