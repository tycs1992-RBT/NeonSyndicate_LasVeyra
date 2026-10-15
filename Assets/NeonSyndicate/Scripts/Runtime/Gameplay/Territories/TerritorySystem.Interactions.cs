using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class TerritorySystem
    {
        private void AddRelayInteractions(Site site)
        {
            Interactable core = Interactable.Create("04B relay core / " + site.Id, site.Core, 3f, "Challenge relay", delegate { UseRelay(site); }, site.Art.Root);
            core.Condition = delegate { return Available(site.Core) && (Game.Events == null || !Game.Events.TerritoryEventActive); };
            core.DynamicPrompt = delegate
            {
                TerritorySiteSave state = Book.Status(site.Id);
                return state.Owned ? "Press E to survey the next relay in this region" : Book.ActiveId == site.Id ?
                    (Book.Remaining > 0 ? "Defeat " + Book.Remaining + " wardens" : Book.ChannelArmed ? "Attuning... remain beside the core" : "Press E to attune cleared relay (six safe seconds)") :
                    "Press E to challenge " + site.Title + " (optional PvE)";
            };
            if (site.Definition.hub)
            {
                Vector3 resource = site.Origin + new Vector3(0f, 0f, 4.5f);
                Interactable station = Interactable.Create("04B resource / " + site.Id, resource, 2.8f, site.Definition.resource, delegate { UseResource(site, resource); }, site.Art.Root);
                station.Condition = delegate { return Available(resource) && !HasActiveEncounter && (Game.Events == null || !Game.Events.TerritoryEventActive); };
                station.DynamicPrompt = delegate { return Book.Claimed("resource:" + site.Id) ? site.Definition.resource + " / finite cache collected" : "Press E to calibrate " + site.Definition.resource + " / finite cache"; };
                LivingTerritories04BQa.RegisterSite(site.Id + "_resource", site.Definition.resource, "resource", station.transform);
            }
            Vector3 service = site.Origin + new Vector3(-3f, 0f, -5f);
            Interactable rest = Interactable.Create("04B relay service / " + site.Id, service, 2.3f, "Use relay recovery service", delegate { RestAt(site, service); }, site.Art.Root);
            rest.Condition = delegate { return Available(service) && Book.Status(site.Id).Owned && !HasActiveEncounter && (Game.Events == null || !Game.Events.TerritoryEventActive); };
            rest.DynamicPrompt = delegate
            {
                TerritorySiteSave state = Book.Status(site.Id);
                return state.Fortification == 0 ? "Relay recovery requires fortification tier one" : state.RestCooldown > 0f ? "Recovery recharge: " + Mathf.CeilToInt(state.RestCooldown) + " seconds of play" :
                    "Press E to recover " + (state.Fortification == 1 ? "25 health / 20 energy" : "50 health / 40 energy");
            };
            if (!_events) return;
            for (int index = 0; index < site.Definition.events.Length; index++)
            {
                string eventId = site.Definition.events[index];
                Vector3 position = site.Origin + new Vector3(index == 0 ? -3f : 3f, 0f, -10f);
                Interactable terminal = Interactable.Create("04B activity / " + eventId, position, 2.4f, "Request regional activity", delegate { StartActivity(site, eventId, position); }, site.Art.Root);
                terminal.Condition = delegate { return Available(position) && !HasActiveEncounter && (Game.Events == null || !Game.Events.TerritoryEventActive); };
                terminal.DynamicPrompt = delegate { return "Press E to request " + ActivityName(eventId) + (Book.Claimed("event:" + eventId) ? " / replay, reward already claimed" : " / one-time reward"); };
                // Existing shared world-marker presentation supplies the same keyboard/gamepad approach cue.
                WorldMarker.Create(position, RegionColor(site.Region), .65f, site.Art.Root, false);
            }
        }
        private bool Available(Vector3 position)
        { return isActiveAndEnabled && _ready && Game.IsPlaying && Game.Player != null && !Game.Player.IsDead && !Game.Player.InVehicle && (Game.Inner == null || !Game.Inner.IsInner) && Reach(position, 3.2f); }
        private void UseResource(Site site, Vector3 point)
        {
            if (!Reach(point) || HasActiveEncounter) return;
            string reason = SafetyReason(); if (reason != null) { Note(reason); return; }
            if (Game.Materials == null) { Note("The material service is unavailable."); return; }
            if (!Book.Claim("resource:" + site.Id)) { Note("This finite regional cache has already been collected in this save."); return; }
            string material = site.Region == "amethyst" ? "shard_chunk" : site.Region == "verdant" ? "mite_dust" : "rift_shard";
            Game.Materials.Add(material, site.Region == "obsidian" ? 2 : 4, true);
            site.Art.SetResourceClaimed(true);
            Note(site.Definition.resource + " calibrated. Finite materials collected; this landmark remains available for regional activities.");
        }
        private void StartActivity(Site site, string eventId, Vector3 point)
        {
            if (!_events || !Reach(point) || HasActiveEncounter) return;
            string reason = SafetyReason(); if (reason != null) { Note(reason); return; }
            if (Game.Events == null) { Note("The event service is unavailable."); return; }
            TerritorySiteSave state = Book.Status(site.Id);
            if (!Game.Events.TryStartTerritoryEvent(eventId, site.Id, site.Origin, state.Owned, delegate(QuestReward reward)
            {
                if (Game.Materials == null || Game.Progress == null || Game.Factions == null || !Book.Claim("event:" + eventId)) return false;
                QuestWorld.Grant(reward, ActivityName(eventId), true); return true;
            }, out reason, state.Fortification)) Note(reason ?? "This activity could not start.");
        }
        private static string ActivityName(string id)
        {
            switch (id)
            {
                case "ev_04b_blossom_gather": return "Rootwater Blossom Survey";
                case "ev_04b_courier_aid": return "Stranded Courier Aid";
                case "ev_04b_canopy_observation": return "Canopy Wildlife Observation";
                case "ev_04b_ancient_resonance": return "Ancient Resonance Sequence";
                case "ev_04b_relay_defense": return "Owned Relay Defense";
                case "ev_04b_rift_flare": return "Rift Flare Stabilization";
                default: return "Regional activity";
            }
        }
        private void RestAt(Site site, Vector3 point)
        {
            if (!Reach(point) || HasActiveEncounter) return;
            string reason = SafetyReason(); if (reason != null) { Note(reason); return; }
            if (Game.Player.Health == null || Game.Abilities == null) return;
            TerritorySiteSave state = Book.Status(site.Id);
            if (!Book.Rest(site.Id)) { Note(state.Fortification == 0 ? "Fortify this owned relay at its core first." : "Recovery has not recharged yet."); return; }
            Game.Player.Health.Heal(state.Fortification * 25f); Game.Abilities.AddEnergy(state.Fortification * 20f);
            Note("Relay recovery used. Recharges after 180 seconds of active play; this cooldown is saved.");
        }
        private string UiImproveReason(string id)
        {
            Site site = Find(id); if (site == null || !site.Placed || site.Definition == null) return "This relay was not placed in this seed.";
            TerritorySiteSave state = Book.Status(id);
            if (!state.Owned) return "Capture this relay first.";
            if (state.Fortification >= 2) return "Both fortification tiers are installed.";
            if (HasActiveEncounter) return "Finish the active relay challenge first.";
            string reason = SafetyReason(true); if (reason != null) return reason;
            if (!Reach(site.Core)) return "Stand beside this relay's physical core to fortify it.";
            if (!site.Art.CanInstallTier(state.Fortification + 1)) return "Move nearby people, creatures and vehicles away from the new cover pockets.";
            if (Game.Materials == null) return "The material service is unavailable.";
            foreach (KeyValuePair<string,int> cost in UpgradeCost(state.Fortification))
                if (!Game.Materials.Has(cost.Key, cost.Value)) return "Required: " + UpgradeDescription(state.Fortification) + ".";
            return null;
        }
        private static List<KeyValuePair<string,int>> UpgradeCost(int tier)
        {
            return new List<KeyValuePair<string,int>> { new KeyValuePair<string,int>("shard_chunk", tier == 0 ? 4 : 8),
                new KeyValuePair<string,int>(tier == 0 ? "mite_dust" : "glass_chitin", tier == 0 ? 2 : 4) };
        }
        private static string UpgradeDescription(int tier) { return tier == 0 ? "4 Shard Chunk + 2 Mite Dust" : "8 Shard Chunk + 4 Glass Chitin"; }
        private void ImproveRelay(string id)
        {
            string reason = UiImproveReason(id); if (reason != null) { Note(reason); return; }
            TerritorySiteSave state = Book.Status(id); List<KeyValuePair<string,int>> costs = UpgradeCost(state.Fortification);
            if (!Game.Materials.Spend(costs)) return;
            if (!Book.Improve(id)) { foreach (KeyValuePair<string,int> cost in costs) Game.Materials.Add(cost.Key, cost.Value, false); return; }
            RefreshViews(); Note("Fortification installed: solid side cover, recovery service, and stronger wards during requested defenses.");
        }
        private void AddCareInteraction(Site site)
        {
            Interactable care = Interactable.Create("04B creature care / " + site.Id, site.Origin, 22f, "Care for a calm nearby creature", delegate { CareForCreature(site.Id); }, site.Art.Root);
            care.Condition = delegate { TerritoryCare04B found; return CareAvailable(site.Id, out found); };
            care.DynamicPrompt = delegate { TerritoryCare04B found; return CareAvailable(site.Id, out found) ? "Press E to " + found.Label + (Book.Claimed("care:" + found.Id) ? " / reward collected" : " / first care: 2 Shard Chunk") : "Wait for a calm resident nearby"; };
        }
        private bool CareAvailable(string habitat, out TerritoryCare04B care)
        {
            care = default(TerritoryCare04B);
            return isActiveAndEnabled && _enabled && _living && !HasActiveEncounter && Game.IsPlaying && SafetyReason() == null &&
                _ecology.FindCare(Game.PlayerPosition, out care) && care.Habitat == habitat;
        }
        private void CareForCreature(string habitat)
        {
            TerritoryCare04B care;
            if (!CareAvailable(habitat, out care) || Game.Materials == null || !_ecology.CompleteCare(care.Id)) return;
            if (Book.Claim("care:" + care.Id)) Game.Materials.Add("shard_chunk", 2, true);
            Note("Resident cared for and recorded in the bestiary. Care never starts combat; its material reward is once per habitat.");
        }
    }
}
