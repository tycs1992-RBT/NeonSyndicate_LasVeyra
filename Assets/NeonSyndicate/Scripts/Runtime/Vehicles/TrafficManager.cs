using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Streams traffic around the player: moving cars with drivers on lanes out of sight, parked cars at the curb,
    /// occasional patrol cruisers and taxis, and cleanup of far, stuck or wrecked vehicles. Hover bikes (safehouse bikes,
    /// LVPD pursuit bikes, the player's own bike and Bring my bike) live in TrafficManager.Bikes.cs.
    /// </summary>
    public sealed partial class TrafficManager : MonoBehaviour
    {
        public readonly List<Vehicle> Moving = new List<Vehicle>();
        public readonly List<Vehicle> ParkedCars = new List<Vehicle>();
        public bool Suppressed;
        private readonly List<int> _lanes = new List<int>();
        private readonly HashSet<int> _usedParking = new HashSet<int>();
        private readonly Dictionary<Vehicle, int> _parkingOf = new Dictionary<Vehicle, int>();
        private System.Random _rand = new System.Random(777);
        private float _timer;
        private float _parkTimer;
        private float _cleanTimer;

        private int Desired(Vector3 p)
        {
            District d = CityLayout.DistrictAt(p);
            float n = 24f * Settings.TrafficDensity;
            if (d == District.Frostline) return 0; // no roads out in the snow
            if (d == District.QuartzBadlands) n *= 0.3f;
            else if (d == District.LasVeyraCore || d == District.MirageStrip) n *= 1.2f;
            if (Game.Atmo != null) n *= Mathf.Lerp(1f, 0.7f, Game.Atmo.NightFactor);
            if (Settings.Graphics == GraphicsPreset.Low) n *= 0.7f;
            return Mathf.RoundToInt(n);
        }

        private void Update()
        {
            if (Game.Layout == null || Game.Player == null) return;
            float dt = Time.deltaTime;
            _timer -= dt;
            _parkTimer -= dt;
            _cleanTimer -= dt;
            Vector3 p = Game.PlayerPosition;
            if (_cleanTimer <= 0f)
            {
                _cleanTimer = 0.5f;
                Cleanup(p);
            }
            UpdateBikes(p, dt);
            if (Suppressed) return;
            if (_timer <= 0f)
            {
                _timer = 0.22f;
                if (Moving.Count < Desired(p)) TrySpawnMoving(p);
            }
            if (_parkTimer <= 0f)
            {
                _parkTimer = 0.6f;
                if (ParkedCars.Count < 14) TrySpawnParked(p);
            }
        }

        // ------------------------------------------------------------------ moving traffic

        private void TrySpawnMoving(Vector3 p)
        {
            CityLayout L = Game.Layout;
            float ahead = 0f;
            if (Game.Player.InVehicle) ahead = Mathf.Clamp(Game.Player.CurrentVehicle.Speed * 2f, 0f, 60f);
            Vector3 center = p + (Game.Player.InVehicle ? Game.Player.CurrentVehicle.Velocity.normalized * ahead : Vector3.zero);
            L.LanesNear(center, 200f, _lanes);
            if (_lanes.Count == 0) return;
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                int id = _lanes[_rand.Next(_lanes.Count)];
                LanePath lane = L.Lanes[id];
                if (lane.IsConnector || lane.Length < 20f) continue;
                float s = (float)_rand.NextDouble() * (lane.Length - 12f) + 4f;
                Vector3 pos = lane.PointAt(s);
                float d = MathUtil.FlatDistance(pos, p);
                if (d < 95f || d > 210f) continue;
                // Never pop a car into plain view (fog doesn't hide this distance); spawn behind buildings or off screen.
                if (cam != null && PedManager.IsVisible(cam, pos) && !Occluded(cam.transform.position, pos)) continue;
                if (!Clear(pos, 14f)) continue;
                Vector3 dir = lane.DirectionAt(s);
                District district = CityLayout.DistrictAt(pos);
                VehicleDef def;
                bool patrol = !CityLayout.IsWilderness(district) && _rand.NextDouble() < 0.06 && CountPolicePatrols() < 2 && (Game.Wanted == null || Game.Wanted.Stars == 0);
                // Now and then the patrol is an LVPD hover bike instead of a cruiser.
                if (patrol) def = _rand.NextDouble() < 0.3 ? VehicleDb.PoliceBike : VehicleDb.FirstOfClass(VehicleClass.Police);
                else def = VehicleDb.RandomCivilian(district, _rand);
                Vehicle v = SpawnCar(def, pos, MathUtil.YawOf(dir), id, s, patrol);
                if (v != null) v.Body.linearVelocity = dir * Mathf.Min(lane.SpeedLimit * 0.7f, 12f);
                return;
            }
        }

        private static bool Occluded(Vector3 from, Vector3 to)
        {
            return Physics.Linecast(from, to + Vector3.up * 1.2f, Layers.WorldMask, QueryTriggerInteraction.Ignore);
        }

        private int CountPolicePatrols()
        {
            int n = 0;
            foreach (Vehicle v in Moving) if (v != null && v.IsPolice) n++;
            return n;
        }

        public static bool Clear(Vector3 pos, float radius)
        {
            for (int i = 0; i < Vehicle.All.Count; i++)
            {
                Vehicle o = Vehicle.All[i];
                if (o != null && (o.transform.position - pos).sqrMagnitude < radius * radius) return false;
            }
            if (Game.Player != null && MathUtil.FlatSqrDistance(Game.PlayerPosition, pos) < 25f) return false;
            return true;
        }

        /// <summary>Spawns a car on a lane with an AI driver (and a partner for police cruisers).</summary>
        public Vehicle SpawnCar(VehicleDef def, Vector3 pos, float yaw, int lane, float s, bool police)
        {
            Vector3 ground = GroundAt(pos);
            Vehicle v = Vehicle.Spawn(def, ground, yaw, null, false);
            v.transform.SetParent(transform, true);
            TrafficDriver ai = v.gameObject.AddComponent<TrafficDriver>();
            ai.Init(v, lane, s);
            NpcController driver = police
                ? NpcController.Spawn(ground + Vector3.up, yaw, NpcRole.Police, BodyFactory.PoliceLook(), -1)
                : NpcController.Spawn(ground + Vector3.up, yaw, NpcRole.Civilian, BodyFactory.CivilianLook(_rand), -1);
            if (police) driver.Arm(WeaponId.VectorSidearm);
            driver.EnterVehicleImmediate(v, 0);
            if (police && def.Seats > 1)
            {
                NpcController partner = NpcController.Spawn(ground + Vector3.up, yaw, NpcRole.Police, BodyFactory.PoliceLook(), -1);
                partner.Arm(WeaponId.VectorSidearm);
                partner.EnterVehicleImmediate(v, 1);
            }
            else if (!police && def.Seats > 1 && _rand.NextDouble() < 0.18)
            {
                NpcController passenger = NpcController.Spawn(ground + Vector3.up, yaw, NpcRole.Civilian, BodyFactory.CivilianLook(_rand), -1);
                passenger.EnterVehicleImmediate(v, 1);
            }
            Moving.Add(v);
            return v;
        }

        public static Vector3 GroundAt(Vector3 p)
        {
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up * 4f, Vector3.down, out hit, 12f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return hit.point;
            return p;
        }

        // ------------------------------------------------------------------ parked cars

        private void TrySpawnParked(Vector3 p)
        {
            CityLayout L = Game.Layout;
            if (L.ParkingSpots.Count == 0) return;
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                int i = _rand.Next(L.ParkingSpots.Count);
                if (_usedParking.Contains(i)) continue;
                Vector3 pos = L.ParkingSpots[i];
                float d = MathUtil.FlatDistance(pos, p);
                if (d < 45f || d > 150f) continue;
                if (cam != null && PedManager.IsVisible(cam, pos) && !Occluded(cam.transform.position, pos)) continue;
                if (!Clear(pos, 5f)) continue;
                VehicleDef def = VehicleDb.RandomCivilian(CityLayout.DistrictAt(pos), _rand);
                if (def.Length > 6f) continue;
                Vehicle v = Vehicle.Spawn(def, GroundAt(pos), L.ParkingYaws[i], null, true);
                v.transform.SetParent(transform, true);
                v.Locked = _rand.NextDouble() < 0.2;
                ParkedCars.Add(v);
                _usedParking.Add(i);
                _parkingOf[v] = i;
                return;
            }
        }

        /// <summary>Spawns a specific parked vehicle (missions, garages, debug menu).</summary>
        public Vehicle SpawnParkedAt(VehicleDef def, Vector3 pos, float yaw, Color? paint)
        {
            Vehicle v = Vehicle.Spawn(def, GroundAt(pos), yaw, paint, true);
            v.transform.SetParent(transform, true);
            ParkedCars.Add(v);
            return v;
        }

        // ------------------------------------------------------------------ cleanup

        private void Cleanup(Vector3 p)
        {
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            for (int i = Moving.Count - 1; i >= 0; i--)
            {
                Vehicle v = Moving[i];
                if (v == null) { Moving.RemoveAt(i); continue; }
                if (v.HasPlayer || v.MissionCritical || KeepsBike(v, p)) continue;
                bool visible = cam != null && PedManager.IsVisible(cam, v.transform.position);
                float d = MathUtil.FlatDistance(v.transform.position, p);
                // Cull out of sight (beyond the 210 m spawn ring), or very far even if visible. Brand-new cars get a grace period.
                bool young = Time.time - v.SpawnTime < 3f;
                bool remove = d > 420f || (d > 230f && !visible && !young);
                if (!remove && v.IsDestroyed && v.DestroyedFor > 45f && !visible) remove = true;
                if (!remove && v.IsUpsideDown && !visible && d > 30f) remove = true;
                if (!remove && v.Driver == null && !v.IsDestroyed && Time.time - v.SpawnTime > 30f && d > 120f && !visible) remove = true;
                if (!remove && v.StolenByPlayer && d > 300f) remove = true;
                if (remove)
                {
                    Moving.RemoveAt(i);
                    DestroyVehicle(v);
                    continue;
                }
                // Cars abandoned by the player become parked cars (kept until far away).
                if (v.Driver == null && v.StolenByPlayer && !ParkedCars.Contains(v))
                {
                    Moving.RemoveAt(i);
                    ParkedCars.Add(v);
                }
            }
            for (int i = ParkedCars.Count - 1; i >= 0; i--)
            {
                Vehicle v = ParkedCars[i];
                if (v == null) { ParkedCars.RemoveAt(i); continue; }
                if (v.HasPlayer || v.MissionCritical || KeepsBike(v, p)) continue;
                bool visible = cam != null && PedManager.IsVisible(cam, v.transform.position);
                float d = MathUtil.FlatDistance(v.transform.position, p);
                if (d > 400f || (d > 170f && !visible) || (v.IsDestroyed && v.DestroyedFor > 45f && !visible))
                {
                    ParkedCars.RemoveAt(i);
                    DestroyVehicle(v);
                }
            }
        }

        private void DestroyVehicle(Vehicle v)
        {
            if (v == null) return;
            if (v == PlayerBike) PlayerBike = null;
            int spot;
            if (_parkingOf.TryGetValue(v, out spot))
            {
                _usedParking.Remove(spot);
                _parkingOf.Remove(v);
            }
            for (int s = 0; s < v.Occupants.Length; s++)
            {
                CharacterBase c = v.Occupants[s];
                if (c == null || c.IsPlayer) continue;
                NpcController n = c as NpcController;
                if (n != null && n.Persistent)
                {
                    n.ExitVehicleImmediate(false);
                    continue;
                }
                Destroy(c.gameObject);
            }
            Destroy(v.gameObject);
        }

        /// <summary>
        /// Removes moving traffic that has an AI driver at the wheel (the Inner World empties the roads). The player's car,
        /// cars the player has driven, police units and drivers already after the player (the Veil is no free escape),
        /// mission vehicles, wrecks and parked cars stay. Returns how many were removed.
        /// </summary>
        public int ClearDrivenTraffic()
        {
            int removed = 0;
            for (int i = Moving.Count - 1; i >= 0; i--)
            {
                Vehicle v = Moving[i];
                if (v == null)
                {
                    Moving.RemoveAt(i);
                    continue;
                }
                if (v.HasPlayer || v.MissionCritical || v.StolenByPlayer || v.IsDestroyed || v.IsPolice) continue;
                CharacterBase driver = v.Driver;
                if (driver == null || driver.IsPlayer || driver.IsDead) continue;
                NpcController npc = driver as NpcController;
                if (npc != null && (npc.Persistent || (npc.Target != null && npc.Target.IsPlayer))) continue;
                Moving.RemoveAt(i);
                DestroyVehicle(v);
                removed++;
            }
            return removed;
        }

        /// <summary>Takes ownership of a vehicle spawned elsewhere so it gets cleaned up like traffic.</summary>
        public void Adopt(Vehicle v)
        {
            if (v != null && !Moving.Contains(v) && !ParkedCars.Contains(v)) Moving.Add(v);
        }

        /// <summary>Removes every traffic and parked vehicle (new game, load, return to title).</summary>
        public void ClearAll()
        {
            for (int i = Moving.Count - 1; i >= 0; i--) DestroyVehicle(Moving[i]);
            for (int i = ParkedCars.Count - 1; i >= 0; i--) DestroyVehicle(ParkedCars[i]);
            Moving.Clear();
            ParkedCars.Clear();
            _usedParking.Clear();
            _parkingOf.Clear();
            Suppressed = false;
            ClearBikes();
        }

        public void ClearAround(Vector3 p, float radius)
        {
            for (int i = Moving.Count - 1; i >= 0; i--)
            {
                Vehicle v = Moving[i];
                if (v == null || v.HasPlayer || v.MissionCritical) continue;
                if (MathUtil.FlatDistance(v.transform.position, p) < radius)
                {
                    Moving.RemoveAt(i);
                    DestroyVehicle(v);
                }
            }
        }
    }
}
