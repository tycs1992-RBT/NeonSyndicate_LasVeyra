using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The life of one finite encounter contract (the state names follow the 04D brief).</summary>
    public enum EncounterState
    {
        Available,
        Unavailable,
        Preparing,
        Active,
        Victory,
        Cleared,
        Retreating,
        Aborted,
        AvailableAfterRetry
    }

    /// <summary>Why an encounter ended without a victory. None of these ever grants a reward.</summary>
    public enum EncounterAbortReason
    {
        None,
        LeftArea,
        PlayerDowned,
        SessionReset,
        VeilCrossed,
        MissionStarted,
        OtherCombatOwner,
        ActorLost,
        SpawnFailed,
        ApproachTimeout,
        Disabled,
        DebugForced
    }

    /// <summary>The four authored jobs an encounter opponent can have (an overlay on an existing species and brain).</summary>
    public enum EncounterRoleKind04D
    {
        Warden,
        Refractor,
        Resonator,
        Skirmisher
    }

    /// <summary>What an encounter opponent intends right now (perception and leash decide; the role decides how).</summary>
    public enum ActorIntent04D
    {
        Guard,
        Acquire,
        Engage,
        Search,
        Return,
        Withdraw
    }

    /// <summary>
    /// Engine-free rules for the 04D finite encounters: the state machine, the spawn budget with single-use
    /// reservations, the victory predicate, abort and admission decisions, perception and leash intents, the scoped
    /// monster-combat noise rule and the per-difficulty tuning. Everything here is plain maths with EditMode tests.
    /// </summary>
    public static class EncounterRules04D
    {
        /// <summary>Authored opponents in the Resonance Breach in total (three at the start and one reinforcement).</summary>
        public const int TotalOpponents = 4;
        /// <summary>Opponents placed when the breach opens; the rest is the single advertised reinforcement batch.</summary>
        public const int InitialWave = 3;
        /// <summary>The player counts as arrived (engaged) within this distance of the origin.</summary>
        public const float EngageRadius = 100f;
        /// <summary>Leaving this far from the origin after arriving ends the encounter (no reward).</summary>
        public const float AbortRadius = 140f;
        /// <summary>Seconds of active play the player has to reach the breach after starting it.</summary>
        public const float ApproachLimit = 480f;
        /// <summary>Seconds of active play before a failed contract can be tried again.</summary>
        public const float RetrySeconds = 30f;
        /// <summary>Seconds after arrival before the reinforcement is called even if nobody has fallen yet.</summary>
        public const float ReinforceAfterSeconds = 25f;
        /// <summary>Seconds between the reinforcement announcement and its arrival (the ground marker fills meanwhile).</summary>
        public const float ReinforceDelay = 4f;
        /// <summary>Seconds a withdrawing opponent may stay in view before it is removed anyway.</summary>
        public const float WithdrawLimit = 20f;

        /// <summary>Seconds without sight before perfect pursuit stops and the last known position is searched.</summary>
        public const float LostSightSeconds = 8f;
        /// <summary>Seconds spent looking around at the last known position.</summary>
        public const float SearchLinger = 3f;
        /// <summary>Upper bound on a whole search.</summary>
        public const float SearchMax = 12f;
        /// <summary>Hard leash: no opponent goes further than this from the origin.</summary>
        public const float HardLeash = 120f;
        /// <summary>Seconds of unrecovered pursuit (no reachable sighting) before an opponent gives up and returns.</summary>
        public const float PursuitLimit = 25f;
        /// <summary>A returning opponent gives up walking home after this long and guards where it stands.</summary>
        public const float ReturnMax = 25f;
        /// <summary>A returning opponent re-engages only when both it and the player are this close to the origin.</summary>
        public const float ReengageRadius = 100f;
        /// <summary>Distance at which an engaged opponent keeps track of a target it can see.</summary>
        public const float EngagedSightRange = 60f;
        /// <summary>A target this much higher than an opponent's feet cannot be reached on foot (no invisible stairs).</summary>
        public const float MaxStepUp = 2.6f;
        /// <summary>Distance to the goal that counts as arrived.</summary>
        public const float GoalTolerance = 2.5f;

        /// <summary>Noise inside this radius of a live encounter's origin may be exempt (with provenance).</summary>
        public const float NoiseAreaRadius = 140f;
        /// <summary>A shot that lands this close to an owned opponent is part of the fight.</summary>
        public const float NoiseImpactRadius = 12f;
        /// <summary>A shot whose path passes this close to an owned opponent is part of the fight.</summary>
        public const float NoiseLineRadius = 3f;
        /// <summary>Seconds after the player last hurt an owned opponent during which noise in the area is part of the fight.</summary>
        public const float NoiseHurtSeconds = 2f;

        public static void ResetStatics() { }

        // ------------------------------------------------------------------ state machine

        /// <summary>True for the resting states from which the player may start (or replay) the contract.</summary>
        public static bool CanStart(EncounterState s)
        {
            return s == EncounterState.Available || s == EncounterState.AvailableAfterRetry || s == EncounterState.Cleared;
        }

        /// <summary>True while an encounter owns actors, leases or reservations.</summary>
        public static bool IsRunning(EncounterState s)
        {
            return s == EncounterState.Preparing || s == EncounterState.Active || s == EncounterState.Retreating || s == EncounterState.Victory;
        }

        /// <summary>
        /// The only legal transitions. Victory comes from Active alone, so retreat, abort and timeouts can never reach a
        /// reward; a session reset rebuilds the machine instead of transitioning.
        /// </summary>
        public static bool CanTransition(EncounterState from, EncounterState to)
        {
            switch (from)
            {
                case EncounterState.Available:
                case EncounterState.AvailableAfterRetry:
                case EncounterState.Cleared:
                    return to == EncounterState.Preparing || to == EncounterState.Unavailable;
                case EncounterState.Unavailable:
                    return to == EncounterState.Available || to == EncounterState.Cleared;
                case EncounterState.Preparing:
                    return to == EncounterState.Active || to == EncounterState.Retreating || to == EncounterState.Aborted;
                case EncounterState.Active:
                    return to == EncounterState.Victory || to == EncounterState.Retreating;
                case EncounterState.Retreating:
                    return to == EncounterState.Aborted;
                case EncounterState.Aborted:
                    return to == EncounterState.AvailableAfterRetry;
                case EncounterState.Victory:
                    return to == EncounterState.Cleared;
                default:
                    return false;
            }
        }

        // ------------------------------------------------------------------ difficulty

        /// <summary>Opponents alive at once: four on Balanced and Veteran, three on Story.</summary>
        public static int AliveCap(DifficultyLevel d)
        {
            return d == DifficultyLevel.Story ? 3 : 4;
        }

        /// <summary>Seconds of continuous sight before a calm opponent commits to the fight.</summary>
        public static float ReactionDelay(DifficultyLevel d)
        {
            switch (d)
            {
                case DifficultyLevel.Story: return 0.9f;
                case DifficultyLevel.Veteran: return 0.45f;
                default: return 0.6f;
            }
        }

        /// <summary>Seconds the warden stands exposed after its committed slam.</summary>
        public static float WardenRecovery(DifficultyLevel d)
        {
            switch (d)
            {
                case DifficultyLevel.Story: return 2.0f;
                case DifficultyLevel.Veteran: return 1.25f;
                default: return 1.6f;
            }
        }

        /// <summary>Wind-up the warden's slam asks for (the ability kit still applies its own minimum and difficulty scale).</summary>
        public static float WardenSlamWindup(DifficultyLevel d)
        {
            return d == DifficultyLevel.Story ? 1.4f : 1.1f;
        }

        /// <summary>Seconds the refractor visibly charges before it fires (never under 0.7 s).</summary>
        public static float RefractorCharge(DifficultyLevel d)
        {
            switch (d)
            {
                case DifficultyLevel.Story: return 1.3f;
                case DifficultyLevel.Veteran: return 0.8f;
                default: return 1.0f;
            }
        }

        /// <summary>Fraction of the charge after which the refractor's aim stops following the target (the lane is fixed).</summary>
        public const float AimLockFraction = 0.75f;

        /// <summary>Seconds between refractor shots (plus a little jitter at the caller).</summary>
        public static float RefractorCooldown(DifficultyLevel d)
        {
            switch (d)
            {
                case DifficultyLevel.Story: return 4f;
                case DifficultyLevel.Veteran: return 2.3f;
                default: return 3f;
            }
        }

        /// <summary>Damage the resonator's linked ally takes while the link holds (a multiplier).</summary>
        public static float LinkDamageTaken(DifficultyLevel d)
        {
            switch (d)
            {
                case DifficultyLevel.Story: return 0.65f;
                case DifficultyLevel.Veteran: return 0.45f;
                default: return 0.5f;
            }
        }

        /// <summary>Seconds the skirmisher visibly pauses before it lunges.</summary>
        public static float SkirmisherPause(DifficultyLevel d)
        {
            switch (d)
            {
                case DifficultyLevel.Story: return 0.9f;
                case DifficultyLevel.Veteran: return 0.55f;
                default: return 0.7f;
            }
        }

        /// <summary>Health multiplier per role (species health times this; logged by the runtime with the result).</summary>
        public static float HealthScale(EncounterRoleKind04D role, DifficultyLevel d)
        {
            float b;
            switch (role)
            {
                case EncounterRoleKind04D.Warden: b = 0.6f; break;
                case EncounterRoleKind04D.Refractor: b = 0.55f; break;
                case EncounterRoleKind04D.Resonator: b = 0.7f; break;
                default: b = 0.8f; break;
            }
            float k = d == DifficultyLevel.Story ? 0.85f : (d == DifficultyLevel.Veteran ? 1.15f : 1f);
            return b * k;
        }

        /// <summary>Damage multiplier per difficulty (creature hits on the player are capped further by CreatureBrainLogic.CapHit).</summary>
        public static float DamageScale(DifficultyLevel d)
        {
            return d == DifficultyLevel.Story ? 0.75f : (d == DifficultyLevel.Veteran ? 1.1f : 0.9f);
        }

        /// <summary>The existing species each role is an overlay on.</summary>
        public static string SpeciesFor(EncounterRoleKind04D role)
        {
            switch (role)
            {
                case EncounterRoleKind04D.Warden: return "shard_golem";
                case EncounterRoleKind04D.Refractor: return "mirror_wraith";
                default: return "hollow";
            }
        }

        /// <summary>The name on each opponent's health bar.</summary>
        public static string DisplayName(EncounterRoleKind04D role)
        {
            switch (role)
            {
                case EncounterRoleKind04D.Warden: return "Crystal Warden";
                case EncounterRoleKind04D.Refractor: return "Refractor";
                case EncounterRoleKind04D.Resonator: return "Resonator";
                default: return "Skirmisher";
            }
        }

        /// <summary>Role of opponent i in the authored order (warden, refractor, skirmisher, then the resonator reinforcement).</summary>
        public static EncounterRoleKind04D RoleAt(int index)
        {
            switch (index)
            {
                case 0: return EncounterRoleKind04D.Warden;
                case 1: return EncounterRoleKind04D.Refractor;
                case 2: return EncounterRoleKind04D.Skirmisher;
                default: return EncounterRoleKind04D.Resonator;
            }
        }

        // ------------------------------------------------------------------ the fight

        /// <summary>True when the single advertised reinforcement should be called now (the budget still has to admit it).</summary>
        public static bool ReinforcementDue(bool alreadyScheduled, bool engaged, int defeated, float engagedSeconds)
        {
            return !alreadyScheduled && engaged && (defeated >= 1 || engagedSeconds >= ReinforceAfterSeconds);
        }

        /// <summary>Facts the run checks every frame for a reason to stop.</summary>
        public struct WatchFacts
        {
            public bool PlayerDown;
            public bool VeilChanged;
            public bool MissionActive;
            public bool OtherOwner;
            public bool ActorLost;
            public bool Engaged;
            public float PlayerDistanceFromOrigin;
            public float ApproachSeconds;
        }

        /// <summary>The first reason the running encounter must end without a reward (None when it may continue).</summary>
        public static EncounterAbortReason Check(WatchFacts f)
        {
            if (f.PlayerDown) return EncounterAbortReason.PlayerDowned;
            if (f.VeilChanged) return EncounterAbortReason.VeilCrossed;
            if (f.MissionActive) return EncounterAbortReason.MissionStarted;
            if (f.OtherOwner) return EncounterAbortReason.OtherCombatOwner;
            if (f.ActorLost) return EncounterAbortReason.ActorLost;
            if (f.Engaged && f.PlayerDistanceFromOrigin > AbortRadius) return EncounterAbortReason.LeftArea;
            if (!f.Engaged && f.ApproachSeconds > ApproachLimit) return EncounterAbortReason.ApproachTimeout;
            return EncounterAbortReason.None;
        }

        /// <summary>One plain line for each abort reason (HUD and contract board).</summary>
        public static string AbortLine(EncounterAbortReason r)
        {
            switch (r)
            {
                case EncounterAbortReason.LeftArea: return "you left the breach";
                case EncounterAbortReason.PlayerDowned: return "you were downed";
                case EncounterAbortReason.SessionReset: return "the session ended";
                case EncounterAbortReason.VeilCrossed: return "you crossed the Veil";
                case EncounterAbortReason.MissionStarted: return "a mission took over";
                case EncounterAbortReason.OtherCombatOwner: return "another fight took over the area";
                case EncounterAbortReason.ActorLost: return "an opponent was lost (removed, not defeated)";
                case EncounterAbortReason.SpawnFailed: return "the opponents could not appear";
                case EncounterAbortReason.ApproachTimeout: return "the breach closed before you arrived";
                case EncounterAbortReason.Disabled: return "the encounter system stopped";
                case EncounterAbortReason.DebugForced: return "it was cancelled from the debug menu";
                default: return "it ended";
            }
        }

        /// <summary>Facts the contract board needs before it may start.</summary>
        public struct AdmissionFacts
        {
            public bool Playing;
            public bool PlayerDown;
            public bool RunActive;
            public float RetryWait;
            public bool MissionActive;
            public int WantedStars;
            public bool BossFight;
            public bool RaidActive;
            public bool CaptureActive;
            public bool DepotActive;
            public bool EventNear;
            public bool InVeil;
            public bool QaBusy;
            public bool SiteValid;
            public string SiteReason;
        }

        /// <summary>A one-line reason the contract may not start now (null when it may).</summary>
        public static string Refusal(AdmissionFacts f)
        {
            if (!f.Playing || f.QaBusy) return "Only available during free play.";
            if (f.PlayerDown) return "Get back on your feet first.";
            if (f.RunActive) return "The breach is already open.";
            if (f.RetryWait > 0f) return "The breach is settling. Try again in " + Mathf.CeilToInt(f.RetryWait) + " s.";
            if (f.MissionActive) return "Finish or leave your current mission first.";
            if (f.WantedStars > 0) return "Lose the police first: a contract cannot start while you are wanted.";
            if (f.BossFight) return "A boss fight is under way.";
            if (f.RaidActive) return "Finish or cancel the active fort raid first.";
            if (f.CaptureActive) return "A relay capture or territory activity is under way.";
            if (f.DepotActive) return "The depot operation is still running.";
            if (f.EventNear) return "A world event is happening nearby. Wait for it to end.";
            if (f.InVeil) return "Step back out of the Veil first.";
            if (!f.SiteValid) return string.IsNullOrEmpty(f.SiteReason) ? "No safe open ground for the breach right now." : f.SiteReason;
            return null;
        }

        // ------------------------------------------------------------------ perception and leash

        /// <summary>A target this far above an opponent's feet is out of reach on foot.</summary>
        public static bool Reachable(float targetFeetY, float actorFeetY)
        {
            return targetFeetY - actorFeetY <= MaxStepUp;
        }

        /// <summary>
        /// Seconds of unrecovered pursuit: grows while an engaged or searching opponent cannot see a reachable target,
        /// and resets on a reachable sighting or outside the pursuit intents.
        /// </summary>
        public static float AccumulatePursuit(float pursuit, ActorIntent04D intent, bool sees, bool reachable, float dt)
        {
            if (intent != ActorIntent04D.Engage && intent != ActorIntent04D.Search) return 0f;
            if (sees && reachable) return 0f;
            return pursuit + Mathf.Max(0f, dt);
        }

        /// <summary>What one opponent knows this think, for NextIntent.</summary>
        public struct ActorFacts
        {
            public ActorIntent04D Intent;
            public float IntentSeconds;
            /// <summary>Line of sight within range and (unless already engaged) inside the view cone.</summary>
            public bool Sees;
            /// <summary>The target stands where this body can walk (EncounterRules04D.Reachable).</summary>
            public bool TargetReachable;
            /// <summary>A noise or a hit gave a new last known position this think.</summary>
            public bool Heard;
            public bool HasLastKnown;
            public float SinceSeen;
            public float Pursuit;
            public float DistanceFromOrigin;
            public float TargetDistanceFromOrigin;
            public bool AtGoal;
            public float GoalSeconds;
            public float ReactionDelay;
            public bool Withdrawing;
        }

        /// <summary>True when the hard leash or the pursuit limit says this opponent must go home.</summary>
        public static bool LeashBroken(float distanceFromOrigin, float pursuit)
        {
            return distanceFromOrigin > HardLeash || pursuit >= PursuitLimit;
        }

        /// <summary>
        /// The bounded perception and leash state machine: guard until a real sighting, a reaction delay before
        /// committing, perfect pursuit for at most LostSightSeconds without sight, then a search of the last known
        /// position, then a visible return home; the hard leash and the pursuit limit always win.
        /// </summary>
        public static ActorIntent04D NextIntent(ActorFacts f)
        {
            if (f.Withdrawing) return ActorIntent04D.Withdraw;
            switch (f.Intent)
            {
                case ActorIntent04D.Guard:
                    if (f.Sees) return ActorIntent04D.Acquire;
                    if (f.Heard && f.HasLastKnown) return ActorIntent04D.Search;
                    return ActorIntent04D.Guard;
                case ActorIntent04D.Acquire:
                    if (!f.Sees) return f.HasLastKnown ? ActorIntent04D.Search : ActorIntent04D.Guard;
                    return f.IntentSeconds >= f.ReactionDelay ? ActorIntent04D.Engage : ActorIntent04D.Acquire;
                case ActorIntent04D.Engage:
                    if (LeashBroken(f.DistanceFromOrigin, f.Pursuit)) return ActorIntent04D.Return;
                    if (!f.Sees && f.SinceSeen >= LostSightSeconds) return f.HasLastKnown ? ActorIntent04D.Search : ActorIntent04D.Return;
                    return ActorIntent04D.Engage;
                case ActorIntent04D.Search:
                    if (LeashBroken(f.DistanceFromOrigin, f.Pursuit)) return ActorIntent04D.Return;
                    if (f.Sees) return ActorIntent04D.Acquire;
                    if ((f.AtGoal && f.GoalSeconds >= SearchLinger) || f.IntentSeconds >= SearchMax) return ActorIntent04D.Return;
                    return ActorIntent04D.Search;
                case ActorIntent04D.Return:
                    if (f.AtGoal || f.IntentSeconds >= ReturnMax) return ActorIntent04D.Guard;
                    // A visible return: no re-engaging an unreachable target, and hysteresis against the leash edge.
                    if (f.Sees && f.TargetReachable && f.TargetDistanceFromOrigin <= ReengageRadius && f.DistanceFromOrigin <= ReengageRadius) return ActorIntent04D.Acquire;
                    return ActorIntent04D.Return;
                default:
                    return ActorIntent04D.Withdraw;
            }
        }

        /// <summary>Seconds after a light-hit flinch during which an encounter creature shrugs off the next one.</summary>
        public const float FlinchResist = 1.2f;

        /// <summary>Brief diminishing-return resistance: one light-hit flinch per FlinchResist seconds (no beam chain-stun).</summary>
        public static bool FlinchAllowed(float now, float lastFlinch)
        {
            return now - lastFlinch >= FlinchResist;
        }

        /// <summary>Recent damage as a fraction of max health; it halves every halving seconds (the refractor's pressure).</summary>
        public static float DecayPressure(float pressure, float dt, float halving)
        {
            if (halving <= 0f) return 0f;
            return pressure * Mathf.Pow(0.5f, Mathf.Max(0f, dt) / halving);
        }

        // ------------------------------------------------------------------ scoped monster-combat noise

        /// <summary>Distance from p to the segment a-b (flat and vertical together).</summary>
        public static float SegmentDistance(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 1e-6f) return Vector3.Distance(a, p);
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2);
            return Vector3.Distance(a + ab * t, p);
        }

        /// <summary>
        /// Gunfire, blast or fire noise that belongs to this contract's monster fight: the encounter is live, the noise
        /// is inside its area, and it has provenance (the shot landed on or passed by an owned opponent, or the player
        /// just hurt one). Everything else stays a crime as before.
        /// </summary>
        public static bool NoiseExempt(bool encounterLive, float noiseDistanceFromOrigin, float nearestOwnedToImpact, float nearestOwnedToLine, float secondsSinceOwnedHurt)
        {
            if (!encounterLive || noiseDistanceFromOrigin > NoiseAreaRadius) return false;
            return nearestOwnedToImpact <= NoiseImpactRadius || nearestOwnedToLine <= NoiseLineRadius || secondsSinceOwnedHurt <= NoiseHurtSeconds;
        }

        /// <summary>
        /// The whole crime decision for the scoped rule: a crime with a victim is exempt only when the victim is one of
        /// the contract's own creatures; a noise crime without a victim follows NoiseExempt; anything else stays a crime.
        /// </summary>
        public static bool CrimeExempt(bool noiseCrime, bool hasVictim, bool victimOwned, bool noiseExempt)
        {
            if (hasVictim) return victimOwned;
            return noiseCrime && noiseExempt;
        }
    }

    /// <summary>
    /// The spawn budget of one encounter: a total number of authored opponents, a cap on how many are alive, and
    /// single-use reservations. A delayed spawn reserves its slot before it is scheduled; Admit consumes the
    /// reservation exactly once, so two callbacks can never admit the same spawn twice. Deaths and removals are
    /// counted separately: a removal is never a defeat.
    /// </summary>
    public sealed class EncounterBudget
    {
        private const int Pending = 0;
        private const int Admitted = 1;
        private const int Cancelled = 2;
        private const int Failed = 3;

        private readonly List<int> _tokens = new List<int>();

        public readonly int Total;
        public readonly int AliveCap;
        public int Reserved { get; private set; }
        public int Spawned { get; private set; }
        public int Defeated { get; private set; }
        public int Removed { get; private set; }
        public int FailedSpawns { get; private set; }

        public EncounterBudget(int total, int aliveCap)
        {
            Total = Mathf.Max(0, total);
            AliveCap = Mathf.Max(1, aliveCap);
        }

        public int Alive { get { return Spawned - Defeated - Removed; } }
        /// <summary>Authored opponents not yet spawned, reserved or failed.</summary>
        public int Remaining { get { return Total - Spawned - Reserved - FailedSpawns; } }
        public bool CanReserve { get { return Remaining > 0 && Alive + Reserved < AliveCap; } }

        /// <summary>Reserves one opponent's slot. Returns its token (1 or more), or 0 when the budget refuses.</summary>
        public int Reserve()
        {
            if (!CanReserve) return 0;
            _tokens.Add(Pending);
            Reserved++;
            return _tokens.Count;
        }

        public bool IsPending(int token)
        {
            return token >= 1 && token <= _tokens.Count && _tokens[token - 1] == Pending;
        }

        /// <summary>Consumes a reservation right before its body is created. True once; every later call is refused.</summary>
        public bool Admit(int token)
        {
            if (!IsPending(token)) return false;
            _tokens[token - 1] = Admitted;
            Reserved--;
            Spawned++;
            return true;
        }

        /// <summary>The admitted body could not be created: the slot is spent and counted as a failure.</summary>
        public bool AdmitFailed(int token)
        {
            if (token < 1 || token > _tokens.Count || _tokens[token - 1] != Admitted) return false;
            _tokens[token - 1] = Failed;
            Spawned--;
            FailedSpawns++;
            return true;
        }

        /// <summary>Releases a reservation that will not be used. True once.</summary>
        public bool Cancel(int token)
        {
            if (!IsPending(token)) return false;
            _tokens[token - 1] = Cancelled;
            Reserved--;
            return true;
        }

        /// <summary>Releases every pending reservation (victory, abort). Returns how many were released.</summary>
        public int CancelAll()
        {
            int n = 0;
            for (int i = 0; i < _tokens.Count; i++)
            {
                if (_tokens[i] != Pending) continue;
                _tokens[i] = Cancelled;
                Reserved--;
                n++;
            }
            return n;
        }

        /// <summary>One living opponent died (health reached zero). The caller guards each opponent to count it once.</summary>
        public bool MarkDefeated()
        {
            if (Alive <= 0) return false;
            Defeated++;
            return true;
        }

        /// <summary>One living opponent vanished without dying (streaming, cleanup, a lost body). Never a defeat.</summary>
        public bool MarkRemoved()
        {
            if (Alive <= 0) return false;
            Removed++;
            return true;
        }

        /// <summary>
        /// The victory predicate: every authored opponent was spawned and died, nothing is reserved, nothing went missing
        /// and no spawn failed. A timeout, a removal or a retreat can never make this true.
        /// </summary>
        public bool VictoryReady
        {
            get { return Total > 0 && Defeated == Total && Removed == 0 && Reserved == 0 && FailedSpawns == 0 && Alive == 0; }
        }

        public override string ToString()
        {
            return "total " + Total + ", cap " + AliveCap + ", spawned " + Spawned + ", alive " + Alive + ", defeated " + Defeated
                + ", removed " + Removed + ", reserved " + Reserved + (FailedSpawns > 0 ? ", failed " + FailedSpawns : "");
        }
    }

    /// <summary>
    /// Engine-free site rules for the Resonance Breach: open ground 160 to 320 m from the Veyra Lofts arrival, outside
    /// every starter safety zone, clear of buildings, roads, doors, the canal and the special sites, scored toward a
    /// comfortable distance. The runtime then checks the ground and the colliders (CrystalEncounters.Site.cs).
    /// </summary>
    public static class EncounterSiteRules04D
    {
        public const float MinDistance = 160f;
        public const float MaxDistance = 320f;
        public const float PreferredDistance = 220f;
        /// <summary>Open radius around the centre (no building or road), enough for the anchors.</summary>
        public const float Clearance = 12f;
        /// <summary>No lot entrance (a door) this close to the centre beyond the clearance.</summary>
        public const float DoorMargin = 6f;
        /// <summary>Extra distance kept from each starter safety zone edge.</summary>
        public const float StarterMargin = 15f;
        /// <summary>Kept from the edge of every special site (arenas, the manor, landmarks).</summary>
        public const float SiteMargin = 60f;
        /// <summary>Grid step of the candidate search.</summary>
        public const float Step = 4f;
        /// <summary>Alternates are at least this far apart.</summary>
        public const float CandidateSpacing = 30f;
        public const int MaxCandidates = 6;

        public static void ResetStatics() { }

        public static bool InBand(float distance)
        {
            return distance >= MinDistance && distance <= MaxDistance;
        }

        /// <summary>The arrival point beside the Lofts door (GameRoot.SpawnOutside without the ground ray).</summary>
        public static bool Arrival(CityLayout layout, out Vector3 arrival)
        {
            arrival = Vector3.zero;
            if (layout == null) return false;
            Poi home = layout.FindPoi("Veyra Lofts");
            if (home == null) return false;
            Vector3 f = home.Facing.sqrMagnitude > 0.01f ? home.Facing.normalized : Vector3.forward;
            arrival = home.Position + f * 3.5f;
            return true;
        }

        /// <summary>True inside any starter safety zone (Lofts, clinic, manor, promenade) widened by StarterMargin.</summary>
        public static bool InStarterZone(CityLayout layout, Vector3 p)
        {
            float m = StarterMargin;
            if (p.x >= 22f - m && p.x <= 238f + m && p.z >= -85f - m && p.z <= 85f + m) return true;
            if (layout == null) return false;
            Poi lofts = layout.FindPoi("Veyra Lofts");
            if (lofts != null && StarterSafetyRules.InRadius(p, lofts.Position, StarterSafetyRules.LoftsRadius + m)) return true;
            Poi clinic = layout.FindPoi("Mara Solane Street Clinic");
            if (clinic != null && StarterSafetyRules.InRadius(p, clinic.Position, StarterSafetyRules.ClinicRadius + m)) return true;
            SpecialSite manor = layout.Site("CrystalManor");
            if (manor != null && StarterSafetyRules.InRadius(p, manor.Position, StarterSafetyRules.ManorRadius + m)) return true;
            return false;
        }

        /// <summary>Layout-only test of one point with an open radius: why it fails, or null when it passes.</summary>
        public static string LayoutProblem(CityLayout layout, Vector3 arrival, Vector3 p, float clearance)
        {
            if (layout == null) return "no city";
            if (Mathf.Abs(p.x) > CityLayout.MapHalf - 40f || Mathf.Abs(p.z) > CityLayout.MapHalf - 40f) return "outside the map";
            if (!InBand(MathUtil.FlatDistance(p, arrival))) return "out of the distance band";
            if (InStarterZone(layout, p)) return "inside a starter safety zone";
            if (Mathf.Abs(p.z - CityLayout.CanalZ) < CityLayout.CanalBandHalf + clearance) return "beside the canal";
            if (layout.IsInsideBuilding(p, clearance)) return "too close to a building";
            if (layout.NearAnyRoad(p, clearance)) return "too close to a road";
            District d = CityLayout.DistrictAt(p);
            if (CityLayout.IsWilderness(d) && layout.InsideRidge(p, clearance)) return "inside a ridge";
            for (int i = 0; i < layout.Sites.Count; i++)
            {
                SpecialSite s = layout.Sites[i];
                if (MathUtil.FlatDistance(s.Position, p) < s.Radius + SiteMargin) return "too close to " + s.Name;
            }
            float door = clearance + DoorMargin;
            foreach (Block b in layout.Blocks)
            {
                if (b.Curb.Inset(-door - 2f).Contains(p) == false) continue;
                foreach (Lot lot in b.Lots)
                {
                    if (lot.Entrance == Vector3.zero || lot.Buildings.Count == 0) continue;
                    if (MathUtil.FlatDistance(lot.Entrance, p) < door) return "too close to a door";
                }
            }
            for (int i = 0; i < layout.Pois.Count; i++)
            {
                if (MathUtil.FlatDistance(layout.Pois[i].Position, p) < door + 4f) return "too close to " + layout.Pois[i].Name;
            }
            return null;
        }

        /// <summary>Higher is better: near the preferred distance, more open ground breaks ties.</summary>
        public static float Score(float distance, bool extraOpen)
        {
            return -Mathf.Abs(distance - PreferredDistance) + (extraOpen ? 25f : 0f);
        }

        /// <summary>
        /// Candidate centres best-first (deterministic for a city): a grid over the band that passes LayoutProblem,
        /// scored, and thinned so alternates are CandidateSpacing apart. Empty when the city has no suitable ground.
        /// </summary>
        public static void Candidates(CityLayout layout, Vector3 arrival, List<Vector3> into)
        {
            into.Clear();
            if (layout == null) return;
            List<KeyValuePair<float, Vector3>> scored = new List<KeyValuePair<float, Vector3>>();
            float r = MaxDistance;
            for (float x = arrival.x - r; x <= arrival.x + r; x += Step)
            {
                for (float z = arrival.z - r; z <= arrival.z + r; z += Step)
                {
                    Vector3 p = new Vector3(Mathf.Round(x), 0f, Mathf.Round(z));
                    float d = MathUtil.FlatDistance(p, arrival);
                    if (!InBand(d)) continue;
                    // Cheap rejections first: most of the core is buildings and roads.
                    if (layout.IsInsideBuilding(p, Clearance)) continue;
                    if (layout.RoadAt(p, 0.5f) != null) continue;
                    if (LayoutProblem(layout, arrival, p, Clearance) != null) continue;
                    bool extra = !layout.IsInsideBuilding(p, Clearance + 3f) && !layout.NearAnyRoad(p, Clearance + 3f);
                    scored.Add(new KeyValuePair<float, Vector3>(Score(d, extra), p));
                }
            }
            scored.Sort((a, b) =>
            {
                int c = b.Key.CompareTo(a.Key);
                if (c != 0) return c;
                c = a.Value.x.CompareTo(b.Value.x);
                return c != 0 ? c : a.Value.z.CompareTo(b.Value.z);
            });
            for (int i = 0; i < scored.Count && into.Count < MaxCandidates; i++)
            {
                Vector3 p = scored[i].Value;
                bool near = false;
                for (int k = 0; k < into.Count; k++) if (MathUtil.FlatDistance(into[k], p) < CandidateSpacing) { near = true; break; }
                if (!near) into.Add(p);
            }
        }

        /// <summary>
        /// The authored layout around a centre facing the approach: [0] warden in front, [1] refractor at the back,
        /// [2] skirmisher on a flank, [3] and [4] the reinforcement arrival points (the runtime picks the one farther
        /// from the player). Pure geometry; the runtime validates and nudges each anchor.
        /// </summary>
        public static void Anchors(Vector3 center, Vector3 approach, List<Vector3> into)
        {
            into.Clear();
            Vector3 a = new Vector3(approach.x, 0f, approach.z);
            if (a.sqrMagnitude < 1e-4f) a = Vector3.forward;
            a.Normalize();
            Vector3 right = new Vector3(a.z, 0f, -a.x);
            into.Add(center + a * 5f);
            into.Add(center - a * 9f + right * 3f);
            into.Add(center + right * 8f - a * 1f);
            into.Add(center - a * 11f - right * 6f);
            into.Add(center - right * 10f + a * 2f);
        }

        private static readonly float[] NudgeAngles = { 25f, -25f, 50f, -50f, 0f, 75f, -75f };
        private static readonly float[] NudgeScales = { 1f, 1f, 1f, 1f, 0.7f, 0.8f, 0.8f };

        /// <summary>Number of alternates Nudge offers after the authored spot.</summary>
        public static int NudgeCount { get { return NudgeAngles.Length; } }

        /// <summary>Alternate offsets tried when an anchor is blocked: rotations around the centre and a pull inward.</summary>
        public static Vector3 Nudge(Vector3 center, Vector3 anchor, int attempt)
        {
            Vector3 off = anchor - center;
            off.y = 0f;
            if (attempt <= 0) return anchor;
            int i = (attempt - 1) % NudgeAngles.Length;
            Vector3 rotated = QMath.AY(NudgeAngles[i]) * off * NudgeScales[i];
            return center + rotated;
        }
    }
}
