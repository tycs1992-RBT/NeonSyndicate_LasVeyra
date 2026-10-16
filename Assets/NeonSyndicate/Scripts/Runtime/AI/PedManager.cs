using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Keeps the streets alive around the player: spawns pedestrians on sidewalks (fewer at night and in the desert),
    /// gang crews on their turf, foot patrols near police stations, and cleans up bodies and far-away characters.
    /// </summary>
    public sealed class PedManager : MonoBehaviour
    {
        private const float SpawnMin = 38f;
        private const float SpawnMax = 115f;
        private const float DespawnDist = 155f;
        private float _timer;
        private float _gangTimer;
        private float _cleanupTimer;
        private System.Random _rand = new System.Random(4242);
        private readonly List<int> _nodes = new List<int>();
        private readonly HashSet<int> _usedGangSpots = new HashSet<int>();
        private readonly List<Vector3> _gangSpots = new List<Vector3>();
        private readonly List<int> _gangSpotGang = new List<int>();
        private readonly Dictionary<int, List<NpcController>> _gangCrews = new Dictionary<int, List<NpcController>>();
        public bool Suppressed;

        public int CivilianCount { get; private set; }

        public void Init(CityLayout layout)
        {
            // Gang hangouts: parking lots, courts and project plazas on each gang's turf.
            foreach (Block b in layout.Blocks)
            {
                GangDef g = Gangs.ForDistrict(b.District);
                if (g == null) continue;
                foreach (Lot lot in b.Lots)
                {
                    bool open = lot.Use == LotUse.Parking || lot.Use == LotUse.Court || lot.Use == LotUse.Plaza || lot.Use == LotUse.Park || lot.Use == LotUse.Empty;
                    if (!open && lot.Use != LotUse.Projects && lot.Use != LotUse.Warehouse) continue;
                    if (lot.Entrance != Vector3.zero)
                    {
                        _gangSpots.Add(lot.Entrance - lot.Facing * 3f);
                        _gangSpotGang.Add(g.Id);
                    }
                    else if (open && lot.Buildings.Count == 0 && lot.Area.Valid)
                    {
                        _gangSpots.Add(lot.Area.Center(0f));
                        _gangSpotGang.Add(g.Id);
                    }
                }
                // Street corners on every turf block, so all eight gangs have somewhere to hang out.
                if (b.WalkNodes.Count > 0 && (b.Id * 7919) % 100 < 40)
                {
                    int node = b.WalkNodes[(b.Id * 31) % b.WalkNodes.Count];
                    if (node >= 0 && node < layout.WalkNodes.Count)
                    {
                        _gangSpots.Add(layout.WalkNodes[node].Pos);
                        _gangSpotGang.Add(g.Id);
                    }
                }
            }
            // Quartz Kings hold the trailer park and the quarry.
            foreach (string key in new[] { "TrailerPark", "Quarry" })
            {
                SpecialSite s = layout.Site(key);
                if (s == null) continue;
                _gangSpots.Add(s.Position + new Vector3(8f, 0f, 6f));
                _gangSpotGang.Add(1);
            }
        }

        private int DesiredCivilians(Vector3 p)
        {
            District d = CityLayout.DistrictAt(p);
            float density = 30f * Settings.PedDensity;
            if (d == District.Frostline) return 0; // nobody walks out in the snow
            if (d == District.QuartzBadlands) density *= 0.08f;
            else if (d == District.MirageStrip || d == District.LasVeyraCore) density *= 1.35f;
            else if (d == District.AshUnderpass || d == District.SunkenRow) density *= 0.85f;
            if (Game.Atmo != null) density *= Mathf.Lerp(1f, 0.55f, Game.Atmo.NightFactor);
            if (Game.Atmo != null && Game.Atmo.RainAmount > 0.4f) density *= 0.7f;
            if (Game.Player != null && Game.Player.InVehicle && Game.Player.CurrentVehicle.Speed > 25f) density *= 0.7f;
            return Mathf.RoundToInt(density);
        }

        private void Update()
        {
            if (Game.Layout == null || Game.Player == null) return;
            // Gang heat keeps cooling and bodies keep clearing while the spawners are paused (the Veil, scripted scenes).
            Gangs.Cool(Time.deltaTime);
            _cleanupTimer -= Time.deltaTime;
            Vector3 p = Game.PlayerPosition;
            if (_cleanupTimer <= 0f)
            {
                _cleanupTimer = 0.5f;
                Cleanup(p);
            }
            if (Suppressed) return;
            // 04E: nobody spawns on the street above a player who is underground.
            if (WorldBounds.IsUnderground(p)) return;
            _timer -= Time.deltaTime;
            _gangTimer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = 0.18f;
                if (CivilianCount < DesiredCivilians(p)) TrySpawnCivilian(p);
            }
            if (_gangTimer <= 0f)
            {
                _gangTimer = 1.5f;
                SpawnGangCrews(p);
                SpawnPolicePatrols(p);
            }
        }

        private void Cleanup(Vector3 p)
        {
            int civ = 0;
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            for (int i = NpcController.All.Count - 1; i >= 0; i--)
            {
                NpcController n = NpcController.All[i];
                if (n == null) continue;
                if (n.Persistent) continue;
                if (n.InVehicle) continue;
                // Humanoid creatures belong to the creature director (it removes them, and they dissolve when they die).
                if (n.IsCreature) continue;
                float d = MathUtil.FlatDistance(n.transform.position, p);
                bool visible = cam != null && IsVisible(cam, n.transform.position);
                if (n.IsDead)
                {
                    float age = Time.time - n.DeathTime;
                    if ((age > 50f && !visible) || age > 150f || d > 120f) Destroy(n.gameObject);
                    continue;
                }
                if ((d > DespawnDist && !visible) || d > 250f || (d > 95f && !visible && Time.time - n.SpawnTime > 20f && n.State != NpcState.Fight))
                {
                    Destroy(n.gameObject);
                    continue;
                }
                if (n.Role == NpcRole.Civilian) civ++;
            }
            CivilianCount = civ;
        }

        public static bool IsVisible(Camera cam, Vector3 pos)
        {
            Vector3 v = cam.WorldToViewportPoint(pos + Vector3.up);
            return v.z > 0f && v.x > -0.1f && v.x < 1.1f && v.y > -0.1f && v.y < 1.1f;
        }

        private void TrySpawnCivilian(Vector3 p)
        {
            CityLayout L = Game.Layout;
            L.WalkNodesNear(p, SpawnMax, _nodes);
            if (_nodes.Count == 0) return;
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            for (int attempt = 0; attempt < 6; attempt++)
            {
                int id = _nodes[_rand.Next(_nodes.Count)];
                Vector3 pos = L.WalkNodes[id].Pos;
                float d = MathUtil.FlatDistance(pos, p);
                if (d < SpawnMin) continue;
                // Only spawn where the camera can't see the spot (off screen or behind a building).
                if (cam != null && IsVisible(cam, pos) && !Physics.Linecast(cam.transform.position, pos + Vector3.up * 1.2f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                pos += new Vector3((float)_rand.NextDouble() * 1.4f - 0.7f, 0f, (float)_rand.NextDouble() * 1.4f - 0.7f);
                pos = Ground(pos);
                if (ExcludedVenue04C(pos)) continue;
                Appearance look = BodyFactory.CivilianLook(_rand);
                NpcController n = NpcController.Spawn(pos, (float)_rand.NextDouble() * 360f, NpcRole.Civilian, look, -1);
                n.State = NpcState.Wander;
                // Some pedestrians carry a weapon and will use it when pushed.
                if (_rand.NextDouble() < 0.05) n.Arm(_rand.NextDouble() < 0.5 ? WeaponId.Switchblade : WeaponId.VectorSidearm);
                n.transform.SetParent(transform, true);
                CivilianCount++;
                // Occasionally spawn a companion walking alongside.
                if (_rand.NextDouble() < 0.18)
                {
                    Vector3 friendPosition = pos + new Vector3(0.9f, 0f, 0.4f);
                    if (ExcludedVenue04C(friendPosition)) return;
                    NpcController friend = NpcController.Spawn(friendPosition, n.transform.eulerAngles.y, NpcRole.Civilian, BodyFactory.CivilianLook(_rand), -1);
                    friend.transform.SetParent(transform, true);
                    CivilianCount++;
                }
                return;
            }
        }

        public static Vector3 Ground(Vector3 p)
        {
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out hit, 8f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return hit.point;
            return p;
        }

        private void SpawnGangCrews(Vector3 p)
        {
            // Remove dead or despawned crews.
            List<int> drop = null;
            foreach (KeyValuePair<int, List<NpcController>> kv in _gangCrews)
            {
                kv.Value.RemoveAll(n => n == null || n.IsDead);
                bool far = _gangSpots.Count > kv.Key && MathUtil.FlatDistance(_gangSpots[kv.Key], p) > 170f;
                if (kv.Value.Count == 0 || far)
                {
                    if (far) foreach (NpcController n in kv.Value) if (n != null && !n.Persistent) Destroy(n.gameObject);
                    if (drop == null) drop = new List<int>();
                    drop.Add(kv.Key);
                }
            }
            if (drop != null) foreach (int k in drop) _gangCrews.Remove(k);
            if (_gangCrews.Count >= 4) return;
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            for (int i = 0; i < _gangSpots.Count; i++)
            {
                if (_gangCrews.ContainsKey(i) || _usedGangSpots.Contains(i)) continue;
                float d = MathUtil.FlatDistance(_gangSpots[i], p);
                if (d < 55f || d > 130f) continue;
                if (LocalQuiet.Blocks(_gangSpots[i])) continue;
                if (cam != null && IsVisible(cam, _gangSpots[i]) && d < 90f) continue;
                if (_rand.NextDouble() > 0.35) { _usedGangSpots.Add(i); continue; }
                GangDef g = Gangs.Get(_gangSpotGang[i]);
                if (g == null) continue;
                int count = 2 + _rand.Next(0, 3);
                List<NpcController> crew = new List<NpcController>();
                for (int k = 0; k < count; k++)
                {
                    Vector3 pos = Ground(_gangSpots[i] + new Vector3((float)_rand.NextDouble() * 4f - 2f, 0f, (float)_rand.NextDouble() * 4f - 2f));
                    if (ExcludedVenue04C(pos)) continue;
                    NpcController n = NpcController.Spawn(pos, (float)_rand.NextDouble() * 360f, NpcRole.Gang, Gangs.Look(g, _rand), g.Id);
                    n.Arm(g.Arsenal[_rand.Next(g.Arsenal.Length)]);
                    // Some crews are led by an enforcer: tougher, glowing, and the main source of mythic gear.
                    if (k == 0 && _rand.NextDouble() < 0.35) n.MakeElite(g.Name + " Enforcer");
                    n.GuardAt(pos);
                    n.transform.SetParent(transform, true);
                    crew.Add(n);
                }
                _gangCrews[i] = crew;
                _usedGangSpots.Add(i);
                if (_gangCrews.Count >= 4) return;
            }
            // Spots far away become available again.
            if (_usedGangSpots.Count > 0 && Time.frameCount % 10 == 0)
            {
                List<int> free = new List<int>();
                foreach (int s in _usedGangSpots) if (!_gangCrews.ContainsKey(s) && MathUtil.FlatDistance(_gangSpots[s], p) > 200f) free.Add(s);
                foreach (int s in free) _usedGangSpots.Remove(s);
            }
        }

        private float _patrolTimer;

        private void SpawnPolicePatrols(Vector3 p)
        {
            _patrolTimer -= 1.5f;
            if (_patrolTimer > 0f) return;
            _patrolTimer = 20f;
            if (Game.Layout == null) return;
            Poi station = Game.Layout.NearestPoi(p, PoiType.Police);
            if (station == null) return;
            float d = MathUtil.FlatDistance(station.Position, p);
            if (d < 40f || d > 140f) return;
            // Station officers are capped and recycled once far away, so they can't pile up.
            for (int i = _stationCops.Count - 1; i >= 0; i--)
            {
                NpcController c = _stationCops[i];
                if (c == null || c.IsDead) { _stationCops.RemoveAt(i); continue; }
                if (MathUtil.FlatDistance(c.transform.position, p) > 200f && !c.Persistent)
                {
                    Destroy(c.gameObject);
                    _stationCops.RemoveAt(i);
                }
            }
            if (_stationCops.Count >= 4) return;
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            if (cam != null && d < 90f && IsVisible(cam, station.Position)) return;
            for (int i = 0; i < 2; i++)
            {
                Vector3 pos = Ground(station.Position + MathUtil.RightOf(station.Facing) * (i * 2f - 1f) * 3f + station.Facing * 1.5f);
                if (ExcludedVenue04C(pos)) continue;
                NpcController cop = NpcController.Spawn(pos, MathUtil.YawOf(station.Facing), NpcRole.Police, BodyFactory.PoliceLook(), -1);
                cop.Arm(WeaponId.VectorSidearm);
                cop.transform.SetParent(transform, true);
                _stationCops.Add(cop);
            }
        }

        private readonly List<NpcController> _stationCops = new List<NpcController>();

        internal static bool ExcludedVenue04C(Vector3 point)
        {
            return Game.Inhabited != null && Game.Inhabited.isActiveAndEnabled && Game.Inhabited.IsOperational && Game.Inhabited.ContainsVenueFootprint(point);
        }

        /// <summary>Removes every non-persistent character (mission restarts, respawns).</summary>
        public void ClearAround(Vector3 p, float radius)
        {
            for (int i = NpcController.All.Count - 1; i >= 0; i--)
            {
                NpcController n = NpcController.All[i];
                if (n == null || n.Persistent || n.InVehicle) continue;
                if (MathUtil.FlatDistance(n.transform.position, p) < radius) Destroy(n.gameObject);
            }
        }
    }
}
