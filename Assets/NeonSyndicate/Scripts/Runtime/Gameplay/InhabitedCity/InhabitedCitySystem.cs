using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>In-place inhabited parcels. One city/player/save registry; physical entrances never depend on a loading menu.</summary>
    public sealed partial class InhabitedCitySystem : MonoBehaviour, IWorldSystem
    {
        private readonly List<VenueBinding04C> _venues = new List<VenueBinding04C>();
        private readonly List<Blip> _placeBlips = new List<Blip>();
        private CityLayout _layout;
        private InhabitedResidents04C _residents;
        private bool _operational, _living, _ambience;
        private float _contextClock;
        private string _occupiedId = "";
        public InhabitedCityBook Book { get; private set; }
        public IReadOnlyList<VenueBinding04C> Venues { get { return _venues; } }
        public bool EnabledAtBoot { get; private set; }
        public bool IsOperational { get { return _operational && EnabledAtBoot && isActiveAndEnabled; } }
        public bool Inside { get { return !string.IsNullOrEmpty(OccupiedId); } }
        public string OccupiedId { get { return IsOperational ? _occupiedId : ""; } }
        public bool Roofed { get; private set; }
        public bool AmbienceAtBoot { get { return _ambience; } }
        public bool ResidentsAtBoot { get { return _living; } }
        public int ResidentCount { get { return _residents == null ? 0 : _residents.ResidentCount; } }

        /// <summary>Prepare before CityBuilder emits any selected building geometry.</summary>
        public void Prepare(CityLayout layout)
        {
            _layout = layout;
            TextAsset resource = Resources.Load<TextAsset>("InhabitedCity04C/venues");
            if (resource == null) throw new InvalidOperationException("04C venue catalog unavailable; original city retained.");
            VenueCatalog04C catalog = JsonUtility.FromJson<VenueCatalog04C>(resource.text);
            _venues.AddRange(InhabitedCityBindings04C.Select(layout, catalog));
            List<string> ids = new List<string>(); foreach (VenueBinding04C venue in _venues) ids.Add(venue.Definition.Id);
            Book = new InhabitedCityBook(ids);
            EnabledAtBoot = InhabitedCitySettings04C.Enabled;
            _living = InhabitedCitySettings04C.Residents; _ambience = InhabitedCitySettings04C.Ambience;
            WorldSystems.Register(this);
            if (!EnabledAtBoot) foreach (VenueBinding04C venue in _venues) venue.Status = "04C disabled at city boot; original building/service retained.";
        }

        /// <summary>Called before original solid building emission. A failed staged replacement leaves the old path intact.</summary>
        public bool TryBuildVenue(Building building, Lot lot, Transform parent)
        {
            if (!EnabledAtBoot || Book == null) return false;
            foreach (VenueBinding04C venue in _venues)
            {
                if (venue.Building != building || venue.Lot != lot) continue;
                try
                {
                    venue.View = ArtInhabitedCity04C.Build(venue, parent);
                    venue.Status = "Built in place; walk-through entrance. Native acceptance pending.";
                    Log.Info("INHABITED_CITY_04C_BUILD " + venue.Definition.Id + " origin=" + venue.Origin.ToString("F3") + " sourceBuilding=" + building.Seed);
                    return true;
                }
                catch (Exception error)
                {
                    venue.View = null; venue.Status = "Skipped: " + error.Message + "; original building and service retained.";
                    Log.Exception("04C optional venue " + venue.Definition.Id, error); return false;
                }
            }
            return false;
        }

        /// <summary>Called after the original services, quests and territory systems exist.</summary>
        public void ActivateWorld()
        {
            if (Book == null || _operational) return;
            _operational = true;
            InitUi();
            if (!EnabledAtBoot) { Log.Info(Summary()); return; }
            Physics.SyncTransforms();
            foreach (VenueBinding04C venue in _venues)
            {
                if (!venue.Available) continue;
                Blip marker = Blip.Add(BlipKind.Service, null, venue.Entrance, new Color(.75f,.85f,.62f), "D", venue.Definition.Title + " / walk-in", false);
                _placeBlips.Add(marker);
            }
            InitActivities();
            _residents = gameObject.AddComponent<InhabitedResidents04C>();
            _residents.Init(this);
            Log.Info(Summary());
        }
        public VenueBinding04C FindVenue(string id)
        {
            foreach (VenueBinding04C venue in _venues) if (venue.Definition.Id == id) return venue;
            return null;
        }
        public bool IsInsideVenue(string id)
        {
            VenueBinding04C venue = FindVenue(id);
            return venue != null && venue.Available && Game.Player != null && (Game.Inner == null || !Game.Inner.IsInner) && venue.View.Contains(Game.PlayerPosition);
        }
        public bool ContainsVenueFootprint(Vector3 position)
        {
            foreach (VenueBinding04C venue in _venues)
                if (venue.Available && venue.Building.Footprint.Contains(position)) return true;
            return false;
        }
        public bool ReplacesBuilding(Building building)
        {
            foreach (VenueBinding04C venue in _venues)
                if (venue.Available && venue.Building == building) return true;
            return false;
        }
        public bool TryGetServiceHost(Poi poi, out Vector3 position)
        {
            position = Vector3.zero;
            if (poi == null) return false;
            foreach (VenueBinding04C venue in _venues)
                if (venue.Available && venue.Poi == poi) { position = venue.Point("service"); return true; }
            return false;
        }
        public bool CanUseService(Poi poi, Vector3 target)
        {
            foreach (VenueBinding04C venue in _venues)
                if (venue.Available && venue.Poi == poi) return CanUseAt(venue.Definition.Id, target);
            return false;
        }
        /// <summary>Interaction authorization is checked again on activation. No global pursuit or mission state is cleared.</summary>
        public bool CanUseAt(string id, Vector3 target)
        {
            VenueBinding04C venue = FindVenue(id);
            PlayerController player = Game.Player;
            if (venue == null || !venue.Available || player == null || player.IsDead || player.IsRagdolled || !Game.IsPlaying ||
                (Game.Inner != null && Game.Inner.IsInner) || !venue.View.Contains(Game.PlayerPosition)) return false;
            if (player.InVehicle) return id == "garage" && player.Seat == 0 && MathUtil.FlatSqrDistance(Game.PlayerPosition, target) < 4.2f * 4.2f;
            if (!player.IsGroundedPublic || player.Climbing || player.TravelState == TravelMode.Flight || Mathf.Abs(Game.PlayerPosition.y - target.y) > 1.4f ||
                MathUtil.FlatSqrDistance(Game.PlayerPosition, target) > 3f * 3f) return false;
            Vector3 from = Game.PlayerPosition + Vector3.up * 1.25f;
            Vector3 to = target + Vector3.up * 1.25f;
            return !Physics.Linecast(from, to, Layers.WorldMask, QueryTriggerInteraction.Ignore);
        }
        public void TrackVenue(string id)
        {
            VenueBinding04C venue = FindVenue(id);
            if (venue == null || !venue.Available) { Note("That venue did not load; its original parcel remains."); return; }
            if (Game.Gps != null) Game.Gps.SetWaypoint(venue.Outside);
            Note(venue.Definition.Title + ": follow the entrance waypoint, then walk through the open doorway.");
        }
        private void Update()
        {
            if (!_operational) return;
            if (_residents != null) _residents.Tick(Time.deltaTime);
            TickActivities(Time.deltaTime);
            if (!Game.IsPlaying) return;
            _contextClock -= Time.deltaTime;
            if (_contextClock > 0f) return;
            _contextClock = .1f; RefreshContext();
        }
        private void RefreshContext()
        {
            _occupiedId = ""; Roofed = false;
            VenueBinding04C home = FindVenue("lofts");
            if (home != null && home.Available)
                home.View.SetProgress(Game.Territories == null || Game.Territories.Book == null ? 0 : Game.Territories.Book.OwnedCount,
                    Game.Quests != null && Game.Quests.IsDone("quiet_signal_05_signal_restored"));
            if (!EnabledAtBoot || Game.Player == null || Game.Player.IsDead || (Game.Inner != null && Game.Inner.IsInner)) return;
            foreach (VenueBinding04C venue in _venues)
            {
                if (!venue.Available || !venue.View.Contains(Game.PlayerPosition)) continue;
                _occupiedId = venue.Definition.Id; Book.Visit(_occupiedId); Roofed = venue.View.IsRoofed(Game.PlayerPosition); break;
            }
        }
        public string Summary()
        {
            int bound = 0, available = 0;
            foreach (VenueBinding04C venue in _venues) { if (venue.Building != null) bound++; if (venue.Available) available++; }
            return "INHABITED_CITY_04C bound=" + bound + "/" + _venues.Count + " available=" + available + " loaded=" + available +
                " occupied=" + (Inside ? 1 : 0) + " residents=" + ResidentCount + " skipped=" + (_venues.Count - available) +
                " / critical shell collision stays resident; optional population streams";
        }
        private void Note(string message, float seconds = 5f) { if (Game.Hud != null) Game.Hud.Notify(message, seconds); }

        /// <summary>Inside saves resume outside the same venue. No unvalidated interior world coordinate is serialized.</summary>
        public void Capture(SaveData save)
        {
            if (Book == null || save == null) return;
            RefreshContext(); Book.SetResume(_occupiedId); save.InhabitedCity = Book.Capture();
            Vector3 point; float yaw;
            if (Inside && TryResolveSavedExterior(save, out point, out yaw))
            { save.PosX = point.x; save.PosY = point.y; save.PosZ = point.z; save.Yaw = yaw; save.AtSafehouse = false; }
        }
        public bool TryResolveSavedExterior(SaveData save, out Vector3 point, out float yaw)
        {
            point = Vector3.zero; yaw = 0f;
            VenueBinding04C venue = save == null || save.InhabitedCity == null ? null : FindVenue(save.InhabitedCity.ResumeVenue);
            if (venue == null || venue.Building == null) return false;
            Vector3 right = new Vector3(venue.Outward.z, 0f, -venue.Outward.x);
            foreach (float forward in new[] { 0f, 1.25f, 2.5f })
            foreach (float side in new[] { 0f, -2f, 2f, -4f, 4f })
            {
                Vector3 candidate = venue.Outside + right * side + venue.Outward * forward;
                if (_layout.IsInsideBuilding(candidate, .5f) || _layout.RoadAt(candidate, .65f) != null) continue;
                RaycastHit hit;
                if (!Physics.Raycast(candidate + Vector3.up * .8f, Vector3.down, out hit, 1.6f, Layers.WorldMask, QueryTriggerInteraction.Ignore) ||
                    Mathf.Abs(hit.point.y - CityLayout.CurbHeight) > .45f || hit.normal.y < .9f) continue;
                int mask = Layers.WorldMask | (1 << Layers.Characters) | (1 << Layers.Vehicles) | (1 << Layers.VehicleShell) | (1 << Layers.Ragdoll);
                if (Physics.CheckCapsule(hit.point + Vector3.up * .5f, hit.point + Vector3.up * 1.75f, .4f, mask, QueryTriggerInteraction.Ignore)) continue;
                point = hit.point; yaw = venue.Yaw; return true;
            }
            return false;
        }
        public void Restore(SaveData save)
        {
            ClearChapterActivity(); if (_residents != null) _residents.ResetForSession();
            if (Book != null) Book.Restore(save == null ? null : save.InhabitedCity);
            _occupiedId = ""; Roofed = false;
        }
        public void ResetAll() { Restore(null); }
        private void OnEnable()
        {
            if (_operational && EnabledAtBoot) InitActivities();
        }
        private void OnDisable()
        {
            ClearActivities(); if (_residents != null) _residents.ClearPopulation();
            _occupiedId = ""; Roofed = false;
            // Keep in-place walls, floors and open exits. Disabling a behaviour never removes an occupied room.
        }
        private void OnDestroy()
        {
            ClearActivities(); ClearUi();
            foreach (Blip marker in _placeBlips) Blip.Remove(marker); _placeBlips.Clear();
            if (Game.Inhabited == this) Game.Inhabited = null;
        }
    }
}
