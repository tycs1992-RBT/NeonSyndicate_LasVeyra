using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The upgraded 04D fight shared by the Foreman, the Djinn and the Queen. One beat at a time: a telegraph (the HUD
    /// names the move and fills a bar, the body glows, the ground or the air shows where it lands), a commit (the aim is
    /// fixed from here on), the active part (projectiles in flight, rings and lanes bursting), then a recovery window in
    /// which the boss stands open and takes more damage (BossProfile04D.CoreBonus). A stagger during a telegraph breaks
    /// the move. Phases open through BossPhaseLatch04D (once each, bursts skip explicitly), supports through
    /// BossAddBudget04D (two at once, one finite batch), and everything the fight owns is released on every way out.
    /// </summary>
    public sealed partial class CreatureBoss
    {
        private enum Beat04D { Idle, Telegraph, Active, Recovery }

        private const string OverlayKey04D = "boss04d";
        private static readonly Color TellColor04D = new Color(1f, 0.62f, 0.12f);
        private static readonly Color WindowColor04D = new Color(1f, 0.85f, 0.35f);

        private BossProfile04D _profile;
        private BossPhaseLatch04D _latch;
        private BossAddBudget04D _budget;
        private BossOwnedLedger04D _ledger;
        private Beat04D _beat;
        private BossMove04D _move;
        private float _beatStart;
        private float _beatUntil;
        private float _recStart;
        private float _recUntil;
        private float _next04D;
        private int _moveSerial;
        private int _sinceSignature = 1;
        private int _sinceRelocate;
        private bool _airborne;
        private float _airCheckAt;
        private float _ventFx;
        private bool _glow04D;
        private readonly List<NpcController> _supports = new List<NpcController>();

        /// <summary>This boss runs the upgraded 04D fight (the Foreman, the Djinn and the Queen; never the Titan).</summary>
        public bool Upgraded04D { get { return _profile != null; } }

        /// <summary>The 04D profile (null for the Titan).</summary>
        public BossProfile04D Profile04D { get { return _profile; } }

        /// <summary>True while the boss stands open after a committed move (its core, or the Djinn spent).</summary>
        public bool WindowOpen04D { get { return _profile != null && _beat == Beat04D.Recovery && Time.time < _recUntil; } }

        private static DifficultyLevel Diff04D { get { return Settings.Difficulty; } }

        // ================================================================== start

        private void InitFight04D()
        {
            _profile = BossProfiles04D.For(Kind);
            if (_profile == null) return;
            _latch = new BossPhaseLatch04D(_profile.PhaseThresholds);
            _budget = new BossAddBudget04D(_profile.MaxSupport, _profile.ReinforcementBatches);
            _ledger = new BossOwnedLedger04D();
            _beat = Beat04D.Idle;
            _next04D = Time.time + 3.5f;
            HudOverlays.Register(OverlayKey04D, DrawOverlayHook04D);
            if (Kind == HumanoidCreature.WraithQueen) RaiseFoci04D(false);
            Log.Info("Boss 04D: " + Species.Id + " fight owner ready (" + Settings.DifficultyName(Diff04D) + ", arena radius " + ArenaRadius.ToString("0") + " m)");
        }

        // ================================================================== frame

        private void Update04D(float dt)
        {
            float now = Time.time;
            if (Npc.IsStaggered) DropSlamRings();
            UpdateHazards(dt);
            UpdateLanes04D(dt);
            UpdateBeams04D(dt);
            UpdateShards04D(dt);
            UpdateOrb04D(now);
            UpdateDecoys04D(dt);
            UpdatePhase04D();
            UpdateSpecial04D(dt, now);
            UpdateDamageScale04D();
            CheckAir04D(now);
            if (_beat == Beat04D.Telegraph && Npc.IsStaggered) CancelMove04D("Interrupted");
            UpdateBeat04D(now);
        }

        private void UpdatePhase04D()
        {
            if (_latch == null) return;
            float f = Npc.Health.Health / Mathf.Max(1f, Npc.Health.MaxHealth);
            int skipped;
            int p = _latch.Evaluate(f, out skipped);
            if (p > 0) EnterPhase04D(p, skipped);
        }

        /// <summary>A phase opens (once): a roar and its one event. A burst that skipped a phase skips that phase's event.</summary>
        private void EnterPhase04D(int p, int skipped)
        {
            Phase = Mathf.Clamp(p, 0, PhaseCount - 1);
            if (_beat == Beat04D.Telegraph) CancelMove04D(null);
            _next04D = Time.time + 3f;
            Npc.CreatureCancelAttack();
            CreatureAudio.Play(Species, CreatureCry.Roar, Npc.HeadPosition, Npc.BodyHeight * 1.5f);
            if (Game.Cam != null) Game.Cam.Shake(0.45f);
            Log.Info("Boss 04D: " + Species.Id + " enters phase " + (Phase + 1) + (skipped > 0 ? " (a burst skipped " + skipped + " phase event)" : ""));
            switch (Kind)
            {
                case HumanoidCreature.GolemForeman: EnterPhaseForeman04D(Phase); break;
                case HumanoidCreature.MirageDjinn: EnterPhaseDjinn04D(Phase); break;
                case HumanoidCreature.WraithQueen: EnterPhaseQueen04D(Phase); break;
            }
        }

        private void UpdateSpecial04D(float dt, float now)
        {
            // The Foreman's plates still break the first time its poise does (the original rule).
            if (Kind == HumanoidCreature.GolemForeman && !_platesBroken)
            {
                bool st = Npc.IsStaggered;
                if (st && !_wasStaggered) BreakPlates();
                _wasStaggered = st;
            }
            if (Kind == HumanoidCreature.GolemForeman) UpdateForeman04D();
            if (_copies.Count > 0 && now > _copiesUntil) ClearCopies04D(true);
            PruneSupports04D();
            if (Kind == HumanoidCreature.MirageDjinn) UpdateIllusion04D(now);
            if (Kind == HumanoidCreature.WraithQueen) UpdateFoci04D(dt);
            // The body glows hard while it winds up and softly while it stands open.
            if (_beat == Beat04D.Telegraph || now < _glowUntil)
            {
                Npc.Anim.Telegraph = 0.75f + 0.25f * Mathf.Sin(now * 14f);
                _glow04D = true;
            }
            else if (_beat == Beat04D.Recovery)
            {
                Npc.Anim.Telegraph = 0.3f + 0.2f * Mathf.Sin(now * 5f);
                _glow04D = true;
                WindowFx04D(dt);
            }
            else if (_glow04D)
            {
                _glow04D = false;
                if (_kit == null || !_kit.Busy) Npc.Anim.Telegraph = 0f;
            }
        }

        private void UpdateDamageScale04D()
        {
            if (_kit == null) return;
            float baseScale = Kind == HumanoidCreature.GolemForeman ? BossProfiles04D.ForemanBaseScale(!_platesBroken) : 1f;
            _kit.DamageTakenScale = BossProfiles04D.DamageTakenScale(baseScale, WindowOpen04D, _profile.CoreBonus);
            // The Queen's own glass guard is down while her core is open.
            if (Kind == HumanoidCreature.WraithQueen && WindowOpen04D && _kit.GuardBrokenUntil < _recUntil) _kit.GuardBrokenUntil = _recUntil;
        }

        /// <summary>A few times a second: is the player above the reach of ground rings?</summary>
        private void CheckAir04D(float now)
        {
            if (now < _airCheckAt) return;
            _airCheckAt = now + 0.25f;
            PlayerController p = Game.Player;
            if (p == null || p.IsDead || p.InVehicle)
            {
                _airborne = false;
                return;
            }
            Vector3 pp = p.transform.position;
            float g;
            // No ground within reach below a player who is not standing: very high up, which is certainly airborne.
            _airborne = CreatureDirector.GroundAt(pp, 1f, out g) ? BossProfiles04D.Airborne(pp.y - g) : !p.IsGroundedPublic;
        }

        // ================================================================== beats

        private void UpdateBeat04D(float now)
        {
            switch (_beat)
            {
                case Beat04D.Telegraph:
                    TickTelegraph04D(now);
                    if (_beat == Beat04D.Telegraph && now >= _beatUntil) Commit04D(now);
                    return;
                case Beat04D.Active:
                    if (ActiveDone04D(now)) BeginRecovery04D(now, _move);
                    return;
                case Beat04D.Recovery:
                    if (now >= _recUntil) FinishMove04D(now);
                    return;
            }
            if (!Npc.CreatureEngaged || Npc.Target == null) return;
            if (now < _next04D || now < Npc.CreatureBusyUntil) return;
            if (Npc.IsStaggered || Npc.CreatureAttacking || Npc.IsRagdolled || (_kit != null && _kit.Busy)) return;
            StartMove04D(now, BossMove04D.None);
        }

        /// <summary>Chooses (or takes the forced) move and starts its telegraph.</summary>
        private void StartMove04D(float now, BossMove04D forced)
        {
            BossFightView04D v;
            v.Phase = Phase;
            v.Distance = Npc.Target != null ? MathUtil.FlatDistance(Npc.Target.transform.position, Npc.transform.position) : 99f;
            v.Airborne = _airborne;
            v.PlatesUp = PlatesUp;
            v.SinceSignature = _sinceSignature;
            v.SinceRelocate = _sinceRelocate;
            BossMove04D m = forced != BossMove04D.None ? forced : BossProfiles04D.Choose(Kind, v, Random.value);
            if (m == BossMove04D.None) return;
            _move = m;
            _moveSerial++;
            if (BossProfiles04D.IsSignature(m)) _sinceSignature = 0;
            else _sinceSignature++;
            if (m == BossMove04D.DjinnRelocate) _sinceRelocate = 0;
            else _sinceRelocate++;
            _beatStart = now;
            bool begun;
            switch (m)
            {
                case BossMove04D.ForemanOrb: begun = BeginOrb04D(now); break;
                case BossMove04D.DjinnChoir: begun = BeginChoir04D(now); break;
                case BossMove04D.DjinnRelocate: begun = BeginRelocate04D(now); break;
                case BossMove04D.QueenBeams: begun = BeginBeams04D(now); break;
                default:
                    PlayFiller04D(m);
                    begun = false;
                    FinishMove04D(now);
                    break;
            }
            if (!begun && _beat == Beat04D.Idle && _next04D <= now) _next04D = now + 1.2f;
        }

        /// <summary>The original ring moves, played as the fillers between signature lessons.</summary>
        private void PlayFiller04D(BossMove04D m)
        {
            CharacterBase t = Npc.Target;
            if (t == null) return;
            switch (m)
            {
                case BossMove04D.ForemanRain: CrystalRain(t, 6 + 2 * Phase); break;
                case BossMove04D.ForemanSlam: RingSlam(); break;
                case BossMove04D.ForemanThrow: if (_kit != null) _kit.StartThrow(); break;
                case BossMove04D.DjinnSandstorm: Sandstorm(t.transform.position, 5 + Phase); break;
                case BossMove04D.QueenShatter: GlassShatter(t, 6 + 2 * Phase); break;
            }
        }

        private void TickTelegraph04D(float now)
        {
            switch (_move)
            {
                case BossMove04D.ForemanOrb: TickOrbTelegraph04D(now); break;
                case BossMove04D.DjinnChoir: TickChoirTelegraph04D(now); break;
                case BossMove04D.QueenBeams: TickBeamsTelegraph04D(now); break;
            }
        }

        private void Commit04D(float now)
        {
            switch (_move)
            {
                case BossMove04D.ForemanOrb: CommitOrb04D(now); break;
                case BossMove04D.DjinnChoir: CommitChoir04D(now); break;
                case BossMove04D.DjinnRelocate: CommitRelocate04D(now); break;
                case BossMove04D.QueenBeams: CommitBeams04D(now); break;
                default: FinishMove04D(now); break;
            }
        }

        private bool ActiveDone04D(float now)
        {
            switch (_move)
            {
                case BossMove04D.ForemanOrb: return OrbDone04D(now);
                case BossMove04D.DjinnChoir: return ChoirLaunched04D(now);
                case BossMove04D.QueenBeams: return now >= _beatUntil;
                default: return true;
            }
        }

        /// <summary>The committed move is spent: the boss stands open for its recovery window.</summary>
        private void BeginRecovery04D(float now, BossMove04D move)
        {
            float seconds = BossTiming04D.Recovery(move != BossMove04D.None ? move : _profile.Signature, Diff04D);
            _recStart = now;
            _recUntil = now + seconds;
            _beat = Beat04D.Recovery;
            Npc.CreatureCancelAttack();
            Npc.CreatureBusyUntil = Mathf.Max(Npc.CreatureBusyUntil, _recUntil);
            _glowUntil = 0f;
            _ventFx = 0f;
            Callout(_profile.WindowLabel);
            switch (Kind)
            {
                case HumanoidCreature.GolemForeman:
                    if (Game.Fx != null) Game.Fx.RockBurst(Npc.ChestPosition, new Color(1f, 0.8f, 0.3f), 12, 1.2f);
                    if (Game.Audio != null) Game.Audio.PlayVar(SfxId.CrashHeavy, Npc.ChestPosition, 0.6f, 0.1f, 60f);
                    break;
                case HumanoidCreature.WraithQueen:
                    if (Game.Fx != null) Game.Fx.GlassBurst(Npc.ChestPosition, Species.HitColor, 26);
                    if (Game.Audio != null) Game.Audio.PlayVar(SfxId.GlassBreak, Npc.ChestPosition, 0.7f, 0.1f, 60f);
                    break;
                case HumanoidCreature.MirageDjinn:
                    if (Game.Fx != null) Game.Fx.SandBurst(Npc.ChestPosition, 1.6f, 16);
                    break;
            }
        }

        /// <summary>Back to plain fighting until the next move.</summary>
        private void FinishMove04D(float now)
        {
            if (_beat == Beat04D.Recovery) Npc.CreatureBusyUntil = Mathf.Min(Npc.CreatureBusyUntil, now);
            _beat = Beat04D.Idle;
            _next04D = now + BossTiming04D.MoveGap(Phase, Diff04D) * Mathf.Lerp(0.9f, 1.1f, Random.value);
        }

        /// <summary>
        /// Breaks the move still winding up (a stagger, the crown stun, a phase change): its telegraph visuals vanish
        /// without hurting anyone and nothing commits. Moves already committed play out.
        /// </summary>
        private void CancelMove04D(string callout)
        {
            if (_beat != Beat04D.Telegraph) return;
            ReleaseMoveLanes04D(_moveSerial);
            ReleaseMoveBeams04D(_moveSerial);
            DropHeldShards04D();
            DropDecoys04D(false);
            CancelOrbTelegraph04D();
            _beat = Beat04D.Idle;
            _glowUntil = 0f;
            Npc.CreatureBusyUntil = Mathf.Min(Npc.CreatureBusyUntil, Time.time);
            _next04D = Time.time + 1.5f;
            if(KingdomCombatRules04N.ExposeInterruptedBoss(Kind,true,!string.IsNullOrEmpty(callout)))BeginRecovery04D(Time.time,_move);
            if (!string.IsNullOrEmpty(callout)) Callout(callout + ": " + BossProfiles04D.MoveShortName(_move) + " broken");
        }

        private void WindowFx04D(float dt)
        {
            _ventFx -= dt;
            if (_ventFx > 0f || Game.Fx == null) return;
            _ventFx = 0.12f;
            Vector3 c = Npc.ChestPosition + Npc.transform.forward * 0.25f * Mathf.Max(1f, Npc.Rig.Scale);
            switch (Kind)
            {
                case HumanoidCreature.GolemForeman:
                    Game.Fx.CrystalBurst(c, new Color(1f, 0.85f, 0.35f), 4, 0.35f);
                    Game.Fx.Smoke(Npc.HeadPosition + Random.insideUnitSphere * 0.4f, Vector3.up * 1.5f, 0.9f, 0.3f);
                    break;
                case HumanoidCreature.WraithQueen:
                    Game.Fx.CrystalBurst(c, Species.HitColor, 4, 0.3f);
                    break;
                default:
                    Game.Fx.SandBurst(c, 0.5f, 3);
                    break;
            }
        }

        // ================================================================== player hits

        /// <summary>Where the player is aimed at: the chest on foot or in the air, the vehicle when driving.</summary>
        private static Vector3 PlayerAimPoint04D(PlayerController p)
        {
            if (p.InVehicle && p.CurrentVehicle != null) return p.CurrentVehicle.transform.position + Vector3.up;
            return p.ChestPosition;
        }

        /// <summary>
        /// One boss hit on the player (capped like every creature hit; a vehicle takes it on its body). Hits from one
        /// volley respect the ring respite, so nothing chains into a stagger lock. True when it landed.
        /// </summary>
        private bool HitPlayer04D(float damageMul, Vector3 from, DamageType type, DamageFlags flags, float force)
        {
            PlayerController p = Game.Player;
            if (p == null || p.IsDead) return false;
            if (!CreatureFightRules.RingMayHit(Time.time, _ringHitAt)) return false;
            float scale = Npc.CreatureOptions != null ? Npc.CreatureOptions.DamageScale : 1f;
            if (p.InVehicle && p.CurrentVehicle != null)
            {
                _ringHitAt = Time.time;
                p.CurrentVehicle.ApplyDamage(Species.Damage * damageMul * scale * 2f, Npc.gameObject);
                return true;
            }
            float raw = CreatureBrainLogic.CapHit(Species.Damage * damageMul * scale, p.Health.MaxHealth, (int)Settings.Difficulty, CombatDirector.PlayerDamageTaken);
            Vector3 dir = p.ChestPosition - from;
            DamageInfo info = DamageInfo.Make(raw, p.ChestPosition, dir.sqrMagnitude > 0.01f ? dir : Vector3.up, Npc.gameObject, type);
            info.Force = force;
            info.Flags = flags & ~DamageFlags.Heavy;
            p.Health.TakeDamage(info);
            if (p.Health.LastBlocked) return false;
            _ringHitAt = Time.time;
            return true;
        }

        /// <summary>A ground ring whose fill time is already the difficulty's (no second wind-up scaling).</summary>
        private Hazard AddRingExact04D(Vector3 center, float radius, float delay, float fill, float damage, DamageFlags flags, int fx)
        {
            float g;
            if (CreatureDirector.GroundAt(center, 4f, out g)) center.y = g;
            Hazard h = new Hazard();
            h.Center = center;
            h.Radius = radius;
            h.Delay = Mathf.Max(0f, delay);
            h.Fill = Mathf.Max(BossTiming04D.TelegraphMin, fill);
            h.Damage = damage;
            h.Flags = flags;
            h.Fx = fx;
            _hazards.Add(h);
            return h;
        }

        // ================================================================== supports

        /// <summary>Living support actors (the Foreman's golems and the mirages or mirror copies).</summary>
        private int AliveSupports04D
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _supports.Count; i++) if (_supports[i] != null && !_supports[i].IsDead) n++;
                for (int i = 0; i < _copies.Count; i++) if (_copies[i] != null && !_copies[i].IsDead) n++;
                return n;
            }
        }

        /// <summary>A support that died (or was removed) leaves the list; its death never opens a new batch.</summary>
        private void PruneSupports04D()
        {
            for (int i = _supports.Count - 1; i >= 0; i--)
            {
                NpcController s = _supports[i];
                if (s != null && !s.IsDead) continue;
                _supports.RemoveAt(i);
                if (_ledger != null) _ledger.Release(BossOwned04D.Support);
                Log.Info("Boss 04D: " + Species.Id + " support down (" + AliveSupports04D + " alive, " + (_budget != null ? _budget.BatchesLeft : 0) + " batches left)");
            }
            for (int i = _copies.Count - 1; i >= 0; i--)
            {
                NpcController c = _copies[i];
                if (c != null && !c.IsDead) continue;
                _copies.RemoveAt(i);
                if (_ledger != null) _ledger.Release(BossOwned04D.Illusion);
            }
        }

        // ================================================================== boss bar

        private string PhaseLabel04D
        {
            get
            {
                if (WindowOpen04D) return _profile.WindowLabel;
                if (IllusionActive04D) return "ILLUSION: THE REAL DJINN CASTS A SHADOW";
                if (PlatesUp) return "HAZARD PLATES UP";
                return PhaseNames[Mathf.Clamp(Phase, 0, PhaseNames.Length - 1)];
            }
        }

        // ================================================================== cleanup

        /// <summary>
        /// Every way out of the fight lands here once (boss death, leaving, the Veil closing, the player's death, the
        /// title, a load, any other removal): the budget and the ledger close (so nothing scheduled can still appear),
        /// and lanes, beams, shards, the orb, decoys, focus points, the illusion and the supports this fight raised go.
        /// The defeat record and the reward are the director's, and only a real player kill reaches them.
        /// </summary>
        private void Cleanup04D(int rings)
        {
            BossCleanup04D reason = ClassifyCleanup04D();
            int lanes = ReleaseAllLanes04D();
            int beams = ReleaseAllBeams04D();
            int shards = ReleaseAllShards04D();
            int decoys = DropDecoys04D(true);
            int orbs = ReleaseOrb04D();
            int foci = ClearFoci04D(false);
            int supports = DismissSupports04D(reason);
            if (_budget != null) _budget.Close();
            if (_ledger != null) _ledger.Close(reason);
            _beat = Beat04D.Idle;
            if (Npc != null && Npc.Anim != null && !Npc.IsDead) Npc.Anim.Telegraph = 0f;
            Log.Info("Boss 04D: " + (Species != null ? Species.Id : "boss") + " cleanup (" + reason + "): rings " + rings + ", lanes " + lanes + ", beams " + beams
                + ", shards " + shards + ", decoys " + decoys + ", orb " + orbs + ", foci " + foci + ", supports " + supports
                + (_latch != null ? ", phases entered " + _latch.Entered + " skipped " + _latch.Skipped : "")
                + (_budget != null ? ", supports admitted " + _budget.Admitted : ""));
        }

        private BossCleanup04D ClassifyCleanup04D()
        {
            bool bossDead = Npc != null && Npc.IsDead;
            bool loading = Game.State == GameState.Loading;
            PlayerController p = Game.Player;
            bool playerDead = p == null || p.IsDead || Game.State == GameState.Wasted || Game.State == GameState.Busted;
            bool veilLost = Kind == HumanoidCreature.WraithQueen && (Game.Inner == null || !Game.Inner.IsInner);
            bool outside = p != null && MathUtil.FlatDistance(Game.PlayerPosition, ArenaCenter) > ArenaRadius + 60f;
            return BossProfiles04D.ClassifyCleanup(bossDead, loading, p == null, playerDead, veilLost, outside);
        }

        /// <summary>
        /// The supports and illusions this fight raised leave with it. On a boss death the director's victory sweep also
        /// scatters them; on every other path this makes sure none outlive their owner.
        /// </summary>
        private int DismissSupports04D(BossCleanup04D reason)
        {
            int n = 0;
            for (int i = 0; i < _supports.Count; i++)
            {
                NpcController s = _supports[i];
                if (s == null || s.IsDead) continue;
                n++;
                if (Game.Fx != null && reason != BossCleanup04D.Title && reason != BossCleanup04D.Load) Game.Fx.RockBurst(s.ChestPosition, s.Health.SurfaceTint, 8, 0.8f);
                Destroy(s.gameObject);
            }
            _supports.Clear();
            for (int i = 0; i < _copies.Count; i++) if (_copies[i] != null && !_copies[i].IsDead) n++;
            ClearCopies04D(reason != BossCleanup04D.Title && reason != BossCleanup04D.Load);
            return n;
        }

        /// <summary>The mirages or mirror copies go (their ledger entries with them).</summary>
        private void ClearCopies04D(bool puff)
        {
            if (_ledger != null) for (int i = 0; i < _copies.Count; i++) if (_copies[i] != null && !_copies[i].IsDead) _ledger.Release(BossOwned04D.Illusion);
            ClearCopies(puff);
        }

        // ================================================================== debug (F2 > Creatures)

        /// <summary>
        /// F2: sets the boss's health just past the next two thresholds in one step (never lethal, never the player's
        /// hit), to show the phase latch enter one phase and skip the other.
        /// </summary>
        public void DebugBurst04D()
        {
            if (!Upgraded04D || Npc == null || Npc.IsDead) return;
            float f = Npc.Health.Health / Mathf.Max(1f, Npc.Health.MaxHealth);
            float target = f >= BossProfiles04D.PhaseTwoAt ? BossProfiles04D.PhaseThreeAt - 0.02f : Mathf.Max(0.05f, Mathf.Min(f, BossProfiles04D.PhaseThreeAt) - 0.04f);
            Npc.Health.Health = Mathf.Max(1f, target * Npc.Health.MaxHealth);
            if (Game.Hud != null) Game.Hud.Notify("Debug burst: health set to " + Mathf.RoundToInt(target * 100f) + " percent in one step", 2f);
        }

        /// <summary>F2: opens the recovery window now.</summary>
        public void DebugOpenWindow04D()
        {
            if (!Upgraded04D || Npc == null || Npc.IsDead) return;
            if (_beat == Beat04D.Telegraph) CancelMove04D(null);
            BeginRecovery04D(Time.time, _profile.Signature);
        }

        /// <summary>F2: the signature move starts now.</summary>
        public void DebugSignature04D()
        {
            if (!Upgraded04D || Npc == null || Npc.IsDead || Npc.Target == null) return;
            if (_beat == Beat04D.Telegraph) CancelMove04D(null);
            if (_beat == Beat04D.Recovery) FinishMove04D(Time.time);
            if (_beat != Beat04D.Idle) return;
            StartMove04D(Time.time, _profile.Signature);
        }

        /// <summary>F2: one line of the fight's state (built on demand for the debug page only).</summary>
        public string DebugStatus04D
        {
            get
            {
                if (!Upgraded04D) return "Original script (no 04D profile)";
                return "04D " + _profile.BossId + ": phase " + (Phase + 1) + " (entered " + _latch.Entered + ", skipped " + _latch.Skipped + ")   beat " + _beat + " " + _move
                    + "   supports " + AliveSupports04D + "/" + _budget.MaxAlive + " batches left " + _budget.BatchesLeft + " admitted " + _budget.Admitted
                    + "   lanes " + LiveLanes04D + " shards " + LiveShards04D + " foci " + LiveFoci04D + (IllusionActive04D ? "   ILLUSION" : "") + (_airborne ? "   player airborne" : "");
            }
        }

        // ================================================================== HUD

        private static void DrawOverlayHook04D(float w, float h)
        {
            CreatureDirector cd = Game.Creatures;
            if (cd == null) return;
            CreatureBoss b = cd.ActiveBossBrain;
            if (b == null || !b.Upgraded04D || b._cleaned) return;
            b.DrawOverlay04D(w, h);
        }

        private static bool ToGui04D(Camera cam, Vector3 world, float w, float h, out Vector2 gui)
        {
            gui = Vector2.zero;
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0.1f) return false;
            float s = UIKit.Scale;
            gui = new Vector2(sp.x / s, (Screen.height - sp.y) / s);
            return gui.x > -40f && gui.x < w + 40f && gui.y > -40f && gui.y < h + 40f;
        }

        /// <summary>
        /// The fight's HUD: the move winding up (name, counter and a filling bar above the boss bar, and a "!!" over the
        /// boss), the open window (a shrinking bar and a ring on the core), and the Queen's focus points.
        /// Text and shapes carry the information, never colour alone.
        /// </summary>
        private void DrawOverlay04D(float w, float h)
        {
            PlayerController p = Game.Player;
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            if (p == null || p.IsDead || cam == null || Npc == null || Npc.IsDead) return;
            if (MathUtil.FlatDistance(p.transform.position, Npc.transform.position) > 140f) return;
            float now = Time.time;
            float cx = w * 0.5f, by = h - 214f;
            bool telling = _beat == Beat04D.Telegraph || (_beat == Beat04D.Active && (_move == BossMove04D.ForemanOrb || _move == BossMove04D.QueenBeams));
            if (telling)
            {
                float end = _move == BossMove04D.ForemanOrb && _beat == Beat04D.Active ? OrbRuptureAt04D : _beatUntil;
                float k = Mathf.Clamp01((now - _beatStart) / Mathf.Max(0.05f, end - _beatStart));
                UIKit.Text(new Rect(cx - 520f, by - 30f, 1040f, 28f), BossProfiles04D.MoveLabel(_move), 20, TellColor04D, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Rect(new Rect(cx - 200f, by, 400f, 6f), new Color(0f, 0f, 0f, 0.6f));
                UIKit.Rect(new Rect(cx - 200f, by, 400f * k, 6f), TellColor04D);
                Vector2 g;
                // The "!!" is for danger only (a lane shift is not an attack).
                if (_move != BossMove04D.DjinnRelocate && ToGui04D(cam, Npc.HeadPosition + Vector3.up * 0.8f, w, h, out g))
                {
                    float a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 12f));
                    UIKit.Text(new Rect(g.x - 60f, g.y - 30f, 120f, 40f), "!!", Mathf.RoundToInt(Mathf.Lerp(24f, 38f, k)), new Color(TellColor04D.r, TellColor04D.g, TellColor04D.b, a), TextAnchor.MiddleCenter, FontStyle.Bold);
                }
            }
            else if (WindowOpen04D)
            {
                float k = Mathf.Clamp01((_recUntil - now) / Mathf.Max(0.05f, _recUntil - _recStart));
                UIKit.Text(new Rect(cx - 520f, by - 30f, 1040f, 28f), _profile.WindowLabel, 20, WindowColor04D, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Rect(new Rect(cx - 200f, by, 400f, 6f), new Color(0f, 0f, 0f, 0.6f));
                UIKit.Rect(new Rect(cx - 200f * k, by, 400f * k, 6f), WindowColor04D);
                Vector2 g;
                if (ToGui04D(cam, Npc.ChestPosition, w, h, out g))
                {
                    float s = 34f + 8f * Mathf.Sin(Time.unscaledTime * 8f);
                    UIKit.Tex(new Rect(g.x - s * 0.5f, g.y - s * 0.5f, s, s), UIKit.Ring, new Color(WindowColor04D.r, WindowColor04D.g, WindowColor04D.b, 0.95f));
                    UIKit.Text(new Rect(g.x - 80f, g.y + s * 0.5f, 160f, 20f), Kind == HumanoidCreature.MirageDjinn ? "SPENT" : "CORE", 15, WindowColor04D, TextAnchor.MiddleCenter, FontStyle.Bold);
                }
            }
            if (Kind == HumanoidCreature.WraithQueen) DrawFoci04D(cam, w, h);
        }
    }
}
