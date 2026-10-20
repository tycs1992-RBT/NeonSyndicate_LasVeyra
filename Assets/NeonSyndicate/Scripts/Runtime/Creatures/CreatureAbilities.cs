using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Abilities of the humanoid creatures, on top of their melee swings: the Hollow's blink to the player's blind side
    /// (a shimmer warns first), the Snow Hollow's chilling touch that drains stamina, the Mirror Wraith's frontal glass
    /// guard (light hits from the front glance off until a heavy blow or a hit from the flank breaks it), the golems'
    /// slam shockwave (a ground ring fills, then bursts) and their thrown shards or packed sand. Also each creature's
    /// damage rules (weaknesses, resistances, the skill tree's creature damage) and the heavy footfalls of big bodies.
    /// Bosses drive the same slam and throw through CreatureBoss.
    /// </summary>
    public sealed partial class CreatureAbilityKit : MonoBehaviour, IDamageFilter
    {
        public NpcController Npc;
        public CreatureSpecies Species;
        public HumanoidCreature Kind;
        /// <summary>Boss phases scale the damage the creature takes (a kneeling Titan, the Foreman's broken plates).</summary>
        public float DamageTakenScale = 1f;
        /// <summary>The Mirror Wraith's guard: up, broken until this time, and the group that shares it (the Queen's copies).</summary>
        public bool HasGuard;
        public float GuardBrokenUntil;
        public List<CreatureAbilityKit> GuardGroup;
        /// <summary>Abilities stop while a boss script plays its own moves.</summary>
        public bool ScriptedOnly;

        private enum Ability { None, Blink, Slam, Throw }

        private Ability _ability;
        private float _t;
        private float _windup;
        private float _total;
        private bool _fired;
        private float _next;
        private float _nextThrow;
        private Vector3 _blinkTo;
        private GroundRing _ring;
        private Vector3 _ringCenter;
        private float _ringRadius;
        private float _slamDamage = 1.4f;
        private float _fxTimer;
        private float _lastDeflectText;
        private float _stepPhase = -1f;

        public void Init(NpcController n, CreatureSpecies sp)
        {
            Npc = n;
            Species = sp;
            Kind = sp != null ? sp.Humanoid : HumanoidCreature.None;
            HasGuard = Kind == HumanoidCreature.MirrorWraith || Kind == HumanoidCreature.WraithQueen;
            _next = Time.time + Random.Range(3f, 6f);
            _nextThrow = Time.time + Random.Range(2f, 5f);
            if (n != null && n.Health != null)
            {
                // Burning goes through these damage rules too (resistances, the Foreman's plates).
                n.Health.FilterBurns = true;
                // A golem's head is stone: its crits come from the chest core (FilterDamage), never from the height test.
                if (IsGolem) n.Health.HeadHeight = 9999f;
            }
        }

        private bool IsGolem { get { return Kind == HumanoidCreature.ShardGolem || Kind == HumanoidCreature.SandGolem || Kind == HumanoidCreature.GlacierTitan || Kind == HumanoidCreature.GolemForeman; } }
        public bool Busy { get { return _ability != Ability.None; } }
        public bool GuardUp { get { return HasGuard && Time.time >= GuardBrokenUntil; } }
        /// <summary>A slam or a throw is winding up (the HUD warns over the creature).</summary>
        public bool WindingUp { get { return (_ability == Ability.Slam || _ability == Ability.Throw) && !_fired; } }
        /// <summary>0..1 through the current wind-up.</summary>
        public float WindupProgress { get { return WindingUp ? Mathf.Clamp01(_t / Mathf.Max(0.01f, _windup)) : 0f; } }
        /// <summary>The move winding up cannot be blocked (the slam's shockwave): the HUD shows an orange "!!".</summary>
        public bool WindupUnblockable { get { return _ability == Ability.Slam && !_fired; } }

        // ================================================================== damage rules

        public bool FilterDamage(ref DamageInfo info)
        {
            if (Npc == null || Npc.IsDead) return true;
            if (Npc.CreatureMirage)
            {
                // A mirage pops at the first touch.
                info.Amount = Npc.Health.Health + 10f;
                return true;
            }
            if (GuardUp && Frontal(info) && (info.Type == DamageType.Melee || info.Type == DamageType.Bullet))
            {
                bool heavy = (info.Flags & (DamageFlags.Heavy | DamageFlags.Riposte)) != 0;
                if (!heavy)
                {
                    Deflect(info);
                    return false;
                }
                BreakGuard(true);
            }
            FilterWard04OG(ref info);
            float mult = CreatureBrainLogic.ElementMultiplier(ElementOf(info), Species != null ? Species.Weakness : Element.None, Species != null ? Species.Resists : Element.None);
            if (info.FromPlayer) mult *= Mathf.Max(0f, SkillMods.CreatureDamage);
            if (IsGolem && (info.Type == DamageType.Bullet || info.Type == DamageType.Power) && Npc.Rig != null)
            {
                // The glowing chest core is the golem's weak point (the bestiary says so): shots and powers that hit it
                // are criticals. A bullet's critical is doubled by the health component (HeadshotMultiplier).
                bool core = CreatureFightRules.OnCore(info.Point, Npc.ChestPosition, Npc.Rig.Scale);
                info.Headshot = core;
                if (core && info.Type == DamageType.Power) mult *= 1.5f;
            }
            info.Amount *= mult * DamageTakenScale;
            return true;
        }

        private static Element ElementOf(DamageInfo info)
        {
            if (info.Type == DamageType.Fire) return Element.Ember;
            if (info.Type == DamageType.Power) return Element.Volt;
            if (info.Type == DamageType.Melee && info.FromPlayer && Game.Gear != null) return Game.Gear.WeaponElement;
            return Element.None;
        }

        private bool Frontal(DamageInfo info)
        {
            Vector3 from = -info.Direction;
            if (info.Source != null) from = info.Source.transform.position - Npc.transform.position;
            from.y = 0f;
            if (from.sqrMagnitude < 0.01f) return true;
            return Vector3.Angle(Npc.transform.forward, from) < 70f;
        }

        private void Deflect(DamageInfo info)
        {
            if (Game.Fx != null) Game.Fx.CreatureHit(info.Point, -info.Direction, ImpactSurface.Glass, Npc.Health.SurfaceTint, 10);
            if (Game.Audio != null) Game.Audio.PlayVar(info.Type == DamageType.Melee ? SfxId.MetalHit : SfxId.Ricochet, info.Point, 0.6f, 0.1f, 30f);
            if (info.FromPlayer && Game.Hud != null && Time.time - _lastDeflectText > 1.2f)
            {
                _lastDeflectText = Time.time;
                Game.Hud.CombatText("Glass guard: flank it or use a heavy", new Color(0.75f, 0.95f, 1f));
            }
        }

        /// <summary>Breaks the glass guard (and every guard that shares it) for a few seconds.</summary>
        public void BreakGuard(bool announce)
        {
            if (GuardGroup != null)
            {
                for (int i = 0; i < GuardGroup.Count; i++) if (GuardGroup[i] != null) GuardGroup[i].CrackOwnGuard();
            }
            else CrackOwnGuard();
            if (announce && Game.Hud != null) Game.Hud.CombatText(GuardGroup != null ? "Every mirror cracks" : "Guard broken", new Color(0.75f, 0.95f, 1f));
        }

        private bool PartBroken(PartEffect e)
        {
            return Npc != null && Npc.Health != null && Npc.Health.Parts != null && Npc.Health.Parts.Has(e);
        }

        /// <summary>A broken Glass Mantle (CreatureParts): this creature's own guard never comes back.</summary>
        public void ShatterGuardForGood()
        {
            HasGuard = false;
            GuardBrokenUntil = float.MaxValue;
            if (Npc != null && Game.Fx != null) Game.Fx.GlassBurst(Npc.ChestPosition, Npc.Health.SurfaceTint, 30);
        }

        private void CrackOwnGuard()
        {
            if (Npc == null || Npc.IsDead) return;
            GuardBrokenUntil = Time.time + 7f;
            Npc.CreatureStagger(1.3f);
            if (Game.Fx != null) Game.Fx.GlassBurst(Npc.ChestPosition, Npc.Health.SurfaceTint, 24);
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.GlassBreak, Npc.ChestPosition, 0.8f, 0.1f, 40f);
        }

        /// <summary>A Snow Hollow's touch chills the player's stamina; Hollows leave a wisp of smoke.</summary>
        public void OnMeleeLanded(CharacterBase target)
        {
            if (target == null || !target.IsPlayer || target.Health.LastBlocked) return;
            if (Kind == HumanoidCreature.SnowHollow && !PartBroken(PartEffect.NoAbility))
            {
                PlayerController p = Game.Player;
                if (p != null && p.Combat != null) p.Combat.SpendStamina(22f);
                if (Game.Fx != null) Game.Fx.SnowBurst(target.ChestPosition, 0.5f, 10);
                if (Game.Hud != null) Game.Hud.CombatText("Chilled", new Color(0.65f, 0.9f, 1f));
            }
        }

        // ================================================================== abilities

        private void Update()
        {
            if (Npc == null || Npc.IsDead){ClearWard04OG();return;}
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            TickWard04OG(dt);
            HeavySteps();
            if (_ability != Ability.None)
            {
                // A stagger (a broken poise, the Foreman's stunned crown, the Titan's kneel) or a fall breaks the move.
                if (Npc.IsStaggered || Npc.IsRagdolled)
                {
                    Cancel();
                    return;
                }
                UpdateAbility(dt);
                return;
            }
            if (ScriptedOnly || !Npc.CreatureEngaged || Npc.IsStaggered || Npc.CreatureAttacking || Npc.IsRagdolled) return;
            if(Npc.CreatureOptions!=null && Npc.CreatureOptions.FrontierFormation04OG!=null && !Npc.CreatureOptions.FrontierFormation04OG.CanBegin(Npc.CreatureOptions.FrontierSlot04OG))return;
            CharacterBase t = Npc.Target;
            float d = MathUtil.FlatDistance(t.transform.position, Npc.transform.position);
            float s = Mathf.Max(0.5f, Npc.Rig.Scale);
            float now = Time.time;
            switch (Kind)
            {
                case HumanoidCreature.Hollow:
                case HumanoidCreature.SnowHollow:
                    if (Kind == HumanoidCreature.Hollow && now >= _next && d > 3f && d < 15f && !PartBroken(PartEffect.NoAbility)) StartBlink();
                    break;
                case HumanoidCreature.ShardGolem:
                case HumanoidCreature.SandGolem:
                    if (now >= _next && d < 5.5f * s * 0.75f) StartSlam(1f, 1.1f, 1.4f);
                    else if (now >= _nextThrow && d > 7f && d < 24f && !PartBroken(PartEffect.NoRanged)) StartThrow();
                    break;
            }
        }

        /// <summary>Hollow blink: a shimmer, then it steps out of the dark behind the player and swings.</summary>
        public bool StartBlink()
        {
            if (Npc == null || Npc.Target == null || _ability != Ability.None) return false;
            Vector3 tp = Npc.Target.transform.position;
            Vector3 look = Game.Cam != null && Npc.Target.IsPlayer ? Game.Cam.FlatForward : Npc.Target.transform.forward;
            Vector3 side = MathUtil.RightOf(look) * (Random.value < 0.5f ? -1f : 1f);
            Vector3 want = tp - look * 2.4f + side * 1.1f;
            RaycastHit hit;
            if (!Physics.Raycast(want + Vector3.up * 2f, Vector3.down, out hit, 5f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return false;
            want = hit.point;
            if (Game.Layout != null && Game.Layout.IsInsideBuilding(want, 0.4f)) return false;
            if (Physics.Linecast(Npc.Target.ChestPosition, want + Vector3.up * 1.2f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return false;
            _blinkTo = want;
            Begin(Ability.Blink, 0.65f, 0.95f);
            CreatureAudio.Play(Species, CreatureCry.Attack, Npc.ChestPosition, Npc.BodyHeight);
            return true;
        }

        /// <summary>Golem slam: arms raised while a ring fills around it, then a shockwave that knocks you flat.</summary>
        public bool StartSlam(float radiusScale, float windup, float damage)
        {
            if (Npc == null || _ability != Ability.None) return false;
            float s = Mathf.Max(0.5f, Npc.Rig.Scale);
            _ringRadius = 3f * s * Mathf.Max(0.3f, radiusScale);
            _ringCenter = Npc.transform.position + Npc.transform.forward * (0.6f * s);
            _slamDamage = damage;
            Begin(Ability.Slam, windup, windup + 0.9f);
            _ring = GroundRing.Show(_ringCenter, _ringRadius, _windup, new Color(1f, 0.3f, 0.15f, 1f));
            CreatureAudio.Play(Species, CreatureCry.Attack, Npc.ChestPosition, Npc.BodyHeight);
            return true;
        }

        /// <summary>Thrown crystal shards (Shard Golem, Foreman) or packed sand (Sand Golem, Djinn).</summary>
        public bool StartThrow()
        {
            if (Npc == null || Npc.Target == null || _ability != Ability.None) return false;
            Begin(Ability.Throw, 0.9f, 1.3f);
            return true;
        }

        /// <summary>
        /// Stops the ability playing now (a stagger broke it): a ground ring that has not burst fades without hurting
        /// anyone, the pose lets go of the slam or the throw, and the move cools down before it can come again.
        /// </summary>
        public void Cancel()
        {
            ClearWard04OG();
            if (_ability == Ability.None) return;
            HumanoidAnimator anim = Npc != null ? Npc.Anim : null;
            if (anim != null)
            {
                if (_ability == Ability.Slam) anim.AttackT = -1f;
                if (_ability == Ability.Throw) anim.ThrowT = -1f;
                anim.Telegraph = 0f;
            }
            if (_ring != null) { _ring.Release(); _ring = null; }
            _next = Time.time + Random.Range(3f, 5f);
            if (_ability == Ability.Throw) _nextThrow = Time.time + Random.Range(4f, 6f);
            _ability = Ability.None;
            _fired = true;
            if (Npc != null) Npc.CreatureBusyUntil = Mathf.Min(Npc.CreatureBusyUntil, Time.time);
        }

        private void OnDisable(){Cancel();}

        private void Begin(Ability a, float windup, float total)
        {
            _ability = a;
            _t = 0f;
            _fired = false;
            _windup = Mathf.Max(a == Ability.Slam ? CreatureAttackDb.MinHeavyWindup : CreatureAttackDb.MinWindup, windup * CombatDirector.EnemyWindupScale * Mathf.Clamp(Npc.Rig.Layout.TimeScale, 1f, 1.6f));
            _total = _windup + Mathf.Max(0.2f, total - windup);
            if(Npc.CreatureOptions!=null && Npc.CreatureOptions.FrontierFormation04OG!=null)Npc.CreatureOptions.FrontierFormation04OG.Began(Npc.CreatureOptions.FrontierSlot04OG,_windup+.3f);
            Npc.CreatureBusyUntil = Time.time + _total + 0.1f;
            Npc.CreatureCancelAttack();
        }

        private void UpdateAbility(float dt)
        {
            _t += dt;
            HumanoidAnimator anim = Npc.Anim;
            float k = Mathf.Clamp01(_t / Mathf.Max(0.01f, _windup));
            if (!_fired) anim.Telegraph = k;
            else anim.Telegraph = Mathf.MoveTowards(anim.Telegraph, 0f, dt * 4f);
            switch (_ability)
            {
                case Ability.Blink:
                    _fxTimer -= dt;
                    if (_fxTimer <= 0f && Game.Fx != null && !_fired)
                    {
                        _fxTimer = 0.06f;
                        Game.Fx.ShadowBurst(Npc.ChestPosition + Random.insideUnitSphere * 0.5f, Npc.Health.SurfaceTint, 2, 0.5f);
                    }
                    if (!_fired && _t >= _windup)
                    {
                        _fired = true;
                        if (Game.Fx != null) Game.Fx.ShadowBurst(Npc.ChestPosition, Npc.Health.SurfaceTint, 14, 1f);
                        Vector3 to = MathUtil.Flat(Npc.Target.transform.position - _blinkTo);
                        Npc.CreatureTeleport(_blinkTo, to.sqrMagnitude > 0.01f ? MathUtil.YawOf(to) : Npc.transform.eulerAngles.y);
                        if (Game.Fx != null) Game.Fx.ShadowBurst(Npc.ChestPosition, Npc.Health.SurfaceTint, 14, 1f);
                        if (Game.Audio != null) Game.Audio.PlayAt(SfxId.Whoosh, _blinkTo, 0.6f, 0.8f, 30f);
                        _total = _t + 0.05f;
                        Npc.CreatureBusyUntil = Time.time + 0.05f;
                        Npc.CreatureAttackSoon();
                    }
                    break;
                case Ability.Slam:
                    anim.AttackAnim = MeleeAnim.Slam;
                    anim.AttackWindup = Mathf.Clamp01(_windup / _total);
                    anim.AttackActive = 0.12f;
                    anim.AttackT = Mathf.Clamp01(_t / _total);
                    if (!_fired && _t >= _windup)
                    {
                        _fired = true;
                        if (_ring != null) { _ring.Burst(); _ring = null; }
                        Shockwave();
                    }
                    break;
                case Ability.Throw:
                    anim.ThrowT = Mathf.Clamp01(_t / _total);
                    if (!_fired && _t >= _windup)
                    {
                        _fired = true;
                        Throw();
                    }
                    break;
            }
            if (_t >= _total)
            {
                if (_ability == Ability.Slam) anim.AttackT = -1f;
                if (_ability == Ability.Throw) anim.ThrowT = -1f;
                anim.Telegraph = 0f;
                if (_ring != null) { _ring.Release(); _ring = null; }
                _next = Time.time + (Kind == HumanoidCreature.Hollow ? Random.Range(7f, 11f) : Random.Range(8f, 12f));
                if (_ability == Ability.Throw) _nextThrow = Time.time + Random.Range(6f, 9f);
                _ability = Ability.None;
            }
        }

        private void Shockwave()
        {
            float s = Mathf.Max(0.5f, Npc.Rig.Scale);
            if (Game.Fx != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    float a = i / 8f * Mathf.PI * 2f;
                    Game.Fx.GroundDust(_ringCenter + new Vector3(Mathf.Cos(a), 0.1f, Mathf.Sin(a)) * _ringRadius * 0.7f, Mathf.Clamp(s * 0.5f, 0.5f, 2f), 3);
                }
                if (Kind == HumanoidCreature.SandGolem) Game.Fx.SandBurst(_ringCenter, s, 14);
                else if (Kind == HumanoidCreature.GlacierTitan) Game.Fx.SnowBurst(_ringCenter, s, 16);
                else Game.Fx.RockBurst(_ringCenter, Npc.Health.SurfaceTint, 10, s * 0.6f);
            }
            CreatureAudio.Play(Species, CreatureCry.Slam, _ringCenter, Npc.BodyHeight);
            if (Game.Cam != null)
            {
                float cd = Vector3.Distance(Game.Cam.transform.position, _ringCenter);
                Game.Cam.Shake(Mathf.Clamp01(0.5f * (1f - cd / (30f + _ringRadius * 3f))));
            }
            PlayerController p = Game.Player;
            if (p == null || p.IsDead) return;
            Vector3 pp = p.InVehicle ? p.CurrentVehicle.transform.position : p.transform.position;
            if (Mathf.Abs(pp.y - _ringCenter.y) > 2.5f || MathUtil.FlatDistance(pp, _ringCenter) > _ringRadius + 0.4f) return;
            if (p.InVehicle)
            {
                p.CurrentVehicle.ApplyDamage(Species.Damage * 3f, Npc.gameObject);
                return;
            }
            float raw = CreatureBrainLogic.CapHit(Species.Damage * _slamDamage * (Npc.CreatureOptions != null ? Npc.CreatureOptions.DamageScale : 1f), p.Health.MaxHealth, (int)Settings.Difficulty, CombatDirector.PlayerDamageTaken);
            DamageInfo info = DamageInfo.Make(raw, p.ChestPosition, MathUtil.Flat(pp - _ringCenter), Npc.gameObject, DamageType.Melee);
            info.Force = 9f;
            info.Flags = DamageFlags.Knockdown | DamageFlags.Unblockable | DamageFlags.Heavy;
            p.Health.TakeDamage(info);
        }

        private void Throw()
        {
            CharacterBase t = Npc.Target;
            if (t == null) return;
            Vector3 from = Npc.Rig != null && Npc.Rig.HandR != null ? Npc.Rig.HandR.position : Npc.ChestPosition + Npc.transform.forward;
            Vector3 aim = FlightMath.DiveAim(t.ChestPosition, t.Velocity, 0.5f);
            Vector3 d = aim - from;
            float flat = MathUtil.Flat(d).magnitude;
            float speed = 20f;
            float time = flat / speed;
            const float gravity = 9f;
            Vector3 vel = MathUtil.Flat(d) / Mathf.Max(0.05f, time);
            vel.y = d.y / Mathf.Max(0.05f, time) + 0.5f * gravity * time;
            bool sand = Kind == HumanoidCreature.SandGolem || Kind == HumanoidCreature.MirageDjinn;
            Color c = sand ? new Color(1f, 0.75f, 0.4f) : Npc.Health.SurfaceTint;
            float raw = Species.Damage * 0.8f * (Npc.CreatureOptions != null ? Npc.CreatureOptions.DamageScale : 1f);
            if (t.IsPlayer) raw = CreatureBrainLogic.CapHit(raw, t.Health.MaxHealth, (int)Settings.Difficulty, CombatDirector.PlayerDamageTaken);
            CreatureShot.Fire(from, vel, gravity, raw, 0.3f, DamageType.Bullet, sand ? Element.None : Element.Volt, c, Npc.gameObject, DamageFlags.None, 2.2f);
            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.Throw, from, 0.7f, 0.7f, 40f);
        }

        /// <summary>Golems and the Titan shake the ground with every step.</summary>
        private void HeavySteps()
        {
            if (!IsGolem || Npc.Anim == null || Npc.Anim.Pose == null || Npc.Rig.Layout.Floating) return;
            float ph = Npc.Anim.Pose.Phase;
            float prev = _stepPhase;
            _stepPhase = ph;
            if (prev < 0f || Npc.Velocity.sqrMagnitude < 0.3f) return;
            bool step = (prev > 0.9f && ph < 0.1f) || (prev < 0.5f && ph >= 0.5f);
            if (!step) return;
            float camD = Game.Cam != null ? Vector3.Distance(Game.Cam.transform.position, Npc.transform.position) : 99f;
            if (camD > 45f) return;
            float s = Npc.Rig.Scale;
            Transform foot = ph < 0.5f ? Npc.Rig.FootL : Npc.Rig.FootR;
            Vector3 at = foot != null ? foot.position : Npc.transform.position;
            if (Game.Fx != null) Game.Fx.GroundDust(at, Mathf.Clamp(s * 0.3f, 0.3f, 1.2f), 2);
            CreatureAudio.Play(Species, CreatureCry.Step, at, s * 1.4f);
            if (s > 2f && Game.Cam != null && camD < 25f) Game.Cam.Shake(0.08f * (1f - camD / 25f));
        }
    }

    /// <summary>
    /// How a humanoid creature leaves the world: a Hollow bursts into smoke, a golem crumbles into rock and dust, a
    /// wraith shatters into glass, and bosses get a short death (the body shakes and flares for a moment, the world
    /// slows, then it bursts). The body is hidden and removed; there is never a ragdoll.
    /// </summary>
    public sealed class CreatureDeathFx : MonoBehaviour
    {
        private NpcController _npc;
        private CreatureDeath _style;
        private Color _tint;
        private float _t;
        private float _burstAt;
        private bool _burst;
        private float _fx;

        public static void Begin(NpcController n, CreatureDeath style, Color tint)
        {
            if (n == null) return;
            CreatureDeathFx d = n.gameObject.AddComponent<CreatureDeathFx>();
            d._npc = n;
            d._style = style;
            d._tint = tint;
            d._burstAt = style == CreatureDeath.BossBurst ? 2.2f : 0.15f;
            if (style == CreatureDeath.BossBurst)
            {
                CombatDirector.Slow(0.9f, 0.4f);
                if (Game.Cam != null) Game.Cam.Shake(0.35f);
            }
        }

        private void Update()
        {
            if (_npc == null) return;
            float dt = Time.deltaTime;
            _t += dt;
            HumanoidAnimator a = _npc.Anim;
            if (!_burst)
            {
                if (_style == CreatureDeath.BossBurst && a != null)
                {
                    // Reeling and flaring before the end.
                    a.StaggerT = Mathf.Repeat(_t * 1.6f, 1f);
                    a.Telegraph = Mathf.Clamp01(_t / _burstAt) * 1.5f;
                    _fx -= dt;
                    if (_fx <= 0f && Game.Fx != null)
                    {
                        _fx = 0.08f;
                        Transform b = _npc.Rig.Bones[Random.Range(0, _npc.Rig.Bones.Length)];
                        Emit(b.position, 0.4f);
                    }
                    if (_npc.Rig != null) _npc.Rig.HitFlash(0.3f + 0.3f * Mathf.Sin(_t * 20f));
                }
                if (_t >= _burstAt) Burst();
                return;
            }
            if (_t > _burstAt + 1.4f) Destroy(_npc.gameObject);
        }

        private void Emit(Vector3 at, float k)
        {
            float s = _npc.Rig != null ? _npc.Rig.Scale : 1f;
            switch (_style)
            {
                case CreatureDeath.Smoke: Game.Fx.ShadowBurst(at, _tint, Mathf.RoundToInt(6 * k + 2), 0.5f * s); break;
                case CreatureDeath.Crumble: Game.Fx.RockBurst(at, _tint, Mathf.RoundToInt(8 * k + 2), 0.6f * s); break;
                case CreatureDeath.Shatter: Game.Fx.GlassBurst(at, _tint, Mathf.RoundToInt(12 * k + 3)); break;
                default:
                    if (_npc.Health.Surface == ImpactSurface.Shadow) Game.Fx.ShadowBurst(at, _tint, Mathf.RoundToInt(6 * k + 2), 0.6f * s);
                    else if (_npc.Health.Surface == ImpactSurface.Glass) Game.Fx.GlassBurst(at, _tint, Mathf.RoundToInt(12 * k + 3));
                    else Game.Fx.RockBurst(at, _tint, Mathf.RoundToInt(8 * k + 2), 0.8f * s);
                    break;
            }
        }

        private void Burst()
        {
            _burst = true;
            if (Game.Fx != null && _npc.Rig != null)
            {
                for (int i = 0; i < _npc.Rig.Bones.Length; i += 2) Emit(_npc.Rig.Bones[i].position, 1f);
                if (_style == CreatureDeath.BossBurst) Game.Fx.Flash(_npc.ChestPosition, _tint, 6f, 18f, 0.6f);
            }
            if (Game.Audio != null)
            {
                if (_style == CreatureDeath.Shatter || _npc.Health.Surface == ImpactSurface.Glass) Game.Audio.PlayVar(SfxId.GlassBreak, _npc.ChestPosition, 0.9f, 0.1f, 50f);
                else if (_style == CreatureDeath.Crumble) Game.Audio.PlayVar(SfxId.CrashHeavy, _npc.ChestPosition, 0.7f, 0.1f, 50f);
            }
            if (_style == CreatureDeath.BossBurst && Game.Cam != null) Game.Cam.Shake(0.6f);
            if (_npc.Rig != null) _npc.Rig.SetVisible(false, false);
            if (_npc.Anim != null) _npc.Anim.Telegraph = 0f;
        }
    }
}
