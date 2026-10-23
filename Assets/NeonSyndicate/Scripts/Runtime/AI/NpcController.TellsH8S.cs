using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// H8S tells for people and humanoid creatures (read-only views of the melee swing in progress, plus the impact
    /// cadence check for creature swings): how far the wind-up has run, whether the blow can be blocked, and when it will
    /// land on the player. The HUD draws the same "!" or "!!" over them as over creatures, and the impact cadence
    /// (CombatCadenceH8S) reads their landing moments live, so a staggered swing frees its moment at once.
    /// </summary>
    public sealed partial class NpcController
    {
        /// <summary>
        /// Seconds until this melee swing lands on the player, when one is winding up on the player (false otherwise:
        /// no swing, past its wind-up, dead, or aimed at someone else).
        /// </summary>
        public bool MeleeImpactInH8S(out float seconds)
        {
            seconds = 0f;
            if (_attack == null || IsDead || Target == null || !Target.IsPlayer) return false;
            if (_attackT >= _attack.Windup) return false;
            seconds = _attack.Windup - _attackT;
            return true;
        }

        /// <summary>0 to 1 through the melee wind-up in progress (0 when none).</summary>
        public float MeleeWindupProgressH8S
        {
            get { return _attack != null && _attack.Windup > 0f ? Mathf.Clamp01(_attackT / _attack.Windup) : 0f; }
        }

        /// <summary>The swing winding up cannot be blocked (an elite's heavy): the HUD warns with an orange "!!".</summary>
        public bool MeleeUnblockableH8S { get { return _attack != null && _attack.Unblockable; } }

        /// <summary>
        /// A humanoid creature's swing on the player waits a beat when it would land within the cadence gap of another
        /// blow; true when it has to wait (it holds its ground facing the player and tries again shortly).
        /// </summary>
        private bool CadenceWaitsH8S(MeleeMove m)
        {
            if (m == null) return false;
            if (CombatCadenceH8S.Fits(this, m.Windup * CombatDirector.EnemyWindupScale)) return false;
            _nextAttackAt = Time.time + CombatCadenceH8S.RetrySeconds;
            return true;
        }
    }
}
