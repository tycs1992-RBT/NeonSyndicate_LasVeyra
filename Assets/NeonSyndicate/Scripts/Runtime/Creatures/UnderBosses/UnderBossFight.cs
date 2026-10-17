using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// One Underheart boss fight (shared base of the Valve Guardian, the Cinder Matron, the Vaultbreaker and the Kiln
    /// Regent). Its own controller on a procedural body, never a Creature: it waits dormant behind a readable boundary
    /// until the player walks in, then runs one beat at a time (a telegraph the HUD names and the floor or the air shows,
    /// a commit where the aim locks, the active part, then a punish window), opens each phase once through a latch,
    /// brings only its declared reinforcements, and on a confirmed death cancels every hazard it owns, stops its loops,
    /// opens the exit, takes a quiet aftermath lease, marks the room cleared and grants the first-win entitlements once.
    /// Every other ending (death, leaving, withdrawing, abort, load, title) cleans up without a reward.
    /// Split: .Hazards (rings, lanes, burning ground, beams and player hits), .Arena (boundary, seal, exit),
    /// .Machine (procedural machine bodies with hit volumes), .Lock (the lock-on stand-in and damage numbers),
    /// .Lights (the one budgeted key light per fight).
    /// </summary>
    public abstract partial class UnderBossFight : MonoBehaviour, IDamageFilter
    {
        public enum FightState
        {
            Dormant = 0,
            Intro = 1,
            Fighting = 2,
            Victory = 3,
            Ended = 4
        }

        protected enum Beat
        {
            Idle,
            Telegraph,
            Active,
            Recovery
        }

        public const float IntroSeconds = 2.6f;
        public const float AftermathSeconds = 240f;

        public UnderBossProfile Profile { get; private set; }
        public BossFightHandle Handle { get; private set; }
        public FightState State { get; private set; }
        public UBossEnd EndReason { get; private set; }
        public Vector3 ArenaCenter { get; private set; }
        public float ArenaRadius { get; private set; }
        public float ArenaYaw { get; private set; }
        public int Phase { get; private set; }

        protected UnderheartBosses Owner;
        /// <summary>The moving body (its CharacterHealth, kinematic Rigidbody and hit colliders live here).</summary>
        protected Transform Body;
        protected CharacterHealth Health;
        protected BossPhaseLatch04D Latch;
        protected UBossAddSquad Squad;
        protected readonly PunishWindow04E Window = new PunishWindow04E();
        protected readonly ArenaWatch04E Watch = new ArenaWatch04E();
        private readonly VictoryLatch04E _victory = new VictoryLatch04E();

        protected Beat CurrentBeat;
        protected UBossMove Move;
        protected float BeatStart;
        protected float BeatUntil;
        protected float NextMoveAt;
        protected int MoveSerial;
        protected int SinceSignature = 1;
        protected bool PlayerAirborne;
        protected float LastHitAt = -99f;
        protected float BodyYaw;

        private float _airCheckAt;
        private float _phasePauseUntil;
        private float _introUntil;
        private float _victoryAt;
        private bool _cleaned;
        private bool _aftermathTidied;
        private string _rewardLine = "";
        private readonly List<UBossPartTag> _parts = new List<UBossPartTag>();
        private readonly List<int> _loops = new List<int>();

        protected static DifficultyLevel Diff { get { return Settings.Difficulty; } }

        public float Health01 { get { return Health != null ? Mathf.Clamp01(Health.Health / Mathf.Max(1f, Health.MaxHealth)) : 0f; } }
        public bool Finished { get { return State == FightState.Victory || State == FightState.Ended; } }
        public bool Running { get { return State == FightState.Dormant || State == FightState.Intro || State == FightState.Fighting; } }
        public string LeaseOwner { get { return "uboss04e:" + (Profile != null ? Profile.Id : "boss"); } }
        public string AddsTag { get { return "uboss04e_adds_" + (Profile != null ? Profile.Id : "boss"); } }
        public Transform BodyTransform { get { return Body; } }
        public string RewardLine { get { return _rewardLine; } }

        // ================================================================== setup

        /// <summary>
        /// Builds the arena and the body and arms the fight (dormant until the player crosses the boundary). False with a
        /// reason when anything could not be built; the caller then destroys this object (nothing is left behind).
        /// </summary>
        public bool Setup(UnderheartBosses owner, UnderBossProfile profile, BossFightHandle handle, Vector3 arena, float yaw, float radius, out string reason)
        {
            reason = "";
            Owner = owner;
            Profile = profile;
            Handle = handle;
            ArenaCenter = arena;
            ArenaYaw = yaw;
            ArenaRadius = radius;
            Latch = new BossPhaseLatch04D(profile.PhaseThresholds);
            Squad = new UBossAddSquad(AddsTag, new AddLedger04E(profile.DeclaredAdds, profile.MaxAddsAlive, profile.Batches != null ? profile.Batches.Length : 0));
            Squad.UseArena(arena, radius);
            transform.SetPositionAndRotation(arena, Quaternion.Euler(0f, yaw, 0f));
            try
            {
                BuildArena();
                Vector3 start;
                float faceYaw;
                StartPose(out start, out faceYaw);
                GameObject bodyGo = new GameObject(profile.Title + " body");
                bodyGo.layer = Layers.Debris;
                bodyGo.transform.SetParent(transform, false);
                bodyGo.transform.SetPositionAndRotation(start, Quaternion.Euler(0f, faceYaw, 0f));
                Body = bodyGo.transform;
                BodyYaw = faceYaw;
                if (!BuildBody(out reason)) return false;
                Health = bodyGo.GetComponent<CharacterHealth>();
                if (Health == null) Health = bodyGo.AddComponent<CharacterHealth>();
                float hp = UnderBossProfiles.HealthFor(profile, Diff);
                Health.MaxHealth = hp;
                Health.Health = hp;
                Health.MaxArmor = 0f;
                Health.Armor = 0f;
                Health.HeadshotMultiplier = 1f;
                Health.HeadHeight = 999f;
                Health.Surface = HitSurface;
                Health.SurfaceTint = profile.Theme;
                Health.Filter = this;
                Health.Died += OnBodyDied;
                Rigidbody rb = bodyGo.GetComponent<Rigidbody>();
                if (rb == null) rb = bodyGo.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.interpolation = RigidbodyInterpolation.None;
                CollectParts();
                if (_parts.Count == 0)
                {
                    reason = "the body has no hit volumes";
                    return false;
                }
                OnSetup();
                CollectShootables();
                // 04E: the lock-on stand-in and the floating damage numbers (UnderBossFight.Lock).
                SetupCombatFeedback();
            }
            catch (Exception e)
            {
                Log.Exception("Underheart boss setup " + profile.Id, e);
                reason = e.Message;
                return false;
            }
            State = FightState.Dormant;
            LocalQuiet.AcquireAdmission(LeaseOwner, ArenaCenter, ArenaRadius + 20f);
            Handle.Status = "Enter the arena to face " + profile.Title + ".";
            Handle.Health01 = 1f;
            Handle.Phase = 0;
            Log.Info("UBOSS04E " + profile.Id + " armed at " + ArenaCenter.ToString("F1") + " radius " + ArenaRadius.ToString("0") + " (" + Settings.DifficultyName(Diff) + ", health "
                + Mathf.RoundToInt(Health.MaxHealth) + ", declared adds " + profile.DeclaredAdds + ", parts " + _parts.Count + ", realtime lights " + _lightsCreated + "/" + BossLightBudget04E.MaxFightLights + " shadowless)");
            return true;
        }

        /// <summary>Where the body starts and which way it faces (default: the far side of the arena, facing the gate or centre).</summary>
        protected virtual void StartPose(out Vector3 pos, out float yaw)
        {
            Vector3 toward = GatePoint != Vector3.zero ? MathUtil.Flat(GatePoint - ArenaCenter) : MathUtil.Flat(Game.PlayerPosition - ArenaCenter);
            if (toward.sqrMagnitude < 0.5f) toward = MathUtil.DirFromYaw(ArenaYaw);
            toward.Normalize();
            Vector3 want = ArenaCenter - toward * (ArenaRadius * 0.35f);
            Vector3 s;
            pos = UnderBossFx.StandSpot(want, ArenaCenter.y, 3f, out s) ? s : ArenaCenter;
            yaw = MathUtil.YawOf(toward);
        }

        /// <summary>Builds the body under Body (hit colliders tagged with UBossPartTag on layer Debris). False with a reason on failure.</summary>
        protected abstract bool BuildBody(out string reason);

        /// <summary>Arena props owned by this fight (lures, plates, furnaces). Called after the body exists.</summary>
        protected virtual void OnSetup() { }

        protected virtual ImpactSurface HitSurface { get { return ImpactSurface.Metal; } }

        private void CollectParts()
        {
            _parts.Clear();
            UBossPartTag[] tags = Body.GetComponentsInChildren<UBossPartTag>(true);
            for (int i = 0; i < tags.Length; i++)
            {
                UBossPartTag t = tags[i];
                if (t.Collider == null) t.Collider = t.GetComponent<Collider>();
                if (t.Collider != null) _parts.Add(t);
            }
        }

        /// <summary>Adds a hit collider to a transform under the body and tags it.</summary>
        protected UBossPartTag Tag(Transform at, Collider c, UBossPart part, int index)
        {
            if (at == null || c == null) return null;
            at.gameObject.layer = Layers.Debris;
            UBossPartTag t = at.gameObject.GetComponent<UBossPartTag>();
            if (t == null) t = at.gameObject.AddComponent<UBossPartTag>();
            t.Part = part;
            t.Index = index;
            t.Collider = c;
            return t;
        }

        // ================================================================== frame

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || Profile == null) return;
            if (State == FightState.Ended) return;
            float now = Time.time;
            if (State == FightState.Victory)
            {
                UpdateAftermath(dt, now);
                return;
            }
            if (SessionEnded()) return;
            if (Body == null || Health == null)
            {
                End(UBossEnd.Aborted, "The boss body was removed; the fight ends without a winner.");
                return;
            }
            Health.Burning = 0f;
            Squad.Tick();
            UpdateAir(now);
            switch (State)
            {
                case FightState.Dormant: UpdateDormant(now); break;
                case FightState.Intro:
                    if (now >= _introUntil) StartFighting(now);
                    break;
                case FightState.Fighting: UpdateFight(dt, now); break;
            }
            if (State == FightState.Ended) return;
            UpdateHazards(dt, now);
            UpdateArena(dt, now);
            try { AnimateBody(dt, now); }
            catch (Exception e) { Log.Exception("Underheart boss animation " + Profile.Id, e); }
            UpdateLockPoint(now, false);
            if (Handle != null)
            {
                Handle.Health01 = Health01;
                Handle.Phase = Phase;
            }
        }

        /// <summary>The player died, the game went to the title or a save is loading: the fight ends without a winner.</summary>
        private bool SessionEnded()
        {
            if (Game.State == GameState.Title)
            {
                End(UBossEnd.Title, "The fight ended when the game returned to the title.");
                return true;
            }
            if (Game.State == GameState.Loading)
            {
                End(UBossEnd.SessionReset, "The fight ended while a save loaded.");
                return true;
            }
            PlayerController p = Game.Player;
            if (p == null || p.IsDead || Game.State == GameState.Wasted || Game.State == GameState.Busted)
            {
                End(UBossEnd.PlayerDied, "You fell. " + Profile.Title + " withdraws: rest, then return to the arena to try again.");
                return true;
            }
            return false;
        }

        private void UpdateDormant(float now)
        {
            Vector3 pp = PlayerFeet;
            float d = MathUtil.FlatDistance(pp, ArenaCenter);
            if (ArenaWatch04E.InsideToStart(d, pp.y - ArenaCenter.y, ArenaRadius)) Wake();
        }

        /// <summary>The player crossed the boundary (or struck the dormant body from close by): the intro starts.</summary>
        protected void Wake()
        {
            if (State != FightState.Dormant) return;
            State = FightState.Intro;
            _introUntil = Time.time + IntroSeconds;
            NoteEntry(PlayerFeet);
            Underheart04ESounds.Play(UhSfx.BossPhase, BodyCenter, 1f, 0.85f);
            if (Game.Hud != null) Game.Hud.Banner(Profile.Title.ToUpper(), Profile.Lesson, Profile.Theme, 4.5f);
            if (Game.Cam != null) Game.Cam.Shake(0.5f);
            Handle.Status = "Defeat " + Profile.Title + ".";
            OnWake();
            Log.Info("UBOSS04E " + Profile.Id + " intro");
        }

        protected virtual void OnWake() { }

        private void StartFighting(float now)
        {
            State = FightState.Fighting;
            NextMoveAt = now + 1.2f;
            TryCloseSeal();
            OnFightStart(now);
            Log.Info("UBOSS04E " + Profile.Id + " fight starts");
        }

        protected virtual void OnFightStart(float now) { }

        private void UpdateFight(float dt, float now)
        {
            Vector3 pp = PlayerFeet;
            float d = MathUtil.FlatDistance(pp, ArenaCenter);
            ArenaVerdict verdict = Watch.Tick(dt, d, pp.y - ArenaCenter.y, ArenaRadius, PressingSeal(pp));
            if (verdict == ArenaVerdict.Withdraw)
            {
                End(UBossEnd.Withdrew, "You withdrew through the seal. " + Profile.Title + "'s arena resets for your return.");
                return;
            }
            if (verdict == ArenaVerdict.Left)
            {
                End(UBossEnd.PlayerLeft, "You left the arena. " + Profile.Title + " withdraws: return to try again.");
                return;
            }
            EvaluatePhase();
            try { OnFightTick(dt, now); }
            catch (Exception e) { Log.Exception("Underheart boss tick " + Profile.Id, e); }
            if (State != FightState.Fighting) return;
            UpdateBeats(now);
        }

        /// <summary>Per-frame boss behaviour while fighting (movement, special mechanics). Beats run after it.</summary>
        protected virtual void OnFightTick(float dt, float now) { }

        /// <summary>Moves and poses the body every frame (also while dormant and in the intro).</summary>
        protected abstract void AnimateBody(float dt, float now);

        private void UpdateAir(float now)
        {
            if (now < _airCheckAt) return;
            _airCheckAt = now + 0.25f;
            PlayerController p = Game.Player;
            if (p == null || p.IsDead || p.InVehicle)
            {
                PlayerAirborne = false;
                return;
            }
            Vector3 pp = p.transform.position;
            float g;
            PlayerAirborne = UnderBossFx.Floor(pp, 0.5f, 6f, out g) ? pp.y - g > UnderBossProfiles.AirHeight : !p.IsGroundedPublic;
        }

        // ================================================================== phases

        private void EvaluatePhase()
        {
            int skipped;
            int p = Latch.Evaluate(Health01, out skipped);
            if (p > 0) EnterPhase(p, skipped);
        }

        private void EnterPhase(int p, int skipped)
        {
            Phase = Mathf.Clamp(p, 0, Profile.PhaseCount - 1);
            CancelTelegraph(null);
            // An open punish window stays open (the player earned it); the next move waits for the phase pause.
            _phasePauseUntil = Time.time + 2.6f;
            Underheart04ESounds.Play(UhSfx.BossPhase, BodyCenter, 1f, 1f);
            if (Game.Cam != null) Game.Cam.Shake(0.45f);
            string name = Profile.PhaseNames != null && Phase < Profile.PhaseNames.Length ? Profile.PhaseNames[Phase] : "PHASE " + (Phase + 1);
            Callout(name);
            Log.Info("UBOSS04E " + Profile.Id + " enters phase " + (Phase + 1) + (skipped > 0 ? " (a burst skipped " + skipped + " phase event)" : ""));
            FireBatches(Phase);
            try { OnPhase(Phase); }
            catch (Exception e) { Log.Exception("Underheart boss phase " + Profile.Id, e); }
        }

        protected virtual void OnPhase(int phase) { }

        /// <summary>The phase's declared reinforcement batches (each fires once; the ledger caps the bodies).</summary>
        private void FireBatches(int phase)
        {
            if (Profile.Batches == null) return;
            for (int i = 0; i < Profile.Batches.Length; i++)
            {
                UBossAddBatch b = Profile.Batches[i];
                if (b.Phase != phase || !Squad.Ledger.TryFireBatch(i)) continue;
                int n = SpawnBatch(b.Species, b.Count);
                if (n <= 0) continue;
                if (Game.Hud != null) Game.Hud.Banner("REINFORCEMENTS", b.Announce, new Color(1f, 0.6f, 0.3f), 3.2f);
                Underheart04ESounds.Play(UhSfx.BossTelegraph, ArenaCenter, 0.8f, 0.8f);
                Log.Info("UBOSS04E " + Profile.Id + " reinforcements: " + n + " " + b.Species + " (" + Squad.Ledger.Spawned + "/" + Squad.Ledger.Declared + " of the declared budget)");
            }
        }

        /// <summary>Spawns up to count bodies around the arena (away from the player), within the ledger. Returns how many came.</summary>
        protected int SpawnBatch(string species, int count)
        {
            int n = 0;
            Vector3 away = MathUtil.Flat(ArenaCenter - PlayerFeet);
            if (away.sqrMagnitude < 0.5f) away = MathUtil.DirFromYaw(ArenaYaw);
            away.Normalize();
            float baseYaw = MathUtil.YawOf(away);
            for (int i = 0; i < count; i++)
            {
                float a = baseYaw + (i - (count - 1) * 0.5f) * 35f;
                Vector3 want = ArenaCenter + MathUtil.DirFromYaw(a) * (ArenaRadius * 0.6f);
                want.y = ArenaCenter.y;
                Vector3 spot;
                if (!UBossAddSquad.Spot(want, ArenaCenter, ArenaCenter.y, out spot)) continue;
                float yaw = MathUtil.YawOf(PlayerFeet - spot);
                if (Squad.Spawn(species, spot, yaw, 1f, 1f) == null) continue;
                if (Game.Fx != null) Game.Fx.RockBurst(spot + Vector3.up, Profile.Theme, 14, 1.4f);
                n++;
            }
            return n;
        }

        // ================================================================== beats

        private void UpdateBeats(float now)
        {
            switch (CurrentBeat)
            {
                case Beat.Telegraph:
                    TickTelegraph(Move, now);
                    if (CurrentBeat == Beat.Telegraph && now >= BeatUntil)
                    {
                        CurrentBeat = Beat.Active;
                        BeatStart = now;
                        Commit(Move, now);
                    }
                    return;
                case Beat.Active:
                    if (ActiveDone(Move, now)) BeginRecovery(Move, now);
                    return;
                case Beat.Recovery:
                    if (!Window.IsOpen(now)) FinishMove(now);
                    return;
            }
            if (now < NextMoveAt || now < _phasePauseUntil || !CanStartMove(now)) return;
            UBossView v = View();
            UBossMove m = UnderBossProfiles.Choose(Profile.Id, v, UnityEngine.Random.value);
            if (m == UBossMove.None) return;
            StartMove(m, now);
        }

        /// <summary>What the boss sees right now (positions and its own state, never input).</summary>
        protected virtual UBossView View()
        {
            UBossView v = new UBossView();
            v.Phase = Phase;
            v.Distance = MathUtil.FlatDistance(PlayerFeet, Body.position);
            v.Airborne = PlayerAirborne;
            v.SinceSignature = SinceSignature;
            return v;
        }

        protected virtual bool CanStartMove(float now) { return true; }

        /// <summary>Starts a move's telegraph (the subclass lays out its shapes in OnTelegraph).</summary>
        protected void StartMove(UBossMove m, float now)
        {
            Move = m;
            MoveSerial++;
            if (UnderBossProfiles.IsSignature(m)) SinceSignature = 0;
            else SinceSignature++;
            CurrentBeat = Beat.Telegraph;
            BeatStart = now;
            BeatUntil = now + UBossTiming.Telegraph(m, Diff);
            Underheart04ESounds.Play(UhSfx.BossTelegraph, BodyCenter, 0.9f, 1f);
            try { OnTelegraph(m, BeatUntil - now); }
            catch (Exception e)
            {
                Log.Exception("Underheart boss telegraph " + Profile.Id + " " + m, e);
                CancelTelegraph(null);
            }
        }

        /// <summary>Lays out the move's telegraph shapes (they burst on their own when their fill ends).</summary>
        protected abstract void OnTelegraph(UBossMove m, float seconds);

        /// <summary>Called every frame while a move winds up (aim lines that follow at a limited rate, then lock).</summary>
        protected virtual void TickTelegraph(UBossMove m, float now) { }

        /// <summary>The wind-up is over: the aim locks and the move fires.</summary>
        protected abstract void Commit(UBossMove m, float now);

        /// <summary>True when the committed move has played out (dashes, breath and beams last a moment).</summary>
        protected virtual bool ActiveDone(UBossMove m, float now) { return true; }

        /// <summary>The punish window a move leaves (focus part, multiplier and the HUD line). seconds 0 for none.</summary>
        protected virtual void WindowFor(UBossMove m, out UBossPart focus, out float multiplier, out string label)
        {
            focus = UBossPart.Body;
            multiplier = 1.3f;
            label = "OPENING: STRIKE NOW";
        }

        private void BeginRecovery(UBossMove m, float now)
        {
            CurrentBeat = Beat.Recovery;
            float seconds = WindowSeconds(m);
            if (seconds <= 0f)
            {
                FinishMove(now);
                return;
            }
            UBossPart focus;
            float mul;
            string label;
            WindowFor(m, out focus, out mul, out label);
            OpenWindow(now, seconds, mul, focus, label);
        }

        /// <summary>Window seconds after a move (the profile's punish timing; subclasses can lengthen special cases).</summary>
        protected virtual float WindowSeconds(UBossMove m)
        {
            return UBossTiming.Recovery(m, Diff);
        }

        /// <summary>Opens (or extends) a punish window and says so.</summary>
        protected void OpenWindow(float now, float seconds, float mul, UBossPart focus, string label)
        {
            if (!(seconds > 0f)) return;
            bool fresh = !Window.IsOpen(now);
            Window.Open(now, seconds, mul, focus, label);
            if (CurrentBeat == Beat.Idle) CurrentBeat = Beat.Recovery;
            if (!fresh) return;
            Callout(label);
            Underheart04ESounds.Play(UhSfx.BossVulnerable, BodyCenter, 1f, 1f);
            OnWindowOpened(focus);
        }

        protected virtual void OnWindowOpened(UBossPart focus) { }

        private void FinishMove(float now)
        {
            CurrentBeat = Beat.Idle;
            Window.Close();
            NextMoveAt = now + UBossTiming.MoveGap(Phase, Diff, Pressured) * Mathf.Lerp(0.9f, 1.1f, UnityEngine.Random.value);
            OnMoveFinished();
        }

        protected virtual void OnMoveFinished() { }

        /// <summary>True while something drives the boss harder (the Matron's lures): shorter gaps between moves.</summary>
        protected virtual bool Pressured { get { return false; } }

        /// <summary>Breaks a move still winding up: its shapes vanish without hurting anyone and nothing commits.</summary>
        protected void CancelTelegraph(string callout)
        {
            if (CurrentBeat != Beat.Telegraph) return;
            ReleaseHazards(MoveSerial);
            OnTelegraphCancelled(Move);
            CurrentBeat = Beat.Idle;
            NextMoveAt = Time.time + 1.4f;
            if (!string.IsNullOrEmpty(callout)) Callout(callout);
        }

        protected virtual void OnTelegraphCancelled(UBossMove m) { }

        /// <summary>0 to 1 through the current telegraph (0 when none).</summary>
        public float TellProgress
        {
            get
            {
                if (CurrentBeat != Beat.Telegraph) return 0f;
                return Mathf.Clamp01((Time.time - BeatStart) / Mathf.Max(0.05f, BeatUntil - BeatStart));
            }
        }

        public bool Telling { get { return CurrentBeat == Beat.Telegraph; } }
        public UBossMove CurrentMove { get { return Move; } }
        public bool WindowOpen { get { return Window.IsOpen(Time.time); } }
        public PunishWindow04E PunishWindow { get { return Window; } }

        // ================================================================== damage

        /// <summary>
        /// Every hit on the body passes here first. Before the fight starts a hit wakes it (and does nothing); after the
        /// end nothing lands. Only the player's side counts (the player, a companion or a fused vehicle, all credited to
        /// the player). The struck part (nearest tagged collider) and the fight's state scale it; never to zero.
        /// </summary>
        public bool FilterDamage(ref DamageInfo info)
        {
            if (State == FightState.Victory || State == FightState.Ended) return false;
            if (!info.FromPlayer) return false;
            if (State != FightState.Fighting)
            {
                if (State == FightState.Dormant && MathUtil.FlatDistance(PlayerFeet, ArenaCenter) < ArenaRadius * 1.6f) Wake();
                return false;
            }
            UBossPartTag tag = PartAt(info.Point);
            // The damage number that follows this hit (UnderBossFight.Lock) is grouped per struck part.
            _lastHitTag = tag;
            UBossPart part = tag != null ? tag.Part : UBossPart.Body;
            UBossDamageState s = DamageState(tag);
            float now = Time.time;
            if (Window.IsOpen(now))
            {
                s.WindowOpen = true;
                s.WindowFocus = Window.Focus;
                s.WindowMul = Window.Multiplier;
            }
            float mul = UnderBossProfiles.PartMultiplier(Profile.Id, part, s) * UnderBossProfiles.ElementMultiplier(Profile, info.Element);
            info.Amount *= mul;
            info.Headshot = false;
            if (part == UBossPart.Weak || (s.CoreOpen && (part == UBossPart.Core || part == UBossPart.Regulator))) info.Crit = true;
            LastHitAt = now;
            try { OnPartHit(tag, part, info.Amount); }
            catch (Exception e) { Log.Exception("Underheart boss part hit " + Profile.Id, e); }
            return true;
        }

        /// <summary>The fight's damage state for a hit on a part (lures, cracks, open core, furnace shield).</summary>
        protected virtual UBossDamageState DamageState(UBossPartTag tag)
        {
            return new UBossDamageState();
        }

        /// <summary>A hit landed on a part (after scaling): seams fracture, flashes, glows.</summary>
        protected virtual void OnPartHit(UBossPartTag tag, UBossPart part, float amount) { }

        /// <summary>The tagged collider nearest to a world point (null when the body has none enabled).</summary>
        protected UBossPartTag PartAt(Vector3 point)
        {
            UBossPartTag best = null;
            float bd = float.MaxValue;
            for (int i = 0; i < _parts.Count; i++)
            {
                UBossPartTag t = _parts[i];
                if (t == null || t.Collider == null || !t.Collider.enabled || !t.gameObject.activeInHierarchy) continue;
                Vector3 c = t.Collider.ClosestPoint(point);
                float d = (c - point).sqrMagnitude;
                if (d < bd)
                {
                    bd = d;
                    best = t;
                }
            }
            return best;
        }

        /// <summary>Tagged parts of a kind (weak point markers for the HUD).</summary>
        public void PartsOf(UBossPart part, List<Vector3> into)
        {
            into.Clear();
            for (int i = 0; i < _parts.Count; i++)
            {
                UBossPartTag t = _parts[i];
                if (t == null || t.Part != part || t.Collider == null || !t.Collider.enabled || !t.gameObject.activeInHierarchy) continue;
                into.Add(t.Collider.bounds.center);
            }
        }

        private void OnBodyDied(DamageInfo info)
        {
            if (State != FightState.Fighting) return;
            CompleteVictory();
        }

        // ================================================================== victory and endings

        /// <summary>
        /// A confirmed death in a running fight: hazards end, loops stop, reinforcements leave, the exit opens, a quiet
        /// aftermath starts, the room is marked cleared, the first-win entitlements are granted once and Defeated fires.
        /// </summary>
        private void CompleteVictory()
        {
            if (!_victory.TryCommit()) return;
            float now = Time.time;
            State = FightState.Victory;
            EndReason = UBossEnd.Victory;
            _victoryAt = now;
            int hazards = ReleaseHazards(-1);
            int loops = StopLoops();
            int adds = Squad.Dismiss(true);
            Squad.Ledger.Close();
            Window.Close();
            CurrentBeat = Beat.Idle;
            try { OnVictory(); }
            catch (Exception e) { Log.Exception("Underheart boss victory props " + Profile.Id, e); }
            OpenArenaOnVictory();
            LocalQuiet.Release(LeaseOwner);
            LocalQuiet.AcquireAftermath(LeaseOwner + ":aftermath", ArenaCenter, ArenaRadius + 10f);
            if (Game.Underheart != null && !string.IsNullOrEmpty(Profile.RoomId))
            {
                try { Game.Underheart.MarkCleared(Profile.RoomId); }
                catch (Exception e) { Log.Exception("Underheart boss cleared room " + Profile.Id, e); }
            }
            _rewardLine = Owner != null ? Owner.GrantFirstWin(Profile) : "";
            if (Handle != null)
            {
                Handle.Done = true;
                Handle.Health01 = 0f;
                Handle.Status = Profile.Title + " is defeated. " + _rewardLine;
            }
            Underheart04ESounds.Play(UhSfx.BossDefeat, BodyCenter, 1f, 1f);
            if (Game.Hud != null) Game.Hud.Banner(Profile.Title.ToUpper() + " DEFEATED", _rewardLine, new Color(1f, 0.85f, 0.4f), 6f);
            if (Game.Cam != null) Game.Cam.Shake(0.6f);
            Log.Info("UBOSS04E " + Profile.Id + " VICTORY: hazards cancelled " + hazards + ", loops stopped " + loops + ", reinforcements dismissed " + adds
                + " (admitted " + Squad.Ledger.Spawned + "/" + Squad.Ledger.Declared + ", confirmed dead " + Squad.Ledger.Dead + "), windows opened " + Window.Count
                + ", phases entered " + Latch.Entered + " skipped " + Latch.Skipped);
            if (Owner != null) Owner.OnFightVictory(this);
        }

        protected virtual void OnVictory() { }

        /// <summary>
        /// Ends a running fight without a winner (death, leaving, withdrawing, abort, load, title, build failure): every
        /// owned hazard, loop, reinforcement, lease and object goes, the handle reports Failed with a reason. Never after
        /// a victory (the aftermath tidies itself).
        /// </summary>
        public void End(UBossEnd reason, string status)
        {
            if (State == FightState.Ended || State == FightState.Victory) return;
            State = FightState.Ended;
            EndReason = reason;
            Cleanup(reason != UBossEnd.Title && reason != UBossEnd.SessionReset);
            if (Handle != null)
            {
                Handle.Failed = true;
                Handle.Status = status ?? "";
            }
            if (Game.Hud != null && reason != UBossEnd.Title && reason != UBossEnd.SessionReset && reason != UBossEnd.Aborted && !string.IsNullOrEmpty(status)) Game.Hud.Notify(status, 4.5f);
            Log.Info("UBOSS04E " + (Profile != null ? Profile.Id : "boss") + " ended without victory (" + reason + "): " + status);
            if (Owner != null) Owner.OnFightEnded(this);
            Destroy(gameObject);
        }

        /// <summary>Removes the aftermath (the husk, the exit marker): a new fight starts, the session resets or the player left.</summary>
        public void Dispose()
        {
            if (State != FightState.Ended)
            {
                if (State != FightState.Victory) EndReason = UBossEnd.Aborted;
                State = FightState.Ended;
            }
            Cleanup(false);
            Destroy(gameObject);
        }

        private void Cleanup(bool puff)
        {
            if (_cleaned) return;
            _cleaned = true;
            int hazards = ReleaseHazards(-1);
            int loops = StopLoops();
            int adds = 0;
            if (Squad != null)
            {
                if (EndReason != UBossEnd.Victory) adds = Squad.Dismiss(puff);
                Squad.Ledger.Close();
            }
            try { OnCleanup(); }
            catch (Exception e) { Log.Exception("Underheart boss cleanup " + (Profile != null ? Profile.Id : "boss"), e); }
            LocalQuiet.Release(LeaseOwner);
            ReleaseCombatFeedback();
            if (Health != null)
            {
                Health.Died -= OnBodyDied;
                Health.Filter = null;
            }
            if (hazards + loops + adds > 0) Log.Info("UBOSS04E " + (Profile != null ? Profile.Id : "boss") + " cleanup: hazards " + hazards + ", loops " + loops + ", reinforcements " + adds);
        }

        /// <summary>Releases subclass-owned runtime state (lights, interactables, targets). Objects under this root go with it.</summary>
        protected virtual void OnCleanup() { }

        private void OnDestroy()
        {
            // Removed from outside (a scene change, a parent destroyed): never leave the handle hanging.
            if (Running && Handle != null && !Handle.Done)
            {
                Handle.Failed = true;
                Handle.Status = "The fight was removed before it ended.";
            }
            Cleanup(false);
        }

        private void UpdateAftermath(float dt, float now)
        {
            if (Health != null) Health.Burning = 0f;
            try { AnimateBody(dt, now); }
            catch (Exception e) { Log.Exception("Underheart boss aftermath " + Profile.Id, e); }
            UpdateArena(dt, now);
            float d = MathUtil.FlatDistance(Game.PlayerPosition, ArenaCenter);
            if (!_aftermathTidied && (now - _victoryAt > AftermathSeconds || d > ArenaRadius + 30f))
            {
                _aftermathTidied = true;
                TidyAftermath();
            }
            if (d > ArenaRadius + 150f || now - _victoryAt > AftermathSeconds * 2f) Dispose();
        }

        // ================================================================== debug

        /// <summary>F2: health just past the next threshold in one step (never lethal), to show the latch.</summary>
        public void DebugBurst()
        {
            if (State != FightState.Fighting || Health == null) return;
            float f = Health01;
            float target = 0.05f;
            for (int i = 0; i < Profile.PhaseThresholds.Length; i++)
            {
                if (f >= Profile.PhaseThresholds[i])
                {
                    target = Profile.PhaseThresholds[i] - 0.02f;
                    break;
                }
            }
            Health.Health = Mathf.Max(1f, target * Health.MaxHealth);
        }

        /// <summary>F2: opens the boss's own punish window now.</summary>
        public void DebugWindow()
        {
            if (State != FightState.Fighting) return;
            CancelTelegraph(null);
            UBossPart focus;
            float mul;
            string label;
            WindowFor(SignatureMove, out focus, out mul, out label);
            OpenWindow(Time.time, UBossTiming.Window(2.5f, Diff), mul, focus, label);
        }

        /// <summary>F2: a debug victory (counts as a real win: the ledger still grants each token once).</summary>
        public void DebugKill()
        {
            if (State == FightState.Dormant) Wake();
            if (State == FightState.Intro) StartFighting(Time.time);
            if (State != FightState.Fighting || Health == null || Game.Player == null) return;
            Health.Kill(Game.Player.gameObject, DamageType.Power);
        }

        protected virtual UBossMove SignatureMove { get { return UBossMove.None; } }

        /// <summary>One line of state for the F2 page (built on demand).</summary>
        public virtual string DebugLine
        {
            get
            {
                return Profile.Id + " " + State + " phase " + (Phase + 1) + "/" + Profile.PhaseCount + " health " + Mathf.RoundToInt(Health01 * 100f) + "% beat " + CurrentBeat + " " + Move
                    + " window " + (WindowOpen ? Window.Label : "-") + " adds " + Squad.Living + " (admitted " + Squad.Ledger.Spawned + "/" + Squad.Ledger.Declared + ") hazards " + LiveHazards
                    + (PlayerAirborne ? " player airborne" : "") + " lights " + _lightsCreated + " lock " + (_lockTag != null ? _lockTag.Part.ToString() : "body");
            }
        }

        // ================================================================== HUD hooks

        /// <summary>The boss bar's right-hand label.</summary>
        public virtual string PhaseLabel
        {
            get
            {
                if (State == FightState.Dormant) return "WAITING BEYOND THE BOUNDARY";
                if (WindowOpen) return Window.Label;
                return Profile.PhaseNames != null && Phase < Profile.PhaseNames.Length ? Profile.PhaseNames[Phase] : "";
            }
        }

        /// <summary>A short line under the bar naming the fight's live mechanic (lures, seams, furnaces), "" for none.</summary>
        public virtual string MechanicLine { get { return ""; } }

        /// <summary>World points the HUD rings as targets right now (weak points during windows, lures, vents).</summary>
        public virtual void Targets(List<Vector3> into, List<string> labels)
        {
            into.Clear();
            labels.Clear();
        }

        // ================================================================== helpers

        /// <summary>The player's feet (the vehicle when driving).</summary>
        protected static Vector3 PlayerFeet
        {
            get
            {
                PlayerController p = Game.Player;
                if (p == null) return Game.PlayerPosition;
                if (p.InVehicle && p.CurrentVehicle != null) return p.CurrentVehicle.transform.position;
                return p.transform.position;
            }
        }

        /// <summary>Where aimed attacks aim: the chest on foot or in the air, the vehicle when driving. Never a prediction.</summary>
        protected static Vector3 PlayerAim
        {
            get
            {
                PlayerController p = Game.Player;
                if (p == null) return Game.PlayerPosition + Vector3.up;
                if (p.InVehicle && p.CurrentVehicle != null) return p.CurrentVehicle.transform.position + Vector3.up;
                return p.ChestPosition;
            }
        }

        /// <summary>The body's chest point (sounds, banners, HUD).</summary>
        public virtual Vector3 BodyCenter { get { return Body != null ? Body.position + Vector3.up * 2f : ArenaCenter; } }

        /// <summary>Where the HUD puts the "!!" over the boss.</summary>
        public virtual Vector3 HeadPoint { get { return Body != null ? Body.position + Vector3.up * 4f : ArenaCenter; } }

        protected static void Callout(string text)
        {
            if (Game.Hud != null && !string.IsNullOrEmpty(text)) Game.Hud.CombatText(text, new Color(1f, 0.78f, 0.4f));
        }

        protected int StartLoop(UhLoop loop, Vector3 pos, float volume)
        {
            int k = Underheart04ESounds.StartLoop(loop, pos, volume);
            if (k != 0) _loops.Add(k);
            return k;
        }

        protected void StopLoop(int key)
        {
            if (key == 0) return;
            Underheart04ESounds.StopLoop(key);
            _loops.Remove(key);
        }

        private int StopLoops()
        {
            int n = _loops.Count;
            for (int i = 0; i < _loops.Count; i++) Underheart04ESounds.StopLoop(_loops[i]);
            _loops.Clear();
            return n;
        }

        /// <summary>Turns the body toward a flat direction at a limited rate (degrees per second). Returns the angle left.</summary>
        protected float TurnToward(Vector3 dir, float rate, float dt)
        {
            Vector3 f = MathUtil.Flat(dir);
            if (f.sqrMagnitude < 1e-4f) return 0f;
            float want = MathUtil.YawOf(f);
            float delta = Mathf.DeltaAngle(BodyYaw, want);
            float step = Mathf.Clamp(delta, -rate * dt, rate * dt);
            BodyYaw += step;
            Body.rotation = Quaternion.Euler(0f, BodyYaw, 0f);
            return Mathf.Abs(delta - step);
        }

        /// <summary>Walks the body along a flat direction, staying inside the arena, off walls and on the floor.</summary>
        protected bool Step(Vector3 dir, float distance, float bodyRadius)
        {
            Vector3 f = MathUtil.Flat(dir);
            if (f.sqrMagnitude < 1e-6f || distance <= 0f) return false;
            f.Normalize();
            Vector3 from = Body.position;
            Vector3 to = from + f * distance;
            Vector3 off = MathUtil.Flat(to - ArenaCenter);
            float limit = ArenaRadius * 0.8f;
            if (off.magnitude > limit) to = ArenaCenter + off.normalized * limit + Vector3.up * (to.y - ArenaCenter.y);
            Vector3 probe = from + Vector3.up * Mathf.Max(1f, bodyRadius);
            if (Physics.SphereCast(probe, Mathf.Max(0.3f, bodyRadius * 0.7f), f, out RaycastHit hit, distance + bodyRadius * 0.6f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return false;
            float y;
            if (UnderBossFx.Floor(to, 1.8f, 4f, out y) && Mathf.Abs(y - from.y) < 1.2f) to.y = Mathf.Lerp(from.y, y, 0.5f);
            else to.y = from.y;
            Body.position = to;
            return true;
        }
    }
}
