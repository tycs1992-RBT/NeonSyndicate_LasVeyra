using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Stealth awareness for the live creatures (Creature.Brain.cs calls StealthGateH8S where it used to take Perceive's
    /// answer at once): a calm creature notices Malik gradually, quicker up close and when he runs, slower when he
    /// crouches, and the sound of his movement carries to it; hunting, fleeing, provoked or planned-hostile creatures
    /// are unchanged. The HUD (StealthHud) reads AwarenessH8S. Numbers in StealthRules.
    /// </summary>
    public sealed partial class Creature
    {
        /// <summary>How aware this creature is of Malik: 0 unaware, StealthRules.Suspicious wary, 1 alarmed.</summary>
        public float AwarenessH8S { get; private set; }
        /// <summary>Seconds left of the alarm flash for the HUD.</summary>
        public float AlertFlashH8S { get { return Mathf.Max(0f, _alertFlashH8S - Time.time); } }

        private float _awareLastH8S = -99f;
        private float _alertFlashH8S;

        /// <summary>Turns this think's raw sight into gradual awareness; true when the creature is alarmed enough to act on it.</summary>
        private bool StealthGateH8S(bool sees, PlayerController p, float dist, float tdt)
        {
            float now = Time.time;
            bool committed = Mode == CreatureMode.Hunt || Mode == CreatureMode.Flee || Mode == CreatureMode.Return || Provoked || _plannedHostile;
            if (committed)
            {
                if (sees) _awareLastH8S = now;
                AwarenessH8S = sees ? StealthRules.Alert : StealthRules.Step(AwarenessH8S, 0f, StealthRules.Alert, now - _awareLastH8S, tdt);
                return sees;
            }
            float rate = 0f;
            float ceiling = StealthRules.Alert;
            if (p != null && !p.IsDead)
            {
                float range = Mathf.Max(5f, (_sp != null ? _sp.Sense : 30f) * Mathf.Clamp(SkillMods.CreatureSense, 0.3f, 2f));
                if (sees) rate = StealthRules.SightRate(dist, range, 1f, p.Crouched, p.FlatVelocity.magnitude, 0f);
                float noise = p.StealthNoiseRadius;
                if (dist < noise)
                {
                    float noiseRate = StealthRules.NoiseRate(dist, noise);
                    if (noiseRate > rate)
                    {
                        rate = noiseRate;
                        ceiling = StealthRules.NoiseCeiling(dist);
                    }
                }
            }
            if (rate > 0f) _awareLastH8S = now;
            float before = AwarenessH8S;
            AwarenessH8S = StealthRules.Step(AwarenessH8S, rate, ceiling, now - _awareLastH8S, tdt);
            if (AwarenessH8S >= StealthRules.Alert && before < StealthRules.Alert) _alertFlashH8S = now + 1.2f;
            return AwarenessH8S >= StealthRules.Alert;
        }
    }
}
