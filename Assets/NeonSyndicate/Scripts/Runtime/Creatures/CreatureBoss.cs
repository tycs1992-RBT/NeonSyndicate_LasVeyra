using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// A boss's fight script on top of its creature NPC. Three phases (at two thirds and one third of its health), each
    /// opened by a short transition: a roar and a summon, and for the Glacier Titan a kneel that leaves its core open.
    /// Area attacks are ground rings that fill, then burst, so the player always sees where not to stand and for how
    /// long; one hit never takes more than the difficulty's share of the player's health. Per boss:
    /// Glacier Titan: stomp shockwave, ice spikes under and ahead of you, a frost-breath line; summons Snow Hollows.
    /// Mirage Djinn: sandstorm rings and packed-sand volleys; mirages that cast no shadow and pop at a touch; Sand Golems.
    /// Shard Golem Foreman: hazard plates that halve damage until a stagger breaks them, lightning stuns its crown, slams
    /// in widening rings, crystal rain; calls Glass Mite swarms. Wraith Queen: glass shattering across the floor and
    /// mirror copies that share her glass guard (break one and they all crack).
    /// 04D: the Foreman, the Djinn and the Queen run the upgraded fights in the CreatureBoss.*04D partials (their
    /// BossProfiles04D profile: signature lessons, telegraphs, exposed windows, a phase latch, a finite support budget and
    /// complete cleanup). The Glacier Titan has no profile and runs the original script below unchanged; the original
    /// Foreman, Djinn and Queen moves stay as the fallback and as fillers of the upgraded fights.
    /// </summary>
    public sealed partial class CreatureBoss : MonoBehaviour
    {
        public const int PhaseCount = 3;
        private static readonly string[] PhaseNames = { "PHASE 1 / 3", "PHASE 2 / 3", "PHASE 3 / 3" };

        public NpcController Npc;
        public CreatureSpecies Species;
        public Vector3 ArenaCenter;
        public float ArenaRadius;
        public string AddsTag;

        public int Phase { get; private set; }
        /// <summary>The Titan kneels between phases: its core takes extra damage.</summary>
        public bool Kneeling { get { return Time.time < _kneelUntil; } }
        /// <summary>The Foreman's hazard plates still soak most of each hit.</summary>
        public bool PlatesUp { get { return Kind == HumanoidCreature.GolemForeman && !_platesBroken; } }
        public HumanoidCreature Kind { get { return Species != null ? Species.Humanoid : HumanoidCreature.None; } }

        private sealed class Hazard
        {
            public GroundRing Ring;
            public Vector3 Center;
            public float Radius;
            public float Delay;
            public float Fill;
            public float T;
            public float Damage;
            public DamageFlags Flags;
            public int Fx;
            public bool Shown;
            /// <summary>Part of the Foreman's widening slam: a stagger that breaks the slam takes these rings with it.</summary>
            public bool Slam;
            /// <summary>04D: a support actor claws up out of this ring when it bursts (the Foreman's one support arrival).</summary>
            public bool Arrival;
        }

        private const int FxIce = 0, FxSand = 1, FxCrystal = 2, FxGlass = 3;

        private CreatureAbilityKit _kit;
        private float _nextMove;
        private float _kneelUntil;
        private bool _platesBroken;
        private bool _wasStaggered;
        private float _breathUntil;
        private Vector3 _breathDir;
        private float _breathFx;
        private float _copiesUntil;
        private float _glowUntil;
        private bool _glowing;
        private bool _cleaned;
        private float _ringHitAt = -9f;
        private readonly List<Hazard> _hazards = new List<Hazard>();
        private readonly List<CreatureAbilityKit> _guards = new List<CreatureAbilityKit>();
        private readonly List<NpcController> _copies = new List<NpcController>();

        public void Init(NpcController npc, CreatureSpecies sp, Vector3 arenaCenter, float arenaRadius, string addsTag)
        {
            Npc = npc;
            Species = sp;
            ArenaCenter = arenaCenter;
            ArenaRadius = arenaRadius;
            AddsTag = addsTag;
            _kit = npc.CreatureKit;
            _nextMove = Time.time + 4.5f;
            if (_kit != null)
            {
                if (Kind == HumanoidCreature.WraithQueen)
                {
                    _guards.Add(_kit);
                    _kit.GuardGroup = _guards;
                }
                if (Kind == HumanoidCreature.GolemForeman) _kit.DamageTakenScale = 0.5f;
            }
            npc.Health.Damaged += OnDamaged;
            if(Regional04OD) { InitRegional04OD(); return; }
            InitFight04D();
        }

        private void OnDestroy()
        {
            if (Npc != null && Npc.Health != null) Npc.Health.Damaged -= OnDamaged;
            Cleanup();
        }

        /// <summary>Phase name for the boss bar.</summary>
        public string PhaseLabel
        {
            get
            {
                if (Regional04OD) return RegionalLabel04OD;
                if (Upgraded04D) return PhaseLabel04D;
                if (Kneeling) return "KNEELING: STRIKE THE CORE";
                if (PlatesUp) return "HAZARD PLATES UP";
                return PhaseNames[Mathf.Clamp(Phase, 0, PhaseNames.Length - 1)];
            }
        }

        // ================================================================== frame

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (Npc == null || Npc.IsDead)
            {
                Cleanup();
                return;
            }
            if (Regional04OD) { UpdateRegional04OD(dt); return; }
            if (Upgraded04D)
            {
                Update04D(dt);
                return;
            }
            if (Npc.IsStaggered) DropSlamRings();
            UpdateHazards(dt);
            UpdatePhase();
            UpdateSpecial(dt);
            if (!Npc.CreatureEngaged || Npc.Target == null) return;
            float now = Time.time;
            if (now < _nextMove || now < Npc.CreatureBusyUntil || Kneeling) return;
            if (Npc.IsStaggered || Npc.CreatureAttacking || Npc.IsRagdolled || (_kit != null && _kit.Busy)) return;
            DoMove();
        }

        private void UpdatePhase()
        {
            float f = Npc.Health.Health / Mathf.Max(1f, Npc.Health.MaxHealth);
            int want = f < 0.33f ? 2 : (f < 0.66f ? 1 : 0);
            if (want > Phase) EnterPhase(want);
        }

        private void EnterPhase(int p)
        {
            Phase = p;
            _nextMove = Time.time + 3.5f;
            Npc.CreatureCancelAttack();
            CreatureAudio.Play(Species, CreatureCry.Roar, Npc.HeadPosition, Npc.BodyHeight * 1.5f);
            if (Game.Cam != null) Game.Cam.Shake(0.45f);
            Vector3 c = Npc.transform.position;
            switch (Kind)
            {
                case HumanoidCreature.GlacierTitan:
                    _kneelUntil = Time.time + 3.4f;
                    Npc.CreatureStagger(3.4f);
                    if (_kit != null) _kit.DamageTakenScale = 1.6f;
                    Callout("The Titan kneels: burn its core!");
                    Summon("snow_hollow", p == 1 ? 2 : 3, FxIce);
                    break;
                case HumanoidCreature.MirageDjinn:
                    Callout(p == 1 ? "The Djinn splits into mirages" : "More mirages: find the one with a shadow");
                    SpawnCopies("mirage_djinn", p == 1 ? 2 : 3);
                    if (p == 1) Summon("sand_golem", 2, FxSand);
                    break;
                case HumanoidCreature.GolemForeman:
                    Callout("The Foreman calls the swarm");
                    if (Game.Creatures != null)
                    {
                        CreatureSpawnOptions o = AddOptions();
                        o.Seed = 3 + p;
                        Vector3 right = MathUtil.RightOf(Npc.transform.forward);
                        Vector3 at;
                        if (!AddSpot(c, 4f, 7f, Mathf.Atan2(right.z, right.x), out at)) at = c;
                        Game.Creatures.SpawnGroup("glass_mite", at, 8 + 4 * p, o);
                        if (Game.Fx != null) Game.Fx.CrystalBurst(at + Vector3.up, Species.HitColor, 30, 2f);
                    }
                    if (p >= 2) BreakPlates();
                    break;
                case HumanoidCreature.WraithQueen:
                    Callout("The Queen splits your reflection");
                    SpawnCopies("mirror_wraith", p == 1 ? 2 : 3);
                    break;
            }
        }

        private void UpdateSpecial(float dt)
        {
            if (_kneelUntil > 0f && Time.time >= _kneelUntil)
            {
                _kneelUntil = 0f;
                if (_kit != null) _kit.DamageTakenScale = 1f;
            }
            if (Kind == HumanoidCreature.GolemForeman && !_platesBroken)
            {
                bool st = Npc.IsStaggered;
                if (st && !_wasStaggered) BreakPlates();
                _wasStaggered = st;
            }
            if (Time.time < _breathUntil)
            {
                _breathFx -= dt;
                if (_breathFx <= 0f && Game.Fx != null)
                {
                    _breathFx = 0.035f;
                    Vector3 mouth = Npc.HeadPosition + Npc.transform.forward * 0.6f * Npc.Rig.Scale;
                    Vector3 v = (_breathDir + Random.insideUnitSphere * 0.12f) * 16f + Vector3.down * 2f;
                    Game.Fx.BreathPuff(mouth, v, new Color(0.7f, 0.92f, 1f), 1.1f, false);
                }
            }
            if (_copies.Count > 0 && Time.time > _copiesUntil) ClearCopies(true);
            // The body glows while it channels a move or kneels; the glow goes out with it.
            float now = Time.time;
            if (Kneeling || now < _glowUntil)
            {
                Npc.Anim.Telegraph = Kneeling ? 0.6f + 0.4f * Mathf.Sin(now * 8f) : 0.75f + 0.25f * Mathf.Sin(now * 14f);
                _glowing = true;
            }
            else if (_glowing)
            {
                _glowing = false;
                if (_kit == null || !_kit.Busy) Npc.Anim.Telegraph = 0f;
            }
        }

        private void OnDamaged(DamageInfo info)
        {
            if (Npc == null || Npc.IsDead) return;
            // Lightning jams the Foreman's crown: crystal powers stun it far sooner than blades.
            if (Kind == HumanoidCreature.GolemForeman && info.Type == DamageType.Power)
            {
                Npc.Poise -= Npc.Health.LastApplied * 1.2f + 25f;
                if (Npc.Poise <= 0f)
                {
                    Npc.Poise = Npc.MaxPoise;
                    Npc.CreatureStagger(1.4f);
                    Callout("Crown stunned");
                }
            }
        }

        private void BreakPlates()
        {
            if (_platesBroken) return;
            _platesBroken = true;
            if (_kit != null) _kit.DamageTakenScale = 1.15f;
            if (Game.Fx != null) Game.Fx.RockBurst(Npc.ChestPosition, new Color(1f, 0.8f, 0.2f), 26, 2.5f);
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.CrashHeavy, Npc.ChestPosition, 0.8f, 0.1f, 60f);
            Callout("Hazard plates shattered: hit the core!");
        }

        private static void Callout(string text)
        {
            if (Game.Hud != null) Game.Hud.CombatText(text, new Color(1f, 0.75f, 0.4f));
        }

        // ================================================================== moves

        private void DoMove()
        {
            CharacterBase t = Npc.Target;
            Vector3 tp = t.transform.position;
            Vector3 me = Npc.transform.position;
            float s = Mathf.Max(1f, Npc.Rig.Scale);
            float d = MathUtil.FlatDistance(tp, me);
            float roll = Random.value;
            switch (Kind)
            {
                case HumanoidCreature.GlacierTitan:
                    if (d < 3.4f * s && roll < 0.55f && _kit != null) _kit.StartSlam(1f, 1.3f, 1.5f);
                    else if (Phase >= 1 && roll < 0.75f) FrostBreath(tp);
                    else IceSpikes(t, 3 + Phase);
                    break;
                case HumanoidCreature.MirageDjinn:
                    if (roll < 0.5f) Sandstorm(tp, 5 + Phase);
                    else if (_kit != null) _kit.StartThrow();
                    break;
                case HumanoidCreature.GolemForeman:
                    if (d < 10f && roll < 0.55f) RingSlam();
                    else CrystalRain(t, 6 + 2 * Phase);
                    break;
                case HumanoidCreature.WraithQueen:
                    if (roll < 0.7f) GlassShatter(t, 6 + 2 * Phase);
                    else if (_copies.Count == 0 && Phase > 0) SpawnCopies("mirror_wraith", Phase + 1);
                    else GlassShatter(t, 5 + Phase);
                    break;
            }
            float gap = (6.5f - Phase * 1.1f) * Mathf.Lerp(0.85f, 1.15f, Random.value) * Mathf.Clamp(CombatDirector.EnemyWindupScale, 0.8f, 1.5f);
            _nextMove = Time.time + gap;
        }

        /// <summary>Stands and channels for a moment (no swings while the rings it called fill).</summary>
        private void Cast(float seconds)
        {
            Npc.CreatureBusyUntil = Mathf.Max(Npc.CreatureBusyUntil, Time.time + seconds);
            Npc.CreatureCancelAttack();
            _glowUntil = Time.time + seconds;
            CreatureAudio.Play(Species, CreatureCry.Attack, Npc.HeadPosition, Npc.BodyHeight);
        }

        private void IceSpikes(CharacterBase t, int n)
        {
            Cast(0.9f);
            Vector3 tp = t.transform.position;
            Vector3 v = MathUtil.Flat(t.Velocity);
            for (int i = 0; i < n; i++)
            {
                Vector3 c = i == 0 ? tp : tp + v * (0.5f + 0.25f * i) + Flat(Random.insideUnitCircle * 4.5f);
                AddRing(c, 2.3f, i * 0.22f, 1.15f, 0.9f, DamageFlags.Unblockable | DamageFlags.Heavy, FxIce);
            }
        }

        private void FrostBreath(Vector3 tp)
        {
            Cast(1.5f);
            Vector3 me = Npc.transform.position;
            Vector3 dir = MathUtil.Flat(tp - me);
            dir = dir.sqrMagnitude > 0.01f ? dir.normalized : Npc.transform.forward;
            float s = Mathf.Max(1f, Npc.Rig.Scale);
            for (int i = 0; i < 5; i++) AddRing(me + dir * (2f * s + i * 3.4f), 2.4f, i * 0.12f, 0.95f, 0.8f, DamageFlags.Unblockable, FxIce);
            _breathDir = dir;
            _breathUntil = Time.time + 1.6f;
        }

        private void Sandstorm(Vector3 tp, int n)
        {
            Cast(1f);
            if (Game.Fx != null) Game.Fx.SandBurst(Npc.ChestPosition, 2f, 18);
            for (int i = 0; i < n; i++)
            {
                Vector3 c = i == 0 ? tp : tp + Flat(Random.insideUnitCircle * 9f);
                AddRing(c, 2.8f, Random.value * 0.8f, 1.3f, 0.75f, DamageFlags.Unblockable, FxSand);
            }
        }

        private void RingSlam()
        {
            if (_kit == null || !_kit.StartSlam(1.05f, 1.2f, 1.3f)) return;
            Vector3 c = Npc.transform.position;
            float delay = 1.2f * CombatDirector.EnemyWindupScale;
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                AddRing(c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 9f, 2.6f, delay * 0.6f, 1f, 0.8f, DamageFlags.Unblockable | DamageFlags.Heavy, FxCrystal).Slam = true;
            }
            if (Phase < 1) return;
            for (int i = 0; i < 8; i++)
            {
                float a = (i + 0.5f) / 8f * Mathf.PI * 2f;
                AddRing(c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 13.5f, 2.6f, delay * 0.6f + 0.8f, 1f, 0.8f, DamageFlags.Unblockable | DamageFlags.Heavy, FxCrystal).Slam = true;
            }
        }

        /// <summary>The slam was broken (a stagger, the stunned crown): its rings that have not burst fade harmlessly.</summary>
        private void DropSlamRings()
        {
            for (int i = _hazards.Count - 1; i >= 0; i--)
            {
                Hazard h = _hazards[i];
                if (!h.Slam) continue;
                if (h.Ring != null) h.Ring.Release();
                _hazards.RemoveAt(i);
            }
        }

        private void CrystalRain(CharacterBase t, int n)
        {
            Cast(0.8f);
            Vector3 tp = t.transform.position;
            for (int i = 0; i < n; i++)
            {
                Vector3 c = i == 0 ? tp : tp + Flat(Random.insideUnitCircle * 7f);
                AddRing(c, 1.9f, i * 0.15f, 1.1f, 0.6f, DamageFlags.Unblockable, FxCrystal);
            }
        }

        private void GlassShatter(CharacterBase t, int n)
        {
            Cast(0.9f);
            Vector3 tp = t.transform.position;
            for (int i = 0; i < n; i++)
            {
                Vector3 c = i == 0 ? tp : tp + Flat(Random.insideUnitCircle * 8f);
                AddRing(c, 2.6f, i * 0.12f, 1.2f, 0.75f, DamageFlags.Unblockable, FxGlass);
            }
        }

        private static Vector3 Flat(Vector2 v)
        {
            return new Vector3(v.x, 0f, v.y);
        }

        // ================================================================== summons and copies

        private CreatureSpawnOptions AddOptions()
        {
            CreatureSpawnOptions o = new CreatureSpawnOptions();
            o.Tag = AddsTag;
            o.Hostile = true;
            o.Persistent = true;
            return o;
        }

        /// <summary>Walls, props and cars a summoned body must not appear inside or behind.</summary>
        private static int AddBlockMask { get { return Layers.WorldMask | (1 << Layers.Debris) | (1 << Layers.Vehicles); } }

        /// <summary>
        /// A clear spot for a summon or a copy minR to maxR metres from the boss (first try at angle radians, then
        /// random ones): ground at about the boss's level, room for a body, and nothing solid between it and the boss
        /// (the pillars of Crystal Manor, the quarry's rocks). False when none of eight tries works.
        /// </summary>
        private static bool AddSpot(Vector3 me, float minR, float maxR, float angle, out Vector3 spot)
        {
            for (int k = 0; k < 8; k++)
            {
                float a = k == 0 ? angle : Random.value * Mathf.PI * 2f;
                Vector3 p = me + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Random.Range(minR, maxR);
                float g;
                if (!CreatureDirector.GroundAt(p, 4f, out g) || Mathf.Abs(g - me.y) > 3f) continue;
                p.y = g;
                if (Physics.CheckCapsule(p + Vector3.up * 1f, p + Vector3.up * 2.2f, 0.65f, AddBlockMask, QueryTriggerInteraction.Ignore)) continue;
                if (Physics.Linecast(me + Vector3.up * 1.2f, p + Vector3.up * 1.2f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                spot = p;
                return true;
            }
            spot = me;
            return false;
        }

        private void Summon(string speciesId, int n, int fx)
        {
            if (Game.Creatures == null) return;
            Vector3 me = Npc.transform.position;
            for (int i = 0; i < n; i++)
            {
                Vector3 p;
                if (!AddSpot(me, 6f, 10f, Random.value * Mathf.PI * 2f, out p)) continue;
                float yaw = Npc.Target != null ? MathUtil.YawOf(Npc.Target.transform.position - p) : Random.value * 360f;
                CharacterBase b = Game.Creatures.SpawnBossAdd(speciesId, p, yaw, AddOptions());
                if (b == null || Game.Fx == null) continue;
                if (fx == FxIce) Game.Fx.SnowBurst(b.transform.position + Vector3.up, 1.2f, 18);
                else if (fx == FxSand) Game.Fx.SandBurst(b.transform.position + Vector3.up, 1.5f, 18);
                else Game.Fx.CrystalBurst(b.transform.position + Vector3.up, Species.HitColor, 18, 1.5f);
            }
        }

        /// <summary>The Djinn's mirages (its own face, no shadow, gone at a touch) or the Queen's mirror copies (sharing her guard).</summary>
        private void SpawnCopies(string speciesId, int n)
        {
            if (Game.Creatures == null) return;
            ClearCopies(false);
            bool mirage = speciesId == "mirage_djinn";
            Vector3 me = Npc.transform.position;
            int faceSeed = Npc.CreatureOptions != null ? Npc.CreatureOptions.Seed : 0;
            for (int i = 0; i < n; i++)
            {
                // A mirage wears the boss's hero-detail face: only as many as there are bodies built ahead (more are
                // ordered as these are used), so no hero mesh is ever built on the main thread mid-fight.
                if (mirage && !Game.Creatures.BossAddReady(speciesId, faceSeed)) break;
                float a = (i + Random.value * 0.5f) / n * Mathf.PI * 2f;
                Vector3 p;
                if (!AddSpot(me, 6f, 9f, a, out p)) continue;
                CreatureSpawnOptions o = AddOptions();
                if (mirage)
                {
                    o.Seed = faceSeed;
                    o.HealthScale = 0.0005f;
                    o.DamageScale = 0.4f;
                    o.DisplayName = Npc.DisplayName;
                }
                else
                {
                    o.HealthScale = 0.6f;
                    o.DamageScale = 0.6f;
                }
                float yaw = Npc.Target != null ? MathUtil.YawOf(Npc.Target.transform.position - p) : Random.value * 360f;
                NpcController c = Game.Creatures.SpawnBossAdd(speciesId, p, yaw, o) as NpcController;
                if (c == null) continue;
                if (mirage)
                {
                    c.CreatureMirage = true;
                    if (c.Rig != null && c.Rig.Body != null) c.Rig.Body.shadowCastingMode = ShadowCastingMode.Off;
                    if (c.CreatureKit != null) c.CreatureKit.ScriptedOnly = true;
                    if (Game.Fx != null) Game.Fx.SandBurst(c.ChestPosition, 1.4f, 14);
                }
                else
                {
                    if (c.CreatureKit != null)
                    {
                        c.CreatureKit.GuardGroup = _guards;
                        if (!_guards.Contains(c.CreatureKit)) _guards.Add(c.CreatureKit);
                    }
                    if (Game.Fx != null) Game.Fx.GlassBurst(c.ChestPosition, Species.HitColor, 18);
                }
                _copies.Add(c);
            }
            _copiesUntil = Time.time + (mirage ? 22f : 40f);
        }

        private void ClearCopies(bool puff)
        {
            for (int i = 0; i < _copies.Count; i++)
            {
                NpcController c = _copies[i];
                if (c == null || c.IsDead) continue;
                if (puff && Game.Fx != null)
                {
                    if (c.CreatureMirage) Game.Fx.SandBurst(c.ChestPosition, 1.2f, 12);
                    else Game.Fx.GlassBurst(c.ChestPosition, Species.HitColor, 12);
                }
                if (c.CreatureKit != null) _guards.Remove(c.CreatureKit);
                Destroy(c.gameObject);
            }
            _copies.Clear();
        }

        // ================================================================== rings

        private Hazard AddRing(Vector3 center, float radius, float delay, float fill, float damage, DamageFlags flags, int fx)
        {
            float g;
            if (CreatureDirector.GroundAt(center, 4f, out g)) center.y = g;
            Hazard h = new Hazard();
            h.Center = center;
            h.Radius = radius;
            h.Delay = Mathf.Max(0f, delay);
            h.Fill = Mathf.Max(CreatureAttackDb.MinHeavyWindup, fill * CombatDirector.EnemyWindupScale);
            h.Damage = damage;
            h.Flags = flags;
            h.Fx = fx;
            _hazards.Add(h);
            return h;
        }

        private static Color RingColor(int fx)
        {
            switch (fx)
            {
                case FxIce: return new Color(0.45f, 0.85f, 1f, 1f);
                case FxSand: return new Color(1f, 0.6f, 0.2f, 1f);
                case FxGlass: return new Color(0.85f, 0.95f, 1f, 1f);
                default: return new Color(0.3f, 1f, 0.95f, 1f);
            }
        }

        private void UpdateHazards(float dt)
        {
            for (int i = _hazards.Count - 1; i >= 0; i--)
            {
                Hazard h = _hazards[i];
                h.T += dt;
                if (!h.Shown)
                {
                    if (h.T < h.Delay) continue;
                    h.Shown = true;
                    h.Ring = GroundRing.Show(h.Center, h.Radius, h.Fill, RingColor(h.Fx));
                }
                if (h.T < h.Delay + h.Fill) continue;
                if (h.Ring != null) h.Ring.Burst();
                BurstFx(h);
                BurstDamage(h);
                _hazards.RemoveAt(i);
                if (h.Arrival) ArrivalBurst04D(h.Center);
            }
        }

        private void BurstFx(Hazard h)
        {
            if (Game.Fx == null) return;
            Vector3 c = h.Center + Vector3.up * 0.2f;
            switch (h.Fx)
            {
                case FxIce:
                    Game.Fx.SnowBurst(c, h.Radius * 0.5f, 12);
                    Game.Fx.CrystalBurst(c + Vector3.up * 0.5f, RingColor(FxIce), 10, h.Radius * 0.4f);
                    break;
                case FxSand:
                    Game.Fx.SandBurst(c, h.Radius * 0.6f, 14);
                    break;
                case FxGlass:
                    Game.Fx.GlassBurst(c + Vector3.up * 0.4f, RingColor(FxGlass), 16);
                    break;
                default:
                    Game.Fx.CrystalBurst(c + Vector3.up * 0.4f, RingColor(FxCrystal), 14, h.Radius * 0.4f);
                    Game.Fx.RockBurst(c, Species.HitColor, 5, h.Radius * 0.3f);
                    break;
            }
            if (Game.Audio != null)
            {
                SfxId id = h.Fx == FxGlass ? SfxId.GlassBreak : SfxId.ExplosionSmall;
                Game.Audio.PlayVar(id, c, 0.45f, 0.2f, 45f);
            }
        }

        /// <summary>
        /// A burst hurts the player standing in its ring. Rings of a volley land a fraction of a second apart: after
        /// one hits, the rest pass harmlessly for RingRespite seconds, and a ring hit flinches lightly (never a heavy
        /// stagger), so a volley can never chain into a stagger lock that blocks the dodge.
        /// </summary>
        private void BurstDamage(Hazard h)
        {
            PlayerController p = Game.Player;
            if (p == null || p.IsDead) return;
            Vector3 pp = p.InVehicle ? p.CurrentVehicle.transform.position : p.transform.position;
            if (Mathf.Abs(pp.y - h.Center.y) > 3f || MathUtil.FlatDistance(pp, h.Center) > h.Radius + 0.3f) return;
            if (!CreatureFightRules.RingMayHit(Time.time, _ringHitAt)) return;
            float scale = Npc.CreatureOptions != null ? Npc.CreatureOptions.DamageScale : 1f;
            if (p.InVehicle)
            {
                _ringHitAt = Time.time;
                p.CurrentVehicle.ApplyDamage(Species.Damage * h.Damage * scale * 2f, Npc.gameObject);
                return;
            }
            float raw = CreatureBrainLogic.CapHit(Species.Damage * h.Damage * scale, p.Health.MaxHealth, (int)Settings.Difficulty, CombatDirector.PlayerDamageTaken);
            Vector3 dir = MathUtil.Flat(pp - h.Center);
            DamageInfo info = DamageInfo.Make(raw, p.ChestPosition, dir.sqrMagnitude > 0.01f ? dir : Vector3.up, Npc.gameObject, DamageType.Melee);
            if(Regional04OD && Species.Id==RegionalBossRules04OD.Sovereign)info.Element=Element.Volt;
            info.Force = 3f;
            info.Flags = h.Flags & ~DamageFlags.Heavy;
            p.Health.TakeDamage(info);
            if (!p.Health.LastBlocked) _ringHitAt = Time.time;
        }

        /// <summary>
        /// The boss is gone: rings that have not burst fade without hurting anyone. An upgraded fight then releases
        /// everything else it owns (lanes, beams, shards, the orb, focus points, decoys, supports, the illusion).
        /// </summary>
        private void Cleanup()
        {
            if (_cleaned) return;
            _cleaned = true;
            int rings = _hazards.Count;
            for (int i = 0; i < _hazards.Count; i++) if (_hazards[i].Ring != null) _hazards[i].Ring.Release();
            _hazards.Clear();
            if(Regional04OD)_regionalCycle04OD.Close();
            if (Upgraded04D) Cleanup04D(rings);
        }
    }
}
