using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>A running Underheart boss fight. Done on a confirmed victory; Failed when aborted or the arena could not be built.</summary>
    public sealed class BossFightHandle
    {
        public string BossId = "";
        public bool Done;
        public bool Failed;
        public string Status = "";
        public float Health01 = 1f;
        public int Phase;
        public Action Abort = () => { };
    }

    /// <summary>A mechanism of Break the Extraction Choir (valve defense, reflector beam, maintenance escort).</summary>
    public sealed class MechanismHandle
    {
        public string Id = "";
        public bool Done;
        public bool Failed;
        public string Objective = "";
        public Action Abort = () => { };
    }

    /// <summary>
    /// Game.UnderBosses: the Valve Guardian miniboss, the Cinder Matron, the Vaultbreaker and the Kiln Regent (three
    /// readable phases for the principal bosses, punish windows, a visible weakness, finite announced reinforcements, a
    /// readable arena, a quiet aftermath and first-win rewards through the Builds reward ledger with the same tokens the
    /// quests claim), plus the three Choir mechanisms. One fight or one mechanism runs at a time. Nothing here is saved:
    /// first wins live in the reward ledger (EverDefeated reads it), a load or the title ends whatever runs without a
    /// reward, and a quest that reloads a checkpoint simply begins the encounter again.
    /// </summary>
    public sealed partial class UnderheartBosses : MonoBehaviour, IWorldSystem
    {
        public const string ModuleId = "bosses_04e";
        public const string ModuleTitle = "Underheart bosses";
        public const float MinRadius = 12f;
        public const float MaxRadius = 60f;

        /// <summary>Raised on a confirmed victory (boss id).</summary>
        public event Action<string> Defeated;

        private UnderBossFight _fight;
        private ChoirMechanism _mechanism;
        private readonly HashSet<string> _sessionWins = new HashSet<string>();
        private readonly List<string> _pendingGrants = new List<string>();
        private float _retryAt;
        private int _retries;
        private bool _inited;

        public void Init()
        {
            if (_inited) return;
            _inited = true;
            WorldSystems.Register(this);
            try { HudOverlays.Register(UnderBossHud.OverlayKey, UnderBossHud.Draw); }
            catch (Exception e) { Log.Exception("Underheart bosses HUD", e); }
            try { RegisterDebugPage(); }
            catch (Exception e) { Log.Exception("Underheart bosses debug page", e); }
            ReportStatus();
        }

        /// <summary>The module row on the STATUS page: what is installed, and what is missing for first-win rewards.</summary>
        private void ReportStatus()
        {
            int installed = UnderBossProfiles.All.Length + UnderBossIds.Mechanisms.Length;
            if (Game.Builds == null)
                ModuleStatus.Report(ModuleId, ModuleTitle, ModuleState.Partial, "The fights run, but first-win rewards cannot be recorded: the build and reward ledger did not start.",
                    "Fights still play; rewards are retried while the session runs.");
            else ModuleStatus.Report(ModuleId, ModuleTitle, ModuleState.Loaded, "");
            ModuleStatus.SetCount(ModuleId, installed, "boss fights and Choir mechanisms ready to start");
        }

        // ================================================================== bosses

        /// <summary>
        /// Starts a boss at an arena (centre on its floor, yaw, boundary radius). The body waits beyond a glowing
        /// boundary until the player walks in. The same boss asked again while it runs returns its live handle (a
        /// repeated quest callback never builds a second fight); another boss or a Choir mechanism refuses with a reason.
        /// </summary>
        public BossFightHandle Begin(string bossId, Vector3 arena, float yaw, float radius, out string reason)
        {
            reason = "";
            if (!UnderBossIds.Has(bossId))
            {
                reason = "Unknown Underheart boss '" + (bossId ?? "") + "'.";
                return FailedHandle(bossId, reason);
            }
            if (_fight != null && _fight.Running)
            {
                if (_fight.Profile.Id == bossId) return _fight.Handle;
                reason = _fight.Profile.Title + " is already being fought.";
                return FailedHandle(bossId, reason);
            }
            if (_mechanism != null && _mechanism.Running)
            {
                reason = "A Choir mechanism is running (" + ChoirRules04E.Title(_mechanism.Id) + "). Finish it first.";
                return FailedHandle(bossId, reason);
            }
            if (!MathUtil.IsFinite(arena) || float.IsNaN(radius) || float.IsNaN(yaw))
            {
                reason = "The arena position is not valid.";
                return FailedHandle(bossId, reason);
            }
            if (_fight != null)
            {
                _fight.Dispose();
                _fight = null;
            }
            UnderBossProfile profile = UnderBossProfiles.For(bossId);
            float r = Mathf.Clamp(radius > 0f ? radius : 24f, MinRadius, MaxRadius);
            BossFightHandle h = new BossFightHandle { BossId = bossId };
            GameObject go = new GameObject("Underheart boss " + profile.Title);
            go.transform.SetParent(transform, false);
            UnderBossFight f = AddFight(go, bossId);
            string why = "";
            if (f == null || !f.Setup(this, profile, h, arena, yaw, r, out why))
            {
                why = f == null ? "no fight controller" : why;
                Destroy(go);
                reason = "The arena of " + profile.Title + " could not be built (" + why + ").";
                ModuleStatus.Report(ModuleId, ModuleTitle, ModuleState.Partial, reason, "The rest of the game still runs. Send the Console error along.");
                Log.Warn("UBOSS04E " + reason);
                return FailedHandle(bossId, reason);
            }
            _fight = f;
            h.Abort = () => AbortFight(f);
            return h;
        }

        private static UnderBossFight AddFight(GameObject go, string bossId)
        {
            switch (bossId)
            {
                case UnderBossIds.ValveGuardian: return go.AddComponent<ValveGuardianFight>();
                case UnderBossIds.CinderMatron: return go.AddComponent<CinderMatronFight>();
                case UnderBossIds.Vaultbreaker: return go.AddComponent<VaultbreakerFight>();
                case UnderBossIds.KilnRegent: return go.AddComponent<KilnRegentFight>();
                default: return null;
            }
        }

        private static BossFightHandle FailedHandle(string bossId, string reason)
        {
            return new BossFightHandle { BossId = bossId ?? "", Failed = true, Status = reason };
        }

        private void AbortFight(UnderBossFight f)
        {
            if (f == null || f != _fight) return;
            if (f.Running) f.End(UBossEnd.Aborted, "The fight was called off.");
        }

        /// <summary>True once the boss has been beaten at least once (its first-win token exists).</summary>
        public bool EverDefeated(string bossId)
        {
            if (!UnderBossIds.Has(bossId)) return false;
            if (_sessionWins.Contains(bossId)) return true;
            IBossRewardSink04E sink = Sink;
            try { return UnderBossRewards.EverDefeated(bossId, sink); }
            catch (Exception e)
            {
                Log.Exception("Underheart boss ledger query", e);
                return false;
            }
        }

        /// <summary>True while any Underheart boss fight runs (waiting at its boundary, in its intro or fighting).</summary>
        public bool FightActive { get { return _fight != null && _fight.Running; } }

        /// <summary>The live fight (also its victory aftermath), or null. Read-only use (HUD, debug, quests).</summary>
        public UnderBossFight ActiveFight { get { return _fight; } }

        /// <summary>The running Choir mechanism, or null.</summary>
        public ChoirMechanism ActiveMechanism { get { return _mechanism != null && _mechanism.Running ? _mechanism : null; } }

        internal void OnFightVictory(UnderBossFight f)
        {
            RaiseDefeated(f.Profile.Id);
        }

        internal void OnFightEnded(UnderBossFight f)
        {
            if (f == _fight) _fight = null;
        }

        // ================================================================== rewards

        private sealed class LedgerSink : IBossRewardSink04E
        {
            private readonly RewardLedger _l;
            public LedgerSink(RewardLedger l) { _l = l; }
            public bool Has(string token) { return _l.Has(token); }
            public bool Grant(string rewardId, string token, string source) { return _l.Grant(rewardId, token, source); }
        }

        private static IBossRewardSink04E Sink
        {
            get
            {
                if (Game.Builds == null) return null;
                RewardLedger l = Game.Builds.Ledger;
                return l != null ? new LedgerSink(l) : null;
            }
        }

        /// <summary>
        /// Grants a victory's first-win entitlements exactly once per token (the ledger refuses a token it already holds,
        /// so repeated callbacks, rematches and quest Claim steps never duplicate). Returns the line the banner shows.
        /// </summary>
        internal string GrantFirstWin(UnderBossProfile p)
        {
            bool before = EverDefeated(p.Id);
            _sessionWins.Add(p.Id);
            IBossRewardSink04E sink = Sink;
            if (sink == null)
            {
                QueueGrant(p.Id);
                return "The reward ledger is unavailable: your first-win rewards are kept for this session and retried.";
            }
            int failed;
            int fresh;
            try { fresh = UnderBossRewards.GrantFirstWin(p.Id, sink, p.Title, out failed); }
            catch (Exception e)
            {
                Log.Exception("Underheart boss first-win grant " + p.Id, e);
                QueueGrant(p.Id);
                return "Your first-win rewards could not be recorded yet; they are retried while the session runs.";
            }
            if (failed > 0) QueueGrant(p.Id);
            Log.Info("UBOSS04E " + p.Id + " first-win grants: " + fresh + " new, " + failed + " not accepted" + (before ? " (an earlier win exists)" : ""));
            if (fresh > 0) return "First-win rewards are in your ledger: " + p.RewardNote + " Claim them on the BUILD page or at a ledger terminal.";
            if (failed > 0) return "Your first-win rewards could not be recorded yet; they are retried while the session runs.";
            return "Its first-win rewards were already earned: nothing is granted twice.";
        }

        private void QueueGrant(string bossId)
        {
            if (!_pendingGrants.Contains(bossId)) _pendingGrants.Add(bossId);
            _retryAt = Time.unscaledTime + 3f;
        }

        private void Update()
        {
            if (_pendingGrants.Count == 0 || Time.unscaledTime < _retryAt) return;
            _retryAt = Time.unscaledTime + 5f;
            IBossRewardSink04E sink = Sink;
            if (sink == null) return;
            if (++_retries > 12)
            {
                Log.Warn("UBOSS04E first-win grants still not accepted after 12 tries: " + string.Join(", ", _pendingGrants.ToArray()));
                _pendingGrants.Clear();
                return;
            }
            for (int i = _pendingGrants.Count - 1; i >= 0; i--)
            {
                string id = _pendingGrants[i];
                UnderBossProfile p = UnderBossProfiles.For(id);
                int failed;
                int fresh = 0;
                try { fresh = UnderBossRewards.GrantFirstWin(id, sink, p != null ? p.Title : id, out failed); }
                catch (Exception e)
                {
                    Log.Exception("Underheart boss first-win retry " + id, e);
                    failed = 1;
                }
                if (failed > 0) continue;
                _pendingGrants.RemoveAt(i);
                if (fresh > 0 && Game.Hud != null) Game.Hud.Notify("First-win rewards for " + (p != null ? p.Title : id) + " are now in your ledger.", 4f);
            }
        }

        // ================================================================== mechanisms

        /// <summary>
        /// Starts a Choir mechanism at an anchor (crown_valve, crown_reflectors, crown_escort). The same mechanism asked
        /// again while it runs returns its live handle; anything else running refuses with a reason.
        /// </summary>
        public MechanismHandle BeginMechanism(string mechanismId, Vector3 anchor, float yaw, out string reason)
        {
            reason = "";
            if (!ChoirRules04E.Has(mechanismId))
            {
                reason = "Unknown Choir mechanism '" + (mechanismId ?? "") + "'.";
                return new MechanismHandle { Id = mechanismId ?? "", Failed = true, Objective = reason };
            }
            if (_mechanism != null && _mechanism.Running)
            {
                if (_mechanism.Id == mechanismId) return _mechanism.Handle;
                reason = ChoirRules04E.Title(_mechanism.Id) + " is already running.";
                return new MechanismHandle { Id = mechanismId, Failed = true, Objective = reason };
            }
            if (_fight != null && _fight.Running)
            {
                reason = _fight.Profile.Title + " is being fought.";
                return new MechanismHandle { Id = mechanismId, Failed = true, Objective = reason };
            }
            if (!MathUtil.IsFinite(anchor))
            {
                reason = "The mechanism anchor is not valid.";
                return new MechanismHandle { Id = mechanismId, Failed = true, Objective = reason };
            }
            if (_mechanism != null) _mechanism.Dispose();
            _mechanism = null;
            MechanismHandle h = new MechanismHandle { Id = mechanismId };
            GameObject go = new GameObject("Choir mechanism " + ChoirRules04E.Title(mechanismId));
            go.transform.SetParent(transform, false);
            ChoirMechanism m = AddMechanism(go, mechanismId);
            string why = "";
            if (m == null || !m.Setup(this, mechanismId, h, anchor, yaw, out why))
            {
                why = m == null ? "no controller" : why;
                Destroy(go);
                reason = ChoirRules04E.Title(mechanismId) + " could not be built (" + why + ").";
                Log.Warn("UBOSS04E " + reason);
                return new MechanismHandle { Id = mechanismId, Failed = true, Objective = reason };
            }
            _mechanism = m;
            h.Abort = () => AbortMechanism(m);
            return h;
        }

        private static ChoirMechanism AddMechanism(GameObject go, string id)
        {
            switch (id)
            {
                case ChoirRules04E.Valve: return go.AddComponent<ChoirValve>();
                case ChoirRules04E.Reflectors: return go.AddComponent<ChoirReflectors>();
                case ChoirRules04E.Escort: return go.AddComponent<ChoirEscort>();
                default: return null;
            }
        }

        private void AbortMechanism(ChoirMechanism m)
        {
            if (m == null || m != _mechanism) return;
            if (m.Running) m.End(false, "The mechanism was left.");
        }

        // ================================================================== companion and fused-vehicle help

        /// <summary>How close (metres) an aim point must be to the boss body's centre, or to a lure or vent, to pick it.</summary>
        public const float BodyAimRadius = 6f, TargetAimRadius = 3f;

        /// <summary>
        /// For a companion's combat assist: the live boss body, lure or furnace vent nearest an aim point (the body within
        /// BodyAimRadius of its centre, a lure or vent within TargetAimRadius), within maxRange of from with a clear line.
        /// Returns the damage receiver to hit with Source = the player and Origin = Companion or FusedVehicle (the fight
        /// credits it to the player once).
        /// </summary>
        public bool TryAssistTarget(Vector3 from, Vector3 aimPoint, float maxRange, out Vector3 point, out IDamageable target)
        {
            point = aimPoint;
            target = null;
            UnderBossFight f = _fight;
            if (f == null || !f.AcceptsAssists) return false;
            float best = float.MaxValue;
            Vector3 c = f.BodyCenter;
            float d = (aimPoint - c).sqrMagnitude;
            if (d <= BodyAimRadius * BodyAimRadius && InReach(from, c, maxRange))
            {
                best = d;
                point = c;
                target = f.BodyHealth;
            }
            for (int i = 0; i < f.ShootableCount; i++)
            {
                UBossTarget t;
                Vector3 p;
                if (!f.ShootableAt(i, out t, out p)) continue;
                float ds = (aimPoint - p).sqrMagnitude;
                if (ds > TargetAimRadius * TargetAimRadius || ds >= best || !InReach(from, p, maxRange)) continue;
                best = ds;
                point = p;
                target = t;
            }
            return target != null;
        }

        /// <summary>
        /// Whether a receiver TryAssistTarget returned can still be hit (the fight is on and the body lives, or the lure or
        /// vent is still armed), with its current aim point (the body moves). No line check: the caller checks its own line.
        /// </summary>
        public bool AssistTargetAlive(IDamageable target, out Vector3 point)
        {
            point = Vector3.zero;
            UnderBossFight f = _fight;
            if (target == null || f == null || !f.AcceptsAssists) return false;
            if (ReferenceEquals(target, f.BodyHealth))
            {
                point = f.BodyCenter;
                return true;
            }
            for (int i = 0; i < f.ShootableCount; i++)
            {
                UBossTarget t;
                Vector3 p;
                if (!f.ShootableAt(i, out t, out p) || !ReferenceEquals(t, target)) continue;
                point = p;
                return true;
            }
            return false;
        }

        /// <summary>
        /// For area hits (a fused form's vent burst, brace ram or discharge): every live boss receiver within radius of
        /// center (the body measured to its nearest hit volume, so a big body counts when its flank is in reach), each with
        /// the point to hit; when needLine, only those with a clear line from from. Fills the lists and returns the count.
        /// </summary>
        public int AssistTargetsNear(Vector3 from, Vector3 center, float radius, bool needLine, List<IDamageable> targets, List<Vector3> points)
        {
            targets.Clear();
            points.Clear();
            UnderBossFight f = _fight;
            if (f == null || !f.AcceptsAssists) return 0;
            float r2 = radius * radius;
            Vector3 s = f.BodySurfaceNear(center);
            if ((s - center).sqrMagnitude <= r2 && (!needLine || LineClear(from, s)))
            {
                targets.Add(f.BodyHealth);
                points.Add(s);
            }
            for (int i = 0; i < f.ShootableCount; i++)
            {
                UBossTarget t;
                Vector3 p;
                if (!f.ShootableAt(i, out t, out p)) continue;
                if ((p - center).sqrMagnitude > r2 || (needLine && !LineClear(from, p))) continue;
                targets.Add(t);
                points.Add(p);
            }
            return targets.Count;
        }

        private static bool InReach(Vector3 from, Vector3 p, float maxRange)
        {
            return (p - from).sqrMagnitude <= maxRange * maxRange && LineClear(from, p);
        }

        private static bool LineClear(Vector3 a, Vector3 b)
        {
            return !Physics.Linecast(a, b, Layers.WorldMask, QueryTriggerInteraction.Ignore);
        }

        // ================================================================== saves and session

        /// <summary>New game or load: whatever runs ends without a reward; the aftermath goes too. First wins live in the ledger.</summary>
        public void ResetAll()
        {
            if (_fight != null)
            {
                if (_fight.Running) _fight.End(UBossEnd.SessionReset, "The fight ended when the session reset.");
                else _fight.Dispose();
            }
            _fight = null;
            if (_mechanism != null)
            {
                if (_mechanism.Running) _mechanism.End(false, "The mechanism ended when the session reset.");
                else _mechanism.Dispose();
            }
            _mechanism = null;
            _sessionWins.Clear();
            _pendingGrants.Clear();
            _retries = 0;
        }

        public void Capture(SaveData d) { }

        public void Restore(SaveData d) { }

        private void OnDestroy()
        {
            try { HudOverlays.Unregister(UnderBossHud.OverlayKey); }
            catch (Exception e) { Log.Exception("Underheart bosses HUD release", e); }
        }

        private void RaiseDefeated(string id)
        {
            if (Defeated == null) return;
            foreach (Delegate h in Defeated.GetInvocationList())
            {
                try { ((Action<string>)h)(id); }
                catch (Exception e) { Log.Exception("UnderBosses Defeated listener", e); }
            }
        }
    }

    /// <summary>Reset hub for every static the BOSSES work stream adds.</summary>
    public static class UnderBossStatics
    {
        public static void ResetStatics()
        {
            UnderBossProfiles.ResetStatics();
            UBossTiming.ResetStatics();
            UnderBossFx.ResetStatics();
            UnderBossHud.ResetStatics();
            CinderMatronFight.ResetStatics();
        }
    }
}
