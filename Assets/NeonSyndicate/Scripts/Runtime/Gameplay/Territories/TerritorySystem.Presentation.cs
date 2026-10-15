using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class TerritorySystem
    {
        private List<TerritoryUiSite04B> BuildUiSites()
        {
            List<TerritoryUiSite04B> result = new List<TerritoryUiSite04B>(_sites.Count);
            foreach (Site site in _sites)
            {
                TerritorySiteSave state = Book == null ? null : Book.Status(site.Id);
                string details = site.Definition == null ? (site.Id == "rift_nesting" ? "Rift nesting ground. Observe from the open approach; these residents retaliate when provoked. Care is unavailable here. Explicit combat activities are at the Shatterworks relays." : "Living habitat. Residents follow authored time and weather schedules, proximity limits and existing creature behavior. Care and bestiary observation are peaceful.") :
                    site.Definition.description + "\nCapture reward: " + site.Definition.rewardCount + " " + MaterialDb.NameOf(site.Definition.rewardMaterial) + " once.\n" +
                    "Tier 1: " + UpgradeDescription(0) + ". Tier 2: " + UpgradeDescription(1) + ". Cover, recovery and defense wards. An owned core surveys its regional network; unknown secondary sites need discovery before tracking. No extra flight or climb multiplier.";
                result.Add(new TerritoryUiSite04B { Id = site.Id, Title = site.Title, Region = site.Region, Kind = site.Kind, Position = site.Origin,
                    Placed = site.Placed, Trackable = site.Placed && (state == null || state.Discovered || site.Definition.hub), PlacementNote = site.Note, Owned = state != null && state.Owned, RewardClaimed = state != null && state.RewardClaimed,
                    Fortification = state == null ? 0 : state.Fortification, Details = details,
                    Status = state == null ? "Living habitat" : state.Owned ? "Owned / tier " + state.Fortification : Book.ActiveId == site.Id ?
                        (Book.Remaining > 0 ? "Contested" : Book.ChannelArmed ? "Attuning" : "Cleared / interact with core") : state.Discovered ? "Discovered / available" : "Surveyed / not visited" });
            }
            return result;
        }
        private string UiSummary()
        {
            int placed = 0; foreach (Site site in _sites) if (site.Definition != null && site.Placed) placed++;
            return (Book == null ? 0 : Book.OwnedCount) + "/9 relays owned. " + placed + "/9 placed in this seed. " +
                (_ecology == null ? "" : _ecology.CreatureCount + " resident creatures / " + _ecology.StaffCount + " staff currently loaded. ") +
                "Explicit local PvE challenges; finite material rewards. " + _homeNote;
        }
        private string UiNearbyHint()
        {
            if (HasActiveEncounter)
                return Book.Remaining > 0 ? Book.Remaining + " wardens remain. Stay within 72m of this relay. Leaving for five seconds cancels." :
                    Book.ChannelArmed ? "Attuning: " + Book.Channel.ToString("0.0") + " / 6 seconds. Remain beside the core without taking damage." :
                    "Wardens cleared. Return to the core. Press E to begin attunement.";
            if (Book == null || Game.Player == null || (Game.Events != null && Game.Events.TerritoryEventActive)) return null;
            Site closest = null; float distance = 55f * 55f;
            foreach (Site site in _sites)
            {
                if (!site.Placed) continue;
                float d = MathUtil.FlatSqrDistance(Game.PlayerPosition, site.Origin);
                if (d < distance) { distance = d; closest = site; }
            }
            return closest == null ? null : closest.Title + ". Visit its marked interactions. Pause > TERRITORIES shows holdings, tracking and fortification costs.";
        }
        private void TrackSite(string id)
        {
            Site site = Find(id); if (site == null || !site.Placed) return;
            TerritorySiteSave state = Book.Status(id);
            if (state != null && !state.Discovered && !site.Definition.hub)
            { Note("Discover this relay on foot or survey it from an owned core in its region first."); return; }
            if (site.Definition != null) Book.Track(id);
            if (Game.Gps != null) Game.Gps.SetWaypoint(site.Core);
            Note("Tracking " + site.Title + ". Story objectives retain their GPS priority.");
        }
        private void ScoutRegion(Site origin)
        {
            Site next = null; float closest = float.PositiveInfinity;
            foreach (Site site in _sites)
            {
                if (!site.Placed || site.Definition == null || site.Region != origin.Region) continue;
                Book.Discover(site.Id);
                float distance = MathUtil.FlatSqrDistance(origin.Origin, site.Origin);
                if (!Book.Status(site.Id).Owned && distance < closest) { next = site; closest = distance; }
            }
            RefreshViews();
            if (next != null) TrackSite(next.Id);
            Note("Regional survey recorded in your save. " + (next != null ? "The nearest unowned regional relay is tracked." : "Every available relay in this region is owned.") + " This is a scouting perk with no movement stat bonus.");
        }
    }
}
