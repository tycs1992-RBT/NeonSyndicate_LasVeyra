using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    [Serializable]
    public sealed class EventSave
    {
        public List<string> SeenIds = new List<string>();
        public List<int> SeenCounts = new List<int>();
        // Appended by the event director (old saves leave them empty): the journal's recent events, oldest first.
        public List<string> RecentIds = new List<string>();
        public List<string> RecentTitles = new List<string>();
        public List<int> RecentOutcomes = new List<int>();
        public List<int> RecentDays = new List<int>();
        public List<float> RecentHours = new List<float>();
        public List<string> RecentPlaces = new List<string>();
        // Appended in 04B. Remaining play time, not wall-clock time; old saves have no explicit cooldowns.
        public List<string> TerritoryCooldownKeys = new List<string>();
        public List<float> TerritoryCooldownSeconds = new List<float>();
        // 04F: event receipts and remaining play-time cooldowns survive a reload. Older saves initialize empty.
        public List<string> CrystalRewardTokens04F = new List<string>();
        public List<string> CrystalCooldownIds04F = new List<string>();
        public List<float> CrystalCooldownSeconds04F = new List<float>();
    }

    /// <summary>
    /// Random world events (Game.Events): data templates in Resources/Events picked by district, time of day, weather,
    /// story progress, cooldown and weight (EventSelector), spawned 80 to 160 m from the player with a small HUD
    /// callout and an E blip, at most two at a time with a gap between them. They respect the Veil, never start (and
    /// end quietly) during story missions, quests, cutscenes and menus, and clean up when done or left far behind.
    /// 04E: none starts while the player is in the Underheart, and one already running is held there and resumes on
    /// the way back up, or ends quietly as expired (WorldEvents.Underground04E, UndergroundEventRules04E).
    /// </summary>
    public sealed partial class WorldEvents : MonoBehaviour, IWorldSystem
    {
        public const int MaxActive = 2;
        /// <summary>Seconds between one event appearing and the director trying the next (plus up to a minute of jitter).</summary>
        public const float MinGap = 75f;
        /// <summary>Quiet seconds at the start of a session.</summary>
        public const float FirstDelay = 50f;
        public const float SpawnMin = 80f;
        public const float SpawnMax = 160f;
        public const int RecentKept = 12;

        public event Action<string> EventCompleted;

        /// <summary>The director picks and spawns events on its own while true (the debug page can switch it off).</summary>
        public bool DirectorEnabled = true;
        public readonly List<EventTemplate> Templates = new List<EventTemplate>();
        public readonly List<string> LoadProblems = new List<string>();

        private readonly List<EventInstance> _active = new List<EventInstance>();
        private readonly List<EventRecord> _recent = new List<EventRecord>();
        private readonly Dictionary<string, float> _lastFired = new Dictionary<string, float>();
        private readonly Dictionary<string, int> _seen = new Dictionary<string, int>();
        private readonly List<EventTemplate> _scratch = new List<EventTemplate>();
        private readonly List<Vector3> _cands = new List<Vector3>();
        private readonly HashSet<string> _blocked = new HashSet<string>();
        private readonly HashSet<EventInstance> _reported = new HashSet<EventInstance>();
        private readonly HashSet<EventInstance> _forced = new HashSet<EventInstance>();
        private float _clock;
        private float _nextSpawn = FirstDelay;
        private int _serial;
        private GRand _rng = new GRand(1);
        private bool _loaded;
        private EventInstance _callout;
        private float _calloutUntil;
        private EventTemplate _debugFire;

        public IList<EventInstance> Active { get { return _active; } }
        public IList<EventRecord> Recent { get { return _recent; } }
        /// <summary>Seconds until the director next tries to spawn (for the debug page).</summary>
        public float NextSpawnIn { get { return Mathf.Max(0f, _nextSpawn - _clock); } }

        public void Init(CityLayout layout)
        {
            WorldSystems.Register(this);
            _rng = new GRand(Game.Seed * 17 + 5);
            try { LoadTemplates(); }
            catch (Exception e) { Log.Exception("World event data", e); }
            try { RegisterUi(); }
            catch (Exception e) { Log.Exception("World event UI", e); }
        }

        private void LoadTemplates()
        {
            Templates.Clear();
            LoadProblems.Clear();
            HashSet<string> ids = new HashSet<string>();
            TextAsset[] files = Resources.LoadAll<TextAsset>("Events");
            Array.Sort(files, (a, b) => string.CompareOrdinal(a.name, b.name));
            foreach (TextAsset ta in files)
            {
                EventFile f = null;
                try { f = JsonUtility.FromJson<EventFile>(ta.text); }
                catch (Exception e) { LoadProblems.Add(ta.name + ": " + e.Message); }
                if (f == null) continue;
                QuestDefaults.Normalize(f);
                foreach (EventTemplate t in f.Events)
                {
                    if (string.IsNullOrEmpty(t.Id) || !ids.Add(t.Id)) { LoadProblems.Add(ta.name + ": event without an id, or a duplicate id '" + t.Id + "'"); continue; }
                    Templates.Add(t);
                }
            }
            foreach (string p in LoadProblems) Log.Warn("World events: " + p);
            Log.Info("World events: loaded " + Templates.Count + " templates from " + files.Length + " files.");
            _loaded = true;
        }

        public EventTemplate Find(string id)
        {
            foreach (EventTemplate t in Templates) if (t.Id == id) return t;
            return null;
        }

        public int SeenCount(string id)
        {
            int n;
            return id != null && _seen.TryGetValue(id, out n) ? n : 0;
        }

        private float LastFired(string id)
        {
            float t;
            return _lastFired.TryGetValue(id, out t) ? t : -1f;
        }

        public void RaiseCompleted(string eventId)
        {
            if (EventCompleted == null) return;
            foreach (Delegate d in EventCompleted.GetInvocationList())
            {
                try { ((Action<string>)d)(eventId); }
                catch (Exception e) { Log.Exception("EventCompleted listener", e); }
            }
        }

        /// <summary>Where and when the director is looking, from the live game.</summary>
        public EventSituation Situation()
        {
            EventSituation s = new EventSituation();
            Vector3 p = Game.PlayerPosition;
            s.District = CityLayout.DistrictAt(p);
            s.Inner = Game.Inner != null && Game.Inner.IsInner;
            s.Hours = Game.Atmo != null ? Game.Atmo.Hours : 12f;
            s.Weather = Game.Atmo != null ? Game.Atmo.Current : Weather.Clear;
            s.Story = Game.Missions != null ? Game.Missions.StoryProgress : 0;
            s.FreeRoam = Game.Root != null && Game.Root.FreeRoam;
            s.ThirdEye = Game.Abilities != null && Game.Abilities.ThirdEyeUnlocked;
            s.Now = _clock;
            return s;
        }

        /// <summary>Why a template cannot appear right now (null when it can), for the debug page.</summary>
        public string WhyNot(EventTemplate t)
        {
            if(t!=null && t.Id=="ev_04oc_azure_watch" && (Game.Layout==null || !Game.Layout.Fractured04OC))return "Fractured wilds preview only";
            HashSet<string> act = new HashSet<string>();
            foreach (EventInstance e in _active) if (!e.Done) act.Add(e.T.Id);
            return EventSelector.WhyNot(t, Situation(), LastFired, act);
        }

        // ================================================================== frame

        /// <summary>Story missions, quests and side activities (anything but open-ended jobs), cutscenes and cinematics.</summary>
        private static bool Blocked()
        {
            if (Game.State == GameState.Cutscene) return true;
            if (Game.Cam != null && Game.Cam.InCinematic) return true;
            MissionContext m = Game.Missions != null ? Game.Missions.Active : null;
            return m != null && m.Def != null && !m.Def.Quiet && !(Game.Events!=null && Game.Events.QuestLease04I);
        }

        private void Update()
        {
            if (!_loaded || Game.Layout == null) return;
            bool session = Game.Player != null && Game.State != GameState.Title && Game.State != GameState.Loading;
            if (!session)
            {
                if (_active.Count > 0) EndAll(false);
                return;
            }
            RetireInvalidTerritoryActivities();
            if (Blocked())
            {
                if (CountRunning() > 0) EndAll(true);
                _nextSpawn = Mathf.Max(_nextSpawn, _clock + 40f);
                return;
            }
            if (!Game.IsPlaying) return;
            float dt = Time.deltaTime;
            _clock += dt;
            TickTerritoryCooldowns(dt);
            Tick(dt);
            if (_debugFire != null)
            {
                EventTemplate t = _debugFire;
                _debugFire = null;
                if (!Spawn(t, true) && Game.Hud != null) Game.Hud.Notify("Could not place " + t.Title + " here.", 3f);
            }
            if (DirectorEnabled && !QuestLease04I && !TrySpawnCrystal04F() && _clock >= _nextSpawn) TrySpawnRandom();
            if (_citySavePending04F)
            {
                _citySavePending04F = false;
                if (!SaveSystem.Save(0)) Log.Warn("04F event receipt autosave failed: " + SaveSystem.LastError);
            }
        }

        private int CountRunning()
        {
            int n = 0;
            foreach (EventInstance e in _active) if (!e.Done) n++;
            return n;
        }

        private void Tick(float dt)
        {
            bool inner = Game.Inner != null && Game.Inner.IsInner;
            bool playerProtected = StarterSafety.BlockAmbientAggression(Game.PlayerPosition);
            bool underground = PlayerUnderground();
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                EventInstance e = _active[i];
                string territoryStop = TerritoryStopReason(e);
                if (!e.Done && territoryStop != null)
                {
                    e.Miss(territoryStop);
                    e.DespawnCreatures();
                }
                bool explicitlyRequested = _forced.Contains(e) || e.PlayerAccepted;
                if (!CityCombatRules04F.IsCityKind(e.T.Kind) && AmbientEncounterRules.RetireForSafety(playerProtected, StarterSafety.BlockAmbientAggression(e.FocusPos), explicitlyRequested, e.Done))
                {
                    // Never advance the runner after retiring it: cleanup must not look like defeated enemies and pay a reward.
                    e.Miss("");
                    e.Cleanup();
                    _active.RemoveAt(i);
                    _forced.Remove(e);
                    _reported.Remove(e);
                    if (_callout == e) _callout = null;
                    continue;
                }
                // 04E: the player is in the Underheart: a running surface event is held (no steps, no clock, no spawns,
                // no callout) and resumes on the way back up, or ends quietly as expired (WorldEvents.Underground04E).
                if (TickUnderground(e, i, underground, dt)) continue;
                e.Age += dt;
                if (!e.Done)
                {
                    // Measured from what the event is about now (the escorted person walks it away from where it began).
                    float d = e.DistToFocus;
                    if (e.Inner != inner)
                    {
                        e.Miss(e.Inner ? "It faded when you left the Inner World." : "It faded when you crossed the Veil.");
                        // Its creatures go with it at once (event spawns are Persistent, so the Veil itself keeps them).
                        e.DespawnCreatures();
                    }
                    else
                    {
                        // 04E: distances are flat, so a player in the Underheart below an event is not "at" it (a running
                        // event is held there and never reaches this line; the check stays as a guard).
                        if (!e.Engaged && d < EventKinds.EngageRange && !underground) e.Engaged = true;
                        e.FarTime = d > 320f ? e.FarTime + dt : 0f;
                        if (e.FarTime > 12f) e.Miss("");
                        else if (!e.Engaged && e.Age > (e.T.Duration > 0f ? e.T.Duration : QuestDefaults.EventDuration)) e.Miss("");
                        else if (e.Runner == null || !e.Runner.Step(e.Label)) { if (!e.Done) e.Miss(""); }
                    }
                }
                if (e.Done && _reported.Add(e)) Report(e);
                if (e.Done && (e.Age - e.DoneAge > 8f || e.DistToFocus > 150f))
                {
                    e.Cleanup();
                    _active.RemoveAt(i);
                    _forced.Remove(e);
                    _reported.Remove(e);
                    if (_callout == e) _callout = null;
                }
            }
        }

        /// <summary>Records a finished event, pays out through the instance, tells the player and raises EventCompleted.</summary>
        private void Report(EventInstance e)
        {
            FinishTerritoryCooldown(e);
            if (CityCombatRules04F.IsCityKind(e.T.Kind))
            {
                _nextSpawn = Mathf.Max(_nextSpawn, _clock + CityCombatRules04F.QuietSeconds);
                _cityNextAttempt04F = Mathf.Max(_cityNextAttempt04F, _clock + CityCombatRules04F.QuietSeconds);
            }
            if (e.Blip != null)
            {
                Blip.Remove(e.Blip);
                e.Blip = null;
            }
            AddRecord(new EventRecord
            {
                Id = e.T.Id,
                Title = e.T.Title,
                Outcome = e.Outcome,
                Day = Game.Atmo != null ? Game.Atmo.Day : 1,
                Hours = Game.Atmo != null ? Game.Atmo.Hours : 12f,
                Place = e.PlaceName
            });
            bool close = e.Engaged || e.DistToFocus < 120f;
            if (Game.Hud != null && close && !string.IsNullOrEmpty(e.EndLine))
            {
                if (e.Outcome == EventOutcome.Success) Game.Hud.Banner(e.T.Title.ToUpperInvariant(), e.EndLine, new Color(0.45f, 1f, 0.6f), 5f);
                else if (e.Outcome == EventOutcome.Failed) Game.Hud.Banner(e.T.Title.ToUpperInvariant(), e.EndLine, new Color(1f, 0.45f, 0.4f), 4.5f);
                else Game.Hud.Notify(e.EndLine, 3.5f);
            }
            if (CityCombatRules04F.IsCityKind(e.T.Kind) && e.Outcome == EventOutcome.Success) _citySavePending04F = true;
            if (e.Outcome == EventOutcome.Success)
            {
                if (Game.Audio != null && close) Game.Audio.Play2D(SfxId.MissionPassed, 0.45f, 1.2f);
                RaiseCompleted(e.T.Id);
            }
        }

        private void AddRecord(EventRecord r)
        {
            _recent.Add(r);
            while (_recent.Count > RecentKept) _recent.RemoveAt(0);
        }

        /// <summary>Ends every event at once (a mission started, a new game). quietly: no records for the ones cut short.</summary>
        private void EndAll(bool record)
        {
            foreach (EventInstance e in _active)
            {
                if (!e.Done)
                {
                    e.Miss("");
                    if (record && e.Engaged) AddRecord(new EventRecord { Id = e.T.Id, Title = e.T.Title, Outcome = EventOutcome.Missed, Day = Game.Atmo != null ? Game.Atmo.Day : 1, Hours = Game.Atmo != null ? Game.Atmo.Hours : 12f, Place = e.PlaceName });
                }
                FinishTerritoryCooldown(e);
                e.Cleanup();
            }
            _active.Clear();
            _questOwner04I=null;_questId04I="";_questCity04I=null;
            _forced.Clear();
            _reported.Clear();
            _callout = null;
        }

        // ================================================================== spawning

        private void TrySpawnRandom()
        {
            if (TerritoryEventActive || (Game.Territories != null && Game.Territories.HasActiveEncounter))
            {
                _nextSpawn = _clock + 10f;
                return;
            }
            if (StarterSafety.BlockAmbientAggression(Game.PlayerPosition))
            {
                _nextSpawn = _clock + 10f;
                return;
            }
            if (!MayStartSurfaceEvent(PlayerUnderground(), false))
            {
                // 04E: surface events would fire on the street over the player's head; wait until they come back up.
                _nextSpawn = _clock + 10f;
                return;
            }
            if (CountRunning() >= MaxActive)
            {
                _nextSpawn = _clock + 15f;
                return;
            }
            PlayerController p = Game.Player;
            if (p == null || p.IsDead || (Game.Wanted != null && Game.Wanted.Stars > 0) || (Game.Phone != null && Game.Phone.IsOpen))
            {
                _nextSpawn = _clock + 10f;
                return;
            }
            EventSituation s = Situation();
            _blocked.Clear();
            foreach (EventInstance e in _active) _blocked.Add(e.T.Id);
            for (int attempt = 0; attempt < 6; attempt++)
            {
                EventTemplate t = EventSelector.Pick(Templates, s, LastFired, _blocked, _rng, _scratch);
                if (t == null) break;
                if (Spawn(t, false))
                {
                    _nextSpawn = _clock + MinGap + _rng.Range(0f, 60f);
                    return;
                }
                _blocked.Add(t.Id);
            }
            _nextSpawn = _clock + 20f;
        }

        /// <summary>
        /// Places and starts an event. Normal spawns sit 80 to 160 m away on the template's anchor in an allowed
        /// district; forced (debug) spawns sit 20 to 90 m away and ignore the district rules.
        /// </summary>
        public bool Spawn(EventTemplate t, bool force)
        {
            if (t == null || t.ManualOnly || Game.Layout == null || Game.Player == null) return false;
            if(t.Id=="ev_04ob_threshold_watch" && !Game.Layout.Gothic04OB)return false;
            if(t.Id=="ev_04oc_azure_watch" && !Game.Layout.Fractured04OC)return false;
            if (TerritoryEventActive || (Game.Territories != null && Game.Territories.HasActiveEncounter)) return false;
            bool crystalCity = CityCombatRules04F.IsCityKind(t.Kind);
            if (crystalCity && CrystalCityRunning04F) return false;
            if (!crystalCity && !AmbientEncounterRules.MaySpawn(StarterSafety.BlockAmbientAggression(Game.PlayerPosition), false, force)) return false;
            if (!MayStartSurfaceEvent(PlayerUnderground(), force)) return false;
            Vector3 pos;
            if (!Place(t, force, out pos)) return false;
            bool inner = Game.Inner != null && Game.Inner.IsInner;
            EventInstance e = new EventInstance(t, ++_serial, pos, QuestWorld.YawToward(pos, Game.PlayerPosition), transform, inner);
            e.PlaceName = QuestPlaces.PlaceName(Game.Layout, pos);
            e.Blip = Blip.Add(BlipKind.Activity, null, pos, KindColor(t, inner), "E", t.Title, true);
            e.Blip.Size = 1.15f;
            e.Runner = new ScriptRunner(EventKinds.For(e));
            _active.Add(e);
            if (force) _forced.Add(e);
            _lastFired[t.Id] = _clock;
            int n;
            _seen.TryGetValue(t.Id, out n);
            _seen[t.Id] = n + 1;
            if (crystalCity) AttachCrystalReceipt04F(e, n + 1);
            _callout = e;
            _calloutUntil = Time.unscaledTime + 8f;
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.Notify, 0.5f, 1.25f);
            return true;
        }

        /// <summary>
        /// 04E: every event template is a surface (or Veil) event placed on the street around the player. While the player
        /// is in the Underheart no new one starts (only a forced debug spawn from the F2 Events page still may).
        /// </summary>
        public static bool MayStartSurfaceEvent(bool playerUnderground, bool force)
        {
            return force || !playerUnderground;
        }

        private static bool PlayerUnderground()
        {
            return Game.Player != null && WorldBounds.IsUnderground(Game.PlayerPosition);
        }

        private bool Place(EventTemplate t, bool force, out Vector3 pos)
        {
            pos = Vector3.zero;
            CityLayout L = Game.Layout;
            Vector3 player = Game.PlayerPosition;
            if(t.Id=="ev_04ob_threshold_watch" || t.Id=="ev_04oc_azure_watch")
            {
                if(!L.Gothic04OB || force)return false;
                Vector3 target=t.Id=="ev_04oc_azure_watch" ? L.AzureGlade04OC : L.RealmChallenge04OC;float distance=MathUtil.FlatDistance(player,target);
                if(distance<35f || distance>95f || LocalQuiet.Blocks(target) || StarterSafety.BlockAmbientAggression(target) ||
                    !CrystalPlacementSafe04F(target) || L.IsInsideBuilding(target,18f))return false;
                pos=QuestWorld.Ground(target);return true;
            }
            bool crystalCity = CityCombatRules04F.IsCityKind(t.Kind);
            float min = force ? 20f : crystalCity ? 38f : SpawnMin, max = force ? 90f : crystalCity ? 95f : SpawnMax;
            QuestPlaces.AnchorCandidates(L, t.Anchor, player, min, max, _cands);
            if ((force || crystalCity) && _cands.Count == 0) QuestPlaces.AnchorCandidates(L, "Any", player, min, max, _cands);
            for (int i = _cands.Count - 1; i >= 0; i--)
            {
                Vector3 c = _cands[i];
                bool bad = L.IsInsideBuilding(c, 0.5f) || (!force && !EventSelector.DistrictAllowed(t, CityLayout.DistrictAt(c)))
                    || (!crystalCity && !AmbientEncounterRules.MaySpawn(false, StarterSafety.BlockAmbientAggression(c), force));
                if (!bad && crystalCity) bad = !CrystalPlacementSafe04F(c);
                if (!bad && !force && LocalQuiet.Blocks(c)) bad = true;
                if (!bad)
                {
                    foreach (EventInstance e in _active)
                    {
                        if (MathUtil.FlatDistance(e.Pos, c) < 60f) { bad = true; break; }
                    }
                }
                if (bad) _cands.RemoveAt(i);
            }
            if (_cands.Count == 0 && crystalCity) RetryCrystalCandidates04F(t, player, min, max, force);
            if (_cands.Count == 0)
            {
                if (!force) return false;
                Vector3 f = Game.Cam != null ? Game.Cam.FlatForward : Vector3.forward;
                pos = QuestWorld.OpenGround(player + f * 30f, player);
                return true;
            }
            pos = QuestWorld.Ground(_cands[_rng.Range(0, _cands.Count)]);
            return true;
        }

        private static Color KindColor(EventTemplate t, bool inner)
        {
            if (inner) return new Color(0.75f, 0.35f, 1f);
            switch (t.Kind)
            {
                case "Fieldwork":
                case "Talk":
                case "Pickup":
                case "Relight":
                case "Escort": return new Color(0.35f, 0.95f, 1f);
                case "Brawl":
                case "Loot":
                case "Sabotage": return new Color(1f, 0.75f, 0.25f);
                default: return new Color(1f, 0.38f, 0.28f);
            }
        }

        // ================================================================== IWorldSystem

        public void ResetAll()
        {
            EndAll(false);
            _recent.Clear();
            _lastFired.Clear();
            _territoryCooldowns.Clear();
            _territoryCooldownScratch.Clear();
            _seen.Clear();
            _clock = 0f;
            _nextSpawn = FirstDelay;
            _serial = 0;
            _debugFire = null;
            _cityRewardTokens04F.Clear();
            _cityNextAttempt04F = 22f;
            _citySavePending04F = false;
        }

        public void Capture(SaveData d)
        {
            EventSave s = new EventSave();
            CaptureTerritoryCooldowns(s);
            CaptureCrystal04F(s);
            foreach (KeyValuePair<string, int> kv in _seen)
            {
                s.SeenIds.Add(kv.Key);
                s.SeenCounts.Add(kv.Value);
            }
            foreach (EventRecord r in _recent)
            {
                s.RecentIds.Add(r.Id);
                s.RecentTitles.Add(r.Title);
                s.RecentOutcomes.Add((int)r.Outcome);
                s.RecentDays.Add(r.Day);
                s.RecentHours.Add(r.Hours);
                s.RecentPlaces.Add(r.Place);
            }
            d.Events = s;
        }

        public void Restore(SaveData d)
        {
            ResetAll();
            EventSave s = d != null ? d.Events : null;
            if (s == null) return;
            RestoreTerritoryCooldowns(s);
            RestoreCrystal04F(s);
            if (s.SeenIds != null && s.SeenCounts != null)
            {
                int n = Mathf.Min(s.SeenIds.Count, s.SeenCounts.Count);
                for (int i = 0; i < n; i++) if (!string.IsNullOrEmpty(s.SeenIds[i])) _seen[s.SeenIds[i]] = Mathf.Max(0, s.SeenCounts[i]);
            }
            if (s.RecentIds == null) return;
            for (int i = 0; i < s.RecentIds.Count; i++)
            {
                EventRecord r = new EventRecord { Id = s.RecentIds[i] ?? "" };
                r.Title = s.RecentTitles != null && i < s.RecentTitles.Count ? s.RecentTitles[i] ?? "" : r.Id;
                int o = s.RecentOutcomes != null && i < s.RecentOutcomes.Count ? s.RecentOutcomes[i] : (int)EventOutcome.Missed;
                r.Outcome = Enum.IsDefined(typeof(EventOutcome), o) ? (EventOutcome)o : EventOutcome.Missed;
                r.Day = s.RecentDays != null && i < s.RecentDays.Count ? s.RecentDays[i] : 1;
                r.Hours = s.RecentHours != null && i < s.RecentHours.Count ? s.RecentHours[i] : 12f;
                r.Place = s.RecentPlaces != null && i < s.RecentPlaces.Count ? s.RecentPlaces[i] ?? "" : "";
                AddRecord(r);
            }
        }
    }
}
