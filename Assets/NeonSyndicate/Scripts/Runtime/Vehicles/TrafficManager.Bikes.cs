using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Hover bikes in the world: a bike parked at every safehouse (so Free Roam starts beside one), a bike beside the
    /// player when a saved game past the prologue loads, LVPD bikes joining pursuits now and then, the player's own bike
    /// (kept from cleanup while it is within reach) and the phone's "Bring my bike" courier. Traffic and parked bikes
    /// come from the normal civilian picks (VehicleDb spawn weights). Nothing here is saved: no SaveData fields.
    /// </summary>
    public sealed partial class TrafficManager
    {
        /// <summary>The player's own bike is never cleaned up while it is closer than this (meters).</summary>
        public const float KeepBikeRange = 900f;

        /// <summary>Seconds between two "Bring my bike" deliveries.</summary>
        public const float BringCooldown = 30f;

        /// <summary>The bike the player rode last (Bring my bike returns it). May be null.</summary>
        public Vehicle PlayerBike { get; private set; }

        private VehicleDef _playerBikeDef;
        private Color _playerBikePaint;
        private float _bringReadyAt = -999f;
        private Blip _bringBlip;
        private float _bringBlipUntil;
        private Vehicle[] _homeBikes;
        private Vector3[] _homeSpots;
        private float[] _homeYaws;
        private float[] _homeRetryAt;
        private float _homeTimer;
        private float _policeBikeTimer = 5f;
        private float _freshUntil;
        private bool _needStartBike;
        private readonly List<int> _bikeLanes = new List<int>();

        /// <summary>Remembers the bike the player just got on as their own bike (model and paint survive its cleanup).</summary>
        public void NoteRiddenBike(Vehicle v)
        {
            if (v == null || v.Def == null || !v.Def.IsBike) return;
            PlayerBike = v;
            _playerBikeDef = v.Def;
            _playerBikePaint = v.PaintColor;
        }

        private bool KeepsBike(Vehicle v, Vector3 p)
        {
            return v != null && v == PlayerBike && !v.IsDestroyed && MathUtil.FlatDistance(v.transform.position, p) < KeepBikeRange;
        }

        private void UpdateBikes(Vector3 p, float dt)
        {
            if (_bringBlip != null && (Time.unscaledTime > _bringBlipUntil || PlayerBike == null || PlayerBike.HasPlayer))
            {
                Blip.Remove(_bringBlip);
                _bringBlip = null;
            }
            _homeTimer -= dt;
            if (_homeTimer <= 0f)
            {
                _homeTimer = 0.5f;
                UpdateHomeBikes(p);
            }
            if (!Suppressed) UpdatePoliceBikes(dt);
            if (_needStartBike && Game.IsPlaying) StartBike(p);
        }

        /// <summary>
        /// Once per session, right after it starts: past the prologue (or in Free Roam) the player always has a bike
        /// within reach, so a loaded save away from the safehouses gets one parked beside the player.
        /// </summary>
        private void StartBike(Vector3 p)
        {
            _needStartBike = false;
            bool earned = (Game.Root != null && Game.Root.FreeRoam) || (Game.Missions != null && Game.Missions.StoryProgress >= 1);
            if (!earned || Game.Player.InVehicle) return;
            // A safehouse start gets the safehouse bike (spawned now, not on its next half-second check).
            UpdateHomeBikes(p);
            for (int i = 0; i < Vehicle.All.Count; i++)
            {
                Vehicle o = Vehicle.All[i];
                if (o != null && o.Def.IsBike && !o.IsDestroyed && o.Driver == null && MathUtil.FlatDistance(o.transform.position, p) < 40f) return;
            }
            Vector3 right = Vector3.Cross(Vector3.up, MathUtil.Flat(Game.Player.transform.forward).normalized);
            float yaw = Game.Player.transform.eulerAngles.y;
            Vector3 spot;
            if (!BikeSpots.FindClear(p + right * 2.5f, yaw, out spot)) return;
            Vehicle bike = Vehicle.Spawn(VehicleDb.Find(VehicleDb.SportBikeName) ?? VehicleDb.CourierBike, spot, yaw, null, true);
            bike.transform.SetParent(transform, true);
            bike.Parked = false;
            bike.Locked = false;
            bike.StolenByPlayer = true;
            ParkedCars.Add(bike);
            NoteRiddenBike(bike);
        }

        private void ClearBikes()
        {
            if (_homeBikes != null)
            {
                for (int i = 0; i < _homeBikes.Length; i++)
                {
                    if (_homeBikes[i] != null) DestroyVehicle(_homeBikes[i]);
                    _homeBikes[i] = null;
                    _homeRetryAt[i] = 0f;
                }
            }
            PlayerBike = null;
            _playerBikeDef = null;
            _bringReadyAt = -999f;
            if (_bringBlip != null)
            {
                Blip.Remove(_bringBlip);
                _bringBlip = null;
            }
            // A new session starts behind a black fade, so the home bike may appear in view for a few seconds.
            _freshUntil = Time.unscaledTime + 5f;
            _needStartBike = true;
            _policeBikeTimer = 5f;
        }

        // ------------------------------------------------------------------ safehouse bikes

        private void UpdateHomeBikes(Vector3 p)
        {
            CityLayout L = Game.Layout;
            if (_homeBikes == null)
            {
                List<Poi> homes = new List<Poi>();
                foreach (Poi poi in L.Pois) if (poi.Type == PoiType.Safehouse) homes.Add(poi);
                int n = homes.Count;
                _homeBikes = new Vehicle[n];
                _homeSpots = new Vector3[n];
                _homeYaws = new float[n];
                _homeRetryAt = new float[n];
                for (int i = 0; i < n; i++)
                {
                    // Beside the doorstep where the player appears (GameRoot spawns 3.5 m out from the door), along the facade.
                    Vector3 f = homes[i].Facing.sqrMagnitude > 0.01f ? homes[i].Facing.normalized : Vector3.forward;
                    Vector3 right = Vector3.Cross(Vector3.up, f);
                    _homeSpots[i] = homes[i].Position + f * 4.2f + right * 2.6f;
                    _homeYaws[i] = MathUtil.YawOf(f) + 90f;
                }
            }
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            bool fresh = Time.unscaledTime < _freshUntil;
            for (int i = 0; i < _homeBikes.Length; i++)
            {
                Vehicle v = _homeBikes[i];
                float d = MathUtil.FlatDistance(_homeSpots[i], p);
                if (v != null)
                {
                    bool taken = v.IsDestroyed || v.Driver != null || MathUtil.FlatDistance(v.transform.position, _homeSpots[i]) > 6f;
                    if (taken)
                    {
                        // Ridden off (or wrecked): it becomes an ordinary parked bike, and a fresh one turns up later out of sight.
                        if (!ParkedCars.Contains(v) && !Moving.Contains(v)) ParkedCars.Add(v);
                        _homeBikes[i] = null;
                        _homeRetryAt[i] = Time.unscaledTime + 45f;
                    }
                    else if (d > 260f && (cam == null || !PedManager.IsVisible(cam, v.transform.position)))
                    {
                        DestroyVehicle(v);
                        _homeBikes[i] = null;
                    }
                    continue;
                }
                if (d > 120f || Time.unscaledTime < _homeRetryAt[i]) continue;
                if (!fresh && cam != null && PedManager.IsVisible(cam, _homeSpots[i]) && !Occluded(cam.transform.position, _homeSpots[i])) continue;
                Vector3 spot;
                if (!BikeSpots.FindClear(_homeSpots[i], _homeYaws[i], out spot))
                {
                    _homeRetryAt[i] = Time.unscaledTime + 10f;
                    continue;
                }
                VehicleDef def = VehicleDb.Find(i % 2 == 0 ? VehicleDb.SportBikeName : VehicleDb.CruiserBikeName) ?? VehicleDb.CourierBike;
                Vehicle bike = Vehicle.Spawn(def, spot, _homeYaws[i], null, true);
                bike.transform.SetParent(transform, true);
                // The safehouse bike is already the player's: no lock, no alarm, no theft.
                bike.Parked = false;
                bike.Locked = false;
                bike.StolenByPlayer = true;
                _homeBikes[i] = bike;
            }
        }

        // ------------------------------------------------------------------ police bikes

        private void UpdatePoliceBikes(float dt)
        {
            _policeBikeTimer -= dt;
            if (_policeBikeTimer > 0f) return;
            _policeBikeTimer = 6f;
            if (Game.Police == null || Game.Wanted == null || Game.Player == null) return;
            int stars = Game.Wanted.Stars;
            if (stars < 1) return;
            int bikes = 0;
            foreach (Vehicle u in Game.Police.Units) if (u != null && !u.IsDestroyed && u.Def.IsBike) bikes++;
            if (bikes >= (stars >= 3 ? 2 : 1) || _rand.NextDouble() > 0.35) return;
            Vector3 p = Game.PlayerPosition;
            if (CityLayout.IsWilderness(CityLayout.DistrictAt(p))) return; // the bike squad works the city streets
            CityLayout L = Game.Layout;
            L.LanesNear(p, 190f, _bikeLanes);
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            for (int attempt = 0; attempt < 8 && _bikeLanes.Count > 0; attempt++)
            {
                LanePath lane = L.Lanes[_bikeLanes[_rand.Next(_bikeLanes.Count)]];
                if (lane.IsConnector || lane.Length < 16f) continue;
                float s = (float)_rand.NextDouble() * (lane.Length - 8f) + 4f;
                Vector3 pos = lane.PointAt(s);
                float d = MathUtil.FlatDistance(pos, p);
                if (d < 85f || d > 185f) continue;
                if (cam != null && PedManager.IsVisible(cam, pos) && !Occluded(cam.transform.position, pos)) continue;
                if (!Clear(pos, 10f)) continue;
                Game.Police.TrySpawnAutomaticUnit(VehicleDb.PoliceBike, pos, MathUtil.YawOf(lane.DirectionAt(s)), stars);
                return;
            }
        }

        // ------------------------------------------------------------------ Bring my bike

        /// <summary>
        /// The phone's "Bring my bike": delivers the player's last bike (or a courier bike) to a curb or road spot 20 to
        /// 40 m away, marks it on the map for a while, and starts a short cooldown. Returns the line the courier says.
        /// </summary>
        public string BringPlayerBike()
        {
            PlayerController pl = Game.Player;
            if (pl == null || pl.IsDead || Game.Layout == null) return "Can't reach you right now.";
            if (pl.InVehicle) return "You're already riding something.";
            MissionContext m = Game.Missions != null ? Game.Missions.Active : null;
            if (m != null && m.ForbidVehicles) return "Not during this job. Call me when it's done.";
            Vector3 here = pl.transform.position;
            Vehicle near = PlayerBike;
            if (near != null && !near.IsDestroyed && near.OccupantCount() == 0 && MathUtil.FlatDistance(near.transform.position, here) < 40f)
            {
                // Already close: just point at it.
                MarkPlayerBike(near);
                return "Your " + near.Def.Name + " is right there, " + Mathf.RoundToInt(MathUtil.FlatDistance(near.transform.position, here)) + " m away. Blue marker.";
            }
            float wait = _bringReadyAt - Time.unscaledTime;
            if (wait > 0f) return "Still on my way back from the last drop. Try again in " + Mathf.CeilToInt(wait) + " seconds.";
            Vector3 p = here;
            Vector3 spot;
            float yaw;
            if (!FindDropSpot(p, out spot, out yaw)) return "I can't get a bike to you here. Try somewhere more open.";
            Vehicle v = PlayerBike;
            if (v != null && (v.IsDestroyed || v.OccupantCount() > 0)) v = null;
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
            if (v != null)
            {
                int parking;
                if (_parkingOf.TryGetValue(v, out parking))
                {
                    _usedParking.Remove(parking);
                    _parkingOf.Remove(v);
                }
                Vector3 at = spot + Vector3.up * 0.05f;
                v.Body.linearVelocity = Vector3.zero;
                v.Body.angularVelocity = Vector3.zero;
                v.Body.position = at;
                v.Body.rotation = rot;
                v.transform.SetPositionAndRotation(at, rot);
                v.Body.WakeUp();
                Physics.SyncTransforms();
                if (!ParkedCars.Contains(v) && !Moving.Contains(v)) ParkedCars.Add(v);
            }
            else
            {
                VehicleDef def = _playerBikeDef ?? VehicleDb.CourierBike;
                v = Vehicle.Spawn(def, spot, yaw, _playerBikeDef != null ? _playerBikePaint : (Color?)null, true);
                v.transform.SetParent(transform, true);
                ParkedCars.Add(v);
                NoteRiddenBike(v);
            }
            // A delivered bike is yours: no theft, no alarm.
            v.Parked = false;
            v.Locked = false;
            v.StolenByPlayer = true;
            PlayerBike = v;
            _bringReadyAt = Time.unscaledTime + BringCooldown;
            if (Game.Fx != null) Game.Fx.Flash(spot + Vector3.up * 0.6f, new Color(0.3f, 0.8f, 1f), 3f, 8f, 0.4f);
            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.Whoosh, spot, 0.8f, 1.1f, 60f);
            MarkPlayerBike(v);
            float dist = MathUtil.FlatDistance(spot, p);
            return "Your " + v.Def.Name + " is parked " + Mathf.RoundToInt(dist) + " m away. Follow the blue marker.";
        }

        /// <summary>Puts a blue "B" marker on the player's bike for 40 seconds (or until they get on).</summary>
        private void MarkPlayerBike(Vehicle v)
        {
            if (_bringBlip != null) Blip.Remove(_bringBlip);
            _bringBlip = Blip.Add(BlipKind.VehicleTarget, v.transform, v.transform.position, new Color(0.3f, 0.7f, 1f), "B", "Your bike", true);
            _bringBlipUntil = Time.unscaledTime + 40f;
        }

        /// <summary>A free curbside spot 18 to 42 m away, else the edge of a nearby lane, else open ground about 22 m off.</summary>
        private bool FindDropSpot(Vector3 p, out Vector3 spot, out float yaw)
        {
            CityLayout L = Game.Layout;
            spot = p;
            yaw = 0f;
            float best = float.MaxValue;
            for (int i = 0; i < L.ParkingSpots.Count; i++)
            {
                Vector3 c = L.ParkingSpots[i];
                float d = MathUtil.FlatDistance(c, p);
                if (d < 18f || d > 42f || Mathf.Abs(c.y - p.y) > 5f) continue;
                if (_usedParking.Contains(i) || !Clear(c, 3.5f)) continue;
                float score = Mathf.Abs(d - 26f);
                if (score < best)
                {
                    best = score;
                    spot = c;
                    yaw = L.ParkingYaws[i];
                }
            }
            if (best < float.MaxValue)
            {
                spot = GroundAt(spot);
                return true;
            }
            L.LanesNear(p, 60f, _bikeLanes);
            foreach (int id in _bikeLanes)
            {
                LanePath lane = L.Lanes[id];
                if (lane.IsConnector) continue;
                for (float s = 2f; s < lane.Length - 2f; s += 3f)
                {
                    Vector3 c = lane.PointAt(s);
                    float d = MathUtil.FlatDistance(c, p);
                    if (d < 18f || d > 42f || Mathf.Abs(c.y - p.y) > 5f) continue;
                    Vector3 dir = lane.DirectionAt(s);
                    Vector3 edge = c + Vector3.Cross(Vector3.up, dir).normalized * 1.1f;
                    if (!Clear(edge, 3.5f)) continue;
                    float score = Mathf.Abs(d - 26f) + 2f;
                    if (score < best)
                    {
                        best = score;
                        spot = edge;
                        yaw = MathUtil.YawOf(dir);
                    }
                }
            }
            if (best < float.MaxValue)
            {
                spot = GroundAt(spot);
                return true;
            }
            // Off the roads (desert, snow): open ground about 22 m away, starting where the camera looks.
            Vector3 fwd = Game.Cam != null ? Game.Cam.FlatForward : MathUtil.Flat(Game.Player.transform.forward);
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            for (int k = 0; k < 8; k++)
            {
                Vector3 dir = Quaternion.Euler(0f, k * 45f, 0f) * fwd.normalized;
                Vector3 want = p + dir * 22f;
                if (L.IsInsideBuilding(want, 1f)) continue;
                Vector3 found;
                if (BikeSpots.FindClear(want, MathUtil.YawOf(dir) + 90f, out found) && Mathf.Abs(found.y - p.y) < 6f)
                {
                    spot = found;
                    yaw = MathUtil.YawOf(dir) + 90f;
                    return true;
                }
            }
            return false;
        }
    }
}
