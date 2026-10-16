using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class InhabitedCitySystem
    {
        private const string DepotTag = "quiet_signal_04c_depot";
        private Interactable _chapterAction;
        private string _chapterKey = "";
        private string _channelVenue = "";
        private string _channelAction = "";
        private float _channelProgress;
        private float _channelSeconds;
        private float _channelLastDamage;
        private float _depotAge;
        private int _depotExpected;
        private readonly List<DepotThreat04C> _depotThreats = new List<DepotThreat04C>();
        private readonly List<Interactable> _readingPoints = new List<Interactable>();

        private sealed class DepotThreat04C
        {
            public CharacterBase Body;
            public CharacterHealth Health;
            public Action<DamageInfo> Died;
            public bool Defeated;
        }

        public bool IsDepotEncounterActive { get { return _depotExpected > 0; } }

        private void InitActivities()
        {
            ClearActivities();
            AddReadingPoint("archive", "record_origins", "record1", "Press E to read the receiver history");
            AddReadingPoint("archive", "record_route", "record2", "Press E to read the depot service route");
            AddReadingPoint("archive", "record_frequency", "record3", "Press E to read the observatory notation");
            AddReadingPoint("inn", "interview", "visitor1", "Press E to ask the courier for neighborhood directions");
        }

        /// <summary>Unavailable replacements lock only their dependent chapter, preserving the original city's services.</summary>
        public string QuestVenueProblem(QuestDef quest)
        {
            if (!IsOperational || quest == null) return "Inhabited city venues are unavailable";
            VenueBinding04C giver = FindVenue(quest.VenueGiver);
            if (giver == null || !giver.Available) return "The chapter's presenter venue is unavailable";
            foreach (QuestPhase phase in quest.Phases) foreach (QuestStep step in phase.Steps)
            {
                if (step.Type != "Venue") continue;
                VenueBinding04C venue = FindVenue(step.VenueId);
                if (venue == null || !venue.Available) return "The required " + step.VenueId + " venue is unavailable";
            }
            return null;
        }

        private void AddReadingPoint(string venueId, string actionId, string anchor, string prompt)
        {
            VenueBinding04C venue = FindVenue(venueId);
            if (venue == null || !venue.Available) return;
            Vector3 point = venue.Point(anchor);
            Interactable reading = Interactable.Create("Venue reading " + venueId + ":" + actionId, point, 2f, prompt,
                () => ReadVenueRecord(venueId, actionId), transform);
            reading.HeightTolerance = 1.25f;
            reading.Condition = () => CanUseAt(venueId, point) && !IsDepotEncounterActive
                && (Game.Quests == null || Game.Quests.Running == null || Game.Quests.Running.CurrentVenue != venueId || Game.Quests.Running.CurrentAction != actionId);
            _readingPoints.Add(reading);
        }

        private void ReadVenueRecord(string venueId, string actionId)
        {
            VenueBinding04C venue = FindVenue(venueId);
            if (venue == null || !CanUseAt(venueId, venue.Point(QuietSignal04C.AnchorFor(venueId, actionId))) || Game.Menus == null) return;
            string title;
            string content;
            switch (actionId)
            {
                case "record_origins": title = "THE HOME RECEIVERS"; content = "The observatory once sent weather and sky reports into the lofts. Hand-tuned home receivers let residents who could not climb the tower hear the same reports as everyone else."; break;
                case "record_route": title = "A STORM DELIVERY"; content = "The replacement relay remained at the service depot after the storm. The maintenance route needs no combat: isolate the feed at the bypass panel, then release the relay at the storage bench."; break;
                case "record_frequency": title = "ALIGNMENT NOTATION"; content = "The receiver's three-step alignment is engraved here: split crescent at the lens, low band at the tuner, then hold focus until the waveform steadies. The observatory instruments repeat this guide."; break;
                default: title = "CINDER DINER DIRECTIONS"; content = "Nessa is at Veyra Lofts. The archive and observatory are neighborhood stops; Mara Solane Street Clinic is farther away in Sunken Row, and Dante's forge is in Old Silver. The service depot has a peaceful relay bypass."; break;
            }
            if (venueId == "archive" && Book != null) Book.ReadRecord(actionId);
            Game.Menus.ShowChoice(title, content, new[] { "CLOSE", venueId == "inn" ? "TRACK VEYRA LOFTS" : "TRACK OBSERVATORY" }, index =>
            {
                if (index == 1 && CanUseAt(venueId, venue.Point(QuietSignal04C.AnchorFor(venueId, actionId)))) TrackVenue(venueId == "inn" ? "lofts" : "observatory");
            });
        }

        /// <summary>Exterior directions until the room is occupied; precise floor positions after entry.</summary>
        public bool TryGetActionTarget(string venueId, string actionId, out Vector3 target)
        {
            target = Vector3.zero;
            if (!QuietSignal04C.ValidAction(venueId, actionId)) return false;
            VenueBinding04C venue = FindVenue(venueId);
            if (!IsOperational || venue == null || !venue.Available) return false;
            target = IsInsideVenue(venueId) ? venue.Point(QuietSignal04C.AnchorFor(venueId, actionId)) : venue.Entrance;
            return true;
        }

        public bool ActionCompleted(string venueId, string actionId)
        {
            return QuietSignal04C.ValidAction(venueId, actionId) && Book != null && Book.HasAction(QuietSignal04C.ActionKey(venueId, actionId));
        }

        private bool ChapterActionAllowed(string venueId, string actionId)
        {
            QuestRun run = Game.Quests != null ? Game.Quests.Running : null;
            VenueBinding04C venue = FindVenue(venueId);
            bool safe = Game.IsPlaying && Game.Player != null && !Game.Player.IsDead
                && (Game.Wanted == null || Game.Wanted.Stars == 0)
                && (Game.Inner == null || !Game.Inner.IsInner)
                && (Game.Territories == null || !Game.Territories.HasActiveEncounter)
                && (Game.Events == null || !Game.Events.AnyEventActive)
                && (Game.Strongholds == null || Game.Strongholds.Book == null || Game.Strongholds.Book.ActiveRaid == null);
            return run != null && Game.Missions != null && Game.Missions.Active == run.C && venue != null && !ActionCompleted(venueId, actionId)
                && QuietSignal04C.MayUseAction(run.Quest.Id, run.CurrentVenue, run.CurrentAction, venueId, actionId, IsInsideVenue(venueId), safe)
                && CanUseAt(venueId, venue.Point(QuietSignal04C.AnchorFor(venueId, actionId)));
        }

        private void CompleteChapterAction(string venueId, string actionId, string message)
        {
            if (!ChapterActionAllowed(venueId, actionId) || Book == null) return;
            Book.CompleteAction(QuietSignal04C.ActionKey(venueId, actionId));
            if (Game.Hud != null && !string.IsNullOrEmpty(message)) Game.Hud.Notify(message, 5f);
        }

        private void TickActivities(float delta)
        {
            QuestRun run = Game.Quests != null ? Game.Quests.Running : null;
            bool invalid = Game.Player == null || Game.Player.IsDead || Game.State == GameState.Title || Game.State == GameState.Loading
                || Game.State == GameState.Busted || Game.State == GameState.Wasted || run == null;
            if (invalid) { ClearChapterActivity(); return; }
            // Pausing a choice, shop or the journal must neither advance nor reset a channel or encounter.
            if (!Game.IsPlaying) return;
            string venueId = run.CurrentVenue;
            string actionId = run.CurrentAction;
            string key = QuietSignal04C.ActionKey(venueId, actionId);
            bool active = QuietSignal04C.ValidAction(venueId, actionId) && IsInsideVenue(venueId) && !ActionCompleted(venueId, actionId);
            if ((Game.Wanted != null && Game.Wanted.Stars > 0) || (Game.Inner != null && Game.Inner.IsInner)) active = false;
            if ((Game.Territories != null && Game.Territories.HasActiveEncounter) || (Game.Events != null && Game.Events.AnyEventActive)
                || (Game.Strongholds != null && Game.Strongholds.Book != null && Game.Strongholds.Book.ActiveRaid != null)
                || Game.Missions == null || Game.Missions.Active != run.C) active = false;
            if (!active || _chapterKey != key)
            {
                ClearChapterActivity();
                if (!active) return;
                VenueBinding04C venue = FindVenue(venueId);
                if (venue == null) return;
                _chapterKey = key;
                _chapterAction = Interactable.Create("Quiet Signal " + key, venue.Point(QuietSignal04C.AnchorFor(venueId, actionId)), 2f,
                    ChapterPrompt(actionId), () => UseChapterAction(venueId, actionId), transform);
                _chapterAction.HeightTolerance = 1.25f;
                _chapterAction.Condition = () => ChapterActionAllowed(venueId, actionId) && !IsDepotEncounterActive && string.IsNullOrEmpty(_channelAction);
            }
            if (!string.IsNullOrEmpty(_channelAction)) TickChapterChannel(delta);
            if (IsDepotEncounterActive) TickDepotEncounter(delta);
        }

        private static string ChapterPrompt(string actionId)
        {
            switch (actionId)
            {
                case "request": return "Press E to speak with Nessa about the quiet signal";
                case "instrument_case": return "Press E to inspect the damaged receiver";
                case "supply_manifest": return "Press E to read Mara's supply manifest";
                case "inspect_supplies": return "Press E to inspect the clinic shipment";
                case "calibrate": return "Press E to use the repair bench";
                case "record_origins": return "Press E to read the receiver's history";
                case "record_route": return "Press E to read the depot service route";
                case "record_frequency": return "Press E to read the alignment notation";
                case "interview": return "Press E to ask about the night delivery";
                case "retrieve_safely": return "Press E to isolate the depot relay without combat";
                case "clear_threat": return "Press E to begin the finite depot encounter";
                case "recover_relay": return "Press E to recover the isolated relay";
                case "align_lens": return "Press E to align the observatory lens";
                case "tune_frequency": return "Press E to tune the receiver frequency";
                case "focus_receiver": return "Press E to hold the receiver in focus";
                case "install_display": return "Press E to install the restored home receiver";
                default: return "Press E to inspect this station";
            }
        }

        private void UseChapterAction(string venueId, string actionId)
        {
            if (!ChapterActionAllowed(venueId, actionId)) return;
            switch (actionId)
            {
                case "inspect_supplies":
                    ChapterChoice(venueId, actionId, "MARA'S SHIPMENT", "Manifest: usable seals are clear and unbroken. The amber crate is cracked; the blue crate has intact seals. Which is safe to deliver?",
                        new[] { "BLUE, INTACT SEALS", "AMBER, CRACKED SEALS", "LEAVE IT" }, 0, "The intact supplies are logged for Mara. No personal materials were removed.");
                    break;
                case "calibrate": ShowRepairChoice(venueId, actionId); break;
                case "retrieve_safely":
                    ChapterChoice(venueId, actionId, "SERVICE BYPASS", "The archive and courier both describe the same bypass: isolate the feed before releasing the relay. The faded plate repeats this warning.",
                        new[] { "ISOLATE THEN RELEASE", "PULL THE LIVE RELAY", "LEAVE IT" }, 0, "", 5f);
                    break;
                case "clear_threat": StartDepotEncounter(); break;
                case "align_lens":
                    ChapterChoice(venueId, actionId, "LENS ALIGNMENT", "The engraved guide reads: align the split crescent, then select the low band, then hold focus. Start with the lens.",
                        new[] { "SPLIT CRESCENT", "FULL CIRCLE", "LEAVE IT" }, 0, "The lens is aligned to the split crescent.");
                    break;
                case "tune_frequency":
                    ChapterChoice(venueId, actionId, "RECEIVER BAND", "The archive notation and the instrument's brass guide specify the low band for the home receiver.",
                        new[] { "HIGH BAND", "LOW BAND", "LEAVE IT" }, 1, "The receiver is tuned to the low band.");
                    break;
                case "focus_receiver": BeginChapterChannel(venueId, actionId, 5f); break;
                default:
                    if (venueId == "archive" && Book != null) Book.ReadRecord(actionId);
                    CompleteChapterAction(venueId, actionId, "");
                    break;
            }
        }

        private void ChapterChoice(string venueId, string actionId, string title, string text, string[] options, int correct, string success, float channelSeconds = 0f)
        {
            if (Game.Menus == null) { if (Game.Hud != null) Game.Hud.Notify("The inspection panel is unavailable. Try again after it recovers.", 4f); return; }
            Game.Menus.ShowChoice(title, text, options, choice =>
            {
                if (!ChapterActionAllowed(venueId, actionId)) return;
                if (choice == correct)
                {
                    if (channelSeconds > 0f) BeginChapterChannel(venueId, actionId, channelSeconds);
                    else CompleteChapterAction(venueId, actionId, success);
                }
                else if (choice != options.Length - 1 && Game.Hud != null) Game.Hud.Notify("Check the written guide and try again. Nothing was spent.", 4f);
            });
        }

        private void ShowRepairChoice(string venueId, string actionId)
        {
            if (Game.Menus == null) return;
            Game.Menus.ShowChoice("A WORKING REPAIR BENCH", "Craft Warding Salve using the existing Forge recipe (base cost $150, 2 Shard Chunks, 2 Hollow Essence; existing discounts apply), or align the loaned calibration tool for free. A previous salve craft also qualifies.",
                new[] { "CRAFT / CHECK SALVE", "CALIBRATE LOANED TOOL", "LEAVE IT" }, choice =>
                {
                    if (!ChapterActionAllowed(venueId, actionId)) return;
                    if (choice == 1) { BeginChapterChannel(venueId, actionId, 4f); return; }
                    if (choice != 0) return;
                    if (Game.Crafting == null) { if (Game.Hud != null) Game.Hud.Notify("The Forge is unavailable. The loaned calibration route remains open.", 4f); return; }
                    if (Game.Crafting.Crafted("brew_warding_salve") > 0) { CompleteChapterAction(venueId, actionId, "The workshop verifies your existing Warding Salve craft."); return; }
                    if (CombatDirector.InCombat) { if (Game.Hud != null) Game.Hud.Notify("The Forge cannot trade during combat. Finish the fight before crafting.", 4f); return; }
                    if (FactionRules.Refuses(Game.Crafting.ForgeRank)) { if (Game.Hud != null) Game.Hud.Notify("Dante will not trade at this reputation. Use the loaned calibration tool.", 4f); return; }
                    string message;
                    if (Game.Crafting.Craft(ForgeDb.Get("brew_warding_salve"), ForgeStation.Forgewright, out message))
                        CompleteChapterAction(venueId, actionId, message);
                    else if (Game.Hud != null) Game.Hud.Notify(message + " The loaned calibration route costs nothing.", 5f);
                });
        }

        private void BeginChapterChannel(string venueId, string actionId, float seconds)
        {
            if (!ChapterActionAllowed(venueId, actionId)) return;
            _channelVenue = venueId;
            _channelAction = actionId;
            _channelSeconds = seconds;
            _channelProgress = 0f;
            _channelLastDamage = Game.Player.Health != null ? Game.Player.Health.LastDamageTime : -99f;
            if (Game.Hud != null) Game.Hud.Notify("Stay beside the marked station while the instrument settles. Moving away or taking damage interrupts it.", 5f);
        }

        private void TickChapterChannel(float delta)
        {
            float lastDamage = Game.Player != null && Game.Player.Health != null ? Game.Player.Health.LastDamageTime : -99f;
            if (!ChapterActionAllowed(_channelVenue, _channelAction) || lastDamage != _channelLastDamage)
            {
                _channelAction = "";
                if (Game.Hud != null) { Game.Hud.ClearCounter(); Game.Hud.Notify("Calibration interrupted. Use the station to try again.", 3f); }
                return;
            }
            _channelProgress += Mathf.Max(0f, delta);
            if (Game.Hud != null) Game.Hud.SetCounter("STEADY CALIBRATION", Mathf.Min(Mathf.FloorToInt(_channelProgress), Mathf.CeilToInt(_channelSeconds)), Mathf.CeilToInt(_channelSeconds));
            if (_channelProgress < _channelSeconds) return;
            string venue = _channelVenue;
            string action = _channelAction;
            _channelAction = "";
            if (Game.Hud != null) Game.Hud.ClearCounter();
            CompleteChapterAction(venue, action, "The instrument settles into a clean, steady signal.");
        }

        private void StartDepotEncounter()
        {
            if (!ChapterActionAllowed("depot", "clear_threat") || IsDepotEncounterActive || Game.Creatures == null) return;
            VenueBinding04C depot = FindVenue("depot");
            int expected = QuietSignal04C.DepotThreatCount((int)Settings.Difficulty);
            Vector3[] points = new Vector3[expected];
            if (!DepotBudgetAvailable(expected)) { Note("The city is at its creature budget. Return later or restart the chapter for the peaceful bypass."); return; }
            for (int i = 0; i < expected; i++)
            {
                if (!TryDepotFloor(depot, i == 0 ? "defense1" : "defense2", out points[i]))
                { Note("A depot spawn point is obstructed. No encounter was started. Clear the room or use the peaceful route."); return; }
            }
            try
            {
                for (int i = 0; i < expected; i++)
                {
                    Vector3 point = points[i];
                    CharacterBase body = Game.Creatures.Spawn("glass_mite", point, 0f, new CreatureSpawnOptions
                    {
                        Tag = DepotTag, Hostile = true, Persistent = true, NoRewards = true, Seed = 41031 + i * 37,
                        HealthScale = Settings.Difficulty == DifficultyLevel.Story ? 0.65f : 1f,
                        DamageScale = Settings.Difficulty == DifficultyLevel.Story ? 0.3f : 0.6f,
                        DisplayName = "Relay Glass Mite"
                    });
                    if (body == null || body.Health == null || Mathf.Abs(body.transform.position.y - point.y) > 1f)
                        throw new InvalidOperationException("A depot body could not occupy its authored floor.");
                    DepotThreat04C threat = new DepotThreat04C { Body = body, Health = body.Health };
                    threat.Died = info => threat.Defeated = true;
                    threat.Health.Died += threat.Died;
                    _depotThreats.Add(threat);
                }
                _depotExpected = expected;
                _depotAge = 0f;
                if (Game.Hud != null) Game.Hud.Notify("Depot encounter accepted: " + expected + " Glass Mite" + (expected == 1 ? "" : "s") + ", no reinforcements or kill rewards. The exit remains open.", 6f);
            }
            catch (Exception exception)
            {
                StopDepotEncounter(true);
                Log.Exception("Quiet Signal depot", exception);
                if (Game.Hud != null) Game.Hud.Notify("The depot encounter could not start safely. Restart this chapter to select the service bypass.", 6f);
            }
        }

        private void TickDepotEncounter(float delta)
        {
            _depotAge += Mathf.Max(0f, delta);
            if (!IsInsideVenue("depot") || _depotAge > 180f)
            {
                StopDepotEncounter();
                if (Game.Hud != null) Game.Hud.Notify("The depot attempt ended. Return to the marked terminal to retry, or restart the chapter for the peaceful route.", 5f);
                return;
            }
            int defeated = 0;
            bool invalid = false;
            VenueBinding04C depot = FindVenue("depot");
            foreach (DepotThreat04C threat in _depotThreats)
            {
                if (threat.Defeated) { defeated++; continue; }
                Creature creature = threat.Body as Creature;
                if (threat.Body == null || threat.Health == null || !threat.Health.IsAlive
                    || (creature != null && !creature.Ready && _depotAge > 18f)
                    || (threat.Body != null && (depot == null || depot.View == null || !depot.View.Contains(threat.Body.transform.position)))) invalid = true;
            }
            if (invalid)
            {
                StopDepotEncounter();
                if (Game.Hud != null) Game.Hud.Notify("An encounter body left its room or failed to load. No completion was awarded; the terminal can retry.", 5f);
                return;
            }
            if (Game.Hud != null) Game.Hud.SetCounter("DEPOT THREATS DEFEATED", defeated, _depotExpected);
            if (!QuietSignal04C.DepotCompleted(_depotExpected, _depotThreats.Count, defeated, false)) return;
            // Defeating the finite bodies records an action, never an extra wallet reward. The relay must still be retrieved.
            if (Book != null) Book.CompleteAction(QuietSignal04C.ActionKey("depot", "clear_threat"));
            StopDepotEncounter();
            if (Game.Hud != null) Game.Hud.Notify("The relay is quiet. Recover it from the marked storage bench.", 5f);
        }

        private void StopDepotEncounter(bool force = false)
        {
            if (!force && _depotExpected == 0 && _depotThreats.Count == 0) return;
            foreach (DepotThreat04C threat in _depotThreats) if (threat.Health != null) threat.Health.Died -= threat.Died;
            _depotThreats.Clear();
            bool wasActive = _depotExpected > 0;
            _depotExpected = 0;
            _depotAge = 0f;
            if (Game.Creatures != null) Game.Creatures.DespawnTag(DepotTag);
            if (wasActive && Game.Hud != null) Game.Hud.ClearCounter();
        }

        private bool DepotBudgetAvailable(int count)
        {
            int groupCap;
            float bodyCap;
            CreatureEcology.Caps(Settings.Graphics, out groupCap, out bodyCap);
            float cost = CreatureEcology.BodyCost(CreatureSpeciesDb.Get("glass_mite")) * count;
            // Live includes waiting, not-yet-ready bodies: reserve the entire finite group before allocating any actor.
            foreach (CharacterBase body in Game.Creatures.Live)
                if (body != null) cost += CreatureEcology.BodyCost(CreatureDirector.SpeciesOf(body));
            return cost <= bodyCap;
        }

        private static bool TryDepotFloor(VenueBinding04C depot, string anchor, out Vector3 point)
        {
            point = depot.Point(anchor);
            RaycastHit hit;
            if (depot.View == null || !depot.View.Contains(point)
                || !Physics.Raycast(point + Vector3.up * .6f, Vector3.down, out hit, 1.2f, Layers.WorldMask, QueryTriggerInteraction.Ignore)
                || Mathf.Abs(hit.point.y - point.y) > .2f || hit.normal.y < .95f) return false;
            float directorGround;
            if (!CreatureDirector.GroundAt(point, 3f, out directorGround) || Mathf.Abs(directorGround - hit.point.y) > .2f) return false;
            int mask = Layers.WorldMask | (1 << Layers.Characters) | (1 << Layers.Vehicles) | (1 << Layers.VehicleShell) | (1 << Layers.Ragdoll);
            if (Physics.CheckCapsule(hit.point + Vector3.up * .5f, hit.point + Vector3.up * 1.05f, .4f, mask, QueryTriggerInteraction.Ignore)) return false;
            point = hit.point;
            return true;
        }

        private void ClearActivities()
        {
            ClearChapterActivity();
            foreach (Interactable reading in _readingPoints)
                if (reading != null) { reading.gameObject.SetActive(false); Destroy(reading.gameObject); }
            _readingPoints.Clear();
        }

        private void ClearChapterActivity()
        {
            if (_chapterAction != null) { _chapterAction.gameObject.SetActive(false); Destroy(_chapterAction.gameObject); }
            _chapterAction = null;
            _chapterKey = "";
            if (!string.IsNullOrEmpty(_channelAction) && Game.Hud != null) Game.Hud.ClearCounter();
            _channelAction = "";
            _channelVenue = "";
            _channelProgress = 0f;
            StopDepotEncounter();
        }
    }
}
