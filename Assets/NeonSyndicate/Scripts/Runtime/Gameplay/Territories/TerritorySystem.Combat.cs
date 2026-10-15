using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class TerritorySystem
    {
        private sealed class Defender
        {
            public CharacterBase Body;
            public CharacterHealth Health;
            public Action<DamageInfo> Death;
            public bool Defeated;
        }
        private readonly List<Defender> _defenders = new List<Defender>(3);
        private float _encounterAge, _outside;
        private string SafetyReason(bool allowPaused = false)
        {
            if (!_enabled || !_ready || !isActiveAndEnabled || Game.Player == null || Game.Player.IsDead || Game.Player.IsRagdolled ||
                Game.Player.InVehicle || !Game.Player.IsGroundedPublic || Game.Player.TravelState == TravelMode.Flight ||
                (!Game.IsPlaying && !(allowPaused && Game.State == GameState.Paused)) ||
                (Game.Missions != null && Game.Missions.Active != null) || QaTools.Busy || (Game.Inner != null && Game.Inner.IsInner))
                return "Land on foot during free exploration to use this site.";
            if (Game.Wanted != null && Game.Wanted.Stars > 0) return "Lose the pursuit first.";
            if (Game.Strongholds != null && Game.Strongholds.Book != null && Game.Strongholds.Book.ActiveRaid != null)
                return "Finish the stronghold raid first.";
            if (Game.Events != null && Game.Events.AnyEventActive) return "Finish the active world event first.";
            if (Game.Player.Health == null || Time.time - Game.Player.Health.LastDamageTime < 5f) return "Reach safety; attunement waits five seconds after damage.";
            return null;
        }
        private bool Reach(Vector3 point, float radius = 3f)
        {
            return Game.Player != null && MathUtil.FlatSqrDistance(Game.PlayerPosition, point) < radius * radius &&
                Mathf.Abs(Game.PlayerPosition.y - point.y) < 2f &&
                !Physics.Linecast(Game.PlayerPosition + Vector3.up, point + Vector3.up, Layers.WorldMask, QueryTriggerInteraction.Ignore);
        }
        private void UseRelay(Site site)
        {
            if (site == null || !site.Placed || !Reach(site.Core)) return;
            string reason = SafetyReason(); if (reason != null) { Note(reason); return; }
            TerritorySiteSave state = Book.Status(site.Id);
            if (state.Owned)
            {
                GrantCapture(site);
                ScoutRegion(site); return;
            }
            if (HasActiveEncounter)
            {
                if (Book.ActiveId != site.Id) { Note("Finish the other relay challenge first."); return; }
                if (Book.Remaining > 0) { Note("Defeat " + Book.Remaining + " remaining wardens before attuning."); return; }
                if (Book.Arm(site.Id)) Note("Attuning. Stay safely beside this core for six seconds. Damage or leaving cancels the channel.");
                return;
            }
            BeginEncounter(site);
        }
        private void BeginEncounter(Site site)
        {
            if (Game.Creatures == null || Game.Materials == null) { Note("The creature or material service is unavailable. No challenge was started."); return; }
            if (StarterSafety.BlockAmbientAggression(site.Origin)) { Note("Challenges are disabled in the protected starter area."); return; }
            Vector3[] anchors = site.Art.DefenderPoints;
            int count = Math.Min(site.Definition.guards.Length, Settings.Difficulty == DifficultyLevel.Story ? 1 : 3);
            if (Settings.Graphics == GraphicsPreset.Low) count = Math.Min(count, 2);
            if (Game.Creatures.Live.Count + count > TerritoryEcologyRules04B.DirectorLimit((int)Settings.Graphics))
            { Note("The nearby creature budget is full. Let the area settle before challenging this relay."); return; }
            if (anchors.Length < count) { Note("This site's defender anchors are unavailable."); return; }
            List<Vector3> grounded = new List<Vector3>(count);
            for (int i = 0; i < count; i++)
            {
                Vector3 at;
                float clearance = site.Definition.guards[i] == "crystal_basilisk" ? 1.8f : 1.2f;
                if (!ActorGround(site.Origin + anchors[i], clearance, out at)) { Note("A defender's spawn point is blocked. The challenge was not started."); return; }
                grounded.Add(at);
            }
            if (!Book.Begin(site.Id, count)) return;
            _encounterAge = _outside = 0f;
            try
            {
                for (int i = 0; i < count; i++)
                {
                    bool story = Settings.Difficulty == DifficultyLevel.Story;
                    CreatureSpawnOptions options = new CreatureSpawnOptions { Tag = EncounterTag(site.Id), Hostile = true,
                        Persistent = true, NoRewards = true, HealthScale = story ? .7f : 1f, DamageScale = story ? .5f : .7f,
                        DisplayName = site.Title + " Warden", Seed = Game.Seed + 9413 + i * 37 };
                    CharacterBase body = Game.Creatures.Spawn(site.Definition.guards[i], grounded[i], 180f, options);
                    if (body == null || body.Health == null) throw new InvalidOperationException("04B defender could not spawn.");
                    Defender watch = new Defender { Body = body, Health = body.Health }; int index = i; string id = site.Id;
                    watch.Death = delegate(DamageInfo info) { if (!watch.Defeated) watch.Defeated = Book.Defeat(id, index); };
                    body.Health.Died += watch.Death; _defenders.Add(watch);
                }
                TrackSite(site.Id);
                Note("Challenge accepted: " + count + " wardens. After defeating them, interact with the core to attune. Leaving the area cancels without rewards.", 9f);
            }
            catch (Exception error) { CancelEncounter(); Note("The challenge could not load. No capture or reward was granted."); Log.Exception("04B capture spawn", error); }
        }
        private static bool ActorGround(Vector3 desired, float radius, out Vector3 ground)
        {
            ground = desired; RaycastHit hit;
            if (!Physics.Raycast(desired + Vector3.up * 3f, Vector3.down, out hit, 7f, Layers.WorldMask, QueryTriggerInteraction.Ignore) || hit.normal.y < .95f || Mathf.Abs(hit.point.y - desired.y) > 1f) return false;
            ground = hit.point + Vector3.up * .08f;
            int mask = Layers.WorldMask | (1 << Layers.Vehicles) | (1 << Layers.Characters) | (1 << Layers.Ragdoll);
            return !Physics.CheckCapsule(ground + Vector3.up * (radius + .1f), ground + Vector3.up * (radius + 1f), radius, mask, QueryTriggerInteraction.Ignore);
        }
        private void TickEncounter()
        {
            if (!HasActiveEncounter || Game.State == GameState.Paused) return;
            if (!Game.IsPlaying || Game.Player == null || Game.Player.IsDead || Game.Player.InVehicle ||
                (Game.Missions != null && Game.Missions.Active != null) || (Game.Wanted != null && Game.Wanted.Stars > 0) ||
                (Game.Inner != null && Game.Inner.IsInner) || QaTools.Busy ||
                (Game.Strongholds != null && Game.Strongholds.Book != null && Game.Strongholds.Book.ActiveRaid != null))
            { CancelEncounter(); return; }
            Site site = Find(Book.ActiveId); if (site == null || !site.Placed) { CancelEncounter(); return; }
            float dt = Time.deltaTime; _encounterAge += dt;
            _outside = MathUtil.FlatSqrDistance(Game.PlayerPosition, site.Origin) > 72f * 72f ? _outside + dt : 0f;
            if (_encounterAge > 480f || _outside > 5f) { CancelEncounter(); Note("Relay challenge cancelled after leaving its area or exceeding eight minutes."); return; }
            foreach (Defender defender in _defenders)
            {
                if (defender.Defeated) continue;
                Creature creature = defender.Body as Creature;
                if (defender.Body == null || defender.Health == null || !defender.Health.IsAlive ||
                    (creature != null && !creature.Ready && _encounterAge > 15f) ||
                    MathUtil.FlatSqrDistance(defender.Body.transform.position, site.Origin) > 110f * 110f)
                { CancelEncounter(); Note("A warden disappeared or left the arena. The relay reset without a capture."); return; }
            }
            bool armed = Book.ChannelArmed;
            if (Book.Advance(dt, SafetyReason() == null, Reach(site.Core), false))
            {
                RemoveDefenders(site.Id); GrantCapture(site); RefreshViews();
                Note(site.Title + " captured. Its crystal now appears at Hearthspire. Two fortification tiers add cover and recovery services.", 8f);
            }
            else if (armed && !Book.ChannelArmed) Note("Attunement interrupted. Return safely to the core and interact to begin again.");
        }
        private static string EncounterTag(string id) { return "04b/relay/" + id; }
        private void RemoveDefenders(string id)
        {
            foreach (Defender defender in _defenders) if (defender.Health != null) defender.Health.Died -= defender.Death;
            _defenders.Clear();
            if (id != null && Game.Creatures != null) Game.Creatures.DespawnTag(EncounterTag(id));
        }
        private void CancelEncounter()
        {
            if (Book == null) return; string id = Book.ActiveId; Book.Cancel(); RemoveDefenders(id); _encounterAge = _outside = 0f;
        }
        private void GrantCapture(Site site)
        {
            if (Game.Materials != null && Book.ClaimCapture(site.Id)) Game.Materials.Add(site.Definition.rewardMaterial, site.Definition.rewardCount, true);
        }
    }
}
