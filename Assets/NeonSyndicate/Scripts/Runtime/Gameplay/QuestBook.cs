using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    [Serializable]
    public sealed class QuestSave
    {
        public List<string> Completed = new List<string>();
        public string Active = "";
        // Appended by the quest runtime (old saves leave them at these defaults).
        /// <summary>Site keys the player has reached (quest requirements such as the Frost Wardens Outpost).</summary>
        public List<string> Visited = new List<string>();
        /// <summary>Skill points earned while no skill tree could take them.</summary>
        public int UnclaimedSkillPoints;
        /// <summary>Contract boards: the guild of each board, its three posted serials (flattened) and contracts done.</summary>
        public List<string> BoardGuilds = new List<string>();
        public List<int> BoardSerials = new List<int>();
        public List<int> ContractsDone = new List<int>();
        /// <summary>Next contract serial (0 in old saves means 1).</summary>
        public int NextSerial;
        /// <summary>In-game day the boards were last refreshed.</summary>
        public int BoardDay;
    }

    /// <summary>
    /// Data-driven quests (Game.Quests): guild arcs and repeatable contracts authored as JSON in Resources/Quests and
    /// run by one generic step runtime (QuestRunner) through the MissionManager, so a quest has the same HUD
    /// objective, GPS target, markers, fail and retry flow as a story mission. Only one runs at a time, never during
    /// a story mission. Saves keep completed quests; an unfinished quest restarts from its beginning at its giver.
    /// </summary>
    public sealed partial class QuestBook : MonoBehaviour, IWorldSystem
    {
        public event Action<string> QuestCompleted;
        /// <summary>Raised when skill points were earned but no skill tree took them (total unclaimed).</summary>
        public event Action<int> SkillPointsPending;

        public readonly List<QuestDef> Quests = new List<QuestDef>();
        public readonly List<GiverDef> Givers = new List<GiverDef>();
        public readonly List<ContractDef> ContractTemplates = new List<ContractDef>();
        /// <summary>Problems found while loading the data (also logged). Shown on the Quests debug page.</summary>
        public readonly List<string> LoadProblems = new List<string>();

        private readonly Dictionary<string, QuestDef> _byId = new Dictionary<string, QuestDef>();
        private readonly HashSet<string> _completed = new HashSet<string>();
        private readonly HashSet<string> _visited = new HashSet<string>();
        private readonly HashSet<string> _announced = new HashSet<string>();
        private string _active = "";
        private string _lastFail = "";
        private QuestRun _run;
        private MissionDef _runDef;
        private QuestDef _pendingOffer;
        private QuestDef _pendingStart;
        private ContractOffer _pendingContract;
        private float _visitTimer;
        private float _refreshTimer;
        private float _quietUntil;
        private bool _loaded;
        /// <summary>Set while Abandon fails the running quest, so the fail does not read as a failed attempt.</summary>
        private bool _abandoning;
        /// <summary>A save was restored: banked skill points go to the skill trees on the next frame (every system has restored by then).</summary>
        private bool _collectBanked;
        private bool _bankedGrantFailed;
        /// <summary>The live reputation lookup handed to QuestProgress (one delegate, not a new one per Progress()).</summary>
        private static readonly Func<string, int> LiveRep = id => Game.Factions != null ? Game.Factions.Rep(id) : 0;

        public int UnclaimedSkillPoints { get; private set; }
        /// <summary>The quest or contract running right now, or null.</summary>
        public QuestRun Running { get { return _run; } }
        /// <summary>Id of the accepted, unfinished guild quest ("" when none). It restarts from the top at its giver.</summary>
        public string ActiveId { get { return _active; } }
        public string LastFailReason { get { return _lastFail; } }

        // ================================================================== boot

        public void Init(CityLayout layout)
        {
            WorldSystems.Register(this);
            try { LoadData(); }
            catch (Exception e) { Log.Exception("Quest data", e); }
            try { InitCampaign04E(); }
            catch (Exception e) { Log.Exception("Underheart campaign", e); }
            try { InitBoards(); }
            catch (Exception e) { Log.Exception("Quest boards", e); }
            try { RegisterUi(); }
            catch (Exception e) { Log.Exception("Quest journal", e); }
        }

        /// <summary>Loads every Resources/Quests file with JsonUtility (the files are checked by QuestValidator in the tests).</summary>
        private void LoadData()
        {
            Quests.Clear();
            Givers.Clear();
            ContractTemplates.Clear();
            Campaigns.Clear();
            _byId.Clear();
            LoadProblems.Clear();
            TextAsset[] files = Resources.LoadAll<TextAsset>("Quests");
            Array.Sort(files, (a, b) => string.CompareOrdinal(a.name, b.name));
            foreach (TextAsset ta in files)
            {
                QuestFile f = null;
                try { f = JsonUtility.FromJson<QuestFile>(ta.text); }
                catch (Exception e) { LoadProblems.Add(ta.name + ": " + e.Message); }
                if (f == null) continue;
                QuestDefaults.Normalize(f);
                foreach (CampaignDef c in f.Campaigns) RegisterCampaign(c);
                foreach (GiverDef g in f.Givers) if (!string.IsNullOrEmpty(g.Guild)) Givers.Add(g);
                foreach (ContractDef c in f.Contracts) if (!string.IsNullOrEmpty(c.Id)) ContractTemplates.Add(c);
                foreach (QuestDef q in f.Quests)
                {
                    if (string.IsNullOrEmpty(q.Id) || q.Phases.Count == 0) { LoadProblems.Add(ta.name + ": a quest without an id or phases was skipped"); continue; }
                    if (_byId.ContainsKey(q.Id)) { LoadProblems.Add(ta.name + ": duplicate quest id " + q.Id); continue; }
                    _byId[q.Id] = q;
                    Quests.Add(q);
                }
            }
            Quests.Sort((a, b) => a.Arc != b.Arc ? string.CompareOrdinal(a.Arc, b.Arc) : a.Order.CompareTo(b.Order));
            foreach (string p in LoadProblems) Log.Warn("Quests: " + p);
            Log.Info("Quests: loaded " + Quests.Count + " quests, " + ContractTemplates.Count + " contract templates and " + Givers.Count + " givers from " + files.Length + " files.");
            _loaded = true;
        }

        // ================================================================== queries

        public bool IsDone(string questId)
        {
            return !string.IsNullOrEmpty(questId) && _completed.Contains(questId);
        }

        public QuestDef Find(string questId)
        {
            QuestDef q;
            return questId != null && _byId.TryGetValue(questId, out q) ? q : null;
        }

        public string TitleOf(string questId)
        {
            QuestDef q = Find(questId);
            return q != null ? q.Title : questId;
        }

        public GiverDef GiverFor(string guild)
        {
            foreach (GiverDef g in Givers) if (g.Guild == guild) return g;
            return null;
        }

        /// <summary>The player's progress as the requirement rules see it.</summary>
        public QuestProgress Progress()
        {
            QuestProgress p = new QuestProgress();
            p.Completed.UnionWith(_completed);
            p.Visited.UnionWith(_visited);
            p.Story = Game.Missions != null ? Game.Missions.StoryProgress : 0;
            p.FreeRoam = Game.Root != null && Game.Root.FreeRoam;
            p.ThirdEye = Game.Abilities != null && Game.Abilities.ThirdEyeUnlocked;
            p.InnerWorld = InnerWorld.Unlocked;
            p.Rep = LiveRep;
            return p;
        }

        /// <summary>Null when the quest can be started now (not done, requirements met), otherwise why not.</summary>
        public string WhyLocked(QuestDef q, QuestProgress p)
        {
            if (q == null) return "Unknown quest";
            if (_completed.Contains(q.Id)) return "Completed";
            if (!string.IsNullOrEmpty(q.VenueGiver))
            {
                if (Game.Inhabited == null || !Game.Inhabited.IsOperational) return "Inhabited city venues are unavailable";
                string unavailable = Game.Inhabited.QuestVenueProblem(q);
                if (unavailable != null) return unavailable;
            }
            string missing = QuestLogic.MissingRequirement(q, p ?? Progress(), TitleOf);
            if (missing != null) return missing;
            // 04E: a campaign quest whose room, Underheart region or system is missing says so instead of starting broken.
            return IsCampaign(q) ? CampaignBlocker(q) : null;
        }

        /// <summary>Every quest that can be started now.</summary>
        public List<QuestDef> Available()
        {
            QuestProgress p = Progress();
            List<QuestDef> list = new List<QuestDef>();
            foreach (QuestDef q in Quests) if (WhyLocked(q, p) == null) list.Add(q);
            return list;
        }

        /// <summary>The quest a guild's giver offers now: the unfinished accepted one first, then the first available in arc order.</summary>
        public QuestDef OfferFor(string guild, QuestProgress p)
        {
            QuestDef active = Find(_active);
            if (active != null && GuildOf(active) == guild && !_completed.Contains(active.Id)) return active;
            foreach (QuestDef q in Quests) if (GuildOf(q) == guild && q.Id != _active && WhyLocked(q, p) == null) return q;
            return null;
        }

        /// <summary>The guild whose giver hands the quest out (its arc), or "" when the quest has its own giver place.</summary>
        public static string GuildOf(QuestDef q)
        {
            return q != null && string.IsNullOrEmpty(q.Giver) ? q.Arc : "";
        }

        /// <summary>True once any quest of the guild's arc is done or available (the guild's board opens then).</summary>
        public bool ArcOpen(string guild, QuestProgress p)
        {
            foreach (QuestDef q in Quests)
            {
                if (GuildOf(q) != guild) continue;
                if (_completed.Contains(q.Id) || WhyLocked(q, p) == null) return true;
            }
            return false;
        }

        public bool HasVisited(string siteKey)
        {
            return _visited.Contains(siteKey);
        }

        /// <summary>Where a quest starts (its giver), in the live world.</summary>
        public Vector3 StartPosition(QuestDef q)
        {
            // 04E: a campaign quest starts at its committed checkpoint's ResumeAt, else at its giver (a cast post or a place token).
            Vector3 cp;
            if (IsCampaign(q) && TryCampaignStart(q, out cp)) return cp;
            if (q != null && !string.IsNullOrEmpty(q.VenueGiver) && Game.Inhabited != null)
            {
                VenueBinding04C venue = Game.Inhabited.FindVenue(q.VenueGiver);
                if (venue != null && venue.Available) return venue.Entrance;
            }
            CityLayout L = Game.Layout;
            Vector3 p;
            if (q != null && !string.IsNullOrEmpty(q.Giver))
            {
                if (QuestPlaces.GiverPosition(L, q.Giver, q.GiverOffsetX, q.GiverOffsetZ, out p)) return p;
            }
            GiverDef g = q != null ? GiverFor(q.Arc) : null;
            if (g != null && QuestPlaces.GiverPosition(L, g.At, g.OffsetX, g.OffsetZ, out p)) return p;
            return Game.PlayerPosition;
        }

        /// <summary>Who hands the quest out: the guild giver's name (also for quests with their own Giver place, where the giver meets you), else the guild.</summary>
        public string GiverName(QuestDef q)
        {
            if (q != null && !string.IsNullOrEmpty(q.Presenter)) return q.Presenter;
            GiverDef g = q != null ? GiverFor(q.Arc) : null;
            if (g != null && !string.IsNullOrEmpty(g.Name)) return g.Name;
            return QuestWorld.FactionName(q != null ? q.Arc : "");
        }

        // ================================================================== starting

        /// <summary>Asks the player to accept a quest (a pausing choice). Accepting starts it next frame.</summary>
        public void Offer(QuestDef q)
        {
            if (q == null || !CanStartNow(true)) return;
            if (!string.IsNullOrEmpty(q.VenueGiver) && (Game.Inhabited == null || Game.Inhabited.QuestVenueProblem(q) != null))
            {
                if (Game.Hud != null) Game.Hud.Notify("This chapter needs a venue that is unavailable. Original services remain open.", 5f);
                return;
            }
            bool resume = q.Id == _active;
            bool campaign = IsCampaign(q);
            string text = campaign ? GiverName(q) + ": " + OfferText(q)
                : q.Summary + "\n\nReward: " + QuestLogic.RewardText(q.Reward) + (resume && !string.IsNullOrEmpty(_lastFail) ? "\nLast attempt: " + _lastFail : "");
            if (Game.Menus == null)
            {
                _pendingStart = q;
                return;
            }
            _pendingOffer = q;
            string accept = campaign && CanResume(q) ? "RESUME" : (resume ? "RESTART" : "ACCEPT");
            Game.Menus.ShowChoice(QuestWorld.FactionName(q.Arc).ToUpperInvariant() + ": " + q.Title, text, new[] { accept, "NOT NOW" }, i =>
            {
                if (i == 0) _pendingStart = _pendingOffer;
                _pendingOffer = null;
            });
        }

        /// <summary>True when nothing blocks starting a quest (no mission running, not wanted, playing).</summary>
        public bool CanStartNow(bool explain)
        {
            if (Game.Missions == null || Game.Player == null || Game.Player.IsDead || !Game.IsPlaying) return false;
            if (Game.Missions.Active != null)
            {
                if (explain && Game.Hud != null) Game.Hud.Notify("Finish \"" + Game.Missions.Active.Def.Title + "\" first.", 2.5f);
                return false;
            }
            if (Game.Wanted != null && Game.Wanted.Stars > 0)
            {
                if (explain && Game.Hud != null) Game.Hud.Notify("Lose the police before you take on a quest.", 3f);
                return false;
            }
            bool encounter = (Game.Territories != null && Game.Territories.HasActiveEncounter)
                || (Game.Strongholds != null && Game.Strongholds.Book != null && Game.Strongholds.Book.ActiveRaid != null)
                || (Game.Events != null && Game.Events.AnyEventActive);
            if (encounter)
            {
                if (explain && Game.Hud != null) Game.Hud.Notify("Finish the active encounter before accepting a quest.", 3f);
                return false;
            }
            return true;
        }

        /// <summary>Next eligible venue offer, including an interrupted quest's restart. Uses the normal quest ledger.</summary>
        public QuestDef NextVenueOffer(string venueId)
        {
            QuestProgress p = Progress();
            foreach (QuestDef q in Quests)
                if (q.VenueGiver == venueId && !_completed.Contains(q.Id) && Game.Inhabited != null && Game.Inhabited.QuestVenueProblem(q) == null
                    && (q.Id == _active || WhyLocked(q, p) == null)) return q;
            return null;
        }

        /// <summary>Starts a quest now through the MissionManager. force skips the requirement check (debug).</summary>
        public bool StartQuest(QuestDef q, bool force)
        {
            if (q == null || !CanStartNow(true)) return false;
            // This finite chapter has a physical display and a single supply allowance; debug force cannot farm it.
            if (QuietSignal04C.IsChapterQuest(q.Id) && _completed.Contains(q.Id)) return false;
            // 04E campaign chapters are finite too (their rewards are once-per-token anyway).
            if (IsCampaign(q) && _completed.Contains(q.Id)) return false;
            if (!string.IsNullOrEmpty(q.VenueGiver) && (Game.Inhabited == null || Game.Inhabited.QuestVenueProblem(q) != null))
            {
                if (Game.Hud != null) Game.Hud.Notify("This chapter needs a venue that is unavailable. Original services remain open.", 5f);
                return false;
            }
            if (!force)
            {
                string why = WhyLocked(q, null);
                if (why != null && q.Id != _active)
                {
                    if (Game.Hud != null) Game.Hud.Notify(why + ".", 3f);
                    return false;
                }
            }
            _active = q.Id;
            _lastFail = "";
            MissionDef def = BuildMission(q, false);
            Game.Missions.StartMission(def);
            return true;
        }

        private MissionDef BuildMission(QuestDef q, bool contract)
        {
            MissionDef def = new MissionDef
            {
                Id = (contract ? "" : "quest_") + q.Id,
                Title = q.Title,
                Chapter = QuestWorld.FactionName(q.Arc) + (contract ? " Contract" : ""),
                Contact = GiverName(q),
                ContactLetter = "Q",
                Summary = q.Summary,
                Reward = Mathf.Max(0, q.Reward.Cash),
                Story = false,
                PassTitle = contract ? "CONTRACT COMPLETE" : "QUEST COMPLETE",
                FailTitle = contract ? "CONTRACT FAILED" : "QUEST FAILED"
            };
            Vector3 start = StartPosition(q);
            // A campaign quest's start moves with its checkpoint (a retry resumes at the latest committed phase).
            if (!contract && IsCampaign(q)) def.StartPosition = () => StartPosition(q);
            else def.StartPosition = () => start;
            def.Script = c => RunScript(q, c, contract);
            def.OnPass = () => OnPassed(q, contract);
            def.OnFail = reason => OnFailed(q, reason, contract);
            def.OnCleanup = (c, passed) => OnCleanedUp(q, c, passed);
            _runDef = def;
            return def;
        }

        private System.Collections.IEnumerator RunScript(QuestDef q, MissionContext c, bool contract)
        {
            _run = new QuestRun(this, q, c, contract, StartPosition(q));
            if (!contract && IsCampaign(q))
            {
                _run.StartPhase = ResumePhaseIndex(q);
                // Every start (accept, resume or Retry) keeps the quest's cast at their posts until it ends.
                ReserveCast(q);
            }
            return QuestRunner.Run(_run);
        }

        private void OnPassed(QuestDef q, bool contract)
        {
            // Completed quest ids are the durable payout token. Duplicate callbacks cannot grant twice.
            if (!contract && _completed.Contains(q.Id)) return;
            try { QuestWorld.Grant(q.Reward, q.Title, false); }
            catch (Exception e) { Log.Exception("Quest reward " + q.Id, e); }
            _quietUntil = Time.time + 9f;
            if (contract)
            {
                // Contracts are repeatable jobs, not quests: no QuestCompleted for their generated ids.
                if (IsCampaignContract(q)) CampaignContractFinished(q);
                else ContractFinished(q);
                return;
            }
            if (IsCampaign(q)) CampaignRules04E.ClearCheckpoint(_campaign.Checkpoints, q.Id);
            _completed.Add(q.Id);
            if (_active == q.Id) _active = "";
            _lastFail = "";
            if (Game.Hud != null && !string.IsNullOrEmpty(q.Done)) Game.Hud.Notify(q.Done, 6f);
            RaiseCompleted(q.Id);
        }

        private void OnFailed(QuestDef q, string reason, bool contract)
        {
            // Only the accepted guild quest has a "Last attempt"; a failed contract or an abandoned quest leaves it alone.
            if (contract || _abandoning) return;
            _lastFail = reason ?? "";
            if (Game.Hud == null) return;
            string resume = IsCampaign(q) ? ResumeLabel(q) : "";
            if (resume.Length > 0) Game.Hud.Notify("\"" + q.Title + "\" resumes at its checkpoint (" + resume + "): retry now, use the resume marker, the giver or the JOURNAL.", 6f);
            else Game.Hud.Notify("\"" + q.Title + "\" can be restarted at " + GiverName(q) + " or from the JOURNAL.", 5f);
        }

        private void OnCleanedUp(QuestDef q, MissionContext c, bool passed)
        {
            QuestRun r = _run;
            if (r != null && r.C == c)
            {
                r.Release();
                _run = null;
            }
            ReleaseCast(q);
        }

        public void RaiseCompleted(string questId)
        {
            if (QuestCompleted == null) return;
            foreach (Delegate d in QuestCompleted.GetInvocationList())
            {
                try { ((Action<string>)d)(questId); }
                catch (Exception e) { Log.Exception("QuestCompleted listener", e); }
            }
        }

        /// <summary>
        /// Walks away: stops the running quest or contract (no retry prompt, no "can be restarted" note) and forgets the
        /// accepted guild quest. Abandoning a running contract keeps the accepted guild quest.
        /// </summary>
        public void Abandon()
        {
            bool contract = _run != null && _run.Contract;
            if (_run != null && Game.Missions != null && Game.Missions.Active != null && Game.Missions.Active.Def == _runDef)
            {
                _abandoning = true;
                try { Game.Missions.Fail("You walked away from the job."); }
                finally { _abandoning = false; }
                Game.Missions.CancelRetry();
            }
            if (contract) return;
            _active = "";
            _lastFail = "";
        }

        // ================================================================== skill points

        /// <summary>Kept for Game.ResetStatics: the quest book's only static is the stateless LiveRep lookup.</summary>
        public static void ResetStatics()
        {
        }

        /// <summary>
        /// Gives skill points through Game.Skills.Grant(int); if no skill system is running they wait
        /// as UnclaimedSkillPoints (saved) and SkillPointsPending is raised so a skill system can collect them with
        /// TakeUnclaimedSkillPoints.
        /// </summary>
        public void GrantSkillPoints(int points, string reason)
        {
            if (points <= 0) return;
            if (Game.Skills != null)
            {
                // Points banked while no skill system was present are handed over too. Game.Skills.Grant shows the notice.
                int banked = TakeUnclaimedSkillPoints();
                try
                {
                    Game.Skills.Grant(points + banked);
                    return;
                }
                catch (Exception e)
                {
                    UnclaimedSkillPoints += banked;
                    Log.Exception("Skill point grant", e);
                }
            }
            UnclaimedSkillPoints += points;
            if (Game.Hud != null) Game.Hud.Notify("+" + points + (points == 1 ? " skill point" : " skill points") + " banked for the skill trees.", 4f);
            if (SkillPointsPending != null)
            {
                try { SkillPointsPending(UnclaimedSkillPoints); }
                catch (Exception e) { Log.Exception("SkillPointsPending listener", e); }
            }
        }

        /// <summary>Hands every banked skill point to the caller (a skill system) and clears the bank.</summary>
        public int TakeUnclaimedSkillPoints()
        {
            int n = UnclaimedSkillPoints;
            UnclaimedSkillPoints = 0;
            return n;
        }

        /// <summary>
        /// Banked points (earned while no skill tree could take them, or restored from a save) go to Game.Skills as soon
        /// as it runs. Called a frame after a restore and once a second; a grant that throws keeps the points banked and
        /// is not retried this session.
        /// </summary>
        private void CollectBankedSkillPoints()
        {
            if (UnclaimedSkillPoints <= 0 || Game.Skills == null || _bankedGrantFailed) return;
            int n = TakeUnclaimedSkillPoints();
            try { Game.Skills.Grant(n); }
            catch (Exception e)
            {
                UnclaimedSkillPoints += n;
                _bankedGrantFailed = true;
                Log.Exception("Banked skill points", e);
            }
        }

        // ================================================================== frame

        private void Update()
        {
            if (!_loaded || Game.Layout == null) return;
            if (_pendingStart != null)
            {
                QuestDef q = _pendingStart;
                _pendingStart = null;
                StartQuest(q, false);
            }
            if (_pendingContract != null)
            {
                ContractOffer o = _pendingContract;
                _pendingContract = null;
                StartContract(o);
            }
            bool live = Game.Player != null && Game.State != GameState.Title && Game.State != GameState.Loading;
            if (!live)
            {
                HideMarkers();
                return;
            }
            float dt = Time.deltaTime;
            try { UpdateCampaign04E(dt); }
            catch (Exception e) { Log.Exception("Underheart campaign", e); }
            _visitTimer -= dt;
            if (_visitTimer <= 0f)
            {
                _visitTimer = 0.5f;
                TrackVisits();
            }
            if (_collectBanked)
            {
                _collectBanked = false;
                CollectBankedSkillPoints();
            }
            _refreshTimer -= dt;
            if (_refreshTimer <= 0f)
            {
                _refreshTimer = 1f;
                try { RefreshMarkers(); }
                catch (Exception e) { Log.Exception("Quest markers", e); }
                try { AnnounceUnlocks(); }
                catch (Exception e) { Log.Exception("Quest unlocks", e); }
                CollectBankedSkillPoints();
            }
        }

        private void TrackVisits()
        {
            Vector3 p = Game.PlayerPosition;
            foreach (SpecialSite s in Game.Layout.Sites)
            {
                if (_visited.Contains(s.Key)) continue;
                if (MathUtil.FlatDistance(p, s.Position) < s.Radius + 5f) _visited.Add(s.Key);
            }
        }

        /// <summary>Tells the player about quests that just became available (quietly, never over a mission).</summary>
        private void AnnounceUnlocks()
        {
            if (Game.Missions != null && Game.Missions.Active != null) return;
            if (Time.time < _quietUntil || !Game.IsPlaying) return;
            QuestProgress p = Progress();
            foreach (QuestDef q in Quests)
            {
                if (_announced.Contains(q.Id) || WhyLocked(q, p) != null) continue;
                _announced.Add(q.Id);
                if (Game.Hud != null)
                {
                    VenueBinding04C venue = Game.Inhabited != null ? Game.Inhabited.FindVenue(q.VenueGiver) : null;
                    string place = string.IsNullOrEmpty(q.VenueGiver) ? QuestWorld.FactionName(q.Arc) + ", Q on the map"
                        : "inside " + (venue != null ? venue.Definition.Title : q.VenueGiver);
                    Game.Hud.Notify("New quest: \"" + q.Title + "\" from " + GiverName(q) + " (" + place + ").", 6f);
                    _quietUntil = Time.time + 7f;
                }
                return;
            }
        }

        /// <summary>After a load: everything already available counts as announced, so loading does not spam.</summary>
        private void MarkAllAnnounced()
        {
            _announced.Clear();
            QuestProgress p = Progress();
            foreach (QuestDef q in Quests) if (WhyLocked(q, p) == null) _announced.Add(q.Id);
        }

        // ================================================================== IWorldSystem

        public void ResetAll()
        {
            _completed.Clear();
            _visited.Clear();
            _announced.Clear();
            _active = "";
            _lastFail = "";
            UnclaimedSkillPoints = 0;
            _collectBanked = false;
            _bankedGrantFailed = false;
            _pendingOffer = null;
            _pendingStart = null;
            _pendingContract = null;
            _quietUntil = Time.time + 20f;
            ResetBoards();
            HideMarkers();
            RestoreCampaign04E(null);
        }

        public void Capture(SaveData d)
        {
            QuestSave s = new QuestSave();
            foreach (QuestDef q in Quests) if (_completed.Contains(q.Id)) s.Completed.Add(q.Id);
            // Ids from data that no longer ships are kept too, so removing a quest file never loses progress.
            foreach (string id in _completed) if (!s.Completed.Contains(id)) s.Completed.Add(id);
            s.Active = _active ?? "";
            s.Visited.AddRange(_visited);
            s.UnclaimedSkillPoints = UnclaimedSkillPoints;
            CaptureBoards(s);
            d.Quests = s;
            CaptureCampaign04E(d);
        }

        public void Restore(SaveData d)
        {
            ResetAll();
            QuestSave s = d != null ? d.Quests : null;
            if (s == null)
            {
                RestoreCampaign04E(d);
                return;
            }
            if (s.Completed != null) foreach (string id in s.Completed) if (!string.IsNullOrEmpty(id)) _completed.Add(id);
            if (s.Visited != null) foreach (string k in s.Visited) if (!string.IsNullOrEmpty(k)) _visited.Add(k);
            _active = s.Active != null && Find(s.Active) != null && !_completed.Contains(s.Active) ? s.Active : "";
            UnclaimedSkillPoints = Mathf.Max(0, s.UnclaimedSkillPoints);
            // Handed to the skill trees next frame rather than here: every system (the skill trees included) has restored by then.
            _collectBanked = UnclaimedSkillPoints > 0;
            RestoreBoards(s);
            // After the completed set: checkpoints of finished or unknown quests are dropped.
            RestoreCampaign04E(d);
            MarkAllAnnounced();
            _quietUntil = Time.time + 15f;
        }
    }
}
