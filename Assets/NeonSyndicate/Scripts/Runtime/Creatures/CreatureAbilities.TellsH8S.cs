using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// H8S tells for the humanoid creatures' slam (owner: CreatureAbilityKit): the ring is drawn orange (it cannot be
    /// blocked: dodge or leave) with its edge at the shockwave's real reach, the wind-up always leaves time to get out of
    /// the ring (CombatTellsH8S.MinAreaWindup; the Glacier Titan keeps its original timing), and a slam the AI chooses on
    /// its own waits a beat when it would land on top of another blow (CombatCadenceH8S). Scripted boss slams are never
    /// held back by the cadence, but every slam on the player books its landing moment so other blows keep their gap.
    /// </summary>
    public sealed partial class CreatureAbilityKit
    {
        /// <summary>How far past the ring radius the shockwave still hits the player's feet (Shockwave).</summary>
        public const float SlamSlackH8S = 0.4f;

        /// <summary>The slam ring radius for a radius scale (StartSlam's formula).</summary>
        private float SlamRadiusH8S(float radiusScale)
        {
            float s = Npc != null && Npc.Rig != null ? Mathf.Max(0.5f, Npc.Rig.Scale) : 1f;
            return 3f * s * Mathf.Max(0.3f, radiusScale);
        }

        /// <summary>The wind-up a slam plays: the original formula, then (outside the Titan) the time to leave its ring.</summary>
        private float SlamWindupH8S(float windup, float ringRadius)
        {
            float ts = Npc != null && Npc.Rig != null && Npc.Rig.Layout != null ? Npc.Rig.Layout.TimeScale : 1f;
            float w = Mathf.Max(CreatureAttackDb.MinHeavyWindup, windup * CombatDirector.EnemyWindupScale * Mathf.Clamp(ts, 1f, 1.6f));
            if (Kind == HumanoidCreature.GlacierTitan) return w;
            return Mathf.Max(w, CombatTellsH8S.MinAreaWindup(ringRadius + SlamSlackH8S, Settings.Difficulty));
        }

        /// <summary>
        /// True when a slam the AI chose on its own (an ambient golem, an encounter warden) may start now: its landing
        /// keeps the impact cadence with every other blow on the player. Always true against anyone but the player.
        /// </summary>
        public bool SlamFitsH8S(float radiusScale, float windup)
        {
            if (Npc == null || Npc.Target == null || !Npc.Target.IsPlayer) return true;
            float now = Time.time;
            if (now < _slamRetryAtH8S) return false;
            if (CombatCadenceH8S.Fits(Npc, SlamWindupH8S(windup, SlamRadiusH8S(radiusScale)))) return true;
            _slamRetryAtH8S = now + CombatCadenceH8S.RetrySeconds;
            return false;
        }

        /// <summary>A slam that had to wait asks the cadence again only after this time (CombatCadenceH8S.RetrySeconds).</summary>
        private float _slamRetryAtH8S;
        /// <summary>The slam playing now has its landing moment booked in the impact cadence.</summary>
        private bool _slamBookedH8S;

        /// <summary>
        /// Books the landing moment of a slam aimed at the player (StartSlam, once its wind-up is set), scripted boss
        /// slams included: they are never held back, but every other blow keeps its gap from them.
        /// </summary>
        private void BookSlamH8S()
        {
            _slamBookedH8S = false;
            if (Npc == null || Npc.Target == null || !Npc.Target.IsPlayer) return;
            CombatCadenceH8S.Reserve(Npc, _windup);
            _slamBookedH8S = true;
        }

        /// <summary>A slam broken before it landed gives its moment back (Cancel, before the ability is cleared).</summary>
        private void ReleaseSlamH8S()
        {
            bool booked = _slamBookedH8S;
            _slamBookedH8S = false;
            if (booked && _ability == Ability.Slam && !_fired) CombatCadenceH8S.Release(Npc);
        }
    }
}
