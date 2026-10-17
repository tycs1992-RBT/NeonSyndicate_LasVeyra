using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// UnderBossFight, 04E: the Phase 6 Resonance loop on the boss. Marks sit on the fight's lock-on stand-in
    /// (UBossLockProxy04E), so a marked body or lock point is detonated by the player's next gun round like any marked
    /// foe: FilterDamage multiplies that hit by the boss burst multiplier (x1.75, after the part, window and element
    /// scaling) and remembers the burst; the body's Damaged hook then hands the landed hit to ResonanceSystem, which
    /// consumes the mark, plays the burst (splash on the adds only, magazine refill, BurstFired), refills crystal energy
    /// within its per-second cap and lets a companion's Glass Lance or a striking power leave its mark. The body's
    /// CharacterHealth has no character owner, so it never applies a burst multiplier of its own (info.Resonance is never
    /// set here for that reason). A mark never stuns, staggers, burns or throws a boss.
    /// </summary>
    public abstract partial class UnderBossFight
    {
        private ResonanceState _pendingBurst;

        /// <summary>The burst multiplier for a hit the filter is scaling (1 when no burst); remembers the burst for the Damaged hook.</summary>
        private float ResonanceBurstFor(DamageInfo info)
        {
            _pendingBurst = ResonanceState.None;
            ResonanceState s;
            try { s = ResonanceSystem.UnderBossBurstState(_lockProxy, info); }
            catch (Exception e)
            {
                Log.Exception("Underheart boss resonance " + (Profile != null ? Profile.Id : "boss"), e);
                return 1f;
            }
            if (s == ResonanceState.None) return 1f;
            _pendingBurst = s;
            return ResonanceRules.BurstDamageMultiplier(true);
        }

        /// <summary>The burst the last filtered hit set off (cleared as it is read).</summary>
        private ResonanceState TakePendingBurst()
        {
            ResonanceState s = _pendingBurst;
            _pendingBurst = ResonanceState.None;
            return s;
        }

        /// <summary>After a hit of the player's side landed on the body: the Resonance loop's burst, energy and marks.</summary>
        private void ResonanceAfterHit(DamageInfo info, ResonanceState burst, float applied, bool killed)
        {
            if (_lockProxy == null) return;
            try { ResonanceSystem.OnUnderBossHit(_lockProxy, info, burst, applied, killed); }
            catch (Exception e) { Log.Exception("Underheart boss resonance hit " + (Profile != null ? Profile.Id : "boss"), e); }
        }

        /// <summary>The Resonance mark on this boss right now (None when unmarked), for the HUD and the debug page.</summary>
        public ResonanceState ResonanceMark
        {
            get { return _lockProxy != null ? ResonanceSystem.StateOf(_lockProxy) : ResonanceState.None; }
        }
    }
}
