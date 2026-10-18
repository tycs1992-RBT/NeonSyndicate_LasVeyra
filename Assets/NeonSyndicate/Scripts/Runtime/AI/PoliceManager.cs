using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// LVPD response scaled by wanted level: pursuit cruisers and interceptors, officers who bail out and fight or
    /// arrest, SWAT Enforcers at four stars, an air unit with a spotlight and marksman from three stars, roadblocks,
    /// and the BUSTED logic for unarmed suspects at low heat.
    /// </summary>
    public sealed partial class PoliceManager : MonoBehaviour
    {
        public readonly List<Vehicle> Units = new List<Vehicle>();
        public readonly List<NpcController> Officers = new List<NpcController>();
        public Helicopter Heli;
        private readonly List<Vehicle> _roadblocks = new List<Vehicle>();
        private readonly List<int> _lanes = new List<int>();
        private System.Random _rand = new System.Random(911);
        private float _spawnTimer;
        private float _roadblockTimer = 20f;
        private float _arrest;
        private float _arrestContactTime = -9f;
        private float _carBustTimer;
        private float _sightTimer;
        private float _standDownTimer;

        private bool _responseActive;
        private bool _responseReady;

        private void Update()
        {
            if (Game.Wanted == null || Game.Player == null || Game.Layout == null) return;
            float dt = Time.deltaTime;
            int stars = Game.Wanted.Stars;
            Units.RemoveAll(v => v == null);
            Officers.RemoveAll(n => n == null);
            ClearHeliWreck();
            if (stars == 0)
            {
                if (_responseActive) _quietUntil04F = Time.time + CityCombatRules04F.QuietSeconds;
                _incidentSpent04F = 0;
                _incidentStart04F = -1f;
                _containUntil04F = 0f;
                _responseActive = false;
                _responseReady = false;
                _spawnTimer = 0f;
                StandDown(dt);
                return;
            }
            _standDownTimer = 0f;
            if (!Game.IsPlaying) return;
            if (!_responseActive)
            {
                _responseActive = true;
                _incidentStart04F = Time.time;
                _incidentSpent04F = 0;
                _spawnTimer = Mathf.Max(PoliceResponseRules.InitialDelay(Settings.Difficulty), _quietUntil04F - Time.time);
                if (!StoryPursuit04F && Game.Hud != null)
                    Game.Hud.Subtitle("Crystal Wardens", "Local incident. Lower your focus for surrender, or break sight and leave the search area. Reinforcements are finite.", 6f);
            }
            if (!StoryPursuit04F && Time.time - _incidentStart04F >= CityCombatRules04F.IncidentSeconds)
            {
                BeginContainment04F();
                StandDown(dt);
                return;
            }
            _spawnTimer -= dt;
            if (_spawnTimer <= 0f)
            {
                _spawnTimer = PoliceResponseRules.DispatchSeconds(Settings.Difficulty);
                _responseReady = true;
                RecycleUnits();
                RecruitPatrols(stars);
                int available = AvailableOfficers(stars);
                if (available > 0 && CountActiveUnits() < PoliceResponseRules.Cars(Settings.Difficulty, stars)) SpawnUnit(stars, available);
                available = AvailableOfficers(stars);
                if (available > 0 && !Game.Player.InVehicle) SpawnFootSquad(stars, available);
            }
            UpdateUnits(stars);
            UpdateHeli(stars);
            UpdateRoadblocks(stars, dt);
            UpdateSightings(dt);
            UpdateArrest(dt, stars);
        }

        /// <summary>Patrol cars already driving around nearby join the chase instead of ignoring it.</summary>
        private void RecruitPatrols(int stars)
        {
            if (Game.Traffic == null || !MayDispatch04F) return;
            Vector3 p = Game.PlayerPosition;
            List<Vehicle> moving = Game.Traffic.Moving;
            for (int i = moving.Count - 1; i >= 0; i--)
            {
                Vehicle v = moving[i];
                if (v == null || !v.IsPolice || v.IsDestroyed || v.HasPlayer || v.Driver == null || Units.Contains(v)) continue;
                if (v.Driver is NpcController scripted && (scripted.Persistent || !string.IsNullOrEmpty(scripted.MissionTag))) continue;
                if (MathUtil.FlatDistance(v.transform.position, p) > 150f) continue;
                TrafficDriver ai = v.GetComponent<TrafficDriver>();
                if (ai == null || !ai.enabled) continue;
                int crew = 0;
                foreach (CharacterBase occupant in v.Occupants)
                    if (occupant != null && !occupant.IsDead && !occupant.IsPlayer) crew++;
                if (!PoliceResponseRules.CanRecruit(Settings.Difficulty, stars, CountActiveUnits(), LivingOfficers(), crew) || crew > AvailableOfficers(stars)) continue;
                moving.RemoveAt(i);
                ai.Mode = DriveMode.Pursuit;
                ai.PursuitTarget = Game.Player;
                ai.IgnoreLights = true;
                v.SirenOn = true;
                v.LightsOn = true;
                Units.Add(v);
                if (!StoryPursuit04F) _incidentSpent04F += crew;
                foreach (CharacterBase c in v.Occupants)
                {
                    NpcController o = c as NpcController;
                    if (o != null && !Officers.Contains(o))
                    {
                        ArmWarden04F(o, stars, o.Role == NpcRole.Swat, Officers.Count);
                        Officers.Add(o);
                    }
                }
            }
        }

        /// <summary>
        /// Keeps the response fresh while wanted: deployed or wrecked cruisers are handed to traffic cleanup, and
        /// manned units left far behind are removed so replacements can spawn near the player.
        /// </summary>
        private void RecycleUnits()
        {
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            Vector3 p = Game.PlayerPosition;
            for (int i = Units.Count - 1; i >= 0; i--)
            {
                Vehicle v = Units[i];
                if (v == null) { Units.RemoveAt(i); continue; }
                if (v.HasPlayer)
                {
                    Units.RemoveAt(i);
                    if (Game.Traffic != null) Game.Traffic.Adopt(v);
                    continue;
                }
                if (ProtectedCrew04F(v)) continue;
                bool manned = v.Driver != null && !v.IsDestroyed;
                if (!manned)
                {
                    Units.RemoveAt(i);
                    if (Game.Traffic != null) Game.Traffic.Adopt(v);
                    continue;
                }
                float d = MathUtil.FlatDistance(v.transform.position, p);
                bool visible = cam != null && PedManager.IsVisible(cam, v.transform.position);
                if (d > 260f && !visible)
                {
                    foreach (CharacterBase c in v.Occupants) if (c != null && !c.IsPlayer) Destroy(c.gameObject);
                    Destroy(v.gameObject);
                    Units.RemoveAt(i);
                }
            }
        }

        private int CountActiveUnits()
        {
            int n = 0;
            foreach (Vehicle v in Units) if (v != null && !v.IsDestroyed && v.Driver != null && !v.Driver.IsPlayer) n++;
            return n;
        }

        private int LivingOfficers()
        {
            int n = 0;
            foreach (NpcController o in NpcController.All)
                if (o != null && !o.IsDead && (o.Role == NpcRole.Police || o.Role == NpcRole.Swat)
                    && (Officers.Contains(o) || (!o.InVehicle && MathUtil.FlatDistance(o.transform.position, Game.PlayerPosition) < 240f))) n++;
            return n;
        }

        private int AvailableOfficers(int stars)
        {
            int activeSpace = PoliceResponseRules.AvailableOfficers(Settings.Difficulty, stars, LivingOfficers());
            if (StoryPursuit04F) return activeSpace;
            if (!MayDispatch04F) return 0;
            return CityCombatRules04F.RemainingDispatch(CityCombatRules04F.IncidentOfficers(Settings.Difficulty, stars), _incidentSpent04F, activeSpace);
        }

        // ------------------------------------------------------------------ dispatch

        private void SpawnUnit(int stars, int crewLimit)
        {
            CityLayout L = Game.Layout;
            Vector3 p = Game.PlayerPosition;
            L.LanesNear(p, 190f, _lanes);
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                if (_lanes.Count == 0) break;
                LanePath lane = L.Lanes[_lanes[_rand.Next(_lanes.Count)]];
                if (lane.IsConnector || lane.Length < 16f) continue;
                float s = (float)_rand.NextDouble() * (lane.Length - 8f) + 4f;
                Vector3 pos = lane.PointAt(s);
                float d = MathUtil.FlatDistance(pos, p);
                if (d < 85f || d > 185f || !OffCameraDispatch04F(pos)) continue;
                if (cam != null && PedManager.IsVisible(cam, pos) && d < 140f && !Physics.Linecast(cam.transform.position, pos + Vector3.up, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                if (!TrafficManager.Clear(pos, 10f)) continue;
                VehicleClass cls = stars >= 4 && _rand.NextDouble() < 0.45 ? VehicleClass.Enforcer : (stars >= 2 && _rand.NextDouble() < 0.4 ? VehicleClass.PoliceSUV : VehicleClass.Police);
                SpawnPursuitCar(VehicleDb.FirstOfClass(cls), pos, MathUtil.YawOf(lane.DirectionAt(s)), stars, crewLimit);
                return;
            }
            // Desert or no lanes: spawn on the nearest road edge behind the player.
            int e = L.NearestEdge(p - (Game.Player.InVehicle ? Game.Player.CurrentVehicle.Velocity.normalized * 140f : Game.Cam.FlatForward * 140f));
            if (e >= 0)
            {
                Vector3 pos = L.ProjectOnEdge(L.Edges[e], p - Game.Cam.FlatForward * 140f);
                if (TrafficManager.Clear(pos, 10f) && MathUtil.FlatDistance(pos, p) > 70f && OffCameraDispatch04F(pos))
                    SpawnPursuitCar(VehicleDb.FirstOfClass(VehicleClass.Police), pos, MathUtil.YawOf(MathUtil.Flat(p - pos)), stars, crewLimit);
            }
        }

        /// <summary>Secondary automatic responders, including traffic bikes, must share the main dispatch budget and delay.</summary>
        public Vehicle TrySpawnAutomaticUnit(VehicleDef def, Vector3 pos, float yaw, int stars)
        {
            if (!_responseActive || !_responseReady || !Game.IsPlaying || Game.Wanted == null || Game.Wanted.Stars <= 0 || def == null) return null;
            stars = Game.Wanted.Stars;
            int available = AvailableOfficers(stars);
            if (available <= 0 || CountActiveUnits() >= PoliceResponseRules.Cars(Settings.Difficulty, stars)) return null;
            return SpawnPursuitCar(def, pos, yaw, stars, available);
        }

        /// <summary>Explicit mission spawns retain their requested crew; automatic dispatch uses a shared crew budget.</summary>
        public Vehicle SpawnPursuitCar(VehicleDef def, Vector3 pos, float yaw, int stars)
        {
            return SpawnPursuitCar(def, pos, yaw, stars, int.MaxValue);
        }

        private Vehicle SpawnPursuitCar(VehicleDef def, Vector3 pos, float yaw, int stars, int crewLimit)
        {
            Vector3 ground = TrafficManager.GroundAt(pos);
            Vehicle v = Vehicle.Spawn(def, ground, yaw, null, false);
            v.transform.SetParent(transform, true);
            v.SirenOn = true;
            v.LightsOn = true;
            TrafficDriver ai = v.gameObject.AddComponent<TrafficDriver>();
            ai.Init(v, -1, 0f);
            ai.Mode = DriveMode.Pursuit;
            ai.PursuitTarget = Game.Player;
            bool swat = def.Class == VehicleClass.Enforcer;
            int crew = Mathf.Min(crewLimit, Mathf.Min(def.Seats, swat ? 4 : 2));
            for (int i = 0; i < crew; i++)
            {
                NpcController o = NpcController.Spawn(ground + Vector3.up, yaw, swat ? NpcRole.Swat : NpcRole.Police, WardenLook04F(stars, swat, i), -1);
                ArmWarden04F(o, stars, swat, i);
                o.EnterVehicleImmediate(v, i);
                Officers.Add(o);
            }
            Units.Add(v);
            if (crewLimit != int.MaxValue && !StoryPursuit04F) _incidentSpent04F += crew;
            return v;
        }

        private WeaponId WeaponFor(int stars, bool swat)
        {
            if (swat) return _rand.NextDouble() < 0.7 ? WeaponId.ArcCarbine : WeaponId.CoilBurst;
            if (stars >= 3) return _rand.NextDouble() < 0.5 ? WeaponId.GlassShot : WeaponId.CoilBurst;
            if (stars >= 2 && _rand.NextDouble() < 0.35) return WeaponId.GlassShot;
            return WeaponId.VectorSidearm;
        }

        private void SpawnFootSquad(int stars, int available)
        {
            CityLayout L = Game.Layout;
            Vector3 p = Game.PlayerPosition;
            List<int> nodes = new List<int>();
            L.WalkNodesNear(p, 90f, nodes);
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            for (int attempt = 0; attempt < 8 && nodes.Count > 0; attempt++)
            {
                Vector3 pos = L.WalkNodes[nodes[_rand.Next(nodes.Count)]].Pos;
                float d = MathUtil.FlatDistance(pos, p);
                if (d < 45f || d > 90f) continue;
                if (cam != null && PedManager.IsVisible(cam, pos) && !Physics.Linecast(cam.transform.position, pos + Vector3.up, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                bool swat = stars >= 4;
                for (int i = 0; i < Mathf.Min(2, available); i++)
                {
                    Vector3 sp = PedManager.Ground(pos + new Vector3(i * 1.2f, 0f, 0.8f));
                    if (PedManager.ExcludedVenue04C(sp) || !OffCameraDispatch04F(sp)) continue;
                    if (Physics.CheckCapsule(sp + Vector3.up * 0.65f, sp + Vector3.up * 1.8f, 0.4f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                    NpcController o = NpcController.Spawn(sp, MathUtil.YawOf(MathUtil.Flat(p - sp)), swat ? NpcRole.Swat : NpcRole.Police, WardenLook04F(stars, swat, i), -1);
                    ArmWarden04F(o, stars, swat, i);
                    if (!StoryPursuit04F) _incidentSpent04F++;
                    o.SetState(NpcState.Chase);
                    o.transform.SetParent(transform, true);
                    Officers.Add(o);
                }
                return;
            }
        }

        private void UpdateUnits(int stars)
        {
            foreach (Vehicle v in Units)
            {
                if (v == null || v.IsDestroyed) continue;
                TrafficDriver ai = v.GetComponent<TrafficDriver>();
                if (ai == null || !ai.enabled || v.Driver == null || v.Driver.IsPlayer) continue;
                ai.Mode = DriveMode.Pursuit;
                ai.PursuitTarget = Game.Player;
                v.SirenOn = true;
                if (ai.WantsDeploy && v.Speed < 4f) Deploy(v, ai);
            }
        }

        private void Deploy(Vehicle v, TrafficDriver ai)
        {
            ai.Mode = DriveMode.Stopped;
            for (int i = v.Occupants.Length - 1; i >= 0; i--)
            {
                NpcController o = v.Occupants[i] as NpcController;
                if (o == null) continue;
                v.OpenDoor(i, true);
                o.ExitVehicleImmediate(false);
                o.SetState(NpcState.Chase);
                o.transform.SetParent(transform, true);
            }
            ai.enabled = false;
        }

        private void StandDown(float dt)
        {
            _arrest = 0f;
            if (Heli != null) Heli.Leave();
            _standDownTimer += dt;
            foreach (Vehicle v in Units)
            {
                if (v == null || ProtectedCrew04F(v)) continue;
                v.SirenOn = false;
                TrafficDriver ai = v.GetComponent<TrafficDriver>();
                if (ai != null && ai.enabled && ai.Mode == DriveMode.Pursuit)
                {
                    ai.Mode = DriveMode.Cruise;
                    ai.SnapToNearestLane();
                }
            }
            if (_standDownTimer < 3f) return;
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            Vector3 p = Game.PlayerPosition;
            for (int i = Units.Count - 1; i >= 0; i--)
            {
                Vehicle v = Units[i];
                if (v == null) { Units.RemoveAt(i); continue; }
                if (ProtectedCrew04F(v)) continue;
                if (v.HasPlayer)
                {
                    Units.RemoveAt(i);
                    if (Game.Traffic != null) Game.Traffic.Adopt(v);
                    continue;
                }
                float d = MathUtil.FlatDistance(v.transform.position, p);
                bool visible = cam != null && PedManager.IsVisible(cam, v.transform.position);
                if (d > 140f || (d > 70f && !visible))
                {
                    foreach (CharacterBase c in v.Occupants) if (c != null && !c.IsPlayer) Destroy(c.gameObject);
                    Destroy(v.gameObject);
                    Units.RemoveAt(i);
                }
            }
            for (int i = Officers.Count - 1; i >= 0; i--)
            {
                NpcController o = Officers[i];
                if (o == null) { Officers.RemoveAt(i); continue; }
                if (o.InVehicle || o.Persistent || !string.IsNullOrEmpty(o.MissionTag)) continue;
                float d = MathUtil.FlatDistance(o.transform.position, p);
                bool visible = cam != null && PedManager.IsVisible(cam, o.transform.position);
                if (o.IsDead || d > 100f || (d > 50f && !visible))
                {
                    if (!o.IsDead || d > 60f) Destroy(o.gameObject);
                    Officers.RemoveAt(i);
                }
            }
            for (int i = _roadblocks.Count - 1; i >= 0; i--)
            {
                Vehicle v = _roadblocks[i];
                if (v == null) { _roadblocks.RemoveAt(i); continue; }
                if (ProtectedCrew04F(v)) continue;
                if (MathUtil.FlatDistance(v.transform.position, p) > 120f)
                {
                    foreach (CharacterBase c in v.Occupants) if (c != null && !c.IsPlayer) Destroy(c.gameObject);
                    Destroy(v.gameObject);
                    _roadblocks.RemoveAt(i);
                }
            }
        }

        // ------------------------------------------------------------------ air unit

        /// <summary>Burnt-out helicopter wrecks are cleared once nobody is looking, whatever the wanted level.</summary>
        private void ClearHeliWreck()
        {
            if (Heli == null || !Heli.IsDestroyed || Heli.DestroyedFor <= 30f) return;
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            if (cam == null || !PedManager.IsVisible(cam, Heli.transform.position) || MathUtil.FlatDistance(Heli.transform.position, Game.PlayerPosition) > 150f)
            {
                Destroy(Heli.gameObject);
                Heli = null;
            }
        }

        private void UpdateHeli(int stars)
        {
            if (StoryPursuit04F && stars >= 3)
            {
                if (Heli == null || Heli.IsDestroyed && Heli.DestroyedFor > 40f)
                {
                    if (Heli != null && Heli.IsDestroyed) { Destroy(Heli.gameObject); Heli = null; }
                    if (Heli == null)
                    {
                        Vector3 from = Game.PlayerPosition - Game.Cam.FlatForward * 260f + Vector3.up * 90f;
                        Heli = Helicopter.Spawn(from, stars >= 4);
                        Heli.transform.SetParent(transform, true);
                    }
                }
                else if (!Heli.IsDestroyed)
                {
                    Heli.Armed = stars >= 4;
                    Heli.Pursue();
                }
            }
            else if (Heli != null && !Heli.IsDestroyed) Heli.Leave();
        }

        // ------------------------------------------------------------------ roadblocks

        private void UpdateRoadblocks(int stars, float dt)
        {
            for (int i = _roadblocks.Count - 1; i >= 0; i--)
            {
                Vehicle v = _roadblocks[i];
                if (v == null) { _roadblocks.RemoveAt(i); continue; }
                if (MathUtil.FlatDistance(v.transform.position, Game.PlayerPosition) > 260f)
                {
                    foreach (CharacterBase c in v.Occupants) if (c != null && !c.IsPlayer) Destroy(c.gameObject);
                    Destroy(v.gameObject);
                    _roadblocks.RemoveAt(i);
                }
            }
            if (!StoryPursuit04F || stars < 3 || !Game.Player.InVehicle) return;
            // Each block creates four officers. Never bypass the response budget through this secondary path.
            if (AvailableOfficers(stars) < 4) return;
            _roadblockTimer -= dt;
            if (_roadblockTimer > 0f || _roadblocks.Count >= 4) return;
            _roadblockTimer = Mathf.Lerp(30f, 18f, (stars - 3) / 2f);
            Vehicle pv = Game.Player.CurrentVehicle;
            if (pv.Speed < 12f) return;
            CityLayout L = Game.Layout;
            Vector3 ahead = pv.transform.position + pv.Velocity.normalized * Mathf.Clamp(pv.Speed * 7f, 120f, 220f);
            int e = L.NearestEdge(ahead);
            if (e < 0) return;
            RoadEdge edge = L.Edges[e];
            Vector3 c0 = L.ProjectOnEdge(edge, ahead);
            if (MathUtil.FlatDistance(c0, pv.transform.position) < 90f) return;
            Camera cam = Game.Cam.Cam;
            if (PedManager.IsVisible(cam, c0) && !Physics.Linecast(cam.transform.position, c0 + Vector3.up, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return;
            Vector3 along = (L.Nodes[edge.B].Pos - L.Nodes[edge.A].Pos).normalized;
            Vector3 across = MathUtil.RightOf(along);
            float half = edge.Width * 0.5f;
            for (int k = -1; k <= 1; k += 2)
            {
                Vector3 pos = c0 + across * k * half * 0.45f;
                Vehicle v = Vehicle.Spawn(VehicleDb.FirstOfClass(stars >= 4 ? VehicleClass.PoliceSUV : VehicleClass.Police), TrafficManager.GroundAt(pos), MathUtil.YawOf(across) + (k > 0 ? 0f : 180f) + 20f, null, false);
                v.transform.SetParent(transform, true);
                v.SirenOn = true;
                v.LightsOn = true;
                v.Handbrake = true;
                _roadblocks.Add(v);
                for (int i = 0; i < 2; i++)
                {
                    Vector3 op = TrafficManager.GroundAt(pos - along * 3.5f * (i == 0 ? 1f : -1f) + across * k * 1.5f);
                    if (PedManager.ExcludedVenue04C(op)) continue;
                    NpcController o = NpcController.Spawn(op, MathUtil.YawOf(-along), NpcRole.Police, BodyFactory.PoliceLook(), -1);
                    o.Arm(stars >= 4 ? WeaponId.ArcCarbine : WeaponId.GlassShot);
                    o.SetState(NpcState.Chase);
                    o.transform.SetParent(transform, true);
                    Officers.Add(o);
                }
            }
            if (Game.Hud != null) Game.Hud.Subtitle("Police Scanner", "Roadblock in position on " + (string.IsNullOrEmpty(edge.Name) ? "the road ahead" : edge.Name) + ".", 3f);
        }

        // ------------------------------------------------------------------ sightings and arrests

        private void UpdateSightings(float dt)
        {
            _sightTimer -= dt;
            if (_sightTimer > 0f) return;
            _sightTimer = 0.3f;
            Vector3 target = Game.Player.ChestPosition;
            foreach (Vehicle v in Vehicle.All)
            {
                if (v == null || !v.IsPolice || v.IsDestroyed || v.Driver == null || v.Driver.IsPlayer) continue;
                Vector3 eye = v.transform.position + Vector3.up * 1.6f;
                float d = Vector3.Distance(eye, target);
                if (d > 75f) continue;
                if (Game.PlayerVehicle == v) continue;
                if (!Physics.Linecast(eye, target, Layers.WorldMask, QueryTriggerInteraction.Ignore))
                {
                    Game.Wanted.ReportSightingAt(eye);
                    return;
                }
            }
        }

        public void ReportArrestContact(NpcController cop, float dt)
        {
            if (Game.Wanted == null || Game.Wanted.Stars != 1) return;
            PlayerController p = Game.Player;
            if (p == null || p.InVehicle || p.IsRagdolled || p.IsDead) return;
            if (p.Weapons.Current.Fire != FireKind.Melee || p.Aiming) return;
            if (p.FlatVelocity.magnitude > 4.5f) { _arrest = Mathf.Max(0f, _arrest - dt); return; }
            if (Time.frameCount == _lastContactFrame) return;
            _lastContactFrame = Time.frameCount;
            _arrestContactTime = Time.time;
            _arrest += dt;
            if (_arrest > 1.6f)
            {
                _arrest = 0f;
                if (Game.Root != null) Game.Root.OnPlayerBusted();
            }
        }

        private int _lastContactFrame = -1;

        private void UpdateArrest(float dt, int stars)
        {
            if (Time.time - _arrestContactTime > 0.4f) _arrest = Mathf.Max(0f, _arrest - dt * 0.5f);
            // Pulled out of a stopped car at low wanted levels.
            PlayerController p = Game.Player;
            if (stars <= 2 && p.InVehicle && p.CurrentVehicle.Speed < 0.6f)
            {
                bool copAtDoor = false;
                foreach (NpcController o in NpcController.All)
                {
                    if (o == null || o.IsDead || o.InVehicle || (o.Role != NpcRole.Police && o.Role != NpcRole.Swat)) continue;
                    if (MathUtil.FlatDistance(o.transform.position, p.CurrentVehicle.transform.position) < p.CurrentVehicle.Def.Width * 0.5f + 2.2f) { copAtDoor = true; break; }
                }
                _carBustTimer = copAtDoor ? _carBustTimer + dt : 0f;
                if (_carBustTimer > 3f)
                {
                    _carBustTimer = 0f;
                    if (Game.Root != null) Game.Root.OnPlayerBusted();
                }
            }
            else _carBustTimer = 0f;
        }

        public float ArrestProgress { get { return Mathf.Clamp01(Mathf.Max(_arrest / 1.6f, _carBustTimer / 3f)); } }

        /// <summary>Removes every police unit (after wasted/busted or when a mission needs a clean slate).</summary>
        public void ClearAll()
        {
            foreach (Vehicle v in Units)
            {
                if (v == null) continue;
                if (v.HasPlayer)
                {
                    v.SirenOn = false;
                    if (Game.Traffic != null) Game.Traffic.Adopt(v);
                    continue;
                }
                foreach (CharacterBase c in v.Occupants) if (c != null && !c.IsPlayer) Destroy(c.gameObject);
                Destroy(v.gameObject);
            }
            Units.Clear();
            foreach (Vehicle v in _roadblocks)
            {
                if (v == null) continue;
                if (v.HasPlayer)
                {
                    if (Game.Traffic != null) Game.Traffic.Adopt(v);
                    continue;
                }
                foreach (CharacterBase c in v.Occupants) if (c != null && !c.IsPlayer) Destroy(c.gameObject);
                Destroy(v.gameObject);
            }
            _roadblocks.Clear();
            foreach (NpcController o in Officers) if (o != null) Destroy(o.gameObject);
            Officers.Clear();
            for (int i = NpcController.All.Count - 1; i >= 0; i--)
            {
                NpcController n = NpcController.All[i];
                if (n != null && !n.Persistent && !n.InVehicle && (n.Role == NpcRole.Police || n.Role == NpcRole.Swat) && n.State != NpcState.Wander) Destroy(n.gameObject);
            }
            if (Heli != null) Destroy(Heli.gameObject);
            Heli = null;
            _arrest = 0f;
            _carBustTimer = 0f;
            _responseActive = false;
            _responseReady = false;
            _spawnTimer = 0f;
            _roadblockTimer = 20f;
            _standDownTimer = 0f;
            _incidentSpent04F = 0;
            _incidentStart04F = -1f;
            _containUntil04F = 0f;
            _quietUntil04F = 0f;
        }
    }
}
