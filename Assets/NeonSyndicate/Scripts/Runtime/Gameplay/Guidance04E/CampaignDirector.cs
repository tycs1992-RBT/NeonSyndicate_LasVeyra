using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>What a quest giver shows above their head, on the map and in the journal (one shared answer).</summary>
    public enum GiverState
    {
        None = 0,
        AvailableMain = 1,
        AvailableSide = 2,
        ActiveTarget = 3,
        TurnIn = 4,
        Unavailable = 5,
        Completed = 6,
        NoWork = 7
    }

    /// <summary>
    /// Game.Campaign: guidance for Underheart and Bonds 04E. Spawns the named cast as real people at their posts (with
    /// an honest message terminal when a person cannot be placed), owns the giver state shared by overhead markers, map,
    /// journal and the offer card, the one-time update card, the opening lead, the post-quest next choices, the floor and
    /// region aware lead route (through Gps) and the STATUS page. Partials: Cast (people, posts, reservations,
    /// acknowledgement), Givers (state and talking), Map, Hud, Session (update card, leads, next choices), Status.
    /// </summary>
    public sealed partial class CampaignDirector : MonoBehaviour, IWorldSystem
    {
        public const string ModuleId = "campaign";

        private CityLayout _layout;
        private GuidanceSave _save = new GuidanceSave();
        private bool _inited;
        private Transform _castRoot;

        /// <summary>The durable guidance block for this playthrough (read by tests and the STATUS page).</summary>
        public GuidanceSave Save { get { return _save; } }

        public void Init(CityLayout layout)
        {
            _layout = layout;
            WorldSystems.Register(this);
            ResidentReservations04E.IsReserved = IsResidentSlotReserved;
            GameObject root = new GameObject("Campaign Cast 04E");
            root.transform.SetParent(transform, false);
            _castRoot = root.transform;
            try { InitCast(); }
            catch (Exception e) { Log.Exception("Campaign cast", e); }
            try { InitStagingH8S(); }
            catch (Exception e) { Log.Exception("Campaign cast staging", e); }
            try { InitGivers(); }
            catch (Exception e) { Log.Exception("Campaign givers", e); }
            try { InitUi(); }
            catch (Exception e) { Log.Exception("Campaign UI", e); }
            if (Game.Quests != null) Game.Quests.QuestCompleted += OnQuestCompleted;
            if (Game.Events != null) Game.Events.EventCompleted += OnCityEncounterCompleted04F;
            _inited = true;
            ReportModule();
        }

        private void OnDestroy()
        {
            if (Game.Quests != null) Game.Quests.QuestCompleted -= OnQuestCompleted;
            if (Game.Events != null) Game.Events.EventCompleted -= OnCityEncounterCompleted04F;
            if (ResidentReservations04E.IsReserved == (Func<string, int, bool>)IsResidentSlotReserved) ResidentReservations04E.IsReserved = null;
            DespawnAll();
            if (Game.Campaign == this) Game.Campaign = null;
        }

        private void Update()
        {
            if (!_inited) return;
            bool live = Game.Player != null && Game.State != GameState.Title && Game.State != GameState.Loading;
            if (!live)
            {
                DespawnAll();
                return;
            }
            float dt = Time.deltaTime;
            try { TickGivers(dt); }
            catch (Exception e) { LogOnce("givers", e); }
            try { TickCast(dt); }
            catch (Exception e) { LogOnce("cast", e); }
            try { TickSession(); }
            catch (Exception e) { LogOnce("session", e); }
            try { TickLead(dt); }
            catch (Exception e) { LogOnce("lead", e); }
        }

        private readonly HashSet<string> _logged = new HashSet<string>();

        /// <summary>A repeating failure is logged once per area per session, so a broken system never floods the Console.</summary>
        private void LogOnce(string area, Exception e)
        {
            if (_logged.Add(area)) Log.Exception("Campaign guidance " + area, e);
        }

        // ================================================================== frozen interface

        /// <summary>A session started (new or loaded): show the update card for an eligible save or the opening lead.</summary>
        public void OnSessionBegan(bool loadedSave, bool freeRoam)
        {
            BeginSession(loadedSave, freeRoam);
        }

        /// <summary>After a campaign quest passes: offer up to three deliberate next choices (never auto-starts anything).
        /// The card waits until the completion banner is over and the player is out of combat.</summary>
        public void ShowNextChoices(string completedQuestId)
        {
            QueueNextChoices(completedQuestId);
        }

        /// <summary>The live actor for a cast member, or null when not spawned (see CastAvailable for why).</summary>
        public NpcController CastActor(string castId)
        {
            CastSlot s = Slot(castId);
            return s != null && s.Actor != null && !s.Actor.IsDead ? s.Actor : null;
        }

        /// <summary>Where a cast member stands (their post), even when not spawned. False when the post is not bound.</summary>
        public bool CastPost(string castId, out Vector3 pos, out float yaw)
        {
            pos = Vector3.zero;
            yaw = 0f;
            CastSlot s = Slot(castId);
            if (s == null) return false;
            EnsureBound(s);
            if (s.PostBound)
            {
                pos = s.Post;
                yaw = s.PostYaw;
                return true;
            }
            // A remote contact (Ion before the observatory chapter) can still be routed to their contact point.
            if (s.RemoteBound)
            {
                pos = s.RemotePoint;
                yaw = s.RemoteYaw;
                return true;
            }
            return false;
        }

        /// <summary>True when the cast member can be met now (in person, or through an honest message terminal); reason
        /// says how they are present or why not (story gate, remote, busy mission, venue not loaded).</summary>
        public bool CastAvailable(string castId, out string reason)
        {
            CastSlot s = Slot(castId);
            if (s == null)
            {
                reason = "Unknown cast member \"" + castId + "\".";
                return false;
            }
            EnsureBound(s);
            string gate = GateReason(s);
            if (gate.Length > 0)
            {
                reason = gate;
                return false;
            }
            if (s.Actor != null)
            {
                reason = "In person at " + s.PostName + ".";
                return true;
            }
            if (s.Terminal != null)
            {
                reason = "Message terminal at " + s.PostName + " (" + s.Def.Name + " is not present: " + s.SpawnFailure + ").";
                return true;
            }
            reason = "Waiting at " + s.PostName + " (appears when you come near).";
            return true;
        }

        /// <summary>Keeps a cast member at their post for a quest (no schedule change can remove them) until Release.</summary>
        public void Reserve(string castId, string questId)
        {
            CastSlot s = Slot(castId);
            if (s == null || string.IsNullOrEmpty(questId)) return;
            s.Reservations.Add(questId);
        }

        public void Release(string castId, string questId)
        {
            CastSlot s = Slot(castId);
            if (s == null) return;
            if (string.IsNullOrEmpty(questId)) s.Reservations.Clear();
            else s.Reservations.Remove(questId);
        }

        /// <summary>The state a quest's giver shows now (the same decision the marker, map, journal and offer use).</summary>
        public GiverState StateFor(QuestDef q)
        {
            string reason;
            return StateForQuest(q, out reason);
        }

        /// <summary>Pins a quest's giver as the one tracked lead (map route and HUD line). A completed quest moves the lead
        /// on to the next main quest. Tracking never accepts or starts a quest.</summary>
        public void TrackLead(string questId)
        {
            TrackQuestLead(questId, true);
        }

        // ================================================================== IWorldSystem

        public void ResetAll()
        {
            _save = new GuidanceSave();
            foreach (CastSlot s in _slots)
            {
                s.Reservations.Clear();
                s.LastGreet = -1f;
                s.WalkedAway = true;
                s.Failures = 0;
                s.RetryAt = 0f;
                s.SpawnFailure = "";
                s.Bound = false;
            }
            DespawnAll();
            ResetSessionState();
            ClearLeadRoute();
        }

        public void Capture(SaveData d)
        {
            if (d == null) return;
            GuidanceSave s = new GuidanceSave
            {
                Version = Mathf.Max(1, _save.Version),
                UpdateCardSeen = _save.UpdateCardSeen,
                OpeningLeadOffered = _save.OpeningLeadOffered,
                TrackedLead = _save.TrackedLead ?? "",
                CityChoicesOffered04F = _save.CityChoicesOffered04F
            };
            if (_save.Met != null) s.Met.AddRange(_save.Met);
            d.Guidance04E = s;
        }

        public void Restore(SaveData d)
        {
            ResetAll();
            GuidanceSave s = d != null ? d.Guidance04E : null;
            if (s == null) return;
            _save.Version = Mathf.Max(1, s.Version);
            _save.UpdateCardSeen = s.UpdateCardSeen;
            _save.OpeningLeadOffered = s.OpeningLeadOffered;
            _save.CityChoicesOffered04F = s.CityChoicesOffered04F;
            _save.TrackedLead = s.TrackedLead ?? "";
            if (s.Met != null) foreach (string id in s.Met) if (CastIds04E.Has(id) && !_save.Met.Contains(id)) _save.Met.Add(id);
        }

        // ================================================================== module status

        /// <summary>Reports the campaign module with what was actually installed (cast posts bound, actors, terminals).</summary>
        private void ReportModule()
        {
            int bound = 0, actors = 0, terminals = 0, waiting = 0;
            foreach (CastSlot s in _slots)
            {
                if (s.PostBound) bound++;
                else waiting++;
                if (s.Actor != null) actors++;
                if (s.Terminal != null) terminals++;
            }
            bool quests = Game.Quests != null && Game.Quests.Find(QuestIds04E.Main[0]) != null;
            ModuleState state = quests && waiting == 0 ? ModuleState.Loaded : ModuleState.Partial;
            string detail = (quests ? "" : "The campaign quest data is not loaded, so givers have no work yet. ")
                + bound + " of " + _slots.Count + " cast posts bound"
                + (actors > 0 ? ", " + actors + " people in the world now" : "")
                + (terminals > 0 ? ", " + terminals + " message terminal(s) standing in" : "")
                + (waiting > 0 ? ", " + waiting + " waiting for their venue, the Underheart or their chapter" : "") + ".";
            ModuleStatus.Report(ModuleId, "Campaign guidance and cast", state, detail,
                quests ? (waiting > 0 ? "Pause > STATUS lists each missing post and why." : "") : "Check F2 > Quests > Check quest data.");
            ModuleStatus.SetCount(ModuleId, bound, "cast posts bound");
            int indoorJobs = 0;
            if (Game.Quests != null)
                foreach (QuestDef q in Game.Quests.Quests) if (GuidanceRules04F.IsInteriorContract(q.Id)) indoorJobs++;
            ModuleStatus.Report("city_jobs_04f", "04F indoor neighborhood contracts", indoorJobs == 3 ? ModuleState.Loaded : ModuleState.Partial,
                indoorJobs + " of 3 optional indoor jobs loaded. Corin offers the first after A Knock Beneath Home.",
                indoorJobs == 3 ? "JOURNAL tracks the giver and the real entrance." : "Check F2 > Quests > Check quest data.");
            ModuleStatus.SetCount("city_jobs_04f", indoorJobs, "optional indoor jobs loaded");
        }
    }

    /// <summary>Guidance's durable block (SaveData.Guidance04E). Append fields only.</summary>
    [Serializable]
    public sealed class GuidanceSave
    {
        public int Version = 1;
        /// <summary>The one-time update card was shown or dismissed for this save.</summary>
        public bool UpdateCardSeen;
        /// <summary>The opening lead was offered (new sessions get it automatically once).</summary>
        public bool OpeningLeadOffered;
        /// <summary>The quest whose giver the player pinned as the tracked lead ("" for none).</summary>
        public string TrackedLead = "";
        /// <summary>Cast members who have already said their first-meeting line in this save (later visits get a short greeting).</summary>
        public List<string> Met = new List<string>();
        /// <summary>The optional routes after the first successful 04F city encounter were offered once for this save.</summary>
        public bool CityChoicesOffered04F;
    }

    /// <summary>Reset hub for every static the GUIDANCE work stream adds.</summary>
    public static class GuidanceStatics04E
    {
        public static void ResetStatics()
        {
            CastRoster04E.ResetStatics();
            CastMarker04E.ResetStatics();
            BuildInfo04E.ResetStatics();
            AccessibilityPageH8S.ResetStatics();
        }
    }

    /// <summary>
    /// Lets the cast keep a 04C resident slot for a campaign actor (Nessa stands at the Lofts entrance instead of the
    /// lobby host body), so a person is never spawned twice. Set by the cast system; read by InhabitedResidents04C.
    /// </summary>
    public static class ResidentReservations04E
    {
        public static Func<string, int, bool> IsReserved;

        public static bool Reserved(string venueId, int slot)
        {
            if (IsReserved == null) return false;
            try { return IsReserved(venueId, slot); }
            catch (Exception e)
            {
                Log.Exception("Resident reservation", e);
                IsReserved = null;
                return false;
            }
        }

        public static void ResetStatics() { IsReserved = null; }
    }
}
