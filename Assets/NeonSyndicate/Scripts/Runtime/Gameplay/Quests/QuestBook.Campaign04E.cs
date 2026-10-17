using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Underheart and Bonds 04E campaign support on the existing quest book: persistent campaign choices and flags, safe
    /// phase checkpoints for long chapters (a restarted or retried quest resumes at its last committed checkpoint phase,
    /// so earlier steps and rewards never repeat), giver reservations, the offer text, campaign availability reasons, a
    /// retry queue for rewards granted while the reward ledger was missing, and the campaign block of the save.
    /// </summary>
    public sealed partial class QuestBook
    {
        /// <summary>Raised when a campaign choice, flag or checkpoint changes (argument: what changed, for example "choice:juveniles").</summary>
        public event Action<string> CampaignChanged;

        /// <summary>The campaigns declared by the loaded quest files (Arc ids that are not guilds).</summary>
        public readonly List<CampaignDef> Campaigns = new List<CampaignDef>();

        private readonly CampaignSave _campaign = new CampaignSave();
        private readonly Dictionary<string, CampaignDeps> _deps = new Dictionary<string, CampaignDeps>();
        private readonly Dictionary<string, List<string>> _reserved = new Dictionary<string, List<string>>();
        private readonly CampaignFacts _facts = new CampaignFacts();
        private bool _checkpointSafe;
        private bool _factsReady;
        private QuestRun _deferredSaveRun;
        private int _deferredSavePhase = -1;
        private float _deferredSaveAt = -1f;
        private float _pendingGrantRetry;
        private Interactable _resumeUse;
        private Blip _resumeBlip;
        private GameObject _resumeRoot;
        private string _resumeFor = "";
        private Vector3 _resumePos;
        private float _resumeCheck;

        /// <summary>What a campaign quest needs from other systems (worked out once from its data).</summary>
        private sealed class CampaignDeps
        {
            public bool Underheart;
            public bool Rescue;
            public bool Fusion;
            public bool Bosses;
            public readonly List<string> Venues = new List<string>();
            public readonly List<string> Cast = new List<string>();
        }

        // ================================================================== choices, flags, checkpoints (public API)

        /// <summary>A persistent campaign decision ("" when not made). Keys and values in QuestIds04E.</summary>
        public string Choice(string key)
        {
            return CampaignRules04E.GetChoice(_campaign.Choices, key);
        }

        /// <summary>Records a campaign decision (the first answer sticks unless overwrite is true). Invalid pairs are ignored.</summary>
        public void SetChoice(string key, string value, bool overwrite)
        {
            if (!CampaignRules04E.SetChoice(_campaign.Choices, key, value, overwrite)) return;
            Log.Info("Campaign: choice " + key + " = " + value + ".");
            RaiseCampaignChanged("choice:" + key);
        }

        /// <summary>True once a campaign flag is set (finished Choir mechanisms, optional rescues, the endgame board).</summary>
        public bool Flag(string name)
        {
            return !string.IsNullOrEmpty(name) && _campaign.Flags.Contains(name);
        }

        /// <summary>Sets a campaign flag (permanent for this save). True when it was new.</summary>
        public bool SetFlag(string name)
        {
            if (!CampaignRules04E.IsName(name) || _campaign.Flags.Contains(name)) return false;
            _campaign.Flags.Add(name);
            RaiseCampaignChanged("flag:" + name);
            return true;
        }

        /// <summary>True while the running quest sits at a committed checkpoint and may be autosaved safely.</summary>
        public bool CheckpointSafeNow { get { return _checkpointSafe; } }

        /// <summary>True for Underheart campaign content (main or side quests).</summary>
        public static bool IsCampaign(QuestDef q) { return q != null && q.Id != null && q.Id.StartsWith("uh05_", StringComparison.Ordinal); }

        /// <summary>The committed checkpoint phase id of a quest ("" when none).</summary>
        public string CheckpointOf(string questId)
        {
            return CampaignRules04E.CheckpointPhase(_campaign.Checkpoints, questId);
        }

        /// <summary>True for a checkpointed campaign quest that is accepted and unfinished: starting it resumes at the checkpoint.</summary>
        public bool CanResume(QuestDef q)
        {
            if (!IsCampaign(q) || _completed.Contains(q.Id)) return false;
            if (_run != null && _run.Quest == q) return false;
            return QuestLogic.PhaseIndex(q, CheckpointOf(q.Id)) > 0;
        }

        /// <summary>The title of the phase a resumed quest starts at ("" when it starts from the beginning).</summary>
        public string ResumeLabel(QuestDef q)
        {
            int i = q != null ? QuestLogic.PhaseIndex(q, CheckpointOf(q.Id)) : -1;
            if (i <= 0) return "";
            string t = q.Phases[i].Title;
            return string.IsNullOrEmpty(t) ? q.Phases[i].Id : t;
        }

        /// <summary>
        /// Resumes a checkpointed quest (the journal's button). It starts on the next frame, after the menu has closed, at the
        /// committed checkpoint phase; earlier steps and rewards never repeat. False (with a HUD reason) when it cannot start.
        /// </summary>
        public bool ResumeQuest(QuestDef q)
        {
            if (!CanResume(q)) return false;
            if (!CanStartNow(true)) return false;
            string why = WhyLocked(q, null);
            if (why != null && q.Id != _active)
            {
                if (Game.Hud != null) Game.Hud.Notify(why + ".", 3f);
                return false;
            }
            _pendingStart = q;
            return true;
        }

        /// <summary>The live facts campaign conditions read (one shared instance, refreshed on every call).</summary>
        public CampaignFacts Facts()
        {
            if (!_factsReady)
            {
                _factsReady = true;
                _facts.Choice = k => Choice(k);
                _facts.Flag = n => Flag(n);
                _facts.OwnsBond = a => Game.Bonds != null && Game.Bonds.Owns(a);
                _facts.QuestDone = id => IsDone(id);
                _facts.Material = m => Game.Materials != null ? Game.Materials.Count(m) : 0;
            }
            _facts.Law = Game.Abilities != null ? Game.Abilities.Law ?? "" : "";
            _facts.StoryDone = Game.Missions != null && Game.Missions.Story.Count > 0 && Game.Missions.StoryProgress >= Game.Missions.Story.Count;
            _facts.FreeRoam = Game.Root != null && Game.Root.FreeRoam;
            _facts.Fused = Game.Fusion != null && Game.Fusion.IsFused;
            _facts.FusionUnlocked = Game.Garage != null && Game.Garage.FusionUnlocked;
            return _facts;
        }

        private void RaiseCampaignChanged(string what)
        {
            if (CampaignChanged == null) return;
            foreach (Delegate d in CampaignChanged.GetInvocationList())
            {
                try { ((Action<string>)d)(what); }
                catch (Exception e) { Log.Exception("CampaignChanged listener", e); }
            }
        }

        // ================================================================== boot

        /// <summary>Registers a loaded campaign declaration (titles and colours for the tracker, journal and offers).</summary>
        private void RegisterCampaign(CampaignDef c)
        {
            if (c == null || string.IsNullOrEmpty(c.Id)) return;
            foreach (CampaignDef k in Campaigns) if (k.Id == c.Id) return;
            Campaigns.Add(c);
            CampaignRegistry04E.Register(c);
        }

        /// <summary>After the data loaded: dependencies, the place-token resolver, the board and the STATUS row.</summary>
        private void InitCampaign04E()
        {
            QuestPlaces.TokenResolver = CampaignPlacesRuntime04E.TryResolveForPlaces;
            _deps.Clear();
            int main = 0, side = 0;
            foreach (QuestDef q in Quests)
            {
                if (!IsCampaign(q)) continue;
                _deps[q.Id] = ScanDeps(q);
                if (q.Kind == "Main") main++;
                else if (q.Kind == "Side") side++;
            }
            int templates = 0;
            foreach (ContractDef t in ContractTemplates) if (CampaignRegistry04E.Has(t.Guild)) templates++;
            try { RegisterCampaignDebug(); }
            catch (Exception e) { Log.Exception("Campaign debug page", e); }
            bool whole = main == QuestIds04E.Main.Length && side == QuestIds04E.Side.Length && templates >= QuestIds04E.Contracts.Length;
            ModuleStatus.Report("campaign_04e", "Underheart campaign", whole ? ModuleState.Loaded : ModuleState.Partial,
                whole ? "" : "Loaded " + main + " of " + QuestIds04E.Main.Length + " main quests, " + side + " of " + QuestIds04E.Side.Length + " side quests and " + templates + " contract grammars.",
                whole ? "" : "F2 > Campaign lists what loaded; check the quest files.");
            ModuleStatus.SetCount("campaign_04e", main + side, "campaign quests loaded");
            Log.Info("Campaign: " + main + " main quests, " + side + " side quests, " + templates + " contract grammars.");
        }

        private static CampaignDeps ScanDeps(QuestDef q)
        {
            CampaignDeps d = new CampaignDeps();
            AddPlaceDep(d, q.Giver);
            foreach (QuestPhase ph in q.Phases)
            {
                AddPlaceDep(d, ph.ResumeAt);
                foreach (QuestStep s in ph.Steps)
                {
                    AddPlaceDep(d, s.At);
                    AddPlaceDep(d, s.To);
                    foreach (string p in s.Points) AddPlaceDep(d, p);
                    switch (s.Type)
                    {
                        case "Rescue": d.Rescue = true; break;
                        case "Fuse":
                        case "Course":
                        case "Park": d.Fusion = true; break;
                        case "UBoss":
                        case "Mechanism": d.Bosses = true; d.Underheart = true; break;
                        case "Skirmish":
                        case "Shortcut": d.Underheart = true; break;
                    }
                }
            }
            return d;
        }

        private static void AddPlaceDep(CampaignDeps d, string at)
        {
            if (string.IsNullOrEmpty(at)) return;
            if (at.StartsWith(CampaignRules04E.UnderPrefix, StringComparison.Ordinal)) d.Underheart = true;
            string v, a;
            if (CampaignRules04E.SplitVenue(at, out v, out a) && !d.Venues.Contains(v)) d.Venues.Add(v);
            string c = CampaignRules04E.CastOf(at);
            if (c.Length > 0 && !d.Cast.Contains(c)) d.Cast.Add(c);
        }

        // ================================================================== availability

        /// <summary>Why a campaign quest cannot run right now because a system or place it needs is missing (null when nothing is).</summary>
        private string CampaignBlocker(QuestDef q)
        {
            CampaignDeps d;
            if (q == null || !_deps.TryGetValue(q.Id, out d)) return null;
            if (d.Underheart)
            {
                if (Game.Underheart == null) return "The Underheart did not start (see STATUS)";
                if (!Game.Underheart.Ready) return "The Underheart is not available: " + Game.Underheart.Status;
            }
            if (d.Venues.Count > 0)
            {
                if (Game.Inhabited == null || !Game.Inhabited.IsOperational) return "The inhabited venues are unavailable (see STATUS)";
                foreach (string v in d.Venues)
                {
                    VenueBinding04C b = Game.Inhabited.FindVenue(v);
                    if (b == null) return CampaignPlacesRuntime04E.VenueTitle(v) + " is not in this build";
                    if (!b.Available) return CampaignPlacesRuntime04E.VenueTitle(v) + " is unavailable: " + b.Status;
                }
            }
            if (d.Rescue && Game.Bonds == null) return "Bonded companions did not start (see STATUS)";
            if (d.Fusion && (Game.Fusion == null || Game.Garage == null)) return "Creature-vehicle fusion did not start (see STATUS)";
            if (d.Bosses && Game.UnderBosses == null) return "The Underheart bosses did not start (see STATUS)";
            return null;
        }

        // ================================================================== start, resume and reservations

        /// <summary>Where a campaign quest starts: its checkpoint's ResumeAt when resuming, else its giver. False when unresolved.</summary>
        private bool TryCampaignStart(QuestDef q, out Vector3 pos)
        {
            pos = Vector3.zero;
            int i = QuestLogic.PhaseIndex(q, CheckpointOf(q.Id));
            if (i > 0 && !string.IsNullOrEmpty(q.Phases[i].ResumeAt) && CampaignPlacesRuntime04E.TryResolveAny(q.Phases[i].ResumeAt, out pos)) return true;
            return CampaignPlacesRuntime04E.TryResolveAny(q.Giver, out pos);
        }

        /// <summary>The phase a starting campaign quest begins at (its committed checkpoint, else 0).</summary>
        private int ResumePhaseIndex(QuestDef q)
        {
            if (!IsCampaign(q)) return 0;
            return Mathf.Max(0, QuestLogic.PhaseIndex(q, CheckpointOf(q.Id)));
        }

        /// <summary>Keeps every cast member the quest names (its giver and anyone it talks to) at their posts while it runs.</summary>
        private void ReserveCast(QuestDef q)
        {
            CampaignDeps d;
            if (q == null || Game.Campaign == null || !_deps.TryGetValue(q.Id, out d) || d.Cast.Count == 0) return;
            List<string> held = new List<string>();
            foreach (string c in d.Cast)
            {
                try
                {
                    Game.Campaign.Reserve(c, q.Id);
                    held.Add(c);
                }
                catch (Exception e) { Log.Exception("Cast reservation " + c, e); }
            }
            _reserved[q.Id] = held;
        }

        private void ReleaseCast(QuestDef q)
        {
            List<string> held;
            if (q == null || !_reserved.TryGetValue(q.Id, out held)) return;
            _reserved.Remove(q.Id);
            if (Game.Campaign == null) return;
            foreach (string c in held)
            {
                try { Game.Campaign.Release(c, q.Id); }
                catch (Exception e) { Log.Exception("Cast release " + c, e); }
            }
        }

        // ================================================================== checkpoints

        /// <summary>
        /// The runner entered a Checkpoint phase: commit "questId:phaseId", then ask for an autosave while CheckpointSafeNow is
        /// true. The save holds completed objectives, claims and choices, never the run's live objects. A second ask follows
        /// a few seconds later when the run is still in this phase, because the autosave refuses to run twice within 4 s.
        /// </summary>
        internal void CommitCheckpoint04E(QuestRun r, int phaseIndex)
        {
            if (r == null || r.Contract || !IsCampaign(r.Quest) || phaseIndex < 0 || phaseIndex >= r.Quest.Phases.Count) return;
            QuestPhase ph = r.Quest.Phases[phaseIndex];
            bool changed = CampaignRules04E.CommitCheckpoint(_campaign.Checkpoints, r.Quest.Id, ph.Id);
            if (changed)
            {
                Log.Info("Campaign: checkpoint " + r.Quest.Id + ":" + ph.Id + ".");
                RaiseCampaignChanged("checkpoint:" + r.Quest.Id);
                if (Game.Hud != null) Game.Hud.Notify("Checkpoint: " + (string.IsNullOrEmpty(ph.Title) ? ph.Id : ph.Title) + ". A restart resumes here.", 3f);
            }
            AskCheckpointSave();
            _deferredSaveRun = r;
            _deferredSavePhase = phaseIndex;
            _deferredSaveAt = Time.unscaledTime + 4.4f;
        }

        private void AskCheckpointSave()
        {
            if (Game.Root == null) return;
            _checkpointSafe = true;
            try { Game.Root.Autosave(); }
            catch (Exception e) { Log.Exception("Checkpoint autosave", e); }
            finally { _checkpointSafe = false; }
        }

        // ================================================================== offers

        /// <summary>The offer card text for a campaign quest: purpose, place and entrance, risk, rewards, and where a restart resumes.</summary>
        public string OfferText(QuestDef q)
        {
            if (q == null) return "";
            StringBuilder sb = new StringBuilder();
            sb.Append(q.Summary);
            sb.Append("\n<b>Where:</b> ").Append(string.IsNullOrEmpty(q.Floor) ? "Las Veyra" : q.Floor);
            if (!string.IsNullOrEmpty(q.Entrance)) sb.Append(". Enter at ").Append(q.Entrance);
            sb.Append(".   <b>Risk:</b> ").Append(CampaignRules04E.DifficultyLabel(q.Difficulty));
            sb.Append(" (").Append(q.Kind == "Main" ? "main quest, act " + q.Act : "side quest").Append(')');
            string claims = CampaignRules04E.ClaimsText(q.Claims);
            string pay = QuestLogic.RewardText(q.Reward);
            sb.Append("\n<b>Rewards:</b> ").Append(pay);
            if (claims.Length > 0) sb.Append(pay.Length > 0 ? "; " : "").Append(claims);
            string resume = ResumeLabel(q);
            if (resume.Length > 0) sb.Append("\n<b>Resumes at:</b> ").Append(resume).Append(" (checkpoint)");
            if (q.Id == _active && !string.IsNullOrEmpty(_lastFail)) sb.Append("\nLast attempt: ").Append(_lastFail);
            return sb.ToString();
        }

        // ================================================================== rewards the ledger could not take yet

        /// <summary>Queues a ledger grant made while the reward ledger was missing (granted once it is back; never lost).</summary>
        internal void QueueLedgerGrant(string rewardId, string token, string source)
        {
            string rec = rewardId + "|" + token + "|" + (source ?? "").Replace("|", "/");
            foreach (string s in _campaign.PendingGrants) if (s.StartsWith(rewardId + "|" + token + "|", StringComparison.Ordinal)) return;
            _campaign.PendingGrants.Add(rec);
        }

        /// <summary>Retries queued grants. A record leaves the queue only once the ledger really holds its token; returns false when some remain.</summary>
        private bool RetryPendingGrants()
        {
            if (_campaign.PendingGrants.Count == 0) return true;
            if (Game.Builds == null || Game.Builds.Ledger == null) return false;
            RewardLedger ledger = Game.Builds.Ledger;
            for (int i = _campaign.PendingGrants.Count - 1; i >= 0; i--)
            {
                string[] parts = _campaign.PendingGrants[i].Split('|');
                if (parts.Length < 2) { _campaign.PendingGrants.RemoveAt(i); continue; }
                try
                {
                    string msg = "";
                    if (!ledger.Has(parts[1])) ledger.GrantAndClaim(parts[0], parts[1], parts.Length > 2 ? parts[2] : "Underheart", out msg);
                    if (!ledger.Has(parts[1])) continue;
                    _campaign.PendingGrants.RemoveAt(i);
                    if (Game.Hud != null && !string.IsNullOrEmpty(msg)) Game.Hud.Notify(msg, 4f);
                }
                catch (Exception e)
                {
                    Log.Exception("Campaign pending grant " + parts[0], e);
                    return false;
                }
            }
            return _campaign.PendingGrants.Count == 0;
        }

        // ================================================================== resume point

        /// <summary>
        /// While a checkpointed campaign quest is accepted but not running, a resume marker and prompt stand at the
        /// checkpoint's ResumeAt place (often the rest point an underground save loads at), so the player never has to walk
        /// back to the giver to continue. Pressing E opens the normal offer card with RESUME.
        /// </summary>
        private void UpdateResumePrompt(float dt)
        {
            _resumeCheck -= dt;
            if (_resumeCheck > 0f) return;
            _resumeCheck = 1f;
            QuestDef q = Find(_active);
            bool want = q != null && CanResume(q) && (Game.Missions == null || Game.Missions.Active == null);
            Vector3 p = Vector3.zero;
            if (want)
            {
                int i = QuestLogic.PhaseIndex(q, CheckpointOf(q.Id));
                want = i > 0 && CampaignPlacesRuntime04E.TryResolveAny(q.Phases[i].ResumeAt, out p);
            }
            if (!want)
            {
                HideResumePrompt();
                return;
            }
            if (_resumeRoot != null && _resumeFor == q.Id && (_resumePos - p).sqrMagnitude < 0.25f) return;
            HideResumePrompt();
            _resumeFor = q.Id;
            _resumePos = p;
            Color c = QuestWorld.FactionColor(q.Arc);
            _resumeRoot = new GameObject("Campaign Resume Point");
            _resumeRoot.transform.SetParent(transform, false);
            WorldMarker.Create(p, c, 0.9f, _resumeRoot.transform, true);
            _resumeUse = Interactable.Create("Campaign resume", p, 2.4f, "", () =>
            {
                QuestDef rq = Find(_resumeFor);
                if (rq != null && CanResume(rq)) Offer(rq);
            }, _resumeRoot.transform);
            _resumeUse.DynamicPrompt = () =>
            {
                QuestDef rq = Find(_resumeFor);
                return rq != null ? "Press E to resume " + rq.Title + " (" + ResumeLabel(rq) + ")" : "";
            };
            _resumeUse.Condition = () => Game.Missions == null || Game.Missions.Active == null;
            _resumeBlip = Blip.Add(BlipKind.Activity, null, p, c, "R", "Resume: " + q.Title, false);
        }

        private void HideResumePrompt()
        {
            if (_resumeBlip != null) Blip.Remove(_resumeBlip);
            _resumeBlip = null;
            if (_resumeRoot != null) UnityEngine.Object.Destroy(_resumeRoot);
            _resumeRoot = null;
            _resumeUse = null;
            _resumeFor = "";
        }

        // ================================================================== frame

        /// <summary>Called from Update while the game is live: deferred checkpoint saves, queued grants, the board and debug starts.</summary>
        private void UpdateCampaign04E(float dt)
        {
            if (_deferredSaveAt > 0f && Time.unscaledTime >= _deferredSaveAt)
            {
                _deferredSaveAt = -1f;
                QuestRun r = _deferredSaveRun;
                _deferredSaveRun = null;
                if (r != null && r == _run && r.Phase == _deferredSavePhase) AskCheckpointSave();
            }
            _pendingGrantRetry -= dt;
            if (_pendingGrantRetry <= 0f) _pendingGrantRetry = RetryPendingGrants() ? 2f : 30f;
            try { UpdateResumePrompt(dt); }
            catch (Exception e) { Log.Exception("Campaign resume point", e); }
            try { UpdateCampaignBoard(dt); }
            catch (Exception e) { Log.Exception("Lantern Refuge board", e); }
            UpdateCampaignDebugStart();
        }

        // ================================================================== save

        /// <summary>Called by Capture: writes the campaign block.</summary>
        private void CaptureCampaign04E(SaveData d)
        {
            if (d == null) return;
            CampaignSave s = new CampaignSave { Version = 2 };
            s.Checkpoints.AddRange(_campaign.Checkpoints);
            s.Choices.AddRange(_campaign.Choices);
            s.Flags.AddRange(_campaign.Flags);
            s.PendingGrants.AddRange(_campaign.PendingGrants);
            s.BoardSerials.AddRange(_campaign.BoardSerials);
            s.BoardNextSerial = _campaign.BoardNextSerial;
            s.ContractsDone = _campaign.ContractsDone;
            s.BoardPlaySeconds = _campaign.BoardPlaySeconds;
            d.Campaign04E = s;
        }

        /// <summary>Called by Restore and ResetAll (d is null on a new game). Missing or malformed records normalize safely.</summary>
        private void RestoreCampaign04E(SaveData d)
        {
            _campaign.Checkpoints.Clear();
            _campaign.Choices.Clear();
            _campaign.Flags.Clear();
            _campaign.PendingGrants.Clear();
            _campaign.BoardSerials.Clear();
            _campaign.BoardNextSerial = 1;
            _campaign.ContractsDone = 0;
            _campaign.BoardPlaySeconds = 0f;
            _checkpointSafe = false;
            _deferredSaveAt = -1f;
            _deferredSaveRun = null;
            ResetCampaignBoard();
            HideResumePrompt();
            CampaignSave s = d != null ? d.Campaign04E : null;
            if (s == null) return;
            if (s.Checkpoints != null)
                foreach (string c in s.Checkpoints)
                {
                    if (string.IsNullOrEmpty(c)) continue;
                    int colon = c.IndexOf(':');
                    if (colon <= 0 || colon >= c.Length - 1) continue;
                    string qid = c.Substring(0, colon), pid = c.Substring(colon + 1);
                    QuestDef q = Find(qid);
                    // A checkpoint survives only for a known, unfinished quest with that phase.
                    if (q == null || _completed.Contains(qid) || QuestLogic.PhaseIndex(q, pid) < 0) continue;
                    CampaignRules04E.CommitCheckpoint(_campaign.Checkpoints, qid, pid);
                }
            if (s.Choices != null)
                foreach (string c in s.Choices)
                {
                    if (string.IsNullOrEmpty(c)) continue;
                    int eq = c.IndexOf('=');
                    if (eq <= 0) continue;
                    CampaignRules04E.SetChoice(_campaign.Choices, c.Substring(0, eq), c.Substring(eq + 1), false);
                }
            if (s.Flags != null) foreach (string f in s.Flags) if (CampaignRules04E.IsName(f) && !_campaign.Flags.Contains(f)) _campaign.Flags.Add(f);
            if (s.PendingGrants != null) foreach (string g in s.PendingGrants) if (!string.IsNullOrEmpty(g) && g.IndexOf('|') > 0) _campaign.PendingGrants.Add(g);
            if (s.BoardSerials != null) foreach (int n in s.BoardSerials) _campaign.BoardSerials.Add(Mathf.Max(0, n));
            _campaign.BoardNextSerial = Mathf.Max(1, s.BoardNextSerial);
            foreach (int n in _campaign.BoardSerials) if (n >= _campaign.BoardNextSerial) _campaign.BoardNextSerial = n + 1;
            _campaign.ContractsDone = Mathf.Max(0, s.ContractsDone);
            _campaign.BoardPlaySeconds = Mathf.Clamp(s.BoardPlaySeconds, 0f, CampaignContracts04E.RefreshSeconds);
        }
    }

    /// <summary>The campaign's durable block (SaveData.Campaign04E). Append fields only.</summary>
    [Serializable]
    public sealed class CampaignSave
    {
        public int Version = 1;
        /// <summary>Committed checkpoints as "questId:phaseId".</summary>
        public List<string> Checkpoints = new List<string>();
        /// <summary>Decisions as "key=value".</summary>
        public List<string> Choices = new List<string>();
        // ---- appended by the CAMPAIGN stream (old saves load them empty)
        /// <summary>Campaign flags (finished Choir mechanisms "mech_*", optional rescues, the endgame board).</summary>
        public List<string> Flags = new List<string>();
        /// <summary>Ledger grants made while the reward ledger was missing, as "rewardId|token|source" (granted once it is back).</summary>
        public List<string> PendingGrants = new List<string>();
        /// <summary>The Lantern Refuge board's posted serials, one per slot (0 for an empty slot).</summary>
        public List<int> BoardSerials = new List<int>();
        /// <summary>The next board serial (0 in old saves means 1).</summary>
        public int BoardNextSerial;
        /// <summary>Campaign contracts finished (pay grows slowly with it, capped).</summary>
        public int ContractsDone;
        /// <summary>Active-play seconds since the board last reposted every slot.</summary>
        public float BoardPlaySeconds;
    }

    /// <summary>
    /// Campaign titles and colours by id, so QuestWorld.FactionName and FactionColor show "Underheart and Bonds" where a
    /// guild name would be. Filled when the quest files load; cleared by CampaignStatics04E.
    /// </summary>
    public static class CampaignRegistry04E
    {
        private static readonly Dictionary<string, CampaignDef> _byId = new Dictionary<string, CampaignDef>();

        public static void Register(CampaignDef c)
        {
            if (c != null && !string.IsNullOrEmpty(c.Id)) _byId[c.Id] = c;
        }

        public static bool Has(string id)
        {
            return !string.IsNullOrEmpty(id) && _byId.ContainsKey(id);
        }

        public static bool TryGet(string id, out CampaignDef c)
        {
            c = null;
            return !string.IsNullOrEmpty(id) && _byId.TryGetValue(id, out c);
        }

        public static void ResetStatics()
        {
            _byId.Clear();
        }
    }

    /// <summary>Reset hub for every static the CAMPAIGN work stream adds.</summary>
    public static class CampaignStatics04E
    {
        public static void ResetStatics()
        {
            CampaignRegistry04E.ResetStatics();
            CampaignPlacesRuntime04E.ResetStatics();
        }
    }
}
