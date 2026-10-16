using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Finite crystal encounters (Game.Encounters, owner: encounters): explicit state, spawn budgets, tracked actors,
    /// leashes, cleanup, the local admission lease and the quiet aftermath after a real victory. The first contract is
    /// the Resonance Breach, a skirmish on open ground 160 to 320 m from the Veyra Lofts arrival: four authored
    /// opponents (a warden, a refractor and a skirmisher, then one advertised resonator reinforcement) on existing
    /// species with EncounterRole04D overlays. A missing actor is never a kill, a timeout is never a victory and an
    /// abort never pays. Split into CrystalEncounters.Run.cs (the run), .Site.cs (where), .Noise.cs (the scoped
    /// monster-combat noise rule) and .Ui.cs (the HUD line and the F2 "Encounters" page).
    /// </summary>
    public sealed partial class CrystalEncounters : MonoBehaviour, IWorldSystem
    {
        /// <summary>Contract ids the arsenal board can start (the first: the introductory Resonance Breach skirmish).</summary>
        public const string ResonanceBreach = "resonance_breach";
        /// <summary>The durable entitlement the first victory grants (ContractCleared reads it back).</summary>
        public const string RewardToken = "contract:resonance_breach";
        public const string RewardSource = "Resonance Breach";
        public const string Title = "Resonance Breach";
        /// <summary>Every quiet lease this system owns starts with this.</summary>
        public const string LeasePrefix = "encounter:";
        /// <summary>The breach's quiet aftermath owner (a replay refreshes it instead of stacking a second one).</summary>
        public const string AftermathOwner = "encounter:resonance_breach:aftermath";

        private CityLayout _layout;
        private EncounterState _state = EncounterState.Available;
        private EncounterRun04D _run;
        private float _retryWait;
        private EncounterAbortReason _lastAbort;
        private string _lastResult = "";
        private int _serial;
        private float _sweepTimer;
        private readonly List<string> _released = new List<string>();
        private readonly List<string> _log = new List<string>();

        /// <summary>The contract's state right now (debug page, tests through the board).</summary>
        public EncounterState State { get { return _state; } }

        public void Init(CityLayout layout)
        {
            _layout = layout;
            WorldSystems.Register(this);
            try { RegisterUi(); }
            catch (Exception e) { Log.Exception("Crystal encounters UI", e); }
            // The pure candidate search runs once here during the boot; the ground and collider checks wait for play.
            try { BuildCandidates(layout); }
            catch (Exception e) { Log.Exception("Crystal encounters site candidates", e); }
        }

        // ================================================================== contract board interface

        /// <summary>True when a contract can start now; reason explains a refusal in one line.</summary>
        public bool ContractAvailable(string contractId, out string reason)
        {
            if (contractId != ResonanceBreach)
            {
                reason = "Unknown contract.";
                return false;
            }
            reason = Refusal();
            return reason == null;
        }

        /// <summary>Starts a contract (an explicit player choice). Returns false with a reason when refused.</summary>
        public bool StartContract(string contractId, out string reason)
        {
            return StartContract(contractId, false, out reason);
        }

        /// <summary>Starts a contract; skipAdmission (debug page only) ignores the busy-world refusals but never a running breach.</summary>
        public bool StartContract(string contractId, bool skipAdmission, out string reason)
        {
            if (contractId != ResonanceBreach)
            {
                reason = "Unknown contract.";
                return false;
            }
            if (_run != null || EncounterRules04D.IsRunning(_state))
            {
                reason = "The breach is already open.";
                return false;
            }
            if (!skipAdmission)
            {
                reason = Refusal();
                if (reason != null) return false;
            }
            else if (Game.Player == null || Game.Creatures == null)
            {
                reason = "No player or creature system.";
                return false;
            }
            EnsureSite(true);
            if (_site == null || !_site.Valid)
            {
                reason = _site != null && !string.IsNullOrEmpty(_site.Reason) ? _site.Reason : "No safe open ground for the breach right now.";
                return false;
            }
            if (_state == EncounterState.Aborted) SetState(EncounterState.AvailableAfterRetry, "debug start skips the retry wait");
            if (!EncounterRules04D.CanStart(_state))
            {
                reason = "The contract cannot start from " + _state + ".";
                return false;
            }
            try
            {
                if (!BeginRun())
                {
                    reason = "The breach could not open.";
                    return false;
                }
            }
            catch (Exception e)
            {
                Log.Exception("Encounter start", e);
                EndRunImmediately(EncounterAbortReason.SpawnFailed, false);
                reason = "The breach could not open (see the log).";
                return false;
            }
            reason = null;
            return true;
        }

        /// <summary>One status line for the board and the journal ("Active: 2 of 4 defeated", "Cleared", ...).</summary>
        public string ContractStatus(string contractId)
        {
            if (contractId != ResonanceBreach) return "";
            bool cleared = ContractCleared(contractId);
            EncounterRun04D r = _run;
            switch (_state)
            {
                case EncounterState.Unavailable:
                    return "Unavailable: " + (_site != null && !string.IsNullOrEmpty(_site.Reason) ? _site.Reason : "no safe ground");
                case EncounterState.Preparing:
                    return "Opening the breach...";
                case EncounterState.Active:
                    if (r == null) return "Active";
                    if (!r.Engaged) return "Active: travel to the breach, " + DistanceLine(r.Origin) + " (GPS set)";
                    return "Active: " + r.Budget.Defeated + " of " + r.Budget.Total + " defeated" + (r.ReinforceScheduled && !r.ReinforceSpawned ? ", reinforcement incoming" : "");
                case EncounterState.Victory:
                    return "Victory";
                case EncounterState.Retreating:
                    return "Ending: " + EncounterRules04D.AbortLine(r != null ? r.AbortReason : _lastAbort) + ". The opponents withdraw.";
                case EncounterState.Aborted:
                    return "Failed: " + EncounterRules04D.AbortLine(_lastAbort) + ". No reward. Retry in " + Mathf.CeilToInt(Mathf.Max(0f, _retryWait)) + " s.";
                case EncounterState.AvailableAfterRetry:
                    return cleared ? "Cleared. Replay available (no new reward)." : "Available again (the last attempt gave no reward).";
                case EncounterState.Cleared:
                    return "Cleared" + (string.IsNullOrEmpty(_lastResult) ? "" : ": " + _lastResult) + ". Replay available (no new reward).";
                default:
                    if (cleared) return "Cleared. Replay available (no new reward).";
                    return _site != null && _site.Valid ? "Available: a resonance breach " + DistanceLine(_site.Center) : "Available";
            }
        }

        /// <summary>True once the contract's first real victory has happened in this save (the arsenal's entitlement).</summary>
        public bool ContractCleared(string contractId)
        {
            if (contractId != ResonanceBreach) return false;
            CrystalArsenal a = Game.Arsenal;
            if (a == null) return false;
            try { return a.HasEntitlement(RewardToken); }
            catch (Exception e)
            {
                Log.Exception("Encounter entitlement query", e);
                return false;
            }
        }

        /// <summary>A boss fell to the player at its arena: the arena gets the local quiet aftermath.</summary>
        public void OnBossVictory(string bossId, Vector3 arenaCenter, float arenaRadius)
        {
            if (string.IsNullOrEmpty(bossId) || !MathUtil.IsFinite(arenaCenter)) return;
            float r = float.IsNaN(arenaRadius) || float.IsInfinity(arenaRadius) ? LocalQuiet.QuietRadius : Mathf.Clamp(arenaRadius, 0f, 400f);
            string owner = "boss:" + bossId;
            bool fresh = LocalQuiet.AcquireAftermath(owner, arenaCenter, r);
            QuietLease l = LocalQuiet.Find(owner);
            Note("quiet aftermath " + (fresh ? "started" : "refreshed") + " at the " + bossId + " arena, radius " + (l != null ? l.Radius.ToString("0") : "?")
                + " m, " + LocalQuiet.QuietSeconds.ToString("0") + " s of play, then leave " + (l != null ? l.DepartRadius.ToString("0") : "?") + " m");
        }

        // ================================================================== admission

        private string Refusal()
        {
            EnsureSite(false);
            PlayerController p = Game.Player;
            EncounterRules04D.AdmissionFacts f = new EncounterRules04D.AdmissionFacts();
            f.Playing = Game.IsPlaying;
            f.QaBusy = QaTools.Busy;
            f.PlayerDown = p == null || p.IsDead;
            f.RunActive = _run != null || _state == EncounterState.Preparing || _state == EncounterState.Active || _state == EncounterState.Victory;
            f.RetryWait = _state == EncounterState.Aborted ? Mathf.Max(0.01f, _retryWait) : (_state == EncounterState.Retreating ? EncounterRules04D.RetrySeconds : 0f);
            f.MissionActive = Game.Missions != null && Game.Missions.Active != null;
            f.WantedStars = Game.Wanted != null ? Game.Wanted.Stars : 0;
            f.BossFight = Game.Creatures != null && Game.Creatures.ActiveBoss != null;
            f.RaidActive = Game.Strongholds != null && Game.Strongholds.Book != null && Game.Strongholds.Book.ActiveRaid != null;
            f.CaptureActive = (Game.Territories != null && Game.Territories.HasActiveEncounter) || (Game.Events != null && Game.Events.TerritoryEventActive);
            f.DepotActive = Game.Inhabited != null && Game.Inhabited.IsDepotEncounterActive;
            f.EventNear = WorldEventNear();
            f.InVeil = Game.Inner != null && Game.Inner.IsInner;
            f.SiteValid = _site != null && _site.Valid;
            f.SiteReason = _site != null ? _site.Reason : "The breach site is still being surveyed.";
            return EncounterRules04D.Refusal(f);
        }

        /// <summary>A running world event within 220 m of the breach or of the player.</summary>
        private bool WorldEventNear()
        {
            WorldEvents ev = Game.Events;
            if (ev == null || ev.Active == null) return false;
            Vector3 pp = Game.PlayerPosition;
            Vector3 site = _site != null ? _site.Center : pp;
            IList<EventInstance> list = ev.Active;
            for (int i = 0; i < list.Count; i++)
            {
                EventInstance e = list[i];
                if (e == null || e.Done) continue;
                if (MathUtil.FlatDistance(e.Pos, site) < 220f || MathUtil.FlatDistance(e.Pos, pp) < 220f) return true;
            }
            return false;
        }

        /// <summary>A heavier combat owner took over (boss fight, fort raid, relay capture, territory activity, depot).</summary>
        private static bool OtherCombatOwner()
        {
            if (Game.Creatures != null && Game.Creatures.ActiveBoss != null) return true;
            if (Game.Strongholds != null && Game.Strongholds.Book != null && Game.Strongholds.Book.ActiveRaid != null) return true;
            if (Game.Territories != null && Game.Territories.HasActiveEncounter) return true;
            if (Game.Events != null && Game.Events.TerritoryEventActive) return true;
            return Game.Inhabited != null && Game.Inhabited.IsDepotEncounterActive;
        }

        // ================================================================== state

        /// <summary>Moves the contract to another state if the rules allow it; every transition is logged once.</summary>
        private bool SetState(EncounterState next, string why)
        {
            if (next == _state) return true;
            if (!EncounterRules04D.CanTransition(_state, next))
            {
                Log.Warn("ENC04D refused transition " + _state + " -> " + next + (string.IsNullOrEmpty(why) ? "" : " (" + why + ")"));
                return false;
            }
            Note(_state + " -> " + next + (string.IsNullOrEmpty(why) ? "" : " (" + why + ")"));
            _state = next;
            return true;
        }

        /// <summary>One combat log row (bounded: transitions and actor events only, never per frame).</summary>
        private void Note(string line)
        {
            EncounterRun04D r = _run;
            string row = "ENC04D " + ResonanceBreach + (r != null ? "#" + r.Serial : "") + " [" + _state + "] " + line
                + (r != null && r.Budget != null ? " | " + r.Budget : "") + " | quiet leases " + LocalQuiet.Count;
            Log.Info(row);
            _log.Add(line);
            while (_log.Count > 10) _log.RemoveAt(0);
        }

        private static string DistanceLine(Vector3 target)
        {
            Vector3 pp = Game.PlayerPosition;
            return Mathf.RoundToInt(MathUtil.FlatDistance(pp, target)) + " m " + LootWorldRules.Compass(pp, target);
        }

        // ================================================================== frame

        private void Update()
        {
            float dt = Time.deltaTime;
            float active = Game.State == GameState.Playing && !GameTime.Paused ? dt : 0f;
            PlayerController p = Game.Player;
            try
            {
                _released.Clear();
                if (LocalQuiet.Tick(active, p != null ? Game.PlayerPosition : Vector3.zero, p != null, _released) > 0)
                    for (int i = 0; i < _released.Count; i++) Note("quiet zone ended (time served and the player left): " + _released[i]);
            }
            catch (Exception e) { Log.Exception("Local quiet tick", e); }
            if (_state == EncounterState.Aborted)
            {
                _retryWait -= active;
                if (_retryWait <= 0f)
                {
                    _retryWait = 0f;
                    SetState(EncounterState.AvailableAfterRetry, "retry window open");
                }
            }
            if (_run != null)
            {
                try { TickRun(_run, dt, active); }
                catch (Exception e)
                {
                    Log.Exception("Encounter run", e);
                    // Fail safe: never leave a half-dead run holding leases, reservations or actors.
                    EndRunImmediately(EncounterAbortReason.Disabled, true);
                }
            }
            _sweepTimer -= Time.unscaledDeltaTime;
            if (_sweepTimer <= 0f)
            {
                _sweepTimer = 1f;
                SweepOrphanLeases();
            }
        }

        /// <summary>Admission leases this system owns but no running encounter claims are released (never leaked).</summary>
        private void SweepOrphanLeases()
        {
            _released.Clear();
            string keep = _run != null ? _run.LeaseOwner : null;
            if (LocalQuiet.ReleaseKind(QuietKind.Admission, LeasePrefix, keep, _released) > 0)
                for (int i = 0; i < _released.Count; i++) Note("orphaned admission lease released: " + _released[i]);
        }

        private void OnDisable()
        {
            ShutDown();
        }

        private void OnDestroy()
        {
            ShutDown();
        }

        /// <summary>
        /// The system is going away: the run ends now (actors removed, reservations and leases released) and every quiet
        /// lease goes too, because nothing would tick an aftermath any more.
        /// </summary>
        private void ShutDown()
        {
            try { EndRunImmediately(EncounterAbortReason.Disabled, false); }
            catch (Exception e) { Log.Exception("Encounter shutdown", e); }
            LocalQuiet.ReleaseAll(null);
            EncounterShard04D.ClearAll();
        }

        // ================================================================== saves

        /// <summary>New game, load or title: an open breach ends without a reward and every quiet lease is released.</summary>
        public void ResetAll()
        {
            EndRunImmediately(EncounterAbortReason.SessionReset, false);
            _released.Clear();
            int n = LocalQuiet.ReleaseAll(_released);
            if (n > 0) Note("session reset released " + n + " quiet lease(s)");
            EncounterShard04D.ClearAll();
            // A reset rebuilds the machine instead of transitioning (completed progression lives in the arsenal).
            _state = EncounterState.Available;
            _retryWait = 0f;
            _lastAbort = EncounterAbortReason.None;
            _lastResult = "";
        }

        /// <summary>Nothing is saved: an open breach is simply gone after a load, and ContractCleared reads the arsenal.</summary>
        public void Capture(SaveData d) { }

        public void Restore(SaveData d)
        {
            ResetAll();
        }
    }

    /// <summary>One reset hub for every static the encounters pass adds (called from Game.ResetStatics).</summary>
    public static class CrystalEncounterStatics
    {
        public static void ResetStatics()
        {
            LocalQuiet.ResetStatics();
            EncounterRules04D.ResetStatics();
            EncounterSiteRules04D.ResetStatics();
            EncounterRole04D.ResetStatics();
            EncounterShard04D.ResetStatics();
        }
    }
}
