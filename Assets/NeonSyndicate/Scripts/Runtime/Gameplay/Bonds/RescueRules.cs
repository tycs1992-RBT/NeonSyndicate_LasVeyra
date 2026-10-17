using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Where a rescue stands.</summary>
    public enum RescuePhase
    {
        /// <summary>Extraction clamps still hold it; each one released makes it flare.</summary>
        Clamped = 0,
        /// <summary>Free but wary: walk in calmly (running at it makes it flare) until it settles.</summary>
        Approach = 1,
        /// <summary>The tuning sequence: press interact as the ring closes, three times.</summary>
        Tuning = 2,
        /// <summary>Waiting for the player to name it (or keep the default).</summary>
        Naming = 3,
        Done = 4,
        Aborted = 5
    }

    /// <summary>
    /// The rescue of a trapped creature as a small state machine: release the extraction clamps (each release makes it
    /// flare a warning; while it warns the player must keep their distance or the warning does not end), approach
    /// calmly until it settles, then a short tuning sequence (three timed presses; a miss only repeats the beat), then
    /// naming. Attacking it never hurts it and never fails the rescue: the approach resets (calm and tuning progress
    /// go back to zero, released clamps stay released) and it panics for a while. Engine-free.
    /// </summary>
    public sealed class RescueRules
    {
        /// <summary>Inside this distance during a warning flare, the warning does not count down.</summary>
        public const float WarnRadius = 5f;
        /// <summary>The distance the HUD asks the player to keep while it warns (the ring drawn on the ground).</summary>
        public const float KeepDistance = 6.5f;
        public const float FlareSeconds = 3f;
        /// <summary>An attack makes it panic for this long (and resets the approach).</summary>
        public const float PanicSeconds = 6f;
        /// <summary>Close enough to start tuning once it is calm.</summary>
        public const float TuneRadius = 2.8f;
        /// <summary>Within this distance, moving calmly, it settles toward the player.</summary>
        public const float CalmRadius = 9f;
        public const float CalmPerSecond = 0.4f;
        /// <summary>The tuning ring closes over this many seconds.</summary>
        public const float BeatPeriod = 1.5f;
        /// <summary>A press counts while the ring is within this share of closed.</summary>
        public const float BeatWindow = 0.2f;
        public const int BeatsNeeded = 3;

        public readonly bool[] ClampOpen;
        public RescuePhase Phase { get; private set; }
        public float FlareLeft { get; private set; }
        public float Calm { get; private set; }
        public int Beats { get; private set; }
        public float BeatClock { get; private set; }
        public int Resets { get; private set; }
        public int Misses { get; private set; }
        /// <summary>True when the player stood inside the warning radius this tick while it warned.</summary>
        public bool TooClose { get; private set; }

        public RescueRules(int clamps)
        {
            ClampOpen = new bool[Mathf.Clamp(clamps, 0, 8)];
            Phase = ClampOpen.Length == 0 ? RescuePhase.Approach : RescuePhase.Clamped;
        }

        public int ClampsTotal { get { return ClampOpen.Length; } }

        public int ClampsLeft
        {
            get
            {
                int n = 0;
                for (int i = 0; i < ClampOpen.Length; i++) if (!ClampOpen[i]) n++;
                return n;
            }
        }

        public bool Flaring { get { return FlareLeft > 0f; } }

        /// <summary>A clamp can be worked on now (still clamped, and not while it warns).</summary>
        public bool CanWorkClamp { get { return Phase == RescuePhase.Clamped && !Flaring; } }

        /// <summary>Releases a clamp. It flares; the last one frees it into the calm approach. False when not allowed.</summary>
        public bool OpenClamp(int index)
        {
            if (!CanWorkClamp || index < 0 || index >= ClampOpen.Length || ClampOpen[index]) return false;
            ClampOpen[index] = true;
            FlareLeft = FlareSeconds;
            if (ClampsLeft == 0)
            {
                Phase = RescuePhase.Approach;
                Calm = 0f;
            }
            return true;
        }

        /// <summary>It was attacked: it panics and the approach starts over (nothing permanent is lost).</summary>
        public void Attacked()
        {
            if (Phase == RescuePhase.Naming || Phase == RescuePhase.Done || Phase == RescuePhase.Aborted) return;
            Resets++;
            FlareLeft = Mathf.Max(FlareLeft, PanicSeconds);
            Calm = 0f;
            Beats = 0;
            BeatClock = 0f;
            if (Phase == RescuePhase.Tuning) Phase = RescuePhase.Approach;
        }

        /// <summary>One step. playerDistance is flat metres to the creature; rushing means running or sprinting at it.</summary>
        public void Tick(float dt, float playerDistance, bool rushing)
        {
            if (dt <= 0f || Phase == RescuePhase.Naming || Phase == RescuePhase.Done || Phase == RescuePhase.Aborted) return;
            TooClose = Flaring && playerDistance < WarnRadius;
            if (Flaring && !TooClose) FlareLeft = Mathf.Max(0f, FlareLeft - dt);
            switch (Phase)
            {
                case RescuePhase.Approach:
                    if (rushing && playerDistance < WarnRadius && !Flaring)
                    {
                        FlareLeft = FlareSeconds;
                        Calm *= 0.5f;
                        break;
                    }
                    if (!Flaring && !rushing && playerDistance <= CalmRadius) Calm = Mathf.Min(1f, Calm + CalmPerSecond * dt);
                    if (!Flaring && Calm >= 1f && playerDistance <= TuneRadius)
                    {
                        Phase = RescuePhase.Tuning;
                        Beats = 0;
                        BeatClock = 0f;
                    }
                    break;
                case RescuePhase.Tuning:
                    if (playerDistance > TuneRadius + 1.5f)
                    {
                        Phase = RescuePhase.Approach;
                        Beats = 0;
                        BeatClock = 0f;
                        break;
                    }
                    BeatClock += dt;
                    if (BeatClock >= BeatPeriod) BeatClock -= BeatPeriod;
                    break;
            }
        }

        /// <summary>The tuning ring: 1 wide open, 0 closed on the creature.</summary>
        public float Ring { get { return 1f - Mathf.Clamp01(BeatClock / BeatPeriod); } }

        /// <summary>A tuning press: true on a hit. A miss only restarts the beat. The last hit moves on to naming.</summary>
        public bool PressTune()
        {
            if (Phase != RescuePhase.Tuning) return false;
            bool hit = Ring <= BeatWindow;
            BeatClock = 0f;
            if (!hit)
            {
                Misses++;
                return false;
            }
            Beats++;
            if (Beats >= BeatsNeeded) Phase = RescuePhase.Naming;
            return true;
        }

        public void Named()
        {
            if (Phase == RescuePhase.Naming) Phase = RescuePhase.Done;
        }

        public void Abort()
        {
            if (Phase != RescuePhase.Done) Phase = RescuePhase.Aborted;
        }

        /// <summary>0 to 1 through the rescue.</summary>
        public float Progress
        {
            get
            {
                switch (Phase)
                {
                    case RescuePhase.Clamped: return ClampsTotal > 0 ? 0.5f * (ClampsTotal - ClampsLeft) / ClampsTotal : 0f;
                    case RescuePhase.Approach: return 0.5f + 0.25f * Calm;
                    case RescuePhase.Tuning: return 0.75f + 0.2f * Beats / BeatsNeeded;
                    case RescuePhase.Naming: return 0.95f;
                    case RescuePhase.Done: return 1f;
                    default: return 0f;
                }
            }
        }

        /// <summary>The objective line for the quest tracker (title is the creature kind, like "quartz drakeling").</summary>
        public string Objective(string title)
        {
            string t = string.IsNullOrEmpty(title) ? "creature" : title;
            if (Flaring && Phase != RescuePhase.Done)
                return TooClose ? "Back away: the " + t + " is warning you. Keep about " + Mathf.RoundToInt(KeepDistance) + " m until it settles." : "It is warning you. Keep your distance until it settles.";
            switch (Phase)
            {
                case RescuePhase.Clamped:
                    return "Release the extraction clamps (" + ClampsLeft + " left). Do not attack the " + t + ".";
                case RescuePhase.Approach:
                    return Calm < 1f ? "Walk in calmly. The " + t + " is settling (" + Mathf.RoundToInt(Calm * 100f) + "%)." : "It is calm. Step close to tune its resonance.";
                case RescuePhase.Tuning:
                    return "Tune its resonance: press interact as the ring closes (" + Beats + " of " + BeatsNeeded + ").";
                case RescuePhase.Naming:
                    return "Give the " + t + " a name, or keep the one it answers to.";
                case RescuePhase.Done:
                    return "Bonded.";
                default:
                    return "Rescue cancelled.";
            }
        }
    }
}
