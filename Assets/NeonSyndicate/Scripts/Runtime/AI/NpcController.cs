using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public enum NpcRole
    {
        Civilian,
        Police,
        Swat,
        Gang,
        Enemy,
        Companion,
        Clerk
    }

    public enum NpcState
    {
        Wander,
        Idle,
        Flee,
        Cower,
        HandsUp,
        Phone,
        Fight,
        Chase,
        Dodge,
        Follow,
        Scripted,
        Seated
    }

    /// <summary>
    /// Every non-player character: pedestrians walking the sidewalk graph and obeying crosswalk signals, panicking,
    /// cowering, putting their hands up, calling the police, dodging cars; gang members guarding turf; police and SWAT
    /// chasing, arresting and shooting; mission enemies; and Luma as a companion who follows and rides along.
    /// </summary>
    public sealed partial class NpcController : CharacterBase
    {
        public static readonly List<NpcController> All = new List<NpcController>();

        public NpcRole Role;
        public NpcState State = NpcState.Wander;
        public int GangId = -1;
        public bool Persistent;
        public bool Hostile;
        public float HostileUntil;
        public CharacterBase Target;
        public float Courage = 0.3f;
        public float Accuracy = 1f;
        public float WalkSpeed = 1.35f;
        public float RunSpeed = 4.6f;
        public Vector3 Home;
        public float SpawnTime;
        public float DeathTime = -1f;
        public bool DropsLoot = true;
        public int CashDrop = 20;
        public bool IgnoreThreats;
        /// <summary>Companions that never fight (Luma, Ion).</summary>
        public bool Pacifist;
        public Action<NpcController> OnKilled;
        public Action<NpcController> OnScriptArrived;
        public string MissionTag;

        private Rigidbody _rb;
        private CapsuleCollider _cap;
        private Vector3 _vel;
        private float _yaw;
        private float _vy;
        private bool _grounded = true;
        private Vector3 _goal;
        private bool _hasGoal;
        private float _goalSpeed;
        private int _node = -1;
        private int _prevNode = -1;
        private bool _waitingCrossing;
        private int _crossLink = -1;
        private float _waitTimer;
        private float _thinkTimer;
        private float _stateTimer;
        private Vector3 _fleeFrom;
        private int _threatSerial;
        private float _burstTimer;
        private bool _bursting;
        private float _lastSeen = -99f;
        private Vector3 _lastKnown;
        private float _strafeDir = 1f;
        private float _strafeTimer;
        private float _damageWindow;
        private float _damageWindowStart;
        private float _stuckTimer;
        private Vector3 _lastPos;
        private float _losTimer;
        private bool _hasLos;
        private float _dodgeTimer;
        private Vector3 _dodgeDir;
        private float _lod;
        private bool _bloodPool;
        private NpcState _afterDodge = NpcState.Wander;
        // Deliberate fighting: poise and staggers, parry vulnerability, telegraphed melee, token-gated gunfire.
        public bool Elite;
        public float Poise = 35f;
        public float MaxPoise = 35f;
        public float MeleeDamageScale = 1f;
        private float _poiseRegenAt;
        private float _staggerUntil;
        private float _staggerStart;
        private float _riposteUntil;
        private MeleeMove _attack;
        private float _attackT;
        private readonly List<CharacterBase> _attackHits = new List<CharacterBase>();
        private float _nextAttackAt;
        private float _circleDir = 1f;
        private float _circleSwap;
        private float _reactUntil;
        private float _warnUntil;
        private bool _hadLosBefore;
        private bool _forced;
        private Vector3 _forcedVel;
        private readonly RaycastHit[] _hits = new RaycastHit[8];
        private static readonly List<int> _tmp = new List<int>();

        public static void ResetStatics()
        {
            All.Clear();
            _tmp.Clear();
        }

        public override Vector3 Velocity
        {
            get
            {
                if (InVehicle) return CurrentVehicle.Velocity;
                if (IsRagdolled && Ragdoll.HipsBody != null) return Ragdoll.HipsBody.linearVelocity;
                return _vel;
            }
        }

        public bool IsHostileToPlayer
        {
            get
            {
                if (IsDead) return false;
                // Creatures follow their temperament: a calm golem is wildlife until it is provoked or hunting the player.
                if (IsCreature) return !QuietAmbientCreature && (CreatureProvoked || CreatureEngaged);
                if (Role == NpcRole.Enemy) return true;
                if (Role == NpcRole.Police || Role == NpcRole.Swat) return Game.Wanted != null && Game.Wanted.Stars > 0;
                if (Role == NpcRole.Gang) return GangHostileToPlayer();
                return Hostile && Target != null && Target.IsPlayer;
            }
        }

        /// <summary>Knocked off balance by a parry: the next strike is a riposte.</summary>
        public bool CanBeRiposted { get { return !IsDead && Time.time < _riposteUntil; } }
        public bool IsStaggered { get { return !IsDead && Time.time < _staggerUntil; } }
        public bool IsWindingUp { get { return _attack != null && _attackT < _attack.Windup; } }
        public float StaggerProgress { get { return IsStaggered ? Mathf.InverseLerp(_staggerStart, _staggerUntil, Time.time) : 0f; } }

        public void ConsumeRiposte()
        {
            _riposteUntil = 0f;
        }

        public void OnParried(PlayerController by)
        {
            CancelAttack();
            Stagger(1.9f);
            _riposteUntil = Time.time + 1.9f;
            Poise = MaxPoise;
        }

        private void Stagger(float seconds)
        {
            CancelAttack();
            _staggerStart = Time.time;
            _staggerUntil = Mathf.Max(_staggerUntil, Time.time + seconds);
            _hasGoal = false;
            Anim.Aiming = false;
            Anim.StaggerT = 0f;
            Anim.StaggerSide = UnityEngine.Random.value < 0.5f ? -0.6f : 0.6f;
        }

        private void CancelAttack()
        {
            if (_attack != null)
            {
                _attack = null;
                Anim.AttackT = -1f;
                Anim.Telegraph = 0f;
            }
            _forced = false;
            CombatDirector.ReleaseMeleeToken(this);
        }

        // ================================================================== spawning

        public static NpcController Spawn(Vector3 pos, float yaw, NpcRole role, Appearance look, int gangId)
        {
            GameObject go = new GameObject(role + " NPC");
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            NpcController n = go.AddComponent<NpcController>();
            n.Init(role, look, gangId, yaw);
            return n;
        }

        private void Init(NpcRole role, Appearance look, int gangId, float yaw, int detail = -1)
        {
            Team team;
            switch (role)
            {
                case NpcRole.Police:
                case NpcRole.Swat: team = NeonSyndicate.Team.Police; break;
                case NpcRole.Gang: team = NeonSyndicate.Team.Gang; break;
                case NpcRole.Enemy: team = NeonSyndicate.Team.Hunter; break;
                case NpcRole.Companion: team = NeonSyndicate.Team.Companion; break;
                default: team = NeonSyndicate.Team.Civilian; break;
            }
            BuildBody(look, team, gangId, detail >= 0 ? detail : (role == NpcRole.Companion ? 2 : BodyFactory.AmbientDetail));
            Role = role;
            GangId = gangId;
            _yaw = yaw;
            Home = transform.position;
            SpawnTime = Time.time;
            _threatSerial = Threats.LatestSerial;
            _cap = gameObject.AddComponent<CapsuleCollider>();
            _cap.height = 1.78f * Rig.Scale;
            _cap.radius = 0.3f;
            _cap.center = new Vector3(0f, 0.89f * Rig.Scale, 0f);
            _rb = gameObject.AddComponent<Rigidbody>();
            _rb.isKinematic = true;
            _rb.useGravity = false;
            _rb.interpolation = RigidbodyInterpolation.None;
            Weapons.InfiniteAmmo = true;
            Health.HeadshotMultiplier = 3f;
            System.Random r = new System.Random(GetInstanceID());
            Courage = (float)r.NextDouble() * 0.6f;
            WalkSpeed = 1.2f + (float)r.NextDouble() * 0.35f;
            DisplayName = look.Female ? "Pedestrian" : "Pedestrian";
            switch (role)
            {
                case NpcRole.Police:
                    Health.MaxHealth = Health.Health = 120f;
                    Health.AddArmor(30f);
                    Courage = 1f;
                    Accuracy = 0.85f;
                    CashDrop = 40;
                    DisplayName = "Crystal Warden";
                    State = NpcState.Wander;
                    break;
                case NpcRole.Swat:
                    Health.MaxHealth = Health.Health = 150f;
                    Health.AddArmor(100f);
                    Courage = 1f;
                    Accuracy = 0.7f;
                    CashDrop = 60;
                    DisplayName = "Crystal Ward Sentinel";
                    break;
                case NpcRole.Gang:
                    Health.MaxHealth = Health.Health = 110f;
                    Courage = 0.75f + (float)r.NextDouble() * 0.25f;
                    Accuracy = 1.25f;
                    CashDrop = 60 + r.Next(0, 120);
                    State = NpcState.Idle;
                    GangDef g = Gangs.Get(gangId);
                    DisplayName = g != null ? g.Name : "Gang member";
                    break;
                case NpcRole.Enemy:
                    Health.MaxHealth = Health.Health = 120f;
                    Courage = 1f;
                    Accuracy = 1.1f;
                    CashDrop = 80;
                    DisplayName = "Hostile";
                    State = NpcState.Fight;
                    break;
                case NpcRole.Companion:
                    Health.MaxHealth = Health.Health = 400f;
                    Courage = 1f;
                    Accuracy = 0.9f;
                    DropsLoot = false;
                    State = NpcState.Follow;
                    break;
                case NpcRole.Clerk:
                    State = NpcState.Idle;
                    Courage = 0.1f;
                    DisplayName = "Clerk";
                    break;
                default:
                    CashDrop = 5 + r.Next(0, 60);
                    break;
            }
            switch (role)
            {
                case NpcRole.Police: MaxPoise = 45f; break;
                case NpcRole.Swat: MaxPoise = 70f; break;
                case NpcRole.Gang: MaxPoise = 40f; break;
                case NpcRole.Enemy: MaxPoise = 50f; break;
                case NpcRole.Companion: MaxPoise = 200f; break;
                default: MaxPoise = 22f; break;
            }
            Poise = MaxPoise;
            _circleDir = r.NextDouble() < 0.5 ? -1f : 1f;
            InitArmor();
        }

        /// <summary>Turns this NPC into a Shard-Touched elite: tougher, heavier hitting, unblockable heavies, mythic loot.</summary>
        public void MakeElite(string title)
        {
            Elite = true;
            Health.MaxHealth *= 3.2f;
            Health.Health = Health.MaxHealth;
            MaxPoise *= 3f;
            Poise = MaxPoise;
            MeleeDamageScale = 1.35f;
            Accuracy *= 0.85f;
            Courage = 1f;
            CashDrop *= 4;
            if (!string.IsNullOrEmpty(title)) DisplayName = title;
            Anim.EliteGlow = 1f;
            RefreshArmor();
        }

        private void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        private void OnDisable()
        {
            ClearResidence04C();
            All.Remove(this);
        }

        public void Arm(WeaponId id)
        {
            WeaponDef d = NeonSyndicate.Weapons.Get(id);
            Weapons.Inventory.Give(id, Mathf.Max(d.ClipSize * 6, 30));
            Weapons.Inventory.Select(id);
            Weapons.RefreshModel();
        }

        // ================================================================== damage and death

        protected override void OnDamaged(DamageInfo info)
        {
            if (IsCreature)
            {
                OnCreatureDamaged(info);
                return;
            }
            bool byPlayer = Game.IsPlayer(info.Source);
            // Actual attacks on ordinary actors override practice and local ambient grace.
            if (byPlayer && !Persistent && string.IsNullOrEmpty(MissionTag))
                StarterSafety.NotifyPlayerAggression(Game.PlayerPosition);
            if (Time.time - _damageWindowStart > 0.35f)
            {
                _damageWindowStart = Time.time;
                _damageWindow = 0f;
            }
            _damageWindow += info.Amount;
            bool knock = info.Type == DamageType.Explosion || (info.Flags & DamageFlags.Knockdown) != 0 || _damageWindow > (Elite ? 220f : 85f);
            if (!IsDead && !InVehicle && knock && !(Elite && info.Type == DamageType.Melee && (info.Flags & DamageFlags.Riposte) == 0 && Poise > 0f))
            {
                Vector3 push = info.Direction * Mathf.Clamp(info.Force, 1f, 14f) * 0.5f + Vector3.up * (info.Type == DamageType.Explosion ? info.Force * 0.4f : 0.6f);
                CancelAttack();
                Knockdown(push, info.Point, 1.6f);
            }
            else if (!IsDead && !InVehicle && info.Type == DamageType.Melee)
            {
                // Poise: every melee hit wears it down; at zero they stagger. Light hits interrupt a non-elite wind-up.
                Poise -= info.Poise > 0f ? info.Poise : info.Amount * 0.5f;
                _poiseRegenAt = Time.time + 2.5f;
                if (Poise <= 0f)
                {
                    Poise = MaxPoise;
                    Stagger(Elite ? 1.4f : 1.1f);
                    if (byPlayer && Game.Hud != null && Elite) Game.Hud.CombatText("Stagger", new Color(1f, 0.85f, 0.4f));
                }
                else if (!Elite && !(_attack != null && _attack.HyperArmor))
                {
                    Stagger(0.28f);
                }
            }
            else if (!IsDead && !InVehicle && info.Type == DamageType.Bullet && !Elite && IsWindingUp)
            {
                // A shot interrupts a melee wind-up, so guns are a good answer to a charging brute.
                Stagger(0.35f);
            }
            if (Game.Fx != null && info.Type == DamageType.Bullet && Health.LastApplied > 0.5f) Game.Fx.Blood(info.Point, -info.Direction, 4);
            if (byPlayer)
            {
                if (!IsDead && Game.Wanted != null)
                {
                    bool cop = Role == NpcRole.Police || Role == NpcRole.Swat;
                    Game.Wanted.ReportCrime(cop ? CrimeType.AttackCop : CrimeType.Assault, transform.position, gameObject);
                }
                OnAttackedBy(Game.Player);
            }
            else if (info.Source != null)
            {
                CharacterBase attacker = info.Source.GetComponentInParent<CharacterBase>();
                if (attacker != null && attacker != this && !SameSide(attacker)) OnAttackedBy(attacker);
            }
        }

        /// <summary>Friendly fire (a stray round from a squadmate) doesn't start a fight between allies.</summary>
        private bool SameSide(CharacterBase other)
        {
            if (other == null || other.Team == null || Team == null || other.Team.Team != Team.Team) return false;
            NpcController n = other as NpcController;
            if (Role == NpcRole.Gang && n != null) return n.GangId == GangId;
            return true;
        }

        private void OnAttackedBy(CharacterBase attacker)
        {
            if (IsDead || attacker == null) return;
            switch (Role)
            {
                case NpcRole.Civilian:
                case NpcRole.Clerk:
                    if (attacker.IsPlayer && Courage > 0.45f && Weapons.Current.Id == WeaponId.Fists && Game.Player.Weapons.Current.Fire == FireKind.Melee)
                    {
                        Hostile = true;
                        HostileUntil = Time.time + 25f;
                        Target = attacker;
                        SetState(NpcState.Fight);
                    }
                    else Flee(attacker.transform.position, 14f);
                    break;
                case NpcRole.Gang:
                    if (attacker.IsPlayer)
                    {
                        Gangs.AddHeat(GangId, 0.35f);
                        AlertGang(attacker);
                    }
                    Hostile = true;
                    HostileUntil = Time.time + 90f;
                    Target = attacker;
                    SetState(NpcState.Fight);
                    break;
                case NpcRole.Companion:
                    if (!attacker.IsPlayer)
                    {
                        Target = attacker;
                        SetState(NpcState.Fight);
                    }
                    break;
                default:
                    Target = attacker;
                    if (State != NpcState.Fight) SetState(NpcState.Fight);
                    break;
            }
        }

        private void AlertGang(CharacterBase attacker)
        {
            foreach (NpcController n in All)
            {
                if (n == this || n.IsDead || n.Role != NpcRole.Gang || n.GangId != GangId) continue;
                if (MathUtil.FlatSqrDistance(n.transform.position, transform.position) > 45f * 45f) continue;
                n.Hostile = true;
                n.HostileUntil = Time.time + 90f;
                n.Target = attacker;
                if (n.State != NpcState.Fight) n.SetState(NpcState.Fight);
            }
        }

        protected override void OnDied(DamageInfo info)
        {
            if (IsCreature)
            {
                OnCreatureDied(info);
                return;
            }
            Vehicle rodeIn = CurrentVehicle;
            bool wasDriver = rodeIn != null && Seat == 0;
            base.OnDied(info);
            DeathTime = Time.time;
            if (_cap != null) _cap.enabled = false;
            CancelAttack();
            CombatDirector.ReleaseShotToken(this);
            bool byPlayer = Game.IsPlayer(info.Source);
            if (byPlayer && Game.Progress != null) Game.Progress.OnEnemyKilled(this);
            // A driver shot dead in an intact car: the passengers get out instead of sitting there forever.
            if (wasDriver && rodeIn != null && !rodeIn.IsDestroyed) EvacuatePassengers(rodeIn, byPlayer);
            if (byPlayer)
            {
                bool cop = Role == NpcRole.Police || Role == NpcRole.Swat;
                if (Game.Stats != null)
                {
                    Game.Stats.Add(StatId.Kills, 1);
                    if (cop) Game.Stats.Add(StatId.CopsKilled, 1);
                    else if (Role == NpcRole.Gang) Game.Stats.Add(StatId.GangKills, 1);
                    else if (Role == NpcRole.Civilian) Game.Stats.Add(StatId.CiviliansKilled, 1);
                    if (info.Headshot) Game.Stats.Add(StatId.Headshots, 1);
                }
                if (Game.Wanted != null && Role != NpcRole.Enemy && Role != NpcRole.Gang) Game.Wanted.ReportCrime(cop ? CrimeType.KillCop : CrimeType.Murder, transform.position, gameObject);
                if (Game.Wanted != null && Role == NpcRole.Gang) Game.Wanted.ReportCrime(CrimeType.Murder, transform.position, gameObject);
                if (Role == NpcRole.Gang) Gangs.AddHeat(GangId, 0.25f);
                if (Game.Abilities != null) Game.Abilities.OnKill();
                if (Game.Hud != null && info.Headshot) Game.Hud.Notify("Headshot", 1.2f);
            }
            if (DropsLoot && Game.Pickups != null)
            {
                Vector3 p = transform.position + Vector3.up * 0.3f;
                if (CashDrop > 0) Game.Pickups.SpawnCash(p + UnityEngine.Random.insideUnitSphere * 0.4f, CashDrop);
                WeaponId w = Weapons.Current.Id;
                if (w != WeaponId.Fists && !NeonSyndicate.Weapons.IsLoot(w) && Role != NpcRole.Companion) Game.Pickups.SpawnWeapon(p + transform.right * 0.6f, w, Mathf.Max(NeonSyndicate.Weapons.Get(w).ClipSize * 2, 12), true);
            }
            // Loot guns and weapon cores (champions, Shard-Touched elites and now and then anyone else).
            if (byPlayer && DropsLoot && Game.Loot != null)
            {
                try { Game.Loot.OnNpcKilled(this); }
                catch (Exception e) { Log.Exception("Loot drop", e); }
            }
            if (IsChampion) OnChampionDied();
            if (OnKilled != null)
            {
                try { OnKilled(this); }
                catch (Exception e) { Log.Exception("NPC killed callback", e); }
            }
            Threats.Report(transform.position, 30f, info.Source, ThreatKind.Violence);
        }

        private static void EvacuatePassengers(Vehicle v, bool killedByPlayer)
        {
            if (v.Occupants == null) return;
            for (int i = 1; i < v.Occupants.Length; i++)
            {
                NpcController n = v.Occupants[i] as NpcController;
                if (n == null || n.IsDead || n.Role == NpcRole.Companion) continue;
                // At speed they tumble out like a bail, instead of appearing upright on the road.
                n.ExitVehicleImmediate(v.Speed > 7f);
                switch (n.Role)
                {
                    case NpcRole.Police:
                    case NpcRole.Swat:
                        n.SetState(NpcState.Chase);
                        break;
                    case NpcRole.Enemy:
                        if (Game.Player != null) n.Attack(Game.Player);
                        break;
                    case NpcRole.Gang:
                        if ((killedByPlayer || n.Hostile) && Game.Player != null) n.Attack(Game.Player);
                        else n.Flee(v.transform.position, 10f);
                        break;
                    default:
                        n.Flee(v.transform.position, 12f);
                        break;
                }
            }
        }

        protected override void OnBeforeRagdoll()
        {
            if (_cap != null) _cap.enabled = false;
            _hasGoal = false;
        }

        protected override void OnAfterRagdoll()
        {
            if (_cap != null) _cap.enabled = true;
            _vel = Vector3.zero;
            _yaw = transform.eulerAngles.y;
            _node = -1;
            if (Role == NpcRole.Civilian && State != NpcState.Fight && MissionTag == null) Flee(Game.PlayerPosition, 10f);
        }

        protected override void OnEnteredVehicle()
        {
            if (_cap != null)
            {
                // Seated hitbox: torso and head only, so the capsule never pokes through the roof.
                _cap.height = 0.95f * Rig.Scale;
                _cap.center = new Vector3(0f, 0.85f * Rig.Scale, 0.05f);
                _cap.radius = 0.24f;
            }
            _vel = Vector3.zero;
            if (State != NpcState.Seated && Role != NpcRole.Companion) SetState(NpcState.Seated);
        }

        protected override void OnExitedVehicle(Vehicle v)
        {
            if (_cap != null)
            {
                _cap.height = 1.78f * Rig.Scale;
                _cap.center = new Vector3(0f, 0.89f * Rig.Scale, 0f);
                _cap.radius = 0.3f;
            }
            _yaw = transform.eulerAngles.y;
            _node = -1;
            if (State == NpcState.Seated) SetState(Role == NpcRole.Police || Role == NpcRole.Swat ? NpcState.Chase : NpcState.Wander);
        }

        public void OnCarjacked(PlayerController thief)
        {
            if (Role == NpcRole.Police || Role == NpcRole.Swat)
            {
                Target = thief;
                SetState(NpcState.Fight);
                return;
            }
            if (Courage > 0.5f && UnityEngine.Random.value < 0.6f)
            {
                Hostile = true;
                HostileUntil = Time.time + 30f;
                Target = thief;
                SetState(NpcState.Fight);
            }
            else Flee(thief.transform.position, 15f);
        }

        // ================================================================== public commands

        public void SetState(NpcState s)
        {
            if (State == s) return;
            State = s;
            _stateTimer = 0f;
            _hasGoal = false;
            _waitingCrossing = false;
            Anim.HandsUp = s == NpcState.HandsUp;
            Anim.Cower = s == NpcState.Cower;
            Anim.Phone = s == NpcState.Phone;
            if (s != NpcState.Fight) Anim.Aiming = false;
        }

        public void Flee(Vector3 from, float seconds)
        {
            if (IsDead || Role == NpcRole.Police || Role == NpcRole.Swat || Role == NpcRole.Enemy || Role == NpcRole.Companion) return;
            _fleeFrom = from;
            SetState(NpcState.Flee);
            _stateTimer = -seconds;
            Anim.Panic = 1f;
        }

        /// <summary>Witness behavior: stop and phone the police (WantedSystem completes the call).</summary>
        public void BeginPhoneCall()
        {
            if (IsDead || InVehicle) return;
            SetState(NpcState.Phone);
        }

        public bool IsOnPhone { get { return State == NpcState.Phone && !IsDead && !IsRagdolled; } }

        public void MoveTo(Vector3 pos, bool run, Action<NpcController> arrived)
        {
            SetState(NpcState.Scripted);
            _goal = pos;
            _hasGoal = true;
            _goalSpeed = run ? RunSpeed : WalkSpeed;
            OnScriptArrived = arrived;
        }

        public void Attack(CharacterBase target)
        {
            Target = target;
            Hostile = true;
            HostileUntil = Time.time + 9999f;
            SetState(NpcState.Fight);
        }

        public void GuardAt(Vector3 pos)
        {
            Home = pos;
            SetState(NpcState.Idle);
        }

        public bool CanSeePlayer(float range)
        {
            if (Game.Player == null) return false;
            Vector3 eye = HeadPosition;
            Vector3 target = Game.Player.ChestPosition;
            Vector3 to = target - eye;
            float dist = to.magnitude;
            if (dist > range) return false;
            return !Physics.Linecast(eye, target, Layers.WorldMask, QueryTriggerInteraction.Ignore);
        }

        // ================================================================== update

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            UpdateRagdollState(dt);
            if (IsDead)
            {
                if (!_bloodPool && IsRagdolled && Ragdoll.IsSettled && Game.Fx != null)
                {
                    _bloodPool = true;
                    Game.Fx.BloodPool(Ragdoll.HipsPosition);
                }
                return;
            }
            if (IsRagdolled) return;
            if (InVehicle)
            {
                Anim.SitWeight = 1f;
                Anim.Velocity = Vector3.zero;
                if (Role == NpcRole.Companion) UpdateCompanionInVehicle();
                return;
            }
            if (UpdateResidence04C(dt)) return;
            // Distance LOD: far characters think and move less often.
            float playerDist = MathUtil.FlatDistance(transform.position, Game.PlayerPosition);
            _lod -= dt;
            float lodStep = playerDist > 90f ? 0.12f : (playerDist > 50f ? 0.05f : 0f);
            if (_lod > 0f && !Persistent) return;
            float stepDt = lodStep > 0f ? Mathf.Max(dt, lodStep) : dt;
            _lod = lodStep;

            _stateTimer += stepDt;
            _thinkTimer -= stepDt;
            if (_thinkTimer <= 0f)
            {
                _thinkTimer = 0.18f + UnityEngine.Random.value * 0.15f;
                Think(playerDist);
            }
            float speed = 0f;
            switch (State)
            {
                case NpcState.Wander: speed = UpdateWander(stepDt); break;
                case NpcState.Idle: speed = UpdateIdle(stepDt); break;
                case NpcState.Flee: speed = UpdateFlee(stepDt); break;
                case NpcState.Cower:
                    if (_stateTimer > 7f && MissionTag == null) Flee(_fleeFrom, 10f);
                    break;
                case NpcState.HandsUp:
                    if (_stateTimer > (Role == NpcRole.Clerk ? 14f : 5f)) Flee(Game.PlayerPosition, 12f);
                    FaceTowards(Game.PlayerPosition, stepDt);
                    break;
                case NpcState.Phone:
                    if (_stateTimer > 12f) SetState(NpcState.Wander);
                    break;
                case NpcState.Fight: speed = UpdateFight(stepDt); break;
                case NpcState.Chase: speed = UpdateChase(stepDt); break;
                case NpcState.Dodge: speed = UpdateDodge(stepDt); break;
                case NpcState.Follow: speed = UpdateFollow(stepDt); break;
                case NpcState.Scripted: speed = UpdateScripted(stepDt); break;
            }
            Move(stepDt, speed);
        }

        // ------------------------------------------------------------------ perception

        private void Think(float playerDist)
        {
            if (CityThink04F()) return;
            if (Hostile && Time.time > HostileUntil && Role != NpcRole.Enemy)
            {
                Hostile = false;
                if (State == NpcState.Fight && Role != NpcRole.Police && Role != NpcRole.Swat) SetState(Role == NpcRole.Gang ? NpcState.Idle : NpcState.Wander);
            }
            StopPassiveGangPursuit();
            ThreatEvent e;
            if (!IgnoreThreats && Threats.Query(transform.position, _threatSerial, out e))
            {
                ReactToThreat(e, playerDist);
            }
            _threatSerial = Threats.LatestSerial;
            if (IsCreature)
            {
                ThinkCreature(playerDist);
                return;
            }

            switch (Role)
            {
                case NpcRole.Police:
                case NpcRole.Swat:
                    ThinkPolice(playerDist);
                    break;
                case NpcRole.Gang:
                    if (State != NpcState.Fight && IsHostileToPlayer && playerDist < 38f && CanSeePlayer(38f))
                    {
                        Target = Game.Player;
                        SetState(NpcState.Fight);
                    }
                    break;
                case NpcRole.Enemy:
                    if ((State == NpcState.Idle || State == NpcState.Dodge) && !Hostile)
                    {
                        // Guards posted with GuardAt hold their ground until they spot the player or get hurt
                        // (stepping out of a car's way doesn't count).
                        if (Health.Health < Health.MaxHealth || (playerDist < 30f && CanSeePlayer(30f))) Attack(Game.Player);
                        break;
                    }
                    if (Target == null || Target.IsDead) Target = Game.Player;
                    if (State != NpcState.Fight && State != NpcState.Scripted) SetState(NpcState.Fight);
                    break;
                case NpcRole.Companion:
                    if (Pacifist)
                    {
                        if (State == NpcState.Fight) SetState(NpcState.Follow);
                        Target = null;
                        break;
                    }
                    if (State == NpcState.Fight && (Target == null || Target.IsDead || (Target is Creature tc && !tc.Targetable))) SetState(NpcState.Follow);
                    if (State == NpcState.Follow && _stateTimer > 0.6f) Target = FindCompanionTarget();
                    if (Target != null && State == NpcState.Follow) SetState(NpcState.Fight);
                    break;
            }
            // Dodge vehicles bearing down on us (scripted walkers and mission characters keep to their script).
            if (State != NpcState.Dodge && State != NpcState.Fight && State != NpcState.Scripted && !IgnoreThreats && Role != NpcRole.Companion && playerDist < 70f) CheckDodge();
        }

        private void ReactToThreat(ThreatEvent e, float playerDist)
        {
            bool fromPlayer = Game.IsPlayer(e.Source) || (Game.Player != null && Game.Player.InVehicle && e.Source == Game.Player.CurrentVehicle.gameObject);
            float d = MathUtil.FlatDistance(e.Position, transform.position);
            switch (Role)
            {
                case NpcRole.Civilian:
                case NpcRole.Clerk:
                    if (State == NpcState.Fight) return;
                    if (e.Kind == ThreatKind.Aim)
                    {
                        if (State == NpcState.Flee) return;
                        if (UnityEngine.Random.value < 0.65f) SetState(NpcState.HandsUp);
                        else Flee(e.Position, 12f);
                        return;
                    }
                    if (e.Kind == ThreatKind.CarCrash)
                    {
                        if (d < 10f && State == NpcState.Wander && UnityEngine.Random.value < 0.25f) Flee(e.Position, 4f);
                        return;
                    }
                    if (State == NpcState.Phone) return;
                    if (d < 9f && UnityEngine.Random.value < 0.35f && e.Kind == ThreatKind.Gunshot)
                    {
                        _fleeFrom = e.Position;
                        SetState(NpcState.Cower);
                    }
                    else Flee(e.Position, 10f + UnityEngine.Random.value * 8f);
                    break;
                case NpcRole.Gang:
                    if (fromPlayer && e.Kind != ThreatKind.CarCrash && d < 40f)
                    {
                        if (IgnoreWelcomeThreat(e)) return;
                        if (e.Kind == ThreatKind.Aim && Courage < 0.85f && !Hostile) return;
                        Hostile = true;
                        HostileUntil = Time.time + 60f;
                        Target = Game.Player;
                        SetState(NpcState.Fight);
                    }
                    break;
                case NpcRole.Enemy:
                    if (IsCreature) { CreatureHearsThreat(e, d, fromPlayer); break; }
                    if ((State == NpcState.Idle || State == NpcState.Dodge) && !Hostile && fromPlayer && e.Kind != ThreatKind.CarCrash && d < 45f) Attack(Game.Player);
                    break;
                case NpcRole.Companion:
                    if (!fromPlayer && e.Source != null && !Pacifist)
                    {
                        CharacterBase c = e.Source.GetComponentInParent<CharacterBase>();
                        if (c != null && c != this && !c.IsDead && c.Team != null && c.Team.Team != NeonSyndicate.Team.Civilian)
                        {
                            Target = c;
                            SetState(NpcState.Fight);
                        }
                    }
                    break;
            }
        }

        private void CheckDodge()
        {
            for (int i = 0; i < Vehicle.All.Count; i++)
            {
                Vehicle v = Vehicle.All[i];
                if (v == null || v.Speed < 6f) continue;
                Vector3 to = transform.position - v.transform.position;
                if (to.sqrMagnitude > 18f * 18f) continue;
                Vector3 vel = v.Velocity;
                float along = Vector3.Dot(to, vel.normalized);
                if (along < 0f || along > vel.magnitude * 1.4f) continue;
                Vector3 lateral = to - vel.normalized * along;
                if (lateral.magnitude > v.Def.Width * 0.5f + 1.2f) continue;
                Vector3 side = Vector3.Cross(Vector3.up, vel.normalized);
                if (Vector3.Dot(side, lateral) < 0f) side = -side;
                _dodgeDir = side;
                _dodgeTimer = 0.55f;
                _afterDodge = State == NpcState.Dodge ? NpcState.Wander : State;
                SetState(NpcState.Dodge);
                if (Game.Audio != null && UnityEngine.Random.value < 0.3f) Game.Audio.PlayVar(SfxId.Whoosh, transform.position, 0.3f, 0.2f, 20f);
                return;
            }
        }

        // ------------------------------------------------------------------ civilians

        private float UpdateWander(float dt)
        {
            CityLayout L = Game.Layout;
            if (L == null) return 0f;
            if (_waitTimer > 0f)
            {
                _waitTimer -= dt;
                if (_waitingCrossing)
                {
                    WalkLink link = L.WalkLinks[_crossLink];
                    bool may = link.SignalNode < 0 ? !CarApproaching(L.WalkNodes[link.A].Pos, L.WalkNodes[link.B].Pos) : (Game.Signals == null || Game.Signals.PedestrianMayCross(link.SignalNode, link.CrossesNSRoad));
                    if (may)
                    {
                        _waitingCrossing = false;
                        _waitTimer = 0f;
                    }
                    else _waitTimer = 0.3f;
                }
                return 0f;
            }
            if (_node < 0)
            {
                _node = L.NearestWalkNode(transform.position, 40f);
                if (_node < 0) { SetState(NpcState.Idle); return 0f; }
                _goal = L.WalkNodes[_node].Pos;
                _hasGoal = true;
            }
            if (!_hasGoal || MathUtil.FlatSqrDistance(transform.position, _goal) < 0.5f)
            {
                // Arrived at a node: pick the next link.
                WalkNode wn = L.WalkNodes[_node];
                int pick = -1;
                _tmp.Clear();
                for (int i = 0; i < wn.Links.Count; i++)
                {
                    WalkLink link = L.WalkLinks[wn.Links[i]];
                    int other = link.A == _node ? link.B : link.A;
                    if (other == _prevNode && wn.Links.Count > 1) continue;
                    _tmp.Add(wn.Links[i]);
                }
                if (_tmp.Count == 0 && wn.Links.Count > 0) _tmp.Add(wn.Links[0]);
                if (_tmp.Count == 0) { SetState(NpcState.Idle); return 0f; }
                int commute04N=CommuteLink04N(L);
                if(commute04N==-2){_waitTimer=8f;Anim.Phone=false;return 0f;}
                pick = commute04N>=0?commute04N:_tmp[UnityEngine.Random.Range(0, _tmp.Count)];
                WalkLink chosen = L.WalkLinks[pick];
                int next = chosen.A == _node ? chosen.B : chosen.A;
                _prevNode = _node;
                _node = next;
                Vector3 offset = UnityEngine.Random.insideUnitSphere * 0.9f;
                offset.y = 0f;
                _goal = L.WalkNodes[next].Pos + offset;
                _hasGoal = true;
                if (chosen.Crosswalk)
                {
                    _waitingCrossing = true;
                    _crossLink = pick;
                    _waitTimer = 0.2f;
                }
                else if (UnityEngine.Random.value < 0.06f)
                {
                    _waitTimer = UnityEngine.Random.Range(2f, 6f);
                    Anim.Phone = UnityEngine.Random.value < 0.4f;
                }
                else Anim.Phone = false;
            }
            return WalkSpeed;
        }

        private static bool CarApproaching(Vector3 a, Vector3 b)
        {
            Vector3 mid = (a + b) * 0.5f;
            for (int i = 0; i < Vehicle.All.Count; i++)
            {
                Vehicle v = Vehicle.All[i];
                if (v == null || v.Speed < 2f) continue;
                Vector3 to = mid - v.transform.position;
                if (to.sqrMagnitude > 30f * 30f) continue;
                if (Vector3.Dot(to, v.Velocity) > 0f) return true;
            }
            return false;
        }

        private float UpdateIdle(float dt)
        {
            if (IsCreature && CreatureWarning && Game.Player != null) FaceTowards(Game.PlayerPosition, dt);
            if (Role == NpcRole.Civilian && _stateTimer > 6f && MissionTag == null)
            {
                SetState(NpcState.Wander);
                return 0f;
            }
            float d = MathUtil.FlatDistance(transform.position, Home);
            if (d > 2.5f)
            {
                _goal = Home;
                _hasGoal = true;
                return WalkSpeed;
            }
            if (!_hasGoal && _stateTimer > 4f && UnityEngine.Random.value < 0.01f)
            {
                Vector2 r = UnityEngine.Random.insideUnitCircle * 2f;
                _goal = Home + new Vector3(r.x, 0f, r.y);
                _hasGoal = true;
                _stateTimer = 0f;
                return WalkSpeed * 0.8f;
            }
            if (_hasGoal) return WalkSpeed * 0.8f;
            if (Role == NpcRole.Gang && Game.Player != null && MathUtil.FlatDistance(transform.position, Game.PlayerPosition) < 8f)
                FaceTowards(Game.PlayerPosition, dt);
            return 0f;
        }

        private float UpdateFlee(float dt)
        {
            if (_stateTimer > 0f)
            {
                Anim.Panic = 0f;
                SetState(Role == NpcRole.Gang ? NpcState.Idle : NpcState.Wander);
                _node = -1;
                return 0f;
            }
            Vector3 away = MathUtil.Flat(transform.position - _fleeFrom);
            if (away.sqrMagnitude < 0.01f) away = transform.forward;
            if (!_hasGoal || MathUtil.FlatSqrDistance(transform.position, _goal) < 2f)
            {
                Vector3 dir = Quaternion.Euler(0f, UnityEngine.Random.Range(-35f, 35f), 0f) * away.normalized;
                _goal = transform.position + dir * 14f;
                _hasGoal = true;
            }
            return RunSpeed * (1f + Courage * 0.1f);
        }

        private float UpdateDodge(float dt)
        {
            _dodgeTimer -= dt;
            _goal = transform.position + _dodgeDir * 3f;
            _hasGoal = true;
            if (_dodgeTimer <= 0f)
            {
                NpcState next = _afterDodge;
                if (Role == NpcRole.Civilian && UnityEngine.Random.value < 0.4f) Flee(transform.position - _dodgeDir, 5f);
                else SetState(next == NpcState.Dodge ? NpcState.Wander : next);
                _node = -1;
            }
            return RunSpeed * 1.2f;
        }

        // ------------------------------------------------------------------ police

        private void ThinkPolice(float playerDist)
        {
            int stars = Game.Wanted != null ? Game.Wanted.Stars : 0;
            if (Game.Police != null && Game.Police.LocalContainment04F && !Persistent && string.IsNullOrEmpty(MissionTag))
            {
                Target = null;
                CancelAttack();
                CombatDirector.ReleaseShotToken(this);
                if (State == NpcState.Fight || State == NpcState.Chase) SetState(NpcState.Wander);
                return;
            }
            if (stars <= 0)
            {
                if (State == NpcState.Fight || State == NpcState.Chase)
                {
                    Target = null;
                    SetState(NpcState.Wander);
                    _node = -1;
                }
                return;
            }
            Target = Game.Player;
            bool sees = playerDist < 70f && CanSeePlayer(70f);
            if (sees)
            {
                _lastSeen = Time.time;
                _lastKnown = Game.PlayerPosition;
                if (Game.Wanted != null) Game.Wanted.ReportSighting(this);
            }
            bool arrestable = stars == 1 && Game.Player != null && !Game.Player.InVehicle && Game.Player.Weapons.Current.Fire == FireKind.Melee && !Game.Player.Aiming;
            NpcState want = arrestable ? NpcState.Chase : (sees ? NpcState.Fight : NpcState.Chase);
            if (State != want && State != NpcState.Scripted) SetState(want);
        }

        private float UpdateChase(float dt)
        {
            if (Game.Player == null) return 0f;
            Vector3 target = Time.time - _lastSeen < 2f ? Game.PlayerPosition : (_lastKnown != Vector3.zero ? _lastKnown : Game.PlayerPosition);
            float d = MathUtil.FlatDistance(transform.position, target);
            int stars = Game.Wanted != null ? Game.Wanted.Stars : 0;
            if (stars == 1 && d < 2.2f && Game.Player != null && !Game.Player.InVehicle && !Game.Player.IsRagdolled)
            {
                // Arrest attempt: hold the suspect at gunpoint up close.
                FaceTowards(target, dt);
                Anim.Aiming = Weapons.Current.IsGun;
                Anim.AimDirection = (Game.Player.ChestPosition - ChestPosition).normalized;
                if (Game.Police != null) Game.Police.ReportArrestContact(this, dt);
                return 0f;
            }
            Anim.Aiming = false;
            _goal = target;
            _hasGoal = d > 1.6f;
            if (d < 4f && Time.time - _lastSeen > 6f)
            {
                // Lost them: search around the last known position.
                Vector2 r = UnityEngine.Random.insideUnitCircle * 12f;
                _lastKnown += new Vector3(r.x, 0f, r.y);
            }
            return RunSpeed;
        }

        // ------------------------------------------------------------------ combat

        private float UpdateFight(float dt)
        {
            if (CityActor04F != null && (Target == null || !CityActor04F.MayDamage(Target)))
            {
                CancelAttack();
                CombatDirector.ReleaseShotToken(this);
                Target = null;
                GuardAt(transform.position);
                return 0f;
            }
            _forced = false;
            if (Poise < MaxPoise && Time.time > _poiseRegenAt) Poise = Mathf.Min(MaxPoise, Poise + MaxPoise * 0.35f * dt);
            if (Target == null || Target.IsDead)
            {
                CancelAttack();
                CombatDirector.ReleaseShotToken(this);
                Target = null;
                if (Role == NpcRole.Police || Role == NpcRole.Swat) SetState(NpcState.Chase);
                else if (Role == NpcRole.Companion) SetState(NpcState.Follow);
                else if (Role == NpcRole.Enemy) Target = Game.Player;
                else SetState(Role == NpcRole.Gang ? NpcState.Idle : NpcState.Wander);
                return 0f;
            }
            // Staggered: reeling, open to punishment.
            if (IsStaggered)
            {
                Anim.StaggerT = StaggerProgress;
                Anim.Aiming = false;
                _hasGoal = false;
                return 0f;
            }
            if (Anim.StaggerT >= 0f) Anim.StaggerT = -1f;
            // Wounded civilians and some gang members break off.
            if (Role != NpcRole.Police && Role != NpcRole.Swat && Role != NpcRole.Enemy && Role != NpcRole.Companion && !Elite && Health.Health < Health.MaxHealth * 0.25f && Courage < 0.9f)
            {
                CancelAttack();
                Flee(Target.transform.position, 15f);
                return 0f;
            }
            Vector3 targetPos = Target.InVehicle ? Target.CurrentVehicle.Center : Target.ChestPosition;
            Vector3 flatTo = MathUtil.Flat(targetPos - transform.position);
            float dist = flatTo.magnitude;
            WeaponDef d = Weapons.Current;
            _losTimer -= dt;
            if (_losTimer <= 0f)
            {
                _losTimer = 0.3f;
                _hasLos = !Physics.Linecast(HeadPosition, targetPos, Layers.WorldMask, QueryTriggerInteraction.Ignore);
                if (_hasLos)
                {
                    _lastSeen = Time.time;
                    _lastKnown = Target.transform.position;
                    if ((Role == NpcRole.Police || Role == NpcRole.Swat) && Target.IsPlayer && Game.Wanted != null) Game.Wanted.ReportSighting(this);
                }
            }
            if (Target.IsPlayer) CombatDirector.MarkCombat();
            Anim.Hold = d.Hold;
            Anim.HoldBlade = d.Fire == FireKind.Melee && d.Id != WeaponId.Fists && d.Id != WeaponId.ArcGauntlets;
            Anim.TwoHanded = d.Id == WeaponId.CrystalMaul || d.Id == WeaponId.SluggerBat;
            if (d.Fire == FireKind.Melee) return UpdateMeleeFight(dt, d, targetPos, flatTo, dist);
            // Armed human enemies fighting the player use cover tactics (NpcController.Tactics.cs).
            float tacticSpeed;
            if (UpdateEventCover04H(dt, d, targetPos, dist, out tacticSpeed)) return tacticSpeed;
            if (UpdateTactics(dt, d, targetPos, flatTo, dist, out tacticSpeed)) return tacticSpeed;

            // Guns: keep a comfortable distance, strafe, shoot in bursts when there is line of sight.
            FaceTowards(targetPos, dt);
            Anim.Aiming = _hasLos && dist < d.Range;
            Anim.AimDirection = (targetPos - ChestPosition).normalized;
            float minD = Role == NpcRole.Companion ? 4f : 6f;
            float maxD = Mathf.Min(22f, d.Range * 0.6f);
            float speed = 0f;
            _strafeTimer -= dt;
            if (_strafeTimer <= 0f)
            {
                _strafeTimer = UnityEngine.Random.Range(1.2f, 3f);
                _strafeDir = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            }
            if (!_hasLos || dist > maxD)
            {
                // Never seen yet: head for the target itself (the last known position starts at the world origin).
                _goal = (_lastSeen < 0f || Time.time - _lastSeen < 5f) ? Target.transform.position : _lastKnown;
                _hasGoal = true;
                speed = dist > 25f ? RunSpeed : WalkSpeed * 1.6f;
            }
            else if (dist < minD)
            {
                _goal = transform.position - flatTo.normalized * 3f + MathUtil.RightOf(flatTo.normalized) * _strafeDir * 2f;
                _hasGoal = true;
                speed = WalkSpeed * 1.5f;
            }
            else
            {
                _goal = transform.position + MathUtil.RightOf(flatTo.normalized) * _strafeDir * 2f;
                _hasGoal = true;
                speed = WalkSpeed;
            }
            // Spotting the player starts a reaction delay, then a few deliberate misses before aimed fire.
            bool canShoot = _hasLos && dist < d.Range;
            if (Target.IsPlayer)
            {
                if (canShoot && !_hadLosBefore)
                {
                    float react = CombatDirector.ReactionTime * UnityEngine.Random.Range(0.8f, 1.25f);
                    _reactUntil = Time.time + react;
                    _warnUntil = _reactUntil + UnityEngine.Random.Range(0.5f, 1.1f);
                }
                _hadLosBefore = canShoot;
            }
            if (canShoot)
            {
                _burstTimer -= dt;
                if (_burstTimer <= 0f)
                {
                    _bursting = !_bursting;
                    _burstTimer = _bursting ? UnityEngine.Random.Range(0.4f, 1.1f) : UnityEngine.Random.Range(0.7f, 1.8f);
                    if (!_bursting && Target.IsPlayer && UnityEngine.Random.value < 0.7f) CombatDirector.ReleaseShotToken(this);
                }
                if (_bursting && (!Target.IsPlayer || Time.time >= _reactUntil))
                {
                    float targetSpeed = Target.Velocity.magnitude;
                    float spread = (1.8f + dist * 0.07f + targetSpeed * 0.35f) * Accuracy * (Target.IsPlayer ? Abilities.EnemyAccuracyScale : 1f);
                    bool fire = true;
                    if (Target.IsPlayer)
                    {
                        spread *= CombatDirector.EnemySpread;
                        if (Game.Player != null && Game.Player.Crouched) spread *= 1.2f;
                        if (Time.time < _warnUntil) spread *= 3.5f;
                        // No attack token: suppressing fire that cracks past instead of hitting.
                        if (!CombatDirector.TryShotToken(this))
                        {
                            spread = Mathf.Max(spread * 4.5f, 9f);
                            // Story mode waits its turn; wide suppression can still accidentally hit the player.
                            fire = Settings.Difficulty != DifficultyLevel.Story && UnityEngine.Random.value < 0.45f;
                        }
                    }
                    if (fire)
                    {
                        Vector3 aim = targetPos + Target.Velocity * 0.1f;
                        if (d.Fire == FireKind.Spin) Weapons.HoldSpin();
                        Weapons.TryFire(aim, true, true, spread);
                    }
                }
            }
            return speed;
        }

        /// <summary>
        /// Melee enemies circle at a respectful distance, wait for an attack token (only a couple swing at once),
        /// then commit to a telegraphed attack with a readable wind-up and a punishable recovery.
        /// </summary>
        private float UpdateMeleeFight(float dt, WeaponDef d, Vector3 targetPos, Vector3 flatTo, float dist)
        {
            Anim.Aiming = false;
            _meleeReady = true;
            if (_attack != null) return UpdateNpcAttack(dt, d, targetPos);
            if (IsCreature) return UpdateCreatureFight(dt, targetPos, flatTo, dist);
            bool vsPlayer = Target.IsPlayer;
            float engage = Mathf.Max(2.6f, d.MeleeRange + 1.2f);
            if (dist > engage + 3f || !_hasLos)
            {
                CombatDirector.ReleaseMeleeToken(this);
                _goal = Target.transform.position;
                _hasGoal = true;
                return dist > 6f ? RunSpeed : WalkSpeed * 1.5f;
            }
            FaceTowards(targetPos, dt);
            bool token = !vsPlayer || CombatDirector.TryMeleeToken(this);
            if (!token || Time.time < _nextAttackAt)
            {
                // Circle and hold distance, stepping in and out like a real fighter.
                _circleSwap -= dt;
                if (_circleSwap <= 0f)
                {
                    _circleSwap = UnityEngine.Random.Range(1.5f, 3.5f);
                    if (UnityEngine.Random.value < 0.4f) _circleDir = -_circleDir;
                }
                float hold = token ? engage : engage + 1.6f;
                Vector3 n = flatTo.normalized;
                Vector3 side = MathUtil.RightOf(n) * _circleDir;
                Vector3 radial = dist < hold - 0.6f ? -n : (dist > hold + 0.6f ? n : Vector3.zero);
                _goal = transform.position + (side * 1.4f + radial * 1.6f);
                _hasGoal = true;
                return WalkSpeed * 0.95f;
            }
            // Token in hand: close in, then strike.
            float reach = Mathf.Max(1.9f, d.MeleeRange + 0.9f);
            if (dist > reach)
            {
                _goal = Target.transform.position;
                _hasGoal = true;
                return WalkSpeed * 1.8f;
            }
            int idx = UnityEngine.Random.value < (Elite ? 0.45f : 0.3f) ? 1 : 0;
            MeleeMove move = MeleeMoves.EnemyMove(MeleeMoves.ClassOf(d.Id), idx, Elite);
            // H8S blow spacing: a swing on the player waits for the shared impact cadence instead of landing with another.
            if (vsPlayer && CadenceWaitsH8S(move)) return 0f;
            StartNpcAttack(move);
            return 0f;
        }

        private void StartNpcAttack(MeleeMove move)
        {
            _attack = move;
            _attack.Windup *= CombatDirector.EnemyWindupScale;
            _attackT = 0f;
            _attackHits.Clear();
            _hasGoal = false;
            Anim.AttackAnim = move.Anim;
            Anim.AttackMirror = false;
            Anim.AttackT = 0f;
            Anim.AttackWindup = move.Windup / move.Total;
            Anim.AttackActive = move.Active / move.Total;
            Anim.Telegraph = 0f;
            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.Swing, transform.position + Vector3.up * 1.4f, 0.3f, 0.7f, 18f);
        }

        private float UpdateNpcAttack(float dt, WeaponDef d, Vector3 targetPos)
        {
            MeleeMove m = _attack;
            float prev = _attackT;
            _attackT += dt;
            // Track during most of the wind-up, then commit to the direction.
            if (_attackT < m.Windup * 0.7f) FaceTowards(targetPos, dt * 0.6f);
            else _faceOverride = true;
            float lungeStart = m.Windup * 0.6f;
            float lungeEnd = m.Windup + m.Active;
            _forced = true;
            _forcedVel = Vector3.zero;
            if (_attackT > lungeStart && _attackT < lungeEnd && m.Lunge > 0f)
            {
                float speed = m.Lunge / Mathf.Max(0.05f, lungeEnd - lungeStart);
                float dist = Target != null ? MathUtil.FlatDistance(Target.transform.position, transform.position) : 99f;
                if (dist < 0.95f) speed = 0f;
                _forcedVel = transform.forward * speed;
            }
            // The glint: wind-up glow ramps up and flashes right before the hit lands.
            Anim.Telegraph = _attackT < m.Windup ? Mathf.Clamp01(_attackT / m.Windup) : Mathf.Max(0f, Anim.Telegraph - dt * 6f);
            if (prev < m.Windup * 0.75f && _attackT >= m.Windup * 0.75f && Game.Fx != null && m.Unblockable)
                Game.Fx.Flash(ChestPosition + transform.forward * 0.4f, CombatTellsH8S.DodgeFlashColor, 2.5f, 4f, 0.25f);
            if (_attackT >= m.Windup && prev < m.Windup + m.Active)
            {
                bool killed;
                float baseDmg = IsCreature ? CreatureStrikeBase(m) : Mathf.Max(d.Damage, 18f) * MeleeDamageScale;
                int before = _attackHits.Count;
                CombatDirector.Strike(this, m, baseDmg, 1f, d.Id, _attackHits, out killed,
                    Mathf.Clamp01((prev-m.Windup)/Mathf.Max(.01f,m.Active)),
                    Mathf.Clamp01((_attackT-m.Windup)/Mathf.Max(.01f,m.Active)));
                if (IsCreature && _attackHits.Count > before) OnCreatureStrikeLanded();
            }
            Anim.AttackT = Mathf.Clamp01(_attackT / m.Total);
            if (_attackT >= m.Total)
            {
                _attack = null;
                _forced = false;
                Anim.AttackT = -1f;
                Anim.Telegraph = 0f;
                CombatDirector.ReleaseMeleeToken(this);
                float rest = Settings.Difficulty == DifficultyLevel.Story ? UnityEngine.Random.Range(2.2f, 3.6f) : (Settings.Difficulty == DifficultyLevel.Balanced ? UnityEngine.Random.Range(1.3f, 2.6f) : UnityEngine.Random.Range(0.7f, 1.6f));
                _nextAttackAt = Time.time + rest * (Elite ? 0.7f : 1f);
            }
            return 0f;
        }

        // ------------------------------------------------------------------ companion

        private CharacterBase FindCompanionTarget()
        {
            CharacterBase best = null;
            float bd = 30f * 30f;
            foreach (NpcController n in All)
            {
                if (n == this || n.IsDead || !n.IsHostileToPlayer) continue;
                if (n.Role == NpcRole.Police || n.Role == NpcRole.Swat) continue;
                float d = (n.transform.position - transform.position).sqrMagnitude;
                if (d < bd)
                {
                    bd = d;
                    best = n;
                }
            }
            // Creatures hunting the player (packs, swarms, a surfaced wyrm) are fair game too; calm wildlife is left alone.
            for (int i = 0; i < Creature.All.Count; i++)
            {
                Creature c = Creature.All[i];
                if (c == null || !c.Targetable || !c.IsEngaged || c.Mode != CreatureMode.Hunt) continue;
                float d = (c.transform.position - transform.position).sqrMagnitude;
                if (d < bd)
                {
                    bd = d;
                    best = c;
                }
            }
            return best;
        }

        private float UpdateFollow(float dt)
        {
            PlayerController p = Game.Player;
            if (p == null) return 0f;
            if (p.InVehicle)
            {
                Vehicle v = p.CurrentVehicle;
                int seat = v.FreeSeat(false);
                float dv = MathUtil.FlatDistance(transform.position, v.transform.position);
                if (seat < 0) return 0f;
                if (dv < 3.5f || (dv > 25f && !Rig.Body.isVisible))
                {
                    EnterVehicleImmediate(v, seat);
                    return 0f;
                }
                _goal = v.DoorPoint(seat);
                _hasGoal = true;
                return dv > 6f ? RunSpeed : WalkSpeed * 1.5f;
            }
            float d = MathUtil.FlatDistance(transform.position, p.transform.position);
            if (d > 60f && !Rig.Body.isVisible)
            {
                transform.position = p.transform.position - p.transform.forward * 2f + p.transform.right * 1.5f;
                return 0f;
            }
            if (d > 3.2f)
            {
                _goal = p.transform.position - p.transform.forward * 1.2f + p.transform.right * 1.2f;
                _hasGoal = true;
                return d > 9f ? RunSpeed * 1.05f : (d > 5f ? WalkSpeed * 2f : WalkSpeed);
            }
            _hasGoal = false;
            FaceTowards(p.transform.position + p.transform.forward * 5f, dt);
            return 0f;
        }

        private void UpdateCompanionInVehicle()
        {
            // Seated companions (Luma in the kidnappers' van) stay put until the script moves them.
            if (State == NpcState.Seated) return;
            PlayerController p = Game.Player;
            if (p == null || !p.InVehicle || p.CurrentVehicle != CurrentVehicle)
            {
                ExitVehicleImmediate(false);
                SetState(NpcState.Follow);
            }
        }

        private float UpdateScripted(float dt)
        {
            if (!_hasGoal) return 0f;
            if (MathUtil.FlatSqrDistance(transform.position, _goal) < 0.6f)
            {
                _hasGoal = false;
                Action<NpcController> cb = OnScriptArrived;
                OnScriptArrived = null;
                if (cb != null)
                {
                    try { cb(this); }
                    catch (Exception e) { Log.Exception("NPC script arrival", e); }
                }
                return 0f;
            }
            return _goalSpeed;
        }

        // ================================================================== locomotion

        private void FaceTowards(Vector3 p, float dt)
        {
            Vector3 to = MathUtil.Flat(p - transform.position);
            if (to.sqrMagnitude < 0.01f) return;
            _yaw = Mathf.MoveTowardsAngle(_yaw, MathUtil.YawOf(to), 540f * dt);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            _faceOverride = true;
        }

        private bool _faceOverride;

        private bool _meleeReady;

        private void Move(float dt, float speed)
        {
            Vector3 pos = transform.position;
            Vector3 desired = Vector3.zero;
            if (_forced)
            {
                desired = _forcedVel;
                _vel = desired;
            }
            else if (_hasGoal && speed > 0f)
            {
                Vector3 to = MathUtil.Flat(_goal - pos);
                float d = to.magnitude;
                if (d > 0.15f)
                {
                    desired = to / d * Mathf.Min(speed, d * 3f + 0.4f);
                    desired = Avoid(pos, desired);
                }
            }
            float accel = speed > 3f ? 10f : 7f;
            _vel = Vector3.MoveTowards(_vel, desired, accel * dt);
            pos += _vel * dt;

            // Ground snap.
            RaycastHit hit;
            if (Physics.Raycast(pos + Vector3.up * 1.1f, Vector3.down, out hit, 3f, Layers.WorldMask | (1 << Layers.Debris), QueryTriggerInteraction.Ignore))
            {
                float gy = hit.point.y;
                if (pos.y - gy > 0.5f)
                {
                    _vy -= 19.6f * dt;
                    pos.y = Mathf.Max(gy, pos.y + _vy * dt);
                    _grounded = pos.y <= gy + 0.01f;
                }
                else
                {
                    pos.y = Mathf.Lerp(pos.y, gy, MathUtil.Damp(20f, dt));
                    _vy = 0f;
                    _grounded = true;
                }
            }
            else
            {
                _vy -= 19.6f * dt;
                pos.y += _vy * dt;
                _grounded = false;
                if (pos.y < WorldBounds.KillY)
                {
                    Destroy(gameObject);
                    return;
                }
            }
            transform.position = pos;
            if (!_faceOverride && _vel.sqrMagnitude > 0.05f)
            {
                _yaw = Mathf.MoveTowardsAngle(_yaw, MathUtil.YawOf(_vel), 480f * dt);
                transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            }
            _faceOverride = false;
            Anim.Velocity = _vel;
            Anim.Grounded = _grounded;
            Anim.SitWeight = 0f;
            Anim.CombatReady = Mathf.MoveTowards(Anim.CombatReady, _meleeReady ? 1f : 0f, dt * 3f);
            _meleeReady = false;
            Anim.Panic = Mathf.MoveTowards(Anim.Panic, State == NpcState.Flee ? 1f : 0f, dt);

            // Stuck detection: pick a different route.
            if (_hasGoal && speed > 0.5f)
            {
                // Frame-rate independent: "stuck" means covering less than 0.2 m per second while trying to move.
                if ((pos - _lastPos).magnitude < 0.2f * dt) _stuckTimer += dt;
                else _stuckTimer = 0f;
                if (_stuckTimer > 1.5f)
                {
                    _stuckTimer = 0f;
                    if (State == NpcState.Wander) { _node = -1; _hasGoal = false; }
                    else if (State == NpcState.Scripted)
                    {
                        // Keep the scripted destination; just shuffle sideways a little to get unstuck.
                        Vector2 r = UnityEngine.Random.insideUnitCircle.normalized * 0.6f;
                        transform.position = pos + new Vector3(r.x, 0f, r.y);
                    }
                    else
                    {
                        Vector2 r = UnityEngine.Random.insideUnitCircle.normalized * 4f;
                        _goal = pos + new Vector3(r.x, 0f, r.y);
                    }
                }
            }
            _lastPos = pos;
        }

        private Vector3 Avoid(Vector3 pos, Vector3 desired)
        {
            float speed = desired.magnitude;
            if (speed < 0.01f) return desired;
            Vector3 dir = desired / speed;
            int mask = Layers.WorldMask | (1 << Layers.Debris) | (1 << Layers.VehicleShell);
            Vector3 origin = pos + Vector3.up * 0.7f;
            float look = 0.9f + speed * 0.25f;
            if (!Physics.SphereCast(origin, 0.22f, dir, out _hit, look, mask, QueryTriggerInteraction.Ignore)) return desired;
            for (int i = 1; i <= 3; i++)
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector3 alt = Quaternion.Euler(0f, s * i * 35f, 0f) * dir;
                    if (!Physics.SphereCast(origin, 0.22f, alt, out _hit, look, mask, QueryTriggerInteraction.Ignore)) return alt * speed;
                }
            }
            return Vector3.zero;
        }

        private RaycastHit _hit;
    }
}
