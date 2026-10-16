using System;

namespace NeonSyndicate
{
    /// <summary>
    /// The 04D encounter seam on humanoid creatures: while a creature belongs to a finite encounter, its
    /// EncounterRole04D overlay makes the think decision (perception, intent, leash, role pattern) and the species'
    /// own temperament logic stands aside. Body, movement, melee, abilities, damage, rewards and death are unchanged.
    /// </summary>
    public sealed partial class NpcController
    {
        /// <summary>The encounter overlay that owns this creature's decisions (null for every other character).</summary>
        public EncounterRole04D EncounterRole;

        private float _encounterFlinchAt = -99f;

        /// <summary>
        /// Light-hit flinches (a melee tap, a shot during a wind-up) of encounter creatures have a brief resistance window,
        /// so a beam's many samples cannot chain-stun them; poise breaks are unchanged. Every other creature: always true.
        /// </summary>
        private bool Encounter04DFlinch()
        {
            if (EncounterRole == null) return true;
            float now = UnityEngine.Time.time;
            if (!EncounterRules04D.FlinchAllowed(now, _encounterFlinchAt)) return false;
            _encounterFlinchAt = now;
            return true;
        }

        /// <summary>
        /// Called first by ThinkCreature. True when an active overlay made this think's decision. An overlay that throws
        /// is detached (logged) and the creature's own brain takes over again, so a fault can never freeze it.
        /// </summary>
        private bool Encounter04DThink(float playerDist)
        {
            EncounterRole04D r = EncounterRole;
            if (r == null || !r.isActiveAndEnabled) return false;
            try { return r.Think(playerDist); }
            catch (Exception e)
            {
                Log.Exception("Encounter role think", e);
                EncounterRole = null;
                return false;
            }
        }
    }
}
