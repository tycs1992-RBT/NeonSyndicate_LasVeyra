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
        /// <summary>Time before which this attacker does not ask for a melee token again (it is waiting out the cadence).</summary>
        private float _cadenceRetryAtH8S;

        /// <summary>
        /// Seconds until this melee swing lands on the player, when one that counts is winding up on the player
        /// (ImpactCadenceH8S.SwingCounts: false for no swing, one past its wind-up, a dead, knocked-down, reeling or seated
        /// fighter, one out of its fight state, one aimed at someone else, or one beyond SwingReach of the player).
        /// </summary>
        public bool MeleeImpactInH8S(out float seconds)
        {
            seconds = 0f;
            bool has = _attack != null;
            bool atPlayer = Target != null && Target.IsPlayer;
            float windup = has ? _attack.Windup : 0f;
            float dist = atPlayer ? MathUtil.FlatDistance(Target.transform.position, transform.position) : float.MaxValue;
            if (!ImpactCadenceH8S.SwingCounts(has, _attackT, windup, IsDead, atPlayer, State == NpcState.Fight, IsRagdolled, IsStaggered, InVehicle, dist))
                return false;
            seconds = windup - _attackT;
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
        /// H8S: whether this character counts as the player's foe, still readable after its death (IsHostileToPlayer
        /// answers false once dead): every creature, enemies, police while wanted, hostile gangs, anyone attacking the
        /// player. The kill confirmation and the hit weight only follow foes, never civilians.
        /// </summary>
        public bool FoeOfPlayerH8S
        {
            get
            {
                if (IsCreature) return true;
                if (Role == NpcRole.Enemy) return true;
                if (Role == NpcRole.Police || Role == NpcRole.Swat) return Game.Wanted != null && Game.Wanted.Stars > 0;
                if (Role == NpcRole.Gang) return GangHostileToPlayer();
                return Hostile && Target != null && Target.IsPlayer;
            }
        }

        /// <summary>
        /// A swing on the player waits a beat when it would land within the cadence gap of another blow; true when it has
        /// to wait. The attacker gives its melee token back while it waits (TryMeleeTokenH8S refuses it for RetrySeconds),
        /// so a waiting attacker never keeps one of the few tokens from someone whose blow would fit.
        /// </summary>
        private bool CadenceWaitsH8S(MeleeMove m)
        {
            if (m == null) return false;
            if (CombatCadenceH8S.Fits(this, m.Windup * CombatDirector.EnemyWindupScale)) return false;
            CombatDirector.ReleaseMeleeToken(this);
            _cadenceRetryAtH8S = Time.time + CombatCadenceH8S.RetrySeconds;
            return true;
        }

        /// <summary>The melee token (CombatDirector.TryMeleeToken), refused while this attacker waits out the cadence.</summary>
        private bool TryMeleeTokenH8S()
        {
            return Time.time >= _cadenceRetryAtH8S && CombatDirector.TryMeleeToken(this);
        }
    }
}
