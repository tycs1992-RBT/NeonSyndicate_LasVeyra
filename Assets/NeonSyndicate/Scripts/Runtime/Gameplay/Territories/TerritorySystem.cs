using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Optional, additive living territory layer. Saves remain registered even with every visual switch off.</summary>
    public sealed partial class TerritorySystem : MonoBehaviour, IWorldSystem
    {
        private sealed class Site
        {
            public string Id, Title, Region, Kind, Note;
            public TerritoryDefinition Definition;
            public Vector3 Origin;
            public TerritoryArt04BView Art;
            public Blip Marker;
            public bool Placed { get { return Art != null; } }
            public Vector3 Core { get { return Origin + (Art == null ? Vector3.zero : Art.CoreLocal); } }
        }
        private readonly List<Site> _sites = new List<Site>();
        private CityLayout _layout;
        private TerritoryCatalog _catalog;
        private TerritoryEcology04B _ecology;
        private bool _ready, _built, _enabled, _relays, _living, _events, _ambience;
        private float _visibilityClock;
        public TerritoryBook Book { get; private set; }
        /// <summary>Capture combat only; WorldEvents owns its separate activity lifetime.</summary>
        public bool HasActiveEncounter { get { return Book != null && Book.ActiveId != null; } }
        public void Init(CityLayout layout)
        {
            if (_ready) return;
            _layout = layout;
            TextAsset data = Resources.Load<TextAsset>("LivingTerritories04B/territories_catalog");
            if (data == null) throw new InvalidOperationException("04B territory catalog missing.");
            _catalog = JsonUtility.FromJson<TerritoryCatalog>(data.text);
            Book = new TerritoryBook(_catalog);
            foreach (TerritoryDefinition definition in _catalog.sites)
            {
                if (MaterialDb.Get(definition.rewardMaterial) == null) throw new InvalidOperationException("04B reward material unavailable.");
                foreach (string species in definition.guards)
                {
                    CreatureSpecies type = CreatureSpeciesDb.Get(species);
                    if (type == null || type.IsHumanoid || type.Boss) throw new InvalidOperationException("04B defender unavailable: " + species);
                }
                _sites.Add(new Site { Id = definition.id, Title = definition.title, Region = definition.region,
                    Kind = definition.hub ? "hub" : "relay", Definition = definition, Note = "Awaiting safe plot survey." });
            }
            _enabled = TerritorySettings04B.Enabled; _relays = TerritorySettings04B.Relays;
            _living = TerritorySettings04B.Ecology; _events = TerritorySettings04B.Events; _ambience = TerritorySettings04B.Ambience;
            _ecology = gameObject.AddComponent<TerritoryEcology04B>(); _ecology.Init();
            WorldSystems.Register(this); InitUi();
            LivingTerritories04BQa.EncounterActive = delegate { return HasActiveEncounter || (Game.Events != null && Game.Events.TerritoryEventActive); };
            LivingTerritories04BQa.StateSummary = UiSummary;
            _ready = true;
        }
        /// <summary>Runs after 02I forts and 04A courts, before save restoration and player spawning.</summary>
        public IEnumerator BuildWorld()
        {
            if (!_ready || _built) yield break;
            _built = true;
            if (!_enabled) { foreach (Site site in _sites) site.Note = "04B disabled at city boot; saved progress retained."; yield break; }
            Physics.SyncTransforms();
            List<Vector3> reserved = new List<Vector3>();
            if (Game.Strongholds != null)
                foreach (string id in new[] { "hearthspire", "galehook", "sunscar" })
                { Vector3 point; if (Game.Strongholds.TryGetVisualAnchor(id, out point)) reserved.Add(point); }
            // Reserve the complete 04A plots, including their non-colliding planting and flight approaches.
            foreach (VisualRebirth04ACourtMarker marker in UnityEngine.Object.FindObjectsByType<VisualRebirth04ACourtMarker>(FindObjectsSortMode.None))
                if (marker.transform.parent != null) reserved.Add(marker.transform.parent.position);
            if (_relays)
            {
                foreach (Site site in _sites)
                    yield return BuildSite(site, site.Definition.anchor[0], site.Definition.anchor[2], site.Definition.layout, site.Definition.variant, reserved);
            }
            else foreach (Site site in _sites) site.Note = "Relay layer disabled at city boot; saved progress retained.";
            string[] habitatIds = { "neutral_sanctuary", "canopy_observation", "rift_nesting", "transitional_roaming" };
            string[] habitatTitles = { "Rootwater Sanctuary", "Canopy Observation Grove", "Shatterworks Rift Nest", "Wayfarer Transition Grove" };
            string[] habitatLayouts = { "habitat_sanctuary", "habitat_canopy", "habitat_rift", "habitat_transition" };
            string[] habitatRegions = { "verdant", "verdant", "obsidian", "amethyst" };
            Vector3[] anchors = { new Vector3(-1400,0,-100), new Vector3(-1380,0,420), new Vector3(460,0,-1210), new Vector3(1360,0,1200) };
            if (_living)
                for (int i = 0; i < habitatIds.Length; i++)
                {
                    Site site = new Site { Id = habitatIds[i], Title = habitatTitles[i], Region = habitatRegions[i], Kind = "habitat" };
                    _sites.Add(site);
                    yield return BuildSite(site, anchors[i].x, anchors[i].z, habitatLayouts[i], 0, reserved);
                    if (site.Placed) { _ecology.RegisterHabitat(site.Id, site.Origin, site.Art.Root); AddCareInteraction(site); }
                }
            try { BuildHome(); } catch (Exception error) { _homeNote = "Optional home station failed; previous home preserved."; Log.Exception("04B home", error); }
            RefreshViews();
            int placed = 0; foreach (Site site in _sites) if (site.Placed) placed++;
            Log.Info("04B territories: " + placed + "/" + _sites.Count + " installed. See TERRITORIES for actual placements and skipped sites.");
        }
        private IEnumerator BuildSite(Site site, float x, float z, string layoutId, int variant, List<Vector3> reserved)
        {
            Vector3 ground = Vector3.zero; bool found = false; int examined = 0;
            foreach (Vector3 point in TerritoryPlacement.Candidates(x, z))
            {
                examined++;
                try { found = TerritoryPlacement.Probe(_layout, point, reserved, out ground); }
                catch (Exception error) { site.Note = "Plot survey failed; existing world retained."; Log.Exception("04B survey " + site.Id, error); break; }
                if (found) break;
                if (examined % 16 == 0) yield return null;
            }
            if (!found)
            {
                if (site.Note == null || site.Note.StartsWith("Awaiting", StringComparison.Ordinal)) site.Note = "Skipped: no safe plot among " + examined + " candidates.";
                Log.Info("04B_SITE " + site.Id + " SKIPPED " + site.Note); yield break;
            }
            try
            {
                site.Art = ArtTerritories04B.Build(transform, ground, layoutId, variant); site.Origin = ground;
                reserved.Add(ground); site.Note = "Placed after " + examined + " candidate checks.";
                site.Marker = Blip.Add(BlipKind.Activity, site.Art.Root, ground, RegionColor(site.Region), site.Kind == "habitat" ? "H" : "T", site.Title, false);
                LivingTerritories04BQa.RegisterSite(site.Id, site.Title, site.Kind, site.Art.Root);
                if (site.Definition != null)
                {
                    AddRelayInteractions(site);
                    if (_living && site.Definition.hub) _ecology.RegisterStaff(site.Id, site.Region, ground, site.Art.Root);
                }
                if (_ambience)
                { try { AddSiteAmbience(site); } catch (Exception error) { Log.Warn("04B optional accent skipped: " + error.Message); } }
                Physics.SyncTransforms();
                Log.Info("04B_SITE " + site.Id + " PLACED " + ground.ToString("F2") + " parts=" + site.Art.PartCount + " triangles=" + site.Art.NearTriangles + "/" + site.Art.FarTriangles + " colliders=" + site.Art.ColliderCount);
            }
            catch (Exception error)
            {
                site.Note = "Skipped: art or interaction setup failed; existing city retained.";
                Blip.Remove(site.Marker); site.Marker = null;
                if (site.Art != null) Destroy(site.Art.gameObject); site.Art = null;
                reserved.Remove(ground); LivingTerritories04BQa.UnregisterSite(site.Id + "_resource");
                LivingTerritories04BQa.UnregisterSite(site.Id); Log.Exception("04B installation " + site.Id, error);
            }
            yield return null;
        }
        private void Update()
        {
            if (!_ready) return;
            if (_enabled && _living) _ecology.Tick(Time.deltaTime);
            TickEncounter();
            if (!Game.IsPlaying || Game.Player == null || Game.Player.IsDead) return;
            Book.Tick(Time.deltaTime); _visibilityClock -= Time.deltaTime;
            if (_visibilityClock <= 0f) { _visibilityClock = .5f; RefreshViews(); RefreshLights(); }
        }
        private void RefreshViews()
        {
            if (Book == null) return;
            bool outer = Game.Inner == null || !Game.Inner.IsInner;
            foreach (Site site in _sites)
            {
                if (!site.Placed) continue;
                TerritorySiteSave state = Book.Status(site.Id);
                if (state != null)
                {
                    if (Game.Player != null && MathUtil.FlatSqrDistance(Game.PlayerPosition, site.Origin) < 190f * 190f) Book.Discover(site.Id);
                    site.Art.SetState(state.Owned, state.Fortification);
                    site.Art.SetResourceClaimed(Book.Claimed("resource:" + site.Id));
                    site.Marker.Letter = state.Owned ? "O" : "T";
                    site.Marker.Color = state.Owned ? new Color(.5f,.95f,.7f) : RegionColor(site.Region);
                    site.Marker.Label = site.Title + (state.Owned ? " / owned tier " + state.Fortification : " / opt-in relay challenge");
                    site.Marker.ShowOnMap = state.Discovered || site.Definition.hub;
                }
                site.Art.gameObject.SetActive(outer); site.Marker.Visible = outer;
            }
            if (_homeDisplay != null) { _homeDisplay.SetProgress(Book.OwnedCount); _homeDisplay.gameObject.SetActive(outer); }
        }
        private Site Find(string id) { foreach (Site site in _sites) if (site.Id == id) return site; return null; }
        private static Color RegionColor(string region)
        { return region == "amethyst" ? new Color(.68f,.45f,1f) : region == "verdant" ? new Color(.4f,.85f,.58f) : new Color(1f,.42f,.22f); }
        private void Note(string message, float seconds = 6f) { if (Game.Hud != null) Game.Hud.Notify(message, seconds); }
        public void Capture(SaveData save) { if (Book != null && save != null) save.Territories = Book.Capture(); }
        public void Restore(SaveData save)
        {
            if (Book == null) return; CancelEncounter(); _ecology.ClearPopulation();
            Book.Restore(save == null ? null : save.Territories); RefreshViews();
            Site tracked = Find(Book.TrackedId); if (tracked != null && tracked.Placed && Game.Gps != null) Game.Gps.SetWaypoint(tracked.Core);
        }
        public void ResetAll() { Restore(null); }
        private void OnDisable() { CancelEncounter(); if (_ecology != null) _ecology.ClearPopulation(); }
        private void OnDestroy()
        {
            CancelEncounter(); if (_ecology != null) _ecology.ClearSites();
            foreach (Site site in _sites) { Blip.Remove(site.Marker); LivingTerritories04BQa.UnregisterSite(site.Id); LivingTerritories04BQa.UnregisterSite(site.Id + "_resource"); }
            WorldSystems.All.Remove(this); ReleaseUi();
            if (_homeDisplay != null) Destroy(_homeDisplay.gameObject);
            if (_homeInteractionRoot != null) Destroy(_homeInteractionRoot);
            LivingTerritories04BQa.UnregisterSite("hearthspire_living");
            if (Game.Territories == this)
            { Game.Territories = null; LivingTerritories04BQa.EncounterActive = null; LivingTerritories04BQa.StateSummary = null; }
        }
    }
}
