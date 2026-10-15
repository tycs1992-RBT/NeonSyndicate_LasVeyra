using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class TerritorySystem
    {
        private TerritoryHome04BDisplay _homeDisplay;
        private GameObject _homeInteractionRoot;
        private string _homeNote = "Home gathering station not installed.";
        private readonly List<Light> _localLights = new List<Light>(3);
        private void BuildHome()
        {
            if (!_living || Game.Strongholds == null) return;
            Vector3 home;
            if (!Game.Strongholds.TryGetVisualAnchor("hearthspire", out home)) { _homeNote = "Hearthspire was not placed; no home geometry was added."; return; }
            VisualRebirth04ACourtMarker closest = null; float best = 240f * 240f;
            foreach (VisualRebirth04ACourtMarker marker in UnityEngine.Object.FindObjectsByType<VisualRebirth04ACourtMarker>(FindObjectsSortMode.None))
            {
                Transform court = marker.transform.parent;
                if (court == null || court.name.IndexOf("sanctuary", StringComparison.OrdinalIgnoreCase) < 0) continue;
                float distance = MathUtil.FlatSqrDistance(home, court.position);
                if (distance < best) { best = distance; closest = marker; }
            }
            if (closest == null) { _homeNote = "No safely installed 04A home sanctuary court; home additions skipped."; Log.Info("04B_HOME " + _homeNote); return; }
            Transform owner = closest.transform.parent;
            _homeInteractionRoot = new GameObject("04B Hearthspire care interactions"); _homeInteractionRoot.transform.SetParent(owner, false);
            if (_ecology.RegisterHabitat("home_care", owner.position, owner))
            {
                Interactable care = Interactable.Create("04B Hearthspire creature care", owner.position + new Vector3(-7f, 0f, -15f), 8f,
                    "Press E to care for Hearthspire's calm resident", delegate { CareForCreature("home_care"); }, _homeInteractionRoot.transform);
                care.Condition = delegate { TerritoryCare04B found; return HomeOwned() && CareAvailable("home_care", out found); };
            }
            Vector3 preferred = owner.position + new Vector3(0f, 0f, -28f), ground;
            if (!HomeStationGround(preferred, out ground))
            { _homeNote = "Home care enabled; gathering display skipped because its clear footer plot failed survey."; Log.Info("04B_HOME " + _homeNote); return; }
            try
            {
                _homeDisplay = ArtTerritories04B.BuildHomeDisplay(owner, ground);
                _ecology.RegisterStaff("hearthspire_caretaker", "amethyst", ground + _homeDisplay.StaffLocal, _homeDisplay.transform, true);
                Vector3 point = ground + _homeDisplay.CoreLocal;
                Interactable ledger = Interactable.Create("04B Hearthspire caretaker ledger", point, 3f, "Press E to consult the caretaker's holdings ledger", delegate
                {
                    if (!Reach(point)) return;
                    if (!HomeOwned()) { Note("Claim Hearthspire's original crystal core first. Its ownership and six build sockets are unchanged."); return; }
                    if (HasActiveEncounter) { Note("Finish the active relay challenge before consulting your caretaker."); return; }
                    OpenLedger();
                }, _homeDisplay.transform);
                ledger.Condition = delegate { return Available(point); };
                LivingTerritories04BQa.RegisterSite("hearthspire_living", "Hearthspire caretaker and holdings", "home", _homeDisplay.transform);
                _homeNote = "Caretaker, holdings ledger, nine-crystal progress display and calm resident care installed.";
                Log.Info("04B_HOME PLACED " + ground.ToString("F2") + " / " + _homeNote);
            }
            catch (Exception error) { _homeNote = "Home display failed; original fort and care preserved."; Log.Exception("04B home gathering station", error); }
        }
        private static bool HomeOwned()
        { return Game.Strongholds != null && Game.Strongholds.Book != null && Game.Strongholds.Book.Tier > 0; }
        /// <summary>Small footer extension outside the old court. Does not occupy its cross paths or fort sockets.</summary>
        private bool HomeStationGround(Vector3 p, out Vector3 ground)
        {
            ground = p;
            if (_layout == null || !StrongholdPlacement.RegionAllowed(p)) return false;
            foreach (RoadEdge edge in _layout.Edges)
            {
                float clearance = edge.Width * .5f + 10f;
                if (StrongholdPlacement.SegmentDistanceSq(p, _layout.Nodes[edge.A].Pos, _layout.Nodes[edge.B].Pos) < clearance * clearance) return false;
            }
            foreach (Poi poi in _layout.Pois) if (MathUtil.FlatSqrDistance(p, poi.Position) < 25f * 25f) return false;
            foreach (Interactable it in Interactable.All) if (it != null && MathUtil.FlatSqrDistance(p, it.transform.position) < (it.Radius + 5f) * (it.Radius + 5f)) return false;
            float min = float.PositiveInfinity, max = float.NegativeInfinity;
            for (int x = -1; x <= 1; x++) for (int z = -1; z <= 1; z++)
            {
                RaycastHit hit;
                if (!Physics.Raycast(p + new Vector3(x * 4f, 8f, z * 4f), Vector3.down, out hit, 12f, Layers.WorldMask, QueryTriggerInteraction.Ignore) || hit.normal.y < .985f) return false;
                min = Mathf.Min(min, hit.point.y); max = Mathf.Max(max, hit.point.y);
            }
            if (max - min > .1f || Mathf.Abs(max - p.y) > .2f) return false;
            ground = new Vector3(p.x, max + .035f, p.z);
            return !Physics.CheckBox(ground + Vector3.up * 3f, new Vector3(4f, 2.85f, 4f), Quaternion.identity, Layers.WorldMask, QueryTriggerInteraction.Ignore);
        }
        private void AddSiteAmbience(Site site)
        {
            if (site.Definition == null || !site.Definition.hub || _localLights.Count >= 3 || Settings.Graphics == GraphicsPreset.Low) return;
            GameObject lamp = new GameObject("04B regional core accent"); lamp.transform.SetParent(site.Art.Root, false);
            lamp.transform.localPosition = new Vector3(0f, 4f, 10f);
            Light light = lamp.AddComponent<Light>(); light.type = LightType.Point; light.color = RegionColor(site.Region);
            light.intensity = 1.2f; light.range = 15f; light.shadows = LightShadows.None; light.enabled = false; _localLights.Add(light);
        }
        private void RefreshLights()
        {
            bool night = Game.Atmo != null && (Game.Atmo.Hours < 6f || Game.Atmo.Hours > 18f);
            Vector3 viewer = LivingTerritories04BQa.FocusOverride ? LivingTerritories04BQa.FocusPoint : Game.PlayerPosition;
            int active = 0;
            foreach (Light light in _localLights)
            {
                if (light == null) continue;
                light.enabled = _ambience && night && Settings.Graphics != GraphicsPreset.Low && active < 2 &&
                    (Game.Inner == null || !Game.Inner.IsInner) && MathUtil.FlatSqrDistance(viewer, light.transform.position) < 90f * 90f;
                if (light.enabled) active++;
            }
        }
    }
}
