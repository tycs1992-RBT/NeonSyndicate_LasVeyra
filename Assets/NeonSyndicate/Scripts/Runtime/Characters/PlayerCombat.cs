using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Malik's close-quarters fighting: stamina, dodge rolls with invulnerability frames, guarding and parries,
    /// light combos and charged heavy attacks for every melee weapon, running and rolling attacks, ripostes on
    /// parried enemies, Shard Tonic healing, hit stagger with anti-stunlock, lock-on, and the last-stand save.
    /// Owned by PlayerController, which feeds it input and applies the motion it asks for.
    /// </summary>
    public sealed partial class PlayerCombat : IDamageFilter
    {
        public enum Phase { Free, Attacking, Dodging, Staggered, Drinking }

        public readonly PlayerController P;
        public Phase State { get; private set; }
        public float Stamina = 100f;
        public float MaxStamina = 100f;
        public int Tonics;
        public bool Guarding { get; private set; }
        public CharacterBase LockTarget { get; private set; }
        public MeleeMove CurrentMove { get { return State == Phase.Attacking ? _move : null; } }
        public bool Exhausted { get; private set; }
        public float LastHitTime { get; private set; }
        /// <summary>World-space velocity requested by rolls, lunges and staggers (valid while OverridesMovement).</summary>
        public Vector3 Motion { get; private set; }
        /// <summary>Facing requested by attacks and rolls (degrees).</summary>
        public float Facing { get; private set; }
        public bool OverridesMovement { get { return State == Phase.Attacking || State == Phase.Dodging || State == Phase.Staggered; } }
        public bool Invulnerable { get { return State == Phase.Dodging && _dodgeT >= _iStart && _dodgeT <= _iEnd; } }
        public float GuardWeight { get; private set; }

        public float SpeedScale
        {
            get
            {
                if (State == Phase.Drinking) return 0.42f;
                if (Guarding) return 0.55f;
                return 1f;
            }
        }

        // Skill trees, Forge flasks and the Menders add charges through SkillMods.TonicChargesBonus.
        public int MaxTonics { get { return CombatDirector.TonicCharges + (Game.Progress != null ? Game.Progress.TonicBonus : 0) + SkillMods.TonicChargesBonus; } }

        // attack
        private MeleeMove _move;
        private float _t;
        private float _ready;
        private float _charge;
        private bool _charging;
        private bool _chargeFromMouse;
        private bool _hitAny;
        private int _chain;
        private float _chainUntil;
        private MoveKind _queued;
        private bool _hasQueue;
        private float _queuedAt;
        private float _lmbDownAt = -1f;
        private bool _lmbConverted;
        private WeaponId _moveWeapon;
        private float _moveBaseDamage;
        /// <summary>The skill crit roll of the current strike frame (SkillMods.MeleeRoll).</summary>
        private bool _skillCrit;
        private readonly List<CharacterBase> _hits = new List<CharacterBase>();
        private CharacterBase _snap;
        // dodge
        private float _dodgeT;
        private float _dodgeDur;
        private float _iStart, _iEnd;
        private Vector3 _dodgeDir;
        private float _dodgeDist;
        /// <summary>Momentum along the dodge direction when it started (rolling out of a run keeps going).</summary>
        private Vector3 _dodgeCarry;
        private bool _backstep;
        private bool _perfectUsed;
        private float _perfectCooldown;
        private float _rollEndedAt = -9f;
        // stagger
        private float _staggerT;
        private float _staggerDur;
        private Vector3 _staggerPush;
        private float _lastStagger = -9f;
        private int _staggerChain;
        private float _poiseUntil;
        // drink
        private float _drinkT;
        private bool _drinkApplied;
        private bool _drinkEmpty;
        private float _healOver;
        // guard
        private bool _guardPrev;
        private float _guardStart = -9f;
        private float _staminaDelay;
        // riposte
        private NpcController _riposteTarget;
        // lock-on
        private float _lockLostTimer;
        private float _switchAccum;
        private float _switchTimer;
        private bool _stickCentered = true;
        private float _lastStand = -999f;
        private readonly RaycastHit[] _ray = new RaycastHit[8];

        public PlayerCombat(PlayerController p)
        {
            P = p;
            Tonics = CombatDirector.TonicCharges;
            Facing = p.transform.eulerAngles.y;
        }

        public void SpendStamina(float amount)
        {
            Stamina = Mathf.Max(0f, Stamina - amount);
            _staminaDelay = Mathf.Max(_staminaDelay, 0.45f);
        }

        public void RefillTonics()
        {
            Tonics = MaxTonics;
        }

        public void ResetState()
        {
            State = Phase.Free;
            _maulWaveReadyAt = 0f;
            _move = null;
            _charging = false;
            _hasQueue = false;
            _queuedAt = 0f;
            ResetResponse04G();
            _riposteTarget = null;
            _snap = null;
            _hits.Clear();
            _propsResolved04H=false;
            Guarding = false;
            GuardWeight = 0f;
            Stamina = MaxStamina;
            Exhausted = false;
            ReleaseLock();
            ClearAnim();
        }

        private void ClearAnim()
        {
            HumanoidAnimator a = P.Anim;
            if (a == null) return;
            a.AttackT = -1f;
            a.DodgeT = -1f;
            a.StaggerT = -1f;
            a.DrinkT = -1f;
            a.Guard = 0f;
        }

        // ================================================================== per-frame

        /// <summary>Reads combat input and advances the current action. wish is the camera-relative move input.</summary>
        public void Tick(float dt, bool canControl, Vector3 wish)
        {
            if (!canControl) { ResetResponse04G(); _lmbDownAt = -1f; _lmbConverted = true; _charging = false; _hasQueue = false; }
            float udt = Time.unscaledDeltaTime;
            WeaponDef d = P.Weapons.Current;
            bool melee = d.Fire == FireKind.Melee;
            bool gun = d.IsGun;
            _perfectCooldown -= udt;
            if (_healOver > 0f)
            {
                float step = Mathf.Min(_healOver, P.Health.MaxHealth * 0.25f * dt);
                P.Health.Heal(step);
                _healOver -= step;
            }

            // Stamina: quick regen after a short pause, slow while guarding, empty means a short breather.
            _staminaDelay -= dt;
            if (_staminaDelay <= 0f && State != Phase.Attacking && State != Phase.Dodging && !P.Sprinting)
                Stamina = Mathf.Min(MaxStamina, Stamina + dt * (Guarding ? 14f : 42f) * SkillMods.StaminaRegen);
            if (Stamina <= 0.5f) Exhausted = true;
            else if (Stamina > MaxStamina * 0.3f) Exhausted = false;

            UpdateLock(dt, canControl);

            if (canControl)
            {
                if (Act.LockOn && !(gun && P.Aiming)) ToggleLock();
                if (Act.Heal) TryDrink();
                if (Act.Dodge) BufferDodge04G(wish);
                AdvanceDodge04G();
            }

            // Guard: right mouse with a melee weapon or fists, or the pad's guard button with anything.
            bool guardHeld = canControl && (Act.PadGuardHeld || (!gun && d.Fire != FireKind.Throw && GInput.GameMouse(1)));
            if (guardHeld && !_guardPrev) _guardStart = Time.time;
            _guardPrev = guardHeld;
            if (guardHeld && State == Phase.Attacking && _move != null && CombatResponse04G.CanDefend(_move, _t, MeleeMoves.ClassOf(_moveWeapon)))
            {
                EndAttack04G();
                _guardStart = Time.time; // Parry timing begins when the arms can actually rise.
            }
            Guarding = guardHeld && State == Phase.Free && Stamina > 0f && !P.InVehicle;
            GuardWeight = Mathf.MoveTowards(GuardWeight, Guarding ? 1f : 0f, dt * 9f);

            // Attacks.
            if (canControl && !P.InVehicle)
            {
                if (melee || (gun && Act.PadLight)) HandleAttackInput(d, melee);
            }
            if (!GInput.GameMouse(0)) _lmbDownAt = -1f;

            switch (State)
            {
                case Phase.Attacking: UpdateAttack(dt, canControl); break;
                case Phase.Dodging: UpdateDodge(dt); break;
                case Phase.Staggered: UpdateStagger(dt); break;
                case Phase.Drinking: UpdateDrink(dt, canControl); break;
                default: Motion = Vector3.zero; break;
            }
            if (State == Phase.Free && !OverridesMovement) Facing = P.transform.eulerAngles.y;

            HumanoidAnimator a = P.Anim;
            a.Guard = GuardWeight;
            // Guns and arc gauntlets block with a palm barrier; fists use a boxer's guard.
            a.EnergyGuard = gun || d.Id == WeaponId.ArcGauntlets;
            a.TwoHanded = d.Id == WeaponId.CrystalMaul || d.Id == WeaponId.SluggerBat;
            a.HoldBlade = melee && d.Id != WeaponId.Fists && d.Id != WeaponId.ArcGauntlets;
            a.PairedBlades = d.Id == WeaponId.Switchblade;
            // Fighting stance while a fight is on: locked on, recently hostile, or mid-combo.
            bool fightOn = LockTarget != null || CombatDirector.InCombat || State == Phase.Attacking || Time.time < _chainUntil + 1.5f;
            _ready = Mathf.MoveTowards(_ready, melee && fightOn && !P.InVehicle && !P.Sprinting ? 1f : 0f, dt * 3f);
            a.CombatReady = _ready;
        }

        // ================================================================== attacks

        private void HandleAttackInput(WeaponDef d, bool melee)
        {
            bool lmbDown = melee && GInput.GameMouseDown(0);
            bool lmbHeld = melee && GInput.GameMouse(0);
            if (lmbDown)
            {
                _lmbDownAt = Time.unscaledTime;
                _lmbConverted = false;
            }
            bool light = Act.PadLight || (melee && !lmbHeld && _lmbDownAt >= 0f && !_lmbConverted);
            bool heavy = melee && Act.PadHeavy;
            if (melee && lmbHeld && _lmbDownAt >= 0f && !_lmbConverted && Time.unscaledTime - _lmbDownAt >= 0.18f)
            {
                _lmbConverted = true;
                heavy = true;
            }
            if (!light && !heavy) return;
            MoveKind kind = heavy ? MoveKind.Heavy : MoveKind.Light;
            if (State == Phase.Attacking)
            {
                _queued = kind;
                _hasQueue = true;
                _queuedAt = Time.time;
                return;
            }
            if (State == Phase.Dodging)
            {
                if (_dodgeT / _dodgeDur > 0.4f && !_backstep)
                {
                    _queued = kind;
                    _hasQueue = true;
                    _queuedAt = Time.time;
                }
                return;
            }
            if (State != Phase.Free) return;
            BeginAttack(kind, d, melee);
            if (kind == MoveKind.Heavy && State == Phase.Attacking && _move != null && _move.Kind == MoveKind.Heavy)
            {
                _charging = true;
                _chargeFromMouse = lmbHeld;
            }
        }

        private bool _chainFromStart;
        private float _maulWaveReadyAt;

        private void BeginAttack(MoveKind kind, WeaponDef d, bool melee)
        {
            if (Stamina <= 0.5f)
            {
                if (Game.Hud != null) Game.Hud.CombatText("Out of stamina", new Color(0.6f, 0.8f, 0.3f));
                return;
            }
            MeleeClass c = MeleeMoves.ClassOf(d.Id);
            MeleeMove move;
            _chainFromStart = false;
            if (!melee)
            {
                move = MeleeMoves.GunBash;
            }
            else if (kind == MoveKind.Light && RiposteReady())
            {
                move = MeleeMoves.Riposte(c);
            }
            else if (kind == MoveKind.Light && Time.time - _rollEndedAt < 0.3f)
            {
                move = MeleeMoves.RollAttack(c);
            }
            else if (kind == MoveKind.Light && P.Sprinting)
            {
                move = MeleeMoves.Running(c);
            }
            else if (kind == MoveKind.Heavy)
            {
                move = MeleeMoves.Heavy(c);
            }
            else
            {
                MeleeMove[] chain = MeleeMoves.Lights(c);
                if (Time.time > _chainUntil) _chain = 0;
                move = chain[_chain % chain.Length];
                _chainFromStart = _chain == 0 || Time.time > _chainUntil;
                // Chain advances only after stamina admission.
            }
            bool maulWave = c == MeleeClass.Maul && kind == MoveKind.Heavy && Time.time >= _maulWaveReadyAt;
            if (c == MeleeClass.Maul && kind == MoveKind.Heavy && !maulWave) move = MeleeMoves.Lights(c)[1];
            bool admitted = StartMove(move, d, 0f);
            if (admitted && move.Kind == MoveKind.Light && kind == MoveKind.Light && melee) _chain = (_chain + 1) % MeleeMoves.Lights(c).Length;
            if (admitted && State == Phase.Attacking && _move == move)
            {
                if (maulWave) _maulWaveReadyAt = Time.time + 6f;
                // A heavy buffered through a previous swing or roll retains the current hold intent.
                if (move.Kind == MoveKind.Heavy)
                {
                    _charging = true;
                    _chargeFromMouse = GInput.GameMouse(0);
                }
            }
        }

        private bool StartMove(MeleeMove move, WeaponDef d, float startT)
        {
            if (move == null || Stamina < move.Stamina)
            {
                if (Game.Hud != null) Game.Hud.CombatText("Recover stamina", UIKit.Gold);
                return false;
            }
            _move = move;
            _t = startT;
            _charge = 0f;
            _charging = false;
            _hitAny = false;
            _hits.Clear();
            _propsResolved04H=false;
            _hasQueue = false;
            _moveWeapon = d.Id;
            _moveBaseDamage = d.Fire == FireKind.Melee ? d.Damage : MeleeMoves.GunBashDamage;
            Stamina = Mathf.Max(-10f, Stamina - move.Stamina);
            _staminaDelay = 0.6f;
            State = Phase.Attacking;
            // Pick who to swing at: the locked target, a riposte target, or the best enemy in front.
            _snap = move.Kind == MoveKind.Riposte && _riposteTarget != null ? _riposteTarget : (LockTarget != null ? LockTarget : FindMeleeTarget(Mathf.Max(3.2f, move.Range + move.Lunge + 0.8f)));
            if (_snap != null)
            {
                Vector3 to = MathUtil.Flat(_snap.transform.position - P.transform.position);
                if (to.sqrMagnitude > 0.01f) Facing = MathUtil.YawOf(to);
            }
            else if (Game.Cam != null && P.FlatVelocity.sqrMagnitude < 1f)
            {
                Facing = Game.Cam.Yaw;
            }
            if (move.Kind == MoveKind.Riposte && _riposteTarget != null)
            {
                _riposteTarget.ConsumeRiposte();
                _riposteTarget = null;
                if (Game.Cam != null) Game.Cam.Shake(0.15f);
            }
            // The whoosh is synchronized with the active blade, after a held windup releases.
            HumanoidAnimator a = P.Anim;
            a.AttackAnim = move.Anim;
            a.AttackMirror = move.Anim == MeleeAnim.Cross || d.Id == WeaponId.Switchblade && move.Anim == MeleeAnim.SlashLeft;
            a.AttackT = 0f;
            P.Weapons.CancelActions();
            _swingCued04G = false;
            return true;
        }

        private void UpdateAttack(float dt, bool canControl)
        {
            MeleeMove m = _move;
            if (m == null) { State = Phase.Free; return; }
            WeaponDef d = P.Weapons.Current;
            if (d.Id != _moveWeapon) { EndAttack04G(); return; }
            // Charging a heavy holds the swing at the top of the wind-up.
            if (_charging)
            {
                bool held = _chargeFromMouse ? GInput.GameMouse(0) : Act.PadHeavyHeld;
                if (held && canControl && _charge < m.ChargeMax)
                {
                    if (_t >= m.Windup * 0.85f)
                    {
                        _charge += dt;
                        _t = m.Windup * 0.85f;
                    }
                }
                else _charging = false;
            }
            float prevT = _t;
            _t += dt;
            float lungeStart = m.Windup * 0.55f;
            float lungeEnd = m.Windup + m.Active;
            // Track the target through most of the wind-up, then commit.
            if (_snap != null && !_snap.IsDead && _t < m.Windup * 0.8f)
            {
                Vector3 to = MathUtil.Flat(_snap.transform.position - P.transform.position);
                if (to.sqrMagnitude > 0.01f) Facing = Mathf.MoveTowardsAngle(Facing, MathUtil.YawOf(to), 540f * dt);
            }
            Vector3 fwd = MathUtil.DirFromYaw(Facing);
            Motion = Vector3.zero;
            if (_t > lungeStart && _t < lungeEnd && m.Lunge > 0f)
            {
                float speed = m.Lunge / Mathf.Max(0.05f, lungeEnd - lungeStart);
                if (_snap != null && !_snap.IsDead)
                {
                    // Big creatures are measured to their edge so the lunge stops at the body, not inside it.
                    float dist = MathUtil.FlatDistance(_snap.transform.position, P.transform.position) - _snap.BodyRadius;
                    if (dist < 1.0f) speed = 0f;
                    else speed = Mathf.Min(speed, (dist - 0.9f) / Mathf.Max(0.05f, lungeEnd - _t) + 0.5f);
                }
                Motion = fwd * speed;
            }
            // Active frames. The sound cannot repeat during a long hit window.
            if (!_swingCued04G && _t >= m.Windup)
            {
                _swingCued04G = true;
                if (Game.Audio != null) Game.Audio.PlayAt(SfxId.Swing, P.ChestPosition, m.Kind == MoveKind.Heavy ? 0.62f : 0.4f,
                    CombatResponse04G.SwingPitch(MeleeMoves.ClassOf(_moveWeapon)), 22f);
            }
            if (_t >= m.Windup && prevT < m.Windup + m.Active)
            {
                float mult = (1f + _charge / Mathf.Max(0.01f, m.ChargeMax) * 0.6f) * DamageMultiplier(d);
                bool killed;
                int before = _hits.Count;
                int n = CombatDirector.Strike(P, m, _moveBaseDamage, mult, _moveWeapon, _hits, out killed,
                    Mathf.Clamp01((prevT-m.Windup)/Mathf.Max(.01f,m.Active)),
                    Mathf.Clamp01((_t-m.Windup)/Mathf.Max(.01f,m.Active)),P.Anim.AttackMirror,!_propsResolved04H);
                _propsResolved04H=true;
                if (_skillCrit && _hits.Count > before) ProgressionHooks.ShowMeleeCrit();
                if (n > 0 && _hits.Count > before && !_hitAny)
                {
                    _hitAny = true;
                    OnLandedHit(m, killed);
                }
            }
            // Combo buffer: the next attack starts as soon as this one may be cancelled.
            float cancelAt = m.Windup + m.Active + m.Recovery * m.CancelAt;
            if (_hasQueue && Time.time - _queuedAt > MeleeMoves.InputBufferSeconds(m)) _hasQueue = false;
            if (_hasQueue && _t >= cancelAt && canControl)
            {
                _hasQueue = false;
                _chainUntil = Time.time + 0.6f;
                EndAttack04G();
                bool meleeWeapon = d.Fire == FireKind.Melee;
                BeginAttack(_queued, d, meleeWeapon);
                return;
            }
            HumanoidAnimator a = P.Anim;
            a.AttackT = Mathf.Clamp01(_t / m.Total);
            a.AttackWindup = m.Windup / m.Total;
            a.AttackActive = m.Active / m.Total;
            if (_t >= m.Total)
            {
                State = Phase.Free;
                _chainUntil = Time.time + 0.55f;
                a.AttackT = -1f;
                _move = null;
            }
        }

        private float DamageMultiplier(WeaponDef d)
        {
            float k = 1f;
            if (Game.Progress != null) k *= Game.Progress.MeleeMultiplier;
            if (Game.Gear != null) k *= Game.Gear.MeleeMultiplier(d.Id);
            // Skill trees: melee damage, the riposte bonus and the skill critical roll (SkillMods; never on a gear critical).
            k *= SkillMods.MeleeRoll(_move != null && _move.Kind == MoveKind.Riposte, Game.Gear != null && Game.Gear.LastCrit, out _skillCrit);
            return k;
        }

        private void OnLandedHit(MeleeMove m, bool killed)
        {
            LastHitTime = Time.time;
            if(Game.Crafting!=null && Game.Fx!=null && Game.Crafting.BuffLeft("brew_vanguard_edge_04k")>0f)
                Game.Fx.RockBurst(P.ChestPosition+P.transform.forward,new Color(.6f,.85f,1f),
                    Game.CrystalCombat!=null&&Game.CrystalCombat.ReducedEffects?1:3,.22f);
            bool heavy = m.Kind == MoveKind.Heavy || m.Kind == MoveKind.Riposte;
            CombatDirector.HitStop((m.Kind == MoveKind.Riposte ? 0.09f : (heavy ? 0.065f : 0.035f)) * (Game.CrystalCombat != null && Game.CrystalCombat.ReducedEffects ? 0.35f : 1f));
            if (Game.Cam != null) Game.Cam.Shake(heavy ? 0.22f : 0.1f);
            Pad.Rumble(heavy ? 0.7f : 0.35f, heavy ? 0.5f : 0.3f, heavy ? 0.22f : 0.1f);
            if (Game.Audio != null)
            {
                WeaponDef d = Weapons.Get(_moveWeapon);
                Game.Audio.PlayVar(d.Fire == FireKind.Melee ? d.Sound : SfxId.Punch, P.ChestPosition + P.transform.forward, heavy ? 0.95f : 0.75f, 0.1f, 30f);
            }
            if (Game.Hud != null) Game.Hud.HitMarker(killed);
            if (Game.Gear != null) Game.Gear.OnMeleeHit(_hits.Count > 0 ? _hits[_hits.Count - 1] : null, m);
            Stamina = Mathf.Min(MaxStamina, Stamina + 2f);
        }

        private CharacterBase FindMeleeTarget(float range)
        {
            Vector3 p = P.transform.position;
            Vector3 aim = Game.Cam != null ? Game.Cam.FlatForward : P.transform.forward;
            Vector3 move = P.FlatVelocity;
            if (move.sqrMagnitude > 1f) aim = (aim + move.normalized).normalized;
            CharacterBase best = null;
            float bestScore = float.MaxValue;
            foreach (NpcController n in NpcController.All)
            {
                if (n.IsDead || n.InVehicle || WeaponController.CrystalProtected(n)) continue;
                Vector3 to = MathUtil.Flat(n.transform.position - p);
                float dist = to.magnitude;
                if (dist > range || Mathf.Abs(n.transform.position.y - p.y) > 1.8f) continue;
                float angle = Vector3.Angle(aim, to);
                if (angle > 75f || Physics.Linecast(P.HeadPosition, n.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                float score = dist + angle * 0.03f - (n.IsHostileToPlayer ? 1.5f : 0f);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = n;
                }
            }
            for (int i = 0; i < Creature.All.Count; i++)
            {
                Creature c = Creature.All[i];
                if (!c.Targetable) continue;
                Vector3 to = MathUtil.Flat(c.transform.position - p);
                float dist = Mathf.Max(0f, to.magnitude - c.BodyRadius);
                float dy = c.transform.position.y - p.y;
                if (dist > range || dy > 1.8f || dy < -(1.8f + c.BodyHeight)) continue;
                float angle = Vector3.Angle(aim, to);
                if (angle > 75f + Mathf.Atan2(c.BodyRadius, Mathf.Max(0.5f, to.magnitude)) * Mathf.Rad2Deg) continue;
                float score = dist + angle * 0.03f - 1.5f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }
            return best;
        }

        private bool RiposteReady()
        {
            if (_riposteTarget == null) return false;
            if (_riposteTarget.IsDead || !_riposteTarget.CanBeRiposted) { _riposteTarget = null; return false; }
            Vector3 to = MathUtil.Flat(_riposteTarget.transform.position - P.transform.position);
            return to.magnitude < 3.2f;
        }

        // ================================================================== dodge

        private void TryDodge(Vector3 wish)
        {
            if (P.InVehicle || !P.IsGroundedPublic) return;
            if (State == Phase.Attacking)
            {
                // Rolls cancel an attack only once its hit has gone out.
                if (_move == null || !CombatResponse04G.CanDefend(_move, _t, MeleeMoves.ClassOf(_moveWeapon))) return;
            }
            else if (State != Phase.Free && State != Phase.Drinking) return;
            if (State == Phase.Drinking && _drinkT < 0.5f) return;
            float dodgeCost = (wish.sqrMagnitude < 0.04f ? 12f : 18f) * SkillMods.DodgeCost;
            if (Stamina < dodgeCost) return;
            _backstep = wish.sqrMagnitude < 0.04f;
            Vector3 dir;
            if (_backstep) dir = -P.transform.forward;
            else dir = wish.normalized;
            _dodgeDir = MathUtil.Flat(dir).normalized;
            if (_dodgeDir.sqrMagnitude < 0.01f) _dodgeDir = -P.transform.forward;
            _dodgeCarry = _dodgeDir * Mathf.Max(0f, Vector3.Dot(MathUtil.Flat(P.FlatVelocity), _dodgeDir));
            _dodgeDur = _backstep ? 0.4f : 0.64f;
            _dodgeDist = _backstep ? 1.9f : 4.3f;
            _iStart = _backstep ? 0.02f : 0.03f;
            _iEnd = _backstep ? 0.22f : 0.42f;
            if (Game.Gear != null) _iEnd += Game.Gear.DodgeBonus;
            _iEnd += SkillMods.DodgeIFrames;
            _iEnd = Mathf.Min(_iEnd, _dodgeDur * 0.72f);
            _dodgeT = 0f;
            _perfectUsed = false;
            Stamina -= (_backstep ? 12f : 18f) * SkillMods.DodgeCost;
            _staminaDelay = 0.7f;
            State = Phase.Dodging;
            _hasQueue = false;
            _charging = false;
            Guarding = false;
            _dodgeBufferedUntil04G = -1f;
            _move = null;
            P.Anim.AttackT = -1f;
            P.Anim.DrinkT = -1f;
            if (!_backstep) Facing = MathUtil.YawOf(_dodgeDir);
            P.Anim.Backstep = _backstep;
            P.Anim.DodgeT = 0f;
            P.Anim.DodgeDir = P.transform.InverseTransformDirection(_dodgeDir);
            P.Weapons.CancelActions();
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.Jump, P.transform.position, 0.3f, 0.15f, 12f);
            Pad.Rumble(0.15f, 0.1f, 0.08f);
        }

        /// <summary>Share of the dodge distance covered at normalised time k. It starts and ends at rest: the feet push
        /// off from where they stand, the body is fastest about a fifth of the way in and has settled before the
        /// stand-up, so the animation never has to slide a planted foot.</summary>
        public static float DodgeCurve(float k)
        {
            k = Mathf.Clamp01(k);
            float u = 1f - k;
            float u2 = u * u;
            return 1f - u2 * u2 * u * (1f + 5f * k);
        }

        private void UpdateDodge(float dt)
        {
            float prev = _dodgeT / _dodgeDur;
            _dodgeT += dt;
            float k = Mathf.Clamp01(_dodgeT / _dodgeDur);
            float carry = 1f - Mathf.Clamp01(k / 0.25f);
            Motion = _dodgeDir * ((DodgeCurve(k) - DodgeCurve(prev)) * _dodgeDist / Mathf.Max(0.0001f, dt)) + _dodgeCarry * (carry * carry);
            if (!_backstep) Facing = MathUtil.YawOf(_dodgeDir);
            P.Anim.DodgeT = k;
            if (_hasQueue && Time.time - _queuedAt > 0.8f) _hasQueue = false;
            if (_hasQueue && k > 0.78f && _dodgeT > _iEnd)
            {
                _hasQueue = false;
                _rollEndedAt = Time.time;
                State = Phase.Free;
                P.Anim.DodgeT = -1f;
                WeaponDef d = P.Weapons.Current;
                BeginAttack(_queued, d, d.Fire == FireKind.Melee);
                return;
            }
            if (k >= 1f)
            {
                State = Phase.Free;
                _rollEndedAt = Time.time;
                P.Anim.DodgeT = -1f;
                Motion = Vector3.zero;
            }
        }

        // ================================================================== stagger

        private void Stagger(float seconds, Vector3 push)
        {
            if (State == Phase.Dodging) return;
            if (Time.time < _poiseUntil) return;
            // Anti-stunlock: the third stagger in a row grants a moment of poise.
            if (Time.time - _lastStagger < 1.6f) _staggerChain++;
            else _staggerChain = 1;
            _lastStagger = Time.time;
            if (_staggerChain >= 3) _poiseUntil = Time.time + 1.4f;
            State = Phase.Staggered;
            _staggerT = 0f;
            _staggerDur = seconds;
            _staggerPush = MathUtil.Flat(push);
            _move = null;
            _charging = false;
            P.Anim.AttackT = -1f;
            P.Anim.DrinkT = -1f;
            P.Anim.StaggerT = 0f;
            Vector3 local = P.transform.InverseTransformDirection(push);
            P.Anim.StaggerSide = Mathf.Clamp(local.x * 2f, -1f, 1f);
            P.Weapons.CancelActions();
        }

        private void UpdateStagger(float dt)
        {
            _staggerT += dt;
            float k = Mathf.Clamp01(_staggerT / _staggerDur);
            Motion = _staggerPush * (1f - k) * 2.2f;
            P.Anim.StaggerT = k;
            if (k >= 1f)
            {
                State = Phase.Free;
                P.Anim.StaggerT = -1f;
                Motion = Vector3.zero;
            }
        }

        // ================================================================== drinking

        private void TryDrink()
        {
            if (State != Phase.Free || P.InVehicle) return;
            State = Phase.Drinking;
            _drinkT = 0f;
            _drinkApplied = false;
            _drinkEmpty = Tonics <= 0;
            P.Anim.DrinkT = 0f;
            P.Weapons.CancelActions();
            if (_drinkEmpty && Game.Hud != null) Game.Hud.CombatText("No Shard Tonic left", new Color(0.9f, 0.5f, 0.5f));
        }

        private void UpdateDrink(float dt, bool canControl)
        {
            _drinkT += dt;
            float dur = _drinkEmpty ? 0.55f : 0.95f;
            P.Anim.DrinkT = Mathf.Clamp01(_drinkT / dur);
            if (!_drinkEmpty && !_drinkApplied && _drinkT >= 0.45f)
            {
                _drinkApplied = true;
                Tonics--;
                float heal = P.Health.MaxHealth * CombatDirector.TonicHeal * (Game.Gear != null ? Game.Gear.HealMultiplier : 1f) * SkillMods.TonicHeal;
                P.Health.Heal(heal * 0.7f);
                _healOver += heal * 0.3f;
                P.Health.Burning = 0f;
                if (Game.Fx != null) Game.Fx.Flash(P.ChestPosition, new Color(0.3f, 1f, 0.85f), 2.5f, 5f, 0.4f);
                if (Game.Audio != null) Game.Audio.Play2D(SfxId.Power, 0.45f, 1.4f);
                Pad.Rumble(0.2f, 0.35f, 0.25f);
            }
            if (_drinkT >= dur)
            {
                State = Phase.Free;
                P.Anim.DrinkT = -1f;
            }
        }

        // ================================================================== lock-on

        private void ToggleLock()
        {
            if (LockTarget != null)
            {
                ReleaseLock();
                return;
            }
            CharacterBase t = FindLockTarget(null, 0);
            if (t != null)
            {
                LockTarget = t;
                _lockLostTimer = 0f;
                if (Game.Audio != null) Game.Audio.Play2D(SfxId.UIHover, 0.5f, 1.3f);
            }
            else if (Game.Cam != null)
            {
                // Nothing to lock on to: recentre the camera behind Malik.
                Game.Cam.RecenterBehindPlayer();
            }
        }

        public void ReleaseLock()
        {
            LockTarget = null;
        }

        private void UpdateLock(float dt, bool canControl)
        {
            if (LockTarget == null) return;
            if (P.InVehicle || P.IsDead) { ReleaseLock(); return; }
            CharacterBase t = LockTarget;
            float dist = t != null ? Vector3.Distance(t.transform.position, P.transform.position) : 999f;
            if (t == null || t.IsDead || dist > 32f + (t != null ? t.BodyRadius : 0f))
            {
                // The target fell (person or creature): hop to the next enemy close by, if any.
                CharacterBase next = t != null && t.IsDead ? FindLockTarget(null, 0, 16f, true) : null;
                LockTarget = next;
                return;
            }
            // A living creature that slipped out of reach (a wyrm diving under the sand, a shade fading out) lets go.
            Creature lockedCreature = t as Creature;
            if (lockedCreature != null && !lockedCreature.Targetable) { ReleaseLock(); return; }
            bool los = !Physics.Linecast(P.HeadPosition, t.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore);
            _lockLostTimer = los ? 0f : _lockLostTimer + dt;
            if (_lockLostTimer > 2.5f) { ReleaseLock(); return; }
            if (!canControl) return;
            // Switch targets with a flick of the right stick or a quick mouse swipe.
            Vector2 stick = Pad.RightStick;
            int dir = 0;
            if (Mathf.Abs(stick.x) < 0.3f) _stickCentered = true;
            else if (_stickCentered && Mathf.Abs(stick.x) > 0.75f)
            {
                _stickCentered = false;
                dir = stick.x > 0f ? 1 : -1;
            }
            _switchTimer -= Time.unscaledDeltaTime;
            if (_switchTimer <= 0f) _switchAccum = 0f;
            float mx = GInput.RawMouseDelta.x;
            if (Mathf.Abs(mx) > 0.05f)
            {
                _switchAccum += mx;
                _switchTimer = 0.25f;
                if (Mathf.Abs(_switchAccum) > 9f)
                {
                    dir = _switchAccum > 0f ? 1 : -1;
                    _switchAccum = 0f;
                    _switchTimer = 0.35f;
                }
            }
            if (dir != 0)
            {
                CharacterBase next = FindLockTarget(LockTarget, dir);
                if (next != null)
                {
                    LockTarget = next;
                    if (Game.Audio != null) Game.Audio.Play2D(SfxId.UIHover, 0.4f, 1.4f);
                }
            }
        }

        private CharacterBase FindLockTarget(CharacterBase from, int side, float range = 26f, bool hostileOnly = false)
        {
            if (Game.Cam == null) return null;
            Vector3 p = P.transform.position;
            Vector3 camFwd = Game.Cam.FlatForward;
            Vector3 camRight = MathUtil.RightOf(camFwd);
            Vector3 fromDir = from != null ? MathUtil.Flat(from.transform.position - p).normalized : camFwd;
            CharacterBase best = null;
            float bestScore = float.MaxValue;
            foreach (NpcController n in NpcController.All)
            {
                if (n == from || n.IsDead || n.InVehicle || !LootCombat.IsFoe(n, P.gameObject)) continue;
                Vector3 to = n.transform.position - p;
                float dist = to.magnitude;
                if (dist > range) continue;
                bool hostile = n.IsHostileToPlayer || n.Target == P;
                if (hostileOnly && !hostile) continue;
                Vector3 flat = MathUtil.Flat(to);
                float angle = Vector3.Angle(camFwd, flat);
                if (side == 0 && angle > 80f && dist > 4f) continue;
                if (side != 0)
                {
                    // Must lie on the requested side of the current target, measured around the camera.
                    float s = Vector3.Dot(flat.normalized - fromDir, camRight);
                    if (s * side < 0.05f) continue;
                    angle = Vector3.Angle(fromDir, flat);
                }
                if (Physics.Linecast(P.HeadPosition, n.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                float score = angle * 0.6f + dist * 1.2f + (hostile ? -25f : (n.Role == NpcRole.Civilian ? 18f : 6f));
                if (score < bestScore)
                {
                    bestScore = score;
                    best = n;
                }
            }
            // Creatures: big ones are measured to their edge, so a drake or the wyrm can be locked from further out.
            for (int i = 0; i < Creature.All.Count; i++)
            {
                Creature c = Creature.All[i];
                if (c == from || !c.Targetable) continue;
                Vector3 to = c.transform.position - p;
                float dist = Mathf.Max(0f, to.magnitude - c.BodyRadius);
                if (dist > range + Mathf.Min(10f, c.BodyRadius)) continue;
                if (hostileOnly && !c.IsHostileToPlayer) continue;
                Vector3 flat = MathUtil.Flat(to);
                float angle = Vector3.Angle(camFwd, flat);
                if (side == 0 && angle > 80f && dist > 4f) continue;
                if (side != 0)
                {
                    float s2 = Vector3.Dot(flat.normalized - fromDir, camRight);
                    if (s2 * side < 0.05f) continue;
                    angle = Vector3.Angle(fromDir, flat);
                }
                if (Physics.Linecast(P.HeadPosition, c.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                float score = angle * 0.6f + dist * 1.2f + (c.IsEngaged ? -28f : -18f);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }
            // 04E: an Underheart boss is not a Creature: its lock-on stand-in (aimed at its open weak point, else its centre).
            CharacterBase boss;
            float bossScore;
            if (UBossLockProxy04E.TryLockScore(p, P.HeadPosition, camFwd, camRight, fromDir, from, side, range, out boss, out bossScore) && bossScore < bestScore) best = boss;
            return best;
        }

        // ================================================================== incoming damage

        public bool FilterDamage(ref DamageInfo info)
        {
            if (P.IsDead) return true;
            bool melee = info.Type == DamageType.Melee;
            // Dodge invulnerability (and the perfect dodge: slip a hit right as the roll starts).
            if (Invulnerable && info.Type != DamageType.Fall && info.Type != DamageType.Vehicle)
            {
                if (!_perfectUsed && _perfectCooldown <= 0f && _dodgeT < 0.24f && (melee || info.Type == DamageType.Bullet || info.Type == DamageType.Power))
                {
                    _perfectUsed = true;
                    _perfectCooldown = 1.5f;
                    CombatDirector.Slow(0.32f, 0.35f);
                    Stamina = Mathf.Min(MaxStamina, Stamina + 15f);
                    if (Game.Hud != null) Game.Hud.CombatText("Perfect dodge", new Color(0.5f, 0.9f, 1f));
                    if (Game.Gear != null) Game.Gear.OnPerfectDodge();
                    ProgressionHooks.OnPerfectDodge();
                    Pad.Rumble(0.1f, 0.4f, 0.12f);
                }
                return false;
            }
            Vector3 toAttacker = -info.Direction;
            toAttacker.y = 0f;
            bool frontal = toAttacker.sqrMagnitude < 0.01f || Vector3.Angle(P.transform.forward, toAttacker) < 75f;
            bool guardable = info.Type == DamageType.Melee || info.Type == DamageType.Bullet || info.Type == DamageType.Explosion || info.Type == DamageType.Power;
            bool unblockable = (info.Flags & DamageFlags.Unblockable) != 0;
            if (Guarding && frontal && guardable && !unblockable)
            {
                CharacterBase attacker = info.Source != null ? info.Source.GetComponentInParent<CharacterBase>() : null;
                // Parry: guard pressed just before the hit.
                if (Time.time - _guardStart <= CombatDirector.ParryWindow + (Game.Gear != null ? Game.Gear.ParryBonus : 0f) + SkillMods.ParryWindowBonus && (melee || info.Type == DamageType.Bullet || info.Type == DamageType.Power && (info.Flags & DamageFlags.Parryable) != 0))
                {
                    OnParry(attacker, info);
                    return false;
                }
                float cost;
                float keep;
                switch (info.Type)
                {
                    case DamageType.Melee: cost = info.Amount * 0.75f + 6f; keep = P.Weapons.Current.Id == WeaponId.Fists ? 0.15f : 0f; break;
                    case DamageType.Bullet: cost = 3f + info.Amount * 0.05f; keep = 0.2f; break;
                    case DamageType.Explosion: cost = 22f; keep = 0.5f; break;
                    default: cost = 6f; keep = 0.3f; break;
                }
                if (Game.Gear != null) cost *= Game.Gear.GuardCostMultiplier;
                cost *= SkillMods.GuardCost;
                if (P.Weapons.Current.Id == WeaponId.Switchblade) cost *= 1.5f;
                _staminaDelay = 0.8f;
                if (Stamina < cost)
                {
                    // Guard break: the block holds this hit but the arms fly open.
                    Stamina = 0f;
                    Exhausted = true;
                    info.Amount *= 0.5f;
                    Stagger(1.0f, info.Direction);
                    if (Game.Hud != null) Game.Hud.CombatText("Guard broken", new Color(1f, 0.45f, 0.3f));
                    if (Game.Audio != null) Game.Audio.PlayAt(SfxId.MetalHit, P.ChestPosition, 0.9f);
                    Pad.Rumble(0.8f, 0.6f, 0.3f);
                    return true;
                }
                Stamina -= cost;
                if (Game.Fx != null) Game.Fx.Sparks(P.ChestPosition + P.transform.forward * 0.4f, toAttacker.normalized * 2f, melee ? 8 : 3);
                if (Game.Audio != null) Game.Audio.PlayVar(melee ? SfxId.MetalHit : SfxId.Ricochet, P.ChestPosition, melee ? 0.7f : 0.4f, 0.1f, 25f);
                if (melee)
                {
                    CombatDirector.HitStop(0.04f);
                    Pad.Rumble(0.4f, 0.3f, 0.12f);
                }
                P.Anim.GuardHit = 1f;
                if (keep <= 0f) return false;
                info.Amount *= keep;
                info.Flags &= ~DamageFlags.Knockdown;
                return true;
            }
            // 04E: the ward (handheld Lattice Shield with its aspect) and companion protection get a say before the hit lands.
            float beforeHooks = info.Amount;
            if (!PlayerDamageHooks.Apply(ref info)) return false;
            if (beforeHooks > 0f && info.Amount <= 0f) return false;
            // A real hit: flinch from melee unless swinging a heavy (hyper armor) or poised. A harmless shove from no one
            // (the Veil throwing the player out) is not a fight: no combat lock on stations and contacts.
            bool shove = info.Amount <= 0f && info.Source == null;
            if (!shove)
            {
                LastHitTime = Time.time;
                CombatDirector.MarkCombat();
            }
            if (melee)
            {
                bool armor = State == Phase.Attacking && _move != null && _move.HyperArmor && _t < _move.Windup + _move.Active;
                // A nibble (a Glass Mite's bite) stings without a flinch, so a swarm can never stun-lock the dodge.
                bool nibble = CreatureFightRules.IsNibble(info.Force, info.Flags, info.Amount);
                if (!armor && !nibble)
                {
                    bool big = info.Force > 6f || (info.Flags & DamageFlags.Heavy) != 0;
                    Stagger(big ? 0.55f : 0.32f, info.Direction);
                }
                Pad.Rumble(nibble ? 0.2f : 0.55f, nibble ? 0.1f : 0.25f, nibble ? 0.08f : 0.18f);
            }
            else if (info.Type == DamageType.Bullet)
            {
                Pad.Rumble(0.25f, 0.15f, 0.08f);
            }
            else if (info.Type == DamageType.Explosion)
            {
                Pad.Rumble(1f, 0.8f, 0.45f);
            }
            if (Game.Gear != null) info.Amount *= Game.Gear.DamageTakenMultiplier(info.Type);
            // Skill trees, brews and Frostline resistance (SkillMods).
            info.Amount *= SkillMods.IncomingScale(P.transform.position);
            return true;
        }

        private void OnParry(CharacterBase attacker, DamageInfo info)
        {
            Stamina = Mathf.Min(MaxStamina, Stamina + 10f);
            P.Anim.ParryT = 0f;
            if (Game.Fx != null)
            {
                Game.Fx.Sparks(P.ChestPosition + P.transform.forward * 0.45f, P.transform.forward * 3f, 18);
                Game.Fx.Flash(P.ChestPosition + P.transform.forward * 0.5f, new Color(0.5f, 0.9f, 1f), 4f, 6f, 0.25f);
            }
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.MetalHit, P.ChestPosition, 1f, 0.05f, 40f);
            Pad.Rumble(0.3f, 1f, 0.15f);
            NpcController npc = attacker as NpcController;
            Creature creature = attacker as Creature;
            if (info.Type == DamageType.Melee && creature != null)
            {
                // Parrying a creature's bite or claw knocks it off balance: it reels, open to punishment.
                creature.OnParried(P);
                CombatDirector.Slow(0.35f, 0.25f);
                if (Game.Hud != null) Game.Hud.CombatText("PARRY", new Color(0.55f, 0.95f, 1f));
                if (Game.Cam != null) Game.Cam.Shake(0.18f);
                if (Game.Gear != null) Game.Gear.OnParry(null);
            }
            else if (info.Type == DamageType.Melee && npc != null)
            {
                npc.OnParried(P);
                _riposteTarget = npc;
                if (Game.Gear != null) Game.Gear.OnParry(npc);
                CombatDirector.Slow(0.35f, 0.25f);
                if (Game.Hud != null) Game.Hud.CombatText("PARRY", new Color(0.55f, 0.95f, 1f));
                if (Game.Cam != null) Game.Cam.Shake(0.18f);
            }
            else
            {
                CombatDirector.Slow(0.12f, 0.4f);
                if (Game.Hud != null) Game.Hud.CombatText("Deflect", new Color(0.55f, 0.95f, 1f));
            }
        }

        /// <summary>Last stand: a killing blow taken from healthy leaves Malik on 1 HP, once in a while.</summary>
        public bool OnLethal(DamageInfo info)
        {
            float cd = CombatDirector.LastStandCooldown;
            if (cd <= 0f) return false;
            if (Time.time - _lastStand < cd) return false;
            if (P.Health.Health < P.Health.MaxHealth * 0.2f) return false;
            if (info.Type == DamageType.Fall || info.Type == DamageType.Vehicle) return false;
            _lastStand = Time.time;
            CombatDirector.Slow(0.8f, 0.3f);
            if (Game.Hud != null) Game.Hud.CombatText("Last stand", new Color(1f, 0.35f, 0.3f));
            Pad.Rumble(1f, 0.5f, 0.5f);
            return true;
        }

        /// <summary>Damage the player deals to the current lock target scales with level and gear (used by HUD previews).</summary>
        public bool HasRiposteTarget { get { return RiposteReady(); } }
    }
}
