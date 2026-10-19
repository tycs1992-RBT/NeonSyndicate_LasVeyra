using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Playing one attack: the wind-up (the pose's readable silhouette, a glow that ramps up, a cry, a red flash before
    /// an unblockable blow, a ground ring that fills for area attacks), the commit at the end of the wind-up (direction
    /// locked, leaps and dives launched, rings burst, projectiles thrown), the active window's hit test, and the
    /// recovery the player can punish. Damage goes through the player's dodge, guard and parry like any enemy blow.
    /// </summary>
    public sealed partial class Creature
    {
        private CreatureAttack[] _attacks = new CreatureAttack[0];
        private float[] _readyAt = new float[0];
        private int _atk = -1;
        private int _previousAttack04L = -1;
        private float _atkT;
        private AttackTiming _timing;
        private bool _atkHit;
        private bool _committed;
        private bool _flashed;
        private Vector3 _atkPoint;
        private Vector3 _atkDir = Vector3.forward;
        private float _atkSpeed;
        private float _tickTimer;
        private float _fxTick;
        private GroundRing _ring;
        private float _actionT = -1f;
        private float _staggerDur = 1f;
        private float _glow = 1f;

        /// <summary>An attack is winding up (the HUD shows a warning over the creature).</summary>
        public bool IsWindingUp { get { return _atk >= 0 && _atkT < _timing.Windup && _attacks[_atk].Shape != AttackShape.Call; } }
        /// <summary>-1 idle, 0 wind-up, 1 active, 2 recovery.</summary>
        public int AttackPhase { get { return _atk < 0 ? -1 : _timing.PhaseAt(_atkT); } }
        public string CurrentAttackName { get { return _atk >= 0 ? _attacks[_atk].Name : ""; } }
        /// <summary>0..1 through the current wind-up.</summary>
        public float WindupProgress { get { return IsWindingUp ? Mathf.Clamp01(_atkT / Mathf.Max(0.01f, _timing.Windup)) : 0f; } }
        /// <summary>The attack winding up cannot be blocked or parried (dodge it): the HUD warns in orange.</summary>
        public bool CurrentAttackUnblockable { get { return _atk >= 0 && (_attacks[_atk].Flags & DamageFlags.Unblockable) != 0; } }

        private void StartAttack(int idx)
        {
            if (idx < 0 || idx >= _attacks.Length || _dead) return;
            CreatureAttack a = _attacks[idx];
            _atk = idx;
            _previousAttack04L = idx;
            _atkT = 0f;
            _atkHit = false;
            _committed = false;
            _flashed = false;
            _tickTimer = 0f;
            _timing = CreatureAttackDb.Timing(a, CombatDirector.EnemyWindupScale, Size);
            if (Options.Vanguard04K)
                _timing=VanguardRules04K.Timing(_timing,Health.Health/Mathf.Max(1f,Health.MaxHealth));
            _readyAt[idx] = Time.time + a.Cooldown;
            _hasGoal = false;
            Vector3 fwd = MathUtil.DirFromYaw(_yaw);
            _atkDir = fwd;
            Color ringColor = RingColor(a);
            switch (a.Shape)
            {
                case AttackShape.Ring:
                    _atkPoint = transform.position + fwd * a.RingOffset;
                    _atkPoint.y = GroundY(_atkPoint.x, _atkPoint.z);
                    _ring = GroundRing.Show(_atkPoint, a.Reach, _timing.Windup, ringColor);
                    break;
                case AttackShape.Under:
                    // The body stays hidden under the sand through the wind-up; it shows when it bursts out (Commit).
                    _atkPoint = Target != null ? FlightMath.DiveAim(Target.transform.position, Target.Velocity, 0.35f) : transform.position;
                    _atkPoint.y = Target != null ? Target.transform.position.y : _atkPoint.y;
                    _ring = GroundRing.Show(_atkPoint, a.Reach, _timing.Windup, ringColor);
                    break;
            }
            CreatureCry cry = a.Shape == AttackShape.Call ? CreatureCry.Roar : (a.Shape == AttackShape.Under ? CreatureCry.Rumble : CreatureCry.Attack);
            CreatureAudio.Play(_sp, cry, HeadPosition, Size);
            if (a.Shape == AttackShape.Call) _nextCry = Time.time + UnityEngine.Random.Range(3f, 4.5f);
        }

        private static Color RingColor(CreatureAttack a)
        {
            if ((a.Flags & DamageFlags.Unblockable) != 0) return new Color(1f, 0.25f, 0.15f, 1f);
            return new Color(1f, 0.6f, 0.2f, 1f);
        }

        private void CancelAttack()
        {
            if (_atk < 0) return;
            _atk = -1;
            _atkT = 0f;
            if (_ring != null) { _ring.Release(); _ring = null; }
            if (_hasToken) { CombatDirector.ReleaseCreatureToken(this); _hasToken = false; }
            if (_hasRangedToken) { CombatDirector.ReleaseCreatureRanged(this); _hasRangedToken = false; }
        }

        private void FinishAttack()
        {
            CreatureAttack a = _attacks[_atk];
            _atk = -1;
            if (_ring != null) { _ring.Release(); _ring = null; }
            if (_hasToken) { CombatDirector.ReleaseCreatureToken(this); _hasToken = false; }
            if (_hasRangedToken) { CombatDirector.ReleaseCreatureRanged(this); _hasRangedToken = false; }
            if (a.Shape == AttackShape.Call)
            {
                _nextAttackAt = Mathf.Max(_nextAttackAt, Time.time + 0.4f);
                return;
            }
            _nextAttackAt = Time.time + CreatureBrainLogic.RestBetweenAttacks((int)Settings.Difficulty, UnityEngine.Random.value, (_sp != null ? _sp.Tempo : 1f) * (Elite ? 0.75f : 1f));
            if (Options.Vanguard04K) _nextAttackAt=Mathf.Max(_nextAttackAt,Time.time+1.1f);
            if (_flying && a.Use == AttackUse.Air) _airAttacks++;
            if (a.Shape == AttackShape.Under) Surface();
        }

        private void UpdateAttack(float dt)
        {
            CreatureAttack a = _attacks[_atk];
            float prev = _atkT;
            _atkT += dt;
            float w = _timing.Windup;
            // The glint: the glow ramps up through the wind-up and flashes as the blow lands.
            _glow = _atkT < w ? 1f + 2.6f * Mathf.Clamp01(_atkT / w) : Mathf.MoveTowards(_glow, 1f, dt * 6f);
            if (!_flashed && (a.Flags & DamageFlags.Unblockable) != 0 && _atkT >= w * 0.75f)
            {
                _flashed = true;
                if (Game.Fx != null) Game.Fx.Flash(ChestPosition + MathUtil.DirFromYaw(_yaw) * Radius, new Color(1f, 0.15f, 0.1f), 3f, 5f + Size, 0.3f);
            }
            if (a.Shape == AttackShape.Under) TrackEruption(a, dt);
            if (!_committed && _atkT >= w) Commit(a);
            if (_atkT >= w && prev < w + _timing.Active) ActiveTick(a, dt);
            if (_atkT >= _timing.Total) FinishAttack();
        }

        /// <summary>The end of the wind-up: direction locked, leaps and dives launched, rings burst, shots thrown.</summary>
        private void Commit(CreatureAttack a)
        {
            _committed = true;
            Vector3 fwd = MathUtil.DirFromYaw(_yaw);
            Vector3 tp = Target != null ? Target.transform.position : transform.position + fwd * 3f;
            Vector3 to = MathUtil.Flat(tp - transform.position);
            _atkDir = to.sqrMagnitude > 0.01f && Mathf.Abs(Vector3.Angle(fwd, to)) < 50f ? to.normalized : fwd;
            switch (a.Shape)
            {
                case AttackShape.Leap:
                {
                    float dist = Mathf.Max(0f, to.magnitude - Radius - 0.4f);
                    _atkSpeed = Mathf.Min(a.Speed, dist / Mathf.Max(0.05f, _timing.Active) + 1f);
                    break;
                }
                case AttackShape.Line:
                    _atkSpeed = a.Speed;
                    if (_flying && Target != null)
                    {
                        Vector3 aim = FlightMath.DiveAim(Target.ChestPosition, Target.Velocity, 0.3f);
                        Vector3 d = aim - ChestPosition;
                        _atkDir = d.sqrMagnitude > 0.01f ? d.normalized : fwd;
                    }
                    break;
                case AttackShape.Ring:
                case AttackShape.Under:
                    if (_ring != null) { _ring.Burst(); _ring = null; }
                    ImpactFx(_atkPoint, a.Reach);
                    if (a.Shape == AttackShape.Under)
                    {
                        // Out of the sand: from now on it can be seen and hit.
                        _burrowed = false;
                        if (CRig.Renderer != null) CRig.Renderer.enabled = true;
                        CRig.SetCollidersEnabled(true);
                    }
                    break;
                case AttackShape.Projectile:
                    Throw(a);
                    break;
                case AttackShape.Cone:
                    // Breath opens aimed at the target in three dimensions (a flyer breathes down at you), then sweeps.
                    if (Target != null) _atkDir = CreatureFightRules.ConeOpening(fwd, CRig.MouthPoint.position, Target.ChestPosition, 50f);
                    break;
                case AttackShape.Call:
                    if (Group != null && Target != null) Group.Alert(Target, this);
                    break;
            }
        }

        /// <summary>The wyrm travels under the ring during the first part of the wind-up so it bursts out right there.</summary>
        private void TrackEruption(CreatureAttack a, float dt)
        {
            if (_atkT > _timing.Windup * 0.75f) return;
            Vector3 p = transform.position;
            Vector3 to = MathUtil.Flat(_atkPoint - p);
            float d = to.magnitude;
            if (d < 0.1f) return;
            float speed = Mathf.Max(d / Mathf.Max(0.1f, _timing.Windup * 0.75f - _atkT), _sp != null ? _sp.RunSpeed : 10f);
            p += to / d * Mathf.Min(d, speed * dt);
            p.y = GroundY(p.x, p.z);
            transform.position = p;
            if (d > 0.5f) _yaw = Mathf.MoveTowardsAngle(_yaw, MathUtil.YawOf(to), 360f * dt);
            DustTrail(dt);
        }

        private void ActiveTick(CreatureAttack a, float dt)
        {
            CharacterBase t = Target;
            switch (a.Shape)
            {
                case AttackShape.Arc:
                    if (!_atkHit && t != null && InArc(t, a)) { _atkHit = true; Deal(t, a, 1f, transform.position); }
                    break;
                case AttackShape.Leap:
                case AttackShape.Line:
                    if (!_atkHit && t != null && Contact(t, a.Reach * PartMelee)) { _atkHit = true; Deal(t, a, 1f, transform.position); _atkSpeed *= 0.3f; }
                    break;
                case AttackShape.Ring:
                case AttackShape.Under:
                    if (!_atkHit && t != null && InRing(t, _atkPoint, a.Reach)) { _atkHit = true; Deal(t, a, 1f, _atkPoint); }
                    break;
                case AttackShape.Cone:
                    ConeTick(a, dt);
                    break;
            }
        }

        // ------------------------------------------------------------------ hit tests

        private Vector3 TargetPoint(CharacterBase t)
        {
            return t.InVehicle ? t.CurrentVehicle.transform.position + Vector3.up : t.transform.position;
        }

        private bool InArc(CharacterBase t, CreatureAttack a)
        {
            Vector3 tp = TargetPoint(t);
            float dy = tp.y - transform.position.y;
            if (dy < -1.5f || dy > Size + 1.2f) return false;
            Vector3 to = MathUtil.Flat(tp - transform.position);
            float edge = to.magnitude - Radius - Mathf.Max(0.3f, t.BodyRadius);
            if (edge > a.Reach * PartMelee) return false;
            if (a.Arc < 300f && to.sqrMagnitude > 0.04f && Vector3.Angle(MathUtil.DirFromYaw(_yaw), to) > a.Arc * 0.5f) return false;
            return !Physics.Linecast(ChestPosition, t.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore);
        }

        private bool Contact(CharacterBase t, float reach)
        {
            Vector3 tp = t.ChestPosition;
            Vector3 c = ChestPosition;
            float d = (tp - c).magnitude - Radius - Mathf.Max(0.3f, t.BodyRadius);
            return d <= reach;
        }

        private static bool InRing(CharacterBase t, Vector3 center, float radius)
        {
            Vector3 tp = t.InVehicle ? t.CurrentVehicle.transform.position : t.transform.position;
            if (Mathf.Abs(tp.y - center.y) > 2.6f) return false;
            return MathUtil.FlatDistance(tp, center) <= radius + 0.3f + Mathf.Max(0.3f, t.BodyRadius);
        }

        /// <summary>Breath and gaze: damage in ticks along a cone from the mouth, with particles pouring out every frame.</summary>
        private void ConeTick(CreatureAttack a, float dt)
        {
            Vector3 mouth = CRig.MouthPoint.position;
            Vector3 dir = CRig.MouthPoint.forward;
            if (Target != null)
            {
                // The cone sweeps after its target a little slower than a sprint carries you round it: outrun it sideways.
                Vector3 want = (Target.ChestPosition - mouth).normalized;
                _atkDir = Vector3.RotateTowards(_atkDir.sqrMagnitude > 0.5f ? _atkDir : dir, want, 0.85f * dt, 0f);
            }
            dir = _atkDir.sqrMagnitude > 0.5f ? _atkDir.normalized : dir;
            _fxTick -= dt;
            if (_fxTick <= 0f && Game.Fx != null)
            {
                _fxTick = 0.03f;
                bool fire = a.Element == Element.Ember;
                Color c = a.Element == Element.Void ? new Color(0.7f, 0.3f, 1f) : (fire ? new Color(1f, 0.55f, 0.2f) : Health.SurfaceTint);
                float spread = Mathf.Tan(a.Arc * 0.5f * Mathf.Deg2Rad);
                Vector3 jitter = new Vector3(UnityEngine.Random.Range(-spread, spread), UnityEngine.Random.Range(-spread, spread) * 0.5f, 0f);
                Vector3 v = (dir + Quaternion.LookRotation(dir) * jitter).normalized * (a.Reach * 1.4f);
                Game.Fx.BreathPuff(mouth + dir * 0.3f, v, c, a.HurtsStillOnly ? 0.4f : 0.9f + Size * 0.1f, fire);
            }
            _tickTimer -= dt;
            if (_tickTimer > 0f || Target == null) return;
            _tickTimer = 0.25f;
            CharacterBase t = Target;
            Vector3 to = t.ChestPosition - mouth;
            float d = to.magnitude;
            if (d > a.Reach || Vector3.Angle(dir, to) > a.Arc * 0.5f + 4f) return;
            if (Physics.Linecast(mouth, t.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return;
            if (a.HurtsStillOnly)
            {
                float speed = t.IsPlayer && !t.InVehicle ? Game.Player.FlatVelocity.magnitude : t.Velocity.magnitude;
                if (speed > 1.4f) return;
                if (t.IsPlayer && Game.Hud != null) Game.Hud.CombatText("Crown gaze: keep moving", new Color(0.8f, 0.5f, 1f));
            }
            Deal(t, a, 1f, mouth);
        }

        private void Throw(CreatureAttack a)
        {
            if (Target == null) return;
            Vector3 from = CRig.MouthPoint.position;
            Vector3 aim = FlightMath.DiveAim(Target.ChestPosition, Target.Velocity, Vector3.Distance(from, Target.ChestPosition) / Mathf.Max(5f, a.Speed));
            Vector3 d = aim - from;
            float flat = MathUtil.Flat(d).magnitude;
            float time = flat / Mathf.Max(5f, a.Speed);
            const float gravity = 9f;
            Vector3 vel = MathUtil.Flat(d) / Mathf.Max(0.05f, time);
            vel.y = d.y / Mathf.Max(0.05f, time) + 0.5f * gravity * time;
            Color c = a.Element == Element.Ember ? new Color(1f, 0.5f, 0.15f) : Health.SurfaceTint;
            float dmg = _baseDamage * a.Damage;
            if (Target.IsPlayer) dmg = CreatureBrainLogic.CapHit(dmg, Target.Health.MaxHealth, (int)Settings.Difficulty, CombatDirector.PlayerDamageTaken);
            CreatureShot.Fire(from, vel, gravity, dmg, 0.35f + Size * 0.05f, a.Type, a.Element, c, gameObject, a.Flags, 1.8f);
        }

        // ------------------------------------------------------------------ dealing damage

        private void Deal(CharacterBase t, CreatureAttack a, float mult, Vector3 from)
        {
            if (t == null || t.IsDead) return;
            // The only practice species is the jackal: its damaging bite and pounce both use this path.
            // Entering a vehicle ends practice; its pending strike must not damage that vehicle first.
            if (Options != null && Options.PracticeTarget && t.InVehicle) return;
            float raw = _baseDamage * a.Damage * mult;
            // Broken claws, fangs or talons halve melee damage (CreatureParts).
            if (IsMeleeShape(a)) raw *= PartMelee;
            if (t.InVehicle)
            {
                Vehicle v = t.CurrentVehicle;
                if (v != null)
                {
                    v.ApplyDamage(raw * 2.5f, gameObject);
                    if (v.Body != null && !v.Body.isKinematic)
                    {
                        Vector3 shove = MathUtil.Flat(v.transform.position - from).normalized * a.Force * 0.8f + Vector3.up * a.Force * (a.Shape == AttackShape.Under ? 0.8f : 0.2f);
                        v.Body.AddForce(shove, ForceMode.VelocityChange);
                    }
                }
                ImpactFx(TargetPoint(t), 1.5f);
                return;
            }
            if (t.IsPlayer) raw = CreatureBrainLogic.CapHit(raw, t.Health.MaxHealth, (int)Settings.Difficulty, CombatDirector.PlayerDamageTaken);
            Vector3 dir = MathUtil.Flat(t.transform.position - from);
            if (dir.sqrMagnitude < 1e-4f) dir = MathUtil.DirFromYaw(_yaw);
            DamageInfo info = DamageInfo.Make(raw, t.ChestPosition, dir, gameObject, a.Type);
            info.Force = a.Force;
            info.Flags = a.Flags;
            info.Poise = raw * 0.5f;
            if (!DealPracticeHit(t, info)) t.Health.TakeDamage(info);
            if (t.Health.LastBlocked) return;
            if (t.IsPlayer)
            {
                PlayerController p = Game.Player;
                if (a.DrainsEnergy && Game.Abilities != null && !PartBroken(PartEffect.MeleeHalf)) Game.Abilities.Energy = Mathf.Max(0f, Game.Abilities.Energy - 10f);
                if (a.DrainsStamina && p != null && p.Combat != null) p.Combat.SpendStamina(a.HurtsStillOnly ? 8f : 18f);
                if (a.Element == Element.Ember && a.Type != DamageType.Fire) t.Health.Burning = Mathf.Max(t.Health.Burning, 1f);
                if (Game.Cam != null && (a.Flags & DamageFlags.Knockdown) != 0) Game.Cam.Shake(Mathf.Clamp(0.15f + Size * 0.05f, 0.15f, 0.45f));
            }
            if (Game.Fx != null && a.Shape != AttackShape.Cone) Game.Fx.CreatureHit(t.ChestPosition, -dir, ImpactSurface.Crystal, Health.SurfaceTint, 4);
        }

        /// <summary>Slams and eruptions: a burst of sand, snow or dust and crystal sparks, a thud and a camera shake.</summary>
        private void ImpactFx(Vector3 at, float radius)
        {
            if (Game.Fx != null)
            {
                int n = Mathf.Clamp(Mathf.RoundToInt(radius * 4f), 6, 30);
                for (int i = 0; i < 6; i++)
                {
                    float ang = i / 6f * Mathf.PI * 2f;
                    Vector3 p = at + new Vector3(Mathf.Cos(ang), 0.1f, Mathf.Sin(ang)) * radius * 0.7f;
                    Game.Fx.GroundDust(p, Mathf.Clamp(radius * 0.25f, 0.4f, 2f), n / 6 + 1);
                }
                Game.Fx.CrystalBurst(at + Vector3.up * 0.3f, Health.SurfaceTint, n, radius * 0.5f);
            }
            CreatureAudio.Play(_sp, CreatureCry.Slam, at, Size);
            if (Game.Cam != null)
            {
                float d = Vector3.Distance(Game.Cam.transform.position, at);
                Game.Cam.Shake(Mathf.Clamp01((0.25f + Size * 0.08f) * (1f - d / (25f + radius * 4f))));
            }
        }
    }
}
