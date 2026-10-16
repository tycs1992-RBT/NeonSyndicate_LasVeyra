using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The four role patterns on top of the shared perception and leash: the frontline warden (advances behind a
    /// frontal guard, commits to one telegraphed slam, then stands exposed with its core marked), the ranged refractor
    /// (holds a clear lane, telegraphs a shot whose aim locks before release, relocates under pressure), the support
    /// resonator (one visible, breakable damage-sharing link to an ally; killing it ends the link) and the skirmisher
    /// (flanks along legal open ground, pauses visibly before lunging, gives up impossible routes). Ground bodies never
    /// climb after a flying or rooftop player: they hold a ring below, and the ranged ones throw visible shards.
    /// </summary>
    public sealed partial class EncounterRole04D
    {
        private enum SkirmishPhase
        {
            Choose,
            Flank,
            Pause,
            Strike,
            Recover
        }

        private static readonly float[] LaneAngles = { 50f, -50f, 80f, -80f, 30f, -30f, 110f, -110f };
        private static readonly Color AimColor = new Color(0.75f, 0.95f, 1f, 1f);
        private static readonly Color AimLockedColor = new Color(1f, 0.6f, 0.35f, 1f);
        private static readonly Color LinkColor = new Color(0.35f, 1f, 0.85f, 1f);
        private static readonly Color HintColor = new Color(0.6f, 0.95f, 1f);
        private const float LinkRange = 18f;
        private const float PressureLimit = 0.16f;
        private const float PressureHalving = 2.5f;
        private const float RefractorShotSpeed = 30f;
        private const float AntiAirShotSpeed = 24f;
        private static Material _aimMat;
        private static Material _aimLockedMat;
        private static Material _linkMat;
        private static float _hintAt = -99f;

        // The warden.
        private bool _slamming;
        private float _recoverUntil;
        private float _nextSlamAt;
        // Shots (the refractor, and the warden's anti-air throw).
        private bool _charging;
        private bool _antiAir;
        private bool _aimLocked;
        private float _chargeStart;
        private float _chargeDur = 1f;
        private Vector3 _aimDir = Vector3.forward;
        private float _nextShotAt;
        private bool _hasShotToken;
        private float _throwFollowAt = -1f;
        private LineRenderer _aim;
        // The refractor.
        private bool _relocating;
        private Vector3 _relocateTo;
        private float _relocateUntil;
        private float _nextRelocateAt;
        private float _pressure;
        private float _pressureAt;
        // The resonator.
        private NpcController _linkAlly;
        private bool _linked;
        private float _allySaved = 1f;
        private float _linkCheckAt;
        private float _linkCooldownUntil;
        private LineRenderer _link;
        private bool _resMoving;
        private Vector3 _resGoal;
        // The skirmisher.
        private SkirmishPhase _sk;
        private Vector3 _flankTo;
        private float _flankStart;
        private float _flankBest;
        private float _flankProgressAt;
        private float _pauseUntil;
        private float _strikeUntil;
        private float _skRecoverUntil;
        private bool _sawSwing;
        private float _nextFlankAt;
        private int _flankSide;
        private bool _tellOn;

        /// <summary>The resonator's link is holding right now.</summary>
        public bool Linked { get { return _linked; } }
        /// <summary>The ally the resonator's link shields (null when no link holds).</summary>
        public NpcController LinkAlly { get { return _linked ? _linkAlly : null; } }

        private void InitPattern()
        {
            float now = Time.time;
            _nextSlamAt = now + 2f;
            _nextShotAt = now + 1.5f;
            _nextFlankAt = now;
            _flankSide = RunSerial % 2;
            _sk = SkirmishPhase.Choose;
        }

        // ================================================================== intent hooks

        private void EnterEngage(float now)
        {
            switch (Kind)
            {
                case EncounterRoleKind04D.Warden:
                    _nextSlamAt = Mathf.Max(_nextSlamAt, now + 1.2f);
                    Detail = "advances behind its guard";
                    break;
                case EncounterRoleKind04D.Refractor:
                    _nextShotAt = Mathf.Max(_nextShotAt, now + 0.8f);
                    _relocating = false;
                    Detail = "holds a firing lane";
                    break;
                case EncounterRoleKind04D.Resonator:
                    _resMoving = false;
                    Detail = "keeps back to support";
                    break;
                default:
                    _sk = SkirmishPhase.Choose;
                    Detail = "looks for a flank";
                    break;
            }
        }

        private void ExitEngage(float now)
        {
            CancelShot(now);
            CreatureAbilityKit kit = Npc != null ? Npc.CreatureKit : null;
            if (_slamming && kit != null) kit.Cancel();
            _slamming = false;
            _relocating = false;
            _resMoving = false;
            BreakLink(false);
            SetTell(false);
            ReleaseTokens();
            if (Npc != null && now >= _recoverUntil) Npc.CreatureBusyUntil = Mathf.Min(Npc.CreatureBusyUntil, now);
        }

        private void ApplyEngage(PlayerController p, float dist, float now)
        {
            switch (Kind)
            {
                case EncounterRoleKind04D.Warden: EngageWarden(p, dist, now); break;
                case EncounterRoleKind04D.Refractor: EngageRefractor(p, dist, now); break;
                case EncounterRoleKind04D.Resonator: EngageResonator(p, dist, now); break;
                default: EngageSkirmisher(p, dist, now); break;
            }
        }

        /// <summary>After a hit, puts the creature back into the pattern's movement without starting anything new.</summary>
        private void ReassertEngage(PlayerController p, float now)
        {
            switch (Kind)
            {
                case EncounterRoleKind04D.Refractor:
                    if (_relocating) { if (Npc.State != NpcState.Scripted) Npc.MoveTo(_relocateTo, true, null); }
                    else HoldFacing(p, now);
                    break;
                case EncounterRoleKind04D.Resonator:
                    if (_resMoving) { if (Npc.State != NpcState.Scripted) Npc.MoveTo(_resGoal, true, null); }
                    else HoldFacing(p, now);
                    break;
                case EncounterRoleKind04D.Skirmisher:
                    if (_sk == SkirmishPhase.Flank) { if (Npc.State != NpcState.Scripted) Npc.MoveTo(_flankTo, true, null); }
                    else if (_sk == SkirmishPhase.Pause || _sk == SkirmishPhase.Recover) HoldFacing(p, now);
                    break;
            }
        }

        private void OnHurtPattern(DamageInfo info)
        {
            if (!info.FromPlayer || Npc == null || Npc.Health == null) return;
            float now = Time.time;
            float frac = Npc.Health.MaxHealth > 0f ? Npc.Health.LastApplied / Npc.Health.MaxHealth : 0f;
            if (Kind == EncounterRoleKind04D.Refractor)
            {
                _pressure = EncounterRules04D.DecayPressure(_pressure, now - _pressureAt, PressureHalving) + frac;
                _pressureAt = now;
            }
            else if (Kind == EncounterRoleKind04D.Resonator && _linked && frac >= 0.12f)
            {
                // A solid hit interrupts the source: the link drops for a few seconds.
                BreakLink(true);
                Hint("Link interrupted");
            }
        }

        /// <summary>Stops every owned threat of this body: wind-ups, the aim line, the link, the telegraph and its tokens.</summary>
        public void StopPatterns()
        {
            float now = Time.time;
            CancelShot(now);
            BreakLink(false);
            SetTell(false);
            if (_aim != null) _aim.enabled = false;
            _relocating = false;
            _resMoving = false;
            if (Npc == null) return;
            CreatureAbilityKit kit = Npc.CreatureKit;
            if (kit != null && !Npc.IsDead) kit.Cancel();
            _slamming = false;
            if (!Npc.IsDead) Npc.CreatureCancelAttack();
            ReleaseTokens();
            if (Npc.Anim != null)
            {
                Npc.Anim.Telegraph = 0f;
                if (_throwFollowAt >= 0f) Npc.Anim.ThrowT = -1f;
            }
            _throwFollowAt = -1f;
        }

        // ================================================================== shared moves

        /// <summary>Stand and face the player inside the creature's fight state (busy keeps its own melee from closing in).</summary>
        private void HoldFacing(PlayerController p, float now)
        {
            if (Npc.State != NpcState.Fight || Npc.Target != p) Npc.Attack(p);
            Npc.CreatureBusyUntil = Mathf.Max(Npc.CreatureBusyUntil, now + 0.5f);
        }

        /// <summary>Lets the creature's own melee run again (unless an ability, a recovery or a charge holds it).</summary>
        private void ReleaseHold(float now)
        {
            CreatureAbilityKit kit = Npc.CreatureKit;
            if ((kit == null || !kit.Busy) && now >= _recoverUntil && !_charging) Npc.CreatureBusyUntil = Mathf.Min(Npc.CreatureBusyUntil, now);
        }

        /// <summary>The creature's own melee fight (approach, attack tokens, telegraphed swings).</summary>
        private void Fight(PlayerController p, float now)
        {
            if (Npc.State != NpcState.Fight || Npc.Target != p) Npc.Attack(p);
            ReleaseHold(now);
        }

        /// <summary>Holds a ring below an unreachable player instead of walking up invisible stairs.</summary>
        private void StandOff(PlayerController p, float radius, float now)
        {
            Vector3 pp = p.transform.position;
            Vector3 dir = MathUtil.Flat(Npc.transform.position - pp);
            if (dir.sqrMagnitude < 0.25f) dir = MathUtil.Flat(Anchor - pp);
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            Vector3 want = ClampToLeash(pp + dir.normalized * radius, 10f);
            if (MathUtil.FlatDistance(Npc.transform.position, want) > 2.5f) Npc.MoveTo(want, true, null);
            else HoldFacing(p, now);
        }

        private Vector3 ClampToLeash(Vector3 want, float margin)
        {
            Vector3 off = MathUtil.Flat(want - Origin);
            float max = EncounterRules04D.HardLeash - margin;
            if (off.magnitude <= max) return want;
            Vector3 c = Origin + off.normalized * max;
            c.y = want.y;
            return c;
        }

        private static bool GroundNear(Vector3 q, float refY, float maxStep, out Vector3 g)
        {
            g = q;
            RaycastHit hit;
            if (!Physics.Raycast(new Vector3(q.x, refY + 4f, q.z), Vector3.down, out hit, 10f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return false;
            if (hit.normal.y < 0.7f || Mathf.Abs(hit.point.y - refY) > maxStep) return false;
            g = hit.point;
            return true;
        }

        private void Hint(string text)
        {
            float now = Time.unscaledTime;
            if (now - _hintAt < 6f || Game.Hud == null || Npc == null) return;
            if (MathUtil.FlatDistance(Npc.transform.position, Game.PlayerPosition) > 45f) return;
            _hintAt = now;
            Game.Hud.CombatText(text, HintColor);
        }

        // ================================================================== the warden

        private void EngageWarden(PlayerController p, float dist, float now)
        {
            CreatureAbilityKit kit = Npc.CreatureKit;
            if (_slamming || (kit != null && kit.Busy))
            {
                Detail = "commits to its slam";
                return;
            }
            if (now < _recoverUntil)
            {
                Detail = "recovering: its core is exposed";
                return;
            }
            if (!TargetReachable)
            {
                if (!_charging) StandOff(p, 9f, now);
                if (Sees && dist < 34f) TryStartShot(p, now, true);
                Detail = "cannot reach you: holds below and throws";
                return;
            }
            if (_charging)
            {
                HoldFacing(p, now);
                return;
            }
            Fight(p, now);
            Detail = "advances behind its guard";
            float s = Npc.Rig != null ? Mathf.Max(0.5f, Npc.Rig.Scale) : 1f;
            float edge = dist - p.BodyRadius;
            if (kit == null || Npc.CreatureAttacking || Npc.IsStaggered || now < _nextSlamAt || edge > 3.4f * s) return;
            if (!CombatDirector.TryMeleeToken(Npc)) return;
            if (kit.StartSlam(1f, EncounterRules04D.WardenSlamWindup(_difficulty), 1.4f))
            {
                _slamming = true;
                Detail = "commits to its slam";
            }
        }

        /// <summary>When the slam ends (or a stagger breaks it) the warden stands exposed: guard down, core marked.</summary>
        private void UpdateWarden(float now)
        {
            if (!_slamming) return;
            CreatureAbilityKit kit = Npc.CreatureKit;
            if (kit != null && kit.Busy) return;
            _slamming = false;
            float rec = EncounterRules04D.WardenRecovery(_difficulty);
            _recoverUntil = now + rec;
            Npc.CreatureBusyUntil = Mathf.Max(Npc.CreatureBusyUntil, _recoverUntil);
            Npc.CreatureHighlightUntil = Mathf.Max(Npc.CreatureHighlightUntil, _recoverUntil);
            if (kit != null && kit.HasGuard) kit.GuardBrokenUntil = Mathf.Max(kit.GuardBrokenUntil, _recoverUntil);
            _nextSlamAt = _recoverUntil + Random.Range(4.5f, 6.5f);
            CombatDirector.ReleaseMeleeToken(Npc);
            Hint("Warden recovering: hit its glowing core");
        }

        // ================================================================== shots

        private bool TryStartShot(PlayerController p, float now, bool antiAir)
        {
            if (_charging || now < _nextShotAt || Npc.IsStaggered || Npc.CreatureAttacking) return false;
            CreatureAbilityKit kit = Npc.CreatureKit;
            if (kit != null && kit.Busy) return false;
            HoldFacing(p, now);
            if (!CombatDirector.TryShotToken(Npc)) return false;
            _hasShotToken = true;
            _charging = true;
            _antiAir = antiAir;
            _aimLocked = false;
            _chargeStart = now;
            _chargeDur = Mathf.Max(0.7f, EncounterRules04D.RefractorCharge(_difficulty) + (antiAir ? 0.4f : 0f));
            _aimDir = AimDirection(p);
            Npc.CreatureBusyUntil = Mathf.Max(Npc.CreatureBusyUntil, now + _chargeDur + 0.6f);
            if (Game.Audio != null) CreatureAudio.Play(Npc.CreatureSpecies, CreatureCry.Attack, Npc.ChestPosition, Npc.BodyHeight);
            return true;
        }

        /// <summary>The visible charge: telegraph glow, a wind-up pose and an aim line that stops following at the lock point.</summary>
        private void UpdateShot(float now)
        {
            if (!_charging) return;
            PlayerController p = Game.Player;
            if (p == null || p.IsDead || Npc.IsStaggered || Intent != ActorIntent04D.Engage)
            {
                CancelShot(now);
                return;
            }
            float k = Mathf.Clamp01((now - _chargeStart) / Mathf.Max(0.05f, _chargeDur));
            if (Npc.Anim != null)
            {
                Npc.Anim.Telegraph = k;
                Npc.Anim.ThrowT = Mathf.Lerp(0f, 0.68f, k);
            }
            if (!_aimLocked)
            {
                _aimDir = AimDirection(p);
                if (k >= EncounterRules04D.AimLockFraction) _aimLocked = true;
            }
            DrawAimLine(_aimDir, _aimLocked, k);
            if (k >= 1f) FireShot(now);
        }

        private void FireShot(float now)
        {
            Vector3 from = Muzzle();
            CreatureSpecies sp = Npc.CreatureSpecies;
            float scale = Npc.CreatureOptions != null ? Npc.CreatureOptions.DamageScale : 1f;
            float raw = (sp != null ? sp.Damage : 15f) * 0.7f * scale;
            PlayerController p = Game.Player;
            if (p != null && p.Health != null) raw = CreatureBrainLogic.CapHit(raw, p.Health.MaxHealth, (int)Settings.Difficulty, CombatDirector.PlayerDamageTaken);
            bool fired = EncounterShard04D.Fire(from, _aimDir, _antiAir ? AntiAirShotSpeed : RefractorShotSpeed, raw, Npc.gameObject, RunSerial);
            if (fired && Game.Audio != null) Game.Audio.PlayAt(SfxId.Throw, from, 0.75f, 1.35f, 50f);
            _charging = false;
            if (_aim != null) _aim.enabled = false;
            if (Npc.Anim != null) Npc.Anim.Telegraph = 0f;
            _throwFollowAt = now;
            _nextShotAt = now + EncounterRules04D.RefractorCooldown(_difficulty) + Random.Range(-0.4f, 0.6f);
            if (_hasShotToken) CombatDirector.ReleaseShotToken(Npc);
            _hasShotToken = false;
            // A short readable recovery after the release.
            Npc.CreatureBusyUntil = Mathf.Max(Npc.CreatureBusyUntil, now + 0.6f);
        }

        private void CancelShot(float now)
        {
            if (!_charging) return;
            _charging = false;
            if (_aim != null) _aim.enabled = false;
            if (Npc != null)
            {
                if (Npc.Anim != null)
                {
                    Npc.Anim.Telegraph = 0f;
                    Npc.Anim.ThrowT = -1f;
                }
                if (_hasShotToken) CombatDirector.ReleaseShotToken(Npc);
            }
            _hasShotToken = false;
            _nextShotAt = Mathf.Max(_nextShotAt, now + 1.5f);
        }

        private void UpdateThrowFollow(float now)
        {
            if (_throwFollowAt < 0f || Npc.Anim == null) return;
            float k = (now - _throwFollowAt) / 0.4f;
            if (k >= 1f)
            {
                Npc.Anim.ThrowT = -1f;
                _throwFollowAt = -1f;
            }
            else Npc.Anim.ThrowT = Mathf.Lerp(0.7f, 1f, k);
        }

        private Vector3 Muzzle()
        {
            if (Npc.Rig != null && Npc.Rig.HandR != null) return Npc.Rig.HandR.position;
            return Npc.ChestPosition + Npc.transform.forward * 0.4f;
        }

        private Vector3 AimDirection(PlayerController p)
        {
            Vector3 d = p.ChestPosition - Muzzle();
            return d.sqrMagnitude > 0.01f ? d.normalized : Npc.transform.forward;
        }

        private void DrawAimLine(Vector3 dir, bool locked, float k)
        {
            if (_aim == null) _aim = MakeLine(transform, "Refractor Aim Line", 0.03f);
            Vector3 from = Muzzle();
            Vector3 end = from + dir * 45f;
            RaycastHit hit;
            if (Physics.Raycast(from, dir, out hit, 45f, Layers.WorldMask | (1 << Layers.Vehicles), QueryTriggerInteraction.Ignore)) end = hit.point;
            _aim.sharedMaterial = locked ? AimMaterial(true) : AimMaterial(false);
            _aim.SetPosition(0, from);
            _aim.SetPosition(1, end);
            _aim.widthMultiplier = locked ? 0.05f + 0.012f * Mathf.Abs(Mathf.Sin(Time.time * 12f)) : 0.016f + 0.014f * k;
            _aim.enabled = true;
        }

        private static LineRenderer MakeLine(Transform parent, string name, float width)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            LineRenderer l = go.AddComponent<LineRenderer>();
            l.positionCount = 2;
            l.useWorldSpace = true;
            l.widthMultiplier = width;
            l.numCapVertices = 0;
            l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            l.receiveShadows = false;
            l.enabled = false;
            return l;
        }

        private static Material AimMaterial(bool locked)
        {
            if (locked)
            {
                if (_aimLockedMat == null) _aimLockedMat = Mats.Glow(AimLockedColor);
                return _aimLockedMat;
            }
            if (_aimMat == null) _aimMat = Mats.Glow(AimColor);
            return _aimMat;
        }

        // ================================================================== the refractor

        private void EngageRefractor(PlayerController p, float dist, float now)
        {
            if (_relocating)
            {
                bool arrived = MathUtil.FlatDistance(Npc.transform.position, _relocateTo) < EncounterRules04D.GoalTolerance;
                if (!arrived && now < _relocateUntil)
                {
                    if (Npc.State != NpcState.Scripted) Npc.MoveTo(_relocateTo, true, null);
                    return;
                }
                _relocating = false;
                _nextRelocateAt = now + 3f;
            }
            float pressure = EncounterRules04D.DecayPressure(_pressure, now - _pressureAt, PressureHalving);
            bool laneLost = !Sees && now - _lastSeenAt > 1.5f;
            bool tooClose = dist < 8f;
            bool tooFar = dist > 30f;
            bool pressured = pressure >= PressureLimit;
            if (!_charging && now >= _nextRelocateAt && (pressured || laneLost || tooClose || tooFar))
            {
                Vector3 lane;
                if (PickLane(p, out lane))
                {
                    BeginRelocate(lane, now, pressured ? "relocates under pressure" : (laneLost ? "looks for a clear lane" : "resets its range"));
                    return;
                }
                _nextRelocateAt = now + 2f;
            }
            HoldFacing(p, now);
            if (!_charging && Sees && dist <= 32f) TryStartShot(p, now, false);
            Detail = _charging ? (_aimLocked ? "lane locked: about to fire" : "charges a refracted shot") : "holds a firing lane";
        }

        private void BeginRelocate(Vector3 lane, float now, string why)
        {
            CancelShot(now);
            ReleaseTokens();
            Npc.CreatureBusyUntil = Mathf.Min(Npc.CreatureBusyUntil, now);
            Npc.MoveTo(lane, true, null);
            _relocating = true;
            _relocateTo = lane;
            _relocateUntil = now + 5f;
            _pressure = 0f;
            _pressureAt = now;
            Detail = why;
        }

        /// <summary>A legal lane: open ground at range from the player, with a clear line to them, inside the leash.</summary>
        private bool PickLane(PlayerController p, out Vector3 lane)
        {
            lane = Vector3.zero;
            Vector3 pp = p.transform.position;
            Vector3 from = MathUtil.Flat(Npc.transform.position - pp);
            if (from.sqrMagnitude < 0.01f) from = MathUtil.Flat(Anchor - pp);
            if (from.sqrMagnitude < 0.01f) from = Vector3.forward;
            from.Normalize();
            float r = _difficulty == DifficultyLevel.Story ? 20f : 18f;
            float refY = Npc.transform.position.y;
            int blockMask = Layers.WorldMask | (1 << Layers.Vehicles);
            for (int i = 0; i < LaneAngles.Length; i++)
            {
                Vector3 q = pp + (QMath.AY(LaneAngles[i]) * from) * r;
                float move = MathUtil.FlatDistance(q, Npc.transform.position);
                if (move < 4f || move > 18f) continue;
                if (MathUtil.FlatDistance(q, Origin) > EncounterRules04D.HardLeash - 15f) continue;
                Vector3 g;
                if (!GroundNear(q, refY, 2.5f, out g)) continue;
                if (Game.Layout != null && Game.Layout.IsInsideBuilding(g, 1f)) continue;
                if (Physics.CheckSphere(g + Vector3.up * 1.1f, 0.45f, blockMask, QueryTriggerInteraction.Ignore)) continue;
                if (Physics.Linecast(g + Vector3.up * 1.6f, p.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                lane = g;
                return true;
            }
            return false;
        }

        // ================================================================== the resonator

        private void EngageResonator(PlayerController p, float dist, float now)
        {
            NpcController ally = PickAlly();
            if (ally == null)
            {
                _resMoving = false;
                Fight(p, now);
                Detail = "has nobody to support and fights";
                return;
            }
            if (dist < 4.5f)
            {
                _resMoving = false;
                Fight(p, now);
                Detail = _linked ? "fends you off, link held" : "fends you off";
                return;
            }
            Vector3 ap = ally.transform.position;
            Vector3 back = MathUtil.Flat(ap - p.transform.position);
            if (back.sqrMagnitude < 0.01f) back = MathUtil.Flat(Npc.transform.position - ap);
            back = back.sqrMagnitude > 0.01f ? back.normalized : Vector3.forward;
            Vector3 want = ClampToLeash(ap + back * 7f, 10f);
            if (MathUtil.FlatDistance(Npc.transform.position, want) > 3.5f)
            {
                if (!_resMoving || Npc.State != NpcState.Scripted || MathUtil.FlatDistance(_resGoal, want) > 2f)
                {
                    _resGoal = want;
                    Npc.MoveTo(want, true, null);
                }
                _resMoving = true;
                Detail = _linked ? "repositions, link held" : "moves to support its ally";
            }
            else
            {
                _resMoving = false;
                HoldFacing(p, now);
                Detail = _linked ? "channels its link" : "readies its link";
            }
        }

        /// <summary>The ally to shield: the current one while it lives, else the warden, else the nearest other opponent.</summary>
        private NpcController PickAlly()
        {
            if (_linkAlly != null && !_linkAlly.IsDead) return _linkAlly;
            NpcController best = null;
            float bestDist = float.MaxValue;
            bool bestWarden = false;
            for (int i = 0; i < All.Count; i++)
            {
                EncounterRole04D o = All[i];
                if (o == null || o == this || o.RunSerial != RunSerial || o.Kind == EncounterRoleKind04D.Resonator || o._withdraw) continue;
                if (o.Npc == null || o.Npc.IsDead || o.Npc.CreatureKit == null) continue;
                bool w = o.Kind == EncounterRoleKind04D.Warden;
                float d = MathUtil.FlatDistance(o.Npc.transform.position, Npc.transform.position);
                if (best == null || (w && !bestWarden) || (w == bestWarden && d < bestDist))
                {
                    best = o.Npc;
                    bestDist = d;
                    bestWarden = w;
                }
            }
            return best;
        }

        /// <summary>The link holds only in range, with a clear line, while the resonator is engaged and steady.</summary>
        private void UpdateLink(float now)
        {
            if (now >= _linkCheckAt)
            {
                _linkCheckAt = now + 0.25f;
                bool want = Intent == ActorIntent04D.Engage && !_withdraw && !Npc.IsStaggered && now >= _linkCooldownUntil;
                NpcController ally = want ? PickAlly() : null;
                bool ok = ally != null && ally.CreatureKit != null && !ally.IsDead
                    && MathUtil.FlatDistance(Npc.transform.position, ally.transform.position) <= LinkRange
                    && !Physics.Linecast(Npc.ChestPosition, ally.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore);
                if (ok && (!_linked || _linkAlly != ally))
                {
                    BreakLink(false);
                    MakeLink(ally);
                }
                else if (!ok && _linked)
                {
                    bool allyGone = _linkAlly == null || _linkAlly.IsDead;
                    BreakLink(!allyGone);
                    if (!allyGone) Hint(Npc.IsStaggered ? "Link interrupted" : "Link broken");
                }
            }
            if (_linked) DrawLink(now);
        }

        private void MakeLink(NpcController ally)
        {
            if (ally == null || ally.CreatureKit == null) return;
            _linkAlly = ally;
            _allySaved = ally.CreatureKit.DamageTakenScale;
            ally.CreatureKit.DamageTakenScale = _allySaved * EncounterRules04D.LinkDamageTaken(_difficulty);
            _linked = true;
            Hint("Resonator link: its ally takes less damage. Break the line or take the resonator down");
        }

        /// <summary>Restores the ally's exact damage scale and hides the beam; cooldown keeps it from re-forming at once.</summary>
        private void BreakLink(bool cooldown)
        {
            if (_linked)
            {
                if (_linkAlly != null && _linkAlly.CreatureKit != null) _linkAlly.CreatureKit.DamageTakenScale = _allySaved;
                _linked = false;
                if (cooldown) _linkCooldownUntil = Time.time + 3.5f;
            }
            _linkAlly = null;
            if (_link != null) _link.enabled = false;
        }

        private void DrawLink(float now)
        {
            if (_linkAlly == null) return;
            if (_link == null)
            {
                _link = MakeLine(transform, "Resonator Link", 0.07f);
                if (_linkMat == null) _linkMat = Mats.Glow(LinkColor);
                _link.sharedMaterial = _linkMat;
            }
            _link.SetPosition(0, Npc.ChestPosition);
            _link.SetPosition(1, _linkAlly.ChestPosition);
            _link.widthMultiplier = 0.06f + 0.025f * Mathf.Abs(Mathf.Sin(now * 5f));
            _link.enabled = true;
        }

        // ================================================================== the skirmisher

        private void EngageSkirmisher(PlayerController p, float dist, float now)
        {
            if (!TargetReachable)
            {
                StandOff(p, 10f, now);
                _sk = SkirmishPhase.Choose;
                Detail = "cannot reach you: waits below";
                return;
            }
            switch (_sk)
            {
                case SkirmishPhase.Choose:
                {
                    if (now < _nextFlankAt)
                    {
                        Fight(p, now);
                        Detail = "circles";
                        return;
                    }
                    Vector3 f;
                    if (PickFlank(p, out f))
                    {
                        _flankTo = f;
                        _sk = SkirmishPhase.Flank;
                        _flankStart = now;
                        _flankBest = MathUtil.FlatDistance(Npc.transform.position, f);
                        _flankProgressAt = now;
                        Npc.CreatureBusyUntil = Mathf.Min(Npc.CreatureBusyUntil, now);
                        Npc.MoveTo(f, true, null);
                        Detail = "flanks along open ground";
                    }
                    else
                    {
                        BeginPause(p, now);
                        Detail = "no legal flank: pauses, then closes in";
                    }
                    return;
                }
                case SkirmishPhase.Flank:
                {
                    float d = MathUtil.FlatDistance(Npc.transform.position, _flankTo);
                    if (d < _flankBest - 0.8f)
                    {
                        _flankBest = d;
                        _flankProgressAt = now;
                    }
                    if (d < 2f || dist < 5f)
                    {
                        BeginPause(p, now);
                        return;
                    }
                    if (now - _flankProgressAt > 2f || now - _flankStart > 7f)
                    {
                        // The route turned out impossible (blocked, cut off): give it up visibly instead of grinding into it.
                        _sk = SkirmishPhase.Recover;
                        _skRecoverUntil = now + 1.2f;
                        _nextFlankAt = now + 3f;
                        HoldFacing(p, now);
                        Detail = "gave up an impossible route";
                        return;
                    }
                    if (Npc.State != NpcState.Scripted) Npc.MoveTo(_flankTo, true, null);
                    return;
                }
                case SkirmishPhase.Pause:
                    HoldFacing(p, now);
                    if (now >= _pauseUntil)
                    {
                        _sk = SkirmishPhase.Strike;
                        _strikeUntil = now + 4f;
                        _sawSwing = false;
                        SetTell(false);
                        Fight(p, now);
                        Detail = "lunges";
                    }
                    return;
                case SkirmishPhase.Strike:
                    Fight(p, now);
                    if (Npc.CreatureAttacking) _sawSwing = true;
                    if ((_sawSwing && !Npc.CreatureAttacking) || now >= _strikeUntil)
                    {
                        _sk = SkirmishPhase.Recover;
                        _skRecoverUntil = now + 1.2f;
                        _nextFlankAt = now + Random.Range(2.5f, 4f);
                        Detail = "backs off";
                    }
                    return;
                default:
                    HoldFacing(p, now);
                    if (now >= _skRecoverUntil) _sk = SkirmishPhase.Choose;
                    return;
            }
        }

        private void BeginPause(PlayerController p, float now)
        {
            _sk = SkirmishPhase.Pause;
            _pauseUntil = now + EncounterRules04D.SkirmisherPause(_difficulty);
            HoldFacing(p, now);
            Detail = "pauses before lunging";
        }

        /// <summary>A flank point beside or behind the player on walkable ground with a straight legal route and a line to them.</summary>
        private bool PickFlank(PlayerController p, out Vector3 flank)
        {
            flank = Vector3.zero;
            Vector3 pp = p.transform.position;
            Vector3 look = Game.Cam != null ? Game.Cam.FlatForward : p.transform.forward;
            look.y = 0f;
            if (look.sqrMagnitude < 0.01f) look = Vector3.forward;
            look.Normalize();
            Vector3 right = new Vector3(look.z, 0f, -look.x);
            for (int k = 0; k < 2; k++)
            {
                float side = (_flankSide + k) % 2 == 0 ? 1f : -1f;
                Vector3 q = pp + right * side * 7f - look * 2f;
                Vector3 g;
                if (!FlankLegal(p, q, out g)) continue;
                _flankSide = (_flankSide + k + 1) % 2;
                flank = g;
                return true;
            }
            return false;
        }

        private bool FlankLegal(PlayerController p, Vector3 q, out Vector3 g)
        {
            if (!GroundNear(q, p.transform.position.y, 1.5f, out g)) return false;
            if (MathUtil.FlatDistance(g, Origin) > EncounterRules04D.HardLeash - 10f) return false;
            if (Game.Layout != null && Game.Layout.IsInsideBuilding(g, 0.8f)) return false;
            int blockMask = Layers.WorldMask | (1 << Layers.Vehicles);
            if (Physics.CheckSphere(g + Vector3.up * 1f, 0.4f, blockMask, QueryTriggerInteraction.Ignore)) return false;
            if (Physics.Linecast(g + Vector3.up * 1.4f, p.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return false;
            Vector3 me = Npc.transform.position;
            if (Physics.Linecast(me + Vector3.up * 0.9f, g + Vector3.up * 0.9f, blockMask, QueryTriggerInteraction.Ignore)) return false;
            if (Physics.Linecast(me + Vector3.up * 1.6f, g + Vector3.up * 1.6f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return false;
            return true;
        }

        private void SetTell(bool on)
        {
            if (on) _tellOn = true;
            else if (_tellOn)
            {
                _tellOn = false;
                if (Npc != null && Npc.Anim != null && !_charging) Npc.Anim.Telegraph = 0f;
            }
        }

        /// <summary>The skirmisher's pause is visible: its body pulses before the lunge.</summary>
        private void UpdateTell(float now)
        {
            if (_sk == SkirmishPhase.Pause && Intent == ActorIntent04D.Engage && Npc.Anim != null)
            {
                Npc.Anim.Telegraph = 0.55f + 0.35f * Mathf.Sin(now * 9f);
                _tellOn = true;
            }
            else SetTell(false);
        }

        // ================================================================== frame

        private void Update()
        {
            if (Npc == null || _cleaned) return;
            float now = Time.time;
            if (Npc.IsDead)
            {
                if (_linked || _charging || _tellOn) StopPatterns();
                return;
            }
            if (Kind == EncounterRoleKind04D.Warden) UpdateWarden(now);
            else if (Kind == EncounterRoleKind04D.Resonator) UpdateLink(now);
            else if (Kind == EncounterRoleKind04D.Skirmisher) UpdateTell(now);
            UpdateShot(now);
            UpdateThrowFollow(now);
        }
    }
}
