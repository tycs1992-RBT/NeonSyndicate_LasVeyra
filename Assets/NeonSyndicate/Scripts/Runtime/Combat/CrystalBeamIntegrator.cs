using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Prism Thread beam's time integrator (engine-free, tested offline). Damage, heat and element procs happen only
    /// on a fixed grid of ticks (CrystalFocusDb.BeamTick), never once per rendered frame, so 30, 60 and 120 frames per
    /// second deal the same total. A frame longer than MaxStep integrates only MaxStep and the excess is discarded and
    /// counted (no catch-up burst after a hitch, a menu or lost focus). The partial tick carries over between frames and
    /// between presses, so tapping the trigger can never mint extra ticks. The concentration ramp grows only with
    /// maintained aimed contact on one target and resets the moment contact breaks.
    /// </summary>
    public sealed class CrystalBeamIntegrator
    {
        public readonly float TickSeconds;
        public readonly float MaxStep;
        public readonly float RampSeconds;
        public readonly float RampMax;
        /// <summary>Seconds of contact per element proc roll.</summary>
        public readonly float ProcInterval;

        /// <summary>Floating point slack so ten frames of 0.01 s make exactly one tick.</summary>
        public const float Epsilon = 1e-4f;

        private float _acc;
        private int _rampTarget;
        private float _ramp;
        private float _procAcc;

        // Totals for the debug page and the tests.
        public int Ticks { get; private set; }
        public int ContactTicks { get; private set; }
        public float SimulatedSeconds { get; private set; }
        public float DiscardedSeconds { get; private set; }
        public float TotalDamage { get; private set; }
        public float TotalHeat { get; private set; }
        public int ProcRolls { get; private set; }

        public CrystalBeamIntegrator()
            : this(CrystalFocusDb.BeamTick, CrystalFocusDb.BeamMaxStep, CrystalFocusDb.BeamRampSeconds, CrystalFocusDb.BeamRampMax, 0.5f)
        {
        }

        public CrystalBeamIntegrator(float tickSeconds, float maxStep, float rampSeconds, float rampMax, float procInterval)
        {
            TickSeconds = Mathf.Max(0.01f, tickSeconds);
            MaxStep = Mathf.Max(TickSeconds, maxStep);
            RampSeconds = Mathf.Max(0.01f, rampSeconds);
            RampMax = Mathf.Max(1f, rampMax);
            ProcInterval = Mathf.Max(TickSeconds, procInterval);
        }

        /// <summary>The time already accumulated toward the next tick (0 to TickSeconds).</summary>
        public float Pending { get { return _acc; } }

        /// <summary>The ramp's current progress 0 to 1 (1 is the full RampMax multiplier).</summary>
        public float RampProgress { get { return Mathf.Clamp01(_ramp / RampSeconds); } }

        /// <summary>
        /// Advances by one simulated frame of the held beam and returns how many whole ticks are due. Time beyond
        /// MaxStep in one frame is discarded (and added to DiscardedSeconds); zero, negative or bad frame times do nothing.
        /// </summary>
        public int Advance(float dt)
        {
            if (!(dt > 0f)) return 0;
            float use = Mathf.Min(dt, MaxStep);
            DiscardedSeconds += dt - use;
            SimulatedSeconds += use;
            _acc += use;
            int n = 0;
            while (_acc >= TickSeconds - Epsilon)
            {
                _acc -= TickSeconds;
                n++;
            }
            // A tick taken within Epsilon of the boundary leaves a tiny negative remainder, so total time is conserved.
            return n;
        }

        /// <summary>
        /// The damage multiplier for one tick of contact with targetId (0 means no contact this tick). The ramp grows by
        /// one tick while aimed contact stays on the same target, and resets on no contact, a new target or hip fire.
        /// </summary>
        public float TickRamp(int targetId, bool aimed)
        {
            if (targetId == 0 || !aimed)
            {
                _rampTarget = targetId;
                _ramp = 0f;
                return 1f;
            }
            if (targetId != _rampTarget)
            {
                _rampTarget = targetId;
                _ramp = 0f;
            }
            float mul = 1f + (RampMax - 1f) * Mathf.Clamp01(_ramp / RampSeconds);
            _ramp = Mathf.Min(RampSeconds, _ramp + TickSeconds);
            return mul;
        }

        /// <summary>True when this tick of contact is due its one element proc roll (one per ProcInterval of contact).</summary>
        public bool TickProc(bool contact)
        {
            if (!contact) return false;
            _procAcc += TickSeconds;
            if (_procAcc < ProcInterval - Epsilon) return false;
            _procAcc -= ProcInterval;
            ProcRolls++;
            return true;
        }

        /// <summary>Records one tick's result in the totals.</summary>
        public void Record(float damage, float heat, bool contact)
        {
            Ticks++;
            if (contact) ContactTicks++;
            TotalDamage += Mathf.Max(0f, damage);
            TotalHeat += Mathf.Max(0f, heat);
        }

        /// <summary>Contact broke (occluded, target gone, beam stopped): the ramp resets.</summary>
        public void BreakContact()
        {
            _rampTarget = 0;
            _ramp = 0f;
        }

        /// <summary>Clears the debug totals (not the pending partial tick).</summary>
        public void ResetTotals()
        {
            Ticks = 0;
            ContactTicks = 0;
            SimulatedSeconds = 0f;
            DiscardedSeconds = 0f;
            TotalDamage = 0f;
            TotalHeat = 0f;
            ProcRolls = 0;
        }

        /// <summary>Forgets everything including the partial tick and the proc accumulator (new game, load).</summary>
        public void ResetAll()
        {
            ResetTotals();
            _acc = 0f;
            _procAcc = 0f;
            BreakContact();
        }
    }
}
