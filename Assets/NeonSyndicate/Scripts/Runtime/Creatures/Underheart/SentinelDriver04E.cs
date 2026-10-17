using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// The Extraction Sentinel's role (Sentinel) on the humanoid creature brain (golem body, golem stone fists): it holds
    /// its post until the player engages its arena, leashes back when drawn too far, and adds scan and clamp. Tell: it stops,
    /// an amber scan fan sweeps from its scanner lens across the player for 1.3 s, then turns red for 0.35 s as it locks;
    /// if the player is still inside the fan with a clear line, the clamp fires along the lock line (it hits only if the
    /// player is still on that line 0.3 s later: dodge sideways, break line of sight, or raise a ward toward it, since the
    /// hit goes through the player's normal damage path). It then stands open for 1 s. Shooting the lens during a scan
    /// blinds it (scan cancelled, 1.5 s stagger); a broken scanner part ends scanning. Up close it stamps (the golem
    /// slam ring). Badly damaged it backs to its post and braces its front plate (frontal hits halved: flank it).
    /// The golem kit's own automatic slam and shard throw are switched off; this driver decides those moves.
    /// </summary>
    public sealed class SentinelDriver04E : MonoBehaviour, IDamageFilter
    {
        private enum ScanState { Idle, Sweep, Lock, Fire, Recover }

        public const float SweepSeconds = 1.3f;
        public const float LockSeconds = 0.35f;
        public const float ClampTravel = 0.3f;
        public const float FanHalfAngle = 25f;
        public const float ClampWidth = 1.1f;
        public const float LensRadius = 0.45f;
        public const float BraceFrontScale = 0.5f;

        public NpcController Npc;
        public UnderRoleDef04E Def;
        public SkirmishArena04E Arena;
        public Vector3 Post;

        private IDamageFilter _inner;
        private AccessoryRig04E _acc;
        private ScanState _scan;
        private float _sT;
        private float _nextScanAt;
        private float _nextSlamAt;
        private float _hurtChaseUntil;
        private float _think;
        private bool _holding;
        private bool _braced;
        private bool _withdrawn;
        private bool _deadHandled;
        private Vector3 _lockOrigin, _lockDir;
        private float _lockRange;
        private float _sweepBase;
        private LineRenderer _tether;
        private float _tetherUntil;
        private float _lastText = -99f;
        private SkirmishArena04E _loose;
        private static Material _tetherMat;

        private static readonly Color FanAmber = new Color(1f, 0.7f, 0.2f, 0.45f);
        private static readonly Color FanRed = new Color(1f, 0.15f, 0.1f, 0.6f);

        public static void ResetStatics()
        {
            _tetherMat = null;
        }

        public void Init(NpcController n, UnderRoleDef04E def)
        {
            Npc = n;
            Def = def;
            Post = n != null ? n.transform.position : Vector3.zero;
            _nextScanAt = Time.time + Random.Range(3f, 5f);
            _nextSlamAt = Time.time + Random.Range(2f, 4f);
            if (n == null) return;
            if (n.CreatureKit != null) n.CreatureKit.ScriptedOnly = true;
            if (n.Health != null)
            {
                _inner = n.Health.Filter;
                n.Health.Filter = this;
            }
            _acc = UnderheartAccessories04E.AttachSentinel(n);
            if (_tetherMat == null) _tetherMat = Mats.Additive("Sentinel Clamp Tether", new Color(1f, 0.6f, 0.15f, 0.9f), UIKit.Circle);
            GameObject go = new GameObject("Clamp Tether");
            go.transform.SetParent(n.transform, false);
            _tether = go.AddComponent<LineRenderer>();
            _tether.sharedMaterial = _tetherMat;
            _tether.positionCount = 2;
            _tether.useWorldSpace = true;
            _tether.startWidth = 0.09f;
            _tether.endWidth = 0.05f;
            _tether.shadowCastingMode = ShadowCastingMode.Off;
            _tether.receiveShadows = false;
            _tether.enabled = false;
        }

        public void Bind(SkirmishArena04E arena, Vector3 post)
        {
            Arena = arena;
            Post = post;
        }

        private SkirmishArena04E ArenaOrLoose
        {
            get
            {
                if (Arena != null) return Arena;
                if (_loose == null) _loose = new SkirmishArena04E { Center = Post, Radius = CreatureRoleDriver04E.LooseArenaRadius };
                return _loose;
            }
        }

        private void OnDestroy()
        {
            StopThreats();
            if (Npc != null && Npc.Health != null && ReferenceEquals(Npc.Health.Filter, this)) Npc.Health.Filter = _inner;
        }

        // ================================================================== damage

        /// <summary>Lens shots blind a scan; a braced plate halves frontal hits; then the golem kit's own rules.</summary>
        public bool FilterDamage(ref DamageInfo info)
        {
            if (Npc != null && !Npc.IsDead)
            {
                bool shot = info.Type == DamageType.Bullet || info.Type == DamageType.Power;
                if (shot && _acc != null && _acc.Lens != null)
                {
                    float r = LensRadius * Mathf.Max(0.6f, Npc.Rig != null ? Npc.Rig.Scale : 1f);
                    if ((info.Point - _acc.Lens.position).sqrMagnitude <= r * r)
                    {
                        info.Headshot = true;
                        if (_scan == ScanState.Sweep || _scan == ScanState.Lock) Blind();
                    }
                }
                if (_braced && Frontal(info) && (info.Type == DamageType.Bullet || info.Type == DamageType.Melee || info.Type == DamageType.Power))
                {
                    info.Amount *= BraceFrontScale;
                    if (info.FromPlayer) Say("Braced plate: hit it from the side", new Color(1f, 0.8f, 0.45f));
                }
                if (info.FromPlayer) _hurtChaseUntil = Time.time + SkirmishRules04E.HurtChaseSeconds;
            }
            return _inner == null || _inner.FilterDamage(ref info);
        }

        private bool Frontal(DamageInfo info)
        {
            Vector3 from = info.Source != null ? info.Source.transform.position - Npc.transform.position : -info.Direction;
            from.y = 0f;
            if (from.sqrMagnitude < 0.01f) return true;
            return Vector3.Angle(Npc.transform.forward, from) < 70f;
        }

        private void Blind()
        {
            EndScan(true);
            Npc.CreatureStagger(1.5f);
            Underheart04ESounds.Play(UhSfx.PlateCrack, Npc.HeadPosition, 0.8f, 1.2f);
            Say("Scanner blinded", new Color(1f, 0.85f, 0.4f));
        }

        // ================================================================== frame

        private void Update()
        {
            if (Npc == null || Def == null)
            {
                Destroy(this);
                return;
            }
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (Npc.IsDead)
            {
                if (!_deadHandled)
                {
                    _deadHandled = true;
                    StopThreats();
                    if (_acc != null) _acc.SetGlow(0.15f);
                }
                return;
            }
            UpdateTether();
            SkirmishArena04E a = ArenaOrLoose;
            if (a.Ending || _withdrawn)
            {
                StopThreats();
                Hold();
                return;
            }
            if (Arena == null) a.PlayerEngaged = Game.Player != null && !Game.Player.IsDead && SkirmishRules04E.PlayerEngaged(Game.PlayerPosition, a.Center, a.Radius);
            TickScan(dt);
            _think -= dt;
            if (_think > 0f) return;
            _think = 0.15f + Random.value * 0.05f;
            Decide(a);
        }

        private void Decide(SkirmishArena04E a)
        {
            PlayerController p = Game.Player;
            bool freeSpawn = Arena == null && (Npc.CreatureOptions == null || !Npc.CreatureOptions.Hostile);
            if (!freeSpawn)
            {
                bool engaged = a.PlayerEngaged || Time.time < _hurtChaseUntil;
                bool leashed = SkirmishRules04E.BeyondLeash(Npc.transform.position, a.Center, a.Radius);
                if (leashed || !engaged || p == null || p.IsDead) Hold();
                else Release();
            }
            if (_holding || p == null || p.IsDead || !Npc.CreatureEngaged) return;
            float d = MathUtil.FlatDistance(p.transform.position, Npc.transform.position);
            // Badly damaged: back to the post and brace the front plate for the rest of the fight.
            if (!_braced && Npc.Health.MaxHealth > 0f && Npc.Health.Health / Npc.Health.MaxHealth < Def.RetreatHealth)
            {
                _braced = true;
                Npc.Home = Post;
                Say("The sentinel braces its plate", new Color(1f, 0.8f, 0.45f));
                Underheart04ESounds.Play(UhSfx.MachineStep, Npc.ChestPosition, 0.8f, 0.8f);
            }
            if (_scan != ScanState.Idle) return;
            CreatureAbilityKit kit = Npc.CreatureKit;
            bool free = !Npc.IsStaggered && !Npc.CreatureAttacking && (kit == null || !kit.Busy);
            if (!free) return;
            if (d < 3.5f && Time.time >= _nextSlamAt && kit != null)
            {
                _nextSlamAt = Time.time + Random.Range(6f, 8f);
                kit.StartSlam(1f, 1f, 1.3f);
                return;
            }
            bool scanner = Npc.Health.Parts == null || !Npc.Health.Parts.Has(PartEffect.NoAbility);
            if (scanner && Time.time >= _nextScanAt && UnderheartRoles04E.InSignatureRange(Def, d) && LineClear(p)
                && ArenaOrLoose.TryClaimSignature(Time.time, SweepSeconds + LockSeconds + ClampTravel)) BeginScan(p);
        }

        /// <summary>Back to the post and calm: it stops fighting until the player engages the arena again.</summary>
        private void Hold()
        {
            if (Npc == null || Npc.IsDead) return;
            EndScan(false);
            Npc.Home = Post;
            if (_holding && Npc.State != NpcState.Fight) return;
            _holding = true;
            if (Npc.CreatureOptions != null) Npc.CreatureOptions.Hostile = false;
            Npc.CreatureProvoked = false;
            Npc.Hostile = false;
            Npc.Target = null;
            Npc.CreatureCancelAttack();
            if (Npc.CreatureKit != null) Npc.CreatureKit.Cancel();
            Npc.SetState(NpcState.Idle);
        }

        private void Release()
        {
            if (!_holding) return;
            _holding = false;
            if (Npc.CreatureOptions != null) Npc.CreatureOptions.Hostile = true;
        }

        private bool LineClear(PlayerController p)
        {
            Vector3 from = _acc != null && _acc.Lens != null ? _acc.Lens.position : Npc.HeadPosition;
            return !Physics.Linecast(from, p.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore);
        }

        // ================================================================== scan and clamp

        private void BeginScan(PlayerController p)
        {
            _scan = ScanState.Sweep;
            _sT = 0f;
            Npc.CreatureCancelAttack();
            Npc.CreatureBusyUntil = Time.time + SweepSeconds + LockSeconds + ClampTravel + Def.PunishSeconds;
            _sweepBase = Random.value < 0.5f ? -1f : 1f;
            Underheart04ESounds.Play(UhSfx.SentinelScan, Npc.HeadPosition, 0.9f, 1f);
        }

        private void TickScan(float dt)
        {
            if (_scan == ScanState.Idle) return;
            PlayerController p = Game.Player;
            if (p == null || p.IsDead || Npc.IsStaggered)
            {
                EndScan(true);
                return;
            }
            _sT += dt;
            float range = Def.RangeMax + 2f;
            switch (_scan)
            {
                case ScanState.Sweep:
                {
                    float k = Mathf.Clamp01(_sT / SweepSeconds);
                    float sweep = _sweepBase * Mathf.Lerp(-30f, 30f, k) * (1f - k * 0.6f);
                    SetFan(true, FanAmber, sweep, range);
                    if (Npc.Anim != null) Npc.Anim.Telegraph = 0.4f + 0.5f * k;
                    if (_acc != null) _acc.SetGlow(1f + 1.5f * k);
                    if (_sT < SweepSeconds) return;
                    _scan = ScanState.Lock;
                    _sT = 0f;
                    return;
                }
                case ScanState.Lock:
                    SetFan(true, FanRed, 0f, range);
                    if (Npc.Anim != null) Npc.Anim.Telegraph = 1f;
                    if (_sT < LockSeconds) return;
                    if (!TryLock(p, range))
                    {
                        EndScan(true);
                        return;
                    }
                    _scan = ScanState.Fire;
                    _sT = 0f;
                    SetFan(false, FanRed, 0f, range);
                    Underheart04ESounds.Play(UhSfx.RescueClamp, _lockOrigin, 1f, 0.8f);
                    return;
                case ScanState.Fire:
                    if (_sT < ClampTravel) return;
                    ResolveClamp(p);
                    _scan = ScanState.Recover;
                    _sT = 0f;
                    return;
                case ScanState.Recover:
                    if (Npc.Anim != null) Npc.Anim.Telegraph = Mathf.MoveTowards(Npc.Anim.Telegraph, 0f, dt * 2f);
                    if (_sT < Def.PunishSeconds) return;
                    EndScan(false);
                    return;
            }
        }

        /// <summary>At the end of the sweep: is the player inside the fan with a clear line? Then the clamp locks on that line.</summary>
        private bool TryLock(PlayerController p, float range)
        {
            Vector3 lens = _acc != null && _acc.Lens != null ? _acc.Lens.position : Npc.HeadPosition;
            Vector3 to = p.ChestPosition - lens;
            Vector3 flatTo = MathUtil.Flat(to);
            float angle = flatTo.sqrMagnitude > 0.01f ? Vector3.Angle(MathUtil.Flat(Npc.transform.forward), flatTo) : 0f;
            bool clear = !Physics.Linecast(lens, p.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore);
            if (!UnderheartRoles04E.SentinelLocks(angle, to.magnitude, FanHalfAngle, range, clear)) return false;
            _lockOrigin = lens;
            _lockDir = to.normalized;
            _lockRange = Mathf.Min(range, to.magnitude + 2f);
            return true;
        }

        /// <summary>The clamp arrives along the lock line: a hit if the player is still on it, through the normal damage path.</summary>
        private void ResolveClamp(PlayerController p)
        {
            Vector3 c = p.ChestPosition;
            Vector3 rel = c - _lockOrigin;
            float along = Vector3.Dot(rel, _lockDir);
            Vector3 lateral = rel - _lockDir * along;
            bool clear = !Physics.Linecast(_lockOrigin, c, Layers.WorldMask, QueryTriggerInteraction.Ignore);
            bool hit = along > 0f && along <= _lockRange && UnderheartRoles04E.ClampHits(lateral.magnitude, ClampWidth, clear);
            Vector3 end = hit ? c : _lockOrigin + _lockDir * _lockRange;
            if (!hit)
            {
                RaycastHit wall;
                if (Physics.Raycast(_lockOrigin, _lockDir, out wall, _lockRange, Layers.WorldMask, QueryTriggerInteraction.Ignore)) end = wall.point;
            }
            ShowTether(end);
            if (!hit) return;
            float raw = (Npc.CreatureSpecies != null ? Npc.CreatureSpecies.Damage : 20f) * 1.2f * Mathf.Max(0.05f, Npc.CreatureOptions != null ? Npc.CreatureOptions.DamageScale : 1f);
            if (p.InVehicle && p.CurrentVehicle != null)
            {
                p.CurrentVehicle.ApplyDamage(raw * 2f, Npc.gameObject);
                return;
            }
            raw = CreatureBrainLogic.CapHit(raw, p.Health.MaxHealth, (int)Settings.Difficulty, CombatDirector.PlayerDamageTaken);
            DamageInfo info = DamageInfo.Make(raw, c, _lockDir, Npc.gameObject, DamageType.Melee);
            info.Force = 7f;
            info.Flags = DamageFlags.Knockdown;
            info.Poise = raw * 0.5f;
            p.Health.TakeDamage(info);
            if (p.Health.LastBlocked) Say("Clamp turned aside", new Color(0.7f, 0.95f, 1f));
            else
            {
                Underheart04ESounds.Play(UhSfx.ClampBreak, c, 0.9f, 0.7f);
                if (Game.Cam != null) Game.Cam.Shake(0.3f);
            }
        }

        private void ShowTether(Vector3 end)
        {
            if (_tether == null) return;
            _tether.enabled = true;
            _tether.SetPosition(0, _lockOrigin);
            _tether.SetPosition(1, end);
            _tetherUntil = Time.time + 0.45f;
        }

        private void UpdateTether()
        {
            if (_tether != null && _tether.enabled && Time.time > _tetherUntil) _tether.enabled = false;
        }

        private void SetFan(bool on, Color c, float yaw, float range)
        {
            if (_acc != null) _acc.SetFan(on, c, yaw, range);
        }

        private void EndScan(bool cooldownShort)
        {
            if (_scan == ScanState.Idle) return;
            _scan = ScanState.Idle;
            SetFan(false, FanAmber, 0f, 1f);
            if (Npc != null)
            {
                if (Npc.Anim != null) Npc.Anim.Telegraph = 0f;
                Npc.CreatureBusyUntil = Mathf.Min(Npc.CreatureBusyUntil, Time.time);
            }
            if (_acc != null) _acc.SetGlow(1f);
            _nextScanAt = Time.time + (cooldownShort ? Def.CooldownMin * 0.6f : UnderheartRoles04E.Cooldown(Def, Random.value));
        }

        /// <summary>Stops every owned threat (scan, lock, a clamp in flight, the tether). Safe to call repeatedly.</summary>
        public void StopThreats()
        {
            EndScan(false);
            if (_tether != null) _tether.enabled = false;
        }

        /// <summary>An aborted encounter: back to the post for good until removed.</summary>
        public void Withdraw()
        {
            _withdrawn = true;
            StopThreats();
            Hold();
        }

        private void Say(string text, Color color)
        {
            if (Game.Hud == null || Time.time - _lastText < 3f) return;
            if (MathUtil.FlatDistance(Npc.transform.position, Game.PlayerPosition) > 35f) return;
            _lastText = Time.time;
            Game.Hud.CombatText(text, color);
        }
    }
}
