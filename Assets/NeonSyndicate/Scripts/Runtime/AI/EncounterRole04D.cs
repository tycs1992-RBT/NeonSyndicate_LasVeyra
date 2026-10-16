using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The 04D encounter role overlay on one humanoid creature that belongs to a finite encounter. It owns the
    /// creature's decisions (bounded perception with range, view cone, line of sight and a reaction delay, hearing that
    /// only moves the last known position, search, the hard leash and the visible return home) and its role pattern
    /// (EncounterRole04D.Patterns.cs), while the creature's own body, movement, melee swings, abilities, damage and
    /// death stay exactly as they are. NpcController.ThinkCreature asks Think first (NpcController.Encounter04D.cs);
    /// attack tokens still come from CombatDirector. It goes away with its creature and never kills, heals or rewards.
    /// </summary>
    public sealed partial class EncounterRole04D : MonoBehaviour
    {
        /// <summary>Every live overlay (group alerts and the resonator's ally search scan only this short list).</summary>
        public static readonly List<EncounterRole04D> All = new List<EncounterRole04D>();

        private const float AlertRadius = 35f;
        private const float MinNoticeRange = 8f;

        public NpcController Npc { get; private set; }
        public EncounterRoleKind04D Kind { get; private set; }
        public ActorIntent04D Intent { get; private set; }
        public Vector3 Anchor { get; private set; }
        public Vector3 Origin { get; private set; }
        public int RunSerial { get; private set; }
        public Vector3 LastKnown { get; private set; }
        public bool HasLastKnown { get; private set; }
        /// <summary>The last think saw the player (range, view and a clear line).</summary>
        public bool Sees { get; private set; }
        /// <summary>This body can still do its job against the player: melee roles need walkable ground (no hovering or roof).</summary>
        public bool TargetReachable { get; private set; }
        /// <summary>Seconds of unrecovered pursuit (EncounterRules04D.AccumulatePursuit).</summary>
        public float Pursuit { get; private set; }
        /// <summary>A short plain description of what it is doing (debug page).</summary>
        public string Detail { get; private set; }

        private DifficultyLevel _difficulty;
        private float _sense = 30f;
        private float _intentAt;
        private float _goalSince = -1f;
        private float _lastSeenAt = -99f;
        private float _lastThink = -1f;
        private bool _heard;
        private float _alertReadyAt;
        private int _threatSerial;
        private bool _withdraw;
        private Vector3 _withdrawTo;
        private Action<DamageInfo> _onHurt;
        private bool _cleaned;

        public static void ResetStatics()
        {
            All.Clear();
            _hintAt = -99f;
            _aimMat = null;
            _aimLockedMat = null;
            _linkMat = null;
        }

        public void Init(NpcController npc, EncounterRoleKind04D kind, Vector3 anchor, Vector3 origin, int runSerial, DifficultyLevel difficulty)
        {
            Npc = npc;
            Kind = kind;
            Anchor = anchor;
            Origin = origin;
            RunSerial = runSerial;
            _difficulty = difficulty;
            _sense = npc != null && npc.CreatureSpecies != null ? npc.CreatureSpecies.Sense : 30f;
            Intent = ActorIntent04D.Guard;
            _intentAt = Time.time;
            _threatSerial = Threats.LatestSerial;
            Detail = "guards its post";
            if (!All.Contains(this)) All.Add(this);
            if (npc != null)
            {
                npc.EncounterRole = this;
                if (npc.Health != null)
                {
                    _onHurt = OnHurt;
                    npc.Health.Damaged += _onHurt;
                }
                npc.GuardAt(anchor);
            }
            InitPattern();
        }

        // ================================================================== thinking (the creature's think tick)

        /// <summary>Makes this think's decision for the creature. Always true while attached: its own temperament stands aside.</summary>
        public bool Think(float playerDist)
        {
            if (Npc == null || Npc.IsDead) return true;
            float now = Time.time;
            float dt = _lastThink < 0f ? 0.2f : Mathf.Min(1f, now - _lastThink);
            _lastThink = now;
            if (_withdraw)
            {
                ApplyWithdraw();
                return true;
            }
            PlayerController p = Game.Player;
            if (p == null || p.IsDead || !Game.IsPlaying)
            {
                HoldStill();
                return true;
            }
            Sense(p, playerDist, now);
            bool atGoal = AtGoal();
            if (atGoal) { if (_goalSince < 0f) _goalSince = now; }
            else _goalSince = -1f;
            EncounterRules04D.ActorFacts f = new EncounterRules04D.ActorFacts();
            f.Intent = Intent;
            f.IntentSeconds = now - _intentAt;
            f.Sees = Sees;
            f.TargetReachable = TargetReachable;
            f.Heard = _heard;
            f.HasLastKnown = HasLastKnown;
            f.SinceSeen = now - _lastSeenAt;
            f.Pursuit = Pursuit;
            f.DistanceFromOrigin = MathUtil.FlatDistance(Npc.transform.position, Origin);
            f.TargetDistanceFromOrigin = MathUtil.FlatDistance(p.transform.position, Origin);
            f.AtGoal = atGoal;
            f.GoalSeconds = _goalSince < 0f ? 0f : now - _goalSince;
            f.ReactionDelay = EncounterRules04D.ReactionDelay(_difficulty);
            ActorIntent04D next = EncounterRules04D.NextIntent(f);
            if (next == ActorIntent04D.Search && Intent == ActorIntent04D.Search && _heard) RestartSearch(now);
            if (next != Intent) SetIntent(next, now);
            _heard = false;
            Pursuit = EncounterRules04D.AccumulatePursuit(Pursuit, Intent, Sees, TargetReachable, dt);
            Apply(p, playerDist, now);
            return true;
        }

        /// <summary>Sight (range from the species' sense and the player's stealth, a view cone until engaged, a clear line) and hearing.</summary>
        private void Sense(PlayerController p, float dist, float now)
        {
            bool engaged = Intent == ActorIntent04D.Engage || Intent == ActorIntent04D.Acquire;
            float range;
            if (engaged) range = EncounterRules04D.EngagedSightRange;
            else
            {
                bool fog = Game.Atmo != null && Game.Atmo.Current == Weather.Fog;
                float night = Game.Atmo != null ? Game.Atmo.NightFactor : 0f;
                CreatureSpecies sp = Npc.CreatureSpecies;
                bool nocturnal = sp != null && sp.Night && !sp.Day;
                float speed = p.InVehicle && p.CurrentVehicle != null ? p.CurrentVehicle.Speed : p.FlatVelocity.magnitude;
                range = CreatureBrainLogic.NoticeRange(_sense, SkillMods.CreatureSense, p.Crouched, speed, p.InVehicle, night, nocturnal, fog);
                if (Intent == ActorIntent04D.Search) range *= 1.3f;
                range = Mathf.Max(range, MinNoticeRange);
            }
            bool seen = false;
            if (dist <= range)
            {
                Vector3 to = MathUtil.Flat(p.transform.position - Npc.transform.position);
                float angle = to.sqrMagnitude > 0.01f ? Vector3.Angle(Npc.transform.forward, to) : 0f;
                bool view = engaged || CreatureBrainLogic.CanNotice(dist, angle, range);
                if (view) seen = !Physics.Linecast(Npc.HeadPosition, p.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore);
            }
            Sees = seen;
            if (seen)
            {
                _lastSeenAt = now;
                LastKnown = p.transform.position;
                HasLastKnown = true;
            }
            // Ranged and support roles can still do their job against a raised target; melee bodies cannot.
            TargetReachable = Kind == EncounterRoleKind04D.Refractor || Kind == EncounterRoleKind04D.Resonator
                || EncounterRules04D.Reachable(p.transform.position.y, Npc.transform.position.y);
            Listen(p);
        }

        /// <summary>A player noise within earshot moves the last known position; it never hands over exact aim.</summary>
        private void Listen(PlayerController p)
        {
            ThreatEvent e;
            if (Threats.Query(Npc.transform.position, _threatSerial, out e))
            {
                bool fromPlayer = Game.IsPlayer(e.Source) || (p.InVehicle && p.CurrentVehicle != null && e.Source == p.CurrentVehicle.gameObject);
                float reach = Mathf.Min(45f, CreatureBrainLogic.NoiseReach(e.Kind, _sense) * Mathf.Clamp(SkillMods.CreatureSense, 0.3f, 2f));
                if (fromPlayer && reach > 0f && Vector3.Distance(e.Position, Npc.transform.position) <= reach)
                {
                    LastKnown = e.Position;
                    HasLastKnown = true;
                    _heard = true;
                }
            }
            _threatSerial = Threats.LatestSerial;
        }

        private bool AtGoal()
        {
            if (Intent == ActorIntent04D.Search) return MathUtil.FlatDistance(Npc.transform.position, LastKnown) < EncounterRules04D.GoalTolerance;
            if (Intent == ActorIntent04D.Return) return MathUtil.FlatDistance(Npc.transform.position, Anchor) < EncounterRules04D.GoalTolerance;
            return false;
        }

        private void SetIntent(ActorIntent04D next, float now)
        {
            ActorIntent04D prev = Intent;
            if (prev == ActorIntent04D.Engage) ExitEngage(now);
            if (prev == ActorIntent04D.Acquire && Npc.Anim != null) Npc.Anim.Telegraph = 0f;
            Intent = next;
            _intentAt = now;
            _goalSince = -1f;
            switch (next)
            {
                case ActorIntent04D.Guard:
                    Pursuit = 0f;
                    Npc.CreatureWarning = false;
                    Npc.GuardAt(Anchor);
                    Detail = "guards its post";
                    break;
                case ActorIntent04D.Acquire:
                    Npc.GuardAt(Npc.transform.position);
                    Npc.CreatureWarning = true;
                    Detail = "spotted you";
                    BroadcastAlert(now);
                    break;
                case ActorIntent04D.Engage:
                    Npc.CreatureWarning = false;
                    EnterEngage(now);
                    BroadcastAlert(now);
                    break;
                case ActorIntent04D.Search:
                    Npc.CreatureWarning = false;
                    Npc.MoveTo(LastKnown, true, null);
                    Detail = "searches where it last saw or heard you";
                    break;
                case ActorIntent04D.Return:
                    Pursuit = 0f;
                    Npc.CreatureWarning = false;
                    ReleaseTokens();
                    Npc.CreatureBusyUntil = Mathf.Min(Npc.CreatureBusyUntil, now);
                    Npc.MoveTo(Anchor, false, null);
                    Detail = "gives up and walks back to its post";
                    break;
            }
        }

        private void RestartSearch(float now)
        {
            _intentAt = now;
            _goalSince = -1f;
            Npc.MoveTo(LastKnown, true, null);
        }

        /// <summary>Re-asserts the creature's state for the current intent (the creature's own damage reaction may have changed it).</summary>
        private void Apply(PlayerController p, float dist, float now)
        {
            switch (Intent)
            {
                case ActorIntent04D.Guard:
                    if (Npc.State != NpcState.Idle) Npc.GuardAt(Anchor);
                    break;
                case ActorIntent04D.Acquire:
                    if (Npc.State != NpcState.Idle) Npc.GuardAt(Npc.transform.position);
                    Npc.CreatureWarning = true;
                    if (Npc.Anim != null) Npc.Anim.Telegraph = 0.3f;
                    break;
                case ActorIntent04D.Search:
                    if (Npc.State != NpcState.Scripted) Npc.MoveTo(LastKnown, true, null);
                    break;
                case ActorIntent04D.Return:
                    if (Npc.State != NpcState.Scripted) Npc.MoveTo(Anchor, false, null);
                    break;
                case ActorIntent04D.Engage:
                    ApplyEngage(p, dist, now);
                    break;
            }
        }

        /// <summary>Shared alerts are bounded: nearby guards of the same encounter get the last known position, never live aim.</summary>
        private void BroadcastAlert(float now)
        {
            if (now < _alertReadyAt || !HasLastKnown) return;
            _alertReadyAt = now + 3f;
            for (int i = 0; i < All.Count; i++)
            {
                EncounterRole04D o = All[i];
                if (o == null || o == this || o.RunSerial != RunSerial || o.Npc == null || o.Npc.IsDead) continue;
                if (MathUtil.FlatDistance(o.Npc.transform.position, Npc.transform.position) > AlertRadius) continue;
                o.AlertFrom(LastKnown);
            }
        }

        /// <summary>An ally called out: a guarding opponent goes to look (it still has to see the player to fight).</summary>
        public void AlertFrom(Vector3 lastKnown)
        {
            if (Intent != ActorIntent04D.Guard || _withdraw) return;
            LastKnown = lastKnown;
            HasLastKnown = true;
            _heard = true;
        }

        private void OnHurt(DamageInfo info)
        {
            if (Npc == null || Npc.IsDead) return;
            if (_withdraw)
            {
                ApplyWithdraw();
                return;
            }
            if (info.FromPlayer && Game.Player != null)
            {
                LastKnown = Game.Player.transform.position;
                HasLastKnown = true;
                _heard = true;
            }
            OnHurtPattern(info);
            PlayerController p = Game.Player;
            if (p == null || p.IsDead) return;
            if (Intent == ActorIntent04D.Engage) ReassertEngage(p, Time.time);
            else Apply(p, MathUtil.FlatDistance(Npc.transform.position, p.transform.position), Time.time);
        }

        private void HoldStill()
        {
            if (Npc.State == NpcState.Fight || Npc.State == NpcState.Scripted) Npc.GuardAt(Npc.transform.position);
            Npc.CreatureWarning = false;
        }

        // ================================================================== withdrawal (abort)

        /// <summary>The encounter ended without a victory: stop every threat and walk away (the run removes it off camera).</summary>
        public void Withdraw(Vector3 to)
        {
            if (_withdraw) return;
            _withdraw = true;
            _withdrawTo = to;
            StopPatterns();
            Intent = ActorIntent04D.Withdraw;
            Detail = "withdraws";
            if (Npc == null || Npc.IsDead) return;
            Npc.Target = null;
            Npc.Hostile = false;
            Npc.CreatureProvoked = false;
            Npc.CreatureWarning = false;
            Npc.CreatureBusyUntil = 0f;
            Npc.MoveTo(to, true, null);
        }

        private void ApplyWithdraw()
        {
            if (Npc == null || Npc.IsDead) return;
            Npc.Target = null;
            Npc.Hostile = false;
            Npc.CreatureProvoked = false;
            if (Npc.State != NpcState.Scripted) Npc.MoveTo(_withdrawTo, true, null);
        }

        private void ReleaseTokens()
        {
            if (Npc == null) return;
            CombatDirector.ReleaseMeleeToken(Npc);
            CombatDirector.ReleaseShotToken(Npc);
            _hasShotToken = false;
        }

        // ================================================================== lifetime

        private void OnDisable()
        {
            Cleanup();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            if (_cleaned) return;
            _cleaned = true;
            StopPatterns();
            if (Npc != null)
            {
                if (_onHurt != null && Npc.Health != null) Npc.Health.Damaged -= _onHurt;
                if (Npc.EncounterRole == this) Npc.EncounterRole = null;
            }
            _onHurt = null;
            All.Remove(this);
        }
    }
}
