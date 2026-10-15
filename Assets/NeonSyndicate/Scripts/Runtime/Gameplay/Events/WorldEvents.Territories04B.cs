using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class WorldEvents
    {
        private readonly Dictionary<string, float> _territoryCooldowns = new Dictionary<string, float>();
        private readonly List<string> _territoryCooldownScratch = new List<string>();

        /// <summary>Retires owned actors and callbacks if the director is disabled or its scene is torn down.</summary>
        private void OnDisable() { EndAll(false); }

        /// <summary>Only one requested territory activity may own actors and prompts at a time.</summary>
        public bool TerritoryActivityRunning
        {
            get
            {
                foreach (EventInstance e in _active) if (!e.Done && !string.IsNullOrEmpty(e.TerritoryKey)) return true;
                return false;
            }
        }

        public bool TerritoryEventActive { get { return TerritoryActivityRunning; } }
        public bool AnyEventActive { get { return CountRunning() > 0; } }

        public float TerritoryCooldown(string eventId, string siteId)
        {
            float seconds;
            return _territoryCooldowns.TryGetValue(siteId + ":" + eventId, out seconds) ? seconds : 0f;
        }

        /// <summary>
        /// Explicit physical gateway. Caller supplies a surveyed activity court and a transaction that claims and
        /// grants the template reward once in its territory ledger. This never runs from the ambient selector.
        /// </summary>
        public bool TryStartTerritoryEvent(string eventId, string siteId, Vector3 safeCenter, bool owned,
            Func<QuestReward, bool> grantOnce, out string reason, int fortificationTier = 0)
        {
            reason = null;
            EventTemplate t = Find(eventId);
            if (t == null || !t.ManualOnly || !TerritoryEventRules.ValidActivity(t.Kind, t.Activity)) reason = "This activity is unavailable.";
            else if (string.IsNullOrEmpty(siteId) || grantOnce == null) reason = "The territory ledger is unavailable.";
            else if (!Game.IsPlaying || Game.Player == null || Game.Player.IsDead || Game.Layout == null) reason = "Enter free exploration first.";
            else if (Blocked() || (Game.Missions != null && Game.Missions.Active != null)) reason = "Finish the current mission or activity first.";
            else if (Game.Wanted != null && Game.Wanted.Stars > 0) reason = "Lose the pursuit first.";
            else if (Game.Inner != null && Game.Inner.IsInner) reason = "Return from the Veil first.";
            else if (Game.Player.InVehicle || !Game.Player.IsGroundedPublic || Game.Player.TravelState == TravelMode.Flight) reason = "Land and leave your vehicle to accept this activity.";
            else if (Vector3.Distance(Game.PlayerPosition, safeCenter) > 18f) reason = "Visit this territory's physical terminal.";
            else if (t.Activity == "Defense" && !owned) reason = "Capture this relay before requesting a defense.";
            else if (t.Kind == "Skirmish" && StarterSafety.BlockAmbientAggression(safeCenter)) reason = "Combat activities are disabled in the protected starter area.";
            else if (QaTools.Busy) reason = "Finish the visual review first.";
            else if (Game.Territories != null && Game.Territories.HasActiveEncounter) reason = "Finish the relay challenge first.";
            else if (Game.Strongholds != null && Game.Strongholds.Book != null && Game.Strongholds.Book.ActiveRaid != null) reason = "Finish the stronghold raid first.";
            else if (AnyEventActive) reason = "Finish the active world event first.";
            else if (TerritoryCooldown(eventId, siteId) > 0f) reason = "This activity is resting for " + Mathf.CeilToInt(TerritoryCooldown(eventId, siteId)) + " more seconds of play.";
            else if (t.ThirdEye && (Game.Abilities == null || !Game.Abilities.ThirdEyeUnlocked)) reason = "Unlock the Third Eye first.";
            if (reason != null) return false;

            EventInstance e = null;
            try
            {
                e = new EventInstance(t, ++_serial, safeCenter, QuestWorld.YawToward(safeCenter, Game.PlayerPosition), transform, false);
                e.TerritoryKey = siteId + ":" + eventId;
                e.TerritoryFortification = Mathf.Clamp(fortificationTier, 0, 2);
                e.RewardHandler = grantOnce;
                e.PlaceName = siteId;
                e.PlayerAccepted = true;
                e.Engaged = true;
                e.Blip = Blip.Add(BlipKind.Activity, null, safeCenter, KindColor(t, false), "E", t.Title, true);
                e.Runner = new ScriptRunner(EventKinds.For(e));
                _active.Add(e);
                _forced.Add(e);
                _territoryCooldowns[e.TerritoryKey] = TerritoryEventRules.RetryCooldown;
                int n;
                _seen.TryGetValue(t.Id, out n);
                _seen[t.Id] = n + 1;
                _callout = e;
                _calloutUntil = Time.unscaledTime + 8f;
                _nextSpawn = Mathf.Max(_nextSpawn, _clock + t.Duration + 20f);
                if (Game.Hud != null) Game.Hud.Notify(t.Callout, 6f);
                return true;
            }
            catch (Exception ex)
            {
                if (e != null) { e.Cleanup(); _active.Remove(e); _forced.Remove(e); }
                Log.Exception("Territory event " + eventId, ex);
                reason = "The activity could not start safely.";
                return false;
            }
        }

        /// <summary>Called before any runner advances, so cleanup can never masquerade as defeating enemies.</summary>
        private string TerritoryStopReason(EventInstance e)
        {
            if (string.IsNullOrEmpty(e.TerritoryKey)) return null;
            return TerritoryEventRules.StopReason(Game.Player == null || Game.Player.IsDead,
                Game.Wanted != null && Game.Wanted.Stars > 0, Game.Inner != null && Game.Inner.IsInner,
                Vector3.Distance(Game.PlayerPosition, e.Pos), e.Age, e.T.Duration);
        }

        /// <summary>Death and mission transitions also clean up while the game is not in its Playing state.</summary>
        private void RetireInvalidTerritoryActivities()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                EventInstance e = _active[i];
                if (e.Done || string.IsNullOrEmpty(e.TerritoryKey)) continue;
                string stop = Game.Missions != null && Game.Missions.Active != null ? "The territory activity ended when another mission began." : TerritoryStopReason(e);
                if (stop == null && Game.State == GameState.Busted) stop = "The territory activity ended during the arrest.";
                if (stop == null) continue;
                e.Miss(stop);
                e.DespawnCreatures();
                Report(e);
                e.Cleanup();
                _active.RemoveAt(i);
                _forced.Remove(e);
                _reported.Remove(e);
                if (_callout == e) _callout = null;
            }
        }

        private void TickTerritoryCooldowns(float delta)
        {
            _territoryCooldownScratch.Clear();
            foreach (string key in _territoryCooldowns.Keys) _territoryCooldownScratch.Add(key);
            foreach (string key in _territoryCooldownScratch)
            {
                float remaining = _territoryCooldowns[key] - delta;
                if (remaining <= 0f) _territoryCooldowns.Remove(key);
                else _territoryCooldowns[key] = remaining;
            }
        }

        private void FinishTerritoryCooldown(EventInstance e)
        {
            if (string.IsNullOrEmpty(e.TerritoryKey)) return;
            float seconds = e.Outcome == EventOutcome.Success ? EventSelector.CooldownOf(e.T) : TerritoryEventRules.RetryCooldown;
            _territoryCooldowns[e.TerritoryKey] = seconds;
        }

        private void CaptureTerritoryCooldowns(EventSave save)
        {
            Dictionary<string, float> pending = new Dictionary<string, float>(_territoryCooldowns);
            foreach (EventInstance e in _active)
                if (!e.Done && !string.IsNullOrEmpty(e.TerritoryKey)) pending[e.TerritoryKey] = TerritoryEventRules.RetryCooldown;
            foreach (KeyValuePair<string, float> pair in pending)
            {
                save.TerritoryCooldownKeys.Add(pair.Key);
                save.TerritoryCooldownSeconds.Add(TerritoryEventRules.SavedCooldown(pair.Value));
            }
        }

        private void RestoreTerritoryCooldowns(EventSave save)
        {
            if (save.TerritoryCooldownKeys == null || save.TerritoryCooldownSeconds == null) return;
            int count = Mathf.Min(save.TerritoryCooldownKeys.Count, save.TerritoryCooldownSeconds.Count);
            for (int i = 0; i < count; i++)
            {
                string key = save.TerritoryCooldownKeys[i];
                float remaining = TerritoryEventRules.SavedCooldown(save.TerritoryCooldownSeconds[i]);
                if (!string.IsNullOrEmpty(key) && key.Length < 200 && remaining > 0f) _territoryCooldowns[key] = remaining;
            }
        }
    }
}
