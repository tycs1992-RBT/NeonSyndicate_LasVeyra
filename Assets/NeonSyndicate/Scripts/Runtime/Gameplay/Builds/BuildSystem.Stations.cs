using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Fitting stations (where suggestions are applied and reverted) and reward ledger terminals (where pending rewards
    /// are claimed), each a real Interactable with a small procedural fixture. The four built-in ones register themselves
    /// once their rooms exist: the Cinder Dispatch worktable (Venue:dispatch.fitting), the Lantern Refuge fitting bench
    /// (Under:refuge.fitting), the home ledger beside the Veyra Lofts desk and the refuge ledger (Under:refuge.ledger).
    /// The home ledger carries the trophy case the home display reward lights.
    /// </summary>
    public sealed partial class BuildSystem
    {
        private sealed class Station
        {
            public string Id;
            public bool Fitting;
            public Interactable Use;
            public GameObject Fixture;
            public Vector3 Position;
            /// <summary>The venue whose interior gates the station (empty for underground and free stations).</summary>
            public string Venue = "";
        }

        private readonly Dictionary<string, Station> _stations = new Dictionary<string, Station>();
        private bool _fittingOpen;
        private Vector3 _fittingAt;
        private float _stationPoll;
        private bool _dispatchDone, _refugeFitDone, _homeDone, _refugeLedgerDone;
        private Transform _stationRoot;
        private GameObject _trophyCase;
        private readonly List<GameObject> _trophies = new List<GameObject>();

        public const string StationDispatch = "dispatch_worktable", StationRefugeFitting = "refuge_fitting";
        public const string TerminalHome = "home_ledger", TerminalRefuge = "refuge_ledger";

        /// <summary>True while the BUILD page was opened at a fitting station and is still open.</summary>
        public bool AtFitting { get { return _fittingOpen && Game.Menus != null && Game.Menus.IsOpen; } }

        public int FittingStationCount { get { int n = 0; foreach (Station s in _stations.Values) if (s.Fitting) n++; return n; } }
        public int LedgerTerminalCount { get { int n = 0; foreach (Station s in _stations.Values) if (!s.Fitting) n++; return n; } }

        /// <summary>Opens the build picker at a fitting station (pauses like a shop); PresetChosen fires on a choice.</summary>
        public bool OpenFitting(Vector3 station, out string reason)
        {
            reason = "";
            PlayerController p = Game.Player;
            if (Game.Menus == null) { reason = "Menus are not available."; return false; }
            if (p == null || p.IsDead) { reason = "Not now."; return false; }
            if (p.InVehicle) { reason = "Step out of the vehicle to use the fitting station."; return false; }
            if (CombatDirector.InCombat) { reason = "Not during a fight. Find a quiet moment."; return false; }
            if (Game.Gadgets != null && Game.Gadgets.WardRaised) { reason = "Lower the ward first (wait for it to end)."; return false; }
            BuildPage.ShowTab(BuildPage.TabSuggestions);
            if (!Game.Menus.OpenRegisteredPage("BUILD")) { reason = "The BUILD page could not open right now."; return false; }
            _fittingOpen = true;
            _fittingAt = station;
            Underheart04ESounds.Play2D(UhSfx.UiCard, 0.6f, 1f);
            return true;
        }

        /// <summary>Opens the BUILD page on its Rewards tab (ledger terminals, the HUD chip).</summary>
        public bool OpenLedger()
        {
            if (Game.Menus == null) return false;
            BuildPage.ShowTab(BuildPage.TabRewards);
            bool ok = Game.Menus.OpenRegisteredPage("BUILD");
            if (ok) Underheart04ESounds.Play2D(UhSfx.UiCard, 0.6f, 1.1f);
            return ok;
        }

        private void UpdateFitting()
        {
            if (_fittingOpen && (Game.Menus == null || !Game.Menus.IsOpen)) _fittingOpen = false;
        }

        private Transform StationRoot
        {
            get
            {
                if (_stationRoot == null)
                {
                    GameObject go = new GameObject("Builds 04E Stations");
                    go.transform.SetParent(transform, false);
                    _stationRoot = go.transform;
                }
                return _stationRoot;
            }
        }

        /// <summary>Registers a fitting station Interactable at a world point (the dispatch worktable, the refuge fitting).</summary>
        public void RegisterFittingStation(string id, Vector3 pos, Transform parent)
        {
            RegisterStation(id, pos, parent, true, "");
        }

        /// <summary>Registers a reward ledger terminal (home, refuge) where pending rewards can be claimed.</summary>
        public void RegisterLedgerTerminal(string id, Vector3 pos, Transform parent)
        {
            RegisterStation(id, pos, parent, false, "");
        }

        private Station RegisterStation(string id, Vector3 pos, Transform parent, bool fitting, string venue)
        {
            if (string.IsNullOrEmpty(id) || !MathUtil.IsFinite(pos)) return null;
            Station s;
            if (_stations.TryGetValue(id, out s))
            {
                if (s.Use != null) Destroy(s.Use.gameObject);
                if (s.Fixture != null) Destroy(s.Fixture);
            }
            s = new Station { Id = id, Fitting = fitting, Position = pos, Venue = venue ?? "" };
            Transform root = parent != null ? parent : StationRoot;
            Station st = s;
            if (fitting)
            {
                s.Use = Interactable.Create("Fitting station " + id, pos, 1.6f, "Press E to use the fitting station (build suggestions)", () => UseFitting(st), root);
                s.Fixture = BuildFixtures04E.FittingEmblem(root, pos);
            }
            else
            {
                s.Use = Interactable.Create("Reward ledger " + id, pos, 1.4f, "Press E to open the reward ledger", () => UseLedger(st), root);
                s.Use.DynamicPrompt = () => GInput.Prompt(_ledger.PendingCount > 0 ? "Press E to open the reward ledger (" + _ledger.PendingCount + " waiting)" : "Press E to open the reward ledger");
                s.Fixture = BuildFixtures04E.LedgerTerminal(root, pos);
            }
            s.Use.HeightTolerance = 1.4f;
            if (s.Venue.Length > 0) s.Use.Condition = () => Game.Inhabited == null || Game.Inhabited.CanUseAt(st.Venue, st.Use.transform.position);
            if (fitting) s.Use.DynamicPrompt = () => GInput.Prompt("Press E to use the fitting station (build suggestions" + (string.IsNullOrEmpty(_preset) ? ")" : ", now " + BuildPresetRules04E.Get(_preset).Title + ")"));
            _stations[id] = s;
            ReportStations();
            return s;
        }

        private void UseFitting(Station s)
        {
            string reason;
            if (!OpenFitting(s.Position, out reason))
            {
                if (Game.Hud != null) Game.Hud.Notify(reason, 3f);
                Underheart04ESounds.Play2D(UhSfx.UiDenied, 0.5f, 1f);
            }
        }

        private void UseLedger(Station s)
        {
            if (CombatDirector.InCombat)
            {
                if (Game.Hud != null) Game.Hud.Notify("Not during a fight.", 2f);
                return;
            }
            OpenLedger();
        }

        private void ReportStations()
        {
            int fit = FittingStationCount, led = LedgerTerminalCount;
            bool all = _dispatchDone && _refugeFitDone && _homeDone && _refugeLedgerDone;
            string missing = "";
            if (!_dispatchDone) missing += " Cinder Dispatch worktable (waits for the dispatch venue);";
            if (!_refugeFitDone) missing += " Lantern Refuge fitting (waits for the Underheart);";
            if (!_homeDone) missing += " home ledger (waits for the Veyra Lofts interior);";
            if (!_refugeLedgerDone) missing += " refuge ledger (waits for the Underheart);";
            ModuleStatus.Report("builds", "Builds, wards and rewards", all ? ModuleState.Loaded : ModuleState.Partial,
                fit + " fitting station(s) and " + led + " ledger terminal(s) placed." + (all ? "" : " Not yet:" + missing.TrimEnd(';') + "."),
                all ? "" : "Suggestions and claims also work from the BUILD page (K); fitting stations are needed only to apply a suggestion.");
            ModuleStatus.SetCount("builds", fit + led, "stations placed");
        }

        /// <summary>Registers the built-in stations as soon as their rooms exist (polled twice a second, cheap).</summary>
        private void UpdateStations()
        {
            if (_dispatchDone && _refugeFitDone && _homeDone && _refugeLedgerDone)
            {
                UpdateTrophies();
                return;
            }
            _stationPoll -= Time.unscaledDeltaTime;
            if (_stationPoll > 0f) return;
            _stationPoll = 0.5f;
            if (!_dispatchDone) _dispatchDone = TryVenueStation(VenueIds04E.Dispatch, "fitting", StationDispatch, true, Vector3.zero);
            if (!_homeDone) _homeDone = TryVenueStation("lofts", "desk", TerminalHome, false, new Vector3(3.4f, 0f, 0.2f));
            UnderheartWorld u = Game.Underheart;
            if (u != null && u.Ready)
            {
                if (!_refugeFitDone) _refugeFitDone = TryUnderStation("refuge.fitting", StationRefugeFitting, true);
                if (!_refugeLedgerDone) _refugeLedgerDone = TryUnderStation("refuge.ledger", TerminalRefuge, false);
            }
            UpdateTrophies();
        }

        /// <summary>A station at a venue anchor (offset in the venue's local frame), once the venue's room is built.</summary>
        private bool TryVenueStation(string venueId, string anchor, string id, bool fitting, Vector3 localOffset)
        {
            InhabitedCitySystem ic = Game.Inhabited;
            if (ic == null) return false;
            VenueBinding04C v = ic.FindVenue(venueId);
            if (v == null || !v.Available || v.View == null) return false;
            if (!HasAnchor(v, anchor))
            {
                Log.Warn("Builds: venue " + venueId + " has no '" + anchor + "' anchor; its " + (fitting ? "fitting station" : "ledger terminal") + " is skipped.");
                return true;
            }
            Vector3 local = v.View.LocalAnchor(anchor) + localOffset;
            Vector3 pos = v.View.Root.TransformPoint(local);
            // Parented to the builds' own root (world position), never into the venue's hierarchy.
            Station s = RegisterStation(id, pos, null, fitting, venueId);
            if (s != null && s.Fixture != null) s.Fixture.transform.rotation = Quaternion.Euler(0f, v.View.AnchorYaw(anchor) + (fitting ? 0f : 180f), 0f);
            if (!fitting && s != null) BuildTrophyCase(s, null);
            return true;
        }

        private static bool HasAnchor(VenueBinding04C v, string anchor)
        {
            if (v.View == null || v.View.Composition == null || v.View.Composition.anchors == null) return false;
            foreach (VenueAnchor04C a in v.View.Composition.anchors) if (a != null && a.id == anchor) return true;
            return false;
        }

        private bool TryUnderStation(string anchor, string id, bool fitting)
        {
            Vector3 pos;
            float yaw;
            if (!Game.Underheart.TryAnchor(anchor, out pos, out yaw)) return false;
            Station s = RegisterStation(id, pos, null, fitting, "");
            if (s != null && s.Fixture != null) s.Fixture.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            return true;
        }

        // ------------------------------------------------------------------ the home trophy display

        private void BuildTrophyCase(Station s, Transform parent)
        {
            if (_trophyCase != null) Destroy(_trophyCase);
            _trophies.Clear();
            GameObject[] slots;
            _trophyCase = BuildFixtures04E.TrophyCase(parent != null ? parent : StationRoot, s.Fixture != null ? s.Fixture.transform : null, out slots);
            if (slots != null) _trophies.AddRange(slots);
            UpdateTrophies();
        }

        private float _trophyClock;

        /// <summary>The case shows one lit trophy per Underheart guardian beaten, once the home display reward is claimed.</summary>
        private void UpdateTrophies()
        {
            if (_trophyCase == null) return;
            _trophyClock -= Time.unscaledDeltaTime;
            if (_trophyClock > 0f) return;
            _trophyClock = 1f;
            bool lit = HasUnlock(RewardCatalog04E.UnlockHomeDisplay);
            if (_trophyCase.activeSelf != lit) _trophyCase.SetActive(lit);
            if (!lit) return;
            for (int i = 0; i < _trophies.Count && i < UnderBossIds.All.Length; i++)
            {
                bool won = _ledger.Has(UnderBossIds.Token(UnderBossIds.All[i])) || (Game.UnderBosses != null && Game.UnderBosses.EverDefeated(UnderBossIds.All[i]));
                if (_trophies[i] != null && _trophies[i].activeSelf != won) _trophies[i].SetActive(won);
            }
        }

        /// <summary>Where a registered station stands (map layer, debug), false when it is not placed.</summary>
        public bool TryStation(string id, out Vector3 pos)
        {
            Station s;
            if (id != null && _stations.TryGetValue(id, out s))
            {
                pos = s.Position;
                return true;
            }
            pos = Vector3.zero;
            return false;
        }

        internal IEnumerable<KeyValuePair<string, Vector3>> StationPoints()
        {
            foreach (Station s in _stations.Values) yield return new KeyValuePair<string, Vector3>(s.Id + (s.Fitting ? "|f" : "|l"), s.Position);
        }
    }
}
