using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The assists. Combat (U on a valid aimed or locked hostile within 25 m, in line of sight and reachable): the
    /// drakeling's Glass Lance wounds and marks the target Glassbound so the player's next gun hit bursts; the warden
    /// plants between the player and the target and its shell takes part of the hits from that side (the brace hook in
    /// the player's damage path, with a capacity, a break and strain on the warden); the skimmer's Static Snare staggers
    /// the target and stops its shots for a moment (bosses only lose balance). Exploration (U otherwise): the drakeling
    /// lights a trail to a hidden cool route nearby or along the tracked route; the warden braces the nearby quest
    /// mechanism or senses dangerous heat and rings it on the ground; the skimmer flies up to bridge the relay or quest
    /// point and lights the route from above. Every exploration assist raises ExplorationAssist. Assist damage uses
    /// Source = the player and Origin = Companion, so kills and rewards are credited once, through the shared pipeline.
    /// An Underheart boss is not a character, so in a boss fight with no valid character target the aimed boss body,
    /// lure or furnace vent comes from Game.UnderBosses.TryAssistTarget and the lance, snare and brace act on it.
    /// </summary>
    public sealed partial class BondSystem
    {
        private enum AssistKind
        {
            None,
            Lance,
            Snare,
            Brace,
            Trail,
            BraceMechanism,
            HeatSense,
            RelayHop,
            Scout
        }

        /// <summary>Underheart anchors a guide can reveal: hidden cool routes, side paths and caches.</summary>
        public static readonly string[] RevealAnchors = { "canals.assist_route", "gardens.cool_route_a", "gardens.cool_route_b", "reliquary.safe_route", "canals.cool_pocket", "canals.branch_cache", "service.service_panel" };
        /// <summary>Underheart anchors a warden can brace (gates, valves, hoists, actuators).</summary>
        public static readonly string[] MechanismAnchors = { "service.gate", "canals.gate", "canals.pressure_valve", "canals.actuator", "workshops.hoist", "crown.valve_mech" };
        /// <summary>Underheart anchors a skimmer can bridge (relays, lifts, reflectors, the descent window).</summary>
        public static readonly string[] RelayAnchors = { "workshops.storm_relay", "refuge.lift", "crown.reflector_mech", "reliquary.window", "crown.lift" };

        private const string BraceHookKey = "bond_warden_brace";
        /// <summary>Before the handheld ward (the shell is in front of the player), after dodge, guard and parry.</summary>
        private const int BraceHookOrder = 40;

        private AssistKind _ak = AssistKind.None;
        private int _stage;
        private float _at;
        private CharacterBase _target;
        /// <summary>The boss body, lure or vent the combat assist acts on when there is no character target (else null).</summary>
        private IDamageable _bossTarget;
        private Vector3 _bossPoint;
        private Vector3 _point;
        private readonly List<Vector3> _route = new List<Vector3>();
        private readonly List<Vector3> _scratch = new List<Vector3>();
        private int _assistUid;
        private int _prevState;
        /// <summary>Per companion uid: x = Time.time the combat assist is ready, y = Time.time the exploration assist is ready.</summary>
        private readonly Dictionary<int, Vector2> _cooldowns = new Dictionary<int, Vector2>();

        private bool _bracing;
        private float _braceUntil;
        private float _braceCap;
        private float _braceCapMax;
        private BraceStats _brace;
        private Vector3 _braceDir = Vector3.forward;
        private Vector3 _bracePoint;
        private CharacterBase _braceTarget;
        private IDamageable _braceBoss;
        private float _pendingStrain;
        private float _braceCue;
        private float _coverHint;

        private bool AssistBusy { get { return _ak != AssistKind.None; } }

        public float LastExplorationAssistTime { get { return _lastExploreTime; } }
        public Vector3 LastExplorationAssistPoint { get { return _lastExplorePoint; } }

        /// <summary>Seconds until a companion's combat assist is ready (0 when ready).</summary>
        public float CombatCooldownLeft(int uid)
        {
            Vector2 c;
            return _cooldowns.TryGetValue(uid, out c) ? Mathf.Max(0f, c.x - Time.time) : 0f;
        }

        /// <summary>Seconds until a companion's exploration assist is ready (0 when ready).</summary>
        public float ExploreCooldownLeft(int uid)
        {
            Vector2 c;
            return _cooldowns.TryGetValue(uid, out c) ? Mathf.Max(0f, c.y - Time.time) : 0f;
        }

        private void SetCooldown(int uid, bool combat, float seconds)
        {
            Vector2 c;
            if (!_cooldowns.TryGetValue(uid, out c)) c = Vector2.zero;
            if (combat) c.x = Time.time + Mathf.Max(0f, seconds);
            else c.y = Time.time + Mathf.Max(0f, seconds);
            _cooldowns[uid] = c;
        }

        /// <summary>True for a living foe the companion may act against: a hostile person, or a creature hostile to the
        /// player (calm habitat residents, civilians, staff and escorts never qualify).</summary>
        public static bool IsHostileTarget(CharacterBase c)
        {
            PlayerController pl = Game.Player;
            if (c == null || pl == null || c.IsDead || c.IsPlayer) return false;
            if (!LootCombat.IsFoe(c, pl.gameObject)) return false;
            NpcController n = c as NpcController;
            if (n != null) return n.IsHostileToPlayer;
            Creature cr = c as Creature;
            return cr != null && cr.IsHostileToPlayer && cr.Targetable;
        }

        private static bool FoeNear(Vector3 p, float radius)
        {
            PlayerController pl = Game.Player;
            if (pl == null) return false;
            List<CharacterBase> foes = LootCombat.FoesAround(p, radius, pl.gameObject);
            for (int i = 0; i < foes.Count; i++) if (IsHostileTarget(foes[i])) return true;
            return false;
        }

        /// <summary>True while an Underheart boss is being fought (its body can be hit).</summary>
        private static bool BossFightOn
        {
            get
            {
                UnderheartBosses b = Game.UnderBosses;
                UnderBossFight f = b != null ? b.ActiveFight : null;
                return f != null && f.AcceptsAssists;
            }
        }

        /// <summary>
        /// The boss body, lure or furnace vent the player aims at (boss fights only: lock-on and the character aim never
        /// find them). The crosshair ray first; when it stopped on something in front of the body (a prop edge, an add's
        /// limb), the body itself when it lies within a few degrees of the aim direction.
        /// </summary>
        private static bool AimedBossTarget(PlayerController pl, out IDamageable target, out Vector3 point)
        {
            target = null;
            point = Vector3.zero;
            UnderheartBosses b = Game.UnderBosses;
            if (pl == null || b == null || !BossFightOn) return false;
            float range = BondRules.AssistRange;
            if (b.TryAssistTarget(pl.HeadPosition, pl.AimPoint, range, out point, out target)) return true;
            UnderBossFight f = b.ActiveFight;
            if (f == null || Game.Cam == null) return false;
            Ray ray = Game.Cam.AimRay;
            Vector3 c = f.BodyCenter;
            Vector3 to = c - ray.origin;
            if (to.sqrMagnitude < 1f || Vector3.Angle(ray.direction, to) > 12f) return false;
            return b.TryAssistTarget(pl.HeadPosition, c, range, out point, out target);
        }

        /// <summary>True while the assist's target can still be struck (a hostile character, or a live boss body, lure or vent).</summary>
        private bool TargetLive()
        {
            if (_target != null) return IsHostileTarget(_target);
            Vector3 p;
            if (_bossTarget == null || Game.UnderBosses == null || !Game.UnderBosses.AssistTargetAlive(_bossTarget, out p)) return false;
            _bossPoint = p;
            return true;
        }

        /// <summary>Where the assist aims: the character's chest, or the boss target's current point.</summary>
        private Vector3 TargetPoint { get { return _target != null ? _target.ChestPosition : _bossPoint; } }

        // ================================================================== U pressed

        private void RequestAssist()
        {
            CompanionRecord r = Active;
            PlayerController pl = Game.Player;
            if (r == null || pl == null)
            {
                Refuse(_rescue != null ? AssistRefusal.Rescue : AssistRefusal.NoCompanion, null, 0f);
                return;
            }
            CharacterBase locked = pl.Combat != null ? pl.Combat.LockTarget : null;
            CharacterBase aimed = pl.AimTarget;
            CharacterBase target = IsHostileTarget(locked) ? locked : (IsHostileTarget(aimed) ? aimed : null);
            IDamageable boss = null;
            Vector3 bossPoint = Vector3.zero;
            if (target == null) AimedBossTarget(pl, out boss, out bossPoint);
            bool hasTarget = target != null || boss != null;
            bool neutral = !hasTarget && pl.Aiming && aimed != null && !aimed.IsDead && !aimed.IsPlayer;
            bool fight = (CombatDirector.InCombat && FoeNear(pl.transform.position, BondRules.AssistRange)) || BossFightOn;
            AssistRefusal refusal;
            AssistIntent intent = BondAssistRules.Decide(hasTarget, neutral, fight, out refusal);
            if (intent == AssistIntent.Refuse)
            {
                Refuse(refusal, r, 0f);
                return;
            }
            bool combat = intent == AssistIntent.Combat;
            float cd = combat ? CombatCooldownLeft(r.Uid) : ExploreCooldownLeft(r.Uid);
            AssistRefusal c = BondAssistRules.ValidateCompanion(true, r.Rank, r.State, combat, AssistBusy, cd, r.State == (int)CompanionState.Fused, pl.InVehicle);
            if (c == AssistRefusal.None && _body == null) c = AssistRefusal.Busy;
            if (c == AssistRefusal.None && r.State == (int)CompanionState.Waiting && (_body.transform.position - pl.transform.position).magnitude > 18f) c = AssistRefusal.Waiting;
            if (c != AssistRefusal.None)
            {
                Refuse(c, r, cd);
                return;
            }
            if (combat)
            {
                Vector3 pp = pl.transform.position;
                Vector3 tp = target != null ? target.transform.position : bossPoint;
                Vector3 aimAt = target != null ? target.ChestPosition : bossPoint;
                // A boss part or lure is reached from the arena floor it stands on, not from its centre high on the body.
                UnderBossFight f = target == null && Game.UnderBosses != null ? Game.UnderBosses.ActiveFight : null;
                if (f != null) tp.y = Mathf.Min(tp.y, f.ArenaCenter.y);
                float dist = Vector3.Distance(pp, tp);
                bool los = !Physics.Linecast(pl.HeadPosition, aimAt, Layers.WorldMask, QueryTriggerInteraction.Ignore);
                bool same = WorldBounds.SameLayer(pp, tp);
                AssistRefusal t = BondAssistRules.ValidateTarget(dist, los, same, tp.y - pp.y, CompanionArchetypes.CanFly(ProfileOf(r)));
                if (t != AssistRefusal.None)
                {
                    Refuse(t, r, 0f);
                    return;
                }
                StartCombat(r, target, boss, bossPoint);
            }
            else StartExplore(r);
        }

        private void BeginAssist(CompanionRecord r, AssistKind kind)
        {
            _ak = kind;
            _assistUid = r.Uid;
            _prevState = r.State == (int)CompanionState.Assisting ? (int)CompanionState.Following : r.State;
            r.State = (int)CompanionState.Assisting;
            _stage = 0;
            _at = 0f;
        }

        /// <summary>Ends the current assist and hands the body back to following (or waiting).</summary>
        private void EndAssist()
        {
            if (_ak == AssistKind.None) return;
            AssistKind was = _ak;
            _ak = AssistKind.None;
            _target = null;
            _bossTarget = null;
            CompanionRecord r = Find(_assistUid);
            if (r != null && r.State == (int)CompanionState.Assisting) r.State = _prevState == (int)CompanionState.Waiting ? (int)CompanionState.Waiting : (int)CompanionState.Following;
            if (_body != null && _body.Mode != CompanionMode.Downed)
            {
                _body.Mode = r != null && r.State == (int)CompanionState.Waiting ? CompanionMode.Wait : CompanionMode.Follow;
                _body.ClearAction();
                _body.ClearLook();
            }
            if (was == AssistKind.Brace && _bracing) EndBrace(false);
        }

        private void Fizzle(CompanionRecord r, string why)
        {
            if (r != null)
            {
                SetCooldown(r.Uid, true, 2f);
                Say(NameOf(r) + " " + why, 2.5f, true);
            }
            PlayRefuse(r != null);
            EndAssist();
        }

        // ================================================================== combat assists

        /// <summary>Starts a combat assist on a hostile character, or (target null) on a boss body, lure or vent.</summary>
        private void StartCombat(CompanionRecord r, CharacterBase target, IDamageable boss, Vector3 bossPoint)
        {
            CompanionProfile prof = ProfileOf(r);
            switch (prof.Combat)
            {
                case CombatAssistKind.Brace: BeginAssist(r, AssistKind.Brace); break;
                case CombatAssistKind.Disruption: BeginAssist(r, AssistKind.Snare); break;
                default: BeginAssist(r, AssistKind.Lance); break;
            }
            _target = target;
            _bossTarget = target == null ? boss : null;
            _bossPoint = bossPoint;
            SetCooldown(r.Uid, true, BondRules.CombatCooldown(r.Archetype, r.Rank, r.Specialization));
            Vector3 aim = TargetPoint;
            if (_ak == AssistKind.Brace)
            {
                PlayerController pl = Game.Player;
                Vector3 to = MathUtil.Flat(aim - pl.transform.position);
                _braceDir = to.sqrMagnitude > 0.01f ? to.normalized : pl.transform.forward;
                _bracePoint = BracePoint(pl.transform.position, _braceDir);
                _body.SetGoal(_bracePoint, prof.SprintSpeed, false);
            }
            else
            {
                Vector3 side = SideOfPlayer(prof, aim);
                if (!LineClear(_body.HeadPosition, aim) || (_body.transform.position - Game.Player.transform.position).magnitude > 6f)
                    _body.SetGoal(side, prof.SprintSpeed, CompanionArchetypes.CanFly(prof) && prof.Locomotion == CompanionLocomotion.Hover);
            }
            _body.SetLook(aim);
            _body.Glow(1f);
        }

        private static bool LineClear(Vector3 a, Vector3 b)
        {
            return !Physics.Linecast(a, b, Layers.WorldMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>A spot at the player's side, a little toward the target, on the floor (flyers a bit higher).</summary>
        private static Vector3 SideOfPlayer(CompanionProfile prof, Vector3 targetPoint)
        {
            PlayerController pl = Game.Player;
            Vector3 pp = pl.transform.position;
            Vector3 to = MathUtil.Flat(targetPoint - pp);
            if (to.sqrMagnitude < 0.01f) to = pl.transform.forward;
            to.Normalize();
            Vector3 right = new Vector3(to.z, 0f, -to.x) * (prof.Side >= 0 ? 1f : -1f);
            Vector3 p = pp + right * 1.4f + to * 0.6f;
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up * 1.2f, Vector3.down, out hit, 3f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) p = hit.point;
            if (prof.Locomotion == CompanionLocomotion.Hover) p.y += prof.HoverHeight;
            return p;
        }

        /// <summary>Where the warden plants: 1.3 m in front of the player toward the threat, on solid floor and clear of walls.</summary>
        private static Vector3 BracePoint(Vector3 player, Vector3 dir)
        {
            Vector3 p = player + dir * 1.3f;
            RaycastHit hit;
            if (Physics.Linecast(player + Vector3.up * 0.6f, p + Vector3.up * 0.6f, out hit, Layers.WorldMask, QueryTriggerInteraction.Ignore))
                p = player + dir * Mathf.Max(0.5f, hit.distance - 0.4f);
            if (Physics.Raycast(p + Vector3.up * 1.2f, Vector3.down, out hit, 3f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) p = hit.point;
            else p = player;
            return p;
        }

        private void TickAssist(float dt)
        {
            if (_ak == AssistKind.None) return;
            CompanionRecord r = Find(_assistUid);
            if (r == null || _body == null || r != Active || r.State == (int)CompanionState.Downed || r.State == (int)CompanionState.Fused || Game.Player == null)
            {
                EndAssist();
                return;
            }
            if (dt <= 0f) return;
            _at += dt;
            switch (_ak)
            {
                case AssistKind.Lance:
                case AssistKind.Snare:
                    TickStrike(r);
                    break;
                case AssistKind.Brace:
                    if (_stage == 0 && !TargetLive())
                    {
                        Fizzle(r, "lost the target.");
                        return;
                    }
                    if (_stage == 0 && (_body.GoalReached || _at > 1.6f))
                    {
                        _stage = 1;
                        StartBrace(r);
                    }
                    break;
                case AssistKind.Trail:
                    if (_stage == 1)
                    {
                        if (_body.GoalReached || _at > 3f) EndAssist();
                    }
                    else if (!_body.ActionPlaying && _at > 1.3f) EndAssist();
                    break;
                case AssistKind.BraceMechanism:
                    if (_stage == 0)
                    {
                        if (_body.GoalReached)
                        {
                            _stage = 1;
                            _at = 0f;
                            _body.Mode = CompanionMode.Goal;
                            _body.SetGoal(_body.transform.position, 0.5f, false);
                            _body.HoldAction(CreatureAction.Charge, 0.47f, 0.35f, 0.6f);
                            _body.SetLook(_point + Vector3.up * 0.8f);
                            _body.Glow(1.2f);
                            Underheart04ESounds.Play(UhSfx.GuardianBrace, _point, 0.9f, 1f);
                            if (Game.Fx != null) Game.Fx.Sparks(_point + Vector3.up * 0.8f, Vector3.up * 2f, 12);
                            RaiseExplore(_point);
                            Say(NameOf(r) + " braces it. The mechanism holds while it pushes.", 3f, false);
                        }
                        else if (_at > 6f)
                        {
                            Say(NameOf(r) + " cannot reach that mechanism from here.", 3f, true);
                            EndAssist();
                        }
                    }
                    else if (_at > 4f) EndAssist();
                    break;
                case AssistKind.HeatSense:
                    if (!_body.ActionPlaying && _at > 1.3f) EndAssist();
                    break;
                case AssistKind.RelayHop:
                    if (_stage == 0)
                    {
                        if (_body.GoalReached || _at > 6f)
                        {
                            if (!_body.GoalReached && (_body.transform.position - _point).magnitude > 6f)
                            {
                                Say(NameOf(r) + " cannot reach that point from here.", 3f, true);
                                EndAssist();
                                return;
                            }
                            _stage = 1;
                            _at = 0f;
                            _body.SetLook(_point);
                            _body.PlayAction(CreatureAction.Spit, 0.8f, 0.4f, 0.6f, OnRelayStrike);
                        }
                    }
                    else if (!_body.ActionPlaying && _at > 0.9f) EndAssist();
                    break;
                case AssistKind.Scout:
                    if (_stage == 0)
                    {
                        if (_body.GoalReached || _at > 3f)
                        {
                            _stage = 1;
                            _at = 0f;
                            _body.PlayAction(CreatureAction.Howl, 1f, 0.35f, 0.7f, OnScoutReveal);
                        }
                    }
                    else if (!_body.ActionPlaying && _at > 1.5f) EndAssist();
                    break;
            }
        }

        private void TickStrike(CompanionRecord r)
        {
            if (!TargetLive())
            {
                if (_stage == 0) Fizzle(r, "lost the target.");
                else if (!_body.ActionPlaying) EndAssist();
                return;
            }
            _body.SetLook(TargetPoint);
            if (_stage == 0)
            {
                bool clear = LineClear(_body.HeadPosition, TargetPoint);
                bool settled = _body.Mode != CompanionMode.Goal || _body.GoalReached;
                if ((clear && settled) || _at > 1.4f)
                {
                    if (!clear)
                    {
                        Fizzle(r, "has no clear line to the target.");
                        return;
                    }
                    _stage = 1;
                    _at = 0f;
                    _body.SetGoal(_body.transform.position, 0.5f, _body.Flying);
                    if (_ak == AssistKind.Lance) _body.PlayAction(CreatureAction.Breath, 0.9f, 0.45f, 0.65f, OnLanceStrike);
                    else _body.PlayAction(CreatureAction.Spit, 0.7f, 0.4f, 0.6f, OnSnareStrike);
                    _body.Glow(1.6f);
                }
            }
            else if (!_body.ActionPlaying && _at > 0.3f) EndAssist();
        }

        private void OnLanceStrike()
        {
            if (_ak == AssistKind.None) return;
            CompanionRecord r = Find(_assistUid);
            if (r == null || _body == null || !TargetLive())
            {
                Fizzle(r, "lost the target.");
                return;
            }
            Vector3 from = _body.MouthPosition, to = TargetPoint;
            if (!LineClear(from, to))
            {
                Fizzle(r, "has no clear line to the target.");
                return;
            }
            CompanionProfile prof = ProfileOf(r);
            float dmg = BondRules.AssistDamage(r.Archetype, r.Rank, r.Tuning, r.Specialization);
            if (Game.Fx != null)
            {
                Game.Fx.Tracer(from, to);
                Game.Fx.CrystalBurst(to, prof.Accent, 14, 0.5f);
                Game.Fx.Flash(to, prof.Accent, 2.5f, 5f, 0.2f);
            }
            Underheart04ESounds.Play(UhSfx.AssistBurst, to, 1f, 1f);
            if (_target == null)
            {
                // A boss body, lure or vent: the lance wounds it (no Glassbound mark: that lives on characters).
                DealDamage(_bossTarget, to, dmg, from, 12f);
                ExposureAt(r, to);
                return;
            }
            CharacterBase primary = _target;
            DealDamage(primary, dmg, from, 12f);
            if (!primary.IsDead) ResonanceSystem.Prime(primary, ResonanceState.Glassbound);
            if (r.Specialization == BondRules.SpecPrism)
            {
                PlayerController pl = Game.Player;
                CharacterBase second = pl != null ? LootCombat.NearestFoe(to, 6f, pl.gameObject, primary, null) : null;
                if (second != null && IsHostileTarget(second) && LineClear(to, second.ChestPosition))
                {
                    if (Game.Fx != null) Game.Fx.Tracer(to, second.ChestPosition);
                    DealDamage(second, dmg * 0.5f, to, 6f);
                }
            }
            if (!primary.IsDead && Game.Hud != null && _data.HintsShown < 6)
            {
                _data.HintsShown++;
                Game.Hud.CombatText("Glassbound: your next gun hit bursts", prof.Accent);
            }
            Exposure(r, primary);
        }

        private void OnSnareStrike()
        {
            if (_ak == AssistKind.None) return;
            CompanionRecord r = Find(_assistUid);
            if (r == null || _body == null || !TargetLive())
            {
                Fizzle(r, "lost the target.");
                return;
            }
            Vector3 from = _body.ChestPosition, to = TargetPoint;
            if (!LineClear(from, to))
            {
                Fizzle(r, "has no clear line to the target.");
                return;
            }
            CompanionProfile prof = ProfileOf(r);
            float dmg = BondRules.AssistDamage(r.Archetype, r.Rank, r.Tuning, r.Specialization);
            Arc(from, to, prof.Accent);
            Underheart04ESounds.Play(UhSfx.SkimmerArc, to, 1f, 1f);
            if (_target == null)
            {
                // A boss body, lure or vent: the snare's arc damages it; a boss is never stunned by it.
                DealDamage(_bossTarget, to, dmg, from, 25f);
                ExposureAt(r, to);
                return;
            }
            CharacterBase primary = _target;
            DealDamage(primary, dmg, from, 25f);
            Disrupt(primary, r, 1f);
            if (r.Specialization == BondRules.SpecStaticNet)
            {
                PlayerController pl = Game.Player;
                CharacterBase second = pl != null ? LootCombat.NearestFoe(to, 7f, pl.gameObject, primary, null) : null;
                if (second != null && IsHostileTarget(second) && LineClear(to, second.ChestPosition))
                {
                    Arc(to, second.ChestPosition, prof.Accent);
                    DealDamage(second, dmg * 0.5f, to, 15f);
                    Disrupt(second, r, 0.6f);
                }
            }
            Exposure(r, primary);
        }

        private static void Arc(Vector3 from, Vector3 to, Color c)
        {
            if (Game.Fx == null) return;
            Game.Fx.Tracer(from, to);
            Vector3 mid = Vector3.Lerp(from, to, 0.5f) + Random.insideUnitSphere * 0.25f;
            Game.Fx.Tracer(from, mid);
            Game.Fx.Sparks(to, Vector3.up * 2f, 10);
            Game.Fx.Flash(to, c, 2.2f, 5f, 0.15f);
        }

        /// <summary>One hit credited to the player (Source) as companion damage (Origin): the shared pipeline pays once.</summary>
        private static void DealDamage(CharacterBase target, float amount, Vector3 from, float poise)
        {
            PlayerController pl = Game.Player;
            if (pl == null || target == null || target.Health == null || target.IsDead || amount <= 0f) return;
            Vector3 at = target.ChestPosition;
            DamageInfo info = DamageInfo.Make(amount, at, at - from, pl.gameObject, DamageType.Power);
            info.Origin = DamageOrigin.Companion;
            info.Poise = poise;
            info.Force = 2f;
            target.Health.TakeDamage(info);
        }

        /// <summary>The same companion hit on a receiver that is not a character (a boss body, lure or vent).</summary>
        private static void DealDamage(IDamageable target, Vector3 at, float amount, Vector3 from, float poise)
        {
            PlayerController pl = Game.Player;
            if (pl == null || target == null || !target.IsAlive || amount <= 0f) return;
            DamageInfo info = DamageInfo.Make(amount, at, at - from, pl.gameObject, DamageType.Power);
            info.Origin = DamageOrigin.Companion;
            info.Poise = poise;
            info.Force = 2f;
            target.TakeDamage(info);
        }

        /// <summary>The skimmer's disruption: a stagger that also stops shots (bosses only lose balance, never a stun lock).</summary>
        private void Disrupt(CharacterBase target, CompanionRecord r, float share)
        {
            if (target == null || target.IsDead) return;
            bool boss = ResonanceSystem.IsBossLike(target);
            Creature cr = target as Creature;
            NpcController n = target as NpcController;
            bool elite = (cr != null && cr.Elite) || (n != null && n.Elite);
            float secs = BondRules.DisruptSeconds(boss, elite, r.Rank, r.Specialization) * share;
            if (cr != null)
            {
                if (boss) cr.DrainPoise(cr.MaxPoise * 0.35f * share);
                else if (secs > 0f) cr.Stagger(secs);
            }
            else if (n != null)
            {
                if (boss) n.Poise -= n.MaxPoise * 0.35f * share;
                else if (secs > 0f) n.CreatureStagger(secs);
            }
            if (Game.Hud != null) Game.Hud.CombatText(boss ? "Off balance" : "Disrupted", ProfileOf(r).Accent);
        }

        /// <summary>Helping costs a little health when the assist puts the companion right next to a foe.</summary>
        private void Exposure(CompanionRecord r, CharacterBase target)
        {
            if (_body == null || target == null) return;
            ExposureAt(r, target.transform.position);
        }

        private void ExposureAt(CompanionRecord r, Vector3 targetPos)
        {
            if (_body == null || r == null) return;
            bool close = (targetPos - _body.transform.position).magnitude < 4f;
            float cost = BondRules.AssistExposure(r.Archetype, MaxHealthOf(r), close);
            if (cost > 0f) HurtActive(cost, targetPos - _body.transform.position);
        }

        // ================================================================== the brace

        private void StartBrace(CompanionRecord r)
        {
            _brace = BondRules.Brace(r.Rank, r.Tuning, r.Specialization);
            _bracing = true;
            _braceUntil = Time.time + _brace.Duration;
            _braceCap = _brace.Capacity;
            _braceCapMax = _brace.Capacity;
            _braceTarget = _target;
            _braceBoss = _target == null ? _bossTarget : null;
            _pendingStrain = 0f;
            _body.Mode = CompanionMode.Goal;
            _body.SetGoal(_body.transform.position, 0.5f, false);
            _body.HoldAction(CreatureAction.Charge, 0.47f, 0.35f, 0.6f);
            _body.Glow(1.5f);
            Underheart04ESounds.Play(UhSfx.GuardianBrace, _body.ChestPosition, 1f, 1f);
            if (Game.Fx != null) Game.Fx.Sparks(_body.ChestPosition + _braceDir * 0.4f, _braceDir * 2f, 14);
            Say(NameOf(r) + " braces against hits from that side. Stay close to it.", 3f, false);
        }

        private void TickBrace(float dt)
        {
            if (!_bracing) return;
            if (_pendingStrain > 0f)
            {
                float s = _pendingStrain;
                _pendingStrain = 0f;
                HurtActive(s, -_braceDir);
                if (!_bracing) return;
            }
            PlayerController pl = Game.Player;
            if (pl == null || _body == null)
            {
                EndBrace(false);
                return;
            }
            Vector3 bossAt;
            if (_braceTarget != null && !_braceTarget.IsDead)
            {
                Vector3 to = MathUtil.Flat(_braceTarget.transform.position - pl.transform.position);
                if (to.sqrMagnitude > 0.01f) _braceDir = Vector3.Slerp(_braceDir, to.normalized, Mathf.Clamp01(dt * 3f));
                _body.SetLook(_braceTarget.ChestPosition);
            }
            else if (_braceBoss != null && Game.UnderBosses != null && Game.UnderBosses.AssistTargetAlive(_braceBoss, out bossAt))
            {
                // A boss moves around its arena: the shell keeps turning to face it.
                Vector3 to = MathUtil.Flat(bossAt - pl.transform.position);
                if (to.sqrMagnitude > 0.01f) _braceDir = Vector3.Slerp(_braceDir, to.normalized, Mathf.Clamp01(dt * 3f));
                _body.SetLook(bossAt);
            }
            float far = MathUtil.FlatDistance(pl.transform.position, _body.transform.position);
            if (far > _brace.CoverRange && Time.time > _coverHint)
            {
                _coverHint = Time.time + 4f;
                CompanionRecord r = Active;
                Say("Stay within " + Mathf.RoundToInt(_brace.CoverRange) + " m of " + NameOf(r) + " to keep its cover.", 2.5f, true);
            }
            if (_braceCap <= 0f)
            {
                EndBrace(true);
                return;
            }
            if (Time.time >= _braceUntil) EndBrace(false);
        }

        /// <summary>Ends the brace; broke means the shell's capacity ran out (it cracks, nothing is refunded).</summary>
        private void EndBrace(bool broke)
        {
            if (!_bracing) return;
            _bracing = false;
            _braceTarget = null;
            _braceBoss = null;
            _pendingStrain = 0f;
            if (_body != null)
            {
                if (broke)
                {
                    Underheart04ESounds.Play(UhSfx.PlateCrack, _body.ChestPosition, 1f, 1f);
                    if (Game.Fx != null) Game.Fx.RockBurst(_body.ChestPosition, new Color(0.55f, 0.5f, 0.45f), 12, 0.5f);
                    CompanionRecord r = Active;
                    Say(NameOf(r) + "'s brace gave way.", 2.5f, true);
                }
                if (_body.Mode != CompanionMode.Downed) _body.ClearAction();
            }
            if (_ak == AssistKind.Brace) EndAssist();
        }

        /// <summary>
        /// The brace in the player's damage path: hits from inside the cone lose part of their force to the shell. The
        /// player's own hits (a grenade at their feet) and friendly companion or fused-vehicle damage pass untouched and
        /// never drain the shell, like the ward's Friendly check.
        /// </summary>
        private bool BraceHook(ref DamageInfo info)
        {
            if (!_bracing || _body == null) return true;
            if (info.FromPlayer || info.Origin != DamageOrigin.Player) return true;
            PlayerController pl = Game.Player;
            if (pl == null) return true;
            float near = MathUtil.FlatDistance(pl.transform.position, _body.transform.position);
            bool hasSource = info.Source != null;
            float shade = BondAssistRules.HeatShade(info.Type, hasSource, info.Amount, _brace, near);
            if (shade > 0f)
            {
                info.Amount = Mathf.Max(0f, info.Amount - shade);
                return true;
            }
            bool braceable = BondAssistRules.Braceable(info.Type, (info.Flags & DamageFlags.Unblockable) != 0, hasSource);
            float took = BondAssistRules.Absorb(_braceDir, info.Direction, info.Amount, _brace, ref _braceCap, braceable, near);
            if (took <= 0f) return true;
            info.Amount = Mathf.Max(0f, info.Amount - took);
            _pendingStrain += took * _brace.Strain;
            if (Time.time > _braceCue)
            {
                _braceCue = Time.time + 0.25f;
                Vector3 at = _body.ChestPosition + _braceDir * 0.5f;
                if (Game.Fx != null) Game.Fx.Sparks(at, _braceDir * 2.5f, 6);
                Underheart04ESounds.Play(UhSfx.GuardianBrace, at, 0.5f, 1.15f);
            }
            return true;
        }

        /// <summary>The brace's remaining capacity (0 to 1) for the HUD, or -1 when not bracing.</summary>
        public float BraceCapacity01 { get { return _bracing && _braceCapMax > 0f ? Mathf.Clamp01(_braceCap / _braceCapMax) : -1f; } }

        // ================================================================== exploration assists

        private void StartExplore(CompanionRecord r)
        {
            CompanionProfile prof = ProfileOf(r);
            SetCooldown(r.Uid, false, BondRules.ExploreCooldown(r.Archetype, r.Specialization));
            PlayerController pl = Game.Player;
            Vector3 pp = pl.transform.position;
            switch (prof.Explore)
            {
                case ExploreAssistKind.BraceAndSense:
                {
                    Vector3 mech;
                    if (NearbyQuestPoint(pp, 12f, MechanismAnchors, out mech))
                    {
                        BeginAssist(r, AssistKind.BraceMechanism);
                        _point = mech;
                        Vector3 back = MathUtil.Flat(pp - mech);
                        Vector3 stand = mech + (back.sqrMagnitude > 0.01f ? back.normalized : Vector3.back) * 1.2f;
                        _body.SetGoal(stand, prof.RunSpeed, false);
                    }
                    else
                    {
                        BeginAssist(r, AssistKind.HeatSense);
                        _body.PlayAction(CreatureAction.Howl, 1.2f, 0.4f, 0.7f, OnHeatSense);
                    }
                    break;
                }
                case ExploreAssistKind.RelayHop:
                {
                    Vector3 relay;
                    if (NearbyQuestPoint(pp, 22f, RelayAnchors, out relay))
                    {
                        BeginAssist(r, AssistKind.RelayHop);
                        _point = relay;
                        _body.SetGoal(relay + Vector3.up * 1.4f, prof.FlySpeed, true);
                    }
                    else
                    {
                        BeginAssist(r, AssistKind.Scout);
                        float ceiling = float.PositiveInfinity;
                        RaycastHit hit;
                        if (Physics.Raycast(pp + Vector3.up * 1f, Vector3.up, out hit, 8f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) ceiling = hit.point.y;
                        float y = Mathf.Min(pp.y + 4.5f, ceiling - prof.BodyHeight - 0.4f);
                        _body.SetGoal(new Vector3(pp.x, Mathf.Max(pp.y + 1.2f, y), pp.z) + pl.transform.forward * 1.5f, prof.FlySpeed, true);
                    }
                    break;
                }
                default:
                    BeginAssist(r, AssistKind.Trail);
                    _body.PlayAction(CreatureAction.Howl, 1.1f, 0.35f, 0.7f, OnTrailReveal);
                    _body.Glow(1.2f);
                    break;
            }
        }

        private void RaiseExplore(Vector3 point)
        {
            _lastExploreTime = Time.time;
            _lastExplorePoint = point;
            RaiseExplorationAssist(point);
        }

        private void OnTrailReveal()
        {
            if (_ak == AssistKind.None) return;
            CompanionRecord r = Find(_assistUid);
            if (r == null || _body == null) return;
            CompanionProfile prof = ProfileOf(r);
            Vector3 from = Game.Player != null ? Game.Player.transform.position : _body.transform.position;
            string label;
            bool found = BuildRoute(from, BondRules.RevealLength(r.Archetype, r.Specialization), out label);
            Underheart04ESounds.Play(UhSfx.AssistReveal, _body.ChestPosition, 0.9f, 1f);
            RaiseExplore(_body.transform.position);
            if (found)
            {
                CompanionTrailFx.Show(_route, prof.Accent, 15f, true);
                Say(NameOf(r) + " lights the way to " + label + ".", 3.5f, false);
                _stage = 1;
                _at = 0f;
                Vector3 ahead = _route[Mathf.Min(4, _route.Count - 1)];
                _body.SetGoal(ahead, prof.RunSpeed, false);
            }
            else Say(NameOf(r) + " sniffs the air: no hidden trail here. Track a lead and it will light the way.", 3.5f, false);
        }

        private void OnHeatSense()
        {
            if (_ak == AssistKind.None) return;
            CompanionRecord r = Find(_assistUid);
            if (r == null || _body == null) return;
            PlayerController pl = Game.Player;
            Vector3 c = pl != null ? pl.transform.position : _body.transform.position;
            int found = 0;
            if (Game.Underheart != null && WorldBounds.IsUnderground(c))
            {
                for (float x = -16f; x <= 16f && found < 12; x += 2.4f)
                    for (float z = -16f; z <= 16f && found < 12; z += 2.4f)
                    {
                        if (x * x + z * z > 16f * 16f) continue;
                        Vector3 p = c + new Vector3(x, 0.3f, z);
                        float heat;
                        if (!Game.Underheart.HazardAt(p, out heat) || heat <= 0f) continue;
                        GroundRing.Show(new Vector3(p.x, c.y, p.z), 1.1f, 6f, new Color(1f, 0.35f, 0.1f));
                        found++;
                    }
            }
            Underheart04ESounds.Play(UhSfx.AssistReveal, _body.ChestPosition, 0.8f, 0.8f);
            RaiseExplore(_body.transform.position);
            Say(found > 0 ? NameOf(r) + " senses dangerous heat: " + found + " hot spot" + (found == 1 ? "" : "s") + " ringed on the ground." : NameOf(r) + " senses no dangerous heat nearby.", 3.5f, false);
        }

        private void OnRelayStrike()
        {
            if (_ak == AssistKind.None) return;
            CompanionRecord r = Find(_assistUid);
            if (r == null || _body == null) return;
            CompanionProfile prof = ProfileOf(r);
            Arc(_body.ChestPosition, _point + Vector3.up * 0.6f, prof.Accent);
            Underheart04ESounds.Play(UhSfx.SkimmerArc, _point, 0.9f, 1.1f);
            RaiseExplore(_point);
            Vector3 from = Game.Player != null ? Game.Player.transform.position : _body.transform.position;
            _route.Clear();
            StraightRoute(from, _point);
            if (_route.Count >= 2) CompanionTrailFx.Show(_route, prof.Accent, 12f, true);
            Say(NameOf(r) + " bridges it with an arc.", 3f, false);
        }

        private void OnScoutReveal()
        {
            if (_ak == AssistKind.None) return;
            CompanionRecord r = Find(_assistUid);
            if (r == null || _body == null) return;
            CompanionProfile prof = ProfileOf(r);
            Vector3 from = Game.Player != null ? Game.Player.transform.position : _body.transform.position;
            string label;
            bool found = BuildRoute(from, BondRules.RevealLength(r.Archetype, r.Specialization), out label);
            Underheart04ESounds.Play(UhSfx.AssistReveal, _body.ChestPosition, 0.9f, 1.2f);
            RaiseExplore(_body.transform.position);
            if (found)
            {
                CompanionTrailFx.Show(_route, prof.Accent, 15f, true);
                Say(NameOf(r) + " scouts from above and lights the way to " + label + ".", 3.5f, false);
            }
            else Say(NameOf(r) + " scouts from above: nothing to follow here. Track a lead first.", 3.5f, false);
        }

        // ================================================================== routes and places

        /// <summary>The quest target when it is close, or the nearest listed Underheart anchor within range on this floor.</summary>
        private static bool NearbyQuestPoint(Vector3 from, float range, string[] anchors, out Vector3 point)
        {
            point = Vector3.zero;
            if (Game.Gps != null && Game.Gps.HasMissionTarget)
            {
                Vector3 t = Game.Gps.MissionTarget;
                if ((t - from).magnitude <= range && WorldBounds.SameLayer(t, from))
                {
                    point = t;
                    return true;
                }
            }
            string id;
            return NearestAnchor(anchors, from, range, out point, out id);
        }

        private static bool NearestAnchor(string[] ids, Vector3 from, float range, out Vector3 point, out string id)
        {
            point = Vector3.zero;
            id = "";
            if (Game.Underheart == null || !Game.Underheart.Ready || ids == null) return false;
            float best = range;
            for (int i = 0; i < ids.Length; i++)
            {
                Vector3 p;
                float yaw;
                if (!Game.Underheart.TryAnchor(ids[i], out p, out yaw)) continue;
                float d = (p - from).magnitude;
                if (d > best || !WorldBounds.SameLayer(p, from)) continue;
                best = d;
                point = p;
                id = ids[i];
            }
            return id.Length > 0;
        }

        /// <summary>
        /// The route to reveal: a hidden cool route nearby first, otherwise the tracked lead. Underground (or toward an
        /// underground target) it asks the Underheart for its walkable route; on the street it follows the GPS route; as a
        /// last resort a straight line that stops at the first wall. Clipped to length metres.
        /// </summary>
        private bool BuildRoute(Vector3 from, float length, out string label)
        {
            _route.Clear();
            label = "";
            Vector3 target;
            string anchor;
            bool hidden = NearestAnchor(RevealAnchors, from, 35f, out target, out anchor);
            if (hidden) label = "a hidden way";
            else
            {
                if (Game.Gps == null || !Game.Gps.HasTarget) return false;
                target = Game.Gps.ActiveTarget;
                label = "your lead";
            }
            bool under = WorldBounds.IsUnderground(from) || WorldBounds.IsUnderground(target) || target.y < -3f;
            if (under && Game.Underheart != null && Game.Underheart.Ready)
            {
                _scratch.Clear();
                string floor;
                bool ok = false;
                try { ok = Game.Underheart.TryRoute(from, target, _scratch, out floor); }
                catch (System.Exception e) { Log.Exception("Bonds route", e); floor = ""; }
                if (ok && _scratch.Count > 0)
                {
                    _route.Add(from);
                    _route.AddRange(_scratch);
                    if (!string.IsNullOrEmpty(floor)) label += " (" + floor + ")";
                }
            }
            if (_route.Count < 2 && !hidden && !under && Game.Gps != null && Game.Gps.Route.Count > 1)
            {
                List<Vector3> gr = Game.Gps.Route;
                int nearest = 0;
                float bd = float.MaxValue;
                for (int i = 0; i < gr.Count; i++)
                {
                    float d = MathUtil.FlatSqrDistance(gr[i], from);
                    if (d < bd) { bd = d; nearest = i; }
                }
                _route.Add(from);
                for (int i = nearest; i < gr.Count; i++) _route.Add(gr[i]);
            }
            if (_route.Count < 2) StraightRoute(from, target);
            ClipRoute(length);
            return _route.Count >= 2;
        }

        /// <summary>A straight line on the floor from a toward b, sampled every 1.4 m, stopping at the first wall or drop.</summary>
        private void StraightRoute(Vector3 a, Vector3 b)
        {
            _route.Clear();
            _route.Add(a);
            Vector3 flat = MathUtil.Flat(b - a);
            float len = Mathf.Min(flat.magnitude, 60f);
            if (len < 0.5f) return;
            Vector3 dir = flat / flat.magnitude;
            Vector3 prev = a;
            for (float s = 1.4f; s <= len; s += 1.4f)
            {
                Vector3 p = a + dir * s;
                RaycastHit hit;
                if (!Physics.Raycast(new Vector3(p.x, prev.y + 1.5f, p.z), Vector3.down, out hit, 4f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) break;
                p.y = hit.point.y;
                if (Physics.Linecast(prev + Vector3.up * 0.5f, p + Vector3.up * 0.5f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) break;
                _route.Add(p);
                prev = p;
            }
        }

        private void ClipRoute(float length)
        {
            float acc = 0f;
            for (int i = 1; i < _route.Count; i++)
            {
                float seg = (_route[i] - _route[i - 1]).magnitude;
                if (acc + seg <= length)
                {
                    acc += seg;
                    continue;
                }
                float t = seg > 1e-4f ? (length - acc) / seg : 0f;
                _route[i] = Vector3.Lerp(_route[i - 1], _route[i], t);
                _route.RemoveRange(i + 1, _route.Count - i - 1);
                return;
            }
        }
    }
}
