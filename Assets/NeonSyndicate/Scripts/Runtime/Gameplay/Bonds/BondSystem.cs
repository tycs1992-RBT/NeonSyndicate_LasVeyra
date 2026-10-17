using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Game.Bonds: the bonded companions of Underheart and Bonds 04E. Owns the roster (one record per archetype, one
    /// active companion), the single live companion body (CompanionController on the shared creature rig), the
    /// player's breadcrumb trail it follows, the on-foot commands (tap N follow or wait, hold N recall, U assist), the
    /// exploration and combat assists, care, trust, ranks, crystal tuning, specializations and evolution, the rescue
    /// sequence for the quest Rescue step, the fusion hand-off (SetFused, Snapshot, RestoreSnapshot, CanFuse), the HUD
    /// panel, the BONDS page and the save block. Companions are not Creatures: lock-on, powers and hostile AI never
    /// treat them as targets, and assist damage is credited once to the player (Source = player, Origin = Companion).
    /// Split into BondSystem.Commands, .Assist, .Care, .Rescue, .Fusion, .Hud and .Debug.
    /// </summary>
    public sealed partial class BondSystem : MonoBehaviour, IWorldSystem
    {
        /// <summary>Raised when the companion's exploration assist reveals something (world point).</summary>
        public event Action<Vector3> ExplorationAssist;
        /// <summary>Raised when a companion joins the roster (record).</summary>
        public event Action<CompanionRecord> Bonded;

        private BondSave _data = new BondSave();
        private IList<CompanionRecord> _rosterView;
        private readonly CompanionTrail _trail = new CompanionTrail();
        private CompanionController _body;
        private CompanionBodyFactory.Job _bodyJob;
        private int _bodyJobUid;
        private CreatureBody _pendingBody;
        private int _pendingUid, _pendingEvo;
        private string _pendingSpec = "";
        private bool _buildFailReported;
        private BondNamingCard _naming;
        private bool _questsHooked;
        private float _lastExploreTime = -999f;
        private Vector3 _lastExplorePoint;
        private bool _riding;
        private float _placeRetry;
        private float _downedT;
        private float _revealT = -1f;
        private float _lastHurt = -99f;
        private float _hurtCue;
        private int _summonFailures;

        public void Init()
        {
            WorldSystems.Register(this);
            _naming = gameObject.AddComponent<BondNamingCard>();
            _rosterView = _data.Companions.AsReadOnly();
            PlayerDamageHooks.Register(BraceHookKey, BraceHookOrder, BraceHook);
            HudOverlays.Register("bonds_04e", DrawHud);
            BondsPage.Register();
            DebugPages.Register(new MenuPage { Title = "Bonds", Order = 56, Draw = DrawDebug });
            ReportStatus();
        }

        // ================================================================== roster

        public CompanionRecord Active { get { return BondRoster.Active(_data); } }
        public IList<CompanionRecord> Roster { get { return _rosterView ?? (_rosterView = _data.Companions.AsReadOnly()); } }

        /// <summary>The live body of the active companion (null while resting, fused or not summoned).</summary>
        public Transform ActiveBody { get { return _body != null && !_body.Hidden ? _body.transform : null; } }

        public bool Owns(string archetype) { return BondRoster.FindArchetype(_data, archetype) != null; }
        public CompanionRecord Find(int uid) { return BondRoster.Find(_data, uid); }
        public CompanionRecord FindArchetype(string archetype) { return BondRoster.FindArchetype(_data, archetype); }

        /// <summary>Adds a companion (one per archetype; granting an owned archetype returns the existing record).</summary>
        public CompanionRecord Grant(string archetype, string name, string source)
        {
            return GrantAt(archetype, name, source, BondIds.RankBonded);
        }

        private CompanionRecord GrantAt(string archetype, string name, string source, int rank)
        {
            bool created;
            CompanionRecord r = BondRoster.Grant(_data, archetype, name, source, rank, out created);
            if (r == null)
            {
                Log.Warn("Bonds: cannot grant unknown archetype '" + archetype + "'.");
                return null;
            }
            if (!created) return r;
            Log.Info("Bonds: " + r.Name + " (" + BondIds.Title(r.Archetype) + ") joined the roster from " + (string.IsNullOrEmpty(source) ? "a grant" : source) + ".");
            if (Active != r && source != "rescue") Say(NameOf(r) + " the " + BondIds.Title(r.Archetype).ToLowerInvariant() + " joined your bonds. Bring it along at a care station or from the BONDS page.", 5f, false);
            RaiseBonded(r);
            ReportStatus();
            return r;
        }

        /// <summary>Raises a companion to at least minRank (never lowers). True when the rank changed.</summary>
        public bool RaiseRank(int uid, int minRank, string reason)
        {
            CompanionRecord r = Find(uid);
            if (r == null || !BondIds.Has(r.Archetype)) return false;
            int before = r.Rank;
            if (!BondRules.RaiseRank(r, minRank)) return false;
            Log.Info("Bonds: " + r.Name + " raised to " + BondRules.RankName(r.Rank) + (string.IsNullOrEmpty(reason) ? "" : " (" + reason + ")") + ".");
            OnRankChanged(r, before);
            return true;
        }

        public int RankOf(int uid)
        {
            CompanionRecord r = Find(uid);
            return r != null ? r.Rank : -1;
        }

        /// <summary>Makes an owned companion the active one (summons it near the player when allowed).</summary>
        public bool SetActive(int uid, out string reason)
        {
            reason = "";
            CompanionRecord r = Find(uid);
            if (r == null || !BondIds.Has(r.Archetype)) { reason = "That companion is not in your roster."; return false; }
            CompanionRecord cur = Active;
            if (cur != null && cur.Uid == uid)
            {
                if (r.State == (int)CompanionState.Resting) r.State = (int)CompanionState.Following;
                reason = r.Name + " is already your active companion.";
                return true;
            }
            if (cur != null && cur.State == (int)CompanionState.Fused) { reason = cur.Name + " is fused with your chassis. Separate first."; return false; }
            if (_rescue != null) { reason = "Finish the rescue first."; return false; }
            if (CombatDirector.InCombat) { reason = "Not during a fight."; return false; }
            if (Game.Player != null && Game.Player.InVehicle) { reason = "Step off your vehicle first."; return false; }
            EndAssist();
            EndBrace(false);
            if (cur != null && cur.State != (int)CompanionState.Downed) cur.State = (int)CompanionState.Resting;
            DespawnBody();
            RemoveHome(uid);
            BondRoster.SetActive(_data, uid);
            if (r.State == (int)CompanionState.Resting || BondRoster.IsTransient(r.State)) r.State = (int)CompanionState.Following;
            _revealT = 0f;
            reason = r.State == (int)CompanionState.Downed ? r.Name + " is active, but still resting off its injuries." : r.Name + " joins you.";
            Underheart04ESounds.Play2D(UhSfx.CompanionFollow, 0.7f, 1f);
            ReportStatus();
            return true;
        }

        /// <summary>Sends a companion home to rest (the body leaves; it stays active and can be summoned again).</summary>
        public bool SendHome(int uid, out string message)
        {
            message = "";
            CompanionRecord r = Find(uid);
            if (r == null) { message = "That companion is not in your roster."; return false; }
            if (r.State == (int)CompanionState.Fused) { message = r.Name + " is fused. Separate first."; return false; }
            if (r.State == (int)CompanionState.Downed) { message = r.Name + " is already resting off its injuries."; return false; }
            if (Active == r)
            {
                EndAssist();
                EndBrace(false);
                if (_body != null && Game.Fx != null) Game.Fx.CrystalBurst(_body.ChestPosition, ProfileOf(r).Accent, 10, 0.4f);
                DespawnBody();
            }
            r.State = (int)CompanionState.Resting;
            message = r.Name + " heads home to rest.";
            ReportStatus();
            return true;
        }

        /// <summary>Calls a resting active companion back to your side.</summary>
        public bool Summon(int uid, out string message)
        {
            message = "";
            CompanionRecord r = Find(uid);
            if (r == null) { message = "That companion is not in your roster."; return false; }
            if (Active != r) return SetActive(uid, out message);
            if (r.State == (int)CompanionState.Downed) { message = r.Name + " is resting off its injuries (" + BondRules.Clock(r.RecoverIn) + ", or at once at a care station)."; return false; }
            if (r.State == (int)CompanionState.Resting) r.State = (int)CompanionState.Following;
            _revealT = 0f;
            message = r.Name + " is on its way.";
            ReportStatus();
            return true;
        }

        public static CompanionProfile ProfileOf(CompanionRecord r)
        {
            return CompanionArchetypes.Get(r != null ? r.Archetype : null) ?? CompanionArchetypes.QuartzGuide;
        }

        public static string NameOf(CompanionRecord r)
        {
            if (r == null) return "Your companion";
            return string.IsNullOrEmpty(r.Name) ? BondIds.DefaultName(r.Archetype) : r.Name;
        }

        public float MaxHealthOf(CompanionRecord r)
        {
            return r == null ? 1f : BondRules.MaxHealth(r.Archetype, r.Rank, r.Tuning);
        }

        // ================================================================== frame

        private void Update()
        {
            HookQuests();
            float dt = Time.deltaTime;
            bool playing = Game.State == GameState.Playing;
            if (playing && dt > 0f) TickRecords(dt);
            RecordTrail();
            ManageBody(dt);
            if (playing) ReadInput();
            else _hold.Reset();
            TickAssist(dt);
            TickBrace(dt);
            TickRescue();
            TickCareStations();
            TickHome();
            TickAutosave();
            TickMessages(Time.unscaledDeltaTime);
            CompanionBudget.Bodies = _body != null && !_body.Hidden ? 1 : 0;
        }

        private void HookQuests()
        {
            if (_questsHooked || Game.Quests == null) return;
            Game.Quests.QuestCompleted += OnQuestCompleted;
            _questsHooked = true;
        }

        private void OnQuestCompleted(string questId)
        {
            CompanionRecord r = Active;
            if (r == null) return;
            int before = r.Rank;
            int gained = BondRules.QuestTrust(r, questId);
            if (gained <= 0) return;
            if (Game.Hud != null) Game.Hud.CombatText(NameOf(r) + ": trust +" + gained, ProfileOf(r).Accent);
            if (r.Rank != before) OnRankChanged(r, before);
        }

        private void TickRecords(float dt)
        {
            List<CompanionRecord> list = _data.Companions;
            CompanionRecord active = Active;
            for (int i = 0; i < list.Count; i++)
            {
                CompanionRecord r = list[i];
                if (r == null || !BondIds.Has(r.Archetype)) continue;
                bool recovered = BondRules.TickTimers(r, dt);
                if (recovered && r != active) r.State = (int)CompanionState.Resting;
                if (recovered && r == active)
                {
                    _revealT = 0f;
                    Say(NameOf(r) + " has rested and is back on its feet.", 4f, false);
                    ReportStatus();
                }
                if (r.State == (int)CompanionState.Downed) continue;
                if (r != active)
                {
                    r.Health01 = Mathf.Min(1f, r.Health01 + 0.05f * dt);
                    continue;
                }
                if (Time.time - _lastHurt > BondRules.RegenDelay && !CombatDirector.InCombat)
                    r.Health01 = Mathf.Min(1f, r.Health01 + BondRules.RegenPerSecond * dt);
            }
            if (_body != null && active != null)
            {
                float heat = _body.ConsumeHazardDamage();
                if (heat > 0f) HurtActive(heat * 0.8f, Vector3.up);
            }
        }

        /// <summary>Wears the active companion's health (heat, brace strain, assist exposure). At zero it is downed, never lost.</summary>
        public void HurtActive(float amount, Vector3 fromDirection)
        {
            CompanionRecord r = Active;
            if (r == null || amount <= 0f || r.State == (int)CompanionState.Downed || r.State == (int)CompanionState.Fused) return;
            _lastHurt = Time.time;
            bool downed = BondRules.Hurt(r, amount, MaxHealthOf(r));
            if (_body != null) _body.Flinch(Mathf.Clamp01(amount / Mathf.Max(1f, MaxHealthOf(r)) * 4f + 0.3f), fromDirection);
            if (downed)
            {
                OnDowned(r);
                return;
            }
            if (Time.time >= _hurtCue)
            {
                _hurtCue = Time.time + 1.4f;
                if (_body != null) Underheart04ESounds.Play(UhSfx.CompanionHurt, _body.ChestPosition, 0.7f, 1f);
            }
        }

        private void OnDowned(CompanionRecord r)
        {
            EndAssist();
            EndBrace(true);
            _downedT = 0f;
            if (_body != null)
            {
                _body.Mode = CompanionMode.Downed;
                _body.PlayAction(CreatureAction.Death, 1.4f, 0.3f, 0.6f, null);
                Underheart04ESounds.Play(UhSfx.CompanionDowned, _body.ChestPosition, 0.9f, 1f);
            }
            Say(NameOf(r) + " is down. It will rest and recover in " + BondRules.Clock(r.RecoverIn) + ", or at once at a care station.", 5f, true);
            ReportStatus();
        }

        private void RecordTrail()
        {
            PlayerController pl = Game.Player;
            if (pl == null || pl.IsDead || Game.State != GameState.Playing) return;
            if (pl.InVehicle)
            {
                if (_trail.Count > 0) _trail.Clear();
                return;
            }
            Vector3 p = pl.transform.position;
            if (_trail.HasNewest && (p - _trail.Newest.P).sqrMagnitude < CompanionTrail.Spacing * CompanionTrail.Spacing) return;
            float heat;
            bool hazard = Game.Underheart != null && Game.Underheart.HazardAt(p + Vector3.up * 0.2f, out heat) && heat > 0f;
            _trail.Record(p, !pl.IsGroundedPublic, hazard);
        }

        // ================================================================== the single companion body

        private void ManageBody(float dt)
        {
            PlayerController pl = Game.Player;
            if (Game.State == GameState.Loading || Game.State == GameState.Title || pl == null)
            {
                DespawnBody();
                return;
            }
            CompanionRecord r = Active;
            if (r == null)
            {
                DespawnBody();
                return;
            }
            if (_body != null && _body.Uid != r.Uid) DespawnBody();
            if (QaTools.Busy || Game.State == GameState.Cutscene)
            {
                if (_body != null && !_body.Hidden) _body.SetHidden(true);
                return;
            }
            switch ((CompanionState)r.State)
            {
                case CompanionState.Resting:
                    DespawnBody();
                    return;
                case CompanionState.Fused:
                    if (_body != null && !_body.Hidden) _body.SetHidden(true);
                    return;
                case CompanionState.Downed:
                    TickDownedBody(dt);
                    return;
            }
            if (InSeparationGrace(pl, dt)) return;
            if (pl.InVehicle && r.State != (int)CompanionState.Waiting)
            {
                if (_body != null && !_body.Hidden)
                {
                    if (Game.Fx != null && (_body.transform.position - pl.transform.position).sqrMagnitude < 30f * 30f)
                        Game.Fx.CrystalBurst(_body.ChestPosition, ProfileOf(r).Accent, 6, 0.3f);
                    EndAssist();
                    EndBrace(false);
                    _body.SetHidden(true);
                }
                _riding = true;
                return;
            }
            int evo = BondRules.EvolutionStage(r.Rank, r.Evolution);
            string spec = r.Specialization ?? "";
            if (_body == null || _body.BuiltEvolution != evo || _body.BuiltSpec != spec)
            {
                if (!BuildBody(r, evo, spec)) return;
            }
            if (_body == null) return;
            if (_riding || (_body.Hidden && !pl.InVehicle))
            {
                _placeRetry -= dt;
                if (_placeRetry > 0f) return;
                _placeRetry = 0.5f;
                if (!PlaceBesidePlayer(r, false)) return;
                _riding = false;
            }
            _body.Trail = _trail;
            _body.DisplayName = NameOf(r);
            _body.Combat = CombatDirector.InCombat;
            if (!AssistBusy && _body.Mode != CompanionMode.Downed)
            {
                if (r.State == (int)CompanionState.Waiting) _body.Mode = CompanionMode.Wait;
                else
                {
                    _body.Mode = CompanionMode.Follow;
                    if (r.State == (int)CompanionState.Assisting) r.State = (int)CompanionState.Following;
                }
            }
            if (_revealT >= 0f)
            {
                _revealT += dt;
                _body.SetDissolve(1f - Mathf.Clamp01(_revealT / 0.6f));
                if (_revealT >= 0.6f) _revealT = -1f;
            }
            if (_body.WantsRecall && _body.Mode == CompanionMode.Follow)
            {
                string why;
                if (_body.TryRecall(false, out why)) _summonFailures = 0;
            }
        }

        private void TickDownedBody(float dt)
        {
            if (_body == null) return;
            if (_body.Hidden) _body.SetHidden(false);
            _body.Mode = CompanionMode.Downed;
            _downedT += dt;
            if (!_body.ActionPlaying) _body.HoldAction(CreatureAction.Death, 0.92f, 0.3f, 0.6f);
            if (_downedT > BondRules.DownedRetreatSeconds)
            {
                float d = Mathf.Clamp01((_downedT - BondRules.DownedRetreatSeconds) / 1f);
                _body.SetDissolve(d);
                if (d >= 1f) DespawnBody();
            }
        }

        /// <summary>Orders and, once ready, places (or swaps in) the body for the active record. True when a body is live.</summary>
        private bool BuildBody(CompanionRecord r, int evo, string spec)
        {
            bool pendingMatches = _pendingBody != null && _pendingUid == r.Uid && _pendingEvo == evo && _pendingSpec == spec;
            if (!pendingMatches)
            {
                _pendingBody = null;
                bool jobMatches = _bodyJob != null && _bodyJobUid == r.Uid && _bodyJob.Evolution == evo && _bodyJob.Spec == spec;
                if (!jobMatches)
                {
                    if (_bodyJob != null) _bodyJob.Cancelled = true;
                    _bodyJob = CompanionBodyFactory.Request(r.Archetype, r.Seed, evo, spec, CompanionLooks.DetailFor(r.Archetype, Settings.Graphics));
                    _bodyJobUid = r.Uid;
                }
                if (!_bodyJob.Finished) return _body != null;
                if (_bodyJob.Failed || _bodyJob.Body == null)
                {
                    if (!_buildFailReported)
                    {
                        _buildFailReported = true;
                        Log.Warn("Bonds: the body for " + NameOf(r) + " could not be built: " + _bodyJob.Error);
                        Say(NameOf(r) + " could not appear (body build failed). It stays bonded; see the Console.", 5f, true);
                        ModuleStatus.Report("bonds", "Bonded companions", ModuleState.Partial, "A companion body failed to build: " + _bodyJob.Error, "Send the Console error along. The bond and progress are kept.");
                    }
                    return _body != null;
                }
                _pendingBody = _bodyJob.Body;
                _pendingUid = r.Uid;
                _pendingEvo = evo;
                _pendingSpec = spec;
                _bodyJob = null;
            }
            CompanionProfile prof = ProfileOf(r);
            Vector3 pos;
            float yaw;
            bool swap = _body != null;
            PlayerController pl = Game.Player;
            if (swap)
            {
                pos = _body.transform.position;
                yaw = _body.Yaw;
            }
            else
            {
                _placeRetry -= Time.deltaTime;
                if (_placeRetry > 0f) return false;
                _placeRetry = 0.5f;
                string why;
                if (pl == null || pl.InVehicle || !CompanionController.FindSafePoint(pl, prof, out pos, out why))
                {
                    _summonFailures++;
                    if (_summonFailures == 1 || _summonFailures % 20 == 0)
                        Say(NameOf(r) + " is waiting for a safe spot beside you" + (pl != null && pl.InVehicle ? " (step off your vehicle)." : "."), 3f, false);
                    return false;
                }
                Vector3 face = pl.transform.position - pos;
                yaw = face.sqrMagnitude > 0.01f ? MathUtil.YawOf(new Vector3(face.x, 0f, face.z)) : 0f;
            }
            CreatureBody body = _pendingBody;
            _pendingBody = null;
            CompanionController c;
            try
            {
                c = CompanionController.Create(body, prof, pos, yaw, transform);
            }
            catch (Exception e)
            {
                Log.Exception("Bonds: companion body", e);
                ModuleStatus.Report("bonds", "Bonded companions", ModuleState.Partial, "A companion body could not be placed: " + e.Message, "Send the Console error along. The bond and progress are kept.");
                return _body != null;
            }
            c.Uid = r.Uid;
            c.BuiltEvolution = evo;
            c.BuiltSpec = spec;
            c.Trail = _trail;
            c.DisplayName = NameOf(r);
            c.Mode = r.State == (int)CompanionState.Waiting ? CompanionMode.Wait : CompanionMode.Follow;
            if (swap)
            {
                c.WaitPoint = _body.WaitPoint;
                if (Game.Fx != null) Game.Fx.CrystalBurst(c.ChestPosition, prof.Accent, 18, 0.6f);
                if (evo > _body.BuiltEvolution) Underheart04ESounds.Play(UhSfx.BondChime, pos, 1f, 0.9f);
                bool wasHidden = _body.Hidden;
                Destroy(_body.gameObject);
                if (wasHidden) c.SetHidden(true);
            }
            else
            {
                c.WaitPoint = pos;
                _revealT = 0f;
                c.SetDissolve(1f);
                if (Game.Fx != null) Game.Fx.CrystalBurst(pos + Vector3.up * 0.4f, prof.Accent, 10, 0.4f);
            }
            _body = c;
            _summonFailures = 0;
            _buildFailReported = false;
            return true;
        }

        /// <summary>Shows the body beside the player on verified ground (after riding, a separation or a summon).</summary>
        private bool PlaceBesidePlayer(CompanionRecord r, bool announce)
        {
            if (_body == null) return false;
            PlayerController pl = Game.Player;
            if (pl == null || pl.InVehicle) return false;
            Vector3 pos;
            string why;
            if (!CompanionController.FindSafePoint(pl, ProfileOf(r), out pos, out why))
            {
                _summonFailures++;
                if (announce || _summonFailures == 1 || _summonFailures % 20 == 0) Say(NameOf(r) + " cannot join you here: " + why, 3f, false);
                return false;
            }
            Vector3 face = pl.transform.position - pos;
            _body.Teleport(pos, face.sqrMagnitude > 0.01f ? MathUtil.YawOf(new Vector3(face.x, 0f, face.z)) : 0f);
            _body.SetHidden(false);
            _body.SetDissolve(0f);
            _body.Mode = r.State == (int)CompanionState.Waiting ? CompanionMode.Wait : CompanionMode.Follow;
            if (Game.Fx != null) Game.Fx.CrystalBurst(pos + Vector3.up * 0.4f, ProfileOf(r).Accent, 8, 0.35f);
            _summonFailures = 0;
            return true;
        }

        private void DespawnBody()
        {
            if (_bodyJob != null)
            {
                _bodyJob.Cancelled = true;
                _bodyJob = null;
            }
            _pendingBody = null;
            if (_body != null)
            {
                EndAssist();
                EndBrace(false);
                Destroy(_body.gameObject);
                _body = null;
            }
            _riding = false;
            CompanionBudget.Bodies = 0;
        }

        // ================================================================== saves (IWorldSystem)

        public void ResetAll()
        {
            AbortRescue();
            DespawnBody();
            if (_naming != null) _naming.Cancel();
            _data = new BondSave();
            _rosterView = _data.Companions.AsReadOnly();
            ClearRuntime();
            ReportStatus();
        }

        public void Capture(SaveData d)
        {
            if (d == null) return;
            d.Bonds = BondRoster.CopyForSave(_data);
        }

        public void Restore(SaveData d)
        {
            AbortRescue();
            DespawnBody();
            if (_naming != null) _naming.Cancel();
            BondSave loaded = d != null && d.Bonds != null ? BondRoster.CopyForSave(d.Bonds) : new BondSave();
            int fixes = BondRoster.Normalize(loaded);
            if (fixes > 0) Log.Info("Bonds: normalized the saved bond block (" + fixes + " fixes).");
            _data = loaded;
            _rosterView = _data.Companions.AsReadOnly();
            ClearRuntime();
            CompanionRecord a = Active;
            if (a != null && a.Rank <= BondIds.RankBonded) ShowControlHint(25f);
            ReportStatus();
        }

        private void ClearRuntime()
        {
            _trail.Clear();
            _cooldowns.Clear();
            _snapshots.Clear();
            _returnSpotSet = false;
            _lastExploreTime = -999f;
            _lastExplorePoint = Vector3.zero;
            _riding = false;
            _revealT = -1f;
            _downedT = 0f;
            _summonFailures = 0;
            _hold.Reset();
            _messages.Clear();
            ClearHome();
            EndAssist();
            EndBrace(false);
            CompanionTrailFx.Clear();
            CompanionBudget.ResetStatics();
        }

        /// <summary>Keeps the STATUS page row honest: what is bonded and active, or how to get the first companion.</summary>
        private void ReportStatus()
        {
            CompanionRecord a = Active;
            string detail;
            if (_data.Companions.Count == 0) detail = "No companion yet. The first bond comes from A Creature Is Not a Battery (rescue the quartz drakeling).";
            else if (a == null) detail = _data.Companions.Count + " bonded, none active.";
            else detail = NameOf(a) + " (" + BondIds.Title(a.Archetype) + ", " + BondRules.RankName(a.Rank) + ", " + StateWord(a) + "). " + _data.Companions.Count + " of 3 archetypes bonded.";
            ModuleRecord rec = ModuleStatus.Find("bonds");
            if (rec == null || rec.State != ModuleState.Failed)
                ModuleStatus.Report("bonds", "Bonded companions", ModuleState.Loaded, detail, "");
            ModuleStatus.SetCount("bonds", _careStations.Count, "care stations placed");
        }

        public static string StateWord(CompanionRecord r)
        {
            if (r == null) return "";
            switch ((CompanionState)r.State)
            {
                case CompanionState.Resting: return "resting at home";
                case CompanionState.Following: return "following";
                case CompanionState.Waiting: return "waiting";
                case CompanionState.Assisting: return "assisting";
                case CompanionState.Downed: return "down, recovering";
                case CompanionState.Fused: return "fused";
                case CompanionState.Rescue: return "being rescued";
                default: return "";
            }
        }

        private void OnDestroy()
        {
            if (_questsHooked && Game.Quests != null) Game.Quests.QuestCompleted -= OnQuestCompleted;
            _questsHooked = false;
            PlayerDamageHooks.Unregister(BraceHookKey);
            HudOverlays.Unregister("bonds_04e");
            CompanionBudget.ResetStatics();
        }

        private void RaiseExplorationAssist(Vector3 p)
        {
            if (ExplorationAssist == null) return;
            foreach (Delegate h in ExplorationAssist.GetInvocationList())
            {
                try { ((Action<Vector3>)h)(p); }
                catch (Exception e) { Log.Exception("ExplorationAssist listener", e); }
            }
        }

        private void RaiseBonded(CompanionRecord r)
        {
            if (Bonded == null) return;
            foreach (Delegate h in Bonded.GetInvocationList())
            {
                try { ((Action<CompanionRecord>)h)(r); }
                catch (Exception e) { Log.Exception("Bonded listener", e); }
            }
        }
    }

    /// <summary>Reset hub for every static the BONDS work stream adds.</summary>
    public static class BondStatics
    {
        public static void ResetStatics()
        {
            CompanionBudget.ResetStatics();
            CompanionBodyFactory.ResetStatics();
            CompanionController.ResetStatics();
            CompanionTrailFx.ResetStatics();
            CompanionArt.ResetStatics();
            BondsPage.ResetStatics();
        }
    }
}
