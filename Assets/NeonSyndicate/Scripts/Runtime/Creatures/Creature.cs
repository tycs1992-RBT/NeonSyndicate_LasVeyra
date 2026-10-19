using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>What a creature's brain is doing (attacks and staggers run on top of it).</summary>
    public enum CreatureMode
    {
        Idle,
        Wander,
        /// <summary>Territorial threat display: it has seen you and wants you gone.</summary>
        Warn,
        /// <summary>Stalkers creep toward your blind side before they strike.</summary>
        Stalk,
        /// <summary>Chasing and fighting its target.</summary>
        Hunt,
        Flee,
        /// <summary>Pulled too far from home: it goes back and heals.</summary>
        Return,
        Dead
    }

    /// <summary>
    /// A non-humanoid creature (jackals, stalkers, basilisks, wyverns, griffins, the drake, the wyrm, shades and mites):
    /// a CharacterBase with health, Team.Creature, poise and stagger, kinematic movement without a NavMesh (feelers for
    /// obstacles, cached ground probes), flight, burrowing, swarming and stealth, the temperament-driven brain and the
    /// species' attacks with readable wind-ups. Deaths play a death pose and dissolve into crystal sparks or shadow
    /// smoke (no ragdoll). Split into Creature.Brain.cs, Creature.Attacks.cs and Creature.Motion.cs.
    /// Humanoid creatures (Hollows, golems, wraiths, the Djinn) are NpcControllers in creature mode instead.
    /// </summary>
    public sealed partial class Creature : CharacterBase, IDamageFilter
    {
        /// <summary>Every live, built creature (non-humanoid). Combat targeting, the HUD and the minimap iterate it.</summary>
        public static readonly List<Creature> All = new List<Creature>();

        public CreatureSpecies Species;
        public CreatureRig CRig;
        public CreatureSpawnOptions Options;
        public string Tag;
        public bool Persistent;
        public bool Elite;
        /// <summary>Spawned by the ecology (counts toward the ambient caps, despawns when far).</summary>
        public bool Ambient;
        /// <summary>An Inner World being: it fades when the Veil closes.</summary>
        public bool InnerOnly;
        public CreatureGroup Group;
        public Vector3 Home;
        public float Poise;
        public float MaxPoise = 40f;
        /// <summary>Height of the body at rest (metres): scales turn rates, timings and staggers.</summary>
        public float Size = 1f;
        /// <summary>Horizontal body radius (metres).</summary>
        public float Radius = 0.5f;
        /// <summary>Unscaled time until which the Third Eye highlight shows its weak point.</summary>
        public float HighlightUntil;
        public CharacterBase Target;
        public int Seed;
        public float SpawnTime;

        public CreatureMode Mode { get; private set; }
        /// <summary>The body is built and the creature is in the world (pending spawns wait for their mesh).</summary>
        public bool Ready { get; private set; }
        /// <summary>It has been hurt by or has chosen to fight the player (territorial and stalker creatures start calm).</summary>
        public bool Provoked { get; private set; }
        /// <summary>The weak-point hit that just landed was a critical (for the HUD).</summary>
        public bool LastCrit { get; private set; }

        private bool _dead;
        private float _deathT;
        private float _deathDur = 1.4f;
        private float _dissolveAt;
        private float _fxTimer;
        private float _staggerUntil;
        private float _staggerStart;
        private float _poiseRegenAt;
        private float _hurt;
        private Vector3 _hurtDir = Vector3.back;
        private float _lastVehicleHitTime = -10f;
        private float _wingDamage;
        private float _wingDamageAt;
        private float _baseDamage = 10f;
        private Vector3 _push;
        private bool _plannedHostile;
        private CreatureSpecies _sp;

        public static void ResetStatics()
        {
            All.Clear();
        }

        public override Vector3 Velocity { get { return _vel; } }

        public bool IsHostileToPlayer { get { return Ready && !_dead && !CalmHabitatResident; } }
        /// <summary>Can be locked on to and aimed at: alive, above ground and not hidden (a stealthed shade is not).</summary>
        public bool Targetable { get { return Ready && !_dead && !(_serpent && _burrowed) && Visibility > 0.3f; } }
        /// <summary>Fighting, chasing, stalking or warning the player right now.</summary>
        public bool IsEngaged { get { return Ready && !_dead && Target != null && Target.IsPlayer && (Mode == CreatureMode.Hunt || Mode == CreatureMode.Stalk || Mode == CreatureMode.Warn); } }
        public bool IsStaggered { get { return !_dead && Time.time < _staggerUntil; } }
        public float StaggerProgress { get { return IsStaggered ? Mathf.InverseLerp(_staggerStart, _staggerUntil, Time.time) : 0f; } }
        /// <summary>A boss or a great beast: boss bar, bigger stagger thresholds, never flees.</summary>
        public bool IsBossLike { get { return _sp != null && (_sp.Boss || _sp.GreatBeast); } }
        public bool Burrowed { get { return _burrowed; } }
        public bool Flying { get { return _flying; } }

        // ================================================================== spawning

        /// <summary>
        /// Creates a creature of a non-humanoid species. With a body it is ready at once; without one (null) it waits,
        /// hidden, until Materialize hands it the body built on a worker thread.
        /// </summary>
        public static Creature Spawn(CreatureSpecies sp, Vector3 pos, float yaw, CreatureSpawnOptions o, CreatureBody body, Mesh sharedMesh)
        {
            GameObject go = new GameObject(sp != null ? sp.Name : "Creature");
            go.layer = Layers.Characters;
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            Creature c = go.AddComponent<Creature>();
            c.Setup(sp, o, yaw);
            if (body != null) c.Materialize(body, sharedMesh);
            return c;
        }

        private void Setup(CreatureSpecies sp, CreatureSpawnOptions o, float yaw)
        {
            _sp = sp;
            Species = sp;
            Options = o ?? new CreatureSpawnOptions();
            Tag = Options.Tag;
            Persistent = Options.Persistent;
            Elite = Options.Elite;
            Seed = Options.Seed;
            _plannedHostile = Options.Hostile;
            DisplayName = !string.IsNullOrEmpty(Options.DisplayName) ? Options.DisplayName : (sp != null ? (Elite ? "Shard-Touched " + sp.Name : sp.Name) : "Creature");
            Home = transform.position;
            SpawnTime = Time.time;
            _yaw = yaw;
            Team = gameObject.AddComponent<TeamTag>();
            Team.Team = NeonSyndicate.Team.Creature;
            Health = gameObject.AddComponent<CharacterHealth>();
            Health.Filter = this;
            Health.FilterBurns = true;
            Health.HeadshotMultiplier = 1f;
            Health.HeadHeight = 9999f;
            float hp = sp != null ? sp.Health : 80f;
            hp *= Mathf.Max(0.05f, Options.HealthScale) * (Elite ? 2.5f : 1f);
            Health.MaxHealth = hp;
            Health.Health = hp;
            Health.Surface = sp != null ? sp.HitSurface : ImpactSurface.Crystal;
            Health.SurfaceTint = sp != null ? sp.HitColor : Color.cyan;
            Health.Damaged += OnHurt;
            Health.Died += OnDied;
            if (Options.PracticeTarget) Health.OnLethal = SurvivePracticeHit;
            MaxPoise = (sp != null ? sp.Poise : 30f) * (Elite ? 1.8f : 1f);
            Poise = MaxPoise;
            _baseDamage = (sp != null ? sp.Damage : 10f) * Mathf.Max(0.05f, Options.DamageScale) * (Elite ? 1.3f : 1f);
            _attacks = sp != null ? sp.Attacks : new CreatureAttack[0];
            _readyAt = new float[_attacks.Length];
            for (int i = 0; i < _readyAt.Length; i++) _readyAt[i] = Time.time + UnityEngine.Random.Range(0.3f, 1.5f);
            Mode = CreatureMode.Idle;
        }

        /// <summary>Builds the rig from a ready body and puts the creature into the world.</summary>
        public void Materialize(CreatureBody body, Mesh sharedMesh)
        {
            if (Ready || body == null || _dead) return;
            CreatureLook look = body.Look ?? new CreatureLook();
            bool tiny = look.Plan == CreaturePlan.Mite;
            CRig = CreatureRig.Build(gameObject, body, sharedMesh, !tiny);
            AimAnchor = CRig.AimPoint;
            HeadAnchor = CRig.HeadPoint;
            Size = Mathf.Max(0.05f, body.Height);
            Radius = Mathf.Max(0.05f, body.Radius);
            BodyRadius = Radius;
            BodyHeight = Size;
            _plan = look.Plan;
            _flies = _sp != null && _sp.Flies && look.Plan == CreaturePlan.Winged && !Options.Vanguard04K;
            _serpent = look.Plan == CreaturePlan.Serpent;
            _wisp = look.Plan == CreaturePlan.Wisp;
            _mite = tiny;
            _turnRate = CreatureBrainLogic.TurnRate(Size);
            InitMotion();
            CRig.Pose.Reset(transform.position, _yaw);
            _parts = Options != null && Options.NoRewards ? null : CreatureParts.Attach(this);
            Ready = true;
            if (!All.Contains(this)) All.Add(this);
            if (_plannedHostile && Game.Player != null) BeginHunt(Game.Player);
            else Mode = _serpent ? CreatureMode.Wander : CreatureMode.Idle;
        }

        private void OnEnable()
        {
            if (Ready && !All.Contains(this)) All.Add(this);
        }

        private void OnDisable()
        {
            All.Remove(this);
            ReleaseTokens();
        }

        private void OnDestroy()
        {
            All.Remove(this);
            ReleaseTokens();
            if (Group != null) Group.Remove(this);
        }

        // ================================================================== frame

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || !Ready) return;
            if (_dead)
            {
                UpdateDeath(dt);
                return;
            }
            float now = Time.time;
            if (Poise < MaxPoise && now > _poiseRegenAt) Poise = Mathf.Min(MaxPoise, Poise + MaxPoise * 0.3f * dt);
            _hurt = Mathf.MoveTowards(_hurt, 0f, dt * 3f);
            Vector3 pp = Game.PlayerPosition;
            float playerDist = Vector3.Distance(transform.position, pp);
            // Distance LOD: far creatures think and move in bigger steps; mission creatures always run fully.
            float lodStep = Persistent || IsEngaged ? 0f : (playerDist > 140f ? 0.25f : (playerDist > 80f ? 0.1f : 0f));
            _lodTimer -= dt;
            _lodDt += dt;
            if (lodStep > 0f && _lodTimer > 0f) return;
            _lodTimer = lodStep;
            float step = Mathf.Min(_lodDt, 0.25f);
            _lodDt = 0f;
            _thinkTimer -= step;
            if (_thinkTimer <= 0f)
            {
                _thinkTimer = (IsEngaged ? 0.12f : 0.3f) + UnityEngine.Random.value * 0.08f;
                Think(playerDist);
            }
            if (_atk >= 0) UpdateAttack(step);
            UpdateMotion(step);
            Animate(step, playerDist);
        }

        // ================================================================== damage

        /// <summary>
        /// Every hit's first stop: underground wyrms cannot be hit, weaknesses and resistances, weak points (criticals),
        /// staggered creatures take extra, and the skill tree's creature damage bonus for hits from the player.
        /// </summary>
        public bool FilterDamage(ref DamageInfo info)
        {
            if (_dead || !Ready) return false;
            if (PracticeDefeated) return false;
            // The optional trial has one opponent. Stray area effects must not turn its spectators into a pack.
            if (CalmHabitatResident && StarterHabitat.SuppressPracticeNoise) return false;
            if (_serpent && _burrowed && _depth > Radius * 0.8f) return false;
            float mult = CreatureBrainLogic.ElementMultiplier(ElementOf(info), _sp != null ? _sp.Weakness : Element.None, _sp != null ? _sp.Resists : Element.None);
            bool crit = false;
            if (info.Type == DamageType.Bullet || info.Type == DamageType.Power)
            {
                crit = CRig.OnWeakPoint(info.Point, 0.08f);
                if (crit) mult *= info.Type == DamageType.Bullet ? 2f : 1.5f;
                // A broken core plate or chest plate bares the core: criticals hit harder still.
                if (crit && PartBroken(PartEffect.CoreExposed)) mult *= CreaturePartRules.CoreExposedCrit;
            }
            else if (info.Type == DamageType.Melee)
            {
                crit = MeleeOnWeakPoint(info);
                if (crit) mult *= 1.6f;
            }
            if (Options.Vanguard04K) mult *= VanguardRules04K.ExposedMultiplier(AttackPhase);
            if (IsStaggered) mult *= 1.25f;
            if (info.FromPlayer) mult *= Mathf.Max(0f, SkillMods.CreatureDamage);
            info.Amount *= mult;
            info.Headshot = crit;
            LastCrit = crit;
            LimitPracticeDamage(ref info);
            return true;
        }

        /// <summary>The element a hit carries: fire is ember, crystal powers are volt, the player's melee carries the weapon's element.</summary>
        private static Element ElementOf(DamageInfo info)
        {
            if (info.Type == DamageType.Fire) return Element.Ember;
            if (info.Type == DamageType.Power) return Element.Volt;
            if (info.Type == DamageType.Melee && info.FromPlayer && Game.Gear != null) return Game.Gear.WeaponElement;
            return Element.None;
        }

        /// <summary>Melee hits land where the swing meets the body: the hit volume nearest the attacker's weapon decides a critical.</summary>
        private bool MeleeOnWeakPoint(DamageInfo info)
        {
            if (CRig.WeakVolumes.Length == 0 || info.Source == null) return false;
            Transform a = info.Source.transform;
            Vector3 swing = a.position + Vector3.up * 1.15f + a.forward * 1.2f;
            int best = -1;
            float bd = float.MaxValue;
            for (int i = 0; i < CRig.ColliderVolume.Count; i++)
            {
                float d = CRig.DistanceToVolume(CRig.ColliderVolume[i], swing);
                if (d < bd) { bd = d; best = CRig.ColliderVolume[i]; }
            }
            if (best < 0) return false;
            for (int i = 0; i < CRig.WeakVolumes.Length; i++) if (CRig.WeakVolumes[i] == best) return true;
            return false;
        }

        private void OnHurt(DamageInfo info)
        {
            float amount = Health.LastApplied;
            if (amount <= 0f) return;
            bool byPlayer = info.FromPlayer;
            CRig.HitFlash(Mathf.Clamp(amount / Mathf.Max(1f, Health.MaxHealth) * 5f, 0.35f, 1f));
            _hurt = Mathf.Clamp(amount / Mathf.Max(1f, Health.MaxHealth) * 6f + 0.3f, 0.3f, 1f);
            if (info.Direction.sqrMagnitude > 1e-4f) _hurtDir = info.Direction;
            CreatureHits.Report(this, info, amount, LastCrit);
            // Poise: melee wears it down fastest, explosions and powers hard, bullets a little.
            float poise;
            switch (info.Type)
            {
                case DamageType.Melee: poise = info.Poise > 0f ? info.Poise : amount * 0.5f; break;
                case DamageType.Explosion: poise = amount * 0.8f; break;
                case DamageType.Power: poise = amount * 0.6f + info.Poise; break;
                case DamageType.Vehicle: poise = amount; break;
                default: poise = amount * 0.22f; break;
            }
            Poise -= poise;
            _poiseRegenAt = Time.time + 2.5f;
            if (Poise <= 0f && Health.IsAlive)
            {
                Poise = MaxPoise;
                Stagger(CreatureBrainLogic.StaggerSeconds(Size, IsBossLike));
                if (byPlayer && Game.Hud != null && (IsBossLike || Elite)) Game.Hud.CombatText("Stagger", new Color(1f, 0.85f, 0.4f));
            }
            else if (_atk >= 0 && Size < 1.3f && !IsBossLike && (info.Type == DamageType.Bullet || info.Type == DamageType.Melee) && AttackPhase == 0)
            {
                // Small creatures flinch out of a wind-up when shot or struck, so guns answer a charging pack.
                CancelAttack();
                Stagger(0.3f);
            }
            if (_flying) WingCheck(info, amount);
            if (info.Source != null)
            {
                CharacterBase attacker = info.Source.GetComponentInParent<CharacterBase>();
                if (attacker != null && attacker != this && (attacker.Team == null || attacker.Team.Team != NeonSyndicate.Team.Creature)) Provoke(attacker);
            }
            if (byPlayer) CombatDirector.MarkCombat();
        }

        /// <summary>A heavy hit on a wing while flying brings a flyer down to fight on the ground.</summary>
        private void WingCheck(DamageInfo info, float amount)
        {
            if (Time.time - _wingDamageAt > 4f) _wingDamage = 0f;
            _wingDamageAt = Time.time;
            int bone = CRig.NearestVolumeBone(info.Point);
            bool wing = false;
            CreatureSkeleton sk = CRig.Body.Skeleton;
            for (int i = 0; i < sk.Wings.Length && !wing; i++)
            {
                WingChain w = sk.Wings[i];
                if (bone == w.Arm || bone == w.Fore || bone == w.Hand) wing = true;
            }
            if (!wing) return;
            _wingDamage += amount;
            if (_wingDamage > Health.MaxHealth * 0.1f)
            {
                _wingDamage = 0f;
                ForceLanding(true);
                if (info.FromPlayer && Game.Hud != null) Game.Hud.CombatText("Wing hit", new Color(1f, 0.8f, 0.4f));
            }
        }

        /// <summary>Breaks the creature's balance: the attack stops and it reels, open to punishment.</summary>
        public void Stagger(float seconds)
        {
            if (_dead) return;
            CancelAttack();
            _staggerStart = Time.time;
            _staggerUntil = Mathf.Max(_staggerUntil, Time.time + seconds);
            _staggerDur = Mathf.Max(0.1f, _staggerUntil - _staggerStart);
            if (_flying && seconds > 0.6f) ForceLanding(false);
        }

        /// <summary>A parried creature reels (bosses recover sooner).</summary>
        public void OnParried(PlayerController by)
        {
            CancelAttack();
            Stagger(IsBossLike ? 1.4f : 2f);
            Poise = MaxPoise;
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.MetalHit, ChestPosition, 0.8f, 0.1f, 40f);
        }

        /// <summary>Takes poise straight away (frost procs, set bonuses).</summary>
        public void DrainPoise(float amount)
        {
            Poise -= amount;
            _poiseRegenAt = Time.time + 2.5f;
            if (Poise <= 0f && !_dead)
            {
                Poise = MaxPoise;
                Stagger(CreatureBrainLogic.StaggerSeconds(Size, IsBossLike));
            }
        }

        public override void KnockdownWithVelocity(Vector3 inherited, Vector3 velocityChange, Vector3 point, float minSeconds)
        {
            // No ragdolls for creatures: small bodies are shoved, big ones barely move; a strong blow staggers.
            if (_dead || !Ready) return;
            float mass = Mathf.Clamp(Size * Size, 0.05f, 30f);
            _push += velocityChange / Mathf.Max(1f, mass);
            if (velocityChange.magnitude > 3f * Mathf.Sqrt(mass)) Stagger(Mathf.Min(1.2f, minSeconds * 0.5f));
        }

        public override void OnVehicleHit(Vehicle vehicle, Vector3 vehicleVelocity, float relativeSpeed)
        {
            if (_dead || !Ready || vehicle == null) return;
            if (Time.time - _lastVehicleHitTime < 0.6f) return;
            _lastVehicleHitTime = Time.time;
            float damage = Mathf.Max(0f, relativeSpeed - 3.5f) * 6.5f;
            GameObject source = vehicle.Driver != null ? vehicle.Driver.gameObject : vehicle.gameObject;
            if (damage > 0f)
            {
                DamageInfo info = DamageInfo.Make(damage, ChestPosition, vehicleVelocity, source, DamageType.Vehicle);
                info.Force = relativeSpeed;
                Health.TakeDamage(info);
            }
            KnockdownWithVelocity(Vector3.zero, vehicleVelocity * 0.6f + Vector3.up * 2f, ChestPosition, 1.2f);
            if (Game.Audio != null) Game.Audio.PlayVar(relativeSpeed > 12f ? SfxId.CrashHeavy : SfxId.BodyFall, transform.position, 0.7f, 0.1f, 40f);
            if (Game.Fx != null) Game.Fx.CreatureHit(ChestPosition, vehicleVelocity, Health.Surface, Health.SurfaceTint, 10);
        }

        protected override void OnBeforeRagdoll() { }
        protected override void OnAfterRagdoll() { }

        /// <summary>Starts a fight with an attacker (and alerts its pack).</summary>
        public void Provoke(CharacterBase by)
        {
            if (_dead || PracticeDefeated || by == null || by == this) return;
            Provoked = true;
            if (Mode != CreatureMode.Hunt) BeginHunt(by);
            else Target = by;
            if (Group != null) Group.Alert(by, this);
        }

        // ================================================================== death

        protected override void OnDied(DamageInfo info)
        {
            if (_dead) return;
            _dead = true;
            // A flyer killed in the air falls to the real ground under it (its ground cache dates from its take-off).
            if (_flying) ProbeGroundFar(transform.position);
            Mode = CreatureMode.Dead;
            CancelAttack();
            ReleaseTokens();
            _deathT = 0f;
            _deathDur = _mite ? 0.5f : Mathf.Clamp(0.9f + Size * 0.35f, 1f, 2.6f);
            _dissolveAt = _deathDur + (_mite ? 0.2f : 0.6f);
            _actionT = 0f;
            CRig.SetCollidersEnabled(false);
            CRig.SetGlow(1f);
            CRig.SetVisibility(1f, Color.white);
            bool byPlayer = info.FromPlayer || (info.Source != null && Game.Player != null && Game.Player.InVehicle && info.Source == Game.Player.CurrentVehicle.gameObject);
            if (Game.Audio != null) CreatureAudio.Play(_sp, CreatureCry.Death, transform.position + Vector3.up * Size * 0.5f, Size);
            if (Game.Creatures != null) Game.Creatures.ReportDeath(this, _sp, byPlayer, info);
            if (Group != null) Group.OnMemberDied(this);
        }

        private void UpdateDeath(float dt)
        {
            _deathT += dt;
            // Flyers drop out of the sky; everything settles onto the ground.
            if (!_serpent)
            {
                Vector3 p = transform.position;
                float g = GroundY(p.x, p.z);
                if (p.y > g + 0.02f)
                {
                    _vy -= 16f * dt;
                    p.y = Mathf.Max(g, p.y + _vy * dt);
                    _flying = false;
                    transform.position = p;
                }
            }
            _actionT = Mathf.Clamp01(_deathT / _deathDur);
            Animate(dt, 0f);
            if (_deathT < _dissolveAt) return;
            float k = Mathf.Clamp01((_deathT - _dissolveAt) / (_mite ? 0.3f : 1.3f));
            CRig.SetDissolve(k * 0.9f);
            CRig.HitFlash(0.6f * (1f - k));
            _fxTimer -= dt;
            if (_fxTimer <= 0f && Game.Fx != null)
            {
                _fxTimer = _mite ? 0.1f : 0.07f;
                Transform b = CRig.Bones[UnityEngine.Random.Range(0, CRig.Bones.Length)];
                int n = _mite ? 3 : Mathf.Clamp(Mathf.RoundToInt(Size * 4f), 4, 14);
                if (_sp != null && _sp.DeathStyle == CreatureDeath.Smoke) Game.Fx.ShadowBurst(b.position, Health.SurfaceTint, n / 2 + 1, Mathf.Clamp(Size * 0.35f, 0.2f, 1.5f));
                else Game.Fx.CrystalBurst(b.position, Health.SurfaceTint, n, Mathf.Clamp(Size * 0.5f, 0.3f, 3f));
            }
            if (k >= 1f) Destroy(gameObject);
        }

        /// <summary>Removes the creature at once (far away, mission cleanup, the Veil closing); an optional puff of smoke.</summary>
        public void Despawn(bool puff)
        {
            if (puff && Game.Fx != null && Ready)
            {
                if (Health.Surface == ImpactSurface.Shadow) Game.Fx.ShadowBurst(ChestPosition, Health.SurfaceTint, 8, Mathf.Clamp(Size * 0.4f, 0.3f, 2f));
                else Game.Fx.CrystalBurst(ChestPosition, Health.SurfaceTint, 12, Mathf.Clamp(Size * 0.5f, 0.3f, 3f));
            }
            ReleaseTokens();
            Destroy(gameObject);
        }

        /// <summary>The point of the body nearest to a world position (for reach and spacing).</summary>
        public Vector3 ClosestBodyPoint(Vector3 world)
        {
            if (!Ready) return transform.position;
            Vector3 c = ChestPosition;
            Vector3 d = world - c;
            float m = d.magnitude;
            return m > Radius ? c + d / m * Radius : world;
        }
    }

    /// <summary>
    /// Shared hit feedback for every creature kind (non-humanoid Creatures and humanoid creature NPCs): the crystal or
    /// smoke hit effect of a blow. The floating damage numbers come from the health component (DamageNumbers).
    /// </summary>
    public static class CreatureHits
    {
        public static void Report(CharacterBase c, DamageInfo info, float applied, bool crit)
        {
            if (c == null || applied <= 0f) return;
            if (Game.Fx != null && info.Type == DamageType.Melee) Game.Fx.CreatureHit(info.Point, -info.Direction, c.Health.Surface, c.Health.SurfaceTint, crit ? 12 : 6);
        }
    }
}
