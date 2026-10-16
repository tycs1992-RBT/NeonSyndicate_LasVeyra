using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The moves of the three upgraded 04D boss fights: each boss's signature lesson, its fillers and the Djinn's shift.</summary>
    public enum BossMove04D
    {
        None = 0,
        /// <summary>The Foreman's delayed gravity orb: a short pull, then a rupture ring and marked rupture lanes.</summary>
        ForemanOrb,
        /// <summary>The Foreman's crystal rain (the original ring volley).</summary>
        ForemanRain,
        /// <summary>The Foreman's widening ring slam (the original slam).</summary>
        ForemanSlam,
        /// <summary>The Foreman's thrown shards (the original arcing throw, aimed at the chest so it reaches a flyer).</summary>
        ForemanThrow,
        /// <summary>The Djinn's shard choir: a fan of guided shards with a limited turn rate that stop steering out of sight.</summary>
        DjinnChoir,
        /// <summary>The Djinn's sandstorm rings (the original ring volley).</summary>
        DjinnSandstorm,
        /// <summary>The Djinn shifts to a new legal lane.</summary>
        DjinnRelocate,
        /// <summary>The Queen's reflected beams: committed beam lanes bounced off her mirror focus points.</summary>
        QueenBeams,
        /// <summary>The Queen's glass shatter rings (the original ring volley).</summary>
        QueenShatter
    }

    /// <summary>Why a boss fight's owned hazards, lanes, projectiles, focus points and summons were cleaned up.</summary>
    public enum BossCleanup04D
    {
        None = 0,
        /// <summary>The registered boss body died (the director decides whether it was the player's victory).</summary>
        BossDied,
        /// <summary>The player walked out of the arena and the fight reset.</summary>
        PlayerLeft,
        /// <summary>The Veil closed during the Queen's fight.</summary>
        VeilExit,
        /// <summary>The player died (or was busted) during the fight.</summary>
        PlayerDied,
        /// <summary>The game went back to the title screen.</summary>
        Title,
        /// <summary>A save was loaded or a new game started.</summary>
        Load,
        /// <summary>The boss body was removed for any other reason (a quest despawn, the debug page).</summary>
        Removed
    }

    /// <summary>The kinds of things a boss fight owns and must release (counted by BossOwnedLedger04D).</summary>
    public enum BossOwned04D
    {
        Ring = 0,
        Lane,
        Beam,
        Bolt,
        Orb,
        Focus,
        Decoy,
        Support,
        Illusion,
        Arrival
    }

    /// <summary>What the move chooser knows about the fight right now.</summary>
    public struct BossFightView04D
    {
        /// <summary>0, 1 or 2 (the boss bar's three phases).</summary>
        public int Phase;
        /// <summary>Flat metres from the boss to the player.</summary>
        public float Distance;
        /// <summary>The player is high enough above the ground that ground rings cannot reach them.</summary>
        public bool Airborne;
        /// <summary>The Foreman's hazard plates are still up.</summary>
        public bool PlatesUp;
        /// <summary>Moves since the boss last used its signature move.</summary>
        public int SinceSignature;
        /// <summary>Moves since the Djinn last shifted to a new lane.</summary>
        public int SinceRelocate;
    }

    /// <summary>
    /// One upgraded boss: which reward its fight teaches (CrystalFocusDb), its signature move, its two phase thresholds,
    /// its support budget, how much more its exposed window takes, and the lines the HUD shows.
    /// </summary>
    public sealed class BossProfile04D
    {
        public string BossId;
        public HumanoidCreature Kind;
        /// <summary>The CrystalFocusDb unique index this fight's first win grants (CrystalFocusDb.SignatureFor).</summary>
        public int SignatureUnique;
        public BossMove04D Signature;
        /// <summary>Health fractions that open phases two and three, highest first (the boss bar's marks).</summary>
        public float[] PhaseThresholds;
        /// <summary>Support actors alive at once (the Foreman's golems, the Djinn's mirages, the Queen's copies).</summary>
        public int MaxSupport;
        /// <summary>Finite reinforcement batches for the whole fight.</summary>
        public int ReinforcementBatches;
        /// <summary>Damage taken multiplier while the exposed window is open.</summary>
        public float CoreBonus;
        /// <summary>The boss bar line while the exposed window is open.</summary>
        public string WindowLabel;
        /// <summary>What phase one teaches (the docs and the debug page).</summary>
        public string Lesson;
        /// <summary>The player's counter (the docs and the debug page).</summary>
        public string Counter;
    }

    /// <summary>
    /// 04D boss profiles for the Shard Golem Foreman, the Mirage Djinn and the Mirror Wraith Queen (owner: bosses), plus
    /// every rule their fights need that is plain maths: the move chooser (signature cadence, and a move that reaches a
    /// flying player whenever the player flies), damage taken while the window is open, the Foreman's bounded pull, the
    /// Djinn's choir numbers and shift rule, the Queen's focus counts and the cleanup classification. The Glacier Titan
    /// has no profile and keeps its original script. Engine-free (tested offline).
    /// </summary>
    public static class BossProfiles04D
    {
        /// <summary>Health fraction under which phase two opens (matches the boss bar's mark and the original script).</summary>
        public const float PhaseTwoAt = 0.66f;
        /// <summary>Health fraction under which phase three opens.</summary>
        public const float PhaseThreeAt = 0.33f;
        public const int MaxSupportActors = 2;
        public const int ReinforcementBatches = 1;
        /// <summary>Metres above the ground past which ground rings (3 m tall) cannot reach the player.</summary>
        public const float AirHeight = 3.2f;
        /// <summary>The Foreman's pull: radius in metres, and the one-off velocity it adds (decays at PullDecay m/s per s).</summary>
        public const float PullRadius = 6f;
        public const float PullSpeed = 3.5f;
        public const float PullDecay = 12f;
        /// <summary>Seconds the orb hangs and pulls before it ruptures.</summary>
        public const float PullSeconds = 0.4f;
        /// <summary>Radius of the orb's rupture, ground ring or burst in the air.</summary>
        public const float RuptureRadius = 3.5f;
        /// <summary>Half width and length of the Foreman's rupture lanes (radial around the orb while the plates hold).</summary>
        public const float LaneHalfWidth = 1.1f;
        public const float RadialLaneLength = 12f;
        /// <summary>Degrees between the fan lanes the Foreman cuts from itself once its plates are broken.</summary>
        public const float FanSpread = 28f;
        /// <summary>Speed of the Djinn's choir shards in metres per second.</summary>
        public const float ChoirSpeed = 14f;
        public const float ChoirLife = 3.5f;
        /// <summary>The longest a choir shard may fly (time to reach a high flyer plus a second, at most this).</summary>
        public const float ChoirMaxLife = 6f;
        /// <summary>Seconds between the shards of one volley leaving the Djinn.</summary>
        public const float ChoirStagger = 0.12f;
        /// <summary>Half width of the Queen's beam lanes and the beam's hit radius around its committed line.</summary>
        public const float BeamRadius = 0.85f;
        public const float BeamLength = 34f;
        /// <summary>The longest a beam leg may reach (far enough for a high flyer, never across the map).</summary>
        public const float BeamMaxLength = 80f;
        /// <summary>The Djinn's shift: inside this share of the arena radius, this far from the player and its old spot.</summary>
        public const float RelocateArenaShare = 0.85f;
        public const float RelocateMinFromPlayer = 10f;
        public const float RelocateMaxFromPlayer = 28f;
        public const float RelocateMinMove = 8f;

        private static BossProfile04D[] _all;

        public static void ResetStatics()
        {
            _all = null;
        }

        private static BossProfile04D[] All
        {
            get
            {
                if (_all == null) _all = Build();
                return _all;
            }
        }

        private static BossProfile04D[] Build()
        {
            return new[]
            {
                new BossProfile04D
                {
                    BossId = "golem_foreman", Kind = HumanoidCreature.GolemForeman, SignatureUnique = CrystalFocusDb.SignatureFor("golem_foreman"),
                    Signature = BossMove04D.ForemanOrb, PhaseThresholds = new[] { PhaseTwoAt, PhaseThreeAt }, MaxSupport = MaxSupportActors,
                    ReinforcementBatches = ReinforcementBatches, CoreBonus = 1.6f, WindowLabel = "CORE EXPOSED: STRIKE NOW",
                    Lesson = "A delayed gravity orb pulls, then ruptures in a ring and marked lanes; the core opens after each discharge.",
                    Counter = "Leave the ring and the lanes, break the hazard plates, punish the open core."
                },
                new BossProfile04D
                {
                    BossId = "mirage_djinn", Kind = HumanoidCreature.MirageDjinn, SignatureUnique = CrystalFocusDb.SignatureFor("mirage_djinn"),
                    Signature = BossMove04D.DjinnChoir, PhaseThresholds = new[] { PhaseTwoAt, PhaseThreeAt }, MaxSupport = MaxSupportActors,
                    ReinforcementBatches = ReinforcementBatches, CoreBonus = 1.35f, WindowLabel = "THE DJINN IS SPENT: STRIKE NOW",
                    Lesson = "Shard choir volleys with a real casting tell among harmless decoy pillars.",
                    Counter = "Break sight or cross the volley's lane, watch the body that casts a shadow."
                },
                new BossProfile04D
                {
                    BossId = "wraith_queen", Kind = HumanoidCreature.WraithQueen, SignatureUnique = CrystalFocusDb.SignatureFor("wraith_queen"),
                    Signature = BossMove04D.QueenBeams, PhaseThresholds = new[] { PhaseTwoAt, PhaseThreeAt }, MaxSupport = MaxSupportActors,
                    ReinforcementBatches = ReinforcementBatches, CoreBonus = 1.5f, WindowLabel = "THE QUEEN'S CORE IS OPEN",
                    Lesson = "Reflected beam lanes bounce off visible mirror focus points; her core opens after each pattern.",
                    Counter = "Step out of the lanes or shatter a focus, then strike the open core."
                }
            };
        }

        /// <summary>The profile of a boss id, or null (the Glacier Titan and every other id have none).</summary>
        public static BossProfile04D For(string bossId)
        {
            if (string.IsNullOrEmpty(bossId)) return null;
            BossProfile04D[] all = All;
            for (int i = 0; i < all.Length; i++) if (all[i].BossId == bossId) return all[i];
            return null;
        }

        /// <summary>The profile of a humanoid creature kind, or null.</summary>
        public static BossProfile04D For(HumanoidCreature kind)
        {
            if (kind == HumanoidCreature.None) return null;
            BossProfile04D[] all = All;
            for (int i = 0; i < all.Length; i++) if (all[i].Kind == kind) return all[i];
            return null;
        }

        public static bool IsUpgraded(string bossId)
        {
            return For(bossId) != null;
        }

        // ================================================================== moves

        /// <summary>The line the HUD shows while a move winds up (what it is and how to answer it).</summary>
        public static string MoveLabel(BossMove04D m)
        {
            switch (m)
            {
                case BossMove04D.ForemanOrb: return "GRAVITY ORB: LEAVE THE RING AND THE MARKED LANES";
                case BossMove04D.ForemanRain: return "CRYSTAL RAIN";
                case BossMove04D.ForemanSlam: return "SLAM: DODGE THE RINGS";
                case BossMove04D.ForemanThrow: return "SHARD THROW";
                case BossMove04D.DjinnChoir: return "SHARD CHOIR: BREAK SIGHT OR CROSS ITS LANE";
                case BossMove04D.DjinnSandstorm: return "SANDSTORM";
                case BossMove04D.DjinnRelocate: return "THE DJINN SHIFTS";
                case BossMove04D.QueenBeams: return "REFLECTED BEAMS: LEAVE THE LANES OR SHATTER A FOCUS";
                case BossMove04D.QueenShatter: return "GLASS SHATTER";
                default: return "";
            }
        }

        /// <summary>A short name for a move (the "interrupted" callout).</summary>
        public static string MoveShortName(BossMove04D m)
        {
            switch (m)
            {
                case BossMove04D.ForemanOrb: return "gravity orb";
                case BossMove04D.DjinnChoir: return "shard choir";
                case BossMove04D.DjinnRelocate: return "shift";
                case BossMove04D.QueenBeams: return "reflected beams";
                default: return "move";
            }
        }

        /// <summary>True for a move that can hurt a player hovering above the ground rings.</summary>
        public static bool ReachesAir(BossMove04D m)
        {
            return m == BossMove04D.ForemanOrb || m == BossMove04D.ForemanThrow || m == BossMove04D.DjinnChoir || m == BossMove04D.QueenBeams;
        }

        /// <summary>True for a boss's signature move (the one its reward is built from).</summary>
        public static bool IsSignature(BossMove04D m)
        {
            return m == BossMove04D.ForemanOrb || m == BossMove04D.DjinnChoir || m == BossMove04D.QueenBeams;
        }

        /// <summary>
        /// The next move. The signature comes at least every other move (the fight keeps teaching its reward); a player
        /// flying above the rings always faces a move that reaches the air (the Djinn may still shift lanes now and then);
        /// fillers are the original ring moves. roll is a random number in [0, 1).
        /// </summary>
        public static BossMove04D Choose(HumanoidCreature kind, BossFightView04D v, float roll)
        {
            switch (kind)
            {
                case HumanoidCreature.GolemForeman:
                    if (v.Airborne) return v.SinceSignature >= 1 || roll < 0.5f ? BossMove04D.ForemanOrb : BossMove04D.ForemanThrow;
                    if (v.SinceSignature >= 1) return BossMove04D.ForemanOrb;
                    if (v.Distance < 10f && roll < 0.6f) return BossMove04D.ForemanSlam;
                    return BossMove04D.ForemanRain;
                case HumanoidCreature.MirageDjinn:
                    if (v.SinceRelocate >= 3) return BossMove04D.DjinnRelocate;
                    if (v.Airborne) return BossMove04D.DjinnChoir;
                    if (v.SinceSignature >= 1) return BossMove04D.DjinnChoir;
                    if (v.Distance < 7f && v.SinceRelocate >= 1) return BossMove04D.DjinnRelocate;
                    return roll < 0.6f ? BossMove04D.DjinnSandstorm : BossMove04D.DjinnChoir;
                case HumanoidCreature.WraithQueen:
                    if (v.Airborne || v.SinceSignature >= 1) return BossMove04D.QueenBeams;
                    return BossMove04D.QueenShatter;
                default:
                    return BossMove04D.None;
            }
        }

        // ================================================================== fight numbers

        /// <summary>
        /// Damage taken multiplier: the base (the Foreman's plates halve hits until broken, then 1.15 like the original
        /// script) times the profile's bonus while the exposed window is open.
        /// </summary>
        public static float DamageTakenScale(float baseScale, bool exposed, float coreBonus)
        {
            return Mathf.Max(0f, baseScale) * (exposed ? Mathf.Max(1f, coreBonus) : 1f);
        }

        /// <summary>The Foreman's damage base: plates up halve, broken plates 1.15 (unchanged from the original fight).</summary>
        public static float ForemanBaseScale(bool platesUp)
        {
            return platesUp ? 0.5f : 1.15f;
        }

        /// <summary>True when the player is high enough above the ground that ground rings cannot reach them.</summary>
        public static bool Airborne(float heightAboveGround)
        {
            return heightAboveGround > AirHeight;
        }

        /// <summary>
        /// The Foreman orb's pull on the player: a one-off velocity toward the orb inside PullRadius, nothing outside it,
        /// nothing through a wall (blocked) and nothing on a player in a vehicle.
        /// </summary>
        public static float PullVelocity(float distance, bool blocked, bool inVehicle)
        {
            if (blocked || inVehicle || distance > PullRadius || distance < 0.2f) return 0f;
            return PullSpeed;
        }

        /// <summary>How far a one-off velocity carries the player before it decays away (metres).</summary>
        public static float PullDisplacement(float velocity)
        {
            float v = Mathf.Max(0f, velocity);
            return v * v / (2f * PullDecay);
        }

        /// <summary>Shards in one choir volley.</summary>
        public static int ChoirCount(DifficultyLevel d)
        {
            return d == DifficultyLevel.Story ? 4 : (d == DifficultyLevel.Balanced ? 5 : 6);
        }

        /// <summary>Degrees per second a choir shard may turn while it still steers.</summary>
        public static float ChoirTurnRate(DifficultyLevel d)
        {
            return d == DifficultyLevel.Story ? 45f : (d == DifficultyLevel.Balanced ? 65f : 85f);
        }

        /// <summary>Seconds a choir shard steers after it leaves the Djinn; after that it flies straight.</summary>
        public static float ChoirSteerSeconds(DifficultyLevel d)
        {
            return d == DifficultyLevel.Story ? 0.7f : (d == DifficultyLevel.Balanced ? 0.9f : 1.1f);
        }

        /// <summary>A choir shard's life for a target this far away: at least ChoirLife, at most ChoirMaxLife.</summary>
        public static float ChoirLifeFor(float distance)
        {
            return Mathf.Clamp(Mathf.Max(0f, distance) / ChoirSpeed + 1f, ChoirLife, ChoirMaxLife);
        }

        /// <summary>A beam leg's length for a target this far away: at least BeamLength, at most BeamMaxLength.</summary>
        public static float BeamLengthFor(float distance)
        {
            return Mathf.Clamp(Mathf.Max(0f, distance) + 6f, BeamLength, BeamMaxLength);
        }

        /// <summary>Shards of one volley that may hurt the player (the rest pass harmlessly).</summary>
        public static int ChoirMaxHits(DifficultyLevel d)
        {
            return d == DifficultyLevel.Story ? 1 : (d == DifficultyLevel.Balanced ? 2 : 3);
        }

        /// <summary>The Queen's mirror focus points at once.</summary>
        public static int FociCount(DifficultyLevel d)
        {
            return d == DifficultyLevel.Veteran ? 4 : 3;
        }

        /// <summary>Beams in one of the Queen's patterns (one in phase one, then two).</summary>
        public static int BeamsPerPattern(int phase)
        {
            return phase <= 0 ? 1 : 2;
        }

        /// <summary>Health of one mirror focus point from the Queen's max health.</summary>
        public static float FocusHealth(float bossMaxHealth)
        {
            return Mathf.Max(90f, Mathf.Max(0f, bossMaxHealth) * 0.04f);
        }

        /// <summary>
        /// True when a spot is a legal new lane for the Djinn: well inside the arena, neither on top of the player nor far
        /// from them, and a real move away from where it stood (the runtime adds ground, room and a clear line).
        /// </summary>
        public static bool RelocateValid(Vector3 candidate, Vector3 player, Vector3 oldSpot, Vector3 arenaCenter, float arenaRadius)
        {
            if (Flat(candidate - arenaCenter).magnitude > Mathf.Max(5f, arenaRadius) * RelocateArenaShare) return false;
            float dp = Flat(candidate - player).magnitude;
            if (dp < RelocateMinFromPlayer || dp > RelocateMaxFromPlayer) return false;
            return Flat(candidate - oldSpot).magnitude >= RelocateMinMove;
        }

        /// <summary>
        /// The cleanup reason from what the runtime sees when the fight's owner goes away. A dead boss wins over every
        /// other signal; a session reset is a title (the player is gone) or a load or new game (the player is placed again).
        /// </summary>
        public static BossCleanup04D ClassifyCleanup(bool bossDead, bool loading, bool playerGone, bool playerDead, bool veilLost, bool playerOutside)
        {
            if (bossDead) return BossCleanup04D.BossDied;
            if (loading) return playerGone ? BossCleanup04D.Title : BossCleanup04D.Load;
            if (playerDead) return BossCleanup04D.PlayerDied;
            if (veilLost) return BossCleanup04D.VeilExit;
            if (playerOutside) return BossCleanup04D.PlayerLeft;
            return BossCleanup04D.Removed;
        }

        /// <summary>
        /// Only a dead boss can be a victory (the director still checks that the player dealt the blow and owns the
        /// defeat record and the reward). Every other cleanup is a reset: the arena waits and nothing is granted.
        /// </summary>
        public static bool CleanupMayBeVictory(BossCleanup04D reason)
        {
            return reason == BossCleanup04D.BossDied;
        }

        private static Vector3 Flat(Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }
    }

    /// <summary>
    /// Telegraph (wind-up) and recovery (exposed window) seconds per move and difficulty. Balanced and Veteran keep
    /// dangerous wind-ups in 0.7 to 1.2 s and windows in 1 to 2 s; Story is longer on both (up to 1.6 s and 2.5 s).
    /// Also the Queen's aim tracking before she commits, the Djinn's illusion length and the pause between moves.
    /// </summary>
    public static class BossTiming04D
    {
        public const float TelegraphMin = 0.7f;
        public const float TelegraphMax = 1.2f;
        public const float StoryTelegraphMax = 1.6f;
        public const float RecoveryMin = 1f;
        public const float RecoveryMax = 2f;
        public const float StoryRecoveryMax = 2.5f;

        public static void ResetStatics() { }

        /// <summary>The Balanced wind-up of a move.</summary>
        public static float BaseTelegraph(BossMove04D m)
        {
            switch (m)
            {
                case BossMove04D.ForemanOrb: return 1.1f;
                case BossMove04D.ForemanRain: return 0.9f;
                case BossMove04D.ForemanSlam: return 1.05f;
                case BossMove04D.ForemanThrow: return 0.9f;
                case BossMove04D.DjinnChoir: return 1f;
                case BossMove04D.DjinnSandstorm: return 1f;
                case BossMove04D.DjinnRelocate: return 0.8f;
                case BossMove04D.QueenBeams: return 1.15f;
                case BossMove04D.QueenShatter: return 0.95f;
                default: return 1f;
            }
        }

        /// <summary>The Balanced exposed window after a move (the signature moves leave the long windows).</summary>
        public static float BaseRecovery(BossMove04D m)
        {
            switch (m)
            {
                case BossMove04D.ForemanOrb: return 1.6f;
                case BossMove04D.DjinnChoir: return 1.3f;
                case BossMove04D.QueenBeams: return 1.7f;
                default: return 1f;
            }
        }

        /// <summary>Seconds a move winds up before it commits, for a difficulty.</summary>
        public static float Telegraph(BossMove04D m, DifficultyLevel d)
        {
            float b = BaseTelegraph(m);
            switch (d)
            {
                case DifficultyLevel.Story: return Mathf.Clamp(b * 1.3f, TelegraphMin, StoryTelegraphMax);
                case DifficultyLevel.Veteran: return Mathf.Clamp(b * 0.85f, TelegraphMin, TelegraphMax);
                default: return Mathf.Clamp(b, TelegraphMin, TelegraphMax);
            }
        }

        /// <summary>Seconds the boss stays open after a committed move, for a difficulty.</summary>
        public static float Recovery(BossMove04D m, DifficultyLevel d)
        {
            float b = BaseRecovery(m);
            switch (d)
            {
                case DifficultyLevel.Story: return Mathf.Clamp(b * 1.25f, RecoveryMin, StoryRecoveryMax);
                case DifficultyLevel.Veteran: return Mathf.Clamp(b * 0.8f, RecoveryMin, RecoveryMax);
                default: return Mathf.Clamp(b, RecoveryMin, RecoveryMax);
            }
        }

        /// <summary>Seconds the Queen's beams follow the player (at a limited turn rate) before their lanes commit.</summary>
        public static float AcquireSeconds(DifficultyLevel d)
        {
            return d == DifficultyLevel.Story ? 0.6f : (d == DifficultyLevel.Balanced ? 0.5f : 0.4f);
        }

        /// <summary>Degrees per second the Queen's aim may turn while it still follows the player.</summary>
        public static float AimTurnRate(DifficultyLevel d)
        {
            return d == DifficultyLevel.Story ? 45f : (d == DifficultyLevel.Balanced ? 60f : 80f);
        }

        /// <summary>Seconds the Djinn's one illusion phase lasts at most.</summary>
        public static float IllusionSeconds(DifficultyLevel d)
        {
            return d == DifficultyLevel.Story ? 16f : (d == DifficultyLevel.Balanced ? 13f : 11f);
        }

        /// <summary>Seconds of plain fighting between two moves (shorter in later phases, never under 1.2 s).</summary>
        public static float MoveGap(int phase, DifficultyLevel d)
        {
            float g = 2.4f - 0.3f * Mathf.Clamp(phase, 0, 2);
            if (d == DifficultyLevel.Story) g *= 1.35f;
            else if (d == DifficultyLevel.Veteran) g *= 0.85f;
            return Mathf.Max(1.2f, g);
        }
    }

    /// <summary>
    /// A boss's phase thresholds, each opened at most once. A burst of damage that crosses several thresholds in one
    /// step enters the deepest phase and marks the ones it passed as skipped, so a phase batch can never run twice
    /// (pause, simultaneous collider hits and healing cannot reopen one).
    /// </summary>
    public sealed class BossPhaseLatch04D
    {
        private readonly float[] _thresholds;
        private readonly bool[] _fired;

        /// <summary>0 before any threshold, then the number of the deepest threshold opened (1 or 2 for the bosses).</summary>
        public int Phase { get; private set; }
        /// <summary>Thresholds passed without their own transition (a burst went straight past them).</summary>
        public int Skipped { get; private set; }
        /// <summary>Transitions actually entered.</summary>
        public int Entered { get; private set; }

        public BossPhaseLatch04D(float[] thresholds)
        {
            int n = thresholds != null ? thresholds.Length : 0;
            _thresholds = new float[n];
            _fired = new bool[n];
            for (int i = 0; i < n; i++) _thresholds[i] = thresholds[i];
            // Highest first, so index i is phase i + 1.
            for (int i = 1; i < n; i++)
            {
                float v = _thresholds[i];
                int j = i - 1;
                while (j >= 0 && _thresholds[j] < v)
                {
                    _thresholds[j + 1] = _thresholds[j];
                    j--;
                }
                _thresholds[j + 1] = v;
            }
        }

        public int Count { get { return _thresholds.Length; } }

        public bool Fired(int index)
        {
            return index >= 0 && index < _fired.Length && _fired[index];
        }

        /// <summary>
        /// Looks at the boss's health fraction: returns the phase to enter now (1 based), or 0 when nothing opens. skipped
        /// is how many thresholds this step passed without their own transition.
        /// </summary>
        public int Evaluate(float healthFraction, out int skipped)
        {
            skipped = 0;
            if (float.IsNaN(healthFraction)) return 0;
            int deepest = -1;
            for (int i = 0; i < _thresholds.Length; i++) if (healthFraction < _thresholds[i]) deepest = i;
            if (deepest < 0 || _fired[deepest]) return 0;
            for (int i = 0; i < deepest; i++)
            {
                if (_fired[i]) continue;
                _fired[i] = true;
                skipped++;
            }
            _fired[deepest] = true;
            Phase = deepest + 1;
            Skipped += skipped;
            Entered++;
            return Phase;
        }
    }

    /// <summary>
    /// A boss's support budget: at most MaxAlive support actors at once and a finite number of reinforcement batches for
    /// the whole fight. A batch is reserved (consumed) before anything is scheduled, so a failed or interrupted spawn never
    /// gives a batch back, a support's death never reopens one, and a closed budget (the fight is over) admits nothing.
    /// </summary>
    public sealed class BossAddBudget04D
    {
        public readonly int MaxAlive;
        public int BatchesLeft { get; private set; }
        /// <summary>Support actors admitted over the whole fight.</summary>
        public int Admitted { get; private set; }
        public bool Closed { get; private set; }

        public BossAddBudget04D(int maxAlive, int batches)
        {
            MaxAlive = Mathf.Max(0, maxAlive);
            BatchesLeft = Mathf.Max(0, batches);
        }

        /// <summary>Takes one batch reservation (false when none is left or the fight is over).</summary>
        public bool TryReserveBatch()
        {
            if (Closed || BatchesLeft <= 0) return false;
            BatchesLeft--;
            return true;
        }

        /// <summary>How many of requested may be admitted now with alive already in the fight.</summary>
        public int Allowance(int requested, int alive)
        {
            if (Closed || requested <= 0) return 0;
            return Mathf.Clamp(Mathf.Min(requested, MaxAlive - Mathf.Max(0, alive)), 0, MaxAlive);
        }

        /// <summary>Records that n support actors actually arrived.</summary>
        public void NoteAdmitted(int n)
        {
            if (n > 0) Admitted += n;
        }

        /// <summary>The fight ended: nothing more is reserved or admitted.</summary>
        public void Close()
        {
            Closed = true;
            BatchesLeft = 0;
        }
    }

    /// <summary>
    /// What a boss fight owns right now, by kind (rings, lanes, beams, shards, the orb, focus points, decoys, supports,
    /// the illusion and scheduled arrivals). Closing it once, with a reason, releases everything and refuses anything new,
    /// so a delayed arrival scheduled before the boss died or the player left can never appear afterwards.
    /// </summary>
    public sealed class BossOwnedLedger04D
    {
        private readonly int[] _live = new int[(int)BossOwned04D.Arrival + 1];

        public bool Closed { get; private set; }
        public BossCleanup04D Reason { get; private set; }
        /// <summary>Items still owned when the ledger closed (what the cleanup released).</summary>
        public int ReleasedOnClose { get; private set; }

        /// <summary>Registers a new owned item; false (and nothing is registered) once the ledger is closed.</summary>
        public bool Open(BossOwned04D kind)
        {
            if (Closed) return false;
            _live[(int)kind]++;
            return true;
        }

        /// <summary>An owned item ended on its own (a ring burst, a shard landed, a support died).</summary>
        public void Release(BossOwned04D kind)
        {
            int k = (int)kind;
            if (_live[k] > 0) _live[k]--;
        }

        public int Live(BossOwned04D kind)
        {
            return _live[(int)kind];
        }

        public int TotalLive
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _live.Length; i++) n += _live[i];
                return n;
            }
        }

        /// <summary>Closes the ledger (true the first time only): every owned item counts as released.</summary>
        public bool Close(BossCleanup04D reason)
        {
            if (Closed) return false;
            Closed = true;
            Reason = reason;
            ReleasedOnClose = TotalLive;
            for (int i = 0; i < _live.Length; i++) _live[i] = 0;
            return true;
        }
    }

    /// <summary>A rupture or beam lane on the floor: a flat strip from Origin along Dir.</summary>
    public struct Lane04D
    {
        public Vector3 Origin;
        /// <summary>Flat unit direction.</summary>
        public Vector3 Dir;
        public float Length;
        public float HalfWidth;

        public static Lane04D Make(Vector3 origin, Vector3 dir, float length, float halfWidth)
        {
            Vector3 f = new Vector3(dir.x, 0f, dir.z);
            float m = f.magnitude;
            Lane04D l;
            l.Origin = origin;
            l.Dir = m > 1e-5f ? f / m : Vector3.forward;
            l.Length = Mathf.Max(0f, length);
            l.HalfWidth = Mathf.Max(0.05f, halfWidth);
            return l;
        }
    }

    /// <summary>
    /// The geometry of boss telegraphs and projectiles: lanes (inside tests, the Foreman's radial and fan sets), the
    /// reachable safe spot every lane set must leave, a beam's hit test against a standing body, limited turning for
    /// shards, and the Queen's aim (it follows the player at a limited rate until it locks, then never moves). Pure.
    /// </summary>
    public static class BossLanes04D
    {
        /// <summary>Metres a player can be expected to cover during a wind-up when checking for a safe spot.</summary>
        public const float SafeReach = 4.5f;

        public static void ResetStatics() { }

        /// <summary>True when a point stands on a lane (flat test; a lane also covers HalfWidth behind its origin).</summary>
        public static bool InLane(Lane04D l, Vector3 p)
        {
            float dx = p.x - l.Origin.x, dz = p.z - l.Origin.z;
            float along = dx * l.Dir.x + dz * l.Dir.z;
            if (along < -l.HalfWidth || along > l.Length) return false;
            float side = Mathf.Abs(dx * l.Dir.z - dz * l.Dir.x);
            return side <= l.HalfWidth;
        }

        /// <summary>True when a point stands on any of the first n lanes.</summary>
        public static bool InAnyLane(Lane04D[] lanes, int n, Vector3 p)
        {
            if (lanes == null) return false;
            int c = Mathf.Min(n, lanes.Length);
            for (int i = 0; i < c; i++) if (InLane(lanes[i], p)) return true;
            return false;
        }

        /// <summary>Fills count lanes evenly spaced around origin, the first at baseDeg (0 is +Z, clockwise from above). Returns the count written.</summary>
        public static int RadialLanes(Vector3 origin, int count, float baseDeg, float length, float halfWidth, Lane04D[] into)
        {
            if (into == null) return 0;
            int n = Mathf.Clamp(count, 0, into.Length);
            for (int i = 0; i < n; i++)
            {
                float a = (baseDeg + i * 360f / Mathf.Max(1, n)) * Mathf.Deg2Rad;
                into[i] = Lane04D.Make(origin, new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)), length, halfWidth);
            }
            return n;
        }

        /// <summary>Fills count lanes fanned spreadDeg apart from origin, centred on the direction to towards. Returns the count written.</summary>
        public static int FanLanes(Vector3 origin, Vector3 towards, int count, float spreadDeg, float length, float halfWidth, Lane04D[] into)
        {
            if (into == null) return 0;
            int n = Mathf.Clamp(count, 0, into.Length);
            Vector3 f = new Vector3(towards.x - origin.x, 0f, towards.z - origin.z);
            float baseYaw = f.sqrMagnitude > 1e-6f ? Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg : 0f;
            for (int i = 0; i < n; i++)
            {
                float a = (baseYaw + (i - (n - 1) * 0.5f) * spreadDeg) * Mathf.Deg2Rad;
                into[i] = Lane04D.Make(origin, new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)), length, halfWidth);
            }
            return n;
        }

        /// <summary>
        /// Looks for a spot within reach of the player that is inside the arena, off every lane and outside the ring
        /// (ringRadius 0 for none): rings of candidates round the player, nearest first. True with the spot when one exists.
        /// </summary>
        public static bool FindSafeSpot(Lane04D[] lanes, int n, Vector3 ringCenter, float ringRadius, Vector3 player, float reach, Vector3 arenaCenter, float arenaRadius, out Vector3 spot)
        {
            const int Rings = 5, Around = 24;
            for (int r = 0; r <= Rings; r++)
            {
                float rad = reach * r / Rings;
                int steps = r == 0 ? 1 : Around;
                for (int s = 0; s < steps; s++)
                {
                    float a = s * Mathf.PI * 2f / steps;
                    Vector3 p = player + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * rad;
                    if (IsSafe(lanes, n, ringCenter, ringRadius, p, arenaCenter, arenaRadius))
                    {
                        spot = p;
                        return true;
                    }
                }
            }
            spot = player;
            return false;
        }

        /// <summary>True when a point is inside the arena, off every lane and outside the ring (with a small margin).</summary>
        public static bool IsSafe(Lane04D[] lanes, int n, Vector3 ringCenter, float ringRadius, Vector3 p, Vector3 arenaCenter, float arenaRadius)
        {
            float ax = p.x - arenaCenter.x, az = p.z - arenaCenter.z;
            if (ax * ax + az * az > arenaRadius * arenaRadius) return false;
            if (ringRadius > 0f)
            {
                float rx = p.x - ringCenter.x, rz = p.z - ringCenter.z;
                float rr = ringRadius + 0.3f;
                if (rx * rx + rz * rz <= rr * rr) return false;
            }
            return !InAnyLane(lanes, n, p);
        }

        /// <summary>
        /// True when a beam's committed line a to b passes within radius of a standing body (feet at feet, height tall):
        /// checked at the shins, the chest and the head.
        /// </summary>
        public static bool BeamHitsBody(Vector3 a, Vector3 b, float radius, Vector3 feet, float height)
        {
            float h = Mathf.Max(0.5f, height);
            return CreatureFightRules.NearShotLine(a, b, feet + Vector3.up * (h * 0.25f), radius)
                || CreatureFightRules.NearShotLine(a, b, feet + Vector3.up * (h * 0.65f), radius)
                || CreatureFightRules.NearShotLine(a, b, feet + Vector3.up * (h * 0.92f), radius);
        }

        /// <summary>
        /// Turns a direction toward another by at most maxDeg degrees (a unit vector comes back). Pure maths: Rodrigues'
        /// rotation about the axis between the two, so it works in three dimensions for flyers too.
        /// </summary>
        public static Vector3 TurnToward(Vector3 from, Vector3 to, float maxDeg)
        {
            float fm = from.magnitude, tm = to.magnitude;
            if (tm < 1e-6f) return fm > 1e-6f ? from / fm : Vector3.forward;
            Vector3 t = to / tm;
            if (fm < 1e-6f) return t;
            Vector3 f = from / fm;
            float dot = Mathf.Clamp(Vector3.Dot(f, t), -1f, 1f);
            float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;
            float step = Mathf.Max(0f, maxDeg);
            if (angle <= step || angle < 1e-3f) return t;
            Vector3 axis = Vector3.Cross(f, t);
            float am = axis.magnitude;
            if (am < 1e-6f)
            {
                axis = Mathf.Abs(f.y) < 0.9f ? Vector3.Cross(f, Vector3.up) : Vector3.Cross(f, Vector3.right);
                am = axis.magnitude;
            }
            axis /= am;
            float r = step * Mathf.Deg2Rad;
            float c = Mathf.Cos(r), s = Mathf.Sin(r);
            Vector3 v = f * c + Vector3.Cross(axis, f) * s + axis * (Vector3.Dot(axis, f) * (1f - c));
            return v.normalized;
        }

        /// <summary>
        /// One step of a guided shard: it turns toward its target at turnRate degrees per second only while it is still in
        /// its steering window and can see the target. Losing sight once ends the steering for good (lost), so a shard
        /// never snakes round cover; out of its window it flies straight.
        /// </summary>
        public static Vector3 SteerShard(Vector3 dir, Vector3 toTarget, float turnRate, float dt, bool sees, bool inWindow, ref bool lost)
        {
            if (!sees) lost = true;
            if (lost || !inWindow || dt <= 0f) return dir.sqrMagnitude > 1e-8f ? dir.normalized : Vector3.forward;
            return TurnToward(dir, toTarget, turnRate * dt);
        }
    }

    /// <summary>
    /// The Queen's beam aim: it follows the player at a limited turn rate during the acquire window, then locks. After the
    /// lock the aim never changes, however the player moves (the lane the player saw is where the beam goes).
    /// </summary>
    public sealed class BossBeamAim04D
    {
        public Vector3 Aim { get; private set; }
        public bool Locked { get; private set; }

        public BossBeamAim04D(Vector3 initial)
        {
            Aim = initial.sqrMagnitude > 1e-8f ? initial.normalized : Vector3.forward;
        }

        /// <summary>Turns toward the target direction by at most turnRate * dt degrees, unless locked.</summary>
        public void Track(Vector3 toTarget, float turnRate, float dt)
        {
            if (Locked || dt <= 0f) return;
            Aim = BossLanes04D.TurnToward(Aim, toTarget, turnRate * dt);
        }

        public void Lock()
        {
            Locked = true;
        }
    }

    /// <summary>One reset hub for every static the bosses pass adds (called from Game.ResetStatics).</summary>
    public static class BossProfileStatics
    {
        public static void ResetStatics()
        {
            BossProfiles04D.ResetStatics();
            BossTiming04D.ResetStatics();
            BossLanes04D.ResetStatics();
            CreatureBoss.ResetStatics04D();
        }
    }
}
